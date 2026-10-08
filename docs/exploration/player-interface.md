# IndexCities — Interface e ferramentas do jogador

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o conteúdo deste documento ainda não foi revisado integralmente pelo responsável e não pode ser tratado como decisão.
>

> **Status:** exploração temática ativa — não é fonte de verdade.
>
> A autoridade de produto continua sendo `docs/SPEC.md`. Este documento concentra pesquisa, alternativas, referências e hipóteses sobre interface e ferramentas do jogador. Quando uma decisão fecha, o resultado oficial deve ser promovido para `docs/SPEC.md`; este arquivo preserva o raciocínio e o material ainda em formação.

## Objetivo

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


A interface do IndexCities deve deixar o jogador **construir, entender e corrigir a cidade com pouco atrito**, sem esconder a profundidade da simulação.

O jogo tem uma dificuldade especial: uma ação aparentemente simples — colocar uma rua ou um prédio — pode envolver dinheiro, materiais, trabalhadores, logística, terreno, acesso, demanda e consequências futuras. A UI precisa mostrar o suficiente para a decisão ser compreensível, mas sem virar uma tela cheia de números.

Princípios usados neste documento:

- primeiro mostrar a ação e a consequência mais importante;
- detalhe adicional aparece sob demanda;
- preview antes de compromisso;
- ferramentas frequentes devem exigir poucos cliques;
- o jogador deve conseguir corrigir planejamento sem ser punido por acidentes de interface;
- mudanças em coisas já construídas devem respeitar a simulação;
- não esconder erro apenas com cor: sempre que algo estiver bloqueado, deve existir um motivo em texto curto;
- profundidade da simulação não deve virar burocracia de UI.

---

## Decisão já oficial: rua em L com um único gesto

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


A ferramenta de ruas terá suporte a um trajeto ortogonal em **L** usando um único gesto.

Fluxo esperado:

1. o jogador seleciona a ferramenta de rua;
2. clica no ponto inicial;
3. arrasta até o ponto final;
4. se os dois pontos diferirem nos dois eixos, aparece uma prévia em L;
5. a ferramenta escolhe uma orientação inicial para a curva;
6. o jogador pode inverter facilmente o lado da curva;
7. soltar/confimar transforma toda a prévia em um projeto de obra.

Se início e fim estiverem alinhados, a ferramenta pode produzir uma reta. A intenção não é forçar um L quando ele não faz sentido; é permitir construir a forma comum "vai para frente e depois vira" sem obrigar duas operações separadas.

### Como escolher entre os dois L possíveis

Ainda é detalhe de interação, mas a solução preferida para protótipo é combinar:

- **inferência pelo movimento do mouse:** a ferramenta tenta entender qual perna o jogador parece estar priorizando;
- **tecla simples para inverter:** por exemplo `Tab`, alternando entre "horizontal primeiro" e "vertical primeiro";
- indicação visual clara do ponto da curva.

Não depender apenas de um menu ou botão na tela para essa troca. É uma operação que precisa continuar fluida durante o arraste.

### Preview da rua

Antes da confirmação, a rua deve mostrar:

- caminho completo;
- ponto da curva;
- comprimento;
- custo monetário;
- materiais necessários;
- trechos que exigem alteração de terreno;
- obstáculos ou demolições;
- ligação ou não com ruas existentes;
- motivo quando algum trecho for inválido.

No futuro, se inclinação tiver consequência viária relevante, a prévia também pode mostrar declive excessivo ou custo adicional.

### Snapping

Snapping deve ajudar, não dominar.

Candidatos a snap:

- centro ou borda de rua existente;
- cruzamentos;
- alinhamento ortogonal;
- pontos de conexão de prédios;
- grid estrutural do mapa, quando aplicável.

Precisamos evitar o problema relatado em ferramentas excessivamente agressivas: muitos nós de snap tornam difícil posicionar exatamente onde o jogador quer. A UI deve oferecer uma forma temporária e rápida de reduzir/desativar snapping durante a colocação.

---

## Pesquisa: o que outros jogos e ferramentas ensinam

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


### Cities: Skylines II

A ferramenta oficial de ruas combina:

- modos diferentes de desenho;
- snapping configurável;
- guias;
- preview;
- modo paralelo;
- ferramenta de substituição;
- cancelamento por etapa.

A maior lição não é copiar os modos. É permitir que uma ferramenta complexa continue previsível porque o jogador vê o resultado **antes de confirmar**.

Fonte:
https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools

### OpenTTD

