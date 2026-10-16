# IndexCities — Estudo de ritmo e velocidades de simulação

> **Status de revisão humana: PENDENTE — não é fonte de verdade.**  
> Referências normativas: Produto: [SPEC](../SPEC.md). Estrutura: [ARCHITECTURE](../ARCHITECTURE.md). Regras de trabalho: [AGENTS](../../AGENTS.md).

---

## Contexto e hipótese do responsável

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

## 5. Recomendação justificada

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
