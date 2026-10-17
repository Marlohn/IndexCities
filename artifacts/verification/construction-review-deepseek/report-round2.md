# Revisão independente — rodada 2 (cancelamento abre trechos, saves antigos, reserva de trajeto de máquina)

- **Revisor (modelo efetivo):** `opencode/deepseek-v4.1-flash-free` (DeepSeek V4.1 Flash), iniciado com `-m opencode/deepseek-v4.1-flash-free`, conforme AGENTS.md §7.
- **Autores do alvo:** Claude Opus 5.5 (`claude-opus-5-5`, co-autor de todos os commits da faixa). Modelo diferente do revisor.
- **Worktree:** `C:\Users\Marlohn\orca\workspaces\IndexCities\pavimentacao-integracao`, branch `Marlohn/pavimentacao-integracao`.
- **Faixa revisada:** `git diff 40d1b34..HEAD -- src tests docs` (`HEAD` = `d9fe9e0`). Commits: `260d1d0`, `9b84bc6`, `7f1efb7`, `f8ec946`, `f624404`, `d9fe9e0`.
- **Base anterior:** rodada 1 (`7d4ab66..40d1b34`, relatório `.../construction-review-deepseek/report.md`).
- **Modo:** revisão **somente leitura**. Não editei `src/`, `tests/` nem `docs/`; não commitei. A sonda própria ficou fora do repositório (`%TEMP%/opencode/r2probe`), referenciando o `.csproj` de simulação.
- **Status:** revisão executada. A entrega **não** está validada até a revisão independente ser aceita e a validação humana ocorrer.

## Verificação executada

| Comando | Resultado |
|---|---|
| `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj --filter "FullyQualifiedName~MachineExitTests"` | **8 aprovados, 0 falhas** (29 s). |
| `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj` | **230 aprovados, 0 falhas** (3 m 1 s), inclui `CityCycleTests` e `MachineExitTests`. |
| `dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj` | **0 erros** (1 aviso CS8629 pré-existente em `Main.ProblemAlertsQA.cs`, fora da faixa). |
| Sonda `r2probe sweep 300 lake` (seeds independentes `s*997+13`) | **300/300 sem falha** (0 sobreposições, 0 travamentos). |
| Sonda `r2probe sweep 100 straight L T corner two-teams busy` | **600/600 sem falha**. |
| Sonda `r2probe det lake <26 seeds>` (save→load em reserva ativa, 150 passos de 1 s) | **26/26 determinístico** (`SaveToJson` idêntico a cada passo). |
| Sonda `r2probe perf` | lago 2877: 0,88 ms/passo; `busy` 1: 0,43 ms/passo. |
| Sonda `r2probe halt` | reproduz o achado 1 abaixo. |
| `scripts/Validate-Docs.ps1` | 63 arquivos, 355 links, 0 quebrados. |

Não reexecutei os smokes **renderizados** no Godot (exigem janela/GPU e edição de fixtures); usei os marcadores/logs de evidência do autor. A sonda cobre simulação pura.

## Confirmações (o que foi verificado sem problema)

