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


**Status:** parcialmente decidido. O saldo monetário individual de todo SIM já é canônico; regras de despesas e decisões financeiras familiares continuam em exploração.

### Renda individual e familiar

Direção atual:

- **todo SIM possui saldo monetário individual real**, conforme a SPEC; para crianças esse saldo pode ser zero ou receber apenas fluxos explicitamente definidos;
- a família/domicílio pode ter métricas agregadas de renda e despesas compartilhadas sem substituir os saldos individuais;
- ainda precisa ser decidido como despesas comuns, dependentes e decisões financeiras familiares afetam os saldos individuais.

A leitura familiar pode calcular **renda familiar disponível** e despesas comuns do domicílio como agregados derivados. Isso preserva individualidade sem exigir microgerenciamento bancário.

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

---

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

**Decisão confirmada na SPEC para atraso de aluguel:** famílias podem deixar de pagar por um prazo, acumulando **dívida real perante o SIM proprietário**, antes da possibilidade de perder a moradia. Registrar valor devido não cria dinheiro: só o pagamento efetivo entre agentes altera saldos. Não há necessidade de intervenção do jogador em cada cobrança.

**Pontos ainda em exploração:**

- duração do prazo e gatilhos para eventual saída/despejo;
- parcelamento/pagamento parcial, prioridade de contas e eventual encargo por atraso (nenhum juro está aprovado);
- **Decidido:** a dívida de aluguel **permanece após a família deixar o imóvel ou mudar de residência**; não há perdão automático ao sair. O crédito permanece vinculado ao credor real e o pagamento posterior, quando ocorrer, será transferência monetária entre agentes;
- **Decidido:** na morte do SIM devedor, o saldo monetário individual disponível é usado para quitar aluguel atrasado até o limite devido; a parte não paga é encerrada e não passa aos herdeiros/familiares. Eventual dinheiro restante segue o destino patrimonial já definido, inclusive Reserva Global quando não houver herdeiro elegível;
- **Decidido:** cada locação tem um **único SIM titular**, responsável pelo aluguel e pelo débito atrasado; os demais adultos do domicílio não são devedores automáticos. Não criar uma dívida coletiva da família nem divisão proporcional entre adultos;
- **Decidido:** se o SIM titular da locação morrer e a família tiver outro adulto, **a locação e a moradia da família continuam**: um dos adultos remanescentes passa a responder pelos **próximos aluguéis**, não pelas dívidas pessoais antigas do falecido. Estas são liquidadas com o saldo individual disponível no falecimento e o restante é encerrado, conforme a SPEC;
- **Decidido:** quando um herdeiro assume o imóvel alugado após a morte do proprietário, os **aluguéis atrasados associados ao imóvel passam a ser devidos a esse herdeiro**; a dívida permanece, muda apenas o credor real;
- **Decidido:** se o proprietário credor morrer **sem herdeiro elegível**, os aluguéis atrasados que lhe eram devidos são **perdoados**, sem transferência à Reserva Global ou ao futuro comprador; a dívida contábil é extinta sem alterar a oferta monetária;
- **Decidido:** após a morte do proprietário sem herdeiro, a família que já alugava o imóvel tem **um prazo de tolerância limitado em meses (valor em aberto)** para continuar morando sem pagar aluguel. Busca nova moradia durante esse prazo; **ao vencer o prazo, se não houver uma nova locação/propriedade que permita a permanência, deve sair desse imóvel**. Sem alternativa residencial na cidade, **pode emigrar**, sem que a emigração seja obrigatória: o estado de população sem moradia permanece possível. Nenhum aluguel/débito referente ao período sem proprietário é criado ou cobrado retroativamente;
- **Decidido:** o imóvel ainda ocupado pode ser comprado por comprador real elegível, que decide automaticamente se deseja **morar nele ou continuar alugando à família atual**. Na segunda hipótese, ele passa a receber aluguéis futuros; na primeira, a família ocupante deve sair com transição/realocação a definir, sem despejo instantâneo;
- **Em aberto:** escolha do novo locatário titular, ausência de adulto na residência, múltiplos credores, duração numérica da tolerância, decisão sistêmica entre emigração e condição sem moradia, regras de transição/saída quando um comprador quiser morar, outras cobranças e falência pessoal;
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

