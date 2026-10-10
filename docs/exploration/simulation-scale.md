# IndexCities — Escala, performance e baselines da simulação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o conteúdo deste documento ainda não foi revisado integralmente pelo responsável e não pode ser tratado como decisão.
>

> **Status:** exploração ativa e referência de calibração — não é fonte de verdade.
>
> Concentra pesquisa sobre população, profundidade individual, limites técnicos, tempo de jogo e baselines empíricos usados para orientar protótipos e benchmarks. Números provisórios não viram requisitos sem promoção explícita para a SPEC.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Calendário — pergunta 1 ainda aberta (2026-10-09)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **pediu esclarecer** quantos dias compõem um mês curto (alternativa B), mas **NÃO aprovou** B nem nenhum número. A aceleração real de todos os acontecimentos já está aprovada na SPEC; **não reabrir nem relativizar** essa regra.

**Exemplos matemáticos, NÃO parâmetros decididos:**

| Dias por mês | Dias por ano (12 meses) | Prazo de três meses |
| --- | --- | --- |
| 10 | 120 | 30 dias da simulação |
| 15 | 180 | 45 dias da simulação |
| 30 | 360 | 90 dias da simulação |

Um mês mais curto **não torna mais eficiente simular cada dia**; apenas faz haver menos jornadas reais por ano fictício. Aluguéis mensais, salários e despesas recorrentes passariam a vencer mais frequentemente em relação aos dias de rotina. Envelhecimento e nascimento mudariam sua proporção com deslocamentos/consumo. O responsável já expressou anteriormente receio de calendários fictícios com poucos dias por mês; portanto a IA **não deve declarar 10 ou 15 dias aprovados** nem criar dois relógios divergentes para mascarar o impacto. **Sugestão não aprovada:** comparar 15 e 30 dias por mês quanto a ritmo de vida, viabilidade da economia, jogabilidade e performance; pode-se manter meses convencionais com aceleração real eficiente se a compressão prejudicar a coerência.

---

## Escala da cidade, profundidade da simulação e tempo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração.

Esta seção reúne evidências para responder às primeiras perguntas de escala do IndexCities. Os números abaixo não são requisitos do produto enquanto não houver uma decisão explícita.

### 1. Qual população representa uma cidade pequena, mas funcionalmente completa?

Não existe um número universal em que uma cidade passe a ter automaticamente hospital, polícia, bombeiros, comércio e demais serviços. Esses serviços dependem de organização regional, política pública, renda, densidade e papel da cidade na região.

Ainda assim, há referências reais úteis para dimensionamento:

- O Censo 2022 do IBGE registrou média nacional de **2,79 moradores por domicílio particular permanente ocupado**. Na Região Sul, a média foi **2,64**.
- O Ministério da Saúde usa parâmetros de população vinculada por equipe de Saúde da Família. Em municípios de até 20 mil habitantes, o parâmetro é **2.000 pessoas por equipe**; de 20.001 a 50 mil, **2.500**; de 50.001 a 100 mil, **2.750**; acima de 100 mil, **3.000**.
- A política federal de CAPS considera **15 mil habitantes** como patamar a partir do qual um CAPS I é indicado/elegível, enquanto modalidades mais especializadas aparecem em faixas maiores.
- O CNES registra nacionalmente milhares de hospitais, prontos atendimentos, farmácias, UBSs e outros estabelecimentos, e fornece dados por município. Portanto, ele pode ser usado depois para comparar cidades reais específicas.
- IBGE/CEMPRE e RAIS fornecem dados de empresas, unidades locais e empregos por setor e município. Esses dados são melhores que uma razão inventada para estimar comércio e indústria.

Usando somente a média domiciliar nacional de 2,79 como referência estatística:

| População | Domicílios ocupados equivalentes |
| ---: | ---: |
| 15.000 | ~5.376 |
| 20.000 | ~7.168 |
| 30.000 | ~10.753 |
| 50.000 | ~17.921 |

Esses valores são apenas conversão população/domicílio, não uma quantidade decidida de lotes ou casas para o jogo.

**Hipótese para investigar:** a faixa de **15 mil a 50 mil habitantes** é especialmente útil para o primeiro estudo porque já corresponde, no mundo real, a municípios capazes de sustentar uma variedade significativa de serviços e, ao mesmo tempo, continua numa escala muito inferior à de grandes metrópoles. Isso ainda precisa ser validado comparando municípios reais e sua estrutura urbana.

**Próximo passo baseado em dados:** selecionar uma amostra de municípios reais entre 15 mil e 50 mil habitantes e levantar, para cada um, população, domicílios, unidades de saúde, escolas, empregos, comércio, indústria, polícia e bombeiros. Só depois definir uma faixa-alvo para o jogo.

