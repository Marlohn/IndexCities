# IndexCities — Estudo de ritmo e velocidades de simulação

> **Status de revisão humana: PENDENTE — não é fonte de verdade.**  
> Referências normativas: Produto: [SPEC](../SPEC.md). Estrutura: [ARCHITECTURE](../ARCHITECTURE.md). Regras de trabalho: [AGENTS](../../AGENTS.md).

---

> **Atualização de 2026-10-16:** os trechos iniciais deste arquivo documentam um experimento anterior com velocidades 15x/60x e propostas de cobrança diária/limite de 300x. **Não descrevem mais a calibração vigente (1x/30x/1000x) nem uma recomendação aceita.** A verificação independente no final do estudo prevalece sobre afirmações empíricas conflitantes da rodada original. A decisão humana atual de calendário real e observação de gerações está na SPEC; a nova análise pendente vem na seção final deste arquivo.

## Contexto histórico do estudo inicial (antes da confirmação geracional de 2026-10-16)

- **Regra vigente na SPEC (linha ~64):** *"1x em tempo real: um segundo real avança um segundo simulado na velocidade normal. Movimento físico, relógio, obras, turnos, produção e consumo seguem a mesma escala; as velocidades rápidas permitem avançar. ... Os dois multiplicadores rápidos continuam sendo calibração técnica; nesta rodada, a calibração é 15x e 60x."*
- **Problema de ritmo identificado:** em velocidade 60x, 1 mês simulado leva aproximadamente 12 horas reais (`2.592.000 s / 60 = 43.200 s = 12 h`). No início de uma partida, o jogador investe o caixa municipal nas obras iniciais (Prefeitura, primeira rua, pátio de obras ou serviços) e rapidamente fica sem saldo disponível, entrando em uma longa espera passiva até a cobrança mensal do imposto imobiliário.
- **Hipótese levantada:** acrescentar multiplicadores de velocidade consideravelmente maiores (ex.: 300x, 1000x, 3000x), permitindo que um mês passe em pouco tempo real, e substituir os números na interface por rótulos genéricos (ex.: normal / médio / rápido, ou `▶`, `▶▶`, `▶▶▶`) em vez de estampar "600x" ou "3000x".

Este estudo analisa a arquitetura atual do motor, apresenta medições reais executadas no cenário integrado da V1, calcula o tempo de 1 mês, identifica os limites físicos de processamento e avalia alternativas de gameplay e produto.

---

## 1. Multiplicadores atuais, arquitetura do passo e física da simulação

### 1.1 Onde os multiplicadores estão definidos hoje
1. **Núcleo de simulação (`src/IndexCities.Simulation/Model.cs:57`):**
   ```csharp
   public double[] SpeedMultipliers {get;set;}=[0,1,15,60];
   ```
   Definido no `SimulationTuning` do estado da cidade.
2. **Interface do jogo (`src/IndexCities.Godot/Main.UI.cs:30`):**
   ```csharp
   foreach(var pair in new[]{("Ⅱ",0),("▶ 1×",1),("▶ 15×",2),("▶ 60×",3)}) { ... }
   ```
   Gera os botões da barra superior e associa as teclas de atalho 0, 1, 2 e 3.
3. **Carga e salvamento (`src/IndexCities.Godot/Main.cs:226`):**
   Ao carregar partidas salvas, `LoadCity` garante a escala humana e os multiplicadores oficiais:
   ```csharp
   sim.State.Tuning.SimulationSecondsPerRealSecond=1;
   sim.State.Tuning.SpeedMultipliers=[0,1,15,60];
   _speed=Math.Clamp(s.Speed,0,3);
   ```

### 1.2 Como o passo da simulação escala no motor
O motor **NÃO aumenta o valor de `dt`** quando a velocidade é acelerada. Ele adota **passo fixo com N iterações por frame**:

1. **Loop do host Godot (`src/IndexCities.Godot/Main.cs:81-83`):**
   ```csharp
   void AdvanceRealClock(ulong at)
   {
       var elapsed=_clockAt==0?0:(at-_clockAt)/1_000_000.0;_clockAt=at;
       _clockBudget+=elapsed*_sim.State.Tuning.SimulationSecondsPerRealSecond*_sim.State.Tuning.SpeedMultipliers[_speed];
       while(_clockBudget>=2){_world.CaptureMotionBeforeStep(_sim.State);_sim.Advance(2);_clockBudget-=2;}
   }
   ```
   O tempo real decorrido multiplicado pelo fator de velocidade acumula no saldo `_clockBudget`. Enquanto houver pelo menos 2 segundos acumulados, o jogo roda um passo discreto de 2 segundos.
2. **Fatiamento interno do núcleo (`src/IndexCities.Simulation/CitySimulation.cs:11, 62-66`):**
   ```csharp
   public double Quantum => 2;
   public void Advance(double seconds)
   {
       if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
       var remaining=seconds;
       while(remaining>1e-8){var dt=Math.Min(Quantum,remaining);Step(dt);remaining-=dt;}
   }
   ```
   Mesmo que o código externo chame `Advance(60)` ou `Advance(3000)`, o método internamente divide a duração em fatias de no máximo `Quantum = 2` segundos, chamando `Step(dt)`.

