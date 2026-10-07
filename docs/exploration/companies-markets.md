# IndexCities — Empresas, mercados e finanças da cidade

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa; várias decisões conceituais já foram promovidas para a SPEC — não é fonte de verdade.
>
> Reúne empresas privadas, demanda, aquisição de ativos, caixa empresarial, falência, abastecimento, conexão econômica externa e interpretação do Caixa da Cidade.

## Como ler este documento

Este arquivo é material de exploração temática. A autoridade do produto continua sendo `docs/SPEC.md`. Trechos históricos podem preservar alternativas já superadas; quando houver divergência, vale a decisão canônica mais recente.

---

## Agricultura e produção local de alimentos

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido em nível de produto; cadeia detalhada ainda aberta.

- Fazendas/agricultura existirão.
- A produção agrícola gera alimento real para abastecer a economia da cidade.
- A produção deve integrar estoque, transporte e consumo já definidos.

Decidido para o primeiro escopo:

- fazendas produzem diretamente a categoria **Alimentos**;
- produtos agrícolas separados e processamento alimentar ficam para uma etapa posterior.

Ainda precisa ser pesquisado/calibrado:

- quais tipos visuais de produção agrícola entram primeiro;
- necessidade de água, trabalhadores, veículos e armazenamento;
- produtividade por área;
- quando vale aprofundar processamento por indústrias alimentícias.


---

---

---

## Importação externa e preços iniciais

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

Decidido:

- mercadorias físicas podem ser importadas enquanto a cidade não as produz localmente em quantidade suficiente;
- importações usam a conexão externa;
- preços externos ficam **estáveis no escopo inicial**, sem mercado externo dinâmico.

Para implementação, o preço deve ser um dado de balanceamento configurável pelo projeto, mas isso não implica necessariamente uma opção exposta ao jogador.


---

---

---

## Transparência das finanças municipais

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido.

O orçamento da prefeitura deve deixar explícito de onde o dinheiro entra e para onde sai.

No mínimo, a interface financeira precisa separar categorias como:

**Entradas**
- impostos;
- exportações;
- outras receitas municipais que forem adicionadas futuramente.

**Saídas**
- salários de trabalhadores públicos;
- importação de materiais;
- frete;
- construção;
- operação/manutenção de serviços;
- pagamento de dívida quando aplicável.

A folha de pagamento dos funcionários públicos não deve ficar escondida dentro de um custo genérico de serviço ou de obra.

Isso também evita dupla cobrança: se um trabalhador do Pátio já recebe salário da prefeitura, esse salário não deve ser cobrado novamente como se fosse um custo separado da mesma mão de obra na obra.


---

---

---

## Abastecimento de alimentos e combustível no primeiro escopo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido.

### Alimentos

Fluxo inicial:

fazenda → Alimentos → transporte → mercado/comércio → cidadão

Se a cidade não produzir Alimentos suficientes:

conexão externa → caminhão → mercado/comércio → cidadão

A importação automática paga produto + frete. O estoque continua físico e pode acabar.

No primeiro escopo, não é necessário separar colheitas, matéria-prima agrícola e alimento processado. Essa profundidade entra apenas quando gerar gameplay suficiente.

### Combustível

Fluxo inicial:

conexão externa → combustível refinado/pronto → transporte → posto → veículo

Produção/refino local continua desejada como aprofundamento econômico, mas não é pré-requisito para a cidade funcionar no primeiro escopo.

### Armazenamento

- Centro de Materiais de Construção: materiais de construção;
- mercados/comércios: Alimentos;
- postos: Combustível;
- hospitais/farmácias/instalações adequadas: Suprimentos médicos.

Essa separação substitui qualquer interpretação anterior de que um único depósito municipal armazenaria todos os oito recursos.


---

---

---

## Propriedade e controle: prefeitura versus empresas privadas

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido. O jogador financia e coloca as construções; a operação econômica posterior pode ser privada.

"Prefeitura" significa o **setor público municipal controlado pelo jogador**, com orçamento próprio. Já estão claramente municipais:

- Pátio Municipal de Obras;
- serviços públicos como escolas, hospitais, polícia, bombeiros e infraestrutura municipal;
- depósitos municipais de materiais de construção.

A dúvida ainda aberta é quem possui e financia atividades econômicas como:

- fazendas;
- concreteiras;
- usinas de asfalto;
- mercados;
- postos;
- fábricas;
- outros comércios e indústrias.

Três modelos possíveis:

**1. Economia municipalizada**
- a prefeitura constrói, possui e opera essas empresas;
- paga construção, salários, importações e operação;
- recebe diretamente toda receita;
- gameplay mais próximo de uma economia planejada.

**2. Economia privada**
- empresas privadas possuem dinheiro, custos, salários, estoque e lucro;
- pagam impostos à prefeitura;
- o jogador ainda pode decidir a localização, conforme a regra atual de posicionamento direto, mas propriedade e caixa são privados;
- exige definir quem financia a construção inicial e como uma empresa privada nasce.

**3. Modelo híbrido**
- prefeitura possui serviços e infraestrutura pública;
- empresas econômicas são privadas;
- o jogador controla o desenho/localização da cidade, mas caixa operacional e lucro pertencem às empresas;
- subsídios, incentivos ou investimento municipal podem ser adicionados apenas se houver necessidade.

