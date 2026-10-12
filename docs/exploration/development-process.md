# IndexCities — Processo de desenvolvimento e documentação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** histórico de decisões já registradas nas fontes canônicas e de propostas de operação ainda não aprovadas. **Este arquivo não é fonte de verdade, checklist de implantação nem instrução obrigatória para IAs.**
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

## Auditoria final do workflow de IA — 2026-10-11

> **Revisão humana desta seção: PENDENTE.** Auditoria de fontes e recomendação final da IA; não altera regras oficiais. **Esta seção prevalece como síntese exploratória mais recente sobre as recomendações de ferramentas das seções anteriores**, que registram alternativas e raciocínio histórico. Nenhuma ferramenta local foi executada nem seus limites reais medidos nesta auditoria.

### Veredito

**A base técnica e documental existente é suficiente para começar e continuar a manutenção do jogo; as lacunas são de operação, validação, custo e segurança, não de necessidade de outra arquitetura.** Na branch padrão, o repositório ainda contém apenas arquivos de documentação, sem solução do jogo, testes executáveis nem `.github/workflows`: nada foi validado em Godot/.NET ou com as configurações locais informadas pelo responsável. Não declarar a stack "testada" a partir de documentação.

**Padrão inicial recomendado, ainda não aprovado:** Orca como ambiente; um executor por mudança (Codex CLI ou outro agente comprovado), Claude Code em revisão sob demanda, OpenCode + DeepSeek/API ou modelos gratuitos em tarefas econômicas delimitadas; Hermes para consultas históricas/pesquisa ocasionais; Hindsight em *experimento opt-in*, não em todas as sessões. **Sem papéis permanentes nem 3 agentes simultâneos por padrão.** O usuário possui **Hermes Agent**, não "Amp": Amp é somente alternativa externa pesquisada.

### Correções e achados omitidos nas rodadas anteriores

