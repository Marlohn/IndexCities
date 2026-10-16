# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso útil sem transformar desenvolvimento em burocracia.

## 1. Fontes de verdade e onde registrar

| Conteúdo | Onde fica | Autoridade |
|---|---|---|
| Produto aprovado: estado atual, regras gerais, recorte da versão, decisões delegadas à IA, índice | `docs/SPEC.md` (hub) | Fonte de verdade |
| Regras de produto de cada sistema, princípios de design | `docs/spec/<sistema>.md`, `docs/spec/principios.md` | Mesma da SPEC |
| Histórico das decisões (opções, datas, rodadas) | `docs/decisoes/<sistema>.md` | Só registro |
| Decisões estruturais de software (fronteiras, responsabilidades, dependências) | `docs/ARCHITECTURE.md` | Fonte de verdade estrutural; não cria requisito de produto |
| Pesquisa, ideias, alternativas, referências, benchmark do gênero | `docs/EXPLORATION.md` (hub) e `docs/exploration/<tema>.md` | Nenhuma — só consulta |
| Regras de trabalho para humanos e IAs | `AGENTS.md` | Fonte de verdade de processo |
| Estado real da implementação | código e histórico do repositório | — |

- Decisão de produto fechada → regra no presente, sem histórico, no arquivo do sistema; histórico em `docs/decisoes/`; mudança de escopo, regras gerais e decisões delegadas → `docs/SPEC.md`.
- A SPEC registra requisitos e limites de produto, deixa explícito o que não foi decidido e não vira diário, depósito de ideias, lista de tarefas, especificação de classes ou previsão do futuro.
- Raiz de `docs/` só para as fontes canônicas; pesquisa nova vai para `docs/exploration/`. Antes de criar arquivo, prefira atualizar o existente; nunca duplique a mesma decisão em dois lugares como se ambos fossem autoritativos. Revise links ao mover conteúdo (`scripts/Validate-Docs.ps1`).
- Não adote OpenSpec, Spec Kit, BMAD ou outro framework de processo só por parecer mais completo; a estrutura só cresce diante de dor concreta e recorrente.

## 2. SPEC antes do código

**SDD contínuo:** nenhum comportamento de produto é implementado sem estar na SPEC antes — vale para funcionalidades, ajustes de gameplay, bugs, refatorações e otimizações. Pedido, conversa, código existente, exploração ou exemplo externo **não** autorizam comportamento.

- **Bug:** se a regra já está clara na SPEC, corrija para restaurá-la sem editar a SPEC por ritual. Se a expectativa está ausente, ambígua ou contraditória, **não invente o comportamento correto**: esclareça, registre e só então corrija.
- **Refatoração, desempenho, testes, manutenção:** só preservam o contrato; não introduzem regra de produto. Se a solução exigir mudar produto, volte à decisão.
- **Autonomia técnica:** dentro da SPEC e da ARCHITECTURE, a IA **resolve sozinha** organização de código, nomes, estruturas de dados, algoritmos, testes e otimizações locais, sem pedir aprovação nem registrar. Ausência de decisão sobre detalhe puramente técnico não é lacuna de produto. Se a escolha alterar comportamento, interface, exceções, causalidade ou escopo, deixa de ser técnica.
- **Arquitetura:** mudanças estruturais relevantes são discutidas e registradas na ARCHITECTURE antes de orientar o código.
- **Calibração:** quando a SPEC delega valores/fórmulas à calibração, a implementação escolhe e testa dentro do comportamento aprovado; calibração não cria regras, exceções, opções do jogador ou efeitos novos.
- **Experimentos técnicos isolados** são opcionais, não são fase obrigatória e não inserem comportamento não aprovado no jogo. Não invente features, abstrações ou sistemas para um futuro hipotético.

### Decisões pequenas delegadas à IA (confirmado pelo responsável em 2026-10-11)

A IA pode escolher **pequenos detalhes de produto** só se forem locais, reversíveis, de baixo impacto, não criarem mecânica/opção nova e não alterarem regra aprovada (ex.: ordenação de lista secundária, apresentação não crítica de dados existentes). **Não delegar:** regras econômicas, patrimoniais, materiais, logísticas, de SIMs/empresas, serviços, políticas, acessos, consequências causais, construção básica, UX/acessibilidade relevante, escopo de versão ou arquitetura estrutural. Menos código não torna uma decisão de produto pequena.

