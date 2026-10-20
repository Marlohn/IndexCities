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


---

## Análise adversarial da viabilidade geracional e escala futura — 2026-10-16 (PENDENTE)

> **Revisão humana desta seção: PENDENTE.** Nova bateria crítica solicitada porque a recomendação de eventos/otimização não demonstrou ainda que suportará a cidade completa. Consulta à SPEC, à V1 e a pesquisas externas primárias. **Nenhum novo benchmark foi executado nesta rodada.** Não é decisão de arquitetura, mudança de produto ou autorização para implementar.

### Pergunta correta e limites do que já medimos

**É possível manter o calendário gregoriano literal, cada SIM, viagem e transação com causalidade real, e acompanhar gerações ao longo de algumas sessões, mesmo quando toda a visão futura do produto estiver implementada?** A resposta sustentada hoje é **desconhecida, com risco alto e não resolvido**. Um teste de 10 segundos em 47 SIMs não valida anos de uma cidade de 15 mil a 50 mil pessoas; essas populações são apenas cenários exploratórios, **não meta aprovada**. A V1 ainda não implementa nascimento/envelhecimento natural e omite educação, saúde, polícia, indústria ampla, família avançada e muitos outros sistemas aprovados para o jogo completo. Assim, **não se pode prometer que otimizações locais, agenda de eventos, paralelismo, GPU ou uma troca de engine bastarão**.

Para 80 anos de referência (sem impor morte aos 80, 365,2425 dias/ano em média), o calendário contém aproximadamente 2,5246 bilhões de segundos e o motor de passo fixo exigiria cerca de **1,262 bilhão de passos de 2 segundos**. Para cumprir 80 anos em 20 horas reais são necessários **35.063x efetivos**, ou **17.532 passos/s** com o motor atual. Em 40 horas: 17.532x e 8.766 passos/s. O benchmark de 1000x com 47 SIMs executou ~500 passos/s. Essas contas exigem reduzir fortemente trabalho por unidade de tempo simulado, não apenas aumentar multiplicador.

### Piso ilustrativo de acontecimentos inevitáveis

**Exemplo, não previsão de comportamento:** com duas viagens realizadas por SIM por dia, ao longo dos mesmos 80 anos:
- 1.000 moradores: aproximadamente 58,4 milhões de viagens (811 viagens por segundo real num horizonte de 20h);
- 10.000 moradores: 584,4 milhões (~8.117 viagens/s);
- 50.000 moradores: 2,92 bilhões (~40.583 viagens/s).

Cada viagem real demanda pelo menos partida e chegada, e potencialmente múltiplos conflitos, decisões de trajeto, combustível e compras. Os números excluem salários, abastecimento, estoques, emergências, educação, história pessoal e moradia; também não incluem variações de população/rotina, nem afirmam que todos viajem duas vezes todos os dias. Eventos discretos **evitam rechecagens desnecessárias, mas não apagam o custo dos eventos obrigatórios**.

### Evidência externa que favorece e que limita

- **ParSim (Heidary et al., 2025, Applied Sciences)**: simulou 1 milhão de viagens de 24h em Bologna em ~41s; cenário maior de 18 milhões de viagens em San Francisco Bay Area consumiu ~26 min; o estudo também reporta configuração com 28 milhões em outras descrições. Isso prova velocidade especializada notável em GPU, **não simulação integrada de finanças, serviços e gerações**. A discussão reconhece que o tratamento simplificado de cruzamentos pode superestimar vazão. Fonte: https://www.mdpi.com/2076-3417/15/13/6960
- **MOSS (Zhang et al., 2024)**: simulação GPU de tráfego microscópico com mais de 2 milhões de veículos e até ~100x sobre CityFlow naquele teste, usando modelo próprio e hardware específico. Um passo padrão é 1s; não extrapolar multiplicador 100x comparativo como anos do IndexCities em tempo de jogo. Fonte: https://arxiv.org/html/2405.12520v1
- **A/B Street (Carlino)**: processamento por eventos permite interpolar caminhada, mas o projeto assume pedestres sem colisão e movimento simplificado de carros; a equivalência com regras atuais do IndexCities exige provas, e congestionamentos podem invalidar previsões. Fonte: https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
- **Factorio FFF-421**: paralelismo em um save real deu ~9,5% na rodada citada, enquanto um benchmark sintético específico teve ganho de 14,9x; paralelizar rede elétrica falhou em melhorar o tempo total porque o gargalo era memória. Ganhos setoriais são importantes, mas não viram garantia de aceleração global. Fonte: https://www.factorio.com/blog/post/fff-421
- **SimMobility/MATSim, pesquisa de agentes de tráfego**: modelos eficazes de larga escala diferenciam granularidades curtas, médias e longas e frequentemente usam um modelo de transporte especializado, não a combinação completa de sistema social, propriedades, consumo físico, política urbana e décadas com as exigências do IndexCities. Não tomar a existência de milhões de agentes como prova da meta geracional. Revisão: https://arxiv.org/html/2102.07505v2
- **Godot, documentação oficial**: perfilar CPU/GC e atenção a restrições de threads; mudar linguagem, GPU ou engine não remove automaticamente o trabalho causal. Fonte: https://docs.godotengine.org/en/stable/tutorials/performance/cpu_optimization.html

### Três caminhos de engenharia ainda sob teste (nenhum aprovado nesta rodada)

1. **Execução orientada a eventos e intervalos analíticos equivalentes**, inclusive em sistemas não visuais, sem novas decisões por SIM enquanto nada mudou. Hipótese com maior potencial de reduzir trabalho vazio; **pode perder eficiência com interações contínuas e elevada taxa de invalidação**.
2. **Motor de dados compactos e índices + kernels paralelos nas partes efetivamente independentes**. Combina estruturas de dados locais, memória contígua e, somente se perfil indicar, threads ou GPU para trânsito/infraestrutura. Requer manter ordem de disputa por recursos e mesmo resultado causal; dependências fortes, sincronização e cópias entre CPU/GPU podem anular o ganho.
3. **Execução rápida em blocos sem desenhar cada instante, com histórico persistente de acontecimentos reais e observação detalhada quando jogador desacelera**. Ajuda custo visual e legibilidade, mas **não substitui nem simplifica o custo físico/econômico do core**. Controles novos dependem de aprovação própria.

**Rejeitar como solução baseada na evidência atual:** só dobrar multiplicadores; prometer 3000x/35000x; usar FPS de 47 SIMs como escala futura; simular somente cidadãos visíveis; substituir viagens por médias sem validação; distribuir threads universalmente; declarar que GPU ou ECS produzirão ganhos de papers especializados na cidade inteira.

