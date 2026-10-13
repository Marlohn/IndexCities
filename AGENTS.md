# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso útil sem transformar desenvolvimento em burocracia.

## Fonte de verdade

- `docs/SPEC.md` define o **produto aprovado**, o escopo vigente e o registro explícito de pequenas decisões provisórias delegadas à IA.
- `docs/EXPLORATION.md` é o **hub/índice central da exploração**: aponta temas ativos, status e documentos temáticos relevantes.
- `docs/exploration/*.md` guarda **pesquisa temática, ideias, alternativas, referências e decisões ainda em discussão** quando o assunto merece documento próprio.
- `docs/ARCHITECTURE.md` define **as decisões estruturais de software**: fronteiras, responsabilidades e direção de dependências.
- `docs/exploration/genre-benchmark.md` guarda a **referência comparativa externa do gênero**, usada periodicamente para confrontar o IndexCities com aprendizados, falhas e expectativas observados em outros city builders; é exploração temática recorrente e não define requisitos.
- O código e o histórico deste repositório definem o estado real da implementação.

Antes de **qualquer trabalho em código**, inclusive início do desenvolvimento, correção de bug, refatoração, otimização ou manutenção, consulte a `docs/SPEC.md` e identifique o comportamento do produto que a mudança deve preservar ou realizar. Leia somente o código necessário para entender o alvo. Consulte a `docs/ARCHITECTURE.md` quando houver implicações estruturais e a exploração relevante quando existirem dúvidas ou decisões ainda abertas. **Pedido, código existente e exploração não substituem a SPEC como autorização de comportamento.**


## Mapa de prontidão e prioridades de definição

Antes de iniciar uma rodada de **definição da SPEC** ou de **priorização de decisões de produto**, consulte [`docs/exploration/project-readiness.md`](docs/exploration/project-readiness.md) para identificar lacunas, riscos e perguntas de maior valor, depois de conferir a SPEC e o hub de exploração atuais. O mapa é **orientação exploratória revisável**, não fonte de verdade, requisito aprovado, plano obrigatório de POC ou motivo para bloquear experimentos seguros. **Não reabra decisões da SPEC só porque aparecem como pendência numa análise antiga;** diferencie decisão ainda necessária, calibração e validação por protótipo. Atualize o mapa apenas quando uma decisão ou evidência mudar materialmente as prioridades.

## Auditoria humana de conteúdo produzido por IA

Conteúdo criado por IA não pode ganhar autoridade apenas porque foi escrito, pesquisado, resumido ou organizado no repositório.

### Status obrigatório de revisão humana

Todo documento temático de exploração criado ou ampliado por IA deve declarar de forma visível, logo no topo, um destes estados:

- **PENDENTE** — o conteúdo ainda não foi revisado pelo responsável;
- **PARCIALMENTE REVISADO** — parte do conteúdo ou das decisões foi discutida/confirmada, mas o documento ainda contém pesquisa, síntese, proposta ou redação da IA que não foi revisada integralmente;
- **REVISADO** — o responsável revisou explicitamente o conteúdo atual daquele documento ou seção.

Quando um documento misturar conteúdo com estados diferentes, use o estado agregado mais conservador no topo e marque também as seções relevantes com `Revisão humana desta seção`.

### Regras

- toda pesquisa, proposta, recomendação, hipótese, benchmark, síntese ou conclusão produzida pela IA começa como **PENDENTE**, salvo quando a conversa atual já contém revisão explícita daquele conteúdo;
- conteúdo criado a partir de uma decisão discutida com o responsável, mas expandido ou sintetizado pela IA sem revisão integral do texto, deve ser **PARCIALMENTE REVISADO**, não `REVISADO`;
- reorganizar, resumir, mover ou reformatar conteúdo **não conta como revisão humana**;
- adicionar conteúdo novo não revisado a um documento previamente revisado exige marcar a nova seção como `PENDENTE` ou reduzir o status agregado do documento para `PARCIALMENTE REVISADO`;
- conteúdo **PENDENTE** não pode ser promovido para `docs/SPEC.md`, `docs/ARCHITECTURE.md` nem virar regra em `AGENTS.md`;
- em conteúdo **PARCIALMENTE REVISADO**, somente a parte explicitamente discutida e confirmada pode ser promovida; a pesquisa/síntese restante continua sem autoridade;
- somente após discussão explícita e confirmação do responsável a conclusão correspondente pode ser promovida para a fonte canônica adequada;
- quando houver dúvida sobre se algo foi realmente aprovado pelo responsável, trate como **PENDENTE**;
- termos como **decidido**, **aprovado**, **confirmado** ou equivalentes só devem descrever uma decisão do responsável ou algo já registrado na fonte canônica; não podem transformar uma recomendação da IA em decisão;
- durante a conversa, sempre que a IA registrar pesquisa ou análise nova que ainda não foi revisada pelo responsável, deve avisar de forma breve que o material foi salvo como **revisão humana pendente**;
- `docs/EXPLORATION.md` deve mostrar o status agregado de revisão humana de cada documento temático, para que o responsável consiga localizar rapidamente o que ainda precisa revisar.

O objetivo é simples: a IA pode pesquisar, propor, comparar e documentar, mas **não decide pelo responsável**. A auditoria deve tornar evidente o que ainda precisa de leitura/discussão humana antes de ganhar autoridade.

**Regra operacional geral para qualquer documento ou trecho não aprovado por humano:** conteúdo marcado **PENDENTE** ou a parte não revisada de material **PARCIALMENTE REVISADO** serve somente de informação para avaliar. Não é requisito, decisão, backlog, plano obrigatório, instrução para agentes, recomendação aceita, autorização de implementação, autorização de copiar recursos nem aprovação tácita — **mesmo que o texto use verbos como "implementar", "usar", "adaptar", "reaproveitar", "importar", "priorizar" ou "próxima etapa"**. Um documento histórico sem status explícito também não ganha aprovação por omissão: conferir a decisão humana e a fonte canônica antes de agir. O rótulo **REVISADO** indica revisão humana do conteúdo, não promove automaticamente cada ideia citada a requisito; para comportamento de produto, vale a SPEC, e para arquitetura, a ARCHITECTURE. Decisões pontuais já confirmadas pelo responsável e registradas na fonte canônica continuam válidas independentemente do status agregado do documento exploratório.

