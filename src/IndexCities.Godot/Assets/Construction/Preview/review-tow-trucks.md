# Revisão independente: guinchos vehicle.tow_truck.a e vehicle.tow_truck_heavy.a

- Modelo revisor efetivo: Claude Haiku 5.5 (claude-haiku-5-5).
- Autor declarado: GPT-6.1-Sol xhigh (modelo diferente, exigência cumprida).
- Escopo: somente leitura. Nenhum arquivo de fonte, cena, C# ou documento canônico foi alterado. Este arquivo é o único escrito.
- Métodos: leitura do diff merge-base (`git diff origin/main...HEAD`), dos scripts, do README, do manifesto, das cenas e dos logs (UTF-16 decodificado); leitura direta dos PNGs; conferência de hashes entre `artifacts/` e `Assets/Construction/Preview/`; cálculo manual do pitch e das folgas. Não executei Godot nem repeti a validação.

## Conclusão

Nenhum achado bloqueante. Nomes, convenções, anchors, envelopes, manifesto, contagens e orientação batem com o README e com os scripts. Não há C# alterado no branch e não há emissão nem luzes nas cenas dos guinchos.

Antes de sair do rascunho, recomendo corrigir os itens 1 e 2. Os itens 3 a 6 são melhorias de verificação e de documentação. A estética continua pendente de revisão humana, como o próprio README declara.

## Achados, por gravidade

1. **Média. Evidências fora do git.** `.gitignore:29` ignora `artifacts/`. Os logs (`build.log`, `validate-only.log`, `render.log`, `smoke.log`, `qa-list.log`, `host-build.log`, `import.log`), as 20 capturas e o corpo da PR citam `artifacts/verification/construction-models`, que não entra no commit. Só `Assets/Construction/Preview/` está versionado (8 PNGs, `validation.json`, `render_metrics.json`).
   Correção: copiar para `Assets/Construction/Preview/` os logs de build, validação e smoke que sustentam o PR, ou declarar no PR que os logs são locais.