### Experimento decisivo de risco antes de refatoração ampla

Proposta **para avaliação humana**, não nova fase mandatória: curto *stress test* reprodutível do core, executando a mesma cidade durante uma semana e um mês, e checkpoint de ano quando custo permitir. Instrumentar CPU e GC por subsistema; tempo real, segundos simulados por segundo, memória, fila de eventos, número efetivo de viagens/transações, latência ao alterar velocidade e invariantes físicos/econômicos. Executar em Release e Debug separadamente, CPU isolada quando possível, com e sem host Godot, e em estados de **47, 1.000, 10.000, 50.000 habitantes** em uma progressão que interrompa antecipadamente os cenários inviáveis. Usar fixtures tecnicamente coerentes e documentar funcionalidades ausentes; fixtures artificiais não provam realismo de futuras populações. O tamanho de cidade e infraestrutura sintéticos precisam refletir um futuro plausível, não apenas multiplicar cópias dos mesmos moradores.

**Critério de decisão:** medir custo por dia simulado (não FPS apenas), tendência do custo por morador/empresa/viagem, total de acontecimentos e picos; identificar quais partes podem dormir, quais devem acontecer realmente e se as otimizações reduzem os custos reais. Antes de uma refatoração grande, exigir um ganho mensurado end-to-end em caso representativo e conferir o custo de integrar novos sistemas. Se o throughput por dia, extrapolado com **margem explícita** para sistemas ainda não implementados, não permitir chegar à meta geracional, não insistir indefinidamente em otimizações micro. Levar ao responsável opções reais de escala/nível de interação/hardware como novo trade-off, **sem mudar o calendário ou os eventos aprovados por conta própria**.

**Conclusão adversarial:** a tese de eventos continua plausível para otimizar, mas **a hipótese de que será suficiente para todo o IndexCities não está demonstrada e deve ser tratada como risco central**. A próxima ação útil é um teste falsificável de escalabilidade, e não mais uma pesquisa genérica ou reescrita antecipada.


---

## Medição de escala do núcleo — 2026-10-16 (PENDENTE)

> **Pesquisa, não fonte de verdade.** Primeira medição real de custo por dia simulado, feita fora do repositório (cópia do núcleo `IndexCities.Simulation` da `main` em `90c0f72`, num projeto de bancada descartável), sem alterar código, regras ou o jogo. Atualiza a seção anterior, que não tinha benchmarks novos.

### Método

- Release, .NET 8, console sem Godot nem renderização, uma thread, Windows 11, CPU desktop de 12 threads. Cronômetro por subsistema dentro de `Step`. Aquecimento de 2 dias simulados antes de medir; depois, dia a dia.
- **Cidade A:** exatamente o roteiro de `CityCycleTests.City()` (47 SIMs, obras reais).
- **Cidades em grade:** sintéticas, colocadas com os comandos públicos (mesma validação) e **concluídas instantaneamente** em vez de obra física. Capacidade das instalações de água, esgoto e energia multiplicada por 50 só para poucas instalações atenderem a cidade (não muda o custo de cálculo). Lago reposicionado ao lado da grade. Cruzamentos de 4 vias receberam semáforo (ação disponível ao jogador; ver achado abaixo).
- **Sonda de otimização:** segunda cópia com índices por ID (no lugar de `FirstOrDefault` lineares em prédios, famílias, empresas, SIMs e veículos) e agrupamento de trabalhadores por prédio em uma passada. **Equivalência verificada:** o save JSON depois de 2 dias tem o mesmo SHA-256 da versão original (cidade A e grade de 109 SIMs).

### Resultados

| Cenário | SIMs | Prédios / nós de rua | s reais por dia simulado | Velocidade efetiva | 80 anos (29.220 dias) |
|---|---|---|---|---|---|
| A, original | 47 | 42 / ~200 | 10,6 | ~8.100x | ~86 h |
| A, sonda | 47 | 42 / ~200 | 5,2 | ~16.600x | ~42 h |
| Grade 3×400 m, original | 109 | 97 / 400 | 30 | ~2.900x | ~244 h |
| Grade 3×400 m, sonda | 109 | 97 / 400 | 14 | ~6.100x | ~114 h |
| Grade 5×800 m, sonda (inválida: ver abaixo) | 400 chegando (91 dentro do mapa) | 337 / 1.214 | 197 | ~440x | ~1.600 h |

Meta pedida (80 anos em 20–40 h) equivale a **2,5–4,9 s reais por dia simulado**.

- **Distribuição do custo (A, original):** infraestrutura 53%, necessidades 24%, operação de empresas 14%, movimento de SIMs 5%, veículos 2%; economia periódica, finanças diárias e impostos <1%. Na grade de 400, com trânsito parado, **veículos 70%**.
- **Crescimento:** de 47 para 109 SIMs o custo triplicou. Varreduras "todos os prédios × todos os SIMs", busca de pedestre contra todos os pedestres, de travessia contra todas as ruas, e poluição local somando todas as células do mapa por casa **a cada 2 s** tornam o custo superlinear.
- **Trabalho desperdiçado:** a cidade A gera ~15 registros de história pessoal e ~3 lançamentos financeiros por SIM por dia, mas o passo fixo recalcula todos os SIMs, prédios e ruas 43.200 vezes por dia. Alocações: 4–6 GB por dia simulado mesmo com 47 SIMs.
- **Memória e saves:** histórico pessoal e livro-caixa crescem sem limite (~15 e ~3 itens por SIM/dia). Para 10.000 SIMs em 80 anos seriam ~4 bilhões de registros de história, o que não cabe em memória nem em save JSON. Guardar a história consultável a longo prazo exige uma decisão de produto sobre retenção/resumo.

### Achados colaterais (possíveis bugs, não confirmados no jogo)

- Grade com cruzamentos de 4 vias em preferência à direita (padrão de `SetDefaultJunctions`): carros ficaram em "Fila na faixa" / "Aguardando preferência" por dias; trabalhadores de água não chegaram; SIMs morreram de fome. Com semáforo o trânsito fluiu.
- ~~400 famílias migrando de uma vez: ~240 carros parados na entrada por 3 dias; 41–51 mortes.~~ **Corrigido em 2026-10-16: artefato da cidade de teste.** A meia-largura do mapa (606 m) não era múltipla de 4, e a rodovia ficou desalinhada das ruas, com nós a 2 m formando vários cruzamentos colados na entrada. Com a grade alinhada, as 400 pessoas chegam sem travar. A linha "Grade 5×800" da tabela acima também foi afetada por isso e não deve ser usada. A grade 3×400 (406 m) tinha o mesmo desalinhamento, sem travamento visível; seus números serão refeitos com a grade alinhada.
- Alguns caminhões de empresa ficaram em "Aguardando motorista embarcar" indefinidamente.

