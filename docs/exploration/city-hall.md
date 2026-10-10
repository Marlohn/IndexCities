# IndexCities — Pesquisa de Prefeitura como construção e gameplay

> **Revisão humana: PARCIALMENTE REVISADO** — o responsável aprovou a **direção operacional principal da Prefeitura, com funcionários SIMs reais, e leitura secundária das necessidades reais da população (2026-10-10)**. As comparações, serviços candidatos, justificativas e detalhes criados por IA permanecem **PENDENTES** de revisão/aprovação. 
>
> **Fonte de verdade:** [SPEC](../SPEC.md) aprova uma **Prefeitura construída fisicamente, com papel principalmente operacional e SIMs empregados de verdade no edifício**, além de leitura complementar e agregada das demandas reais da população. **Serviços concretos, capacidade, presença de usuários, requisito de construção, efeitos sobre sistemas existentes e demais regras operacionais permanecem por decidir**. Esta pesquisa não aprova implementá-los.
>
> **Critério de leitura:** diferenciar mecânica observada em outro jogo, hipótese proposta para IndexCities, exigências já aprovadas na SPEC e vantagens alegadas/experiências comunitárias. Fontes oficiais/de desenvolvedores preferidas; wiki de fãs e comunidades rotuladas como tal. Jogos de épocas diferentes são comparados quanto ao *padrão de gameplay*, não copiados.

## Direção de produto aprovada posteriormente — 2026-10-10

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável escolheu explicitamente o papel primário **operacional, concreto, com SIMs funcionários reais**; a Prefeitura deverá **em segunda instância informar necessidades reais da população**. O detalhamento da IA abaixo sobre funções e opções ainda não foi revisado.

**Decisão oficial na SPEC:** a Prefeitura não pode limitar-se a um painel ou edifício simbólico. É um **local de trabalho municipal real**, com SIMs que ocupam empregos legítimos no prédio, comparecem conforme a simulação e recebem salários reais, para prestar atividades que precisam ser especificadas antes de código. Como segunda função, mostra problemas e demandas humanas **já existentes de verdade** no jogo, com leitura agregada por área e causa, sem criar reclamações ou serviços por animação.

**Lacuna central remanescente:** *que trabalho real é feito ali?* A existência de servidores não decide automaticamente a natureza do serviço. Alternativas que merecem contraste:
- **Administração de serviços municipais/contratos/obras reais:** servidores poderiam atuar no funcionamento agregado de processos que já existem, sem nova moeda burocrática nem atrasar arbitrariamente obras/tributos. É preciso escolher **uma tarefa concreta e causal**, não colocar funcionários apenas para autorizar cada clique.
- **Atendimento presencial excepcional a SIMs/empresas reais:** demanda relacionada a eventos específicos (não pagar impostos pessoalmente todo mês), com funcionários, tempo e acesso reais. Falta definir qual evento precisa de atendimento e por que isso ajuda o jogador.
- **Ouvidoria pública operacional ligada a problemas reais:** funcionários consolidam, analisam ou tratam solicitações ligadas a condições concretas, com diferença prática além do relatório que a UI já sabe gerar. Evitar criar tickets artificiais ou exigir cada SIM reclamar para um alerta existir.
- **Composição enxuta:** reunir uma atividade municipal interna concreta e atendimento cidadão **somente quando fizer sentido**, com informação agregada como complemento.

**Atenção a conflito já resolvido:** trabalhadores SIMs reais **foram aprovados**; a recomendação histórica abaixo de manter o prédio apenas informativo caso não se identifique trabalho útil **não representa a intenção vigente**. O próximo passo é definir tarefas com efeito verdadeiro e operação legível, não questionar novamente se a Prefeitura deve ter funcionários.

**Questões pendentes sem assumir resposta:** obrigatoriedade de construir, número e tipo de vagas, trabalho presencial versus balcão para público, limite por área ou global, consequências quando prédio está fechado/sem equipe, ocupação e custo, uso ou não de visitas de SIMs.

---

## Direção de aprofundamento confirmada pelo responsável — 2026-10-10

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável **concordou com a recomendação de priorizar coordenação real dos serviços municipais (opção C da rodada seguinte), com elementos pontuais de atendimento aos cidadãos (opção B)**. Isso complementa a decisão anterior de Prefeitura operacional com SIMs empregados e leitura secundária de demandas reais. **Não foram aprovados processos específicos nem obrigação de visitar presencialmente.**

