# Migration Specification: Azure Agent Framework Standardization

**Author:** Arthur (Lead Architect)  
**Date:** 2026-03-06  
**Status:** Implementation Brief — Dave Davis has directed this migration  
**For:** Ford (Backend Dev)

---

## Executive Summary

All three travel agents must standardize on the Azure AI Foundry Agent Framework (`Azure.AI.Projects` SDK). The HotelAgent already implements this pattern correctly and serves as the reference implementation. Remove all Semantic Kernel dependencies from POI and Flight agents, then refactor them to match the HotelAgent's `PromptAgentDefinition` + Responses API pattern. This is a framework consolidation — the agent contracts (`ITravelAgent`, `AgentResponse`) and orchestration layer remain unchanged.

---

## 1. Chosen SDK and Version

### Primary Package
```
Azure.AI.Projects 2.0.0-beta.1
```

**Verified:** This is the version currently installed in `TravelAssistant.Agents.HotelSearch.csproj` and is the correct package for Azure AI Foundry Agent Framework.

### Transitive Dependencies (automatically included)
- `Azure.AI.Projects.OpenAI` — Provides `GetProjectResponsesClientForAgent()` extension
- `Azure.Identity` — For `DefaultAzureCredential` authentication
- `OpenAI` — Provides `ResponseResult`, `CreateResponseOptions`, `ResponseItem` types

**Note:** The pragma `#pragma warning disable OPENAI001` is required because the `CreateResponseAsync` overload is in preview.

---

## 2. Canonical Agent Pattern

All agents must follow this exact pattern, derived from `HotelAgent`:

### 2.1 Required Usings

```csharp
using System.Text.RegularExpressions;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using TravelAssistant.Abstractions;
```

### 2.2 Constructor Signature

Every agent must implement **two constructors**:

```csharp
public class MyAgent : TravelAgentBase
{
    private const string PromptName = "search";
    private const string DefaultModelDeployment = "gpt-4o";

    private readonly AIProjectClient? _projectClient;
    private readonly string _modelDeploymentName;
    private string? _registeredAgentName;
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    /// <summary>
    /// Test-only constructor: no Azure AI Foundry integration.
    /// ProcessAsync returns a graceful fallback, not an exception.
    /// </summary>
    public MyAgent(IPromptLoader promptLoader)
        : this(promptLoader, null, DefaultModelDeployment)
    {
    }

    /// <summary>
    /// Production constructor: full Azure AI Foundry integration.
    /// </summary>
    public MyAgent(
        IPromptLoader promptLoader,
        AIProjectClient? projectClient,
        string modelDeploymentName = DefaultModelDeployment)
        : base(promptLoader)
    {
        _projectClient = projectClient;
        _modelDeploymentName = modelDeploymentName;
    }
}
```

### 2.3 Agent Registration Pattern (Thread-Safe Lazy Init)

Every agent must lazily register itself with Azure AI Foundry using this pattern:

```csharp
/// <summary>
/// Lazily registers (or re-uses) the agent in Azure AI Foundry.
/// Thread-safe — only one registration request is sent even under concurrent load.
/// </summary>
private async Task EnsureAgentRegisteredAsync(
    PromptTemplate template,
    CancellationToken cancellationToken)
{
    if (_registeredAgentName is not null) return;

    await _registrationLock.WaitAsync(cancellationToken);
    try
    {
        if (_registeredAgentName is not null) return;

        var definition = new PromptAgentDefinition(_modelDeploymentName)
        {
            Instructions = template.SystemPrompt
        };

        var agentVersion = await _projectClient!.Agents
            .CreateAgentVersionAsync(
                agentName: AgentName, // e.g., "poi", "flight", "hotel"
                options: new AgentVersionCreationOptions(definition),
                cancellationToken: cancellationToken);

        _registeredAgentName = agentVersion.Value.Name;
    }
    finally
    {
        _registrationLock.Release();
    }
}
```

### 2.4 ProcessAsync Implementation Pattern

