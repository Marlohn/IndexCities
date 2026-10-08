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

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** A SPEC aprova **pause, velocidades 1, 2 e 3 e escala/multiplicadores configuráveis para calibração**. Em **2026-10-08 o responsável adiou explicitamente a escolha de modelo temporal porque realiza pesquisa independente**: **A, B e C continuam não escolhidos**. A pesquisa comparativa abaixo permanece **PENDENTE de revisão humana** e não autoriza escolher ou implementar modelo temporal por suposição.

**Questão P0:** conciliar minutos de deslocamento, turnos diários, compras e alimentação, obras, salários e aluguéis mensais, reajustes anuais, infância, educação, reprodução e envelhecimento **sem que as diferentes escalas deixem a cidade artificial ou tornem o jogo lento**. Números de outras simulações não são parâmetros do IndexCities.

#### Três modelos de calendário a comparar (nenhum escolhido)

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

Comparar **A e B primeiro**, guardando **C como alternativa séria caso ambas falhem**. Isso reduz complexidade de implementação sem excluir uma solução comprovadamente viável em outros domínios. Avaliar no **jogo integrado definido pela SPEC**, com parâmetros ajustáveis e cenários reproduzíveis: (1) casa → trabalho → compra → casa, incluindo pico de tráfego; (2) mês de salários, aluguéis, consumo e caixa empresarial; (3) construção e entrega sem demoras artificiais; (4) formação e evolução de famílias durante vários anos; (5) alternância pause/1/2/3, save e replay.

Medir **legibilidade de viagens, taxas de transações/consumo, tempo de espera percebido, evolução das gerações, conservação monetária, pontualidade dos eventos e custo computacional**. Não fixar agora “12 minutos por dia”, “30 dias por ano”, multiplicadores exatos ou fórmulas de aceleração etária: esses eram **exemplos hipotéticos**, não recomendação final nem decisão aprovada.

**Recomendação histórica da IA (PENDENTE; escolha adiada pela pesquisa independente do responsável):** uma linha temporal de eventos causalmente consistente é desejável; **isso não obriga que calendário, visualização e envelhecimento compartilhem a mesma razão de aceleração**. Manter o modelo final aberto até observação conjunta de gameplay e economia. **Confiança alta** na necessidade de calibração e invariantes; **moderada/baixa** na escolha de um modelo vencedor sem medição.

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
