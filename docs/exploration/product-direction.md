# IndexCities — Direção de produto e princípios de simulação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa, com vários princípios já promovidos para SPEC/AGENTS — não é fonte de verdade.
>
> Reúne a direção geral do jogo, questões de identidade/loop, princípios de profundidade sistêmica, legibilidade e microgerenciamento. Quando um trecho já foi promovido para a SPEC ou para AGENTS, ele permanece aqui apenas como histórico e justificativa.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Ponto de partida do produto

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Direção atual do jogo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Princípios desejados para a simulação

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Escala física e forma de construir

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Pesquisa prioritária: desafios opcionais e longevidade do sandbox

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — **a direção de sandbox livre com desafios opcionais foi aprovada em 2026-10-08 e registrada na SPEC**; a análise e as possibilidades abaixo permanecem PENDENTES de revisão humana, sem virar requisitos.

**Direção aprovada na [SPEC](../SPEC.md):** o jogo permanece livre para construir e desenvolver a cidade sem objetivos ou vitórias obrigatórias. Desafios são opcionais; não bloqueiam a experiência central nem autorizam por si só sistemas de missão, metas, recompensas ou desbloqueios.

**Em aberto para pesquisa:** quais desafios realmente ampliam decisões interessantes no médio/longo prazo, como comunicá-los sem tarefas repetitivas e se precisam de qualquer recompensa além dos efeitos emergentes da cidade. **Não** tratar uma população-alvo ("chegar a X habitantes") como objetivo decidido.

A pesquisa futura pode confrontar a opção C aprovada com modelos de outros jogos para **extrair aprendizados, não reabrir silenciosamente a direção nem adotar suas mecânicas**:

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

Critério principal: identificar desafios opcionais **distintivos, relevantes para decisões sistêmicas e economicamente coerentes**, sem destruir a liberdade de city builder nem produzir uma lista infinita de tarefas. Exemplos de condições urbanas como desemprego, escassez e congestionamento **são hipóteses de exploração**, não desafios aprovados.

O documento [exploration/genre-benchmark.md](genre-benchmark.md) já contém evidências sobre endgame e progressão e deve ser uma das fontes dessa pesquisa, mas não substitui uma rodada dedicada.

---

---

## Princípio geral: gameplay antes de microgerenciamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido como critério transversal de design; também registrado em AGENTS e SPEC.

Toda decisão de sistema deve ser avaliada em duas dimensões:

1. **qual decisão/consequência interessante o detalhe cria?**
2. **quanto trabalho manual repetitivo ele exige do jogador?**

O IndexCities deve buscar profundidade sistêmica sem confundir profundidade com quantidade de cliques.

Manter quando gera gameplay:
- logística física;
- escassez;
- capacidade;
- distâncias;
- congestionamento;
- emprego;
- finanças;
- produção;
- falhas e consequências;
- escolhas com trade-offs.

Abstrair ou automatizar quando tende a virar rotina:
- contabilidade de propriedade sem decisão útil;
- transferência manual entre estoques equivalentes;
- autorizações repetitivas;
- configuração individual que o sistema pode resolver de forma previsível;
- tarefas administrativas que não mudam estratégia.

A interface pode ser mais simples que a simulação. O sistema pode saber exatamente onde cada carga está, quem a produziu e como ela se move, enquanto o jogador recebe uma visão agregada suficiente para decidir.

Esse critério deve ser reaplicado a transporte, serviços, empresas, cidadãos, cadeias produtivas, construção, finanças e demais sistemas à medida que forem definidos.


---

---

## Revisão geral: profundidade, legibilidade e dois níveis de leitura

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** princípio aprovado e revisão transversal realizada.

### Objetivo

O IndexCities deve permitir duas formas de interação com **a mesma simulação**:

**Leitura imediata**
- sinais visuais claros;
- ícones consistentes;
- causa curta;
- próxima ação compreensível;
- pouca leitura obrigatória.

**Leitura profunda**
- estatísticas;
- históricos;
- decomposição de fatores;
- fluxos físicos/econômicos;
- diagnóstico causal;
- espaço para otimização.

Não são dois jogos e não exigem, neste momento, um "modo infantil" e um "modo avançado". É profundidade progressiva: a superfície resolve orientação; o detalhe resolve investigação.

### Referências externas

A revisão Economy 2.0 de Cities: Skylines II declarou explicitamente que a simulação econômica anterior não era transparente o suficiente e oferecia pouco controle. O objetivo da revisão foi tornar sistemas mais diretos e responsivos, preservando a possibilidade de novos jogadores terem sucesso sem acompanhar cada fluxo e permitindo que jogadores experientes ganhem vantagem por otimização.

As Xbox Accessibility Guidelines reforçam princípios compatíveis:
- navegação consistente e intuitiva ajuda jogadores novos;
- telas e elementos precisam fornecer contexto suficiente;
- objetivos e próximos passos devem ser compreensíveis;
- símbolos importantes se beneficiam de rótulos/contexto adicional e de mais de um canal de comunicação.

