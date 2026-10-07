# IndexCities — Arquitetura de Software

> Este documento define as decisões técnicas estruturais do IndexCities.
>
> Ele descreve **como o software deve ser organizado** para preservar as decisões de produto da [SPEC](SPEC.md), permitir evolução segura e manter partes importantes do jogo testáveis e substituíveis.
>
> Não é uma segunda SPEC: regras de gameplay continuam pertencendo à `SPEC.md`; pesquisa e alternativas ainda abertas continuam em `EXPLORATION.md`.

## Estado

**Decisão arquitetural inicial.**

O projeto ainda não possui implementação de jogo. Esta arquitetura define fronteiras e direção de dependências antes do primeiro código justamente porque essas escolhas têm alto custo de reversão depois que simulação, apresentação e conteúdo começam a se misturar.

Ela deve continuar pequena. Novas camadas, frameworks, assemblies ou abstrações só entram quando resolverem uma necessidade observável.

---

## Objetivos

A arquitetura deve permitir que:

- regras de simulação sejam desenvolvidas, executadas e testadas sem abrir o Godot;
- apresentação 3D, UI e assets possam evoluir sem alterar regras de gameplay;
- mudanças locais tenham raio de impacto previsível;
- sistemas centrais possam ser benchmarkados e recalibrados isoladamente;
- agentes de IA diferentes possam trabalhar em áreas delimitadas com menor risco de modificar comportamento fora do escopo;
- estado salvo sobreviva à evolução visual e possa receber migrações quando necessário;
- performance possa evoluir de estruturas simples para representações mais data-oriented sem exigir reescrever a interface inteira;
- o Godot continue sendo a engine e o host do jogo, sem se tornar o modelo de domínio da cidade.

O objetivo **não** é maximizar quantidade de camadas, interfaces ou projetos. Modularidade só tem valor quando reduz acoplamento real.

---

## Decisão principal: monólito modular

O IndexCities será inicialmente um **monólito modular**.

Isso significa:

- um único jogo e um único produto executável;
- módulos internos com responsabilidades e dependências explícitas;
- nenhuma divisão em microserviços ou processos separados como requisito arquitetural;
- fronteiras lógicas fortes o suficiente para permitir testes e substituições locais.

Separar processos apenas para "forçar modularidade" adicionaria serialização, IPC, sincronização, falhas distribuídas e dificuldade de debug sem benefício atual. Uma execução headless pode existir no mesmo processo .NET e compartilhar diretamente o núcleo de simulação.

---

## Visão de alto nível

```text
                    ┌──────────────────────────────┐
                    │        Godot Host            │
                    │ input, clock, lifecycle      │
                    └──────────────┬───────────────┘
                                   │ commands
                                   ▼
                    ┌──────────────────────────────┐
                    │   Application / Orchestration│
                    │ use cases, command routing   │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │      Simulation Core         │
                    │ state + rules + systems      │
                    │      PURE C# / .NET          │
                    └──────────────┬───────────────┘
                                   │ state/read models/events
                 ┌─────────────────┼──────────────────┐
                 ▼                 ▼                  ▼
        ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
        │  Presentation  │ │       UI       │ │  Persistence   │
        │ world rendering│ │ HUD/inspection │ │ save/load      │
        │     Godot      │ │     Godot      │ │    adapter     │
        └───────┬────────┘ └────────────────┘ └────────────────┘
                │
                ▼
        ┌────────────────┐
        │ Assets/Content │
        │ scenes, meshes │
        │ audio, visuals │
        └────────────────┘
```

A regra central é simples:

> **A simulação não depende do Godot, da UI, de cenas, meshes, animações ou caminhos de assets.**

O fluxo de dependências deve apontar para o núcleo, nunca do núcleo para a apresentação.

---

## 1. Simulation Core

O **Simulation Core** é a parte que representa a cidade e decide o que acontece nela.

Deve ser uma biblioteca C#/.NET sem dependência de `Godot.*`.

Exemplos de responsabilidades:

- cidadãos, famílias e residências;
- empresas, empregos, caixa, produção e estoque;
- materiais e fluxos econômicos;
- construção e obras;
- serviços públicos;
- mobilidade, viagens e tráfego no nível em que fizer parte da simulação;
- tempo simulado;
- regras, fórmulas, invariantes e transições de estado;
- causas auditáveis dos eventos da simulação.

