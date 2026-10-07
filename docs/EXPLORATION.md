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
