# IndexCities — Pesquisa de assets 3D: produção, escala e gestão

> **EXPLORAÇÃO — não é fonte de requisitos. Revisão humana: PENDENTE.** Estudo ampliado em 2026-10-10 a partir de jogos, devlogs, palestras em vídeo, documentação técnica, notícias e relatos de comunidades. Recomendações abaixo **não estão aprovadas**. A [SPEC](../SPEC.md) define o produto; a [ARCHITECTURE](../ARCHITECTURE.md) separa simulação e apresentação.

## Conclusão

**A melhor aposta para o IndexCities é uma fábrica híbrida de assets:** poucos **modelos-base por família física**, peças compartilhadas (portas, janelas, telhados, detalhes, letreiros), variantes de materiais e composição **semi-automática na autoria**, com **construções especializadas feitas sob medida** quando necessário. O jogo instancia o resultado visual apropriado, sem criar empresas, funções econômicas ou capacidades a partir da mesh. Não criar por antecipação um editor de prédios para o jogador, nem um gerador procedural complexo em tempo real.

**Podemos começar já** com direção visual, padrões métricos, blockouts e importação. **Não começar em massa** antes de visualizar um pequeno bairro integrado com câmera, ruas, SIMs e veículos em escala. A ausência de contagem definitiva dos edifícios **não impede essa preparação**.

## 1. Casos reais: o que aprender e o que NÃO copiar

