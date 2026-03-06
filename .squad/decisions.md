# Team Decisions

## 2026-03-06: Project bootstrapped
**By:** Dave Davis
**What:** Multi-agent travel assistant on Azure AI Foundry using C#. Initial agents: POI search, flight search, hotel search. Prompts are maintained in separate files from code (not embedded in C# source). Agent Framework to be confirmed at implementation start.
**Status:** Accepted

## 2026-03-06: Agent Framework and Project Structure
**By:** Arthur (Lead Architect)
**What:** Use Semantic Kernel for orchestration with Azure.AI.Projects for resource management. Solution structure: src/TravelAssistant.sln with projects Abstractions, Agent.POI, Agent.Flight, Agent.Hotel, Host, Tests. Prompts in each agent's Prompts/ folder as .prompt.yaml files.
**Status:** Accepted
**Rationale:** Semantic Kernel provides mature multi-agent patterns (group chat, pipelines, selection strategies). Aligns with Microsoft's strategic direction (SK + AutoGen convergence).

## 2026-03-06: POI Agent Implementation Patterns
**From:** Ford (Backend Dev)  
**Date:** 2026-03-06  
**Status:** Proposed — for team awareness

### Context
POI Search Agent is first fully-implemented agent. Patterns chosen here serve as template for Flight and Hotel agents.

### Decisions Made

**1. Semantic Kernel version: 1.73.0**
- SK 1.30.0 has transitive `OpenAI = 2.1.0-beta.2` that conflicts with `Azure.AI.Projects 2.0.0-beta.1` (requires `OpenAI >= 2.8.0`)
- SK 1.73.0 resolves cleanly against `OpenAI 2.8.0`
- **Action:** FlightSearch should pin to `1.73.0` for consistency

**2. Optional `Kernel?` constructor parameter**
- `PoiAgent(IPromptLoader promptLoader, Kernel? kernel = null)`
- Keeps existing unit tests intact; allows full DI injection in production
- When `ProcessAsync` called without kernel, throws `InvalidOperationException` with clear message
- **Note for Marvin:** Test `ProcessAsync` with mock `IChatCompletionService` constructed in test

**3. Direct `IChatCompletionService` usage — not `KernelFunction`**
- Agent retrieves `IChatCompletionService` from kernel and builds `ChatHistory` manually
- Uses system prompt and rendered user message from `IPromptLoader`
- Avoids coupling agent to SK's prompt template engine (Handlebars, Liquid, etc.)
- All prompt text stays in YAML files

**4. LLM output contract: JSON-only**
- System prompt instructs LLM to return valid JSON matching documented schema
- `ParsePoiResult` extracts and deserialises JSON
- Graceful fallback: raw text stored in `Summary` if LLM does not comply
- Allows structured data to flow through `AgentResponse.Data` as typed `PoiSearchResult`

**5. Azure OpenAI configuration via `IConfiguration`**
- Host reads `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName` from `IConfiguration`
- If not present, warning printed but app starts
- Supports local development (user-secrets / appsettings.Development.json) and production (env vars / Key Vault)
- **Action for Trillian:** Confirm configuration key names; consider managed identity (keyless auth) for production

### Files Affected
| File | Change |
|------|--------|
| `TravelAssistant.Agents.PointsOfInterest/PoiAgent.cs` | Full SK implementation |
| `TravelAssistant.Agents.PointsOfInterest/PoiSearchResult.cs` | New typed result model |
| `TravelAssistant.Agents.PointsOfInterest/Prompts/search.prompt.yaml` | JSON output contract prompt |
| `TravelAssistant.Agents.PointsOfInterest/*.csproj` | Added SK 1.73.0 |
| `TravelAssistant.Host/*.csproj` | Added SK 1.73.0 + AzureOpenAI connector |
| `TravelAssistant.Host/Program.cs` | Kernel registration from IConfiguration |

## 2026-03-06: Flight Agent Implementation Patterns
**Author:** Ford (Backend Dev)
**Date:** 2026-03-06
**Status:** Accepted

