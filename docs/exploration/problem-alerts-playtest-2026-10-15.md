# Alertas de problemas nos prédios — implementação e playtest de 2026-10-15

Revisão humana: **PENDENTE — não é fonte de verdade.**

Documento de trabalho da IA: registra o que foi implementado, os comandos efetivamente executados, os
resultados observados e o feedback honesto de jogabilidade. A decisão de produto que autoriza esta
feature está na [SPEC](../SPEC.md), na seção *Alertas de problemas sobre os prédios — decisão do
responsável em 2026-10-15* (e na revisão humana de IA-017, que rejeitou o balão de quantidade de
pessoas). Nada aqui cria, altera ou amplia requisito; produto vigente continua sendo a SPEC.

## Contexto

- Branch de trabalho: `Marlohn/alertas-problemas-predios`.
- Escopo: remover o balão de quantidade de pessoas (IA-017) e substituí-lo por alertas de problema
  sobre prédios concluídos, derivados do mesmo estado que a simulação já usa para essas causas.
- A indicação de carga/descarga (caminhão real em operação) continuou existindo, como permitido.
- `src/IndexCities.Simulation` **não foi alterado**: os alertas apenas leem `HasExteriorAccess`,
  `WaterDemand`, `WaterRatio`, `ElectricityDemand` e `ElectricityRatio` já existentes.

## O que foi implementado

Apresentação apenas, na camada `IndexCities.Godot`:

- `Assets/UI/problem_alert_tile.svg`, `problem_road.svg`, `problem_water.svg`, `problem_energy.svg`:
  pictogramas planos (estrada, gota, raio) sobre um tile escuro com contorno vermelho de alerta,
  desenhados com a mesma técnica do balão anterior (sprites billboard em `Sprite3D`, `NoDepthTest`,
  tamanho constante na tela até o zoom 100, como o indicador já existente).
- `CityWorld.ProblemAlerts.cs` (novo):
  - âncora por prédio concluído, acima da entrada (altura calibrada em relação ao topo do modelo);
  - ícones criados sob demanda e reaproveitados (prédio sem problema não ganha nenhum nó);
  - atualização em ritmo moderado (no máximo a cada 0,5 s simulado) e só para prédios concluídos;
  - acesso viário usa `CitySimulation.HasExteriorAccess` — o mesmo teste da causa "Sem acesso viário";
  - água e energia usam demanda > 0 e razão recebida < 0,5 (mesmo limiar das causas na simulação);
    usinas eólicas não alertam por energia (decisão do operador para esta feature, tratada como
    calibração dentro da decisão aprovada — **pendente de confirmação humana**);
  - histerese assimétrica: a falta precisa persistir 20 s simulados para alertar, e o ícone some em
    poucos segundos depois que o problema acaba (tempo acumulado é limitado, para que uma queda
    longa não atrase o sinal de recuperação);
  - leitura read-only para QA e inspeção: `ProblemAlertIcons(id)` e `BuildingProblemText(id)`.
- `CityWorld.Activity.cs`: removida a parte de pessoas do balão (painel, ícone e contagem, além da
  contagem de interiores por quadro); mantida apenas a indicação de carga/descarga real.
- `CityWorld.cs`: `Sync` passou a aceitar a simulação (opcional) para alimentar os alertas; removido o
  glifo `◉`/`ϟ` que marcava água/energia abaixo de 90% em prédios concluídos (substituído pelos
  alertas, para não haver dois critérios concorrendo nem alerta sem problema real).
- `Main.UI.cs`: a inspeção do prédio passou a mostrar a linha "Problemas: …" com exatamente os mesmo
  problemas dos ícones (some quando não há problema).
- `Main.ProblemAlertsQA.cs` (novo): cenário controlado + cenário real + playtest roteirizado.
- `Main.ActivityIndicatorsQA.cs`: adaptado (não confunde mais ausência de balão de pessoas com falha) e
  `Main.PresentationQA.cs` ajustado para a remoção do balão.

## Comandos executados (Godot 4.7.2 mono, Windows)

Engine: `C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64.exe`. Como em toda execução de
QA, argumentos da engine vêm antes de `--` e argumentos do jogo depois; saves sempre em pasta isolada
sob `artifacts/verification`.

1. Build + testes da simulação
   `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1`
   → build OK; **184 testes aprovados, 0 falhas**.