### Leitura

1. **Com o passo fixo atual a meta é inviável**, inclusive na cidade de 47 SIMs (86 h; 42 h com a sonda). Com 400 SIMs a sonda já está ~40–80x acima da meta e o custo cresce mais que linearmente; para milhares de SIMs a distância tende a ser maior, mas não foi medida.
2. Otimizações locais equivalentes dão ~2x e são válidas por si (menos CPU em qualquer velocidade), mas **não mudam a conclusão**.
3. A meta só parece alcançável se o custo passar a depender de **acontecimentos** e não de **entidades × tempo**: no meio da faixa da meta (~3,7 s por dia, 30 h para 80 anos), 10.000 SIMs com ~20 eventos/dia são ~54 mil eventos por segundo, volume normal para um motor de eventos. **Não há evidência ainda de que isso possa ser feito com equivalência causal.** (O censo abaixo mostrou que o maior custo não é o movimento.)
4. Sistemas futuros da SPEC (nascimento/envelhecimento, educação, saúde, indústria, transporte coletivo) acrescentam entidades e viagens; com o modelo atual o custo cresce junto.

### Ferramenta de medição

`src/IndexCities.Headless` reproduz estas medições sem Godot, usando o núcleo real (não uma cópia):

```powershell
dotnet build src/IndexCities.Headless -c Release
dotnet src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll scale cycle --days=7
dotnet src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll scale grid --rows=3 --length=400 --days=3
dotnet src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll census grid --rows=3 --length=400 --days=3
```

Imprime tempo real por dia simulado, horas estimadas para 80 anos, GC, memória, tamanho/tempo de save e o SHA-256 do save final. Mesmo SHA antes e depois de uma otimização indica comportamento idêntico (`scale cycle --days=2` deu `5ED4C3E031A41342` na `main` de 2026-10-16). A grade conclui as obras na hora via métodos privados do núcleo, então **não mede construção**. A divisão do custo por subsistema citada acima veio de uma cópia instrumentada temporária, que não está no repositório.

### Censo de movimento — 2026-10-16 (PENDENTE)

`census` classifica cada SIM e veículo após cada passo de 2 s (`--days=3`: 3 dias medidos após 2 de aquecimento; janelas diferentes mudam as proporções dos veículos presos):

| Cidade | SIMs parados | andando | em veículo | caminhada livre | caminhadas/dia | viagens de veículo/dia |
|---|---|---|---|---|---|---|
| A (47 SIMs) | 98,8% | 1,2% | 0,1% | 99,9% | 617 | 47 |
| Grade 3×400 (109 SIMs) | 97,3% | 0,9% | 1,8% | 99,5% | 569 | 82 |

- Na cidade A, 98% dos passos de veículos em viagem são livres. Na grade, só 1,2%: o restante são **três caminhões presos indefinidamente** ("Aguardando espaço para acessar a faixa" 64%, "Aguardando motorista embarcar" 34%). Isso parece bug, não carga real.
- **Correção da leitura anterior:** nessas escalas o custo dominante **não é o movimento** (5–7% do tempo de CPU na cidade A, pela cópia instrumentada citada acima; o censo mede estados, não tempo). É recalcular a cada 2 s necessidades, infraestrutura e operação de empresas de agentes parados, cujo estado só muda em poucos momentos do dia: chegada, saída, compra, troca de turno, refeição, falta de água/energia. Movimento e interação só dominam com trânsito travado (grade de 400).

### Próximo experimento sugerido (proposta, não aprovado)

1. **Primeiro, agenda para sistemas estacionários.** Protótipo isolado no qual necessidades, consumo de água/energia, salário por hora e operação de empresas são integrados entre mudanças de estado, em vez de somados a cada 2 s. Recalcula só quando algo muda: chegada/saída, turno, compra, limiar de fome/saúde, capacidade de serviço. Comparar com o motor atual na cidade A e na grade de 109: mesmos eventos pessoais e lançamentos financeiros, saldos finais e mortes/migrações. Diferenças só de arredondamento de ponto flutuante exigem uma **tolerância definida antes**: por exemplo, mesmo conjunto de eventos e horários com diferença de no máximo um passo (2 s), saldos iguais ao centavo. **Critério de sucesso:** ≤ 1 s por dia simulado na grade de 109 SIMs.
2. **Depois, mobilidade por eventos**, só se o passo 1 deixar o movimento como gargalo medido.
3. **Separadamente,** investigar como bugs os travamentos de cruzamento de 4 vias e de caminhões parados, que distorcem qualquer medição em cidades maiores.

### Resultado do experimento 1 — sistemas estacionários em intervalo maior (2026-10-16, PENDENTE)

> **Pesquisa, não decisão.** Branch `experimento/agenda-estacionaria`, que **não deve ser mesclada como está**. Necessidades, infraestrutura, operação de empresas e condição de manutenção rodam a cada `--interval` segundos com o intervalo inteiro, em vez de a cada passo de 2 s. Movimento, obras, economia periódica e finanças continuam iguais, e o acumulado é sempre descarregado antes da virada do dia e da cobrança mensal. Com `--interval=2` o save tem o mesmo SHA do motor atual.

**Velocidade** (rodadas sequenciais, 2 dias medidos):

| Cidade | 2 s (atual) | 60 s | 300 s |
|---|---|---|---|
| A (47 SIMs) | 9,6 s/dia (78 h para 80 anos) | 0,96 s/dia (8 h) | 0,72 s/dia (6 h) |
| Grade 109 SIMs | 36,5 s/dia | dia 1: 4,8 s; dia 2: 64 s | 4,0 s/dia |

Na grade, o salto do dia 2 com 60 s não vem do protótipo. Três caminhões presos em "Aguardando espaço para acessar a faixa" mantêm `MoveVehicles` no modo de subpassos de entrada na faixa (`Mobility.LaneEntry.cs`), que passou a ocupar 92% do tempo. É um custo por passo que nunca termina porque o caminhão nunca entra. **Corrigir esse travamento é pré-requisito para medir cidades maiores.**

**Fidelidade:** 7 dias na grade de 109 SIMs. Motor atual com sementes 42/43/44 (variação natural) contra intervalos de 10/30/60/300 s com semente 42:

| Agregado em 7 dias | Motor atual (3 sementes) | Intervalos 10–300 s |
|---|---|---|
| Mortes | 0 – 7 | 3 – 6 |
| Refeições | 1.592 – 1.882 | 1.555 – 1.676 |
| Salários pagos | 11.225 – 12.952 | 11.213 – 12.031 |
| Contas de água / energia (valor) | 192–241 / 217–247 | 189–209 / 205–232 |
| Saúde média | 83,6 – 98,5 | 82,8 – 90,8 |