**Na SPEC, diretriz agora confirmada:** a Prefeitura deve coordenar *serviços municipais reais* por trabalho de servidores SIMs reais, e pode incluir atendimentos pontuais que tenham objeto concreto. Apresentar demandas agregadas da população é papel secundário já aprovado. Evitar equipes de fachada, filas, licenças obrigatórias sem gameplay, bônus passivos e 'pontos de administração'.

**Pesquisa/propostas pendentes de escolha (não são regras):**
- Coordenação de trabalho municipal de **manutenção, solicitações e operações** que já ocorrem: decidir o que exatamente o servidor faz e que resultado observável cria, sem contradizer a priorização automática de manutenção, despacho de emergência ou trabalho do Pátio Municipal de Obras já aprovados.
- **Atendimento excepcional aos SIMs:** se um atendimento realmente justificar viagem, equipe, espera e resultado, escolher uma situação já modelada ou aprovar novo evento que agregue gameplay. Sem isso, não modelar visitas apenas por estética.
- **Escala e falta de servidores:** o trabalho deve possuir demanda/capacidade reais, mas não se presume paralisar funções municipais essenciais já definidas ou impor uma fila de autorizações a cada obra por ausência da Prefeitura.

**Próximo passo de alto valor:** escolher **um processo municipal específico** que dependa de coordenação humana real e produza efeito concreto demonstrável; só então decidir se e por que algum SIM precisará de atendimento presencial no edifício.

---

## Pergunta de produto

**Por que construir uma Prefeitura no mapa agrega valor que não seria entregue por um botão de finanças ou uma tela geral?**

No IndexCities, o jogador já consegue construir serviços, ajustar seis impostos imobiliários, contratar crédito e acompanhar a cidade; esses sistemas **não dependem** do prédio. O sandbox não exige desbloqueio de escolas/hospitais por marcos, e construções prontas **não têm upgrade de porte no escopo inicial**. SIMs e empresas são entidades reais com dinheiro, empregos e caminhos, sem salários, estoques e benefícios fabricados.

**Hipótese central:** combinar **sede cívica visível + trabalho municipal de pessoas reais + escuta de problemas concretos + interface transparente**, sem fazer o jogador gerir filas, processos, funcionários individuais ou decisões fiscais fictícias.

## Referências comparativas — 14 experiências e o que realmente oferecem

