# IndexCities — Mapa de prontidão e decisões pendentes

> **Revisão humana:** PENDENTE — análise, percentuais e prioridades propostos por IA em 2026-10-08; o responsável aprovou registrar o mapa, **não** aprovou automaticamente suas conclusões.
>
> **Status:** instrumento de orientação para definição progressiva do produto e planejamento técnico, **não é fonte de verdade** nem checklist obrigatório de implementação.
>
> **Última auditoria cruzada:** 2026-10-08, após as decisões sobre realocação de estabelecimentos. Percentuais históricos/heurísticos, não métricas verificadas de implementação. Atualizar apenas diante de mudança material.

## Para que serve

Dar às próximas sessões uma visão única do que já está suficientemente definido, do que precisa de **decisão de produto**, do que é apenas **calibração técnica** e do que precisa de **evidência de protótipo**. Evitar reabrir decisões aprovadas ou transformar detalhes de baixo impacto em uma entrevista interminável.

**Ordem de autoridade:** [SPEC](../SPEC.md) determina o produto; [ARCHITECTURE](../ARCHITECTURE.md) determina as fronteiras de software; [AGENTS](../../AGENTS.md) determina como trabalhar; o código mostra o que está implementado. O [hub de exploração](../EXPLORATION.md) e este mapa registram pensamento não oficial. Se este mapa divergir da SPEC, **não** promover sua recomendação silenciosamente; identificar a divergência e corrigir a fonte apropriada conforme a decisão humana.

As prioridades são **orientação revisável**, não um impedimento para experimentar e tampouco autorização para implementar funcionalidades não aprovadas. A direção de trabalho é **Spec-Driven Development leve**: definir comportamentos oficiais na SPEC antes de implementá-los. A eventual POC é uma **validação integrada do jogo**; organizar a implementação em incrementos técnicos não cria uma obrigação de POCs separadas nem altera o escopo aprovado.

## Diagnóstico geral — fotografia de 2026-10-08

- **75% — estimativa heurística de preparação documental para iniciar a implementação integrada**, dando maior peso à arquitetura, à economia e à construção, que estão mais amadurecidas. **Não é média aritmética** das categorias, métrica científica, porcentagem da SPEC fechada nem fração do jogo pronta. A validação principal é do conjunto funcional, conforme a SPEC.
- **0% — implementação de jogo verificada no repositório na data da análise**: a árvore contém documentação e arquivos administrativos, sem código executável do jogo, testes ou medições reais. Isso não é uma estimativa do trabalho já realizado fora do GitHub.
- **Principal lacuna de validação:** preparar cenários reproduzíveis que permitam observar o funcionamento conjunto de construção, logística, economia, serviços e cidadãos, **sem transformar os cenários em cortes do escopo da implementação integrada**.
- **Principal risco:** sistemas detalhados, isoladamente coerentes, combinarem-se em uma experiência de microgerenciamento, desempenho ruim ou problemas difíceis de diagnosticar.
- **Importante:** não buscar “100% definido” antes de codificar. Fechar regras causais e comportamentos importantes; deixar fórmulas finas, limiares e otimizações para calibração baseada em evidências.

| Categoria | Prontidão estimada | Interpretação |
| --- | ---: | --- |
| Arquitetura e tecnologia | 90% | Godot/C#, monólito modular, Simulation Core separado, testes e observabilidade orientados. |
| Economia e finanças | 85% | Reserva Global, oferta monetária fixa e transações reais definidas; parâmetros iniciais pendentes. |
| Construção e materiais | 85% | Fluxo físico de obras, materiais, importação e equipes bem detalhado; calibração pendente. |
| Empresas e produção | 80% | Operadores, caixa real, preços híbridos, produção adaptativa e abastecimento decididos; ajuste fino pendente. |
| Cidadãos e moradias | 75% | SIMs reais, rotinas, patrimônio, consumo e moradia definidos em princípio; algoritmos e parâmetros pendentes. |
| Mapa e terreno | 65% | Seed, mapa plano, natureza e conexão exterior definidos; dimensões e grid ainda a validar. |
| Trânsito e movimentação | 60% | Comportamento desejado detalhado; representação, pathfinding e desempenho ainda não comprovados. |
| Interface e ferramentas | 55% | Intenção de uso clara; grande parte das soluções de interação está em exploração. |
| Cenários e critérios de validação | 40% | Falta definir como avaliar empiricamente o jogo integrado; não representa escopo de uma POC reduzida. |

