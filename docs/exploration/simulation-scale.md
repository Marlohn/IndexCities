# IndexCities — Escala, performance e baselines da simulação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o conteúdo deste documento ainda não foi revisado integralmente pelo responsável e não pode ser tratado como decisão.
>

> **Status:** exploração ativa e referência de calibração — não é fonte de verdade.
>
> Concentra pesquisa sobre população, profundidade individual, limites técnicos, tempo de jogo e baselines empíricos usados para orientar protótipos e benchmarks. Números provisórios não viram requisitos sem promoção explícita para a SPEC.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---


## Reavaliação técnica da stack Godot + C#/.NET (2026-10-10)

> **Revisão humana desta seção: PENDENTE.** Avaliação e recomendação produzidas por IA, ainda não revisadas integralmente pelo responsável. **Não é nova decisão de produto nem de arquitetura.** A escolha **Godot como engine e C# como linguagem principal já está aprovada** na [SPEC](../SPEC.md#tecnologia); a separação entre Godot e núcleo de simulação C#/.NET já está aprovada na [ARCHITECTURE](../ARCHITECTURE.md). Este registro apenas avalia sua adequação e seus riscos diante do escopo atual.

### Veredito condicionado

**A recomendação atual é manter Godot + C#/.NET, sem migração preventiva.** A combinação é coerente com o produto planejado **porque o Godot ficará responsável pelo host, visualização 3D isométrica, input, cenas e interface**, enquanto a simulação econômica, social e de mobilidade tem estado e regras **independentes de objetos Godot**, como já estabelece a arquitetura. O conhecimento prévio do responsável em C#/.NET também reduz custo de aprendizado e desenvolvimento do núcleo.

Esta é uma **avaliação de adequação**, não uma comprovação de capacidade máxima. O repositório ainda não possui uma implementação integrada nem benchmarks próprios que provem desempenho com dezenas ou centenas de milhares de SIMs. A maior incerteza não é simplesmente o FPS: é quantos **dias/horas/eventos simulados corretos por segundo real** conseguimos processar na velocidade acelerada aprovada, preservando viagens, transações, consumo, produção e história individual.

### Evidência técnica e implicações para o IndexCities

| Tema | Evidência e leitura técnica | Consequência prática |
| --- | --- | --- |
| **C#/.NET e ferramentas** | Godot suporta C# com runtime .NET moderno e integração com IDEs externos como Visual Studio/Rider. A documentação oficial caracteriza C# como opção de bom equilíbrio entre produtividade e velocidade; cálculos intensivos, porém, exigem atenção a alocações e GC. | Boa aderência ao perfil do desenvolvedor e ao núcleo em C# puro; não presumir que qualquer código C# será rápido sem medir. |
| **Muitas instâncias visuais** | Godot documenta APIs de **Servers** de menor nível para contornar custo da SceneTree e **MultiMesh** para desenhar muitas instâncias em lotes. MultiMesh não realiza culling individual de cada instância; agrupamento espacial/visibilidade precisa ser considerado. | Representar somente o necessário no Godot; avaliar instanciamento em lote, LOD e divisão por regiões conforme perfil real. **Milhões de instâncias desenháveis não equivalem a milhões de agentes simulados.** |
| **Fronteira C# ↔ engine** | A FAQ oficial adverte que muitas chamadas à API Godot podem anular parte da vantagem computacional do C# devido ao custo de interop/serialização. Acessos de leitura frequentes aos Servers também podem impor sincronização. | Evitar chamada à engine por SIM e por quadro. Medir especialmente a transferência de posições, estados e instâncias em lotes do núcleo para a apresentação. |
| **Threads e SceneTree** | Godot suporta multithreading, mas a **SceneTree ativa não é thread-safe** e componentes de renderização/física têm restrições/configuração própria. | Paralelismo da simulação deve trabalhar com dados próprios; acesso ao Godot precisa respeitar suas fronteiras. Não assumir arquitetura multithread ou GPU antes de medir gargalos. |
| **Portabilidade de C#** | A documentação estável registra exportação desktop (Windows/Linux/macOS), mas **Godot 4 + C# ainda não exporta para web**; Android/iOS seguem experimentais. | Não é impedimento identificado para a direção 3D desktop em estudo, **mas não define plataforma-alvo do produto**. Se navegador ou mobile virarem requisitos, reavaliar antes de prometer suporte. |
| **3D e interface** | Godot dispõe de renderização 3D, câmera, cenas, UI, input e ferramentas de inspeção; as limitações da simulação descritas acima não implicam incapacidade de apresentar uma cidade. | Não há evidência atual de que migrar de engine resolveria os gargalos centrais; desempenho visual em cidades densas ainda precisa de profiling separado. |

**Importante:** a documentação do MultiMesh descreve capacidade de desenho de grande volume, **não um benchmark do IndexCities**. A taxa real de atualização da cena, geometria, materiais, sombras, memória de vídeo, visibilidade e hardware-alvo podem tornar o resultado muito diferente. Algumas páginas de performance da documentação estável ainda têm avisos de atualização para versões recentes: confirmar APIs e custos na versão efetivamente adotada ao implementar.

### Principal risco: tempo acelerado com causalidade real

