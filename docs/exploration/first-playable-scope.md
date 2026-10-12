# IndexCities — Primeira versão jogável (V1): recorte e dependências

> **EXPLORAÇÃO — não é fonte de verdade. Revisão humana: PARCIALMENTE REVISADO.** O responsável **aprovou expressamente em 2026-10-12 o recorte funcional da V1** proposto neste documento; a seção *V1 — primeira versão jogável integrada* da [SPEC](../SPEC.md) passou a ser seu contrato oficial. Esta exploração permanece material de apoio: explicações, sequências técnicas, cenários de teste e propostas de integração produzidas pela IA **não foram todas revisadas nem transformadas em requisitos**. Em caso de divergência, prevalece a SPEC.

## Intenção

**V1 = primeira entrega jogável integrada do modo Convencional em computador (Windows prioritário, mouse e teclado).** O objetivo é construir e observar uma pequena cidade economicamente real, com ações que provocam mudanças observáveis e podem ser corrigidas. **Não é a versão 1.0 comercial, nem uma SPEC paralela.** A visão completa da SPEC segue válida, inclusive os sistemas adiados para outras entregas.

**Critério de escolha:** menor ciclo que contém construção física + agentes reais + dinheiro finito + emprego/consumo + abastecimento/logística + leitura de causa e efeito, sem mecanismos mágicos para fingir os sistemas ainda não implementados. O objetivo não é obrigar a implementação simultânea: entregar incrementos integrados e verificáveis durante a construção da V1.

## Funcionalidades da V1 — recorte aprovado na SPEC

| Grupo | Recorte funcional aprovado para a primeira entrega | Limite explícito |
| --- | --- | --- |
| Mundo/controles | Mapa plano determinístico por seed, vegetação simples, rio/lago, conexão externa com rodovia existente; Godot 3D isométrico, câmera e seleção por mouse/teclado | Sem edição de relevo ou expansão de bordas; sem versão móvel/Criativo |
| Início/catálogo | Mapa urbano vazio; Prefeitura única opção inicial; confirmação de sua posição libera ferramentas imediatamente, sem concluir a obra | Sem missões/desbloqueios posteriores, em conformidade com a SPEC |
| Construção e ruas | Prévia/snap híbrido e validação de espaço; traçado de rua urbana 1+1 em linha/L com calçadas, postes; obras com custos, materiais reservados/importados, caminhões e mão de obra externa ou do Pátio; causas de paralisação e cancelamento | O perfil urbano 2+2, obras especiais e Centro de Materiais ficam para versões posteriores; a entrega direta de importação ao canteiro é **regra já aprovada**, não estoque global fictício |
| Catálogo de edifícios | Prefeitura; Pátio Municipal de Obras; casa residencial simples de baixa densidade; mercearia pequena; fazenda pequena; posto de combustível; instalações essenciais de captação/tratamento de água, tratamento de esgoto e **uma fonte eólica** de energia | São **tipos funcionais aprovados para a V1**, não o número final de modelos visuais. Edifícios e funções futuros da SPEC permanecem aprovados para expansão. Água captada deve ser tratada; não presumir que captação e tratamento sejam um único prédio sem verificar a SPEC/obter decisão |
| Dinheiro/agentes | Caixa da Cidade, compromissos e pagamentos reais, Reserva Global e invariância da oferta monetária; SIMs e empresas persistentes com dinheiro próprio, contratação, salários, consumo, propriedade e impostos aplicáveis; migrantes/empresas externas avaliam oportunidade sem garantia | Nenhum morador, comprador, empresa ou salário artificial para completar a cena; decisões avançadas de família, previdência, aluguéis empresariais, imóveis mistos etc. serão integradas mais tarde |
| Operação privada | Compra real de imóvel novo; empresa autônoma opera mercearia, fazenda, posto; fazenda produz Alimentos com terreno, trabalho, tempo, capacidade e estoque; mercearia compra localmente ou importa, recebe carga física; SIMs viajam para trabalhar/comprar e pagam com saldo real | Sem catálogo amplo de negócios, hotéis, indústria pesada, mineração, refinarias ou produção local de material de construção nesta entrega. Não inventar demanda/venda por existir prédio |
| Logística e mobilidade | Pedestres e rotas viárias reais para viagens disponíveis, caminhões externos de obra/mercadoria, caminhões locais de carga e abastecimento físico de Combustível importado com estoque de posto e custo; veículos com combustível real e restrições de acesso | Não incluir ainda ônibus, táxis, emergência, bicicletas/ciclovias, aeroporto ou rodovias novas. Não simplificar deslocamento real para teletransporte |
| Infraestrutura básica | Oferta efetiva de água potável e esgoto por capacidade/atendimento; energia eólica variável por condição ambiental; déficits e causas visíveis sem serviços fictícios | Estações eólicas não geram potência constante garantida; outras fontes de energia, poluição sofisticada, resíduos e serviços completos ficam para expansão. A ausência de atendimento não pode ser escondida |
| Interface/continuidade | Catálogo visual selecionável sem leitura obrigatória, estados de obra, inspeção de SIM/empresa/edifício, resumo financeiro com despesas reais, material em trânsito, alertas causais discretos; tempo pause/1/2/3 e calendário aprovado; saves de cidades e autosave configurável | Relatórios/controles avançados fora da entrega não aparecem como funcionalidades falsas; projetos não confirmados seguem persistentes se a ferramenta correspondente entrar na V1 |

