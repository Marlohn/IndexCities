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

---

## Proposta para calcular a influência ambiental percebida pelos SIMs — 2026-10-11

> **Revisão humana desta seção: PENDENTE.** O comportamento de produto 5C **já foi aprovado na SPEC**: decoração colocada fisicamente pode contribuir, de forma local e contextual, para bem-estar/atratividade/valor imobiliário. A **fórmula e a organização da simulação abaixo são hipóteses técnicas**, não requisitos aprovados, nem sistema novo obrigatório.

**Modelo mínimo sugerido:**

1. **Dados espaciais existentes.** Particionar a cidade em pequenas áreas de consulta espacial (quadras/células ou outra estrutura conforme medição), reaproveitando as buscas e regiões de influência já necessárias a poluição, prédios, parques, serviços e avaliação imobiliária. Cada objeto decorativo relevante contribui para a **presença/qualidade ambiental efetiva perto dele**, não para uma soma mundial de felicidade.
2. **Sem somar itens um por um infinitamente.** Uma área com alguma vegetação melhora em relação à mesma área totalmente asfaltada, mas árvores/canteiros repetidos dentro da mesma área têm **retorno decrescente/teto**. Quantidades de árvores, pontos, raio, pesos, relevância de cerca/banco e aparência de fachadas são calibração/decisão de catálogo, sem assumir que todo objeto tem bônus.
3. **Exposição real do SIM.** Usar como início o entorno da **residência efetivamente ocupada** e, quando justificado, ambientes onde o SIM de fato permaneceu (trabalho, parque, visita) com tempo/contexto; não atribuir a felicidade de um jardim do outro lado da cidade. Amostrar passagem por trechos relevantes apenas se valer o custo, não a cada frame nem inspecionando cada objeto. Se ficar em casa, seu entorno residencial importa; se mudar, o ambiente novo pode gradualmente mudar a contribuição.
4. **Não duplicar causas já simuladas.** Poluição ambiental já influencia saúde/atratividade e parques usados já oferecem benefício de lazer; a contribuição visual/local deve **complementar**, não somar duas vezes a mesma poluição, o mesmo acesso ao parque ou a mesma visita. Lixo acumulado que já afeta atratividade não precisa de penalidade decorativa separada. Não introduzir poluição sonora, clima ou manutenção estética como efeito novo por inferência.
5. **Atualização econômica.** Recalcular apenas áreas impactadas quando houver colocação/remoção relevante, alteração de espaço público, mudança ambiental já existente ou troca do local efetivamente vivido pelo SIM. Derivar indicadores visuais e valor imobiliário de forma compartilhada; não iterar todos os pares objeto × SIM continuamente.
6. **Feedback legível.** Na inspeção de moradia/SIM, explicar influência local por causas entendíveis: «vegetação no entorno», «praça acessível» (se aplicável), «poluição existente», sem exibir fórmula. Se útil, reaproveitar mapas temáticos 9B da SPEC; no Criativo, não fingir uma economia/felicidade equivalente à simulação convencional.

**Exemplo qualitativo:** três árvores e um jardim distribuídos pelo entorno de várias casas podem ajudar mais do que vinte vasos concentrados num mesmo canto sem acesso/uso; um bairro arborizado não neutraliza automaticamente falta de água, fome ou poluição industrial. A regra exata de saturação, alcance, tempo de exposição e relevância por objeto depende de experimento de gameplay e desempenho. **Recomendo começar pela exposição residencial e ampliar para trajetos apenas se o teste demonstrar valor.**

**Evidência, não parametrização literal:** [OMS, espaços verdes e saúde (2016)](https://www.who.int/europe/publications/i/item/WHO-EURO-2016-3352-43111-60341), [OMS, espaços verdes e saúde (2025)](https://www.who.int/publications/i/item/B09366), [revisão de espaços verdes e bem-estar (2021)](https://pubmed.ncbi.nlm.nih.gov/34066187/). As associações não fornecem percentuais automaticamente transferíveis para pontos de felicidade de um jogo.

---

## Referência visual fornecida

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção visual em exploração; a apresentação 3D isométrica já está confirmada na SPEC.

Foram fornecidas imagens de referência produzidas a partir de assets já existentes do projeto/autor. Elas devem ser consideradas nas futuras decisões de direção visual, escala de câmera, densidade urbana e legibilidade.

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

A intenção atual é produzir boa parte dos assets visuais com uma ferramenta/IA externa especializada e integrá-los ao jogo depois. O pipeline exato de formatos, importação, LODs, colisões, materiais e validação ainda não foi definido.

---

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

**Atualização:** a SPEC já aprovou que elementos decorativos/vegetação colocados possam contribuir localmente para bem-estar, atratividade e valor imobiliário (5C). A forma de calcular alcance/exposição e objetos elegíveis permanece em exploração nesta seção, sem efeitos automáticos idênticos por árvore.

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
