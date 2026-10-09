# Pesquisa de fronteira — ideias radicalmente diferentes para o motor da cidade

> **Revisão humana: PENDENTE.** Estudo especulativo da IA em **2026-10-09**, solicitado pelo responsável. Pesquisa, NÃO requisito de produto, decisão estrutural nem autorização para implementar.
>
> **Escopo:** matemática, química, compiladores, circuitos, robótica, algoritmos incrementais, computação paralela e ideias originais. Complementa [pesquisa principal](simulation-engine-performance-research.md) e [pesquisa transversal](simulation-engine-cross-domain-research.md), sem transformá-las em requisitos. Decisões oficiais continuam em [SPEC](../SPEC.md) e [ARCHITECTURE](../ARCHITECTURE.md); regras de documentação em [AGENTS](../../AGENTS.md).

## 1. Problema real: encontrar uma maneira diferente de pensar

O requisito do IndexCities não é simplesmente desenhar 100 mil pessoas nem aumentar FPS. É **executar de verdade uma cidade**, mantendo todo SIM, empresa, veículo e acontecimento individual — movimento e congestionamento fora da câmera, consumo presencial, transações de dinheiro reais, produtos e caminhões reais, jornadas e contas, produção, aluguel, dívida, envelhecimento. Aceleração em pause/1/2/3 não autoriza avanço falso do calendário. A SPEC permite processamento mais eficiente **somente se estado, identidade, interações, causalidade e efeitos equivalentes forem preservados**.

Em motor tradicional, cada entidade executa um update a cada fração de segundo. Com eventos, somente entidades com acontecimentos importantes acordam. Esta rodada pergunta algo mais ambicioso:

**E se o motor representasse a cidade como uma rede de acontecimentos e condições, capaz de produzir um plano TEMPORÁRIO do que deve acontecer, conhecer exatamente o prazo de validade do plano e invalidá-lo quando algo mudar?**

É um conceito de **“programa causal temporário”**. A expressão é nossa: NÃO existe nestas fontes uma engine pronta que implemente toda a proposta para uma cidade semelhante ao IndexCities. Não confundir contrato temporário com prever o destino e inventá-lo. Acontecimento real continua exigindo suas condições efetivas e precisa preservar sequência e participantes.

Três custos a atacar separadamente: (a) perguntar repetidamente “algo mudou?”; (b) descobrir repetidamente “quem pode interagir?”; (c) recalcular intervalos matemáticos estáveis em passos microscópicos desnecessários. A verdadeira carga inevitável é a quantidade de interações reais; nunca assumir que todas podem ser compactadas.

## 2. Evidência nova de áreas muito distantes dos jogos

| Referência | Descoberta sustentada pela fonte | Ideia para IndexCities | Maior limite |
| --- | --- | --- | --- |
| **Química: Next Reaction Method**, Gibson & Bruck [R01,R02] | Uma fila temporal e um grafo de dependências permitem reagendar só as reações afetadas após cada evento; o método é exato para a classe de modelo estocástico definida. | Processar vaga, estoque, fábrica e rotina quando uma condição muda, sem varrer a população. | “Exato para reações químicas” NÃO significa exato para regras econômicas ou escolha humana. |
| **Robótica: self-triggered control** [R03,R04] | O controle calcula agora **quando será necessária a próxima avaliação**, em lugar de consultar periodicamente o estado. | Caminhão/produção ou serviço define próximo limite e assina invalidadores. | Garantias dependem do modelo físico e da perturbação; motoristas podem reagir imprevisivelmente. |
| **Stanford: compilador de simulação de circuitos** [R05,R06] | Trabalhos em Verilog/Esterel reduzem overhead de agenda gerando código especializado e, em casos adequados, evitam uma fila central dinâmica. | Pré-planejar cadeias estáveis de acontecimentos, sem dispatch de objeto/evento constante. | Topologia de um circuito é mais estável que emprego, comércio, ruas e agentes autônomos. |
| **Salsa: compilador incremental** [R07,R08] | Consulta memoriza campos realmente lidos, resultados e versões; reaproveita cálculos quando dependências não mudam. | Reutilizar cálculo de acessibilidade, fornecedores, emprego e oportunidades. | Não transformar resultado derivado em compra/emprego fictício; grafo pode explodir. |
| **QSS Solver: ciência matemática** [R09,R10] | Compila fórmulas de sistemas híbridos em C e usa eventos/limiares, com discretização por estado em vez de apenas tempo. | Estudar cálculo até estoque acabar, produção concluir, capacidade mudar. | QSS usa QUANTIZAÇÃO, que é aproximação; nunca assumir equivalência física automática. |
| **Ptolemy II: tempo superdenso** [R11] | Distingue instante do modelo e microetapas para ordenar acontecimentos de tempo igual. | Resolver dependências simultâneas sem depender da ordem acidental das threads. | Não determina prioridade econômica se a SPEC não decidiu. Não são dois calendários. |
| **Random123: RNG por chave e contador** [R12] | Sorteio pode ser calculado independentemente da ordem de avaliação e sem estado de RNG global. | Reproduzir escolhas aleatórias legítimas de cada SIM/empresa após otimizações/paralelismo. | Não autoriza criar aleatoriedade nova nem substituir decisões já estabelecidas. |
| **Dinâmica molecular EDMD** [R13] | Implementação mantém índices de vizinhos e pode agendar só o próximo evento por partícula. | Minimizaria fila e memória por veículo em movimento previsível. | Trânsito tem mudança de faixa, prioridade, pedestre e motoristas, diferentemente de esferas rígidas. |
| **ROSS: paralelismo de eventos** [R14] | Lookahead conservador pode antecipar execução sem violar causalidade; Time Warp otimista pode produzir avalanche de rollback. | Regiões independentes avançam até primeiro instante possível de influência externa. | Mercado, rede elétrica ou ordem do jogador podem gerar influência imediata global: janela útil pode ser zero. |
| **Alive2 / dReach** [R15,R16] | Ferramentas de verificação checam equivalência de certos trechos ou alcançabilidade de sistemas híbridos sob limites específicos. | Verificar matematicamente otimizações locais e encontrar contraexemplos. | NÃO provam o jogo inteiro C#/Godot; limites importantes de linguagem, modelo e precisão. |
| **Ptolemy: scheduler estático** [R17] | Agenda pode ser calculada e reutilizada enquanto a estrutura permanece válida. | Compilar a ordem de operações de uma rotina, recompilando só após alteração relevante. | Recompilar frequentemente é caro; decisão de SIM pode invalidar rotina. |
| **Mob City: relato do desenvolvedor** [R18] | Atualizar NPCs menos vezes fora da câmera gerou diferenças perceptíveis de tempo/movimento em comparação à mesma ordem observada. | Caso de rejeição: câmera não pode determinar a verdade física do IndexCities. | Ganho posterior em FPS/instanciamento não equivale a capacidade global de simular economia. |
| **Omnith: outro jogo C#** [R19] | Criador relata 10 mil agentes GOAP em laptop com 60 FPS e usa profiling/partes quentes otimizadas. | Confirma que organização dos dados e custos quentes podem importar mais que troca total de engine. | Relato próprio, carga diferente, não benchmark de city builder completo. |
| **Model checking simbólico** [R20] | Representações simbólicas compactam alguns conjuntos enormes de estados possíveis. | Talvez provar propriedades de trechos de estado estável ou regras financeiras simples. | Explosão combinatória e variáveis reais: não é simulação individual de um mundo mutável. |
| **FPGA de eventos** [R21] | Pesquisa propõe hardware dedicado ao processamento paralelo de eventos. | Inspiração para reduzir agendamento/código genérico. | Hardware de usuário e complexidade inviabilizam uso como motor principal. |

**Observação sobre evidência:** as propostas para IndexCities nas colunas 3 são minhas extrapolações, NÃO resultados obtidos pelos autores. Nem o código do IndexCities foi executado nesta rodada, nem os benchmarks externos foram reproduzidos.

## 3. Dezessete ideias realmente fora da caixa

Legenda: **A** investigar como solução local de baixo risco; **B** explorar se o perfil mostrar gargalo; **C** experimental arriscada; **D** não recomendar. São prioridades de INVESTIGAÇÃO, nunca autorização de arquitetura.

