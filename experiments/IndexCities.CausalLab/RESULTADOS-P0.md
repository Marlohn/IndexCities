# P0 — resultados executados: tempo, recursos e eventos causais

> **Status: executado em C#/.NET 8 no GitHub Actions, com testes isolados e confronto de estado integrado.** Sem alteração de gameplay. **Não é um novo motor completo, nem demonstra 10.000 SIMs por 80 anos em 6 horas.** Experimento da [PR #53](https://github.com/Marlohn/IndexCities/pull/53); proposta e critérios globais na [PR #52](https://github.com/Marlohn/IndexCities/pull/52). A comparação real **continua executando todos os quanta físicos** de 2 s do jogo atual.

## Execuções reais

- **[Run 39813126021](https://github.com/Marlohn/IndexCities/actions/runs/39813126021)**: sonda Python, **P0 C# compilado/executado com `FixedPoint` real**, 2.000 testes aleatórios de delta constante, 434/434 testes do núcleo.
- **[Run 39813870002](https://github.com/Marlohn/IndexCities/actions/runs/39813870002)**: 600 testes de avanço de fome por evento versus **26.852.724 passos de referência**, 100% iguais para fome, estoque e instantes das refeições; C# e regressões passaram.
- **[Run 39814130455](https://github.com/Marlohn/IndexCities/actions/runs/39814130455)**: fixture real `Grid(42,3,400,true)` após um dia de aquecimento, com **108 SIMs e 83 veículos**; 24 checkpoints de hora em hora durante um dia completo, hashes exatos com `Advance(3600)`, `Advance(60)` e `Advance(2)`; save/load sem mudança e 434/434 regressões.
- **[Run 39814419258](https://github.com/Marlohn/IndexCities/actions/runs/39814419258)**: candidato mais recente do branch, acrescentando **1.500 cenários de duas a seis pessoas dividindo despensa com entregas datadas**; cada entrega deve acordar os consumidores e a ordem de SIMs deve resolver a última unidade. **Rodada em .NET 8 validada:** 1.500 cenários, **3.236 refeições comparadas**, eventos `instante:sim`, estoque e fome com **zero divergência**; 24 checkpoints da cidade integrada idênticos; **434/434 regressões passaram**. Artefatos e logs preservados no Actions. Este resultado ainda não prova velocidade para uma cidade completa.

## Cinco atalhos inválidos — testados com C# real

| Operação isolada | Referência passo 2 s | Atalho ingênuo | Conclusão |
| --- | ---: | ---: | --- |
| Produção, 10 deltas de 0,49 nano-unidade | 0 nanos | 5 nanos | Quantizar apenas no final cria estoque |
| Produção, 10 deltas de 0,51 nano-unidade | 10 nanos | 5 nanos | Quantizar apenas no final perde estoque |
| Poluição, 300 passos / 10 min | 0,31383218650738038121 | 0,31383218650737404898 (C#) | Exponencial de intervalo não replica 300 arredondamentos decimais |
| Energia do vento, 24h, capacidade nominal 100 | 59,354634294 | 58,259140884 (amostragem horária) | Amostragem grossa altera oferta energética |
| Consumo de alimento no limiar 12h | segundo 60 | segundo 120 | Checar só no fim do intervalo muda o instante do fato |

O decaimento de poluição calculado pela sonda Python dá um último decimal diferente na alternativa ingênua por causa de conversão/aritmética: **preferir os números medidos no C# do run real**; ambos refutam o atalho.

## Técnica validada para parte do problema

Para entrada constante e **sem qualquer limiar/alteração de entrada dentro do trecho**, `base + N * Quantize(delta_por_quantum)` reproduziu o cálculo passo a passo, inclusive em **2.000 segmentos aleatórios**. O código C# de `TemporalKernel` usou o limiar da alimentação para agendar somente o próximo instante de verificação. Com **600 casos / 26.852.724 passos**, os resultados foram iguais (sem suporte, neste ensaio, a viagem, saúde ou estoque compartilhado).

**Uma medida isolada de uma execução:** cálculo de fome 39,025 ms com passos contra 0,3016 ms por limiares (~129×). São operações baratas, com estados pequenos, sem movimentação, trabalho, serviços, filas, corrupção de save ou histórico. **Não anunciar 129× como melhoria de dia do jogo.**

`SharedPantryProbe` acrescenta consumo por ordem de entidade, múltiplos SIMs e chegadas de alimento em passos conhecidos, comparando registros `instante:sim`, saldos da despensa e fome exata. Não inclui dinheiro, loja, veículo, saúde ou todo o modelo familiar.

## O que a comparação integrada realmente estabelece

A fixture headless real manteve estado persistido **idêntico em 24 horas** sob tamanhos de chamada diferentes, com `SaveToJson`/SHA e `FromJson` corretos. Isso valida uma parte do **oráculo da comparação**, não a alteração dos algoritmos do núcleo. Tempo observado nas três variações: **3,054 s / 3,040 s / 3,189 s** por dia (uma única execução, ruído de runner; diferenças não demonstram otimização). Em todos os casos, os **43.200 `Step(2)` por dia foram executados**.

## Ponto difícil ainda sem solução comprovada

- **Vento, água, energia e poluição:** mudam continuamente e por quantização; a identidade matemática segura não inclui automaticamente composição de alocação, efeitos de saúde ou limiares.
- **Invalidação real:** trabalho, moradia, chegada, movimentação de veículos e alteração de serviços precisam cancelar/agendar reavaliações corretas, sem polling global.
- **Histórico, crescimento de 10k e 80 anos:** não foram executados neste laboratório, muito menos no produto completo.

**Decisão:** manter a abordagem de tempo discreto exato com horizonte apenas quando puder ser certificado; **não** usar a agregação ingênua dos cinco contraexemplos. Não promover P0 à aprovação do motor definitivo. E22 da outra sessão responde o risco separado do trânsito; não fazer merge automático.
