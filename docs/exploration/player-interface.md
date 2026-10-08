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

**Direção aprovada na SPEC em 2026-10-08:** a construção adota uma grade lógica interna com posicionamento assistido de vias e prédios e liberdade limitada de ajuste. O grid não força todas as construções a células completas; colisão e acesso funcional real continuam obrigatórios. **Revisão humana desta atualização:** PARCIALMENTE REVISADO — a direção foi escolhida pelo responsável; a síntese operacional e os candidatos abaixo ainda dependem de validação. O encaixe da ferramenta de rua em L deve continuar compatível com a trajetória ortogonal confirmada na SPEC; curvas livres não foram aprovadas como regra geral.

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


**Status: exploração de ferramentas de movimentação/realocação ainda aberta.** **Dinheiro do cancelamento foi decidido em 2026-10-08:** valor comprometido ainda não pago é liberado no Caixa; pagamentos efetivos não são estornados. A interface deve mostrar quanto será liberado e quanto já foi gasto, sem exigir que o jogador controle pagamentos individualmente. A regra de **materiais recuperados ao cancelar obras está confirmada na SPEC**: materiais apenas reservados e ainda não entregues são liberados; materiais já entregues ao canteiro foram consumidos e não retornam. **Em 2026-10-08 o responsável reafirmou explicitamente que prefere manter a perda do material entregue**, por considerar baixa a contribuição à gameplay de rastrear e recuperar sobras. O argumento de economia de CPU é uma hipótese, não a razão técnica principal da decisão: a simplificação de estados, devolução logística, UX e implementação é mais robusta. O cenário de recuperação integral abaixo permanece como **alternativa exploratória conflitante e não aprovada**, preservada apenas para histórico, sem autoridade para reabrir a SPEC automaticamente.

**Decisão oficial adicional em 2026-10-08 — Mover obra inacabada (opção C):** o responsável aprovou reposicionamento assistido em uma ação, gratuito antes de gasto/entrega, e sujeito às perdas já realizadas após pagamentos ou chegada de materiais. A interface mostrará a prévia do novo local, perdas e novo custo; a simulação tratará o evento como cancelar a obra anterior e gerar outra, sem teletransportar material, reproduzir progresso ou estornar despesa real. Entregas em trânsito exigem solução técnica física, sem microgerenciamento. **Revisão humana desta atualização:** PARCIALMENTE REVISADO — a escolha foi confirmada, mas o detalhamento redigido pela IA e hipóteses adjacentes não foram revisados integralmente. A realocação de construção concluída continua aberta na exploração e não foi promovida à SPEC.

A prioridade aqui é gameplay. O sistema de obras é profundo, mas corrigir um erro de planejamento não pode exigir que o jogador espere uma longa operação logística ou seja punido por detalhes de materiais parcialmente consumidos que não criam uma decisão interessante.

A fronteira mais promissora é simples:

> **antes de a construção estar concluída, ela continua sendo recuperável; depois de concluída, passa a ser um ativo físico de verdade.**

Isso evita microgerenciamento sem transformar prédios prontos em objetos sem consequência.

### Caso A — projeto colocado, obra ainda não concluída

**Histórico de alternativa superada pela opção C aprovada na SPEC:** o jogador já tem a ação **Mover** para reposicionar a obra sem cancelamento manual, mas continua sujeito aos gastos efetivos e à perda de material entregue. O cenário abaixo é **não aprovado e conflitante com a SPEC quanto à devolução de materiais já entregues**:

- o jogador pode cancelar ou reposicionar o projeto;
- todos os materiais comprometidos com aquela obra retornam ao estoque elegível da cidade;
- não precisamos rastrear para gameplay quanto concreto já virou fundação ou quanta madeira já foi aplicada;
- conceitualmente, o canteiro recupera/reaproveita os materiais;
- o jogo pode fazer essa devolução de forma imediata ou praticamente imediata na interface;
- o novo local volta a depender normalmente de acesso, logística, equipe e demais regras de construção.

