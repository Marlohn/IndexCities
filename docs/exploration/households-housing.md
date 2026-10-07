# IndexCities — Famílias, moradia e patrimônio

> **Status:** exploração ativa; parte do modelo de propriedade e investimento já foi promovida para a SPEC — não é fonte de verdade.
>
> Reúne renda e consumo familiar, valor imobiliário, vulnerabilidade financeira, aluguel, propriedade, herança e investimento residencial por cidadãos.

## Como ler este documento

Este arquivo é material de exploração temática. A autoridade do produto continua sendo `docs/SPEC.md`. Trechos históricos podem preservar alternativas já superadas; quando houver divergência, vale a decisão canônica mais recente.

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

---

## Clarificação: para onde vai o aluguel

**Status:** parcialmente superado. O destino do aluguel foi posteriormente decidido e promovido para a SPEC: o pagamento vai ao proprietário real do imóvel. Este trecho preserva o raciocínio anterior e as questões que ainda permaneceram abertas naquele momento.

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

**Status:** decisão parcial promovida para a SPEC; regras de herdeiro elegível e usos futuros do fundo continuam abertas.

### Decisão atual

Quando um SIM morre **sem herdeiro elegível**:

- o dinheiro remanescente vai para um **Fundo de Patrimônio Não Reclamado**;
- o fundo é um ledger financeiro separado e rastreável;
- ele não faz parte do Caixa da Cidade, das carteiras dos SIMs ou dos caixas das empresas;
- o jogador não pode gastar esse saldo;
- imóveis e outros ativos podem ficar explicitamente **sem proprietário / não reclamados**;
- o fundo não se torna proprietário desses ativos;
- se um imóvel não reclamado for vendido, o pagamento entra no fundo;
- o uso futuro do saldo fica aberto;
- o fundo não representa nem controla a conexão exterior.

### Por que não fazer o fundo possuir os imóveis

**Prós de deixar o ativo sem dono**
- mantém o fundo apenas como custódia financeira;
- evita transformar o fundo em imobiliária;
- evita perguntas sobre aluguel, manutenção, impostos e operação pelo fundo;
- cria um estado genérico reutilizável de ativo sem proprietário.

**Contra**
- sistemas de propriedade precisam aceitar `owner = none` de forma explícita.

Esse custo foi considerado menor que criar um proprietário artificial.

### Alternativas descartadas para o primeiro modelo

- **Espólio individual por falecido:** completo, mas cria entidade e processo jurídico sem gameplay suficiente.
- **Beneficiário social intermediário:** apenas adiciona outro possível recebedor antes do caso realmente sem sucessor.
- **Tudo para o Caixa da Cidade:** transforma mortes em receita direta do jogador.
- **Dinheiro desaparece:** quebra rastreabilidade e causalidade.
- **Fundo como controlador da conexão exterior:** mistura responsabilidades sem relação direta e cria risco de virar um agente econômico mágico.

### Pesquisa de referência

A pesquisa externa mostrou que sistemas reais frequentemente tratam patrimônio sem herdeiro por mecanismos públicos ou de patrimônio não reclamado. Isso serviu como referência para procurar uma solução rastreável, mas o IndexCities deliberadamente não replica processo jurídico detalhado.

As referências e alternativas pesquisadas permanecem material de contexto; a regra canônica atual está na SPEC.

### Pontos ainda abertos

- quem conta como herdeiro elegível e em qual prioridade;
- se obrigações já existentes na simulação são liquidadas antes da transferência ao fundo;
- se o fundo algum dia terá outra função de gameplay;
- tratamento de outros tipos de ativos além de dinheiro e imóveis quando eles forem introduzidos.