1. **Orca não é só visualização.** Existe [orquestração própria experimental](https://www.onorca.dev/docs/cli/orchestration): runs, tasks, dependências, dispatches, workers supervisionados e decision gates. O `Run` **não agenda nem inicia agentes por si só**; é necessário criar tarefas e disparar workers. Não implementar orquestrador próprio/Oh My OpenCode por desconhecer esse recurso. Para uma tarefa única, o próprio Orca sugere `orca terminal send`; para continuidade breve, há [checkpoints em comentários da worktree](https://www.onorca.dev/docs/cli/worktree-checkpoints). Testar orquestração nativa **somente quando coordenação manual virar gargalo**.
2. **Segurança do Orca é risco imediato, não detalhe opcional.** A [documentação](https://www.onorca.dev/docs/agents/supported) confirma launch padrão com bypass de permissões dos CLIs, inclusive Codex/Claude/Hermes; worktree **não** é sandbox de acesso ao computador. Antes do uso, selecionar **Settings → Agents → Agent Permissions → Manual**, revisar overrides por agente e desativar **Settings → Agents → Trust the folder when Orca starts an agent** se se desejar o prompt de confiança nativo. Conferir ferramentas `bash`, `edit` e acesso externo do [OpenCode](https://opencode.ai/docs/permissions/). Não expor chaves de API nos prompts, commits, logs ou memória.
3. **Hindsight tem custos e efeitos secundários concretos.** O [README oficial](https://github.com/vectorize-io/hindsight/blob/main/hindsight-integrations/coding-agents/README.md) confirma `autoInject: reflect`, `codebaseSurvey: true`, `retainSessions: true`, refresh programado de páginas e `autoUpdate: true` nos defaults do coding-agent integration. `autoInject: none` evita injeção automática **mas não desliga retenção de sessões**; `retainSessions: false` evita retenção e também afeta estatísticas dependentes do write-back. `autoInject: recall/pages` consulta sem LLM de síntese no momento, mas **ingestão/consolidação/atualizações podem continuar usando LLM**. Não chamar Hindsight "grátis" por executar localmente: infraestrutura/extração local exigem compute e frequentemente credencial LLM; cloud tem precificação e envio de dados.
4. **Memórias Hermes ≠ Hindsight compartilhado automaticamente.** Hermes [mantém memória nativa mesmo com provedor externo](https://github.com/NousResearch/hermes-agent/blob/main/website/docs/user-guide/features/memory-providers.md), e seu plugin tem **bank_id padrão `hermes`**; a integração Hindsight coding-agents usa **`coding-agent::{gitProject}`** por padrão. Logo, **não há compartilhamento garantido** entre Hermes e os outros agentes apenas porque usam a marca Hindsight. Só sincronizar se configurar conscientemente um banco comum, permissões e escopos; evitar capturar duas vezes a mesma conversa. Hermes alerta que dois processos escrevendo no **mesmo perfil/home** podem corromper o significado da memória; usar perfis distintos ([fonte](https://github.com/NousResearch/hermes-agent/blob/main/website/docs/user-guide/features/memory.md)).
5. **Benchmark Hindsight não é economia comprovada no IndexCities.** A tabela de 61 tarefas × 3 rodadas existe no README do fornecedor (Codex 0,577→0,276 USD; Claude 0,445→0,338; OpenCode 0,634→0,553), mas usa tarefas externas e a taxa de conclusão praticamente não muda. **Não presumir** que o custo total de cloud/daemon, ingestão, correções e manutenção esteja incluído; não fazer projeção percentual para este repositório. A ferramenta tem mudanças rápidas e issues recentes: registrar versão, manter `autoUpdate: false` no experimento e consultar compatibilidade antes de atualizações.
6. **Custo de contexto é real, mas tamanho em linhas não mede tokens.** `AGENTS.md` (286 linhas) e SPEC (mais de 1.000 linhas, ~326 kB) exigem navegação seletiva. Manter leitura das regras obrigatórias conforme AGENTS e localizar **seções pertinentes** da SPEC; não ignorar nenhuma regra oficial, nem reduzir AGENTS por conta própria antes de auditar duplicações e pedir revisão. Começar com `rg`/índice de headings, `git log` e testes; só avaliar QMD (busca documental) e Serena (símbolos C#) se a busca atual for cara, lenta ou falhar.
7. **CI e ambiente reproduzível não podem ficar só na teoria.** Ao nascer a solução, fixar versões compatíveis do **Godot com .NET** e SDK, dotnet build/test e verificação de importação/execução headless do Godot quando a engine estiver envolvida. Godot C# requer editor .NET e SDK instalados, e `--headless` tem comandos próprios ([documentação Godot](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html), [CLI](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)). GitHub Actions em runners padrão é **gratuito para repositórios públicos**; privado depende do plano/cotas ([GitHub](https://docs.github.com/en/billing/concepts/product-billing/github-actions)). Acesso de workflows deve usar permissões mínimas e segredos não devem ser enviados a tarefas de agentes sem necessidade.
8. **Continuidade e verificabilidade:** Git/commit + SPEC + ARCHITECTURE + testes + seed/logs têm precedência sobre memória e descrição do agente. Handoff breve: base, seção aprovada, resultado efetivo, testes executados/falhas e próxima ação. **Não salvar conclusões provisórias de agentes como decisões oficiais**, especialmente trabalhos paralelos ainda não integrados. Status da worktree é temporário e não substitui o commit; após merge, handoff obsoleto não pode guiar novas alterações.
9. **Custos de modelos:** distinguir assinatura do ChatGPT/Claude, **cotas de CLIs**, créditos API do DeepSeek e despesas do Hindsight. Não assumir que um login dá API ilimitada ou que monitor de uso de uma ferramenta mostra todas as cobranças. Regra de orçamento sugerida: **sem fallback silencioso para API paga**, limitar tentativas repetidas e subir para modelo forte diante de falha reproduzível complexa. Medir **custo por alteração correta integrada**, não tokens por mensagem; incluir revisão, memória, retrabalho e CI quando cobrado.
10. **Não substituir validação visual e de gameplay por testes automatizados.** O núcleo C# testável não demonstra cena 3D correta, fluidez de edição, leitura visual para criança nem integração sistêmica. Preservar a decisão de validação principal global ao completar o jogo integrado, com verificações técnicas/visuais proporcionais ao longo da construção.

### Possível roteiro avaliado na pesquisa — não aprovado como plano de execução

- **Antes do primeiro agente escrever:** verificar versão real das instalações e contas/cotas; alterar permissões no Orca e revisar segurança de credenciais; registrar que *não existe ainda* build/CI no repositório padrão.
- **Primeira unidade técnica:** usar a SPEC/ARCHITECTURE para estruturar somente a solução mínima de Godot .NET + Core C# sem `Godot.*`, build/testes reais e o menor CI que os reproduza. Um executor; revisão separada se risco justificar. Isso é início do **jogo integrado aprovado**, não POC setorial obrigatória.
- **Sessões seguintes:** uma missão objetiva, contexto pontual, 1 escritor, validação local, diff conferido, commit e handoff em checkpoint/PR somente se necessário. Experimentar revisão independente de outro modelo em mudanças de estado econômico e arquitetura.
- **Piloto de memória:** comparar algumas tarefas de continuidade com Git/rg/Hermes FTS e depois com Hindsight em **uma CLI** e somente no IndexCities. Usar `optInOnly` + `optInPaths`, `autoUpdate: false`, `codebaseSurvey: false`, `pageTriggerType: manual`; escolher conscientemente `retainSessions` e `autoInject` conforme o teste (por exemplo, `recall` restrito em vez de `reflect` automático). **Não ligar tudo e chamar qualquer melhora de causal**; registrar versão, dados retidos, gastos e se houve redução real de retrabalho.
- **Só depois:** testar dois escritores em worktrees independentes, e, se as dependências começarem a exigir controle recorrente, avaliar **o recurso de orquestração já presente no Orca**, antes de construir ou adotar mais um. Introduzir Serena/QMD e memória compartilhada ampla apenas diante de evidência de retorno positivo.

**Ponto de parada da pesquisa:** não há evidência suficiente de que outra assinatura, MCP adicional ou orquestrador permanente melhore o projeto agora. Melhor próximo conhecimento vem de **build, sessões e custo medidos no próprio IndexCities**.

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

## Pesquisa — escolha de ferramentas para começar (2026-10-11)

> **Revisão humana desta seção: PENDENTE.** Avaliação de mercado e recomendação operacional da IA, não processo aprovado. Preços, autenticação, limites e compatibilidade de ferramentas podem mudar; conferir os links oficiais antes de pagar. Não altera a SPEC nem a ARCHITECTURE.

### Alternativa de ferramentas estudada — não é configuração aprovada

**Hipótese formulada na época da pesquisa (não aprovada):** usar **Orca** como central visual de agentes/terminais/diffs/worktrees, **Codex CLI** (login ChatGPT) como implementador principal, **Claude Code** (login Claude Pro/Max) para revisão independente e tarefas complexas, e **OpenCode** com chave **DeepSeek API** ou modelos gratuitos disponíveis para tarefas baratas e delimitadas. **GitHub + AGENTS + SPEC + ARCHITECTURE + testes** continuam sendo as autoridades duráveis, não a memória das ferramentas.

**Por quê:** essas ferramentas já estão ao alcance do responsável, permitem usar assinaturas existentes sem necessariamente contratar novas, e separam **coordenação visual** (Orca) da **inteligência do agente** (Codex/Claude/OpenCode). Orca oferece suporte declarado a CLI Codex, Claude Code, OpenCode e outros, worktrees e visualização de diffs. **Não é garantia de orquestração automática inteligente:** no início, a IA implementadora ou o responsável coordena as tarefas. Uma sessão/branch por escritor; revisão pode ser só leitura.

| Ferramenta | Valor real | Limite e decisão recomendada |
| --- | --- | --- |
| **[Orca](https://www.onorca.dev/)** | Central gratuita/open source para worktrees, vários CLI agents, diffs, GitHub e terminais (Windows/macOS/Linux) | **Principal para operação**; não confundir interface de múltiplos agentes com planejador correto |
| **[Codex](https://openai.com/codex/)** | Ferramenta oficial de código incluída em planos ChatGPT, CLI/IDE e cloud em contas elegíveis | **Principal implementador** inicialmente, com quotas do plano; não presumir API ilimitada |
| **[Claude Code](https://support.claude.com/en/articles/11145838-use-claude-code-with-your-pro-or-max-plan)** | CLI oficial usando assinatura Pro/Max dentro das cotas | **Revisor e segundo executor independente**; conferir para não acionar cobrança de API por `ANTHROPIC_API_KEY` no ambiente |
| **[OpenCode](https://opencode.ai/docs/providers/)** | Cliente multi-provedor e subagentes; conectividade oficial com DeepSeek API, OpenAI/ChatGPT e modelos gratuitos sujeitos a mudança | **Operação econômica**; chave DeepSeek gera consumo no saldo da API |
| **[DeepSeek Harness](https://www.deepseek.com/en/harness/)** | Ferramenta plugável com sessões rastreáveis, subagentes e equipes experimentais | **Alternativa experimental**, não adicionar sobre Orca sem problema demonstrado; produto em preview |
| **[Amp](https://ampcode.com/news/free-agent)** | Sistema de agentes com runners próprios (modalidade gratuita anunciada em set. 2026), assinaturas/chaves externas e opção de computadores remotos | **Alternativa séria ao fluxo principal** se Orca for insuficiente; registro histórico da interpretação equivocada de “Arms”; o responsável esclareceu que possui Hermes Agent, não Amp |
| **[Google Antigravity](https://www.antigravity.google/pricing)** | Ambiente de agentes e cota individual gratuita | **Opção para benchmark** sem nova assinatura, não criar segundo fluxo permanente |
| **[Oh My OpenCode](https://github.com/code-yeongyu/oh-my-opencode)** | Especialistas, tarefas paralelas, orquestrador sobre OpenCode | **Adiar**: mais automação/complexidade do que o repositório vazio necessita |
| **[Cursor](https://www.cursor.com/pricing)** | IDE comercial com agentes e multitarefa | Bom produto, mas **sem argumento para outra assinatura agora** |
| **[Vibe Kanban](https://www.vibekanban.com/blog)** | Painel para agentes em worktrees | **Não escolher** como nova base: projeto comercial encerrado, manutenção repassada à comunidade em abr. 2026 |

**Ressalva de autenticação que evita gastos e problemas:** Codex via cliente oficial com login ChatGPT e Claude Code via cliente oficial com login Claude são o caminho recomendado para aproveitar assinaturas. O OpenCode declara que uso de **Claude Pro/Max por plugins não oficiais é proibido pela Anthropic**; usar Claude Code no Orca, e não OAuth Claude dentro de OpenCode. OpenCode conecta DeepSeek pela **API key**, com cobrança ao saldo da API; as assinaturas de chat não são automaticamente créditos API. Alguns agentes e modelos gratuitos mudam de disponibilidade; não montar dependências críticas sobre promoções temporárias.

### Exemplo de configuração pesquisada — não executar automaticamente

1. Abrir o repositório **IndexCities** no Orca e conferir branch/estado limpo do Git. Instalar/atualizar **Codex CLI** e autenticá-lo pelo login ChatGPT; abrir um terminal Codex dentro do projeto. Instalar/atualizar **Claude Code** e fazer login no plano Pro/Max; deixá-lo como revisor quando preciso. No **OpenCode**, usar `/connect` → DeepSeek → API key, depois `/models`; impor limite de gasto pela conta DeepSeek. Não colocar segredos no repositório.
2. **Primeira tarefa sugerida (técnica, sem nova gameplay):** com base em AGENTS/SPEC/ARCHITECTURE, criar o mínimo de solução/projetos Godot .NET/C# com Simulation Core independente e base de build/testes; nenhuma regra nova de produto ou framework adiantado. Codex implementa; Claude revisa apenas fronteiras/diff; resolver falhas e integrar. Isso é **fundação da implementação integrada**, não POC isolada obrigatória.
3. **Sessões seguintes:** um objetivo pequeno coberto pela SPEC, um escritor, evidência de `dotnet build`/`dotnet test` e teste Godot apenas se tocou a engine. Agentes secundários para revisão, busca de documentação, scripts de assets e código sem acoplamento. Paralelizar dois escritores **somente** depois de interfaces estáveis, em worktrees isolados, revalidando no merge.
4. **Handoff curto:** commit/branch atual, referência à seção da SPEC, alterado, testes, falhas, próximo passo. Nova sessão compara resumo com Git real; a memória da conversa não substitui arquivos nem testes. Handoff em issue/PR/branch temporário só se necessário; não criar uma nova documentação canônica.
5. **Reconsiderar ferramentas depois de tarefas reais:** se a coordenação manual do Orca virar gargalo, testar primeiro a orquestração experimental nativa do Orca; outras ferramentas só depois de medir a necessidade. Medir resultado correto integrado, tempo, custo e conflito, não número de agentes rodando.

### Links de verificação

- [Orca — integrações, worktrees e diff](https://www.onorca.dev/)
- [OpenAI — Codex com plano ChatGPT](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan)
- [Anthropic — Claude Code com Pro/Max](https://support.claude.com/en/articles/11145838-use-claude-code-with-your-pro-or-max-plan)
- [OpenCode — provedores e observação de restrição Claude Pro/Max](https://opencode.ai/docs/providers/)
- [DeepSeek Harness — estado de preview](https://github.com/deepseek-ai/deepseek-harness)
- [Amp — anúncio de uso gratuito com runners próprios](https://ampcode.com/news/free-agent)
- [Antigravity — planos](https://www.antigravity.google/pricing)
- [Vibe Kanban — anúncio de encerramento da empresa e manutenção comunitária](https://www.vibekanban.com/blog)

---

## Reavaliação — Hermes Agent, Hindsight e economia de tokens (2026-10-11)

> **Revisão humana desta seção: PENDENTE.** Pesquisa técnica, não decisão vigente. Corrige a identificação anterior: o responsável possui **Hermes Agent**, não Amp. Não houve benchmark executado no IndexCities.

### Recomendação revista

**Stack candidata:** Orca como interface de worktrees/sessões; Codex CLI como implementador inicial; Claude Code como revisor para mudanças arriscadas e executor de tarefas independentes; OpenCode/DeepSeek para tarefas econômicas; **Hermes Agent** para pesquisa, busca histórica e automações ocasionais; **Hindsight** como experimento de memória compartilhada com limites de custo. Não manter todos trabalhando continuamente nem construir orquestrador próprio ainda.

- **Hermes Agent:** memória curta própria (MEMORY.md/USER.md), busca de sessões FTS5 local, skills sob demanda, subagentes, modelos auxiliares para atividades baratas, comando `/usage`. A busca de texto não faz chamadas adicionais a LLM; já pode responder muitas questões de continuidade sem uma memória externa. Hermes dispõe de plugin Hindsight com instalação `hermes plugins install hindsight` e seleção `hermes memory setup`. Isso pode coexistir com memória nativa, mas pode duplicar retenção e contexto.
- **Hindsight Coding Agents:** pacote `@vectorize-io/hindsight-coding-agents` integra Codex, Claude Code, OpenCode, DeepSeek Harness e outros, com banco por repositório compartilhado entre os agentes e worktrees. Recomendo conectar **um agente primeiro** e só expandir se houver economia medida, em vez de `install all`.
- **Custo escondido nos defaults do Hindsight:** `autoInject: reflect`, `codebaseSurvey: true`, `retainSessions: true`, cinco páginas de conhecimento com síntese recorrente e `autoUpdate: true`. Podem melhorar memória, mas também causar chamadas, atrasos e mudanças invisíveis. Teste de baixo custo: no arquivo local `~/.hindsight/coding-agent.json`, habilitar `optInOnly: true` e `optInPaths` apenas para o IndexCities; experimentar `autoInject: none`, `codebaseSurvey: false`, `pageTriggerType: manual`, `gitIngest: message`, `retainExtractionMode: concise`, `autoUpdate: false`. Captura de sessões e consolidação são custos adicionais: controlar e comparar o que se ganha com o que se gasta.
- **Evidência do fornecedor:** a integração publica 61 tarefas × 3 execuções, com menos correções e custos médios Codex (US$ 0,577→0,276), Claude (US$ 0,445→0,338) e OpenCode (US$ 0,634→0,553), sem ganho material na taxa de conclusão. **Não é estudo feito no jogo**; medir memória + tokens de execução + retrabalho.
- **Maior ganho de tokens pode ser contexto menor:** SPEC ultrapassa 1.000 linhas e AGENTS.md tem 286. A OpenAI relata que `AGENTS.md` monolítico dificulta recuperação e eleva custo. **Hipótese para aprovação:** auditar tamanho e redundância, conservar todas as regras canônicas, facilitar leitura seletiva da SPEC por título/assunto e tornar AGENTS um mapa compacto que preserve os limites obrigatórios. Não alterar a fonte canônica sem revisão humana.
- **Opções novas pesquisadas:** **QMD** (busca Markdown local com BM25/índice semântico, útil se `rg` deixar de ser suficiente); **Serena MCP** (localizar/editar símbolos de código com menor leitura de arquivos, especialmente C# quando o Core crescer). Não ligar as duas nem vários MCPs por padrão: cada ferramenta tem custo operacional e ocupa contexto.
- **Guardrail urgente:** Orca informa lançamento padrão com bypass de permissões. Recomendo **Settings → Agents → Agent Permissions → Manual**, revisando flags personalizadas, antes de agentes terem acesso amplo à máquina. Worktree não contém efeitos sobre rede, segredos e arquivos fora do checkout.
- **Outra economia:** no Hermes, rotear modelos auxiliares simples a modelos baratos e usar busca diferida de ferramentas; no OpenCode, restringir permissões por agente e remover outputs antigos de ferramentas/compactar quando útil. Cache de prefixos da API pode diminuir custo, mas regras API não se transferem automaticamente a assinaturas de CLIs.
- **Arquitetura para anos de manutenção:** Git, SPEC, ARCHITECTURE, testes, seed/log de reprodução e código são memória operacional; Hindsight é memória auxiliar sujeita a erro/desatualização. Cada sessão deve identificar objetivo, contrato, diff e teste e encerrar ao completar a unidade. Uma revisão independente só quando risco/custo justificarem. Orquestrar paralelamente apenas trabalho desacoplado.

### Teste econômico sugerido

Começar com **um executor e sem serviços extras**; preparar build/testes C# e validações objetivas do Godot conforme a tarefa. Comparar tarefas representativas primeiro com Git + instruções canônicas + busca Hermes, depois com Hindsight restrito em um agente; registrar sucesso, correções, custo total inclusive de memória, tempo e violações de SPEC. Se Hindsight compensar, conectar outros agentes à mesma memória por repo. Só testar QMD/Serena se houver custo de busca real. Não instalar um segundo orquestrador sem esse sinal.

### Fontes

[Hermes Agent](https://github.com/NousResearch/hermes-agent) · [Hermes memory](https://hermes-agent.nousresearch.com/docs/user-guide/features/memory) · [Hermes/Hindsight](https://hermes-agent.nousresearch.com/docs/plugins/hindsight) · [Hindsight coding agents](https://github.com/vectorize-io/hindsight/blob/main/hindsight-integrations/coding-agents/README.md) · [Orca permissions](https://www.onorca.dev/docs/agents/supported) · [OpenCode permissions](https://opencode.ai/docs/agents/) · [QMD](https://github.com/tobi/qmd) · [Serena](https://github.com/oraios/serena) · [OpenAI Harness Engineering](https://openai.com/index/harness-engineering/).

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