2. Importação dos recursos novos (SVGs) no projeto Godot
   `Godot… --headless --path src/IndexCities.Godot --editor --import --quit`
3. QA dos alertas
   `Godot… --path src/IndexCities.Godot -- --problem-alerts-smoke --review-save-dir=<abs>/artifacts/verification/problem-alerts/review-smoke`
4. Playtest roteirizado (relógio real do host em 60x)
   `Godot… --path src/IndexCities.Godot -- --problem-alerts-playtest --review-save-dir=<abs>/artifacts/verification/problem-alerts/review-playtest`
5. Regressões de apresentação e ciclo integrado
   `--presentation-smoke`, `--smoke` (headless), `--interaction-regressions-smoke`,
   `--real-time-start-smoke`, `--activity-indicators-smoke` (com cópia de cidade real),
   `--environment-smoke`, `--city-names-smoke`, `--neighbourhood-smoke`, `--road-recovery-smoke`.

## Resultados observados

### QA dos alertas (`--problem-alerts-smoke`) — passou

Saída final (trecho):

```
PROBLEM_ALERTS_QA ok building=29 alerts=road
PROBLEM_ALERTS_QA ok building=31 alerts=water
PROBLEM_ALERTS_QA ok building=33 alerts=power
PROBLEM_ALERTS_QA ok building=35 alerts=
PROBLEM_ALERTS_QA ok buildings=29,31,…,51 alerts=none pop=17 potable=424
PROBLEM_ALERTS_QA ok buildings=29,31,…,51 alerts=water cause="Abastecimento de água insuficiente" potable=0 power=926
```

- Cenário controlado (cópia nunca salva): quatro casas lado a lado — sem rua (rua realmente cortada
  em duas etapas), com água forçada abaixo da demanda, com energia forçada abaixo da demanda e uma
  sem problema. Cada ícone aparece para a causa certa, o conjunto some quando a rua é reconectada e
  as razões voltam ao normal, e a inspeção passa a exibir "Problemas: Falta de água" exatamente
  quando o ícone de água está desenhado.
- Cenário real: cidade com ruas, 12 casas, eólica, captação e tratamento. Com a água potável
  realmente operando, nenhum ícone aparece. Cortando o único acesso viário da captação (ação real do
  jogador), a água potável cai para 0 e todas as casas ocupadas alertam; a causa exibida na inspeção
  é a mesma ("Abastecimento de água insuficiente"). Reconectando a rua, a água volta e os ícones
  somem.

Capturas: `artifacts/verification/problem-alerts/smoke-e0783d028e3548c3b591e10bb382d107/`
(`controlled-initial.png`, `controlled-alerts.png`, `controlled-alerts-detail.png`,
`controlled-inspection.png`, `controlled-cleared.png`, `real-water-healthy.png`,
`real-water-healthy-detail.png`, `real-water-shortage.png`, `real-water-shortage-detail.png`,
`real-water-recovered.png`, `real-water-recovered-detail.png`).

### Playtest roteirizado (`--problem-alerts-playtest`) — passou

Marcadores (resumo do log completo em `playtest-…/log.txt`):

```
before-families  road=0 water=0  power=0 clean=16
real-clock-60x   road=0 water=14 power=0 clean=2   (famílias chegaram em tempo real, a 60x)
city-healthy     road=0 water=0  power=0 clean=16   (ciclo real de água operando)
after-cutting-intake-access road=1 water=15 power=0 clean=1
water-recovery   road=0 water=0  power=0 clean=16   (água de volta após reconectar a rua)
PROBLEM_ALERTS_PLAYTEST_OK waterAlertsAppearedAndCleared buildings=16 population=17
```

Capturas: `artifacts/verification/problem-alerts/playtest-94a6fb8babe3419ababe3f479276b61d/`
(`playtest-start.png`, `playtest-healthy.png`, `playtest-water-shortage.png`,
`playtest-water-detail.png`, `playtest-inspection.png`, `playtest-recovered.png`).

### Outros cenários