### 1.3 Por que a física exige sub-passos e NÃO aguenta `dt` grande
A simulação de veículos, pedestres e obras **não suporta passos largos** (ex.: `dt = 30s` ou `dt = 60s`). Se `dt` fosse aumentado:

1. **Varredura e reserva de cruzamentos (`Mobility.cs:215`):**
   A entrada em cruzamentos verifica `(node.Neighbours.Count >= 3 || node.Crosswalk) && d < 9 && !MayEnterJunction(v, node)`. Um veículo trafega a 12 m/s na via urbana e 24 m/s na rodovia. Em um passo de `dt = 30s`, o veículo percorreria até 360 metros de uma vez: ele saltaria ("tunneling") por cima do raio de detecção de 9 metros do cruzamento, cruzando semáforos vermelhos e atravessando outros veículos que estivessem passando no mesmo instante.
2. **Conflito temporal entre pedestres e veículos nas faixas (`Mobility.cs:77-83`):**
   No método `Step(dt)`, `MoveVehicles(dt)` roda antes de `MoveCitizens(dt)`. Se `dt = 60s`, todos os veículos da cidade movem 60 segundos inteiros de percurso e liberam as faixas. Em seguida, os pedestres movem seus 60 segundos e encontram as faixas desocupadas, sem "ver" que carros passaram ali no segundo 10 ou 20. O modelo causal de travessia seria completamente quebrado.
3. **Ingresso lateral na faixa a partir de lotes (`Mobility.LaneEntry.cs:76-93`):**
   A física de trânsito é tão refinada que já exige um fatiador contínuo de eventos (`NextVehicleMovementEvent`) que quebra `dt = 2s` em sub-fatias ainda menores para evitar batidas na saída de garagens.
4. **Cálculo de fila e espaçamento (`gap` e `leaderDistance`):**
   A distância para o líder à frente é calculada no início do passo. Com `dt` elevado, o seguidor colidiria com o veículo líder se este desacelerasse ou parasse durante aquele intervalo.

**Conclusão estrutural:** para manter a fidelidade e integridade física/causal, **a simulação precisa obrigatoriamente executar passos discretos de no máximo 2 segundos**. Acelerar a simulação significa estritamente **executar mais passos por segundo real**.

---

## 2. Medições empíricas em cenário headless integrado

As medições foram realizadas em cenário integrado real da V1 salvo em `artifacts/verification/city-cycle-snapshot.json`, composto por:
- **42 prédios** (Prefeitura, Pátio de Obras, Mercearia, Posto, Fazenda, Eólica, Captação, Tratamento de Água e Esgoto, além de 34 residências);
- **245 nós viários** conectados à rodovia externa;
- **47 cidadãos SIMs** com residências, turnos de trabalho e rotas a pé/carro;
- **37 veículos** (caminhões municipais, caminhões de abastecimento e carros particulares);
- **45 projetos/obras** e ciclo econômico ativo.

### 2.1 Resultados das medições no Godot (host headless)

A instrumentação mediu 10 segundos reais de execução em cada multiplicador, registrando frames renderizados, tempo de simulação por frame, tempo de apresentação, ticks por segundo alcançados, taxa de atraso (`lagRatio = simSeconds / targetSeconds`) e verificação de invariantes.

| Multiplicador | Segundos Reais | Segundos Simulados | Horas Simuladas | Ticks/s alcançados | Simulação / frame | Apresentação / frame | FPS | Lag Ratio | Invariantes |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **60x** | 10,00 s | 600 s | 0,17 h | 30,0 ticks/s | 0,25 ms | 1,85 ms | 144 | 1,000 (0% atraso) | OK |
| **300x** | 10,00 s | 3.002 s | 0,83 h | 150,1 ticks/s | 1,04 ms | 2,33 ms | 145 | 1,000 (0% atraso) | OK |
| **1.000x** | 10,01 s | 10.006 s | 2,78 h | 500,0 ticks/s | 2,09 ms | 1,99 ms | 144 | 1,000 (0% atraso) | OK |
| **3.000x** | 10,00 s | 30.006 s | 8,33 h | 1.500,0 ticks/s | 4,92 ms | 1,73 ms | 144 | 1,000 (0% atraso) | OK |
| **6.000x** | **Trava** | — | — | < 1.950 ticks/s | > 15,0 ms | — | **0 FPS** | **Espiral de morte** | — |

### 2.2 Medição do núcleo puro em C#/.NET (sem Godot)
No teste de longa duração `MonthlySoak` sobre o mesmo cenário de cidade:
- **1 dia simulado completo** (86.400 segundos = 43.200 passos de `Step(2)`): **22,12 segundos reais**.
- **Taxa de throughput máxima da CPU (1 core):** **~1.953 ticks por segundo** (equivalente a ~3.900x em tempo real sem apresentação).
- **Tempo para 1 mês de simulação pura na CPU:** `30 * 22,12 s` = **663,6 segundos = ~11 minutos reais de processamento contínuo**.