Teste antes de decidir: (1) existe regra aprovada? respeite; (2) toca resultados, regras, usuários ou sistemas importantes? decisão humana; (3) é pequena e reversível? só então delegue; (4) registre na seção própria da SPEC, **antes de implementar**, comportamento, justificativa, origem IA, ID e status **PENDENTE DE REVISÃO HUMANA**. Na dúvida, consulte o responsável. Registre 100% das decisões de produto da IA (inclusive listas, alertas e pequenos padrões visíveis), nunca as puramente técnicas. Decisão IA pendente é implementável só pela delegação; não é aprovação humana, e a IA nunca marca a própria escolha como aprovada. A revisão posterior pode aprovar, modificar ou rejeitar e exige sincronizar código e SPEC. Não esconda mudança de produto como calibração, estética, bug, refatoração ou decisão técnica.

### Porta obrigatória antes e durante a implementação

1. **Localize a base na SPEC** (ou o comportamento que deve permanecer intacto, em refatoração/otimização) e confira se pertence ao recorte aprovado da versão. Leia só o código necessário para entender o alvo.
2. **Cheque suficiência e conflitos** com a SPEC e, quando pertinente, ARCHITECTURE e código. Não bloqueie por decisões técnicas locais.
3. **Bloqueie a parte indefinida:** se faltar definição, houver ambiguidade/contradição ou a escolha não passar na delegação, não escreva o código dependente; explique a lacuna e peça decisão; continue o trabalho independente já autorizado.
4. **Registre antes de codificar** (decisão humana na SPEC ou decisão IA pendente com ID); ajuste a ARCHITECTURE se pertinente.
5. **Repita a checagem durante o trabalho.** Urgência, conveniência técnica ou testes passando não autorizam desvio silencioso.
6. **Valide aderência:** rode primeiro os testes mais próximos da área alterada e amplie conforme o risco, validando o ciclo integrado desde cedo; revise diff, efeitos colaterais e testes contra a SPEC, inclusive decisões IA pendentes. Se código e SPEC divergirem, não altere a SPEC para justificar o código sem decisão de produto.

Não é exigido issue, plano, PR, ADR, TDD ou documento extra por mudança — é exigido que o comportamento esteja aprovado ou estritamente delegado e registrado antes do código.

**Pronto** = comportamento coberto pela SPEC, dentro do recorte vigente, funcionando conforme o contrato, validado adequadamente e sem divergência conhecida entre código e SPEC.

### Entrega incremental

Construir o **Convencional** em versões menores, integradas e jogáveis (direção de 2026-10-11; recorte V1 aprovado em 2026-10-12 na SPEC), respeitando dependências e validando cedo economia, ações do jogador e consequências reais. Manter a visão completa sem tentar codificá-la antes da primeira partida utilizável. Limitar o catálogo não autoriza dinheiro, estoques, agentes, deslocamentos ou consequências fictícias, nem POCs isoladas obrigatórias. Exemplos da exploração não ampliam o escopo; mudança de escopo exige decisão humana e SPEC atualizada antes. O modo **Criativo** é intenção futura: não implementar, especificar nem criar camadas técnicas para ele agora. A frente de assets segue o pedido do coordenador e não introduz regras de produto.

## 3. Critérios de produto em toda decisão

Antes de propor, especificar, implementar ou revisar algo, aplique os princípios de [`docs/spec/principios.md`](docs/spec/principios.md) e responda explicitamente:

- **Gameplay x microgerenciamento x custo interno:** o que acrescenta a decisões e consequências interessantes? Quantos cliques, etapas e esperas impõe? Quantos estados, exceções e testes cria por dentro? Regras existentes ou eventos entregam o mesmo com menos?
- **Diversão de construir:** como a consequência aparece no mundo para quem só quer construir, sem ler texto? Onde controle, espera ou erro frustram? (sem falsificar sistemas para parecer infantil)
- **Vida dos SIMs e empresas:** quem é afetado, por qual mecanismo real, e como o jogador percebe?
- **Causalidade e diagnóstico:** existe caixa-preta, automação invisível ou agregado que não se decompõe? Se sim, a decisão fica em exploração até haver diagnóstico.
- **Classificação:** melhor como **política municipal opcional** (escolha interessante e consequência verificável), **regra automática da simulação**, **ação direta de construção/gestão** ou **não incluir**? Avaliar não é aprovar.
- **Configurável?** Só expor opção quando houver valor real para calibração, acessibilidade, debug ou gameplay; registrar a justificativa.

A direção da Prefeitura (ampliar escolhas estratégicas do jogador e reavaliar gratuidades/tarifas antigas) está em [`docs/spec/prefeitura-financas.md`](docs/spec/prefeitura-financas.md).

## 4. Conversa com o responsável

- Trate o que ele disser como entrada ativa de produto. Afirmação factual verificável: pesquise antes de registrar; prefira fontes primárias, documentação oficial, dados públicos e benchmarks reproduzíveis.
- Decisão explícita → atualize a fonte de verdade na hora, verifique coerência global (conflitos, regras antigas, consequências contraditórias) e corrija na mesma sessão; conflito que não dá para resolver com segurança → avise antes de assumir. Documentação desatualizada → corrija; atualize o que mudou em vez de acumular transcrição.
- Sem evidência ou decisão suficiente, registre como aberto e diga o que precisa ser pesquisado ou medido.
- **Perguntas autoexplicativas:** comece com um caso real simples, mostre A/B/C sem abstrações nem jargão, com recomendação e desvantagem claras. Antes de perguntar, confira SPEC e exploração para não repetir o que já foi decidido; diferencie casos parecidos já decididos.
- Agrupe decisões realmente relacionadas em pacotes coerentes; não agrupe escolhas independentes de modo que uma resposta aprove requisitos distintos por acidente.
- Priorize lacunas que alterem comportamento, gameplay ou coerência sistêmica; deixe parâmetros pequenos para calibração. Em entrevista de definição, siga em rodadas curtas enquanto houver decisões relevantes e encerre quando as perguntas passarem a ter baixo valor.
- Apresente opções com prós, contras e o que cada uma sacrifica; reabra a decisão se surgir consequência nova relevante. **Raciocine criticamente:** não valide uma hipótese só porque o responsável sugeriu; questione premissas, confronte com SPEC, fluxos econômicos e custo de simulação, procure híbridos; diferencie simplicidade conceitual de custo real.
- "Decidido/aprovado/confirmado" só descreve decisão do responsável ou o que está na fonte canônica. Resposta condicionada a dúvida ("não entendi", "e daí?") não é decisão. Benchmark de outros jogos não autoriza copiar políticas, subsídios, multas ou burocracia.

## 5. Exploração é pesquisa, não autoridade

**Decisão do responsável em 2026-10-16:** `docs/EXPLORATION.md` e `docs/exploration/*.md` são documentos de pesquisa registrados por IAs, não fonte de verdade, e em geral não serão revisados por humano; não se exige rótulo de revisão por documento (rótulos antigos ficam sem efeito).

- **Serve para consulta:** antes de pesquisar um tema, propor uma mecânica ou decidir, procure no hub pesquisa, alternativas descartadas ou evidência existentes e aproveite-as; confira se ainda valem, pois SPEC e código prevalecem. Para definição/priorização de produto, consulte também [`docs/exploration/project-readiness.md`](docs/exploration/project-readiness.md), sem reabrir decisões da SPEC só porque aparecem como pendência antiga (diferencie decisão necessária, calibração e validação por protótipo; atualize o mapa só quando uma decisão mudar as prioridades).
- Nada da exploração vira requisito, decisão, backlog, plano, instrução para agentes ou autorização de implementação/cópia — **mesmo que o texto diga "implementar", "usar", "reaproveitar" ou "próxima etapa"**. Só ganha autoridade com aprovação explícita do responsável registrada na fonte canônica; na dúvida, não aprovado.
- Escreva hipóteses como opções de estudo e diga, em uma frase, quando apresentar material de pesquisa.
- Organização: comece pelo hub (só nome, status curto, escopo e link); atualize o documento temático existente; agrupe por domínio durável, não por conversa; não crie arquivo cedo demais nem deixe um crescer misturando domínios; preserve evidências e alternativas rejeitadas, não transcrições; marque o que foi decidido, superado ou segue aberto; ao promover algo, registre o resultado conciso na fonte canônica e aponte para ela.

