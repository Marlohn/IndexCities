# IndexCities — Processo de desenvolvimento e documentação

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

**Status:** decisão consolidada.

A pesquisa e a direção arquitetural inicial foram consolidadas em [ARCHITECTURE.md](ARCHITECTURE.md), que passa a ser a autoridade sobre organização técnica, fronteiras e direção de dependências.

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

A primeira migração aplicada foi a interface do jogador para [`exploration/player-interface.md`](exploration/player-interface.md).