**Como ler os percentuais:** são um retrato qualitativo de **prontidão para investigar/construir**, ponderado por julgamento, **não** uma média matemática, cobertura de requisitos, conclusão da SPEC, qualidade de código ou porcentagem de um jogo finalizado. A soma das categorias não precisa produzir o percentual global. Não recalcular por ritual após cada decisão; revisar quando houver mudança material.

## Prioridade P0 — tratar primeiro

P0 reúne os maiores riscos ou decisões que exigem atenção para **integrar e validar o jogo previsto na SPEC**, não para criar uma POC menor. **Nem todos os itens precisam estar fechados antes de qualquer código.** Cada item distingue decisão, calibração e experimento.

| Categoria | Natureza | Questão / próximo passo | Por quê |
| --- | --- | --- | --- |
| Cenários de validação do conjunto | **Decisão de plano**, não requisito novo | Descrever cenários observáveis do mapa vazio à cidade funcionando, abrangendo dinheiro, obras, materiais, trabalhadores, empresas e serviços, com critérios de diagnóstico. | Sem cenários e evidência, não sabemos se a implementação integrada funciona, mesmo que sistemas isolados pareçam corretos. |
| Espaço, grid e construção | **Modelo de produto decidido; validação espacial P0 pendente** | A SPEC escolheu **grade lógica e posicionamento assistido (modelo híbrido)**. Validar dimensões, ocupação física, snapping, acesso real dos prédios, encaixe do L ortogonal e custo de consultas, **sem reabrir grid rígido versus liberdade total por padrão**. | Uma base híbrida mal implementada pode gerar colisões, conexões falsas, frustração de controle e custo alto de pathfinding/consultas. |
| Bootstrap econômico e obras | **Calibração + teste de coerência** | Determinar saldos e custos de um cenário inicial reproduzível, sem violar a oferta monetária fixa, que já é regra; exercitar a primeira obra com equipe externa e a chegada de materiais. | Uma cidade vazia precisa conseguir começar sem soluções artificiais. |
| Mobilidade e logística | **Experimento técnico** | Selecionar representação provisória da rede, rotas e execução de deslocamentos/entregas reais; medir filas e custo de cálculo. O algoritmo definitivo permanece em aberto. | Caminhões físicos são fundamento de construção e mercados. |
| Interface mínima | **Decisão de experiência + protótipo** | Validar câmera, seleção, ferramentas de ruas/prédios, prévia de custo, andamento de obra e diagnóstico do motivo de bloqueios. | Profundidade sem leitura e controles fluidos não gera bom gameplay. |
| Ritmo da realocação e paralisação percebida | **Direção de gameplay decidida; casos longos e calibração em aberto** | A SPEC confirmou que **Realocar não exige paralisação obrigatória**: manter a operação antiga durante preparação quando viável e minimizar interrupção conforme condições reais. Distinguir tempo de obra, tempo sem serviço e espera percebida; **sem duração fixa nem novo subsistema de salários aprovado**. Testar gargalos antes de criar exceções para paralisações prolongadas. | Gameplay e microgerenciamento devem ser avaliados globalmente sem apagar custos, materiais ou consequências reais. |
| Bootstrap sistêmico da cidade vazia | **Direção de produto decidida; risco de integração P0 ainda aberto** | A SPEC já permite entrada em qualquer ordem por oportunidades concretas, sem compradores, empregos ou dinheiro fictícios. Validar cenário reproduzível de moradia → migração → empresas/contratação → infraestrutura/abastecimento e também a ordem inversa, testando liquidez real da Reserva Global, falhas e diagnóstico de vacância. | A regra reduz a dependência circular conceitual, mas ainda é preciso comprovar que há caminhos economicamente viáveis desde a cidade vazia, com água/energia, salários e logística reais. |
| Critérios de avaliação | **Decisão de teste / medição** | Verificar invariantes: dinheiro não surge nem desaparece; materiais são realmente entregues; obra não conclui sem recursos; seed reproduz cenário; gargalos são explicáveis; custo de simulação é medido. | Evita declarar êxito apenas porque algo aparece em 3D. |

