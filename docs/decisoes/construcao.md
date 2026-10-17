# Histórico de decisões — Construção

> Só registro histórico; as regras vigentes estão em [docs/spec/construcao.md](../spec/construcao.md).

- 2026-10-17 — Responsável aprovou a indicação: prévias livres; obras com Mover/Cancelar; Prefeitura pronta somente Mover sede, mantendo ferramentas e gestão; demais prédios municipais Mover/Demolir; privados com demolição indenizada, adiando realocação privada para reavaliar a experiência. Exigiu consequências sempre claras ao jogador, inclusive custos, indenização, beneficiários, ocupantes e momento da retirada; autorizou implementação em time supervisionado por Orca. Tarefas simples usam GLM-5.3 Flash Free e/ou DeepSeek V4.1 Flash Free; para tarefas complexas, esclareceu a autorização de GPT e autorizou Claude. A proibição geral de GPT/Claude registrada anteriormente era uma interpretação incorreta do coordenador. Mantidas conferência do coordenador e revisão independente. → seções Demolição e Mudança de prédios municipais concluídos.
- 2026-10-16 — Responsável escolheu A para a pavimentação: máquinas com equipe externa inicialmente e custo incluído na obra, evitando exigir frota municipal na primeira rua. A proposta discutida usa basculante, pavimentadora e rolo em trabalho físico automático, com abertura ao trânsito na conclusão; frota própria do Pátio fica para outra discussão. Autorizou depois o registro e a implementação orquestrada. → seção Pavimentação física das ruas.
- 2026-10-16 — No caso de rua parcialmente finalizada que dá acesso a outra obra, responsável escolheu permitir somente acesso de obras no trecho pronto. Tráfego comum permanece fechado até a conclusão; não vale para trechos só planejados ou fisicamente inacabados. → seção Pavimentação física das ruas.
- 2026-10-16 — Para rua cancelada antes da conclusão, responsável escolheu A: trechos já compactados abrem ao trânsito como rua comum e podem ser demolidos depois. Rejeitadas: mantê-los fechados só para obras (viraria asfalto inútil, pois não há retomada de obra) e removê-los (o asfalto pago sumiria sem remoção física). Desvantagem aceita: pode sobrar ponta sem saída, e mover rua inacabada deixa o trecho pronto no local anterior. → seção Pavimentação física das ruas.

