# Prefeitura e finanças públicas

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Histórico das decisões em [docs/decisoes/prefeitura-financas.md](../decisoes/prefeitura-financas.md).

## Direção de revisão — preferência do responsável (2026-10-11)

**Preferência expressa do responsável:** ampliar a **agência de decisão agregada do jogador** sobre a administração e a política municipal no Convencional, inclusive **reavaliando decisões anteriores**, em vez de confiar automaticamente à simulação escolhas de financiamento, cobrança, prioridade e acesso que poderiam gerar gameplay interessante.

- Ao revisar educação (creches, escolas/universidades), saúde, transporte, estacionamento, atrações, assistência, água/energia, previdência e demais serviços municipais, **não tratar gratuidade, tarifas automáticas, benefícios e escolhas antigas como imunes a reavaliação**. Identificar opções concretas para o jogador e seus efeitos reais sobre saldo municipal, utilização, capacidade, renda, saúde, acesso a trabalho e rotina familiar.
- **Reavaliação não revoga requisitos:** a SPEC vigente continua valendo enquanto o responsável não escolher expressamente alternativa substituta. Documentar candidatos e análises pendentes na exploração; só depois da aprovação promover mudança à SPEC e corrigir contradições.
- Distinguir **política estratégica agregada** (gratuito/pago/subsidiado, prioridade, nível de financiamento quando realmente significativo), **ferramenta de gestão** (construir, ajustar capacidade, empréstimos, orçamento) e **automação operacional necessária** (pagar contas devidas, cobrar tarifas aprovadas, transportar material, despachar equipe, decidir autonomamente cada SIM/empresa). Mais controle não significa microgerenciar pessoas, rotas e transações.
- Confrontar cada proposta com o valor de gameplay: **quem decide, qual é o exemplo concreto, o que acontece se escolher A ou B, quem ganha/perde, qual despesa/receita real muda, que risco de frustração para criança construtora e que custo de simulação adicional existem?** Se opções só diferem de nome, descartá-las.
- **Perguntas ao responsável precisam ser autoexplicativas.** Começar com um caso real simples (por exemplo: escola gratuita custeada pelo Caixa versus escola paga que pode excluir famílias sem saldo); formular a diferença das alternativas A/B/C sem abstrações e sem presumir familiaridade com conceitos técnicos; indicar recomendação e desvantagem claramente. Não rotular como “decidido” uma resposta expressamente condicionada a dúvida, incompreensão ou revisão de valor (“não entendi”, “e daí?”); mantenha-a em exploração até confirmação inequívoca.
- Não usar benchmarking de outros jogos como autorização automática para copiar políticas, subsídios, multas, programas ou burocracia. Preservar dinheiro finito, fluxos físicos, autonomia de SIMs e empresas e diagnóstico causal.

## Em vigor no jogo (V1)

### Moradia e finanças públicas

#### Moradia sem água ou energia suficiente

- Imóveis residenciais podem ser comprados e ocupados por SIMs mesmo quando água e/ou eletricidade não atendem plenamente a residência, sem bloqueio artificial de compra/mudança por falta de abastecimento.

- A falta ou insuficiência efetiva dos serviços pode prejudicar saúde, bem-estar e funcionamento cotidiano conforme os sistemas já previstos, de modo gradual e diagnosticável, sem criar atendimento gratuito, consumo faturado inexistente nem impedir a família de decidir mudar.

- Intensidade e prazos são calibração.

#### Superlotação residencial permitida com consequências

- Cada casa/apartamento possui uma capacidade confortável de moradores coerente com seu porte; a família pode excedê-la sem despejo automático, proibição de nascimento ou obrigação de mudar.

- A superlotação prejudica a qualidade de vida e pode influenciar a busca autônoma por moradia maior quando disponível e financeiramente viável.

- Mostrar ocupantes e insuficiência de espaço sem simular cômodos individualmente; limites e efeitos concretos são calibração.

- Famílias podem se mudar de residência conforme fatores como renda, tamanho da família e localização.

- Residências têm preço e/ou aluguel real que afetam o orçamento familiar.

- Cidadãos e empresas pagam impostos reais para a cidade.

- O sistema tributário é organizado por categorias, diferenciando tipos de incidência para permitir escolhas econômicas legíveis, sem exigir administração individual de cada contribuinte.

- A tributação das construções é organizada por uso residencial, comercial e industrial, com controles separados para baixa e alta densidade; essa organização serve para oferecer controles agregados ao jogador, sem cobrança manual por prédio.

- O jogador ajusta os impostos no nível dessas categorias.

- São seis controles independentes: residencial de baixa densidade, residencial de alta densidade, comercial de baixa densidade, comercial de alta densidade, industrial de baixa densidade e industrial de alta densidade.

- As escolhas tributárias devem produzir consequências econômicas e influenciar a atratividade dos agentes/atividades afetados, com causas observáveis, sem criar microgerenciamento individual.

#### Fazendas na tributação industrial de baixa densidade

- No modelo inicial, os imóveis rurais produtivos/fazendas sujeitos ao imposto imobiliário usam a alíquota industrial de baixa densidade já controlada pelo jogador, aplicada ao valor de mercado e com proprietário/devedor real como nas outras propriedades econômicas.

- Não criar sétimo controle tributário ou isenção rural por esta escolha; eventual categoria rural própria é melhoria futura não aprovada.

- O imposto de uma construção privada só começa a ser cobrado quando ela tem proprietário privado real.

- Residências usam o saldo do SIM proprietário; estabelecimentos comerciais e industriais usam o caixa da empresa proprietária.

- Construção ainda sem proprietário privado não gera esse imposto, e não se cria um pagador fictício.

