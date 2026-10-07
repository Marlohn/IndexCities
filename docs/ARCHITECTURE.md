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

### Identidade

Entidades da simulação devem usar identidades próprias, como `CitizenId`, `CompanyId`, `BuildingId` e `VehicleId`, em vez de referências para Nodes.

A apresentação pode manter um mapeamento entre um ID da simulação e sua representação visual.

Isso permite que uma entidade:

- exista sem estar renderizada;
- seja descarregada visualmente sem desaparecer da cidade;
- seja testada headless;
- seja salva sem serializar objetos Godot;
- mude de representação visual sem mudar sua identidade.

### Estado e regras

O estado autoritativo de gameplay vive na simulação.

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

**Ausência visual não significa ausência na simulação.**

Um cidadão fora da câmera pode continuar existindo e evoluindo sem manter um `Node3D` ativo.

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

- formato versionado;
- IDs estáveis;
- ausência de referências a Nodes;
- possibilidade de migração entre versões;
- separação entre save de gameplay e preferências puramente visuais.

O formato concreto — JSON, binário ou outro — **não está decidido** e deve ser escolhido quando houver um primeiro estado real para persistir.

Não criar um framework de migração antes de existir a primeira mudança de schema; apenas não fechar a porta para ele.

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
3. medir CPU, memória, GC e tempo de tick;
4. identificar sistemas quentes;
5. otimizar a representação desses sistemas;
6. considerar arrays contíguos, pools, SoA/ECS ou jobs somente onde os benchmarks justificarem.

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
- frequência dos ticks de cada sistema;
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

### Princípios gerais

- Game Programming Patterns — Decoupling:
  https://gameprogrammingpatterns.com/decoupling-patterns.html
- Game Programming Patterns — Event Queue:
  https://gameprogrammingpatterns.com/event-queue.html
- Game Programming Patterns — Data Locality:
  https://gameprogrammingpatterns.com/data-locality.html
- Martin Fowler — Linking Modular Architecture to Development Teams:
  https://martinfowler.com/articles/linking-modular-arch.html
- Microsoft — Dependency inversion / explicit dependencies:
  https://learn.microsoft.com/en-gb/dotnet/architecture/modern-web-apps-azure/architectural-principles

A conclusão comum útil para o IndexCities é: **fronteiras fortes reduzem o impacto de mudanças, mas abstração e distribuição têm custo.** Portanto, separar aquilo que já sabemos que precisa evoluir independentemente — simulação, Godot/apresentação, UI e assets — e manter o restante simples até existir evidência para mais estrutura.
