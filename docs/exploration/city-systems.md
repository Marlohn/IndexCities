# IndexCities — Mobilidade, serviços e infraestrutura urbana

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa; diversos comportamentos já estão parcialmente promovidos para a SPEC — não é fonte de verdade.
>
> Concentra pesquisa e raciocínio sobre serviços urbanos, mobilidade, vias, educação, saúde, infraestrutura técnica, conexão externa e capacidades operacionais. A SPEC continua sendo a autoridade sobre o que já foi decidido.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Água e energia: mesma distribuição simplificada e distância secundária (2026-10-09)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável confirmou expressamente **o mesmo comportamento inicial para água e eletricidade**, sem transformar a distância das instalações em uma decisão importante do jogador. A preferência original por atender regiões próximas antes de regiões distantes em escassez continua uma direção qualitativa, agora também para energia. **O modelo agregado com redução regional suave foi aprovado explicitamente em seguida (2026-10-09) e está na SPEC; parâmetros e resultados de performance continuam em calibração/validação, não são promessas.**

**Direção aprovada na SPEC:** manter capacidade e demanda reais **independentes para cada recurso**, com regras compartilhadas de distribuição. Construir usina ou estação distante não deve, isoladamente, provocar falta de serviço numa cidade com oferta total suficiente e infraestrutura funcional. Em escassez, distância pode pesar de modo **leve e gradual** contra regiões mais afastadas das instalações operacionais, sem hierarquia de consumidores por classe (por exemplo, hospitais não recebem prioridade hídrica/energética artificial). Para água, considerar estrutura de abastecimento efetivo de **água tratada**, não mera proximidade do rio. Não exigir desenho manual de canos ou fiação. Preservar a **prioridade financeira** já aprovada para despesas de serviços essenciais: ela é distinta da regra de alocação de água/energia.

**Modelo aprovado na SPEC: capacidade global separada por recurso + escassez regional suave.** A expressão “dois reservatórios contábeis + sombra leve de distância” é apenas analogia explicativa; não cria armazenamento físico de eletricidade ou de água fora das estruturas reais.

- Tratar cada recurso como um **saldo agregado de capacidade de serviço** da cidade: produção operacional real versus demanda real, separadamente para água e energia. **Não** misturar recursos; água não cobre déficit elétrico.
- Se a capacidade do recurso cobrir a demanda e as instalações estiverem funcionais, **todos os consumidores elegíveis recebem normalmente**, onde quer que estejam. Sem alcance rígido por distância, pagamento de "frete" desses serviços ou propagação individual simulada.
- Só em **déficit**, calcular um **índice de atendimento por área/bairro**, suavemente menor nas áreas distantes da instalação operacional mais próxima; uma falta de 5% não deveria apagar abruptamente um bairro inteiro. Efeito espacial moderado e calibrável; nem alocação proporcional idêntica obrigatória nem corte instantâneo "longe perde tudo". Sem benefício por classe de prédio. **O princípio foi aprovado; parâmetros concretos de gradiente ainda não.** A soma do consumo efetivamente atendido não ultrapassa a produção disponível.
- **Direção leve aprovada, com estratégia técnica a medir:** acumular demanda por setor espacial e guardar distância aproximada/instalação ativa mais próxima. Atualizar distâncias quando instalações/topologia relevantes mudarem; recomputar índices quando produção ou demanda agregadas mudarem significativamente, sem rota, hidráulica ou fluxo elétrico individual a cada frame. Não criar uma simulação espacial separada por SIM.
- **Diagnóstico:** duas barras simples (produção vs demanda por recurso) e mapa/inspeção opcional de áreas com serviço reduzido; consequências de capacidade insuficiente continuam reais (perda de eficiência, interrupções em situações graves) conforme regras já aprovadas.

