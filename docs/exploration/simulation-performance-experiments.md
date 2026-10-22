# IndexCities — Registro dos experimentos de desempenho

> **Exploração técnica, sem autoridade de produto ou arquitetura.** Este documento registra evidências e conclusões provisórias. A [SPEC](../SPEC.md) define o jogo, [AGENTS](../../AGENTS.md) define como trabalhar, e o código da `main` define o que está integrado. Nenhum ganho de benchmark, branch ou PR experimental aprova uma mudança de gameplay ou um merge.

## Objetivo investigado

Descobrir como simular uma cidade com **até 10 mil SIMs individuais** e avançar décadas de calendário real em tempo de jogo praticável, sem omitir compras, deslocamentos, trabalho, escassez, pagamentos, mortes ou outras consequências causais. O alvo exploratório de **80 anos em 20–40 horas reais** equivale aproximadamente a **2,5–4,9 segundos de execução por dia simulado**; **é referência de pesquisa, não meta oficial comprovada**. Os números precisam ser revistos em cidade representativa e hardware de referência.

A pergunta decisiva é: **quanto uma mudança reduz o tempo de um dia simulado completo, mantendo o mesmo estado e os mesmos acontecimentos?** Ganho de uma função isolada não responde isso.

## Experimentos atuais (2026-10-21, horário de Brasília)

