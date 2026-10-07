# IndexCities — IA para decisões, simulação e desenvolvimento

> **Revisão humana:** PENDENTE.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o conteúdo deste documento ainda não foi revisado integralmente pelo responsável e não pode ser tratado como decisão.
>

> **Status:** pesquisa concluída para a fase atual — não é fonte de verdade.
>
> A autoridade de produto continua sendo `docs/SPEC.md` e decisões estruturais oficiais pertencem a `docs/ARCHITECTURE.md`. Este documento preserva a pesquisa, os trade-offs e as conclusões atuais sobre uso de modelos de decisão como Jev e Laya no IndexCities.

## Objetivo

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Avaliar se modelos de decisão especializados e baratos podem trazer vantagem real ao IndexCities em dois contextos:

1. **runtime/gameplay** — decisões de cidadãos, famílias, empresas ou outros agentes da simulação;
2. **desenvolvimento** — apoio a código, arquitetura, revisão e evolução da SPEC.

A pergunta central não é apenas se a tecnologia funciona, mas se ela resolve melhor um problema real do projeto do que regras determinísticas, Utility AI, testes/analyzers ou modelos generativos com acesso ao contexto do repositório.

---

## 1. IA local para decisões da simulação

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Conclusão atual:** não recomendada como estratégia de performance e não vale priorizar uma POC apenas para esse objetivo.

Foi avaliada a ideia de usar modelos de decisão do tipo Jev e alternativas locais como Laya para decidir ações de cidadãos, famílias ou empresas.

### Por que não usar no loop central

Inferência neural local continua sendo muito mais cara do que regras, funções de utilidade e filtros simples em C# para decisões massivas. Benchmarks públicos recentes do Laya mostram latência de dezenas de milissegundos por pergunta isolada em GPU e centenas de milissegundos em CPU em algumas configurações, embora batching reduza bastante o custo médio.

Isso é interessante para uma IA, mas não para substituir decisões simples de milhares de agentes.

Além disso:

- usar GPU para inferência durante um jogo 3D disputa recursos com a própria renderização;
- o runtime adiciona consumo de memória e complexidade de distribuição;
- fine-tuning passa a ser parte do produto quando o modelo base não resolve bem o domínio;
- os melhores resultados do Laya dependem de especialização;
- decisões neurais adicionam dificuldade de diagnóstico justamente onde o IndexCities exige causalidade auditável.

### Caminho preferido para performance

Para decisões massivas da simulação, a direção continua sendo:

- decisões locais simples e auditáveis;
- atualização somente quando necessário;
- filtros para reduzir candidatos antes de pontuar;
- processamento em lotes distribuídos no tempo;
- estruturas de dados leves e benchmarkadas;
- regras/Utility AI quando houver múltiplas alternativas com pesos diferentes.

### Possível uso futuro

IA local continua sendo uma opção interessante se surgir um objetivo de **gameplay** que regras explícitas não atendam bem — por exemplo, comportamento deliberadamente menos previsível ou uma camada especial de personalidade/estratégia em decisões raras.

Nesse caso, ela deve ser avaliada como melhora de comportamento, não como otimização de performance, e deve permanecer subordinada ao estado autoritativo e às invariantes do Simulation Core.

---

## 2. Modelos de decisão no processo de desenvolvimento

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Conclusão atual:** não recomendados como camada geral de desenvolvimento, arquitetura ou definição de produto nesta fase.

Jev/Laya não substituem um modelo generativo ou raciocínio humano/assistido por IA para escrever código, projetar arquitetura ou decidir produto. Esses trabalhos são abertos: exigem criar alternativas, combinar contexto amplo, explicar consequências e produzir código ou texto novo.

Modelos System One são mais adequados quando a resposta já está delimitada a escolha, score ou sim/não.

### Nicho potencial

Existe um nicho útil para **gates e classificações repetitivas e bem definidas**, por exemplo:

- classificar risco de um diff;
- escolher qual suíte de testes executar;
- detectar provável violação de uma regra arquitetural explícita;
- rotear uma tarefa para um módulo;
- decidir se um caso deve ser escalado para um modelo maior.

Benchmarks públicos recentes indicam que Jev pode ser competitivo em revisões pequenas quando as regras são explícitas. Isso não demonstra capacidade de revisão aberta de PRs, descoberta geral de bugs ou desenho de arquitetura.

Laya tende a precisar de fine-tuning para ficar forte em domínios específicos, o que adiciona dataset, treinamento, calibração e manutenção.

### Por que não adicionar agora

No IndexCities atual, essa camada provavelmente custaria mais complexidade do que economizaria:

- o volume de decisões repetitivas de desenvolvimento ainda é baixo;
- arquitetura e SPEC continuam evoluindo;
- não existe um conjunto grande e estável de decisões rotuladas;
- um modelo generativo com acesso ao repositório consegue lidar melhor com problemas abertos;
- regras realmente objetivas são mais bem protegidas por código, testes ou analyzers.

### Regra prática

1. **Regra formalizável:** código, teste, analyzer ou outra checagem determinística.
2. **Julgamento fechado, subjetivo e repetitivo em alto volume:** considerar Jev; considerar Laya quando existir dataset estável e houver vantagem concreta em execução local/especialização.
3. **Decisão aberta, arquitetura, design de produto, escrita de SPEC ou geração/revisão profunda de código:** usar modelo generativo com acesso ao contexto do repositório e exigir justificativa verificável.

---

## Critérios para reconsiderar

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


Reavaliar Jev/Laya apenas quando existir uma dor concreta que combine com o formato da tecnologia.

### Na simulação

Reconsiderar se aparecer uma decisão:

- rara o suficiente para não entrar no caminho crítico de performance;
- difícil de modelar satisfatoriamente com regras/Utility AI;
- com valor observável de gameplay;
- cujas opções e invariantes possam continuar sob controle do Simulation Core;
- cuja qualidade possa ser comparada objetivamente com a alternativa determinística.

### No desenvolvimento

Reconsiderar se surgir uma classificação:

- recorrente;
- em volume suficiente para representar custo ou latência relevantes;
- estável o bastante para ter critérios claros;
- acompanhada de exemplos rotulados que permitam medir acurácia.

Não introduzir Jev/Laya apenas como uma segunda opinião genérica.

---

## Resumo da posição atual

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


- **Jev/Laya no core para ganhar performance:** não.
- **POC de IA local apenas para performance:** não vale priorizar.
- **IA local para comportamento raro/especial:** possível no futuro, se houver necessidade concreta de gameplay.
- **Jev/Laya como ferramenta geral de desenvolvimento ou arquitetura:** não.
- **Jev/Laya como gate/classificador especializado em alto volume:** possível no futuro.
- **SPEC, arquitetura e decisões abertas:** continuar usando raciocínio contextual, pesquisa e modelos generativos com acesso ao repositório.
