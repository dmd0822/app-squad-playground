# Travel Assistant

A multi-agent travel application built on Azure AI Foundry using C# and Semantic Kernel for orchestration.

## What This Project Does

The Travel Assistant helps users plan trips by coordinating multiple specialized AI agents:

- **POI Agent** — Finds points of interest, attractions, restaurants, and activities
- **Flight Agent** — Searches for flights and provides booking recommendations  
- **Hotel Agent** — Searches for accommodations and lodging options

When a user submits a travel query, the orchestrator routes it to the appropriate agents, which can work in parallel to provide comprehensive responses.

## Solution Structure

```
src/
├── TravelAssistant.slnx              # Solution file
├── TravelAssistant.Abstractions/     # Shared contracts and interfaces
│   ├── ITravelAgent.cs               # Agent interface all agents implement
│   ├── TravelAgentBase.cs            # Base class with common functionality
│   ├── AgentResponse.cs              # Standard response structure
│   ├── TravelContext.cs              # Shared conversation context
│   └── IPromptLoader.cs              # Prompt loading abstraction
├── TravelAssistant.Agents.PointsOfInterest/
│   ├── PoiAgent.cs                   # POI search agent implementation
│   └── Prompts/
│       └── search.prompt.yaml        # POI search prompt template
├── TravelAssistant.Agents.FlightSearch/
│   ├── FlightAgent.cs                # Flight search agent implementation
│   └── Prompts/
│       └── search.prompt.yaml        # Flight search prompt template
├── TravelAssistant.Agents.HotelSearch/
│   ├── HotelAgent.cs                 # Hotel search agent implementation
│   └── Prompts/
│       └── search.prompt.yaml        # Hotel search prompt template
├── TravelAssistant.Host/             # Main orchestration app
│   ├── Program.cs                    # Entry point with DI setup
│   ├── Orchestration/
│   │   └── TravelOrchestrator.cs     # Routes queries to agents
│   ├── YamlPromptLoader.cs           # Loads .prompt.yaml files
│   └── Prompts/
│       └── orchestrator.prompt.yaml  # Orchestrator system prompt
└── TravelAssistant.Tests/            # xUnit test project
    └── AgentCanHandleTests.cs        # Agent routing tests
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure subscription with:
  - Azure AI Foundry project provisioned
  - Azure OpenAI or compatible model deployed (e.g., gpt-4o)
- Visual Studio 2024 or VS Code with C# Dev Kit

### Build and Run

```bash
cd src
dotnet build
dotnet run --project TravelAssistant.Host
```

### Run Tests

```bash
cd src
dotnet test
```

### Azure Setup (Required for LLM Integration)

1. Create an Azure AI Foundry project in the Azure Portal
2. Deploy a model (e.g., gpt-4o) to your project
3. Set environment variables:
   ```bash
   export AZURE_AI_PROJECT_ENDPOINT="https://your-project.api.azureml.ms"
   # Authentication uses DefaultAzureCredential — ensure you're logged in via Azure CLI
   ```

## Prompt File Convention

Prompts are stored in `.prompt.yaml` files alongside each agent in a `Prompts/` folder.

### Format

```yaml
# Required metadata
name: flight_search
description: Searches for flights and provides booking recommendations

# Model settings (optional, defaults shown)
settings:
  model: gpt-4o
  temperature: 0.7
  max_tokens: 2048

# The system prompt (instructions to the LLM)
system_prompt: |
  You are a flight search assistant...

# User message template with {{placeholders}}
user_message_template: |
  Origin: {{origin}}
  Destination: {{destination}}
  User query: {{query}}
```

### How Prompts Are Loaded

- Prompt files are copied to the output directory during build (`PreserveNewest`)
- At runtime, `YamlPromptLoader` reads prompts from `bin/Debug/net10.0/Prompts/{agentId}/`
- Placeholders like `{{destination}}` are substituted at execution time

### Adding a New Agent

1. Create a new class library: `dotnet new classlib -n TravelAssistant.Agents.YourAgent`
2. Add a reference to `TravelAssistant.Abstractions`
3. Implement `ITravelAgent` (or extend `TravelAgentBase`)
4. Create a `Prompts/` folder with your `.prompt.yaml` files
5. Register the agent in `Program.cs`: `builder.Services.AddSingleton<ITravelAgent, YourAgent>()`
6. Update the Host `.csproj` to copy your prompts to output

## Architecture Decisions

### Agent Framework: Semantic Kernel

We chose **Semantic Kernel** over the raw Azure AI Agent Service SDK for orchestration because:

- **Mature multi-agent patterns**: Group chat, pipelines, selection strategies out of the box
- **Plugin architecture**: Easy to expose agents and functions to the LLM
- **Unified model access**: Works with Azure OpenAI, OpenAI, and other providers
- **Active development**: Microsoft's recommended path forward (converging with AutoGen)

The Azure.AI.Projects SDK is still used for Azure AI Foundry resource management (models, connections, datasets).

### Prompt Files Separate from Code

Prompts live in YAML files rather than embedded in C# because:

- Non-developers can edit prompts without touching code
- Prompts can be versioned and reviewed independently
- Easy to A/B test different prompt variations
- Deployment can update prompts without rebuilding

## Next Steps

- [ ] Integrate Semantic Kernel for actual LLM calls
- [ ] Add Azure AI Foundry connection for model deployment
- [ ] Implement tool calling for real flight/hotel APIs
- [ ] Add conversation memory and context management
- [ ] Set up CI/CD pipeline for Azure deployment

## Web UI

The Travel Assistant includes a React frontend and ASP.NET Core Web API backend.

### Run the API

```bash
dotnet run --project src/TravelAssistant.Api
```

The API runs on http://localhost:5000 by default.

**Endpoints:**
- `POST /api/travel/search` — Search flights, hotels, and POI
- `GET /api/travel/health` — Health check

### Run the React App

```bash
cd frontend
npm install
npm run dev
```

The React dev server runs on http://localhost:5173 by default (Vite).