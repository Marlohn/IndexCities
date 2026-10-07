# IndexCities — EXPLORATION

> Este arquivo é o **hub central da exploração**.
>
> Ele não é fonte de verdade do produto. Decisões oficiais de produto pertencem a [SPEC.md](SPEC.md), decisões estruturais a [ARCHITECTURE.md](ARCHITECTURE.md) e regras de trabalho a [../AGENTS.md](../AGENTS.md).

## Como usar

Use este arquivo para localizar rapidamente o pensamento ainda não oficial do projeto.

- Pesquisa curta e transversal pode ficar temporariamente aqui.
- Quando um tema ganhar volume, alternativas, referências ou trabalho paralelo, mova-o para `docs/exploration/<tema>.md`.
- Documentos temáticos preservam pesquisa, trade-offs, alternativas superadas e questões abertas.
- Uma conclusão em exploração só vira requisito quando for promovida para a fonte canônica apropriada.
- Não criar documentos por ritual; os arquivos abaixo existem porque já há volume suficiente para justificar a separação.

## Explorações temáticas

- **Direção de produto e princípios de simulação** — ativa; identidade, loop, profundidade, legibilidade e microgerenciamento: [`exploration/product-direction.md`](exploration/product-direction.md).
- **Escala, performance e baselines** — ativa; população, profundidade individual, tempo e referências empíricas para benchmark: [`exploration/simulation-scale.md`](exploration/simulation-scale.md).
- **Mundo, mapa, terreno e ambiente** — ativa; direção visual, seed/reprodutibilidade, terreno, vegetação, clima e grid: [`exploration/world-map-environment.md`](exploration/world-map-environment.md).
- **Mobilidade, serviços e infraestrutura urbana** — ativa; vias, transporte, saúde, educação, serviços, água, energia, resíduos e conexão externa: [`exploration/city-systems.md`](exploration/city-systems.md).
- **Famílias, moradia e patrimônio** — ativa; renda, consumo, valor imobiliário, aluguel, propriedade, vulnerabilidade financeira e investimento residencial: [`exploration/households-housing.md`](exploration/households-housing.md).
- **Empresas, mercados e finanças da cidade** — ativa; empresas privadas, demanda, aquisição, caixa, falência, abastecimento e fluxos monetários da cidade: [`exploration/companies-markets.md`](exploration/companies-markets.md).
- **Construção, materiais e logística** — ativa; materiais físicos, estoques, importação/exportação, obras, Pátio e cadeia logística: [`exploration/construction-materials-logistics.md`](exploration/construction-materials-logistics.md).
- **Interface e ferramentas do jogador** — ativa; construção, ferramentas, snapping, overlays, realocação e fluxo de interação: [`exploration/player-interface.md`](exploration/player-interface.md).
- **Referência comparativa do gênero** — recorrente; benchmark externo para confrontar escolhas do IndexCities com outros city builders: [`exploration/genre-benchmark.md`](exploration/genre-benchmark.md).
- **IA para decisões, simulação e desenvolvimento** — pesquisa concluída para a fase atual; Jev/Laya não são recomendados no core por performance nem como camada geral do workflow: [`exploration/ai-decision-systems.md`](exploration/ai-decision-systems.md).
- **Processo de desenvolvimento e documentação** — decidido para a fase atual; histórico das alternativas e justificativas, enquanto as regras vigentes ficam em `AGENTS.md`: [`exploration/development-process.md`](exploration/development-process.md).

## Regra de autoridade

Documentos em `docs/exploration/` **não são requisitos por si só**.

Quando uma exploração fecha:
- comportamento/produto decidido → `docs/SPEC.md`;
- decisão estrutural de software → `docs/ARCHITECTURE.md`;
- regra de trabalho/documentação → `AGENTS.md`.

O documento temático pode continuar guardando o **porquê**, inclusive alternativas descartadas e evidências, desde que deixe claro o que está decidido, superado ou ainda aberto.


---

## Ciclo patrimonial após morte de SIM e encerramento de empresa

**Status:** em exploração; a proposta surgiu ao fechar o destino de imóveis após morte/falência.

### Problema

Ao modelar dinheiro e propriedade reais, remover uma entidade cria duas perguntas diferentes:

1. quem passa a possuir seus ativos;
2. para onde vai o dinheiro que já existia ou que entra pela venda desses ativos.

Simplesmente apagar o dinheiro viola o princípio de causalidade. Transferir tudo automaticamente ao Caixa da Cidade também cria uma receita potencialmente estranha e pode transformar morte/falência em benefício fiscal direto para o jogador.

### SIM que morre sem outro morador/proprietário

Hipótese preferida:

- se existir herdeiro elegível, patrimônio pode futuramente ser transferido a um SIM real por uma regra simples de herança;
- se não existir herdeiro elegível, o imóvel fica desocupado e retorna ao mercado;
- dinheiro pessoal e receita de venda não devem desaparecer silenciosamente;
- o destino exato do patrimônio sem herdeiro ainda precisa ser fechado.

Alternativas para patrimônio sem herdeiro:

**A. Caixa da Cidade recebe tudo**

Prós:
- fluxo simples e concreto;
- nenhuma entidade temporária.

Contras:
- cria incentivo fiscal estranho ligado a mortes;
- faz a cidade lucrar diretamente com patrimônios privados;
- pode ser economicamente desproporcional e difícil de justificar como regra geral.

**B. Dinheiro desaparece**

Prós:
- implementação mínima.

Contras:
- quebra conservação/causalidade monetária;
- contradiz o princípio de evitar sumidouros mágicos;
- não recomendado.

**C. Espólio temporário**

O SIM deixa um espólio contábil temporário:
- mantém dinheiro e propriedade;
- coloca imóvel à venda quando aplicável;
- recebe o valor da venda;
- depois distribui a um herdeiro ou encerra o patrimônio por uma regra explícita.

Prós:
- preserva origem e destino;
- permite herança futura sem reescrever o ciclo;
- não transforma a prefeitura em herdeira universal.

Contras:
- adiciona uma entidade temporária;
- exige uma regra terminal quando não há herdeiro.

**Recomendação atual:** usar a ideia de espólio como mecanismo interno simples apenas se necessário; preferir transferência direta para um herdeiro real quando existir. Para patrimônio sem herdeiro, não mandar automaticamente tudo ao Caixa da Cidade. Uma saída futura pode ser uma transferência explícita para o restante do mundo/sistema sucessório não modelado, usando a conexão exterior como fronteira econômica, mas isso ainda precisa de decisão.

### Empresa que deixa de operar

A mesma lógica não precisa ser idêntica à morte de um SIM.

Uma empresa que fecha um estabelecimento ainda pode possuir:
- caixa;
- estoque;
- imóvel;
- outros estabelecimentos.

Por isso, **fechar um estabelecimento não deve necessariamente matar a entidade empresa**.

Hipótese preferida:

empresa deixa de conseguir operar um local
→ estabelecimento fecha
→ imóvel entra à venda
→ empresa continua existindo enquanto possuir caixa/ativos
→ outra empresa compra o imóvel
→ pagamento vai para a empresa vendedora
→ a empresa vendedora paga obrigações pendentes e pode tentar outra oportunidade
→ só deixa de existir quando não possui operações, ativos nem capital economicamente relevante

Prós:
- o dinheiro da revenda tem destinatário concreto;
- evita mandar venda para prefeitura sem motivo;
- evita sumir com dinheiro;
- uma empresa pode falhar em um endereço sem desaparecer magicamente;
- caixa acumulado ganha função real de sobrevivência e reentrada;
- reaproveita o mercado de aquisição já decidido.

Contras:
- empresas inativas podem permanecer na simulação algum tempo;
- precisa existir um estado simples de empresa sem operação/procurando oportunidade;
- é necessário definir quando uma entidade sem ativos e sem capacidade de voltar ao mercado pode ser removida.

### Alternativa: liquidar e enviar sobra à prefeitura

Não recomendada como padrão.

Ela simplifica a remoção da empresa, mas cria uma transferência privada → Caixa da Cidade sem causa fiscal/econômica suficiente e pode recompensar indiretamente falências.

### Direção recomendada

Separar **fechamento de estabelecimento** de **morte da empresa**.

A empresa continua sendo um agente econômico enquanto tiver ativos ou caixa. O prédio fechado volta ao mercado, mas continua pertencendo à empresa até a venda; o comprador paga à empresa vendedora, não à prefeitura.

Isso reduz muito o problema terminal de dinheiro sem dono. A remoção definitiva da entidade empresa pode ficar restrita ao caso em que ela não possui mais estabelecimento, ativos ou recursos economicamente úteis.
