# IndexCities — Genre Benchmark

> **Revisão humana:** PENDENTE.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o conteúdo deste documento ainda não foi revisado integralmente pelo responsável e não pode ser tratado como decisão.
>

> **Status:** exploração temática de referência recorrente — não é fonte de verdade.
>
> Este documento é uma **referência comparativa do gênero city builder**.
>
> Ele reúne evidências externas sobre o que outros jogos, desenvolvedores e comunidades fizeram, o que funcionou, o que falhou e quais trade-offs apareceram.
>
> **Não é uma fonte de requisitos do IndexCities.** A fonte de verdade do produto continua sendo [SPEC.md](../SPEC.md). Hipóteses e decisões ainda abertas continuam em [EXPLORATION.md](../EXPLORATION.md).

## Para que este documento existe

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


O objetivo não é copiar concorrentes nem tentar construir uma média do mercado.

Este benchmark existe para responder periodicamente a uma pergunta simples:

> **O que estamos criando ainda faz sentido como jogo, considerando a experiência que queremos entregar e o que já se aprendeu no gênero?**

Ele deve ajudar a detectar dois tipos de problema:

- estamos repetindo uma falha conhecida sem uma razão consciente;
- estamos nos afastando de padrões do gênero de forma que pode ser excelente, mas precisa ser uma escolha deliberada e validada.

Diferença em relação a outros jogos **não é problema por si só**. Muitas das melhores ideias surgem justamente de quebrar padrões. O benchmark serve para tornar essa diferença consciente.

## Quando revisitar

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Este documento não precisa ser consultado em toda mudança pequena.

Ele deve ser revisitado especialmente quando houver um marco que altere significativamente a experiência do jogo, por exemplo:

1. **antes de fechar o loop principal**;
2. **depois do primeiro vertical slice realmente jogável**;
3. **quando cidadãos, economia, construção e trânsito começarem a interagir entre si**;
4. **quando uma cidade média já puder ser jogada por tempo suficiente para aparecer microgestão e repetição**;
5. **quando o late game começar a existir**;
6. **antes de consolidar metas de escala e performance**;
7. **antes de Alpha / Early Access / lançamento ou outro marco equivalente**;
8. sempre que surgir a sensação de que um sistema tecnicamente impressionante pode estar piorando a experiência.

Em cada revisão, a pergunta não deve ser “estamos iguais aos outros jogos?”, mas:

- o problema que outros jogos encontraram também está aparecendo aqui?
- fizemos uma escolha diferente de forma consciente?
- nossa solução preserva a fantasia e o loop principal do IndexCities?
- a complexidade adicionada está gerando decisão interessante ou apenas trabalho?
- aquilo que prometemos como simulação é realmente perceptível e confiável para o jogador?

## Como interpretar as evidências

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Nem todas as fontes têm o mesmo peso.

| Confiança | Tipo de evidência |
| --- | --- |
| **Alta** | código, documentação oficial, post-mortem, talk técnica, devlog do próprio desenvolvedor |
| **Média** | padrão repetido em muitas avaliações, discussões e comunidades independentes |
| **Baixa** | comentário isolado, opinião individual ou inferência ainda pouco corroborada |

Feedback de jogadores é especialmente útil para detectar **dor, expectativa e comportamento percebido**. Ele não deve ser tratado automaticamente como a melhor solução para o problema.

---

## Benchmark comparativo

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


