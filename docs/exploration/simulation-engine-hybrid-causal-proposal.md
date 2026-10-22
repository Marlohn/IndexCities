# Proposta técnica sob teste — motor causal híbrido para 10 mil SIMs e décadas reais

> **Estado: EXPLORAÇÃO, NÃO APROVADA.** Proposta arquitetural condicionada a provas de desempenho e correção, registrada em 2026-10-22 a pedido do responsável. **Não** altera a [SPEC](../SPEC.md), a [ARCHITECTURE](../ARCHITECTURE.md), o escopo V1 ou o código, nem autoriza uma reescrita/merge. As regras aprovadas sempre prevalecem. Não há benchmark deste motor proposto.
>
> **Pergunta:** se projetássemos o núcleo de simulação desde o zero, que arquitetura teria uma chance tecnicamente defensável de executar uma cidade de **pelo menos 10 mil SIMs simultâneos**, acompanhando aproximadamente **80 anos do calendário gregoriano** em poucas horas reais, sem substituir a vida física/econômica por estatísticas ou dias inventados?
>
> **Resposta condicional:** estudar um **motor C#/.NET modular, com um relógio autoritativo global e agenda causal; execução orientada a acontecimentos para vida/economia; e mobilidade híbrida que combina trajetórias válidas por intervalos com resolução física detalhada e compacta quando há conflitos.** A mobilidade densa e a integração causal são os maiores riscos. A escolha só se sustenta se testes representativos mostrarem ganho líquido, regras preservadas e custo viável de manutenção.

## 1. Contrato que nenhuma otimização pode violar

Referências canônicas: [tempo e persistência](../spec/mundo-tempo-saves.md), [população](../spec/populacao.md), [transporte/circulação](../spec/transporte-circulacao.md), [economia](../spec/economia.md), [construção e obras](../spec/construcao.md), [princípios](../spec/principios.md) e [fronteiras de software](../ARCHITECTURE.md).

- **Mesmo calendário para tudo:** dias de 24 horas e datas gregorianas (inclusive anos bissextos); viagem, nascimento, envelhecimento, produção, salários, dívidas e morte obedecem ao mesmo tempo. Não encurtar dias ou acelerar só a idade.
- **Identidade e causalidade individuais:** SIM, família, empresa, imóvel, veículo e recurso mantêm identidade, propriedade, localização, disponibilidade e consequências reais. Fora da câmera não significa fora da simulação.
- **Mobilidade física:** ruas, faixas, pedestres, conflitos, filas, cruzamentos, prioridades, estacionamento, combustível, guincho e entregas têm consequências físicas reais. Nenhum veículo atravessa bloqueio por ter uma chegada prevista.
- **Economia concreta:** compras presenciais exigem viagem, oferta, estoque e dinheiro reais; produção depende de insumos/trabalho/energia; transferências monetárias têm pagador e recebedor; duas pessoas não compram a última unidade ao mesmo tempo.
- **Construção e serviços:** obras dependem de materiais entregues, pessoas/equipamentos e acesso reais; interrupções de infraestrutura propagam suas consequências. Sem conclusão retroativa fictícia.
- **Vida e observabilidade:** a trajetória de um SIM pode ser inspecionada, com histórico de fatos efetivamente ocorridos. O jogador consegue diagnosticar por que algo falhou, sem microgerenciamento individual obrigatório.
- **Sem novas regras de produto escondidas em performance:** escolhas de desempate, política, velocidade, tolerância física, resolução temporal e efeitos sociais permanecem nas fontes aprovadas ou exigem decisão humana quando alterarem comportamento. O motor novo não pode definir isso por conveniência.

A separação Godot (apresentação) / C# puro (simulação), já adotada na ARCHITECTURE, é compatível com esta hipótese. Isto não demonstra que C# será suficientemente rápido nem recomenda trocar de tecnologia.

## 2. Meta e orçamento: hipótese, não compromisso de produto

Para cerca de **29,2 mil dias em 80 anos**, os orçamentos de execução do *mundo inteiro* seriam, aproximadamente:

| Duração real total | Tempo máximo por dia simulado |
| --- | ---: |
| 4 horas | 0,49 segundo |
| **6 horas** | **0,74 segundo** |
| 20 horas | 2,46 segundos |
| 40 horas | 4,93 segundos |

