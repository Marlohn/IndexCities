# IndexCities — lacunas de assets visuais da V1

> **EXPLORAÇÃO — NÃO É FONTE DE VERDADE. Revisão humana: PENDENTE.** Pesquisa produzida por IA em 2026-10-19 para avaliação; não cria requisito, backlog, tarefa ou autorização de implementação. A fonte de verdade continua sendo a [SPEC](../SPEC.md) e as seções V1 de [docs/spec](../spec/). Documentos relacionados: [Briefing de assets V1](first-playable-assets.md) e [Pipeline de assets](asset-production-pipeline.md).
>
> **Análise somente leitura.** Não implementei, não alterei assets, não editei documentos canônicos e não executei o jogo.
>
> **Data:** 2026-10-19  
> **Fonte de verdade:** [SPEC](../SPEC.md) e as seções **“Em vigor no jogo (V1)”** de [docs/spec](../spec/).  
> **Evidência de assets:** `src/IndexCities.Godot/Assets/V1/manifest.v1.json`, `Assets/V1/README.md`, `Assets/Construction/manifest.construction.json`, pastas `Assets/V1`, `Assets/Construction`, `Assets/UI` e `Assets/RecoveryEnvironment`.  
> **Evidência de integração:** código C# em `src/IndexCities.Godot` (arquivos e linhas citados abaixo).  
> **Limite:** o briefing exploratório `docs/exploration/first-playable-assets.md` não foi tratado como requisito; nenhuma sugestão dele virou obrigação neste relatório. Também não julguei qualidade estética — o próprio `Assets/V1/README.md` declara a revisão humana de estética e medidas como pendente.

---

## 1. Resultado em uma frase

A V1 **já tem o conjunto central de arte**: os 10 prédios do catálogo, as peças de rua 1+1 (reta, L e poste), árvore, SIM pedestre, carro, caminhão de carga, frota de obra e os ícones de problema. **Não encontrei lacuna P0** (nada que impeça a primeira partida jogável). As lacunas **P1** são de legibilidade e integração — semáforo, diferença visual da rodovia, vagas/garagem, canteiro e equipe de obra — e o restante é **P2** (variantes, estados já modelados que não são ligados, acabamento) ou **fora de escopo**.

### Resumo por prioridade

| Prioridade | Quantidade | Itens |
|---|---:|---|
| **P0** — bloqueia a primeira partida | **0** | — |
| **P1** — importante para a V1 | **5** | semáforo, rodovia, vagas/garagem, canteiro, equipe de obra |
| **P2** — polimento | **7** | fases do SIM, variantes de edifício, cultivo da fazenda, lodo/decantador, peças de rua/meio-fio, sinalização de preferência, destaque de seleção |
| **Fora de escopo / não exigido** | — | ver §5 |

---

## 2. Como classifiquei

| Classificação | Significado |
|---|---|
| **Existe** | O asset existe no repositório e está integrado ao jogo, ou o requisito é atendido por geometria procedural/material existente. |
| **Parcial** | Existe uma base, mas ela é placeholder, está incompleta ou o asset foi produzido e **não está integrado**. Não é o mesmo que “ausente”. |
| **Ausente** | Não há cena/malha/material/ícone dedicado para o requisito. |
| **Sistema ausente ≠ asset ausente** | Se a SPEC não pede o sistema na V1, não há lacuna de arte. Exemplos: chuva, ônibus, ponte, interior de prédio. |
| **P0** | Impede a primeira partida jogável ou torna uma regra V1 impossível de cumprir. |
| **P1** | Não impede jogar, mas compromete a leitura de uma regra importante ou a experiência central de construir/observar. |
| **P2** | Polimento, variedade e fidelidade visual; pode esperar uma entrega posterior. |

**Regra de leitura:** “asset ausente” significa que falta um arquivo/peça de arte. “Integração pendente” significa que a peça já existe, mas o jogo não a usa. As duas colunas aparecem separadas na tabela.