#### Inadimplência de imposto imobiliário

- Quando o SIM proprietário residencial ou a empresa proprietária comercial/industrial não tiver dinheiro real suficiente para quitar o imposto devido por imóvel privado, a parte não paga se torna dívida monetária verdadeira do proprietário com o Caixa da Cidade, acompanhada por imóvel e devedor sem duplicação de créditos.

- O pagamento efetivamente realizado, mesmo parcial, reduz a dívida e só então aumenta a receita municipal; imposto lançado, mas não pago, não é arrecadação nem autoriza criar moeda.

- A simulação procura recuperar gradualmente o valor em atraso quando o devedor receber ou dispuser de dinheiro, respeitando a capacidade financeira real, a prioridade definida de necessidades essenciais do domicílio e outras obrigações, sem exigir cobrança manual por proprietário ou prédio.

- Nas empresas, a dívida integra suas obrigações reais e, em falência terminal, participa da liquidação proporcional definida.

- Não aplicar automaticamente os três meses da inadimplência do aluguel, nem impor despejo, apreensão, venda forçada do imóvel, transferência de titularidade, multas/juros ou anistia fictícia por atraso de imposto; esses eventos não decorrem da inadimplência de imposto imobiliário.

- Para SIM falecido, não inventar uma regra de cobrança contra herdeiros nem quitação compulsória de ativos físicos: o tratamento sucessório de outras dívidas pessoais continua dependente de definição própria quando necessário.

- Cadência de lançamento, pagamentos, recuperação, coexistência de credores e arredondamentos são calibração/definição técnica proporcional, sem mudar os efeitos de produto aqui aprovados.

- O imposto de cada imóvel privado é calculado sobre seu valor de mercado: imposto = valor de mercado do imóvel × alíquota definida pelo jogador para sua categoria (residencial/comercial/industrial × baixa/alta densidade).

- **Em aberto:** O tamanho não é uma base de cobrança direta; pode influenciar o valor de mercado, conforme o modelo de avaliação ainda a definir.

- O valor de mercado de cada imóvel é dinâmico e calculado pelas condições concretas e observáveis da cidade, como localização, acesso a serviços, poluição e demanda, considerando as características do imóvel.

- Essa avaliação deve funcionar desde o primeiro imóvel, sem depender de histórico de compras e vendas de imóveis semelhantes.

- A valorização/desvalorização deve ter causas diagnosticáveis para o jogador e servir à avaliação do imóvel e à base de incidência tributária já definida; nenhuma alteração de valor cria ou destrói dinheiro.

- No primeiro modelo, o preço de compra e venda de imóveis é exatamente seu valor de mercado calculado no momento da transação, tanto na primeira aquisição como nas revendas, sem negociação automática de descontos ou acréscimos.

- A procura continua determinada pela capacidade financeira e pela decisão dos compradores; ela não garante venda.

#### Imóvel residencial com proprietário, mas desocupado — sem manutenção imobiliária adicional inicialmente

- Se o SIM possui casa ou apartamento e ainda não mora ou não encontrou inquilino, continua responsável pelo imposto imobiliário normal, calculado pelo valor de mercado.

- Não cobrar despesa periódica adicional de manutenção simplesmente por estar vazio nem impor deterioração automática decorrente da vacância no primeiro modelo.

- Isso não isenta o imóvel dos custos reais já definidos que efetivamente ocorrerem por outros motivos nem inventa pagamentos sem contraparte real.

- Imóvel que ainda não tem proprietário privado não gera imposto imobiliário, conforme a regra definida de titularidade; não confundir propriedade privada desocupada com construção ainda não comprada.

- A eventual manutenção específica de patrimônio privado e deterioração por abandono podem ser reavaliadas no futuro se demonstrarem valor para gameplay, sem aprovação vigente.

#### Pacote tributário da V1

- As seis alíquotas são anuais, começam em 1% ao ano e podem ser ajustadas pelo jogador entre 0% e 8% ao ano.

- A cobrança é mensal e proporcional: imposto mensal = valor de mercado atual do imóvel × alíquota anual da categoria ÷ 12.

- Permanecem em calibração a fórmula de avaliação, sua atualização e os pesos dos efeitos sobre demanda/atratividade, preservando as causas econômicas reais e a dívida por valores devidos não pagos.

- A cidade possui orçamento municipal real, com receitas e despesas.

#### Crise fiscal municipal sem fim automático de partida

- Déficit, inadimplência e falta persistente de recursos da prefeitura não encerram o save nem provocam derrota obrigatória.

- O jogador pode continuar tentando recuperar a cidade, sujeito às consequências financeiras e operacionais reais definidas (serviços reduzidos/paralisados, dívidas, falta de capacidade e impactos sobre a população), sem resgate monetário, manutenção gratuita ou perdão de dívida por esse motivo.

- A gravidade e as causas da crise devem ser legíveis de modo agregado, sem transformar a recuperação em microgerenciamento de cada despesa.

- O menu financeiro deve separar claramente entradas e saídas da prefeitura.

- Salários de todos os trabalhadores públicos devem aparecer explicitamente entre as despesas municipais.

- A cidade pode usar empréstimos/dívida municipal para evitar travamentos financeiros e permitir recuperação de caixa.

- O principal de um empréstimo sai da Reserva Global e entra no Caixa da Cidade; amortizações e juros retornam à Reserva Global.

- **Pacote de empréstimo da V1 aprovado pelo responsável em 2026-10-12:** limite de 100.000 de principal municipal em aberto, prazo de 12 meses e juros de 0,3% ao mês sobre o principal restante.

- A distribuição das amortizações dentro do prazo permanece em calibração.

- A contratação continua manual, depende de saldo real na Reserva e fica indisponível enquanto houver obrigação vencida não quitada.

- O empréstimo redistribui dinheiro existente e não cria moeda nova.

#### Inadimplência de empréstimo municipal

- Se a cidade não conseguir pagar parcela/obrigação de crédito já contratado no vencimento, a quantia não paga permanece como dívida real com a Reserva Global, sem criar pagamento fictício, perdão, cancelamento de moeda ou contratação automática de outro empréstimo.

- Enquanto houver obrigação vencida e não quitada, o jogador não pode contratar novos empréstimos municipais; o acesso pode ser recuperado quando regularizar o atraso usando caixa real.

- A insuficiência e o crédito indisponível são apresentados no painel financeiro em nível agregado.

- Não forçar captura automática de todas as receitas futuras com precedência sobre despesas essenciais, não impor renegociação automática, novos juros/multas nem bloqueio definitivo de crédito por inferência; vencimentos, pagamentos efetivos e critérios de recuperação obedecem às obrigações e à priorização financeira definidas, com parâmetros técnicos para calibração.

- O jogador segue responsável pelo crédito municipal, sem administração de cada lançamento individual.

#### Contratação manual de empréstimo municipal

- Somente o jogador decide solicitar e confirmar um empréstimo nas ferramentas de finanças municipais, com informação compreensível de valor, dívida e condições efetivas.

- A simulação não contrata empréstimos automaticamente ao faltar dinheiro, não oferece financiamento aceito por omissão e não injeta recursos sem a transferência real da Reserva Global.

- A contratação deve respeitar saldo existente na Reserva, regras de elegibilidade e limites a calibrar; não assumir crédito disponível infinito, juros, perdão, inadimplência ou sanção específica sem decisão posterior.

- Falta de Caixa continua afetando obras e operação pelas regras financeiras existentes, sem gestão de cada pagamento individual.

#### Priorização automática de manutenção essencial em déficit

- Se o dinheiro ou os recursos efetivamente disponíveis não forem suficientes para manter toda a infraestrutura municipal, a simulação prioriza automaticamente as despesas e atividades viáveis necessárias à continuidade de serviços urbanos essenciais e de suas dependências críticas, em vez de exigir que o jogador autorize reparo ou verba individual por construção.

- A priorização considera consequências reais para moradores e operação da cidade (por exemplo, água, energia, saúde e emergência, segundo as dependências e capacidades existentes) e não é garantia de funcionamento sem equipe, insumos ou pagamento: se não houver recursos para tudo, até um serviço essencial pode perder capacidade ou parar conforme a falta real.

- Serviços menos críticos podem ser reduzidos antes quando isso de fato preservar operações essenciais, sem criar despesa paga, dívida automática, dinheiro novo, manutenção realizada ou funcionamento fictício.

- O jogador deve enxergar, em resumo, a insuficiência, o que foi preservado, o que perdeu capacidade e por quê, podendo agir nas decisões financeiras e urbanas agregadas definidas; não administrar prioridades por edifício.

- A lista/classificação detalhada das dependências críticas, o critério de desempate e o momento de redução e retomada são detalhes para definição/calibração antes do comportamento dependente ser codificado, sem ordem fixa universal inventada agora.

#### Consequência da falta de orçamento para manutenção

- Não pagar uma despesa de manutenção não equivale a tê-la realizado.

- Se a insuficiência do Caixa impedir manter trabalhadores, adquirir insumos ou contratar os serviços reais necessários ao funcionamento da infraestrutura, a capacidade/eficiência dos sistemas afetados pode deteriorar-se e, se a insuficiência persistir, o serviço correspondente pode ser interrompido, em consequência da falta efetiva de recursos/atividade, sem desligamento instantâneo e indiscriminado da cidade.

- A evolução do problema e sua causa devem ser visíveis ao jogador, preferencialmente em nível agregado, e a retomada depende de pagamentos, pessoas e insumos reais disponíveis.

- Não criar falhas aleatórias, desgaste individual obrigatório, pagamentos a receptores fictícios, dívida automática sem contratação ou reembolso mágico de recursos.

- **Em aberto:** Prioridades entre serviços, atraso tolerável e dinâmica de recuperação ainda precisam de calibração/decisões proporcionais antes do código correspondente.

#### Custo recorrente de manutenção/funcionamento da infraestrutura municipal

- Além da construção, manter ruas, redes e demais infraestruturas municipais exige despesas recorrentes reais, apresentadas ao jogador de forma agregada por categoria e proporcionais à infraestrutura/capacidade efetivamente existente, para que a expansão tenha compromisso financeiro duradouro.

- O Caixa da Cidade paga salários já existentes, recursos e serviços de manutenção efetivamente devidos aos agentes locais corretos, ou a contrapartes externas quando realmente prestarem serviço conforme os fluxos com a Reserva Global; nenhuma quantia desaparece nem se cria um recebedor fictício só para drenar caixa.

- Evitar cobrança duplicada de equipe pública, operação de serviços ou outras despesas já contabilizadas; fórmula e periodicidade ficam para calibração baseada em sistema real e preservam equilíbrio monetário.

- Essa decisão não aprova desgaste físico individual, quebra periódica fictícia, minigame de reparação nem clicar para consertar cada rua/tubo.

- A arrecadação tributária já definida entra no Caixa e ajuda a pagar a manutenção, mas não aumenta automaticamente para igualar despesas; se a cidade construir e operar acima da receita, a insuficiência de caixa e eventuais empréstimos seguem as regras financeiras existentes e são legíveis no orçamento.

