# Pesquisa transversal — técnicas de outros domínios para acelerar uma cidade real

> **Revisão humana: PENDENTE.** Pesquisa nova da IA em **2026-10-09**, solicitada pelo responsável. Este arquivo **não é requisito aprovado, decisão arquitetural nem autorização de implementação**.
>
> **Propósito:** desafiar a pesquisa centrada em jogos e trânsito usando **simulação molecular, física, redes, sistemas de regras, bancos de dados, sistemas distribuídos, robótica, biologia computacional, jogos de física e compiladores**.
>
> **Fonte do produto:** [SPEC](../SPEC.md), em especial [tempo de jogo](../SPEC.md#tempo-de-jogo), [trânsito detalhado](../SPEC.md#comportamento-detalhado-do-tráfego) e economia. **Fonte estrutural:** [ARCHITECTURE](../ARCHITECTURE.md). Documento complementar e contexto anterior: [Pesquisa principal do motor](simulation-engine-performance-research.md), [escala/calendário](simulation-scale.md) e [hub](../EXPLORATION.md).
>
> **Critério inegociável:** pause/1/2/3 realmente executam todos os acontecimentos da cidade. SIMs, empresas, veículos, viagens, estoque, dinheiro, emprego e interações continuam verdadeiros mesmo sem câmera. Alterar o cálculo é permitido apenas quando preserva o modelo físico/econômico da SPEC e a ordem causal. Sem promessas de taxa de aceleração antes de medidas.

## Resumo executivo: o que descobrimos de realmente novo

**Tese reforçada e ampliada:** reduzir três formas distintas de trabalho:

1. **Avaliação inútil:** não perguntar incessantemente a cada entidade se precisa agir; use gatilhos e próximo evento.
2. **Descoberta repetida de interações:** não refazer buscas de vizinhos, dependentes, fornecedores e colisões sem mudança que invalide os resultados.
3. **Integração numérica desnecessariamente fina:** quando for possível estabelecer matematicamente um intervalo em que todas as hipóteses do modelo permanecem válidas, avançar até o **próximo limite causal**; usar cálculo detalhado quando o intervalo seguro se encerra.

**Nova hipótese mais promissora:** um **horizonte de validade causal** por trecho/atividade, derivado de limites físicos e de dependências, reunindo conceitos de LAMMPS, SUNDIALS e dinâmica molecular orientada por eventos. **Esta combinação é proposta nossa, não técnica comprovada para uma cidade**; veja seção 3.

**Grande descoberta negativa:** não basta “colocar o trânsito em outro motor rápido”. O SUMO demonstra quantitativamente que **consultar estado veículo a veículo via interface** pode custar várias vezes o tempo do motor isolado. A fronteira entre mobilidade e economia precisa ser medida e, se possível, acessada de forma compacta e em lote.

**Grande descoberta de qualidade:** algumas técnicas de processamento adiado **reordenam acontecimentos**; Drools documenta esse risco. Portanto: agrupar trabalho computacional não é autorização de agrupar consequências e comprometer sua ordem.

## 1. Evidências cruzadas — projetos que não são city builders

| Projeto/domínio | Como funciona, segundo a fonte | Ideia transferível | Por que NÃO copiar integralmente | Prioridade |
| --- | --- | --- | --- | --- |
| **LAMMPS** (física molecular) [X01,X02] | Listas espaciais de partículas próximas, com margem extra (*skin*), só são reconstruídas quando deslocamento justifica. | **Índice de vizinhos com validade demonstrável**, para pedestres/veículos por faixa ou espaço. | Átomos não precisam obedecer sinalização, estacionamentos, decisões e prioridade viária. | **Muito alta** |
| **EDMD de Smallenburg** (física de esferas duras) [X03] | Agendamento de **próximas colisões**, índices de vizinhança; repositório expõe variantes com diferentes calendários de eventos. | **Próxima interação em vez de próximo frame**; limitar eventos por entidade. | Esferas duras uniformes são matematicamente mais previsíveis que carro freando, mudando faixa e cedendo passagem. | **Muito alta como hipótese** |
| **SUNDIALS CVODE** (integração de equações) [X04] | Integra estados contínuos e detecta **cruzamentos de limiar** usando funções de raiz definidas pelo modelador. | Determinar primeiro instante de **estoque acabar, produção completar, reserva atingir limite**, quando a fórmula e premissas o permitem. | Métodos são numéricos, às vezes aproximados, e nem todos os limiares são detectáveis com as mesmas técnicas; não aplicar cegamente a transações discretas. | **Alta, local** |
| **Box2D** (física de jogos) [X05] | Objetos ou ilhas paradas dormem; colisões/alterações acordam corpos afetados. Usa IDs e estruturas data-oriented. | **Dormir e despertar grupos fisicamente estáveis**; manter identidade sem Update constante. | Simulação de corpos rígidos não modela decisões nem todas as regras de tráfego. | **Alta** |
| **ns-3** (redes de computadores) [X06] | Agenda temporal precisa; cancelamento de evento pode ser barato mas consome memória se não removido; vários schedulers implementados e benchmark próprios. | **Scheduler leve, desempate explícito, cancelar/reagendar sem perder causalidade**; medir estrutura antes de escolher. | Mensagens de rede não reproduzem automaticamente continuidade física. | **Muito alta** |
| **SimPy** (logística/processos) [X07] | Eventos de timeout, interrupção, espera **AnyOf/AllOf**, IDs para desempatar eventos no mesmo tempo. | Empresas/obras/viagens podem aguardar **condições reais** como insumo + trabalhador + doca, sem polling. | Objetos Python/generators individuais não são boa fundação de desempenho para milhões de SIMs em C#. | **Alta conceitual** |
| **Drools Phreak** (regras empresariais) [X08,X09] | Redes de correspondências de regras, avaliação **lazy** e propagação em lote quando entradas mudam. A documentação avisa que certas semânticas mudam sem propagação imediata. | **Reavaliar somente decisões cuja condição pode ter mudado**. | Não montar um motor de regras global nem postergar pagamento/compra com efeito sobre terceiros. | **Alta como algoritmo incremental** |
| **Materialize / Differential Dataflow** (banco de dados) [X10] | Mantém *views* e junções **incrementalmente**, atualizando resultados afetados por alterações. | Ofertas de empregos, produtos acessíveis, rankings de oportunidades e painéis derivados por deltas. | Views são **derivadas**, não autoritativas; reproduzir um banco distribuído seria custo inútil. | **Muito alta para decisões/consultas** |
| **SQLite** (transações) [X11] | Atomicidade e escrita serializável; uma transação altera vários registros de forma coerente. | Transferir dinheiro + mercadoria + reserva como **uma confirmação causal**. | Não exige SQLite em runtime; ideia de transação local e disciplina de confirmação. | **Muito alta** |
| **FoundationDB** (banco distribuído) [X12] | Testa lógica real em tempo virtual determinístico e injeta falhas repetíveis. | **Executar o mesmo cenário integral com seeds e comandos reproduzíveis** para provar que otimização não remove efeitos. | Os números publicados de tempo acelerado testam rede/armazenamento, não uma cidade e seu trânsito. | **Muito alta para testes** |
| **Noita** (outro estilo de jogo: areia/física) [X13,X14] | Dev talk descreve áreas modificadas (*dirty regions*), blocos e processamento paralelo por padrão espacial. | **Particionar mudanças efetivas**, não recalcular área estável; avaliar conflitos de fronteira na paralelização. | A estratégia de mundo parcialmente descarregado **não preserva automaticamente acontecimentos fora de câmera** e viola a SPEC se copiada como simulação. | **Alta apenas para invalidação/partições** |
| **RVO2 / ORCA** (robótica/multidões) [X15] | Algoritmo local para evitar colisões entre muitos agentes; há versão **C#** aberta. | Inspiração para **pedestres em locais congestionados**, com índice de vizinhos e restrições de movimento. | ORCA muda a política de desvio dos agentes e opera por passos; precisa compatibilizar travessias apenas em faixas, semáforos e comportamento aprovado. | **Média** |
| **FLAME GPU 2** (biologia e agentes científicos) [X16,X17] | Agentes individuais na GPU, mensagens espaciais, nascimento/morte; benchmarks reproduzíveis de boids e segregação. | Pensar em **cálculos homogêneos** e comunicação localizada, se hotspot real justificar GPU. | Testes em GPU de HPC e modelos sociais/biológicos simples não são testes de economia + físico do IndexCities; licença merece avaliação própria. | **Média** |
| **GAMA Platform** (modelagem social/geográfica) [X18] | Agentes geográficos/espaciais com inspeção e simulação de múltiplos domínios. | Referência para **estados individuais observáveis** e verificação de modelos por setor sem perder o todo. | Linguagem/Java/plataforma completa aumentariam dependências; sem benchmark equivalente, não recomendar migração. | **Média** |
| **ROSS** (supercomputadores/distributed simulation) [X19] | Paraleliza eventos com **Time Warp** e rollback, inclusive computação reversa, para milhões de modelos lógicos. | Reforça que paralelismo de eventos tem **custo causal concreto**. | Reverter compra, estoque, salário, rota e efeitos em cadeia pode ser muito mais caro que executá-los em ordem; não recomendar como núcleo. | **Baixa para adoção** |
| **Orleans** (atores virtuais C#) [X20] | Ativa entidades sob demanda, desativa em ociosidade e usa lembretes. | Confirma valor do **agente persistente sem objeto ativo** o tempo todo. | Overhead de runtime distribuído, IO e timers de parede do relógio; não usar em simulação local temporal. | **Baixa para adoção** |
| **SimGrid** (redes e infraestrutura) [X21] | Modela atividades concorrentes que compartilham recursos, considerando capacidade e duração. | Capacidade compartilhada como **restrição para próxima conclusão** de produção, energia e serviço, quando compatível com especificação. | Modelagem de rede/CPU por compartilhamento não substitui caminhões, estoques ou trabalho real. | **Média** |
| **OpenTTD** (logística de outro gênero) [X22] | Guarda pacotes de carga com origem e tempo compartilhados; caminhos de veículos têm cache. | Compartilhar **metadados de cargas homogêneas**, sem duplicar origem/rota, e validar cache de caminhos. | Agregar cargas não autoriza tornar SIMs um pacote anônimo, ou pular transferência individual de propriedade quando necessária. | **Média** |
| **Performance Fish** (mod de RimWorld) [X23] | Substitui trechos específicos por implementações melhores, promete funcionalidade equivalente e permite desativar patches. | **Otimização reversível por hotspot**, com comparação antes/depois sem reescrever núcleo. | Não serve como benchmark do nosso jogo; patches dependem da arquitetura do RimWorld. | **Alta como disciplina** |
| **.NET / PGO** (compiladores e runtime) [X24] | JIT com otimizações orientadas por perfil e decisões distintas para loops quentes. | Medir otimização com binário de release, JIT aquecido e runtime real do Godot/C#. | Ganhos de compilador não compensam algoritmo ruim nem têm mesma magnitude em todos os loops. | **Alta operacional** |
| **SUMO TraCI / Libsumo** (fronteira de integração) [X25] | Documentação mede custo elevado de ler cada posição via comunicação externa; biblioteca local evita overhead de socket. | **Trocas de dados compactas, em lote, próximas do Core**; medir a interface inteira, não o kernel isolado. | Não implica incorporar SUMO ou trocar por C++; mostra risco de conexão entre dois motores. | **Altíssima** |
| **RVO2 / tráfego robótico** [X26] | Algoritmos globais de planejamento livre de colisão são estudados em armazéns e podem ser caros em áreas densas. | Buscar conflitos **onde surgem**, não planejar toda a cidade como um único problema multiagente ótimo. | Robôs coordenados não equivalem a motoristas e pedestres autônomos em cidade aberta. | **Média** |

## 2. Lições negativas tão importantes quanto os ganhos

### 2.1. “Lazy” muda a ordem se for mal aplicado

Drools documenta casos em que uma regra de consulta com propagação adiada passa a responder diferente conforme a ordem de fatos; requer propagação imediata em casos específicos [X09]. **Regra para nossa hipótese:** adiar **cálculo de proposta** é possível; adiar **efeito que altera oportunidade de terceiros** exige provar mesma ordem causal.

Exemplo: o anúncio de uma vaga pode ser indexado tardiamente apenas se nenhum SIM legitimamente procurar antes da atualização; no primeiro compromisso de contratação, disponibilidade, critérios e tempo devem corresponder ao estado real.

### 2.2. Um kernel 88x mais rápido não significa um jogo 88x mais rápido

O caso MOSS da pesquisa anterior é sólido **para tráfego isolado e hardware específico**, mas o custo de atravessar a fronteira entre motores pode dominar. Em exemplo oficial SUMO, para cenário Bologna de 9.000 veículos × 5.000 passos, a execução sem TraCI levava **8 s**; com consulta simples por posição via TraCI **90 s**; subscrições **42 s** [X25]. **Não extrapolar esse número ao C# ou a uma GPU**: mostra a ordem de grandeza de um risco de arquitetura.

**Consequência:** uma versão GPU só faria sentido com interface física em lote, comandos sincronizados e número de transferências medido; o Core C# precisa continuar autoridade da economia. Se ônibus, abastecimento, estacionamento e pedestres exigirem ida/volta ao dispositivo a cada evento, ganho pode evaporar.

### 2.3. Não resolver desempenho apagando a simulação fora da câmera

Noita e Cataclysm: DDA têm estratégias de simular preferencialmente uma área ativa. Isso é útil nos respectivos jogos, mas **não é compatível por padrão com a cidade** que deve continuar física e economicamente existente onde o jogador não olha. Aproveitar somente **dirty regions por mudança real, particionamento e atualização local**, nunca “a câmera decide se o engarrafamento existe”.

### 2.4. Não confundir passo adaptativo com fidelidade automaticamente mantida

LAMMPS avisa que reconstruir vizinhos com frequência insuficiente omite interações. SUNDIALS indica que detecção de raízes por mudança de sinal pode perder eventos que não a produzam [X02,X04]. Em IndexCities:

- qualquer horizonte precisa ser **conservador** e invalidável;
- colisões, ultrapassagens, travessia, faixas, capacidade, fila e limite de estoque têm gatilhos explícitos;
- quando não for possível provar a segurança, **recuar para processo mais detalhado**, não fingir que a condição não aconteceu.

### 2.5. Data-oriented não é automaticamente grátis

O Box2D mostra IDs com layout de memória otimizado; FLAME GPU mostra espaço de paralelismo; mas a diferença vem dos **dados tocados**, das relações e da sincronização. Evitar migrar todas as entidades para framework novo ou carregar todo o mundo para GPU antes de perfil e comparação. Limites de licença e portabilidade devem ser vistos **antes de incorporar código externo**, não no meio.

## 3. Hipótese técnica inédita para o projeto: horizonte conservador de validade causal

> **Revisão humana desta subseção: PENDENTE.** Proposta de engenharia, não algoritmo validado, nem alteração das regras de direção/física. Faz conexão entre estudos independentes [X01–X04]. **Não chamar de solução pronta.**

### 3.1. Ideia em linguagem direta

Em vez de perguntar de novo *“onde estão todos os agentes e quem pode colidir com quem?”* a cada atualização:

1. Cada trecho/atividade conhece os **agentes reais relevantes** e condições que tornam seu próximo estado previsível.
2. Calcula **até qual instante é seguro reutilizar essas condições** sem ignorar acontecimento.
3. Agenda o **primeiro momento em que algo poderia alterar o resultado**; também assina mudanças externas capazes de invalidá-lo antes.
4. Enquanto válido, posição/estado podem ser obtidos pela lei física/operacional do modelo, sem polling constante.
5. No limite ou em invalidação, executa **o acontecimento físico verdadeiro**, atualiza agentes/índices e calcula novo limite.

É possível que o ganho venha **mais de deixar de descobrir vizinhos novamente** do que de pular física; por isso medir as duas coisas separadamente.

### 3.2. Exemplo concreto: caminhão seguindo carro

- Temos distância real entre eles, velocidade, frenagem admissível, estado de faixa e eventos de entrada/interseção.
- Sob hipótese muito restrita de velocidades constantes e sem mudanças externas, **aproximação de tempo até alcançar** é a distância útil dividida pela velocidade de aproximação, se esta for positiva. **Isso não inclui automaticamente distância de frenagem nem comportamento detalhado do jogo.**
- Para um horizonte **conservador**, usar limites superiores de aproximação e espaço de segurança com modelo de aceleração/frenagem autorizado, além da possibilidade de terceiros ingressarem na faixa.
- Uma frenagem ou mudança de faixa do líder às 10:07 invalida o cálculo, ainda que a colisão estivesse prevista para 10:10.
- Desviar, atravessar, parar e ocupar espaço continuam acontecimentos reais em qualquer câmera. Pedestres, emergência e prioridade de conversão requerem tratamento específico.

**Problema científico genuíno:** com milhares de carros reagindo uns aos outros, os horizontes podem ser quase zero; nesses casos o scheduler pode custar **mais** que passos regulares, e o algoritmo deve saber voltar a atualização de passo sem mudar física.

### 3.3. Exemplo equivalente de produção

- A fábrica real tem insumos, funcionários presentes, capacidade energética, tempo de produção e espaço disponível; conhece o instante até o próximo lote ser concluído **se nada mudar**.
- Agenda conclusão física/lógica de cada lote conforme regras da SPEC e seu estoque. Falta de energia, saída de funcionário, mudança de insumo ou parada cancelam a previsão.
- Um SIM ou empresa compra somente estoque que **já existe**; dinheiro e mercadoria mudam de dono de forma consistente, na ordem correta.
- Produção **não** é “+100 produtos por dia” inventados; a técnica representa as transições reais que se dão no intervalo.

### 3.4. Quando é arriscado ou perde eficiência

- Faixas densas e instáveis; mudança de faixa contínua; atravessamentos irregulares; emergência em andamento.
- Tráfego cuja física depende de variável não observada ou aproximação sem limites conservadores.
- Cascatas: um acidente altera dezenas de rotas, ocupações, entregas e decisões em tempo muito curto.
- Granularidade fina com tantos cancelamentos que o heap/calendar domina a CPU.

**Resultado honesto:** horizonte conservador é hoje a hipótese transversal **mais interessante**, mas ainda precisa demonstrar economia líquida com interações sem sacrificar regras. Não existe prova de que toda física de veículos admita solução analítica útil.

## 4. Segunda hipótese: mundo indexado por dependências e mudanças

### 4.1. Inspiração: manutenção incremental de banco de dados

Em lugar de cada SIM varrer todas as vagas e estabelecimentos, manter um índice de:

- vagas reais com condições vigentes e localização;
- postos/comércios com categoria, produto e disponibilidade derivados do estoque real;
- obras aguardando lotes, caminhões, trabalhadores ou energia;
- agentes/rotas dependentes de acessos, cruzamentos, serviços e energia;
- eventos cuja validade depende de determinada condição ou versão de recurso.

Quando muda o estoque de um mercado, atualizam-se **os índices e interesses efetivamente afetados**. Mas cada SIM continua escolhendo autonomamente e fazendo viagem/compra real; índice não pode se tornar **agente agregado fictício**.

**Risco:** índice precisa ser reconstruível a partir da verdade; atualização atrasada que muda competição viola a especificação. Preferir índices pequenos e de propósito concreto a um megaframework de regras.

### 4.2. Inspiração: física dorme, mas desperta por dependência

Box2D dorme corpos parados e os desperta por colisão/mudança. Analogia válida: empresa com produção bloqueada espera de insumo, cidadão dormindo aguarda rotina ou emergência; a identidade e o estado permanecem consultáveis. **Nem todo ciclo de envelhecimento, dívida ou consumo pode dormir indefinidamente**: gatilhos temporais e causais precisam existir.

### 4.3. Inspiração: condições combinadas do SimPy

Uma obra pode esperar **(material entregue E equipe disponível E acesso físico)**, e reagir a qualquer mudança dessas condições. Uma pessoa pode deixar de esperar quando **(transporte chega OU desiste pelos critérios aprovados)**. Os operadores “E/OU” são uma linguagem para pensar dependências; não uma proposta de criar máquina nova de workflows.

## 5. Terceira hipótese: ordem causal separada da computação em lote

**Princípio proposto:** tarefas independentes podem ser calculadas juntas, mas efeitos sobre estado disputado exigem revalidação/commit ordenado no tempo simulado.

Aplicações:

- Cálculo de “qual supermercado o SIM prefere” pode ser paralelo se lê snapshot coerente.
- Compra precisa confirmar presença real, estoque, preço, dinheiro disponível, logística de acesso e ordem temporal.
- Atualizar posição de milhares de veículos em espaço sem conflito pode ser SIMD/GPU, mas reservar cruzamento e alterar fila/ocupação requer sincronização causal.
- Em múltiplos acontecimentos no mesmo instante, manter regra de desempate explícita e verificável, inspirada no SimPy [X07] — **não inventar critério de prioridade de compra novo por implementação**.

**Importante:** tal abordagem não substitui a SPEC nem decide comportamento simultâneo indefinido; é só método de organizar trabalho técnico dentro das regras já decididas.

## 6. O que esta rodada mudou nas prioridades da pesquisa anterior

| Hipótese | Avaliação antes | Avaliação agora | Por quê |
| --- | --- | --- | --- |
| GPU microscópica | Passou de distante a alternativa importante na rodada MOSS | **Continua alternativa, não fundamento obrigatório** | SUMO evidencia risco enorme em comunicação; não sabemos proporção CPU→GPU para uma cidade inteira. |
| Eventos | Forte como base geral | **Ainda mais forte, mas com riscos explicitados** | ns-3, SimPy e EDMD trazem detalhes reais de agenda, desempate e cancelamentos. |
| Busca espacial / colisões | Tema de trânsito | **Pode ser gargalo prioritário independente de CPU/GPU** | LAMMPS e Box2D mostram listas espaciais e dormir objetos; custo de achar interações pode dominar. |
| Economia baseada em regras | Scheduler de transições | **Incrementalização de consultas e dependências é oportunidade separada** | Materialize e Drools, desde que propagação respeite ordem causal. |
| Calcular intervalos | Ideia promissora e genérica | **Hipótese mais técnica: limite matemático + invalidação por dependência** | SUNDIALS, EDMD, margens espaciais; abordagem híbrida potencialmente demonstrável para alguns estados. |
| Paralelismo | Depois de medir | **Continua depois de medir** | ROSS confirma complexidade real de reversões; Noita mostra trade-offs de fronteiras. |
| Validação | Testes de invariantes e comparação CPU/GPU | **Reprodução sistemática vira critério central de escolha** | FoundationDB e SQLite: estado compartilhado + ordem + falhas precisam ser confiáveis. |
| ECS completo | Adiado | **Adiado** | Box2D inspira handles e dados compactos sem exigir ECS global. |
| Otimizar API Core↔Host | Importante | **Potencial decisivo para não perder desempenho** | SUMO TraCI/Libsumo isola custo de interface e chamadas por entidade. |

## 7. O que eu não recomendaria

1. **Trocar o Simulation Core pelo SUMO, FLAME GPU, GAMA, SimPy, Drools, FoundationDB, Orleans ou ROSS.** São referências de conceitos, não soluções plug-and-play para a SPEC, e criariam alto custo de integração.
2. **Descartar execução de SIMs afastados da câmera** à maneira de jogos com *reality bubble*.
3. **Aplicar IA neural para prever viagens reais** sem demonstrar equivalência; o estudo de fast-forward da pesquisa anterior mediu desvios.
4. **Criar um banco de dados ou regra engine por agente.** Use índices C# locais quando o gargalo real justificar.
5. **Converter rede viária inteira em busca global ótima de colisões de robôs.** Custo pode crescer explosivamente; resolver interações localizadas.
6. **Mudar física/causalidade por conveniência de CPU, GPU, scheduler ou renderizador** sem decisão explícita do responsável e atualização prévia da SPEC.
7. **Criar ECS completo/threads/server/shaders por antecipação.** O Core C# independente da SceneTree já aprovado preserva alternativas futuras.

## 8. Experimentos verificáveis, sem exigir fase de POC por subsistema

*Observação:* estes são **experimentos técnicos opcionais, de baixo escopo, realizados conforme necessidade na implementação integrada**; nunca substituem o produto oficial.

| Pergunta discriminante | Comparar | Critério de rejeição |
| --- | --- | --- |
| Descobrir vizinhos custa mais que mover veículos? | Pesquisa total vs índice por faixa/área com limite conservador de validade | Ignorar a entrada de veículo/pedestre ou elevar custo de atualização de índice acima do ganho. |
| Agendar próximas interações é melhor que passo curto? | CPU física de referência vs eventos com cancelamentos em tráfego livre, urbano e congestionado | Alterar posição, capacidade, fila ou chegada; ou eventos crescerem muito em trânsito instável. |
| Motor separado mata o ganho? | Tempo do kernel isolado vs **tempo integrado** Core↔mobilidade, com chegada/saída/carga/ônibus | Ganho desaparecer por transferências, marshaling, atualização visual ou sincronização. |
| Índices econômicos reduzem custo? | Todas as empresas pesquisadas por SIM vs índice de oportunidades com invalidação real | Decisão usar dado vencido; vaga/estoque duplicado; erro de causalidade. |
| Calcular intervalos funciona? | Avanço passo a passo vs solução de intervalos **até próximo evento-limite**, sob perturbações | Pular limiar, aniversário, conta, entrega, falta de energia, mão de obra ou emergência. |
| SIMD/SoA/.NET PGO ajudam? | Build release na mesma máquina, perfil aquecido, simulação headless, execução integrada | Diferenças insignificantes frente à maior complexidade/manutenção. |
| Paralelismo muda comportamento? | Mesma seed e comandos, com/sem tarefas concorrentes | Saldo, estoque, fila, propriedade, emprego ou resultado de disputa incoerentes. |

**Métricas por cenário:** segundos de tempo simulado por segundo real; eventos/segundo real; porcentagem de **agentes ativos**; número de vizinhos consultados por veículo; replanejamentos, cancelamentos, recomputações de índice, atrasos p95/p99; %CPU em movimentação, seleção, economia, renderização/transferência, GC; número de invalidações em cascata; integridade do dinheiro fixo e cargas individuais.

**Casos adversariais importantes:** cruzamento lotado; carro muda de faixa diante de ônibus; ambulância atravessa cidade; via fecha enquanto carga a ocupa; energia cai no meio de produção; dois SIMs chegam ao último produto; mudança súbita de preços/emprego; todo mundo sai do trabalho ao mesmo tempo; cidade muda enquanto simulação 3 está ativa. Tudo isso **dentro e fora da câmera**.

**Teste de reprodução essencial:** usar seeds, saves e sequência de comandos para executar comparação entre motor de referência e técnica nova, mantendo **causalidade e efeitos observáveis**. Atenção: paralelismo e float podem impedir igualdade bit a bit entre plataformas; isso não elimina a exigência de invariantes e resultados do produto coerentes. Se diferença alterar o comportamento, precisa ser explicada, não marcada como “erro aceitável de otimização” automaticamente.

## 9. Biblioteca de fontes verificáveis desta rodada

Todos os links abaixo apontam para repositórios oficiais, documentação de mantenedores, artigos originais ou publicações de autores. Fontes de vídeo e comunidade estão identificadas separadamente. Não copiar ganhos entre modelos/hardwares; não afirmar que uma API/repo foi integrada ou benchmarkado pelo IndexCities.

### Ciência física, métodos numéricos e simuladores gerais

- **[X01] LAMMPS — neighbor lists, código/documentação dos desenvolvedores:** https://docs.lammps.org/Developer_par_neigh.html — índices de pares e armazenamento contíguo.
- **[X02] LAMMPS — regras de rebuild/invalidação e risco de perder interações:** https://docs.lammps.org/stable/neigh_modify.html — margem *skin*, gatilho por deslocamento e cautelas.
- **[X03] F. Smallenburg, *Efficient event-driven simulations of hard spheres* (2022), código e variantes:** https://github.com/FSmallenburg/EDMD — https://doi.org/10.1140/epje/s10189-022-00180-8
- **[X04] SUNDIALS — CVODE rootfinding, limites e detecção de cruzamentos:** https://sundials.readthedocs.io/en/v6.1.0/cvode/Mathematics_link.html
- **[X05] Box2D — simulação/ilhas adormecidas/IDs:** https://github.com/erincatto/box2d/blob/main/docs/simulation.md
- **[X06] ns-3 — eventos, diferentes agendas, cancelamentos e benchmark de filas:** https://github.com/nsnam/ns-3-dev-git/blob/master/doc/manual/source/events.rst
- **[X07] SimPy — eventos e desempate cronológico:** https://simpy.readthedocs.io/en/latest/api_reference/simpy.events.html — https://github.com/simpx/simpy/blob/master/docs/topical_guides/time_and_scheduling.rst
- **[X21] SimGrid — cálculo de duração sob recursos compartilhados:** https://simgrid.org/doc/latest/Models.html

### Bancos de dados, lógica incremental e confiabilidade

- **[X08] Drools — PHREAK, avaliação lazy, propagação em lote:** https://docs.jboss.org/drools/release/latest/drools-docs/drools/rule-engine/index.html
- **[X09] Drools — casos em que lazy muda ordem de regras e deve ser forçado immediate:** https://docs.drools.org/6.5.0.Final/drools-docs/html/ch07.html
- **[X10] Materialize — arrangements e incremental view maintenance:** https://materialize.com/docs/fundamentals/concepts/arrangements/
- **[X11] SQLite — isolamento e escrita serializável:** https://www.sqlite.org/isolation.html
- **[X12] FoundationDB — simulação determinística e falhas reproduzíveis:** https://apple.github.io/foundationdb/testing.html — https://apple.github.io/foundationdb/engineering.html
- **[X20] Microsoft Orleans — coleta de agentes ociosos:** https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/activation-collection
- **[X24] Microsoft .NET — otimização com perfil de execução (PGO):** https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation
- **[X27] Jepsen — linearizability, operações atômicas e ordem observável:** https://jepsen.io/consistency/models/linearizable

### GPU, robótica, jogos de outros gêneros e integração

- **[X13] Noita — palestra GDC original (YouTube, Petri Purho):** https://www.youtube.com/watch?v=prXuyMCgbTc — [descrição editorial da palestra GDC](https://www.gamedeveloper.com/design/video-understanding-the-remarkable-tech-and-design-of-i-noita-i-). A descrição e materiais associados sustentam pesquisa sobre otimização de áreas; detalhes do algoritmo específicos requerem o vídeo.
- **[X14] Noita — postagem com transcrição informal de perguntas ao desenvolvedor:** https://benlau6.github.io/notes/noita/ — tratar como secundária, conferir falas contra original.
- **[X15] RVO2 — algoritmo de evasão de multidões e repositório C#:** https://github.com/snape/RVO2-CS — https://gamma-web.iacs.umd.edu/RVO2/
- **[X16] FLAME GPU 2 — agentes em GPU, repo e licença:** https://github.com/FLAMEGPU/FLAMEGPU2
- **[X17] FLAME GPU — ensaio reproduzível com comparação de frameworks:** https://github.com/FLAMEGPU/ABM_Framework_Comparisons — https://developer.nvidia.com/blog/fast-large-scale-agent-based-simulations-on-nvidia-gpus-with-flame-gpu/
- **[X18] GAMA — plataforma geográfica de agentes:** https://github.com/gama-platform/gama
- **[X19] ROSS — execução distribuída de eventos e rollback:** https://ross-org.github.io/ROSS-docs/docs/html/ — https://github.com/ROSS-org/ROSS
- **[X22] OpenTTD — tipo de dados CargoPacket e cache do YAPF:** https://docs.openttd.org/source/df/dd6/structCargoPacket — https://docs.openttd.org/source/d4/d54/yapf_8h
- **[X23] Performance Fish — mod RimWorld voltado a eficiência funcional equivalente:** https://github.com/bbradson/Performance-Fish
- **[X25] SUMO — custos numéricos da interface TraCI e alternativa libsumo:** https://sumo.dlr.de/docs/TraCI/ — https://sumo.dlr.de/docs/Libsumo.html
- **[X26] CBS/MAPF — planejar conflitos robóticos, paper original:** https://doi.org/10.1016/j.artint.2014.11.006
- **[X28] Unity — documentação de critérios de otimização C#/DOTS (atualizada em setembro/2026):** https://learn.unity.com/article/c-and-dots-specific-performance-tips

### Outros relatos, palestras e advertências

- **[V01] Mike Acton, GDC — custo de abstrações e foco nos dados:** https://gdcvault.com/play/1012200/Three-Big-Lies-Typical-Design
- **[V02] Mike Acton, CppCon 2014 (YouTube):** https://www.youtube.com/watch?v=rX0ItVEVjHc
- **[V03] Noita, GDC (YouTube):** https://www.youtube.com/watch?v=prXuyMCgbTc
- **[C01] Unity Discussions — usuário tentando desenhar 1 milhão de entidades e obtendo desempenho baixo:** https://discussions.unity.com/t/trying-to-have-30-fps-with-1-million-entities/860725 — anedótico, não benchmark do framework.
- **[C02] Comunidade Noita — dificuldade de conflitos de fronteira em física multithread:** https://www.reddit.com/r/noita/comments/1vsflcl/ive_been_trying_to_make_my_own_falling_everything/ — relato de implementação, não fonte da arquitetura oficial Noita.
- **[C03] GitHub Cataclysm: DDA — realidade localizada fora da bolha:** https://github.com/CleverRaven/Cataclysm-DDA/wiki/FAQ-from-Discord — caso negativo para a SPEC, não modelo para copiar.
- **[C04] RimWorld Performance Fish — código verificável; comunidade usa diversas combinações de patches:** https://github.com/bbradson/Performance-Fish — evitar quantificar ganho sem mesmo save/hardware.
- **[C05] Oxygen Not Included, notas oficiais de março/2026:** https://kleiforums.com/game-updates/oni-alpha/719533-r2701/ — exemplos de que mudanças pequenas na física (troca de calor) podem alterar conservação e gerar bugs; **não** assumir algoritmo de alta performance a partir de patch notes.

**Escopo real da investigação:** fontes oficiais e vários casos de fórum/vídeo foram consultados; **não** houve execução local dos programas, reprodução independente de benchmarks externos, análise de transcrições completas de todos os vídeos ou prova de equivalência física de qualquer técnica. Propostas próprias são explicitamente identificadas.

## Conclusão — o que eu recomendo de acordo com esta rodada

> **Revisão humana: PENDENTE.** Esta conclusão é julgamento técnico da IA, **não decisão oficial**.

**Eu NÃO substituiria a direção do Simulation Core C# já aprovada, nem escolheria GPU como arquitetura básica.** A nova pesquisa sugere que existe algo potencialmente melhor do que “só eventos + GPU”: **um núcleo causal que conhece as dependências e os limites de validade de cada cálculo**.

Minha recomendação, em ordem de valor para o IndexCities:

1. **Fundação simples e correta:** estado individual persistente, agenda de eventos leve, transações econômicas coerentes, relógio único do mundo, render independente e métricas. Isso já cabe na ARCHITECTURE.
2. **Primeira otimização de alto potencial:** **índices de vizinhos e dependentes atualizados somente quando deixam de ser válidos**. Inspiração LAMMPS + Box2D + Materialize. É uma aposta mais transversal que escolher GPU para todo o trânsito.
3. **Aposta técnica fora da caixa:** **horizontes conservadores de causalidade** para trechos de movimento e atividades estáveis; avançar ao primeiro possível conflito, insumo faltante, conclusão ou alteração do mundo. Inspiração EDMD + SUNDIALS + ns-3. Só adotar onde a equivalência puder ser demonstrada; em locais caóticos, física detalhada com passos suficientes.
4. **Paralelismo local e GPU depois de verificar gargalo:** CPU primeiro, dados compactos e jobs; GPU de mobilidade continua alternativa séria da rodada MOSS se mostrar ganho **integrado**, não apenas no benchmark isolado.
5. **Exigir verificabilidade:** testes de invariantes, comparação de mesmas seeds/cidades, reprodução de condições extremas. O que garante a preservação de SIMs não é o nome do algoritmo; é a **demonstração de que nenhuma mudança real foi omitida**.

**A principal mudança de avaliação:** nosso problema não é unicamente “simular mais veículos por segundo”; pode ser sobretudo **detectar eficientemente o pequeno conjunto de relações que realmente muda a cada momento**, em trânsito e economia. Este é o ponto de encontro mais poderoso entre física molecular, bancos incrementais, simuladores de rede e motores de jogos.

**Ponto em aberto mais importante:** o custo para manter os limites de validade e seus cancelamentos em congestionamento real pode ser alto demais; se for, um algoritmo de passos CPU/GPU com índices de vizinhança vencerá. Não declarar a hipótese como solução até comparar em cenários integrados.

**Minha escolha hoje:** **modelo híbrido orientado por interações verificáveis + atualização física detalhada onde há conflito + economia incremental com transações reais**, dentro de um único Core. Sem prometer população/velocidade máxima antecipadamente, sem POCs de produto obrigatórias e **sem sacrificar SIMs ou acontecimentos**.