---

## 3. Lacunas priorizadas

### 3.1 Tabela principal

| # | Item / categoria | Requisito da SPEC (caminho e seção) | Estado atual | Lacuna concreta (tipo de asset) | Prioridade | Nota |
|---|---|---|---|---|---|---|
| G1 | **Semáforo urbano** | `docs/spec/transporte-circulacao.md#controle-do-jogador-sobre-semáforos-e-preferência-dos-cruzamentos`; `#regras-de-circulação-viária` (“Semáforos operam inicialmente em ciclos fixos”) | **Parcial** — um único `BoxMesh` chamado `Signal`, em `(-4, 3, 4)`, por trecho; a cor alterna verde/vermelha globalmente (`CityWorld.cs:281` e `:133`) | **Cena/malha 3D**: poste, caixa, lentes e braço; materiais emissivos; opcional faixa de retenção | **P1** | O cubo comunica “verde/vermelho”, mas não é reconhecível como semáforo e não mostra fases por direção. |
| G2 | **Rodovia x via urbana** | `docs/spec/transporte-circulacao.md#vias-calçadas-e-estacionamento` (dois tipos básicos; via urbana com calçadas); `#regras-de-circulação-viária` (rodovia com velocidade maior) | **Parcial** — a rodovia usa a mesma largura, o mesmo asfalto e a mesma calçada da via urbana; `road.Highway` só omite postes e faixas de pedestres (`CityWorld.cs:272-273`); a calçada é desenhada para todas as vias (`CityWorld.RoadSurface.cs:82-86`) | **Malha/material/cena**: perfil próprio de rodovia — acostamento, ausência de calçada, defensas/balizas, tracejado e sinalização distintos | **P1** | Hoje a rodovia parece uma rua urbana sem postes; o jogador não distingue os dois tipos. |
| G3 | **Vaga/garagem privada** | `docs/spec/visao-geral.md#cidade-viva-na-v1` (“Vagas reais: cada casa tem 1 vaga privada no lote…”); `docs/spec/transporte-circulacao.md#vias-calçadas-e-estacionamento` (“Casas e prédios podem ter vagas/garagens privadas”; “Veículos particulares precisam estacionar de verdade”) | **Parcial** — a simulação tem `ParkingSpot` e os carros estacionam, mas não há pad, pintura, garagem ou marcador visível; a casa simples declaradamente não tem garagem (`Assets/V1/README.md §3`) | **Malha/material/marker**: pad de vaga, pintura de solo, garagem simples ou marcador de vaga | **P1** | A regra existe, mas o jogador não vê onde o carro estaciona nem onde a vaga foi prevista. |
| G4 | **Canteiro de obra (prédio e via)** | `docs/spec/construcao.md#construção-e-obras` (obra real, progresso, paralisação e causas); `#pavimentação-física-das-ruas` (“Marcação de canteiro, base, aplicação de asfalto e acabamento”) | **Parcial** — prédio em obra é uma base plana + 4 postes + `Label3D` com percentual/causa (`CityWorld.cs:123-127`); a via mostra máquinas e caixas escuras de asfalto aplicado (`CityWorld.Construction.cs:112-120`), sem cerca, cones ou camada de base | **Cena/props**: tapume/cerca, cones/balizas, andaime, pilha de materiais; camada de base/gravel; opcional mesh parcial do prédio | **P1** | O progresso é legível por texto, mas para quem não lê o canteiro não se anuncia como obra. |
| G5 | **Equipe de obra visível** | `docs/spec/construcao.md#construção-e-obras` (“Obras dependem de trabalhadores reais”; “as máquinas e a equipe executam trabalho real no canteiro”); `docs/spec/materiais-logistica.md#bootstrap-inicial-de-construção` (equipe externa inicial) | **Parcial** — `CrewAboard` e `CrewCount` são apenas flags de simulação (`CityWorld.Construction.cs:78` e `:99`); nenhum trabalhador ou operador é desenhado no canteiro ou na máquina | **Cena/animação**: reutilizar `agent.sim_pedestrian.a` com pose/animação de trabalho ou criar operário simples (capacete/colete) | **P1** | Não exige sistema novo: posicionar a figura a partir de `CrewAboard`/`CrewCount` já existentes. |
| G6 | **Variantes de SIM por fase da vida** | `docs/spec/populacao.md#relacionamentos-família-e-ciclo-de-vida` (infância, adolescência, vida adulta e velhice); `docs/spec/mundo-tempo-saves.md#tempo-de-jogo` (acompanhamento geracional) | **Parcial** — um único `agent.sim_pedestrian.a` adulto é usado para todos os cidadãos (`CityWorld.cs:134`); não há criança, adolescente ou idoso | **Cena/malha**: 2–3 silhuetas (criança, adolescente/idoso) + paleta de roupa; animações `idle`/`walk` compatíveis | **P2** | A SPEC exige o ciclo de vida, não modelos distintos; a variedade é a forma natural de tornar o ciclo visível. |
| G7 | **Variantes visuais de edifício** | `docs/spec/visao-geral.md#conceito-e-apresentação` (“Edifícios são oferecidos em modelos completos e variantes visuais prontas…”; quantidades exatas são calibração/produção) | **Parcial** — existe uma cena por família (10/10 prédios), sem fachada, telhado ou cor alternativa | **Cena ou material**: ao menos 1 variante para casa, mercearia e fazenda; miniaturas/ícones se a variante entrar no catálogo | **P2** | Não bloqueia; atende à “liberdade estética” da V1, com quantidade a calibrar. |
| G8 | **Estado de cultivo da fazenda** | `docs/spec/economia.md#agricultura` (produção real da fazenda); `docs/spec/visao-geral.md#cidade-viva-na-v1` (atividade real visível) | **Parcial** — `State_Crop/Crop` existe no manifesto e no `Assets/V1/README.md §12.2`, mas nenhum código o liga; o campo fica sempre com solo preparado e vazio | **Integração** (sem asset novo): ligar `State_Crop` ao estado real de cultivo/estoque | **P2** | O asset já foi produzido; falta o vínculo com o estado real. |
| G9 | **Estado de lodo e ponte do decantador** | `docs/spec/servicos.md#água-esgoto-energia-e-polução` (tratamento de esgoto); `docs/spec/visao-geral.md#cidade-viva-na-v1` | **Parcial** — `State_Sludge` e `ClarifierBridge` existem no manifesto/README §12.2, mas nenhum código os liga ou anima | **Integração** (sem asset novo): lodo visível e ponte girando com o estado real | **P2** | O asset já foi produzido; falta o vínculo com o estado real. |
| G10 | **Peças autorais de rua e meio-fio** | `docs/spec/transporte-circulacao.md#vias-calçadas-e-estacionamento` (calçadas reais); `docs/SPEC.md` §V1 (ruas 1+1 com calçadas e postes) | **Parcial** — as cenas `road.urban_1x1.a.straight/corner_l` e os materiais `road_curb`, `road_marking_yellow` e `road_sidewalk_service` existem, mas a superfície ativa é procedural e só usa `road_asphalt`/`road_sidewalk` (`CityWorld.RoadSurface.cs:76-99`); a linha central é pintada com hex, não com o material | **Integração/material**: usar as peças autorais ou aplicar `road_curb` e `road_marking_yellow` no perfil procedural | **P2** | A regra de calçada é atendida; é fidelidade visual, não requisito faltante. |
| G11 | **Sinalização de preferência no cruzamento** | `docs/spec/transporte-circulacao.md#controle-do-jogador-sobre-semáforos-e-preferência-dos-cruzamentos`; `#padrão-automático-inicial-de-cruzamentos-urbanos` | **Parcial** — o semáforo aparece no mundo; a preferência só aparece no painel de inspeção (`Main.UI.cs:100`), sem placa ou marcação no cruzamento | **Malha/material**: placa de preferência/dê-a-preferência ou marcação viária | **P2** | A SPEC exige o controle, não a placa; a sinalização ajuda a ler a regra sem abrir o painel. |
| G12 | **Destaque de seleção** | `docs/spec/populacao.md#cidadãos-e-simulação` (seleção individual); `docs/spec/visao-geral.md#conceito-e-apresentação` (leitura em camadas) | **Parcial** — a seleção é guardada em `Main.cs:156` e a inspeção abre, mas não há contorno/highlight no prédio, SIM, veículo ou rua selecionada | **Shader/material ou marker**: contorno de seleção | **P2** | Não exigido explicitamente; melhora o retorno do clique e a leitura de quem foi selecionado. |

