# IndexCities — Construção, materiais e logística

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa; muitas regras já foram promovidas para a SPEC, enquanto calibração e diferenciação de gameplay continuam abertas — não é fonte de verdade.
>
> Concentra a evolução do modelo de construção física, materiais, estoques, importação/exportação, reservas, execução de obras e logística. Alternativas superadas permanecem identificadas para preservar por que o modelo atual foi escolhido.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Asfalto e material por largura de rua — reflexão de 2026-10-09

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável perguntou se **construir asfalto deve gastar material físico**, e posteriormente **aprovou dois perfis iniciais concretos para ruas urbanas: 1+1 e 2+2 faixas**, ao confirmar a sugestão da IA. Não foram decididos consumo por metro, largura em metros ou perfis adicionais.

**Já é produto oficial na SPEC:** Asfalto é uma das oito categorias físicas iniciais de recursos, com destino em vias/superfícies pavimentadas. Areia e brita servem à base viária. **Toda obra, incluindo rua, paga dinheiro, utiliza materiais e exige trabalho e logística reais**. A pergunta do responsável portanto **reforça um fundamento existente**; não inventar sistema de pavimentação abstrata que contorne estoque e caminhões.

**Requisito de produto já confirmado pela SPEC:** ao construir rua pavimentada, a quantidade de **Asfalto e Areia e brita consumida segue o comprimento e a largura reais da obra**, inclusive a escolha do perfil 1+1 ou 2+2, com entrega de material físico e custo real; não exigir que o jogador dose os insumos por trecho. **Quantidades e proporções exatas ficam para calibração**, sem criar uma segunda etapa manual de pavimentação. O custo mostrado antes da confirmação deve refletir os recursos a reservar/entregar e a faixa escolhida, sem duplicar cobrança do mesmo material ou permitir obra materializar pavimento sem carga real. **Os perfis 1+1 (uma faixa por sentido) e 2+2 (duas por sentido) foram aprovados expressamente**; não continuam hipóteses. Novos perfis só mediante decisão futura. Avaliar largura de calçadas, estacionamento, conexões e eventual necessidade de faixas adicionais com gameplay/medição, não por catálogo amplo a priori.

Não presumir neste estágio **ruas de terra, pavimentação como uma segunda obra ou alargamento de via pronta**, que contrariam ou extrapolam o escopo atual de rua urbana/rodovia e construir/demolir.

---

## Materiais de construção, importação e estoque

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** pesquisa histórica parcialmente superada pela seção **Revisão 2 — base material realista da cidade**. As evidências e cadeias abaixo continuam úteis, mas a lista inicial oficial está agora na SPEC.

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

### Tier 1 da pesquisa histórica (não define POC obrigatória)

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
- uma única categoria **Madeira** na implementação inicial conforme a SPEC;
- não separar tábuas, vigas, compensado, CLT etc.;
- decidir separadamente se árvores comuns do mapa podem virar matéria-prima ou se haverá silvicultura própria.

#### 6. Asfalto

Cadeia simplificada:

agregados + ligante asfáltico → usina de asfalto → caminhão → obra viária

A FHWA informa que misturas asfálticas são majoritariamente agregados, ligados por material asfáltico derivado do processamento de petróleo.

Sinergia importante:
- o ligante pode futuramente se conectar à cadeia de combustível/refino já planejada;
- agregados compartilham a mesma cadeia usada por concreto.

### Tier 2 da pesquisa histórica (não define fase posterior obrigatória)

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
- muito usado em interiores, mas com pouca consequência urbana/logística distinta no cenário econômico originalmente estudado.
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

Ordem histórica sugerida para investigar cadeias (não define fases obrigatórias da entrega):

1. **concreteira local** usando cimento e agregados importados;
2. **usina de asfalto local** usando agregados + ligante importados;
3. **serraria** quando a origem dos logs estiver definida;
4. **produção local de agregados** depois de decidir recursos minerais no mapa;
5. **fábrica de cimento** depois de decidir calcário/argila e indústria pesada;
6. **produção de aço** mais tarde, por ser uma cadeia industrial muito mais pesada.

