# IndexCities — estudo independente de direção visual

> **EXPLORAÇÃO — revisão humana: PENDENTE.** Estudo da IA em 2026-10-14, baseado nas referências fornecidas pelo responsável, nos documentos canônicos e no código da branch `main` consultado nessa data (HEAD `2a75a4ba`). **Não é direção artística aprovada, requisito adicional, backlog, pedido de assets ou autorização para implementar.** A [SPEC](../SPEC.md) define o produto e o recorte da V1; [AGENTS](../../AGENTS.md) define como trabalhar; [ARCHITECTURE](../ARCHITECTURE.md) governa decisões estruturais.
>
> **Escopo desta entrega:** somente estudo e documentação, sem modificar código, cenas, assets, shaders, SPEC, arquitetura ou coordenação do Orca. Nenhuma build, benchmark gráfico ou nova partida foi executada nesta análise; informações sobre a implementação vêm da inspeção do repositório e de relatos já registrados.

## Meta visual e limites da referência

**Preferência expressa pelo responsável nesta conversa:** as imagens enviadas, produzidas por IA, representam um **piso de qualidade estética desejada**: buscar resultado igual ou melhor, **não inferior**. Não são desenho técnico perfeito, ordem para copiar elementos, prova de implementação em Godot, métricas de desempenho, escala ou licença de redistribuição. O julgamento de “igual ou melhor” ainda é qualitativo e depende de comparação humana em capturas reais do jogo; não se considera satisfeito por semelhança de cor ou quantidade de polígonos.

Características a perseguir como direção candidata: bairros isométricos acolhedores, casas com silhuetas e telhados variados, ruas e calçadas contínuas, vegetação abundante mas organizada, entorno dos lotes detalhado, água/terreno integrados, materiais coerentes e luz agradável. **Não copiar** incoerências das imagens geradas: geometrias irreais, encaixes impossíveis, pontes sem rede funcional, cruzamentos inconsistentes e composição feita apenas para um ângulo de câmera.

