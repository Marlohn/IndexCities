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

### Identidade, presença física e representação visual

Entidades da simulação devem usar identidades próprias, como `CitizenId`, `CompanyId`, `BuildingId` e `VehicleId`, em vez de referências para Nodes.

Devem ser separados quatro conceitos:

1. **existência lógica** — a entidade existe, tem identidade e estado persistente;
2. **estado físico da simulação** — quando aplicável, a simulação sabe onde ela está, por onde se desloca, o que ocupa e quais consequências físicas produz;
3. **computação ativa** — a entidade ou sistema só precisa consumir CPU quando houver trabalho relevante; scheduler, eventos e estados inativos podem evitar updates inúteis;
4. **representação visual** — Node, Node3D, MultiMesh, RenderingServer ou outra técnica usada apenas para mostrar aquele estado.

A apresentação pode manter um mapeamento entre um ID da simulação e sua representação visual.

Isso permite que uma entidade:

- exista sem estar renderizada;
- continue ocupando espaço, viajando ou produzindo consequências físicas sem possuir um Node ativo;
- seja descarregada visualmente sem desaparecer da cidade;
- seja testada headless;
- seja salva sem serializar objetos Godot;
- mude de representação visual sem mudar sua identidade.

**Um Node/Node3D nunca é a entidade autoritativa da simulação; é apenas uma possível representação dela.**

### Estado e regras

O estado autoritativo de gameplay vive na simulação.

Para mobilidade, isso inclui as informações necessárias para que viagens, filas, congestionamento, estacionamento, carga/descarga, chegada ao trabalho e demais efeitos continuem causalmente corretos mesmo quando a câmera não está observando a área. O nível interno exato de detalhe pode variar por algoritmo e performance, mas o resultado causal não pode depender da visibilidade.

A apresentação **não pode** ser a fonte de verdade de dinheiro, estoque, ocupação, emprego, velocidade lógica, produção, saúde de empresa ou qualquer outro valor que altere resultado de gameplay.

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

### Scheduler de simulação e frequências diferentes

O núcleo terá um **scheduler de simulação**: sistemas não devem assumir que todos precisam executar na mesma frequência.

A arquitetura deve permitir, por exemplo, que mobilidade local, decisões de cidadãos, economia, serviços e estatísticas sejam atualizados em cadências diferentes, sempre com semântica explícita.

Regras:

- não chamar indiscriminadamente `Update()` em toda entidade a cada frame/tick;
- sistemas registram trabalho por necessidade/cadência, em vez de cada entidade possuir um loop autônomo obrigatório;
- entidades ou subsistemas sem trabalho relevante podem ficar inativos até um evento, deadline ou mudança de estado acordá-los;
- trabalho caro pode ser distribuído entre ticks quando a resposta não precisa ser instantânea;
- frequências concretas continuam sendo parâmetros de benchmark, não números fixados agora;
- ordem de atualização e dependências entre sistemas precisam ser explícitas para preservar causalidade e reprodutibilidade.

O scheduler não é um framework genérico de jobs neste momento. É apenas a responsabilidade explícita de decidir **o que atualiza, quando e com qual orçamento**.

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

**Ausência visual não significa ausência na simulação. Culling visual não autoriza culling causal.**

Um cidadão ou veículo fora da câmera pode continuar existindo, deslocando-se e produzindo consequências reais sem manter um `Node3D` ativo.

Quando um agente está em deslocamento físico, a simulação deve conseguir determinar seu estado espacial relevante independentemente de existir representação visual ativa. A apresentação apenas materializa esse estado quando necessário.

Quando a câmera passa a mostrar uma área, a apresentação deve refletir o estado real daquela área. Um congestionamento real não pode aparecer como uma avenida vazia apenas porque seus veículos estavam anteriormente fora da câmera.

A otimização deve acontecer em **como representar** o estado — pooling, instancing, MultiMesh, RenderingServer, LOD ou outras técnicas adequadas — e não em falsificar o estado da simulação.

Essa separação é particularmente importante para a escala pretendida do IndexCities.

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

A estratégia segue as fronteiras arquiteturais.

### Simulation Core

Maior concentração de testes automatizados.

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

### Godot / integração

Testar somente onde a engine é parte do comportamento:

- conversão de coordenadas;
- integração de cena;
- picking/input;
- carregamento de assets;
- UI crítica;
- integração entre host e núcleo.

Não duplicar no Godot testes de regra já cobertos no núcleo.

### Testes de arquitetura

Quando houver projetos/assemblies reais, vale criar guardrails baratos que impeçam o núcleo de ganhar dependência de `Godot.*`.

Não adotar cobertura percentual como objetivo.

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

## 13. Determinismo e aleatoriedade

A simulação deve favorecer comportamento reproduzível.

- aleatoriedade entra por uma fonte explícita;
- seeds devem poder ser controladas em testes e benchmarks;
- sistemas não devem usar aleatoriedade global escondida;
- um bug deve poder ser reproduzido a partir de estado + configuração + seed sempre que praticável.

Determinismo bit-a-bit entre todas as plataformas **não é requisito atual**. O objetivo imediato é diagnóstico e teste reproduzível.

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