Os números ficaram dentro da faixa natural, e o dinheiro se conservou em todas as rodadas (invariantes OK). **Isso não prova equivalência:** foi uma semente por intervalo, 7 dias e uma cidade pequena. A comparação acontecimento a acontecimento não é possível, porque diferenças mínimas de horário mudam decisões seguintes (efeito borboleta) e as cidades divergem em poucos dias, inclusive entre sementes do próprio motor atual.

**O que o intervalo maior muda de fato:** um SIM que chega ao trabalho no meio do intervalo só começa a ganhar salário na avaliação seguinte; a refeição e a falta de água são percebidas com até um intervalo de atraso; os valores de água/energia do HUD se atualizam a cada intervalo. Com 60 s, isso dá até 1 minuto em turnos de 8 horas. **Se algo assim for adotado no jogo, é decisão do responsável**, porque afeta o que a SPEC chama de efeitos equivalentes.

**Leitura:** para a cidade pequena e sem veículos presos, o ganho de ~10x põe 80 anos na faixa de algumas horas headless. É a primeira evidência de que a meta pode não ser impossível. Para escalas maiores, faltam: corrigir os veículos presos; medir de novo 400+ SIMs sem travamento; repetir com várias sementes por intervalo; e só então decidir sobre uma versão orientada a eventos (recalcular ao chegar, sair ou trocar de turno) em vez de intervalo fixo.

### Resultado do item 2 — 400 e 1.000 SIMs após corrigir os travamentos (2026-10-17, PENDENTE)

> **Pesquisa, não decisão.** Mesma branch experimental (`experimento/agenda-estacionaria`), já com as correções de trânsito das PRs #27 e #28. Cidade em grade crescendo por casas novas (`--growth`) até a população-alvo, dois dias de aquecimento e então medição.

**400 SIMs, três sementes, motor atual (2 s) contra intervalo de 60 s** (3 dias medidos; as seis rodadas correram em paralelo, o que infla os tempos — a rodada sequencial de 60 s com semente 42 deu 38 s/dia contra 63 s/dia em paralelo):

| Semente | 2 s: s/dia (h para 80 anos) | 60 s: s/dia (h para 80 anos) | Refeições 2 s / 60 s | Gasto com alimentos 2 s / 60 s | Satisfação média 2 s / 60 s |
|---|---|---|---|---|---|
| 42 | 467 (3.794) | 63 (511) | 3.932 / 3.966 | 39.513 / 37.387 | 71,8 / 75,0 |
| 43 | 407 (3.302) | 79 (644) | 4.034 / 4.063 | 35.793 / 34.856 | 72,0 / 75,4 |
| 44 | 457 (3.708) | 124 (1.009) | 4.046 / 4.079 | 40.540 / 35.134 | 71,6 / 75,0 |

- Nenhum travamento de trânsito; dinheiro conservado.
- **Fidelidade:** o número de refeições bate, mas o gasto com alimentos fica 2–13% menor e a **satisfação média sobe ~3 pontos em todas as sementes, fora da faixa natural do motor atual**. É uma diferença sistemática, não ruído: o intervalo fixo de 60 s muda o jogo de forma mensurável.

**1.000 SIMs (1.037), intervalo de 60 s, semente 42, rodada sequencial com perfil:**

- **3.936 s por dia simulado (o cálculo é só 22x mais rápido que o tempo do jogo) → ~31.900 h para 80 anos.** O crescimento até 1.000 SIMs levou 24 min e o aquecimento de 2 dias, 2 h 25 min.
- De 400 para 1.000 SIMs (2,5x), o tempo por dia com 60 s subiu ~100x (38 s → 3.936 s): **crescimento muito pior que linear**.
- Perfil: **`MoveCitizens` 94,6%**, revisão econômica 3,5%, o resto abaixo de 1%. O intervalo maior não alcança o movimento, que continua a cada 2 s; na escala de 1.000 é ele, não os sistemas estacionários, que decide.
- Alocação: ~2,7 TB alocados por dia simulado (427 mil coletas gen0), com memória residente de só 69 MB — muita alocação temporária por passo.
- A rodada de 1.000 SIMs com o motor atual (2 s) não foi medida até o fim: só o crescimento já passava de uma hora.

**Leitura:** o intervalo fixo ajuda até algumas centenas de SIMs, mas muda resultados (satisfação) e não resolve a escala de 1.000+, onde a caminhada domina com custo que cresce mais que linearmente (verificações de distância contra todos os pedestres e consultas lineares por prédio, ver medição de escala acima). O próximo passo técnico que não muda regra nenhuma é atacar esse custo — índice espacial para pedestres, dicionário por Id para prédios, menos alocação por passo — cada um com prova de SHA idêntico pela bancada. Só depois disso faz sentido decidir entre intervalo maior, agenda de próximos acontecimentos ou outra arquitetura.

### Veículos presos na grade: causa e correção (2026-10-17, PENDENTE)

> **Pesquisa, não decisão.** Registro do diagnóstico que acompanhou a correção na branch `Marlohn/veiculos-presos`. A regra é a da SPEC ([transporte-circulacao](../spec/transporte-circulacao.md), "Veículo parado na via não trava a rua"); aqui ficam só a causa e as medições.

- **Causa:** os caminhões externos que ficavam sem combustível eram da **equipe de manutenção viária**. O projeto agrupa até 16 trechos por célula de 64 m, ordenados pela distância à entrada. Numa célula com duas ruas paralelas, os trechos alternam entre elas, e o caminhão se reposicionava a cada trecho (~250 m por salto) sem nova checagem. O tanque de 40 L acabava no meio da rua. Daí vinham as filas, os carros presos saindo da vaga, as mortes por fome dentro deles e o modo de subpassos permanente.
- **Correção aplicada:** a equipe confere trecho + volta ao exterior antes de cada reposicionamento; se faltar, volta para reabastecer na importação e a obra chama equipe de novo. O caminhão local confere a volta à fazenda. A parada no posto exige estoque e dinheiro para o resto da viagem; abastecimento parcial não parte.
- **Medição (`stuck grid --rows=3 --length=400 --days=11`, após 2 dias de aquecimento):**

| Semente | Sem combustível na via (main → corrigido) | Parados ≥30 min, máx. | Mortes | Dia mais lento ÷ mais rápido |
|---|---|---|---|---|
| 42 | 4 → 0 | 18 → 0 | 11 → 0 | 3,8 → 1,1 |
| 7 | 4 → 0 | 15 → 0 | 6 → 0 | 4,3 → 1,1 |
| 43 | 4 → 0 | 25 → 0 | 19 → 0 | 5,5 → 1,1 |

  Cidade `cycle`, 10 dias: 0/0 nos dois casos e a mesma morte pré-existente. As rodadas do main correram em paralelo; compare a razão dentro da mesma rodada, não os segundos absolutos.
