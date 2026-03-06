# Marvin — History

## Core Context
**Project:** Multi-agent travel assistant on Azure AI Foundry
**Stack:** C#, Azure AI Foundry, Agent Framework
**User:** Dave Davis

**What we are building:**
A multi-agent travel application hosted on Azure AI Foundry. Three initial agents: POI search,
flight search, hotel search. More will follow. All prompts are separate files from C# code.
Test framework: xUnit. Integration tests tagged [Integration] to allow CI to skip Azure-dependent tests.

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