### 3.2 Evidências que sustentam a tabela

- **Semáforo:** `CityWorld.cs:281` cria `new MeshInstance3D{Name="Signal",Mesh=new BoxMesh{Size=new(.3f,.7f,.3f)},Position=new(-4,3,4)…}`. `CityWorld.cs:133` faz `signal.Visible = road.Control==JunctionControl.Signal && road.Neighbours.Count>=3` e troca o material entre `6ca56b` e `ba594d`.
- **Rodovia:** `CityWorld.cs:258-272` usa `half = RoadWidth/2` para qualquer via e só condiciona postes a `!road.Highway`; `CityWorld.RoadSurface.cs:82-86` desenha `_sidewalkSurface` para o conjunto inteiro de pegadas, sem separar rodovia.
- **Vagas:** a simulação usa `ParkingSpot` (ver `Main.CirculationQA.cs:34`, que menciona “Private lot spots are 4.6 m apart”); não há referência a mesh/pad de vaga no mundo. O `README` da casa diz “Não tem garagem, cerca, jardim nem janela acesa” (`§3`).
- **Canteiro:** `CityWorld.cs:123` desenha `Box(node, new(width,.22f,depth), …)` e quatro postes; `:125-127` monta `((int)(project.Progress*100))+"%"` + causa no `Label3D`. A via aplicada é `Box(root, size, …, "3e4345")` em `CityWorld.Construction.cs:119`; os materiais `construction_gravel.tres`/`construction_asphalt.tres`/`construction_sand.tres` existem, mas não são usados por esse caminho.
- **Equipe:** `CityWorld.Construction.cs:78` e `:99` usam `CrewAboard`/`CrewCount` para decidir descarga e pavimentação; não há nó de trabalhador no canteiro.
- **SIM:** `CityWorld.cs:134` instancia sempre `Scene("agent.sim_pedestrian.a")`; o manifesto descreve “SIM pedestre (adulto estilizado)”.
- **Estados não ligados:** `BindVisual` (`CityWorld.cs:249-255`) liga `Nacelle/Rotor`, `State_Water` e `State_Stock`; não liga `State_Crop`, `State_Sludge` nem `ClarifierBridge`. O manifesto/README §12.2 registra esses nós.
- **Rua procedural:** `CityWorld.RoadSurface.cs:76-99` usa `road_asphalt.tres` e `road_sidewalk.tres`; as peças `straight`/`corner_l` não são instanciadas para a malha viária ativa. O poste é a exceção: `road.urban_1x1.a.lamp` é usado em `CityWorld.cs:272`.
- **Junction:** `Main.UI.cs:100` oferece os botões de configuração na inspeção da rua; não há placa/marcação no mundo.
- **Seleção:** `Main.cs:156` limpa `_selectedBuilding/_selectedCitizen/_selectedVehicle/_selectedRoad`; não há passe de contorno no `CityWorld`.

