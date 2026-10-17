# Espera circular entre máquina saindo do canteiro e veículos da faixa (2026-10-16)

- Modelo efetivo do autor: **Claude Opus 5.5 (`claude-opus-5-5`)**, sem troca de modelo.
- Base: `9b84bc6` na branch `Marlohn/pavimentacao-integracao`. Commits deste trabalho: `7f1efb7` (simulação), `f8ec946` (testes), `f624404` (smoke da esquina) e o commit deste relatório.
- Não alterados: SPEC, economia, frota, traçado de saída (`PavingExitRoute`), prazos, fixtures de simulação e assets.
- A mudança de asserção do smoke da esquina foi aprovada pelo coordenador antes de ser feita (ASK, opção A). Detalhes abaixo.

## Causa comprovada

Rastreamento passo a passo da seed 2877 do `--lake-recovery-smoke` (`trace-2877-before.log`, quantum de 2 s):

1. O rolo V31 sai do canteiro descendo a faixa x=106,35 com heading π. O basculante V18 volta ao exterior pela mesma faixa, atrás dele.
2. Entre t=1008 e t=1010, `MoveVehicles` roda antes de `ReturnPavingMachines`. O V18 avança 12,5 m e encosta no rolo até a margem de 0,1 m: (106,35; 6,5), frente em z=2,9. Até ali ele só testava o **corpo atual** do rolo (`SafeSiteTravel(machinesOnly)` contra a máquina `Parked`).
3. Em seguida, o rolo chega à esquina (104; 1,65) e precisa girar para −π/2 para seguir a oeste. Nessa pose, o corpo dele vai até z=2,81 e fica a 0,09 m da frente do V18, abaixo da margem de 0,1 m. Todas as sondas de 0,5 m colidem até ~1,5 m de avanço. O rolo fica "Aguardando espaço ocupado no canteiro" com o heading anterior restaurado (correção 94555b8). O V18 segue bloqueado pelo corpo do rolo.
4. Com o rolo parado na diagonal (heading −0,96), o corpo dele invade a faixa leste z=−1,65, e V19/V21/V22 param atrás dele. A P26 fica 105/105 `InProgress` para sempre e P27/P28 não começam.

Causa estrutural: os veículos respeitam só o corpo **atual** da máquina, não as poses que ela ainda vai ocupar. Assim um veículo pode parar exatamente onde a máquina precisa passar em seguida (atrás de uma curva, numa esquina, no cruzamento de volta da faixa de fachada), e cada um espera o outro.

A varredura sem a correção (`sweep-before-lake300.log`, `sweep-before.log`) mostrou três geometrias da mesma família, mais uma vizinha:

| Geometria | Exemplo | Mecanismo |
|---|---|---|
| Seguidor atrás da curva | seed 2877 | caminhão encosta atrás; o giro do rolo na esquina toca nele |
| Contramão na transição de fachada | seed 408 (V22 parado em x=−178,5 desde t=948) | o rolo volta da faixa de fachada (z=−4,4) cruzando a faixa leste, e o caminhão que vem de frente para encostado na diagonal |
| Esquina com curvas opostas | seed 593 | o caminhão vira à esquerda e o rolo à direita no mesmo canto; cada corpo fica no varrido do outro |
| Veículo girando no lugar para dentro da máquina | seed 1 (sobreposição física de 0,84 m) | ao sair da doca, o basculante chega à faixa e grava o heading da curva mesmo bloqueado, como o rolo fazia antes de 94555b8 |

## Solução

1. **A máquina em movimento no canteiro reserva o trajeto restante** (`MachineReservation` em `Construction.Feeding.cs`):
   - Vale para pavimentadora e rolo `Parked` em `OnSite` ou `LeavingSite` com `SitePath` pendente.
   - As poses reservadas são o giro no lugar em direção ao próximo ponto e amostras de 0,5 m ao longo de todo o restante do caminho.
   - A reserva **termina antes do primeiro corpo que já ocupa o trajeto**. Quem já está no caminho pode sair dele, e o espaço depois de um bloqueio real (outra máquina parada, fila) não fica preso.
   - Máquina parada por falta de combustível ou verba não reserva nada.
   - `SafeSiteTravel` de qualquer veículo que não seja máquina recusa sonda que entre numa pose reservada. Isso vale para veículos de via (`machinesOnly`) e para caminhões em caminho de canteiro.
2. **Um veículo de via bloqueado não gira no lugar para dentro do corpo de uma máquina** (`Mobility.cs`): se o passo deu 0 e o novo heading sobrepõe uma máquina `Parked`, ele mantém o heading anterior. É a mesma regra de 94555b8, agora aplicada ao veículo. O relatório anterior apontou esse padrão análogo sem generalizar. Aqui há evidência: sem essa parte, a seed 1 volta a ter sobreposição de 0,84 m (`sweep-without-pose-rule.log`).