Essa ordem era hipótese de sequenciamento de investigação técnica, **não cronograma aprovado de POCs separadas nem autorização para retirar recursos aprovados na SPEC**. Serve para analisar o loop produção → emprego → estoque → caminhão → obra.

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

---

## Bens e materiais básicos para uma cidade funcionar

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

Esta foi uma **sugestão histórica de teste econômico reduzido**, não fase de produto: alimentos, medicamentos/suprimentos médicos, combustível, materiais de construção e bens gerais. **A lista oficial da entrega integrada são as oito categorias da SPEC.** Aprofundar cadeias somente se trouxerem consequências interessantes, sem cortes automáticos de escopo.

---

---

## Estado inicial, depósitos e abastecimento de obras

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Bootstrap logístico da primeira construção

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** resolvido.

Não existe depósito gratuito nem estoque inicial artificial.

Quando uma obra exige material inexistente localmente:

- a interface mostra o custo adicional de importação;
- materiais podem ser entregues fisicamente diretamente ao canteiro;
- o pagamento não teletransporta a carga.

Para o problema de mão de obra inicial, uma equipe externa temporária entra pela conexão externa e pode executar o primeiro Pátio Municipal de Obras e a infraestrutura mínima necessária. Depois que o Pátio entra em operação, as obras municipais usam o fluxo normal de trabalhadores públicos locais.


---

---

## Compra automática de material importado ao construir

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** comportamento principal decidido; detalhes de UI e balanceamento precisam ser verificados/calibrados no jogo integrado.

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

---

## Regra de custo de construção e transparência de importação

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Depósito municipal inicial

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Exportação automática e contabilidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido para o escopo inicial.

Excedentes podem ser exportados automaticamente para reduzir microgerenciamento.

Requisito de UI/finanças:

- receita de exportação precisa aparecer separadamente;
- o pagamento externo sai da **Reserva Global** e vai para o agente que vende a mercadoria;
- quantidade e tipo de mercadoria exportada devem ser consultáveis;
- custos logísticos relevantes não devem ficar escondidos;
- o jogador precisa conseguir entender se a cidade está ganhando dinheiro por produção interna ou dependendo de importações.

No futuro pode ser avaliado controle manual por categoria, limites mínimos de estoque ou políticas de exportação, mas isso não é necessário no primeiro escopo.


---

---

## Reserva de materiais e fila de obras

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Política de importação de materiais acionada por obra

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

Se a importação exigir pagamento antecipado, ele ocorre como transferência real à Reserva Global e não é automaticamente desfeito se a obra for cancelada. O material continua sujeito à logística física e a obra espera a entrega dos caminhões; **a reserva inicial do orçamento no Caixa não equivale a esse pagamento**.


---

---

## Quem executa as obras

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Cancelamento versus demolição

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** materiais e tratamento monetário do cancelamento decididos na SPEC; logística de pedidos em trânsito, momento contratual de pagamento e balanceamento continuam sendo questões técnicas de integração.

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável aprovou em 2026-10-08 reservar o custo no Caixa, pagar contrapartes reais conforme despesas e liberar o saldo não pago. Detalhes de transações/roteamento ainda dependem de validação.

### Cancelamento de obra inacabada

Decidido:
- materiais reservados e ainda não entregues são liberados para outras obras;
- materiais entregues ao canteiro são consumidos e não retornam ao depósito;
- o custo monetário previsto da obra fica comprometido no Caixa da Cidade na confirmação, **sem transferência monetária**;
- pagamentos reais deixam o Caixa e reduzem a quantia comprometida quando efetivamente devidos, inclusive pagamentos antecipados de importação quando aplicável;
- cancelar libera automaticamente a **parcela não paga** do orçamento comprometido, sem estornar pagamentos reais nem criar moeda;
- o jogador vê o valor que será liberado e os custos já incorridos antes de confirmar cancelamento;
- a Reserva Global só participa como recebedor real de fluxos externos (ou em seus outros papéis oficiais), nunca como conta de retenção de obras.

A folha salarial periódica do Pátio permanece gasto municipal comum; não duplicar a remuneração de seus trabalhadores na contabilização por obra. Diferenciar pagamento antecipado de entrega/consumo: uma carga paga e ainda em trânsito não implica que o dinheiro seja recuperável, mas sua localização física deve continuar rastreável e sem duplicação.

