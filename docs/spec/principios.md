# Princípios de design e critérios de avaliação

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Valem para **todos os sistemas** e devem ser aplicados em toda definição, implementação, revisão e teste. As quatro últimas seções (gameplay e microgerenciamento, diversão de construir, vida dos SIMs, causalidade e diagnóstico) vieram do `AGENTS.md` (regras permanentes do responsável, 2026-10-08 e 2026-10-11) e foram movidos para cá sem alteração de conteúdo.

## Princípios de design do produto

### Profundidade sem microgerenciamento

- O jogo deve priorizar **decisões sistêmicas e consequências observáveis**, não tarefas repetitivas de administração.
- Realismo deve ser mantido quando cria gameplay; detalhes que apenas adicionam cliques, contabilidade ou manutenção manual podem ser agregados ou automatizados.
- A interface pode apresentar informações agregadas enquanto a simulação mantém localização, transporte, capacidade, escassez e demais consequências físicas internamente.
- Esse princípio vale para todos os sistemas do produto.
- **Gameplay e microgerenciamento como critério global obrigatório (reafirmado em 2026-10-08):** toda mecânica deve justificar seu impacto sobre a experiência do jogador, considerando **cliques, etapas, esperas e decisões repetitivas**, mas também o **microgerenciamento invisível** de estados por SIM/empresa, regras especiais, exceções, buscas contínuas, memória e custo de processamento. A profundidade só vale o custo quando produz consequências, escolhas ou diagnóstico relevantes; antes de adicionar subsistemas, preferir aproveitar as regras existentes e processamento por eventos quando suficientes, **sem eliminar economia física, dinheiro, logística ou efeitos reais**. Não criar esperas artificiais nem sistemas de manutenção complexos para problemas que possam ser resolvidos pelo fluxo normal. O ritmo percebido e a necessidade de exceções devem ser avaliados com o jogo integrado.

### Leitura em camadas e causalidade

- A mesma simulação deve servir tanto a jogadores que preferem feedback direto quanto a jogadores que gostam de investigar estatísticas e otimizar sistemas.
- Problemas importantes devem aparecer primeiro por sinais visuais claros e consistentes, com uma causa resumida e uma ação compreensível.
- O jogador deve poder aprofundar o mesmo problema e ver dados, fatores e cadeia causal quando quiser.
- Consequências relevantes, como falência, falta de recurso, mudança de demanda ou interrupção de serviço, não podem depender de caixas-pretas impossíveis de explicar.
- Informações importantes não devem depender apenas de cor ou de um ícone sem contexto; texto curto, tooltip ou outro canal de explicação deve estar disponível.
- Simplificar a interface não significa simplificar artificialmente a simulação: o detalhe pode continuar existindo internamente desde que seja legível e diagnosticável.
- A experiência atual é o **Convencional**, com profundidade progressiva na mesma interface. O futuro Criativo permanece como direção adiada, sem exigir agora duplicação de telas, simulação ou infraestrutura.
- **Mapas temáticos de diagnóstico — escolha 9B aprovada (2026-10-11):** o jogador pode ativar **sobreposições visuais no mapa da cidade** para ler condições urbanas já simuladas, como congestionamento, acesso/capacidade de serviços, poluição e concentração de dificuldades econômicas ou de emprego quando esses dados de fato existirem. As camadas destacam locais afetados e permitem consultar explicações e causas reais ao selecionar a região/elemento, sem transformar toda variação em alerta e sem sugerir automaticamente ordens de construção. Mapas temáticos derivados não criam serviços, cobertura ou satisfação por si; o emprego de mapas no futuro Criativo não faz parte do escopo atual. Legenda, filtros e atualização proporcional são detalhes de interface e calibração.
- **Sem ferramenta específica de delimitação/nomeação de bairros no primeiro modelo — pergunta 19 adiada pelo responsável (2026-10-11):** não implementar agora desenho manual de fronteiras, cadastro de distritos, nomes/estatísticas de bairros definidos pelo jogador nem administração territorial nova. A cidade **continua podendo exibir condições por áreas realmente afetadas** nos mapas temáticos, desafios regionais e diagnósticos aprovados, sem exigir distrito cadastrado; essa decisão **não proíbe** referências descritivas a regiões/bairros no texto. Revisar a conveniência de uma ferramenta de bairros apenas quando houver ganho concreto de organização/consulta no jogo utilizável.
- **Consequências humanas das decisões do jogador — princípio global:** intervenções urbanas, impostos, serviços, empregos, transporte e outras decisões devem produzir efeitos reais sobre os SIMs e suas famílias conforme os sistemas existentes; quando esses efeitos forem relevantes, o jogo deve permitir ao jogador **perceber quem ou quantas pessoas foram afetadas, de que maneira e por quê**. Em ações de grande impacto previsível, apresentar uma prévia curta das consequências antes da confirmação, sem prometer resultados incertos.
- A leitura deve funcionar em camadas: resumo agregado e contextual primeiro, inspeção de SIMs/famílias/empresas e causas quando necessário. **Não criar confirmação ou notificação individual por cidadão**, alertas a cada pequena oscilação, ou rotinas extras de cálculo por SIM sem ganho observável de gameplay. As consequências continuam sendo estados/eventos reais da simulação, não indicadores artificiais criados apenas para exibir impacto.