Ao atualizar exploração pendente, escrever hipóteses e possibilidades como **opções de estudo**, não como ordens de execução. Quando uma pesquisa anterior sugeriu copiar, portar ou usar algo sem aprovação, corrigir sua linguagem e não iniciar essa ação. A autonomia técnica do agente segue válida **dentro das decisões canônicas**, mas não permite inferir autorização de reutilização de acervos externos ou antigos.

## Fluxo contínuo de conversa e documentação

Durante conversas de exploração ou definição com o responsável pelo projeto:

- trate o que ele disser como entrada ativa de produto, não apenas como contexto de conversa;
- quando houver afirmação factual verificável, pesquise e confirme antes de registrá-la como fato;
- prefira fontes primárias, documentação oficial, dados públicos e benchmarks reproduzíveis; não use suposição como substituto de evidência;
- quando houver uma decisão explícita, atualize a fonte de verdade apropriada imediatamente, sem esperar um pedido separado de documentação;
- decisões de produto vão para `docs/SPEC.md`;
- hipóteses, ideias, alternativas, referências, dúvidas e resultados de pesquisa ficam em `docs/EXPLORATION.md` quando forem curtos; quando o tema crescer ou estiver sendo trabalhado em paralelo, use `docs/exploration/<tema>.md` e mantenha `docs/EXPLORATION.md` como índice/status;
- regras de desenvolvimento e de trabalho vão para `AGENTS.md`;
- se a documentação existente ficar desatualizada, contraditória ou incompleta à luz da conversa atual, corrija-a na mesma sessão;
- não transforme suposição em requisito: quando não houver evidência ou decisão suficiente, registre como aberto e indique o que precisa ser pesquisado ou medido;
- mantenha a documentação enxuta: atualize o que mudou em vez de acumular transcrições da conversa;
- quando a conversa estiver sendo usada como entrevista de definição do produto, faça perguntas curtas em sequência, registre cada rodada e continue em loop enquanto existirem decisões relevantes, conflitos ou riscos reais a esclarecer; encerre naturalmente quando o plano estiver maduro e as novas perguntas passarem a ter baixo valor, repetir temas já resolvidos ou apenas "encher linguiça", mesmo que o responsável não tenha pedido explicitamente para parar;
- antes de formular novas perguntas, consulte `docs/SPEC.md` e `docs/EXPLORATION.md` e evite repetir perguntas já respondidas ou decisões já registradas;
- **seja preciso e econômico na entrevista de produto:** diferencie explicitamente casos parecidos já decididos antes de propor uma nova escolha; agrupe decisões realmente relacionadas em perguntas ou pacotes coerentes, com alternativas e consequências claras, em vez de fragmentar cada detalhe em uma rodada; não agrupe escolhas independentes de modo que uma única resposta aprove requisitos distintos por acidente;
- **priorize fechar lacunas que alterem o comportamento, a gameplay ou a coerência sistêmica**. Não prolongue indefinidamente a revisão com perguntas sobre parâmetros pequenos, exceções hipotéticas e pormenores de implementação: quando seguro, deixe-os explicitamente para calibração/implementação na exploração e avance para outro bloco significativo, ou encerre a revisão;
- antes de implementar um comportamento, parâmetro ou sistema novo, avalie explicitamente se ele deve ser configurável/opcional. Não crie opções por padrão: registre a justificativa e só exponha configuração quando a variabilidade tiver valor real para calibração, acessibilidade, debug ou gameplay;
- **Avalie novas funcionalidades também como possíveis políticas municipais:** sempre que surgir uma ideia de produto, regra ou alteração de gameplay, considere se ela fica melhor como uma **política opcional decidida na Prefeitura** (com alternativas relevantes, afetados, custos e efeitos reais), como uma **regra automática da simulação**, como uma **ação direta de construção/gestão** ou como algo a **não incluir**. Não transformar tudo em política nem criar uma opção só para aumentar o menu; política precisa de escolha interessante e consequência verificável, sem reproduzir controles já aprovados, burocracia ou bônus arbitrários. Identifique claramente a melhor classificação ao apresentar novas ideias ao responsável; **avaliar não é aprovar** uma política. Qualquer política nova ou mudança de regra existente depende de aprovação explícita e registro na SPEC antes da implementação;
- sempre que registrar uma nova decisão, verifique a coerência global com `docs/SPEC.md` e `docs/EXPLORATION.md`, procure conflitos, regras antigas ou consequências contraditórias e corrija a documentação na mesma sessão. Se existir conflito real ou ambiguidade que não possa ser resolvida com segurança, avise explicitamente o responsável antes de assumir uma resposta.
- Antes de recomendar ou fechar uma alternativa de produto, apresente de forma limpa as opções relevantes, seus principais prós, contras e consequências para gameplay/coerência. Não empurre uma solução apenas porque parece simples; registre também o que ela sacrifica e reabra a decisão quando surgir uma consequência nova relevante.
- **Raciocine criticamente além das alternativas apresentadas:** não valide uma hipótese só porque o responsável a sugeriu; questione suas premissas, confrontando-a com a SPEC, os fluxos econômicos e o custo de simulação, explicite consequências negativas e investigue modelos híbridos ou opções não mencionadas quando puderem oferecer resultado melhor. Diferencie simplicidade conceitual de custo efetivo de execução e manutenção. Recomende com justificativa e deixe explícito o que ainda depende de decisão ou protótipo, sem promover uma sugestão da IA a requisito.

