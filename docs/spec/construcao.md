# Construção

> Parte da SPEC do IndexCities — fonte de verdade de produto. Índice e regras gerais em [docs/SPEC.md](../SPEC.md). Histórico das decisões em [docs/decisoes/construcao.md](../decisoes/construcao.md).

## Em vigor no jogo (V1)

### Recuperação da interação da V1

- Ruas por dois cliques: o primeiro clique marca o início; movimento do cursor mostra o traçado, sua forma em L, ligação e custo.
- O segundo clique encomenda o trecho exibido, sem botão separado de confirmação.
- Um trecho inválido ou sem saldo não cria obra nem pagamento e conserva o início para corrigir o fim.
- Depois do sucesso, a extremidade final vira automaticamente o início do próximo trecho; a mesma ferramenta continua ativa.
- Tab alterna o L; o primeiro clique direito curto cancela só o traçado e mantém a ferramenta; outro clique sem início ativo volta à seleção.
- Esc encerra a ferramenta.
- Essas ações não cancelam obras já encomendadas.
- O arrasto direito continua girando a câmera.
- Materiais, orçamento, acesso e construção física continuam reais; a continuidade não encomenda trecho sem outro clique e o esboço continua sujeito à persistência já aprovada.
- Ligações entre ruas em obra: permitir ligações e cruzamentos entre traçados já encomendados, cobrando e construindo cada ponto uma única vez.
- O encaixe e a prévia consideram tanto ruas prontas quanto traçados reais de obras ativas, distinguindo ligação planejada de acesso funcional.
- Pontos já pertencentes a outra obra ativa não são novamente encomendados ou cobrados no novo trecho.
- Apenas a rede efetivamente pronta concede acesso físico; obras dependentes aguardam a ligação real e cancelar uma delas pode deixar as demais desconectadas.
- Uma obra viária com pontos próprios já construídos aguarda a ligação real de todos esses trechos à rede exterior antes de ser marcada como concluída.
- Se a obra dona do ponto compartilhado for cancelada, a dependente continua aguardando até existir outra ligação real; não criar o ponto, duplicar cobrança ou concluir automaticamente para esconder o vão.
- Preservar materiais, equipe, pagamento, progresso, cancelamento e consequências reais; não concluir ruas ou criar conexões funcionais antecipadamente.
- Grade visual automática e G: ao entrar em construção de ruas ou prédios, exibir automaticamente uma grade de alinhamento no terreno.
- Oferecer botão e atalho G para mostrar ou ocultar essa grade durante o uso; ao sair da construção, ocultar a exibição automática.
- G também permite mostrar ou ocultar a grade fora do modo de construção.
- A grade é somente apresentação: não modifica encaixes, subdivisões lógicas, posições válidas, custos, acesso, colisões ou geografia; suas linhas não afirmam que uma posição é edificável.
- Demolir pelo catálogo: oferecer uma ferramenta Demolir no catálogo.
- Clicar num trecho urbano abre o mesmo orçamento de remoção e os efeitos reais sobre acessos e rotas; a remoção continua exigindo confirmação, com os mesmos custos e consequências.
- A ferramenta não remove rodovias nem apaga obras, propriedades ou recursos por atalho.
- A remoção pela inspeção permanece disponível.
- Giro da câmera por mouse: arrastar com o botão direito gira a câmera; um clique curto com esse botão cancela a ferramenta, com a exceção para ruas: primeiro cancela só o traçado, depois retorna à seleção.
- Esc cancela a ferramenta.
- Manter Q/E e os demais controles existentes.
- Cities: Skylines II serve como referência de familiaridade, sem autorização automática para trocar outras teclas ou gestos.
- Prévia de prédio em 3D: a ferramenta mostra o modelo 3D transparente antes da confirmação, com entrada destacada, no lugar de depender apenas do retângulo de terreno.
- A prévia representa tipo, posição e rotação, sem criar obra, edifício concluído, propriedade, população, estoque ou compromisso antes do clique que encomenda.
- Prédios por um clique: após escolher o tipo, o modelo transparente acompanha o cursor e mostra entrada, ligação, validade e custo antes da ação.
- Um clique no terreno encomenda a obra na posição exibida, sem confirmação separada; o mesmo tipo e rotação ficam ativos para repetir a construção.
- Um clique inválido ou sem saldo só informa a causa, sem criar obra/cobrança.
- Botão direito ou Esc cancela a ferramenta.
- Reposicionamento de esboços usa a mesma prévia/custo antes do clique e mantém as obrigações e limites físicos vigentes.
- O gesto não elimina entregas, equipe, tempo ou pagamentos reais.
- 1x em tempo real: um segundo real avança um segundo simulado na velocidade normal.
- Movimento físico, relógio, obras, turnos, produção e consumo seguem a mesma escala; as velocidades rápidas permitem avançar.
- Aplicar também ao retomar cidades existentes, sem alterar o horário já alcançado, posições, recursos ou obrigações.
- Os dois multiplicadores rápidos continuam sendo calibração técnica.
- Velocidades provisórias: as velocidades passam provisoriamente a 1x, 30x e 1000x (passos iguais em escala: cerca de 30 vezes entre cada uma); a mais rápida em 1000x (medição em [ritmo-velocidades.md](../exploration/ritmo-velocidades.md): estável em cidade pequena; 3000x inviável), até a revisão separada do ritmo.
- Não manter uma aceleração oculta de 15 vezes no controle normal.
- Identificação da versão e FPS: mostrar na interface a identificação da compilação em execução e o FPS real de renderização.
- A identificação permite distinguir branch/commit e alterações locais da compilação; não consultar apenas o checkout atual e apresentá-lo como se fosse o código já carregado.
- Informação de diagnóstico, sem alterar tempo ou regras da simulação.
- Nomear e renomear cidades: oferecer nome opcional ao criar uma cidade, separado do código que gera o mapa.
- Se o nome ficar vazio, manter o rótulo inicial `Cidade <seed>`.
- Permitir renomear cidades salvas mantendo o mesmo arquivo e identificador, toda a simulação, câmera, preferências e esboços.
- Renomear não cria outra partida nem altera o mapa, horário, dinheiro, recursos ou agentes.
- A confirmação informa o resultado real do salvamento; uma falha não deve anunciar sucesso nem trocar o nome da partida aberta.

### Ferramentas de construção mais fluidas

- As regras de custo, materiais, obra física, acesso e validação continuam as mesmas; muda o gesto e o auxílio de posicionamento.
- Prédio vira sozinho para a rua: com um tipo de prédio selecionado, quando o cursor está perto de uma rua urbana, a prévia gira automaticamente para que a entrada fique voltada para a rua mais próxima e encaixa alinhada a ela.
- R continua girando manualmente; longe de ruas, vale a rotação escolhida.
- Encaixe junto a ruas em obra: o mesmo auxílio de posição e orientação considera também o traçado de ruas já encomendadas, ainda não concluídas.
- O traçado serve de referência espacial; não concede acesso funcional.
- Enquanto não existir uma rua utilizável e ligada à origem necessária, a prévia permanece âmbar e informa que a obra aguarda acesso.
- Validade física, custo, reservas, equipe, entregas e progresso continuam sujeitos às regras reais; sobrepor o prédio à obra viária continua inválido.
- Fileira por arrastar: com um tipo de prédio selecionado, segurar o botão esquerdo e arrastar ao longo de uma rua mostra uma fileira de prévias encaixadas lado a lado, com quantidade e custo total.
- Soltar encomenda cada prédio como uma obra normal e independente, com o próprio custo, reserva, materiais e equipe; nada é concluído ou ocupado por isso.
- Um clique simples sem arrastar continua encomendando um único prédio.
- Aviso forte de obra sem rua: continua permitido encomendar um prédio fisicamente válido sem acesso viário, mas a prévia e o canteiro deixam esse estado claramente visível como obra parada até haver rua.

### Grade lógica e posicionamento assistido

- Correção geral de posicionamento: o auxílio de todos os prédios usa as bordas e dimensões reais do lote, considerando a rotação, em vez de arredondar uniformemente seus centros.
- Próximo de rua pronta ou planejada, alinhar a frente à borda da via por uma regra comum ao catálogo, preservando espaço físico e acesso reais.
- A grade de ruas deve pôr o nó escolhido no centro da célula apontada, mostrando a ocupação antes do primeiro clique; as subdivisões dos prédios devem permitir reconhecer suas bordas.
- Tamanho e apresentação da grade seguem calibração integrada.
- Preservar dimensões, largura viária atual, orçamento, materiais, rotação, L/Tab e continuidade.
- Não exigir que os 10,5 m da rua equivalham a células inteiras, nem reposicionar prédios existentes automaticamente.
- A prévia, a obra e a geometria pronta precisam concordar.
- É ação direta de construção, sem política municipal ou configuração nova.
- Modelo híbrido: o mapa usa uma grade lógica interna para organizar e consultar ocupação do espaço, validade de posicionamento e relações espaciais relevantes, enquanto a colocação de prédios e vias pelo jogador é assistida por encaixes e alinhamentos, sem impor que toda construção esteja visualmente presa a uma célula inteira ou a uma orientação única.
- O posicionamento permite liberdade limitada e controlável de ajuste e orientação, desde que respeite a ocupação física efetiva, evite sobreposição inválida e mantenha conexões e acessos coerentes com a simulação.
- Uma prévia visualmente alinhada não pode ser tratada como acesso viário ou conexão funcional garantidos.
- Distinguir invalidez física de falta de ligação viária: sobreposição/ocupação fisicamente inválida impede a colocação e informa o motivo; a ausência de rua/rota de entrega não impede por si confirmar um projeto de prédio fisicamente posicionável, mas a prévia precisa avisar que a obra ficará sem progresso até haver acesso real.
- A obra não pode presumir caminhão, equipe ou mercadoria capazes de atravessar trechos desconectados.
- O encaixe deve ajudar, não dificultar o controle: a ferramenta permite ajustar a posição sem uma atração excessiva a pontos de snap.
- Formas exatas de alternar/reduzir o snapping, tamanho da grade, subdivisões para detalhes de vias, geometrias/rotações permitidas e representação interna da ocupação ficam para calibração e validação no jogo integrado, sem introduzir uma ferramenta de desenho totalmente livre como requisito.
- O modelo híbrido não substitui a ferramenta de rua ortogonal em L vigente nem autoriza automaticamente curvas livres; também não elimina restrições atuais de rodovias, níveis sobrepostos, acesso e logística.
- Consultas espaciais e validação devem ser proporcionais às operações relevantes, evitando verificações geométricas globais contínuas sem necessidade.

### Ferramenta de construção de ruas

- Encontros previsíveis: a ferramenta evita pequenas dobras acidentais perto do alinhamento.
- Perto do alinhamento, a ponta livre prioriza uma reta; L/Tab continuam disponíveis quando seus braços comportam a ocupação da rua.
- Um encaixe com rua pronta ou em obra não pode ser deslocado silenciosamente para forçar uma reta; se a curva não couber, mostrar a restrição antes de encomendar.
- Ao aproximar a ponta de outra via, o auxílio pode levá-la ao encontro real válido, com a prévia e orçamento do traçado resultante, sem fundir paralelas nem conceder conexão apenas por superfícies próximas.
- Medidas e tolerâncias seguem calibração integrada.
- O auxílio funciona com prédio antes da rua e na ordem inversa, sem referência frontal azul.
- Não alterar largura, prédios, acesso, orçamento, materiais nem traçados já encomendados para produzir o resultado.
- É a ação de construção existente, sem nova política ou opção de gameplay.
- A experiência continua em avaliação humana.
- Cancelar somente o traçado: durante a ferramenta de rua, um clique direito curto cancela o início/fim ainda não confirmados (inclusive o início de continuação) e mantém a ferramenta pronta para escolher outro início.
- Um novo clique direito curto, sem início ativo, retorna à seleção.
- Arrastar com o botão direito continua girando a câmera; Esc continua saindo da ferramenta.
- Cancelar o esboço não cancela uma obra encomendada nem altera custos ou materiais.
- É o gesto da ação de construção existente, sem configuração ou política municipal adicional.
- Recuperação controlada da ferramenta: preservar o L/Tab e a continuidade entre encomendas; tornar legível a ocupação efetiva da rua em relação à grade e aos prédios e coerentes a prévia, a obra e a geometria pronta.
- Superfícies encostadas ou sobrepostas não equivalem a conexão: ligações em T, cruzamentos e encontros com ruas prontas ou em obra precisam corresponder ao grafo real, com indicação clara de posições inválidas.
- Não ampliar perfis, alterar medidas globais sem evidência, antecipar acesso por obra pendente nem modificar saves, custos, materiais, entregas e trânsito para facilitar a demonstração.
- Trata-se da ação direta de construção já existente, sem nova política municipal ou configuração obrigatória; calibração do auxílio espacial continua sujeita à validação integrada.
- O traçado estreito/sobreposto é recusado.
- A recusa deve aparecer antes da encomenda, preservando T e cruzamentos válidos.
- Interação: a ferramenta de ruas permite criar um trajeto ortogonal em L por dois cliques, no início e no fim, quando os pontos diferirem nos dois eixos.
- O clique e arraste não é o gesto principal.
- Depois do primeiro clique, o cursor mostra a prévia completa do trajeto; o segundo clique fixa essa prévia, antes de confirmar a obra.
- Quando houver duas formas possíveis de fazer o L, o jogador deve conseguir alternar ou influenciar de forma simples qual lado recebe a curva.
- Se início e fim estiverem alinhados, o mesmo fluxo de dois cliques pode produzir um trecho reto; o objetivo não é obrigar curvas artificiais, mas evitar exigir que o jogador construa cada perna do L separadamente.
- A confirmação da rua deve respeitar as regras normais de obra, custo, materiais, terreno e demais restrições da simulação.
- Operações sobre ruas concluídas: no primeiro modelo o jogador constrói novas ruas ou demole ruas existentes.
- Não existe ferramenta vigente para modificar, reformar, modernizar ou substituir uma rua construída em uma só operação.
- Caso deseje uma configuração diferente, usa demolição e construção com os custos e consequências normais de cada ação; não inventar uma etapa automática de alteração ou obra sobre rua ativa.
- Regras de desvios e acesso por remoção efetiva continuam sujeitas à circulação física existente, sem aprovar por inferência um modo de interdição parcial durante uma suposta ferramenta de modificar rua.