**Observações de compatibilidade:** os sistemas acima só estarão “incluídos” quando preservarem todas as regras da SPEC **pertinentes ao comportamento efetivamente implementado**. Quando uma dependência aprovada que parecia secundária se mostrar necessária para esse ciclo (por exemplo compromissos, proprietários, combustível, funcionamento das lojas ou acesso dos SIMs), incorporá-la no recorte ou trazer ao responsável um ajuste explícito, jamais criar uma exceção muda. A inclusão de um domínio não afirma que todos os sistemas avançados do mesmo domínio já estão prontos.

## Fluxo demonstrável de uma partida

1. Em mapa vazio com conexão externa real, posicionar a Prefeitura. **Confirmar libera o catálogo**, mas a obra só progride após acesso e entregas reais, inclusive com equipes externas quando cabíveis.
2. Construir trecho de rua, Pátio e casas. Observar reservas de materiais, caminhões, custo já pago e obra parada se faltar acesso; completar sem construir instantaneamente.
3. Residência concluída entra no mercado; família candidata com recursos **pode** decidir se mudar (não é forçada), comprando com dinheiro vindo da Reserva Global.
4. Mercearia e fazenda (e posto quando houver abastecimento de veículos) são adquiridos **se houver empresa real e oportunidade plausível**, contratam SIMs acessíveis e operam segundo capacidade/recursos/caixa.
5. Alimentos são produzidos localmente ou importados, transportados a estoque da mercearia e adquiridos por SIMs reais; viagens, trabalho, salários, pagamentos a fornecedores, tributos e saldo do Caixa se alteram de modo auditável.
6. O jogador muda a cidade: acrescenta moradia, melhora ruas/infraestrutura ou resolve falta de trabalhadores, estoque, água, eletricidade ou caixa, e observa consequências sem missão obrigatória.

## Critérios de aceitação integrados (não são novas regras de produto)

- Construir sem rota é permitido quando o espaço for válido, mas **não progride por entrega inexistente**; o aviso mostra o motivo e a ligação viária habilita a entrega quando viável.
- Cada entrada/saída de dinheiro possui pagador e beneficiário corretos; total Caixa + SIMs + empresas + Reserva Global se conserva, e valores apenas comprometidos não são pagos duas vezes.
- Sem moradores/empregados/estoques reais, casa/mercearia/fazenda não parecem ocupadas e produtivas artificialmente. Falta de demanda pode impedir crescimento e deve ser diagnosticável.
- Compra e consumo de Alimentos e Combustível exigem estoque, viagem/entrega e pagamento; os caminhões/cidadãos não teleportam ou percorrem ruas desconectadas.
- Despesa pública, capacidade real de água/energia, situação de obra e principais causas de não funcionamento são inspecionáveis. Falta de recursos ou falência municipal não fecha automaticamente a partida.
- Uma cidade pode ser salva, reaberta, continuar simulando e reproduzir o mapa pela seed. O jogo pode ser utilizado com mouse e teclado e alguém que ainda não lê consegue reconhecer e posicionar edifícios básicos.
- Registrar medições de desempenho e falhas de integração **à medida que aparecem**, não somente após concluir toda a V1.

