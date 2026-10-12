# IndexCities

IndexCities é um projeto em preparação para o desenvolvimento de seu primeiro jogo jogável, a partir de uma SPEC de produto já amplamente definida.

A intenção é começar com uma folha em branco: pesquisar, discutir e decidir o produto progressivamente, sem herdar automaticamente arquitetura, requisitos ou decisões de trabalhos anteriores.

## Documentos principais

- [`docs/SPEC.md`](docs/SPEC.md) — **o que já foi decidido para o produto**.
- [`docs/EXPLORATION.md`](docs/EXPLORATION.md) — **pesquisas, ideias, alternativas e decisões ainda em formação**.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — **decisões estruturais de software**.
- [`AGENTS.md`](AGENTS.md) — **como humanos e IAs devem trabalhar e como esses documentos se relacionam**.

A SPEC é o **contrato vivo do jogo**: todo comportamento precisa estar ali documentado **antes do código**, seja como decisão aprovada pelo responsável, seja como **pequena decisão local e reversível tomada pela IA sob delegação expressa**, identificada como pendente de revisão humana. Regras importantes, exceções ou mudanças em comportamentos aprovados continuam exigindo decisão humana. Bugs que restauram regras claras e refatorações que preservam o contrato não precisam reescrever a SPEC. Decisões estruturais relevantes vão para a ARCHITECTURE; pesquisas e propostas não aprovadas ficam na EXPLORATION.

O desenvolvimento segue **Spec-Driven Development (SDD) contínuo** e será feito **em versões menores, jogáveis e integradas**, com testes de gameplay e integração desde cedo. A visão completa aprovada permanece na SPEC, mas não precisa ser entregue toda de uma vez. **O recorte exato da primeira versão ainda precisa de aprovação do responsável**, depois de mapear dependências dos sistemas. Não impor POCs isoladas como fases obrigatórias nem usar simulações fictícias de dinheiro, materiais ou SIMs para fingir funcionamento.

A IA tem **autonomia técnica interna** dentro da arquitetura e autonomia estritamente limitada para pequenos detalhes de produto, desde que **todas as decisões de produto tomadas por IA sejam registradas previamente na SPEC para revisão humana**. Ela não pode decidir sozinha regras importantes, exceções ou consequências sistêmicas. O processo permanece leve, sem microespecificar classes e algoritmos.