**Detalhes ainda técnicos:** como corrigir previsões de custo durante a execução; tratamento físico de pedido já despachado quando a obra é cancelada; calendário de liquidação de cada contraparte. Validar sem impor créditos fictícios, parcelas de custo duplicadas ou microgerenciamento.

### Mover obra inacabada — decisão oficial (opção C, 2026-10-08)

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável escolheu explicitamente o reposicionamento assistido com preservação das consequências econômicas. Explicações técnicas abaixo ainda não foram revisadas integralmente.

A [SPEC](../SPEC.md) passa a permitir que o jogador **mova uma obra incompleta em uma ação**, em vez de cancelar e recolocar manualmente. A operação segue economicamente o cancelamento da localização anterior e a confirmação de uma nova obra, com prévia de custos e perdas antes da ação. Antes de gastos ou entregas, não existe penalidade extra. Depois, **pagamentos reais não são revertidos, e materiais entregues já são considerados consumidos**; reservas ainda não consumidas são liberadas/recalculadas. Progresso da estrutura física não é teleportado para o novo local. Os caminhões e materiais eventualmente em trânsito conservam existência/rastreabilidade; escolher se devem concluir a entrega anterior, retornar ou seguir nova rota depende do estado logístico efetivo, sem duplicação nem reembolso artificial.

**Limites para validação técnica:** recálculo do preço quando muda a distância; atomicidade da troca de reservas para não comprometer o Caixa duas vezes; entrega já despachada/paga; custo de trabalho local já remunerado; feedback claro da prévia. A realocação de **construções concluídas** não está aprovada por essa decisão.

### Risco de espera na realocação: obra total versus paralisação (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável confirmou que a paralisação não é obrigatória durante Realocar e que gameplay e microgerenciamento devem ser avaliados globalmente; a decisão consta na SPEC. Tempos específicos e novas regras salariais permanecem pendentes.

O tempo de preparar/construir o destino não precisa coincidir com o tempo sem operação do endereço antigo. Quando as condições espaciais permitirem, a opção C já deixa a atividade antiga funcionando durante a construção real do substituto; o período de transição final pode ser curto se materiais, logística e operador efetivamente estiverem prontos. Em casos de demolição necessária antes da conclusão, a interrupção pode se estender, sem inventar prontidão ou material instantâneo. Recomenda-se **medir separadamente** duração da obra, duração da paralisação, segundos reais de espera perceptível para o jogador e número de ações manuais, em vez de adotar prazo uniforme de mudança. As faixas de duração propostas na pesquisa de ritmo de construção ao fim deste arquivo **não são requisitos** e referem-se à construção ativa, não à paralisação empresarial.

**Atenção ao custo de simulação:** antes de criar mecânicas de folha salarial e retenção seletiva apenas para a realocação, confrontar a duração real de paralisação com regras já existentes de salários periódicos, emprego, caixa e falência. O usuário não aprovou nenhuma alternativa de manutenção de pessoal para o período parado. Estoques, equipamentos e trabalhadores continuam físicos; não justificar teletransporte como atalho para atingir um prazo inventado.

### Realocação de estabelecimento concluído — continuidade condicionada aprovada (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou a **opção C** de continuidade operacional condicionada durante a realocação; regras finas de compatibilidade entre duas obras e sequência de mudança continuam para calibração; a recuperação e eventual perda do estoque real agora estão definidas na SPEC.

Conforme a [SPEC](../SPEC.md), ao acionar **Realocar** para comércio, indústria ou serviço municipal, **a construção antiga pode continuar operando** enquanto sua estrutura, acesso e necessidade espacial da intervenção permitirem. A construção substituta não surge pronta: exige materiais, equipe, transporte e recursos financeiros reais. **Se for necessário retirar o prédio antigo antes de o novo operar, o estabelecimento para naquele endereço**, sem impedir indefinidamente a intervenção. **A ação direta Demolir continua distinta:** a execução física da demolição permanece instantânea quando autorizada e não herda a tentativa de continuidade da ferramenta Realocar. Regras de desocupação residencial seguem independentes.

