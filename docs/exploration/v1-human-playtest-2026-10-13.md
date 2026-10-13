# IndexCities — Avaliação humana da partida da V1 (2026-10-13)

> **Revisão humana desta síntese: PENDENTE.** O conteúdo abaixo organiza o relato espontâneo do responsável durante uma partida no Windows, transcrito diretamente na conversa com o ChatGPT em 2026-10-13. **As opiniões e experiências relatadas são do responsável; a classificação, a organização e as hipóteses são da IA**, ainda não aprovadas. Não é uma lista de requisitos, autorização de implementação nem prova de bug reproduzido. Regras oficiais continuam em [SPEC](../SPEC.md); trabalho em [AGENTS](../../AGENTS.md).
>
> **Finalidade:** preservar contexto que se perde entre sessões e fornecer ao coordenador Codex e a eventuais verificadores uma referência concreta da experiência que precisa melhorar. Uma correção isolada não conclui esta avaliação. A validação final da jogabilidade depende do responsável jogando.
>
> **Limite da evidência:** houve uma captura de tela compartilhada com o ChatGPT e uma narração longa durante uma sessão de jogo. Esta página **não incorpora o arquivo original da captura nem uma gravação da partida**. O texto abaixo é uma síntese detalhada do relato, não sua transcrição literal completa. Quem receber apenas o repositório não deve alegar que assistiu ao jogo ou à captura original.

## Resultado percebido pelo jogador

A V1 já permite construir uma cidade, ver edifícios, ruas, caminhões, carros e SIMs, mas foi considerada muito aquém da experiência pretendida. A crítica central não é a quantidade de funções: **construir ruas é frustrante; menus e diagnósticos parecem logs de desenvolvimento; a cidade fica visualmente parada ou robótica; o ritmo pode deixar o jogador sem dinheiro e sem ação; cenário, água e vias parecem inacabados**.

O objetivo para uma recuperação futura, se aprovada, é melhorar a experiência **como conjunto**, e não encerrar o trabalho após corrigir um encaixe ou um rótulo. O responsável explicitamente não considera a V1 satisfatória.

## O que funcionou e deve ser preservado

- **Encaixe de edifícios** junto a ruas prontas após o PR #4: ficou fácil posicionar/reorientar casas e mercearia; a prévia e o traço **verde de ligação à via** ajudam.
- **Rotação por R** funciona após a tentativa inicial; vale investigar a inconsistência da primeira interação, sem concluir que é bug.
- **Arrastar para colocar fileira de casas** ficou bom, assim como alertas de posicionamento e obras sem acesso.
- **Caminhões trazendo materiais e progresso físico de obra** são interessantes, ainda que a apresentação possa melhorar no futuro (máquinas de construção foram mencionadas como ideia futura, não decisão).
- Boa parte dos **modelos de edifícios e ícones do catálogo inferior** agrada ao responsável. Evitar refazer tudo desnecessariamente.
- **Acompanhar** uma pessoa selecionada funciona; veículos particulares melhoraram em relação à versão anterior.

## Percurso da partida e problemas percebidos

### 1. Fundação e ferramentas de construção — incômodo mais forte

