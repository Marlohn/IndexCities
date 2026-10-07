# IndexCities — EXPLORATION

> Este arquivo é o espaço para **pesquisa, ideias e decisões ainda em formação**.
>
> Ele não é a fonte de verdade do produto. Quando uma decisão fica fechada, o resultado oficial deve entrar de forma curta em [SPEC.md](SPEC.md).

## Como usar

Para cada assunto relevante:

1. registre a pergunta ou problema;
2. junte referências e evidências úteis;
3. compare alternativas;
4. anote riscos, dúvidas e trade-offs;
5. marque a conclusão quando houver;
6. se a conclusão mudar o produto, atualize a SPEC.

Não é necessário documentar cada conversa pequena. Use este arquivo quando preservar o raciocínio ajudar uma sessão ou uma IA futura.

---


## Referência comparativa do gênero

A pesquisa comparativa extensa sobre outros city builders, repositórios, post-mortems e comunidades foi consolidada em [GENRE_BENCHMARK.md](GENRE_BENCHMARK.md).

Esse documento deve ser revisitado em marcos relevantes do desenvolvimento para comparar o que está sendo criado com padrões, acertos, falhas e expectativas observados no gênero. O objetivo **não é copiar concorrentes nem transformar padrões externos em requisitos**, mas verificar se as escolhas do IndexCities continuam coerentes com a experiência desejada e se diferenças importantes são deliberadas.

Descobertas ainda abertas continuam sendo discutidas aqui na EXPLORATION; somente decisões fechadas entram na SPEC.

---

## Processo de desenvolvimento com IA

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

O IndexCities adota por enquanto uma abordagem de **“Go Horse com cerca”**:

- uma única SPEC curta e autoritativa;
- AGENTS.md com regras de trabalho;
- EXPLORATION para pesquisa e decisões ainda abertas;
- implementação rápida dentro desses limites;
- validação proporcional ao risco;
- nenhum framework adicional de SDD por enquanto.

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

## Ponto de partida do produto

**Status:** aberto.

O IndexCities parte de uma folha em branco. Nenhuma decisão de produto de trabalhos anteriores é herdada automaticamente.

Assuntos a explorar e decidir progressivamente podem incluir, entre outros:

- qual é a fantasia central do jogo;
- qual experiência o jogador deve ter;
- gênero e loop principal;
- plataforma e tecnologia;
- direção visual;
- escala;
- simulação;
- construção;
- economia;
- população;
- trânsito;
- progressão;
- interface;
- performance;
- persistência.

Essa lista não é roadmap nem compromisso. É apenas um mapa inicial de perguntas possíveis.

Material de projetos anteriores pode ser consultado no futuro como pesquisa, mas deve ser tratado como **evidência externa**, não como requisito.


---

## Escala da cidade, profundidade da simulação e tempo

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

**Conclusão da pesquisa:** hoje não existe evidência suficiente para escolher honestamente um limite de 10 mil, 50 mil, 100 mil ou 1 milhão de cidadãos para o IndexCities. O número precisa sair de benchmarks do próprio modelo de simulação.

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

Ainda não há base factual suficiente para fixar quanto tempo real corresponde a uma hora ou um dia do jogo.

O multiplicador de tempo interfere diretamente em:

- duração percebida de trajetos;
- jornada de trabalho e escola;
- congestionamento;
- consumo e produção;
- nascimento, envelhecimento e morte;
- ritmo de construção e economia;
- legibilidade das velocidades 1x, 2x e 3x.

Portanto, o valor deve começar **configurável** e ser calibrado a partir de uma simulação funcional.

**Experimento necessário:** implementar um relógio de simulação parametrizado e medir cenários representativos, como casa → trabalho → compras → casa, até encontrar uma escala em que deslocamentos, jornada diária e decisões do jogador continuem legíveis nas três velocidades. Só depois os multiplicadores devem virar decisão de produto.

### Regra de evidência para decisões futuras

Para os assuntos desta seção:

- dados reais servem para determinar ordens de grandeza e relações;
- benchmarks do próprio IndexCities determinam limites técnicos;
- referências de outros jogos servem como comparação, não como prova;
- nenhum número vira requisito apenas porque parece razoável.


---

## Direção atual do jogo

**Status:** em exploração, exceto onde a SPEC já registra uma decisão fechada.

### Referência de experiência

O ponto de referência declarado é **Cities: Skylines**, principalmente pelo estilo geral de construção e gestão de cidade. Isso é referência de comparação, não requisito de copiar sistemas ou arquitetura.

### Escala e densidade

A intenção atual é explorar uma cidade **menor em extensão e população que as grandes cidades típicas de Cities: Skylines**, mas com muito mais detalhe por lote, edifício, família, cidadão e veículo.

A escala populacional ainda não foi decidida. Ela deve ser derivada de duas frentes:

1. dados reais sobre o que caracteriza uma cidade pequena, mas funcionalmente completa;
2. benchmarks do próprio IndexCities para descobrir quanto detalhe individual cabe no orçamento de CPU, memória e tempo de frame.

### Simulação de cidadãos

A ambição de exploração é simular cidadãos com vida persistente e conectada à cidade, incluindo, quando viável:

- família e residência;
- estudo e trabalho;
- renda, consumo e participação na economia;
- necessidades e bem-estar;
- relacionamentos, casamento, filhos, envelhecimento e morte;
- propriedade e uso de veículos;
- deslocamentos que impactam o trânsito.

O objetivo é maximizar coerência sistêmica, mas a profundidade final só deve ser fechada depois de benchmarks e protótipos. Não há autorização para inventar limites de população ou cortar sistemas apenas por suposição.

### Trânsito e veículos

A direção desejada é que veículos pertençam de forma coerente a pessoas ou famílias e que os deslocamentos tenham causa observável. O trânsito deve emergir das rotinas e necessidades da população, evitando tráfego puramente decorativo.

O grau exato de fidelidade, pathfinding, estacionamento, posse de veículos, transporte público e regras viárias continua aberto para pesquisa e prototipagem.

### Organização técnica a explorar

Há preferência por separar claramente:

- núcleo de simulação e regras;
- integração com Godot;
- apresentação/renderização;
- UI;
- assets.

Também há interesse em uma arquitetura que permita agentes de IA trabalharem em partes isoladas do projeto com menor risco de alterar regras centrais sem intenção.

Isso ainda precisa ser transformado em arquitetura técnica concreta com base em protótipos, profiling e necessidades reais do código.

### Testes e guardrails

A direção é usar testes e outras proteções onde eles realmente defendam comportamento importante, especialmente regras da simulação e invariantes de sistemas interligados. A cobertura e a estratégia exatas ainda não estão decididas.


---

## Referência visual fornecida

**Status:** direção visual em exploração; a apresentação 3D isométrica já está confirmada na SPEC.

Foram fornecidas imagens de referência produzidas a partir de assets já existentes do projeto/autor. Elas devem ser consideradas nas futuras decisões de direção visual, escala de câmera, densidade urbana e legibilidade.

Características observáveis nas referências:

- visual 3D estilizado, com geometria simples e leitura limpa;
- câmera alta/isométrica capaz de mostrar simultaneamente ruas, calçadas, lotes e edifícios;
- edificações individuais claramente distinguíveis;
- escala urbana de bairro/cidade pequena, com casas, comércio de esquina, vegetação, mobiliário urbano e vias;
- prioridade para legibilidade visual em vez de realismo fotográfico;
- presença visível de detalhes urbanos como faixas de pedestre, postes, bancos, cercas, jardins e mesas externas;
- cidadãos e veículos aparecem em escala compatível com leitura individual.

As imagens são referência de exploração, não especificação dimensional. Medidas, grid, tamanho de lote, distância de câmera e densidade ainda precisam ser definidos e testados.

### Pipeline de assets

A intenção atual é produzir boa parte dos assets visuais com uma ferramenta/IA externa especializada e integrá-los ao jogo depois. O pipeline exato de formatos, importação, LODs, colisões, materiais e validação ainda não foi definido.

---

## Princípios desejados para a simulação

**Status:** direção de produto em exploração; profundidade e limites dependem de pesquisa e benchmark.

Além de "ter muitos agentes", o objetivo declarado é que os sistemas tenham **causa e consequência coerentes**.

Exemplos da direção desejada:

- um cidadão pertence a uma residência/família;
- cidadãos estudam e trabalham em locais reais da cidade;
- trabalho e atividade econômica geram ou movimentam recursos;
- cidadãos possuem renda/dinheiro e consomem;
- bem-estar/alegria deve refletir as condições de vida;
- relacionamentos e ciclo de vida podem incluir casamento, filhos, envelhecimento e morte;
- veículos devem estar ligados de forma coerente a pessoas ou famílias;
- viagens devem existir por algum motivo da simulação, não apenas como decoração;
- os sistemas econômicos, populacionais, de mobilidade e serviços devem influenciar uns aos outros.

O objetivo é chegar ao **máximo de profundidade viável**, sem fixar antecipadamente quais desses sistemas serão simulados em todos os ticks ou para toda a população. Estratégias como níveis de detalhe de simulação, atualização por frequência e agregação continuam abertas e devem ser avaliadas por benchmark.

---

## Escala física e forma de construir

**Status:** parcialmente decidido.

A granularidade de construção em casas e prédios já está decidida na SPEC.

Direções ainda em exploração:

- cidade com extensão controlada, menor que grandes mapas/metrópoles de city builders tradicionais;
- menos dependência de grandes áreas de zoneamento automático;
- maior importância para cada lote e edifício individual;
- quantidade de grids/células e tamanho total do mapa ainda não definidos;
- mesmo com área menor, a cidade deve poder atingir uma escala urbana significativa e oferecer os principais serviços e atividades de uma cidade funcional.

A quantidade final de população, lotes, casas, edifícios, comércio e indústria deve vir de dados reais e benchmarks, não de um número arbitrário.

---

## Objetivos de arquitetura de desenvolvimento

**Status:** direção arquitetural a validar.

A organização técnica deve buscar separação clara de responsabilidades para facilitar manutenção, testes e trabalho assistido por IA.

Áreas que se deseja manter desacopladas sempre que isso fizer sentido:

- núcleo da simulação e regras de domínio;
- integração com Godot;
- apresentação/renderização;
- UI;
- assets e conteúdo visual.

Como o desenvolvimento principal será em C#, a organização do código deve permanecer compreensível e convencional para desenvolvimento C#, evitando abstrações desnecessárias.

Também existe o objetivo de criar **guardrails técnicos** para permitir que modelos de IA mais baratos façam alterações locais com menor risco de regressão em gameplay. Esses guardrails devem surgir de necessidades concretas e podem incluir testes, limites de módulos, validações e convenções de código.

### Ambiente de desenvolvimento

O desenvolvimento está planejado para acontecer principalmente em máquina local. Fluxo de build, execução, profiling e automação local ainda precisam ser definidos quando houver código executável.


---

## Baselines empíricos provisórios

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

## Pesquisa comparativa de city builders

**Status:** em exploração.

Esta seção registra padrões recorrentes encontrados em post-mortems, entrevistas de desenvolvedores, devlogs e feedback de comunidades. Eles servem como evidência comparativa; não são requisitos automáticos do IndexCities.

### Padrões que se repetem

