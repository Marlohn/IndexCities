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

- **Mapa de prontidão, lacunas e prioridades de pesquisa** — revisão humana: **PENDENTE**; triagem transversal com P0/P1/P2, riscos e questões consolidadas da pesquisa externa de 2026-10-08, **atualizada quanto às decisões 1A/2A/3A de 2026-10-09**, sem recalcular percentuais heurísticos. Não é fonte de verdade nem altera a SPEC: [`exploration/project-readiness.md`](exploration/project-readiness.md).

## Explorações temáticas

- **Direção de produto e princípios de simulação** — revisão humana: **PARCIALMENTE REVISADO**; ativa; identidade, loop, profundidade, **impactos globais sobre SIMs**, **sandbox, desafios emergentes de problemas/oportunidades 5C, reconhecimento não econômico 2A, interface 3C e conclusão automática por melhoria sustentada 5A aprovados na SPEC; condições específicas ainda em pesquisa**, **gameplay e microgerenciamento como critério obrigatório de todo sistema**, legibilidade: [`exploration/product-direction.md`](exploration/product-direction.md).
- **Escala, performance e baselines** — revisão humana: **PARCIALMENTE REVISADO**; ativa. **DECISÃO na SPEC (2026-10-08): tempo acelera de verdade, com todos os acontecimentos físicos, humanos e econômicos efetivamente simulados**, sem saltar datas ou inventar operações. **ABERTO:** duração do calendário, aceleração máxima e estratégia concreta de desempenho; pesquisa técnica nova **PENDENTE** sobre eventos, dependências, dados compactos, rotas e paralelismo. A–H são histórico de alternativas, sem aprovar replay, previsão ou mudança automática de velocidade. Pause/1/2/3 permanecem aprovados: [`exploration/simulation-scale.md`](exploration/simulation-scale.md).
- **Pesquisa independente: motor de simulação de cidade em altíssima velocidade** — revisão humana: **PENDENTE**; levantamento aprofundado de fontes primárias e acadêmicas, ideias fora da caixa (eventos, intervalos equivalentes, certificados cinemáticos, filas físicas, dados compactos, replanejamento, paralelismo), limitações, experimentos e **conclusão explícita da IA**. **Não altera a SPEC nem aprova nova arquitetura**: [`exploration/simulation-engine-performance-research.md`](exploration/simulation-engine-performance-research.md).
- **Mundo, mapa, terreno e ambiente** — revisão humana: **PARCIALMENTE REVISADO**; ativa; direção visual, seed/reprodutibilidade, terreno, vegetação e **modelo híbrido de grid aprovado na SPEC**, detalhes de posicionamento em calibração: [`exploration/world-map-environment.md`](exploration/world-map-environment.md).
- **Mobilidade, serviços e infraestrutura urbana**; **Água/energia por consumo 1B, tarifa automática 2B e dívida residencial sem corte 3B aprovadas; inadimplência empresarial sem corte 2A e salários municipais automáticos 3A APROVADOS (2026-10-09); detalhes financeiros/calibração ainda em exploração; foco em expansão de estruturas e capacidade conforme demanda; transporte grátis/pago 2C com tarifa automática quando pago e sem embarque sem saldo 4A; possível política de subsídios/cobertura de custo PENDENTE como melhoria futura; emprego único 4A e dois empregos apenas melhoria futura** — revisão humana: **PARCIALMENTE REVISADO**; ativa; vias, transporte, saúde, educação, serviços, água, energia, resíduos, conexão externa e **pendulares nos dois sentidos 4C com identidade/vínculo externos persistentes e exterior simplificado 4A; turismo pela atratividade real 2A; manutenção 5B/priorização essencial 5A; horários comerciais 1A e limite físico de equipe 2B aprovados; parâmetros em pesquisa**: [`exploration/city-systems.md`](exploration/city-systems.md).
- **Famílias, moradia e patrimônio**; **água/energia como despesas por consumo real 1B a preços automáticos 2B, dívida real sem corte residencial individual 3B, sem tarifas extras de esgoto/lixo por inferência; detalhes financeiros a calibrar** — revisão humana: **PARCIALMENTE REVISADO**; ativa; renda, consumo, valor imobiliário, aluguel, propriedade, primeira aquisição, **intervenção indenizada, pagamento antecipado, desocupação assistida e prioridade de primeira oferta da casa substituta ao SIM antigo proprietário aprovados** e migração pioneira: [`exploration/households-housing.md`](exploration/households-housing.md).
- **Empresas, mercados e finanças da cidade**; **preços privados de serviços 3A e troca de ramo da mesma empresa proprietária sem venda 5A aprovados, com custo/adaptação e destino de estoque reais** — revisão humana: **PARCIALMENTE REVISADO**; ativa; **dez negócios iniciais, B2B, autonomia, consumo opcional e reuso de imóvel aprovados; categoria comercial ampla abrange revenda e serviços 1A, SIM empregado pode avaliar oportunidade melhor ocasionalmente 5B, refeição casa/fora 3B, horários 1A, equipe limitada pelo porte 2B, assets reutilizados e salários privados adaptativos 3A; parâmetros exatos PENDENTES**, empresas privadas, demanda, aquisição, **intervenção indenizada, autonomia empresarial, prioridade de compra, reavaliação do emprego (A), continuidade econômica/institucional (C) e recuperação/perda de estoque real na realocação aprovadas; vínculos e salários de empresa privada durante paralisação seguem opção 1A APROVADA (2026-10-09), preservando decisão do SIM; casos de paralisação municipal e logística fina seguem abertos**, bootstrap por oportunidades, caixa, falência, Reserva Global e oferta monetária fixa: [`exploration/companies-markets.md`](exploration/companies-markets.md).
- **Construção, materiais e logística** — revisão humana: **PARCIALMENTE REVISADO**; ativa; materiais físicos, estoques, importação/exportação, obras e **ritmo ágil, paralisação não obrigatória e recuperação/perda física de estoque em realocações confirmados**, Pátio e cadeia logística; tempos e detalhes de liquidação ainda em exploração; **cancelamento monetário decidido na SPEC**: [`exploration/construction-materials-logistics.md`](exploration/construction-materials-logistics.md).
- **Interface e ferramentas do jogador**; **painel prioriza demanda, produção efetiva/capacidade de água/energia, déficit, orçamento e receitas efetivas; tarifas automáticas 2B, opção de transporte grátis/pago 2C e dívida 3B aprovadas; UI exata pendente** — revisão humana: **PARCIALMENTE REVISADO**; ativa; construção, **repetir/pipeta 3B e reuso visual de modelos-base 2B aprovados, orientação inicial/tutorial ADIADOS 4A até gameplay ser melhor definida e validada; número de modelos ainda em pesquisa**, **mover obra inacabada, intervenção indenizada, continuidade condicionada e prévia agregada de risco de perda de estoque na realocação aprovados**, **prévia contextual de valorização/desvalorização (B) aprovada**, realocação assistida de prédio pronto (emprego privado em paralisação 1A aprovado, situação municipal distinta) e **prazo de desocupação residencial** em calibração, primeira oferta preferencial ao antigo dono (SIM/empresa) **aprovada** e **reavaliação global do trabalho (A) aprovada**, snapping e impactos sobre SIMs: [`exploration/player-interface.md`](exploration/player-interface.md).
- **Benchmark de jogos e pesquisa de sistemas urbanos reais** — revisão humana: **PENDENTE**; recorrente; inclui rodada ampliada com **24 frentes de investigação**, devlogs, fóruns, vídeos, IBGE, OCDE, ITDP, EPA, FHWA e estudo urbano de Louveira; evidência e alternativas **não** viram requisitos automaticamente: [`exploration/genre-benchmark.md`](exploration/genre-benchmark.md).
- **IA para decisões, simulação e desenvolvimento** — revisão humana: **PENDENTE**; pesquisa concluída para a fase atual; Jev/Laya não são recomendados no core por performance nem como camada geral do workflow: [`exploration/ai-decision-systems.md`](exploration/ai-decision-systems.md).
- **Processo de desenvolvimento e documentação** — revisão humana: **PARCIALMENTE REVISADO**; decidido para a fase atual; **SPEC viva obrigatória antes e durante todo desenvolvimento (features, bugs e manutenção)**, com lacunas decididas pelo responsável antes do código; primeira entrega/validação globais, sem POCs setoriais obrigatórias. Contém referência externa nova **PENDENTE**. Histórico: [`exploration/development-process.md`](exploration/development-process.md); regras vigentes em `AGENTS.md`.

## Regra de autoridade

Documentos em `docs/exploration/` **não são requisitos por si só**.

Quando uma exploração fecha:
- comportamento/produto decidido → `docs/SPEC.md`;
- decisão estrutural de software → `docs/ARCHITECTURE.md`;
- regra de trabalho/documentação → `AGENTS.md`.

O documento temático pode continuar guardando o **porquê**, inclusive alternativas descartadas e evidências, desde que deixe claro o que está decidido, superado ou ainda aberto.


---