### O que não entra no núcleo

- `Node`, `Node3D`, `Resource`, `PackedScene` ou qualquer tipo Godot;
- câmera;
- HUD, janelas, tooltips ou menus;
- mesh, textura, material, animação ou áudio;
- input de mouse/teclado;
- nomes de cenas e caminhos `res://...`;
- lógica que exista apenas para produzir um efeito visual.

### O que existe na simulação e o que aparece na tela

Cidadãos, empresas, prédios e veículos devem ter uma identidade própria dentro da simulação, como `CitizenId`, `CompanyId`, `BuildingId` e `VehicleId`. Eles não devem ser identificados por um Node do Godot.

Precisamos separar quatro coisas:

1. **existir no jogo** — por exemplo, João continua sendo o mesmo cidadão ao longo do tempo;
2. **estar em algum lugar da cidade** — quando necessário, a simulação sabe onde João ou um veículo está, por onde está passando e o que está ocupando;
3. **precisar de processamento naquele momento** — nem todo cidadão ou veículo precisa executar trabalho de CPU o tempo todo; o jogo pode atualizar cada sistema somente quando for necessário;
4. **aparecer na tela** — Godot usa Node, Node3D, MultiMesh, RenderingServer ou outra técnica apenas para desenhar aquilo que já existe na simulação.

A parte visual pode ligar o ID de um cidadão, prédio ou veículo à sua imagem na tela.

Com isso:

- algo pode continuar existindo mesmo quando está fora da tela;
- um veículo pode continuar ocupando a rua e causando trânsito sem precisar manter um Node ativo;
- tirar algo da tela não faz esse objeto desaparecer da cidade;
- a simulação pode ser testada sem abrir a parte gráfica do jogo;
- o save não precisa guardar objetos internos do Godot;
- podemos mudar completamente a forma de desenhar algo sem mudar quem ou o que aquilo representa.

**Um Node ou Node3D nunca é o cidadão, empresa, prédio ou veículo real da simulação. Ele é apenas uma forma de mostrar esse objeto na tela.**

### Onde fica a verdade do jogo

A simulação é quem guarda o estado real do jogo.

No trânsito, por exemplo, isso significa manter informação suficiente para que viagens, filas, congestionamento, estacionamento, carga e descarga e chegada ao trabalho continuem corretos mesmo quando a câmera está olhando para outro lugar. A forma interna de calcular isso pode mudar por questão de desempenho, mas o resultado não pode mudar só porque o jogador não está vendo.

A parte visual **não pode** decidir dinheiro, estoque, ocupação, emprego, produção, velocidade real de um veículo ou qualquer outro valor que altere o que acontece no jogo.

Quando uma animação ou efeito visual terminar, isso pode informar o host, mas não deve inventar uma segunda regra paralela.

---

## 2. Application / Orchestration

Entre input e domínio haverá uma camada fina de **aplicação/orquestração**.

Ela não é um "backend". Seu papel é transformar intenções externas em operações coerentes sobre a simulação.

Exemplos:

- `PlaceBuildingCommand`;
- `BuildRoadCommand`;
- `DemolishCommand`;
- pausar/retomar/alterar velocidade;
- carregar uma cidade;
- solicitar uma visão detalhada de uma empresa ou cidadão.

Essa camada:

- valida a forma do pedido e coordena operações;
- chama regras do núcleo;
- devolve resultado, erro ou dados de leitura;
- não contém rendering;
- não deve duplicar regras de domínio.

A princípio, ela pode viver no mesmo assembly do núcleo se isso mantiver o projeto mais simples. A fronteira conceitual importa antes da fronteira física.

---

## 3. Godot Host / Adapter

O Godot é o **host da aplicação** e o adaptador para recursos da engine.

Responsabilidades:

- lifecycle do jogo;
- frame loop e integração com o relógio real;
- captura de input;
- câmera;
- cenas;
- ponte entre o tick da simulação e o frame de apresentação;
- uso de RenderingServer/PhysicsServer quando apropriado;
- carregar conteúdo visual;
- transformar comandos do jogador em comandos da aplicação.

O host pode conhecer o núcleo. O núcleo não conhece o host.

### Tick de simulação != frame de renderização

A simulação não deve assumir que cada `_Process` equivale a um passo lógico.

