# IndexCities — SPEC

> **Este documento é a fonte de verdade do produto.**
>
> Ele descreve somente decisões já tomadas para o IndexCities. Não herda requisitos de projetos, protótipos ou conversas anteriores.
>
> Nova funcionalidade de produto entra aqui **antes** da implementação. Pesquisa e alternativas ficam em [EXPLORATION.md](EXPLORATION.md) até existir uma decisão.

## Estado atual

O IndexCities está em **fase inicial de definição**.

Neste momento, o objetivo é deliberadamente não preencher esta SPEC com decisões prematuras. Plataforma, direção visual, escala, sistemas de gameplay, simulação, persistência, performance-alvo e demais características serão discutidos e decididos progressivamente.

## Regras da SPEC

- Só entra aqui aquilo que foi explicitamente decidido para o IndexCities.
- Decisão antiga de outro projeto não é requisito deste projeto.
- Uma hipótese pode ser experimentada sem virar requisito.
- Quando uma exploração resultar em decisão de produto, esta SPEC deve ser atualizada de forma curta.
- Se uma decisão for substituída, este documento deve refletir o estado desejado atual, não manter versões antigas por histórico.

## Decisões de produto confirmadas

### Conceito e apresentação

- O IndexCities será um **city builder**.
- A apresentação será **3D com câmera isométrica**.
- A construção será feita na granularidade de **casas e prédios**, sem construção por cômodos.
- No escopo atual, o jogador posiciona **cada prédio diretamente**; zoneamento automático não faz parte da proposta atual.

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
- No escopo atual, quando o jogador conclui um prédio de atividade econômica privada, como fazenda, mercado, posto ou fábrica, o prédio pode permanecer **vazio/procurando operador** até que uma empresa local ou vinda da conexão exterior decida adquiri-lo e operá-lo.
- Empresas terão funcionários reais.
- Cada emprego corresponderá a uma **vaga real** dentro de uma empresa ou serviço.
- Empresas privadas terão **caixa monetário real e individual**, mantido pela simulação e auditável.
- Estoque e/ou produção devem existir como parte do modelo econômico das empresas.
- O consumo será modelado inicialmente por **categorias de produtos**, não por SKU individual.
- **Compras de bens de consumo pelos cidadãos são presenciais no primeiro modelo:** o SIM se desloca fisicamente até um comércio acessível, realiza a compra de **categorias de produtos efetivamente disponíveis no estoque** com dinheiro real, e segue seu deslocamento de volta ou para o próximo destino, sem teletransporte. A compra reduz o estoque do estabelecimento e transfere o pagamento ao caixa da empresa operadora; ausência de estoque ou dinheiro impede a aquisição correspondente. As viagens geram movimento real de pedestres/veículos e demanda local, inclusive fora da câmera. **Não substituir a ida ao comércio por uma transferência econômica abstrata que dispense a viagem**. **A escolha do comércio é automática e considera preço, distância/acessibilidade real e disponibilidade de estoque.** Se o estabelecimento escolhido não puder atender a compra (por exemplo, falta de produto), o SIM **pode buscar outro comércio acessível** com o produto e recursos compatíveis, com os deslocamentos correspondentes; não há estabelecimento habitual obrigatório nem reposição garantida. **Se nenhuma opção viável atender à demanda, a compra não ocorre e a necessidade continua não atendida**, com as consequências já previstas. A decisão não cria mercadorias, dinheiro nem teletransporte. A ponderação dos fatores, o limite de tentativas e o custo de busca devem ser calibrados/prototipados para evitar varreduras contínuas de toda a cidade por cada SIM. A frequência e o agrupamento de compras continuam para calibração, sem exigir compras por SKU ou uma viagem para cada item/categoria.
- **Estoque doméstico real por categoria:** bens de consumo destinados ao domicílio, começando por **Alimentos**, são comprados em quantidades para vários dias, trazidos fisicamente da loja para casa e registrados em **quantidades agregadas por categoria no estoque da família/domicílio**, sem inventário individual por SIM, unidades de SKU ou simulação de cada embalagem. As necessidades dos moradores consomem gradualmente essas quantidades ao longo do tempo; o consumo não faz novo pagamento, pois o dinheiro já foi transferido na compra real. Quando o estoque fica baixo, novas compras presenciais podem ser planejadas automaticamente, respeitando preço, disponibilidade, deslocamentos e recursos reais. **Falta de estoque nos comércios não elimina imediatamente a reserva que a família já tem em casa**; se o estoque doméstico se esgotar e não houver reposição viável, a necessidade correspondente deixa de ser atendida, com consequências reais. Volume comprado, ritmo de consumo, gatilhos de reposição, transporte dos bens durante a viagem e tratamento dos itens em mudança/perda da moradia ficam para calibração/protótipos, sem exigir microgestão do jogador.
- Compras reais reduzem estoque real, movimentam dinheiro real e geram necessidade de reposição/logística.
- Empresas poderão falir quando não conseguirem sustentar suas obrigações econômicas; o jogador deve conseguir identificar as receitas, despesas e eventos concretos que levaram à deterioração e à falência.
- Fazendas e agricultura produzem diretamente a categoria **Alimentos** no escopo inicial.
- Mercadorias físicas modeladas como estoque podem ser importadas pela conexão externa quando a oferta local for insuficiente ou inexistente.
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