### 2.3 Diagnóstico dos problemas em velocidades altas

1. **A "Espiral da Morte" (Death Spiral) acima do teto de CPU:**
   - Em 6.000x, o jogo exige 3.000 passos por segundo real.
   - Como a CPU é capaz de processar no máximo ~1.950 passos por segundo no núcleo puro (e menos ainda quando o Godot roda em paralelo), o tempo necessário para esvaziar o `_clockBudget` é maior que o tempo real que passa durante o próprio frame.
   - Resultado: o saldo `_clockBudget` acumula progressivamente a cada frame em vez de zerar, o loop `while(_clockBudget >= 2)` não consegue terminar, o frame rate desaba para zero e o jogo congela indefinidamente.
2. **Degradação e colapso visual:**
   - A 3.000x em 60 FPS, cada quadro na tela avança 50 segundos simulados (25 passos da simulação).
   - O método `_world.CaptureMotionBeforeStep` é chamado 25 vezes dentro do frame e sobregrava posições passadas; o `_world.Sync` só atualiza a tela no final do quadro.
   - Veículos e pedestres não se movem de forma contínua: eles simplesmente "teleportam" dezenas ou centenas de metros entre um frame e outro.
3. **Perda de eventos percebidos pelo jogador:**
   - Alertas pontuais de infraestrutura (ex.: queda momentânea de energia ou falta de água que dure 30 segundos) surgem e desaparecem em menos de um único quadro renderizado. O jogador nunca chega a ver a causa.
   - Obras rápidas começam e terminam instantaneamente, impedindo qualquer acompanhamento do trabalho dos operários.
4. **Escalabilidade com o crescimento da cidade:**
   - A medição acima ocorreu em uma cidade pequena (42 prédios). À medida que a cidade cresce para 150 ou 300 prédios, o custo de cada passo aumenta em função das verificações de tráfego e rotas.
   - Se o custo por passo subir de 0,2 ms para 2,0 ms, o teto da CPU cai de 1.950 ticks/s para 500 ticks/s, fazendo com que até velocidades como 1.000x entrem na espiral de congelamento.

---

## 3. Tempo real de 1 mês simulado e teto viável

Considerando 1 mês civil médio de 30 dias (2.592.000 segundos simulados):

| Multiplicador | Ticks necessários por segundo | Tempo real de 1 dia | Tempo real de 1 mês | Viabilidade técnica no motor |
| :---: | :---: | :---: | :---: | :---: |
| **1x** (tempo real) | 0,5 ticks/s | 24 horas | **720 horas (30 dias)** | Perfeita (referência canônica) |
| **15x** | 7,5 ticks/s | 1,6 hora (96 min) | **48 horas (2 dias)** | Perfeita |
| **60x** | 30 ticks/s | 24 minutos | **12 horas** | Perfeita (máximo da V1 atual) |
| **300x** | 150 ticks/s | 4,8 minutos | **2,4 horas (2h 24m)** | Boa (estável em cidade pequena) |
| **600x** | 300 ticks/s | 2,4 minutos | **1,2 hora (72 min)** | Boa em cidade pequena |
| **1.000x** | 500 ticks/s | 1,44 minuto (86 s) | **43,2 minutos** | Limite prático com perda visual |
| **3.000x** | 1.500 ticks/s | 28,8 segundos | **14,4 minutos** | Teto extremo; animação quebrada |
| **10.000x** | 5.000 ticks/s | 8,6 segundos | **4,3 minutos** | **Inviável** (excede capacidade da CPU) |

### Conclusão sobre o tempo de espera
Mesmo subindo a simulação para o teto extremo de **3.000x** (com CPU a 100% e veículos se teleportando), **1 mês ainda demora quase 15 minutos reais**. Em 1.000x, demora **mais de 40 minutos**.

Portanto, **aumentar a velocidade da simulação NÃO resolve o problema de o jogador ficar sem ter o que fazer esperando a arrecadação mensal**. Nenhuma criança ou adulto achará divertido ficar 15 a 45 minutos parado olhando para uma tela estática esperando o caixa subir para colocar a próxima rua.

---

## 4. Comparativo das alternativas de produto e gameplay