O relógio do jogo deve permitir:

- passo fixo ou agenda explícita de atualização;
- pause;
- velocidades de jogo;
- execução headless mais rápida que tempo real;
- benchmarks repetíveis.

A política exata de frequência ainda será medida; a separação já deve existir.

### Cada sistema pode atualizar em um ritmo diferente

A simulação terá uma **agenda de atualização** (scheduler): nem tudo precisa ser recalculado na mesma frequência.

Por exemplo, movimento de veículos, decisões de cidadãos, economia, serviços e estatísticas podem ser atualizados em ritmos diferentes.

Regras:

- não executar `Update()` em todos os cidadãos, veículos e empresas a cada frame sem necessidade;
- cada sistema deve trabalhar somente quando houver algo relevante para atualizar;
- partes sem trabalho podem ficar paradas até chegar a hora ou acontecer algo que exija uma atualização;
- trabalhos pesados podem ser divididos entre várias atualizações quando não precisam de resposta imediata;
- a frequência exata de cada sistema será decidida por testes de desempenho;
- a ordem entre sistemas precisa ser clara para que causa e efeito continuem corretos.

Isso não significa criar agora um sistema complexo de tarefas ou threads. Significa apenas controlar **o que precisa ser atualizado, quando e quanto trabalho pode ser feito de uma vez**.

### Threads

Não será definida agora uma arquitetura multithread completa.

A decisão inicial é apenas não prender o estado da simulação à SceneTree. Isso preserva a opção de paralelizar sistemas depois.

A interação com SceneTree e objetos visuais deve permanecer no lado Godot, respeitando as restrições de thread da engine.

---

## 4. Presentation / World View

A **Presentation** transforma estado da simulação em mundo visível.

Exemplos:

- criar/remover representação visual de prédios;
- mostrar cidadãos e veículos que precisam estar visíveis;
- animações;
- LOD visual;
- efeitos;
- sinalização visual de problemas;
- interpolação entre estados lógicos;
- detalhes urbanos decorativos.

Ela pode descartar, agrupar ou simplificar representação visual por distância e performance, desde que isso não altere o estado autoritativo da simulação.

### Regra importante

**Sair da tela não significa sair da simulação.**

Um cidadão ou veículo pode continuar existindo, se deslocando e causando consequências reais mesmo sem um `Node3D` ativo.

Se um cidadão ou veículo está viajando, a simulação precisa continuar sabendo o suficiente sobre onde ele está e o que está acontecendo. A parte visual apenas mostra esse estado quando o jogador olha para aquela área.

Quando a câmera chega a uma região, o que aparece na tela deve combinar com a situação real da simulação. Uma avenida congestionada não pode aparecer vazia só porque antes estava fora da câmera.

Para ganhar desempenho, podemos mudar **como desenhamos** os objetos — usando reutilização de objetos, instâncias, MultiMesh, RenderingServer, LOD ou outras técnicas — mas não podemos falsificar o que está acontecendo na cidade.

Essa separação é especialmente importante para a escala pretendida do IndexCities.

---

## 5. UI

A UI é uma camada de leitura e comando, não dona da cidade.

Ela deve:

- consultar modelos de leitura da simulação;
- mostrar causas reais e dados auditáveis;
- enviar comandos/intenção;
- manter somente estado de interface: seleção, aba aberta, filtros, câmera, preferências visuais etc.

Ela não deve:

- alterar diretamente campos internos de cidadãos, empresas ou prédios;
- recalcular uma versão própria da economia;
- inferir a causa de um problema usando lógica diferente da simulação.

Isso reforça o princípio já definido de causalidade: a explicação exibida precisa vir dos mesmos dados que produziram o resultado.

---

## 6. Assets e conteúdo visual

Assets devem ser substituíveis sem mudar gameplay.

O núcleo não referencia:

- `.tscn`;
- meshes;
- texturas;
- materiais;
- animações;
- sons;
- caminhos de arquivo.

Em vez disso, a simulação trabalha com conceitos semânticos. Exemplo:

```text
BuildingTypeId = "small_house"
VisualId       = "residential.small_house.a"
```

A apresentação resolve `VisualId` para uma cena/mesh/material apropriado.

Trocar a casa por outro modelo, criar skins, melhorar LOD ou reorganizar pastas visuais não deve mudar a regra econômica daquela residência.