### Concretude antes de abstração

- O IndexCities deve preferir, sempre que viável, **entidades, propriedade, dinheiro, recursos e fluxos concretos** a proxies abstratos criados apenas para simplificar a simulação.
- Uma abstração só deve permanecer quando reduzir microgerenciamento, custo técnico ou complexidade visual sem esconder a causa real do sistema.
- Estados agregados de interface podem existir, mas devem ser derivados de entidades e fluxos reais e inspecionáveis.
- Dinheiro, recursos, demanda e propriedade não devem surgir, desaparecer ou mudar de mãos sem uma origem e um destino economicamente explicáveis.
- Esse princípio não proíbe abstrações já úteis ao produto, como interiores não renderizados, visões agregadas de materiais ou redes de serviço por capacidade; ele exige que essas abstrações preservem causalidade e diagnóstico.


## Gameplay, microgerenciamento e custo interno da simulação

**Regra permanente confirmada pelo responsável em 2026-10-08: qualidade de gameplay e controle de microgerenciamento são critérios obrigatórios para TODO sistema do jogo, não apenas construção ou realocação.** A IA deve questionar propostas que criem espera, fricção, controles, estados ou exceções sem benefício proporcional, inclusive quando ela própria sugeriu a complexidade. Preservar causalidade e consequências reais; o objetivo não é transformar profundidade em burocracia.

Para qualquer decisão de produto, avalie explicitamente o microgerenciamento do jogador **e** o custo de manter e executar a lógica nos bastidores contra o valor de gameplay.

