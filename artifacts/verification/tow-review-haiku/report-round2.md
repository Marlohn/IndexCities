# Revisão independente — guincho (rodada 2)

- **Revisor:** Claude Haiku 5.5 (fallback; DeepSeek V4.1 Flash com chave expirada). **Autor:** Claude Opus 5.5.
- **Faixa:** `0296334..730f94b`. Somente leitura; sondas em `%TEMP%` (`haiku-probe`, `haiku-strand42.txt`, `haiku-r1b-strand42.txt`).
- **Testes:** suíte completa 266 aprovados, 0 falhas. Bancada `stuck --strand` seed 42 reproduzida idêntica à do autor (sha256 do save igual). Na rodada 1 (`0296334`) o mesmo cenário travava 15 veículos já no dia 2; no HEAD só no dia 10/11.

## Achados (resumo fiel)
- **A1 [Alto]** Trava residual na seed 42 (dias 10–11): carro #773 ficou sem combustível dentro do cruzamento (−16,0); o guincho fica na fila porque a contramão não pode atravessar cruzamento e não há rota alternativa (grade em escada com um único conector). 9 veículos parados ≥30 min, 0 mortos.
- **A2 [Alto]** Guincho carregado sem destino que caiba no combustível fica parado para sempre (as pessoas descem após o limite, mas carro e guincho ficam). Só alcançável forçando combustível; resultado exige decisão de produto.
- **M1 [Médio]** Guincho substituto (após volta para reabastecer) não gera nova cobrança: regra "um serviço por resgate" não registrada.
- **M2 [Médio]** Posto que zera durante a descarga deixava o carro preso em `Unloading`.
- **M3 [Médio]** Guincho que zera fora dos replanejamentos (retorno, desvios) vira obstáculo; só reproduzido forçando combustível.
- **M4 [Médio]** Escopo além da SPEC: meia-volta para quem espera cruzamento retido por veículo parado (ou por quem está preso atrás dele), meia-volta/contorno do guincho em fila travada, roteamento do guincho em volta de cruzamentos travados — pendente de decisão do coordenador.
- **B1–B4 [Baixo]** ordem das checagens (custo), `TowPrepaid` residual, folga do ponto de coleta sem verificação visual, testes frágeis (posto zerado antes da descarga, não durante).
- **Veredito do revisor:** não validado; A1 e A2 bloqueantes; expor pendência ao responsável (segunda rodada com problema importante).

## Resposta do autor
- **M2:** corrigido (`DeliverTowed` reconfere o posto e segue pela mesma regra); teste cobre posto removido, zerado antes e zerado durante a descarga.
- **B1:** checagens baratas primeiro (`IntersectionClaim`, `IsTow`) antes de `HeldBehindStranded`.
- **A1:** depende da pergunta ao coordenador (guincho atravessar pela contramão um cruzamento travado pela própria situação) — sem resposta até o fim da sessão.
- **M4:** pergunta 5a enviada ao coordenador (manter e registrar, ou remover) — sem resposta; código isolado em `JunctionHeldByStranded`/`TowHeldTooLong`/`TowClosedRoads`.
- **A2, M3:** só alcançáveis com combustível forçado (o plano inicial confere ida e volta com margem); resultado exige decisão de produto — exposto como pendência.
- **M1:** o combustível do substituto sai do estoque externo do próprio prestador (Reserva Global), sem moeda criada; a regra "um serviço por resgate" é exposta como pendência para registro.
