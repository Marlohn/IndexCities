# IndexCities — Processo de desenvolvimento e documentação

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** decidido para a fase atual; histórico e justificativas preservados aqui — não é fonte de verdade.
>
> Este documento preserva a pesquisa que levou ao processo leve de desenvolvimento, às fronteiras documentais e à arquitetura de trabalho. Regras vigentes para humanos e IAs pertencem a `AGENTS.md`; decisões estruturais vigentes pertencem a `docs/ARCHITECTURE.md`.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Processo de desenvolvimento com IA

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido para a fase atual.

### Dor observada

Experiências anteriores mostraram que muita cerimônia de processo — múltiplas etapas, papéis, documentos, branches e handoffs — pode aumentar custo de tokens e tempo sem produzir melhora proporcional no produto.

Ao mesmo tempo, desenvolvimento totalmente sem limites pode causar deriva de escopo, decisões esquecidas e arquitetura desnecessária.

### Alternativas consideradas

- **Spec Kit:** processo forte e completo, mas pesado demais para a fase atual.
- **OpenSpec:** mais leve e flexível, porém ainda adicionaria proposal/spec/design/tasks e estado próprio antes de existir uma dor que justifique isso.
- **BMAD:** orientado a vários papéis e etapas; incompatível com a busca atual por velocidade.
- **Skills especializadas:** ideias úteis de review, retro e execução assistida, mas sem necessidade de adotar um workflow inteiro.
- **Sem processo:** máxima velocidade, mas pouca proteção contra deriva de produto.
- **Spec leve própria:** uma SPEC curta como cerca, com execução agressiva dentro dela.

### Conclusão

O IndexCities adota **Spec-Driven Development (SDD) leve**: a especificação vem antes do desenvolvimento, sem transformar cada mudança em um processo pesado.

Na prática:

- uma única SPEC curta e autoritativa;
- ARCHITECTURE para decisões estruturais;
- AGENTS.md com regras de trabalho;
- EXPLORATION para pesquisa e decisões ainda abertas;
- implementação rápida depois que a intenção estiver definida;
- validação proporcional ao risco;
- nenhum framework adicional de SDD por enquanto.

A pesquisa confirmou que SDD normalmente significa colocar intenção e especificação antes da implementação. Ferramentas como Spec Kit estruturam isso em várias etapas, mas o IndexCities adota apenas o princípio necessário, sem copiar a cerimônia inteira.

### SPEC viva durante todo o ciclo do jogo — direção confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou a regra do processo em conversa; a redação consolidada pela IA ainda não foi revisada integralmente. Regras oficiais estão em `AGENTS.md`.

O contrato da SPEC **não termina com a primeira implementação**. Ele vale para cada feature, mudança de regra, correção de bug, refatoração e manutenção futura. Antes de escrever código, a IA deve localizar a regra já documentada que a mudança realiza ou preserva. Encontrar comportamento ausente, ambíguo ou contraditório **bloqueia somente o código dependente da lacuna**: expor o problema, apresentar consequências/opções e obter a decisão humana; registrar a decisão na SPEC antes de implementar. Não há permissão para completar regra de produto por suposição.

Um bug cujo comportamento correto já está claro na SPEC pode ser corrigido usando essa regra, sem gerar alteração cerimonial no documento. Da mesma forma, refatorações e otimizações podem escolher detalhes internos compatíveis com a arquitetura, desde que preservem o comportamento especificado. Uma decisão estrutural pertence à ARCHITECTURE, mas nunca substitui a aprovação de gameplay na SPEC.

