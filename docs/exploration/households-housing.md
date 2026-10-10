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

## Moradia sem abastecimento, superlotação e fome — nova rodada (2026-10-09)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável confirmou **1A, 2B e 3A**. O desenvolvimento deve seguir a SPEC; calibrações quantitativas e decisões adicionais não foram aprovadas.

- **1A — residência sem água/energia:** compra e ocupação por SIM real continuam possíveis, sem impor ligação plena como bloqueio rígido. Falta real de serviço prejudica saúde, bem-estar e rotinas conforme consequências existentes; não se cobra consumo não realizado ou inventa abastecimento. Diferenciar **habitar** de **ter qualidade de vida**.
- **2B — ocupação acima do conforto:** cada residência tem capacidade confortável, mas **não um limite absoluto que expulse familiares ou impeça nascimento**. Superlotação diminui bem-estar e ajuda a justificar decisão de mudar para imóvel maior, somente quando viável economicamente e com oferta real; sem desenho de cômodos ou gestão de camas pelo jogador.
- **3A — fome pode ser fatal:** consumir de fato o estoque doméstico e refeições pagas continua obrigatório. Privação por período prolongado reduz saúde gradativamente e pode levar à morte, sem inventar fornecimento, punição instantânea, popup para cada família ou nova rotina de monitoramento per-frame.

Riscos de gameplay para medir: cidade recém-fundada sem água/energia ainda pode atrair pessoas, mas a migração e permanência reagem às condições ruins; impacto de fome não pode matar populações inteiras por desequilíbrio de frequência de compras e duração do dia. **Não adicionar política municipal de assistência alimentar por analogia** com assistência social futura ainda não aprovada.

---

## Vida familiar, crédito e débitos sucessórios — rodada de 2026-10-09

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou **2A, 4A e 6A**, mas casos de múltiplos credores e de criança sem parente elegível não foram definidos.

- **2A — sem empréstimos privados inicialmente:** não há bancos/financiamentos pessoais, imobiliários ou empresariais como sistema próprio. Empréstimos do jogador ao Caixa da Cidade continuam válidos conforme regras já existentes.
- **4A — imposto imobiliário e água/energia de SIM falecido:** abater as dívidas municipais **do dinheiro do falecido efetivamente disponível**, depois de respeitar a precedência já definida para aluguel atrasado; encerrar o restante não pago como crédito contábil, sem cobrar herdeiros, vender compulsoriamente imóvel ou atribuir renda fictícia ao município. **Ordem/rateio entre múltiplos credores municipais, se dinheiro insuficiente, permanece lacuna dependente**, não assumir proporcionalidade por analogia com falência de empresas.
- **6A — menor sem adulto responsável:** procurar **parentes reais elegíveis já representados**, sem criar novas pessoas, imóveis, pagamentos, tempo de viagem ou assistência fictícios. O caso sem parente viável permanece **PENDENTE** e requer decisão antes do código correspondente. **O responsável quer assistência social/acolhimento público como expansão futura** — registrar como hipótese desejada, **NÃO aprovada agora**.

Risco a validar: sem crédito privado, imóveis caros podem ficar com poucos compradores; se isso ocorrer no teste integrado, reexaminar financiamento sem criar moeda ou forçar compradores.

---

## Contas municipais: valor automático e dívida sem corte residencial (2B/3B, 2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou tarifas automáticas de água/energia e dívida real sem suspensão individual de residência na SPEC. Forma de cobrança, ordem entre dívidas e titularidade em locações ainda são detalhes abertos.

Contas de água e eletricidade decorrem apenas do uso real e têm preço automático. Se o responsável familiar não puder pagar, a diferença é **dívida com o Caixa da Cidade**, separada do aluguel devido ao proprietário; não contabilizar como receita recebida. Reaproveitar recuperação gradual simples quando o SIM receber dinheiro, sem descontar duas vezes recursos escassos ou gerenciar cada família manualmente. **A inadimplência não gera corte específico da casa no modelo inicial**; isso não protege contra **falha geral de fornecimento por capacidade, infraestrutura ou recursos insuficientes**. O prazo de três meses associado à possível perda da moradia por aluguel **não se aplica automaticamente**. Percentuais, eventos de recuperação e coexistência de obrigações reais ainda devem ser definidos/calibrados de modo simples.

---

## Água e energia como despesas domésticas efetivas — opção 1B (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou pagamento familiar por consumo real de água e eletricidade ao Caixa municipal; este trecho apenas relaciona essa regra à economia familiar, sem definir devedor contratual em casos especiais ou política de inadimplência.