O ativo privado ainda operando pode já ter sido **indenizado na confirmação**, mas não pode ser revendido ou indenizado novamente. A operação temporária não cria estoque duplicado, recebimentos fictícios nem movimento gratuito para o novo endereço. **Decisão C posterior aprovada na SPEC (2026-10-08):** se a empresa original comprar o prédio substituto, mantém a mesma identidade econômica, caixa e obrigações, com vínculos de trabalho válidos sujeitos à reavaliação individual na mudança de endereço; serviços municipais preservam a instituição. Mercadorias mantêm localização e titularidade e a transferência viável exige transporte físico real já existente, sem duplicar capacidade/estoque nem exigir ordens manuais do jogador. **Recuperação de estoque aprovada na SPEC (opção C, 2026-10-08):** preservar automaticamente as quantidades que possam ser transferidas fisicamente ou vendidas/exportadas por fluxos econômicos e logísticos reais antes da retirada; o saldo que permanecer sem saída viável quando a estrutura precisar ser retirada é perdido sem receita ou indenização fictícia. Não criar armazém, caminhão ou comprador garantido; alertar sobre risco e perdas em agregado. **Ainda em aberto:** sequência fina de entregas, tratamento de contratos e salários em paralisações prolongadas e eventual modelagem de equipamentos (sem inventário próprio aprovado); **sem teletransporte, garantia de sobrevivência ou pausa financeira automática**. Não adicionar um novo subsistema contínuo de monitoramento; preferir mudanças de estágio e checagens físicas pertinentes. A prévia de intervenção deve comunicar risco de interrupção e impacto agregado para gameplay.

### Demolição de construção concluída

Decidido:

- não recupera o dinheiro original da construção;
- não recupera os materiais originalmente consumidos;
- a demolição continua tendo seu próprio custo configurável conforme já definido.

Não há sistema de salvage/reciclagem de material no escopo atual.


---

---

## Hipótese de diferenciação: cidade construída por cadeias materiais

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

Essa direção deve ser **validada no gameplay do jogo integrado** antes de ser tratada como diferencial definitivo. Experimentos técnicos isolados podem ajudar, mas não são POCs obrigatórias.

Precisamos verificar se:

- a logística é compreensível;
- esperar material cria decisões e não apenas atraso;
- produzir localmente é recompensador;
- importação continua útil sem ser sempre a melhor opção;
- o número de materiais permanece administrável;
- o jogador entende claramente por que uma obra está parada e quanto custa depender do exterior.


---

---

## Estoque do Pátio versus depósito dedicado

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Bootstrap do primeiro Pátio

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Capacidade do Pátio e contratação externa por obra

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Consumo instantâneo de materiais ao chegar ao canteiro

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


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

---

## Revisão 2 — base material realista da cidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** aprovada. A base inicial foi promovida para a SPEC.

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
- **Suprimentos médicos** — medicamentos e consumíveis de saúde agregados; o adiamento histórico por causa de uma POC menor está **superado**: já pertencem às oito categorias oficiais da SPEC.

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

### Decisão aprovada

A base inicial oficial agora é:

- alimentos;
- combustível;
- suprimentos médicos;
- areia e brita;
- concreto;
- aço;
- madeira;
- asfalto.

Cimento permanece como insumo da concreteira, não como material final obrigatório de toda obra. O mesmo princípio vale para culturas/produtos agrícolas, madeira em tora, ligante asfáltico/bitume e pedra bruta: entram quando suas cadeias produtivas forem implementadas.

Essa lista foi movida para a SPEC.


---

### Checagem de coerência desta decisão

A base de oito recursos é compatível com as decisões já existentes:

- **alimentos** já eram necessidade real dos cidadãos e agora ganham uma categoria física inicial;
- **combustível** já fazia parte da cadeia econômica e dos veículos;
- **suprimentos médicos** complementam o sistema de saúde real sem introduzir SKUs individuais;
- **areia e brita, concreto, aço, madeira e asfalto** alimentam o pilar de construção material;
- água e eletricidade continuam como sistemas de capacidade, evitando duplicidade conceitual;
- a regra de categorias agregadas continua respeitada: nenhum desses recursos exige SKU individual no escopo inicial.

Não foi identificado conflito com as regras atuais de importação, armazenamento, Pátio Municipal de Obras ou logística física.


---

---