- **Regra SPEC de cancelamento (achado 1 da rodada 1) está implementada.** `OpenCompactedRoads` (`Construction.cs:256`) libera `ConstructionOnly` **apenas** para vias com `WorkProjectId==p.Id`, que são exatamente as células criadas pelo rolo em `Construction.Paving.cs:164`; pontos só planejados nunca viraram rua. Chamado na conclusão (`:249`) e no cancelamento (`:267`). Bate com `docs/spec/construcao.md:168,171`. Teste novo cobre (`PavingTests.cs:37-51`, `Cancelling_a_partial_street_opens_only_the_compacted_cells`) e o smoke `--construction-paving-smoke` passou a exigir `compacted=opened` (`Main.ConstructionPavingQA.cs:69-77`).
- **Remoção da conversão de saves antigos está completa e sem código morto.** `MigrateConstructionState`/`CompleteLegacyTruckConversion` (`Construction.Fleet.cs`), `LegacyBodyAfterReturn`/`HasExplicitBody` (`Model.cs`) e a chamada nos construtores (`CitySimulation.cs:31,33`) não têm mais nenhuma referência (`grep` sem resultados). `Persistence.cs:13` rejeita `ConstructionStateVersion<1` com a mensagem de versão antiga. `ConstructionStateVersion=1` continua gravado em cidades novas (`CitySimulation.cs:21`).
- **Menu e aviso.** `CitySaves.ListAll()` marca e lista o save antigo (`CitySaves.cs:36-47`); `List()` exclui os antigos (`:33`); o menu renderiza "versão antiga do jogo" sem botão de renomear (`Main.MenuCards.cs:45,48`). `LoadCity` (`Main.cs:212`) só atribui `_sim` **depois** de `FromJson` ter sucesso; no erro cai no `catch` e mostra "Falha ao carregar: … versão antiga …" via `SetStatus` (`Main.UI.cs:25`), sem trocar a partida nem apagar o arquivo. Teste cobre isso (`Main.QA.cs:18-21`).
- **Determinismo save/load com a reserva ativa.** `MachineReservation` só lê estado serializado (`State.Vehicles`, `SitePath`, `SitePathIndex`, `MachinePhase`, `Cause`, `SiteDockHeading`); não muta nada. Confirmado por sonda em 26 seeds do lago a partir de uma reserva ativa (150 passos de 1 s, `SaveToJson` idêntico). A regra de heading em `Mobility.cs:221` também é determinística.
- **Não permite atravessar corpos.** A checagem de reserva usa a mesma `BodiesOverlap` com margem `+0.1` (`Construction.Feeding.cs:93,122-130`); a sonda monitorou penetração física máquina×veículo **a cada passo** em 900 cidades independentes + as 8 do teste: **0 sobreposições**.
- **Não cria novo impasse entre veículos/máquinas.** Só veículos não-máquina constroem reserva (`Construction.Feeding.cs:87`); máquinas ignoram reservas e continuam testando só corpos (`:92`). Logo não há reserva mútua cruzada entre veículos, nem entre máquinas. Varredura própria (900 cidades) + varredura do autor (700 + 300) = **0 travamentos**.
- **Testes não foram afrouxados indevidamente.** A única remoção é `LegacyTruckFleetTests.cs` (−123 linhas), coerente com a remoção da migração; `PavingTests.Legacy_partial_streets…` virou `Saves_from_before_material_trucks_are_refused_instead_of_converted` (mesmo comportamento da nova decisão). A troca no smoke da esquina (ordem → passagem física) foi aprovada pelo coordenador (ASK opção A) e é mais rica que a anterior (não sobreposição, não giro no lugar, espera fora do trajeto). Composição da frota 3/1/1/1 continua coberta (`PavingTests.cs:139-142`).

## Achados

### 1. [Médio] Máquina que fica sem combustível ao **sair** do canteiro não é excluída da reserva e prende o trajeto inteiro

- **Onde:** `src/IndexCities.Simulation/Construction.Feeding.cs:105` (exclusão) e `Construction.Feeding.cs:67` (causa efetivamente gravada).
- **Detalhe:** a exclusão de máquina parada é `machine.Cause is WorkCause.PavingOutOfFuel or WorkCause.NoBudgetForPaving`. Isso cobre o caso **OnSite** (`Construction.Paving.cs:152` grava `WorkCause.PavingOutOfFuel`). Porém, no caminho de **saída** (`MoveConstructionSitePath`), a falta de combustível grava uma **string avulsa** — `vehicle.Cause="Sem combustível para manobra no canteiro"` (`:67`) — e não o `WorkCause`. Essa string não é `WorkCause.PavingOutOfFuel`, então a máquina **não** é excluída e reserva **todo o trajeto restante** para sempre. Contradiz o comentário do próprio código (`:99-101`) e o relatório do autor ("Máquina parada por falta de combustível ou verba não reserva nada", `construction-exit-deadlock/report.md:34,38`).
- **Cenário concreto (reproduzido na sonda, `halt corner 42`):** máquina de pavimentação fica sem combustível trabalhando, o jogador cancela a obra (`CancelProject`). `ReleasePavingMachines`→`BeginLeavingSite` põe a máquina em `LeavingSite`; `ReturnPavingMachines`→`MoveConstructionSitePath` grava a string:
  ```
  machine=Paver at (-190,132) fuel=0,00
  cause='Sem combustível para manobra no canteiro'  (excludedAsFuel=False)
  site path (34 poses): (-192,132) (-192,128) … (-192,4) (-194,0)
  ```
  Ou seja, a máquina presa (sem mecanismo de reabastecimento no canteiro — `EnsureFuel` só existe para caminhões) reserva **132 m de rua dentro do mapa**. Caminhões de outra obra que usem essa rua ficam impedidos indefinidamente. Também reproduzido em `busy` e `straight`.
- **Impacto:** é um estado alcançável em jogo (cidade sem verba/combustível para pavimentar → o jogador cancela a obra — ação natural). Amplia o bloqueio do **corpo** da máquina (~4 m) para **todo o trajeto de saída**. Não observei sobreposição nem novo laço de veículos nas varreduras, mas o bloqueio permanente de uma rua inteira pode travar outra obra.
- **Severidade:** **Médio**. Correção pontual: incluir `PavingSiteOccupied`/a string de falta de manobra na exclusão **ou** gravar `WorkCause.PavingOutOfFuel` também em `Construction.Feeding.cs:67`.
- **Classificação:** bug de implementação de comportamento já aprovado (a própria intenção registrada é "não reserva nada"); não exige decisão de produto.

### 2. [Baixo] O cálculo do smoke da esquina não espelha a produção