- **Em aberto:** Detalhes de prestadores/custo variável e consequências específicas de inadimplência de manutenção exigem definição antes de implementação dependente.

#### Prestadores de manutenção da V1

- A manutenção de ruas e instalações municipais é automática, executada por equipes reais do Pátio Municipal de Obras, com apoio externo contratado quando faltar capacidade local.

- Trabalho, materiais e entregas têm custos e execução físicos reais; contratação ou compromisso não equivale a manutenção concluída.

- Sem recursos suficientes, a atividade fica pendente e a infraestrutura afetada perde capacidade gradualmente, conforme a regra de insuficiência definida.

- Não exigir comandos de reparo por trecho/prédio nem criar novas construções ou políticas por esta decisão; quantidades, periodicidade e intensidade permanecem em calibração proporcional.

### Prefeitura — construção física, administração e participação cidadã

- Consequências causais de políticas municipais na vida dos SIMs: cada política realmente oferecida ao jogador na Prefeitura — tarifas, isenções por condição econômica, creches, vans, serviços, assistência, impostos e verbas municipais — deve produzir efeitos rastreáveis sobre dinheiro disponível de famílias e empresas, possibilidades reais de consumo/deslocamento/atendimento, frequência escolar, cuidado infantil, tempo para trabalhar, renda, bem-estar e condições materiais quando esses vínculos já existirem na simulação e houver causa concreta.

- Não atribuir bônus ou castigo abstrato pelo simples movimento de um controle: efeitos precisam passar por pagamentos, viagens, serviços, emprego, vagas e tempo efetivos.

- O painel da Prefeitura deve mostrar a decisão escolhida, custos e receitas reais, população impactada e consequências observadas, distinguindo previsão de resultado; informar também se a política não conseguiu produzir o efeito esperado por falta de recursos, capacidade ou elegibilidade.

- Não simular cada cidadão por quadro nem impor ao jogador decisões individuais só para parecer que existe profundidade.

#### Acesso à administração por menu e edifício

- O jogador pode abrir a gestão municipal por um acesso na interface ou selecionando a Prefeitura no mapa.

- Ambos levam à administração da mesma cidade e não criam controles paralelos, necessidade de deslocamento ou bloqueio por falta de operação do edifício, preservada a regra definida de liberação inicial das ferramentas após confirmar a posição da primeira Prefeitura.

#### Funcionários administrativos simples

- A Prefeitura emprega SIMs reais em ocupações administrativas genéricas, com vagas reais, deslocamentos, horários, salários pagos pelo Caixa e demais regras normais de emprego.

- Não exigir simular papelada individual, cargos especializados, protocolos, licenças, pontos burocráticos ou um funcionário executando cada botão.

- Os servidores não são um gargalo artificial para construir, alterar impostos, pagar despesas ou manter serviços definidos.

- Capacidades específicas de futuros programas só dependerão de equipe quando a funcionalidade correspondente for definida e aprovada; número de vagas e parâmetros ficam para calibração proporcional.

#### Construção e continuidade

- O edifício é construído fisicamente com dinheiro, materiais, trabalho e prazo reais; a primeira confirmação de seu posicionamento libera as ferramentas normais antes de a obra ficar pronta, conforme a regra de início da cidade.

- A prefeitura pode servir de referência visual para o crescimento urbano, sem limitar onde o jogador constrói.

- Sua ausência ou paralisação posterior não derruba a administração básica nem bloqueia novamente o catálogo.

### Caixa da cidade e investimento em construções

- O jogador administra um único Caixa da Cidade como recurso monetário principal controlável.

- Ele representa o capital disponível para desenvolver e operar a cidade no gameplay, não uma representação jurídica literal apenas do caixa da prefeitura.

#### Transparência completa de gastos da Prefeitura

- Toda saída monetária efetiva do Caixa da Cidade deve ser rastreável e consultável pelo jogador em um painel financeiro da Prefeitura, começando com total/resumo e categorias claras (por exemplo, obras e materiais, salários, serviços municipais, manutenção, transporte, atrações, previdência, assistência social, indenizações, juros/obrigações e outras despesas efetivamente modeladas).

- O jogador pode aprofundar por serviço/programa, período e lançamentos reais, identificando motivo, valor, data e recebedor/destino econômico quando houver, e relacionar custo efetivo ao resultado do serviço ou à política quando pertinente.

- Mostrar separadamente dinheiro apenas comprometido/reservado, pagamentos realmente realizados e dívidas pendentes, sem apresentar compromisso como despesa já paga, sem cobrança dupla e sem inventar números, contratos ou destinatários para preencher relatórios.

- A transparência vale para qualquer despesa atual ou futura da Prefeitura, não só auxílio-moradia; relatórios detalhados ficam disponíveis sob demanda, sem alertar o jogador por cada transação nem burocracia obrigatória para executar a rotina.

- Agregações e histórico devem aproveitar o registro causal/transacional já necessário à economia; retenção e desempenho ficam para implementação/calibração.

- Receitas municipais, impostos e outras entradas definidas alimentam esse caixa; despesas públicas, salários e construções ordenadas pelo jogador consomem esse caixa.

- Como o jogador posiciona diretamente as construções no escopo atual, obras públicas e privadas ordenadas pelo jogador consomem dinheiro do Caixa da Cidade e materiais físicos.

- Esse custo é parte deliberada da gameplay e do diferencial do IndexCities.

- O dinheiro gasto não desaparece: pagamentos a agentes locais reais vão para esses agentes; parcelas cuja contraparte seja externa ou não modelada retornam à Reserva Global.

#### Orçamento monetário de obras