Famílias/SIMs responsáveis têm despesas de água e energia proporcionais ao consumo físico agregado do imóvel, **sem cobrança de recursos inexistentes**. O dinheiro circula entre saldos reais e o **Caixa da Cidade**. Isso é distinto dos **aluguéis ao proprietário** e dos impostos sobre imóveis privados; não somar cobranças duplicadas. A forma exata de cobrar numa residência com locação ou múltiplos moradores, preço e consequências por falta de saldo ainda carecem de especificação/calibração quando relevantes. Não criar cobrança de esgoto/lixo por analogia.

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

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — em 2026-10-08 o responsável aprovou intervenção municipal com indenização automática (B), valor integral de mercado, desocupação assistida e **pagamento ao início da intervenção (A)**; também confirmou o princípio de consequências humanas globais. O detalhamento técnico, casos-limite e análises complementares permanecem parcialmente não revisados.

A propriedade residencial está registrada em SIM real, que pode morar no imóvel ou alugá-lo a outra família. Realocar a **estrutura** e realocar o **domicílio** são operações distintas: mudar localização altera potencialmente valor de mercado, acesso e bem-estar; na locação, proprietário e ocupante têm interesses distintos. **A SPEC agora permite a intervenção municipal mesmo contra a vontade do proprietário, desde que seja indenizado em dinheiro real do Caixa da Cidade.** A família ocupante procura automaticamente moradia compatível seguindo regras existentes, podendo ficar sem moradia ou emigrar; pagar o dono não significa pagar ou garantir casa ao locatário.

A ferramenta de demolição também respeita a indenização. **A indenização foi aprovada como exatamente 100% do valor de mercado do imóvel na localização original, antes da intervenção**, calculado pelo modelo econômico já existente, sem desconto ou acréscimo. A prefeitura transfere esse valor uma única vez do Caixa ao proprietário registrado. O morador que aluga a casa não recebe indenização imobiliária automática nem moradia garantida. **Desocupação assistida foi aprovada em 2026-10-08 (opção C):** em intervenção municipal sobre residência **ocupada**, a família tem uma transição curta, em que busca automaticamente outro imóvel compatível, sem negociação individual do jogador. Se sair antes, a intervenção pode prosseguir; se o período acabar sem nova casa, desocupa mesmo assim e pode ficar sem moradia ou emigrar conforme a SPEC, sem reassentamento ou dinheiro fictício. Enquanto o imóvel original ainda existir e estiver ocupado, seu proprietário permanece registrado e o aluguel mensal devido permanece com ele. **A decisão A foi aprovada em 2026-10-08:** o Caixa paga a indenização integral imediatamente na confirmação da intervenção, para o SIM proprietário poder financiar outra residência já durante a busca. O imóvel permanece temporariamente registrado como **indenizado/retirada pendente**, sem poder ser revendido, reassumido por novos moradores ou indenizado novamente; a locação vigente pode continuar produzindo aluguel enquanto a família original ainda o ocupa. A titularidade temporária permite manter o destinatário real do aluguel sem criar nova transação patrimonial; depois da retirada, o imóvel deixa de existir. Não criar aluguel futuro do imóvel já removido nem perdoar dívidas vencidas pelo deslocamento. **Os dias exatos são calibração;** os prazos de três meses de outras causas de desocupação não são automaticamente reutilizados. O tratamento dos detalhes de liquidação e situações-limite ainda precisa de validação. **Lacuna de liquidez resolvida pela decisão A:** o proprietário-morador recebe o valor real do imóvel no início da transição e pode procurar nova residência sem esperar a demolição. Conferir na validação integrada que pagamento, bloqueio de revenda, aluguel temporário, transferência física e extinção do ativo preservem titularidade, patrimônio não duplicado e oferta monetária fixa. **Ainda não definido:** possibilidade de desfazer intervenção já indenizada e tratamento de exceções de mudança de titular no prazo; não inventar reversão automática nem pagamento repetido. Nenhum preço, moradia ou dinheiro pode ser criado só para eliminar as consequências.

**Preferência residencial aprovada em 2026-10-08 — revisão humana: PARCIALMENTE REVISADO:** o SIM antigo proprietário indenizado tem **primeira oportunidade automática de comprar a casa substituta pronta** pelo **valor de mercado vigente do endereço novo**, sem obrigação de aceitar, preço congelado ou gratuidade. Se não quiser, não for elegível ou não puder pagar, o imóvel passa ao mercado geral. A primeira aquisição vai à Reserva Global. **A SPEC anterior negava preferência, mas foi explicitamente substituída pela nova decisão humana.** O direito é do proprietário antigo, não automaticamente do inquilino; a desocupação segue real e prioridade não garante moradia enquanto a obra acontece.

