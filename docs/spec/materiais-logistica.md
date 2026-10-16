# Materiais e logística

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Histórico das decisões em [docs/decisoes/materiais-logistica.md](../decisoes/materiais-logistica.md).

## Em vigor no jogo (V1)

### Pilar de gameplay: economia material

- A expansão da cidade não deve ser resolvida apenas por dinheiro: materiais físicos são um recurso central de gameplay.
- Construir a cidade exige obter materiais por produção local e/ou importação.
- Produção, armazenamento, transporte e consumo desses materiais formam cadeias observáveis que geram atividade econômica, logística e empregos.
- Dinheiro continua relevante para pagar obras, importações, frete, operação e outros custos, mas não substitui a disponibilidade física dos materiais.

### Recursos físicos iniciais

- A primeira base de recursos físicos do jogo é composta por **oito categorias**. As categorias abaixo estão no recorte V1; **Suprimentos médicos**, a oitava categoria, permanece descrita em [Recursos físicos iniciais da Visão aprovada](#recursos-físicos-iniciais-1):
- Alimentos — consumo da população.
- Combustível — veículos, logística e serviços.
- Areia e brita — base viária, drenagem e insumo de concreto/asfalto.
- Concreto — material final de construção.
- Aço — material estrutural agregado em uma categoria.
- Madeira — material de construção agregado em uma categoria.
- Asfalto — vias e superfícies pavimentadas.
- O nome exibido ao jogador será Areia e brita, evitando o termo técnico pouco intuitivo "agregados".
- Água, eletricidade, esgoto e lixo permanecem sistemas de serviço/capacidade, não recursos de estoque equivalentes a essas categorias.

### Terreno e logística econômica

- Todo mapa jogável deve conter pelo menos um rio ou um lago.
- Estabelecimentos comerciais podem fechar quando não conseguem sustentar sua operação, incluindo falta de clientes.
- Indústrias precisam receber matéria-prima real e escoar produção real.
- Aquisição autônoma de caminhões e frota inicial da fábrica: fazendas, indústrias e demais empresas fornecedoras podem adquirir autonomamente caminhões físicos, quando tiverem necessidade operacional, caixa e oferta viável.
- Quando uma empresa adquirir e preparar a operação de uma fábrica que realmente precise realizar entregas, seu investimento inicial inclui a aquisição automática de uma frota-base pequena, proporcional à necessidade de transporte, se puder custeá-la, sem intervenção do jogador.
- O custo sai do caixa da empresa adquirente, NÃO do Caixa da Cidade, que já financiou a construção do prédio; a frota não acompanha gratuitamente o prédio vazio nem surge na compra sem pagamento.
- A empresa compra de vendedor real local, quando existir oferta vigente, ou importa via conexão exterior, com custo, entrada/entrega física, titularidade e disponibilidade individuais.
- Sem caminhão entregue, motorista SIM, combustível, rota ou dinheiro, a empresa não realiza entregas fictícias, podendo operar com capacidade logística limitada ou aguardar suprimento.
- A empresa expande a frota automaticamente conforme necessidade e solvência; não obrigar estabelecimentos sem entregas a comprar caminhões, nem criar transportadora ou gestão manual de veículos.
- Quantidade mínima exata e cronograma de compra são calibração, não licença para frota infinita nem regra fixa de um veículo por estabelecimento.
- Caminhões de carga circulam fisicamente entre fornecedores, indústrias, comércio e obras.
- Entregas comerciais locais na primeira implementação integrada — responsabilidade do fornecedor: quando um comércio compra mercadorias de uma fazenda, indústria ou outro fornecedor local, a empresa fornecedora organiza e executa a entrega ao comprador, utilizando seus veículos de carga e motoristas reais da simulação.
- Caminhões partem da origem física do estoque e seguem a rede viária até o ponto de carga/descarga do comprador, com carga, capacidade, distância, trânsito e tempo de entrega reais; a quantidade só entra no estoque do comércio depois da entrega.
- Disponibilidade operacional de veículos e motoristas limita as entregas, podendo causar espera e falta de produtos sem geração ou teletransporte de carga.
- O frete é custo econômico real da operação, coerente com a formação de preços vigente; como será incorporado/faturado entre fornecedor e comprador fica para calibração, sem pagamento duplicado ou recebedor fictício.
- A entrega é coordenada automaticamente pela simulação, sem despacho manual do jogador, nem uma transportadora independente obrigatória no primeiro modelo.
- Importações continuam usando veículos e motoristas externos pela conexão com o exterior, conforme suas regras próprias.
- Origem/disponibilização da frota inicial, custos operacionais específicos, escala de entregas e detalhes de alocação de motoristas ficam para prototipagem, sem novo sistema formal de cargos.

### Caminhões reconhecíveis pelo material

- Entregas de Areia e brita usam caminhão basculante com carga visível; Asfalto usa basculante com carga escura e cobertura; Concreto usa caminhão-betoneira; Aço e Madeira usam carroceria aberta com feixes ou peças presos. O caminhão fechado permanece nas entregas de mercadorias compatíveis com sua função.
- Cada caminhão físico conserva seu modelo e sua carroceria também entre entregas, inclusive após retornar ao exterior: uma betoneira continua betoneira, um basculante continua basculante e uma carroceria aberta continua aberta. A frota é organizada automaticamente conforme compatibilidade real de material, sem transformar um mesmo veículo em outro modelo, duplicar capacidade ou exigir escolha manual. Variações de carga e cobertura compatíveis com a mesma carroceria acompanham o material e a quantidade realmente transportados; o retorno vazio não muda o modelo.
- Caçamba, descarga e betoneira só apresentam trabalho de entrega quando a operação correspondente realmente ocorre. Peças móveis são controladas pelo estado da simulação, sem descarga decorativa, carga fictícia ou animação que faça a obra avançar por si.
- A diferenciação inicial preserva capacidades, custos e regras logísticas existentes. Não exige que o jogador compre, escolha ou despache cada modelo; não acrescenta perecibilidade do concreto, temperatura do asfalto, novas receitas ou gestão manual de frota.
- **Em aberto:** em saves anteriores à diferenciação, inferir a carroceria de todas as cargas em viagem pode deixar a frota sem algum modelo necessário. Permitir uma única redistribuição de modelos ao retornar ao exterior durante a conversão do save é uma exceção ainda não aprovada à regra de modelo fixo; não implementar essa troca nem criar caminhões extras por inferência. Quantidades, cargas, dinheiro e identidades existentes devem ser preservados enquanto essa compatibilidade é definida.

### Comércio exterior e logística externa

- Caminhões vindos do exterior podem ser operados por motoristas externos, que não pertencem à população simulada da cidade.

### Fluxo de importação para obras

- Quando uma obra específica depende de material importado, os caminhões externos podem entregar diretamente no canteiro a quantidade necessária para aquela obra.
- Essa entrega direta não exige passagem pelo Centro de Materiais de Construção.
- Para materiais de construção, não há importação genérica para formar estoque no escopo atual; a importação ocorre quando uma obra exige recursos indisponíveis localmente.

### Disponibilidade, reserva e prioridade de materiais de obra

- Para o jogador, materiais de construção são apresentados como Disponível na cidade, agregando a oferta local elegível.
- Essa visão agregada não teletransporta materiais: cada quantidade continua existindo em uma localização física real, como Centro de Materiais, Pátio, fábrica ou fornecedor.
- Ao confirmar uma obra, o sistema reserva automaticamente materiais locais elegíveis.
- Materiais reservados deixam de estar disponíveis para outras obras.
- Se faltar material local, o sistema importa a quantidade necessária conforme as regras de importação já definidas.
- Caminhões continuam obrigatórios para levar o material de sua origem física até o canteiro.
- Quando várias obras disputam a mesma oferta local, a prioridade inicial segue a ordem em que as obras foram criadas.
- Prioridade automática de obras sem reordenação manual: quando novas quantidades de materiais locais ainda livres e elegíveis forem disputadas por obras, a alocação segue a ordem de criação das obras, conforme reserva automática vigente; o jogador não dispõe de controle para mudar essa fila manualmente no modelo inicial.
- Material já reservado para outra obra permanece comprometido, e cargas entregues/consumidas não são retiradas ou recuperadas por mudança de prioridade.
- Pedidos/importações e entregas em trânsito seguem seus compromissos econômicos e logísticos reais, sem troca retroativa, estorno ou teletransporte; não criar prioridades especiais automáticas por tipo de prédio no modelo inicial.
- Não existe sistema de prioridade adicional.

### Política inicial de importação de materiais

- No escopo atual, a importação de materiais de construção acontece em resposta a uma obra que exige recursos indisponíveis localmente.
- Não existe compra genérica automática de materiais apenas para encher estoque.
- O custo de frete da importação faz parte do custo monetário total exibido para a construção.

### Pátio Municipal de Obras

- A execução das obras municipais depende de um Pátio Municipal de Obras construído pelo jogador.
- O Pátio emprega trabalhadores públicos reais da população simulada.
- Esses trabalhadores recebem salários pagos pela prefeitura e registrados explicitamente nas despesas municipais.
- O Pátio possui capacidade operacional limitada, determinada pela quantidade de equipes/trabalhadores públicos disponíveis para executar obras.
- Quando a capacidade local do Pátio é insuficiente para uma nova obra, a obra não precisa esperar: a prefeitura pode contratar/importar uma equipe externa temporária, aumentando o custo monetário daquela construção.
- O Pátio também possui um pequeno estoque físico de materiais de construção.

### Bootstrap inicial de construção

- A cidade não começa com o Pátio Municipal de Obras pronto.
- As primeiras obras necessárias para colocar o sistema municipal em funcionamento podem ser executadas por uma equipe externa temporária.
- Essa equipe e seus trabalhadores vêm pela conexão externa e não pertencem à população residente da cidade.
- A equipe externa pode executar o primeiro Pátio Municipal de Obras e a infraestrutura mínima necessária para viabilizá-lo.
- Materiais dessas primeiras obras continuam obedecendo às regras normais de custo, importação e entrega física.
- Depois que o Pátio Municipal de Obras entra em operação, as obras municipais passam ao fluxo normal com trabalhadores públicos da cidade.

### Capacidade local e mão de obra externa de obras

- Obras usam equipes locais do Pátio Municipal de Obras quando houver trabalhadores públicos disponíveis.
- A quantidade de trabalhadores/equipes disponíveis define quantas obras podem ser atendidas localmente.
- Nota de escopo: o dimensionamento de fábricas de insumos não altera o limite físico e a operação do Pátio de Obras.
- Se não houver capacidade local suficiente, a prefeitura pode contratar uma equipe externa temporária pela conexão externa.
- O uso de equipe externa aumenta o custo monetário da obra e deve aparecer explicitamente no custo apresentado ao jogador.
- Falta de capacidade de mão de obra local, por si só, não bloqueia a criação de novas obras.

### Consumo de materiais no canteiro

- Os materiais necessários para uma obra são consumidos de uma vez quando a carga correspondente chega ao canteiro.
- A obra não precisa exibir consumo gradual de materiais ao longo do progresso.

### Armazenamento por tipo de recurso

- Alimentos, combustível e suprimentos médicos não usam o Centro de Materiais como estoque normal.
- Esses bens usam armazenamento coerente com sua função, como estoques de mercados/comércios, postos de combustível, hospitais, farmácias e outras instalações apropriadas.

### Abastecimento inicial de alimentos e combustível

- Mercados/comércios podem importar automaticamente Alimentos quando a oferta local não atender à reposição ou quando a alternativa externa for significativamente melhor na comparação econômica, respeitando a preferência local moderada e os critérios de custo total, disponibilidade e prazo já definidos para abastecimento comercial.
- A importação de Alimentos paga produto e frete e gera entrega física.
- No escopo inicial, Combustível pode ser importado já refinado/pronto pela conexão externa e distribuído fisicamente aos postos.

### Visão agregada de materiais

- A interface mostra a oferta de materiais de construção de forma agregada em nível da cidade.
- Para cada material, o jogador deve conseguir entender pelo menos o total disponível localmente, o que está reservado, o que está em trânsito e quanto precisará ser importado para uma nova obra.
- A agregação é apenas de interface e decisão; localização física, transporte, capacidade de armazenamento e congestionamento continuam simulados.

## Visão aprovada (ainda não implementada)

### Recursos físicos iniciais

- Abastecimento hospitalar automático com estoque físico: hospitais públicos consomem Suprimentos médicos reais quando aplicáveis e repõem seus estoques automaticamente por aquisição efetiva de fornecedores locais ou importação exterior, com Caixa da Cidade, preço, capacidade, transporte e entrega física, sem pedido manual por hospital.
- Suprimentos ausentes podem limitar o atendimento conforme operação real, sem reposição gratuita ou teletransporte.
- O jogador atua sobre orçamento, capacidade e condições urbanas; critério de compra e estoque de segurança ficam para calibração.
- Suprimentos médicos — medicamentos e consumíveis de saúde agregados em uma categoria.
- Insumos industriais intermediários só precisam aparecer quando a respectiva cadeia produtiva existir.
- Exemplos incluem cimento, culturas/produtos agrícolas, madeira em tora, ligante asfáltico/bitume e pedra bruta.
- Jazidas de Areia e brita sem esgotamento individual no primeiro modelo: o mapa gerado por seed inclui depósitos/áreas minerais reais identificáveis em que operadores econômicos compatíveis podem extrair Areia e brita.
- A jazida não perde um contador finito de reserva nem deixa de funcionar só por ter produzido durante anos de simulação; essa persistência é uma simplificação deliberada de gameplay, não regeneração geológica real.
- Cada jazida tem produção limitada por período, condicionada ao espaço físico ocupado e efetivamente explorável, à operação, aos trabalhadores, à capacidade disponível, ao tempo e à logística, com produto físico em estoque, transporte, custos e transações reais.
- Se sua área ou estrutura produtiva deixar de existir/operar, sua oferta para.
- O jogador aumenta oferta construindo outras unidades viáveis ou importando, sem repor manualmente minas esgotadas; não existe material pronto ou estoque infinitos.
- Outros minerais/insumos podem continuar vindo de importação real e não se exige mineração ampla ou múltiplos novos minérios.
- Posicionamento das jazidas, rendimento e capacidade são calibração.
- Madeira de fonte florestal renovável com produção limitada: no modelo inicial, áreas florestais produtivas permitem obter Madeira continuamente, sem consumir um estoque finito de árvores individuais nem exigir plantio ou replantio manual.
- Isso representa manejo/regeneração florestal agregados, não madeira surgida gratuitamente: cada quantidade produzida exige área florestal produtiva efetivamente disponível, operador, trabalhadores, capacidade operacional e tempo, com custos econômicos, estoque físico e transporte reais segundo as regras existentes.
- A produção por período é limitada pela área e operação; estoques, vendas e entregas não são infinitos.
- A ocupação ou perda efetiva de área florestal produtiva reduz a capacidade correspondente.
- O jogador administra área e infraestrutura, não cada árvore nem ciclos individuais de plantio/corte.
- Silvicultura com plantio, crescimento e colheita explícitos fica como possível evolução futura, não aprovada agora.
- Esta regra não torna automaticamente renováveis ou inesgotáveis jazidas, combustível ou insumos industriais; estender o princípio exige decisão por origem de recurso.
- Cadeia inicial curta de materiais de construção: a produção local dos materiais finais de obra pode ocorrer em fábricas com insumos físicos reais, obtidos de fornecedores existentes ou por importação efetiva quando não houver produção local.
- Cada transformação consome estoque, recursos, tempo, trabalho e dinheiro conforme a economia já definida; não surge material sem origem.
- Não exigir, neste modelo, uma cadeia local completa de extração e beneficiamento para todo insumo intermediário.
- Cadeia produtiva local de petróleo e combustível: além das oito categorias físicas finais/iniciais, a cadeia local possui um insumo intermediário físico distinto chamado Petróleo bruto.
- O mapa gerado pela seed pode conter jazidas de petróleo exploráveis por instalações de extração reais; empresas operadoras com trabalhadores, equipamentos/capacidade, energia, recursos e caixa real extraem petróleo bruto, que entra em estoque na origem e é transportado fisicamente até refinarias operacionais.
- Refinarias são edifícios industriais próprios construídos no mapa, operados por empresas privadas compatíveis, que consomem petróleo bruto, trabalho, energia, dinheiro e tempo para fabricar quantidades reais de Combustível, distribuídas fisicamente a postos/clientes pela logística existente.
- Combustível pronto pode continuar sendo importado, especialmente para iniciar a cidade vazia; não tornar refinaria local pré-requisito universal de veículos/serviços.
- Não aprovar automaticamente transporte por oleodutos, subprodutos petroquímicos, profissões especializadas, refino microgerenciado ou novos derivados.
- Distribuição geográfica das jazidas, rendimentos, preços e detalhes de assets ficam para calibração proporcional; as jazidas também não se esgotam no primeiro modelo, sem estoque ou vazão infinitos.
- Jazidas de petróleo sem esgotamento por volume no modelo inicial: as jazidas geográficas de petróleo exploráveis não possuem contador de reserva que zera com a produção acumulada, simplificação consciente igual à de Areia e brita.
- A produção real por período é limitada por área/instalação, equipe, capacidade, operação, tempo, recursos, caixa e transporte.
- A extração gera petróleo bruto em estoque apenas quando operada, sem fornecimento ou combustível refinado infinitos, e pode cessar se o local/empresa não estiver operacional.
- Reservas finitas e exaustão são possíveis melhorias futuras NÃO aprovadas.
- Importação física de petróleo bruto para refinarias: empresas operadoras de refinarias podem comprar e importar petróleo bruto real da conexão exterior, com pagamento do próprio caixa empresarial à Reserva Global e transporte/entrega físicos ao estoque da refinaria, sujeitos a preço, capacidade, acesso e disponibilidade econômica legítimos.
- Essa opção complementa, não substitui, a extração local e a importação vigente de Combustível pronto.
- Não importar petróleo gratuitamente, teletransportar matéria-prima para a usina, garantir oferta infinita de caminhões ou transformar o refino em atividade gerenciada por cada pedido do jogador.

### Comércio exterior e logística externa

- A cidade pode exportar excedentes produzidos localmente pela conexão externa.
- Exportações e importações usam fluxos físicos pela conexão externa.

### Centro de Materiais de Construção

- O armazenamento principal de materiais de construção é tratado como Centro de Materiais de Construção, evitando que a interface force uma distinção de propriedade pública/privada.
- O Pátio Municipal de Obras pode manter um estoque pequeno; o Centro de Materiais oferece capacidade significativamente maior.
- O Centro funciona como infraestrutura física de armazenamento e logística da cidade; sua existência não implica que todo material armazenado pertença juridicamente à prefeitura.

### Capacidade e operação do Centro de Materiais

- Centros de Materiais de Construção possuem capacidade física limitada.
- No escopo inicial, um mesmo Centro pode armazenar todos os tipos de materiais de construção.
- Centros possuem capacidade limitada de carga e descarga simultânea.
- Saturação de carga/descarga pode gerar fila física de caminhões.

### Exportação automática

- No escopo inicial, excedentes elegíveis podem ser exportados automaticamente.
- O pagamento externo sai da Reserva Global e vai para o agente econômico que efetivamente vende a mercadoria; exportação privada não vira automaticamente receita do Caixa da Cidade.
- Toda receita e movimentação relevante de exportação deve aparecer de forma clara na leitura econômica/financeira da cidade.

### Pátio Municipal de Obras

- O estoque do Pátio tem capacidade menor que a de um Centro de Materiais de Construção.
- Centros de Materiais continuam sendo a infraestrutura principal para armazenamento de grandes quantidades de materiais.

### Armazenamento por tipo de recurso

- Centros de Materiais de Construção armazenam materiais de construção: areia e brita, concreto, aço, madeira e asfalto.

### Abastecimento inicial de alimentos e combustível

- Combustível inicial real nos veículos municipais: ambulâncias, viaturas, caminhões de coleta, bombeiros e outros veículos municipais adquiridos no pacote inicial da instalação entram em operação com combustível real em seus tanques, cujo custo e obtenção estão incluídos no investimento efetivo; não são abastecidos gratuitamente nem recebem autonomia infinita.
- Isso permite a primeira utilização mesmo sem posto operante.
- Após gastar o combustível inicial, o abastecimento segue obrigatoriamente a regra vigente de postos reais, com estoque e pagamento efetivo; ausência de posto acessível pode interromper o serviço.
- Produção/refino local de Combustível para o modelo inicial, conforme as regras de extração, refino e importação de petróleo bruto: extração real de Petróleo bruto em jazidas limitadas por capacidade, importação real opcional de petróleo bruto e refinarias locais com insumos, trabalho, energia, custos, tempo e distribuição físicos.
- A primeira operação da cidade ainda pode usar Combustível refinado importado, sem exigir refinaria/extração antes do funcionamento inicial.