---

## 4. Cobertura confirmada (não é lacuna)

Esta seção existe para evitar a leitura de que “falta tudo”. O conjunto abaixo foi verificado no manifesto, nas pastas e no código de integração.

| Categoria | O que já existe | Onde / evidência |
|---|---|---|
| **10 prédios do catálogo V1** | Prefeitura, Pátio de Obras, casa simples, mercearia, fazenda, posto, captação, tratamento de água, tratamento de esgoto e eólica — cada um com cena `.tscn`, malhas, materiais, ícone SVG e miniatura PNG | `Assets/V1/Scenes`, `Meshes`, `Materials`, `Icons`, `Thumbnails`; `manifest.v1.json`; `README §1` e `§12.2` |
| **Ruas 1+1** | Peças reta e L, com conectores, calçada, meio-fio, linha central e slot de poste; miniatura da reta usada no catálogo | `Assets/V1/Scenes/road.urban_1x1.a.*`; `README §1`; `Main.UI.cs:46` |
| **Poste** | Cena com `Body`, `LampGlass`, `Candidate_LightSource`; o jogo instancia, acende à noite e troca o material conforme energia | `CityWorld.cs:272` e `:133`; `road_lamp_glass_on/off.tres` |
| **Árvore** | Cena, malha e miniatura; 145 árvores posicionadas por seed; removidas quando uma obra ocupa o lote | `CityWorld.cs:50` e `:140`; `vegetation.tree_broadleaf.a` |
| **SIM pedestre** | Cena com `idle` e `walk`, partes de malha compartilhadas e slots de material (`sim_skin`, `sim_hair`, `sim_top`, `sim_bottom`, `sim_shoes`) | `Assets/V1/README.md §3`; `CityWorld.cs:134-136` |
| **Carro particular** | Cena, quatro rodas, pintura em 6 cores (base + vermelho, azul, branco, amarelo e verde) | `CityWorld.cs:68`; `Materials/car_paint*.tres`; `README §3` |
| **Caminhão de carga** | Cena com baú fechado, rodas compartilhadas e `Candidate_CargoDoor` | `Assets/V1/Scenes/vehicle.truck_cargo.a.tscn`; `README §3` |
| **Frota de obra** | Basculante (com carga de areia/brita e asfalto, cobertura, caçamba e pistão), betoneira, carroceria aberta (aço/madeira), pavimentadora (hopper, screed), rolo, guincho de plataforma e guincho pesado | `Assets/Construction/Scenes/*`; `manifest.construction.json`; `CityWorld.Construction.cs` |
| **Animação de obra** | Caçamba bascula, cobertura sai, cargas aparecem, betoneira gira, hopper da pavimentadora encolhe, screed desce, rolo gira, guincho carrega e inclina o veículo | `CityWorld.Construction.cs:72-104` |
| **Progresso de pavimentação** | O trecho aplicado aparece atrás da pavimentadora como caixas escuras; o rolo finaliza; o asfalto final substitui o trecho | `CityWorld.Construction.cs:112-120`; `CityWorld.RoadSurface.cs:76-99` |
| **Ícones de problema** | `problem_road`, `problem_water`, `problem_energy`, `problem_intake`, `problem_noraw`, `problem_staff`, `problem_stock`, `problem_maintenance` e `problem_alert_tile` | `Assets/UI`; mapeamento em `CityWorld.ProblemAlerts.cs` |
| **Outros ícones de UI** | `activity_badge.svg`, `activity_cargo.svg` e `demolish.svg`; `activity_people.svg` existe, mas o balão de pessoas foi retirado pela IA-017 | `Assets/UI`; `Main.UI.cs:47` |
| **Ambiente** | Grama e água com materiais/shaders; lago com contorno irregular e margens; terreno plano e borda fixa | `Assets/RecoveryEnvironment`; `CityWorld.Lake.cs` |
| **Dia/noite** | O sol muda energia e rotação conforme a hora; postes acendem à noite e dependem de energia; rotor da eólica gira com `WindFactor` | `Main.cs` (setup do sol); `CityWorld.cs:128` e `:132-133` |
| **Estados de operação** | Água tratada, efluente de esgoto, estoque do Pátio por material e rotor da eólica são ligados ao estado real | `CityWorld.cs:128` e `:249-255` |
| **Prévia de construção** | Miniatura do tipo no cartão de resumo; caixas translúcidas de posição, linha de acesso, marcadores de início/fim/curva e rótulos de validade | `Main.Preview.cs:38-74`; `CityWorld.cs:302-337` |
| **Faixa de captação** | Faixa verde procedural em volta das fontes, derivada das mesmas regras de validação | `CityWorld.WaterIntakeRange.cs` |

