# IndexCities — Economia, moradia e empresas

> **Status:** exploração ativa; várias decisões conceituais já foram promovidas para a SPEC — não é fonte de verdade.
>
> Preserva pesquisa, alternativas e revisões sobre dinheiro, consumo, propriedade, moradia, demanda, empresas, falência, aquisição de ativos e fluxos econômicos. Trechos superados são mantidos como histórico quando ajudam a explicar a decisão atual.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Dinheiro, renda familiar e consumo

**Status:** em exploração.

### Renda individual e familiar

Direção discutida:

- cidadãos adultos podem ter dinheiro individual;
- crianças não precisam possuir uma conta financeira própria por padrão;
- a família/domicílio pode ter uma noção agregada de renda e despesas compartilhadas;
- ainda precisa ser decidido como dividir salário, patrimônio, contas da casa, dependentes e gastos individuais.

Uma opção promissora para testar é manter **renda e patrimônio individuais**, mas calcular também métricas de **renda familiar disponível** e despesas comuns do domicílio. Isso preserva individualidade sem obrigar toda decisão econômica a existir em nível bancário extremamente granular.

### Até onde simular compras?

A questão central não é “simular tudo”, mas decidir quais detalhes geram consequências úteis no jogo.

Há três níveis possíveis:

1. **Compra abstrata**  
   O cidadão gasta dinheiro e a loja recebe receita, mas não existe item/estoque real.

2. **Compra por categorias**  
   A loja mantém estoques de categorias como comida, remédios, combustível, roupas e materiais. O cidadão consome uma categoria, não uma SKU específica.

3. **Compra por item individual**  
   Cada produto possui unidade, preço, estoque e consumo próprios.

### Avaliação provisória

**Decisão fechada:** o modelo inicial será **por categorias de produtos**, não por SKU individual.

Razões preservadas desta exploração:

- mantém a relação real entre produção, logística, estoque, consumo e falta de produtos;
- permite que comércio tenha motivo para receber entregas;
- permite que preço e escassez afetem cidadãos e empresas;
- evita explodir o número de entidades com milhares de SKUs que provavelmente não gerariam gameplay proporcional;
- deixa espaço para aprofundar categorias específicas mais tarde se elas se provarem importantes.

### Regra de profundidade sugerida

Um detalhe só deve entrar na simulação se produzir ao menos uma consequência observável relevante, como:

- alterar decisão do cidadão;
- alterar preço, estoque ou logística;
- causar falta de produto/serviço;
- mudar tráfego;
- afetar renda, bem-estar ou emprego;
- criar uma decisão interessante para o jogador.

Se um detalhe não muda nenhum sistema relevante, ele pode permanecer agregado.


---

---

## Valor imobiliário e propriedade de lotes

**Status:** em exploração.

Ainda não está decidido se cada lote terá proprietário individual e valor de mercado próprio.

Também não está definido como calcular valor imobiliário.

Fatores candidatos a investigar com dados e referências reais:

- proximidade de empregos e comércio;
- acesso viário e transporte;
- qualidade de escolas e saúde;
- criminalidade;
- poluição e ruído;
- acesso a áreas verdes e água;
- congestionamento;
- oferta e demanda por moradia;
- tamanho/qualidade do imóvel;
- renda da vizinhança;
- impostos e custos recorrentes.

O modelo deve ser validado antes de virar regra de produto. Evitar fórmula arbitrária sem referência empírica.

### Dívida municipal

A dívida municipal passa a fazer parte da direção do produto porque uma cidade sem mecanismo de financiamento pode entrar em estado de caixa negativo sem caminho de recuperação.

Ainda precisa ser decidido:

- quem empresta;
- limite de endividamento;
- juros;
- prazo;
- consequências de inadimplência;
- se empréstimos aparecem automaticamente como ferramenta de emergência ou exigem decisão explícita do jogador.


---

---

## Falência pessoal, população sem moradia e demanda imobiliária

