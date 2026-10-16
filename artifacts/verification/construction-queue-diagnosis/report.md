# Diagnóstico e correção da fila da pavimentação (CornerCity) — 2026-10-16

**Escopo limitado:** corrige só o bloqueio mútuo rolo × caminhão do caso mínimo. Isto **não** declara a entrega global de pavimentação pronta: as duas lacunas humanas abaixo seguem fora deste trabalho.

- Modelo efetivo do autor: **Claude Opus 5.5 (`claude-opus-5-5`)**, padrão configurado pelo usuário; nenhuma troca de modelo ou configuração.
- Base: `b8a2462` (código `fcf6e89`, autor anterior GPT-6.1-sol xhigh). Worktree `pavimentacao-diagnostico`, branch `Marlohn/pavimentacao-diagnostico`.
- Correção técnica aprovada pelo coordenador antes de editar o produto (ASK bloqueante, resposta: "Aprovada tecnicamente a correção mínima para restaurar a ocupação física").
- Não alterados: SPEC, economia, frota, escopo, arquitetura e `Assets/Construction`.

## Causa confirmada

Uma **máquina bloqueada girava a carroceria no lugar sem verificar colisão**. A dependência do passo de tempo prova a causa: com o quantum real de 2 s (`Advance(7200)`), a obra trava. Com `Advance(1)` repetido, a mesma cidade conclui em t=522 (`trace-before-dt1.log`).

Sequência no passo t=512→514 (`trace-before-dt2.log`):

1. `MoveVehicles` roda antes de `ReturnPavingMachines`. O caminhão V22 (carroceria aberta, vazio, voltando ao exterior pelo acesso exclusivo de obras) desce a faixa x=-193,65 até (-193,65; 119,44). Fica lado a lado com o rolo V30, em (-196,4; 117,32) e heading π. A folga lateral é 2,75 m, acima dos 2,71 m exigidos (1,45 + 1,16 + 0,1), portanto sem sobreposição: o movimento é legítimo.
2. O rolo, em `LeavingSite`, chega a (-196,4; 116) e mira o próximo ponto de saída, (-193,65; 112), na diagonal de volta à faixa. `MoveConstructionSitePath` (`Construction.Feeding.cs`) gravava `Heading=2,54` **antes** de `SafeSiteTravel`. O avanço resultava em 0 por causa do caminhão, mas o heading girado ficava persistido. O giro aumenta a extensão lateral do rolo e cria uma sobreposição com o caminhão numa pose que nunca foi testada: `VehicleBodiesOverlap` = true nos dois sentidos.
3. A partir daí, todas as sondas de 0,5 m colidem: as do rolo nos primeiros 3 m e as do caminhão em `Mobility.cs`, que chama `SafeSiteTravel` com `machinesOnly` contra a máquina `Parked`. Resultado: bloqueio mútuo permanente. V30 fica em "Aguardando espaço ocupado no canteiro" e V22 em "Fila na faixa" desde t=514. A P26 fica com 49/49 pontos e `InProgress`, a rua fechada e a casa sem conclusão.

O mesmo padrão existia em `AdvancePavingMachine` (`Construction.Paving.cs`), usado na aplicação e na compactação.

## Correção (mínima)

Em `MoveConstructionSitePath` e `AdvancePavingMachine`, o heading anterior é guardado e restaurado quando `SafeSiteTravel` retorna 0. Um corpo bloqueado mantém a última pose verificada. No caso mínimo, o rolo continua alinhado, o caminhão passa, o rolo entra na faixa atrás dele e a P26 conclui em t=532.

Por que não muda contrato de produto: a correção não abre a rua mais cedo, não remove nem teleporta máquinas, não deixa veículos se atravessarem e não relaxa asserts nem prazos. Ela só aplica a regra existente "máquinas e veículos respeitam ocupação física" também à rotação. `Mobility.cs`, linha do `v.Heading=Math.Atan2(facing…)`, tem padrão análogo para veículos de via. Por orientação do coordenador, não foi generalizado sem evidência.

Arquivos:

- `src/IndexCities.Simulation/Construction.Feeding.cs`, `Construction.Paving.cs`: a correção.
- `tests/IndexCities.Simulation.Tests/PavingTests.cs`: teoria regressiva `Blocked_leaving_roller_keeps_its_pose_so_a_passing_truck_and_the_corner_street_finish`, com quantum de 2 s e de 1 s. Exige conclusão da rua e da casa, retorno das máquinas, invariantes e que uma máquina `LeavingSite` parada não mude de heading.
- `src/IndexCities.Godot/Main.ConstructionCornerQA.cs` (+ registro em `Main.QARegistry.cs`): fixture de QA isolada `--construction-corner-smoke`, sem gameplay novo, porque não havia cenário CornerCity renderizado.

## Comandos e resultados

