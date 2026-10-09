# Viabilidade de cache para a simulação causal do IndexCities

> **Revisão humana: PENDENTE.** Pesquisa/avaliação produzida por IA em **2026-10-09**, a partir da hipótese do responsável de **guardar e reutilizar a maior parte dos cálculos de uma cidade previsível**. Não é decisão oficial, prova de desempenho, alteração arquitetural ou autorização de desenvolvimento.
>
> **Pergunta:** até onde podemos **executar resultados de cache em vez de recalcular operações**, preservando absolutamente todos os SIMs, veículos, empresas, estoques, deslocamentos e consequências reais? Resposta: **sim, para cálculos e transições locais cujas entradas, dependências, ordem e efeitos são equivalentes; não há evidência de viabilidade de cachear a maioria das sequências de acontecimentos do mundo inteiro.** Medir.
>
> **Autoridades:** [SPEC: tempo de jogo](../SPEC.md#tempo-de-jogo), [SPEC: tráfego](../SPEC.md#comportamento-detalhado-do-tráfego), [ARCHITECTURE](../ARCHITECTURE.md), [AGENTS](../../AGENTS.md). Contexto: [motor de simulação](simulation-engine-performance-research.md), [pesquisa transversal](simulation-engine-cross-domain-research.md) e [hipóteses radicais](simulation-engine-radical-hypotheses.md). Esta análise **não altera** a SPEC nem a ARCHITECTURE.

## 1. Veredito resumido

**O cache é tecnicamente viável e merece alta prioridade como família de otimizações.** Isso é sustentado por Factorio, algoritmos incrementais e artigos de memoização *dentro de simulações*, inclusive para blocos que modificam estado.

**Mas existem três propostas diferentes que não devem ser confundidas:**

1. **Cache de cálculo:** o mesmo problema produz a mesma resposta (distância, rota candidata, necessidade de insumo, elegibilidade, ranking). Reutilizar o cálculo; **executar normalmente toda ação real** que depende da resposta. **Alta viabilidade**.
2. **Cache de evolução/trecho:** guardar uma solução física/operacional sob condições explícitas (trajetória livre, produção estável); usar enquanto nenhuma dependência mudar, resolvendo no instante correto cada interação. **Viável em casos restritos; caro ou inseguro em tráfego denso sem limites conservadores**.
3. **Cache de acontecimentos com efeitos:** guardar uma transformação de estado, inclusive efeitos colaterais, para executá-la novamente sob as mesmas entradas e na mesma ordem causal. **Há pesquisa que prova viabilidade em modelos específicos, mas não há evidência para a maior parte de uma cidade com competição e decisões autônomas**. A unidade deve ser **pequena, auditável e revalidável**.

**Não** confundir isso com *replay de vídeo*, simulação apenas perto da câmera, gravar o calendário seguinte e reproduzi-lo mesmo com trânsito alterado, ou dados agregados fictícios. Essas versões contrariam a SPEC.

## 2. Evidências primárias realmente decisivas

### E1 — Factorio: cache de rotas, pedaços de rotas e resultados negativos (produção)

- [FFF #121: Path Finder Optimisation II](https://www.factorio.com/blog/post/fff-121) descreve diretamente um cache de rotas de tamanho limitado; busca no **interior de rotas guardadas**, conectando novos inícios/fins a trechos úteis, e guarda até respostas de **não existir caminho**.
- [API oficial atual, PathFinderMapSettings](https://lua-api.factorio.com/latest/concepts/PathFinderMapSettings.html) expõe configuração para habilitar cache e limites de memória/aceitação (o tamanho do cache do jogo não deve ser copiado como parâmetro do IndexCities).
- [FFF #317](https://www.factorio.com/blog/post/fff-317) apresenta uso de busca hierárquica e reuso da estrutura de busca numa malha que muda. Não significa que colisões físicas e decisões de todos os veículos sejam reproduzidas de cache.

**Lição:** um cálculo cara pode ser aproveitado **parcialmente**, não apenas repetir um resultado A→B idêntico. Esta é provavelmente uma das oportunidades mais concretas de cache para a mobilidade do IndexCities: caminhos macro, porções da rede, conectividade e informação estática. Um caminho guardado **não determina** uma viagem fisicamente livre no presente.

### E2 — MemoSim / RWTH Aachen: cache de código com efeitos colaterais (pesquisa diretamente pertinente)

- [Projeto MemoSim dos pesquisadores](https://www.comsys.rwth-aachen.de/research/projects/memosim/).
- [Stoffers et al., ACM TOMACS 2018, artigo na página do autor](https://daniel.schemmel.net/publication/2018-automated-memoization/) e [PDF integral](https://daniel.schemmel.net/publication/2018-automated-memoization.pdf); [DOI](https://doi.org/10.1145/3186316).
- [Artigo SIGSIM-PADS 2016, automação para linguagens impuras](https://www.comsys.rwth-aachen.de/publication/2016/2016_stoffers_automated-memoization-for-parameter/).

**Como funciona:** a pesquisa decompõe um bloco C++ em **valores externos lidos** (vetor de entradas) e **valores/locais modificados** (vetor de saídas). Se as entradas exatas reaparecem, a ferramenta pode reaplicar as saídas sem refazer a computação cara. Quando encontra operações que não consegue representar com segurança, **não memoiza e executa o código normal**. O artigo relata **mais de 80× num estudo de simulação de rede OFDM**, considerando as condições daquele estudo de parâmetros.

**Ressalvas críticas lidas no artigo:** efeitos como **alocação/desalocação, I/O e chamadas opacas** não são suportados genericamente; o exemplo de implementação considera **uma unidade memoizada por vez / restrições de threads**; comparar/capturar entradas e restaurar saídas tem custo; exige pelo menos **a segunda execução da mesma entrada** para que o cache compense. O contexto predominante é **execução repetida de experimentos/estudos paramétricos**, não inevitavelmente um jogo com estado sempre mutável.

**Relevância muito forte:** torna a proposta de cache de transições reais **cientificamente séria**. **Não conclui** que podemos aplicar um arquivo de salários, vendas, congestionamentos ou estoque previamente gravado sem revalidar todos os recursos e ações. A pesquisa não é ferramenta C# pronta para nosso Core.

### E3 — Simulação por eventos com memoização de agendamento

[Kwon, Han e Lee, *Speed Optimization in DEVS-Based Simulations: A Memoization Approach*, 2023](https://doi.org/10.3390/app132312958) compara um motor DEVS hierárquico com coordenador que cacheia **descoberta do próximo evento/avanço temporal**. Relata de **7,4 a 11,7×** nos cenários estudados de hierarquia extrema/RTL-DEVS.

**Lição específica:** memoizar a *estrutura do processamento* e a decisão de **quem acorda depois** pode valer mais do que copiar o resultado da atividade. **Não** mede aceleração de gameplay de IndexCities.

### E4 — Wormhole, USENIX NSDI 2026: reutilizar trechos inteiros de simulação de rede

[Paper e apresentação oficiais](https://www.usenix.org/conference/nsdi26/presentation/long); [PDF dos autores](https://www.usenix.org/system/files/nsdi26-long.pdf); [arXiv](https://arxiv.org/abs/2602.10615).

O sistema memoiza **padrões de competição por recursos de rede** e identifica **regimes estáveis**, evitando um enorme número de eventos de pacotes. Relata **744× sobre ns-3** num cenário GPT e **510×** em MoE, ou mais combinando paralelismo. **Os autores reportam erro inferior a 1% em métricas de conclusão de fluxos**, e a técnica acelera um padrão específico de rede de treinamento de modelos, não uma cidade com 100 mil consumidores.

**Este é o caso mais próximo do sonho de cachear sequências grandes, mas NÃO certifica equivalência estrita** exigida pela SPEC. Mesmo que o erro agregado seja pequeno, um produto não entregue, uma fila ignorada ou uma compra que mudou de ordem são efeitos diferentes e importantes em gameplay. Ganho medido no artigo **não pode** virar meta numérica do jogo.

### E5 — Salsa: cache com dependências e revisão de estado

[Algoritmo red–green](https://github.com/salsa-rs/salsa/blob/master/book/src/reference/algorithm.md); [documentação de funções rastreadas](https://docs.rs/salsa/latest/salsa/attr.tracked.html).

Uma função de consulta registra os **inputs/campos efetivamente acessados** e as versões; se eles não mudaram, retorna resposta guardada. Mesmo se algum input mudou, pode recalcular e detectar que o **resultado derivado permaneceu igual**, impedindo invalidações desnecessárias em cadeia.

**Lição:** cache funciona melhor quando registra **qual mudança realmente altera a resposta**, e não descarta tudo a cada mudança na cidade. Para IndexCities: oportunidades de emprego, distância/acessibilidade, conectividade, fornecedores elegíveis e alguns índices/painéis.

**Limite:** funções do Salsa são consultas **sem alterações autoritativas**; não usar essa semântica para dispensar processamento real de compra, folha ou entrega.

### E6 — Adapton: cache sob demanda, não recomputar o mundo inteiro

[Adapton, PLDI 2014](https://matthewhammer.org/adapton/): resultados intermediários são rastreados por **grafo de dependências** e recomputados conforme o que realmente é requisitado. Útil como princípio de consultas e oportunidades, não justificativa para interromper acontecimentos offscreen. **Uma consulta pode ser sob demanda; o mundo autoritativo não.**

### E7 — D* Lite: conservar esforço de busca anterior

[D* Lite, artigo da Carnegie Mellon](https://publications.ri.cmu.edu/d-lite): algoritmos incrementais podem aproveitar buscas anteriores quando o ambiente muda. A rota de um SIM pode ser **replanejada** sem calcular do zero em certas condições.

**Atenção:** não cachear custo temporal do trajeto como se congestionamento atual fosse igual ao de ontem; nem declarar veículo chegado por rota prevista.

### E8 — Determinismo, replay e por que gravar “o filme da cidade” não serve

[Gaffer on Games — Deterministic Lockstep](https://gafferongames.com/post/deterministic_lockstep/): com **mesmo estado inicial e entradas**, a simulação determinística **executada novamente** pode produzir o mesmo resultado. Isso não elimina o custo da física; e sistemas físicos nem sempre são determinísticos entre hardware/runtime diferentes.

[Snapshot Interpolation](https://gafferongames.com/post/snapshot_interpolation/) produz **representação visual** a partir de estados gravados, sem executar a simulação física no cliente. Útil como conceito de apresentação, mas **não substitui o Core** de um jogo cuja economia e física acontecem realmente.

**Lição:** reproduzir vídeo/snapshots de ontem **não aplica transações e colisões de hoje**; mesmo replay determinístico precisaria executar a simulação de novo, a menos que se repliquem corretamente **todas** as transformações de estado e observações.

### E9 — .NET: memória e invalidação de caches

[Microsoft Learn — Caching in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching): bibliotecas oferecem tokens de alteração, prioridades e limites de tamanho. [Microsoft Learn — Memory Cache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory) recomenda fallback seguro, limite de tamanho e cuidado com entradas arbitrárias.

**Lição:** um cache não deve crescer até virar problema de GC/memória. **TTL por segundos/horas não prova validade causal** para estoque, prioridade e tráfego. Para consultas determinísticas, versões de dependências e limites de ocupação são mais apropriados.

## 3. Mapa concreto de onde cache parece ganhar, e onde não

**Classificações são hipóteses técnicas minhas, NÃO ganhos medidos do IndexCities.** Viabilidade depende de frequência de repetição, custo de produzir resposta e invalidações.

| Trabalho da simulação | Cache recomendado? | O que se guarda | O que continua 100% real |
| --- | --- | --- | --- |
| Rede de ruas, ligações, acessibilidade e componentes | **Sim, forte** | Estrutura, conexões e índices enquanto trechos não mudam | Carros, congestionamento, limites físicos e passagem |
| Rotas e sub-rotas de casas→comércios/empregos | **Sim, forte** | Busca macro, partes reusáveis, caminhos sem saída | Escolha atual, posição, faixa, preferência, trânsito, viagem e parada |
| Planejamento fino de faixa e viagem em estrada livre | **Experimental, local** | Segmento de trajetória **com certificação de condições e instante seguro** | Frenagem, entrada de terceiros, ultrapassagem, cruzamento, bloqueio |
| Trânsito denso em rotatória/semáforo | **Fraco para replay completo** | Geometria estática, regras, vizinhos/indexação; talvez pedaços estáveis | Competição, filas, mudanças de faixa, atraso, cada veículo |
| Consultas de vagas/mercados/fornecedores | **Sim, seletivo** | Índice de candidatos elegíveis e resultados que não mudaram | Cada SIM/empresa decide e negocia nas regras, desloca-se e confirma negócio |
| Rotinas diárias de SIMs | **Parcial** | Regras/cálculos/condições de turno, custo de decisão repetida | SIM decide mudar emprego/casa/destino quando as condições justificam |
| Produção com insumos e energia estáveis | **Sim, com gatilho** | Fórmula, horizonte até concluir lote/precisar reavaliar | Estoques, recursos, disponibilidade de trabalhador e lote concluído no tempo correto |
| Fórmulas de salário/aluguel/contas | **Sim, forte nos cálculos** | Fórmula derivada, parâmetros e resultados repetidos | Dívida, dinheiro transferido, titular/devedor, vencimento e inadimplência |
| Compras de último produto, aquisição de imóvel, contratação | **Não para replay da operação** | Pré-consultas, validações que continuam verdadeiras | Ordem real de disputa, presença, estoque, saldo e titularidade |
| Estatísticas de UI e alertas | **Sim, forte** | Agregações derivadas e diffs de estado | Verdade das entidades originais e causas rastreáveis |
| Visualização/animação | **Sim, mas só visual** | Instâncias, recursos gráficos, geometria e interpolação | O mundo físico fora da câmera não muda |

**Observação fundamental:** “cachear a maior parte” só terá sentido se medirmos **porcentagem do tempo de CPU economizado**, não porcentagem de entidades com algum campo memoizado. Guardar uma fórmula trivial usada por 100 mil SIMs pode economizar menos que reaproveitar algumas buscas de rotas caríssimas.

## 4. Algoritmo de cache seguro — modelo de pesquisa, não arquitetura aprovada

### 4.1. Cache de consulta (via mais confiável)

Uma entrada conceitual contém:

- **chave lógica:** tipo de consulta e entradas realmente significativas (origem, destino, restrições, categoria, parâmetros específicos, sem incluir ID do agente se a escolha independer dele);
- **resultado derivado:** rota, ranking, elegibilidade, conectividade, capacidade derivada;
- **dependências e versões:** segmentos da rede, disponibilidade relevante, critérios, estado individual lido;
- **limite de armazenamento e política de descarte**, preferindo cálculos que custam caro e se repetem.

Antes de usar, conferir versões de **todos os dados relevantes** que o cálculo leu. Se não correspondem, recomputar e registrar o novo conjunto de dependências. Um número de versão GLOBAL da cidade invalidaria tudo e provavelmente anularia boa parte do ganho; versões específicas por domínio/recurso são hipótese melhor.

**Não confundir mesmo resultado antigo com estado autoritativo atual:** o mercado ainda pode vender zero produtos depois da consulta; a efetivação precisa verificar disponibilidade verdadeira no momento correto.

### 4.2. Cache de transição com efeitos (fronteira experimental mais valiosa)

Inspirado no MemoSim: para uma **operação pequena** com precondições suficientes e efeitos observáveis delimitados:

1. Capturar somente o conjunto de **entradas lidas** (ou suas versões mais conteúdo quando necessário) e contexto causal relevante (tempo, ID/contador de RNG legítimo, regras em vigor).
2. Guardar uma **transformação** ou vetor de saídas (incluindo quem teria seu estado alterado); não copiar saldos/IDs de pessoas diferentes como se fossem intercambiáveis.
3. Quando a mesma classe de operação reaparecer, verificar que **todas** as entradas relevantes são equivalentes. Se não, executar o caminho comum.
4. Se o resultado puder ser reutilizado, **aplicá-lo no instante devido**, respeitando ordem de competição por recursos, logs/efeitos necessários, novas dependências e confirmações de estado.
5. Se surgir efeito que não foi modelado (novo SIM, vaga, side effect de serviço, callback, I/O, custo temporal, movimento ou dependência oculta), **abortar o cache**, não assumir equivalência.
6. Não memoizar grandes blocos em que a identidade e o estado de dezenas de milhares de terceiros fazem parte da entrada: a chance de repetição pode tender a zero, e inspecionar a chave custará mais que recalcular.

**Exemplo que pode funcionar:** rotina pura/reproduzível de calcular custo de produzir lote de tamanho X com receitas/insumos/energia/eficiência definidas; manter separada a compra/consumo físico desses insumos e a produção efetiva. **Exemplo experimental mais ousado:** execução da transformação de um lote em condições idênticas com vetor completo de leituras/escritas, mas isso exige identificar transições intermédias e quem pode observá-las.

**Exemplo que NÃO funciona:** gravar “SIM João saiu às 8h, comprou pão, recebeu R$ 100, chegou às 9h” e reproduzir depois sem revalidar estoque do pão, trânsito, presença física, salário devido, fluxo de dinheiro, e escolhas do próprio SIM.

### 4.3. Cache de sequência de acontecimentos (não é equivalente a uma gravação)

Guardar uma **sequência de operações possíveis** com condições/versões pode ser mais interessante que guardar todos os resultados. Em cada passo:
- testar se as condições ainda são verdadeiras no instante correto;
- reutilizar cálculos/ordem enquanto válida;
- **confirmar cada efeito verdadeiro** (movimento, retirada do estoque, débito, crédito, chegada);
- ao primeiro ponto de divergência, **interromper a reutilização e retornar ao motor normal**; não executar eventos posteriores da gravação.

**Possibilidade mais radical:** se um componente for de fato **causalmente fechado/isolado** num intervalo, sem entradas de terceiros, e pudermos compor matematicamente sua evolução sem perder transições observáveis, reutilizar uma transformação completa. Entretanto, verificar isolamento e cada pré-condição pode custar mais que executar; a cidade raramente é isolada. Não criar “modo fast-forward de replay” como regra de gameplay.

### 4.4. Cache parcial/transversal

**Uma oportunidade além do simples resultado:** cachear uma **subsolução**. Factorio usa trechos de rotas; um compilador incremental retém resultados de subconsultas. Em IndexCities:

- reutilizar acesso bairro→ponte ou casa→eixo de transporte sem congelar a decisão de faixa/trânsito;
- reutilizar candidatos de fornecedor por categoria e endereço, **avaliando preço/estoque atual e a escolha autônoma**;
- compartilhar **dados estáticos da geometria**, enquanto cada veículo/pedestre tem seu estado dinâmico real.

Isso pode ter taxas de acerto melhores do que exigir que uma cidade inteira se repita exatamente.

## 5. Por que “cachear quase tudo” provavelmente NÃO funcionará como replay de mundo

**A crítica mais importante à hipótese original:** uma cidade é previsível em muitas **leis e rotinas**, mas não necessariamente reproduz o mesmo **estado completo**.

- O preço, saldo, estoque, presença, horário e fila mudam; uma única variável pode produzir outra escolha, que altera próximas compras, trabalho, consumo e deslocamento.
- Se a chave contém “toda a cidade”, quase nunca haverá duas situações idênticas; calcular o hash da cidade pode custar mais que as consultas individuais.
- Se a chave é incompleta, o cache vira incorreto: carro atravessa congestionamento, duas pessoas compram o mesmo item, família evita dívida sem transferência real.
- Grandes sequências gravadas exigem memória, escrita, leitura e invalidação; reconstruí-las e verificar condições pode custar mais do que executar operações curtas.
- O mesmo padrão diário (“vai trabalhar”) **não garante a mesma trajetória, vaga, horário, empresa, saldo ou disponibilidade de transporte**.
- Qualquer técnica de “cache aproximado” exige decisão de produto antes de aceitar resultados diferentes. A regra atual é equivalência causal, não só médias parecidas.

**O problema não é o cache; é confundir repetição de PADRÃO com repetição de ENTRADAS.** Duas compras parecem iguais, mas não têm necessariamente as mesmas condições nem os mesmos efeitos.

**Não temos nenhum dado de hit rate, custo de invalidar ou memória do IndexCities**. Não é possível afirmar que a maior parte da CPU será cacheável antes de executar cenários com a simulação verdadeira.

## 6. Custo versus benefício: critério quantitativo correto

Não medir só taxa de acerto ou FPS; estimar a economia líquida:

**Economia = acertos × (custo do cálculo normal − consulta do cache − validar dependências − reaplicar efeitos) − faltas × (consulta + custo adicional de registrar) − custo de invalidações, memória e GC.**

Para cálculos sem efeitos a parte “reaplicar efeitos” é pequena ou zero. Para compras/contratações pode aproximar-se do próprio custo de execução e destruir a economia.

Se **consultar + validar + reaplicar** já custa o mesmo que computar normalmente, **100% de acertos não salva a ideia**.

Os autores de MemoSim mostram explicitamente esse dilema, oferecem fallback e descarte; .NET recomenda limitar memória; Factorio usa cache limitado e reaproveita caminhos difíceis. Nenhum benchmark externo fornece percentuais confiáveis para a composição de cargas deste jogo.

**Instrumentação que realmente decide:**

- tempo total e por domínio em cidade integrada, **segundos simulados processados por segundo real**;
- frequência de cálculo por tipo, custo sem cache, taxa de repetição de entradas relevantes e custo de consultar chave;
- taxa de acerto REAL e valor do cálculo poupado (acerto em função barata não vale quase nada);
- quantas entradas são invalidadas por: construção, crise energética, ponte fechada, estoques, emprego, decisões de SIMs, chuva/tempo (se existir na SPEC);
- invalidação em cascata, memória retida por cache, bytes escritos/lidos, GC, p95/p99;
- por versão de algoritmo: **mesmos saldos, mercadorias, filas, destinos, horários e razões das decisões**, não apenas agregados e FPS;
- desempenho com **cidade estável** e **cidade mudando o tempo inteiro**, bem como congestionamento saturado.

## 7. Experimento discriminante proporcional (sem fase de POC obrigatória)

**Proposta técnica opcional a aplicar no desenvolvimento integrado quando houver código e perfil** — não meta oficial nem novo requisito.

| Caso | Versão de referência | Versão cache | Aprovar somente se |
| --- | --- | --- | --- |
| 10 mil viagens casa/trabalho em malha estável | cálculo normal de rota a cada pedido | cache de trechos, conectividade e buscas reutilizáveis | cada SIM tem destino/rota válida; filas/viagens reais idênticas; CPU líquido menor |
| jogador fecha a ponte com 5 mil deslocamentos | refaz rotas necessárias | invalida apenas subrotas/dependentes atingidos | ninguém atravessa bloqueio; rotas afetadas e não afetadas corretas |
| mercado com um único pão, dois compradores | transações reais em ordem | memoização só da busca/cálculo prévio | um único comprador recebe pão; saldo/devedor/estoque reais |
| fábrica com 10 ciclos regulares | cálculo normal do progresso/lotes | memoização ou contrato temporal de ciclo com gatilhos | cada lote/insumo/energia/jornada vence nos instantes corretos |
| falta inesperada de energia na fábrica | interrompe conforme condições | cache de trecho inválido cai para execução normal | nenhuma produção/cobrança fictícia; mesma sequência causal |
| congestionamento pesado e mudanças de faixa | simulação física de referência | rotas estáticas em cache e dinâmica detalhada | nenhuma chegada antecipada, cruzamento ou colisão omitida |
| cidade pacífica 10 dias x cidade em crise 10 dias | eventos normais | caches localizados combinados | vantagem líquida em cenário variado, e sem divergência não aprovada |

**Critério de adoção:** cache local isolável, memória controlada, ganho líquido repetível no hardware alvo **e nenhum acontecimento inconsistente**; se não conseguir provar dependências de um bloco, não memoizar o bloco.

## 8. Estratégias comparadas — minha escolha

| Estratégia | Potencial | Confiabilidade para SPEC | Custo | Minha posição |
| --- | --- | --- | --- | --- |
| Memoizar rotas/sub-rotas e conectividade | Alto se buscas dominarem | Alto com invalidação correta | Baixo/médio | **Primeiro candidato** |
| Memoizar resultados de decisões/consultas puras | Alto se mesmas entradas reaparecem | Alto se separar ação real | Médio | **Primeiro candidato** |
| Cache por dependência fina e versão | Muito transversal | Alto se lista de leituras é completa | Médio | **Fundamento, mas localizado** |
| Memoização de blocos com efeitos (MemoSim) | Pode ser alto em blocos caros repetidos | Possível com read/write set/ordem completos | Alto | **Investigar em um fluxo restrito** |
| Trechos temporais físicos com certificado | Potencialmente alto em trânsito livre | Incerto em rede urbana; requer garantia | Alto | **Experimento técnico separado** |
| Cache de “roteiro diário” com ações tentativas | Limitado como planejamento | Depende de revalidar cada ação | Médio/alto | **Não é prioridade** |
| Replay de acontecimentos reais já gravados | Baixo se estado nunca repete | **Inaceitável sem equivalência** | Muito alto | **Não adotar** |
| Cache global de vários dias de cidade | Sem evidência de acerto ou isolamento | Muito difícil | Muito alto | **Descartar como arquitetura inicial** |
| Cache visual/interpolação | Alto para apresentação | Não altera Core | Baixo/médio | Válido, **não acelera tempo lógico** |
| Wormhole aproximado aplicado à cidade | Paper impressionante em outra área | **Erro permitido na fonte** | Alto | Não adotar para verdade do jogo |

**Nota de autoridade:** a ARCHITECTURE já separa Core de Godot, usa IDs e agenda de atualizações, permite otimizações por profiling. Tudo aqui pode ser **investigado** dentro da arquitetura atual. Um cache global autoritativo novo seria alteração estrutural importante e exigiria decisão expressa; esta pesquisa **não a aprova**.

## Conclusão — o que eu realmente recomendo ao IndexCities

> **Revisão humana desta conclusão: PENDENTE.** Parecer técnico da IA, não decisão do responsável.

**SIM: reutilização de cálculos por cache tem viabilidade comprovada em softwares e estudos científicos. E existe evidência forte até para memoização de blocos que alteram estado.** Portanto a intuição do responsável é boa e merece peso real na investigação do motor.

**NÃO: não encontramos base para afirmar que será possível reproduzir diretamente do cache a maioria dos acontecimentos de uma cidade continuamente mutável, com precisão individual.** O que se repete com frequência pode ser **a fórmula, a busca, a sub-rota, a condição, a dependência**, não o fato concreto de que João recebeu salário, Carlos comprou o último pão ou um ônibus cruzou a avenida.

**Minha proposta preferida, em três camadas, sem criar arquitetura nova prematuramente:**

1. **Cache forte de resultados derivados e partes de caminhos** (Factorio, Salsa, D* Lite) — a implementação do mundo segue real, com estado autoritativo único.
2. **Cache dependente de estado/tempo** para trechos previsíveis de produção e circulação, com invalidação conservadora; isso conversa diretamente com os horizontes causais das pesquisas anteriores.
3. **Memoização seletiva de uma transformação real cara** somente quando conseguimos capturar todas as entradas e efeitos (lição MemoSim), validar os estados relevantes e aplicá-los na mesma ordem que a execução não cacheada. Caso contrário, **fallback normal sempre**.

**O ajuste mais importante da pesquisa anterior:** antes falávamos em “compilar próximos acontecimentos”; agora há evidência quantitativa de que **memoização de cálculos com efeitos pode mesmo ser valiosa**, mas também evidência de que **granularidade pequena, identificação completa de dependências e fallback** são o que tornam essa ideia segura. O sonho da “grande maioria da CPU em cache” continua uma **hipótese de performance a medir**, não conclusão.

**Ponto de decisão prático:** na implementação integrada, medir 3 famílias separadas — **busca de rotas**, **consultas econômicas**, **trechos físicos/operacionais estáveis**. A primeira que mostrar alto custo e repetição verdadeira recebe cache localizado, com comparação funcional e benchmarks reais. Nenhuma POC de sistema de produto é etapa obrigatória.

**Status final: PENDENTE. Nenhuma alteração na SPEC, ARCHITECTURE ou código.**