- **Subpassos com veículo parado:** com o bug, todo passo de 2 s entrava no modo de subpassos (~12 fatias por passo). As fatias vinham da chegada prevista de veículos retidos na fila atrás do parado, que nunca acontecia. Agora entrada bloqueada por veículo parado ou pela fila retida atrás dele não gera subpassos. Sem veículo parado o cálculo é idêntico (SHA igual ao main em `cycle` e na grade, 3 dias).
- **Em aberto:**
  - **B2:** carro saindo da vaga com veículo parado na frente.
  - **B3:** duas frentes de fila travadas entre dois parados em faixas opostas.
  - **Custo do reenvio da equipe externa.**
  - **Motorista preso em caminhão sem posto viável.**
  - **Ordem em zigue-zague dos trechos de manutenção:** cada salto custa combustível e, agora, reenvios pagos. Melhoria técnica possível, ainda não feita.
### Otimizações equivalentes do núcleo — 2026-10-17 (PENDENTE)

> **Pesquisa, não decisão.** Branch `Marlohn/otimizacao-caminhada`. Opção B aprovada pelo responsável em 2026-10-17: só otimizações de desempenho que preservam o comportamento. Nenhuma regra, tuning, ordem de eventos, causa exibida ou formato de save mudou; o SHA-256 do save da bancada é **idêntico antes e depois** em todas as cidades medidas. Nada aqui decide intervalo maior, agenda de eventos ou threads.

**Onde estava o custo** (perfil por amostragem com `dotnet-trace`, motor atual de 2 s, antes das mudanças):

| Cidade | Pontos quentes |
|---|---|
| Grade 3×400, 108 SIMs | infraestrutura 45%, veículos 21% (cópia e ordenação de todos os veículos a cada passo), necessidades 15%, operação de empresas 12% |
| Grade 5×800 crescendo, ~300–400 SIMs | infraestrutura 41%, necessidades 37%, operação de empresas 9%: buscas lineares por Id (`FamilyById`, `BuildingById`), trabalhadores presentes contados prédio × todos os SIMs, dicionários alocados a cada passo |
| Grade 7×1500, ~1.000 SIMs | revisão de empregos 75% (uma busca de rota por SIM × prédio e contagem de vagas sobre todos os SIMs); depois dela, `PedestrianPath` 92% |

O "`MoveCitizens` 94,6%" da seção anterior era quase todo **planejamento de rota no início de cada caminhada** (`CitizenRoutine` → `StartJourney` → `PedestrianPath`), não o andar a cada passo: a recuperação de quem está dentro da faixa de rolamento fazia uma busca de rota completa por faixa de pedestre do mapa (centenas por caminhada), e a escolha da travessia compartilhada fazia outra busca e um recálculo de geometria (varredura de todas as ruas) por faixa candidata. Por isso o custo crescia muito mais que linearmente.

**O que mudou** (cada item verificado com SHA idêntico antes de seguir):

1. Índices por Id validados na consulta para prédios, famílias, empresas, SIMs e veículos, no lugar de `FirstOrDefault` linear (ver `docs/ARCHITECTURE.md` §8).
2. Trabalhadores presentes e equipe no trabalho contados numa passada sobre os SIMs, em vez de prédio × SIMs.
3. Infraestrutura com buffers reutilizados; a divisão de capacidade (`CapacityAllocation`) usa arrays por posição com as mesmas somas na mesma ordem.
4. Poluição: fator `exp(−d/64)` em cache por ponto amostrado e por posição das células; casas somadas em lotes de quatro com acumuladores independentes, cada soma na ordem das células; decaimento sem buscas repetidas.
5. Pedestres: travessia procurada só na lista de faixas do passo; fila de pedestres num laço simples que ignora quem está longe demais para limitar o passo.
6. Veículos: ordenação por `Array.Sort` com chave total no lugar de `OrderBy(...).ThenBy(...)`.
7. Revisão de empregos: alcance viário calculado uma vez por origem (mesma regra de `FindRoadRoute`) e contagem viva de vagas.
8. Planejamento de caminhada: uma árvore de busca por origem para a faixa mais próxima (mesmas rotas e comprimentos, parando quando uma camada inteira passa do limite); travessia compartilhada testando os candidatos do mais próximo ao mais distante com uma busca retomável; geometria das faixas em cache só durante `Advance`, invalidada por mudança da rede viária.

**Prova de equivalência** (16 primeiros hex do SHA-256 do save JSON ao fim da rodada; "antes" = commit `620b770`, núcleo igual ao da `main`; "depois" = cabeça da branch):

| Cidade | SIMs | Semente | Dias (aquecimento + medidos) | Antes | Depois |
|---|---|---|---|---|---|
| `cycle` (obras reais) | 47 | 42 | 2 + 2 | `E1B7DAE87D62EB41` | `E1B7DAE87D62EB41` |
| `cycle` | 50 | 7 | 2 + 2 | `A18A692CC553ECE9` | `A18A692CC553ECE9` |
| Grade 3×400 | 108 | 42 | 2 + 2 | `A584C1D808CCA8B3` | `A584C1D808CCA8B3` |
| Grade 3×400 | 107 | 7 | 2 + 2 | `A38AFBEF2B813E57` | `A38AFBEF2B813E57` |
| Grade 5×800, `--growth=10` | 400 | 42 | 25 h + 2 + 1 | `F980C11F933AD247` | `F980C11F933AD247` |
| Grade 5×800, `--growth=10` (1 morte) | 408 | 43 | 25 h + 2 + 1 | `EE1B03B8512D4343` | `EE1B03B8512D4343` |

Também: `dotnet test` 244/244; Godot 4.7.2 com `INDEXCITIES_SMOKE_OK`, `INDEXCITIES_PERFORMANCE_OK` e `INDEXCITIES_PERSISTENCE_OK`.

**Tempos** (Release, .NET 8, Windows 11, CPU de 12 threads; rodadas **sequenciais** com a máquina livre, semente 42; "s/dia" é tempo real por dia simulado nos dias medidos, depois do aquecimento):

| Cidade | SIMs | Antes: s/dia (h para 80 anos) | Depois: s/dia (h para 80 anos) | Ganho | Alocação/dia antes → depois |
|---|---|---|---|---|---|
| `cycle` | 47 | 11,0 (90) | 2,22 (18) | 5,0× | 6,3 GB → 0,49 GB |
| Grade 3×400 | 108 | 31,5 (256) | 4,62 (38) | 6,8× | 9,6 GB → 0,94 GB |
| Grade 5×800 com crescimento | 400 | 361 (2.930) | 19,3 (156) | 18,7× | 36,6 GB → 3,1 GB |

