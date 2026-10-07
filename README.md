# IndexCities

IndexCities é uma nova base para um city builder 3D desktop, construída em **Godot 4 + C#**.

O projeto começa propositalmente simples: primeiro queremos provar um jogo **jogável, visualmente convincente e tecnicamente saudável**. Processo e arquitetura só crescem quando resolvem um problema real.

## Documentos principais

- [`docs/SPEC.md`](docs/SPEC.md) — **o que o produto é e o que está decidido**.
- [`docs/EXPLORATION.md`](docs/EXPLORATION.md) — **pesquisas, ideias, alternativas e decisões ainda em formação**.
- [`AGENTS.md`](AGENTS.md) — **como humanos e IAs devem trabalhar e como esses documentos se relacionam**.

Uma funcionalidade nova de produto entra primeiro na SPEC. Pesquisa e discussão ficam na EXPLORATION até a decisão ser fechada. Bugs que apenas restauram comportamento já especificado podem ser corrigidos diretamente.

## Origem

IndexCities aproveita as lições aprendidas no [CityBuilder](https://github.com/Marlohn/CityBuilder), mas não carrega automaticamente sua arquitetura, processo ou implementação.

O CityBuilder continua sendo referência histórica e técnica, especialmente para simulação, determinismo, save/replay, dados e experimentos visuais. O novo projeto deve reaproveitar somente o que continuar fazendo sentido.
