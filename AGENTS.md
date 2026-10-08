# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso útil sem transformar desenvolvimento em burocracia.

## Fonte de verdade

- `docs/SPEC.md` define o **produto decidido** e o escopo atual.
- `docs/EXPLORATION.md` é o **hub/índice central da exploração**: aponta temas ativos, status e documentos temáticos relevantes.
- `docs/exploration/*.md` guarda **pesquisa temática, ideias, alternativas, referências e decisões ainda em discussão** quando o assunto merece documento próprio.
- `docs/ARCHITECTURE.md` define **as decisões estruturais de software**: fronteiras, responsabilidades e direção de dependências.
- `docs/exploration/genre-benchmark.md` guarda a **referência comparativa externa do gênero**, usada periodicamente para confrontar o IndexCities com aprendizados, falhas e expectativas observados em outros city builders; é exploração temática recorrente e não define requisitos.
- O código e o histórico deste repositório definem o estado real da implementação.

Antes de uma mudança relevante, leia a SPEC e somente o código necessário para entender o alvo. Consulte a ARCHITECTURE quando a mudança afetar organização técnica, fronteiras ou dependências. Consulte a EXPLORATION quando a tarefa depender de uma discussão ainda aberta ou do raciocínio que levou a uma decisão.


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
- sempre que registrar uma nova decisão, verifique a coerência global com `docs/SPEC.md` e `docs/EXPLORATION.md`, procure conflitos, regras antigas ou consequências contraditórias e corrija a documentação na mesma sessão. Se existir conflito real ou ambiguidade que não possa ser resolvida com segurança, avise explicitamente o responsável antes de assumir uma resposta.
- Antes de recomendar ou fechar uma alternativa de produto, apresente de forma limpa as opções relevantes, seus principais prós, contras e consequências para gameplay/coerência. Não empurre uma solução apenas porque parece simples; registre também o que ela sacrifica e reabra a decisão quando surgir uma consequência nova relevante.
- **Raciocine criticamente além das alternativas apresentadas:** não valide uma hipótese só porque o responsável a sugeriu; questione suas premissas, confrontando-a com a SPEC, os fluxos econômicos e o custo de simulação, explicite consequências negativas e investigue modelos híbridos ou opções não mencionadas quando puderem oferecer resultado melhor. Diferencie simplicidade conceitual de custo efetivo de execução e manutenção. Recomende com justificativa e deixe explícito o que ainda depende de decisão ou protótipo, sem promover uma sugestão da IA a requisito.

## Arquitetura de especificação

O IndexCities usa deliberadamente **Spec-Driven Development (SDD)** de forma leve: primeiro definimos claramente o que deve existir; depois implementamos.

Aqui isso significa:

- mudança de produto ou comportamento novo entra na `SPEC.md` antes do código;
- decisão estrutural importante entra na `ARCHITECTURE.md` antes de orientar implementação;
- dúvidas, pesquisa e alternativas ainda abertas ficam na `EXPLORATION.md` ou, quando forem temáticas/extensas, em `docs/exploration/<tema>.md` referenciadas pelo hub;
- bugs que apenas restauram comportamento já especificado não exigem mudar a SPEC;
- experimentos técnicos isolados são opcionais para investigar riscos e não criam fases obrigatórias de produto; só viram produto quando a decisão for registrada na SPEC.

SDD aqui **não** significa adotar obrigatoriamente Spec Kit, OpenSpec, BMAD ou criar uma cadeia de documentos para cada mudança. A especificação vem antes do desenvolvimento, mas o processo continua proporcional ao risco e ao tamanho da mudança.

**Direção da primeira implementação, confirmada pelo responsável:** desenvolver o **jogo integrado definido pela SPEC**, com a **validação principal global depois que o conjunto estiver funcional**. Não estabelecer POCs de sistemas isolados (economia, fazenda, mapa, interface etc.) como entregas obrigatórias, nem usar recomendações históricas de "primeira POC" ou "após a POC" para cortar automaticamente funcionalidades aprovadas. **Implementação integrada não significa um único commit ou integração às cegas:** construir em incrementos técnicos coerentes, com testes locais e verificações proporcionais quando úteis, evita descobrir falhas estruturais só no final. Experimentos técnicos isolados são opcionais, não fases de produto. Qualquer alteração do escopo oficial exige decisão explícita na SPEC.

O IndexCities usa deliberadamente uma arquitetura de especificação mínima.

### SPEC

`docs/SPEC.md` é a autoridade sobre **o que o produto deve ser agora**.

Ela deve:

- descrever somente o estado desejado já decidido;
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

Material externo pode ser consultado como pesquisa quando ajudar, mas qualquer conceito só vira requisito daqui depois de ser reavaliado e decidido para este projeto.

### Evolução futura

Não adote OpenSpec, Spec Kit, BMAD ou outro framework de processo apenas por parecer mais completo.

A arquitetura só deve ficar mais sofisticada quando existir uma dor concreta e recorrente, por exemplo:

- a SPEC única ficou grande demais para continuar clara;
- agentes perdem contexto importante entre sessões;
- implementação e SPEC divergem repetidamente;
- múltiplas frentes paralelas começam a conflitar;
- rastreabilidade adicional passa a economizar mais tempo do que custa.

Até isso acontecer, **SPEC + ARCHITECTURE + EXPLORATION hub + este AGENTS.md continuam sendo a arquitetura central**. Documentos em `docs/exploration/` são extensões da exploração, não novas fontes de verdade. O benchmark do gênero também vive ali, apesar de ter uso recorrente.


## Princípio de gameplay, microgerenciamento e custo interno da simulação

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

- **Funcionalidade nova de produto precisa estar na SPEC antes de ser implementada.**
- Se uma solicitação introduzir comportamento novo ainda não especificado, atualize a SPEC como parte da mesma mudança e então implemente.
- Bug que apenas restaura comportamento já especificado pode ser corrigido diretamente.
- Experimentos técnicos pontuais podem acontecer isoladamente, sem instituir uma sequência de POCs por sistema. Só viram produto quando aceitos e incorporados à SPEC.
- Não invente features, abstrações ou sistemas para um futuro hipotético.

## Filosofia de desenvolvimento

- Priorize resultado observável e feedback rápido.
- Prefira a menor mudança coerente.
- Não existe obrigação de criar issue, PR, branch especial, plano, ADR, TDD ou documento extra para toda mudança.
- Use testes quando eles protegem comportamento ou evitam regressão; não crie testes cerimoniais.
- Não crie handoffs artificiais entre papéis de IA.
- Processo novo só entra quando resolve uma dor recorrente e observável.
- Planejamento deve ser proporcional ao risco.

## Como trabalhar

1. Entenda o comportamento desejado na SPEC e no pedido atual.
2. Se a decisão ainda estiver aberta, pesquise e registre o raciocínio na EXPLORATION antes de transformá-la em produto.
3. Inspecione somente o necessário.
4. Implemente diretamente.
5. Rode primeiro os testes mais próximos da área alterada; amplie a validação somente quando a mudança justificar.
6. Revise o diff final e confirme que não adicionou escopo não pedido.
7. Atualize a SPEC somente quando o produto mudou.

## Definição prática de pronto

Uma mudança está pronta quando o comportamento pedido funciona, a validação adequada passa e não existe divergência conhecida entre implementação e SPEC.

Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
