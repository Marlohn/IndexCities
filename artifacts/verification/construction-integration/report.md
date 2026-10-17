# Integração caminhões por material + pavimentação física (coordenador Claude Opus 5.5)

Assumido após esgotar a cota do coordenador/worker Codex GPT-6.1-sol, em 2026-10-16.

## O que entrou nesta integração

- `5139ac5` conversão única da frota de saves antigos (SPEC `materiais-logistica.md`, regra aprovada em 2026-10-16). Implementação do worker Codex (ctx_340abaac4dab), deixada sem commit quando a cota acabou; conferida contra a SPEC e commitada pelo coordenador.
- `94555b8` (cherry-pick de `f1e067c`, worker Claude Opus 5.5 ctx_71c524c75ef8): máquina bloqueada não gira a carroceria para pose não verificada; resolve o bloqueio mútuo rolo × caminhão em CornerCity. Relatório: `../construction-queue-diagnosis/report.md`.

## Verificação sobre `94555b8`

| Verificação | Resultado |
|---|---|
| `dotnet test … --filter 'FullyQualifiedName!~CityCycleTests'` | 225 aprovados, 0 falhas (`regression-tests.log`) |
| `dotnet test … --filter 'FullyQualifiedName~CityCycleTests'` (soak) | 2 aprovados, 0 falhas (`citycycle.log`) |
| Build `IndexCities.Godot.csproj` | 0 erros (`build.log`) |
| Godot renderizado `--construction-corner-smoke` | `INDEXCITIES_CONSTRUCTION_CORNER_OK models=final captures=4 street=completed house=completed roller=yielded-without-turning` |
| Godot renderizado `--construction-paving-smoke` | `INDEXCITIES_CONSTRUCTION_PAVING_OK models=final captures=27 straight=L=physical money=conserved save-load=deterministic cancel=physical-return` |

Capturas inspecionadas pelo coordenador: `L-unload-Asphalt.png` (basculante descarrega na pavimentadora), `corner-roller-yields.png`, `L-completed.png`.

## Pendências

- Revisão independente (DeepSeek V4.1 Flash) deste estado.
- Decisão humana em aberto: destino dos trechos compactados quando a rua é cancelada antes da conclusão (`docs/spec/construcao.md`).
- Folga lateral de 4 cm na saída do rolo em CornerCity (traçado preexistente) e padrão análogo de giro em `Mobility.cs` não generalizado — sem falha observada.
- Validação humana da experiência.

## Revisão DeepSeek V4.1 Flash e correção (rodada 1)

Revisor: `opencode/deepseek-v4.1-flash-free`, executado via `opencode run -m` porque o Orca não fixa modelo no OpenCode (a tentativa anterior abriu "Big Pickle"). Relatório: `../construction-review-deepseek/report.md`. 225/225 confirmados pelo revisor; sem edição de código por ele.

- Achado 1 (médio, cancelamento deixa trecho compactado fechado): corrigido conforme decisão humana A registrada em `40d1b34`. `CancelWork` usa o mesmo `OpenCompactedRoads` da conclusão. Teste novo `PavingTests.Cancelling_a_partial_street_opens_only_the_compacted_cells`; smoke Godot passou a exigir o trecho aberto (`compacted=opened`). Regressão sem soak: 226/0. Smoke renderizado: `cancel-opens/engine-paving.log`, captura `cancel-opens/godot-paving2/cancelled-returned.png`. Observação visual: asfalto aplicado mas não compactado continua visível depois do trecho aberto, sem virar rua.
- Achado 2 (médio, save antigo de pavimentação sem verba de máquinas fica travado em `NoBudgetForPaving`): decisão de produto pendente com o responsável.
- Baixos 3 a 5: anotados, sem correção nesta rodada.