| Alternativa | Prós | Contras | Custo de Simulação | Aderência à Criança Construtora e Microgerenciamento (AGENTS.md) |
| :--- | :--- | :--- | :--- | :--- |
| **(a) Velocidades altas com rótulos genéricos (`>`, `>>`, `>>>`)** | Não promete multiplicador numérico exato; convenção familiar de outros jogos do gênero. | **Não resolve o problema de ritmo:** 1 mês ainda leva 15-45 minutos reais. Destrói a legibilidade visual da circulação (carros teleportam). Risco alto de travar cidades maiores. | Muito alto (exige saturar a CPU com 500 a 1.500 ticks/s). | **Ruim.** A criança aperta `>>>`, não vê as coisas acontecerem de forma compreensível e continua sem dinheiro para construir por longos minutos. |
| **(b) "Avançar até o próximo evento" (obra pronta, arrecadação)** | Dá controle intencional ao jogador; permite rodar em lote (batch) sem renderizar cada frame gráfico intermediário. | Simular 1 mês em lote ainda leva ~11 minutos de barra de progresso travada; exige lógica complexa de interrupção (crises, faltas de água) para a cidade não colapsar durante o salto. | Médio/Alto (processa todos os passos, apenas poupa tempo de renderização). | **Fraca.** A criança quer construir e ver sua cidade em ação, não interagir com modais de avanço e telas de espera. |
| **(c) Cobrança diária pro-rata (ou fluxo contínuo de receita)** | **Elimina a causa-raiz do problema:** o imposto anual é rateado por dia (`Imposto / 365`). O caixa da prefeitura recebe injeções diárias. Em 60x, entra dinheiro a cada 24 min; em 180x, a cada 8 min; em 300x, a cada 4,8 min! O jogador recupera fôlego para construir com frequência. | Exige ajuste na SPEC sobre o período contábil do imposto predial. Empresas e famílias precisam ter liquidez para saques diários. | **Zero.** `DailyFinance` já roda uma vez por dia no núcleo; o custo computacional é insignificante. | **Excelente.** A criança gasta o dinheiro construindo e vê o caixa voltar a subir aos poucos em tempo perceptível, permitindo continuar sua expansão sem travas artificiais. |
| **(d) Híbrido: Cobrança diária pro-rata + Velocidade moderada segura (180x/300x) com rótulos limpos + Proteção de frame** | Resolve o caixa sem exigir velocidades absurdas; mantém a circulação de carros visível e agradável; protege o motor contra a espiral da morte limitando passos máximos por frame; rótulos limpos na UI. | Requer harmonizar a calibração de caixa inicial, impostos diários e velocidade máxima recomendada. | Baixo a Moderado (mantém o motor na faixa confortável de 90 a 150 ticks/s). | **Excelente.** Atende plenamente à criança construtora (construção fluida e caixa progressivo) e ao adulto (consistência causal rigorosa, economia transparente e simulação estável). |

---

## 5. Recomendação histórica do estudo inicial (não aprovada)

1. **Não tentar resolver a falta de caixa aumentando a simulação para 1.000x ou 3.000x:**
   - As medições mostram que a física em passos de 2s impõe um teto severo. Mesmo no teto de 3.000x, 1 mês ainda exige quase 15 minutos de espera passiva, enquanto a experiência visual de trânsito é arruinada e o risco de congelamento por espiral da morte em cidades maiores se torna iminente.
2. **Atacar a causa-raiz através da receita contínua (Cobrança diária pro-rata):**
   - Alterar a apuração do imposto imobiliário na Prefeitura para regime diário pro-rata (`ValorAnual / 365`), processado no evento já existente `DailyFinance`.
   - Isso transforma o fluxo financeiro de uma "espera de 12 horas por uma bolha mensal" em um gotejamento diário contínuo: em velocidade 60x, uma nova receita entra a cada 24 minutos reais; em 180x, a cada 8 minutos reais (correção do coordenador: 1 dia simulado = 86.400 s; ÷60 = 24 min).
   - Isso desbloqueia a gameplay da criança construtora, que passa a ver o dinheiro acumulando aos poucos e pode planejar sua próxima obra sem ficar bloqueada.
3. **Adotar uma velocidade máxima segura e estável (ex.: 180x ou 300x) com rótulos `▶`, `▶▶`, `▶▶▶`:**
   - Adicionar uma velocidade rápida calibrada entre 180x (90 ticks/s) e 300x (150 ticks/s), onde o consumo de CPU é de apenas ~1 ms por frame e a física/interpolação permanecem suaves.
   - Apresentar na interface rótulos visuais limpos (`▶ 1×`, `▶▶ Médio`, `▶▶▶ Rápido`), desvinculando a expectativa do jogador de um multiplicador literal gigantesco.
4. **Implementar trava anti-espiral (clamp de passos por frame) no host Godot:**
   - Em `AdvanceRealClock`, limitar o número máximo de passos que podem rodar em um único frame (ex.: máximo de 10 a 15 iterações de 2s por frame). Se o computador do jogador tiver uma queda brusca de FPS, o jogo temporariamente desacelera o relógio em vez de entrar no loop infinito de acumulação de `_clockBudget`.

---

## Verificação independente (GPT-6.1) — PENDENTE de revisão humana

> **Revisão humana desta seção: PENDENTE.** Medição e análise independentes em 2026-10-16; não aprovam mudanças de produto. As seções anteriores foram preservadas. Base do código: `2131b4e2e5692b44c8b5835f8849dcb577cc2dde`.

### Método e diferenças de cenário

Ambiente: Windows, Intel Core i5-13420H (8 núcleos/12 threads), Godot 4.7.2 mono, .NET 8, build Debug. Execuções sequenciais, headless, com `--max-fps 144`. Havia outras instâncias Godot no computador; não houve isolamento de CPU. Os valores são amostras locais, não capacidade garantida para outras máquinas/cidades.

