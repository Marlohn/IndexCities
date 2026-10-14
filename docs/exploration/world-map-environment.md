# IndexCities — Mundo, mapa, terreno e ambiente

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa, com algumas decisões já promovidas para a SPEC — não é fonte de verdade.
>
> Reúne direção visual, geração e reprodutibilidade do mapa, terreno, vegetação, clima e decisões de representação física do espaço urbano. Decisões já oficiais devem ser lidas na SPEC.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

## Estudo de água e margens após a partida — 2026-10-14

**Revisão humana desta seção: PENDENTE.** O responsável voltou a considerar rio/lago visualmente insatisfatórios após a integração dos materiais. O coordenador solicitou amostras originais ao **Claude Opus 5.5**, com esforço `max` confirmado no Orca; `ultracode` não é uma opção efetivamente disponível nesse runtime. O estudo está isolado na branch `Marlohn/recuperacao-v1-agua-estudo`, commit `01ef5f7`, sem alteração de gameplay ou integração ao jogo.

![Estado atual, material com a mesma malha e geometria conceitual](evidence/water-study/compare-day.jpg)

O código atual coloca a tampa do lago 9,5 cm acima do chão e usa um cilindro circular. A amostra A preserva a malha e pinta margens irregulares; a B apresenta uma bacia rebaixada e uma ligação a rio apenas na cena de assets. O coordenador conferiu imagens de dia, de noite e da margem de perto. A pintura de terra dentro da área circular bloqueada de A pode confundir o posicionamento, então não é recomendação de integração. B permanece proposta visual; não comprova captação, recursos, profundidade, fluxo ou operação urbana.

![Amostra B: bacia e rio somente na prévia](evidence/water-study/concept-b-day.png)

