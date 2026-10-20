# Economia

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Histórico das decisões em [docs/decisoes/economia.md](../decisoes/economia.md).

## Em vigor no jogo (V1)

### Patrimônio e investimento residencial dos cidadãos

- Cidadãos podem acumular dinheiro e patrimônio ao longo da vida.

- A compra só pode ocorrer com dinheiro real disponível do comprador; a decisão deve considerar fatores econômicos concretos, como preço, demanda e expectativa de ocupação/renda.

- A fórmula exata de decisão e os limites de investimento continuam em calibração/exploração.

- No primeiro modelo, cada imóvel residencial privadamente possuído tem um único cidadão como proprietário registrado; um imóvel pode ficar temporariamente sem proprietário quando entrar em estado de patrimônio não reclamado.

- Um casal/família pode somar recursos para viabilizar a compra, mas a propriedade fica registrada em nome de um único SIM; copropriedade fica fora do primeiro modelo.

- Ao entrar na simulação, cidadãos podem iniciar com um saldo monetário explícito conforme regra de geração/migração; o valor e sua distribuição devem ser configuráveis e auditáveis, sem criação invisível de riqueza.

#### Sem empréstimos privados no modelo inicial

- SIMs e empresas compram bens, imóveis, instalações e insumos com dinheiro realmente disponível, sem criar agora bancos, financiamentos imobiliários, empréstimos pessoais ou crédito empresarial.

- Esta regra não altera os empréstimos municipais entre Caixa da Cidade e Reserva Global nem permite saldo privado negativo fictício.

- Se compras grandes ficarem inviáveis na economia, avaliar em testes antes de aprovar financiamento futuro.

### Oferta monetária fixa e Reserva Global

- A simulação usa uma oferta monetária global fixa: o dinheiro não é criado nem destruído pelos fluxos normais; ele apenas muda de titular.

- O total monetário global é a soma do Caixa da Cidade + carteiras dos SIMs + caixas das empresas + Reserva Global e deve permanecer constante/auditável durante a simulação.

- A Reserva Global é um saldo técnico por trás das cenas, não controlado nem normalmente exibido ao jogador.

- Ela representa a parcela do dinheiro global que não está naquele momento com os agentes econômicos locais.

- Todo lançamento na Reserva Global deve ser rastreável por origem, destino, motivo e entidade/evento relacionado.

- Quando uma família ou empresa entra a partir do mundo exterior, seu capital inicial sai da Reserva Global; quando um fluxo econômico sai da cidade para uma contraparte externa não modelada, o valor retorna à Reserva Global.

- Quando um SIM morre sem herdeiro elegível ou uma empresa é encerrada definitivamente sem outro titular econômico definido para o saldo remanescente, esse dinheiro retorna à Reserva Global com a origem registrada.

- No caso do SIM devedor, eventual aluguel atrasado devido a credor real é abatido do dinheiro individual disponível antes de determinar o saldo remanescente.

- Patrimônio sem sucessor continua podendo permanecer explicitamente sem proprietário; a Reserva Global não se torna proprietária do ativo físico.

- Quando um ativo sem proprietário é vendido, o pagamento entra na Reserva Global com a origem registrada.

- A Reserva Global não substitui destinatários reais: se existe um SIM, empresa, proprietário, fornecedor ou outro recebedor econômico concreto, o dinheiro deve ir para esse agente.

- Em importações, o pagador real transfere dinheiro para a Reserva Global; em exportações, o pagamento sai da Reserva Global e vai para o vendedor/recebedor econômico real.

- Esses fluxos não criam nem destroem moeda.

- O valor inicial total, sua distribuição inicial e as faixas de capital dadas a novos migrantes/empresas são parâmetros de calibração e devem ser reproduzíveis pela seed.

### Empresas e economia

- Empresas são entidades reais da simulação.

#### Autonomia econômica das entidades

- SIMs e empresas são agentes distintos, com decisões próprias, ainda que usem critérios econômicos e oportunidades concretas em comum.

- A empresa não é apenas a construção que ocupa, não age automaticamente como extensão da ordem do jogador e decide por conta própria se aceita oportunidades reais de aquisição e operação, se continua sua atividade em um endereço proposto ou se encerra uma operação, conforme seu caixa, demanda, custos, trabalhadores, insumos, acesso e logística.

- Isso não autoriza a empresa a construir ou mover fisicamente prédios por iniciativa própria fora das ações e regras de construção decididas para o jogador.

- Um SIM decide sobre suas próprias escolhas de moradia, emprego, consumo e patrimônio nas regras correspondentes; uma decisão da empresa não substitui a decisão do SIM, e vice-versa.

- Não criar uma camada adicional de IA por agente nem verificações incessantes: reutilizar as decisões normais e eventos que geram oportunidades.

- Autonomia não torna os agentes imunes a intervenções urbanas aprovadas, insolvência ou escassez.

- No escopo atual, quando o jogador conclui um prédio de atividade econômica privada, como fazenda, mercado, posto ou fábrica, o prédio pode permanecer vazio/procurando operador até que uma empresa local ou vinda da conexão exterior decida adquiri-lo e operá-lo.

- Empresas têm funcionários reais.

- Cada emprego corresponde a uma vaga real dentro de uma empresa ou serviço.

- Empresas privadas têm caixa monetário real e individual, mantido pela simulação e auditável.

- Estoque e/ou produção devem existir como parte do modelo econômico das empresas.

#### Armazenamento privado no próprio estabelecimento

- No primeiro modelo, cada loja, fazenda, fábrica e instalação econômica privada utiliza sua capacidade de armazenamento físico já associada ao estabelecimento, com limites reais de estoque, mercadorias, entregas e operação.

- Não criar como novo tipo de construção armazém privado separado, depósito terceirizado ou rede logística de armazéns entre empresas nesta etapa.

- Isto não elimina o Centro de Materiais de Construção definido, as áreas físicas de carga/descarga nem os estoques e veículos reais.

- Estocagem externa independente pode ser discutida futuramente se trouxer ganho proporcional.

#### Produção adaptativa com estoque de segurança (modelo inicial)

- Fazendas, indústrias e demais empresas produtoras de mercadorias devem ajustar gradualmente e automaticamente a quantidade produzida conforme pedidos/vendas reais, estoque disponível e capacidade produtiva real, mantendo uma reserva operacional simples para atender novas compras e evitar paradas/reinícios a cada pedido.

- Estoque persistentemente alto e vendas insuficientes reduzem a produção planejada; maior saída real e necessidade de reposição podem elevá-la até o limite da capacidade e dos recursos disponíveis.

- Produzir exige os insumos, trabalhadores e condições operacionais aplicáveis; não criar mercadorias, demanda, clientes ou dinheiro artificialmente.

- O jogador não define metas ou ordens de produção por empresa, mas deve enxergar oferta, vendas, estoque, utilização de capacidade e causas de ociosidade quando relevantes, aproveitando os alertas de demanda insuficiente já definidos.

- Preservar o equilíbrio entre gameplay e performance: no primeiro modelo integrado, a regra de adaptação deve ser simples, previsível e de baixo custo, sem previsão sofisticada, otimização por agente a cada instante, recálculos desnecessários ou administração manual.

- Ritmo/periodicidade de ajuste, tamanho da reserva, tratamento de pedidos pendentes, capacidade efetiva e consequências sobre emprego/custos ficam para calibração/protótipo, sem adicionar antecipadamente subsistemas próprios.

- Excedentes e exportações continuam sob as regras existentes, sem saída ou comprador garantido.

- O consumo é modelado inicialmente por categorias de produtos, não por SKU individual.

#### Preços de mercadorias — modelo híbrido inicial

- Cada recurso/categoria física relevante possui um preço de referência compartilhado em nível de cidade, como base econômica legível e comparável; isso não cria estoque global nem define um preço único obrigatório para todas as transações.

- Para bens de consumo vendidos em comércios, cada estabelecimento pode ter seu próprio preço de venda por categoria, inicialmente calculado de modo simples e previsível a partir de custos reais de aquisição, transporte/frete e margem de comercialização, com diferenças locais possíveis mesmo para a mesma categoria.

- O preço efetivamente pago é o da transação com a empresa real e movimenta dinheiro existente entre os agentes.

- O modelo deve explicar ao jogador preço de referência e principais motivos de diferenças locais, preservando a escolha de comércio por preço, distância e disponibilidade.

- **Em aberto:** No primeiro modelo integrado, não implementar negociação complexa nem flutuação sofisticada e contínua de preços por demanda/escassez, deixando a dinâmica futura do preço de referência, margens e ajustes de mercado em exploração; fórmula, parâmetros e periodicidade simples de atualização ficam para calibração/protótipo.

- Os preços externos de importação continuam estáveis, como já decidido.

- Os materiais de construção compartilham o catálogo/referência das categorias físicas, mas mantêm suas regras próprias de aquisição, estoque, custos e transporte para obras; não aplicar automaticamente regras de varejo a materiais de obra.

- A referência de preços é apenas informação econômica derivada/configurada, nunca um comerciante, estoque ou fonte/sumidouro de dinheiro.

#### Compras de bens de consumo pelos cidadãos são presenciais no primeiro modelo

- O SIM se desloca fisicamente até um comércio acessível, realiza a compra de categorias de produtos efetivamente disponíveis no estoque com dinheiro real, e segue seu deslocamento de volta ou para o próximo destino, sem teletransporte.

- A compra reduz o estoque do estabelecimento e transfere o pagamento ao caixa da empresa operadora; ausência de estoque ou dinheiro impede a aquisição correspondente.

- As viagens geram movimento real de pedestres/veículos e demanda local, inclusive fora da câmera.

- Não substituir a ida ao comércio por uma transferência econômica abstrata que dispense a viagem.

- A escolha do comércio é automática e considera preço, distância/acessibilidade real e disponibilidade de estoque.

- Se o estabelecimento escolhido não puder atender a compra (por exemplo, falta de produto), o SIM pode buscar outro comércio acessível com o produto e recursos compatíveis, com os deslocamentos correspondentes; não há estabelecimento habitual obrigatório nem reposição garantida.

- Se nenhuma opção viável atender à demanda, a compra não ocorre e a necessidade continua não atendida, com as consequências já previstas.

- A decisão não cria mercadorias, dinheiro nem teletransporte.

- A ponderação dos fatores, o limite de tentativas e o custo de busca devem ser calibrados/prototipados para evitar varreduras contínuas de toda a cidade por cada SIM.

- A frequência e o agrupamento de compras continuam para calibração, sem exigir compras por SKU ou uma viagem para cada item/categoria.

#### Estoque doméstico real por categoria

- Bens de consumo destinados ao domicílio, começando por Alimentos, são comprados em quantidades para vários dias, trazidos fisicamente da loja para casa e registrados em quantidades agregadas por categoria no estoque da família/domicílio, sem inventário individual por SIM, unidades de SKU ou simulação de cada embalagem.

- As necessidades dos moradores consomem gradualmente essas quantidades ao longo do tempo; o consumo não faz novo pagamento, pois o dinheiro já foi transferido na compra real.

- Quando o estoque fica baixo, novas compras presenciais podem ser planejadas automaticamente, respeitando preço, disponibilidade, deslocamentos e recursos reais.

- Falta de estoque nos comércios não elimina imediatamente a reserva que a família já tem em casa; se o estoque doméstico se esgotar e não houver reposição viável, a necessidade correspondente deixa de ser atendida, com consequências reais.