**Seis horas é uma hipótese agressiva proposta para dimensionar a engenharia, não uma meta aprovada na SPEC**, e 20–40 horas são referências de exploração anteriores, não um resultado validado. Hardware de referência, nível de congestionamento e composição da cidade ainda precisam de definição e medição. Contar 10 mil pessoas *ao longo* da história em vez de **10 mil vivas e simuladas simultaneamente** falsearia a avaliação.

Uma divisão provisória do teto de **0,74 s/dia** para revelar gargalos, não para prometer viabilidade: mobilidade 0,30 s; economia/logística 0,14 s; infraestrutura 0,10 s; SIMs/famílias/saúde 0,08 s; persistência/histórico 0,07 s; agendamento/overhead/margem 0,05 s. Se o orçamento for consumido pela mobilidade, otimizar só o trânsito não resolve o jogo.

A pergunta de desempenho não é FPS nem quantos eventos a agenda consegue inserir. É **quantos dias completos, causalmente corretos e representativos, cabem por segundo real** incluindo armazenamento, GC, Godot quando aplicável e os momentos mais congestionados.

## 3. Arquitetura candidata

```text
Godot: interface, mundo visível, entrada, inspeção e snapshots coerentes
                  |
          comandos / consultas
                  v
C# Simulation Core — ÚNICO RELÓGIO GLOBAL
  |-- agenda de próximos fatos e condições
  |-- estado autoritativo por IDs estáveis
  |-- resolução causal e commits ordenados
  |-- SIMs, famílias, trabalho, necessidades e vida
  |-- economia, transações, estoques, produção e logística
  |-- infraestrutura, serviços e obras com índices incrementais
  |-- mobilidade híbrida:
  |     A) trechos previsíveis com horizonte de validade
  |     B) grupos conflitantes com física detalhada em lotes
  |-- RNG reproduzível, invariantes, diagnósticos e métricas
  |
  |-- snapshots versionados de save
  `-- histórico durável de eventos reais, particionado e consultável