| Jogo | Função observada da sede/administração | Força | O que **não** importar automaticamente |
| --- | --- | --- | --- |
| **Cities: Skylines II** | City Hall com efeitos municipais amplos como juros menores, custo de importação, criminalidade e custo de evolução de edifícios; administração inclui outros serviços como Welfare Office. | Construção reconhecível, impacto imediatamente perceptível. | **Bônus mágicos globais** sem serviço, trabalho ou fluxo econômico correspondente; juros e taxas são já regulados em fluxos reais do IndexCities. |
| **SimCity (2013)** | City Hall e departamentos como Finanças, Saúde/Segurança, Educação, Transporte, Turismo e Utilidades, vinculados a desbloqueio regional de construções/controles. | Prefeitura representa crescimento e especialização da cidade. | Exigir prefeitura/departamento e novo asset/upgrade para hospital, escola, taxas e outros sistemas **já aprovados**, além de contrariar ausência inicial de upgrade físico de edifício. |
| **SimCity 2000/4** | Prefeitura pode surgir como marco/recompensa e oferecer indicadores ou prestígio cívico; não é necessariamente instituição operacional profunda. | Identidade visual / história urbana. | Prédio apenas decorativo ou preso a marco obrigatório de população; recompensas artificiais por progresso. |
| **Banished** | Town Hall revela inventário, produção e gráficos de muitos anos, além de permitir deliberar sobre entrada de grupos de nômades. | Diagnóstico agregado é valioso em simulação física e econômica. | Esconder informação essencial do jogador até construir Prefeitura ou trocar migração autônoma por autorização manual de cada grupo. |
| **Farthest Frontier** | Town Center é primeira construção; concentra panoramas de necessidades, emprego, recursos e políticas e participa da progressão. | Um **centro de diagnóstico**, com causas e gargalos legíveis. | Centro obrigatório como prédio inicial, árvore de unlocks e updates/upgrade de porte exigidos apenas por tradição do gênero. |
| **Foundation** | Manor House oficial agrupa funções de Tax Office, Great Hall, Bailiff Office, Watchpost e Treasury; tem funcionários e partes configuráveis. | Relação concreta entre administração, pessoas, orçamento e prédio. | Diversas salas/etapas, micromódulos e sede como barreira para recolher impostos. |
| **Ostriv** | O desenvolvedor apresentou em 2017 uma hipótese de Prefeitura com conselheiros e tarefas, exigindo equipe, móveis/papel e recurso de tempo do prefeito; notas da atualização inicial registram Town Hall para salários, preços e aluguéis. | Referência próxima de economia com indivíduos, empregos e decisões de preço. | Plano de 2017 não pode ser tratado como *tudo implementado hoje*; criar pontos administrativos, papel consumível por burocracia e tempo de prefeito a cada ação pode ser custo oculto sem gameplay suficiente. |
| **Caesar III** | Senado possui funcionários e arrecadação local; pessoas desempregadas aparecendo na escadaria dão sinal visual da condição urbana; fóruns expandem arrecadação por áreas. | A condição da cidade pode **aparecer no cenário** além dos painéis. | Coletores percorrendo cada imóvel para taxar; IndexCities já tem impostos cobrados por transferências de contas reais, não coleta física no quarteirão. |
| **Anno 1800** | Town Hall equipa especialistas/itens com efeitos em residências dentro de raio; Palace oferece políticas com alcance por ruas. | Layout e localização têm consequência espacial. | Bônus por item/raio, perks que inventam residentes, renda ou produtividade sem processo produtivo verificável. |
| **Tropico 6** | Palácio e governo simbolizam poder; a gestão política usa eleições, facções, mandatos, editos, constituição e menu de indicadores. | Decisões com conflitos sociais claros em sociedade viva. | Sistema de política partidária, eleições obrigatórias, término de partida por mandato ou facções criadas sem necessidade. O IndexCities prioriza sandbox e SIMs com comportamento real. |
| **Frostpunk 2** | Council Hall/câmara habilita propostas de leis votadas por representantes de facções; a governança vira o motor principal do jogo. | Políticas têm **trade-offs**, oposição e custo social. | Copiar parlamento, votação constante ou bloquear acesso a gestão atual por órgão obrigatório. Seria outro jogo. |
| **Timberborn** | District Center cria área administrativa/trabalho logístico, emprega construtores e relaciona mão de obra e caminho físico à produtividade. | Um edifício concreto como lugar de trabalho operacional, com gargalos reais. | Inventar distritos rígidos, redes de distribuição isoladas ou raio obrigatório para todo serviço urbano; IndexCities possui sistemas agregados independentes de distrito. |
| **Manor Lords** | Manor conecta tributação ao tesouro próprio do jogador e aspectos de administração/defesa. | Demonstra que impostos podem ter origem institucional e efeitos estratégicos. | Separar caixa pessoal do governante e Caixa da Cidade, tributar duas vezes ou interromper impostos já definidos quando não há prédio. |
| **Songs of Syx** | Administração com trabalhadores gera pontos de administração para manter/expandir domínio e políticas; fonte consultada avisa estar desatualizada. | Tenta transformar governo em capacidade produtiva de trabalho. | Nova moeda abstrata de **pontos administrativos**, tarefas paralelas e exigência recorrente de expandir escritórios sem causa concreta na economia; exemplo apenas conceitual. |

### Fontes dos jogos (preferência por material oficial)

