# IndexCities

IndexCities é um projeto novo em fase inicial de definição.

A intenção é começar com uma folha em branco: pesquisar, discutir e decidir o produto progressivamente, sem herdar automaticamente arquitetura, requisitos ou decisões de trabalhos anteriores.

## Documentos principais

- [`docs/SPEC.md`](docs/SPEC.md) — **o que já foi decidido para o produto**.
- [`docs/EXPLORATION.md`](docs/EXPLORATION.md) — **pesquisas, ideias, alternativas e decisões ainda em formação**.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — **decisões estruturais de software**.
- [`AGENTS.md`](AGENTS.md) — **como humanos e IAs devem trabalhar e como esses documentos se relacionam**.

A SPEC é o **contrato vivo do jogo**: toda implementação de comportamento deve estar coberta por decisão aprovada ali **antes do código**, desde o início do projeto e em todas as evoluções futuras. O mesmo vale para mudanças e bugs que revelem regras ausentes: a lacuna é discutida com o responsável e registrada na SPEC antes de implementar. Bugs que apenas restauram regras já especificadas e refatorações que preservam o comportamento usam a SPEC existente, sem reescrevê-la por ritual. Decisões estruturais de software vão para a ARCHITECTURE. Pesquisa e discussão ficam na EXPLORATION até a decisão ser fechada.

O desenvolvimento segue **Spec-Driven Development (SDD) contínuo**: primeiro são definidas as regras do produto na SPEC, depois o jogo é implementado. Agentes não podem preencher lacunas de produto com suposições; devem sinalizar a dúvida e obter a decisão antes de codificar a parte indefinida. A **primeira entrega e sua validação principal serão integradas**, não uma série de POCs isoladas por sistema. A implementação pode evoluir modularmente e incluir verificações técnicas pontuais.

A IA tem **autonomia para decisões técnicas internas** compatíveis com a SPEC e a ARCHITECTURE — não é necessário especificar cada classe, algoritmo ou detalhe de código. Ela não pode decidir sozinha novas regras, exceções ou consequências do jogo. O processo é propositalmente leve: estrutura adicional só deve ser criada quando resolver uma necessidade concreta.