Recriei a cidade pelo teste `Entire_v1_catalog_can_be_built_and_operate_with_real_people_and_payments`, sem usar instrumentação do autor original. O snapshot final reproduziu **42 prédios, 245 nós, 47 SIMs, 37 veículos e 45 projetos**. Correções de composição: são **32 residências**, não 34; os outros 10 prédios incluem duas eólicas. Além disso, os 45 projetos do snapshot final estavam concluídos: quantidade de projetos não comprova obras em andamento.

Para atender ao cenário com obra ativa, gerei também um checkpoint imediatamente após `CityCycleTests.City()`, antes das 48 horas adicionais do teste: mesmas contagens, **uma obra ainda ativa**, horário 01/01/2026 16:00. Essa fixture foi obtida por teste temporário e validada com `CheckInvariants`; SHA-256: `6A2879A045C16650D76DCEB5984EC543EB3B7618EC975536C2F8B69DE8E44AB7`.

Cada multiplicador começou em processo novo, carregando a mesma fixture, sem aquecimento descartado. A instrumentação temporária substituiu somente o multiplicador experimental do slot 3, desligou autosave e contou chamadas reais a `Advance(2)`. Mediu o custo de `AdvanceRealClock` com `Stopwatch` e o intervalo real com `Time.GetTicksUsec`, mantendo `CaptureMotionBeforeStep`, `Sync` e HUD. O encerramento ocorreu no primeiro frame completo após 10 segundos reais; por isso há amostras maiores em sobrecarga. O tempo real inclui o custo do último frame, sem parar o cronômetro antes do processamento caro.

Definições: **ticks/s = passos concluídos / tempo real**; **FPS = chamadas completas a `_Process` / tempo real**; **razão = segundos simulados / (tempo real × multiplicador)**. FPS headless mede o host, não renderização/GPU. `CheckInvariants` rodou ao final de cada amostra e, na rodada com obra ativa, nos relatórios intermediários de aproximadamente um segundo. Instrumentação e teste temporário foram revertidos antes do commit; nenhum código do jogo integra esta entrega.

### Resultados com obra ativa

| Velocidade | Real (s) | Simulado (s) | Ticks/s | Simulação/frame (ms) | Apresentação/frame (ms) | FPS do host | Simulado/alvo | Atraso | Maior processamento de frame (ms) | Invariantes |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | :---: |
| 60x | 10,000 | 598 | 29,90 | 0,280 | 1,851 | 141,99 | 0,9966 | 0,34% | 65,36 | OK |
| 300x | 10,005 | 3.000 | 149,93 | 0,874 | 1,891 | 141,13 | 0,9995 | 0,05% | 65,39 | OK |
| 1000x | 10,003 | 9.996 | 499,63 | 2,872 | 2,208 | 130,56 | 0,9993 | 0,07% | 82,00 | OK |
| 3000x | 10,253 | 29.828 | 1.454,56 | 918,859 | 9,371 | 1,07 | 0,9697 | 3,03% | 1.983,56 | OK |
| 6000x | 14,222 | 56.436 | 1.984,17 | 1.765,606 | 8,305 | 0,56 | 0,6614 | 33,86% | 4.815,27 | OK |

Em 3000x houve apenas **11 frames completos**; em 6000x, **8**. Ambos terminaram normalmente e passaram nos invariantes, mas os frames de segundos representam pausas graves de resposta. A razão próxima de 1 em 3000x **não significa interface fluida**. Os pequenos déficits nas velocidades menores incluem a quantização de 2 segundos e o custo do último frame.

Como controle mais próximo do snapshot descrito originalmente, repeti as cinco velocidades também com todas as obras concluídas:

| Velocidade | Real (s) | Simulado (s) | Ticks/s | Simulação/frame (ms) | FPS do host | Simulado/alvo |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 60x | 10,006 | 600 | 29,98 | 0,289 | 141,92 | 0,9994 |
| 300x | 10,005 | 3.000 | 149,93 | 1,222 | 137,73 | 0,9995 |
| 1000x | 10,001 | 9.996 | 499,76 | 2,813 | 136,59 | 0,9995 |
| 3000x | 10,058 | 29.860 | 1.484,45 | 24,714 | 36,49 | 0,9896 |
| 6000x | 13,349 | 56.440 | 2.114,00 | 1.660,978 | 0,60 | 0,7047 |

Todos passaram em `CheckInvariants`. As diferenças entre checkpoints e a ausência de aquecimento impedem atribuir a queda de FPS exclusivamente à obra ativa. Não reproduzi os **144 FPS em 3000x** do estudo: mesmo o controle ficou em 36,49 FPS. Tampouco reproduzi congelamento permanente em 6000x: houve progresso, frames muito longos e encerramento normal.

### Estabilidade e movimento: o que foi e não foi verificado

