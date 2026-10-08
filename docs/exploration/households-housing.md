# IndexCities — Famílias, moradia e patrimônio

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa; parte do modelo de propriedade e investimento já foi promovida para a SPEC — não é fonte de verdade.
>
> Reúne renda e consumo familiar, valor imobiliário, vulnerabilidade financeira, aluguel, propriedade, herança e investimento residencial por cidadãos.

## Como ler este documento

Este arquivo é material de exploração temática. A autoridade do produto continua sendo `docs/SPEC.md`. Trechos históricos podem preservar alternativas já superadas; quando houver divergência, vale a decisão canônica mais recente.

---

## Dinheiro, renda familiar e consumo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido. Os saldos monetários individuais e a **cooperação financeira automática entre adultos nas despesas comuns** já são canônicos; a distribuição exata dessas despesas e decisões financeiras familiares continua em exploração.

### Renda individual e familiar

Direção atual:

- **todo SIM possui saldo monetário individual real**, conforme a SPEC; para crianças esse saldo pode ser zero ou receber apenas fluxos explicitamente definidos;
- **Decidido:** adultos da mesma família podem contribuir automaticamente com recursos reais dos próprios saldos para despesas comuns, sem carteira monetária familiar separada; qualquer movimentação entre SIMs é transferência real, não criação de dinheiro;
- **Decidido:** cada locação continua tendo **um único SIM titular responsável pelos aluguéis e pelas dívidas de aluguel perante o proprietário**. Contribuições de outros adultos para pagamentos atuais podem ser transferidas ao titular antes da quitação; **os outros adultos não assumem automaticamente as dívidas pessoais dele**, nem passam a sofrer descontos salariais por essas dívidas;
- **Decidido:** quando a renda/saldo disponível não bastar para tudo, a família prioriza **necessidades essenciais, incluindo alimentação e moradia**, antes de lazer e consumo não essencial. A redução de consumo não essencial vem primeiro; a falta do essencial provoca consequências reais e observáveis, sem criar dinheiro ou mercadorias;
- **Em aberto para calibração/implementação:** quanto cada adulto contribui, ordem fina entre necessidades essenciais, momentos de contribuição, efeitos e limiares de necessidades não atendidas, tratamento das despesas dos dependentes e casos sem adultos ou sem recursos suficientes.

A leitura familiar pode calcular **renda familiar disponível** e despesas comuns do domicílio como agregados derivados. Isso preserva individualidade sem exigir microgerenciamento bancário. A cooperação ocorre por decisões automáticas e movimentações entre agentes reais, e não torna dívida pessoal uma dívida coletiva. A prioridade para moradia não elimina a possibilidade de aluguel atrasado, e a prioridade para alimentação não garante comida sem estoque ou pagamento real.

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

**Decisões fechadas na SPEC:** o modelo inicial será **por categorias de produtos**, não por SKU individual. **As compras de bens de consumo pelos SIMs são presenciais:** o cidadão se desloca fisicamente até o comércio, compra somente categorias disponíveis no estoque com dinheiro real e realiza o deslocamento de saída/retorno, afetando mobilidade e demanda local. A transação transfere dinheiro do SIM à empresa operadora e retira estoque real; não substituir a viagem por uma compra econômica invisível. A visita e compra são automatizadas pela simulação, sem microgerenciamento do jogador. **A escolha da loja considera preço, distância/acessibilidade e estoque disponível; diante de falta de produto, o SIM pode tentar outro comércio acessível, realizando o deslocamento físico necessário. Sem loja viável, a compra não ocorre e a necessidade fica não atendida**, sem compra ou abastecimento fictício. A categoria econômica do produto, a necessidade de deslocamento e a escolha inteligente de comércio são decisões distintas.

Razões preservadas desta exploração:

- mantém a relação real entre produção, logística, estoque, consumo e falta de produtos;
- permite que comércio tenha motivo para receber entregas;
- permite que preço e escassez afetem cidadãos e empresas;
- evita explodir o número de entidades com milhares de SKUs que provavelmente não gerariam gameplay proporcional;
- deixa espaço para aprofundar categorias específicas mais tarde se elas se provarem importantes.

**Decidido na SPEC — estoque doméstico:** compras de bens para uso do domicílio, inicialmente **Alimentos**, abastecem um **estoque real da família por categoria**, com quantidades agregadas (sem SKU, embalagens nem inventários pessoais). A família compra para vários dias, leva os bens para casa em deslocamento físico e os utiliza gradualmente; a redução da quantidade em casa representa consumo, **não uma segunda compra ou pagamento**. Compras de reposição são desencadeadas pelo consumo/estoque conforme a calibração. Se o comércio ficar sem alimentos, a família pode continuar consumindo o que já armazenou; quando também esgotar sua reserva e não conseguir comprar, a necessidade fica não atendida, sem estoque gerado artificialmente. Este estoque não é o estoque comercial e tampouco é carteira monetária familiar.

