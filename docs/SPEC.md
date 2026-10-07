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
- Cada residência deve ter ocupantes/famílias reais.
- Casas representam uma residência/família; prédios residenciais podem conter múltiplas unidades e múltiplas famílias.

### Empresas e economia

- Empresas serão entidades reais da simulação.
- Empresas terão funcionários reais.
- Cada emprego corresponderá a uma **vaga real** dentro de uma empresa ou serviço.
- Empresas terão dinheiro e estado econômico próprio.
- Estoque e/ou produção devem existir como parte do modelo econômico das empresas.
- O consumo será modelado inicialmente por **categorias de produtos**, não por SKU individual.
- Compras reais reduzem estoque real, movimentam dinheiro real e geram necessidade de reposição/logística.
- Empresas poderão falir quando suas condições econômicas levarem a isso.
- Fazendas e agricultura produzem diretamente a categoria **Alimentos** no escopo inicial.
- Mercadorias físicas modeladas como estoque podem ser importadas pela conexão externa quando a oferta local for insuficiente ou inexistente.
- No escopo inicial, os preços externos de importação permanecem estáveis.

### Serviços públicos

- Serviços como escola, hospital, polícia e bombeiros devem funcionar como sistemas reais da cidade, não como bônus abstratos.
- Esses serviços terão capacidade real.
- Esses serviços dependerão de funcionários reais da população simulada.

### Princípio de design: profundidade sem microgerenciamento

- O jogo deve priorizar **decisões sistêmicas e consequências observáveis**, não tarefas repetitivas de administração.
- Realismo deve ser mantido quando cria gameplay; detalhes que apenas adicionam cliques, contabilidade ou manutenção manual podem ser agregados ou automatizados.
- A interface pode apresentar informações agregadas enquanto a simulação mantém localização, transporte, capacidade, escassez e demais consequências físicas internamente.
- Esse princípio vale para todos os sistemas do produto.

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

### Construção e obras

- Prédios e infraestrutura não aparecem instantaneamente prontos.
- Construções passam por uma fase real de obra.
- Toda obra possui custo em **dinheiro e materiais**.
- Obras consomem materiais reais da cidade.
- Obras dependem de trabalhadores reais. No fluxo normal, as equipes do Pátio Municipal de Obras são formadas por cidadãos empregados pela prefeitura; no bootstrap inicial, uma equipe externa temporária pode executar as primeiras obras.
- Materiais de construção precisam estar fisicamente disponíveis e reservados para a obra antes de serem consumidos, seja em estoque municipal ou já entregues no canteiro.
- No início de uma cidade, materiais podem ser importados pela conexão externa usando o dinheiro inicial.
- O custo apresentado da obra deve indicar explicitamente quando materiais faltantes serão importados e quanto essa importação aumenta o custo monetário.
- A cidade precisa possuir infraestrutura física de armazenamento, como depósitos/galpões, para materiais de construção.
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
- Vagas podem exigir níveis de educação e/ou qualificação compatíveis.
- Cada vaga possui salário real.
- Salários são pagos periodicamente ao trabalhador.


### Moradia e finanças públicas

- Famílias podem se mudar de residência conforme fatores como renda, tamanho da família e localização.
- Residências têm preço e/ou aluguel real que afetam o orçamento familiar.
- Cidadãos e empresas pagam impostos reais para a cidade.
- A cidade possui orçamento municipal real, com receitas e despesas.
- O menu financeiro deve separar claramente entradas e saídas da prefeitura.
- Salários de todos os trabalhadores públicos devem aparecer explicitamente entre as despesas municipais.
- A cidade poderá usar empréstimos/dívida municipal para evitar travamentos financeiros e permitir recuperação de caixa.


### Terreno e logística econômica

- Todo mapa jogável deve conter **pelo menos um rio ou um lago**.
- Estabelecimentos comerciais podem fechar quando não conseguem sustentar sua operação, incluindo falta de clientes.
- Indústrias precisam receber matéria-prima real e escoar produção real.
- Caminhões de carga circulam fisicamente entre fornecedores, indústrias, comércio e obras.


### Propriedade, demanda e vulnerabilidade social

- Lotes não precisam ter proprietário individual.
- Toda construção é colocada pelo jogador.
- A cidade deve calcular e expor **demanda** para orientar o que faz sentido construir.
- Cidadãos podem entrar em falência pessoal.
- Pode existir população sem moradia.
- Oferta e demanda devem influenciar preços de aluguel e venda, sem exigir um modelo excessivamente complexo.


### Transporte público e veículos

- O jogo terá transporte público com operação real.
- Ônibus terão linhas, frota, motoristas e capacidade reais.
- Outros modais de transporte público também farão parte do sistema; os tipos exatos serão definidos progressivamente.
- Veículos particulares precisam estacionar de verdade.
- O realismo operacional dos carros deve ser alto, incluindo deslocamento e estacionamento coerentes.
- Veículos consomem combustível.
- Combustível participa de uma cadeia econômica real entre produção/refino, distribuição para postos e consumo pelos veículos.
- Postos mantêm estoque real de combustível e podem ficar sem produto.
- O mesmo princípio de estoque real vale para comércios como padarias, farmácias, mercados e outros estabelecimentos.


### Mobilidade ativa e circulação de pedestres

- Pedestres se deslocam fisicamente pela cidade.
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

- Táxis e transporte sob demanda existirão com motorista e passageiro reais.
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

- Escolas têm professores reais e alunos reais.
- A capacidade escolar depende do quadro de professores e da quantidade de alunos atendidos.
- O jogo terá ensino superior/universidade.
- Professores são trabalhadores reais, ocupando vagas reais.
- Educação e qualificação influenciam acesso a empregos e salários.
- Crianças e adolescentes podem ficar sem escola quando não houver vaga ou acesso adequado.


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
- Toda receita e movimentação relevante de exportação deve aparecer de forma clara nas finanças da cidade.


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
- A política de devolução de valores monetários em cancelamento de obra ainda não está definida.
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