- Não houve exceção, timeout de 60 segundos ou falha de invariantes nas dez amostras válidas. Os invariantes cobrem oferta monetária/saldos, estoques, combustível/carga, compromissos/obras, IDs, propriedade e famílias (`src/IndexCities.Simulation/CitySimulation.cs:82`). Não são uma auditoria completa de colisões, semáforos ou continuidade visual.
- Na fixture com obra ativa, a maior distância entre posições **da simulação** de um mesmo veículo em estado `Travelling` em dois frames consecutivos foi **46,38 m em 60x**, **147,35 m em 300x** e **312,58 m em 1000x**. Não são posições interpoladas do modelo visual. A métrica não separa um quantum físico normal de uma chegada/saída ou frame lento; não prova teletransporte físico. Em 3000x/6000x não houve deslocamento positivo capturado por esse filtro, com pouquíssimos frames; isso não comprova ausência de saltos.
- O risco de perder legibilidade entre frames permanece fundamentado: `CaptureMotionBeforeStep` sobrescreve a amostra anterior a cada passo (`src/IndexCities.Godot/CityWorld.cs:78`), e `Sync` calcula a apresentação a partir dessa última amostra (`src/IndexCities.Godot/CityWorld.cs:110`). Muitos passos por quadro reduzem a sequência apresentada. **Veículos visualmente “pulando” não foram confirmados por observação renderizada nesta verificação headless.**

### Conferência das três afirmações no código

1. **Passo de 2 segundos: confirmado na implementação atual.** `src/IndexCities.Godot/Main.cs:84` executa `while(_clockBudget>=2)` e chama `_sim.Advance(2)`. `src/IndexCities.Simulation/CitySimulation.cs:11` define `Quantum=>2`; `:65` fatia chamadas maiores em `Math.Min(Quantum,remaining)`. A velocidade aumenta a quantidade de passos, não o tamanho deles. Isso não demonstra, por si só, que 2 segundos seja um limite físico universal ou que todo defeito hipotético com outro `dt` já tenha sido reproduzido.
2. **Imposto mensal com alíquota anual: confirmado; atribuição a `DailyFinance`: incorreta para o lançamento do imposto atual.** A SPEC, linha 660, aprova valor de mercado × alíquota anual ÷ 12 por mês. `CitySimulation.cs:24` inicializa `NextTaxDate=State.Now.Date.AddMonths(1)`; `:79` chama `PropertyTaxes` quando `Now>=NextTaxDate` e avança a data com `AddMonths(1)`. `Finance.cs:38` contém `PropertyTaxes`, e `:41` cobra `MarketValue × TaxRates / 12m`, transfere o pagamento à cidade e registra a parte não paga como dívida. Não há lançamento anual integral nem rateio diário `/365` nesse caminho. Meses seguem o calendário, não duração fixa de 30 dias.
3. **`DailyFinance` existe, mas já há receitas municipais fora do imposto mensal.** O evento diário dispara em `CitySimulation.cs:78`, com datas inicializadas em `:24`. `Finance.cs:13` cobra água e energia efetivamente consumidas para a conta `city`; `:19` recupera dívidas. `RecoverDebt` também é chamado no ciclo econômico (`CitySimulation.cs:77`). Portanto, “esperar o mês para qualquer entrada no caixa” não descreve todos os fluxos existentes. Receita efetiva continua dependendo de consumo, serviço e dinheiro disponível dos devedores; a medição não identificou a causa de uma crise fiscal concreta.
4. **Limite de passos por frame: não existe no caminho atual.** Em `Main.cs:78–84` não há teto de iterações, prazo de CPU ou descarte/clamp do saldo. Porém, o `while` não acrescenta tempo real dentro dele: recebe um orçamento finito no começo da chamada e subtrai 2 por passo. Assim, a descrição original de “loop que não consegue terminar” é imprecisa. O risco confirmado é realimentação **entre frames**: o frame longo aumenta o intervalo cobrado na chamada seguinte, podendo produzir degradação progressiva. Não foi demonstrado loop literalmente infinito.

### Conclusões que permanecem e que precisam ser retiradas ou limitadas

- **Permanecem:** o modelo atual custa mais passos quando acelerado; falta proteção por frame; sobrecarga prejudica resposta e leitura; um mês idealizado de 30 dias exige 12 h em 60x, 43,2 min em 1000x e 14,4 min em 3000x, se o alvo for sustentado. Esses tempos são aritmética, não garantia de throughput nem diagnóstico de diversão.
- **Não se sustentam como afirmações gerais:** 3000x a 144 FPS; 6000x necessariamente a 0 FPS ou congelado para sempre; teto universal de 1.950 ticks/s; suavidade visual garantida em 180x/300x; ausência de defeitos físicos apenas porque invariantes passaram. O controle integrado chegou a 2.114 ticks/s, mas não repeti o `MonthlySoak` puro: isso não substitui um novo benchmark do núcleo nem estabelece outro teto fixo.
- **Precisam de nova avaliação de produto:** a tese de que falta de caixa tem como causa única a cobrança mensal, de que o jogador necessariamente fica passivo e de que cobrança diária resolve sua liquidez. Não foram medidos caixa disponível, compromissos, arrecadação líquida nem capacidade financeira dos pagadores ao longo de um mês. As alternativas diária/híbrida seguem opções de estudo, sujeitas a decisão humana e SPEC; não são implementação autorizada. A existência de `DailyFinance` não comprova custo adicional zero nem benefício econômico/UX.
- **Limites:** uma amostra por velocidade/checkpoint, sem aquecimento, sem isolamento de CPU, sem cidades maiores, sem soak mensal e sem renderização. São resultados independentes suficientes para contestar certezas do texto original, não para aprovar uma velocidade máxima ou política tributária.

### Validação e comandos

Comandos executados a partir da raiz da worktree; os caminhos completos dos argumentos do jogo evitam depender do diretório de trabalho adotado pela engine:

```powershell
$root = (Get-Location).Path
$godot = 'C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
dotnet test tests/IndexCities.Simulation.Tests --filter FullyQualifiedName~Entire_v1_catalog --logger 'console;verbosity=normal'
# 1 teste aprovado; cenário integrado e snapshot gerados.
dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj
# Após reverter a instrumentação: 0 erros; 1 aviso preexistente CS8629 em Main.ProblemAlertsQA.cs:198.
& $godot --headless --editor --import --path src/IndexCities.Godot --quit
# Importação concluída; arquivos .import/UID gerados foram revertidos/removidos.
& $godot --headless --path src/IndexCities.Godot --max-fps 144 -- "--fixture=$root/artifacts/verification/ritmo-independent/fixture-active.json" "--review-save-dir=$root/artifacts/verification/ritmo-independent/saves-qa" --performance-smoke --performance-speed=3
# Código restaurado, velocidade aprovada 60x: INDEXCITIES_PERFORMANCE_OK; 900 s simulados / 15,002793 s reais; invariantes OK.
```

Nas medições experimentais, o comando headless foi o mesmo formato, usando `--independent-speed=60`, `300`, `1000`, `3000` ou `6000` **em vez** das flags `--performance-*`, com uma pasta de saves isolada por velocidade. Essa flag existiu somente na instrumentação temporária removida; não é opção do jogo entregue. O checkpoint ativo foi gerado por `dotnet test tests/IndexCities.Simulation.Tests --filter FullyQualifiedName~Independent_fixture --logger 'console;verbosity=normal'` (1 aprovado), também removido. Logs, fixtures, cópias de segurança e JSONs brutos permanecem apenas em `artifacts/verification/ritmo-independent/`, ignorado pelo Git; os resultados duráveis estão nas tabelas desta seção.

---

## Revisão de engenharia e pesquisa externa — 2026-10-16 (PENDENTE)

> **Revisão humana desta seção: PENDENTE.** Revisão de código da `main` após merge da PR #19 (`b75be2f`), confronto com os benchmarks registrados acima e pesquisa em fontes técnicas primárias. Não foram executados novos benchmarks, nem aprovada nova arquitetura ou funcionalidade.

### Direção do produto confirmada pelo responsável

- **Calendário civil único e literal**, com dias de 24 horas, anos gregorianos e todos os efeitos físicos, financeiros e pessoais reais. Não encurtar meses, não criar relógios divergentes nem acelerar apenas a idade.
- O jogador deve poder **acompanhar uma geração inteira em algumas horas ou sessões**, usando aceleração opcional. O número concreto de horas, o tamanho da cidade e o hardware-alvo ainda precisam ser medidos/definidos. A SPEC é a fonte autorizada desta decisão.
- Essa meta **não está demonstrada** por 1000x, e não pode ser declarada concluída antes de implementar/validar nascimento, envelhecimento e morte natural na simulação.

### Dimensão quantitativa do problema

Usando 80 anos como horizonte ilustrativo (não idade fixa obrigatória de morte): a 1000x são cerca de **701 horas reais**; para 80 anos em 40 horas de partidas seriam necessários aproximadamente **17.500x**, e em 20 horas cerca de **35.100x** efetivos. Esses fatores são aritmética, **não capacidade comprovada**. Com a arquitetura atual `Quantum=2`, o último alvo exigiria ~17.500 passos completos por segundo real, contra ~500 passos/s efetivos no benchmark de 1000x com 47 SIMs e 42 edifícios. Mudar só o multiplicador não atende ao objetivo; crescer a cidade tende a ampliar a carga, mas quanto ainda não foi medido.

A verificação independente mediu em cenário ativo aproximadamente **130,56 FPS do host em 1000x**, **1,07 FPS em 3000x**, sem aquecimento, isolamento de CPU, validação gráfica ou execução longa. A descrição anterior de 3000x a 144 FPS não foi reproduzida. O loop `while(_clockBudget>=2)` tem orçamento finito **por chamada**, portanto não é um loop necessariamente infinito; o problema é retroalimentação de atrasos **entre frames**, bloqueio da interface e perda da velocidade efetiva. O quantum de 2s é uma **restrição da implementação atual**, não uma lei que impeça integrar intervalos estáveis por um algoritmo equivalente.

### Evidências primárias externas e limites de transferência