- Realismo não é objetivo suficiente por si só: detalhe adicional precisa criar decisão, consequência, leitura sistêmica ou feedback interessante.
- Preserve profundidade sistêmica quando ela gera causa e efeito observável, especialmente produção, logística, emprego, trânsito, capacidade, escassez e finanças.
- Abstraia ou automatize tarefas repetitivas que exigem cliques/contabilidade sem produzir decisões significativas.
- Prefira interfaces agregadas para leitura e decisão quando a simulação puder manter o detalhe físico internamente.
- Não remova logística, escassez ou consequências apenas para simplificar; simplifique a operação manual, não necessariamente a simulação.
- Ao comparar alternativas, registre quando uma opção é mais realista porém pior de jogar, ou mais simples porém destrói consequências importantes.
- Aplique este critério de forma genérica a todos os sistemas, não apenas a materiais e construção.
- **Automatizar não torna um sistema simples:** muitos estados por SIM, rotinas recorrentes, exceções, dependências e regras de conciliação também são microgerenciamento interno, consumindo performance, memória, manutenção, testes e capacidade de diagnóstico.
- Antes de acrescentar um subsistema, verifique se sua consequência relevante pode ser representada com menos estados, menos interações e uma regra clara, preferindo processamento por eventos ou ciclos apropriados a verificações constantes quando viável.
- Profundidade causal é desejável, mas simular burocracia, contabilidade ou negociações em detalhe só se justifica quando gerar consequência de gameplay relevante e observável. Não empurre complexidade para trás da interface para parecer que o jogo ficou simples.
- **Antes de propor, especificar ou implementar uma mecânica, confronte:** o que ela acrescenta às decisões/consequências interessantes; quantos cliques, etapas e esperas reais impõe ao jogador; quais estados, exceções, consultas e testes acrescenta internamente; e se regras já existentes ou uma transição acionada por eventos entregam o mesmo valor com menos custo. Em caso de baixo ganho, simplifique ou não crie a mecânica.
- **Calibre a espera pelo ritmo percebido no jogo**, distinguindo tempo simulado, tempo real do jogador e interrupção efetiva de serviços/produção. Não acrescente atrasos ou sistemas auxiliares para resolver problemas hipotéticos antes de verificar se realmente aparecem na gameplay integrada; não use essa diretriz para apagar custos, materiais, deslocamentos ou consequências aprovados.

## Diversão de construir para diferentes públicos

**Decisão expressa do responsável em 2026-10-11:** o IndexCities deve oferecer profundidade de gestão para adultos **e** levar em conta quem, como uma criança que ainda não lê, se diverte prioritariamente construindo. **Esse critério deve estar presente em TODA revisão, definição de funcionalidade, interface, avaliação de gameplay e teste**, mesmo quando o assunto técnico/econômico não parecer direcionado a crianças.

- Em cada sistema ou decisão relevante, pergunte: **como a consequência aparece no mundo?** O jogador que só quer construir consegue identificar, posicionar, experimentar e observar algo interessante **sem depender de ler textos ou dominar a gestão**? Onde o controle, a espera ou o erro se tornam frustrantes?
- Preserve a profundidade aprovada de dinheiro, materiais, SIMs, empresas, logística e causalidade **no modo Convencional**. Não desative nem falsifique sistemas para parecer infantil; procure fluidez de construção, prévia, feedback visual, iconografia reconhecível, poucas etapas, personalização estética e informações em camadas.
- **Um sistema avançado não precisa ser operado por uma criança.** Avalie o impacto dele na diversão de construir, e não imponha a exigência impossível de tornar cada painel econômico legível sem texto. Não confunda interface acessível com ausência de regras econômicas.
- Valide o que for possível com observação de usuários com perfis distintos, incluindo uma pessoa que ainda não lê; priorize evidência de uso real ao invés de decidir dificuldade apenas por hipótese. Não transforme todo sistema em teste/POC obrigatório.
- **Escopo vigente:** desenvolver o jogo **Convencional**. Ter os modos **Convencional e Criativo** é uma intenção de produto confirmada **para o futuro**, com desenho e implementação do Criativo adiados. **Não implemente, especifique ou crie camadas técnicas extras para o Criativo agora**, nem force a solução histórica de uma segunda simulação. Os detalhes anteriormente discutidos foram preservados na exploração para eventual reavaliação. A diretriz de pensar na experiência infantil **vale desde já**, independentemente do modo futuro.

## Decisões do jogador afetam a vida dos SIMs

**Esta é uma orientação permanente para toda IA que pesquisar, especificar, implementar, revisar ou testar o IndexCities.** A regra de produto aprovada está em `docs/SPEC.md`, na seção *Leitura em camadas e causalidade* acima; este critério transforma essa direção em verificação obrigatória de trabalho.