Há duas classes de imagens no estudo: referências urbanas geradas por IA e capturas de um experimento visual externo com café, casas e props. **O experimento externo não é implementação do IndexCities e não concede permissão de reutilização de assets/código.** O briefing anterior preserva [uma referência em branch específica](https://github.com/Marlohn/IndexCities/blob/references/v1-neighborhood/references/visual/v1-neighborhood.png); outras imagens anexadas à conversa não foram incorporadas a este commit.

## Onde o projeto está — evidências e lacunas

Evidência conferida no repositório; **estado atual da branch de desenvolvimento pode avançar em paralelo**:

| Área | Base encontrada | Lacuna visual ou incerteza |
| --- | --- | --- |
| Edifícios | Dez tipos funcionais da V1 com cenas, meshes, materiais e miniaturas. Alguns modelos e ícones agradaram no [playtest humano](v1-human-playtest-2026-10-13.md). | Ainda não há evidência de que o conjunto alcance a referência na composição urbana e em zooms variados. |
| Casas | `CityWorld.Visual` mapeia a casa para `residential.small_house.a`; a receita oferece telhado, esquadrias e acabamento. | Uma silhueta principal tende a repetir-se; a receita não inclui jardim/cerca/garagem. Apenas variar cor não garante diversidade convincente. |
| Comércio | Mercearia com toldo, fachada e letreiro intercambiável, preenchido com a empresa operadora real. | O entorno, a relação com a calçada e o reconhecimento da operação precisam ser avaliados no bairro completo. |
| Vegetação | Uma família principal `vegetation.tree_broadleaf.a`; `CityWorld.Reset` tenta distribuir até 145 árvores por seed, mudando escala e giro, e oculta conflitos de ocupação. | Distribuição majoritariamente aleatória e silhuetas parecidas não formam, por si, paisagismo natural. |
| Ruas | `CityWorld.RoadSurface.cs` compõe superfícies contínuas de asfalto e calçada; existem marcações, travessias e postes. | Cruzamentos, limites entre lote/calçada, meio-fio e faixas precisam de validação visual integrada; parte já está sob revisão do Orca. |
| Luz | Sol direcional com sombras, câmera ortográfica e ambiente definidos em `Main.cs`; postes respondem à noite e ao fornecimento elétrico real. | Qualidade da luz e contraste nas diferentes horas/zooms não foram comparados sistematicamente às referências. |
| Terreno e água | Shaders recentes `RecoveryEnvironment` variam a grama e a superfície d'água, integrados sob decisão provisória IA-020 na SPEC. | O terreno permanece plano; o lago usa um cilindro de contorno circular. Shader melhora aparência, **não** sua silhueta geométrica. |

Arquivos-chave: [`CityWorld.cs`](../../src/IndexCities.Godot/CityWorld.cs), [`CityWorld.RoadSurface.cs`](../../src/IndexCities.Godot/CityWorld.RoadSurface.cs), [`Main.cs`](../../src/IndexCities.Godot/Main.cs), [receitas e cenas V1](../../src/IndexCities.Godot/Assets/V1/README.md) e [`RecoveryEnvironment`](../../src/IndexCities.Godot/Assets/RecoveryEnvironment/README.md). O [playtest de 2026-10-13](v1-human-playtest-2026-10-13.md) registra problemas observados no mundo/água/ruas, mas não substitui uma avaliação do estado mais recente.

**Distância atual da referência:** significativa no **acabamento do conjunto e da paisagem**, mesmo com bons recursos individuais. Esta é uma avaliação qualitativa baseada no código e no playtest, **não** um percentual medido ou certificação visual da build atual.

## Direção artística candidata — ainda não aprovada

**“Urbanismo acolhedor estilizado”**: 3D low-poly de geometria clara, mas com composição, proporção e acabamento deliberados. Evitar tanto o realismo pesado quanto a aparência de peças soltas sobre um plano uniforme.

- **Formas e escala:** silhuetas diferenciáveis a distância; portas compatíveis com SIMs e acessos reais; altura, telhados e volumes coordenados entre vizinhos. A variação deve ser controlada por famílias, não sorteio irrestrito.
- **Paleta:** verdes moderados com contraste entre grama, copas e arbustos; fachadas quentes e claras; telhados em terracota, tons neutros e alguns frios; asfalto e calçada contrastantes sem dominar o bairro; água azul-esverdeada menos saturada. **Cores exatas não estão aprovadas**; preservar e refinar materiais que já funcionam antes de substituí-los.
- **Iluminação:** sombras que ajudem a ler volume, luz natural agradável e visibilidade em diferentes horários. Efeito emissivo, postes e sinais operacionais devem obedecer às condições físicas aprovadas.
- **Densidade visual em camadas:** longe, ler bairro/rua/vegetação; no zoom comum, perceber fachadas, acesso e jardins; perto, ver detalhes limpos sem interseções grosseiras. Decoração não pode prejudicar prévias nem inspeção.
- **Natureza:** combinar árvores de famílias/tamanhos e arbustos em grupos plausíveis, bordas, espaços abertos e vizinhanças; não confundir densidade de vegetação com distribuir muitos objetos ao acaso.
- **Água e relevo:** no escopo V1, buscar integração visual compatível com lago e terreno **planos** existentes; geometria orgânica nova deve ser investigada separadamente, pois pode alterar construção, captação, limites e geração do mapa.

## Onde o ganho tende a ser maior

Estimativas **relativas, para avaliar**, não fila de trabalho autorizada:

| Experimento candidato | Benefício visual esperado | Esforço/risco estimado | Cuidado |
| --- | --- | --- | --- |
| Jardins, caminhos e pequenos elementos de entorno em lotes existentes | Muito alto | Baixo–médio | Respeitar footprint, acesso, custo e natureza funcional/decorativa; não autoriza nova ferramenta de decoração V1. |
| Duas ou mais silhuetas residenciais *candidatas* compatíveis | Muito alto | Médio | Reusar famílias e materiais; variante visual não altera família, propriedade, capacidade ou construção. Quantidade a testar, não aprovada. |
| Vegetação mais variada e distribuição com intenção | Muito alto | Médio | Não obstruir rua/obra; medir desenho e custo de muitas instâncias. |
| Calibração de luz, sombras e contraste do conjunto | Alto | Baixo–médio | Conferir manhã, tarde/noite, sombras, legibilidade e renderizador real. |
| Integração lote–calçada e acabamento de cruzamentos | Muito alto | Médio–alto | Área já trabalhada no Orca; não criar mudança concorrente nem alterar conectividade por estética. |
| Margens e ambientação do lago atual sem alterar geometria funcional | Alto | Médio | Evitar margem que indique terra física onde ainda é água/área não posicionável. |
| Forma orgânica de rio/lago e eventual relevo | Muito alto potencial | Alto, transversal | **Pesquisa futura separada**; não é melhoria gratuita de asset nem requisito V1. |
| Mobiliário e animações/indicadores urbanos | Médio–alto | Variável | Atividades humanas/veiculares devem vir de entidades/eventos reais; não simular figurantes. |

**Ordem sugerida para um teste visual futuro:** trabalhar primeiro composição de casas/lotes/vegetação e contraste em um bairro pequeno, enquanto a equipe atual resolve vias/água. A priorização muda conforme o estado real do jogo e resultados medidos.

## Bairro exemplar para avaliação — sem mecânica nova

Um **cenário de comparação**, não um mapa predefinido ou geração automática obrigatória:

- Rua 1+1 conectada fisicamente à rede externa, com trechos retos/esquina em L e calçadas/cruzamentos que correspondam à topologia real.
- Aproximadamente **6–10 casas simples** colocadas individualmente, com propostas de acabamentos visuais variados e escala consistente; quantidade meramente ilustrativa.
- **Uma mercearia pequena** com acesso de pessoas e entregas coerente e nome da empresa operadora real; **não adicionar café/restaurante** por aparecer na referência.
- **Prefeitura** como marco visual e **Pátio de Obras** em área operacional compatível, quando forem parte da cidade efetivamente construída; fazenda pode aparecer na borda sem virar paisagismo falso.
- Árvores em grupos, áreas livres e elementos de jardim que deem leitura de bairro **sem depender de lago, ponte ou terreno editável**.
- Pedestres, carros e entregas somente quando derivados da simulação. Casas vazias e serviços parados continuam possíveis e precisam ser visualmente honestos.

Julgar em **três escalas** (distante, zoom normal de construção, próximo) e mais de um horário; comparar o mesmo save, ângulo, iluminação e resolução antes/depois. Uma prancha estática bonita pode orientar o estilo, mas não comprova legibilidade, construção, jogabilidade nem escala real.

## Critérios propostos para futura validação

São **critérios candidatos**, sem constituir aceite formal ou meta de desempenho já acordada:

1. O responsável considera o bairro real renderizado na Godot **não inferior em qualidade percebida** às referências de IA, levando em conta suas imperfeições; não basta reproduzir uma imagem em ângulo único.
2. Casas e árvores repetidas não parecem clones; os lotes e acessos formam ruas coerentes.
3. Não existem defeitos graves de encaixe, interseção, sombras ou escala em zoom normal e próximo.
4. Interface de seleção, construção e diagnóstico continua clara; efeitos decorativos não encobrem informações críticas.
5. Não há novos SIMs, tráfego, estoques, iluminação funcional nem eventos fictícios introduzidos para dar “vida” à cena.
6. Medir FPS, tempo de atualização, draw calls, uso de memória e variação entre zooms no renderizador/hardware realmente usado. **Nenhuma meta numérica foi definida ou comprovada**; cidades maiores exigem amostra com muitos edifícios/agentes reais.
7. Observar custo de malhas/material/sombras/luzes/objetos, e testar reutilização, MultiMesh por região e níveis de detalhe **somente onde houver gargalo medido**. Recursos Godot procedurais não recebem LOD automático por suposição.

## Orientação para pedido futuro à IA de assets

Usar [briefing V1](first-playable-assets.md) e [pesquisa do pipeline](asset-production-pipeline.md) como base; **este estudo não é um pedido de produção**. Quando o coordenador solicitar um experimento delimitado:

- Partir das receitas, cenas, IDs e materiais originais já existentes; variantes compatíveis podem compartilhar malhas/materiais. Blender/GLB só quando resolver necessidade comprovada.
- Propor poucas alternativas originais de silhueta e paisagismo, com paleta coerente, em vez de produção indiscriminada de todo o catálogo.
- Entregar fontes editáveis, `VisualId` estável, pivôs, dimensões candidatas, marcadores de acesso, licenças/origem e capturas lado a lado na câmera do jogo.
- Testar repetição de modelos, oclusão, sombra e custo de renderização no conjunto; visual não decide footprint, lógica, custos, empresa, moradores nem acesso.
- Não copiar meshes, códigos, arquivos ou interfaces de outro projeto nem usar imagens geradas como assets finais por inferência.
- Integração e qualquer mudança de comportamento/escopo continuam sob a SPEC e a coordenação vigente.

## Dependências e decisões ainda abertas

- **Aprovação humana pendente:** linguagem visual proposta, paleta, diversidade desejada, custo gráfico aceitável e critérios concretos de comparação.
- **Experimento, não decisão:** qual combinação mínima de variantes, lotes e vegetação oferece o melhor ganho na Godot real.
- **Trabalho externo em andamento:** vias, UI, câmera, SIMs, circulação e água são responsabilidade do Orca neste momento. Não interferir, alterar esses recursos em paralelo ou declarar sua conclusão.
- **Fora da V1 aprovada:** pontes/viadutos, parques especializados, prédios mistos, edição de terreno, novos serviços e cenas com atividades não modeladas. Futuras propostas precisam de recorte e decisão na SPEC antes de entrar no produto.
- **Fonte de verdade:** uma escolha estética aceita pelo responsável pode orientar produção; se criar comportamento, ferramenta, custo, interação ou escopo novo, registrar primeiro a regra na SPEC. Este documento permanece exploração até revisão explícita.

Este estudo é deliberadamente independente e limitado a **direção visual passível de comparação futura**, não plano obrigatório de implementação.