### Construção e obras

- Planejamento urbano sem compromisso econômico: no modo Convencional, o jogador pode desenhar antecipadamente múltiplos prédios e ruas como projetos visuais transparentes, revisar suas posições e apagá-los antes de confirmar.
- Projetos não executados não existem fisicamente para SIMs, tráfego, serviços ou empresas, não dão acesso, capacidade ou efeitos urbanos e não reservam, gastam nem transferem dinheiro, materiais, pessoal ou propriedade.
- O jogador confirma cada prédio ou trecho/obra individualmente para convertê-lo em pedido de obra normal, sujeito naquele instante à verificação real de lugar, diagnóstico do acesso (com obra podendo aguardar ligação viária conforme a regra específica), custo, disponibilidade, reservas e demais regras vigentes; não há execução automática de lista inteira.
- Revisar um esboço não é desfazer uma obra/compra já realizada.
- A prévia pode indicar impactos e orçamento estimado sem prometer recursos futuros.
- As regras desta seção sobre custos, materiais, trabalhadores, entregas e obras físicas regem o modo Convencional, único em desenvolvimento agora.
- Exceções de construção para o futuro Criativo ficam adiadas e não devem ser implementadas por antecipação.
- Sem ampliação ou upgrade de edifício já concluído no modelo inicial: o jogador não dispõe de comando para aumentar o porte, a área, a quantidade de vagas ou a capacidade estrutural de uma construção pronta, nem para trocá-la por uma versão evoluída na mesma operação.
- Para obter capacidade adicional, constrói outra unidade real com os custos, espaço, materiais e obras normais.
- Esta restrição não impede reparos de danos, demolição, realocação assistida, nem adaptações privadas de atividade vigentes que preservem o porte/estrutura e respeitem custos físicos, pois não são upgrades de capacidade do edifício.
- Possíveis expansões/evoluções de prédios permanecem em exploração, não como funcionalidade inicial.
- Prédios e infraestrutura não aparecem instantaneamente prontos.
- Ritmo de construção orientado ao gameplay: quando materiais, acesso e equipe estão disponíveis, a execução da obra deve ser relativamente rápida e sem espera artificial longa apenas por realismo.
- A duração total continua sujeita a entregas físicas, deslocamentos, disponibilidade e capacidade real das equipes, porte da construção e gargalos logísticos; esses fatores produzem atrasos reais e explicáveis, sem teletransporte de materiais ou construção instantânea.
- Obra fisicamente válida sem rua/rota de entrega: é permitido confirmar uma construção de prédio quando a posição é fisicamente válida mesmo que não haja acesso viário real para os caminhões, inclusive para a primeira Prefeitura.
- O pedido e seus compromissos econômicos continuam sujeitos às regras normais, mas a obra não avança enquanto não houver uma rota efetiva e viável para entregar os materiais e fazer chegar a equipe necessária da origem correspondente ao canteiro.
- Mostrar aviso visual claro e contextual na prévia e durante a paralisação, por exemplo «Obra parada: sem acesso para entrega de materiais», distinguindo falta de rua de falta de estoque, dinheiro, equipe ou congestionamento.
- Ao ligar a obra à rede de vias realmente utilizável, o fornecimento, deslocamentos e avanço podem retomar automaticamente somente quando as demais condições reais também forem atendidas, sem gerar entrega retroativa, material, trabalhador ou viagem fictícios.
- Não tornar conexão viária uma trava arbitrária da liberdade de posicionar; inviabilidade geométrica/ocupação continuam impedindo confirmação.
- Não criar sistema adicional de residência isolada por causa desta regra.
- Múltiplas obras podem avançar em paralelo dentro da capacidade real disponível, enquanto o jogador realiza outras atividades; não exigir gerenciamento manual de cada entrega ou trabalhador para fazer uma obra progredir.
- Se uma obra parar ou atrasar significativamente, o jogo mostra a causa concreta de forma simples.
- Tempos exatos por porte, taxas de progresso e efeito quantitativo das equipes permanecem para calibração e validação do jogo integrado, sem fixar agora faixas de segundos/minutos nem criar um modo alternativo de construção.
- Construções passam por uma fase real de obra.
- Toda obra possui custo em dinheiro e materiais.
- Obras consomem materiais reais da cidade.
- Obras dependem de trabalhadores reais.
- No fluxo normal, as equipes do Pátio Municipal de Obras são formadas por cidadãos empregados pela prefeitura; no bootstrap inicial, uma equipe externa temporária pode executar as primeiras obras.
- Materiais de construção precisam estar fisicamente disponíveis e reservados para a obra antes de serem consumidos, seja na oferta local elegível da cidade ou já entregues no canteiro.
- No início de uma cidade, materiais podem ser importados pela conexão externa usando o dinheiro inicial.
- O custo apresentado da obra indica explicitamente quando materiais faltantes são importados e quanto essa importação aumenta o custo monetário.
- Materiais precisam chegar fisicamente ao canteiro de obras, normalmente por veículos de carga, antes de serem consumidos pela construção.

### Pavimentação física das ruas

- A obra de rua inclui entrega de Asfalto por basculante que descarrega na pavimentadora efetivamente presente, aplicação pela pavimentadora e acabamento por rolo compactador. As máquinas e a equipe executam trabalho real no canteiro: o pavimento aparece atrás da pavimentadora conforme a execução efetiva, e o rolo finaliza o trecho aplicado. Não substituir essa descarga por entrega anterior numa baia distante enquanto a pavimentadora ainda está no exterior, nem concluir por animação independente de materiais, equipe, máquinas ou acesso.
- No início, as máquinas vêm com uma equipe externa pela conexão exterior, com deslocamento e disponibilidade físicos e custo incluído e discriminado na obra. Não exigir aquisição de máquinas municipais para construir a primeira rua. Uma futura frota própria do Pátio não integra esta decisão.
- Máquinas externas mantêm identidade, localização, ocupação e recursos operacionais reais durante chegada, trabalho e saída. Não criar máquinas adicionais para aparentar atividade nem atender duas obras incompatíveis com o mesmo equipamento. Custos efetivos de contratação/operação usam dinheiro real e destinatário econômico legítimo; não cobrar novamente despesas já incluídas na equipe, no frete ou nos materiais.
- A logística de chegada respeita o acesso de obra já aprovado; circular e trabalhar dentro do canteiro não torna a rua em construção utilizável pelo tráfego normal. Trechos fisicamente finalizados de uma rua ainda em obra podem servir exclusivamente ao acesso real de caminhões, máquinas e equipes de outras obras, inclusive para viabilizar ligações em T e cruzamentos compartilhados; carros comuns e entregas sem finalidade de obra aguardam a conclusão da rua. Não atravessar trechos desconectados, base ainda não finalizada ou pontos apenas planejados, nem abrir uma rua de terra intermediária.
- O jogador desenha e confirma a rua; fornecimento, máquinas e execução são automáticos. Falta de material, equipe, equipamento, verba ou acesso limita o avanço e mostra a causa concreta. A rua abre ao trânsito somente ao concluir a obra (ou, no cancelamento, apenas os trechos já compactados, conforme abaixo), preservando as regras de ligação e de trechos compartilhados.
- Marcação de canteiro, base, aplicação de asfalto e acabamento tornam o progresso real perceptível no mundo, sem confirmações manuais por etapa nem espera extra apenas para prolongar a animação. Materiais continuam consumidos uma única vez na chegada ao canteiro, conforme a regra vigente; essas aparências não criam um segundo estoque ou consumo.
- Duração e distribuição do trabalho, porte das máquinas e valores de contratação/operação são calibração dentro deste contrato: execução ágil quando os meios reais estão presentes, sem controle de temperatura de asfalto ou nova cadeia industrial obrigatória na V1.
- Ao cancelar uma rua antes da conclusão integral, os trechos já compactados pelo rolo abrem ao trânsito como rua comum, com as mesmas consequências de um trecho concluído (calçadas, manutenção e acesso), e podem ser demolidos depois pelas regras normais. Pontos só planejados ou fisicamente inacabados não viram rua. As regras vigentes de pagamentos, reservas e materiais consumidos continuam valendo; mover uma rua inacabada aplica a mesma regra ao trecho pronto no local anterior.

### Demolição

- **Ferramentas por estado e propriedade (decisão humana de 2026-10-17):** prévias não confirmadas podem ser ajustadas ou descartadas livremente; obras incompletas oferecem Mover e Cancelar pelas regras vigentes. Depois de concluída, a Prefeitura oferece somente **Mover sede**, sem Demolir isoladamente. Outros prédios municipais oferecem **Mover** e **Demolir**. Prédios privados oferecem **Demolir com indenização**, quando houver proprietário real; a realocação de imóveis privados fica adiada para reavaliação da experiência, preservando as regras da visão futura sem implementá-las na V1.
- **Consequências claras antes de confirmar:** identificar o alvo e apresentar custo de remoção, indenização separada, proprietário beneficiário ou ausência de indenização, total que sairá do Caixa e disponibilidade real. Mostrar famílias/moradores, atividade e empregos afetados, estoque e risco de perda, impactos sobre capacidade dos serviços e acesso/entregas quando pertinentes e calculáveis. Explicar se a retirada é imediata ou aguarda desocupação, sem prometer nova moradia, continuidade empresarial ou reembolso. Ícones e destaque do alvo acompanham frases curtas; detalhes ficam consultáveis sem confirmação por SIM. Cancelar a prévia não gasta nem altera a cidade. Confirmar revalida orçamento, propriedade, ocupação e impactos relevantes; se mudaram, reapresentar a prévia antes de executar. Depois da ordem, a inspeção mostra andamento e resultados reais, inclusive indenização paga e eventual espera pela saída dos moradores.
- A execução física da demolição de prédios e vias é instantânea no escopo atual.
- A ordem sobre uma residência ocupada pode, porém, abrir o período de desocupação assistida definido abaixo: a remoção física só ocorre depois da transição, sem exigir uma obra de demolição prolongada.
- Remoção de rua que isola construções: o jogador pode demolir uma via mesmo que ela seja o único acesso viário de casas, atividades ou serviços, sem exigir rua alternativa construída antes da confirmação.
- A prévia da demolição deve alertar de forma clara, resumida e verificável que o corte da conexão real poderá isolar edifícios e afetar moradores, entregas, obras, empregos ou emergências, com indicação dos locais/quantidades relevantes quando calculável, sem fingir certeza sobre efeitos futuros incertos.
- Se o jogador confirma, a remoção respeita custo, eventuais indenizações/ocupantes e regras de demolição vigentes; rotas, entregas, viagens e serviços passam a respeitar imediatamente a rede remanescente, sem teletransporte, desvio inventado ou bloqueio administrativo adicional.
- Reconectar vias pode restabelecer operação quando demais recursos estiverem presentes; reutilizar checagem de conectividade existente, não recalcular toda a cidade por quadro.
- Demolição tem custo configurável.
- Se um prédio pertence a um SIM ou empresa, uma ordem municipal de demolição deve respeitar também a regra de intervenção sobre propriedade privada abaixo; a ferramenta não pode ignorar a indenização ou as consequências sobre ocupantes e operações.

### Cancelamento de obras e demolição

- Ao cancelar uma obra ainda não concluída, materiais apenas reservados e ainda não entregues/consumidos deixam de ficar reservados.
- Materiais que já chegaram ao canteiro foram consumidos e não retornam ao depósito.
- Destino físico de materiais em trânsito após cancelar obra: cancelar uma obra não destrói, duplica nem muda por si só a propriedade de cargas de materiais ainda em caminhões.
- Material que já pertence legitimamente à Prefeitura, mas não foi entregue/consumido no canteiro cancelado, busca estoque municipal com capacidade e acesso reais, mediante redirecionamento e percurso físico; sem destino viável, não fingir estocagem instantânea.
- Material ainda pertencente ao fornecedor tem retorno ou destino conforme seu contrato e logística reais, sem apropriação pública automática.
- Pagamento efetivo anterior não é estornado automaticamente e compromissos ainda não pagos continuam liberados pelas regras existentes; contabilizar separadamente propriedade, dinheiro, localização e estoque físico, sem pedir ao jogador que gerencie cada caminhão.
- Dinheiro: cancelar uma obra ainda não concluída libera o valor monetário reservado e ainda não pago, que continua pertencendo ao Caixa da Cidade.
- Gastos efetivamente pagos a fornecedores, trabalhadores ou contrapartes externas não são reembolsados automaticamente.
- A Reserva Global não recebe dinheiro apenas comprometido.
- A interface informa antes de confirmar o cancelamento quanto orçamento será liberado e quais custos e materiais já foram perdidos.
- Demolir uma construção já realizada não devolve o dinheiro nem os materiais originalmente gastos.

### Reposicionamento de obra ainda não concluída

- O jogador pode selecionar uma obra inacabada e usar Mover para escolher um novo local ou traçado, com prévia do posicionamento, acessos, custo e consequências antes de confirmar, sem precisar cancelar manualmente e procurar novamente o item no catálogo.
- O reposicionamento equivale, para as regras econômicas e materiais, a cancelar o projeto no local anterior e criar a obra no novo local em uma única ação assistida; deve reaproveitar essas regras existentes em vez de criar transporte instantâneo de prédio, mão de obra ou materiais.
- O novo local inicia sua própria execução e passa pelas validações normais de ocupação, acesso, custos, equipes e logística; trabalho/progresso físico da obra anterior não são transferidos magicamente.
- Sem gastos nem entregas efetivas, mover é gratuito: libera ou reatribui reservas antigas e compromete o orçamento do novo local sem pagar multa ou duplicar retenções.
- Com gastos ou entregas já ocorridos, permanecem as perdas previstas no cancelamento: dinheiro efetivamente pago não é estornado, materiais já entregues ao canteiro foram consumidos; reservas ainda não pagas/entregues são liberadas ou recalculadas para a obra no novo local.
- O jogo apresenta antes da confirmação as perdas já ocorridas, os custos adicionais e a necessidade de orçamento disponível.
- Pedidos pagos, caminhões e materiais já em trânsito não podem ser duplicados nem teletransportados ao novo endereço: devem continuar rastreáveis e receber tratamento logístico coerente na implementação, sem reembolso fictício nem microgerenciamento do jogador.
- **Em aberto:** O mecanismo de redirecionamento/encerramento de entregas e a recalibração detalhada dos custos ficam para validação técnica.
- Prédios e infraestrutura já concluídos não são abrangidos pelas regras de Mover obra inacabada.
- Para prédios municipais concluídos, vale Mover conforme a seção abaixo. A realocação de prédios privados concluídos está adiada para reavaliação; não é requisito de implementação da V1.
- A retirada de moradores de residências ocupadas já segue a regra de desocupação assistida vigente, com duração curta ainda a calibrar; isso não define automaticamente a mudança de estoque ou funcionários.

