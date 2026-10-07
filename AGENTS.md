# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso jogável sem transformar desenvolvimento em burocracia.

## Fonte de verdade

- `docs/SPEC.md` define o produto e o escopo atual.
- O código e o histórico deste repositório definem o estado real da implementação.
- O [CityBuilder](https://github.com/Marlohn/CityBuilder) é referência histórica e técnica, não uma arquitetura a ser copiada por padrão.

Antes de uma mudança relevante, leia a SPEC e somente o código necessário para entender o alvo. Não investigue, documente ou planeje por ritual.

## Regra principal de escopo

- **Funcionalidade nova de produto precisa estar na SPEC antes de ser implementada.**
- Se uma solicitação introduzir comportamento novo ainda não especificado, atualize a SPEC como parte da mesma mudança e então implemente.
- Bug que apenas restaura comportamento já especificado pode ser corrigido diretamente.
- Experimentos e POCs podem acontecer rapidamente fora da SPEC enquanto estiverem isolados. Só viram produto quando forem aceitos e incorporados à SPEC.
- Não invente features, abstrações ou sistemas para um futuro hipotético.

## Filosofia de desenvolvimento

- Priorize resultado jogável e feedback rápido.
- Prefira a menor mudança coerente.
- Não existe obrigação de criar issue, PR, branch especial, plano, ADR, TDD ou documento extra para toda mudança.
- Use testes quando eles protegem comportamento ou evitam regressão; não crie testes cerimoniais.
- Não crie handoffs artificiais entre papéis de IA.
- Processo novo só entra quando resolve uma dor recorrente e observável.
- Não adote OpenSpec, Spec Kit ou outro framework de processo sem uma decisão explícita baseada em necessidade real.

## Garantias técnicas

Estas lições do CityBuilder continuam válidas até a SPEC decidir o contrário:

- Plataforma alvo: **Godot 4 .NET + C#**, desktop-first.
- Mesma seed + mesmos comandos devem produzir o mesmo resultado quando o sistema for determinístico.
- Separe regra de simulação de apresentação/renderização quando isso preservar testabilidade e determinismo.
- Regras e números que representam o mundo real devem ficar em dados/configuração quando apropriado e ter fonte confiável.
- Preserve compatibilidade de save/replay quando formatos persistidos passarem a existir; mudança incompatível precisa ser deliberada.
- Performance é requisito de produto: medir antes de construir otimizações grandes.
- Não copie assets ou código de terceiros sem verificar licença e atribuição aplicáveis.

## Como trabalhar

1. Entenda o comportamento desejado na SPEC e no pedido atual.
2. Inspecione somente o necessário no código e, quando útil, no CityBuilder.
3. Implemente diretamente.
4. Rode a validação relevante para a mudança.
5. Revise o diff final e confirme que não adicionou escopo não pedido.
6. Atualize a SPEC somente quando o produto mudou.

## Definição prática de pronto

Uma mudança está pronta quando o comportamento pedido funciona, a validação adequada passa e não existe divergência conhecida entre implementação e SPEC.

Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