```

### 3.1. Tempo, agenda e ordem causal

- Uma única coordenada de tempo autoritativa, preferencialmente representada por inteiro de 64 bits em unidade definida e testada para a precisão necessária. A unidade **não deve** introduzir arredondamentos ou simultaneidades que alterem o contrato físico; a escolha exata é técnica, condicionada a validação.
- Agenda ordenada pelo instante real e por critérios determinísticos compatíveis com as prioridades e desempates da SPEC. Ordenação interna não pode criar regra de gameplay nova. Eventos carregam entidade/versão de validade; os obsoletos são ignorados e contabilizados.
- Resolver **efeitos confirmados** antes de agendar acontecimentos dependentes; leituras especulativas não gastam dinheiro, não reservam estoque nem movem objetos por antecipação. Decisões concorrentes sobre recurso disputado são revalidadas no commit.
- Para efeitos contínuos, materializar o valor correto em qualquer consulta/evento intermediário (por exemplo, produção ou fome cruzando limiar). Sempre agendar também as fronteiras temporais que acordariam esse estado, mesmo sem notificações externas.
- Aleatoriedade precisa ser reproduzível e desacoplada da mera ordem de execução da agenda (por exemplo, fluxos estáveis por entidade/tipo de fato, com contadores explícitos), **sem mudar distribuições nem regras aprovadas**. Verificar replays e saves.

**Não** materializar cada antigo passo de dois segundos como evento: isso apenas deslocaria o gargalo para um heap.

### 3.2. Vida, famílias e escolhas de SIMs

Um SIM continua com idade, relações, moradia, carteira, atividade, necessidades, saúde, posição e trajetória. Não possui `Update()` obrigatório em todo passo global. Suas próximas ações são agendadas por horários, limiares contínuos e fatos externos que tornam necessária reavaliação. Exemplos: acordar, sair para o emprego, chegar fisicamente, comprar, mudar de salário, nascimento, aniversário, separação, falecimento.

Uma empresa falida pode acordar trabalhadores realmente afetados; uma criança sem responsável aciona a cadeia real de cuidado prevista na SPEC; um novo comércio acessível pode atualizar índices de candidatos de compra sem obrigar todo habitante a procurar novamente toda loja.

**Risco:** rotinas e notificações podem gerar milhões de reavaliações simultâneas. Interesses precisam ser indexados por critério específico e medidos. Não usar uma assinatura universal “qualquer alteração econômica acorda todos”.

### 3.3. Mobilidade: dois executores, uma única semântica física

**Candidato A — horizonte físico verificável:** somente quando houver prova das condições do trecho, guardar uma trajetória parametrizada pelo tempo e o primeiro instante em que **qualquer** interação relevante pode mudar seu resultado. Outros veículos podem consultar sua posição/ocupação exatas em instantes intermediários. Entrada de outro veículo, freada, sinal, mudança de faixa ou obstáculo invalida a trajetória antes do efeito físico correspondente. Não presumir velocidade constante se a lei atual do veículo não for constante.

**Candidato B — resolução detalhada em lotes:** trânsito denso, cruzamentos complexos, pedestres, ultrapassagens, fila parada, guincho e manobras sem horizonte demonstrável usam integração física com resolução suficiente para o modelo aprovado. O estado quente (corpos, segmentos, faixas, vizinhos e conflitos) deve usar arrays compactos e índices locais. Essa região ativa pode operar com passos curtos, **sem obrigar regiões independentes a atualizar-se no mesmo ritmo**.

**Contrato entre A e B:** nenhum executor tem autoridade distinta sobre posição, tempo, combustível, desgaste, manobras ou passagem. A transição de um para o outro deve preservar continuidade do estado e mesma ordem causal. Grupos só são independentes se todas as dependências que poderiam cruzar suas fronteiras forem consideradas; separação por mera distância não prova independência. Quando não for possível certificar, ampliar o grupo ou usar o executor detalhado. Não usar relógios locais que adiam o efeito real até o fim do passo externo.

**Risco principal:** um congestionamento pode aproximar a zero os horizontes de validade, provocar invalidação em cascata e tornar tanto a agenda quanto os grupos ativos caros. Num engarrafamento interligado por cruzamentos, o grupo físico pode crescer até quase toda a cidade. Por isso B **não é apenas um fallback para tornar a proposta “correta”**: precisa ser um motor competitivo por si só no pior caso.

**Medições específicas:** eventos previstos por viagem, invalidações por previsão, tempo para detectar conflitos, comprimento de cadeias de propagação de frenagem, custo por atualização física válida, alocações por interação, percentil 99 de processamento de evento e custo de alternância A↔B. É perfeitamente possível reutilizar milhões de previsões e não acelerar o tempo total.

### 3.4. Economia e logística: contas/estoques verdadeiros

- Um ledger de transações, propriedade e estoque por entidade, com valores monetários em unidades exatas e acumuladores de ponto fixo compatíveis com a SPEC.
- Produção pode acumular trabalho/quantidade entre mudanças, mas conclusão de lotes, limites de capacidade, falta de energia/insumo, entregas e compras continuam ocorrendo no instante causal correto. Não creditar lote antes da produção real.
- Compras presenciais: escolha, deslocamento, chegada, disputa por estoque, débito/crédito e eventual nova busca; nenhuma transação “virtual” apenas por estar agendada.
- Ordens de trabalho, caminhões, entregas e construção acordam mediante chegada efetiva e alteração de pré-requisitos (material **E** equipe **E** acesso). Nada se conclui retroativamente por restabelecer o acesso.
- Avaliações independentes podem ser executadas em paralelo sobre snapshot coerente, mas **comitar** compras, reservas, vagas e outros recursos disputados em ordem causal, revalidando as premissas.

### 3.5. Infraestrutura e índices de dependência

Redes, recursos, empresas, imóveis e vagas mantêm índices de alcance, capacidade, ocupação, disponibilidade e consumidores afetados. Ao mudar uma fonte, reavaliar apenas componentes potencialmente atingidos, mas reconhecer que alterações grandes podem exigir uma recomputação completa. Índices são reconstruíveis a partir do estado autoritativo e não podem deixar estoque, energia, acessibilidade ou rotas desatualizados.

**Não** construir um grafo gigante de todas as relações; usar estruturas locais por finalidade. Medir custo de manter índices versus custo de consultas antes de adotá-los.

### 3.6. Memória, histórico, saves e renderização

- **Hot state** em dados compactos indexados por IDs estáveis para movimento, recursos e agenda; objetos C# convencionais para informações pouco acessadas. ECS completo não é premissa.
- **História append-only** de acontecimentos realmente registrados, particionada por período/entidade e comprimida com índices para consultar a vida do SIM. Não descartar silenciosamente fatos nem mantê-los todos carregados em `List<>` de cada cidadão.
- **Snapshots** consistentes do estado e vínculo com o histórico. Save/load/replay preservam agenda, RNG, previsões válidas ou sua reconstrução determinística e efeitos monetários pendentes. Fila de gravação tem limite e backpressure monitorado; `async` não elimina custo de disco.
- **Godot apenas apresenta** o estado. A máxima aceleração precisa executar fora do ritmo do `_Process`, expondo snapshots consistentes. Uma viagem simulada de 20 minutos dura apenas ~10 ms reais se 80 anos couberem em 6 horas: não é realisticamente observável quadro a quadro a 60 FPS. Acompanhar gerações visualmente deve usar inspeção/histórico e velocidades apropriadas, **sem nunca reduzir a física por câmera ou trocar por resultados estatísticos**.
- A execução headless e os testes não dependem da apresentação.

### 3.7. Paralelismo condicionado

Começar com executor serial e índices corretos. Depois, paralelizar avaliações puras, cálculo de rotas independente, integração de componentes físicos certificados independentes e outras tarefas que realmente compensam. Aplicação de efeitos compartilhados mantém ordem determinística. Não dividir arbitrariamente a cidade em relógios geográficos nem usar GPU/threads como resposta antecipada ao gargalo.

## 4. Fatos já observados que contrariam soluções fáceis

As evidências abaixo são de **motores/experimentos do projeto atual**, não benchmarks desta proposta. Branches experimentais não são produção.

| Evidência | Resultado | Implicação |
| --- | --- | --- |
| [E17: perfil de `Step`](https://github.com/Marlohn/IndexCities/actions/runs/39789683967) | 108 SIMs: ~2,965 s/dia, com infraestrutura ~46,62% e necessidades ~20,95%. 400 SIMs/286 veículos: ~371,99 s/dia, trânsito ~93,39%, infraestrutura ~2,77%, necessidades ~2,01%. | O gargalo muda radicalmente com a carga; não basta otimizar trânsito e ignorar outros sistemas. |
| [E15: relógios locais](https://github.com/Marlohn/IndexCities/actions/runs/39784523030) | Separação espacial local alterou posições, desgaste, RNG, combustível e resultados econômicos; ganho aparente ~1%–2%. | Não dividir relógios ou declarar independência por distância. |
| [E18: remoção de limites de rota](https://github.com/Marlohn/IndexCities/actions/runs/39790205008) | Divergência já aos 4 s simulados no caso de 400; mudança posterior de veículos, dinheiro e histórico. | “Previsão provavelmente irrelevante” não é prova de equivalência. |
| [E19: memoização de previsão de rota](https://github.com/Marlohn/IndexCities/actions/runs/39793698254) | **400 SIMs, um dia:** baseline 256,532 s vs. memo 259,152 s; 4.537.262 hits em 5.721.574 consultas (~79,3%), 53.314 invalidações, 217.804 subpassos; 24 checkpoints e SHA final iguais; alocação ~156.233 MB vs. ~156.870 MB. **108 SIMs:** 4,266 s vs. 4,096 s (uma rodada, sujeito a ruído), 24 hashes iguais. | Reutilização de 79% não implicou aceleração integrada de 400; o overhead pode anular o cálculo poupado. **E19 ainda mantém a física atual e não testa o motor híbrido proposto.** |
| [Pesquisas de hipóteses](simulation-engine-radical-hypotheses.md) e [transversal](simulation-engine-cross-domain-research.md) | Horizonte conservador é hipótese; invariantes e dependências ainda precisam de prova. | Não confundir argumento arquitetural com algoritmo comprovado. |

O sucesso de um workflow significa somente que os testes configurados passaram. Hash horário igual reduz risco mas não prova que estados intermediários, situações novas e a futura SPEC expandida sejam equivalentes. Tempo numa execução de runner compartilhado tem ruído; repetir em hardware controlado.

## 5. Principais maneiras de a proposta fracassar

| Falha potencial | Efeito no produto | Prevenção / sinal de descarte |
| --- | --- | --- |
| **Explosão de invalidações e eventos** em filas e interseções | Mais CPU que a física de passo fixo | Medir churn e p99; testar engarrafamento severo; rejeitar agenda detalhada se ela não oferecer benefício líquido e consistente. |
| **Erro no horizonte físico** ou evento não inscrito | Teletransporte lógico, colisão, prioridade errada, divergência econômica | Testes adversariais e comparação do primeiro evento divergente; fallback *antes* de qualquer efeito duvidoso. |
| **Coortes de tráfego falsamente independentes** | Mudança de desgaste, horário, RNG e manobras | Dependências físicas demonstráveis, tempo único e auditoria dos limites entre grupos. |
| **Executor B muito caro em tráfego denso** | Meta impossível justamente com cidade real | Teste de 10 mil SIMs com pico real; orçamento de trânsito; não aprovar a arquitetura só porque A ganha em rua livre. |
| **Eventos de vida/serviços em tempestade de notificações** | Milhões de decisões redundantes e GC | Índices com assinaturas específicas, coalescência *somente quando causalmente equivalente*, comparações integradas. |
| **Dependências desatualizadas** | Venda de produto inexistente, obra impossível ou falha de energia ignorada | Fonte de verdade única, versões, invalidação e índices reconstruíveis. |
| **Paralelismo alterando desempates/RNG** | Resultados não reproduzíveis; dinheiro/estoques errados | Commit serial/ordenado de recursos compartilhados; RNG reproduzível por fato. |
| **Fila e histórico não cabem no orçamento** | Stall do disco, memória crescente, saves inviáveis | Bench de 80 anos e escrita real, particionamento, limites de buffer, consultas e replay. |
| **Complexidade de manutenção supera a equipe** | Toda nova regra exige modificar previsores frágeis | Núcleo enxuto, poucos contratos de dependência, diagnósticos de origem, auditoria da evolução de novas SPECs. |
| **Velocidade extrema versus acompanhamento visual** | Simula corretamente mas não há como observar cada atividade na tela | Separar ritmo de simulação e apresentação, permitir velocidades apropriadas, histórico e inspeção verdadeiros. |

**Risco fundamental sem solução garantida:** podem existir tantas interações físicas e econômicas reais por dia que a máquina de referência simplesmente não consiga processá-las dentro de 0,49–0,74 s, independentemente de agenda, cache, threads ou GPU. A arquitetura não altera esse limite físico de custo computacional.

## 6. Como falsificar barato, antes de comprometer o jogo

Isto é uma **sequência proposta de decisão técnica**, não uma fase obrigatória de POC imposta ao trabalho da V1. Não substituir o jogo atual por protótipos fictícios nem ignorar integração.

**Porta A — referência e custo elementar.** Instrumentar cenários representativos e adversariais: movimento livre, fila densa, quatro entradas bloqueadas, prioridade pela direita, travessia real, estacionamento saturado, guincho e obra com obstrução. Medir tempo por efeito físico, evento, cache, invalidação, índice, RNG e GC. Fixar a semântica física de referência antes de comparar executores novos. Uma física nova não precisa produzir os mesmos bits de todas as aproximações antigas, mas precisa cumprir as regras aprovadas e tolerâncias técnicas definidas antes do teste.

**Porta B — duelo físico A versus B versus referência.** Num **pequeno núcleo independente da UI**, testar trajetórias com horizonte e executor detalhado compacto, incluindo transição nos dois sentidos. Exigir conservação da ocupação dos corpos, ausência de atravessamento, prioridades, posições consultáveis, continuidade, consumo/desgaste e reprodução do primeiro conflito. Registrar percentagem de tempo em cada executor e custo da troca. Se A só melhorar vazio e B não aguentar congestionamento, não prosseguir com uma reescrita geral.

**Porta C — ciclo integrado de verdade.** Conectar viagens reais com salários, consumo presencial, produção, logística, serviços, crianças, saúde e condições de infraestrutura; medir cidades de 108, 400, 1.000, 5.000 e **10.000 SIMs simultâneos**, com cidades densas e cidades com pouca mobilidade, diferentes seeds e horários. Comparar os resultados factuais, invariantes e rastros causais com a referência válida; repetir benchmark para quantificar ruído. Um dia sem viagens não pode representar uma cidade bem-sucedida.

**Porta D — tempo longo e persistência.** Rodar dias, semanas, um ano e o horizonte geracional sem limpar históricos para ganhar memória. Medir crescimento de entidades/índices, alocação total, disco gravado, save/load, consulta de biografias, vazamentos, envelhecimento e custo ao longo de gerações. Um teste de 900 segundos ou de um único dia não prova 80 anos.

### Critérios explícitos de continuação ou abandono

**Prosseguir somente se**, em cenários integrados e representativos: (a) não houver violação causal conhecida; (b) o ganho for **líquido e repetível** contando agenda, invalidações, índices, GC e histórico; (c) o trânsito de pico mantiver custo compatível com o orçamento total da meta escolhida; (d) o custo dos demais domínios tiver margem mensurada; e (e) saves, debug e evolução das regras permanecerem viáveis. Não confundir 3× num kernel isolado com 3× na cidade.

**Interromper ou redesenhar** se o churn de eventos vencer continuamente o executor compacto, se a prova de independência exigir invalidar toda a cidade, se o pior tráfego inviabilizar o orçamento ou se não for possível detectar e reproduzir a primeira divergência. Não continuar por custo afundado.

**Se a meta física não fechar:** manter alternativas abertas (motor de passo compacto com índices; mudanças *aprovadas* na duração-alvo/hardware; alternativas como GPU somente após perfil e custo de transferência). **Nunca** rebaixar silenciosamente fidelidade fora da tela, abreviar calendário, apagar eventos ou alterar resultados econômicos. Qualquer concessão de produto exige decisão humana na SPEC.

## 7. Escolhas explicitamente rejeitadas como premissas

- **Tudo por eventos:** rejeitado como dogma. Congestionamento pode não ter horizonte útil; custo do heap, cancelamentos e previsão pode exceder o do passo físico.
- **Tudo por `Update` fixo de dois segundos:** rejeitado como arquitetura-alvo. Cerca de 43.200 passos globais por dia impõem volume desnecessário a domínios estacionários.
- **Coortes com relógios locais baseadas só em proximidade:** refutadas por E15; equivalência causal não é uma distância arbitrária.
- **Economia estatística/off-camera simplificada:** incompatível com viagens, estoque, dinheiro e causalidade física da SPEC.
- **ECS universal, GPU obrigatória, microserviços, paralelismo indiscriminado, megagrafo de regras:** nenhuma destas escolhas ataca, por si só, a quantidade de interações; só considerar após prova e profiling.
- **Reescrever a V1 agora:** não faz parte desta recomendação. O protótipo da fronteira física deve decidir se existe fundamento para alteração estrutural.

## 8. Parecer final para decisão humana

**Minha aposta técnica permanece um motor causal híbrido**, não um agendador universal superposto à física atual: único relógio, fatos ordenados, agentes individuais, economia transacional, dependências verificáveis e **duas estratégias físicas com a mesma semântica**. O ponto difícil não é criar a agenda, mas **evitar trabalho proporcional a todas as entidades em todos os instantes e, ao mesmo tempo, pagar integralmente pelo custo dos conflitos reais**.

**A aposta ainda não está demonstrada.** Antes de qualquer decisão de reescrita, a pergunta que precisa de resposta reproduzível é: *um executor físico compacto, em congestionamento denso e integrado ao jogo, pode entregar o custo por dia necessário quando escalado a dez mil SIMs, ou o custo inevitável das interações torna a meta de horas inviável?*

Se a resposta for negativa, escolher outra arquitetura por entusiasmo apenas repetirá o fracasso. Se for positiva e houver margem comprovada para economia, infraestrutura, vida e histórico, então haverá fundamento técnico para promover uma decisão estrutural na ARCHITECTURE, após aprovação humana.

### Referências complementares

- [Exploração principal do motor](simulation-engine-performance-research.md)
- [Pesquisa transversal sobre horizontes causais](simulation-engine-cross-domain-research.md)
- [Hipóteses radicais e contraprovas](simulation-engine-radical-hypotheses.md)
- [Análise de cache e equivalência](simulation-cache-feasibility.md)
- [Escala e baselines](simulation-scale.md)
- [Ritmo/velocidades](ritmo-velocidades.md)
- [Hub da exploração](../EXPLORATION.md)