OpenTTD usa ferramentas de arrastar para infraestrutura e uma ferramenta automática que deduz a orientação do trecho a partir do movimento do cursor.

Pontos úteis:

- construção e remoção usam gestos parecidos;
- atalhos reduzem troca constante de menus;
- a prévia da infraestrutura aparece antes da colocação;
- uma ferramenta automática pode substituir vários botões direcionais.

Fontes:
https://wiki.openttd.org/en/Manual/Building%20roads
https://wiki.openttd.org/en/Archive/Manual/Autorail

### Workers & Resources: Soviet Republic

É uma boa referência tanto positiva quanto negativa.

Pontos positivos:

- grande quantidade de overlays e informações;
- hotbar configurável;
- ferramentas de medição e construção;
- a simulação profunda exige feedback detalhado.

Problemas recorrentes relatados por jogadores:

- excesso de pequenos controles;
- muitas operações dependentes de atalhos pouco descobertos;
- ações simples podem exigir muitos cliques;
- overlays úteis nem sempre são fáceis de alcançar.

A lição para o IndexCities é não confundir **quantidade de ferramentas** com **boa interface**.

Fontes:
https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/General
https://steamcommunity.com/app/784150/discussions/0/6895657465779638292/

### Against the Storm

É uma referência interessante para contexto durante construção.

Ao posicionar certos prédios, o jogo mostra apenas a informação relevante para aquela ação:

- alcance;
- recursos próximos;
- solo adequado;
- relações com outros prédios.

Também diferencia edifícios que podem ser movidos gratuitamente, movidos com custo ou não movidos.

Fonte:
https://wiki.hoodedhorse.com/Against_the_Storm/Buildings

### Manor Lords

O desenho livre de estradas cria cidades orgânicas, mas discussões de jogadores mostram um problema importante: quando curva e snapping ficam difíceis de prever, a liberdade vira frustração.

Isso reforça a ideia de o IndexCities começar com uma ferramenta assistida simples em vez de tentar entregar imediatamente um editor de spline muito livre.

Referências:
https://www.reddit.com/r/ManorLords/comments/1plw0ul/road_placement/
https://www.reddit.com/r/ManorLords/comments/1rb3jic/reclamation_starter_map_road_shape_tools/

### Projetos open source

O projeto Burgage usa preview fantasma, validação de colisão, snap magnético e estradas por spline. É uma referência útil de implementação e interação, sem ser requisito para nossa arquitetura.

Fonte:
https://github.com/aleksandrbelov/Burgage

---

## Modelo proposto para a GUI principal

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status: proposta para protótipo, ainda não decisão oficial completa.**

A interface deve manter o centro da tela o mais livre possível. O mapa é o principal instrumento do jogo.

### Barra superior: estado da cidade

Informação persistente, pequena e de consulta rápida:

- dinheiro da cidade;
- população;
- velocidade/pausa;
- data/tempo da simulação;
- alertas importantes;
- eventualmente materiais críticos em falta, se isso provar ser necessário.

Evitar colocar uma dúzia de indicadores fixos apenas porque existem na simulação. Se um número não muda uma decisão frequente, ele pode ficar em painel ou overlay.

### Barra inferior: ações do jogador

A zona principal de construção e ferramentas.

Categorias iniciais candidatas:

- ruas e mobilidade;
- residências;
- comércio/empresas;
- indústria e produção;
- serviços públicos;
- infraestrutura;
- obras e logística;
- demolição;
- ferramentas de edição/planejamento.

A barra inferior funciona bem para construção porque fica previsível e deixa as laterais disponíveis para inspeção e informação contextual.

### Painel lateral ao selecionar algo

Selecionar cidadão, prédio, empresa, obra ou trecho de infraestrutura abre um painel contextual.

Primeira camada:

- o que é;
- estado atual;
- principal problema, se houver;
- ações relevantes.

Camadas seguintes:

- detalhes;
- histórico;
- fluxos econômicos;
- causas;
- conexões;
- dados de debug quando aplicável.

Isso segue a decisão da SPEC de leitura em camadas: informação rápida primeiro, cadeia causal quando o jogador quiser investigar.

### Canto de notificações

Notificação deve significar "isso merece atenção", não ser um feed de tudo que aconteceu.

Categorias:

- bloqueio de obra;
- serviço crítico parado;
- empresa em risco;
- falta severa de material;
- infraestrutura desconectada;
- situação que exige ação do jogador.

Eventos normais e repetitivos devem ser agregados ou ficar em histórico.

---

