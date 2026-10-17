# Revisão independente — frota por material + pavimentação física

- **Revisor (modelo efetivo):** `opencode/deepseek-v4.1-flash-free` (DeepSeek V4.1 Flash), conforme AGENTS.md §7.
- **Autor(es) do alvo:** GPT-6.1-sol (worker, esforço xhigh) e Claude Opus 5.5 (coordenação/cherry-pick). Modelos diferentes do revisor.
- **Worktree:** `C:\Users\Marlohn\orca\workspaces\IndexCities\pavimentacao-integracao`, branch `Marlohn/pavimentacao-integracao`.
- **Base do diff:** `7d4ab66..HEAD` (`HEAD` = `40d1b34`), 24 arquivos em `src/`+`tests/` (~515+/88-), mais commits de `docs/`.
- **Modo:** revisão somente-leitura. Não editei `src/`, `tests/` nem `docs/`; não commitei. A sonda de verificação foi criada fora do repositório (em `%TEMP%/opencode/cancelprobe`).
- **Status:** revisão executada; entrega **não validada** por revisão independente de outro modelo além desta.

## Verificação executada

| Comando | Resultado |
|---|---|
| `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj --filter "FullyQualifiedName!~CityCycleTests"` | **225 aprovados, 0 falhas** (51 s). Confirma o relato do autor. |
| Leitura de `git diff 7d4ab66..HEAD -- src tests docs` | Feita integralmente. |
| Sonda `cancelprobe` (fora do repo, referencia `IndexCities.Simulation.csproj`) | Reproduz os achados 1 e 2 (abaixo). |
| Evidência Godot do autor (`artifacts/verification/construction-integration/engine-*.log`) | Marcadores `INDEXCITIES_CONSTRUCTION_CORNER_OK` e `..._PAVING_OK` presentes; não reexecutei a engine (sem GPU garantida nesta sessão). |

Confirmei por leitura de código os pontos centrais do contrato: frota inicial fixa de **6 externos** (3 Dump / 1 Mixer / 1 Flatbed / 1 Cargo) em `CitySimulation.cs:27-28`; `BodyFor` em `Construction.Fleet.cs:5-7`; conversão única de save antigo em `Construction.Fleet.cs:17-48`; descarga de Asfalto no funil da pavimentadora presente em `Mobility.cs:280` e `Construction.cs:129-152`.

## Achados

### 1. [Médio] Cancelar rua parcialmente pavimentada deixa os trechos compactados fechados para sempre

- **Onde:** `src/IndexCities.Simulation/Construction.cs:261` (`CancelWork`) e `:249` (`CompleteConstructionProject`).
- **O que acontece:** `CompleteConstructionProject` libera `ConstructionOnly=false` para as vias do projeto; `CancelWork` **não**. Ao cancelar uma rua já parcialmente compactada, os nós criados pelo rolo (`Construction.Paving.cs:164`) permanecem com `ConstructionOnly=true` e `WorkProjectId=p.Id` indefinidamente.
- **Cenário reproduzido (sonda, seed 42, rua `(-192,0)->(-192,48)`, cancelada com 2/12 pontos rolados):**
  ```
  [before] status=InProgress progress=0,583 built=2 rolled=2/12
     road 29 ConstructionOnly=True normal=closed construction=open
  CancelProject success=True
  [after60] status=Cancelled progress=0,583
     road 29 ConstructionOnly=True normal=closed construction=open
  ```
  O trecho pago nunca abre ao trânsito comum, some das calçadas (`RoadNetwork.Sidewalks.cs:48,76,87`) e não vira rua utilizável — vira um toco fechado permanente.
- **Conflito com a fonte de verdade:** a própria HEAD (`40d1b34`) registra em `docs/spec/construcao.md:171` que "ao cancelar uma rua antes da conclusão integral, os trechos já compactados pelo rolo abrem ao trânsito como rua comum … e podem ser demolidos depois". O código diverge dessa regra.
- **Ressalva de escopo:** o relatório de integração (`artifacts/verification/construction-integration/report.md:25`) lista este item como "decisão humana em aberto". Como a HEAD já aprovou a opção A na SPEC, trato como divergência código×SPEC a confirmar; o código hoje assume o comportamento definitivo "permanece fechado".
- **Severidade:** Médio (perda de utilidade de trecho já pago; contradiz a SPEC vigente). Não bloqueia os fluxos testados, porque nenhum teste cobre esse cancelamento.

### 2. [Médio] Save legado com rua parcialmente pavimentada trava permanentemente em `NoBudgetForPaving`

- **Onde:** `src/IndexCities.Simulation/Construction.Fleet.cs:34-40` (`MigrateConstructionState`), commit `fcf6e89`.
- **O que mudou:** foi removida a linha `p.BudgetRemaining+=Math.Min(Available,p.MachineryQuoted)` (e o comentário "Real uncommitted city money only; never mint money to migrate a work."). O save legado de pavimentação passa a usar apenas a verba que já tinha.
- **Cenário reproduzido (mesma sonda, save legado sintético como o do teste):**
  ```
  budget=33,08  quoted=661,65  causeBefore="Aplicação e acabamento de asfalto em andamento"
  after 3600: status=InProgress progress=0,583
              cause="Sem verba restante para máquinas e operadores" machineryPaid=0
  ```
  A obra nunca avança nem conclui; o jogador fica com um trecho meio asfaltado preso.
