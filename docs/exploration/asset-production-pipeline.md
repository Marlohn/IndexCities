# IndexCities — Assets 3D: revisão do pipeline e evidência procedural

> **EXPLORAÇÃO — não é fonte de verdade. Revisão humana: PENDENTE.** Auditoria técnica de 2026-10-11. As decisões aprovadas são apontadas na [SPEC](../SPEC.md) e na [ARCHITECTURE](../ARCHITECTURE.md); propostas técnicas, custos e medidas abaixo **não** são requisitos aprovados.

## Resumo executivo

**Hipótese de pipeline técnico da pesquisa, ainda não aprovada: produzir modelos paramétricos novos diretamente na Godot com auxílio de IA, gerar a geometria uma vez e manter recursos e variantes reutilizáveis.** O teste visual apresentado pelo responsável mostrou potencial concreto para casas, comércio, rua, vegetação e mobiliário. **Blender não é necessário nesse fluxo**; pode ser usado pontualmente se um tipo de modelo não ficar bom por código. Geradores text-to-3D externos também são alternativas, não dependências.

**Não confundir:**
- **Modelagem procedural na autoria** = scripts que criam/atualizam meshes e cenas antes do uso ou sob demanda controlada. **Recomendada para experimentar**.
- **Geração procedural da cidade durante a partida** = criação/alteração dinâmica de geometria e layout quando o mundo precisar. **Não está sendo aprovada ou exigida aqui**.
- **Editor modular para o jogador** = combinar peças de cada edifício dentro do jogo. **Não integra o primeiro modelo**: a SPEC aprovou modelos completos e variantes prontas.

É possível preparar e produzir assets **sem conhecer a contagem final de edifícios**. A produção em volume deve esperar uma validação curta de escala, integração, diversidade e desempenho. **Não existe POC isolada obrigatória**: pode acontecer em incrementos do jogo integrado.

## 1. O que o IndexCities já decidiu

| Oficial na SPEC/ARCHITECTURE | Consequência na produção |
| --- | --- |
| Jogo 3D com câmera isométrica; cidade legível e construções individuais | Testar edifícios e ruas no enquadramento do jogo, não somente em close-up |
| Grade lógica com posicionamento assistido; acessos, ocupação e vias reais | O desenho do prédio **não define sozinho** seu footprint, a validade da posição nem a conexão viária |
| Reuso de modelos-base para comércio; exceções físicas quando necessárias | Dez atividades comerciais **não exigem** dez meshes exclusivos; posto/hotel podem precisar de estrutura própria |
| Modos **Criativo e Convencional** com a mesma apresentação básica | Miniaturas/silhuetas reconhecíveis sem leitura, estados coerentes com cada modo |
| No primeiro modelo, **variantes visuais prontas**, sem montagem livre por peças pelo jogador | Peças e regras podem ser modulares **nos bastidores**, mas o catálogo entrega modelos/variantes completos |
| Sem upgrade/ampliação estrutural de edifício pronto | Não produzir por inferência cadeias de estágios de ampliação |
| Simulação independente de cenas/meshes/arquivos de arte | Trocar `VisualId` ou a mesh não deve recriar empresa, propriedade, empregados, moradores ou regras |

**Ainda aberto:** estilo final aprovado, câmera/zoom exatos, medidas de ruas/calçadas e footprints por família, número de modelos/variantes, custo gráfico por cena, ferramenta de autoria definitiva e catálogo exato de decoração. Isso **não** reabre decisões já fechadas.

## 2. O que o teste visual realmente demonstrou

**Origem da evidência:** duas capturas enviadas pelo responsável de **cena estática criada por IA em outro projeto**, mais descrição das funções em `cena.gd`. **O script e o projeto executável não foram disponibilizados nesta auditoria**, então não houve inspeção do código, benchmark nem verificação de animação/exportação.

**Observável nas capturas:** quarteirão com café, casas, cruzamento, calçadas proporcionais, vegetação coerente, mobiliário, janelas iluminadas e iluminação quente. A cena tem linguagem consistente, integração dos lotes com a rua e acabamento low-poly suficientemente convincente para **validar essa direção estética como candidata forte** — não garante por si qualidade de todos os tipos futuros nem desempenho de uma cidade inteira.

**Técnica relatada:** (a) primitivas de triângulos: caixas chanfradas, icosferas facetadas, cilindros, cones, prismas, polígonos extrudados; (b) composição paramétrica: `window()`, `roof()`, `house()`, `tree()`; (c) paleta compartilhada, materiais e shaders de superfície. O resultado resulta também de **proporções, colocação dos objetos e iluminação**, não só da geometria.

