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