---

## 5. Fora de escopo / não exigido na V1

Esta lista é separada da tabela de lacunas de propósito: **não são requisitos de arte da V1**. Não devem ser tratados como pendência sem uma decisão humana que os traga para o escopo.

### 5.1 Adiado explicitamente pela SPEC

`docs/SPEC.md` §“V1 — primeira versão jogável integrada”, bloco “Adiados para entregas posteriores”:

- via urbana **2+2** e perfis adicionais;
- **Centro de Materiais de Construção** como construção específica;
- variedade integral de negócios, **alta densidade**, prédios mistos e locação comercial;
- indústria pesada e cadeias produtivas avançadas;
- polícia, saúde, educação e creches;
- transporte coletivo/sob demanda, turismo e atrações;
- programas municipais avançados e sistemas sociais/familiares avançados;
- desafios/conquistas;
- modificações estéticas posteriores;
- **pontes e viadutos**;
- parques especializados;
- **modo Criativo**.

Não há asset obrigatório para esses itens nesta entrega.

### 5.2 Visão aprovada, ainda não implementada

`docs/spec/visao-geral.md#visão-aprovada-ainda-não-implementada`:

- identidade visual simples da empresa na fachada (o painel `SignText` já existe e é preenchido com o nome da empresa, mas isso não é requisito V1);
- troca de aparência de prédio concluído;
- pincel de vegetação/decoração;
- linha do tempo da cidade, desafios opcionais e reconhecimento;
- tutoria/orientação inicial.