- A **Prefeitura é a primeira construção disponível**, antes da ferramenta de rua; o jogador tenta girá-la, relata falha aparente inicial e depois consegue com R.
- A rodovia inicial não oferece encaixe intuitivo para a Prefeitura. O jogador a encomenda no terreno sem rua, libera o catálogo, constrói um trecho de rua e descobre que a Prefeitura original ficou **sem acesso**. Depois usa **Mover obra**, cujo novo encaixe junto à rua pronta funciona bem.
- A ferramenta de **desenhar ruas** é descrita como muito difícil: encaixes imprecisos, esquinas e conexões confusas; o jogador repete várias vezes que essa é uma prioridade de revisão completa.
- Com trechos **ainda no estado de projeto/obra, recebendo materiais**, não consegue posicionar edifícios como gostaria ao longo da futura via. Investigar o que é impossibilidade intencional, erro de prévia ou comportamento em conflito com a SPEC; não atribuir automaticamente acesso funcional a rua ainda não construída.
- **Faixas de pedestres** parecem surgir excessivamente, até em situações que o jogador associa à colocação de casas. **Esquinas e meio-fio** são visualmente estranhos. Verificar o gerador viário e as regras reais de travessia, não só maquiagem visual.
- **Demolição** exige escolher a ferramenta Selecionar, clicar no segmento e procurar a ação Remover trecho; o jogador esperava uma ferramenta global mais direta. Essa proposta é mudança relevante de UX, portanto depende de avaliação/autorização segundo a SPEC.
- **Câmera:** Q/E giram, mas o jogador deseja giro intuitivo por botão direito do mouse; avaliar conflitos com botão direito/Esc para cancelar ferramentas antes de propor alterações.
- **Carros estacionados na rua** não estão representados como desejado. A SPEC já registra adiamento da decisão sobre vagas no meio-fio, pois a rua 1+1 atual não comporta a solução sem conflito físico.

### 2. Interface e comunicação — crítica transversal

- **Barra superior inteira** (Salvar, Cidades, Finanças, Pessoas, Obras e serviços, Controles), barra inferior, avisos sobrepostos ao cenário e janelas de inspeção foram consideradas **feias, excessivamente textuais e difíceis de compreender**.
- Menu **Cidades**: o campo de *seed/código do mapa* e o botão Criar induzem confusão entre criar e carregar. Ao criar uma nova cidade, o jogador acreditou ter perdido o save; depois conseguiu abrir a anterior. Solicita **nomes legíveis para cidades** e feedback/organização claros. Investigar sem afirmar perda real de dados.
- **Finanças**: disposição atual não ajuda a entender o essencial; receita/despesa, periodicidade e possibilidades de ação precisam fazer sentido para um jogador.
- **Pessoas**: lista longa e repetitiva, nomes como “Lia 13”, “Eva 4”; informações em forma de log. Seguir SIM funcionou, mas o painel pouco ajuda a interpretar a história ou a atividade dele.
- **Obras e serviços**: enorme lista de status e causas; encontrar o problema importante exige ler como se fosse um log. A mesma crítica vale para Controles.
- **Inspeção dos edifícios:** campos de proprietário, manutenção, água/energia em 0%, capacidade operacional em 0 e presença de funcionários são apresentados sem hierarquia causal; o usuário não entende se está funcionando ou **o que fazer**. Botão “Construir outro” em quase todo painel parece deslocado.
- Exemplo descrito: uma casa tem falta de abastecimento, mas a informação só fica clara ao clicar nela; pensar em linguagem visual consistente de avisos reais e prioridades, sem inundar o mapa com texto.
- **Caminhões:** em inspeção, houve uma percepção de carga vazia mesmo com veículo aparentemente transportando materiais; estado “motorista aguardando” também gerou confusão. Reproduzir antes de tratar como bug de simulação.
- **Indicador de FPS/branch/commit** aparece com posicionamento visual inadequado sobre o HUD. Preservar utilidade de diagnóstico, mas avaliar forma de exibição.

### 3. Tempo, dinheiro e progresso

- Mesmo em **60x**, a sensação foi de horas avançando lentamente, sem atividade econômica suficiente e com ausência prolongada de receita.
- O jogador relata uma cidade cujo dinheiro acabou e que ficou sem ações interessantes enquanto aguardava arrecadação de impostos aparentemente mensal.
- Investigar tanto **periodicidade econômica / fluxo de caixa / causas reais da cidade parada** quanto **o ritmo percebido e a relação com velocidades aprovadas**. Não oferecer dinheiro mágico nem alterar os multiplicadores ou o 1x aprovados sem decisão humana.
- Referência de ordem de grandeza para análise: 30 dias simulados no teto atual de 60x requerem aproximadamente **12 horas reais**, se o jogo mantiver a cadência sem pausas. Isso não é, por si só, prova de bug — é uma questão de ritmo de produto.

### 4. Pessoas, veículos e cidade viva

- SIMs foram percebidos **sobre casas, aparentemente presos no meio da rua, ou surgindo em pequenos grupos e depois desaparecendo**; movimentação parece robótica e descontínua. Verificar representação, interpolação, horários e estados reais, sem inferir automaticamente travamento causal.
- Carros particulares melhoraram, mas o jogador ainda não percebe atividade urbana convincente nem trânsito semelhante ao esperado de city builders de referência.
- **Fazendas** parecem pequenas e praticamente inertes, sem indicação visual de produção/trabalho real.
- **Eólicas:** algumas giram e outras não; a rotação em 1x parece excessivamente lenta e em 60x parece mais natural. Distinguir geração real dependente do vento, capacidade operacional e velocidade da animação.
- **Posto de combustível, Pátio de Obras e tratamento de água** aparecem com campos técnicos, por vezes todos em 0; o jogador não consegue inferir finalidade, dependências e situação. Apresentação deve mostrar causa e consequência, não apenas porcentagens.

### 5. Ambiente e acabamento visual

- O corpo d'água destinado a rio/lago foi considerado **muito artificial e visualmente ruim**, sem aparência convincente de água natural.
- Terreno e vegetação: poucas árvores esparsas num gramado uniforme, sensação de mapa vazio e genérico.
- **Geometria de ruas**, esquinas, calçadas, meio-fio e travessias precisa de avaliação visual e funcional integrada, especialmente se vagas na rua forem reconsideradas.
- Os modelos de construções existentes foram vistos mais positivamente que o mundo/menus. Comparação com referência visual anterior indicou que o jogo ainda está distante do acabamento desejado, mas **não há autorização para copiar** outra obra ou refazer o inventário completo.

## Captura de tela descrita (não anexada nesta página)

Na captura enviada na conversa (janela **IndexCities DEBUG**, compilação exibida como `6f4ea824`, branch `codex/construcao-inicial-visual`) vê-se cidade isométrica, ruas e loteamentos à esquerda, casas e fazendas, eólicas, instalações de água e grandes áreas de terreno vazio. HUD ocupa a parte superior e inferior, com inspector lateral direito. O rodapé exibe, naquele instante, déficit de água potável e demanda maior que a capacidade; barra superior exibe saldo, população e velocidades. **Não extrapolar** dessa imagem para estados de outras partidas ou para comprovar os episódios narrados.

## Questões que precisam ser investigadas, não presumidas

- Problema técnico reproduzível versus preferência de interação: rotação inicial; encaixe de ruas e esquinas; posicionamento junto a obras viárias planejadas; faixas de pedestres; SIMs visualmente presos/sobre casas; carga e motorista de veículos; eólicas e velocidade da animação; save e criação de cidades.
- Diagnóstico causal completo do ritmo lento: em que fase a cidade parou, quais empregos/negócios/serviços estavam operacionais, entradas e saídas reais do Caixa, dependências de água e energia, cobrança de impostos e efeitos de acelerar.
- Necessidades de **decisão humana prévia**: qualquer alteração da regra inicial Prefeitura → ruas, fluxo básico de construção/demolição/câmera, estacionamento nas ruas, velocidades/tributação, mudança relevante de UX, novas atividades causais ou expansão de escopo.
- Propostas de apresentação e assets podem ser prototipadas dentro dos limites aprovados; não confundir protótipo com aprovação de novo comportamento. A arquitetura atual mantém Codex como coordenador e Claude para assets conforme AGENTS.

## Direção de melhoria sugerida pela IA — ainda não aprovada

Não transformar o relato em um backlog de tickets independentes. Propor uma recuperação **integrada** da experiência, com duas dimensões complementares:

1. **Construir a cidade:** fluxo inicial e ruas, conexões e planejamento, construção, demolição, câmera, sinais de validação no terreno.
2. **Entender e acompanhar a cidade:** interface e diagnósticos consistentes, ritmo e causalidade econômica inteligíveis, SIMs e empresas observáveis, ambiente com linguagem visual coerente.

Proposta do coordenador deve cruzar este documento com SPEC e código, distinguir bugs reais de decisões pendentes, e apresentar antes da implementação **as escolhas de UX importantes**, o conjunto de mudanças de maior impacto e como será feita comparação antes/depois no Godot.

**Não dar missão por concluída** depois de melhorar um ícone, um encaixe ou apenas passar testes. A aceitação depende de nova partida humana mostrando progresso significativo **nas dificuldades acima**. Preservar o que já funcionou.

## Como usar este documento entre agentes e sessões

- **Codex coordenador:** ler este relato integral antes de definir prioridades, junto com AGENTS, SPEC e exploração; não tratar esta síntese como requisito oficial. Pode propor correções e pedir aprovação das mudanças relevantes.
- **Claude especializado:** receber apenas pedidos delimitados do coordenador dentro das permissões do AGENTS, principalmente assets; não assumir alterações de gameplay ou SPEC.
- **Verificador independente:** avaliar se cada entrega resolveu dificuldades relevantes do relato e se o jogo continua coerente como experiência; apontar o que ainda não mudou, além dos resultados de testes.
- **Responsável:** validar o resultado jogando. Seus comentários e aprovações posteriores devem atualizar o estado deste registro, sem apagar observações anteriores nem carimbar hipóteses da IA como decisões humanas.


## Proposta operacional da IA — recuperação integrada da V1 (ainda PENDENTE de aprovação humana)

> **Status: PENDENTE de revisão e aprovação humana.** Este é um plano executável **condicional ao contrato atual**, não uma decisão aprovada, nem autorização para alterar regras oficiais. O coordenador deve verificar o checkout local, atualizar sua leitura da SPEC e implementar **somente** comportamentos já autorizados ou correções que os restauram; para qualquer mudança relevante ou pendente, apresentar decisão ao responsável e registrar aprovação na SPEC **antes** do código. Não repetir uma fase de planejamento amplo: identificar diferenças entre este plano e o checkout e seguir com trabalho independente autorizado.

### Alvo de qualidade

Uma pessoa deve conseguir começar do zero, estabelecer acesso viário, construir um bairro simples, entender por que obras e serviços funcionam ou param, encontrar pessoas/veículos, compreender seus gastos e retomar um save **sem lutar com controles nem decifrar logs**. O observador deve perceber atividades **reais** da cidade e estados visualmente coerentes. Estética e gameplay só se consideram validadas após nova partida humana; asserções automáticas não substituem a experiência.

### O que pode começar sem outra rodada de planejamento

**Frente A — construção, acessos e geometria viária (primeira prioridade).**

- Executar no Godot o percurso narrado: Prefeitura → rua ligada à rodovia → esquina/L → rua em obra → casa/mercearia → mover obra → salvar/retomar. Capturar *antes/depois* e anotar falhas reproduzíveis. Identificar o problema real em prévia, snap, grafo, acesso, colisão ou feedback.
- Restaurar os gestos da SPEC: estrada por dois cliques com L/Tab, prédio por um clique com prévia 3D e ligação real, repetir/arrastar tipo e R manual. Não permitir acessar via inexistente ou criar entregas fictícias.
- Corrigir inconsistências técnicas reproduzidas de encaixe, desenho e conectividade física; garantir que cruzamentos, faixa de pedestres e meio-fio **visualmente representem a topologia e as travessias reais**. Verificar save/load dessas obras e regressão do PR #4. **Não** alterar formato/largura da via nem introduzir estacionamento sem decisão.
- Estudar colocação junto de **vias encomendadas ainda não prontas**. Pode mostrar/diagnosticar traçado planejado com base real já aprovada; novas garantias de acesso, confirmação antecipada ou dependências físicas diferentes precisam de decisão humana.

**Frente B — leitura da interface existente (alta prioridade, paralelizável com A se arquivos não colidirem).**

