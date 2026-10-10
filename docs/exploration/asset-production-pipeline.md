# IndexCities — Estudo de produção de assets 3D

> **Exploração temática, NÃO é fonte de verdade nem requisito aprovado.** Produto: [SPEC](../SPEC.md); arquitetura: [ARCHITECTURE](../ARCHITECTURE.md).
>
> **Revisão humana: PENDENTE.** Pesquisa e recomendações da IA em 2026-10-10, ainda não revisadas ou aprovadas pelo responsável. Os fatos já confirmados são identificados pela fonte canônica; os números e o pipeline abaixo são hipóteses de produção a validar.

## Pergunta e resposta

**Podemos produzir assets em paralelo ao jogo?** Sim: a SPEC e a arquitetura já permitem e favorecem que a apresentação visual evolua independentemente do funcionamento da cidade. A intenção de usar ferramenta/IA externa já estava registrada em [mundo e ambiente](world-map-environment.md). **Mas não é prudente gerar o catálogo definitivo antes de validar escala, perfis de rua, ocupação, acessos, legibilidade isométrica e importação real no Godot.** Esses riscos são mais concretos do que a ausência de uma contagem exata de modelos.

**Podemos começar agora?** Sim, com referências visuais, componentes modulares e uma pequena amostra importável. Não é preciso encerrar toda a gameplay nem fixar o número final de edificações para preparar isso. O que deve ser evitado é produzir em massa numa escala ainda não validada.

## Fatos decididos x decisões abertas

| Assunto | Já decidido na SPEC/ARCHITECTURE | Ainda aberto / a validar |
| --- | --- | --- |
| Apresentação | 3D com câmera isométrica; casas e edifícios como unidades de construção | Ângulo/alcance do zoom, paleta, tratamento de materiais e densidade visual definitiva |
| Espaço e vias | Mapa plano inicial; grade lógica com posicionamento assistido e alguma liberdade; vias ortogonais em L, pontes/viadutos | Escala concreta, perfil da rua/calçada, regras geométricas, footprints, pivôs e encaixe funcional |
| Moradia | Casas individuais e edifícios de apartamentos com múltiplas unidades | Famílias visuais e silhuetas, pavimentos por variante, dimensões e linguagem arquitetônica |
| Negócios | Dez **atividades privadas** iniciais; famílias de lojas pequenas/comércios médios; exceções físicas como posto e hotel; reuso de modelo-base aprovado | Quantidade de modelos e variações, proporções e diversidade visual em escala |
| Outros edifícios | Sistemas públicos, agro, indústria, logística e serviços/veículos físicos constam na SPEC | Inventário gráfico fechado e quais exigem geometria única |
| Evolução | Não há upgrade/ampliação estrutural de prédio pronto; construir outra unidade amplia capacidade | Variações puramente estéticas e estados visuais de obra/dano com o mínimo necessário |
| Arquitetura | Simulação separada de assets/meshes; identidade econômica não depende do VisualId | Convenções de autoria, entrega, QA, compressão, LOD e organização do catálogo |

O visual 3D estilizado e a referência de legibilidade descritos em [mundo e ambiente](world-map-environment.md) são **direção exploratória**, não ficha técnica dimensional já aprovada.

## Não confundir quatro quantidades

1. **Tipos de comportamento/edifício:** casa, apartamento, estabelecimento privado, refinaria, hospital etc. Derivados da SPEC e das suas regras de capacidade.
2. **Famílias geométricas de modelos-base:** silhuetas de casa, loja pequena, comércio médio, edifício residencial, unidade especializada etc.
3. **Variantes visuais:** letreiro, cor, acabamento, telhado, ornamentação, módulo de fachada; não criam empresas nem novas capacidades.
4. **Instâncias na cidade:** centenas ou milhares de repetições dos mesmos modelos, distintas em ocupação, propriedade, moradores, operações e estado.

Portanto, **dez atividades comerciais NÃO significam dez meshes exclusivos**. Tampouco um único modelo comercial pode servir a qualquer porte e atividade especializada: as capacidades e acessos físicos precisam fazer sentido. Não existe contagem final aprovada e não há base para uma estimativa confiável de horas/custos de produção antes de inventariar famílias e validar o kit de referência.

### Famílias de assets a mapear (inventário de áreas, não catálogo aprovado)

