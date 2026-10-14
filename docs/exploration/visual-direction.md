# IndexCities — estudo independente de direção visual

> **EXPLORAÇÃO — revisão humana: PENDENTE.** Segunda auditoria crítica da IA concluída em **2026-10-14**. Comparação das imagens fornecidas pelo responsável com a [SPEC](../SPEC.md), o [AGENTS](../../AGENTS.md), a [ARCHITECTURE](../ARCHITECTURE.md), os assets, o código da `main` (commit `2a75a4ba`), capturas reais de prévias na Godot e documentação oficial da engine. **Pesquisa e recomendações ainda não foram aprovadas pelo responsável.**
>
> **Limites:** este documento **não cria requisitos, backlog obrigatório, aceitação formal da V1, ordem de produzir assets nem autorização para mudar código, gameplay ou arquitetura**. Nenhuma nova build, partida ou medição foi executada nesta auditoria. A frente Orca continua independente; o código pode avançar depois do commit examinado.

## 1. Objetivo e significado das referências

**Preferência expressa pelo responsável:** as imagens de bairros low-poly geradas por IA são o **piso de qualidade visual desejada**; o resultado almejado deve ser tão bom ou melhor, **não uma imitação literal**. Essa é uma ambição qualitativa para a apresentação do jogo, **não a declaração de que a V1 funcional já precisa reproduzir todos os elementos das imagens**. A [SPEC da V1](../SPEC.md#v1--primeira-versão-jogável-integrada-escopo-aprovado-em-2026-10-12) permite expressamente um mundo visualmente enxuto e não inclui pontes, parques especializados, prédios mistos ou edição de terreno.

O valor das referências está na **composição urbana**: proporções convincentes, variedade controlada, transição rua–lote, vegetação, luz, cores e espaços vazios intencionais. As imagens também têm problemas típicos da geração por IA (cruzamentos/encaixes incoerentes, pontes sem lógica física, detalhes impraticáveis). **Aprovar o visual requer capturas e partida reais do IndexCities**, não apenas imagens conceituais ou um screenshot de ângulo favorável.

A cena com café, pessoas, carros e mobiliário mostrada na conversa é um **experimento visual externo**, não implementação do IndexCities, benchmark do jogo ou fonte autorizada de malhas/código. Uma referência anterior está [preservada para consulta em branch própria](https://github.com/Marlohn/IndexCities/blob/references/v1-neighborhood/references/visual/v1-neighborhood.png). As demais imagens desta conversa não foram adicionadas ao repositório.

## 2. Estado verificado: o que já temos e o que falta demonstrar

Fontes: [assets V1](../../src/IndexCities.Godot/Assets/V1/README.md), [prancha V1](../../src/IndexCities.Godot/Assets/V1/Preview/Captures/v1_board_iso.png), [close da prancha](../../src/IndexCities.Godot/Assets/V1/Preview/Captures/v1_board_closeup.png), [prancha de instalações](../../src/IndexCities.Godot/Assets/V1/Preview/Captures/v1_wave2_board_iso.png), [antes/depois de grama e água](../../src/IndexCities.Godot/Assets/RecoveryEnvironment/Preview/Captures/env_compare_z110_h10.png), [`CityWorld.cs`](../../src/IndexCities.Godot/CityWorld.cs), [`Main.cs`](../../src/IndexCities.Godot/Main.cs), [primeiro playtest humano](v1-human-playtest-2026-10-13.md).

| Elemento | Evidência concreta na `main` | Lacuna ou ressalva |
| --- | --- | --- |
| Prédios e ícones | Os **10 tipos funcionais V1** têm cenas/miniaturas. Prefeitura, casa e mercearia apresentam volumes e detalhes legíveis nas prévias. | Prévia é vitrine, **não uma cidade jogada**. Não há prova atual de qualidade do conjunto num bairro. |
| Variedade residencial | `CityWorld.Visual` usa **uma** cena `residential.small_house.a` para a casa. Receita permite trocar materiais na autoria. | **Não há seleção/uso efetivo de variantes residenciais pelo host** no código inspecionado. Repetição de silhueta e telhado permanece. |
| Ocupação e escala | Casa visual: envelope aproximado **8,78 × 8,59 m** no chão; lote lógico da casa: **12 × 16 m**. Mercearia: **10,4 × 13,5 m** para lote **16 × 18 m**. Prefeitura: envelope **24,6 × 17,8 m**, lote lógico **20 × 18 m**. | O código **reduz uniformemente** o modelo que ultrapassa o lote. Na Prefeitura a redução calculável é ~0,81; na casa e mercearia sobra área considerável no lote. Isso pode afetar proporção, composição e distância visual à porta. **Não afirmar defeito reproduzido sem capturar no jogo.** |
| Entrada de SIM/carga | A simulação calcula `Building.Door` pelo centro da frente do **lote lógico**; assets guardam marcadores `Candidate_PedestrianEntry` / `Candidate_CargoPoint`. | O host não usa esses marcadores para redefinir a `Door` na integração examinada. **Verificar** se pedestres desaparecem antes de alcançar a porta desenhada e se a entrega aparenta ocorrer no lugar correto. Não mudar acesso funcional por estética. |
| Ruas e postes | `CityWorld.RoadSurface.cs` já desenha asfalto/calçadas contínuos; há cruzamentos, faixas, postes, prévias e ligações reais. Postes acendem conforme noite/energia; luzes locais existentes **não projetam sombra**. | Validar esquina, meio-fio, travessias e ligação com lote no jogo; **Orca já atua nessa área**. A referência não justifica alargar vias ou inventar estacionamento. |
| Árvores | Uma família de árvore; `CityWorld.Reset` gera **até 145 candidatas** por seed, varia escala/giro e oculta as que conflitam visualmente com vias e prédios. | Uma família e distribuição majoritariamente aleatória explicam parte da paisagem rarefeita. Não pressupor que árvores de cenário já sejam árvores econômicas/ambientais simuladas. |
| Água e terreno | Materiais `RecoveryEnvironment` já aplicados: grama com variação de cor; água com faixa rasa e ondulação no sombreamento. | O mapa continua plano; o lago é um `CylinderMesh` de contorno circular. **Shader melhora a superfície, mas não cria margem orgânica física.** |
| Iluminação e câmera | Câmera ortográfica com zoom e giro; sol direcional com sombra e intensidade ligada ao horário; iluminação ambiente. | A qualidade em manhã/noite, diferentes rotações, zoom de construção e GL Compatibility precisa ser **julgada na mesma renderização usada para jogar**. |

**Correção importante da primeira versão deste estudo:** não se deve partir da hipótese de que “faltam modelos bons”. O [playtest humano](v1-human-playtest-2026-10-13.md) elogia parte dos modelos e ícones; as capturas do kit mostram acabamento individual razoável. A diferença mais evidente para as referências está em **integração dos lotes, densidade/composição do ambiente e repetição**, seguida por compatibilidade de escala e iluminação. Isto é um **parecer visual**, não medição ou diagnóstico definitivo da build atual.

**Outra limitação de evidência:** as pranchas de assets foram renderizadas originalmente em **Forward+/Vulkan**; o `project.godot` principal usa **GL Compatibility/OpenGL**. O kit já teve checagens também em GL, que registraram cores mais claras/lavadas. Avaliar a paleta com a prancha Forward+ como se fosse o resultado em jogo seria incorreto.

## 3. Linguagem artística candidata — ainda não aprovada

**Urbanismo acolhedor estilizado:** geometria low-poly limpa e legível, paleta harmonizada, silhuetas variadas sem perder a família visual e composição de bairro que pareça habitado **quando a simulação realmente tiver moradores e operações**.

- **Primeiro encaixe e proporção; depois detalhe.** Porta desenhada, acesso lógico, calçada, degraus, recuos, caminhões e SIMs precisam parecer compatíveis. O modelo bonito isolado não compensa um morador que desaparece longe da fachada.
- **Variedade com curadoria, não ruído aleatório.** Fachadas, telhados e árvores podem mudar de forma e cor sem parecer objetos de estilos incompatíveis. A Prefeitura é arquitetonicamente mais elaborada que a casa; avaliar se isso cria hierarquia cívica desejada ou desunião estética.
- **Paisagem com composição.** Espaços verdes livres, concentração de árvores, bordas e lotes bem resolvidos importam mais que simplesmente elevar a contagem de props. Verificar a visibilidade de portas, acessos e veículos sob copas.
- **Paleta e luz condicionadas ao renderizador real.** Experimentar variações sutis dos materiais existentes antes de reescrever a biblioteca; evitar grama excessivamente uniforme, verdes fluorescentes, sombras pesadas e contraste que apague prévias/ícones. Nenhum código hexadecimal fica fixado aqui como estilo aprovado.
- **Detalhe por distância.** De longe: malha urbana, silhuetas, manchas verdes. Zoom normal: acesso, fachadas, lotes, calçadas. De perto: portas, acabamentos e objetos sem sobreposição grosseira. Não preencher a tela de rótulos ou efeitos para compensar falta de atividade.

Não é necessário adicionar **texturas fotográficas, iluminação global avançada, reflexos caros, complexidade arquitetônica extrema ou centenas de modelos exclusivos** para testar a direção. Também não existe evidência de que uma troca integral de pipeline produza automaticamente uma cidade melhor.

## 4. Prioridades recomendadas para estudar (não é backlog)

| Ordem de investigação | O que comparar primeiro | Motivo / limite |
| --- | --- | --- |
| **1. Escala e acessos visuais** | Mesmo prédio em lote real, posição da porta, trajetória dos SIMs e entrega; Prefeitura reduzida pelo host; recuo casa/mercearia. | **Pré-requisito de qualidade** identificado no código e no briefing. Corrigir representação sem alterar acesso, footprint ou gameplay sem decisão correspondente. |
| **2. Composição dos lotes existentes** | Casa + mercearia + calçada + área livre: variação visual de caminhos, jardins, pequenos detalhes e materiais **compatíveis com a área efetiva**. | Provável maior ganho por esforço no bairro. **Distinguir detalhe incorporado ao modelo (puramente visual)** de árvore/jardim/objeto físico comprável que tem custos e possíveis efeitos previstos na SPEC; não criar esse sistema na V1 por inferência. |
| **3. Vegetação e distribuição** | Uma única árvore repetida versus poucas formas coerentes e agrupamentos de cenário, com distância segura das ruas. | Alto potencial de ganho; distribuição e visibilidade podem importar mais que novas malhas. Não atribuir benefícios ambientais fictícios. |
| **4. Repetição de casas e linguagem comum** | Só então testar poucas variações compatíveis de telhado, volume e cor, na mesma rua e em vários zooms. | Evitar produzir dez modelos antes de descobrir se o problema principal era o entorno. Sem troca de propriedade, capacidade ou histórico por variante. |
| **5. Luz e materiais na cena integrada** | Sol/ambiente/postes em GL, manhã/tarde/noite, zoom e rotação equivalentes. | Ajuste potencialmente econômico, mas exige medição e cuidado com leitura dos estados reais. |
| **6. Acabamento viário, lago e futuro relevo** | Avaliar entrega Orca em ruas; testar marginalia do lago **sem alterar a geometria física**; estudar forma orgânica/relevo só separadamente. | Rua é fundamental, mas **não duplicar trabalho em andamento**. Contorno novo de água pode exigir decisões transversais de mapa, captação e construção; pontes/terraplanagem não pertencem à V1. |

A ordem é uma **hipótese de melhor custo-benefício**, sujeita ao resultado visual e ao desenvolvimento simultâneo. Não iniciar projeto de paisagismo amplo, sistema modular de edifícios ou ferramentas novas para validar essas ideias.

## 5. Uma comparação pequena, mas fiel ao jogo

No futuro, aproveitar um **save real pequeno** com Prefeitura colocada, via urbana conectada, algumas casas simples, mercearia e vegetação; Pátio/fazenda apenas se couberem naturalmente na partida. Nenhuma quantidade fixa de casas, arruamento ideal ou bairro pré-fabricado é requisito.

- **A = situação atual; B = proposta visual:** mesmo save, câmera, resolução, horário, seed e estado da simulação. Capturar **zoom distante, zoom normal de construir e zoom próximo**, de mais de uma direção. Repetir também com uma rua contendo várias casas iguais.
- Observar **entrada lógica versus entrada desenhada**, escadas e calçadas, recuos, sobreposição de copas, faixas, contraste da prévia e seleção de imóveis. Uma cena do editor só serve para triagem; **validar novamente na cidade jogável**.
- Comparar qualidade com as imagens do responsável, **sem exigir ponte, café, edifício misto, parque especializado ou lago ao lado do bairro**. A referência é padrão de acabamento, não catálogo V1.
- Verificar cidade efetivamente viva **somente quando há SIMs/empresas/viagens reais**. Casas vazias, obras paradas e falta de energia não devem ser mascaradas por figurantes ou animações inventadas.
- Se aprovado como marco visual, a aceitação humana é: “o bairro real do IndexCities transmite qualidade igual ou superior à referência, inclusive na câmera normal do jogador?”. **Não há percentual de semelhança, métrica estética automática nem aprovação antecipada.**

## 6. Performance: cuidado com inferências do protótipo

O [README dos assets V1](../../src/IndexCities.Godot/Assets/V1/README.md) registra **medição sintética** em Forward+/RTX 3050: uma grade com 400 casas e 400 árvores apresentou aproximadamente **36 draw calls e 3,2 ms de GPU**. Isso é evidência de que o **kit isolado** pode desenhar instâncias repetidas de modo eficiente **naquele renderizador** — **não** um benchmark de uma cidade simulada no **GL Compatibility**. A [documentação oficial da Godot sobre desempenho 3D](https://docs.godotengine.org/en/stable/tutorials/performance/optimizing_3d_performance.html) informa que o *automatic instancing* de `MeshInstance3D` idênticos é recurso do **Forward+**, não do Compatibility. Logo, não transportar os números para o jogo.

O `Main.QA.cs` já possui um **performance smoke** que registra FPS, percentil 95 de frame, tempo de simulação/apresentação e draw calls. Quando houver experimento autorizado, começar por essa medição existente num save representativo, sem instrumentação nova por ritual.

- Luzes `OmniLight3D` dos postes **não lançam sombras** no host atual; o sol lança. Não tratá-las como dezenas de shadow maps já ativos, mas observar quantidade de luzes visíveis e limites do Compatibility ([Godot: renderizadores](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html), [luzes](https://docs.godotengine.org/en/stable/tutorials/3d/lights_and_shadows.html)).
- Medir custo de cenas/instâncias, materiais, sombras, transparências, rótulos, regeneração viária, memória e CPU/GPU **com SIMs, UI e trânsito reais**, inclusive zoom longe e noite. Não existe meta de FPS ou hardware-alvo de aceite definida neste estudo.
- Reaproveitar cenas/meshes e otimizar por evidência. `MultiMesh` pode ajudar instâncias repetidas, mas **não faz culling individual**; se usado, testar por regiões ([Godot: MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html)). Não pressupor LOD automático em malhas nativas geradas por código.
- A pesquisa externa fundamenta **limitações técnicas**, não comprova custo ou qualidade estética do IndexCities. Trocar renderizador ou reescrever toda a arte é decisão de impacto maior, sem justificativa demonstrada aqui.

## 7. Orientação futura e fronteiras da SPEC

O [briefing de assets](first-playable-assets.md) e o [pipeline](asset-production-pipeline.md) já descrevem receitas editáveis, versões, escala, pivôs, materiais, IDs e marcadores. Não criar outro pipeline ou produzir um lote por causa deste estudo. Um futuro pedido delimitado do coordenador deve incluir **comparação renderizada na cena do jogo**, fontes originais, compatibilidade física, capturas em múltiplos zooms e custo mensurado.

A [SPEC — apresentação](../SPEC.md#conceito-e-apresentação) já aprova em sua **visão mais ampla** variantes prontas, vegetação, jardins e decoração com regras pertinentes; [terreno e espaços públicos](../SPEC.md#terreno-vegetação-e-espaços-públicos) inclui efeitos ambientais locais de objetos **reais**. **Essas decisões não inserem automaticamente novas ferramentas, despesas, árvores plantáveis ou benefícios na V1 reduzida.** Um detalhe pintado/incorporado à fachada não vira árvore funcional; uma árvore real plantada e cobrada não é mero shader.

Regras de fronteira para qualquer experimento visual: conservar identidade de prédios, SIMs e empresas; não deslocar pontos de acesso lógico sem autorização; respeitar colisão/footprints e saves; não falsificar veículos, clientes, produção, água, energia, iluminação ou ecologia; não copiar malhas de projetos anteriores ou imagens de IA. O Orca é responsável pelas melhorias integradas em andamento.

## 8. Decisões e evidências ainda necessárias

- **Humano:** aprovar/ajustar a linguagem artística candidata após comparar imagens **do próprio jogo**, incluindo Prefeitura, casas, mercearia e entorno; dizer se o “piso de qualidade” foi atingido **no marco visual futuro**. A preferência estética já expressa não aprova esta redação integral.
- **Teste técnico:** verificar em Godot os possíveis desencontros de escala/portas, quarteirão repetitivo, paleta sob GL, cenário com vegetação e custo visual real. O código sugere riscos; **não alegamos que os defeitos foram reproduzidos nesta sessão**.
- **Produto:** somente se uma solução exigir novos controles, variantes escolhidas pelo jogador, itens decorativos compráveis, custos, efeitos ambientais ou mudanças de regra, esclarecer e registrar na SPEC **antes** de implementação.
- **Futuro:** geometria natural nova para lagos/rios, edição de relevo, pontes e serviços extras precisam ser avaliados pelo seu próprio benefício/custo; **não são condições para uma boa cidade V1 de terreno plano**.

**Conclusão da rechecagem independente:** preservar o investimento existente em modelos e receitas; priorizar **consistência visual–física e composição do bairro** antes de multiplicar assets; comparar no **GL Compatibility do jogo** e em uma partida verdadeira. A direção é viável como hipótese artística, **sem promessa de qualidade ou desempenho antes dessa validação**.
