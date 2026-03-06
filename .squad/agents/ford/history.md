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

### 2026-03-06: Flight Search Agent Fully Implemented

**What:** Implemented the complete FlightAgent, FlightSearchResult model, and the
`search.prompt.yaml` prompt for the flight search agent.

**Files produced / modified:**
- `src/TravelAssistant.Agents.FlightSearch/FlightAgent.cs` — full SK implementation
- `src/TravelAssistant.Agents.FlightSearch/FlightSearchResult.cs` — typed result model
- `src/TravelAssistant.Agents.FlightSearch/Prompts/search.prompt.yaml` — crafted prompt
- `src/TravelAssistant.Agents.FlightSearch/TravelAssistant.Agents.FlightSearch.csproj` — added `Microsoft.SemanticKernel 1.73.0`
- `src/TravelAssistant.Host/Program.cs` — FlightAgent DI registration with Kernel

**Key implementation decisions:**

1. **Optional Kernel constructor overload** — `FlightAgent` has two constructors:
   `FlightAgent(IPromptLoader)` and `FlightAgent(IPromptLoader, Kernel)`.
   The single-arg form preserves compatibility with existing `CanHandle`-only tests.
   The two-arg form is used in production DI.

2. **Typed result model** — `FlightSearchResult` + `FlightOption` records live in
   the FlightSearch project (not Abstractions) because they are agent-specific.
   `AgentResponse.Data` is typed as `object?` so callers cast to `FlightSearchResult`.

3. **Prompt asks for JSON + prose** — The prompt requests a natural-language summary
   followed by a fenced `\`\`\`json … \`\`\`` block. `ParseFlightSearchResult()` extracts
   the JSON block if present; falls back to prose-only `FlightSearchResult` with an
   empty `Options` list if the model returns unstructured text.

4. **Template variable substitution** — A private `BuildUserMessage()` method
   handles all `{{variable}}` replacements. Origin and CabinClass come from
   `TravelContext.Metadata["Origin"]` / `["CabinClass"]` (keys by convention);
   everything else comes from typed `TravelContext` fields.

5. **DI wiring** — In `Program.cs` the FlightAgent is registered with a factory
   lambda (`sp => new FlightAgent(...)`) to explicitly pass the `Kernel` singleton.
   This is cleaner than relying on the DI container to resolve the two-arg ctor
   ambiguously.

6. **SK version alignment** — Pinned `Microsoft.SemanticKernel` to `1.73.0` in
   the FlightSearch project, matching the Host project's pinned version.

**Build / test status:** `dotnet build` clean, all 9 pre-existing tests pass.


### 2026-03-06: POI Agent Fully Implemented

**What:** Implemented the full POI search agent with Semantic Kernel integration.

**Files created/modified:**
- `TravelAssistant.Agents.PointsOfInterest/PoiAgent.cs` — Full implementation inheriting `TravelAgentBase`
- `TravelAssistant.Agents.PointsOfInterest/PoiSearchResult.cs` — Typed result model (`PoiSearchResult`, `PoiItem`)
- `TravelAssistant.Agents.PointsOfInterest/Prompts/search.prompt.yaml` — Rewritten with structured JSON output instructions
- `TravelAssistant.Agents.PointsOfInterest/TravelAssistant.Agents.PointsOfInterest.csproj` — Added `Microsoft.SemanticKernel 1.73.0`
- `TravelAssistant.Host/TravelAssistant.Host.csproj` — Added SK + `Microsoft.SemanticKernel.Connectors.AzureOpenAI 1.73.0`
- `TravelAssistant.Host/Program.cs` — Kernel registration reading Azure OpenAI config from `IConfiguration`

**Key patterns used:**

1. **Optional `Kernel?` constructor injection** — `PoiAgent(IPromptLoader, Kernel? kernel = null)` keeps existing unit tests passing (they only test `CanHandle`) while allowing full DI injection in production. When kernel is null, `ProcessAsync` throws `InvalidOperationException` with a descriptive message.