Na grade de 400 SIMs, o crescimento (25 h simuladas) caiu de 166 s para 17 s, e o aquecimento de 2 dias, de 705 s para 38 s. A grade de 1.000 SIMs está registrada abaixo.

**Divisão por subsistema depois** (cronômetros temporários em `Step`, fora do repositório; mesmos SHAs), em s/dia:

| Subsistema | 108 SIMs antes | 108 SIMs depois | 400 SIMs depois |
|---|---|---|---|
| Infraestrutura | 14,1 | 2,15 | 7,29 |
| Necessidades (inclui poluição das casas) | 11,3 | 0,92 | 4,74 |
| Operação de empresas | 5,67 | 0,56 | 1,88 |
| Veículos | 0,61 | 0,27 | 1,72 |
| Pedestres | 1,51 | 0,29 | 1,67 |
| Condição de manutenção | 0,22 | 0,22 | 0,69 |
| Revisão econômica | 0,31 | 0,12 | 0,44 |

**Grade de 1.000 SIMs** (Grade 7×1500, `--growth=10 --warmup=1 --days=1`, semente 42, só depois e com os cronômetros temporários; o "antes" não foi medido nesse tamanho; o crescimento agora leva 307 s): 1.076 SIMs depois de 69 h de crescimento, 1.084 SIMs com 585 veículos ao fim do aquecimento de 1 dia (140 s). O dia medido levou **3.236 s** (~26.000 h para 80 anos), com **96% em veículos**: a cidade entra no modo de subpassos de entrada em faixa (veículos aguardando a entrada se reavaliam a cada subpasso; reservas de canteiro e o quadro de entrada a cada fatia) e aloca ~460 GB por dia. Os demais sistemas somam ~67 s/dia (pedestres), ~24 s (infraestrutura), ~23 s (necessidades), ~11 s (revisão econômica) e ~6 s (operação). Esse modo é efeito do tráfego daquela cidade, não custo fixo por SIM; reduzi-lo sem mudar a ordem dos eventos pede estudo próprio e ficou fora desta rodada.

**Leitura:**

1. Sem mudar uma regra, o motor de 2 s ficou **5–19× mais rápido** nestas cidades, e o ganho cresce com o tamanho, porque o que foi removido eram custos quadráticos ou cúbicos (entidades × entidades, rotas × faixas). A cidade de 47 SIMs chega a ~18 h para 80 anos, dentro da meta de 20–40 h. A de 400 SIMs fica em ~156 h, ainda 4–8× acima da meta.
2. O que sobra é, em maior parte, trabalho **linear por passo** sobre todos os prédios e SIMs: infraestrutura, necessidades e operação recalculadas a cada 2 s para agentes parados (ver censo acima). Daqui em diante, ganhos grandes pedem mudar **quando** se calcula (intervalo, agenda de acontecimentos), e isso já é decisão de produto (ver experimento 1).
3. A soma de poluição por casa (casas × células) é o único termo ainda quadrático nos sistemas estacionários. Somá-la noutra ordem mudaria o resultado no último bit (portanto o SHA) e ficou fora desta rodada.
4. Ficaram de fora, por não serem custo por passo: `UpdateCrosswalks` (varre todas as ruas para cada rua; roda ao confirmar ou concluir obras e domina o crescimento da grade de 1.000 SIMs) e a fila de entrada em faixa (abaixo).

**Ferramenta:** a bancada ganhou `--growth=N` (casas novas por hora simulada até ocupar a grade, vinda da branch experimental, sem o `--interval`; escala o tanque como lá, só nesse modo) e `--warmup=N` (dias de aquecimento, padrão 2):

```powershell
dotnet src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll scale grid --rows=5 --length=800 --growth=10 --days=1 --cap=100000
```

**Revisão independente:** DeepSeek V4.1 Flash (`opencode/deepseek-v4.1-flash-free`), em duas rodadas e sem achados Alto ou Médio. O revisor reproduziu os SHAs (6/6 depois; 5/6 antes, porque o sexto não terminou no tempo dele) e rodou sondas próprias contra reimplementações literais do código removido (≈2 milhões de checagens, 0 divergências). As notas que ele deixou foram corrigidas: redação da ARCHITECTURE, premissa de Id único documentada e buffer esvaziado ao fim do lote. Relatórios em `artifacts/verification/otimizacao-caminhada/review-deepseek/`.

## 2026-10-19 — quantidades inteiras: comparação pareada com a main