O Opus executou importação e renderização GL Compatibility com 29 capturas e validador com **1.593 verificações, zero falhas**. A comparação local de custo da prévia não mede FPS nem desempenho da cidade integrada; não foi executada build exportada ou testada outra GPU. O [README e os recursos do estudo](https://github.com/Marlohn/IndexCities/blob/01ef5f71fed550cbef286c2c647a58589c0ab39a/src/IndexCities.Godot/Assets/WaterStudy/README.md) registram dimensões, materiais, cenários e limitações. A avaliação estética humana continua pendente.

Antes de integrar uma geometria diferente, é necessário decidir sua relação com a área física de água, captação, validade das construções, mapas/saves existentes e eventual rio sem pontes na V1. Não há aprovação dessas mudanças nesta seção. A investigação visual deve ajudar a decidir com imagens; não transformar pintura de margem em prova de correção da experiência.

---

## Qualidade ambiental local — referência temática (2026-10-11)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** A direção 5C e seu recorte residencial enxuto foram aprovados na SPEC; cálculos, fontes e resultados de pesquisa continuam PENDENTES de validação.

A proposta antes descrita aqui foi consolidada no documento temático [**Qualidade ambiental local — gameplay, cálculo e performance**](local-environment-wellbeing.md). O modelo inicial prioriza vegetação/jardins reais no entorno de residências, influência local moderada e saturável, consulta de condição ambiental compartilhável pelos moradores e recálculo quando entradas reais mudarem. Não analisar cada decoração/trajeto por SIM ou por frame; não introduzir sistema de felicidade no Criativo por inferência. Fórmula, objetos elegíveis, renderização 3D e custos permanecem para calibração e medição no jogo integrado.

---

## Referência visual fornecida

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção visual em exploração; a apresentação 3D isométrica já está confirmada na SPEC.

**Revisão humana desta nova análise visual: PENDENTE.** Foram fornecidas referências visuais e, posteriormente, **duas capturas de uma cena low-poly produzida por IA via código na Godot, em outro projeto**. Elas ajudam a avaliar linguagem visual, câmera, densidade e legibilidade, **mas não são assets implementados do IndexCities, nem prova de desempenho, medidas ou pipeline já aprovado**. A avaliação técnica está no [estudo de assets](asset-production-pipeline.md).

Características observáveis nas referências:

- visual 3D estilizado, com geometria simples e leitura limpa;
- câmera alta/isométrica capaz de mostrar simultaneamente ruas, calçadas, lotes e edifícios;
- edificações individuais claramente distinguíveis;
- escala urbana de bairro/cidade pequena, com casas, comércio de esquina, vegetação, mobiliário urbano e vias;
- prioridade para legibilidade visual em vez de realismo fotográfico;
- presença visível de detalhes urbanos como faixas de pedestre, postes, bancos, cercas, jardins e mesas externas;
- cidadãos e veículos aparecem em escala compatível com leitura individual.

As imagens são referência de exploração, não especificação dimensional. Medidas, grid, tamanho de lote, distância de câmera e densidade ainda precisam ser definidos e testados.

### Pipeline de assets

**Hipótese atual após o teste visual de 2026-10-11:** validar primeiro **IA criando geometria paramétrica na própria Godot**, gerar/salvar as variantes e reutilizá-las; **Blender e IA 3D externa ficam opcionais** para casos que não funcionarem bem por código. O formato, a escala detalhada, a gestão das malhas, LOD e desempenho ainda exigem teste. Esta é **pesquisa PENDENTE**, não ferramenta ou arquitetura aprovada. Ver [estudo de assets](asset-production-pipeline.md).

---

## Terreno, vegetação e alcance de serviços

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Edição de terreno

**Adiada para o futuro.**

No escopo inicial, o jogador não precisa modificar relevo.

Razão prática: terraplanagem aumenta a complexidade de vários sistemas ao mesmo tempo, incluindo:

- colocação de edifícios;
- vias e inclinações;
- navegação;
- água;
- colisões;
- visual;
- geração/validação do mapa.

A decisão pode ser reavaliada depois que construção, vias, água e terreno-base estiverem estáveis.

### Vegetação

Direção decidida:

- vegetação deve ter presença visual forte;
- árvores e outros elementos naturais podem ser removidos para construção;
- variedade, densidade e integração com ruas/lotes devem receber atenção visual.

**Atualização:** 5C e o recorte inicial de vegetação próxima às moradias estão na SPEC; fórmula, alcance, catálogo e medição continuam em [pesquisa temática própria](local-environment-wellbeing.md), sem bônus automático por árvore.

### Parques, amenidades e serviços

O modelo de alcance e influência ainda está em pesquisa. A seção específica **"Alcance, influência e escolha de serviços"** contém a comparação atual entre raio rígido, heurística de influência, custo de viagem e modelos híbridos.

### Poluição sonora

**Fora do escopo atual.**

Pode ser reavaliada futuramente caso ruído se prove relevante para bem-estar, moradia ou valor imobiliário.


---

---

## Geração de mapa e reprodutibilidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** estrutura decidida; algoritmo ainda em exploração.

Decidido:

- seed determinística;
- mesma seed deve reproduzir o mesmo mapa-base;
- seed será ferramenta importante de debug, teste e reprodução de bugs;
- terreno inicial já inclui natureza e conexão externa;
- sem compra de tiles/áreas no escopo atual;
- borda fixa no início.

Ainda precisa ser definido:

- geração da base plana do mapa;
- distribuição de vegetação;
- geração de rio/lago;
- posição e quantidade de conexões externas;
- tamanho do mapa;
- como versionar a geração para que uma mesma seed continue reproduzível quando o algoritmo mudar.

### Observação importante para debug

Se o algoritmo de geração evoluir, apenas guardar a seed pode não ser suficiente para reproduzir mapas antigos. Uma solução futura pode exigir armazenar também a **versão do gerador** ou serializar o mapa resultante no save. Isso deve ser considerado quando a implementação começar.


---

---

## Terreno plano no escopo inicial

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** decidido.

O mapa inicial do IndexCities será plano.

Consequências para o escopo atual:

- não há morros ou variações relevantes de elevação;
- não é necessário resolver inclinação de ruas ou prédios nesta fase;
- edição de terreno continua adiada;
- rios e lagos podem existir em um terreno essencialmente plano;
- pontes e outras estruturas que dependam de desnível devem ser avaliadas separadamente, sem assumir relevo acidentado.

Essa decisão reduz complexidade de construção, pathfinding, geração de mapa e validação durante as primeiras fases do projeto.


---

---

## Construção híbrida: grid lógico + liberdade de posicionamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** **direção de produto decidida na SPEC em 2026-10-08** (alternativa C: grade lógica com posicionamento assistido). A geometria exata, os detalhes de snapping e o custo de consultas espaciais seguem em validação no jogo integrado, sem POC isolada obrigatória.

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — o responsável escolheu explicitamente o modelo híbrido; evidências comparativas, hipóteses de implementação e análise de riscos redigidas pela IA permanecem sem revisão integral.

A escolha rejeita tanto o **encaixe visual rigidamente limitado a células completas** quanto o **posicionamento inteiramente livre como obrigação**. A grade fornece referência lógica para ocupação/consultas espaciais, enquanto a interface oferece encaixes úteis com alguma liberdade de ajuste. A ocupação efetiva, colisões, acessos viários, conexões e demais restrições físicas continuam vinculantes. A confirmação de uma obra não pode depender apenas de alinhamento visual. A rua ortogonal em L, a proibição de ligação direta de edifícios a rodovias e os viadutos em níveis separados permanecem definidos pela SPEC.

**Trade-offs aceitos:** menor rigidez para o jogador e layouts mais variados, ao custo de validar footprints e acessos que podem não coincidir exatamente com células de grade. A implementação deve evitar geometria arbitrária difícil de diagnosticar, rechecagens globais contínuas e falsa conexão visual. A divisão da grade em células menores para mobiliário/detalhes de rua é uma hipótese técnica, **não requisito do produto**.

### Evidência comparativa

Farthest Frontier começou com construção em grid por razões estratégicas e, em 2026, adicionou posicionamento livre em 360° após feedback recorrente dos jogadores. O jogo manteve a possibilidade de alternar entre modo livre e grid durante a colocação. Algumas estruturas continuam presas ao grid por causa das restrições do pathfinding.

Fontes:
- Crate Entertainment — Free-Build Mode: https://forums.crateentertainment.com/t/v1-1-patch-preview/152189
- Crate Entertainment — v1.1.0: https://forums.crateentertainment.com/t/farthest-frontier-v1-1-0/153834
- Godot 4.5 — GridMap: https://docs.godotengine.org/en/4.5/classes/class_gridmap.html

### Hipóteses técnicas a verificar na implementação integrada

A **existência de grade lógica e o posicionamento assistido já foram decididos**; os mecanismos abaixo seguem como possibilidades, sem aprovação individual de cada item.

Testar:
- uma **grade lógica interna** para ocupação, colisão, lotes e consultas espaciais;
- posicionamento visual com alguma liberdade;
- snap opcional a borda da rua, alinhamento com vizinhos, linhas/células da grade e ângulos úteis;
- possibilidade de mostrar/ocultar a grade como ajuda visual.

### Riscos a medir na validação integrada

- footprints irregulares;
- gaps visuais;
- colisão entre construções;
- conexão correta com calçada/rua;
- estacionamento e pontos de carga;
- pathfinding de pedestres;
- performance das consultas espaciais;
- clareza do feedback de posicionamento.

---

---

## Clima e ambiente sazonal

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** documentado para futuro; fora do escopo atual.

Tópicos preservados para reavaliação futura:

- chuva;
- temperatura;
- estações do ano;
- efeitos climáticos sobre a cidade;
- chuva forte e alagamentos;
- impactos de alagamento no trânsito e em edificações;
- aumento de risco de incêndio em períodos secos/quentes;
- variação de consumo de energia com frio/calor;
- redução de disponibilidade de água em períodos secos.

Nenhum desses sistemas deve ser implementado no escopo atual. Devem ser revisitados quando a simulação básica de mobilidade, serviços urbanos, energia, água e emergências estiver estável.


---
