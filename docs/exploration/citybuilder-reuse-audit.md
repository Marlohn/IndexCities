# CityBuilder → IndexCities: mapa de reaproveitamento

> **Revisão humana: PENDENTE.** Pesquisa e recomendações produzidas por IA em 2026-10-11, ainda não revisadas integralmente pelo responsável.
>
> **Documento de exploração; não é fonte de verdade.** Nenhuma solução, número, arquitetura ou regra do CityBuilder fica aprovada para o IndexCities por aparecer aqui. Antes de implementar, respeitar [SPEC](../SPEC.md), [ARCHITECTURE](../ARCHITECTURE.md) e [AGENTS](../../AGENTS.md).
>
> **Escopo da auditoria:** inspeção estática via GitHub da árvore completa da branch principal do CityBuilder, dos arquivos-fonte principais por subsistema, dos testes representativos, dos catálogos de dados/assets, da documentação e de branches/issues históricas relevantes. **Não houve execução do jogo, compilação, medição independente de FPS ou verificação funcional de cada teste/arquivo.** O inventário cobre os componentes encontrados; não pretende certificar cada linha nem garantir que todos estejam corretos.

## Resposta curta

**Vale reaproveitar o conhecimento e partes da lógica, não portar o jogo inteiro.** O CityBuilder contém soluções concretas e investigadas para identificadores estáveis, números aleatórios reproduzíveis, índices de vagas, grafo de ruas, A*, filas com orçamento por atualização, histórico de pessoas, comandos, relatórios, cenários automáticos e depuração. Tudo isso pode economizar pesquisa, tentativas e tokens ao construir o IndexCities em **C# com Godot**.

A maior armadilha é confundir *já existe código* com *atende ao produto*. Grande parte das regras de tempo, dinheiro, construção, serviços, empresas, zoneamento e deslocamento do CityBuilder é mais simples ou incompatível com a SPEC atual. O ideal é pedir a uma IA que **consulte somente o arquivo antigo indicado, extraia o algoritmo ou o teste relevante e implemente o comportamento aprovado do IndexCities**.

**Não copiar:** solução completa em TypeScript/React/Babylon, calendário antigo, economia que cria receitas sem contrapartida, crescimento residencial por zoneamento automático, serviços simplificados ou imagens/GLBs sem conferir proveniência.

## 1. Qual versão foi examinada?