| Projeto / fonte | Tema | O que fizeram | Funcionou | Problema / risco | Lição comparativa | Confiança |
| --- | --- | --- | --- | --- | --- | --- |
| **SimCity / GlassBox** | Microsimulação | Economia, serviços e mobilidade fortemente baseados em agentes | Processos da cidade ficaram fisicamente observáveis | Tráfego, roteamento e limites de escala viraram problemas centrais | Agentes individuais são uma escolha de arquitetura e produto, não decoração | Alta |
| **SimCity 4 / Micropolis** | Abstração | Grande parte da cidade é modelada de forma agregada | Escala e profundidade sem simular cada pessoa | Menos vínculo com indivíduos | Abstração pode produzir profundidade real quando o detalhe individual não gera gameplay | Alta |
| **Cities: Skylines** | Cidade + mobilidade | Trânsito e infraestrutura são parte importante do loop | Forte capacidade de construir e observar redes urbanas | Trânsito/pathfinding domina parte da experiência e do custo técnico | Mobilidade precisa ser divertida e diagnosticável, não apenas complexa | Alta/Média |
| **Cities: Skylines II** | Simulação e performance | Aumentou ambição de agentes, economia e detalhe | Promessa forte de cidade viva e sistêmica | Performance e transparência da simulação viraram problemas centrais após lançamento | Quanto maior a promessa de simulação, maior a exigência de coerência percebida | Alta |
| **Banished** | Agentes e pathfinding | População individual com trabalho, residência e rotinas | Forte vínculo entre pessoas e funcionamento da cidade | Pathfinding exigiu correções recorrentes e casos extremos podiam destruir performance | Pathfinding deve ser tratado como risco estrutural desde cedo | Alta |
| **Tropico 6** | Cidadãos individuais | Pessoas autônomas ajudam a criar personalidade e apego | A cidade parece formada por indivíduos reais | Decisão/pathfinding de agentes precisa ser eficiente e legível | Individualidade vale quando o jogador percebe histórias e consequências | Alta |
| **Workers & Resources** | Realismo operacional | Logística, construção, infraestrutura e trabalho extremamente detalhados | Profundidade é o principal atrativo para parte do público | A mesma profundidade pode virar burocracia e repetição | Complexidade pode ser produto; repetição sem nova decisão não deve ser | Média |
| **Songs of Syx** | Escala + indivíduos | Populações enormes com indivíduos, mas grande automação de tarefas | Mantém sensação de escala sem exigir micro constante | Controle manual total seria inviável | Conforme a cidade cresce, o nível de decisão do jogador também precisa subir | Alta/Média |
| **Against the Storm** | Progressão e late game | Cidades terminam antes de entrar em estado totalmente resolvido | Mantém decisões interessantes e variedade entre partidas | Não atende a fantasia de cidade permanente | Todo city builder precisa responder o que acontece quando a cidade “já funciona” | Alta |
| **Kingdoms Reborn** | Foco do loop | Testou combate RTS e outras features grandes | Iteração com jogadores ajudou a encontrar o foco | Combate desviava a atenção da construção e precisou ser removido/refeito | Uma feature boa isoladamente pode piorar o jogo inteiro | Alta |
| **Farthest Frontier** | Construção | Grid estratégico e depois posicionamento livre em 360° | Grid dá legibilidade; liberdade aumenta expressão | Cada extremo sacrifica algo | Ferramentas híbridas podem equilibrar precisão, estratégia e estética | Alta |
| **Ostriv** | Profundidade e UX | Prefere aprofundar sistemas existentes e melhorar diagnóstico | Sistemas ganham coerência e significado | Desenvolvimento fica mais lento e exige retrabalho | Profundidade bem explicada vale mais que catálogo enorme de features | Alta |
| **Urbek** | Abstração seletiva | Profundidade espacial e econômica sem microsimular tudo | Baixa burocracia com decisões urbanísticas fortes | Menos atraente para quem busca vida individual profunda | Nem todo sistema precisa usar o mesmo nível de fidelidade | Média |
| **Timberborn** | Sistema central diferenciador | Água e terreno são sistemas físicos simplificados, mas centrais | Um sistema forte gera muitas decisões emergentes | Simulação física completa seria desnecessária e cara | Simular profundamente o que gera gameplay; aproximar o restante | Alta |
| **Anno 1800** | Economia e logística | Cadeias de produção e transporte são o centro da economia | Fluxos materiais dão sentido ao crescimento | Complexidade cresce muito conforme cadeias se acumulam | Economia profunda funciona melhor quando seus fluxos são visíveis | Alta |
| **Citybound** | Microsimulação em escala | Arquitetura orientada especificamente para muitos agentes e interações | Ambição técnica coerente com a visão | Escopo enorme e desenvolvimento muito longo | Microsimulação profunda precisa justificar o custo de arquitetura e desenvolvimento | Alta |
| **A/B Street** | Tráfego e scheduling | Migrou de timestep frequente para simulação baseada em eventos | Mantém agentes sem atualizar todos continuamente | Scheduler e eventos aumentam complexidade do modelo | Entidade persistente não precisa significar processamento contínuo | Alta |
| **OpenTTD** | Pathfinding | Hierarquia, caches e otimizações para redes enormes | Escala por décadas de jogo e mapas complexos | Invalidação e manutenção de caches são difíceis | Pathfinding global ingênuo não deve ser a arquitetura final para grande escala | Alta |
| **Highrise City** | Escala percebida | População lógica muito maior que entidades visualmente ativas | Permite sensação de megacidade | Números exibidos podem ser mais agregados que individuais | População lógica, agentes ativos e representação visual são métricas diferentes | Alta |
| **SlimCity** | Arquitetura leve | Simulação determinística separada de renderização; tráfego estatístico | Boa performance e separação de responsabilidades | Menos fidelidade individual | Serve como extremo agregado para comparar com microsimulação real | Alta |
| **IsoCity** | Protótipo integrado | Pedestres, veículos, economia, zoning e pathfinding no mesmo projeto | Mostra que é possível prototipar rapidamente | Acoplamento cresce muito quando todos os sistemas aparecem juntos | Primeiro provar interação; depois aprofundar arquitetura | Alta |
| **r/CityBuilders / Steam** | Expectativas de jogadores | Discussões recorrentes sobre realismo, micro, UI e endgame | Jogadores valorizam coerência, consequências e identidade | Rejeitam tanto simulação “fake” quanto burocracia excessiva | Espaço promissor: mundo coerente com baixa fricção operacional | Média |
| **r/gamedev** | Muitos agentes | Batching, LOD, eventos e frequências diferentes aparecem repetidamente | Técnicas simples reduzem muito o custo | Atualizar tudo todo frame não escala | Cada sistema deve ter orçamento e frequência adequados | Média |