### Context
Flight Search Agent required several small decisions that the full team should be aware of — especially patterns that will recur in Hotel and POI agents.

### Decisions

**1. Typed result models live in the agent project, not Abstractions**
- `FlightSearchResult` and `FlightOption` are in `TravelAssistant.Agents.FlightSearch`
- `AgentResponse.Data` remains `object?`. Callers that need typed data cast explicitly
- Keeps Abstractions lean and avoids coupling other agents to flight-specific types
- **Recommendation:** Apply same pattern for HotelSearchResult, PoiSearchResult, etc.

**2. Optional Kernel constructor preserves test compatibility**
- `FlightAgent` has two constructors — one with only `IPromptLoader` (unit tests) and one with `Kernel` (production)
- Avoids forcing tests to construct a Kernel or mock it
- **Recommendation:** All agents that use SK should follow this two-constructor pattern

**3. Origin and CabinClass travel via `TravelContext.Metadata`**
- `TravelContext` has no `Origin` or `CabinClass` fields
- These are stored in `Metadata["Origin"]` and `Metadata["CabinClass"]` by convention
- Orchestrator or calling code sets them before invoking agent
- **Action required for Arthur/Trillian:** Consider whether `Origin` and `CabinClass` should be promoted to first-class fields on `TravelContext`

**4. Prompt requests JSON in a fenced block for structured parsing**
- Flight prompt asks LLM to return natural-language summary plus ``` json … ``` fenced block
- Agent parses JSON block and falls back to prose-only if parsing fails
- **Pattern can be reused** in other agents that return structured lists (hotels, POI)

**5. SK version pinned to 1.73.0 across projects**
- Both `TravelAssistant.Host` and `TravelAssistant.Agents.FlightSearch` now pin `Microsoft.SemanticKernel` to `1.73.0`
- Any new agent project that adds SK should use same version to avoid transitive diamond conflicts

## 2026-03-06: Hotel Agent Uses Azure AI Foundry Agent Framework (Not Semantic Kernel)
**From:** Ford (Backend Dev)
**Date:** 2026-03-06
**Status:** Proposed — needs team review

### What changed
Hotel Search Agent (`HotelAgent`) implemented using Azure AI Foundry Agent Framework (`Azure.AI.Projects` + `Azure.AI.Projects.OpenAI` packages) at Dave Davis's explicit direction.

Other two agents (POI, Flight) implemented with Semantic Kernel (`Microsoft.SemanticKernel`).

### Decision needed: Standardise on one agent execution framework

Project now has two different execution frameworks for agent calls:

| Agent | Package | Pattern |
|---|---|---|
| `PoiAgent` | `Microsoft.SemanticKernel 1.73.0` | `IChatCompletionService` |
| `FlightAgent` | `Microsoft.SemanticKernel 1.73.0` | `IChatCompletionService` |
| `HotelAgent` | `Azure.AI.Projects 2.0.0-beta.1` | `PromptAgentDefinition` + Responses API |

**Options:**

**A) Keep the mixed approach**
- Each agent can use whichever SDK fits best
- Acceptable short-term; may create DI complexity in Host and confusion for future agents

**B) Migrate POI + Flight to Azure AI Foundry**
- Aligns with Dave's direction for Hotel
- Removes Semantic Kernel dependency from three agent projects
- Host still needs Kernel for orchestration-level features if Arthur's design uses them

**C) Migrate Hotel to Semantic Kernel**
- Reverses the directive
- Not recommended unless Dave reconsiders

### Impact on Abstractions
`TravelAgentBase` currently only injects `IPromptLoader`. Neither `AIProjectClient` nor `Kernel` is in base class or any interface. Intentional (agents manage own SDK dependencies). No changes to Abstractions required for current mixed approach.

