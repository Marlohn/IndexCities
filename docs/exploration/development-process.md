# IndexCities — Processo de desenvolvimento e documentação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** decidido para a fase atual; histórico e justificativas preservados aqui — não é fonte de verdade.
>
> Este documento preserva a pesquisa que levou ao processo leve de desenvolvimento, às fronteiras documentais e à arquitetura de trabalho. Regras vigentes para humanos e IAs pertencem a `AGENTS.md`; decisões estruturais vigentes pertencem a `docs/ARCHITECTURE.md`.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Processo de desenvolvimento com IA

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido para a fase atual.

### Dor observada

Experiências anteriores mostraram que muita cerimônia de processo — múltiplas etapas, papéis, documentos, branches e handoffs — pode aumentar custo de tokens e tempo sem produzir melhora proporcional no produto.

Ao mesmo tempo, desenvolvimento totalmente sem limites pode causar deriva de escopo, decisões esquecidas e arquitetura desnecessária.

### Alternativas consideradas

- **Spec Kit:** processo forte e completo, mas pesado demais para a fase atual.
- **OpenSpec:** mais leve e flexível, porém ainda adicionaria proposal/spec/design/tasks e estado próprio antes de existir uma dor que justifique isso.
- **BMAD:** orientado a vários papéis e etapas; incompatível com a busca atual por velocidade.
- **Skills especializadas:** ideias úteis de review, retro e execução assistida, mas sem necessidade de adotar um workflow inteiro.
- **Sem processo:** máxima velocidade, mas pouca proteção contra deriva de produto.
- **Spec leve própria:** uma SPEC curta como cerca, com execução agressiva dentro dela.

### Conclusão

O IndexCities adota **Spec-Driven Development (SDD) leve**: a especificação vem antes do desenvolvimento, sem transformar cada mudança em um processo pesado.

Na prática:

- uma única SPEC curta e autoritativa;
- ARCHITECTURE para decisões estruturais;
- AGENTS.md com regras de trabalho;
- EXPLORATION para pesquisa e decisões ainda abertas;
- implementação rápida depois que a intenção estiver definida;
- validação proporcional ao risco;
- nenhum framework adicional de SDD por enquanto.

A pesquisa confirmou que SDD normalmente significa colocar intenção e especificação antes da implementação. Ferramentas como Spec Kit estruturam isso em várias etapas, mas o IndexCities adota apenas o princípio necessário, sem copiar a cerimônia inteira.

### SPEC viva durante todo o ciclo do jogo — direção confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou a regra do processo em conversa; a redação consolidada pela IA ainda não foi revisada integralmente. Regras oficiais estão em `AGENTS.md`.

O contrato da SPEC **não termina com a primeira implementação**. Ele vale para cada feature, mudança de regra, correção de bug, refatoração e manutenção futura. Antes de escrever código, a IA deve localizar a regra já documentada que a mudança realiza ou preserva. Encontrar comportamento ausente, ambíguo ou contraditório **bloqueia somente o código dependente da lacuna**: expor o problema, apresentar consequências/opções e obter a decisão humana; registrar a decisão na SPEC antes de implementar. Não há permissão para completar regra de produto por suposição.

Um bug cujo comportamento correto já está claro na SPEC pode ser corrigido usando essa regra, sem gerar alteração cerimonial no documento. Da mesma forma, refatorações e otimizações podem escolher detalhes internos compatíveis com a arquitetura, desde que preservem o comportamento especificado. Uma decisão estrutural pertence à ARCHITECTURE, mas nunca substitui a aprovação de gameplay na SPEC.

