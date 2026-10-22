# IndexCities — P0: equivalência de tempo e acumuladores

> **Experimento isolado, NÃO aprovado/implementado no jogo.** Ver programa da [PR #52](https://github.com/Marlohn/IndexCities/pull/52). Não modifica `CitySimulation`, SPEC, economia ou Godot; preserva a branch principal. É uma bancada **matemática parcial**, não uma engine executada.

## Objetivo

Tentar refutar a suposição de que “somar 300 passos de dois segundos de uma vez dá sempre o mesmo mundo”. Testa **com o próprio `FixedPoint` da biblioteca**:
- produção em quantidades quantizadas: soma em lote **antes** de quantizar falha;
- avanço exatamente equivalente de quantidade linear com entradas constantes: `n * quantização(delta_por_quantum)` é correto até que uma dependência/limiar interrompa o trecho;
- poluição: arredondar em cada passo difere de um único decaimento exponencial;
- vento: amostrar a cada hora em vez de a cada quantum altera energia disponível;
- fome: verificar apenas no fim do intervalo adia consumo real;
- mudança de taxas por segmentos e 2.000 casos aleatórios: equivalência da composição de deltas constantes.

## Executar

```sh
dotnet run --project experiments/IndexCities.CausalLab/IndexCities.CausalLab.csproj -c Release
```

Retorna JSON e código 0 quando os **contraexemplos realmente divergem** e os casos de composição segura coincidem. Retorna código 1 em falha. Não afirmar que `0` significa viabilidade da arquitetura.

## Evidências e limitações

É um teste pequeno. Não verifica distribuição multiestabelecimento de água/energia, emissões de veículos móveis, mudança de equipe, saúde ligada a escassez real, economia, histórico, save/load ou a aceleração de 10 mil Sims. Para responder se existe ganho integrado, será necessário um executor experimental que detecte todas as alterações/limiares e compare o primeiro acontecimento divergente com `CitySimulation`.

**Importante:** `dotnet` não está disponível no ambiente local desta sessão; o código C# foi publicado mas **não foi compilado/executado aqui**. Uma sondagem separada em Python com fórmulas equivalentes demonstrou contraexemplos e consta no relatório. As saídas C# ainda dependem de compilação/CI ou de um runtime .NET; não tratar a sondagem Python como equivalência bit a bit com C#.