- **O loop principal precisa vencer as features secundárias.** Kingdoms Reborn testou combate RTS e descobriu que muitos jogadores paravam de construir a cidade para lidar com as batalhas. O sistema foi removido e depois reintroduzido de forma muito mais simples. Uma feature pode ser boa isoladamente e ainda enfraquecer o jogo inteiro.
- **Complexidade útil é a que produz decisões legíveis.** Against the Storm cortou e reformulou sistemas quando eles aumentavam complexidade sem melhorar a experiência. Sua evolução reforça a importância de testar cedo com jogadores reais.
- **Simulação individual compra vínculo, mas cobra CPU e design.** Tropico 6 implementou cidadãos autônomos porque a equipe considerava que agentes individuais aumentavam personalidade e apego. Ao mesmo tempo, pathfinding e decisão de agentes foram prototipados desde cedo por serem sistemas centrais e caros.
- **Pathfinding é risco estrutural, não detalhe de acabamento.** Em Banished, o pathfinding foi descrito pelo próprio desenvolvedor como um sistema corrigido continuamente; casos sem caminho podiam levar a buscas enormes e derrubar o frame rate quando muitos agentes falhavam simultaneamente.
- **Escala e arquitetura precisam ser pensadas juntas.** Citybound adotou arquitetura orientada a atores, mensagens e otimizações de localidade de memória justamente para perseguir simulação microscópica em larga escala. A ambição de simulação determinou a arquitetura, e não o contrário.
- **Automação é uma ferramenta de escala.** Songs of Syx busca populações enormes, mas explicitamente automatiza tarefas mundanas para que o jogador passe a decidir em nível cada vez mais alto conforme a cidade cresce.
- **Logística pode ser o coração do jogo.** Anno 1800 trata transporte e cadeias de produção como parte central da economia; a cidade cresce porque fluxos materiais funcionam, não apenas porque indicadores abstratos sobem.
- **Um sistema físico diferenciador pode carregar o jogo.** Timberborn investiu pesadamente em água e irrigação e usou um modelo híbrido de simulação para obter comportamento interessante sem exigir fidelidade física total. A lição é simular com precisão aquilo que produz gameplay e aproximar o restante.
- **Early Access melhora o jogo, mas reduz liberdade para mudanças radicais depois.** Kingdoms Reborn relata que feedback e receita foram extremamente úteis, mas que mudanças rápidas em sistemas centrais passaram a exigir mais cautela depois que jogadores criaram expectativas sobre o produto.
- **A comunidade não deve dirigir o produto por votação.** O mesmo relato de Kingdoms Reborn mostra preferências conflitantes e features refeitas várias vezes. Feedback é evidência sobre problemas e desejos; a solução ainda precisa preservar a identidade do jogo.
- **Limitações de construção estética importam para jogadores modernos.** Em 2026, Farthest Frontier decidiu substituir a limitação rígida de grid por posicionamento livre em 360 graus após feedback recorrente da comunidade, apesar de o grid ter sido uma decisão estratégica original.
- **Performance tardia pode destruir a própria fantasia de escala.** Cities XL mostrou historicamente que cidades grandes com simulação pesada e arquitetura incapaz de usar bem o hardware podem transformar o crescimento, que deveria ser recompensa, em degradação progressiva da experiência.
- **Profundidade não exige que tudo seja simulado da mesma forma.** Jogos bem-sucedidos variam radicalmente: alguns simulam cidadãos individualmente; outros concentram profundidade em logística, espaço, recursos ou decisões sociais. O nível de fidelidade deve seguir a fantasia central.

### Heurísticas provisórias para futuros protótipos

Estas heurísticas ainda não são decisões de produto:

1. começar pela menor versão do loop que já permite uma decisão interessante;
2. medir uma feature pelo quanto ela fortalece o loop central, não por quão impressionante ela é isoladamente;
3. tratar pathfinding, trânsito, economia e quantidade de agentes como problemas de arquitetura desde os primeiros benchmarks;
4. permitir que o nível de controle do jogador suba conforme a escala cresce, evitando repetir manualmente operações que já foram compreendidas;
5. separar fidelidade de simulação de fidelidade visual: um cidadão pode ter estado persistente sem exigir atualização completa e renderização permanente;
6. preferir sistemas cuja causa e consequência possam ser explicadas ao jogador;
7. prototipar o maior risco técnico e o maior risco de diversão cedo, antes de produzir grande quantidade de conteúdo;
8. observar jogadores em vez de confiar apenas em opinião declarada: comportamento real costuma revelar problemas diferentes dos pedidos explícitos;
9. manter a possibilidade de cortar ou simplificar sistemas que desviem atenção do city building;
10. definir explicitamente qual fantasia domina o IndexCities antes de comprar complexidade para sistemas auxiliares.

### Referências principais desta rodada

- GDC Vault, Against the Storm Postmortem: https://gdcvault.com/play/1034422/-Against-the-Storm
- Kingdoms Reborn — entrevista com Ittinop Dumnernchanvanit: https://www.unrealengine.com/developer-interviews/inside-kingdoms-reborn-s-game-dev-s-journey-of-discovery-and-city-building
- Tropico 6 — pesquisa, design claims e simulação de agentes: https://www.unrealengine.com/developer-interviews/limbic-entertainment-revamps-tropico-6-with-unreal-engine-4
- Tropico 6 — agentes individuais e vínculo com a população: https://www.unrealengine.com/developer-interviews/how-new-developers-rebuilt-a-banana-republic-for-tropico-6
- Banished — pathfinding: https://banished-wiki.com/wiki/Pathfinding
- Citybound — arquitetura e simulação: https://aeplay.org/citybound
- Citybound — Living Design Doc: https://app.notion.com/aeplay/citybound-living-design-doc-3b42707cbca54d079d301d9190ac85bb
- Songs of Syx — proposta de simulação em grande escala com automação: https://songsofsyx.com/
- Anno 1800 — logística: https://www.anno-union.com/devblog-pushing-carts/
- Timberborn — deep dive da simulação de água: https://www.gamedeveloper.com/design/deep-dive-timberborn-s-water-mechanics
- Farthest Frontier — evolução pós-Early Access e construção livre: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Cities XL — histórico de gargalos de CPU/memória: https://community.simtropolis.com/forums/topic/34341-multicore-support/
- Surviving Mars — como a fantasia central alterou o city builder tradicional: https://www.gamedeveloper.com/design/how-the-i-surviving-mars-i-devs-built-a-city-builder-on-a-barren-planet
- Manor Lords — jornada de desenvolvimento solo e prototipagem: https://www.unrealengine.com/developer-interviews/solo-dev-makes-sophisticated-sim-manor-lords-using-unreal-engine


### Pesquisa ampliada: repositórios, comunidades e vídeos

**Status:** evidência adicional, não decisão de produto.

| Fonte | O que mostra | Sinal positivo | Risco / sinal negativo | Lição provisória |
| --- | --- | --- | --- | --- |
| IsoCity (GitHub) | City builder isométrico com veículos, pedestres, pathfinding, economia e zoning | Mostra que é possível prototipar muitos sistemas em arquitetura simples | Misturar tráfego, pedestres, economia e crescimento aumenta rapidamente o acoplamento | Usar como referência de protótipo, não como prova de escala |
| SlimCity (GitHub) | Simulação determinística em Web Worker, renderização instanciada e tráfego estatístico | Separa simulação de apresentação e ganha desempenho | Tráfego deixa de ser totalmente individual | Forte referência para comparar agente real vs representação estatística |
| KotCity (GitHub) | Engine multithread, A*, economia dinâmica, overlays e mapas grandes | Arquitetura explícita para cálculo paralelo | Roadmap muito amplo mostra como o escopo cresce rapidamente | Escopo e arquitetura precisam permanecer proporcionais |
| Cimulity (GitHub) | Núcleo pequeno, mapa 64x64, fixed timestep e serviços essenciais | Simulação legível e limitada facilita iteração | Menos profundidade e escala | Boa referência para vertical slice pequena |
| Citybound (GitHub) | Realismo, detalhes microscópicos, Rust e actor model | Ambição técnica tratada desde a arquitetura | Projeto extremamente ambicioso e longo | Microsimulação exige arquitetura dedicada e disciplina de escopo |
| Micropolis / SimCity Classic (GitHub) | Código original/derivado de SimCity separado da interface em versões modernas | Demonstra o valor duradouro de separar engine de simulação e UI | Modelo antigo é muito agregado para alguns objetivos modernos | Abstração pode gerar gameplay profundo sem agentes individuais |
| LinCity-NG (GitHub) | Projeto mantido por décadas, economia e sustentabilidade | Longevidade e código aberto mostram valor de sistemas estáveis e compreensíveis | Evolução longa cria compatibilidade e dívida de saves | Versionar persistência cedo quando o jogo começar a estabilizar |
| ProcIsoCity (GitHub) | Save versionado, deltas, checksums, tráfego opcional por passes | Persistência e determinismo são tratados como sistemas de primeira classe | A cada feature o formato de save ganha complexidade | Savegame deve ser considerado na arquitetura antes de produção de conteúdo |
| r/gamedev | Discussão sobre centenas de agentes | Atualizações espaçadas, abstração, batching e LOD são práticas recorrentes | Atualizar tudo a cada frame é inviável | Frequência de atualização deve variar por sistema e relevância |
| r/CityBuilders | Realismo sem tédio | Jogadores querem escala e coerência | Rejeitam tanto abstração artificial quanto micro excessivo | Procurar coerência com baixa burocracia |
| Steam — Workers & Resources | Feedback sobre UI, empregos, terreno e logística | Público hardcore aceita muita profundidade | Falta de informação e repetição transformam profundidade em trabalho | Complexidade precisa de diagnóstico, automação e controles em lote |
| Steam — Cities: Skylines II | Discussões sobre simulação vs city painter | Jogadores valorizam causa e consequência reais | Se agentes/economia parecem falsos, confiança na simulação cai | Não prometer profundidade que o jogador consegue contradizer facilmente |
| Farthest Frontier fórum | Migração de grid rígido para colocação 360° | Liberdade estética é valorizada | Grid rígido limitou expressão visual | Construção precisa equilibrar precisão, legibilidade e expressão |
| Against the Storm — GDC / entrevistas | O início da cidade é a parte mais interessante; resets evitam late game estagnado | Repetição com variação mantém decisões frescas | Cidade infinita tende a chegar a estado resolvido | Precisamos resolver cedo qual é o propósito do late game |
| Songs of Syx — site/devlogs/entrevistas | Milhares de indivíduos detalhados com tarefas mundanas automatizadas | Profundidade individual pode coexistir com grande escala | Sem automação, a escala seria impraticável para o jogador | O nível de controle deve subir conforme a cidade cresce |
| Discussões sobre endgame em r/CityBuilders | Jogadores divergem entre sandbox, vitória e campanhas | Metas e novos desafios mantêm motivação | Crescimento sem novos problemas vira rotina | Progressão precisa mudar a natureza das decisões, não só aumentar números |

Referências desta rodada incluem: GitHub (IsoCity, SlimCity, KotCity, Cimulity, Citybound, Micropolis, LinCity-NG, ProcIsoCity), GDC Vault, Steam Community, Reddit r/CityBuilders e r/gamedev, Crate Entertainment Forums e devlogs/entrevistas de Songs of Syx.


### Rodada ampliada: arquitetura técnica, comunidades e casos históricos

**Status:** exploração; nenhuma linha abaixo é requisito automático.