```csharp
public override async Task<AgentResponse> ProcessAsync(
    string query,
    TravelContext context,
    CancellationToken cancellationToken = default)
{
    try
    {
        // 1. Load prompt template
        var template = await PromptLoader.LoadAsync(AgentId, PromptName, cancellationToken);
        
        // 2. Build user message with variable substitution
        var userMessage = BuildUserMessage(query, context, template);

        // 3. If Azure AI Foundry is configured, invoke the agent
        if (_projectClient is not null)
        {
            await EnsureAgentRegisteredAsync(template, cancellationToken);

            var responseClient = _projectClient.OpenAI
                .GetProjectResponsesClientForAgent(_registeredAgentName!);

#pragma warning disable OPENAI001
            ResponseResult result = await responseClient.CreateResponseAsync(
                new CreateResponseOptions([ResponseItem.CreateUserMessageItem(userMessage)]),
                cancellationToken);
#pragma warning restore OPENAI001

            var message = result.GetOutputText();

            // 4. Parse structured result (agent-specific)
            var data = ParseResult(message, context);

            return CreateResponse(message, data: data, confidence: 0.9);
        }

        // 5. Graceful fallback when no client configured (test scenarios)
        var fallbackData = CreateFallbackData(context);
        return CreateResponse(
            "Prompt loaded. Configure Azure AI Foundry for live results.",
            data: fallbackData,
            confidence: 0.5);
    }
    catch (Exception ex)
    {
        return CreateErrorResponse($"{DisplayName} failed: {ex.Message}");
    }
}
```

### 2.5 Key Differences from Semantic Kernel Pattern

| Aspect | Old (Semantic Kernel) | New (Azure AI Foundry) |
|--------|----------------------|------------------------|
| Chat abstraction | `IChatCompletionService` | `ProjectResponsesClient` |
| Chat state | `ChatHistory` object | Single `ResponseItem` list |
| Model selection | `PromptExecutionSettings.ModelId` | `PromptAgentDefinition(modelDeployment)` |
| System prompt | Added to `ChatHistory` constructor | `PromptAgentDefinition.Instructions` |
| Invocation | `GetChatMessageContentAsync()` | `CreateResponseAsync()` |
| Output | `result.Content` | `result.GetOutputText()` |
| No-config behavior | Throws `InvalidOperationException` | Returns graceful fallback |

---

## 3. Per-Agent Migration Table

### 3.1 PoiAgent

| File | Action |
|------|--------|
| `PoiAgent.cs` | **Rewrite** using canonical pattern |
| `TravelAssistant.Agents.PointsOfInterest.csproj` | **Replace** `Microsoft.SemanticKernel` with `Azure.AI.Projects` |

**Remove from PoiAgent.cs:**
```csharp
// DELETE these usings:
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// DELETE these fields:
private readonly Kernel? _kernel;

// DELETE this constructor pattern:
public PoiAgent(IPromptLoader promptLoader, Kernel? kernel = null)

// DELETE this invocation pattern:
var chatHistory = new ChatHistory(promptTemplate.SystemPrompt);
var chatService = _kernel.GetRequiredService<IChatCompletionService>();
var result = await chatService.GetChatMessageContentAsync(...);

// DELETE the InvalidOperationException throw when _kernel is null
```

**Keep in PoiAgent.cs:**
- `PoiSearchResult`, `PoiItem` models
- `ParsePoiResult()` method (rename internal DTOs if desired)
- `RenderUserMessage()` / `BuildUserMessage()` method
- `Keywords` array
- `CanHandle()` implementation
- `CreateResponse()` / `CreateErrorResponse()` (inherited from base)

**New PoiAgent constants:**
```csharp
public const string AgentName = "poi";  // Used for agent registration
private const string PromptName = "search";
private const string DefaultModelDeployment = "gpt-4o";
```

### 3.2 FlightAgent

| File | Action |
|------|--------|
| `FlightAgent.cs` | **Rewrite** using canonical pattern |
| `TravelAssistant.Agents.FlightSearch.csproj` | **Replace** `Microsoft.SemanticKernel` with `Azure.AI.Projects` |

**Remove from FlightAgent.cs:**
```csharp
// DELETE these usings:
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// DELETE these fields:
private readonly Kernel? _kernel;

// DELETE both SK-based constructors:
public FlightAgent(IPromptLoader promptLoader)
public FlightAgent(IPromptLoader promptLoader, Kernel kernel)

// DELETE this invocation pattern:
var chatHistory = new ChatHistory(promptTemplate.SystemPrompt);
var chatService = _kernel.GetRequiredService<IChatCompletionService>();
var result = await chatService.GetChatMessageContentAsync(...);

// DELETE PromptExecutionSettings with ExtensionData
```

