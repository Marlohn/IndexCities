# Quantidades acumuladas em inteiros — retomada de 2026-10-19

Task `task_bcdedccc2f4b`, Dispatch `ctx_887e347ab053`. Branch `Marlohn/quantidades-inteiras`. Correções e verificações desta retomada concluídas; revisão independente final e avaliação humana continuam com o coordenador.

## Alvo e base de produto

Pedido recebido: integrar a main com o guincho da PR #38, corrigir os bloqueios e ajustes baratos de `review.md`, refazer a comparação pareada com a main atual, executar suíte completa e Godot reconstruído, fazer push e abrir PR para main sem merge. A revisão anterior de `a9625b4` foi feita por Claude Haiku 5.5; esta retomada corrige seus achados e não substitui a revisão final independente pedida para depois do PR.

Contrato em `docs/spec/mundo-tempo-saves.md`: quantidades acumuladas em inteiros, mudança única de arredondamentos, preservação de regras, limiares e ordem causal; movimento/posições mantidos e dinheiro decimal. Escalas e fronteiras em `docs/ARCHITECTURE.md`. Nenhuma agenda, paralelismo, regra de guincho ou tuning novo.

Critérios de produto: representação interna das regras automáticas existentes, sem clique, opção, etapa ou espera nova. SIMs e empresas mantêm consumo, produção, saúde, emprego e consequências físicas/econômicas; causas e métricas continuam derivadas do estado real. O decaimento e sua remoção permanecem explícitos. Não há configuração nova para o jogador; as escalas pertencem ao formato de save.

## Código integrado e correções

