# Arthur — History

## Core Context
**Project:** Multi-agent travel assistant on Azure AI Foundry
**Stack:** C#, Azure AI Foundry, Agent Framework
**User:** Dave Davis

**What we're building:**
A multi-agent travel application hosted on Azure AI Foundry. Three initial agents:
1. Points of Interest (POI) search agent
2. Flight search agent
3. Hotel search agent

More agents will be added in the future. All prompts live in separate files (not embedded in C# code).

## Learnings

### 2026-03-06: Project Structure Established

**Framework Decision:** Semantic Kernel for orchestration
- Chosen over raw Azure.AI.Projects SDK because SK provides mature multi-agent patterns (group chat, pipelines, selection strategies)
- Azure.AI.Projects will still be used for resource management when needed
- This aligns with Microsoft's strategic direction (SK + AutoGen convergence)

**Prompt File Convention:**
- Location: `{AgentProject}/Prompts/{promptName}.prompt.yaml`
- Format: YAML with `system_prompt`, `user_message_template`, and `settings` sections
- Loaded via `IPromptLoader` abstraction, implemented by `YamlPromptLoader`
- Files are copied to output directory at build time (CopyToOutputDirectory: PreserveNewest)

**Solution Structure Created:**
```
src/
├── TravelAssistant.slnx
├── TravelAssistant.Abstractions/     # ITravelAgent, AgentResponse, TravelContext
├── TravelAssistant.Agents.PointsOfInterest/
├── TravelAssistant.Agents.FlightSearch/
├── TravelAssistant.Agents.HotelSearch/
├── TravelAssistant.Host/             # TravelOrchestrator, YamlPromptLoader
└── TravelAssistant.Tests/            # 9 passing tests
```

**Key Files:**
- `src/TravelAssistant.Abstractions/ITravelAgent.cs` — Contract all agents implement
- `src/TravelAssistant.Host/Orchestration/TravelOrchestrator.cs` — Fan-out orchestration
- `src/TravelAssistant.Host/YamlPromptLoader.cs` — Loads .prompt.yaml files
- Each agent has `Prompts/search.prompt.yaml`

**Build verified:** All 9 tests passing, solution compiles cleanly.