- Volume comprado, ritmo de consumo, gatilhos de reposição, transporte dos bens durante a viagem e tratamento dos itens em mudança/perda da moradia ficam para calibração/protótipos, sem exigir microgestão do jogador.

- Compras reais reduzem estoque real, movimentam dinheiro real e geram necessidade de reposição/logística.

#### Alimentos não perecíveis no modelo inicial

- Estoques de Alimentos existentes em fazendas, comércios, restaurantes e domicílios não perdem quantidade por prazo de validade, apodrecimento ou descarte automático.

- As quantidades só mudam por operações já definidas, como produção, entrega, compra e consumo; perecibilidade pode ser avaliada futuramente, sem novo sistema de refrigeração ou rastreamento por lote agora.


- Fazendas e agricultura produzem diretamente a categoria Alimentos no escopo inicial.

#### Reposição comercial e escolha de fornecedores — preferência local moderada

- Comércios que precisam reabastecer categorias de estoque escolhem automaticamente entre produtores/fornecedores locais reais e importação externa, considerando preço de aquisição, frete/custo total, disponibilidade e prazo de entrega.

- Quando alternativas são economicamente próximas e capazes de atender ao pedido, preferem o fornecedor local, sem obrigação de comprar localmente independentemente do custo; se o fornecedor externo oferecer vantagem significativa ou não existir oferta local viável, a importação pode ser escolhida mesmo havendo alguma oferta na cidade.

- O pedido depende de dinheiro real da empresa compradora, quantidade física disponível e entrega por logística real; pagamentos locais vão ao fornecedor real e pagamentos de importação seguem a Reserva Global.

- O jogador não seleciona manualmente fornecedor ou pedido.

- Não criar proteção local absoluta, estoque garantido nem buscas contínuas por todos os fornecedores: gatilhos de reposição, limites de busca, ponderação de custo/prazo e limiar de diferença pequena ou vantagem significativa ficam para calibração/protótipo, com decisões diagnosticáveis.

- Esta política é para abastecimento operacional de comércio; materiais de construção para obras conservam sua política específica de disponibilidade/reserva e importação acionada por obra.

- Mercadorias físicas modeladas como estoque podem ser importadas pela conexão externa quando a oferta local for insuficiente ou inexistente, ou quando a comparação econômica da reposição comercial justificar a alternativa externa conforme a regra acima.

- No escopo inicial, os preços externos de importação permanecem estáveis.

#### Papéis econômicos das empresas

- Separar conceitualmente empresas produtoras (criam mercadorias físicas mediante insumos, trabalho e capacidade), empresas comerciais/revendedoras (adquirem e vendem mercadorias existentes, com estoque físico e transações reais) e empresas prestadoras de serviços (atendem pessoas ou outras demandas reais e recebem pagamento pela prestação, sem inventar mercadoria armazenável para cada serviço).

- São papéis operacionais, não identidades jurídicas exclusivas: uma atividade mista pode produzir, vender e/ou prestar atendimento quando as respectivas regras forem aprovadas.

- Todas continuam empresas autônomas com caixa, vagas, custos, tributos, propriedade e falência conforme a SPEC, sem criar três engines ou moedas separadas.

- A definição não adiciona por si novos negócios nem SKUs.

#### Categoria urbana escolhida pelo jogador; atividade escolhida pela empresa

- Ao posicionar uma construção econômica privada, o jogador escolhe uma categoria urbana ampla compatível com o edifício, incluindo comércio e serviços numa mesma categoria comercial (sem exigir categorias urbanas mutuamente exclusivas de revenda versus atendimento), sem selecionar a empresa operadora nem obrigar uma atividade específica.

- Na aquisição de imóvel disponível, a empresa interessada escolhe autonomamente a atividade específica compatível com categoria, estrutura, acessos e regras econômicas aprovadas, considerando demanda de clientes reais, concorrentes próximos, capacidade já instalada, localização, custos, empregados e perspectiva financeira.

- A proximidade de dois negócios idênticos é permitida: concorrência pode reduzir clientela e lucro e levar a vacância ou falência; não introduzir regra de distância mínima, alternância estética ou proibição de ramo duplicado.

- Evitar concentração não intencional por avaliação econômica, sem gerar tipos aleatórios, demanda fictícia ou forçar diversidade.

- Se nenhuma atividade parecer viável, o imóvel pode continuar desocupado/procurando empresa.

- Compatibilidade comercial ampla: numa estrutura comercial compatível, cafeteria, salão de beleza, loja e demais ramos de comércio/serviços podem se suceder, seja por nova empresa compradora ou pela empresa proprietária atual, sem que o jogador precise mudar o uso urbano por serem revenda ou prestação de serviço.

- Isso não autoriza qualquer atividade em qualquer prédio: porte, instalações, capacidade, acesso, estoques e obras/adaptações físicas reais continuam restringindo a operação; posto de combustível ou hotel podem exigir instalações especializadas.

- Agricultura e indústria não passam automaticamente a ser usos comerciais.

- Quais subcategorias são expostas na ferramenta e detalhes de ponderação ficam para calibração e UI; não instituir escolha manual de cada ocupante pelo jogador.

### Catálogo inicial de atividades privadas