2. **`IPromptLoader` + manual template rendering** — Rather than using SK's `KernelFunction` for template rendering, the user-message template from YAML is rendered with simple `string.Replace` for `{{destination}}`, `{{travel_dates}}`, `{{interests}}`, `{{query}}`. This avoids coupling the agent to SK's prompt template engine while keeping prompts external.

3. **`IChatCompletionService` directly** — Retrieved from the kernel via `_kernel.GetRequiredService<IChatCompletionService>()`. Used with a `ChatHistory` (system + user messages). This is the lowest-ceremony SK pattern for a single chat call.

4. **JSON output contract in prompt** — The system prompt instructs the LLM to return only valid JSON matching a specific schema. `ParsePoiResult` then tries to extract and deserialize JSON from the response using `System.Text.Json`, falling back to storing raw text in `Summary` if parsing fails. This keeps the agent resilient to LLM non-compliance.

5. **`AgentResponse.Data` carries typed result** — `PoiSearchResult` is stored in `Data`, `Message` carries a human-readable summary. Orchestrators and callers that need structured data cast `Data` to `PoiSearchResult`.

6. **SK version pinned to 1.73.0** — SK 1.30.0 required `OpenAI = 2.1.0-beta.2` which conflicted with `Azure.AI.Projects 2.0.0-beta.1` in HotelSearch (which requires `OpenAI >= 2.8.0`). SK 1.73.0 resolved against `OpenAI 2.8.0` without conflicts.

**Build status:** Solution builds, all 9 tests passing.

### 2026-03-06: Hotel Search Agent Fully Implemented

**What:** Implemented `HotelAgent` using the **Azure AI Foundry Agent Framework** (`Azure.AI.Projects`
+ `Azure.AI.Projects.OpenAI`) — NOT Semantic Kernel. Dave explicitly directed this agent to use the
Azure AI Agent Service SDK.

**Files created / modified:**
- `src/TravelAssistant.Agents.HotelSearch/HotelAgent.cs` — Full Azure AI Foundry implementation
- `src/TravelAssistant.Agents.HotelSearch/HotelSearchResult.cs` — Typed result model (`HotelSearchResult`, `HotelOption`)
- `src/TravelAssistant.Agents.HotelSearch/Prompts/search.prompt.yaml` — Rewritten with structured hotel recommendation prompt and correct template variables
- `src/TravelAssistant.Agents.HotelSearch/TravelAssistant.Agents.HotelSearch.csproj` — Added `Azure.AI.Projects 2.0.0-beta.1` (brings in `Azure.AI.Projects.OpenAI 2.0.0-beta.1` + `OpenAI 2.8.0` transitively)

**Key implementation decisions:**

1. **Azure AI Foundry pattern (not SK)** — `HotelAgent` uses `AIProjectClient` for agent
   registration (`projectClient.Agents.CreateAgentVersionAsync`) and `ProjectResponsesClient`
   (via `projectClient.OpenAI.GetProjectResponsesClientForAgent`) for invocation.
   This is the "new agents" pattern in Azure AI Foundry: `PromptAgentDefinition` + Responses API.

2. **Optional `AIProjectClient?` constructor injection** — Two constructors: `HotelAgent(IPromptLoader)`
   for tests/dev (no AI Foundry client needed) and `HotelAgent(IPromptLoader, AIProjectClient?, string)`
   for production. Graceful fallback when client is null.

3. **Lazy, thread-safe agent registration** — `EnsureAgentRegisteredAsync` uses `SemaphoreSlim(1,1)`
   to register the agent in Azure AI Foundry exactly once per process lifetime. The system prompt from
   the YAML file becomes the agent's `Instructions` in the `PromptAgentDefinition`.

4. **Prompt variables** — Template uses `{{destination}}`, `{{check_in}}`, `{{check_out}}`,
   `{{guests}}`, `{{star_rating}}`, `{{budget_per_night}}`, `{{query}}`. `BuildUserMessage()`
   substitutes these via `string.Replace`. Star rating comes from `TravelContext.Metadata["star_rating"]`.

