---
name: orquestracao
description: Cuidados práticos para coordenar workers do IndexCities pelo Orca (despacho, espera, retomada após limite de uso, limpeza, PR e push). Use ao orquestrar ou delegar uma entrega a outros agentes.
---

# Orquestração no IndexCities — cuidados práticos

Complementa a seção 7 do `AGENTS.md` (as regras ficam lá) e o guia do próprio Orca (`orca skills get orchestration`). Aqui só estão armadilhas que já custaram tempo ou trabalho.

## Antes de despachar

- Parta do `origin/main` atualizado (`git fetch` e uma worktree nova a partir dele), não da worktree em que a sessão foi aberta: ela pode estar atrás e já ter conflito com a SPEC.
- Confira a cota de cada modelo (rodapé do terminal) **antes** de mandar trabalho. Não ponha coordenador e todos os workers no mesmo provedor: um limite de uso para todos ao mesmo tempo.
- Escreva o pedido num arquivo e passe com `--spec "$(cat arquivo)"` pelo Bash. Aspas e quebras de linha no PowerShell quebram o argumento.
- Workers em paralelo só com fronteira de arquivos clara (ex.: simulação × `Assets/`). Defina antes o contrato entre eles (nomes de nós, campos expostos).

## Durante a espera

- Leia **todas** as mensagens de um lote do `check` antes do `--ack`: perguntas chegam misturadas com heartbeats.
- Sessões abertas fora do Orca (terminal comum) não mandam pergunta nem `worker_done`. Prefira sempre `worker-start`.
- Depois de um limite de uso, **olhe o terminal do worker antes de despachar de novo**: as sessões costumam voltar sozinhas. Despachar no mesmo terminal costuma falhar (`agent_readiness: timeout`), e o Orca recusa o `worker_done` de uma tarefa já encerrada. A mensagem recusada ainda chega ao coordenador com o relatório; leia-a.
- Com OpenCode, `--model` só é aceito em worktree que já existe e, mesmo assim, pode ser recusado ("cannot verify launch-time model selection"). Sem `--model`, ele abre com o modelo da própria configuração (aqui, o último usado): confira no terminal qual abriu e registre o modelo efetivo.
- Confira você mesmo as capturas dos cenários (abra as imagens): os testes não pegam carro invisível, objeto flutuando etc.

## Limpeza

- **Nunca remova a worktree em que a sua própria sessão roda**: a sessão cai. Deixe essa para o responsável.
- Ruído de reimportação do Godot (`*.import`, `*.uid` alterados só por fim de linha ou hash) não entra em commit: descarte com `git checkout -- <arquivos>` depois de conferir o diff.

## PR e push

- Antes de dar push numa branch de PR: `gh pr view N --json state`. O merge costuma ser rápido, e um commit depois do merge fica órfão.
- Se o `git push` der 403 por credencial de outra conta, use a conta do `gh`:
  `git -c credential.helper= -c 'credential.helper=!gh auth git-credential' push`
