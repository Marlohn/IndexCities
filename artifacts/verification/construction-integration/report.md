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