| Jogo / evidência | Aprendizado aplicável | Armadilha a evitar |
| --- | --- | --- |
| **SimCity (2013)** — [palestra em vídeo da Maxis/GDC](https://www.gdcvault.com/play/1019107/Building-SimCity-Art-in-the) | Arte deve tornar a **simulação legível**: edifícios, redes, veículos e habitantes comunicam como a cidade funciona | Detalhe ornamental bonito sem leitura de estado/uso |
| **Foundation** — [descrição e discussões da comunidade](https://steamcommunity.com/app/690830/discussions/0/600770111531419420/), [problemas com subedifícios](https://steamcommunity.com/app/690830/discussions/2/4361247613257623565/) | Peças de arquitetura e funções podem se combinar em construções variadas | Copiar montagem manual, ampliações ou gerenciamento de partes pelo jogador: **contradiz** o modelo inicial do IndexCities |
| **Workers & Resources: Soviet Republic** — [guia oficial de modelagem](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Modelling), [editor](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Building_editor) | Escala métrica e **pontos funcionais**; modelos podem ter elementos/etapas distintos vinculados à construção | Misturar componentes decorativos com regras físicas arbitrárias ou copiar formatos proprietários |
| **Transport Fever 2** — [modelo/LODs/metadados](https://www.transportfever2.com/wiki/doku.php?id=modding%3Aresourcetypes%3Amdl), [construções parametrizadas](https://www.transportfever2.com/wiki/doku.php?id=modding%3Aintroduction) | Geometria, variantes, parâmetros funcionais e níveis de detalhe são coisas diferentes | Transformar cada variante visual em entidade de gameplay |
| **Tiny Glade** — [entrevista com os criadores](https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games) | Regras de combinação de peças criadas por artistas podem dar **variedade sem modelar tudo individualmente** | Reproduzir seu sistema de arquitetura procedural interativa: não é a gameplay aprovada |
| **Cities: Skylines II** — [lançamento do editor beta em 04/12/2025](https://www.paradoxinteractive.com/zh-CN/games/cities-skylines-ii/news/asset-mods-patch-notes) e [melhorias divulgadas em 11/08/2026](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/expanding-the-modding-tools) | O gargalo também é **achar, editar, testar e distribuir assets**; busca por nomes/IDs e redução de cliques são importantes | Investir cedo em editor sofisticado; nossa ferramenta interna pode ser **uma lista + prévias + scripts simples** |
| **Factorio** — [processo de criação da arte](https://www.factorio.com/blog/post/fff-146), [renderização/texturas](https://www.factorio.com/blog/post/fff-227) | Primeiro validar uma entidade com arte provisória; só depois finalizar. Materiais/atlas e organização importam em cidades repetitivas | Otimizar texturas e meshes sem medir o impacto real em GPU e memória |
| **Real City RTS, devlog 15 (12/04/2026)** — [relato técnico](https://itch.io/devlog/1484215/devlog-15-making-the-city-scalable.amp) | Renderizar **cada prédio como um objeto independente** não escalou no caso apresentado; agrupamento espacial e LOD por tamanho aparente resolveram o gargalo | Assumir que combinar todas as meshes é sempre bom: edifícios interativos exigem identidade e atualização próprias |
| **Produção profissional de arte isométrica, 2026** — [SunStrike Studios](https://sunstrikestudios.com/en/blog/isometric-city-builder-art/) | Uma referência visual compacta, **câmera e escala primeiro**, bairro amostral e depois kits modulares; silhueta importa mais que minúcia | Copiar estimativas comerciais, 2D pré-renderizado, monetização/live ops ou tamanhos de tile como regras do IndexCities |

**Sinais de quem realmente está produzindo:** em [relato de criador no Reddit (jun/2026)](https://www.reddit.com/r/CitiesSkylines2/comments/1ufay13/i_figured_out_how_to_do_building_assets/) importação exigiu cuidados com materiais e meshes; [outros criadores (mai/2026)](https://www.reddit.com/r/CitiesSkylines2/comments/1tfsvn5/asset_creation/) descrevem curva de aprendizagem e requisitos de textura. Em [experimento Godot (jun/2026)](https://www.reddit.com/r/godot/comments/1uithfi/procedurally_generating_assets/) fachadas procedurais poupam modelagem, mas centenas de operações CSG precisam ser convertidas/medidas, não deixadas rodando ingenuamente. São **relatos individuais**, não benchmarks reproduzíveis.

**Referências em vídeo de maior valor:** [GDC — *Building SimCity: Art in the Service of Simulation*](https://www.gdcvault.com/play/1019107/Building-SimCity-Art-in-the); [GDC — *Building Blocks: Artist-Driven Procedural Buildings*](https://www.gdcvault.com/play/1012930/Building-Blocks-Artist-Driven-Procedural); [GDC — *Procedural and Automation Techniques for Sunset Overdrive*](https://www.gdcvault.com/play/1022216/Procedural-and-Automation-Techniques-for). Esta pesquisa usa **descrições oficiais verificadas das apresentações**, não alega ter assistido ou transcrito integralmente os vídeos.

## 2. Estratégias possíveis

| Abordagem | Ganho | Custo/risco | Parecer |
| --- | --- | --- | --- |
| Um arquivo 3D exclusivo por edifício/ramo | Autoria direta; identidade específica | Cresce linearmente, repetição de trabalho, difícil corrigir escala | **Não** como regra geral |
| Poucos modelos prontos + skins | Barato, previsível, fácil de medir | Silhuetas repetidas; fraco para funções especiais | **Base inicial útil**, insuficiente sozinho |
| Prédios totalmente procedurais em runtime | Grande variedade e footprints adaptativos | Engine de telhados, UV, colisão, portas, LOD e bugs; esforço alto | **Não** como requisito inicial |
| **Híbrido: famílias, módulos e variantes pré-montadas** | Equilíbrio entre diversidade, controle de custo e desempenho | Exige disciplina de escala e compatibilidade das peças | **Recomendado** |

**Ideia além do básico:** criar uma **biblioteca de regras de fachada para o artista**, que compõe variações **antes de importar ao jogo** (por exemplo, via Blender/Geometry Nodes ou scripts), usando telhados, janelas e letreiros compatíveis. Os resultados são versões 3D comuns, fáceis de revisar/perfilar. Não precisamos deixar um gerador inteiro rodando na simulação. Guardar **receita/seed da variante visual** pode permitir regeneração futura; a identidade do prédio e a seed do mundo continuam preservadas conforme a SPEC. [Palestra GDC sobre fachadas procedurais](https://www.gdcvault.com/play/1012930/Building-Blocks-Artist-Driven-Procedural); [instâncias no Blender](https://docs.blender.org/manual/en/latest/modeling/geometry_nodes/instances.html).

## 3. Quantidades: uma distinção essencial

Quatro números diferentes **não devem ser confundidos**:

- **Tipos funcionais da SPEC:** casa, apartamentos, atividades comerciais, instalações municipais, fábricas, fazendas, transporte, extração etc.
- **Famílias gráficas-base:** casas, prédios residenciais, loja pequena, comércio médio, indústria, serviços municipais, estruturas especializadas, vias/props, veículos/SIMs.
- **Variantes visuais:** fachada, telhado, placa, textura, cor e detalhes; só ganham lógica própria quando a SPEC já exigir função física distinta.
- **Instâncias:** os muitos prédios da cidade, cada um com sua entidade, operação, propriedade e moradores próprios.

Já há **dez atividades comerciais** definidas, **não dez modelos exclusivos exigidos**. Loja pequena/comércio médio podem compartilhar aparência; posto, hotel e estruturas com logística/carga/frota exigem dimensões e acesso próprios. **Número final de bases e variantes: ainda aberto.** Casas e apartamentos terão diversidade visual, sem inventar upgrades de prédios prontos (proibidos na SPEC atual).

## 4. Medidas: começar pelo que interage fisicamente

**Escala de autoria recomendada para teste: 1 unidade Godot = 1 metro**, sem escalonar assets individualmente na importação. Medidas abaixo são **propostas de referência**, não decisões de produto:

| Elemento | Referência de teste | Validação necessária |
| --- | --- | --- |
| SIM adulto | ~1,7–1,8 m de altura | Visibilidade no zoom real, porta e calçada |
| Carro | ~4–5 m comprimento × ~1,8–2,0 m largura | Faixa, estacionamento, curvas e distância entre veículos |
| Ônibus | ~10–13 m comprimento × ~2,5 m largura | Giro, ponto de parada, acesso à garagem |
| Faixa urbana | ~3,0–3,3 m largura | Trânsito, carga e ônibus; referência NACTO |
| Calçada **livre** | ~1,8–2,4 m por lado | SIMs cruzando, ponto de ônibus e mobiliário |
| Rua simples: 2 faixas + 2 calçadas livres | ~9,6–11,4 m **sem estacionamento nem faixa de árvores** | Medir esquina, conexão com edifícios e sobreposição com lotes |
| **Envelopes ilustrativos** (não lotes obrigatórios) | Casa ~8×12 m; loja pequena ~8×12 m; comércio médio ~16×24 m | Testar 2–3 alternativas por família; não impor gabarito único |
| Apartamentos/hotel/refinaria/garagem/fazenda | **Sem dimensão prefixada** | Área, unidades, manobra, equipe, cargas e capacidade reais |

[NACTO — largura de faixas](https://nacto.org/publication/urban-street-design-guide/street-design-elements/lane-width/) e [calçadas](https://nacto.org/publication/urban-street-design-guide/street-design-elements/sidewalks/sidewalk-design/): referências urbanísticas externas para orientar proporção, não regulamentação nem escala obrigatória. A experiência de [Workers & Resources com unidades métricas](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Modelling) reforça o benefício de usar medidas físicas consistentes.

**Definir três geometrias distintas por construção:** (1) **mesh visual** e possíveis beirais/volumes; (2) **área física ocupada/colisão** usada pelas regras espaciais; (3) **acessos e zonas operacionais** (entrada de SIM, veículo, carga, parada, garagem, doca). Uma porta visível sem ponto acessível **não é** acesso real. O grid do IndexCities é lógico e híbrido: **não arredondar todos os edifícios a tiles inteiros**, não sacrificar orientações nem forçar recuos arbitrários. Pontes, viadutos e ruas em L precisam ser testados com suas próprias peças/geração geométrica.

**Regra de dimensionamento que reduz retrabalho:** primeiro fechar **SIM + carro/ônibus + rua/calçada + câmera/zoom**; depois produzir envelopes de casa/comércio; só então especialistas. **Aprovar a escala por cenas**, não apenas por uma tabela de metros.

## 5. Como criar e organizar sem burocracia

**Contrato simples de entrega por asset** (recomendação):
- **ID visual estável**, família, variante e tipo(s) de edifício compatíveis; gameplay usa IDs semânticos independentes, como já especifica a ARCHITECTURE.
- Arquivo **fonte editável** (ex.: `.blend`), entrega **`.glb`/glTF 2.0** e uma prévia padronizada; cada resultado deve informar **origem/licença** (especialmente IA, terceiros e fornecedor).
- Dimensões em metros, piso em Y=0, pivô consistente, orientação frontal definida, footprint candidato e marcadores de entradas. Godot usa Y vertical; para modelos orientados o **+Z é a frente do asset** (distinto do **-Z** à frente da câmera).
- Texturas e materiais simples, paleta coerente, sem dezenas de materiais únicos por casa; modelos completos não devem virar centenas de nós decorativos no runtime.

**Por que GLB:** é um dos formatos recomendados pelo [importador da Godot](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html) e mantém meshes e texturas empacotadas. `.blend` direto também é suportado quando Blender está instalado, mas o `.glb` cria uma entrega independente do software de autoria. Ver [eixos e exportação](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html).

**Organização candidata, sem nova plataforma de gestão:**
- **Biblioteca de autoria**: famílias e módulos fonte, preferivelmente reutilizáveis no [Asset Browser do Blender](https://docs.blender.org/manual/en/latest/files/asset_libraries/index.html).
- **Biblioteca de jogo**: `.glb`, materiais/cenas de apresentação e acesso por ID visual; sem dependências da simulação nos caminhos dos arquivos.
- **Índice leve** (CSV/YAML ou tabela versionada) com ID, família, estado `referência → blockout → revisão → aceito`, dimensão, fonte/licença, caminho, data/versão e screenshot; **não** duplicar um cadastro por instância colocada no mapa.
- **Prévia automática**: sempre mesma luz, câmera e 2–3 níveis de zoom. Uma *contact sheet* de dezenas de miniaturas lado a lado expõe inconsistência de escala, cor e repetição **antes** de lançar a cidade.
- **Validação barata** na entrega: verificar `.glb` com [validador oficial Khronos](https://github.com/KhronosGroup/glTF-Validator), conferir pivot/dimensões, textura, ID duplicado, importação Godot e aspecto na cena-padrão. Sem criar um framework de pipeline antes de um problema recorrente.
- **Arquivos pesados:** adotar [Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage) **somente se o volume dos fontes binários justificar**; avaliar armazenamento de originais separado do repositório de execução, com versionamento/referências verificáveis. Evitar poluir o Git com centenas de exportações temporárias.

**IA na criação:** útil para referências/conceitos, variações, texturas e até proposta inicial de malha, **não** passe automático para o jogo. Verificar direitos, coerência com a linguagem visual, geometria editável, escala, UV, pivôs, materiais e silhueta em Godot. Para protótipos com uso comercial permitido, [Poly Haven CC0](https://polyhaven.com/license) é fonte verificável, mas misturar pacotes de estilos diferentes sem direção visual tende a prejudicar o conjunto. A licença de **cada** fornecedor/modelo de IA deve ser examinada antes da adoção.

## 6. Performance: projetar pelo que a câmera enxerga

- **De perto:** detalhes úteis para identificar lojas, portas, veículos, obras e estado de funcionamento. **De longe:** silhueta, altura e cor importam mais que puxadores/janelas individuais. Tomar decisões pelo **tamanho visível na tela** e zoom, como no [devlog de Real City RTS (2026)](https://itch.io/devlog/1484215/devlog-15-making-the-city-scalable.amp).
- **Compartilhar materiais/geometria** entre prédios repetidos, árvores e props, sem impedir aparência individual; comparar draw calls, GPU, CPU e VRAM. **Godot gera LOD de mesh automaticamente** em cenas 3D importadas; experimentar [LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html) primeiro e [faixas de visibilidade/HLOD](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html) se medir ganho.
- Para objetos muito repetidos, testar [MultiMesh por região e família](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html), **não um MultiMesh único para a cidade**: o descarte por câmera não ocorre individualmente por instância do mesmo conjunto. Árvores e detalhes estáticos são candidatos mais seguros; prédios selecionáveis, veículos e SIMs exigem tratamento de estado/movimento sem perder identidade.
- **Não impor limite universal** de triângulos, textura 512/1024, materiais ou draw calls sem medir na câmera e hardware-alvo. A cena-teste deve medir **dez, centenas e milhares de instâncias** com padrões realistas de repetição. A simulação continua independente da apresentação — não criar um `Node3D` por SIM só porque ele existe no núcleo.

## 7. Caminho recomendado: pequeno e verificável

1. **Art bible de 1 página:** referências de estilo (3D estilizado, leitura limpa), paleta provisória, silhuetas por domínio (residencial, comercial, industrial e municipal), limites de detalhe em 2–3 zooms e 1 modelo bom/ruim de exemplo.
2. **Kit de escala, não catálogo:** ~10–15 **amostras provisórias** que cubram SIM, carro, ônibus, via/calçada/esquina, casa, prédio de apartamentos, loja pequena, comércio médio, construção especializada e vegetação. É **tamanho de teste**, não requisito de POC separada nem número final de assets.
3. **Cena real integrada de um bairro:** gente/veículos em movimento, entradas e acesso reais, fachadas repetidas, obra/estado quando pertinente. Conferir com o mesmo zoom, hora do dia e câmera; anotar espaço, legibilidade e custo gráfico.
4. **Aprovar envelopes e estilo, gerar ficha do catálogo:** uma linha por *família* com função física, variantes possíveis, entradas, tamanho/porte e prioridades. **Quantidade exata nasce daí**, não de extrapolação das dez atividades comerciais.
5. **Produção em pequenos lotes:** criar/reutilizar peças, exportar, validar automaticamente o básico, inspecionar as prévias, testar no jogo. Só adicionar um novo modelo-base se mudar realmente a **silhueta, o porte, o acesso ou a função visível**. Caso contrário, tentar combinação de módulos/material.
6. **Rever a estratégia quando houver evidência:** se os kits ficarem repetitivos, expandir as famílias; se um tipo exigir acessos especiais, modelo dedicado; se o custo de montagem crescer muito, considerar geração offline de variantes. **Nenhum sistema procedural genérico vira obrigação automática.**

### Pendências de decisão de maior impacto

- **Direção artística final + câmera/zoom:** as referências já existentes são ponto de partida, mas não especificação final.
- **Geometria de rua, calçada e acesso** validada com veículos e SIMs: destrava dimensões das construções.
- **Contrato mínimo com quem produzir os modelos**: fonte editável, licença comercial, formatos e critério de aceite; depois escolher quem produz/qual IA usar.

**Resultado:** já dá para iniciar **a produção de referência e o pipeline leve**. Falta validar escala e leitura na Godot antes de disparar a produção massiva. O IndexCities deve gerir **famílias de formas e significados**, não uma coleção desordenada de arquivos 3D.