**Status:** em exploração; existência desses fenômenos já está decidida na SPEC.

### Falência pessoal

Ainda precisa ser definido o que acontece quando um cidadão/família perde capacidade de pagar suas despesas.

Perguntas a investigar:

- atraso de aluguel;
- despejo;
- perda de acesso a bens/serviços;
- mudança para moradia mais barata;
- busca emergencial de emprego;
- apoio social;
- endividamento pessoal ou ausência dele;
- efeitos sobre bem-estar, saúde e criminalidade.

A consequência deve criar dinâmica sistêmica sem virar punição arbitrária.

### População sem moradia

A população sem moradia deve existir como estado real da simulação, mas a resposta pública ainda está aberta.

Opções a pesquisar:

- abrigos temporários;
- assistência social;
- moradia pública/subsidiada;
- programas de emprego;
- saúde e atendimento emergencial;
- impacto em orçamento municipal e uso do espaço urbano.

A prefeitura deve poder reagir, mas o nível de controle e automação ainda precisa ser decidido.

### Demanda orientando construção

Como toda construção é colocada pelo jogador, a simulação precisa indicar demanda de forma clara.

A demanda pode considerar, entre outros:

- número de famílias procurando moradia;
- faixa de renda;
- vagas de emprego abertas;
- falta de comércio/serviços por categoria;
- estoque insuficiente;
- distância e acessibilidade;
- capacidade ociosa existente;
- crescimento populacional.

A intenção é usar demanda como **sinal para decisão do jogador**, não como zoneamento automático.

### Oferta e demanda nos preços

Direção desejada: modelo simples, legível e configurável.

Hipótese inicial a avaliar:

- preço-base do imóvel/aluguel;
- multiplicador de oferta/demanda;
- modificadores de localização e qualidade;
- limites de variação para evitar instabilidade artificial.

A fórmula final só deve ser escolhida depois de testar se o jogador consegue entender por que os preços subiram ou caíram.


---

---

## Agricultura e produção local de alimentos

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

## Importação externa e preços iniciais

**Status:** parcialmente decidido.

Decidido:

- mercadorias físicas podem ser importadas enquanto a cidade não as produz localmente em quantidade suficiente;
- importações usam a conexão externa;
- preços externos ficam **estáveis no escopo inicial**, sem mercado externo dinâmico.

Para implementação, o preço deve ser um dado de balanceamento configurável pelo projeto, mas isso não implica necessariamente uma opção exposta ao jogador.


---

---

## Transparência das finanças municipais

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

## Abastecimento de alimentos e combustível no primeiro escopo

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

## Propriedade e controle: prefeitura versus empresas privadas

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

## Dinheiro privado por empresa: validação de gameplay

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

## Revisão: quem paga quando o jogador constrói

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

## Falência empresarial sem simular processo jurídico

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

## Demanda como orientação, não bloqueio

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

## Destino econômico de aluguel e preço residencial

**Status:** parcialmente superado. O destino do aluguel foi decidido: vai ao proprietário real. Este trecho preserva as alternativas discutidas; primeira venda, revenda e herança ainda têm detalhes abertos.

Foi decidido que casas e apartamentos seguem o mesmo loop de construção dos demais prédios:

Caixa da Cidade + materiais
→ obra
→ residência disponível
→ família ocupa
→ aluguel e/ou preço afetam o orçamento familiar

A questão restante é **para quem vai o dinheiro pago pela família**.

### Opção A — pagamento retorna ao Caixa da Cidade

**Recomendação anterior, agora em revisão.**

Como o jogador financiou diretamente a construção, aluguel funciona como retorno econômico daquele investimento.

Vantagens:
- fluxo extremamente legível;
- nenhuma entidade extra de proprietário/locador;
- construção residencial deixa de ser apenas despesa e ganha retorno econômico;
- fácil de balancear e diagnosticar;
- combina com a interpretação do Caixa da Cidade como capital de desenvolvimento controlado pelo jogador, não como conta jurídica literal da prefeitura.