### Princípio de design: leitura em camadas e causalidade

- A mesma simulação deve servir tanto a jogadores que preferem feedback direto quanto a jogadores que gostam de investigar estatísticas e otimizar sistemas.
- Problemas importantes devem aparecer primeiro por sinais visuais claros e consistentes, com uma causa resumida e uma ação compreensível.
- O jogador deve poder aprofundar o mesmo problema e ver dados, fatores e cadeia causal quando quiser.
- Consequências relevantes, como falência, falta de recurso, mudança de demanda ou interrupção de serviço, não podem depender de caixas-pretas impossíveis de explicar.
- Informações importantes não devem depender apenas de cor ou de um ícone sem contexto; texto curto, tooltip ou outro canal de explicação deve estar disponível.
- Simplificar a interface não significa simplificar artificialmente a simulação: o detalhe pode continuar existindo internamente desde que seja legível e diagnosticável.
- Não está decidido criar modos separados "simples" e "avançado"; o objetivo atual é obter profundidade progressiva na mesma experiência.

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

### Ferramenta de construção de ruas

- A ferramenta de ruas deve permitir construir um trajeto ortogonal em **L** num único gesto de clique e arraste quando o ponto inicial e o ponto final diferirem nos dois eixos.
- Durante o arraste, o jogo deve mostrar uma prévia completa do trajeto antes da confirmação.
- Quando houver duas formas possíveis de fazer o L, o jogador deve conseguir alternar ou influenciar de forma simples qual lado recebe a curva.
- Se início e fim estiverem alinhados, o mesmo gesto pode produzir um trecho reto; o objetivo não é obrigar curvas artificiais, mas evitar exigir que o jogador construa cada perna do L separadamente.
- A confirmação da rua deve respeitar as regras normais de obra, custo, materiais, terreno e demais restrições da simulação.

### Construção e obras

- Prédios e infraestrutura não aparecem instantaneamente prontos.
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

## Fora de escopo automático

Nada é considerado obrigatório apenas por ter existido em outro projeto.

Qualquer ideia anterior pode ser revisitada futuramente como material de pesquisa, mas precisa passar novamente pelo processo de exploração e decisão antes de entrar nesta SPEC.