## Ferramentas essenciais candidatas

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status: exploração.**

### 1. Seleção / inspeção

Ferramenta padrão.

Clique em:

- cidadão;
- veículo;
- prédio;
- obra;
- rua;
- empresa;
- serviço.

A seleção deve funcionar como porta de entrada para entender a simulação, não apenas para editar objetos.

### 2. Construção de prédio

Fluxo preferido:

- escolher prédio;
- preview fantasma;
- girar;
- possivelmente espelhar quando fizer sentido;
- snapping opcional;
- mostrar acesso viário e restrições;
- mostrar custo e materiais;
- confirmar projeto;
- obra começa conforme as regras da simulação.

O prédio não aparece magicamente pronto.

### 3. Pipeta / copiar

Selecionar uma construção existente e entrar diretamente no modo de colocar outra do mesmo tipo.

Ferramenta de alto valor porque elimina navegação repetida pelo catálogo.

Não significa copiar estado, funcionários ou empresa; apenas escolher o mesmo tipo de construção.

### 4. Rua

Primeiro modo: L assistido, descrito acima.

Extensões que podem ser testadas depois:

- reta explícita;
- sequência de múltiplos segmentos antes de confirmar;
- curva suave;
- rua paralela;
- substituir/modernizar tipo de rua;
- largura ou variantes;
- ponte/túnel.

Não colocar todos esses modos na primeira versão apenas porque outros jogos possuem.

### 5. Demolição

Precisa ser rápida, mas segura.

Diferença importante:

- cancelar projeto ainda não iniciado;
- cancelar obra em andamento;
- demolir estrutura concluída.

Essas três ações podem ter consequências econômicas diferentes.

Para estruturas habitadas, empregando cidadãos ou contendo estoque, a UI precisa explicar o impacto antes da confirmação.

### 6. Medição

Ferramenta simples para:

- distância;
- eventualmente área;
- comprimento estimado de rua.

Workers & Resources mostra que jogadores de jogos espaciais profundos valorizam bastante uma ferramenta de medição.

### 7. Overlays

Overlays devem responder perguntas, não apenas pintar o mapa.

Exemplos:

- trânsito;
- acesso a empregos;
- educação;
- saúde;
- incêndio;
- água/energia/esgoto;
- poluição;
- materiais/logística;
- valor ou custo do solo, se existir;
- demanda/vacância;
- obras.

Ao selecionar um prédio para construir, alguns overlays devem aparecer automaticamente quando forem relevantes. O jogador não deveria precisar abrir manualmente "cobertura de escola" toda vez que posicionar uma escola.

### 8. Desfazer durante planejamento

Precisamos distinguir duas coisas:

**desfazer interface/planejamento**
- mover preview;
- alterar rota ainda não confirmada;
- cancelar blueprint não iniciado.

**desfazer mundo/simulação**
- apagar uma construção já realizada;
- restaurar prédio demolido;
- devolver materiais e tempo.

A primeira categoria deveria ser fácil. A segunda não deve virar uma máquina do tempo invisível.

---

## Cancelar, mover e realocar construções

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status: exploração aberta. Não é requisito oficial ainda.**

A prioridade aqui é gameplay. O sistema de obras é profundo, mas corrigir um erro de planejamento não pode exigir que o jogador espere uma longa operação logística ou seja punido por detalhes de materiais parcialmente consumidos que não criam uma decisão interessante.

A fronteira mais promissora é simples:

> **antes de a construção estar concluída, ela continua sendo recuperável; depois de concluída, passa a ser um ativo físico de verdade.**

Isso evita microgerenciamento sem transformar prédios prontos em objetos sem consequência.

### Caso A — projeto colocado, obra ainda não concluída

Hipótese preferida para teste:

- o jogador pode cancelar ou reposicionar o projeto;
- todos os materiais comprometidos com aquela obra retornam ao estoque elegível da cidade;
- não precisamos rastrear para gameplay quanto concreto já virou fundação ou quanta madeira já foi aplicada;
- conceitualmente, o canteiro recupera/reaproveita os materiais;
- o jogo pode fazer essa devolução de forma imediata ou praticamente imediata na interface;
- o novo local volta a depender normalmente de acesso, logística, equipe e demais regras de construção.

Isso inclui tanto uma obra que ainda espera materiais quanto uma obra visualmente já iniciada.

A simplificação é deliberada: rastrear "material usado versus recuperável" acrescentaria contabilidade e punição, mas provavelmente pouca decisão interessante.