1. **Cities: Skylines II**, diário oficial de serviços, incluindo City Hall e Welfare Office: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies ; economia/empréstimos: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production
2. **SimCity 2013**, guia de construções governamentais (guia de terceiro): https://www.gamerevolution.com/guides/59384-simcity-2013-city-buildings-government-reference-guide ; guia Prima Games: https://primagames.com/eguides/simcity-eguide/behind-the-scenes/quick-reference/buildings
3. **SimCity 2000/4**, descrição comparativa em wiki de fãs: https://simcity.fandom.com/wiki/City_hall e https://simcity.fandom.com/wiki/Reward_building
4. **Banished**, página sobre Town Hall (wiki de fãs): https://banished.fandom.com/wiki/Town_hall ; arquivos de registros e nômades: https://banished-wiki.com/wiki/Town_Hall
5. **Farthest Frontier**, guia oficial do Town Center: https://www.farthestfrontier.com/guide/information/town-center/
6. **Foundation**, wiki oficial da Manor House e subedifícios: https://wiki.polymorph.games/foundation/Manor_house
7. **Ostriv**, diário do desenvolvedor com hipóteses de 2017: https://ostrivgame.com/city-guard-and-town-hall/ ; implementação inicial de opções econômicas na Patch 9: https://ostrivgame.com/patch-9-release-notes/
8. **Caesar III**, transcrição do handbook original: https://caesar3augustus.com/book/government/senate e https://caesar3augustus.com/book/government/forum
9. **Anno 1800**, wiki de fãs sobre Town Hall: https://anno1800.fandom.com/wiki/Town_Hall ; Ubisoft em português sobre Palace e políticas: https://www.ubisoft.com/pt-br/game/anno/1800/news-updates/1L7VAkGiULLGJokKsQC80g/devblog-sede-do-poder
10. **Tropico 6**, manual da editora: https://download.kalypsomedia.com/manuals/Tropico6_Manual_PS4_UK_ONLINE.pdf
11. **Frostpunk 2**, explicação de Council Hall e voto: https://dotesports.com/frostpunk/news/frostpunk-2-how-to-set-and-replace-laws ; wiki de comunidade: https://frostpunk.fandom.com/wiki/The_Council
12. **Timberborn**, wiki oficial mantida pela comunidade: https://timberborn.wiki.gg/wiki/District_Center e https://timberborn.wiki.gg/wiki/Districts
13. **Manor Lords**, wiki oficial: https://wiki.hoodedhorse.com/Manor_Lords/Buildings/en e https://wiki.hoodedhorse.com/Manor_Lords/FAQ
14. **Songs of Syx**, wiki com aviso explícito de conteúdo desatualizado: https://www.songsofsyx.com/wiki/index.php?title=Administration

### Leituras cruzadas fora dos jogos

- **NYC311 (site oficial):** reclamações e solicitações de serviço nascem de *problemas concretos* e são encaminhadas a órgãos, com resolução e acompanhamento. A fonte é inspiração para agregar **sinais de bairros**, não para simular protocolos para cada buraco de rua: https://portal.311.nyc.gov/about-nyc-311/ e https://www.nyc.gov/site/311reporting/311-reports/service-requests.page
- **Prefeitura de São Paulo, Orçamento Cidadão (fonte oficial):** participação por territórios e prioridades orçamentárias pode inspirar leitura regional de demandas, não cria mandato de eleições nem verba gratuita: https://prefeitura.sp.gov.br/web/planejamento/w/orcamento-cidadao-ap
- **Feedback de comunidade NÃO representativo (Reddit):** jogadores divergem entre desejar Prefeitura presente já em cidade pequena e sentir que o edifício em CS2 chega tarde/grande e com bônus pouco importantes: https://www.reddit.com/r/CitiesSkylines2/comments/1icc8ev . Discussões sobre cidade como mera pintura versus simulação exigente mostram preferências diferentes: https://www.reddit.com/r/CitiesSkylines/comments/17hjdhp . **São opiniões e relatos, não evidência objetiva de solução ideal**.
- **Fórum Steam (discussão técnica de jogadores):** ideia de administração enriquecida com atendimento/capacidade, mas também risco de excesso de complexidade: https://www.reddit.com/r/CitiesSkylines/comments/1czmo80/improving_administration_in_cities_skylines_ii/

## Padrões que emergem