5. **`OPENAI001` diagnostic suppressed** — `ResponseItem` and `CreateResponseOptions` from
   `OpenAI.Responses` are marked experimental in v2.8.0. Suppressed with targeted `#pragma warning`
   around the call site only.

6. **`HotelSearchResult` is agent-specific** — Lives in the HotelSearch project, not Abstractions.
   `AgentResponse.Data` carries it as `object?`; consumers cast to `HotelSearchResult`.

7. **Structured output gap** — `Hotels` list is empty in the returned `HotelSearchResult`;
   the raw AI response is in `RawResponse`. A future parsing step can populate `Hotels`.

**Build / test status:** Solution builds clean (0 errors, 0 warnings), all 9 pre-existing tests pass.

**Framework divergence note:** POI and Flight agents use Semantic Kernel; Hotel uses Azure AI Agent
Framework. Arthur should be informed so the Abstractions layer can be reviewed for consistency.

### 2026-03-06: TravelController.Search() Endpoint Implemented

**What:** Replaced the `throw new NotImplementedException` stub with a full implementation of `POST api/travel/search`.

**Files modified:**
- `src/TravelAssistant.Api/Controllers/TravelController.cs` — full Search() implementation + three private mapping helpers
- `src/TravelAssistant.Api/Program.cs` — changed `FlightAgent` registration to explicit factory lambda (matching Host pattern) to guarantee the Kernel is injected into the two-arg constructor

**Key implementation decisions:**

1. **TravelContext field mapping** — `CheckIn`/`CheckOut` (DateOnly) map to `DepartureDate`/`ReturnDate` on `TravelContext`; `Guests` maps to `TravelerCount`; `Origin` and `Interests` go into `Metadata["Origin"]` and `Metadata["interests"]` respectively (keys match what FlightAgent and PoiAgent read from `Metadata`).

2. **Routing by AgentId string constants** — Used literal strings `"poi"`, `"flight"`, `"hotel"` in the switch rather than importing the agent ID constants, to avoid coupling the controller to the agent assemblies beyond what's necessary for mapping. (All three agent AgentId constants match these strings.)

3. **Data cast, not JSON deserialise** — `AgentResponse.Data` carries the typed result object directly in memory (no JSON roundtrip in the in-process orchestrator). The controller casts with `response.Data is PoiSearchResult` etc. rather than calling `JsonSerializer`. This is simpler and correct for the synchronous fan-out pattern.

4. **PoiItem name collision** — Two types named `PoiItem` exist: `TravelAssistant.Agents.PointsOfInterest.PoiItem` and `TravelAssistant.Api.Models.PoiItem`. Resolved with `using` aliases (`AgentPoiItem`, `AgentPoiSearchResult`) at the top of the file so the Models versions stay unqualified.

5. **FlightItem airport gap** — `FlightOption` does not carry airport codes; `FlightItem` requires `DepartureAirport`/`ArrivalAirport` as non-null strings. Mapped to `string.Empty` pending a future prompt/model change to emit those fields.

6. **FlightAgent DI factory** — The API `Program.cs` previously used `AddSingleton<ITravelAgent, FlightAgent>()`. The DI container has two `FlightAgent` constructors to choose from; the explicit factory lambda (`sp => new FlightAgent(loader, kernel)`) guarantees the Kernel-enabled constructor is always selected, matching the Host project pattern.

**Build / test status:** Solution builds clean, all 9 pre-existing tests pass.

### 2026-03-06: React Frontend UI Implemented

**What:** Implemented all React UI components for the Travel Assistant frontend.