Ainda precisa ser decidido se existe algum custo monetário/trabalho perdido ao cancelar uma obra incompleta. A preferência atual é evitar penalidade relevante enquanto isso não gerar gameplay claro.

### Por que isso continua coerente com a simulação

Não é necessário fingir que materiais brotam do nada.

A abstração pode ser:

obra incompleta
→ materiais continuam economicamente recuperáveis
→ cancelamento libera/devolve o lote comprometido
→ estoque da cidade volta a tê-los disponíveis

A animação pode mostrar retirada do canteiro quando isso for útil, mas o jogador não deve precisar esperar caminhões por muito tempo apenas para corrigir layout.

O detalhe físico existe para dar causa e consequência, não para criar burocracia.

### Caso B — prédio concluído e demolido

Aqui a fronteira muda.

Se o jogador simplesmente demolir um prédio pronto:

- o investimento da obra foi consumido;
- os materiais investidos não retornam integralmente;
- o dinheiro investido não é devolvido;
- construir novamente exige uma nova obra.

Podemos futuramente explorar sucata/reciclagem se isso criar gameplay suficiente, mas não precisamos disso para justificar a regra básica.

### Caso C — prédio concluído e jogador quer apenas mudar de lugar

Não devemos automaticamente obrigar o jogador a:

1. demolir;
2. esperar tudo desaparecer;
3. abrir o menu de construção;
4. procurar o mesmo prédio;
5. colocar novamente;
6. esperar uma segunda obra longa.

Isso é coerente fisicamente, mas pode ser péssimo de jogar.

A hipótese mais promissora é uma ação de alto nível chamada **Realocar**.

Fluxo possível:

1. selecionar o prédio;
2. escolher `Realocar`;
3. posicionar um ghost do mesmo prédio no novo local;
4. confirmar;
5. o local antigo começa a desaparecer enquanto o novo começa a aparecer;
6. veículos/equipe de obra podem fazer algumas viagens visuais entre os dois pontos;
7. após um período curto e calibrado, a entidade passa a operar no novo endereço.

Para o jogador, é uma única ação fluida.

Para a apresentação, parece uma mudança física e não um teleporte instantâneo.

Para a simulação, não precisamos obrigatoriamente reproduzir uma reconstrução completa com o mesmo tempo e a mesma cadeia logística de uma obra nova.

### A realocação pode ser uma abstração deliberada

É aceitável que a realocação seja mais rápida e simples do que construir do zero se isso gerar uma experiência melhor.

O importante é não esconder a ação:

- existe um estado "em realocação";
- o prédio antigo vai desaparecendo;
- o novo vai sendo montado;
- podem existir caminhões/equipe conectando visualmente os dois;
- moradores, empresa, serviço ou estoque não aparecem nos dois lugares ao mesmo tempo;
- durante a transição, a operação pode ficar pausada ou parcialmente indisponível.

Não precisamos transformar a realocação em uma simulação detalhada de desmontagem de cada material.

### Custo e tempo da realocação

Ainda aberto.

Três alternativas merecem avaliação no jogo integrado (sem POC independente obrigatória):

**1. custo pequeno + tempo curto**

Pró:
- preserva consequência;
- continua fluido.

Contra:
- precisamos justificar/calibrar custo.

**2. apenas tempo curto**

Pró:
- excelente qualidade de vida;
- muito simples de entender.

Contra:
- pode permitir reorganizar a cidade inteira sem consequência econômica.

**3. custo proporcional ao prédio, mas muito menor que reconstrução**

Pró:
- dificulta abuso sem tornar a ferramenta punitiva;
- comunica que mover estrutura pronta tem trabalho real.

Contra:
- acrescenta mais um parâmetro econômico.

Não escolher isso por realismo. Testar qual alternativa produz decisões sem transformar reorganização em tarefa chata.

### O que preservar durante uma realocação

Uma vantagem importante de tratar isso como `Realocar` em vez de demolir + construir é preservar a identidade da entidade.

Possíveis exemplos:

- a mesma escola continua sendo a mesma escola;
- a mesma empresa continua operando o estabelecimento;
- funcionários continuam vinculados;
- histórico do prédio/serviço não desaparece;
- configurações específicas continuam;
- estoque pode ser transferido dentro da operação.

Isso precisa ser decidido por tipo de entidade, mas provavelmente cria uma experiência muito melhor do que destruir conceitualmente tudo só porque o endereço mudou.

### Princípio provisório

A profundidade deve estar **na consequência relevante**, não na quantidade de espera ou cliques.