1. **Prefeitura como menu bonito:** Banished e Farthest mostram que concentrar informações é útil; mas no IndexCities a informação básica já precisa ser legível **antes de construir um prédio**. Um edifício somente de menu pode parecer decorativo.
2. **Prefeitura como bônus passivo:** Cities II e Anno são fáceis de entender, porém reduções de juros, aumento arbitrário de satisfação ou bônus de região podem quebrar a economia de fluxos reais do IndexCities se não corresponderem a serviço pago, trabalhador, localização ou mudança causal explícita.
3. **Prefeitura como gargalo administrativo:** SimCity 2013, Foundation e Frostpunk 2 tornam o prédio requisito para sistemas/políticas. Só é interessante quando esses sistemas são o **núcleo escolhido** do jogo. Hoje impostos, crédito, saúde, escolas e outros mecanismos do IndexCities **já existem conceitualmente sem esse bloqueio**.
4. **Prefeitura como local de trabalho e presença humana:** Ostriv, Timberborn e Caesar mostram empregados e função institucional. Combina com SIMs reais, se houver serviço verdadeiro que justifique salário e deslocamento.
5. **Prefeitura como expressão dos problemas da cidade:** Caesar (desempregados na escada), Banished/Farthest (indicadores) e sistemas 311 (demandas reais) sugerem a combinação menos explorada nos city builders: **observações coletivas baseadas em acontecimentos reais**, mostradas como panorama e quando fizer sentido, como presença de SIMs no mapa.
6. **Prefeitura como narrativa urbana:** construção com cara de administração, praça cívica, registros de acontecimentos históricos da cidade. Só é ganho legítimo se o edifício acrescentar identidade visual e acesso a registros reais, sem esconder dados essenciais.

## Propostas para o IndexCities — comparar antes de decidir

### A — Prefeitura administrativa/painel de controle (menor risco)

O edifício materializa a sede do município. Selecioná-lo dá acesso rápido a finanças, indicadores, alertas, contratos e histórico da cidade **já existentes**. A interface global preserva acesso integral sem o prédio.

- **Prós:** clareza, poucos sistemas novos, baixo custo, ponto físico de referência.
- **Contras:** não responde bem à pergunta de por que um SIM iria até lá; pode ser apenas asset com outro atalho de menu.
- **Adequação:** boa como camada inicial/pedestal, insuficiente como única função principal.

### B — Prefeitura com funcionários reais e serviços administrativos concretos (potencial alto, risco médio)

Instituição emprega SIMs e paga salários/custos reais para realizar **uma ou duas atividades administrativas de fato necessárias**, escolhidas explicitamente. Servidores trabalham no prédio (vagas, deslocamento, turnos, ausências, acesso). Moradores/empresas só o visitam quando houver uma razão legítima e incomum. Não criar ponto de administração, papel burocrático obrigatório ou fila para cada ato econômico rotineiro.

**Possíveis serviços candidatos (NÃO APROVADOS):** recepção de pedidos formais pontuais, registros públicos específicos, administração de solicitações municipais coletivas; interface de dados e decisões de políticas já definidas. **Dependência crítica:** selecionar o evento/serviço concreto que não duplica regras já automáticas. Se não existir, escritório cheio de empregados produzindo nada é pior que A.

- **Prós:** edifício existe como parte da vida econômica, produz empregos e tráfego real, manutenção tem causa.
- **Contras:** risco de burocratizar tarefas automáticas e gerar custo sem valor; visita individual em massa pode ser cara.
- **Adequação:** forte, mas somente se B for associada a um atendimento real escolhido, não a servidor por obrigação estética.

### C — Prefeitura como ouvidoria e observatório vivo dos bairros (recomendação inicial)

Um **canal de escuta** agrega situações observáveis da simulação por tema e lugar: escola sem vagas persistentes, lixo não recolhido, ônibus insuficientes, moradia inacessível, poluição, hospital lotado, desemprego real, atrasos de infraestrutura. **Não surgem missões, reclamações nem problemas falsos.**

O jogador abre a Prefeitura e encontra **três perguntas práticas**: *onde estão os problemas mais persistentes?*, *quem é afetado?*, *quais causas concretas do sistema devem ser melhoradas?*. Os registros podem mostrar exemplos de SIMs reais atingidos sem criar uma voz individual obrigatória para cada cidadão.

Se houver hipótese posterior de presença física: eventualmente alguns SIMs realmente afetados poderiam visitar/manifestar-se diante do prédio, **mas só com agenda/deslocamento/tempo reais e quantidade limitada**, sem filas rotineiras nem verificação por SIM a cada quadro.

- **Prós:** ligação forte com cidade viva, valor informativo observável, decisões agregadas de gestão, diferenciação em relação a um painel genérico.
- **Contras:** pode **duplicar alertas, overlays e desafios emergentes já aprovados**. Para justificar o prédio, a Prefeitura precisa oferecer uma visão de *priorização, persistência e impacto humano* que ainda não seja fornecida pelo painel geral. Não esconder informação crítica fora dele.
- **Adequação:** **mais promissora**, se for versão enxuta e reaproveitar dados/eventos já simulados.