### H01 — “Cidade como rede de reações” [A]

Adaptar **o princípio**, não a matemática probabilística, do método Gibson–Bruck. Uma reação urbana é a mudança real de um estado: entregou material à doca; alguém comprou uma unidade; trabalhador iniciou ou terminou jornada; salário transferiu de uma carteira real para outra. Guardar interessados e condições de cada operação. Um acontecimento acorda apenas as próximas operações realmente afetadas.

**Potencial:** custo aproxima-se de acontecimentos e dependentes, em vez de todos os SIMs.
**Falha:** relações entre empresa/mercado/cidadãos podem ser tão amplas que índices custam muita memória. Não converter compra presencial em reação abstrata.

### H02 — “Contrato temporal verificável por objeto” [A/B]

Cada processo pode registrar: ID, estado atual, trajetória ou evolução permitida, instante limite, condições de validade e dependências externas. Exemplo: uma máquina continuará operando até faltar insumo ou encerrar turno; uma trajetória só vale enquanto distância, aceleração, faixa, semáforo e conflitos permitirem.

**Potencial:** guardar a evolução real e calcular diretamente posições/estados válidos sem milhares de verificações idênticas.
**Falha:** mudança de comportamento, bloqueio, acidente e entrada de novo vizinho exigem invalidação imediata. Em tráfego pesado os limites podem ser curtíssimos. Não é “chegar automaticamente”.

### H03 — “Compilar temporariamente o bairro” [B/C]

Inspirado em compiladores de circuitos e Ptolemy: criar planos compactos de **ordem de avaliação** para cadeias estáveis (turnos, vencimentos, produção com insumos). Um plano é reaproveitado enquanto suas dependências estão inalteradas. Não precisa de compilador C# runtime: um vetor compacto de tarefas pode simular a ideia.

**Potencial:** remover parte do custo da fila e de chamadas indiretas.
**Falha:** recompilar após cada alteração de estoque, emprego, energia e trânsito pode sair mais caro; grandes planos não são independentes das decisões dos SIMs.

### H04 — “Motor inverso: recursos chamam agentes” [A/B]

Em vez de cada pessoa verificar toda loja, fábrica, vaga e residência, o recurso/serviço sinaliza quando sua oportunidade muda. Índices permitem que somente SIMs interessados avaliem conforme a regra e autonomia própria.

**Potencial:** reduzir milhares de buscas por entidade.
**Falha:** necessidade do SIM também muda sem recurso emitir sinal; indexar o universo completo de preferências pode ser impossível. SIM continua decidindo por si mesmo e concorrendo de verdade.

### H05 — “Quem mantém a ordem do tempo?” [A]

Um único tempo autoritativo, com microetapas técnicas para eventos no mesmo instante quando houver dependência. Calcular em lote pode ser paralelo, **confirmar** estoque/saldo/vaga em ordem coerente. Desvincular de qual thread/câmera processou primeiro.

**Falha:** critério substantivo de prioridade entre dois interessados não pode ser inventado apenas porque uma ferramenta exige desempate.

### H06 — “Veículos self-triggered” [A/B]

Cada carro calcula seu **próximo instante de possível interação** de acordo com o modelo físico (não apenas a próxima posição). Se nenhum conflito puder acontecer antes, não recalcular todos os passos. Ao receber perturbador, invalida e reavalia.

**Potencial:** grandes trechos livres muito baratos.
**Falha:** limiar precisa considerar frenagem, faixa, ônibus, pedestre, acidente, rotatória, cruzamento e entrada inesperada. Sem prova, voltar a micropassos; técnica física variável não pode virar nível de simulação simplificado.

### H07 — “Tráfego como grafo cinético” [B/C]

Veículos nas faixas têm relações dinâmicas com líder, seguidor, cruzamento e zona de conflito. Atualizar somente arestas que mudam. Aproveitar certificados cinéticos e índices de vizinhos pesquisados nas rodadas anteriores.

**Falha:** redes urbanas dinâmicas geram custos de manutenção, reordenação e eventos falsos. Não confundir grafo de vizinhança com resolver dinâmica completa.

### H08 — “Janelas de paralelismo causal” [B/C]

