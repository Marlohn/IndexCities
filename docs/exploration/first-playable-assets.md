# IndexCities — V1: briefing e entrega paralela de assets

> **EXPLORAÇÃO — não é fonte de verdade. Revisão humana: PARCIALMENTE REVISADO.** Briefing operacional preparado pela IA (2026-10-12) para uma frente independente de assets. O **recorte funcional da V1 foi aprovado pelo responsável em 2026-10-12 e está na [SPEC](../SPEC.md)**. As famílias visuais abaixo derivam desse recorte, mas **prioridades A/B/C, escolhas de ferramenta, dimensões, paleta, quantidade de variantes e soluções de pipeline continuam recomendações técnicas da IA pendentes de validação**, não decisões de produto automaticamente aprovadas. Fronteiras: [ARCHITECTURE](../ARCHITECTURE.md); pesquisa: [asset-production-pipeline.md](asset-production-pipeline.md).

## Objetivo e limite da equipe de arte

Preparar **assets originais, legíveis em câmera 3D isométrica e reutilizáveis** para a primeira cidade com dinheiro, construção e SIMs reais. A IA de arte trabalha em paralelo, mas **não altera comportamento de gameplay, footprints, capacidades, preços, emprego ou a SPEC** por conveniência visual. As cenas do Godot são apresentação; o Simulation Core é dono da lógica. Não usar nem portar assets de outros projetos antigos ou imagens de referência como fonte de malhas copiadas. Registre origem e licenças de dependências/recursos externos.

