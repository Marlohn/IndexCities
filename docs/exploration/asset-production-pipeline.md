# IndexCities — Pesquisa final sobre assets 3D, criação e gestão

> **EXPLORAÇÃO — NÃO é SPEC nem regra aprovada. Revisão humana: PENDENTE.** Síntese atualizada em **2026-10-11**. Inclui estudos de casos, palestras, relatos de desenvolvedores, documentação técnica e verificações de licença. **Recomendações, medidas e experimentos abaixo são hipóteses**, não decisões do responsável. Consulte [SPEC](../SPEC.md), [ARCHITECTURE](../ARCHITECTURE.md) e [mundo/mapa](world-map-environment.md).

## Resposta final em poucas linhas

**Recomendação: um catálogo semântico de construções + uma biblioteca pequena de modelos-base e componentes artísticos + variantes geradas/compostas fora do runtime + poucos edifícios especializados.** Produção externa com Blender/IA ou diretamente na Godot conforme o custo real de cada classe de asset. O visual continua substituível sem afetar a simulação. **Não** criar editor procedural completo, dezenas de meshes exclusivas por atividade ou um sistema sofisticado de gestão antes de existir necessidade.

**Produção já pode começar** com linguagem visual, modelos provisórios e uma cena representativa. **Produção em massa, não**, até validar câmera, escala, vias/calçadas, portas, colisão/ocupação e desempenho. Não precisamos conhecer a quantidade definitiva de edifícios para fazer esse primeiro trabalho.

**Mudança importante desde a pesquisa anterior:** a SPEC de 2026-10-11 aprovou **dois modos, Criativo e Convencional**, e valoriza **construção compreensível sem leitura, variedade estética e feedback visual**. Isso aumenta a importância de silhuetas fáceis de reconhecer, miniaturas consistentes, variantes visuais e acessos perceptíveis. **Após a rodada de 2026-10-11**, a SPEC já aprovou moradores persistentes com rotinas simplificadas no Criativo e decoração com efeitos locais no Convencional. **Continuam abertos** o catálogo concreto de ornamentos, número de variantes e qualquer editor modular de peças pelo jogador.

## Viabilidade de modularidade no lançamento — reavaliação de 2026-10-11

> **Revisão humana desta seção: PENDENTE.** O responsável **não aprovou** o editor modular da alternativa 4B; perguntou se é viável no primeiro momento. Esta é uma análise/recomendação para discussão, não nova funcionalidade da SPEC.

