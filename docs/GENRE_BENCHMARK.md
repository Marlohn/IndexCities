# IndexCities — Genre Benchmark

> Este documento é uma **referência comparativa do gênero city builder**.
>
> Ele reúne evidências externas sobre o que outros jogos, desenvolvedores e comunidades fizeram, o que funcionou, o que falhou e quais trade-offs apareceram.
>
> **Não é uma fonte de requisitos do IndexCities.** A fonte de verdade do produto continua sendo [SPEC.md](SPEC.md). Hipóteses e decisões ainda abertas continuam em [EXPLORATION.md](EXPLORATION.md).

## Para que este documento existe

O objetivo não é copiar concorrentes nem tentar construir uma média do mercado.

Este benchmark existe para responder periodicamente a uma pergunta simples:

> **O que estamos criando ainda faz sentido como jogo, considerando a experiência que queremos entregar e o que já se aprendeu no gênero?**

Ele deve ajudar a detectar dois tipos de problema:

- estamos repetindo uma falha conhecida sem uma razão consciente;
- estamos nos afastando de padrões do gênero de forma que pode ser excelente, mas precisa ser uma escolha deliberada e validada.

Diferença em relação a outros jogos **não é problema por si só**. Muitas das melhores ideias surgem justamente de quebrar padrões. O benchmark serve para tornar essa diferença consciente.

## Quando revisitar

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

Nem todas as fontes têm o mesmo peso.

| Confiança | Tipo de evidência |
| --- | --- |
| **Alta** | código, documentação oficial, post-mortem, talk técnica, devlog do próprio desenvolvedor |
| **Média** | padrão repetido em muitas avaliações, discussões e comunidades independentes |
| **Baixa** | comentário isolado, opinião individual ou inferência ainda pouco corroborada |

Feedback de jogadores é especialmente útil para detectar **dor, expectativa e comportamento percebido**. Ele não deve ser tratado automaticamente como a melhor solução para o problema.

---

## Benchmark comparativo

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

Quando uma comparação revelar um problema:

- **não copie automaticamente a solução usada por outro jogo**;
- registre o problema e as alternativas em [EXPLORATION.md](EXPLORATION.md);
- experimente quando necessário;
- somente uma decisão fechada do IndexCities deve entrar na [SPEC.md](SPEC.md).

Quando o IndexCities deliberadamente escolher um caminho diferente do padrão do gênero, essa diferença deve ser tratada como uma escolha consciente a validar, não como erro automático.

---

## Fontes principais

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
