# Alertas de problemas nos prédios — implementação e playtest de 2026-10-15

Revisão humana: **PENDENTE — não é fonte de verdade.**

**Autoria deste documento:** as seções até *Limites desta entrega* foram escritas pelo
**Step 5 Preview** (3 primeiros alertas: estrada, água, energia). A seção
*Extensão para 8 alertas* foi escrita por **Muse Spark 1.3 (opencode)**, que também
estendeu o código, o QA e as evidências. Nada aqui cria, altera ou amplia requisito;
produto vigente continua sendo a SPEC.

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

## Limites desta entrega (Step 5 Preview)

- Nenhuma regra de simulação, economia, save ou consequência foi alterada; a feature é apresentação.
- Canteiros de obra mantêm o aviso forte de obra sem rua já aprovado.
- A parte de carga/descarga do IA-017 continua como estava (pendente de revisão humana na SPEC).
- A supressão do alerta de energia em usinas eólicas e os limiares/tamanhos/estilo são calibração
  desta implementação e estão pendentes de confirmação humana.

---

## Extensão para 8 alertas (Muse Spark 1.3) — revisão humana pendente

### Contexto

A SPEC ampliou a entrada *Alertas de problemas sobre os prédios* para 8 alertas mais a regra
geral (toda causa futura por prédio que pedir ação do jogador também vira balão). Os 3 primeiros
alertas (estrada, água, energia) continuam exatamente como o Step 5 os deixou; nenhum
comportamento deles foi alterado além da refatoração interna descrita abaixo, e o cenário
controlado original continua passando sem modificação.

### O que foi implementado

Apresentação apenas, na camada `IndexCities.Godot`, mais um acessor de leitura na simulação:

- `src/IndexCities.Simulation/Mobility.cs`: novo `BuildingOpen(Building)` público, que só expõe
  a regra de horário que a simulação já usa para `b.Cause`. Nenhuma regra, fórmula ou transição
  foi alterada; é leitura para apresentação, no sentido permitido da dependência
  (Godot → Simulation). Sem isso, o alerta de equipe/estoque precisaria duplicar a regra de
  horário na apresentação e poderia divergir dela (a ARCHITECTURE proíbe a UI de inferir causa
  com lógica própria).
- `CityWorld.ProblemAlerts.cs` reescrito como tabela: `ProblemAlertDefs` (nome, textura, texto
  curto, persistência) + predicado `ProblemAlertActive` por nome, lendo o mesmo estado que a
  simulação usa em `Infrastructure.cs`. Adicionar uma causa futura como alerta agora é: uma linha
  na tabela, um `case` no predicado e um SVG em `Assets/UI`. Estados informativos nunca viram
  alerta: casas não alertam por equipe; loja `Procurando empresa` (sem companhia) e operação
  encerrada de propósito não geram alertas de equipe/estoque; fora do horário não gera
  equipe/estoque (utilitários operam em 3 turnos e contam como abertos, igual à causa).
- Persistência por tipo (calibração, pendente de confirmação humana): acesso/manutenção/captação
  imediatos (condição estrutural ou lenta/monotônica); água/energia/tratamento 20 s simulados
  (como antes); estoque 60 s (evita piscar a cada venda/reposição); equipe 300 s (evita piscar
  na troca de turno). Recuperação 5x mais rápida em todos, como antes.
- 5 SVGs novos no mesmo padrão (tile escuro com contorno vermelho + pictograma plano):
  `problem_intake.svg` (gota riscada, como a SPEC sugere), `problem_noraw.svg` (gota vazada),
  `problem_staff.svg` (pessoa riscada), `problem_stock.svg` (engradado riscado),
  `problem_maintenance.svg` (chave de boca).
- `Main.ProblemAlertsQA.cs`: novo cenário `ProblemAlertsNewCauses` (cópia nunca salva, seed 91)
  com captação seca, captação úmida (controle negativo), tratamento sem água bruta, mercearia
  com empresa real sem saldo/estoque e Prefeitura sem funcionários; `ProblemAlertSnapshot`
  ganhou a coluna `other` (soma dos 5 novos); cenário real e playtest ganharam asserções para
  os novos alertas (tratamento `noraw` no corte, `other==0` na cidade saudável e na recuperação).

### Comandos executados (Godot 4.7.2 mono, Windows)

Engine: `C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64.exe`. Mesmo padrão de
isolamento do Step 5 (argumentos da engine antes de `--`, do jogo depois; saves em
`artifacts/verification`).

