# Escalabilidade do motor integrado — experimentos

> **Pesquisa exploratória isolada, sem autoridade de produto.** A fonte do produto é `docs/SPEC.md`; regras de trabalho em `AGENTS.md` e arquitetura em `docs/ARCHITECTURE.md`. Esta branch/PR não altera a V1 nem autoriza integração.

## Pergunta central

Qual é a velocidade **efetiva e causalmente correta** do `IndexCities.Simulation` atual, com população, trânsito, economia, serviços, eventos e histórico de pessoas reais do motor? O que impede aproximar uma cidade futura com **10.000 ou mais SIMs** de uma experiência de várias gerações em algumas sessões?

**80 anos em 6 horas é hipótese de benchmark**, não requisito aprovado da V1. O hardware de referência e o ritmo final permanecem em calibração. Nenhuma otimização pode omitir viagens, vidas, transações, estoques, ordem causal ou histórico factual.

## Fronteira e interpretação

- **Esta PR:** benchmark e diagnóstico do **núcleo integrado real** `src/IndexCities.Simulation` por `src/IndexCities.Headless`, com eventual comparação ao host Godot quando existir cenário confiável. Alterações de instrumentação/fixtures só nesta branch, sem alterar produção em `main`.
- **PR #51:** pesquisa E01–E29 de otimização de trânsito, independente e preservada; não adotar candidatos não validados automaticamente.
- **PR #55:** motor C# experimental próprio, simplificado; resultados de décadas e histórico não demonstram desempenho do motor integrado. Preservar a pesquisa e não portar sistemas indiscriminadamente.
- **PR #52/#53:** pesquisa separada; não tratar experimentos como comportamento aprovado.
- A fixture `cycle` usa construção e sistemas reais, mas é pequena; `grid` usa o mesmo `CitySimulation` **com atalhos artificiais de preparação**: conclusão instantânea de obras, capacidade de utilidades ampliada, orçamento elevado, e configurações sintéticas de mapa/famílias. É **teste de estresse técnico, não cidade representativa**. Não declarar prova de 10 mil habitantes a partir dela.
- O motor atual usa `Quantum=2` segundos; **80 anos** corresponderiam a cerca de **1,26 bilhão de passos** se mantido esse executor. Não extrapolar tempo de um único dia linearmente sem verificar aumento do custo ao longo do tempo, população e histórico.
- Teste sem Godot não mede renderização, responsividade visual nem custo da interface.

## Protocolo enxuto

1. Medir a **linha de base do motor real** em Release com cenários existentes `cycle` e `grid`, **duas repetições sequenciais por condição** no mesmo job/runner, seed e parâmetros fixados. Coletar tempo de aquecimento, população final, dias concluídos, tempo real/dia, aceleração efetiva, alocação, GC, memória, tamanho de save, custo save/load, hash e invariantes. A execução de um teste isolado não permite declarar equivalência causal com outra implementação.
2. Confrontar os resultados com a representatividade das fixtures; preparar o **menor cenário integrado com carga realmente exercida**, distinguindo residentes vivos, cidadãos em circulação, veículos efetivos, viagens, turnos, comércio, água/energia/esgoto e persistência. Não “atingir 10 mil” inflando números ou capacidade sem registrar a artificialidade.
3. Aumentar primeiro **carga e horizonte de forma discriminante**, com limites explícitos de custo/timeout e checkpoints, sem lançar 80 anos cegamente. Perfilar somente o gargalo que os dados apontarem. Uma candidata exige A/B reproduzível com estado causal, invariantes e cobertura adversarial, antes de considerar adoção.
4. Quando um gargalo estrutural demonstrado impedir progresso, documentar o limite e reavaliar abordagem. Não colecionar experimentos ou commits sem ganho de informação.

## Primeira execução — baseline C# real

Workflow isolado `.github/workflows/integrated-scale.yml`, disparado por **push de instrumentação/workflow relevante** nesta branch. Começar com `scale cycle` e `scale grid`, `--seed=42 --warmup=1 --days=1 --cap=300`, e grid `--rows=3 --length=400`. Duas repetições por condição; `cap` interrompe só **depois** do dia completo, por isso existe timeout do job. Nenhum número de desempenho é aceito até ler o GitHub Actions correspondente.

