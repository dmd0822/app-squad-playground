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

### 2026-03-06: Three Agents Implemented + Testing Required

**Agent Implementations Complete (Ford):**
- **PoiAgent** — Semantic Kernel 1.73.0, JSON-based output, optional Kernel constructor
- **FlightAgent** — SK pattern, metadata-based parameters (Origin/CabinClass), JSON fenced-block output
- **HotelAgent** — Azure AI Foundry Agent Framework (mixed framework; standardization decision pending)

**Web UI Added:** TravelAssistant.Api project + React frontend in /frontend. All components implemented.

**Testing Scope for Marvin:**
1. **Unit Tests** — All three agents need `ProcessAsync` tests
   - PoiAgent: Mock `IChatCompletionService`, construct test `Kernel`, verify JSON parsing
   - FlightAgent: Same pattern, verify Origin/CabinClass metadata reading
   - HotelAgent: Mock `AIProjectClient`, verify response parsing

2. **Integration Tests** — Tag with [Integration] for optional CI skip
   - Live agent calls to Azure OpenAI (POI, Flight) and Azure AI Foundry (Hotel)
   - Full TravelOrchestrator orchestration with all three agents
   - Verify parallel fan-out behavior

3. **API Controller Tests** — TravelController.Search endpoint
   - Mock agents, verify endpoint returns flat response structure
   - Test error handling and aggregation

4. **Cross-Agent Contract Tests** — Verify DTOs serialize/deserialize correctly
   - PoiSearchResult → PoiItem mapping
   - FlightSearchResult → FlightItem mapping (flagged: airport code fields to-be-decided)
   - HotelSearchResult → HotelItem mapping

**Framework Notes for Tests:**
- Both SK and Azure AI Foundry agents now in production — tests must cover both paths
- Mixed framework approach approved short-term; standardization pending (see decisions.md)
- Optional constructor pattern used in SK agents — tests should exercise both constructor paths

**Test Infrastructure Needed:**
- Azure OpenAI credentials for live integration tests (environment-based config)
- Azure AI Foundry credentials for HotelAgent tests
- Mock fixtures for unit testing all three agent patterns

**Current Build Status:** Solution builds, all 9 initial tests passing. Tests cover only `CanHandle` logic. Agent logic tests (ProcessAsync) need to be written.