- 2026-10-08 — A residência substituta é nova e oferece primeira compra ao antigo proprietário indenizado, sem herança automática; substitui mercado aberto sem preferência (primeira oferta prioritária) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — A operação antiga pode continuar na realocação enquanto houver estrutura, espaço e acesso reais, evitando fechamento antecipado desnecessário (opção C) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — Realocar não impõe paralisação fixa; vínculos e salários privados e municipais seguem as regras normais e a autonomia dos SIMs (opções C, 1A privada e A municipal) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — A empresa indenizada decide sobre adquirir e operar o substituto, com primeira oferta e sem garantir aceitação ou empregos (autonomia empresarial) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — A empresa que adquire o substituto e o serviço municipal preservam identidade, com capacidade, vínculos e estoques condicionados à operação real (opção C) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — Estoques são recuperados automaticamente pela logística viável, ou perdidos uma única vez se a retirada não puder esperar (opção C) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-08 — A realocação mostra tendência imobiliária discreta e valores opcionais estimados, sem mudar indenização, prioridade ou preço efetivo futuro (opção B) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-09 — Ruas concluídas só recebem demolição e construção novas, sem ferramenta única de reforma ou substituição (escopo confirmado) → seção Ferramenta de construção de ruas
- 2026-10-10 — O modelo inicial não amplia nem evolui a capacidade de edifícios prontos, evitando variantes de assets e ferramentas de upgrade desde o início (decisão confirmada) → seção Construção e obras
- 2026-10-11 — Projetos urbanos transparentes não comprometem recursos e cada obra recebe confirmação individual (escolha 10B) → seção Construção e obras
- 2026-10-11 — Prédio fisicamente válido pode ser encomendado sem rua, inclusive a Prefeitura, mas aguarda rota real de materiais e equipe (pergunta 6) → seção Construção e obras
- 2026-10-11 — A rua pode ser removida mesmo isolando edifícios, mediante aviso e consequências físicas reais (pergunta 9A) → seção Demolição
- 2026-10-11 — Cancelar preserva propriedade e rastreio das cargas em trânsito, redirecionando material municipal para estoque viável (revisão 6B) → seção Cancelamento de obras e demolição
- 2026-10-11 — A intervenção no edifício misto afeta a estrutura única e indeniza cada ativo real uma vez (escolha 19A) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- 2026-10-12 — Ruas usam dois cliques com prévia e encomenda no segundo, dispensando confirmação separada (fluxo confirmado) → seção Recuperação da interação da V1
- 2026-10-12 — Prédios usam modelo 3D transparente e um clique de encomenda, mantendo tipo e rotação para repetição (nova rodada) → seção Recuperação da interação da V1
- 2026-10-12 — A velocidade normal passa a um segundo simulado por segundo real, com calibração rápida inicial em 15x e 60x (nova rodada) → seção Recuperação da interação da V1
- 2026-10-12 — O pacote melhora giro automático, fileiras por arrasto e aviso de obra sem rua após a avaliação de ferramentas incômodas, obras paradas e repetição de casas (pacote 1, noite; mantém pergunta 6) → seção Ferramentas de construção mais fluidas
- 2026-10-12 — O L ortogonal por dois cliques substitui o arrasto como gesto principal (interação revisada) → seção Ferramenta de construção de ruas
- 2026-10-13 — O catálogo oferece Demolir e mantém orçamento, confirmação e remoção pela inspeção (aprovação humana) → seção Recuperação da interação da V1
- 2026-10-13 — Arrastar o botão direito gira a câmera e clique curto cancela a ferramenta, usando Cities: Skylines II apenas como referência de familiaridade (aprovação humana) → seção Recuperação da interação da V1
- 2026-10-13 — A interface identifica a compilação carregada e informa o FPS real (pedido confirmado) → seção Recuperação da interação da V1
- 2026-10-13 — Cidades recebem nome opcional e renomeação sem trocar partida ou anunciar sucesso após falha (aprovação humana) → seção Recuperação da interação da V1
- 2026-10-13 — O encaixe de prédios considera ruas encomendadas sem conceder acesso funcional (aprovação humana) → seção Ferramentas de construção mais fluidas
- 2026-10-14 — Após encomendar uma rua, a ponta final vira o início seguinte sem criar outro trecho até novo clique (continuidade aprovada) → seção Recuperação da interação da V1
- 2026-10-14 — Ruas em obra podem compartilhar ligações sem cobrança duplicada e só concluem após ligação real à rede exterior, inclusive depois do cancelamento de uma obra compartilhada (aprovação e conferência humanas) → seção Recuperação da interação da V1
- 2026-10-14 — A grade aparece automaticamente na construção e admite botão/G, inclusive G fora da construção por solicitação posterior sem data própria (aprovação humana) → seção Recuperação da interação da V1
- 2026-10-15 — O primeiro clique direito cancela só o traçado e o seguinte sem início volta à seleção, mantendo arrasto de câmera e Esc (ajuste do responsável) → seção Ferramenta de construção de ruas
- 2026-10-15 — O alinhamento comum usa bordas reais dos lotes, substituindo o ajuste exclusivo de 4 m no centro da Prefeitura (correção geral) → seção Grade lógica e posicionamento assistido
- 2026-10-15 — As pontas priorizam retas perto do alinhamento e encontros válidos sem minidobras, deslocamento silencioso de encaixes ou referência frontal azul (retornos do responsável) → seção Ferramenta de construção de ruas
- 2026-10-15 — A recuperação preserva L/Tab, continuidade e grafo real e é aceita para abrir PR sob correção do traçado estreito/sobreposto, com versão local, regressão e revisão independente (pedido e avaliação humanos) → seção Ferramenta de construção de ruas
- 2026-10-16 — As velocidades passam provisoriamente a 1x/30x/1000x até revisão do ritmo, após medição estável em cidade pequena e 3000x inviável (ajuste do responsável) → seção Recuperação da interação da V1

## Registros sem data explícita

A seção original não informa a data destas escolhas; nenhuma data foi inferida.

- Sem data explícita — O modelo híbrido usa grade lógica interna e colocação assistida com liberdade controlada (modelo híbrido aprovado, sem data explícita) → seção Grade lógica e posicionamento assistido
- Sem data explícita — A intervenção sobre propriedade privada dispensa negociação individual e exige indenização real (opção B, sem data explícita) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- Sem data explícita — A indenização integral é paga na confirmação antes da desocupação e retirada (opção A, sem data explícita) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
- Sem data explícita — A desocupação residencial é assistida, automática e curta, sem impedir a intervenção indefinidamente (opção C, sem data explícita) → seção Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos
