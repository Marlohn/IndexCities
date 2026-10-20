---
name: qa-godot
description: Como validar uma mudança do IndexCities com testes da simulação e cenários de QA reais no Godot (listar, rodar, criar cenário, conferir capturas). Use ao testar, validar ou declarar pronta qualquer entrega.
---

# QA no IndexCities

As regras de QA estão no `AGENTS.md`, seção 7 (argumentos da engine antes de `--`, saves isolados, o banner da engine não prova o cenário). Aqui fica o como fazer.

## 1. Testes da simulação

```powershell
dotnet test tests/IndexCities.Simulation.Tests -c Release --nologo
```

- O `dotnet test` é o resultado que vale.
- `dotnet run --project tests/IndexCities.Simulation.Tests -c Release` usa um executor local mais rápido, mas ele não roda `Theory` com `MemberData`: essa falha conhecida não é regressão.
- Rode primeiro os testes da área alterada (ex.: `BreakdownTests`, `TowingTests`, com `--filter`) e depois a suíte inteira.

## 2. Cenários de QA no Godot

- A lista oficial está em `src/IndexCities.Godot/Main.QARegistry.cs` (o que cada um verifica, se precisa de janela real e de que cidade precisa). Para imprimir:
  `& $engine --path src/IndexCities.Godot -- --qa-list`
- Para rodar um cenário (nesta máquina, `$engine` é a engine mono em `C:\Users\Marlohn\Tools\Godot`):

```powershell
$dir = (Resolve-Path .).Path + '\artifacts\verification\<entrega>'
& $engine --path src/IndexCities.Godot --resolution 1280x800 -- --breakdown-smoke "--review-save-dir=$dir"
```

- O `--review-save-dir` precisa ser **caminho absoluto dentro de `artifacts/verification`**; senão o jogo recusa. Saves e capturas vão para lá.
- Sucesso = a linha `INDEXCITIES_<CENÁRIO>_OK ...` no log e saída 0. Falha = exceção no log e saída 1. Sem o marcador, o cenário **não** foi executado, por mais que a engine tenha aberto: confira a chamada.
- Cenários com janela real abrem uma janela; não rode vários ao mesmo tempo na mesma worktree.
- Pela GUI, `scripts/Play.ps1` compila, importa e abre o jogo; argumentos do jogo vão com o separador: `-GodotArguments '--',('--review-save-dir='+$dir)`.

## 3. Conferir as capturas

Abra as imagens geradas e olhe de verdade: objetos invisíveis, flutuando, atravessando prédios, texto duplicado na inspeção. Vários problemas só aparecem assim. Cite no relato os caminhos das capturas que comprovam cada comportamento.

## 4. Criar um cenário novo

- Um arquivo `Main.<Nome>QA.cs`, mais o registro em `Main.QARegistry.cs` (nome, flags, o que verifica, janela, fixture, método).
- O cenário cria a própria cidade (ou declara a fixture), executa o comportamento pela simulação real, captura com nomes descritivos, imprime `INDEXCITIES_<NOME>_OK` com um resumo e encerra com `Quit()`; em exceção, `PushError` e `Quit(1)`.
- Para encurtar o tempo, ajuste só a fixture do cenário, nunca as regras do jogo.

## 5. Antes de declarar pronto

Testes da área e suíte inteira verdes; cenário novo e cenários vizinhos (os que usam o que você mexeu) executados com marcador OK; capturas conferidas; `scripts/Validate-Docs.ps1` se mexeu em docs; diff revisado sem ruído de `*.import`/`*.uid`. Depois vem a revisão independente (AGENTS, seção 7).