**Possível ordem interna de implementação e feedback (não aprovada como plano obrigatório):** mundo e ferramentas de construção → obras, materiais e logística → cidadãos, moradias, empresas, serviços e consumo → avaliação do conjunto. Trata-se apenas de sequência técnica de desenvolvimento, **sem POCs separadas e sem retirar funcionalidades já previstas na SPEC da primeira entrega integrada**.

## Prioridade P1 — esclarecer quando os sistemas entrarem na integração

| Categoria | Natureza | Ponto em aberto |
| --- | --- | --- |
| Rotinas de cidadãos e domicílios | Calibração/protótipo | Ritmo de deslocamento, necessidades, estoque doméstico, trabalho e compras presenciais sem verificações caras por SIM. |
| Empresas e produção | **Calibração de decisão já aprovada** | Produção adaptativa com estoque de segurança **já consta na SPEC**. Testar frequência de ajuste, tamanho da reserva e reação a pedidos, vendas e capacidade; não reabrir sua existência sem motivo. |
| Mercado e preços | Calibração/protótipo | Parâmetros de preço por estabelecimento/categoria, frete, salários, impostos e ritmo das transações. O modelo híbrido global/referência local já foi decidido. |
| Moradia e migração | Calibração/protótipo | Fórmulas iniciais de preço imobiliário, compra/ocupação e elegibilidade de famílias externas; respeitar fluxo monetário da Reserva Global. |
| Demanda e avisos | Calibração de comportamento decidido | Limiar e duração dos alertas de vendas insuficientes, distinguidos de bloqueios logísticos; agregar avisos para evitar ruído. |
| Escala e performance | Experimento/benchmark | Medir cenários crescentes com SIMs, veículos e empresas antes de escolher população-alvo definitiva, scheduler ou estrutura de dados sofisticada. |
| Mover obra inacabada e cancelamento | **Decisões aprovadas; logística a validar** | Mover obra inacabada é ação única equivalente a cancelar e recriar; orçamento ainda não gasto é liberado, pagamentos feitos e materiais entregues permanecem consumidos. Precisam de tratamento físico correto as cargas pagas/em trânsito e o recálculo de reservas, sem duplicações. |
| Intervenção indenizada e desocupação residencial | **Produto decidido; prazo a calibrar** | Ao intervir em prédio privado, pagar **uma única indenização de 100% do valor de mercado antigo** no momento da confirmação; impedir revenda/segunda indenização enquanto aguarda retirada. Residência ocupada tem desocupação assistida curta. A casa substituta é **novo ativo**: **primeira oferta ao antigo SIM proprietário indenizado**, pelo preço de mercado novo; se não comprar, mercado geral. Não herda dono/moradores nem garante solução de moradia antes da conclusão da obra. |
| Realocação comercial e de serviços | **Autonomia e primeira oferta empresarial aprovadas; continuidade C; transição ainda aberta** | A empresa original decide autonomamente se deseja operar no destino e **tem a primeira oportunidade de adquirir o prédio substituto ao valor de mercado novo**, se ainda existir e puder pagar. Se não comprar, vai ao mercado geral. Os funcionários fazem suas próprias escolhas. Antigo estabelecimento pode operar enquanto viável (C), sem paralisação obrigatória. **Em aberto:** salários/vagas em interrupções, estoque, logística e sequência de transferência. Não presumir aquisição, reabertura, continuidade garantida ou teletransporte. |
| Emprego e prévia de avaliação imobiliária | **Escolhas aprovadas; parâmetros a calibrar** | Empregados públicos e privados reavaliam permanecer/sair **quando o endereço efetivo mudar**, só se empregador/vaga existirem (opção A). A ferramenta mostra tendência discreta e valores/custos detalhados sob demanda (prévia B); preço futuro é estimativa, não garantia. |
| Preferência de recompra do substituto | **DECISÃO APROVADA para SIM e empresa; implementação por evento** | **SIM proprietário antigo e empresa proprietária antiga** têm primeira oportunidade automática de compra de seus respectivos imóveis substitutos quando estiverem concluídos/disponíveis, **ao valor de mercado vigente da nova localização e com recursos reais**. Se recusarem, não puderem ou não forem elegíveis, oferta imediata ao mercado geral. Primeiro pagamento à **Reserva Global**. Não há transferência gratuita, reserva indefinida, compra garantida ou dispensa de efeitos da desocupação/falência. Regra anterior residencial sem preferência foi expressamente substituída na SPEC. |
| Estoque local e pagamento | **Validação de invariantes, detalhes por calibrar** | Ao reservar materiais de fornecedores locais reais, garantir origem física, titularidade, pagamento ao recebedor real, custo e não duplicação de estoque durante transporte e cancelamento. |
| Liquidez e Reserva Global | **Validação sistêmica, parâmetros por calibrar** | Conservação da oferta monetária **não** garante que cidade, famílias e empresas tenham liquidez suficiente. Testar capital inicial, fluxo de importação, primeira aquisição, formação de empresas e empréstimos quando o saldo da Reserva Global estiver baixo, sem inventar moeda. |
| Direção de gameplay e progressão | **Decisão de produto em aberto, pesquisa pendente** | Não há objetivo central, progressão ou endgame oficial; avaliar alternativas à luz do loop material e econômico, sem impor vitória por população nem tratar isso como requisito já escolhido. |

