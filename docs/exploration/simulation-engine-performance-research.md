# Pesquisa independente — motor de simulação de cidade extremamente rápido

> **Revisão humana: PENDENTE.** Pesquisa e conclusões propostas por IA em **2026-10-09**, solicitadas pelo responsável, sem revisão nem aprovação de arquitetura ou requisitos. Este documento **não é fonte de verdade** e nenhuma recomendação técnica aqui autoriza mudar o produto.
>
> **Escopo:** investigação técnica independente e continuável sobre aceleração REAL de uma cidade com SIMs, veículos, propriedades, estoques e acontecimentos individuais. Não é plano obrigatório de POC nem implementação.
>
> **Fontes canônicas:** [SPEC](../SPEC.md) (produto), [ARCHITECTURE](../ARCHITECTURE.md) (fronteiras técnicas), [AGENTS](../../AGENTS.md) (regras de trabalho); [EXPLORATION](../EXPLORATION.md) (índice). [Pesquisa anterior de escala/calendário](simulation-scale.md) permanece separada; este arquivo aprofunda especificamente **engenharia de simulação**.
>
> **Leitura rápida:** ver [conclusão](#conclusão--o-que-eu-recomendo-para-o-indexcities) ao final. Todas as sugestões abaixo são hipóteses de engenharia, não decisões.

## 1. Problema real e restrições

**Pergunta:** como avançar de verdade uma cidade inteira excepcionalmente rápido sem descartar cidadãos, viagens, estoques, dinheiro e acontecimentos? A pergunta não é “como aumentar FPS”; é **quantos segundos/dias simulados corretos conseguimos executar por segundo de CPU/tempo real**, com gameplay responsiva.

O que a SPEC já exige, entre outras coisas:

- SIMs persistentes e selecionáveis, com moradia, empregos, carteiras, decisões e consequências individuais;
- empresas reais com caixa, trabalhadores, estoques, produção e decisões próprias;
- viagens presenciais, caminhões e materiais fisicamente entregues; trânsito, faixas, cruzamentos, estacionamentos e pedestres com efeitos dentro e fora da câmera;
- salários, aluguel, dívida, consumo e oferta monetária fixa, sem operações fictícias ou criação/destruição de valores;
- **velocidades pause/1/2/3 e aceleração efetiva da mesma simulação**; duração do calendário, máxima aceleração e velocidades adicionais ainda abertas;
- Godot/C# e Simulation Core sem dependência do Godot, com scheduler e possibilidade de execução headless; não pressupor ECS, threads ou event bus global.

**Tese:** existência persistente ≠ objeto gráfico ≠ atualização contínua ≠ decisão recorrente. Reduzir o trabalho necessário por fato real costuma ser mais promissor que tentar acelerar todos os cálculos existentes com paralelismo.

### Três níveis de equivalência que não devem ser confundidos

1. **Equivalência de estado e causalidade:** um evento ou integração de intervalo produz as mesmas mudanças que uma execução detalhada correta teria produzido, respeitando a ordem de efeitos importantes.
2. **Equivalência física exigida pela SPEC:** posições, viagens, filas, consumo, acessibilidade e limites de capacidade precisam corresponder ao modelo operacional aprovado. Um veículo individual com viagem real não é “um percentual médio de veículos por zona”.
3. **Equivalência numérica bit a bit:** executar na mesma plataforma pode permitir comparação estrita; a ARCHITECTURE não impõe determinismo bit a bit entre todas as plataformas. Diferenças de ponto flutuante ou ordem matemática só são aceitáveis se não mudarem indevidamente os resultados exigidos.

**Alerta:** “agendar chegada e interpolar posição” só funciona como simulação correta enquanto nenhuma interação invalidar trajetória, velocidade, capacidade, fila ou estado. Fora dessa condição, é previsão condicional, não verdade.

## 2. O que a evidência externa realmente ensina

### Estudos e relatos mais diretamente úteis

| Evidência | Observação verificada | Transferível? / limite |
| --- | --- | --- |
| **Factorio — entidades adormecidas** [F01, F02] | Roboports que não precisavam executar lógica contínua passaram de ~1 ms para ~0,025 ms por tick **naquela categoria de trabalho**, segundo relato dos autores. Outras rotinas passaram a despertar por alteração de estado. | Forte inspiração para disparos por necessidade. **Não** representa 40x no jogo inteiro. |
| **Factorio — posições relativas e segmentos** [F03, F04] | Para itens em esteiras, o motor mantém intervalos entre elementos e altera offsets de segmentos; desenvolvedores relataram ganhos muito altos **na operação específica de movimento de itens**. | Ideia extraordinária para séries ordenadas sem ultrapassagem, não diretamente aplicável a carros com trocas de faixa e colisões. |
| **Factorio — quando threads não ajudaram** [F01, F05] | Uma tentativa de paralelizar eletricidade aumentou uso de CPU, mas não a velocidade geral: acesso à memória era o gargalo. | Não assumir que “mais cores = mais velocidade”. |
| **A/B Street — eventos discretos** [F06, F07] | Pedestres e motoristas mantêm estados temporais; só são despertados em transições ou quando uma fila/liberação exige reavaliar. | Aplicação direta da ideia de estados temporais; autor explicita limitações e custo dos casos complicados. |
| **SUMO micro x meso** [F08, F09] | Modelo mesoscópico em filas documenta até 100x de aceleração em contextos próprios, **sacrificando parte dos detalhes**. | Excelente contraste para estudar filas; não copiar o modelo integral como se preservasse a física da SPEC. |
| **MATSim** [F10] | Biblioteca aberta para populações/agendas/tráfego baseados em agentes, com módulos substituíveis. | Inspira organização e benchmark; economia e física do IndexCities são mais amplas. |
| **SimMobility (MIT)** [F11, F12] | Pesquisa integra agentes, demanda e mobilidade em múltiplas escalas; versão Short-Term detalha movimento de tráfego/pedestres/cargas. | Bom mapa de interfaces entre escalas, mas misturar escalas de simulação exige provar conservação e causalidade. |
| **GEMSim** [F13] | Trabalho científico relata ganhos expressivos sobre MATSim em modelo de mobilidade **mesoscópico em GPU**, com hardware/cenário específicos. | Prova viabilidade de GPU para certos kernels, não aceleração automática do nosso trânsito microscópico completo. |
| **OSRM / roteamento** [F14] | Usa Contraction Hierarchies e Multi-Level Dijkstra, com etapas de partição e customização de pesos. | Boa inspiração para consultas macro e atualização de rotas; não implica usar serviço externo nem recalcular redes a cada frame. |
| **D* Lite** [F15, F16] | Replanejamento incremental reutiliza busca prévia; otimização com buckets mostrou ganho em aplicação experimental a jogos. | Investigar quando ruas mudam; perfil urbano de muitos destinos pode favorecer outros algoritmos. |
| **Kinetic Data Structures (KDS)** [F17] | Literatura de geometria mantém propriedades de objetos em movimento por certificados válidos até seu próximo evento de falha. | Inspiração fora da caixa para prever **próxima interação**, não garantia de simular cruzamentos complexos sem passos físicos. |
| **Filas de agendamento** [F18, F19] | Timing wheels e calendar queues aceleram administração de grande número de eventos **sob condições adequadas**. | Comparar com heap simples **após medir** o custo de enfileiramento, cancelamento e desempate. |
| **Differential Dataflow** [F20] | Framework mostra como atualizar cálculos derivados quando entradas mudam, sem refazer toda a consulta. | Inspiração para índices de vagas, bairros, oferta e impacto de rede; usar o conceito, não importar uma infraestrutura distribuída. |
| **Godot / .NET** [F21–F24] | Godot recomenda Servers e MultiMesh quando SceneTree é cara; ferramentas .NET permitem medir GC/CPU. | Renderização e núcleo separados; não confundir FPS com capacidade de avançar o calendário. |
| **OpenTTD — tempo** [F25] | Separar relógios exigiu revisão extensa de compatibilidade, estatísticas, custos e UI. | Advertência: múltiplos relógios não são atalho transparente e não foram aprovados para o IndexCities. |
| **Feedback de jogadores CS2** [F26] | Relatos mostram desaceleração do calendário mesmo com FPS utilizável em cidades grandes. | Evidência qualitativa, não benchmark controlado nem medição de causa específica. |

**Como ler os números:** melhorias percentuais, milhões de agentes e multiplicadores pertencem estritamente aos modelos, tarefas e máquinas de cada fonte. Não são metas ou estimativas de ganho do IndexCities. Fontes primárias e trabalhos científicos pesam mais que comentários comunitários.

## 3. Inventário de estratégias, incluindo ideias fora da caixa

Classificação: **P1 = prioridade alta para investigação técnica**, **P2 = investigar quando houver gargalo**, **P3 = experimental ou pouco recomendável agora**. Essas prioridades não são aprovação nem fases de entrega.

### A. Fazer menos trabalho mantendo o mundo verdadeiro

**A1 — Máquina de estados com despertar sob demanda (P1).** SIM dormindo, loja fechada, fábrica aguardando insumo e veículo estacionado permanecem agentes reais, mas não executam varreduras repetidas. Quem muda a condição desperta o interessado. *Risco:* esquecer um gatilho e criar estado desatualizado. Evidência: [F01, F02, F06].

**A2 — Scheduler por “próxima transição causal” (P1).** Agendar fim do turno, vencimento do aluguel, começo de viagem, chegada a cruzamento, conclusão de carregamento, atualização de preço quando devida. A fila guarda eventos reais; eventos condicionais são invalidados quando condições mudam. *Risco:* explosão da fila em congestionamentos e eventos de curtíssimo prazo. [F06, F18, F19].

**A3 — Cálculo analítico de intervalos estáveis (P1).** Em vez de executar a mesma subtração milhares de vezes, calcular o consumo ou progresso acumulado **até o próximo limite real**, como estoque zerar, turno acabar, reposição chegar ou fornecimento falhar. **Não** gerar pagamentos fictícios, ignorar compra física ou pular mudança de estado. *Risco:* uma condição oculta muda no meio do intervalo e invalida a integração. Conceito relacionado a [F06, F17].

**A4 — Predizer apenas para saber quando verificar (P1).** A previsão não confirma o resultado. Ela determina o próximo instante em que a condição pode deixar de ser verdadeira; nesse instante o núcleo confere e efetiva a transição. Ex.: previsão de chegada à parada, mas um bloqueio às 10h cancela o evento de chegada anterior. *Risco:* garantir que invalidações sejam completas.

**A5 — Regra única para ciclos repetitivos, efeitos individuais na hora certa (P2).** Compartilhar parâmetros de horários entre milhares de funcionários sem tornar trabalhadores intercambiáveis: cada vínculo, presença, salário e trajetória continuam próprios. Rotina e configuração podem ser compartilhadas; **estado e efeitos não**. *Risco:* tentar “processar um salário por todos” sem conferir fundos e obrigações individuais.

**A6 — Fronteiras físicas como pontos de reavaliação (P1).** Dentro de trecho livre e previsível, derivar posição com intervalo; antes de parar, mudar faixa, acessar prédio ou atravessar cruzamento, resolver a interação efetiva. *Risco:* mudança de congestionamento entre fronteiras precisa ativar reavaliação antecipada.

**A7 — Separar trabalho programado de trabalho provocado (P1).** “Às 8h abre o mercado” é evento temporal; “acabou o estoque” é evento de estado. Ambos acessam as mesmas regras. Evitar polling para descobrir acontecimentos já conhecidos.

### B. A parte mais radical: mover pessoas sem atualizar todos os movimentos a cada instante

**B1 — Coordenadas relativas em comboios/filas ordenadas (P2, experimental).** Adaptar **conceitualmente** a otimização das esteiras do Factorio [F03]: IDs e espaçamentos individuais persistem, mas um conjunto ordenado num trecho uniforme compartilha avanço de referência. Inserção, saída, mudança de faixa e parada quebram/reorganizam grupos. *Limite:* carros têm velocidade e interação próprias; os grupos podem desintegrar-se continuamente, anulando o ganho. Não permitir salto de posição ou interpenetração.

**B2 — Certificados cinemáticos de validade (P2, fora da caixa).** Inspirado em KDS [F17]: em vez de atualizar a cada passo um veículo com aceleração/velocidade previsível, manter condições do tipo “não alcançará o líder antes de t”, “não entrará no cruzamento antes de t”. Agendar **falha potencial do certificado**. *Risco maior:* número de certificados, cálculos numéricos, mudanças bruscas e objetos que cruzam vários segmentos. Exige comparação rigorosa com referência microscópica, sem prometer exatidão universal.

**B3 — Tráfego misto **por situação**, não pela câmera (P2).** Trechos livres seguem progressão por eventos; interações difíceis usam cálculos físicos frequentes; quando estabilizam, voltam ao regime barato. Essa troca **só é válida se os dois regimes representam o mesmo modelo de física da SPEC**, sem desligar ultrapassagens ou capacidade. *Risco:* transições custosas e discrepâncias no limite entre trechos. Não confundir com simplesmente usar SUMO MESO em bairros distantes. [F06–F09].

**B4 — Filas como estado autoritativo em gargalos (P1/P2).** Cruzamentos, entradas/saídas, pontos de ônibus, faixas de travessia e docas podem tratar admissão e liberação individual em filas, cada ocupante com ID e posição. Uma fila pode conter milhares de agentes sem precisar que todos tentem entrar no cruzamento a cada tick. *Risco:* comprimentos, spillback, mudança de faixa, veículos longos, calçadas e prioridade física [F06, F08].

**B5 — Resolver conflitos onde ocorrem (P1).** Semáforo ou interseção é dono do direito de entrada, capacidade e fila; veículos não fazem tentativas globais insistentes. *Risco:* starvation, escolha de prioridade errada, bloqueio circular, e referência a espaço já ocupado.

**B6 — Próxima interação em vez de próximo frame (P2).** Estudar eventos de alcançamento do líder, liberação de faixa, chegada ao ponto de frenagem e abertura de sinal. *Risco:* custo de previsão pode ser maior que pequenos passos quando há muitos agentes interagindo. Não supor que toda dinâmica de tráfego admita solução fechada.

### C. Economia, moradia, empregos, suprimento: atualizar apenas os dependentes

**C1 — Índices reversos e conjuntos de interessados (P1).** Empresa perde energia → localizar consumidores dela; fornecedor perdeu lote → localizar entregas/pedidos afetados; rua muda → localizar rotas cuja validade depende dela. Nada de iterar todos os SIMs. [F20] é referência conceitual, não framework recomendado.

**C2 — Mercado de oportunidades atualizado incrementalmente (P1).** Mudou uma vaga, salário, residência ou acesso → atualizar índices de vagas acessíveis e marcar **apenas os agentes pertinentes** para sua decisão normal. Não criar nova IA por pessoa, nem contratar automaticamente sem comparar opções e condições reais.

**C3 — Separar consulta de decisão e confirmação transacional (P1).** Avaliações independentes podem ser preparadas em lote; alterações em estoque, propriedade, vagas e carteiras exigem confirmação ordenada/atômica. Dois compradores competindo por um último alimento não podem ambos recebê-lo. *Risco:* uma avaliação preparada usa preço ou quantidade que mudou; revalidar no compromisso, sem inventar disponibilidade.

**C4 — Regras de preços e produção com gatilhos claros (P1).** Custos, vendas, pedidos e parâmetros de ajuste podem provocar reavaliação da empresa somente quando úteis; produto não precisa ser precificado por SIM ou por frame. Eventos reais de produção, falta de insumo, uso de capacidade e vendas continuam registrados.

**C5 — Consumo doméstico por quantidades e cruzamento de limiar (P1).** A SPEC já agrega estoque doméstico por categoria e exige compra presencial. Para consumo regular, calcular o instante em que reserva chega ao gatilho de compra/esgotamento; reagendar após compra, falta, mudança de membros ou serviço. *Risco:* a frequência efetiva de alimentação e suas consequências devem continuar reais.

**C6 — Mudanças urbanas como propagação localizada (P1).** Fechamento de via, queda de energia e alteração de área de serviço geram trabalho proporcional ao número de relações afetadas, não automaticamente à população total. *Risco:* propagação em cascata genuinamente grande não pode ser falsamente suprimida.

**C7 — Materializar apenas agregados DERIVADOS (P1).** Estatísticas da cidade e overlays podem ser contadores incrementais derivados de transações reais, sem recalcular mil saldos. Não criar “saldo da cidade” adicional ou estoque global fictício. Diferenciar read model de estado primário.

### D. Organização do trabalho e dados

**D1 — Estrutura compacta de agentes com dados quentes/frios (P1/P2).** IDs estáveis e entidades persistentes, mantendo dados usados em loop próximos na memória; histórico, aparência e atributos raros separados. Evitar um objeto pesado por cada atualização. *Risco:* complexidade de sincronização de representações; medir antes de SoA/ECS completo. [F05].

**D2 — Conjuntos ativos e bitsets de mudanças (P1/P2).** Estado completo existe para todos; lista compacta de ativos por domínio evita varrer milhares de inativos. Dirty flags têm proprietário claro e invalidam derivados corretamente.

**D3 — Calendário de eventos eficiente (P2).** Começar com heap de prioridade simples e ordem determinística para empates. Se a fila virar gargalo, comparar calendar queue, timing wheel hierárquica ou buckets segundo distribuição real de horários, cancelamentos e mudanças. Não aceitar O(1) teórico fora das condições da fonte. [F18, F19].

**D4 — Lotes de operações homogêneas sem apagar as identidades (P2).** Avaliar em lote decisões de candidatos ou cálculos de consumo; registrar resultados por ID e confirmar efeitos conforme regras. *Risco:* ordens de processamento e aleatoriedade que dependam de tamanho do lote; incluir seed/estado de RNG reproduzível.

**D5 — Separação completa apresentação x simulação (já aprovada; executar conforme ARCHITECTURE).** Posições e estado pertencem ao núcleo; Godot desenha com Nodes quando justificado, MultiMesh/Servers quando vantagem medida. Não remover veículos fora da câmera nem decidir economia na renderização. [F21, F22].

**D6 — Renderização com posição sob demanda (P1/P2).** Apenas a visão precisa calcular coordenada fina para *desenhar* todos os objetos visíveis em cada frame. O núcleo mantém estado temporal e disponibiliza posição correta a qualquer instante consultado; eventos físicos continuam acontecendo fora da vista. *Risco:* consulta visual jamais pode “ativar” simulação causal ou alterar resultado.

**D7 — Perfil de carga da cidade, não apenas população (P1).** Métricas de eventos/s, interações por cruzamento, rotas inválidas, transações e operações físicas são mais explicativas que “50 mil habitantes”. Uma cidade com 25 mil pessoas em movimento e bloqueios pode custar mais que uma de 100 mil em período estável.

### E. Algoritmos e métodos de pesquisa menos óbvios

**E1 — Particionamento por redes de dependência, não só quadrados do mapa (P2).** Trabalhos espacialmente distantes podem competir pelo mesmo estoque, emprego ou via arterial. Antes de paralelizar “bairros”, separar tarefas realmente independentes; conflitos cruzados viram dependências explícitas. *Risco:* concentração de interações no centro elimina o paralelismo.

**E2 — Execução paralela em duas etapas (P2).** Preparar cálculos independentes sobre snapshot imutável; validar e aplicar mutações compartilhadas numa ordem consistente. *Limite:* snapshot pode envelhecer; deve haver revalidação e resolução de conflito. Não cria segundo estado autoritativo.

**E3 — Conservative Parallel Discrete Event Simulation / lookahead (P3).** Se cada região conhece um intervalo mínimo sem eventos externos capazes de alterá-la, pode progredir em paralelo até a fronteira segura de tempo. É ideia da literatura de PDES, mas cruzamentos, logística, clientes e energia diminuem a janela segura. *Risco:* sincronização complexa sem ganhos num jogo interativo. [F27, F28].

**E4 — Time Warp otimista com rollback (P3, não recomendado agora).** Deixar regiões avançarem e desfazer efeitos se evento anterior chegar atrasado. Em cidade com estoque, dívidas, compras e tráfego, reverter toda cadeia causal pode consumir mais memória/CPU que o ganho. Pesquisa histórica, não proposta prática. [F27].

**E5 — Batch de integração matemática com prova de limites (P2).** Se uma equação é separável e todas as condições relevantes continuam fixas no intervalo, processar resultado acumulado e registrar marcos de mudança; prove equivalência e preserve a sequência de transações que envolvem agentes distintos. Não converter “vender uma vez por mês” em venda fictícia. Essa é uma otimização **da matemática conhecida**, não nova regra do jogo.

**E6 — Preparação especulativa descartável (P3).** Calcular antecipadamente rotas candidatas ou rankings de fornecedores, **sem efetivar viagens/transações**; usar somente se condições ainda válidas. Se invalidou, descartar. *Risco:* desperdício de CPU em candidatos que nunca serão usados. Pior na aceleração alta se o mundo muda rapidamente.

**E7 — Consultas incrementais como banco de dados (P2, conceitual).** Índices de candidatos, estoques acessíveis, regiões conectadas e motivos de falha podem reagir a mudanças. Differential Dataflow mostra a ideia geral [F20]; **não** recomenda instalar framework ou criar camada de banco de dados na primeira versão.

**E8 — Work stealing, SIMD e GPU para kernels realmente homogêneos (P3 inicialmente).** Úteis para consultas massivas ou cálculos sem dependências; inadequados como solução genérica para decisões variáveis em cadeia. GPU pode custar cópia/sincronização, e lanes de execução divergentes perdem eficiência. [F13, F05].

**E9 — Separar relógio de renderização, mas NÃO criar calendários divergentes (já previsto).** Godot atualiza quadros visuais enquanto o núcleo avança passos/eventos próprios. Velocidade requerida é diferente de velocidade sustentável; se o núcleo atrasar, precisa informar a limitação, não declarar acontecimentos executados sem fazê-los. [F25] mostra armadilhas de múltiplos calendários.

**E10 — Registrar somente causalidade relevante + checkpoints (P2).** Para diagnóstico, cada transação real precisa ser rastreável a pagador, recebedor e motivo; não é necessário persistir todas as posições renderizadas por frame. Seeds, saves e sequência de comandos permitem comparar algoritmos. **Histórico/replay consultável como feature não foi aprovado.**

**E11 — Instrumentar custo por tipo de acontecimento (P1).** O gargalo pode ser sequência de 50 mil eventos inúteis, lookup de rota, GC, dispersão de memória ou uma única atualização enorme. Contar eventos, agenda, retries, invalidações e tempo por domínio antes de adotar solução arquitetural. [F23, F24].

**E12 — Rejeitar microdetalhe sem valor, mas apenas por decisão de produto (fora desta pesquisa).** Se uma regra obrigatória se mostrar inviável, não removê-la como “otimização técnica”: voltar ao responsável/SPEC. O desenvolvedor pode simplificar *implementação equivalente*, não mudar resultado.

## 4. Modelos mentais úteis: casos concretos de causalidade

### Caso 1: caminhão em percurso e bloqueio inesperado

1. Caminhão real, motorista real e carga identificada iniciam trajeto. Segmento sem interação pode ter posição derivada de intervalo **enquanto a hipótese se mantém**.
2. Evento de fechamento de via no instante simulado T torna algumas rotas inválidas. Antes de prosseguir, o scheduler resolve fechamento e posição/ocupação dos veículos afetados em T.
3. Caminhão pode precisar parar, replanejar ou aguardar. A carga **não** chega ao estoque do destino enquanto não houver chegada e descarregamento reais.
4. A alteração desencadeia atraso da obra/loja e possíveis faltas/novas decisões, com efeitos financeiros verdadeiros.

**Teste que derruba solução ruim:** mesmo tempo/seed, com e sem câmera apontada, deve produzir a mesma carga no mesmo lugar; caminhão não pode atravessar bloqueio porque seu “evento de chegada” já estava marcado.

### Caso 2: dois compradores e o último alimento

1. Ambos podem analisar opções disponíveis.
2. Na compra presencial, estoque/preço/saldo/acesso/capacidade são revalidados.
3. Uma unidade não pode ser vendida duas vezes; o perdedor mantém necessidade não atendida ou busca outra loja **com novos trajetos reais**.
4. O dinheiro só é transferido uma vez e o estoque doméstico só aumenta para quem recebeu o item.

**Teste que derruba solução ruim:** paralelismo nunca duplica dinheiro, estoque ou atendimento.

### Caso 3: dez mil SIMs dormindo

10 mil identidades e estados continuam persistidos, consultáveis e passíveis de eventos excepcionais; não há obrigação técnica de executar 10 mil rotinas vazias a cada frame. Horários, emergências, necessidade e acesso podem despertá-los individualmente ou em grupos **sem colapsar suas decisões em um agente coletivo**.

**Teste que derruba solução ruim:** selecionar um SIM durante a “inatividade” deve mostrar estado coerente, e um evento que deveria afetá-lo não pode ser ignorado.

### Caso 4: rua livre e rua com congestionamento

No trecho livre, cálculo por intervalo pode bastar. Perto de interação com outro carro, mudança de faixa, pedestre ou gargalo, custo real aumenta. É aceitável gastar mais CPU quando há mais interações reais; não é aceitável ocultá-las porque o jogador acelerou ou moveu a câmera.

## 5. O que eu descartaria ou deixaria para depois

| Ideia atraente | Por que não começar nela |
| --- | --- |
| **Um Node / Update por SIM no Godot** | SceneTree e callback por objeto tornam o custo proporcional ao número de agentes mesmo quando nada mudou. |
| **Simulação microscópica a passo fixo para tudo** | Correta se muito bem feita, mas paga custo enorme por intervalos previsíveis. Boa **referência de teste em escala reduzida**, não motor obrigatório final. |
| **Só simular perto da câmera** | Viola explicitamente persistência, viagens, congestionamento e causalidade. |
| **Trocar veículos por fluxo agregado de bairro** | Pode ganhar velocidade como SUMO MESO, mas sacrifica regras físicas do IndexCities. Não é otimização equivalente por padrão. |
| **Eventos de chegada sem invalidação** | Transforma cálculo provável em destino garantido, atravessando bloqueios e filas. |
| **Uma IA neural/LLM por SIM** | Aumenta custo e opacidade onde regras de decisão rastreáveis podem ser suficientes; não melhora o processamento por si. |
| **Adotar ECS completo já no primeiro dia** | Arquitetura oficial prefere estruturas simples, profiling e otimização dos pontos quentes antes de ECS/SoA amplo. |
| **Distribuir toda cidade por threads/servidores** | Sincronização de vias, estoques e dinheiro pode dominar o custo; Factorio exemplifica o perigo da memória. |
| **GPU para “executar a economia inteira”** | Decisões condicionais, relações cruzadas, estado mutável e sincronização exigiriam grande redesenho sem evidência. |
| **Time Warp/rollback global** | Complexidade e custo elevados de desfazer transações, viagens, entregas e heranças. |
| **Mais uma velocidade, salto de calendário ou múltiplos relógios** | Seriam decisões novas de produto e não resolvem o custo das operações reais. |
| **“Garantir” 100x ou milhões de SIMs** | Sem hardware, calendário, congestionamento e benchmark integrado, seria marketing e não engenharia. |

## 6. Experimentos técnicos úteis no desenvolvimento integrado

**Não são POCs de produto obrigatórias, nem propõem reduzir a primeira entrega integrada.** São comparações locais de algoritmos, justamente para escolher infraestrutura de desempenho sem reabrir o escopo.

### Critérios obrigatórios

- **Performance:** segundos/dias de simulação concluídos por segundo real; operações/eventos reais por segundo; p50/p95/p99 do custo de avanço; uso de CPU por núcleo, memória, alocações e pausas GC; latência de comando; fila de eventos pendentes.
- **Causalidade:** conservação da oferta monetária; estoques e cargas não duplicados; obra só consome material entregue; nenhum pagamento sem saldo real/credor definido; ID persistente, horários/turnos, propriedades e empregos coerentes.
- **Física:** distâncias, posições, filas, espaços ocupados, estacionamento, chegada e bloqueios condizentes com o trânsito exigido, em qualquer posição da câmera.
- **Reprodutibilidade:** comparar mesmos saves, seeds, comandos, ordenação de eventos e estados de RNG. Conforme ARCHITECTURE, equivalência de produto é necessária; determinismo bit a bit multiplataforma não é requisito.
- **Complexidade:** custo de manutenção, sincronização, invalidações e diagnósticos; uma otimização que cria bugs opacos precisa de benefício muito alto para compensar.

### Conjunto curto de experimentos discriminantes

1. **Scheduler:** comparar uma referência simples por passos com eventos/ativação sob demanda em atividades estáveis, vencimentos, necessidades e reposição. Medir *updates evitados* **e** equivalência do estado.
2. **Mobilidade (maior risco):** em rua livre, semáforo, fluxo misto, rodovia, cruzamento complexo, bloqueio repentino e congestionamento severo, comparar passo físico de referência, eventos por trecho e possíveis certificados de próxima interação. Medir número de interações e erros físicos, não só FPS.
3. **Roteamento:** selecionar empregos/comércios repetidos, fechar um acesso e medir conectividade, seleção macro, rota detalhada e replanejamento incremental; comparar custo de invalidação e caminhos realmente usados.
4. **Concorrência econômica:** muitos consumidores pelo mesmo estoque, empresas pelo mesmo prédio ou empregados por vaga, entregas simultâneas; medir custo de confirmação, revalidação e conflitos.
5. **Layout e paralelismo:** quando houver hotspot real, comparar representação simples e compacta, depois paralelização seletiva. Se ganhar CPU e não ganhar tempo simulado, investigar memória/GC/sincronização antes de ampliar.

**Escalas de observação sugeridas, não metas:** 10 mil, 25 mil, 50 mil e 100 mil SIMs, variando principalmente **agentes ativos, viagens simultâneas, interações por via, empresas e cargas**, não apenas população. A cidade de referência deve conter os domínios aprovados que já estiverem integrados; o ensaio local de algoritmo não substitui a validação global do produto.

### Critérios objetivos para escolher a técnica

- Preferir alternativa de menor complexidade que preserva estados/acontecimentos e melhora *tempo simulado por segundo real* sob o **mesmo** cenário.
- Se eventos produzem muitas invalidações/retries, retornar a passos limitados naquela situação; não aceitar mudança de física escondida.
- Se a fila de eventos domina, testar estrutura especializada; se memória domina, compactar layout; se rotas dominam, índice/hierarquia; se conflitos dominam, reduzir disputa/particionar cálculos, **sempre guiado por medição**.
- Se velocidade solicitada excede capacidade do hardware, registrar a velocidade efetivamente alcançada; não fabricar acontecimentos para “alcançar” o calendário visual.

## 7. Priorização comparativa

| Caminho | Benefício plausível | Risco de incompatibilidade | Esforço/complexidade | Meu julgamento |
| --- | --- | --- | --- | --- |
| Despertar por eventos e ciclos distintos | Alto em agentes inativos | Baixo a médio (invalidação) | Baixo a médio | **Primeira aposta** |
| Integração exata até próximo evento-limite | Alto em fluxos estáveis | Médio (limiares escondidos) | Médio | **Primeira aposta, por domínio** |
| Filas e liberação por evento | Alto em gargalos | Médio (posição/spillback) | Médio/alto | **Central para mobilidade** |
| Rotas hierárquicas e índices incrementais | Alto quando há muitos pedidos | Baixo/médio | Médio | **Preparar API, medir algoritmo** |
| Dados compactos + conjunto de ativos | Médio/alto nos hotspots | Baixo/médio | Médio | **Depois do primeiro profiling** |
| Posições relativas em corredores | Potencial muito alto em trechos regulares | Alto (interações complexas) | Alto | **Experimento fora da caixa** |
| Certificados cinemáticos / próxima interação | Potencial alto, ainda incerto | Alto (numérica e eventos) | Alto | **Pesquisa seletiva, não fundação inicial** |
| Paralelismo por trabalhos independentes | Médio/alto se CPU limitada | Médio/alto | Médio/alto | **Depois de reduzir trabalho inútil** |
| GPU / PDES / rollback | Incerto no modelo real | Alto | Muito alto | **Não recomendar agora** |

## 8. Fontes para continuar pesquisando

**Fontes primárias de desenvolvedores, projetos de código aberto e documentações oficiais:**

- **[F01] Factorio, Friday Facts #421 — Optimizations 2.0 (2024).** Inatividade, processamento de robôs em intervalos, paralelismo e tentativa malsucedida no sistema elétrico. https://www.factorio.com/blog/post/fff-421
- **[F02] Factorio, Friday Facts #148 — Optimizations for 0.14 (2016).** Princípio “do less”, dormir/acordar, agrupamento. https://www.factorio.com/blog/post/fff-148
- **[F03] Factorio, Friday Facts #176 — Belts optimization (2017).** Segmentos, offsets e distâncias relativas entre itens. https://www.factorio.com/blog/post/fff-176
- **[F04] Factorio, Friday Facts #271 — Fluid optimisations (2018).** Localidade de memória, agrupamento de tubos e custos. https://www.factorio.com/blog/post/fff-271
- **[F05] Factorio, Friday Facts #204 — Another day, another optimisation (2017).** Latência/banda de memória versus complexidade de objeto. https://www.factorio.com/blog/post/fff-204
- **[F06] A/B Street, Dustin Carlino — Discrete event traffic simulation (2021).** Posições derivadas, filas, despertar e limitações. https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
- **[F07] A/B Street — retrospectiva dos três anos (2021).** Limites e objetivos da escolha de eventos. https://a-b-street.github.io/docs/project/history/retrospective/index.html
- **[F08] Eclipse SUMO — MESO, documentação oficial.** Velocidade e sacrifícios de detalhe no mesoscópico. https://sumo.dlr.de/docs/Simulation/Meso.html
- **[F09] Eclipse SUMO — FAQ/performance.** Atualizações de veículos e dependência das interações, passo temporal e custo. https://sumo.dlr.de/docs/FAQ.html
- **[F10] MATSim — código/documentação do motor de tráfego baseado em agentes.** https://github.com/matsim-org/matsim-libs
- **[F11] MIT, SimMobility Short-Term — repositório institucional da pesquisa.** https://dspace.mit.edu/entities/publication/b4cf8dd0-cf8f-4e5b-9894-d516ac6f6642
- **[F12] SimMobility — código aberto.** https://github.com/smart-fm/simmobility-prod
- **[F13] GEMSim — *Large-scale multi-agent mobility simulations on a GPU* (2019).** Medições publicadas de sistema especializado em mobilidade. https://www.sciencedirect.com/science/article/pii/S1877050919305617
- **[F14] OSRM — backend e algoritmos CH/MLD.** https://github.com/Project-OSRM/osrm-backend
- **[F15] Koenig & Likhachev — D* Lite (AAAI 2002).** Pesquisa original de replanejamento incremental. https://publications.ri.cmu.edu/d-lite
- **[F16] Likhachev & Koenig — Incremental Heuristic Search in Games (AIIDE 2006).** https://ojs.aaai.org/index.php/AIIDE/article/view/18758
- **[F17] Basch, Guibas & Hershberger — *Data Structures for Mobile Data* (J. Algorithms, 1999).** Estruturas cinéticas/certificados; **pesquisa conceitual**, não implementação urbana demonstrada. https://doi.org/10.1006/jagm.1998.0988
- **[F18] Varghese & Lauck — *Hashed and Hierarchical Timing Wheels* (1987).** Estruturas eficientes de timers. https://doi.org/10.1145/37499.37504
- **[F19] Brown — *Calendar Queues: A Fast O(1) Priority Queue...* (1988).** Resultados condicionados à distribuição de eventos. https://doi.org/10.1145/63039.63045
- **[F20] Differential Dataflow — documentação do projeto.** Manutenção incremental de derivados, não sugestão de framework. https://timelydataflow.github.io/differential-dataflow/
- **[F21] Godot — Optimization using Servers.** https://docs.godotengine.org/en/4.5/tutorials/performance/using_servers.html
- **[F22] Godot — Optimization using MultiMeshes.** https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html
- **[F23] Godot — Thread-safe APIs (4.5).** https://docs.godotengine.org/en/4.5/tutorials/performance/thread_safe_apis.html
- **[F24] Microsoft .NET — dotnet-counters.** https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters
- **[F25] OpenTTD — *The stoppable march of time* (2024).** História e dívidas de separar calendários. https://www.openttd.org/news/2024/03/23/timekeeping
- **[F26] Discussão comunitária sobre diferença entre FPS e velocidade de simulação em Cities: Skylines II (2025).** Evidência anedótica e heterogênea, não benchmark. https://www.reddit.com/r/CitiesSkylines2/comments/1idkeyy/
- **[F27] Rajaei et al. — *The local Time Warp approach to parallel simulation* (1993).** Limitações de paralelismo conservador/otimista. https://doi.org/10.1145/174134.158474
- **[F28] *Relaxing Synchronization in Parallel Agent-Based Road Traffic Simulation*.** Estudo de regiões e sincronização de tráfego. https://dare.uva.nl/id/8b8fd15c-c8d2-4819-ad49-dec2fe8d7571
- **[F29] *GEMSim: A GPU-accelerated multi-modal mobility simulator* (2019).** Versão ampliada do trabalho especializado. https://www.sciencedirect.com/science/article/pii/S1569190X19300267
- **[F30] *A data-driven approach to run agent-based multi-modal traffic simulations on heterogeneous CPU-GPU hardware* (2021).** Custos relativos entre CPU/GPU em modelo especializado. https://www.sciencedirect.com/science/article/pii/S1877050921007985
- **[F31] SimGrid — models.** Inspiração para eventos de término e compartilhamento de recursos sem simular cada microinstrução. Não é motor de cidade. https://simgrid.org/doc/latest/Models.html
- **[F32] Factorio, Friday Facts #416 — Fluids 2.0 (2024).** Alerta: às vezes otimização e mudança de física/gameplay aparecem misturadas; no IndexCities isso exige decisão de produto. https://www.factorio.com/blog/post/fff-416
- **[F33] Factorio, Friday Facts #322 — New Particle System (2019).** Caso em que tirar entidades leves do sistema genérico evitou custo e buscas desnecessárias. https://www.factorio.com/blog/post/fff-322

**Critério para novas rodadas:** priorizar novos relatos com perfil reproduzível, implementação aberta, condições de hardware e descrição clara das simplificações. Continuar incluindo histórias de fracasso, não somente “x vezes mais rápido”. Não usar números de literatura como garantia de desempenho do IndexCities.

## Conclusão — o que eu recomendo para o IndexCities

**Recomendação central:** construir o Simulation Core aprovado com **processamento orientado a mudanças e a eventos, cálculo de intervalos comprovadamente equivalentes e estado individual persistente**. Não começar pelo paradigma “um update por objeto a cada frame”, e também não transformar toda a cidade num event bus sofisticado por antecipação.

Minha ordem de preferência **como investigação técnica, não plano obrigatório de entregas**:

1. **Fundação barata e observável:** relógio de simulação separado da renderização, IDs persistentes, scheduler simples, agentes adormecidos e despertados pelos gatilhos corretos, economia/mobilidade em domínios claros; métricas desde cedo. Isso se apoia diretamente na ARCHITECTURE já vigente.
2. **Otimização mais promissora:** rotina e economia calculadas **quando mudam ou chegam a seu próximo limite**, não por milhares de consultas vazias. Testar conservação de dinheiro, estoque e sequência causal.
3. **Maior investigação científica própria:** **mobilidade individual com estados temporais e processamento adaptativo por interações reais**. Trânsito livre usa eventos/intervalos enquanto válidos; cruzamentos, filas e conflitos exigem detalhe suficiente para preservar as regras físicas. Estudar trechos relativos e certificados cinemáticos como experimentos opcionais de alto potencial — não presumir que funcionam.
4. **Depois de medir:** índices de dependentes, replanejamento de rotas eficiente, dados compactos/SoA localizados, lotes/threads apenas em tarefas independentes.
5. **Não adotaria neste momento:** ECS total, GPU como arquitetura central, rollback otimista, cidade dividida em servidores, múltiplos calendários ou substituição de veículos por fluxos agregados.

**Minha aposta fora da caixa mais valiosa:** em vez de fazer a CPU perguntar continuamente “onde está cada veículo?”, fazer o motor saber “**qual é a próxima ocasião em que a interação entre veículos pode mudar?**” e executar detalhes extras somente nessas ocasiões. Aplicar raciocínio semelhante à economia (“quando a escassez pode mudar a decisão?”), com invalidação robusta. A ideia é inspirada em A/B Street, Factorio e KDS, mas a combinação **não foi comprovada para todas as regras do IndexCities**; é justamente a hipótese de maior retorno a testar.

O limite honesto: cidades densas com incontáveis interações reais custarão CPU. Não existe garantia de aceleração arbitrária sem custo. O que pode diferenciar o IndexCities é medir e **evitar o trabalho que não acrescenta nenhum acontecimento verdadeiro**, preservando todo o trabalho que acrescenta.

**Status final:** conclusão da IA, **PENDENTE de revisão humana**. Não registrar como decisão da SPEC/ARCHITECTURE; avaliar conforme resultados e decisão do responsável.