**Referência exploratória nova — revisão humana desta nota: PENDENTE.** O GitHub Spec Kit diferencia modelos em que a especificação é descartada após o código, preservada como âncora de mudanças futuras ou mantida como contrato vivo atualizado antes das mudanças. O IndexCities usa a SPEC persistente e viva, mas **não adota o framework** nem artefatos adicionais por isso. Referências: [Spec Persistence Models](https://github.com/github/spec-kit/blob/main/docs/concepts/spec-persistence.md) e [Spec Kit](https://github.github.com/spec-kit/). Essas referências servem como contexto de pesquisa, não como novas regras.

### Primeira entrega integrada — direção confirmada

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável confirmou **SPEC antes do desenvolvimento**, **primeira implementação integrada** e **validação global posterior**; as considerações técnicas abaixo ainda não foram revisadas integralmente.

**Decidido (regras vigentes em `AGENTS.md`):** não existe roteiro obrigatório de POCs de produção, economia, interface, mapa ou outro sistema. A primeira entrega busca integrar o produto conforme a SPEC; a validação principal de gameplay e interação entre sistemas considera o **conjunto funcional**. Recomendações antigas de "primeira POC" em documentos temáticos são pesquisa histórica, não cortes de escopo autorizados.

**Distinção importante:** entrega integrada **não exige todo o código num único commit nem desenvolvimento sem qualquer teste até o final**. Incrementos técnicos, verificações e testes proporcionais ao risco podem ocorrer durante a implementação. Isso não cria POCs de produto nem marcos burocráticos; ajuda a evitar erros difíceis de localizar na integração global.

**Risco a acompanhar (análise ainda pendente):** integrar muitos sistemas profundos sem observar suas interações pode encarecer correções. Preservar invariantes e diagnóstico causal durante a implementação, sem trocar a estratégia de entrega integrada por etapas de produto isoladas.

### Princípio de evolução

Só adicionar processo quando for possível apontar uma dor real que ele resolve.

Exemplos de sinais para reconsiderar a arquitetura:

- SPEC grande ou difícil de navegar;
- agentes perdendo contexto entre sessões;
- divergências recorrentes entre decisão e código;
- trabalho paralelo causando conflitos;
- necessidade real de rastreabilidade requisito → implementação → teste.

Até lá, simplicidade é uma característica do processo, não uma deficiência.

---

---

## Arquitetura de desenvolvimento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decisão consolidada.

A pesquisa e a direção arquitetural inicial foram consolidadas em [ARCHITECTURE.md](../ARCHITECTURE.md), que passa a ser a autoridade sobre organização técnica, fronteiras e direção de dependências.

Conclusões principais:

- monólito modular, sem microserviços/processos distribuídos como requisito;
- núcleo de simulação em C#/.NET puro, sem dependência de Godot;
- Godot como host/adaptador e camada de apresentação;
- UI como leitura + comandos, sem possuir o estado autoritativo da cidade;
- assets e conteúdo visual desacoplados das regras de gameplay;
- capacidade de execução headless para testes, benchmarks e ferramentas/agentes;
- ECS, multithreading, event bus, DI e outras estruturas adicionais permanecem adiados até existir profiling ou necessidade concreta.

A pesquisa que sustentou essas decisões está resumida no próprio documento de arquitetura. Novas alternativas ou revisões ainda não decididas voltam a ser exploradas aqui antes de alterar a arquitetura oficial.

---

---

## Organização dos documentos de exploração

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido e aplicado.

A organização oficial é:

```
docs/
  SPEC.md
  ARCHITECTURE.md
  EXPLORATION.md
  exploration/
    player-interface.md
    genre-benchmark.md
    <tema>.md
```

Regras:
- `SPEC.md` continua sendo a fonte de verdade do produto;
- `ARCHITECTURE.md` continua sendo a fonte de verdade estrutural;
- `EXPLORATION.md` é o hub/índice da exploração;
- pesquisa extensa, alternativas e rascunhos temáticos ficam em `docs/exploration/<tema>.md`;
- documentos temáticos declaram que não são fonte de verdade e indicam status;
- decisões fechadas são resumidas na SPEC e o material exploratório é marcado como decidido/superado quando aplicável;
- evitar novas subpastas ou arquivos por ritual;
- novos arquivos soltos em `docs/` precisam de uma função especial recorrente e justificável.

A migração é gradual: não é necessário desmontar o conteúdo histórico deste hub de uma vez. Conforme um tema voltar a ser trabalhado, ele pode ser limpo, consolidado e movido para um documento temático.

`exploration/genre-benchmark.md` também fica em `docs/exploration/`. Apesar de ter uso comparativo recorrente, continua sendo material de pesquisa/referência e não uma fonte canônica.

A primeira migração aplicada foi a interface do jogador para [`exploration/player-interface.md`](player-interface.md).