## Materiais no modelo híbrido público/privado

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — a proposta antiga foi superada; a atualização abaixo segue a decisão atual da SPEC.

**Status:** modelo antigo de financiamento privado da obra superado.

A separação rígida “obra pública paga pela cidade / obra privada paga por investidor” não vale mais no escopo atual.

### Regra atual

- toda obra pública ou privada ordenada pelo jogador é financiada pelo **Caixa da Cidade**;
- materiais continuam físicos e precisam chegar ao canteiro;
- pagamentos a fornecedores/trabalhadores locais vão para esses agentes reais;
- parcelas importadas ou com contraparte externa vão para a **Reserva Global**;
- a UI continua usando a visão agregada **Disponível na cidade**, sem obrigar o jogador a separar estoque por proprietário;
- depois da conclusão, ativos privados seguem seus fluxos de aquisição/operação definidos na SPEC.

A diferença público/privado aparece principalmente **depois da obra**:
- serviço público continua ligado ao Caixa da Cidade;
- empresa privada passa a operar com caixa próprio;
- residência passa à economia privada após a primeira aquisição.

A logística física continua preservando produção, estoque, caminhões, distância, congestionamento e importação sem introduzir financiamento privado da construção.

## Revisão de gameplay: disponibilidade da cidade versus propriedade jurídica

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** aprovada e promovida para a SPEC.

A tentativa anterior de separar materiais de construção em estoques públicos e privados é economicamente plausível, mas cria um problema de UX: o jogador já controla diretamente a colocação de todos os edifícios. Exigir que ele também raciocine sobre "este aço é municipal, aquele aço é privado" pode transformar o core material em contabilidade de propriedade, em vez de logística e planejamento urbano.


### Decisão aprovada nesta revisão

- a UI usa **Disponível na cidade** como visão agregada dos materiais de construção;
- localização física real continua existindo;
- caminhões continuam obrigatórios para transportar materiais até a obra;
- a antiga nomenclatura **Depósito Municipal** é substituída por **Centro de Materiais de Construção**;
- o Centro é infraestrutura logística física e não força a interface a separar material público de privado;
- propriedade econômica pode continuar existindo internamente quando necessário, sem virar dois estoques principais para o jogador.


### Referências de gameplay

**Anno 1800**

A Ubisoft explica que qualquer recurso colocado em um warehouse fica disponível em qualquer warehouse da mesma ilha. É uma abstração deliberada que reduz microgerenciamento de estoque sem eliminar produção, transporte entre ilhas ou cadeias produtivas.

Fonte:
- Ubisoft — Anno 1800 Console Edition: 4 Tips for Getting Started: https://news.ubisoft.com/en-gb/article/6z7wAll2mXTdQtp79h2B2x/anno-1800-console-edition-4-tips-for-getting-started

**Manor Lords**

O jogo separa Regional Wealth do Treasury, mas recursos de construção são tratados como recursos da região e construções consomem materiais disponíveis no assentamento. Storehouses recolhem recursos de estoques locais e o jogo expõe reservas/limites de produção em nível regional.

Fontes:
- Manor Lords Official Wiki — Resources: https://wiki.hoodedhorse.com/Manor_Lords/Resources
- Manor Lords Official Wiki — Buildings: https://wiki.hoodedhorse.com/Manor_Lords/Buildings/en
- Manor Lords Official Wiki — Storehouse: https://wiki.hoodedhorse.com/Manor_Lords/Storehouse

**Workers & Resources: Soviet Republic**

No Realistic Mode, construções precisam receber recursos e trabalhadores fisicamente; construction offices buscam materiais em fontes específicas e levam ao canteiro. A documentação oficial descreve esse modo como mais sistemático e management-heavy.

A comunidade mostra os dois lados:
- jogadores valorizam muito a satisfação de ver tudo ser produzido e transportado;
- outros relatam que o excesso de atribuição, fases, veículos subutilizados e esperas transforma construção em microgerenciamento cansativo.

Fontes:
- Official Wiki — Game settings: https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Game_settings
- Official Wiki — Construction: https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Construction
- Official Wiki — Construction office: https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Construction_office
- Reddit /r/Workers_And_Resources — "I'm giving up on realistic mode" (2025)
- Reddit /r/Workers_And_Resources — "Should realistic mode be split..." (2023)

