# Revisão independente — guincho (rodada 1)

- **Revisor (modelo efetivo):** Claude Haiku 5.5 — fallback do AGENTS.md §7: DeepSeek V4.1 Flash indisponível (OpenCode: "This API key has expired", ver `../tow-review-deepseek/run.log`).
- **Autor:** Claude Opus 5.5. Faixa: `origin/main..0296334` (e `e2873db`). Somente leitura; sondas fora do repositório.
- **Comandos:** `dotnet test tests/IndexCities.Simulation.Tests` em `e2873db` e `0296334`: 263 aprovados, 0 falhas. Sondas P1b–P4 reproduzem nos dois commits; P5 exploratória não reproduziu.

## Achados (texto do revisor, resumido fielmente)

1. **[Alto]** Guincho que fica sem combustível no caminho deixa carro e guincho parados para sempre (`Towing.cs` replaneja sem checar combustível; guincho parado não é rebocado; o carro mantém `TowTruckId` e não chama de novo). Sonda P1b.
2. **[Alto]** Posto removido durante a entrega deixa o motorista dentro do carro estacionado (sem `LeaveCar`/`Disembark`). Sonda P2.
3. **[Alto]** Carro carregado sem rota para nenhum destino espera para sempre com pessoas dentro (replanejamento sem limite nem saída). Sonda P3.
4. **[Médio]** Posto sem estoque na entrega prende o carro em `Unloading` na bomba (laço de 60 s do `Refuel`, anterior ao guincho, mas com novo caminho até ele). Sonda P4.
5. **[Médio]** Meia-volta além da SPEC: (a) quem espera um cruzamento retido por veículo parado faz meia-volta; (b) o guincho faz meia-volta após 180 s em qualquer fila (o `blocker` pode ser qualquer ocupante da faixa).
6. **[Baixo]** Guincho pode passar do ponto de coleta depois da contramão (não confirmado).
7. **[Baixo]** Guincho pesado a 6,5 m e não à soma das metades (7,6 m); a sobreposição do berço pode explicar, mas o código não diz.
8. **[Baixo]** Frete cobrado no despacho, não na entrega.
9. **[Baixo]** Veículos fora de `Towable` (máquinas, na bomba, fora da via) não recebem chamado nem causa própria.
10. **[Baixo]** Guinchos sem `OwnerAccount` explícito (latente).
11. **[Baixo]** Veículo deixado no espaço de espera não tem como o dono resolvê-lo (limitação conhecida).
12. **[Baixo]** Sem testes de regressão para 1–4.

**Verificado sem achados:** pagamento A1 e dívida da Prefeitura; fórmula do frete; combustível do exterior; escolha do posto mais próximo; chamado impossível refeito com a malha; limite de 1 h; espaço de espera; caminhão externo com/sem entrega pendente; chegada pela frente e checagem da contramão; equivalência de `OccupiesLane`; guincho fora da frota externa; save/load no meio do reboque; Godot só visual; `--strand` só medição.

**Veredito do revisor:** não validado; 1–3 pedem decisão de produto ou correção; 5 pede SPEC ou remoção.

## Correções do autor

(preenchido abaixo após a rodada de correções)
