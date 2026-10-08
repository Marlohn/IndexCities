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
- o pagador real da importação transfere o valor para a **Reserva Global**;
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
- receitas de exportação que pertençam ao setor público, quando houver;
- outras receitas municipais que forem adicionadas futuramente.

Exportações privadas pertencem ao agente econômico que vende a mercadoria; o pagamento externo sai da Reserva Global e não entra automaticamente no Caixa da Cidade.

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

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — a direção atual foi discutida e promovida para a SPEC; o histórico de alternativas permanece resumido.

**Status:** decidido no modelo atual.

O jogador controla o desenvolvimento físico da cidade e financia, pelo **Caixa da Cidade**, as obras públicas e privadas que ele ordena. Depois da conclusão, a operação econômica diverge:

- serviços públicos permanecem ligados ao Caixa da Cidade;
- comércio e indústria privados passam a ser propriedade/operação de empresas reais com caixa próprio;
- residências passam para proprietários privados conforme as regras de aquisição residencial.

O custo de construir atividade privada é deliberadamente responsabilidade do Caixa da Cidade no gameplay. Isso preserva o loop único de construção e evita criar financiamento privado, incorporadoras ou aprovações paralelas.

A empresa privada **não recebe capital operacional do Caixa da Cidade** por padrão. Uma empresa nova vinda do exterior recebe seu capital inicial da **Reserva Global**; empresa local já existente usa o próprio caixa.

Alternativas históricas consideradas:
- economia totalmente municipalizada;
- obra privada financiada por investidor/empresa;
- modelo híbrido com financiamento privado da implantação.

Essas alternativas foram superadas para o escopo atual porque adicionavam etapas sem melhorar o loop principal.

## Dinheiro privado por empresa: validação de gameplay

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido após pesquisa e revisão de causalidade. Caixa real por empresa foi escolhido, sem microgestão bancária pelo jogador.

Foi confirmado que fazendas, mercados, postos, fábricas e demais atividades econômicas colocadas pelo jogador podem ser operadas por empresas privadas.

A questão foi fechada após a exploração abaixo: cada empresa terá **saldo monetário explícito e auditável**. As alternativas mais abstratas permanecem registradas apenas como histórico.

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

A empresa precisa de capital operacional explícito, mas esse capital **não é transferido pelo Caixa da Cidade por padrão**. No primeiro modelo:
- empresa nova vinda do exterior recebe capital inicial finito da **Reserva Global**;
- empresa local já existente usa seu próprio caixa para expandir;
- criação de empresa nova puramente local continua fora do primeiro modelo até existir uma fonte concreta de capital.

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
- exige calibrar capital inicial e liquidez para que falências reflitam causas econômicas reais, sem bailout invisível;
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

Para entrar como empresa nova vinda do exterior, a entidade recebe capital inicial explícito retirado da **Reserva Global**. Não existe investidor abstrato criando moeda nova.

Se a empresa operar com prejuízo persistente, reduz produção/emprego e eventualmente fecha. Isso preserva consequência econômica real com caixa explícito, mantendo a contabilidade automática para não virar microgerenciamento.

### Critério para aprofundar depois

Só promover **crédito, dívida empresarial, recapitalização e investidores individuais** a novos sistemas reais se aparecer gameplay que dependa deles, por exemplo:
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
- capital inicial explícito e inspecionável, vindo da Reserva Global para empresas externas ou do próprio caixa para empresas locais existentes.

A UI normal não precisa mostrar toda contabilidade, mas o painel detalhado/debug deve conseguir reconstruir por que a empresa piorou.

A comparação foi encerrada em favor de **caixa explícito real**, por ser o modelo mais previsível, calibrável e explicável. A operação desse caixa continua automática para evitar microgerenciamento.

---

---

---

## Revisão: quem paga quando o jogador constrói

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — decisão discutida e promovida para a SPEC; calibração de valores continua aberta.

**Status:** decidido para o escopo atual.

O loop é único:

1. o jogador escolhe o prédio;
2. o jogo mostra dinheiro + materiais;
3. o **Caixa da Cidade** paga a obra;
4. pagamentos a trabalhadores/fornecedores locais vão para os agentes reais;
5. parcelas com contraparte externa ou não modelada vão para a **Reserva Global**;
6. materiais são entregues fisicamente e a obra acontece;
7. se for um ativo privado, ele entra no fluxo de aquisição correspondente.

### Construção privada

Construir comércio, indústria ou residência privada continua sendo custo do Caixa da Cidade. Isso é uma escolha de gameplay, não uma representação jurídica literal.