**Files created / modified:**
- `frontend/src/types/travel.ts` — Replaced scaffold types with flat DTO types (`PoiItem`, `FlightOption`, `HotelOption` directly on `TravelSearchResponse`). Removed the intermediate `*ResultDto` wrapper types that the scaffold had, as the backend API exposes flat arrays.
- `frontend/src/components/SearchForm.tsx` — Full controlled form with fields: destination (required), origin, check-in, check-out, guests (default 1), interests. Submits via `onSearch` callback; disables all fields + changes button label while `isLoading`.
- `frontend/src/components/PoiResults.tsx` — Lists POI items with name (bold), category badge, description, visit time, address. Props: `items: PoiItem[]`.
- `frontend/src/components/FlightResults.tsx` — Lists flight options with airline, flight number, cabin class badge, formatted departure/arrival times, duration, stop count (shows "Direct"), and price range. Props: `flights: FlightOption[]`.
- `frontend/src/components/HotelResults.tsx` — Lists hotels with name, star rating (⭐ × n), price/night range, location, top 3 amenities, cancellation policy. Props: `hotels: HotelOption[]`.
- `frontend/src/App.tsx` — Updated prop names to `items=`, `flights=`, `hotels=` to match new component interfaces.
- `frontend/src/App.css` — Replaced Vite boilerplate with app-specific styles: single-column layout, result panels, badges, cards.

**Key decisions:**

1. **Flat types** — `TravelSearchResponse` carries `pointsOfInterest: PoiItem[]`, `flights: FlightOption[]`, `hotels: HotelOption[]` directly. The old nested `*ResultDto` wrappers are gone. The API contract must match.

2. **No external CSS framework** — All styling via inline styles for component-local layout and shared CSS classes in `App.css` for panels, badges, and cards. Keeps the bundle lean.

3. **Inline styles for layout, CSS classes for shared patterns** — Grid/flex layout within components uses inline styles (local, no naming collisions); reusable atoms like `.result-card`, `.badge`, `.empty-msg` are in `App.css`.

4. **`formatTime` helper in FlightResults** — Parses ISO datetime strings from the API and formats to `HH:MM` for display. Falls back to raw string on parse failure.

5. **`StarRating` sub-component** — Tiny helper renders ⭐ emoji × star count (capped at 5). Avoids repeating string repeat logic.

**Build status:** `npm run build` clean (0 TS errors, 0 warnings), 131 modules transformed.

### 2026-03-06: FlightAgent Migrated to Azure AI Foundry Agent Framework

**What:** Migrated FlightAgent from Semantic Kernel to Azure AI Foundry Agent Framework to match HotelAgent pattern. Part of team-wide standardization on Azure.AI.Projects SDK.

**Files modified:**
- `src/TravelAssistant.Agents.FlightSearch/FlightAgent.cs` — Complete rewrite following canonical Azure AI Foundry pattern
- `src/TravelAssistant.Agents.FlightSearch/TravelAssistant.Agents.FlightSearch.csproj` — Replaced `Microsoft.SemanticKernel 1.73.0` with `Azure.AI.Projects 2.0.0-beta.1`
- `src/TravelAssistant.Host/TravelAssistant.Host.csproj` — Updated `Azure.Identity` to `1.17.1` (required by Azure.AI.Projects)
- `src/TravelAssistant.Api/TravelAssistant.Api.csproj` — Updated `Azure.Identity` to `1.17.1`

**Key implementation decisions:**

1. **Followed HotelAgent reference pattern exactly** — Used HotelAgent.cs as canonical implementation guide. FlightAgent now has identical constructor signature, registration pattern, and ProcessAsync structure to HotelAgent.

2. **Two-constructor pattern preserved** — 
   - `FlightAgent(IPromptLoader)` — test-only, chains to production constructor with `null` client
   - `FlightAgent(IPromptLoader, AIProjectClient?, string)` — production, full Azure AI Foundry integration
   - Maintains backward compatibility with existing unit tests that only test `CanHandle()`