Portanto, a direção para protótipo é:

- **obra incompleta:** pode cancelar/mover e recuperar integralmente os materiais;
- **demolição de prédio pronto:** perde o investimento e precisa reconstruir se quiser outro;
- **realocação de prédio pronto:** ferramenta fluida e abreviada, com transição visual entre origem e destino, sem exigir uma reconstrução completa e demorada;
- detalhes de custo, duração, paralisação e transferência de ocupantes/estoque continuam em teste.


---

## Uma ideia importante: editar sem perder identidade

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Se um prédio for realocado, reconstruído ou substituído, precisamos decidir o que acontece com sua identidade.

Exemplo:

- uma escola muda de endereço;
- funcionários continuam vinculados?
- alunos continuam vinculados?
- histórico continua sendo da mesma instituição?
- estoque acompanha?
- empresa que opera o prédio continua sendo a mesma?

Isso pode ser mais importante para gameplay do que a animação da mudança.

Uma boa ferramenta de "realocação" talvez deva preservar a **entidade institucional/econômica** enquanto troca sua estrutura física.

Isso permite dizer:

> "A Escola Municipal Central está sendo transferida para o novo prédio"

em vez de destruir uma escola conceitualmente e criar outra sem relação.

Esse ponto ainda precisa de pesquisa e decisão.

---

## Ferramentas de qualidade de vida a considerar

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


### Favoritos / hotbar

Jogador deve poder colocar ferramentas ou construções frequentes em atalhos.

Isso evita navegar por categorias repetidamente.

### Último item usado

Após colocar uma construção, manter acesso rápido ao último tipo usado.

### Construção contínua

Para itens repetitivos:

- árvores;
- postes;
- pequenas estruturas;
- ruas.

Pode manter a ferramenta ativa depois de confirmar.

### Tecla modificadora para comportamento temporário

Exemplos possíveis:

- ignorar snap enquanto segura uma tecla;
- inverter o L;
- apagar usando a mesma ferramenta;
- fazer ajuste fino de rotação.

Melhor do que criar um botão permanente para cada variação.

### Transparência automática

Quando prédio/árvore esconde o ponto onde o jogador está tentando construir, elementos entre câmera e cursor podem ficar transparentes temporariamente.

### Auto-overlay contextual

Ao colocar:

- escola → mostrar alunos/cobertura relevante;
- hospital → mostrar cobertura/capacidade relevante;
- indústria → mostrar logística e acesso;
- residência → mostrar acesso, emprego e serviços;
- depósito → mostrar fluxos de materiais;
- rua → mostrar conexões, inclinação e obstáculos.

### Informação perto do cursor, sem tapar o alvo

Fóruns de Farthest Frontier relatam que janelas de placement frequentemente ficam em cima da área que o jogador está tentando posicionar.

Regra para o IndexCities:

- dados essenciais podem acompanhar o cursor;
- painel deve escolher automaticamente o lado livre;
- informação detalhada fica fora da área imediata de placement;
- nunca esconder o ponto exato que está sendo editado.

Referência:
https://forums.crateentertainment.com/t/reduce-clicks-move-the/128750

---

## Ideias fora do padrão para testar

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


### 1. "Planejar bairro"

O jogador desenha vários prédios e ruas como blueprints antes de liberar a obra.

Benefícios:

- permite revisar layout inteiro;
- orçamento agregado;
- materiais totais;
- prioridade de execução;
- menos necessidade de demolir erro de posicionamento.

Risco:
- não virar um modo complexo de desenho arquitetônico.

### 2. Comparação antes/depois

Ao realocar, demolir ou substituir algo importante, mostrar:

- situação atual;
- situação prevista;
- mudanças principais.

Exemplo:
"Esta escola deixará 230 moradores fora do alcance durante a obra."

### 3. Ferramenta "Por que aqui?"

Ao passar o mouse com um prédio selecionado, mostrar rapidamente:

- por que aquele local é bom;
- por que é ruim;
- principal gargalo.

Não um score mágico. Deve ser derivado de causas reais.

### 4. Ferramenta "Consertar conexão"

Quando um prédio está sem acesso adequado, clicar no alerta poderia abrir diretamente a ferramenta de rua já apontando para as conexões possíveis.

A UI não apenas informa o problema; leva o jogador à ferramenta que pode resolvê-lo.

### 5. Preview temporal da obra

Além de custo, uma obra poderia mostrar estimativa baseada no estado atual:

- equipe disponível;
- material presente;
- material a importar;
- distância logística.