Risco:
- é uma abstração forte: em uma economia real, toda moradia privada não pertenceria à cidade/jogador;
- precisa evitar transformar aluguel em fonte automática de dinheiro sem risco.

Consequências úteis de gameplay podem vir de fatores já existentes:
- vacância;
- renda das famílias;
- localização;
- preço/aluguel;
- atratividade;
- capacidade de pagamento.

### Opção B — proprietário/investidor privado recebe

É mais realista, mas exigiria criar proprietário, conta ou setor imobiliário separado.

Problemas:
- nova camada financeira com pouca interação direta do jogador;
- risco alto de caixa-preta;
- jogador paga a construção mas o retorno vai para outro agente, o que pode parecer incoerente;
- exigiria explicar como o investidor nasce e como recupera capital.

Não recomendada para o primeiro escopo.

### Opção C — conta econômica do próprio prédio

Cada residência/prédio teria uma conta operacional que recebe aluguel e paga custos.

É auditável, mas adiciona outra entidade financeira além de famílias, empresas e Caixa da Cidade.

Só vale se manutenção predial, condomínio, proprietário ou investimento imobiliário virarem gameplay real.

### Recomendação anterior — reaberta

A recomendação de começar com **aluguel retornando ao Caixa da Cidade** foi reaberta porque ela entra em tensão com uma economia centrada em impostos e com a possibilidade futura de famílias possuírem ou herdarem moradia.

Interpretação de gameplay:

o jogador investe em moradia
→ famílias ocupam
→ pagam aluguel real
→ o Caixa da Cidade recupera parte do investimento ao longo do tempo

Isso não precisa significar juridicamente que "a prefeitura é dona de todas as casas". O Caixa da Cidade já foi definido como uma abstração de capital de desenvolvimento controlado pelo jogador.

### Compra/venda da residência

Venda é mais delicada que aluguel porque cria uma pergunta de propriedade e revenda.

Recomendação para evitar micro prematuro:
- manter **aluguel** como fluxo econômico inicial mais simples;
- não fechar compra/venda real de imóveis até existir motivo de gameplay para propriedade residencial.

Se preço de venda continuar na simulação, ele pode inicialmente funcionar como indicador de valor/affordability sem exigir transferência jurídica completa de propriedade.

Essa decisão deve ser revisitada antes de implementar compra de imóvel real.


---

---

## Interpretação do Caixa da Cidade

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

## Revisão de moradia: impostos, aluguel, propriedade e herança

**Status:** aberto; a recomendação anterior de enviar todo aluguel ao Caixa da Cidade foi suspensa.

### Novo ponto de partida

A intenção de gameplay é que a receita recorrente controlada pelo jogador venha **principalmente de impostos e outras receitas da cidade**, e não de o jogador funcionar como proprietário universal de moradias e empresas.

Isso combina melhor com a operação privada já decidida para empresas.

### Consequência importante

Se no futuro houver diferença entre uma família que **possui/herdou** uma casa e uma família que **aluga**, mandar todo aluguel direto ao Caixa da Cidade deixa de ser conceitualmente limpo.

A propriedade da moradia passa a ter consequências reais:

- família proprietária: não paga aluguel; possui um ativo; pode pagar imposto sobre propriedade e custos de manutenção; uma herança pode transferir esse ativo;
- família locatária: paga aluguel recorrente; não possui o imóvel; tende a ter menor barreira de entrada e maior facilidade de mudança;
- cidade: recebe impostos/tributos definidos, não necessariamente o aluguel bruto.

### Alternativa A — todas as moradias são aluguel no primeiro escopo

**Prós**
- modelo muito simples;
- nenhuma lógica de compra, venda, herança ou proprietário individual;
- fácil de balancear;
- combina com famílias mudando de residência.

**Contras**
- enfraquece riqueza patrimonial familiar;
- herança de imóvel não existe;
- duas famílias com a mesma renda ficam economicamente parecidas mesmo que, conceitualmente, uma pudesse ter patrimônio;
- reduz profundidade potencial dos cidadãos persistentes.