## Ordem de integração proposta (planejamento técnico, não marcos obrigatórios)

1. Mundo/câmera/edição urbana + Prefeitura/ruas/obra verdadeira/importação externa.
2. Pátio, dinheiro/Reserva, save e diagnóstico de materiais/finanças, mantendo o mesmo executável jogável.
3. Casa, SIM/família/migração, mercado/empresa/emprego e compras com importação física.
4. Fazenda/fornecedor local e comparação com importação; posto, combustível e entregas; infraestrutura básica de água/energia/esgoto.
5. Verificação da cidade funcionando ao longo do tempo, diferentes partidas/seeds, UX de construção e correção de inconsistências; nenhuma etapa exige um produto descartável separado.

## Fora da primeira entrega, sem revogação da SPEC

Catálogo completo de dez atividades privadas, alta densidade, prédios mistos, locação comercial, indústria/extração/refino, serviços públicos extensos (escolas, saúde, segurança, emergências, creches), transporte coletivo/sob demanda, veículos e meios adicionais, programas municipais, parques/atrações/turismo, ciclo familiar avançado, desafios e conquistas, modificações estéticas posteriores, pontes/viadutos, infraestrutura avançada, poluição/ambiente aprofundados e modo Criativo. **Isto é fila de integração, não cancelamento de decisões.** Se uma regra dessas for indispensável para o fluxo escolhido, revisar explicitamente a fronteira antes de codificar sua dependência.

## Validações e lacunas a tratar durante a integração

- **Recorte aprovado em 2026-10-12** e registrado oficialmente na [SPEC](../SPEC.md). Detalhes ainda não decididos, mudanças de escopo e conflitos materiais devem seguir o fluxo de decisão do AGENTS antes da implementação dependente; esta lista não é autorização para inventar exceções.
- **Contrato espacial/câmera de referência com a IA de assets:** dimensões, pivôs, vias, acessos e slots precisam ser pactuados antes de produção em massa; ver [briefing de assets V1](first-playable-assets.md).
- **Instalações de água e esgoto e efeito de ausência de utilidades:** conferir exatamente quais edifícios/etapas são necessários no catálogo do recorte, sem inventar estação universal; separar o que é escolha de produto do que é detalhe técnico reversível.
- **Escala e economia de bootstrap:** validar custo real das primeiras obras e capital de famílias/empresas. Se o modelo impedir iniciar uma pequena cidade com dinheiro físico legítimo, ajustar parâmetros delegados à calibração; não criar moeda, cidadãos ou produção falsos.
- **Transições do ciclo dos SIMs/empresas não incluídas ainda:** evitar telas e promessas de regras não implementadas; se a ausência de uma transição quebrar um sistema já integrado, tratar como dependência real e incluir ou rever escopo com aprovação.

## Trabalho em paralelo

- **IA do jogo:** desenvolve apenas o que a SPEC autorizar, testa entradas/saídas físicas/econômicas, publica cedo o contrato de escala, pontos funcionais e IDs visuais; não toma decisões de produto por conta da IA de arte.
- **IA de assets:** pode preparar referências originais, paleta, silhuetas, protótipos de famílias e modelos candidatos em paralelo **desde já**; só produzir catálogo dimensional definitivo após validação do contrato e integração de uma amostra. Não copiar assets nem herdar código ou desenhos de projetos externos.
- **Responsável:** já aprovou o recorte V1 na SPEC; decide eventuais mudanças de escopo e comportamentos importantes ausentes, além de revisar as entregas visuais no jogo, não apenas imagens isoladas.