- **Onde:** `src/IndexCities.Godot/Main.ConstructionCornerQA.cs:21-30` (`CornerQARemainingPath`) versus `Construction.Feeding.cs:102-118` (`MachineReservation`).
- **Detalhe:** a versão do smoke **não** trunca no primeiro corpo, **não** exclui causa de combustível/verba e ignora `SiteDockHeading`. É uma verificação **mais estrita** que a produção. Passa, mas pode falhar em geometrias onde a produção, de propósito, trunca a reserva num corpo já presente — falso negativo/positivo dependendo do layout.
- **Severidade:** **Baixo** (fidelidade de teste). Nada quebrado hoje.

### 3. [Baixo] Rótulo "versão antiga" também pega save corrompido; save ilegível some da lista

- **Onde:** `src/IndexCities.Godot/CitySaves.cs:41-48`.
- **Detalhe:** `ListAll` lista como `Outdated` **qualquer** save cujo `CitySimulation.FromJson` lance (inclusive corrupção/versão de schema não suportada) desde que `Name`/`Now` existam no envelope; e **não lista** save cujo envelope não parseie (`OutdatedSummary`→null). A SPEC (`docs/spec/mundo-tempo-saves.md:37`) diz que a cidade incompatível "continua listada no menu"; um arquivo ilegível some.
- **Cenário:** save corrompido mas com envelope válido → aparece como "versão antiga do jogo"; save com envelope inválido → não aparece.
- **Severidade:** **Baixo** (apresentação; não apaga nem altera nada).

### 4. [Baixo/informativo] Custo de alocação da reserva por veículo por passo

- **Onde:** `src/IndexCities.Simulation/Construction.Feeding.cs:87-88` (chamado por `Mobility.cs:218` para **todo** veículo **a cada** passo).
- **Detalhe:** quando há máquina no canteiro com trajeto pendente, cada `SafeSiteTravel` de veículo não-máquina materializa `MachineReservation` de cada máquina (lista de tuplas a cada 0,5 m) e achata num `ToList` — O(V·P) por veículo, O(V²·P) por passo, com alocação por passo. Fora da janela de pavimentação o caminho rápido de `:89` evita o custo. Medido de ponta a ponta: **0,43–0,88 ms/passo**; sem regressão perceptível. Fica registrado como possível pressão de GC em aceleração alta/muitos veículos, não como problema demonstrado.
- **Severidade:** **Baixo**.

## Refutação de hipóteses

- **"A reserva cria impasse com dois veículos/máquinas reservando trajetos que se cruzam":** refutado. Só veículos não-máquina constroem reserva (`Construction.Feeding.cs:87`) e máquinas só testam corpos (`:92`); veículos não reservam entre si. 900 cidades independentes + 1000 do autor → 0 travamentos. **Não procede.**
- **"Veículo já parado no trajeto causaria impasse":** a reserva **trunca no primeiro corpo** (`:112`), então quem já está no caminho pode sair; o autor também documenta esse limite residual (`report.md:116`). Não reproduzido em 1900 cidades. **Não procede como bloqueador** (fica como limite conhecido).
- **"A reserva permite atravessar corpos":** refutado por monitor de penetração física a cada passo (0 em 900 cidades). **Não procede.**
- **"Determinismo save/load quebrado":** refutado (26 seeds, 150 passos cada). **Não procede.**
- **Achado 2 da rodada 1 (soft-lock de save legado em `NoBudgetForPaving`):** **obsoleto** — a migração foi removida por decisão humana (`docs/decisoes/mundo-tempo-saves.md`, `docs/spec/materiais-logistica.md:54`) e saves antigos agora nem abrem.

## Limitações desta revisão

- Não reexecutei os smokes renderizados (janela/GPU) nem inspecionei capturas novas; validei simulação pura, build e leitura de evidências do autor.
- O achado 1 foi reproduzido por sonda que **força** o estado (máquina `OnSite` com `Fuel=0` + cancelamento); a cadeia é a mesma da causa real `WorkCause.PavingOutOfFuel`, mas o bloqueio de terceiros não foi observado organicamente nas varreduras.
- Não medi o custo da reserva isolado da lógica preexistente; os números são de ponta a ponta.
- A varredura própria usa seeds diferentes das do autor; cobre 900 cidades (300 lago + 600 em 6 formatos), além das 42 do teste `MachineExitTests`.

## Encaminhamento

- **Achado 1 (Médio):** tratar antes de "pronto" — excluir também a string de falta de manobra (ou padronizar o `WorkCause`) em `Construction.Feeding.cs:67/105`. É correção de comportamento já aprovado; não requer nova decisão de produto.
- **Achados 2–4 (Baixo):** não bloqueiam; ajustes opcionais de fidelidade de teste e apresentação.
- Demais pontos da faixa (cancelamento abre trechos compactados; remoção da conversão de saves; reserva sem novo impasse/sobreposição; determinismo) **verificados e conformes** à SPEC vigente.
