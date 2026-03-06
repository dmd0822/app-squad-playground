# Ford — History

## Core Context
**Project:** Multi-agent travel assistant on Azure AI Foundry
**Stack:** C#, Azure AI Foundry, Agent Framework
**User:** Dave Davis

**What we are building:**
A multi-agent travel application hosted on Azure AI Foundry. Three initial agents:
1. Points of Interest (POI) search agent
2. Flight search agent
3. Hotel search agent

All agent prompts live in separate files (not inline C# strings). Project framework TBD (Semantic Kernel
or Azure AI Agent Service SDK) — Arthur will confirm before first implementation sprint.

## Learnings

### 2026-03-06: Project Scaffolded by Arthur

**What:** Arthur scaffolded the complete .NET multi-agent solution structure.

**Solution Created:** `src/TravelAssistant.sln` with 6 projects:
- TravelAssistant.Abstractions — Shared contracts
- TravelAssistant.Agents.PointsOfInterest — POI agent
- TravelAssistant.Agents.FlightSearch — Flight agent
- TravelAssistant.Agents.HotelSearch — Hotel agent
- TravelAssistant.Host — Orchestration (TravelOrchestrator, YamlPromptLoader)
- TravelAssistant.Tests — xUnit tests (9 passing)

**Framework Decision:** Semantic Kernel for orchestration (mature multi-agent patterns)

**Prompt Convention:** YAML files at `{AgentProject}/Prompts/{promptName}.prompt.yaml`

**Build Status:** Solution builds, all 9 tests passing. Ready for Ford to implement agent logic.