| Cenário | Resultado |
| --- | --- |
| `--presentation-smoke` | OK (`INDEXCITIES_PRESENTATION_OK …`) |
| `--smoke` (headless) | OK (`INDEXCITIES_SMOKE_OK buildings=0 roads=17`) |
| `--interaction-regressions-smoke` | OK |
| `--real-time-start-smoke` | OK (cidade fundada, rua e casa construídas, família chegando de carro) |
| `--activity-indicators-smoke` (cópia real) | OK — casa com 2 pessoas no interior, eólica com 2 pessoas dentro, casa vazia sem indicador, badge de carga só com caminhão real descarregando |
| `--environment-smoke`, `--city-names-smoke`, `--neighbourhood-smoke`, `--road-recovery-smoke` | OK |

### Falhas anteriores encontradas neste workspace (não relacionadas a esta feature)

Verificadas também na árvore limpa (`git stash`), falhando igual:

- `--recovery-smoke`: para em "Rua recusada mantém início para corrigir fim".
- `--ui-smoke`: para em "Segundo clique encomenda obra viária real e prepara próxima rua".
- `--circulation-smoke` e `--finances-smoke`: exigem cidade-checkpoint que não existe neste workspace.

## Feedback honesto de jogabilidade

**O que ficou bom**

- Os três ícones são reconhecíveis sem ler nada: estrada laranja com faixas, gota azul, raio amarelo,
  todos sobre um tile escuro de contorno vermelho. Em zoom urbano (≈45–100) ficam bem legíveis e
  pequenos o suficiente para não cobrir o prédio.
- O alerta aparece onde o jogador deve olhar (em cima da entrada do prédio com problema) e some
  quando o problema acaba, sem precisar de clique.
- A inspeção passa a explicitar os mesmos problemas dos ícones, o que reduz a dúvida "o que é esse
  desenho?".
- O balão de pessoas não faz falta: a informação continua na inspeção (moradores, equipe, presença),
  e a tela ficou mais limpa.

**O que ficou confuso ou incômodo**

- Quando a cidade inteira sofre (ex.: captação sem rua), muitos prédios alertam ao mesmo tempo. A
  informação é verdadeira, mas visualmente vira "ruído de gotas". Uma futura agregação (painel/contagem
  por tipo de problema) tornaria a leitura mais barata — sugestão, não requisito.
- Nos primeiros minutos de uma cidade nova, assim que as famílias chegam, os alertas de água aparecem
  em massa porque captação/tratamento ainda não têm equipe. É real, mas pode parecer "o jogo está
  me acusando de algo que ainda estou construindo". A histerese de 20 s ajuda pouco nesse caso.
- O ícone de energia em usina eólica foi suprimido (conforme orientação da tarefa). Ainda assim, uma
  usina sem equipe não produz, e o jogador só percebe pelos alertas de energia **nos consumidores** —
  a usina em silêncio não mostra nada. Vale reavaliar se a usina deveria alertar por "sem equipe".
- Um trecho de rua cortado na frente de um lote gera alerta imediato de acesso, sem tempo de
  carência. Como a demolição de rua tem confirmação com consequências, parece adequado; mas vale
  observar se o jogador entende que a casa está isolada só pelo ícone.

**Problemas do jogo encontrados fora desta feature (para discussão, sem ação tomada)**

- Em cidade pequena, o mercado de trabalho tem um efeito de trava: quem já está empregado só troca de
  emprego se o novo salário for ≥15% maior. Com isso, captação/tratamento/eólica podem ficar sem
  equipe por dias, e sem equipe não há água nem energia — os alertas então aparecem em massa e ficam.
  Nos testes, uma cidade com 12 casas, 1 eólica, captação e tratamento levou horas até o ciclo de água
  operar sozinho; com 2 eólicas e poucos moradores, a eólica pode nunca ser empregada.
- A remoção de um único nó de rua pode isolar um prédio sem aviso adicional além do alerta novo; o
  orçamento da demolição já informa quantas construções perdem conexão, o que é bom.
- Os QAs `--recovery-smoke` e `--ui-smoke` falham neste workspace por motivo anterior a esta feature
  (verificado na árvore limpa); a recuperação da V1 portanto continua sem validação integrada
  automatizada aqui.

## Limites desta entrega

- Nenhuma regra de simulação, economia, save ou consequência foi alterada; a feature é apresentação.
- Canteiros de obra mantêm o aviso forte de obra sem rua já aprovado.
- A parte de carga/descarga do IA-017 continua como estava (pendente de revisão humana na SPEC).
- A supressão do alerta de energia em usinas eólicas e os limiares/tamanhos/estilo são calibração
  desta implementação e estão pendentes de confirmação humana.