### DI wiring needed for HotelAgent in Host
```csharp
// Configure AIProjectClient (reads endpoint from config / env vars)
var projectEndpoint = builder.Configuration["AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("Missing AZURE_AI_FOUNDRY_PROJECT_ENDPOINT");
var modelDeployment = builder.Configuration["AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT"] ?? "gpt-4o";

builder.Services.AddSingleton(new AIProjectClient(
    new Uri(projectEndpoint),
    new DefaultAzureCredential()));

builder.Services.AddSingleton<HotelAgent>(sp =>
    new HotelAgent(
        sp.GetRequiredService<IPromptLoader>(),
        sp.GetRequiredService<AIProjectClient>(),
        modelDeployment));
```

Environment variables required:
- `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` — e.g., `https://<resource>.services.ai.azure.com/api/projects/<project>`
- `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT` — model deployment name (e.g., `gpt-4o`)

### Recommendation
Accept the mixed approach for now (Option A) to keep momentum. Schedule brief team sync to decide whether POI and Flight agents should also migrate to Azure AI Foundry before the next sprint. Flag to Trillian to ensure infrastructure provisioning covers the Azure AI Foundry project endpoint and model deployment.

## 2026-03-06: TravelController.Search() — API Contract Choices
**By:** Ford (Backend Dev)
**Date:** 2026-03-06

### Airport codes not present in FlightOption

`FlightOption` (returned by FlightAgent) has no `DepartureAirport` / `ArrivalAirport` fields, but `FlightItem` DTO declares both as required strings. Currently mapped to `string.Empty`. 

**Team should decide** whether to:
- Add airport fields to `FlightOption` (prompt + model change in FlightAgent)
- Remove the required constraint on those DTO fields
- Drop the fields from `FlightItem` in favour of the richer `EstimatedPriceRange` / `CabinClass` that FlightOption carries but FlightItem does not expose

### TravelContext metadata keys are convention-only

`Origin` (capital O) and `interests` (lowercase) are metadata keys read by FlightAgent and PoiAgent respectively. These are convention, not enforced by any contract. 

**Arthur may want to:** Add typed properties to `TravelContext` or introduce an `ITravelContext` interface to make these explicit.

### AgentId routing by string literal

Controller switches on `"poi"`, `"flight"`, `"hotel"` as literals. These match agent constant values today. If agent IDs change, controller must be updated. 

**Consider:** Exposing a shared constants class in `TravelAssistant.Abstractions` for canonical IDs.

### FlightAgent DI registration pattern

Both Host and API `Program.cs` now use same explicit factory lambda for `FlightAgent` to guarantee the Kernel-enabled constructor is selected. 

**This pattern should be standard** for any future agent with optional constructor injection.

## 2026-03-06: Web UI Architecture
**By:** Arthur
**What:** Added React + ASP.NET Core Web API to travel assistant

**Decision:** 
- TravelAssistant.Api (new project) exposes REST endpoints, wires up all three agents via DI
- React + Vite + TypeScript frontend in /frontend at repo root
- API runs on :5000, React dev server on :5173 (Vite default)
- TravelController.POST /api/travel/search fans out to all three agents and returns unified response
- DTOs defined in Api project, TypeScript types mirrored in frontend/src/types/travel.ts

**Why:** Dave requested React + ASP.NET Core API as UI layer

## 2026-03-06: React Frontend — Flat DTO Contract
**By:** Ford
**What:** React frontend uses flat arrays for results in `TravelSearchResponse`, not nested `*ResultDto` wrapper objects

**Decision:**
- `TravelSearchResponse` shape: `{ destination, searchedAt, pointsOfInterest: PoiItem[], flights: FlightOption[], hotels: HotelOption[], errors: string[] }`
- C# API controller must return this flat structure (no wrapping in `PoiResultDto` / `FlightResultDto` / `HotelResultDto`)
- TypeScript types and component props all match this flat shape

**Why:** Scaffold had nested `{ success, message, items[] }` wrapper per result type, but task spec from Dave replaced it with flat arrays. Frontend is now built to that spec; backend must match.

**Action needed:** Arthur / Trillian — verify `TravelController` serializes to this shape. If API still returns nested wrappers, either update API or update frontend types.