2. **Média. Base defasada e comparação enganosa.** O branch está 23 commits atrás de `origin/main` (merge-base `35225f5`, PR #31). Por isso `git diff origin/main --stat` lista 44 arquivos, incluindo C# de Simulation, Headless e testes. Essas diferenças são commits novos do main, não deste branch. O diff real do PR (`git diff origin/main...HEAD`) tem 22 arquivos, todos em `Assets/Construction/`, e nenhum `.cs`.
   Correção: reportar com `origin/main...HEAD`. Antes de sair do rascunho, atualizar a branch com o main e repetir pelo menos build, validate-only e smoke.

3. **Baixa-média. Encaixe do caminhão rebocado não aparece nas capturas.** Nos tiles "REBOCADO / RODAS NO SOLO" de `sheet_tow_truck_heavy.png` e em `detail_tow_heavy_back_iso.png`, o berço de `LiftAnchor` fica sob a cabine e não é legível. `detail_tow_heavy_lift.png` mostra o braço sem caminhão. Nenhuma captura de aproximação mostra o eixo dianteiro nos berços.
   Correção: adicionar um detalhe (por exemplo `detail_tow_heavy_cradle.png`) com câmera baixa, caminhão montado e foco em `LiftPivot/LiftAnchor` e nos berços, e registrá-lo no `render_metrics.json`.

4. **Baixa. Folga do caminhão rebocado sem checagem numérica.** `verify_tow_heavy` (em `verify_construction.gd`) confere centragem do eixo, escala, orientação, roda traseira e altura acima do solo. Não compara cabine ou para-choque com `LiftArm`, `RecoveryBody` nem `Body`. Estimativa manual na pose de repouso: o canto inferior do para-choque fica perto de (y 1,09; z 2,87) e o topo do braço perto de y 1,0 em z 3,3 a 3,9. A folga parece de alguns décimos de metro, o que é plausível.
   Correção: amostrar a AABB do caminhão contra as AABB de `LiftArm` e `RecoveryBody` nas duas poses e exigir folga mínima (por exemplo 0,02 m), ou declarar no README que a folga é só visual.

5. **Baixa. Checagem de contato dos pneus traseiros é fraca.** Em `verify_tow_heavy`, `bounds(w)` usa vértices locais do próprio nó da roda (`nodes()` inclui o próprio nó e a transformação é identidade). Assim a checagem ignora a rotação `-pitch` aplicada à roda. Ela só confirma que o centro da roda está a 0,47 m; não mede o ponto mais baixo do pneu girado na cena.
   Correção: calcular o Y mínimo dos vértices de RL e RR no espaço de `posed` e exigir valor maior ou igual a -0,002.

6. **Baixa. Nota do manifesto diverge do código.** `build_tow_truck_heavy` (campo `alignment` em `manifest.construction.json`) diz "Counter-rotate rear wheels by body pitch". Mas `align_heavy_truck` contragira as quatro rodas do caminhão rebocado (FL, FR, RL, RR). As dianteiras estão no ar, então o efeito visual é inócuo, mas a nota está errada.
   Correção: trocar a nota para "all four towed-truck wheels" ou restringir o código a RL e RR, e regenerar o manifesto.

7. **Baixa. Amostragem do convés inclinado.** `verify_tow_truck` amostra o fundo do convés em z = 1,45, 2,65 e 3,17. O para-lama traseiro (`metal_dark`, z 0,91 a 3,17, topo y 1,13, limiar 1,131) fica coberto, porque o convés é plano e o mínimo cai na extremidade 3,17. A cauda, na pose (z ~3,8 a 4,1, y ~1,02 a 1,07), não tem obstáculo modelado. Sem ação agora; se surgir peça ali, ampliar a amostragem.

8. **Informativo. Sobreposição de 3 cm no repouso.** O fundo do convés em repouso está em y 1,10 e o topo do para-lama traseiro em 1,13 (z 0,91 a 3,17). A interpenetração fica dentro do convés e não aparece. Sem ação obrigatória.

9. **Informativo. Legibilidade em escala de tile.** Em `sheet_tow_truck_heavy.png`, o tile "ELEVAÇÃO DO BRAÇO" se distingue pouco de "VAZIO" na isométrica. A diferença de 0,27 m do anchor e de 10° fica melhor em `detail_tow_heavy_lift.png`. Sem ação exigida; a legibilidade final entra na revisão humana.

## Verificado sem achados

- **Convenções:** metros, Y para cima, frente −Z, raiz no solo. Rodas `Wheel_FL/FR/ML/MR/RL/RR` com eixo X, raio 0,47 e rolagem negativa (`wheel_contract`).
- **Nós:** tow tem `DeckPivot/Deck`, `DeckPivot/CarryAnchor` (Marker3D), `RecoveryFrame` e rodas. Heavy tem `RecoveryBody`, `LiftPivot/LiftArm`, `LiftPivot/LiftAnchor` (Marker3D) e rodas. Confere com o README.
- **Anchors:** `CarryAnchor` local (0; 0,09; −1,815) vira (0; 1,24; 1,085) na raiz, como documentado. `LiftAnchor` local (0; 0,42; 1,5) vira (0; 1,10; 3,935) com `LiftPivot` em (0; 0,68; 2,435). O shift de −0,4 e depois −0,065 no `build_construction.gd` está coerente com essa posição.
- **Pitch heavy:** `asin((1,10 − 0,47)/4,30)` ≈ 8,4° em repouso e ≈ 11,9° com X = −10°. Sinais conferidos: X positivo levanta −Z (convés e braço) e X negativo levanta +Z (berço).
- **Carro no convés:** X de −1,14 a 1,14 com retrovisores e Z de −1,015 a 3,185 dentro de −1,10 a 3,27. Frente virada para −Z nas capturas.
- **Manifesto:** 7 entradas, com tow e heavy, anchors, envelopes e alinhamento. JSON parseado sem erro.
- **Build e verify:** 7 cenas; `verify` espera 7 famílias; `validation.json` tem 813 checks e 0 falhas. Contagens batem com a tabela do README: tow 2.750 triângulos, 27 superfícies, 9 MeshInstances, envelope 2,90 × 2,97 × 7,20; heavy 2.894, 28, 9, envelope 2,90 × 3,29 × 8,13.
- **Orçamento:** tow 2.750 e heavy 2.894 triângulos, na mesma ordem da família (basculante 2.522 vazio e 3.502 total; betoneira 2.778; carroceria 2.374 e 3.898). Nenhum outlier.
- **Emissão e luzes:** nenhum `emission` nos `.tres` novos; as cenas dos guinchos não têm Light, Script, AnimationPlayer nem Camera. Giroflex apagado conforme o README.
- **C# e host:** nenhum `.cs` no diff do branch. O aviso CS8629 em `Main.ProblemAlertsQA.cs:198` está em `host-build.log` e o arquivo não foi tocado.
- **Hashes:** as 8 capturas em `Preview/Captures` são idênticas às de `artifacts/` (md5).
- **Capturas:** guincho leve legível em frente, costas e isométrica; carro dentro dos guardas e da cabeceira; pose de basculamento mostra a frente elevada e a cauda baixa sem cortar o para-lama visualmente. Heavy: traseiro do rebocado no solo, frente elevada, mesma orientação, sem clipping visível na escala dos tiles.
- **Originalidade:** cabine e rodas vêm do próprio kit V1 (`truck._cab`), sem recursos externos.

## Limitações

- Não executei Godot, não reproduzi o build nem o `verify` e não rodei cenários QA.
- A ausência de clipping foi avaliada por imagem em tiles pequenos (500 × 380 por tile); folgas milimétricas não são verificáveis por eles.
- Estética e leitura humana permanecem pendentes, como declarado no README.
- Li os PNGs visualmente; o modelo revisor suporta visão.

## Resposta do autor após a revisão — 2026-10-18

Autoria desta seção: GPT-6.1-Sol xhigh. O texto da revisão acima foi preservado; esta seção registra as correções posteriores, sem atribuí-las ao revisor.

- Itens 1 e 2: logs principais copiados em UTF-8 para `Preview/Verification/`. A branch recebeu `origin/main` em `9020670`, incluindo `daac4fb` (PRs #32 e #33); o diff `origin/main...HEAD` da entrega continua restrito ao kit de arte. Build do host, geração das sete famílias, validate-only e smoke foram repetidos após a integração.
- Item 3: nova captura real `Captures/detail_tow_heavy_cradle.png`, câmera baixa com o caminhão montado, foco no eixo/berços. Registro em `render_metrics.json`. São 21 capturas principais mais o detalhe da betoneira.
- Item 4: checagem de 0,02 m entre as superfícies trianguladas da cabine/chassi rebocados e `Body`, `RecoveryBody` e `LiftPivot/LiftArm`, nas duas poses. AABB por triângulo limita os pares candidatos; a distância usa vértices/faces e arestas, além da detecção de interseção. O eixo dianteiro que deve encostar no berço é a única região de contato excluída. Uma cabine deslocada 0,75 m para dentro do guincho é rejeitada, verificando que a checagem detecta sobreposição real.
- Item 5: contato traseiro agora usa o menor Y dos vértices de cada pneu no espaço do guincho após todas as transformações, com tolerância de 0,002 m, em ambas as poses.
- Item 6: nota de alinhamento corrigida no gerador e no manifesto para as quatro rodas contragiradas na pose de revisão plana.
- Itens 7 a 9: observações informativas mantidas, sem mudança de geometria. A estética permanece PENDENTE de revisão humana.

Resultado final executado pelo autor: build do host com zero erros (aviso CS8629 em arquivo C# não alterado), build das sete famílias, 821 verificações sem falhas em validate-only e janela real Windows/GL Compatibility, e `INDEXCITIES_SMOKE_OK buildings=0 roads=17`. Saves isolados em `artifacts/verification/construction-models/smoke-saves`. A revisão independente não repetiu esses testes; as correções acima foram verificadas pelo autor.