3. **Replaced SK invocation with Azure AI Foundry pattern:**
   - REMOVED: `Kernel?`, `IChatCompletionService`, `ChatHistory`, `GetChatMessageContentAsync()`
   - ADDED: `AIProjectClient?`, `EnsureAgentRegisteredAsync()` with `SemaphoreSlim(1,1)`, `PromptAgentDefinition`, `CreateAgentVersionAsync()`, `GetProjectResponsesClientForAgent()`, `CreateResponseAsync()`, `GetOutputText()`
   - Added `#pragma warning disable/restore OPENAI001` around experimental API call

4. **Kept all flight-specific logic intact:**
   - `FlightSearchResult` + `FlightOption` models (separate file unchanged)
   - `ParseFlightSearchResult()` — JSON parsing with ```json fence extraction
   - `BuildUserMessage()` — template variable substitution ({{origin}}, {{destination}}, {{departure_date}}, {{return_date}}, {{passengers}}, {{cabin_class}}, {{query}})
   - `GetMetaString()` helper for reading `Metadata["Origin"]` and `Metadata["CabinClass"]`
   - `Keywords` array and `CanHandle()` — unchanged

5. **Graceful fallback when client is null** — Returns a valid `AgentResponse` with confidence 0.5 and message "Flight search prompt loaded. Configure Azure AI Foundry for live results." Does NOT throw exception. Follows HotelAgent pattern exactly.

6. **Constants renamed for consistency:**
   - `AgentIdValue` → `AgentName` (matches HotelAgent)
   - `SearchPromptName` → `PromptName` (matches HotelAgent)
   - Added `DefaultModelDeployment = "gpt-4o"` constant

7. **Azure.Identity version conflict resolved** — Azure.AI.Projects 2.0.0-beta.1 requires Azure.Identity >= 1.17.1. Updated both Host and Api projects from 1.13.2 to 1.17.1 to resolve NuGet downgrade errors.

**Build / test status:** Solution builds clean (0 errors, 0 warnings), all 9 pre-existing tests pass.

**Migration complete:** FlightAgent now uses identical Azure AI Foundry pattern as HotelAgent. Only PoiAgent remains on Semantic Kernel.

### 2026-03-06: DI Wiring Migrated to Azure AI Foundry

**What:** Updated DI wiring in both `TravelAssistant.Host/Program.cs` and `TravelAssistant.Api/Program.cs` to use Azure AI Foundry Agent Framework (`Azure.AI.Projects`) instead of Semantic Kernel.

**Files modified:**
- `src/TravelAssistant.Host/Program.cs` — Removed SK registration, added `AIProjectClient` registration with graceful fallback
- `src/TravelAssistant.Api/Program.cs` — Same DI changes as Host
- `src/TravelAssistant.Host/TravelAssistant.Host.csproj` — Removed SK packages, added `Azure.AI.Projects 2.0.0-beta.1` + `Azure.Identity 1.17.1`
- `src/TravelAssistant.Api/TravelAssistant.Api.csproj` — Same package changes as Host

**Key implementation decisions:**

1. **Unified agent registration pattern** — All three agents (POI, Flight, Hotel) now use identical factory lambda pattern: `sp => new XAgent(promptLoader, projectClient, modelDeployment)`. This matches the new constructor signature `(IPromptLoader, AIProjectClient?, string)` that all agents implement after migration.

2. **Configuration key change** — Switched from `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName` to `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` and `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT`. The new pattern uses `DefaultAzureCredential` (managed identity support), not API keys.

3. **Graceful null handling** — When `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` is missing, `projectClient` is null and agents return fallback responses. No exceptions at startup; warning printed to console.

4. **Azure.Identity version** — Used `1.17.1` (not `1.13.2`) because `Azure.AI.Projects 2.0.0-beta.1` transitively requires `>= 1.17.1`. Package downgrade error prompted the correction.

5. **Removed all Semantic Kernel references** — Both `Microsoft.SemanticKernel 1.73.0` and `Microsoft.SemanticKernel.Connectors.AzureOpenAI 1.73.0` packages removed from both projects. No SK usings remain in Program.cs files.

**Build status:** Solution builds clean (0 errors, 0 warnings). All 7 projects compile successfully.

### 2026-03-06: POI Agent Migrated to Azure Agent Framework

**What:** Migrated `PoiAgent` from Semantic Kernel to Azure AI Foundry Agent Framework following the canonical pattern from `HotelAgent`.

**Files modified:**
- `src/TravelAssistant.Agents.PointsOfInterest/PoiAgent.cs` — Complete rewrite to Azure Agent Framework pattern
- `src/TravelAssistant.Agents.PointsOfInterest/TravelAssistant.Agents.PointsOfInterest.csproj` — Replaced `Microsoft.SemanticKernel 1.73.0` with `Azure.AI.Projects 2.0.0-beta.1`

**Key implementation patterns:**

1. **Two-constructor pattern** — `PoiAgent(IPromptLoader)` for test-only use (chains to production constructor with null client), and `PoiAgent(IPromptLoader, AIProjectClient?, string modelDeploymentName = "gpt-4o")` for production with full Azure AI Foundry integration.

2. **Constants renamed for consistency** — Changed `Id = "poi"` to `AgentName = "poi"` and `SearchPromptName = "search"` to `PromptName = "search"` to match the standard pattern. Added `DefaultModelDeployment = "gpt-4o"` constant.

3. **Removed all Semantic Kernel code** — Deleted `Kernel?` field, `IChatCompletionService`, `ChatHistory`, `GetChatMessageContentAsync()` invocation pattern, and `PromptExecutionSettings`. No more `using Microsoft.SemanticKernel` references.

4. **Added Azure AI Foundry agent registration** — Implemented `EnsureAgentRegisteredAsync()` with `SemaphoreSlim(1,1)` for thread-safe lazy registration. Uses `PromptAgentDefinition` with system prompt as `Instructions`, then calls `CreateAgentVersionAsync()`.

5. **Switched to Responses API invocation** — Uses `GetProjectResponsesClientForAgent()` → `CreateResponseAsync()` → `GetOutputText()` pattern with `#pragma warning disable/restore OPENAI001` around the experimental API call.