### 5.3 Opcional ou experimental

- **Chuva visual/sonora:** `docs/spec/visao-geral.md#conceito-e-apresentação` diz que o mundo **pode** apresentar episódios ocasionais; é um teste visual, não um requisito. Não há partículas/som de chuva, e isso **não** é lacuna P1/P2.
- **Clima, neve, estações visuais:** não aprovados.
- **Skybox, nuvens, pássaros, efeitos ambientais:** não exigidos.

### 5.4 Sistemas abstratos ou fora do recorte

- interior de edifícios e construção por cômodos (a SPEC declara o interior abstrato);
- editor modular por peças no jogo;
- ônibus, táxi, bicicleta, trem, porto, aeroporto;
- acidentes de trânsito com colisão/atendimento;
- oficina local, empresa de guincho local e manutenção mecânica;
- modelo 3D exclusivo por ramo de negócio (a própria SPEC permite reutilização visual com fachada/letreiro);
- LODs/HLODs e otimizações de malha (trabalho técnico, não requisito visual de produto);
- ícones individuais para cada recurso no HUD (o catálogo visual já cobre a ação básica; seria polimento).

### 5.5 Itens opcionais que podem virar P2 se houver pedido

- **Caminhão-tanque de combustível:** a SPEC exige entrega física de combustível, mas **não exige** uma carroceria-tanque; o caminhão fechado (`TruckBody.Cargo`) atende. Se for desejado para reconhecimento, é P2.
- **Portal/placa da conexão exterior:** a rodovia chegando à borda já cumpre a conexão; um portal é decorativo.
- **Luzes de farol/lanterna dos veículos à noite:** os materiais existem como “off” e não são emissivos; não há requisito explícito de iluminação de veículos.
- **Mais variedades de árvore, carro e caminhão:** “vegetação simples” e frota atual atendem; variedade é P2.
- **Ícones de “mover”, “cancelar” e “selecionar”:** o catálogo já usa miniaturas/ícones para as ações principais; os botões textuais são aceitáveis, mas podem ganhar ícone em polimento.
- **Destaque de prévia de demolição no mundo:** o modal já lista custo, prédios isolados e obras afetadas; um highlight 3D seria polimento.