- Reproduzir no Godot o fluxo Salvar / Cidades / Criar / Carregar, finanças, obras, pessoas, edifícios e veículos; checar dados e mensagens existentes. O código atual de `Main.UI.cs` mistura criação por seed, lista de saves e autosave em `ShowCities`, e renderiza listagens extensas em `ShowAlerts`, `ShowFinances` e `ShowPeople`; isso é um alvo concreto de legibilidade.
- Melhorar **apresentação dos mesmos dados e ações aprovadas**, mantendo acesso a diagnósticos financeiros, obras, necessidades de água/energia/esgoto e veículos. Destacar estado principal, causa e consequência **reais**, com hierarquia visual simples, sinalizações consistentes, painéis que caibam na tela e explicações curtas. Não mascarar falta de serviço nem substituir informação causal por decoração.
- Separar visualmente **Nova cidade / seed** de **cidades salvas**, informar claramente que uma cidade nova foi criada, que a anterior foi salva e quando carregar altera a sessão; verificar ausência de perda de save antes de mexer no sistema de persistência. Proposta de **renomear cidades** e reestruturar profundamente menus são decisões de produto/UX a confirmar, não execução automática.
- Verificar dados exibidos no inspector (por exemplo, presença de funcionários, água/energia 0, carga do caminhão) contra simulação antes de corrigir. Não inventar atividades ou porcentagens.

**Frente C — cidade viva, tempo e causalidade (investigação e reparos respaldados, paralela quando isolada).**

- Reproduzir em save real uma cidade inicial e outra povoada, observando em 1x e 60x: relógio, deslocamentos, filas, atendimento/produção, receitas/despesas, escassez de caixa e tempo até próximos eventos. Distinguir **ausência de eventos por regras reais** de travamentos técnicos e lentidão de host.
- Conferir estado/posição/interpolação de pedestres e veículos, animação da eólica e indicadores de fazenda, pátio, posto e água. Corrigir bugs reproduzidos e melhor apresentar operação já existente; **não criar clientes, trânsito, recursos, receitas nem figurantes falsos**.
- Preservar tempo 1x = tempo real e fluxo econômico real conforme SPEC. Aceleração máxima local de 60x torna 30 dias simulados ≈12 horas reais; isso fundamenta discussão de ritmo, **não** autoriza mudar escala temporal, periodicidade de impostos ou criar dinheiro grátis. Propor opções com impactos, caso fique comprovado que o ciclo é inviável.

**Frente D — arte e visual do mundo (Claude, em paralelo quando não bloquear A).**

- Codex define pedido específico ao Claude a partir do briefing da V1: amostras de lago/rio, terreno e vegetação, acabamento de interseções/calçadas/faixas, ícones/estados visuais e elementos de atividade genuína. **Reutilizar assets aproveitáveis** e preservar escala, pivôs, colisão/acesso e compatibilidade com o jogo.
- Primeiro validar **uma amostra na engine** e o encaixe visual com A; só depois encomendar lote definitivo. O Claude produz assets na área designada, não altera lógica do jogo ou documentos oficiais por conta própria.
- Não copiar a referência externa nem expandir catálogo funcional por causa de arte. Mudança estética de direção geral permanece dependente da revisão humana.

### Propostas com decisão humana necessária (não bloquear frentes independentes)

1. **Fluxo inicial:** manter Prefeitura como primeira obra e melhorar explicações/posicionamento, ou permitir rua antes? A SPEC atual só libera rua **depois** de encomendar a Prefeitura.
2. **Controles e ferramentas:** demolição direta/global, câmera por botão direito (atualmente conflita com cancelar gesto), alteração significativa do planejamento das vias, redesenho de menus e nomes editáveis de cidades.
3. **Ritmo da economia:** rever velocidade máxima, calendário, vencimentos ou opções de recuperação após ficar sem caixa — com estudo de efeitos causais, sem simular dinheiro.
4. **Estacionamento na via:** a decisão vigente adiou vagas na rua 1+1 por incompatibilidade com o perfil geométrico; só reconsiderar com autorização.
5. **Direção estética:** aprovar amostra suficiente antes de uma substituição de ambiente/ruas generalizada.

### Execução por entregas e paralelismo