| Referência | Evidência observada | O que funcionou | O risco / falha | Relevância para o IndexCities |
| --- | --- | --- | --- | --- |
| A/B Street | Microsimulação de carros, bicicletas e pedestres; migrou de timestep discreto para simulação por eventos | Agentes individuais sem precisar acordar todos a cada 0,1 s | Modelo exige scheduler/eventos e simplificações explícitas | Forte referência para cidadãos/veículos persistentes com atualização sob demanda |
| OpenTTD | Pathfinding em mapas enormes; introduziu pathfinding hierárquico por regiões e caches | Reduziu busca de baixa resolução e reutilizou caminhos | Manter abstrações e invalidá-las após mudanças aumenta complexidade | Considerar hierarquia/caches em vez de A* global por viagem |
| OpenTTD — saves extremos | Save real com ~50 mil estações e outro com ~13,9 mil veículos expôs gargalos | Saves patológicos viraram benchmarks de otimização | Escala real revela problemas invisíveis em mapas pequenos | Criar saves sintéticos grandes como benchmark desde cedo |
| SlimCity | Simulação determinística em worker, fixed timestep, renderização instanciada, tráfego estatístico | Separação forte entre sim e apresentação | Tráfego não é microsimulado | Referência do extremo agregado para comparar com agentes reais |
| IsoCity | Veículos/pedestres autônomos, semáforos, economia e zoning | Prova que muitos sistemas cabem em protótipo simples | Acoplamento cresce rapidamente | Útil para estudar interfaces mínimas entre sistemas |
| Citybound | Microsimulação como objetivo central e arquitetura criada para isso | A arquitetura nasce da ambição | Escopo técnico gigantesco e desenvolvimento muito longo | Evitar copiar a ambição sem validar o valor para o jogador |
| SimCity 2013 / GlassBox | Agent-based simulation tornou tráfego e economia centrais | Criou sensação de processos físicos percorrendo a cidade | Ajuste de agentes e limites de mapa viraram problemas centrais pós-lançamento | Agentes são uma escolha de produto + arquitetura, não um detalhe visual |
| Ostriv | Prefere melhorar sistemas existentes antes de adicionar mais conteúdo | Detalhe e coerência tornam features existentes mais significativas | O gênero exige ciclos longos de pensar/testar/refazer | Melhorar profundidade antes de ampliar catálogo de prédios |
| Ostriv — UI/construção | Refez UI, footprint de prédios, fila e indicadores de construção travada | Diagnóstico reduz micro e frustração | Sem feedback, jogador não sabe por que algo parou | Todo sistema profundo precisa explicar seu estado e seus bloqueios |
| Farthest Frontier | Grid inicialmente escolhido por valor estratégico; depois free-build 360° após anos de feedback | Grid ajudou legibilidade/estratégia; free-build aumentou expressão | Liberdade total também pode tornar posicionamento mais trabalhoso | Talvez oferecer precisão/assistência opcional em vez de dogma grid vs livre |
| Farthest Frontier — QoL | Comunidade pede menos cliques, atalhos, melhor janela de placement e menos obstrução | QoL preserva profundidade sem remover sistema | UI pode transformar uma boa mecânica em fadiga física | Medir ações repetidas e distância de interação, não só regras da simulação |
| Project Highrise | Sistemas complexos são introduzidos progressivamente conforme torre cresce | Camadas de complexidade aparecem quando passam a importar | Expor tudo no início seria esmagador | Unlock deve ensinar sistemas por necessidade, não apenas premiar nível |
| Highrise City | Promete dezenas de milhões de cidadãos e dezenas de milhares de edifícios, com número visual/simulado de veículos muito menor | Diferencia população lógica de representação ativa | Números gigantes podem ser mais agregados que individuais | População total, agentes ativos e entidades visuais devem ser métricas separadas |
| TheoTown — comunidade | Muito citado como leve e capaz de rodar em hardware fraco | Escala visual relevante sem hardware extremo | Menos fidelidade gráfica/individual | Hardware-alvo é também uma decisão de design do nível de simulação |
| Urbek — comunidade | Frequentemente recomendado a quem quer construção com pouco micro | Relações espaciais e recursos substituem parte da microgestão | Menos interessante para quem busca cidadão individual | Profundidade espacial pode substituir profundidade agent-based em alguns sistemas |
| r/CityBuilders — realismo vs tédio | Jogadores pedem repetidamente realismo de escala sem a burocracia de Workers & Resources | Coerência é valorizada | Realismo operacional passo a passo cansa parte do público | Alvo promissor: mundo coerente com baixa burocracia manual |
| r/CityBuilders — zen builders | Parte do público rejeita city painters sem sistemas | Sistemas dão propósito ao layout | Cidade puramente estética pode parecer sem vida | Construção visual deve produzir consequência observável |
| r/CityBuilders — endgame | Parte prefere meta; parte prefere sandbox; consenso frequente é que problemas acabam cedo demais | Objetivos/opções diferentes prolongam interesse | Crescimento linear termina em estado resolvido | Progressão precisa mudar decisões ou permitir novos objetivos |
| r/gamedev — 500+ agentes | Práticas recorrentes: abstração, batching, LOD, pathfinding esporádico, atualizações em frequências diferentes | Reduz CPU sem abandonar agentes | Implementar ingenuamente tudo por frame não escala | Projetar scheduler de simulação e budgets explicitamente |
| Comunidade low-end | SimCity 4, TheoTown e indies leves continuam citados por rodar bem | Acessibilidade de hardware amplia público | Jogos modernos frequentemente ficam CPU/GPU-heavy | Definir orçamento de CPU/GPU/memória antes da ambição crescer |

#### Padrões técnicos que ficaram mais fortes nesta rodada

1. **Entidade persistente não precisa significar processamento contínuo.** Estado pode existir sempre e ser atualizado por eventos, agenda ou frequência adaptativa.
2. **Pathfinding deve ser hierárquico, cacheável e invalidável.** Buscar do zero em toda viagem é a solução mais simples, não a mais escalável.
3. **População lógica, agentes ativos e representação visual são grandezas diferentes.** Elas podem ter ordens de magnitude distintas.
4. **Saves extremos devem fazer parte do benchmark.** Vários gargalos só aparecem depois que redes e cidades ficam grandes.
5. **Interface é parte da simulação.** Se o jogador não entende causa, gargalo ou bloqueio, a profundidade se transforma em ruído.
6. **Automação deve aparecer depois que a decisão já foi aprendida.** Isso preserva o valor inicial da microgestão sem obrigar sua repetição em escala.
7. **Construção precisa equilibrar liberdade e assistência.** Grid dá legibilidade; free-build dá expressão; ferramentas híbridas podem capturar ambos.
8. **Escala é uma decisão de arquitetura.** Aumentar população, rede viária ou quantidade de edifícios não é apenas mudar um número.

Fontes principais adicionais:
- A/B Street — discrete event simulation: https://a-b-street.github.io/docs/tech/trafficsim/discrete_event/index.html
- A/B Street — repositório: https://github.com/a-b-street/abstreet
- OpenTTD — novo pathfinder hierárquico de navios: https://www.openttd.org/news/2024/02/24/new-ship-pathfinder
- OpenTTD — performance em saves extremos: https://www.openttd.org/news/2019/04/01/monthly-dev-post
- SimCity / GlassBox retrospective: https://simscommunity.info/2013/10/04/blog-post-state-of-simcity/
- Ostriv — Alpha 2: https://ostrivgame.com/alpha-2-released/
- Project Highrise — entrevista: https://www.gamegrin.com/articles/project-highrise-interview/
- Highrise City — Steam: https://store.steampowered.com/app/1489970/Highrise_City/
- Farthest Frontier — grid/free placement: https://forums.crateentertainment.com/t/building-direction/123861/5
- Farthest Frontier — free build 360: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Reddit r/gamedev — 500+ agentes: https://www.reddit.com/r/gamedev/comments/1uccg2b/how_can_colony_management_games_simulate_500/
- Reddit r/CityBuilders — realista sem tédio: https://www.reddit.com/r/CityBuilders/comments/1ve9qdu/best_city_builder_that_is_realistic_but_not/
- Reddit r/CityBuilders — endgame: https://www.reddit.com/r/CityBuilders/comments/1uiy417/should_city_builders_ever_have_a_true_endgame/


---

## Dinheiro, renda familiar e consumo

**Status:** em exploração.

### Renda individual e familiar

Direção discutida:

- cidadãos adultos podem ter dinheiro individual;
- crianças não precisam possuir uma conta financeira própria por padrão;
- a família/domicílio pode ter uma noção agregada de renda e despesas compartilhadas;
- ainda precisa ser decidido como dividir salário, patrimônio, contas da casa, dependentes e gastos individuais.

Uma opção promissora para testar é manter **renda e patrimônio individuais**, mas calcular também métricas de **renda familiar disponível** e despesas comuns do domicílio. Isso preserva individualidade sem obrigar toda decisão econômica a existir em nível bancário extremamente granular.

### Até onde simular compras?

A questão central não é “simular tudo”, mas decidir quais detalhes geram consequências úteis no jogo.

Há três níveis possíveis:

1. **Compra abstrata**  
   O cidadão gasta dinheiro e a loja recebe receita, mas não existe item/estoque real.

2. **Compra por categorias**  
   A loja mantém estoques de categorias como comida, remédios, combustível, roupas e materiais. O cidadão consome uma categoria, não uma SKU específica.

3. **Compra por item individual**  
   Cada produto possui unidade, preço, estoque e consumo próprios.

### Avaliação provisória

**Decisão fechada:** o modelo inicial será **por categorias de produtos**, não por SKU individual.

Razões preservadas desta exploração:

- mantém a relação real entre produção, logística, estoque, consumo e falta de produtos;
- permite que comércio tenha motivo para receber entregas;
- permite que preço e escassez afetem cidadãos e empresas;
- evita explodir o número de entidades com milhares de SKUs que provavelmente não gerariam gameplay proporcional;
- deixa espaço para aprofundar categorias específicas mais tarde se elas se provarem importantes.

### Regra de profundidade sugerida

Um detalhe só deve entrar na simulação se produzir ao menos uma consequência observável relevante, como:

- alterar decisão do cidadão;
- alterar preço, estoque ou logística;
- causar falta de produto/serviço;
- mudar tráfego;
- afetar renda, bem-estar ou emprego;
- criar uma decisão interessante para o jogador.

Se um detalhe não muda nenhum sistema relevante, ele pode permanecer agregado.


---

## Capacidade hospitalar e equipe médica

**Status:** em exploração; existência de funcionários reais e capacidade real já está decidida na SPEC.

A direção mais coerente é evitar um hospital com capacidade puramente abstrata.

Modelo a investigar:

- cada hospital/unidade possui um quadro real de funcionários;
- médicos, enfermeiros e outros profissionais podem ter funções diferentes;
- capacidade de atendimento depende da equipe disponível, instalações e tempo;
- leitos podem ser um recurso separado da capacidade de consulta;
- turnos podem reduzir a equipe disponível em determinados horários;
- ausência de profissionais pode reduzir capacidade mesmo quando o prédio físico comportaria mais pacientes;
- emergências, consultas e internações podem competir por recursos diferentes.

### Pergunta em aberto

Ainda precisa ser decidido o nível de granularidade da equipe:

1. apenas "médicos" e "enfermeiros";
2. especialidades médicas relevantes;
3. funções hospitalares adicionais;
4. turnos e escalas individuais.

A regra de profundidade continua a mesma: só detalhar quando isso gerar consequência clara para gameplay, capacidade, custo, deslocamento ou decisão do jogador.


---

## Turnos de trabalho e operação contínua

**Status:** em exploração; suporte a múltiplos turnos já está decidido na SPEC.

A intenção é permitir turnos diferentes para empresas e serviços, inclusive quando houver operação noturna, mas sem transformar escala de funcionários em microgerenciamento manual obrigatório.

Direção a validar:

- cada trabalhador pode ter um horário/turno individual;
- empresas definem janelas de operação e quantidade de vagas por turno;
- serviços críticos podem precisar de cobertura contínua;
- a simulação pode gerar escalas automaticamente a partir das necessidades da empresa/serviço;
- o jogador deve intervir apenas quando houver uma decisão relevante, como ampliar capacidade, mudar horário de funcionamento ou lidar com falta de pessoal;
- horários diferentes devem distribuir ou concentrar tráfego ao longo do dia.

O objetivo é obter consequências reais de horário e escala sem obrigar o jogador a montar manualmente cada escala de trabalho, salvo se isso se provar divertido em protótipo.


---

## Valor imobiliário e propriedade de lotes

**Status:** em exploração.

Ainda não está decidido se cada lote terá proprietário individual e valor de mercado próprio.

Também não está definido como calcular valor imobiliário.

Fatores candidatos a investigar com dados e referências reais:

- proximidade de empregos e comércio;
- acesso viário e transporte;
- qualidade de escolas e saúde;
- criminalidade;
- poluição e ruído;
- acesso a áreas verdes e água;
- congestionamento;
- oferta e demanda por moradia;
- tamanho/qualidade do imóvel;
- renda da vizinhança;
- impostos e custos recorrentes.

O modelo deve ser validado antes de virar regra de produto. Evitar fórmula arbitrária sem referência empírica.

### Dívida municipal

A dívida municipal passa a fazer parte da direção do produto porque uma cidade sem mecanismo de financiamento pode entrar em estado de caixa negativo sem caminho de recuperação.

Ainda precisa ser decidido:

- quem empresta;
- limite de endividamento;
- juros;
- prazo;
- consequências de inadimplência;
- se empréstimos aparecem automaticamente como ferramenta de emergência ou exigem decisão explícita do jogador.


---

## Falência pessoal, população sem moradia e demanda imobiliária

**Status:** em exploração; existência desses fenômenos já está decidida na SPEC.

### Falência pessoal

Ainda precisa ser definido o que acontece quando um cidadão/família perde capacidade de pagar suas despesas.

Perguntas a investigar:

- atraso de aluguel;
- despejo;
- perda de acesso a bens/serviços;
- mudança para moradia mais barata;
- busca emergencial de emprego;
- apoio social;
- endividamento pessoal ou ausência dele;
- efeitos sobre bem-estar, saúde e criminalidade.

A consequência deve criar dinâmica sistêmica sem virar punição arbitrária.

### População sem moradia

A população sem moradia deve existir como estado real da simulação, mas a resposta pública ainda está aberta.

Opções a pesquisar:

- abrigos temporários;
- assistência social;
- moradia pública/subsidiada;
- programas de emprego;
- saúde e atendimento emergencial;
- impacto em orçamento municipal e uso do espaço urbano.

A prefeitura deve poder reagir, mas o nível de controle e automação ainda precisa ser decidido.

### Demanda orientando construção

Como toda construção é colocada pelo jogador, a simulação precisa indicar demanda de forma clara.

A demanda pode considerar, entre outros:

- número de famílias procurando moradia;
- faixa de renda;
- vagas de emprego abertas;
- falta de comércio/serviços por categoria;
- estoque insuficiente;
- distância e acessibilidade;
- capacidade ociosa existente;
- crescimento populacional.

A intenção é usar demanda como **sinal para decisão do jogador**, não como zoneamento automático.

### Oferta e demanda nos preços

Direção desejada: modelo simples, legível e configurável.

Hipótese inicial a avaliar:

- preço-base do imóvel/aluguel;
- multiplicador de oferta/demanda;
- modificadores de localização e qualidade;
- limites de variação para evitar instabilidade artificial.

A fórmula final só deve ser escolhida depois de testar se o jogador consegue entender por que os preços subiram ou caíram.


---

## Transporte público, estacionamento e combustível

**Status:** parcialmente decidido.

### Transporte público

Já está decidido que o sistema não ficará restrito a ônibus.

Princípios já definidos:

- veículos de transporte público são entidades reais;
- linhas têm percurso real;
- capacidade importa;
- funcionários/motoristas reais fazem parte da operação;
- outros modais além de ônibus devem existir.

Ainda precisa ser explorado quais modais entram primeiro, por exemplo:

- ônibus;
- vans/micro-ônibus;
- bonde/VLT;
- metrô;
- trem suburbano;
- táxi/transporte sob demanda.

A seleção deve considerar escala da cidade e custo de simulação, não apenas catálogo de features.

### Estacionamento

O estacionamento será parte real da mobilidade.

Questões em aberto:

- estacionamento na rua;
- vagas privadas em residências e empresas;
- estacionamentos públicos;
- custo de estacionamento;
- tempo de procura por vaga;
- efeito da falta de vagas no trânsito e na escolha modal.

O objetivo é manter alto realismo de veículos sem transformar estacionamento em microgestão excessiva para o jogador.

### Cadeia de combustível

Decisão de direção:

produção/refino → distribuição → postos → consumo por veículos.

Pontos a explorar:

- origem da matéria-prima;
- refinaria/fábrica como unidade produtiva;
- transporte por caminhões-tanque;
- estoque real nos postos;
- preço do combustível;
- impacto de falta de combustível na mobilidade e economia;
- consumo diferente por tipo de veículo.

O sistema deve se conectar à logística já decidida, em vez de funcionar como recurso abstrato isolado.

---

## Funcionalidades adiadas

**Status:** fora do escopo atual, mas preservadas para reavaliação futura.

- assistência social municipal, incluindo abrigos e programas de apoio;
- saúde mental e dependência química.

Esses tópicos não devem ser implementados agora. Podem ser revisitados futuramente quando os sistemas básicos de população, saúde, moradia e orçamento já estiverem maduros.


---

## Mobilidade ativa e realismo de deslocamento

**Status:** parcialmente decidido.

Já está decidido que pedestres e bicicletas existem fisicamente na cidade.

### Pedestres

A intenção é de alto realismo de deslocamento:

- cidadãos caminham fisicamente entre origem e destino;
- viagens multimodais podem incluir trechos a pé;
- cidadãos podem caminhar até estacionamento, ponto de ônibus, estação, comércio, escola e trabalho;
- o caminho de pedestres precisa respeitar calçadas, travessias e acessibilidade viária.

O nível exato de animação, detecção de obstáculos e priorização de travessias ainda precisa ser prototipado.

### Bicicletas

Decidido:

- bicicletas circulam fisicamente;
- bicicleta é um modal real;
- ciclovias fazem parte da infraestrutura.

Adiado para o futuro:

- estacionamento específico para bicicletas.

### Estoque real no comércio

Foi reforçada a regra de que comércio não possui estoque meramente decorativo.

Ela vale para:

- postos de combustível;
- padarias;
- farmácias;
- mercados;
- demais comércios baseados em bens.

Falta definir a granularidade das categorias e a política de reposição para cada tipo de estabelecimento.

---

## Funcionalidades adiadas de mobilidade

**Status:** fora do escopo atual.

- acidentes de trânsito com colisões, feridos e resposta emergencial;
- manutenção mecânica e quebra de veículos;
- estacionamento específico para bicicletas.

Esses sistemas podem ser revisitados depois que mobilidade básica, trânsito, estacionamento de carros e logística estiverem estáveis.


---

## Vias, travessias e controle de tráfego

**Status:** parcialmente decidido.

### Tipos de via iniciais

Decidido para o escopo atual:

- via urbana comum;
- rodovia.

A via urbana comum terá calçada por padrão.

Novas categorias de via, perfis, larguras, faixas exclusivas e outras variações podem ser adicionadas depois, caso se provem necessárias.

### Estacionamento

Decidido:

- estacionamento na rua ocupa espaço físico real;
- edificações podem oferecer vagas privadas;
- vagas privadas reduzem pressão por estacionamento público.

### Travessias de pedestres

Decidido no escopo atual: pedestres atravessam vias urbanas somente em faixas.

### Semáforos

Decidido no escopo atual: semáforos usam ciclos fixos. Controle adaptativo pode ser reconsiderado futuramente.


---

## Função da rodovia e hierarquia viária

**Status:** direção confirmada parcialmente.

A rodovia será tratada como infraestrutura de mobilidade de alta capacidade, não como via de acesso local.

Implicações já definidas:

- maior velocidade operacional;
- sem construção direta de edifícios ao longo da rodovia;
- acesso à cidade feito por conexões apropriadas com a malha urbana;
- capacidade depende do número de faixas;
- rotatórias e cruzamentos fazem parte da rede urbana.

### Razão de design

Separar rodovia de via urbana ajuda a manter uma hierarquia viária compreensível:

- rodovia: deslocamento rápido entre áreas;
- via urbana: acesso local, calçadas, travessias e estacionamento.

Isso também reduz um problema comum em redes viárias: misturar tráfego local com tráfego de passagem na mesma infraestrutura.

### Travessias

Decidido:

- pedestres atravessam somente em faixas.

Ainda pode ser explorado futuramente:

- semáforo para pedestres;
- tempo de espera;
- prioridade de pedestres;
- travessias elevadas ou passarelas.

### Semáforos

Decidido para o escopo inicial:

- ciclos fixos.

Controle adaptativo pode ser reconsiderado no futuro se congestionamentos e gameplay justificarem a complexidade.


---

## Granularidade operacional de serviços

**Status:** direção parcialmente fechada.

A simulação deve evitar ações instantâneas quando isso destruir causa e consequência, mas também não precisa reproduzir cada segundo do mundo real.

Direção atual:

- carga/descarga leva tempo;
- coleta de lixo leva tempo;
- atendimento de emergência leva tempo;
- embarque/desembarque leva tempo;
- interior dos prédios permanece abstrato;
- entradas e saídas continuam físicas e observáveis.

A duração exata deve ser configurável e calibrada durante os testes de ritmo do jogo.

### Entregas sem janelas artificiais

Não haverá, por enquanto, obrigação de janelas horárias fixas de entrega para comércio.

A logística deve emergir principalmente de:

- disponibilidade de estoque;
- necessidade de reposição;
- disponibilidade de veículos;
- capacidade de carga;
- distância;
- trânsito;
- fila/capacidade no ponto de carga e descarga.

Se no futuro restrições de horário gerarem gameplay útil, elas podem ser reavaliadas.


---

## Filas físicas e horários de funcionamento

**Status:** documentado para futuro; fora do escopo atual.

### Filas físicas em comércio e saúde

No futuro pode ser útil representar filas físicas quando a capacidade de um comércio, hospital ou outro serviço for excedida.

Possíveis consequências:

- espera visível;
- ocupação de calçadas/espaço externo;
- impacto em satisfação;
- atraso em atendimento;
- incentivo para ampliar capacidade.

Não é necessário implementar isso agora.

### Horários de funcionamento

Comércio e serviços podem futuramente ter horários reais de abertura e fechamento.

Possíveis efeitos:

- concentração de viagens em certos horários;
- necessidade de turnos;
- indisponibilidade temporária de serviços;
- redistribuição de demanda.

Também fica fora do escopo atual até a simulação básica de rotina, trabalho e mobilidade estar estável.


---

## Eventos temporários e turismo

**Status:** eventos temporários adiados para o futuro; turismo básico já está decidido na SPEC.

No futuro, eventos como shows, feiras e festivais podem ser usados para gerar picos temporários de:

- visitantes;
- demanda por hotéis;
- trânsito;
- transporte público;
- comércio;
- segurança e serviços urbanos.