**Conclusão:** famílias de meshes e peças modulares **na autoria de assets** são tecnicamente viáveis e já estão recomendadas no pipeline; **não equivalem** a oferecer no lançamento um editor in-game de fachadas, telhados, cercas e peças compatíveis. Este último exige catálogo de encaixes, controle de colisão/footprint/acesso, UI sem leitura, persistência de combinações e testes de desempenho em escala, além de produção artística. Na Godot, [instanciamento/otimização 3D](https://docs.godotengine.org/en/stable/tutorials/performance/optimizing_3d_performance.html) ajuda a renderizar repetições, mas não resolve experiência de autoria ou regras de encaixe. O jogo [Foundation](https://wiki.polymorph.games/foundation/Buildings) demonstra modularidade seletiva em construções específicas, enquanto o criador de [Tiny Glade relatou](https://steamcommunity.com/app/2198150/discussions/0/4844273660400957331/) que até controlar os adereços individuais exigiu limitar escopo no lançamento.

**Direção de produto aprovada depois da pesquisa — 4A (2026-10-11):** o primeiro modelo oferecerá **modelos completos e variantes visuais prontas** (silhuetas/cores/fachadas/telhados compatíveis conforme catálogo), além de decoração. A biblioteca pode usar módulos **na produção** de variantes offline, mas **não haverá editor modular de montagem de peças pelo jogador no primeiro modelo**. Eventual editor futuro não está aprovado. Número e conteúdo exato de variantes/ornamentos seguem para produção/calibração. A decoração com efeitos locais 5C também já está na SPEC; seu cálculo concreto segue em exploração.

## 1. Evidências externas comparadas (e limites)

| Caso / fonte verificável | Aprendizado específico que vale aproveitar | NÃO copiar |
| --- | --- | --- |
| [SimCity — GDC: arte a serviço da simulação](https://www.gdcvault.com/play/1019107/Building-SimCity-Art-in-the) | Edifícios, vias e estados devem **comunicar processos**, não só decorar | Arte que exige mais dados de gameplay do que a SPEC já permite |
| [Foundation — discussões de criação modular](https://steamcommunity.com/app/690830/discussions/2/4361247613257623565/) | Kits reutilizáveis e leitura de estruturas diferentes | Construção por peças pelo jogador ou upgrades de prédios prontos, fora do escopo |
| [Workers & Resources — guia de modelagem](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Modelling) e [editor de edifícios](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Building_editor) | Modelos 3D têm **acesso e área operacional** reais, além da fachada | Reaproveitar sistema proprietário ou um lote universal |
| [Transport Fever 2 — formatos e LOD](https://www.transportfever2.com/wiki/doku.php?id=modding%3Aresourcetypes%3Amdl) | Mesh, identidade, configurações e níveis de detalhe podem ser separados | Um tipo de negócio = um novo conjunto de assets exclusivos |
| [Tiny Glade — entrevista técnica](https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games) | Artista cria peças/regras para combinar formas com coerência | Replicar o sistema inteiro de construção dinâmica |
| [Manor Lords — entrevista com desenvolvedor solo](https://www.unrealengine.com/developer-interviews/solo-dev-makes-sophisticated-sim-manor-lords-using-unreal-engine) e [relato sobre lotes](https://store.steampowered.com/news/posts/?appgroupname=Codemasters&enddate=1657372501&feed=steam_community_announcements) | Prédio e terreno devem parecer **integrados ao bairro**; fachada isolada não resolve | Sistema de lotes residenciais automático, não aprovado no IndexCities |
| [Cities: Skylines II — melhorias de editor, 11/08/2026](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/expanding-the-modding-tools) | A dor real é **encontrar, editar, substituir e testar** assets, não só criá-los | Editor completo de mods/assets antes de demanda |
| [Factorio — produção artística](https://www.factorio.com/blog/post/fff-146) e [renderização](https://www.factorio.com/blog/post/fff-227) | Arte provisória primeiro; consistência e texturas compartilhadas importam | Copiar solução de sprites 2D para o 3D sem medir |
| [GDC — Building Blocks: Artist Driven Procedural Buildings](https://gdcvault.com/play/1012219/Building-Blocks-Artist-Driven-Procedural) | **Gramática de fachadas**: regras simples organizam portas, janelas e pavimentos com peças desenhadas por artistas | Um sistema procedural completo na engine do jogo |
| [Projeto Houdini — Shape Grammar](https://github.com/kiryha/Houdini/wiki/procedural-city) e [estudo de fachadas, 22/05/2026](https://jonasmeyerohle.dev/blog/2026/05/city-planning-5/) | As mesmas regras de fachada se aplicam a várias larguras/alturas **na autoria** | Gerar centenas de nós/CSG por prédio a cada frame |

**Comunidade, evidência anedótica, não benchmark:** [artista de ambientes no r/godot, ago/2026](https://www.reddit.com/r/godot/comments/1vubyyp/what_do_your_art_pipelines_look_like_both_2d_and/) enfatiza paleta, iluminação e densidade consistentes acima da quantidade de packs; [desenvolvedor Godot, mai/2026](https://www.reddit.com/r/godot/comments/1toda72/building_a_tiny_open_world_in_godot_with_blender/) usa Blender para modelos e ferramentas, Godot para cenário jogável e IA para tarefas de fluxo; [criadores de Cities: Skylines II, dez/2025](https://www.reddit.com/r/CitiesSkylines2/comments/1pdxzry/official_update_patch_152f1_released_asset_editor/) alertaram sobre incompatibilidade potencial de assets antigos em atualização de pipeline. Esses relatos não provam qual ferramenta será mais barata para nós.

**Palestras em vídeo:** as apresentações GDC acima são referências técnicas úteis; **não afirmamos ter assistido/transcrito integralmente as gravações**. Priorizamos descrições oficiais e conteúdos escritos verificáveis.

## 2. Seis achados que alteram a estratégia

### A. Uma construção tem 4 identidades, não apenas um arquivo

1. **Tipo funcional** definido na SPEC (capacidade, economia, vagas, materiais).
2. **Envelope espacial e pontos de operação** (ocupação, entrada de SIM, doca, estacionamento, saída/chegada de frota, colisões).
3. **Família visual** (casa, loja pequena, comércio médio, apartamentos, indústria, serviço etc.).
4. **Variante visual** (telhado, parede, placa, cor, detalhes, estado visível).

A ARCHITECTURE já separa identificadores de edifício e visual; essa distinção é **direção aprovada**. **Hipótese de produção:** registrar dimensões e posições dos conectores num contrato pequeno e verificável, sem derivar capacidade automaticamente do tamanho da mesh. Acesso invisível/incompatível vira erro observável na integração. Uma troca de `VisualId` não deve recriar empresa/moradores nem quebrar saves antigos; caso uma variante desapareça, o sistema de apresentação deve ter mapeamento/fallback visual estável **a validar**, preservando a entidade real.

### B. Dois modos aumentam valor da legibilidade, não de meshes exclusivos

O Criativo aprovado permite construir sem depender de leitura ou administração financeira; o Convencional mantém economia física. Ambos compartilham apresentação e ferramentas. **Hipóteses a testar no jogo:**
- miniatura grande e padronizada, silhueta identificável, cor/placa como reforço (não depender só de texto ou cor);
- reconhecimento de casa/loja/hospital/posto/garagem no **zoom de construção**, não só num render próximo;
- ao selecionar, prévia/realce mostra footprint, orientação e **entrada física efetiva**; prédio alto não pode esconder indefinidamente objeto/rua selecionada (investigar transparência contextual/contorno somente se ocorrer);
- estados de obra, dano, falta de operação e funcionamento podem reutilizar andaime, sinalização, efeitos e materiais em vez de criar **uma mesh por estado**; representar fatos reais no Convencional, sem assumir simulação completa no Criativo;
- paisagem/vegetação/decoração e pequenas opções de aparência têm valor próprio mesmo sem efeito econômico; **catálogo exato continua aberto**.

**Nova prioridade:** incluir **teste de identificação por imagem sem ler texto** e teste de escolha/posicionamento com poucos passos na validação do asset — não apenas avaliação estética adulta.

### C. Geração procedural **offline**, como um compilador visual

Em vez de escolher entre "100 modelos manuais" e "cidade procedural em runtime": criar 1–2 famílias experimentais compostas por **massa simples + módulos de janelas/portas/telhados + paleta + regras de encaixe**. Blender Geometry Nodes/scripting, ou alternativa local, gera um lote de modelos exportáveis e prévias; selecionar/revisar versões boas e distribuir como assets estáveis. O [estudo Houdini](https://github.com/kiryha/Houdini/wiki/procedural-city) e a [palestra GDC](https://gdcvault.com/play/1012219/Building-Blocks-Artist-Driven-Procedural) sustentam a técnica; **não** provam vantagem de custo para o nosso caso sem experimento.

**Ideia de baixo custo:** cada variante guarda **receita + seed artística + versão do gerador + fonte dos módulos**, de modo que possa ser reconstruída se a direção visual mudar. **Não confundir** essa seed visual com a seed determinística do mundo/simulação da SPEC. Gerar variante em lote fora do jogo evita depender de execução procedural a cada construção.

### D. Mais variedade pode custar menos que mais materiais

Variar **silhueta e agrupamento de detalhes** quando útil, mas buscar materiais reutilizados e, onde combinar com o estilo, paleta/trim sheet/atlas compartilhados. Em [produção de mapas na MY.GAMES](https://mygames_dev.artstation.com/blog/dRWD/trim-sheets-tilemaps-and-terrain-how-we-remaster-game-maps), mini-atlases e densidade de texels coerente simplificam materiais; é caso de outro gênero, portanto hipótese aplicável, não métrica nossa. Para cores por instância, a Godot suporta [uniformes por instância](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shading_language.html) ou [MultiMesh por instância](https://docs.godotengine.org/en/stable/classes/class_multimesh.html), evitando clonar material para cada telhado colorido. **Cautela:** paleta única não deve apagar silhuetas nem diferenças de função.

### E. O gargalo pode ser **organização dos objetos renderizados**, não triângulos

A Godot possui [LOD automático](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html), [HLOD/faixas de visibilidade](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html), [occlusion culling](https://docs.godotengine.org/en/stable/tutorials/3d/occlusion_culling.html) e [MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html). **Detalhe decisivo:** instâncias muito espalhadas no mesmo MultiMesh não são descartadas individualmente; compartilharão a mesma seleção de LOD. **Candidato:** agrupar árvores e detalhes repetidos por **região + família**; separar prédios interativos e agentes/veículos conforme observabilidade, atualização e seleção necessárias. Testar dezenas/centenas/milhares em **hardware de referência**, medindo tempo de GPU, CPU da apresentação, draw calls, VRAM, carregamento e troca de zoom; **não** fixar limites de triângulos por ritual.

### F. IA 3D em 2026: três rotas, não uma aposta em fornecedor

| Rota | Melhor aplicação provável | Maior risco |
| --- | --- | --- |
| **Blender/Geometry Nodes + IA auxiliar** | Modelos-base, famílias padronizadas, variações editáveis, exportação em lote | Precisa inicializar templates e conferir modelos |
| **Gerador externo de 3D (ex.: Meshy/Tripo)** | Referência volumétrica, props, árvores, rascunhos de objeto exclusivo | Malhas/topologia, UV, estilos divergentes, custo oculto de limpeza e direitos |
| **Geometria nativa da Godot + agente** | Primitivas, prototipagem rápida, peças low-poly muito simples | Dependência de ferramentas específicas, limite de qualidade/portabilidade e pipeline menos uniforme |

**Caso novo de automação em Godot:** [godot-create-3d-assets](https://github.com/LuigiDeFacci/godot-create-3d-assets) (repositório MIT, julho de 2026) propõe fluxo de agentes para criar/revisar assets estilizados na engine, incluindo escala, colisão e multiângulos. É **experimento de comunidade pequeno**, não ferramenta validada no IndexCities. Em [relato de setembro de 2026](https://www.reddit.com/r/aigamedev/comments/1weswib/i_built_a_fully_local_text_3d_game_asset_pipeline/), outro desenvolvedor demonstrou proposta de geração local com GLB, LOD, colisão e validação, mas usando hardware de ponta; **não há evidência independente de custo ou qualidade para nossa produção**.

**Não escolher por velocidade do prompt.** Comparar custo total por asset *aceito*: **geração + limpeza + adaptação artística + exportação + QA + integração + retrabalho**. Em [comparação com uma única imagem (set/2026)](https://fuser.studio/articles/meshy-vs-rodin-vs-tripo), ferramentas entregaram diferenças muito grandes de triângulos e materiais; é um exemplo de risco, **não benchmark geral**.

**Licença é bloqueio real de produção comercial:** [Meshy — termos atualizados em setembro de 2026](https://www.meshy.ai/terms-of-use) e [guia oficial de direitos](https://help.meshy.ai/en/articles/9992001-can-i-use-meshy-assets-commercially-license-copyright-explained) distinguem pago (direitos conforme termos e fontes legítimas) de gratuito (**CC BY 4.0 com atribuição**); [Tripo — regras oficiais](https://www.tripo3d.ai/help/privacy-policy/how-to-use-tripo-models-commercially) indica direitos comerciais no pago, **não equivalentes no gratuito**. Upload de referência protegida não é saneado porque a ferramenta gerou a mesh. Registrar **fornecedor, plano/termo vigente, data, origem da referência, licença, direitos de distribuição e editabilidade**; preservar cópia local, pois conteúdo hospedado pode expirar.

## 3. Contrato mínimo de produção (uma ficha, não burocracia)

**Um registro por modelo-base/variante, não por prédio colocado na cidade.** Campos candidatos: `visual_id`, `family`, `compatible_types`, `source_path`, `export_path`, `size_m`, `front/pivot`, `entry_markers`, `license/provenance`, `status`, `thumbnail`. Acessos e ocupação de gameplay continuam controlados pelo núcleo/contrato espacial correspondente, não pelo asset arbitrário.

**Convenção técnica candidata:**
- **1 unidade 3D = 1 metro** como escala de autoria, Y vertical, frente e pivô explícitos; validar carros/SIMs/ruas **junto** com as edificações antes de fixar largura/altura dos modelos.
- Fonte editável (ex.: `.blend`) + exportação preferencial **glTF 2.0/GLB**; padronizar material, imagem, transform, nome, orientation e miniatura. [Godot — formatos](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).
- Primeiro usar configuração de importação da Godot; só se houver tarefa repetida fazer **pós-importação automática** via [EditorScenePostImport](https://docs.godotengine.org/en/stable/classes/class_editorscenepostimport.html) (inclusive C#), sem criar nova ferramenta de pipeline completa.
- **Gate barato de QA:** arquivo abre, escala/orientação plausíveis, não faltam texturas, front/pivot/entradas conferem, ID e origem válidos, imagem nos mesmos ângulos/zooms, e validação no [Khronos glTF Validator](https://github.com/KhronosGroup/glTF-Validator). Quando já houver rotina repetida, usar [Godot headless](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html) para detectar erro de importação. Não prometer que validador de GLB garanta arte, licença ou acesso funcional.
- **Versionamento:** arquivos de autoria e exports estáveis sob controle/versionamento ou armazenamento referenciado; manter `.import` versionado quando pertinente e ignorar cache `.godot/` conforme [orientação oficial](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html). Git LFS **só se** arquivos binários pesados passarem a justificar. IDs estáveis e fallback visual ajudam saves continuarem abrindo após troca de assets.

**Gerenciamento econômico de arte:** antes de criar mesh nova, perguntar **"muda silhueta, escala física, uso visível, acesso ou clareza de construção?"**. Se não, preferir variar cor/telhado/placa/módulo. Distribuir tempo de produção conforme **frequência de aparição × área visível × importância para o jogador × risco de incompatibilidade**, em vez de dar mesmo esforço a um banco de praça e uma garagem de ônibus.

## 4. Escala, dimensões e contagem: o que está aberto

A SPEC já decidiu **3D isométrico, grade lógica híbrida, posicionamento assistido, prédio individual, ruas em L, pontes/viadutos, 10 atividades comerciais com modelos compartilháveis e ausência de upgrades estruturais de prédio pronto**. Também decidiu **Criativo/Convencional** em 2026-10-11. **A opção 4A agora estabelece variantes completas prontas, sem edição modular pelo jogador no modelo inicial**; ainda não decidiu número de modelos/variantes, formatos obrigatórios, escala espacial definitiva, lotes, zoom e conjunto visual final.

**Referências para apenas um blockout de teste**, não requisitos: adulto ~1,7–1,8 m; carro ~4–5 m de comprimento; ônibus ~10–13 m; faixa urbana ~3,0–3,3 m; faixa livre de calçada ~1,8–2,4 m. Para 2 faixas e duas calçadas livres, seção ilustrativa ~9,6–11,4 m, **sem estacionamento/canteiros/mobiliário**. Fontes urbanísticas orientativas: [NACTO — lane width](https://nacto.org/publication/urban-street-design-guide/street-design-elements/lane-width/) e [calçada](https://nacto.org/publication/urban-street-design-guide/street-design-elements/sidewalks/sidewalk-design/). **Não fixar um lote universal de 8×12 m ou 16×24 m**; verificar famílias funcionais reais, recuos operacionais, estrada, curvas, entradas, área de carga, garagens e câmera.

Quantidades diferentes: (1) tipos funcionais do jogo, (2) famílias gráficas, (3) variantes, (4) instâncias renderizadas. Um **comércio pequeno** com módulos e uma variante de fachada pode servir a atividades diferentes sem criar nova economia; **posto/hotel/refinaria** têm requisitos físicos diferentes. A escolha do negócio não deve alterar magicamente o imóvel. Número exato só após matriz **tipo → família → envelope/acessos → variantes possíveis** baseada na SPEC e teste visual.

## 5. Experimento curto que realmente decide a estratégia

**Executar no jogo integrado, sem fase obrigatória de POC isolada:**

1. **Escolher art bible de 1 página e câmera em 2–3 distâncias**: cinco imagens de referência e exemplos de bom/ruim, paleta/material, estilo de telhado, tamanho legível de portas/placas. A cena de teste deve servir ao Criativo (seleção por imagem) e ao Convencional (funcionamento real).
2. **Kit provisório ~10–15 modelos** (tamanho de experimento, não escopo oficial): SIM, carro, ônibus, ruas/calçadas/esquinas, casa, prédio residencial, loja pequena, comércio médio, uma construção especializada, árvore/vegetação e detalhes. Montar **um quarteirão com repetição intencional** e conexão real.
3. Para uma **única família (ex.: comércio pequeno)** comparar três fluxos: **A** modelo manual/reusado no Blender; **B** montagem de peças/regra offline; **C** IA 3D com limpeza e exportação. Medir **tempo total para ficar aceito**, clareza nas miniaturas, compatibilidade com entradas, licença e qualidade após alterar a paleta/escala. Testar geometria direta na Godot apenas para props/volumes simples em que faça sentido.
4. Conferir **10, 100 e, se útil, ~1000 instâncias renderizadas** e 2–3 níveis de zoom. Medir CPU/GPU, draw calls/VRAM e ocorrência de objetos ilegíveis/oclusão; são cenários de comparação, **não metas oficiais de FPS**.
5. **Critério para liberar produção em lotes:** proporções e câmera não exigem refazer tudo; SIMs/veículos acessam prédios; miniaturas reconhecíveis sem texto; GLB/fonte/referência/licença confiáveis; importações reproduzíveis; ganho visual compensa custo; gargalos de render conhecidos. Se falhar, corrigir **uma família** antes de gerar 100 arquivos.
6. Só então montar catálogo leve de prioridades (tipos que aparecem mais, prédios funcionalmente únicos, ruas/veículos/vegetação, estados visuais realmente necessários), produzir em lotes pequenos e reavaliar com screenshots do bairro.

## Decisão recomendada ao responsável (não aprovada)

**Aceitar a direção híbrida de produção** e liberar o kit de validação de escala/arte; **não** fechar agora contagem final de modelos, orçamento universal de polígonos/texturas nem fornecedor de IA; **não** autorizar sistemas novos de gameplay apenas porque um asset permite. O maior risco não é faltar prédio bonito, mas acumular **arte incompatível com espaço, estilo, simulação, saves ou direitos comerciais**.

O ganho mais promissor **fora da caixa** é construir a *variedade* como **produto de um pequeno vocabulário artístico controlado**, usando um "compilador de variantes" **fora do jogo** só se o teste medir vantagem; **não** transformar a cidade numa rede de geometrias dinâmicas. O maior aprendizado dessa última rodada é acrescentar ao padrão visual **legibilidade para quem não lê, contrato físico estável, rastreio de licença, portabilidade e custo total de manutenção**.