**Alerta de complexidade interna — revisão humana desta nota: PARCIALMENTE REVISADO.** O responsável reforçou que microgerenciamento não significa apenas cliques do jogador: uma rotina invisível de cobrança com múltiplas exceções, prioridades de despesas, contratos e renegociações pode ser complexa demais para manter e simular em larga escala. **Direção escolhida:** descontar um percentual simples da dívida de aluguel **apenas quando o SIM responsável recebe salário**, com limite no valor devido. Existe **um único SIM titular por locação**, sem dívida compartilhada automaticamente por todos os adultos; quando ele morre e existe outro adulto na família, este assume somente a locação e os pagamentos futuros. A porcentagem e os critérios para escolher qual adulto assume continuam abertos. Evitar estados/checagens constantes e prioridades complexas de contas. A morte do **SIM devedor** já tem regra explícita na SPEC; quando o **SIM credor** morre com herdeiro que assume o imóvel, esse herdeiro também assume o crédito do aluguel atrasado. Sem herdeiro elegível, a dívida de aluguel antigo do credor falecido é encerrada; a família permanece sem aluguel durante uma **tolerância temporária em meses ainda a definir**. A compra por novo proprietário permite continuidade do aluguel ou moradia própria do comprador; sem dono, ninguém pode cobrar aluguel. Ao vencer o prazo sem nova locação/propriedade, os ocupantes devem sair do imóvel e podem emigrar se não encontrarem outra casa; não se exige emigração em todos os casos (SPEC).

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
- percentual de aluguel, periodicidade de cobrança e parâmetros do cálculo do valor de mercado (preço de venda já definido como igual à avaliação);
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

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou aluguel proporcional ao valor de mercado e **reajuste periódico** dos contratos, além de apontar ajuste adicional pela procura apenas como possível melhoria. Detalhes de calibração e consequências elaborados pela IA permanecem **PENDENTES** de revisão.

**Decisão oficial (ver SPEC):** no primeiro modelo, cada locação possui **um único SIM responsável pelo aluguel e pela dívida**, sem atribuição automática aos demais adultos. O aluguel residencial é **valor de mercado do imóvel × percentual de aluguel** (a calibrar). O valor de mercado já considera condições da cidade, incluindo demanda; não existe um segundo fator de procura aplicado diretamente ao aluguel. **Contratos em andamento só têm o valor do aluguel recalculado em intervalos periódicos**, e não a cada mudança de avaliação do imóvel. O pagamento continua sendo transferência real do locatário ao SIM proprietário, sem microgerenciamento do jogador.

**Possível melhoria futura, não aprovada como funcionalidade atual:** ajuste automático adicional do aluguel conforme procura por locação e vacância, mesmo quando o valor de mercado do imóvel não muda. Poderia reagir mais rapidamente ao mercado de locação, mas adiciona volatilidade, complexidade e risco de duplicar o efeito da demanda já refletido na avaliação. Não há compromisso de implementar.

**Em aberto:** percentual de referência, duração do intervalo de reajuste, periodicidade de pagamento/cobrança, tratamento do início de novas locações e parâmetros da inadimplência (prazo, pagamento parcial, encargos, despejo, responsáveis e eventual quitação/extinção). O **princípio de dívida real com prazo antes da possível perda da moradia e sua permanência após a mudança estão decididos**; reajuste periódico também, mas sua duração exata não foi aprovada. Reavaliações do imóvel entre reajustes não alteram imediatamente o aluguel contratado.


### Dívida de aluguel quando morre o SIM devedor — regra confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável **confirmou explicitamente** a quitação com dinheiro existente no falecimento e o encerramento do restante sem herdar a dívida. Parâmetros correlatos e outros tipos de obrigação continuam pendentes de revisão/decisão.