### Dados de gameplay x dados de apresentação

Devem ser distinguidos mesmo quando ambos forem editáveis:

**Gameplay:** capacidade, custo, materiais, vagas, produção, consumo, regras.

**Apresentação:** cena, mesh, material, ícone, animação, offsets, VFX, áudio.

Um mesmo arquivo de autoria pode futuramente facilitar edição, mas no runtime a fronteira deve continuar clara.

Godot `Resource` pode ser útil para autoria de conteúdo visual e configuração no editor, mas o núcleo não deve receber um `Resource` como seu modelo de domínio. O adaptador converte os dados necessários para tipos C# próprios.

---

## 7. Módulos internos da simulação

A simulação crescerá por **domínios**, não por uma pasta gigante de "Managers".

Fronteiras prováveis, criadas somente conforme o código surgir:

```text
Simulation/
  Time/
  City/
  Population/
  Economy/
  Construction/
  Mobility/
  Services/
```

Isso é um mapa inicial, não uma obrigação de criar sete assemblies vazios.

### Regra de dependência entre domínios

Preferir:

- IDs e dados explícitos;
- APIs pequenas;
- operações coordenadas por sistemas/orquestração;
- dependências visíveis no construtor ou método.

Evitar:

- singleton global com acesso irrestrito;
- `GameManager` que conhece tudo;
- módulos lendo e alterando estruturas internas uns dos outros;
- event bus global como padrão de comunicação;
- interfaces criadas apenas "porque talvez um dia troquemos a implementação".

Quando comunicação assíncrona/eventos realmente trouxer vantagem, eventos devem carregar dados suficientes e ter escopo claro. Um barramento global não é arquitetura padrão.

### Mobilidade e pathfinding são um subsistema especializado

`Mobility` não deve expor apenas uma função genérica do tipo `FindPath(A, B)` que qualquer agente possa chamar sem controle.

A arquitetura deve permitir uma estratégia hierárquica:

1. **conectividade barata** — saber rapidamente se origem e destino pertencem a regiões conectadas;
2. **grafo macro/coarse** — estimar distância/custo e selecionar corredores sem percorrer toda a malha fina;
3. **rota detalhada** — calcular o caminho fino somente quando ele realmente será usado;
4. **movimento local** — decisões de faixa, avoidance e microcomportamento podem operar em estruturas próprias sem refazer a rota global.

Mudanças de ruas, pontes, cruzamentos e acessos devem invalidar/recalcular somente os derivados afetados sempre que a representação escolhida permitir.

Consultas caras devem passar por fila/orçamento/prioridade quando necessário. Um pico de centenas ou milhares de agentes pedindo rota ao mesmo tempo não pode bloquear o tick inteiro sem limite.

A API exata, algoritmos e uso de NavigationServer continuam em exploração/prototipagem. A decisão arquitetural é preservar essa separação para não confundir **decidir destino**, **estimar custo**, **encontrar rota** e **mover-se pela rota**.

---

## 8. Modelo de comunicação

A fronteira principal terá três tipos de informação:

### Commands

Expressam intenção de alterar o mundo.

Exemplos: construir, demolir, mudar velocidade, definir uma rota/ação quando aplicável.

### Queries / Read models

Expressam leitura.

Exemplos: detalhes de uma empresa, população de um prédio, estoque, causa de falência, dados para overlay.

A UI não precisa receber a estrutura interna inteira da simulação para desenhar uma janela.

### Domain events

Expressam algo que já aconteceu e que outras partes precisam observar.

Exemplos possíveis: empresa encerrou, obra concluiu, cidadão mudou de residência.

Eventos não substituem chamadas diretas simples. Eles serão usados quando houver desacoplamento real a ganhar.

### Estado primário x estado derivado

A simulação deve distinguir:

- **estado autoritativo/primário** — dados que definem o mundo, como dinheiro, estoque, residência, emprego, vias e localização lógica;
- **estado derivado** — índices, agregados, overlays, conectividade, estatísticas, caches e read models calculados a partir do estado primário.

Estado derivado não deve virar uma segunda fonte de verdade.

Quando recalcular um derivado for caro, a implementação pode usar:

- atualização incremental;
- dirty flags;
- buckets;
- caches de curta duração;
- snapshots/read models;
- recomputação em lote.