**Coordenação definida pelo responsável em 2026-10-12:** Codex administra a V1, decide os assets necessários ao incremento, envia pedidos e cuida de integração/validação. Claude Opus 5.5 com ultracode fica exclusivamente na frente de assets. A regra de trabalho está no [AGENTS](../../AGENTS.md#coordenação-da-v1-e-frente-de-assets--2026-10-12). **Este inventário é contexto, não uma ordem para produzir todo o catálogo.**

**Amostra inicial solicitada pelo coordenador (histórico):** Prefeitura, casa, mercearia, via 1+1 reta/em L com calçada/poste, árvore, pedestre e caminhão, com ícones e preview. A entrega fica em `src/IndexCities.Godot/Assets/V1/`; serve para verificar encaixes e linguagem visual, sem criar uma partida ou definir gameplay. Medidas, conectores e estética candidatos permanecem **PENDENTES de revisão humana** e de compatibilização. O coordenador solicitou posteriormente Pátio, fazenda, posto, captação, tratamento de água, tratamento de esgoto e eólica. O pacote foi entregue e integrado em 2026-10-12, com 1.044 verificações técnicas sem falhas no Godot do projeto. Essa validação técnica não aprova a estética nem transforma marcadores candidatos em regras de acesso.

## Referência visual enviada nesta rodada

> **Revisão humana desta seção: PENDENTE.** O responsável enviou uma imagem isométrica de referência durante a produção da amostra e perguntou sobre alcançar qualidade semelhante ou melhor. A análise abaixo é da IA; não é aprovação de estilo final, de modelos específicos ou de novas funcionalidades.

**Referência preservada no repositório:** [imagem enviada pelo responsável](../../references/visual/v1-neighborhood.png). **Cópia remota enviada por solicitação expressa:** [GitHub — referência visual V1](https://github.com/Marlohn/IndexCities/blob/references/v1-neighborhood/references/visual/v1-neighborhood.png), na branch dedicada `references/v1-neighborhood`; upload e SHA do blob foram conferidos. Uso como referência de autoria; a origem e os direitos de redistribuição da imagem não foram verificados, e ela não é um asset do jogo.

A imagem orienta candidatos de acabamento: proporções coerentes de rua/calçada/SIM, copas facetadas com volumes variados, fachadas e janelas legíveis, paleta coesa, detalhes de entorno e iluminação quente com sombras suaves. A comparação precisa ser feita em render da engine e no jogo, incluindo repetição e custo gráfico. A referência não é recurso para distribuir, malha a copiar nem autorização para adicionar café, prédio misto ou bicicleta à V1. Seu uso nesta frente é observação visual para autoria original.

## Inventário visual derivado do recorte aprovado da V1

| Prioridade proposta | Família / assets para cobrir a V1 | Como abordar e o que comunicar |
| --- | --- | --- |
| A | **Terreno plano + borda, rio/lago e transição com rodovia externa** | Recursos simples e repetíveis; mapa gerado pela seed, não cenário urbano pronto. Água e borda precisam ser reconhecíveis na câmera normal; não entregar mapa completo estático. |
| A | **Via urbana 1+1 com calçadas, postes e sinais básicos** | Kit visual compatível com trechos retos, cantos em L, T e cruzamentos; vias e conectividade são definidas pelo simulador. Luz de poste responde ao relógio/energia efetivos; não inventar infraestrutura gratuita. |
| A | **Prefeitura** | Silhueta singular reconhecível desde o catálogo; obra, construção concluída e seleção visual. Deve funcionar isolada num mapa vazio. |
| A | **Pátio Municipal de Obras** | Galpão/área operacional, pátio, entrada e zona de carga coerentes; caminhões e equipe não atravessam paredes. |
| A | **Casa simples de baixa densidade** | Família visual com base/variantes prontas (telhado/cor/fachada); ser selecionável uma a uma e ter ponto de entrada legível. Não criar editor modular para o jogador. |
| A | **Mercearia pequena** | Modelo-base de comércio com letreiro/identificação da empresa intercambiável e espaço de entrega; outro ramo no futuro não exige refazer estrutura nem entidade. |
| A | **SIM pedestre e veículos básicos** | Silhueta de cidadão diferenciável da escala urbana; animações mínimas de caminhar/parar, caminhão de materiais e caminhão de mercadorias. Toda pessoa/viagem exibida corresponde a agente real. |
| B | **Fazenda pequena** | Estrutura produtiva e área cultivável coerentes com porte fixo aprovado; área, acesso e produção vêm da simulação, não de vegetação decorativa. |
| B | **Posto de combustível** | Bomba(s), cobertura, tanque/aparência comercial, área de parada e entrada de carga/veículos; abastecimento visual só quando acontecer na simulação. |
| B | **Captação de água e tratamento de água potável** | Modelos candidatos separados até a arquitetura física de instalações e compatibilidade com a SPEC serem verificadas; cenário inclui rio/lago. Não fundir funções por decisão estética. |
| B | **Tratamento de esgoto** | Silhueta de instalação municipal distinta da estação de água; capacidade e emissão são dados de simulação. |
| B | **Fonte eólica de energia** | Turbina reconhecível, animação visual condicionada à operação real; não girar como indicador de geração garantida quando o sistema estiver parado. |
| B | **Veículos e sinais de operação** | Caminhão de carga local, possível veículo particular quando o SIM efetivamente adquirir/usar; prévias, estados de obra/sem acesso, estoque insuficiente, falta de dinheiro, energia/água. Reusar modelos/ícones quando não houver motivo funcional para duplicar. |
| C | **Vegetação e detalhes de ambiente** | Poucas famílias de árvores/arbustos/grama, decoração urbana mínima e superfícies; priorizar diversidade em zoom normal, sem exigir dezenas de meshes por árvore. |
| C | **Ícones e miniaturas do catálogo** | Uma miniatura legível por tipo funcional da entrega, ícones de materiais (Areia e brita, Concreto, Aço, Madeira, Asfalto), Alimentos, Combustível, dinheiro, trabalhadores, operações, status e controles básicos de câmera/tempo/save. Não exigir leitura para selecionar a ação básica. |

**Prioridades A/B/C são sugestão de sequência do trabalho visual, não desbloqueios do jogador.** Os tipos funcionais estão aprovados na SPEC; escala, medidas, técnicas e número de variantes precisam ser acordados com a implementação, sem introduzir novas categorias de produto.

### Estados e identidade — tão importantes quanto o modelo

Para cada tipo visual que se aplique: **prévia válida/inválida, obra em andamento, concluído, selecionado, ocioso/sem operador, sem acesso/sem insumo**. Estado deve ser obtido de **dados reais da simulação**; não criar população falsa ou animação de produção decorativa. A mesma construção pode ter diversos visuais, mas trocar sua apresentação não troca identidade, propriedade, capacidade, dinheiro nem histórico. Letreiro de comércio mostra o **operador real**, que pode mudar independentemente do dono do imóvel.

### O que NÃO produzir em lote para a V1

Polícia/bombeiros/ambulâncias, ônibus/vans/táxis, trens, escola/hospital, prédios altos/mistos, museu, mirante, aeroporto, refinaria/extrator de petróleo, fábrica de automóveis, dezenas de estabelecimentos diferentes, pontes/viadutos, rede de encanamento manual, fachadas de upgrades estruturais ou conteúdos do modo Criativo. Esses elementos podem estar na SPEC completa, mas **não foram incluídos no recorte aprovado da V1**. Não produzir conteúdo de outra origem apenas porque está disponível.

## Contrato de integração a fechar cedo com a IA do jogo

**Pedido atual do responsável — 2026-10-16:** produzir os caminhões por material, pavimentadora e rolo aprovados na [SPEC de logística](../spec/materiais-logistica.md#caminhões-reconhecíveis-pelo-material) e na [SPEC de construção](../spec/construcao.md#pavimentação-física-das-ruas). A modelagem desta entrega usa **GPT-6.1-sol, esforço xhigh**, por solicitação expressa, substituindo para esta frente a atribuição histórica ao Claude. A tarefa de arte deve preservar o padrão visual do conjunto atual, com fonte original editável, partes móveis separadas e sem autoplay; apresentar amostras cedo dentro do Godot, em escala de rua e com câmera do jogo. A aprovação do comportamento não equivale à validação humana da estética dos modelos produzidos.

O responsável pela implementação e a IA de assets precisam alinhar, **antes de congelar os modelos dimensionais**:

1. **Escala e orientação:** unidade de trabalho, eixo vertical da engine (Godot: Y), eixo de fachada e pivô no solo; a pesquisa técnica sugere **1 unidade 3D = 1 metro**, mas isso precisa ser testado no projeto e não fixa lotes arbitrários.
2. **Footprint por família:** dimensões, envelope visual, zona de exclusão física e áreas de interação necessárias. **O asset não determina o lote nem o preço.** Não repetir uma medida universal para casa, posto, fazenda e utilidades.
3. **Pontos funcionais explícitos:** portas de SIM, entrada de veículo, carga/descarga, parada/abastecimento e sentido das vias conforme aplicável. Um detalhe desenhado não equivale a acesso funcional.
4. **IDs e variantes estáveis:** integrar `BuildingTypeId` funcional separado de `VisualId`, usando o adaptador da ARCHITECTURE; preservar nomes, versões e relação fonte/gerado. Sugestões de ID são **exemplos técnicos**, não novas categorias.
5. **Níveis de visualização e desempenho:** câmera/isometria, zoom de construção, iluminação dia/noite, seleção/previews, sombra, material e instâncias múltiplas. Medir a amostra na engine em vez de aprovar por render isolado.
6. **Entrega/arquivos:** diretórios e convenções acordados com quem mantém o Godot; código-fonte/receitas originais editáveis quando gerados proceduralmente, recursos e cenas resultantes, referência de material/licença e miniatura. Testar carregamento também em build exportada para Windows.

**Regra para trabalho paralelo:** a IA responsável pela arte **não edita arquivos de gameplay nem altera SPEC/ARCHITECTURE** para adaptar a malha. Ela entrega recursos sob caminho próprio; a IA integradora valida e conecta os IDs, acessos e comportamento. Conflitos de dimensões/interface são discutidos cedo, sem rebase acidental em arquivos comuns.

## Opções de produção — duas ondas exploratórias

**Onda 1 — começar imediatamente, mesmo sem projeto jogável:** reunir referências de estilo **sem copiar**, definir uma paleta candidata, produzir um *style sheet* original com silhuetas de Prefeitura, casa, mercearia e estrada; esboçar ícones básicos e **um exemplar técnico editável** de casa, via e SIM/caminhão com escala relativa. Sinalizar `PENDENTE` para tamanho, porta, colisão e material ainda não validados. A visão low-poly é candidata forte no estudo atual, mas **não está aprovada como estilo final imutável**.

**Onda 2 — após validar a primeira amostra no Godot com a IA do jogo:** fechar escala/pivô/acessos e produzir variantes prontas por família do recorte aprovado; gerar miniaturas consistentes, estados visuais, recursos de GPU repetíveis e assets de instalações especializadas. Preferir receitas paramétricas na Godot para malhas estilizadas **como experimento inicial**, salvando/reutilizando cenas e meshes quando fizer sentido; Blender/GLB é **opcional** para tipos difíceis, personagens e animações. Não gerar mesh pesada do zero a cada edifício colocado; não prometer que recursos Godot gerados por código ganham LOD automaticamente.

**Intercâmbio:** Godot importa glTF 2.0/`.glb` para modelos externos; recursos e cenas nativos (`.tres`, `.res`, `.tscn`) são adequados quando a geração é dentro da Godot. Não obrigar GLB se ambos os lados usam recursos Godot nativos. Conferir licenças, materiais, animações e importação na versão estável efetivamente usada pelo jogo. Referência oficial: [Godot — formatos 3D](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).

## Definição de asset pronto para integração

- Tem origem autoral/licença conhecida, fonte editável e ID visual claro; funciona com materiais/iluminação e câmera da cidade, não apenas em um render frontal.
- Respeita escala/pivô/footprint e conectores **combinados**; não invade acesso, pista ou lote vizinho; selecionável independentemente.
- Permite variação visual sem alterar gameplay; estados essenciais são comandados por dados reais, não por animação autônoma independente da simulação.
- Foi carregado numa cena Godot, medido no conjunto mínimo (casa + via + mercearia + vegetação + SIM/caminhão), com nota de gargalos/reutilização; testar em build exportada antes de considerar a família finalizada.
- Os recursos estão disponíveis em pasta própria da frente de arte e as interfaces com a implementação estão documentadas no mesmo trabalho, sem mudanças surpreendentes nos arquivos de simulação.

## Exemplo de pedido — referência exploratória

**O texto abaixo é exemplo histórico da IA, PENDENTE de revisão humana; não substitui um pedido atual e delimitado do coordenador.** A ampliação para o inventário completo citada nele não está autorizada automaticamente.

> Trabalhe no IndexCities consultando `AGENTS.md`, `docs/SPEC.md`, `docs/ARCHITECTURE.md`, `docs/exploration/first-playable-scope.md`, este briefing e `docs/exploration/asset-production-pipeline.md`. Sua frente é visual, sem alterar gameplay. Inicie imediatamente uma amostra original de casa, via urbana e mercearia com linguagem 3D isométrica simples e legível, acompanhada de sugestão de paleta, silhuetas e contrato de escala/pivô/entradas a validar com a IA integradora. Não copie recursos de outros projetos nem trate hipóteses pendentes como decisões humanas. Depois de validar a amostra em Godot, amplie para o inventário V1 aprovado, fornecendo fontes editáveis, IDs visuais estáveis, cenas/meshes/ícones e instruções de integração, com critérios de desempenho, licenças e estados visuais comandados pela simulação.

