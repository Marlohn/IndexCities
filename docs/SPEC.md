# IndexCities — SPEC

> **Este documento é a fonte de verdade do produto.**
>
> Ele descreve somente decisões já tomadas para o IndexCities. Não herda requisitos de projetos, protótipos ou conversas anteriores.
>
> **Toda implementação de comportamento do jogo deve partir de uma decisão já registrada aqui.** Isso inclui a primeira versão, features futuras, mudanças de gameplay e correções de bugs que exijam definir ou alterar uma regra. Pesquisa e alternativas ficam em [EXPLORATION.md](EXPLORATION.md) até existir decisão do responsável. O fluxo obrigatório de trabalho está em [AGENTS.md](../AGENTS.md).

## Estado atual

O IndexCities está em **fase inicial de definição**. A **primeira implementação será do jogo integrado conforme esta SPEC**, com validação principal do conjunto funcional; **não há POCs isoladas por sistema como entregas obrigatórias**. A implementação pode ser modular e incluir testes técnicos pontuais, sem reduzir o escopo oficial. Processo e testes são regidos pelo `AGENTS.md` e pela `ARCHITECTURE.md`.

Neste momento, o objetivo é deliberadamente não preencher esta SPEC com decisões prematuras. Plataforma, direção visual, escala, sistemas de gameplay, simulação, persistência, performance-alvo e demais características serão discutidos e decididos progressivamente.

## Regras da SPEC

- Só entra aqui aquilo que foi explicitamente decidido para o IndexCities.
- **Esta é uma especificação viva:** permanece como contrato de comportamento durante todo o desenvolvimento e a manutenção, não apenas antes da primeira implementação.
- Se um bug, pedido ou dificuldade de implementação revelar comportamento ausente, ambíguo ou contraditório, o responsável decide e a SPEC é corrigida **antes** do código que dependa dessa definição. Não se presumem regras por conveniência técnica.
- Um bug que apenas restaura uma regra já clara nesta SPEC não exige alterar o texto; a correção precisa segui-la. Refatorações e otimizações também devem preservar esse contrato.
- Decisão antiga de outro projeto não é requisito deste projeto.
- Uma hipótese pode ser pesquisada ou investigada em experimento técnico **isolado**, sem virar requisito nem código de produto. Para incorporar comportamento ao jogo, é necessária decisão do responsável registrada nesta SPEC antes da implementação.
- Quando uma exploração resultar em decisão de produto, esta SPEC deve ser atualizada de forma curta.
- Se uma decisão for substituída, este documento deve refletir o estado desejado atual, não manter versões antigas por histórico.

## Fora de escopo automático

Nada é considerado obrigatório apenas por ter existido em outro projeto.

Qualquer ideia anterior pode ser revisitada futuramente como material de pesquisa, mas precisa passar novamente pelo processo de exploração e decisão antes de entrar nesta SPEC.

A SPEC deve crescer **com as decisões do produto e antes do código que as implementa**, sem antecipar funcionalidades ainda hipotéticas.

## Decisões de produto confirmadas

### Conceito e apresentação

- O IndexCities será um **city builder**.
- A apresentação será **3D com câmera isométrica**.
- A construção será feita na granularidade de **casas e prédios**, sem construção por cômodos.
- No escopo atual, o jogador posiciona **cada prédio diretamente**; zoneamento automático não faz parte da proposta atual.
- **Direção de progressão — opção C aprovada (2026-10-08):** a experiência principal é um **sandbox livre, com desafios opcionais**. O jogador pode construir, administrar e desenvolver a cidade no próprio ritmo, **sem missões, objetivos, vitória ou sequência de progressão obrigatórios para acessar a experiência central**. Desafios podem oferecer propósito adicional, mas **não são condição para continuar jogando, crescer ou manter uma cidade funcional**. A escolha não autoriza automaticamente sistema de missões, recompensas, marcos, desbloqueios, rankings, metas numéricas ou eventos artificiais: **origem, formato, apresentação, consequências e eventual recompensa dos desafios permanecem em pesquisa e exigem definição explícita antes de implementação**. Favorecer desafios significativos ligados aos sistemas e decisões reais do jogo, preservando liberdade, clareza e baixo microgerenciamento.

### Tecnologia

- A engine do jogo será **Godot**.
- O desenvolvimento principal será em **C#**.

### Cidadãos e simulação

- Qualquer cidadão deve poder ser selecionado individualmente pelo jogador.
- O jogador deve poder acompanhar a vida e o estado daquele cidadão ao longo do tempo.
- Cidadãos são entidades persistentes da simulação, e não apenas elementos visuais decorativos.
- Cada cidadão possui **saldo monetário individual real**, que pode ser inspecionado e afetado por renda, consumo, impostos, aluguel, compra de patrimônio e demais fluxos econômicos definidos.
- **Cooperação financeira do domicílio:** adultos da mesma família **podem contribuir automaticamente com dinheiro real de seus próprios saldos para despesas comuns**, sem comandos individuais do jogador, carteira familiar fictícia ou criação de moeda. A renda e as despesas familiares podem ser exibidas como agregados derivados, mas a titularidade dos saldos e os pagamentos efetivos continuam rastreáveis. No aluguel, permanece **um único SIM locatário titular e único devedor perante o proprietário**: contribuições dos outros adultos para o pagamento corrente podem chegar a esse titular por transferência real, **sem tornar os contribuintes corresponsáveis pela dívida pessoal de aluguel**, sem transferência automática de dívida atrasada e sem alterar a regra de cobrança no salário do devedor. Critérios de quem contribui e quanto contribui ficam para calibração/implementação, evitando controles financeiros por SIM.
- **Prioridade das despesas familiares em escassez:** quando os recursos monetários da família forem insuficientes, a simulação prioriza necessidades essenciais, **incluindo alimentação e moradia**, antes de lazer e consumo não essencial. O consumo não essencial é reduzido ou deixa de ocorrer primeiro; necessidades essenciais que não puderem ser atendidas **produzem consequências reais e legíveis**, sem criar produtos, serviços ou dinheiro fictício para compensar a falta. A prioridade orçamentária não elimina dívidas, não impede inadimplência de aluguel e não substitui os pagamentos entre agentes reais. A ordenação fina entre necessidades essenciais, limites e impactos concretos ficam para calibração, sem decisões manuais de cada despesa pelo jogador.
- Cada residência deve ter ocupantes/famílias reais.
- Casas representam uma residência/família; prédios residenciais podem conter múltiplas unidades e múltiplas famílias.

### Patrimônio e investimento residencial dos cidadãos

- Cidadãos podem acumular dinheiro e patrimônio ao longo da vida.
- Um cidadão/família local com recursos suficientes pode adquirir **imóveis residenciais adicionais** além da própria moradia.
- A decisão de investir em outro imóvel acontece pela simulação, sem exigir aprovação manual do jogador.
- Um imóvel adicional pode ser disponibilizado para aluguel a outra família real.
- O aluguel pago deve ir ao **proprietário real do imóvel**, e não a um recebedor abstrato.
- **Se o proprietário morrer e um herdeiro assumir o imóvel alugado, esse herdeiro também passa a ser credor dos aluguéis atrasados associados ao imóvel.** A dívida permanece e as cobranças futuras passam a beneficiar o novo credor real, sem criar moeda. **Se morrer sem herdeiro elegível, os aluguéis atrasados devidos a ele são perdoados**: não passam à Reserva Global nem ao futuro comprador. Extinguir a obrigação contábil não cria nem destrói dinheiro. O imóvel pode permanecer sem proprietário até uma aquisição futura, conforme a regra de patrimônio não reclamado.
- **Se houver família morando de aluguel em imóvel que ficou sem proprietário**, ela pode permanecer temporariamente sem pagar novos aluguéis nem acumular dívida durante **3 meses do calendário da simulação, contados a partir do momento em que o imóvel fica sem proprietário**. A família procura automaticamente outra moradia durante esse prazo, conforme a regra geral de realocação. **Se o prazo terminar e não houver nova situação de locação ou propriedade que permita sua permanência, a família deve desocupar o imóvel.** Se não encontrar outra residência na cidade, **pode optar por sair da cidade por migração**, como consequência real da falta de moradia; essa saída não é obrigatória nem garantida, e a população sem moradia continua sendo um estado possível conforme as demais regras da simulação. Não se inventa credor para cobrar aluguel sem proprietário, nem dívida ou cobrança retroativa pelo período sem dono. O imóvel permanece disponível para compra, mesmo após a saída dos ocupantes. O prazo inicial é de **3 meses**, sujeito a calibração futura; os critérios simples para desocupação, migração ou situação sem moradia seguem em definição.
- **Um imóvel sem proprietário pode ser adquirido mesmo ocupado**, por um comprador real elegível que decide automaticamente entre **morar nele** ou **mantê-lo alugado à família atual**. Se optar pela locação, os aluguéis futuros passam a ser devidos a esse novo proprietário, sem retroatividade. Se optar por morar, a família anterior dispõe de **3 meses do calendário da simulação, contados a partir da aquisição**, para encontrar outro lugar e deixar o imóvel; o novo proprietário só passa a ocupar a residência após a desocupação. Não há expulsão instantânea. **Durante esses 3 meses, enquanto a família ainda ocupar a residência, ela deve pagar aluguel ao novo proprietário real**, desde a aquisição e sem cobrança retroativa pelo período em que o imóvel estava sem dono. O valor e os reajustes seguem as regras gerais de aluguel residencial já definidas, sem impor um reajuste automático somente pela compra. A escolha é da simulação, não uma gestão manual do jogador. Mantêm-se as regras existentes de elegibilidade de compradores, inclusive para famílias que migram do exterior.
- Em cada locação residencial, há **um único SIM locatário responsável** pelo pagamento do aluguel e por eventuais dívidas de aluguel. Outros adultos que moram na mesma residência **não são automaticamente corresponsáveis** por essas obrigações. A identificação/seleção do locatário titular é feita pela simulação. **Se esse SIM morrer e houver outro adulto na família, a locação continua com os moradores, e a simulação atribui a outro adulto a responsabilidade pelos aluguéis futuros**, sem herdar a dívida pessoal do falecido, que segue sua regra própria de quitação na morte. Os critérios de escolha do novo titular, sua substituição por outros motivos e o caso sem adulto elegível permanecem em exploração.
- No primeiro modelo, **o valor do aluguel residencial é proporcional ao valor de mercado do imóvel**, usando um percentual de referência a calibrar. A procura influencia o aluguel apenas na medida em que afeta o valor de mercado: **não há ajuste adicional independente por oferta/procura** nesta versão. O aluguel é calculado pela simulação, sem negociação manual de cada imóvel.
- **O aluguel residencial é pago mensalmente, uma vez por mês do calendário da simulação**, por transferência do saldo do SIM locatário responsável ao SIM proprietário real. A regra não cria cobrança quando o imóvel estiver sem proprietário; falta de pagamento segue as regras de inadimplência. A data exata do vencimento mensal e a cobrança proporcional em início/fim de locação permanecem em calibração.
- Para uma locação em andamento, **o aluguel é reajustado anualmente, a cada 12 meses do calendário da simulação**, não a cada variação do valor de mercado. Na data do reajuste, a simulação recalcula o valor conforme o valor de mercado do imóvel e o percentual vigente, sem ajuste independente adicional por procura. **Os pagamentos continuam mensais; reajustar anualmente não significa cobrar aluguel só uma vez por ano.** Troca de proprietário, isoladamente, não antecipa o reajuste. O marco de contagem da primeira data de reajuste e ajustes de calendário permanecem em calibração.
- Se uma família não conseguir pagar aluguel, **o atraso gera dívida real com o proprietário do imóvel e o prazo de inadimplência é de 3 meses do calendário da simulação, contados a partir do primeiro aluguel mensal vencido e não quitado; ao atingir esse prazo, a família pode perder a moradia**. **Pagamentos parciais abatem o saldo devido, mas não reiniciam nem suspendem a contagem dos 3 meses enquanto restar dívida vencida. A quitação integral do débito encerra a inadimplência e zera sua contagem; um novo atraso inicia novo prazo.** Após 3 meses com dívida vencida ainda pendente, a família **pode perder a moradia**, sem despejo obrigatório e instantâneo em todos os casos. Os critérios exatos de efetivação da saída ficam para definição/calibração. **A dívida de aluguel não é perdoada automaticamente quando a família deixa o imóvel ou se muda; permanece como obrigação devida ao credor real até ocorrer uma forma de quitação ou resolução ainda a definir.** A dívida é uma obrigação rastreável, não saldo monetário criado: apenas um pagamento efetivo transfere dinheiro dos SIMs devedores ao SIM credor. O procedimento deve ocorrer automaticamente, sem aprovações manuais do jogador. Na recuperação da dívida de aluguel, foi escolhida a **cobrança gradual por desconto percentual no salário quando ele é pago**, limitada ao salário e ao saldo devido; sem salário recebido, não há desconto nesse evento. **Se o SIM devedor morrer**, a dívida de aluguel é quitada primeiro com o dinheiro disponível em seu saldo individual, até o limite devido, por transferência real ao SIM proprietário credor. **A parcela que exceder esse dinheiro é encerrada sem cobrança dos herdeiros ou familiares**, e nenhuma dívida monetária é convertida em dinheiro criado/destruído. Eventual saldo remanescente do falecido só segue depois para a herança ou, sem herdeiro elegível, para a Reserva Global. A regra não exige venda compulsória de imóveis para pagar o aluguel atrasado. A porcentagem de desconto salarial, critérios de escolha do SIM locatário titular ou de seu sucessor adulto, o caso sem outro adulto, o tratamento de múltiplas dívidas, outros meios de pagamento, encargos, os critérios de efetivação da perda da moradia após 3 meses de inadimplência, limiares da condição de falência pessoal e critérios para migração ou permanência na cidade sem moradia continuam em definição. Evitar cálculos contínuos e exceções complexas por SIM.
- **Realocação residencial após perda da moradia:** quando uma família precisa deixar o imóvel por inadimplência, término da tolerância sem proprietário ou compra para moradia do novo dono, a simulação **procura automaticamente uma residência disponível e compatível com os recursos reais da família**. Se encontrar, a família pode mudar para ela, seguindo as regras normais de aquisição/locação. **Se não encontrar**, a família pode permanecer na cidade **sem moradia** ou **emigrar**, segundo suas condições e as da cidade. A falta de imóvel compatível não garante emigração, reassentamento artificial ou moradia gratuita; não exige seleção de destino nem aprovação manual do jogador. Os critérios para escolher entre ficar sem moradia e emigrar, bem como a procura/mudança, ficam para calibração/implementação.
- A compra só pode ocorrer com dinheiro real disponível do comprador; a decisão deve considerar fatores econômicos concretos, como preço, demanda e expectativa de ocupação/renda.
- A fórmula exata de decisão e os limites de investimento continuam em calibração/exploração.
- No primeiro modelo, cada imóvel residencial **privadamente possuído** tem um único cidadão como proprietário registrado; um imóvel pode ficar temporariamente sem proprietário quando entrar em estado de patrimônio não reclamado.
- Um casal/família pode somar recursos para viabilizar a compra, mas a propriedade fica registrada em nome de um único SIM; copropriedade fica fora do primeiro modelo.
- Ao entrar na simulação, cidadãos podem iniciar com um saldo monetário explícito conforme regra de geração/migração; o valor e sua distribuição devem ser configuráveis e auditáveis, sem criação invisível de riqueza.