### Alternativa B — propriedade residencial real por família + aluguel privado

**Prós**
- cria diferença concreta entre proprietário e locatário;
- patrimônio familiar pode afetar vulnerabilidade econômica;
- herança passa a ter significado real;
- preço de imóvel deixa de ser apenas um indicador;
- impostos sobre propriedade podem gerar receita municipal de forma clara;
- combina com cidadãos/famílias persistentes.

**Contras**
- exige definir compra, venda e transferência de propriedade;
- exige resolver quem recebe aluguel de imóveis locados;
- herança e propriedade adicionam estado persistente;
- risco de transformar moradia em um sistema grande demais cedo.

### Alternativa C — setor/proprietário imobiliário abstrato

**Prós**
- aluguel não precisa ir para o Caixa da Cidade;
- cidade recebe impostos;
- evita proprietário individual para cada imóvel.

**Contras**
- alto risco de caixa-preta;
- adiciona dinheiro circulando para um agente que o jogador não vê;
- repete exatamente o tipo de abstração econômica difícil de diagnosticar que o projeto quer evitar.

**Não recomendada** sem uma entidade econômica real e inspecionável.

### Alternativa D — empresa imobiliária privada real para imóveis de aluguel

Imóveis locados são operados por uma empresa imobiliária, usando o mesmo modelo econômico já definido para outras empresas.

Fluxo:

família locatária
→ paga aluguel
→ caixa da empresa imobiliária
→ manutenção/custos/impostos
→ resultado da empresa

**Prós**
- reaproveita um sistema já existente: empresa privada com caixa e ledger;
- aluguel tem origem e destino reais;
- cidade recebe impostos;
- nenhuma necessidade de um "landlord virtual" invisível;
- pode operar apartamentos e casas destinadas a aluguel sem microgerenciamento do jogador.

**Contras**
- cria mais um tipo de empresa;
- ainda precisa definir como o prédio passa a ser operado por essa empresa;
- se adotado cedo demais, pode ampliar o escopo de economia imobiliária.

### Evidência de referência

Cities: Skylines II originalmente tinha um landlord virtual na economia de aluguel. Na Economy 2.0, a Paradox removeu esse landlord virtual e passou a dividir o upkeep entre os locatários, junto com uma revisão ampla de aluguel e transparência econômica. Isso é um alerta útil contra introduzir um recebedor abstrato de aluguel apenas para "fechar a conta".

Manor Lords mantém uma separação clara entre riqueza regional e tesouro do jogador, com tributação transferindo riqueza para o tesouro. É um precedente de gameplay em que a economia dos habitantes existe separada da receita controlada pelo jogador.

Fontes:
- Paradox — Economy 2.0 Part 2: https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-two
- Paradox — Zones & Signature Buildings: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
- Manor Lords Official Wiki — Regional Wealth: https://wiki.hoodedhorse.com/Manor_Lords/Special%3AMyLanguage/regional_wealth

### Recomendação atual

**Não mandar aluguel bruto para o Caixa da Cidade por padrão.**

A direção mais coerente parece ser:

- Caixa da Cidade recebe principalmente **impostos e receitas públicas**;
- empresas privadas continuam com caixa próprio;
- se houver aluguel privado, ele deve ir para uma entidade privada real e auditável, preferencialmente reaproveitando o modelo de empresa;
- se houver propriedade familiar, a família proprietária não paga aluguel e o imóvel pode se tornar patrimônio/herança;
- imposto sobre propriedade e/ou outras taxas podem alimentar o Caixa da Cidade.

Porém, **não promover propriedade/herança para a SPEC ainda**. Antes disso, decidir se essa profundidade gera gameplay suficiente para entrar no primeiro escopo ou se deve ser uma evolução posterior.

### Pergunta macro restante

A decisão realmente importante agora não é "quem recebe R$ X de aluguel", mas:

**o primeiro escopo já deve distinguir família proprietária de família locatária, ou todas as famílias começam num modelo de aluguel e propriedade/herança entra depois?**

Essa escolha define o tamanho real do sistema de moradia.


---

---

## Clarificação: para onde vai o aluguel

**Status:** exploração; nenhuma escolha promovida para a SPEC ainda.

A proposta anterior de usar uma "empresa imobiliária" como recebedora padrão de todo aluguel foi considerada insuficientemente clara.

### Regra econômica desejada

**Dinheiro de aluguel não deve desaparecer nem ir para um agente genérico sem dono.**

Se houver aluguel real:

família locatária
→ paga aluguel
→ **proprietário real daquela unidade residencial**

O Caixa da Cidade recebe apenas os impostos/taxas aplicáveis.

### Modelo mais coerente em exploração: propriedade por unidade

Cada casa ou unidade de apartamento teria um proprietário econômico explícito.

O proprietário poderia ser:

- uma família;
- uma entidade privada proprietária/operadora de imóveis;
- eventualmente o próprio setor público em casos específicos, se houver moradia pública no futuro.

Consequências:

**Família proprietária**
- mora na própria unidade;
- não paga aluguel a si mesma;
- pode pagar imposto/taxas e manutenção;
- o imóvel é patrimônio;
- no futuro pode ser transferido por venda ou herança.

**Família locatária**
- ocupa imóvel de outro proprietário;
- paga aluguel ao proprietário real;
- não possui aquele patrimônio.

**Entidade privada proprietária**
- recebe aluguel das unidades que possui;
- paga impostos e custos definidos;
- seu fluxo precisa ser auditável se for modelada como agente econômico real.

### Por que isso é mais limpo

Prós:
- cada pagamento tem origem e destino;
- propriedade e aluguel usam a mesma regra;
- uma futura herança passa a ser uma simples transferência de propriedade;
- imposto continua sendo a principal ponte de receita para o Caixa da Cidade;
- não existe "landlord virtual" escondido;
- a diferença proprietário versus locatário tem consequência real.

Contras:
- precisamos manter o proprietário de cada unidade;
- compra/venda e herança ficam conceitualmente conectadas ao sistema;
- apartamentos podem ter propriedade por unidade, aumentando estado persistente;
- ainda é necessário decidir quem é o proprietário inicial quando o jogador constrói uma nova moradia.

### Ponto crítico revelado

A pergunta "para quem vai o aluguel?" mostra que **todas as famílias alugarem sem modelar propriedade não é tão simples quanto parecia**.

Ou:
1. existe um proprietário real;
2. o Caixa da Cidade vira proprietário/locador;
3. ou o dinheiro vai para uma abstração/sumidouro.

A opção 3 contradiz o princípio de causalidade do projeto. A opção 2 conflita com a intenção de uma economia centrada principalmente em impostos.

Por isso, a hipótese de **propriedade residencial real, porém automática para o jogador**, ganhou força.

### O que não precisa vir junto

Modelar propriedade não obriga imediatamente a implementar:
- hipoteca;
- financiamento bancário;
- cartório;
- processo jurídico;
- escolha manual de proprietário pelo jogador;
- mercado financeiro imobiliário detalhado.

O estado mínimo pode ser apenas:

unidade residencial
→ owner entity id
→ occupant household id
→ preço/aluguel
→ impostos/custos aplicáveis

Isso permite profundidade econômica sem obrigar microgestão.

### Questão macro ainda aberta

**Quem recebe a propriedade inicial de uma nova residência construída pelo jogador?**

Alternativas a comparar antes de decidir:
- fica inicialmente no Caixa da Cidade/desenvolvedor e depois é vendida ou alugada;
- é adquirida automaticamente por uma família com capacidade financeira;
- é adquirida por uma entidade privada real de propriedade imobiliária;
- existe uma regra híbrida conforme demanda e capacidade de compra.

Não decidir esse ponto por conveniência técnica; ele define o fluxo monetário de moradia.


