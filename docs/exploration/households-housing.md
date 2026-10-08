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

Já está decidido na SPEC que a **tributação de imóveis residenciais, comerciais e industriais incide sobre o valor de mercado**, com alíquota da categoria escolhida pelo jogador. Também foi confirmado que **o valor dos imóveis é dinâmico e estimado pelas condições concretas da cidade**: localização, acesso a serviços, poluição, demanda e características do imóvel podem fazer o valor subir ou descer. **A estimativa funciona desde o primeiro imóvel, sem exigir vendas anteriores como referência.** Esse foi o modelo escolhido em vez de basear o valor principalmente nas negociações passadas. O modelo deve explicar as causas da variação ao jogador e não criar/destruir moeda: reavaliação patrimonial não é movimentação financeira. Ainda não foram decididos fórmula, pesos, frequência de atualização, diferenciação precisa entre tipos de imóvel ou a relação entre valor estimado e preço final negociado; tamanho pode ser um fator de avaliação, mas não é a base direta do imposto.

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
- fórmula de preço e aluguel;
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
- se obrigações já existentes são liquidadas antes da transferência do saldo;
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

O ponto ainda aberto é a formação do preço e a seleção automática do comprador, não o destino do pagamento.