Isso inclui tanto uma obra que ainda espera materiais quanto uma obra visualmente já iniciada.

A simplificação é deliberada: rastrear "material usado versus recuperável" acrescentaria contabilidade e punição, mas provavelmente pouca decisão interessante.

**Essa hipótese não vale como regra atual:** a SPEC decidiu que não há multa fictícia, mas gastos reais já pagos não são devolvidos. Trabalho público já remunerado não gera cobrança duplicada; o detalhamento da liquidação é técnico.

### Coerência pretendida pela alternativa (não pela regra atual da SPEC)

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

### Realocação de prédios prontos: opção C e intervenção indenizada B aprovadas (2026-10-08)

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável escolheu a **intervenção com indenização automática (B)** e reafirmou a **realocação assistida (C) como direção de experiência a testar**, enfatizando impacto global das decisões sobre SIMs. A SPEC agora contém essas direções e as consequências obrigatórias; as alternativas, detalhes de execução e hipóteses abaixo seguem em exploração sem aprovação individual.

**Decisão oficial da SPEC:** o jogador pode intervir em **propriedade privada de SIMs e empresas** sem consentimento/negociação individual, mas a cidade **indeniza o proprietário real** com dinheiro do Caixa e mantém as consequências sobre quem mora ou trabalha ali. A realocação assistida é a direção de UX a testar, não um teletransporte: deslocamento, operação econômica e transporte físico continuam sujeitos às regras normais. A indenização não deve ser confundida com um reassentamento garantido; uma família afetada pode ficar sem moradia ou emigrar, e a empresa pode sofrer interrupção. **O valor da indenização foi fechado em 2026-10-08 (opção A): exatamente o valor de mercado do imóvel na localização original, imediatamente antes da intervenção.** A simulação paga uma única vez ao dono real, sem bônus ou desconto. **O momento do pagamento foi decidido depois (opção A, 2026-10-08): é imediato à confirmação da intervenção**, permitindo ao dono financiar a mudança antes da demolição. O prédio aguarda a saída dos moradores com status de indenização já paga, mantendo apenas ocupação e locação vigentes, sem oferecer revenda, nova ocupação ou segunda indenização. A própria demolição não pode depreciar previamente a base usada no pagamento. O método geral de avaliação imobiliária permanece parametrizável/calibrável e os detalhes de transição seguem abertos.

**Critério global confirmado:** tornar legíveis, com avisos contextuais agregados, as consequências humanas de decisões do jogador em todos os sistemas relevantes, com detalhes de SIMs acessíveis sob demanda. Evitar pop-ups por cidadão e simulação extra por quadro. O custo da simplificação deve ser medido em gameplay, implementação e processamento.

**Casos cobertos pela decisão e detalhes que ainda exigem validação:**

| Estado do edifício | Risco central | Hipótese para avaliação, ainda não aprovada |
| --- | --- | --- |
| Público/municipal | Interrupção de serviço, funcionários, materiais e custo de reconstrução | Realocação assistida é a direção de experiência escolhida; detalhar e validar continuidade operacional, sem apagar viagens/capacidade real |
| Privado concluído ainda não adquirido, desocupado | Custo de reconstrução e logística, mas sem proprietário/ocupante privado | Não há indenização a dono fictício; realocação assistida segue validação de custo e logística |
| Residencial privado com dono, ocupado ou vazio | Propriedade pertence a SIM real; valor muda com localização; se alugado, há dono e locatário distintos | **Intervenção municipal sem consentimento já aprovada:** indenizar o proprietário na confirmação; ocupantes passam por desocupação assistida quando houver; imóvel fica bloqueado para revenda/reocupação |
| Comércio/indústria em operação | Empresa é proprietária e operadora, tem caixa, trabalhadores, estoque e acesso | **Intervenção indenizada já aprovada:** pagar empresa proprietária na confirmação. Destino do estoque, manutenção da operação e funcionários ainda dependem de decisão |
| Residência sem proprietário, mas ocupada | O morador pode ter permanência temporária reconhecida na SPEC | Sem proprietário real não existe indenização fictícia; moradores seguem desocupação assistida e procura de moradia, sem teletransporte |

**Conflito principal resolvido na SPEC:** a demolição instantânea de um prédio privado **também** exige indenização ao proprietário e dispara consequências para ocupantes e empresa; a ferramenta de demolição não é um atalho para evitar custos. A SPEC preserva busca automática de residência compatível, possibilidade de ficar sem moradia/emigrar e ausência de moradia gratuita garantida. A base da indenização **já foi decidida: 100% do valor de mercado antes da intervenção**, sem bônus ou desconto. **Desocupação residencial assistida foi aprovada em 2026-10-08 (opção C):** uma ordem de intervenção em casa ocupada inicia uma busca automática de outra moradia durante período curto; após saída antecipada ou expiração do prazo, a demolição física pode ocorrer. Falta de casa não bloqueia para sempre, podendo resultar em população sem moradia ou emigração. A locação segue enquanto houver imóvel ocupado e proprietário registrado; a indenização é **transferida ao proprietário na confirmação da intervenção**, não apenas na retirada. O imóvel indenizado fica indisponível para revenda, reocupação e indenização duplicada durante o período de transição. O número exato de dias fica em calibração. Ainda requerem definição ou validação os tratamentos físicos/operacionais de estoque e empresas, bem como a realocação concluída da entidade/estrutura.

**Alternativas debatidas (histórico; opção B foi aprovada, A e C não são regras vigentes):**
- **Proteção até transação:** poder mover diretamente apenas prédios públicos ou efetivamente sem proprietário e desocupados; imóvel privado demanda aquisição/saída real antes de intervenção, sem presumir venda garantida. Baixa complexidade, menor liberdade de reurbanização.
- **Intervenção automática com compensação:** prefeitura pode iniciar obra/intervenção em imóvel privado, mas deve haver titular real, transferência/indenização com dinheiro do Caixa e efeito sobre família/empresa. Mais liberdade, porém risco de deslocamento arbitrário, alta despesa e regras adicionais.
- **Consentimento/negociação automática:** proposta ao proprietário; pode rejeitar ou exigir condições e deixar a obra travada. Mais agência, mas risco de fricção de gameplay e rotina onerosa de simulação.

**Critério de avaliação:** a propriedade é de uma entidade econômica, não do jogador; mover localização pode alterar valor de mercado e acesso a empregos/serviços. Transferências monetárias precisam de origem/destino reais e preservar a oferta fixa. A decisão do morador de mudar não equivale a autorizar a transferência de sua propriedade; proprietários que alugam precisam ser tratados separadamente dos inquilinos. Não criar menus de negociação por cada SIM nem cálculos contínuos para todo o mapa: se necessário, avaliar apenas quando uma intervenção for solicitada.