---

---

## Mercado de aquisição ligado à conexão exterior

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

## Decisão: patrimônio individual e investimento residencial local

**Status:** decidido no modelo conceitual; fórmulas e regras familiares ainda precisam de calibração.

Foi aprovado que os cidadãos terão **dinheiro real individual** e que isso pode gerar investimento residencial emergente.

### Carteira/patrimônio do cidadão

Cada SIM possui saldo monetário real.

Esse saldo pode ser afetado por:
- salários e outras rendas definidas;
- consumo;
- impostos;
- aluguel pago ou recebido;
- compra de imóvel;
- venda futura de patrimônio;
- demais fluxos econômicos explícitos.

Ao criar/introduzir um cidadão na simulação, ele pode entrar com um saldo inicial explícito. Para migrantes vindos do exterior, esse dinheiro representa patrimônio trazido do restante do mundo.

O valor inicial não deve ser mágico nem infinito:
- precisa seguir uma regra de geração;
- deve ser calibrável;
- precisa aparecer em debug/diagnóstico;
- não pode funcionar como fonte ilimitada de capital externo.

A regra exata por idade, perfil, família e origem ainda está aberta.

### Investimento residencial por cidadãos locais

Um cidadão/família que acumulou dinheiro suficiente pode decidir automaticamente comprar outro imóvel.

Fluxo conceitual:

cidadão acumula patrimônio
→ encontra imóvel disponível
→ avalia preço + demanda + expectativa de ocupação/renda
→ compra com dinheiro real
→ torna-se proprietário
→ outra família pode alugar
→ aluguel vai para o proprietário real
→ cidade recebe impostos aplicáveis

Isso cria uma diferença econômica visível entre:
- renda do trabalho;
- consumo;
- poupança;
- patrimônio;
- renda de aluguel.

### Prós

- aluguel deixa de precisar de empresa imobiliária abstrata;
- dinheiro sempre tem origem e destino;
- cidadãos ricos podem se comportar de forma diferente de cidadãos sem patrimônio;
- herança futura ganha significado concreto;
- desigualdade patrimonial pode emergir sem um score artificial;
- propriedades vazias e demanda imobiliária passam a ter agentes reais;
- reforça o valor de cidadãos persistentes.

### Contras/riscos

- aumenta o estado econômico por cidadão;
- pode gerar concentração extrema de imóveis se não houver comportamento/limites plausíveis;
- exige cuidado para não transformar a simulação em mercado financeiro imobiliário excessivamente detalhado;
- casais/famílias levantam a questão de quem juridicamente possui o imóvel;
- dinheiro inicial de novos cidadãos precisa ser limitado para não criar capital externo infinito.

### Regra de microgerenciamento

O jogador não escolhe qual cidadão compra qual imóvel.

A decisão é automática, mas deve ser diagnosticável:
- comprador;
- preço;
- dinheiro antes/depois;
- motivo econômico da compra;
- imóvel adquirido;
- renda/aluguel esperado quando aplicável.

### Granularidade decidida para o primeiro modelo

O saldo individual do SIM foi aprovado e a propriedade residencial terá **um único titular cidadão**.

Um casal/família pode somar dinheiro para uma compra, mas:
- um SIM específico fica registrado como proprietário;
- aluguel recebido vai para esse proprietário conforme as regras econômicas definidas;
- copropriedade fica fora do primeiro modelo.

Essa simplificação preserva dinheiro e propriedade concretos sem exigir divisão jurídica de ativos.

### Coerência com comprador exterior

Permanece a decisão anterior:
- família residencial vinda do exterior compra para **migrar e morar**;
- investimento imobiliário externo puro continua fora do primeiro modelo.

Depois de migrar e tornar-se residente real, essa família/cidadão pode futuramente acumular patrimônio e participar das mesmas regras de investimento local.


---

---

## Propriedade humana das empresas

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

## Fluxo do player após concluir comércio/indústria

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
