# Checkpoint de caminhões e pavimentação — 2026-10-16

**Entrega incompleta; não integrar como produto pronto.** Código congelado em `fcf6e896e36be74d0d79cad76a9f5c0aaef7defd`, branch `Marlohn/pavimentacao-integracao`. O coordenador pediu congelamento após persistência do bloqueio de fila; não houve nova tentativa de correção depois dessa instrução.

Implementação: carrocerias permanentes por veículo e carga real; pavimentadora/rolo externos com IDs, operadores, pagamento, combustível e trajeto persistentes; aplicação/acabamento físicos; acesso intermediário exclusivo para obras. A chegada consome material uma vez. Arte `170ad66`, integrada como `68805ee`, sem alterações minhas em `Assets/Construction`.

Correções após o primeiro checkpoint `7d4ab66`:

- Seis caminhões no total, sem os 18 veículos extras apontados pela revisão: três basculantes, uma betoneira, uma carroceria aberta e um cargueiro. Mantidos capacidade, tarifas e limite existentes.
- Migração fecha ao tráfego comum os nós de obras legadas ainda ativas, preserva IDs/material consumido e não reserva mais dinheiro ao carregar. Orçamento legado insuficiente permanece insuficiente, com causa explícita.
- Asfalto só é despachado para pavimentadora presente, com operador/combustível; o caminhão manobra até o funil real, descarrega e percorre a saída. Abastecimento ocorre na baia real da máquina fora da faixa; ela depois percorre o acesso até o trabalho. Quantidade e posição são estados do core, sem autoplay.
- Trânsito interno e retorno verificam ocupação física. Aplicação/compactação são sequenciais na faixa de trabalho. A movimentação de aproximação usa velocidade de trânsito; cobrança por trabalho ocorre no trecho executado. Cotação não duplica equipe genérica da rua.
- Verificação de autonomia antes da contratação e vínculo de cada máquina preservado até liberar a equipe. O teste com duas equipes impede reutilizar uma pavimentadora retornada enquanto o rolo da sua obra ainda trabalha.

## Fatos verificados

Raiz das evidências: `artifacts/verification/construction-paving-feeding/`, sempre nesta worktree. Nenhum save pessoal usado.

- Regressão atual: **214 passaram, 3 falharam, 217 no total**, 1 min 13 s. `regression-tests.log`. Comando: `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj --no-build --nologo --filter 'FullyQualifiedName!~CityCycleTests'`. O soak `CityCycleTests` não foi executado neste fechamento.
- Onze testes de pavimentação estão na suíte, cobrindo carrocerias/retorno/próxima viagem, bootstrap sem Pátio, conservação, nós fechados, acesso/equipe/dinheiro/combustível, concorrência, cancelamento antes da compactação, migração, save/load determinístico e duas equipes. O teste isolado novo de duas equipes passou: `two-teams-tests.log`.
- Host compilou com zero erros. Permanece o aviso preexistente CS8629 em `Main.ProblemAlertsQA.cs:198`. `build.log`. A última alteração posterior à compilação foi somente fortalecer a asserção da fixture `Main.NeighbourhoodQA` para exigir progresso parcial, ausência de conclusão e nós exclusivos de obras; essa fixture renderizada não foi executada neste fechamento.
- Godot **real renderizado**, RTX 3050 / GL Compatibility: `INDEXCITIES_CONSTRUCTION_PAVING_OK models=final captures=27`. `engine.log`, `evidence.txt`, 27 PNG com JSON de estado. O cenário está cadastrado em `-- --qa-list` e exige janela real. Capturas verificadas incluem descarga no funil (`straight-unload-Asphalt.png`), betoneira (`straight-unload-Concrete.png`), carroceria aberta (`straight-unload-Steel.png`), aplicação e rolo em L, retorno vazio das três carrocerias, parada causal sem combustível e conclusão.
- O smoke compara JSON completo após save/load em obra e após cancelamento, verifica dinheiro/material sem duplicação, duas máquinas físicas retornando e conservação monetária. O ramo sem combustível usa clone isolado; a continuação paga original não recebe combustível fictício.
- Proveniência visual: o rodapé das últimas capturas mostra `bcd5ad72` com alterações locais; são as alterações de simulação/visual agora commitadas em `fcf6e89`. Não atribuir as imagens a uma build limpa desse hash. O smoke foi executado antes do fortalecimento da asserção de outra fixture citado acima.

