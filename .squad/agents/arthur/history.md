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