---

## Padrões recorrentes do gênero

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


### 1. Complexidade só vale quando produz decisão

Adicionar estados, números, agentes ou cadeias não cria profundidade automaticamente.

Uma complexidade é valiosa quando o jogador consegue:

1. perceber que existe;
2. entender a causa;
3. tomar uma decisão;
4. observar a consequência.

Quando uma etapa falha, a complexidade tende a virar ruído.

### 2. Realismo e burocracia são coisas diferentes

Jogadores frequentemente gostam de cidades coerentes, serviços que realmente funcionam, empregos reais, logística e cidadãos persistentes.

A rejeição aparece quando coerência exige repetir manualmente operações já compreendidas.

Uma heurística útil:

> **Problema novo = gameplay. Procedimento repetido = candidato a automação.**

### 3. Simule indivíduos; gerencie populações

Identidade individual pode ser mantida mesmo quando o processamento e o controle são agregados.

É possível separar:

- cidadão existente;
- cidadão com evento pendente;
- cidadão realizando atividade;
- cidadão em deslocamento;
- cidadão próximo da câmera;
- cidadão renderizado em alto detalhe.

Esses grupos não precisam ter o mesmo tamanho nem a mesma frequência de atualização.

### 4. A escala precisa mudar o nível de decisão

Uma cidade pequena pode justificar decisões prédio por prédio.

Conforme cresce, repetir as mesmas operações tende a deixar de ser interessante.

O jogo pode precisar evoluir de algo como:

- “onde coloco esta loja?”
- para “este bairro precisa de comércio”
- para “quero incentivar comércio nesta região”.

Isso não determina a solução do IndexCities, mas é um risco importante a acompanhar.

### 5. Sistemas profundos precisam explicar seu estado

Uma simulação pode ser correta e ainda parecer quebrada.

Se uma empresa ficou sem funcionários, o jogador precisa conseguir descobrir a cadeia causal relevante: transporte, distância, salário, residência, qualificação ou qualquer outro fator real.

Diagnóstico, overlays e UI fazem parte do design da simulação.

### 6. Pathfinding e scheduling são riscos arquiteturais

Nos casos estudados, mobilidade em grande escala raramente permanece como “A* completo para cada agente, sempre”.

Aparecem repetidamente:

- hierarquia;
- caches;
- rotas reutilizadas;
- eventos;
- LOD;
- atualização em frequências diferentes;
- representação agregada fora das áreas importantes.

### 7. O late game precisa de uma resposta

Quando comida, dinheiro, infraestrutura e mobilidade deixam de ser problemas, muitos city builders entram em estado resolvido.

As respostas encontradas no gênero incluem:

- novas camadas de sistemas;
- eventos e crises;
- objetivos;
- expansão regional;
- dificuldade crescente;
- cidades que terminam;
- sandbox puramente criativo.

O IndexCities ainda precisa decidir qual resposta combina com sua fantasia.

---

## Perguntas para comparar com o IndexCities

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Ao revisitar este benchmark, use perguntas como estas:

### Loop e identidade
- Qual é a ação que continua divertida mesmo depois de muitas horas?
- Nossos sistemas reforçam essa ação ou competem com ela?
- Existe alguma feature tecnicamente impressionante roubando atenção do jogo principal?

### Simulação
- O jogador consegue perceber por que algo aconteceu?
- Há detalhes simulados que não produzem decisão nem consequência?
- Estamos gastando CPU com fidelidade que o jogador não vê?

### Microgestão
- Quantas vezes o jogador repete a mesma solução manualmente?
- Existe um ponto em que automação, políticas, seleção múltipla ou gestão por área deveria substituir o controle individual?

### Construção
- A construção permite expressão suficiente?
- As restrições existentes produzem decisões interessantes ou apenas atrito?
- A cidade resultante parece pertencer ao jogador?

### Escala
- O jogo fica mais interessante quando cresce ou apenas mais trabalhoso?
- O nível de controle evolui junto com a escala?
- Performance e legibilidade continuam aceitáveis em cidades maduras?

### Confiança na simulação
- O jogador consegue seguir causa → consequência?
- A cidade faz coisas que contradizem suas próprias regras aparentes?
- Uma informação importante está escondida em menus, alertas genéricos ou números sem explicação?

### Late game
- O que o jogador passa a querer depois que a cidade básica funciona?
- Novos sistemas mudam decisões ou apenas aumentam números?
- Existe uma razão para continuar além de “crescer porque sim”?

---

## Regra para futuras revisões

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Quando uma comparação revelar um problema:

- **não copie automaticamente a solução usada por outro jogo**;
- registre o problema e as alternativas em [EXPLORATION.md](../EXPLORATION.md);
- experimente quando necessário;
- somente uma decisão fechada do IndexCities deve entrar na [SPEC.md](../SPEC.md).

Quando o IndexCities deliberadamente escolher um caminho diferente do padrão do gênero, essa diferença deve ser tratada como uma escolha consciente a validar, não como erro automático.

---

## Fontes principais

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Esta lista não é exaustiva; ela preserva as referências de maior utilidade encontradas até agora.