## Arquitetura de especificação

O IndexCities adota **Spec-Driven Development (SDD) contínuo, com SPEC viva e obrigatória**: o comportamento desejado é decidido e documentado antes de ser codificado. Essa regra vale desde o primeiro código e durante toda a evolução do jogo — novas funcionalidades, alterações de regras, ajustes de gameplay, correções de bugs, refatorações e otimizações.

**A `docs/SPEC.md` é o contrato do produto, não apenas um documento de preparação.** Nenhum agente pode tratar como aprovado comportamento que esteja só em conversas, pesquisa, código ou exemplos externos. A única exceção de autonomia de produto é a **pequena decisão delegada, reversível, previamente registrada na SPEC e marcada PENDENTE DE REVISÃO HUMANA**. Sem esses requisitos, a lacuna depende de decisão do responsável.

- **Produto e comportamento:** toda regra nova ou alterada precisa estar registrada na `SPEC.md` **antes** do código, com **aprovação humana** ou como **decisão pequena provisória expressamente delegável**, identificada para revisão. O segundo caso nunca substitui uma regra já aprovada nem permite criar mecânica relevante.
- **Bug:** confronte o comportamento observado com a SPEC. Se a regra já estiver clara, corrija para restaurá-la sem modificar a SPEC por ritual. Se a expectativa estiver ausente, ambígua ou contraditória, **não invente o comportamento correto**: esclareça, registre a decisão na SPEC e só então corrija.
- **Refatoração, desempenho e manutenção:** só podem preservar o contrato de comportamento já documentado. Não são autorização para mudar gameplay, interfaces ou consequências do jogo; se a solução exigir mudança de produto, volte à decisão e à SPEC antes de codificá-la.
- **Autonomia técnica sem microespecificação:** dentro da SPEC e das decisões estruturais vigentes, a IA **pode e deve resolver autonomamente detalhes de implementação**, como organização interna de código, nomes, estruturas de dados, algoritmos, testes e otimizações locais. Não precisa pedir aprovação nem registrar esses detalhes na SPEC quando preservarem integralmente as regras e consequências do produto. **Ausência de uma decisão sobre detalhe puramente técnico não é lacuna de produto.** Se uma escolha alterar comportamento, interface, exceções, causalidade, escopo ou qualquer decisão já aprovada, deixa de ser apenas detalhe técnico e exige o fluxo de decisão/documentação antes do código.
- **Arquitetura:** `docs/ARCHITECTURE.md` governa decisões estruturais aprovadas, mas **não cria requisitos de produto**. Dentro dessas fronteiras, soluções técnicas locais e reversíveis são responsabilidade de quem implementa. Mudanças estruturais relevantes devem ser discutidas e registradas na ARCHITECTURE antes de orientar o código, sem substituir a aprovação na SPEC de qualquer alteração de produto.
- **Exploração e experimentos:** `docs/EXPLORATION.md` e documentos temáticos guardam hipóteses, alternativas e pesquisas, nunca autorização de implementação de produto. Experimentos técnicos isolados são opcionais, não constituem fase obrigatória nem permitem inserir comportamento não aprovado no jogo. Qualquer código incorporado ao produto continua condicionado à SPEC.
- **Calibração:** quando a própria SPEC delegar explicitamente valores ou fórmulas de ajuste à calibração, a implementação pode escolher e testar esses detalhes dentro do comportamento aprovado. Tentativas transitórias não são decisões incorporadas ao produto; se uma escolha efetiva criar ou alterar comportamento perceptível não coberto pelo contrato, registrar decisão delegada se elegível ou pedir aprovação humana. Calibração não permite criar silenciosamente novas regras, exceções, opções do jogador ou efeitos de gameplay.

### Autonomia delegada da IA e registro na SPEC

**Delegação confirmada pelo responsável em 2026-10-11.** A IA pode resolver sem interrupção detalhes técnicos locais; e pode escolher **pequenos detalhes de produto** somente se forem locais, reversíveis, de baixo impacto, não criarem mecânica/opção nova e não alterarem nenhuma regra aprovada. Exemplos possíveis: ordenação de lista secundária e apresentação não crítica de dados já existentes. **Não delegar** regras econômicas, patrimoniais, materiais, logísticas, dos SIMs/empresas, serviços, políticas, acessos, consequências causais, construção básica, UX/acessibilidade relevante, escopo de versão ou arquitetura estrutural. Menor quantidade de código não torna uma decisão de produto pequena.

**Teste obrigatório antes de decidir:** (1) já existe regra aprovada? respeitá-la; (2) a mudança toca resultados, regras, usuários ou sistemas importantes? exigir decisão humana; (3) é realmente pequena e reversível? somente então delegar; (4) registrar comportamento, justificativa sucinta, origem IA, ID e status **PENDENTE DE REVISÃO HUMANA** na seção própria da SPEC **antes de implementar**. Na dúvida, consultar o responsável. Não esconder mudança de produto como calibração, estética, bug, refatoração ou decisão técnica.

**Registro obrigatório de 100% das decisões de produto tomadas pela IA**, inclusive listas, alertas e pequenos padrões visíveis. Não registrar cada variável, método, algoritmo ou teste quando forem puramente técnicos e preservarem o contrato; decisões técnicas estruturais relevantes seguem a ARCHITECTURE. Uma decisão IA pendente é **implementável apenas por causa da delegação e de seu baixo impacto**, não é uma decisão aprovada pelo responsável. A revisão posterior pode aprovar, modificar ou rejeitar e exige sincronizar código e SPEC; a IA nunca pode marcar sua própria escolha como aprovada pelo humano.

### Desenvolvimento incremental do Convencional