Fontes:
- IBGE, Censo Demográfico 2022: https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102011.pdf
- Ministério da Saúde, parâmetro populacional das equipes de Saúde da Família: https://www.gov.br/saude/pt-br/composicao/saps/esf/equipe-saude-da-familia
- Ministério da Saúde, CAPS: https://www.gov.br/saude/pt-br/composicao/saes/desmad/raps/caps/caps
- CNES / Dados Abertos SUS: https://dadosabertos.saude.gov.br/dataset/cnes-cadastro-nacional-de-estabelecimentos-de-saude
- IBGE, Cadastro Central de Empresas: https://www.ibge.gov.br/estatisticas/economicas/servicos/9016-estatisticas-do-cadastro-central-de-empresas.html
- MTE, RAIS 2024: https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/estatisticas-trabalho/rais/rais-2024/rais-2024-1

### 2. Até onde é viável simular cidadãos e veículos individualmente?

Há evidência forte de que simulações baseadas em agentes podem trabalhar com populações grandes, mas isso **não prova** que o mesmo nível de detalhe seja executável em tempo real dentro do Godot.

Referências:

- Um estudo de 2024 usando MATSim modelou uma amostra de mais de **1 milhão de indivíduos** com trajetórias explícitas para a região de Los Angeles.
- Outro estudo usando MATSim simulou dezenas de milhares de agentes com atividades e viagens individuais.
- Essas ferramentas são especializadas em transporte e não carregam, para cada pessoa, toda a combinação pretendida pelo IndexCities: família, dinheiro, emprego, necessidades, relacionamentos, envelhecimento, renderização, animação, IA local e outros sistemas.

Para Godot, a documentação oficial estabelece limites arquiteturais relevantes:

- a engine oferece multithreading, mas **nem toda a API é thread-safe**;
- interagir com a SceneTree ativa a partir de threads não é seguro;
- os Servers do Godot são indicados para controlar grandes quantidades de instâncias, e a própria documentação cita **dezenas de milhares** de instâncias como um cenário apropriado para uso direto dos Servers em vez de Nodes na SceneTree.

Isso favorece uma hipótese arquitetural, ainda não decidida: **estado de simulação em estruturas C# leves e independentes da SceneTree, com representação visual no Godot somente quando necessária**. O limite real deve ser medido no hardware-alvo.

**Conclusão da pesquisa:** hoje não existe evidência suficiente para escolher honestamente um limite de 10 mil, 50 mil, 100 mil ou 1 milhão de cidadãos para o IndexCities. O número precisa sair de benchmarks do próprio modelo de simulação. Esses benchmarks podem ser pontuais e técnicos: **não são POCs por subsistema nem substituem a validação global da primeira entrega integrada**.

**Benchmark necessário antes da decisão:**
1. cidadão apenas com estado demográfico;
2. cidadão + família + residência + emprego;
3. cidadão + agenda e deslocamento;
4. veículos e pathfinding;
5. economia e necessidades;
6. comparar 10k, 25k, 50k, 100k e faixas maiores até localizar os gargalos;
7. medir separadamente CPU, memória, tempo de tick e custo de apresentação no Godot.

Fontes:
- Godot, Thread-safe APIs: https://docs.godotengine.org/en/4.5/tutorials/performance/thread_safe_apis.html
- MATSim / Los Angeles, 2024: https://www.sciencedirect.com/science/article/pii/S1877050924013218
- MATSim / Amsterdam, 2016: https://www.sciencedirect.com/science/article/pii/S1877050916310134

### 3. Granularidade física dos edifícios

A direção discutida é usar **casas e prédios como unidades físicas**, sem simular cômodos individualmente.

Uma casa pode conter uma família ou residência; um prédio pode conter múltiplas residências/famílias. A modelagem exata de unidades internas ainda não está fechada.

### 4. Escala de tempo e velocidades

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Em 2026-10-08 o responsável **APROVOU a aceleração real de toda a simulação**, registrada na SPEC: quando o tempo avança mais rápido, os acontecimentos de SIMs, transporte, produção, estoques e economia também acontecem de verdade, sem salto de data nem resultados inventados. **Duração do calendário, velocidades adicionais e estratégia concreta de performance continuam NÃO DECIDIDAS.** As comparações e técnicas abaixo são pesquisa da IA **PENDENTE de revisão humana**.

**Questão P0:** conciliar minutos de deslocamento, turnos diários, compras e alimentação, obras, salários e aluguéis mensais, reajustes anuais, infância, educação, reprodução e envelhecimento **sem que as diferentes escalas deixem a cidade artificial ou tornem o jogo lento**. Números de outras simulações não são parâmetros do IndexCities.

#### Direção de produto aprovada: acelerar o mundo real da simulação