Esses eventos não entram no escopo atual. Devem ser revisitados depois que turismo, mobilidade, hotelaria e capacidade dos serviços estiverem estáveis.


---

## Capacidade educacional

**Status:** estrutura decidida; parâmetros ainda em exploração.

A escola não usará apenas um bônus abstrato de "capacidade".

Já está decidido que:

- professores são cidadãos reais;
- alunos são cidadãos reais;
- quantidade de professores e quantidade de alunos importam;
- falta de vaga/acesso pode deixar jovens fora da escola;
- universidade faz parte do sistema de educação e qualificação.

Ainda precisa ser pesquisado e calibrado:

- razão professor/alunos;
- capacidade física por escola;
- necessidade de salas/turmas explícitas ou agregadas;
- duração das etapas de ensino;
- efeito da distância e transporte no acesso;
- relação entre nível educacional, empregos e salário.

Esses valores devem ser baseados em dados reais ou benchmark do jogo, não escolhidos arbitrariamente.

---

## Conexão externa

**Status:** conceito decidido; representação física ainda em exploração.

A conexão externa representa o "mundo fora do mapa".

Ela deve sustentar, conforme os sistemas forem implementados:

- entrada e saída de turistas;
- imigração e emigração;
- transporte rodoviário externo;
- transporte público/interurbano;
- entrada e saída de cargas;
- possíveis outros modais externos no futuro.

A conexão externa evita criar agentes ou mercadorias no meio do mapa sem origem observável.

Ainda precisa ser definido se haverá um único ponto físico, múltiplos pontos por modal ou uma camada lógica comum conectada a diferentes terminais.


---

## Infraestrutura de água, esgoto e energia

**Status:** parcialmente decidido.

### Água e esgoto

Decidido:

- captação física a partir de rio/lago;
- tratamento de água;
- geração real de esgoto;
- tratamento de esgoto;
- demanda e capacidade importam;
- o jogador não precisa desenhar manualmente encanamentos no escopo atual.

Direção de modelagem:

- edifícios consomem capacidade hídrica;
- infraestrutura construída aumenta capacidade de captação/tratamento;
- excesso de demanda pode gerar falta d'água ou esgoto sem atendimento;
- cobertura pode ser modelada de forma agregada ou por área, sem rede de tubos explícita.

O modelo exato de cobertura ainda precisa ser escolhido.

### Energia

Decidido:

- geração real;
- demanda real;
- capacidade limitada;
- não exigir desenho manual detalhado de linhas/rede elétrica no escopo atual;
- solar e eólica fazem parte das opções.

Ainda em exploração:

- carvão;
- nuclear;
- regras de custo, combustível, poluição e capacidade por tipo de usina.

### Hidrelétrica

**Adiada para o futuro.**

Como hidrelétrica depende fortemente de relevo, curso d'água, barragem e alteração física do terreno, ela deve ser reconsiderada depois que terreno, água e simulação ambiental estiverem mais maduros.

### Poluição

Já está decidido que poluição deve vir de fontes reais da cidade e afetar sistemas reais.

Ainda precisa ser definido:

- tipos de poluição (ar, água, solo, ruído);
- raio/dispersão;
- relação com saúde;
- impacto em valor imobiliário;
- efeito de vento, relevo ou fluxo de água.


---

## Resíduos e degradação por falta de água

**Status:** parcialmente decidido.

### Resíduos

Decidido:

- lixo precisa ter destino físico;
- aterro e/ou incineração fazem parte do sistema;
- capacidade de processamento importa;
- reciclagem fica fora do escopo inicial.

Ainda precisa ser definido:

- tipos de instalação iniciais;
- custos operacionais;
- impacto ambiental;
- distância/logística de coleta;
- capacidade por instalação.

### Falta de água

Decidido:

- a resposta não é instantânea;
- primeiro há perda de eficiência;
- após um período sem abastecimento, a atividade pode parar;
- duração e limiares devem ser configuráveis.

Isso permite calibrar o sistema sem transformar uma interrupção momentânea em fechamento imediato.


---

## Clima e ambiente sazonal

**Status:** documentado para futuro; fora do escopo atual.

Tópicos preservados para reavaliação futura:

- chuva;
- temperatura;
- estações do ano;
- efeitos climáticos sobre a cidade;
- chuva forte e alagamentos;
- impactos de alagamento no trânsito e em edificações;
- aumento de risco de incêndio em períodos secos/quentes;
- variação de consumo de energia com frio/calor;
- redução de disponibilidade de água em períodos secos.

Nenhum desses sistemas deve ser implementado no escopo atual. Devem ser revisitados quando a simulação básica de mobilidade, serviços urbanos, energia, água e emergências estiver estável.


---

## Terreno, vegetação e alcance de serviços

**Status:** parcialmente decidido.

### Edição de terreno

**Adiada para o futuro.**

No escopo inicial, o jogador não precisa modificar relevo.

Razão prática: terraplanagem aumenta a complexidade de vários sistemas ao mesmo tempo, incluindo:

- colocação de edifícios;
- vias e inclinações;
- navegação;
- água;
- colisões;
- visual;
- geração/validação do mapa.

A decisão pode ser reavaliada depois que construção, vias, água e terreno-base estiverem estáveis.

### Vegetação

Direção decidida:

- vegetação deve ter presença visual forte;
- árvores e outros elementos naturais podem ser removidos para construção;
- variedade, densidade e integração com ruas/lotes devem receber atenção visual.

Ainda pode ser explorado no futuro se vegetação terá efeitos sistêmicos adicionais além de apresentação e uso de espaços públicos.

### Parques, amenidades e serviços

O modelo de alcance e influência ainda está em pesquisa. A seção específica **"Alcance, influência e escolha de serviços"** contém a comparação atual entre raio rígido, heurística de influência, custo de viagem e modelos híbridos.

### Poluição sonora

**Fora do escopo atual.**

Pode ser reavaliada futuramente caso ruído se prove relevante para bem-estar, moradia ou valor imobiliário.


---

## Alcance, influência e escolha de serviços

**Status:** em pesquisa; não é uma regra fechada de produto.

A discussão anterior sobre "não usar círculo de influência" foi fechada cedo demais e fica corrigida aqui.

O que se quer investigar:

- um círculo/área visual pode ser útil para mostrar onde um serviço tende a ter **maior influência ou conveniência**;
- esse círculo não precisa significar um corte absoluto em que cidadãos fora dele ficam proibidos de usar o serviço;
- um cidadão pode aceitar viajar mais longe quando houver motivo, necessidade, qualidade superior, falta de alternativa ou capacidade disponível;
- diferentes sistemas podem precisar de modelos diferentes: hospital, escola, parque, comércio e lazer não necessariamente usam a mesma regra.

Modelos a comparar:

1. **Raio rígido** — simples e barato, mas pouco realista.
2. **Raio como peso/heurística** — proximidade aumenta preferência, sem bloquear destinos mais distantes.
3. **Custo de viagem** — escolha baseada em tempo/distância real de rota.
4. **Modelo híbrido** — raio para UI/diagnóstico + custo de viagem/capacidade para decisão real.

A decisão deve vir de pesquisa, protótipo e legibilidade para o jogador. Não assumir que "fora do círculo ninguém vem".

---

## Geração de mapa e reprodutibilidade

**Status:** estrutura decidida; algoritmo ainda em exploração.

Decidido:

- seed determinística;
- mesma seed deve reproduzir o mesmo mapa-base;
- seed será ferramenta importante de debug, teste e reprodução de bugs;
- terreno inicial já inclui natureza e conexão externa;
- sem compra de tiles/áreas no escopo atual;
- borda fixa no início.

Ainda precisa ser definido:

- geração da base plana do mapa;
- distribuição de vegetação;
- geração de rio/lago;
- posição e quantidade de conexões externas;
- tamanho do mapa;
- como versionar a geração para que uma mesma seed continue reproduzível quando o algoritmo mudar.

### Observação importante para debug

Se o algoritmo de geração evoluir, apenas guardar a seed pode não ser suficiente para reproduzir mapas antigos. Uma solução futura pode exigir armazenar também a **versão do gerador** ou serializar o mapa resultante no save. Isso deve ser considerado quando a implementação começar.


---

## Terreno plano no escopo inicial

**Status:** decidido.

O mapa inicial do IndexCities será plano.

Consequências para o escopo atual:

- não há morros ou variações relevantes de elevação;
- não é necessário resolver inclinação de ruas ou prédios nesta fase;
- edição de terreno continua adiada;
- rios e lagos podem existir em um terreno essencialmente plano;
- pontes e outras estruturas que dependam de desnível devem ser avaliadas separadamente, sem assumir relevo acidentado.

Essa decisão reduz complexidade de construção, pathfinding, geração de mapa e validação durante as primeiras fases do projeto.


---

## Materiais de construção, importação e estoque

**Status:** consumo real, importação, armazenamento e entrega física estão decididos; a lista de materiais abaixo é uma recomendação de pesquisa e ainda precisa de aprovação.

### Resultado da pesquisa ampla

A evidência converge para alguns materiais muito mais importantes que outros quando o objetivo é representar **edifícios + ruas + pontes + infraestrutura urbana** sem explodir o número de SKUs.

O USGS destaca areia, cascalho, rocha/pedra e argila como materiais minerais que formam grande parte de edifícios, vias, pontes e infraestrutura. A FHWA trata agregados como elemento crítico de base de vias, pavimento asfáltico, concreto, estruturas e drenagem. No concreto, agregados representam aproximadamente 60–75% do volume; no asfalto, agregados ficam perto de 90–95% da mistura em massa. Aço é central em edifícios, concreto armado e pontes, e madeira permanece material estrutural relevante em construções e pontes.

Fontes principais:
- USGS — Building America: Construction Materials (2026): https://www.usgs.gov/mission-areas/geology-energy-minerals/science/building-america-usgs-and-nations-construction
- FHWA — Aggregates: https://www.fhwa.dot.gov/pavement/aggregates/
- American Cement Association — Applications of Cement: https://www.cement.org/cement-concrete/applications-of-cement/
- FHWA — Warm Mix Asphalt FAQ: https://www.fhwa.dot.gov/innovation/everydaycounts/edc-1/wma-faqs.cfm
- U.S. DOE — Iron and Steel Manufacturing: https://www.energy.gov/cmei/ito/iron-and-steel-manufacturing
- worldsteel — Rebar / construction: https://worldsteel.org/wider-sustainability/life-cycle-thinking/lca-eco-profiles-2026-release/global-rebar-construction/
- USDA Forest Service — Wood Handbook: https://research.fs.usda.gov/fpl/wood-handbook

### Tier 1 recomendado para o primeiro POC

#### 1. Agregados

Representação agregada de:
- areia;
- cascalho;
- brita/pedra britada.

Por que merece existir:
- entra diretamente em base de ruas;
- é a maior fração física de concreto e asfalto;
- também aparece em drenagem, fundações e terraplenagem leve;
- cria uma cadeia de carga pesada com grande impacto logístico.

Cadeia candidata:

extração/pedreira/areal → britagem/classificação → agregados → depósito / concreteira / usina de asfalto / obra

**Dependência ainda aberta:** produzir agregados localmente exige decidir como recursos minerais aparecem no mapa. Até isso existir, matéria-prima ou agregado acabado pode ser importado.

#### 2. Cimento

Cimento não deve ser confundido com concreto.

Cadeia física real simplificada:

calcário + argila/xisto + outros corretivos → forno → clínquer → moagem com gesso/outros materiais → cimento

Fonte: American Cement Association — Cement & Concrete FAQ:
https://www.cement.org/cement-concrete/cement-concrete-faq/