**Direção confirmada pelo responsável em 2026-10-11, com recorte V1 aprovado em 2026-10-12:** preservar a visão completa já aprovada na SPEC, mas desenvolver em **versões menores, integradas e jogáveis**, com validação desde os primeiros ciclos. **A V1 já tem escopo funcional aprovado na seção *V1 — primeira versão jogável integrada* da SPEC.** Implementar em incrementos coerentes, respeitando dependências, sem impor POCs isoladas, protótipos descartáveis ou testes cerimoniais. Limitar o catálogo não autoriza criar dinheiro, estoques, agentes, deslocamentos ou consequências fictícias. A IA de assets pode trabalhar em paralelo conforme briefing exploratório, mas nem a frente visual nem a implementação podem introduzir novas regras de produto fora da SPEC. O recorte de versões futuras continua sujeito à aprovação humana.

### Porta obrigatória antes e durante qualquer implementação

1. **Localizar a base na SPEC:** identifique a regra ou o comportamento esperado que justifica o trabalho. Para mudanças sem alteração funcional (como refatoração ou otimização), identifique o comportamento já especificado que deve permanecer intacto.
2. **Checar suficiência e conflitos:** compare o pedido com a SPEC e, quando pertinente, a ARCHITECTURE e o código. Verifique se é possível implementar sem decidir comportamento, exceções ou consequências de produto por conta própria. **Não bloqueie por decisões técnicas locais que possam ser tomadas dentro do contrato vigente.**
3. **Bloquear a parte indefinida:** se faltar definição relevante, houver ambiguidade/contradição ou a escolha não passar no filtro de delegação, **não escreva o código dependente**. Explique a lacuna e solicite decisão humana; continue trabalho independente já autorizado.
4. **Registrar antes de codificar:** insira a decisão humana aprovada na SPEC **ou**, se estritamente delegável, registre a pequena escolha como **IA — PENDENTE DE REVISÃO HUMANA**, com ID e comportamento verificável. Ajuste a ARCHITECTURE quando pertinente. **Só então** implemente; a IA não aprova sua própria decisão.
5. **Repetir a checagem durante o trabalho:** se surgir regra ausente, conflito ou necessidade de alterar o comportamento, retorne ao passo 3. Nem urgência de bug, nem conveniência técnica, nem testes que passam autorizam desvio silencioso.
6. **Validar aderência:** revise implementação, efeitos colaterais e testes contra a SPEC vigente. Se código e SPEC divergirem, não altere a SPEC para justificar código sem uma decisão de produto explícita.

Não é exigido criar issue, plano, PR, checklist ou documento de especificação separado para cada mudança: é obrigatório que o comportamento esteja **aprovado na SPEC ou estritamente autorizado por delegação provisória e registrado antes do código**.

**Direção de entrega vigente:** construir o **Convencional em versões menores, jogáveis e integradas**, seguindo o recorte aprovado para cada versão e validando desde cedo economia, ações do jogador, consequências e dependências reais. Manter a visão completa da SPEC, sem tentar codificá-la integralmente antes da primeira partida utilizável. Não impor POCs independentes nem justificar atalhos fictícios na simulação. O conteúdo exato da **V1 está aprovado na SPEC**; exemplos da exploração não ampliam seu escopo. Mudança de escopo exige decisão humana explícita e atualização prévia da SPEC.

O IndexCities usa deliberadamente uma arquitetura de especificação mínima.

### SPEC

`docs/SPEC.md` é a autoridade sobre **o que o produto deve ser agora**.

Ela deve:

- descrever decisões humanas aprovadas e, em seção separada, pequenas decisões provisórias de IA explicitamente identificadas;
- permanecer curta, legível e útil;
- registrar requisitos e limites de produto, não detalhes acidentais de implementação;
- deixar explícito quando algo ainda não foi decidido.

Ela não deve virar:

- diário de desenvolvimento;
- histórico de discussões;
- depósito de ideias;
- lista de tarefas;
- especificação detalhada de classes e arquivos;
- documento gigante tentando prever o futuro.

### EXPLORATION

`docs/EXPLORATION.md` é o **hub central do pensamento ainda não oficial**. Ele deve permanecer navegável e indicar:
- temas ativos;
- status resumido;
- links para documentos temáticos;
- questões transversais que ainda não justificam arquivo próprio.

Use `docs/exploration/<tema>.md` quando:
- a pesquisa ficar extensa;
- houver muitas alternativas/evidências;
- o tema estiver sendo trabalhado em paralelo por conversas diferentes;
- manter tudo no hub piorar leitura ou aumentar conflito de edição.

Regras dos documentos temáticos:
- devem declarar no topo que **não são fonte de verdade**;
- devem indicar status quando útil;
- preservam pesquisa, alternativas, referências, prós/contras e raciocínio;
- quando uma decisão fecha, **o resultado oficial entra resumido na SPEC**;
- o documento pode continuar como histórico/raciocínio, mas deve marcar o que foi decidido, superado ou ainda está aberto;
- não criar subpastas adicionais ou novos documentos por ritual; só separar quando houver volume ou paralelismo real.

Evite criar novos arquivos Markdown soltos em `docs/` para exploração. A raiz de `docs/` fica reservada às fontes canônicas (`SPEC.md`, `ARCHITECTURE.md`, `EXPLORATION.md`) e a eventuais documentos que sejam explicitamente aprovados como canônicos.

### Padrão de organização da exploração para IAs

Este padrão existe para evitar que `docs/EXPLORATION.md` volte a se tornar um arquivo monolítico com centenas ou milhares de linhas, difícil de navegar, caro para carregar em contexto e propenso a conflitos quando várias sessões trabalham em paralelo.

Ao trabalhar com exploração:

1. **Comece pelo hub.** Leia `docs/EXPLORATION.md` para localizar o domínio relevante antes de criar ou editar conteúdo.
2. **Atualize o documento temático existente.** Se o assunto já pertence claramente a um domínio listado no hub, escreva em `docs/exploration/<tema>.md`; não replique a pesquisa no hub.
3. **Use o hub apenas para navegação e estado.** Mantenha ali nome do tema, status curto, uma frase de escopo e link. Pesquisa extensa, benchmarks, alternativas, histórico e raciocínio pertencem ao documento temático.
4. **Agrupe por domínio durável, não por conversa.** Prefira documentos como `construction-materials-logistics.md` ou `companies-markets.md` em vez de arquivos por sessão, data, pergunta ou rodada de pesquisa.
5. **Não crie um arquivo novo cedo demais.** Se a observação for curta, transversal e não tiver domínio natural, ela pode ficar temporariamente no hub. Separe somente quando o conteúdo ganhar volume real, referências, alternativas ou trabalho paralelo.
6. **Não deixe documentos temáticos crescerem sem limite.** Se um arquivo passar a misturar domínios que podem evoluir independentemente, divida por fronteira conceitual estável e atualize o hub. Não fragmente apenas por tamanho.
7. **Preserve o histórico útil, não a transcrição.** Conserve evidências, alternativas rejeitadas, trade-offs e motivo de decisões quando ajudarem trabalho futuro. Remova repetição, conversa cronológica e texto que não acrescenta contexto reutilizável.
8. **Marque o estado do conhecimento.** Quando algo for decidido, superado, reaberto ou continuar em exploração, deixe isso claro no documento temático. Não mantenha uma recomendação antiga parecendo vigente.
9. **Promova sem duplicar autoridade.** Se uma exploração virar decisão oficial, registre o resultado conciso na `SPEC.md`, `ARCHITECTURE.md` ou `AGENTS.md` e mantenha no documento temático apenas o raciocínio/histórico, apontando que a fonte canônica é outra.
10. **Revise links após mover conteúdo.** Documentos em `docs/exploration/` têm caminho relativo diferente do hub; corrija referências e verifique que nenhum link ficou apontando para arquivo removido ou localização antiga.

O objetivo não é produzir mais documentação. É tornar o contexto **mais barato de localizar, mais fácil de carregar seletivamente e menos sujeito a conflito**, para humanos e IAs. Uma IA futura deve conseguir ler o hub, abrir somente os temas necessários para a tarefa e chegar ao contexto relevante sem precisar consumir todo o histórico de exploração.



### Roteamento rápido de documentação

Antes de criar ou editar documentação, escolha o destino pelo conteúdo:

- **Decisão de produto já fechada** → `docs/SPEC.md`.
- **Decisão estrutural de software já fechada** → `docs/ARCHITECTURE.md`.
- **Pesquisa/ideia curta ou questão transversal** → `docs/EXPLORATION.md`.
- **Pesquisa extensa ou tema trabalhado em paralelo** → `docs/exploration/<tema>.md`, com link/status no hub.
- **Regra de trabalho para humanos/IAs** → `AGENTS.md`.

Não trate nenhum arquivo em `docs/exploration/` como requisito só porque ele contém uma conclusão ou recomendação. Uma decisão de produto só é oficial depois de aparecer na SPEC; uma decisão estrutural só é oficial depois de aparecer na ARCHITECTURE.

Antes de criar um novo arquivo:
1. verifique se já existe documento temático equivalente;
2. prefira atualizar o existente;
3. só crie outro quando separar o assunto reduzir conflito ou melhorar claramente a leitura;
4. nunca duplique a mesma decisão em dois documentos exploratórios como se ambos fossem autoritativos.

### Sem herança automática

Nada de outro projeto, protótipo, conversa antiga ou implementação anterior vale automaticamente no IndexCities.

**Diretriz expressa do responsável sobre projetos anteriores:** o IndexCities nasce do zero. **Não copiar, portar, importar ou reutilizar diretamente código-fonte, algoritmos implementados, testes, arquivos, assets/modelos 3D, texturas, dados, configurações, interfaces, pipelines ou documentos de experiências anteriores.** Licença permissiva, compatibilidade aparente ou sugestão em exploração não constitui autorização de uso; qualquer exceção futura depende de decisão humana expressa e específica.

**Relatos históricos são apenas lições autossuficientes, não pistas para mineração de componentes.** Sem solicitação específica do responsável, não pesquisar, abrir ou consultar repositórios antigos, commits, issues, código, testes ou acervos de experiências anteriores por iniciativa própria. Ler uma nota informal de aprendizados não autoriza reconstruir sua procedência nem tentar adaptar a implementação de origem. Em vez disso, partir da SPEC e da ARCHITECTURE, criar a solução própria e considerar uma lição apenas como alerta quando ela for pertinente.

Material externo ou histórico de pesquisa pode informar uma investigação concreta quando permitido, mas **não** deve ser herdado como funcionalidade, arquitetura, dependência, recurso ou plano de implementação. Qualquer conceito novo de produto só vira requisito após decisão explícita do responsável registrada na SPEC; decisões estruturais seguem a ARCHITECTURE. **Esta distinção vale para todos os documentos exploratórios sem revisão humana integral.**

### Evolução futura

Não adote OpenSpec, Spec Kit, BMAD ou outro framework de processo apenas por parecer mais completo.

A arquitetura só deve ficar mais sofisticada quando existir uma dor concreta e recorrente, por exemplo:

- a SPEC única ficou grande demais para continuar clara;
- agentes perdem contexto importante entre sessões;
- implementação e SPEC divergem repetidamente;
- múltiplas frentes paralelas começam a conflitar;
- rastreabilidade adicional passa a economizar mais tempo do que custa.

Até isso acontecer, **SPEC + ARCHITECTURE + EXPLORATION hub + este AGENTS.md continuam sendo a arquitetura central**. Documentos em `docs/exploration/` são extensões da exploração, não novas fontes de verdade. O benchmark do gênero também vive ali, apesar de ter uso recorrente.


## Direção da Prefeitura: mais escolhas estratégicas do jogador — 2026-10-11