**Discussão adicional, PENDENTE (2026-10-08):** o responsável considerou o exemplo de proprietário indenizado em R$ 100 mil que avalia casa substituta em R$ 80 mil (R$ 20 mil de saldo liberado), e o caso inverso de nova casa mais cara. A análise da IA aponta que essa diferença **não é ganho monetário mágico**: patrimônio no imóvel menor e dinheiro disponível maior representam composição diferente dos ativos. O novo preço deve refletir localização concreta, mas a avaliação apresentada antes da construção é uma **estimativa**, enquanto a compra efetiva da casa pronta segue o valor de mercado do **momento da transação** já definido na SPEC. A apresentação contextual da estimativa (B) e a prioridade da primeira oferta ao antigo proprietário estão aprovadas; apenas calibração de estimativas e transição entre os imóveis seguem em aberto, mantendo preço efetivo no momento da aquisição.

**Decisão de apresentação aprovada em 2026-10-08 — opção B:** a ferramenta de realocação exibe tendência discreta de valorização/desvalorização enquanto o jogador posiciona a construção, deixando o **valor atual e o valor/diferença estimados do endereço novo com causas concretas** para detalhes opcionais e confirmação. A projeção não fixa preço de compra futura, que permanece o valor de mercado do momento da transação; indenização continua apurada sobre o imóvel original na confirmação. **A preferência de primeira oferta foi aprovada posteriormente em 2026-10-08**, sem fixar preço na prévia nem dispensar compra real.

**Decisão residencial atualizada de 2026-10-08 — venda prioritária ao antigo proprietário seguida de mercado aberto:** após a obra física, o novo imóvel residencial **oferece primeiro a compra ao SIM antigo proprietário indenizado**, com recursos e avaliação econômica reais e **valor de mercado do novo endereço**. Se houver recusa, inelegibilidade ou incapacidade de pagar, o imóvel integra o mercado normal para compradores locais e famílias elegíveis da conexão exterior. A primeira aquisição segue à **Reserva Global**. Não há herança automática de titularidade, locação ou ocupantes, entrega gratuita, comprador garantido ou reserva indefinida. **Esta decisão substitui a anterior de entrada imediata no mercado geral sem prioridade.**