Essas otimizações só entram quando houver benefício mensurável e precisam ter invalidação centralizada junto às operações que alteram o estado primário.

---

## 9. Conteúdo configurável e regras

O projeto precisa ser data-driven onde isso ajuda calibração, mas "data-driven" não significa colocar toda regra em JSON.

### Bom candidato a dados

- custos;
- capacidades;
- tempos;
- taxas;
- catálogos de tipos;
- parâmetros de balanceamento;
- associação entre tipo semântico e visual.

### Bom candidato a código

- invariantes;
- algoritmos;
- regras com múltiplas relações;
- transações econômicas;
- lógica que precisa de testes fortes;
- comportamento cujo significado ficaria escondido em uma mini-linguagem de configuração.

Configuração deve ser validada ao carregar. Dados inválidos devem falhar cedo e de forma diagnosticável.

---

## 10. Persistência e migrações

O save não deve serializar a SceneTree como estado autoritativo da cidade.

A persistência deve trabalhar sobre um modelo próprio de estado da simulação.

Requisitos arquiteturais:

- formato versionado desde a primeira versão persistida;
- número/identificador de schema explícito no save;
- IDs estáveis;
- ausência de referências a Nodes;
- possibilidade de migração entre versões;
- separação entre save de gameplay e preferências puramente visuais.

O formato concreto — JSON, binário ou outro — **não está decidido** e deve ser escolhido quando houver um primeiro estado real para persistir.

Não criar um framework de migração antes de existir a primeira mudança de schema. Quando o primeiro schema mudar, preferir migrações pequenas e explícitas de versões anteriores para a versão atual, preservando testes com saves antigos relevantes.

---

## 11. Testes

A estratégia segue as fronteiras arquiteturais e deve priorizar **retorno rápido durante o desenvolvimento**.

A regra principal é: **rode o menor conjunto de testes que realmente prova a mudança; aumente o alcance quando o risco aumentar.**

Isso evita que toda pequena alteração obrigue a executar a suíte inteira.

### Testes rápidos do Simulation Core

A maior parte dos testes automatizados deve ficar no núcleo da simulação, porque são baratos de executar e protegem as regras mais importantes.

Devem proteger principalmente:

- invariantes econômicas;
- transferências de dinheiro e estoque;
- construção e consumo de materiais;
- empregos e capacidade;
- causalidade de falha;
- transições de estado;
- regras que já causaram regressão;
- cenários de simulação reproduzíveis.

Esses testes devem rodar com `dotnet test`, sem editor Godot.

### Testes de integração

Testes de integração verificam se duas ou mais partes realmente funcionam juntas. Devem existir principalmente nas fronteiras onde há risco real, por exemplo:

- Simulation Core ↔ camada de aplicação;
- persistência ↔ estado da simulação;
- Godot ↔ comandos e dados vindos da simulação.

Eles devem ser menos numerosos que os testes rápidos do núcleo, porque custam mais tempo e normalmente precisam de mais infraestrutura.

### Godot / testes mais amplos

Testar com Godot somente onde a engine é parte do comportamento:

- conversão de coordenadas;
- integração de cena;
- picking/input;
- carregamento de assets;
- UI crítica;
- integração entre host e núcleo.

Não duplicar no Godot testes de regra já cobertos no núcleo.

### Ordem prática de execução

Durante desenvolvimento:

1. executar primeiro os testes da área alterada;
2. se a mudança cruzar uma fronteira, executar também os testes de integração daquela fronteira;
3. executar testes Godot somente quando a alteração tocar comportamento que depende da engine;
4. executar a suíte completa em momentos de maior confiança necessária, como integração de mudanças amplas, marcos importantes e antes de releases.

Quando a suíte crescer, os testes devem poder ser filtrados por projeto, domínio e categoria usando os recursos normais do `dotnet test`.

A automação de CI pode usar caminhos alterados para evitar iniciar jobs totalmente irrelevantes, desde que mudanças em contratos compartilhados continuem disparando os testes dependentes.

### Performance não é teste funcional

Benchmarks de desempenho devem ficar separados dos testes funcionais normais.

Eles servem para medir, por exemplo:

- tempo de tick;
- quantidade de cidadãos/veículos suportados;
- custo de pathfinding;
- alocações e GC;
- tempo de save/load.