- Ao confirmar uma construção, o custo monetário previsto fica comprometido no próprio Caixa da Cidade, deixando de estar disponível para novas despesas ou obras; esse comprometimento não transfere nem cria dinheiro e não utiliza a Reserva Global como depósito temporário.

- O jogador distingue saldo total, parcela comprometida e saldo disponível.

#### Pagamentos efetivos

- Valores comprometidos só saem do Caixa quando a despesa real correspondente vencer/for realizada, indo ao recebedor econômico correto (por exemplo, fornecedor local ou Reserva Global em importação).

- Uma importação pode exigir pagamento antes da entrega física, sem que isso dispense caminhões; não adiar automaticamente todos os pagamentos até a conclusão da obra.

- Salários periódicos já contabilizados de funcionários públicos, inclusive do Pátio, não podem ser cobrados uma segunda vez como custo da mesma mão de obra na obra.

#### Cancelamento monetário

- Ao cancelar uma obra inacabada, libera-se automaticamente o orçamento comprometido ainda não gasto.

- Pagamentos já efetivados não são revertidos por padrão, mesmo que a entrega física esteja pendente.

- Se nenhum gasto ocorreu, todo o valor comprometido volta a ficar disponível; não há multa fictícia de cancelamento.

- Nenhum cancelamento cria moeda ou retira dinheiro de um fornecedor/trabalhador que recebeu legitimamente.

- A primeira aquisição de um ativo privado recém-construído não devolve o preço ao Caixa da Cidade; o pagamento vai para a Reserva Global, evitando reciclagem automática de capital e preservando impostos/receitas públicas como fonte principal de recuperação financeira do jogador.

- Depois que uma empresa adquire e passa a operar o prédio, ela usa seu próprio caixa, receitas e despesas; o jogador não pode usar diretamente esse dinheiro como Caixa da Cidade nem precisa administrá-lo manualmente.

- O resultado de uma empresa privada beneficia ou prejudica a cidade por efeitos econômicos reais, como empregos, salários, impostos, produção, logística e eventual fechamento, não por transferência livre de seu caixa para o jogador.

## Visão aprovada (ainda não implementada)

### Moradia e finanças públicas

#### Prioridade fiscal entre serviços críticos e previdência

- Diante de Caixa insuficiente, o sistema procura preservar primeiro as despesas viáveis que sustentam serviços urbanos críticos e suas dependências reais (água, saúde, atendimento emergencial etc.) conforme a priorização essencial definida; aposentadorias devidas têm precedência sobre despesas municipais não essenciais, com recursos reais efetivamente disponíveis após compromissos críticos.

- Se mesmo assim não houver verba bastante para previdência, valem o rateio proporcional das aposentadorias, a dívida pública por parcelas não pagas, a perda de felicidade dos afetados e a quitação proporcional das aposentadorias atrasadas quando houver verba.

- Não converter prioridade em funcionamento/pagamento garantido sem caixa, nem exigir decisões manuais por instalação/SIM.

- **Em aberto:** Limites práticos e conflitos entre obrigações críticas ainda exigem calibração coerente.

### Prefeitura — construção física, administração e participação cidadã

> Programas avançados ficam fora da V1.

#### Auxílio-aluguel com teto mensal definido pelo jogador

- Além de ativar/desativar o programa municipal definido, o jogador pode definir pela Prefeitura um limite monetário de despesa por mês simulado para as transferências efetivas de auxílio-aluguel, sem selecionar famílias uma a uma.

- O sistema calcula elegibilidade, valores parciais limitados e prioridade automática às famílias com maior risco real de perda da casa segundo as regras do programa; a soma de pagamentos do programa no mês não pode superar o teto aprovado nem o dinheiro realmente disponível do Caixa.

- Se a verba não alcançar, famílias não atendidas continuam sujeitas às consequências definidas; não prometer ajuda ou quitar aluguel sem pagar locador real.

- Mostrar no mesmo painel teto do mês, já gasto, ainda disponível, pagamentos devidos/pendentes e famílias atendidas/não atendidas.

- Valor inicial e forma de ajustar o limite numérico seguem interface/calibração sem impor barra percentual ainda não aprovada.

#### Financiamento municipal por verba real

- No menu da Prefeitura, o jogador pode definir verbas monetárias reais por área de serviço, como saúde, educação, transporte e manutenção, com influência sobre operações futuras viáveis e investimentos, em teste de gameplay, no lugar de um cursor de eficiência abstrata.

- Cada painel deve mostrar necessidade de financiamento estimada e fundamentada nos custos físicos/contratuais atuais, verba que o jogador decidiu destinar, gasto realmente pago no período, receita efetivamente recebida, compromissos, obrigações vencidas e atendimento/capacidade observados, conciliados ao Caixa detalhado definido; a verba autorizada não é dinheiro pago.

- Escolher verba menor não cancela salários já devidos, direitos, contratos ou custos inevitáveis: obrigações reais continuam exigíveis e podem gerar atrasos/dívidas, enquanto contratações, compras e ampliação operacionais ainda não assumidas ficam limitadas por recursos.

- A decisão de verba também não substitui as prioridades essenciais e previdenciárias definidas em crise fiscal, que devem ser explicadas se restringirem a vontade do jogador.

- Verba maior não multiplica automaticamente veículos, professores, médicos ou capacidade sem pessoas, instalações, insumos, abastecimento, logística, tempo e saldo monetário verdadeiros.

- Este modelo é um teste inicial de gameplay, não uma arquitetura definitiva nem um percentual de 50–150%; formato de controle, horizonte temporal/periodicidade e calibração serão validados na interface e simulação integradas.

