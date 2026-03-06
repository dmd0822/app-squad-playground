# Team Decisions

## 2026-03-06: Agent Framework and Project Structure
**By:** Arthur (Lead Architect)
**What:** Use Semantic Kernel for orchestration with Azure.AI.Projects for resource management. Solution structure: src/TravelAssistant.sln with projects Abstractions, Agent.POI, Agent.Flight, Agent.Hotel, Host, Tests. Prompts in each agent's Prompts/ folder as .prompt.yaml files.
**Status:** Accepted
**Rationale:** Semantic Kernel provides mature multi-agent patterns (group chat, pipelines, selection strategies). Aligns with Microsoft's strategic direction (SK + AutoGen convergence).

## 2026-03-06: Project bootstrapped
**By:** Dave Davis
**What:** Multi-agent travel assistant on Azure AI Foundry using C#. Initial agents: POI search, flight search, hotel search. Prompts are maintained in separate files from code (not embedded in C# source). Agent Framework to be confirmed at implementation start.
**Status:** Accepted