Executar bairros ou domínios em paralelo somente até instante em que podem receber influência externa, como fazem simulações distribuídas conservadoras. Usar custo real do caminho de informação: caminhão leva tempo físico, mas transação/energia pode mudar instantaneamente.

**Falha:** janela nula ou pequena demais; sincronização pior que execução sequencial.

### H09 — “Aleatoriedade amarrada a cada fato” [A]

Para decisões que já admitem sorteio, derivar números pseudoaleatórios de ID do SIM, tipo e sequência local de acontecimento. Comparar algoritmos/paralelismo sem que mudanças irrelevantes na ordem de CPU alterem todas as decisões futuras.

**Falha:** não resolverá conflitos causais; mudar gerador altera comportamento se havia algoritmo prévio.

### H10 — “Reaproveitar matemática de ciclos sem inventar acontecimentos” [B]

Quando comportamento matemático é estável e todos os limites relevantes são conhecidos, usar composição de operações ou integração direta. Exemplo: estoque diminui a taxa constante **somente até** primeiro evento de falta, chegada, mudança de taxa ou necessidade de confirmação individual.

**Falha:** pagamentos são transações reais entre SIMs/empresas; não concentrar várias remunerações e receitas ficticiamente no último instante. Um evento intermediário que altera a capacidade de compra deve ser processado.

### H11 — “Simulação parcialmente simbólica” [C]

Representar apenas fórmulas e dependências de alguns **estados derivados**, não agentes abstratos. Exemplo: disponibilidade real ou custo de deslocamento derivado podem ser expressões recomputadas sob demanda.

**Falha:** transformar carteiras, estoques ou movimentos autoritativos em valores sem consequências intermediárias viola a SPEC. Árvore de expressões pode ser mais lenta que inteiros em arrays.

### H12 — “Reordenação somente de acontecimentos independentes” [B]

Se dois eventos têm conjuntos de leitura/escrita disjuntos, preparar/processar em paralelo ou reordenar computacionalmente sem mudar estado final nem observações válidas. Inspirado em redução de ordens equivalentes da verificação formal.

**Falha:** compartilhamento oculto (fila, RNG, recurso, salário, preço, acesso) pode tornar suposta independência falsa. Ordem de pagamento/saldo sempre precisa ser correta.

### H13 — “Contribuições por lote preservando IDs” [B]

Milhares de trabalhadores podem compartilhar **cálculo** de um mesmo contrato/política salarial, mas toda transferência continua própria e ocorre no instante devido. Passageiros compartilham posição física do ônibus, não renda e histórico.

**Falha:** compartilhamento computacional não pode esconder inadimplência, capacidade, lotação, embarque e escolhas individuais.

### H14 — “Executor adaptativo por tipo de conflito” [B/C]

Se eventos dominam em estrada vazia e passos vetorizados vencem num cruzamento saturado, o motor usa métodos numéricos distintos **preservando exatamente a mesma regra física**. A GPU pode ser executor técnico opcional para muitos veículos simultâneos.

**Falha:** transição de método e cópias entre CPU/GPU podem custar mais que economia; equivalência de física é a condição, não um bônus.

### H15 — “Prova de otimização com contraexemplos” [A/B]

Quando duas implementações alegam produzir o mesmo resultado, gerar cenários extremos e localizar o primeiro instante diferente. Para transformação matemática local muito restrita, estudar Alive2, dReach e testes baseados em propriedades. Não validar “parece igual no FPS”.

**Falha:** ferramentas formais não validam automaticamente o Core todo; prove somente o que o modelo realmente cobre.

### H16 — “Acelerador FPGA/neuromórfico específico” [D]

Criar hardware que executa filas de eventos ou redes de decisões diretamente, sem CPU convencional. Há pesquisas nessa direção, mas é impraticável como dependência de um city builder doméstico, e não resolve semântica de economia e estado dinâmico.

### H17 — “Computação quântica, IA para calcular o futuro” [D]

Nenhuma fonte analisada mostra vantagem prática na execução causal de uma cidade individual com física, carros e contas. Gerar resultado provável não é realizar todos os acontecimentos. IA pode ajudar a **pesquisar ou instrumentar algoritmos**, nunca substituir estado verdadeiro aprovado.