**Em aberto para calibração/protótipos:** quantidade por compra, ritmo de consumo conforme moradores, reposição e agrupamento de categorias por viagem, pesos de preço/distância/estoque na escolha da loja, alcance e número de alternativas, conciliação com trabalho/atividades, transporte de produtos sem simular cada embalagem, destino das reservas domésticas em mudança ou perda de moradia e custo de processamento em grande escala. Não exigir varreduras contínuas de comércios nem uma viagem individual para cada produto; preservar viagens, quantidades e transações reais mesmo com otimização fora da câmera.

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

---

## Intervenções sobre imóvel privado concluído — questão transversal (2026-10-08)

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou em 2026-10-08 a intervenção municipal com indenização automática ao proprietário (opção B), sem travar remodelação da cidade, e o princípio global de consequências humanas observáveis. A análise complementar, cálculo da indenização e detalhes de transição continuam não revisados/abertos.

A propriedade residencial está registrada em SIM real, que pode morar no imóvel ou alugá-lo a outra família. Realocar a **estrutura** e realocar o **domicílio** são operações distintas: mudar localização altera potencialmente valor de mercado, acesso e bem-estar; na locação, proprietário e ocupante têm interesses distintos. **A SPEC agora permite a intervenção municipal mesmo contra a vontade do proprietário, desde que seja indenizado em dinheiro real do Caixa da Cidade.** A família ocupante procura automaticamente moradia compatível seguindo regras existentes, podendo ficar sem moradia ou emigrar; pagar o dono não significa pagar ou garantir casa ao locatário.

A ferramenta de demolição também respeita a indenização. **A indenização foi aprovada como exatamente 100% do valor de mercado do imóvel na localização original, antes da intervenção**, calculado pelo modelo econômico já existente, sem desconto ou acréscimo. A prefeitura transfere esse valor uma única vez do Caixa ao proprietário registrado. O morador que aluga a casa não recebe indenização imobiliária automática nem moradia garantida. **Desocupação assistida foi aprovada em 2026-10-08 (opção C):** em intervenção municipal sobre residência **ocupada**, a família tem uma transição curta, em que busca automaticamente outro imóvel compatível, sem negociação individual do jogador. Se sair antes, a intervenção pode prosseguir; se o período acabar sem nova casa, desocupa mesmo assim e pode ficar sem moradia ou emigrar conforme a SPEC, sem reassentamento ou dinheiro fictício. Enquanto o imóvel original ainda existir e estiver ocupado, seu proprietário permanece registrado e o aluguel mensal devido permanece com ele. Antes de remover o ativo, a indenização integral é paga uma vez ao proprietário real. Não criar aluguel futuro do imóvel já removido nem perdoar dívidas vencidas pelo deslocamento. **Os dias exatos são calibração;** os prazos de três meses de outras causas de desocupação não são automaticamente reutilizados. O tratamento dos detalhes de liquidação e situações-limite ainda precisa de validação. Nenhum preço, moradia ou dinheiro pode ser criado só para eliminar as consequências.