Por que é geral: nenhum veículo novo entra no espaço que a máquina ainda vai percorrer. Então a máquina nunca fica bloqueada por alguém que chegou depois dela, e quem a espera fica fora do trajeto, sem segurar o avanço dela. A ordem deixa de depender de quem se moveu primeiro dentro do passo. Nada é teleportado, removido ou atravessado, nenhuma rua abre antes e não há veículo, dinheiro nem material novo. A ocupação física fica mais estrita.

Efeito visível: o veículo que chega depois de uma máquina em saída espera fora do trajeto dela e segue atrás. Na CornerCity seed 42, antes o rolo cedia ao caminhão. Agora o caminhão espera em "Fila na faixa" (t=512–514), o rolo entra primeiro na faixa x=−193,65 e o caminhão segue atrás. A SPEC não define quem cede, só ocupação física e conclusão real. O coordenador aprovou trocar a asserção de ordem do `--construction-corner-smoke` (opção A).

## Alternativas descartadas (com evidência)

| Tentativa | Resultado | Por que descartada |
|---|---|---|
| Reserva limitada a um passo (14 m = `VehicleSpeed`×quantum), truncada no primeiro corpo | lago 3/300 travando (seeds 593, 4404, 6069) | na chegada simultânea à esquina, o caminhão entra antes que a reserva alcance o canto |
| Reserva de 3 passos (42 m) | lago **30/300** travando (seeds 10028, 10583, …) | quem vem de frente para na borda da reserva, que fica sobre o caminho futuro da máquina. Toda reserva de alcance fixo tem essa borda |
| Trajeto inteiro, sem truncar, com isenção para quem já está nele | lago 0/300; duas equipes **40/40** travando | um rolo bloqueado por outra máquina parada (V36) segura o trajeto inteiro e impede o basculante de levar o asfalto de que essa máquina depende (ciclo por dependência de obra) |
| Regra "mover para longe de um contato é permitido" | não implementada | trataria o sintoma: a sobreposição de partida vinha de um giro não verificado, que foi corrigido na origem |
| Mudar o traçado de saída (curvas na faixa certa) | não implementada | mudaria a geometria de saída aprovada e o trajeto dos veículos de via |

A solução final é o trajeto inteiro truncado no primeiro corpo: 0/300 no lago e 0/40 com duas equipes.

## Evidências

| Verificação | Comando / arquivo | Resultado |
|---|---|---|
| Reprodução antes | `probe`: `dotnet run -c Release -- 2877` com `src` em `9b84bc6` | P26 105/105 `InProgress`, V31 parado em (104; 1,65), V18/V19/V21/V22 "Fila na faixa" (`trace-2877-before.log`) |
| Depois | mesmo comando no HEAD | `ALL COMPLETED t=1504` (`trace-2877-after.log`) |
| Varredura antes | `probe sweep 100 lake straight L corner T twoteams busy` | **16/700** falhas, todas no lago: 6 sobreposições físicas e 10 travamentos (`sweep-before.log`). Lago com 300 seeds: **28/300** (`sweep-before-lake300.log`, monitor anterior: penetração >0,05 m e sem carros de passeio) |
| Varredura depois | mesmo comando | **0/700** (`sweep-after.log`). Lago com 300 seeds: **0/300** (`sweep-after-lake300.log`). Monitor estrito: penetração física >0 m entre máquina e qualquer veículo, e qualquer veículo `Travelling` (inclusive carros) ou máquina saindo parado por mais de 900 s |
| Sem a regra de pose do veículo | `Mobility.cs` de `9b84bc6`, lago com 100 seeds | seed 1 com sobreposição de 0,84 m (`sweep-without-pose-rule.log`) |
| Testes novos antes da correção | `dotnet test … --filter MachineExitTests` com `src` em `9b84bc6` | **2 falhas** (regressão 2877 e varredura do lago), 6 aprovados (`regression-tests-before-fix.log`) |
| Suíte completa | `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj` | **230/230**, 1 min 29 s, incluindo `CityCycleTests` (`full-tests.log`) |
| Custo | `--filter CityCycleTests\|PavingTests` antes e depois | 1 min 11 s antes, 1 min 2 s depois (dentro do ruído; sem cache) |
| Build do host | `dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj` | 0 erros (`build.log`) |
| QA list | `godot --headless --path src/IndexCities.Godot -- --qa-list` | `qa-list.log` |

Testes novos em `tests/IndexCities.Simulation.Tests/MachineExitTests.cs`:

- `A_truck_following_the_leaving_roller_waits_clear_of_its_turn_and_the_lake_streets_finish`: seed 2877 com o layout exato do smoke do lago. Exige obras concluídas, máquinas no exterior, prédios com acesso, invariantes, nenhum veículo parado indefinidamente e nenhuma sobreposição física.
- `Leaving_machines_and_street_traffic_never_wait_on_each_other_forever`: teoria com 7 formatos e 42 cidades. São 24 seeds do lago (todas as geometrias que falhavam antes) e 3 seeds de cada um destes: reta, L, T com casa, esquina CornerCity + tronco, duas equipes simultâneas e várias casas com três ruas. Os mesmos critérios por passo. Os 8 casos levam ~11 s.

Godot renderizado (`C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe --path src/IndexCities.Godot --resolution 1440x960 --position 80,60 -- --<cenário> --review-save-dir=<abs>/artifacts/verification/construction-exit-deadlock/godot-<cenário>`), executados de novo depois do último commit de código:

| Cenário | Marcador |
|---|---|
| `--lake-recovery-smoke` | `INDEXCITIES_LAKE_RECOVERY_OK` (seeds 42/1412/2877; a 2877 travava antes). O cenário grava em `water-integrated/rendered-01e010d4…`; as capturas da 2877 foram copiadas para `godot-lake-recovery/` |
| `--construction-paving-smoke` | `INDEXCITIES_CONSTRUCTION_PAVING_OK models=final captures=27 … straight=L=physical money=conserved save-load=deterministic cancel=physical-return compacted=opened` |
| `--construction-corner-smoke` | `INDEXCITIES_CONSTRUCTION_CORNER_OK models=final captures=4 … street=completed house=completed roller=entered-first-truck-followed overlap=none turn-in-place=none` |
| `--neighbourhood-smoke` | `INDEXCITIES_NEIGHBOURHOOD_OK saved=isolated shared-points=unique …` (grava em `road-reference/rendered-b3bdd14f…`) |
| `--persistence-smoke` | `INDEXCITIES_PERSISTENCE_OK cities=2 outdated=warned … continuation=identical` |

Ambiente: NVIDIA GeForce RTX 3050 6GB Laptop GPU. Saves só nos diretórios de verificação; nenhum save pessoal foi usado.

### Capturas inspecionadas

- `godot-construction-corner/corner-truck-yields.png`: rolo já na faixa, à frente, e caminhão parado atrás com folga visível.
- `godot-construction-corner/corner-roller-passed.png`: caminhão seguindo o rolo na mesma faixa.
- `godot-construction-corner/corner-completed.png`: rua em L e casa prontas.
- `godot-lake-recovery/road-supported-2877.png`: rua da margem norte concluída na seed que travava.
- `godot-construction-paving/L-rolling.png` e `L-completed.png`: acabamento e rua em L concluída.

O rodapé das capturas mostra `f8ec946b · alterações locais`. É o build feito antes do commit `f624404`, cujo único conteúdo é o próprio smoke e o registro. Os JSON de estado da esquina estão ao lado das capturas.

### Mudança no smoke da esquina (aprovada, opção A)

A asserção antiga, "Rolo cedeu ao caminhão e seguiu atrás dele", fixava a ordem gerada pela correção anterior. Agora o smoke exige, **a cada passo**:

- nenhuma sobreposição física entre máquina e veículo dentro do mapa (o portal exterior é a origem de preparação compartilhada e fica fora da comparação);
- nenhum giro no lugar da máquina parada;
- todo veículo em "Fila na faixa" perto do rolo em saída está fora do trajeto restante dele.

Exige também que o caminhão esperou e depois avançou mais de 4 m seguindo o rolo na faixa, que rua e casa concluíram e que as máquinas voltaram ao exterior. A descrição no `Main.QARegistry.cs` e o marcador foram atualizados.

## Probe

O código fica em `probe/` (`ExitProbe.csproj`, `Program.cs`, `Sweep.cs`, `Trace.cs`) e é ferramenta de diagnóstico, não produto:

- `dotnet run -c Release -- <seed> [de] [até]` rastreia o layout do lago;
- `sweep <n> <formatos…>` roda a varredura;
- `trace <formato> <seed> <de> <até>` rastreia qualquer formato.

## Limites

- A reserva termina no primeiro corpo do trajeto. Em tese, um veículo que já estava no caminho quando a máquina começou a se mover, ficou parado lá por outro motivo e depois precisa avançar **em direção** à máquina ainda pode formar um impasse. Nenhuma cidade das varreduras finais (700 em 7 formatos e 300 do lago) produziu esse caso.
- Veículos de via continuam sem testar corpos uns dos outros (só a lógica de líder). Este trabalho tratou apenas a interação máquina × veículo.
- A folga lateral de ~4 cm entre caminhão e rolo na baia de saída da CornerCity, registrada antes, não mudou.
- Não houve revisão independente nesta etapa. A revisão por modelo diferente (DeepSeek V4.1 Flash ou Claude Haiku 5.5), conforme AGENTS.md §7, cabe ao coordenador. Também não houve validação humana.