Antes de decidir, é preciso preservar coerência com as regras já existentes:
- o jogador posiciona diretamente todos os prédios;
- prédios econômicos colocados pelo jogador passam a ser operados por empresas privadas;
- empresas têm estado econômico próprio e podem falir;
- a granularidade exata do dinheiro/caixa empresarial foi reaberta e não deve ser tratada como decisão fechada.

O modelo privado ou híbrido parece mais compatível com empresas terem caixa próprio e poderem falir, mas o financiamento da construção precisa ser definido antes de virar requisito.


---

---

---

## Dinheiro privado por empresa: validação de gameplay

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido após pesquisa e revisão de causalidade. Caixa real por empresa foi escolhido, sem microgestão bancária pelo jogador.

Foi confirmado que fazendas, mercados, postos, fábricas e demais atividades econômicas colocadas pelo jogador podem ser operadas por empresas privadas.

A questão em aberto é se cada empresa precisa ter um **saldo bancário explícito e rígido** ou se basta simular receitas, custos, lucro/prejuízo e saúde financeira de forma mais abstrata.

Critério transversal: manter detalhe econômico quando ele cria decisões ou consequências úteis; abstrair quando ele vira contabilidade que o jogador não controla diretamente.



### Decisão final: caixa real auditável por empresa

O primeiro escopo usará **caixa monetário real por empresa**.

A escolha não é para aumentar microgestão; é para evitar caixas-pretas e dar uma fonte de verdade econômica que possa ser calibrada e depurada.

Fluxo conceitual:

capital privado inicial
+ vendas/receitas
- salários
- insumos
- frete
- impostos
- demais custos explícitos
= caixa da empresa

Regras:

- toda movimentação relevante precisa ter origem e destino identificáveis;
- o jogador não transfere dinheiro manualmente entre empresas;
- a UI superficial mostra situação e causa curta;
- a visão detalhada permite reconstruir receitas, despesas e evolução do caixa;
- indicadores como "saudável" ou "em risco" podem existir apenas como resumo derivado, nunca como variável econômica mágica;
- não existe bailout invisível;
- crédito e recapitalização ficam fora do escopo até serem decididos explicitamente.

A empresa recebe **capitalização operacional inicial** como parte do custo da construção privada pago pelo caixa da cidade. A transferência é explícita, quantificada e registrada no ledger. O valor exato deve ser um parâmetro de balanceamento e pode variar por tipo/escala de empresa.

A relação foi fechada: o caixa da cidade financia a construção ordenada pelo jogador e também a capitalização operacional inicial da empresa privada.

### Por que esta opção venceu

Em comparação com apenas lucro/prejuízo acumulado ou um estado abstrato:

- permite saber exatamente se a empresa consegue pagar uma compra;
- permite reproduzir e explicar falências;
- permite medir efeitos de salários, impostos, frete e preços;
- facilita telemetria e balanceamento;
- preserva a possibilidade futura de crédito, bancos e investimentos sem exigir esses sistemas agora.

O custo adicional de simulação não deve virar custo de operação para o jogador: a contabilidade acontece automaticamente.

### Evidência comparativa

**Cities: Skylines II**

A Paradox descreve uma economia com quatro tipos de entidade monetária: cidade/jogador, famílias, empresas e **investidores abstratos**. Empresas compram recursos, pagam salários, aluguel e custos de transporte; avaliam lucro, ajustam produção e podem falir se não conseguirem voltar à lucratividade. Ao mesmo tempo, o próprio design declara que a cadeia produtiva foi feita para que o jogador **não precise microgerenciá-la**, embora possa investigar seus detalhes.

A revisão Economy 2.0 também foi motivada por feedback de que a simulação econômica estava pouco transparente e dava pouco controle ao jogador — um alerta direto contra adicionar complexidade financeira invisível sem benefício de decisão.

Fontes:
- Paradox — Cities: Skylines II, Economy & Production: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production
- Paradox — Economy 2.0 Part 1: https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-one

**Manor Lords**

Manor Lords usa **Regional Wealth** como riqueza agregada da região para importações e investimentos econômicos, separada do Treasury pessoal. O jogo não exige um saldo bancário individual para cada oficina ou produtor.

Fonte:
- Manor Lords Official Wiki — Regional Wealth: https://wiki.hoodedhorse.com/Manor_Lords/Regional_wealth

**Workers & Resources: Soviet Republic**

O jogo demonstra o extremo oposto: a economia doméstica pode funcionar quase sem dinheiro interno, com profundidade concentrada em recursos, trabalho, produção, transporte e comércio exterior. A moeda é principalmente relevante para importações/exportações. Apesar do contexto de economia planejada ser diferente do IndexCities, ele prova que **realismo físico e econômico não depende de um saldo bancário por empresa**.

Fonte:
- Workers & Resources Official Wiki — Economy/overview: https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Workers_%26_Resources%3A_Soviet_Republic

### Leitura para o IndexCities

Há três níveis possíveis:

**A. Caixa bancário explícito por empresa**
- cada empresa tem saldo em moeda;
- toda compra e pagamento exige liquidez imediata;
- empréstimos/capitalização passam a ser necessários para evitar falências artificiais;
- é o modelo mais realista contabilmente, mas cria grande complexidade e risco de comportamento opaco.