### Mudança de prédios municipais concluídos

- Mover organiza uma substituição física em outro endereço numa ação guiada: selecionar o prédio, escolher o destino, conferir custos e consequências e confirmar. O novo prédio exige obra, materiais, equipe, acesso, dinheiro e tempo reais. Não transportar instantaneamente a estrutura nem cobrar indenização por patrimônio municipal.
- A Prefeitura mantém ferramentas de construção e gestão liberadas durante a mudança de sede. A sede antiga e a substituta representam uma única instituição em transição; a substituição não permite encomendar Prefeituras independentes adicionais. A interface de gestão não cria capacidade administrativa ou atendimento fictícios.
- O serviço antigo continua funcionando enquanto estrutura, acesso, recursos e necessidade espacial da intervenção permitirem. A transição preserva identidade institucional, com capacidade e salários/vagas condicionados aos recursos e à presença física reais, sem duplicar atendimento, funcionários ou estoque. A mudança efetiva do trabalho permite a reavaliação autônoma dos empregados já aprovada.
- A retirada do endereço antigo aplica custos de remoção e regras físicas/econômicas vigentes; a prévia apresenta separadamente construção, remoção, compromissos e perdas previsíveis. Eventual interrupção tem causa e capacidade perdida visíveis. Não inserir fechamento ou espera artificial quando a continuidade for viável.
- Estoques e cargas seguem recuperação/transporte físico possível e perda residual real conforme as regras vigentes; não surgem no destino automaticamente. A prévia informa os riscos pertinentes. Transição e obrigações persistem no save/load sem repetir cobranças nem criar uma segunda sede.

### Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos

- Intervenção com indenização automática: para preservar a liberdade de redesenhar a cidade, o jogador pode determinar a demolição de uma construção concluída mesmo quando ela já pertence a um SIM ou empresa privada, sem precisar solicitar aprovação ou negociar individualmente com o proprietário. Realocação privada permanece na visão aprovada, adiada para reavaliação; não é uma ferramenta disponível na V1.
- Isso é uma intervenção municipal, não uma movimentação gratuita do patrimônio de terceiros.
- Propriedade e dinheiro reais: antes da retirada de um imóvel privado, a cidade deve pagar indenização monetária ao proprietário econômico registrado (SIM ou empresa), usando dinheiro disponível do Caixa da Cidade.
- Indenização = 100% do valor de mercado atual do imóvel, calculado imediatamente antes da intervenção, com as condições da localização original ainda válidas.
- É o mesmo modelo de avaliação imobiliária já definido para compra e venda; não há desconto, sobretaxa, multiplicador ou negociação de valor na indenização.
- A própria intervenção/demolição não pode reduzir antecipadamente a avaliação usada para o pagamento.
- A fórmula geral do valor de mercado permanece sujeita à calibração conforme a seção de moradia e finanças públicas.
- Se não houver dinheiro para a indenização e demais custos obrigatórios, a intervenção não pode ser executada.
- O dinheiro é transferido ao proprietário real, não à Reserva Global, preservando a oferta monetária fixa; a propriedade antiga deixa de existir com a retirada do ativo.
- Beneficiário único da indenização imobiliária: paga-se o valor de mercado integral do imóvel uma vez ao proprietário privado registrado; não se paga o mesmo valor uma segunda vez por ser uma realocação assistida ou por o proprietário também morar nele.
- Em imóveis concluídos sem proprietário econômico real (inclusive patrimônio não reclamado), não há indenização fictícia à Reserva Global; continuam possíveis custos físicos reais da intervenção.
- A indenização por perda do imóvel não inclui ajuda financeira adicional automática ao inquilino nem cobre por si só custos de nova construção ou transferência de estoque, que seguem seus fluxos econômicos próprios.
- Momento do pagamento: a indenização é transferida integralmente ao proprietário real no instante da confirmação da intervenção, antes da desocupação, da retirada física e de qualquer alteração da avaliação provocada pela própria intervenção.
- A confirmação só é permitida com dinheiro existente suficiente no Caixa da Cidade para a indenização e os custos obrigatórios conhecidos; não há crédito fictício nem retenção na Reserva Global.
- O proprietário já pode usar o dinheiro para comprar ou alugar outro imóvel durante a transição.
- Imóvel indenizado aguardando retirada: se uma residência ainda estiver ocupada, sua estrutura e o vínculo de propriedade/locação continuam registrados apenas pelo tempo necessário à desocupação assistida, marcados como intervenção confirmada e indenização paga.
- O imóvel fica fora de novas compras, revendas e ocupações, não pode gerar segunda indenização e não deve aparecer como patrimônio negociável disponível além do dinheiro já recebido.
- O aluguel do ocupante atual continua devido ao proprietário registrado apenas enquanto o imóvel existir e a locação estiver vigente; ao fim, encerram-se ocupação e aluguel futuro, preservando dívidas preexistentes.
- A transferência antecipada não teletransporta moradores ou garante casa nova.
- **Em aberto:** Uma eventual operação de desfazer intervenção já indenizada não está definida e não pode criar devolução monetária automática ou duplicação de propriedade.
- Dono não é necessariamente ocupante: quando uma casa está alugada, o proprietário é quem recebe a indenização pelo imóvel, mas a família locatária sofre o deslocamento.
- Quando é moradia própria, a mesma pessoa pode ser proprietária e moradora.
- Famílias desalojadas entram no fluxo já definido de busca automática por outra residência compatível com seus recursos; se não houver, podem ficar sem moradia ou emigrar conforme as regras atuais.
- A indenização ao proprietário não garante nova casa nem concede dinheiro fictício ou compensação automática ao inquilino.
- Desocupação assistida em intervenção municipal residencial: se houver moradores numa residência escolhida para demolição ou realocação por ordem do jogador, a confirmação inicia um período de transição curto e de andamento automático.
- Durante esse período, a família pode continuar no endereço original enquanto a simulação procura outra moradia existente, disponível e financeiramente compatível, sem exigir que o jogador escolha o destino ou negocie com cada SIM.
- Na busca durante a desocupação assistida, as famílias deslocadas têm a primeira oportunidade de adquirir uma casa vaga compatível, antes de novas famílias migrantes. A aquisição usa recursos reais e as regras normais do mercado; a prioridade não garante moradia, não concede desconto nem mantém uma casa reservada quando a família não pode adquiri-la. A entrada de novos moradores pode ser adiada por essa preferência.
- Uma opção encontrada não é residência gratuita: compra ou locação exige as transações reais já definidas.
- Conclusão da desocupação: se os moradores desocuparem antes do prazo, a intervenção pode prosseguir sem esperas artificiais.
- Ao fim do prazo curto, a intervenção não fica bloqueada indefinidamente por falta de outra casa: a família que não se mudou deixa o imóvel e passa ao fluxo vigente de ausência de moradia ou eventual emigração.
- Não inventar imóvel substituto, saldo, migração garantida nem obrigação municipal de reassentamento individual.
- Uma residência ainda ocupada não pode ser fisicamente demolida antes de resolver esse deslocamento, nem manter moradores num prédio já removido.
- Aluguel, proprietário e indenização durante a transição: enquanto o imóvel original continuar existindo e ocupado, mantém-se a propriedade registrada e a locação vigente, quando houver, com aluguéis mensais ao proprietário real conforme as regras gerais, sem suspensão, perdão ou cobrança dupla automática.
- A indenização de 100% do valor de mercado imediatamente anterior à intervenção é paga uma única vez ao proprietário registrado no momento da confirmação da ordem, antes do começo da busca/retirada; pagar a indenização não equivale a dar a casa nova ao morador.
- O imóvel continua temporariamente registrado e bloqueado para comércio enquanto durar a desocupação assistida, sem nova indenização.
- Depois da desocupação/retirada, não nasce aluguel futuro de um imóvel inexistente, e dívidas já vencidas seguem sua regra própria.
- A liquidação econômica e a remoção do ativo precisam conservar a oferta monetária e impedir proprietário/ocupantes duplicados.
- Prazo e escopo: a transição deve ser curta para não transformar correção urbana em espera punitiva; o número exato de dias de simulação fica para calibração antes da implementação dependente dele.
- Ela é distinta dos prazos de três meses já definidos para inadimplência, imóvel sem proprietário e compra por novo dono para moradia própria: não substitui nem estende automaticamente esses casos.
- **Em aberto:** A regra vale para residências ocupadas afetadas por intervenção municipal; a continuidade condicionada da operação antiga de empresas/serviços durante a realocação está vigente; seus detalhes de paralisação, transferências e aquisição da construção substituta continuam abertos.
- Leitura de impacto: na prévia da ordem e enquanto a desocupação estiver pendente, apresentar de modo agregado o número de famílias afetadas, o valor da indenização e o estado da mudança, sem pop-ups obrigatórios por família ou cálculos globais contínuos.
- Casos de falta de moradia devem ser visíveis como consequência real, não como bloqueio manual de toda a reforma.
- Empresas também são afetadas: se a intervenção atingir comércio ou indústria privada, a indenização é paga à empresa proprietária e a retirada do estabelecimento tem efeitos reais sobre sua operação, empregados, acessos e estoques.
- Não presumir continuidade automática, teletransporte de estoques ou transferência de empregados/empresa para um novo imóvel.
- Impacto legível sem burocracia: antes de uma intervenção com consequências significativas, informar de maneira agregada proprietário atingido, estimativa de custo, famílias possivelmente deslocadas e operações afetadas; depois, permitir entender os resultados reais.
- Não exigir decisões manuais para cada família ou empresa nem calcular continuamente cenários de desapropriação sem uma ação solicitada.