**Keep in FlightAgent.cs:**
- `FlightSearchResult`, `FlightOption` models
- `ParseFlightSearchResult()` method
- `BuildUserMessage()` method
- `GetMetaString()` helper
- `Keywords` array
- `CanHandle()` implementation

**New FlightAgent constants:**
```csharp
public const string AgentName = "flight";  // Used for agent registration
private const string PromptName = "search";
private const string DefaultModelDeployment = "gpt-4o";
```

### 3.3 HotelAgent

| File | Action |
|------|--------|
| `HotelAgent.cs` | **No changes** — this is the reference |
| `TravelAssistant.Agents.HotelSearch.csproj` | **No changes** |

---

## 4. DI Wiring Changes

### 4.1 TravelAssistant.Host/Program.cs

**DELETE all Semantic Kernel registration:**

```csharp
// DELETE:
using Microsoft.SemanticKernel;

// DELETE: Kernel builder block (lines 21-39 approximately)
var kernelBuilder = Kernel.CreateBuilder();
var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];
var deployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
{
#pragma warning disable SKEXP0010
    kernelBuilder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
#pragma warning restore SKEXP0010
}
else { ... warning ... }
builder.Services.AddSingleton(kernelBuilder.Build());

// DELETE: Old agent registrations
builder.Services.AddSingleton<ITravelAgent, PoiAgent>();
builder.Services.AddSingleton<ITravelAgent>(sp =>
    new FlightAgent(
        sp.GetRequiredService<IPromptLoader>(),
        sp.GetRequiredService<Kernel>()));
builder.Services.AddSingleton<ITravelAgent, HotelAgent>();
```

**ADD new Azure AI Foundry registration:**

```csharp
using Azure.AI.Projects;
using Azure.Identity;
using TravelAssistant.Abstractions;
using TravelAssistant.Agents.PointsOfInterest;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;
using TravelAssistant.Host;
using TravelAssistant.Host.Orchestration;

var builder = Host.CreateApplicationBuilder(args);

// Register prompt loader
builder.Services.AddSingleton<IPromptLoader, YamlPromptLoader>();

// Azure AI Foundry configuration
var projectEndpoint = builder.Configuration["AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"];
var modelDeployment = builder.Configuration["AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT"] ?? "gpt-4o";

AIProjectClient? projectClient = null;
if (!string.IsNullOrWhiteSpace(projectEndpoint))
{
    projectClient = new AIProjectClient(
        new Uri(projectEndpoint),
        new DefaultAzureCredential());
    builder.Services.AddSingleton(projectClient);
}
else
{
    Console.WriteLine("⚠️  Azure AI Foundry not configured — agents will return fallback responses.");
    Console.WriteLine("    Set AZURE_AI_FOUNDRY_PROJECT_ENDPOINT in configuration.");
}

// Register all agents with unified pattern
builder.Services.AddSingleton<ITravelAgent>(sp =>
    new PoiAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new FlightAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new HotelAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

// Register orchestrator
builder.Services.AddSingleton<TravelOrchestrator>();

var host = builder.Build();
// ... rest of Program.cs unchanged ...
```

### 4.2 TravelAssistant.Api/Program.cs

Apply the **exact same changes** as Host/Program.cs:

```csharp
using Azure.AI.Projects;
using Azure.Identity;
using TravelAssistant.Abstractions;
using TravelAssistant.Agents.PointsOfInterest;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;
using TravelAssistant.Host;
using TravelAssistant.Host.Orchestration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register prompt loader
builder.Services.AddSingleton<IPromptLoader, YamlPromptLoader>();

// Azure AI Foundry configuration
var projectEndpoint = builder.Configuration["AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"];
var modelDeployment = builder.Configuration["AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT"] ?? "gpt-4o";

AIProjectClient? projectClient = null;
if (!string.IsNullOrWhiteSpace(projectEndpoint))
{
    projectClient = new AIProjectClient(
        new Uri(projectEndpoint),
        new DefaultAzureCredential());
    builder.Services.AddSingleton(projectClient);
}
else
{
    Console.WriteLine("⚠️  Azure AI Foundry not configured — agents will return fallback responses.");
    Console.WriteLine("    Set AZURE_AI_FOUNDRY_PROJECT_ENDPOINT in configuration.");
}

// Register all agents with unified pattern
builder.Services.AddSingleton<ITravelAgent>(sp =>
    new PoiAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new FlightAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new HotelAgent(
        sp.GetRequiredService<IPromptLoader>(),
        projectClient,
        modelDeployment));

// Register orchestrator
builder.Services.AddSingleton<TravelOrchestrator>();

var app = builder.Build();
// ... rest of Program.cs unchanged ...
```

---

## 5. Files to Delete/Clean

### 5.1 NuGet Package Removals

| Project | Remove | Add |
|---------|--------|-----|
| `TravelAssistant.Agents.PointsOfInterest.csproj` | `Microsoft.SemanticKernel 1.73.0` | `Azure.AI.Projects 2.0.0-beta.1` |
| `TravelAssistant.Agents.FlightSearch.csproj` | `Microsoft.SemanticKernel 1.73.0` | `Azure.AI.Projects 2.0.0-beta.1` |
| `TravelAssistant.Agents.HotelSearch.csproj` | (none) | (already correct) |
| `TravelAssistant.Host.csproj` | `Microsoft.SemanticKernel 1.73.0`<br>`Microsoft.SemanticKernel.Connectors.AzureOpenAI 1.73.0` | `Azure.AI.Projects 2.0.0-beta.1`<br>`Azure.Identity 1.*` |
| `TravelAssistant.Api.csproj` | `Microsoft.SemanticKernel 1.73.0`<br>`Microsoft.SemanticKernel.Connectors.AzureOpenAI 1.73.0` | `Azure.AI.Projects 2.0.0-beta.1`<br>`Azure.Identity 1.*` |

### 5.2 Code Patterns to Eliminate

**Across all agent files, remove any:**
- `using Microsoft.SemanticKernel;`
- `using Microsoft.SemanticKernel.ChatCompletion;`
- `Kernel` fields, parameters, or constructor arguments
- `IChatCompletionService` usage
- `ChatHistory` construction
- `PromptExecutionSettings` with SK-specific properties
- `GetChatMessageContentAsync()` calls
- `#pragma warning disable SKEXP0010` (SK-specific pragma)

### 5.3 Abstractions Layer — NO CHANGES

These remain unchanged:
- `ITravelAgent.cs`
- `AgentResponse.cs`
- `TravelContext.cs`
- `TravelAgentBase.cs`
- `IPromptLoader.cs`
- `PromptTemplate.cs`

The `TravelAgentBase` class contains no framework references — it only depends on `IPromptLoader` and defines abstract methods. This is correct and should not change.

### 5.4 Orchestration Layer — NO CHANGES

`TravelOrchestrator.cs` calls `agent.ProcessAsync(query, context, ct)` which is framework-agnostic. No changes required.

---

## 6. Environment Variables

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` | Full endpoint URL of the Azure AI Foundry project | `https://myresource.services.ai.azure.com/api/projects/myproject` |
| `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT` | Model deployment name (optional, defaults to `gpt-4o`) | `gpt-4o` |

### Removed Variables

Remove these from configuration (no longer used):
| Variable | Reason |
|----------|--------|
| `AzureOpenAI:Endpoint` | Replaced by `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` |
| `AzureOpenAI:ApiKey` | Replaced by `DefaultAzureCredential` |
| `AzureOpenAI:DeploymentName` | Replaced by `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT` |

### Authentication

Azure AI Foundry uses `DefaultAzureCredential` which automatically handles:
- Local development: Azure CLI, Visual Studio, environment variables
- Production: Managed Identity

No API keys should be stored in configuration after this migration.

---

## 7. Test Considerations

### Unit Tests

Existing unit tests that only call `CanHandle()` or construct agents with just `IPromptLoader` will continue to work — the single-parameter constructor still exists and returns graceful fallbacks.

Tests that previously mocked `IChatCompletionService` or `Kernel` will need to be rewritten if they invoke `ProcessAsync`. Two options:

