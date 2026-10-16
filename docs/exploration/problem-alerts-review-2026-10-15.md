# Revisão independente — alertas de problemas nos prédios (2026-10-15)

Revisão humana: **PENDENTE** — não é fonte de verdade.

Revisor: **DeepSeek V4.1 Flash (opencode)**, em contexto independente (não escreveu este código).
Branch revisada: `Marlohn/alertas-problemas-predios` (commits `88496ff`..`4742b15` sobre `origin/main` @ `b0a30dc`).
Contrato: entrada *Alertas de problemas sobre os prédios — decisão do responsável em 2026-10-15* (`docs/SPEC.md:87`) e a revisão humana de IA-017 (`docs/SPEC.md:131`).
Relato do autor conferido: `docs/exploration/problem-alerts-playtest-2026-10-15.md`.

## Método

Leitura do diff completo contra `origin/main`, leitura cruzada da SPEC, da simulação (`Infrastructure.cs:54-60`, `Mobility.cs`, `Maintenance.cs`) e do código de apresentação; execução real dos cenários no Godot 4.7.2 mono; comparação em worktree limpo de `origin/main` para as falhas pré-existentes. Nada foi corrigido no código.

## Comandos e resultados

| Verificação | Comando | Resultado |
| --- | --- | --- |
| Build + testes | `scripts/Build.ps1` | OK; **184 testes aprovados, 0 falhas** |
| QA dos alertas | `--problem-alerts-smoke` (engine args antes de `--`; `--review-save-dir` isolado) | **OK**, exit 0, todos os marcadores `PROBLEM_ALERTS_QA` presentes (`smoke-60dc3a94…`) |
| Playtest | `--problem-alerts-playtest` | **OK**, exit 0, `PROBLEM_ALERTS_PLAYTEST_OK waterAlertsAppearedAndCleared buildings=16 population=17` (`playtest-54cdd0de…`) |
| Balão de carga (regressão) | `--activity-indicators-smoke` com save real existente | **OK** (`cargo-indicator=(1, True)` com caminhão real; ninguém fabricado) |
| `--recovery-smoke` em `origin/main` vs branch | worktree temporário (sem stash) | falha **igual** nas duas árvores nesta máquina ("fundação pela GUI"; alegação do autor de que já falhava antes confirmada) |
| `--ui-smoke` em `origin/main` vs branch | idem | falha **igual** ("Segundo clique encomenda obra viária real e prepara próxima rua") |

## Aderência à SPEC (os 8 alertas)

Todos derivam do mesmo estado que a simulação usa em `b.Cause` (`Infrastructure.cs:58`), sem cálculo novo por SIM e sem tocar regras:

| Alerta | Predicado (`CityWorld.ProblemAlerts.cs`) | Estado na simulação | Confere? |
| --- | --- | --- | --- |
| `road` | `!HasExteriorAccess(b)` (linha 77) | causa "Sem acesso viário" | sim |
| `water` | demanda>0 e `WaterRatio<.5` (78) | mesmo limiar da causa de água | sim |
| `power` | demanda>0 e `ElectricityRatio<.5`, exceto eólica (79) | causa usa `<.1` em não-casas; calibração registrada | sim (ver MENOR-3) |
| `intake` | captação sem fonte natural (82) | mesma regra de distância à margem | sim |
| `noraw` | tratamento com `RawWaterCapacity<=0` (85) | causa "Falta captação de água bruta" | sim |
| `staff` | não-casa, aberto/utilitário, não encerrada, sem empresa pendente, 0 presentes (91-94) | mesmas condições da causa | sim (ver IMPORTANTE-1) |
| `stock` | loja/posto aberto com empresa e produto `<.1` (97-99) | causa "Estoque insuficiente" | sim |
| `maintenance` | municipal com `MaintenanceRatio<.9` (102) | causa "Manutenção pendente" | sim |

- Estados informativos não alertam: fora de horário (equipe/estoque), "Procurando empresa", "Operação encerrada" e casas sem equipe estão excluídos por código e verificados no QA (noite às 23h silencia Prefeitura/mercearia; utilitários mantêm equipe, coerente com os turnos 24h de `Mobility.cs:11`).
- Histerese: água/energia/bruto 20 s, estoque 60 s, equipe 300 s, com recuperação 5× mais rápida e teto no tempo acumulado (linhas 124-131) — evita piscar na venda/reposição e na troca de turno; o QA comprova a ordem de persistência (60 s mostra `noraw`/`stock` e ainda não `staff`; 300 s mostra `staff`) e a limpeza com mercadoria real.
- Inspeção bate com o ícone: `Main.UI.cs:79` mostra "Problemas: …" derivado da mesma lista desenhada (`BuildingProblemText`, linhas 170-174); QA compara texto exato com o ícone.
- **Tooltip de hover: não existe** (achado BLOQUEANTE-1 abaixo).