---

## 6. Ambiguidades da SPEC

A SPEC não decide, em todos os casos, se o requisito funcional exige um **asset visual dedicado**. As perguntas abaixo precisam de decisão humana antes de uma encomenda grande; a recomendação é o menor caminho que preserva a leitura do jogo.

| # | Pergunta | O que a SPEC exige | O que ela não decide | recomendação |
|---|---|---|---|---|
| A1 | O semáforo precisa de modelo 3D completo? | Controle do jogador, ciclos fixos e leitura do cruzamento | Poste, caixa, lentes, braço, fase por direção ou faixa de retenção | **Sim, P1**: um semáforo low-poly reconhecível, mesmo com uma fase visual por aproximação. |
| A2 | A rodovia precisa ser visualmente distinta da via urbana? | Dois tipos básicos; via urbana com calçadas; rodovia mais rápida | Acostamento, defensa, baliza, tracejado, largura ou placa | **Sim, P1**: ao menos remover a calçada e marcar o acostamento; o jogador precisa distinguir os tipos. |
| A3 | “Vaga real” exige pintura/pad/garagem? | Uma vaga privada por casa e vagas nos demais lotes; carro estaciona fisicamente | Se a vaga precisa ser desenhada no chão ou basta existir na simulação | **Sim, P1/P2**: um pad ou pintura simples + carro parado resolve a leitura; garagem fechada é opcional. |
| A4 | O canteiro precisa de cerca/andaime ou a base + postes + texto basta? | “Marcação de canteiro” e progresso perceptível | Se a marcação pode ser só a base e quatro postes | **P1**: ao menos um kit de tapume/cones; o texto não serve para quem não lê. |
| A5 | A equipe de obra precisa aparecer? | Trabalhadores reais e equipe executando trabalho no canteiro | Se a equipe pode ser só simulada, sem figura no mundo | **P1**: sim, reutilizando o SIM com pose de trabalho; é barato e dá causalidade visível. |
| A6 | O combustível exige caminhão-tanque? | Entrega física de combustível ao posto | Carroceria dedicada ou caminhão fechado genérico | **Não obrigatório**: o caminhão fechado atende; tanque é P2 se a legibilidade pedir. |
| A7 | Quantas variantes visuais de prédio a V1 exige? | “Variantes visuais prontas”; quantidades são calibração/produção | Número mínimo por família | **P2**: pelo menos 1 variante para casa, mercearia e fazenda; o resto pode esperar. |
| A8 | Criança/idoso precisam de malha própria? | Ciclo de vida e acompanhamento geracional | Se a fase da vida precisa mudar a silhueta | **P2**: 2–3 silhuetas já tornam o ciclo legível; não é bloqueio. |
| A9 | Cultivo e lodo precisam ser visíveis? | Produção agrícola e tratamento de esgoto reais | Se o estado interno precisa aparecer no modelo | **Integração P2**: os nós `State_Crop`/`State_Sludge` já existem; ligar é barato. |
| A10 | As peças autorais de rua devem substituir a superfície procedural? | Calçadas, postes e vias 1+1 | Se a malha ativa precisa ser a cena autoral ou se o procedural basta | **Não obrigatório**: o procedural atende; usar `road_curb`/`road_marking_yellow` é polimento. |
| A11 | A preferência de cruzamento precisa de placa no mundo? | Controle do jogador e regra de preferência | Se a escolha precisa ser visível fora do painel | **P2**: uma placa/marcação ajuda, mas o painel já é o controle oficial. |
| A12 | A seleção precisa de contorno no mundo? | Seleção individual e leitura em camadas | Se o painel de inspeção basta como feedback | **P2**: um contorno simples melhora o retorno, mas não é requisito explícito. |
| A13 | Chuva é lacuna? | Pode apresentar chuva visual/sonora como teste | Se a V1 precisa ter chuva | **Não**: opcional; não encomendar como se fosse obrigação. |