Os cenários de propriedade e as alternativas anteriores estão no [documento de interface](player-interface.md#realocação-de-prédios-prontos-opção-c-e-intervenção-indenizada-b-aprovadas-2026-10-08). **A intervenção com indenização já foi aprovada e consta na SPEC**, sem consentimento obrigatório do dono e sem assentamento fictício. Não inventar **número exato de dias** para a transição curta aprovada, custo adicional nem realocação gratuita: esses detalhes ainda precisam de calibração ou decisão pertinente. **O critério monetário de indenização já está fechado na SPEC** e não é apenas uma referência aproximada.

## Valor imobiliário e propriedade de lotes

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou valor de mercado dinâmico como base de impostos; critérios específicos, pesquisa e método de avaliação abaixo seguem **PENDENTES** de revisão.


**Status:** em exploração.

Ainda não está decidido se cada lote terá proprietário individual e valor de mercado próprio.

Já está decidido na SPEC que a **tributação de imóveis residenciais, comerciais e industriais incide sobre o valor de mercado**, com alíquota da categoria escolhida pelo jogador. Também foi confirmado que **o valor dos imóveis é dinâmico e estimado pelas condições concretas da cidade**: localização, acesso a serviços, poluição, demanda e características do imóvel podem fazer o valor subir ou descer. **A estimativa funciona desde o primeiro imóvel, sem exigir vendas anteriores como referência.** Esse foi o modelo escolhido em vez de basear o valor principalmente nas negociações passadas. O modelo deve explicar as causas da variação ao jogador e não criar/destruir moeda: reavaliação patrimonial não é movimentação financeira. No primeiro modelo, **o preço da primeira compra e das revendas é igual ao valor de mercado calculado**, sem desconto/acréscimo negociado; tamanho pode ser um fator de avaliação, mas não é a base direta do imposto. Ainda não foram decididos fórmula, pesos, frequência de atualização nem diferenciação precisa entre tipos de imóvel. A opção de negociações automáticas conforme oferta/demanda foi guardada apenas como **possível melhoria futura**, sem compromisso de implementação.

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

A dívida municipal faz parte do produto como mecanismo de recuperação de caixa.

Já está decidido:
- o principal do empréstimo sai da **Reserva Global** e entra no Caixa da Cidade;
- amortizações retornam à Reserva Global;
- se houver juros, eles também são transferidos para a Reserva Global;
- o empréstimo apenas redistribui a oferta monetária fixa e não cria moeda nova.

Ainda precisa ser decidido:
- limite de endividamento;
- taxa de juros;
- prazo;
- consequências de inadimplência;
- se o empréstimo exige ação explícita do jogador ou pode ser apresentado como ferramenta de emergência.


---

---

---

## Falência pessoal, população sem moradia e demanda imobiliária

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração; existência desses fenômenos e o princípio de prazo com dívida real por aluguel atrasado já estão decididos na SPEC.

### Falência pessoal

**Decidido na SPEC — primeiro modelo:** a falência pessoal é **uma condição econômica**, derivada do dinheiro real insuficiente perante obrigações e dívidas do SIM, com consequências no consumo financiável, na manutenção dos débitos e na possibilidade de manter ou encontrar moradia. Não exige processo jurídico, liquidação patrimonial compulsória própria, renegociação formal nem perdão automático por falir. A dívida continua obedecendo às regras específicas já aprovadas, inclusive quitação/extinção na morte quando cabível. Sem novos saldos monetários fictícios ou burocracia por cidadão. **Em calibração:** critérios quantitativos de entrada/saída dessa condição; não criar subsistema de falência separado sem nova decisão humana.

**Decisão confirmada na SPEC para atraso de aluguel:** famílias que deixam de pagar acumulam **dívida real perante o SIM proprietário** e, após **3 meses de inadimplência**, contados do primeiro aluguel mensal vencido e não quitado, **podem perder a moradia** caso ainda haja dívida. **Pagamento parcial reduz o saldo devedor, mas não reinicia nem suspende o prazo. Quitação integral encerra a inadimplência e zera a contagem; novo atraso inicia novo prazo.** A passagem dos 3 meses não obriga despejo automático: seus gatilhos de efetivação ainda serão definidos. Registrar valor devido não cria dinheiro: só o pagamento efetivo entre agentes altera saldos. Não há necessidade de intervenção do jogador em cada cobrança.

**Pontos ainda em exploração:**

- **Decidido:** o prazo de inadimplência é de **3 meses** (distinto dos dois prazos de 3 meses por morte sem herdeiro e compra para moradia própria). A partir dele existe possibilidade de perda da moradia, não despejo instantâneo obrigatório;
- **Decidido:** pagamentos parciais abatem a dívida sem reiniciar ou suspender o prazo de 3 meses. A quitação integral encerra a inadimplência e zera o prazo; um novo atraso abre outra contagem;
- **Em aberto:** critérios concretos para efetivar a perda da moradia depois dos 3 meses com dívida ainda pendente;
- eventual parcelamento formal, prioridade entre contas e encargos por atraso (nenhum juro está aprovado);
- **Decidido:** a dívida de aluguel **permanece após a família deixar o imóvel ou mudar de residência**; não há perdão automático ao sair. O crédito permanece vinculado ao credor real e o pagamento posterior, quando ocorrer, será transferência monetária entre agentes;
- **Decidido:** na morte do SIM devedor, o saldo monetário individual disponível é usado para quitar aluguel atrasado até o limite devido; a parte não paga é encerrada e não passa aos herdeiros/familiares. Eventual dinheiro restante segue o destino patrimonial já definido, inclusive Reserva Global quando não houver herdeiro elegível;
- **Decidido:** cada locação tem um **único SIM titular**, responsável pelo aluguel e pelo débito atrasado; os demais adultos do domicílio não são devedores automáticos. Não criar uma dívida coletiva da família nem divisão proporcional entre adultos;
- **Decidido:** se o SIM titular da locação morrer e a família tiver outro adulto, **a locação e a moradia da família continuam**: um dos adultos remanescentes passa a responder pelos **próximos aluguéis**, não pelas dívidas pessoais antigas do falecido. Estas são liquidadas com o saldo individual disponível no falecimento e o restante é encerrado, conforme a SPEC;
- **Decidido:** quando um herdeiro assume o imóvel alugado após a morte do proprietário, os **aluguéis atrasados associados ao imóvel passam a ser devidos a esse herdeiro**; a dívida permanece, muda apenas o credor real;
- **Decidido:** se o proprietário credor morrer **sem herdeiro elegível**, os aluguéis atrasados que lhe eram devidos são **perdoados**, sem transferência à Reserva Global ou ao futuro comprador; a dívida contábil é extinta sem alterar a oferta monetária;
- **Decidido:** após a morte do proprietário sem herdeiro, a família que já alugava o imóvel tem **3 meses do calendário da simulação, desde que o imóvel fica sem proprietário** para continuar morando sem pagar aluguel. Busca nova moradia durante esse prazo; **ao vencer o prazo, se não houver uma nova locação/propriedade que permita a permanência, deve sair desse imóvel**. Sem alternativa residencial na cidade, **pode emigrar**, sem que a emigração seja obrigatória: o estado de população sem moradia permanece possível. Nenhum aluguel/débito referente ao período sem proprietário é criado ou cobrado retroativamente;
- **Decidido:** o imóvel ainda ocupado pode ser comprado por comprador real elegível, que decide automaticamente se deseja **morar nele ou continuar alugando à família atual**. Na segunda hipótese, ele passa a receber aluguéis futuros; na primeira, a família ocupante tem **3 meses do calendário da simulação, contados da compra**, para procurar outra moradia e sair, sem despejo instantâneo. **Durante esses 3 meses, enquanto a família permanecer no imóvel, o aluguel é pago ao novo proprietário**, sem retroagir ao intervalo em que a casa esteve sem dono. Não assumir reajuste imediato apenas pela aquisição;
- **Em aberto:** escolha do novo locatário titular, ausência de adulto na residência, múltiplos credores, eventual calibração do prazo inicial de 3 meses, critérios da escolha automática entre emigração e condição sem moradia, outras cobranças e limiares da falência pessoal;
- como buscar uma residência mais barata ou assistência, sem transformar a inadimplência em microgerenciamento;
- consequências de outras despesas não pagas;

Outras respostas à vulnerabilidade financeira ainda possíveis:
- perda de acesso a bens/serviços;
- mudança para moradia mais barata;
- busca emergencial de emprego;
- apoio social;
- endividamento pessoal ou ausência dele;
- efeitos sobre bem-estar, saúde e criminalidade.

A consequência deve criar dinâmica sistêmica sem virar punição arbitrária.

**Alerta de complexidade interna — revisão humana desta nota: PARCIALMENTE REVISADO.** O responsável reforçou que microgerenciamento não significa apenas cliques do jogador: uma rotina invisível de cobrança com múltiplas exceções, prioridades de despesas, contratos e renegociações pode ser complexa demais para manter e simular em larga escala. **Direção escolhida:** descontar um percentual simples da dívida de aluguel **apenas quando o SIM responsável recebe salário**, com limite no valor devido. Existe **um único SIM titular por locação**, sem dívida compartilhada automaticamente por todos os adultos; quando ele morre e existe outro adulto na família, este assume somente a locação e os pagamentos futuros. A porcentagem e os critérios para escolher qual adulto assume continuam abertos. Evitar estados/checagens constantes e prioridades complexas de contas. A morte do **SIM devedor** já tem regra explícita na SPEC; quando o **SIM credor** morre com herdeiro que assume o imóvel, esse herdeiro também assume o crédito do aluguel atrasado. Sem herdeiro elegível, a dívida de aluguel antigo do credor falecido é encerrada; a família permanece sem aluguel durante uma **tolerância de 3 meses**. A compra por novo proprietário permite continuidade do aluguel ou moradia própria do comprador; sem dono, ninguém pode cobrar aluguel. Ao vencer o prazo sem nova locação/propriedade, os ocupantes devem sair do imóvel e podem emigrar se não encontrarem outra casa; não se exige emigração em todos os casos (SPEC).

### População sem moradia

**Decidido na SPEC — realocação após perda da moradia:** em casos como inadimplência, vencimento do prazo em imóvel sem proprietário e compra para moradia própria do novo dono, a família procura **automaticamente** uma residência disponível que consiga pagar, sem intervenção manual do jogador. Se não houver opção compatível na cidade, **pode permanecer sem moradia ou emigrar** conforme suas condições e as da cidade; não presumir emigração automática, casa concedida artificialmente nem garantia de realocação. Este é o fluxo geral; prazos e condições particulares continuam os já definidos na SPEC. A lógica fina de seleção de imóvel e de migração fica para calibração/implementação.

**Escopo inicial confirmado na SPEC:** a população sem moradia é um estado real, que influencia indicadores sociais e migração e deve ser legível ao jogador. A prefeitura reage **indiretamente** com construção/oferta de moradias, empregos, serviços e condições urbanas melhores. **Abrigamentos e programas próprios de assistência social não entram no primeiro modelo.** A simulação não deve inventar benefício monetário, vaga em abrigo ou moradia para eliminar a consequência.

**Possíveis melhorias futuras — PENDENTES de revisão humana, não aprovadas como funcionalidades:**

- **Abrigos temporários** com capacidade, ocupação e custos reais, caso a vulnerabilidade sem moradia crie uma lacuna relevante de gameplay;
- **Assistência social municipal e apoio para obtenção de moradia**, avaliando mecanismos de ajuda e seus custos sem introduzir microgerenciamento por família;
- **Moradia pública ou subsidiada** e/ou apoio à renda/emprego, se a resposta apenas indireta da cidade se mostrar insuficiente;
- **Indicadores e políticas adicionais**, para tornar visíveis a quantidade de famílias sem moradia, as causas, o impacto social e a efetividade de intervenções futuras.

Essas ideias **não são requisitos atuais**. Reavaliar com dados e feedback da simulação inicial, considerando interação com orçamento, empregos, serviços e economia monetária de agentes reais antes de aprovar qualquer nova funcionalidade. O cálculo dos impactos sociais e de migração no escopo inicial continua para calibração.

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

**Decidido na SPEC para o primeiro modelo:** a venda segue o valor de mercado calculado, e o **aluguel residencial é proporcional a esse valor**, sem multiplicador adicional de procura. A procura já pode influenciar indiretamente ambos ao afetar o próprio valor de mercado.

O método de avaliação e a taxa de aluguel ainda exigem calibração. Hipóteses sobre a avaliação (não sobre negociação adicional de aluguel):

- referência de valor imobiliário;
- efeito de oferta/demanda sobre a avaliação;
- modificadores de localização e qualidade;
- limites de variação para evitar instabilidade artificial.

A fórmula de avaliação e o percentual de aluguel proporcional ao valor ainda devem ser calibrados para que o jogador compreenda por que os valores mudam. O primeiro modelo não negocia descontos/acréscimos na venda nem aplica ajuste extra de procura diretamente ao aluguel.


---

---

---

## Destino econômico de aluguel, compra e propriedade residencial

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — fluxo principal discutido e promovido para a SPEC; preço, herança e granularidade de apartamentos ainda têm pontos abertos.

**Status:** decidido em nível de fluxo.

### Construção e primeira aquisição

- o jogador escolhe a residência e a obra é financiada pelo **Caixa da Cidade**;
- a residência pronta pode permanecer temporariamente sem proprietário;
- a primeira família compradora paga com dinheiro real;
- o pagamento da **primeira aquisição vai para a Reserva Global**, não para o Caixa da Cidade;
- o comprador torna-se proprietário real.

### Depois da primeira aquisição

- proprietário que mora no próprio imóvel não paga aluguel a si mesmo;
- locatário paga aluguel ao proprietário real;
- revenda transfere dinheiro do comprador ao proprietário atual;
- a cidade recebe impostos/taxas definidos, não aluguel bruto nem o valor integral da venda.

### Por que essa direção venceu

Ela preserva:
- dinheiro com origem e destino reais;
- propriedade e patrimônio dos cidadãos;
- impostos como principal ponte recorrente de receita para o Caixa da Cidade;
- o diferencial de gameplay em que o jogador/cidade arca com a construção;
- ausência de incorporadora, banco ou landlord abstrato obrigatório.

Foram superadas:
- aluguel retornando ao Caixa da Cidade;
- conta econômica do próprio prédio;
- empresa imobiliária como recebedora padrão;
- primeira venda devolvendo total ou parcialmente o custo ao Caixa;
- financiamento privado obrigatório antes de iniciar a obra.

### Estado mínimo de propriedade

Enquanto um imóvel residencial estiver em propriedade privada:
- existe um proprietário econômico real;
- no primeiro modelo, o titular registrado é um único SIM;
- ocupantes são registrados separadamente;
- um imóvel pode ficar excepcionalmente sem proprietário quando entrar em estado não reclamado.

Ainda abertos:
- percentual de aluguel, dia de vencimento da cobrança **mensal** e parâmetros do cálculo do valor de mercado (preço de venda já definido como igual à avaliação);
- prioridade de herdeiros elegíveis;
- granularidade de propriedade em apartamentos;
- detalhes de revenda e custos recorrentes quando forem necessários.

O histórico de alternativas foi consolidado aqui para evitar que propostas já superadas pareçam decisões atuais.

## Decisão: patrimônio individual e investimento residencial local

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

Ao criar/introduzir um cidadão na simulação, ele pode entrar com um saldo inicial explícito. Para migrantes vindos do exterior, esse saldo é transferido da **Reserva Global** e representa patrimônio trazido do restante do mundo; não há criação de moeda.

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

O saldo individual do SIM foi aprovado e, enquanto houver propriedade privada, a unidade residencial terá **um único titular cidadão**. O estado excepcional de patrimônio não reclamado pode deixar o imóvel explicitamente sem proprietário.

Um casal/família pode somar dinheiro para uma compra, mas:
- um SIM específico fica registrado como proprietário enquanto o imóvel estiver em propriedade privada;
- aluguel recebido vai para esse proprietário conforme as regras econômicas definidas;
- copropriedade fica fora do primeiro modelo;
- patrimônio não reclamado é a exceção explícita em que o imóvel pode ficar sem proprietário.

Essa simplificação preserva dinheiro e propriedade concretos sem exigir divisão jurídica de ativos.

### Coerência com comprador exterior

Permanece a decisão anterior:
- família residencial vinda do exterior compra para **migrar e morar**;
- investimento imobiliário externo puro continua fora do primeiro modelo.

Depois de migrar e tornar-se residente real, essa família/cidadão pode futuramente acumular patrimônio e participar das mesmas regras de investimento local.


---

---


---

## Morte do SIM e destino do patrimônio

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — regra principal discutida e promovida para a SPEC; prioridade de herdeiros e obrigações anteriores continuam abertas.

**Status:** decidido no caso sem herdeiro elegível.

Quando um SIM morre **sem herdeiro elegível**:

- dinheiro remanescente sem outro titular econômico definido volta para a **Reserva Global**;
- o lançamento registra que a origem foi patrimônio sem herdeiro;
- imóvel ou outro ativo físico pode ficar explicitamente **sem proprietário / não reclamado**;
- a Reserva Global não se torna proprietária do ativo;
- se esse ativo for vendido depois, o pagamento entra na Reserva Global com a origem registrada.

A Reserva Global é também a contraparte monetária dos fluxos externos, mas isso não significa que ela controle a conexão física com o exterior. Ela é apenas um ledger monetário de bastidor.

### Por que não fazer a Reserva Global possuir imóveis

- evita transformar a reserva em imobiliária;
- evita aluguel, manutenção e impostos artificiais para um agente técnico;
- preserva um estado simples de ativo sem proprietário.

### Pontos ainda abertos

- quem conta como herdeiro elegível e em qual prioridade;
- **Decidido para aluguel atrasado do SIM falecido:** quitação até o limite de seu saldo monetário disponível antes da herança/Reserva Global, com encerramento do restante devido; o tratamento de outras obrigações ainda está aberto;
- tratamento de outros ativos quando forem introduzidos.

## Primeira aquisição de residência recém-construída

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — fluxo central confirmado diretamente pelo responsável; preço e fórmula de escolha do comprador continuam em calibração.

**Status:** decisão promovida para a SPEC.

Fluxo atual:

Caixa da Cidade financia a obra + materiais
→ residência fica pronta e pode permanecer sem proprietário
→ família local ou família exterior que vai migrar compra com dinheiro real
→ pagamento da **primeira aquisição vai para a Reserva Global**
→ comprador torna-se proprietário
→ revendas futuras acontecem entre proprietários privados
→ aluguel vai ao proprietário real
→ cidade recebe impostos e outras receitas públicas definidas

### Migração pioneira — decisão aprovada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável escolheu explicitamente a entrada por oportunidades concretas (alternativa C) em 2026-10-08; esta síntese complementar da IA não foi revisada integralmente.

A [SPEC](../SPEC.md) agora permite que famílias provenientes do exterior avaliem a migração **sem exigir emprego já garantido**, desde que exista possibilidade real de moradia sob as regras vigentes e que tragam capital próprio explícito, transferido da Reserva Global. A oferta efetiva de vagas e a expectativa de oportunidades influenciam a decisão, mas não se convertem em emprego, salários ou moradores fictícios. Uma família pode perder suas reservas, ficar desempregada ou sair da cidade se as condições não se concretizarem. Moradias disponíveis podem apoiar a avaliação de empresas, **sem serem confundidas com consumidores já presentes**. Critérios de risco, pesos e tempo de avaliação ficam para calibração; não há garantia de migrantes nem gestão manual de entradas.

### Por que o dinheiro não volta ao Caixa

Enviar o valor da primeira aquisição de volta ao Caixa reciclava o capital de construção e diminuía demais a importância dos impostos. A cidade deve sentir o custo de desenvolver moradia e recuperar capacidade financeira principalmente pela atividade econômica e receitas públicas ao longo do tempo.

### Por que usar a Reserva Global

A Reserva Global fecha a origem/destino sem criar incorporadora, banco ou investidor artificial:
- o comprador perde dinheiro real;
- o Caixa não recebe reembolso automático;
- o dinheiro continua dentro da oferta monetária global fixa;
- a origem do lançamento fica auditável.

Foram superadas as alternativas de:
- devolver integralmente ou parcialmente a primeira venda ao Caixa;
- financiar a obra com capital privado antes de construir;
- criar fundos/buckets separados para patrimônio sem titular e liquidação exterior.

O preço de venda no primeiro modelo já foi definido como igual ao valor de mercado calculado. Continuam abertos os detalhes da avaliação e da seleção automática do comprador, não o destino do pagamento.


### Negociação automática de imóveis — possibilidade futura

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável apontou esta alternativa como possível melhoria futura; vantagens, riscos e critérios descritos pela IA continuam **PENDENTES** de revisão.

**Não faz parte do primeiro modelo:** compras/vendas de imóveis por preços negociados automaticamente acima ou abaixo da avaliação, em resposta a oferta/demanda. O primeiro modelo vende pelo valor de mercado calculado, conforme a SPEC. A negociação automática foi mantida apenas como possibilidade para reavaliação futura, sem compromisso de implementação ou prazo.

Hipótese de benefício: negócios mais variados e sensíveis à urgência dos agentes. Riscos a investigar: complexidade de lógica, volatilidade, legibilidade para o jogador e possíveis distorções tributárias caso o preço pago se afaste do valor estimado.


## Aluguel residencial — modelo inicial e melhoria possível

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou aluguel proporcional ao valor de mercado, **pagamento mensal** e **reajuste anual a cada 12 meses** dos contratos, além de apontar ajuste adicional pela procura apenas como possível melhoria. Detalhes de calibração e consequências elaborados pela IA permanecem **PENDENTES** de revisão.

**Decisão oficial (ver SPEC):** no primeiro modelo, cada locação possui **um único SIM responsável pelo aluguel e pela dívida**, sem atribuição automática aos demais adultos. O aluguel residencial é **valor de mercado do imóvel × percentual de aluguel** (a calibrar). O valor de mercado já considera condições da cidade, incluindo demanda; não existe um segundo fator de procura aplicado diretamente ao aluguel. **Contratos em andamento só têm o valor do aluguel recalculado anualmente, a cada 12 meses do calendário da simulação**, e não a cada mudança de avaliação do imóvel ou troca de proprietário. **O pagamento do aluguel é mensal, uma vez por mês do calendário da simulação**, por transferência real do SIM responsável ao SIM proprietário, sem microgerenciamento do jogador. O calendário de pagamentos mensais é distinto do intervalo de reajuste anual.

**Possível melhoria futura, não aprovada como funcionalidade atual:** ajuste automático adicional do aluguel conforme procura por locação e vacância, mesmo quando o valor de mercado do imóvel não muda. Poderia reagir mais rapidamente ao mercado de locação, mas adiciona volatilidade, complexidade e risco de duplicar o efeito da demanda já refletido na avaliação. Não há compromisso de implementar.

**Em aberto:** percentual de referência, referência inicial para contagem dos 12 meses do reajuste anual, dia do vencimento mensal, tratamento de início/fim de locações no meio do mês e detalhes não aprovados da inadimplência (encargos, execução da saída após 3 meses e outras formas de quitação/extinção). O **princípio de dívida real, seu prazo de 3 meses antes da possível perda da moradia, o efeito dos pagamentos parciais e da quitação integral e sua permanência após a mudança estão decididos**; reajuste **anual a cada 12 meses** também está aprovado. Reavaliações do imóvel entre reajustes não alteram imediatamente o aluguel contratado.


### Dívida de aluguel quando morre o SIM devedor — regra confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável **confirmou explicitamente** a quitação com dinheiro existente no falecimento e o encerramento do restante sem herdar a dívida. Parâmetros correlatos e outros tipos de obrigação continuam pendentes de revisão/decisão.

**Decidido na SPEC:** cobrar aluguel atrasado gradualmente por desconto percentual simples quando um SIM recebe salário, sem dinheiro fictício. A dívida não desaparece pela mudança de moradia. Quando o **SIM devedor morre**, sua dívida de aluguel é paga ao credor real **com o saldo monetário individual disponível no instante da morte**, até o limite devido; o restante não pago é **encerrado sem cobrança automática de familiares ou herdeiros**. Exemplo: dívida R$ 3.000, saldo R$ 800 → o proprietário recebe R$ 800 e os R$ 2.200 restantes deixam de ser exigíveis. Encerrar uma obrigação contábil não cria nem destrói moeda.

**Ordem no fluxo de patrimônio:** abater o aluguel atrasado do dinheiro do falecido **antes** de destinar eventual saldo restante à herança ou, sem herdeiro elegível, à Reserva Global. A regra confirmada não determina venda compulsória de ativos físicos para pagar essa obrigação. **Decidido:** quando o locatário titular morre e há outro adulto na família, a família não perde automaticamente a moradia e o novo titular responde só pelos aluguéis futuros; a dívida do falecido não passa a ele. **Ainda abertos:** como selecionar o adulto substituto, caso sem adulto elegível, múltiplas dívidas e credores e dívidas de outras naturezas. A ocupação temporária sem aluguel durante 3 meses em imóvel sem proprietário, seguida de desocupação se não houver nova condição válida, já foi decidida na SPEC.


### Morte do proprietário credor — herdeiro ou ausência dele

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou a transferência do crédito ao herdeiro, o perdão dos valores atrasados sem herdeiro, a tolerância limitada sem aluguel, a compra do imóvel ocupado para morar ou continuar alugando, e **a desocupação ao fim da tolerância com possibilidade de emigração**. Os dois prazos de 3 meses e o aluguel devido ao novo proprietário durante a transição foram confirmados; detalhes de saída/migração seguem **PENDENTES**.

**Decidido na SPEC:** se o proprietário morrer e um herdeiro assumir o imóvel alugado, esse mesmo herdeiro também passa a ser o credor do aluguel atrasado relativo ao imóvel. O SIM locatário continua devendo o mesmo valor e a cobrança prossegue para o novo credor; não se cria dinheiro nem outra entidade econômica. Isso é distinto da morte do SIM **devedor**, que quita a dívida com o dinheiro disponível e tem o restante perdoado.

**Decidido na SPEC para ausência de herdeiro:** os aluguéis atrasados devidos ao proprietário falecido **são perdoados**, sem transferência do crédito à Reserva Global nem ao futuro comprador. A dívida é extinta no registro, mas nenhum saldo de dinheiro é movimentado/criado/destruído. O imóvel pode ficar sem proprietário, seguindo as regras já existentes de patrimônio não reclamado.

**Decidido na SPEC para ocupantes:** a família que já alugava a casa tem um **período de tolerância de 3 meses do calendário da simulação, contado desde que o imóvel ficou sem proprietário**, no qual pode continuar morando sem pagar aluguel enquanto não houver proprietário e deve buscar outra moradia. **No fim do prazo, se não existir nova condição válida de permanência, desocupa o imóvel.** Se não encontrar outra casa na cidade, **pode emigrar**; não é saída da cidade forçada em 100% dos casos, pois população sem moradia segue como estado possível. A ausência de credor real impede cobrança de aluguel durante a falta de dono e nenhum valor retroativo se acumula. Se aparecer comprador elegível antes disso, ele decide automaticamente entre morar no imóvel ou mantê-lo alugado à família; em ambos os casos, enquanto os atuais moradores continuarem ocupando o imóvel após a aquisição, o aluguel é pago ao proprietário real. Se ele quiser morar, os inquilinos têm 3 meses para sair. A Reserva Global não vira proprietária/credora fictícia.

**Decidido:** quando o comprador quiser morar no imóvel ainda ocupado, o antigo inquilino dispõe de **3 meses contados a partir da compra** para buscar nova moradia e desocupar. Esse prazo é diferente do período de tolerância por ausência de proprietário, que começa quando o imóvel fica sem dono.

**Decidido:** durante o prazo de 3 meses após aquisição para moradia própria, a família ocupante **paga aluguel ao novo dono real** enquanto continuar morando no imóvel, sem retroatividade pelo período anterior sem proprietário. O valor/reajuste segue as regras gerais do aluguel na SPEC; não se introduz reajuste automático por mudança de dono.

**Em aberto:** eventual calibração futura dos 3 meses, critérios concretos da escolha automática entre emigrar e permanecer na cidade sem moradia, e prioridade/elegibilidade de herdeiros. **Risco a evitar:** criar aluguel para a Reserva Global, comprador garantido, emigrar automaticamente todas as famílias ou simular reintegração jurídica complexa. A desocupação ao vencer a tolerância está decidida; a saída após compra para moradia própria tem prazo aprovado de 3 meses, contado da aquisição.