### Recomendação para o IndexCities

Separar **disponibilidade de gameplay** de **propriedade econômica**.

Para o jogador, materiais de construção devem aparecer como **Disponível na cidade**, não como dois estoques principais "público" e "privado".

Exemplo de UI:

Concreto
- disponível localmente: 120 t
- reservado: 40 t
- em trânsito: 30 t
- produção local: 20 t/dia
- dependência de importação: 35%

A origem física continua real:

- depósito de construção;
- concreteira;
- serraria;
- usina de asfalto;
- fornecedor local;
- conexão externa.

O sistema sabe onde o material está e caminhões precisam buscá-lo e entregá-lo, mas o jogador não precisa gerenciar propriedade jurídica de cada tonelada.

### Como uma obra funcionaria

jogador coloca a construção
→ sistema calcula materiais necessários
→ verifica a oferta física disponível na cidade
→ reserva automaticamente fontes locais elegíveis
→ importa apenas o que faltar
→ caminhões entregam ao canteiro
→ material é consumido ao chegar

Esse fluxo pode valer para qualquer construção, evitando duas mecânicas diferentes de materiais.

### Dinheiro continua seguindo agentes reais

A unificação da disponibilidade material **não unifica o dinheiro**.

No modelo atual:

- toda obra pública ou privada ordenada pelo jogador é financiada pelo **Caixa da Cidade**;
- produtores e trabalhadores locais recebem os pagamentos que lhes cabem;
- pagamentos de importação vão para a **Reserva Global**;
- depois da conclusão, empresas privadas operam com caixa próprio e residências seguem a economia de seus proprietários.

Essas transferências acontecem na simulação sem obrigar o jogador a administrar estoques por proprietário.

Em outras palavras: **um painel agregado de oferta local não significa que juridicamente todo material pertence à prefeitura nem que todos os pagamentos vão para o mesmo caixa**.

### Papel do jogador

A regra atual de que o jogador posiciona todos os edifícios já coloca o jogador acima do papel literal de um prefeito.

Uma interpretação mais coerente para a gameplay é:

- o jogador controla o desenvolvimento da cidade;
- o orçamento municipal continua existindo como entidade econômica real;
- empresas continuam tendo caixa e estado econômico próprios;
- mas a UI do jogador pode agregar recursos e capacidade da cidade para permitir decisões claras.

Isso evita tentar fazer a interface obedecer literalmente à propriedade jurídica de cada recurso.

### Centro de Materiais de Construção

A revisão foi concluída: o antigo "Depósito Municipal" foi substituído na SPEC por **Centro de Materiais de Construção**.

O Centro é infraestrutura física de armazenamento/logística e a visão "Disponível na cidade" pode incluir também materiais em produtores e outras origens elegíveis. Isso evita transformar propriedade jurídica em microgerenciamento.

### Critério de design

Preservar o que gera gameplay:
- materiais físicos;
- produção;
- estoque limitado;
- caminhões;
- distância;
- congestionamento;
- capacidade de carga/descarga;
- importação;
- custo;
- escassez.

Evitar o que tende a virar microgerenciamento contábil:
- escolher manualmente o dono de cada lote;
- separar a mesma categoria em "aço público" e "aço privado" na UI principal;
- exigir depósitos duplicados apenas por propriedade;
- fazer o jogador autorizar cada transação entre empresas e prefeitura.

### Recomendação atual

A melhor direção parece ser **mercado/oferta material da cidade com logística física**, e não um estoque juridicamente único nem estoques públicos/privados expostos ao jogador.

Na validação global do jogo integrado, verificar se o jogador consegue entender:
- quanto existe na cidade;
- onde está fisicamente;
- quanto já está reservado;
- quanto precisa ser importado;
- por que uma obra está esperando;
- quanto produzir localmente está economizando.

Essa direção mantém realismo sistêmico sem transformar o jogo em contabilidade manual.


---


## Ritmo de construção e espera — pesquisa exploratória (2026-10-08)

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — em 2026-10-08 o responsável confirmou a **direção de obras relativamente rápidas, sem espera artificial longa, preservando materiais, deslocamentos e mão de obra reais**, agora na SPEC. A pesquisa comparativa, os tempos sugeridos pela IA e a proposta monetária ainda não foram aprovados nem revisados integralmente.