## Achados

### BLOQUEANTE-1 — A SPEC exige explicação curta "ao passar o mouse"; só há explicação no clique

- Onde: `src/IndexCities.Godot/CityWorld.ProblemAlerts.cs:137-160` (ícones são `Sprite3D`, sem hover/tooltip); única explicação textual em `src/IndexCities.Godot/Main.UI.cs:79` (inspeção, aberta por clique). Requisito em `docs/SPEC.md:87`: "…e ter explicação curta ao passar o mouse".
- Cenário de falha: o jogador passa o mouse sobre o ícone de problema e nada acontece; a explicação só aparece clicando no prédio. O autor reconhece a lacuna no próprio relato (herdada do Step 5).
- Sugestão: implementar hover 3D real sobre o tile (raycast/pick já existe para prédios e pode alimentar um `TooltipPanel`) **ou** obter decisão humana explícita aceitando a inspeção no clique como canal substituto e atualizar a SPEC. Sem isso, a entrega não atende literalmente o contrato aprovado.

### IMPORTANTE-1 — "Sem funcionários" acusa fim de expediente normal (e equipe do Pátio em obra)

- Onde: `src/IndexCities.Godot/CityWorld.ProblemAlerts.cs:91-94` combina `BuildingOpen()` (`Mobility.cs:14`, não-casas abertas das 6h às 19h) com `WorkersPresent()==0`, mas a escala real de trabalho é 7h-15h (`Mobility.cs:6-13`) e o Pátio manda a equipe para canteiros (`WorkerPresentAt` exige `ConstructionProjectId==null`, `Mobility.cs:18`).
- Cenário de falha (evidência real do playtest, `playtest-54cdd0de…/log.txt`): a Prefeitura (prédio 25) mostra `staff` às 15:00-15:10 e volta a `clean` às 19:00 — ou seja, alerta das ~15h05 às 19h **todos os dias**, só porque o expediente acabou; e também por volta das 6h antes do turno começar (`25:staff` às 06:55, `clean` às 07:00). O mesmo vale para Fazenda e Pátio de Obras (turno único 7-15, "abertos" até 19h), inclusive com a equipe do Pátio fisicamente trabalhando em canteiros.
- Consequência: é o falso-positivo que a SPEC manda evitar ("estados normais ou informativos… não viram alerta"); como a `Cause` da simulação tem a mesma semântica, o alerta apenas expõe a divergência entre "aberto" e "escalado".
- Sugestão: definir "em horário de funcionamento" do alerta como **cobertura real de turno** (existe trabalhador escalado com turno ativo agora) em vez de `Open()`, ou levar a decisão ao responsável; se mantido o comportamento atual, registrar a aceitação explícita. Vale também considerar excluir/atenuar equipe em Pátio enquanto `LocalCrew` estiver em obra.

### MENOR-1 — Desempenho: varredura O(prédios × cidadãos) a cada 0,5 s simulados

- Onde: `CityWorld.ProblemAlerts.cs:107-136` roda em cada atualização (a 60x ≈ 120×/s reais), com `new List<string>` por prédio (linha 116) e `WorkersPresent` O(cidadãos) para cada prédio elegível (`Mobility.cs:19`).
- Não há sinais de problema na escala da V1 (o QA passou a 60x com 16 prédios; a varredura só roda com relógio simulado avançando e é gated a 0,5 s), mas o custo cresce quadrático com cidade grande.
- Sugestão: cachear `WorkersPresent` por passe/por mudança de presença e reutilizar a lista de mostrados; medir com `--performance-smoke` em cidade cheia antes de otimizar.

### MENOR-2 — QA: marcador do playtest imprime o texto de interpolação literal

- Onde: `src/IndexCities.Godot/Main.ProblemAlertsQA.cs:441` — string concatenada com `"{_sim.State.Now:dd/MM HH:mm}"` literal; o stdout mostra `time={_sim.State.Now:dd/MM HH:mm}` em vez do horário (o `log.txt` da linha 440 está correto; meus marcadores reproduzem o defeito).
- Sugestão: interpolar de verdade (ou remover o campo do `GD.Print`).

