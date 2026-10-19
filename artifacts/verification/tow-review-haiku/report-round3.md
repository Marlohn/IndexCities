# Revisão independente — guincho (rodada 3)

- **Revisor:** Claude Haiku 5.5 (fallback; DeepSeek V4.1 Flash com chave expirada no OpenCode). **Autor:** Claude Opus 5.5.
- **Faixa:** `7b38110..4bb4463` (decisões de 2026-10-18: travessia do cruzamento travado pela própria situação, meia-volta no cruzamento retido, IA-026). Somente leitura; sondas em `%TEMP%` (`haiku-probe`, `haiku-r3-probe.txt`). O revisor não pôde gravar o arquivo e devolveu o relatório como texto; este registro é a transcrição fiel feita pelo autor.
- **Testes:** suíte completa 271 aprovados. Bancada longa não rodada pelo revisor (leu `guincho-bench/stuck-*`/`strand-*`).

## Achados (resumo fiel)
- **A1 [Alto, bloqueante]** Semente 42 com `--strand=2` ainda trava. O revisor contestou a descrição do autor: no relatório final do dia 11, #744 estava a 1,3 m do carro parado #773 dentro do cruzamento e #752 no braço norte, não na boca leste. O impasse é de produto (trecho sem rota alternativa, `Mobility.Bypass.cs:82`), e a SPEC não trata dele. Opções: aceitar o limite ou recuar pela própria faixa. Não recomenda passar por cima do corpo de quem espera.
- **M1 [Médio]** Na travessia, guincho e carro parado inclinado se sobrepõem ~1,5 s: `DetourBodyClear` e `MoveAlongDetour` ignoram o alvo.
- **M2 [Médio, não reproduzido]** `TryUTurn` do guincho no cruzamento retido (`Mobility.cs:283`) é chamado com `blocker==null`, sem conferir o corpo de quem retém o cruzamento.
- **B1 [Baixo]** `StartDelivery` roda a cada passo com o guincho parado (medido 0,17 ms/passo).
- **B2 [Baixo]** Combustível do guincho substituto sem nova cobrança: coerente com a decisão 4, mas a SPEC deveria dizer quem absorve.
- **B3 [Baixo]** Testes frouxos (`StoppedVehicleTests` checa sobreposição só com `Travelling`; `TowingTests` 241–245).
- **B4 [Baixo]** `Stuck.cs:57` usa `First()`. **B5 [Baixo]** `TowPrepaid` só é zerado em `DeliverTowed`.
- **Sem achado:** decisão 1 (reservas; pedestre recusado), meia-volta sem regressão em filas normais, decisão 3, conservação, save/load.
- **Veredito do revisor:** não validado (A1).

## Resposta do autor (commit `a5808e1`)
- **A1:** o revisor acertou ao contestar: o estado final era outro. Com `STUCK_TRACE=773,935,744,752`, o rastro mostra #752/#744 na boca leste do cruzamento, na contramão do guincho, das 06:41 às 07:01. Às ~07:05 o limite de 1 h faz as pessoas descerem, e `LeaveCar` apagava a reserva do cruzamento do carro parado. Com isso, quem esperava atravessava o cruzamento passando pelo corpo do carro (#744 terminou sobreposto a ele), e o guincho, logo atrás, já não podia atravessar porque o cruzamento deixou de contar como travado. **Corrigido:** o carro parado mantém a reserva (`Cars.cs`, `LeaveCar`), com teste de regressão. Também `#726` (fila atrás do parado) agora faz meia-volta: quem espera na boca do cruzamento travado, e a fila atrás dele, não contam mais como vindo em sentido contrário (`HeldInStrandedChain`). Com isso, o travamento que sobra na semente 42 é só o topológico: #752, #744 e #748 querem sair do trecho leste da rua z=0, cujo único acesso é o cruzamento (-16,0), esperam na contramão do guincho e não têm outra rota. A decisão 1 exige contramão vazia, então resolver isso exige nova decisão de produto, levada ao coordenador.
- **M1:** corrigido: `DetourBodyClear` confere o corpo do veículo parado em todas as pernas, menos a última (que termina na pose à frente dele).
- **M2:** corrigido: a meia-volta do guincho no cruzamento retido recebe quem o retém como bloqueador (corpo conferido em `Hits`).
- **B1:** corrigido: o guincho parado sem plano de entrega tenta de novo a cada 10 s (`TowReplanSeconds`, `TowRetryAt`).
- **B2:** a SPEC já registra (IA-026) um único serviço por resgate; o combustível extra sai do estoque externo do próprio prestador, sem moeda criada (teste de conservação monetária).
- **B4:** corrigido (`FirstOrDefault`). **B3/B5:** mantidos; baixo risco.