A [SPEC](../SPEC.md#tempo-de-jogo) exige relógio gregoriano único, pause e velocidades 1/2/3, com **todos os acontecimentos efetivamente simulados**. Não podemos mascarar lentidão simulando apenas a câmera visível, saltando datas ou inventando resultado de transações. A arquitetura atual distingue **tick lógico e frame gráfico**, admite agenda de atualizações, núcleo headless, medições e entidades persistentes sem Node próprio. Isso é apropriado e **não exige trocar Godot**.

Há três custos distintos a medir, sem confundi-los:

1. **CPU da simulação:** decisões de SIMs, economia, estoque, deslocamentos, congestionamento, escolha de rotas, eventos e dependências.
2. **Comunicação e renderização:** custo de converter/enviar estado C# para o Godot, instâncias visíveis, culling e FPS.
3. **Memória/persistência:** estados individuais, histórico de viagens e compras, índices, pressão de GC, tamanho e tempo de save/load.

Nenhuma marca de engine, por si só, resolve essas três dimensões. Resultados de outros jogos e demos de instancing ou ECS **não autorizam declarar meta de população, aceleração ou FPS**; os números exploratórios de 10 mil/25 mil/50 mil/100 mil SIMs já existentes neste documento permanecem **cenários de observação, não requisitos aprovados**.

### Alternativas consideradas, sem decisão de migração

| Alternativa | O que potencialmente oferece | Trade-off específico neste projeto |
| --- | --- | --- |
| **Manter Godot + núcleo .NET próprio** | Continuidade da stack, domínio C# familiar, ferramentas prontas de UI/3D e fronteira clara de simulação. | Exige disciplina contra excesso de Nodes/chamadas por entidade, além de profiling gráfico e de GC. |
| **Unity com C#** | Outra combinação de engine e linguagem familiar; diferentes recursos e ecossistema de renderização/data-oriented. | Migração e retrabalho sem benchmark comparável; não dispensa desenvolver o mesmo motor causal de cidade. |
| **Unreal com C++/Blueprints** | Outra opção de infraestrutura gráfica e ferramentas avançadas. | Custo de mudança de linguagem/integração e de stack; não fornece automaticamente economia, tráfego e rotina de SIMs com aceleração real. |
| **Engine/renderizador próprio** | Controle fino sobre todas as fronteiras e estruturas. | Alto custo de criar/manter câmera, editor, assets, UI, renderização e ferramentas que o Godot já entrega. |

**Não existe demonstração comparável de que Unity, Unreal ou engine própria executariam a simulação do IndexCities melhor**. Se surgir um limite concreto e repetível de Godot (não um gargalo do C# core), comparar a mesma carga e a mesma fidelidade em alternativas antes de propor mudança estrutural.

### Como validar durante o desenvolvimento integrado

**Sem criar POCs setoriais obrigatórias nem prometer um resultado antes da medição**, usar a execução headless do core e a integração visual à medida que existirem; comparação ideal com a **mesma seed, carga, velocidade e estado**.

- **Simulação:** horas/dias simulados por segundo real, eventos/s, p50/p95/p99 do tempo de atualização, backlog do scheduler, operações de rota, estoque/dinheiro e consistência causal.
- **Visual:** FPS/tempo de frame, custo de nós/instâncias, draw calls, memória de GPU, picos na atualização de buffers e diferença entre câmera parada e movimentos/zoom.
- **C#/.NET:** CPU por subsistema, bytes alocados, coleções e pausas de GC, memória total e custo da comunicação com Godot.
- **Persistência:** tamanho de saves, escrita/leitura, acesso ao histórico individual e crescimento por ano de jogo.
- **Escala/cenários:** começar por medidas honestas com o que já estiver implementado; variar 10k/25k/50k/100k SIMs como **faixas exploratórias**, sobretudo número de agentes ativos e viagens simultâneas, e não somente residentes inativos.

**Gatilho para reabrir a escolha:** evidência reproduzível de que o host/renderizador Godot (incluindo a fronteira C#↔Godot) é gargalo decisivo **depois de alternativas razoáveis de otimização**, e que outra solução melhora a carga equivalente com custo/portabilidade aceitáveis. Se o gargalo estiver no algoritmo ou nos dados da simulação, mudar de engine provavelmente não o corrige.

### Fontes primárias consultadas (2026-10-10)

- Godot — [C#/.NET e suporte a plataformas](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html) e [C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html).
- Godot — [CPU optimization (linguagens, GC, threads)](https://docs.godotengine.org/en/stable/tutorials/performance/cpu_optimization.html) e [FAQ (custo de chamadas C# ↔ Godot)](https://docs.godotengine.org/en/stable/about/faq.html).
- Godot — [Optimization using Servers](https://docs.godotengine.org/en/stable/tutorials/performance/using_servers.html), [MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html) e [Thread-safe APIs](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html).
- Fontes internas: [SPEC](../SPEC.md), [ARCHITECTURE](../ARCHITECTURE.md), [pesquisa sobre o motor acelerado](simulation-engine-performance-research.md) e [pesquisa transversal de técnicas de simulação](simulation-engine-cross-domain-research.md).

**Síntese:** **manter Godot + C#/.NET é recomendável hoje e consistente com a arquitetura aprovada**, sem garantia de escala. A decisão sensível futura é a capacidade de preservar o mundo acelerado no hardware-alvo, medida separadamente do FPS; não adotar ECS, GPU, threads extras ou trocar engine como solução por antecipação.

---

## Calendário civil aprovado (2026-10-09); meses curtos descartados em 2026-10-16

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **decidiu depois iniciar com calendário real gregoriano** de dias de 24 horas, meses de 28/29/30/31 dias e anos de 365/366 dias, seguindo relógio único e simulação real. **Em 2026-10-16, o responsável confirmou que não aceita encurtar dias, meses, anos ou separar a evolução da vida do relógio real; a evolução de gerações em algumas horas/sessões exige aceleração verdadeira.** A aceleração real de todos os acontecimentos já está aprovada na SPEC; **não reabrir nem relativizar** essa regra.

**Exemplos históricos de calendário condensado — NÃO aprovados e incompatíveis com a confirmação humana de 2026-10-16:**

| Dias por mês | Dias por ano (12 meses) | Prazo de três meses |
| --- | --- | --- |
| 10 | 120 | 30 dias da simulação |
| 15 | 180 | 45 dias da simulação |
| 30 | 360 | 90 dias da simulação |

Um mês mais curto **não torna mais eficiente simular cada dia**; apenas faz haver menos jornadas reais por ano fictício. Aluguéis mensais, salários e despesas recorrentes passariam a vencer mais frequentemente em relação aos dias de rotina. Envelhecimento e nascimento mudariam sua proporção com deslocamentos/consumo. O responsável já expressou anteriormente receio de calendários fictícios com poucos dias por mês; portanto a IA **não deve declarar 10 ou 15 dias aprovados** nem criar dois relógios divergentes para mascarar o impacto. **Direção confirmada em 2026-10-16: calendário convencional como na vida real, sem exceção para acelerar o ciclo de vida.** As alternativas de meses encurtados permanecem apenas como histórico da exploração, não como opção recomendada ou solução de desempenho. A redução de dias por mês não reduz o custo de cada dia simulado.

---

## Escala da cidade, profundidade da simulação e tempo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração.

Esta seção reúne evidências para responder às primeiras perguntas de escala do IndexCities. Os números abaixo não são requisitos do produto enquanto não houver uma decisão explícita.

### 1. Qual população representa uma cidade pequena, mas funcionalmente completa?

Não existe um número universal em que uma cidade passe a ter automaticamente hospital, polícia, bombeiros, comércio e demais serviços. Esses serviços dependem de organização regional, política pública, renda, densidade e papel da cidade na região.

Ainda assim, há referências reais úteis para dimensionamento:

- O Censo 2022 do IBGE registrou média nacional de **2,79 moradores por domicílio particular permanente ocupado**. Na Região Sul, a média foi **2,64**.
- O Ministério da Saúde usa parâmetros de população vinculada por equipe de Saúde da Família. Em municípios de até 20 mil habitantes, o parâmetro é **2.000 pessoas por equipe**; de 20.001 a 50 mil, **2.500**; de 50.001 a 100 mil, **2.750**; acima de 100 mil, **3.000**.
- A política federal de CAPS considera **15 mil habitantes** como patamar a partir do qual um CAPS I é indicado/elegível, enquanto modalidades mais especializadas aparecem em faixas maiores.
- O CNES registra nacionalmente milhares de hospitais, prontos atendimentos, farmácias, UBSs e outros estabelecimentos, e fornece dados por município. Portanto, ele pode ser usado depois para comparar cidades reais específicas.
- IBGE/CEMPRE e RAIS fornecem dados de empresas, unidades locais e empregos por setor e município. Esses dados são melhores que uma razão inventada para estimar comércio e indústria.

Usando somente a média domiciliar nacional de 2,79 como referência estatística:

| População | Domicílios ocupados equivalentes |
| ---: | ---: |
| 15.000 | ~5.376 |
| 20.000 | ~7.168 |
| 30.000 | ~10.753 |
| 50.000 | ~17.921 |

Esses valores são apenas conversão população/domicílio, não uma quantidade decidida de lotes ou casas para o jogo.

**Hipótese para investigar:** a faixa de **15 mil a 50 mil habitantes** é especialmente útil para o primeiro estudo porque já corresponde, no mundo real, a municípios capazes de sustentar uma variedade significativa de serviços e, ao mesmo tempo, continua numa escala muito inferior à de grandes metrópoles. Isso ainda precisa ser validado comparando municípios reais e sua estrutura urbana.

**Próximo passo baseado em dados:** selecionar uma amostra de municípios reais entre 15 mil e 50 mil habitantes e levantar, para cada um, população, domicílios, unidades de saúde, escolas, empregos, comércio, indústria, polícia e bombeiros. Só depois definir uma faixa-alvo para o jogo.

Fontes:
- IBGE, Censo Demográfico 2022: https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102011.pdf
- Ministério da Saúde, parâmetro populacional das equipes de Saúde da Família: https://www.gov.br/saude/pt-br/composicao/saps/esf/equipe-saude-da-familia
- Ministério da Saúde, CAPS: https://www.gov.br/saude/pt-br/composicao/saes/desmad/raps/caps/caps
- CNES / Dados Abertos SUS: https://dadosabertos.saude.gov.br/dataset/cnes-cadastro-nacional-de-estabelecimentos-de-saude
- IBGE, Cadastro Central de Empresas: https://www.ibge.gov.br/estatisticas/economicas/servicos/9016-estatisticas-do-cadastro-central-de-empresas.html
- MTE, RAIS 2024: https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/estatisticas-trabalho/rais/rais-2024/rais-2024-1

### 2. Até onde é viável simular cidadãos e veículos individualmente?

Há evidência forte de que simulações baseadas em agentes podem trabalhar com populações grandes, mas isso **não prova** que o mesmo nível de detalhe seja executável em tempo real dentro do Godot.

Referências:

- Um estudo de 2024 usando MATSim modelou uma amostra de mais de **1 milhão de indivíduos** com trajetórias explícitas para a região de Los Angeles.
- Outro estudo usando MATSim simulou dezenas de milhares de agentes com atividades e viagens individuais.
- Essas ferramentas são especializadas em transporte e não carregam, para cada pessoa, toda a combinação pretendida pelo IndexCities: família, dinheiro, emprego, necessidades, relacionamentos, envelhecimento, renderização, animação, IA local e outros sistemas.

Para Godot, a documentação oficial estabelece limites arquiteturais relevantes:

- a engine oferece multithreading, mas **nem toda a API é thread-safe**;
- interagir com a SceneTree ativa a partir de threads não é seguro;
- os Servers do Godot são indicados para controlar grandes quantidades de instâncias, e a própria documentação cita **dezenas de milhares** de instâncias como um cenário apropriado para uso direto dos Servers em vez de Nodes na SceneTree.

Isso favorece uma hipótese arquitetural, ainda não decidida: **estado de simulação em estruturas C# leves e independentes da SceneTree, com representação visual no Godot somente quando necessária**. O limite real deve ser medido no hardware-alvo.

**Conclusão da pesquisa:** hoje não existe evidência suficiente para escolher honestamente um limite de 10 mil, 50 mil, 100 mil ou 1 milhão de cidadãos para o IndexCities. O número precisa sair de benchmarks do próprio modelo de simulação. Esses benchmarks podem ser pontuais e técnicos: **não são POCs por subsistema nem substituem a validação global da primeira entrega integrada**.

**Benchmark necessário antes da decisão:**
1. cidadão apenas com estado demográfico;
2. cidadão + família + residência + emprego;
3. cidadão + agenda e deslocamento;
4. veículos e pathfinding;
5. economia e necessidades;
6. comparar 10k, 25k, 50k, 100k e faixas maiores até localizar os gargalos;
7. medir separadamente CPU, memória, tempo de tick e custo de apresentação no Godot.

Fontes:
- Godot, Thread-safe APIs: https://docs.godotengine.org/en/4.5/tutorials/performance/thread_safe_apis.html
- MATSim / Los Angeles, 2024: https://www.sciencedirect.com/science/article/pii/S1877050924013218
- MATSim / Amsterdam, 2016: https://www.sciencedirect.com/science/article/pii/S1877050916310134

### 3. Granularidade física dos edifícios

A direção discutida é usar **casas e prédios como unidades físicas**, sem simular cômodos individualmente.

Uma casa pode conter uma família ou residência; um prédio pode conter múltiplas residências/famílias. A modelagem exata de unidades internas ainda não está fechada.

### 4. Escala de tempo e velocidades

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Em 2026-10-08 o responsável **APROVOU a aceleração real de toda a simulação**, registrada na SPEC: quando o tempo avança mais rápido, os acontecimentos de SIMs, transporte, produção, estoques e economia também acontecem de verdade, sem salto de data nem resultados inventados. **O calendário civil real foi aprovado depois em 2026-10-09.** Duração do dia simulado em tempo real, eventual velocidade adicional e estratégia concreta de performance continuam em aberto. As comparações e técnicas abaixo são pesquisa da IA **PENDENTE de revisão humana**.

**Questão P0:** conciliar minutos de deslocamento, turnos diários, compras e alimentação, obras, salários e aluguéis mensais, reajustes anuais, infância, educação, reprodução e envelhecimento **sem que as diferentes escalas deixem a cidade artificial ou tornem o jogo lento**. Números de outras simulações não são parâmetros do IndexCities.

#### Direção de produto aprovada: acelerar o mundo real da simulação

**Fonte de verdade: [SPEC](../SPEC.md#tempo-de-jogo).** Aumentar a velocidade deve fazer o **mesmo mundo efetivamente avançar**, preservando identidades individuais, viagens/cargas físicas, trabalho, empresas, produção, necessidades, saldos, transações, dívidas e envelhecimento. Não basta alterar números do calendário, ocultar operações ou trocar resultados por estimativas. **Processar eventos de maneira mais eficiente não significa perder acontecimentos**: intervalos estáveis podem ser calculados sem atualizar cada frame se o estado e as consequências permanecerem equivalentes.

**Situação vigente após 2026-10-16:** calendário civil real e 1x igual a 1 segundo simulado por segundo real; velocidades provisórias de 30x/1000x. Não há aprovação de desempenho garantido, velocidade geracional máxima, controles adicionais ou modelo técnico específico. Estratégias de operação, calendário compacto/representativo, relógios separados, replay e projeções são **histórico de pesquisa**, não escolhas atuais. Modelos que substituem eventos por dias/produção/salários fictícios conflitam com o princípio agora aprovado. A prioridade da investigação muda para **capacidade de executar a simulação causal mais rapidamente**, sem abandonar a legibilidade do jogador.

#### Como buscar performance excepcional — hipóteses técnicas, NÃO arquitetura aprovada

> **Revisão humana desta subseção: PENDENTE.** Síntese e ordem de investigação propostas pela IA após o responsável decidir **o resultado desejado**, mas **sem aprovar a implementação**. A ARCHITECTURE já prevê scheduler, simulação independente do render, headless e medições; não criar novas camadas por antecipação.

| Estratégia candidata | Ideia simples | Maior cuidado |
| --- | --- | --- |
| **Atualização por acontecimentos** | Quem está dormindo, viajando num trecho previsível ou aguardando entrega não precisa recalcular tudo a cada frame; agendar próxima mudança útil | Invalidar previsões quando houver congestionamento, interrupção, cancelamento ou novo comando |
| **Dependências e propagação localizada** | Quando faltar energia, reavaliar consumidores afetados, não toda a cidade | Índices corretos, reconstruíveis e sem deixar agentes em estado desatualizado |
| **Dados compactos e amigáveis à memória** | Processar grandes conjuntos de estados mínimos em estruturas eficientes, mantendo SIMs/empresas individuais | Não sacrificar identidade nem introduzir duplicação inconsistente de estado |
| **Paralelismo controlado** | Preparar trabalho independente em paralelo e aplicar efeitos compartilhados em ordem definida | Duas famílias não podem comprar o último produto ao mesmo tempo; sincronização e memória podem custar mais que o ganho |
| **Roteamento e apresentação separados** | Reutilizar informação de redes/rotas quando válido; renderizar posições intermediárias sem decidir economia no Godot | Alterações de ruas e tráfego devem atualizar o resultado real; desenhar mais rápido não acelera simulação |

**Evidências externas úteis:** [Factorio — atualizações inativas e robôs](https://www.factorio.com/blog/post/fff-421), [Factorio — otimização](https://www.factorio.com/blog/post/fff-148), [A/B Street — eventos de trânsito](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html) e [Microsoft .NET — SIMD](https://learn.microsoft.com/en-us/dotnet/standard/simd). Não transportar ganhos publicados de outros jogos como estimativas para IndexCities. Mais threads podem piorar o custo de memória/sincronização; **a primeira pergunta é como realizar menos trabalho por evento verdadeiro**.

**Investigação prioritária na próxima sessão:** comparar execução simples e otimizada em cenários reproduzíveis com população crescente; medir tempo **simulado/segundo real**, eventos/segundo, custo de tráfego e pathfinding, tempo por sistema, memória, GC e picos. Verificar **os mesmos resultados causais em checkpoints comparáveis**, conservação de dinheiro, estoque, entregas, empregos e envelhecimento nas velocidades aprovadas. Não prometer 100x, anos em segundos nem criar POC setorial obrigatória. Identificar gargalos reais e então escolher a técnica mais simples que os resolve. No ritmo muito acelerado, a **legibilidade visual** continua desafio distinto da correção da simulação.

#### Modelos de calendário pesquisados antes da decisão de aceleração real (histórico; nenhum escolhido)

| Alternativa | Como funcionaria | Benefício | Custo / perigo |
| --- | --- | --- | --- |
| **A — calendário único condensado** | Relógio, rotinas, finanças e ciclo de vida avançam na mesma escala lógica, acelerada em relação ao mundo real | Sequência causal e datas intuitivas; menos exceções | Para envelhecer gerações em tempo agradável, pode comprimir trabalho, viagens e vencimentos até perder significado |
| **B — dia operacional representativo** | Dia/noite e rotinas físicas representam um intervalo maior de calendário, por exemplo um mês | Permite observar a cidade amadurecer em menos sessões | A equivalência entre compras, salários, alimentação, idades e datas torna-se menos literal; exige convenções explícitas |
| **C — calendário demográfico/histórico separado do ritmo econômico/operacional** | Progressão das idades e marcos do calendário pode avançar de modo diferente das operações comerciais/viárias | Ajuste mais independente do ritmo das gerações e da economia | Datas, periodicidade de aluguéis/salários, educação, gravidez, estoques, vencimentos, estatísticas, replays e saves podem divergir; obriga mapear eventos entre escalas |

**Evidência de desenvolvedores:**
- [OpenTTD 14.0 — artigo técnico “The stoppable march of time”](https://www.openttd.org/news/2024/03/23/timekeeping) e [notas da versão](https://www.openttd.org/news/2024/04/13/openttd-14-0): separou **economy time** e **calendar time** para permitir desacelerar evolução histórica sem desacelerar transporte e produção; a equipe descreve dificuldades com estatísticas, datas, interface e compatibilidade. **Importante:** o jogo não modela a combinação completa de família, aluguel, escola e patrimônio do IndexCities; a solução não pode ser copiada diretamente.
- [Cities: Skylines II — Climate & Seasons, desenvolvedores](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/climate-seasons): um ciclo de dia/noite equivale a um mês e um ano tem 12 ciclos. Demonstra a opção B; **não prova** adequação à economia física dos nossos SIMs.
- [Banished — mod Faster Aging](https://banishedinfo.com/mods/view/23-Faster-Aging) e [Banished Ventures — Speed Aging](https://www.banishedventures.com/mods/speed-aging/): mudanças comunitárias na taxa de envelhecimento indicam que a **reposição de gerações** é variável importante e discutida; mods são testemunhos de preferências, não estudo controlado de gameplay.
- [Sapiens — tempo](https://wiki.playsapiens.com/index.php/Time): ano comprimido em poucos dias; referência de solução possível, não baseline.
- [A/B Street — simulação por eventos](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html): indivíduos fisicamente presentes podem ter estado persistente e atualizar-se por eventos relevantes, sem processar todos a cada frame. **Frequência de atualização é um problema técnico distinto da semântica do calendário**. Não impor scheduler de eventos global sem medições.

#### Relações causais que não podem se perder na avaliação

- Nenhum salário, aluguel, juro, imposto, benefício, conta ou dívida surge porque *somente a idade/calendário visual* acelerou sem evento econômico correspondente; o recebedor/pagador continua agente real da SPEC.
- Compra de comida, consumo do estoque doméstico, reposição por carga física e preço têm de permanecer coerentes com horários de mercado, viagens e dinheiro — mesmo se “um dia” representar período maior.
- SIMs não devem viver ciclos de gravidez, escola, trabalho e envelhecimento incompatíveis entre si; um modelo separado precisaria regras explícitas sobre **qual escala governa cada duração**, sem duplicar pagamentos.
- Obra, deslocamento e trabalho não podem parecer teletransportados só para compensar a compressão do calendário; nem devem exigir longas esperas artificiais.
- Mudanças de velocidade, pause, save/reload e replay não devem alterar sequência econômica, idade nem resultados aleatórios por mero artefato do relógio.
- Tempo simulado, duração de uma obra e **tempo real percebido pelo jogador** são grandezas distintas. Um modelo formalmente consistente pode ser lento ou frustrante.

#### Comparação recomendada — não é nova POC ou decisão de arquitetura

**Recomendação histórica anterior à decisão de aceleração real:** comparar A e B primeiro, deixando C como alternativa. **Não é mais a prioridade da pesquisa atual**; o requisito aprovado é executar o mundo realmente acelerado. A seleção de calendário permanece aberta. Avaliar no **jogo integrado definido pela SPEC**, com parâmetros ajustáveis e cenários reproduzíveis: (1) casa → trabalho → compra → casa, incluindo pico de tráfego; (2) mês de salários, aluguéis, consumo e caixa empresarial; (3) construção e entrega sem demoras artificiais; (4) formação e evolução de famílias durante vários anos; (5) alternância pause/1/2/3, save e replay.

Medir **legibilidade de viagens, taxas de transações/consumo, tempo de espera percebido, evolução das gerações, conservação monetária, pontualidade dos eventos e custo computacional**. Não fixar agora “12 minutos por dia”, “30 dias por ano”, multiplicadores exatos ou fórmulas de aceleração etária: esses eram **exemplos hipotéticos**, não recomendação final nem decisão aprovada.

**Nota histórica (PENDENTE):** antes da aprovação, a IA recomendava apenas preservar causalidade e comparar calendários. **Agora a aceleração real dos acontecimentos está decidida na SPEC; a escala exata do calendário e o desempenho alcançável continuam por medir/decidir.**

#### Hipótese D — calendário convencional e avanço temporal estratégico (2026-10-08)

> **Revisão humana desta subseção: PENDENTE — ideia documentada a pedido do responsável, mas expressamente NÃO DECIDIDA e sujeita a novas rodadas de pesquisa.** Não converter esta proposta em requisito, plano de implementação, velocidade extra, nova modalidade de jogo ou preferência definitiva.

**Motivação (histórica; superada em 2026-10-16, quando o responsável definiu o acompanhamento geracional como meta — ver SPEC):** o responsável expressou preocupação com a estranheza de um calendário ficcional de poucos dias por mês. A reflexão seguinte questionou uma premissa anterior: **a SPEC determina nascimentos, envelhecimento e ciclo de vida, mas não exige que o jogador observe uma geração inteira em algumas dezenas de horas**. Acelerar toda a economia para alcançar esse objetivo talvez seja otimizar a variável errada.

**Hipótese combinada (D), composta por possibilidades independentes e ainda não aprovadas:**

1. **Calendário familiar/convencional:** estudar dias de 24 horas, meses e anos reconhecíveis (potencialmente próximos ao calendário terrestre), com um relógio lógico causal para acontecimentos operacionais, econômicos e demográficos. **Atualização posterior:** o calendário real, incluindo anos de 365/366 dias, **foi aprovado depois**; a duração do dia em segundos reais e o custo computacional permanecem em calibração e medição.
2. **Migração com idades variadas:** estudar entradas voluntárias de famílias com crianças, adultos em idades distintas e idosos, para criar estrutura etária e necessidades variadas **sem acelerar artificialmente o crescimento de uma criança nascida na cidade**. A **migração de agentes reais** já é aprovada na SPEC; **a distribuição etária de candidatos, a composição concreta das coortes e qualquer política para favorecer diversidade ainda NÃO foram aprovadas**. Continuam obrigatórios oportunidade plausível, vontade do agente, moradia/condições e capital real transferido da Reserva Global; não gerar moradores fictícios nem garantir imigração.
3. **Avanço temporal estratégico opcional:** explorar avançar a simulação até uma data ou condição observável enquanto o jogador não precisa assistir a cada frame, como alternativa ao acompanhamento normal. **Não é salto de data**: ações de SIMs, deslocamentos e cargas, consumo, produção, pagamentos, restrições, falhas, contratação e decisões autônomas devem continuar causalmente consistentes, com estados reais e sem teletransporte. Uma eventual interrupção em acontecimento importante precisaria critérios e frequência cuidadosos para **não transformar o jogo em alarmes ou cliques repetitivos**. A interface, ações "próximo mês/fim da obra" e modo adicional **não são aprovados**.
4. **Histórias de curto/médio prazo:** investigar se mudança de emprego, moradia, família, falência de empresas, serviços e bairros já fornecem histórias emergentes em meses/anos, **sem obrigar que nascimento → vida adulta → velhice ocorra durante uma sessão**. Não inventar missões nem acontecimentos roteirizados só para entreter.

**O que esta hipótese realmente resolve — e o que NÃO resolve:**

| Questão | Potencial | Limite não resolvido |
| --- | --- | --- |
| Naturalidade de datas | Dias, meses, vencimentos e aniversários familiares são mais intuitivos | Tempo real do jogador por mês/ano pode continuar excessivo |
| Sensação de cidade viva | Agentes migrantes em idades variadas e trajetórias pessoais produzem acontecimentos desde cedo | Não acelera a maturação real dos recém-nascidos, nem garante chegada de novos moradores |
| Espera percebida | Jogador poderia não assistir a todos os movimentos durante avanços longos | A simulação integrada continua fazendo trabalho real; **anos em segundos são hipótese sem benchmark**, não promessa |
| Coerência monetária e logística | Em princípio, preserva as mesmas transações e relações causais em ambos os modos | Otimizar viagens, estoques e operações não pode fabricar transações ou suprimir gargalos |
| Desempenho e save | Arquitetura já separa tick/renderização, permite scheduler e headless | Isso **não** prova avanço arbitrariamente rápido; eventos de muitos SIMs/veículos podem limitar muito a aceleração |

**Compatibilidade com autoridade existente:** a SPEC só aprova **pause e velocidades 1/2/3**, com parâmetros configuráveis; o avanço estratégico seria **funcionalidade adicional** e exigiria aprovação antes de implementação. A ARCHITECTURE já prevê simulação desacoplada do render, scheduler e headless — **capacidade técnica, não autorização de gameplay nem garantia de performance**. A SPEC ainda determina aluguéis mensais, reajustes anuais, dívidas com prazos, trabalho presencial, estoque doméstico, produção, salários e oferta monetária fixa. D deve preservar essas invariantes.

**Relação com propostas anteriores (histórico):** A, B e C eram modelos de calendário pesquisados; D trouxe calendário convencional, migração etária e avanço estratégico. **A escolha posterior do responsável agora exige tempo acelerado com todos os acontecimentos reais**. Qualquer variante que substitua jornadas, deslocamentos, compras ou produção por dias fictícios fica incompatível com esse princípio. **Posteriormente foi aprovado apenas o calendário convencional único**; o pacote D histórico não foi aprovado integralmente, incluindo modo adicional de avanço e migração etária.

**Próximas rodadas de pesquisa — questões que realmente discriminam propostas:**
- **Objetivo de tempo percebido:** para o jogador, o essencial é acompanhar **rotinas diárias**, a evolução da **cidade em anos**, ou uma **vida individual completa**? Precisamos de experiências de referência e cenários comparáveis antes de estipular duração.
- **Viabilidade real do avanço:** quantos eventos, deslocamentos, transações, pathfindings, alterações de estoque e mudanças de emprego ocorrem num mês com população crescente? Qual aceleração sustentável um benchmark headless integrado demonstra? Onde aparecem backlog, quedas de desempenho e perda de legibilidade?
- **Preservação da história causal:** avançar um mês com e sem renderização/visualização gera resultados compatíveis para dinheiro, mercadorias, vidas dos SIMs e saves? Se usar agrupamento fora da câmera, o que continua fisicamente rastreável?
- **Risco de interação:** quando parar automaticamente, quando apenas registrar um resumo, e como evitar interromper constantemente o jogador? Avanço entre dois eventos pode criar um ciclo de espera/clicks mais chato que x3.
- **Idade e coortes:** a variedade de idades dos migrantes ajuda na experiência ou introduz ondas de mortes/aposentadoria, necessidade instantânea de serviços e desequilíbrio de capacidade?
- **Alternativas adicionais:** aceleração adaptativa baseada no que precisa ser processado; avanço por evento em vez de data; apresentar **histórico/painéis da cidade** sem acelerar o mundo; agregação *somente* para cálculo de estados comprovadamente equivalentes (não agentes fictícios); manter tempo normal e aceitar que uma geração leve várias sessões. Todas exigem confronto com gameplay, causalidade e custo antes de recomendação.

**Status para futuras sessões:** hipótese D continua **PENDENTE de revisão humana** como pacote; **só o princípio de aceleração real da simulação foi aprovado depois, na SPEC**. Novas sessões devem priorizar **engenharia de desempenho e correção causal**, sem voltar a debater modelos de tempo substitutivos, sem prometer aceleração extrema e sem implementar funcionalidades não aprovadas.

#### Rodada complementar — execução por eventos, ritmo contextual, lupa temporal e cenários futuros (2026-10-08)

> **Revisão humana desta subseção: PENDENTE — pesquisa e recomendações da IA registradas a pedido do responsável, SEM decisão de produto.** O usuário solicitou mais alternativas fora da caixa e depois perguntou qual era a favorita, **não aprovou** nenhuma. Não modificar a SPEC, a arquitetura ou o escopo por inferência.

**Descoberta central:** a conversa misturava **(i) semântica do calendário**, **(ii) trabalho computacional para avançar a cidade** e **(iii) capacidade do jogador de acompanhar acontecimentos**. A, B, C e D discutem principalmente o primeiro tema e combinações de ritmo; as quatro ideias a seguir são **estratégias complementares**, **não quatro modelos novos de calendário**. Podem ser avaliadas independentemente e não resolvem sozinhas a duração dos anos.

| Ideia de pesquisa | Problema atacado | Benefício possível | Risco/custo e autoridade |
| --- | --- | --- | --- |
| **E — intervalos/eventos relevantes; cálculo sob demanda** | SIMs, viagens, veículos, estoques e empresas não precisam necessariamente recalcular seu estado a cada frame | Menos atualizações desnecessárias; estado/posição de um trajeto previsível podem ser derivados de intervalo conhecido até um evento que o altere | Congestionamento novo, emergência, mudança de rota, estoque vencido ou falta de pagamento têm de invalidar previsões corretamente; custos de filas e replanejamento podem anular ganho. **A ARCHITECTURE já aprova scheduler, atualizações por ritmos e tick separado do render; E é aprofundamento/hipótese de otimização, NÃO nova funcionalidade automaticamente aprovada.** |
| **F — ritmo de observação contextual automático** | Trocar manualmente velocidade repetidamente e esperar períodos sem decisões | Considerar alternância assistida entre velocidades **1/2/3 já previstas na SPEC**, respeitando escolha do jogador, sem pressupor saltos | Cidade nunca fica inteiramente ociosa (turnos noturnos, caminhões, saúde, emergências); critérios para desacelerar podem gerar interrupções e comportamento imprevisível. **Mudar velocidade automaticamente é novo comportamento de UI/gameplay NÃO aprovado**, mesmo sem criar 100x. |
| **G — lupa temporal / histórico consultável de SIMs** | Jogador perde trajetos e acontecimentos importantes quando observa outra parte da cidade | Consultar, depois, eventos pessoais reais (mudança de emprego, compra, visita, mudança de casa), talvez trechos de deslocamento com dados suficientes; favorece histórias sem obrigar simulação inteira a rodar devagar | Histórico por SIM exige limites de armazenamento, retenção, identidade e diagnóstico; **replay visual exato não pode ser prometido sem registrar estado suficiente**; evento resumido NÃO equivale a reconstituição fiel. **Nova funcionalidade NÃO aprovada**; preservar registros existentes somente quando de fato úteis. |
| **H — projeção de cenários alternativos** | Desejo de avaliar impacto de decisões em horizontes longos sem esperar anos na cidade principal | Exibir cenários hipotéticos condicionais, distinguindo simulação corrente de estimativas (ex.: emprego/fluxos após nova indústria) | Previsões podem sugerir lucro/causalidade garantidos; ramificação de mundo ou segundo modelo agregado pesa em performance, regras, manutenção, memória e microgerenciamento. **Nova funcionalidade NÃO aprovada; baixa prioridade no estágio atual.** |

**Fontes e limites concretos:**
- [A/B Street — discrete event traffic simulation](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html): o desenvolvedor descreve pedestres/veículos que agendam transições futuras e posições interpoladas; também explica por que congestionamento e mudanças de faixa tornam o método complexo. Evidência técnica para **E no domínio de mobilidade**, não prova de que todas as decisões econômicas do IndexCities possam ser simplificadas do mesmo modo.
- [Kerbal Space Program / kOS — time warp](https://ksp-kos.github.io/KOS_DOC/structures/misc/timewarp.html): distingue *physics warp* de *rails warp*, que calcula movimento orbital sob hipóteses simplificadas **sem executar toda a física**. Inspira a distinção entre modos de processamento, mas **não serve como autorização para suprimir viagens, comércio ou produção reais**.
- [Stardew Valley — day cycle](https://wiki.stardewvalley.net/Day_Cycle): o jogador pode dormir e encerrar cedo o seu dia. É analogia de **redução de espera**, mas um jogo de um protagonista não equivale a uma cidade inteira ativa, nem comprova que F seja desejável.
- [Factorio — replay system, documentação oficial](https://wiki.factorio.com/Replay_system): replays com velocidade ajustável dependem de gravação e têm restrições de versões/mods, navegação e reprodução retroativa. Apoia estudar **G**, inclusive os custos; um replay completo da cidade não é requisito.
- [UrbanSim — cenários](https://cloud.urbansim.com/docs/block-model/documentation/scenarios.html) e [uso de cenários e projeções](https://cloud.urbansim.com/docs/general/documentation/urbansim.html): compara **futuros condicionados a premissas**, não previsões garantidas. Ferramenta de planejamento especializada, distante do escopo inicial de um city builder com agentes individuais; **H é hipótese de pesquisa futura**.

**Preferência histórica da IA (NÃO aprovada, sujeita a novas rodadas): E como direção técnica de investigação, combinada com G em versão modesta de histórico baseado em eventos, não replay visual universal.** Priorizar E porque se alinha ao scheduler e à persistência causal já previstos, podendo beneficiar desempenho sem alterar datas ou fatos do mundo. Investigar G somente se a observação de histórias individuais comprovar valor real; começar por eventos relevantes, não gravar continuamente todos os movimentos. **D (calendário convencional + migração etária variada e possibilidade de avanço estratégico)** é contexto interessante para E/G, mas **o calendário convencional foi aprovado posteriormente, mas avanço estratégico adicional permanece não aprovado**. F fica como pesquisa de experiência; H como hipótese mais distante. A/B/C e meses compactos são alternativas históricas ou futuras a reexaminar somente por nova decisão; **não estão abertas como escolha do modelo inicial**, pois o calendário real foi aprovado. A preferência da IA não implica decisão do responsável nem altera prioridade de implementação.

**Critérios de confronto com gameplay, causalidade, micro e performance:**
- **Mesma cidade, dois modos de execução:** comparar pequenos passos versus agendas por evento em situações de tráfego com mudanças dinâmicas, estoque esgotando, entrega e pagamentos. Invariantes: dinheiro global, quantidade/posição física de cargas, elegibilidade de serviço, pontualidade e identidade de SIMs. Não aceitar um algoritmo somente por FPS.
- **Custo real de acelerar:** medir eventos, invalidações, replanejamentos, atrasos de scheduler, pathfinding, alocações e latência do comando em população crescente. Não prometer anos em segundos, nem modo 100x.
- **Histórico realmente útil:** acompanhar um SIM enquanto o jogador constrói outra região; comparar informações agregadas existentes, eventos significativos e hipotético replay visual. Medir legibilidade, armazenamento, quantidade de notificações e custo de instrumentação; não confundir *gravar evento* com *reverter o mundo*.
- **Interrupções e autonomia:** ritmo contextual não deve assumir que “madrugada é vazia”; possíveis desacelerações precisam respeitar escolhas do jogador e não virar burocracia invisível.
- **Previsão versus verdade:** toda projeção hipotética, caso um dia aprovada, precisa declarar premissas e incerteza e **não** substituir transações concretas, produção física ou decisões autônomas por números mágicos.

**O que permanece realmente em aberto após 2026-10-16:** como tornar possível observar uma vida completa em algumas horas/sessões preservando calendário real e eventos causais; duração real-alvo, tamanho de cidade e eficiência sob congestionamento; eventual valor de avanço estratégico, ritmo contextual e projeções (não aprovados). **Não iniciar implementação ou criar subsistemas extras com base nesta pesquisa.** A validação pode ocorrer nos cenários do jogo integrado definidos na SPEC e na ARCHITECTURE, sem POCs independentes obrigatórias.

### Regra de evidência para decisões futuras

Para os assuntos desta seção:

- dados reais servem para determinar ordens de grandeza e relações;
- benchmarks do próprio IndexCities determinam limites técnicos;
- referências de outros jogos servem como comparação, não como prova;
- nenhum número vira requisito apenas porque parece razoável.


---

---

## Baselines empíricos provisórios

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status:** referência de calibração, não decisão de produto.

Para evitar que a simulação fique sem parâmetros enquanto decisões finais ainda estão abertas, o projeto pode usar **baselines empíricos provisórios** derivados de dados reais. Esses valores devem permanecer configuráveis e ser substituídos quando estudos mais específicos ou benchmarks do próprio IndexCities justificarem.

### Referência geográfica inicial

Os primeiros baselines abaixo usam **Brasil** por disponibilidade e qualidade de dados oficiais. Isso **não define** que o mundo do IndexCities será brasileiro. O recorte geográfico/estético do jogo continua aberto.

### Domicílios

O Censo 2022 registrou média de **2,79 moradores por domicílio particular permanente ocupado** no Brasil.

Isso equivale aproximadamente a:

- **358 domicílios para cada 1.000 habitantes**;
- 10 mil habitantes → ~3.584 domicílios;
- 20 mil habitantes → ~7.168 domicílios;
- 30 mil habitantes → ~10.753 domicílios;
- 50 mil habitantes → ~17.921 domicílios.

Fonte: IBGE, Censo Demográfico 2022:
https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102011.pdf

### Posse de automóvel e motocicleta por domicílio

Na PNAD Contínua 2023:

- **48,1%** dos domicílios possuíam automóvel;
- **24,6%** possuíam motocicleta;
- **12,6%** possuíam ambos.

Logo, usando a média nacional apenas como baseline:

Para cada **1.000 habitantes** (~358 domicílios):

- ~172 domicílios têm automóvel;
- ~88 têm motocicleta;
- ~45 têm ambos;
- ~215 têm pelo menos automóvel ou motocicleta;
- ~143 não têm nenhum dos dois.

Exemplo para uma cidade de **30 mil habitantes** (~10.753 domicílios):

- ~5.172 domicílios com automóvel;
- ~2.645 com motocicleta;
- ~1.355 com ambos;
- ~6.462 com ao menos um dos dois;
- ~4.291 sem automóvel nem motocicleta.

Esses números representam **domicílios que possuem o bem**, não quantidade total de veículos. Um domicílio pode possuir mais de um carro ou moto.

Há grande variação regional: em 2023, por exemplo, a posse de automóvel por domicílio variava de **27,6% no Nordeste** a **67,5% no Sul**. Portanto, posse de veículos deve ser tratada como parâmetro socioeconômico/geográfico, não constante universal.

Fonte: IBGE, PNAD Contínua — Características gerais dos domicílios e moradores 2023:
https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102158_informativo.pdf

### Frota registrada: referência administrativa

A Senatran registrou **129,1 milhões de veículos** no Brasil em dezembro de 2025, incluindo cerca de **64,6 milhões de automóveis**.

Com a população estimada pelo IBGE em 2025 de **213,4 milhões de habitantes**, isso equivale aproximadamente a:

- **303 automóveis registrados por 1.000 habitantes**;
- **605 veículos registrados de todos os tipos por 1.000 habitantes**.

Em uma cidade hipotética de 30 mil habitantes, aplicar diretamente essas razões daria cerca de **9,1 mil automóveis registrados** e **18,1 mil veículos totais**.

**Importante:** isso não deve ser usado como quantidade de veículos ativos na simulação. O cadastro da Senatran inclui veículos registrados que podem não estar efetivamente em circulação e também veículos de empresas, carga, reboques e outras categorias. Esse número funciona como teto/referência administrativa e deve ser confrontado com posse domiciliar e uso real.

Fontes:
- Senatran, Frota de Veículos 2025:
https://www.gov.br/transportes/pt-br/assuntos/transito/conteudo-Senatran/frota-de-veiculos-2025
- IBGE, Estimativas da População 2025:
https://ftp.ibge.gov.br/Estimativas_de_Populacao/Estimativas_2025/POP2025_20260113.pdf

### Uso real no deslocamento ao trabalho

O Censo 2022 mostrou, entre trabalhadores que se deslocam:

- **32,3%** usam automóvel como principal meio;
- **21,4%** usam ônibus;
- **17,8%** vão a pé;
- **16,4%** usam motocicleta;
- **6,2%** usam bicicleta;
- os demais modos têm participações menores.

Também há forte diferença regional: o automóvel chega a **45,9%** no Sul, enquanto motocicletas têm peso muito maior no Norte e Nordeste.

Para o IndexCities, isso sugere separar três conceitos:

1. **posse** de veículo;
2. **disponibilidade** do veículo naquele momento;
3. **escolha modal** para cada viagem.

Ter carro não implica usá-lo em toda viagem.

Fonte: IBGE, Censo 2022 — Deslocamentos para trabalho:
https://educa.ibge.gov.br/jovens/materias-especiais/23064-censo-2022-como-a-populacao-se-desloca-para-estudar-e-trabalhar.html

### Consequência para o modelo de veículos

Baseline de exploração:

- veículos devem existir como patrimônio de pessoas/famílias/empresas, e não ser criados apenas quando uma animação precisa aparecer;
- quantidade de veículos possuídos e quantidade de veículos simultaneamente nas ruas são coisas diferentes;
- geração de tráfego deve depender de agenda, destino, posse, disponibilidade, custo e escolha modal;
- parâmetros de posse e uso devem ser configuráveis para representar cidades com perfis socioeconômicos diferentes;
- o jogo não deve tentar manter toda a frota visível/movendo ao mesmo tempo apenas porque ela existe no estado da cidade.

### Próximas médias a investigar

A mesma metodologia deve ser aplicada progressivamente a:

- composição etária;
- tamanho e tipos de família;
- população economicamente ativa e empregos;
- distribuição de empregos por comércio, indústria e serviços;
- quantidade e porte de escolas;
- saúde;
- polícia e bombeiros;
- estabelecimentos comerciais;
- taxas de nascimento, casamento e mortalidade;
- quantidade de viagens por pessoa/dia;
- distâncias e tempos de deslocamento;
- taxa de ocupação dos veículos;
- transporte público;
- consumo e renda.

Sempre que possível, usar uma combinação de dados nacionais + amostra de municípios pequenos reais, em vez de uma única média nacional.


---


## Contraprova de duas atividades econômicas reais: frete e construção (2026-10-22)

> **Somente experimento isolado na [PR #55](https://github.com/Marlohn/IndexCities/pull/55), não regra aprovada do produto.** A SPEC continua responsável pelos trabalhos, materiais, emprego e serviços. Não integrar esse piloto ao jogo por dedução.

**Problema observado:** o laboratório de 10 mil cidadãos com lojas e fazendas, mesmo quando abriu vagas reais e dimensionou a produção alimentar pela demanda concreta, manteve 10.255 vivos no primeiro ano, mas terminou 10 anos com **70 vivos** ([run #39854044809](https://github.com/Marlohn/IndexCities/actions/runs/39854044809)). A geração de renda por comércio alimentar é estreita para sustentar todas as famílias. Empresas não têm proprietários SIM modelados nem pagam dividendos no primeiro modelo **por regra explícita da [SPEC de economia](../spec/economia.md)**; **não inventar distribuição de lucros** para maquiar o fluxo monetário.

**Experimento de fornecedor e logística:** criadas vagas adicionais reais nas fazendas para 64 motoristas, distinguindo quem colhe de quem entrega. Uma entrega de alimento desconta estoque da fazenda, registra cobrança atacadista + frete efetivamente pago pela mercearia, mantém carga com o próprio trabalhador durante deslocamento pela rede e só credita o estoque da mercearia na chegada. Motorista recebe salário bruto de caixa real do fornecedor e desconto contributivo real. O primeiro rascunho entrava em loop quando residência e fazenda estavam no mesmo nó; corrigido e revalidado. Outra falha real foi usar agricultores no transporte, reduzindo colheitas; vagas extras evitaram a substituição. O laboratório **não modela ainda caminhão individual, combustível nem motorista externo como entidade plena**, portanto não satisfaz toda a logística da SPEC. [Teste 1 ano aprovado #39857244199](https://github.com/Marlohn/IndexCities/actions/runs/39857244199): **10.257 vivos**, mínimo 10.000, **1.866 refeições não atendidas**, **12.629 entregas com motorista**, 64 funcionários destinados a transporte; conserva dinheiro e comida, snapshot determinístico. [Dez anos reprovados no mesmo run](https://github.com/Marlohn/IndexCities/actions/runs/39857244199): **102 vivos**, 1.216.143 refeições não atendidas.

**Experimento de obras:** famílias com cinco ou mais membros vivos e dinheiro suficiente podem financiar um único aumento físico de sua residência, pagando **empreiteiro real** (salários do caixa da empresa) e **madeira de um estoque externo finito** (pagamento real à Reserva Global). Material sai do estoque exterior, viaja em veículo de entrega pela rede, entra no canteiro e é incorporado à obra somente em turnos em que um trabalhador SIM contratado compareceu; trabalho e pagamento ficam no histórico do Sim. Cada casa concluída carrega o estado físico e dez unidades incorporadas. É uma **hipótese de demanda residencial para laboratório**, não aprovação da mecânica de ampliação da V1 nem representação completa de propriedade, veículo, modelo 3D, combustível e lote. [Teste 1 ano aprovado #39858224145](https://github.com/Marlohn/IndexCities/actions/runs/39858224145): 144 contratos iniciados, **119 concluídos**, 2.583 turnos pagos, 10.255 vivos e conservação **monetária, alimentar e de materiais**; save/resume exato, CI normal de 10 mil Sims também verde.

**Dois ciclos em uma mesma cidade:** [ensaio 10 anos #39858441908](https://github.com/Marlohn/IndexCities/actions/runs/39858441908) combina o mercado de vagas, transporte remunerado e obras custeadas por famílias. **REPROVADO**: 327 contratos de obra concluídos, 6.540 turnos de construção, 111.044 fretes feitos, 1.216.800 refeições não atendidas e **somente 60 Sims vivos ao fim**. Sem criação arbitrária de moeda, alimento ou madeira, o desemprego e a renda doméstica continuam insustentáveis numa economia incompleta, com pouco retorno do caixa das empresas às pessoas. Esta prova negativa é **mais informativa** do que ajustar preços e salários até conseguir 10 mil vivos artificialmente.

**Limite do que se pode concluir:** a [carga sintética de 80 ciclos anuais independentes](https://github.com/Marlohn/IndexCities/actions/runs/39849177974) demonstrou 10 mil Sims vivos por ciclo (29.220 dias somados, 570,9 milhões de viagens, ~724 s), mas **não uma cidade contínua de 80 anos com histórico persistido**. É necessário incorporar progressivamente os demais fluxos econômicos realmente aprovados na V1 — contratos de obras e fornecedores externos nos termos da SPEC, salários municipais quando efetivamente financiados, combustível, transporte e consumo de serviços com contrapartes físicas — antes de alegar estabilidade demográfica e financeira secular. Não usar mercados, motoristas, dinheiro ou proprietários fictícios para preencher o que falta.

---


## 2026-10-22 — falsificação econômica por famílias e teto demográfico (PR #55)

**Cadeia observada no cenário integrado:** [Actions #39858441908](https://github.com/Marlohn/IndexCities/actions/runs/39858441908), empregos reais + frete pago + obra residencial: 10.255 Sims vivos no dia 365, 9.989 no dia 730, 2.359 no dia 1.095, e 60 no fim de 10 anos. Dinheiro, comida e material conservados. No dia 730 a população já tinha 4.303 adultos sem saldo; o caixa doméstico total caiu de 168.814.744 (dia 365) a 43.275.000 centavos, enquanto empresas tinham 1.681.732.790 centavos, com alimento disponível nas lojas. A causa principal é falta de circulação de renda para pessoas, não insuficiência geral de alimento.

**Teste novo, sem empregos artificiais:** [Actions #39860077433](https://github.com/Marlohn/IndexCities/actions/runs/39860077433), opção experimental de contratação que prioriza famílias sem trabalhador mantendo exatamente as vagas reais financiadas e as condições de presença. Todos os 2.500 lares possuíam empregado no primeiro ano. Em 730 dias, 10.131 Sims vivos contra 9.989 no cenário anterior; em 1.095 dias, 2.309 contra 2.359 (2.302 ao término de 1.096 dias). Invariantes monetárias, alimentares e de material passaram. Distribuição mais justa não sanou o déficit agregado.

**Identidade relevante:** nesse teste com cobertura familiar, os salários brutos acumulados no primeiro ano foram 60.810.555 centavos, contra 3.717.399 alimentos comprados × 50 = 185.869.950 centavos de desembolso alimentar real. A folha representa aproximadamente 32,7% desse gasto e os salários líquidos são menores por contribuições. A demanda por comida sozinha não financia renda suficiente para as famílias, dadas a capacidade efetiva de trabalho, as firmas e os parâmetros desta fixture. Não inferir que uma V1 com atividade econômica completa necessariamente falharia, nem inventar dividendos contrários à SPEC.

**Hipótese geracional isolada:** [Actions #39860786690](https://github.com/Marlohn/IndexCities/actions/runs/39860786690), teto demográfico matemático de 80 anos, com idades, morte aos 85, necessidade de dois adultos de 18–59 anos na família, lotação vitalícia de 16 pessoas, intervalo de quatro anos entre nascimentos e agendamento rotativo de uma família por dia; dinheiro, água e alimento foram intencionalmente excluídos. **Não representa uma cidade sustentável nem população economicamente viável.** Mesmo com o agendamento original, restaram 21.010 vivos (21.010 nascimentos, 10.000 mortes), mínimo 10.000 em 29.220 dias. Sem o limite de agendamentos, o teto foi 25.920. Assim, o calendário de nascimentos por si só não prova impossibilidade demográfica; a viabilidade material e familiar conforme SPEC ainda não foi demonstrada.

**Critérios técnicos corrigidos:** registrar o mínimo de vivos também no instante de cada morte, evitando esconder uma queda intradiária; incluir finalização real do arquivo GZip no cronômetro; distinguir sobrevivência de 10 mil só na janela simulada da prova de 80 anos com história. A saída do laboratório declara explicitamente que não valida a SPEC inteira.

**Conclusão sem promoção a produto:** a PR segue isolada e em draft. Não existe prova de uma mesma cidade manter 10 mil Sims por 80 anos contínuos, com história completa, até seis horas. O laboratório omite demanda/mercados, atividades municipais, infraestrutura física, contratos econômicos/familiares e migração da V1; a física viária também é parcial. Mais salários ou natalidade artificiais não resolveriam esse déficit de modelagem. É necessário validar circuitos completos de emprego, receita, capacidade, serviços e pagamento aprovados pela SPEC antes de usar esse laboratório como prova da simulação final.


## 2026-10-22 — preços reais, abastecimento e limite de atendimento no motor isolado

Esta investigação é **exploratória**, isolada na PR #55. NENHUM dos parâmetros de preço, rendimento ou atendimento abaixo foi aprovado como regra de jogo.

**A/B com 10.000 Sims no mesmo mundo por 3.653 dias gregorianos:** [preço derivado dos custos #39864139286](https://github.com/Marlohn/IndexCities/actions/runs/39864139286) elevou a sobrevivência em três anos de 2.349 para 9.367, mas fechou dez anos com 6.841 vivos. A primeira fórmula dividia toda folha de pagamento do comércio pelas poucas vendas realizadas, elevando preços até **42.186 centavos por unidade**, o que amplifica a queda de vendas. [Preço por custo normal de atendimento #39864691247](https://github.com/Marlohn/IndexCities/actions/runs/39864691247) removeu essa explosão (21 centavos/unidade), mas fechou com **6.636 vivos**.

**Capacidade REAL de produção e frete, sem inventar recursos:** o primeiro dimensionamento supunha 30 dias de colheita por pessoa a cada 30 dias, apesar da folga semanal explicitamente simulada. Usar 26 turnos efetivos por mês, com vagas e salários financiados por firmas e trabalho presencial, elevou [a população decenal #39865252370](https://github.com/Marlohn/IndexCities/actions/runs/39865252370) a **8.926**. Havia 3,84 milhões de alimentos nas fazendas, mas sete mercearias vazias e famílias famintas; cada mercearia estava amarrada a uma única fazenda. Permitir frete pago por motorista SIM e entrega física de **qualquer fazenda com estoque para mercearia realmente solvente** elevou [o cenário #39865775711](https://github.com/Marlohn/IndexCities/actions/runs/39865775711) a **9.565**; não havia lojas sem estoque no diagnóstico do décimo ano. A contabilização de salários, alimento, frete, madeira e moeda passou.

**Preço a montante:** no [A/B #39866376785](https://github.com/Marlohn/IndexCities/actions/runs/39866376785), custo salarial efetivamente pago por fazenda dividido pela produção física do período definiu o preço atacadista da fixture (média final de 6 centavos por unidade); as lojas pagaram essas faturas reais e formaram preço de varejo baseado nas compras e capacidade laboral, ainda com margem experimental. Dez anos: **9.867 vivos** contra 9.565, **663.309 refeições não atendidas**. População abaixo dos dez mil em algum momento; NÃO houve teste histórico de 80 anos.

**Falsificação mais importante de fidelidade:** mesmo nos ensaios promissores anteriores, o varejo não limitava unidades vendidas pela capacidade de funcionários presentes: bastava um caixa ativo para transacionar quantidades ilimitadas. [A/B #39867029613](https://github.com/Marlohn/IndexCities/actions/runs/39867029613) impôs reserva de folha no caixa real e o limite opcional de **cinco unidades de alimento por turno presencial**, coerente **apenas com a hipótese de throughput usada para calcular as vagas no mesmo laboratório**. O modelo caiu de **9.867 para 1.955 vivos** após dez anos (**7.157 já no primeiro ano**) e acumulou **1.308.469 refeições não atendidas**. Os dois modelos conservaram dinheiro, alimento e material. **Isso não aprova cinco unidades/turno como produtividade de uma mercearia e não prova que o jogo não pode escalar**. Mostra que os resultados otimistas dependem criticamente de uma capacidade de atendimento implícita e não validada. O serviço físico verdadeiro deve ser calibrado por **visitas de compradores, cestas/itens, turnos, horários e capacidade de prédio**, e vagas precisam ser proporcionais a esse serviço; não presumir que 64 pequenas mercearias possam sustentar milhares de empregos reais ou que um operador atenda clientes ilimitados.

**Conclusão e decisão técnica de pesquisa:** preço, renda familiar, alimentação, capacidade de serviço e trabalho formam uma cadeia acoplada. Não continuar reduzindo preço, ampliando emprego ou mudando natalidade apenas para fazer o indicador de população ficar verde. O cenário de 72 firmas sobre 64 nós, sem a economia completa V1, está mal especificado como cidade sustentável de 10k; e a ausência de atendimento finito impede tratá-lo como validação de fidelidade. A PR continua **draft sem merge**. O orçamento computacional foi promissor nos fragmentos, porém ninguém executou e validou **29.220 dias de um mundo com >=10 mil vivos a todo instante, todos os sistemas da SPEC e histórico inteiro em até seis horas**.


## 2026-10-22 — atendimento por cesta, vagas reais e procura física de outra loja (PR #55)

**Somente experimento isolado, não decisão de produto.** A [SPEC de economia](../spec/economia.md) requer compra presencial, equipe real e paga, atendimento finito, escolha por preço/estoque/acesso e tentativa física de outra loja quando não há atendimento. As constantes são hipóteses de laboratório, não parâmetros aprovados para a V1.

**Correção dimensional:** o ensaio anterior usava *cinco unidades de alimento por turno* tanto para dimensionar empregados quanto para limitar vendas, apesar de um comprador transportar uma cesta de até **32 unidades**. No piloto opt-in de *atendimento por cesta*, cada compra concluída consome **uma quota de checkout** de um trabalhador SIM presente (e não uma por alimento); o salário depende do caixa realmente reservado, há limite experimental de oito funcionários por pequena mercearia e a vaga é dimensionada por cestas/turno × 26 dias efetivos, demanda observada em *visitas de compradores atendidos* (inclusive cestas incompletas) e demanda alimentar prevista. Nenhum empregado, dinheiro, alimento ou viagem é criado para cobrir faltas.

**A/B decenal:** [CI #39868958772](https://github.com/Marlohn/IndexCities/actions/runs/39868958772) executou a mesma cidade de dez mil habitantes por **3.653 dias**. Ao impor limite antigo de cinco **itens**/turno, restaram **1.955 vivos**; com cinco **cestas**/turno, **7.536** (semente 42) e **7.331** (semente 73). Na semente 42 houve ~1,21 milhão de compras físicas e **797.473** visitas recusadas por falta de quota. Checkpoint/resume reproduz o mesmo estado; todos preservaram moeda, comida e madeira. Isso **não valida cinco cestas/turno como capacidade realista**.

**Busca alternativa física:** a fixture de loja substituta permite até três visitas adicionais por caminhos físicos para lojas com estoque, funcionário presente, disponibilidade de atendimento e preço pagável no momento da escolha. [CI #39869316202](https://github.com/Marlohn/IndexCities/actions/runs/39869316202): semente 42, **7.536 → 7.570 vivos**; foram **527.011 viagens de procura** e **301.663 compras** depois da tentativa inicial. Semente 73, **7.331 → 7.391**. Benefício real, insuficiente. O raio de três nós e limite de três visitas são somente calibração de laboratório.

**Sensibilidade conjunta de capacidade, contratação e preço:** um único parâmetro de cestas por turno realimenta as vagas comerciais e o custo unitário da mercadoria; demanda usa número de compras concluídas, não apenas alimento vendido. [CI final #39869954858](https://github.com/Marlohn/IndexCities/actions/runs/39869954858), semente 42, após 10 anos:

| Cestas/turno (hipótese) | Sims vivos | Visitas recusadas por capacidade |
| ---: | ---: | ---: |
| 5 | 7.570 | 1.005.676 |
| 20 | 7.476 | 46.650 |
| 60 | 7.470 | 262 |

Na semente 73, 20 cestas/turno resultaram em **7.264 vivos**. Ampliar atendimento até praticamente eliminar o gargalo de quota **não sustenta a população**. A fixture de vinte cestas tinha somente **720 empregados** e **702 famílias com empregado** entre 1.572 famílias habitadas, com **179.519.343 centavos** distribuídos nos saldos individuais e **mais de 1,5 bilhão de centavos** nos caixas das empresas. Havia alimentos em lojas e fazendas. A falta de trabalho e fluxos de renda real para as famílias continua dominante; **não distribuir lucros fictícios**, vedados pela SPEC para empresas sem proprietário SIM.

**Limites e decisão de pesquisa:** não são simulados ainda tempo de checkout, fila de consumidores, instalações reais por porte, toda a adaptação da colheita aos pedidos, outros mercados de consumo, bens patrimoniais, operações municipais e demais atividades da V1. Esse cenário reduz a cidade a 64 mercearias, oito fazendas e obras pontuais; seus fluxos **não representam uma economia integrada completa**. A melhoria de componentes de varejo não demonstra 10.000 Sims vivos continuamente por 80 anos com histórico completo em seis horas, e nenhuma das capacidades foi promovida à SPEC. PR #55 permanece draft e sem merge. O caminho útil é introduzir **circuitos materiais e financeiros efetivamente aprovados** com contrapartes concretas, e validar os mesmos invariantes e a população sem atalhos.


## 2026-10-22 — experimento de impostos municipais, coleta, tratamento e história persistente em 80 anos

> **Pesquisa somente na PR #55, não aprova mecânicas da V1.** A coleta física de resíduos com pessoal e veículos reais é diretriz do escopo completo em `docs/spec/servicos.md`, mas **o catálogo reduzido da V1 não aprova aqui uma instalação própria de lixo**. O laboratório não representa frota, combustível, valor imobiliário dinâmico, aterro real, equipes e construções com todas as regras aprovadas. Impostos patrimoniais escolhidos abaixo NÃO são taxas de lixo ou preços oficiais.

**Novo fluxo fechado:** tributos patrimoniais realmente pagos por empresas existentes no seu caixa financiam vagas e salários municipais; SIMs percorrem ruas até casas habitadas e retiram somente unidades de resíduos geradas por consumo de alimento, carregando-as para depósito finito de 50 milhões. Na etapa seguinte, outros SIMs reais comparecem fisicamente ao depósito para transformar lotes do estoque bruto em estoque de material tratado também finito (500 milhões). Tratamento não faz lixo sumir. Os materiais permanecem em estoques auditados; a folha sai do Caixa e entra no bolso do trabalhador com contribuição real. Auditorias independentes verificam conservação de moeda, comida, madeira, lixo bruto, carga em trânsito, material tratado e checkpoint exato. **Faltam caminhões com combustível/modelagem plena**, não é aceitação de toda a causalidade da SPEC.

**Resultados com 10 mil SIMs, uma cidade por 3.653 dias:**
- [CI #39874189238](https://github.com/Marlohn/IndexCities/actions/runs/39874189238), com coleta semanal aproximadamente a cada seis dias: base 7.476/7.264 vivos (sementes 42/73) passou a **8.967/9.252**, com ~1,27 milhão de coletas pagas em 10 anos por cidade; recursos conservados.
- [CI #39874791141](https://github.com/Marlohn/IndexCities/actions/runs/39874791141), coleta a cada 3 dias **10.397/10.913 vivos** e a cada 2 dias **12.038/12.230 vivos**, mínimos de 10 mil em ambas sementes. Mais trabalho físico real requer mais impostos reais, não salários gratuitos. Extensão **80 anos sem história reprovada**: depósito bruto encheu exatamente 50 milhões perto do ano 13, interrompendo coleta e causando uma crise sanitária até extinção.
- [CI #39875253414](https://github.com/Marlohn/IndexCities/actions/runs/39875253414), tratamento físico custeado pelo mesmo caixa: coleta a cada 4 dias terminou com 9.802/10.076 vivos, mas mínimos 9.698/9.757, **reprovados**. O tratamento compete com a coleta por dinheiro e pessoas; não criar empregos extras sem orçamento.
- [CI #39875582822](https://github.com/Marlohn/IndexCities/actions/runs/39875582822), coleta 3 dias + tratamento: **11.823/11.977** vivos nos dois decênios, mínimos 10 mil; a 2 dias, **12.393/12.382**. O mesmo cenário de 3 dias estendido a **29.220 dias com histórico integral gravado** terminou em **0 habitantes**, apesar de conservar recursos. Produziu **1.105.972.716 fatos históricos** em **699,4 segundos** (11,7min incluindo GZip). Histórico persistido não significa a meta demográfica satisfeita.
- [CI #39875957327](https://github.com/Marlohn/IndexCities/actions/runs/39875957327), despachante equitativo a todas as residências reais, preservado por checkpoint: com coleta 4 dias, os dois decênios terminaram com **11.221/11.102** (mínimos 10 mil), 0 lares excessivamente sujos e nenhum lar sem visita por 15 dias; com coleta 3 dias, **11.874/12.022**. A execução integral **80 anos com história persistida** (intervalo 4 dias) terminou com **391 habitantes**, min391. Produziu **1.085.146.633 fatos** em **540 segundos** (9min), mantendo material/moeda preservados, mas não a população mínima.
- [CI #39876401491](https://github.com/Marlohn/IndexCities/actions/runs/39876401491), contraprova demográfica barata **sem persistência** por 80 anos: mesma cidade com 4 dias terminou com **391 vivos**, e 3 dias com **0 vivos**. Com 3 dias o mundo tinha **11.903 vivos no ano70** e só 0 no80, apesar de ainda haver dinheiro pessoal. A queda coincide com insolvência em série dos estabelecimentos: tributos e demais despesas reduziram os saldos de empresas privadas ao longo das gerações; ao fim, não existiam empregados suficientes e a economia parou. Não inferir uma fórmula oficial nova de impostos para salvar o laboratório.

**Conquista técnica real e limite:** os dois pilotos concluíram 29.220 dias com mais de um bilhão de fatos individuais fechados em gzip e tempos observados muito menores que seis horas. Contudo, o mundo foi ficando progressivamente menor ao fim, e a computação e simulação **não incluem todos os sistemas da V1/visão**, portanto não comprovam custo de 10 mil SIMs vivos em todo o horizonte nem fidelidade do jogo. Produto requer ainda salário/serviço de água e energia, propriedade, instalações, aquisição de veículo e combustível, logística completa, dinâmica de contratos e mortalidade conforme SPEC. É mais confiável dizer **velocidade demonstrada em laboratório parcial, população secular reprovada**, do que afirmar vitória da meta.

**Hipóteses futuras que merecem falsificação:** receita gerada por compradores e preços que recuperam custos reais de insumos, salários, frete e impostos pagos; limite de serviços sanitários e patrimônio com lastro físico; estoques e investimento/obras reais; equilíbrio tributário sem consumir infinitamente o capital inicial de empresas e sem distribuir lucros proibidos. Não ampliar os empregos ou reduzir o preço só para deixar o censo verde. Usar sempre duas sementes, mínimo intradiário, auditoria de transferências e prova de 80 anos com história persistente.


## 2026-10-23 — duas rotas experimentais para 80 anos contínuos; V1 ainda incompleta (PR #55)

**Somente laboratório C#, não regra aprovada do produto.** As comparações abaixo são uma única cidade por 29.220 dias, com mínimo intradiário e conservação de moeda/alimento/material. Tráfego, empresas, serviços, imóveis, veículos, famílias e mercado permanecem parcialmente simplificados: nada aqui é aceite integral da V1.

- **Receita residencial realmente paga** ([Actions #39879287667](https://github.com/Marlohn/IndexCities/actions/runs/39879287667)): tributação experimental de 0/50/150/300 centavos mensais por residência com dono SIM e dívidas não pagas fora do Caixa. Com recuperação de 25% de custos empresariais pelos preços de vendas reais, houve **891 / 3.434 / 11.692 / 10.639 vivos ao fim de 80 anos**, respectivamente. **150 centavos**: mínimo contínuo **10.000**, 201,6 s **sem histórico**, 312.798.470 centavos pagos efetivamente, 6.443.530 de dívida. Aos 300 centavos, mínimo 9.419: resposta não monotônica. Deeds iniciais e valores são apenas hipóteses.
- **Trabalho físico municipal** ([Actions #39880555800](https://github.com/Marlohn/IndexCities/actions/runs/39880555800)): sem imposto residencial novo, tratamento de resíduos de **50 / 75 / 100 unidades por turno efetivamente pago** teve **891 / 11.915 / 10.557 vivos aos 80 anos**, mínimos **891 / 10.000 / 9.307**. A variante 75 executou 29.220 dias com **1.314.953.566 fatos gravados em GZip**, 614,9 s incluindo encerramento do fluxo. O fingerprint final D56BFF015982DB59 igualou a execução sem gravação. A validação adicional do arquivo abortou por o CI consultar a chave inexistente historyFacts em vez de recordedFacts; a leitura integral precisa ser confirmada por novo run corrigido. Números de unidades/turno não são produtividade V1 aprovada.
- **Contraprova conjunta** ([Actions #39881630774](https://github.com/Marlohn/IndexCities/actions/runs/39881630774)): misturar imposto residencial 150, tratamento 100 e contagem de vagas somente por moradores vivos resultou em mínimos **8.016 / 8.176 / 7.837** nas seeds 42/73/101. **0 de 3** sustenta 10k apesar de cada mudança ter justificativa própria. Melhor throughput reduz turnos e empregos necessários, alterando a renda sem criar/retirar moeda.
- **Lotação residencial viva versus antepassados falecidos** ([Actions #39880967854](https://github.com/Marlohn/IndexCities/actions/runs/39880967854)): corrigir a condição hipotética de até 16 moradores para contar apenas vivos não mudou o desfecho do ensaio com tratamento100 e sem receita residencial: seeds 42/73 terminaram em **10.557 / 10.723**; mínimos **9.307 / 9.488**, idênticos ao controle. Falecidos permanecem registrados no histórico familiar.

**Conclusão exploratória:** há demonstrações relevantes de 80 anos em cidades experimentais com ≥10 mil vivos, mas a região sustentável depende da combinação causal de arrecadação **paga**, emprego **remunerado**, coleta/tratamento **físicos**, alimentação e ciclo familiar. Não extrapolar constantes nem funcionalidades da fixture para a SPEC. O alvo integral depende de história reaberta e verificada, robustez multi-semente e implementação dos demais contratos da V1.


## 2026-10-23 — cenário integrado de infraestrutura e economia (PR #55)

O modo opcional `--integrated-v1-stress` testa de uma só vez **captação e tratamento de água em instalações distintas, energia eólica variável, esgoto em estoque finito com processamento remunerado, importação rodoviária de combustível, abastecimento de fretes e aquisição paga de títulos residenciais**. Os operadores são SIMs presentes fisicamente em instalações distintas, remunerados por turnos pagos pelo Caixa ou pela empresa existente. Produtos e recursos não são creditados enquanto uma entrega física não termina. O teste audita conservação de água, esgoto, energia, combustível, moeda, alimentos e material, além de persistência exata de snapshot.

**Correção obrigatória de fidelidade (antes da prova):** a primeira implementação retinha energia elétrica em estoque entre horas, o que equivalia a uma bateria não aprovada. Essa variante não vale como demonstração de V1 e foi substituída por geração e consumo na mesma hora, com energia não utilizada explicitamente descartada; a manutenção é executada por SIM remunerado e, após longo período sem manutenção, a usina para. Nenhum excedente é transportado gratuitamente entre horas. Os resultados de qualquer CI anterior a esta correção são apenas diagnósticos de um modelo inválido.

**Limite de fidelidade:** as instalações e casas começam presentes como *fixture*, não foram construídas no mundo com todas as etapas/custos de obra; não há rede espacial completa de tubulações/distribuição elétrica, contadores/contratos completos, veículos completos com combustível para o próprio caminhão de importação, nem a oficina/guincho V1. O combustível dos fretes locais é consumido por segmento real; o externo chega por viagem física, com motorista exterior ainda não individualizado. Preços, capacidades, produtividades e propriedades iniciais são **hipóteses experimentais**, não regras aprovadas da V1. **Não atribuir cumprimento integral da SPEC ao resultado**, mesmo se a população ficar acima de 10 mil.

**Validação:** regressão de 120 dias com snapshot e invariantes; execução combinada de três seeds (42, 73, 101) por 29.220 dias cada, começando do controle anterior de tratamento 75, com relatórios diagnósticos e artefatos preservados após falha; persistência e leitura integral de fatos só se a cidade atingir o mínimo contínuo. Workflow [Causal Engine](https://github.com/Marlohn/IndexCities/actions/workflows/causal-engine.yml) acionado no commit da branch experimental. Resultados finais ainda não registrados aqui até validação do CI.