| Verificação | Comando | Resultado |
|---|---|---|
| Reprodução antes | `dotnet run --project artifacts/verification/construction-paving-feeding/corner-probe/CornerProbe.csproj` | P26 `InProgress`, V30 parado em (-196,4; 116), V22 em (-193,65; 119,438) — confirmado |
| Rastreamento | `trace-probe` (`dotnet run -- 2` / `-- 1`) | `trace-before-dt2.log` trava e `trace-before-dt1.log` conclui. `trace-after-scratch-fix-dt2.log` foi feito em cópia isolada no scratchpad, antes da aprovação |
| Teste novo sem a correção | arquivos de produto revertidos temporariamente | quantum 2 s **falha**, 1 s passa (`regression-test-before-fix.log`) |
| Testes próximos | `--filter PavingTests\|PedestrianCrossingTests` | 16/16 aprovados |
| Regressão sem soak | `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj --no-build --nologo --filter 'FullyQualifiedName!~CityCycleTests'` | **219 aprovados, 0 falhas, 219 no total**, 49 s (antes: 214/3/217). Os 3 `PedestrianCrossingTests` passam sem alterar asserts (`regression-tests.log`) |
| Probe original depois | mesmo comando de reprodução | P26 e P27 (casa) `Completed`; paver/rolo/caminhão no exterior (`corner-probe-after-fix.log`, `corner-after-fix.json`) |
| Build host | `dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj` | 0 erros; segue o aviso preexistente CS8629 em `Main.ProblemAlertsQA.cs:198` (`build.log`) |
| QA list | `godot --headless --path src/IndexCities.Godot -- --qa-list` | 35 cenários, inclui `--construction-corner-smoke` (`qa-list.log`) |
| Godot renderizado, esquina | `& 'C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe' --path src/IndexCities.Godot --resolution 1440x960 --position 80,60 -- --construction-corner-smoke '--review-save-dir=C:/Users/Marlohn/orca/workspaces/IndexCities/pavimentacao-diagnostico/artifacts/verification/construction-queue-diagnosis/godot-corner'` | `INDEXCITIES_CONSTRUCTION_CORNER_OK models=final captures=4 … street=completed house=completed roller=yielded-without-turning` (`engine-corner.log`) |
| Godot renderizado, pavimentação | mesmo executável e argumentos de engine, `-- --construction-paving-smoke '--review-save-dir=…/construction-queue-diagnosis/godot-paving'` | `INDEXCITIES_CONSTRUCTION_PAVING_OK models=final captures=27 … straight=L=physical money=conserved save-load=deterministic cancel=physical-return` (`engine-paving.log`) |

Ambiente: OpenGL 3.3 Compatibility, NVIDIA GeForce RTX 3050 6GB Laptop GPU. Antes de rodar foi necessária a importação (`--headless --editor --import --quit`, `import.log`). Os `.import` alterados por ela foram revertidos e não estão no commit. Saves somente em `godot-*/cities` nesta worktree; nenhum save pessoal foi usado.

O teste de duas equipes e os de save/load continuam aprovados: estão nos 219, e o smoke de pavimentação compara o JSON após save/load e após cancelamento.

Capturas da esquina, todas com JSON de estado ao lado:

- `corner-ordered`: obra encomendada.
- `corner-roller-yields`: o rolo saindo, ao lado do caminhão, mantém a pose.
- `corner-truck-passed`: o rolo já está na faixa, atrás do caminhão.
- `corner-completed`: rua em L aberta e casa pronta.

O rodapé das capturas mostra `b8a2462b` com "alterações locais", que são exatamente as deste commit. Do smoke de pavimentação, o commit leva `evidence.txt` e três capturas representativas. As 27 ficam fora para não inflar o repositório; o marcador `OK` está no log.

## Limites

- A folga lateral simulada entre o caminhão na faixa e o rolo na baia de saída é de só ~4 cm (2,75 m contra 2,71 m), um traçado preexistente de `PavingExitRoute`. Na captura, os modelos parecem encostados na projeção isométrica. Não alterei o traçado: isso mudaria a geometria de saída já aprovada no checkpoint e não é necessário para eliminar o bloqueio.
- A Prefeitura (P24) desse caso segue "Sem acesso para materiais/equipe", porque a rua não alcança o lote dela. É uma obra sem rua conforme a SPEC, não regressão, e o caso mínimo só exige rua e casa.
- `CityCycleTests` (soak) não foi executado.
- Não houve revisão independente nesta etapa. A revisão por modelo diferente (DeepSeek V4.1 Flash ou Claude Haiku 5.5) cabe ao coordenador. Também não houve validação humana.
- O smoke e a fixture provam a geometria testada, não a ausência de bloqueio em qualquer bairro.

## Lacunas humanas (não implementadas aqui)

1. **Destino dos trechos já compactados ao cancelar uma rua:** abrir, manter fechados para obras ou remover. Segue aberto na SPEC. O comportamento do checkpoint não foi tocado.
2. **Frota legada de saves com 6 cargas do mesmo material:** o coordenador informou que o usuário aprovou um ajuste único depois de concluir a viagem e retornar ao exterior, e está registrando isso na SPEC. Por instrução dele, **não foi implementado** neste trabalho.