## Prioridade P2 — não antecipar decisões finas sem evidência

- **Mobilidade avançada:** faixas, ultrapassagens e casos complexos de tráfego pertencem à visão do produto, mas a otimização e o detalhamento incremental exigem medição.
- **Famílias e patrimônio:** refinar exceções de herança, aluguel, titularidade e inadimplência conforme o impacto real.
- **Serviços urbanos:** calibrar detalhes operacionais de hospitais, escolas, saneamento, energia, polícia e emergências conforme evidências; **os comportamentos já aprovados para esses serviços continuam na implementação integrada**.
- **Arte e apresentação final:** pipeline de assets, animações complexas e acabamento devem acompanhar necessidades demonstráveis, não antecipar todas as telas.
- **Arquitetura prematura:** ECS, multithreading, job system, event bus e assemblies adicionais permanecem adiados na ARCHITECTURE até haver necessidade medida.

**P2 não significa “fora da primeira entrega” nem “fora do jogo”**: apenas orienta a não fixar algoritmos, parâmetros ou exceções de baixo valor antes de haver evidência. O escopo de produto continua sendo o que está na SPEC.

## Auditoria de consistência — 2026-10-08 (atualizada após as decisões recentes)

1. **Corrigido — produção adaptativa.** A SPEC aprovou a **produção adaptativa com estoque de segurança**, mas uma frase posterior alegava que a política ainda não estava aprovada. O trecho foi corrigido para remeter ao modelo decidido; somente os parâmetros seguem para calibração.
2. **Conflito de proposta explicitado — cancelamento.** A [SPEC](../SPEC.md) determina que material entregue ao canteiro é consumido e não recuperável. A [exploração de interface](player-interface.md) contém uma hipótese de recuperação total para reduzir fricção, **não aprovada e incompatível com a SPEC atual**. O documento agora explicita essa diferença; não alterar o produto sem decisão humana. O cancelamento **monetário foi decidido depois desta auditoria**: comprometimento no Caixa da Cidade, pagamento real a seu destinatário e liberação da parcela ainda não paga. Reposicionamento/realocação continuam abertos.
3. **Corrigido — hierarquia da SPEC.** O título *Fora de escopo automático* aparecia antes de dezenas de subseções de produto oficial, fazendo-as parecer subordinadas a um título de exclusão. O bloco foi movido para antes de *Decisões de produto confirmadas*, preservando as decisões.
4. **Direção decidida; validação pendente — bootstrap econômico.** A SPEC agora admite entrada de famílias e empresas em qualquer ordem com base em oportunidades concretas, sem exigir emprego ou clientela já realizados e sem criar moeda/recursos/demanda. Isso responde ao risco conceitual de dependência circular, mas **não demonstra** que um cenário inicial tenha capital, infraestrutura, operadores e logística suficientes. Manter P0 para validação integrada, medindo causas reais dos bloqueios.
5. **Novas escolhas aprovadas após a auditoria inicial:** grade lógica híbrida, Mover obra inacabada, indenização no valor imobiliário e paga na confirmação, desocupação residencial curta, prévia B de avaliação imobiliária, reavaliação de empregados A e continuidade condicionada C no prédio antigo. **Não rediscutir essas escolhas como se estivessem abertas.** **A regra residencial foi atualizada depois desta auditoria:** antigo proprietário tem prioridade de primeira oferta, tal como a empresa indenizada, pelo valor de mercado novo; demais compradores só participam após não aquisição pelo titular anterior. A SPEC já reflete a decisão.
6. **Lacuna relevante — intervalo operacional da realocação:** após a retirada do prédio antigo ainda não estão definidos os efeitos específicos sobre continuidade institucional/econômica, vagas/salários, estoques e aquisição do novo prédio privado. O princípio C de operar no endereço antigo enquanto possível não resolve automaticamente o período restante. Duração física da construção, interrupção e tempo percebido pelo jogador não são a mesma coisa. **Questão P0 de gameplay e comportamento**, não mero detalhe de implementação.
7. **Risco de manutenção — extensão da SPEC e tamanho de tabelas do mapa.** A SPEC já reúne muitas regras detalhadas; manter a navegação enxuta e o mapa legível, sem fragmentação por cerimônia nem duplicação de decisões oficiais.