1. Build + testes da simulação
   `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1`
   → build OK; **184 testes aprovados, 0 falhas** (várias rodadas, inclusive após cada correção).
2. Importação dos SVGs novos
   `Godot… --headless --path src/IndexCities.Godot --editor --import --quit`
   (a reimportação reescreveu `.import` antigos sem diff de conteúdo; só os 5 novos entram no commit).
3. QA dos alertas (3 tentativas até o verde; ver *Falhas do cenário novo e correções* abaixo)
   `Godot… --path src/IndexCities.Godot -- --problem-alerts-smoke --review-save-dir=<abs>/artifacts/verification/problem-alerts/review-smoke`
4. Playtest roteirizado (relógio real do host em 60x)
   `Godot… --path src/IndexCities.Godot -- --problem-alerts-playtest --review-save-dir=<abs>/artifacts/verification/problem-alerts/review-playtest`
5. Regressões: `--presentation-smoke`, `--smoke` (headless), `--interaction-regressions-smoke`,
   `--real-time-start-smoke`, `--activity-indicators-smoke` (com a cópia real
   `artifacts/verification/activity-indicators/city.json` deixada no workspace pelo Step 5),
   `--environment-smoke`, `--persistence-smoke`, `--city-names-smoke`, `--neighbourhood-smoke`,
   `--road-recovery-smoke` — todas OK (detalhes na tabela abaixo).

### Resultados observados

#### QA dos alertas (`--problem-alerts-smoke`) — passou

Diretório final: `artifacts/verification/problem-alerts/smoke-0afe0ca094d64645bc167c9ab1e3e2cf/`.

```
PROBLEM_ALERTS_QA ok building=29 alerts=road
PROBLEM_ALERTS_QA ok building=31 alerts=water
PROBLEM_ALERTS_QA ok building=33 alerts=power
PROBLEM_ALERTS_QA ok building=35 alerts=
...
newcauses dry=137 treatment=133 shop=135 hall=25
PROBLEM_ALERTS_QA ok building=137 alerts=intake
PROBLEM_ALERTS_QA ok building=133 alerts=noraw
PROBLEM_ALERTS_QA ok building=135 alerts=stock
PROBLEM_ALERTS_QA ok building=25 alerts=staff
PROBLEM_ALERTS_QA ok building=165 alerts=staff
newcauses inspection problems=staff+stock
PROBLEM_ALERTS_QA ok night hall= shop= dry=intake,staff treatment=noraw,staff
newcauses maintenance appeared-and-cleared
newcauses stock cleared-by-real-goods
PROBLEM_ALERTS_QA ok buildings=29,31,…,51 alerts=none pop=17 potable=424
PROBLEM_ALERTS_QA ok intake=237 alerts=road,water treatment=239 alerts=water,noraw
PROBLEM_ALERTS_QA ok buildings=29,31,…,51 alerts=water cause="Abastecimento de água insuficiente" potable=0 power=926
```

- Cenário controlado original: inalterado e verde (os 3 ícones antigos não quebraram).
- Cenário de novas causas: cada ícone aparece para a causa certa e na ordem de persistência
  (captação imediata; tratamento/estoque após 1 min apresentado; equipe só após 5 min);
  captação úmida nunca alerta por localização; à noite Prefeitura e mercearia silenciam e os
  utilitários mantêm equipe; manutenção aparece/restaura de imediato; estoque real apaga o
  ícone; inspeção mostra `Problemas: Sem funcionários · Estoque vazio` igual aos ícones.
- Cenário real: cidade saudável sem nenhum ícone (inclusive captação/tratamento); após o corte,
  a captação isolada mostra `road,water` e o tratamento `water,noraw` lado a lado; tudo limpa
  na recuperação.
- Capturas novas: `newcauses-baseline.png`, `newcauses-flows.png`,
  `newcauses-flows-detail.png` (mercearia com pessoa + engradado riscados, legíveis),
  `newcauses-inspection.png` (inspeção igual aos ícones), `newcauses-maintenance.png`
  (chave de boca sobre a Prefeitura).

#### Playtest roteirizado (`--problem-alerts-playtest`) — passou

Diretório final: `artifacts/verification/problem-alerts/playtest-f82ecba357c04e0e8565449f5544e5ac/`.
A coluna nova `other` (soma dos 5 alertas novos) aparece em todo o log; destaques:

```
before-families      road=0 water=0  power=0 other=0 clean=16
real-clock-60x       road=0 water=14 power=0 other=2 clean=0
water-chain-starting road=0 water=15 power=0 other=3 clean=0  (pico: 176:water,staff 178:water,staff)
water-chain-starting road=0 water=0  power=0 other=0 clean=16  (ciclo operando, equipe real presente)
city-healthy         road=0 water=0  power=0 other=0 clean=16  (asserção other==0 nova)
after-cutting        road=1 water=15 power=0 other=1 clean=1   (178:road,water 180:water,noraw)
water-recovery       road=0 water=0  power=0 other=0 clean=16  (asserção other==0 nova)
PROBLEM_ALERTS_PLAYTEST_OK waterAlertsAppearedAndCleared buildings=16 population=17
```

#### Outros cenários

| Cenário | Resultado |
| --- | --- |
| `--presentation-smoke` | OK |
| `--smoke` (headless) | OK (`buildings=0 roads=17`) |
| `--interaction-regressions-smoke` | OK |
| `--real-time-start-smoke` | OK (fundação, rua, casa, família chegando de carro) |
| `--activity-indicators-smoke` (cópia real) | OK (casa 2 pessoas, eólica 2 pessoas, vazia sem indicador, carga só real) |
| `--environment-smoke` | OK |
| `--persistence-smoke` | OK |
| `--city-names-smoke` | OK (a linha `UnauthorizedAccessException` no log faz parte do próprio teste de falha de escrita) |
| `--neighbourhood-smoke` | OK |
| `--road-recovery-smoke` | OK |

Não repetidos nesta entrega: `--recovery-smoke`, `--ui-smoke` (falhavam antes desta feature
segundo o Step 5; sem `git stash` disponível para reconferir na árvore limpa, não foram
reverificados), `--circulation-smoke`, `--finances-smoke` (exigem cidade-checkpoint ausente).

#### Falhas do cenário novo e correções (só afetaram o QA, nunca o jogo)

1. Rua da margem nascendo desconectada (seed 91 tem o lago em x≈172, fora da rua principal
   que ia até x=100): a obra nunca recebia material. Correção só no QA: a rua principal agora
   se estende até a coluna da margem. Achado via reprodução headless com teste temporário
   (depois apagado), não no Godot.
2. Captação úmida sem posição válida: a rua terminava em z=40 mas a faixa de fonte natural
   (regra real ≤22 da margem) só começa em z≈60. Correção só no QA: rua até `shoreZ-10`
   (ainda longe da água) em vez de `shoreZ-14`.
3. Asserção de manutenção lia antes do refresh (ícones reavaliam com tempo simulado):
   correção só no QA: avançar 60 s antes de cada leitura. Também reposicionada a câmera para
   a Prefeitura na captura da manutenção.

### Cobertura determinística: o que foi e o que não foi

- Afirmado no QA: cada um dos 8 aparece pela causa certa; captação úmida nunca alerta por
  localização; tratamento com água bruta nunca alerta `noraw`; noite silencia equipe/estoque
  fora de horário; manutenção e estoque limpam (restauração forçada e mercadoria real);
  estrada/água/energia limpam (cenário original); cidade saudável e recuperação com `other==0`
  para os 5 novos (playtest, com equipe real presente).
- Não demonstrado deterministicamente: a transição `equipe 0 → presente` apagando um ícone
  de equipe ativo (exigiria contratar e caminhar um SIM até a porta, frágil por natureza); o
  caminho de limpeza é o mesmo código dos demais (histerese + layout compartilhados), e o
  playtest saudável exercita `presente>0 ⇒ sem alerta` com equipe real.
- Visual com 3+ ícones simultâneos: o layout suporta N lado a lado, mas o máximo observado
  foi 2 (ex.: `road,water`, `staff,stock`); vale uma captura futura se combinações triplas
  ficarem comuns.

### Feedback honesto de jogabilidade (Muse Spark 1.3, sessão roteirizada + capturas)

**O que ficou bom**

- Os 5 ícones novos se distinguem entre si e dos 3 antigos sem ler nada: gota riscada
  (captação), gota vazada (tratamento), pessoa riscada (equipe), engradado riscado (estoque) e
  chave de boca (manutenção). No detalhe ficam bem legíveis; na visão de cidade o tile
  vermelho cumpre o papel de "tem problema aqui, aproxime o zoom".
- As combinações lado a lado funcionam na prática (`road,water` na captação isolada,
  `water,noraw` no tratamento, `staff,stock` na mercearia) e a inspeção repete exatamente os
  mesmos problemas — o jogador que não decifrar o desenho descobre clicando.
- A cidade saudável zerando os 8 (playtest `other==0` com equipe real nos turnos) mostra que
  o sistema não gera ruído quando está tudo bem; o barulho aparece só na fase de arranque.

**O que me incomodou (para decisão humana, sem ação tomada)**

- O alerta de equipe pisca nas trocas de turno de cidade pequena: com 1–2 funcionários por
  prédio, qualquer buraco acima de 5 min simulados vira ícone (observado `other=1..3`
  durante o arranque e prédios inteiros sem ninguém às 22h). É o que a causa da simulação
  diz, mas como leitura de jogo parece acusação por algo que o jogador ainda está montando —
  mesmo efeito que o Step 5 notou para água/energia no início.
- Previsão não observada, mas decorrente da regra: fazenda (e Prefeitura/Pátio) tem turno
  único 7–15h e fica "aberta" até as 19h — deve alertar `Sem funcionários` todo fim de tarde
  mesmo com equipe completa. Se isso for considerado falso-positivo, a definição de "horário
  de funcionamento" do alerta precisa de decisão humana (o código atual segue a letra da tarefa).
- Com 6+ tipos possíveis, prédios utilitários em crise podem empilhar vários ícones; com 2
  ficou bom, com 3+ ainda não há evidência visual.
- A explicação curta "ao passar o mouse" exigida pela SPEC continua sem implementação (herdado
  do Step 5): hoje a explicação está no clique (inspeção), não no hover. Sprite3D não tem
  tooltip nativo; fazer hover 3D real é trabalho novo de apresentação.
- Numa captura do playtest a barra de catálogo apareceu parcialmente renderizada
  (`playtest-water-shortage.png` mostra só 2 ferramentas). O código do catálogo não foi
  tocado e as demais capturas estão normais; parece artefato transitório da captura, mas
  merece um olhar separado.

### Limites desta extensão

- `src/IndexCities.Simulation` ganhou só o acessor de leitura `BuildingOpen`; nenhuma regra,
  economia, save ou consequência mudou. A feature continua sendo apresentação.
- Limiares (0,5 de água/energia; 0,1 de energia na causa; 0,1 de estoque; 0,9 de manutenção) e
  persistências (0/20/60/300 s) são calibração desta implementação, pendentes de confirmação
  humana, como a SPEC já previa.
- Falhas pré-existentes citadas pelo Step 5 (`--recovery-smoke`, `--ui-smoke`) não foram
  rechecadas aqui.

---

## Correções pós-revisão independente (Muse Spark 1.3) — revisão humana pendente

Revisor: DeepSeek V4.1 Flash, em `problem-alerts-review-2026-10-15.md` (commit `96ea238`).
Corrigidos nesta seção: BLOQUEANTE-1, IMPORTANTE-1, MENOR-1, MENOR-2 e MENOR-5. MENOR-3 e
MENOR-4 ignorados por orientação (o 3 já foi esclarecido pelo revisor como artefato do QA;
o 4 é encaminhamento de revisão humana, sem código).

### O que foi implementado

- **BLOQUEANTE-1 — tooltip no hover** (`Main.AlertTooltip.cs` novo; ganchos em `Main.cs`,
  `Main.UI.cs`, `Main.RoadTools.cs`; projeção em `CityWorld.ProblemAlerts.cs`):
  com o cursor sobre um prédio **ou** sobre o balão que tem alertas, aparece um painel flutuante
  com o nome do prédio + o mesmo texto da inspeção (`BuildingProblemText`). Reaproveita o
  `PickBuilding` do clique de seleção mais a projeção da âncora do balão (raio de 64 px);
  avaliado só em movimento do mouse (e reposicionado em movimento de câmera com o último
  pixel, sem varredura por quadro); suprimido com qualquer ferramenta ativa (construção, rua,
  demolir, mover), com modal aberto e sobre controles da HUD.
- **IMPORTANTE-1 — equipe só quando pede ação** (`ProblemAlertActive`, sem tocar a `Cause`):
  só alerta com ninguém presente quando há turno ativo previsto para alguém (`OnShift>0`)
  ou ninguém contratado (`Hired==0`); equipe contratada fora de turno (ex.: Prefeitura 15–19h)
  é estado normal e silencia. Persistência de 300 s mantida.