Um benchmark não deve tornar cada edição lenta. Rode benchmarks quando houver mudança de algoritmo, estrutura de dados, escala ou quando uma regressão de desempenho for suspeita.

### Testes de arquitetura

Quando houver projetos/assemblies reais, vale criar guardrails baratos que impeçam o núcleo de ganhar dependência de `Godot.*`.

Não adotar cobertura percentual como objetivo.

Também não duplicar o mesmo comportamento em várias camadas sem necessidade. Um teste novo deve existir porque protege uma regra, uma integração importante ou uma regressão concreta.

### Observabilidade de simulação

Performance e causalidade precisam ser inspecionáveis por subsistema.

A instrumentação deve poder medir, quando o sistema existir:

- tempo por tick e por sistema;
- quantidade de entidades processadas/ativas;
- filas e atrasos do scheduler;
- solicitações, falhas e tempo de pathfinding;
- recomputações de caches/índices;
- alocações e pressão de GC;
- tamanhos relevantes de coleções;
- causas de fallback, retry ou backlog.

A instrumentação pode começar simples e só crescer com os sistemas reais. O objetivo é evitar descobrir tarde que "o jogo está lento" sem saber qual mecanismo criou o custo.

---

## 12. Execução headless, benchmarks e agentes de IA

O núcleo deve poder ser criado e avançado sem renderer.

Isso permite:

- `dotnet test`;
- benchmarks de 10k/25k/50k/... agentes;
- simular dias ou anos rapidamente;
- reproduzir bugs a partir de seed/estado;
- comparar fórmulas;
- agentes de IA alterarem e validarem regras sem abrir o editor;
- ferramentas futuras de balanceamento.

**Não é necessário criar agora um servidor ou IPC.**

Quando surgir o primeiro benchmark ou ferramenta que precise de entrada executável, pode ser criado um pequeno `IndexCities.Headless`/CLI que referencia diretamente o núcleo.

A fronteira headless é uma capacidade arquitetural; não precisa virar um subsistema distribuído.

---

## 13. Reproduzir cidades e bugs

A simulação deve favorecer comportamento reproduzível.

### Seed do mapa

A seed define o ponto de partida reproduzível do mapa e da geração inicial.

Usar a mesma seed deve permitir gerar novamente a mesma cidade-base, desde que a versão e as configurações relevantes também sejam compatíveis.

**A seed sozinha não reproduz uma cidade horas depois de gameplay.** Depois que o jogador constrói, cidadãos tomam decisões e números aleatórios avançam, o estado mudou.

### Pacote de reprodução de bug

Quando precisarmos reproduzir um bug ocorrido durante uma cidade em andamento, o formato desejado é guardar um pequeno conjunto de informações:

- versão/build do jogo;
- seed inicial;
- configurações que alteram a simulação;
- um save ou checkpoint próximo do problema;
- estado dos geradores de números aleatórios quando necessário;
- sequência de comandos/ações desde o checkpoint até o bug.

Assim podemos carregar um ponto conhecido e repetir os mesmos passos até a falha.

Não é necessário registrar para sempre cada frame ou cada detalhe da cidade. O sistema de reprodução deve ser proporcional ao problema e pode evoluir quando houver bugs reais que justifiquem mais informação.

### Aleatoriedade

- aleatoriedade entra por fontes explícitas;
- seeds devem poder ser controladas em testes e benchmarks;
- evitar aleatoriedade global escondida;
- quando um sistema usar um gerador próprio, seu estado deve poder ser salvo/restaurado se isso for necessário para reprodução.

O objetivo é conseguir repetir cenários e diagnosticar bugs. Determinismo bit-a-bit entre todas as plataformas **não é requisito atual**.

---

## 14. Performance: simples primeiro, dados primeiro quando necessário

O IndexCities pretende simular muitos cidadãos, empresas e veículos. Isso torna layout de memória, frequência de atualização e pathfinding riscos reais.

Mesmo assim, não será adotado ECS ou uma arquitetura data-oriented completa por antecipação.

Estratégia:

1. manter simulação fora de Nodes;
2. representar entidades por IDs e estruturas C# controladas pelo núcleo;
3. evitar atualizar entidades inativas ou trabalho cujo resultado não é necessário naquele tick;
4. medir CPU, memória, GC, cache behavior quando possível e tempo por sistema/tick;
5. identificar sistemas quentes;
6. otimizar algoritmos e frequência antes de simplesmente adicionar threads;
7. melhorar localidade/representação dos hotspots;
8. considerar arrays contíguos, pools, hot/cold split, SoA/ECS ou jobs somente onde os benchmarks justificarem.

