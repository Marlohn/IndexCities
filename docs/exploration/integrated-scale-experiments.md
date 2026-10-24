# Escalabilidade do motor integrado — experimentos

> **Pesquisa exploratória isolada, sem autoridade de produto.** A fonte do produto é `docs/SPEC.md`; regras de trabalho em `AGENTS.md` e arquitetura em `docs/ARCHITECTURE.md`. Esta branch/PR não altera a V1 nem autoriza integração.

## Pergunta central

Qual é a velocidade **efetiva e causalmente correta** do `IndexCities.Simulation` atual, com população, trânsito, economia, serviços, eventos e histórico de pessoas reais do motor? O que impede aproximar uma cidade futura com **10.000 ou mais SIMs** de uma experiência de várias gerações em algumas sessões?

**80 anos em 6 horas é hipótese de benchmark**, não requisito aprovado da V1. O hardware de referência e o ritmo final permanecem em calibração. Nenhuma otimização pode omitir viagens, vidas, transações, estoques, ordem causal ou histórico factual.

## Fronteira e interpretação

- **Esta PR:** benchmark e diagnóstico do **núcleo integrado real** `src/IndexCities.Simulation` por `src/IndexCities.Headless`, com eventual comparação ao host Godot quando existir cenário confiável. Alterações de instrumentação/fixtures só nesta branch, sem alterar produção em `main`.
- **PR #51:** pesquisa E01–E29 de otimização de trânsito, independente e preservada; não adotar candidatos não validados automaticamente.
- **PR #55:** motor C# experimental próprio, simplificado; resultados de décadas e histórico não demonstram desempenho do motor integrado. Preservar a pesquisa e não portar sistemas indiscriminadamente.
- **PR #52/#53:** pesquisa separada; não tratar experimentos como comportamento aprovado.
- A fixture `cycle` usa construção e sistemas reais, mas é pequena; `grid` usa o mesmo `CitySimulation` **com atalhos artificiais de preparação**: conclusão instantânea de obras, capacidade de utilidades ampliada, orçamento elevado, e configurações sintéticas de mapa/famílias. É **teste de estresse técnico, não cidade representativa**. Não declarar prova de 10 mil habitantes a partir dela.
- O motor atual usa `Quantum=2` segundos; **80 anos** corresponderiam a cerca de **1,26 bilhão de passos** se mantido esse executor. Não extrapolar tempo de um único dia linearmente sem verificar aumento do custo ao longo do tempo, população e histórico.
- Teste sem Godot não mede renderização, responsividade visual nem custo da interface.

## Protocolo enxuto

1. Medir a **linha de base do motor real** em Release com cenários existentes `cycle` e `grid`, **duas repetições sequenciais por condição** no mesmo job/runner, seed e parâmetros fixados. Coletar tempo de aquecimento, população final, dias concluídos, tempo real/dia, aceleração efetiva, alocação, GC, memória, tamanho de save, custo save/load, hash e invariantes. A execução de um teste isolado não permite declarar equivalência causal com outra implementação.
2. Confrontar os resultados com a representatividade das fixtures; preparar o **menor cenário integrado com carga realmente exercida**, distinguindo residentes vivos, cidadãos em circulação, veículos efetivos, viagens, turnos, comércio, água/energia/esgoto e persistência. Não “atingir 10 mil” inflando números ou capacidade sem registrar a artificialidade.
3. Aumentar primeiro **carga e horizonte de forma discriminante**, com limites explícitos de custo/timeout e checkpoints, sem lançar 80 anos cegamente. Perfilar somente o gargalo que os dados apontarem. Uma candidata exige A/B reproduzível com estado causal, invariantes e cobertura adversarial, antes de considerar adoção.
4. Quando um gargalo estrutural demonstrado impedir progresso, documentar o limite e reavaliar abordagem. Não colecionar experimentos ou commits sem ganho de informação.

## Primeira execução — baseline C# real

Workflow isolado `.github/workflows/integrated-scale.yml`, disparado por **push de instrumentação/workflow relevante** nesta branch. Começar com `scale cycle` e `scale grid`, `--seed=42 --warmup=1 --days=1 --cap=300`, e grid `--rows=3 --length=400`. Duas repetições por condição; `cap` interrompe só **depois** do dia completo, por isso existe timeout do job. Nenhum número de desempenho é aceito até ler o GitHub Actions correspondente.