1. **Fallback testing:** Call `ProcessAsync` without an `AIProjectClient` — agent returns a fallback response with `confidence: 0.5`
2. **Integration testing:** Provide a real `AIProjectClient` pointing to an Azure AI Foundry test project

### Marvin should:
- Verify all existing tests still pass after migration
- Add integration tests that exercise the full Azure AI Foundry flow
- Update any mocks that reference Semantic Kernel types

---

## 8. Migration Checklist for Ford

- [ ] Update `TravelAssistant.Agents.PointsOfInterest.csproj` packages
- [ ] Rewrite `PoiAgent.cs` using canonical pattern
- [ ] Update `TravelAssistant.Agents.FlightSearch.csproj` packages
- [ ] Rewrite `FlightAgent.cs` using canonical pattern
- [ ] Update `TravelAssistant.Host.csproj` packages
- [ ] Rewrite `TravelAssistant.Host/Program.cs` DI registration
- [ ] Update `TravelAssistant.Api.csproj` packages
- [ ] Rewrite `TravelAssistant.Api/Program.cs` DI registration
- [ ] Run `dotnet restore` on solution
- [ ] Run `dotnet build` — must compile with no errors
- [ ] Run `dotnet test` — all tests must pass
- [ ] Manual smoke test: start API, verify agents respond

---

## Appendix: Complete PoiAgent Skeleton

For reference, here's what the migrated `PoiAgent.cs` should look like structurally:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.PointsOfInterest;

public class PoiAgent : TravelAgentBase
{
    public const string AgentName = "poi";
    private const string PromptName = "search";
    private const string DefaultModelDeployment = "gpt-4o";

    private static readonly string[] Keywords = [...]; // Keep existing

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AIProjectClient? _projectClient;
    private readonly string _modelDeploymentName;
    private string? _registeredAgentName;
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    public PoiAgent(IPromptLoader promptLoader)
        : this(promptLoader, null, DefaultModelDeployment) { }

    public PoiAgent(
        IPromptLoader promptLoader,
        AIProjectClient? projectClient,
        string modelDeploymentName = DefaultModelDeployment)
        : base(promptLoader)
    {
        _projectClient = projectClient;
        _modelDeploymentName = modelDeploymentName;
    }

    public override string AgentId => AgentName;
    public override string DisplayName => "Points of Interest";

    public override bool CanHandle(string query, TravelContext context)
    {
        // Keep existing implementation
    }

    public override async Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await PromptLoader.LoadAsync(AgentId, PromptName, cancellationToken);
            var userMessage = RenderUserMessage(template.UserMessageTemplate, query, context);

            if (_projectClient is not null)
            {
                await EnsureAgentRegisteredAsync(template, cancellationToken);

                var responseClient = _projectClient.OpenAI
                    .GetProjectResponsesClientForAgent(_registeredAgentName!);

#pragma warning disable OPENAI001
                ResponseResult result = await responseClient.CreateResponseAsync(
                    new CreateResponseOptions([ResponseItem.CreateUserMessageItem(userMessage)]),
                    cancellationToken);
#pragma warning restore OPENAI001

                var responseText = result.GetOutputText();
                var poiResult = ParsePoiResult(responseText, context.Destination ?? query);

                return CreateResponse(
                    BuildSummaryMessage(poiResult),
                    data: poiResult,
                    confidence: 0.9);
            }

            // Graceful fallback
            return CreateResponse(
                "POI prompt loaded. Configure Azure AI Foundry for live results.",
                data: new PoiSearchResult
                {
                    Destination = context.Destination ?? "unknown",
                    PointsOfInterest = []
                },
                confidence: 0.5);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"POI search failed: {ex.Message}");
        }
    }

    private async Task EnsureAgentRegisteredAsync(
        PromptTemplate template,
        CancellationToken cancellationToken)
    {
        // Copy from HotelAgent — use AgentName constant
    }

    // Keep existing helper methods:
    private static string RenderUserMessage(...) { ... }
    private static PoiSearchResult ParsePoiResult(...) { ... }
    private static string BuildSummaryMessage(...) { ... }
}

// Keep existing model classes:
public class PoiSearchResult { ... }
public class PoiItem { ... }
```

---

**End of specification. Ford: implement this exactly as written. No design deviations without Arthur review.**