**B. Contabilidade econômica sem caixa rígido — alternativa considerada, não escolhida**
- cada empresa registra receita, salários, insumos, frete, impostos e lucro/prejuízo;
- compra e venda continuam movimentando valores entre agentes para fins econômicos;
- porém o jogo não exige que o jogador acompanhe ou administre uma conta bancária individual;
- sobrevivência depende de lucratividade/saúde financeira ao longo do tempo, não de um saldo instantâneo zerar;
- investidores iniciais são abstratos e fornecem o capital de abertura da empresa.

**C. Economia privada totalmente agregada**
- empresas não têm nem P&L individual relevante;
- só existe riqueza privada agregada da cidade;
- simples, mas enfraquece decisões já desejadas como empresas falirem individualmente e salários/custos influenciarem cada negócio.

### Recomendação anterior — superada

A recomendação inicial de começar pelo modelo **B** foi substituída após a revisão de causalidade. O modelo escolhido é caixa real auditável por empresa, com UI em camadas e sem microgestão bancária.

Uma empresa privada deve ter um **estado econômico individual**, mas não precisa de uma carteira que o jogador trate como recurso separado.

Ao selecionar a empresa, mostrar apenas informação útil:
- receita;
- salários;
- custo de insumos;
- frete;
- impostos;
- lucro/prejuízo;
- situação: saudável / pressionada / risco de falência.

Não exigir do jogador:
- transferir capital entre empresas;
- acompanhar saldo bancário diário;
- aprovar empréstimos empresariais;
- financiar capital de giro manualmente.

Para nascer, a empresa recebe capital de **investidores privados abstratos**. Isso é coerente com o jogador controlar desenvolvimento urbano sem precisar simular investidores individuais.

Se a empresa operar com prejuízo persistente, reduz produção/emprego e eventualmente fecha. Isso preserva consequência econômica real sem fazer a simulação depender de uma contabilidade de caixa completa.

### Critério para aprofundar depois

Só promover saldo bancário, crédito, dívida empresarial e investidores individuais a sistemas reais se aparecer gameplay que dependa deles, por exemplo:
- bancos;
- juros;
- crises de crédito;
- investimentos privados concorrentes;
- aquisição/fusão de empresas;
- políticas municipais de financiamento.



### Revisão após o princípio de causalidade

A preocupação com caixa-preta muda a recomendação anterior.

Não é suficiente ter apenas um estado abstrato "saudável / pressionada / falindo". O estado econômico precisa ser **derivado de fluxos concretos e auditáveis**.

Direção decidida para POC:
- caixa real por empresa;
- vendas/receitas reais;
- salários reais;
- custo real de insumos;
- frete real;
- impostos reais;
- demais custos definidos explicitamente;
- resultado econômico e saldo calculados a partir desses fluxos;
- capitalização inicial explícita e inspecionável.

A UI normal não precisa mostrar toda contabilidade, mas o painel detalhado/debug deve conseguir reconstruir por que a empresa piorou.

A comparação foi encerrada em favor de **caixa explícito real**, por ser o modelo mais previsível, calibrável e explicável. A operação desse caixa continua automática para evitar microgerenciamento.

---

---

---

## Revisão: quem paga quando o jogador constrói

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido para o escopo atual.

A discussão sobre "investidor privado paga a construção" estava criando uma camada conceitual que brigava com a regra central já decidida: **o jogador posiciona diretamente todos os prédios**.

A direção escolhida é mais simples e mais coerente com a gameplay:

1. o jogador escolhe o prédio;
2. o jogo mostra dinheiro + materiais necessários;
3. o **caixa da cidade** paga o custo monetário;
4. a oferta de materiais da cidade é reservada/importada;
5. a obra acontece fisicamente;
6. se for uma atividade privada, uma empresa privada passa a operar o prédio após a inauguração.

### Construção privada

O custo mostrado para uma construção privada inclui:
- construção;
- materiais/frete conforme as regras já definidas;
- **capitalização operacional inicial** da empresa.

Quando o prédio entra em operação, essa capitalização é transferida para o caixa real da empresa. Não existe um "investidor mágico" criando dinheiro fora do sistema.

Assim, o jogador continua tendo um loop único e previsível:

**construir = gastar dinheiro da cidade + consumir materiais**.

A distinção público/privado começa depois:
- prédio público: operação e salários continuam saindo do caixa da cidade;
- prédio privado: operação passa para o caixa da empresa; a cidade recebe efeitos indiretos e impostos.

### Trade-off de realismo

Esse modelo significa que a cidade/jogador financia a implantação de atividades privadas. Não é uma representação literal de uma economia de mercado.

A escolha é consciente porque:
- o jogador já controla diretamente onde cada prédio nasce;
- criar um mercado de investidores separado retiraria o controle do jogador ou criaria um segundo sistema monetário de construção;
- um único custo de construção é mais legível;
- mantém o pilar dinheiro + materiais;
- permite que a economia privada fique profunda **na operação**, onde receita, custo, emprego, estoque e falência criam consequências úteis.

Se futuramente o produto adotar desenvolvimento privado autônomo/zoneamento, essa decisão deve ser reavaliada.

### Capitalização inicial

A capitalização não é uma abstração invisível:
- aparece como parte do custo detalhado da construção privada;
- tem valor explícito;
- é transferida do caixa da cidade para o caixa da nova empresa;
- pode ser calibrada por tipo/escala;
- entra no ledger da empresa como saldo inicial.