**Decidido na SPEC:** cobrar aluguel atrasado gradualmente por desconto percentual simples quando um SIM recebe salário, sem dinheiro fictício. A dívida não desaparece pela mudança de moradia. Quando o **SIM devedor morre**, sua dívida de aluguel é paga ao credor real **com o saldo monetário individual disponível no instante da morte**, até o limite devido; o restante não pago é **encerrado sem cobrança automática de familiares ou herdeiros**. Exemplo: dívida R$ 3.000, saldo R$ 800 → o proprietário recebe R$ 800 e os R$ 2.200 restantes deixam de ser exigíveis. Encerrar uma obrigação contábil não cria nem destrói moeda.

**Ordem no fluxo de patrimônio:** abater o aluguel atrasado do dinheiro do falecido **antes** de destinar eventual saldo restante à herança ou, sem herdeiro elegível, à Reserva Global. A regra confirmada não determina venda compulsória de ativos físicos para pagar essa obrigação. **Decidido:** quando o locatário titular morre e há outro adulto na família, a família não perde automaticamente a moradia e o novo titular responde só pelos aluguéis futuros; a dívida do falecido não passa a ele. **Ainda abertos:** como selecionar o adulto substituto, caso sem adulto elegível, múltiplas dívidas e credores, ocupação e cobrança de aluguéis novos em imóvel sem proprietário, e dívidas de outras naturezas.


### Morte do proprietário credor — herdeiro ou ausência dele

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou a transferência do crédito ao herdeiro, o perdão dos valores atrasados sem herdeiro, a tolerância limitada sem aluguel, a compra do imóvel ocupado para morar ou continuar alugando, e **a desocupação ao fim da tolerância com possibilidade de emigração**. Duração exata e mecanismo da saída/migração seguem **PENDENTES**.

**Decidido na SPEC:** se o proprietário morrer e um herdeiro assumir o imóvel alugado, esse mesmo herdeiro também passa a ser o credor do aluguel atrasado relativo ao imóvel. O SIM locatário continua devendo o mesmo valor e a cobrança prossegue para o novo credor; não se cria dinheiro nem outra entidade econômica. Isso é distinto da morte do SIM **devedor**, que quita a dívida com o dinheiro disponível e tem o restante perdoado.

**Decidido na SPEC para ausência de herdeiro:** os aluguéis atrasados devidos ao proprietário falecido **são perdoados**, sem transferência do crédito à Reserva Global nem ao futuro comprador. A dívida é extinta no registro, mas nenhum saldo de dinheiro é movimentado/criado/destruído. O imóvel pode ficar sem proprietário, seguindo as regras já existentes de patrimônio não reclamado.

**Decidido na SPEC para ocupantes:** a família que já alugava a casa tem um **período de tolerância limitado, em meses a definir**, no qual pode continuar morando sem pagar aluguel enquanto não houver proprietário e deve buscar outra moradia. **No fim do prazo, se não existir nova condição válida de permanência, desocupa o imóvel.** Se não encontrar outra casa na cidade, **pode emigrar**; não é saída da cidade forçada em 100% dos casos, pois população sem moradia segue como estado possível. A ausência de credor real impede cobrança de aluguel durante a falta de dono e nenhum valor retroativo se acumula. Se aparecer comprador elegível antes disso, este decide automaticamente entre morar no imóvel ou mantê-lo alugado à família; só quando locação continuar passam a ser pagos novos aluguéis a ele. A Reserva Global não vira proprietária/credora fictícia.

**Em aberto:** duração numérica do período de tolerância, critério de escolha entre emigrar e permanecer na cidade sem moradia, transição para realocação quando comprador quiser morar, e prioridade/elegibilidade de herdeiros. **Risco a evitar:** criar aluguel para a Reserva Global, comprador garantido, emigrar automaticamente todas as famílias ou simular reintegração jurídica complexa. A desocupação ao vencer a tolerância está decidida; a saída após compra para moradia própria também está decidida em princípio, com transição ainda a definir.