### D — Prefeitura como lugar da participação cívica (interessante, mas não prioritária)

Reuniões públicas pontuais e *consultas por bairro*, com presença eventual de SIMs reais e temas derivados de problemas efetivos. Podem aparecer perfis humanos sem inventar facções políticas nem sistema eleitoral. **Escolhas não podem fabricar dinheiro, votos, eventos obrigatórios ou alterações mágicas de infraestrutura.**

- **Prós:** mais identidade humana e histórica, conflito de interesses concreto entre bairros e famílias.
- **Contras:** alto risco de se tornar painel de missões, novo sistema de política urbana ou simulação cara de assembleias. O que cidadãos decidem/como o jogador reage precisa de regra expressa.
- **Adequação:** **evolução futura** a avaliar depois da Prefeitura simples.

### E — Prefeitura como emissora de políticas/decretos (inspirada em Tropico/Frostpunk)

Concentrar decisões de tributação, ônibus grátis/pago e outras opções *que já existem na SPEC*. Não criar uma nova lista de decretos por ritual nem restringir o jogador a mudar taxas apenas em dias/assembleias não aprovados.

- **Prós:** organização conceitual clara.
- **Contras:** é apenas *outra navegação de menus* se não existir nova decisão; inventar leis de bônus altera economia e escopo.
- **Adequação:** boa **como interface**, fraca como função isolada. Política pública nova deve ser decidida e especificada uma a uma.

### F — Paço cívico vivo e memória da cidade (baixo risco se visual)

Ancorar referências visuais de vida pública ao redor do edifício: trabalhadores chegando, visitas ocasionais genuínas quando um serviço existir, espaço para observação de manifestações/celebrações derivadas de acontecimentos reais, e **arquivo histórico da cidade** (crescimento, crises, obras, mudanças de bairros) extraído de eventos registrados. Não exigir upgrade de asset; não criar festas, multidões nem bônus sem causa.

- **Prós:** identidade própria, história do jogador e vínculo entre SIMs e espaço urbano.
- **Contras:** visuais precisam corresponder à simulação, não animações mentirosas; arquivo pode inchar save ou repetir o histórico individual dos SIMs.
- **Adequação:** excelente camada de apresentação, não substitui utilidade administrativa.

## Modelo combinado que merece debate, não aprovação

**"Prefeitura: administração visível + observatório humano de problemas reais"** (A + C + elementos muito selecionados de B e F), **hipótese histórica parcialmente substituída** pela prioridade operacional aprovada depois:

1. **O prédio existe** como uma instituição municipal física, construível segundo SPEC, sem gerar capacidade/vantagem monetária fictícia.
2. **O jogador continua administrando** cidade, obras, impostos e emergências sem bloqueio na ausência de Prefeitura. Se o prédio for construído, selecioná-lo permite acessar um **painel cívico unificado**, reaproveitando interfaces existentes e uma *priorização de problemas por persistência e impacto* se esse diferencial for aprovado.
3. **Demanda cidadã** vem de fatos reais, agregada por bairro e assunto; não abrir mil chamados nem exigir responder a cada SIM. Um exemplo concreto pode apontar o SIM real afetado e suas necessidades. Um alerta crítico continua disponível na UI geral.
4. **Trabalho real** na Prefeitura só se o serviço escolhido justificar vagas, salários e presença; não contratar figurantes sem serviço, inventar novas moedas, gerar receita tributária por simples existência do prédio, ou bloquear cobrança normal de imposto quando servidor falta.
5. **Vida pública visível** (visitas ocasionais, movimento de trabalhadores, eventos coletivos) só quando houver participantes reais e programação/custo proporcionais. Fora disso o edifício pode estar quieto, sem encenar filas.
6. **O histórico cívico** usa eventos reais armazenados de forma enxuta e consultável, não recalcula a cidade inteira por quadro.