Isso preserva conservação monetária e facilita diagnóstico.


---

---

---

## Falência empresarial sem simular processo jurídico

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção decidida; parâmetros ainda precisam de calibração.

Não é necessário simular legislação de insolvência, credores, tribunais ou processo jurídico.

A empresa tem caixa e ledger reais. Se sua operação se deteriora por causas concretas e ela não consegue se sustentar por um período calibrável, entra em **risco de fechamento**.

O objetivo de design é:

causa econômica real
→ aviso simples
→ diagnóstico opcional
→ tempo para o sistema/jogador reagir
→ fechamento se o problema persistir

Exemplos de causas:
- poucos clientes;
- custo de insumos alto;
- falta de mercadoria;
- frete excessivo;
- salários/custos operacionais incompatíveis com a receita.

A UI deve apontar a causa dominante com base nos dados reais. O detalhamento permite inspecionar a decomposição completa.

Não foi decidido:
- destino do prédio após fechamento;
- possibilidade de outro operador assumir;
- recuperação judicial/crédito;
- intervenções/subsídios específicos.

Esses temas ficam fora até criarem uma decisão de gameplay relevante.


---

---

---

## Demanda como orientação, não bloqueio

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido no comportamento principal; fórmula ainda aberta.

O jogador continua responsável pela gestão do tecido econômico.

A demanda **não impede** uma construção. Se o jogador quiser colocar cinco mercados próximos, o jogo permite.

O sistema deve, porém, tornar a consequência legível:

- antes/depois da colocação, indicar baixa demanda quando aplicável;
- mostrar que estabelecimentos equivalentes disputam a mesma clientela;
- deixar o jogador correr o risco econômico conscientemente;
- se a empresa depois tiver poucos clientes e prejuízo, a deterioração precisa apontar essa causa real.

Exemplo conceitual:

cinco mercados concentrados
→ clientela disponível dividida/insuficiente
→ aviso de baixa demanda
→ vendas menores
→ pressão sobre o caixa das empresas
→ risco de fechamento se persistir.

Isso é preferível a bloquear o prédio, porque mantém agência e transforma demanda em ferramenta de gestão.

### Ainda aberto

A fórmula não deve ser inventada agora. Antes da implementação, definir e testar:

- se demanda é principalmente local por área de atendimento ou também possui componente global;
- efeito de distância/tempo de viagem;
- população potencial atendida;
- capacidade dos estabelecimentos;
- concorrência entre negócios equivalentes;
- substituição entre categorias parecidas;
- como mostrar os fatores sem criar uma barra misteriosa.

A regra transversal continua valendo: o jogador precisa conseguir descobrir por que a demanda está baixa.


---

---

---

## Interpretação do Caixa da Cidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido.

O **Caixa da Cidade** é o recurso monetário principal controlado pelo jogador e representa capital disponível para desenvolvimento e operação da cidade.

Ele **não deve ser interpretado literalmente apenas como a conta jurídica da prefeitura**.

Isso permite um único loop de construção:

jogador decide construir
→ Caixa da Cidade paga
→ materiais são consumidos
→ o prédio entra em operação

Depois da inauguração, a operação pode divergir:
- serviço público continua ligado às despesas públicas;
- empresa privada opera com caixa próprio;
- moradia participa da economia familiar e ainda precisa fechar o destino do aluguel/preço.

A abstração evita criar vários bolsos de investimento controlados pelo jogador sem eliminar caixas reais de entidades simuladas quando eles geram gameplay.


---

---

---

## Mercado de aquisição ligado à conexão exterior

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente promovido para a SPEC. Aquisição residencial externa para migração, prédios econômicos vazios aguardando empresa e aquisição por empresas foram decididos; fórmulas, preços e alguns fluxos futuros seguem em calibração/exploração.

### Ideia

Quando o jogador termina uma construção privada, o prédio pode começar **sem proprietário/operador definitivo**.

Ele entra em um mercado automático de aquisição/ocupação.

Candidatos podem vir de:
- famílias já existentes na cidade;
- empresas já existentes na cidade;
- novos candidatos vindos da conexão exterior.

Baixa demanda significa menos candidatos e pode deixar o ativo vazio por mais tempo.

A conexão exterior deixa de servir apenas para pessoas, carga e serviços: ela também pode representar a entrada de **capital, novas famílias e novas empresas** na cidade.

### Referências de gameplay

Cities: Skylines II usa uma lógica próxima em dois pontos, embora não simule compra explícita do imóvel:
- residências construídas mas desocupadas reduzem demanda residencial; novos cidadãos procuram moradia conforme emprego, educação e atratividade;
- edifícios econômicos podem ficar desocupados até uma empresa se instalar, e empresas avaliam localização, clientes, trabalhadores, recursos e custos antes de escolher onde operar.

A documentação também usa Outside Connections como origem de novos cidadãos e de recursos/serviços externos.

Fontes:
- Paradox — Zones & Signature Buildings: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
- Paradox — Economy & Production: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production
- Paradox — Public & Cargo Transportation: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/public-cargo-transportation
- Paradox — City Services: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies

Isso valida o princípio de **prédio vazio + agente adequado entra conforme demanda**, mas não valida automaticamente um sistema de propriedade/compra; essa parte é proposta específica do IndexCities.

### Principal vantagem

A proposta conecta sistemas que já existem em vez de criar uma camada isolada:

jogador constrói
→ capital fica imobilizado no ativo
→ ativo entra no mercado
→ demanda define velocidade/interesse
→ família/empresa local ou externa adquire
→ dinheiro da aquisição tem origem real
→ ocupante/empresa entra em operação
→ cidade passa a receber impostos e efeitos econômicos

Isso transforma vacância em consequência real de uma decisão ruim do jogador.

### Residencial

A proposta é especialmente forte para moradia.

Uma residência concluída poderia receber candidatos:

**Família local**
- já mora na cidade;
- avalia preço, renda/poupança, tamanho da família, localização e acesso;
- se comprar para morar, muda de residência e deixa a anterior disponível;
- se puder comprar como investimento, pode tornar-se proprietária e alugá-la.

**Família exterior**
- avalia cidade, emprego, preço e moradia;
- ao concluir a compra para residência própria, entra pela conexão exterior e passa a ser família real da cidade.

Essa dinâmica pode criar cadeias naturais:

família compra casa nova
→ libera imóvel antigo
→ outra família ocupa/compra
→ migração e mobilidade residencial emergem do mesmo mercado.

### Grande ponto em aberto: compra para morar versus comprar para alugar

Permitir que qualquer família compre imóveis extras para renda gera gameplay patrimonial interessante, mas amplia bastante o sistema.

Prós:
- diferencia riqueza de renda;
- cria proprietários e locatários reais;
- dá significado a herança;
- aluguel tem destino claro;
- pode produzir concentração patrimonial e vulnerabilidade econômica emergentes.

Contras:
- exige poupança/riqueza familiar real;
- pode exigir limite ou lógica de investimento para evitar comportamento absurdo;
- amplia a importância de preço dos imóveis;
- pode gerar especulação e imóveis vazios;
- external buyer que compra apenas para alugar sem virar entidade local recriaria um landlord invisível.

**Decisão promovida para a SPEC:** no primeiro modelo, comprador residencial vindo do exterior só pode comprar para **morar e migrar para a cidade**. Investidor residencial externo que permaneça fora da cidade apenas recebendo aluguel fica fora deste primeiro modelo.


### Decisão fechada: comprador residencial exterior

Foi aprovado que, no primeiro modelo, um comprador residencial vindo da conexão exterior **só compra para migrar e morar na cidade**.

Consequências:
- o comprador externo vira família real da simulação;
- ocupa fisicamente a residência adquirida;
- passa a participar de emprego, consumo, trânsito, impostos e demais sistemas;
- não existe proprietário residencial externo invisível recebendo aluguel à distância;
- investimento imobiliário externo puro fica fora até existir uma entidade e uma justificativa de gameplay claras.

Essa decisão reduz risco de caixa-preta e de entrada monetária sem agente correspondente.

### Comércio e indústria: usar o mesmo mecanismo, mas não necessariamente a mesma propriedade

Para comércio e indústria, o conceito de **prédio vazio aguardando empresa** é forte.

Candidatos:
- empresa local existente querendo expandir;
- nova empresa formada/local;
- empresa exterior entrando na cidade.

A empresa avalia:
- demanda/clientes;
- trabalhadores;
- insumos;
- logística;
- localização;
- custos.

Baixa oportunidade pode deixar o prédio vazio.

Porém, não é obrigatório separar:
- proprietário jurídico do prédio;
- empresa operadora.

No primeiro escopo, pode ser mais simples tratar a empresa que assume o prédio como **proprietária/operadora econômica** daquele ativo.

Isso evita criar mercado imobiliário comercial separado apenas por realismo.


### Decisão fechada: comércio e indústria aguardam empresa adquirente

Foi aprovado:

jogador constrói prédio privado
→ prédio concluído fica vazio/procurando operador
→ empresas locais ou exteriores avaliam o ativo
→ uma empresa adquire
→ pagamento retorna ao Caixa da Cidade
→ empresa torna-se proprietária + operadora
→ operação segue com caixa empresarial próprio

No primeiro modelo não haverá separação entre proprietário do imóvel comercial/industrial e a empresa operadora.

A regra anterior de o Caixa da Cidade fornecer **capitalização operacional inicial** para a empresa fica superada por esta decisão. A empresa adquirente precisa chegar ao negócio com capital próprio real.

Ainda permanece aberto **quem é o proprietário humano da empresa** e como o capital de uma empresa nova é formado.

### Grande risco 1: dinheiro infinito vindo do exterior

Se qualquer prédio puder ser vendido instantaneamente a um comprador externo com dinheiro ilimitado, surge um exploit:

importar materiais
→ construir
→ vender para exterior
→ receber mais dinheiro
→ repetir infinitamente

Portanto, **conexão exterior não pode ser comprador infinito**.

A demanda exterior precisa ser limitada por causas observáveis, por exemplo:
- empregos disponíveis;
- qualidade/atratividade;
- preço;
- tipo/tamanho de moradia;
- oportunidade econômica;
- oferta já vazia na cidade.

A UI deve conseguir mostrar algo como:
- candidatos locais interessados;
- candidatos externos interessados;
- motivo de baixa procura;
- tempo em vacância.

### Grande risco 2: de onde vem e para onde vai o preço de compra

Se o jogador usa o Caixa da Cidade para construir e depois um agente compra o ativo, o preço de aquisição precisa ter destino explícito.

Hipótese coerente:

Caixa da Cidade paga a construção
→ prédio concluído é um ativo do desenvolvimento da cidade
→ comprador paga aquisição
→ valor retorna ao Caixa da Cidade
→ depois a receita recorrente da cidade vem principalmente de impostos

Assim, venda de imóvel/prédio funciona como **reciclagem do capital investido**, enquanto impostos continuam sendo a receita recorrente.

Isso também diferencia:
- fluxo de capital: construir/vender;
- fluxo fiscal: impostos durante a operação.

### Risco de arbitragem

Se preço de venda for simplesmente maior que custo de construção e a demanda exterior não tiver limite, o jogador vira incorporador com lucro garantido.

Precisam ser testados:
- custo total da obra;
- valor de mercado;
- oferta concorrente;
- demanda local/exterior;
- tempo de vacância;
- orçamento real dos compradores.

Não assumir lucro garantido.

### Pré-requisito oculto: dinheiro das famílias

Compra residencial real exige que famílias tenham alguma forma explícita de:
- dinheiro/poupança;
- capacidade de compra.

Hoje renda e orçamento familiar já existem conceitualmente, mas a granularidade de patrimônio/poupança ainda precisa ser fechada.

Sem poupança real, a escolha de comprador vira caixa-preta.

Hipoteca/banco **não precisa** entrar automaticamente. Uma primeira versão pode permitir compra apenas quando a família possui recursos suficientes, deixando financiamento imobiliário para uma decisão futura.

### Apartamentos

Há duas opções conceituais:

1. prédio inteiro tem um proprietário;
2. cada unidade residencial tem proprietário próprio.

Propriedade por unidade combina melhor com:
- família proprietária;
- herança;
- compra/venda individual;
- aluguel por unidade.

Mas aumenta estado persistente.

Não decidir ainda; avaliar no POC de moradia.

### Como evitar microgerenciamento

O jogador não precisa:
- escolher comprador;
- aprovar propostas;
- organizar leilão;
- administrar contratos.

O mercado pode resolver automaticamente.

A superfície mostra:
- **vazio / à venda / procurando operador**;
- demanda boa/média/baixa;
- interesse local/exterior;
- principal motivo de demora.

A visão detalhada mostra candidatos, preço, capacidade financeira e fatores relevantes.

### Coerência após aprovação

A SPEC foi revisada:
- o Caixa da Cidade continua pagando a construção;
- a capitalização operacional automática da empresa foi removida;
- a empresa adquirente compra o ativo com capital próprio;
- o valor da aquisição retorna ao Caixa da Cidade.

Ainda precisam ser definidos a origem do capital de empresas novas e o vínculo entre empresa e SIM proprietário.

### Avaliação atual

A hipótese parece mais coerente que o modelo anterior porque:
- conecta demanda, vacância, migração, empresas e conexão exterior;
- mantém o jogador no controle da construção;
- permite que impostos sejam a receita recorrente principal;
- reduz necessidade de proprietários abstratos;
- cria consequências visíveis para construir demais.

O maior risco não é complexidade de simulação. É **criar capital externo infinito ou uma disputa de compradores opaca**.

Por isso, a ideia merece continuar sendo explorada, mas só deve virar requisito depois de fechar:
1. quem pode adquirir residencial;
2. se comprador exterior precisa entrar/migrar;
3. como comércio/indústria tratam aquisição versus operação;
4. como o preço de compra retorna ao Caixa da Cidade;
5. como limitar e explicar demanda exterior.


---

---

---

## Propriedade humana das empresas

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido para o primeiro modelo: empresa é entidade econômica independente sem SIM proprietário modelado.

A hipótese anterior era dar a cada empresa um SIM proprietário. O problema levantado é válido: se o SIM investe dinheiro pessoal, a empresa lucra em um caixa separado e depois precisamos criar dividendos, retiradas, herança de participação e outras transferências só para o dinheiro "voltar ao bolso" do dono.

Isso adiciona um sistema de propriedade empresarial antes de ele gerar gameplay claro.

### Opção A — empresa independente, sem proprietário humano modelado

**Decisão aprovada para o primeiro modelo.**

A empresa continua sendo uma entidade econômica concreta e persistente:
- caixa próprio real;
- estoque;
- receitas e despesas;
- salários;
- impostos;
- funcionários e vagas;
- propriedades/estabelecimentos;
- falência;
- expansão.

O que fica fora é apenas a camada de acionistas/dono humano.

Fluxo:
empresa possui caixa
→ compra/adquire estabelecimento
→ opera
→ lucros ficam no caixa da própria empresa
→ usa caixa para sobreviver, comprar insumos e futuramente expandir
→ paga impostos ao Caixa da Cidade

**Prós**
- muito mais simples;
- elimina dividendos/retiradas e confusão entre carteira do SIM e caixa empresarial;
- empresa continua totalmente auditável;
- lucro tem destino concreto: permanece na própria empresa;
- facilita empresas vindas do exterior;
- evita forçar um proprietário a migrar para a cidade;
- preserva possibilidade de adicionar donos/acionistas depois.

**Contras**
- não existe, por enquanto, um cidadão enriquecendo diretamente por possuir uma empresa;
- patrimônio empresarial não entra em herança;
- é uma abstração jurídica: toda empresa real teria algum dono/acionista, mas esse ator não é modelado.