Os cenários de propriedade e as alternativas anteriores estão no [documento de interface](player-interface.md#realocação-de-prédios-prontos-opção-c-e-intervenção-indenizada-b-aprovadas-2026-10-08). **A intervenção com indenização já foi aprovada e consta na SPEC**, sem consentimento obrigatório do dono e sem assentamento fictício. Não inventar **número exato de dias** para a transição curta aprovada, custo adicional nem realocação gratuita: esses detalhes ainda precisam de calibração ou decisão pertinente. **O critério monetário de indenização já está fechado na SPEC** e não é apenas uma referência aproximada.

## Imóvel vazio versus imóvel sem proprietário — esclarecimento de 2026-10-09

> **Revisão humana: PARCIALMENTE REVISADO.** O responsável questionou corretamente a hipótese de cobrar impostos sobre uma casa que ninguém comprou. A distinção de titularidade **já estava decidida na SPEC**; o responsável **aprovou posteriormente a opção 9A**, de **não acrescentar manutenção recorrente específica pela vacância** de um imóvel já comprado, deixando apenas os impostos e outras despesas realmente existentes.

- **Imóvel sem proprietário privado** (por exemplo, residência recém-construída que aguarda a primeira compra): **não há imposto imobiliário privado**, pois falta o SIM pagador; esse caso já está fechado na SPEC.
- **Imóvel que um SIM já comprou, mas está vazio/sem inquilino:** há proprietário real; o imposto imobiliário normal continua devido **independentemente de ocupação ou aluguel recebido**. Não é tributação fictícia de imóvel sem dono.
- **Decisão 9A aprovada (2026-10-09):** no segundo caso, o SIM proprietário continua pagando **imposto imobiliário normal, sem nova taxa recorrente de manutenção ou deterioração por vacância** no primeiro modelo. Não criar obrigações extras fictícias nem dispensar custos reais já previstos por motivos diferentes. Caso de imóvel sem comprador continua sem imposto por ausência de titular privado.

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

> **Revisão humana desta subseção: PARCIALMENTE REVISADO.** O responsável aprovou expressamente contratação **manual (2A)** e, depois, **inadimplência persistente com suspensão de novos empréstimos até regularizar (1A)** em 2026-10-09. Fórmulas financeiras e prazos ainda não revisados.

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
- **Inadimplência 1A (2026-10-09) já aprovada:** dívida vencida continua devida à Reserva Global e **bloqueia novos empréstimos enquanto houver atraso não quitado**; sem refinanciamento automático, confisco compulsório de receitas futuras acima da prioridade essencial nem baixa fictícia de dívida. Visibilidade agregada ao jogador. **Ainda aberto:** vencimentos, método de amortização e parâmetros, sem reabrir o comportamento de bloqueio por inadimplência;
- **Decidido em 2026-10-09 (opção 2A):** empréstimo municipal exige ação e confirmação **manual do jogador**, em vez de contratação automática ou aceite por omissão. Ainda em aberto: apresentação da ferramenta, elegibilidade e parâmetros financeiros concretos.


---

---

---

## Falência pessoal, população sem moradia e demanda imobiliária

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração; existência desses fenômenos e o princípio de prazo com dívida real por aluguel atrasado já estão decididos na SPEC.

### Falência pessoal

**Decidido na SPEC — primeiro modelo:** a falência pessoal é **uma condição econômica**, derivada do dinheiro real insuficiente perante obrigações e dívidas do SIM, com consequências no consumo financiável, na manutenção dos débitos e na possibilidade de manter ou encontrar moradia. Não exige processo jurídico, liquidação patrimonial compulsória própria, renegociação formal nem perdão automático por falir. A dívida continua obedecendo às regras específicas já aprovadas, inclusive quitação/extinção na morte quando cabível. Sem novos saldos monetários fictícios ou burocracia por cidadão. **Em calibração:** critérios quantitativos de entrada/saída dessa condição; não criar subsistema de falência separado sem nova decisão humana.

**Decisões oficiais de aluguel (3 meses e opção 4A aprovada em 2026-10-09):** famílias que deixam de pagar acumulam **dívida real perante o SIM proprietário**. Após **3 meses completos de inadimplência** desde o primeiro aluguel vencido não quitado, **se ainda houver dívida vencida, a locação termina automaticamente**, sem aprovação do proprietário ou tolerância adicional inventada. **Pagamento parcial reduz o débito, mas não reinicia nem suspende os 3 meses; quitação integral zera o atraso e a contagem.** A família procura outra moradia automaticamente conforme o fluxo já decidido; sem opção acessível, pode ficar sem moradia ou emigrar, **sem teletransporte e sem perdão da dívida apenas por sair**. Registrar dívida não movimenta dinheiro; só pagamento real entre SIMs altera saldos. Não há intervenção manual do jogador por cobrança.

**Pontos ainda em exploração:**

- **Decidido (4A, 2026-10-09):** o prazo de inadimplência é de **3 meses** (distinto dos outros prazos de três meses). Com dívida vencida restante no vencimento, a **locação termina automaticamente** e a família deve sair seguindo a procura/realocação física normal; não criar uma decisão individual de despejo pelo proprietário;
- **Decidido:** pagamentos parciais abatem a dívida sem reiniciar ou suspender o prazo de 3 meses. A quitação integral encerra a inadimplência e zera o prazo; um novo atraso abre outra contagem;
- **Fechado pela opção 4A:** gatilho do encerramento automático da locação é o término dos três meses com dívida vencida; sequência física de procura/mudança e comportamento sem alternativa seguem as regras da SPEC e calibração técnica;
- eventual parcelamento formal, prioridade entre contas e encargos por atraso (nenhum juro está aprovado);
- **Decidido:** a dívida de aluguel **permanece após a família deixar o imóvel ou mudar de residência**; não há perdão automático ao sair. O crédito permanece vinculado ao credor real e o pagamento posterior, quando ocorrer, será transferência monetária entre agentes;
- **Decidido:** na morte do SIM devedor, o saldo monetário individual disponível é usado para quitar aluguel atrasado até o limite devido; a parte não paga é encerrada e não passa aos herdeiros/familiares. Eventual dinheiro restante segue o destino patrimonial já definido, inclusive Reserva Global quando não houver herdeiro elegível;
- **Decidido:** cada locação tem um **único SIM titular**, responsável pelo aluguel e pelo débito atrasado; os demais adultos do domicílio não são devedores automáticos. Não criar uma dívida coletiva da família nem divisão proporcional entre adultos;
- **Decidido:** se o SIM titular da locação morrer e a família tiver outro adulto, **a locação e a moradia da família continuam**: um dos adultos remanescentes passa a responder pelos **próximos aluguéis**, não pelas dívidas pessoais antigas do falecido. Estas são liquidadas com o saldo individual disponível no falecimento e o restante é encerrado, conforme a SPEC;
- **Decidido:** quando um herdeiro assume o imóvel alugado após a morte do proprietário, os **aluguéis atrasados associados ao imóvel passam a ser devidos a esse herdeiro**; a dívida permanece, muda apenas o credor real;
- **Decidido:** se o proprietário credor morrer **sem herdeiro elegível**, os aluguéis atrasados que lhe eram devidos são **perdoados**, sem transferência à Reserva Global ou ao futuro comprador; a dívida contábil é extinta sem alterar a oferta monetária;
- **Decidido:** após a morte do proprietário sem herdeiro, a família que já alugava o imóvel tem **3 meses do calendário da simulação, desde que o imóvel fica sem proprietário** para continuar morando sem pagar aluguel. Busca nova moradia durante esse prazo; **ao vencer o prazo, se não houver uma nova locação/propriedade que permita a permanência, deve sair desse imóvel**. Sem alternativa residencial na cidade, **pode emigrar**, sem que a emigração seja obrigatória: o estado de população sem moradia permanece possível. Nenhum aluguel/débito referente ao período sem proprietário é criado ou cobrado retroativamente;
- **Decidido:** o imóvel ainda ocupado pode ser comprado por comprador real elegível, que decide automaticamente se deseja **morar nele ou continuar alugando à família atual**. Na segunda hipótese, ele passa a receber aluguéis futuros; na primeira, a família ocupante tem **3 meses do calendário da simulação, contados da compra**, para procurar outra moradia e sair, sem despejo instantâneo. **Durante esses 3 meses, enquanto a família permanecer no imóvel, o aluguel é pago ao novo proprietário**, sem retroagir ao intervalo em que a casa esteve sem dono. Não assumir reajuste imediato apenas pela aquisição;
- **Em aberto:** escolha do novo locatário titular, ausência de adulto na residência, múltiplos credores, eventual calibração do prazo inicial de 3 meses, critérios da escolha automática entre emigração e condição sem moradia, outras cobranças e limiares da falência pessoal. **Não reabrir o gatilho automático 4A da inadimplência.**
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

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Fluxo econômico decidido; **propriedade de prédio integral ou apartamentos individuais 2C aprovada em 2026-10-09**. Venda de unidades por proprietário integral **aprovada posteriormente como 10A**, sem duplicar patrimônio; representação e valores finos seguem para calibração. Partilha simplificada de herança também já aprovada.

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
- **Herança simplificada APROVADA em 2026-10-09:** classes 3B; dinheiro igual entre elegíveis; casas, apartamentos, prédios e carros atribuídos integralmente por valor aproximado e preferência de ocupação do herdeiro residente. Sem vendas e compensações obrigatórias. Valores e desempates são calibração;
- **Resolvido pela opção 2C em 2026-10-09:** propriedade integral do edifício ou por apartamentos individuais, com vários imóveis por SIM conforme capacidade financeira. **Posteriormente aprovado (10A):** venda de unidades individuais por titular que comprou prédio inteiro, conservando unidades restantes; representação/transação sem duplicidade e custos comuns exigem implementação coerente;
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

## Propriedade integral ou por apartamentos — decisão 2C (2026-10-09)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou expressamente que um prédio de apartamentos possa pertencer inteiramente a um SIM ou ter apartamentos vendidos a SIMs diferentes; qualquer SIM pode comprar quantas casas e unidades conseguir pagar, inclusive para renda de aluguel. Detalhes de transação e avaliação ainda não foram revisados.

**Decidido na SPEC:** o apartamento é patrimônio real de um SIM individual; o prédio inteiro também pode representar a propriedade de um SIM que mantém suas unidades locáveis. **A titularidade é exclusiva**, sem propriedade dupla simultânea de prédio e unidade: comprar um imóvel usa dinheiro real, e rendas/alugueis/impostos seguem o dono concreto, sem dupla cobrança. Não impor limite arbitrário de unidades por investidor nem moradia automática do comprador. **Decidido posteriormente pela opção 10A (2026-10-09):** o titular que comprou o prédio inteiro **PODE vender um ou mais apartamentos separadamente** e conservar os demais, sem venda obrigatória de todo o edifício. A mesma liberdade vale para um SIM que possui duas unidades e deseja vender apenas uma; a transação altera somente a titularidade da unidade vendida e transfere dinheiro real ao vendedor. Um prédio inicialmente integral precisa permitir titularidade por unidade quando começar a vender apartamentos, **sem manter valor/título de prédio inteiro em paralelo com as unidades já distribuídas**. A venda não faz moradores desaparecerem; contratos e transição física seguem regras habitacionais. **Ainda em aberto para viabilizar implementação:** quando o prédio nasce inteiro ou com unidades vendidas, a representação da transição sem titularidade dupla, tributação/avaliação por unidade e áreas comuns sem criar síndico ou condomínio como minijogo.

---

## Herança automática e aproximadamente justa — aprovada em 2026-10-09

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Após questionar o custo de dividir um imóvel indivisível e lembrar a existência de **carros e outros bens**, o responsável aprovou **herança automática, simples e aproximadamente justa**. O princípio está na SPEC; critérios de valor, desempate e implementação são calibração, não um novo sistema jurídico.

**Regra decidida na SPEC:**

- **Mesma classe de herdeiros já aprovada (3B):** primeiro cônjuge/companheiro e filhos; na ausência de ambos, pais e irmãos. Dinheiro disponível **depois de obrigações com tratamento já aprovado** é **dividido igualmente** entre SIMs elegíveis.
- **Uma só lógica para bens físicos:** casas, apartamentos, prédios residenciais inteiros, carros e outros bens individuais são atribuídos **inteiros** a um herdeiro elegível por bem. A simulação **tenta aproximar o valor patrimonial total** recebido por cada um, sem garantir ótimo nem igualdade matemática perfeita; operação por evento da morte.
- **Preferir herdeiro que já mora no imóvel:** quando elegível, tentar preservar sua residência e a continuidade do domicílio, sem transferir titularidade a um não herdeiro por presunção e sem mudança/teletransporte de moradores apenas pela troca patrimonial.
- **Desigualdade restante é aceitável:** se houver um único imóvel e dois herdeiros, um pode ficar com o imóvel, mesmo sem compensar exatamente o outro. **Não** criar venda obrigatória, copropriedade, dívidas de compensação, empréstimos, inventário judicial ou gestão manual para resolver o caso raro.
- **Carros também entram:** o veículo muda de proprietário, mas continua fisicamente onde estava, sujeito às regras de acesso e circulação. Na ausência total de herdeiros, dinheiro e bens físicos seguem a regra de patrimônio sem sucessor da SPEC.
- **Calibração/implementação:** valores estimados dos bens, desempates reprodutíveis, centavos e busca eficiente dos herdeiros elegíveis. Não reabrir divisão exata como requisito sem evidência de problema de gameplay.

**Alternativas históricas descartadas para o escopo atual:** partilha ideal por preço, compra forçada de quotas entre irmãos, venda compulsória de imóvel indivisível, titularidade provisória e inventário detalhado. Eram hipóteses de pesquisa, **não aprovadas**; o responsável preferiu simplicidade pelo impacto baixo na gameplay.

---

## Morte do SIM e destino do patrimônio

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** As classes de herdeiros (3B) e a partilha simplificada de dinheiro e bens físicos foram aprovadas em 2026-10-09; desempates e tratamento de obrigações não reguladas continuam para definição proporcional.

**Status:** elegibilidade e comportamento principal da partilha APROVADOS na SPEC; detalhes de desempate/valores ficam para calibração. **Se não houver nenhum elegível**, aplica-se a regra de patrimônio sem herdeiro já aprovada.

**Herdeiros elegíveis:** primeiro cônjuge/companheiro e filhos; na ausência de ambos, pais ou irmãos. Não exigir família estendida neste modelo. Um SIM solteiro e sem filhos **pode ter herdeiros** se tiver pais ou irmãos vivos/elegíveis; só sem nenhum elegível o dinheiro remanescente retorna à Reserva Global e imóveis podem permanecer sem dono. Com múltiplos elegíveis, dividir dinheiro igualmente e distribuir bens físicos inteiros de modo aproximadamente equilibrado, conforme regra aprovada, sem copropriedade fictícia nem venda obrigatória.

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

- **Aprovados (3B + partilha simplificada, 2026-10-09):** classe primária (cônjuge/companheiro e filhos), depois pais/irmãos na ausência; dinheiro dividido igualmente, bens inteiros distribuídos por aproximação de valor, preferência ao herdeiro que já habita o imóvel. Sem igualdade perfeita, venda compulsória ou dívidas de compensação; valores e desempates técnicos ficam para calibração;
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

---

## Exterior como válvula de escape — análise ainda não aprovada (2026-10-10)

> **Revisão humana desta seção: PENDENTE.** O responsável pediu avaliar a ideia de remeter ao exterior os casos difíceis de resolver no primeiro modelo; não aprovou a regra geral nem a saída automática de menores sem responsáveis.

**Caso concreto:** a SPEC já procura parentes reais para crianças/adolescentes sem adultos no domicílio. Na falta deles, a decisão continua aberta; assistência social municipal própria está adiada.

**Hipótese em discussão:** uma criança sem responsável local poderia sair pela conexão exterior **apenas se houver destino/acolhimento externo definido pelo modelo**, sem inventar parente local, dinheiro, veículo, capacidade infinita ou desaparecimento instantâneo. O evento deveria preservar identidade e histórico já aplicáveis, possuir origem, saída física/temporal coerente e razão observável ao jogador. Não foi decidido que essa infraestrutura externa exista, quem oferece cuidado, seu custo ou se a saída será garantida.

**Crítica à regra universal "tudo difícil vai para fora":** o exterior já ajuda com importação, trabalho pendular, comércio e migração, mas transformá-lo em solução automática de crise (crianças sem responsáveis, moradores sem teto, falta de hospital, lixo, falências) esconderia consequências urbanas, diluiria responsabilidade do jogador e introduziria oferta externa fictícia. Cada integração exterior precisa de condições, capacidade/economia próprias proporcionais ao caso, sem simular uma segunda cidade inteira. **Recomendação de pesquisa, não decisão:** exterior como fallback explícito e limitado, jamais descarte universal de problemas. Comparar saída tutelada para menores com permanência vulnerável ou assistência local apenas quando a solução afetar a implementação.

## Nova análise — acolhimento local sem parentes (2026-10-10)

> **Revisão humana desta seção: PENDENTE.** O responsável não ficou satisfeito com o envio ao exterior como fallback para crianças/adolescentes sem parentes; pediu outra possibilidade. **Não aprovou ainda nenhuma solução complementar** à busca por parentes reais da SPEC.

**Alternativas a considerar:**

- **Famílias acolhedoras reais e sem parentesco:** buscar domicílios de adultos SIMs existentes, com capacidade e disposição para assumir cuidado, moradia, despesas, educação e deslocamentos verdadeiros; decisões automáticas, sem gerenciar cada criança. **Risco:** nenhuma família adequada pode estar disponível, especialmente numa cidade pequena. O sistema não pode criar uma família fictícia; a elegibilidade e custos/consentimento precisam de regra.
- **Acolhimento municipal simplificado:** serviço com vagas finitas, equipe, recursos e custos municipais reais, alocação automática, sem minijogo de adoção. **Risco:** novo serviço, instalação/asset e complexidade econômica; não equivale a aprovar toda uma rede de assistência social.
- **Exterior com destino plausível:** transferência real a serviço externo apenas quando existir alternativa legitimamente disponível, sem desaparecimento automático. **Risco:** fallback infinito oculto; o responsável expressou insatisfação com esta abordagem.
- **Permanência sem acolhimento:** preserva criança real na cidade e efeitos de vulnerabilidade, mas não fornece cuidado adequado. **Risco:** consequências graves e pouca ação significativa do jogador; não recomendar como solução normal.

**Recomendação preliminar (não aprovada):** explorar primeiro **família acolhedora local não aparentada**, sem obrigatoriedade de aceitar e sem criar recursos. Resolver explicitamente o caso limite sem família disponível antes de aprovar essa opção como comportamento completo. Considerar serviço municipal pequeno como último recurso se a simulação local não conseguir garantir cuidado. Não transformar hipótese em SPEC sem escolha expressa.


## Pesquisa comparativa de acolhimento e alternativas extras — 2026-10-10

> **Revisão humana desta seção: PENDENTE.** Pesquisa e recomendações de IA ainda não aprovadas. A SPEC já define busca por parentes reais; a ausência deles continua sem regra oficial. O responsável solicitou novas possibilidades e não fechou solução.

### Evidência externa pertinente

- **UNICEF — Alternative care:** priorizar soluções familiares/seguras, evitar institucionalização como resposta padrão e, quando necessário, oferecer alternativas adequadas à idade e circunstâncias. https://data.unicef.org/topic/child-protection/children-alternative-care/
- **Better Care Network — Continuum of Care:** descreve cuidado por parentes e conhecidos, famílias acolhedoras, pequenos grupos e moradia independente supervisionada para adolescentes em situações adequadas; não tratar todos os menores como caso idêntico. https://bettercarenetwork.org/library/the-continuum-of-care
- **MDS Brasil — Serviços de acolhimento de crianças e adolescentes:** apresenta família acolhedora (com seleção/apoio), casa-lar com cuidador residente e vagas limitadas, abrigo, e oferta regionalizada para municípios menores; **os números/categorias reais são referência, não parâmetros aprovados para o jogo**. https://www.gov.br/mds/pt-br/acoes-e-programas/suas/unidades-de-atendimento/servicos-de-acolhimento-para-criancas-adolescentes-e-jovens
- **Secretaria de Desenvolvimento Social do PR — Família Acolhedora:** famílias precisam ser previamente selecionadas, preparadas e acompanhadas; o acolhimento familiar é provisório e distinto de adoção. https://www.desenvolvimentosocial.pr.gov.br/Pagina/Servico-de-Acolhimento-em-Familia-Acolhedora
- **Better Care Network — Ending Child Institutionalization:** pequenas unidades são exceção preferencialmente temporária, priorizando manutenção de vínculos e integração à comunidade. https://bettercarenetwork.org/library/principles-of-good-care-practices/ending-child-institutionalization

### Modelos candidatos, problemas e custo para o jogo

| Caminho | Fluxo possível sem microgerenciamento | Lacuna principal |
| --- | --- | --- |
| Adultos de confiança conhecidos (amigos/padrinhos/vizinhos com vínculo real) | SIM conhecido e elegível aceita cuidar; criança muda para residência real, preservando relação, necessidades e escola | Não inventar vínculo nem atribuir guarda a qualquer vizinho, falta de adulto elegível |
| Família acolhedora voluntária | Domicílio/SIM real com disposição e capacidade assume acolhimento temporário, sem adoção automática | Critério de seleção, vagas e custo de suporte, possibilidade de zero famílias |
| Casa-lar municipal pequena | Espaço residencial físico, SIM(s) cuidadores em turnos/residência e custos reais, vagas finitas; alocação automática | Exige decisão de incluir serviço público inicial, edifício e orçamento; não ampliar para política social geral |
| Cuidado temporário na casa existente do menor | Profissional SIM real presta cuidados no domicílio real, enquanto se busca solução mais estável | Titularidade de imóvel, deslocamento/turnos e casa nem sempre disponível/segura; custo por criança pode explodir |
| Serviço regional pactuado | Contrato municipal com vagas externas limitadas, dinheiro efetivamente transferido, transporte físico; não apagar eventos do SIM | Como representar capacidade/contraparte e vida posterior fora do mapa sem segunda cidade |
| Moradia apoiada para adolescente elegível | Em casos de maturidade/idade apropriada, moradia física e supervisão/custos reais, sem tutela fictícia | Não atende bebês/crianças; obriga definição de idade e disponibilidade |
| Evitar separação evitável | Se houver adulto responsável elegível e ajuda viável, apoiar continuidade em família real | Não resolve morte de todos os responsáveis; prevenção não substitui último recurso |

**Insight de implementação/produto:** uma **casa-lar simples em edifício residencial visualmente reutilizável** poderia atender poucos menores com cuidador empregado, sem catálogo de assets exclusivos nem tarefa manual por criança. Também poderia funcionar como último recurso se famílias acolhedoras não existirem. **Isto é proposta, não arquitetura nem serviço aprovado.** Checar compatibilidade com a regra atual de não ampliar construções e com a prioridade de salário/custos reais.

**Recomendação exploratória, não decisão:** manter a prioridade aprovada de parentes reais; testar camada intermediária de **adultos de confiança/famílias acolhedoras reais** e fallback de **casa-lar pequena com custo e capacidade físicos**, acionado automaticamente quando houver vagas. Preservar irmãos juntos quando possível e não fingir vagas indisponíveis. O jogador receberia diagnóstico agregado de **menores sem acolhimento / vagas ocupadas / falta de cuidadores**, administrando oferta e orçamento, jamais ordens individualizadas.

**Teste de consistência indispensável antes de aprovar:** cidade pequena sem parentes, sem família acolhedora, sem casa-lar construída, orçamento quebrado, ou duas crianças irmãs e somente uma vaga: nenhuma opção pode registrar acolhimento inexistente. Decidir tratamento temporário realmente possível sem prometer guarda, suprimentos, renda ou moradia gratuitos. O sistema regional é alternativa opcional de estudo, não solução mágica obrigatória. Abrigo institucional grande como padrão é menos alinhado às referências.

**Status:** decisão 7 continua aberta. As modalidades e capacidades acima são pesquisa de referência, não implementação autorizada.