### Desenvolvedores, post-mortems e documentação

- Against the Storm — GDC Postmortem: https://gdcvault.com/play/1034422/-Against-the-Storm
- Kingdoms Reborn — entrevista de desenvolvimento: https://www.unrealengine.com/developer-interviews/inside-kingdoms-reborn-s-game-dev-s-journey-of-discovery-and-city-building
- Tropico 6 — simulação de agentes: https://www.unrealengine.com/developer-interviews/limbic-entertainment-revamps-tropico-6-with-unreal-engine-4
- Tropico 6 — cidadãos individuais: https://www.unrealengine.com/developer-interviews/how-new-developers-rebuilt-a-banana-republic-for-tropico-6
- Timberborn — water mechanics deep dive: https://www.gamedeveloper.com/design/deep-dive-timberborn-s-water-mechanics
- Anno 1800 — logística: https://www.anno-union.com/devblog-pushing-carts/
- Manor Lords — desenvolvimento: https://www.unrealengine.com/developer-interviews/solo-dev-makes-sophisticated-sim-manor-lords-using-unreal-engine
- Ostriv — devlog: https://ostrivgame.com/alpha-2-released/
- Farthest Frontier — free build 360°: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- SimCity / GlassBox retrospective: https://simscommunity.info/2013/10/04/blog-post-state-of-simcity/

### Código e arquitetura aberta

- Citybound: https://github.com/citybound/citybound
- A/B Street: https://github.com/a-b-street/abstreet
- A/B Street — discrete event simulation: https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
- OpenTTD — hierarchical pathfinder: https://www.openttd.org/news/2024/02/24/new-ship-pathfinder
- OpenTTD — extreme saves/performance: https://www.openttd.org/news/2019/04/01/monthly-dev-post
- Micropolis / SimCity Classic: https://github.com/osgcc/simcity
- IsoCity: https://github.com/amilich/isometric-city
- SlimCity: https://github.com/rbenzing/SlimCityGame
- LinCity-NG: https://github.com/lincity-ng/lincity-ng

### Comunidades

- r/CityBuilders — discussões sobre realismo, tédio e endgame: https://www.reddit.com/r/CityBuilders/
- r/gamedev — discussões de escala de agentes: https://www.reddit.com/r/gamedev/
- Steam Community — Workers & Resources: https://steamcommunity.com/app/784150/discussions/
- Steam Community — Cities: Skylines II: https://steamcommunity.com/app/949230/discussions/


### Grid hierárquico / subgrid para detalhe local

**Status:** referência técnica promissora; não é decisão de produto.

Uma ideia que merece teste no IndexCities é separar a resolução lógica do mapa em pelo menos dois níveis:

- **grid macro:** usado para estrutura urbana, ocupação principal, lotes, ruas e edifícios;
- **subgrid local:** usado dentro ou ao redor de uma célula/elemento macro para detalhes menores, alinhamento fino e mobiliário urbano.

A referência mais clara encontrada até agora é **Anno 117: Pax Romana**. A equipe da Ubisoft subdividiu cada tile do grid em **4 subtiles** para permitir estradas e edifícios diagonais sem abandonar a precisão e a simplicidade do grid. Eles também mudaram a representação visual das ruas para um grafo entre nós, em vez de renderizar cada pedaço como um tile independente.

Isso não prova que o IndexCities deva usar exatamente 2x2 ou aplicar o mesmo sistema a todos os objetos, mas valida o princípio de **manter uma grade estrutural mais grossa e usar resolução mais fina quando o problema exige**.

Uma extensão a prototipar seria permitir que uma célula de rua/lote exponha posições locais menores para elementos como:

- bancos;
- postes;
- árvores;
- lixeiras;
- canteiros/grama;
- pontos de ônibus;
- placas;
- faixas e outros detalhes de calçada.

Nesse modelo, esses elementos não precisariam consumir uma célula inteira do grid urbano.

**Cuidados:**

- não criar subcélulas para tudo apenas por flexibilidade futura;
- separar ocupação lógica importante de decoração visual;
- evitar multiplicar memória/estado globalmente se o subgrid só for necessário localmente;
- testar se a granularidade menor realmente melhora construção e legibilidade;
- considerar coordenadas locais ou slots paramétricos dentro do elemento em vez de materializar uma matriz completa quando isso for suficiente.

**Referências:**
- Anno 117 — Roads & building in the grid: https://www.anno-union.com/devblog-roads-building-in-the-grid/
- Anno 117 — All roads lead to Anno: https://www.anno-union.com/devblog-all-roads-lead-to-anno/
- Simtropolis — discussão histórica sugerindo grids menores guiados pela largura de faixas: https://community.simtropolis.com/forums/topic/16662-simtropolis-1000/?page=21

---

## Pesquisa histórica ampliada migrada do hub

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


> Esta seção preserva a rodada extensa de pesquisa que antes vivia no `docs/EXPLORATION.md`. O benchmark curado acima continua sendo a leitura principal; o material abaixo serve como evidência e histórico adicional.