**Resultado inicial confirmado:** [GitHub Actions #40060955808](https://github.com/Marlohn/IndexCities/actions/runs/40060955808), SHA `25eebe02f2a5a9b5cea08f13ea530a70db6aa29c`, job `baseline` **success** (2026-10-24 04:07 UTC); compilação .NET 8.0.32 Release aprovada, quatro execuções completas, logs brutos no artefato `integrated-scale-baseline-raw`. Runner Ubuntu, `Environment.ProcessorCount=4`; os tempos são amostras de runner compartilhado, não hardware de referência do jogador.

| Cenário real do núcleo | Repetição 1 | Repetição 2 | SIMs vivos | Veículos existentes | Tempo simulado/s real (aprox.) | Alocação gerenciada/dia | Memória de trabalho reportada | Hash save igual? |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| `cycle`, seed 42, 1 dia medido depois de 1 dia aquecido | **2,53 s/dia** | **2,50 s/dia** | **47** | **42** | 34.176× / 34.579× | **851 MB** | 85–86 MB | **sim** (`7357FCCBAA99AA96`) |
| `grid`, seed 42, 3 linhas/400 m, mesma janela | **5,24 s/dia** | **5,30 s/dia** | **108** | **83** | 16.489× / 16.313× | **1.612 MB** | 90 MB | **sim** (`2D095BC89EB9A34D`) |

As duas medições de cada cenário diferiram aproximadamente 1,1–1,2% entre si; são **somente duas repetições**, não base estatística para declarar ganho. Ambas terminaram com `INVARIANTS OK`, `SAVE` e `SUMMARY days=1`; nenhum cenário possui divergência demonstrada. `cycle`: GC0 51/dia, GC2 1/dia; `grid`: GC0 96/dia, GC2 0/dia. Save/load no runner: `cycle` ~0,19–0,20 s / 0,12–0,13 s para ~0,7 MB; `grid` ~0,19–0,20 s / 0,14–0,15 s para ~1,0 MB. A preparação da fixture e aquecimento **não estão incluídos** no wall/dia.

**Comparação com hipótese de 80 anos/6h:** 29.220 dias exigiriam média de **0,739 s por dia simulado** (≈116.880× efetivos). Mesmo neste ensaio pequeno e simplificado, `cycle` mede 2,50–2,53 s/dia (~3,4× acima desse limite), e `grid` 5,24–5,30 s/dia (~7,1× acima). A saída `hours80y=21/20` e `43/43` do CLI **é projeção linear hipotética, não simulação de 80 anos**; não modela população mutável, dívida histórica, envelhecimento, congestionamento, crescimento do save ou novos sistemas. Estes resultados **não autorizam** concluir se 10 mil SIMs/80 anos caberão ou não: estabelecem apenas que a meta exploratória está longe até em cidades minúsculas.

**Limitação decisiva:** `grid` conclui obras artificialmente, aumenta capacidades de serviços e orçamento; `cycle` só tem 47 cidadãos. A linha `moving=0 travelling=0` no fim do dia NÃO comprova ausência de viagens durante o dia, mas **não há métricas de viagens/carga real durante o dia** nesses logs. A população em `grid` (108) não constitui cenário de 10k, nem valida trânsito congestionado. Invariantes + hash final iguais validam apenas estado final repetido de **mesma implementação**, não equivalência de uma otimização nem toda ordem causal interna. Também não há perfil por sistema, memória histórica de décadas, nem custo Godot/UI. Não atribuir o rendimento medido a uma arquitetura nova, já que nenhum algoritmo foi alterado.

**Próxima pergunta discriminante:** instrumentar **somente observação** de atividade ao longo do dia (movimentos reais, viagens e trabalho/produção/consumo), sem alterar regras da simulação; escolher cidade integrada representativa, ou demonstrar quantitativamente por que a fixture atual não serve. Depois ampliar população/carga progressivamente e perfilar maior gargalo real. Não gastar CI em mais uma repetição idêntica ou rodar décadas cegamente.

## Registro de resultados

Cada entrada nova deve trazer: hipótese; SHA da branch; workflow/job/log; fixture/seed/parametrização e atalhos; **população real e atividade efetiva**; repetições e dispersão; wall/CPU quando disponível, RAM/GC, dias/s real; invariantes/hash/checkpoints; falhas, limites e **decisão técnica**. Se o teste falhar, registrar causa; não declarar ganho por CI verde, nem substituir prova do motor real pelo laboratório da PR #55.

**Próxima decisão:** usar os números do primeiro CI para escolher entre corrigir a fixture, ampliar carga real ou perfilar o gargalo dominante. Não executar automaticamente teste de décadas antes dessa escolha.

## Segunda execução — censo físico real e paridade de granularidade (24/10/2026)

**Evidência material:** [Actions #40061323240](https://github.com/Marlohn/IndexCities/actions/runs/40061323240), SHA `4349a86575782b1e1e689476cfac7b2c74dd60be`, jobs `baseline` e `activity` **success**, logs/artefatos brutos anexados. Release .NET 8.0.32, seed 42, `--warmup=1 --days=1 --cap=300 --rows=3 --length=400`. O `census` chamou `Advance(2)` **43.200 vezes por dia**; `scale` chamou `Advance(86400)`. As duas execuções por fixture passaram invariantes e salvaram o estado; não houve releitura independente de todo o histórico pessoal.

| Medida no dia observado | `cycle` | `grid` |
|---|---:|---:|
| Residentes vivos / veículos existentes | 47 / 42 | 108 / 83 |
| Amostras SIM-quantum / veículo-quantum | 2.030.400 / 1.814.400 | 4.665.600 / 3.585.600 |
| SIMs parados, média por amostra | **98,7%** | **99,1%** |
| SIMs andando, média por amostra | 1,2% | 0,8% |
| Caminhadas detectadas como iniciadas/dia | 610 | 565 |
| Viagens de veículos detectadas como iniciadas/dia | 35 | 83 |
| Veículos viajando, fração de veículo-quantum (arredondada) | 0,0% | 0,1% |
| Veículos em viagem sem espera, fração de viagem-quantum | 97,6% | 83,9% |
| Espera por preferência/sinal/travessia, fração de viagem-quantum | 1,7% | 12,4% |
| `census` wall/dia (com observação por quantum) | 2,80 s | 5,61 s |
| Hash do save `census` vs `scale` da mesma fixture | `7357FCCBAA99AA96` = igual | `2D095BC89EB9A34D` = igual |

**Conclusão:** há viagens e esperas reais **durante** o dia, apesar de `moving=0` ao final; contudo, a carga observada é dominada por agentes parados. Não extrapolar congestionamento de cidade expandida nem presumir que toda a execução pode saltar períodos inativos: trabalho, comércio e interações podem ocorrer sem movimento. Contagens de início detectam transições amostradas, **não** comprovam trabalho físico, produção, consumo ou todas as viagens. Os percentuais de retenção são condicionais a **veículos já em viagem**, não a toda a frota. Igualdade do hash final de **16 dígitos hexadecimais (prefixo SHA-256)** não prova equivalência da ordem de eventos intermediários. Os tempos do censo incluem instrumentação e não devem ser comparados como regressão aos tempos sem censo nem extrapolados a 80 anos. `grid` permanece artificial e `cycle` minúscula.

**Próxima hipótese discriminante, antes de otimizar:** ao passar de `grid --rows=3 --length=400` para `--rows=6 --length=600`, a população/carga e a concorrência efetiva aumentam, ou o motor continua majoritariamente ocioso e o custo por dia cresce sem atividade proporcional? O próximo CI executa controle e degrau **pareados no mesmo runner**, seed 42, `--warmup=1 --days=1 --signals=1`, duas repetições `scale` por condição e um `census` por condição, timeout de 180 s por processo, sem décadas. Mede wall, CPU de processo (shell), GC, RAM, alocação, população, viagens/esperas, picos simultâneos e médias da hora mais movimentada. Compara SHA-256 **completo** do save entre repetições e com o censo, preservando divergência como evidência. Picos são observados a cada 2 s e as horas são janelas contíguas desde o início da medição; não equivalem a observação contínua dentro de cada quantum. Fixture continua com obras concluídas instantaneamente, caixa e utilidades artificialmente elevados: é **degrau de carga**, não demonstração de cidade viável ou de 10 mil SIMs. O resultado só será declarado após ler jobs/logs.

## Terceira execução — carga real pareada e picos simultâneos (24/10/2026)

**Evidência nova:** [Actions #40062113860](https://github.com/Marlohn/IndexCities/actions/runs/40062113860), SHA `084f481b491dcb3a12e01af3258350020f7c3e8a`, job `load-step` **success**, logs e artefato `integrated-scale-load-step-raw`. Release .NET 8, seed 42, `grid --warmup=1 --days=1 --cap=150 --signals=1`; mesmo runner, duas repetições `scale` e um `census` por condição. Controle `--rows=3 --length=400` versus degrau `--rows=6 --length=600`. A fixture ainda conclui obras instantaneamente e injeta capacidade/caixa; não representa cidade sustentável.

| Métrica do dia medido | Controle 3×400 | Degrau 6×600 |
|---|---:|---:|
| SIMs vivos / empregados / veículos | 108 / 108 / 83 | 363 / 334 / 264 |
| Wall `scale`, repetições | **5,25 / 5,38 s/dia** | **20,09 / 19,99 s/dia** |
| Velocidade efetiva (repetições) | 16.465× / 16.050× | 4.301× / 4.323× |
| Alocação gerenciada/dia; working set | 1.612 MB; 88–89 MB | 5.286 MB; 95 MB |
| Histórico de SIMs ao fim do dia | 2.482 fatos | 9.120 fatos |
| Pico simultâneo de SIMs andando / andando ou em veículo | 26 / 29 | 97 / 124 |
| Pico simultâneo de veículos em viagem / retidos | 12 / 5 | 36 / 19 |
| Maior média horária de veículos viajando / retidos | 0,33 / 0,05 | 1,82 / 0,51 |
| Amostras com algum veículo em viagem | 2,2% | 5,1% |
| SIMs parados (amostras) | 99,1% | 98,9% |
| Save / load | 1,0 MB; 0,19 / 0,14 s | 3,1 MB; 0,22 / 0,24–0,25 s |

**Leitura:** população ×3,36, custo diário medido ×3,72–3,83 e alocação ×3,28. Há aumento **real** de deslocamentos e filas (picos amostrados a cada 2 s), mas a maior parte das amostras permanece parada. Isso não demonstra se CPU é gasto com agentes inativos, logística, tráfego ou outros sistemas: fração de agentes em movimento não é perfil de CPU. `TIME cpuUser/cpuSystem` inclui construção da fixture, warmup e save; não atribuir esses totais ao dia medido. Invariantes passaram nas seis execuções. Os hashes SHA-256 completos foram iguais entre as duas repetições e o censo dentro de cada fixture (`2D095B...5C5E7` e `0C5185...A562F`); igualdade do estado final não prova ordem interna de todos os eventos. Nenhum cenário atingiu 10.000 vivos ou mediu Godot.

**Próxima hipótese única (teste curto):** o custo computacional por hora do mesmo dia aumenta nas janelas com deslocamentos simultâneos/filas, ou permanece elevado mesmo quando há quase nenhuma mobilidade? O próximo CI acrescenta **somente observação no Headless**: 24 janelas de uma hora, cada uma com 1.800 quanta de 2 s, registrando wall, CPU de processo, alocação/GC e médias horárias de SIMs/veículos em movimento e retidos. Compara os dois tamanhos, mesma seed, um dia após warmup; timeout 180 s por execução, sem décadas. Valida invariantes e **SHA-256 completo** contra o censo anterior para detectar alteração de estado pela instrumentação. Custos incluem overhead de amostragem, ruído de runner e tarefas econômicas concomitantes; correlação não prova causalidade nem atribuição de hotspot. Se inconclusivo, não repetir o mesmo teste: partir para perfil de subsistemas por investigação isolada.

## Quarta execução — custo por hora versus mobilidade (24/10/2026)

**Resultado material:** [Actions #40063440724](https://github.com/Marlohn/IndexCities/actions/runs/40063440724), SHA `daa5a7bfe15727922b75e18a60e37424fd67281e`, job `hourly-cost` **success** e [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40063440724/artifacts/12547404932). `census grid --seed=42 --warmup=1 --days=1 --cap=150 --signals=1`, controle `--rows=3 --length=400` (108 vivos) e degrau `--rows=6 --length=600` (363 vivos). Ambos percorreram 43.200 quanta de 2 s no dia medido, passaram invariantes e mantiveram **SHA-256 completo do save idêntico** ao censo anterior da mesma fixture. O perfil registra wall/CPU de processo, alocação e GC por hora **incluindo o censo**, não tempo de subsistema nem medição Godot.

| Janela do dia observado | Controle 108 vivos | Degrau 363 vivos |
|---|---:|---:|
| Dia completo `census` (wall; CPU do shell inclui setup/warmup) | **5,69 s/dia** | **21,14 s/dia** |
| Horas 20–22 (sem SIM andando nem veículo viajando): wall por hora | **0,221–0,222 s** | **0,756–0,762 s** |
| Horas 20–22: alocação gerenciada por hora | **63,7 MB** | **192,2 MB** |
| Hora 12 (pico): wall / alocação | **0,276 s / 84,7 MB** | **1,407 s / 361,4 MB** |
| Hora 12: médias de SIMs andando / veículos viajando / retidos | 4,68 / 0,33 / 0,05 | 16,17 / 1,82 / 0,51 |
| Dia completo: GC0 / GC2; working set | 96 / 0; 88 MB | 316 / 0; 101 MB |

**Interpretação e limite:** o degrau ainda consome cerca de 0,76 s e aloca 192 MB **por hora simulada sem movimento observado**; portanto, não se pode atribuir o custo total ao congestionamento. Hora sem movimento **não é hora sem trabalho, transações, revisão de mercado, manutenção ou verificações por quantum**. Ao meio-dia o custo aumenta, mas a correlação com deslocamentos não isola causalidade. A hora 12 no degrau custa ~1,85× a hora 20; as duas executam o mesmo relógio e agentes, mas não as mesmas tarefas. Amostragem, GC, variação do runner e tarefas concomitantes impedem afirmar hotspot. O hash final igual não comprova a ordem completa dos eventos. Fixture ainda usa obras instantâneas, utilidades e caixa ampliados, população <<10k e nenhum Godot.

**Próxima hipótese única, sem otimização prematura:** quais fases do `CitySimulation.Step` consomem tempo mesmo quando não há mobilidade? Instrumentação **somente nesta branch** registra duração agregada por 13 fases já existentes, sem alterar decisões nem pular quanta. A/B no **mesmo runner**, seed 42, um dia após aquecimento, controle `scale` versus `profile` em 3×400 e 6×600, comparando SHA-256 completo, invariantes, 43.200 passos e overhead do observador. Timeout 180 s por processo, sem histórico de décadas. Tempos de fase usam `Stopwatch` e incluem pausas GC, não são CPU exclusivo; `Advance` fora de `Step` e save/load não entram na soma. Se a instrumentação alterar o estado final, o teste deve falhar e a conclusão fica bloqueada.

## Quinta execução — custo por fase no motor integrado (24/10/2026)

**Resultado novo:** [Actions #40064586582](https://github.com/Marlohn/IndexCities/actions/runs/40064586582), SHA `c8968b282fc72ba02034f81d712cca2dc281b7e1`, job `phase-profile` **success**, seed 42, `scale` (controle) versus `profile` (observação) em `grid --warmup=1 --days=1 --cap=150 --signals=1`, dimensões 3×400 (108 vivos) e 6×600 (363 vivos), 43.200 quanta de 2 s. Ambos passaram invariantes, salvaram e releram checkpoints; SHA-256 **completo** idêntico entre controle, observação e censo anterior para cada fixture. Medições: `scale` 5,30 / 20,28 s/dia e `profile` 5,21 / 20,35 s/dia, respectivamente. Diferenças pequenas não provam ganho ou regressão: execução única por modo e ruído do runner.

| Fase de `CitySimulation.Step` | 108 vivos: wall / fração | 363 vivos: wall / fração |
| --- | ---: | ---: |
| Infraestrutura (`UpdateInfrastructure`) | **2,382 s / 45,73%** | **7,372 s / 36,24%** |
| Necessidades (`UpdateNeeds`) | **1,050 s / 20,16%** | **5,188 s / 25,50%** |
| Veículos | 0,491 s / 9,42% | 2,911 s / 14,31% |
| Empresas | 0,532 s / 10,22% | 2,150 s / 10,57% |
| Mobilidade dos cidadãos | 0,260 s / 4,99% | 1,343 s / 6,60% |
| Demais oito fases | 0,493 s / 9,48% | 1,382 s / 6,78% |

**Conclusão:** infraestrutura + necessidades consomem **65,89%** do dia observado com 108 vivos e **61,74%** com 363, mais que veículos + mobilidade (14,41% e 20,91%). A hipótese de que o gargalo principal seja apenas trânsito está **limitada** pelos dados. Não significa que estas fases sejam intrinsecamente ineficientes: há verificações físicas, água, esgoto, energia, poluição, consumo e saúde por quantum. Tempos de `Stopwatch` incluem GC, não são CPU exclusivo; `Advance` fora de `Step`, preparação da fixture, save/load e Godot não foram atribuídos. Cenários são artificiais, <<10 mil vivos, e não demonstram custo do produto expandido.

**Próxima hipótese única (diagnóstico curto, não otimização):** dentro das duas fases dominantes, qual fração vem de presença de trabalhadores/demandas, alocação física de energia/água/esgoto/poluição, amostragem de poluição residencial e necessidades individuais? Acrescentar **somente observação** de oito subetapas já existentes, mantendo controle `scale` vs `profile` no mesmo runner, seed 42, um dia e 43.200 quanta, fixtures 3×400 e 6×600, SHA-256 completo e invariantes. Tempo de subetapa não é CPU exclusiva nem causalidade isolada; medir overhead e não alterar cálculos. Se um custo dominante aparecer, decidir se a próxima comparação deve ser perfil direcionado ou otimização estritamente preservadora de estado. Não iniciar 80 anos ou aumentar população antes de resolver a representatividade.

## Sexta execução — subfases de infraestrutura e necessidades (24/10/2026)

**Evidência material:** [Actions #40065940898](https://github.com/Marlohn/IndexCities/actions/runs/40065940898), SHA `95cb95b67ad161b4e231e6bae81c7031d9a8d4a2`, job `phase-profile` **success**, [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40065940898/artifacts/12549760403). Release .NET 8, seed 42, `grid --warmup=1 --days=1 --cap=150 --signals=1`, `scale` versus `profile` no mesmo runner em 3×400 (108 vivos) e 6×600 (363 vivos). Foram 43.200 quanta de 2 s no dia medido. Os quatro testes passaram invariantes, salvaram e releram o estado, com SHA-256 completo idêntico entre controle/perfil e o controle anterior de cada fixture. Wall medido `scale/profile`: **5,56/5,66 s/dia** (108) e **20,47/20,07 s/dia** (363); a diferença de uma repetição por modo não comprova ganho de desempenho.

| Subfase cronometrada dentro de `Step` | 108 vivos | 363 vivos |
| --- | ---: | ---: |
| Infraestrutura: presença de trabalhadores | 0,395 s (15,51% da fase) | 1,332 s (18,40%) |
| Infraestrutura: demandas | 0,290 s (11,37%) | 1,012 s (13,99%) |
| Infraestrutura: energia | 0,494 s (19,40%) | 1,362 s (18,82%) |
| Infraestrutura: água | 0,394 s (15,46%) | 0,730 s (10,09%) |
| Infraestrutura: esgoto e poluição | 0,539 s (21,15%) | 1,440 s (19,89%) |
| Infraestrutura: finalização | 0,432 s (16,96%) | 1,357 s (18,75%) |
| Necessidades: snapshot, casas e poluição | **0,581 s (52,72% de necessidades)** | **3,134 s (60,45%)** |
| Necessidades: atualização individual | 0,516 s (46,80%) | 2,044 s (39,41%) |

**Interpretação:** com 363 vivos, o maior bloco individual medido é a amostragem de poluição residencial (**3,134 s; ~15,6% do dia de 20,07 s**), que agrega cópia das células, seleção de posições de casas e amostragem. Nenhuma subfase isolada de infraestrutura ultrapassa 1,44 s, embora a fase completa ainda custe 7,238 s. Não atribuir a amostragem apenas ao número de SIMs: seu custo também depende de casas distintas, células de poluição e layout; o benchmark não registrou essas contagens. Tempos de `Stopwatch` incluem GC e não são CPU exclusiva; cenário artificial, sem Godot, com menos de 10 mil habitantes. Hash igual confirma estado final serializado, não toda a ordem interna.

**Próxima hipótese única — sensibilidade à escala, sem otimização ou regra nova:** aumentar de `grid --rows=6 --length=600` para `--rows=10 --length=800` elevará população, prédios e células; medir se custo total e o bloco de poluição crescem mais que a carga observada. O CI seguinte compara `scale` e `profile` no **mesmo runner**, seed 42, `--warmup=1 --days=1 --cap=150 --signals=1`, um dia/43.200 quanta, timeout de **360 s por processo** e **18 min por job**. Exigir invariantes, save relido, hash completo idêntico entre controle e perfil de cada fixture, e hash anterior do controle 6×600. Reportar população efetiva, atividade de emprego/consumo e histórico contados, wall, GC/RAM/alocação e fases; se timeout ou falha, registrar como resultado negativo. **Não** extrapolar para 10 mil/80 anos nem chamar fixture de cidade viável: obras instantâneas, dinheiro e utilidades artificiais continuam presentes; `profile` não mede Godot.

## Sétima execução — expansão 10×800 interrompida antes do dia observado (24/10/2026)

**Falha material:** [Actions #40068952389](https://github.com/Marlohn/IndexCities/actions/runs/40068952389), SHA `e7585e996e83c977a5a0433d35429ced03e754b1`, job `phase-profile` **failure** (exit 124 do `timeout 360s`), [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40068952389/artifacts/12550611866). Release .NET 8, seed 42, `grid --warmup=1 --days=1 --cap=150 --signals=1`. Controle 6×600: 363 vivos/334 empregados/264 veículos, **20,52 s/dia** (`scale`) e **20,21 s/dia** (`profile`), ~5.286 MB alocados/dia, 95 MB de working set, 9.120 fatos de histórico e 1.089 refeições; invariantes e save/load passaram, SHA-256 completo igual entre os modos e ao controle anterior. Na expansão 10×800, a construção levou **45,4 s** e preparou 521 casas, 150 locais de trabalho, 2.428 nós de via e 708 vagas, mas o processo foi interrompido após **360,024 s** totais, **sem concluir nem imprimir o aquecimento de um dia**. Não existem população final, custo/dia, perfil, invariantes ou save relido dessa expansão; o timeout **não comprova travamento nem o gargalo exato**. A primeira tentativa de escalar o dia completo não passou, portanto não há prova de 10 mil SIMs.

**Diagnóstico discriminante seguinte, sem repetir o timeout:** medir **4 horas simuladas reais (14.400 s = 7.200 quanta de 2 s)** a partir da fixture recém-construída (`--warmup=0 --window=14400`) em 6×600 e 10×800, mesma seed 42, `scale` versus `profile` no mesmo runner. O novo parâmetro `--window` pertence apenas ao Headless experimental e mantém `--days` de 86.400 s como padrão; rejeita duração inválida e não altera `CitySimulation`. Coletar tempo real/CPU, construção, população, trabalho/consumo, alocação, GC, memória, 13 fases e 8 subfases; exigir 7.200 passos, invariantes, checkpoint salvo/relido e SHA-256 completo igual entre modos para cada fixture. Timeout **240 s por processo**, job de 14 min. Quatro horas iniciais **não** representam um dia aquecido nem toda a atividade diurna; não comparar seus tempos brutos diretamente com o dia completo anterior, não projetar décadas e não atribuir custo a uma fase sem medir. Se a expansão ainda não completar, registrar o limite e investigar o hotspot estrutural em vez de elevar timeouts.


## Oitava execução — janela curta revela dominância crescente de veículos (24/10/2026)

**Evidência material:** [Actions #40069919906](https://github.com/Marlohn/IndexCities/actions/runs/40069919906), SHA `b681d1a562e034c46d1590e3b59de68a46f379ad`, job `phase-profile` **success**, [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40069919906/artifacts/12550153936). Release .NET 8.0.32, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, 6×600 versus 10×800, `scale`/ `profile` no mesmo runner, **7.200 quanta reais de 2 s por condição**. Invariantes, save/load e SHA-256 completo iguais entre controle e perfil nas duas fixtures; não comprova equivalência da ordem de eventos intermediários.

| Janela inicial de quatro horas | 6×600 | 10×800 |
| --- | ---: | ---: |
| SIMs vivos / empregos atribuídos / veículos | 363 / 334 / 263 | 808 / 708 / 576 |
| Casas / nós viários | 235 / 1.085 | 521 / 2.428 |
| Wall `scale` / `profile` | **8,53 / 8,47 s** | **71,54 / 70,75 s** |
| Avanço efetivo `scale` | **1.688 s simulados/s real** | **201 s simulados/s real** |
| Alocação gerenciada / GC0 / GC2 (`profile`) | 2.582 MB / 155 / 2 | 22.770 MB / 1.367 / 6 |
| Working set (`profile`) / fatos de histórico | 92 MB / 3.702 | 126 MB / 10.228 |
| Fase `vehicles` no `profile` | **5,540 s (65,45%)** | **61,323 s (86,69%)** |
| Fases `infrastructure` + `needs` | 1,609 s | 4,259 s |
| Refeições / salários contabilizados | 0 / 0 | 0 / 0 |

**Conclusão limitada:** na janela inicial, o degrau de 363 para 808 vivos (×2,23) elevou o wall do controle ×8,39 e o tempo da fase de veículos ×11,07; a alocação cresceu ×8,82. **O gargalo dominante passou a ser a fase de veículos**, não a poluição residencial, nesse cenário e período. O perfil não distingue inspeção de entrada de faixa, ordenação, previsões de eventos e movimento físico. Há 576 veículos mas apenas 10 viajando ao final da janela; isto não quantifica a atividade durante os 7.200 quanta. A população nasce durante essas quatro horas (`warmup=0`), sem refeições nem salários, e a fixture injeta caixa/serviços e conclui obras instantaneamente. Portanto não comparar diretamente com um dia aquecido, não inferir cidade economicamente viável, 10 mil Sims, Godot ou décadas.

**Próxima hipótese única, teste curto:** a fase `MoveVehicles` cresce sobretudo por detecção/previsão de entrada na faixa e subdivisão de quanta, ou pela movimentação/colisão propriamente dita? Instrumentar **somente cronômetros e contadores** no wrapper existente, discriminando detecção inicial, atualização operacional, geração de subeventos e movimento, além de passos com/sem subdivisão e número de fatias. Reutilizar as mesmas quatro execuções 4h `scale`/`profile` do workflow, seed 42, 6×600/10×800, timeout 240 s por processo; exigir 7.200 passos, invariantes, save relido e SHA completo igual entre controle/perfil. Não alterar decisões, quantização, ordenação, viagens nem produção Godot. Tempos incluem GC e overhead; uma janela inicial não representa um dia completo.

## Nona execução — o custo está no movimento, não na previsão de entradas (24/10/2026)

**Resultado material:** [Actions #40071042363](https://github.com/Marlohn/IndexCities/actions/runs/40071042363), SHA `9f0a579230386d82317488346e73296bc878ea78`, job `phase-profile` **success**, [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40071042363/artifacts/12551317580). .NET 8.0.32 Release, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, 6×600 e 10×800, `scale` vs `profile` em cada fixture. Foram 7.200 quanta reais de 2 s por condição; invariantes e save/load passaram, SHA-256 **completo** igual entre controle e perfil. Hash igual não demonstra a ordem de todos os eventos intermediários.

| Janela inicial de 4h | 6×600 (363 vivos; 263 veículos) | 10×800 (808 vivos; 576 veículos) |
| --- | ---: | ---: |
| Wall `scale/profile` | 6,21 / 6,20 s | 54,98 / 53,85 s |
| Avanço efetivo `scale` | 2.321 s simulados/s real | 262 s simulados/s real |
| Fase `vehicles` | 4,116 s (66,40%) | 47,026 s (87,34%) |
| Subetapa `movement` | **4,055 s (98,53% de veículos)** | **45,946 s (97,70% de veículos)** |
| `entry-check` / `operations` / `entry-events` | 0,046 / 0,001 / 0,013 s | 0,080 / 0,255 / 0,743 s |
| Passos subdivididos / fatias | 24 / 87 | 5.407 / 13.689 |
| Alocação gerenciada; GC0 / GC2 | 2.582 MB; 155 / 2 | 22.770 MB; 1.367 / 6 |
| Fatos de histórico em memória; refeições / salários | 3.702; 0 / 0 | 10.228; 0 / 0 |

**Interpretação:** o custo dominante está em `MoveVehiclesUnsplit` (inclui passos não subdivididos), não na detecção inicial de entrada nem na previsão de eventos. O código contém duas varreduras completas de `State.Vehicles` por veículo em movimento, sugerindo potencial crescimento quadrático; os dados ainda **não atribuem** o tempo dessas varreduras separadamente de manobras e colisões. Diferenças de wall entre runs podem ser ruído do runner. Fixture sem aquecimento econômico, com obras instantâneas, caixa/capacidade artificiais, população <10 mil, sem Godot; não representa cidade sustentável ou 24h de atividade.

**Próxima hipótese única:** varreduras de líderes na faixa e na curva explicam parcela majoritária do bloco `movement`? Instrumentar apenas cronômetros e número de candidatos efetivamente enumerados, sem alterar ordenação, física ou decisões; comparar os mesmos 6×600 e 10×800, seed 42, 4h, `scale` vs `profile` e exigir 7.200 quanta, invariantes, save relido e SHA-256 completo idêntico. Se forem minoritárias, investigar manobras/geometria antes de otimizar. Cronômetros incluem GC e overhead; não extrapolar 4h iniciais para 80 anos.

## Décima execução — duas varreduras respondem por ~40% do movimento (24/10/2026)

**Resultado material:** [Actions #40072673296](https://github.com/Marlohn/IndexCities/actions/runs/40072673296), SHA `51acd67df4d52d5cf8fc7a7fd94f1d758f159d9b`, job `phase-profile` **success**, [logs brutos](https://github.com/Marlohn/IndexCities/actions/runs/40072673296/artifacts/12551868429). Release .NET 8, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, controle `scale` e `profile` em 6×600 e 10×800. Cada condição executou 7.200 quanta de 2 s; invariantes e save/load passaram e os SHA-256 **completos** dos saves coincidiram entre controle/perfil dentro da mesma fixture.

| Janela inicial de 4h | 6×600 (363 vivos; 263 veículos) | 10×800 (808 vivos; 576 veículos) |
| --- | ---: | ---: |
| Wall `scale/profile` | 10,65 / 10,53 s | 91,37 / 93,34 s |
| Velocidade efetiva `scale` | 1.352 s simulados/s real | 158 s simulados/s real |
| Fase veículos / subfase movimento | 6,809 / 6,716 s | 81,342 / 79,468 s |
| Varredura de líderes na faixa | **2,057 s (30,63% do movimento)** | **24,006 s (30,21%)** |
| Varredura de líderes em curvas | **0,705 s (10,50% do movimento)** | **7,416 s (9,33%)** |
| Visitas por varredura (faixa e curva, cada) | 34.013.001 | 450.793.152 |
| Invocações médias de cada varredura por quantum | 17,96 | 108,70 |
| Alocação / GC0 / GC2; working set (`profile`) | 2.582 MB / 155 / 2; 91 MB | 22.770 MB / 1.367 / 6; 126 MB |

**Conclusão discriminante:** as duas varreduras integrais de `State.Vehicles` respondem por **41,13%** do tempo de `movement` na fixture média e **39,54%** na ampliada. Não constituem maioria: aproximadamente 60% ainda pertence a manobras, verificações de geometria, outros percursos e overhead. A contagem de visitas aumenta ×13,25 ao expandir a frota ×2,19 e as invocações de varredura por quantum ×6,05, coerente com custo multiplicativo de frota e atividade. **Visita não significa bloqueador relevante**; as medições atuais não contam quantos veículos efetivamente satisfazem as condições de contenção. Os cronômetros incluem GC/overhead e a diferença de wall em relação a CIs anteriores é ruído não controlado; não atribuir causalidade apenas à comparação entre execuções. A janela é inicial, sem salários/refeições, com obras e caixa/serviços artificiais, <<10 mil vivos e sem Godot.

**Próxima hipótese única, sem alterar a física:** apenas uma pequena fração das centenas de milhões de visitas pode efetivamente bloquear a faixa/curva. Contar chamadas a `HoldFor` (candidatos de contenção, não necessariamente o bloqueador final) e entradas em `bendLeaders` (candidatos geométricos), preservando a ordem de varredura, e reportar `hits/visits`. Reutilizar a janela 4h, seed 42, controle `scale` versus `profile`, 7.200 quanta, invariantes, save relido e SHA-256 completo igual. Se a fração for alta, índice espacial pode não ser o primeiro alvo; se baixa, investigar estrutura de candidatos com prova de equivalência causal antes de otimizar. Não criar novo mecanismo de trânsito nesta medição.

## Décima primeira execução — quase todas as visitas são irrelevantes para contenção (24/10/2026)

**Evidência material:** [Actions #40075037628](https://github.com/Marlohn/IndexCities/actions/runs/40075037628), SHA `21f3f6b462e12a669d3c2fcafdd31f44280e9f92`, job `phase-profile` **success** ([logs do job](https://github.com/Marlohn/IndexCities/actions/runs/40075037628/job/120816464617)). .NET 8 Release, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, `--rows=6 --length=600` e `--rows=10 --length=800`, modos `scale`/ `profile`. Foram **7.200 quanta reais de 2 s** em cada condição. Todas passaram invariantes e save/load; SHA-256 **completo** dos saves coincide entre controle/perfil e com os resultados anteriores das respectivas fixtures. Hash final igual não comprova igualdade de toda a sequência interna.

| Janela inicial 4h, seed 42 | 6×600: 363 vivos, 263 veículos | 10×800: 808 vivos, 576 veículos |
| --- | ---: | ---: |
| Wall `scale/profile` (s) | 10,55 / 10,77 | 93,57 / 93,55 |
| Avanço efetivo `scale` (s simulado/s real) | 1.365 | 154 |
| Movimento / varredura de faixa / curva (s, `profile`) | 6,856 / 2,064 / 0,703 | 79,460 / 24,310 / 6,512 |
| Visitas da varredura de faixa | 34.013.001 | 450.793.152 |
| Chamadas a `HoldFor` na faixa | **874.486 (2,5710% das visitas)** | **5.030.690 (1,1160%)** |
| Visitas da varredura de curva | 34.013.001 | 450.793.152 |
| Candidatos geométricos de curva | **22 (~0,000065%)** | **376 (~0,000083%)** |
| Alocação gerenciada / GC0 / GC2 | 2.576 MB / 155 / 2 | 22.743 MB / 1.364 / 6 |
| Histórico em memória / refeições / salários | 3.702 / 0 / 0 | 10.228 / 0 / 0 |

**Conclusão discriminante:** a grande maioria das visitas não produz sequer um candidato de contenção. As duas varreduras custaram **~40,4% do movimento** em 6×600 e **~38,8%** em 10×800; sua eliminação integral, mesmo se fosse semanticamente possível, **não** resolveria sozinha os ~60% restantes do movimento. `HoldFor` pode ser chamado para candidatos que não viram bloqueador final; os 22/376 candidatos de curva são contagens após predicados geométricos, não colisões reais. A baixa seletividade dá fundamento para investigar uma filtragem espacial **sem mudar ordem de candidatos, estados mutáveis ou desempates por ID**, mas **não prova** que um índice terá custo menor ou equivalência causal. Uma otimização candidata exigirá controle A/B, invariantes, save relido, hash completo e testes de trânsito adversariais antes de qualquer adoção.

**Limites:** janela inicial sem refeições/salários, fixture com obras instantâneas e dinheiro/serviços artificiais, menos de 10 mil SIMs, sem Godot e sem história individual externa persistida. `profile` mede wall com GC e overhead; a variação entre CIs não demonstra aceleração ou regressão. Próxima incerteza de maior valor: se é possível reduzir visitas por filtro espacial **exato e atualizado após movimento**, preservando todas as exceções de acesso à faixa, veículos parados e curvas; não implementar aproximação que omita colisões/causalidade.

## Décima segunda execução — alcance dos candidatos de contenção (24/10/2026)

**Evidência material:** [Actions #40078072059](https://github.com/Marlohn/IndexCities/actions/runs/40078072059), SHA `3baf62d286e93244cab6b45594622074592ecd75`, job `phase-profile` **success**. .NET 8 Release, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, controle `scale` e observação `profile` em `--rows=6 --length=600` e `--rows=10 --length=800`. Cada condição executou 7.200 quanta de 2 s (4 horas iniciais); invariantes, save/load e SHA-256 completo idêntico entre controle e perfil por fixture. Nenhuma alteração na ordem de movimento ou nas decisões de contenção.

| Medida em 4h, seed 42 | 6×600 | 10×800 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregos atribuídos | 363 / 263 / 334 | 808 / 576 / 708 |
| Wall controle / perfil | 10,63 / 10,69 s | 93,99 / 93,30 s |
| Avanço efetivo controle | 1.355× | 153× |
| Visitas à varredura de faixa / candidatos `HoldFor` | 34.013.001 / 874.486 | 450.793.152 / 5.030.690 |
| Candidatos a até 32 m | 305.247 (34,91%) | 1.791.821 (35,62%) |
| Candidatos de 32 a 128 m | 427.834 | 2.296.344 |
| Candidatos de 128 a 512 m | 135.471 | 813.923 |
| Candidatos **além de 512 m** | **5.934 (0,68%)** | **128.602 (2,56%)** |
| Maior separação entre veículo e candidato | 650,46 m | 850,46 m |
| Candidatos com projeção de entrada de faixa | 58 | 13.965 |
| Movimento / duas varreduras (perfil) | 6,776 / 2,771 s | 79,295 / 31,964 s |
| Alocação / GC0 / GC2 / working set (perfil) | 2.576 MB / 155 / 2 / 91 MB | 22.743 MB / 1.364 / 6 / 101 MB |
| Histórico em memória / refeições / salários | 3.702 / 0 / 0 | 10.228 / 0 / 0 |

**Conclusão:** mesmo entre os raros candidatos da varredura de faixa, existem `HoldFor` além de 512 m. **Descartar candidatos por raio fixo de 32/128/512 m não preserva automaticamente a semântica**, especialmente para acesso à faixa, ultrapassagem e distância projetada ao longo da via. Porém, `HoldFor` contabiliza todos os candidatos, não apenas o **líder mais próximo selecionado**; esses números ainda não provam que um candidato distante limita o passo ou causa uma espera. O filtro exato continua hipótese não validada. Runner compartilhado, instrumentação com overhead, janela inicial sem refeições/salários, fixture artificial com obras/caixa/serviços ampliados, menos de 10 mil vivos, sem Godot e sem persistência histórica individual externa.

**Próxima hipótese única, com limite explícito:** os candidatos distantes podem ser frequentes mas nunca ser o líder efetivamente selecionado nem limitar o deslocamento alcançável no quantum. Instrumentar somente contagem e distribuição espacial do **líder vencedor** da varredura já existente e se a distância longitudinal selecionada pode limitar o passo livre `min(distância ao alvo, velocidade×tempo restante)` após a folga de 8 m, **antes** de cruzamentos/geometria. Não é medição de colisão nem autorização para remover candidatos; mesmo ausência de vencedores distantes nesta fixture não prova equivalência global. Reutilizar o mesmo A/B `scale` vs `profile`, seed 42, 6×600/10×800, 4h/7.200 quanta, invariantes, save relido e SHA-256 completo. Nenhum salto temporal, regra ou algoritmo de movimento alterado.

## Décima terceira execução — líderes distantes existem, mas não limitaram o passo livre observado (24/10/2026)

**Evidência material:** [Actions #40079377259](https://github.com/Marlohn/IndexCities/actions/runs/40079377259), SHA `ad08d7f96abfad60440964ecfa31d8608c26527c`, job `phase-profile` **success**, [logs](https://github.com/Marlohn/IndexCities/actions/runs/40079377259/job/120829530361). .NET 8 Release, seed 42, `grid --warmup=0 --days=1 --window=14400 --cap=150 --signals=1`, `--rows=6 --length=600` e `--rows=10 --length=800`, modos `scale` e `profile` na mesma execução. **7.200 quanta de 2 s** por condição, invariantes e save/load aprovados; SHA-256 completo do save idêntico entre controle/perfil por fixture (6×600 `7B3E15DB17B6D06A86DBDF974719F74F990551F573C8B572486E31CE6D698E11`; 10×800 `FC17D58BC43299549841464041E4E65F5FD24D869858F2F17A37267FB05AD307`). Igualdade do estado final não demonstra igualdade de cada evento interno.

| Janela inicial de 4h, seed 42 | 6×600 | 10×800 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregos atribuídos | 363 / 263 / 334 | 808 / 576 / 708 |
| Wall controle / perfil | 10,94 / 11,08 s | 95,87 / 94,94 s |
| Avanço efetivo controle | 1.316 s simulados/s real | 150 s simulados/s real |
| Líderes selecionados pela varredura | 109.699 | 672.903 |
| Líderes selecionados a até 32 m | 94.712 | 582.487 |
| Líderes selecionados de 32 a 128 m | 12.505 | 62.504 |
| Líderes selecionados de 128 a 512 m | 2.479 | 26.905 |
| Líderes selecionados **além de 512 m** | **3** | **1.007** |
| Limitações potenciais do passo livre pelo líder | 74.390 | 487.639 |
| Limitações potenciais com líder **além de 32 m** | **0** | **0** |
| Maior distância euclidiana até líder selecionado | 546,46 m | 748,31 m |
| Varreduras faixa + curva / movimento (perfil) | 2,899 / 7,028 s | 30,897 / 79,921 s |
| Alocação / GC0 / GC2 / working set (perfil) | 2.576 MB / 155 / 2 / 91 MB | 22.743 MB / 1.364 / 6 / 102 MB |
| Histórico em memória / refeições / salários | 3.702 / 0 / 0 | 10.228 / 0 / 0 |

**Conclusão discriminante:** líderes distantes (>512 m) **podem vencer a seleção** (3/1.007 ocorrências), portanto não se pode alegar que o líder selecionado é sempre local. Contudo, nesta janela, **nenhum líder selecionado a mais de 32 m** reduziu o passo livre `min(distância ao alvo, velocidade × tempo restante)` pela folga de 8 m. Isso é observação restrita à fixture e à comparação prévia a cruzamentos/geometria, **não prova** que remover líderes distantes preserve manobras, decisões posteriores, colisões ou casos adversariais. A ausência de limitação observada não autoriza raio fixo nem mudança de regra; um filtro futuro precisa ser exato em coordenadas viárias, candidatos de entrada, estados mutáveis, ordem e desempates. O custo de varredura continua relevante, mas cerca de 60% do movimento fica fora dessas duas varreduras.

**Limites e próximo teste:** janela inicial de 4h sem salários/refeições, obras instantâneas, dinheiro/serviços artificiais, população <<10 mil, sem Godot e sem histórico externo gravado. Tempos incluem GC e ruído de runner. Antes de implementar índice espacial, construir **casos adversariais diferenciais curtos** (curvas, entrada de faixa, líderes parados, mesma posição, desempates, mudança de segmento) e demonstrar seleção e decisão idênticas contra a varredura completa; só depois medir custo de um filtro exato, sem mudar física ou ordem causal. Não repetir esta bateria apenas para acumular CI.


## Décima quarta execução — seed 73 não concluiu a janela de quatro horas (24/10/2026)

**Falha material, não divergência causal demonstrada:** [Actions #40081418663](https://github.com/Marlohn/IndexCities/actions/runs/40081418663), SHA `95c97cf64c3eeefb3fa76a8be26ff5acf80fb0f1`, job `phase-profile` **failure**, exit **124** de `timeout 240s`. A tentativa era `grid --seed=73 --warmup=0 --days=1 --window=14400 --cap=150 --rows=10 --length=800 --signals=1` em Release .NET 8, seguida de `profile` e seed 101. Compilação passou sem warnings/erros; a fixture de seed 73 preparou 521 casas, 150 locais de trabalho, 2.428 nós viários, 708 vagas e 57 veículos iniciais em **44,7 s**. A primeira execução `scale` consumiu **240,026 s de wall total**, cerca de **195 s após a preparação**, sem emitir `WINDOW`, `SUMMARY`, `SAVE` ou `INVARIANTS`. O processo foi interrompido pelo limite; **não existem população final, hash, atividade ou perfil dessa seed**. A seed 101 nem começou. Não chamar o timeout de deadlock, falha de conservação ou prova de impossibilidade de completar a simulação. Para referência, a seed 42 da mesma fixture/4h havia concluído em **95,87 s** de simulação no CI #40079377259; comparações entre runners são indicativas, não prova de efeito exclusivo da seed.

**Próxima hipótese discriminante e barata:** a diferença de custo da seed 73 já surge **na primeira hora**, ou cresce somente nas horas seguintes? Sem mexer em física, produção, trânsito ou eventos, reduzir a janela de ambas as seeds **42 e 73** para **3.600 segundos reais simulados (1.800 quanta de 2 s)** e executar `scale`/`profile` por seed na mesma fixture 10×800 e mesmo runner. Timeout **180 s por processo**, job **14 min**, sem warmup; comparar wall/CPU, alocação/GC, frota/atividade, subfases de veículos, líderes e visitas, exigir invariantes, save/load e SHA-256 completo igual entre controle/perfil. Não executar seed 101 nem repetir 4h até localizar a divergência de custo. Uma hora inicial sem refeições/salários, fixture com caixa/serviços e obras artificiais e menos de 10 mil vivos **não** representa a cidade completa; eventual hash final igual não prova igualdade da ordem interna.

## Décima quinta execução — primeira hora não explica sozinha o timeout de quatro horas (24/10/2026)

**Evidência material:** [Actions #40083017553](https://github.com/Marlohn/IndexCities/actions/runs/40083017553), SHA `42b83a2fa50a297e93462e99a7daf2492865bb87`, job `phase-profile` **success**, [logs](https://github.com/Marlohn/IndexCities/actions/runs/40083017553/job/120840255091). .NET 8 Release, fixture `grid --warmup=0 --days=1 --window=3600 --cap=150 --rows=10 --length=800 --signals=1`, seeds **42 e 73**, modos `scale`/ `profile`, 1.800 quanta causais de 2 s por condição. Compilação, invariantes e save/load passaram; SHA-256 **completo** coincide entre controle e perfil por seed. Nenhum resultado de 4h da seed 73 foi recuperado do timeout anterior.

| Medida na primeira hora | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregados | 808 / 576 / 558 | 812 / 578 / 483 |
| Wall da janela `scale/profile` | 43,80 / 43,96 s | 48,87 / 48,48 s |
| Avanço efetivo `scale` | **82 s simulados/s real** | **74 s simulados/s real** |
| Tempo total incluindo preparação `scale/profile` | 85,79 / 84,83 s | 89,62 / 89,16 s |
| Alocação por janela / GC0 / GC2 (`profile`) | 12.408 MB / 496 / 2 | 13.340 MB / 534 / 3 |
| Working set / histórico em memória | 115 MB / 3.842 fatos | 123 MB / 3.775 fatos |
| Fase veículos / movimento (`profile`) | 41,440 / 41,256 s | 45,989 / 45,757 s |
| Varredura de faixa: visitas / tempo | 292.554.432 / 13,195 s | 318.319.301 / 14,671 s |
| Varredura de curva: visitas / tempo | 292.554.432 / 3,522 s | 318.319.301 / 3,892 s |
| Líderes selecionados / limitadores potenciais | 483.345 / 386.677 | 525.254 / 432.924 |
| Limitadores potenciais com líder a mais de 32 m | **0** | **0** |
| SHA controle/perfil | `05C5084ED7E558719BAB61D68E39429D35D779EA770FF9DEE1802B3262495F0A` | `B759A461605F72E8B97D676481E45A2582C1CD8110C434D7E261D3EBF7923A1E` |

**Conclusão discriminante:** a seed 73 já é **~11,6% mais lenta** que 42 na primeira hora (48,87 versus 43,80 s), com 8,8% mais visitas por varredura, ~7,5% mais alocação e menos empregados, apesar de frota quase igual. O movimento responde por ~99,5% da fase de veículos e por ~94–95% do tempo medido. Isso **não** explica, por si só, a diferença observada entre 4h da seed 42 (~95,87 s de janela em outra execução) e o timeout da seed 73 (>195 s de processamento após preparação); o crescimento relativo depois da primeira hora é hipótese, **não fato demonstrado**, porque os runners e a janela anterior diferem. O timeout de 4h não provou deadlock, divergência causal ou violação de invariantes. Nesta primeira hora não houve refeições/salários; o histórico é somente em memória, sem arquivo individual externo. Fixture artificial, <10 mil SIMs, sem Godot; hash final igual não demonstra igualdade da sequência interna.

**Próximo teste de uma hipótese:** comparar seeds 42/73 na mesma fixture por **duas horas contínuas** (3.600 quanta de 2 s), `scale` versus `profile`, com limite proporcional, para verificar se a diferença de custo cresce após a primeira hora. Não repetir 4h nem acrescentar novas regras; exigir invariantes, save/load e hash completo igual entre controle/perfil por seed. Caso a diferença aumente, investigar os contadores de frota, varreduras e movimento antes de otimizar.
