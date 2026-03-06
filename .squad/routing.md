# Routing Rules

## Default Routing
| Task Signal | Route To |
|-------------|----------|
| Architecture, system design, orchestration, agent wiring | Arthur |
| C# implementation, agent code, SDK usage, Azure AI Foundry code | Ford |
| Azure infrastructure, deployment, configuration, Foundry setup | Trillian |
| Prompt file authoring and management | Ford |
| Testing, QA, edge cases, integration testing | Marvin |
| Session logs, decisions, cross-agent memory | Scribe |
| Work queue, backlog monitoring | Ralph |

## Domain Routing
| Domain | Primary | Secondary |
|--------|---------|-----------|
| Agent orchestration pattern | Arthur | Ford |
| Individual agent implementation (POI, flight, hotel) | Ford | Arthur |
| Prompt engineering (separate .prompt files) | Ford | Arthur |
| Azure AI Foundry SDK / agent registration | Ford | Trillian |
| Azure deployment, infrastructure-as-code (Bicep) | Trillian | Arthur |
| Unit & integration tests | Marvin | Ford |
| CI/CD pipelines | Trillian | — |
| Adding a new agent | Arthur (design) | Ford (impl) |