- **MENOR-1 — sem O(prédios×cidadãos)**: `CitySimulation.WorkforceByBuilding()` (novo,
  leitura) conta presente/em-turno/contratados por prédio numa passada só; `ShiftActive`
  foi dividido sem mudar comportamento (`ShiftActiveIn` recebe o prédio, eliminando o
  `BuildingById` linear por SIM). O update dos alertas consome o snapshot; nenhum scan por
  prédio resta nos alertas.
- **MENOR-2**: marcador do playtest agora interpola o horário de verdade.
- **MENOR-5**: o alerta de água cobre também `WaterOutageSeconds>=WaterClosureHours` (a
  "parada após falta persistente", mesma família); casas nunca carregam essa causa, então só
  vale para não-casas, igual à cadeia da simulação.
- `src/IndexCities.Simulation` continua só com leitura nova (`BuildingOpen`,
  `WorkforceByBuilding`, refatoração interna idêntica de `ShiftActive`); nenhuma regra mudou
  (184 testes passam sem alteração).

### Comandos e resultados

`scripts/Build.ps1` → build OK, **184 testes, 0 falhas**.

- `--problem-alerts-smoke` — passou (dir. `smoke-d5db700f26ac4a26acd544629d310209`).
  Marcadores novos: `newcauses tooltip=balloon+building` (cursor real com `WarpMouse`,
  texto `Mercearia\nProblemas: Sem funcionários · Estoque vazio` igual à inspeção),
  `newcauses tooltip hidden-with-tool-and-away`, `newcauses 16h hired-crew-between-shifts
  silent` (contratado residente na Prefeitura, 16h sem alerta), `real outage-tail
  water-appeared-and-cleared` (contador forçado no tratamento recuperado). Captura nova:
  `newcauses-tooltip.png` (balão + inspeção + tooltip com o mesmo texto).
- `--problem-alerts-playtest` — passou (`playtest-b21ef045d75241f2acf176d192d74d7a`,
  `PROBLEM_ALERTS_PLAYTEST_OK`, marcadores de horário agora interpolados). Efeito da nova
  regra no jogo real: Prefeitura (25) às 15:10 `clean` (antes `staff`), sem `staff` às 6:55;
  `other=0` no arranque (antes 1–2); cidade saudável e recuperação com `other==0`.
- `--presentation-smoke` OK; `--activity-indicators-smoke` OK com a mesma cópia real
  (`cargo-indicator=(1, True)`).

### Cobertura e limites desta correção

- Afirmado: tooltip aparece no balão e no prédio, some com ferramenta e longe de alertas;
  16h sem alerta com contratados + alerta com zero contratados (manhã do mesmo cenário);
  outage→água aparece e limpa; snapshot único (por construção, sem scan por prédio).
- Não coberto: tooltip sobre controles da HUD (mesmo padrão não-determinístico do preview);
  transição equipe 0→presente apagando ícone ativo (caminho compartilhado, exercitado com
  equipe real no playtest saudável); combinações de 3+ ícones (máx. observado continua 2).

### Feedback honesto (Muse Spark 1.3, após a correção)

- O falso-positivo das 15–19h sumiu de verdade no playtest, e o arranque ficou silencioso
  (`other=0` onde era 1–2). O preço: à noite, com turnos vagos e ninguém presente, os
  utilitários ainda alertam — correto pela regra (ninguém contratado/escalado), mas vai
  parecer "obra acusando de madrugada" para quem joga; é o comportamento que a tarefa pediu,
  e a calibração (ou uma noção de "turno previsto vs. quadro vazio") segue aberta.
- O tooltip ficou legível e no lugar certo, mas numa captura ele sobrepõe o painel de
  inspeção aberto — inevitável quando os dois mostram a mesma coisa ao mesmo tempo; sem
  inspeção aberta ele flutua limpo ao lado do cursor. Posição e raio (64 px) são calibração.
- A divergência `Cause` × alerta ficou explícita e intencional: às 16h a inspeção diz
  "Sem funcionários presentes no turno" (causa real da simulação) sem balão. É o que a
  tarefa mandou (alerta = pede ação), mas o jogador que ler a inspeção pode estranhar a
  ausência do ícone — vale decisão humana sobre o texto da causa ou do alerta.