Não precisa prometer precisão absoluta, mas ajuda a comparar decisões.

### 6. Modo de planejamento sem compromisso

Permite experimentar layout sem iniciar obras imediatamente.

Só ao clicar "aprovar plano" os projetos entram na fila real.

Pode ser muito valioso porque o IndexCities possui construção material e lenta; planejamento incorreto custa mais do que em um city painter.

### 7. Histórico visual curto de alterações

Após uma sessão grande de obras, um pequeno histórico poderia mostrar as últimas ações do jogador:

- projeto criado;
- projeto cancelado;
- prédio marcado para realocação;
- rua alterada.

Não como sistema de undo total, mas como orientação e diagnóstico.

---

## O que evitar

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


### Interface cheia de indicadores permanentes

O IndexCities terá muitos dados. Colocar todos na HUD tornará a cidade secundária.

### Ícones sem texto em ações críticas

Ícones podem acelerar uso experiente, mas tooltip e nome devem existir.

### Ferramentas escondidas apenas em atalhos

Atalho acelera. Não deve ser a única forma de descobrir uma função importante.

### Snap impossível de controlar

Ajuda automática que luta contra o jogador é pior do que não ter ajuda.

### Janela sobre a área de construção

Informação de placement não pode esconder a própria placement.

### "Mover" que teleporta toda a simulação

Prédio, estoque, família, empresa e funcionários não devem mudar de localização instantaneamente só porque a UI usa a palavra mover.

### Confirmação para toda ação pequena

Confirmação deve existir onde o custo do erro é alto. Se cada rua e blueprint exigir modal, a interface fica lenta.

---

## Cenário de validação da interface integrada (não é entrega separada)

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Aspectos da interface integrada a observar:

1. HUD mínima;
2. seleção de entidades;
3. barra inferior de construção;
4. placement de prédio com ghost + rotação + validação;
5. ferramenta de rua em L;
6. custo/material no preview;
7. cancelamento de blueprint;
8. mover blueprint ainda não iniciado;
9. demolir;
10. um overlay de trânsito;
11. um overlay de logística/material;
12. painel lateral simples de inspeção.

Esta lista é um roteiro de observação e **não reduz o escopo da SPEC**. Verificar usabilidade sem excluir automaticamente funcionalidades oficiais da entrega integrada.

### Testes observáveis

Na validação integrada, medir/observar:

- quantos cliques para construir uma rua em L;
- quantos cliques para colocar cinco casas iguais;
- se usuário entende por que placement está inválido;
- se consegue inverter o L sem procurar menu;
- se corrige um blueprint sem demolir/recriar;
- se encontra a origem de uma obra parada;
- se painéis cobrem o local de construção;
- se snapping ajuda ou luta contra o cursor;
- se alertas levam diretamente a uma ação possível.

---

## Questões ainda abertas

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


1. A ferramenta de rua em L deve usar grid rígido, alinhamento assistido ou ambos?
2. Como exatamente o jogador inverte o lado do L?
3. Teremos curvas livres no primeiro escopo ou só depois?
4. O jogo terá modo de planejamento em lote?
5. Blueprint sem obra iniciada poderá sempre ser movido gratuitamente?
6. Realocação de prédio concluído será permitida para todos os tipos ou só alguns?
7. Realocar preserva a identidade da empresa/serviço?
8. O que acontece com moradores, funcionários e estoque durante a realocação?
9. Quanto material pode ser recuperado ao cancelar/demolir?
10. Quais overlays entram na primeira implementação integrada?
11. Hotbar será fixa, configurável ou híbrida?
12. Quais ações merecem confirmação?
13. Teremos "copiar/pipeta" desde o início?
14. A posição principal da barra de construção será inferior mesmo após testes de resolução e escala?
15. Como a UI adapta a densidade de informação em telas menores?

---

## Direção atual

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


O caminho mais coerente, neste momento, é uma interface de **mapa limpo + ferramentas contextuais + preview forte**.

A experiência de construção deve ser rápida o bastante para não transformar cada ação em trabalho administrativo, mas cada confirmação deve continuar ligada à simulação real.

A rua em L é um bom exemplo do princípio: o jogador faz **um gesto simples**, enquanto o jogo resolve a geometria e apresenta as consequências.

A realocação de prédios deve seguir a mesma filosofia: **uma ação simples na interface pode disparar um processo profundo na simulação**, em vez de obrigar o jogador a executar manualmente cada etapa ou, no extremo oposto, fingir que a etapa não existe.