- Na V1, o catálogo econômico contém mercearia pequena, fazenda pequena e posto de combustível. A **oficina mecânica** (com guincho próprio) também faz parte do catálogo, com regras em [transporte e circulação](transporte-circulacao.md#quebras-guincho-local-e-oficina). O catálogo completo e as demais atividades ficam na visão aprovada.

#### Mercado e mercearia

- Ambos podem comercializar as mesmas categorias físicas de produtos já modeladas, sem catálogo/SKU exclusivo obrigatório; diferenciam-se por porte físico compatível, capacidade de atendimento e armazenamento, custos reais de operação e eventuais limites de escala.

- Se ambos oferecerem a mesma mercadoria e tiverem estoque, clientes consideram preço, deslocamento, disponibilidade e capacidade conforme as regras existentes.

- Não impor que um só venda determinada categoria, nem criar produtos só para diferenciá-los.

- Dimensões, taxas e valores monetários ficam para calibração dos modelos-base e das operações.

#### Nova procura de estabelecimento quando compra não pode ocorrer

- Quando o SIM chegar para comprar mercadoria ou realizar compra presencial e não conseguir concluir por fechamento, falta real de estoque, ausência de atendimento/capacidade ou preço acima de seus recursos efetivos, pode buscar automaticamente outra loja/estabelecimento acessível e viável, respeitando horário, preço, estoque, rede de circulação, disponibilidade de tempo e deslocamento físico.

- Se não existir alternativa razoável, termina a tentativa sem compra, entrega, gasto, reposição ou chegada fictícia, e a necessidade continua conforme suas regras existentes.

- Reaproveitar a busca eficiente por candidatos reais, limitar tentativas/rotas redundantes sem criar uma busca global por cliente a cada frame, e não inventar importação instantânea pelo vendedor.

#### Famílias visuais iniciais

- Produzir e reutilizar poucas famílias de assets 3D, com lojas pequenas, estabelecimentos comerciais de porte médio e modelos específicos somente quando o funcionamento físico exigir, como posto de combustível (acesso e abastecimento de veículos) ou hotel (ocupação e hospedagem).

- Reutilizar volumetria e componentes de fachadas, placas, cores e acabamentos entre negócios compatíveis.

- O tamanho/capacidade e o acesso precisam corresponder à construção real; não usar um prédio único indiferente à atividade, nem exigir dez assets exclusivos.

- A identidade da empresa/ramo é independente da representação visual.

- Quantidade exata de modelos e variantes, dimensão dos lotes, visual de cada ramo e processo de produção de arte ficam para desenho/validação, sem limitar cada família a um único asset.

#### Adesão às regras de contratação e concorrência

- Construir um imóvel não escolhe nem cria automaticamente uma empresa específica, seus clientes ou sua operação.

- A empresa adquirente escolhe um desses ramos apenas quando imóvel, categoria escolhida pelo jogador, estrutura, localização, custo e demanda forem compatíveis.

- Negócios idênticos podem coexistir e disputar clientes; baixa viabilidade pode manter imóveis vazios ou levar a prejuízo/falência.

#### Funcionamento comercial com horários simples por tipo

- No modelo inicial, cada tipo de negócio dispõe de uma janela habitual simples de abertura e fechamento adequada à sua atividade; a operação efetiva depende de trabalhadores reais alocados aos turnos, acesso, condições do estabelecimento e recursos/estoques quando aplicáveis.

- Estar fora da janela ou sem pessoal disponível significa não atender clientes naquele período, sem venda ou receita simuladas.

- Turnos múltiplos continuam permitidos conforme a regra geral, mas não exigem escala individual administrada pelo jogador, nem otimizador complexo de horários por empresa.

- Horários exatos, diferenças entre tipos e tratamento de operações com continuidade necessária (como hospedagem) são ajustes a especificar/calibrar; não importar horários da vida real como obrigação rígida e não estender turnos ficticiamente.

#### Dimensionamento automático de trabalhadores com teto físico

- Cada empresa contrata ou reduz gradualmente sua equipe conforme clientes/pedidos reais, utilização, custos, caixa/salários e oportunidades viáveis de trabalho, sem solicitar ao jogador a quantidade de funcionários.

- A estrutura física, seu porte, instalações e capacidade operacional impõem um limite máximo real de postos/trabalhadores simultâneos; empresa pequena não pode contratar pessoas ilimitadas, mesmo com alta demanda ou muito capital.

- Quando houver turnos, o limite por turno/presença e o número total de vínculos devem corresponder à capacidade e cobertura efetivamente possível, sem usar alternância de turnos para multiplicar produção/atendimento além da capacidade real.

- Vagas precisam ser existentes e financiáveis, com SIMs contratados reais; a equipe pode permanecer abaixo do teto por escassez de interessados/caixa.

- Ajustes são periódicos ou por mudanças relevantes, com reação gradual para evitar contratar/demitir a cada oscilação e evitar varreduras constantes por empresa ou SIM.

- Valores exatos do limite por porte, ritmos e gatilhos de ajuste ficam para calibração.

### Aquisição e operação de comércio e indústria

- Prédios privados de comércio e indústria não recebem automaticamente uma empresa ao serem concluídos.

- O ativo concluído entra em procura por operador/adquirente; baixa oportunidade econômica pode mantê-lo vazio por mais tempo.

- Empresas locais existentes e novas empresas vindas da conexão exterior podem disputar esses ativos conforme fatores concretos como demanda/clientes, trabalhadores disponíveis, insumos, logística, localização e custos.

#### Aquisição e distinção entre propriedade e operação

- A empresa que compra o imóvel comercial/industrial torna-se sua proprietária e, na operação direta inicial, pode ser sua própria operadora.

- O preço pago pela primeira aquisição vai para a Reserva Global, não retorna ao Caixa da Cidade.

- A procura externa não é infinita e não pode garantir venda/lucro; deve depender de condições econômicas reais e diagnosticáveis.

- O jogador não escolhe manualmente qual empresa assume o prédio nem negocia propostas individuais.

- No primeiro modelo, a empresa é uma entidade econômica independente sem proprietário humano/SIM modelado.

- A camada de sócios/acionistas fica fora do escopo inicial.

### Ciclo inicial de um prédio econômico privado

- A regra vale tanto para comércio quanto para indústria privados.

- O jogador financia e constrói o prédio com Caixa da Cidade + materiais físicos.

- Quando a obra termina, o prédio pode permanecer vazio em estado de procurando empresa.

- No início da cidade, novas empresas candidatas podem vir da conexão exterior com capital próprio explícito e finito.

- A empresa candidata avalia a oportunidade econômica do prédio; a lógica segue o mesmo princípio causal da demanda residencial, mas com fatores próprios de negócio, como clientes, concorrência, trabalhadores, insumos, logística, localização e custos.

- Se uma empresa adquirir o ativo, o pagamento vai para a Reserva Global; a empresa passa a possuir e operar o estabelecimento.

- Após a aquisição, a empresa contrata SIMs reais, compra insumos/estoque, vende/produz, paga salários e impostos e mantém seu próprio caixa.

- Empresas já presentes na cidade podem adquirir outros estabelecimentos usando o próprio caixa acumulado, conforme a regra de expansão orgânica nesta seção.

- A criação de uma empresa nova puramente local, sem origem externa nem empresa anterior, fica fora do primeiro modelo até existir uma fonte concreta de capital.

### Finanças privadas das empresas

- Cada empresa privada possui um saldo monetário real.

- O lucro permanece no próprio caixa da empresa e pode financiar operação, absorção de prejuízos e expansão futura; não existem dividendos ou retiradas para um proprietário humano no primeiro modelo.

- Receitas e despesas da empresa devem ser lançadas a partir de fluxos econômicos reais da simulação, incluindo vendas, salários, insumos, frete, impostos e outros custos definidos.

- O estado econômico da empresa e uma eventual falência devem ser derivados desses fluxos e do caixa real, não de um indicador oculto independente.

- O jogador não precisa administrar transferências bancárias, capital de giro ou pagamentos individuais manualmente.

- A leitura normal pode resumir a situação da empresa; uma visão detalhada deve permitir inspecionar caixa, receitas, despesas e causas de deterioração.

- Não haverá resgates, crédito ou dinheiro invisível para impedir falência.

- Se crédito, subsídio ou recapitalização forem adicionados futuramente, deverão ser sistemas explícitos e diagnosticáveis.

- A construção física de um prédio privado é financiada pelo Caixa da Cidade; após concluído, o ativo pode ser adquirido por uma empresa com capital próprio conforme as regras de aquisição.

### Falência e encerramento de empresas

- Empresas podem falir quando não conseguirem sustentar suas obrigações econômicas; o jogador deve conseguir identificar as receitas, despesas e eventos concretos que levaram à deterioração e à falência.

- O jogo não precisa simular legislação ou processo jurídico detalhado de falência.

- Para gameplay, falência significa que uma empresa não consegue sustentar sua operação por tempo suficiente e encerra definitivamente como empresa; ela não permanece indefinidamente tentando recomeçar.

- A deterioração deve vir de causas reais e auditáveis, como falta de clientes/receita, custos de insumos, salários, frete, impostos ou interrupções de abastecimento.

- Antes do fechamento, a empresa deve permanecer em estado de risco por um período calibrável e mostrar ao jogador a principal causa real do problema.

- O limiar e o tempo exatos de encerramento são parâmetros de balanceamento e não exigem microgerenciamento do jogador.

- O encerramento pode ter uma etapa técnica curta de liquidação apenas para dar destino causal aos ativos e ao dinheiro; ela não é uma mecânica de gestão para o jogador.

- Enquanto essa liquidação existir, imóveis ainda pertencentes à empresa podem ser colocados automaticamente no mercado; o pagamento de uma venda entra no caixa da empresa em liquidação.

- Ao concluir a curta liquidação de acordo com a regra proporcional abaixo, eventual saldo monetário que reste após a quitação das obrigações viáveis volta para a Reserva Global, e a empresa é removida.

- Ativos não vendidos podem ficar sem proprietário conforme a regra existente, sem impedir eternamente o encerramento.

#### Liquidação empresarial proporcional, simples e automática

- Após a falência terminal, a empresa interrompe operações e utiliza o dinheiro real que ainda possuir, incluindo valores efetivamente recebidos de vendas de imóveis/ativos durante a curta etapa de liquidação já prevista, para quitar dívidas efetivamente registradas e ainda devidas a credores reais (por exemplo, salários de SIMs, fornecedores e Caixa da Cidade por impostos, água ou energia).

- Se o valor disponível cobrir todas as dívidas, paga-se integralmente a cada credor.

- Se for insuficiente, cada credor recebe automaticamente a mesma fração proporcional de seu crédito restante, limitada ao dinheiro real disponível; por exemplo, caixa de R$ 200 para dívidas de R$ 500 em salários, R$ 300 em fornecedores e R$ 200 ao município paga respectivamente R$ 100, R$ 60 e R$ 40.

- A parcela impagável de cada obrigação é encerrada como crédito perdido na extinção definitiva da empresa, sem cobrar herdeiros, funcionários, famílias, empresas aleatórias ou Reserva Global, sem criar nem destruir moeda: baixar uma dívida contábil não é transferência de caixa e registrar crédito devido não constitui receita recebida.

- Se houver saldo de dinheiro após todas as dívidas serem pagas, o valor sem outro titular econômico vai para a Reserva Global, com origem auditável.

- Imóveis/ativos físicos não vendidos até o encerramento podem permanecer explicitamente sem proprietário e ser negociados mais tarde conforme regra patrimonial definida; nesse caso, o pagamento posterior vai para a Reserva Global, não reabre a falência nem obrigações já encerradas.

- Uma empresa que continua existindo e fecha apenas um estabelecimento não passa por esta liquidação.

- O fluxo é automático e pode ser explicado ao jogador por perdas reais a funcionários, fornecedores e arrecadação, sem prioridade especial a salários, ordem cronológica de credores, processo judicial, negociações individuais, empréstimo-resgate ou micromanagement.

- Contabilizar valores efetivamente recebidos/pagos sem duplicar caixa, estoque ou bens; arredondamentos de centavos, duração técnica da liquidação e condições de venda seguem calibração, sem autorizar venda fictícia ou demorar indefinidamente para encerrar a empresa.

- Se um ativo ficar sem titular após o encerramento, ele pode permanecer explicitamente sem proprietário e continuar disponível no mercado; uma venda futura envia o valor à Reserva Global com a origem registrada.

- Fechar um estabelecimento isolado de uma empresa que continua saudável em outros locais não encerra a empresa inteira; essa distinção existe sem exigir uma simulação jurídica de falência.

### Propriedade, demanda e vulnerabilidade social

- Lotes não precisam ter proprietário individual.

- Toda construção é colocada pelo jogador.

- A cidade deve calcular e expor demanda por categoria de atividade, em vez de uma única barra genérica de comércio/indústria.

- A demanda deve ser principalmente local, refletindo a clientela potencial acessível àquele estabelecimento e àquela área da cidade.

- Os fatores conceituais principais da demanda são população/clientes potenciais, capacidade já existente, acesso/tempo de deslocamento e concorrência entre estabelecimentos equivalentes ou substitutos.

#### Demanda é informação de gestão, não uma trava de construção

- O jogador pode construir mesmo quando a demanda é baixa.

- A interface deve alertar claramente quando uma nova atividade econômica estiver entrando em uma situação de baixa demanda ou alto risco de pouca clientela.

#### Alerta de falta de compradores durante a operação

- Fazendas, indústrias e comércios devem permitir ao jogador perceber quando vendas/pedidos reais ficam insuficientes em relação à oferta ou produção e a situação persiste com consequências relevantes, como estoque se acumulando, receita baixa ou risco econômico.

- O sinal deve se apoiar em quantidades efetivamente vendidas, produzidas e armazenadas, inclusive vendas externas quando existirem, não somente na contagem de compradores: um único comprador pode adquirir grande volume.

- Distinguir demanda insuficiente de falha de entrega (há pedidos, mas caminhões ou capacidade logística impedem a saída dos produtos), além de outras causas reais, em vez de emitir alerta incorreto de poucos compradores.

- Mostrar diagnóstico simples, com detalhes ao inspecionar a atividade e sugestões de causas/ações urbanas cabíveis; agregar avisos para evitar excesso de notificações ou alertas por oscilações passageiras.

- Limiares, duração e apresentação ficam para calibração.

- Este alerta diagnostica a situação econômica, mas não é uma ordem manual de produção: o ajuste automático da atividade segue a política de produção adaptativa com estoque de segurança já definida em *Empresas e economia*; seus parâmetros continuam para calibração.

- Na colocação e na leitura normal, a demanda pode ser resumida em estados simples como boa / média / baixa; o jogador deve poder abrir os fatores detalhados que produziram esse resultado.

- Múltiplos estabelecimentos equivalentes disputando a mesma clientela, como vários mercados concentrados na mesma área, podem reduzir a oportunidade econômica para novos estabelecimentos e aumentar o risco de prejuízo.

- A fórmula exata, os pesos e os limiares continuam sendo parâmetros de calibração; não usar uma barra opaca sem decomposição causal.

- Oferta e demanda devem influenciar o valor de mercado dos imóveis, com reflexos indiretos no aluguel residencial proporcional a esse valor, sem exigir um modelo excessivamente complexo.

- No primeiro modelo, não há multiplicador adicional de procura sobre o aluguel e o preço de venda é igual ao valor de mercado calculado, sem negociação de desconto ou acréscimo.

### Financiamento de moradia

- O jogador continua decidindo diretamente onde as residências serão construídas, e a obra consome Caixa da Cidade + materiais físicos.

- Uma residência privada recém-construída pode ficar sem proprietário até a primeira aquisição.

- Também vale para residência privada substituta construída por realocação assistida: concluída a obra, não herda dono ou moradores do imóvel anterior, mas oferece primeiro ao SIM proprietário original indenizado uma oportunidade de aquisição pelo valor de mercado novo, quando elegível e com recursos reais; se não comprar, segue para oferta residencial normal.

- Na primeira aquisição, o comprador paga com dinheiro real; esse pagamento vai para a Reserva Global, não retorna ao Caixa da Cidade.

- Depois que existe um proprietário privado real, aluguel vai ao proprietário e revendas transferem dinheiro entre comprador e proprietário normalmente.

- A cidade recupera financeiramente o custo de desenvolver moradia principalmente de forma indireta, por impostos e demais receitas públicas, e não por revenda do ativo.

### Agricultura

- A V1 oferece fazenda pequena com área cultivável predefinida por porte; os portes médio e grande permanecem na visão aprovada.

#### Produção agrícola determinada também pela área cultivável

- A capacidade produtiva real de uma fazenda considera a área física cultivável/ocupada para cultivo, trabalhadores disponíveis e capacidade operacional efetiva, além de recursos/tempo pertinentes.

- Fazendas maiores podem produzir mais, mas somente se houver pessoal, meios produtivos e condições reais; a área não gera Alimentos automaticamente nem ignora demanda/estoque adaptativos já definidos.

- Não criar sistema separado de fertilidade regional, plantio célula a célula, escolha manual de culturas ou manejo agrícola individual por esta decisão.

- Relação área/capacidade/produção é calibração.

- Fazendas ocupam terreno físico real.

- Fazendas empregam trabalhadores reais da população simulada.

- A produção agrícola é transportada fisicamente por veículos de carga.

#### Fazendas sem sazonalidade por estações no modelo inicial

- A produção agrícola é possível ao longo do ano quando existirem área, trabalhadores, insumos, tempo, capacidade e demanda reais.

- Não aplicar variações automáticas de produtividade por estação do ano nem exigir plantio e colheita em épocas específicas.

- Estoques e produção ainda variam conforme a economia e o funcionamento efetivo.

- Melhoria futura desejada: sazonalidade simples, se mostrar ganho concreto de gameplay, sem já aprovar culturas ou calendários individuais.

### Estado inicial da cidade

- A cidade começa essencialmente vazia, sem tecido urbano pré-construído.

- O mapa mantém apenas os elementos naturais já definidos e a conexão externa; nenhum edifício municipal, incluindo o Pátio Municipal de Obras, começa construído.

### Bootstrap sistêmico de moradores e empresas

#### Entrada por oportunidades concretas, sem ordem obrigatória entre moradores e empresas

- No início da cidade, famílias e empresas candidatas vindas do exterior podem considerar oportunidades economicamente plausíveis que ainda não se realizaram por completo.

- Empresas podem adquirir prédios econômicos concluídos mesmo antes de existir clientela ou mão de obra local suficiente, avaliando condições observáveis como moradias disponíveis, acessibilidade, infraestrutura, logística, custos e possibilidade de atrair trabalhadores e compradores.

- Famílias podem migrar mesmo sem emprego previamente ocupado/garantido, desde que exista uma possibilidade real de moradia conforme as regras de aquisição e que seus recursos monetários explícitos permitam assumir o risco; vagas efetivamente ofertadas, serviços, custos e condições da cidade influenciam a escolha.

#### Potencial não equivale a demanda realizada

- Residências vazias não contam como clientes, vagas sem ocupantes não contam como trabalhadores e nenhuma expectativa autoriza vendas, salários, produção, contratação, migração ou abastecimento fictícios.

- A chegada de famílias e empresas continua voluntária, condicionada à avaliação econômica, e pode falhar; não existe ocupação, crescimento, rentabilidade ou resgate garantido.

- Os capitais externos continuam saindo da Reserva Global, respeitando saldo disponível e oferta monetária fixa; compra de imóveis, estoque, insumos, água/energia, transporte e demais operações seguem as regras físicas e monetárias normais.

- O jogador não aprova migrações ou aquisições uma a uma; a simulação deve distinguir oportunidade potencial de condições efetivamente atendidas e explicar por que uma entrada ocorreu, não ocorreu ou fracassou.

- Pesos, periodicidade de avaliação e reservas financeiras mínimas ficam para calibração, sem sistema de promessas ou contratos prévios.

## Visão aprovada (ainda não implementada)

### Patrimônio e investimento residencial dos cidadãos

#### Rateio entre créditos municipais na morte

- Respeitada a precedência já decidida de quitar aluguel atrasado do falecido com saldo disponível, eventual dinheiro remanescente insuficiente para múltiplas dívidas pessoais municipais exigíveis de imposto imobiliário, água e energia é distribuído proporcionalmente aos créditos efetivamente devidos, com transferência real ao Caixa e baixa do saldo de cada obrigação.

- O valor que não couber é encerrado pela regra sucessória existente, sem privilégios arbitrários entre tributos, dinheiro inventado ou cobrança aos herdeiros.

#### Sem adulto elegível para assumir locação residencial

- Se faltar o locatário responsável e não houver outro adulto da família apto a assumir aluguéis futuros, a simulação procura um responsável adulto real e elegível no fluxo familiar/de cuidado existente; se não houver, a locação não continua indefinidamente sem pagador e é encerrada, preservando o cuidado, acolhimento e acesso à moradia possíveis para os menores, conforme regras definidas.

- Não atribuir dívida ou contrato automaticamente a criança, nem presumir desocupação instantânea sem transição física.

#### Titular do aluguel após separação

- Se o locatário responsável sair da residência depois da separação e outro adulto elegível permanecer com a família, a simulação atribui aluguéis futuros a esse ocupante real, mantendo contrato/ocupação viáveis.

- Dívidas pessoais anteriores continuam no SIM que efetivamente as contraiu; não dividi-las automaticamente pelo casal, perdoá-las, duplicar contratos ou impor ao ex-morador novos pagamentos perpetuamente.

- Sem adulto elegível, vale a regra específica acima.

#### Dívidas municipais pessoais na morte do SIM

- Quitar dívidas reais pendentes de imposto imobiliário e contas de água/energia com o saldo monetário efetivamente disponível do falecido, até onde ele alcançar, antes da distribuição da herança monetária.

- O saldo dessas obrigações que não puder ser pago encerra-se contabilmente com a morte, sem transferir dívida pessoal aos herdeiros ou familiares, exigir venda de bens físicos, criar receitas municipais não recebidas ou destruir dinheiro.

- Preservar a precedência de aluguel atrasado definida no uso do saldo do falecido; quando houver múltiplos créditos municipais sem dinheiro suficiente, aplica-se o rateio proporcional desta seção, sem pagamento fictício.

#### Herdeiros elegíveis para SIM falecido

- Na morte de um SIM, o patrimônio monetário ou físico remanescente, após obrigações com tratamento explicitamente aprovado, pode passar a herdeiro elegível real.

- A primeira classe é cônjuge/companheiro e filhos; na ausência de ambos, também podem herdar pais e irmãos vivos/elegíveis.

- Não exigir busca por família estendida ou genealogia sem limite no primeiro modelo.

- Partilha automática simplificada entre múltiplos herdeiros: entre os herdeiros elegíveis da classe aplicável, o dinheiro remanescente é dividido em partes iguais por SIM, em transferências monetárias reais.

- Bens físicos de propriedade individual, incluindo casas, apartamentos, edifícios residenciais inteiros e carros, seguem uma única regra compartilhada: cada bem continua existindo e passa integralmente a um único herdeiro elegível, com distribuição automática que procura aproximar o valor total recebido por cada um, considerando os valores econômicos estimados dos bens existentes, sem garantir divisão matematicamente perfeita.

- Quando um herdeiro elegível já reside no imóvel herdado, sua continuidade na moradia deve receber preferência na atribuição daquele imóvel, sem criar copropriedade ou desapropriação instantânea por motivo sucessório.

- Se houver apenas um imóvel e vários herdeiros, é aceitável que um herdeiro receba o bem e outros recebam patrimônio de menor valor ou nenhum bem físico: não criar compensação obrigatória com dinheiro inexistente, dívida nova, empréstimo, venda compulsória, avaliação judicial ou processo de inventário só para igualar parcelas.

- Herdar altera a titularidade, não duplica ou teletransporta casas, apartamentos ou veículos, nem altera automaticamente a ocupação/uso físico sem as decisões e deslocamentos reais já previstos.

- A transmissão acontece pelo evento de falecimento, sem decisões manuais do jogador ou varreduras sucessórias contínuas; valores estimados, desempates estáveis entre herdeiros e destino de bens físicos sem herdeiro seguem calibração/implementação das regras patrimoniais existentes, sem transformar a herança em nova camada de gameplay.

- A regra é intencionalmente aproximada e pode ser reavaliada após feedback da simulação, não exige equivalência patrimonial absoluta.

- Se não houver nenhum desses herdeiros elegíveis, aplicar a regra definida de patrimônio sem sucessor: dinheiro sem destinatário vai para a Reserva Global, imóvel pode ficar sem proprietário e sua eventual venda posterior remete pagamento à Reserva Global.

- Ser solteiro ou não ter filhos não significa automaticamente não ter herdeiro, pois pais/irmãos podem existir.

- Dívida pessoal de aluguel do falecido conserva seu tratamento específico anterior; não transferir automaticamente a dívida aos familiares.

#### Venda individual de apartamentos mesmo após compra do prédio inteiro

- Um SIM que comprou todo o prédio residencial controla economicamente todos os apartamentos reais daquele edifício e pode vender um ou vários deles isoladamente, sem ser obrigado a vender o conjunto inteiro.

- Um SIM que já possui apenas duas unidades também pode vender só uma.

- Ao vender, somente a titularidade e o pagamento real da unidade vendida mudam, com valor de mercado do apartamento e transferência ao vendedor real conforme a regra patrimonial existente; o antigo proprietário conserva os apartamentos não vendidos.

- Não manter simultaneamente o mesmo apartamento na titularidade de um prédio integral e na propriedade unitária de outro SIM, não tributar/cobrar renda duas vezes e não duplicar unidades físicas, moradores ou contratos.

- A representação do título de prédio integral deve poder se desdobrar em títulos por unidade quando ocorrer a primeira venda individual, preservando número de apartamentos e suas ocupações; a soma de valores e incidências deve continuar coerente com o patrimônio real sem ganho fictício com a divisão.

- Vender uma unidade já alugada não desaloja magicamente seus moradores: locação e comportamento de novo proprietário seguem as regras habitacionais aprovadas.

- A configuração de registro inicial, avaliação por unidade, repartição sem duplicação e áreas comuns são detalhes para especificação técnica proporcional, sem exigir condomínio, síndico ou autorização manual do jogador.

#### Prédios residenciais com vários donos, sem condomínio financeiro próprio

- Cada apartamento mantém seu SIM proprietário e suas obrigações de impostos, contas e aluguéis efetivamente definidos; não criar taxa extra de condomínio, caixa condominial, rateio obrigatório de despesas comuns ou síndico em edifícios exclusivamente residenciais no modelo inicial.

- Exceção delimitada: em edifícios mistos, reparos físicos efetivos de partes estruturais compartilhadas são rateados automaticamente entre os proprietários das unidades segundo a regra específica da seção de edifícios mistos; isso não introduz condomínio financeiro ou cobrança genérica recorrente.

- A unidade real e sua titularidade não dependem de uma entidade jurídica de condomínio.

- **Em aberto:** A ausência de tarifa comum não elimina custos físicos e obrigações já previstos por outros sistemas; necessidade futura de custear áreas compartilhadas fica em exploração, sem taxa inventada.

#### Propriedade residencial por prédio inteiro ou por apartamento

- Em construções residenciais com múltiplas unidades, o patrimônio pode estar na forma de prédio inteiro com um único SIM proprietário, que mantém os direitos sobre as unidades e pode alugá-las, ou de apartamentos individualmente possuídos por SIMs diferentes, com compras, vendas, locações e tributação atribuídas às unidades e titulares reais.

- Um SIM pode possuir quantas casas e apartamentos conseguir adquirir com seu dinheiro real, inclusive imóveis de investimento para viver de aluguéis; não impor limite numérico arbitrário nem compra/ocupação compulsórias.

- Cada apartamento privado possui um único proprietário SIM, aplicando o mesmo princípio definido para imóveis individuais; uma família residente é distinta do proprietário e não surge automaticamente por compra.

- Não registrar simultaneamente dois donos sobre o mesmo apartamento, nem cobrar aluguel, imposto, preço de compra ou renda de um mesmo ativo duas vezes (prédio integral e unidades).

- A venda individual a partir de edifício integral segue a regra específica desta seção; como o edifício nasce integralmente ou subdividido, a avaliação sem duplicidade, a administração de áreas comuns e custos compartilhados permanecem detalhes para definição/validação coerentes com o modelo físico antes da implementação dependente, sem criar síndico/condomínio como minijogo ou titular econômico fictício.

- Um cidadão/família local com recursos suficientes pode adquirir imóveis residenciais adicionais além da própria moradia.

- A decisão de investir em outro imóvel acontece pela simulação, sem exigir aprovação manual do jogador.

- Um imóvel adicional pode ser disponibilizado para aluguel a outra família real.

- O aluguel pago deve ir ao proprietário real do imóvel, e não a um recebedor abstrato.

- Se o proprietário morrer e um herdeiro assumir o imóvel alugado, esse herdeiro também passa a ser credor dos aluguéis atrasados associados ao imóvel.

- A dívida permanece e as cobranças futuras passam a beneficiar o novo credor real, sem criar moeda.

- Se morrer sem herdeiro elegível, os aluguéis atrasados devidos a ele são perdoados: não passam à Reserva Global nem ao futuro comprador.

- Extinguir a obrigação contábil não cria nem destrói dinheiro.

- O imóvel pode permanecer sem proprietário até uma aquisição futura, conforme a regra de patrimônio não reclamado.

- Se houver família morando de aluguel em imóvel que ficou sem proprietário, ela pode permanecer temporariamente sem pagar novos aluguéis nem acumular dívida durante 3 meses do calendário da simulação, contados a partir do momento em que o imóvel fica sem proprietário.

- A família procura automaticamente outra moradia durante esse prazo, conforme a regra geral de realocação.

- Se o prazo terminar e não houver nova situação de locação ou propriedade que permita sua permanência, a família deve desocupar o imóvel.

- Se não encontrar outra residência na cidade, pode optar por sair da cidade por migração, como consequência real da falta de moradia; essa saída não é obrigatória nem garantida, e a população sem moradia continua sendo um estado possível conforme as demais regras da simulação.

- Não se inventa credor para cobrar aluguel sem proprietário, nem dívida ou cobrança retroativa pelo período sem dono.

- O imóvel permanece disponível para compra, mesmo após a saída dos ocupantes.

- **Em aberto:** O prazo inicial é de 3 meses, sujeito a calibração futura; os critérios simples para desocupação, migração ou situação sem moradia seguem em definição.

- Um imóvel sem proprietário pode ser adquirido mesmo ocupado, por um comprador real elegível que decide automaticamente entre morar nele ou mantê-lo alugado à família atual.

- Se optar pela locação, os aluguéis futuros passam a ser devidos a esse novo proprietário, sem retroatividade.

- Se optar por morar, a família anterior dispõe de 3 meses do calendário da simulação, contados a partir da aquisição, para encontrar outro lugar e deixar o imóvel; o novo proprietário só passa a ocupar a residência após a desocupação.

- Não há expulsão instantânea.

- Durante esses 3 meses, enquanto a família ainda ocupar a residência, ela deve pagar aluguel ao novo proprietário real, desde a aquisição e sem cobrança retroativa pelo período em que o imóvel estava sem dono.

- O valor e os reajustes seguem as regras gerais de aluguel residencial já definidas, sem impor um reajuste automático somente pela compra.

- A escolha é da simulação, não uma gestão manual do jogador.

- Mantêm-se as regras existentes de elegibilidade de compradores, inclusive para famílias que migram do exterior.

- Em cada locação residencial, há um único SIM locatário responsável pelo pagamento do aluguel e por eventuais dívidas de aluguel.

- Outros adultos que moram na mesma residência não são automaticamente corresponsáveis por essas obrigações.

- A identificação/seleção do locatário titular é feita pela simulação.

- Se esse SIM morrer e houver outro adulto na família, a locação continua com os moradores, e a simulação atribui a outro adulto a responsabilidade pelos aluguéis futuros, sem herdar a dívida pessoal do falecido, que segue sua regra própria de quitação na morte.

- **Em aberto:** Os critérios de escolha do novo titular permanecem em exploração; separação e ausência de adulto elegível seguem as regras específicas desta seção.

- No primeiro modelo, o valor do aluguel residencial é proporcional ao valor de mercado do imóvel, usando um percentual de referência a calibrar.

- A procura influencia o aluguel apenas na medida em que afeta o valor de mercado: não há ajuste adicional independente por oferta/procura nesta versão.

- O aluguel é calculado pela simulação, sem negociação manual de cada imóvel.

- O aluguel residencial é pago mensalmente, uma vez por mês do calendário da simulação, por transferência do saldo do SIM locatário responsável ao SIM proprietário real.

- A regra não cria cobrança quando o imóvel estiver sem proprietário; falta de pagamento segue as regras de inadimplência.

- A data exata do vencimento mensal e a cobrança proporcional em início/fim de locação permanecem em calibração.

- Para uma locação em andamento, o aluguel é reajustado anualmente, a cada 12 meses do calendário da simulação, não a cada variação do valor de mercado.

- Na data do reajuste, a simulação recalcula o valor conforme o valor de mercado do imóvel e o percentual vigente, sem ajuste independente adicional por procura.

- Os pagamentos continuam mensais; reajustar anualmente não significa cobrar aluguel só uma vez por ano.

- Troca de proprietário, isoladamente, não antecipa o reajuste.

- O marco de contagem da primeira data de reajuste e ajustes de calendário permanecem em calibração.

- Se uma família não conseguir pagar aluguel, o atraso gera dívida real com o proprietário do imóvel e o prazo de inadimplência é de 3 meses do calendário da simulação, contados a partir do primeiro aluguel mensal vencido e não quitado; ao atingir esse prazo com dívida vencida ainda pendente, a locação é encerrada automaticamente e a família perde o direito de permanecer no imóvel.

- Pagamentos parciais abatem o saldo devido, mas não reiniciam nem suspendem a contagem dos 3 meses enquanto restar dívida vencida.

- A quitação integral do débito encerra a inadimplência e zera sua contagem; um novo atraso inicia novo prazo. Ao completar os 3 meses com dívida vencida ainda pendente, a locação termina automaticamente por inadimplência; não há votação/decisão adicional do SIM proprietário, autorização do jogador ou tolerância extra por padrão.

- A família entra no processo automático de procura e saída da moradia definido; a ocupação e a mudança seguem caminhos físicos/tempos reais viáveis, sem teletransporte nem garantia de casa nova.

- Se não houver moradia compatível, permanecem as alternativas aprovadas de situação sem moradia ou emigração.

- O gatilho é o vencimento dos 3 meses, não uma ordem manual ou um despejo com novo prazo arbitrário.

- A dívida de aluguel não é perdoada automaticamente quando a família deixa o imóvel ou se muda; permanece como obrigação devida ao credor real até ocorrer uma forma de quitação ou resolução ainda a definir.

- A dívida é uma obrigação rastreável, não saldo monetário criado: apenas um pagamento efetivo transfere dinheiro dos SIMs devedores ao SIM credor.

- O procedimento deve ocorrer automaticamente, sem aprovações manuais do jogador.

- Na recuperação da dívida de aluguel, foi escolhida a cobrança gradual por desconto percentual no salário quando ele é pago, limitada ao salário e ao saldo devido; sem salário recebido, não há desconto nesse evento.

- Se o SIM devedor morrer, a dívida de aluguel é quitada primeiro com o dinheiro disponível em seu saldo individual, até o limite devido, por transferência real ao SIM proprietário credor.

- A parcela que exceder esse dinheiro é encerrada sem cobrança dos herdeiros ou familiares, e nenhuma dívida monetária é convertida em dinheiro criado/destruído.

- Eventual saldo remanescente do falecido só segue depois para a herança ou, sem herdeiro elegível, para a Reserva Global.

- A regra não exige venda compulsória de imóveis para pagar o aluguel atrasado.

- **Em aberto:** A porcentagem de desconto salarial, critérios de escolha do SIM locatário titular ou de seu sucessor adulto, outros meios de pagamento, encargos, limiares da condição de falência pessoal e critérios para migração ou permanência na cidade sem moradia continuam em definição; ausência de adulto elegível e rateio entre créditos municipais seguem as regras específicas desta seção.

- Evitar cálculos contínuos e exceções complexas por SIM.

#### Realocação residencial após perda da moradia

- Quando uma família precisa deixar o imóvel por inadimplência, término da tolerância sem proprietário ou compra para moradia do novo dono, a simulação procura automaticamente uma residência disponível e compatível com os recursos reais da família.

- Se encontrar, a família pode mudar para ela, seguindo as regras normais de aquisição/locação.

- Se não encontrar, a família pode permanecer na cidade sem moradia ou emigrar, segundo suas condições e as da cidade.

- A falta de imóvel compatível não garante emigração, reassentamento artificial ou moradia gratuita; não exige seleção de destino nem aprovação manual do jogador.

- Os critérios para escolher entre ficar sem moradia e emigrar, bem como a procura/mudança, ficam para calibração/implementação.

#### Mudança residencial voluntária em busca de melhores condições

- Além de mudanças por despejo/perda de moradia, famílias com residência válida podem avaliar ocasionalmente mudar para outra moradia real, considerando renda/saldo, tamanho e lotação, valor de compra/aluguel, acessibilidade a emprego/serviços, qualidade de vida e preferências.

- Uma mudança ocorre somente se houver alternativa compatível e economicamente viável, com compra ou locação verdadeira, contratos/obrigações e deslocamento físico correspondentes; não criar demanda fictícia, teletransportar moradores nem obrigar migração por pequena oscilação.

- O SIM ou família pode preferir permanecer mesmo quando existir alternativa.

- Reavaliação deve ser por eventos relevantes ou intervalos econômicos, não varrer imóveis e moradores a cada quadro; limiares/frequência são calibração.

### Empresas e economia

#### Danos compartilhados em edifício misto

- Quando um dano real na parte estrutural compartilhada (por exemplo telhado afetado por incêndio) demandar o rateio proporcional previamente aprovado entre os proprietários reais das unidades, se não houver dinheiro suficiente para concluir o reparo o serviço não é realizado gratuitamente nem bancado automaticamente por outros proprietários ou pela Prefeitura.

- A parte estrutural continua danificada e produz consequências físicas e operacionais coerentes até que o custo possa ser efetivamente pago, sem caixa negativo fictício, taxa condominial mensal ou pedido manual ao jogador para cobrar cada dono.

- Não requer granularidade de dano por peça.

#### Granularidade dos danos em edifício misto

- Representar os danos de forma agregada por unidade operacional (por exemplo loja ou apartamento) e pelo estado relevante da estrutura compartilhada, sem simular individualmente paredes, janelas, telhas, cômodos ou todas as peças 3D.

- Dano localizado afeta a unidade realmente atingida; dano estrutural compartilhado sério pode restringir várias unidades por condições reais, não automaticamente por mero evento distante.

- Proprietários, contratos, moradores, produção e atendimentos seguem as regras de consequências físicas existentes, com motivo simples consultável; sem duplicar custo de reparo ou criar microssimulador estrutural.

#### Esclarecimento da locação comercial

- O mesmo princípio automático de proporcionalidade ao preço do imóvel, pagamento mensal e reajuste anual das residências foi aceito para aluguel comercial, sem impor a mesma porcentagem numérica para tipos de imóvel distintos.

- Venda do imóvel com locação em curso preserva inicialmente o operador; o novo proprietário pode pedir o imóvel para usar por conta própria, mas o contrato e a saída devem respeitar aviso e consequências físicas/econômicas reais, nunca encerramento instantâneo de atividades ou desaparecimento de estoque.

#### Prazo de atraso no aluguel empresarial

- Aplicar inicialmente os mesmos três meses do calendário simulado usados para inadimplência de aluguel residencial para encerrar automaticamente uma locação comercial cuja dívida vencida não foi integralmente quitada, com pagamentos parciais abatendo valor mas sem reiniciar o prazo enquanto houver atraso pendente.

- A dívida permanece com a empresa operadora real após a saída e integra obrigações/possível falência.

- O encerramento do contrato não teletransporta pessoas, estoque ou veículos nem apaga as etapas físicas de saída/fechamento; há aviso e transição reais conforme contrato e estado da operação, sem sistema judicial.

- Trata-se de unificar a regra de prazo, não de garantir solvência ou equiparar todas as consequências sociais de moradia e comércio.

#### Locação empresarial — condições complementares

- Preservada a locação entre empresas distintas já definida, o aluguel do imóvel comercial/industrial é automaticamente proporcional ao valor de mercado, no mesmo princípio inicial do aluguel residencial, com pagamentos mensais e revisão do valor anual, conforme o calendário já existente; percentual concreto pode variar por tipo de imóvel e permanece para calibração.

- O contrato é contínuo enquanto ambas as partes mantiverem condições e interesse, com renovação automática e saída mediante aviso real (9A), sem negociar manualmente cada vencimento.

- Falta de pagamento cria dívida monetária exigível da locatária à proprietária; inadimplência persistente encerra a locação automaticamente pelo prazo inicial de três meses, conforme regra específica desta seção; o valor vencido continua obrigação real mesmo depois do encerramento.

- Pagamentos parciais somente abatem dívida real.

#### Responsabilidades na locação empresarial

- O imposto imobiliário permanece com a empresa proprietária; água e energia de operação realmente consumidas são despesas da empresa locatária, sem cobrança dupla.

- Reparo efetivo da estrutura do imóvel cabe à proprietária, e alterações/adaptações e danos de sua atividade cabem à operadora; distinguir partes comuns do edifício misto quando aplicável.

- Custos são pagos por agentes reais, sem criar taxa mensal de condomínio, reparo inventado ou fiscalização por objeto físico.

#### Venda, falência do locador e saída da operadora

- O imóvel comercial pode ser vendido com locação em andamento, e a compradora real assume os direitos e obrigações futuros de locadora, preservando a operação enquanto o contrato válido continuar.

- O novo proprietário pode solicitar retomada do imóvel para uso próprio, mediante aviso/transição contratual real que permita encerrar ou transferir a operação e destinar estoques/trabalhadores conforme regras existentes; não há expulsão instantânea, cancelamento de dívidas, transmissão automática da empresa locatária ou prazo numérico de aviso aprovado.

- Se a proprietária falir, o imóvel próprio entra na liquidação e pode ser vendido com contrato vigente; o prédio alugado nunca vira ativo da locatária pela quebra do locador.

- Se uma empresa locatária encerrar uma filial mas continuar existindo, pode sair da locação conforme o contrato e quitar/continuar responsável pelas obrigações reais pendentes, sem precisar falir a entidade inteira.

#### Busca de novo endereço durante aviso de retomada comercial

- Quando a empresa proprietária de imóvel comercial alugado solicitar legitimamente sua retomada para uso próprio, a empresa locatária procura automaticamente outro imóvel real e compatível durante o período de aviso, podendo manter a operação no endereço antigo enquanto o contrato e as condições efetivas permitirem.

- Encontrar novo ponto não é garantido: aquisição/locação, custo, estoque, trabalhadores, transporte e transição devem respeitar recursos, tempo, deslocamentos e contratos reais.

- Não tornar a permanência indefinida, não expulsar instantaneamente e não obrigar o jogador a escolher outro endereço por empresa.

- Prazo numérico de aviso continua para calibração/definição proporcional.

#### Insumos iniciais agregados para fábrica de automóveis

- Fabricar carros reaproveita recursos físicos agregados já existentes, como aço, além de energia, trabalho de SIMs, tempo e capacidade industrial reais; não é necessário criar agora a categoria Componentes Automotivos, nem simular motores, pneus, eletrônicos e peças separadamente.

- Produzir gera veículo físico real apenas mediante consumo de recursos e operação econômica correspondentes; preço, quantidade de materiais, cadência e logística ficam para calibração/especificação simples.

- Aprofundamento futuro desejado pelo responsável: ampliar as cadeias e tipos de produtos quando houver benefício de gameplay demonstrável, não aprovado como novas categorias no modelo inicial.

#### Preferência local moderada na compra de veículos

- Compradores economicamente elegíveis (SIMs e agentes que operam frotas reais) comparam automóveis/ônibus locais e importados por preço total, disponibilidade, tempo e condições reais; quando as alternativas forem semelhantes e viáveis, preferem o veículo fabricado na cidade.

- Se faltar oferta local ou a importação apresentar vantagem material, importação continua possível; não impor reserva obrigatória para fabricantes locais, compradores garantidos, veículos fictícios ou preço sem fundamento.

- A heurística deve reaproveitar a preferência local moderada já definida para abastecimento comercial, com limiares calibráveis, sem decisão manual de fornecedor por comprador.

#### Venda direta de veículos pela fábrica

- No modelo inicial, a empresa fabricante vende carros produzidos fisicamente diretamente ao comprador real elegível, com saída de estoque, pagamento efetivo ao caixa da empresa e entrega/deslocamento físico da unidade à destinação.

- Não exigir concessionária ou revendedora intermediária como instalação obrigatória; compradores também podem importar conforme as regras definidas.

- Critérios de oferta, entrega, preços e vínculo com produção de ônibus ficam para especificação proporcional, sem vendas de veículos inexistentes.

#### Fábrica de automóveis

- Como indústria privada possível, converte insumos e capacidade reais em carros que podem ser adquiridos e circular fisicamente; não cria veículo ou receita sem produção/venda.

- O ramo segue o modelo aprovado de empresa autônoma proprietária/operadora, sem controle de produção pelo jogador e sem obrigar o início da cidade a possuir indústria automobilística.

- Ônibus também podem ser fabricados localmente quando houver instalação e fluxo econômico apropriados; preservar a importação como alternativa.

- Os materiais, forma de compra/distribuição e receitas precisam de especificação antes do código deste ramo, sem adicionar um catálogo técnico de centenas de peças por padrão.

#### Empresas privadas de serviços presenciais

- Além de atividades baseadas em mercadorias e dos negócios já previstos, o IndexCities inclui estabelecimentos privados de prestação de serviços com empresas econômicas reais, trabalhadores/vagas reais, clientes atendidos e pagamentos monetários efetivos.

- Reutilizar o modelo aprovado de construção/aquisição de estabelecimento privado, caixa empresarial, contratação, demanda local e falência; atendimento exige existência de operador, funcionários, acesso, cliente e capacidade disponíveis, sem produção de mercadoria física ou estoque inventados apenas para representar serviços imateriais.

- Quando o serviço depender da presença do cliente, preservar deslocamento físico e operação compatível com o modelo atual de visitas.

- A experiência do jogador não exige aprovar clientes, escalas ou transações uma a uma.

- **Em aberto:** O catálogo inicial completo tem dez atividades, listadas na seção própria; necessidades particulares, capacidade, fluxos físicos e diferenças operacionais de cada ramo ainda precisam ser especificados antes da implementação correspondente, sem criar demanda fictícia.

- Para expansões posteriores, avaliar relevância urbana, benefício sistêmico, demanda existente, diversidade visual e custo de assets.

- Não confundir variedade de negócios com necessidade de modelos 3D exclusivos nem com dezenas de engines profissionais, estoques ou necessidades artificiais; eventuais necessidades/custos próprios de cada atividade precisam de definição antes de sua implementação. [Pesquisa de candidatos e custo de assets](../exploration/companies-markets.md#pesquisa-de-portfolio-enxuto-de-servicos-privados-2026-10-08).

#### Diversidade comercial com catálogo ampliável e assets reaproveitados

- A cidade deve oferecer variedade perceptível de atividades econômicas, sem ficar restrita a dois ou três negócios repetidos pelo mapa.

- Buscar um catálogo de negócios mais amplo do que um punhado de tipos, usando poucos comportamentos econômicos compartilhados (produção, compra/revenda de bens e prestação de serviços) e famílias de edifícios/modelos 3D reutilizáveis, com diferenciação visual simples onde couber; negócios podem receber assets especializados progressivamente sem alterar automaticamente empresa, proprietário ou atividade.

- Um tipo de negócio só entra oficialmente no catálogo quando sua operação tiver demanda ou clientela identificável, capacidade, custos, receita/pagamento real e consequências coerentes com a SPEC, aproveitando regras existentes antes de criar novas necessidades, insumos, profissões ou subsistemas.

- A distribuição das empresas deve resultar de oportunidades, demanda, concorrência, localização e decisões econômicas reais, sem cotas artificiais para alternar fachadas nem ocupação garantida; excesso de um ramo pode gerar baixa rentabilidade, vacância ou fechamento.

- **Em aberto:** Os dez tipos de negócio iniciais constam da seção própria; o número de famílias de assets e o desenho econômico próprio de ramos ainda não modelados continuam abertos.

- A direção aprova variedade com reuso e causalidade, não geração indiscriminada de empresas ou produção antecipada de assets únicos. [Exploração temática](../exploration/companies-markets.md#atualizacao-empresas-diferentes-com-poucos-modelos-base-2026-10-08).

#### Edifícios mistos de comércio no térreo e moradias acima (direção inicial)

- Admitir alguns modelos prontos de construção de uso misto, onde unidades residenciais reais e espaço(s) comerciais reais coexistem no mesmo volume físico, mantendo entradas/acessos, capacidade, famílias, empresa, trabalhadores, consumo e estoques correspondentes, sem criar clientes, imóveis ou empresas por aparência.

- Trata-se de uma categoria composta explicitamente escolhida pelo jogador na construção, exceção delimitada à categoria única dos prédios comuns, e não autoriza editor modular por pavimento, zoneamento automático nem upgrade de prédio pronto.

- A titularidade econômica de cada unidade residencial e da parte comercial deve ser inequívoca e distinta quando houver proprietários diferentes, sem cobrar/vender/indenizar o mesmo ativo duas vezes.

- A unidade comercial pertence individualmente a uma empresa e as residenciais a SIMs, tributadas/avaliadas sem duplicação.

- **Em aberto:** Partes comuns, despesas estruturais, intervenções no conjunto, interface e modelos exatos ainda requerem especificação antes do código dependente, sem presumir condomínio, copropriedade ou gestor comum novo.

#### Propriedade separada dos espaços de edifício misto

- A parte comercial de um prédio misto pode pertencer à empresa real operadora e os apartamentos/unidades residenciais a SIMs proprietários distintos.

- Registrar cada unidade econômica uma única vez, com preço de aquisição, aluguel residencial quando aplicável, titularidade, imposto de seu tipo de uso, receitas e indenização exclusivos de seu ativo; o prédio 3D compartilhado não é mais um imóvel integral que possa ser vendido, tributado ou indenizado novamente por cima das unidades já individualizadas.

- A empresa proprietária da unidade comercial que adquiriu pode operá-la diretamente ou alugá-la a outra empresa operadora; SIMs podem morar ou alugar suas unidades conforme as regras residenciais.

- A mudança de operadora não altera a propriedade da unidade comercial.

- Partes comuns e execução do rateio permanecem para modelagem proporcional; a intervenção/demolição do conjunto preserva unidade econômica e indenização de cada titular sem duplicação, sem condomínio novo por inferência.

- A titularidade e a operação da unidade podem pertencer a empresas distintas, conforme a regra de locação comercial.

#### Oferta inicial de unidades de edifício misto

- Após a conclusão efetiva de um prédio misto, seus apartamentos e espaço(s) comercial(is) podem ser oferecidos e comprados separadamente desde a primeira venda, mesmo que as demais unidades permaneçam disponíveis.

- SIMs reais adquirem as unidades residenciais e empresas reais adquirem unidades comerciais, conforme viabilidade, patrimônio, pagamentos e regras existentes.

- Não exigir comprador simultâneo para todo o edifício nem criar venda do volume 3D como patrimônio adicional duplicado; titularidade, imposto, aluguel, indenização e responsabilidades comuns pertencem às unidades reais e à estrutura única já prevista.

- A regra não proíbe formas de titularidade integral já permitidas quando economicamente coerentes, sem dupla propriedade.

#### Reparos das partes comuns de edifício misto

- Se uma estrutura física efetivamente compartilhada por unidades residenciais e comerciais, como telhado ou estrutura comum, precisar de reparo por dano real, os proprietários reais das unidades contribuem proporcionalmente para o custo real, sem cobrar novamente de uma unidade por existir um único modelo 3D de edifício e sem transferir obrigação privada ao Caixa da Cidade.

- A cobrança e execução devem ser automáticas, mantendo os saldos e destinos dos pagamentos concretos, sem exigir síndico, assembleia, condomínio como entidade econômica ou clique individual do jogador.

- Não criar manutenção condominial mensal por inferência: o rateio é para reparo real compartilhado.

- **Em aberto:** Critério exato de proporção e indenização/intervenção simultânea de unidades exigem definição coerente antes da implementação; falta de caixa e estado de dano/reparo seguem as regras específicas desta seção, sem reparo gratuito ou dívida fictícia.

- Esta decisão é uma exceção delimitada à regra de prédios exclusivamente residenciais sem rateio obrigatório de custos comuns.

#### Reutilização de imóvel econômico e mudança de atividade

- Após fechamento/saída de um estabelecimento, seu edifício permanece como ativo físico segundo as regras normais de propriedade, liquidação e compra/venda.

- Um novo comprador pode instalar outra atividade compatível com o imóvel e a categoria urbana, inclusive igual à atividade de um concorrente vizinho, não ficando preso ao ramo anterior.

- Se a troca exigir obras/adaptação física, deve haver custos reais de dinheiro, materiais, trabalho e tempo correspondentes, pagos pelo agente econômico responsável quando definidos e nunca criados/desaparecidos sem contrapartida; se bastar trocar identificação/uso de estrutura já adequada, não impor reforma artificial.

- A operação nova só inicia quando acesso, equipe, capacidade e condições reais exigidas estiverem viáveis.

- Mudança não duplica propriedade, estoque, empregados nem a empresa antiga; a compra e a eventual falência seguem fluxos já previstos.

- Não dar ao jogador controle direto sobre a decisão comercial da empresa.

- Pormenores de adaptação ficam para definição proporcional ao risco.

#### Imóvel econômico vazio ainda pertencente à empresa

- Encerrar a operação de um estabelecimento não extingue automaticamente a propriedade do imóvel nem a empresa que mantém outros negócios ou capacidade econômica efetiva.

- Enquanto mantiver o ativo, a mesma empresa pode deixá-lo vazio e sem operação, arcando com impostos, custos de propriedade e obrigações realmente existentes, e posteriormente vendê-lo ou reutilizá-lo com atividade permitida e adaptações reais quando necessárias, segundo perspectivas econômicas e seu caixa próprio; não haveria receita, clientes ou funcionários fictícios durante a vacância.

- Se a empresa inteira falir, seus imóveis remanescentes seguem a liquidação patrimonial definida, não ficam com uma empresa extinta.

- O aluguel comercial entre empresas é permitido; o imóvel mantido vazio pode ser oferecido a outra empresa operadora mediante contrato e aluguel reais, conforme a regra específica.

- A empresa dona não perde o título só porque alugou.

#### Locação comercial entre empresas

- A empresa dona também pode alugar a outra empresa operadora, que não se torna proprietária pela locação.
- Não misturar a titularidade do imóvel ao estoque/caixa/funcionários da locatária e não introduzir propriedade de imóveis comerciais por SIMs.

- A empresa proprietária de um imóvel comercial/industrial pode alugá-lo a outra empresa economicamente distinta, que passa a ser sua operadora quando a atividade for viável.

- O contrato registra proprietário e locatária reais, imóvel físico único e aluguel periódico monetário real da empresa locatária à proprietária, sem compra disfarçada, aluguel para si mesma, dinheiro, emprego, clientes ou patrimônio fictícios.

- O dono mantém titularidade patrimonial, possíveis impostos sobre o imóvel e direitos de venda/decisão econômica compatíveis; a locatária usa seu próprio caixa, trabalhadores, mercadorias, receitas, custos de operação e logística e continua sujeita a riscos financeiros e falência próprios.

- Imóvel desocupado pode continuar nas mãos do dono conforme a regra de imóvel vazio; aluguel não é obrigatório nem garantido.

- Na falência da locatária, o prédio alheio não entra na liquidação dela; obrigações efetivamente devidas ao proprietário são créditos da empresa locadora conforme regras econômicas pertinentes.

- Valor, ciclo, contrato, inadimplência, responsabilidades e venda/encerramento seguem as regras específicas desta seção.

- **Em aberto:** O aviso de retomada pelo novo dono, interação fina com intervenção municipal e calibração ainda requerem definição antes do código correspondente; não importar automaticamente o prazo de despejo de três meses do aluguel residencial, criar bancos/financiamento ou gestão contratual manual do jogador.

- SIMs não possuem imóveis comerciais neste modelo.

#### Escolha econômica entre comprar e alugar imóvel comercial

- Quando houver oportunidade real de instalar/operar uma atividade em imóvel compatível, a empresa avalia compra e locação efetivamente ofertadas segundo seu caixa/capital disponível, preço de aquisição, aluguel periódico, custos e obrigações, demanda esperada, risco e perspectivas de operação.

- Pode comprar ou alugar conforme viabilidade, sem preferência absoluta por propriedade ou locação e sem transformar uma intenção em contrato automático.

- Aquisição transfere dinheiro/propriedade reais; aluguel mantém titularidade distinta e pagamento periódico real.

- Reusar decisões ocasionais de oportunidade da empresa, sem negociação manual do jogador nem busca global por quadro.

### Catálogo inicial de atividades privadas

- As dez identidades de negócio abaixo pertencem ao catálogo completo aprovado, sem substituir fazendas, fábricas ou outros sistemas produtivos já previstos.

- São tipos de atividade, não dez empresas obrigatórias, dez prédios diferentes nem dez mecanismos econômicos separados:

| Grupo para leitura do catálogo | Tipos de negócio confirmados |
| --- | --- |
| **Comércio de mercadorias** | **Mercado**, **mercearia**, **posto de combustível** |
| **Alimentação fora de casa** | **Café**, **lanchonete**, **restaurante** |
| **Serviços pessoais e lazer** | **Barbearia**, **salão de beleza**, **academia** |
| **Hospedagem** | **Hotel** |

#### Visual e economia separados

- Pequenos comércios/serviços podem compartilhar prédios-base, fachadas e rotinas econômicas; posto de combustível e hotel podem demandar estruturas e capacidades próprias.

- A distinção de nome não obriga novo SKU, necessidade individual ou modelo 3D exclusivo, mas cada negócio precisa ter causa real de uso e receita.

- Mercado/mercearia podem compartilhar mercadorias modeladas com diferenças compatíveis de porte/capacidade; café/lanchonete/restaurante podem compartilhar a base de Alimentos e de atendimento presencial, respeitando compras e consumo domiciliares já definidos; barbearia/salão/academia usam o princípio de serviços opcionais do SIM, sem necessidades essenciais fictícias; posto preserva a cadeia física de Combustível; hotel preserva turistas, hospedagem, ocupação e capacidade real.

#### Alimentação em cafés, lanchonetes e restaurantes

- Esses estabelecimentos adquirem e mantêm estoque físico real da categoria Alimentos, proveniente de fornecedores/importação e logística conforme as regras existentes; uma refeição vendida e consumida no local desconta a quantidade correspondente do estoque do estabelecimento e satisfaz efetivamente a alimentação do SIM.

- O SIM realiza uma única compra/pagamento real pelo atendimento/refeição; o mesmo consumo não retira Alimentos do estoque doméstico nem exige segunda cobrança quando o SIM chegar em casa.

- Compra para casa no mercado/mercearia mantém as regras próprias de transporte e estoque familiar.

- Para simular refeição não são necessários ingredientes, receitas individuais, SKUs adicionais nem uma cadeia produtiva fictícia; quantidade por refeição, frequência e eventual diferença de porte/serviço ainda ficam para calibração coerente.

- A visita e compra são presenciais, respeitando funcionários, acesso e capacidade reais; não permitir que um restaurante funcione sem Alimentos, equipe ou dinheiro do cliente.

#### Barbearia, salão de beleza e academia

- Compartilham a mesma lógica simples de visitas opcionais e atendimento presencial, aproveitando preferências, tempo, acesso, recursos financeiros e empresas reais.

- Cada atividade pode ter preço do serviço, duração de atendimento, capacidade e custos de pessoal/instalação diferentes, compatíveis com seu funcionamento, para produzir consequências econômicas reais e ocupação distinta, sem criar necessidades obrigatórias de cabelo, estética ou exercício, recursos físicos fictícios, profissão formal própria ou controle individual pelo jogador.

- Um atendimento só gera receita se realmente houver cliente, equipe/capacidade e transferência monetária.

- Valores, frequência de procura e tempo por atendimento são parâmetros de balanceamento, não requisitos numéricos pré-fixados.

#### Limites da aprovação

- Nenhum número de prédios/variantes 3D foi definido.

- **Em aberto:** As diferenças conceituais de refeição, varejo, serviços opcionais e famílias 3D seguem as regras desta seção; parâmetros numéricos, insumos e exceções ainda não decididos precisam de regras coerentes com a SPEC antes do código que dependa deles; não inventar estoque de serviço, contratação fictícia nem faturamento automático.

- A oficina mecânica está aprovada como estrutura especializada própria (ver [quebras, guincho e oficina](transporte-circulacao.md#quebras-guincho-local-e-oficina)), fora desse catálogo de dez. Lavanderia, escritórios e outras atividades estudadas permanecem fora desse catálogo de dez, podendo ser exploradas futuramente sem entrar automaticamente na implementação.

#### Prestação de serviços entre empresas

- Além de atender cidadãos/turistas quando aplicável, uma empresa privada pode contratar e pagar serviços de outra empresa privada quando existir necessidade comercial/operacional concreta, contraparte real capaz de prestá-lo, preço, disponibilidade e recursos financeiros suficientes.

- O serviço concluído transfere dinheiro existente do caixa da empresa cliente para o caixa da prestadora, sujeito às mesmas regras de custo, receitas e continuidade de entidades; pagamento não cria saldo nem serviço fictício.

- Quando o atendimento envolver presença ou execução física, deve respeitar pessoal, acesso, capacidade e deslocamentos reais; não inventar viagens presenciais para serviços que não as exijam.

- A simulação automatiza essa escolha sem solicitar aprovação de cada compra ao jogador.

- A aprovação não cria por si escritórios, catálogo de serviços empresariais, contratos formais detalhados, assinaturas obrigatórias ou despesas periódicas fictícias; somente serviços concretos especificados para algum ramo poderão ser prestados, e o regime de cobrança deverá ser definido antes da implementação dependente.

#### Preços privados de prestação de serviços

- Cada empresa prestadora define autonomamente seu próprio preço efetivamente cobrado pelo serviço oferecido, considerando custos reais de pessoal e operação, clientela/demanda acessível, concorrência de substitutos e capacidade financeira/operacional.

- Dois estabelecimentos do mesmo ramo podem cobrar preços diferentes; não criar preço fixo obrigatório da cidade nem controle de tabela pelo jogador.

- A atualização deve ser gradual, simples e periódica ou por eventos relevantes, evitando ajustes per-frame/por visita ou uma disputa de preços que oscile sem causa.

- Só existe receita quando cliente real recebe serviço real e paga ao caixa da empresa, com equipe, tempo e capacidade disponíveis; sem clientela garantida nem aumento fictício de dinheiro.

- Intensidade e periodicidade dos ajustes ficam para calibração e não substituem a política própria de preços das categorias físicas de mercadorias já especificada.

#### Adaptações físicas de imóvel econômico executadas pela empresa

- Quando a empresa privada que comprou/possui o imóvel escolher uma atividade compatível que exija adaptação da estrutura, ela própria decide, financia e coordena automaticamente as obras necessárias usando seu caixa, materiais reais, trabalho e tempo efetivos, sem autorização individual do jogador, sem transferir a despesa ao Caixa da Cidade e sem exigir comandos manuais de reforma.

- Reutilizar os princípios de obra e logística física definidos, com pagamentos aos recebedores reais e sem gerar mão de obra, materiais, progresso ou dinheiro fictícios; a escolha comercial pode falhar ou ser adiada por falta de recursos, acesso ou viabilidade.

- Não impor obra se o imóvel já atender às exigências físicas nem autorizar mudar de categoria urbana, operar sem adaptações necessárias ou simular vendas durante inatividade.

- Equipamentos específicos, coordenação da mão de obra e destino físico de adaptações ficam para definição/calibração proporcionais antes da implementação dependente, sem criar por antecipação uma segunda cadeia de construção.

#### Mudança de ramo pela própria empresa, sem vender o imóvel

- Uma empresa ativa que já possui e opera um imóvel privado pode decidir autonomamente mudar a atividade comercial/de serviços daquele estabelecimento, preservando a mesma entidade econômica, propriedade, caixa e obrigações, desde que o novo ramo seja permitido pela categoria urbana escolhida pelo jogador, seja compatível com o imóvel ou tenha adaptação física viável, e haja perspectiva econômica real de operação.

- A decisão não exige venda do prédio nem compra por nova empresa e não pode ser comandada ramo a ramo pelo jogador; o fechamento e abertura da operação devem respeitar eventuais obras, materiais, dinheiro, tempo, acesso, contratação e capacidade reais, sem troca mágica de equipamentos/mercadorias, receita instantânea ou clientes artificiais.

- Estoque físico remanescente continua rastreável, com aproveitamento quando válido ou destinação econômica/física legítima, sem apagar/duplicar bens; trabalhadores e vagas seguem as regras de vínculo e condições reais, sem conversão compulsória automática de todo empregado para outra função.

- Evitar alternância frequente por pequenas oscilações de demanda: a troca é uma decisão significativa sujeita a custos e à saúde financeira, não um algoritmo de mudar fachada a cada ciclo.

- **Em aberto:** Critérios detalhados, tratamento do período de adaptação e destino de estoques/funcionários dependentes ainda exigem definição/calibração proporcional, antes do código pertinente.

#### Vínculos de empregados ao mudar de atividade

- Quando a empresa adaptar legitimamente uma unidade para outro ramo, preserva os vínculos de SIMs cuja disponibilidade, jornada e vagas reais forem compatíveis com a nova operação e ajusta/encerra automaticamente os vínculos para os quais não existirem postos viáveis, respeitando custos, salários e obrigações já devidos.

- Não dispensar toda a equipe por padrão nem manter salários por trabalho inexistente; trabalhadores desligados podem procurar outras vagas reais.

- A mudança não cria necessidade de profissões formais ou treinamento inventado, nem concede à empresa permissão para obra nova fora das regras aprovadas.

### Aquisição e operação de comércio e indústria

Ver [Locação comercial entre empresas](#locação-comercial-entre-empresas).

### Ciclo inicial de um prédio econômico privado

#### Expansão patrimonial orgânica da mesma empresa

- A empresa é uma entidade econômica persistente e autônoma, comparável ao SIM quanto a identidade, saldo próprio, patrimônio e continuidade, e pode adquirir e operar diversos imóveis econômicos comerciais/industriais ao longo do tempo, inclusive com atividades compatíveis diferentes, conforme oportunidade, dinheiro realmente disponível, preço, concorrência, mão de obra, localização, insumos, acesso e custos de operação.

- A aquisição é decisão da própria empresa, sem clique do jogador nem expansão obrigatória/garantida quando acumular capital; não impor teto arbitrário de uma unidade ou transferir Caixa da Cidade para comprar seu segundo imóvel.

- A empresa continua com um único caixa/identidade econômica, enquanto cada imóvel/unidade mantém titularidade registrada, estoque físico, produção/atendimento, vagas, funcionários, acesso e operações próprios; as receitas/despesas de todos afetam o caixa da mesma empresa sem duplicar bens ou dinheiro.

- O fechamento/venda/paralisação de uma filial não mata automaticamente a empresa; sua extinção econômica no primeiro modelo decorre da falência terminal real, seguida da liquidação definida, abrangendo os bens e obrigações reais que a empresa ainda detenha.

- Não introduzir grupo empresarial de controladas, subsidiárias, sócios ou acionistas nem duração de vida artificial.

- Critérios de decisão, cadência e apresentação são calibração/implementação simples, sem simular uma holding ou gestão manual de múltiplas filiais.

#### Transferência interna de produtos entre unidades da mesma empresa

- Quando uma empresa possui fábrica, fazenda, comércio ou mais de uma unidade compatível, ela pode decidir abastecer seus próprios estabelecimentos com mercadorias da própria produção/estoque, respeitando origem e destino físicos, quantidades, capacidade, trabalho, caminhão/condutor, acesso, trajeto, frete/combustível e tempo reais.

- A quantidade só sai do estoque de origem e entra efetivamente no destino após a transferência física pertinente, sem teletransporte, duplicação, geração de mercadoria ou venda fictícia para a própria empresa: a mesma entidade com caixa único não realiza pagamento/receita interna para si mesma.

- Pagamentos operacionais reais a trabalhadores, postos ou fornecedores externos permanecem, sem contá-los duas vezes; a operação interna não exclui comprar/vender entre empresas diferentes, exportar ou considerar viabilidade de abastecimento.

- No primeiro modelo, nenhum minigame de transferências ou ordem logística por filial ao jogador; decisões são automáticas conforme disponibilidade e necessidades reais.

### Propriedade, demanda e vulnerabilidade social

- Falência pessoal no primeiro modelo é uma condição econômica simples e derivada, quando o SIM não dispõe de dinheiro suficiente para sustentar suas obrigações e possui dívidas.

- Suas consequências são percebidas no consumo que consegue financiar, nas dívidas reais e na capacidade de manter ou obter moradia, seguindo as regras definidas de aluguel, inadimplência e realocação.

- Não haverá processo jurídico próprio de falência, liquidação patrimonial compulsória como sistema separado, renegociação formal nem perdão automático das dívidas por entrar nesse estado.

- Exceções de extinção de obrigações definidas, como as regras específicas de falecimento, continuam valendo.

- Critérios/limiares exatos de identificação e recuperação ficam para calibração, sem criar dinheiro ou burocracia por SIM.

#### População sem moradia integra o primeiro modelo como consequência real e observável

- A ausência de residência pode afetar indicadores sociais e decisões de migração, com causas legíveis ao jogador.

- A resposta inicial do jogador é indireta, pela ampliação da oferta de moradias e melhoria de empregos, serviços e condições da cidade, usando os sistemas urbanos já previstos.

- Não incluir inicialmente abrigos para adultos sem moradia, moradia subsidiada ou programas gerais de assistência social; a unidade municipal de acolhimento de crianças/adolescentes sem responsáveis é exceção expressamente aprovada, não autorização para outros serviços automáticos.

- **Em aberto:** Métricas e efeitos exatos são calibração; outras formas de assistência permanecem em exploração.

#### SIM sem moradia pode continuar empregado

- Perder a residência não cancela automaticamente o vínculo de emprego.

- O SIM continua apto a comparecer ao trabalho real enquanto tiver acesso, tempo, saúde, disponibilidade e demais condições efetivas, podendo receber salário devido e procurar recuperação econômica.

- A falta de moradia ainda causa prejuízos reais de bem-estar, rotina, acesso e satisfação de necessidades que podem indiretamente inviabilizar o emprego; não tratar SIM sem teto como se tivesse casa/endereço válido, nem criar demissão compulsória ou emprego garantido por essa condição.

- O jogador acompanha causas e efeitos agregados, não reabilita pessoalmente cada trabalhador.

#### Impossibilidade de pagar passagem

- Quando o transporte público municipal estiver sob política paga, um SIM que não possui dinheiro suficiente para a tarifa da viagem não pode embarcar naquela viagem, sem isenção automática, dívida da passagem, dinheiro negativo nem embarque fictício.

- A escolha do percurso pode considerar caminhar por trajeto real viável, outros meios legítimos existentes ou adiar/desistir da viagem; não teletransportar o SIM nem garantir que exista rota a pé acessível.

- A ausência de passagem não gera receita ao Caixa da Cidade e pode causar efeitos reais sobre acesso a empregos, serviços e satisfação de necessidades, diagnosticáveis quando relevantes.

- Se o jogador escolher transporte público gratuito, a regra de falta de dinheiro para a passagem não se aplica, mas operação e capacidade continuam reais.

- Política de tarifas: o jogador escolhe Grátis, Barata, Normal ou Cara para os ônibus municipais; nas três faixas pagas, o jogo determina o valor monetário exato dentro da faixa escolhida, sem edição de preço individual por viagem ou passageiro.

- Valores e cobrança concreta continuam para calibração.

- Não criar passes sociais nem exceções automáticas sem aprovação posterior.

#### Tarifas de ônibus sob controle agregado do jogador

- Na Prefeitura, o jogador escolhe Grátis, Barata, Normal ou Cara para viagens de ônibus municipais, como política geral, com quatro faixas agregadas.

- Cada faixa paga determina uma tarifa real positiva, calculada a partir de referência/custos calibrados e sem número arbitrário por passageiro; a faixa Grátis cobra zero.

- Em qualquer faixa, só o SIM que efetivamente embarcar/viajar e possuir dinheiro real pode pagar e gerar receita efetivamente recebida pelo Caixa, mantendo a regra definida de não embarcar sem saldo suficiente em serviço pago.

- Frota, motorista, combustível, capacidade, rotas e acesso permanecem físicos e pagos pela Prefeitura.

- Tarifa cara não garante maior receita total: pode reduzir demanda e acesso a trabalho/serviços.

- O jogador não administra o preço por linha, motorista ou pessoa; valores numéricos e resposta da demanda são calibração.

### Agricultura

#### Fazendas com área de cultivo predefinida por porte

- No modelo inicial, o jogador escolhe entre fazenda pequena, média ou grande, cada uma ocupando uma área cultivável física compatível com seu porte, verificada no posicionamento e sujeita à construção normal.

- Não é necessário desenhar manualmente o polígono de cultivo nem expandir automaticamente para terrenos vizinhos.

- A capacidade produtiva continua condicionada à área, empregados, insumos e operação reais; tamanhos métricos exatos ficam para calibração.