A SPEC deve crescer com o produto, **não antes dele**.


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
- A primeira aquisição de um ativo privado recém-construído **não devolve o preço ao Caixa da Cidade**; o pagamento vai para a Reserva Global, evitando reciclagem automática de capital e preservando impostos/receitas públicas como fonte principal de recuperação financeira do jogador.
- Depois que uma empresa adquire e passa a operar o prédio, ela usa seu próprio caixa, receitas e despesas; o jogador não pode usar diretamente esse dinheiro como Caixa da Cidade nem precisa administrá-lo manualmente.
- O resultado de uma empresa privada beneficia ou prejudica a cidade por efeitos econômicos reais, como empregos, salários, impostos, produção, logística e eventual fechamento, não por transferência livre de seu caixa para o jogador.

### Financiamento de moradia

- O jogador continua decidindo diretamente onde as residências serão construídas, e a obra consome Caixa da Cidade + materiais físicos.
- Uma residência privada recém-construída pode ficar sem proprietário até a primeira aquisição.
- Na primeira aquisição, o comprador paga com dinheiro real; esse pagamento vai para a **Reserva Global**, não retorna ao Caixa da Cidade.
- Depois que existe um proprietário privado real, aluguel vai ao proprietário e revendas transferem dinheiro entre comprador e proprietário normalmente.
- A cidade recupera financeiramente o custo de desenvolver moradia principalmente de forma indireta, por impostos e demais receitas públicas, e não por revenda do ativo.

### Terreno e logística econômica

- Todo mapa jogável deve conter **pelo menos um rio ou um lago**.
- Estabelecimentos comerciais podem fechar quando não conseguem sustentar sua operação, incluindo falta de clientes.
- Indústrias precisam receber matéria-prima real e escoar produção real.
- Caminhões de carga circulam fisicamente entre fornecedores, indústrias, comércio e obras.


### Propriedade, demanda e vulnerabilidade social

- Lotes não precisam ter proprietário individual.
- Toda construção é colocada pelo jogador.
- A cidade deve calcular e expor **demanda por categoria de atividade**, em vez de uma única barra genérica de comércio/indústria.
- A demanda deve ser **principalmente local**, refletindo a clientela potencial acessível àquele estabelecimento e àquela área da cidade.
- Os fatores conceituais principais da demanda são população/clientes potenciais, capacidade já existente, acesso/tempo de deslocamento e concorrência entre estabelecimentos equivalentes ou substitutos.
- **Demanda é informação de gestão, não uma trava de construção:** o jogador pode construir mesmo quando a demanda é baixa.
- A interface deve alertar claramente quando uma nova atividade econômica estiver entrando em uma situação de baixa demanda ou alto risco de pouca clientela.
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

- Demolição de prédios e vias é instantânea no escopo atual.
- Demolição tem custo configurável.


### Estado inicial da cidade

- A cidade começa essencialmente vazia, sem tecido urbano pré-construído.
- O mapa mantém apenas os elementos naturais já definidos e a conexão externa; nenhum edifício municipal, incluindo o Pátio Municipal de Obras, começa construído.


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
- A política de devolução de valores monetários em cancelamento de obra ainda não está definida. Qualquer solução futura deve respeitar a oferta monetária fixa: reembolso é transferência de dinheiro existente, nunca criação/destruição de moeda.
- Demolir uma construção já realizada não devolve o dinheiro nem os materiais originalmente gastos.


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

- Se a oferta local de Alimentos for insuficiente, mercados/comércios podem importar automaticamente Alimentos pela conexão externa.
- A importação de Alimentos paga produto e frete e gera entrega física.
- No escopo inicial, Combustível pode ser importado já refinado/pronto pela conexão externa e distribuído fisicamente aos postos.
- Refino/produção local de combustível pode ser aprofundado depois, sem ser necessário para o primeiro fluxo funcional da cidade.


### Visão agregada de materiais

- A interface deve mostrar a oferta de materiais de construção de forma agregada em nível da cidade.
- Para cada material, o jogador deve conseguir entender pelo menos o total disponível localmente, o que está reservado, o que está em trânsito e quanto precisará ser importado para uma nova obra.
- A agregação é apenas de interface e decisão; localização física, transporte, capacidade de armazenamento e congestionamento continuam simulados.
