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
