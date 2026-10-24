# Escalabilidade do motor integrado — experimentos

> **Pesquisa exploratória isolada, sem autoridade de produto.** A fonte do produto é `docs/SPEC.md`; regras de trabalho em `AGENTS.md` e arquitetura em `docs/ARCHITECTURE.md`. Esta branch/PR não altera a V1 nem autoriza integração.

## Pergunta central

Qual é a velocidade **efetiva e causalmente correta** do `IndexCities.Simulation` atual, com população, trânsito, economia, serviços, eventos e histórico de pessoas reais do motor? O que impede aproximar uma cidade futura com **10.000 ou mais SIMs** de uma experiência de várias gerações em algumas sessões?

**80 anos em 6 horas é hipótese de benchmark**, não requisito aprovado da V1. O hardware de referência e o ritmo final permanecem em calibração. Nenhuma otimização pode omitir viagens, vidas, transações, estoques, ordem causal ou histórico factual.

## Fronteira e interpretação

- **Esta PR:** benchmark e diagnóstico do **núcleo integrado real** `src/IndexCities.Simulation` por `src/IndexCities.Headless`, com eventual comparação ao host Godot quando existir cenário confiável. Alterações de instrumentação/fixtures só nesta branch, sem alterar produção em `main`.
- **PR #51:** pesquisa E01–E29 de otimização de trânsito, independente e preservada; não adotar candidatos não validados automaticamente.
- **PR #55:** motor C# experimental próprio, simplificado; resultados de décadas e histórico não demonstram desempenho do motor integrado. Preservar a pesquisa e não portar sistemas indiscriminadamente.
- **PR #52/#53:** pesquisa separada; não tratar experimentos como comportamento aprovado.
- A fixture `cycle` usa construção e sistemas reais, mas é pequena; `grid` usa o mesmo `CitySimulation` **com atalhos artificiais de preparação**: conclusão instantânea de obras, capacidade de utilidades ampliada, orçamento elevado, e configurações sintéticas de mapa/famílias. É **teste de estresse técnico, não cidade representativa**. Não declarar prova de 10 mil habitantes a partir dela.
- O motor atual usa `Quantum=2` segundos; **80 anos** corresponderiam a cerca de **1,26 bilhão de passos** se mantido esse executor. Não extrapolar tempo de um único dia linearmente sem verificar aumento do custo ao longo do tempo, população e histórico.
- Teste sem Godot não mede renderização, responsividade visual nem custo da interface.

## Protocolo enxuto

1. Medir a **linha de base do motor real** em Release com cenários existentes `cycle` e `grid`, **duas repetições sequenciais por condição** no mesmo job/runner, seed e parâmetros fixados. Coletar tempo de aquecimento, população final, dias concluídos, tempo real/dia, aceleração efetiva, alocação, GC, memória, tamanho de save, custo save/load, hash e invariantes. A execução de um teste isolado não permite declarar equivalência causal com outra implementação.
2. Confrontar os resultados com a representatividade das fixtures; preparar o **menor cenário integrado com carga realmente exercida**, distinguindo residentes vivos, cidadãos em circulação, veículos efetivos, viagens, turnos, comércio, água/energia/esgoto e persistência. Não “atingir 10 mil” inflando números ou capacidade sem registrar a artificialidade.
3. Aumentar primeiro **carga e horizonte de forma discriminante**, com limites explícitos de custo/timeout e checkpoints, sem lançar 80 anos cegamente. Perfilar somente o gargalo que os dados apontarem. Uma candidata exige A/B reproduzível com estado causal, invariantes e cobertura adversarial, antes de considerar adoção.
4. Quando um gargalo estrutural demonstrado impedir progresso, documentar o limite e reavaliar abordagem. Não colecionar experimentos ou commits sem ganho de informação.

## Primeira execução — baseline C# real

Workflow isolado `.github/workflows/integrated-scale.yml`, disparado por **push de instrumentação/workflow relevante** nesta branch. Começar com `scale cycle` e `scale grid`, `--seed=42 --warmup=1 --days=1 --cap=300`, e grid `--rows=3 --length=400`. Duas repetições por condição; `cap` interrompe só **depois** do dia completo, por isso existe timeout do job. Nenhum número de desempenho é aceito até ler o GitHub Actions correspondente.

**Resultado:** aguardando a primeira execução real (não confundir criação do workflow com teste concluído).

## Registro de resultados

Cada entrada nova deve trazer: hipótese; SHA da branch; workflow/job/log; fixture/seed/parametrização e atalhos; **população real e atividade efetiva**; repetições e dispersão; wall/CPU quando disponível, RAM/GC, dias/s real; invariantes/hash/checkpoints; falhas, limites e **decisão técnica**. Se o teste falhar, registrar causa; não declarar ganho por CI verde, nem substituir prova do motor real pelo laboratório da PR #55.

**Próxima decisão:** usar os números do primeiro CI para escolher entre corrigir a fixture, ampliar carga real ou perfilar o gargalo dominante. Não executar automaticamente teste de décadas antes dessa escolha.