### Pesquisa comparativa de city builders

**Status:** em exploração.

Esta seção registra padrões recorrentes encontrados em post-mortems, entrevistas de desenvolvedores, devlogs e feedback de comunidades. Eles servem como evidência comparativa; não são requisitos automáticos do IndexCities.

### Padrões que se repetem

- **O loop principal precisa vencer as features secundárias.** Kingdoms Reborn testou combate RTS e descobriu que muitos jogadores paravam de construir a cidade para lidar com as batalhas. O sistema foi removido e depois reintroduzido de forma muito mais simples. Uma feature pode ser boa isoladamente e ainda enfraquecer o jogo inteiro.
- **Complexidade útil é a que produz decisões legíveis.** Against the Storm cortou e reformulou sistemas quando eles aumentavam complexidade sem melhorar a experiência. Sua evolução reforça a importância de testar cedo com jogadores reais.
- **Simulação individual compra vínculo, mas cobra CPU e design.** Tropico 6 implementou cidadãos autônomos porque a equipe considerava que agentes individuais aumentavam personalidade e apego. Ao mesmo tempo, pathfinding e decisão de agentes foram prototipados desde cedo por serem sistemas centrais e caros.
- **Pathfinding é risco estrutural, não detalhe de acabamento.** Em Banished, o pathfinding foi descrito pelo próprio desenvolvedor como um sistema corrigido continuamente; casos sem caminho podiam levar a buscas enormes e derrubar o frame rate quando muitos agentes falhavam simultaneamente.
- **Escala e arquitetura precisam ser pensadas juntas.** Citybound adotou arquitetura orientada a atores, mensagens e otimizações de localidade de memória justamente para perseguir simulação microscópica em larga escala. A ambição de simulação determinou a arquitetura, e não o contrário.
- **Automação é uma ferramenta de escala.** Songs of Syx busca populações enormes, mas explicitamente automatiza tarefas mundanas para que o jogador passe a decidir em nível cada vez mais alto conforme a cidade cresce.
- **Logística pode ser o coração do jogo.** Anno 1800 trata transporte e cadeias de produção como parte central da economia; a cidade cresce porque fluxos materiais funcionam, não apenas porque indicadores abstratos sobem.
- **Um sistema físico diferenciador pode carregar o jogo.** Timberborn investiu pesadamente em água e irrigação e usou um modelo híbrido de simulação para obter comportamento interessante sem exigir fidelidade física total. A lição é simular com precisão aquilo que produz gameplay e aproximar o restante.
- **Early Access melhora o jogo, mas reduz liberdade para mudanças radicais depois.** Kingdoms Reborn relata que feedback e receita foram extremamente úteis, mas que mudanças rápidas em sistemas centrais passaram a exigir mais cautela depois que jogadores criaram expectativas sobre o produto.
- **A comunidade não deve dirigir o produto por votação.** O mesmo relato de Kingdoms Reborn mostra preferências conflitantes e features refeitas várias vezes. Feedback é evidência sobre problemas e desejos; a solução ainda precisa preservar a identidade do jogo.
- **Limitações de construção estética importam para jogadores modernos.** Em 2026, Farthest Frontier decidiu substituir a limitação rígida de grid por posicionamento livre em 360 graus após feedback recorrente da comunidade, apesar de o grid ter sido uma decisão estratégica original.
- **Performance tardia pode destruir a própria fantasia de escala.** Cities XL mostrou historicamente que cidades grandes com simulação pesada e arquitetura incapaz de usar bem o hardware podem transformar o crescimento, que deveria ser recompensa, em degradação progressiva da experiência.
- **Profundidade não exige que tudo seja simulado da mesma forma.** Jogos bem-sucedidos variam radicalmente: alguns simulam cidadãos individualmente; outros concentram profundidade em logística, espaço, recursos ou decisões sociais. O nível de fidelidade deve seguir a fantasia central.

### Heurísticas provisórias para futuros protótipos

Estas heurísticas ainda não são decisões de produto:

1. começar pela menor versão do loop que já permite uma decisão interessante;
2. medir uma feature pelo quanto ela fortalece o loop central, não por quão impressionante ela é isoladamente;
3. tratar pathfinding, trânsito, economia e quantidade de agentes como problemas de arquitetura desde os primeiros benchmarks;
4. permitir que o nível de controle do jogador suba conforme a escala cresce, evitando repetir manualmente operações que já foram compreendidas;
5. separar fidelidade de simulação de fidelidade visual: um cidadão pode ter estado persistente sem exigir atualização completa e renderização permanente;
6. preferir sistemas cuja causa e consequência possam ser explicadas ao jogador;
7. prototipar o maior risco técnico e o maior risco de diversão cedo, antes de produzir grande quantidade de conteúdo;
8. observar jogadores em vez de confiar apenas em opinião declarada: comportamento real costuma revelar problemas diferentes dos pedidos explícitos;
9. manter a possibilidade de cortar ou simplificar sistemas que desviem atenção do city building;
10. definir explicitamente qual fantasia domina o IndexCities antes de comprar complexidade para sistemas auxiliares.