### MENOR-3 — Barra de catálogo parcial no playtest é artefato determinístico do QA, não regressão

- Onde: `src/IndexCities.Godot/Main.ProblemAlertsQA.cs:404-407`: `BuildCatalog()` é chamado antes de a Prefeitura ser colocada (por simulação direta, sem o `Confirm()` do jogo) e nunca é reconstruído; por isso as capturas conferidas do playtest (`playtest-start`, `playtest-healthy`, `playtest-water-shortage`, `playtest-inspection`) mostram só "Selecionar/Prefeitura". O relato do autor supôs artefato transitório de captura — na verdade é determinístico e sem efeito nas asserções.
- Sugestão: chamar `BuildCatalog()` após fundar a Prefeitura no cenário, para o estado de UI bater com o de um jogador real.

### MENOR-4 — Decisões de apresentação registradas como pendentes fora da seção de delegação da SPEC

- Supressão do alerta de energia em usinas eólicas (`CityWorld.ProblemAlerts.cs:79`; coerente com a causa que também exclui eólicas) e limiares (energia 0,5 no alerta vs 0,1 na causa) estão listados só no relatório exploratório (`problem-alerts-playtest-2026-10-15.md:192-193` e 403-405), não na seção de decisões IA pendentes da SPEC. Pela regra do AGENTS, decisão de produto visível da IA deveria estar registrada lá antes do código; como são calibração/localizáveis e já estão sinalizadas, sugiro apenas encaminhar a revisão humana (confirmar, ajustar ou mover para a SPEC).

### MENOR-5 — Causa "Parada após falta persistente de água" não tem alerta dedicado

- Onde: `Infrastructure.cs:58` (causa após `WaterClosureHours`); durante a escassez o alerta de água cobre o caso, mas após a água voltar o prédio pode seguir parado por um curto período sem ícone.
- Observação para a regra geral da SPEC; não vi falso alerta nem risco prático (transitório e decrescente). Sem sugestão de ação imediata.

## Regressões e integrações

- Balão de carga/descarga continua e foi verificado com caminhão real (`CityWorld.Activity.cs:19-29,68-72`; `--activity-indicators-smoke` OK); a contagem de pessoas foi removida como decidido no diff (`Main.PresentationQA.cs` ajustado; `--presentation-smoke` foi executado pelo autor com OK e não repetido nesta revisão).
- A simulação ganhou **apenas um acessor de leitura** (`BuildingOpen`, `Mobility.cs:15-17`); nenhuma regra, fórmula ou transição mudou; 184 testes da simulação passam e o QA verifica que a apresentação não altera o estado salvo.
- Persistência/saves: nenhum campo novo é serializado; um save antigo carregou sem problema no QA de indicadores. Não há impacto em autosave nem em cidades existentes.
- Remoção do glifo `◉`/`ϟ` abaixo de 90% de água/energia: mudança de apresentação fora da letra da entrada da SPEC, porém coerente com "não ter dois critérios concorrentes" e registrada no relatório; recomendo confirmação humana junto da revisão desta entrega.

## Qualidade

- Adicionar nova causa é realmente trivial e está documentado no topo do arquivo: linha na tabela `ProblemAlertDefs` (linha 25), predicado em `ProblemAlertActive` (72) e um SVG em `Assets/UI`. A tela desenha N ícones lado a lado com pool de nós reaproveitados, sem criar/destruir por quadro.
- O QA não é cerimonial: cenário controlado com estado forçado e reaplicado, corte real de rua e reconexão, cadeia real de água ponta a ponta, controle negativo (captação úmida), ordem de persistência, supressão noturna, limpeza por mercadoria real e comparação exata inspeção × ícone. Os limites (transição 0→presente sem equipe real, combinações de 3+ ícones) estão declarados honestamente no relato.

## Limites desta revisão

- Executada nesta máquina/obra; os smokes de GUI (`--recovery-smoke`, `--ui-smoke`) são instáveis por natureza de interação, e nesta máquina o ponto de parada do `--recovery-smoke` diferiu do citado pelo autor ("fundação pela GUI"), mas falha **igualmente** em `origin/main` e na branch — não é regressão da feature.
- Não repeti os cenários `--circulation-smoke`/`--finances-smoke` (dependem de cidade-checkpoint ausente) nem medições formais de desempenho em cidade cheia.
- O worktree temporário de `origin/main` foi removido ao final; nenhum commit de código foi feito.