> **Observação:** a recomendação é uma sugestão de produção, não uma aprovação de produto. Itens P1 continuam dependendo de decisão humana se a interpretação “asset dedicado” não estiver clara.

---

## 7. Sequência sugerida (se a produção for autorizada)

1. **Fechar P1 primeiro:** semáforo, perfil de rodovia, vaga/garagem, kit de canteiro e equipe de obra. São os itens que mais mudam a leitura da V1 sem tocar em regras.
2. **Integrar o que já existe (P2 barato):** `State_Crop`, `State_Sludge`, `ClarifierBridge`, `road_curb`/`road_marking_yellow`.
3. **Produzir variedade (P2):** 1 variante de casa/mercearia/fazenda, 2–3 silhuetas de SIM por fase da vida.
4. **Polimento opcional:** placa de preferência, contorno de seleção, luzes de veículos, ícones de recurso, portal exterior, tanque de combustível.

---

## 8. Método, limites e conformidade

- **Método:** inventário de arquivos e manifestos; leitura das seções V1 da SPEC; leitura seletiva do código que instancia cenas, desenha superfícies, liga estados e monta a UI. Não houve execução do Godot, captura de tela ou teste de jogabilidade nesta análise.
- **Não alterei** assets, código, `docs/SPEC.md`, `docs/spec/*.md` ou qualquer documento canônico. O único arquivo criado é este documento em `docs/exploration/asset-gaps-v1.md`; o recorte original também existe como artefato de verificação.
- **Não tratei** o briefing exploratório como fonte de requisito; as citações de requisito são da SPEC.
- **Não julguei** estética, escala final ou qualidade de modelagem: o `Assets/V1/README.md` registra que estética, medidas e marcadores ainda são propostas pendentes de revisão humana.
- **Não confundi** sistema ausente com asset ausente: por exemplo, chuva, ônibus, ponte e interior de prédio não estão na V1 e não geram lacuna de arte.
- **P0 = 0** é uma conclusão sobre o conjunto atual: há arte suficiente para uma partida jogável. As lacunas P1 são de legibilidade, não de bloqueio.

---

### Arquivos de referência consultados

- `docs/SPEC.md` — estado atual, recorte V1, catálogo, decisões delegadas.
- `docs/spec/visao-geral.md` — conceito, cidade viva, dia/noite, chuva, alertas, variantes.
- `docs/spec/construcao.md` — prévia, obras, canteiro, pavimentação, demolição.
- `docs/spec/materiais-logistica.md` — caminhões reconhecíveis, Pátio, bootstrap, combustível.
- `docs/spec/transporte-circulacao.md` — vias, calçadas, semáforos, preferência, vagas, conexão externa.
- `docs/spec/populacao.md` — ciclo de vida, seleção individual, migração.
- `docs/spec/servicos.md` — água, esgoto, energia e estados operacionais.
- `docs/spec/economia.md` — agricultura e atividade econômica.
- `docs/spec/mundo-tempo-saves.md` — terreno, vegetação, lago, calendário.
- `src/IndexCities.Godot/Assets/V1/manifest.v1.json`, `Assets/V1/README.md`, `Assets/Construction/manifest.construction.json`.
- `src/IndexCities.Godot/CityWorld.cs`, `CityWorld.Construction.cs`, `CityWorld.RoadSurface.cs`, `CityWorld.ProblemAlerts.cs`, `CityWorld.Lake.cs`, `CityWorld.WaterIntakeRange.cs`, `Main.cs`, `Main.UI.cs`, `Main.Preview.cs`, `Main.MenuCards.cs`.
