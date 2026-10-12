# Qualidade ambiental local — gameplay, cálculo e performance

> **EXPLORAÇÃO. Revisão humana: PARCIALMENTE REVISADO (direção de produto aprovada; pesquisa, fórmula e recomendações técnicas PENDENTES).** Estudo de 2026-10-11. A regra oficial está na [SPEC](../SPEC.md#terreno-vegetação-e-espaços-públicos), escolha **5C**; este documento **não substitui a SPEC nem aprova uma arquitetura específica**.

## Resumo: direção aprovada versus implementação ainda hipotética

**Vale manter, de maneira enxuta.** Árvores, jardins e áreas verdes reais podem melhorar moderadamente o ambiente local onde SIMs moram, contribuindo para bem-estar e, quando coerente, atratividade/valor da região. **Não** transformar cada objeto decorativo em um bônus individual de felicidade.

**Direção simplificada aprovada pelo responsável (2026-10-11):**
- Começar pela **qualidade do entorno residencial** no modo Convencional; presença de vegetação distribuída é o exemplo principal. O Criativo continua livre para decorar, **sem inventar um sistema equivalente de satisfação/economia**.
- A influência é **espacial e limitada**, com retorno decrescente: cinquenta vasos ou árvores no mesmo canto não produzem felicidade infinita.
- Usar **resultado local compartilhável**, consultado na atualização normal de satisfação dos moradores; **nunca** examinar cada decoração para cada SIM a cada frame.
- Atualizar os resultados afetados quando o mundo realmente muda, reaproveitando índices regionais e eventos já previstos para poluição/parques/valor imobiliário **onde for mais simples**.
- Não começar com análise do trajeto de cada SIM, simulação de percepção visual, manutenção por vaso, efeitos próprios de cada detalhe ou nova cadeia de produção. Efeitos de decoração não substituem refeições, emprego, saneamento, lazer real ou outros fatores de qualidade de vida.

**Ainda não decidido:** fórmula matemática, alcance e tamanho das regiões, peso no bem-estar, quais árvores/objetos terão efeito, frequência exata de atualização, necessidade de cache/grade próprios ou metas numéricas de desempenho. São escolhas de implementação e calibração proporcionais, com retorno à SPEC se mudarem a mecânica.

## Evidências úteis e seus limites

| Referência primária | Aprendizado aplicável | Limite |
| --- | --- | --- |
| [Factorio — poluição](https://wiki.factorio.com/Pollution) | Usa unidades espaciais (*chunks*) para representar poluição; o estado não exige comparar todo emissor com todo alvo a cada instante. | É poluição, não felicidade, e o IndexCities já tem modelo próprio de poluição; não copiar tamanho/frequência. |
| [Factorio FFF #161](https://www.factorio.com/blog/post/fff-161) | Agendar apenas regiões com próximo trabalho, em vez de percorrer regiões ociosas a cada tick, trouxe ~7% no caso de grandes fábricas relatado pelo desenvolvedor. | Ganho **exclusivo daquele cenário**; não é estimativa de melhoria do IndexCities. |
| [Cities: Skylines II — serviços e parques](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies) e [valor imobiliário](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings) | O jogo diferencia satisfação de necessidades e disponibilidade de lazer de bônus diretos de valor imobiliário por simplesmente multiplicar serviços. | O IndexCities não precisa copiar o sistema de lazer nem a economia do outro jogo. |
| [OMS — espaços verdes urbanos e saúde (2016)](https://www.who.int/europe/publications/i/item/WHO-EURO-2016-3352-43111-60341) e [síntese de 2025](https://www.who.int/publications/i/item/B09366) | Acesso/exposição a espaços verdes se relacionam a saúde e bem-estar, justificando a direção temática. | A evidência **não** informa uma fórmula de pontos, raios ou felicidade para o jogo. |
| [Salsa — algoritmo incremental](https://github.com/salsa-rs/salsa/blob/master/book/src/reference/algorithm.md) | Dados derivados podem reutilizar resultado quando entradas não mudam; mudanças de entrada não exigem refazer todo o sistema. | É referência conceitual: **não** acrescentar framework de dependências/caching por causa desta mecânica. |
| [Godot — MultiMesh](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html), [LOD/visibilidade](https://docs.godotengine.org/en/stable/tutorials/performance/optimizing_3d_performance.html), [perfilador](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/debugger_panel.html) | Instanciamento, LOD e perfilagem ajudam a medir e controlar muitos objetos visuais; um MultiMesh grande não faz *culling* individual. | Custo da **GPU/renderização** é separado do custo de calcular bem-estar; não há benchmark do jogo. |

## Modelo funcional possível — proposta não aprovada

1. **Fonte concreta.** A vegetação existente no mapa e a que foi realmente colocada/removida pode contribuir para um ambiente residencial mais verde. Contar apenas o que existe fisicamente, com localização e tipo compatíveis. Bancos, cercas, placas, vasos e detalhes pequenos podem ser **somente visuais** no começo; nem tudo recebe pontos.
2. **Consulta local.** Para cada casa ocupada, obter a condição ambiental **no entorno imediato**, por agregados regionais/células já utilizados ou uma busca espacial simples, **sem introduzir um grid novo por obrigação**. Considerar vizinhanças entre regiões para evitar saltos artificiais na borda de uma célula. Não exige que o SIM enxergue cada árvore pela câmera.
3. **Resposta limitada.** O benefício deve ser **pequeno/moderado e saturável**. Vegetação espalhada de modo útil pode ajudar várias moradias; muitos objetos amontoados num mesmo ponto têm retorno adicional baixo. Não permitir que decoração neutralize fome, poluição, violência ou falta de serviços.
4. **Influência no indivíduo.** Cada SIM usa a qualidade do entorno da **residência que efetivamente ocupa** quando sua satisfação já for reavaliada normalmente. Moradores preservam demais condições e preferências pessoais, mas não ganham uma nova rotina de análise por quadro. Mudança de residência ou remoção de vegetação altera a condição consultada.
5. **Relação com valor imobiliário.** Se a condição ambiental influenciar a avaliação local, aproveitar a **rotina já existente de avaliação**, sem criar créditos/dinheiro nem valorização instantânea em cascata só por plantar um vaso. Evitar que valor → felicidade → valor vire realimentação ilimitada.
6. **Não contar duas vezes.** Visita efetiva a parque/praça continua produzindo o benefício de lazer **já aprovado**; o entorno verde não deve atribuir uma segunda visita fictícia. Poluição e lixo já têm causas/efeitos próprios: não reaplicar a mesma penalidade sob o nome de “feiúra”. Árvores **não absorvem magicamente poluição** sem regra específica na SPEC.
7. **Explicação simples.** Na inspeção de moradia/SIM, quando relevante, mostrar causa legível como “entorno arborizado” ou “pouca vegetação”. Mapas temáticos, se exibirem esse dado, devem ler o mesmo estado derivado e não uma nota separada.

### O que aciona o trabalho

| Acontecimento | Trabalho mínimo |
| --- | --- |
| Colocar/remover árvore, jardim ou espaço verde | Invalidar/recalcular só os locais cujo entorno pode mudar; atualizar visão/consultas dependentes quando necessário |
| Construir sobre vegetação preexistente | Considerar a vegetação realmente removida; evitar contar modelo antigo e novo ao mesmo tempo |
| SIM mudar de residência | Próxima consulta da satisfação lê o entorno da nova casa, sem busca global por jardins |
| Alterar apenas câmera, zoom ou frame gráfico | **Nenhum recálculo da qualidade ambiental** |
| Cidade sem alterações ambientais | Reaproveitar dados atuais; não varrer todos os adornos para descobrir que nada mudou |
| Carregar save | Reconstruir/verificar derivados a partir de objetos/ocupação autoritativos, sem perder identidade, seed ou criar estado econômico |

**Cuidado de implementação:** efeitos regionais são um *resultado derivado*, não um novo estado que muda por conta própria. Se uma alteração modificar muitas áreas verdadeiras, essas áreas devem ser processadas; não esconder consequências. Se as consultas simples já forem rápidas, **não implementar cache, filas ou índices complexos** apenas pela hipótese de ganho.

## Comparação exploratória de custos e alternativas de escopo

| Caminho | CPU da simulação | Manutenção/complexidade | Gameplay | Avaliação |
| --- | --- | --- | --- | --- |
| Cada SIM examina objetos e trajetos a cada atualização/frame | Pode crescer com SIMs × objetos consultados × frequência | Alta, invalidação e eventos difíceis | Detalhe provavelmente pouco perceptível | **Não começar assim** |
| Qualidade residencial compartilhada com atualização local por mudança | Consultas proporcionais a áreas/moradias afetadas e ao ritmo normal da satisfação | Baixa a moderada quando reaproveita estruturas existentes | Visível e explicável | **Escolha recomendada** |
| Somente aparência, sem influência causal | Quase nula no núcleo; GPU continua relevante | Mínima | Menos consequência à escolha estética | Fallback se custo/valor não compensar |

**Possível maior gargalo: desenho da cidade, não o índice ambiental.** Muitas árvores, sombras, materiais e instâncias podem pesar na GPU ou na comunicação C# ↔ Godot. Não fazer milhares de nós/consultas à engine por SIM; avaliar instancing/LOD e **agrupar visualmente por áreas quando o perfil justificar**, respeitando a limitação de culling do MultiMesh. Isso não exige que o índice de felicidade use o mesmo tamanho de lote usado pelo renderizador.

## Como verificar sem criar um projeto paralelo

**Quando existir o jogo integrado**, medir nas mesmas cidades/saves/seeds e velocidades (inclusive 3×) em três situações: cidade estável, inserção/remoção concentrada de muitos objetos e expansão grande/alta densidade de moradias. Separar:

- **CPU do Core:** custo específico das consultas ambientais, atualizações de regiões e avaliações derivadas; eventos/s, tempo de simulação por segundo real, p95/p99 de processamento, backlog e alocações/GC.
- **GPU/host:** FPS e tempo de frame, draw calls, sombras, memória e distância de visão **com e sem os meshes decorativos renderizados**, independentemente da influência na felicidade.
- **Correção:** árvores na borda da região, retirada ao construir, residência que muda de bairro, milhares de adornos repetidos e retorno decrescente; confirmar que causas e valor imobiliário não são duplicados.
- **Benefício de gameplay:** o jogador consegue perceber a diferença entre bairros arborizados e desprovidos de verde, entender por quê e querer posicionar áreas verdes? O efeito é significativo, mas não supera necessidades básicas?

**Comparação útil:** com os mesmos objetos visuais, desligar temporariamente **somente o cálculo ambiental** no benchmark permite isolar seu custo; depois medir efeitos de **renderização** separadamente. Isto é uma técnica de medição, **não** uma opção de gameplay ou redução permanente da simulação. Não fixar meta artificial de FPS, população, porcentagem de CPU, raio ou pontos de felicidade antes das medidas. Se o custo exceder o ganho observável, simplificar o cálculo (menos tipos/consultas), sem violar a regra de produto sem nova decisão.

## Limites e melhorias futuras (não aprovadas)

Analisar tempo gasto em trabalho, trajeto, praças, preferências individuais de paisagismo, visual efetivamente percebido, clima, ruído, manutenção estética ou consequências ecológicas próprias de cada espécie **não integra a solução inicial**. Investigar apenas se gameplay/medição demonstrar benefício; evoluções que mudem comportamento devem voltar à SPEC.

**Conclusão:** conservar a decisão 5C numa versão **residencial, local, saturável e acionada por mudanças**, aproveitando a estrutura de dados existente. **Nenhuma evidência atual permite prometer custo zero ou desempenho específico**. Primeiro confirmar que a mecânica acrescenta decisão interessante; depois medir o custo real e manter o mínimo necessário.