## 4. Modelo mental: “motor causal compilável” (NÃO é nova arquitetura aprovada)

**Nome provisório exclusivamente de pesquisa.** Um Simulation Core C# é a autoridade, e implementações locais eficientes fazem:

- **Verdade individual:** SIM, empresa, veículo, posição, dinheiro, mercadoria, propriedade e relações reais por ID estável.
- **Dependências:** índices de quem precisa acordar quando trânsito, renda, oferta, horário ou acesso mudar.
- **Agenda leve:** executar o próximo acontecimento efetivo, ordenar por tempo e dependência; cancelar/reagendar previsões invalidadas.
- **Contrato temporal opcional:** representar o próximo intervalo seguramente computável de um processo físico/econômico; nunca confirmar conclusão antes das condições reais.
- **Trechos de programação estáveis opcionais:** uma sequência curta de funções com condições conhecidas e revalidáveis, eliminando dispatch desnecessário. Não um compilador global.
- **Processamento físico individual:** quando há conflito real, usar cálculo de colisão/frenagem/mudança de faixa suficiente — sem distinção por câmera.
- **Transação real:** decidir por SIM/empresa, confirmar competição de compra, entregar produto, debitar/creditar saldos, preservar oferta monetária.
- **Prova prática:** mesmas seeds/comandos, reproduzir o primeiro acontecimento divergente, conservar dinheiro e estoques.

**Exemplo integral 1 — indústria perde energia:** queda de energia confirma instante real; índice identifica consumidores; futuros lotes dependentes são invalidados antes de serem concluídos; entregas físicas e salários seguem suas regras; comércios sem produto e consumidores sofrem efeitos verdadeiros; retorno da energia acorda agentes pertinentes.

**Exemplo integral 2 — veículo em rodovia:** posição é calculável num trecho realmente livre; ao entrar veículo próximo, fazer curva, trocar faixa, atingir semáforo ou sofrer bloqueio, a validade acaba; resolver os acontecimentos individuais e agendar novo instante seguro. Um caminhão só descarrega após deslocar-se fisicamente; ele não pode cruzar bloqueio por ter previsão de chegada.

**Exemplo integral 3 — dois compradores do último produto:** cada um avalia comércio e se desloca de verdade; no momento da compra a unidade pertence a quem realmente conseguiu comprá-la pelas regras aprovadas; só essa compra transfere dinheiro e estoque. Nunca aceitar dois commits simultâneos baseados no mesmo resultado derivado.

## 5. O que NÃO funciona como atalho mágico

1. **Não** desligar simulação off-camera, pular calendário ou substituir rotina real por projeção.
2. **Não** substituir transações de dinheiro, lotes e entregas por médias coletivas.
3. **Não** usar quantização numérica ou previsão de rede neural como equivalência demonstrada: são aproximações em muitos contextos.
4. **Não** assumir que “fila global zero” é melhor: compiladores de circuitos só mostram oportunidade em grafos relativamente estáveis.
5. **Não** confundir vida individual persistente com objeto gráfico/polling contínuo.
6. **Não** criar ECS, compilador JIT, motor de regras, GPU, servidor ou rollback global por entusiasmo.
7. **Não** prometer 100x/1000x ou milhões de agentes até haver benchmark de **simulação integrada com todas as interações**.
8. **Não** supor que uma arquitetura mais complexa é automaticamente mais rápida. Seus índices, cancelamentos e transferências também consomem CPU.

## 6. Quais hipóteses eu tentaria refutar primeiro