**Resultado inicial confirmado:** [GitHub Actions #40060955808](https://github.com/Marlohn/IndexCities/actions/runs/40060955808), SHA `25eebe02f2a5a9b5cea08f13ea530a70db6aa29c`, job `baseline` **success** (2026-10-24 04:07 UTC); compilação .NET 8.0.32 Release aprovada, quatro execuções completas, logs brutos no artefato `integrated-scale-baseline-raw`. Runner Ubuntu, `Environment.ProcessorCount=4`; os tempos são amostras de runner compartilhado, não hardware de referência do jogador.

| Cenário real do núcleo | Repetição 1 | Repetição 2 | SIMs vivos | Veículos existentes | Tempo simulado/s real (aprox.) | Alocação gerenciada/dia | Memória de trabalho reportada | Hash save igual? |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| `cycle`, seed 42, 1 dia medido depois de 1 dia aquecido | **2,53 s/dia** | **2,50 s/dia** | **47** | **42** | 34.176× / 34.579× | **851 MB** | 85–86 MB | **sim** (`7357FCCBAA99AA96`) |
| `grid`, seed 42, 3 linhas/400 m, mesma janela | **5,24 s/dia** | **5,30 s/dia** | **108** | **83** | 16.489× / 16.313× | **1.612 MB** | 90 MB | **sim** (`2D095BC89EB9A34D`) |

As duas medições de cada cenário diferiram aproximadamente 1,1–1,2% entre si; são **somente duas repetições**, não base estatística para declarar ganho. Ambas terminaram com `INVARIANTS OK`, `SAVE` e `SUMMARY days=1`; nenhum cenário possui divergência demonstrada. `cycle`: GC0 51/dia, GC2 1/dia; `grid`: GC0 96/dia, GC2 0/dia. Save/load no runner: `cycle` ~0,19–0,20 s / 0,12–0,13 s para ~0,7 MB; `grid` ~0,19–0,20 s / 0,14–0,15 s para ~1,0 MB. A preparação da fixture e aquecimento **não estão incluídos** no wall/dia.

**Comparação com hipótese de 80 anos/6h:** 29.220 dias exigiriam média de **0,739 s por dia simulado** (≈116.880× efetivos). Mesmo neste ensaio pequeno e simplificado, `cycle` mede 2,50–2,53 s/dia (~3,4× acima desse limite), e `grid` 5,24–5,30 s/dia (~7,1× acima). A saída `hours80y=21/20` e `43/43` do CLI **é projeção linear hipotética, não simulação de 80 anos**; não modela população mutável, dívida histórica, envelhecimento, congestionamento, crescimento do save ou novos sistemas. Estes resultados **não autorizam** concluir se 10 mil SIMs/80 anos caberão ou não: estabelecem apenas que a meta exploratória está longe até em cidades minúsculas.

**Limitação decisiva:** `grid` conclui obras artificialmente, aumenta capacidades de serviços e orçamento; `cycle` só tem 47 cidadãos. A linha `moving=0 travelling=0` no fim do dia NÃO comprova ausência de viagens durante o dia, mas **não há métricas de viagens/carga real durante o dia** nesses logs. A população em `grid` (108) não constitui cenário de 10k, nem valida trânsito congestionado. Invariantes + hash final iguais validam apenas estado final repetido de **mesma implementação**, não equivalência de uma otimização nem toda ordem causal interna. Também não há perfil por sistema, memória histórica de décadas, nem custo Godot/UI. Não atribuir o rendimento medido a uma arquitetura nova, já que nenhum algoritmo foi alterado.

**Próxima pergunta discriminante:** instrumentar **somente observação** de atividade ao longo do dia (movimentos reais, viagens e trabalho/produção/consumo), sem alterar regras da simulação; escolher cidade integrada representativa, ou demonstrar quantitativamente por que a fixture atual não serve. Depois ampliar população/carga progressivamente e perfilar maior gargalo real. Não gastar CI em mais uma repetição idêntica ou rodar décadas cegamente.

## Registro de resultados

Cada entrada nova deve trazer: hipótese; SHA da branch; workflow/job/log; fixture/seed/parametrização e atalhos; **população real e atividade efetiva**; repetições e dispersão; wall/CPU quando disponível, RAM/GC, dias/s real; invariantes/hash/checkpoints; falhas, limites e **decisão técnica**. Se o teste falhar, registrar causa; não declarar ganho por CI verde, nem substituir prova do motor real pelo laboratório da PR #55.

**Próxima decisão:** usar os números do primeiro CI para escolher entre corrigir a fixture, ampliar carga real ou perfilar o gargalo dominante. Não executar automaticamente teste de décadas antes dessa escolha.