| Família | Exemplos de necessidades existentes | Estratégia candidata |
| --- | --- | --- |
| Habitação | Casa, edifício com múltiplos apartamentos | 2 ou mais silhuetas-base por diferença física; variação de telhado/fachada depois de testar leitura |
| Comércio/serviços | Mercado, mercearia, café, lanchonete, restaurante, barbearia, salão, academia | Loja pequena e comércio médio com módulos/placas/cores compartilhados; mesma fachada não implica mesma empresa |
| Especializados privados | Posto, hotel | Estruturas próprias quando abastecimento/hospedagem, acessos e capacidade justificarem |
| Produção e logística | Fazenda, fábrica, extração de areia/brita/madeira/petróleo, refinaria, fábrica de veículos, Centro de Materiais/Pátio de Obras | Separar edifício, máquinas e área operacional; validar footprints e pontos de carga reais |
| Serviços municipais | Prefeitura, escolas, saúde, bombeiros, polícia, acolhimento infantil, água/energia, esgoto, resíduos, transporte/garagens | Compartilhar materiais e detalhes, mas preservar escala, identidade reconhecível, entrada e área de operação |
| Vias e espaço público | Segmentos retos, L, cruzamentos, calçadas, faixas, pontes/viadutos, pontos de ônibus, parques, praças | Peças encaixáveis ou geração geométrica; não presumir um tile rígido como regra de posicionamento |
| Agentes e veículos | SIMs, automóveis, ônibus, táxis, caminhões de carga/coleta, frotas de emergência, bicicletas | Dimensões coerentes, orientações e pontos de acesso; compartilhar estruturas/materiais quando possível |
| Natureza e detalhe urbano | Árvores, gramado, vegetação, postes, bancos, cercas, sinalização | Poucas famílias com variantes; alta capacidade de repetição e controle de distância |

A tabela é **levantamento de categorias**, não autorização para adicionar objetos ou serviços ausentes da SPEC. Vários elementos exigirão modelos diferentes mesmo dentro da mesma família; outros poderão ser representados de modo procedural/modular sem arquivo 3D exclusivo.

## Escala: hipótese de referência, não aprovação dimensional