## Visão aprovada (ainda não implementada)

### Construção e obras

- A cidade precisa possuir infraestrutura física de armazenamento, como Centros de Materiais de Construção, para materiais de construção.
- A cidade pode desenvolver produção local de materiais por meio de indústrias/fábricas apropriadas, reduzindo dependência de importações.
- As cadeias produtivas e a ordem de introdução dos insumos intermediários continuam em estudo na [EXPLORATION](../EXPLORATION.md).

### Intervenção municipal sobre propriedades privadas e realocação de prédios concluídos

- Intervenção no edifício misto: um edifício de uso misto é uma estrutura física única para intervenção/demolição municipal: a ordem sobre a estrutura pode afetar todas as unidades residenciais e comerciais, cada uma com proprietário e indenização devida pelo seu ativo real, exatamente uma vez, ao preço de mercado anterior à intervenção e mediante Caixa disponível.
- Respeitar desocupação residencial assistida, locações comerciais válidas, interrupção/realocação empresarial, pessoas, estoques e acesso reais, sem cancelar contratos silenciosamente nem exigir veto coletivo dos proprietários.
- Não criar demolição de apartamento isolado que preserve magicamente o resto do volume físico.
- O cálculo técnico da parcela de cada ativo não cria um terceiro imóvel indenizável.
- Casa substituta em realocação assistida residencial — primeira oferta prioritária ao antigo proprietário: quando a intervenção gerar uma nova residência privada em outro endereço, ela é imóvel novo, sem herdar automaticamente proprietário, contrato de locação ou moradores.
- Quando estiver fisicamente concluída e disponível para aquisição, o SIM proprietário anterior indenizado recebe a primeira oportunidade automática de decidir comprá-la, pelo valor de mercado vigente da nova localização; deve ser elegível, estar economicamente apto e usar dinheiro real, sem receber casa gratuita, desconto, reserva indefinida ou preço de indenização congelado.
- Se aceitar e puder pagar, a aquisição ocorre pelas regras normais, com primeiro pagamento à Reserva Global.
- Se rejeitar, não puder pagar ou não for elegível, a prioridade termina e a casa é oferecida no mercado residencial normal a famílias locais ou candidatas externas elegíveis, sem comprador ou ocupação garantidos.
- A avaliação de interesse/viabilidade ocorre no evento da primeira oferta, sem negociação manual ou consulta contínua a todos os SIMs; a prioridade não suspende as regras de moradia/desocupação nem garante que o antigo proprietário continue na cidade até a conclusão.
- O imóvel pode ficar vazio, materiais/obra/logística continuam reais e a compra não representa segunda indenização.
- Prioridade de compra não equivale à propriedade automática do novo imóvel.
- Continuidade operacional condicionada durante a realocação: quando o jogador usa Realocar sobre um estabelecimento concluído privado (comércio/indústria) ou serviço municipal, a operação no prédio antigo pode continuar normalmente enquanto ele ainda existir, estiver funcional e seu espaço/acesso não precisar ser liberado pela intervenção urbana.
- Se a intervenção exigir retirar ou tornar indisponível o endereço antigo antes de o novo estar apto a operar, a atividade naquele endereço é interrompida, com efeitos reais sobre atendimento, produção, emprego, clientes e finanças; a cidade não precisa bloquear indefinidamente a obra para preservar uma operação.
- A construção nova continua dependente de obra, materiais, trabalhadores, acesso e tempo reais.
- Estar pronta fisicamente não garante continuidade econômica da empresa, reabertura de serviço, comprador ou operador do novo prédio.
- Paralisação não obrigatória: Realocar não impõe uma etapa fixa de paralisação: buscar operação ininterrupta ou interrupção mínima quando a obra e a transferência puderem ocorrer com meios reais.
- Tempo de preparação/construção e tempo de atividade interrompida são diferentes.
- Se o estabelecimento antigo puder operar durante a construção, ele continua conforme a continuidade operacional condicionada; não parar apenas porque o jogador confirmou a mudança, nem inserir dias/segundos fictícios de fechamento para simular realismo.
- Quando a liberação antecipada do terreno, a ausência de acesso/recursos, a logística física ou a impossibilidade real de operar exigirem parada, a interrupção acontece e pode durar o tempo necessário, com consequências reais e causa legível.
- Não definir agora tempo máximo ou mínimo rígido nem garantir reabertura: obra, materiais, trabalhadores, estoque e operador seguem condições econômicas/físicas existentes.
- Nas empresas privadas, a paralisação segue a regra de vínculos e salários privados: contratos e salários permanecem enquanto a empresa sustenta e mantém as vagas, com reduções e insolvência pelas regras normais e liberdade de saída do SIM; não criar suspensão, retenção ou folha especial por causa de Realocar.
- Serviços municipais seguem a regra de vínculos e salários municipais: preservar vínculos/vagas e salários enquanto existirem e houver recursos reais do Caixa; SIMs permanecem livres para sair e reavaliam o vínculo na mudança efetiva do endereço, sem funcionamento fictício.
- Preservar o direito vigente de reavaliação do emprego na mudança efetiva de endereço.
- Demolição, indenização e propriedade na transição: a continuidade operacional condicionada é uma diretriz de Realocar, não um novo prazo obrigatório para uma ordem de Demolir diretamente.
- A demolição física permanece instantânea no momento em que pode ser executada, respeitando a desocupação residencial já definida, e encerra a possibilidade de operar no prédio retirado.
- Para ativo privado, a indenização integral continua sendo paga na confirmação da intervenção, uma única vez, mesmo que o estabelecimento funcione por algum tempo no endereço antigo.
- Enquanto aguarda retirada, o ativo permanece vinculado ao titular para operações existentes, identificado como já indenizado e indisponível para nova venda/indenização; funcionar temporariamente não restaura o direito de revendê-lo nem gera segundo pagamento.
- A continuidade operacional condicionada não altera a desocupação assistida das residências nem promete que a propriedade do imóvel novo será transferida ao antigo titular.
- Caixa, estoques e pessoas durante a transição: enquanto a atividade continuar de fato no endereço antigo, mantém-se sua economia real normal: receitas, insumos, custos, salários efetivamente devidos e movimentações físicas usam os mesmos agentes, regras e estoques.
- Se a atividade for interrompida, não há produção, vendas/atendimento ou capacidade daquele prédio inexistente ou inoperante; valores já devidos, ativos e vínculos não podem desaparecer ou ser quitados ficticiamente.
- Estoques permanecem quantidades físicas com localização/titularidade, não são copiados nem teleportados ao novo endereço; a recuperação possível e a perda na retirada seguem a regra vigente abaixo.
- Sequência e parâmetros finos de transporte continuam para calibração sem criar nova camada de logística.
- Durante paralisação de empresa privada, aplicar a regra de vínculos e salários privados: vínculos e salários são mantidos enquanto as vagas/contratos forem sustentados, sem desligamento automático, mas sujeitos a escolhas do SIM, redução normal de pessoal e falência.
- Paralisação de serviços municipais segue a regra de vínculos e salários municipais, com continuidade de vínculos/salários custeados pelo Caixa enquanto viável, sem obrigar o SIM a permanecer.
- Quando houver mudança efetiva de local de trabalho e vaga existente, aplica-se a reavaliação individual vigente para funcionários públicos e privados.
- Em serviços municipais, a continuidade física até a retirada pode preservar atendimento enquanto houver condições reais de funcionamento; a interrupção deve reduzir a capacidade efetiva, sem criar atendimento fictício.
- Jogabilidade, condições e limites: a ação assistida deve informar de modo agregado se a operação antiga pode continuar, a existência de obra nova, a possibilidade de interrupção e as consequências principais; não exigir administrar empresa, trabalhadores ou estoques individualmente.
- Mudanças relevantes de estágio/ocupação do terreno disparam verificações proporcionais por evento, sem varredura global constante.
- **Em aberto:** O critério concreto de compatibilidade espacial e o calendário fino de retirada, a coordenação operacional entre dois prédios, a continuidade da identidade econômica/institucional segue a regra vigente abaixo; a logística fina de estoque/cargas e a execução operacional da aquisição do novo prédio privado ainda exigem especificação antes de implementação dependente; os vínculos privados e municipais durante paralisação já estão definidos, sem transformar a continuidade operacional condicionada em garantia silenciosa de continuidade.
- Decisão da empresa sobre o endereço substituto: quando o jogador usa Realocar sobre uma fábrica, comércio ou outro estabelecimento de empresa privada, a empresa proprietária indenizada decide autonomamente se deseja voltar a operar no endereço proposto, considerando valor efetivo de aquisição, caixa e oportunidades, clientela, fornecedores, logística e mão de obra.
- O jogador define a intervenção física, não determina que a empresa aceitará operar na nova sede; ela pode considerar outras oportunidades disponíveis, não adquirir o destino ou deixar de operar naquele estabelecimento, com os efeitos financeiros reais.
- Essa decisão é distinta da escolha individual dos funcionários, vigente: mesmo que a empresa opte pelo destino, cada SIM pode reavaliar o próprio emprego quando o endereço de trabalho mudar.
- O prédio novo não herda automaticamente empresa, estoque, funcionários nem propriedade, e a empresa não recebe dinheiro ou prazo de sobrevivência fictícios; se não operar, não há produção/receita naquele local.
- A empresa proprietária original tem prioridade de primeira oferta para adquirir o prédio substituto, da mesma forma que um SIM antigo proprietário residencial: quando a construção ficar pronta e disponível, ela é a primeira a avaliar e decidir a compra ao valor de mercado vigente da nova localização, com seu próprio caixa real e elegibilidade econômica; a primeira aquisição paga a Reserva Global.
- Se não quiser, não puder comprar ou tiver encerrado definitivamente, o prédio segue ao mercado normal de empresas candidatas, sem reserva indefinida.
- A prioridade não garante venda, propriedade automática, operação, sobrevivência da empresa ou emprego para os funcionários; a empresa decide autonomamente se deseja comprar e operar, e cada trabalhador tem seu direito próprio de reavaliar o vínculo.
- A avaliação é pontual na oferta, sem novo sistema recorrente por agente.
- A oportunidade de decisão surge de um evento pertinente da realocação/oferta, sem consulta contínua por empresa.
- **Em aberto:** A continuidade de vínculos e ativos durante paralisações ou a sequência operacional exata seguem em exploração; não reabrir o princípio da decisão autônoma por causa dessas lacunas.
- Continuidade da entidade econômica/institucional na realocação: se a empresa original adquirir o imóvel privado substituto conforme sua prioridade de primeira oferta e as regras normais de compra, ela continua sendo a mesma entidade econômica, com seu caixa real, obrigações e histórico; mudar de endereço não encerra ou recria automaticamente a empresa nem exige recomeçar contratações e abastecimento do zero.
- Os vínculos de trabalho existentes só permanecem quando empregador, vaga e possibilidade efetiva de trabalho continuarem válidos; na mudança efetiva do endereço, cada funcionário mantém o direito à reavaliação individual já decidido.
- A operação no prédio novo só pode ocorrer quando existir aquisição válida, estrutura apta, acesso, trabalhadores, insumos, estoques e demais condições reais; comprar o imóvel não libera produção nem atendimento imediato fictícios.
- Mercadorias e outros estoques físicos mantêm titularidade, quantidade e localização rastreáveis: sua transferência, quando possível, usa a logística real existente, sem duplicação ou teletransporte e sem exigir ordens manuais de mudança do jogador.
- A operação antiga pode continuar durante preparação e transporte somente enquanto o endereço antigo efetivamente puder funcionar; não há operação dupla ou capacidade adicional gratuita.
- Se o prédio antigo for retirado antes de o novo operar, a empresa conserva identidade e caixa enquanto economicamente existente, mas perde a capacidade/receita daquele estabelecimento durante a interrupção e segue normalmente sujeita a custos, insolvência e encerramento definidos na SPEC; a prioridade de compra não suspende falência nem garante retorno.
- Serviços municipais realocados preservam a continuidade da instituição/serviço público, sem compra privada ou recriação institucional artificial, mas atendimento e capacidade dependem dos prédios e recursos realmente operacionais; vagas e funcionários só persistem se continuarem válidos, com reavaliação individual na mudança de local.
- Esta regra não cria novo subsistema de mudança, salários especiais ou transporte fictício.
- **Em aberto:** O princípio de recuperação e perda do estoque sem destino apto está definido abaixo; coordenação fina das entregas continua em exploração; vínculos durante interrupções seguem a regra de vínculos e salários privados para empresas privadas e a regra de vínculos e salários municipais para serviços municipais, sem retenção compulsória dos SIMs.
- Recuperação automática de estoque em realocação: ao realocar estabelecimento concluído com mercadorias ou suprimentos físicos modelados, a simulação tenta preservar automaticamente os bens existentes quando isso for viável antes da retirada física do prédio antigo, reutilizando estoques, destinos, transportes e transações normais.
- Quantidades elegíveis podem seguir para um endereço real com capacidade e acesso disponíveis, mediante transporte físico, ou ser vendidas/exportadas pelos canais econômicos vigentes somente se houver comprador/destinatário real elegível, pagamento real quando cabível, frete, rota e tempo viáveis; exportações continuam condicionadas à Reserva Global e suas regras.
- A operação não garante escoamento, compra externa, depósito temporário, transportador ou reembolso.
- Se a retirada não puder esperar e ainda houver quantidade física sem saída viável antes da remoção, esse estoque restante é perdido, baixado uma única vez da simulação e reconhecido como perda patrimonial/operacional de seu titular, sem criar pagamento, receita, indenização adicional ou retorno mágico do bem.
- O risco não bloqueia indefinidamente a intervenção, nem permite material persistir num prédio inexistente.
- Enquanto a estrutura funcionar, a atividade e o abastecimento seguem as regras existentes; não impor parada nem venda antecipada obrigatória se não forem necessárias.
- A prévia/confirmação da realocação sinaliza de forma agregada estoque afetado e risco de perda; na execução, o jogador pode diagnosticar quantidade preservada, vendida/exportada e perdida, com causas reais, sem comandar remessas, caminhões ou negociações individualmente.
- A regra não inventa inventário separado de máquinas/equipamentos nem se confunde com materiais já consumidos em canteiros de obra.
- Checagens por eventos relevantes, sem varreduras globais ou uma segunda cadeia logística de mudança.
- Experiência do jogador: realocação assistida de prédio concluído é a direção de interação escolhida para avaliar no jogo integrado: um comando guiado, com indicação de custo, propriedade e impactos, evitando que o jogador repita manualmente uma sequência longa de ações.
- Isso não dispensa custos de obra, materiais, logística nem indenização, nem move pessoas, dinheiro ou bens instantaneamente.
- As regras econômicas acima valem também quando a mudança for feita diretamente por demolição.
- **Em aberto:** Forma operacional detalhada da realocação pronta, logística fina de cargas em trânsito, coordenação física das transferências e detalhes de desocupação ainda não definidos acima seguem abertos para especificação ou calibração; vínculos/salários em paralisações seguem as regras de vínculos e salários para empresas privadas e serviços municipais, sempre preservando a autonomia dos SIMs; a continuidade da identidade econômica da empresa e institucional do serviço, quando aplicável, é vigente, sem assumí-los no código.
- A continuidade condicionada da operação do endereço antigo é vigente e não pode ser substituída silenciosamente por encerramento imediato nem por preservação garantida.
- Titularidade da casa privada substituta: imóvel novo, sem herança automática da propriedade antiga; após a conclusão, primeira oferta prioritária ao SIM proprietário original indenizado, pelo valor de mercado vigente, e somente depois mercado residencial geral se ele não adquirir.
- A desocupação assistida de residências ocupadas é vigente e não pode ser substituída por retirada instantânea dos moradores.
- Prévia de avaliação imobiliária durante realocação: ao posicionar uma nova localização para imóvel privado residencial, comercial ou industrial, a interface mostra discretamente a tendência de valorização ou desvalorização estimada e seus fatores principais, sem obrigar o jogador a acompanhar uma tabela de preços durante cada movimento.
- Na inspeção opcional e na confirmação da ação, o jogador pode consultar o valor de mercado atual do imóvel original, o valor estimado do imóvel na nova localização e a diferença estimada, acompanhados de causas concretas (por exemplo, serviços, acessibilidade, demanda e poluição) e identificação clara de que a nova avaliação é estimativa, não garantia.
- A confirmação deve manter visíveis custos obrigatórios reais, indenização ao proprietário e impactos sobre SIMs, distinguindo tais custos da projeção do valor futuro.
- A prévia usa o modelo imobiliário existente, sem multiplicadores, bônus monetários ou mercado fictício exclusivos da ferramenta; durante o posicionamento, atualizar apenas quando a localização/condições relevantes mudarem, sem exigir recomputação integral em todo frame.
- Para eventual compra futura, prevalece a regra vigente: valor de mercado efetivo na data da transação, mesmo se diferente da prévia.
- A indenização do imóvel antigo permanece definida pelo valor vigente na confirmação da intervenção.
- A prévia de preço não altera nem cria prioridade de compra: vale a primeira oferta ao proprietário antigo (SIM ou empresa) vigente acima, sem aquisição garantida ou mudança automática de propriedade.
- Para serviços e construções não privadas, informar custos e efeitos pertinentes, sem inventar preço de venda imobiliária ou comprador privado.

### Pontes, viadutos e sobreposição viária

- O jogador pode construir pontes.
- Ruas podem passar por cima de outras ruas por meio de viadutos/níveis sobrepostos.
- A malha viária deve permitir cruzamentos em níveis diferentes sem exigir interseção no mesmo plano.