Comando renderizado executado (argumentos da engine antes de `--`):

```powershell
& 'C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe' --path src/IndexCities.Godot --resolution 1440x960 --position 80,60 -- --construction-paving-smoke '--review-save-dir=C:/Users/Marlohn/orca/workspaces/IndexCities/pavimentacao-integracao/artifacts/verification/construction-paving-feeding'
```

## Regressão técnica persistente

Os três testes de `PedestrianCrossingTests` falham em `CornerCity`, linha 15, **antes** de testar o comportamento dos pedestres: a casa não fica pronta após 7200 s. Reproduzido isoladamente em `corner-regression.log` e no programa mínimo `corner-probe/Program.cs`; estado completo em `corner-stalled.json`. Reproduzir: `dotnet run --project artifacts/verification/construction-paving-feeding/corner-probe/CornerProbe.csproj`.

Caso mínimo: seed 42; encomendar Prefeitura em (-152,16), rua de (-192,0) a (-128,132) com cotovelo invertido, casa em (-190,146); avançar 7200 s. Resultado confirmado: obra P26 com 49/49 pontos aplicados/compactados, ainda `InProgress`; rolo V30 `LeavingSite/Parked` em (-196.4,116), causa ocupação; caminhão V22 em (-193.65,119.4383448), causa fila. A rua continua fechada e Prefeitura/casa dependentes não concluem. Não é apenas prazo antigo da fixture. O detalhe geométrico exato do bloqueio mútuo ainda requer diagnóstico independente.

Asserções de conclusão foram mantidas; o diagnóstico foi acrescentado às falhas. As tentativas de retirar baias da faixa e desviar a saída resolveram os cenários simples e parte da regressão, mas não este caso. Alternativas para avaliação técnica, **não implementadas**: reservar entrada/saída compartilhada antes de admitir carga; ou reorganizar o trajeto de saída com envelopes completos e prioridade causal de desocupação. Não adotar atravessamento de veículos, teleporte, remoção da máquina ou abertura antecipada como solução.

## Decisões humanas ainda necessárias

1. Destino dos trechos já compactados ao cancelar uma rua. A SPEC registra a lacuna aberta. O código do checkpoint retém nós compactados fechados; isso não é uma decisão aprovada, e o teste de cancelamento atual acontece antes de o rolo construir nós. Não há validação de aceitação desse caso aberto.
2. Migração de caminhões genéricos antigos: seis carregados com asfalto tornam-se seis basculantes permanentes, deixando a cidade sem betoneira/cargueiro/carroceria aberta. Não criei caminhões nem implementei troca posterior de carroceria. Uma eventual exceção única após retorno ao exterior foi perguntada ao coordenador e ainda não tem decisão humana.

Limites adicionais: cotação é estimativa Manhattan e cobrança usa rota real; precisam ser avaliadas no jogo. A autonomia mínima evita contratar uma viagem já sabidamente impossível, mas não há reabastecimento remoto novo de máquinas. O smoke simples passou; ele não prova ausência de bloqueios em bairros ou geometria arbitrária. O desempenho de busca de pontos citado pela revisão não foi otimizado.

## Revisão e experiência

Revisão independente efetivamente realizada: **Claude Haiku 5.5**, fallback por indisponibilidade do DeepSeek V4.1 Flash, sobre o primeiro checkpoint equivalente `9e080f3`. Fonte: `C:/Users/Marlohn/orca/workspaces/IndexCities/caminhões-de-obra/artifacts/verification/construction-review-haiku/report.md`. O revisor fez leitura estática e sonda de frota; não executou novo Godot. Os dois achados altos foram corrigidos acima; o risco de ocupação agora tem reprodução. A correção atual ainda não recebeu nova revisão independente nem validação humana global.

Modelo do autor registrado pelo coordenador/revisor: **GPT-6.1-sol**; arte GPT-6.1-sol xhigh. Esta sessão se identifica como Codex/GPT-6 e não expõe uma variante adicional verificável. Não atribuir revisão ao DeepSeek.

Critérios de produto: regra automática sem novos cliques; a espera decorre de material/equipe/acesso/ocupação reais, com causas visíveis. A consequência para SIMs e empresas é acesso indisponível até concluir. IDs e fases existem para impedir equipamento gratuito ou simultâneo; nenhuma opção municipal ou mecânica adicional foi criada. A fila persistente prejudica diretamente a diversão de construir e impede declarar a entrega pronta.