| Experimento técnico opcional | Hipótese sob ataque | Critério de aprovação | Critério de descarte |
| --- | --- | --- | --- |
| Caminho livre, faixa densa, semáforo, rotatória e rua fechada | H02/H06/H07 | Mesmas posições, filas, viagens, tempos e consequências na referência; ganho líquido | Erro físico, horizonte muito curto ou invalidação cara |
| 10k+ SIMs com produção, emprego e compras concorrentes | H01/H04/H10 | Estado real individual, dinheiro/estoque conservados, resultados equivalentes; menos buscas | Índice explode, prioridade errada, efeitos fictícios |
| Turnos repetidos em período estável versus cidade mudando a cada instante | H03/H12 | Agendamento especializado mais barato mesmo contando recomposição | Recompilação/ordenação mais cara que heap simples |
| Simulação headless serial versus tarefas concorrentes | H08/H09/H12 | Mesmo resultado causal, processamento efetivo por segundo maior | Inversão de eventos, deadlock, RNG imprevisível |
| Cidade com trânsito livre e crise generalizada | H14 | Troca de executor mantém física e melhora tempo integrado | Transferências/sincronização anulam ganhos |
| Mesmo save/cidade/eventos com versão simples e acelerada | H15 | Explicar toda primeira divergência ou provar equivalência | Mudança de saldo, estoque, idade, posição ou prioridade |

**Métrica real principal:** segundos simulados *efetivamente processados* por segundo de relógio real, mais p95/p99 de latência de eventos, agentes/interações físicas ativas, custo de GC, tempo de rotas/colisões, dependências invalidadas e tempo de aplicação de efeitos econômicos. FPS isolado não mede sucesso. Fazer essas comparações como experimentos **proporcionais ao risco dentro do desenvolvimento integrado**, não criar POCs obrigatórias por sistema.

## 7. Referências e projetos verificáveis (ordem por relevância)

Estas fontes foram consultadas em busca online e/ou README/código público dos mantenedores; números de artigos não foram reproduzidos por nós. Fontes relatadas por comunidades são identificadas como auto-relatos. O algoritmo hipotético combinado **não** foi publicado ou validado por esses autores.

- **[R01]** Gibson & Bruck, artigo original de Next Reaction, eventos dependentes: https://www.stat.yale.edu/~jtc5/GeneticNetworksGroup/GibsonAndBruck2000.pdf
- **[R02]** FERN, análise técnica de método exato e aproximado de química: https://pmc.ncbi.nlm.nih.gov/articles/PMC2553347/
- **[R03]** Self-triggered e event-triggered control, IEEE 2012: https://ieeexplore.ieee.org/document/6425820
- **[R04]** Robótica/UAS, explica monitoramento e cálculo do próximo instante: https://pmc.ncbi.nlm.nih.gov/articles/PMC8880505/
- **[R05]** Stanford, *A General Method for Compiling Event-Driven Simulations*: https://suif.stanford.edu/papers/rfrench95/paper.html
- **[R06]** Compilação de Esterel em eventos estáticos: https://doi.org/10.1016/j.entcs.2006.02.027
- **[R07]** Salsa, repositório do compilador incremental: https://github.com/salsa-rs/salsa
- **[R08]** Salsa, red-green incremental: https://github.com/salsa-rs/salsa/blob/master/book/src/reference/algorithm.md
- **[R09]** QSS Solver, código e documentação de μ-Modelica compilada: https://github.com/CIFASIS/qss-solver
- **[R10]** OpenModelica, sistemas quantizados QSS e limites: https://build.openmodelica.org/Documentation/ModelicaDEVS.UsersGuide.QSS.html
- **[R11]** Ptolemy II, tempo superdenso: https://ptolemy.berkeley.edu/ptolemyII/ptII11.0/ptII/doc/codeDoc/ptolemy/actor/SuperdenseTimeDirector.html
- **[R12]** Random123, RNG por chave e contador: https://github.com/DEShawResearch/random123
- **[R13]** EDMD, vários calendários de eventos e vizinhanças: https://github.com/FSmallenburg/EDMD
- **[R14]** ROSS, execução conservadora e risco de rollback: https://ross-org.github.io/feature/schedulers.html
- **[R15]** Alive2, validação de otimizações específicas: https://github.com/AliveToolkit/alive2
- **[R16]** dReach/dReal, segurança híbrida de alcance limitado: https://dreal.github.io/dReach/
- **[R17]** Ptolemy, cache e invalidação de agenda: https://ptolemy.berkeley.edu/ptolemyII/ptII11.0/ptII/doc/codeDoc/ptolemy/actor/sched/Scheduler.html
- **[R18]** Mob City, inconsistência ao atualizar fora da câmera com ritmos diferentes: https://mobcitygame.com/308/
- **[R19]** Omnith, relato técnico do autor em C#/Godot: https://werlinger.dev/writing/what-is-omnith
- **[R20]** Estudos dos limites do model checking simbólico: https://www.sciencedirect.com/science/article/pii/S1574013710000407
- **[R21]** PDES-A, FPGA para eventos paralelos: https://doi.org/10.1145/3064911.3064930
- **[R22]** Ptolemy, static scheduling reutilizável: https://ptolemy.berkeley.edu/ptolemyII/ptII11.0/ptII11.0.1/doc/codeDoc/ptolemy/actor/sched/StaticSchedulingDirector.html
- **[R23]** Estudo sobre eventos inválidos por precisão finita: https://doi.org/10.1007/s40571-014-0021-8
- **[R24]** Controle assíncrono com passos locais: https://doi.org/10.1016/j.cma.2012.11.004
- **[R25]** Manual original ns-3 de eventos/agenda/cancelamento: https://github.com/nsnam/ns-3-dev-git/blob/master/doc/manual/source/events.rst
- **[R26]** NVIDIA GPU Gems: seleção de pares candidatos de colisão: https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-32-broad-phase-collision-detection-cuda
- **[R27]** Pesquisa em self-adjusting computation: https://doi.org/10.1016/j.entcs.2005.11.043
- **[R28]** Debate comunitário sobre um milhão de agentes e custos ECS: https://discussions.unity.com/t/should-i-use-ecs-for-my-game/1561389
- **[R29]** SIMD/eventos no jogo, auto-relato de Unreal 100k agentes: https://www.reddit.com/r/unrealengine/comments/1jf124j/100000_ai_agents_in_ue5_with_collision/
- **[R30]** Avanço por grupos com erro medido (pesquisa anterior): https://philipp-andelfinger.net/pdfs/andelfinger2020fastforwarding.pdf

