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


## Décima sexta execução — explosão de subpassos na segunda hora, não apenas diferença de seed (24/10/2026)

**Evidência material:** [Actions #40083956162](https://github.com/Marlohn/IndexCities/actions/runs/40083956162), SHA `593e0bc43c4ee7f4749cd1a88f2da605e409db2b`, job `phase-profile` **success**, [artefato bruto](https://github.com/Marlohn/IndexCities/actions/runs/40083956162/artifacts/12556169064). Mesmo runner, .NET 8 Release, `grid --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`, seeds 42/73, controle `scale` e observação `profile`. Cada execução percorreu **3.600 quanta causais de 2 s**, passou invariantes, save/load e SHA-256 **completo idêntico entre controle/perfil por seed** (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`). Os saves de 1h e 2h são estados de instantes diferentes e **não** devem ter hashes iguais.

| Medida na janela contínua de 2h | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregos atribuídos | 808 / 576 / 708 | 812 / 578 / 708 |
| Wall `scale/profile` da janela | **64,87 / 65,34 s** | **120,86 / 115,83 s** |
| Avanço efetivo `scale` | **111 s simulados/s real** | **60 s simulados/s real** |
| Alocação / GC0 / GC2 (`profile`) | 18.360 MB / 223 / 3 | 35.456 MB / 427 / 3 |
| Movimento de veículos / duas varreduras (`profile`) | 57,397 / 23,151 s | 104,620 / 33,381 s |
| Quanta com subpassos / subpassos totais | 1.807 / 5.386 | 1.807 / 24.128 |
| Visitas por varredura de líderes | 388.679.616 | 786.637.443 |
| Veículos em viagem no fim / histórico em memória | 9 / 8.807 fatos | 29 / 8.596 fatos |

**Diagnóstico novo ao combinar contadores determinísticos deste CI com o teste anterior de 1h** ([#40083017553](https://github.com/Marlohn/IndexCities/actions/runs/40083017553), SHA `42b83a2fa50a297e93462e99a7daf2492865bb87`, mesmas flags exceto `--window=3600`): os **primeiros 1.800 quanta** tiveram somente **11 quanta fracionados** em ambas as seeds, com 141 subpassos (42) e 69 (73). Por subtração dos contadores acumulados, os **1.800 quanta seguintes** tiveram **1.796 quanta fracionados** em ambas, porém **5.245 subpassos na seed 42 contra 24.059 na 73** (médias **2,92 vs 13,40** por quantum fracionado). Visitas adicionais por varredura: **96.125.184 (42) vs 468.318.142 (73)**, diferença **4,87×** apesar da frota praticamente igual. Alocação adicional aproximada por janela: **5.952 MB vs 22.116 MB**. Os contadores acumulados são comparáveis porque o código, o estado inicial e as flags são os mesmos; **wall por hora inferido subtraindo CIs diferentes não é medida controlada**, portanto não se atribui tempo exato à segunda hora. No CI pareado de 2h, a seed 73 consumiu **1,86×** o wall do controle 42.

**Causa ainda não provada:** `MoveVehicles` em `Mobility.LaneEntry.cs` divide o quantum em eventos quando há entrada de faixa e reexecuta movimento e seleção de candidatos a cada subpasso. O salto para quase todos os quanta fracionados após 1h e a multiplicação de subpassos/visitas são um **alvo mais específico** que otimizar genericamente o raio de líderes. Ainda não sabemos se o fracionamento extra na seed 73 vem de tempos até mudança de segmento/combustível, projeções de desbloqueio de entrada ou rechecagens repetidas; **não** remover eventos, pular quanta ou trocar ordem de decisões sem teste diferencial. O mesmo teste teve **zero refeições e salários**, apenas histórico em memória, obras instantâneas e recursos/caixa artificiais, <<10 mil SIMs e nenhum Godot: não demonstra velocidade sustentável do jogo expandido.

**Próxima hipótese única e teste dirigido:** instrumentar *somente observação* em `NextVehicleMovementEvent` para contar, por seed e por primeira/segunda hora, quantas vezes o limite do subpasso veio de `TimeToMovementChange` versus projeção de liberação de entrada, quantas entradas estavam bloqueadas e distribuição de durações dos subpassos. Reutilizar a fixture 10×800, seeds 42/73, **2h no máximo**, `scale` vs `profile`, SHA-256 completo e invariantes. Só propor otimização se o mecanismo causal do custo ficar identificado. **Não repetir 80 anos nem ampliar a cidade nesta fase.**

## Décima sétima execução — limites de movimento dominam os subpassos (24/10/2026)

**Evidência material:** [Actions #40086671726](https://github.com/Marlohn/IndexCities/actions/runs/40086671726), SHA `fd9ce9567865f13ffb5a4c382eb7bddc68b8f216`, job `phase-profile` **success**. .NET 8 Release, `grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`, controle `scale` versus `profile`, 3.600 quanta de 2s por seed. Invariantes, save/load e SHA-256 completo iguais por seed: 42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`.

| Segunda hora simulada (3600–7200s) | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| Subpassos por `TimeToMovementChange` | **3.445** | **22.257** |
| Subpassos por projeção de liberação de entrada | 4 | 6 |
| Subpassos de horizonte completo | 1.796 | 1.796 |
| Subpassos totais | **5.245** | **24.059** |
| Subpassos >1s / duração máxima | 1.518 / 1,076899s | 0 / 0,333333s |
| Entradas bloqueadas examinadas | 5.337 | 72.502 |
| Wall 2h, controle/perfil | 44,74 / 44,88s | 83,15 / 81,78s |
| Alocação / working set (perfil) | 18.360 MB / 99 MB | 35.456 MB / 108 MB |

**Conclusão:** 92,5% dos subpassos da segunda hora da seed 73 vieram de limites de movimento; apenas seis de rechecagem projetada de entrada. A seed 73 tem **6,46×** mais limites que 42, apesar de frota quase igual (578/576). Ainda não sabemos se o limite decorre de combustível, nó/cruzamento, mudança de velocidade, curva ou fim de rota. Queda de wall frente ao CI anterior ocorreu sem otimização e não prova ganho; runner compartilhado introduz ruído. Saves finais iguais não demonstram igualdade de toda ordem interna. Fixture artificial <10k, sem Godot, sem refeições/salários e sem histórico individual gravado externamente.

**Próxima hipótese única:** classificar por hora a causa do limite de movimento vencedor, observacionalmente, mantendo seeds 42/73, 2h, controle/perfil, invariantes e hash completo. Nenhum evento será omitido.

## Décima oitava execução — limites de nó/cruzamento explicam quase todo o excesso de subpassos (24/10/2026)

**Evidência nova:** [Actions #40088433168](https://github.com/Marlohn/IndexCities/actions/runs/40088433168), SHA `3db4ae2028446a9f8139ea51e5028bccbe5f4a97`, job `phase-profile` **success**. Mesmo runner, .NET 8 Release, `grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`, controle `scale` e observação `profile`; 3.600 quanta de 2s por seed. Invariantes, save/load e SHA-256 completo idênticos entre modos por seed (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`).

| Segunda hora (3.600–7.200s), perfil | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| Limites de movimento `movementBoundary` | 3.445 | **22.257** |
| Limites de nó/cruzamento `movementJunction` | **2.866 (83,2%)** | **21.746 (97,7%)** |
| Limites de curva / velocidade / fim de rota / combustível | 382 / 19 / 178 / 0 | 357 / 1 / 153 / 0 |
| Rechecagens projetadas de entrada | 4 | 6 |
| Entradas bloqueadas examinadas | 5.337 | **72.502** |
| Wall de 2h `scale` / `profile` | 56,42 / 56,48 s | 105,92 / 103,14 s |
| Avanço efetivo `scale` | 128× | **68×** |
| Alocação `profile` / GC0 / working set | 18.360 MB / 223 / 172 MB | 35.456 MB / 427 / 177 MB |

**Conclusão:** a diferença de **18.880 limites de movimento** entre seeds se concentra na categoria de nó/cruzamento (**18.880 limites adicionais**, 21.746−2.866). A categoria é produzida por `node.Crosswalk || node.Neighbours.Count>=3` em `TimeToMovementChange`, e **não distingue ainda travessia, entroncamento, um veículo que vence repetidamente, nem uma frota inteira percorrendo nós**. É atribuição do evento vencedor, não prova de falha de física, deadlock ou ineficiência intrínseca. Os tempos mudaram entre CIs sem otimização: não inferir ganho. A janela inicial não inclui refeições/salários, a fixture injeta obras/recursos/caixa, tem apenas 808/812 vivos, histórico somente em memória e nenhum Godot.

**Próxima hipótese única e barata:** o excesso de limites de nó/cruzamento na seed 73 está concentrado em poucos veículos que repetem a mesma condição, ou distribuído pela frota? Acrescentar **somente observação** do ID do veículo cujo `TimeToMovementChange` efetivamente reduz o horizonte, por hora, registrando número de IDs distintos e top 1/top 3; não alterar evento, tempo, seleção, regra nem ordem. Repetir apenas a janela dirigida de 2h nas mesmas seeds com `scale`/ `profile`, invariantes e SHA-256 completo. Se distribuído, examinar densidade de nós/rotas; se concentrado, isolar caso adversarial de veículo antes de propor otimização.

## Décima nona execução — um único veículo concentra os limites de cruzamento da seed 73 (24/10/2026)

**Evidência material:** [Actions #40089980043](https://github.com/Marlohn/IndexCities/actions/runs/40089980043), SHA `720db6ca75e4a32b4d2f1da1caff360bd9fbe4b1`, job `phase-profile` **success** (logs do job `120860507279`). Release .NET 8; `scale|profile grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`. Mesmo runner, 3.600 quanta de 2 s por seed/modo; invariantes, save/load e SHA-256 **completo** idêntico entre controle e perfil (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`).

| Segunda hora (3.600–7.200 s), observação | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| SIMs vivos / veículos (fim da janela) | 808 / 576 | 812 / 578 |
| Limites vencedores de nó/cruzamento | 2.866 | **21.746** |
| IDs distintos vencedores | 144 | 130 |
| ID mais frequente / vitórias | 3949 / 1.284 (**44,8%**) | **5179 / 20.788 (95,6%)** |
| Top 3 IDs / participação | 1.869 (**65,2%**) | **20.905 (96,1%)** |
| Wall da janela `scale` / `profile` | 42,76 / 42,59 s | 76,87 / 75,76 s |
| Avanço efetivo `scale` | 168× | 94× |
| Alocação `profile` / GC0 / working set | 18.360 MB / 1.102 / 99 MB | 35.456 MB / 2.123 / 106 MB |

**Conclusão discriminante:** a diferença de 18.880 limites de nó/cruzamento entre seeds **não está distribuída proporcionalmente pela frota**: na seed 73, um único veículo (ID 5179) venceu 20.788 vezes na segunda hora, contra 1.284 vitórias do líder da seed 42. A instrumentação apenas conta o vencedor de `TimeToMovementChange`; **não comprova** que o veículo esteja parado, em loop geométrico, com colisão, nem que todas as vitórias causem atraso visível. O custo adicional é consistente com os subpassos e varreduras repetidas já medidos, mas não prova sua causa física. A igualdade do save final não verifica a ordem de cada evento intermediário. Não houve refeições/salários; fixture com obras/recursos/caixa artificiais, <10 mil SIMs, histórico apenas em memória e nenhum Godot.

**Próxima hipótese isolada:** observar, **sem mudar movimento**, a trajetória do vencedor 5179 durante a segunda hora (nó de destino, `RouteIndex`, posição/distância ao alvo, velocidade e progresso entre limites) e comparar com o líder da seed 42. Distinguir cruzamento repetido por progresso legítimo de limite reemitido sem avanço; só depois considerar correção/otimização diferencial com casos adversariais. Não repetir o benchmark de 2h sem instrumentação capaz de responder a essa pergunta.


## Vigésima execução — um veículo quase imóvel provoca dezenas de milhares de limites repetidos (24/10/2026)

**Evidência material:** [Actions #40093387222](https://github.com/Marlohn/IndexCities/actions/runs/40093387222), SHA `ff11a8746b882e80e3c8cd46211f7ec087378729`, job `phase-profile` **success** ([logs](https://github.com/Marlohn/IndexCities/actions/runs/40093387222/job/120870514001)). .NET 8 Release; `scale|profile grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`; cada execução percorreu **3.600 quanta de 2 segundos** sem pular tempo. A observação nova registra posição, destino viário, índice de rota e distância ao alvo **somente quando o veículo vence um limite de nó/cruzamento**, não uma trajetória contínua. Invariantes, save/load e SHA-256 **completo** coincidiram entre controle/perfil por seed (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`).

| Medida na segunda hora (3.600–7.200 s) | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregados (fim da janela) | 808 / 576 / 708 | 812 / 578 / 708 |
| Vencedor dominante de limite de cruzamento | ID **3949**: 1.284 vitórias | ID **5179**: **20.788 vitórias** |
| Destinos viários distintos nas vitórias | 2 | **1** (nó **3027**, 20.788/20.788) |
| Transições consecutivas sem deslocamento observável | **1.282/1.283** | **20.783/20.787** |
| Maior sequência sem deslocamento observável | 1.282 | **10.921** |
| Índice de rota inicial → final | 15 → 30 | **3 → 3** |
| Distância entre posições sucessivamente amostradas, acumulada | 54,142863 m | **0,022910 m** |
| Distância ao nó-alvo inicial → final | 0,186563 → 7,538295 m (mudou de alvo) | **1,167627 → 1,144717 m** (mesmo alvo) |
| Wall da janela `scale/profile` | 76,43 / 76,92 s | 135,18 / 135,97 s |
| Avanço efetivo `scale` / alocação por janela | **94 s simulados/s real** / 18.360 MB | **53 s simulados/s real** / 35.456 MB |
| Working set / fatos históricos em memória (`scale`) | 97 MB / 8.807 | 135 MB / 8.596 |

**Conclusão discriminante:** na seed 73, **99,98%** das transições entre vitórias consecutivas do veículo 5179 ocorreram sem deslocamento mensurável (20.783/20.787), sempre mirando o mesmo nó e com índice de rota inalterado; sua posição avançou somente ~**2,3 cm** entre a primeira e a última amostra da segunda hora. O vencedor da seed 42 também apresenta uma sequência imóvel, porém **16,2× menos vitórias**. Assim, a explosão de subpassos é **compatível com reemitir limites previstos de chegada a nó para um veículo que fisicamente não consegue progredir**, e não com 20 mil cruzamentos efetivamente realizados. O método `TimeToMovementChange` calcula o tempo até o alvo pela distância/velocidade nominal; a rotina real pode bloquear movimento. A instrumentação **ainda não identifica o bloqueador nem a causa da imobilidade**; não prova bug de tráfego, deadlock, perda de conservação ou solução segura por supressão de eventos.

**Limites:** a métrica de deslocamento é entre *amostras de vitórias*, não odômetro contínuo; controle/perfil com save final igual não prova igualdade de toda a ordem intermediária. Cenário artificial com obras instantâneas, caixa/serviços ampliados, apenas 808/812 vivos, zero refeições/salários nas duas horas, histórico em memória sem arquivo factual externo e sem custo Godot. Tempos de CIs distintos não são ganho/regressão controlados; comparar apenas o par deste job. **Próximo teste curto:** registrar, somente no perfil e para o veículo vencedor, `WaitingSince`, `Cause`, estado de entrada/cruzamento e o bloqueio efetivo no trecho de movimento; distinguir fila legítima de reavaliação inútil antes de otimizar. Nenhum evento, travessia ou ordem causal pode ser omitido para obter velocidade.

## Vigésima primeira execução — fila física confirmada no vencedor, sem progressão após cada subpasso (24/10/2026)

**Evidência nova:** [Actions #40094749841](https://github.com/Marlohn/IndexCities/actions/runs/40094749841), SHA `1f4b2b705068ee1e925a508d718349c5f2f51297`, job `120874545390` **success**. Release .NET 8, `scale|profile grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`. Cada execução completou 3.600 quanta de 2 s, invariantes e save/load; SHA-256 integral idêntico entre controle e perfil por seed (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`).

| Resultado observacional na segunda hora | Seed 42 (veículo 3949) | Seed 73 (veículo 5179) |
| --- | ---: | ---: |
| Limites vencedores de cruzamento | 1.284 | **20.788** |
| Subpassos vencedores sem deslocamento após movimento | **1.283/1.284** | **20.788/20.788** |
| Em espera após o subpasso | 1.283 | **20.788** |
| Causa pós-subpasso | 836 preferência/semáforo/travessia; 447 fila na faixa; 1 em viagem | **20.788 fila na faixa** |
| Índice de rota alterado após o subpasso | 1 | **0** |
| Distância efetivamente percorrida nos subpassos vencedores | 0,186563 m | **0,000000 m** |
| Reivindicação de cruzamento / entrada em faixa elegível | 1 / 0 | **0 / 0** |
| Wall controle/perfil para 2h | 78,06 / 79,14 s | 137,46 / 138,44 s |
| Avanço efetivo do controle | 92 s simulados/s real | **52 s simulados/s real** |
| Alocação / working set no perfil | 18.360 MB / 98 MB | **35.456 MB / 137 MB** |

**Conclusão:** o veículo 5179 permaneceu em **fila na faixa** em todas as 20.788 oportunidades em que sua previsão de chegada a nó venceu o subpasso; em nenhuma delas houve deslocamento efetivo. A previsão em `TimeToMovementChange` usa distância e velocidade nominal, enquanto `MoveVehiclesUnsplit` aplica retenções físicas, incluindo líderes e áreas compartilhadas. Há forte evidência de **previsões repetidas sem progresso causal**, não de 20 mil cruzamentos realizados. `WaitingSince` passou de 3.611,052747 a 5.542,047383 nas amostras; dois eventos de nova espera impedem afirmar fila ininterrupta durante todo o período. O bloqueador concreto ainda não foi identificado: `Fila na faixa` pode decorrer de líder, afastamento de corpos ou restrições de passagem. **Não** suprimir eventos ou tratar a espera como falha de conservação sem diagnóstico.

**Limites:** amostras somente do veículo vencedor, não de toda a frota; hash final igual não prova igualdade da ordem interna; fixture artificial com 808/812 vivos, obras/caixa/capacidade artificiais, zero refeições e salários nas 2h, histórico em memória e nenhum Godot. Wall entre CIs não serve para alegar ganho.

**Hipótese isolada seguinte:** o veículo 5179 está limitado por um mesmo líder próximo (distância livre <=8 m), ou por outro teste de ocupação física? Instrumentar **somente perfil** no ponto exato em que `MoveVehiclesUnsplit` calcula o passo zero, capturando ID/status/distância do líder, passo antes/depois da segurança e principais bloqueadores em amostras espaçadas. Reutilizar as duas seeds e janela de 2h, controle/perfil, invariantes e hash; sem alterar movimento nem regras.

## Vigésima segunda execução — distância física confirma bloqueio imediato, mas não a origem da fila (24/10/2026)

**Evidência nova:** [Actions #40096496341](https://github.com/Marlohn/IndexCities/actions/runs/40096496341), SHA `deec91f6d2ca68a688cd5d285c9659522fcdf34a`, job `120879742037` **success**. .NET 8 Release, `scale|profile grid --seed=42|73 --warmup=0 --days=1 --window=7200 --cap=150 --rows=10 --length=800 --signals=1`; 3.600 quanta de 2 s por execução, invariantes e save/load OK, SHA-256 final idêntico entre controle e perfil em cada seed (42 `C7381C1699199D70D9195A24E53B833B93693C53FD84B97E602BEF67CE1CB621`; 73 `01533F5C64C59E7842F225D067B8BE6B9E0390CEE768D494343741B482EE893C`).

| Medida de 2h | Seed 42 | Seed 73 |
| --- | ---: | ---: |
| SIMs vivos / veículos / empregados | 808 / 576 / 708 | 812 / 578 / 708 |
| Wall simulação `scale/profile` | 60,75 / 64,57 s | 116,51 / 112,00 s |
| Avanço efetivo `scale` | 119 s simulados/s real | 62 s simulados/s real |
| Alocação `profile` / GC0 / working set | 18.360 MB / 223 / 176 MB | 35.456 MB / 427 / 179 MB |
| Subpassos `profile` | 5.386 | **24.128** |
| Tempo da fase veículos `profile` | 58,201 s | **105,551 s (94,25% do perfil)** |

**Mecanismo imediato demonstrado na seed 73:** em amostras do veículo **5179**, entre 3.611 e 7.068 s simulados, o bloqueador foi repetidamente o **veículo 5294** (status `Travelling`), a distância medida foi **8,000000 m**, o passo livre antes de segurança foi **0,000000 m** e o passo final **0,000000 m**; não havia retenção de baia, entrada de faixa ou corpos de curva nessas amostras. `WaitingSince` do bloqueador permaneceu em **3.628,661403 s** entre as amostras de 3.840 e 5.362 s. O limitador é **distância física mínima atrás de outro veículo**, não uma travessia efetivamente realizada pelo 5179. Isso **não** identifica o que impede o 5294 de andar, se há bloqueio recíproco ou se a fila é legítima; também não autoriza omitir eventos. O controle e o perfil tiveram hash final igual, não uma auditoria da ordem intermediária. Cenário sintético, 812 vivos, zero salários/refeições nas 2h, obras e recursos artificiais, histórico só em memória, nenhum Godot.

**Próxima pergunta discriminante:** por que o **5294** permanece parado? A menor reprodução útil começa logo após 3.600 s e observa somente o estado/causa do líder, seu próprio bloqueador e corpos próximos, sem modificar decisões físicas. Usar **75 minutos** em vez de repetir 2h, seed 73, controle/perfil e SHA/invariantes. Não otimizar por supressão do evento enquanto a dependência física não estiver compreendida.


## Vigésima terceira execução — líder da fila bloqueado depois da varredura de faixa (24/10/2026)

**Resultado material:** [Actions #40099319368](https://github.com/Marlohn/IndexCities/actions/runs/40099319368), SHA `c9e00fb9ef8bdd12a0e5481dcf197174f6f6d1df`, job `120888011735` **success**. O primeiro disparo da mesma sonda, [#40097953294](https://github.com/Marlohn/IndexCities/actions/runs/40097953294), SHA `340cb7000ed27eb4f56fb388b72bc8811156d3a5`, **falhou tecnicamente** após completar apenas o controle: a asserção do workflow ainda exigia `secondsEach=7200` embora o comando tivesse `--window=4500`; corrigida para 4500, sem alterar simulação. Não interpretar a falha como divergência causal.

**Comando/cenário:** .NET 8 Release, `scale|profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, 2.250 quanta reais de 2s por modo, no mesmo runner. **812 SIMs vivos**, 708 empregados, 578 veículos (25 viajando ao final), 355 SIMs em deslocamento, 2.181 entradas de ledger, 5.945 fatos em memória, **zero refeições/salários** na janela. Wall de simulação `scale/profile` **94,18/93,78 s**, rendimento efetivo `scale` **48 segundos simulados/s real**; perfil com **22.073 MB alocados**, GC0 **1.324**, working set **138 MB**; 6.419 subpassos, fase de veículos **88,449 s (94,32%)**, varredura de líderes de faixa **490.103.213 visitas**. Invariantes e save/load OK, save **4,7 MB**, SHA-256 completo idêntico entre modos: `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. Hash final não comprova igualdade de toda a ordem intermediária.

**Cadeia causal observada (3.611–4.354s):** veículo **5179** permanece a **8,000000 m** do **5294**, com `freeBeforeSafety=0` e `finalStep=0` (4.000 amostras acumuladas até 4.354s). O **5294** também tem `finalStep=0`, mas seu líder selecionado pela varredura de faixa está a **~386 m** (ID **5228**) e `freeBeforeSafety=1,146960 m`; seu alvo é o nó **3029**, `RouteIndex=1`, e `WaitingSince=3628,661403` permanece nas amostras de 3.840 a 4.354s. Na vizinhança do 5294 há **5183 a ~3,43 m** (`Aguardando espaço para acessar a faixa`) e **4990 a ~5,60 m** (`Fila na faixa`). Portanto, o **5294 não está parado pelo limite longitudinal do líder distante**: o passo positivo foi zerado **depois** dessa etapa, em `SafeSiteTravel` ou `ClearOfCrossingAhead` (ou ambos), que verificam ocupação física. Ainda não há identificação inequívoca do corpo/restrição que zera o passo, nem prova de deadlock ou bug; não eliminar reavaliações por hipótese.

**Próxima discriminação de baixo custo:** medir observacionalmente o passo imediatamente após `SafeSiteTravel` e após `ClearOfCrossingAhead` para o 5294, e quais corpos realmente interceptam a primeira tentativa de 0,5 m; mesma seed, janela curta de 4.500s, controle/perfil, invariantes e SHA. Isso distingue bloqueio de obra/corpo de retenção em cruzamento e orienta eventual correção causal. A fixture continua sintética (<10 mil SIMs), com obras instantâneas, caixa/serviços artificiais, sem ciclo econômico diurno, histórico externo ou Godot; nenhuma inferência sobre décadas/10 mil.

## Vigésima quarta execução — ocupante de entrada lateral impede avanço do líder (24/10/2026)

**Evidência:** [Actions #40099858079](https://github.com/Marlohn/IndexCities/actions/runs/40099858079), SHA `b3c5188587cdcf6f574180ad4c6ed712bbbf242e`, job `120889593942` **success**. Mesmo `scale|profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, 2.250 quanta de 2 s, **812 SIMs vivos**, 578 veículos, 708 empregados, 5.945 fatos históricos em memória, **zero salários/refeições**. Wall de simulação `scale/profile` **54,96/54,78 s** (82 s simulados/s real no controle); alocação `profile` **22.207 MB**, GC0 **1.332**, working set **139 MB**, 6.419 subpassos, veículos **52,144 s (95,19%)**. Invariantes e save/load OK, SHA-256 completo igual nos dois modos: `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. A melhora de wall ante CI anterior ocorreu **sem otimização** e não prova ganho, dada a variabilidade do runner.

**Mecanismo físico agora localizado:** nas amostras de 3.611 a 4.354 s simulados, o veículo **5294** tinha **1,146960–1,167627 m** disponíveis após a varredura longitudinal e após `SafeSiteTravel` (`afterSite=freeBeforeSafety>0`), mas `ClearOfCrossingAhead` reduziu o passo a **zero** (`afterCrossing=0`), com **5183** como corpo sobreposto na primeira tentativa de avanço de 0,5 m (`crossingBodyOverlaps=5183`). O **5183** estava a **3,43–3,44 m**, com causa `Aguardando espaço para acessar a faixa`; atrás do 5294, o 5179 permanecia a 8 m. **Não** era o líder longitudinal distante 5228 nem reserva de obra. O 5183 parece tentar entrar na faixa ocupada, mas **a reciprocidade exata ainda não foi medida**: sua causa textual não prova que esteja aguardando especificamente o 5294. O código de `ClearOfCrossingAhead` impede sobreposição física; removê-lo ou ignorar o 5183 seria inseguro. A causa da permanência do 5183 nessa posição ainda está aberta.

**Próxima decisão causal:** conferir observacionalmente se `BlocksLaneEntry(5183, acesso, 5294)` é verdadeiro no mesmo instante em que o 5294 é bloqueado fisicamente pelo 5183. Se sim, há dependência circular local de admissão/movimento a tratar sem sobreposição; se não, rastrear o verdadeiro ocupante que impede a entrada do 5183. Não suprimir subpassos ou mudar prioridade antes de validar causalidade e invariantes. Fixture sintética, sem Godot, com recursos/obras artificiais, histórico apenas em memória e sem atividade diurna representativa; não extrapolar para 10 mil/gerações.

## Vigésima quinta execução — dependência circular local confirmada entre entrada de faixa e passagem (24/10/2026)

**Evidência nova:** [Actions #40100234272](https://github.com/Marlohn/IndexCities/actions/runs/40100234272), SHA `f44679798c5fcf74b44ec76bc8cdbc064cb0318b`, job `120890701354` **success**. O disparo anterior da mesma sonda, [#40100175084](https://github.com/Marlohn/IndexCities/actions/runs/40100175084), SHA `44e8a9596`, falhou **na compilação** (`CS0165`, variável local `entrantAccess` possivelmente não inicializada); corrigido inicializando `LaneAccess` antes da condição, sem mudança de física. Não interpretar essa falha como desvio de causalidade.

**Comando e contexto:** .NET 8 Release, `scale|profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, 2.250 quanta de 2 s por modo. **812 SIMs vivos**, 708 empregados, 578 veículos, 355 SIMs em deslocamento, 2.181 entradas de ledger e 5.945 fatos em memória; **zero refeições e salários** na janela. Wall de simulação controle/perfil **94,46/96,39 s**, avanço efetivo controle **48 segundos simulados/s real**; perfil **22.207 MB alocados**, GC0 **1.332**, working set **137 MB**, **6.419 subpassos**, fase de veículos **90,814 s (94,22%)** e 490.103.213 visitas à varredura de líderes. Invariantes e save/load OK, save 4,7 MB; SHA-256 completo idêntico nos dois modos: `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. O hash final **não** audita toda a ordem intermediária.

**Dependência física recíproca observada:** nas amostras entre **3.611 e 4.354 s**, `ClearOfCrossingAhead(5294)` zera o passo positivo do veículo **5294** devido ao corpo do **5183** a ~**3,43–3,44 m** (`crossingBodyOverlaps=5183`). **No mesmo instante**, a avaliação de admissão `TryLaneEntry(5183)` foi **verdadeira** e `BlocksLaneEntry(5183, ..., 5294)` foi **verdadeira**; os bloqueadores de admissão do 5183 eram **5179 e 5294** nas primeiras amostras, depois **4990, 5179 e 5294**. O 5179, por sua vez, permanecia a **8,000000 m** atrás do 5294, com passo livre zero. O 5183 estava `Travelling`, `RouteIndex=0`, causa `Aguardando espaço para acessar a faixa`, `WaitingSince=3608,887124`; 5294 manteve `RouteIndex=1` e passo final zero. Trata-se de **ciclo local de dependências de circulação/admissão** (`5183` aguarda a faixa ocupada por `5294/5179`; `5294` não avança porque o corpo do `5183` intercepta sua passagem). É um mecanismo discriminante para o custo de subpassos repetidos, **não** prova isolada de bug, deadlock global, nem de que uma prioridade ou supressão de rechecagens seja segura.

**Decisão e próximo teste mínimo:** não modificar `ClearOfCrossingAhead`, remover corpo nem reduzir subpassos sem preservar colisões/ordem. Antes de escolher correção, observar **a origem geométrica e a primeira transição do 5183 de estacionamento/saída para a tentativa de entrada**, por volta de 3.600–3.612 s, e verificar se já ocupava o corredor da faixa **antes** de aguardar ou se entrou parcialmente durante a manobra. Essa distinção separa problema de vaga/fixture de problema de admissão/compromisso físico e determina a intervenção segura. Inspecionar também os testes existentes de entrada lateral/ocupação antes de propor A/B. Não disparado outro CI nesta execução. Fixture sintética, sem Godot, sem ciclo econômico diário, histórico somente em memória e muito abaixo de 10 mil SIMs; não extrapolar para gerações.

## Vigésima sexta execução — entrada parcial após saída simultânea de vagas, não ocupação inicial da via (24/10/2026)

**Evidência:** [Actions #40101008751](https://github.com/Marlohn/IndexCities/actions/runs/40101008751), SHA `238f97dcb884547c5464b86d5b4677edcf08a69e`, job `120892958933` **success**. Sonda de estado/posição de `5183` limitada a 3.550–3.620 s, executada apenas no modo perfil; .NET 8 Release, `scale|profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, 2.250 quanta de 2 s. **812 vivos**, 708 empregados, 578 veículos, 355 SIMs em deslocamento, ledger 2.181, histórico em memória 5.945, **zero salários/refeições**. Wall de simulação controle/perfil **91,59/90,49 s** (49/50 segundos simulados/s real); perfil alocou **22.207 MB**, GC0 **1.332**, working set **141 MB**, **6.419 subpassos** e fase de veículos **85,834 s (94,87%)**. Save/load e invariantes OK, SHA-256 integral idêntico: `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`; igualdade final não prova ordem interna. Wall próximo ao controle não é evidência de otimização.

**Linha causal nova:** o veículo **5183** estava **estacionado**, imóvel em **(-335,400000; 187,700000)**, até 3.606 s, sem obstruir a posição/passo inicial de 5294 (`crossingNow=false`, `crossingNext05=false`). Em **3.608 s**, já estava `Travelling` mas **ainda na vaga**, `RouteIndex=0`, com `TryLaneEntry=true`, `fromSide=-7,7 m`, `toSide=+1,65 m`, e **nem 5294 nem 5179** eram bloqueadores de entrada naquele instante. O 5294 também passou de `Parked` (3.606 s) para `Travelling` (3.608 s), ainda em **(-338,600000; 172,300000)** na primeira amostra. Em **3.609,220321 s**, o 5183 já havia avançado fisicamente para **(-335,797676; 181,502875)** (~**6,21 m** desde a vaga), permanecia no índice 0 e **passou a ter 5294 como bloqueador de entrada**. Nessa posição, seu corpo **não** sobrepunha o 5294 parado (`crossingNow=false`), mas **interceptava a primeira tentativa de avanço de 0,5 m do 5294** (`crossingNext05=true`). Em 3.611,052747 s, o 5179 também entrou no conjunto de bloqueadores do 5183. A posição do 5183 permaneceu idêntica até pelo menos 3.620 s; as sondas anteriores mostram o bloqueio persistindo até 4.354 s.

**Conclusão discriminante:** o ciclo de espera **não nasce de um carro já estacionado sobre a pista**: surge **depois de o 5183 iniciar uma entrada lateral e avançar parcialmente**, enquanto o 5294 também inicia viagem na mesma janela. O diagnóstico aponta falha potencial na **admissão/coordenação de manobras simultâneas**, ou geometria de suas trajetórias, e não justifica apagar colisões ou subpassos. Ainda não foi demonstrado que o 5294 era ele próprio elegível a `TryLaneEntry` em 3.608 s, nem qual decisão de prioridade permitiria ambos concluírem com segurança. **Próxima ação de maior valor:** construir repro adversarial **curto e determinístico** com duas saídas de vagas opostas e destinos de faixa conflitantes (incluindo a geometria observada), verificar a ordem de admissão e o progresso de ambos, e só então comparar uma candidata de reserva/precedência para a **manobra completa** contra o comportamento original. Cobrir posições e colisões em checkpoints, combustível, identidade, save/load e múltiplos tamanhos de `Advance`; não declarar equivalência causal apenas por hash final. Não foi implementada otimização nesta execução. Fixture continua artificial, <10 mil SIMs, sem economia diurna, histórico externo ou Godot.

## Vigésima sétima execução — ambos os veículos elegíveis antes da entrada parcial, mas varredura do primeiro nó não detecta conflito (24/10/2026)

**Evidência nova:** [Actions #40101908936](https://github.com/Marlohn/IndexCities/actions/runs/40101908936), SHA `016e737bd1ad6a4aaa44f00f963be672600c7d71`, job `120895628803` **success**. .NET 8 Release; `scale|profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`; 2.250 quanta de 2 segundos. **812 SIMs vivos**, 708 empregados, 578 veículos, 355 SIMs em deslocamento; 2.181 lançamentos no ledger e 5.945 fatos em memória, mas **zero salários/refeições**. Wall de simulação controle/perfil **90,56/91,47 s** (50/49 segundos simulados por segundo real); perfil **22.207 MB alocados**, GC0 **1.332**, working set **139 MB**, **6.419 subpassos**. Invariantes e save/load passaram; SHA-256 integral do save idêntico nos dois modos: `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. Hash final não audita toda a ordem de eventos.

**Resultado discriminante:** em **3.608,000000 s**, os veículos **5183 e 5294** estavam `Travelling`, ambos com `RouteIndex=0` e **`TryLaneEntry=true`**; deslocamentos laterais de **-7,7 m** e **+7,7 m** até **+1,65 m**, respectivamente, em seus referenciais de rota. Nesse instante o 5294 não bloqueava a admissão do 5183. A comparação observacional das poses amostradas por `Manoeuvre(..., through:0)` nos primeiros nós registrou **zero pares de corpos sobrepostos** (`firstNodeSweepsOverlap=false`). Em **3.609,220321 s**, após avanço parcial, `BlocksLaneEntry(5183,...,5294)` passou a ser verdadeiro e a próxima tentativa de avanço de **0,5 m** do 5294 interceptava o corpo do 5183 (`crossingNow=false`, `crossingNext05=true`). Portanto, a checagem pontual do primeiro nó **não detectou antecipadamente** o conflito que surgiu no movimento real. Não conclui que os caminhos completos sejam seguros ou que a geometria amostrada cubra toda a manobra, e não autoriza prioridade nova, remoção de colisões ou supressão de subpassos.

## Vigésima oitava execução — reprodução adversarial reduzida não reproduziu a espera circular (24/10/2026)

**Resultado novo de teste já incorporado por outra execução:** [Actions #40102453261](https://github.com/Marlohn/IndexCities/actions/runs/40102453261), SHA `58ad31bc0acd95388c5a89a11b0dd5b7614e1381`, job `120897213262` **success**. A classe `DualExitConflictExperiment` exercitou **dois veículos externos** saindo simultaneamente de posições laterais opostas em via reta, com separação longitudinal inicial de **15,4 m**, durante **30 segundos simulados**, com `Advance(0.1)`, `Advance(0.5)` e `Advance(2)`. Os três testes terminaram sem espera mútua contínua observada (`maxMutualStall=0`); cada carro deslocou-se **44,689065 m** e **44,116805 m** e terminou estacionado. Distância mínima entre centros amostrada: **4,235124 / 5,034695 / 4,129529 m** nos três quanta; **não** é prova de ausência de colisão entre corpos orientados. Invariantes, total monetário e save/load passaram. A reprodução **não contém o terceiro carro 5179**, a malha nem todas as posições/rotas da seed 73; o resultado negativo **não refuta** a espera circular observada na cidade artificial.

No mesmo [CI #40102453261](https://github.com/Marlohn/IndexCities/actions/runs/40102453261), o controle e o perfil de 4.500 s da seed 73 foram repetidos pelo workflow existente, com **812 vivos**, 708 empregados, 578 veículos, 355 em deslocamento, 5.945 fatos em memória, zero salários/refeições, wall **96,48/91,53 s**, invariantes e save/load OK e SHA-256 final igual ao de #40101908936 nos dois modos. A diferença de wall entre execuções e modos **não** é ganho de otimização; nenhuma física foi alterada.

**Decisão:** o teste de duas saídas em via reta não isola a causa da fila da seed 73. Evitar mais repetições genéricas de 75 minutos e novos critérios artificiais de precedência. O próximo experimento que pode discriminar o mecanismo deve partir de **checkpoint determinístico próximo de 3.608 s**, preservando inicialmente a geometria/rotas e os três veículos **5183/5294/5179** reais; comparar controle versus ablação isolada de candidatos **apenas em cópia experimental** e verificar colisões/posições intermediárias, combustível, identidade, relógio, invariantes e persistência. Remover carros numa ablação é instrumento de diagnóstico, **não** solução de gameplay nem benchmark de throughput. Se a reprodução exata não for viável a custo proporcional, documentar a dependência da fixture e retomar os limites estruturais de varredura/subpassos com carga representativa. Nenhuma dessas medições prova 10 mil habitantes, gerações, história factual persistida ou desempenho Godot.

## Vigésima nona execução — checkpoint real reproduz o bloqueio nos três tamanhos de passo (24/10/2026)

**Evidência:** [Actions #40103094963](https://github.com/Marlohn/IndexCities/actions/runs/40103094963), SHA `4f96990c45c1fc7b94345e152c733b95a7abd25c`, job `120899085677` **success**. Comando somente experimental `repro grid --seed=73 --rows=10 --length=800 --signals=1` reexecuta a cidade sintética completa até **3.606 s** (812 vivos, 578 veículos, 708 empregados, sem salário/refeição), salva e restaura esse checkpoint e avança **36 s adicionais** com chamadas `Advance(0.1)`, `Advance(0.5)`, `Advance(2)`. Em todas as três variantes, invariantes e round-trip de save/load passaram; nenhum sistema, evento ou colisão foi omitido para acelerar. O novo workflow roda só essa reprodução curta, sem repetir o perfil de 75 minutos já documentado.

| Passo externo | Carro 5183: distância real / maior espera contínua sem mover | Carro 5294: distância real / maior espera | Carro 5179: distância real / maior espera | Maior espera simultânea 5183/5294 |
| --- | --- | --- | --- | --- |
| 0,1 s | 6,209871 m / 34,3 s | 8,959415 m / 13,6 s | 20,015866 m / 13,6 s | 13,6 s |
| 0,5 s | 6,209871 m / 34,0 s | 8,210712 m / 15,5 s | 19,282948 m / 15,5 s | 15,5 s |
| 2 s | 6,209871 m / 32,0 s | 6,237917 m / 12,0 s | 16,404099 m / 12,0 s | 12,0 s |

As distâncias da tabela são **comprimento efetivamente percorrido entre amostras**, não a distância líquida origem-destino. O 5183 terminou em todas as condições exatamente em **(-335,797676; 181,502875)**, ainda no `RouteIndex=0`, com causa `Aguardando espaço para acessar a faixa`. O 5294 terminou perto de **(-337,147; 178,350)**, e o 5179 perto de **(-345,147; 178,350)**. Os três `sha256` completos diferem entre os tamanhos de passo; cidadãos em trânsito, históricos e tempos de espera também divergem. Assim, a reprodução comprova **persistência do impasse físico no cenário real sob três granularidades**, não equivalência causal entre elas nem generalização a outros seeds. O checkpoint em `3606` é **anterior** à partida registrada em `3608`, e mantém os outros ~575 veículos: o teste reduz o horizonte, **não a causalidade do contexto integrado**.

**Decisão técnica:** abandonar a geometria simplificada de apenas dois veículos como referência principal e usar o replay curto integrado para testar causas e alternativas com checkpoints e geometria completos. Ainda não houve correção ou ganho de desempenho. Próxima hipótese a discriminar: por que a previsão de manobra de ambos no primeiro nó não revelou o bloqueio real após os primeiros metros; medir a geometria e o espaço seguro necessário **além do primeiro nó**, sem introduzir uma prioridade ad hoc ou ignorar corpos. O custo estrutural de varrer centenas de milhões de pares continua independente deste impasse; demanda A/B conservativo depois. Nenhuma evidência de 10 mil Sims, 80 anos, economia diurna completa ou histórico externo foi gerada.

## Trigésima execução — interseção de corredores aparece após o primeiro nó; anomalia de determinismo no replay (24/10/2026)

**Evidência:** [Actions #40103510572](https://github.com/Marlohn/IndexCities/actions/runs/40103510572), HEAD de instrumentação `d6d6bbf89d28342f793b662a1a9be04f6da95424`, job `120900330636` **success**. Mesmo checkpoint integral da seed **73**, `grid --rows=10 --length=800 --signals=1`, aos **3.606 s**; **812 SIMs vivos**, **708 empregados**, **578 veículos**, 2.017 lançamentos no ledger e 4.159 fatos em memória no checkpoint, **zero salários/refeições**. Replay de mais **36 s** com `Advance(0.1)`, `Advance(0.5)` e `Advance(2)`; invariantes e save/load passaram. Somente a variante de 2 s recebeu `StepCostProfile` para a nova sonda de geometria. Não houve alteração intencional de física ou regras.

**Nova discriminação geométrica:** em **3.608 s**, `5183` e `5294` eram ambos elegíveis à entrada de faixa, ainda nas posições de partida. As poses amostradas pelo `Manoeuvre` de cada um **até o primeiro nó** tinham **0 pares sobrepostos**; ao estender a previsão até o índice de rota **1**, aparecem **105 pares** de poses cujos corpos se sobrepõem; até índice **2**, **319**; até índice **4**, **749** (`PR57_ENTRY_SWEEP`). Portanto, a previsão limitada ao primeiro nó **não cobre toda a região de conflito geométrico** presente nos percursos posteriores. Esses números contam **pares de poses espaciais, não colisões efetivas nem instantes simultâneos**: não demonstram que os veículos ocupariam essas posições ao mesmo tempo ou que reservar todos os índices seja a solução. No replay, o 5183 continuou retido depois de percorrer 6,209871 m, e o par apresentou 12–15,5 s de espera simultânea conforme a granularidade externa.

**Anomalia nova a resolver antes de qualquer alegação de equivalência:** comparando com [Actions #40103094963](https://github.com/Marlohn/IndexCities/actions/runs/40103094963), o SHA-256 final do replay **0,1 s** permaneceu `AC4D9CE35A913378529C14D400CA7DA6B8C381AFCAD615C405102C985E4F08C0` e o de **2 s** permaneceu `A4538689C67B5321295613BEA2E41C00F4A693F843868F706693135F0622EA46`, mas o de **0,5 s** mudou de `0856BAA81A49D19FEC248ACC73B6AC114BB982C58796435AC4CCD3477CC3A8A2` para `1558BC6B7B6F2023ABDD48037D3D6B231CCB64867303D293FBB13643068107B5`. Os contadores agregados e as posições finais dos três veículos não explicam a diferença. A instrumentação nova só é habilitada na variante de **2 s**; não se pode atribuir o desvio de 0,5 s a ela sem localizar o primeiro estado divergente. Hipóteses abertas: serialização/ordem de coleções, não determinismo no estado integrado ou detalhe da execução. A igualdade de hashes nos outros dois passos não prova igualdade de todos os checkpoints intermediários.

**Decisão:** não aplicar reserva/preferência nova ainda. O próximo teste útil deve comparar **duas execuções 0,5 s a partir do mesmo snapshot**, idealmente com checkpoints e primeiro campo divergente, e depois distinguir diferença de bytes/ordem de serialização de diferença causal de estado; manter a geometria da seed 73. Para uma candidata de admissão, prever conflitos além do primeiro nó **com horizonte e temporalidade justificados**, sem bloquear dois carros por reservar caminhos inteiros indiscriminadamente. Este ensaio sintético de 36 s não mede economia diária, Godot, 10 mil SIMs, gerações nem throughput representativo.

## Trigésima primeira execução — replay duplicado determinístico dentro do processo, divergência entre execuções ainda aberta (24/10/2026)

**Evidência:** [Actions #40103813332](https://github.com/Marlohn/IndexCities/actions/runs/40103813332), SHA de código `b3ac75303101dda787014297e682fb1e86b0e714`, job `120901232820` **success**. Comando `dotnet run --no-build -c Release --project src/IndexCities.Headless/IndexCities.Headless.csproj -- repro grid --seed=73 --warmup=0 --rows=10 --length=800 --signals=1`, .NET 8 Release, checkpoint aos **3.606 s** e replay integral de mais **36 s** com `Advance(0.1)`, `Advance(0.5)` e `Advance(2)`. Checkpoint: **812 SIMs vivos**, **708 empregados**, **578 veículos** (9 em viagem no instante), 159 SIMs em deslocamento, ledger **2.017**, histórico em memória **4.159**, **zero salários/refeições**; save 4.532.775 caracteres, SHA-256 `2D12DE3D0DF6F46CBB399D1E40AF259C24B492C207A7C3490518A5CDDB007CCD`. Todos os replays passaram por invariantes e round-trip save/load; após 36 s havia 812 vivos, 4.223 fatos em memória, 2.017 lançamentos e ainda zero salários/refeições. A fila de 5183/5294 persistiu (espera simultânea máxima de **13,6 / 15,5 / 12,0 s** nos três quanta), sem correção de física.

**Teste discriminante de determinismo:** a variante `Advance(0.5)` foi reexecutada **72 vezes a partir de outra restauração do mesmo snapshot, no mesmo processo**, sem alterar o estado da primeira cópia. Os dois saves finais tiveram **4.551.967 caracteres e conteúdo integral idêntico** (`PR57_HALF_REPEAT same=True firstDifferentChar=-1`), SHA-256 `0856BAA81A49D19FEC248ACC73B6AC114BB982C58796435AC4CCD3477CC3A8A2`. Esse hash coincide com o replay de 0,5 s de [#40103094963](https://github.com/Marlohn/IndexCities/actions/runs/40103094963), **mas difere** do `1558BC6B7B6F2023ABDD48037D3D6B231CCB64867303D293FBB13643068107B5` de [#40103510572](https://github.com/Marlohn/IndexCities/actions/runs/40103510572). As variantes 0,1 e 2 s mantiveram seus hashes anteriores (`AC4D9CE3…` e `A4538689…`). O único código novo em `b3ac753` é impressão do SHA do checkpoint e duplicação observacional do replay 0,5 s **após** a primeira cópia: não há mudança intencional de física. A repetição idêntica **descarta divergência entre essas duas restaurações nesse processo**, mas **não explica** a divergência entre runs e não prova determinismo entre processos ou igualdade causal de granularidades diferentes.

**Decisão:** antes de propor reserva/precedência para entradas simultâneas, localizar se a divergência de 0,5 s já existe no **snapshot inicial** entre processos ou aparece durante o replay; comparar hashes de checkpoint em pelo menos duas execuções frescas e, se iguais mas os saves finais divergirem, capturar o primeiro checkpoint e campo JSON divergente. Um ensaio CI curto, de execução controlada, deve substituir novos perfis longos; não é necessário repetir a sonda geométrica de primeiro nó. Nenhum CI adicional foi disparado nesta rodada (somente registro documental). Esta fixture continua sintética, com obras/caixa/capacidade artificiais, 812 SIMs, 36 s de replay, histórico somente em memória e sem salários, refeições, Godot ou avaliação de décadas/10 mil habitantes. Os hashes finais e invariantes não auditam toda a ordem causal intermediária.

## Trigésima segunda execução — primeira otimização A/B real reduz visitas mas ganha pouco tempo (24/10/2026)

**Hipótese / alteração:** [commit `b78011343aa755eb70374ab368b64d6920896ba3`](https://github.com/Marlohn/IndexCities/commit/b78011343aa755eb70374ab368b64d6920896ba3), isolado nesta branch. O modo experimental `PR57_ACTIVE_SCAN=1` retira carros particulares **estacionados** das duas buscas repetidas de líderes e de curva em `MoveVehiclesUnsplit`, pois esses carros não ocupam a faixa nem são considerados por `BodyAcrossLane`. Preserva candidatos do snapshot `_entryFrame`, ordem original de veículos e adiciona ao conjunto carros que deixam de estar estacionados durante a própria iteração. `PR57_ACTIVE_SCAN=0` usa a lista original; nenhum comportamento aprovado do jogo foi alterado em `main`. A hipótese de filtragem é experimental e **não** foi validada em toda transição adversarial possível.

**Execução manual confirmada:** [GitHub Actions #40106166739](https://github.com/Marlohn/IndexCities/actions/runs/40106166739), job `120908182173` **success**. Mesmo runner Ubuntu / .NET 8 Release, duas execuções sequenciais `profile grid --seed=73 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, controle antes da candidata. Cenário sintético com **812 SIMs vivos, 708 empregados, 578 veículos, 355 SIMs em movimento no fim do intervalo, 2.181 registros no ledger e 5.945 fatos em memória; zero salários e refeições**. Obras, orçamento e capacidades artificiais; não constitui cidade economicamente representativa.

| Medida em 4.500 s simulados | Controle | Candidata experimental |
| --- | ---: | ---: |
| Wall de simulação | **69,60 s** | **66,42 s** |
| Throughput efetivo | ~65 s simulados/s real | ~68 s simulados/s real |
| Visitas à busca líder de faixa | **490.103.213** | **255.325.352** |
| Visitas à busca líder de curva | **490.103.213** | **255.325.352** |
| Wall da busca de faixa | 19,275 s | 15,481 s |
| Wall da busca de curva | 6,025 s | 5,800 s |
| Alocação gerenciada no intervalo | 22.207 MB | **22.714 MB** |
| GC0 / working set | 1.332 / 143 MB | 1.362 / 146 MB |

**Interpretação:** ~**47,9% menos visitas** às duas consultas, mas somente **1,048× de speedup aparente (~4,6% de redução do wall)**, com **~2,3% mais alocação**. O resultado é uma única ordem controle→candidata, no runner compartilhado e com profiler; a diferença de ~3,18 s pode conter variação de ambiente. Os saves tiveram **SHA-256 integral idêntico** `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`, `INVARIANTS OK` e save/load OK nos dois modos. Isso demonstra igualdade dos saves finais **deste caso**, não igualdade causal geral, nem de todos os estados/eventos intermediários.

**Decisão:** **não integrar** esta candidata com base nesse primeiro par e não declarar uma vitória de escalabilidade. Ela serve como prova de que cortar quase metade da varredura isolada ainda oferece ganho global modesto. Uma confirmação com ordem invertida e outros seeds/cenários é útil antes de decidir se vale preservar a filtragem, mas o esforço principal deve avançar para um índice/consulta realmente local que elimine mais custo sem perder ordem e bloqueadores dinâmicos, ou outro gargalo estrutural comparado por A/B. Retomar o caso específico 5183/5294 apenas como teste adversarial de equivalência. Nenhuma evidência de 10 mil SIMs, 80 anos, histórico persistido multigeracional, economia diária completa ou desempenho Godot foi produzida.

## Trigésima terceira execução — índice espacial A/B é regressão mensurada, não ganho (24/10/2026)

**Candidata isolada:** [commit `3eedb44d80473203be7c521a51a5c95db1abc678`](https://github.com/Marlohn/IndexCities/commit/3eedb44d80473203be7c521a51a5c95db1abc678), `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=1`, em `MoveVehiclesUnsplit` do motor integrado. Índice em células de 32 m por fatia de movimento, atualizado após cada veículo, com ordem original dos candidatos e verificações físicas originais preservadas; varredura de entrada lateral continua integral. Controle: `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0`. Código experimental somente nesta branch, **desligado por padrão**. Não houve mudança de gameplay em `main`.

**Resultado:** [Actions #40106983742](https://github.com/Marlohn/IndexCities/actions/runs/40106983742), job `120910601206` **success**. Mesmo runner Ubuntu/.NET 8 Release; `profile grid --seed=73|42 --warmup=0 --days=1 --window=4500 --cap=150 --rows=10 --length=800 --signals=1`, 4.500 s simulados completos por execução, sem omitir subpassos, corpos, deslocamentos, operações ou eventos. Seed 73 com duas repetições e ordem **controle→candidata→candidata→controle**; seed 42 com ordem inversa **candidata→controle**. Ambas as fixtures artificiais, sem salário/refeição e com obras/caixa/capacidade ampliados.

| Seed / repetição | Controle wall | Candidata wall | Controle visitas líder / curva | Candidata visitas líder / curva | Alocação controle → candidata |
| --- | ---: | ---: | ---: | ---: | ---: |
| 73 / A | **58,71 s** | **81,92 s** | 255.325.352 / 255.325.352 | 172.600.499 / 163.577.065 | 22.715 → 29.899 MB |
| 73 / B (ordem invertida) | **56,62 s** | **82,03 s** | idem | idem | 22.715 → 29.899 MB |
| 42 / B→A | **45,68 s** | **65,67 s** | 218.461.329 / 218.461.329 | 158.509.656 / 152.288.316 | 16.974 → 23.566 MB |

**Conclusão negativa robusta:** mesmo eliminando **32,4%** das visitas de líderes de faixa e **35,9%** das visitas de curva na seed 73, a candidata foi **39,5–44,9% mais lenta** em wall real (speedup controle/candidata **0,717× / 0,690×**); na seed 42, **43,8% mais lenta** (0,696×). O custo de construir/consultar/ordenar células superou a economia das verificações. Na seed 73, GC0 **275→361**, working set **186→186–187 MB**, e tempos internos das consultas líder **12,5→24,4 s** e curva **3,3–3,6→15,2 s**. **Não adotar** esta implementação de índice como otimização. O A/B anterior de filtragem de carros estacionados permanece distinto e não valida este índice.

**Controles observados:** seed 73 **812 SIMs vivos, 708 empregados, 578 veículos, 355 SIMs em deslocamento**, 2.181 lançamentos, 5.945 fatos em memória; SHA-256 integral do save `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B` idêntico nas quatro execuções. Seed 42 **808 vivos, 708 empregados, 576 veículos, 361 em deslocamento**, 2.172 lançamentos, 5.885 fatos; SHA-256 `2CC16369699A5DF6BC9BC629C684D4377B080CF2804B5D3DA312151C9C520C5D` idêntico nos dois modos. `INVARIANTS OK` e round-trip de save/load em todos os seis casos. **Limites:** hash final não comprova equivalência causal intermediária, não foram incluídos checkpoints físicos adversariais específicos, não há economia diurna nem história persistida de gerações, e o profiler acrescenta overhead. A candidata fica experimental e desligada; nenhum ganho para 10 mil SIMs ou décadas foi demonstrado.

**Decisão para a próxima rodada:** não repetir a mesma malha espacial de 32 m. Se persistir no gargalo, testar estrutura que evite **ordenar/alocar por consulta** e cuja atualização seja proporcional a veículos realmente deslocados, preservando ordem original, admissões e mudanças de status, com controle de checkpoints intermediários; comparar também alternativas de diminuir o número de consultas por dependências/eventos antes de expandir a arquitetura. Não há CI adicional em voo após este resultado.

## Trigésima quarta execução — A/B melhora 10–14% o wall sem divergir nos três checkpoints (24/10/2026)

**Candidata e execução:** [commit `437c86914fd4ee6b51750c03394bd0ca216297f6`](https://github.com/Marlohn/IndexCities/commit/437c86914fd4ee6b51750c03394bd0ca216297f6), [Actions #40108527359](https://github.com/Marlohn/IndexCities/actions/runs/40108527359), job `120915168859` **success**, .NET 8 Release/Ubuntu compartilhado. Controle `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0 PR57_FAST_SCAN=0`; candidata `PR57_FAST_SCAN=1`, mantendo os demais flags. No mesmo `MoveVehiclesUnsplit` integrado, a candidata reutiliza seno/cosseno da direção fixa durante a varredura de líderes e faz rejeição conservadora AABB + distância **antes** dos predicados geométricos caros da consulta de curva. Preserva a lista original, ordem, corpos, reservas, prioridades, admissões e as verificações exatas para candidatos próximos. Código experimental **desligado por padrão**, somente nesta branch; o índice espacial de células da rodada anterior continua desligado e **não** foi adotado.

**Protocolo:** `profile grid --warmup=0 --days=3 --window=1500 --cap=150 --rows=10 --length=800 --signals=1`, sementes **73** e **42**, total **4.500 s simulados por processo**, seis execuções no mesmo runner. Seed 73 em ordem **controle→candidata→candidata→controle (ABBA)**; seed 42 **candidata→controle**. `Advance(1500)` por janela; os hashes SHA-256 **completos** foram capturados após 1.500, 3.000 e 4.500 s, com invariantes por checkpoint e round-trip final de save/load. O tempo dos checkpoints/saves é excluído do wall das janelas, mas pode influenciar GC/cache subsequentes em ambos os modos. Tempos são do profiler e runner compartilhado, não benchmark Godot nem throughput sem instrumentação.

| Seed/ordem | Wall controle (4.500 s) | Wall candidata | Speedup controle/candidata | GC0 controle→candidata | Alocação MB/janela controle→candidata |
| --- | ---: | ---: | ---: | ---: | ---: |
| 73, A→B | **51,13 s** | **46,24 s** | **1,106×** | 1.363→1.356 | 7.594→7.555 |
| 73, B→A | **51,46 s** | **45,72 s** | **1,126×** | 1.363→1.356 | 7.594→7.555 |
| 42, B→A | **40,26 s** | **35,31 s** | **1,140×** | 1.020→1.015 | 5.680→5.651 |

**Ganho medido:** redução de wall de **9,6% / 11,2%** na seed 73 e **12,3%** na seed 42, com ordens invertidas e mesma carga. Throughput aproximado de **88→97–98 s simulados/s real** na seed 73 e **112→127 s/s** na seed 42. Na seed 73, o wall da consulta de líder de faixa caiu de **13,38/13,07 s** (controles) para **7,11/7,12 s** (candidatas), enquanto a consulta de curva **piorou** de **5,15/4,61 s** para **5,81/5,76 s**: a filtragem de curva não deve ser creditada como ganho isolado. A candidata visitou os mesmos **255.325.352** elementos em cada consulta da seed 73, mas rejeitou **118.587.710** deles antes dos predicados caros de curva; seed 42: **218.461.329** visitas, **91.120.164** rejeições precoces. O ganho líquido da combinação é reproduzível nestes três pares, porém o efeito de reutilizar trigonometria e o da rejeição de curva **não foram isolados**. Working set final seed 73 **199–200→196–197 MB**; seed 42 **169→163 MB**. GC2 seed 73 =5 em ambos, seed 42 =6 em ambos.

**Estado causal observado:** seed 73 **812 vivos, 708 empregados, 578 veículos, 355 SIMs movendo-se e 25 veículos viajando** ao final; seed 42 **808 vivos, 708 empregados, 576 veículos, 361 SIMs movendo-se e 9 veículos viajando**. Na seed 73: **2.181 lançamentos e 5.945 fatos em memória**; seed 42: **2.172 lançamentos e 5.885 fatos**. **Zero salários/refeições e zero mortes** neste horizonte inicial. Todos os seis processos passaram `INVARIANTS OK` e save/load; **todos os três checkpoints SHA-256 completos coincidiram** entre controle/candidata/repetições de cada seed. SHA final seed 73 `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`; seed 42 `2CC16369699A5DF6BC9BC629C684D4377B080CF2804B5D3DA312151C9C520C5D`. Três checkpoints e hashes finais não demonstram equivalência universal de ordem de eventos entre checkpoints, transições adversariais ou persistência multigeracional.

**Decisão:** **ganho A/B local confirmado**, sem alegar meta de 10k/80 anos. Próxima candidata discriminante: **isolar somente o reúso de trigonometria de líder** (desligando a rejeição de curva que piorou o wall), manter o mesmo controle, seeds e checkpoints, e testar casos adversariais de entrada/mudança de estado antes de qualquer proposta de integração. A fixture ainda usa obras instantâneas, orçamento/utilidades artificiais e menos de mil vivos; sem salários, consumo, nascimentos/mortes, décadas, Godot ou responsividade. Não ampliar horizonte nem proclamar otimização pronta sem essas verificações.

## Trigésima quinta execução — trigonometria isolada melhora; CI excede prazo na segunda seed (24/10/2026)

**Código e execução:** [commit `38c3b81`](https://github.com/Marlohn/IndexCities/commit/38c3b81d6bc032d70b43bb766068e5f0622778d7), correção técnica de YAML em [`a445341`](https://github.com/Marlohn/IndexCities/commit/a4453416062861915a049d534063db8c36aa554b), [Actions #40109798846](https://github.com/Marlohn/IndexCities/actions/runs/40109798846), job `120918939422` **cancelled por timeout de 15 min**. O primeiro push [#40109765149](https://github.com/Marlohn/IndexCities/actions/runs/40109765149) falhou na sintaxe YAML antes de executar benchmark; corrigido sem alterar a física. A candidata `PR57_FAST_SCAN=1 PR57_BEND_PREFILTER=0` isola o reúso de seno/cosseno na busca de líder; a combinação anterior usa `PR57_BEND_PREFILTER=1`. Todos os modos mantêm `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0`. O teste adversarial `DualExitConflictExperiment` **passou** com três modos e seis checkpoints completos a cada 5 s durante 30 s de `Advance(0.5)`, restaurando o mesmo snapshot; isto não prova cobertura de todas as entradas e colisões.

**Resultados materiais completos da seed 73:** .NET 8 Release, mesmo runner, `profile grid --seed=73 --warmup=0 --days=3 --window=1500 --cap=150 --rows=10 --length=800 --signals=1`, três janelas de 1.500 s (`Advance(1500)`), ordem controle→trig→combinada→combinada→trig→controle. Wall de simulação **87,67 / 78,55 / 80,84 / 80,78 / 79,47 / 86,60 s**. Assim, trig isolado é **1,116× e 1,090× mais rápido que o controle** nos pares reversos, e **~2,9% / ~1,6% mais rápido que a combinação** no mesmo runner. Throughput efetivo trig **57–56,6 s simulados/s real**, contra controle **51,3–52,0 s/s**; números inferiores ao CI anterior por variação substancial do runner, não regressão demonstrada. Todas as seis execuções tiveram **812 SIMs vivos, 708 empregados, 578 veículos, 2.181 lançamentos, 5.945 fatos em memória**, zero salários/refeições; passaram invariantes e save/load, com três hashes completos por execução **idênticos entre os três modos e as duas repetições**; SHA final `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. Alocação média por janela **7.594 MB** no controle e trig, **7.555 MB** na combinação; GC0 **1.363** controle/trig e **1.357** combinação. O filtro de curva não melhora o wall isoladamente nesta comparação; a trigonometria é a candidata local mais promissora.

**Limites e falha:** a seed 42 não completou o protocolo: o timeout ocorreu durante a terceira janela da **primeira** execução (combinada); **não** comparar essa execução parcial às completas. Houve mudança do hash intermediário de 3.000 s da seed 73 entre esta execução (`4F3F402A…`) e [#40108527359](https://github.com/Marlohn/IndexCities/actions/runs/40108527359) (`A766E384…`), embora os seis modos atuais concordem e o hash final permaneça igual. Isso impede alegar determinismo entre processos/checkpoints históricos sem investigar a divergência. **Decisão:** não repetir a seed 73; se houver disponibilidade e nenhuma execução concorrente, testar **somente seed 42** com três modos e timeout proporcional, ou avançar para avaliação estrutural se o ganho adicional não justificar custo. O cenário continua artificial (<1.000 SIMs), sem economia diária, história geracional ou Godot; o ganho não comprova 10 mil SIMs/80 anos.

## Trigésima sexta execução — trigonometria isolada confirma ganho na seed 42 em ABBA (24/10/2026)

**Execução dirigida:** [commit `a03dafec`](https://github.com/Marlohn/IndexCities/commit/a03dafec43fa0207190873c08afaf2ab8ed9d822), [Actions #40111450569](https://github.com/Marlohn/IndexCities/actions/runs/40111450569), job `120923862376` **success**. Apenas seed **42**, sem repetir seed 73 já medida. Mesmo runner Ubuntu/.NET 8 Release; `profile grid --seed=42 --warmup=0 --days=3 --window=1500 --cap=150 --rows=10 --length=800 --signals=1`, **3 × 1.500 s simulados** por processo (`Advance(1500)`). Controle: `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0 PR57_FAST_SCAN=0 PR57_BEND_PREFILTER=0`; candidata: mesmos flags, exceto `PR57_FAST_SCAN=1`, com **reúso exclusivo de seno/cosseno do heading do veículo**. O prefilter de curva e o índice espacial permanecem desligados. Ordem **controle→trig→trig→controle (ABBA)**; wall somente das janelas de simulação, sem build/fixture/save/checkpoint. Código experimental continua desligado por padrão.

| Ordem | Controle (wall 4.500 s) | Trig isolada | Speedup | Redução do wall | Wall busca líder controle→trig |
| --- | ---: | ---: | ---: | ---: | ---: |
| A→B | **42,01 s** | **36,14 s** | **1,162×** | **14,0%** | 11,787→6,326 s |
| B→A | **40,45 s** | **35,22 s** | **1,148×** | **12,9%** | 11,497→6,334 s |

**Custo e trabalho efetivo:** 808 SIMs vivos (zero mortes), 708 empregados, 576 veículos, 361 SIMs em deslocamento e 9 veículos viajando ao final; 2.172 lançamentos e 5.885 fatos em memória. **Zero salários/refeições** nas primeiras 1h15 simuladas. Os quatro processos executaram **2.250 passos de 2 s, 2.303 fatias de veículos**, **218.461.329 visitas à busca de líder** e igual número à busca de curva; nenhuma varredura, corpo ou subpasso foi suprimido. A alocação permaneceu em **5.680 MB por janela** nos dois modos; GC0 controle 1.020/1.021, trig 1.020/1.021; GC2 **6** em todos; working set final 171/193 MB controle e 164/192 MB trig (variação do runner). A consulta de curva não acelerou: 4,124→4,525 s no primeiro par e 4,015→4,123 s no segundo. Ganho está na busca de líder, não em reduzir número de verificações.

**Persistência e equivalência observada:** `INVARIANTS OK` e round-trip save/load nos quatro processos; os hashes SHA-256 **integrais** após 1.500, 3.000 e 4.500 s coincidiram entre controle/candidata e repetições: `6F3975E253A084FB631CC3E5053BBF20AD00AAC512F3C4193F2B7544F28AC7D1`, `D0313F890D651A57B4E389FE42B899E63DCF96771DD54FBADFAB7DB0AF8F159E`, `2CC16369699A5DF6BC9BC629C684D4377B080CF2804B5D3DA312151C9C520C5D`. O teste adversarial `DualExitConflictExperiment` passou **4/4**. Hashes de três checkpoints e esse adversarial não provam equivalência física/causal universal; a divergência de SHA intermediário da **seed 73 entre execuções históricas** registrada na rodada anterior segue sem explicação.

**Decisão:** confirmar **ganho local real de ~13–14% de wall** para trigonometria isolada na seed 42, além dos pares positivos da seed 73, sem integrar ao gameplay. O índice espacial anterior permanece rejeitado. A próxima validação útil deve testar um horizonte que atravesse trabalho/salários/consumo e, antes de integração, resolver a discrepância histórica de checkpoint da seed 73 e ampliar casos adversariais de tráfego. A fixture é artificial (obras instantâneas, orçamento e capacidades ampliados), tem <1.000 vivos, nenhuma economia diurna neste intervalo, e o profiler/runner compartilhado não representam o Godot ou décadas/10 mil SIMs. **Nenhum novo CI foi disparado por esta atualização documental.**


## Trigésima sétima execução — ciclo econômico de 48h não confirma ganho no cenário pequeno (24/10/2026)

**Execução:** [commit `bd922687`](https://github.com/Marlohn/IndexCities/commit/bd922687fac9f2ded2e51a93c655fe1c0282c195), [Actions #40112574626](https://github.com/Marlohn/IndexCities/actions/runs/40112574626), job `120927194473` **success**, Ubuntu/.NET 8 Release. Comando por processo: `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0 PR57_FAST_SCAN={0|1} PR57_BEND_PREFILTER=0 PR57_AB_CHECKPOINT=1 dotnet run --no-build -c Release --project src/IndexCities.Headless/IndexCities.Headless.csproj -- profile cycle --seed={42|73} --warmup=0 --days=8 --window=21600 --cap=90`. Oito janelas de `Advance(21600)` = **172.800 segundos (48h)** por execução, ordem controle→trig→trig→controle (ABBA) para cada seed; wall só das janelas. Reúso trigonométrico isolado, experimental e desligado por padrão. Teste `DualExitConflictExperiment` aprovado.

| Seed | Wall controle A/B | Wall trig A/B | Speedup nos pares | Throughput aproximado controle/trig |
| --- | ---: | ---: | ---: | ---: |
| 42 | 4,390 / 4,470 s | 4,480 / 4,310 s | **0,980× / 1,037×** | 39,4/38,6 mil e 38,7/40,1 mil s simulados/s real |
| 73 | 9,510 / 9,310 s | 9,270 / 9,540 s | **1,026× / 0,976×** | 18,2/18,6 mil e 18,6/18,1 mil s simulados/s real |

**Conclusão negativa:** os pares de ordem invertida mudam de sinal; **não há ganho de desempenho confiável da trigonometria isolada neste cenário `cycle` pequeno**, apesar dos ~9–14% reproduzidos anteriormente na fixture `grid` com centenas de veículos. O custo-alvo não domina a cidade de 47–50 habitantes. Não misturar throughput desta fixture pequena com o da `grid` nem extrapolar para 10 mil SIMs.

**Atividade factual e estado:** seed 42 terminou com **47 vivos, 47 empregados, 42 veículos, 0 viajando, 181 refeições, salários acumulados 704,94, 1.127 lançamentos e 1.729 fatos em memória**; seed 73 com **50 vivos, 48 empregados, 41 veículos, 10 viajando, 177 refeições, salários 744,74, 1.127 lançamentos e 1.333 fatos**. Nenhuma morte em 48h. Os **oito hashes completos por seed coincidiram entre os quatro modos/repetições**, assim como o save final (seed 42 `7357FCCBAA99AA9665CE4988D46EE1079F524AA21883B43455B9D1A8EA595810`; seed 73 `F749D8959294D0352CC32DD14113AE22470ECB1FA9EB09E1810205588B6A7A92`); oito `INVARIANTS OK` e save/load passaram. GC0/alocação por janela foram iguais por modo: seed 42 ~140/290 MB; seed 73 ~330/688 MB, GC2=3, working set final ~124–125 MB. Hashes de 6h não demonstram igualdade de toda ordem causal intermediária. Salários/refeições presentes provam essas atividades na fixture pequena, **não** produção, comércio e trânsito representativos, nem histórico individual geracional. O cenário continua headless e sem Godot.

**Decisão:** não gastar mais CI repetindo `cycle` para medir uma otimização de tráfego que não domina sua carga. A próxima comparação de desempenho deve usar `grid` com carga veicular alta, testar uma broad phase **exata e sem truncamento arbitrário de distância**, preservar ordem original e mudanças de posição/estado; medir número de visitas, overhead de manutenção do índice, wall, memória/GC e checkpoints/adversariais. Não integrar a candidata até validar trânsito e economia com cargas representativas.

## Trigésima oitava execução — broad phase com bitsets e ordem original: ganho pequeno, custo de manutenção relevante (24/10/2026)

**Candidata isolada:** [commit `d7aa968`](https://github.com/Marlohn/IndexCities/commit/d7aa96889c7f85b7daf73c67d29298868e0535dc), [Actions #40115825284](https://github.com/Marlohn/IndexCities/actions/runs/40115825284), job `120936769819` **success**. Índice experimental de células de 32 m com **bitsets por índice original de `State.Vehicles`**; união das células e enumeração em ordem crescente, sem ordenar candidatos por consulta. Atualiza posição/célula após cada veículo processado. Para corredores com mais de 256 células, usa **lista integral**, não cutoff espacial; entrada de veículos na faixa usa lista integral por causa do alcance variável. Os predicados exatos de colisão, líderes, curvas e prioridades não mudaram. `PR57_BITSET_SCAN=1` só no modo candidato; controle `=0`; ambos com `PR57_ACTIVE_SCAN=1 PR57_FAST_SCAN=1 PR57_SPATIAL_SCAN=0 PR57_BEND_PREFILTER=0`. Candidata **desligada por padrão**, índice anterior por listas/sort também desligado.

**Protocolo:** mesmo runner Ubuntu/.NET 8 Release, `profile grid --seed=73 --warmup=0 --days=3 --window=1500 --cap=180 --rows=10 --length=800 --signals=1`, três janelas de `Advance(1500)` por processo, total **4.500 s simulados**. Quatro processos **controle→bitset→bitset→controle (ABBA)**, `PR57_AB_CHECKPOINT=1`, invariantes e save/load completos. Testes `DualExitConflictExperiment` passaram, incluindo modo bitset e checkpoints de 5 s em saída simultânea; cobertura adversarial ainda restrita.

| Métrica | Controle A / B | Bitset A / B |
| --- | ---: | ---: |
| Wall efetivo por 4.500 s | **76,85 / 78,62 s** | **75,19 / 77,37 s** |
| Redução de wall pareada | — | **2,16% / 1,59%** (speedup 1,022× / 1,016×) |
| Visitas busca líder | 255.325.352 | **173.075.425** (−32,2%) |
| Visitas busca curva | 255.325.352 | **163.577.065** (−35,9%) |
| GC0 por processo | 1.363 | **1.370** |
| Alocação MB/janela | 7.594 | **7.631** |
| GC2 / working set final | 5 / 194–195 MB | 5 / 195–196 MB |

**Equivalência observada:** quatro processos completaram os 3 checkpoints, `INVARIANTS OK` e save/load. SHA-256 **completo** idêntico nos três checkpoints em ambos os modos e repetições: 1.500 s `557497E4C0F2C98B7620379A65984874AACEFB8166C634C5195B34CEC529BB9A`; 3.000 s `4F3F402A1D4D1266E8495E951B61E5D86C94CD0E7ADB7290C44C70D006E58624`; 4.500 s `7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`. Ao final: **812 vivos, 708 empregados, 578 veículos, 355 SIMs em movimento, 25 veículos viajando, 2.181 lançamentos, 5.945 fatos, zero salários/refeições**. Foram processados todos os 4.500 s, sem omitir subpassos por configuração. Checkpoints e adversarial não provam equivalência causal universal; a discrepância histórica da seed 73 entre processos anteriores ainda requer diagnóstico antes de integração.

**Decisão:** o índice reduz visitas em ~32–36%, mas o ganho líquido de **1,6–2,2%** é pequeno frente à variação do runner; além disso aumenta alocação/GC0. **Não integrar nem proclamar speedup robusto ou escala viável.** Manter experimental e desligado por padrão. A próxima hipótese deve atacar o custo de construir/consultar o índice ou a varredura de líder sem alocações por fatia; medir em carga maior e preservar igualdade de checkpoints/atividade. O cenário ainda é artificial, tem <1.000 SIMs, apenas 75 minutos, sem ciclo econômico diário, gerações ou Godot.


## Trigésima nona execução — filtro geométrico antecipado melhora o bitset, mas ganho segue modesto (24/10/2026)

**Mudança e execução:** [commit `c18809cc`](https://github.com/Marlohn/IndexCities/commit/c18809cc194666a0a337cb8010e6e4b69423a9b4), [Actions #40117544104](https://github.com/Marlohn/IndexCities/actions/runs/40117544104), job `120941887526` **success**, Ubuntu/.NET 8 Release. No modo experimental `PR57_BITSET_SCAN=1`, após preservar as verificações de corpo sobre faixa, coincidência e bloqueio de entrada, calcula projeções longitudinal e lateral usando seno/cosseno já reutilizados; só consulta a diferença de headings se `forward>0` e `side<2.6`. Os três caminhos subsequentes de líder (desvio, guincho oposto, mesma direção) já exigiam essas condições; os predicados exatos, ordem de `State.Vehicles` e tratamento especial de entrada permanecem. **Desligado por padrão**; nenhum cutoff de distância finita foi introduzido. A segurança universal do filtro ainda requer mais casos adversariais e revisão de estados/precisão.

**A/B:** `profile grid --seed=73 --warmup=0 --days=3 --window=1500 --cap=180 --rows=10 --length=800 --signals=1`, três `Advance(1500)` por processo, 4.500 s simulados, ordem **controle→bitset→bitset→controle (ABBA)** no mesmo runner; `PR57_ACTIVE_SCAN=1 PR57_SPATIAL_SCAN=0 PR57_FAST_SCAN=1 PR57_BEND_PREFILTER=0 PR57_AB_CHECKPOINT=1` em ambos, alterando só `PR57_BITSET_SCAN=0|1`. Wall efetivo controle **77,34 / 76,63 s**; candidata **73,95 / 72,81 s**. Redução pareada de wall **4,38% / 4,99%** (speedup **1,046× / 1,052×**); throughput aproximado **58–59→61–62 s simulados/s real**. O ganho é **local e modesto**, não valida escalabilidade. Em comparação com a rodada bitset anterior (~1,6–2,2%), houve melhora observada, mas a variação entre runners não permite atribuir toda diferença ao novo filtro sem A/B direto entre as duas versões do bitset.

**Custo:** visitas à busca de líder **255.325.352→173.075.425** (−32,2%), à busca de curva **255.325.352→163.577.065** (−35,9%); os números de acertos permaneceram **5.580.918** e **207**, respectivamente. Wall da busca de líder controle **13,530/13,205 s**, bitset **10,756/10,794 s**; curva controle **7,029/6,354 s**, bitset **5,778/5,712 s**. O índice ainda cobra manutenção: **GC0 1.363→1.370**, alocação **7.594→7.631 MB/janela**, GC2=5; working set final controle **201–205 MB**, candidata **192–193 MB** (variação de ambiente). `STEP_PROFILE` registrou **2.250 passos**, `VEHICLE_PROFILE` **457 passos em lote, 1.793 sem lote e 6.419 fatias**, iguais nos modos.

**Estado e limites:** os quatro processos terminaram com **812 SIMs vivos, 708 empregados, 578 veículos, 355 SIMs em movimento, 25 veículos viajando, 2.181 lançamentos e 5.945 fatos em memória**, zero mortes/salários/refeições. Teste adversarial `DualExitConflictExperiment` **4/4**, invariantes e save/load aprovados. SHA-256 completo idêntico entre modos e repetições após **1.500 s** (`557497E4C0F2C98B7620379A65984874AACEFB8166C634C5195B34CEC529BB9A`), **3.000 s** (`4F3F402A1D4D1266E8495E951B61E5D86C94CD0E7ADB7290C44C70D006E58624`) e **4.500 s** (`7116E7B3E31CCD469CA076B9C3DC9B505C7CF69B4FF232706D172A39D371F95B`). Esses checkpoints não provam equivalência em todos os eventos intermediários, manobras, casos adversariais ou entre processos históricos. Fixture sintética com obras/capacidades artificiais, **75 min e menos de 1.000 SIMs**, sem ciclo econômico diurno, gerações ou Godot.

**Decisão:** preservar filtro/bitset **somente como candidato desligado**, sem integração. O custo estrutural continua dominado pelo movimento (aprox. 94% do wall da seed 73), com redução de visitas muito maior que a redução do tempo total. Antes de investir em outro índice mais complexo, medir se a atualização/enumeração do bitset e a alocação de candidatos anulam o ganho; avaliar consulta sem lista intermediária ou redução de consultas por dependências **sem mudar causalidade**, e só ampliar cargas/horizonte com cobertura de eventos adequada. **Nenhum novo CI foi disparado pela atualização documental.**
