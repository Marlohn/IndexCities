---
name: qa-godot
description: Como validar uma mudança do IndexCities com testes da simulação e cenários de QA reais no Godot (listar, rodar, criar cenário, conferir capturas). Use ao testar, validar ou declarar pronta qualquer entrega.
---

# QA no IndexCities

As regras de QA estão no `AGENTS.md`, seção 7 (argumentos da engine antes de `--`, saves isolados, o banner da engine não prova o cenário). Aqui fica o como fazer. Rode tudo **a partir da raiz do repositório**.

## 1. Testes da simulação

```powershell
dotnet test tests/IndexCities.Simulation.Tests -c Release --nologo
dotnet test tests/IndexCities.Simulation.Tests -c Release --nologo --filter BreakdownTests   # só uma área
```

- O `dotnet test` é o resultado que vale.
- `dotnet run --project tests/IndexCities.Simulation.Tests -c Release` usa um executor local mais rápido, mas ele não roda `Theory` com `MemberData`: essa falha conhecida não é regressão.
- Rode primeiro os testes da área alterada e depois a suíte inteira.

## 2. Cenários de QA no Godot

Use a engine **de console**: a outra não mostra o log no terminal, e você fica sem ver o marcador. Nesta máquina:

```powershell
$engine = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe'
```

- **Listar:** a fonte é `src/IndexCities.Godot/Main.QARegistry.cs` (o que cada cenário verifica, se precisa de janela real e de que cidade ou fixture precisa). Para imprimir, com build de debug (o padrão ao rodar pelo `--path`):
  `& $engine --path src/IndexCities.Godot -- --qa-list`
- **Rodar:**

```powershell
$dir = (Resolve-Path .).Path + '\artifacts\verification\<entrega>'
& $engine --path src/IndexCities.Godot --resolution 1280x800 -- --breakdown-smoke "--review-save-dir=$dir"
```

- Argumentos do jogo vão depois do `--`: o cenário, `--review-save-dir=` (caminho **absoluto dentro de `artifacts/verification`**, senão o jogo recusa) e, para cenários que precisam de cidade pronta, `--fixture=<caminho>` (veja a coluna de fixture no registro).
- **Onde ficam saves e capturas:** depende do cenário. Alguns usam o `--review-save-dir`; outros gravam em `artifacts/verification/<cenário>` por conta própria. Confira no código do cenário (`Main.<Nome>QA.cs`) antes de declarar onde está a evidência.
- **Sucesso ou falha:** cada cenário imprime o **seu** marcador de sucesso; não existe um padrão único (ex.: `INDEXCITIES_BREAKDOWN_OK`, `PROBLEM_ALERTS_QA ok`, `GRID_ALIGNMENT_SMOKE_OK`, `ROAD_TOOL_NATIVE_VERIFIED`). Procure no código do cenário qual é. Falha sai com código 1: por exceção, ou com `INDEXCITIES_QA_PRECONDITION` quando falta a cidade ou fixture exigida. Os modos "antes" (`--activity-before`, `--pedestrian-before`, `--services-before` etc.) só registram a linha de base e saem com 0 sem provar nada.
- Sem o marcador certo, o cenário **não** passou, por mais que a engine tenha aberto. Confira a chamada.
- Cenários com janela real abrem uma janela; não rode vários ao mesmo tempo na mesma worktree.
- Pela GUI, `scripts/Play.ps1` compila, importa e abre o jogo; argumentos do jogo vão com o separador: `-GodotArguments '--',('--review-save-dir='+$dir)`.

## 3. Conferir as capturas

Abra as imagens geradas e olhe de verdade: objetos invisíveis, flutuando, atravessando prédios, texto duplicado na inspeção. Vários problemas só aparecem assim. Cite no relato os caminhos das capturas que comprovam cada comportamento.

## 4. Criar um cenário novo

- Um arquivo `Main.<Nome>QA.cs`, mais o registro em `Main.QARegistry.cs` (nome, flags, o que verifica, janela, fixture, método).
- O cenário cria a própria cidade (ou declara a fixture), executa o comportamento pela simulação real, captura com nomes descritivos, imprime um marcador próprio no formato `INDEXCITIES_<NOME>_OK` com um resumo e encerra com `Quit()`; em exceção, `PushError` e `Quit(1)`.
- Para encurtar o tempo, ajuste só a fixture do cenário, nunca as regras do jogo.

## 5. Antes de declarar pronto

Testes da área e suíte inteira verdes; cenário novo e cenários vizinhos (os que usam o que você mexeu) executados, com o marcador certo de cada um; capturas conferidas; `scripts/Validate-Docs.ps1` se mexeu em docs; diff revisado sem ruído de `*.import`/`*.uid`. Depois vem a revisão independente (AGENTS, seção 7).