**Exemplo de história de gameplay, hipotética:** em um bairro com alta demanda escolar e nenhuma vaga acessível, o jogador recebe alerta agregado normal. Na Prefeitura, a versão aprofundada associa *há quanto tempo o problema persiste*, quantas crianças reais estão atingidas e como a espera, deslocamento ou ausência escolar se distribuem entre bairros, oferecendo atalhos para as escolas. A Prefeitura **não concede vaga**, não cria criança, não muda escola por clique e não esconde o alerta caso seja demolida. Quando o jogador construir escola e ela funcionar com equipe real, a condição e o panorama mudam automaticamente. O valor agregado está na **compreensão e nas consequências humanas**, não num bônus.

## Riscos e correções críticas

- **Problema 1 — "prédio sem função":** painel já acessível por atalhos + modelo 3D não criam gameplay. Solução: não chamar decoração de sistema; decidir qual interação cívica própria justificaria trabalhadores/visitas, ou aceitar honestamente valor visual e histórico apenas.
- **Problema 2 — serviços paralisados por burocracia implícita:** exigir Prefeitura para arrecadar tributos, crédito, obras, escola e saúde contradiz/ameaça escolhas existentes; **evitar como padrão**.
- **Problema 3 — bônus abstratos e exploits:** redução fixa de crime, juros, custo de material, felicidade ou tributação por mera presença não preserva causalidade; exigiria fonte econômica/operacional e decisão própria.
- **Problema 4 — micromanagement visível ou invisível:** protocolos, agendas, documentos de licença, filas presenciais, papéis, contagem de pontos de governo e contratação individual aumentam custos e complexidade; evitar.
- **Problema 5 — obrigatoriedade disfarçada:** se o jogador precisa ver um indicador vital mas o prédio ainda não existe, o painel vira penalidade artificial. **Informação essencial não deve ficar atrás do edifício** sem uma decisão muito clara e benefício real.
- **Problema 6 — duplicação de funcionalidades:** já existem **indicadores, alertas, desafios emergentes opcionais, políticas tributárias, crédito e históricos de SIMs**. Proposta de Prefeitura precisa ser filtrada contra essas interfaces antes de gerar novo sistema ou armazenamento.
- **Problema 7 — escalabilidade:** não consultar todo SIM a cada tick para criar "reclamação". Eventos significativos já detectados, agregação por área/tema, amostragem explícita para escolher exemplos humanos, retenção limitada de *agregados* e detalhamento sob demanda. Nenhuma nova restrição de retenção do **histórico de SIMs 10B** é autorizada por essa recomendação.
- **Problema 8 — assets/expansão:** SPEC não aprova melhorar/ampliar prédio pronto. Reutilizar o mesmo modelo físico, manter lote comum sem unidades modulares ou construir outra unidade independente somente após decisão específica.

## Três decisões que realmente destravam a direção da Prefeitura

**Pergunta 1 — Valor principal do edifício (RESOLVIDA posteriormente: operacional primeiro, observatório de necessidades em segundo):**
- **A:** paço cívico e histórico + navegação administrativa, sobretudo papel visual e informativo;
- **B:** sede de empregados e **atendimento administrativo físico com serviço real** ainda a selecionar;
- **C:** **observatório de problemas e demandas humanas reais por bairro**, como diferencial forte, podendo evoluir para presença/atendimento real.
**Recomendação crítica:** **C**, eventualmente combinada com A; só adicionar B se houver atendimento verdadeiramente útil e claro. O diferencial C precisa ir além de alertas já aprovados.

**Pergunta 2 — Prédio ausente/inoperante:**
- **A:** toda a administração atual continua operando; sem benefícios exclusivos da Prefeitura;
- **B:** serviços próprios *novos* da Prefeitura podem deixar de funcionar, mas impostos, crédito, obras e serviços já aprovados continuam;
- **C:** sem Prefeitura, administração atual da cidade deixa de funcionar.
**Recomendação crítica:** **B somente depois** de definir serviço novo concreto; até lá A preserva as decisões existentes. C cria travamento e incoerências graves.

**Pergunta 3 — Funcionários e visita de SIMs:**
- **A:** funções de painel/arquivo sem atendimentos; edifício pode ser visual inicialmente;
- **B:** funcionários reais quando houver serviço com capacidade e atendimento real, com visitas **por eventos significativos**, sem fila obrigatória para procedimentos cotidianos;
- **C:** toda decisão municipal gera processo presencial, filas e tempo administrativo.
**Recomendação crítica:** **B como evolução focal**, A como estado mínimo honesto se não for possível definir atendimento real; rejeitar C.

## Como avaliar num teste integrado sem inventar arquitetura

Cenários: (1) cidade muito pequena sem Prefeitura; (2) Prefeitura instalada e sem atendimento próprio — verificar se benefício não é só atalho para UI; (3) bairro sem escola e crise de coleta — observatório deve apontar situações reais, não número genérico; (4) cidade com milhares de SIMs — custo de agregação e consultas sob demanda; (5) prédio demolido — impostos, construção, alertas essenciais continuam; (6) serviços municipais colapsados por caixa — Prefeitura não deve fabricar solução.

**Conclusão de pesquisa, parcialmente superada pela decisão posterior:** a Prefeitura precisa ter **atividades concretas e servidores SIMs empregados no próprio edifício** como papel primário, e oferecer leitura secundária das necessidades humanas. Falta decidir **qual trabalho municipal específico** torna esse emprego útil. A alternativa de deixar apenas um painel informativo como função principal foi descartada pelo responsável; pesquisas sobre tipos de serviço, visitas, obrigatoriedade e custo de capacidade continuam PENDENTES. Reutilizar estado real dos SIMs e serviços, não criar segunda simulação burocrática.

**Nenhuma regra acima altera a SPEC sem decisão explícita do responsável.**


## Candidato operacional mais concreto — manutenção extraordinária e contratações (2026-10-10)

> **Revisão humana desta seção: PENDENTE.** É uma recomendação da IA pedida pelo responsável, **não foi aprovada ainda**; a SPEC preserva somente a direção já escolhida: coordenar serviços municipais por funcionários reais, com atendimento pontual e informação das necessidades como complementos.

**Proposta focal:** a Prefeitura atuaria como **central operacional para demandas municipais excepcionais que exigem coordenar recursos e prestadores**, particularmente **reparo de estruturas públicas danificadas**, onde equipe/material/capacidade municipal corrente não atendem à demanda. Não seria uma autorização obrigatória para o funcionamento normal de hospitais, escolas, coleta, polícia, bombeiros, tributos, obras nem manutenção recorrente.

**Fluxo demonstrativo hipotético:** incêndio danifica escola municipal → dano e necessidade física já pertencem ao estado real da simulação → equipe da Prefeitura coordena **contratação complementar real** para reparo quando a capacidade pública não resolve → contratante e prestador reais, orçamento do Caixa e materiais efetivamente disponíveis, trabalhadores e deslocamentos reais executam ação → escola recupera capacidade somente após conclusão física. Sem equipe/insumos/caixa, não há conserto mágico. A Prefeitura não vira um segundo Pátio de Obras nem cria obra paralela inventada; as responsabilidades de execução e contrato ainda carecem definição.

**Diferencial testável:** o serviço exclusivo deve produzir **ação comprovável além da interface** (por exemplo contratação e programação de reforço operacional realmente entregue) sem transformar um serviço que hoje funciona automaticamente em atraso burocrático arbitrário. Se uma operação já está coberta pelo Pátio, despacho automático, manutenção municipal ou compra automática de hospital, não duplicá-la por necessidade de manter o edifício ocupado.

**Atendimento pontual:** ocorrências reais de moradores ou empresas podem ser encaminhadas à Prefeitura só quando existir serviço apropriado, resultado e razão para comparecer. Nunca exigir cada SIM reportar problemas para que o jogo detecte falta de vagas, lixo acumulado ou falha de infraestrutura que já são simulados; a leitura coletiva de demandas segue segunda função.

**Riscos abertos:** extensão do trabalho do Pátio Municipal de Obras versus função da Prefeitura, origem da equipe de reparo/serviço terceirizado e recursos físicos, qual evento justifica contrato extraordinário, efeito da ausência de Prefeitura sem contradizer reparos públicos já definidos, contratação automática sem autorização individual do jogador, custos de pessoal e impacto de capacidade para não introduzir nova taxa administrativa fictícia.

**Próxima decisão candidata:** *A Prefeitura deve organizar contratações e reparos municipais extraordinários quando faltar capacidade operacional local, sem ser requisito para serviços e manutenção rotineiros?* A aprovação dessa opção ainda demandaria separação clara entre coordenar contratos e executar o trabalho, além de definir fluxo econômico e físico.