**Referência exploratória nova — revisão humana desta nota: PENDENTE.** O GitHub Spec Kit diferencia modelos em que a especificação é descartada após o código, preservada como âncora de mudanças futuras ou mantida como contrato vivo atualizado antes das mudanças. O IndexCities usa a SPEC persistente e viva, mas **não adota o framework** nem artefatos adicionais por isso. Referências: [Spec Persistence Models](https://github.com/github/spec-kit/blob/main/docs/concepts/spec-persistence.md) e [Spec Kit](https://github.github.com/spec-kit/). Essas referências servem como contexto de pesquisa, não como novas regras.

### Primeira entrega integrada — direção confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou **SPEC antes do desenvolvimento**, **primeira implementação integrada** e **validação global posterior**; as considerações técnicas abaixo ainda não foram revisadas integralmente.

**Decidido (regras vigentes em `AGENTS.md`):** não existe roteiro obrigatório de POCs de produção, economia, interface, mapa ou outro sistema. A primeira entrega busca integrar o produto conforme a SPEC; a validação principal de gameplay e interação entre sistemas considera o **conjunto funcional**. Recomendações antigas de "primeira POC" em documentos temáticos são pesquisa histórica, não cortes de escopo autorizados.

**Distinção importante:** entrega integrada **não exige todo o código num único commit nem desenvolvimento sem qualquer teste até o final**. Incrementos técnicos, verificações e testes proporcionais ao risco podem ocorrer durante a implementação. Isso não cria POCs de produto nem marcos burocráticos; ajuda a evitar erros difíceis de localizar na integração global.

**Risco a acompanhar (análise ainda pendente):** integrar muitos sistemas profundos sem observar suas interações pode encarecer correções. Preservar invariantes e diagnóstico causal durante a implementação, sem trocar a estratégia de entrega integrada por etapas de produto isoladas.

### Princípio de evolução

Só adicionar processo quando for possível apontar uma dor real que ele resolve.

Exemplos de sinais para reconsiderar a arquitetura:

- SPEC grande ou difícil de navegar;
- agentes perdendo contexto entre sessões;
- divergências recorrentes entre decisão e código;
- trabalho paralelo causando conflitos;
- necessidade real de rastreabilidade requisito → implementação → teste.

Até lá, simplicidade é uma característica do processo, não uma deficiência.

---

---

## Pesquisa — desenvolvimento com múltiplas IAs e guardrails (2026-10-11)

> **Revisão humana desta seção: PENDENTE.** Pesquisa e recomendações da IA, **não aprovadas como regras de trabalho**. Não substituem `AGENTS.md`, `SPEC.md` ou `ARCHITECTURE.md`.

### Resposta curta

**Usar a IA mais cara em tudo não é necessário; usar a mais barata indiscriminadamente também é um erro.** O caminho recomendado é **modelos conforme risco + tarefas delimitadas + verificações executáveis + escalonamento quando necessário**. O custo real é o da tarefa **aceita e correta**, incluindo tentativas, revisão e regressões, não o preço por token.

O repositório já possui a maior parte do desenho conceitual: SPEC viva, `AGENTS.md`, arquitetura de monólito modular, Simulation Core em C# puro, Godot separado e proposta de execução headless. **Em 2026-10-11 ainda não há código do jogo nem CI**, portanto os guardrails de código abaixo são **propostas para ativar quando houver implementação**, não ferramentas já existentes.

### O que a pesquisa externa sugere

- O estudo prático da OpenAI sobre uma base escrita inteiramente por agentes aponta **repositório navegável, limites arquiteturais verificáveis, testes, observabilidade e feedback** como mais úteis que pedir a um modelo que “tenha cuidado”. É um caso interno, não uma garantia de que todos os jogos terão os mesmos resultados.
- A orientação do GitHub para código gerado por IA recomenda **compilar, executar testes e análise estática antes de revisar intenção e arquitetura**. Checks exigidos no GitHub podem impedir integração com testes falhando quando configurados.
- A DORA 2025 descreve IA como **amplificadora** do processo existente, não solução automática para processo frágil.
- Pesquisas de 2026 sobre `AGENTS.md` **divergem**: uma observou menos tempo/tokens com instruções; outra encontrou pior sucesso e maior custo com contexto excessivo. Conclusão prudente: instruções **curtas e indispensáveis**, com referências para aprofundar somente o tema da tarefa. No IndexCities, `AGENTS.md` e principalmente a SPEC já são extensos; vale medir se agentes estão encontrando a seção certa, **sem cortar regras oficiais às cegas**.
- Roteamento entre modelos e escalonamento por dificuldade mostram potencial em avaliações recentes (NVIDIA, 2026), mas **os ganhos publicados não são previsão de economia para IndexCities**. O experimento METR (2025) também mostra que IA pode aumentar o tempo em certos projetos maduros: custo e velocidade precisam ser medidos na tarefa real.

### Proposta prática para o IndexCities

1. **Contexto dirigido à tarefa.** A IA verifica `AGENTS.md`, localiza as regras pertinentes na SPEC, a fronteira relevante na ARCHITECTURE e só então lê o código necessário. O pedido contém **objetivo, seção da SPEC, escopo, restrições, como validar e quando parar**. Não despejar a SPEC inteira no prompt repetidamente nem criar uma segunda fonte de verdade.
2. **Mudanças pequenas e coerentes.** Pedir uma unidade de comportamento por vez, incluindo os testes necessários; evitar “implemente todo o sistema econômico” em uma chamada. Isso é incremento técnico dentro do **jogo integrado**, não retorno a POCs setoriais obrigatórias. Sem refatorações extras ou funcionalidades não decididas.
3. **Guardrails que executam, não apenas aconselham.** Quando existir código, começar com `dotnet build`, `dotnet test` e um teste simples de dependência proibida de `Godot.*` no Simulation Core. Acrescentar verificações conforme surgirem regras/erros reais: **conservação de dinheiro e materiais nos fluxos aprovados**, proprietários sem duplicidade, capacidade/estoques não negativos quando a regra exigir, e cenários determinísticos de bug. Não perseguir porcentagem de cobertura.
4. **Integração proporcional ao risco.** Rodar testes próximos da mudança; testes de integração ao cruzar fronteiras; Godot headless/importação ao alterar cenas/host; benchmark quando alterar algoritmo, escala ou rota. Quando houver pipeline, executar no GitHub checks relevantes antes de integrar mudanças de maior risco. Agent não deve ter acesso desnecessário a secrets nem autorização irrestrita para publicar alterações destrutivas. Não exigir PR, checklist ou revisão em várias etapas para toda correção trivial.
5. **Revisão diferente de geração.** Para alterações críticas, outra IA pode revisar **somente o diff, as regras aplicáveis e os resultados dos testes**, procurando violações concretas; o autor corrige. Revisão automática ajuda, mas não comprova conformidade de gameplay, especialmente se autor e revisor repetirem a mesma interpretação errada da SPEC. Questões novas de produto voltam ao responsável.
6. **Falha reproduzível em vez de palpite.** Sempre que possível, entregar seed/cenário/estado inicial, comando, resultado esperado e observado. Teste de regressão deve demonstrar o erro antes e a correção depois. Não deixar um agente “consertar” o bug mudando a regra esperada.
7. **Controle de tentativa e escalonamento.** Se houver repetição do mesmo erro, diff crescente, alteração de vários domínios, testes que não explicam a falha ou necessidade de inventar regra, **parar e escalar** em vez de consumir ciclos baratos indefinidamente. Falta de decisão de produto não é problema para um modelo maior adivinhar.

### Qual IA para qual trabalho? (ponto de partida, não regra fixa)

| Nível | Tarefas candidatas | Condição para aceitar |
| --- | --- | --- |
| **Econômica** | Busca de contexto, documentação exploratória, mudanças mecânicas, scripts locais simples, testes de casos já especificados, ajustes visuais isolados | Diff pequeno; aceitação objetiva; validação automatizada ou conferência direta |
| **Intermediária** | Implementação localizada de regra aprovada, UI/adapter delimitado, integração pequena, correção de bug reproduzível | Testes da área e revisão de fronteiras afetadas |
| **Mais capaz** | Decisões técnicas difíceis, bugs sem causa clara, interação economia–SIMs–logística, persistência/migração, desempenho, mudanças transversais de arquitetura | Análise explícita de riscos; testes e, quando necessário, revisão independente |

**Importante:** modelo econômico pode alterar até código crítico **se a alteração for estreita e houver verificações suficientemente fortes**. Modelo caro também erra; não recebe passe livre para alterar produto ou pular testes. Quando a falha tem custo alto, começar no nível superior pode ser mais econômico que várias tentativas baratas.

### Como testar se está funcionando sem virar burocracia

Após existir um pequeno conjunto de tarefas reais e repetíveis, comparar **dois ou três modelos** em categorias representativas do próprio repositório (por exemplo: UI simples, regra financeira, bug reproduzível e integração). Registrar só **resultado aceito, regressões detectadas, número de tentativas, custo total e tempo até passar**. Reavaliar periodicamente porque modelos/preços mudam. **Não criar já um orquestrador, enxame de agentes, scorecard permanente ou framework de prompts**; introduzir automação somente se a escolha manual de modelo virar dor recorrente.

**Próximo passo recomendado (ainda não aprovado):** manter o processo atual; junto ao primeiro código C#/.NET criar build/testes e proteção da fronteira Godot/Core; adicionar mais guardrails quando houver uma falha concreta ou um risco recorrente. Evitar mexer agora na SPEC (produto) ou ARCHITECTURE (decisão estrutural) com base apenas nesta pesquisa.

### Fontes principais consultadas

- [OpenAI — Harness engineering (fev. 2026)](https://openai.com/index/harness-engineering/) — caso prático de desenvolvimento por agentes e restrições verificáveis.
- [GitHub — Review AI-generated code](https://docs.github.com/en/copilot/tutorials/review-ai-generated-code) e [Required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets) — testes, inspeção e bloqueios de integração.
- [GitHub — Instruções por repositório e AGENTS.md](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions) — suporte e escopo dependem da ferramenta.
- [Google DORA 2025](https://research.google/pubs/dora-2025-state-of-ai-assisted-software-development-report/) — efeito da IA condicionado ao processo.
- [Gloaguen et al., 2026 — Evaluating AGENTS.md](https://arxiv.org/abs/2602.11988) e [Lulla et al., 2026 — Impact of AGENTS.md](https://arxiv.org/abs/2601.20404) — evidências divergentes; evitar generalização.
- [NVIDIA — Routing de modelos por tarefa/etapa/escalonamento (ago. 2026)](https://developer.nvidia.com/blog/route-ai-agent-workloads-across-models-with-nvidia-nemo-switchyard/) — evidência prática de trade-off de custo/qualidade, fora do contexto específico do jogo.
- [METR, 2025 — estudo randomizado de produtividade](https://arxiv.org/abs/2507.09089) — custo de revisão e possível perda de eficiência em certas tarefas.
- [OpenAI — Running Codex safely (mai. 2026)](https://openai.com/index/running-codex-safely/) — privilégios e limites proporcionais ao risco.

---

## Pesquisa — orquestração multiagente e sessões longas (2026-10-11)

> **Revisão humana desta seção: PENDENTE.** Recomendação exploratória, não é regra oficial de trabalho. `AGENTS.md`, `docs/SPEC.md` e `docs/ARCHITECTURE.md` permanecem autoritativos. Estudo externo, **não experimento medido no IndexCities**.

### Recomendação em uma frase

**Orquestração leve e seletiva, não enxame permanente:** uma sessão/agent responsável por integrar o trabalho, **um implementador de cada mudança dependente**, revisor independente conforme o risco e **paralelo somente para tarefas com fronteiras estáveis e verificáveis**. Um orquestrador pode inicialmente ser apenas a IA principal consultando Git/CI e delegando quando oportuno; **não precisa ser serviço, framework ou agente sempre ligado**. Usar modelos de fornecedores diferentes é viável **se** o ambiente/integrações permitirem executá-los, cada um operar sobre o mesmo commit-base e a revisão/integração não depender de conversas privadas. Nenhuma plataforma específica foi escolhida.

### Evidência, limites e alternativas

- **Agente único, sequencial:** menor custo de coordenação e melhor continuidade local; para uma mudança curta e acoplada, geralmente preferível.
- **Paralelismo irrestrito no mesmo código:** pode reduzir tempo de calendário, mas multiplica mudanças incompatíveis, repetição, custo de tokens e trabalho de merge. **Anthropic Research (2025)** relata ~15 vezes os tokens de chat em seu sistema multiagente; ressalta que **pesquisa é mais paralelizável que a maior parte da programação**. Esse número **não se aplica diretamente** ao IndexCities.
- **Paralelismo isolado:** no experimento de **16 agentes construindo um compilador C (Anthropic, fev. 2026)**, tarefas independentes progrediram bem, mas todos tentando resolver um único bloqueio da compilação do kernel produziam correções concorrentes e sobrescritas. O experimento consumiu ~US$ 20 mil em API: prova de viabilidade **não de custo/benefício para um jogo**. Isolar agentes e aplicar testes fortes foram decisivos.
- **Planejador → implementador → avaliador:** experimento da Anthropic (mar. 2026) ganhou robustez ao separar execução de avaliação e ao usar handoffs verificáveis. Porém sua versão com 3 agentes podia consumir muito mais tempo e custo (exemplo de $9 / 20 min vs $200 / 6 h). Não adotar três papéis para todo commit. **No IndexCities, nenhum planejador pode ampliar a SPEC por conta própria.**
- **Sessões longas:** estudos da Anthropic (nov. 2025/mar. 2026) apontam falhas quando o contexto cresce: tentar tudo num único prompt, parar no meio ou declarar tudo concluído. Reiniciar contexto com **handoff estruturado e estado real do repositório** pode ser melhor que insistir na mesma conversa; compaction ajuda mas não substitui checar Git/testes. Experimento OpenAI Codex (fev. 2026) também enfatiza memória durável **fora do chat**.

**Não confundir duas formas de paralelismo:** (a) *paralelo de leitura/pesquisa/revisão*, frequentemente seguro e barato de integrar; (b) *paralelo de código*, apropriado somente com contratos e arquivos/domínios suficientemente separados. **Nunca tratar consenso entre modelos como teste de correção.**

### Fluxo recomendado para começar (proposta)

1. **Um responsável pela integração** (agente principal; pode ser modelo mais capaz no planejamento de mudanças complexas). Lê regras atuais, seleciona uma mudança pequena coberta pela SPEC e explicita entradas, saídas e dependências. Não há permissão para inventar funcionalidade.
2. **Executar prioritariamente em série:** uma IA implementa a menor parte coerente em branch/worktree, roda os testes pertinentes e entrega diff, resultado e pendências. Agente barato quando a tarefa tiver aceitação clara; modelo mais forte quando envolver causalidade cruzada ou investigação complexa.
3. **Revisão independente por risco:** outro modelo **somente lê** regras/alterações/testes, busca violações de SPEC, quebra de invariantes e dependências proibidas. Revisão de IA não substitui teste nem validação de gameplay. Se encontrar algo, devolver ao implementador, sem dois agentes corrigindo o mesmo arquivo às cegas.
4. **Integrar uma mudança por vez:** testes locais, integração dos contratos afetados, checks de CI quando existirem, regressões reproduzíveis. `main` deve permanecer recuperável. Para trabalho paralelo, cada escritor trabalha numa branch ou `git worktree` separado, em commit-base identificado, sem acesso irrestrito a mudanças dos outros; integrar apenas após atualizar a base, resolver conflitos e validar novamente.
5. **Aumentar paralelismo por evidência:** começar com 1 implementador e revisor sob demanda; experimentar 2 implementadores somente quando houver tarefas de baixo acoplamento e baseline que compila. Não fixar N agentes/etapas como regra permanente.

**Exemplos IndexCities:** produzir catálogo/validação de assets ou UI demonstrativa **sobre um contrato já fechado** em paralelo a testes independentes do Core pode fazer sentido. Duas IAs alterando simultaneamente transferência de dinheiro, Reserva Global, propriedade e tributação **não**: esses subsistemas partilham invariantes, mesmo que estejam em arquivos diferentes. No começo, contratos mínimos e infraestrutura de compilação vêm antes de delegar amplamente sobre interfaces ainda móveis. Isso **não altera** a primeira entrega integrada definida na SPEC.

### Problema dos prompts one-shot e como atravessar sessões

**One-shot por unidade de trabalho, não one-shot do jogo inteiro.** Uma tarefa boa já chega com comportamento da SPEC, arquivos/fronteiras prováveis, critérios verificáveis e condição de parada. O agente deve primeiro confirmar o estado de Git e testes, depois executar. Ao acabar, não pedir novo código genérico em sequência no contexto já carregado: encerrar a unidade com resultado verificável e iniciar outra sessão quando a tarefa ou o contexto tiver mudado materialmente.

**Fonte de continuidade:** (1) SPEC/ARCHITECTURE/AGENTS vigentes; (2) código e commits; (3) testes e saídas da CI; (4) **handoff curto da tarefa corrente** — em descrição de PR/issue ou nota temporária no branch quando realmente necessário — contendo apenas *commit/base, objetivo e seção da SPEC, arquivos/contratos tocados, o que foi validado, estado real, falhas e próximo passo*. Não usar resumo de conversa como autoridade nem copiar histórico de raciocínio. Remover/encerrar notas temporárias após integração; se handoffs recorrentes falharem, considerar um registro leve de estado de implementação, **sem criar agora uma segunda SPEC ou uma esteira obrigatória**.

**Ao iniciar nova sessão:** conferir branch/HEAD e diff; ler regras pertinentes e handoff; executar um comando curto de validação; confrontar o estado descrito com o existente; escolher **uma** mudança. Se o agente disser que concluiu sem teste, divergir da SPEC ou solicitar decisão já registrada, não confiar na narrativa — voltar às fontes e evidências.

### Qual modelo coordena? E quando escalar?

O **orquestrador não precisa ser o mais caro sempre**: delegação simples, controle de status e coleta de diffs podem ser determinísticos ou feitos por modelo econômico. Usar um modelo mais forte para decompor tarefa inédita/ambígua ou revisar risco alto, nunca para **autorizar gameplay** ausente. Modelos diferentes servem também como revisão independente, embora seus erros possam ser correlacionados. Escalar diante de tentativas repetidas, diff crescente, mudança de contratos centrais, erro sem reprodução, resultado sem teste ou suspeita de incompatibilidade entre módulos. **O ganho relevante é custo/tempo por mudança integrada e correta**, não tokens ou linhas de código por minuto.

**Teste de adoção após o primeiro código real:** comparar pequeno conjunto de tarefas representativas resolvidas com agente único vs implementador+revisor vs duas tarefas independentes em paralelo, usando custo completo, tempo até integração, conflitos e defeitos encontrados. Se os ganhos não superarem a coordenação, manter agentes sequenciais. **Não automatizar orquestração complexa antes de essa comparação indicar necessidade.**

### Fontes primárias

- [Anthropic — How we built our multi-agent research system (jun. 2025)](https://www.anthropic.com/engineering/multi-agent-research-system): forte paralelismo em pesquisa; custo de tokens e limitações em código.
- [Anthropic — Building a C compiler with a team of parallel Claudes (fev. 2026)](https://www.anthropic.com/engineering/building-c-compiler): isolamento, colisões, testes e custo real.
- [Anthropic — Effective harnesses for long-running agents (nov. 2025)](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents): initializer, handoff e retomar por Git/validação.
- [Anthropic — Harness design for long-running application development (mar. 2026)](https://www.anthropic.com/engineering/harness-design-long-running-apps): resets de contexto, avaliação separada e custo.
- [OpenAI — Run long horizon tasks with Codex (fev. 2026)](https://developers.openai.com/blog/run-long-horizon-tasks-with-codex) e [Harness engineering (fev. 2026)](https://openai.com/index/harness-engineering/): memória externa, verificações e worktrees.
- [OpenAI — Orchestration and handoffs](https://developers.openai.com/api/docs/guides/agents/orchestration): agente gestor e especialistas como ferramentas.
- [Git — git-worktree](https://git-scm.com/docs/git-worktree): múltiplas árvores isoladas do mesmo repositório.

---

## Arquitetura de desenvolvimento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decisão consolidada.

A pesquisa e a direção arquitetural inicial foram consolidadas em [ARCHITECTURE.md](../ARCHITECTURE.md), que passa a ser a autoridade sobre organização técnica, fronteiras e direção de dependências.

Conclusões principais:

- monólito modular, sem microserviços/processos distribuídos como requisito;
- núcleo de simulação em C#/.NET puro, sem dependência de Godot;
- Godot como host/adaptador e camada de apresentação;
- UI como leitura + comandos, sem possuir o estado autoritativo da cidade;
- assets e conteúdo visual desacoplados das regras de gameplay;
- capacidade de execução headless para testes, benchmarks e ferramentas/agentes;
- ECS, multithreading, event bus, DI e outras estruturas adicionais permanecem adiados até existir profiling ou necessidade concreta.

A pesquisa que sustentou essas decisões está resumida no próprio documento de arquitetura. Novas alternativas ou revisões ainda não decididas voltam a ser exploradas aqui antes de alterar a arquitetura oficial.

---

---

## Organização dos documentos de exploração

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido e aplicado.

A organização oficial é:

```
docs/
  SPEC.md
  ARCHITECTURE.md
  EXPLORATION.md
  exploration/
    player-interface.md
    genre-benchmark.md
    <tema>.md
```

Regras:
- `SPEC.md` continua sendo a fonte de verdade do produto;
- `ARCHITECTURE.md` continua sendo a fonte de verdade estrutural;
- `EXPLORATION.md` é o hub/índice da exploração;
- pesquisa extensa, alternativas e rascunhos temáticos ficam em `docs/exploration/<tema>.md`;
- documentos temáticos declaram que não são fonte de verdade e indicam status;
- decisões fechadas são resumidas na SPEC e o material exploratório é marcado como decidido/superado quando aplicável;
- evitar novas subpastas ou arquivos por ritual;
- novos arquivos soltos em `docs/` precisam de uma função especial recorrente e justificável.

A migração é gradual: não é necessário desmontar o conteúdo histórico deste hub de uma vez. Conforme um tema voltar a ser trabalhado, ele pode ser limpo, consolidado e movido para um documento temático.

`exploration/genre-benchmark.md` também fica em `docs/exploration/`. Apesar de ter uso comparativo recorrente, continua sendo material de pesquisa/referência e não uma fonte canônica.

A primeira migração aplicada foi a interface do jogador para [`exploration/player-interface.md`](player-interface.md).