A fronteira entre simulação e apresentação torna essa evolução possível sem reescrever UI e assets.

---

## 15. Estrutura inicial de solução

Quando começar a implementação, a estrutura mínima recomendada é:

```text
IndexCities/
  src/
    IndexCities.Simulation/
      # C# puro: estado, regras, sistemas, comandos/queries essenciais

    IndexCities.Godot/
      project.godot
      # host, cenas, apresentação, UI, adapters, conteúdo Godot

  tests/
    IndexCities.Simulation.Tests/

  docs/
    SPEC.md
    EXPLORATION.md
    ARCHITECTURE.md
```

Não criar projetos separados para `Application`, `Persistence`, `UI`, `Rendering`, cada domínio etc. antes de haver código suficiente para a fronteira pagar seu custo.

O Godot suporta workspace C# com múltiplos projetos; portanto a separação física do núcleo em uma biblioteca .NET é compatível com o stack escolhido.

### Evolução possível

Somente quando houver necessidade:

```text
src/
  IndexCities.Simulation/
  IndexCities.Headless/       # benchmarks/tools
  IndexCities.Godot/
```

Outros assemblies devem exigir justificativa concreta.

---

## 16. Regras de dependência

### Permitido

```text
Godot Host ───────► Application/Simulation
Presentation ─────► Simulation read models
UI ───────────────► Application + Simulation read models
Persistence ──────► Simulation state/contracts
Headless ─────────► Application/Simulation
```

### Proibido

```text
Simulation ─X─► Godot
Simulation ─X─► UI
Simulation ─X─► scenes/assets
Simulation ─X─► camera/input
Assets ─────X─► alterar regra de gameplay por efeito colateral
UI ─────────X─► mutar estado interno da simulação diretamente
```

---

## 17. Critério para uma nova fronteira

Antes de criar interface, projeto, serviço ou camada, responder:

1. Existe hoje mais de uma implementação relevante?
2. Precisamos testar uma parte sem a outra?
3. A dependência atual impede trabalho paralelo ou causa regressões?
4. A fronteira protege uma regra importante?
5. Há um custo de performance ou plataforma que exige separação?

Se a resposta for "não" para tudo, preferir código direto.

---

## 18. Decisões explicitamente adiadas

Ainda não estão decididos:

- ECS;
- framework de dependency injection;
- event bus global;
- arquitetura multithread;
- formato de save;
- formato de configuração de gameplay;
- estratégia exata de snapshots/read models;
- frequências concretas do scheduler;
- algoritmo e representação definitivos de pathfinding;
- job system;
- modding API;
- scripting externo;
- divisão de cada domínio em assembly próprio.

Esses assuntos devem ser resolvidos por protótipo, profiling ou necessidade real.

---

## Evidências e referências

Esta arquitetura não foi escolhida apenas por preferência interna.

### Godot

A documentação oficial do Godot registra que:

- a SceneTree ativa não é thread-safe;
- Servers são apropriados para controlar volumes muito grandes de instâncias;
- Resources são containers de dados da engine;
- projetos C# podem usar cenários com múltiplos `.csproj`.

Referências:

- https://docs.godotengine.org/en/4.6/tutorials/performance/thread_safe_apis.html
- https://docs.godotengine.org/en/stable/engine_details/architecture/godot_architecture_diagram.html
- https://docs.godotengine.org/en/4.5/tutorials/scripting/resources.html
- https://docs.godotengine.org/en/stable/classes/class_projectsettings.html
- https://docs.godotengine.org/en/4.4/tutorials/navigation/navigation_optimizing_performance.html
- https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationservers.html

### City builders e simulações abertas

**Micropolis / SimCity:** versões modernas separam explicitamente o engine de simulação da interface; o núcleo pode rodar headless e ser conectado a frontends diferentes.

- https://github.com/SimHacker/MicropolisCore
- https://github.com/dheid/micropolis

**Citybound:** city simulation focada em detalhes microscópicos que trata arquitetura e modelo de atores como parte do problema de escala.

- https://github.com/citybound/citybound