### Referências principais desta rodada

- GDC Vault, Against the Storm Postmortem: https://gdcvault.com/play/1034422/-Against-the-Storm
- Kingdoms Reborn — entrevista com Ittinop Dumnernchanvanit: https://www.unrealengine.com/developer-interviews/inside-kingdoms-reborn-s-game-dev-s-journey-of-discovery-and-city-building
- Tropico 6 — pesquisa, design claims e simulação de agentes: https://www.unrealengine.com/developer-interviews/limbic-entertainment-revamps-tropico-6-with-unreal-engine-4
- Tropico 6 — agentes individuais e vínculo com a população: https://www.unrealengine.com/developer-interviews/how-new-developers-rebuilt-a-banana-republic-for-tropico-6
- Banished — pathfinding: https://banished-wiki.com/wiki/Pathfinding
- Citybound — arquitetura e simulação: https://aeplay.org/citybound
- Citybound — Living Design Doc: https://app.notion.com/aeplay/citybound-living-design-doc-3b42707cbca54d079d301d9190ac85bb
- Songs of Syx — proposta de simulação em grande escala com automação: https://songsofsyx.com/
- Anno 1800 — logística: https://www.anno-union.com/devblog-pushing-carts/
- Timberborn — deep dive da simulação de água: https://www.gamedeveloper.com/design/deep-dive-timberborn-s-water-mechanics
- Farthest Frontier — evolução pós-Early Access e construção livre: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Cities XL — histórico de gargalos de CPU/memória: https://community.simtropolis.com/forums/topic/34341-multicore-support/
- Surviving Mars — como a fantasia central alterou o city builder tradicional: https://www.gamedeveloper.com/design/how-the-i-surviving-mars-i-devs-built-a-city-builder-on-a-barren-planet
- Manor Lords — jornada de desenvolvimento solo e prototipagem: https://www.unrealengine.com/developer-interviews/solo-dev-makes-sophisticated-sim-manor-lords-using-unreal-engine


### Pesquisa ampliada: repositórios, comunidades e vídeos

**Status:** evidência adicional, não decisão de produto.

| Fonte | O que mostra | Sinal positivo | Risco / sinal negativo | Lição provisória |
| --- | --- | --- | --- | --- |
| IsoCity (GitHub) | City builder isométrico com veículos, pedestres, pathfinding, economia e zoning | Mostra que é possível prototipar muitos sistemas em arquitetura simples | Misturar tráfego, pedestres, economia e crescimento aumenta rapidamente o acoplamento | Usar como referência de protótipo, não como prova de escala |
| SlimCity (GitHub) | Simulação determinística em Web Worker, renderização instanciada e tráfego estatístico | Separa simulação de apresentação e ganha desempenho | Tráfego deixa de ser totalmente individual | Forte referência para comparar agente real vs representação estatística |
| KotCity (GitHub) | Engine multithread, A*, economia dinâmica, overlays e mapas grandes | Arquitetura explícita para cálculo paralelo | Roadmap muito amplo mostra como o escopo cresce rapidamente | Escopo e arquitetura precisam permanecer proporcionais |
| Cimulity (GitHub) | Núcleo pequeno, mapa 64x64, fixed timestep e serviços essenciais | Simulação legível e limitada facilita iteração | Menos profundidade e escala | Boa referência para vertical slice pequena |
| Citybound (GitHub) | Realismo, detalhes microscópicos, Rust e actor model | Ambição técnica tratada desde a arquitetura | Projeto extremamente ambicioso e longo | Microsimulação exige arquitetura dedicada e disciplina de escopo |
| Micropolis / SimCity Classic (GitHub) | Código original/derivado de SimCity separado da interface em versões modernas | Demonstra o valor duradouro de separar engine de simulação e UI | Modelo antigo é muito agregado para alguns objetivos modernos | Abstração pode gerar gameplay profundo sem agentes individuais |
| LinCity-NG (GitHub) | Projeto mantido por décadas, economia e sustentabilidade | Longevidade e código aberto mostram valor de sistemas estáveis e compreensíveis | Evolução longa cria compatibilidade e dívida de saves | Versionar persistência cedo quando o jogo começar a estabilizar |
| ProcIsoCity (GitHub) | Save versionado, deltas, checksums, tráfego opcional por passes | Persistência e determinismo são tratados como sistemas de primeira classe | A cada feature o formato de save ganha complexidade | Savegame deve ser considerado na arquitetura antes de produção de conteúdo |
| r/gamedev | Discussão sobre centenas de agentes | Atualizações espaçadas, abstração, batching e LOD são práticas recorrentes | Atualizar tudo a cada frame é inviável | Frequência de atualização deve variar por sistema e relevância |
| r/CityBuilders | Realismo sem tédio | Jogadores querem escala e coerência | Rejeitam tanto abstração artificial quanto micro excessivo | Procurar coerência com baixa burocracia |
| Steam — Workers & Resources | Feedback sobre UI, empregos, terreno e logística | Público hardcore aceita muita profundidade | Falta de informação e repetição transformam profundidade em trabalho | Complexidade precisa de diagnóstico, automação e controles em lote |
| Steam — Cities: Skylines II | Discussões sobre simulação vs city painter | Jogadores valorizam causa e consequência reais | Se agentes/economia parecem falsos, confiança na simulação cai | Não prometer profundidade que o jogador consegue contradizer facilmente |
| Farthest Frontier fórum | Migração de grid rígido para colocação 360° | Liberdade estética é valorizada | Grid rígido limitou expressão visual | Construção precisa equilibrar precisão, legibilidade e expressão |
| Against the Storm — GDC / entrevistas | O início da cidade é a parte mais interessante; resets evitam late game estagnado | Repetição com variação mantém decisões frescas | Cidade infinita tende a chegar a estado resolvido | Precisamos resolver cedo qual é o propósito do late game |
| Songs of Syx — site/devlogs/entrevistas | Milhares de indivíduos detalhados com tarefas mundanas automatizadas | Profundidade individual pode coexistir com grande escala | Sem automação, a escala seria impraticável para o jogador | O nível de controle deve subir conforme a cidade cresce |
| Discussões sobre endgame em r/CityBuilders | Jogadores divergem entre sandbox, vitória e campanhas | Metas e novos desafios mantêm motivação | Crescimento sem novos problemas vira rotina | Progressão precisa mudar a natureza das decisões, não só aumentar números |

