# IndexCities — laboratório P0: avanço causal de tempo

> **Experimento isolado (PR #53), não incorporado à V1.** Referência de arquitetura: [PR #52](https://github.com/Marlohn/IndexCities/pull/52). Não modifica `CitySimulation`, SPEC nem Godot.

Este diretório **já foi compilado e executado em .NET 8** por GitHub Actions. Os resultados verificados estão em [RESULTADOS-P0.md](RESULTADOS-P0.md). O ambiente local desta conversa não tem `dotnet`, então as execuções C# foram feitas **no CI do próprio repositório**, não simuladas por Python.

## Executar com .NET 8

```bash
# 5 contraexemplos e 2.000 casos de composição de delta
dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release

# Agenda de limiar vs. milhões de atualizações passo a passo
dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release -- --segments

# Despensa compartilhada, concorrência por alimento, entregas e despertares
dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release -- --shared

# Cidade VERDADEIRA do IndexCities: 108 SIMs, 83 veículos, 1 dia / 24 checkpoints
dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release -- --integrated

# Sonda independente de fórmulas, em Python (não equivale a teste C#)
python3 experiments/IndexCities.CausalLab/p0_math_probe.py
```

CI: [P0: acumuladores e fronteiras temporais](https://github.com/Marlohn/IndexCities/actions/workflows/p0-causal-lab.yml). É importante selecionar a branch do laboratório. `Actions` exige permissões adequadas.

## Limites

- O **motor atual continua executando os 43.200 passos de 2 segundos por dia** em `--integrated`. A equivalência dos seus hashes não representa aceleração.
- `--segments` prova apenas uma decisão local de fome em casa, com despensa própria. `--shared` cobre concorrência por alimento e entregas, mas **não** viagens, saúde, dinheiro, falência ou trajetórias.
- Os cálculos de vento/poluição são sondas reduzidas para *refutar* atalhos, não substitutos das rotinas completas de infraestrutura.
- No futuro, o novo executor só poderá ser considerado equivalente após acoplar os sistemas e comparar os fatos/estados reais sob diversas seeds, congestionamento, nascimentos e anos.
- **Não anunciar speedup de 10k/80 anos.** Ganho em microteste isolado não é ganho do dia integrado; a PR #52 registra o orçamento completo e os critérios de descarte.
