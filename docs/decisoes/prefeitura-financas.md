# Histórico de decisões — Prefeitura e finanças públicas

> Só registro histórico; as regras vigentes estão em [docs/spec/prefeitura-financas.md](../spec/prefeitura-financas.md).

- 2026-10-08 — Priorização automática de manutenção essencial em déficit: se o dinheiro ou os recursos efetivamente disponíveis não forem suficientes para manter toda a infraestrutura municipal, a simulação prioriza automaticamente as despesas e atividades viáveis necessárias à continuidade de serviços urbanos essenciais e de suas dependências críticas, em vez de exigir que o jogador autorize reparo ou verba individual por construção (origem: opção 5A) → seção Moradia e finanças públicas

- 2026-10-08 — Consequência da falta de orçamento para manutenção: não pagar uma despesa de manutenção não equivale a tê-la realizado (origem: opção 5B) → seção Moradia e finanças públicas

- 2026-10-08 — Custo recorrente de manutenção/funcionamento da infraestrutura municipal: além da construção, manter ruas, redes e demais infraestruturas municipais exige despesas recorrentes reais, apresentadas ao jogador de forma agregada por categoria e proporcionais à infraestrutura/capacidade efetivamente existente, para que a expansão tenha compromisso financeiro duradouro (origem: opção 5B) → seção Moradia e finanças públicas

- 2026-10-09 — Moradia sem água ou energia suficiente: imóveis residenciais podem ser comprados e ocupados por SIMs mesmo quando água e/ou eletricidade não atendem plenamente a residência, sem bloqueio artificial de compra/mudança por falta de abastecimento (origem: opção 1A) → seção Moradia e finanças públicas

- 2026-10-09 — Superlotação residencial permitida com consequências: cada casa/apartamento possui uma capacidade confortável de moradores coerente com seu porte; a família pode excedê-la sem despejo automático, proibição de nascimento ou obrigação de mudar (origem: opção 2B) → seção Moradia e finanças públicas

- 2026-10-09 — Inadimplência de imposto imobiliário: quando o SIM proprietário residencial ou a empresa proprietária comercial/industrial não tiver dinheiro real suficiente para quitar o imposto devido por imóvel privado, a parte não paga se torna dívida monetária verdadeira do proprietário com o Caixa da Cidade, acompanhada por imóvel e devedor sem duplicação de créditos (origem: opção 1A) → seção Moradia e finanças públicas

- 2026-10-09 — Imóvel residencial com proprietário, mas desocupado — sem manutenção imobiliária adicional inicialmente: se o SIM possui casa ou apartamento e ainda não mora ou não encontrou inquilino, continua responsável pelo imposto imobiliário normal, calculado pelo valor de mercado (origem: opção 9A) → seção Moradia e finanças públicas

- 2026-10-09 — Inadimplência de empréstimo municipal: se a cidade não conseguir pagar parcela/obrigação de crédito já contratado no vencimento, a quantia não paga permanece como dívida real com a Reserva Global, sem criar pagamento fictício, perdão, cancelamento de moeda ou contratação automática de outro empréstimo (origem: opção 1A) → seção Moradia e finanças públicas

- 2026-10-09 — Contratação manual de empréstimo municipal: somente o jogador decide solicitar e confirmar um empréstimo nas ferramentas de finanças municipais, com informação compreensível de valor, dívida e condições efetivas (origem: opção 2A) → seção Moradia e finanças públicas

- 2026-10-10 — Fazendas na tributação industrial de baixa densidade: no modelo inicial, os imóveis rurais produtivos/fazendas sujeitos ao imposto imobiliário usam a alíquota industrial de baixa densidade já controlada pelo jogador, aplicada ao valor de mercado e com proprietário/devedor real como nas outras propriedades econômicas (origem: opção 9A) → seção Moradia e finanças públicas

- 2026-10-10 — Crise fiscal municipal sem fim automático de partida: déficit, inadimplência e falta persistente de recursos da prefeitura não encerram o save nem provocam derrota obrigatória (origem: opção 1A) → seção Moradia e finanças públicas

- 2026-10-10 — Prioridade fiscal entre serviços críticos e previdência: diante de Caixa insuficiente, o sistema procura preservar primeiro as despesas viáveis que sustentam serviços urbanos críticos e suas dependências reais (água, saúde, atendimento emergencial etc.) conforme a priorização essencial definida; aposentadorias devidas têm precedência sobre despesas municipais não essenciais, com recursos reais efetivamente disponíveis após compromissos críticos (origem: opção 13A) → seção Moradia e finanças públicas

