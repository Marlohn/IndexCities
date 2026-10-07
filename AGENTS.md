# Regras de desenvolvimento do IndexCities

Estas regras valem para humanos e IAs. O objetivo é maximizar progresso jogável sem transformar desenvolvimento em burocracia.

## Fonte de verdade

- `docs/SPEC.md` define o **produto decidido** e o escopo atual.
- `docs/EXPLORATION.md` guarda **pesquisa, ideias, alternativas e decisões ainda em discussão**.
- O código e o histórico deste repositório definem o estado real da implementação.
- O [CityBuilder](https://github.com/Marlohn/CityBuilder) é referência histórica e técnica, não uma arquitetura a ser copiada por padrão.

Antes de uma mudança relevante, leia a SPEC e somente o código necessário para entender o alvo. Consulte a EXPLORATION quando a tarefa depender de uma discussão ainda aberta ou do raciocínio que levou a uma decisão.

## Arquitetura de especificação

O IndexCities usa deliberadamente uma arquitetura de especificação mínima.

### SPEC

`docs/SPEC.md` é a autoridade sobre **o que o jogo deve ser agora**.

Ela deve:

- descrever o estado desejado atual do produto;
- permanecer curta, legível e útil;
- registrar somente decisões já tomadas;
- dizer o que está dentro e fora do escopo atual;
- descrever requisitos e garantias de produto, não detalhes acidentais de implementação.

Ela não deve virar:

- diário de desenvolvimento;
- histórico de discussões;
- depósito de ideias;
- lista de tarefas;
- especificação detalhada de classes e arquivos;
- documento gigante tentando prever todo o futuro.

### EXPLORATION

`docs/EXPLORATION.md` é o espaço de pensamento.

Use para:

- pesquisar uma ideia;
- comparar alternativas;
- registrar referências, prós e contras;
- manter perguntas ainda abertas;
- preservar por que uma opção foi aceita ou descartada.

Quando uma discussão fecha, **a decisão final entra resumida na SPEC**. A EXPLORATION pode preservar o raciocínio e as evidências, mas não substitui a SPEC.

### Evolução futura

Não adote OpenSpec, Spec Kit, BMAD ou outro framework de processo apenas por parecer mais completo.

A arquitetura só deve ficar mais sofisticada quando existir uma dor concreta e recorrente, por exemplo:

- a SPEC única ficou grande demais para continuar clara;
- agentes perdem contexto importante entre sessões;
- implementação e SPEC divergem repetidamente;
- múltiplas frentes paralelas começam a conflitar;
- rastreabilidade adicional passa a economizar mais tempo do que custa.

Até isso acontecer, **uma SPEC + uma EXPLORATION + este AGENTS.md são suficientes**.

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
- Planejamento deve ser proporcional ao risco: experimento visual pode ir quase direto para código; mudança estrutural, persistência, determinismo ou contrato merece mais cuidado.

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
2. Se a decisão ainda estiver aberta, pesquise e registre o raciocínio na EXPLORATION antes de transformá-la em produto.
3. Inspecione somente o necessário no código e, quando útil, no CityBuilder.
4. Implemente diretamente.
5. Rode a validação relevante para a mudança.
6. Revise o diff final e confirme que não adicionou escopo não pedido.
7. Atualize a SPEC somente quando o produto mudou.

## Definição prática de pronto

Uma mudança está pronta quando o comportamento pedido funciona, a validação adequada passa e não existe divergência conhecida entre implementação e SPEC.

Arquitetura elegante, documentação extra e processo perfeito não são objetivos por si só.
