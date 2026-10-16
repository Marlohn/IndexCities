# Construction — caminhões e pavimentação

Arte 3D autoral do IndexCities, preparada em GPT-6.1-Sol com esforço xhigh (confirmado pelo terminal Orca). Estética final **PENDENTE de revisão humana**. Fonte editável em `Source/`; cenas e malhas são derivadas, sem scripts de runtime, física ou autoplay. Usa somente o kit e materiais do próprio projeto V1, sem modificar essa família. Não usa imagens como malhas nem recursos externos.

Contrato: metros, Y para cima, frente −Z, raiz no solo/centro. Carrocerias permanecem no retorno vazio. O host controla as cargas, quantidades e movimentos reais. Todas as cargas e coberturas vêm **ocultas**. Os nomes abaixo são estáveis; caminhos relativos à raiz de cada cena.

| Cena em `Scenes/` | Partes para o host |
| --- | --- |
| `vehicle.truck_dump.a.tscn` | `BedPivot/Bed`; `BedPivot/TailgatePivot/Tailgate`; `BedPivot/LoadSandGravel`; `BedPivot/LoadAsphalt`; `BedPivot/Cover`; `HydraulicBase`; `HydraulicRod`; `Wheel_FL/FR/ML/MR/RL/RR` |
| `vehicle.truck_mixer.a.tscn` | `DrumMount/Drum`; `ChutePivot/Chute`; `Wheel_FL/FR/ML/MR/RL/RR` |
| `vehicle.truck_flatbed.a.tscn` | `LoadSteel`; `LoadWood`; `Wheel_FL/FR/ML/MR/RL/RR` |
| `vehicle.asphalt_paver.a.tscn` | `HopperLeftPivot/Wing`; `HopperRightPivot/Wing`; `HopperLoad`; `ScreedPivot/Screed`; `TrackLeft`; `TrackRight` |
| `vehicle.road_roller.a.tscn` | `FrontSteering/FrontDrum`; `RearDrum`; `FrontSteering` |

Basculante: `BedPivot.rotation.x` positivo levanta a frente da caçamba; tampa traseira abre com X negativo. Cargas são filhas da caçamba. `Cover` usa lona escura com abertura traseira que permite reconhecer o asfalto escuro; não ligar em areia/brita. Retirar a lona antes de bascular. Cilindro hidráulico tem dois nós para o host ajustar entre os pontos `HydraulicAnchor` e `BedPivot/HydraulicTarget`.

Betoneira: `DrumMount` contém a inclinação fixa do eixo; girar `DrumMount/Drum.rotation.z` no eixo local do tambor. O tambor é carroceria, não carga visual e não desaparece quando vazio. `ChutePivot.rotation.y` direciona a calha; giro não significa entrega real por si.

Rodas: eixo X local, rolagem rumo −Z = ângulo negativo, raio 0,47 m. Dianteiras podem esterçar em Y. Pavimentadora: asas do funil giram em Z, mesa em X; em repouso `ScreedPivot.position.y = 0.50`, em trabalho `0.42` com rotação X zero (contato no solo). Esteiras são peças independentes, sem animação embutida. Rolo: cilindros giram em X (raio 0,5885 m); esterço dianteiro em Y. Poses de operação constam no manifesto e na ferramenta de validação, sem impor limites à simulação.

Quantidade: os pivôs de `BedPivot/LoadSandGravel` e `BedPivot/LoadAsphalt` ficam na base interna da caçamba (Y local 0,14); `LoadSteel`/`LoadWood` no piso Y=1,32 e `HopperLoad` na base Y=0,84. O host pode ajustar `scale.y` desses nós à quantidade real, sem escalar a carroceria. Esconder a carga quando vazia; no concreto o recipiente permanece fechado, sem massa de concreto visual fictícia.

`Candidate_*` são apenas referências visuais, não acessos funcionais. Não há trabalhador figurante. Materiais não emitem luz. Malhas agrupam detalhes por material, com recursos compartilhados entre rodas.

Aplicação dos princípios: a apresentação distingue materiais e execução sem cliques novos, espera extra ou estados econômicos adicionais; silhuetas dão feedback a quem constrói sem ler. SIMs e empresas só são afetados pelas regras já existentes no host. Cargas ocultas e ausência de autoplay evitam atividade fictícia e mantêm a causa no estado real. Classificação: apresentação de logística automática e da ação de construção já aprovada; nenhuma política ou configuração do jogador nova.

Base de produto: `docs/spec/materiais-logistica.md` (Caminhões reconhecíveis pelo material) e `docs/spec/construcao.md` (Pavimentação física das ruas), decisão humana de 2026-10-16. Esta documentação de arte não substitui a SPEC.

![Prancha na escala urbana atual](Preview/Captures/board_urban_loaded_iso.png)

Regeneração a partir da raiz do repositório (Godot 4.7.2 mono descoberta pelo `scripts/Play.ps1`):

```powershell
$engine = 'C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Construction/Source/build_construction.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Construction/Source/verify_construction.gd -- --validate-only
& $engine --path src/IndexCities.Godot --rendering-method gl_compatibility --resolution 2000x1200 --position 20,20 --script res://Assets/Construction/Source/verify_construction.gd
```

O terceiro comando exige janela real. Gera 12 capturas principais e um detalhe em `artifacts/verification/construction-models`, além de uma cena de prancha em `Preview/construction_board.tscn`. As cargas visíveis nessa prancha são ilustrações de autoria, não uma cidade simulada. A ferramenta usa câmera ortográfica na direção `(300,330,300)`, luz e ambiente lidos do host atual. Nenhum save é lido ou escrito. Cópias permanentes das capturas úteis, medições e relato ficam em `Preview/`.

Medição da geometria final (superfícies somadas por instância, inclusive as rodas; “todos” inclui cargas alternativas ocultas, que não devem aparecer simultaneamente):

| Família | Triângulos vazio / todos | Superfícies vazio / todos | MeshInstances totais | Envelope repouso X × Y × Z (m) |
| --- | ---: | ---: | ---: | --- |
| Basculante | 2.522 / 3.502 | 25 / 30 | 14 | 2,90 × 2,78 × 7,20 |
| Betoneira | 2.778 / 2.778 | 27 / 27 | 10 | 2,90 × 3,28 × 7,20 |
| Carroceria aberta | 2.374 / 3.898 | 23 / 29 | 10 | 2,90 × 2,78 × 7,20 |
| Pavimentadora | 1.358 / 1.372 | 16 / 17 | 7 | 2,54 × 2,84 × 4,62 |
| Rolo | 952 / 952 | 14 / 14 | 4 | 2,32 × 2,71 × 3,97 |

Cada malha tem até 7 superfícies. Rodas reutilizam um recurso; os dois cilindros do rolo compartilham outro. A medição do preview em GL Compatibility/RTX 3050 registrou 641 chamadas e 57.096 primitivas na prancha urbana inteira, e 57 chamadas/3.060 primitivas no detalhe da pavimentadora. São contadores globais de render com cenário, rótulos e sombras, não o custo isolado do asset nem benchmark de partida. Não foram medidos FPS, tempo de GPU, LOD ou frota numerosa.

Validação executada: build do host, importação real, lista de 33 cenários de QA, `INDEXCITIES_SMOKE_OK buildings=0 roads=17`, 546 verificações de recursos/contrato sem falhas e render real das pranchas em GL Compatibility. Logs detalhados em `artifacts/verification/construction-models`. A revisão independente da entrega integrada e a ligação ao estado real da simulação ficam com o coordenador; build Windows exportada e avaliação humana da estética continuam pendentes. Não declarar esta amostra como experiência integrada aprovada.