| Experimento | Branch preservada | Evidência principal | Conclusão / situação |
| --- | --- | --- | --- |
| **E01 — vizinhos da malha viária** | [`experimento/indice-vias-10k`](https://github.com/Marlohn/IndexCities/tree/experimento/indice-vias-10k) | 10 mil nós: **391,32 → 31,25 ms**, **12,52×** na reconstrução; vizinhos e ordem idênticos; **8/8** testes específicos e **442/442** gerais. [Execução C#](https://github.com/Marlohn/IndexCities/actions/runs/39697599407). | **Ganho local confirmado**; ganho de um dia completo não demonstrado. [PR #50](https://github.com/Marlohn/IndexCities/pull/50) **encerrada sem merge**; branch preservada. |
| **E02 — cache de acesso prédio–rua** | [`experimento/cache-acesso-ruas-10k`](https://github.com/Marlohn/IndexCities/tree/experimento/cache-acesso-ruas-10k) | Em 4 passagens sobre **10 mil prédios sintéticos**, **40 mil → 10 mil** buscas completas, respostas iguais; **4,80–4,84×** na consulta em três execuções; **3/3** testes específicos e **437/437** gerais. [Execução C#](https://github.com/Marlohn/IndexCities/actions/runs/39701334607). | **Redução de buscas confirmada no cenário extremo**. Dez mil SIMs **não equivalem** a dez mil prédios. A branch contém instrumentação temporária; não mesclar como está. |
| **E03 — combinação E01 + E02** | [`experimento/performance-combinada`](https://github.com/Marlohn/IndexCities/tree/experimento/performance-combinada) | **11/11** testes específicos, **445/445** gerais. Saves finais com **SHA idêntico** à `main` em duas cidades: `cycle` com 47 SIMs (**1,36 s → 1,44 s/dia**), `grid` com 108 SIMs (**2,95 s → 2,96 s/dia**). O microbenchmark de malha nessa execução marcou **14,15×**, mas fora do dia simulado. [Execução integrada](https://github.com/Marlohn/IndexCities/actions/runs/39705009432). | **Equivalência dos saves nos cenários testados; ganho integrado não demonstrado.** Uma execução por cenário, cidades pequenas e variabilidade do runner impedem afirmar regressão estável. |

| **E04 — índice espacial dinâmico para encontros entre pedestres** | [`experimento/indice-pedestres-dinamico`](https://github.com/Marlohn/IndexCities/tree/experimento/indice-pedestres-dinamico) | Busca por vizinhos próximos, mantendo posição atualizada na ordem sequencial dos SIMs. **434/434** testes. **SHA de save idêntico** em quatro cidades/seed; dias antes → depois: 47 SIMs **2,56 → 2,61 s**; 108 SIMs **5,40 → 5,66 s**; 107 SIMs **5,43 → 5,56 s**; 400 SIMs **212,22 → 207,51 s**. Alocação no maior caso **85.843 → 86.308 MB/dia**. [Execução](https://github.com/Marlohn/IndexCities/actions/runs/39707006075). | **Ganho integrado insuficiente e incerto** (2,2% numa única rodada de 400; perda nos menores); alocação piorou ligeiramente. **Não integrar**; falta perfil preciso do custo restante de `MoveCitizens`. |

**Leitura conjunta:** E01 e E02 eliminam trabalho repetido identificável, mas E03 não demonstrou ganho integrado em cidades pequenas. E04 foi medido até 400 SIMs e **também não reduziu substancialmente o tempo do dia**: indexar encontros próximos não basta para acelerar a rotina de mobilidade. Isso exige perfil mais específico antes de outra otimização. Não há evidência para extrapolar esses experimentos para 10 mil pessoas ou décadas.

### Limites dos testes

- `roadgraph 10000` contém **10 mil pontos de rua**, não 10 mil habitantes.
- `accesscache 10000` contém **10 mil prédios artificiais**, não uma cidade construída e economicamente operante. O controle de 3 mil prédios teve **3 mil misses em ambas as políticas**; a variação de tempos nesse controle revelou ruído/viés. O critério mais robusto foi a contagem de consultas evitadas.
- E03 usa cidades **com 47 e 108 SIMs**, com um dia medido após um dia de aquecimento; ambas terminaram com o mesmo SHA por cenário. Isso **não valida eventos intermediários isoladamente** nem operação por dezenas de anos.
- E04 exercitou apenas um dia medido com 47–400 SIMs e **uma comparação pareada por cenário**, em runner compartilhado. Apesar dos SHAs finais idênticos e testes atuais aprovados, isso não substitui rastrear todos os estados intermediários em casos adversariais, nem valida o benefício sob congestionamento crônico.
- Nenhuma dessas execuções testa conjuntamente **10 mil SIMs, trânsito congestionado, economia pressionada, histórico de décadas e renderização Godot**.
- As medições foram feitas em runners compartilhados de GitHub Actions. O hardware, a concorrência e a versão do código podem afetar tempos absolutos; comparar repetições e o cenário integrado antes de atribuir aceleração.

## Como preservar experimentos daqui para frente

**Um registro, sem colecionar branches e PRs desnecessárias.** Quando um experimento produzir aprendizado útil, acrescentar aqui uma entrada curta com: hipótese, base/commit, cenário e comando reproduzível, resultado de custo, conferência de causalidade, limitações e desfecho. Referenciar logs de CI e código pelo commit/branch. Se falhou ou piorou o jogo, **registrar também**, para não repetir a tentativa.

O **código experimental** pode permanecer temporariamente em branch isolada, enquanto houver valor em reproduzir ou comparar. Não precisa ser incorporado à `main`, nem receber uma PR individual. Após consolidar evidência e não haver perspectiva concreta de reaproveitamento, a branch pode ser removida **em decisão posterior**, sem perder o registro. Artefatos temporários do CI podem expirar: números essenciais, versões e comandos devem permanecer no documento.

A **documentação exploratória** pertence a `docs/exploration/`; materiais conceituais continuam em [pesquisa do motor](simulation-engine-performance-research.md), [ritmo e velocidades](ritmo-velocidades.md) e [cache da simulação](simulation-cache-feasibility.md). Este arquivo é **índice de evidências dos experimentos**, não uma segunda SPEC, backlog, processo obrigatório ou reprodução dessas pesquisas.

Uma **PR de implementação** só faz sentido depois que uma candidata apresentar ganho relevante no cenário integrado, preservar os resultados esperados e passar pela revisão/QA previstos em `AGENTS.md`. Branches experimentais existentes ficam **paradas**, sem merge nem limpeza automática.

## Próxima pergunta útil

Os experimentos E01–E04 não demonstraram ganho integrado compatível com a meta de 10 mil SIMs. O perfil histórico encontrou `MoveCitizens` dominante em uma cidade de ~1.000 pessoas, **mas E04 mostra que a varredura por vizinhos sozinha não explica o custo inteiro**. A próxima investigação recomendável é **medir as partes internas de `MoveCitizens` e suas alocações em cidades ativas**, evitando escolher outra estrutura de dados por palpite. Isso é direção de pesquisa, não requisito.