**Preferência expressa do responsável:** ampliar a **agência de decisão agregada do jogador** sobre a administração e a política municipal no Convencional, inclusive **reavaliando decisões anteriores**, em vez de confiar automaticamente à simulação escolhas de financiamento, cobrança, prioridade e acesso que poderiam gerar gameplay interessante.

- Ao revisar educação (creches, escolas/universidades), saúde, transporte, estacionamento, atrações, assistência, água/energia, previdência e demais serviços municipais, **não tratar gratuidade, tarifas automáticas, benefícios e escolhas antigas como imunes a reavaliação**. Identificar opções concretas para o jogador e seus efeitos reais sobre saldo municipal, utilização, capacidade, renda, saúde, acesso a trabalho e rotina familiar.
- **Reavaliação não revoga requisitos:** a SPEC vigente continua valendo enquanto o responsável não escolher expressamente alternativa substituta. Documentar candidatos e análises pendentes na exploração; só depois da aprovação promover mudança à SPEC e corrigir contradições.
- Distinguir **política estratégica agregada** (gratuito/pago/subsidiado, prioridade, nível de financiamento quando realmente significativo), **ferramenta de gestão** (construir, ajustar capacidade, empréstimos, orçamento) e **automação operacional necessária** (pagar contas devidas, cobrar tarifas aprovadas, transportar material, despachar equipe, decidir autonomamente cada SIM/empresa). Mais controle não significa microgerenciar pessoas, rotas e transações.
- Confrontar cada proposta com o valor de gameplay: **quem decide, qual é o exemplo concreto, o que acontece se escolher A ou B, quem ganha/perde, qual despesa/receita real muda, que risco de frustração para criança construtora e que custo de simulação adicional existem?** Se opções só diferem de nome, descartá-las.
- **Perguntas ao responsável precisam ser autoexplicativas.** Começar com um caso real simples (por exemplo: escola gratuita custeada pelo Caixa versus escola paga que pode excluir famílias sem saldo); formular a diferença das alternativas A/B/C sem abstrações e sem presumir familiaridade com conceitos técnicos; indicar recomendação e desvantagem claramente. Não rotular como “decidido” uma resposta expressamente condicionada a dúvida, incompreensão ou revisão de valor (“não entendi”, “e daí?”); mantenha-a em exploração até confirmação inequívoca.
- Não usar benchmarking de outros jogos como autorização automática para copiar políticas, subsídios, multas, programas ou burocracia. Preservar dinheiro finito, fluxos físicos, autonomia de SIMs e empresas e diagnóstico causal.

## Princípio de gameplay, microgerenciamento e custo interno da simulação

**Regra permanente confirmada pelo responsável em 2026-10-08: qualidade de gameplay e controle de microgerenciamento são critérios obrigatórios para TODO sistema do jogo, não apenas construção ou realocação.** A IA deve questionar propostas que criem espera, fricção, controles, estados ou exceções sem benefício proporcional, inclusive quando ela própria sugeriu a complexidade. Preservar causalidade e consequências reais; o objetivo não é transformar profundidade em burocracia.

Para qualquer decisão de produto, avalie explicitamente o microgerenciamento do jogador **e** o custo de manter e executar a lógica nos bastidores contra o valor de gameplay.

- Realismo não é objetivo suficiente por si só: detalhe adicional precisa criar decisão, consequência, leitura sistêmica ou feedback interessante.
- Preserve profundidade sistêmica quando ela gera causa e efeito observável, especialmente produção, logística, emprego, trânsito, capacidade, escassez e finanças.
- Abstraia ou automatize tarefas repetitivas que exigem cliques/contabilidade sem produzir decisões significativas.
- Prefira interfaces agregadas para leitura e decisão quando a simulação puder manter o detalhe físico internamente.
- Não remova logística, escassez ou consequências apenas para simplificar; simplifique a operação manual, não necessariamente a simulação.
- Ao comparar alternativas, registre quando uma opção é mais realista porém pior de jogar, ou mais simples porém destrói consequências importantes.
- Aplique este critério de forma genérica a todos os sistemas, não apenas a materiais e construção.
- **Automatizar não torna um sistema simples:** muitos estados por SIM, rotinas recorrentes, exceções, dependências e regras de conciliação também são microgerenciamento interno, consumindo performance, memória, manutenção, testes e capacidade de diagnóstico.
- Antes de acrescentar um subsistema, verifique se sua consequência relevante pode ser representada com menos estados, menos interações e uma regra clara, preferindo processamento por eventos ou ciclos apropriados a verificações constantes quando viável.
- Profundidade causal é desejável, mas simular burocracia, contabilidade ou negociações em detalhe só se justifica quando gerar consequência de gameplay relevante e observável. Não empurre complexidade para trás da interface para parecer que o jogo ficou simples.
- **Antes de propor, especificar ou implementar uma mecânica, confronte:** o que ela acrescenta às decisões/consequências interessantes; quantos cliques, etapas e esperas reais impõe ao jogador; quais estados, exceções, consultas e testes acrescenta internamente; e se regras já existentes ou uma transição acionada por eventos entregam o mesmo valor com menos custo. Em caso de baixo ganho, simplifique ou não crie a mecânica.
- **Calibre a espera pelo ritmo percebido no jogo**, distinguindo tempo simulado, tempo real do jogador e interrupção efetiva de serviços/produção. Não acrescente atrasos ou sistemas auxiliares para resolver problemas hipotéticos antes de verificar se realmente aparecem na gameplay integrada; não use essa diretriz para apagar custos, materiais, deslocamentos ou consequências aprovados.

## Critério permanente: diversão de construir para diferentes públicos