O custo pode incluir:
- materiais;
- frete/importação;
- mão de obra/execução;
- demais custos de obra já definidos.

Ele **não inclui capitalização operacional automática da futura empresa**.

Quando uma empresa externa entra para adquirir um ativo, seu capital vem da Reserva Global. O preço da primeira aquisição também vai para a Reserva Global, não retorna ao Caixa da Cidade.

### Consequência de gameplay

O jogador não recupera automaticamente o custo construindo e revendendo ativos. A recuperação financeira da cidade acontece principalmente por impostos e demais receitas públicas ao longo da operação.

Isso evita transformar o jogo em um simulador de incorporação e mantém o orçamento municipal relevante.

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

O destino principal após falência já foi fechado:
- a falência da empresa é terminal;
- pode existir uma liquidação técnica curta e automática;
- imóveis podem ser ofertados automaticamente enquanto a empresa ainda existe apenas para liquidar ativos;
- se um ativo ficar sem titular, pode permanecer sem proprietário e uma venda posterior envia o valor à Reserva Global;
- saldo final sem outro titular econômico definido retorna à Reserva Global.

Continuam fora/abertos:
- recuperação judicial;
- crédito empresarial;
- intervenções ou subsídios específicos;
- tratamento detalhado de estoques e outros ativos quando esses casos existirem.


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
- moradia participa da economia familiar; aluguel vai ao proprietário real e a primeira aquisição segue a Reserva Global.

A abstração evita criar vários bolsos de investimento controlados pelo jogador sem eliminar caixas reais de entidades simuladas quando eles geram gameplay.


---

---

---

## Mercado de aquisição ligado à conexão exterior

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o modelo atual foi discutido e promovido para a SPEC; fórmulas e preços seguem em calibração.

**Status:** decidido em nível de fluxo.

### Residencial

- residência privada concluída pode ficar vazia aguardando comprador;
- família local pode comprar com dinheiro real;
- família exterior só compra para **migrar e morar**;
- investimento residencial externo puramente passivo fica fora do primeiro modelo;
- o pagamento da **primeira aquisição** vai para a Reserva Global;
- depois disso, revendas normais transferem dinheiro entre proprietários privados e aluguel vai ao proprietário real.

### Comércio e indústria

- prédio concluído pode ficar vazio/procurando empresa;
- empresa local existente ou empresa nova vinda do exterior pode adquirir;
- a empresa adquirente vira proprietária + operadora;
- empresa local usa caixa próprio;
- empresa externa recebe capital inicial finito da Reserva Global;
- o pagamento da primeira aquisição vai para a Reserva Global;
- depois da aquisição, operação, salários, estoque, impostos e falência pertencem ao caixa da empresa.

### Demanda e limite exterior

A Reserva Global **não é um comprador automático**. A procura continua limitada por condições reais e diagnosticáveis:
- residencial: preço, emprego, moradia disponível, localização e atratividade;
- comércio/indústria: clientes, concorrência, trabalhadores, insumos, logística, localização e custos.

Baixa oportunidade pode deixar ativos vazios. O jogador não escolhe manualmente o comprador.

### Riscos já resolvidos

Foram descartados:
- pagamento da primeira aquisição retornando ao Caixa da Cidade, por reciclar capital;
- comprador exterior com dinheiro criado infinitamente;
- proprietário residencial exterior invisível apenas para receber aluguel;
- negociação manual de propostas.

A oferta monetária fixa + Reserva Global fecha a origem/destino do dinheiro sem remover vacância, demanda ou risco econômico.

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
- entra com capital inicial explícito retirado da Reserva Global;
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
- empresas novas externas podem entrar com capital explícito retirado da Reserva Global;
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
8. o valor da compra vai para a Reserva Global;
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

O capital inicial é retirado da **Reserva Global**. Deve ser explícito, limitado e diagnosticável; a entrada da empresa redistribui dinheiro existente, sem criar moeda.

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
- Quando não houver mais ativos pendentes, saldo remanescente sem outro titular econômico definido volta para a Reserva Global e a empresa é removida.
- Se algum ativo perder o titular com o encerramento, ele pode ficar explicitamente sem proprietário; uma venda posterior envia o valor à Reserva Global com a origem registrada.
- Isso não impede uma empresa saudável com vários estabelecimentos de fechar apenas um local e continuar operando os demais.

### Por que esta versão foi escolhida

