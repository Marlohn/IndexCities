# IndexCities — Pesquisa de testes, diagnóstico e reprodução de bugs

> **Revisão humana: PENDENTE.** Pesquisa e recomendação técnica produzidas por IA em 2026-10-09. **Não é fonte de verdade nem decisão de arquitetura/produto**; propostas dependem de revisão. O comportamento aprovado está na [SPEC](../SPEC.md), as fronteiras técnicas vigentes na [ARCHITECTURE](../ARCHITECTURE.md), e as regras de trabalho no [AGENTS](../../AGENTS.md).
>
> **Pergunta:** como descobrir, reproduzir e evitar bugs num jogo em que milhares de SIMs, empresas e veículos agem autonomamente, inclusive com tempo acelerado, sem sobrecarregar a simulação ou criar um processo burocrático?

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