A [documentação oficial da Godot](https://docs.godotengine.org/en/stable/tutorials/3d/introduction_to_3d.html) adota **1 unidade 3D = 1 metro**; vale usar metros reais na autoria e no importador para reduzir ajustes posteriores. Referências físicas para um teste integrado:

| Elemento | Hipótese inicial para checar visual e movimento | Motivo / observação |
| --- | --- | --- |
| SIM adulto | Cerca de **1,7–1,8 m** de altura | Referência visual simplificada, não requisito de estatura individual |
| Automóvel | Aproximadamente **4–5 m** de comprimento e **1,8–2,0 m** de largura | Envelope de teste, não define frota nem capacidade |
| Ônibus | Aproximadamente **10–13 m** de comprimento | Testar conversão, embarque, curvas e garagem |
| Faixa urbana | Aproximadamente **3,0–3,3 m** de largura | Comparável à orientação NACTO de 10–11 pés em vias urbanas; não importa regras de trânsito estrangeiras como requisito |
| Faixa livre de calçada | Aproximadamente **1,8–2,4 m** | Comparável à orientação GDCI; mobiliário/árvores não devem bloquear passagem real |

Para uma rua simples de duas faixas de 3 m com calçadas livres de 2 m por lado, a seção de teste teria **~10 m**, **sem** incluir estacionamento, árvores/faixa de serviço, acostamento, ônibus em baia etc. Esse exemplo permite montar e visualizar o primeiro quarteirão, **não fixa a largura final**. Ver [NACTO — Lane Width](https://nacto.org/publication/urban-street-design-guide/street-design-elements/lane-width/) e [Global Designing Cities — Sidewalk Geometry](https://globaldesigningcities.org/publication/global-street-design-guide/designing-streets-people/designing-for-pedestrians/sidewalks/geometry/).

**Não fixar tamanho universal de lote.** Casa, residência multifamiliar, comércio de pequena/média escala, depósito, refinaria e fazenda têm footprint, acesso, recuos funcionais e altura distintos. Definir um conjunto pequeno de **envelopes de teste** com dimensões medidas após combinar rua + calçada + carro + SIM + edifício. Separar:
- **malha visual** (silhueta e detalhes) de **footprint lógico** e ocupação/colisões;
- **pontos de acesso físicos** (pedestres, carga, veículos, atendimento) da orientação do modelo;
- **ponto de origem** padronizado (candidato: centro horizontal do footprint ao nível do solo) de entradas e áreas de manobra representadas por marcadores separados;
- **área funcional** (lotação, estacionamento, garagem, pátio e estoques) do simples volume decorativo.

A grade aprovada é **lógica e híbrida**. A padronização artística facilita snapping, mas **não pode converter silenciosamente o jogo num tabuleiro de tiles rígidos**. Viadutos/níveis também devem ser possíveis sem exigir peças desenhadas apenas para superfície plana.

## Fluxo externo → Godot: recomendação experimental

1. **Contrato mínimo de referência:** exemplos de estilo e escala, dimensões de envelope de teste, convenção de eixo/pivô, pontos de acesso, paleta/materiais, limites observáveis de complexidade e como identificar autoria/licença.
2. **Blockout de volumes simples:** antes do modelo bonito, testar casa, comércio, prédio, veículos, vias e acesso numa cena real do jogo; medir visual com o zoom previsto.
3. **Autoria 3D externa:** ferramenta de modelagem ou gerador assistido por IA pode produzir base; revisar escala, malha, topologia, UV/material, direitos de uso e preservação do arquivo editável. IA gerando uma imagem **não substitui** um modelo 3D importável.
4. **Troca de arquivos:** preferir **glTF 2.0 binário `.glb`** como entrega para Godot e guardar o fonte editável (ex.: `.blend`) separadamente quando houver. `.gltf` + recursos separados serve quando facilitar revisão, composição ou tratamento de texturas. Não impor exportações duplicadas só por ritual.
5. **Orientação e importação:** Godot usa **Y para cima**; glTF/Godot têm convenção própria para a frente do modelo orientado (**+Z**) distinta do vetor de visão da câmera (**-Z**). Testar na primeira importação, não supor que o forward padrão do Blender e do jogo coincidam. Aplicar transforms e conferir triangulação/face culling conforme orientação oficial.
6. **Montagem da apresentação:** resolver `VisualId` em cenas/meshes/materiais, adicionando acessos, seleção, sombras, colisões e estados visuais como responsabilidade de apresentação/adaptador, **não** convertendo `.tscn` em regra da simulação. Acabamento, letreiro e luz da cena podem ser ajustados na Godot.
7. **Reutilização e substituição:** modelo-base + poucos componentes de fachada, cor e letreiro quando compatíveis; asset visual novo não altera `BuildingTypeId`, capacidade, proprietário ou operação econômica.
8. **Aceite por amostra importada:** inspecionar artefatos com escala/câmera real, medir uso de GPU, instâncias, materiais e tempo de carregamento; só então encomendar/gerar lotes de variantes.

Fontes técnicas: [Godot — formatos 3D](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html); [Godot — convenções e exportação](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html); [Blender — glTF](https://docs.blender.org/manual/en/latest/addons/scene_gltf2.html). A documentação da Godot também desaconselha confiar na equivalência das luzes importadas entre motores.

### Guia mínimo por arquivo (proposta, sem criar processo pesado)

- Nome/ID visual, família geométrica e referência ao tipo semântico compatível, **sem atrelar aparência à entidade econômica**.
- Dimensões em metros, altura, pivô no solo, direção da frente, eixos de exportação e dimensões do footprint testado.
- Entradas compatíveis (SIM, veículo, carga/descarga), sem acessos falsos; não presumir interiores totalmente modelados se a câmera não os mostrar.
- Materiais simples, número de superfícies reduzido, evitar transparência e materiais únicos excessivos, letras/logos substituíveis.
- Ficheiro editável e `.glb` importável; texturas dependentes identificadas; origem, licença e restrições de uso verificadas quando houver material de terceiros ou gerado com serviço externo.
- Estados de obra/dano/atividade: **não multiplicar meshes por antecipação**; usar sobreposições/efeitos/componentes compartilhados quando der leitura correta. O estado real de obra e danos continua existindo na simulação.
- Regras de desempenho por medição, **não** um limite rígido universal de triângulos/texturas inventado.

### Desempenho e risco de produção massiva

Num city builder a **repetição** pesa mais do que o número de arquivos diferentes. Para árvores, fachadas repetidas e pequenos detalhes, avaliar instancing/`MultiMesh` por região; a própria Godot avisa que instâncias no mesmo MultiMesh são ocultadas/visíveis em conjunto, por isso um MultiMesh para a cidade inteira pode piorar o trabalho da GPU. Para distância, testar **LOD automático** e **faixas de visibilidade/HLOD**: não pedir LOD manual para cada detalhe até existir motivo medido. Fontes: [Godot — MultiMeshes](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html), [Mesh LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html), [Visibility ranges](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html).

O custo da simulação de SIMs e economias **não deve ser mascarado pela aparência**: se a simulação tiver centenas de milhares de habitantes, seus registros não precisam corresponder a `Node3D` de todos em todas as cenas. Isso já segue a fronteira simulação/apresentação da ARCHITECTURE e **não reduz** a obrigação de simular acontecimentos reais da SPEC.

## Caminho de menor retrabalho

**Etapa A — Amostra de escala/linguagem (não catálogo):** prototipar *aproximadamente 10–15 representações simples*, incluindo SIM, carro, ônibus, via reta/L/cruzamento com calçada, casa, edifício residencial, loja pequena, comércio médio, um prédio funcional especializado e árvore. O número é um **orçamento exploratório de amostras**, não quantidade oficial de assets, nem exigência de POC isolada. Pode ser montado em incrementos do jogo integrado.

**Etapa B — Validar três imagens/cenas equivalentes:** quarteirão residencial, comércio com circulação e rua com ônibus/carga. Conferir simultaneamente identificação de porta, visibilidade de faixas/calçadas, encaixe sem colisões, relação entre pessoas/veículos e edifícios, zoom distante e variações de fachada. Avaliar quadros com **algumas dezenas e centenas de instâncias** sem estabelecer antes uma meta irreal de FPS por asset.

**Etapa C — Inventário de produção:** ao aprovar escala e estilo, converter os tipos físicos existentes na SPEC em uma matriz compacta: `BuildingTypeId`, categoria funcional, envelopes/entradas, família visual compartilhável, peças específicas, estados indispensáveis e quantidades de variantes desejáveis. Esse inventário deve nascer do produto **como definido naquele momento**, não de uma lista inventada de todo edifício possível numa cidade.

**Etapa D — Produção progressiva:** gerar modelos externos por famílias compatíveis, importar e testar de forma contínua. Favorecer componentes e skins reutilizáveis sem reduzir a silhueta urbana a uma única caixa. Adicionar variedade onde repetição estiver feia/ilegível no jogo, em vez de prometer dezenas de fachadas arbitrárias.

### O que deve ser decidido pelo responsável antes da produção em escala?

1. **Direção visual definitiva** — confirmar/ajustar a referência 3D estilizada em três cenas, incluindo câmera/zoom e limites de detalhes perceptíveis.
2. **Envelope espacial testado** — aprovar a relação rua/calçada/veículos/SIM/edifícios, não necessariamente uma tabela rígida de lotes para todos os prédios.
3. **Escopo de produção por família** — depois do teste, o inventário com número de modelos-base e variantes; antes dele, qualquer número é uma aposta.
4. **Entrega externa e propriedade dos fontes** — ferramenta/fornecedor, disponibilidade de projeto editável, licença para distribuição comercial, formatos e critérios objetivos de aceite.

**Recomendação:** não bloquear preparação, referências, blockouts e amostra por falta do catálogo exato; **bloquear somente produção em massa** até haver uma cena importada convincente e escala testada. É a menor decisão com maior redução de retrabalho.

## Referências principais

- [IndexCities — SPEC](../SPEC.md) — regras oficiais de posicionamento, negócios, construção e serviços.
- [IndexCities — ARCHITECTURE](../ARCHITECTURE.md) — separação da simulação, `BuildingTypeId` e `VisualId`.
- [IndexCities — mundo, mapa e ambiente](world-map-environment.md) — referências visuais e pipeline externo originalmente cogitado.
- [Godot — Introduction to 3D](https://docs.godotengine.org/en/stable/tutorials/3d/introduction_to_3d.html) — sistema métrico.
- [Godot — Available 3D formats](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html) — glTF, GLB, arquivos Blender.
- [Godot — Model export considerations](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html) — convenções de eixo, materiais, transformações e luz.
- [Godot — MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html), [LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html) e [HLOD](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html) — desempenho.
- [NACTO — Lane width](https://nacto.org/publication/urban-street-design-guide/street-design-elements/lane-width/) e [Global Designing Cities — Sidewalks](https://globaldesigningcities.org/publication/global-street-design-guide/designing-streets-people/designing-for-pedestrians/sidewalks/geometry/) — referências físicas externas, não requisitos automáticos do jogo.