**Prós**
- fecha todos os fluxos sem dinheiro ou propriedade desaparecerem;
- não exige que o jogador acompanhe liquidação, credores ou processo jurídico;
- falência continua tendo consequência clara e terminal;
- evita acumular empresas “zumbis”;
- reutiliza a Reserva Global para valores sem titular, registrando a origem como patrimônio não reclamado quando aplicável.

**Contras**
- existe um pequeno estado técnico de liquidação;
- detalhes de estoque e outros ativos terão de ser definidos quando esses casos realmente existirem;
- a Reserva Global também recebe valores sem titular econômico, sempre preservando a origem do lançamento.

O objetivo é manter essa camada no mínimo necessário para preservar causalidade. Não adicionar credores, ordem jurídica de pagamento, administrador judicial ou outras regras enquanto não houver gameplay concreto que justifique isso.


---

## Reserva Global, oferta monetária fixa e primeira aquisição privada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — conceito central confirmado diretamente pelo responsável; números e calibração continuam abertos.

**Status:** decisão promovida para a SPEC.

### Regra central

Existe uma **oferta monetária global fixa**. O dinheiro total não é criado nem destruído pelos fluxos normais; ele fica distribuído entre:

- Caixa da Cidade;
- carteiras dos SIMs;
- caixas das empresas;
- **Reserva Global**.

A soma deve permanecer constante e auditável.

### Papel da Reserva Global

A Reserva Global é um saldo técnico de bastidor, não controlado pelo jogador. Todo lançamento registra origem, destino, motivo e entidade/evento associado.

Ela fornece dinheiro para:
- capital inicial de famílias que entram do exterior;
- capital inicial de empresas que entram do exterior;
- principal de empréstimos municipais.

Ela recebe dinheiro de:
- importações/fluxos cuja contraparte seja externa;
- patrimônio monetário sem titular econômico definido;
- saldo final de empresa encerrada sem titular;
- primeira aquisição de residências, comércios e indústrias construídos pela cidade;
- amortizações de empréstimos e, se houver, juros.

### Regra de destinatário real

A Reserva Global não é uma lixeira contábil. Se existe um destinatário real modelado — trabalhador, fornecedor, empresa, proprietário ou prefeitura — o pagamento vai para esse agente.

Exemplo de construção:
- salário de trabalhador local → SIM;
- compra de concreto local → empresa fornecedora;
- parte importada → Reserva Global.

### Empréstimos

O principal do empréstimo sai da Reserva Global e entra no Caixa da Cidade. A amortização volta para a Reserva Global. Taxa de juros, prazo, limite e condições ainda são parâmetros de calibração.

### Nome

Nome adotado: **Reserva Global**.

“Oferta monetária global” descreve o total fixo de dinheiro; “Reserva Global” é apenas a parcela que está fora dos agentes econômicos locais naquele momento.


## Tributação por categorias — direção confirmada e calibração aberta

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou controle de impostos pelo jogador, influência na atratividade e cobrança somente após existir proprietário privado; consequências detalhadas abaixo são apenas hipóteses da IA, pendentes de revisão.

**Decidido na SPEC:** tributos de construções por uso residencial, comercial ou industrial, com seis controles independentes (baixa/alta densidade para cada uso); o jogador ajusta as categorias sem administrar contribuintes individualmente. O imposto é proporcional ao **valor de mercado do imóvel**, calculado pela alíquota de sua categoria; não é calculado diretamente pelo tamanho. A cobrança começa com proprietário privado real: SIM proprietário residencial ou empresa proprietária de estabelecimento comercial/industrial. Não criar pagador fictício para imóveis sem proprietário.

**Exploração pendente:**
- **Decidido:** seis combinações uso × densidade têm controles independentes. Avaliar apresentação enxuta para não transformar os seis ajustes em microgerenciamento.
- Avaliar efeitos causais diferenciados: impostos residenciais podem afetar capacidade de pagar moradia e decisão de compra/migração; impostos comerciais/industriais podem afetar margem de operação, demanda por estabelecimentos, contratação e sobrevivência. Efeitos não são regras confirmadas.
- **Base de cálculo decidida:** valor de mercado do imóvel × alíquota da categoria. Ainda definir método de avaliação/atualização desse valor, periodicidade, inadimplência e situações de ocupação sem propriedade.
- Não confundir isenção de imposto em ativo ainda sem proprietário com eventual custo de manutenção: este último não foi decidido.
- Evitar indicador mágico de atratividade: as consequências devem ser explicáveis por custos e decisões efetivos dos SIMs/empresas.
