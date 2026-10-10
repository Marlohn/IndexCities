# IndexCities — Pesquisa de testes, diagnóstico e reprodução de bugs

> **Revisão humana: PENDENTE.** Pesquisa e recomendação técnica produzidas por IA em 2026-10-09. **Não é fonte de verdade nem decisão de arquitetura/produto**; propostas dependem de revisão. O comportamento aprovado está na [SPEC](../SPEC.md), as fronteiras técnicas vigentes na [ARCHITECTURE](../ARCHITECTURE.md), e as regras de trabalho no [AGENTS](../../AGENTS.md).
>
> **Pergunta:** como descobrir, reproduzir e evitar bugs num jogo em que milhares de SIMs, empresas e veículos agem autonomamente, inclusive com tempo acelerado, sem sobrecarregar a simulação ou criar um processo burocrático?

## Complemento da pesquisa: novas conclusões (2026-10-09)

> **Revisão humana desta ampliação: PENDENTE.** Investigação interdisciplinar solicitada após a pesquisa inicial. Referências e técnicas abaixo são **evidência, exemplos ou hipóteses para avaliar**, não decisões de produto, obrigações técnicas nem software já existente. Preservar o que está decidido em SPEC/ARCHITECTURE/AGENTS.

**Mudança de conclusão:** o melhor investimento não é apenas guardar logs e seeds. É conseguir **detectar o primeiro estado incorreto, preservar o contexto, reproduzir a mesma execução e diminuir o cenário até revelar uma causa**. O laboratório deve executar o **mesmo Simulation Core** que o jogo usa, sem virar outra engine.

| Descoberta | Relevância / custo | Direção proposta |
| --- | --- | --- |
| **Simulação adversarial determinística**, inspirada em FoundationDB e TigerBeetle | Muito alta; requer controlar relógio, RNG, agenda e entradas | Explorar com o Core headless, sem simulador duplicado |
| **Teste por propriedades com ações válidas geradas** | Muito alta para milhares de combinações econômicas; moderada | Começar por dinheiro, posse, estoque e agenda; ampliar por risco |
| **Redução automática de caso que falhou** (*shrinking/delta debugging*) | Alta quando o bug envolve longa sequência | Experimentar depois que houver reprodução estável |
| **Teste de sobrevivência do estado após save/load**, inclusive em transições | Muito alta | Desde o primeiro save; modo pesado só sob demanda |
| **Comparação diferencial** entre velocidades, FPS, cache, mudanças de scheduler | Muito alta para aceleração real | Comparar efeitos e ordem causal, não só valores agregados |
| **Verificação antecipada de integridade e progresso** | Alta; custo depende da frequência | Checar invariantes baratas com frequência, varreduras profundas só em teste |
| **Recibos causais por entidade/evento e buffer circular** | Alta; custo baixo a moderado quando seletivo | Exportar causas reais sem logar cada SIM a cada frame |
| **Mutation testing do próprio conjunto de testes** | Médio; caro se aplicado indiscriminadamente | Usar para provar que guardrails essenciais de fato detectam erros |
| **Instrumentação do host e testes Godot** | Necessários para entrada/cenas/assets, mas caros vs Core | Concentrar nos riscos que só existem no Godot |
| **Validação de gameplay integrada** | Insusbtituível para ritmo e microgerenciamento | Diferente de correção técnica; teste automatizado não prova diversão |

**Três qualidades distintas:** (1) integridade interna — moeda, propriedades, recursos e tempo não se corrompem; (2) conformidade com **comportamentos já aprovados** na SPEC — consequências corretas, mesmo quando desagradáveis; (3) **qualidade de gameplay** — ritmo, leitura, liberdade e microgerenciamento, que exigem teste humano do conjunto integrado. Os métodos de verificação/validação de simulações ajudam a separar essas perguntas, sem importar um processo pesado da NASA para o projeto.

---


## 1. Conclusão

**Recomendação para avaliação:** combinar **testes rápidos no Simulation Core + invariantes do mundo + cenários headless com seed controlada + save/checkpoint e replay de comandos + logs causais sob demanda**. Acrescentar testes de cache, desempenho e interface somente à medida que esses sistemas existirem.

O maior diferencial não seria ter muitos testes unitários, mas conseguir responder: **“Qual foi a primeira transição inválida, em qual entidade, com quais entradas, em qual tick, e como executá-la de novo?”**

