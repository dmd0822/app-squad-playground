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

### 2026-03-06: Web UI Architecture Added

**New Projects:**
- `src/TravelAssistant.Api/` — ASP.NET Core Web API exposing REST endpoints
- `frontend/` — React + Vite + TypeScript frontend at repo root

**API Design:**
- Runs on http://localhost:5000
- CORS configured for React dev server (http://localhost:5173)
- `POST /api/travel/search` — accepts TravelSearchRequest, fans out to all 3 agents, returns unified TravelSearchResponse
- `GET /api/travel/health` — health check endpoint

**DTO Contracts (in TravelAssistant.Api/Models/):**
- `TravelSearchRequest` — destination, origin, checkIn, checkOut, guests, interests
- `TravelSearchResponse` — unified response with PoiResultDto, FlightResultDto, HotelResultDto, errors list
- Item DTOs: PoiItem, FlightItem, HotelItem

**React Frontend Structure (frontend/src/):**
- `types/travel.ts` — TypeScript interfaces mirroring C# DTOs
- `services/travelApi.ts` — axios-based API client
- `components/SearchForm.tsx` — travel search form
- `components/PoiResults.tsx`, `FlightResults.tsx`, `HotelResults.tsx` — result displays
- `App.tsx` — main layout with React Query integration

**Key Files Ford Needs to Implement:**
1. `src/TravelAssistant.Api/Controllers/TravelController.cs` — implement POST /api/travel/search (marked with TODO)
   - Build TravelContext from request
   - Construct query from request fields
   - Call TravelOrchestrator.ProcessQueryAsync
   - Map AgentResponse list to TravelSearchResponse DTOs
2. `frontend/src/components/SearchForm.tsx` — implement form with validation
3. `frontend/src/components/PoiResults.tsx` — render POI items
4. `frontend/src/components/FlightResults.tsx` — render flight options
5. `frontend/src/components/HotelResults.tsx` — render hotel options

### 2026-03-06: Three Agents Fully Implemented + Web UI Complete

**Agent Implementations (Ford):**
- **PoiAgent** — Semantic Kernel 1.73.0, JSON-based output contract, optional Kernel constructor for test/prod flexibility
- **FlightAgent** — Same SK pattern as POI, metadata-based Origin/CabinClass parameters, JSON fenced-block output
- **HotelAgent** — Azure AI Foundry Agent Framework (mixed framework approach; team to decide on standardization)

**API Implementation (Ford):**
- `TravelController.cs` — `POST /api/travel/search` orchestrates all three agents in parallel (fan-out)
- Flat response structure (no nested wrappers): `{ destination, searchedAt, pointsOfInterest[], flights[], hotels[], errors[] }`
- Enriches `TravelContext.Metadata` with Origin/CabinClass for Flight agent
- DI wiring complete — PoiAgent and FlightAgent use factory lambdas, HotelAgent requires AIProjectClient configuration

**React Frontend (Ford):**
- All components implemented (SearchForm, ResultsList, PoiCard, FlightCard, HotelCard)
- TypeScript types match C# DTO flat structure exactly
- Client consumes TravelController endpoint successfully
- React dev server on :5173, API on :5000

**Key Decisions & Flags:**
- **Mixed framework approach:** POI/Flight use Semantic Kernel, Hotel uses Azure AI Foundry. Team decision pending on standardization (see decisions.md)
- **Airport codes:** FlightOption lacks airport fields — team to decide whether to backfill or remove from DTO
- **Metadata conventions:** Origin/CabinClass stored as convention in TravelContext.Metadata — Arthur should consider typed properties
- **Agent IDs:** String literals in controller routing — consider exposing shared constants in Abstractions

**Current Status:** Solution builds, all three agents functional, Web UI fully integrated. Ready for end-to-end testing and team decision review on framework standardization.

### 2026-03-06: Azure Agent Framework Migration Specified

**Decision:** Dave Davis directed full migration from Semantic Kernel to Azure AI Foundry Agent Framework.

**Key architectural points established:**
1. **Single SDK:** `Azure.AI.Projects 2.0.0-beta.1` for all agents
2. **Reference implementation:** HotelAgent already implements the correct pattern — POI and Flight agents must match
3. **Constructor signature:** All agents take `(IPromptLoader, AIProjectClient?, string modelDeployment)`
4. **Thread-safe registration:** Lazy `SemaphoreSlim`-guarded `EnsureAgentRegisteredAsync()` pattern
5. **Graceful fallback:** When `AIProjectClient` is null, agents return stub responses (confidence 0.5) instead of throwing
6. **No API keys:** `DefaultAzureCredential` replaces AzureOpenAI API key configuration

**Files produced:**
- `.squad/agents/ford/migration-spec-azure-agent-framework.md` — full implementation brief for Ford
- `.squad/decisions/inbox/arthur-azure-agent-framework-migration.md` — decision record

**Abstractions unchanged:** `ITravelAgent`, `TravelAgentBase`, `TravelOrchestrator` remain framework-agnostic — no modifications needed.

**Environment variable migration:**
- Old: `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName`
- New: `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT`, `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT`

**Assignees:** Ford (implementation), Marvin (test updates)

### 2026-03-06: Azure Agent Framework Migration — Complete

**Outcome:** Full migration completed successfully. All agents now use Azure AI Foundry Agent Framework exclusively.

**Execution Summary:**
- **PoiAgent:** Fully migrated to Azure AI Foundry pattern, all tests passing
- **FlightAgent:** Fully migrated to Azure AI Foundry pattern, all tests passing
- **HotelAgent:** Already reference implementation, no changes needed
- **Host/Api DI:** Updated to unified `AIProjectClient` registration, removed all Semantic Kernel wiring

**Build Status:** ✅ Clean build, 0 errors, 0 warnings, all 9 tests passing

**Architecture Achievement:** Framework divergence fully resolved. Project now standardized on single SDK across all three agents. No more mixed pattern confusion. Environment variables simplified to `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` and `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT`.

**Orchestration logs:**
- `.squad/orchestration-log/2026-03-06T20-34-59Z-arthur.md` — Arthur's specification work
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-poi.md` — PoiAgent migration
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-flight.md` — FlightAgent migration
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-di.md` — DI wiring updates

**Session log:** `.squad/log/2026-03-06T20-34-59Z-azure-agent-framework-migration.md`