- **Nota:** o próprio teste do autor foi alterado para codificar o travamento — `tests/.../PavingTests.cs:114` trocou `Assert.True(restored.Status==WorkStatus.Completed, …)` por `Assert.Equal(WorkCause.NoBudgetForPaving,restored.Cause)`; `:112` passou a exigir `ConstructionOnly` após a migração. Ou seja, é comportamento intencional do commit, mas **não há decisão registrada** em `docs/spec/construcao.md`/`docs/decisoes/` prevendo o travamento de saves antigos. O comentário anterior indicava que a verba vinha de dinheiro real disponível (não cunhagem; `ProjectPay` ainda guarda a transferência real).
- **Severidade:** Médio (soft-lock de save antigo). Precisa de confirmação: se for intencional, registrar; se não, restaurar o reforço de verba a partir de `Available` (dinheiro real já existente).

### 3. [Baixo] Migração pode atribuir `Cargo` (default do enum) a externos legados além da composição 3/1/1/1

- **Onde:** `src/IndexCities.Simulation/Construction.Fleet.cs:20,30`.
- **Detalhe:** `bodies` tem 6 posições; `bodies.FirstOrDefault(...)` retorna `default(TruckBody)` = `Cargo` quando todas as cotas estão preenchidas. Se um save legado tiver mais externos do que `ExternalTruckCount`, os excedentes viram `Cargo` em vez da mistura pretendida.
- **Cenário:** improvável com `ExternalTruckCount=6` (`Model.cs:84`), mas o código não é explícito sobre o limite. Sem falha nos testes atuais.
- **Severidade:** Baixo.

### 4. [Baixo/informativo] Afrouxamento aparente de asserts — em geral equivalente

- A maioria das trocas (`ConnectedRoadProjectTests.cs:97,120,152`, `ConstructionAndRoutingTests.cs:71`, `MaintenanceTests.cs:75`, `NearbyJunctionTests.cs`, `PedestrianCrossingTests.cs`, `PedestrianSidewalkTests.cs`, `PavingTests.cs:79`) é apenas `Assert.Equal(esperado, obtido)` → `Assert.True(obtido==esperado, Diagnostic(sim))`: **mesma semântica**, só melhora a mensagem. `NearbyJunctionTests` ainda **adicionou** uma asserção (todas as obras concluídas).
- Duas trocas **mudam** a expectativa: `PavingTests.cs:114` (acha 2) e `LogisticsAndPriorityTests.cs:17` (passou de "6 caminhões esperando com carga" fixo para `cancelledDeliveries.Length` e removeu `depot.Quantity(Wood)>0`). A de `LogisticsAndPriority` é defensável (frota finita); a de `PavingTests` codifica o achado 2.
- **Severidade:** Baixo (não encontrei regressão escondida por assert enfraquecido além do item 2).

### 5. [Baixo] Asfalto de origem local configura aproximação a partir do nó de entrada

- **Onde:** `src/IndexCities.Simulation/Mobility.cs:278` chama `ConfigureAsphaltApproach`, que usa `truck.Route[0]` (nó de entrada) como partida em `Construction.Feeding.cs` (`AsphaltApproach`), mesmo quando o caminhão está fisicamente no Pátio de origem (`source!=null`). A rota pode começar "para trás".
- **Cenário:** Asfalto reservado localmente (Pátio com estoque de Asfalto). Não reproduzi travamento; é ineficiência/possível gasto extra de combustível. O caminho de importação (`source==null`), que é o fluxo aprovado, está correto e testado.
- **Severidade:** Baixo.

## O que verifiquei sem encontrar problema

- Frota inicial: exatamente 6 externos, 3/1/1/1 por carroceria; nenhum veículo criado por diferenciação (`PavingTests.Specialist_bodies…`, `LegacyTruckFleetTests`).
- Conversão de save antigo: única, por identidade de `Id`, preserva cargas/identidades/dinheiro, determinística, não se repete após save/load (`LegacyTruckFleetTests`, `PavingTests.cs:112-114`).
- Descarga de Asfalto somente com pavimentadora real `OnSite/FeedingReady/CrewAboard` a ≤7 m e alinhada (`Mobility.cs:280`), consumida uma única vez na chegada ao canteiro.
- Correção do bloqueio mútuo rolo×caminhão: máquina bloqueada restaura o `Heading` verificado (`Construction.Feeding.cs` `MoveConstructionSitePath`, `Construction.Paving.cs:153-154`); teste regressivo com quantum 2 s e 1 s passa.
- Determinismo save/load dentro dos testes de pavimentação e de frota legada.

## Limitações desta revisão

- Não reexecutei os smokes renderizados no Godot (dependem de GPU/janela); usei apenas os marcadores/logs de evidência do autor. A validação visual de modelos/descarga não foi refeita por este revisor.
- Não executei `CityCycleTests` (soak) nesta rodada; o autor também registrou não tê-lo rodado na etapa original.
- Achados 1 e 2 foram confirmados por sonda determinística de simulação (sem renderização); os demais vêm de leitura de código.

## Encaminhamento

- **Achados 1 e 2 (Médio)** pedem decisão do coordenador antes de "pronto": o 1 confronta a SPEC vigente (`construcao.md:171`); o 2 pode ser intencional mas está sem registro. Os demais são baixos e não bloqueiam.
- A entrega permanece **não validada** até a revisão independente ser aceita e a validação humana da experiência ocorrer.
