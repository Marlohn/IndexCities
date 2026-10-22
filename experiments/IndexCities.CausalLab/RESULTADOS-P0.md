# E23 / P0 — primeira sonda de tempo e acumuladores

> **Estado em 2026-10-22:** TESTE MATEMÁTICO PYTHON EXECUTADO; **VALIDAÇÃO DO EXECUTÁVEL C# PENDENTE**. Não é uma simulação integrada, não altera gameplay, não comprova aceleração em 10 mil SIMs. A numeração E23 é apenas um identificador local provisório para esta branch, **não representa registro oficial na PR #51**.

## Objetivo

Examinar se podemos pular muitos passos temporais de 2 s dos sistemas estacionários sem mudar estados, arredondamentos ou momentos em que fatos reais acontecem. Fonte: [proposta PR #52](https://github.com/Marlohn/IndexCities/pull/52).

## O que foi executado de verdade

```bash
python3 experiments/IndexCities.CausalLab/p0_math_probe.py
```

Teste executado no ambiente local da sessão com **Python 3.13**, usando `decimal` e as fórmulas públicas de quantização, decaimento, vento e fome do repositório. É uma **sonda independente de contraexemplos**, não execução direta do C#; arredondamentos binários/decimais do C# devem ser auditados separadamente.

**Resultados observados na sonda:**

| Componente | Referência 2s | Atalho ingênuo | Equivalência |
| --- | ---: | ---: | --- |
| Produção com incremento de 0,49 nano a cada 2 s, 10 passos | 0 nanos | 5 nanos | **NÃO** |
| Produção com incremento de 0,51 nano a cada 2 s, 10 passos | 10 nanos | 5 nanos | **NÃO** |
| Poluição com decaimento e arredondamento por passo, 10 minutos | 0,31383218650738038121 | 0,31383218650737420606 | **NÃO** |
| Energia eólica em um dia, capacidade nominal 100, seed 42 | 59,354634294 unidades | 58,259140884 unidades (amostragem por hora) | **NÃO** |
| Consumo alimentar com fome inicialmente em 11h59m, 1 alimento em casa | fato aos 60 s | fato aos 120 s | **NÃO** |

Os números são do **modelo reduzido da sonda**, não de uma cidade completa. O comportamento real combina esses sistemas em cascata e pode amplificar pequenas diferenças.

### O que ainda é uma possibilidade promissora

Um acumulador puramente linear, com entradas rigorosamente constantes, sem limiar, teto, mudança de fonte, arredondamento intermediário distinto ou dependência do tempo, pode compor **o delta já quantizado por passo**: `base + N * Quantize(delta_per_step)`. Essa identidade foi usada matematicamente no script e está incluída como teste automatizado no C# (2.000 intervalos aleatórios e mudança de taxa por segmentos). **A otimização só é válida até o primeiro limite/alteração causal**, que precisa ser conhecido/certificado pelo sistema.

### Próxima validação necessária

- Executar `dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release` numa máquina com .NET 8 e registrar JSON, tempo e SHA do código. O C# usa diretamente o `FixedPoint` real do IndexCities; no ambiente atual o SDK .NET não existe, portanto **não declarei testes C# aprovados**.
- Transformar o contraexemplo em experimento integrado: comparar `UpdateInfrastructure` → energia/água → `UpdateNeeds` → saúde e consumo, com alterações reais de rede/produção, e gravar o primeiro estado/fato divergente. O teste aqui **não atesta** essa integração.
- Só considerar saltos de tempo quando houver prova de limiares e eventos futuros de todas as entradas, inclusive vento, poluição, quantização por passo, dinheiro e suas cascatas; se não houver, seguir no caminho de atualização detalhada.
- Manter o teste da outra sessão (E22, trânsito) independente, sem criar competição ou conflito de branches.

**Conclusão provisória:** o primeiro filtro refutou **cinco atalhos comuns**. Não refutou o motor causal, mas demonstrou por que ele precisa de composição discreta exata e agendamento de fronteiras de estados. Não há ainda medição de velocidade útil ou viabilidade de 80 anos.