Fontes:
- Paradox — Economy 2.0 Part 1: https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-one
- Microsoft XAG 109 — Objective clarity: https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/109
- Microsoft XAG 112 — UI navigation: https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112
- Microsoft XAG 114 — UI context: https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/114
- Microsoft XAG 103 — Additional channels for visual/audio cues: https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/103

### Sistemas atualmente bem alinhados

**Materiais e construção**
- "Disponível na cidade" simplifica leitura;
- cargas continuam físicas;
- material reservado/em trânsito/importado pode ser inspecionado;
- importação e frete são custos explícitos.

**Finanças municipais**
- entradas e saídas são separadas;
- salários públicos aparecem explicitamente;
- evita custos ocultos.

**Logística e trânsito**
- caminhões, filas, capacidade, estacionamento e congestionamento existem fisicamente;
- as consequências tendem a ser observáveis no mapa.

**Serviços públicos**
- funcionários e capacidade são reais;
- falta de vaga/equipe pode ser ligada a entidades concretas.

### Sistemas com risco de caixa-preta e que exigem regra de explicação antes da implementação

**Economia das empresas**
- o modelo financeiro principal já está definido: cada empresa possui caixa real e auditável;
- empresa pode falir quando os fluxos concretos não sustentam a operação;
- não usar um número abstrato de "saúde financeira" que cai sem causa rastreável;
- toda deterioração deve decompor receita, salários, insumos, frete, impostos e outras causas reais;
- capital inicial de empresa externa sai da Reserva Global, sem criação de moeda.

**Demanda**
- já está decidido que a cidade expõe demanda;
- a fórmula ainda não está definida;
- a UI deve mostrar os principais fatores positivos/negativos, não apenas uma barra misteriosa.

**Preço de aluguel/venda**
- oferta e demanda influenciam preços;
- antes da implementação, definir fatores e uma explicação legível de por que determinada área ficou cara/barata.

**Migração e saída da cidade**
- emprego, moradia e qualidade de vida influenciam decisões;
- evitar um "score de felicidade" opaco;
- quando alguém sai ou entra, os principais motivos devem ser diagnosticáveis.

**Turismo e atratividade**
- parques, comércio e atrações aumentam demanda turística;
- precisa de fatores visíveis antes de virar fórmula.

**Poluição e mudança residencial**
- cidadãos podem abandonar áreas poluídas;
- o limiar/efeito precisa ser compreensível e observável.

**Escolha de serviços**
- o modelo de raio versus custo de viagem continua aberto;
- qualquer solução precisa mostrar por que um cidadão escolheu/não conseguiu usar determinado serviço.

**Exportação automática**
- automação reduz microgerenciamento, mas "excedente elegível" precisa de regra clara;
- o jogador deve conseguir entender o que foi exportado e por quê, evitando exportar algo aparentemente necessário sem explicação.

**Importações automáticas de alimentos**
- manter automação;
- quando ela falhar ou ficar cara, mostrar causa: preço, tráfego, distância, capacidade de descarga, falta de caixa ou outra causa real.

### Regra de diagnóstico recomendada

Todo problema relevante deveria seguir, conceitualmente:

**sinal → causa resumida → ação possível → detalhes opcionais**

Exemplo:

ícone de alimento
→ "Mercado sem estoque"
→ "Entrega atrasada"
→ ação: melhorar acesso / aumentar produção / aguardar importação
→ detalhes: consumo, estoque, caminhão, rota, tempo, fornecedor, custo.

A explicação não pode ser uma dica genérica inventada pela UI. Ela deve apontar para o gargalo real da simulação.

### Consequência para testes e calibração

Para evitar sistemas impossíveis de nivelar:

- entradas e saídas devem ser mensuráveis;
- parâmetros de balanceamento devem ser explícitos;
- mudanças precisam produzir resultados reproduzíveis quando possível;
- entidades/sistemas importantes devem permitir inspeção de estado durante desenvolvimento;
- falhas devem carregar motivos/códigos causais suficientes para diagnóstico;
- métricas agregadas devem ser derivadas de dados reais, não funcionar como variáveis mágicas independentes.

Isso permite corrigir balanceamento sem "chutar" o motivo de um comportamento emergente.


---

---

## Princípio reforçado: evitar abstração sem causa concreta

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido como critério transversal; também registrado em AGENTS e SPEC.

O objetivo não é eliminar toda abstração. Isso seria incompatível com decisões já úteis, como interiores abstratos, UI agregada e sistemas de água/energia por capacidade.

O objetivo é evitar ao máximo **abstrações que substituem entidades e fluxos econômicos reais sem necessidade**.

Preferir:
- dinheiro com origem e destino;
- proprietário real quando propriedade gera consequência;
- materiais em local físico;
- empresa/família identificável quando recebe ou paga;
- demanda derivada de clientes, capacidade, acesso e concorrência;
- migração ligada a famílias reais.

Aceitar abstração quando:
- evita microgerenciamento sem apagar consequências;
- reduz custo técnico sem mudar o resultado econômico/físico relevante;
- é derivada de estado concreto e pode ser decomposta;
- não cria dinheiro, recurso, proprietário, demanda ou comportamento mágico.

Esse critério deve ser reaplicado em todas as próximas decisões.


---
