# IndexCities — SPEC

> **Este documento é a fonte de verdade do produto.**
>
> Ele descreve somente decisões já tomadas para o IndexCities. Não herda requisitos de projetos, protótipos ou conversas anteriores.
>
> Nova funcionalidade de produto entra aqui **antes** da implementação. Pesquisa e alternativas ficam em [EXPLORATION.md](EXPLORATION.md) até existir uma decisão.

## Estado atual

O IndexCities está em **fase inicial de definição**.

Neste momento, o objetivo é deliberadamente não preencher esta SPEC com decisões prematuras. Plataforma, direção visual, escala, sistemas de gameplay, simulação, persistência, performance-alvo e demais características serão discutidos e decididos progressivamente.

## Regras da SPEC

- Só entra aqui aquilo que foi explicitamente decidido para o IndexCities.
- Decisão antiga de outro projeto não é requisito deste projeto.
- Uma hipótese pode ser experimentada sem virar requisito.
- Quando uma exploração resultar em decisão de produto, esta SPEC deve ser atualizada de forma curta.
- Se uma decisão for substituída, este documento deve refletir o estado desejado atual, não manter versões antigas por histórico.

## Decisões de produto confirmadas

### Conceito e apresentação

- O IndexCities será um **city builder**.
- A apresentação será **3D com câmera isométrica**.
- A construção será feita na granularidade de **casas e prédios**, sem construção por cômodos.
- No escopo atual, o jogador posiciona **cada prédio diretamente**; zoneamento automático não faz parte da proposta atual.

### Tecnologia

- A engine do jogo será **Godot**.
- O desenvolvimento principal será em **C#**.

### Cidadãos e simulação

- Qualquer cidadão deve poder ser selecionado individualmente pelo jogador.
- O jogador deve poder acompanhar a vida e o estado daquele cidadão ao longo do tempo.
- Cidadãos são entidades persistentes da simulação, e não apenas elementos visuais decorativos.
- Cada residência deve ter ocupantes/famílias reais.
- Casas representam uma residência/família; prédios residenciais podem conter múltiplas unidades e múltiplas famílias.

### Empresas e economia

- Empresas serão entidades reais da simulação.
- Empresas terão funcionários reais.
- Cada emprego corresponderá a uma **vaga real** dentro de uma empresa ou serviço.
- Empresas terão dinheiro e estado econômico próprio.
- Estoque e/ou produção devem existir como parte do modelo econômico das empresas.
- O consumo será modelado inicialmente por **categorias de produtos**, não por SKU individual.
- Compras reais reduzem estoque real, movimentam dinheiro real e geram necessidade de reposição/logística.
- Empresas poderão falir quando suas condições econômicas levarem a isso.

### Serviços públicos

- Serviços como escola, hospital, polícia e bombeiros devem funcionar como sistemas reais da cidade, não como bônus abstratos.
- Esses serviços terão capacidade real.
- Esses serviços dependerão de funcionários reais da população simulada.

### Construção e obras

- Prédios e infraestrutura não aparecem instantaneamente prontos.
- Construções passam por uma fase real de obra.
- Obras consomem materiais da cidade.
- Obras dependem de trabalhadores reais da população simulada.

### Necessidades e serviços urbanos

- Cidadãos precisam se alimentar regularmente e adquirir comida de forma real dentro da economia.
- Cada prédio consome água e eletricidade de forma real.
- Água e eletricidade dependem de redes/capacidades reais da cidade.
- Prédios geram lixo.
- A coleta de lixo deve acontecer fisicamente por veículos/serviços da cidade.
- Cidadãos podem adoecer individualmente e buscar atendimento real.
- Crime pode ser cometido por cidadãos individuais.
- A polícia deve reagir a ocorrências reais geradas pela simulação.

### Tempo de jogo

- O jogo terá **pause** e velocidades **1, 2 e 3**.
- A escala de tempo e seus multiplicadores devem ser configuráveis para permitir calibração durante o desenvolvimento.

## Fora de escopo automático

Nada é considerado obrigatório apenas por ter existido em outro projeto.

Qualquer ideia anterior pode ser revisitada futuramente como material de pesquisa, mas precisa passar novamente pelo processo de exploração e decisão antes de entrar nesta SPEC.

A SPEC deve crescer com o produto, **não antes dele**.


### Educação, saúde, segurança e emergências

- Crianças e adolescentes têm idade escolar, matrícula real e deslocamento diário para a escola.
- Hospitais e unidades de saúde têm funcionários reais.
- A capacidade de atendimento deve depender de recursos reais da unidade, incluindo equipe disponível.
- Prisões/cadeias existem fisicamente e criminosos podem cumprir pena por um período.
- Prédios podem pegar fogo individualmente.
- Bombeiros precisam deslocar veículos e equipes fisicamente até a ocorrência.
- Mortes individuais geram consequências reais para a cidade.
- O sistema funerário/cemitério e a remoção física de corpos fazem parte da simulação.