- 2026-10-11 — Consequências causais de políticas municipais na vida dos SIMs: cada política realmente oferecida ao jogador na Prefeitura — tarifas, isenções por condição econômica, creches, vans, serviços, assistência, impostos e verbas municipais — deve produzir efeitos rastreáveis sobre dinheiro disponível de famílias e empresas, possibilidades reais de consumo/deslocamento/atendimento, frequência escolar, cuidado infantil, tempo para trabalhar, renda, bem-estar e condições materiais quando esses vínculos já existirem na simulação e houver causa concreta (origem: aprovação expressa registrada na SPEC) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Auxílio-aluguel com teto mensal definido pelo jogador: além de ativar/desativar o programa municipal definido, o jogador pode definir pela Prefeitura um limite monetário de despesa por mês simulado para as transferências efetivas de auxílio-aluguel, sem selecionar famílias uma a uma (origem: aprovação expressa registrada na SPEC) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Financiamento municipal por verba real: no menu da Prefeitura, o jogador pode definir verbas monetárias reais por área de serviço, como saúde, educação, transporte e manutenção, com influência sobre operações futuras viáveis e investimentos, em teste de gameplay, no lugar de um cursor de eficiência abstrata (origem: revisão 11B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Persistência da verba mensal de serviços públicos: quando o jogador definir uma verba monetária mensal para determinada área de serviço na Prefeitura, o mesmo limite continua valendo nos meses simulados seguintes até o jogador alterá-lo, sem exigir aprovação a cada virada de mês nem funcionar como depósito que se esgota para sempre (origem: escolha 1A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Encerramento automático de solicitações resolvidas: quando a necessidade real e persistente que originou um pedido efetivamente feito por SIM ou empresa deixar de existir, a solicitação correspondente é encerrada automaticamente e preservada em histórico consultável, com causa/resultado observados; construir um prédio sem equipe, vaga ou serviço efetivo não resolve um pedido só pela aparência (origem: escolha 1A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Manifestação contextual de opiniões: SIMs reais podem decidir expressar opinião quando alguma decisão municipal alterar de forma significativa sua situação/interesses concretos (origem: escolha 2A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Política opcional de preferência local nas compras municipais: a Prefeitura pode habilitar/desabilitar uma preferência moderada por fornecedores locais reais em aquisições municipais, aceitando custo total um pouco maior somente dentro de uma tolerância calibrável e quando a oferta, prazo e entrega físicos forem viáveis (origem: escolha 3B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Auxílio-aluguel — elegibilidade e cobertura: o programa opcional definido pode atender famílias com dificuldade econômica efetiva e risco verificável de não conseguir pagar aluguel residencial existente, mesmo antes do primeiro atraso; não basta aluguel alto com renda suficiente nem se exige dívida prévia (origem: escolhas 4B/5A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Auxílio-aluguel — continuidade enquanto houver necessidade e verba: a condição econômica da família beneficiária continua reavaliada automaticamente a cada mês do calendário simulado, junto à obrigação do aluguel (origem: revisão 1B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Auxílio-aluguel — débitos atuais e atrasados: a assistência pode abater aluguéis mensais atuais e parcelas vencidas do mesmo contrato e locador reais, dentro dos limites e da verba efetivamente paga pelo Caixa, priorizando famílias sob risco concreto de despejo segundo a regra definida (origem: escolha 7C) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Cooperação externa para emergências: a Prefeitura pode habilitar/desabilitar pedido de apoio emergencial real vindo do exterior quando faltar capacidade local, por exemplo caminhão de bombeiros com equipe real (origem: revisão 3C) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Abrangência da cooperação emergencial externa: a política municipal opcional de solicitar ajuda externa quando faltar capacidade local pode abranger combate a incêndios, socorro médico emergencial e apoio policial, somente para ocorrências reais e pertinentes a cada serviço (origem: revisão 3C) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Papel central: a Prefeitura é o centro de gestão municipal e de participação da população, reunindo de forma organizada as decisões permanentes do jogador (incluindo os controles de impostos, Caixa, crédito, serviços e transporte definidos), pautas/demandas ligadas a condições reais de SIMs e bairros, e a direção de programas e políticas municipais com efeitos econômicos e sociais reais (origem: opção C, opção B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Solicitações feitas por SIMs reais: moradores existentes podem decidir apresentar pedidos reais à Prefeitura, motivados por necessidades e condições que efetivamente vivenciam (por exemplo, falta de vaga escolar para seu filho) (origem: escolha 1B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Pedidos sem compromisso obrigatório: o jogador pode consultar uma solicitação, investigar suas causas e agir livremente por qualquer meio já disponível ou não agir, sem precisar aceitá-la/rejeitá-la formalmente (origem: escolha 2A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Interesses divergentes da população: a Prefeitura permite perceber que moradores e grupos podem ter interesses diferentes ou conflitantes diante de uma medida (por exemplo, contribuintes preferindo menos impostos enquanto outros dependem dos serviços financiados) (origem: escolha 3B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Primeiras políticas a organizar: começar pela apresentação e acompanhamento das políticas e decisões municipais definidas, especialmente as quatro faixas agregadas (Grátis/Barata/Normal/Cara) definidas separadamente para ônibus e estacionamentos públicos de superfície, além das seis alíquotas do imposto imobiliário, além dos controles pertinentes de orçamento e serviços (origem: escolha 4A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Ingressos municipais sob controle agregado: o jogador escolhe Grátis, Barato, Normal ou Caro como política geral para atrações municipais dedicadas com ingresso (por exemplo museus e mirantes), com quatro faixas agregadas (origem: aprovação expressa registrada na SPEC) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Política de auxílio ao aluguel residencial: a Prefeitura pode ativar/desativar um programa agregado opcional de assistência a famílias residentes em dificuldade financeira habitacional real, oferecendo ajuda condicionada à necessidade e recursos reais, sem prazo máximo fixo por família, para parte do aluguel efetivamente devido de contrato existente e locador real (origem: pergunta 7B, revisão 1B, revisão 4B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Prioridade do auxílio-aluguel quando a verba é insuficiente: se o programa municipal opcional tiver recursos reais insuficientes para atender todos os domicílios elegíveis, priorizar automaticamente as famílias com maior risco concreto e iminente de perda da moradia, inferido de condições verificáveis como compromissos de aluguel não pagos, proximidade do prazo de desocupação e capacidade real de pagamento, sem premiar apenas ordem de envio de pedidos nem sortear famílias arbitrariamente (origem: pergunta 4A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Quando um morador solicita providências: um SIM pode decidir enviar uma solicitação quando enfrenta um problema concreto, relevante e persistente em sua vida ou família, em vez de gerar uma reclamação para cada dificuldade transitória (origem: escolha 1A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Pedido sem visita obrigatória: um SIM pode registrar solicitação na Prefeitura sem deslocar-se fisicamente até o prédio; não criar atendimento presencial obrigatório, rota, fila ou funcionário responsável por receber cada pedido (origem: escolha 2A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Empresas também podem solicitar: empresas existentes, como entidades econômicas autônomas com dificuldades e interesses próprios, podem apresentar pedidos ligados a uma situação econômica ou operacional real (por exemplo, problemas persistentes de acesso logístico) (origem: escolha 3B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Opiniões expressas por moradores: além de diagnosticar quem uma decisão afeta, a Prefeitura pode apresentar opiniões efetivamente manifestadas por SIMs reais sobre medidas municipais, com apoio ou crítica coerentes com suas situações e interesses (origem: escolha 4B) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Pedidos organizados por assunto e região: a Prefeitura apresenta solicitações semelhantes agrupadas por tema e área, com possibilidade de consultar quais SIMs ou empresas efetivamente as fizeram (origem: escolha 5A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Visualização compacta de pedidos ainda não resolvidos: solicitações reais e persistentes que ainda correspondam a problemas existentes continuam consultáveis, agrupadas compactamente por tema e região, sem serem arquivadas automaticamente apenas por envelhecerem e sem destaque igual para todos os itens (origem: revisão 7C, escolha 1A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Acesso à administração por menu e edifício: o jogador pode abrir a gestão municipal por um acesso na interface ou selecionando a Prefeitura no mapa (origem: escolha 7A) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Limites e pontos ainda abertos: não duplicar alertas nem desafios opcionais e não transformar participação em fila de tarefas (origem: pergunta 6) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-11 — Transparência completa de gastos da Prefeitura: toda saída monetária efetiva do Caixa da Cidade deve ser rastreável e consultável pelo jogador em um painel financeiro da Prefeitura, começando com total/resumo e categorias claras (por exemplo, obras e materiais, salários, serviços municipais, manutenção, transporte, atrações, previdência, assistência social, indenizações, juros/obrigações e outras despesas efetivamente modeladas) (origem: aprovação expressa registrada na SPEC) → seção Caixa da cidade e investimento em construções

- 2026-10-12 — Pacote tributário da V1: as seis alíquotas são anuais, começam em 1% ao ano e podem ser ajustadas pelo jogador entre 0% e 8% ao ano (origem: aprovação expressa registrada na SPEC) → seção Moradia e finanças públicas

- 2026-10-12 — O principal de um empréstimo sai da Reserva Global e entra no Caixa da Cidade; amortizações e juros retornam à Reserva Global (origem: aprovação expressa registrada na SPEC) → seção Moradia e finanças públicas

- 2026-10-12 — Prestadores de manutenção da V1: a manutenção de ruas e instalações municipais é automática, executada por equipes reais do Pátio Municipal de Obras, com apoio externo contratado quando faltar capacidade local (origem: aprovação expressa registrada na SPEC) → seção Moradia e finanças públicas

- 2026-10-12 — Confirmar a posição da primeira Prefeitura libera o catálogo antes da conclusão da obra, substituindo a hipótese verbal de paralisar a administração para evitar bloqueio circular no início vazio (origem: escopo V1; hipótese anterior 3B registrada na seção temática) → seção Prefeitura — construção física, administração e participação cidadã

- 2026-10-17 — Ordem dos trechos de manutenção viária: a equipe faz uma rua de cada vez, em sequência ao longo da rua (A1, A2, A3 e depois B1, B2, B3), em vez de alternar entre ruas próximas; os trechos consertados são os mesmos (decisão do responsável; motivo: menos combustível em travessias) → seção Prestadores de manutenção da V1

- 2026-10-17 — Reenvio da equipe externa após voltar para reabastecer: o novo envio paga frete e combustível de novo, pela regra existente de envio da equipe externa (decisão do responsável) → seção Prestadores de manutenção da V1