Recomendação de gameplay:
- cimento é insumo industrial;
- pode ser importado;
- produção local completa exige indústria pesada e recursos minerais, portanto pode entrar depois da concreteira.

#### 3. Concreto

Cadeia simplificada:

cimento + agregados + água → usina/concreteira → caminhão-betoneira → canteiro

A American Cement Association registra que concreto é formado por cimento, água e agregados, e que concreto de central pode ser entregue ao canteiro por caminhão-misturador.

Por que é muito forte para o jogo:
- usado em edifícios, fundações, pontes e infraestrutura;
- conecta água + cimento + agregados;
- exige produção e transporte físico;
- cria motivo claro para construir uma concreteira local em vez de importar tudo.

#### 4. Aço

Aço deve permanecer uma única categoria de material inicialmente, sem separar vergalhão, perfil, chapa etc.

Rotas reais principais:
- minério de ferro → ferro → aciaria/BOF → aço;
- sucata → forno elétrico a arco (EAF) → aço.

O DOE registra ferro-minério e sucata como matérias-primas importantes; worldsteel mostra aço estrutural e vergalhão em edifícios, rodovias e pontes.

Recomendação de gameplay:
- **import-first** no começo;
- produção local de aço deve ser uma indústria mais avançada;
- quando implementada, pode usar matéria-prima importada e/ou sucata, evitando exigir uma mina de ferro em todo mapa.

#### 5. Madeira serrada

Cadeia simplificada:

floresta/logs → serraria → madeira serrada/produtos estruturais → depósito/obra

A USDA documenta madeira, madeira serrada e produtos engenheirados como materiais de engenharia usados em edifícios e pontes.

Recomendação:
- uma única categoria **madeira** no primeiro POC;
- não separar tábuas, vigas, compensado, CLT etc.;
- decidir separadamente se árvores comuns do mapa podem virar matéria-prima ou se haverá silvicultura própria.

#### 6. Asfalto

Cadeia simplificada:

agregados + ligante asfáltico → usina de asfalto → caminhão → obra viária

A FHWA informa que misturas asfálticas são majoritariamente agregados, ligados por material asfáltico derivado do processamento de petróleo.

Sinergia importante:
- o ligante pode futuramente se conectar à cadeia de combustível/refino já planejada;
- agregados compartilham a mesma cadeia usada por concreto.

### Tier 2 recomendado para depois do POC

Esses materiais são reais e importantes, mas acrescentam menos ao primeiro loop material ou criam cadeias muito específicas.

#### Alvenaria / tijolos / blocos
- argila/xisto → preparação → moldagem → forno → tijolo;
- ou cimento + agregados → bloco de concreto.
- USGS confirma argila/xisto como base de tijolos.
- recomendação: deixar para depois; regionalidade e sobreposição com concreto reduzem a urgência.

Fonte:
https://pubs.usgs.gov/myb/vol1/2019/myb1-2019-clay-shale.pdf

#### Vidro
- areia silicosa + soda ash + calcário → forno → vidro.
- relevante em edifícios, principalmente fachadas/janelas.
- recomendação: material de segunda fase, especialmente para prédios maiores/comerciais.

Fonte:
https://www.usgs.gov/centers/national-minerals-information-center/soda-ash-statistics-and-information

#### Gesso / drywall
- gesso → processamento → placas/produtos de acabamento.
- muito usado em interiores, mas tem pouca consequência urbana/logística distinta no primeiro POC.
- recomendação: adiar ou agregar em acabamento.

Fonte:
https://www.usgs.gov/centers/national-minerals-information-center/gypsum-statistics-and-information

#### Cobre e componentes elétricos
- cobre é fortemente ligado a fiação, energia e edifícios.
- como a rede elétrica detalhada não será desenhada no escopo atual, separar cobre cedo tende a criar detalhe sem decisão equivalente.
- recomendação: adiar; pode entrar quando infraestrutura elétrica/industrial justificar.

Fonte:
https://www.usgs.gov/centers/national-minerals-information-center/copper-statistics-and-information

#### Alumínio, plásticos, borracha e outros componentes
- reais e relevantes;
- melhor agregar ou deixar implícitos até existir gameplay específico.

### Recomendação de lista inicial

Para validar o diferencial de economia material, o melhor conjunto inicial parece ser:

1. **agregados**
2. **cimento**
3. **concreto**
4. **aço**
5. **madeira**
6. **asfalto**

Essa lista é pequena, mas cria quatro cadeias diferentes e conectadas:

- mineral → agregados;
- mineral/processamento → cimento → concreto;
- metalurgia → aço;
- floresta → madeira;
- petróleo + agregados → asfalto.

### O que produzir localmente primeiro

Nem todo material precisa ter cadeia completa de extração local desde o primeiro protótipo.

Ordem recomendada de POC:

1. **concreteira local** usando cimento e agregados importados;
2. **usina de asfalto local** usando agregados + ligante importados;
3. **serraria** quando a origem dos logs estiver definida;
4. **produção local de agregados** depois de decidir recursos minerais no mapa;
5. **fábrica de cimento** depois de decidir calcário/argila e indústria pesada;
6. **produção de aço** mais tarde, por ser uma cadeia industrial muito mais pesada.

Essa ordem permite testar o loop produção → emprego → estoque → caminhão → obra sem exigir, de saída, geologia de recursos e indústria pesada completa.

### Consequência importante para geração de mapa

Se o jogo permitir extração local de agregados, calcário, argila ou minério, será necessário decidir como **depósitos de recursos naturais** são gerados pela seed.

Isso ainda não está na SPEC e não deve ser assumido durante a implementação.

### Fluxo de construção já decidido

1. a cidade começa com dinheiro;
2. a obra calcula dinheiro + materiais;
3. estoque local é reservado;
4. materiais faltantes podem ser importados, com preço + frete visíveis;
5. caminhões entregam fisicamente;
6. quando a carga chega ao canteiro, o material é consumido de uma vez;
7. mão de obra local do Pátio é usada quando disponível;
8. se faltar capacidade local, equipe externa é contratada e encarece a obra;
9. produção local reduz dependência e custo de importação ao longo do crescimento.


---

## Pesquisa prioritária: objetivo central, progressão e endgame

**Status:** pesquisa específica e ampla obrigatória antes de fechar a estrutura de objetivos.

O IndexCities ainda não tem uma condição de vitória, objetivo central ou modelo de progressão decidido.

Essa decisão não deve ser preenchida com uma meta genérica de "chegar a X habitantes".

A pesquisa futura deve comparar amplamente:

- sandbox puro;
- marcos/milestones;
- objetivos econômicos;
- qualidade de vida;
- crescimento sustentável;
- desafios/cenários;
- metas escolhidas pelo jogador;
- progressão baseada em desbloqueios;
- crises e recuperação;
- objetivos de longo prazo/endgame;
- como outros city builders evitam que o late game vire apenas crescimento numérico.

Critério principal: encontrar **algo distintivo e memorável que dê propósito às decisões sistêmicas do IndexCities**, sem destruir a liberdade de city builder.

O documento [GENRE_BENCHMARK.md](GENRE_BENCHMARK.md) já contém evidências sobre endgame e progressão e deve ser uma das fontes dessa pesquisa, mas não substitui uma rodada dedicada.

---

## Bens e materiais básicos para uma cidade funcionar

### Correção de escopo da pesquisa de materiais

A proposta anterior de cinco famílias misturou três problemas diferentes e **não deve ser tratada como lista aprovada**:

1. materiais para construir e manter infraestrutura;
2. insumos essenciais para operar serviços/economia;
3. mercadorias consumidas pela população.

A pesquisa pedida para o IndexCities deve primeiro responder: **qual é o menor conjunto coerente de materiais físicos necessário para construir, manter e fazer uma cidade funcionar**, preservando cadeias logísticas interessantes sem criar SKUs desnecessários.

Alimentos, medicamentos e bens de consumo continuam relevantes para a economia, mas não devem ser usados para responder sozinhos à pergunta sobre materiais básicos da cidade.


**Status:** pesquisa de baseline; categorias finais ainda não são requisito.

A pergunta aqui é mais ampla do que "quais materiais entram numa obra". O objetivo é identificar quais fluxos físicos mínimos precisam existir para a cidade parecer funcional sem criar centenas de SKUs.

### Referências usadas

A FEMA organiza serviços essenciais de uma comunidade em lifelines que incluem, entre outros, **alimentos/água/abrigo, saúde e cadeia de suprimentos médicos, energia/combustível, transporte e sistemas de água**. Isso ajuda a separar o que realmente sustenta uma cidade do que é apenas variedade comercial.

A OMS trata medicamentos essenciais como produtos que precisam estar disponíveis continuamente em sistemas de saúde funcionais.

O Freight Analysis Framework da FHWA separa fluxos reais de carga em famílias como alimentos, combustíveis, farmacêuticos, madeira, minerais, metais e outros bens manufaturados.

Fontes:
- FEMA — Community Lifelines: https://www.fema.gov/emergency-managers/practitioners/lifelines
- FEMA — Supply Chain Resilience Guide: https://www.fema.gov/sites/default/files/2020-07/supply-chain-resilience-guide.pdf
- WHO — Essential medicines: https://www.who.int/news-room/fact-sheets/detail/essential-medicines
- FHWA — Freight Analysis Framework: https://ops.fhwa.dot.gov/freight/freight_analysis/faf/

### Baseline recomendada para a primeira economia física

Em vez de dezenas de produtos, testar primeiro **cinco famílias de estoque**:

1. **Alimentos** — abastecem residências, mercados, restaurantes e padarias.
2. **Medicamentos e suprimentos médicos** — abastecem farmácias, hospitais e unidades de saúde.
3. **Combustível** — abastece postos e veículos e se conecta a trânsito, transporte público, logística e serviços urbanos.
4. **Materiais de construção** — abastecem obras; família interna inicial a testar: concreto, aço, madeira e asfalto.
5. **Bens gerais** — representa produtos domésticos/duráveis e consumo cotidiano que não justificam SKU próprio no início.

### O que não precisa virar estoque da mesma forma

Alguns sistemas essenciais já decididos devem continuar como **serviços/capacidade**, não como mercadoria genérica:

- água potável;
- tratamento de esgoto;
- eletricidade;
- coleta/destinação de lixo.

### Camada de insumos produtivos

Para produção local, algumas famílias precisam de insumos intermediários. Eles só devem aparecer quando a cadeia correspondente existir.

Exemplos:
- cimento + agregados + água → concreto;
- aço → construção/manufatura;
- madeira → construção/manufatura;
- agregados + ligante asfáltico → asfalto;
- combustível refinado → postos/veículos.

Outros candidatos para fases seguintes, caso tragam gameplay suficiente:
- vidro;
- tijolos/cerâmica;
- plásticos/borracha;
- papel;
- produtos químicos;
- peças/máquinas;
- fertilizantes;
- produtos agrícolas in natura.

### Recomendação atual

Para o primeiro protótipo econômico, começar com alimentos, medicamentos/suprimentos médicos, combustível, materiais de construção e bens gerais. Aprofundar apenas cadeias que criarem decisões interessantes ou gargalos logísticos relevantes.

---

## Construção híbrida: grid lógico + liberdade de posicionamento

**Status:** direção de protótipo aceita; ainda precisa ser validada em implementação.

A proposta atual é testar um modelo **híbrido**, em vez de escolher desde já entre grid rígido e posicionamento totalmente livre.

### Evidência comparativa