- **Entrega 1 — controle sobre a construção:** Frente A e diagnósticos críticos de B; arte de cruzamentos apenas depois de acertar compatibilidade viária. Medir percurso do primeiro bairro no Godot.
- **Entrega 2 — saber o que ocorre:** integrar restante de B e os reparos reproduzidos de C; mostrar claramente o que funciona, o que falta e como o jogador identifica a causa. Validar saves e inspeção.
- **Entrega 3 — cidade e mundo mais vivos:** C + D, condicionado à amostra visual e às decisões relevantes. Observar atividades reais em dois tamanhos de cidade e diferentes velocidades.

Paralelizar quando houver **entregas e arquivos independentes**: por exemplo, inspector/UI e investigação de simulação podem ocorrer em worktrees diferentes; Claude pode preparar **amostra** de ambiente enquanto Codex corrige a interação. Não iniciar simultaneamente mudanças conflitantes em `Main.cs`, `Main.UI.cs`, `CityWorld.cs` ou na geometria de ruas. Integrar em pequenas etapas, com revisão independente **em modo leitura**, sem agentes editarem os mesmos arquivos. DeepSeek Flash pode ser experimentado como triagem de PR **se estiver disponível e for competente no teste inicial**, sem confiança automática; Codex responde pela revisão e integração. A regra atual do AGENTS limita Claude a **assets**; revisão independente com Claude exige uma alteração de responsabilidades autorizada pelo responsável. Não desperdiçar Opus em revisão repetitiva.

### Verificação orientada pela partida, não só pela suíte

Cenários mínimos para cada entrega (adaptar ao escopo real):

1. **Início sem instruções:** fundar com Prefeitura, conectar à rodovia, fazer rua reta e esquina, colocar uma fileira de casas e mercearia, identificar por que um prédio ficou sem via e mover obra. Não haver bloqueios inexplicáveis.
2. **Obra ainda não pronta:** visualizar a rua em construção, tentar construir ao lado e entender claramente se é permitido, em que condição e por quê; caminhões transportam materiais de fato; estados sobrevivem a salvar e carregar.
3. **Inspeção sem decifrar logs:** localizar falta de água, energia ou acesso; encontrar a causa de obra parada; distinguir capacidade, demanda, manutenção, proprietário e presença real; identificar transporte de carga. Informação profunda permanece acessível.
4. **Economia:** em 1x e 60x medir horas simuladas por segundo real e causas de estagnação, lançamentos e cobranças reais; relatar a duração real de avançar até um novo evento relevante. Nenhuma correção deve falsificar saldo ou criar imposto indevido.
5. **Cidade povoada:** acompanhar SIM a pé/de carro, operações da mercearia/fazenda/posto, movimento eólico e presença no prédio; comparar antes/depois em condições reprodutíveis. Ausência verdadeira de atividade deve ter explicação, não enfeite enganoso.
6. **Legibilidade/estética:** testar em resolução de desktop e com leitura mínima nas ações básicas; mostrar capturas reais antes/depois do mesmo ângulo, com HUD e estado da cidade.

**Relatório de cada entrega:** o que mudou na experiência em linguagem de jogador; quais reclamações deste documento foram melhoradas (evidência do Godot); quais não foram; onde há decisão bloqueada; o que será testado pelo responsável. PR tecnicamente válido pode ser parcial, **não** equivale a missão completa.

### Instrução para o coordenador ao iniciar no Orca

Após a integração desta documentação na `main`, ler AGENTS, SPEC, exploração e este documento; **não refazer a descoberta do problema nem pedir outro plano amplo**. Inspecionar código e build local só para confirmar alvos, reproduzir e dividir mudanças. Iniciar trabalho técnico já autorizado por entregas, com frentes independentes em paralelo quando realmente possível. Pedir aprovação somente para decisões relevantes listadas acima e mudanças que se revelarem necessárias; não inventar autorização porque esta proposta foi escrita pela IA. Claude produz amostras visuais delimitadas conforme AGENTS; revisão barata separada é opcional e depende de disponibilidade real do Orca. Preservar o relato como critério da missão até o responsável testar a nova experiência.