- **Toda decisão relevante do jogador deve ter consequências reais, coerentes e explicáveis para os SIMs, famílias e, quando pertinente, empresas**: moradia, trabalho, renda, custos de vida, deslocamentos, acesso a serviços, patrimônio e demais sistemas existentes. Não criar punições ou benefícios arbitrários apenas para parecer que há impacto.
- **Não confundir intervenção sobre estruturas com decisão dos agentes:** SIMs e empresas são entidades reais que tomam decisões econômicas próprias; o jogador escolher onde construir/realocar não decide automaticamente quem vai ocupar, adquirir ou operar o imóvel. Em especial, empresas avaliam autonomamente a oportunidade de continuar no endereço novo; SIMs empregados avaliam independentemente permanecer no emprego. Consulte as regras específicas da SPEC: **em realocação assistida privada, o proprietário original (SIM ou empresa) tem a primeira oportunidade de comprar o substituto pelo valor de mercado novo**, quando elegível e interessado; isso não autoriza transferência automática de propriedade/estoque, aquisição gratuita ou continuidade garantida. Preservar gameplay fluido e evitar criar uma camada adicional de IA por agente sem necessidade.
- **Antes de fechar qualquer nova funcionalidade ou alteração de gameplay, verificar quem é afetado, por qual mecanismo real e como o jogador percebe isso.** Se o impacto relevante ficar invisível, opaco ou contraditório, apontar a lacuna e propor diagnóstico simples antes de implementar.
- Mostrar o importante de modo **contextual e agregado**, com aprofundamento disponível sob demanda; evitar pop-ups por SIM, gestão individual obrigatória, cálculos contínuos globais ou novos estados por pessoa sem ganho demonstrado.
- **Liberdade de construir e modificar a cidade importa:** não bloquear o jogador com burocracia apenas para simular realismo; preservar, porém, os efeitos reais sobre propriedade, pessoas e economia e os custos monetários/físicos já aprovados.
- Nas revisões da SPEC e do código, conferir esse princípio **transversalmente**, inclusive em ferramentas de edição, demolição, serviços e tributos. Ele não autoriza que a IA invente consequências ainda não decididas: lacunas de comportamento de produto devem voltar à discussão e entrar na SPEC antes de virar código.

## Causalidade e diagnóstico

Sistemas profundos não podem virar caixas-pretas.

- Toda consequência relevante para o jogador deve ser explicável a partir do estado real da simulação.
- Quando algo falhar, o sistema deve conseguir responder **o que aconteceu, por que aconteceu e qual ação pode resolver**, usando as mesmas variáveis que realmente produziram o resultado.
- Evite variáveis vagas de "saúde", "felicidade" ou "economia" quando elas apenas escondem causas reais. Agregados podem existir para UI, mas precisam ser derivados de fatores inspecionáveis.
- Automação e abstração de interface não autorizam lógica invisível sem rastreabilidade.
- Para calibração e debug, prefira fórmulas determinísticas, parâmetros explícitos e estados observáveis. Aleatoriedade só deve ser usada quando tiver função clara de gameplay e continuar diagnosticável.
- Ao implementar sistemas sistêmicos, mantenha uma forma proporcional de inspecionar entradas, saídas e causas — por UI de diagnóstico, debug ou telemetria conforme a fase do projeto.
- O feedback superficial deve ser simples e acionável; o detalhamento deve estar disponível sob demanda sem exigir que todos os jogadores o leiam.
- Não crie uma explicação separada da simulação: a explicação exibida deve ser derivada da causa real.
- Em toda nova conversa, sessão ou decisão de produto, aplique explicitamente este princípio antes de fechar o sistema: procure caixas-pretas, causas não rastreáveis, automações invisíveis e estados agregados que não possam ser decompostos. Se existirem, mantenha a decisão em exploração até haver uma forma clara de diagnóstico.
- Prefira o máximo possível **entidades, fluxos e estados concretos da simulação** em vez de variáveis ou agentes abstratos criados apenas para fechar uma conta. Abstrações continuam permitidas quando reduzem microgerenciamento ou custo técnico sem destruir causalidade, mas devem ser derivadas de estado real, auditáveis e não podem virar fonte ou sumidouro mágico de dinheiro, recursos, demanda ou comportamento.