Referências desta rodada incluem: GitHub (IsoCity, SlimCity, KotCity, Cimulity, Citybound, Micropolis, LinCity-NG, ProcIsoCity), GDC Vault, Steam Community, Reddit r/CityBuilders e r/gamedev, Crate Entertainment Forums e devlogs/entrevistas de Songs of Syx.


### Rodada ampliada: arquitetura técnica, comunidades e casos históricos

**Status:** exploração; nenhuma linha abaixo é requisito automático.

| Referência | Evidência observada | O que funcionou | O risco / falha | Relevância para o IndexCities |
| --- | --- | --- | --- | --- |
| A/B Street | Microsimulação de carros, bicicletas e pedestres; migrou de timestep discreto para simulação por eventos | Agentes individuais sem precisar acordar todos a cada 0,1 s | Modelo exige scheduler/eventos e simplificações explícitas | Forte referência para cidadãos/veículos persistentes com atualização sob demanda |
| OpenTTD | Pathfinding em mapas enormes; introduziu pathfinding hierárquico por regiões e caches | Reduziu busca de baixa resolução e reutilizou caminhos | Manter abstrações e invalidá-las após mudanças aumenta complexidade | Considerar hierarquia/caches em vez de A* global por viagem |
| OpenTTD — saves extremos | Save real com ~50 mil estações e outro com ~13,9 mil veículos expôs gargalos | Saves patológicos viraram benchmarks de otimização | Escala real revela problemas invisíveis em mapas pequenos | Criar saves sintéticos grandes como benchmark desde cedo |
| SlimCity | Simulação determinística em worker, fixed timestep, renderização instanciada, tráfego estatístico | Separação forte entre sim e apresentação | Tráfego não é microsimulado | Referência do extremo agregado para comparar com agentes reais |
| IsoCity | Veículos/pedestres autônomos, semáforos, economia e zoning | Prova que muitos sistemas cabem em protótipo simples | Acoplamento cresce rapidamente | Útil para estudar interfaces mínimas entre sistemas |
| Citybound | Microsimulação como objetivo central e arquitetura criada para isso | A arquitetura nasce da ambição | Escopo técnico gigantesco e desenvolvimento muito longo | Evitar copiar a ambição sem validar o valor para o jogador |
| SimCity 2013 / GlassBox | Agent-based simulation tornou tráfego e economia centrais | Criou sensação de processos físicos percorrendo a cidade | Ajuste de agentes e limites de mapa viraram problemas centrais pós-lançamento | Agentes são uma escolha de produto + arquitetura, não um detalhe visual |
| Ostriv | Prefere melhorar sistemas existentes antes de adicionar mais conteúdo | Detalhe e coerência tornam features existentes mais significativas | O gênero exige ciclos longos de pensar/testar/refazer | Melhorar profundidade antes de ampliar catálogo de prédios |
| Ostriv — UI/construção | Refez UI, footprint de prédios, fila e indicadores de construção travada | Diagnóstico reduz micro e frustração | Sem feedback, jogador não sabe por que algo parou | Todo sistema profundo precisa explicar seu estado e seus bloqueios |
| Farthest Frontier | Grid inicialmente escolhido por valor estratégico; depois free-build 360° após anos de feedback | Grid ajudou legibilidade/estratégia; free-build aumentou expressão | Liberdade total também pode tornar posicionamento mais trabalhoso | Talvez oferecer precisão/assistência opcional em vez de dogma grid vs livre |
| Farthest Frontier — QoL | Comunidade pede menos cliques, atalhos, melhor janela de placement e menos obstrução | QoL preserva profundidade sem remover sistema | UI pode transformar uma boa mecânica em fadiga física | Medir ações repetidas e distância de interação, não só regras da simulação |
| Project Highrise | Sistemas complexos são introduzidos progressivamente conforme torre cresce | Camadas de complexidade aparecem quando passam a importar | Expor tudo no início seria esmagador | Unlock deve ensinar sistemas por necessidade, não apenas premiar nível |
| Highrise City | Promete dezenas de milhões de cidadãos e dezenas de milhares de edifícios, com número visual/simulado de veículos muito menor | Diferencia população lógica de representação ativa | Números gigantes podem ser mais agregados que individuais | População total, agentes ativos e entidades visuais devem ser métricas separadas |
| TheoTown — comunidade | Muito citado como leve e capaz de rodar em hardware fraco | Escala visual relevante sem hardware extremo | Menos fidelidade gráfica/individual | Hardware-alvo é também uma decisão de design do nível de simulação |
| Urbek — comunidade | Frequentemente recomendado a quem quer construção com pouco micro | Relações espaciais e recursos substituem parte da microgestão | Menos interessante para quem busca cidadão individual | Profundidade espacial pode substituir profundidade agent-based em alguns sistemas |
| r/CityBuilders — realismo vs tédio | Jogadores pedem repetidamente realismo de escala sem a burocracia de Workers & Resources | Coerência é valorizada | Realismo operacional passo a passo cansa parte do público | Alvo promissor: mundo coerente com baixa burocracia manual |
| r/CityBuilders — zen builders | Parte do público rejeita city painters sem sistemas | Sistemas dão propósito ao layout | Cidade puramente estética pode parecer sem vida | Construção visual deve produzir consequência observável |
| r/CityBuilders — endgame | Parte prefere meta; parte prefere sandbox; consenso frequente é que problemas acabam cedo demais | Objetivos/opções diferentes prolongam interesse | Crescimento linear termina em estado resolvido | Progressão precisa mudar decisões ou permitir novos objetivos |
| r/gamedev — 500+ agentes | Práticas recorrentes: abstração, batching, LOD, pathfinding esporádico, atualizações em frequências diferentes | Reduz CPU sem abandonar agentes | Implementar ingenuamente tudo por frame não escala | Projetar scheduler de simulação e budgets explicitamente |
| Comunidade low-end | SimCity 4, TheoTown e indies leves continuam citados por rodar bem | Acessibilidade de hardware amplia público | Jogos modernos frequentemente ficam CPU/GPU-heavy | Definir orçamento de CPU/GPU/memória antes da ambição crescer |