**Decisão expressa do responsável em 2026-10-11:** o IndexCities deve oferecer profundidade de gestão para adultos **e** levar em conta quem, como uma criança que ainda não lê, se diverte prioritariamente construindo. **Esse critério deve estar presente em TODA revisão, definição de funcionalidade, interface, avaliação de gameplay e teste**, mesmo quando o assunto técnico/econômico não parecer direcionado a crianças.

- Em cada sistema ou decisão relevante, pergunte: **como a consequência aparece no mundo?** O jogador que só quer construir consegue identificar, posicionar, experimentar e observar algo interessante **sem depender de ler textos ou dominar a gestão**? Onde o controle, a espera ou o erro se tornam frustrantes?
- Preserve a profundidade aprovada de dinheiro, materiais, SIMs, empresas, logística e causalidade **no modo Convencional**. Não desative nem falsifique sistemas para parecer infantil; procure fluidez de construção, prévia, feedback visual, iconografia reconhecível, poucas etapas, personalização estética e informações em camadas.
- **Um sistema avançado não precisa ser operado por uma criança.** Avalie o impacto dele na diversão de construir, e não imponha a exigência impossível de tornar cada painel econômico legível sem texto. Não confunda interface acessível com ausência de regras econômicas.
- Valide o que for possível com observação de usuários com perfis distintos, incluindo uma pessoa que ainda não lê; priorize evidência de uso real ao invés de decidir dificuldade apenas por hipótese. Não transforme todo sistema em teste/POC obrigatório.
- **Escopo vigente:** desenvolver o jogo **Convencional**. Ter os modos **Convencional e Criativo** é uma intenção de produto confirmada **para o futuro**, com desenho e implementação do Criativo adiados. **Não implemente, especifique ou crie camadas técnicas extras para o Criativo agora**, nem force a solução histórica de uma segunda simulação. Os detalhes anteriormente discutidos foram preservados na exploração para eventual reavaliação. A diretriz de pensar na experiência infantil **vale desde já**, independentemente do modo futuro.

## Princípio global: decisões do jogador afetam a vida dos SIMs

**Esta é uma orientação permanente para toda IA que pesquisar, especificar, implementar, revisar ou testar o IndexCities.** A regra de produto aprovada está em `docs/SPEC.md`, na seção *Princípio de design: leitura em camadas e causalidade*; este `AGENTS.md` transforma essa direção em critério obrigatório de trabalho.

- **Toda decisão relevante do jogador deve ter consequências reais, coerentes e explicáveis para os SIMs, famílias e, quando pertinente, empresas**: moradia, trabalho, renda, custos de vida, deslocamentos, acesso a serviços, patrimônio e demais sistemas existentes. Não criar punições ou benefícios arbitrários apenas para parecer que há impacto.
- **Não confundir intervenção sobre estruturas com decisão dos agentes:** SIMs e empresas são entidades reais que tomam decisões econômicas próprias; o jogador escolher onde construir/realocar não decide automaticamente quem vai ocupar, adquirir ou operar o imóvel. Em especial, empresas avaliam autonomamente a oportunidade de continuar no endereço novo; SIMs empregados avaliam independentemente permanecer no emprego. Consulte as regras específicas da SPEC: **em realocação assistida privada, o proprietário original (SIM ou empresa) tem a primeira oportunidade de comprar o substituto pelo valor de mercado novo**, quando elegível e interessado; isso não autoriza transferência automática de propriedade/estoque, aquisição gratuita ou continuidade garantida. Preservar gameplay fluido e evitar criar uma camada adicional de IA por agente sem necessidade.
- **Antes de fechar qualquer nova funcionalidade ou alteração de gameplay, verificar quem é afetado, por qual mecanismo real e como o jogador percebe isso.** Se o impacto relevante ficar invisível, opaco ou contraditório, apontar a lacuna e propor diagnóstico simples antes de implementar.
- Mostrar o importante de modo **contextual e agregado**, com aprofundamento disponível sob demanda; evitar pop-ups por SIM, gestão individual obrigatória, cálculos contínuos globais ou novos estados por pessoa sem ganho demonstrado.
- **Liberdade de construir e modificar a cidade importa:** não bloquear o jogador com burocracia apenas para simular realismo; preservar, porém, os efeitos reais sobre propriedade, pessoas e economia e os custos monetários/físicos já aprovados.
- Nas revisões da SPEC e do código, conferir esse princípio **transversalmente**, inclusive em ferramentas de edição, demolição, serviços e tributos. Ele não autoriza que a IA invente consequências ainda não decididas: lacunas de comportamento de produto devem voltar à discussão e entrar na SPEC antes de virar código.

## Princípio de causalidade e diagnóstico

Sistemas profundos não podem virar caixas-pretas.

- Toda consequência relevante para o jogador deve ser explicável a partir do estado real da simulação.
- Quando algo falhar, o sistema deve conseguir responder **o que aconteceu, por que aconteceu e qual ação pode resolver**, usando as mesmas variáveis que realmente produziram o resultado.
- Evite variáveis vagas de "saúde", "felicidade" ou "economia" quando elas apenas escondem causas reais. Agregados podem existir para UI, mas precisam ser derivados de fatores inspecionáveis.
- Automação e abstração de interface não autorizam lógica invisível sem rastreabilidade.
- Para calibração e debug, prefira fórmulas determinísticas, parâmetros explícitos e estados observáveis. Aleatoriedade só deve ser usada quando tiver função clara de gameplay e continuar diagnosticável.
- Ao implementar sistemas sistêmicos, mantenha uma forma proporcional de inspecionar entradas, saídas e causas — por UI de diagnóstico, debug ou telemetria conforme a fase do projeto.
- O feedback superficial deve ser simples e acionável; o detalhamento deve estar disponível sob demanda sem exigir que todos os jogadores o leiam.
- Não crie uma explicação separada da simulação: a explicação exibida deve ser derivada da causa real.
- Em toda nova conversa, sessão ou decisão de produto, aplique explicitamente este princípio antes de fechar o sistema: procure caixas-pretas, causas não rastreáveis, automações invisíveis e estados agregados que não possam ser decompostos. Se existirem, mantenha a decisão em exploração até haver uma forma clara de diagnóstico.
- Prefira o máximo possível **entidades, fluxos e estados concretos da simulação** em vez de variáveis ou agentes abstratos criados apenas para fechar uma conta. Abstrações continuam permitidas quando reduzem microgerenciamento ou custo técnico sem destruir causalidade, mas devem ser derivadas de estado real, auditáveis e não podem virar fonte ou sumidouro mágico de dinheiro, recursos, demanda ou comportamento.

