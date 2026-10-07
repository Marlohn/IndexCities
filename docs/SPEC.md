# IndexCities — SPEC

> **Este documento é a fonte de verdade do produto.**
>
> Ele descreve o estado desejado atual, não o histórico do projeto. Deve permanecer curto, útil e atualizado.
>
> Nova funcionalidade de produto entra aqui **antes** da implementação. Detalhes técnicos que não definem comportamento não precisam estar aqui.

## 1. Visão

IndexCities é um **city builder 3D desktop**, inspirado em jogos como Cities: Skylines, com foco em:

- construir e acompanhar uma cidade viva;
- simular pessoas e sistemas urbanos de forma compreensível;
- usar referências brasileiras quando regras, dados ou números representarem o mundo real;
- suportar cidades grandes sem sacrificar jogabilidade;
- apresentar uma direção visual low-poly limpa, acolhedora e legível;
- ser desenvolvido com forte ajuda de IA sem depender de uma IA/LLM para o jogo funcionar.

O objetivo inicial não é construir todos os sistemas possíveis. É chegar rapidamente a um núcleo jogável e convincente e expandir a partir de evidência real.

## 2. Plataforma e direção técnica atual

- **Engine:** Godot 4 .NET.
- **Linguagem principal:** C#.
- **Plataforma:** desktop-first.
- **Visual:** 3D low-poly estilizado.
- **Mapa lógico de referência:** 256 × 256 tiles de 16 m, enquanto essa escala continuar adequada.
- O projeto deve poder ser executado e desenvolvido sem depender de um orquestrador de IA específico.

Essas escolhas podem mudar no futuro, mas somente por decisão explícita registrada nesta SPEC.

## 3. Garantias do produto

### Determinismo

Quando um sistema for definido como determinístico, **mesma seed + mesmos comandos = mesmo resultado**.

Aleatoriedade de gameplay deve passar por uma fonte controlada de RNG; tempo de relógio ou aleatoriedade global não deve alterar silenciosamente a simulação.

### Dados e realidade

Regra, número ou parâmetro que represente o mundo real deve ter origem clara e, quando apropriado, viver em dados/configuração em vez de ficar espalhado no código.

### Save e replay

Quando save/replay forem introduzidos, passam a ser contratos importantes. Mudanças de formato devem ser deliberadas e compatibilidade deve ser preservada ou migrada conscientemente.

### Performance

Cidade grande faz parte do produto. Otimizações devem ser guiadas por medição, mas arquitetura obviamente incompatível com escala não deve ser aceita por conveniência.

No renderer, grandes conjuntos repetidos devem permitir particionamento e culling adequados; não desenhar a cidade inteira sem necessidade.

### Separação de responsabilidades

Regra de jogo não deve depender de renderização. A fronteira exata pode evoluir com o projeto, mas a simulação deve continuar testável sem exigir a cena visual completa.

## 4. Fase atual — fundação jogável

A prioridade é criar uma base pequena que prove a nova direção antes de portar ou inventar sistemas em massa.

### Dentro do escopo

- projeto Godot 4 .NET que abre, compila e roda de forma reproduzível;
- câmera e navegação adequadas a um city builder;
- uma pequena cena urbana visualmente convincente;
- carregamento e uso de assets 3D com licença compatível;
- ruas, lotes/prédios e vegetação suficientes para validar a leitura da cidade;
- uma cena/fixture determinística de escala maior para medir renderer e navegação;
- instrumentação básica de performance quando necessária para decisões;
- estrutura mínima para posteriormente integrar ou portar simulação.

### Referência visual e técnica

O trabalho anterior do CityBuilder pode ser consultado como evidência, especialmente:

- POCs visuais e assets aprovados;
- testes de escala e lições de culling;
- simulação determinística;
- contratos, save/replay, config e dados.

Referência histórica principal: [CityBuilder #322](https://github.com/Marlohn/CityBuilder/issues/322).

Reaproveitar conceitos ou código somente quando simplificar o IndexCities. **Não portar a arquitetura antiga inteira por inércia.**

## 5. Fora de escopo por enquanto

Até a fundação jogável estar convincente, não priorizar:

- portar toda a simulação do CityBuilder de uma vez;
- reproduzir todo o roadmap antigo;
- adicionar sistemas de gameplay sem necessidade para o vertical slice atual;
- arquitetura genérica para necessidades futuras hipotéticas;
- framework pesado de processo ou SDD;
- produção massiva de documentação;
- dependência de LLM em runtime;
- otimização sem medição, salvo problemas estruturais evidentes.

Ideias fora deste escopo podem ser registradas para avaliação futura, mas não devem entrar silenciosamente na implementação.

## 6. Primeiro marco de maturidade

Consideramos a fundação validada quando:

1. o projeto abre e roda de forma reproduzível no desktop;
2. a câmera e a interação básica dão sensação clara de city builder;
3. existe ao menos uma composição urbana que represente a qualidade visual desejada;
4. uma cena significativamente maior continua navegável e fornece métricas suficientes para decisões de performance;
5. a estrutura permite começar a integrar gameplay/simulação sem reescrever a base visual;
6. conseguimos continuar desenvolvendo rapidamente sem depender de cerimônia de processo.

Depois desse marco, esta SPEC deve ser revisada antes de ampliar o escopo.

## 7. Regra para novas ideias

Antes de implementar uma nova funcionalidade de produto, responder:

1. Ela ajuda diretamente o jogo que está descrito aqui?
2. É necessária agora ou pode esperar?
3. Como saberemos que funcionou?
4. O custo de performance e complexidade é aceitável?

Se a decisão for implementar, atualize esta SPEC de forma curta e então escreva o código.

A SPEC deve crescer com o jogo, **não antes dele**.