#### Padrões técnicos que ficaram mais fortes nesta rodada

1. **Entidade persistente não precisa significar processamento contínuo.** Estado pode existir sempre e ser atualizado por eventos, agenda ou frequência adaptativa.
2. **Pathfinding deve ser hierárquico, cacheável e invalidável.** Buscar do zero em toda viagem é a solução mais simples, não a mais escalável.
3. **População lógica, agentes ativos e representação visual são grandezas diferentes.** Elas podem ter ordens de magnitude distintas.
4. **Saves extremos devem fazer parte do benchmark.** Vários gargalos só aparecem depois que redes e cidades ficam grandes.
5. **Interface é parte da simulação.** Se o jogador não entende causa, gargalo ou bloqueio, a profundidade se transforma em ruído.
6. **Automação deve aparecer depois que a decisão já foi aprendida.** Isso preserva o valor inicial da microgestão sem obrigar sua repetição em escala.
7. **Construção precisa equilibrar liberdade e assistência.** Grid dá legibilidade; free-build dá expressão; ferramentas híbridas podem capturar ambos.
8. **Escala é uma decisão de arquitetura.** Aumentar população, rede viária ou quantidade de edifícios não é apenas mudar um número.

Fontes principais adicionais:
- A/B Street — discrete event simulation: https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
- A/B Street — repositório: https://github.com/a-b-street/abstreet
- OpenTTD — novo pathfinder hierárquico de navios: https://www.openttd.org/news/2024/02/24/new-ship-pathfinder
- OpenTTD — performance em saves extremos: https://www.openttd.org/news/2019/04/01/monthly-dev-post
- SimCity / GlassBox retrospective: https://simscommunity.info/2013/10/04/blog-post-state-of-simcity/
- Ostriv — Alpha 2: https://ostrivgame.com/alpha-2-released/
- Project Highrise — entrevista: https://www.gamegrin.com/articles/project-highrise-interview/
- Highrise City — Steam: https://store.steampowered.com/app/1489970/Highrise_City/
- Farthest Frontier — grid/free placement: https://forums.crateentertainment.com/t/building-direction/123861/5
- Farthest Frontier — free build 360: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Reddit r/gamedev — 500+ agentes: https://www.reddit.com/r/gamedev/comments/1uccg2b/how_can_colony_management_games_simulate_500/
- Reddit r/CityBuilders — realista sem tédio: https://www.reddit.com/r/CityBuilders/comments/1ve9qdu/best_city_builder_that_is_realistic_but_not/
- Reddit r/CityBuilders — endgame: https://www.reddit.com/r/CityBuilders/comments/1uiy417/should_city_builders_ever_have_a_true_endgame/


---
