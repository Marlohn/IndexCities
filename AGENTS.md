# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso útil sem transformar desenvolvimento em burocracia.

## Fonte de verdade

- `docs/SPEC.md` define o **produto decidido** e o escopo atual.
- `docs/EXPLORATION.md` guarda **pesquisa, ideias, alternativas e decisões ainda em discussão**.
- `docs/GENRE_BENCHMARK.md` guarda a **referência comparativa externa do gênero**, usada periodicamente para confrontar o IndexCities com aprendizados, falhas e expectativas observados em outros city builders; não define requisitos.
- O código e o histórico deste repositório definem o estado real da implementação.

Antes de uma mudança relevante, leia a SPEC e somente o código necessário para entender o alvo. Consulte a EXPLORATION quando a tarefa depender de uma discussão ainda aberta ou do raciocínio que levou a uma decisão.


## Fluxo contínuo de conversa e documentação

Durante conversas de exploração ou definição com o responsável pelo projeto:

- trate o que ele disser como entrada ativa de produto, não apenas como contexto de conversa;
- quando houver afirmação factual verificável, pesquise e confirme antes de registrá-la como fato;
- prefira fontes primárias, documentação oficial, dados públicos e benchmarks reproduzíveis; não use suposição como substituto de evidência;
- quando houver uma decisão explícita, atualize a fonte de verdade apropriada imediatamente, sem esperar um pedido separado de documentação;
- decisões de produto vão para `docs/SPEC.md`;
- hipóteses, ideias, alternativas, referências, dúvidas e resultados de pesquisa vão para `docs/EXPLORATION.md`;
- regras de desenvolvimento e de trabalho vão para `AGENTS.md`;
- se a documentação existente ficar desatualizada, contraditória ou incompleta à luz da conversa atual, corrija-a na mesma sessão;
- não transforme suposição em requisito: quando não houver evidência ou decisão suficiente, registre como aberto e indique o que precisa ser pesquisado ou medido;
- mantenha a documentação enxuta: atualize o que mudou em vez de acumular transcrições da conversa;
- quando a conversa estiver sendo usada como entrevista de definição do produto, faça perguntas curtas em sequência, registre cada rodada e continue para a próxima até o responsável pedir para parar;
- antes de formular novas perguntas, consulte `docs/SPEC.md` e `docs/EXPLORATION.md` e evite repetir perguntas já respondidas ou decisões já registradas;
- antes de implementar um comportamento, parâmetro ou sistema novo, avalie explicitamente se ele deve ser configurável/opcional. Não crie opções por padrão: registre a justificativa e só exponha configuração quando a variabilidade tiver valor real para calibração, acessibilidade, debug ou gameplay;
- sempre que registrar uma nova decisão, verifique a coerência global com `docs/SPEC.md` e `docs/EXPLORATION.md`, procure conflitos, regras antigas ou consequências contraditórias e corrija a documentação na mesma sessão. Se existir conflito real ou ambiguidade que não possa ser resolvida com segurança, avise explicitamente o responsável antes de assumir uma resposta.

## Arquitetura de especificação

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

`docs/EXPLORATION.md` é o espaço de pensamento.

Use para:

- pesquisar uma ideia;
- comparar alternativas;
- registrar referências, prós e contras;
- manter perguntas ainda abertas;
- preservar por que uma opção foi aceita ou descartada.

Quando uma discussão fecha, **a decisão final entra resumida na SPEC**. A EXPLORATION pode preservar o raciocínio e as evidências, mas não substitui a SPEC.

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

Até isso acontecer, **uma SPEC + uma EXPLORATION + este AGENTS.md continuam sendo a arquitetura central**. Documentos auxiliares só devem existir quando resolvem uma dor concreta; `docs/GENRE_BENCHMARK.md` existe especificamente para manter a pesquisa comparativa extensa fora da EXPLORATION e servir como referência periódica de validação.


## Princípio de gameplay e microgerenciamento

Para qualquer decisão de produto, avalie explicitamente o custo de microgerenciamento contra o valor de gameplay.

- Realismo não é objetivo suficiente por si só: detalhe adicional precisa criar decisão, consequência, leitura sistêmica ou feedback interessante.
- Preserve profundidade sistêmica quando ela gera causa e efeito observável, especialmente produção, logística, emprego, trânsito, capacidade, escassez e finanças.
- Abstraia ou automatize tarefas repetitivas que exigem cliques/contabilidade sem produzir decisões significativas.
- Prefira interfaces agregadas para leitura e decisão quando a simulação puder manter o detalhe físico internamente.
- Não remova logística, escassez ou consequências apenas para simplificar; simplifique a operação manual, não necessariamente a simulação.
- Ao comparar alternativas, registre quando uma opção é mais realista porém pior de jogar, ou mais simples porém destrói consequências importantes.
- Aplique este critério de forma genérica a todos os sistemas, não apenas a materiais e construção.

## Regra principal de escopo

- **Funcionalidade nova de produto precisa estar na SPEC antes de ser implementada.**
- Se uma solicitação introduzir comportamento novo ainda não especificado, atualize a SPEC como parte da mesma mudança e então implemente.
- Bug que apenas restaura comportamento já especificado pode ser corrigido diretamente.
- Experimentos e POCs podem acontecer rapidamente fora da SPEC enquanto estiverem isolados. Só viram produto quando forem aceitos e incorporados à SPEC.
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
5. Rode a validação relevante para a mudança.
6. Revise o diff final e confirme que não adicionou escopo não pedido.
7. Atualize a SPEC somente quando o produto mudou.

## Definição prática de pronto

Uma mudança está pronta quando o comportamento pedido funciona, a validação adequada passa e não existe divergência conhecida entre implementação e SPEC.

Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