### Oferta monetária fixa e Reserva Global

- A simulação usa uma **oferta monetária global fixa**: o dinheiro não é criado nem destruído pelos fluxos normais; ele apenas muda de titular.
- O total monetário global é a soma do **Caixa da Cidade + carteiras dos SIMs + caixas das empresas + Reserva Global** e deve permanecer constante/auditável durante a simulação.
- A **Reserva Global** é um saldo técnico por trás das cenas, não controlado nem normalmente exibido ao jogador. Ela representa a parcela do dinheiro global que não está naquele momento com os agentes econômicos locais.
- Todo lançamento na Reserva Global deve ser rastreável por origem, destino, motivo e entidade/evento relacionado.
- Quando uma família ou empresa entra a partir do mundo exterior, seu capital inicial sai da Reserva Global; quando um fluxo econômico sai da cidade para uma contraparte externa não modelada, o valor retorna à Reserva Global.
- Quando um SIM morre sem herdeiro elegível ou uma empresa é encerrada definitivamente sem outro titular econômico definido para o saldo remanescente, esse dinheiro retorna à Reserva Global com a origem registrada. No caso do SIM devedor, eventual aluguel atrasado devido a credor real é abatido do dinheiro individual disponível **antes** de determinar o saldo remanescente.
- Patrimônio sem sucessor continua podendo permanecer explicitamente **sem proprietário**; a Reserva Global não se torna proprietária do ativo físico.
- Quando um ativo sem proprietário é vendido, o pagamento entra na Reserva Global com a origem registrada.
- A Reserva Global não substitui destinatários reais: se existe um SIM, empresa, proprietário, fornecedor ou outro recebedor econômico concreto, o dinheiro deve ir para esse agente.
- Em importações, o pagador real transfere dinheiro para a Reserva Global; em exportações, o pagamento sai da Reserva Global e vai para o vendedor/recebedor econômico real. Esses fluxos não criam nem destroem moeda.
- O valor inicial total, sua distribuição inicial e as faixas de capital dadas a novos migrantes/empresas são parâmetros de calibração e devem ser reproduzíveis pela seed.

### Empresas e economia

- Empresas serão entidades reais da simulação.
- **Autonomia econômica das entidades — princípio confirmado em 2026-10-08:** **SIMs e empresas são agentes distintos, com decisões próprias**, ainda que usem critérios econômicos e oportunidades concretas em comum. A empresa não é apenas a construção que ocupa, não age automaticamente como extensão da ordem do jogador e **decide por conta própria** se aceita oportunidades reais de aquisição e operação, se continua sua atividade em um endereço proposto ou se encerra uma operação, conforme seu caixa, demanda, custos, trabalhadores, insumos, acesso e logística. **Isso não autoriza a empresa a construir ou mover fisicamente prédios por iniciativa própria fora das ações e regras de construção decididas para o jogador.** Um SIM decide sobre suas próprias escolhas de moradia, emprego, consumo e patrimônio nas regras correspondentes; **uma decisão da empresa não substitui a decisão do SIM**, e vice-versa. Não criar uma camada adicional de IA por agente nem verificações incessantes: reutilizar as decisões normais e eventos que geram oportunidades. Autonomia não torna os agentes imunes a intervenções urbanas aprovadas, insolvência ou escassez.
- No escopo atual, quando o jogador conclui um prédio de atividade econômica privada, como fazenda, mercado, posto ou fábrica, o prédio pode permanecer **vazio/procurando operador** até que uma empresa local ou vinda da conexão exterior decida adquiri-lo e operá-lo.
- Empresas terão funcionários reais.
- Cada emprego corresponderá a uma **vaga real** dentro de uma empresa ou serviço.
- Empresas privadas terão **caixa monetário real e individual**, mantido pela simulação e auditável.
- Estoque e/ou produção devem existir como parte do modelo econômico das empresas.
- **Produção adaptativa com estoque de segurança (modelo inicial):** fazendas, indústrias e demais empresas produtoras de mercadorias devem ajustar **gradualmente e automaticamente** a quantidade produzida conforme **pedidos/vendas reais, estoque disponível e capacidade produtiva real**, mantendo uma **reserva operacional simples** para atender novas compras e evitar paradas/reinícios a cada pedido. **Estoque persistentemente alto e vendas insuficientes reduzem a produção planejada; maior saída real e necessidade de reposição podem elevá-la até o limite da capacidade e dos recursos disponíveis.** Produzir exige os insumos, trabalhadores e condições operacionais aplicáveis; não criar mercadorias, demanda, clientes ou dinheiro artificialmente. O jogador **não define metas ou ordens de produção por empresa**, mas deve enxergar oferta, vendas, estoque, utilização de capacidade e causas de ociosidade quando relevantes, aproveitando os alertas de demanda insuficiente já definidos. **Preservar o equilíbrio entre gameplay e performance:** na primeira implementação integrada, a regra de adaptação deve ser simples, previsível e de baixo custo, sem previsão sofisticada, otimização por agente a cada instante, recálculos desnecessários ou administração manual. Ritmo/periodicidade de ajuste, tamanho da reserva, tratamento de pedidos pendentes, capacidade efetiva e consequências sobre emprego/custos ficam para calibração/protótipo, sem adicionar antecipadamente subsistemas próprios. Excedentes e exportações continuam sob as regras existentes, sem saída ou comprador garantido.
- O consumo será modelado inicialmente por **categorias de produtos**, não por SKU individual.
- **Preços de mercadorias — modelo híbrido inicial:** cada recurso/categoria física relevante possui **um preço de referência compartilhado em nível de cidade**, como base econômica legível e comparável; **isso não cria estoque global nem define um preço único obrigatório para todas as transações**. Para bens de consumo vendidos em comércios, cada estabelecimento pode ter **seu próprio preço de venda por categoria**, inicialmente calculado de modo simples e previsível a partir de **custos reais de aquisição, transporte/frete e margem de comercialização**, com diferenças locais possíveis mesmo para a mesma categoria. O preço efetivamente pago é o da transação com a empresa real e movimenta dinheiro existente entre os agentes. O modelo deve explicar ao jogador preço de referência e principais motivos de diferenças locais, preservando a escolha de comércio por preço, distância e disponibilidade. **No primeiro modelo integrado, não implementar negociação complexa nem flutuação sofisticada e contínua de preços por demanda/escassez**, deixando a dinâmica futura do preço de referência, margens e ajustes de mercado em exploração; fórmula, parâmetros e periodicidade simples de atualização ficam para calibração/protótipo. **Os preços externos de importação continuam estáveis**, como já decidido. Os materiais de construção compartilham o catálogo/referência das categorias físicas, mas mantêm suas regras próprias de aquisição, estoque, custos e transporte para obras; não aplicar automaticamente regras de varejo a materiais de obra. A referência de preços é apenas informação econômica derivada/configurada, nunca um comerciante, estoque ou fonte/sumidouro de dinheiro.
- **Compras de bens de consumo pelos cidadãos são presenciais no primeiro modelo:** o SIM se desloca fisicamente até um comércio acessível, realiza a compra de **categorias de produtos efetivamente disponíveis no estoque** com dinheiro real, e segue seu deslocamento de volta ou para o próximo destino, sem teletransporte. A compra reduz o estoque do estabelecimento e transfere o pagamento ao caixa da empresa operadora; ausência de estoque ou dinheiro impede a aquisição correspondente. As viagens geram movimento real de pedestres/veículos e demanda local, inclusive fora da câmera. **Não substituir a ida ao comércio por uma transferência econômica abstrata que dispense a viagem**. **A escolha do comércio é automática e considera preço, distância/acessibilidade real e disponibilidade de estoque.** Se o estabelecimento escolhido não puder atender a compra (por exemplo, falta de produto), o SIM **pode buscar outro comércio acessível** com o produto e recursos compatíveis, com os deslocamentos correspondentes; não há estabelecimento habitual obrigatório nem reposição garantida. **Se nenhuma opção viável atender à demanda, a compra não ocorre e a necessidade continua não atendida**, com as consequências já previstas. A decisão não cria mercadorias, dinheiro nem teletransporte. A ponderação dos fatores, o limite de tentativas e o custo de busca devem ser calibrados/prototipados para evitar varreduras contínuas de toda a cidade por cada SIM. A frequência e o agrupamento de compras continuam para calibração, sem exigir compras por SKU ou uma viagem para cada item/categoria.
- **Estoque doméstico real por categoria:** bens de consumo destinados ao domicílio, começando por **Alimentos**, são comprados em quantidades para vários dias, trazidos fisicamente da loja para casa e registrados em **quantidades agregadas por categoria no estoque da família/domicílio**, sem inventário individual por SIM, unidades de SKU ou simulação de cada embalagem. As necessidades dos moradores consomem gradualmente essas quantidades ao longo do tempo; o consumo não faz novo pagamento, pois o dinheiro já foi transferido na compra real. Quando o estoque fica baixo, novas compras presenciais podem ser planejadas automaticamente, respeitando preço, disponibilidade, deslocamentos e recursos reais. **Falta de estoque nos comércios não elimina imediatamente a reserva que a família já tem em casa**; se o estoque doméstico se esgotar e não houver reposição viável, a necessidade correspondente deixa de ser atendida, com consequências reais. Volume comprado, ritmo de consumo, gatilhos de reposição, transporte dos bens durante a viagem e tratamento dos itens em mudança/perda da moradia ficam para calibração/protótipos, sem exigir microgestão do jogador.
- Compras reais reduzem estoque real, movimentam dinheiro real e geram necessidade de reposição/logística.
- Empresas poderão falir quando não conseguirem sustentar suas obrigações econômicas; o jogador deve conseguir identificar as receitas, despesas e eventos concretos que levaram à deterioração e à falência.
- Fazendas e agricultura produzem diretamente a categoria **Alimentos** no escopo inicial.
- **Reposição comercial e escolha de fornecedores — preferência local moderada:** comércios que precisam reabastecer categorias de estoque escolhem automaticamente entre produtores/fornecedores locais reais e importação externa, considerando **preço de aquisição, frete/custo total, disponibilidade e prazo de entrega**. Quando alternativas são economicamente próximas e capazes de atender ao pedido, **preferem o fornecedor local**, sem obrigação de comprar localmente independentemente do custo; se o fornecedor externo oferecer vantagem significativa ou não existir oferta local viável, a importação pode ser escolhida mesmo havendo alguma oferta na cidade. O pedido depende de dinheiro real da empresa compradora, quantidade física disponível e entrega por logística real; pagamentos locais vão ao fornecedor real e pagamentos de importação seguem a Reserva Global. O jogador não seleciona manualmente fornecedor ou pedido. **Não criar proteção local absoluta, estoque garantido nem buscas contínuas por todos os fornecedores**: gatilhos de reposição, limites de busca, ponderação de custo/prazo e limiar de diferença pequena ou vantagem significativa ficam para calibração/protótipo, com decisões diagnosticáveis. Esta política é para **abastecimento operacional de comércio**; materiais de construção para obras conservam sua política específica de disponibilidade/reserva e importação acionada por obra.
- Mercadorias físicas modeladas como estoque podem ser importadas pela conexão externa quando a oferta local for insuficiente ou inexistente, ou quando a comparação econômica da reposição comercial justificar a alternativa externa conforme a regra acima.
- No escopo inicial, os preços externos de importação permanecem estáveis.

### Aquisição e operação de comércio e indústria

- Prédios privados de comércio e indústria não recebem automaticamente uma empresa ao serem concluídos.
- O ativo concluído entra em procura por operador/adquirente; baixa oportunidade econômica pode mantê-lo vazio por mais tempo.
- Empresas locais existentes e novas empresas vindas da conexão exterior podem disputar esses ativos conforme fatores concretos como demanda/clientes, trabalhadores disponíveis, insumos, logística, localização e custos.
- No primeiro modelo, a empresa que adquire o prédio é também sua **proprietária e operadora econômica**; não haverá separação entre proprietário imobiliário e empresa operadora para comércio/indústria.
- O preço pago pela primeira aquisição vai para a **Reserva Global**, não retorna ao Caixa da Cidade.
- A procura externa não é infinita e não pode garantir venda/lucro; deve depender de condições econômicas reais e diagnosticáveis.
- O jogador não escolhe manualmente qual empresa assume o prédio nem negocia propostas individuais.
- No primeiro modelo, a empresa é uma **entidade econômica independente sem proprietário humano/SIM modelado**. A camada de sócios/acionistas fica fora do escopo inicial.

### Ciclo inicial de um prédio econômico privado

- A regra vale tanto para **comércio quanto para indústria** privados.
- O jogador financia e constrói o prédio com Caixa da Cidade + materiais físicos.
- Quando a obra termina, o prédio pode permanecer vazio em estado de **procurando empresa**.
- No início da cidade, novas empresas candidatas podem vir da conexão exterior com capital próprio explícito e finito.
- A empresa candidata avalia a oportunidade econômica do prédio; a lógica segue o mesmo princípio causal da demanda residencial, mas com fatores próprios de negócio, como clientes, concorrência, trabalhadores, insumos, logística, localização e custos.
- Se uma empresa adquirir o ativo, o pagamento vai para a Reserva Global; a empresa passa a possuir e operar o estabelecimento.
- Após a aquisição, a empresa contrata SIMs reais, compra insumos/estoque, vende/produz, paga salários e impostos e mantém seu próprio caixa.
- Empresas já presentes na cidade podem futuramente adquirir outros estabelecimentos usando o próprio caixa acumulado.
- A criação de uma empresa nova puramente local, sem origem externa nem empresa anterior, fica fora do primeiro modelo até existir uma fonte concreta de capital.

### Finanças privadas das empresas

- Cada empresa privada possui um saldo monetário real.
- O lucro permanece no próprio caixa da empresa e pode financiar operação, absorção de prejuízos e expansão futura; não existem dividendos ou retiradas para um proprietário humano no primeiro modelo.
- Receitas e despesas da empresa devem ser lançadas a partir de fluxos econômicos reais da simulação, incluindo vendas, salários, insumos, frete, impostos e outros custos definidos.
- O estado econômico da empresa e uma eventual falência devem ser derivados desses fluxos e do caixa real, não de um indicador oculto independente.
- O jogador não precisa administrar transferências bancárias, capital de giro ou pagamentos individuais manualmente.
- A leitura normal pode resumir a situação da empresa; uma visão detalhada deve permitir inspecionar caixa, receitas, despesas e causas de deterioração.
- Não haverá resgates, crédito ou dinheiro invisível para impedir falência. Se crédito, subsídio ou recapitalização forem adicionados futuramente, deverão ser sistemas explícitos e diagnosticáveis.
- A construção física de um prédio privado é financiada pelo Caixa da Cidade; após concluído, o ativo pode ser adquirido por uma empresa com capital próprio conforme as regras de aquisição.

### Falência e encerramento de empresas

- O jogo não precisa simular legislação ou processo jurídico detalhado de falência.
- Para gameplay, falência significa que uma empresa não consegue sustentar sua operação por tempo suficiente e **encerra definitivamente como empresa**; ela não permanece indefinidamente tentando recomeçar.
- A deterioração deve vir de causas reais e auditáveis, como falta de clientes/receita, custos de insumos, salários, frete, impostos ou interrupções de abastecimento.
- Antes do fechamento, a empresa deve permanecer em estado de risco por um período calibrável e mostrar ao jogador a principal causa real do problema.
- O limiar e o tempo exatos de encerramento são parâmetros de balanceamento e não exigem microgerenciamento do jogador.
- O encerramento pode ter uma etapa técnica curta de liquidação apenas para dar destino causal aos ativos e ao dinheiro; ela não é uma mecânica de gestão para o jogador.
- Enquanto essa liquidação existir, imóveis ainda pertencentes à empresa podem ser colocados automaticamente no mercado; o pagamento de uma venda entra no caixa da empresa em liquidação.
- Quando a empresa não tiver mais ativos pendentes, eventual saldo monetário remanescente sem outro titular econômico definido volta para a **Reserva Global**, e a entidade empresa é removida.
- Se um ativo ficar sem titular após o encerramento, ele pode permanecer explicitamente sem proprietário e continuar disponível no mercado; uma venda futura envia o valor à Reserva Global com a origem registrada.
- Fechar um estabelecimento isolado de uma empresa que continua saudável em outros locais **não encerra a empresa inteira**; essa distinção existe sem exigir uma simulação jurídica de falência.

### Serviços públicos

- Serviços como escola, hospital, polícia e bombeiros devem funcionar como sistemas reais da cidade, não como bônus abstratos.
- Esses serviços terão capacidade real.
- Esses serviços dependerão de funcionários reais da população simulada.

### Princípio de design: profundidade sem microgerenciamento

- O jogo deve priorizar **decisões sistêmicas e consequências observáveis**, não tarefas repetitivas de administração.
- Realismo deve ser mantido quando cria gameplay; detalhes que apenas adicionam cliques, contabilidade ou manutenção manual podem ser agregados ou automatizados.
- A interface pode apresentar informações agregadas enquanto a simulação mantém localização, transporte, capacidade, escassez e demais consequências físicas internamente.
- Esse princípio vale para todos os sistemas do produto.
- **Gameplay e microgerenciamento como critério global obrigatório (reafirmado em 2026-10-08):** toda mecânica deve justificar seu impacto sobre a experiência do jogador, considerando **cliques, etapas, esperas e decisões repetitivas**, mas também o **microgerenciamento invisível** de estados por SIM/empresa, regras especiais, exceções, buscas contínuas, memória e custo de processamento. A profundidade só vale o custo quando produz consequências, escolhas ou diagnóstico relevantes; antes de adicionar subsistemas, preferir aproveitar as regras existentes e processamento por eventos quando suficientes, **sem eliminar economia física, dinheiro, logística ou efeitos reais**. Não criar esperas artificiais nem sistemas de manutenção complexos para problemas que possam ser resolvidos pelo fluxo normal. O ritmo percebido e a necessidade de exceções devem ser avaliados com o jogo integrado.

### Princípio de design: leitura em camadas e causalidade

- A mesma simulação deve servir tanto a jogadores que preferem feedback direto quanto a jogadores que gostam de investigar estatísticas e otimizar sistemas.
- Problemas importantes devem aparecer primeiro por sinais visuais claros e consistentes, com uma causa resumida e uma ação compreensível.
- O jogador deve poder aprofundar o mesmo problema e ver dados, fatores e cadeia causal quando quiser.
- Consequências relevantes, como falência, falta de recurso, mudança de demanda ou interrupção de serviço, não podem depender de caixas-pretas impossíveis de explicar.
- Informações importantes não devem depender apenas de cor ou de um ícone sem contexto; texto curto, tooltip ou outro canal de explicação deve estar disponível.
- Simplificar a interface não significa simplificar artificialmente a simulação: o detalhe pode continuar existindo internamente desde que seja legível e diagnosticável.
- Não está decidido criar modos separados "simples" e "avançado"; o objetivo atual é obter profundidade progressiva na mesma experiência.
- **Consequências humanas das decisões do jogador — princípio global:** intervenções urbanas, impostos, serviços, empregos, transporte e outras decisões devem produzir efeitos reais sobre os SIMs e suas famílias conforme os sistemas existentes; quando esses efeitos forem relevantes, o jogo deve permitir ao jogador **perceber quem ou quantas pessoas foram afetadas, de que maneira e por quê**. Em ações de grande impacto previsível, apresentar uma prévia curta das consequências antes da confirmação, sem prometer resultados incertos.
- A leitura deve funcionar em camadas: resumo agregado e contextual primeiro, inspeção de SIMs/famílias/empresas e causas quando necessário. **Não criar confirmação ou notificação individual por cidadão**, alertas a cada pequena oscilação, ou rotinas extras de cálculo por SIM sem ganho observável de gameplay. As consequências continuam sendo estados/eventos reais da simulação, não indicadores artificiais criados apenas para exibir impacto.

### Princípio de design: concretude antes de abstração

- O IndexCities deve preferir, sempre que viável, **entidades, propriedade, dinheiro, recursos e fluxos concretos** a proxies abstratos criados apenas para simplificar a simulação.
- Uma abstração só deve permanecer quando reduzir microgerenciamento, custo técnico ou complexidade visual sem esconder a causa real do sistema.
- Estados agregados de interface podem existir, mas devem ser derivados de entidades e fluxos reais e inspecionáveis.
- Dinheiro, recursos, demanda e propriedade não devem surgir, desaparecer ou mudar de mãos sem uma origem e um destino economicamente explicáveis.
- Esse princípio não proíbe abstrações já úteis ao produto, como interiores não renderizados, visões agregadas de materiais ou redes de serviço por capacidade; ele exige que essas abstrações preservem causalidade e diagnóstico.

### Pilar de gameplay: economia material

- A expansão da cidade não deve ser resolvida apenas por dinheiro: **materiais físicos são um recurso central de gameplay**.
- Construir a cidade exige obter materiais por produção local e/ou importação.
- Produção, armazenamento, transporte e consumo desses materiais devem formar cadeias observáveis que geram atividade econômica, logística e empregos.
- Dinheiro continua relevante para pagar obras, importações, frete, operação e outros custos, mas não substitui a disponibilidade física dos materiais.

### Recursos físicos iniciais

A primeira base de recursos físicos do jogo será composta por oito categorias:

- **Alimentos** — consumo da população.
- **Combustível** — veículos, logística e serviços.
- **Suprimentos médicos** — medicamentos e consumíveis de saúde agregados em uma categoria.
- **Areia e brita** — base viária, drenagem e insumo de concreto/asfalto.
- **Concreto** — material final de construção.
- **Aço** — material estrutural agregado em uma categoria.
- **Madeira** — material de construção agregado em uma categoria.
- **Asfalto** — vias e superfícies pavimentadas.

- O nome exibido ao jogador será **Areia e brita**, evitando o termo técnico pouco intuitivo "agregados".
- Insumos industriais intermediários só precisam aparecer quando a respectiva cadeia produtiva existir. Exemplos incluem cimento, culturas/produtos agrícolas, madeira em tora, ligante asfáltico/bitume e pedra bruta.
- Água, eletricidade, esgoto e lixo permanecem sistemas de serviço/capacidade, não recursos de estoque equivalentes a essas categorias.

### Grade lógica e posicionamento assistido

- **Modelo híbrido aprovado:** o mapa usa uma **grade lógica interna** para organizar e consultar ocupação do espaço, validade de posicionamento e relações espaciais relevantes, enquanto a colocação de **prédios e vias pelo jogador é assistida por encaixes e alinhamentos**, sem impor que toda construção esteja visualmente presa a uma célula inteira ou a uma orientação única.
- O posicionamento permite **liberdade limitada e controlável** de ajuste e orientação, desde que respeite a **ocupação física efetiva**, evite sobreposição inválida e mantenha conexões e acessos coerentes com a simulação. Uma prévia visualmente alinhada não pode ser tratada como acesso viário ou conexão funcional garantidos; quando a colocação for inválida, a interface deve indicar a restrição concreta ao jogador, antes da confirmação da obra.
- O encaixe deve **ajudar, não dificultar** o controle: a ferramenta deve permitir ajustar a posição sem uma atração excessiva a pontos de snap. Formas exatas de alternar/reduzir o snapping, tamanho da grade, subdivisões para detalhes de vias, geometrias/rotações permitidas e representação interna da ocupação ficam para calibração e validação no jogo integrado, sem introduzir uma ferramenta de desenho totalmente livre como requisito.
- O modelo híbrido **não substitui** a ferramenta de rua ortogonal em L já aprovada nem autoriza automaticamente curvas livres; também não elimina restrições atuais de rodovias, níveis sobrepostos, acesso e logística. Consultas espaciais e validação devem ser proporcionais às operações relevantes, evitando verificações geométricas globais contínuas sem necessidade.

### Ferramenta de construção de ruas

- A ferramenta de ruas deve permitir construir um trajeto ortogonal em **L** num único gesto de clique e arraste quando o ponto inicial e o ponto final diferirem nos dois eixos.
- Durante o arraste, o jogo deve mostrar uma prévia completa do trajeto antes da confirmação.
- Quando houver duas formas possíveis de fazer o L, o jogador deve conseguir alternar ou influenciar de forma simples qual lado recebe a curva.
- Se início e fim estiverem alinhados, o mesmo gesto pode produzir um trecho reto; o objetivo não é obrigar curvas artificiais, mas evitar exigir que o jogador construa cada perna do L separadamente.
- A confirmação da rua deve respeitar as regras normais de obra, custo, materiais, terreno e demais restrições da simulação.

### Construção e obras

- Prédios e infraestrutura não aparecem instantaneamente prontos.
- **Ritmo de construção orientado ao gameplay:** quando materiais, acesso e equipe estão disponíveis, a execução da obra deve ser **relativamente rápida e sem espera artificial longa apenas por realismo**. A duração total continua sujeita a entregas físicas, deslocamentos, disponibilidade e capacidade real das equipes, porte da construção e gargalos logísticos; esses fatores produzem atrasos reais e explicáveis, sem teletransporte de materiais ou construção instantânea.
- Múltiplas obras podem avançar em paralelo **dentro da capacidade real disponível**, enquanto o jogador realiza outras atividades; não exigir gerenciamento manual de cada entrega ou trabalhador para fazer uma obra progredir. Se uma obra parar ou atrasar significativamente, o jogo deve mostrar a causa concreta de forma simples.
- **Tempos exatos por porte, taxas de progresso e efeito quantitativo das equipes permanecem para calibração e validação do jogo integrado**, sem fixar agora faixas de segundos/minutos nem criar um modo alternativo de construção.
- Construções passam por uma fase real de obra.
- Toda obra possui custo em **dinheiro e materiais**.
- Obras consomem materiais reais da cidade.
- Obras dependem de trabalhadores reais. No fluxo normal, as equipes do Pátio Municipal de Obras são formadas por cidadãos empregados pela prefeitura; no bootstrap inicial, uma equipe externa temporária pode executar as primeiras obras.
- Materiais de construção precisam estar fisicamente disponíveis e reservados para a obra antes de serem consumidos, seja na oferta local elegível da cidade ou já entregues no canteiro.
- No início de uma cidade, materiais podem ser importados pela conexão externa usando o dinheiro inicial.
- O custo apresentado da obra deve indicar explicitamente quando materiais faltantes serão importados e quanto essa importação aumenta o custo monetário.
- A cidade precisa possuir infraestrutura física de armazenamento, como Centros de Materiais de Construção, para materiais de construção.
- Materiais precisam chegar fisicamente ao canteiro de obras, normalmente por veículos de carga, antes de serem consumidos pela construção.
- A cidade pode desenvolver produção local de materiais por meio de indústrias/fábricas apropriadas, reduzindo dependência de importações.
- As cadeias produtivas e a ordem de introdução dos insumos intermediários continuam sendo aprofundadas na EXPLORATION.

### Necessidades e serviços urbanos

- Cidadãos precisam se alimentar regularmente e adquirir comida de forma real dentro da economia.
- Cada prédio consome água e eletricidade de forma real.
- Água e eletricidade dependem de redes/capacidades reais da cidade.
- Prédios geram lixo.
- A coleta de lixo deve acontecer fisicamente por veículos/serviços da cidade.
- Cidadãos podem adoecer individualmente e buscar atendimento real.
- Crime pode ser cometido por cidadãos individuais.
- A polícia deve reagir a ocorrências reais geradas pela simulação.

### Tempo de jogo

- O jogo terá **pause** e velocidades **1, 2 e 3**.
- A escala de tempo e seus multiplicadores devem ser configuráveis para permitir calibração durante o desenvolvimento.

### Educação, saúde, segurança e emergências

- Crianças e adolescentes têm idade escolar, matrícula real e deslocamento diário para a escola.
- Hospitais e unidades de saúde têm funcionários reais.
- A capacidade de atendimento deve depender de recursos reais da unidade, incluindo equipe disponível.
- Prisões/cadeias existem fisicamente e criminosos podem cumprir pena por um período.
- Prédios podem pegar fogo individualmente.
- Bombeiros precisam deslocar veículos e equipes fisicamente até a ocorrência.
- Mortes individuais geram consequências reais para a cidade.
- O sistema funerário/cemitério e a remoção física de corpos fazem parte da simulação.


### Trabalho, emprego e renda

- Cidadãos empregados têm horários reais de entrada e saída.
- Os horários de trabalho devem influenciar diretamente os padrões de deslocamento e o trânsito.
- Empresas e serviços podem operar em múltiplos turnos quando necessário.
- Cidadãos desempregados procuram vagas reais disponíveis.
- **Reavaliação do emprego quando o local de trabalho muda — opção A aprovada (2026-10-08):** sempre que uma mudança **efetiva** do endereço de trabalho afetar um SIM empregado, independentemente de ser **empresa privada ou serviço público**, a simulação deve oferecer a ele a **oportunidade automática e individual de avaliar se continua naquele vínculo ou se sai**. A avaliação considera **tempo e viabilidade do novo trajeto**, acessibilidade/transporte, salário, turno/horário e alternativas reais de emprego, usando o estado concreto da cidade. Não presumir que todos devam pedir demissão, que todos aceitem a mudança ou que já exista outra vaga garantida; o resultado pode diferir entre funcionários do mesmo estabelecimento.
- **Vínculos e vagas reais:** continuar é possível **somente se o empregador/instituição e a vaga correspondente ainda existirem** e houver possibilidade real de continuidade do trabalho. Se o SIM optar por sair, o vínculo termina; a vaga **permanece disponível para contratação normal apenas se ainda existir e o empregador a mantiver**. Não criar vaga, salário ou emprego fictícios para preservar a aparência da mudança. A decisão influencia deslocamentos, renda, quadro de pessoal, capacidade de serviços/produção e vagas oferecidas, com consequências observáveis. A falência/encerramento da empresa ou a extinção da vaga seguem suas regras próprias, não são anuladas por esta opção.
- **Gameplay e desempenho da reavaliação:** aproveitar critérios e rotinas normais de escolha/permanência no emprego, com reavaliação **acionada pelo evento relevante de mudança real do local de trabalho**, sem checagens contínuas adicionais para todos os SIMs, sem recálculo a cada quadro e sem solicitação de decisão individual ao jogador. A interface deve permitir ler os impactos **agregados** sobre funcionários que permaneceram/saíram, vagas e capacidade afetada quando relevantes, com causas acessíveis sob demanda. Pesos e limiares da decisão ficam para calibração, sem inventar cargos formais.
- **Limite desta regra:** a escolha do trabalhador **não determina** quando ou se o novo prédio entra em operação; não promete continuidade automática de empresas/serviços, conservação de estoque, salários durante paralisações nem teletransporte de funcionários. A **continuidade condicionada da operação no imóvel antigo durante a realocação (opção C)** está definida na seção de intervenção municipal; **permanecem em exploração** o tratamento dos vínculos/salários durante paralisações, a manutenção institucional/econômica e as etapas de transferência, sem garantia de emprego no novo endereço.
- No primeiro modelo, empregos não possuem cargos ou profissões formais como mecânica; o vínculo principal é o SIM trabalhar em determinada empresa, serviço ou estrutura.
- Uma estrutura pode exigir uma quantidade de funcionários, e vagas podem continuar tendo salário, turno e requisitos de educação ou qualificação quando necessário, sem exigir uma hierarquia de cargos.
- Não haverá gerente, chefe ou outro cargo obrigatório para uma empresa ou serviço funcionar no primeiro modelo.
- Cada vaga possui salário real.
- Salários são pagos periodicamente ao trabalhador e entram em seu saldo monetário real.


### Moradia e finanças públicas

- Famílias podem se mudar de residência conforme fatores como renda, tamanho da família e localização.
- Residências têm preço e/ou aluguel real que afetam o orçamento familiar.
- Cidadãos e empresas pagam impostos reais para a cidade.
- O sistema tributário será organizado **por categorias**, diferenciando tipos de incidência para permitir escolhas econômicas legíveis, sem exigir administração individual de cada contribuinte.
- A tributação das construções é organizada por **uso residencial, comercial e industrial**, com controles separados para **baixa e alta densidade**; essa organização serve para oferecer controles agregados ao jogador, sem cobrança manual por prédio.
- **O jogador ajusta os impostos** no nível dessas categorias. São **seis controles independentes**: residencial de baixa densidade, residencial de alta densidade, comercial de baixa densidade, comercial de alta densidade, industrial de baixa densidade e industrial de alta densidade. As escolhas tributárias devem produzir consequências econômicas e influenciar a **atratividade** dos agentes/atividades afetados, com causas observáveis, sem criar microgerenciamento individual.
- O imposto de uma construção privada só começa a ser cobrado **quando ela tem proprietário privado real**. Residências usam o saldo do SIM proprietário; estabelecimentos comerciais e industriais usam o caixa da empresa proprietária. Construção ainda sem proprietário privado não gera esse imposto, e não se cria um pagador fictício.
- O imposto de cada imóvel privado é calculado sobre seu **valor de mercado**: **imposto = valor de mercado do imóvel × alíquota definida pelo jogador para sua categoria** (residencial/comercial/industrial × baixa/alta densidade). O tamanho não é uma base de cobrança direta; pode influenciar o valor de mercado, conforme o modelo de avaliação ainda a definir.
- O **valor de mercado de cada imóvel é dinâmico** e calculado pelas condições concretas e observáveis da cidade, como localização, acesso a serviços, poluição e demanda, considerando as características do imóvel. Essa avaliação deve funcionar desde o primeiro imóvel, **sem depender de histórico de compras e vendas de imóveis semelhantes**. A valorização/desvalorização deve ter causas diagnosticáveis para o jogador e servir à avaliação do imóvel e à base de incidência tributária já definida; nenhuma alteração de valor cria ou destrói dinheiro. No primeiro modelo, **o preço de compra e venda de imóveis é exatamente seu valor de mercado calculado no momento da transação**, tanto na primeira aquisição como nas revendas, sem negociação automática de descontos ou acréscimos. A procura continua determinada pela capacidade financeira e pela decisão dos compradores; ela não garante venda.
- Valores iniciais das alíquotas, periodicidade da cobrança, fórmula exata de avaliação e frequência da atualização do valor de mercado, efeitos específicos sobre demanda/atratividade, limites de ajuste e eventual custo de manutenção de imóveis vazios ainda estão em exploração.
- A cidade possui orçamento municipal real, com receitas e despesas.
- O menu financeiro deve separar claramente entradas e saídas da prefeitura.
- Salários de todos os trabalhadores públicos devem aparecer explicitamente entre as despesas municipais.
- A cidade poderá usar empréstimos/dívida municipal para evitar travamentos financeiros e permitir recuperação de caixa.
- O principal de um empréstimo sai da **Reserva Global** e entra no Caixa da Cidade; amortizações retornam à Reserva Global. Se houver juros, eles também são transferências para a Reserva Global; taxa, prazo e limites continuam em calibração. O empréstimo redistribui dinheiro existente e não cria moeda nova.


### Caixa da cidade e investimento em construções

- O jogador administra um único **Caixa da Cidade** como recurso monetário principal controlável. Ele representa o capital disponível para desenvolver e operar a cidade no gameplay, não uma representação jurídica literal apenas do caixa da prefeitura.
- Receitas municipais, impostos e outras entradas definidas alimentam esse caixa; despesas públicas, salários e construções ordenadas pelo jogador consomem esse caixa.
- Como o jogador posiciona diretamente as construções no escopo atual, **obras públicas e privadas ordenadas pelo jogador consomem dinheiro do Caixa da Cidade e materiais físicos**. Esse custo é parte deliberada da gameplay e do diferencial do IndexCities.
- O dinheiro gasto não desaparece: pagamentos a agentes locais reais vão para esses agentes; parcelas cuja contraparte seja externa ou não modelada retornam à Reserva Global.
- **Orçamento monetário de obras:** ao confirmar uma construção, o custo monetário previsto fica **comprometido no próprio Caixa da Cidade**, deixando de estar disponível para novas despesas ou obras; esse comprometimento **não transfere nem cria dinheiro** e não utiliza a Reserva Global como depósito temporário. O jogador distingue saldo total, parcela comprometida e saldo disponível.
- **Pagamentos efetivos:** valores comprometidos só saem do Caixa quando a despesa real correspondente vencer/for realizada, indo ao recebedor econômico correto (por exemplo, fornecedor local ou Reserva Global em importação). Uma importação pode exigir pagamento **antes da entrega física**, sem que isso dispense caminhões; não adiar automaticamente todos os pagamentos até a conclusão da obra. Salários periódicos já contabilizados de funcionários públicos, inclusive do Pátio, não podem ser cobrados uma segunda vez como custo da mesma mão de obra na obra.
- **Cancelamento monetário:** ao cancelar uma obra inacabada, libera-se automaticamente o **orçamento comprometido ainda não gasto**. Pagamentos já efetivados não são revertidos por padrão, mesmo que a entrega física esteja pendente. Se nenhum gasto ocorreu, todo o valor comprometido volta a ficar disponível; não há multa fictícia de cancelamento. Nenhum cancelamento cria moeda ou retira dinheiro de um fornecedor/trabalhador que recebeu legitimamente.
- A primeira aquisição de um ativo privado recém-construído **não devolve o preço ao Caixa da Cidade**; o pagamento vai para a Reserva Global, evitando reciclagem automática de capital e preservando impostos/receitas públicas como fonte principal de recuperação financeira do jogador.
- Depois que uma empresa adquire e passa a operar o prédio, ela usa seu próprio caixa, receitas e despesas; o jogador não pode usar diretamente esse dinheiro como Caixa da Cidade nem precisa administrá-lo manualmente.
- O resultado de uma empresa privada beneficia ou prejudica a cidade por efeitos econômicos reais, como empregos, salários, impostos, produção, logística e eventual fechamento, não por transferência livre de seu caixa para o jogador.

### Financiamento de moradia

- O jogador continua decidindo diretamente onde as residências serão construídas, e a obra consome Caixa da Cidade + materiais físicos.
- Uma residência privada recém-construída pode ficar sem proprietário até a primeira aquisição. **Também vale para residência privada substituta construída por realocação assistida:** concluída a obra, não herda dono ou moradores do imóvel anterior, mas **oferece primeiro ao SIM proprietário original indenizado uma oportunidade de aquisição pelo valor de mercado novo**, quando elegível e com recursos reais; se não comprar, segue para oferta residencial normal.
- Na primeira aquisição, o comprador paga com dinheiro real; esse pagamento vai para a **Reserva Global**, não retorna ao Caixa da Cidade.
- Depois que existe um proprietário privado real, aluguel vai ao proprietário e revendas transferem dinheiro entre comprador e proprietário normalmente.
- A cidade recupera financeiramente o custo de desenvolver moradia principalmente de forma indireta, por impostos e demais receitas públicas, e não por revenda do ativo.

### Terreno e logística econômica

- Todo mapa jogável deve conter **pelo menos um rio ou um lago**.
- Estabelecimentos comerciais podem fechar quando não conseguem sustentar sua operação, incluindo falta de clientes.
- Indústrias precisam receber matéria-prima real e escoar produção real.
- Caminhões de carga circulam fisicamente entre fornecedores, indústrias, comércio e obras.
- **Entregas comerciais locais na primeira implementação integrada — responsabilidade do fornecedor:** quando um comércio compra mercadorias de uma fazenda, indústria ou outro fornecedor **local**, a **empresa fornecedora organiza e executa a entrega ao comprador**, utilizando seus veículos de carga e motoristas reais da simulação. Caminhões partem da origem física do estoque e seguem a rede viária até o ponto de carga/descarga do comprador, com carga, capacidade, distância, trânsito e tempo de entrega reais; a quantidade só entra no estoque do comércio depois da entrega. **Disponibilidade operacional de veículos e motoristas limita as entregas**, podendo causar espera e falta de produtos sem geração ou teletransporte de carga. O frete é custo econômico real da operação, coerente com a formação de preços já aprovada; como será incorporado/faturado entre fornecedor e comprador fica para calibração, sem pagamento duplicado ou recebedor fictício. A entrega é coordenada automaticamente pela simulação, sem despacho manual do jogador, nem uma transportadora independente obrigatória no primeiro modelo. **Importações continuam usando veículos e motoristas externos pela conexão com o exterior**, conforme suas regras próprias. Origem/disponibilização da frota inicial, custos operacionais específicos, escala de entregas e detalhes de alocação de motoristas ficam para prototipagem, sem novo sistema formal de cargos.


### Propriedade, demanda e vulnerabilidade social

- Lotes não precisam ter proprietário individual.
- Toda construção é colocada pelo jogador.
- A cidade deve calcular e expor **demanda por categoria de atividade**, em vez de uma única barra genérica de comércio/indústria.
- A demanda deve ser **principalmente local**, refletindo a clientela potencial acessível àquele estabelecimento e àquela área da cidade.
- Os fatores conceituais principais da demanda são população/clientes potenciais, capacidade já existente, acesso/tempo de deslocamento e concorrência entre estabelecimentos equivalentes ou substitutos.
- **Demanda é informação de gestão, não uma trava de construção:** o jogador pode construir mesmo quando a demanda é baixa.
- A interface deve alertar claramente quando uma nova atividade econômica estiver entrando em uma situação de baixa demanda ou alto risco de pouca clientela.
- **Alerta de falta de compradores durante a operação:** fazendas, indústrias e comércios devem permitir ao jogador perceber quando **vendas/pedidos reais ficam insuficientes em relação à oferta ou produção e a situação persiste com consequências relevantes**, como estoque se acumulando, receita baixa ou risco econômico. O sinal deve se apoiar em **quantidades efetivamente vendidas, produzidas e armazenadas**, inclusive vendas externas quando existirem, **não somente na contagem de compradores**: um único comprador pode adquirir grande volume. **Distinguir demanda insuficiente de falha de entrega** (há pedidos, mas caminhões ou capacidade logística impedem a saída dos produtos), além de outras causas reais, em vez de emitir alerta incorreto de poucos compradores. Mostrar diagnóstico simples, com detalhes ao inspecionar a atividade e sugestões de causas/ações urbanas cabíveis; agregar avisos para evitar excesso de notificações ou alertas por oscilações passageiras. Limiares, duração e apresentação ficam para calibração. **Este alerta diagnostica a situação econômica, mas não é uma ordem manual de produção:** o ajuste automático da atividade segue a política de **produção adaptativa com estoque de segurança** já definida em *Empresas e economia*; seus parâmetros continuam para calibração.
- Na colocação e na leitura normal, a demanda pode ser resumida em estados simples como **boa / média / baixa**; o jogador deve poder abrir os fatores detalhados que produziram esse resultado.
- Múltiplos estabelecimentos equivalentes disputando a mesma clientela, como vários mercados concentrados na mesma área, podem reduzir a oportunidade econômica para novos estabelecimentos e aumentar o risco de prejuízo.
- A fórmula exata, os pesos e os limiares continuam sendo parâmetros de calibração; não usar uma barra opaca sem decomposição causal.
- **Falência pessoal no primeiro modelo é uma condição econômica simples e derivada**, quando o SIM não dispõe de dinheiro suficiente para sustentar suas obrigações e possui dívidas. Suas consequências são percebidas no consumo que consegue financiar, nas dívidas reais e na capacidade de manter ou obter moradia, seguindo as regras já aprovadas de aluguel, inadimplência e realocação. **Não haverá processo jurídico próprio de falência, liquidação patrimonial compulsória como sistema separado, renegociação formal nem perdão automático das dívidas por entrar nesse estado.** Exceções de extinção de obrigações já aprovadas, como as regras específicas de falecimento, continuam valendo. Critérios/limiares exatos de identificação e recuperação ficam para calibração, sem criar dinheiro ou burocracia por SIM.
- **População sem moradia integra o primeiro modelo como consequência real e observável:** a ausência de residência pode afetar indicadores sociais e decisões de migração, com causas legíveis ao jogador. A resposta inicial do jogador é **indireta**, pela ampliação da oferta de moradias e pela melhoria de empregos, serviços e condições da cidade, usando os sistemas urbanos já previstos. **Não incluir no escopo inicial abrigos específicos para população sem moradia, programas municipais de assistência social ou moradia subsidiada como sistemas próprios.** Métricas e efeitos exatos serão calibrados; possíveis sistemas de assistência permanecem apenas em exploração, sem compromisso de implementação.
- Oferta e demanda devem influenciar o **valor de mercado** dos imóveis, com reflexos indiretos no aluguel residencial proporcional a esse valor, sem exigir um modelo excessivamente complexo. No primeiro modelo, não há multiplicador adicional de procura sobre o aluguel e o preço de venda é igual ao valor de mercado calculado, sem negociação de desconto ou acréscimo.


### Transporte público e veículos

- O jogo terá transporte público com operação real.
- Ônibus terão linhas, frota, capacidade e SIMs reais executando a condução durante a operação. Essa atribuição operacional não exige um sistema formal de cargos/profissões no primeiro modelo.
- Outros modais de transporte público também farão parte do sistema; os tipos exatos serão definidos progressivamente.
- Veículos particulares precisam estacionar de verdade.
- O realismo operacional dos carros deve ser alto, incluindo deslocamento e estacionamento coerentes.
- Veículos consomem combustível.
- Combustível participa de uma cadeia econômica real entre produção/refino, distribuição para postos e consumo pelos veículos.
- Postos mantêm estoque real de combustível e podem ficar sem produto.
- O mesmo princípio de estoque real vale para comércios como padarias, farmácias, mercados e outros estabelecimentos.


### Mobilidade ativa e circulação de pedestres

- Pedestres se deslocam fisicamente pela cidade.
- A posição e o deslocamento de um cidadão continuam existindo na simulação mesmo quando ele está fora da tela; deixar de desenhá-lo por distância ou câmera não pode mudar sua viagem real.
- Caminhadas fazem parte real das viagens, incluindo trechos até estacionamentos, pontos de ônibus e outros destinos.
- Bicicletas existirão como modal real de transporte e circularão fisicamente pela cidade.
- Ciclovias farão parte da infraestrutura viária.


### Vias, calçadas e estacionamento

- No escopo atual existirão dois tipos básicos de via: **via urbana comum** e **rodovia**.
- Vias urbanas comuns terão calçadas por padrão.
- O sistema viário poderá ser aprofundado futuramente com novos tipos e variações.
- Vagas de estacionamento na rua ocupam espaço físico real da via.
- Casas e prédios podem ter vagas/garagens privadas.
- Vagas privadas reduzem a necessidade de estacionamento na rua.


### Regras de circulação viária

- Pedestres atravessam vias urbanas apenas em faixas de pedestres.
- Semáforos operam inicialmente em ciclos fixos.
- Rodovias permitem velocidades maiores que vias urbanas comuns.
- Não é permitido construir casas, comércios ou outros edifícios diretamente conectados às rodovias.
- Rotatórias fazem parte do sistema viário.
- O número de faixas influencia capacidade, fluxo e congestionamento de forma real.
- O trânsito deve buscar alto realismo operacional.


### Comportamento detalhado do tráfego

- O trânsito continua existindo na simulação mesmo fora da câmera: um veículo que não está sendo desenhado ainda ocupa a via e continua causando filas, congestionamento, atraso e outras consequências.
- Quando o jogador olha para uma área, o que aparece na tela deve representar o trânsito real daquela área; uma rua congestionada não pode parecer vazia apenas para economizar processamento gráfico.
- Deixar de desenhar um veículo, ou desenhá-lo de forma mais simples, não pode fazê-lo desaparecer da simulação, teletransportá-lo nem eliminar atraso, estacionamento, consumo, bloqueio ou qualquer outra consequência real.
- Veículos escolhem a faixa adequada antes de conversões.
- Filas são mantidas por faixa e afetam o fluxo de forma independente.
- Mudanças de faixa e ultrapassagens fazem parte da simulação.
- Rotatórias, cruzamentos e acessos respeitam regras reais de preferência.
- Ônibus param fisicamente nos pontos.
- Passageiros embarcam e desembarcam fisicamente nos pontos de ônibus.
- Congestionamentos podem bloquear cruzamentos e gerar efeitos em cascata na malha viária.


### Operação física de serviços e embarque

- Veículos de emergência têm prioridade no trânsito, e os demais veículos devem ceder passagem quando aplicável.
- Veículos de serviço, incluindo coleta de lixo, entregas, manutenção e emergência, precisam parar/estacionar fisicamente para executar suas tarefas.
- Comércio e indústria possuem pontos físicos de carga e descarga.
- Veículos precisam sair fisicamente de garagens ou estacionamentos antes de entrar na via.
- Filas de pedestres em pontos de ônibus, entradas de prédios e travessias ocupam espaço físico real.


### Transporte sob demanda e operação de carga

- Táxis e transporte sob demanda existirão com um SIM real conduzindo o veículo e um passageiro real; a função de condução é uma atribuição operacional, não um cargo/profissão formal no primeiro modelo.
- Caminhões podem ter tamanhos e capacidades diferentes.
- A capacidade do caminhão influencia quantas viagens são necessárias para transportar uma carga.
- Comércio não precisa operar com janelas fixas de entrega; o tempo de chegada pode emergir do tráfego e da logística.
- A entrada e saída de pessoas dos prédios é simulada fisicamente, enquanto o interior dos edifícios permanece abstrato.
- Serviços como coleta, carga/descarga e atendimento de emergência levam algum tempo para serem executados, mas não precisam buscar fidelidade temporal extrema.


### Atividades internas, lazer e visitas

- Quando um cidadão está dentro de um prédio, sua atividade e estado continuam sendo simulados, mas sua posição interna pode permanecer abstrata.
- Cidadãos podem sair para alimentação, lazer e socialização.
- Cidadãos podem visitar amigos e familiares em outras residências.


### Relacionamentos, família e ciclo de vida

- Amizades e relacionamentos amorosos evoluem ao longo do tempo.
- Cidadãos podem formar união/casamento e criar um novo domicílio.
- Casais/famílias podem decidir ter filhos com base nas condições de vida e contexto da simulação.
- Nascimentos devem ocorrer em hospital quando houver acesso e capacidade adequados.
- O ciclo de vida inclui infância, adolescência, vida adulta e velhice.
- A fase da vida altera necessidades, educação, trabalho e comportamento.


### Migração, turismo e crescimento populacional

- Cidadãos podem entrar e sair da cidade por migração.
- Novos moradores podem se mudar para a cidade em resposta a fatores como emprego, moradia e qualidade de vida.
- Cidadãos podem deixar a cidade quando não encontram condições adequadas, incluindo trabalho, moradia ou satisfação.
- Turistas e visitantes existem como população temporária.
- O crescimento populacional deve vir de mecanismos reais da simulação, principalmente nascimentos e migração, evitando criação arbitrária de moradores.


### Aquisição residencial pela conexão exterior

- Residências privadas concluídas podem permanecer vazias enquanto aguardam comprador/ocupante compatível com a demanda.
- Famílias já existentes na cidade e famílias vindas da conexão exterior podem participar da procura por moradia.
- No primeiro modelo, uma família compradora vinda do exterior só pode adquirir uma residência para **migrar e morar na cidade**.
- Não haverá, neste primeiro modelo, investidor residencial externo que permaneça fora da cidade comprando imóveis apenas para receber aluguel.
- A procura exterior não é infinita: deve depender de fatores reais e diagnosticáveis, como emprego, preço, disponibilidade de moradia e atratividade da cidade.
- Baixa demanda pode manter uma residência vazia por mais tempo; isso é consequência econômica válida e deve ser legível para o jogador.
- Famílias locais podem comprar imóveis adicionais para aluguel quando tiverem recursos reais; a fórmula de decisão, a prioridade exata de herdeiros, a revenda e a granularidade de propriedade em apartamentos continuam em definição. A primeira aquisição de um ativo residencial recém-construído já segue as regras da Reserva Global desta SPEC.

### Turismo e hospedagem

- Turistas precisam de hospedagem real quando permanecem na cidade.
- Hotéis funcionam como empresas reais, com funcionários, capacidade, receita e ocupação.
- Parques, comércio, atrações e outros pontos de interesse podem aumentar a demanda turística.
- Turistas chegam fisicamente à cidade.
- A chegada pode ocorrer por rodovia, transporte público e carro particular.


### Conexão externa da cidade

- O mapa terá uma **conexão externa** que representa a ligação da cidade com o restante do mundo.
- Fluxos externos entram e saem da cidade por essa conexão.
- Turistas, novos moradores, cargas e outros fluxos externos usam essa conexão.
- Todos os meios de transporte externos devem se integrar a esse sistema de conexão com o exterior.
- O mapa deve começar com pelo menos uma **rodovia conectando a área jogável ao exterior**.
- A forma exata da conexão externa e quantos pontos físicos ela terá ainda podem evoluir, mas o conceito é obrigatório.


### Educação e qualificação

- Escolas têm funcionários reais e alunos reais.
- No primeiro modelo, a capacidade escolar depende da quantidade/capacidade de funcionários da escola e da quantidade de alunos atendidos, sem exigir cargos profissionais distintos.
- O jogo terá ensino superior/universidade.
- Educação e qualificação influenciam acesso a empregos e salários.
- Crianças e adolescentes podem ficar sem escola quando não houver vaga ou acesso adequado.
- A diferenciação futura entre cargos como professor e outras funções escolares fica fora do primeiro modelo.


### Água, esgoto, energia e poluição

- A cidade capta água de fontes naturais, como rio ou lago.
- A água precisa passar por tratamento antes do consumo.
- Prédios geram esgoto e a cidade precisa tratar esse esgoto.
- No escopo atual, água e esgoto não exigem desenho manual de encanamentos.
- O sistema funciona por demanda e capacidade de infraestrutura construída.
- Usinas geram energia real para a cidade.
- A energia também funciona por demanda e capacidade, sem exigir desenho manual detalhado da rede elétrica no escopo atual.
- Poluição é gerada por fontes reais, incluindo indústria, trânsito e esgoto.
- Poluição afeta saúde e valor/atratividade das áreas.
- Energia solar e eólica fazem parte das opções de geração.


### Resíduos e falhas de infraestrutura

- A cidade terá destinação física de resíduos, incluindo estruturas como aterro e/ou incineração, com capacidade real.
- Reciclagem fica fora do escopo inicial.
- Falta de água reduz a eficiência de empresas e serviços.
- Se a falta de água persistir, empresas e serviços podem interromper a operação.
- O tempo e os limiares para redução de eficiência e fechamento devem ser configuráveis.


### Falhas de energia e resposta à poluição

- Quando a demanda de energia supera a geração disponível, podem ocorrer apagões.
- Cidadãos podem decidir se mudar de áreas excessivamente poluídas.


### Terreno, vegetação e espaços públicos

- Edição manual de relevo fica fora do escopo inicial.
- Vegetação faz parte importante da apresentação da cidade.
- Árvores e vegetação podem ser removidas quando necessário para construir.
- Parques e espaços públicos podem afetar lazer, bem-estar e atratividade da cidade.
- Praças, bancos, playgrounds e outros espaços públicos devem ter uso real pelos cidadãos.
- Poluição sonora fica fora do escopo atual.




### Mapa, seed e limites

- O mapa é gerado/reproduzido a partir de uma **seed determinística**.
- A mesma seed deve permitir reproduzir o mesmo mapa/cidade-base para facilitar debug e testes.
- O mapa inicial será **plano** e já virá com vegetação, rio e/ou lago e conexão externa preparados.
- Não haverá compra/desbloqueio progressivo de novas áreas no escopo atual.
- O mapa começa com **borda fixa**.
- Não há recuo mínimo obrigatório entre construções e a margem de rios/lagos no escopo atual.


### Pontes, viadutos e sobreposição viária

- O jogador pode construir pontes.
- Ruas podem passar por cima de outras ruas por meio de viadutos/níveis sobrepostos.
- A malha viária deve permitir cruzamentos em níveis diferentes sem exigir interseção no mesmo plano.


### Saves e persistência

- O jogo permite múltiplos saves e múltiplas cidades.
- Autosave existe e sua frequência deve ser configurável.


### Demolição

- A **execução física da demolição** de prédios e vias é instantânea no escopo atual. A ordem sobre uma **residência ocupada** pode, porém, abrir o período de **desocupação assistida** definido abaixo: a remoção física só ocorre depois da transição, sem exigir uma obra de demolição prolongada.
- Demolição tem custo configurável.
- Se um prédio pertence a um SIM ou empresa, uma ordem municipal de demolição deve respeitar também a regra de **intervenção sobre propriedade privada** abaixo; a ferramenta não pode ignorar a indenização ou as consequências sobre ocupantes e operações.


### Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos

- **Intervenção com indenização automática (opção B):** para preservar a liberdade de redesenhar a cidade, o jogador pode determinar a intervenção, demolição ou realocação assistida de uma construção concluída **mesmo quando ela já pertence a um SIM ou empresa privada**, sem precisar solicitar aprovação ou negociar individualmente com o proprietário. Isso é uma intervenção municipal, não uma movimentação gratuita do patrimônio de terceiros.
- **Propriedade e dinheiro reais:** antes da retirada de um imóvel privado, a cidade deve pagar **indenização monetária ao proprietário econômico registrado** (SIM ou empresa), usando dinheiro disponível do **Caixa da Cidade**. **Indenização = 100% do valor de mercado atual do imóvel, calculado imediatamente antes da intervenção, com as condições da localização original ainda válidas.** É o mesmo modelo de avaliação imobiliária já definido para compra e venda; não há desconto, sobretaxa, multiplicador ou negociação de valor na indenização. A própria intervenção/demolição não pode reduzir antecipadamente a avaliação usada para o pagamento. A fórmula geral do valor de mercado permanece sujeita à calibração conforme a seção de moradia e finanças públicas. Se não houver dinheiro para a indenização e demais custos obrigatórios, a intervenção não pode ser executada. O dinheiro é transferido ao proprietário real, **não à Reserva Global**, preservando a oferta monetária fixa; a propriedade antiga deixa de existir com a retirada do ativo.
- **Beneficiário único da indenização imobiliária:** paga-se o valor de mercado integral do imóvel **uma vez ao proprietário privado registrado**; não se paga o mesmo valor uma segunda vez por ser uma realocação assistida ou por o proprietário também morar nele. Em imóveis concluídos sem proprietário econômico real (inclusive patrimônio não reclamado), não há indenização fictícia à Reserva Global; continuam possíveis custos físicos reais da intervenção. A indenização por perda do imóvel **não inclui ajuda financeira adicional automática ao inquilino** nem cobre por si só custos de nova construção ou transferência de estoque, que seguem seus fluxos econômicos próprios.
- **Momento do pagamento — opção A aprovada:** a indenização é **transferida integralmente ao proprietário real no instante da confirmação da intervenção**, antes da desocupação, da retirada física e de qualquer alteração da avaliação provocada pela própria intervenção. A confirmação só é permitida com dinheiro existente suficiente no Caixa da Cidade para a indenização e os custos obrigatórios conhecidos; não há crédito fictício nem retenção na Reserva Global. O proprietário já pode usar o dinheiro para comprar ou alugar outro imóvel durante a transição.
- **Imóvel indenizado aguardando retirada:** se uma residência ainda estiver ocupada, sua estrutura e o vínculo de propriedade/locação continuam registrados apenas pelo tempo necessário à desocupação assistida, **marcados como intervenção confirmada e indenização paga**. O imóvel fica **fora de novas compras, revendas e ocupações**, não pode gerar segunda indenização e não deve aparecer como patrimônio negociável disponível além do dinheiro já recebido. O aluguel do ocupante atual continua devido ao proprietário registrado apenas enquanto o imóvel existir e a locação estiver vigente; ao fim, encerram-se ocupação e aluguel futuro, preservando dívidas preexistentes. A transferência antecipada não teletransporta moradores ou garante casa nova. Uma eventual operação de desfazer intervenção já indenizada não está definida e não pode criar devolução monetária automática ou duplicação de propriedade.
- **Casa substituta em realocação assistida residencial — primeira oferta prioritária ao antigo proprietário (2026-10-08):** quando a intervenção gerar uma **nova residência privada em outro endereço**, ela será **imóvel novo**, sem herdar automaticamente proprietário, contrato de locação ou moradores. **Quando estiver fisicamente concluída e disponível para aquisição**, o **SIM proprietário anterior indenizado recebe a primeira oportunidade automática de decidir comprá-la**, pelo **valor de mercado vigente da nova localização**; deve ser elegível, estar economicamente apto e usar **dinheiro real**, sem receber casa gratuita, desconto, reserva indefinida ou preço de indenização congelado. **Se aceitar e puder pagar**, a aquisição ocorre pelas regras normais, com **primeiro pagamento à Reserva Global**. **Se rejeitar, não puder pagar ou não for elegível**, a prioridade termina e a casa é oferecida no **mercado residencial normal** a famílias locais ou candidatas externas elegíveis, sem comprador ou ocupação garantidos. A avaliação de interesse/viabilidade ocorre no **evento da primeira oferta**, sem negociação manual ou consulta contínua a todos os SIMs; a prioridade não suspende as regras de moradia/desocupação nem garante que o antigo proprietário continue na cidade até a conclusão. O imóvel pode ficar vazio, materiais/obra/logística continuam reais e a compra não representa segunda indenização. **Prioridade de compra não equivale à propriedade automática do novo imóvel.** Esta decisão **substitui a regra anterior de mercado aberto sem preferência**.
- **Dono não é necessariamente ocupante:** quando uma casa está alugada, o proprietário é quem recebe a indenização pelo imóvel, mas a família locatária sofre o deslocamento. Quando é moradia própria, a mesma pessoa pode ser proprietária e moradora. Famílias desalojadas entram no fluxo já definido de busca **automática** por outra residência compatível com seus recursos; se não houver, podem ficar sem moradia ou emigrar conforme as regras atuais. A indenização ao proprietário **não garante** nova casa nem concede dinheiro fictício ou compensação automática ao inquilino.
- **Desocupação assistida em intervenção municipal residencial (opção C):** se houver moradores numa residência escolhida para demolição ou realocação por ordem do jogador, a confirmação inicia um **período de transição curto e de andamento automático**. Durante esse período, a família pode continuar no endereço original enquanto a simulação procura outra moradia existente, disponível e financeiramente compatível, sem exigir que o jogador escolha o destino ou negocie com cada SIM. Uma opção encontrada não é residência gratuita: compra ou locação exige as transações reais já definidas.
- **Conclusão da desocupação:** se os moradores desocuparem antes do prazo, a intervenção pode prosseguir sem esperas artificiais. **Ao fim do prazo curto, a intervenção não fica bloqueada indefinidamente por falta de outra casa**: a família que não se mudou deixa o imóvel e passa ao fluxo já aprovado de ausência de moradia ou eventual emigração. Não inventar imóvel substituto, saldo, migração garantida nem obrigação municipal de reassentamento individual. Uma residência ainda ocupada não pode ser fisicamente demolida antes de resolver esse deslocamento, nem manter moradores num prédio já removido.
- **Aluguel, proprietário e indenização durante a transição:** enquanto o imóvel original continuar existindo e ocupado, mantém-se a propriedade registrada e a locação vigente, quando houver, com **aluguéis mensais ao proprietário real** conforme as regras gerais, sem suspensão, perdão ou cobrança dupla automática. A indenização de **100% do valor de mercado imediatamente anterior à intervenção** é paga **uma única vez ao proprietário registrado no momento da confirmação da ordem**, antes do começo da busca/retirada; pagar a indenização não equivale a dar a casa nova ao morador. O imóvel continua temporariamente registrado e bloqueado para comércio enquanto durar a desocupação assistida, sem nova indenização. Depois da desocupação/retirada, não nasce aluguel futuro de um imóvel inexistente, e dívidas já vencidas seguem sua regra própria. A liquidação econômica e a remoção do ativo precisam conservar a oferta monetária e impedir proprietário/ocupantes duplicados.
- **Prazo e escopo:** a transição deve ser curta para não transformar correção urbana em espera punitiva; o **número exato de dias de simulação fica para calibração antes da implementação dependente dele**. Ela é **distinta** dos prazos de três meses já definidos para inadimplência, imóvel sem proprietário e compra por novo dono para moradia própria: não substitui nem estende automaticamente esses casos. A regra vale para **residências ocupadas afetadas por intervenção municipal**; a continuidade condicionada da operação antiga de empresas/serviços durante a realocação já está aprovada; seus detalhes de paralisação, transferências e aquisição da construção substituta continuam abertos.
- **Leitura de impacto:** na prévia da ordem e enquanto a desocupação estiver pendente, apresentar de modo agregado o número de famílias afetadas, o valor da indenização e o estado da mudança, sem pop-ups obrigatórios por família ou cálculos globais contínuos. Casos de falta de moradia devem ser visíveis como consequência real, não como bloqueio manual de toda a reforma.
- **Continuidade operacional condicionada durante a realocação — opção C aprovada (2026-10-08):** quando o jogador usa **Realocar** sobre um estabelecimento concluído **privado (comércio/indústria) ou serviço municipal**, a operação no prédio **antigo pode continuar normalmente enquanto ele ainda existir, estiver funcional e seu espaço/acesso não precisar ser liberado pela intervenção urbana**. A intenção é evitar fechamento antecipado e espera artificial quando a substituição puder acontecer sem conflito físico. **Se a intervenção exigir retirar ou tornar indisponível o endereço antigo antes de o novo estar apto a operar**, a atividade naquele endereço **é interrompida**, com efeitos reais sobre atendimento, produção, emprego, clientes e finanças; a cidade não precisa bloquear indefinidamente a obra para preservar uma operação. A construção nova continua dependente de obra, materiais, trabalhadores, acesso e tempo reais. Estar pronta fisicamente **não garante continuidade econômica** da empresa, reabertura de serviço, comprador ou operador do novo prédio.
- **Paralisação não obrigatória — decisão de gameplay confirmada em 2026-10-08:** **Realocar não impõe uma etapa fixa de paralisação**: buscar operação ininterrupta ou interrupção mínima quando a obra e a transferência puderem ocorrer com meios reais. **Tempo de preparação/construção e tempo de atividade interrompida são diferentes.** Se o estabelecimento antigo puder operar durante a construção, ele continua conforme a opção C; não parar apenas porque o jogador confirmou a mudança, nem inserir dias/segundos fictícios de fechamento para simular realismo. **Quando a liberação antecipada do terreno, a ausência de acesso/recursos, a logística física ou a impossibilidade real de operar exigirem parada, a interrupção acontece e pode durar o tempo necessário**, com consequências reais e causa legível. Não definir agora tempo máximo ou mínimo rígido nem garantir reabertura: obra, materiais, trabalhadores, estoque e operador seguem condições econômicas/físicas existentes. **Não criar um subsistema especial de salários, suspensão ou retenção seletiva de vagas apenas por causa da ferramenta Realocar sem evidência de que a paralisação relevante exija comportamento não coberto pela SPEC**; vínculos, pagamentos efetivamente devidos e custos continuam reais, e situações de paralisação prolongada ainda dependem de definição específica antes de implementação que dela dependa. Preservar o direito já aprovado de reavaliação do emprego na mudança efetiva de endereço.
- **Demolição, indenização e propriedade na transição:** a opção C é uma diretriz de **Realocar**, não um novo prazo obrigatório para uma ordem de **Demolir** diretamente. A demolição física permanece instantânea **no momento em que pode ser executada**, respeitando a desocupação residencial já definida, e encerra a possibilidade de operar no prédio retirado. Para ativo privado, a indenização integral continua sendo paga **na confirmação da intervenção**, uma única vez, mesmo que o estabelecimento funcione por algum tempo no endereço antigo. Enquanto aguarda retirada, o ativo permanece vinculado ao titular para operações existentes, **identificado como já indenizado e indisponível para nova venda/indenização**; funcionar temporariamente não restaura o direito de revendê-lo nem gera segundo pagamento. A opção C **não altera** a desocupação assistida das residências nem promete que a propriedade do imóvel novo será transferida ao antigo titular.
- **Caixa, estoques e pessoas durante a transição:** enquanto a atividade continuar de fato no endereço antigo, mantém-se sua **economia real normal**: receitas, insumos, custos, salários efetivamente devidos e movimentações físicas usam os mesmos agentes, regras e estoques. Se a atividade for interrompida, **não há produção, vendas/atendimento ou capacidade daquele prédio inexistente ou inoperante**; valores já devidos, ativos e vínculos não podem desaparecer ou ser quitados ficticiamente. **Estoques permanecem quantidades físicas com localização/titularidade**, não são copiados nem teleportados ao novo endereço; a recuperação possível e a perda na retirada seguem a regra aprovada abaixo. Sequência e parâmetros finos de transporte continuam para calibração sem criar nova camada de logística. **O tratamento dos vínculos, vagas e salários durante uma paralisação** continua aberto: não presumir nem pagamento garantido sem trabalho nem demissão/suspensão automática indiscriminada. Quando houver **mudança efetiva de local de trabalho e vaga existente**, aplica-se a reavaliação individual já aprovada para funcionários públicos e privados. Em serviços municipais, a continuidade física até a retirada pode preservar atendimento enquanto houver condições reais de funcionamento; a interrupção deve reduzir a capacidade efetiva, sem criar atendimento fictício.
- **Jogabilidade, condições e limites:** a ação assistida deve informar **de modo agregado** se a operação antiga pode continuar, a existência de obra nova, a possibilidade de interrupção e as consequências principais; não exigir administrar empresa, trabalhadores ou estoques individualmente. Mudanças relevantes de estágio/ocupação do terreno disparam verificações proporcionais **por evento**, sem varredura global constante. O critério concreto de compatibilidade espacial e o calendário fino de retirada, a coordenação operacional entre dois prédios, a continuidade da identidade econômica/institucional segue a regra aprovada abaixo; o tratamento de contratos/estoques/salários na paralisação e a execução operacional da aquisição do novo prédio privado **ainda exigem especificação antes de implementação dependente**, sem transformar a opção C em garantia silenciosa de continuidade.
- **Decisão da empresa sobre o endereço substituto — autonomia aprovada em 2026-10-08:** quando o jogador usa **Realocar** sobre uma fábrica, comércio ou outro estabelecimento de empresa privada, **a empresa proprietária indenizada decide autonomamente se deseja voltar a operar no endereço proposto**, considerando valor efetivo de aquisição, caixa e oportunidades, clientela, fornecedores, logística e mão de obra. O jogador define a **intervenção física**, não determina que a empresa aceitará operar na nova sede; ela pode considerar outras oportunidades disponíveis, não adquirir o destino ou deixar de operar naquele estabelecimento, com os efeitos financeiros reais. **Essa decisão é distinta da escolha individual dos funcionários**, já aprovada: mesmo que a empresa opte pelo destino, cada SIM pode reavaliar o próprio emprego quando o endereço de trabalho mudar. O prédio novo não herda automaticamente empresa, estoque, funcionários nem propriedade, e a empresa não recebe dinheiro ou prazo de sobrevivência fictícios; se não operar, não há produção/receita naquele local. **A empresa proprietária original tem prioridade de primeira oferta para adquirir o prédio substituto**, da mesma forma que um SIM antigo proprietário residencial: quando a construção ficar pronta e disponível, **ela é a primeira a avaliar e decidir a compra ao valor de mercado vigente da nova localização**, com **seu próprio caixa real** e elegibilidade econômica; a primeira aquisição paga a **Reserva Global**. Se não quiser, não puder comprar ou tiver encerrado definitivamente, o prédio segue ao **mercado normal de empresas candidatas**, sem reserva indefinida. **A prioridade não garante venda, propriedade automática, operação, sobrevivência da empresa ou emprego para os funcionários**; a empresa decide autonomamente se deseja comprar e operar, e cada trabalhador tem seu direito próprio de reavaliar o vínculo. A avaliação é pontual na oferta, sem novo sistema recorrente por agente. A oportunidade de decisão surge de um evento pertinente da realocação/oferta, sem consulta contínua por empresa. A continuidade de vínculos e ativos durante paralisações ou a sequência operacional exata seguem em exploração; não reabrir o princípio da decisão autônoma por causa dessas lacunas.
- **Continuidade da entidade econômica/institucional na realocação — opção C aprovada (2026-10-08):** se a **empresa original adquirir** o imóvel privado substituto conforme sua prioridade de primeira oferta e as regras normais de compra, **ela continua sendo a mesma entidade econômica**, com seu caixa real, obrigações e histórico; mudar de endereço **não** encerra ou recria automaticamente a empresa nem exige recomeçar contratações e abastecimento do zero. Os vínculos de trabalho existentes **só permanecem quando empregador, vaga e possibilidade efetiva de trabalho continuarem válidos**; na mudança efetiva do endereço, cada funcionário mantém o direito à reavaliação individual já decidido. **A operação no prédio novo só pode ocorrer quando existir aquisição válida, estrutura apta, acesso, trabalhadores, insumos, estoques e demais condições reais**; comprar o imóvel não libera produção nem atendimento imediato fictícios. Mercadorias e outros estoques físicos mantêm titularidade, quantidade e localização rastreáveis: sua transferência, quando possível, usa a logística real existente, sem duplicação ou teletransporte e sem exigir ordens manuais de mudança do jogador. A operação antiga pode continuar durante preparação e transporte somente enquanto o endereço antigo efetivamente puder funcionar; não há operação dupla ou capacidade adicional gratuita. Se o prédio antigo for retirado antes de o novo operar, a empresa conserva identidade e caixa **enquanto economicamente existente**, mas perde a capacidade/receita daquele estabelecimento durante a interrupção e segue normalmente sujeita a custos, insolvência e encerramento definidos na SPEC; a prioridade de compra não suspende falência nem garante retorno. **Serviços municipais realocados preservam a continuidade da instituição/serviço público**, sem compra privada ou recriação institucional artificial, mas atendimento e capacidade dependem dos prédios e recursos realmente operacionais; vagas e funcionários só persistem se continuarem válidos, com reavaliação individual na mudança de local. Esta regra **não cria novo subsistema de mudança**, salários especiais ou transporte fictício. O princípio de recuperação e perda do estoque sem destino apto está definido abaixo; coordenação fina das entregas e tratamento específico de vagas/salários em interrupções prolongadas continuam abertos antes da implementação desses casos.
- **Recuperação automática de estoque em realocação — opção C aprovada (2026-10-08):** ao realocar estabelecimento concluído com mercadorias ou suprimentos físicos modelados, a simulação **tenta preservar automaticamente os bens existentes quando isso for viável antes da retirada física do prédio antigo**, reutilizando estoques, destinos, transportes e transações normais. Quantidades elegíveis podem seguir para um endereço real com capacidade e acesso disponíveis, mediante transporte físico, ou ser vendidas/exportadas pelos canais econômicos já aprovados **somente se houver comprador/destinatário real elegível, pagamento real quando cabível, frete, rota e tempo viáveis**; exportações continuam condicionadas à Reserva Global e suas regras. A operação não garante escoamento, compra externa, depósito temporário, transportador ou reembolso. **Se a retirada não puder esperar e ainda houver quantidade física sem saída viável antes da remoção, esse estoque restante é perdido**, baixado uma única vez da simulação e reconhecido como perda patrimonial/operacional de seu titular, **sem criar pagamento, receita, indenização adicional ou retorno mágico do bem**. O risco não bloqueia indefinidamente a intervenção, nem permite material persistir num prédio inexistente. Enquanto a estrutura funcionar, a atividade e o abastecimento seguem as regras existentes; não impor parada nem venda antecipada obrigatória se não forem necessárias. A prévia/confirmação da realocação sinaliza **de forma agregada** estoque afetado e risco de perda; na execução, o jogador pode diagnosticar quantidade preservada, vendida/exportada e perdida, com causas reais, **sem comandar remessas, caminhões ou negociações individualmente**. A regra não inventa inventário separado de máquinas/equipamentos nem se confunde com materiais já consumidos em canteiros de obra. Checagens por eventos relevantes, sem varreduras globais ou uma segunda cadeia logística de mudança.
- **Empresas também são afetadas:** se a intervenção atingir comércio ou indústria privada, a indenização é paga à **empresa proprietária** e a retirada do estabelecimento tem efeitos reais sobre sua operação, empregados, acessos e estoques. Não presumir continuidade automática, teletransporte de estoques ou transferência de empregados/empresa para um novo imóvel.
- **Experiência do jogador:** realocação assistida de prédio concluído é a direção de interação escolhida para **avaliar no jogo integrado**: um comando guiado, com indicação de custo, propriedade e impactos, evitando que o jogador repita manualmente uma sequência longa de ações. Isso **não** dispensa custos de obra, materiais, logística nem indenização, nem move pessoas, dinheiro ou bens instantaneamente. As regras econômicas acima valem também quando a mudança for feita diretamente por demolição. Forma operacional detalhada da realocação pronta, logística fina de cargas em trânsito, tratamento de vínculos/salários durante interrupções e **detalhes de desocupação ainda não definidos acima** seguem abertos para especificação ou calibração; a continuidade da identidade econômica da empresa e institucional do serviço, quando aplicável, já foi aprovada, sem assumí-los no código. A **continuidade condicionada da operação do endereço antigo (opção C)** já foi aprovada e não pode ser substituída silenciosamente por encerramento imediato nem por preservação garantida. **A titularidade da casa privada substituta já foi decidida**: imóvel novo, sem herança automática da propriedade antiga; após a conclusão, primeira oferta prioritária ao SIM proprietário original indenizado, pelo valor de mercado vigente, e somente depois mercado residencial geral se ele não adquirir. A **desocupação assistida de residências ocupadas já foi aprovada** e não pode ser substituída por retirada instantânea dos moradores.
- **Prévia de avaliação imobiliária durante realocação — opção B aprovada (2026-10-08):** ao posicionar uma **nova localização para imóvel privado** residencial, comercial ou industrial, a interface mostra **discretamente a tendência de valorização ou desvalorização estimada** e seus fatores principais, sem obrigar o jogador a acompanhar uma tabela de preços durante cada movimento. **Na inspeção opcional e na confirmação da ação**, o jogador pode consultar **o valor de mercado atual do imóvel original, o valor estimado do imóvel na nova localização e a diferença estimada**, acompanhados de causas concretas (por exemplo, serviços, acessibilidade, demanda e poluição) e identificação clara de que a nova avaliação é **estimativa, não garantia**. A confirmação deve manter visíveis custos obrigatórios reais, indenização ao proprietário e impactos sobre SIMs, distinguindo tais custos da projeção do valor futuro. A prévia usa o modelo imobiliário existente, **sem multiplicadores, bônus monetários ou mercado fictício exclusivos da ferramenta**; durante o posicionamento, atualizar apenas quando a localização/condições relevantes mudarem, **sem exigir recomputação integral em todo frame**. Para eventual compra futura, prevalece a regra já aprovada: **valor de mercado efetivo na data da transação**, mesmo se diferente da prévia. A indenização do imóvel antigo permanece definida pelo valor vigente **na confirmação da intervenção**. A prévia de preço não altera nem cria prioridade de compra: vale a primeira oferta ao proprietário antigo (SIM ou empresa) já aprovada acima, sem aquisição garantida ou mudança automática de propriedade. Para serviços e construções não privadas, informar custos e efeitos pertinentes, sem inventar preço de venda imobiliária ou comprador privado.
- **Impacto legível sem burocracia:** antes de uma intervenção com consequências significativas, informar de maneira agregada proprietário atingido, estimativa de custo, famílias possivelmente deslocadas e operações afetadas; depois, permitir entender os resultados reais. Não exigir decisões manuais para cada família ou empresa nem calcular continuamente cenários de desapropriação sem uma ação solicitada.