Essa abstração é considerada aceitável se propriedade empresarial não produzir decisões relevantes no primeiro escopo. A empresa **não é abstrata**: ela é o próprio agente econômico concreto. Apenas seus acionistas ficam fora da simulação.

### Opção B — empresa separada com um SIM proprietário

**Prós**
- conecta riqueza pessoal e empresas;
- permitiria dividendos, herança e empreendedorismo individual;
- proprietário humano fica visível.

**Contras**
- exige definir investimento inicial, retirada/dividendos, patrimônio empresarial e herança;
- cria transferências adicionais sem benefício claro para a gestão da cidade neste momento;
- complica empresas externas;
- pode virar detalhe econômico que não muda as decisões do jogador.

Não recomendada para o primeiro modelo.

### Ponto crítico: origem do capital

Empresa sem dono modelado não autoriza dinheiro mágico.

Precisamos distinguir:

**Empresa já existente**
- usa o próprio caixa acumulado para adquirir novo estabelecimento/expandir.

**Empresa nova vinda do exterior**
- entra com capital inicial explícito proveniente da conexão exterior;
- esse valor é finito, configurável e diagnosticável;
- a procura externa continua limitada pelas condições econômicas da cidade.

**Empresa nova criada localmente**
- não deve aparecer com dinheiro do nada;
- sua formação precisa de uma fonte real de capital antes de ser habilitada;
- pode ficar fora do primeiro modelo até existir uma regra clara.

### Lucro

Sem proprietário modelado, o lucro não "some":
- aumenta o caixa/patrimônio da empresa;
- melhora sua capacidade de absorver prejuízo;
- permite comprar insumos;
- permite contratar e operar;
- pode financiar expansão futura;
- gera impostos conforme as regras fiscais.

Só será necessário criar dividendos/retiradas se futuramente decidirmos que a relação entre cidadão e propriedade empresarial gera gameplay suficiente.

### Relação com o princípio de evitar abstrações

Evitar abstração não significa modelar toda camada jurídica do mundo real.

Uma abstração é aceitável quando:
- a entidade relevante continua concreta;
- dinheiro tem origem e destino;
- nenhuma consequência importante fica escondida;
- a camada omitida não gera decisão de gameplay.

Nesse caso, a empresa é concreta; o acionista é a camada omitida.

### Decisão atual

Para o primeiro modelo:
- empresa é entidade econômica independente;
- não existe SIM proprietário modelado;
- lucro permanece na empresa;
- empresas existentes expandem com caixa próprio;
- empresas novas externas podem entrar com capital externo explícito;
- criação de empresa nova local fica aberta até existir fonte concreta de capital.

Reavaliar donos/acionistas somente se surgirem sistemas como empreendedorismo individual, dividendos, herança empresarial, compra/venda de empresas ou concentração de capital que justifiquem o custo.

---

---

---

## Fluxo do player após concluir comércio/indústria

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** modelo conceitual decidido; detalhes de apresentação e parâmetros seguem para POC/calibração.

A regra vale para **comércio e indústria** privados.

### Fluxo

1. jogador escolhe e financia a construção;
2. Caixa da Cidade paga dinheiro e materiais conforme as regras de obra;
3. obra física é concluída;
4. prédio fica **vazio / procurando empresa**;
5. empresas candidatas avaliam o ativo;
6. no início da cidade, a fonte normal de novas empresas é a conexão exterior;
7. uma empresa com capital suficiente compra o ativo;
8. o valor da compra retorna ao Caixa da Cidade;
9. a empresa passa a ser proprietária + operadora;
10. empresa contrata SIMs reais e inicia operação;
11. dali em diante caixa, estoque, salários, impostos, produção/vendas e falência pertencem à entidade empresa.

### Demanda: mesmo princípio, fatores diferentes

Residência e empresa usam a mesma ideia de mercado:

ativo vazio
→ candidatos avaliam
→ baixa oportunidade gera demora/vacância

Mas não devem compartilhar uma fórmula genérica.

**Residencial** observa principalmente:
- preço/affordability;
- moradia disponível;
- emprego;
- localização/acesso;
- tamanho/necessidade da família.

**Comércio/indústria** observa principalmente:
- clientes/demanda do produto;
- concorrência;
- mão de obra;
- insumos;
- logística;
- custos;
- localização.

Assim, "demanda" é um princípio comum, não uma barra mágica universal.

### Empresas exteriores

No primeiro modelo, uma empresa nova que entra pela conexão exterior é criada como entidade econômica concreta com:
- tipo/atividade;
- caixa inicial finito;
- capacidade de pagar a aquisição;
- capital restante suficiente para iniciar operação segundo regras calibráveis.

O capital inicial representa recursos trazidos do exterior. Deve ser explícito, limitado e diagnosticável.

Depois de entrar, a empresa deixa de ser apenas "externa": passa a existir na cidade e obedece às mesmas regras de caixa, emprego, estoque, impostos e falência.

### Empresas locais existentes

Uma empresa que já opera na cidade pode futuramente comprar outro estabelecimento usando seu próprio caixa.

Isso é o sentido de:
**empresa existente usa o próprio caixa para expandir**.

Não significa que o jogador manda expandir; a empresa toma a decisão automaticamente conforme oportunidade e capacidade financeira.

### Empresa nova puramente local

No primeiro modelo, não é necessário permitir que uma empresa completamente nova nasça dentro da cidade sem origem definida.