## Regra principal de escopo

- **Sem comportamento de produto documentado e autorizado na SPEC, não há implementação desse comportamento.** Isso vale para o início do projeto e para cada mudança futura.
- Se o pedido exigir decisão ausente, obtenha aprovação humana; somente decisão comprovadamente pequena e delegável pode ser registrada como IA pendente antes do código.
- Bug com comportamento esperado já definido na SPEC pode ser corrigido diretamente **com base nessa regra**; bug que revela conflito ou regra significativa ausente exige decisão humana antes do código dependente.
- Refatorações, ajustes técnicos e testes não podem introduzir silenciosamente regras de produto nem contornar o registro obrigatório.
- Experimentos técnicos isolados não são fases obrigatórias nem autorizam inserir features não aprovadas no jogo.
- Não invente features, abstrações ou sistemas para um futuro hipotético.

## Coordenação da V1 e frente de assets — 2026-10-12

**Divisão de trabalho expressamente definida pelo responsável:** Codex administra e orquestra a criação da primeira versão jogável, revisa a documentação e cuida da implementação, integração e validação. Claude Opus 5.5, com `ultracode` solicitado, trabalha exclusivamente na produção de assets.

- Codex decide quais assets são necessários ao incremento dentro do catálogo aprovado na SPEC e envia pedidos delimitados ao Claude. O inventário exploratório não é uma ordem automática para produzir tudo.
- Claude pode criar recursos visuais originais, fontes/geradores de arte, cenas de apresentação, ícones e prévias técnicas dentro da pasta de assets indicada no pedido. Não implementa simulação, ferramentas do jogador, economia, persistência ou regras de produto; não edita SPEC, ARCHITECTURE nem arquivos compartilhados sem pedido específico do coordenador.
- Escala, orientação, pivôs e conectores são compatibilizados com Codex antes de produzir um lote definitivo. Dimensões candidatas de uma amostra visual não aprovam footprints, acessos, capacidades ou custos de gameplay.
- Codex verifica cada entrega, solicita correções ao Claude e integra os recursos ao jogo. Uma amostra visual não comprova funcionamento da simulação nem conclusão da V1.
- Confirmar modelo e configuração efetivamente usados pelo runtime. Se houver indisponibilidade ou substituição, comunicar a diferença; não afirmar que o pedido foi atendido apenas porque as opções foram enviadas.

**Autorização operacional nesta sessão:** o responsável pediu aprovar os próximos requests do Orca relacionados ao trabalho, sem novas perguntas de confirmação. Essa autorização cobre a coordenação e os pedidos necessários aos assets; comportamentos do jogo continuam regidos pela SPEC e pelas regras de decisão acima.

**Continuidade sem o Codex — pedido do responsável em 2026-10-12 (noite):** com os créditos do Codex esgotados, o responsável pediu que Claude continue o trabalho de implementação, integração e validação que o Codex vinha fazendo. Enquanto durar essa substituição, Claude segue as mesmas regras do Codex (SPEC antes do código, delegação limitada, validação proporcional) e não herda autorização para decisões de produto; quando o Codex voltar, a divisão acima volta a valer, salvo nova decisão.

## Filosofia de desenvolvimento

- Priorize resultado observável e feedback rápido.
- Prefira a menor mudança coerente.
- Não existe obrigação de criar issue, PR, branch especial, plano, ADR, TDD ou documento extra para toda mudança.
- Use testes quando eles protegem comportamento ou evitam regressão; não crie testes cerimoniais.
- Não crie handoffs artificiais entre papéis de IA.
- Processo novo só entra quando resolve uma dor recorrente e observável.
- Planejamento deve ser proporcional ao risco.

## Como trabalhar

1. **Leia a SPEC antes de mexer em código** e encontre a decisão vigente pertinente ao trabalho, mesmo em bugs e manutenção.
2. Confira se a funcionalidade pertence ao recorte aprovado da versão e se a SPEC define seu comportamento. Lacuna, conflito ou mudança significativa exige parar essa parte e consultar o responsável; somente detalhe pequeno que passe integralmente pelo teste de delegação pode ser decidido pela IA e registrado antes do código.
3. Consulte a ARCHITECTURE para restrições estruturais e a EXPLORATION para contexto aberto; inspecione somente o código necessário.
4. Implemente o comportamento autorizado pela SPEC para a entrega, sem novos atalhos, simulações fictícias ou expansão de escopo.
5. Se surgir nova lacuna durante a implementação, volte ao passo 2. Execute primeiro os testes mais próximos da área alterada e amplie quando o risco justificar; valide o ciclo integrado desde cedo.
6. Revise o diff e os testes contra a SPEC, inclusive os registros IA pendentes; corrigir divergência não autoriza alterar silenciosamente o contrato.
7. Atualize a SPEC **antes**, nunca depois, de codificar qualquer decisão de produto (humana ou delegada). Não a reescreva por detalhes exclusivamente técnicos que já preservam os requisitos existentes.

## Definição prática de pronto

Uma mudança está pronta quando o comportamento implementado está coberto pela SPEC (decisão humana aprovada ou pequena decisão IA autorizada e explicitamente pendente), pertence ao recorte vigente, funciona conforme esse contrato, passa pela validação adequada e não deixa divergência conhecida entre código e SPEC.

Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
