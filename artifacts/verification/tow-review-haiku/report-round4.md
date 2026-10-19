# Revisão independente — guincho (rodada 4)

- **Revisor:** Claude Haiku 5.5 (fallback; DeepSeek V4.1 Flash com chave expirada). **Autor:** Claude Opus 5.5.
- **Faixa:** `972e920..a5808e1` (correções da rodada 3). Somente leitura; sondas em `%TEMP%\haiku-r4-probe` (removidas). O revisor devolveu o relatório como texto; este registro é a transcrição fiel feita pelo autor.
- **Testes:** suíte completa 272 aprovados.

## Achados (resumo fiel)
- **[Alto]** Manter a reserva do carro parado sempre (`Cars.cs`, `LeaveCar`) trava o cruzamento quando nenhum guincho pode vir. Sonda: carro parado dentro do cruzamento (4,0), sem ligação com o exterior; um carro na faixa oposta, que na base atravessa em menos de 400 s, fica parado em x=8 ("sem rota alternativa"). Comportamento não registrado na SPEC. Alternativa sugerida: manter a reserva só enquanto há guincho a caminho.
- **[Médio, não reproduzido]** Na meia-volta, os veículos isentos da checagem de faixa oposta (`HeldBeyond`) não tinham o corpo conferido no ponto de giro; só o bloqueio era conferido (`Hits`).
- **[Médio]** Faltavam testes de `HeldAtStrandedJunction`/`orAtItsJunction`, M1, M2 e B1.
- **Verificado sem regressão:** M1, M2, B1, refatoração de `HeldBehindStranded` (sem recursão), entrada na faixa, `Stuck.cs`.
- **Semente 42:** confirma que o travamento restante é só o topológico descrito pelo autor (#744, #748, #752 esperando na boca leste do cruzamento (-16,0), na contramão do guincho, sem rota alternativa; guincho #935 parado em x=-24,4). Ressalva: o impasse precisava ficar registrado como pendente. Cenário injetado pelo FORCE da bancada.

## Resposta do autor
- **Alto:** corrigido como sugerido. O carro parado só mantém a reserva depois que as pessoas descem se há guincho a caminho (`TowTruckId`); sem guincho possível, solta como antes. O teste cobre os dois casos (`A_car_stranded_in_a_junction_keeps_holding_it_after_its_people_walk_on_while_its_tow_comes`).
- **Médio (corpo no giro):** corrigido. `Hits` confere o corpo contra o bloqueio e contra qualquer veículo na via perto do ponto de giro.
- **Médio (testes):** novo teste `Behind_a_vehicle_stopped_in_a_junction_one_waiting_at_its_far_mouth_is_not_oncoming_traffic`, que falha sem a isenção. M1 e M2 seguem cobertos pela travessia e pela bancada. B1 é só calibração de frequência.
- **Semente 42:** registrada como **Em aberto (sem decisão)** em `docs/spec/transporte-circulacao.md` e levada ao coordenador.
- **Revalidação:** 273 testes aprovados. Bancada (sementes 42/7/43): sem forçar, 0 parados e 0 mortos; `--strand=2`: 7 e 43 com 0 parados, 42 com 8 parados (topológico) e 0 mortos. Godot `--tow-smoke` (`INDEXCITIES_TOW_OK`) e `--smoke` (`INDEXCITIES_SMOKE_OK`).