1. **A/B Street — simulação de tráfego por eventos:** movimentos previsíveis de pedestres tornam-se estados temporais interpoláveis até a próxima interação; cruzamentos acordam veículos quando a passagem fica disponível. O próprio autor documenta simplificações na física dos veículos e limites com ultrapassagem e congestionamento. **Aplicável como conceito**, não como motor de trânsito substituto sem conferir equivalência. https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
2. **Factorio FFF-421 e FFF-148:** entidades inativas podem dormir; a otimização dos roboports caiu de ~1 ms/tick para ~0,025 ms/tick **naquele subsistema e save específico**, não 40x para o jogo inteiro. Transporte por segmentos e índices reduzem operações repetidas sem abandonar a realidade dos itens. https://www.factorio.com/blog/post/fff-421 ; https://www.factorio.com/blog/post/fff-148
3. **SUMO MESO:** documenta até 100x no trânsito mesoscópico, **mudando detalhes** de faixas, ultrapassagem e cinemática. Não é prova de ganho compatível com a microssimulação aprovada. https://sumo.dlr.de/docs/Simulation/Meso.html
4. **Godot e .NET:** orientam medir gargalos antes de otimizar: CPU, GC, estruturas de dados, algoritmos, apresentação, custos de thread. RenderingServer/MultiMesh aceleram visualização, mas **não executam o mundo econômico**. https://docs.godotengine.org/pt-br/4.x/tutorials/performance/general_optimization.html ; https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-highcpu
5. **OpenTTD 14:** separar calendário da economia foi uma mudança estrutural de longo alcance com risco de datas incoerentes. Não corresponde à decisão atual do IndexCities de manter relógio único. https://www.openttd.org/news/2024/03/23/timekeeping

### Leitura do código e direção de investigação proposta (não aprovada)

- **Primeiro, perfil por subsistema e duração real:** no `CitySimulation.Step` tudo passa por uma sequência de loops a cada 2s; `MoveCitizens`, `MoveVehicles`, `OperateBusinesses`, `UpdateNeeds` e `UpdateInfrastructure` são alvos observáveis. Em `OperateBusinesses`, cada prédio percorre a lista de cidadãos para trabalhadores; infraestrutura também procura pessoas presentes por prédio. Medir antes de escolher índices ou caches específicos.
- **Depois, tornar o trabalho proporcional a mudanças reais:** considerar índices de quem trabalha/mora em cada imóvel, agenda de próxima decisão e eventos de transação/limiar, cálculo de consumo/produção/salários em intervalos estáveis e só reavaliar trajetórias quando interação ou invalidação exigir. Preservar pagamento com pagador real, ordem de disputa por estoque/vaga, rota e presença física, juros, salário, aluguel e dívida em suas datas corretas. Priorizar cortes locais reversíveis antes de um scheduler universal.
- **Eliminar travamento visual sem falsificar tempo:** orçamento máximo de processamento por frame com backlog contabilizado e velocidade efetiva exibida; a cidade não pode acumular tempo fictício só porque a UI continuou fluida. Estudar execução rápida em blocos sem renderizar cada passo intermediário, mas **com todos os eventos reais**, histórico consultável e alternância voluntária para velocidades baixas ao acompanhar uma pessoa. Modo novo de avanço exige decisão humana; render menos não significa simular menos acontecimentos.
- **Não apostar primeiro em multithreading/GPU/ECS/troca de engine:** podem fazer sentido após profiling, mas também adicionam ordem concorrente, sincronização e risco de alterar causalidade.
- **Validar com cidade crescente e horizontes progressivos**, mesma seed/estado, código otimizado versus referência, ciclos de uma semana/mês/ano, Release e Debug separados, com e sem Godot, aquecimento, percentis, GC, tempo real total, interatividade e comportamento de diferentes SIMs. Além de invariantes de dinheiro/estoque, conferir trajetórias, pagamentos, nascimento/envelhecimento/morte, eventos e save/load. Um teste de poucos segundos a 1000x não verifica viabilidade de um ciclo de vida inteiro.

### Economia: imposto diário é uma hipótese distinta

O imposto imobiliário atual é **lançado mensalmente** em `PropertyTaxes` como alíquota anual / 12; salários e água/energia já têm liquidações diárias. Cobrar diariamente mudaria liquidez e possivelmente decisões e inadimplência — portanto precisa de aprovação humana separada na SPEC. **Não há evidência de que só mudar a periodicidade resolva o caixa**: exemplo ilustrativo de 30 imóveis privados a 40.000 cada e 1% ao ano gera apenas ~32,9 unidades monetárias/dia, se tudo for efetivamente pago. Valor real, compromissos e receitas líquidas devem ser medidos na cidade do playtest antes de recomendar a mudança. Evitar dividir por 365 indiscriminadamente sem considerar ano bissexto, valor de mercado variável, arredondamento e diferença em relação à base mensal corrente.

**Síntese pendente:** a opção mais fiel à decisão humana é otimizar a **execução real dos acontecimentos** (menos varreduras e passos desnecessários, especialmente em intervalos estáveis), combinar apresentação legível com execução rápida e medir um objetivo geracional explícito. A magnitude do ganho necessária **não está comprovada**. Se benchmarks mostrarem limite persistente, expor o trade-off e pedir nova decisão; não alterar silenciosamente o calendário, criar gerações fictícias ou declarar sucesso com FPS apenas.