## 6. Sem herança automática

O IndexCities nasce do zero (diretriz expressa do responsável). **Não copiar, portar, importar ou reutilizar** código, algoritmos, testes, arquivos, assets/modelos, texturas, dados, configurações, interfaces, pipelines ou documentos de projetos ou experiências anteriores; licença permissiva, compatibilidade ou sugestão em exploração não autorizam — exceção só por decisão humana expressa e específica. Sem pedido específico, não pesquisar repositórios, commits, issues ou acervos antigos; relatos históricos são só lições, não pistas para minerar componentes. Material externo pode informar uma investigação permitida, mas não é herdado como funcionalidade, arquitetura, dependência ou recurso.

## 7. Entregas, revisão e QA

- Quem coordena (qualquer agente designado pelo responsável) implementa ou delega, integra e joga cada entrega. A missão permanece aberta até validação humana da experiência global, inclusive após PRs intermediários.
- Cada entrega integrada passa por reprodução com evidências, desenvolvimento, testes e cenários reais no Godot, revisão crítica independente, correções e nova verificação proporcional.
- **Revisão independente:** revisor de **modelo diferente do autor** (preferência atual: DeepSeek V4.1 Flash; se indisponível, Claude Haiku 5.5). Recebe diff, SPEC aplicável, relato humano e evidências; verifica regressões, integração, clareza, gameplay e soluções superficiais. Registrar modelo efetivo, limitações, achados e correções; sem revisão real, a entrega não está validada. Após duas rodadas com problema importante persistente, expor pendência e alternativas.
- Manter uma entrega integrada principal, sem limitar trabalhos independentes úteis em workspaces isoladas com fronteiras explícitas. Reproduzir cedo o cenário original e integrar amostras visuais cedo; tentativas sem melhora pedem reavaliar a direção. Reutilizar evidências de código inalterado e repetir verificações conforme o risco; evitar revisões separadas que só repetem a da integração.
- **Saves:** preservar os saves do jogador e usar saves isolados na validação. As cidades de avaliação atuais são descartáveis (orientação de 2026-10-15): não interromper para pedir backup; isso não remove requisitos de persistência nem autoriza apagar outros arquivos.
- **QA no Godot:** argumentos da engine (`--path`, `--position`, `--resolution`) antes de `--`; argumentos do jogo (`--*-smoke`, `--review-save-dir=...`) depois. Liste os cenários, o que verificam, se exigem janela real e de que cidade precisam com `-- --qa-list`; `--review-save-dir` deve ser caminho absoluto dentro de `<raiz>/artifacts/verification`. Atalhos via `scripts/Play.ps1` também usam o separador: `-GodotArguments '--',('--review-save-dir='+$saveFolder)`. O banner da engine ou `-DryRun` não provam o cenário: confira marcadores/capturas e o destino dos saves antes de declarar teste executado.
- **Orquestração multiagente** (lições de 2026-10-16, para qualquer orquestrador): uma worktree isolada por worker e uma branch integrada; remover worktrees e fechar terminais após o merge; acompanhar progresso real, não só o aviso de conclusão (~15 min sem mudança → cutucar uma vez, depois reatribuir; detectar pedidos de permissão); escolher o modelo pela dificuldade, conferir cota antes de despachar e registrar o modelo efetivo; pedido com alvo, limites e aceitação verificável, incluindo cenário Godot executado de verdade; estudo que embasa decisão do responsável passa por verificação de um segundo modelo.

## 8. Filosofia

Resultado observável e feedback rápido; a menor mudança coerente; planejamento proporcional ao risco; testes quando protegem comportamento, nunca cerimoniais; sem handoffs artificiais entre papéis de IA; processo novo só para dor recorrente e observável. Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