- **CityBuilder (main):** [commit 32fdf298](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038). Árvore Git completa com **356 arquivos versionados**, incluindo **187 .ts, 8 .tsx e 83 .glb**, além de configuração, dados, documentação, automações e testes. O projeto é um monorepo npm com TypeScript, Babylon.js, React, Vite, Web Worker, Vitest e Playwright.
- **IndexCities (main, referência da leitura):** [árvore e documentação](https://github.com/Marlohn/IndexCities/tree/main). No momento da inspeção, possui documentação de produto/arquitetura/exploração, **sem implementação do jogo**.
- **Experimento gráfico não incorporado à main:** [branch perf/web-scale-gate, commit 038b506](https://github.com/Marlohn/CityBuilder/tree/038b506a21d0ffcd1e5726601b8497f79c2f40dc). Contém lógica de chunks e medições importantes ausentes da main. Foi preservado como experimento.
- **Experimentos de arte:** [POC v3, commit 91ee65e](https://github.com/Marlohn/CityBuilder/tree/91ee65e5d200403371f4b01662cd878dd23c94c9) e [POC v2, commit 1024b84](https://github.com/Marlohn/CityBuilder/tree/1024b8446cd09ac5402c1beb534f55ef4fe53fc9). São referências de investigação visual, não implementações aprovadas para o IndexCities.
- **Histórico decisivo:** [#319, POC visual v4](https://github.com/Marlohn/CityBuilder/issues/319), [#320, avaliação de escala Web](https://github.com/Marlohn/CityBuilder/issues/320), [#321, solução experimental de culling](https://github.com/Marlohn/CityBuilder/issues/321) e [#322, decisão de migrar aquele projeto para Godot/C#](https://github.com/Marlohn/CityBuilder/issues/322). A decisão da #322 dizia respeito **ao CityBuilder**, não cria requisito do IndexCities.

**Como consultar sem gastar contexto:** comece pela matriz da seção 3, abra **um ou dois arquivos** da mesma linha em um commit fixo, leia o teste associado e compare com a seção da SPEC relevante. Evite despejar todo o CityBuilder no prompt.

## 2. Diferenças que limitam o reaproveitamento

| Assunto | O CityBuilder implementava | O IndexCities exige/decidiu | Consequência |
| --- | --- | --- | --- |
| Plataforma | TypeScript, Babylon.js, React e navegador | Godot + C#, núcleo .NET puro | Aproveitar lógica/conceito; reescrever integração e UI |
| Relógio | Um dia jogável representa **um ano de vida**; 120 segundos reais por dia em 1x | Calendário civil real, dia de 24h, ano de 365/366 dias, acontecimentos efetivamente processados | **Não portar scheduler, envelhecimento, custos por ano ou replays sem redesenho** |
| Construção | Zoneamento e edifícios aparecendo em lotes; construção baseada em tempo/custo monetário | Jogador posiciona edifícios individualmente; obras demandam materiais, trabalhadores, entregas, acesso, caixa e tempo | Reutilizar validação de comandos, não a regra de crescimento |
| Economia | Receitas municipais por morador e custeio simplificado | Dinheiro global conservado entre Caixa, SIMs, empresas e Reserva Global; mercadorias e pagamentos físicos | **Não importar Treasury/EconomySystem como motor financeiro** |
| Empresas | Comércio/indústria representados principalmente por emprego e demanda de zona | Empresas com donos, caixa, estoque, compras, produção, vendas, fretes, imóveis e possível falência | Reconstruir modelo econômico |
| Serviços | Água, luz e esgoto ligados a componentes viários e capacidade por “pessoas equivalentes” | Consequências de fornecimento, cobrança, operação, restrições reais e causalidade da SPEC | Conectividade inspira; modelo de rede/serviço não é suficiente |
| Mobilidade | Grade de ruas rígida, A* e rotinas carro/a pé, apresentação visual temporalmente alongada | Geometria/encaixe flexível, vias e acessos coerentes, SIMs e veículos físicos, ônibus/carga e tempo causal | Portar ideias de grafo e rotas, **não assumir geometria e tempos iguais** |
| Partida | Estrada pré-instalada, expansão por zona/bot | Mapa vazio; primeiro o jogador confirma projeto da Prefeitura | Não importar bootstrap de cidade |
| Apresentação | GLBs prontos e alguns meshes produzidos no Babylon | Low-poly isométrico Godot, com produção procedural de assets em exploração; geometria pode ser reutilizada | Avaliar modelos caso a caso; não herdar pipeline Babylon |
| Modos | Sandbox com dinheiro infinito disponível | Foco atual no **Convencional**; Criativo adiado | Não importar sandbox/arquitetura extra |
| Jogo para públicos distintos | Painéis tradicionais de React | Gestão adulta **e** construção fluida para criança que ainda não lê; leitura visual em camadas | Usar ideias de telas como referência, não transplantar interface |

**Regra prática:** qualquer sugestão abaixo está condicionada à SPEC, inclusive quando o código antigo parece pronto.

## 3. Matriz dos componentes: o que existe, como foi feito e o que aproveitar

Legenda: **A = estudar no início da implementação correspondente**; **B = consultar quando surgir a necessidade**; **C = só referência/contraste**. “Adaptar” significa **reimplementação em C#**, não cópia direta de dependências TypeScript.

| Área e arquivos do CityBuilder (todos sob packages/) | Como foi feito | Uso no IndexCities | Prioridade |
| --- | --- | --- | --- |
| **sim/src/core/rng.ts**; **core/rng.test.ts** | RNG sfc32 por seed, com fluxos nomeados e estado recuperável; mudanças num subsistema não deslocam necessariamente sorteios de outro | **Adaptar algoritmo e testes** de determinismo em C#; definir política de streams coerente | A |
| **sim/src/core/indexedSet.ts**, **core/growable.ts** | Conjunto de IDs com vetor de posições, inclusão/remoção/amostragem em O(1); arrays contíguos crescem por capacidade | **Adaptar pequena estrutura** para candidatos de vagas, veículos ou filas, se medição/uso justificar; não impor SoA a todo domínio | A |
| **sim/src/people/population.ts**, **people/households.ts**, **world/buildings.ts**, **traffic/vehicles.ts** | IDs duráveis; dados por campo em TypedArrays; mortos, emigrados e demolidos permanecem rastreáveis; vínculos pessoa–família–edifício e veículo–dono | **Padrões de identidade e layout de dados**; desenhar estruturas novas considerando propriedade, dinheiro, contratos, localização e estado da SPEC | A |
| **sim/src/people/actions.ts** | Ações centralizam mutações e mantêm índices de casa/emprego/escola/família sincronizados; cada mudança gera evento | **Adaptar padrão de operação atômica** em serviços do núcleo. Complementar com lançamentos e movimentações reais de dinheiro/estoques | A |
| **sim/src/people/events.ts**, **view/people.ts** | Histórico compacto por pessoa via encadeamento ao último evento da mesma pessoa; leitura não percorre todo histórico global | **Excelente referência** para histórico pessoal consultável, eventos importantes/cotidianos e vínculo causal; ajustar retenção e consultas | A |
| **sim/src/debug/invariants.ts**, **core/log.ts** | Inspeções de estado (pessoa, endereço, carro, rota) com IDs e motivos; logs JSON por tick/sistema/ação/causa | **Adaptar invariantes e formato de diagnóstico**, ampliando para conservação global de dinheiro, estoques, propriedades, entregas e tempo | A |
| **sim/src/core/perf.ts**, **city.ts** | Contadores de unidades de trabalho independentes do hardware; atualização escalonada de população em slots e índices de ano | **Adaptar medição de rotas, eventos e agentes por tick**, distinguindo trabalho determinístico de duração real; redesenhar calendário | A |
| **sim/src/sim.ts**, **game.ts** | Motor montado por sistemas numa ordem de execução conhecida, comandos enfileirados e estado único; o mesmo motor serve CLI/web/testes | **Referência de organização do Core**, mantendo monólito modular C# já decidido, sem copiar toda separação em packages | A |
| **sim/src/world/world.ts**, **world/roadNetwork.ts** | Arrays de mapa, números de versão e flood-fill de componentes conectados, recalculados só após mudança de rua; saída externa identificável | **Adaptar versionamento e conectividade**, com novo modelo geométrico e redes diferentes onde a SPEC exigir | A |
| **sim/src/routing/pathfinder.ts**, **routing/pathfinder.test.ts** | A* com custos inteiros, heap binária, arrays reutilizados e critério de desempate fixo | **Portar algoritmo/testes conceituais** para C#, começando por grafo/semântica de vias aprovadas; medir antes de adicionar complexidade | A |
| **sim/src/routing/routeService.ts** | Cache de rotas invalidado por `roadVersion`; pedidos do tick T publicados em T+1, em ordem fixa, mesmo com cache | **Adaptar cache e publicação determinística**, útil se roteamento for paralelo; T+1 é mecanismo técnico, não regra obrigatória do IndexCities | A |
| **sim/src/markets/markets.ts**, **systems/matching.ts**, **systems/hospitalAdmission.ts** | Conjuntos só com vagas disponíveis; busca por amostra e proximidade; filas circulares processadas com orçamento e retomadas quando mercado muda | **Adaptar índices de capacidade e processamento incremental** para vagas reais, escola, emprego, saúde, moradia; não adotar seleção por sorteio sem avaliar causalidade | A |
| **sim/src/save/replay.ts**, **save/commandCompat.ts** | Save do jogo antigo guarda seed, overrides, comandos e tick; versão/migração por degraus; erros explicam comando inválido | **Aproveitar validação/versionamento e casos de teste**; não escolher replay como único save sem avaliar desempenho e estado C# da SPEC | A |
| **contract/src/commands.ts**, **contract/src/view.ts**, **contract/src/messages.ts** | Comandos de alteração separados das visões somente leitura; validação via Zod; mensagens para Worker | **Aplicar padrão Commands + Queries/Read Models em C#** já previsto pela arquitetura; dispensar Worker/Zod/IPC não necessários | A |
| **sim/src/view/stats.ts**, **view/report.ts**, **view/mapViews.ts**, **view/people.ts** | Relatórios, censo e snapshots para CLI/tela sem acesso a estruturas mutáveis | **Adaptar relatórios headless e leitura em camadas**, com custos/receitas reais e motivo de sucesso/falha | A |
| **bots/src/mayor.ts**, **bots/src/scenario.ts**, **cli/src/main.ts**, **cli/src/run.ts** | Prefeito automático emite comandos normais; cenários YAML agendam comandos; CLI roda seed, pessoa, relatórios, replay e estresse sem renderer | **Muito útil como ferramenta de teste**, não como prefeito do gameplay; cenários de obras/logística/crescimento precisam novas ações e regras | A |
| **sim/src/traffic/trafficSystem.ts**, **traffic/vehicles.ts** | Viagem por motivo e horário, ida/volta, estacionamento, deslocamento exterior, propriedade do carro e estado da rota | **Referência de máquina de estados e regressões**; redesenhar duração, transporte coletivo, pedestres, carga, consumo e acesso físico | B |
| **sim/src/view/trafficVisuals.ts**, **web/src/worker.ts** | A visualização prolonga viagens muito rápidas no jogo velho; renderização não altera posição autoritativa do motor | **Aproveitar a separação observador/motor**, mas **não** fabricar deslocamento como se fosse simulação real no IndexCities | B/C |
| **sim/src/systems/removal.ts**, **commands/apply.ts** | Demolir remove prédio/via e põe moradores, alunos e trabalhadores em transição; comandos recusam ações com motivo | **Portar cenários de consequências e validação**, não rotinas de demolição/obra: Index tem propriedade, materiais e reparação/realocação mais ricos | B |
| **sim/src/systems/immigration.ts**, **family.ts**, **lifecycle.ts**, **demography/lifeTable.ts**, **metrics/demography.ts** | Entrada pela estrada condicionada a vaga/emprego, casamento/nascimento/morte e dados de mortalidade/fecundidade; aniversário anual condensado num dia jogável | **Aproveitar funções matemáticas, estrutura de histórico e casos-limite**; revisar fontes e **refazer relógio, agenda, renda e lógica familiar** | B |
| **sim/src/people/census.ts**, **metrics/realism.ts** | Censo agregando necessidades não atendidas e indicadores comparados a referências | **Referência** para diagnósticos da Prefeitura, motivos de pedidos e acompanhamento; não adicionar placar de “realismo” sem aprovação | B |
| **sim/src/systems/utilities.ts** | Água/energia/esgoto por componentes de rua e capacidade agregada por edifício, com recomputação periódica | **Apenas ideia de conectividade/capacidade**; fornecimento, preço, cobrança e consequências reais seguem modelo próprio | B/C |
| **sim/src/economy/treasury.ts**, **systems/economy.ts** | Despesas/custos classificados e receitas agregadas por morador, inclusive modo dinheiro infinito | **Usar somente ideia de categorias, extrato e fechamento**; fluxo monetário original **não é conservativo** e não atende à Reserva Global | C |
| **sim/src/systems/growth.ts**, **config/growth.yaml** | Construtoras automáticas ocupam lotes zoneados conforme demanda RCI e vagas | **Não reutilizar mecanismo**: Index não tem zoneamento automático como ferramenta de colocar prédios | C |
| **sim/src/config/load.ts**, **config/schema.ts**, **data/buildings.yaml** | Dados YAML validados por Zod, catálogo de edificações e valores com fonte ou anotação pendente | **Aproveitar disciplina de validação e dados editáveis**, não os números, campos, tipos de zonas ou limites antigos | B |
| **render/src/roads.ts**, **render/src/camera.ts**, **render/src/accessRoad.ts** | Máscara de quatro vizinhos escolhe peça/rotação da rua; ray-ground/ray-box/picking testados fora de Babylon | **Adaptar geometria matemática** para Godot quando viável, sem exigir grade rígida nem estrada inicial fixa | B |
| **render/src/models.ts**, **render/src/instances.ts**, **render/src/layers.ts**, **render/src/ground.ts**, **render/src/renderer.ts** | Biblioteca GLB, mesh procedural de serviços, thin instances globais, terreno-textura atualizado quando muda, câmera e iluminação | **Referência de visual e batching**, não copiar classes Babylon. Considerar MultiMesh, dirty updates e culling espacial quando medidos | B/C |
| **render/src/pocV4.ts**, **web/src/main.tsx** | Cena opcional com 17 GLBs e câmera/luz próprias, isolada dos sistemas; modo cinema para avaliar aparência | **Reaproveitar roteiro de comparação visual, convenções e possivelmente GLBs**, após conferir direitos, escala e estilo | B |
| **web/src/tools.ts**, **ui/src/** (App, store, painéis) | Drag/preview vira comando; barra de velocidade, seleção, informações de pessoas, finanças e cidade, persistência de estado de painel | **Aproveitar fluxos de UX como comparação**, redesenhar visual para Godot e construção acessível sem leitura | B/C |
| **director/src/** | LLM opcional devolve só comandos limitados e registrados; desligado por padrão, respostas gravadas para reproduzir testes | **Não incorporar diretora de IA ao produto** sem SPEC. Apenas técnica de *mock* reprodutível se surgir caso concreto | C |
| **roadmap/src/**, **roadmap/**, **.github/workflows/roadmap.yml** | Sinais automáticos, pontuação RICE, vinculação a issues e geração de roadmap | **Não transplantar workflow**: AGENTS/SPEC/ARCHITECTURE já definem governança enxuta | C |

### Notas de implementação que poupam investigação

**Determinismo e performance.** A combinação mais útil é RNG com streams (`core/rng.ts`), IDs estáveis (`people/population.ts`), índices de vagas (`core/indexedSet.ts`), custo por operação medido (`core/perf.ts`) e comandos aplicados numa ordem reproduzível (`sim.ts`). Isso é mais valioso do que copiar a estrutura de pacotes.

**Rotas e alterações do mapa.** `roadNetwork.ts` usa versão de rede, `pathfinder.ts` faz A* com heap reaproveitada e desempate explícito, `routeService.ts` controla cache e ordem de entrega. O conjunto é uma excelente referência técnica de baixa dependência. **Mas** o grafo do IndexCities terá que respeitar ruas, calçadas, mudanças de acesso, veículos reais e geometria da SPEC.

**História individual.** `people/events.ts` permite navegar eventos por SIM sem varrer o mundo, e `people/actions.ts` concentra transições consistentes. Considerar separação de eventos importantes, rotina e retenção de longo prazo. Não registrar cada frame como evento permanente.

**Capacidade e vagas.** `matching.ts` usa fila circular com orçamento; `markets.ts` registra somente candidatos ainda com vagas. Boa economia de processamento para cidadãos numerosos. O Índice deve refletir mudanças de vagas, acesso, elegibilidade e dinheiro **efetivas**, não só distância.

**Diagnóstico humano.** `debug/invariants.ts` e `view/report.ts` mostram o que aconteceu e qual entidade falhou. Adaptar para explicar, por exemplo: “obra não evoluiu porque não existe rota física de entrega”; “empresa não consegue comprar aço”; “prefeitura tem verba aprovada, mas não executou porque falta caixa”.

## 4. Assets e direção visual: dois acervos diferentes

### 4.1 Modelos Kenney — candidatos a teste de importação

A main tem **83 GLBs no total**, distribuídos em `packages/web/public/models/`. As pastas `cars/`, `commercial/`, `roads/` e `suburban/` incluem carros, caminhões/van, edifícios de tamanhos diferentes, ruas e árvores. O arquivo [models/LICENSE.md](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models/LICENSE.md) registra origem Kenney / CC0 e um repositório intermediário (Hidencod/tge-assets).

**O que testar:** importação em Godot, materiais PBR/UV, escala, pivô, frente, sombras, triângulos, repetição visual e compatibilidade com a direção artística do IndexCities. CC0 declarada facilita o uso, mas convém conferir o pacote original e manter proveniência organizada. Nem todo modelo do kit precisa ser usado.

### 4.2 GLBs da POC v4 — avaliar autoria/direito separadamente

O diretório `packages/web/public/models/poc-v4/` tem **17 GLBs**: seis peças de rua (`rua_reta`, `rua_curva`, `rua_t`, `rua_cruzamento`, `rua_fim`, `rua_praca`); duas casas; café e loja; árvore, banco, floreira, três carros e uma pessoa. [README específico](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models/poc-v4/README.md).

Os arquivos foram apresentados como **assets autorais fornecidos pelo proprietário**, não como modelos Kenney. O README registra: **1 unidade = 1 m; lote/rua 16 × 16 m; origem ao centro da base; frente +Z; asfalto 0,02 m; calçada 0,18 m; gramado 0,30 m**. O teste #319 verificou hashes de 17 arquivos contra o pacote original.

**Recomendação:** confirmar titularidade, arquivos-fonte e permissão de redistribuição para o IndexCities, importar somente um subconjunto e avaliar qualidade visual e desempenho no Godot. **Não** assumir que a licença CC0 da pasta geral cobre estes 17 modelos. **Não** transformar o tile de 16 m ou essas alturas em requisito de jogo.

### 4.3 O que as POCs visuais ensinaram

- **v2:** blocos compactos, composição urbana, jardim/calçada/props, menos vazios. Experimento de cor e material; não confiar em recoloração automática de qualquer GLB.
- **v3:** testar materiais semanticamente identificados (parede, telhado, vidro etc.), câmera ortográfica versus perspectiva estreita e calçadas derivadas da vizinhança; [issue #318](https://github.com/Marlohn/CityBuilder/issues/318). É referência para experimentação visual, **não decisão atual de câmera**.
- **v4:** pacote mais coerente de ruas/calçadas/lotes com PBR e iluminação, porém ainda pequeno e com risco de custo gráfico; [issue #319](https://github.com/Marlohn/CityBuilder/issues/319). Isolamento do ambiente facilita comparar o mesmo visual sem confundir renderização com simulação.

A [exploração de assets já existente no IndexCities](asset-production-pipeline.md) considera promissora a produção **paramétrica/procedural na Godot**, gerando recursos uma vez e reutilizando variantes. Os GLBs antigos podem ser **benchmark, complemento ou insumo temporário**, não motivo para trocar de pipeline sem teste.

## 5. Achado especialmente valioso: experimento de escala que NÃO está na main

A otimização mais instrutiva do CityBuilder está na [branch perf/web-scale-gate](https://github.com/Marlohn/CityBuilder/tree/038b506a21d0ffcd1e5726601b8497f79c2f40dc), não na versão principal. Fontes:

- [instances.ts da branch](https://github.com/Marlohn/CityBuilder/blob/038b506a21d0ffcd1e5726601b8497f79c2f40dc/packages/render/src/instances.ts): `ChunkedBatchSet`, janela visível, buffers e estatísticas.
- [pocV4.ts da branch](https://github.com/Marlohn/CityBuilder/blob/038b506a21d0ffcd1e5726601b8497f79c2f40dc/packages/render/src/pocV4.ts): snapshots de chunks estáticos e atualizações sujas.
- [POC-V4-PERFORMANCE.md](https://github.com/Marlohn/CityBuilder/blob/038b506a21d0ffcd1e5726601b8497f79c2f40dc/docs/POC-V4-PERFORMANCE.md): protocolo e instrumentos.
- [#320](https://github.com/Marlohn/CityBuilder/issues/320), [#321](https://github.com/Marlohn/CityBuilder/issues/321), [#322](https://github.com/Marlohn/CityBuilder/issues/322): problema, execução e decisão histórica.

**Problema original:** agrupamento de thin instances por **modelo globalmente** distribuía milhares de cópias por toda a cidade; um bounding volume enorme anulava boa parte do descarte de objetos invisíveis. O código original ainda forçava ativação de meshes. Efeitos caros (sombras, SSAO, MSAA e bloom) ampliavam a carga.

**Solução experimental:** manter no CPU pequenos **chunks espaciais de 16 × 16 tiles**, detectar alterações nos chunks de ruas/prédios/árvores, considerar a janela da câmera e transferir para a GPU só as matrizes das instâncias necessárias. Buffer GPU só é atualizado quando a janela ou seu conteúdo muda; objetos pequenos à distância deixam de ser enviados. Particularidade Babylon: clones compartilham Geometry e buffers de thin instances, então foi evitada duplicação de malha por chunk.

A [issue #322](https://github.com/Marlohn/CityBuilder/issues/322) registrou para um cenário **sintético de 256 × 256**: 6.017 prédios, 54.361 instâncias totais, **31.220 → 7.768 instâncias ativas**, aproximadamente **70,05 → 19,83 milhões de triângulos ativos** e **576 → 140 chunks visíveis**, com zero uploads estáticos redundantes na medição citada. Essas são **contagens estruturais relatadas pelo projeto**, não prova de FPS aceitável no hardware real depois das correções. Antes delas, o responsável registrou aproximadamente **1 FPS** em teste manual de cidade grande, motivo para abandonar a plataforma Web naquele projeto.

**Lição transferível para Godot:** **MultiMesh não faz culling por instância individual.** Particionar de acordo com região visível, controlar mudanças pontuais, compartilhar recursos e medir triângulos/draw calls/tempo de frame costuma ser mais importante do que simplesmente trocar engine. **Não** copiar o tamanho 16×16 nem criar um sistema de chunks antes de conhecer a carga real do IndexCities.

## 6. Dados e conteúdo: quais podem economizar trabalho?

| Material antigo | O que contém | Ação prudente |
| --- | --- | --- |
| `data/names.json`, `data/raw/nomes-*`, `data/raw/sobrenomes-*`, `data/raw/FONTES.md` | Nomes brasileiros com origem documentada em conjuntos IBGE/repositório intermediário | **Consultar e conferir fonte/licença/representatividade**; pode poupar trabalho de geração de nomes |
| `data/mortality-br-2023.json`, `data/fertility-br-2023.json`, `tools/calibrate-demography.ts` | Tabelas calibradas a indicadores demográficos brasileiros, com rotina para calculá-las | **Aproveitar método e testes**, revalidar ano/hipóteses e não usar regras compactadas por “ano a cada dia” |
| `data/buildings.yaml` | Tipos de prédios, capacidade, área, custos e obras, comentários de fontes e itens PENDENTE | **Fonte de pistas de pesquisa**, não importação de preços, durações, quantidade de empregos ou zoneamento |
| `config/traffic.yaml`, `config/roads.yaml`, `config/health.yaml`, `config/lifecycle.yaml` | Referências e parâmetros de rotinas, circulação, saúde e ciclos de vida | **Roteiro de validação de dados**, com conferência primária antes de qualquer uso no novo jogo |
| `config/world.yaml`, `config/utilities.yaml`, `config/economy.yaml`, `config/growth.yaml` | Grade fixa, água e energia por capacidade, receita municipal, crescimento automático | **Não importar como definições do IndexCities**; o modelo econômico e espacial mudou |
| `data/reference/cidade-real.yaml`, `config/realism.yaml` | Indicadores comparativos, fontes e lacunas de serviços | **Referência de pesquisa/diagnóstico**, não lista de requisitos nem novo placar automático |
| `scenarios/bairro-basico.yaml`, `scenarios/estresse.yaml` | Cidades reproduzíveis por seed, comandos e parâmetros | **Usar formato mental de fixture**, reescrever ações para obras físicas do IndexCities |

**Cuidado com números “com fonte”:** vários comentários antigos declaram `PENDENTE`, usam aproximações ou fontes indiretas. Referência não equivale a calibração válida. A SPEC prevalece também quando um YAML antigo parece realista.

## 7. Testes antigos que valem como casos de regressão

Não transferir a suíte TypeScript inteira. **Transformar casos perigosos em testes C# quando o respectivo sistema existir**:

| Caso que já foi investigado | Evidência no CityBuilder | Invariante/caso novo sugerido |
| --- | --- | --- |
| Mesmo seed + mesmos comandos; reabrir save sem desviar | `tests/e2e/determinism.spec.ts`, `tests/unit/save.test.ts`, `tests/unit/reference.test.ts` | Comparar estado C# e comportamento após save/load; preservar versão e IDs |
| Falta de acesso e construção | `packages/sim/test/commands.test.ts`, `tests/e2e/access-road.spec.ts` | Projeto pode ser confirmado quando fisicamente posicionável, mas obra **não progride sem entrega viária real**, conforme SPEC |
| Carro “desaparecido” ou parado numa via demolida | `packages/sim/test/traffic-continuity.test.ts`, `carro-via-demolida.test.ts`, `traffic-visuals.test.ts` | Identidade, localização e estado lógico/visual coerentes; remover rua exige consequência real |
| Rota e viagens por motivo | `packages/sim/src/routing/pathfinder.test.ts`, `test/trips-by-purpose.test.ts`, `tests/acceptance/37-rotina-jornada-por-pessoa.test.ts` | Origem/destino/motivo, agenda e chegada efetivos, sem viagens inventadas ou escondidas |
| Capacidade/corte de água e esgoto | `packages/sim/test/utilities.test.ts`, `tests/acceptance/108-esgoto-motor.test.ts` | Serviço com capacidade/ligação real; impactos e cobranças condicionados a fornecimento |
| Fila de internação e alta | `tests/acceptance/239-fila-internacao-alta.test.ts`, `packages/sim/src/systems/hospitalAdmission.ts` | Capacidade/solicitação/atendimento reais; não criar leito ou viagem fictícios |
| Gastos municipais categorizados | `packages/sim/test/finance-history.test.ts`, `tests/acceptance/142-historico-financeiro.test.ts` | Exigir contrapartida monetária real, saldos conservados e detalhamento de despesas |
| Estresse e trabalho por sistema | `tests/slow/performance.test.ts` | Medir 10k/25k/50k quando fizer sentido; comparar trabalho, CPU/GC, desempenho em hardware efetivo |
| POC visual integrada versus padrão | `tests/e2e/poc-v4.spec.ts` | Validar importação Godot, escala, materiais, câmera e cenas sem confundir com aprovação do estilo |

**Importante:** nomes dos testes e assertions foram inspecionados estaticamente. Não significa que a suíte atual inteira foi executada ou está verde. Alguns arquivos de aceitação contêm comentários históricos de “teste antes da implementação”; conferir o código vigente antes de confiar neles como verdade.

## 8. O que não vale gastar tokens portando

1. **Toda a interface React e transporte Web Worker.** A arquitetura aprovada usa Godot/C#; a separação conceitual já está decidida.
2. **Ciclo de vida “um dia = um ano”, clock e despesas anuais condensadas.** Introduziria erros sistêmicos em agendas, pessoas, produção, serviços e pagamentos.
3. **Crescimento por zoneamento automático / gerador de bairros do prefeito como gameplay.** O jogador coloca os edifícios, e as regras de investimento/empresa são outras. O prefeito automático ainda pode inspirar **fixtures de teste**.
4. **Finanças com receita automática por população e sandbox infinito.** Quebrariam oferta monetária fixa e escopo atual do modo Convencional.
5. **Construção sem cadeia física.** Não equivale a reservar/comprar/transportar/consumir material e pagar trabalhadores e frete.
6. **Serviços resolvidos apenas por distância/capacidade agregada em rua.** Não substituem redes, falhas, pagamentos e consequências aprovados.
7. **Animação de tráfego que alonga o tempo observável como substituto de trânsito real.** Útil como observador no relógio antigo, perigosa se mascarar tempo causal no IndexCities.
8. **Automação de roadmap, agente LLM diretor, excesso de CI e divisão em oito pacotes** sem necessidade comprovada. O IndexCities já escolheu governança e estrutura enxutas.
9. **Dependência da grade exata 256 × 256 / 16 m, estrada inicial pronta e catálogos visuais completos.** Esses fatos pertencem ao CityBuilder.
10. **Otimizações do Babylon copiadas literalmente.** Transpor a lição do culling para APIs do Godot, não reproduzir suas estruturas por inércia.

## 9. Ordem sugerida para usar esse acervo quando a implementação começar

**Etapa 1 — fundamento barato:** consultar `rng.ts`, `indexedSet.ts`, `population.ts`, `people/events.ts`, `debug/invariants.ts` e os testes determinísticos; implementar as estruturas mínimas que a SPEC exige em C#. A prioridade é identidade, invariantes econômicas e observabilidade.

**Etapa 2 — mundo, construção e rotas:** consultar `world/roadNetwork.ts`, `pathfinder.ts`, `routeService.ts`, `commands/apply.ts` e testes de vias; modelar novas conexões, acesso de obra, entrega de materiais e comando de confirmação sem herdar zoneamento.

**Etapa 3 — SIMs, empresas e serviços:** usar `actions.ts`, `markets.ts`, `matching.ts` e testes de fila/remoção como referências; construir os estados de dinheiro/estoque/contratos e o calendário civil que o CityBuilder não tem.

**Etapa 4 — debug headless integrado:** aprender de `cli/src/main.ts`, `bots/src/scenario.ts`, `bots/src/mayor.ts` e `view/report.ts`; gerar uma cidade de teste e explicar por que algo não acontece. **Não** criar bot de prefeito como funcionalidade do jogo.

**Etapa 5 — visual e escala:** testar importação de poucos assets (com direitos verificados), olhar POC v3/v4 e especialmente a branch `perf/web-scale-gate`. Medir CPU, GPU, frame time, culling e custo de recursos Godot na escala **real** necessária, não repetir o erro de otimizar apenas vitrine pequena.

Esta ordem é **roteiro de consulta**, não novas fases/POCs obrigatórias nem alteração do plano de jogo integrado definido em AGENTS.

## 10. Instrução curta para uma futura IA implementadora

> Implemente **[subsistema X]** do IndexCities a partir da SPEC/ARCHITECTURE atuais. Antes de codificar, consulte o mapa em `docs/exploration/citybuilder-reuse-audit.md` e abra **apenas os arquivos listados para X** do CityBuilder no commit indicado, mais os testes relevantes. Explique em duas ou três linhas: (1) algoritmo reaproveitável; (2) incompatibilidade de produto; (3) como será adaptado em C#. Não porte código/depêndencias/valores em bloco. Se faltar decisão de gameplay, interrompa apenas a parte indefinida e informe a lacuna. Teste o comportamento do IndexCities, não a equivalência literal ao CityBuilder.

**Critério para chamar algo de reuso bem-sucedido:** o agente economizou investigação sem importar comportamento indevido, ganhou um teste/invariante útil e consegue apontar exatamente qual arquivo antigo motivou o algoritmo novo. Não medir economia de tokens por estimativas fictícias; comparar tempo/custo/iterações em tarefas reais se desejado.

## 11. Lacunas honestas desta auditoria

- Inventário completo da árvore Git e leitura focalizada dos módulos principais **não substituem auditoria linha a linha de todos os testes e scripts**. Se um módulo for portado, reler seu arquivo inteiro no commit fixo, dependências e casos relevantes.
- Nenhum teste foi rodado nesta pesquisa; resultados de FPS, CI e contagens acima são históricos reportados nas issues/experimentos, não benchmark reproduzido no IndexCities.
- Os 17 GLBs da POC v4 exigem confirmação de titularidade e condições de redistribuição; licença dos kits Kenney é distinta.
- A branch de performance não faz parte da main; **não consultar só a main** quando pesquisar otimização visual.
- Esta auditoria não aprova qualquer mudança em `SPEC.md`, `ARCHITECTURE.md` ou `AGENTS.md`. A partir de agora, a maior economia provável virá de consultar **trechos específicos deste catálogo**, não de criar mais documentação sobre o projeto antigo.

## Fontes rápidas

- [CityBuilder — guia do código](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/docs/GUIA-DO-CODIGO.md)
- [CityBuilder — código principal da simulação](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src)
- [CityBuilder — testes](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/tests)
- [CityBuilder — modelos e licença](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models)
- [CityBuilder — branch de performance](https://github.com/Marlohn/CityBuilder/tree/038b506a21d0ffcd1e5726601b8497f79c2f40dc)
- [IndexCities — SPEC](../SPEC.md), [arquitetura](../ARCHITECTURE.md), [exploração](../EXPLORATION.md), [pipeline de assets](asset-production-pipeline.md), [testes e reprodução](testing-debugging-reproducibility.md).