#### Persistência da verba mensal de serviços públicos

- Quando o jogador definir uma verba monetária mensal para determinada área de serviço na Prefeitura, o mesmo limite continua valendo nos meses simulados seguintes até o jogador alterá-lo, sem exigir aprovação a cada virada de mês nem funcionar como depósito que se esgota para sempre.

- Cada mês contabiliza separadamente limite, compromissos, pagamentos efetivos e insuficiência de recursos; não reservar nem criar dinheiro fictício fora do Caixa da Cidade.

- A verba é limite de financiamento de operação/novas despesas viáveis, não garantia de cobertura ou autorização para ignorar obrigações reais vencidas, prioridades essenciais e demais regras definidas.

- Preservar o caráter de teste de gameplay do orçamento por área; valores e interface são calibração.

#### Encerramento automático de solicitações resolvidas

- Quando a necessidade real e persistente que originou um pedido efetivamente feito por SIM ou empresa deixar de existir, a solicitação correspondente é encerrada automaticamente e preservada em histórico consultável, com causa/resultado observados; construir um prédio sem equipe, vaga ou serviço efetivo não resolve um pedido só pela aparência.

- Não exigir clique de aceite/arquivamento nem confundir o fim da solicitação com o fechamento de um alerta diferente.

#### Manifestação contextual de opiniões

- SIMs reais podem decidir expressar opinião quando alguma decisão municipal alterar de forma significativa sua situação/interesses concretos.

- A ocorrência não implica que todos opinem; mostrar autores e opiniões de fato expressas, separando-as dos impactos diagnosticados e sem pesquisa obrigatória, voto inventado, varredura contínua de todos os cidadãos ou recompensa política.

#### Política opcional de preferência local nas compras municipais

- A Prefeitura pode habilitar/desabilitar uma preferência moderada por fornecedores locais reais em aquisições municipais, aceitando custo total um pouco maior somente dentro de uma tolerância calibrável e quando a oferta, prazo e entrega físicos forem viáveis.

- Sem a política, selecionar pelas condições econômicas/logísticas reais sem esse favorecimento.

- Não comprar material inexistente, impor compra obrigatória de fornecedor local, garantir empregos/lucros ou exigir aprovação manual por pedido; despesa e fornecedor reais ficam rastreáveis no Caixa.

- A preferência definida para reabastecimento de empresas privadas permanece autônoma e distinta desta política.

#### Auxílio-aluguel — elegibilidade e cobertura

- O programa opcional definido pode atender famílias com dificuldade econômica efetiva e risco verificável de não conseguir pagar aluguel residencial existente, mesmo antes do primeiro atraso; não basta aluguel alto com renda suficiente nem se exige dívida prévia.

- O auxílio pode complementar a parcela que faltar ao pagamento viável pela família, até um teto do programa, e o dinheiro realmente liberado pelo Caixa é transferido ao locador legítimo, abatendo a mesma obrigação, sem pagamento duplo, crédito presumido ou garantia de atendimento universal.

- A prioridade por risco de perda da moradia em falta de verba continua aprovada.

#### Auxílio-aluguel — continuidade enquanto houver necessidade e verba

- A condição econômica da família beneficiária continua reavaliada automaticamente a cada mês do calendário simulado, junto à obrigação do aluguel.

- Não há prazo máximo fixo de participação por família: o programa pode continuar nos meses seguintes enquanto o domicílio mantiver dificuldade financeira real e contrato elegível, a política municipal estiver ativa, o teto mensal do programa permitir e o Caixa possuir dinheiro verdadeiro.

- Cada mês exige avaliação, prioridade automática e pagamento efetivo ao locador; verba aprovada não é saldo recebido nem promessa de cobertura universal.

- Se a situação melhorar, cessar a elegibilidade; se a verba faltar, benefícios não pagos não surgem como dinheiro e permanecem os efeitos normais do aluguel devido, inclusive riscos de desocupação.

- Elegibilidade, teto mensal, prioridade por risco e parcelas atrasadas seguem as demais regras do programa, sem microgerenciamento familiar.

#### Auxílio-aluguel — débitos atuais e atrasados

- A assistência pode abater aluguéis mensais atuais e parcelas vencidas do mesmo contrato e locador reais, dentro dos limites e da verba efetivamente paga pelo Caixa, priorizando famílias sob risco concreto de despejo segundo a regra definida.

- Dívida não se extingue apenas por elegibilidade, e a contagem de inadimplência somente cessa/zera quando a dívida vencida for integralmente quitada conforme a regra residencial.

- Sem dinheiro, as regras de desocupação permanecem.

#### Cooperação externa para emergências

- A Prefeitura pode habilitar/desabilitar pedido de apoio emergencial real vindo do exterior quando faltar capacidade local, por exemplo caminhão de bombeiros com equipe real.

- Resgate só ocorre se houver serviço externo efetivamente disponível e limitado, chegada por acesso físico/tempo, operação e custo monetário verdadeiros, pagos pelo Caixa ao destino econômico correto; não há viatura, profissional, capacidade, atendimento imediato ou receita fictícios.

- A política não substitui serviços locais nem garante disponibilidade universal; o custo e o atendimento efetivo devem ser diagnosticáveis, sem despachar manualmente cada ocorrência.

- O alcance abrange incêndio, socorro médico emergencial e apoio policial; não estender por analogia a outros serviços.

#### Abrangência da cooperação emergencial externa

- A política municipal opcional de solicitar ajuda externa quando faltar capacidade local pode abranger combate a incêndios, socorro médico emergencial e apoio policial, somente para ocorrências reais e pertinentes a cada serviço.