As correções históricas citadas são **editoriais/de consistência**; os pontos 4, 6 e 7 são riscos ou lacunas para discussão e medição, **não funcionalidades novas aprovadas**. O conteúdo desta auditoria continua **PENDENTE de revisão humana**.

## Revisão humana da exploração — 2026-10-07

O [hub](../EXPLORATION.md) registra 11 documentos temáticos: **9 PARCIALMENTE REVISADO**, **2 PENDENTE**, **0 REVISADO** integralmente. Os dois pendentes são [benchmark do gênero](genre-benchmark.md) e [IA e sistemas de decisão](ai-decision-systems.md). Revisão parcial **não** aprova toda a pesquisa do arquivo; seções podem ser ainda mais restritivas. Não exigir leitura integral dos 11 arquivos como pré-condição para iniciar experimentos — revisar material diretamente relevante ao tema da sessão.

## Como usar em outras sessões

1. Ler primeiro [AGENTS](../../AGENTS.md), [SPEC](../SPEC.md) e o [hub](../EXPLORATION.md); consultar [ARCHITECTURE](../ARCHITECTURE.md) quando o trabalho afetar software.
2. Antes de propor novas rodadas de perguntas da SPEC, verificar aqui a prioridade e conferir na **SPEC atual** se a decisão já foi tomada. Evitar repetir perguntas e distinguir decisão real de calibração.
3. Para cada item selecionado, pesquisar/consultar apenas o documento temático relevante. Explicar alternativas e consequências para **gameplay, microgerenciamento, performance e causalidade**, sem concordância automática.
4. Após decisão humana explícita, atualizar a fonte oficial adequada e ajustar a prioridade/estado deste mapa quando houver mudança relevante. Propostas e conteúdo gerado sem revisão ficam **PENDENTE**.
5. Tratar a fotografia e os percentuais como **históricos** até nova auditoria. Não declarar automaticamente que a prontidão aumentou porque uma tabela foi editada.

## Fontes desta avaliação

- [AGENTS.md](../../AGENTS.md), [SPEC.md](../SPEC.md), [ARCHITECTURE.md](../ARCHITECTURE.md), [EXPLORATION.md](../EXPLORATION.md).
- [Direção de produto](product-direction.md), [Escala e performance](simulation-scale.md), [Mapa e ambiente](world-map-environment.md), [Mobilidade e serviços](city-systems.md), [Famílias e moradia](households-housing.md), [Empresas e mercados](companies-markets.md), [Construção e logística](construction-materials-logistics.md), [Interface do jogador](player-interface.md), [Benchmark](genre-benchmark.md), [IA e decisões](ai-decision-systems.md) e [Processo de desenvolvimento](development-process.md).

**Próxima revisão recomendada:** após definir um cenário de validação integrado ou obter as primeiras medições de desempenho e gameplay; antes disso, atualizar apenas correções materiais.