**Loopolis:** projeto C# + Godot 4 recente que usa um core C# puro sem dependências Godot, testes NUnit e execução headless. É evidência de viabilidade da fronteira, não um template a copiar; em particular, o IndexCities não adota seu IPC por arquivos.

- https://github.com/codewithagents/loopolis-city-builder

**Cimulity:** city builder recente com fluxo explícito input → commands → core → render, reforçando a utilidade de manter rendering como consumidor do estado, não dono dele.

- https://github.com/zeikar/cimulity

**SimCity / GlassBox:** o design separava Resources, Units, Maps e regras, e mantinha agentes móveis deliberadamente simples para suportar grandes quantidades. É evidência útil de que "entidade persistente" não precisa significar lógica pesada executada por objeto em todo tick.

- https://www.andrewwillmott.com/talks/inside-glassbox
- https://www.gamedeveloper.com/design/gdc-2012-breaking-down-em-simcity-em-s-glassbox-engine

**Banished:** o pós-mortem de pathfinding mostra que otimizar A* não bastava. O jogo separou conectividade, grafo macro e caminho detalhado; decisões de distância passaram a usar representação coarse muito mais barata. Esse caso sustenta tratar mobilidade como arquitetura hierárquica, não como chamada de A* por agente.

- https://shiningrocksoftware.com/2013-11-21-more-bugs-pathfinding-problems/
- https://banished-wiki.com/wiki/Pathfinding

**Factorio:** os devlogs documentam repetidamente que atualizar milhares de entidades a cada tick, baixa localidade de memória e multithreading ingênuo criam gargalos. Otimizações eficazes incluem colocar sistemas para "dormir", buckets de atualização, mover lógica para managers especializados e paralelizar apenas trabalhos com dependências controladas.

- https://www.factorio.com/blog/post/fff-421
- https://www.factorio.com/blog/post/fff-324
- https://www.factorio.com/blog/post/fff-204
- https://www.factorio.com/blog/post/fff-151
- https://www.factorio.com/blog/post/fff-215
- https://www.factorio.com/blog/post/fff-364
- https://www.factorio.com/blog/post/fff-415

**OpenTTD:** mantém estado de simulação determinístico, savegames versionados e compatibilidade explícita por versão. Serve como evidência de que versionamento de estado e reprodutibilidade precisam ser preocupações de base em simuladores longevos.

- https://github.com/OpenTTD/OpenTTD/blob/master/docs/desync.md
- https://docs.openttd.org/source/d6/dd4/saveload_8h_source

### Testes, SDD e reprodução

- GitHub Spec Kit — conceito de Spec-Driven Development:
  https://github.com/github/spec-kit/blob/main/docs/concepts/sdd.md
- Microsoft .NET — filtros para executar testes selecionados:
  https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests
- Martin Fowler — Practical Test Pyramid:
  https://martinfowler.com/articles/practical-test-pyramid.html
- GitHub Actions — filtros por caminhos alterados:
  https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow
- Godot — geração aleatória, seed e estado do gerador:
  https://docs.godotengine.org/en/4.7/tutorials/math/random_number_generation.html

### Princípios gerais

- Game Programming Patterns — Decoupling:
  https://gameprogrammingpatterns.com/decoupling-patterns.html
- Game Programming Patterns — Event Queue:
  https://gameprogrammingpatterns.com/event-queue.html
- Game Programming Patterns — Data Locality:
  https://gameprogrammingpatterns.com/data-locality.html
- Game Programming Patterns — Dirty Flag:
  https://gameprogrammingpatterns.com/dirty-flag.html
- Game Programming Patterns — Update Method:
  https://gameprogrammingpatterns.com/update-method.html
- Martin Fowler — Linking Modular Architecture to Development Teams:
  https://martinfowler.com/articles/linking-modular-arch.html
- Microsoft — Dependency inversion / explicit dependencies:
  https://learn.microsoft.com/en-gb/dotnet/architecture/modern-web-apps-azure/architectural-principles

A conclusão comum útil para o IndexCities é: **fronteiras fortes reduzem o impacto de mudanças, mas abstração e distribuição têm custo.** Portanto, separar aquilo que já sabemos que precisa evoluir independentemente — simulação, Godot/apresentação, UI e assets — e manter o restante simples até existir evidência para mais estrutura.