- Em todos os casos, veículos, pessoal disponível, acesso pela conexão exterior, viagem, tempo de resposta, atividade e custo/contrapartida monetária devem ser efetivos e limitados; não fornecer automaticamente viaturas, ambulâncias, profissionais, vagas hospitalares, prisões ou policiamento infinito, nem garantir atendimento ou substituir a rede local.

- A simulação decide quando solicitar o apoio conforme gravidade e capacidade, sem despacho manual por ocorrência; disponibilidade e comportamento operacional específico de cada tipo permanecem para definição proporcional antes do código correspondente.

#### Papel central

- A Prefeitura é o centro de gestão municipal e de participação da população, reunindo de forma organizada as decisões permanentes do jogador (incluindo os controles de impostos, Caixa, crédito, serviços e transporte definidos), pautas/demandas ligadas a condições reais de SIMs e bairros, e a direção de programas e políticas municipais com efeitos econômicos e sociais reais.

- A administração e os programas incluem espaço para moradores apresentarem interesses, solicitações e posições potencialmente conflitantes; o jogador pode compreender quem é afetado, comparar consequências e decidir, sem perder o foco de construir uma cidade.

- A participação não vira missão obrigatória, eleição, partido ou sistema de votação por inferência.

#### Solicitações feitas por SIMs reais

- Moradores existentes podem decidir apresentar pedidos reais à Prefeitura, motivados por necessidades e condições que efetivamente vivenciam (por exemplo, falta de vaga escolar para seu filho).

- Pedidos semelhantes podem ser mostrados juntos por tema e região, preservando a identificação dos requerentes reais e distinguindo pedido efetivamente feito, problema apenas detectado pela simulação e iniciativa do jogador.

- Não inventar autores, falas, assinaturas ou unanimidade, nem produzir notificação por SIM; o meio de envio, critérios de iniciativa e frequência ficam para definição proporcional, sem exigir comparecimento físico por inferência.

#### Pedidos sem compromisso obrigatório

- O jogador pode consultar uma solicitação, investigar suas causas e agir livremente por qualquer meio já disponível ou não agir, sem precisar aceitá-la/rejeitá-la formalmente.

- Uma pauta não gera promessa, prazo, missão, penalidade por falta de resposta, obra ou política executada por clique.

- O problema subjacente continua produzindo as consequências reais normais, e o jogador pode acompanhar se a situação dos afetados melhorou de fato; não confundir fechar a visualização de um pedido com solucionar a necessidade.

#### Interesses divergentes da população

- A Prefeitura permite perceber que moradores e grupos podem ter interesses diferentes ou conflitantes diante de uma medida (por exemplo, contribuintes preferindo menos impostos enquanto outros dependem dos serviços financiados).

- Mostrar quem é afetado e o fundamento concreto do interesse, sem tratar uma opinião individual como vontade coletiva, atribuir posição fictícia a SIMs ou introduzir eleições, votação ou facções políticas.

- **Em aberto:** O jogador decide de acordo com o estado real e os efeitos possíveis na cidade; detalhes de como interesses são expressos e agrupados ainda precisam ser definidos.

#### Primeiras políticas a organizar

- Começar pela apresentação e acompanhamento das políticas e decisões municipais definidas, especialmente as quatro faixas agregadas (Grátis/Barata/Normal/Cara) definidas separadamente para ônibus e estacionamentos públicos de superfície, além das seis alíquotas do imposto imobiliário, além dos controles pertinentes de orçamento e serviços.

- Reunir escolhas, custos, grupos afetados e resultados observados não cria novas alíquotas, programas, subsídios ou poderes de governo.

- Políticas novas serão avaliadas separadamente quanto a seu valor para gameplay, recursos e consequências efetivas.

#### Ingressos municipais sob controle agregado

- O jogador escolhe Grátis, Barato, Normal ou Caro como política geral para atrações municipais dedicadas com ingresso (por exemplo museus e mirantes), com quatro faixas agregadas.

- Nas faixas pagas, o valor positivo decorre de base/custos calibrados; só visitantes reais que de fato consigam entrar e pagar transferem dinheiro ao Caixa, sem receitas imaginárias.

- Gratuidade pode ampliar o acesso mas não elimina salários/manutenção; preço alto pode reduzir visitantes e arrecadação efetiva.

- Não estender automaticamente a parques, praças, escolas, hospitais ou empreendimentos privados, nem definir preço manual por visitante.

#### Política de auxílio ao aluguel residencial

- A Prefeitura pode ativar/desativar um programa agregado opcional de assistência a famílias residentes em dificuldade financeira habitacional real, oferecendo ajuda condicionada à necessidade e recursos reais, sem prazo máximo fixo por família, para parte do aluguel efetivamente devido de contrato existente e locador real.

- A despesa sai do Caixa da Cidade e é paga ao proprietário credor do aluguel, vinculada à família/locação beneficiada, abatendo a mesma obrigação devida sem duplicar pagamento do SIM, receita do locador ou moeda.

- Sem dinheiro suficiente do Caixa ou do limite mensal, não se presume benefício pago, cobertura ilimitada nem extinção automática de dívida por mera elegibilidade.

- Elegibilidade, complementação, reavaliação mensal, inclusão de atrasos e teto monetário mensal seguem as regras específicas do programa, sem prazo máximo fixo e com atendimento automático segundo risco real.

- Não criar moradias municipais, bolsa sem destinatário, burocracia individual nem resgate financeiro garantido por inferência.

#### Prioridade do auxílio-aluguel quando a verba é insuficiente

- Se o programa municipal opcional tiver recursos reais insuficientes para atender todos os domicílios elegíveis, priorizar automaticamente as famílias com maior risco concreto e iminente de perda da moradia, inferido de condições verificáveis como compromissos de aluguel não pagos, proximidade do prazo de desocupação e capacidade real de pagamento, sem premiar apenas ordem de envio de pedidos nem sortear famílias arbitrariamente.

- A seleção não gera novas verbas nem garante cobertura integral; a soma efetivamente transferida a locadores reais nunca excede o Caixa aplicável e abate os respectivos aluguéis devidos.

- Mostrar no painel da Prefeitura valor gasto, alcance, famílias atendidas/não atendidas e principal motivo de prioridade ou falta de recursos, sob demanda.

- Pesos, limites, desempates e janela de assistência são calibração/definição antes da implementação dependente; preservar a regra residencial existente quando o auxílio não for recebido.

#### Quando um morador solicita providências

- Um SIM pode decidir enviar uma solicitação quando enfrenta um problema concreto, relevante e persistente em sua vida ou família, em vez de gerar uma reclamação para cada dificuldade transitória.

- A existência de uma dificuldade não significa automaticamente que todos os afetados apresentaram pedido; decidir solicitar continua sendo um comportamento do SIM.

- Quais fatos justificam solicitação, persistência mínima e cadência são definidos proporcionalmente ao modelo real de necessidades, sem notificações repetitivas.

#### Pedido sem visita obrigatória

- Um SIM pode registrar solicitação na Prefeitura sem deslocar-se fisicamente até o prédio; não criar atendimento presencial obrigatório, rota, fila ou funcionário responsável por receber cada pedido.

- **Em aberto:** O canal concreto de apresentação é detalhe de interface/simulação a definir, não uma autorização para fabricar requerimentos.

#### Empresas também podem solicitar

- Empresas existentes, como entidades econômicas autônomas com dificuldades e interesses próprios, podem apresentar pedidos ligados a uma situação econômica ou operacional real (por exemplo, problemas persistentes de acesso logístico).

- A solicitação não transfere ao jogador a gestão privada da empresa, não obriga intervenção nem cria demanda fictícia; distinguir pedidos de empresas, de SIMs e diagnósticos automáticos.

#### Opiniões expressas por moradores

- Além de diagnosticar quem uma decisão afeta, a Prefeitura pode apresentar opiniões efetivamente manifestadas por SIMs reais sobre medidas municipais, com apoio ou crítica coerentes com suas situações e interesses.

- Manter separadas avaliação objetiva dos impactos, opinião individual expressa e grupo realmente representado; não presumir que todo morador opina, inventar unanimidade ou inferir voto/facção.

- **Em aberto:** A forma de manifestação e agrupamento das opiniões permanece por definir.

#### Pedidos organizados por assunto e região

- A Prefeitura apresenta solicitações semelhantes agrupadas por tema e área, com possibilidade de consultar quais SIMs ou empresas efetivamente as fizeram.

- Quantidades de pedidos referem-se somente a solicitações reais, não ao número de afetados detectado pelo jogo.

- Uma mesma necessidade não deve aparecer como dezenas de avisos nem como missões obrigatórias; mecanismos de agrupamento e atualização ficam para implementação.

#### Visualização compacta de pedidos ainda não resolvidos

- Solicitações reais e persistentes que ainda correspondam a problemas existentes continuam consultáveis, agrupadas compactamente por tema e região, sem serem arquivadas automaticamente apenas por envelhecerem e sem destaque igual para todos os itens.

- Casos graves e atuais podem continuar evidenciados, reutilizando diagnóstico e relevância existentes, sem criar penalidade por ignorar pedidos, protocolos obrigatórios ou alertas duplicados.

- O encerramento automático permanece reservado a necessidades efetivamente resolvidas, com histórico consultável, conforme a regra de encerramento automático de solicitações resolvidas.

#### Limites e pontos ainda abertos

- Não duplicar alertas nem desafios opcionais e não transformar participação em fila de tarefas.

- Pedidos e opiniões: pedidos motivados por problemas persistentes, sem visita obrigatória, podendo vir de empresas; opiniões efetivamente expressas por SIMs; agregação por assunto/região; acesso por menu ou prédio.

- Estatísticas e prestação de contas são parte da gestão definida, não uma política nova: a Prefeitura reúne informações consultáveis e diagnosticáveis dos serviços municipais (por exemplo, utilização efetiva do transporte público, despesas, arrecadação, atendimento e capacidade conforme o que a simulação registrar), além da transparência detalhada dos gastos definida no Caixa da Cidade.

- A visualização desses dados integra a gestão; não criar bônus, recompensas, notificações, relatórios obrigatórios ou um sistema especial para julgar cada decisão.

- Comparativos automáticos de antes/depois por política continuam não aprovados e precisam de nova decisão se acrescentarem comportamento além da visualização comum.

- **Em aberto:** Continuam em exploração: critérios e cadência de pedidos, expressão e agrupamento de opiniões, desenho concreto da interface e políticas/programas específicos.

- Educação de adultos e preferência local nas compras municipais constam de regras próprias na SPEC; nenhuma pauta cria emprego, edifício, dinheiro ou felicidade por si só.

#### Limites de aprovação

- Os princípios de solicitações reais, liberdade de resposta, interesses divergentes e uso inicial das políticas não definem canal/ritmo de pedidos, formalização de opiniões, programas específicos nem procedimentos detalhados.

- Educação de adultos e a preferência local moderada em compras municipais seguem regras próprias; a ideia de menor preço obrigatório como política distinta não foi aprovada.

- Não criar catálogo de leis, bônus genéricos, novas exigências burocráticas ou dependências extras por inferência.
