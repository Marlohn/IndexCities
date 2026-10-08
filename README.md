# IndexCities

IndexCities é um projeto novo em fase inicial de definição.

A intenção é começar com uma folha em branco: pesquisar, discutir e decidir o produto progressivamente, sem herdar automaticamente arquitetura, requisitos ou decisões de trabalhos anteriores.

## Documentos principais

- [`docs/SPEC.md`](docs/SPEC.md) — **o que já foi decidido para o produto**.
- [`docs/EXPLORATION.md`](docs/EXPLORATION.md) — **pesquisas, ideias, alternativas e decisões ainda em formação**.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — **decisões estruturais de software**.
- [`AGENTS.md`](AGENTS.md) — **como humanos e IAs devem trabalhar e como esses documentos se relacionam**.

Uma funcionalidade nova de produto entra primeiro na SPEC. Decisões estruturais de software vão para a ARCHITECTURE. Pesquisa e discussão ficam na EXPLORATION até a decisão ser fechada.

O desenvolvimento segue **Spec-Driven Development (SDD)**: primeiro são definidas as regras do produto na SPEC, depois o jogo é implementado. A **primeira entrega e sua validação principal serão integradas**, não uma série de POCs isoladas por sistema. A implementação pode evoluir modularmente e incluir verificações técnicas pontuais.

O processo é propositalmente leve: estrutura adicional só deve ser criada quando resolver uma necessidade concreta.