**Fonte de verdade: [SPEC](../SPEC.md#tempo-de-jogo).** Aumentar a velocidade deve fazer o **mesmo mundo efetivamente avançar**, preservando identidades individuais, viagens/cargas físicas, trabalho, empresas, produção, necessidades, saldos, transações, dívidas e envelhecimento. Não basta alterar números do calendário, ocultar operações ou trocar resultados por estimativas. **Processar eventos de maneira mais eficiente não significa perder acontecimentos**: intervalos estáveis podem ser calculados sem atualizar cada frame se o estado e as consequências permanecerem equivalentes.

**O que NÃO foi decidido:** duração do dia/mês/ano, limites numéricos de aceleração, qualquer velocidade acima das 1/2/3 já previstas, modelo específico de atualização, garantias de desempenho ou hardware-alvo. Estratégias de operação, calendário compacto/representativo, relógios separados, replay e projeções são **histórico de pesquisa**, não escolhas atuais. Modelos que substituem eventos por dias/produção/salários fictícios conflitam com o princípio agora aprovado. A prioridade da investigação muda para **capacidade de executar a simulação causal mais rapidamente**, sem abandonar a legibilidade do jogador.

#### Como buscar performance excepcional — hipóteses técnicas, NÃO arquitetura aprovada

> **Revisão humana desta subseção: PENDENTE.** Síntese e ordem de investigação propostas pela IA após o responsável decidir **o resultado desejado**, mas **sem aprovar a implementação**. A ARCHITECTURE já prevê scheduler, simulação independente do render, headless e medições; não criar novas camadas por antecipação.

| Estratégia candidata | Ideia simples | Maior cuidado |
| --- | --- | --- |
| **Atualização por acontecimentos** | Quem está dormindo, viajando num trecho previsível ou aguardando entrega não precisa recalcular tudo a cada frame; agendar próxima mudança útil | Invalidar previsões quando houver congestionamento, interrupção, cancelamento ou novo comando |
| **Dependências e propagação localizada** | Quando faltar energia, reavaliar consumidores afetados, não toda a cidade | Índices corretos, reconstruíveis e sem deixar agentes em estado desatualizado |
| **Dados compactos e amigáveis à memória** | Processar grandes conjuntos de estados mínimos em estruturas eficientes, mantendo SIMs/empresas individuais | Não sacrificar identidade nem introduzir duplicação inconsistente de estado |
| **Paralelismo controlado** | Preparar trabalho independente em paralelo e aplicar efeitos compartilhados em ordem definida | Duas famílias não podem comprar o último produto ao mesmo tempo; sincronização e memória podem custar mais que o ganho |
| **Roteamento e apresentação separados** | Reutilizar informação de redes/rotas quando válido; renderizar posições intermediárias sem decidir economia no Godot | Alterações de ruas e tráfego devem atualizar o resultado real; desenhar mais rápido não acelera simulação |

**Evidências externas úteis:** [Factorio — atualizações inativas e robôs](https://www.factorio.com/blog/post/fff-421), [Factorio — otimização](https://www.factorio.com/blog/post/fff-148), [A/B Street — eventos de trânsito](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html) e [Microsoft .NET — SIMD](https://learn.microsoft.com/en-us/dotnet/standard/simd). Não transportar ganhos publicados de outros jogos como estimativas para IndexCities. Mais threads podem piorar o custo de memória/sincronização; **a primeira pergunta é como realizar menos trabalho por evento verdadeiro**.

**Investigação prioritária na próxima sessão:** comparar execução simples e otimizada em cenários reproduzíveis com população crescente; medir tempo **simulado/segundo real**, eventos/segundo, custo de tráfego e pathfinding, tempo por sistema, memória, GC e picos. Verificar **os mesmos resultados causais em checkpoints comparáveis**, conservação de dinheiro, estoque, entregas, empregos e envelhecimento nas velocidades aprovadas. Não prometer 100x, anos em segundos nem criar POC setorial obrigatória. Identificar gargalos reais e então escolher a técnica mais simples que os resolve. No ritmo muito acelerado, a **legibilidade visual** continua desafio distinto da correção da simulação.

#### Modelos de calendário pesquisados antes da decisão de aceleração real (histórico; nenhum escolhido)

| Alternativa | Como funcionaria | Benefício | Custo / perigo |
| --- | --- | --- | --- |
| **A — calendário único condensado** | Relógio, rotinas, finanças e ciclo de vida avançam na mesma escala lógica, acelerada em relação ao mundo real | Sequência causal e datas intuitivas; menos exceções | Para envelhecer gerações em tempo agradável, pode comprimir trabalho, viagens e vencimentos até perder significado |
| **B — dia operacional representativo** | Dia/noite e rotinas físicas representam um intervalo maior de calendário, por exemplo um mês | Permite observar a cidade amadurecer em menos sessões | A equivalência entre compras, salários, alimentação, idades e datas torna-se menos literal; exige convenções explícitas |
| **C — calendário demográfico/histórico separado do ritmo econômico/operacional** | Progressão das idades e marcos do calendário pode avançar de modo diferente das operações comerciais/viárias | Ajuste mais independente do ritmo das gerações e da economia | Datas, periodicidade de aluguéis/salários, educação, gravidez, estoques, vencimentos, estatísticas, replays e saves podem divergir; obriga mapear eventos entre escalas |

**Evidência de desenvolvedores:**
- [OpenTTD 14.0 — artigo técnico “The stoppable march of time”](https://www.openttd.org/news/2024/03/23/timekeeping) e [notas da versão](https://www.openttd.org/news/2024/04/13/openttd-14-0): separou **economy time** e **calendar time** para permitir desacelerar evolução histórica sem desacelerar transporte e produção; a equipe descreve dificuldades com estatísticas, datas, interface e compatibilidade. **Importante:** o jogo não modela a combinação completa de família, aluguel, escola e patrimônio do IndexCities; a solução não pode ser copiada diretamente.
- [Cities: Skylines II — Climate & Seasons, desenvolvedores](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/climate-seasons): um ciclo de dia/noite equivale a um mês e um ano tem 12 ciclos. Demonstra a opção B; **não prova** adequação à economia física dos nossos SIMs.
- [Banished — mod Faster Aging](https://banishedinfo.com/mods/view/23-Faster-Aging) e [Banished Ventures — Speed Aging](https://www.banishedventures.com/mods/speed-aging/): mudanças comunitárias na taxa de envelhecimento indicam que a **reposição de gerações** é variável importante e discutida; mods são testemunhos de preferências, não estudo controlado de gameplay.
- [Sapiens — tempo](https://wiki.playsapiens.com/index.php/Time): ano comprimido em poucos dias; referência de solução possível, não baseline.
- [A/B Street — simulação por eventos](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html): indivíduos fisicamente presentes podem ter estado persistente e atualizar-se por eventos relevantes, sem processar todos a cada frame. **Frequência de atualização é um problema técnico distinto da semântica do calendário**. Não impor scheduler de eventos global sem medições.

#### Relações causais que não podem se perder na avaliação

- Nenhum salário, aluguel, juro, imposto, benefício, conta ou dívida surge porque *somente a idade/calendário visual* acelerou sem evento econômico correspondente; o recebedor/pagador continua agente real da SPEC.
- Compra de comida, consumo do estoque doméstico, reposição por carga física e preço têm de permanecer coerentes com horários de mercado, viagens e dinheiro — mesmo se “um dia” representar período maior.
- SIMs não devem viver ciclos de gravidez, escola, trabalho e envelhecimento incompatíveis entre si; um modelo separado precisaria regras explícitas sobre **qual escala governa cada duração**, sem duplicar pagamentos.
- Obra, deslocamento e trabalho não podem parecer teletransportados só para compensar a compressão do calendário; nem devem exigir longas esperas artificiais.
- Mudanças de velocidade, pause, save/reload e replay não devem alterar sequência econômica, idade nem resultados aleatórios por mero artefato do relógio.
- Tempo simulado, duração de uma obra e **tempo real percebido pelo jogador** são grandezas distintas. Um modelo formalmente consistente pode ser lento ou frustrante.

#### Comparação recomendada — não é nova POC ou decisão de arquitetura

**Recomendação histórica anterior à decisão de aceleração real:** comparar A e B primeiro, deixando C como alternativa. **Não é mais a prioridade da pesquisa atual**; o requisito aprovado é executar o mundo realmente acelerado. A seleção de calendário permanece aberta. Avaliar no **jogo integrado definido pela SPEC**, com parâmetros ajustáveis e cenários reproduzíveis: (1) casa → trabalho → compra → casa, incluindo pico de tráfego; (2) mês de salários, aluguéis, consumo e caixa empresarial; (3) construção e entrega sem demoras artificiais; (4) formação e evolução de famílias durante vários anos; (5) alternância pause/1/2/3, save e replay.

Medir **legibilidade de viagens, taxas de transações/consumo, tempo de espera percebido, evolução das gerações, conservação monetária, pontualidade dos eventos e custo computacional**. Não fixar agora “12 minutos por dia”, “30 dias por ano”, multiplicadores exatos ou fórmulas de aceleração etária: esses eram **exemplos hipotéticos**, não recomendação final nem decisão aprovada.

**Nota histórica (PENDENTE):** antes da aprovação, a IA recomendava apenas preservar causalidade e comparar calendários. **Agora a aceleração real dos acontecimentos está decidida na SPEC; a escala exata do calendário e o desempenho alcançável continuam por medir/decidir.**

#### Hipótese D — calendário convencional e avanço temporal estratégico (2026-10-08)

> **Revisão humana desta subseção: PENDENTE — ideia documentada a pedido do responsável, mas expressamente NÃO DECIDIDA e sujeita a novas rodadas de pesquisa.** Não converter esta proposta em requisito, plano de implementação, velocidade extra, nova modalidade de jogo ou preferência definitiva.

**Motivação:** o responsável expressou preocupação com a estranheza de um calendário ficcional de poucos dias por mês. A reflexão seguinte questionou uma premissa anterior: **a SPEC determina nascimentos, envelhecimento e ciclo de vida, mas não exige que o jogador observe uma geração inteira em algumas dezenas de horas**. Acelerar toda a economia para alcançar esse objetivo talvez seja otimizar a variável errada.

**Hipótese combinada (D), composta por possibilidades independentes e ainda não aprovadas:**

1. **Calendário familiar/convencional:** estudar dias de 24 horas, meses e anos reconhecíveis (potencialmente próximos ao calendário terrestre), com um relógio lógico causal para acontecimentos operacionais, econômicos e demográficos. **Não** é decisão de usar 365 dias por ano nem duração fixa de dia real; o custo de um ano longo permanece central.
2. **Migração com idades variadas:** estudar entradas voluntárias de famílias com crianças, adultos em idades distintas e idosos, para criar estrutura etária e necessidades variadas **sem acelerar artificialmente o crescimento de uma criança nascida na cidade**. A **migração de agentes reais** já é aprovada na SPEC; **a distribuição etária de candidatos, a composição concreta das coortes e qualquer política para favorecer diversidade ainda NÃO foram aprovadas**. Continuam obrigatórios oportunidade plausível, vontade do agente, moradia/condições e capital real transferido da Reserva Global; não gerar moradores fictícios nem garantir imigração.
3. **Avanço temporal estratégico opcional:** explorar avançar a simulação até uma data ou condição observável enquanto o jogador não precisa assistir a cada frame, como alternativa ao acompanhamento normal. **Não é salto de data**: ações de SIMs, deslocamentos e cargas, consumo, produção, pagamentos, restrições, falhas, contratação e decisões autônomas devem continuar causalmente consistentes, com estados reais e sem teletransporte. Uma eventual interrupção em acontecimento importante precisaria critérios e frequência cuidadosos para **não transformar o jogo em alarmes ou cliques repetitivos**. A interface, ações "próximo mês/fim da obra" e modo adicional **não são aprovados**.
4. **Histórias de curto/médio prazo:** investigar se mudança de emprego, moradia, família, falência de empresas, serviços e bairros já fornecem histórias emergentes em meses/anos, **sem obrigar que nascimento → vida adulta → velhice ocorra durante uma sessão**. Não inventar missões nem acontecimentos roteirizados só para entreter.

**O que esta hipótese realmente resolve — e o que NÃO resolve:**

| Questão | Potencial | Limite não resolvido |
| --- | --- | --- |
| Naturalidade de datas | Dias, meses, vencimentos e aniversários familiares são mais intuitivos | Tempo real do jogador por mês/ano pode continuar excessivo |
| Sensação de cidade viva | Agentes migrantes em idades variadas e trajetórias pessoais produzem acontecimentos desde cedo | Não acelera a maturação real dos recém-nascidos, nem garante chegada de novos moradores |
| Espera percebida | Jogador poderia não assistir a todos os movimentos durante avanços longos | A simulação integrada continua fazendo trabalho real; **anos em segundos são hipótese sem benchmark**, não promessa |
| Coerência monetária e logística | Em princípio, preserva as mesmas transações e relações causais em ambos os modos | Otimizar viagens, estoques e operações não pode fabricar transações ou suprimir gargalos |
| Desempenho e save | Arquitetura já separa tick/renderização, permite scheduler e headless | Isso **não** prova avanço arbitrariamente rápido; eventos de muitos SIMs/veículos podem limitar muito a aceleração |

**Compatibilidade com autoridade existente:** a SPEC só aprova **pause e velocidades 1/2/3**, com parâmetros configuráveis; o avanço estratégico seria **funcionalidade adicional** e exigiria aprovação antes de implementação. A ARCHITECTURE já prevê simulação desacoplada do render, scheduler e headless — **capacidade técnica, não autorização de gameplay nem garantia de performance**. A SPEC ainda determina aluguéis mensais, reajustes anuais, dívidas com prazos, trabalho presencial, estoque doméstico, produção, salários e oferta monetária fixa. D deve preservar essas invariantes.

**Relação com propostas anteriores (histórico):** A, B e C eram modelos de calendário pesquisados; D trouxe calendário convencional, migração etária e avanço estratégico. **A escolha posterior do responsável agora exige tempo acelerado com todos os acontecimentos reais**. Qualquer variante que substitua jornadas, deslocamentos, compras ou produção por dias fictícios fica incompatível com esse princípio. Os parâmetros do calendário ainda estão em aberto; D não foi aprovado integralmente, incluindo modo adicional de avanço e migração etária.

**Próximas rodadas de pesquisa — questões que realmente discriminam propostas:**
- **Objetivo de tempo percebido:** para o jogador, o essencial é acompanhar **rotinas diárias**, a evolução da **cidade em anos**, ou uma **vida individual completa**? Precisamos de experiências de referência e cenários comparáveis antes de estipular duração.
- **Viabilidade real do avanço:** quantos eventos, deslocamentos, transações, pathfindings, alterações de estoque e mudanças de emprego ocorrem num mês com população crescente? Qual aceleração sustentável um benchmark headless integrado demonstra? Onde aparecem backlog, quedas de desempenho e perda de legibilidade?
- **Preservação da história causal:** avançar um mês com e sem renderização/visualização gera resultados compatíveis para dinheiro, mercadorias, vidas dos SIMs e saves? Se usar agrupamento fora da câmera, o que continua fisicamente rastreável?
- **Risco de interação:** quando parar automaticamente, quando apenas registrar um resumo, e como evitar interromper constantemente o jogador? Avanço entre dois eventos pode criar um ciclo de espera/clicks mais chato que x3.
- **Idade e coortes:** a variedade de idades dos migrantes ajuda na experiência ou introduz ondas de mortes/aposentadoria, necessidade instantânea de serviços e desequilíbrio de capacidade?
- **Alternativas adicionais:** aceleração adaptativa baseada no que precisa ser processado; avanço por evento em vez de data; apresentar **histórico/painéis da cidade** sem acelerar o mundo; agregação *somente* para cálculo de estados comprovadamente equivalentes (não agentes fictícios); manter tempo normal e aceitar que uma geração leve várias sessões. Todas exigem confronto com gameplay, causalidade e custo antes de recomendação.

**Status para futuras sessões:** hipótese D continua **PENDENTE de revisão humana** como pacote; **só o princípio de aceleração real da simulação foi aprovado depois, na SPEC**. Novas sessões devem priorizar **engenharia de desempenho e correção causal**, sem voltar a debater modelos de tempo substitutivos, sem prometer aceleração extrema e sem implementar funcionalidades não aprovadas.

#### Rodada complementar — execução por eventos, ritmo contextual, lupa temporal e cenários futuros (2026-10-08)

> **Revisão humana desta subseção: PENDENTE — pesquisa e recomendações da IA registradas a pedido do responsável, SEM decisão de produto.** O usuário solicitou mais alternativas fora da caixa e depois perguntou qual era a favorita, **não aprovou** nenhuma. Não modificar a SPEC, a arquitetura ou o escopo por inferência.

**Descoberta central:** a conversa misturava **(i) semântica do calendário**, **(ii) trabalho computacional para avançar a cidade** e **(iii) capacidade do jogador de acompanhar acontecimentos**. A, B, C e D discutem principalmente o primeiro tema e combinações de ritmo; as quatro ideias a seguir são **estratégias complementares**, **não quatro modelos novos de calendário**. Podem ser avaliadas independentemente e não resolvem sozinhas a duração dos anos.

| Ideia de pesquisa | Problema atacado | Benefício possível | Risco/custo e autoridade |
| --- | --- | --- | --- |
| **E — intervalos/eventos relevantes; cálculo sob demanda** | SIMs, viagens, veículos, estoques e empresas não precisam necessariamente recalcular seu estado a cada frame | Menos atualizações desnecessárias; estado/posição de um trajeto previsível podem ser derivados de intervalo conhecido até um evento que o altere | Congestionamento novo, emergência, mudança de rota, estoque vencido ou falta de pagamento têm de invalidar previsões corretamente; custos de filas e replanejamento podem anular ganho. **A ARCHITECTURE já aprova scheduler, atualizações por ritmos e tick separado do render; E é aprofundamento/hipótese de otimização, NÃO nova funcionalidade automaticamente aprovada.** |
| **F — ritmo de observação contextual automático** | Trocar manualmente velocidade repetidamente e esperar períodos sem decisões | Considerar alternância assistida entre velocidades **1/2/3 já previstas na SPEC**, respeitando escolha do jogador, sem pressupor saltos | Cidade nunca fica inteiramente ociosa (turnos noturnos, caminhões, saúde, emergências); critérios para desacelerar podem gerar interrupções e comportamento imprevisível. **Mudar velocidade automaticamente é novo comportamento de UI/gameplay NÃO aprovado**, mesmo sem criar 100x. |
| **G — lupa temporal / histórico consultável de SIMs** | Jogador perde trajetos e acontecimentos importantes quando observa outra parte da cidade | Consultar, depois, eventos pessoais reais (mudança de emprego, compra, visita, mudança de casa), talvez trechos de deslocamento com dados suficientes; favorece histórias sem obrigar simulação inteira a rodar devagar | Histórico por SIM exige limites de armazenamento, retenção, identidade e diagnóstico; **replay visual exato não pode ser prometido sem registrar estado suficiente**; evento resumido NÃO equivale a reconstituição fiel. **Nova funcionalidade NÃO aprovada**; preservar registros existentes somente quando de fato úteis. |
| **H — projeção de cenários alternativos** | Desejo de avaliar impacto de decisões em horizontes longos sem esperar anos na cidade principal | Exibir cenários hipotéticos condicionais, distinguindo simulação corrente de estimativas (ex.: emprego/fluxos após nova indústria) | Previsões podem sugerir lucro/causalidade garantidos; ramificação de mundo ou segundo modelo agregado pesa em performance, regras, manutenção, memória e microgerenciamento. **Nova funcionalidade NÃO aprovada; baixa prioridade no estágio atual.** |

**Fontes e limites concretos:**
- [A/B Street — discrete event traffic simulation](https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html): o desenvolvedor descreve pedestres/veículos que agendam transições futuras e posições interpoladas; também explica por que congestionamento e mudanças de faixa tornam o método complexo. Evidência técnica para **E no domínio de mobilidade**, não prova de que todas as decisões econômicas do IndexCities possam ser simplificadas do mesmo modo.
- [Kerbal Space Program / kOS — time warp](https://ksp-kos.github.io/KOS_DOC/structures/misc/timewarp.html): distingue *physics warp* de *rails warp*, que calcula movimento orbital sob hipóteses simplificadas **sem executar toda a física**. Inspira a distinção entre modos de processamento, mas **não serve como autorização para suprimir viagens, comércio ou produção reais**.
- [Stardew Valley — day cycle](https://wiki.stardewvalley.net/Day_Cycle): o jogador pode dormir e encerrar cedo o seu dia. É analogia de **redução de espera**, mas um jogo de um protagonista não equivale a uma cidade inteira ativa, nem comprova que F seja desejável.
- [Factorio — replay system, documentação oficial](https://wiki.factorio.com/Replay_system): replays com velocidade ajustável dependem de gravação e têm restrições de versões/mods, navegação e reprodução retroativa. Apoia estudar **G**, inclusive os custos; um replay completo da cidade não é requisito.
- [UrbanSim — cenários](https://cloud.urbansim.com/docs/block-model/documentation/scenarios.html) e [uso de cenários e projeções](https://cloud.urbansim.com/docs/general/documentation/urbansim.html): compara **futuros condicionados a premissas**, não previsões garantidas. Ferramenta de planejamento especializada, distante do escopo inicial de um city builder com agentes individuais; **H é hipótese de pesquisa futura**.

**Preferência histórica da IA (NÃO aprovada, sujeita a novas rodadas): E como direção técnica de investigação, combinada com G em versão modesta de histórico baseado em eventos, não replay visual universal.** Priorizar E porque se alinha ao scheduler e à persistência causal já previstos, podendo beneficiar desempenho sem alterar datas ou fatos do mundo. Investigar G somente se a observação de histórias individuais comprovar valor real; começar por eventos relevantes, não gravar continuamente todos os movimentos. **D (calendário convencional + migração etária variada e possibilidade de avanço estratégico)** é contexto interessante para E/G, mas **nem calendário convencional nem avanço rápido estão escolhidos**. F fica como pesquisa de experiência; H como hipótese mais distante. A/B/C e calendário compacto continuam opções abertas. A preferência da IA não implica decisão do responsável nem altera prioridade de implementação.

**Critérios de confronto com gameplay, causalidade, micro e performance:**
- **Mesma cidade, dois modos de execução:** comparar pequenos passos versus agendas por evento em situações de tráfego com mudanças dinâmicas, estoque esgotando, entrega e pagamentos. Invariantes: dinheiro global, quantidade/posição física de cargas, elegibilidade de serviço, pontualidade e identidade de SIMs. Não aceitar um algoritmo somente por FPS.
- **Custo real de acelerar:** medir eventos, invalidações, replanejamentos, atrasos de scheduler, pathfinding, alocações e latência do comando em população crescente. Não prometer anos em segundos, nem modo 100x.
- **Histórico realmente útil:** acompanhar um SIM enquanto o jogador constrói outra região; comparar informações agregadas existentes, eventos significativos e hipotético replay visual. Medir legibilidade, armazenamento, quantidade de notificações e custo de instrumentação; não confundir *gravar evento* com *reverter o mundo*.
- **Interrupções e autonomia:** ritmo contextual não deve assumir que “madrugada é vazia”; possíveis desacelerações precisam respeitar escolhas do jogador e não virar burocracia invisível.
- **Previsão versus verdade:** toda projeção hipotética, caso um dia aprovada, precisa declarar premissas e incerteza e **não** substituir transações concretas, produção física ou decisões autônomas por números mágicos.

**O que permanece realmente em aberto:** calendário e duração de dia/mês/ano; objetivo de acompanhar vida inteira versus evolução de cidade; se avanço estratégico acrescenta diversão; custo medido de eventos sob congestionamento; se histórico de SIMs merece sistema próprio; valor efetivo de ritmo contextual e projeções. **Não iniciar implementação ou criar subsistemas extras com base nesta pesquisa.** A validação pode ocorrer nos cenários do jogo integrado definidos na SPEC e na ARCHITECTURE, sem POCs independentes obrigatórias.

### Regra de evidência para decisões futuras

Para os assuntos desta seção:

- dados reais servem para determinar ordens de grandeza e relações;
- benchmarks do próprio IndexCities determinam limites técnicos;
- referências de outros jogos servem como comparação, não como prova;
- nenhum número vira requisito apenas porque parece razoável.


---

---

## Baselines empíricos provisórios

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status:** referência de calibração, não decisão de produto.

Para evitar que a simulação fique sem parâmetros enquanto decisões finais ainda estão abertas, o projeto pode usar **baselines empíricos provisórios** derivados de dados reais. Esses valores devem permanecer configuráveis e ser substituídos quando estudos mais específicos ou benchmarks do próprio IndexCities justificarem.

### Referência geográfica inicial

Os primeiros baselines abaixo usam **Brasil** por disponibilidade e qualidade de dados oficiais. Isso **não define** que o mundo do IndexCities será brasileiro. O recorte geográfico/estético do jogo continua aberto.

### Domicílios

O Censo 2022 registrou média de **2,79 moradores por domicílio particular permanente ocupado** no Brasil.

Isso equivale aproximadamente a:

- **358 domicílios para cada 1.000 habitantes**;
- 10 mil habitantes → ~3.584 domicílios;
- 20 mil habitantes → ~7.168 domicílios;
- 30 mil habitantes → ~10.753 domicílios;
- 50 mil habitantes → ~17.921 domicílios.

Fonte: IBGE, Censo Demográfico 2022:
https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102011.pdf

### Posse de automóvel e motocicleta por domicílio

Na PNAD Contínua 2023:

- **48,1%** dos domicílios possuíam automóvel;
- **24,6%** possuíam motocicleta;
- **12,6%** possuíam ambos.

Logo, usando a média nacional apenas como baseline:

Para cada **1.000 habitantes** (~358 domicílios):

- ~172 domicílios têm automóvel;
- ~88 têm motocicleta;
- ~45 têm ambos;
- ~215 têm pelo menos automóvel ou motocicleta;
- ~143 não têm nenhum dos dois.

Exemplo para uma cidade de **30 mil habitantes** (~10.753 domicílios):

- ~5.172 domicílios com automóvel;
- ~2.645 com motocicleta;
- ~1.355 com ambos;
- ~6.462 com ao menos um dos dois;
- ~4.291 sem automóvel nem motocicleta.

Esses números representam **domicílios que possuem o bem**, não quantidade total de veículos. Um domicílio pode possuir mais de um carro ou moto.

Há grande variação regional: em 2023, por exemplo, a posse de automóvel por domicílio variava de **27,6% no Nordeste** a **67,5% no Sul**. Portanto, posse de veículos deve ser tratada como parâmetro socioeconômico/geográfico, não constante universal.

Fonte: IBGE, PNAD Contínua — Características gerais dos domicílios e moradores 2023:
https://www.ibge.gov.br/biblioteca/visualizacao/livros/liv102158_informativo.pdf

### Frota registrada: referência administrativa

A Senatran registrou **129,1 milhões de veículos** no Brasil em dezembro de 2025, incluindo cerca de **64,6 milhões de automóveis**.

Com a população estimada pelo IBGE em 2025 de **213,4 milhões de habitantes**, isso equivale aproximadamente a:

- **303 automóveis registrados por 1.000 habitantes**;
- **605 veículos registrados de todos os tipos por 1.000 habitantes**.

Em uma cidade hipotética de 30 mil habitantes, aplicar diretamente essas razões daria cerca de **9,1 mil automóveis registrados** e **18,1 mil veículos totais**.

**Importante:** isso não deve ser usado como quantidade de veículos ativos na simulação. O cadastro da Senatran inclui veículos registrados que podem não estar efetivamente em circulação e também veículos de empresas, carga, reboques e outras categorias. Esse número funciona como teto/referência administrativa e deve ser confrontado com posse domiciliar e uso real.

Fontes:
- Senatran, Frota de Veículos 2025:
https://www.gov.br/transportes/pt-br/assuntos/transito/conteudo-Senatran/frota-de-veiculos-2025
- IBGE, Estimativas da População 2025:
https://ftp.ibge.gov.br/Estimativas_de_Populacao/Estimativas_2025/POP2025_20260113.pdf

### Uso real no deslocamento ao trabalho

O Censo 2022 mostrou, entre trabalhadores que se deslocam:

- **32,3%** usam automóvel como principal meio;
- **21,4%** usam ônibus;
- **17,8%** vão a pé;
- **16,4%** usam motocicleta;
- **6,2%** usam bicicleta;
- os demais modos têm participações menores.

Também há forte diferença regional: o automóvel chega a **45,9%** no Sul, enquanto motocicletas têm peso muito maior no Norte e Nordeste.

Para o IndexCities, isso sugere separar três conceitos:

1. **posse** de veículo;
2. **disponibilidade** do veículo naquele momento;
3. **escolha modal** para cada viagem.

Ter carro não implica usá-lo em toda viagem.

Fonte: IBGE, Censo 2022 — Deslocamentos para trabalho:
https://educa.ibge.gov.br/jovens/materias-especiais/23064-censo-2022-como-a-populacao-se-desloca-para-estudar-e-trabalhar.html

### Consequência para o modelo de veículos

Baseline de exploração:

- veículos devem existir como patrimônio de pessoas/famílias/empresas, e não ser criados apenas quando uma animação precisa aparecer;
- quantidade de veículos possuídos e quantidade de veículos simultaneamente nas ruas são coisas diferentes;
- geração de tráfego deve depender de agenda, destino, posse, disponibilidade, custo e escolha modal;
- parâmetros de posse e uso devem ser configuráveis para representar cidades com perfis socioeconômicos diferentes;
- o jogo não deve tentar manter toda a frota visível/movendo ao mesmo tempo apenas porque ela existe no estado da cidade.

### Próximas médias a investigar

A mesma metodologia deve ser aplicada progressivamente a:

- composição etária;
- tamanho e tipos de família;
- população economicamente ativa e empregos;
- distribuição de empregos por comércio, indústria e serviços;
- quantidade e porte de escolas;
- saúde;
- polícia e bombeiros;
- estabelecimentos comerciais;
- taxas de nascimento, casamento e mortalidade;
- quantidade de viagens por pessoa/dia;
- distâncias e tempos de deslocamento;
- taxa de ocupação dos veículos;
- transporte público;
- consumo e renda.

Sempre que possível, usar uma combinação de dados nacionais + amostra de municípios pequenos reais, em vez de uma única média nacional.


---