**Estado atual verificado em 2026-10-09:**
- O repositório contém documentação, mas **ainda não contém código do jogo, projetos de teste nem CI**. Logo, nenhuma ferramenta ou cobertura de testes está implementada.
- A [ARCHITECTURE §§ 11–13](../ARCHITECTURE.md#11-testes) **já define** Core C# testável por `dotnet test`, testes de integração seletivos, Godot apenas quando necessário, execução headless, instrumentação, seed, pacote de reprodução de bug e aleatoriedade explícita. **Não reinventar nem duplicar essa decisão**.
- A [SPEC](../SPEC.md#mapa-seed-e-limites) exige seed determinística para o **mapa/cidade-base**; a [SPEC de saves](../SPEC.md#saves-e-persistência) prevê múltiplos saves e autosave configurável. A [SPEC de tempo](../SPEC.md#tempo-de-jogo) exige aceleração que preserva os acontecimentos reais, não saltos fictícios.
- **Novo valor desta pesquisa:** detalhar *como validar* essas decisões e onde estão os riscos, sem escolher antecipadamente toda a infraestrutura.

## 2. O que testar, na prática

| Camada | Exemplo útil para IndexCities | Frequência sugerida |
| --- | --- | --- |
| **Regras isoladas (unitário)** | aluguel vencido mantém dívida correta; compra não duplica saldo; escola cheia não aceita outra matrícula | Durante alterações da regra |
| **Invariantes (propriedades globais)** | dinheiro total fixo; material não aparece sem origem; não há dois donos exclusivos de um mesmo bem; ocupação não excede capacidade aprovada | Em testes do Core e cenários relevantes |
| **Integração de sistemas** | falta de energia reduz produção, afeta estoque/entrega/receita sem inventar recursos; falência distribui dinheiro real e encerra obrigações conforme SPEC | Ao alterar fronteiras afetadas |
| **Cenários headless** | cidade-semente executa dias/meses com construção, contratação, tráfego, pagamentos, mortes e migração; falha se alguma regra essencial quebrar | Marcos de integração e, quando barato, automação |
| **Save/load/replay** | executar, salvar, restaurar e continuar: estado econômico e decisões causais seguem coerentes | Sempre que mudar persistência/scheduler/RNG |
| **Godot e UI** | clique vira comando único, posição de obra correta, cenas abrem, alertas exibem causas reais | Só em mudanças que envolvam host, cenas ou interface |
| **Desempenho** | ms/tick, filas, memória, rotas, custo do diagnóstico, save/load, velocidades 1/2/3 | Benchmarks separados; não bloquear toda edição por ruído |

**Invariante não é “o saldo de todo SIM nunca pode ficar negativo”**: a SPEC permite dívidas. Distinguir saldo, dívida, estoque, crédito e insuficiência de caixa. Uma regra de teste tem de refletir o contrato real, não impor uma simplificação inventada.

**Testes baseados em propriedades:** em vez de escrever apenas “com três SIMs deu certo”, gerar muitas sequências válidas de pagamentos, compras, heranças, realocações e falências e verificar propriedades. Quando surgir um erro, guardar a seed e, quando possível, **reduzir a sequência ao menor caso que ainda falha**. [FsCheck](https://github.com/fscheck/FsCheck) é candidato .NET; não é necessário instalá-lo antes de um caso que mereça geração sistemática.

**Testes de relações (metamórficos):** checar propriedades sem exigir prever cada resultado complexo:
- Mesma build + mesmo checkpoint + mesmos comandos/ordem + estado completo de RNG e scheduler → mesmo resultado autoritativo em ambiente controlado.
- Salvar/carregar em um ponto e continuar → mesmo estado relevante que continuar diretamente, sob as mesmas condições.
- Velocidades 1 e 3, alcançando o **mesmo tempo simulado** com os **mesmos comandos aplicados nos mesmos instantes simulados**, não podem criar/perder operações por causa do FPS. A igualdade deve ser avaliada no estado autoritativo, não nas animações.
- Ligar/desligar ou invalidar corretamente um cache → **mesmo resultado causal**, se a otimização afirmar equivalência estrita.
- Mudar câmera ou ocultar objetos → **nenhuma mudança econômica/física no Core**.

Essas relações são especialmente valiosas para o IndexCities porque a resposta exata de uma cidade inteira é difícil de antecipar, mas há comportamentos que **jamais** podem mudar.

## 3. Seed ≠ save ≠ replay: como chegar “ao instante do bug”

**Seed do mapa:** reproduz o terreno inicial. **Não** reconstrói uma cidade depois de horas de escolhas, decisões automáticas e alterações de RNG.

**Save/checkpoint:** fotografa o **estado autoritativo completo** em determinado tempo. Para continuar corretamente pode precisar também de relógio, filas de eventos, jobs pendentes, estados de RNG, ordens de desempate, configurações, versão de schema e referências de entidades. Cache derivado pode ser reconstruído se isso não alterar comportamento. Um teste deve detectar estado necessário que ficou fora do save.

**Log de comandos:** anota ações *externas* que afetaram a simulação (construir, demolir, alterar velocidade quando relevante, carregar etc.), com tempo/tick, ordem e parâmetros. Decisões autônomas de SIMs **não precisam ser gravadas uma a uma se puderem ser recalculadas de modo reproduzível**. Se algum subsistema não puder garantir isso, capturar os resultados não determinísticos relevantes em um modo de diagnóstico direcionado.

**Pacote de reprodução recomendado (conceito, não formato final):**
1. identificador da build/commit, versão do save, plataforma/runtime e configuração;
2. seed inicial e **checkpoint próximo à falha**;
3. estados necessários de RNG e agenda/scheduler;
4. comandos desde o checkpoint, com tick e ordenação;
5. erro/invariante violada, ID(s) de entidade e janela temporal;
6. resumo de hashes/indicadores de estado e logs causais dessa janela.

**Fluxo de investigação desejado:** carregar checkpoint → repetir comandos em headless até o tick da falha → pausar/examinar entidade e causa → corrigir → incorporar o pacote ou caso reduzido à suíte de regressão.

**Localizar o primeiro ponto ruim:** guardar **assinaturas canônicas compactas** do estado autoritativo por domínio em intervalos escolhidos. Quando o replay divergir, comparar checkpoints, estreitar a janela e avançar passo a passo. Isso é melhor que comparar arquivos binários inteiros ou reter cada frame. Hashes **não explicam** o erro: apenas indicam onde aprofundar. Comparações semânticas por IDs e campos explicam a diferença. Frequência, custo e necessidade dos hashes devem ser medidos, não fixados por ritual.

**Limites reais:** não prometer replay idêntico entre sistemas operacionais, hardware, builds ou versões de regras diferentes. Ordem de coleções, números de ponto flutuante, concorrência, tempo de relógio real e eventos externos podem causar divergência. **Alvo inicial sugerido:** reprodutibilidade em **mesma build e ambiente controlado**. A ARCHITECTURE já diz que determinismo bit a bit multiplataforma **não é requisito**. Para regressão entre versões, comparar invariantes e resultados semânticos, não exigir que cada SIM faça escolhas idênticas após qualquer alteração de algoritmo.

**Atenção à RNG:** separar fontes explícitas por domínio ou operação, quando houver benefício, reduz o efeito cascata em que uma chamada aleatória extra muda toda a cidade. Porém é preciso persistir/derivar o estado corretamente e testar a ordenação; **“mesma seed” com consumo de RNG diferente não garante mesmo futuro**.

## 4. Logs bons sem destruir performance

Evitar `log` por SIM, a cada frame. Em vez disso, separar três camadas:

1. **Registro leve contínuo:** erros, avisos e eventos *significativos*, com código de causa e campos estruturados. Ex.: `rent_payment_failed`, tick 8120, locatário 42, imóvel 8, dívida +X, motivo `insufficient_funds`. Não guardar nomes pessoais nem string de texto para cada operação trivial.
2. **Métricas agregadas e checagens:** tempo por domínio/tick, filas atrasadas, CPU/alocações, dinheiro total, estoque total por tipo, quantidade de entidades, cache hit/miss/invalidation, tempo de rotas, indicadores de inconsistência. Amostragem quando necessária.
3. **Investigação direcionada:** ativar trace por `CitizenId`, `CompanyId`, `BuildingId`, área, domínio ou faixa de ticks; seguir cadeia *causa → decisão → efeito*, incluindo valores que **realmente** alimentaram a regra. Manter buffer circular limitado e exportar quando ocorrer falha.

**Exemplo de investigação:** uma fábrica não produz. O diagnóstico deve mostrar a causa real: “energia operacional recebida abaixo do necessário”, ou “sem combustível”, ou “sem trabalhadores em turno”; e, caso seja consequência de outra falha, permitir seguir o vínculo. Não adicionar um “índice de saúde da fábrica” desconectado da simulação.

**Padrão técnico a avaliar:** `Microsoft.Extensions.Logging` / `ILogger` com mensagens estruturadas, níveis e categorias; usar variantes de baixo overhead onde o profiling justificar. Instrumentação de duração/contadores pode usar ferramentas .NET existentes, sem inventar telemetria distribuída. Retenção, tamanho máximo e modo debug devem ser configuráveis **internamente** quando houver necessidade; não criar opções de gameplay por isso. [Microsoft — high-performance logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging).

### Ferramentas de debug (proposta enxuta, não feature do jogador)

A primeira interface pode ser **CLI headless + relatório textual**, sem menu complexo. Quando houver jogo integrado e dificuldade real de investigação, considerar um menu **somente de desenvolvimento** com: pausar/avançar um passo lógico, localizar SIM/empresa/prédio por ID, ver estado e causas, ligar trace de um alvo, salvar checkpoint e exportar pacote de bug. Isso é ferramenta técnica; **não** aprova rewind jogável, histórico completo de vida de cada SIM nem novos elementos na UI pública.

## 5. Guardrails que protegem o jogo

**Prioridade máxima — invariantes com ligação direta à SPEC:**
- **Moeda:** soma de todos os saldos monetários autoritativos, incluindo Reserva Global e outros recipientes aprovados, permanece coerente com a oferta fixa. Transferências não criam ou duplicam dinheiro; **dívida não é automaticamente saldo monetário**. Usar contabilidade de teste que conte cada saldo uma vez.
- **Recursos:** nenhuma produção, venda, importação, transporte ou consumo cria estoque físico sem origem/efeito autorizado; estoques e reservas não ficam incoerentes.
- **Identidade e exclusividade:** SIM/empresa/veículo/imóvel persistem com IDs estáveis; bens exclusivos e vagas não são atribuídos duas vezes de forma indevida.
- **Capacidade física:** disponibilidade de serviço, escola, fábrica, caminhão e infraestrutura obedece à capacidade efetivamente operacional aprovada.
- **Causalidade:** chegada, trabalho, compra, falência, morte e realocação respeitam tempo, ordem e precondições; nenhuma entidade muda de estado por estar fora da câmera.
- **Saves:** carregar não esquece dívida, fila, posse, relógio, RNG ou obrigação pendente que altera o futuro.
- **Tempo acelerado e cache:** não apagam movimento, eventos, prioridades, pagamentos nem consequências físicas/econômicas.

**Nem tudo é um teste binário:** crescimento populacional, lucro de empresas, desemprego e equilíbrio de tráfego variam legitimamente. Para esses aspectos, usar cenários, métricas, inspeção e limites justificados — não “valores mágicos” ou snapshots gigantes de toda a cidade que falhem em toda recalibração.

### Como uma feature entraria no fluxo

Exemplo: adicionar fábrica de automóveis, que já consta na SPEC. O implementador identifica as regras autorizadas → testa transações e capacidade relevantes → cria um cenário pequeno da fábrica e fornecedores/transportes → roda as integrações afetadas → confere invariantes → adiciona um caso de regressão se surgir defeito. **Nenhuma ferramenta de teste pode inventar comportamento de produto**; lacuna real volta à SPEC antes do código.

**Se uma correção alterar uma regra de gameplay não especificada, os testes não autorizam a mudança.** Valem as regras já aprovadas do AGENTS.

## 6. Estratégia progressiva, sem transformar testes em outra POC

| Quando houver... | Menor investimento útil |
| --- | --- |
| **Primeiras regras reais do Core** | um projeto de testes .NET; invariantes centrais; RNG explícita; IDs e relógio observáveis |
| **Primeiros fluxos com vários sistemas** | cenários headless pequenos e repetíveis, sem Godot; comparação save/load; logs de causa |
| **Primeira persistência e falha difícil** | checkpoint com estado completo + comandos ordenados + exportação de pacote de bug; trace direcionado |
| **Integração com Godot** | smoke tests de abertura, comando e cena; cobertura visual só onde o Core não basta |
| **Primeira otimização com cache/threads** | teste diferencial “com/sem otimização”; checagem de invalidação, ordenação e estado semântico |
| **Cidade integrada funcional** | cenários longos variados e validação global de gameplay; medir escala e velocidades reais |

**Automação sugerida quando houver código:** validação rápida de compilação/testes próximos da mudança; testes mais amplos nos pontos de integração e antes de versões; cenários longos/fuzz/benchmarks fora do ciclo de toda edição, eventualmente programados se valer o custo. Não criar agora CI vazio, “100% de cobertura”, centenas de casos artificiais, dashboards ou obrigação de rodar cidade inteira a cada commit.

**Cenários úteis:** cidade pequena com água/energia deficitárias; contratação e escola lotada; obra sem materiais e posterior entrega; empresa falindo; imóvel sem dono; congestionamento impedindo deslocamento; acidentes de contabilidade; save no meio de viagem/obra/fatura. Registrar seeds que revelarem falhas e **promover casos reais a regressões permanentes**. Exercitar variações de escala sem tentar “enumerar todos os mundos”.

**Desempenho:** separar correção de throughput. Usar profiler e contadores por domínio, benchmarks headless baseados em saves **representativos**, registrar build/hardware/cenário e comparar distribuições, não um único tempo. Uma instrumentação de log pode, ela própria, custar CPU — medir com ela ligada e desligada. Ferramentas a considerar quando úteis: [BenchmarkDotNet](https://benchmarkdotnet.org/), [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters), [dotnet-trace/EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) e profiler do Godot.

## 7. O que outros projetos realmente ensinam

| Referência primária | Evidência | Adaptação prudente |
| --- | --- | --- |
| [OpenTTD — debugging_desyncs](https://github.com/OpenTTD/OpenTTD/blob/master/docs/debugging_desyncs.md), [desync](https://github.com/OpenTTD/OpenTTD/blob/master/docs/desync.md) | comando ordenado + saves periódicos permitem replay; checagem especial de cache encontra divergência | **Referência mais direta** para pacote de bug e diagnóstico de cache |
| [Factorio — FFF #55](https://www.factorio.com/blog/post/fff-55), [#188](https://www.factorio.com/blog/post/fff-188) | hashes parciais de estado, comparação de saves, relatórios de desync | hashes baratos por domínio e diff semântico, **sem exigir multiplayer lockstep** |
| [Factorio — FFF #154](https://www.factorio.com/blog/post/fff-154), [#78](https://www.factorio.com/blog/post/fff-78) | testes gerados a partir de cenas e benchmarks por save/CLI | cenários construídos no próprio jogo podem virar fixtures; medir saves reais |
| [Cataclysm: DDA — CONTRIBUTING](https://github.com/CleverRaven/Cataclysm-DDA/blob/master/CONTRIBUTING.md) | suíte própria de testes automatizados integrada ao desenvolvimento | testes próximos das regras em jogo sistêmico com muitas interações |
| [Gaffer on Games — Deterministic Lockstep](https://gafferongames.com/post/deterministic_lockstep/) | replays requerem mesmo estado, entradas e ordem; floats/física podem divergir | não prometer replay multiplataforma nem “seed resolve tudo” |
| [FsCheck](https://github.com/fscheck/FsCheck) | testes baseados em propriedades e geração de casos .NET | experimentar quando surgirem estados combinatórios reais |
| [Godot — linha de comando/headless](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html), [gdUnit4](https://github.com/godot-gdunit-labs/gdUnit4), [gdUnit4Net](https://github.com/godot-gdunit-labs/gdUnit4Net) | execução automatizada e testes de cena/C# são possíveis | **não instalar addon já**; escolher apenas quando houver teste da camada Godot |
| [Microsoft — Testing in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/), [logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging) | testes filtráveis/CI e logs estruturados | usar ecossistema .NET do Core, sem framework próprio |

**Diferença crucial:** OpenTTD e Factorio investigam também sincronização multiplayer, que **não é requisito do IndexCities**. O que interessa aqui é a técnica de reconstrução e comparação, não copiar sua arquitetura de rede.

## 8. Alternativas e riscos

- **Só logs:** implementação simples, mas volume alto, custo de CPU/disco e impossibilidade de reproduzir um estado que não foi capturado. **Insuficiente sozinho.**
- **Só seed:** barato para mapas e cenários iniciais; não guarda ações, estado intermediário, agenda ou decisões. **Insuficiente.**
- **Salvar a cidade inteira a cada instante:** facilita voltar no tempo, mas custo de memória/I/O cresce com escala e aceleração. **Não recomendado por padrão.**
- **Gravar cada decisão de cada SIM:** replay potencialmente robusto contra variações, porém pode rivalizar em custo com a própria simulação e vira um segundo estado difícil de reconciliar. **Usar apenas trace seletivo se necessário.**
- **Checkpoint + comandos + estados necessários:** equilíbrio promissor entre reprodutibilidade e custo; depende de controlar RNG, agenda e ordem de execução. **Recomendação principal.**
- **Determinismo bit a bit universal:** caro e desnecessário para o escopo atual. **Não recomendado agora.**
- **Golden snapshots de cidade inteira:** detectam diferenças, mas podem congelar ajustes legítimos de gameplay. Preferir invariantes, casos focados e hashes semânticos direcionados.

## 9. O que ainda precisa de verificação antes de virar decisão técnica adicional

1. **Quanto custa o pacote de reprodução?** Medir tamanho e tempo de checkpoint, gravação de comandos e hashes numa cidade pequena, média e grande **depois que existirem estados reais**.
2. **Qual o grau de determinismo alcançável?** Testar duas execuções idênticas, save/load e variação do FPS, com scheduler e RNG completos. Se divergir, encontrar a primeira causa em vez de exigir uma arquitetura de replay complexa às cegas.
3. **Quais invariantes têm melhor custo-benefício?** Começar por moeda, estoque e identidade. Algumas verificações globais pesadas podem rodar só no teste/headless ou sob demanda.
4. **Qual primeira ferramenta de debug realmente falta?** CLI e arquivos podem ser suficientes; adicionar menu visual de desenvolvimento apenas se houver necessidade recorrente.
5. **Como lidar com paralelismo e cache?** Antes de ativar otimização, comparar execução normal/otimizada; validar ordem de commits, invalidação e equivalência de consequências.
6. **Quais testes realmente seguram integração?** À medida que a cidade integrada crescer, montar um corpus pequeno de saves/seeds reais que cubra fronteiras vulneráveis, sem impor um plano de POCs por sistema.

## 10. Proposta final para revisão do responsável

**Priorizar quatro capacidades:** (1) regras críticas verificadas por invariantes; (2) execução headless reproduzível; (3) checkpoint + comandos ordenados para reproduzir falhas; (4) causas e logs estruturados com investigação por ID/tick. **Não construir tudo antecipadamente**: cada capacidade nasce quando houver estado e fluxo reais que a justifiquem. Separar testes rápidos, cenários longos e performance.

Se essa direção for confirmada, promover **somente as decisões estruturais ainda ausentes** à ARCHITECTURE e, se surgir uma nova regra obrigatória de trabalho, ao AGENTS. A SPEC não precisa receber implementação de log, framework de teste, painel técnico ou arquitetura de replay como mecânicas de jogo. Nenhuma proposta deste documento está aprovada apenas por estar escrita aqui.

*Pesquisa externa consultada em 2026-10-09. Links de fontes primárias incorporados às seções acima. Exemplos numéricos, frequências, esquemas e nomes de ferramentas são candidatos técnicos, não compromissos nem benchmarks medidos do IndexCities.*

## 11. Ampliação interdisciplinar: o que realmente muda na estratégia

### 11.1. Um laboratório de cidades adversariais — não outra engine

**Ideia vinda de FoundationDB e TigerBeetle/VOPR.** Sistemas de banco de dados complexos testam a **implementação real** sob um relógio simulado, entradas controladas, variações de ordem e falhas deliberadas. Ao encontrar violação, salvam seed e contexto para reproduzir. No IndexCities, o mesmo Core headless pode receber: estado inicial ou checkpoint, RNG explícita, agenda controlada e ações externas ordenadas. O teste cria condições difíceis **permitidas pelas regras do jogo**: dois compradores para um mesmo imóvel; obra sem material; energia insuficiente durante produção; caminhão bloqueado; aluguel com prazo no limite; herança enquanto há dívida e contrato. Isso **não** autoriza inventar crises aleatórias na gameplay.

O ganho potencial é enorme porque a cidade é um sistema de agentes dependentes. Um erro raro no começo (ex.: vender estoque reservado) pode contaminar empregos, salários e receita muitos dias depois. O objetivo do laboratório é **achar a primeira transição ilegal**, não simplesmente sobreviver 100 dias sem travar. Não copiar integralmente os laboratórios de bancos distribuídos: não há servidor nem consenso distribuído no escopo atual. Toda hipótese de custo/ganho precisa ser medida quando existir Core.

### 11.2. Fuzzing inteligente: gerar ações válidas, não apenas números aleatórios

Gerar milhares de valores sem sentido tende a provocar exceções triviais. Melhor gerar **sequências de situações legalmente alcançáveis**: criar empresa, construir instalação, competir por trabalhadores, comprar matéria-prima, iniciar viagem, entrar em escassez, pagar salário, salvar, carregar e retomar. Em cada ação, o gerador conhece **pré-condições estruturais**; o jogo continua responsável pelas decisões autônomas reais.

**Três níveis possíveis:** testes por propriedade de funções isoladas (baratos); sequências curtas de transações cruzando domínios (fortes); pequenos mundos sintéticos mas **executados no Core verdadeiro** para expor interações (mais caros). Ferramentas FsCheck em .NET e Hypothesis stateful (referência conceitual de outra linguagem) ilustram como gerar, repetir e reduzir casos. Para parser de save ou comandos inválidos, usar fuzzing de bytes em alvo delimitado pode ser útil; para economia/IA, ações válidas geradas entregam melhor sinal.

**Não fixar percentual de cobertura.** Cobrir uma operação de compra não significa cobrir dois compradores que chegam ao mesmo imóvel no mesmo instante.

### 11.3. Reduzir um bug de uma cidade enorme para cinco ações

O problema do save gigante é que reproduzir é só metade do diagnóstico. Técnicas de **delta debugging** e **shrinking** tentam eliminar entradas mantendo a *mesma* falha. No IndexCities, o objeto reduzido pode ser uma sequência de comandos, uma janela temporal ou um cenário gerado — sempre respeitando precondições.

Exemplo hipotético: depois de três semanas, propriedade duplicada. Em vez de inspecionar 15.000 operações, o redutor descarta ações não essenciais até restarem duas aquisições concorrentes e uma falha de invalidação de reserva.

**Restrições:** (1) remover ação pode tornar ações seguintes impossíveis, exigindo recriar um cenário válido; (2) uma exceção diferente não é a mesma falha — preservar o identificador da invariante; (3) sem replay confiável, o redutor produz falsos resultados; (4) editar diretamente um save para apagar SIMs pode criar referências quebradas. Por isso, **redutor de comandos primeiro**, não um editor de mundo arbitrário. Construir apenas depois de ocorrer problema que justifique seu custo.

### 11.4. Guardrails de segurança **e** progresso

Além de "nunca pode existir dinheiro duplicado" (*safety*), deve haver testes para "tarefas viáveis não devem ficar eternamente abandonadas por bug da agenda" (*liveness*). Isso vale para fila de entregas, eventos programados e tarefas de decisão.

Atenção à nuance: falta de vagas, sem combustível, pobreza e engarrafamento permanente **podem ser estados legítimos**. Um teste não pode impor "todo SIM consegue emprego" nem "toda entrega termina". Uma propriedade de progresso precisa declarar suas condições: "se há rota disponível, trabalho ativo, insumo reservado e nenhuma interrupção relevante ao longo da janela, o evento agendado não pode desaparecer da fila". Critérios concretos sempre derivados da SPEC. A literatura de sistemas críticos diferencia explicitamente segurança de progresso; adaptar sem transferir lógica bancária ao jogo.

### 11.5. Primeiro ponto de divergência, não apenas hash final

OpenTTD e Factorio mostram como determinismo, **ordem de comandos**, caches e **estado esquecido no save** podem produzir divergências difíceis de observar. Um plano de diagnóstico possível:
1. assinatura canônica **somente do estado autoritativo** por domínio/intervalo, além de contagens e totais;
2. duas execuções idênticas ou normal vs otimizada;
3. detectar a primeira janela em que seus estados diferem;
4. estreitar a janela **reexecutando a partir de checkpoints reproduzíveis**;
5. criar **diff semântico**: ID de cidadão/empresa/bem, campo e primeira causa.

Hash global diferente não é diagnóstico. A mesma quantidade total de dinheiro pode mascarar uma transferência para destinatário errado. Comparar efeitos, proprietários, filas e causalidade. Checksums e reconstrução de caches devem ser ferramentas de **teste ou debug configurado**, não trabalho obrigatório em cada tick da cidade do jogador.

### 11.6. O save não é só arquivo: ele contém o futuro da cidade

Uma cidade salva pode **parecer igual visualmente** e ainda estar quebrada, se esquecer: RNG, ordem de eventos, vencimentos, destinatários de entregas, fila de pathfinding, reservas de material, estado de transição, tempo real vs tempo simulado. Fazer teste comparativo:

**A:** continuar da mesma cidade por um tempo lógico. **B:** salvar/carregar naquele ponto e continuar com os **mesmos inputs no mesmo instante lógico**. Estados autoritativos e consequências devem seguir equivalentes sob ambiente controlado. Em teste pesado e cidade pequena, salvar/carregar a cada tick ou limite de transição, como o modo experimental do Factorio. Não é recomendado para cada commit ou para o save do jogador.

Além disso, testar **corrompimento de arquivo, schema, interrupção de escrita e falha de leitura**, garantindo que o sistema reporte erro adequadamente e não sobrescreva silenciosamente um save bom. Estratégia de escrita atômica/versionamento só deve ser concretizada quando o formato de persistência existir, dentro da ARCHITECTURE.

### 11.7. Testes metamórficos de aceleração — ponto mais singular do IndexCities

Não sabemos prever onde todos os SIMs estarão no dia 37. Mas podemos exigir relações entre execuções:
- **Velocidades 1 e 3:** no mesmo instante de tempo simulado e com os mesmos comandos nos mesmos momentos, a aceleração não deve criar/pular trajetos, salários, produção ou outras operações. O FPS muda; o mundo autoritativo não.
- **Câmera longe, objetos fora da tela:** mesmos eventos físicos/econômicos autoritativos.
- **Cache ligado/desligado:** mesma operação causal, com invalidação correta de rotas, elegibilidade, estoque e índices, quando o cache promete equivalência exata.
- **Save interposto vs execução contínua:** mesma evolução em cenário controlado.
- **Múltiplas threads vs scheduler sequencial de referência:** preservar exclusividade de posse e reserva, sem corridas ou ordem de decisões contrária às regras aprovadas.

**Armadilha:** comparar só agregados (saldo total, população) pode deixar decisões erradas invisíveis. Comparar também histórico de eventos **relevantes**, IDs e transições. Se refatoração mudou legítima regra de desempate permitida pela SPEC, a verificação precisa distinguir o que é contrato do que é acaso; não congelar todo detalhe de implementação por snapshot.

### 11.8. A aleatoriedade tem mais de uma origem

A seed inicial ajuda, mas não resolve tudo. Causas de não reprodutibilidade incluem horário real do computador, ordem de enumeração, hash com seed variável, tarefas paralelas, ordem da fila, callbacks do Godot, float, mudanças de build ou de parâmetros.

Estratégias em ordem de custo: RNG explícita e persistível; clock injetável e tick lógico; comandos ordenados; desempates estáveis nos pontos em que regras exigem prioridade; eventos pendentes persistidos; isolamento da câmera; *somente depois*, controlar scheduler de threads se a concorrência trouxer bugs reais. Dividir RNG por domínio ou por entidade pode reduzir efeito cascata, mas **não é determinismo gratuito** e cria estado a manter. Meta inicial: reproduzir no mesmo ambiente/build; não exigir igualdade bit a bit global multiplataforma.

### 11.9. Logs que contam causas — “recibo causal”

O log comum registra "fábrica sem produzir". Precisamos de uma resposta rastreável: a fábrica tentou lote X; não tinha energia efetivamente atendida; por quê? Produção municipal abaixo da demanda; qual infraestrutura afetada? Os valores que originaram a decisão devem vir **da própria avaliação do Core**, não de uma explicação paralela.

Proposta de três intensidades:
- **Barato e contínuo:** código de evento/erro, domínio, tick, IDs, métricas agregadas de filas, duração e falhas.
- **Direcionado:** buffer circular de transições relevantes por ID/região/período, com condicionais/precondições e IDs da causa; exporta contexto *antes* da falha.
- **Investigação profunda em teste:** trace de transições, hash de estado mais frequente, assertions globais, auditoria de cache.

Não colocar ID de cada SIM como etiqueta de métricas globais: cardinalidade explosiva. Para logs C#, considerar ILogger com estrutura e generator de alto desempenho; para profiler usar contadores .NET e profiler Godot; **não adotar telemetry stack distribuída**.

### 11.10. Checagem de consistência explícita e teste do teste

Muitos defeitos são detectáveis por um verificador de estado profundo: IDs referenciados existem, imóvel não tem dois donos exclusivos, salários/locações referenciam credores corretos, soma monetária fecha, estoque reservado não excede o que está disponível, heap de eventos não contém entradas ilegais etc. O Factorio tem referência de consistency check.

O verificador profundo **pode custar caro** e deve rodar em cenários/headless/depuração, não continuamente por força de hábito. Algumas asserções pontuais próximas às transições valem mais: verificar **imediatamente após comprar** reduz a distância temporal até a causa.

**Mutation testing** verifica qualidade dos próprios testes: injetar propositalmente *somente em cópia para testes* bug de moeda, duplicação de proprietário, mensalidade cobrada duas vezes; as invariantes deveriam acusar. Stryker.NET é ferramenta candidata no ecossistema C#. Não perseguir 100% mutation score nem modificar regras de jogo para ajudar suíte.

### 11.11. Concorrência, memória e tempo: emprestar ferramentas sem trazer dívida técnica

Microsoft Coyote e predecessores como CHESS exploram **interleavings** de tarefas que expõem condições de corrida e permitem replay. Pode ser crucial se compras, pathfinding, scheduler e recursos forem paralelizados. **Não há motivo para incorporar agora**, pois o projeto ainda não tem Core concorrente implementado. Simutrans usa sanitizers para C++ e testa cenários integrados; isso é boa inspiração, mas sanitizers nativos **não são a stack de C#**.

Para C#, considerar no momento certo teste do scheduler, cancelamento, GC/alloc, benchmark de save/rotas, profiler e EventPipe. Benchmark deve medir a cidade real, em hardware/cenários controlados; tempo de execução de suíte funcional não é meta de performance de jogo.

### 11.12. Os testes podem viver no jogo — mas sem poluir a UI final

Simutrans executa testes como **cenários** em CI, usando o jogo. Factorio documentou um gerador a partir de cenas montadas visualmente. No IndexCities, pode ser útil usar um *save de cidade problemática como fixture*: reproduzir headless, inspecionar resultado; um modo de debug (apenas desenvolvimento) permitiria pausar um tick, ver um ID, ativar trace, gerar pacote de reprodução. Começar por CLI/arquivos; UI de debug é opcional e depende de dor real. Não aprova ferramenta de rewind temporal para o jogador nem replay de história jogável.

### 11.13. Casos extremos precisam ser *legítimos* no produto

Exemplos fortes: duas famílias concorrendo por única moradia; fila de matrícula lotada; apagão parcial afetando fábricas; herança de proprietário com locatário inadimplente; falta de combustível numa térmica; construção cancelada com material em trânsito; caminhão no meio do mapa quando ocorre save/load; hospital lotado e busca alternativa; escassez de água regional; empreendimento falindo durante entrega. Todos podem ocorrer seguindo comportamentos existentes.

O gerador não deve inventar status imunes a consequências, criar dinheiro ou colocar agente em estado impossível. Se falta regra na SPEC, marcar **lacuna para decisão humana** e evitar teste que imponha por conta própria uma saída.

### 11.14. Validar o jogo que existe, não apenas o que pode ser calculado

Oxygen Not Included usa branch de testes e pede saves, logs e informação contextual de bugs; Space Station 14 e RobustToolbox têm conjuntos de integração com mundo, engine e relógio; Cataclysm possui muitos testes pequenos por domínio; Simutrans executa o jogo como suíte; Mindustry roda testes em PR. **Nenhum padrão único vence.**

Adaptação: repetir cenários de economia/autonomia no Core e, quando o conjunto integrado estiver funcional, jogar de verdade para avaliar consequências visíveis, ritmo e microgerenciamento. Uma cidade “sem erro técnico” pode ter gameplay terrível; uma cidade com desemprego alto pode estar **correta**, dependendo do que o jogador fez. Não tornar "bom resultado econômico" uma asserção universal.

## 12. Cenário ilustrativo: bug raro de venda fantasma

**Sintoma:** depois de dez dias simulados, uma empresa comercializou um item já reservado para outra entrega.

1. Guardrail no Core acusa a primeira violação de estoque/reserva, antes de se espalhar para lucro/emprego.
2. O teste produz pacote com build, checkpoint próximo, seed, estado RNG+agenda, comandos externos com tick, IDs e transições relacionadas.
3. Reexecução headless chega ao mesmo instante; comparação de estados mostra a primeira divergência e o recibo causal revela uma reserva repetida.
4. Redução retira ações não causais mantendo o mesmo tipo/ID de violação.
5. Correção baseada **na SPEC existente**; teste pequeno, permanente, com dois pedidos que disputam a última unidade. Se ordem de prioridade não estiver definida, **decidir primeiro na SPEC**.
6. Mesmo cenário é executado com e sem cache/otimização, sem aceitar diferenças que alterem a causalidade aprovada.

O cenário demonstra a utilidade do sistema; **não é prova que já o implementamos**.

## 13. Plano incremental como *hipótese*, não processo obrigatório

| Primeiro fato observado no projeto | Menor capacidade com retorno | Adiar |
| --- | --- | --- |
| Primeira transação monetária, propriedade e estoque | Unitário + invariantes locais + IDs e RNG explícita | logs detalhados de tudo |
| Primeiras decisões automáticas/scheduler | Mesmo Core rodando headless; gerar cenários válidos curtos | engine de simulação paralela |
| Primeiro save | A/B salvar-carregar vs continuar; versão e estado pendente corretos | checkpoint toda frame no jogo |
| Primeira falha sistêmica difícil | Pacote com save próximo + comandos ordenados + trace por ID | ferramenta visual gigante |
| Primeiros caches/aceleração/concorrência | Testes diferenciais e agenda/RNG sob controle | determinismo universal bit a bit |
| Cidade integrada funcional | Cenários adversariais e longos + benchmarks independentes + playtesting | 100% cobertura ritual |

**Medir qualidade pelos problemas resolvidos:** bugs reproduzíveis, tempo até primeira causa, regressões que voltaram, overhead ligado/desligado, instabilidade/falso positivo de testes, custo por suíte e capacidade de recuperar cenários antigos. Não presumir que uma biblioteca ou workflow novo dará esse resultado por si.

## 14. Decisão técnica proposta, ainda não aprovada

**Direção preferida:** usar o **mesmo Core** como laboratório reproduzível; testes de integridade e comportamento em cenários reais; snapshot/checkpoint + estado completo da agenda e da aleatoriedade + comandos externos ordenados; comparações de execução e investigação causal por IDs; geração adversarial e redução quando houver ROI. Separar custo alto de validação da execução normal. Preservar a primeira entrega integrada e a validação global, **sem instituir POC por subsistema**, nem mover pesquisa para a SPEC ou ARCHITECTURE sem discussão explícita.

**Por que não só seed?** Depois de dezenas de milhares de decisões e mudanças de cidade, mesma seed de mapa não restaura o mesmo estado. **Por que não event sourcing global?** Gravar cada decisão e cada transição consome CPU/armazenamento, pode duplicar fonte de verdade e complica playback após mudanças de schema. **Por que não só snapshot?** Reproduz estado inicial, mas não explica qual comando/ordem produz o defeito. **Por que não apenas logs?** A informação necessária pode não ter sido registrada e o bug pode ser não determinístico. A combinação *focada* resolve limitações de cada peça.


## 15. Catálogo ampliado de fontes e pesquisas (por função)

**116 URLs distintas**, além das explicações e exemplos que as transformam em conclusões nas seções 11–14. Fontes oficiais e repositórios citados servem para **verificação e aprofundamento**; nem toda referência é prova do mesmo grau, benchmark aplicável ao IndexCities ou dependência recomendada. Alguns links apontam a projetos com técnicas **não C#**, incluídos para extrair princípios e limitações. Parte foi inspecionada diretamente no GitHub, parte via documentação oficial/páginas públicas e parte é literatura de apoio — **não houve benchmark executado**.

### Jogos, engines e práticas reais (37)

1. [OpenTTD — documentação de desync](https://github.com/OpenTTD/OpenTTD/blob/master/docs/desync.md)
2. [OpenTTD — como depurar desync](https://github.com/OpenTTD/OpenTTD/blob/master/docs/debugging_desyncs.md)
3. [OpenTTD — logging de desenvolvimento](https://wiki.openttd.org/en/Development/Debugging)
4. [Factorio — CRC por tick, FFF 47](https://www.factorio.com/blog/post/fff-47)
5. [Factorio — testes com cenários, FFF 78](https://www.factorio.com/blog/post/fff-78)
6. [Factorio — cache de rotas, FFF 121](https://www.factorio.com/blog/post/fff-121)
7. [Factorio — gerar testes a partir de cenário, FFF 154](https://www.factorio.com/blog/post/fff-154)
8. [Factorio — investigar desync, FFF 188](https://www.factorio.com/blog/post/fff-188)
9. [Factorio — consistency check, FFF 242](https://www.factorio.com/blog/post/fff-242)
10. [Factorio — save/load determinístico, FFF 270](https://www.factorio.com/blog/post/fff-270)
11. [Factorio — benchmark de saves, FFF 281](https://factorio.com/blog/post/fff-281)
12. [Factorio — teste save/load por tick, FFF 315](https://factorio.com/blog/post/fff-315)
13. [Factorio — busca hierárquica, FFF 317](https://www.factorio.com/blog/post/fff-317)
14. [Factorio — multithread determinístico, FFF 415](https://www.factorio.com/blog/post/fff-415)
15. [Factorio — casos de desempenho, FFF 421](https://factorio.com/blog/post/fff-421)
16. [Factorio — branch de versões experimentais, FFF 444](https://factorio.com/blog/post/fff-444)
17. [Factorio — replay e lockstep, FFF 37](https://factorio.com/blog/post/fff-37)
18. [Factorio — sincronização inicial, FFF 55](https://www.factorio.com/blog/post/fff-55)
19. [Factorio — custo de save, FFF 201](https://updater.factorio.com/blog/post/fff-201)
20. [Cataclysm DDA — contribuição/testes](https://github.com/CleverRaven/Cataclysm-DDA/blob/master/CONTRIBUTING.md)
21. [Cataclysm DDA — catálogo de testes](https://github.com/CleverRaven/Cataclysm-DDA/tree/master/tests)
22. [Cataclysm DDA — testes de calendário](https://github.com/CleverRaven/Cataclysm-DDA/blob/master/tests/calendar_test.cpp)
23. [Simutrans — cenários automatizados com sanitizers](https://github.com/simutrans/simutrans/blob/master/.github/workflows/run-tests.yml)
24. [Simutrans — executador de testes integrado](https://github.com/simutrans/simutrans/blob/master/tools/run-automated-tests.sh)
25. [Mindustry — testes de PR](https://github.com/Anuken/Mindustry/blob/master/.github/workflows/pr.yml)
26. [Space Station 14 — testes de integração](https://github.com/space-wizards/space-station-14/tree/master/Content.IntegrationTests)
27. [RobustToolbox — testes de relógio/timing](https://github.com/space-wizards/RobustToolbox/tree/master/Robust.Shared.IntegrationTests/Timing)
28. [RobustToolbox — conjunto de integrações](https://github.com/space-wizards/RobustToolbox/tree/master/Robust.Shared.IntegrationTests)
29. [Freeciv — testes de regras](https://github.com/Freeciv/freeciv/tree/master/tests)
30. [Transport Fever 2 — ferramentas de diagnóstico no jogo](https://www.transportfever2.com/wiki/doku.php?id=modding%3Aingametools)
31. [Transport Fever 2 — model editor](https://www.transportfever2.com/wiki/doku.php?id=modding%3Amodeleditor)
32. [Transport Fever 2 — troubleshooting de mods](https://www.transportfever2.com/wiki/doku.php?id=gamemanual%3Amodtroubleshooting)
33. [Oxygen Not Included — branch pública de testes](https://support.klei.com/hc/en-us/articles/4404626280852-Oxygen-Not-Included-How-to-access-the-Public-Testing-Branch)
34. [Oxygen Not Included — captura de logs e saves](https://support.klei.com/hc/en-us/articles/360052993772-Oxygen-Not-Included-DLCs-Logs-and-Useful-Information-for-Bug-Reports)
35. [Luanti — debug e profiler](https://docs.luanti.org/for-creators/debug/)
36. [Gaffer on Games — deterministic lockstep](https://gafferongames.com/post/deterministic_lockstep/)
37. [Gaffer on Games — snapshots de estado vs simulação](https://gafferongames.com/post/snapshot_interpolation/)

### Sistemas críticos, simulação de falhas e concorrência (21)

38. [FoundationDB — simulação determinística do próprio código](https://apple.github.io/foundationdb/testing.html)
39. [FoundationDB — testing client, seed e workloads](https://github.com/apple/foundationdb/blob/main/documentation/sphinx/source/client-testing.rst)
40. [TigerBeetle — VOPR](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/internals/vopr.md)
41. [TigerBeetle — arquitetura](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/ARCHITECTURE.md)
42. [TigerBeetle — protocol-aware DST (2026)](https://tigerbeetle.com/blog/2026-08-20-protocol-aware-dst/)
43. [Antithesis — determinismo de simulação](https://antithesis.com/docs/resources/deterministic_simulation_testing/)
44. [Antithesis — multiverso/testes](https://antithesis.com/docs/introduction/how_antithesis_works/)
45. [Antithesis — depuração e reprodução](https://antithesis.com/docs/product/debugging/)
46. [Antithesis — relatos de custos de testes](https://antithesis.com/blog/2025/testing_pangolin/)
47. [Antithesis — mutation testing (2026)](https://antithesis.com/blog/2026/mutation-testing/)
48. [Microsoft Coyote — agenda de tarefas reproduzível](https://microsoft.github.io/coyote/get-started/using-coyote/)
49. [Microsoft Coyote — teste de concorrência C#](https://github.com/microsoft/coyote)
50. [Microsoft — artigo CHESS sobre heisenbugs](https://www.microsoft.com/en-us/research/publication/finding-and-reproducing-heisenbugs-in-concurrent-programs/)
51. [SQLite — como testa o banco de dados](https://www.sqlite.org/testing.html)
52. [SQLite — TH3 e cobertura de caminhos](https://sqlite.org/th3.html)
53. [rr — gravar/reproduzir execução Linux](https://rr-project.org/)
54. [Jepsen Knossos — checagem de operações concorrentes](https://github.com/jepsen-io/knossos)
55. [Microsoft Coyote — repositório e exemplos](https://github.com/microsoft/coyote/tree/main/Samples)
56. [TigerBeetle — repositório/casos de teste](https://github.com/tigerbeetle/tigerbeetle)
57. [FoundationDB — repositório/casos de teste](https://github.com/apple/foundationdb)
58. [Antithesis — guia de invariantes/asserts](https://antithesis.com/docs/using_antithesis/assertions/)

### Testes generativos, modelos e pesquisa metodológica (29)

59. [FsCheck — quickstart e geradores](https://fscheck.github.io/FsCheck/QuickStart.html)
60. [FsCheck — geração de dados e shrinking](https://fscheck.github.io/FsCheck/TestData.html)
61. [FsCheck — propriedades](https://fscheck.github.io/FsCheck/Properties.html)
62. [FsCheck — repositório C#](https://github.com/fscheck/FsCheck)
63. [Hypothesis — testes stateful](https://hypothesis.readthedocs.io/en/latest/stateful.html)
64. [Hypothesis — introdução](https://hypothesis.readthedocs.io/en/latest/)
65. [Delta debugging — artigo Zeller](https://www.cs.purdue.edu/homes/xyzhang/fall07/Papers/delta-debugging.pdf)
66. [Debugging Book — DeltaDebugger](https://www.debuggingbook.org/html/DeltaDebugger.html)
67. [Delta debugging — retrospectiva científica 2025](https://publications.cispa.de/articles/journal_contribution/Simplifying_and_Isolating_Failure-Inducing_Input_A_Retrospective_on_Delta_Debugging/28714898)
68. [NIST — metamorphic testing em simulação](https://tsapps.nist.gov/publication/get_pdf.cfm?pub_id=931851)
69. [NIST — metamorphic testing em simulação híbrida](https://tsapps.nist.gov/publication/get_pdf.cfm?pub_id=932547)
70. [Metamorphic testing de software científico](https://pmc.ncbi.nlm.nih.gov/articles/PMC7252536/)
71. [Metamorphic testing em modelos oceânicos](https://arxiv.org/abs/2206.05457)
72. [Metamorphic testing combinado com propriedades](https://arxiv.org/abs/2211.12003)
73. [Artigo — estudos de teste de software científico](https://romisatriawahono.net/lecture/rm/survey/software%20engineering/Software%20Testing/Kanewala%20-%20Testing%20Scientific%20Software%20-%202014.pdf)
74. [LLVM libFuzzer — orientação guiada por cobertura](https://llvm.org/docs/LibFuzzer.html)
75. [AFL++ — estratégia de fuzzing](https://github.com/AFLplusplus/AFLplusplus/blob/stable/docs/fuzzing_in_depth.md)
76. [AFL++ — limites do modo persistente](https://github.com/AFLplusplus/AFLplusplus/blob/stable/instrumentation/README.persistent_mode.md)
77. [Stryker.NET — mutation testing](https://github.com/stryker-mutator/stryker-net)
78. [NASA — manual de modelagem e simulação](https://standards.nasa.gov/standard/NASA/NASA-HDBK-7009)
79. [NASA — padrão de simulação V&V](https://standards.nasa.gov/standard/NASA/NASA-STD-7009)
80. [NASA — perguntas sobre V&V](https://public.ksc.nasa.gov/mns/faq/)
81. [NASA — verification assessment](https://www.grc.nasa.gov/WWW/wind/valid/tutorial/verassess.html)
82. [Estudo — frameworks de testes de modelos de agentes](https://www.tandfonline.com/doi/full/10.1057/jos.2012.26)
83. [Estudo — validação de modelos baseados em agentes](https://www.sciencedirect.com/science/article/pii/S1364815222002596)
84. [NIST — overview de resultados/metamorphic](https://csrc.nist.gov/pubs/sp/800/160/v1/upd2/final)
85. [Model checking — Alloy Analyzer](https://alloytools.org/)
86. [Model checking — TLA+](https://lamport.azurewebsites.net/tla/tla.html)
87. [QuickCheck — artigo seminal](https://www.cse.chalmers.se/~rjmh/QuickCheck/manual.html)

### Godot, .NET, logs e execução prática (29)

88. [Godot — modo headless/CLI](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)
89. [Godot — painel de debugger/profiler](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/debugger_panel.html)
90. [Godot — performance monitors customizados](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/custom_performance_monitors.html)
91. [Godot — catálogo de debug](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/index.html)
92. [Godot — testes nativos de engine, não gameplay](https://docs.godotengine.org/en/stable/engine_details/architecture/unit_testing.html)
93. [gdUnit4 — suíte de testes de Godot](https://github.com/godot-gdunit-labs/gdUnit4)
94. [gdUnit4 — scene runner](https://godot-gdunit-labs.github.io/gdUnit4/latest/advanced_testing/sceneRunner/)
95. [gdUnit4Net — execução C#](https://github.com/godot-gdunit-labs/gdUnit4Net)
96. [gdUnit4 — setup de C#](https://godot-gdunit-labs.github.io/gdUnit4/latest/csharp_project_setup/csharp-setup/)
97. [GUT — testes GDScript](https://github.com/bitwes/Gut)
98. [Godot Asset Library — gdUnit4](https://godotengine.org/asset-library/asset/4390)
99. [.NET — estratégias de testes](https://learn.microsoft.com/en-US/dotnet/core/testing/)
100. [.NET — tutorial testar biblioteca](https://learn.microsoft.com/en-us/dotnet/core/tutorials/test-class-library)
101. [.NET — xUnit usando CLI](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit)
102. [.NET — MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)
103. [.NET — testes MSTest detalhados](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests)
104. [.NET — EventPipe](https://learn.microsoft.com/dotnet/core/diagnostics/eventpipe)
105. [.NET — logging estruturado eficiente](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
106. [.NET — LoggerMessage source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
107. [.NET — System.Diagnostics.Metrics](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics)
108. [.NET — instrumentação de métricas](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)
109. [.NET — counters de runtime](https://learn.microsoft.com/dotnet/core/diagnostics/event-counters)
110. [.NET — counters disponíveis](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters)
111. [.NET — dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
112. [.NET — dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
113. [BenchmarkDotNet — documentação](https://benchmarkdotnet.org/)
114. [Godot — linha de comando teste integração em ambiente CI](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html#running-a-script)
115. [Microsoft — source generation e instrumentação](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors)
116. [Go — FlightRecorder (referência conceitual, não biblioteca C#)](https://pkg.go.dev/runtime/trace#FlightRecorder)

---

**Estado final desta ampliação:** revisão humana **PENDENTE**. Nenhuma especificação de comportamento, regra estrutural ou código foi alterado; permanecem a entrega integrada e a validação global aprovadas. O ganho desta investigação é orientar ferramentas e experimentos *quando a implementação existir*, evitando engenharia prematura.