### Estado inicial da cidade

- A cidade começa essencialmente vazia, sem tecido urbano pré-construído.
- O mapa mantém apenas os elementos naturais já definidos e a conexão externa; nenhum edifício municipal, incluindo o Pátio Municipal de Obras, começa construído.


### Bootstrap sistêmico de moradores e empresas

- **Entrada por oportunidades concretas, sem ordem obrigatória entre moradores e empresas:** no início da cidade, famílias e empresas candidatas vindas do exterior podem considerar oportunidades economicamente plausíveis que ainda não se realizaram por completo. Empresas podem adquirir prédios econômicos concluídos mesmo antes de existir clientela ou mão de obra local suficiente, avaliando **condições observáveis** como moradias disponíveis, acessibilidade, infraestrutura, logística, custos e possibilidade de atrair trabalhadores e compradores. Famílias podem migrar mesmo sem emprego previamente ocupado/garantido, desde que exista uma possibilidade real de moradia conforme as regras de aquisição e que seus recursos monetários explícitos permitam assumir o risco; vagas efetivamente ofertadas, serviços, custos e condições da cidade influenciam a escolha.
- **Potencial não equivale a demanda realizada:** residências vazias não contam como clientes, vagas sem ocupantes não contam como trabalhadores e nenhuma expectativa autoriza vendas, salários, produção, contratação, migração ou abastecimento fictícios. A chegada de famílias e empresas continua voluntária, condicionada à avaliação econômica, e pode falhar; não existe ocupação, crescimento, rentabilidade ou resgate garantido.
- Os capitais externos continuam saindo da **Reserva Global**, respeitando saldo disponível e oferta monetária fixa; compra de imóveis, estoque, insumos, água/energia, transporte e demais operações seguem as regras físicas e monetárias normais. O jogador não aprova migrações ou aquisições uma a uma; a simulação deve distinguir **oportunidade potencial** de condições efetivamente atendidas e explicar por que uma entrada ocorreu, não ocorreu ou fracassou. Pesos, periodicidade de avaliação e reservas financeiras mínimas ficam para calibração, sem sistema de promessas ou contratos prévios.

### Comércio exterior e logística externa

- Caminhões vindos do exterior podem ser operados por motoristas externos, que não pertencem à população simulada da cidade.
- A cidade pode exportar excedentes produzidos localmente pela conexão externa.
- Exportações e importações usam fluxos físicos pela conexão externa.


### Agricultura

- Fazendas ocupam terreno físico real.
- Fazendas empregam trabalhadores reais da população simulada.
- A produção agrícola é transportada fisicamente por veículos de carga.


### Centro de Materiais de Construção

- O armazenamento principal de materiais de construção será tratado como **Centro de Materiais de Construção**, evitando que a interface force uma distinção de propriedade pública/privada.
- O Pátio Municipal de Obras pode manter um estoque pequeno; o Centro de Materiais oferece capacidade significativamente maior.
- O Centro funciona como infraestrutura física de armazenamento e logística da cidade; sua existência não implica que todo material armazenado pertença juridicamente à prefeitura.


### Fluxo de importação para obras

- Quando uma obra específica depende de material importado, os caminhões externos podem entregar diretamente no canteiro a quantidade necessária para aquela obra.
- Essa entrega direta não exige passagem pelo Centro de Materiais de Construção.
- Para materiais de construção, não há importação genérica para formar estoque no escopo atual; a importação ocorre quando uma obra exige recursos indisponíveis localmente.


### Capacidade e operação do Centro de Materiais

- Centros de Materiais de Construção possuem capacidade física limitada.
- No escopo inicial, um mesmo Centro pode armazenar todos os tipos de **materiais de construção**.
- Centros possuem capacidade limitada de carga e descarga simultânea.
- Saturação de carga/descarga pode gerar fila física de caminhões.


### Exportação automática

- No escopo inicial, excedentes elegíveis podem ser exportados automaticamente.
- O pagamento externo sai da Reserva Global e vai para o agente econômico que efetivamente vende a mercadoria; exportação privada não vira automaticamente receita do Caixa da Cidade.
- Toda receita e movimentação relevante de exportação deve aparecer de forma clara na leitura econômica/financeira da cidade.


### Disponibilidade, reserva e prioridade de materiais de obra

- Para o jogador, materiais de construção são apresentados como **Disponível na cidade**, agregando a oferta local elegível.
- Essa visão agregada não teletransporta materiais: cada quantidade continua existindo em uma localização física real, como Centro de Materiais, Pátio, fábrica ou fornecedor.
- Ao confirmar uma obra, o sistema reserva automaticamente materiais locais elegíveis.
- Materiais reservados deixam de estar disponíveis para outras obras.
- Se faltar material local, o sistema importa a quantidade necessária conforme as regras de importação já definidas.
- Caminhões continuam obrigatórios para levar o material de sua origem física até o canteiro.
- Quando várias obras disputam a mesma oferta local, a prioridade inicial segue a ordem em que as obras foram criadas.


### Política inicial de importação de materiais

- No escopo atual, a importação de materiais de construção acontece em resposta a uma obra que exige recursos indisponíveis localmente.
- Não existe compra genérica automática de materiais apenas para encher estoque.
- O custo de frete da importação faz parte do custo monetário total exibido para a construção.


### Cancelamento de obras e demolição

- Ao cancelar uma obra ainda não concluída, materiais apenas reservados e ainda não entregues/consumidos deixam de ficar reservados.
- Materiais que já chegaram ao canteiro foram consumidos e não retornam ao depósito.
- **Dinheiro:** cancelar uma obra ainda não concluída libera o valor monetário **reservado e ainda não pago**, que continua pertencendo ao Caixa da Cidade. Gastos efetivamente pagos a fornecedores, trabalhadores ou contrapartes externas não são reembolsados automaticamente. A Reserva Global não recebe dinheiro apenas comprometido. A interface informa antes de confirmar o cancelamento quanto orçamento será liberado e quais custos e materiais já foram perdidos.
- Demolir uma construção já realizada não devolve o dinheiro nem os materiais originalmente gastos.


### Reposicionamento de obra ainda não concluída

- O jogador pode selecionar uma **obra inacabada** e usar **Mover** para escolher um novo local ou traçado, com prévia do posicionamento, acessos, custo e consequências antes de confirmar, **sem precisar cancelar manualmente e procurar novamente o item no catálogo**.
- O reposicionamento equivale, para as regras econômicas e materiais, a **cancelar o projeto no local anterior e criar a obra no novo local em uma única ação assistida**; deve reaproveitar essas regras existentes em vez de criar transporte instantâneo de prédio, mão de obra ou materiais. O novo local inicia sua própria execução e passa pelas validações normais de ocupação, acesso, custos, equipes e logística; trabalho/progresso físico da obra anterior não são transferidos magicamente.
- **Sem gastos nem entregas efetivas**, mover é gratuito: libera ou reatribui reservas antigas e compromete o orçamento do novo local sem pagar multa ou duplicar retenções. **Com gastos ou entregas já ocorridos**, permanecem as perdas previstas no cancelamento: dinheiro efetivamente pago não é estornado, materiais já entregues ao canteiro foram consumidos; reservas ainda não pagas/entregues são liberadas ou recalculadas para a obra no novo local. O jogo apresenta antes da confirmação as perdas já ocorridas, os custos adicionais e a necessidade de orçamento disponível.
- Pedidos pagos, caminhões e materiais **já em trânsito** não podem ser duplicados nem teletransportados ao novo endereço: devem continuar rastreáveis e receber tratamento logístico coerente na implementação, sem reembolso fictício nem microgerenciamento do jogador. O mecanismo de redirecionamento/encerramento de entregas e a recalibração detalhada dos custos ficam para validação técnica.
- **Prédios e infraestrutura já concluídos não são abrangidos pelas regras de Mover obra inacabada.** Para esses ativos, a SPEC já aprovou **intervenção com indenização** e realocação assistida como direção de experiência a validar; os detalhes operacionais, especialmente continuidade de serviços/empresas e destino da nova construção, permanecem abertos. **A retirada de moradores de residências ocupadas já segue a regra de desocupação assistida aprovada**, com duração curta ainda a calibrar; isso não define automaticamente a mudança de estoque ou funcionários.

### Pátio Municipal de Obras

- A execução das obras municipais depende de um **Pátio Municipal de Obras** construído pelo jogador.
- O Pátio emprega trabalhadores públicos reais da população simulada.
- Esses trabalhadores recebem salários pagos pela prefeitura e registrados explicitamente nas despesas municipais.
- O Pátio possui capacidade operacional limitada, determinada pela quantidade de equipes/trabalhadores públicos disponíveis para executar obras.
- Quando a capacidade local do Pátio é insuficiente para uma nova obra, a obra não precisa esperar: a prefeitura pode contratar/importar uma equipe externa temporária, aumentando o custo monetário daquela construção.
- O Pátio também possui um pequeno estoque físico de materiais de construção.
- O estoque do Pátio tem capacidade menor que a de um Centro de Materiais de Construção.
- Centros de Materiais continuam sendo a infraestrutura principal para armazenamento de grandes quantidades de materiais.


### Bootstrap inicial de construção

- A cidade não começa com o Pátio Municipal de Obras pronto.
- As primeiras obras necessárias para colocar o sistema municipal em funcionamento podem ser executadas por uma **equipe externa temporária**.
- Essa equipe e seus trabalhadores vêm pela conexão externa e não pertencem à população residente da cidade.
- A equipe externa pode executar o primeiro Pátio Municipal de Obras e a infraestrutura mínima necessária para viabilizá-lo.
- Materiais dessas primeiras obras continuam obedecendo às regras normais de custo, importação e entrega física.
- Depois que o Pátio Municipal de Obras entra em operação, as obras municipais passam ao fluxo normal com trabalhadores públicos da cidade.


### Capacidade local e mão de obra externa de obras

- Obras usam equipes locais do Pátio Municipal de Obras quando houver trabalhadores públicos disponíveis.
- A quantidade de trabalhadores/equipes disponíveis define quantas obras podem ser atendidas localmente.
- Se não houver capacidade local suficiente, a prefeitura pode contratar uma equipe externa temporária pela conexão externa.
- O uso de equipe externa aumenta o custo monetário da obra e deve aparecer explicitamente no custo apresentado ao jogador.
- Falta de capacidade de mão de obra local, por si só, não bloqueia a criação de novas obras.


### Consumo de materiais no canteiro

- Os materiais necessários para uma obra são consumidos de uma vez quando a carga correspondente chega ao canteiro.
- A obra não precisa exibir consumo gradual de materiais ao longo do progresso.


### Armazenamento por tipo de recurso

- Centros de Materiais de Construção armazenam materiais de construção: areia e brita, concreto, aço, madeira e asfalto.
- Alimentos, combustível e suprimentos médicos não usam o Centro de Materiais como estoque normal.
- Esses bens usam armazenamento coerente com sua função, como estoques de mercados/comércios, postos de combustível, hospitais, farmácias e outras instalações apropriadas.


### Abastecimento inicial de alimentos e combustível

- Mercados/comércios podem importar automaticamente Alimentos quando a oferta local não atender à reposição ou quando a alternativa externa for significativamente melhor na comparação econômica, respeitando a **preferência local moderada** e os critérios de custo total, disponibilidade e prazo já definidos para abastecimento comercial.
- A importação de Alimentos paga produto e frete e gera entrega física.
- No escopo inicial, Combustível pode ser importado já refinado/pronto pela conexão externa e distribuído fisicamente aos postos.
- Refino/produção local de combustível pode ser aprofundado depois, sem ser necessário para o primeiro fluxo funcional da cidade.


### Visão agregada de materiais

- A interface deve mostrar a oferta de materiais de construção de forma agregada em nível da cidade.
- Para cada material, o jogador deve conseguir entender pelo menos o total disponível localmente, o que está reservado, o que está em trânsito e quanto precisará ser importado para uma nova obra.
- A agregação é apenas de interface e decisão; localização física, transporte, capacidade de armazenamento e congestionamento continuam simulados.