**Referências para contraste, não requisitos:** [Farthest Frontier — guia oficial](https://www.farthestfrontier.com/guide/gameplay/buildings/) permite realocar edificações com custo de trabalho, mas não representa necessariamente propriedade privada por SIM como no IndexCities; [Cities: Skylines II — Paradox, Signature Buildings](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings) diferencia edificações colocáveis realocáveis; [Workers & Resources — discussão de moradores e demolição](https://steamcommunity.com/app/784150/discussions/0/5568165891211111202/) mostra risco de deslocamento e controles trabalhosos, evidência comunitária não formal.

**Atualização aprovada em 2026-10-08 — preferência de primeira oferta na casa substituta (revisão humana: PARCIALMENTE REVISADO):** o responsável **alterou a regra anterior de mercado aberto sem preferência**. A residência substituta continua sendo imóvel **novo**, sem herdar automaticamente dono, locação ou morador. **Quando a obra estiver concluída e houver oferta de compra**, o **SIM proprietário antigo indenizado é o primeiro a receber oportunidade automática** pelo **valor de mercado do endereço novo**, usando dinheiro real; se não quiser, não for elegível ou não puder pagar, a casa entra no mercado residencial normal para compradores elegíveis locais/externos. Primeira aquisição paga à **Reserva Global**, nunca à prefeitura; não há casa gratuita, reserva indefinida ou comprador garantido. Não confundir proprietário indenizado com inquilino deslocado. Esta regra substitui a decisão residencial anterior de venda aberta sem prioridade.

**Decisão operacional aprovada em 2026-10-08 — opção C, continuidade condicionada:** ao realocar estabelecimento privado ou serviço municipal, manter atividade no prédio antigo **enquanto estrutura, acesso e necessidade espacial da obra permitirem**. Se for preciso liberar o terreno antes de a estrutura nova operar, a atividade antiga para, com efeitos reais. **Não** cria espera obrigatória na ferramenta de Demolir, nem altera desocupação assistida residencial. Indenização de empresa proprietária é paga **na confirmação**, mesmo que opere temporariamente no antigo endereço, sem nova venda ou indenização. Receitas, salários devidos, estoques e capacidade são reais; nada se duplica ou teletransporta. O procedimento deve ser de **uma ação de gameplay, com status agregado**, e avaliações de incompatibilidade/etapas por eventos pertinentes, não por simulação contínua de cenários.

**Autonomia empresarial confirmada pelo responsável em 2026-10-08 — registrada na SPEC:** ao ordenar Realocar uma fábrica ou comércio, o jogador escolhe **onde o prédio substituto será construído**, mas a **empresa proprietária antiga decide autonomamente se quer operar naquele novo endereço**; cada SIM funcionário decide independentemente se permanecerá em sua vaga quando o local de trabalho mudar. Não apresentar Realocar como garantia de que empresa e equipe seguirão a estrutura, nem encerrar automaticamente toda a empresa porque perdeu um imóvel. Empresa pode avaliar condições econômicas e ofertas reais sem novo painel de microgerenciamento. **A primeira oportunidade de compra pelo antigo proprietário está aprovada (SIM ou empresa)**; querer comprar não garante titularidade, dispensa de pagamento nem operação efetiva. A empresa decide se quer aceitar a oferta.

**Próximos pontos ainda abertos:** as etapas físicas/operacionais detalhadas de realocação de prédio concluído (sobretudo empresas, funcionários, estoque e destino de moradores), **continuidade econômica e operacional depois da retirada**, salários/vagas e logística durante paralisações, além de detalhes não cobertos pela desocupação residencial. **A agência da empresa para avaliar o novo endereço já está decidida, não é lacuna.** **O destino econômico da casa substituta foi decidido** e não é mais uma dúvida de propriedade. **A direção do prazo curto de desocupação já está na SPEC**, restando calibrar os dias exatos. **Não reabrir o valor da indenização**, que foi aprovado exatamente ao valor de mercado do imóvel no estado anterior à intervenção. Não transformar esses detalhes em regras arbitrárias por conta de uma preferência geral. O direito de intervenção indenizada, a UX assistida como direção e a exposição dos impactos humanos estão decididos na SPEC. A experiência de obras **inacabadas** permanece aprovada e independente.

#### Primeira oferta preferencial aprovada e detalhes de transição ainda abertos (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **aprovou em 2026-10-08 prioridade de primeira oferta ao proprietário original indenizado, SIM ou empresa**, com compra ao valor de mercado novo e entrada no mercado geral caso haja recusa ou incapacidade. A opção A de reavaliação global do emprego e a opção C de continuidade operacional condicionada permanecem aprovadas. Autonomia decisória da empresa também está na SPEC. **Ainda abertos** o tratamento financeiro/trabalhista específico durante paralisações prolongadas, o destino de estoque sem local apto e as etapas operacionais detalhadas; **a identidade econômica/institucional e a preferência de compra já estão decididas**.

**Modelo agora aprovado na SPEC — primeira oferta prioritária, não posse ou preço garantido:** ao ser concluído o **novo** imóvel privado, antes de ofertá-lo aos demais, o antigo proprietário indenizado (SIM ou empresa economicamente existente e elegível) recebe **uma única oportunidade automática de compra** pelo **valor de mercado da nova localização**, com dinheiro real e avaliação usual de viabilidade. Se aceitar e puder pagar, primeira aquisição vai à Reserva Global conforme SPEC. Se não puder/não quiser ou não for elegível, o imóvel entra no mercado geral, sem reserva indefinida, subsídio, negociações nem consultas contínuas. **Revisão humana: PARCIALMENTE REVISADO — prioridade e preço de mercado novo são decisões aprovadas; detalhes técnicos continuam para calibração.**

**Hipótese alternativa mais favorável, porém economicamente delicada:** oferecer a casa nova ao antigo preço de indenização, ainda que o novo endereço valha mais. Isso cria desconto/subsídio implícito e contraria a regra vigente de primeira aquisição pelo valor de mercado. O ganho de patrimônio não cria moeda, mas transfere vantagem econômica sem origem/pagador explicitado; avaliar criticamente antes de propor como regra.

**Riscos concretos que não podem ser omitidos:**
- Imóvel antigo indenizado é retirado após desocupação assistida curta; obra nova precisa de materiais, entregas e trabalhadores reais e pode demorar mais. **Prioridade na conclusão não garante moradia durante a transição** nem permanência na cidade. Conservar a busca normal do morador por outras casas. Contrato/reserva antecipada ou encadeamento físico das obras seriam decisões **adicionais**, não implícitas.
- Empresa privada tem caixa e identidade próprios, mas a falência **terminal** já foi definida na SPEC. Preservar a empresa sem sede até a obra nova exige estado/limites operacionais coerentes e consequências para salários, pedidos, estoque e funcionários; não inventar sobrevivência eterna ou teletransporte. Prioridade de compra não é garantia de continuidade empresarial.
- Escola, hospital e demais serviços municipais **não são vendidos a empresa privada**: a continuidade da mesma instituição durante a mudança já foi aprovada; capacidade real, recursos, vagas e atendimento continuam condicionados ao funcionamento físico. **Decisão A aprovada para trabalhadores públicos e privados:** se empregador e vaga ainda existirem, o SIM tem reavaliação automática ao mudar de endereço de trabalho e pode sair, abrindo vaga apenas quando a vaga permanecer real. Não presumir demissão coletiva nem retenção obrigatória.
- Processar preferência de compra **apenas no evento da oferta** e revisar contratos/viagens dos trabalhadores afetados **quando a localização mudar** ou em eventos de emprego já existentes. Não impor varreduras permanentes de todos os SIMs nem microgerenciamento ao jogador.
- **Semelhança da UI, semânticas econômicas diferentes:** ação de alto nível `Realocar` pode oferecer uma experiência uniforme sem confundir nova residência privada colocada à venda, empresa que precisa adquirir sede e serviço municipal que mantém comando público.

**Questões ainda abertas:** (1) como lidar com estoque físico caso o prédio antigo precise ser retirado antes de existir destino apto, sem criar depósitos e etapas burocráticas por antecipação; (2) o efeito sobre vagas e salários durante paralisações prolongadas, à luz das regras de emprego e falência já existentes; (3) como coordenar a logística de transferência com o mínimo de interrupção. **Já decidido:** a primeira oferta ao antigo dono (SIM/empresa), a autonomia empresarial, a continuidade da identidade econômica/institucional quando cabível, a operação antiga enquanto possível (C) e a escolha dos funcionários (A).

#### Nova reflexão de 2026-10-08 — decisão de trabalhadores e visibilidade da avaliação imobiliária

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **aprovou a opção B de visualização imobiliária e a opção A de reavaliação automática do emprego de trabalhadores públicos e privados** em 2026-10-08; ambas constam na SPEC. **A preferência de primeira oferta já foi aprovada para SIM e empresa**; a **continuidade da identidade econômica/institucional após a retirada** já foi decidida na SPEC (opção C, condicionada à aquisição pela empresa original no caso privado); manter o prédio antigo operando quando possível também está aprovado. Análises e parâmetros da IA não foram revisados integralmente.

**Decisão aprovada na SPEC — reavaliação global do emprego (opção A):** quando o local **efetivo** de trabalho muda, funcionários de escola, hospital, comércio, indústria ou outros empregadores, **públicos ou privados**, podem avaliar automaticamente se querem continuar em seu vínculo existente, usando trajeto real, acessibilidade, salário, turno e outras oportunidades reais. Nem demissão coletiva automática nem retenção obrigatória; só podem permanecer se empregador e vaga sobreviverem à transição. Quem optar por sair deixa uma vaga **se essa vaga ainda existir**, sem emprego ou salário fictício. Processamento acionado pela mudança do endereço e pelos mecanismos normais de emprego, não por verificação global contínua. **Decidido separadamente pela opção C:** manter a operação no endereço antigo enquanto viável e interrompê-la caso a intervenção imponha a retirada antes de a substituição funcionar. **A continuidade da identidade da empresa original que compra o destino e da instituição municipal já foi aprovada separadamente (opção C)**; continuam em aberto salários/vagas durante paralisação prolongada, destino excepcional do estoque e sequência detalhada da mudança; a opção A não aprova esses detalhes.

**Valoração da casa ao mover — exemplos, não tabela de ganhos:** mesmo tipo de estrutura em outro bairro pode ter preço diferente por acesso, poluição, serviços, demanda e condições locais, conforme avaliação imobiliária dinâmica já aprovada. Indenização da casa original a R$ 100 mil e nova compra a R$ 80 mil deixam R$ 20 mil **em dinheiro** ao SIM, mas a nova propriedade vale R$ 20 mil menos: não se criaram R$ 20 mil de patrimônio líquido. O inverso (nova casa a R$ 130 mil) exige R$ 30 mil de dinheiro adicional. Uma compra pode ser recusada por condições sociais/econômicas além do preço; não pressupor ganho garantido.

**Decisão de interface aprovada na SPEC — opção B, prévia contextual em camadas (2026-10-08):** para realocação de imóvel privado, mostrar **indicação discreta de valorização/desvalorização prevista** durante o posicionamento, apoiada nos motivos concretos da avaliação. Na inspeção sob demanda e ao confirmar a intervenção, mostrar **valor atual do imóvel antigo, avaliação estimada do imóvel na nova localização, diferença estimada e causas principais**, claramente diferenciados da **indenização já calculada e de custos reais**. Não obrigar o jogador a olhar números continuamente nem esconder informação monetária importante. O preço efetivo de compra futura continua sendo o valor de mercado do **momento da transação**, sem preço garantido na prévia. Evitar avaliação completa a cada frame: usar o mesmo modelo imobiliário já definido e atualizar por interação/mudança relevante, sem engine paralela. **A opção B foi aprovada pelo responsável; apenas fórmula, periodicidade exata, design visual e critérios de atualização ficam para calibração/implementação técnica compatível.**

**Alternativas rejeitadas nesta decisão de UI (histórico, não requisitos):**
- **Valor numérico sempre no arrasto:** deixaria cada passo do posicionamento dominado por números, aumentaria o risco de min-max de preço e de confundir estimativa com promessa.
- **Valor oculto até a conclusão:** enfraqueceria a previsão de custos/impactos e a leitura causal antes da intervenção.

**Alerta sobre gameplay e coerência:** mostrar custos e consequências humanas relevantes antes de confirmação não obriga o jogo a transformar todo arrasto em uma calculadora especulativa. A cidade paga indenização e obra com recursos reais; a valorização do imóvel novo é uma **estimativa de ativo futuro**, não receita automática para a prefeitura ou o antigo dono. Desincentivar min-max artificial com causalidade, custos reais e incerteza de mercado, não escondendo informação essencial. A prioridade da primeira oferta e o preço ao valor de mercado vigente no endereço novo já estão aprovados; o instante efetivo da oferta é a conclusão/possibilidade de aquisição do imóvel, sem reserva perpétua ou promessa de residência intermediária.

#### Discussão de ritmo de realocação — risco de microgerenciamento interno (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Em 2026-10-08, o responsável **confirmou que a paralisação não é etapa obrigatória de Realocar** e reafirmou **gameplay/microgerenciamento como critério global obrigatório**, agora registrados na SPEC e no AGENTS.md. Exemplos, pesquisa de duração e propostas de parâmetros da IA seguem **PENDENTES**; não foi aprovada uma nova regra de salário, suspensão de contratos ou tempo fixo de transferência.

**Ponto central:** diferenciar (1) **tempo total da obra/preparação** de uma construção substituta, sujeito a materiais/entregas/equipes reais já aprovados na SPEC; (2) **tempo em que o estabelecimento antigo ainda pode operar**, já coberto pela continuidade condicionada C; e (3) **tempo efetivo de interrupção** entre o encerramento da operação no antigo endereço e uma eventual retomada viável no destino. A soma de dias/meses do calendário de simulação não equivale a tempo de espera passiva em segundos reais para o jogador, pois a obra ocorre em paralelo às outras decisões.

**Pesquisa externa contextual, não parâmetro do jogo:** em mudanças de escritórios, prestadores descrevem planejamento de várias semanas (um plano de 90 dias é exemplo), com fechamento concentrado entre sexta-feira e segunda-feira: [Mobilix (2026)](https://mobilixmoving.com/blog/office-relocation-timeline-planning) e [Layner Group (2026)](https://laynergroup.com/blog/en/office-relocation-without-stopping-work-an-8-week-plan). Já um projeto complexo de transferência de planta industrial pode durar meses, com etapas de transição e produção parcialmente sobrepostas: [InterimProjects (2026)](https://www.interimprojects.eu/practice-areas/plant-closure-relocation/). **Esses prazos são exemplos comerciais, não dados universais nem alvos de balanceamento**; distinguem preparação prolongada de interrupção produtiva efetiva.

**Direção confirmada na SPEC — realocação fluida e paralisação excepcional, não obrigatória:** preparar a obra nova e a logística necessária **enquanto o prédio antigo funciona**, sempre que fisicamente possível; minimizar a interrupção da operação, **sem pausa artificial apenas para dramatizar uma mudança**. Quando a área original precisar ser liberada antecipadamente, a opção C já admite interrupção real que pode ser maior, inclusive por falta de materiais, acesso, operador ou dinheiro. Não garantir tempo máximo por teletransporte de estoque, empregados, equipamentos, serviços ou edifícios; se a transição rápida requer transferências físicas, considerar se a infraestrutura e a sequência viabilizam essas transferências, sem camada burocrática nova.

**Critério global confirmado pelo responsável:** toda regra deve ser avaliada quanto à gameplay, cliques, espera, microgerenciamento interno, performance e valor causal; não criar complexidade por antecipação. **Risco concreto neste fluxo:** se o downtime normal for breve, não faz sentido introduzir automaticamente um novo módulo de contratos, manutenção parcial de folha, reservas de vagas, decisões trabalhistas repetidas e várias exceções por empresa. Salários efetivamente devidos, caixa real, falência e vagas existentes continuam regras da SPEC; **salários/vínculos durante paralisações significativas seguem abertos**. Só decidir complexidade adicional se um cenário real de obra bloqueada justificar consequências observáveis que as regras atuais não cubram. A opção A global de reavaliação do emprego no endereço novo continua aprovada.

**Tempo em tela e teste:** a SPEC exige obras relativamente rápidas, mas **não fixou segundos/minutos**. A [exploração de construção](construction-materials-logistics.md#ritmo-de-construção-e-espera--pesquisa-exploratória-2026-10-08) contém faixas de segundos/minutos **propostas pela IA e não aprovadas**; não confundir duração completa de obra com downtime. Avaliar com cronômetro do jogador e ciclos simulados distintos: tempo total, interrupção efetiva, cliques, risco de parada longa, conflitos espaciais, destino de estoque, porcentagem de empresas que reabrem e impacto econômico por tipo. Evitar novos números definitivos sem observação do gameplay.

**Tensão documental a resolver na implementação, sem alterar a decisão:** a antiga hipótese abaixo de que a realocação poderia dispensar “a mesma cadeia logística” **não pode suplantar a SPEC atual**, que já exige obra substituta com custo, materiais, trabalhadores e logística reais. A ferramenta pode compactar **cliques**, não criar transporte ou construção instantâneos.

> **Nota de atualização da exploração — 2026-10-08:** os exemplos e alternativas a seguir são **hipóteses históricas**, anteriores às regras oficiais atuais. A SPEC já exige intervenção indenizada em propriedade privada, **nova obra física com materiais, equipe, logística e custos reais**, desocupação residencial assistida, continuidade econômica/institucional condicionada (C), reavaliação do emprego na mudança efetiva (A) e prioridade de primeira oferta da casa substituta ao antigo SIM proprietário antes do mercado comum. A opção de realocar **somente por um tempo curto e sem custos**, o teletransporte da identidade/estoque e a operação automática da nova sede **não estão aprovados**. Permanecem por decidir os casos de paralisação, salários/vagas, estoques, continuidade de empresas/serviços e o ritmo percebido da transição. Esta seção serve de histórico, não de fonte para implementar comportamento.

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

**Hipótese histórica anterior à SPEC atual (não aprovada):** cogitou-se não reproduzir uma reconstrução completa com o mesmo tempo e cadeia logística de uma obra nova. **Isso não autoriza dispensar os custos e fluxos físicos reais definidos posteriormente na SPEC.** Uma experiência de interface abreviada deve continuar compatível com obra e logística reais; a duração exata está em calibração.

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

Uma vantagem **hipotética e dependente do tipo de edifício** de tratar isso como `Realocar` em vez de demolir + construir é preservar a identidade institucional ou operacional da entidade. **Não aplicar automaticamente à casa privada:** para ela, a SPEC determina nova construção oferecida no mercado residencial, sem titularidade nem moradores transferidos.

Possíveis exemplos:

- a mesma escola continua sendo a mesma escola;
- a mesma empresa continua operando o estabelecimento;
- funcionários continuam vinculados;
- histórico do prédio/serviço não desaparece;
- configurações específicas continuam;
- estoque pode ser transferido dentro da operação.

Isso precisa ser decidido por tipo de entidade, mas provavelmente cria uma experiência muito melhor do que destruir conceitualmente tudo só porque o endereço mudou.

### Resumo da alternativa exploratória (não aprovada)

A profundidade deve estar **na consequência relevante**, não na quantidade de espera ou cliques.

Portanto, a hipótese para avaliação futura — **sem alterar a SPEC atual** — era:

- **obra incompleta (alternativa rejeitada):** poderia cancelar/mover recuperando integralmente materiais, mas essa hipótese foi substituída pelo **Mover assistido aprovado na SPEC**, sem recuperação de cargas já entregues;
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


1. **Direção já decidida:** grade lógica e alinhamento assistido; ainda validar como o usuário reduz/inverte snapping sem perder precisão nem sugerir ligações viárias falsas.
2. Como exatamente o jogador inverte o lado do L?
3. Teremos curvas livres no primeiro escopo ou só depois?
4. O jogo terá modo de planejamento em lote?
5. **Decidido:** obra inacabada pode ser movida gratuitamente antes de despesas/entregas; ainda testar interação de prévia e reposicionamento, respeitando reservas, custos efetivos e transporte real.
6. Realocação de prédio concluído será permitida para todos os tipos ou só alguns?
7. Realocar preserva a identidade da empresa/serviço?
8. O que acontece com moradores, funcionários e estoque durante a realocação?
9. **Regra encerrada na SPEC:** materiais entregues ao canteiro não retornam após cancelamento; demolição concluída não recupera materiais. Não fazer nova rodada de escolha sem consequência nova e relevante. Validar apenas o tratamento de cargas pagas/em trânsito.
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
