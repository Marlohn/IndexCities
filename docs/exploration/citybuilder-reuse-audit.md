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

## 12. Segunda passagem crítica — riscos concretos e oportunidades não evidentes (2026-10-11)

> **Revisão humana desta seção: PENDENTE.** Segunda leitura independente e **estática** das implementações, suas combinações, algumas issues históricas, POCs não integradas e regras da SPEC atual. Não houve execução de testes; exemplos abaixo descrevem *riscos inferidos do código*, não bugs reproduzidos no IndexCities. As prioridades da seção 3 indicam **qual código vale consultar**, não autorização para copiá-lo ou implementá-lo antecipadamente.

### 12.1 O que a primeira leitura precisava deixar mais explícito

| Achado verificável no CityBuilder | Diferença no IndexCities / risco | Decisão prudente para reuso |
| --- | --- | --- |
| **A malha de ruas é plana e indiferenciada.** [roadNetwork.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/world/roadNetwork.ts) faz flood-fill pelos quatro vizinhos, associa **qualquer via na borda** a uma saída exterior e procura **um primeiro acesso** contíguo ao prédio. [pathfinder.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/routing/pathfinder.ts) usa custos fixos por tipo de tile. | A SPEC prevê **conexão exterior preparada**, **pontes e viadutos com níveis sobrepostos**, acessos reais, circulação/embarque/carga e ruas que podem isolar a cidade. Vizinhos no plano não são necessariamente conectados no espaço ou na direção de circulação. | **Reaproveitar:** noção de componentes conectados, versionamento e algoritmo de busca. **Redesenhar:** grafo por conexão efetiva, nível, modalidade e regras de direção, entrada/saída e acessos; testes de via cruzada sem conexão, ponte, entrega e demolição. Não importar a definição “qualquer borda é saída”. |
| **Cache de rotas só considera mudanças viárias e chave numérica.** [routeService.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/routing/routeService.ts) usa `from * world.size + to`, limpa tudo após `roadVersion` e devolve trajetos com custos estáticos. | Rotas do IndexCities dependem também de tipo de veículo/deslocamento, acesso, operação da via, níveis e impedimentos pertinentes. Um cache compartilhado entre modalidades/restrições poderia devolver percurso impossível; limpeza integral pode ficar cara sob edição frequente. | **Reaproveitar:** cache versionado e ordenação de resultados para reprodução. **Não copiar:** chave, invalidação ou custos como contrato definitivo. Indexar por características que mudam o resultado; medir precisão e economia antes de cache regional/hierárquico. A proposta de [issue #7](https://github.com/Marlohn/CityBuilder/issues/7) é **proposta histórica**, não algoritmo implementado nem ganho comprovado no IndexCities. |
| **A busca por vagas pode não acordar só porque o mundo ficou acessível.** Em [markets.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/markets/markets.ts), `VacancyMarket.version` aumenta ao **adicionar um prédio ao conjunto de vagas**, não por qualquer alteração de acessibilidade/capacidade. Em [matching.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/systems/matching.ts), candidatos com `triedJob/triedSchool/triedClinic === version` não procuram de novo. A seleção é amostrada e usa Manhattan, não uma rota efetiva. | Uma rua religada, mudança de serviço, modalidade ou capacidade pode alterar quais vagas são viáveis **sem** provocar o incremento usado como gatilho. O resultado pode ficar desatualizado, dependendo do restante dos sistemas. A escolha de vaga também precisa respeitar renda, critérios, viagem efetiva e contratação real. | **Não portar o protocolo de retry literal.** Adotar índices de candidatos por elegibilidade com gatilhos de invalidação apropriados e verificação de capacidade antes de compromisso. **Teste novo:** depois de perder/restaurar acesso à vaga, o SIM volta a considerar oportunidades quando deveria, sem varrer toda a população a cada frame. |
| **O tráfego visual tem outro cache.** [trafficVisuals.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/view/trafficVisuals.ts) armazena `walkPaths` por par origem/destino e não consulta `roadVersion` ao reutilizá-los; há reconciliação específica para carro. | Um caminho *desenhado* pode manter uma rota antiga após alteração de vias, mesmo quando o motor recalcula corretamente. No IndexCities, seguir SIM e observar deslocamentos reais é requisito explícito, e a remoção de rua deve afetar a circulação **imediatamente**. | **Reaproveitar:** detecção e recuperação de divergência lógico/visual como problema a testar. **Não copiar:** animação desacoplada de 6 s, cache de pedestres sem invalidação ou reconstrução de caminhos a partir de uma realidade diferente do Core. |
| **História individual é um log encadeado, mas cresce sem retenção.** [people/events.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/people/events.ts) armazena eventos globalmente em arrays crescentes e índice do último evento por pessoa. Os códigos cobrem sobretudo marcos, vínculos e necessidades não atendidas. | A SPEC exige também **compras, trajetos, atividades cotidianas reais**, busca histórica e persistência por cidade/indivíduo. O formato antigo não resolve, por si, paginação, memória de longo prazo, migração de schema nem volume histórico do jogo novo. | **Reaproveitar:** índices por cidadão e IDs estáveis de tipo de evento. **Projetar:** histórico cotidiano com consultas, persistência, escala e apresentação resumida sem descartar silenciosamente dados exigidos. Testar anos de jogo com milhares de SIMs, não apenas uma pessoa. |
| **Chegada de famílias exige emprego antes de criá-las.** [immigration.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/systems/immigration.ts) faz busca de vaga/emprego **prévia** à entrada de adultos não aposentados, e o sistema opera com moradia disponível. | A SPEC atual permite famílias entrarem **sem emprego previamente garantido** e empresas chegarem **antes** de demanda/clientela efetivamente realizada, se houver oportunidade plausível e **capital vindo da Reserva Global**. Exigir empregado antes de migrar criaria um bloqueio circular de crescimento. | **Não portar o filtro de imigração.** Usar apenas geração reproduzível de composição familiar, rastreabilidade da entrada e casos de teste; reconstruir decisão econômica de entrada, risco, moradia, propriedade e conta individual. |
| **O cofre antigo aceita negativo e receita sem remetente.** [treasury.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/economy/treasury.ts) usa `charge` para baixar saldo mesmo sem dinheiro, `earn` para aumentá-lo e `sandbox` que não baixa o saldo. | No IndexCities, moeda global é conservada, existem titulares reais, Reserva Global, dívidas explícitas, consumo físico, impostos e pagamentos parciais. Substituir “saldo negativo” por “dívida” não basta: é preciso preservar **quem deve a quem**, se o pagamento aconteceu e onde ficou o dinheiro. | **Sem reuso de motor financeiro.** Aproveitar apenas classificação de extratos. **Teste fundamental:** soma de Caixa + saldos de SIMs + caixas de empresas + Reserva Global constante; obrigação vencida ou material reservado **não** conta como dinheiro/mercadoria paga ou entregue. |
| **O replay não é um snapshot completo.** [replay.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src/save/replay.ts) grava seed/overrides/tick/comandos e **reexecuta todos os ticks**, com migração do formato. | O IndexCities terá economia real mais ampla, autosave de cidades grandes, **esboços de obras/ruas ainda não confirmados**, estoques, cargas e contratos duráveis. A repetição total pode crescer em tempo e exige que **todas** as mudanças relevantes sejam reproduzíveis, inclusive sistemas autônomos. | **Reaproveitar:** esquema versionado, repro de bug e ideia de comando determinístico. **Avaliar snapshots persistentes + log incremental, ou outra estratégia confiável**, sem tratar replay puro como solução obrigatória. Testar save/load durante frete, reserva, inadimplência e projeto não confirmado. |
| **As provas visuais não garantem qualidade de arte.** [poc-v4.spec.ts](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/tests/e2e/poc-v4.spec.ts) verifica asset carregado, configuração e ausência de erro; [poc-art-v2.spec.ts](https://github.com/Marlohn/CityBuilder/blob/poc/isometric-art-direction-v2/tests/e2e/poc-art-v2.spec.ts) chega a usar tamanho de screenshot como assert. | Teste automático verde não prova que uma rua, fachada, calçada ou personagem ficou **bonito, legível, acessível ou próximo da referência**, tampouco que roda bem numa cidade grande. | **Reaproveitar:** cenas pequenas determinísticas de comparação, presença dos assets, picking e métricas de render. **Acrescentar quando útil:** comparação visual controlada e avaliação humana em vários zooms, cenas e hardware. Não transformar contagem de bytes de screenshot em critério de direção artística. |

### 12.2 Arte: uma branch adicional útil, mas não um gerador procedural pronto

Além das POCs já listadas, a branch [`poc/isometric-art-direction-v2`](https://github.com/Marlohn/CityBuilder/tree/poc/isometric-art-direction-v2) contém [`packages/render/src/illustrated.ts`](https://github.com/Marlohn/CityBuilder/blob/poc/isometric-art-direction-v2/packages/render/src/illustrated.ts), **não presente na main**. Ela gera *dressing* por composição: gramado/piso, caminho de acesso, canteiro, árvore, cerca viva, banco e detalhe de estacionamento conforme dimensões, frente, tipo de lote e uma variação determinística por posição. É **um roteiro implementado de ambientação visual a partir de geometria simples**; pode inspirar geradores paramétricos de lote no Godot sem obrigar a modelar todos os jardins individualmente.

**Atenção a dois erros fáceis de herdar:** (1) essa POC tenta adivinhar o papel do material pela cor original do GLB para recolorir, heurística que a [issue #317](https://github.com/Marlohn/CityBuilder/issues/317) reconheceu como frágil; a [POC v3](https://github.com/Marlohn/CityBuilder/blob/poc/visual-v3-babylon/packages/render/src/pocV3.ts) experimentou paletas por **papel semântico explícito** (parede, telhado, vidro, pavimento etc.); (2) `illustrated.ts` adiciona decoração visual diretamente com base na posição/tipo do prédio. No IndexCities, **vegetação e objetos colocados pelo jogador têm espaço e custos reais**, e a decoração não pode fingir que um objeto físico inexistente foi construído/pago. Separar **acabamento visual que faz parte do asset** de **entidades colocáveis, cobradas e persistidas**. O tamanho/efeito de cada variação será questão da implementação, não regra herdada.

O [comparativo da POC v3](https://github.com/Marlohn/CityBuilder/issues/317) também registrou um detalhe instrutivo para protótipos: testar **os mesmos modelos** com iluminação/câmera/material diferentes para isolar causa da aparência, em vez de trocar simultaneamente engine, conteúdo e apresentação. O melhor reuso dessas POCs é **metodologia de comparação e pequenas funções de geração visual**; não é a implementação do renderer Babylon.

### 12.3 Licenças: há uma contradição interna a resolver antes de copiar assets

- O [`LICENSE` da raiz](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/LICENSE) identifica o **código do repositório** como Apache License 2.0. Isso não transforma automaticamente modelos de terceiros em Apache 2.0 nem dispensa obrigações de atribuição/avisos se código for efetivamente reutilizado.
- O [`packages/web/public/models/LICENSE.md`](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models/LICENSE.md) afirma genericamente que **todos os modelos da pasta** são Kenney/CC0, mas o [`models/poc-v4/README.md`](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models/poc-v4/README.md) diz que os 17 GLBs são **originais fornecidos pelo proprietário** a partir de ZIP externo. **As duas afirmações não podem ser aplicadas indiscriminadamente ao mesmo subconjunto.** Os hashes comprovam identidade dos arquivos, **não autoria ou direito de distribuição**.
- Os [nomes brutos](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/data/raw/FONTES.md) incluem dados de 2010 por repositório intermediário e sobrenomes de 2022 com menção a **notícia de imprensa**, não uma licença ou base primária arquivada no documento. Validar proveniência, termos e precisão antes de copiar dados.

**Conclusão operacional:** separar inventários de **código Apache-2.0**, **kits Kenney presumidamente CC0, a confirmar por origem** e **GLBs v4 de titularidade/redistribuição ainda não comprovadas**. Não mover assets para IndexCities até resolver o último grupo. É verificação documental, não parecer jurídico.

### 12.4 Como usar estes achados sem repetir o fracasso da migração antiga

A [issue #322](https://github.com/Marlohn/CityBuilder/issues/322) propôs migrar o próprio CityBuilder inteiro preservando regras e equivalência com TypeScript. **Não executar esse plano no IndexCities.** Ele foi desenhado para preservar **um produto diferente**, inclusive grade 256 × 256, 16 m, zoneamento e outros elementos que o IndexCities não aprovou.

Ao implementar um domínio já definido na SPEC, fazer somente este circuito:

1. **SPEC atual primeiro**: identificar comportamento, efeitos sobre SIMs, dinheiro, logística e limites de apresentação. A ARCHITECTURE orienta fronteiras técnicas.
2. **Escolher um algoritmo ou teste antigo por necessidade**, com caminho exato da seção 3 ou 12.1; abrir fonte e dependências em commit fixo. Reavaliar se aquele código é correto e se seu pressuposto ainda vale.
3. **Traduzir a ideia, não transportar o subsistema**: separar entrada/saída e invariantes; portar/recriar somente o mecanismo útil em C# quando necessário, preservando licenças aplicáveis.
4. **Reproduzir o caso crítico novo**: por exemplo, passagem em viaduto sem conexão; nova estrada que libera oportunidade; entrega cancelada em trânsito; pagamento parcial sem criação de dinheiro; SIM realmente visível durante a viagem; save de projeto ainda não aprovado.
5. **Não exigir uma POC por sistema nem testes de paridade com CityBuilder**: são evidências opcionais para tarefas de risco, não gates obrigatórios nem novas decisões oficiais.

**Prioridade revisada deste material:** o ganho imediato maior continua em **testes e diagnóstico + determinismo**, seguido de **rotas/conectividade refeitas para a SPEC**, **histórico individual escalável** e **composição procedural do visual**; o código financeiro e a lógica de imigração antiga permanecem **anti-exemplos de comportamento**. Performance Web é referência de problema/medição, **não prova de que Godot já resolveu a escala**. Na [branch de performance](https://github.com/Marlohn/CityBuilder/tree/038b506a21d0ffcd1e5726601b8497f79c2f40dc), a medição automatizada após as correções não foi seguida por um resultado independente de **FPS em GPU real** documentado nesta auditoria.

**Condição para alterar este documento de novo:** evidência nova (teste executado, medição em Godot, licença confirmada, mudança de SPEC ou componente de fato reaproveitado); não ampliar o catálogo por ritual. A revisão humana deste adendo permanece **PENDENTE**.

## Fontes rápidas

- [CityBuilder — guia do código](https://github.com/Marlohn/CityBuilder/blob/32fdf298a9225e3cb0840f54979e689f5d4fc038/docs/GUIA-DO-CODIGO.md)
- [CityBuilder — código principal da simulação](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/sim/src)
- [CityBuilder — testes](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/tests)
- [CityBuilder — modelos e licença](https://github.com/Marlohn/CityBuilder/tree/32fdf298a9225e3cb0840f54979e689f5d4fc038/packages/web/public/models)
- [CityBuilder — branch de performance](https://github.com/Marlohn/CityBuilder/tree/038b506a21d0ffcd1e5726601b8497f79c2f40dc)
- [IndexCities — SPEC](../SPEC.md), [arquitetura](../ARCHITECTURE.md), [exploração](../EXPLORATION.md), [pipeline de assets](asset-production-pipeline.md), [testes e reprodução](testing-debugging-reproducibility.md).
