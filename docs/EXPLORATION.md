# IndexCities — EXPLORATION

> Este arquivo é o **hub central da exploração**.
>
> Ele não é fonte de verdade do produto. Decisões oficiais de produto pertencem a [SPEC.md](SPEC.md), decisões estruturais a [ARCHITECTURE.md](ARCHITECTURE.md) e regras de trabalho a [../AGENTS.md](../AGENTS.md).

## Como usar

Use este arquivo para localizar rapidamente o pensamento ainda não oficial do projeto.

- Pesquisa curta e transversal pode ficar temporariamente aqui.
- Quando um tema ganhar volume, alternativas, referências ou trabalho paralelo, mova-o para `docs/exploration/<tema>.md`.
- Documentos temáticos preservam pesquisa, trade-offs, alternativas superadas e questões abertas.
- Uma conclusão em exploração só vira requisito quando for promovida para a fonte canônica apropriada.
- Não criar documentos por ritual; os arquivos abaixo existem porque já há volume suficiente para justificar a separação.

## Status de revisão humana

O status abaixo é agregado por documento. `PARCIALMENTE REVISADO` significa que o arquivo mistura material discutido/confirmado com conteúdo da IA ainda não revisado integralmente. Seções individuais podem ter status mais restritivo no próprio arquivo.

Auditoria reconciliada em **2026-10-07** com o histórico de conversas disponível. A classificação é conservadora: conteúdo discutido pode ser parcial, mas pesquisa/síntese da IA que não foi lida integralmente continua pendente.

## Mapa transversal de prontidão e prioridades

- **Prontidão para a primeira validação integrada/POC e decisões pendentes** — revisão humana: **PENDENTE**; análise de 2026-10-08; percentuais qualitativos, prioridades P0/P1/P2, riscos e inconsistências para orientar as próximas rodadas de definição. Não é fonte de verdade nem altera a SPEC: [`exploration/project-readiness.md`](exploration/project-readiness.md).

## Explorações temáticas

- **Direção de produto e princípios de simulação** — revisão humana: **PARCIALMENTE REVISADO**; ativa; identidade, loop, profundidade, legibilidade e microgerenciamento: [`exploration/product-direction.md`](exploration/product-direction.md).
- **Escala, performance e baselines** — revisão humana: **PARCIALMENTE REVISADO**; ativa; população, profundidade individual, tempo e referências empíricas para benchmark: [`exploration/simulation-scale.md`](exploration/simulation-scale.md).
- **Mundo, mapa, terreno e ambiente** — revisão humana: **PARCIALMENTE REVISADO**; ativa; direção visual, seed/reprodutibilidade, terreno, vegetação, clima e grid: [`exploration/world-map-environment.md`](exploration/world-map-environment.md).
- **Mobilidade, serviços e infraestrutura urbana** — revisão humana: **PARCIALMENTE REVISADO**; ativa; vias, transporte, saúde, educação, serviços, água, energia, resíduos e conexão externa: [`exploration/city-systems.md`](exploration/city-systems.md).
- **Famílias, moradia e patrimônio** — revisão humana: **PARCIALMENTE REVISADO**; ativa; renda, consumo, valor imobiliário, aluguel, propriedade, primeira aquisição e patrimônio residencial: [`exploration/households-housing.md`](exploration/households-housing.md).
- **Empresas, mercados e finanças da cidade** — revisão humana: **PARCIALMENTE REVISADO**; ativa; empresas privadas, demanda, aquisição, caixa, falência, Reserva Global e oferta monetária fixa: [`exploration/companies-markets.md`](exploration/companies-markets.md).
- **Construção, materiais e logística** — revisão humana: **PARCIALMENTE REVISADO**; ativa; materiais físicos, estoques, importação/exportação, obras, Pátio e cadeia logística: [`exploration/construction-materials-logistics.md`](exploration/construction-materials-logistics.md).
- **Interface e ferramentas do jogador** — revisão humana: **PARCIALMENTE REVISADO**; ativa; construção, ferramentas, snapping, overlays, realocação e fluxo de interação: [`exploration/player-interface.md`](exploration/player-interface.md).
- **Referência comparativa do gênero** — revisão humana: **PENDENTE**; recorrente; benchmark externo para confrontar escolhas do IndexCities com outros city builders: [`exploration/genre-benchmark.md`](exploration/genre-benchmark.md).
- **IA para decisões, simulação e desenvolvimento** — revisão humana: **PENDENTE**; pesquisa concluída para a fase atual; Jev/Laya não são recomendados no core por performance nem como camada geral do workflow: [`exploration/ai-decision-systems.md`](exploration/ai-decision-systems.md).
- **Processo de desenvolvimento e documentação** — revisão humana: **PARCIALMENTE REVISADO**; decidido para a fase atual; **SDD antes do código e primeira entrega/validação globais do jogo integrado, sem POCs setoriais obrigatórias**. Histórico: [`exploration/development-process.md`](exploration/development-process.md); regras vigentes em `AGENTS.md`.

## Regra de autoridade

Documentos em `docs/exploration/` **não são requisitos por si só**.

Quando uma exploração fecha:
- comportamento/produto decidido → `docs/SPEC.md`;
- decisão estrutural de software → `docs/ARCHITECTURE.md`;
- regra de trabalho/documentação → `AGENTS.md`.

O documento temático pode continuar guardando o **porquê**, inclusive alternativas descartadas e evidências, desde que deixe claro o que está decidido, superado ou ainda aberto.


---