6. **Graceful null client fallback** — When `_projectClient is null`, returns valid `AgentResponse` with fallback message and confidence 0.5 instead of throwing `InvalidOperationException`. Allows tests to pass without Azure AI Foundry configuration.

7. **Preserved all existing logic** — Kept `ParsePoiResult()` method with JSON extraction and deserialization, `BuildUserMessage()` (renamed from `RenderUserMessage()`), `FormatTravelDates()`, `BuildSummaryMessage()`, `Keywords` array, `CanHandle()`, and internal `PoiResultJson` + `PoiItemJson` DTOs intact.

8. **PoiSearchResult.cs unchanged** — Separate file with `PoiItem` and `PoiSearchResult` records remains untouched; no model changes required.

**Build / test status:** Solution builds clean (0 errors, 0 warnings), all 9 tests passing. POI agent now follows identical Azure Agent Framework pattern as Hotel and Flight agents.

### 2026-03-06: Azure Agent Framework Migration — Complete

**Overall outcome:** Full standardization on Azure AI Foundry Agent Framework achieved. All three agents (POI, Flight, Hotel) now use unified SDK and DI pattern.

**Migration sweep:**
1. **PoiAgent** — Migrated from SK to Azure AI Foundry; graceful null fallback; all tests pass
2. **FlightAgent** — Migrated from SK to Azure AI Foundry; retained all flight logic and metadata patterns; all tests pass
3. **HotelAgent** — Already reference implementation; unchanged
4. **DI wiring** — Unified across both Host and Api projects; `AIProjectClient` registration; no more mixed pattern

**Build verification:** ✅ Clean (0 errors, 0 warnings), all 9 tests passing, all 7 projects compile

**Orchestration logs:**
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-poi.md`
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-flight.md`
- `.squad/orchestration-log/2026-03-06T20-34-59Z-ford-di.md`

**Session log:** `.squad/log/2026-03-06T20-34-59Z-azure-agent-framework-migration.md`