- `origin/main` observado após fetch: `13b8e98` (merge da PR #38). Integrado sem conflitos em `f4f890f`.
- Correções em `de62d24`. As somas inteiras de combustível/temporizador em `Towing.cs` foram preservadas pelo merge. O novo `TowExitWaitSince` é instante derivado, não acumula dt; entrou na lista permitida por esse motivo. A varredura de escritas compostas em bordas double não encontrou ocorrências na simulação nem no Godot (`resume-accumulator-audit.txt`).
- Poluição: produto do estado inteiro pelo fator de recuperação truncado em direção a zero. Emissões continuam quantizadas ao inteiro mais próximo, empate afastando do zero. O limiar continua 100 unidades = 1e-10 ponto. Cinco testes usam resíduos iniciais de 100, 1.000, 100.000, 143.000 e 144.000 unidades, exigem queda estrita por passo de 2 s e remoção; antes, o arredondamento ao mais próximo os estabilizava.
- QA: `Main.PresentationQA.cs` usa `AddElapsedSeconds(2)`.
- Persistência: versão ausente, não numérica, fracionária ou fora de Int32 resulta em `InvalidDataException`, sem `GetInt32` indevido. Oito casos novos cobrem formatos inválidos.
- Guarda de tipos: parte de `CityState`, percorre propriedades persistidas, anuláveis e coleções e compara doubles contra lista explícita de entradas, instantes, geometria e valores recalculados. Estoques têm conversor verificado para `Units` inteiros. Novos campos double exigem revisão dessa lista.
- `ProductionAccumulator` foi removido após a busca confirmar ausência de uso operacional.

## Escalas e limites

| Quantidade | Escala | Limite positivo aproximado em long |
|---|---|---|
| Tempo, temporizadores e fome | 10^6 por segundo | 9,22 × 10^12 s, cerca de 292 mil anos |
| Recursos e integrais de serviços | 10^9 por unidade atual do catálogo | 9,22 × 10^9 unidades |
| Saúde, proporções e poluição | 10^12 por ponto | 9,22 × 10^6 pontos |

Cada delta é quantizado antes da soma inteira, com estouro verificado. As escalas não inventam kg, litros ou outras unidades físicas ausentes do catálogo. A média móvel e o decaimento compõem suas recorrências a cada passo; não se tornam taxas constantes. `QuantityStateVersion=1` guarda os inteiros exatos; saves anteriores são recusados, sem migração e sem apagar o arquivo.

## Verificações executadas

| Verificação | Resultado | Evidência |
|---|---|---|
| Testes completos da main `13b8e98` | 278/278, zero falhas/ignorados | `resume-main-tests.log` |
| Testes focados de quantidades | 21/21 | `resume-targeted-tests.log` |
| Testes completos da branch | 299/299, zero falhas/ignorados | `resume-tests.log` |
| Build Headless, antes/depois | exit 0, zero avisos/erros | `resume-{main-,}headless-build.log` |
| cycle 42/7/43, 7 dias, antes/depois | 6 execuções completas, invariantes OK | `resume-{before,after}-cycle-*.log` |
| grid 42/7/43, 11 dias, antes/depois | 6 execuções completas, invariantes OK | `resume-{before,after}-grid-*.log` |
| Godot 4.7.2 mono: rebuild real, import real | exit 0 | `resume-godot-build.log`, `resume-godot-import.log` |
| Godot `-- --qa-list` | 36 cenários e requisitos listados | `resume-godot-qa-list.log` |
| Godot `-- --smoke` | `INDEXCITIES_SMOKE_OK buildings=0 roads=17`, exit 0 | `resume-godot-smoke.log` |
| Godot `-- --persistence-smoke` | `INDEXCITIES_PERSISTENCE_OK`, continuação idêntica, aviso de versão antiga, exit 0 | `resume-godot-persistence-smoke.log` |
| Documentação e diff | `Validate-Docs.ps1` sem links quebrados; `git diff --check` sem erros | `resume-docs.log` |

Comando da suíte: `dotnet test tests/IndexCities.Simulation.Tests -c Release --nologo`. Os 278 casos da main permaneceram intactos; a branch tem 21 casos novos no total. Os avisos observados são preexistentes: CS0219 em `TowingTests.cs:54` e CS8629 em `Main.ProblemAlertsQA.cs:198`.

O Godot foi reconstruído por `dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj -t:Rebuild --nologo`. A engine de console foi executada com `--headless --path <projeto> -- <flag-do-jogo> --review-save-dir=<absoluto>`, após importação com `--editor --import --quit`. Script: `resume-godot.ps1`; hash da biblioteca reconstruída: `resume-godot-assembly.txt`.

O marcador confirmou `C:/Users/Marlohn/orca/workspaces/IndexCities/quantidades-inteiras/artifacts/verification/fixed-quantities/resume-godot-saves/saves-d61dce6601474939920afe9e61e12b1c`; três arquivos de cidade foram conferidos nesse diretório. O smoke simples não grava cidade. Nenhum save do jogador foi usado ou apagado. Metadados de importação regenerados pelo Godot não tiveram mudança de conteúdo e ficaram fora do diff.

## Comparação pareada honesta

Antes = main `13b8e98`, via `git archive`, núcleo/testes intactos. Depois = árvore de código `de62d24`. Apenas `Headless/Program.cs` e `Fixtures.cs` do depois foram usados também no antes, para métricas iguais e o mesmo acesso válido da captação no ciclo43. Os hashes dos dois arquivos coincidem; hashes das bibliotecas e ordem dos comandos em `resume-bench-metadata.txt`.

`resume-bench.ps1` executou em série, sem builds concorrentes deste worker: cycle 42/7/43, depois grid 42/7/43, antes e depois em cada par. Ambos usam aquecimento de 2 dias e limite de 3.600 s. Extração reprodutível em `resume-compare.py` (`py -3` no Windows), resultados brutos em `resume-comparison.json` e tabelas em `resume-tables.md`; a tabela comentada está em `docs/exploration/ritmo-velocidades.md`.

| Cidade / semente | Eventos pessoais antes → depois | Lançamentos antes → depois | Δ salários | Δ satisfação |
|---|---|---|---|---|
| cycle 42 | 5856 → 5856 | 2298 → 2298 | 0,00 | -0,000048 |
| cycle 7 | 4547 → 4547 | 2019 → 2019 | 0,00 | -0,000063 |
| cycle 43 | 4832 → 4832 | 2327 → 2327 | 0,00 | -0,000061 |
| grid 42 | 13626 → 13609 (-0,125%) | 4716 → 4722 | +0,65 | -0,024277 |
| grid 7 | 13119 → 13156 (+0,282%) | 4716 → 4728 | -0,58 | -0,062879 |
| grid 43 | 13678 → 13878 (+1,462%) | 4762 → 4783 | -0,13 | +0,078007 |

Mortes e refeições coincidem em todos os pares. Saúde média coincide nas seis casas exibidas. As três grades têm zero veículos parados ≥30min, zero falta de combustível e zero guinchos chamados. Os ciclos apenas confirmam zero veículos em viagem no encerramento; não medem o máximo de imobilizados.

Há divergência de trajetória nas grades após quantizar; +200 eventos na semente 43 é cerca de +1,5%, não só um último dígito diferente. As métricas/invariantes não detectaram regressão de regra nos cenários observados, mas não identificam a primeira decisão que separou cada trajetória e não provam equivalência universal. A dispersão entre sementes não é usada para diminuir a diferença pareada. SHAs de formatos numéricos diferentes não são prova de equivalência; não há alegação de ganho de desempenho.

## Pendências e limites

- Revisão final por modelo independente será despachada pelo coordenador após o PR; ainda não realizada nesta retomada. A entrega global continua dependendo da validação humana.
- Estes smokes são headless e as grades não forçam panes; não substituem playtest visual nem ensaio novo do guincho. Os testes existentes de guincho passaram na suíte integrada.
- Quantização recebe double; um empate decimal exato pode chegar como aproximação binária. Fatores double viram decimal nas recorrências; `HungerHours` converte horas em segundos na borda. Esses limites observados na revisão permanecem, sem mudar a representação inteira canônica.
- A lista permitida detecta novos doubles persistidos, mas uma mudança de significado em campo já permitido continua exigindo revisão. A varredura das escritas compostas foi registrada como verificação complementar.
- Nenhuma autorização de agenda ou mudança de regra foi inferida dos testes ou da pesquisa.