Isso evita criar capital inicial do nada.

Uma forma futura pode usar:
- empreendedorismo de SIMs;
- cisão/investimento de empresa existente;
- crédito;
- outra fonte concreta.

Nenhuma delas entra sem decisão explícita.

### Estrutura de funcionários no primeiro modelo

**Status:** decidido para o primeiro modelo: sem cargos ou profissões formais como mecânica.

A regra vale de forma transversal para empresas e serviços, incluindo comércio, indústria, hospital, escola e demais estruturas com trabalhadores.

Exemplo inicial:

hospital
→ precisa de 5 funcionários
→ SIM A trabalha no hospital
→ SIM B trabalha no hospital

A engine não precisa distinguir funções profissionais no primeiro modelo. O vínculo relevante é:
- SIM;
- local de trabalho;
- vaga;
- salário;
- turno;
- qualificação quando aplicável.

### Por que começar assim

Prós:
- reduz bastante o escopo inicial da engine de emprego;
- evita organograma, promoções e dependências hierárquicas sem gameplay comprovado;
- permite validar primeiro contratação, salários, deslocamento, turnos e capacidade de pessoal;
- mantém a simulação concreta: cada trabalhador continua sendo um SIM real ligado a uma vaga real.

Contras:
- perde identidade profissional dos trabalhadores;
- salários diferentes podem precisar de explicação por qualificação ou turno sem um nome de cargo;
- alguns serviços ficam mais abstratos no primeiro modelo.

A abstração é aceitável no início porque não cria dinheiro ou trabalhador mágico; apenas adia a diferenciação funcional entre vagas.

### Evolução futura desejada: cargos e profissões

Fica registrada como evolução interessante uma engine de cargos/profissões.

Ela poderia permitir:
- vagas nomeadas;
- salários diferentes por cargo;
- qualificações específicas;
- quantidades por função;
- efeitos operacionais quando faltar determinada função;
- melhor leitura da carreira e identidade do SIM.

Isso não faz parte do primeiro modelo e só deve virar mecânica quando houver valor de gameplay suficiente para justificar a complexidade.

A ordem desejada é:
1. emprego simples por estrutura;
2. validar salários, turnos, deslocamento, capacidade e contratação;
3. depois avaliar cargos como aprofundamento.


---


---

## Encerramento de estabelecimento e continuidade da empresa

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decisão promovida para a SPEC; detalhes de estoque e outros ativos específicos permanecem abertos até existirem no modelo.

### Decisão

Evitar transformar falência em uma segunda simulação de gestão empresarial.

- **Falência da empresa é terminal.** Depois do período de risco já definido, a empresa não fica como entidade inativa procurando eternamente uma nova oportunidade.
- Pode existir uma etapa técnica curta de **liquidação**, invisível como microgerenciamento: operações param, imóveis são ofertados automaticamente e recebimentos continuam rastreáveis.
- Enquanto houver ativos a vender, a entidade pode existir apenas como suporte contábil da liquidação.
- Quando não houver mais ativos pendentes, saldo remanescente vai para o Fundo de Patrimônio Não Reclamado e a empresa é removida.
- Se algum ativo perder o titular com o encerramento, ele pode ficar explicitamente sem proprietário; uma venda posterior envia o valor ao fundo.
- Isso não impede uma empresa saudável com vários estabelecimentos de fechar apenas um local e continuar operando os demais.

### Por que esta versão foi escolhida

**Prós**
- fecha todos os fluxos sem dinheiro ou propriedade desaparecerem;
- não exige que o jogador acompanhe liquidação, credores ou processo jurídico;
- falência continua tendo consequência clara e terminal;
- evita acumular empresas “zumbis”;
- reutiliza o mecanismo já decidido de patrimônio não reclamado.

**Contras**
- existe um pequeno estado técnico de liquidação;
- detalhes de estoque e outros ativos terão de ser definidos quando esses casos realmente existirem;
- o fundo passa a servir genericamente para valores sem titular econômico, não apenas heranças.

O objetivo é manter essa camada no mínimo necessário para preservar causalidade. Não adicionar credores, ordem jurídica de pagamento, administrador judicial ou outras regras enquanto não houver gameplay concreto que justifique isso.


---

## Reavaliação do pagamento da primeira aquisição privada

> **Revisão humana desta seção:** PENDENTE — problema reaberto por impacto direto no gameplay; não é decisão oficial.

A regra atual da SPEC em que o Caixa constrói comércio/indústria e recebe de volta o preço pago pela empresa cria o mesmo risco identificado em moradia: capital de construção pode ser reciclado e reduzir demais a importância dos impostos.

Alternativa em discussão:
- o Caixa da Cidade continua arcando com a construção como parte do diferencial do IndexCities;
- a empresa que assume o ativo paga com dinheiro real;
- esse pagamento não volta ao jogador; é liquidado contra a economia exterior;
- o jogador passa a recuperar o investimento apenas indiretamente, por empregos, atividade econômica e impostos;
- não criar financiamento, incorporadora ou negociação manual.

**Pró:** mantém o loop de construção simples e evita transformar o jogador em desenvolvedor imobiliário/comercial buscando revenda.

**Contra:** cria saída monetária da economia local e exige calibrar entradas externas para não drenar capital demais.

A regra canônica ainda não foi alterada neste ponto; precisa de confirmação antes de mudar a SPEC.