Medição técnica, sem autoridade de produto. Autorização do responsável registrada em [mundo-tempo-saves](../spec/mundo-tempo-saves.md#tempo-de-jogo); inventário de acumuladores e limites para uma agenda em [pesquisa do motor, §17](simulation-engine-performance-research.md#17-inventário-de-quantidades-acumuladas-no-núcleo-2026-10-18).

**Árvores medidas:** antes = `origin/main` em `13b8e98` (inclui a PR #38), depois = `de62d24` (main integrada e correções da revisão). .NET 8.0.32, Release, aquecimento de 2 dias. `cycle`: `scale cycle --days=7`; `grid`: `stuck grid --rows=3 --length=400 --days=11`; cada comando com `--seed=42`, `7` e `43`, `--warmup=2` e `--cap=3600`. Foram 12 execuções completas, em série, antes/depois por cenário e semente, sem builds concorrentes deste worker. Refeições são os acontecimentos reais de consumo doméstico; salários são pagamentos da categoria `Salários / <prédio>`; médias usam SIMs vivos e residentes.

O baseline saiu de `git archive 13b8e98`; seu núcleo e testes ficaram intactos. Somente `Headless/Program.cs` (métricas) e `Fixtures.cs` (acesso válido da captação na semente 43) foram copiados da branch para usar a mesma bancada nas duas árvores. O script e os hashes dos arquivos/bibliotecas estão em `artifacts/verification/fixed-quantities/resume-bench.ps1` e `resume-bench-metadata.txt`; logs completos em `resume-{before,after}-{cycle,grid}-{42,7,43}.log`. Esta rodada substitui a comparação anterior contra `c50923a`.

| Cenário | Seed | Mortes antes → depois | Refeições antes → depois | Salários antes → depois | Saúde média antes → depois | Satisfação média antes → depois | Circulação antes → depois |
|---|---|---|---|---|---|---|---|
| cycle, 7 dias | 42 | 1 → 1 | 840 → 840 | 5297,66 → 5297,66 | 99,999989 → 99,999989 | 69,718855 → 69,718807 | em viagem no fim: 0 → 0 |
| cycle, 7 dias | 7 | 0 → 0 | 894 → 894 | 5353,94 → 5353,94 | 99,999984 → 99,999984 | 66,648491 → 66,648428 | em viagem no fim: 0 → 0 |
| cycle, 7 dias | 43 | 0 → 0 | 948 → 948 | 5428,14 → 5428,14 | 99,999995 → 99,999995 | 73,438801 → 73,438740 | em viagem no fim: 0 → 0 |
| grid, 11 dias | 42 | 0 → 0 | 2700 → 2700 | 19013,13 → 19013,78 | 99,999996 → 99,999996 | 68,829473 → 68,805196 | parados ≥30min (máx): 0 → 0 |
| grid, 11 dias | 7 | 0 → 0 | 2675 → 2675 | 18962,69 → 18962,11 | 99,999996 → 99,999996 | 68,935944 → 68,873065 | parados ≥30min (máx): 0 → 0 |
| grid, 11 dias | 43 | 0 → 0 | 2750 → 2750 | 19021,82 → 19021,69 | 99,999996 → 99,999996 | 68,916647 → 68,994654 | parados ≥30min (máx): 0 → 0 |

No ciclo, `scale` confirma zero veículos em viagem no encerramento; não mede o máximo de imobilizados ao longo do período. Na grade, `stuck` mede o máximo de veículos sem deslocamento por pelo menos 30 min e a falta de combustível acumulada; ambos zero nas três sementes, antes/depois. Nenhum guincho foi chamado nestas grades sem pane forçada; elas não exercitam o resgate por si.

| Cenário | Seed | Eventos pessoais antes → depois | Variação | Lançamentos antes → depois | Δ salários | Δ satisfação |
|---|---|---|---|---|---|---|
| cycle | 42 | 5856 → 5856 | 0 (0%) | 2298 → 2298 | 0,00 | -0,000048 |
| cycle | 7 | 4547 → 4547 | 0 (0%) | 2019 → 2019 | 0,00 | -0,000063 |
| cycle | 43 | 4832 → 4832 | 0 (0%) | 2327 → 2327 | 0,00 | -0,000061 |
| grid | 42 | 13626 → 13609 | -17 (-0,125%) | 4716 → 4722 | +0,65 | -0,024277 |
| grid | 7 | 13119 → 13156 | +37 (+0,282%) | 4716 → 4728 | -0,58 | -0,062879 |
| grid | 43 | 13678 → 13878 | +200 (+1,462%) | 4762 → 4783 | -0,13 | +0,078007 |

**Correção da bancada, aplicada igualmente antes/depois:** o acesso da captação no ciclo com seed 43 era rejeitado porque atravessava a Casa #260. A fixture conserva o traçado original se `QuoteRoad` o aceita; caso contrário usa a faixa livre z=32. O núcleo não foi alterado. O ciclo43 foi medido já com esse ajuste; 42/7 mantêm seus traçados originais. A falha original foi preservada em `before-cycle-43-original-fixture.log`.

**Leitura pareada:** mortes e refeições coincidiram nas seis cidades, e a saúde média coincidiu nas seis casas exibidas. Nos ciclos, os salários e as contagens finais de eventos/lançamentos também coincidiram. Nas grades, há divergência de trajetória após a quantização, não apenas diferença no último dígito: na semente 43 são +200 eventos pessoais (cerca de +1,5%) e +21 lançamentos; a satisfação sobe 0,078007 ponto. Esses números não provam equivalência de trajetórias, e a dispersão entre sementes não é usada para minimizá-los. As métricas e invariantes observadas não detectaram regressão de regra, mas não explicam qual decisão inicial separou cada par de trajetórias. SHAs de formatos distintos não são comparáveis como prova de equivalência. Os tempos estão nos logs como contexto; esta rodada não estabelece ganho de desempenho.

**Validação atual:** main `13b8e98`: **278/278** testes; branch `de62d24`: **299/299**, sem falhas/ignorados nem alteração das expectativas da suíte original. Os 21 casos novos cobrem soma de incrementos quantizados, fome/saúde variável, ordem refeição/limites, recorrências, estoques/cópias, round-trip bruto acima de 2^53, 100 anos, maior estoque atual e estouro; incluem cinco resíduos de poluição que devem decair até a remoção, oito versões numéricas inválidas e guarda por lista permitida de doubles persistidos. Logs: `resume-main-tests.log`, `resume-targeted-tests.log` e `resume-tests.log`.

No Godot 4.7.2 mono, `dotnet build -t:Rebuild` e importação reais, seguidos de `-- --qa-list` (36 cenários), `-- --smoke` e `-- --persistence-smoke`. Marcadores: `INDEXCITIES_SMOKE_OK buildings=0 roads=17` e `INDEXCITIES_PERSISTENCE_OK ... continuation=identical ... directory=.../resume-godot-saves/saves-d61dce6601474939920afe9e61e12b1c`. Diretório absoluto isolado: `C:/Users/Marlohn/orca/workspaces/IndexCities/quantidades-inteiras/artifacts/verification/fixed-quantities/resume-godot-saves`; arquivos confirmados nesse destino. Logs e hash da biblioteca reconstruída: `resume-godot-*`. São cenários headless; não substituem playtest visual/humano. O smoke simples não grava cidade.

**Revisão:** achados bloqueantes e ajustes baratos da revisão Claude Haiku 5.5 sobre `a9625b4` corrigidos: comparação atual, decaimento residual, escrita de QA, guarda de persistência, exceção de versão e campo morto. A revisão final independente será despachada pelo coordenador depois do PR; ainda não executada nesta retomada. Permanecem os limites numéricos de borda: quantização recebe double, fatores double viram decimal nas recorrências e `HungerHours` converte horas em segundos antes de quantizar. Isso não permite transformar a futura agenda em soma de taxas constantes para estados não lineares.

## 2026-10-19 — rotas a pé guardadas e decisões que não recalculam (PENDENTE)

> **Pesquisa, não decisão.** Branch `Marlohn/rotas-a-pe-cache`. Só desempenho: nenhuma regra, tuning, ordem de eventos ou formato de save mudou, e os 16 primeiros hex do SHA-256 do save da bancada (a convenção desta bancada) são **idênticos antes e depois** nas cinco cidades medidas. A estrutura nova está em `docs/ARCHITECTURE.md` §8.

**Por que tantas rotas** (sonda temporária de contadores, fora do repositório; grade de 1.000 SIMs, semente 42, dia medido): 135.113 buscas de rota a pé por dia, das quais **129.600 falham** e quase todas repetem exatamente a mesma origem, destino e malha (95% de repetição já no aquecimento). Elas vêm de ~3 SIMs em "Sem rota" (100.800 tentativas de voltar para casa e 28.800 de ir ao trabalho por dia): um SIM parado em "Sem rota" cuja `PathRoadRevision` difere da revisão atual da malha tem a próxima decisão antecipada a cada passo (`Mobility.cs`, início de `MoveCitizens`), e uma tentativa que falha não atualiza essa revisão. Assim, a espera de 300 s depois de "Sem rota" nunca acontece: o SIM tenta de novo a cada 2 s, sempre em vão. Cada tentativa também chama `TryDrive` antes da rota a pé (136.183 chamadas/dia; 87 mil param em "família sem carro", 46 mil adiante, perto do carro). **Possível bug de comportamento, não corrigido aqui**: corrigir mudaria o resultado e pede decisão sobre a regra esperada.

**O que mudou** (cada item com SHA idêntico):

1. Rotas a pé guardadas por ponto exato de origem, porta e nó de acesso, só durante `Advance`, invalidadas por qualquer mudança da malha ou dos lotes e conferidas por inteiro entre chamadas (`RoadNetwork.WalkRoutes.cs`). As tentativas repetidas viram consulta a um dicionário: na sonda, a busca a pé caiu de 49 s para 0,7 s por dia, e `CitizenRoutine`, de 66 s para 15 s.
2. `FamilyCar` monta as contas `sim:<id>` da família uma vez por chamada, em vez de uma vez por veículo e membro.
3. `WalkCrossesRoad` descarta pela caixa do trajeto (com 1 m de folga) as ruas que não podem estar a menos de 4,65 m dele, antes do cálculo original.

**Prova de equivalência** (16 primeiros hex do SHA-256 do save; antes = `origin/main` em `58bed59`, depois = cabeça da branch; `--cap=100000`):

| Cidade | Semente | Comando | Antes | Depois |
|---|---|---|---|---|
| `cycle` | 42 | `scale cycle --days=2` | `419A5ACE934BD16F` | `419A5ACE934BD16F` |
| `cycle` | 7 | `scale cycle --days=2` | `19BBA169594EE657` | `19BBA169594EE657` |
| Grade 3×400 | 42 | `scale grid --rows=3 --length=400 --days=2` | `B6F9736766F55B54` | `B6F9736766F55B54` |
| Grade 3×400 | 7 | `scale grid --rows=3 --length=400 --days=2` | `D841FA8845927281` | `D841FA8845927281` |
| Grade 7×1440, ~1.000 SIMs | 42 | `scale grid --rows=7 --length=1440 --growth=40 --days=1` | `B262964FF295F874` | `B262964FF295F874` |

**Tempos na grade de 1.000** (Release, .NET 8.0.32, CPU de 12 threads, rodadas em série; havia processos de outra worktree em parte do tempo, de uma thread cada, então a variação entre rodadas é real; s = tempo real):

| Rodada (ordem) | Dia medido: antes → depois | Aquecimento de 2 dias: antes → depois | Concorrência observada |
|---|---|---|---|
| 1 (antes, depois) | 88,05 → 84,53 | 169,0 → 110,6 | processo de outra worktree começou durante o "depois" |
| 2 (depois, antes) | 84,19 → 54,20 | 167,9 → 102,2 | um processo de uma thread de outra worktree nas duas |
| 3 (antes, depois) | 95,82 → 64,63 | 222,0 → 141,9 | `dotnet test` de outra worktree no fim do "antes" e em todo o "depois" |
| **Mediana** | **88,05 → 64,63 (−27%)** | **169,0 → 110,6 (−35%)** | |

Alocação no dia medido: 54.042 → 15.915 MB (−71%); coletas gen0: ~8.690 → ~2.600. O crescimento (16 h simuladas) não muda além do ruído (72–96 s antes, 85–101 s depois). Na sonda de contadores, que cronometra cada chamada, a parte cortada soma ~50 s/dia; o ganho menor no cronômetro de parede sugere que a sonda superestima o custo por chamada e que a rodada 1 do "depois" sofreu interferência.

**Leitura:** o ganho vem quase todo de não refazer a mesma busca a pé fracassada a cada 2 s. Ele depende de haver SIMs nesse laço: na grade 3×400 (semente 42, 1 dia) a sonda não registrou nenhuma tentativa fracassada e só 552 buscas a pé; no `cycle` não contei; nelas não medi diferença confiável de tempo (na revisão, o `cycle` ficou dentro do ruído). A causa do laço (a nova tentativa antecipada a cada passo) segue intacta e ainda custa `TryDrive` e a própria decisão a cada 2 s; resolvê-la é decisão de produto, não otimização. Também não houve ganho nos outros ~50 s/dia (veículos, infraestrutura, necessidades), fora do alvo desta rodada.

**Validação:** `dotnet test` 299/299 (inclui dois testes novos em `WalkRouteCacheTests`, que conferem cada rota guardada contra uma busca nova depois de mudanças feitas por fora entre chamadas, inclusive restaurando os valores; os dois falham quando a conferência `Matches` é desligada, e o segundo também quando a cópia não é descartada junto com as rotas). Godot 4.7.2 mono: `-- --smoke` (`INDEXCITIES_SMOKE_OK buildings=0 roads=17`) e `-- --persistence-smoke` (`INDEXCITIES_PERSISTENCE_OK ... continuation=identical`), com saves em `artifacts/verification/rotas-a-pe-cache/` (caminho absoluto). São cenários headless; não substituem playtest visual.

**Revisão independente:** DeepSeek V4.1 Flash (`opencode/deepseek-v4.1-flash-free`). Sem achados Alto ou Médio; confirmou a equivalência de `FamilyCar` e `WalkCrossesRoad`. Na 1ª rodada, achou um contraexemplo Baixo real ao contrato: se o estado fosse alterado por fora e depois restaurado exatamente entre chamadas de `Advance`, a cópia antiga voltaria a coincidir e aceitaria rotas calculadas no estado alterado (inalcançável pelo jogo; sem efeito no SHA). Corrigido (a cópia é descartada junto com as rotas) e coberto por teste. Também foram incluídos os Ids na comparação, e o teste foi reforçado. Na 2ª rodada (mesmo modelo; a primeira tentativa estourou o tempo limite sem saída), sem Alto ou Médio: confirmou a correção em todas as sequências (mudança externa, carimbo no meio de `Advance`, cache vazio, exceção) e, em cópia própria, que os testes falham com cada defeito; os ajustes de redação que apontou foram aplicados. Relatórios em `artifacts/verification/rotas-a-pe-cache/` (fora do Git).