### Direção de produto confirmada em 2026-10-08

A SPEC passou a estabelecer obras de execução relativamente ágil quando insumos, acesso e trabalhadores estão presentes, **sem duração longa adicionada artificialmente**. Entregas físicas, escassez, trânsito e capacidade real podem atrasar as obras; o andamento deve ser legível e várias obras podem avançar em paralelo conforme a capacidade existente. O jogador não deve precisar microgerenciar caminhões ou trabalhadores.

**Não foram aprovados** segundos ou minutos por prédio, fórmulas, multiplicadores novos ou recuperação de materiais entregues. **A política monetária de cancelamento foi aprovada posteriormente**, em 2026-10-08, e está na SPEC: comprometimento no Caixa, pagamento real e liberação do saldo não gasto. Materiais entregues seguem consumidos/perdidos; reservas de materiais não entregues são liberadas.

### O que os jogos mostram (comparação qualitativa, não experimento controlado)

- **Timberborn:** construtores transportam materiais, trabalham nos canteiros e disputam capacidade conforme prioridades; o gargalo pode estar em suprimento, acesso e disponibilidade de equipes. Fonte: [Wiki oficial, Construction](https://timberborn.wiki.gg/wiki/Construction_sites).
- **Against the Storm:** colocação de projeto, prioridades e construtores disponíveis; muitos projetos podem ser movidos gratuitamente antes da conclusão, reduzindo punição por planejamento. Fonte: [Wiki oficial, Buildings](https://wiki.hoodedhorse.com/Against_the_Storm/Buildings).
- **Workers & Resources: Soviet Republic:** construção física pode usar equipes/materiais e o jogo também dispõe de construção rápida financiada, fora do modo realista; a demora é parte relevante da experiência mais sistemática. Fontes: [Wiki oficial, Construction](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Construction), [Game settings](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Game_settings).
- **Feedback qualitativo divergente:** alguns jogadores relatam satisfação por construir tudo fisicamente; outros relatam espera longa, gargalos opacos e trabalho repetitivo, especialmente no início de *Workers & Resources*. Não tratar comentários como amostra representativa nem como medição de duração ideal. Exemplos: [satisfação](https://www.reddit.com/r/Workers_And_Resources/comments/123rsn1/realistic_mode_is_the_best_mode/), [frustração](https://www.reddit.com/r/Workers_And_Resources/comments/1lu9a5i/im_giving_up_on_realistic_mode/).

### Hipóteses ainda pendentes de calibração no jogo integrado

Separar **tempo de suprimento e deslocamento reais** de **trabalho ativo de construção**. Não adicionar espera fixa longa apenas para simular realismo. O primeiro prédio útil precisa aparecer cedo para que o mapa vazio não vire espera sem decisões; obras maiores podem demorar mais, especialmente se a cidade possui poucos trabalhadores, congestionamento ou importação distante. Permitir que diversas obras evoluam em paralelo enquanto o jogador faz outras decisões; sem microgerenciar ordens de cada caminhão ou trabalhador. Um projeto parado deve mostrar a causa concreta: aguardando material, entrega, acesso ou equipe; progresso de obra deve corresponder a trabalho/capacidade real, sem animação como fonte independente de verdade.

**Faixas para testar, não requisito nem benchmark externo:** em velocidade 1, com insumos já no canteiro e equipe disponível, observar se um prédio pequeno concluir em cerca de **20–60 segundos reais**, um médio em **1–2 minutos** e um grande em **2–4 minutos** dá ritmo suficiente. Essas são apenas propostas de design da IA, não tempos confirmados por testes. Entrega e congestionamento podem estender significativamente a duração total; a aceleração x2/x3 aprovada na SPEC reduz a espera percebida. Revisar com teste de gameplay, sobretudo primeiros minutos de cidade vazia, dezenas de obras em paralelo e diferenças de infraestrutura/quadras. Não escolher tamanho de grade, fórmula, limite rígido, modo especial de construção instantânea ou novas opções configuráveis somente a partir desta pesquisa.

### Decisão separada que segue aberta

**Decisão aprovada em 2026-10-08 e registrada na SPEC:** custo previsto comprometido no próprio Caixa, desembolso para destinatário real quando a despesa ocorrer, e liberação do valor comprometido ainda não pago em cancelamento. A Reserva Global não retém dinheiro de obras. A regra dos materiais entregues/consumidos permanece vigente; detalhes de pagamentos antecipados, pedidos em trânsito e calibração são assuntos de validação, não autorizações para estornos fictícios.

---

## Jazidas simples e madeira renovável — rodada de 2026-10-10

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** Jazidas de Areia e brita (2B) e a decisão posterior de **madeira de fonte florestal renovável com capacidade limitada** (8 refinada) foram aprovadas pelo responsável e registradas na SPEC. A regra foi **estendida expressamente a jazidas de Areia e brita sem esgotamento**, conforme a SPEC atual; outras extensões seguem PENDENTES.

**Confirmado 2B:** áreas/depósitos de areia e brita gerados na seed viabilizam extração local física e importação alternativa, sem sistema amplo de mineração ou vários SKUs adicionais. **Complemento aprovado posteriormente: as jazidas não se esgotam por volume acumulado no primeiro modelo; a produção por período continua finita e exige operação real**.

**Confirmado 8 refinada:** floresta produtiva pode fornecer Madeira continuamente, sem gastar árvores individuais nem exigir replantio manual. A vazão por período depende de área produtiva disponível, trabalho, operação e tempo; unidades produzidas entram em estoques e são transportadas e comercializadas fisicamente. É renovação/manejo agregados, não madeira instantânea ou capacidade/estoque infinitos. Se a área for ocupada por construções, a produção disponível diminui. Plantio/crescimento/colheita explícitos seguem como eventual melhoria futura **não aprovada**. A representação visual deve preservar coerência; não aparentar que a mesma árvore é cortada infinitas vezes.

### Extender o princípio a outros recursos básicos — PENDENTE (2026-10-10)

> **Revisão humana desta subseção: PARCIALMENTE REVISADO.** O responsável confirmou expressamente que jazidas de Areia e brita não se esgotam no modelo inicial, preservando capacidade produtiva limitada, operação e logística. As demais hipóteses de extensão seguem PENDENTES.

**Princípio em avaliação:** distinguir duração/reposição da **fonte**, capacidade de **produção por período** e quantidade de **estoque físico** existente. Uma fonte persistente não implica estoque ou produção infinitos.

- **Alimentos:** fazendas já produzem recorrentemente com terra, trabalhadores, insumos e tempo reais; não exigem consumo definitivo do solo em cada safra. Não criar estoque infinito.
- **Areia e brita — APROVADO:** os depósitos seed-gerados **não se esgotam por contador de reserva no modelo inicial**, embora minerais reais não se regenerem biologicamente. Cada local oferece produção limitada por área, operação, trabalho, tempo, custo e transporte, gerando unidades físicas verdadeiras somente quando produzidas. Não cria produto armazenado infinito, nem elimina escassez econômica e espacial. Esta é uma abstração intencional para evitar reconstrução repetitiva.
- **Concreto, Aço e Asfalto:** materiais industriais transformados. Produção contínua depende de **insumos efetivamente adquiridos**, fábricas, equipe, energia e logística. Não substituir insumos por geração gratuita nem simular minas de produtos finais.
- **Combustível e Suprimentos médicos:** preservar obtenção real local/importada, preços, estoques e entrega física. Não criar combustível inesgotável em uma bomba nem suprimentos surgindo num hospital.
- **Água, eletricidade, esgoto e lixo:** conforme a SPEC, são sistemas agregados de serviço e capacidade, não estoques desses oito materiais. Fontes e capacidade ainda impõem limites.

**Síntese após decisão:** tanto florestas produtivas quanto jazidas iniciais fornecem matérias-primas continuamente **com vazão real limitada**; edifícios de transformação seguem dependentes de insumos reais, dinheiro, trabalho e transporte. Não generalizar inexauribilidade a todas as matérias-primas sem decisão específica; para produtos industriais, o gargalo é sua cadeia de fornecimento, não um depósito mágico. Parâmetros de vazão e distribuição são calibração de gameplay.

