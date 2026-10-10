# IndexCities — Pesquisa de testes, diagnóstico e reprodução de bugs

> **Revisão humana: PENDENTE.** Pesquisa e recomendação técnica produzidas por IA em 2026-10-09. **Não é fonte de verdade nem decisão de arquitetura/produto**; propostas dependem de revisão. O comportamento aprovado está na [SPEC](../SPEC.md), as fronteiras técnicas vigentes na [ARCHITECTURE](../ARCHITECTURE.md), e as regras de trabalho no [AGENTS](../../AGENTS.md).
>
> **Pergunta:** como descobrir, reproduzir e evitar bugs num jogo em que milhares de SIMs, empresas e veículos agem autonomamente, inclusive com tempo acelerado, sem sobrecarregar a simulação ou criar um processo burocrático?

## Quinta rodada — síntese executiva final (2026-10-10)

> **PENDENTE de revisão humana.** Pesquisa concluída **como exploração de alternativas**, não solução já validada. Os novos mecanismos e sua avaliação crítica estão nas [seções 22–23](#22-quinta-rodada-final--ideias-realmente-novas-vindas-de-outras-áreas-2026-10-10).

**Diferença desta rodada:** além de procurar bugs que aconteceram, investigar **por que uma decisão/evento esperado não aconteceu**; medir se os testes realmente exploraram **estados do jogo**, e não somente linhas C#; verificar **fluxo local de dinheiro, propriedade e materiais**; investigar cenários raros, simetrias de grafos e performance adversarial sem transformar tudo em arquitetura obrigatória.

**Ordem sugerida para análise humana:** (1) integridade por transação; (2) diagnóstico focal de **avaliado vs não avaliado**; (3) exploração de estados semânticos; (4) replay/corpus reproduzíveis; (5) buscas sofisticadas só quando casos reais as justificarem. Bots RL, IA como oráculo da SPEC, segunda engine, event sourcing integral e algoritmos evolucionários **não são recomendados para começar**.

**Limite honesto:** a investigação encontrou novas opções sustentadas por pesquisa externa, mas **não mediu ganho, overhead nem funcionamento em IndexCities**. A próxima fonte de confiança deverá vir de código e execuções reais quando existirem, mantendo a implementação fiel à SPEC.

---

## Quarta rodada — avaliação crítica e novas hipóteses (2026-10-09)

> **Revisão humana: PENDENTE.** A terceira rodada não encerrava a investigação: reunia técnicas, mas ainda não demonstrava confiabilidade do replay, custo de CPU/memória ou capacidade de explicar falhas reais do IndexCities. A quarta rodada confronta propostas com falhas observadas e critérios de refutação. **Não altera SPEC, ARCHITECTURE, AGENTS ou código.**

**Veredito da pesquisa nesta rodada:** adotar gradualmente uma escada de diagnóstico — **detectar a primeira transição incorreta → salvar estado/causa → reproduzir a partir de checkpoint → repetir com instrumentação profunda → reduzir a sequência → incorporar teste de regressão**. Além disso, testar separadamente comportamento emergente e desempenho. A vantagem dessa direção está em evitar logs de cada SIM e não construir uma segunda engine.

**Novidades mais relevantes:** (1) invariância da divisão do tempo em intervalos; (2) aleatoriedade vinculada a eventos/IDs em vez de uma sequência global — hipótese, não escolha; (3) descobrir o ponto em que o bug se torna inevitável por bifurcações controladas; (4) logs retroativos extraídos de replay; (5) erros de referências a entidades mortas/IDs reutilizados; (6) reconciliação de transações locais, além da soma global; (7) corrupção/interrupção do save; (8) testes de concorrência e modelos formais apenas para casos realmente difíceis. Detalhes e evidências na [seção 19](#19-quarta-rodada-profunda-avaliação-das-lacunas).

---

## Terceira rodada — novas evidências e mudança de prioridades (2026-10-09)

> **Revisão humana desta rodada: PENDENTE.** Pesquisa complementar de simuladores de agentes/trânsito, agendamento de eventos, experimentação estatística, testes combinatórios e engenharia de regressão. [Evidências e fontes da rodada](#18-evidências-primárias-da-terceira-rodada). É **exploração**, não aprovação técnica nem alteração na SPEC/ARCHITECTURE.

**Síntese:** chegamos a uma distinção que faltava: (a) **reproduzir exatamente uma falha**, com o mesmo código/ambiente e estado suficiente; (b) **validar o comportamento emergente** de muitas cidades, que pode variar legitimamente; e (c) **testar a execução e o desempenho do tempo acelerado**. São objetivos relacionados, mas não devem ter um oráculo único.

| Descoberta nova desta rodada | Consequência para o IndexCities |
| --- | --- |
| **Save de jogador ≠ checkpoint forense** | Um save válido pode não preservar todos os estados necessários para replay idêntico. A opção de registro diagnóstico deve ser avaliada sem ampliar o save comum por ritual. |
| **Logs/monitores podem mudar a simulação** | Instrumentação não pode consumir a mesma aleatoriedade nem modificar a ordem dos eventos; testar *debug ligado vs desligado*. |
| **Tempo simultâneo precisa de desempate reproduzível** | No mesmo tick, dois compradores ou dois veículos disputando um recurso expõem defeitos de ordem e de prioridade. Não inventar a prioridade de gameplay. |
| **Testes combinatórios reduzem explosão de casos** | Combinar limites reais (cheio/vazio, com/sem energia, saldo suficiente/insuficiente, deslocamento concluído/pendente) de modo intencional, sem fazer produto cartesiano gigante. |
| **Múltiplas sementes servem para validar dinâmica, não só achar crash** | Estatísticas, tendências e casos extremos podem apontar comportamento suspeito, **sem aprovar metas numéricas econômicas arbitrárias**. |
| **Snapshots “verdes” não provam correção** | Gravar a saída como padrão-ouro só verifica que nada mudou; o padrão pode estar errado. Exigir invariantes e expectativas aprovadas para dar significado à comparação. |
| **Tempo acelerado exige duas avaliações** | Correção temporal (nenhum efeito pulado) **e** capacidade real de acompanhar o tempo simulado (filas, throughput, atraso, memória). |

**Conclusão recomendada, diferente da segunda rodada:** em vez de começar imaginando um único sistema de replay completo, conceber **uma escada de diagnóstico**: invariantes locais + seed/RNG/agenda controladas → saves reexecutáveis → pacote forense de bug quando necessário → comparações multi-seed e cenário adversarial → análise profunda/visual apenas nos erros difíceis. Não presumir instrumentação universal nem arquivos gigantes.

---

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

## 16. Terceira rodada: testes de agentes, trânsito, agenda e observadores

### 16.1. Dois conceitos de reprodutibilidade que não devem ser misturados

**Reprodução forense de um bug:** mesma build, mesma configuração, mesmo checkpoint completo (estado primário, relógio, scheduler, RNG, decisões externas em ordem e versões dos dados). Queremos observar o mesmo erro **no mesmo evento/tick**, sujeito às limitações reais de plataforma. Uma mudança legítima de código pode impedir que a sequência antiga seja exatamente igual; o arquivo de reprodução deve reter identificação da versão e, se necessário, ser convertido em teste semântico de regressão.

**Validação de uma cidade emergente:** executar a cidade com **várias sementes e parâmetros** para observar distribuição de desemprego, congestionamento, utilização de água/energia, renda/estoque, falências etc. Não exigir que toda execução e toda atualização do jogo tenham números finais idênticos. **A aleatoriedade legítima não é bug**; valores implausíveis, regressões generalizadas ou efeitos sem causa merecem investigação, não tolerância arbitrária para quebrar a SPEC.

**Exemplo simples:** se a mesma simulação controlada paga aluguel duas vezes no mesmo mês, isso é erro factual; se famílias diferentes procuram estabelecimentos diferentes em seeds distintas, isso pode ser resultado legítimo; se trocar a velocidade de apresentação altera quem recebeu uma casa sem mudança causal permitida, pode ser erro de scheduler.

Fontes: [SUMO — Randomness](https://sumo.dlr.de/docs/Simulation/Randomness.html), [NetLogo — BehaviorSpace](https://docs.netlogo.org/behaviorspace), [Mesa — batch runner](https://mesa.readthedocs.io/stable/apis/batchrunner.html).

### 16.2. A lição surpreendente do SUMO: um save pode estar correto para continuar, mas insuficiente para repetir

O [SUMO — SaveAndLoad](https://sumo.dlr.de/docs/Simulation/SaveAndLoad.html) documenta que o estado das fontes aleatórias **não é salvo por padrão**; uma opção separada inclui esse estado. Também documenta modelos internos que não são integralmente persistidos, dependência de certos arquivos de entrada e limitações entre plataformas. O [tutorial de 2026](https://eclipse.dev/sumo/docs/Tutorials/2026.html) reforça que continuar após carregar com nova aleatoriedade e repetir com RNG restaurada são **duas operações úteis, com objetivos diferentes**.

**Lição para IndexCities:** diferenciar explicitamente, durante o desenvolvimento, **qual contrato é exigido do save normal** e **que informação adicional um pacote de reprodução precisa**. O save normal já precisa preservar todas as consequências persistentes e não perder eventos reais segundo a SPEC; não pode ser “incompleto” a ponto de perder dívidas, empregos, viagens ou estoques. O diagnóstico pode necessitar ainda de elementos como **estado exato da RNG, ordem da agenda, versão executável, comandos subsequentes e assinaturas de comparação**, sem impor ao jogador retenção de log contínuo ou histórico de cada decisão.

**Não antecipar dois formatos de arquivo complexos.** Um manifesto que acompanha o save ou metadados adicionais em modo debug podem bastar. Medir custo de serializar RNG+fila e a possibilidade real de reconstruir índices. **Reprodução exata não é garantida por apenas carregar um save aparentemente íntegro.**

### 16.3. O observador não pode mudar a cidade (*heisenbug de telemetria*)

O [guia de programação do NetLogo](https://docs.netlogo.org/programming) explica que monitores usam gerador aleatório auxiliar para não interferir na RNG principal, e oferece mecanismo de aleatoriedade local. O [SUMO — Randomness](https://sumo.dlr.de/docs/Simulation/Randomness.html) usa fontes aleatórias separadas por aspecto (carregamento, fluxos, direção, dispositivos) para evitar que uma operação não relacionada altere outra sequência.

**Novo teste metamórfico:** mesma build/seed/inputs, com logging/tracing/overlays ativados e desativados → **mesma sequência de transições autoritativas**, salvo overhead de CPU (comparado no mesmo horizonte de **tempo simulado**, não pelo tempo real). Abrir um painel de diagnóstico, pedir informação de uma fábrica ou capturar um contador não deveria consumir a RNG usada nas decisões econômicas.

Riscos: instrumentar com callbacks que alteram filas, enumerar coleções e inadvertidamente mudar a ordem, RNG de UI compartilhada com engine, hashes instáveis e logs que bloqueiam threads. Conclusão: **inspecionar deve ser uma operação somente de leitura**, e a auditoria de causalidade deve capturar variáveis já avaliadas pela regra, nunca recalcular decisões com outra fonte aleatória.

Esta proposta **não impõe** RNG por SIM; um conjunto pequeno de fluxos explícitos já pode ser suficiente. Importante também testar que o **modo profundo** de diagnóstico não esconde um bug por alterar sua temporização.

### 16.4. SimPy mostra que “mesmo tick” não significa “mesmo efeito”

O [SimPy — Time and Scheduling](https://simpy.readthedocs.io/en/latest/topical_guides/time_and_scheduling.html) destaca que resolução de tempo pode fundir eventos distintos no mesmo instante e demonstra uma fila por tempo e identificador de sequência para desempate reproduzível. O [guia de monitoramento](https://simpy.readthedocs.io/en/stable/topical_guides/monitoring.html) mostra instrumentar **agendamento e execução** separadamente — uma pista importante para encontrar eventos perdidos.

**Roteiro técnico para testar a agenda**, sem decidir regras de produto:
- Mesmo tempo lógico, **dois comandos concorrentes para um recurso exclusivo**: uma compra não pode produzir dois proprietários; o vencedor respeita a prioridade **quando houver regra aprovada**, não uma prioridade inventada por teste.
- Dois eventos agendados com o mesmo timestamp precisam ser executados numa **ordem suficientemente definida para reprodução**, ao menos no cenário controlado; ordem técnica não equivale a prioridade social do produto.
- O mesmo evento não pode ser **executado duas vezes** após salvar, carregar, pausar ou acelerar.
- Cancelar/reagendar deve deixar filas e referências coerentes, sem executar evento fantasma.
- Um evento válido e já vencido não deve desaparecer se a simulação continua avançando.
- O trace deve distinguir **agendado → cancelado/reagendado → executado**, não simplesmente “evento criado”.

Testes explícitos de **véspera de vencimento, virada de mês, fim de prazo de três meses, reentrada de entidades e ordem de faturamento** podem encontrar bugs que milhares de seeds comuns não atingem. Não confundir data civil, tempo simulado e tick físico.

### 16.5. Matriz de testes combinatórios *pairwise* para fronteiras econômicas

[Microsoft PICT](https://github.com/microsoft/pict) gera combinações representativas dos valores de parâmetros e pode cobrir pares ou trios sem enumerar todo o produto cartesiano. Aplicação como **método de criação de cenários**, não dependência obrigatória:

| Dimensão representativa | Valores de teste, dentro de estados legais da simulação |
| --- | --- |
| Saldo do responsável | insuficiente / exatamente suficiente / acima |
| Infraestrutura | capacidade folgada / no limite / deficitária |
| Estoque da empresa | vazio / totalmente reservado / disponível |
| Situação de imóvel | sem dono / ocupação vigente / oferta elegível |
| Agenda | antes do vencimento / no vencimento / imediatamente depois |
| Transporte | rota livre / rota congestionada / via indisponível |
| Execução | sequência normal / checkpoint+reload / velocidade acelerada |

Combinar pares/trios **prioritariamente quando houver dependência causal**; alguns valores são incompatíveis entre si e o gerador deve rejeitar estados impossíveis. Pairwise não encontra todos os bugs de sequência temporal nem interação de quatro fatores. **Combinatórios complementam** casos explícitos de risco (herança + aluguel em atraso + imóvel ocupado + migração, por exemplo) e testes generativos longos.

### 16.6. Um laboratório de experimentos, não só um laboratório de exceções

[NetLogo BehaviorSpace](https://docs.netlogo.org/behaviorspace) varia sistematicamente parâmetros, repete cenários e coleta resultados; [Mesa batch runner](https://mesa.readthedocs.io/stable/tutorials/11_batch_run.html) executa lotes por combinações e sementes. [SUMO — FAQ](https://sumo.dlr.de/docs/FAQ.html) recomenda múltiplas sementes porque uma execução única pode ser enviesada pela aleatoriedade específica; [SUMO — testes](https://sumo.dlr.de/docs/Developer/Tests.html) também explica os limites dos resultados congelados como oráculo.

**Adaptação potencial:** para calibrações **delegadas pela SPEC**, construir um pequeno conjunto de cidades e medir métricas **que já existam no Core**, como empregos reais, tempos de deslocamento, compras, saldo e capacidade atendida, distribuição de causas, fila de eventos. O pesquisador/jogador avalia se há equilíbrio/experiência convincente; nenhum valor estatístico sozinho autoriza alterar gameplay.

**Distinção crítica:** usar mesma seed numa implementação comparada não garante as mesmas decisões se uma mudança alterar a quantidade/ordem de chamadas aleatórias; comparar **propriedades e tendências** sem falsificar igualdade. Técnicas de comparação por **common random numbers** podem reduzir variância em condições compatíveis, mas sincronização das fontes aleatórias teria de ser demonstrada, não presumida ([Winter Simulation Conference, 1990](https://ieeexplore.ieee.org/document/129543/)).

### 16.7. O problema do “teste passou porque estava errado desde o início”

[SUMO — Developer Tests](https://sumo.dlr.de/docs/Developer/Tests.html) usa arquivos de saída aprovados como referência, mas reconhece expressamente que esses resultados também podem estar errados. A literatura de [validação de agent-based models](https://www.jasss.org/27/1/11.html) distingue comparação com modelos de referência, validação empírica, amostragem e análise causal.

**Oráculos em ordem de robustez para nosso caso:**
1. **Contrato explícito** (SPEC) + invariantes de saldo/capacidade/posse: a regra é verificável.
2. **Relação metamórfica**: mudar só FPS, pausar ou interpor save não deve criar efeito econômico.
3. **Casos manuais revisados**: poucos cenários com resultado esperado compreensível, não gigantescos snapshots.
4. **Baseline estatístico multi-seed**: detectar mudanças de tendência e pedir investigação humana, **não afirmar automaticamente “jogo quebrado”**.
5. **Saída inteira congelada**: evidência de regressão potencial, mas pode congelar bugs. Exigir revisão em mudanças legítimas.

Um modelo de referência independente *pequeno* pode servir para conferir cálculo de dinheiro/contabilidade, lote ou desempate já especificado. **Não manter segunda engine completa de cidade**: custo, divergências, falsa confiança e duplicação da SPEC.

### 16.8. Testar a aceleração em duas dimensões que parecem iguais, mas não são

**A — Correção causal:** para chegar ao mesmo tempo simulado, a mesma sequência de comandos e mesmas pré-condições reais precisa produzir transições equivalentes; não pode concluir obra/viagem/pagamento pelo simples avanço do calendário. Testes de velocidade 1 vs 3, render ligado/desligado, cache on/off e pause-save-load.

**B — Capacidade operacional:** o jogo precisa conseguir acompanhar o relógio sob a carga alvo, sem filas atrasadas se acumularem indefinidamente, UI congelar, GC explodir ou o scheduler deslocar eventos para compensar artificialmente a lentidão. Medir **tempo por tick e percentis**, backlog e **idade** das filas, eventos processados por segundo real, ocupação de CPU/memória, custo de rota, custo da telemetria e o **tempo simulado efetivamente processado por segundo real**. Não inventar tolerâncias antes de um baseline representativo.

A [documentação do Godot sobre jitter e stutter](https://docs.godotengine.org/en/stable/tutorials/rendering/jitter_stutter.html) diferencia problemas de apresentação e física; [interpolação física](https://docs.godotengine.org/en/stable/tutorials/physics/interpolation/physics_interpolation_introduction.html) esclarece por que FPS e ticks podem divergir. **Não copiar sem crítica a integração de física Godot**: pelo ARCHITECTURE, o Core C# é autoridade; Godot é host/apresentação.

**Teste adicional proposto:** medir debug leve vs profundo, porque um sistema de diagnóstico que inviabiliza a aceleração 3 **não pode ficar profundo sempre ligado**. Benchmarks headless avaliam Core; testes visuais verificam se o que aparece na tela não contradiz o estado da cidade.

### 16.9. Observabilidade não é só log: monitorar cadeia causal sem invadir as decisões

O [SimPy — Monitoring](https://simpy.readthedocs.io/en/stable/topical_guides/monitoring.html) distingue inspeção **por tempo**, **por mudança de estado** e **por evento**. Isso inspira um desenho barato:

- **Contador/medida** global por domínio para detectar tendência (ex.: eventos atrasados, materiais reservados).
- **Transição local** com campos explicáveis e código de causa sempre que uma decisão *relevante* falhar (fábrica parada por falta de energia).
- **Trace com alvo** quando a investigação exigir seguir uma cadeia específica de efeitos, com janela curta e buffer limitado.
- **Exportar somente artefato suficiente** e evitar copiar o estado inteiro de cada entidade por tick.

Um sistema de logs pode revelar *quando* e *quem*, mas a pergunta *por que* exige capturar **entradas/precondições reais da função decisória**, e não gerar explicação posterior pela UI. Mais verbosidade não necessariamente traz mais diagnóstico.

### 16.10. “Um teste detectou alguma diferença”: triagem em três possíveis categorias

Ao comparar builds, seeds ou opções, um resultado diferente pode ser:

1. **Bug factual / quebra da SPEC** (dinheiro criado, ação duplicada, tempo saltado, decisão contrária à prioridade aprovada) → falha de teste.
2. **Mudança legítima de algoritmo/calibração dentro da SPEC** (escolha de destino mudou, sem quebrar causalidade) → registrar evidências, atualizar baseline se fizer sentido.
3. **Ausência de comportamento decidido na SPEC** (duas ações simultâneas sem regra de prioridade material) → **não impor regra por teste**. Sinalizar lacuna para decisão humana **se ela alterar gameplay**; detalhe técnico neutro/reversível continua com autonomia do implementador.

Essa triagem protege o processo SDD de virar burocracia ou de criar requisitos acidentais por causa da suíte.

## 17. Comparação de estratégias e proposta revisada

| Estratégia | O que ganha | Custo/armadilha | Recomendação da pesquisa |
| --- | --- | --- | --- |
| **Seed + testes de Core** | cenário rápido e repetível no início | não captura história da cidade | primeira camada |
| **Save comum + comandos** | reexecução prática perto da falha | pode faltar estado RNG/agenda/metadados | validar suficiência, não presumir |
| **Checkpoint de diagnóstico seletivo** | contexto forense sem gravar toda vida do SIM | serialização e versões custam memória/I/O | avaliar quando aparecer bug difícil |
| **Registro de cada decisão de todos SIMs** | forte auditabilidade em teoria | CPU, memória, arquivo e segunda fonte de verdade | **não** por padrão |
| **Multi-seed e cenários combinatórios** | descobre estados que ninguém antecipou | comparação estatística não prova gameplay correta | alto potencial, separar de testes de contrato |
| **Duas engines completas para comparar** | oráculo diferencial independente | manter duas implementações é caríssimo | **não** |
| **Referência pequena de regra monetária/estoque** | facilita descobrir bug em cálculo crítico | pode duplicar lógica se mal desenhada | apenas para regra específica valiosa |
| **Validação de toda cidade a cada alteração** | cobre integração ampla | atrasa desenvolvimento, resultados podem ser ambíguos | marcos e execuções programadas conforme custo |
| **Testes por mudanças afetadas + smoke global barato** | feedback rápido com proteção transversal | seleção errada omite impacto indireto | avaliar quando CI crescer; scheduler, dinheiro, estado compartilhado exigem alcance amplo |

**Recomendação em linguagem simples:** testar primeiro as regras que **nunca** podem ser violadas. Garantir que a engine possa executar e repetir cenários **sem abrir o Godot**. Criar casos combinatórios de alto risco. Quando surgir um erro obscuro, exportar um save + manifesto com todos os elementos necessários para reconstruí-lo. Quando o jogo integrado estiver funcionando, comparar famílias de cidades e observar gameplay real. Separar verificação de causalidade e benchmark da aceleração.

**O mais importante que ainda não sabemos:** o custo real de salvar toda informação forense, quanto debug perturba o desempenho, quão reproduzível é o scheduler/RNG e quais combinações de sistemas mais quebram primeiro. Esses pontos pedem medição com código **quando existir** — não uma arquitetura elaborada no escuro.

## 18. Evidências primárias da terceira rodada

Fontes consultadas especificamente para acrescentar técnicas **ainda pouco cobertas nas rodadas anteriores**; não repetir o catálogo extenso da seção 15. Aqui há **fontes oficiais, código de referência e estudos metodológicos**; nem todas são experiências controladas equivalentes ao IndexCities. Evitar atribuir métricas de outros simuladores ao nosso jogo.

**Simulação de trânsito e seus limites de reprodução:**
- [SUMO — Randomness: fontes RNG separadas, seed e reprodutibilidade](https://sumo.dlr.de/docs/Simulation/Randomness.html)
- [SUMO — SaveAndLoad: RNG opcional e limites conhecidos de estado salvo](https://sumo.dlr.de/docs/Simulation/SaveAndLoad.html)
- [SUMO — FAQ: replicações multi-seed e resultados comparativos](https://sumo.dlr.de/docs/FAQ.html)
- [SUMO — Developer Tests: comparação com saídas de referência e limitações](https://sumo.dlr.de/docs/Developer/Tests.html)
- [SUMO — Tutorial 2026: escolher replay da RNG ou nova aleatoriedade](https://eclipse.dev/sumo/docs/Tutorials/2026.html)
- [SUMO — opções RNG para simulação multithread](https://sumo.dlr.de/userdoc/sumo.html)

**Modelos de agentes e desenho de experimentos:**
- [NetLogo — BehaviorSpace: explorar parâmetros e replicar simulações](https://docs.netlogo.org/behaviorspace)
- [NetLogo — Programming Guide: seed, RNG local e monitor que não muda o Core](https://docs.netlogo.org/programming)
- [Mesa — Batch Runner: combinações de cenários com RNG explícita](https://mesa.readthedocs.io/stable/apis/batchrunner.html)
- [Mesa — Tutorial de experimentos batch](https://mesa.readthedocs.io/stable/tutorials/11_batch_run.html)
- [Mesa — boas práticas de RNG](https://mesa.readthedocs.io/stable/best-practices.html)
- [JASSS (2024) — métodos de validação de modelos baseados em agentes](https://www.jasss.org/27/1/11.html)
- [Pesquisa sobre comparação de modelos independentes (*docking*)](https://pmc.ncbi.nlm.nih.gov/articles/PMC9731510/)

**Eventos, testes combinatórios e regressão:**
- [SimPy — Time and Scheduling: ordem estável no mesmo timestamp](https://simpy.readthedocs.io/en/latest/topical_guides/time_and_scheduling.html)
- [SimPy — Monitoring: rastrear eventos criados, agendados e executados](https://simpy.readthedocs.io/en/stable/topical_guides/monitoring.html)
- [Microsoft PICT — geração pairwise de combinações](https://github.com/microsoft/pict)
- [PICT — formatos, constraints e cobertura combinatória](https://github.com/microsoft/pict/blob/main/doc/pict.md)
- [OSS-Fuzz — corpus mínimo, casos de regressão e cobertura](https://google.github.io/oss-fuzz/advanced-topics/ideal-integration/)
- [OSS-Fuzz — arquivo reprodutor de falha](https://google.github.io/oss-fuzz/advanced-topics/reproducing/)
- [Winter Simulation Conference — *common random numbers* para comparação](https://ieeexplore.ieee.org/document/129543/)

**Godot e separação entre visualização e simulação:**
- [Godot — explicação de física vs rendering ticks](https://docs.godotengine.org/en/stable/tutorials/physics/interpolation/physics_interpolation_introduction.html)
- [Godot — jitter, stutter e input lag](https://docs.godotengine.org/en/stable/tutorials/rendering/jitter_stutter.html)

---

**Status: pesquisa PENDENTE de revisão humana.** Esta rodada não introduziu código, bibliotecas, CI, menu visual nem mudanças canônicas. A pesquisa propõe capacidades, **não** aprova detalhes de implementação, metas de desempenho ou novos comportamentos do jogador.

## 19. Quarta rodada profunda: avaliação das lacunas

> **Revisão humana desta seção: PENDENTE.** A literatura pesquisada inclui análises formais, engenharia de jogos, bancos de dados, modelos baseados em agentes e bugs reais. A validade de uma ideia no projeto de origem **não mede seu ganho no IndexCities**.

### 19.1. Antes de tudo: o que a pesquisa anterior ainda não provava

**Ponto fraco 1: replay.** Seed + save + comandos é uma boa receita, mas só produz repetição fiel se todas as entradas relevantes forem controladas: relógio, RNG, ordem de eventos, trabalho assíncrono, estados não salvos, conteúdo e versão. **Não chamar isso de garantia até repetir dois runs reais e comparar.** Evidência: [Factorio — save/load determinístico](https://www.factorio.com/blog/post/fff-270), [SUMO — estado da RNG opcional e limites de save](https://sumo.dlr.de/docs/Simulation/SaveAndLoad.html).

**Ponto fraco 2: oráculo.** Um hash indica que duas cidades estão diferentes, mas não diz qual é correta. Um teste de soma monetária detecta moeda criada, mas não detecta R$ 100 enviados para a pessoa errada. Precisamos de **contratos locais** derivados da SPEC e de comparação das transições que geraram cada efeito; não apenas snapshots ou contadores finais.

**Ponto fraco 3: custo.** Verificações globais, milhões de eventos gravados, checkpoints frequentes e múltiplas seeds podem consumir mais recursos que a simulação. Não temos baseline do jogo. Todas as propostas de frequência, tamanho e ferramenta ficam **a medir**, não aprovadas.

**Ponto fraco 4: correção de jogo versus validação científica.** Estudos de modelos de agentes procuram validar semelhança com fenômenos reais; o IndexCities deve preservar as regras aprovadas e produzir gameplay interessante. Estatísticas econômicas de cidades reais são referências de plausibilidade, **não testes automáticos de lucro, desemprego ou migração sem decisão de produto**. [JASSS — revisão de nove técnicas](https://www.jasss.org/27/1/11.html).

### 19.2. Descoberta importante para aceleração: invariância de particionamento temporal

Comparar velocidade 1 contra 3 ajuda, mas **pode perder um bug que ocorre quando o Core agrupa ou divide trabalho por intervalos**. Novo teste metamórfico proposto: partir do mesmo checkpoint; processar uma janela T inteira pela estratégia de avanço permitida versus duas ou mais janelas que somam T; aplicar comandos externos **nos mesmos instantes simulados**; comparar eventos de negócio e estado autoritativo relevante.

Exemplos: uma cobrança exatamente no limite do mês; dois caminhões competindo por estoque; fábrica operando até faltar energia durante um intervalo; compra e morte de proprietário em eventos contíguos. Se o sistema otimizado disser que ambas as execuções são equivalentes, **não pode pular salários, viagens, limites físicos, reservas ou eventos vencidos**.

**Ressalva:** não exigir igualdade exata de posições em integrações físicas numéricas de passos distintos sem demonstrar essa propriedade. É válido exigir igualdade de eventos discretos e propriedades causais sob regras aprovadas; tolerância física exata requer validação própria. Inspiração: [NIST — metamorphic testing em simulação](https://www.nist.gov/publications/metamorphic-testing-continuum-verification-and-validation-simulation-models), [MathWorks — eventos pontuais e passo temporal](https://www.mathworks.com/help/simevents/ug/discrete-event-chart-precise-timing.html). **Relevância muito alta para a SPEC de aceleração real.**

### 19.3. Ideia ousada: sorteio baseado em evento, não na posição da chamada global

A pesquisa [Random123, dos autores](https://random123.com/) e sua [descrição técnica](https://random123.com/releases/latest/docs/CBRNG.html) demonstram RNGs por chave + contador (Philox/Threefry). Uma mesma combinação produz o mesmo valor, sem avançar um único gerador global. [NumPy Philox](https://numpy.org/doc/stable/reference/random/bit_generators/philox.html) documenta usos com fluxos paralelos.

**Hipótese:** derivar sorteios de elementos estáveis, como seed da cidade, domínio, ID do evento e contador da decisão. Inserir um sorteio irrelevante para outro agente deixaria de mudar automaticamente todos os sorteios posteriores. Isso pode favorecer reprodução, testes de concorrência e manutenção da ordem causal.

**Por que NÃO aprovar agora:** criar e persistir identidades de eventos, garantir contadores estáveis, não duplicar chaves, versionar o algoritmo e não mascarar disputas materiais entre agentes traz custo real. A RNG nunca isola uma escolha de suas **dependências econômicas**; se duas empresas disputam o último lote, a segunda decisão depende legitimamente da primeira. Comparar RNG por domínio versus counter-based no Core real antes de escolher.

### 19.4. Retroceder no diagnóstico sem criar rewind jogável

O [Antithesis — When did the bug start? (2026)](https://antithesis.com/blog/2026/causality_analysis/) demonstra uma investigação curiosa: de uma falha reprodutível, voltar a checkpoints, criar múltiplas continuações controladas e observar a **probabilidade do erro** após cada ponto. Isso ajuda a descobrir onde a falha se tornou quase inevitável — nem sempre quando ocorreu o crash.

O [Antithesis — Retroactive Logging (2026)](https://antithesis.com/blog/2026/retrologging/) descreve reconstruir logs de um trecho **por replay**, em vez de armazenar logs detalhados o tempo inteiro. **Adaptação mínima ao IndexCities:** quando o replay estiver comprovadamente fiel, reexecutar uma janela curta com trace profundo por entidade/tick e procurar primeiro estado divergente. Só investigar ramificações contrafactuais se um bug muito raro justificar CPU e instrumentação extra.

**Limite:** bifurcar não prova automaticamente causa — pode mudar entradas indiretas e resultados aleatórios. E nada disso funciona sem replay robusto. **Não é recomendação de construir hipervisor, histórico completo ou máquina do tempo do jogador.**

### 19.5. Bugs de identidade: referências antigas podem atingir cidadãos novos

Há um [bug concreto de remoção de agentes durante atualização no Mesa](https://github.com/mesa/mesa/issues/302): modificar a coleção em iteração pode impedir outras atualizações. Em [issue do Bevy de 2026](https://github.com/bevyengine/bevy/issues/25416), um índice reutilizado e uma referência obsoleta prejudicaram o processamento de outra entidade. O [debate de serialização no Bevy](https://github.com/bevyengine/bevy/discussions/7235) demonstra o risco de confundir ID, slot e geração.

**Teste que merece destaque:** criar um evento pendente de SIM/empresa; essa entidade sai, morre, perde imóvel ou fecha; o scheduler processa a fila posteriormente. O evento **não** pode atingir outra entidade que reutilizou um slot, transferir pagamento para antigo proprietário, manter vínculo que terminou ou processar o mesmo evento duas vezes. Também conferir ordem de remoção/iteradores.

Possíveis alvos: herança durante aluguel; empresa falida com caminhão em viagem; cidadão falecido com salário agendado; imóvel com novo dono; linha de ônibus alterada com ônibus em circulação. O tratamento concreto da operação deve respeitar SPEC, sem inventar regra geral de cancelamento. **IDs persistentes nunca reutilizados podem ser mais simples que referências geracionais** em alguns domínios; testar antes de adicionar uma camada de handles.

### 19.6. Dois níveis de checagem: operação local e reconciliação global

Na economia do IndexCities, a oferta monetária global é fixa. Mas uma transferência para o destinatário errado **pode preservar o total**. Um bom teste monetário precisa verificar origem, destino, montante, motivo e vínculo real **na própria operação**. O segundo nível recompõe periodicamente saldos do Core e confere Reserva Global + Caixa + SIMs + empresas, sem confiar no índice derivado suspeito.

No estoque físico, **a quantidade global não é constante**: importação, produção, transformação, consumo, perda e descarte mudam as quantidades legitimamente. A auditoria tem de verificar fluxos e origens permitidos, não impor conservação falsa. O mesmo vale para passageiros, vagas e imóveis: verificar ocupação/posse por ID e precondições concretas.

[TigerBeetle — VOPR](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/internals/vopr.md) destaca verificadores além de asserções; exemplos de [bugs gerados por seed/commit](https://github.com/tigerbeetle/tigerbeetle/issues/1020) mostram como um pacote pode ser reproduzido. **Não transportar sem crítica a política do banco de encerrar o processo em toda inconsistência**: na versão do jogador, preservar dados/diagnóstico e evitar reparos mágicos que gerem dinheiro ou recursos. Checagem agressiva em testes/headless é diferente do tratamento de erro em produção.

### 19.7. Falha de gravação é um problema distinto de salvar/carregar

Até aqui a estratégia testava a continuidade de um save completo. **Faltava testar corrupção/truncamento, schema antigo e interrupção da gravação**. Proposta de cenário de teste: iniciar gravação, injetar falha em pontos controlados, reler último save válido e verificar integridade ou mensagem de erro explícita. Também testar upgrade de schema real quando a primeira mudança existir.

O [.NET File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace) oferece troca de arquivo e backup com restrições; **não presumir garantia absoluta de durabilidade** entre plataformas, discos, volumes ou desligamento. Técnica concreta só precisa ser escolhida com o formato de persistência real. Não construir um sistema de migração universal antes de existir schema real.

### 19.8. Model checking de pequenos mecanismos é pesquisa; do mundo inteiro, não

[Java PathFinder](https://github.com/javapathfinder/jpf-core/wiki/What-is-JPF) explora ordenações possíveis e gera um caminho para o defeito, mas é Java; a [própria documentação](https://github.com/javapathfinder/jpf-core/wiki/Testing-vs.-Model-Checking) reconhece a **explosão do espaço de estados**. Não há justificativa de adotá-lo no Core C#.

Se um mecanismo pequeno continuar problemático (duas aquisições disputando um bem, cancelamento/reagendamento de uma entrega), pode valer modelar **apenas 2–3 agentes e uma transação** para examinar ordens de execução. Mas modelo correto não garante que a implementação obedece ao modelo. Antes disso, testes com estados pequenos e sequência gerada no próprio Core são mais produtivos.

### 19.9. O custo do diagnóstico também faz parte da correção

A execução normal não deve pagar o custo de armazenar cada decisão. Proposta de **níveis sem obrigação de existir desde o início**: (a) invariantes locais/baratas e métricas agregadas; (b) testes headless profundos com hash/diff; (c) falha rara reproduzida com trace completo em janela estreita; (d) análise contrafactual sob demanda. Medir log OFF, leve e profundo, inclusive sobre FPS, ticks de Core, GC, atraso de scheduler e geração de arquivo.

O [Space Station 14 — iniciativa de testes de integração de 2026](https://github.com/space-wizards/space-station-14/issues/44384) aposta em **simulações curtas** de comportamento real. Seus [relatórios de falha automatizados](https://github.com/space-wizards/space-station-14/issues/44550) ilustram benefício e também custo de investigar falha de regra versus infraestrutura de teste. Não automatizar abertura de issue por cada seed fracassada antes de existir processo que agregue sinal.

### 19.10. Matriz de risco por domínio e o primeiro teste que realmente ajuda

| Domínio | Erro sistêmico | Teste que primeiro merece investimento |
| --- | --- | --- |
| Dinheiro/Reserva Global | criação monetária ou pagamento a destinatário errado | verificador por transação + soma independente |
| Estoque/logística | reserva dupla, entrega fantasma, perda indevida | disputa pelo último item + checkpoint em trânsito |
| Moradia/herança | dois donos, aluguel para ex-proprietário, ID obsoleto | sequências curtas e limites de calendário |
| Scheduler | evento vencido esquecido, duplicado ou fora de ordem | dois eventos no mesmo instante + save + execução particionada |
| SIMs e trânsito | atualização offscreen diferente, teletransporte | câmera/FPS diferente + eventos de entrada/chegada |
| Cache | resultado desatualizado muda escolha/serviço real | modo otimizado versus referência com entradas equivalentes |
| Persistência | continua diferente ou arquivo fica irrecuperável | continuidade A/B + injeção de falha de escrita |
| Escala | backlogs crescentes e velocidade 3 não sustentada | benchmarks separados, piores percentis e atraso de fila |

**A prioridade não é o número de testes; é o tamanho da consequência e a dificuldade de descobrir a causa depois.** Não tornar uma regra econômica em asserção antes de ela existir oficialmente na SPEC.

### 19.11. Critérios falsificáveis: como saber se uma proposta NÃO vale a pena

| Hipótese | Prova necessária quando houver implementação | Motivo para adiar/rejeitar |
| --- | --- | --- |
| Replay de checkpoint funciona | reproduções repetidas no mesmo build com evento e estado equivalentes | divergências não controláveis com custo proporcional |
| Replay retroativo substitui log profundo contínuo | mesmo erro reproduz e trace recupera causa anterior | falta de fidelidade impede reconstrução |
| RNG por evento melhora manutenção | teste de agente independente + benchmark CPU/memória | complexidade de IDs/streams supera benefício |
| Teste de divisão do tempo encontra regressão | cenários com fronteira, com/sem otimização | semântica não garante igualdade esperada |
| Verificador profundo dá sinal cedo | bugs deliberados em cópia de testes são detectados junto à operação | só detecta dias depois e pesa demais |
| Redução automática vale o custo | diminuir caso **real** sem mudar tipo de falha | sequência fica inválida ou custo é excessivo |
| Snapshot forense é viável | tamanho, serialização, reprodução e envio medidos | salva demais, atrasa o jogo ou não contém estado suficiente |
| Suíte estatística multi-seed ajuda gameplay | revela tendências que um humano confirma em cidades integradas | falsos alarmes ou metas que não constam na SPEC |

### 19.12. Conclusão revisada e limites da pesquisa

**Direção mais promissora:** capacidade de reproduzir, observar e auditar o Core real, com testes locais e sistêmicos, sem duas engines, sem event sourcing integral e sem instrumentação sempre profunda. O diferencial para IndexCities é testar o **encadeamento causal** entre sistemas: recursos, dinheiro, imóveis, SIMs, mobilidade e tempo. O melhor diagnóstico é saber **qual foi a primeira operação errada, quais eram suas precondições e quais entidades ela afetou**.

**Ainda não é possível concluir com rigor:** formato de arquivo, escolha de RNG, overhead de rastreamento, escala de simulação, política de checkpoints e metas de aceleração. Só um estado real do Core permitirá medir. A pesquisa anterior não autorizava afirmá-los como resolvidos; esta rodada tampouco.

**Explicitamente NÃO recomendar agora:** hipervisor/time-travel completo, log contínuo de cada SIM, replay jogável, prova formal de toda cidade, 100% de cobertura, novas ferramentas sem uso observado, ou pausas/etapas de POC de produto obrigatórias. A validação global da primeira implementação integrada continua indispensável, especialmente para gameplay e microgerenciamento.

## 20. Novas fontes verificadas e leitura crítica

Esta seleção contém **mecanismos novos e relatos reais** e não substitui o catálogo de 116 links da seção 15. Algumas fontes são documentação de produto ou issue e, portanto, exigem interpretação diferente de artigo com experimento controlado. Links relevantes:

- [NIST — simulações sem oráculo simples](https://www.nist.gov/publications/metamorphic-testing-continuum-verification-and-validation-simulation-models); [MathWorks — timing de eventos](https://www.mathworks.com/help/simevents/ug/discrete-event-chart-precise-timing.html).
- [Random123 — fonte dos autores](https://random123.com/); [documentação dos geradores counter-based](https://random123.com/releases/latest/docs/CBRNG.html); [NumPy Philox](https://numpy.org/doc/stable/reference/random/bit_generators/philox.html).
- [Antithesis — ponto de inevitabilidade do bug, 2026](https://antithesis.com/blog/2026/causality_analysis/); [logs retroativos, 2026](https://antithesis.com/blog/2026/retrologging/); [limites de construir hipervisor](https://antithesis.com/blog/deterministic_hypervisor/).
- [Mesa — bug real de remoção em scheduler](https://github.com/mesa/mesa/issues/302); [Bevy — bug de índice reutilizado de entidade](https://github.com/bevyengine/bevy/issues/25416); [Bevy — discussão de identidade e serialização](https://github.com/bevyengine/bevy/discussions/7235).
- [TigerBeetle — simulador do código real VOPR](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/internals/vopr.md); [issue de bug encontrada por seed](https://github.com/tigerbeetle/tigerbeetle/issues/1020).
- [Factorio — detalhes determinísticos de save/load](https://www.factorio.com/blog/post/fff-270); [testes pequenos e CRC](https://www.factorio.com/blog/post/fff-60).
- [Java PathFinder — capacidades](https://github.com/javapathfinder/jpf-core/wiki/What-is-JPF); [limites de exploração de estados](https://github.com/javapathfinder/jpf-core/wiki/Testing-vs.-Model-Checking).
- [Microsoft .NET — FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview); [File.Replace e backup](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace).
- [Space Station 14 — iniciativa de integração 2026](https://github.com/space-wizards/space-station-14/issues/44384); [falha de teste relatada automaticamente](https://github.com/space-wizards/space-station-14/issues/44550).
- [JASSS 2024 — nove abordagens de validação de agentes](https://www.jasss.org/27/1/11.html).

**Resultado: quarta rodada documentada, pesquisa PENDENTE de revisão humana.** Nenhum benchmark da engine foi executado; não há código do jogo no repositório. Não promover hipóteses desta seção para SPEC/ARCHITECTURE sem revisão e decisão explícitas.


## 22. Quinta rodada final — ideias realmente novas vindas de outras áreas (2026-10-10)

> **Revisão humana: PENDENTE.** Última rodada exploratória solicitada pelo responsável. Análise crítica baseada em pesquisas de segurança, bancos de dados, sistemas concorrentes, matemática industrial e testes de videogame. **NENHUMA destas técnicas foi medida no IndexCities**: o repositório consultado ainda não continha implementação da engine. As evidências provam que existem técnicas e resultados em seus contextos originais, **não** seu desempenho aqui. Nenhuma decisão é promovida a SPEC/ARCHITECTURE.

### 22.1. Maior descoberta: explicar o que **não aconteceu** — why-not provenance

Há uma tradição acadêmica de explicar não só **por que existe** um resultado, mas **por que falta** o resultado esperado. [Why and Where (Buneman, Khanna & Tan)](https://doi.org/10.1007/3-540-44503-X_20), [Efficiently Computing Provenance Graphs for Queries with Negation](https://arxiv.org/abs/1701.05699) e a [demonstração ICDE 2022 de proveniência SQL](https://db.cs.uni-tuebingen.de/publications/2022/how-where-and-why-data-provenance-improves-query-debugging--a-visual-demonstration-of-fine-grained-provenance-analysis-for-sql/) mostram o conceito em consultas, **não** em city builders.

**Nossa adaptação original:** muitos defeitos de SIMs e empresas são **não eventos**: a fábrica nunca procurou insumo, um cidadão elegível jamais considerou um emprego, uma entrega nunca foi despachada, a compra não foi sequer avaliada, o imóvel não voltou a ser ofertado após uma mudança. Um log de decisões realizadas não explica isso.

Em modo de investigação **por entidade e janela de ticks**, observar no Core: (1) momento em que a oportunidade deveria ser reconsiderada; (2) **avaliação aconteceu ou não?**; (3) quais condições foram consultadas de fato; (4) decisão tomada e motivo real; (5) houve agendamento de reavaliação ou o scheduler perdeu o gatilho. A resposta **“a decisão nem chegou a ser avaliada”** é diferente de **“o SIM decidiu não agir”**.

Exemplo: o SIM não foi trabalhar. Possibilidades legítimas incluem falta de emprego, turno não iniciado, rota impossível, ocupação em outra atividade. Uma quinta explicação é bug: **a avaliação do turno não foi agendada**. Não fabricar texto de causa na UI; o diagnóstico deve usar a verdade da engine. [Pesquisa de why-not com negação](https://arxiv.org/abs/1701.05699) também ensina a **recortar só subgrafos relevantes** em vez de calcular todas as explicações possíveis.

**Valor muito alto.** É talvez a melhor técnica nova, porque complementa diretamente causalidade e autonomia dos agentes. **Limite:** explicações completas de ausência em lógica recursiva podem ser caras ([estudo de complexidade](https://arxiv.org/abs/2303.12773)); começar com rastreamento seletivo de chamadas reais, não solver de decisões nem histórico integral.

### 22.2. Cobertura de estados da cidade, em vez de apenas cobertura de linhas C#

[StateFuzz — USENIX Security 2022](https://www.usenix.org/conference/usenixsecurity22/presentation/zhao-bodong) demonstrou que guiar fuzzing apenas por funções/linhas perde estados relevantes: duas execuções da mesma função podem atravessar **estados de negócio diferentes**. [Model-Guided Fuzzing — OOPSLA 2025](https://repository.tudelft.nl/record/uuid:66d18d3c-fead-4df0-8310-5df11370db13) explora cobertura de transições de um modelo abstrato; [StateAFL](https://github.com/stateafl/stateafl) usa feedback de estados de protocolo. **Seus benchmarks são de drivers/protocolos, não da economia de uma cidade.**

**Proposta para IndexCities:** guardar quais **transições semânticas** os testes realmente exercitaram, sem criar enums artificiais como regras do produto. Exemplos: empresa de lucrativa para insolvência com material pendente; imóvel ofertado para disputado e alugado; estoque disponível para reservado e depois entregue; trabalhador em deslocamento para emprego encerrado; entrega esperando rota e depois reagendada; locatário em atraso para três meses vencidos com mudança de proprietário.

O laboratório privilegia seeds/comandos que levam o **mesmo Core** a novas combinações, registra casos úteis como regressões, e deixa casos redundantes para testes mais baratos. Não precisa usar IA nem instalar fuzzer pesado: um conjunto explícito de categorias de **teste** extraídas do estado já implementado pode bastar.

**Oráculo correto:** alcançar transição nova não prova que ela está correta. Sempre cruzar com **invariantes e SPEC**; não tratar como obrigação de gameplay uma transição que apenas apareceu em execuções. Testar ganho contra geração aleatória com **mesmo orçamento**, medindo falhas novas confirmadas, não quantidade bruta de estados.

**Valor muito alto após existir fluxo entre sistemas.** Isso pode dar mais retorno que meta de cobertura percentual de linhas.

### 22.3. Clonar checkpoints promissores para encontrar eventos quase impossíveis

O método de [Adaptive Multilevel Splitting](https://pmc.ncbi.nlm.nih.gov/articles/PMC4440697/) explora eventos raros em simulações científicas fazendo várias continuações de trajetórias que chegaram perto da condição procurada. A [revisão matemática de AMS](https://doi.org/10.1063/1.5082247) discute variantes e limitações.

**Aplicação hipotética:** um teste busca defeito raríssimo causado por inadimplência, morte do proprietário e transferência de titularidade quase simultâneas. Em vez de gerar milhares de cidades desde zero, guardar checkpoint próximo ao evento, variar **somente RNG/comandos externos permitidos** e executar várias continuações. Preservar qualquer falha como pacote reproduzível. Também se aplica a falta de combustível em energia e carga, congestionamento e disputas de material durante obra.

**CRÍTICA ESSENCIAL:** selecionar preferencialmente caminhos raros **altera a distribuição de casos**. Um teste assim pode achar um bug, mas **NÃO** afirmar que ele ocorre com determinada frequência em partidas naturais. Estimativas probabilísticas exigiriam correções estatísticas explícitas. Além disso, toda ramificação deve ser estado **alcançável pelas regras reais**, não inventar SIM com dinheiro impossível ou empresa proprietária de bem inexistente.

**Valor potencial alto, custo alto**, dependente de checkpoint e critérios de proximidade; experimentar com poucas ramificações manuais antes de copiar algoritmos de AMS.

### 22.4. MAP-Elites: um zoológico de cidades extremas, não apenas a pior cidade

[Mouret e Clune — MAP-Elites](https://arxiv.org/abs/1504.04909) mostra busca que mantém resultados diversos em vez de só um ótimo. A ideia foi aplicada à [geração de testes de software em trabalho publicado em 2024](https://research.birmingham.ac.uk/en/publications/automated-test-suite-generation-for-software-product-lines-based-/), com [código de referência](https://github.com/gzhuxiangyi/SPLTestingMAP).

**Proposta:** manter um **corpus enxuto de cidades incomuns** e reprodutíveis em múltiplas dimensões: falta de energia vs. material; mercado residencial congestionado vs. livre; obras simultâneas vs. nenhuma; alta demanda de rota vs. baixa; alta rotatividade de empresas vs. estável. O gerador preferiria **novidade de comportamento e classes distintas de bug**, não “mais caos” ou cidades cada vez maiores.

Uma cidade pequena com poucas casas, um único fornecedor e reservas simultâneas pode ser mais útil para descobrir um bug do que uma cidade com 100 mil habitantes. **Pode começar como biblioteca manual de saves**, sem MAP-Elites instalado. A complexidade de escolher dimensões, medir qualidade e evitar categorias exponenciais torna a abordagem algorítmica prematura.

**Valor médio/alto para selecionar corpus**, baixo para iniciar agora um projeto de algoritmo evolutivo.

### 22.5. Descoberta automática de invariantes (Daikon): saber perguntar, sem criar regras

[Daikon](https://plse.cs.washington.edu/daikon/) observa execuções e infere **invariantes prováveis**, inclusive em [programas C#](https://plse.cs.washington.edu/daikon/download/doc/daikon/Example-usage.html). Não prova propriedades universais; encontra padrões **nas amostras observadas**.

É possível usar isso para descobrir hipóteses difíceis de notar: que determinado sistema nunca considera famílias sem carro; que um fluxo de compra jamais ocorre numa faixa de preço; que entregas sempre usam um mesmo caminho mesmo quando alternativa existe. **Não significa que tudo isso seja bug**.

**Maior risco:** institucionalizar uma falha existente. Se o Core atual tem um bug que impede certa compra, o minerador pode deduzir falsamente “essa compra nunca acontece” e gerar um teste que protege a limitação. Assim, qualquer regra inferida precisa passar por **SPEC, contraexemplos e revisão humana** antes de se tornar expectativa de teste. A mineração é uma **lupa para formular perguntas**, não autorização de comportamento.

**Valor médio, futuro; não prioridade de implantação.**

### 22.6. Rastreamento reverso de causas, inspirado em proveniência e program slicing

[Provenance in Databases: Why, How and Where](https://www.research.ed.ac.uk/en/publications/provenance-in-databases-why-how-and-where/), [Database Queries that Explain their Work](https://arxiv.org/abs/1408.1675) e [Dynamic Program Slicing (PLDI 1990)](https://doi.org/10.1145/93542.93576) estudam **recortar o conjunto de entradas e operações que contribuiu para um resultado**.

**Adaptação:** diante do estado “fábrica 15 sem insumo”, partir dessa entidade e navegar **para trás** só nos fatos relevantes: estoque vazio ← entrega atrasou ← veículo sem acesso ← via bloqueada ← obra iniciada pelo jogador. Isso é mais útil que 100 mil logs cronológicos sem filtro.

Para aproveitar sem event sourcing global, começar por **IDs de causa** em transições relevantes e trace profundo **durante replay**, com retenção limitada e foco por entidade/domínio. Expor uma **linha do tempo causal curta**, não grafo completo permanente de toda a cidade.

**Cuidado:** proveniência de dados e dependência de um cálculo **não provam causalidade contrafactual**; parte dos sistemas da cidade divide recursos comuns e possui múltiplas causas. Evitar alegar que uma única seta explica o comportamento econômico inteiro.

**Valor alto como aprimoramento do diagnóstico já pesquisado.**

### 22.7. Transformações por simetria: bugs escondidos em IDs e coordenadas

Metamorphic testing permite testar relações quando não conhecemos a saída exata ([padrões de relações metamórficas, 2025](https://doi.org/10.1002/stvr.70003)). Algoritmos de grafos reconhecem quando grafos têm a **mesma estrutura com IDs renomeados** ([NetworkX isomorphism](https://networkx.org/documentation/stable/reference/algorithms/isomorphism.html)).

**Teste local novo:** construir um grafo pequeno de ruas ou suprimentos, trocar todos os IDs de nós **preservando atributos, conexões, desempates e prioridades**, rodar o algoritmo e mapear resultado de volta. Se a operação deveria ser independente do nome dos nós e mudou, existe possível bug de ordem de coleção/hash. Outra variação é transladar um cenário geometricamente isolado em coordenadas sem mudar as relações relevantes.

**Não fazer isso para a cidade inteira por padrão:** posição, acesso exterior, topografia, recursos, preços e regras de prioridade podem depender legitimamente da localização e dos IDs. IDs podem fazer parte de um desempate técnico autorizado. **Usar só onde a equivalência realmente é garantida.**

**Valor médio/alto para pathfinding, índices, posição e algoritmos locais.**

### 22.8. Pensar dinheiro e recursos como equações de transição (redes de Petri)

Pesquisas de [PetriDotNet](https://doi.org/10.1016/j.scico.2017.09.003) e [manufatura/estoque em redes de Petri, estudo brasileiro](https://www.scielo.br/j/gp/a/4xzqzggzCy9ZyPzPHvjvczK/?lang=pt) usam transições e invariantes de fluxo para verificar sistemas com estados discretos.

**Abordagem concreta e barata:** para cada operação crítica, especificar **entradas, saídas e condições** como contrato de teste:
- transferir dinheiro: debitar pagador, creditar recebedor real, preservar oferta monetária total;
- reservar material: diminuir **disponível** e aumentar **reservado** sem duplicar quantidade física;
- transportar: estoque sai da origem e vira em trânsito, chega ao destino sem teleporte nem segunda cópia;
- produzir: transformar insumos em produtos e resíduos **permitidos pelas receitas da SPEC**; não exigir conservação ingênua do mesmo material;
- importar: material vem da origem externa autorizada e dinheiro segue o fluxo econômico aprovado.

**A vantagem inédita:** a soma global de dinheiro pode fechar mesmo com **recebedor incorreto**; o contrato local acusaria. O total de estoque pode parecer correto mesmo com **lote duplicado e outro perdido**; checagem por lote/transação acusaria.

**Não redesenhar a engine como Petri net completo.** Só aproveitar a ideia matemática para oráculos locais; grafos de estados da cidade inteira explodem.

**Valor alto, custo baixo/moderado: melhor nova recomendação prática.**


### 22.9. Procurar ciclos de espera e progresso impossível

Sistemas de concorrência e manufatura analisam grafos de espera e dependências entre recursos. Para o IndexCities, a adaptação **não é** instalar um detector de deadlock de sistema operacional, e sim analisar uma **cadeia de bloqueios de negócio** quando um cenário fica parado.

Exemplo: fábrica A aguarda material de B, B aguarda caminhão de C, e C aguarda combustível fornecido por A. Aparentemente, ninguém faz nada, embora todas as entidades tenham passado por decisões válidas. Uma execução longa sem crash jamais acusaria esse estado.

**Ferramenta opcional:** em investigação por entidade/área, montar um pequeno grafo com causa de pendência e recurso esperado. Procurar ciclos e identificar **condições de saída legitimamente existentes** (compra externa, novo fornecedor, mudança de rota, cancelamento aprovado, produção própria), além de gatilhos da agenda que esqueceram de reavaliar uma oportunidade.

**Advertência:** ciclos e escassez podem fazer parte da simulação correta. Não impor que toda economia progrida; o que deve ser testado é se o Core **respeitou condições e eventos de reavaliação** exigidos pela SPEC. A ausência de evento do item 22.1 é mais valiosa aqui que um alerta genérico de “cadeia travada”.

**Valor médio:** ferramenta de análise de raros bloqueios logísticos, não invariante sempre ativa.

### 22.10. Bots exploradores e inteligência artificial: ideia forte, momento errado

A [EA SEED — Automatic Gameplay Testing with Curiosity](https://www.ea.com/seed/news/gameplay-testing-curiosity) treinou agentes que exploram ambientes 3D para identificar problemas; [ASE 2022 — agent-based 3D game testing](https://doi.org/10.1145/3551349.3560507) investigou aprendizado por reforço para complementar testes com scripts. **Esses trabalhos não validam um agente capaz de jogar uma economia urbana integrada.**

**Aplicação em etapas, se necessária no futuro:**
1. Bot simples que emite **comandos de jogador já aprovados**, via mesma camada de comando do jogo: construir, ligar vias, salvar/carregar, alterar velocidade, pesquisar estado. Pode gerar sequências válidas orientadas por cobertura semântica da seção 22.2.
2. Seleção evolutiva por novidade, corpus diverso de cidades ou busca com restrições.
3. Só depois, caso bots simples falhem e houver evidência, avaliar agente RL de exploração ou IA que escolha comandos para provocar situações novas.

**Riscos sérios:** treinar pode custar mais que testar, recompensa mal definida leva a *reward hacking* (o bot aprende a bater num contador, não a encontrar bug), sequências bizarras podem alcançar somente estados impossíveis para jogador, e RL introduz mais aleatoriedade/artefatos para reproduzir. Além disso, bot tester **não deve criar nova decisão de produto**.

**Conclusão:** bots simples e buscas baseadas em estado têm potencial; **não recomendar rede neural ou LLM como núcleo de teste** agora.

### 22.11. Um adversário que tenta derrubar a performance, mantendo o jogo correto

Em vez de usar só uma cidade grande “normal” no benchmark, investigar as **piores cidades válidas**: anéis viários que forçam milhares de replanejamentos, caminhos que quase existem mas terminam bloqueados, trabalhadores e empresas fazendo reavaliações sincronizadas, rotas de carga com disputas de vagas, estoque insuficiente disparando novas tentativas no mesmo intervalo.

**Proposta:** um gerador altera topologias e comandos de maneira permitida, executa o **Core real** e procura aumentar métricas como duração de ticks (p95/p99), backlog de eventos, idade de filas, chamadas de pathfinding, alocações, invalidadores de cache e tempo simulado efetivamente processado por segundo real. Preservar e reduzir **o menor cenário que ainda é patológico**. Isso pode complementar pesquisa de [qualidade/diversidade de soluções](https://arxiv.org/abs/1504.04909) e localização histórica de regressão usando [git bisect](https://git-scm.com/docs/git-bisect).

**Não confundir:** cidade muito congestionada por decisões do jogador pode ser gameplay correta. O defeito é o custo computacional desproporcional, fila de scheduler esquecida ou quebra de causalidade sob carga. É essencial medir com hardware/build fixados, múltiplas execuções, warm-up e log do Core; não atribuir flutuação de GC ou sistema operacional a um algoritmo sem confirmação.

**Valor alto quando o jogo integrado existir**, mais forte que benchmark de função isolada; **custo de execução potencialmente alto**, logo fora do ciclo de toda edição.

### 22.12. O que é novidade real versus reciclagem de pesquisa anterior

| Linha de investigação | O que acrescenta de fato | Limite |
| --- | --- | --- |
| **Why-not (ausência de decisão)** | Encontra ação omitida e gatilho que **nunca disparou**; só “trace de ações” não detecta | Rastrear por ID/janela; nada de todas as alternativas de todos SIMs |
| **Cobertura semântica** | Guia os testes a **situações de gameplay inéditas**, não a funções nunca chamadas | Estados instrumentais não podem virar contrato |
| **Fluxos e contratos de transição** | Aponta **quem recebeu/consumiu indevidamente**, mesmo com total agregado correto | Seguir SPEC, não inventar conservação de recurso transformado |
| **Eventos raros ramificados** | Explora mais eficientemente condições muito improváveis | Distorce frequência natural; exige replay/checkpoint |
| **Cidades diversas MAP-Elites** | Mantém corpus de **fenômenos diferentes**, não só seeds redundantes | Dimensões ruins geram arquivo grande e falsa variedade |
| **Mineração de invariantes** | Descobre perguntas ainda não formuladas por humano | Observação **não é regra aprovada** |
| **Causas reversas/proveniência** | Explica resultado via pequeno **subconjunto de fatos relevantes** | Não confundir linhagem de dados com causalidade contrafactual |
| **Simetria/relabeling** | Detecta bugs de algoritmo dependentes de ID ou coordenada irrelevantes | Só em algoritmos cuja simetria é real e verificável |
| **Ciclos de espera** | Identifica onde cadeia de abastecimento/agenda pode parar | Falha de progresso nem sempre é bug |
| **Bots e RL** | Exploram escolhas de jogador não antecipadas | RL é custo muito alto para benefício incerto no estágio atual |
| **Perf adversarial** | Procura piores casos que inviabilizam aceleração | Requer cenário integrado e baseline; não é teste funcional |

### 22.13. Critérios de falsificação: como **rejeitar** ideias atraentes

- **Why-not:** em um cenário de teste, remover o agendamento de uma avaliação que já existe e conferir se o diagnóstico mostra “não avaliado” em vez de inventar “recusou”. Se não distinguir, a ferramenta não entrega a vantagem.
- **Cobertura semântica:** mesmo orçamento de tempo, comparar geração aleatória, pairwise e guia por estados. Exigir **novas classes de falha confirmadas** e diversidade útil, não mais contagens.
- **Fluxos de recurso:** alterar em cópia de teste o destinatário de um pagamento, criar reserva dupla, perder lote em transporte. Os testes locais devem acusar **mesmo que o total monetário global permaneça igual**.
- **Simetria:** renomear grafo de uma rota pequena cuja regra é invariável a ID e checar resultado após inversão do mapeamento. Se regra usar ID legitimamente, **o teste está errado**, não o Core.
- **Evento raro:** ramificar um estado próximo da inadimplência/herança e conferir replay fiel e diversidade real de continuações. Se quase todas forem equivalentes ou muito caras, rejeitar.
- **Zoológico de cidades:** comparar corpus diverso versus seeds aleatórias sob mesmo orçamento; rejeitar categorias que só medem mudança de escala ou ruído.
- **Invariantes aprendidas:** tentar gerar contraexemplos e validar contra a SPEC; descartar hipóteses que apenas descrevem comportamento acidental/errado.
- **Bot explorador:** comparar bot simples com script padrão antes de gastar recursos treinando agente RL.
- **Performance adversarial:** comparar pior caso encontrado com benchmark normal e registrar hardware/commit; rejeitar busca se só produzir ruído ou já encontrar o mesmo gargalo trivial.

### 22.14. O que definitivamente NÃO merece ser construído agora

**Não recomendar:** um simulador paralelo para simular o jogo; event sourcing integral de todas as decisões de cada SIM; hash do mundo inteiro em toda frame normal; checkpoints forenses contínuos para toda a população; hipervisor determinístico; monitoramento distribuído; IA/LLM que decida o “comportamento correto” sem SPEC; RL como primeira ferramenta; solver formal da cidade inteira; sistema de métricas que transforma observação em mecânica econômica; migração obrigatória de todos os testes para um novo framework.

Oportunidades mais poderosas **não implicam arquitetura mais complexa desde o primeiro dia**. A arquitetura do projeto já prevê Core C# separado do Godot, execução headless, sementes, persistência, comandos, diagnóstico e testes. A contribuição desta rodada é melhorar **oráculos, seleção de cenários e explicação de omissões**, não adicionar mais uma camada obrigatória ao produto.

### 22.15. Ordem de adoção mais justificável após as cinco rodadas

1. **Já no primeiro domínio implementado:** contratos pré/pós de ações críticas (dinheiro, posse, estoque), IDs e testes unitários focados. Não exigir rollout de ferramenta de mineração/IA.
2. **Quando scheduler e SIMs existirem:** cenários headless que exercitam a **mesma engine**; distinguir **avaliou/não avaliou/decidiu/aguarda reavaliação** no diagnóstico focado; guardar cobertura semântica de casos realmente importantes.
3. **Quando persistência existir:** provar reexecução com checkpoint/RNG/agenda/comandos, checar round-trip e sobrevivência a erro de gravação, sem prometer replay perfeito antes dos dados.
4. **Quando integrações e aceleração existirem:** testes metamórficos de tempo/FPS/câmera/cache, reavaliação de eventos ausentes, monitoramento da fila e testes adversariais de performance.
5. **Quando falhas raras forem observadas:** ramos de checkpoint, redução de casos, corpus de situações extremas e trace causal reverso — tudo seletivo.
6. **Só se a geração simples ficar insuficiente:** considerar qualidade/diversidade automática, mineração de invariantes ou bot curioso. A decisão depende de experimentos que mostrem benefícios, não de prestígio das técnicas.

**Parecer final:** a pesquisa ampla pode ser **encerrada como exploração de opções**, sem fechar implementação nem presumir que está provada. A melhor melhoria da última rodada é o trio **(a) teste das transições reais + (b) cobertura de estados significativos + (c) explicações tanto para eventos que aconteceram como para eventos que deixaram de acontecer**.

## 23. Bibliografia nova e proveniência da quinta rodada

Estas fontes, publicadas pelos próprios autores, instituições, conferências ou repositórios dos projetos, acrescentam temas que **não eram o foco** nas seções 1–21. Artigos sobre bancos, segurança ou física sustentam a **existência do método**, não os resultados esperados no jogo. Não há benchmark do IndexCities nesta rodada.

**Explicar o que aconteceu e o que faltou:**
- [Buneman et al., Why and Where (ICDT 2001)](https://doi.org/10.1007/3-540-44503-X_20)
- [Why-not provenance por grafos com negação (2017)](https://arxiv.org/abs/1701.05699)
- [Provenance in Databases — Why, How and Where (2009)](https://www.research.ed.ac.uk/en/publications/provenance-in-databases-why-how-and-where/)
- [Database Queries that Explain their Work (2014)](https://arxiv.org/abs/1408.1675)
- [ICDE 2022 — demonstração de proveniência SQL](https://db.cs.uni-tuebingen.de/publications/2022/how-where-and-why-data-provenance-improves-query-debugging--a-visual-demonstration-of-fine-grained-provenance-analysis-for-sql/)
- [Dynamic program slicing (PLDI 1990)](https://doi.org/10.1145/93542.93576)

**Explorar situações difíceis com menos casos redundantes:**
- [StateFuzz (USENIX 2022)](https://www.usenix.org/conference/usenixsecurity22/presentation/zhao-bodong)
- [StateAFL (código aberto)](https://github.com/stateafl/stateafl)
- [Model-Guided Fuzzing (OOPSLA 2025)](https://repository.tudelft.nl/record/uuid:66d18d3c-fead-4df0-8310-5df11370db13)
- [Data Coverage for Guided Fuzzing (USENIX 2024)](https://www.usenix.org/conference/usenixsecurity24/presentation/wang-mingzhe)
- [Adaptive Multilevel Splitting (2019)](https://doi.org/10.1063/1.5082247)
- [Adaptive Multilevel Splitting — estudo de física molecular](https://pmc.ncbi.nlm.nih.gov/articles/PMC4440697/)
- [Mouret e Clune — MAP-Elites (2015)](https://arxiv.org/abs/1504.04909)
- [Quality-diversity aplicado a testes (artigo de 2024)](https://research.birmingham.ac.uk/en/publications/automated-test-suite-generation-for-software-product-lines-based-/)
- [Código da pesquisa de geração MAP-Elites](https://github.com/gzhuxiangyi/SPLTestingMAP)

**Novos oráculos e ferramentas exploratórias:**
- [Daikon — inferência dinâmica de prováveis invariantes](https://plse.cs.washington.edu/daikon/)
- [Daikon — análise de programas C#](https://plse.cs.washington.edu/daikon/download/doc/daikon/Example-usage.html)
- [NetworkX — isomorfismo](https://networkx.org/documentation/stable/reference/algorithms/isomorphism.html)
- [Pesquisa de padrões metamórficos (2025)](https://doi.org/10.1002/stvr.70003)
- [PetriDotNet — aplicações industriais](https://doi.org/10.1016/j.scico.2017.09.003)
- [Brasil: medição de inventário via redes de Petri](https://www.scielo.br/j/gp/a/4xzqzggzCy9ZyPzPHvjvczK/?lang=pt)
- [EA SEED — Automatic Gameplay Testing with Curiosity](https://www.ea.com/seed/news/gameplay-testing-curiosity)
- [ASE 2022 — testar jogos 3D com agentes RL](https://doi.org/10.1145/3551349.3560507)
- [Git — bisect de regressões](https://git-scm.com/docs/git-bisect)

---

**Quinta rodada encerrada como pesquisa, revisão humana PENDENTE.** Nenhum código, CI, biblioteca, benchmark, nova funcionalidade de gameplay ou alteração canônica foi criado. A única alteração pretendida é esta documentação exploratória e sua entrada no índice. As incertezas fundamentais são mensuráveis **somente quando houver Core real**.
