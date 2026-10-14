# IndexCities — EXPLORATION

> **Hub central da exploração — não é fonte de verdade.** Produto: [SPEC](SPEC.md). Estrutura: [ARCHITECTURE](ARCHITECTURE.md). Trabalho: [AGENTS](../AGENTS.md).
>
> **Revisão humana agregada: PARCIALMENTE REVISADO.** O escopo V1 e as decisões expressas já registradas nas fontes canônicas continuam válidos. A organização e a síntese desta revisão são da IA e permanecem **PENDENTES** de revisão humana; reorganizar não aprova conteúdo.
>
> Conteúdo **PENDENTE** ou a parte não revista de material **PARCIALMENTE REVISADO** é informação para avaliação. Não cria requisito, backlog, tarefa obrigatória, autorização de implementação ou de reutilização de acervos antigos. Mesmo um documento REVISADO não substitui a SPEC/ARCHITECTURE.

## Foco atual: primeira versão jogável

A [V1 aprovada](SPEC.md#v1--primeira-versão-jogável-integrada-escopo-aprovado-em-2026-10-12) é uma entrega integrada do **Convencional**: fundar, construir, operar o catálogo reduzido, acompanhar fluxos reais e salvar/retomar a cidade. A visão completa permanece na SPEC; sistemas futuros não entram automaticamente por aparecerem numa pesquisa. A primeira partida do responsável expôs dificuldades de uso; a V1 continua em validação de jogabilidade. O relato e a análise pendente estão em [Interface e ferramentas](exploration/player-interface.md#primeira-partida-da-v1--2026-10-12).

**Avaliação humana mais recente (2026-10-14):** o [relato detalhado da partida V1](exploration/v1-human-playtest-2026-10-13.md#nova-partida-e-avaliação-crítica-do-relatório-processado--2026-10-14) agora inclui o novo feedback processado, a comparação inicial com o protótipo local e verificações em cópias dos saves. Ruas continuam sendo a maior insatisfação relatada. Observações, causas verificadas e propostas sem aprovação estão separadas. A organização por IA segue **PENDENTE de revisão humana**, sem criar requisitos ou autorizar novas regras.

**Divisão de trabalho definida pelo responsável em 2026-10-12:** Codex coordena e cuida de documentação, jogo, integração e validação. Claude Opus 5.5 com ultracode produz somente assets pedidos por Codex. A regra operacional está no [AGENTS](../AGENTS.md#coordenação-da-v1-e-frente-de-assets--2026-10-12); o [briefing visual](exploration/first-playable-assets.md) organiza famílias e limites, sem autorizar produção indiscriminada do inventário.

| Tema de entrada | Revisão humana do documento | Escopo / estado |
| --- | --- | --- |
| [Recorte e dependências V1](exploration/first-playable-scope.md) | PARCIALMENTE REVISADO | Recorte aprovado na SPEC; sequência e cenários técnicos são apoio exploratório. Investigação e continuidade da validação de jogabilidade, trânsito e partidas prolongadas em 2026-10-13 (PENDENTES). |
| [Briefing de assets V1](exploration/first-playable-assets.md) | PARCIALMENTE REVISADO | Pacote produzido e integrado à V1; estética e propostas visuais permanecem pendentes de revisão humana. |
| [Mapa de prontidão](exploration/project-readiness.md) | PENDENTE | Riscos/lacunas para orientar investigação; percentuais históricos não medem implementação. |
| [Pipeline de assets](exploration/asset-production-pipeline.md) | PENDENTE | Alternativas de autoria, formatos, recursos compartilhados e validação gráfica. |

### Pontos de integração a observar

> **Revisão humana desta seção: PENDENTE.** Síntese da IA para avaliação; não acrescenta regras à V1 nem impõe uma fase de POC.

- Bootstrap: a confirmação da Prefeitura libera ferramentas; materiais, equipe, dinheiro e acesso continuam reais. Conferir o fluxo sem concluir obras ou criar entregas artificialmente.
- Economia/logística: observar conservação monetária, compromissos versus pagamentos, titularidade e transporte efetivo; moradia/atividade disponível não garante migração, emprego ou venda.
- Infraestrutura: conferir captação e tratamento de água, esgoto e variabilidade eólica na SPEC. Reduzir modelos visuais não funde funções físicas nem apaga déficit ou consequências pertinentes.
- **Para revisar com o responsável — vagas de carro no meio-fio:** adiadas porque a rua 1+1 não tem largura para elas; alternativas em [Interface e ferramentas](exploration/player-interface.md#vagas-de-carro-no-meio-fio--pendente-de-revisão).
- Arte/interface: validar amostra na engine, compatibilizar escala/pivôs/acessos e reconhecer construções sem leitura obrigatória. Uma prévia técnica não comprova gameplay.

## Explorações temáticas

Os estados são agregados por documento. Em material parcialmente revisado, consultar também os estados por seção. As decisões vigentes são encontradas nas fontes canônicas; propostas antigas podem estar superadas.

| Domínio | Revisão humana | Escopo / estado |
| --- | --- | --- |
| [Direção de produto](exploration/product-direction.md) | PARCIALMENTE REVISADO | Identidade, causalidade, profundidade, públicos e histórico do Criativo adiado. |
| [Mundo, mapa e ambiente](exploration/world-map-environment.md) | PARCIALMENTE REVISADO | Seed, terreno, vegetação e posicionamento; estudo visual de água/margens concluído em prévia isolada, estética e geometria pendentes. |
| [Construção, materiais e logística](exploration/construction-materials-logistics.md) | PARCIALMENTE REVISADO | Obras, reservas, importação/exportação, estoques, Pátio e transporte. |
| [Famílias, moradia e patrimônio](exploration/households-housing.md) | PARCIALMENTE REVISADO | Aquisição, renda/consumo, migração, contratos e sistemas familiares futuros. |
| [Empresas e mercados](exploration/companies-markets.md) | PARCIALMENTE REVISADO | Autonomia, propriedade/operação, emprego, produção, caixa e falência. |
| [Serviços e infraestrutura](exploration/city-systems.md) | PARCIALMENTE REVISADO | Mobilidade, água/esgoto/energia e histórico de serviços de versões futuras. |
| [Prefeitura](exploration/city-hall.md) | PARCIALMENTE REVISADO | Impostos e crédito da V1 fechados na SPEC; administração, participação e políticas futuras em exploração. |
| [Interface e ferramentas](exploration/player-interface.md) | PARCIALMENTE REVISADO | Primeira partida com dificuldades de uso; construção visual, inspeção/diagnóstico e controles em avaliação. |
| [Qualidade ambiental local](exploration/local-environment-wellbeing.md) | PARCIALMENTE REVISADO | Direção humana registrada na SPEC; fórmulas e medições ainda exploratórias. |
| [Escala e baselines](exploration/simulation-scale.md) | PARCIALMENTE REVISADO | Calendário, aceleração real e referências de calibração/desempenho. |
| [Motor de simulação](exploration/simulation-engine-performance-research.md) | PENDENTE | Pesquisa de CPU/GPU, eventos e alternativas de cálculo. |
| [Técnicas de outros domínios](exploration/simulation-engine-cross-domain-research.md) | PENDENTE | Investigação transversal de aceleração causal. |
| [Hipóteses radicais do motor](exploration/simulation-engine-radical-hypotheses.md) | PENDENTE | Alternativas especulativas; nenhuma decisão de arquitetura. |
| [Cache da simulação](exploration/simulation-cache-feasibility.md) | PENDENTE | Viabilidade, equivalência causal, invalidação e custo líquido a medir. |
| [Testes, diagnóstico e reprodução](exploration/testing-debugging-reproducibility.md) | PENDENTE | Invariantes, reprodução, cobertura semântica e cenários adversariais. |
| [Benchmark do gênero](exploration/genre-benchmark.md) | PENDENTE | Referência comparativa recorrente de jogos e sistemas urbanos reais. |
| [IA para decisões e desenvolvimento](exploration/ai-decision-systems.md) | PENDENTE | Pesquisa de alternativas; não adota dependências nem uma IA no núcleo. |
| [Processo e ferramentas](exploration/development-process.md) | PARCIALMENTE REVISADO | Histórico/raciocínio de workflow; regras atuais pertencem ao AGENTS. |
| [Lições de experiência anterior](exploration/lessons-from-earlier-experiment.md) | PENDENTE | Nota autossuficiente; sem permissão para consultar ou reutilizar a origem. |

## Como usar e manter este hub

Leia a SPEC e a ARCHITECTURE pertinentes ao alvo, localize o tema acima e abra somente a exploração necessária. Para definição/priorização de produto, consulte também o mapa de prontidão, sem reabrir decisões canônicas por uma pendência histórica.

Pesquisa extensa, alternativas, evidências e raciocínio pertencem ao documento do domínio. O hub mantém links, estados e questões transversais curtas. Os resumos cronológicos de rodadas foram retirados deste índice; regras vigentes continuam na SPEC e o contexto temático permanece nos documentos vinculados.

## Regra de autoridade

Produto aprovado → SPEC. Estrutura aprovada → ARCHITECTURE. Regra de trabalho → AGENTS. Pesquisa e propostas → exploração, com estado visível de revisão humana. A coordenação operacional não amplia o escopo de produto.