Farthest Frontier começou com construção em grid por razões estratégicas e, em 2026, adicionou posicionamento livre em 360° após feedback recorrente dos jogadores. O jogo manteve a possibilidade de alternar entre modo livre e grid durante a colocação. Algumas estruturas continuam presas ao grid por causa das restrições do pathfinding.

Fontes:
- Crate Entertainment — Free-Build Mode: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Crate Entertainment — v1.1.0: https://forums.crateentertainment.com/t/farthest-frontier-v1-1-0/153834
- Godot 4.5 — GridMap: https://docs.godotengine.org/en/4.5/classes/class_gridmap.html

### Protótipo recomendado

Testar:
- uma **grade lógica interna** para ocupação, colisão, lotes e consultas espaciais;
- posicionamento visual com alguma liberdade;
- snap opcional a borda da rua, alinhamento com vizinhos, linhas/células da grade e ângulos úteis;
- possibilidade de mostrar/ocultar a grade como ajuda visual.

### Riscos a medir no POC

- footprints irregulares;
- gaps visuais;
- colisão entre construções;
- conexão correta com calçada/rua;
- estacionamento e pontos de carga;
- pathfinding de pedestres;
- performance das consultas espaciais;
- clareza do feedback de posicionamento.

---

## Estado inicial, depósitos e abastecimento de obras

**Status:** decidido em nível de fluxo; números ainda precisam de calibração.

Decidido:
- a cidade começa essencialmente vazia;
- não é necessário um depósito gratuito inicial;
- materiais podem ser importados pela conexão externa;
- quando uma obra específica não tem material local suficiente, a importação pode ser entregue diretamente ao canteiro;
- depósitos/galpões municipais armazenam materiais produzidos ou mantidos localmente;
- materiais precisam chegar fisicamente ao canteiro antes do avanço da obra;
- fábricas locais podem produzir materiais depois.

Esse fluxo elimina o soft-lock do primeiro depósito sem criar um edifício gratuito artificial.

---

## Agricultura e produção local de alimentos

**Status:** decidido em nível de produto; cadeia detalhada ainda aberta.

- Fazendas/agricultura existirão.
- A produção agrícola gera alimento real para abastecer a economia da cidade.
- A produção deve integrar estoque, transporte e consumo já definidos.

Ainda precisa ser pesquisado/decidido:

- quais tipos de produção agrícola entram primeiro;
- se haverá matéria-prima agrícola separada de alimento processado;
- necessidade de água, trabalhadores, veículos e armazenamento;
- produtividade por área;
- processamento por indústrias alimentícias.


---

## Importação externa e preços iniciais

**Status:** parcialmente decidido.

Decidido:

- mercadorias físicas podem ser importadas enquanto a cidade não as produz localmente em quantidade suficiente;
- importações usam a conexão externa;
- preços externos ficam **estáveis no escopo inicial**, sem mercado externo dinâmico.

Para implementação, o preço deve ser um dado de balanceamento configurável pelo projeto, mas isso não implica necessariamente uma opção exposta ao jogador.


---

## Bootstrap logístico da primeira construção

**Status:** resolvido.

Não existe depósito gratuito nem estoque inicial artificial.

Quando uma obra exige material inexistente localmente:

- a interface mostra o custo adicional de importação;
- materiais podem ser entregues fisicamente diretamente ao canteiro;
- o pagamento não teletransporta a carga.

Para o problema de mão de obra inicial, uma equipe externa temporária entra pela conexão externa e pode executar o primeiro Pátio Municipal de Obras e a infraestrutura mínima necessária. Depois que o Pátio entra em operação, as obras municipais usam o fluxo normal de trabalhadores públicos locais.


---

## Compra automática de material importado ao construir

**Status:** comportamento principal decidido; detalhes de UI e balanceamento ainda precisam ser prototipados.

Ideia: quando o jogador tenta construir algo e a cidade não possui materiais suficientes em estoque, a interface pode mostrar explicitamente que o material faltante será importado.

Exemplo conceitual:

- custo da obra;
- materiais necessários;
- quantidade disponível localmente;
- quantidade faltante;
- custo de importação do material faltante;
- indicação clara de que a obra só avança após a entrega física.

### Vantagens

- evita exigir um estoque inicial artificial;
- explica desde o começo a relação entre dinheiro, materiais e logística;
- mantém a cidade vazia no início;
- evita soft-lock de primeira construção;
- torna o custo real da dependência externa visível;
- reforça a decisão futura de produzir materiais localmente.

### Regra importante

Pagar pela importação **não teletransporta o material**.

O pedido gera um fluxo logístico real:

conexão externa → caminhão externo → destino/depósito/canteiro → descarga → disponibilidade para a obra.

A construção só deve avançar quando o material realmente chegar.

### Regra fechada

Importações acionadas por uma obra específica podem ir diretamente ao canteiro. Importações genéricas de materiais de construção não fazem parte do escopo atual.


---

## Regra de custo de construção e transparência de importação

**Status:** decidido.

Toda construção combina dois tipos de custo:

- **dinheiro**;
- **materiais físicos**.

Quando o estoque local não cobre todos os materiais necessários, a interface deve separar claramente:

- custo base da obra;
- materiais necessários;
- materiais disponíveis localmente;
- materiais faltantes;
- custo adicional de importação;
- custo monetário total.

O objetivo é fazer a dependência externa aparecer como consequência econômica visível, e não como taxa escondida.

Pagar a importação não elimina a logística: a obra só avança depois que os materiais importados chegarem fisicamente.

### Entrega dedicada à obra

Para uma importação disparada por uma obra específica:

- o pedido deve corresponder à quantidade necessária;
- um ou mais caminhões externos transportam essa quantidade;
- os caminhões podem ir diretamente ao canteiro;
- não é necessário desviar a carga para o depósito municipal.

Isso evita transporte e armazenamento artificiais quando o destino da carga já é conhecido.

### Importação para estoque

Para materiais de construção, compra genérica para abastecer estoque fica fora do escopo atual. A importação é acionada por uma obra com falta de recursos.

---

## Depósito municipal inicial

**Status:** decidido em nível funcional; números ainda precisam de calibração.

No escopo inicial:

- um mesmo tipo de depósito municipal aceita todos os materiais;
- capacidade de armazenamento é limitada;
- capacidade de carga/descarga também é limitada;
- excesso de caminhões pode formar fila física;
- tipos especializados de armazenamento podem ser avaliados depois, apenas se criarem gameplay suficiente.

Parâmetros que devem ser configuráveis durante desenvolvimento:

- capacidade total;
- quantidade de baias/pontos simultâneos;
- velocidade de carga e descarga;
- custo de construção;
- custo operacional.

Isso não significa necessariamente expor todas essas opções ao jogador.

---

## Exportação automática e contabilidade

**Status:** decidido para o escopo inicial.

Excedentes podem ser exportados automaticamente para reduzir microgerenciamento.

Requisito de UI/finanças:

- receita de exportação precisa aparecer separadamente;
- quantidade e tipo de mercadoria exportada devem ser consultáveis;
- custos logísticos relevantes não devem ficar escondidos;
- o jogador precisa conseguir entender se a cidade está ganhando dinheiro por produção interna ou dependendo de importações.

No futuro pode ser avaliado controle manual por categoria, limites mínimos de estoque ou políticas de exportação, mas isso não é necessário no primeiro escopo.


---

## Reserva de materiais e fila de obras

**Status:** decidido para o escopo inicial.

Ao confirmar uma obra:

1. o sistema verifica o estoque municipal disponível;
2. a quantidade existente é reservada imediatamente para aquela obra;
3. materiais reservados deixam de estar disponíveis para outras obras;
4. se faltar material, a obra aguarda a entrega física;
5. quando várias obras competem pelo mesmo recurso, a prioridade segue a ordem de criação.

### Por que FIFO é uma boa regra inicial

A ordem de criação (FIFO) é:

- determinística;
- fácil de explicar;
- simples de depurar;
- previsível para o jogador;
- suficiente para validar o sistema antes de adicionar controles extras.

### Possível evolução futura

Pode valer a pena permitir prioridade manual de obras críticas, por exemplo:

- hospital;
- bombeiros;
- água;
- energia;
- infraestrutura de emergência.

Isso fica fora do escopo atual até existir necessidade real observada no gameplay.


---

## Política de importação de materiais acionada por obra

**Status:** decidido.

Para materiais de construção, o fluxo inicial não usa reposição genérica de estoque.

A importação acontece quando:

- o jogador confirma uma obra;
- a obra exige materiais;
- a cidade não possui quantidade suficiente disponível localmente.

Nesse caso, a interface calcula e mostra:

- materiais locais usados;
- materiais faltantes;
- preço dos materiais importados;
- frete;
- custo monetário total da construção.

O frete não aparece como cobrança posterior separada: ele já compõe o preço mostrado antes da confirmação.

Mesmo pago antecipadamente, o material continua sujeito à logística física e a obra espera a entrega dos caminhões.


---

## Quem executa as obras

**Status:** Pátio Municipal de Obras, bootstrap externo e contratação externa por falta de capacidade local decididos.



### Decisão atual: Pátio Municipal de Obras

Foi escolhido um modelo municipal para o primeiro escopo:

- o jogador constrói fisicamente o Pátio Municipal de Obras;
- ele não existe no início da cidade;
- trabalhadores do Pátio são cidadãos reais e funcionários públicos;
- sua folha salarial aparece nas despesas da prefeitura;
- o Pátio tem capacidade operacional limitada;
- possui um pequeno estoque próprio de materiais;
- depósitos municipais dedicados continuam necessários para estoque em maior escala.

Construtoras privadas permanecem como possibilidade futura, não como requisito atual.


### Modelo escolhido para o escopo atual

O modelo inicial não usa uma construtora privada como executora principal das obras municipais.

Foi escolhido:

- **Pátio Municipal de Obras** como estrutura operacional municipal;
- trabalhadores públicos reais;
- capacidade limitada de execução;
- pequeno estoque próprio;
- equipe externa temporária apenas para o bootstrap antes do primeiro Pátio existir.

Construtoras privadas continuam como possibilidade futura e só devem voltar à discussão se criarem gameplay útil sem duplicar desnecessariamente o sistema municipal.


### Classificação econômica real

Construção não precisa ser encaixada artificialmente como indústria de manufatura ou comércio varejista. O NAICS trata **Construction (setor 23)** como setor econômico próprio. Ele inclui construção de edifícios, obras pesadas/engenharia e empreiteiros especializados. Empreiteiros gerais normalmente assumem a responsabilidade por um projeto inteiro e podem subcontratar partes do trabalho.

Fontes:
- U.S. Census Bureau — NAICS Sector 23, Construction: https://www.census.gov/naics/resources/archives/sect23.html
- U.S. Census Bureau — Construction sector profile: https://data.census.gov/profile/23_-_Construction

### Alternativas consideradas

Foram considerados modelos abstrato, construtora privada totalmente simulada e modelo híbrido.

A decisão atual é começar pelo **Pátio Municipal de Obras**, por manter trabalhadores, capacidade e custos reais sem exigir desde já uma economia completa de empreiteiras privadas.

Máquinas e equipamentos de obra ainda precisam de pesquisa específica para decidir o que vale representar fisicamente e o que pode permanecer agregado.


---

## Cancelamento versus demolição

**Status:** parcialmente decidido.

### Cancelamento de obra inacabada

Decidido:

- materiais reservados e ainda não entregues são liberados para outras obras;
- materiais que já chegaram ao canteiro foram consumidos e não retornam ao depósito.

Ainda em aberto:

- quanto dinheiro, se algum, é devolvido ao cancelar;
- como tratar trabalho já executado;
- como tratar apenas o dinheiro e o trabalho já executado, pois materiais entregues já são considerados consumidos.

### Demolição de construção concluída

Decidido:

- não recupera o dinheiro original da construção;
- não recupera os materiais originalmente consumidos;
- a demolição continua tendo seu próprio custo configurável conforme já definido.

Não há sistema de salvage/reciclagem de material no escopo atual.


---

## Hipótese de diferenciação: cidade construída por cadeias materiais

**Status:** direção prioritária de produto; hipótese de diferenciação ainda precisa ser validada em gameplay.

A economia material passa a ser uma linha central de investigação do IndexCities.

A intenção é fugir do padrão em que construir é principalmente clicar em algo e pagar dinheiro. No IndexCities, a cidade deve crescer porque consegue **obter, produzir, transportar e aplicar materiais físicos**.

### Loop central a explorar

necessidade de construir  
→ calcular dinheiro + materiais  
→ usar estoque local e/ou importar faltantes  
→ transportar fisicamente  
→ executar a obra com trabalhadores  
→ nova infraestrutura/empresa entra em operação  
→ gera empregos, produção, consumo e novos fluxos  
→ produção local reduz dependência externa e cria novas possibilidades de expansão

### Por que isso pode ser um diferencial

Se funcionar bem, o material deixa de ser apenas um custo adicional e passa a conectar:

- construção;
- indústria;
- agricultura quando aplicável;
- importação/exportação;
- logística;
- depósitos;
- trânsito de carga;
- empregos;
- finanças municipais;
- crescimento da cidade.

A hipótese é que a cidade se torne legível como uma rede produtiva real, e não apenas como uma coleção de edifícios comprados com dinheiro.

### Regra de pesquisa daqui em diante

Ao avaliar uma nova construção ou infraestrutura, investigar:

- quais materiais ela consome;
- de onde esses materiais podem vir;
- como podem ser produzidos localmente;
- como são armazenados;
- como são transportados;
- quais empregos a cadeia cria;
- quais gargalos e consequências a falta do material produz;
- se a granularidade acrescenta gameplay ou apenas microgerenciamento.

### Validação necessária

Essa direção deve ser prototipada antes de ser tratada como o diferencial definitivo do jogo.

Precisamos verificar se:

- a logística é compreensível;
- esperar material cria decisões e não apenas atraso;
- produzir localmente é recompensador;
- importação continua útil sem ser sempre a melhor opção;
- o número de materiais permanece administrável;
- o jogador entende claramente por que uma obra está parada e quanto custa depender do exterior.


---

## Transparência das finanças municipais

**Status:** decidido.

O orçamento da prefeitura deve deixar explícito de onde o dinheiro entra e para onde sai.

No mínimo, a interface financeira precisa separar categorias como:

**Entradas**
- impostos;
- exportações;
- outras receitas municipais que forem adicionadas futuramente.

**Saídas**
- salários de trabalhadores públicos;
- importação de materiais;
- frete;
- construção;
- operação/manutenção de serviços;
- pagamento de dívida quando aplicável.

A folha de pagamento dos funcionários públicos não deve ficar escondida dentro de um custo genérico de serviço ou de obra.

Isso também evita dupla cobrança: se um trabalhador do Pátio já recebe salário da prefeitura, esse salário não deve ser cobrado novamente como se fosse um custo separado da mesma mão de obra na obra.


---

## Estoque do Pátio versus depósito dedicado

**Status:** direção decidida; capacidades ainda precisam de calibração.

O Pátio Municipal de Obras terá um estoque pequeno para apoiar operações correntes.

A intenção é criar dois níveis:

- **Pátio Municipal de Obras:** estoque pequeno e conveniente, ligado diretamente à execução das obras;
- **Depósito municipal dedicado:** capacidade muito maior, voltado a estoque em escala da cidade.

Questões de calibração:

- capacidade relativa entre os dois;
- se o Pátio recebe automaticamente materiais reservados para obras;
- número de pontos de carga/descarga;
- quando o estoque do Pátio é usado antes do depósito;
- se o Pátio pode receber diretamente uma importação destinada a uma obra próxima.

Esses detalhes precisam ser testados sem criar movimentação logística redundante.


---

## Bootstrap do primeiro Pátio

**Status:** decidido.

O mapa continua começando sem edifícios municipais.

Uma equipe externa temporária:

- entra pela conexão externa;
- possui trabalhadores externos, não residentes;
- executa o primeiro Pátio Municipal de Obras;
- pode executar também a infraestrutura mínima necessária para viabilizar esse Pátio;
- usa materiais pagos/importados e entregues fisicamente segundo as mesmas regras de construção já definidas.

Assim que o Pátio entra em operação, a equipe externa deixa de ser necessária para o fluxo municipal normal.

### Coerência com as demais regras

Esse bootstrap preserva:

- cidade inicial vazia;
- construção não instantânea;
- materiais físicos;
- logística pela conexão externa;
- mão de obra real;
- ausência de um edifício inicial gratuito.

A equipe externa é uma exceção apenas de **origem da mão de obra**, não uma exceção às regras de materiais, dinheiro ou transporte.


---

## Capacidade do Pátio e contratação externa por obra

**Status:** decidido em nível de comportamento; números ainda precisam de calibração.

A capacidade do Pátio Municipal de Obras não funciona como um limite duro que bloqueia novas construções.

Fluxo:

1. a obra é criada;
2. o sistema verifica se existe equipe/trabalhadores públicos disponíveis;
3. se houver, a obra usa capacidade municipal local;
4. se não houver, uma equipe externa temporária é contratada/importada;
5. essa contratação aumenta o custo monetário da obra.

Isso cria um trade-off direto:

- manter mais trabalhadores públicos aumenta a capacidade local, mas eleva a folha recorrente da prefeitura;
- depender de equipes externas evita fila de obras, mas encarece construções individuais.

A regra precisa aparecer com clareza na interface de custo da obra, separando quando possível:

- custo base;
- materiais;
- frete/importação de materiais;
- custo adicional de equipe externa.

A equipe externa pode usar a mesma conexão externa já definida para o bootstrap inicial. Seus trabalhadores não precisam virar residentes da cidade.

### Consequência para o Pátio

O Pátio continua relevante mesmo sem bloquear obras:

- reduz custo de depender de mão de obra externa;
- gera empregos públicos locais;
- transforma capacidade de construção em decisão de orçamento;
- pode manter pequeno estoque próprio;
- fornece a base operacional municipal da construção.

O número exato de trabalhadores por equipe, produtividade e quantidade de equipes deve ser calibrado posteriormente.


---

## Consumo instantâneo de materiais ao chegar ao canteiro

**Status:** decidido.

Para evitar microgestão desnecessária, o jogo não precisa mostrar materiais sendo consumidos gradualmente durante a obra.

Quando a carga necessária chega ao canteiro:

- os materiais daquela entrega são consumidos imediatamente pela obra;
- a interface pode considerar aquela parcela como incorporada à construção;
- não existe necessidade de um estoque físico persistente no canteiro durante toda a execução.

### Efeito sobre cancelamento

Essa decisão substitui a ideia anterior de devolver material já entregue:

- material apenas reservado, ainda não entregue, pode ser liberado;
- material que chegou ao canteiro já foi consumido e é perdido se a obra for cancelada depois.

Isso mantém uma regra simples e coerente com o modelo de consumo imediato.


---

## Revisão 2 — base material realista da cidade

**Status:** pesquisa; ainda não é requisito.

A lista anterior ficou excessivamente centrada em construção e usou o termo técnico "agregados", que não é claro como linguagem de gameplay. A revisão separa três papéis: bens essenciais consumidos pela cidade, materiais finais usados em obras e insumos industriais.

### Evidência

A FEMA trata como funções críticas de uma comunidade: alimentos/abrigo, saúde e cadeia de suprimentos médicos, energia/combustível, transporte e água/esgoto. A OMS reforça a necessidade de disponibilidade contínua de medicamentos essenciais.

As contas de fluxo material da Eurostat separam biomassa, metais, minerais não metálicos e combustíveis fósseis. Em 2025, minerais não metálicos responderam por cerca de 53% do consumo material doméstico da UE, biomassa 26%, fósseis 16% e minérios metálicos 5%. Esses percentuais são evidência de importância física, não valores de balanceamento para o jogo.

O USDA descreve a cadeia alimentar ligando produção agrícola, processamento, atacado, varejo/restaurantes e consumo, com armazenamento e transporte entre etapas.

O USGS e a FHWA mostram que areia, cascalho e pedra britada são matérias-primas básicas de enorme volume para edifícios, estradas, pontes, concreto, asfalto, base viária e drenagem.

Fontes principais:
- FEMA Community Lifelines
- FEMA Supply Chain Resilience Guide
- WHO Essential medicines
- Eurostat Material Flow Accounts
- USDA ERS Retailing & Wholesaling
- USGS Building America: Construction Materials
- FHWA Aggregates
- FHWA Freight Analysis Framework

### Nomenclatura

"Agregados" é tecnicamente correto, mas não é recomendado como nome exibido ao jogador.

Se areia e brita permanecerem agrupadas, usar **Areia e brita**. Internamente podem continuar sendo tratadas como uma família mineral.

### Proposta revisada

**Bens essenciais da cidade**
- **Alimentos** — consumo dos cidadãos; importação e produção local.
- **Combustível** — veículos, logística e serviços; já faz parte do produto decidido.
- **Suprimentos médicos** — medicamentos e consumíveis de saúde em uma categoria agregada; forte candidato, mas pode entrar após o primeiro POC se o escopo precisar ser menor.

**Materiais consumidos diretamente por obras**
- **Areia e brita** — base de vias, drenagem e insumo de concreto/asfalto.
- **Concreto** — material final de obra; cadeia candidata: cimento + areia/brita + água.
- **Aço** — uma categoria inicialmente.
- **Madeira** — uma categoria inicialmente.
- **Asfalto** — principalmente vias e superfícies pavimentadas.

**Insumos industriais que só precisam aparecer quando a cadeia local existir**
- cimento;
- produtos agrícolas/culturas;
- madeira em tora;
- ligante asfáltico/bitume;
- pedra bruta.

Isso evita começar simultaneamente com clínquer, minério, tijolo, vidro, gesso, cobre, alumínio, plásticos e dezenas de outros produtos.

### Benchmark do gênero

Workers & Resources: Soviet Republic já usa construção física por materiais, importação e produção local. Seu conjunto inclui gravel, concrete, asphalt, steel, prefab panels, bricks, boards e componentes, além de comida e combustível.

Portanto, **construção por materiais não é inédita por si só**. O diferencial potencial do IndexCities está em integrar esse núcleo com cidadãos persistentes, emprego real, Pátio Municipal de Obras, orçamento/salários públicos, mão de obra externa quando falta capacidade, importação física e economia familiar/empresarial.

Fonte: Workers & Resources: Soviet Republic Official Wiki — Construction materials, Construction, Resources e Food.

### Direção recomendada para o primeiro POC

Testar primeiro:
- alimentos;
- combustível;
- areia e brita;
- concreto;
- aço;
- madeira;
- asfalto.

Suprimentos médicos entram logo depois ou já no mesmo POC se a simulação de saúde for implementada nessa fase.

Cimento deve existir como insumo da concreteira, não necessariamente como material consumido diretamente pelas obras. O mesmo princípio vale para culturas, toras, bitume e pedra bruta.

Ainda precisa de aprovação antes de mover qualquer lista para a SPEC.
