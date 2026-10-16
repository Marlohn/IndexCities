# Mundo, tempo e saves

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Histórico das decisões em [docs/decisoes/mundo-tempo-saves.md](../decisoes/mundo-tempo-saves.md).

## Em vigor no jogo (V1)

### Tempo de jogo

- O jogo terá **pause**, a velocidade normal **1x em tempo real** e **dois multiplicadores rápidos**, cujos valores são calibração técnica (ver *1x em tempo real* em [Recuperação da interação da V1](construcao.md#recuperação-da-interação-da-v1)).
- A escala de tempo e seus multiplicadores devem ser configuráveis para permitir calibração durante o desenvolvimento.
- **Aceleração real do tempo:** nas velocidades de jogo, o tempo simulado deve avançar de fato e **todos os acontecimentos e consequências correspondentes devem continuar sendo simulados**, incluindo rotinas e deslocamentos dos SIMs, trabalho, produção, consumo, estoques, logística, salários, aluguéis, dívidas, nascimento e envelhecimento. Acelerar **não** significa apenas saltar a data, inventar dias de atividade, concluir viagens/obras por aparência ou criar transações e resultados que não ocorreram. A simulação pode usar técnicas de processamento eficientes, inclusive eventos ou cálculo de intervalos previsíveis, **desde que preservem estado, identidade, interações, ordem causal e efeitos físicos/econômicos equivalentes**. O jogador vê o mundo efetivamente avançando, não uma projeção ou resumo substituindo sua execução.
- **Calendário convencional único:** começar com **calendário civil real (gregoriano), dias de 24 horas, meses com 28/29/30/31 dias e anos com 365/366 dias**, mantendo o mesmo relógio para rotinas, deslocamentos, produção, salários, aluguéis, dívidas, educação, nascimentos, envelhecimento e demais eventos. A velocidade do jogo apenas acelera **a execução real desses acontecimentos**, conforme o princípio já aprovado; não substitui dias por resultados resumidos, nem acelera somente a idade dos SIMs. **A duração de um dia do jogo em minutos/segundos reais, a velocidade numérica de 1/2/3 e metas de desempenho permanecem para calibração e medição**, sem inventar agora uma duração. **Possível revisão futura, NÃO aprovada:** se testes mostrarem necessidade e benefício concreto, investigar meses com menos dias para reduzir a quantidade de dias simulados por ano do jogo; isso muda o ritmo econômico/demográfico e **não reduz, por si só, o custo de processamento de cada dia**. Qualquer alteração do calendário exige nova decisão na SPEC, em vez de ocorrer silenciosamente como otimização. Não aprovar múltiplos relógios inconsistentes, salto de datas, replay ou arquitetura específica por inferência. Ver [exploração de escala](../exploration/simulation-scale.md#4-escala-de-tempo-e-velocidades).

### Terreno, vegetação e espaços públicos

- Edição manual de relevo fica fora do escopo inicial.
- Vegetação faz parte importante da apresentação da cidade.
- Árvores e vegetação podem ser removidas quando necessário para construir.
- Poluição sonora fica fora do escopo atual.

### Mapa, seed e limites

- O mapa é gerado/reproduzido a partir de uma **seed determinística**.
- A mesma seed deve permitir reproduzir o mesmo mapa/cidade-base para facilitar debug e testes.
- O terreno edificável do mapa inicial será **plano**, com vegetação, rio e/ou lago e conexão externa preparados. A exceção aprovada abaixo permite relevo natural nas margens do novo lago, sem ferramenta de edição de terreno.
- **Recuperação do lago:** cidades novas passam a ter lago de contorno irregular e margens em relevo. A água visível deve coincidir com a área considerada água na validação de construção e vias; não desenhar terra seca dentro de uma área proibida apenas para esconder o contorno circular. O mapa continua determinístico por seed e não recebe rio adicional, edição manual, recurso, fluxo hidráulico ou atividade econômica fictícia por inferência. Partidas já salvas preservam sua geografia, construções, acessos e funcionamento dos serviços, sem regeneração automática do mapa. A implementação deve distinguir os mapas novos dos anteriores e verificar save/load, posições válidas, captação e desempenho antes da entrega integrada. Alturas, forma e materiais são calibrados nessa direção, com avaliação visual no Godot e sem garantir aprovação estética apenas pelos testes.
- Não haverá compra/desbloqueio progressivo de novas áreas no escopo atual.
- O mapa começa com **borda fixa**.
- Não há recuo mínimo obrigatório entre construções e a margem de rios/lagos no escopo atual.

### Saves e persistência

- **Projetos urbanos não confirmados persistentes:** os esboços visuais de ruas/prédios ainda sem confirmação são **salvos com a cidade**, inclusive no **autosave já aprovado e configurável**. Ao carregar, preservam posições e condição de projeto sem passar a existir como obra, bloquear tráfego, consumir materiais, comprometer Caixa ou validar automaticamente algo que se tornou inviável. Não criar biblioteca/manual de projetos como obrigação.
- O jogo permite múltiplos saves e múltiplas cidades.
- Autosave existe e sua frequência deve ser configurável.

## Visão aprovada (ainda não implementada)

### Terreno, vegetação e espaços públicos

- **Decoração com efeitos urbanos reais e locais:** além de parques/praças funcionais já previstos, **elementos decorativos construídos de fato** (como jardins, canteiros, árvores ornamentais, bancos, cercas e outros itens pertinentes) podem contribuir para a **qualidade percebida do ambiente, o bem-estar dos SIMs expostos a esse ambiente, a atratividade e, quando fizer sentido, a avaliação imobiliária local**. No Convencional, colocação tem **custo e recursos reais pertinentes ao item**, não é bônus gratuito nem transação fictícia; o futuro Criativo terá suas regras concretas revisitadas quando sua implementação for retomada. **Nem todo objeto acrescenta felicidade automaticamente, de forma igual, permanente ou cumulativa ilimitada**: presença, localização, contexto, acesso/uso quando aplicável e repetição importam. Não criar uma necessidade de decoração por objeto, trabalho de manutenção individual, lucro automático, atendimento fictício ou substituição dos benefícios por visita real a parques já aprovados. Tipos/efeitos e fórmula simples precisam ser calibrados e testados para impedir exploração de bônus com centenas de objetos. *(Aprovada para experimentar no modelo inicial.)*
- **Versão inicial enxuta da qualidade ambiental:** implementar a influência positiva sobretudo a partir de **vegetação/jardins/espaços verdes reais próximos à residência ocupada** no Convencional, com contribuição **local, moderada e de retorno decrescente**, em vez de somar pontos idênticos por objeto. Os SIMs **não analisam individualmente cada adorno, câmera ou trajeto** para receber felicidade; a satisfação considera a condição ambiental da moradia no seu ciclo normal, podendo compartilhar resultados territoriais calculados/atualizados quando fontes reais mudarem. Objetos pequenos e ornamentais podem ser **apenas visuais** nesta versão, sem bônus próprios. Preservar uso/visita real aos parques, poluição e valor imobiliário como causas distintas, sem duplicar efeitos ou multiplicar valores imobiliários automaticamente; a localização realmente alterada é a fonte de efeito, nunca apenas o modelo gráfico. A direção busca **baixo custo de CPU**, mas não promete desempenho sem medição no jogo integrado; **fórmula, tipos de objetos relevantes, alcance, frequência e otimizações são calibração/implementação**, não novos subsistemas obrigatórios. Melhorias mais elaboradas só entram com evidência de valor e decisão de produto apropriada. Pesquisa, riscos de CPU/GPU e critérios de verificação: [qualidade ambiental local](../exploration/local-environment-wellbeing.md).
- **Benefícios locais de parques e praças com uso real:** parques e praças **podem receber visitas efetivas de SIMs**, proporcionando lazer/bem-estar coerentes com uso e acesso reais; **além das visitas individuais, sua presença acessível influencia positivamente a atratividade e o valor dos imóveis na região próxima**, segundo um efeito espacial simples vinculado à área atendida, não bônus global à cidade inteira ou renda monetária artificial. O jogador decide onde construir espaços públicos, e as condições locais reais determinam sua influência. Não exigir fila de entrada, lotação rígida, compra de bilhetes ou simulação de capacidade individual de visitantes. Alcance, efeito regional e frequência de recálculo de valor/atratividade ficam para calibração, reutilizando índices espaciais e eventos significativos.
- Parques e espaços públicos podem afetar lazer, bem-estar e atratividade da cidade.
- Praças, bancos, playgrounds e outros espaços públicos devem ter uso real pelos cidadãos.