**Comparação:** capacidade global totalmente uniforme seria ainda mais simples, mas eliminaria o efeito espacial desejado; malhas individualizadas por canos/fios, redes e fluxo por edifício tornariam a distância muito central e criariam estados/cálculos de retorno pouco úteis ao gameplay inicial. **Limite assumido da escolha aprovada:** o modelo é uma abstração de cobertura, não uma previsão fiel de hidráulica ou potência elétrica; fontes múltiplas e critérios para iniciar falha total precisam ser calibrados e testados. Agrupar/cachear é técnica conhecida em simulações de redes, mas não garante custo nulo: o benefício precisa ser medido. Referência técnica comparativa, não requisito: [GameDev SE — agrupamento/cache de redes elétricas](https://gamedev.stackexchange.com/questions/138686/how-should-i-make-realistic-electricity).

**Estado após aprovação expressa (2026-10-09):** **simetria água/energia, capacidade global independente, atendimento pleno quando oferta basta, redução regional suave apenas na escassez e custo interno contido** estão oficialmente registrados na SPEC. Permanecem em exploração **parâmetros, medidas de desempenho e combinações de múltiplas fontes**, não a escolha do modelo.
---

## Água/energia empresarial e salários municipais — decisões 2A/3A (2026-10-09)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **aprovou 2A e 3A** em 2026-10-09 e ambas estão na SPEC; comparações, riscos e explicações adicionais da IA seguem sem revisão integral. As opções alternativas abaixo são históricas, não funcionalidades aprovadas. Preservar tarifa automática (2B), cobrança por consumo real (1B), dívida residencial sem corte (3B) e foco do jogador em capacidade de água/energia.

### Inadimplência empresarial em água e energia — 2A aprovada

**Decidido:** a empresa deve ao Caixa da Cidade por água e energia efetivamente consumidas. Falta de pagamento não gera receita nem corte individual deliberado no primeiro modelo, mas a dívida real piora sua situação financeira e pode contribuir para falência pelas regras normais. Pagamentos parciais abatem dívida; fornecimento continua sujeito à capacidade e aos recursos físicos da cidade, sem garantia sistêmica. **Em aberto:** periodicidade, prioridade de pagamentos e liquidação de débitos empresariais quando há encerramento.

**Situação que motivou a escolha:** uma fábrica segue consumindo energia, mas não tem caixa para quitar a fatura municipal. **Alternativas históricas:**

- **A — dívida empresarial real sem corte por fatura isolada:** o valor não pago permanece como obrigação com o Caixa da Cidade, comprometendo o resultado/solvência da empresa e podendo culminar no encerramento pelas regras gerais; a prefeitura não registra receita inexistente. **Opção aprovada na SPEC:** evita sistema novo de suspensão/restabelecimento por empresa e usa insolvência já existente; requer que a dívida influencie realmente sua viabilidade, sem serviço eternamente gratuito.
- **B — dívida e corte do fornecimento por inadimplência persistente:** após prazo de tolerância, o estabelecimento perde fornecimento e capacidade produtiva até regularização. Consequência diretamente visível, mas exige regra de prazo, religação, pagamento parcial, ordem de corte e mais estados internos.
- **C — cobrança antecipada/prepaga para empresa:** falta de saldo impede consumo futuro. Evita dívida crescente, mas antecipa transações, modifica o fluxo aprovado de cobrança por consumo efetivo e pode interromper produção abruptamente.

**Risco:** A pode esconder empresas inviáveis se os débitos não afetarem insolvência; B/C multiplicam casos de operação e transições. Em qualquer alternativa, escassez física de geração continua separada de inadimplência, sem criar nem destruir dinheiro ou energia.

### Política salarial dos trabalhadores municipais — 3A aprovada

**Decidido:** salários dos trabalhadores municipais são ajustados automaticamente e gradualmente, considerando competição por SIMs reais, dificuldade de preencher/reter vagas e dinheiro efetivo da prefeitura. O jogador não administra salário por serviço; pagamentos saem do Caixa e precisam ser explicáveis no orçamento. Sem pessoal ou dinheiro, a capacidade operacional sofre consequências reais; não existem contratações fictícias nem aumentos infinitos. **Em aberto:** parâmetros e frequência de ajuste, não o princípio automático.

**Situação que motivou a escolha:** escola/hospital/serviço de água precisa contratar SIMs reais, enquanto empresas privadas disputam esses mesmos trabalhadores. **Alternativas históricas:**

- **A — remuneração municipal ajustada automaticamente:** valor de referência reage de modo gradual à dificuldade real de contratar/reter, às alternativas de emprego e aos recursos do Caixa. **Opção aprovada na SPEC:** evita gerenciamento de folhas por serviço e preserva concorrência real; exige limites/diagnóstico para não crescer automaticamente além do orçamento.
- **B — política salarial agregada escolhida pelo jogador:** um controle geral (por exemplo, econômica/equilibrada/competitiva) influencia salários municipais, contratação e custos. Cria uma alavanca fiscal estratégica, mas é mais uma obrigação de administração e requer retorno claro da interface.
- **C — salários estáveis por tipo de serviço:** valores de referência pouco adaptativos e calibrados. Previsível e barato de simular, mas pode manter serviços sem funcionários mesmo havendo recursos para competir no mercado.

**Risco:** salários não podem atrair funcionários fictícios nem permitir pagamento sem caixa. Qualquer política preserva um emprego por SIM, vagas reais, turnos e impactos orçamentários. Não definir salários municipais pela política privada sem decisão explícita.

---

## Capacidade hospitalar e equipe médica

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração; existência de funcionários reais e capacidade real já está decidida na SPEC.

A direção mais coerente é evitar um hospital com capacidade puramente abstrata.

Modelo a investigar:

- cada hospital/unidade possui um quadro real de funcionários;
- médicos, enfermeiros e outros profissionais podem ter funções diferentes;
- capacidade de atendimento depende da equipe disponível, instalações e tempo;
- leitos podem ser um recurso separado da capacidade de consulta;
- turnos podem reduzir a equipe disponível em determinados horários;
- ausência de profissionais pode reduzir capacidade mesmo quando o prédio físico comportaria mais pacientes;
- emergências, consultas e internações podem competir por recursos diferentes.

### Pergunta em aberto

Ainda precisa ser decidido o nível de granularidade da equipe:

1. apenas "médicos" e "enfermeiros";
2. especialidades médicas relevantes;
3. funções hospitalares adicionais;
4. turnos e escalas individuais.

A regra de profundidade continua a mesma: só detalhar quando isso gerar consequência clara para gameplay, capacidade, custo, deslocamento ou decisão do jogador.


---

---

### Decisão transversal: escolha do SIM quando emprego muda de endereço (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou explicitamente a **opção A**, reavaliação automática para trabalhadores **privados e públicos**; a regra de produto foi registrada na SPEC. **Em 2026-10-09, o responsável também aprovou manter os vínculos e salários municipais durante paralisação temporária pela realocação (opção A)**, desde que vagas/instituição persistam e o Caixa possa custear, sem obrigar o SIM a continuar. Apenas a logística e parâmetros técnicos continuam em exploração.

**Confirmado na SPEC:** quando ocorre mudança efetiva do endereço de trabalho, cada SIM afetado reavalia automaticamente a permanência no emprego, considerando novo trajeto/tempo de viagem, transporte, salário, turno e oportunidades reais. Sem demissão coletiva automática, retenção obrigatória ou seleção manual pelo jogador. **Só é possível continuar se empregador e vaga persistirem**; se o SIM sair, apenas uma vaga que ainda exista volta a ficar disponível para contratação normal. Aplicar a decisão pelos mecanismos normais do mercado de trabalho e em resposta ao evento de mudança, sem varredura contínua por todos os cidadãos. O resultado deve afetar capacidade real de escola, hospital ou outro estabelecimento e produzir deslocamento/efeitos sociais legíveis.

**Continuidade operacional condicionada agora aprovada na SPEC (opção C):** quando uma escola, hospital ou outro serviço municipal for realocado, seu prédio antigo **pode continuar prestando o serviço** enquanto existir, funcionar e não precisar liberar a área da intervenção; se a retirada antecipada for necessária, o atendimento/capacidade daquele endereço é interrompido. A nova estrutura exige obra e condições reais, sem transferência física instantânea nem serviço garantido só porque o destino foi escolhido. **Continuidade institucional agora decidida na SPEC (opção C, 2026-10-08):** escola, hospital e demais serviços municipais permanecem sendo a mesma instituição durante a realocação, sem recriação institucional ou compra privada; capacidade e atendimento só existem onde estrutura, funcionários, acesso e recursos de fato permitirem. Vínculos/vagas seguem válidos apenas se realmente existirem; funcionários reavaliam individualmente quando o endereço de trabalho mudar. **Regra aprovada posteriormente (2026-10-09, opção A):** a interrupção temporária não suspende nem extingue automaticamente os vínculos e vagas municipais; salários permanecem devidos e são pagos pelo Caixa da Cidade enquanto existirem vínculos/vagas e recursos efetivos, sem produção ou atendimento fictício. Falta de orçamento segue as regras existentes de priorização e perda de capacidade; não inventar pagamentos, demissões coletivas nem suspensão especial. **Os SIMs continuam independentes:** podem mudar de emprego normalmente e devem reavaliar individualmente a permanência na mudança efetiva do endereço, se o vínculo e a vaga existirem. **Em aberto, sem autorização para suposição:** capacidade de atendimento temporário, destino operacional fino dos suprimentos, eventual logística de equipamentos não especificados e critérios finos de transição. A recuperação automática de estoques físicos quando viável e perda do remanescente na retirada inevitável, sem nova indenização ou capacidade fictícia, seguem a SPEC e não exigem novas regras de mercado para serviços públicos. Continuidade institucional não garante operação ininterrupta nem serviço fictício.

## Passageiro sem dinheiro em transporte municipal pago — opção 4A (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou que um SIM **não embarca em transporte pago sem saldo suficiente**, precisando caminhar se houver trajeto viável; a regra consta na SPEC. Forma da tarifa continua em pesquisa.

Sem cobrança negativa, dívida de passagem, isenção automática nem receita de viagem não realizada. Quando transporte for gratuito por decisão do jogador, não exigir passagem. Falta de dinheiro pode inviabilizar acesso ao trabalho/serviços; SIMs precisam tomar decisões de deslocamento reais e diagnosticáveis, nunca teletransportar. Caminhada pode ser a solução se existir caminho acessível, sem garantir que qualquer distância seja percorrível.

### Direção oficial atualizada: oferta de água/energia é decisão de capacidade (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou preço automático 2B e inadimplência residencial 3B, e reforçou o foco de gameplay em **dimensionar quantas estruturas de geração, captação e tratamento construir para suprir a cidade**, não em controlar tarifas. A regra correspondente foi promovida à SPEC.

O jogador constrói/amplia capacidade real, pagando obras e manutenção; demanda de famílias, empresas e serviços confronta **capacidade instalada versus capacidade efetiva**. Custo por ociosidade, escassez, atendimento insuficiente e incapacidade do orçamento de manter infraestrutura devem aparecer no painel. O sistema calcula operação/fornecimento sob limites físicos, sem meta manual por usina/estação. **Tarifa automática deriva de referência operacional estável**, não converte o custo inteiro de uma usina quase vazia numa conta exorbitante de um único cidadão. Contas não pagas são créditos registrados, **não caixa recebido**.

**Possível melhoria posterior (NÃO APROVADA):** política agregada de subsidiar, buscar equilíbrio dos custos ou pequeno excedente, em limites explicáveis e sem preços livres; só reavaliar após feedback sobre o loop de expansão e a economia.

---

### Pesquisa: preço justo e inadimplência das utilidades — PENDENTE (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **aprovou depois as opções 2B (preços automáticos) e 3B (dívida real sem corte residencial individual)**, hoje registradas na SPEC. As alternativas A/C abaixo são **histórico de pesquisa NÃO APROVADO**; a opção de controlar políticas de subsídio/cobertura/excedente é **somente sugestão para possível evolução futura**. Tempo/calendário continua em pesquisa independente.

**O que já está aprovado:** água e eletricidade cobradas conforme consumo real de famílias/empresas com pagamento ao Caixa (1B); transporte municipal gratuito ou pago pelo jogador (2C) e **SIM sem saldo não embarca na viagem paga** (4A). A SPEC define para **aluguel**, não para utilidades: dívida real ao credor, prazo de três meses até possível perda de moradia, pagamentos parciais, recuperação gradual por desconto em salário recebido. **Não copiar o prazo e o despejo para água/energia.**

**Evidência útil:**
- O regulador britânico [Ofgem — como funciona o teto da tarifa de energia](https://www.ofgem.gov.uk/your-energy-supply/your-energy-bill/energy-price-cap-and-standing-charges-explained) vincula teto de preços a categorias de custos reais e o revisa periodicamente. **É referência conceitual**, não parâmetro ou lei do jogo.
- O [Banco Mundial — Troubled Tariffs (2021)](https://documents.worldbank.org/en/publication/documents-reports/documentdetail/568291635871410812) e [Doing More with Less](https://www.worldbank.org/en/topic/water/publication/smarter-subsidies-for-water-supply-and-sanitation) apontam o conflito entre cobertura dos custos, acessibilidade e eficiência; subsídio sem alvo pode beneficiar quem não precisa. **Preço descontrolado de utilidade essencial pode explorar demanda pouco elástica, não apenas baixar consumo.**
- [ANEEL — regras de corte da energia](https://www.gov.br/aneel/pt-br/consumidores/como-resolver) prevê aviso em inadimplência, sem corte imediato; [ANA — interrupção de água por inadimplência](https://www.gov.br/ana/pt-br/legislacao/resolucoes/resolucoes-regulatorias/2024/230/) também exige comunicação prévia. [Ofgem — dívida de energia](https://www.ofgem.gov.uk/policy/debt-strategy-update-supporting-reduction-energy-debt) ressalta o risco de serviços essenciais e a raridade de corte. **Referências de princípios, não obrigação de reproduzir a regulação real no jogo.**

**Pergunta 2 — preços de água/energia e passagem paga:**
- **A — custo de referência com três políticas agregadas:** a simulação deriva um **preço-base da operação real** com valores estáveis e sem divisão absurda quando houver poucos usuários. O jogador escolhe **subsidiar, equilibrar ou gerar pequeno excedente** dentro de uma **faixa limitada e justificada pelos custos**. Água e energia têm políticas separadas; transporte segue gratuito/pago e o valor pago usa referência simples, não leilão. Efeito previsto antes da escolha: cobertura real, contas/viagens pagáveis e arrecadação provável. **Recomendação da IA:** decisões urbanas claras e sem estratégia de tarifa ilimitada; exige apenas parâmetro agregado por serviço.
- **B — preços automáticos estritamente pelo custo:** equilíbrio mais simples e sem abuso, mas quase retira decisão fiscal significativa do jogador.
- **C — preço manual livre:** amplia liberdade, mas **não garante equilíbrio**: com demanda essencial pouco elástica, elevar ao máximo pode render até inadimplência; exige complexa reação social/limites para não virar exploração de gameplay.

**Guardrails da opção A:** não criar lucro automático por tarifa ou receita de conta não paga; preço deve ser por unidade **efetivamente utilizada**, e a arrecadação só ocorre com transferência de dinheiro existente. Cobertura de custo pode ser incompleta (déficit real); custo-base não pode subir indefinidamente porque a utilização caiu. Respostas de economia/atração devem resultar de fatos (caixa familiar, lucro empresarial e oferta efetiva), não debuffs artificiais.

**Pergunta 3 — inadimplência de água/energia:**
- **A — mesmo modelo do aluguel, com prazo e suspensão:** dívida real e cobrança gradual, após tolerância há interrupção. Reaproveita bastante lógica, mas cortar água/energia em residência pode gerar **espiral de necessidades** e exigir regra de restabelecimento, domicílios vulneráveis e dezenas de exceções.
- **B — dívida real, cobrança gradual, sem corte residencial no primeiro modelo:** registrar obrigação devida ao **Caixa**, abater pagamentos efetivos e recuperar parte ao receber salário **reutilizando a rotina simples de cobranças do aluguel, sem misturar os credores**. Se não puder pagar, o Caixa **não recebe**, podendo ter dificuldade real para operar serviço e financiar infraestrutura. Mostrar agregado do déficit; **sem corte individual de residência**. **Recomendação da IA para a primeira versão:** privilegia gameplay e custo de simulação, mas implica **risco de dívida crescente**; validar se o déficit público e a falência privada dão consequência suficiente, sem simulação de devedor por frame.
- **C — dívida real e redução progressiva de consumo não essencial por inadimplência persistente:** preserva uso doméstico básico e dificulta uso ilimitado sem pagar, mas exige **distinguir consumo essencial do supérfluo**, vincular restrição a medição, decidir durações e exceções. Mais caro de desenvolver; pode ser evolução após observar B.

**Agora decidido (2B/3B):** os valores são **automáticos**, sem controle tarifário direto do jogador; cobrança de água/energia por consumo real e passagem paga por viagem real; conta residencial inadimplida gera dívida real com a prefeitura, recuperação gradual quando houver dinheiro e **sem corte individual da residência por inadimplência no primeiro modelo**. **Continuam pendentes:** parâmetros do preço/custo, titular da conta na locação, periodicidade, múltiplas dívidas, priorização entre credores, detalhes da cobrança empresarial 2A e eventual reavaliação futura de políticas de subsídio. **Não aplicar por inferência os três meses do aluguel, cobrança judicial, desligamento individual, isenção automática ou subsídio sem fonte monetária.** A prioridade geral de necessidades essenciais e conservação monetária permanecem na SPEC.

---

## Cobrança de serviços municipais e opção de tarifa de transporte (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Além de 1B e 2C, foram **aprovadas 2B (valor automático de água, energia e passagem paga)** e **3B (dívida residencial sem corte individual)**. Apenas parâmetros concretos, operacionalização da dívida empresarial 2A e modalidades externas seguem em estudo.

**Água/energia 1B:** domicílios e empresas devem pagar **proporcionalmente ao consumo real dos sistemas municipais**, por transferências de dinheiro existente **ao Caixa da Cidade**, com apuração agregada e sem microgerenciar medidores. Isso não transforma água ou eletricidade em mercadoria estocável por SIM, não isenta a prefeitura de custos de operação nem estabelece cobrança adicional para lixo/esgoto por inferência. **Preço unitário é automático (2B); inadimplência residencial gera dívida real sem corte individual (3B).** Periodicidade, conta em locação, recuperação gradual e regras empresariais precisam ser calibradas/definidas sem inventar juros ou corte imediato.

**Transporte público 2C:** a prefeitura/jogador define **gratuito versus pago** para o transporte municipal, em controle agregado; serviço grátis continua consumindo dinheiro e recursos reais. No regime pago, receita decorre **somente de passageiros/viagens efetivas** e transfere dinheiro do SIM ao Caixa, não do veículo vazio. A política pode mudar a escolha de rota/meio de viagem por acessibilidade e custo; **valor exato e forma técnica de cobrança** continuam para calibração; **a tarifa é automática (2B) e passageiros sem saldo não embarcam (4A)**. Não assumir automaticamente tarifa para prestadores privados ou modais fora da prefeitura.

## Um vínculo empregatício ativo por SIM — opção 4A (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou **um emprego ativo por SIM na versão inicial** e solicitou **guardar a possibilidade de dois empregos como melhoria futura**, não requisito.

SIM pode aceitar uma única vaga ativa por vez; vínculos de empresas locais, serviços públicos e trabalho fora da cidade respeitam o limite, sem pagamento duplicado nem turnos simultâneos. Mudança de empregador requer encerrar vínculo anterior. A hipótese de **dois empregos conciliados por turno, disponibilidade e deslocamento** é **PENDENTE para evolução futura**, avaliando impactos sobre descanso, rotinas, calendário ainda em pesquisa e performance — **não desenvolver antecipadamente**.

---

## Contratação gradual sujeita ao porte do estabelecimento — opção 2B (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou contratação e redução gradual pela demanda, com **teto físico de pessoal segundo o tamanho/capacidade do negócio**. Parâmetros exatos permanecem para calibração.

O número de vagas e pessoas simultaneamente trabalhando fica limitado pelo edifício e sua capacidade real. Contratar mais turnos não aumenta a capacidade simultânea, embora permita cobertura efetiva em horários diferentes. Receita/caixa não cria vagas sem espaço, nem emprego sem SIM ou salário real. Ajustes por período ou evento significativo, sem contratação/demissão a cada oscilação, sem aprovação manual do jogador e sem varreduras a cada quadro.

---

## Turnos de trabalho e operação contínua

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status atualizado (2026-10-08):** múltiplos turnos e **horários simples de atendimento por tipo de negócio (1A)** estão aprovados na SPEC; horas exatas, cobertura real por SIMs e parâmetros continuam para calibração. As hipóteses seguintes não autorizam gestão manual de escalas.

A intenção é permitir turnos diferentes para empresas e serviços, inclusive quando houver operação noturna, mas sem transformar escala de funcionários em microgerenciamento manual obrigatório.

Direção a validar:

- cada trabalhador pode ter um horário/turno individual;
- **regra oficial:** cada tipo de negócio tem janela habitual simples; sua operação real depende de pessoas presentes em turnos, sem otimizador permanente por empresa;
- serviços críticos podem precisar de cobertura contínua;
- a simulação pode gerar escalas automaticamente a partir das necessidades da empresa/serviço;
- **horários e escalas individuais não são configurados pelo jogador na opção 1A**; ele age sobre capacidade e condições urbanas já previstas;
- horários diferentes devem distribuir ou concentrar tráfego ao longo do dia.

O objetivo é obter consequências reais de horário e escala sem obrigar o jogador a montar manualmente cada escala de trabalho, salvo se isso se provar divertido em protótipo.


---

---

## Transporte público, estacionamento e combustível

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Transporte público

Já está decidido que o sistema não ficará restrito a ônibus.

Princípios já definidos:

- veículos de transporte público são entidades reais;
- linhas têm percurso real;
- capacidade importa;
- funcionários/motoristas reais fazem parte da operação;
- outros modais além de ônibus devem existir.

Ainda precisa ser explorado quais modais entram primeiro, por exemplo:

- ônibus;
- vans/micro-ônibus;
- bonde/VLT;
- metrô;
- trem suburbano;
- táxi/transporte sob demanda.

A seleção deve considerar escala da cidade e custo de simulação, não apenas catálogo de features.

### Estacionamento

O estacionamento será parte real da mobilidade.

Questões em aberto:

- estacionamento na rua;
- vagas privadas em residências e empresas;
- estacionamentos públicos;
- custo de estacionamento;
- tempo de procura por vaga;
- efeito da falta de vagas no trânsito e na escolha modal.

O objetivo é manter alto realismo de veículos sem transformar estacionamento em microgestão excessiva para o jogador.

### Cadeia de combustível

Decisão de direção:

produção/refino → distribuição → postos → consumo por veículos.

Pontos a explorar:

- origem da matéria-prima;
- refinaria/fábrica como unidade produtiva;
- transporte por caminhões-tanque;
- estoque real nos postos;
- preço do combustível;
- impacto de falta de combustível na mobilidade e economia;
- consumo diferente por tipo de veículo.

O sistema deve se conectar à logística já decidida, em vez de funcionar como recurso abstrato isolado.

---

---

## Funcionalidades adiadas

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** fora do escopo atual, mas preservadas para reavaliação futura.

- assistência social municipal, incluindo abrigos e programas de apoio — **explicitamente fora do primeiro modelo** após escolha do escopo inicial para população sem moradia; melhorias futuras permanecem em exploração, sem implementação aprovada (ver `households-housing.md`);
- saúde mental e dependência química.

Esses tópicos não devem ser implementados agora. Podem ser revisitados futuramente quando os sistemas básicos de população, saúde, moradia e orçamento já estiverem maduros.


---

---

## Mobilidade ativa e realismo de deslocamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

Já está decidido que pedestres e bicicletas existem fisicamente na cidade.

### Pedestres

A intenção é de alto realismo de deslocamento:

- cidadãos caminham fisicamente entre origem e destino;
- viagens multimodais podem incluir trechos a pé;
- cidadãos podem caminhar até estacionamento, ponto de ônibus, estação, comércio, escola e trabalho;
- o caminho de pedestres precisa respeitar calçadas, travessias e acessibilidade viária.

O nível exato de animação, detecção de obstáculos e priorização de travessias ainda precisa ser prototipado.

### Bicicletas

Decidido:

- bicicletas circulam fisicamente;
- bicicleta é um modal real;
- ciclovias fazem parte da infraestrutura.

Adiado para o futuro:

- estacionamento específico para bicicletas.

### Estoque real no comércio

Foi reforçada a regra de que comércio não possui estoque meramente decorativo.

Ela vale para:

- postos de combustível;
- padarias;
- farmácias;
- mercados;
- demais comércios baseados em bens.

**Decidido na SPEC:** compras de bens de consumo pelos cidadãos exigem **visita física a um comércio**, usando os deslocamentos reais de pedestres e/ou veículos, inclusive fora da câmera. O SIM compra apenas produtos de categorias disponíveis no estoque com pagamento real; o estoque diminui e o dinheiro entra no caixa da empresa operadora. A visita de compra é automática, sem teletransporte ou transação invisível que dispense o trajeto.

**Decidido na SPEC para escolha de comércio:** a seleção automática considera **preço, distância/acessibilidade e estoque disponível**. Se a loja não puder suprir a compra, o SIM pode tentar outra opção acessível e deve percorrer a rota real; se nenhuma for viável, a necessidade não é atendida. Isso não exige manter uma loja habitual nem tornar a busca global e contínua. A escolha de outro comércio preserva a concorrência local e os efeitos de ruptura de estoque.

A granularidade inicial por **categorias de produtos** já foi decidida na SPEC; a subdivisão concreta dessas categorias, a política de reposição por estabelecimento, a frequência/agrupamento de compras, os pesos da seleção e o limite de alternativas permanecem para calibração/protótipos, com atenção ao custo em larga escala.

---

---

## Funcionalidades adiadas de mobilidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** fora do escopo atual.

- acidentes de trânsito com colisões, feridos e resposta emergencial;
- manutenção mecânica e quebra de veículos;
- estacionamento específico para bicicletas.

Esses sistemas podem ser revisitados depois que mobilidade básica, trânsito, estacionamento de carros e logística estiverem estáveis.


---

---

## Vias, travessias e controle de tráfego

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Tipos de via iniciais

Decidido para o escopo atual:

- via urbana comum;
- rodovia.

A via urbana comum terá calçada por padrão.

Novas categorias de via, perfis, larguras, faixas exclusivas e outras variações podem ser adicionadas depois, caso se provem necessárias.

### Estacionamento

Decidido:

- estacionamento na rua ocupa espaço físico real;
- edificações podem oferecer vagas privadas;
- vagas privadas reduzem pressão por estacionamento público.

### Travessias de pedestres

Decidido no escopo atual: pedestres atravessam vias urbanas somente em faixas.

### Semáforos

Decidido no escopo atual: semáforos usam ciclos fixos. Controle adaptativo pode ser reconsiderado futuramente.


---

---

## Função da rodovia e hierarquia viária

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção confirmada parcialmente.

A rodovia será tratada como infraestrutura de mobilidade de alta capacidade, não como via de acesso local.

Implicações já definidas:

- maior velocidade operacional;
- sem construção direta de edifícios ao longo da rodovia;
- acesso à cidade feito por conexões apropriadas com a malha urbana;
- capacidade depende do número de faixas;
- rotatórias e cruzamentos fazem parte da rede urbana.

### Razão de design

Separar rodovia de via urbana ajuda a manter uma hierarquia viária compreensível:

- rodovia: deslocamento rápido entre áreas;
- via urbana: acesso local, calçadas, travessias e estacionamento.

Isso também reduz um problema comum em redes viárias: misturar tráfego local com tráfego de passagem na mesma infraestrutura.

### Travessias

Decidido:

- pedestres atravessam somente em faixas.

Ainda pode ser explorado futuramente:

- semáforo para pedestres;
- tempo de espera;
- prioridade de pedestres;
- travessias elevadas ou passarelas.

### Semáforos

Decidido para o escopo inicial:

- ciclos fixos.

Controle adaptativo pode ser reconsiderado no futuro se congestionamentos e gameplay justificarem a complexidade.


---

---

## Granularidade operacional de serviços

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção parcialmente fechada.

A simulação deve evitar ações instantâneas quando isso destruir causa e consequência, mas também não precisa reproduzir cada segundo do mundo real.

Direção atual:

- carga/descarga leva tempo;
- coleta de lixo leva tempo;
- atendimento de emergência leva tempo;
- embarque/desembarque leva tempo;
- interior dos prédios permanece abstrato;
- entradas e saídas continuam físicas e observáveis.

A duração exata deve ser configurável e calibrada durante os testes de ritmo do jogo.

### Entregas comerciais locais: fornecedor executa o transporte

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — a opção de **entrega pelo fornecedor** foi aprovada pelo responsável; parametrização, disponibilização de frota inicial e custo de execução seguem pendentes.

**Decidido na SPEC para a primeira implementação integrada:** fazendas, indústrias e demais fornecedores locais organizam suas entregas comerciais com caminhões sob sua operação e motoristas SIM reais. Os veículos carregam produtos na origem e se deslocam fisicamente ao comércio comprador, usando capacidade, vias, trânsito, estacionamento/ponto de descarga e duração de entrega reais. Não transferir o estoque ao comprador antes da chegada; não exigir que cada comércio tenha caminhão próprio para buscar compras nem implementar transportadoras independentes obrigatórias. Ausência de capacidade logística pode atrasar entregas. Os custos de frete permanecem relevantes no preço/custo total já decidido, sem inventar serviço ou cobrança fictícia. Importações usam motoristas/veículos externos conforme a SPEC, e entregas de material para obras preservam suas regras existentes.

**Pendente de calibração/prototipagem:** como empresas obtêm e alocam caminhões/motoristas, limites mínimos do bootstrap, manutenção/frete/faturamento, acúmulo de pedidos, agrupamento e rotas. Avaliar o custo de simulação e os efeitos sobre disponibilidade sem bloquear toda a cadeia por uma burocracia logística excessiva.

### Entregas sem janelas artificiais

Não haverá, por enquanto, obrigação de janelas horárias fixas de entrega para comércio.

A logística deve emergir principalmente de:

- disponibilidade de estoque;
- necessidade de reposição;
- disponibilidade de veículos;
- capacidade de carga;
- distância;
- trânsito;
- fila/capacidade no ponto de carga e descarga.

Se no futuro restrições de horário gerarem gameplay útil, elas podem ser reavaliadas.


---

---

## Filas físicas e horários de funcionamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status revisado:** filas físicas detalhadas continuam futuras; **horários básicos de funcionamento já foram aprovados posteriormente como opção 1A na SPEC**. A indicação histórica de adiamento não se aplica mais aos horários.

### Filas físicas em comércio e saúde

No futuro pode ser útil representar filas físicas quando a capacidade de um comércio, hospital ou outro serviço for excedida.

Possíveis consequências:

- espera visível;
- ocupação de calçadas/espaço externo;
- impacto em satisfação;
- atraso em atendimento;
- incentivo para ampliar capacidade.

Não é necessário implementar isso agora.

### Horários de funcionamento

**Horários básicos foram decididos pela opção 1A:** estabelecimentos têm janelas por tipo de negócio, só atendendo quando efetivamente operacionais, com funcionários e acesso reais.

Possíveis efeitos:

- concentração de viagens em certos horários;
- necessidade de turnos;
- indisponibilidade temporária de serviços;
- redistribuição de demanda.

**Detalhes adicionais de otimização contínua de horários e filas físicas seguem em pesquisa;** horários simples e múltiplos turnos básicos já constam na SPEC.


---

---

## Eventos temporários e turismo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** eventos temporários adiados para o futuro; turismo básico já está decidido na SPEC.

No futuro, eventos como shows, feiras e festivais podem ser usados para gerar picos temporários de:

- visitantes;
- demanda por hotéis;
- trânsito;
- transporte público;
- comércio;
- segurança e serviços urbanos.

Esses eventos não entram no escopo atual. Devem ser revisitados depois que turismo, mobilidade, hotelaria e capacidade dos serviços estiverem estáveis.


---

---

## Capacidade educacional

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** estrutura decidida; parâmetros ainda em exploração.

A escola não usará apenas um bônus abstrato de "capacidade".

Já está decidido que:

- professores são cidadãos reais;
- alunos são cidadãos reais;
- quantidade de professores e quantidade de alunos importam;
- falta de vaga/acesso pode deixar jovens fora da escola;
- universidade faz parte do sistema de educação e qualificação.

Ainda precisa ser pesquisado e calibrado:

- razão professor/alunos;
- capacidade física por escola;
- necessidade de salas/turmas explícitas ou agregadas;
- duração das etapas de ensino;
- efeito da distância e transporte no acesso;
- relação entre nível educacional, empregos e salário.

Esses valores devem ser baseados em dados reais ou benchmark do jogo, não escolhidos arbitrariamente.

---

---

## Turismo e atratividade efetiva — opção 2A aprovada (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou turismo causado pelas condições reais da cidade; fórmulas e taxas de demanda são para calibração.

A chegada de turistas depende da combinação **real e verificável** de hospedagem com capacidade, lazer, comércio/serviços, atrações, acesso viário/transporte pela conexão exterior e condições urbanas. **Não** gerar chegada aleatória independente da cidade nem encher hotel só porque foi construído. Turistas são agentes/viagens reais, consumindo serviços e pagando com dinheiro existente quando efetivamente atendidos. Pernoite exige hospedagem com vaga; a visita de curta duração e a ponderação das atrações ainda são aspectos a definir/calibrar. Mostrar razões de procura/ausência quando relevantes sem criar índice mágico impossível de explicar.

---

## Trabalho pendular bidirecional — opção 4C (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável escolheu C, com preocupação explícita sobre custo técnico e equilíbrio. Direção aprovada na SPEC; modelagem do exterior e dos pagamentos continua aberta para validação.

**Evidência:** o [IBGE — Censo 2022, divulgado em 2025](https://educa.ibge.gov.br/jovens/materias-especiais/23064-censo-2022-como-a-populacao-se-desloca-para-estudar-e-trabalhar.html) identifica **9,3 milhões de pessoas (10,7% dos ocupados) trabalhando em município distinto da residência**, sendo 7,9 milhões com deslocamento três ou mais dias por semana. Essa proporção não é parâmetro aprovado do jogo.

**Benefício para gameplay:** residentes podem trabalhar fora quando há emprego externo realmente acessível, e empresas locais podem contratar trabalhadores externos quando há vagas reais. A entrada e saída pela conexão externa criam tráfego e condições de acesso, sem obrigar o jogador a construir uma segunda cidade. Não contar trabalhadores de fora como moradores nem esconder desemprego dos residentes.

**Opção 4A aprovada depois (2026-10-08):** trabalhadores que residem fora têm **identidade individual e vínculo de emprego persistentes, remuneração e turno coerentes**, com viagens **físicas dentro do mapa** e **vida, casa, família e economia fora do mapa não simuladas detalhadamente**. A opção de empregados externos anônimos foi descartada; **a simulação de uma segunda cidade inteira também não está aprovada**. Um mesmo pendular não pode ocupar vínculos incompatíveis ou preencher automaticamente toda escassez de vagas. **Ainda em pesquisa/calibração:** limite da oferta externa, persistência técnica off-map e save, ritmo do deslocamento (dependente do modelo temporal ainda não decidido) e representação de dinheiro/carteira sem duplicar Reserva Global nem pagamentos. Evitar estado de uma cidade externa e atualização global de agentes a cada frame.

## Manutenção municipal recorrente — opção 5B (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou despesas recorrentes reais e leitura agregada, sem reparos manuais. Fórmulas, serviços/destinatários e tratamento de falta de recursos continuam pendentes.

Expandir rua, energia, água e outras redes traz custos permanentes para o **Caixa da Cidade**, além da obra inicial. A **arrecadação de impostos já especificada** ajuda a financiar os custos, mas nunca sobe automaticamente para cobri-los: gastos excessivos podem gerar déficit, empréstimo e pressão para ajustar impostos. O painel financeiro precisa distinguir manutenção de salários e outras operações, sem cobranças duplicadas.

**Regra anti-ficção:** valor cobrado só sai do Caixa se houver causa e destinatário econômico real: trabalho público que já não tenha sido remunerado, insumos, prestação local ou serviço externo efetivamente devido (com conciliação via Reserva Global). Não criar imposto fictício ou pagamento para ninguém e não inventar falhas de manutenção por rua. Calibrar periodicidade, valor e dependência da capacidade/extent.

**Resposta à falta de recursos — opção 5B aprovada em 2026-10-08:** quando o Caixa não permite pagar trabalhadores, insumos ou atendimento de manutenção indispensáveis, **a infraestrutura afetada pode perder eficiência/capacidade e, se persistir a falta real de recursos ou pessoal, parar de prestar o serviço**. **Não há falha generalizada instantânea** nem degradação sorteada por prédio, nem serviço ficticiamente realizado sem pagamento. O jogador enxerga déficit, recurso faltante, capacidade atingida e possibilidade de recuperação; a restauração depende de condições e dinheiro reais, não de bônus automático. **Priorização automática 5A aprovada na SPEC (rodada seguinte de 2026-10-08):** diante de verba ou recursos insuficientes, a simulação procura **preservar primeiro o funcionamento viável de serviços essenciais e suas dependências críticas**, sem exigir que o jogador escolha verba por prédio. Exemplos para estudo incluem água, energia, saúde e emergências, mas **a hierarquia exata não está decidida**. Recursos, equipes e dinheiro precisam existir: proteger um serviço essencial não garante operação se nada puder ser pago/fornecido. O orçamento/diagnóstico agregado mostra o que foi protegido e o que perdeu capacidade. A classificação das dependências, limiares e recuperação seguem **pendentes de definição/calibração** antes do código dependente. Referências: [EPA — asset management](https://www.epa.gov/dwcapacity/about-asset-management) e [Paradox — Economy 2.0](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-one), apenas para comparação.

---

## Conexão externa

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** conceito decidido; representação física ainda em exploração.

A conexão externa representa o "mundo fora do mapa".

Ela deve sustentar, conforme os sistemas forem implementados:

- entrada e saída de turistas;
- imigração e emigração;
- transporte rodoviário externo;
- transporte público/interurbano;
- entrada e saída de cargas;
- possíveis outros modais externos no futuro.

A conexão externa evita criar agentes ou mercadorias no meio do mapa sem origem observável.

Ainda precisa ser definido se haverá um único ponto físico, múltiplos pontos por modal ou uma camada lógica comum conectada a diferentes terminais.


---

---

## Infraestrutura de água, esgoto e energia

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Água e esgoto

Decidido:

- captação física a partir de rio/lago;
- tratamento de água;
- geração real de esgoto;
- tratamento de esgoto;
- demanda e capacidade importam;
- o jogador não precisa desenhar manualmente encanamentos no escopo atual.

Direção de modelagem:

- edifícios consomem capacidade hídrica;
- infraestrutura construída aumenta capacidade de captação/tratamento;
- excesso de demanda pode gerar falta d'água ou esgoto sem atendimento;
- cobertura pode ser modelada de forma agregada ou por área, sem rede de tubos explícita.

**Direção revisada em 2026-10-09:** água e energia usarão a **mesma lógica de distribuição**, com localização pouco penalizadora quando houver capacidade suficiente e preferência espacial leve/gradual quando houver escassez. A versão de corte rígido das regiões mais distantes foi reconsiderada; **modelo global com índice de atendimento por área foi APROVADO (2026-10-09)**. Algoritmo, proximidade com múltiplas fontes e consequências exatas ficam para definição/validação.

### Energia

Decidido:

- geração real;
- demanda real;
- capacidade limitada;
- não exigir desenho manual detalhado de linhas/rede elétrica no escopo atual;
- solar e eólica fazem parte das opções.
- **Distribuição usa a mesma direção simplificada da água**, decidida em 2026-10-09, mantendo geração/consumo independentes por recurso. O **modelo de atendimento regional leve em escassez está aprovado**; graduação numérica e agrupamento espacial ficam para calibração. Não impor rede elétrica manual ou penalidade espacial permanente pesada.

Ainda em exploração:

- carvão;
- nuclear;
- regras de custo, combustível, poluição e capacidade por tipo de usina.

### Hidrelétrica

**Adiada para o futuro.**

Como hidrelétrica depende fortemente de relevo, curso d'água, barragem e alteração física do terreno, ela deve ser reconsiderada depois que terreno, água e simulação ambiental estiverem mais maduros.

### Poluição

Já está decidido que poluição deve vir de fontes reais da cidade e afetar sistemas reais.

Ainda precisa ser definido:

- tipos de poluição (ar, água, solo, ruído);
- raio/dispersão;
- relação com saúde;
- impacto em valor imobiliário;
- efeito de vento, relevo ou fluxo de água.


---

---

## Resíduos e degradação por falta de água

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Resíduos

Decidido:

- lixo precisa ter destino físico;
- aterro e/ou incineração fazem parte do sistema;
- capacidade de processamento importa;
- reciclagem fica fora do escopo inicial.

Ainda precisa ser definido:

- tipos de instalação iniciais;
- custos operacionais;
- impacto ambiental;
- distância/logística de coleta;
- capacidade por instalação.

### Falta de água

Decidido:

- a resposta não é instantânea;
- primeiro há perda de eficiência;
- após um período sem abastecimento, a atividade pode parar;
- duração e limiares devem ser configuráveis.

Isso permite calibrar o sistema sem transformar uma interrupção momentânea em fechamento imediato.


---

---

## Alcance, influência e escolha de serviços

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status:** em pesquisa; não é uma regra fechada de produto.

A discussão anterior sobre "não usar círculo de influência" foi fechada cedo demais e fica corrigida aqui.

O que se quer investigar:

- um círculo/área visual pode ser útil para mostrar onde um serviço tende a ter **maior influência ou conveniência**;
- esse círculo não precisa significar um corte absoluto em que cidadãos fora dele ficam proibidos de usar o serviço;
- um cidadão pode aceitar viajar mais longe quando houver motivo, necessidade, qualidade superior, falta de alternativa ou capacidade disponível;
- diferentes sistemas podem precisar de modelos diferentes: hospital, escola, parque, comércio e lazer não necessariamente usam a mesma regra.

Modelos a comparar:

1. **Raio rígido** — simples e barato, mas pouco realista.
2. **Raio como peso/heurística** — proximidade aumenta preferência, sem bloquear destinos mais distantes.
3. **Custo de viagem** — escolha baseada em tempo/distância real de rota.
4. **Modelo híbrido** — raio para UI/diagnóstico + custo de viagem/capacidade para decisão real.

A decisão deve vir de pesquisa, protótipo e legibilidade para o jogador. Não assumir que "fora do círculo ninguém vem".

---
