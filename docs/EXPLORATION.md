# IndexCities — EXPLORATION

> **Hub central da exploração — não é fonte de verdade.** Produto: [SPEC](SPEC.md). Estrutura: [ARCHITECTURE](ARCHITECTURE.md). Trabalho: [AGENTS](../AGENTS.md).
>
> **Revisão humana agregada: PARCIALMENTE REVISADO.** O escopo V1 e as decisões expressas já registradas nas fontes canônicas continuam válidos. A organização e a síntese desta revisão são da IA e permanecem **PENDENTES** de revisão humana; reorganizar não aprova conteúdo.
>
> Conteúdo **PENDENTE** ou a parte não revista de material **PARCIALMENTE REVISADO** é informação para avaliação. Não cria requisito, backlog, tarefa obrigatória, autorização de implementação ou de reutilização de acervos antigos. Mesmo um documento REVISADO não substitui a SPEC/ARCHITECTURE.

## Foco atual: primeira versão jogável

A [V1 aprovada](SPEC.md#v1--primeira-versão-jogável-integrada-escopo-aprovado-em-2026-10-12) é uma entrega integrada do **Convencional**: fundar, construir, operar o catálogo reduzido, acompanhar fluxos reais e salvar/retomar a cidade. A visão completa permanece na SPEC; sistemas futuros não entram automaticamente por aparecerem numa pesquisa. A primeira partida do responsável expôs dificuldades de uso; a V1 continua em validação de jogabilidade. O relato e a análise pendente estão em [Interface e ferramentas](exploration/player-interface.md#primeira-partida-da-v1--2026-10-12).

**Avaliação humana mais recente (2026-10-14):** o [relato detalhado da partida V1](exploration/v1-human-playtest-2026-10-13.md#nova-partida-e-avaliação-crítica-do-relatório-processado--2026-10-14) agora inclui o novo feedback processado, a comparação inicial com o protótipo local e verificações em cópias dos saves. Ruas continuam sendo a maior insatisfação relatada. Observações, causas verificadas e propostas sem aprovação estão separadas. A organização por IA segue **PENDENTE de revisão humana**, sem criar requisitos ou autorizar novas regras.

**Construtor de ruas em fechamento isolado (2026-10-15):** o responsável aceitou a experiência e autorizou PR após corrigir o traçado estreito/sobreposto da última captura. A [versão local e a validação](exploration/player-interface.md#rua-e-prefeitura-disponíveis-desde-o-início--implementação-para-avaliação-2026-10-15) incluem bordas dos prédios alinhadas, nó da rua no centro da célula e ocupação antes do primeiro clique, preservando medidas e largura de 10,5 m. A referência azul foi retirada. A [consulta crítica ao Claude](exploration/player-interface.md#consulta-crítica-ao-claude--2026-10-15) informou a comparação; alargar para 12 m continua suspenso. O comportamento aprovado está na SPEC; parecer, síntese e resultados de teste seguem **PENDENTES de revisão humana**. Fechamento técnico concluído: 204 testes aprovados e revisão Haiku 5.5 com achados corrigidos/reconferidos, após DeepSeek indisponível. Os limites de desempenho e as evidências estão no relato temático.

**Lago irregular em integração (2026-10-14):** mapas novos e compatibilidade dos antigos já têm implementação e cenários Godot; duas rodadas de revisão independente concluídas, avaliação estética humana em aberto. Evidências e limites em [Mundo, mapa e ambiente](exploration/world-map-environment.md#integração-do-lago-irregular--2026-10-14), com a síntese da IA **PENDENTE de revisão humana**.

**Início com Rua e Prefeitura — implementado e avaliado:** as duas ferramentas estão disponíveis desde o início, em qualquer ordem; os demais prédios e a gestão abrem ao confirmar o projeto da Prefeitura. O responsável aceitou a experiência para fechamento técnico. A [versão local e seus limites](exploration/player-interface.md#rua-e-prefeitura-disponíveis-desde-o-início--implementação-para-avaliação-2026-10-15) estão registrados, com síntese **PENDENTE de revisão humana**. A regra canônica está na SPEC.

**Divisão de trabalho definida pelo responsável em 2026-10-12:** Codex coordena e cuida de documentação, jogo, integração e validação. Claude Opus 5.5 com ultracode produz somente assets pedidos por Codex. A regra operacional está no [AGENTS](../AGENTS.md#coordenação-da-v1-e-frente-de-assets--2026-10-12); o [briefing visual](exploration/first-playable-assets.md) organiza famílias e limites, sem autorizar produção indiscriminada do inventário.

**Piloto paralelo de 2026-10-14:** Claude produziu os indicadores reais nos prédios enquanto Codex corrigia as rotas; Haiku concluiu a revisão independente. Os seis casos de pessoas paradas melhoraram, mas a revisão confirmou travessias fora das faixas em segmentos intermediários. Por repasse autorizado no [AGENTS](../AGENTS.md), o Claude Opus corrigiu essa falha (F1): as rotas pedonais agora atravessam a via só nas faixas reais em todo o percurso. A revisão independente do DeepSeek V4.1 Flash não encontrou regressão. A experiência ainda depende de avaliação humana. Resultados e limites no [relato V1](exploration/v1-human-playtest-2026-10-13.md#pedestres-e-leitura-da-presença--entrega-9-2026-10-14); a análise da IA segue pendente de revisão humana.

| Tema de entrada | Revisão humana do documento | Escopo / estado |
| --- | --- | --- |
| [Recorte e dependências V1](exploration/first-playable-scope.md) | PARCIALMENTE REVISADO | Recorte aprovado na SPEC; sequência e cenários técnicos são apoio exploratório. Investigação e continuidade da validação de jogabilidade, trânsito e partidas prolongadas em 2026-10-13 (PENDENTES). |
| [Briefing de assets V1](exploration/first-playable-assets.md) | PARCIALMENTE REVISADO | Pacote produzido e integrado à V1; estética e propostas visuais permanecem pendentes de revisão humana. |
| [Lacunas de assets da V1](exploration/asset-gaps-v1.md) | PENDENTE | Cruzamento da SPEC V1 com os assets existentes: 0 P0, 5 P1, 7 P2 e ambiguidades para decisão humana. |
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
| [Mundo, mapa e ambiente](exploration/world-map-environment.md) | PARCIALMENTE REVISADO | Lago irregular integrado em mapas novos, com cenários Godot e compatibilidade de mapas antigos; revisão independente concluída e estética humana pendente. |
| [Construção, materiais e logística](exploration/construction-materials-logistics.md) | PARCIALMENTE REVISADO | Obras, reservas, importação/exportação, estoques, Pátio e transporte. |
| [Famílias, moradia e patrimônio](exploration/households-housing.md) | PARCIALMENTE REVISADO | Aquisição, renda/consumo, migração, contratos e sistemas familiares futuros. |
| [Empresas e mercados](exploration/companies-markets.md) | PARCIALMENTE REVISADO | Autonomia, propriedade/operação, emprego, produção, caixa e falência. |
| [Serviços e infraestrutura](exploration/city-systems.md) | PARCIALMENTE REVISADO | Mobilidade, água/esgoto/energia e histórico de serviços de versões futuras. |
| [Prefeitura](exploration/city-hall.md) | PARCIALMENTE REVISADO | Impostos e crédito da V1 fechados na SPEC; administração, participação e políticas futuras em exploração. |
| [Interface e ferramentas](exploration/player-interface.md) | PARCIALMENTE REVISADO | Primeira partida com dificuldades de uso; construção visual, inspeção/diagnóstico e controles em avaliação. |
| [Qualidade ambiental local](exploration/local-environment-wellbeing.md) | PARCIALMENTE REVISADO | Direção humana registrada na SPEC; fórmulas e medições ainda exploratórias. |
| [Escala e baselines](exploration/simulation-scale.md) | PARCIALMENTE REVISADO | Calendário, aceleração real e referências de calibração/desempenho. |
| [Ritmo e velocidades de simulação](exploration/ritmo-velocidades.md) | PENDENTE | Benchmarks da PR #19, pesquisa sobre gerações com calendário real e medição de escala e censo de movimento (2026-10-16: passo fixo inviável para a meta; custo dominante é recalcular agentes parados; experimento de agenda para sistemas estacionários proposto, não aprovado). |
| [Motor de simulação](exploration/simulation-engine-performance-research.md) | PENDENTE | Pesquisa de CPU/GPU, eventos e alternativas de cálculo. |
| [Técnicas de outros domínios](exploration/simulation-engine-cross-domain-research.md) | PENDENTE | Investigação transversal de aceleração causal. |
| [Hipóteses radicais do motor](exploration/simulation-engine-radical-hypotheses.md) | PENDENTE | Alternativas especulativas; nenhuma decisão de arquitetura. |
| [Cache da simulação](exploration/simulation-cache-feasibility.md) | PENDENTE | Viabilidade, equivalência causal, invalidação e custo líquido a medir. |
| [Testes, diagnóstico e reprodução](exploration/testing-debugging-reproducibility.md) | PENDENTE | Invariantes, reprodução, cobertura semântica e cenários adversariais. |
| [Benchmark do gênero](exploration/genre-benchmark.md) | PENDENTE | Referência comparativa recorrente de jogos e sistemas urbanos reais. |
| [IA para decisões e desenvolvimento](exploration/ai-decision-systems.md) | PENDENTE | Pesquisa de alternativas; não adota dependências nem uma IA no núcleo. |
| [Processo e ferramentas](exploration/development-process.md) | PARCIALMENTE REVISADO | Histórico/raciocínio de workflow; regras atuais pertencem ao AGENTS. |
| [Lições de experiência anterior](exploration/lessons-from-earlier-experiment.md) | PENDENTE | Nota autossuficiente; sem permissão para consultar ou reutilizar a origem. |
| [Alertas de problemas nos prédios (playtest 2026-10-15)](exploration/problem-alerts-playtest-2026-10-15.md) | PENDENTE | Implementação e evidências da feature aprovada na SPEC; feedback de jogabilidade e limites para avaliação humana. |

## Como usar e manter este hub

Leia a SPEC e a ARCHITECTURE pertinentes ao alvo, localize o tema acima e abra somente a exploração necessária. Para definição/priorização de produto, consulte também o mapa de prontidão, sem reabrir decisões canônicas por uma pendência histórica.

Pesquisa extensa, alternativas, evidências e raciocínio pertencem ao documento do domínio. O hub mantém links, estados e questões transversais curtas. Os resumos cronológicos de rodadas foram retirados deste índice; regras vigentes continuam na SPEC e o contexto temático permanece nos documentos vinculados.

## Regra de autoridade

Produto aprovado → SPEC. Estrutura aprovada → ARCHITECTURE. Regra de trabalho → AGENTS. Pesquisa e propostas → exploração, com estado visível de revisão humana. A coordenação operacional não amplia o escopo de produto.