**Não foi demonstrado:** custo de geração, tempo de manutenção ao crescer o catálogo, tamanho em disco/memória, capacidade de editar centenas de variantes, draw calls, FPS, seleção individual de edifícios, acessos de SIMs/cargas, colisões, LOD, licenças e animação avançada. Não herdar mecânicas ou a arquitetura do projeto externo.

## 3. Alternativas e preferência técnica exploratória — sem decisão

| Rota | Onde tende a ser útil | Limitação principal | Ordem de preferência |
| --- | --- | --- | --- |
| **IA + geradores Godot (GDScript/C#)** | Low-poly geométrico, casas, comércios, fachadas, ruas, vegetação, props e veículos estilizados simples | Código dos geradores pode ficar difícil de manter; precisa validar repetição/otimização | **Primeira candidata** por evidência prática |
| **Blender automatizado ou manual** | Malhas que exijam edição/UV/rigging especializado, exportação interoperável | Mais um ambiente e mais passos, apesar de o software ser gratuito | **Opcional conforme necessidade** |
| **Serviços de IA que entregam 3D pronto** | Rascunhos/objetos específicos e formas difíceis de obter por primitivas | Limpeza da malha, estilo, custo por asset aceito e licença | **Experimento pontual**, não padrão |
| **Montagem procedural em runtime** | Geometria que realmente dependa da edição/mudança do mapa (ex.: determinados segmentos viários) | Custo de CPU, cache, atualização e integração | **Só quando houver necessidade concreta** |

**Blender vs. Godot não é uma decisão de qualidade universal.** Para o estilo observado, o fluxo direto é especialmente promissor. Se personagens articulados, animações complexas ou construções difíceis exigirem outra ferramenta, escolhê-la pela necessidade **não invalida o restante do catálogo**.

## 4. Exemplo de fluxo possível para produzir e manter modelos — não obrigatório

Um conjunto mínimo de responsabilidades, sem impor novas camadas arquiteturais:

1. **Biblioteca de formas e famílias:** poucas primitivas confiáveis; funções por família (casa, loja, árvore, etc.); composição por parâmetros de dimensões, telhado, aberturas, acabamento e paleta. Evitar duplicar scripts inteiros a cada variante ou concentrar toda a cidade num `cena.gd` monolítico.
2. **Receitas e variantes:** produzir as variantes com parâmetros controlados e, se houver aleatoriedade estética, registrar seed/versão **daquela variante**. A seed artística é distinta da seed de mundo e não cria variação econômica. Revisar visualmente o resultado antes de incluí-lo no catálogo.
3. **Resultado reutilizável:** preferir **gerar/atualizar no editor ou em ferramenta de build**, salvar meshes (`.tres`/`.res`) e cenas (`.tscn`/`.scn`) quando adequado e reutilizar recursos da mesma variante, em vez de recalcular triângulos a cada construção. O **gerador + parâmetros** continuam fonte editável; a malha salva é resultado derivado. Ao serializar uma cena com `PackedScene.pack()`, conferir o `owner` dos nós filhos e verificar erros de `ResourceSaver.save()`: salvar apenas o nó raiz não garante incluir todos os descendentes. Conferir também carregamento numa **build exportada**, não só no editor.
4. **Vínculo com o jogo:** separar **tipo funcional**, **área de ocupação/portas/pontos de operação**, **família gráfica** e **variante**. A ARCHITECTURE já prevê tipos de construção e `VisualId`; não introduzir camadas extras por ritual. Guardar identificador visual estável e tratar atualizações sem destruir identidade em saves.
5. **Prévia e origem:** manter miniatura no mesmo ângulo/iluminação, dimensões, ID, versão/receita, referência/ licença e estado da variante. Uma lista versionada pequena basta; não criar um sistema de gestão de arte separado antes de existir gargalo.

**Correção importante da documentação anterior:** **GLB não é obrigatório** para meshes criadas na Godot. Os formatos nativos de recursos/cenas são suficientes quando o pipeline opera inteiramente na engine; **glTF/GLB são intercâmbio opcional** com ferramentas externas. O validador glTF não valida `.res`/`.tscn`. Recursos nativos **não passam pelo importador 3D que gera LOD automaticamente**, e arquivos exportados/caches precisam ser verificados na build distribuída. Arquivos `.import` de fontes externas armazenam configurações e são versionados; `.godot/` é cache regenerável e geralmente não é versionado. Fontes: [ArrayMesh](https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/arraymesh.html), [PackedScene](https://docs.godotengine.org/en/stable/classes/class_packedscene.html), [ResourceSaver](https://docs.godotengine.org/en/stable/classes/class_resourcesaver.html), [LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html) e [importação](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html).

## 5. Escala e contrato físico

**Referência técnica recomendada: 1 unidade 3D = 1 metro, Y para cima**, em linha com a [documentação da Godot](https://docs.godotengine.org/en/stable/tutorials/3d/introduction_to_3d.html). Essa unidade **não** determina o tamanho do grid ou dos lotes. **Não escolher GridMap por conveniência do motor**: o GridMap visual rigidamente celular não substitui o posicionamento híbrido aprovado; pode servir como recurso técnico local apenas onde for compatível.

Definir por família (quando implementada) uma medida verificável e um pivô/orientação consistente, por exemplo no solo. **Não fixar por antecipação um lote universal**. Diferenciar:
- **Mesh desenhada:** pode ter beiral, jardim, floreira e detalhes que não definem área comercial/capacidade.
- **Footprint/ocupação válida:** regra do mundo, colisão e área efetiva.
- **Entradas e zonas operacionais:** local onde SIM, carro, caminhão, passageiro ou serviço realmente pode chegar/manobrar. Uma porta pintada não cria acesso.
- **Prédios especializados:** dimensionar posto, hospital, garagem, depósito, fazenda etc. segundo função e espaço; não apenas multiplicar uma loja por escala.

Para começar, montar **uma relação coerente entre SIM, carro, ônibus, faixa, calçada e edifício** e conferir em câmera isométrica e próxima. Referências urbanísticas (ex.: [NACTO — faixa viária](https://nacto.org/publication/urban-street-design-guide/street-design-elements/lane-width/)) podem ajudar proporção, mas **não obrigam** dimensões da cidade fictícia. As capturas têm rua/calçada bem proporcionadas; **não fornecem cotas métricas confiáveis**, logo não justificam travar números com base em pixels.

## 6. Performance: riscos verdadeiros e o que medir

**A geração de geometria procedural em Godot via ArrayMesh/SurfaceTool usa CPU.** Por isso, os edifícios estáticos podem ser gerados uma vez e compartilhados. O peso posterior de desenhá-los depende de **malhas, materiais/superfícies, sombras, luzes, instâncias visíveis, transparências, resolução e câmera**, além do número de triângulos.

- **Reusar meshes e materiais** sempre que fizer sentido. **Agrupar por material dentro do edifício** pode ser bom, mas **não unir a cidade inteira em uma mesh**: perderíamos oportunidades de ocultação por câmera e atualização/seleção de imóveis. Preservar partes que precisam mudar (como letreiro, indicação de obra ou estado de funcionamento) sem obrigar reconstruir todo o modelo.
- Para árvores/pequenos objetos repetidos, **testar MultiMesh por região e família**; não existe culling individual dentro de um mesmo MultiMesh. Deixar identidade e interação independentes do desenho.
- **LOD automático é gerado no processo de importação de cenas 3D**; não assumir que uma mesh criada por código e salva em `.res` recebeu LOD. Se houver gargalo, experimentar versões simples por distância/zoom e faixas de visibilidade (HLOD); decidir com medições.
- Medir separadamente **tempo de criação/regeneração** de modelos e **CPU/GPU da cena renderizada**. Medir também draw calls, materiais, memória, carregamento e transição de zoom no hardware-alvo. Mil instâncias por si só não provam nem refutam viabilidade; depende de como são construídas/exibidas.
- **Nunca substituir as regras reais dos SIMs do modo Convencional por animações decorativas para baratear renderização.** A apresentação pode ocultar ou simplificar o visual de entidades fora da câmera sem desligar a simulação exigida.

Fontes primárias: [Godot — geometria procedural](https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/index.html), [ArrayMesh](https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/arraymesh.html), [MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html), [Mesh LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html) e [faixas de visibilidade](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html).

## 7. O que vale aprender dos outros jogos (sem copiar a gameplay)

| Evidência | Aplicação prática | Limite |
| --- | --- | --- |
| [GDC / Maxis: *Building SimCity — Art in the Service of Simulation*](https://www.gdcvault.com/play/1019107/Building-SimCity-Art-in-the) | **A apresentação deve explicar a cidade**: tipos e estados dos imóveis reconhecíveis | Palestra indica direção; não comprova escala/custo na Godot |
| [Foundation — edifícios modulares, wiki oficial](https://wiki.polymorph.games/foundation/Buildings) | O **vocabulário de componentes** pode criar variedade | Não trazer montagem de edifícios por peças para o jogador |
| [Workers & Resources — guia oficial de modelagem](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Modelling) | Pensar em **espaço e pontos funcionais** dos edifícios | Não importar formato nem sistemas desse outro jogo |
| [GDC: *Building Blocks — Artist Driven Procedural Buildings*](https://gdcvault.com/play/1012219/Building-Blocks-Artist-Driven-Procedural) | Compor silhuetas/fachadas a partir de regras | Não exige Houdini nem geração procedural em runtime |
| [Godot — infraestrutura para geometria, cache e LOD](https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/index.html) | Solução acessível sem DCC obrigatória, **mas** com limites claros de desempenho | Manuais demonstram APIs, não benchmarks do IndexCities |

**Cuidado com evidência:** a pesquisa anterior incluiu notícias, posts de Reddit, vídeos e exemplos de ferramentas. Eles são úteis para levantar hipóteses, **mas não foram benchmarks reproduzíveis nem substituem a cena já demonstrada**. Mantemos aqui os exemplos diretamente relevantes e documentação primária; não alegamos ter assistido integralmente às palestras ou executado projetos de terceiros. Relatos individuais e comparadores comerciais não justificam adotar um fornecedor específico.

**Direitos de uso:** modelos e shaders escritos para o projeto, modelos baixados, ferramentas de IA, texturas e referências externas podem ter **situações jurídicas diferentes**. Registrar origem e termos aplicáveis aos recursos de terceiros; não inferir que um asset é automaticamente livre para redistribuição porque foi gerado por IA. Não reproduzir literalmente elementos protegidos de uma imagem de referência.

## 8. Possível validação pequena e integrada — sugestão

**Sem POC isolada obrigatória.** Dentro dos primeiros incrementos visuais do jogo, usar uma **cena de referência pequena**, com casa, comércio, trecho de via/esquina, calçada, vegetação e um agente/veículo para comparação. O objetivo não é refazer por ritual o teste externo; é verificar **compatibilidade com o IndexCities**.

**Observar:**
1. **Qualidade:** reconhecer por miniatura/câmera comum; proporções, composição, paleta, luz, densidade e oclusão; não depender de texto no Criativo.
2. **Regeneração:** modificar telhado, largura, cor ou posição de porta em uma família e atualizar as variantes sem trabalho manual em dezenas de arquivos.
3. **Integração:** visual não invade footprint inválido; porta corresponde a acesso; orientação, seleção e variantes preservam o tipo funcional e a entidade real.
4. **Repetição:** comparar várias dezenas e depois centenas de instâncias com alterações de câmera; medir draw calls, CPU/GPU/memória e decidir se batching/LOD realmente ajudam.
5. **Entrega e manutenção:** o recurso funciona no **jogo exportado**, preserva nós/componentes e referências, não exige geração de arquivos dentro de `res://` quando a aplicação já está empacotada e pode ser regenerado a partir do código/receita. Custo real por **asset aceito** = prompt/geração + revisão + correção + integração + nova geração após mudança. Se uma família ficar difícil de gerar bem, só então avaliar Blender ou serviço 3D externo para aquele caso.

**Liberar produção em lotes** quando a primeira família for visualmente coerente, editável, importável/usável, identificável e tiver comportamento de desempenho conhecido. A quantidade exata de famílias e variantes será derivada do catálogo físico aprovado e da diversidade visual necessária, **não de um número arbitrário de modelos**.

## Parecer ao responsável — ainda NÃO aprovado

**Priorizar Godot procedural assistida por IA como caminho inicial para assets estáticos low-poly**, com recursos gerados/salvos/reutilizados e parâmetros/versionamento enxutos. **Blender é uma alternativa pontual, não parte do caminho crítico.** Não criar editor modular no jogo nem geração procedural indiscriminada durante a partida. Não prometer desempenho, custo ou adequação a personagens/animações complexas antes de medir.

A evidência visual existente é forte o bastante para **avançar para validação de produção**, mas não para declarar a abordagem definitiva para 100% dos assets. Isso **não altera a SPEC nem a ARCHITECTURE**.
