# IndexCities

IndexCities é uma nova base para um city builder 3D desktop, construída em **Godot 4 + C#**.

O projeto começa propositalmente simples: primeiro queremos provar um jogo **jogável, visualmente convincente e tecnicamente saudável**. Processo e arquitetura só crescem quando resolvem um problema real.

## Fonte de verdade

- [`docs/SPEC.md`](docs/SPEC.md) — define **o que o produto é e o que está no escopo atual**.
- [`AGENTS.md`](AGENTS.md) — define **como humanos e IAs devem trabalhar no repositório**.

Uma funcionalidade nova de produto entra primeiro na SPEC. Bugs que apenas restauram comportamento já especificado podem ser corrigidos diretamente.

## Origem

IndexCities aproveita as lições aprendidas no [CityBuilder](https://github.com/Marlohn/CityBuilder), mas não carrega automaticamente sua arquitetura, processo ou implementação.

O CityBuilder continua sendo referência histórica e técnica, especialmente para simulação, determinismo, save/replay, dados e experimentos visuais. O novo projeto deve reaproveitar somente o que continuar fazendo sentido.