## Conclusão — minha recomendação após procurar ideias improváveis

> **Revisão humana desta conclusão: PENDENTE.** Não alterar a SPEC, a ARCHITECTURE ou o código a partir desta pesquisa sem o processo de decisão apropriado.

**A ideia mais forte não é “rodar cada SIM em uma GPU” nem “substituir a cidade por estatísticas”. Minha aposta é executar uma cidade como uma rede de mudanças reais, com planos temporários verificáveis de quais acontecimentos podem acontecer e de quando uma reavaliação é realmente necessária.**

Apelido puramente exploratório: **motor causal compilável**. Ele combinaria três princípios: (1) **próxima reação e grafo de dependência** da química, (2) **próxima amostragem necessária** do controle de robôs e (3) **agenda parcialmente pré-compilada** dos simuladores de circuitos. A leitura incremental de oportunidades viria dos compiladores como Salsa. A **combinação não foi demonstrada para o IndexCities**.

**O que eu faria agora, concretamente:**

1. **Preservar a base já aprovada:** Core C# único, IDs persistentes, ordem temporal causal, scheduler simples, Godot apenas como host/apresentação, economia e física verdadeiras. Não criar um novo megaframework.
2. **Explorar índices de dependência e operações orientadas a mudanças** para economia e trânsito. É o benefício plausível mais amplo e mais barato.
3. **Estudar “contratos temporais verificáveis” como experimento técnico opcional**, em um fluxo já definido pela SPEC: atividade estável ou via de trânsito previsível; comparar contra execução detalhada, medindo ganho e primeira divergência.
4. **Somente com gargalo de agenda comprovado**, experimentar compilação compacta das sequências estáveis; **somente com gargalo físico comprovado**, comparar CPU microscópica e GPU opcional.
5. **Rejeitar sem apego** a ideia extraordinária se invalidações, memória, recomposição ou bugs ultrapassarem o ganho.

**O salto conceitual seria passar de “atualizar o estado de todos a cada instante” para “executar somente as mudanças e interações que a física, a economia e as escolhas dos agentes realmente exigem”, mantendo a história causal inteira.** Aceleramos computação inútil, não a eliminamos falsamente da história. O limite real permanece: uma cidade com milhões de interações verdadeiras custa trabalho verdadeiro, independentemente do algoritmo.

**Status final: PENDENTE de revisão humana. Sem decisões técnicas ou de produto promovidas.**
