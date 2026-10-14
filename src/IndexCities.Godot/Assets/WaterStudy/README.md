# IndexCities — estudo de água e margens (WaterStudy)

> **Revisão humana: PENDENTE.** Estudo visual da frente de assets (IA), pedido pelo coordenador em 2026-10-14 depois do retorno humano de que o rio/lago atual parece “ridículo e totalmente irreal” e de que um material novo não resolveu. Cores, medidas, perfis, intensidades e recomendações são **propostas para avaliar**. Este README documenta a entrega; não é fonte de verdade. Comportamento do jogo: [`docs/SPEC.md`](../../../../docs/SPEC.md). **Nada aqui foi integrado** ao `CityWorld`, ao `Main`, à simulação, aos saves ou à documentação oficial.
>
> **A amostra B é GEOMETRIA CONCEITUAL, NÃO APROVADA PARA GAMEPLAY.** A bacia rebaixada, a margem em relevo e o trecho de rio existem **só na prévia isolada** desta pasta. Não mudam o mapa, a seed, a regra de água (`InWater`), a validade de prédios e ruas, a captação, a poluição, os saves nem nada da simulação.
>
> **Só aparência, nas duas amostras.** Faixa de margem, linha d'água irregular, raso, “profundidade”, reflexo, ondulação, riscos de correnteza, juncos e pedras **não representam** nível, profundidade, vazão, correnteza, poluição, captação, recurso, área navegável, recuo de obra nem vegetação da regra de qualidade ambiental. Nenhuma malha nova tem colisão, picking ou conector.
>
> **Amostra C (segunda rodada, 2026-10-14): proposta visual não integrada, revisão humana PENDENTE.** Feita depois da crítica independente de A/B. Só lago, sem rio; água visível até o círculo real do lago. Ver a seção [Amostra C](#amostra-c--segunda-rodada-proposta-não-integrada). As seções 1 a 9 abaixo descrevem A e B e ficam como histórico; a seção 10 (recomendação) foi revista nesta rodada.

## Amostra C — segunda rodada (proposta não integrada)

> **Revisão humana: PENDENTE.** Proposta da frente de assets (IA) para avaliação do coordenador e do responsável. **Não é integração**: nada mudou no `Main`, no `CityWorld`, na simulação, nos saves, na SPEC, na ARCHITECTURE, no AGENTS ou na documentação oficial. Medidas, alturas, cores e intensidades são **propostas, não decisões de produto**. As capturas servem para julgamento humano; nenhuma métrica de cor é usada aqui como prova estética.

![Referência com materiais integrados | A | B | C — seed 42, zoom 110, 10:00](Preview/Captures/c_compare4_z110_day.jpg)

> **Crítica independente e conferência do coordenador (2026-10-14):** Claude Haiku 5.5 importou e validou C em cópia própria e criticou o disco escuro, a linha da borda e as manchas marrons fragmentadas. Não executou GPU nessa primeira revisão. Seu achado de material antigo na referência **não se confirmou**: `Preview/ws_before.tscn` já usa `env_lake_water.tres` no `LakeSurface`, assim como o gerador A/B e o jogo. O relatório original foi preservado. A segunda conferência independente confirmou os materiais, validou os três casos e executou o renderizador em GPU: 50 capturas, sem afirmar qualidade estética ou desempenho da cidade. As comparações C agora usam uma referência parametrizada por seed, com os materiais integrados, identificada como **referência**, não como captura de partida real. Parecer e logs locais: `artifacts/water-study/c-recheck/`.
>
> **Não integrar C como solução concluída.** Ela troca o chão e abaixa a superfície aproximadamente **0,245 m em relação à água atual** (0,15 m abaixo da grama), além dos limites originais da IA-020. O responsável aprovou depois uma direção diferente: lago irregular com margens em relevo em cidades novas, preservando partidas salvas; a decisão está registrada na SPEC, commit `5ebd3f6`. C continua circular e não satisfaz essa direção. Aparência, alturas e cores desta amostra continuam propostas PENDENTES.
### C.1 O que a amostra C é

- **Só o lago.** Nenhum rio novo, nenhuma fonte, produção ou fluxo hidráulico. A ondulação é só da normal de sombreamento e usa o mesmo relógio decorativo `host_time_s` que o `CityWorld` já alimenta (para com a pausa), como na IA-020.
- **Água visível até a borda circular real.** A linha d'água é o próprio círculo de jogo (`LakeCentre`/`LakeRadius`, o mesmo de `InWater`). Ela é desenhada como um polígono de 192 vértices **sobre** o círculo; no meio de cada corda, a água fica de 3,8 mm (R = 28,6 m) a 5,3 mm (R = 40 m) para dentro. Dentro do círculo não há terra seca; fora dele continua o chão do jogo, com a grama real (`env_grass_soft`, sem cópia nem espelho). Não há faixa de terra dentro da água nem água fora do círculo.
- **Rebaixo só sob a água.** Na prévia, a caixa do chão do jogo é trocada pelo mesmo chão de 512 × 512 m com uma abertura poligonal no círculo. Na abertura entram um barranco vertical **no** círculo e uma bacia. Propostas desta amostra: superfície d'água 0,15 m abaixo da grama; fundo entre 0,04 e 0,72 m abaixo da água, sempre acima da “saia” marrom do mapa (y = −1); prateleira rasa de 0,8 a 6 m de largura junto à margem, com baías e lobos.
- **Água translúcida com reflexo suave.** O fundo é desenhado já “visto através da água”, com absorção por canal: raso claro e esverdeado, fundo escuro azul-esverdeado. A superfície soma o reflexo do céu do jogo (`#b9c9d5`) e da margem oposta. Ela usa uma única passada transparente pré-multiplicada, sem textura de tela nem de profundidade.
- **Parametrizada pelos dados reais.** O gerador `Source/ws_lake_c.gd` recebe só centro e raio; não tem constante de seed. Os dados de três seeds vêm de `new CitySimulation(seed).State`, conferidos pelas regras reais do jogo, e estão em `Source/ws_c_cases.json`:

  | Seed | Centro (x; z) | Raio | Papel |
  | --- | --- | ---: | --- |
  | 42 | (152,609; 85,983) | 28,577 m | **vitrine** versionada (mesmo contexto de A/B) |
  | 1412 | (151,856; 105,868) | 28,146 m | conferência: menor x da faixa medida, centro ~20 m adiante em z |
  | 2877 | (166,117; 109,252) | 39,933 m | conferência: lago quase máximo, centro deslocado em x e z |

  Nas seeds 1–5000, o centro varia em x [151,86; 175,83] e z [85,29; 109,26], e o raio em [28; 40). O validador cobre esse envelope inteiro com 15 lagos sintéticos, além dos 3 reais.

### C.2 O que muda em relação à crítica — e o que continua

| Problema apontado | O que C faz | O que continua |
| --- | --- | --- |
| Anel marrom uniforme (A) | Não há anel marrom contínuo; ainda há manchas marrons fragmentadas em trechos. A transição fica **debaixo d'água**: raso esverdeado de largura irregular, capim submerso em trechos e pouca areia. Acima da água há só um barranco de 0,15 m. Ele aparece apenas do lado oposto à câmera, com grama pendente irregular. | Do lado da câmera, a borda é uma linha circular nítida, com um leve escurecimento por dentro em alguns trechos. Os trechos rasos podem ler como manchas claras. |
| Água noturna como buraco (A/B) | À noite, o reflexo do céu fica em 90% do dia: no jogo, a cor do céu e a luz ambiente não mudam com a hora, só o sol enfraquece. Os postes aparecem na água como um risco vertical quebrado pela ondulação, sem atenuação pela transparência. | À noite, a água continua um pouco mais escura que a grama. O risco de luz é estilizado. A cena só tem 2 postes. |
| Objeto recortado sobre a grama (A/B) | A água fica **abaixo** do chão, o fundo aparece no raso, o barranco aparece do lado oposto e a margem oposta reflete. | O contorno continua um **círculo perfeito**, porque a regra de jogo é um círculo (ver as perguntas). |

### C.3 Capturas (1280 × 800, câmera, sol e postes do jogo)

Godot 4.7.2 mono, **GL Compatibility** (`opengl3`, OpenGL 3.3 NVIDIA 546.18), RTX 3050 Laptop, MSAA 4x, água com tempo decorativo fixo de 12 s. Os focos de A/B foram reexpressos em relação ao centro do lago de cada seed.

| Tomada | Zoom | Hora | Onde |
| --- | ---: | --- | --- |
| `z110_day` / `z110_night` | 110 | 10:00 / 21:00 | seeds 42, 1412 e 2877 |
| `z45_day` / `z45_night` | 45 | 10:00 / 21:00 | seeds 42, 1412 e 2877 |
| `z45_limit_{farm,house}_day` | 45 | 10:00 | lotes válidos no limite (C.4), nas 3 seeds |
| `z110_dusk`, `z110_yaw225_day`, `z250_day` | 110 / 110 / 250 | 17:00 / 10:00 / 10:00 | seed 42, só C |

![Referência com materiais integrados | A | B | C — seed 42, zoom 110, 21:00](Preview/Captures/c_compare4_z110_night.jpg)
![Referência com materiais integrados | A | B | C — seed 42, zoom 45, 10:00](Preview/Captures/c_compare4_z45_day.jpg)
![Referência com materiais integrados | A | B | C — seed 42, zoom 45, 21:00](Preview/Captures/c_compare4_z45_night.jpg)
![C nas seeds 42, 1412 e 2877 — zoom 110, 10:00](Preview/Captures/c_seeds_z110_day.jpg)
![C nas seeds 42, 1412 e 2877 — zoom 110, 21:00](Preview/Captures/c_seeds_z110_night.jpg)
![Nível da água: 0 m, 0,15 m e 0,40 m abaixo do chão — seed 42, zoom 45](Preview/Captures/c_drop_compare_z45_day.jpg)

Na comparação de nível, 0 m e 0,15 m quase não se distinguem no zoom 45. Com 0,40 m, o barranco do lado oposto fica visível. O que muda a leitura é a água translúcida sobre o fundo, e ela exige a abertura no chão nos três casos.

As capturas e logs originais do autor permanecem em `artifacts/water-study/c/final/`. A regeneração do coordenador com a referência identificada e o validador exigindo todas as cenas declaradas fica em `artifacts/water-study/c-fixes/`: Godot renderizado GL Compatibility e 14 comportamentos/468 asserções sem falhas. O coordenador não repetiu o benchmark de três rodadas; os números de C.5 são a medição anterior do autor.

### C.4 Lotes válidos que entram na água (condição que já existe hoje)

A regra atual (`QuoteBuilding`) testa só o centro e os 4 cantos do lote contra o círculo. Por isso, a aresta de um lote **válido** pode entrar no círculo. Com a água desenhada até o círculo, isso aparece, como já aparecia com o disco atual. A ferramenta de dados mediu, na grade do cursor (2 m, rotações de 90°), a maior invasão válida:

| Tipo (lote) | Seed 42 | Seed 1412 | Seed 2877 | Limite contínuo na seed 42 (R = 28,6) |
| --- | ---: | ---: | ---: | ---: |
| Casa (12 × 16 m) | 0,59 m | 0,29 m | 0,68 m | 1,14 m |
| Captação (14 × 16 m) | 0,97 m | 1,01 m | 0,19 m | 1,14 m |
| Esgoto (26 × 28 m) | 2,97 m | 3,29 m | 2,19 m | 3,66 m |
| Fazenda (36 × 40 m) | 7,97 m | 8,01 m | 5,19 m | 8,17 m |
| Rua reta válida | folga de +0,16 m | +0,47 m | +0,07 m | nunca entra |

Com o menor raio possível (R = 28 m), o limite contínuo da fazenda chega a 8,4 m.

![Fazenda válida no limite: referência com materiais integrados | C — seed 42](Preview/Captures/c_limit_farm_s42_z45_day.jpg)
![Casa válida no limite: referência com materiais integrados | C — seed 42](Preview/Captures/c_limit_house_s42_z45_day.jpg)

A amostra **não muda a regra**. Os exemplos ficam no nó oculto `LimitExamples` de cada cena e só aparecem nessas tomadas. A decisão está na pergunta 2 (C.7).

### C.5 Custo indicativo

Tempo de GPU do quadro inteiro (ms), medido só na seed 42: 1280 × 800, MSAA 4x, RTX 3050 Laptop. Método igual ao de A/B: aquecimento, 3 rodadas com a ordem girando e 150 quadros por medida. Média, com o intervalo entre rodadas entre parênteses. Os quatro foram medidos na mesma rodada; os números de antes, A e B desta rodada diferem um pouco dos da §6, que é de outra sessão.

| Vista | Antes | A | B | C |
| --- | ---: | ---: | ---: | ---: |
| Zoom 110, dia | 0,788 (0,773–0,797) | 0,782 (0,734–0,815) | 1,147 (1,112–1,165) | 0,962 (0,906–0,997) |
| Zoom 110, noite (2 postes) | 0,724 (0,713–0,738) | 0,815 (0,758–0,901) | 1,264 (1,243–1,280) | 1,020 (0,963–1,135) |
| Zoom 45 no lago | 0,858 (0,835–0,901) | 1,016 (0,994–1,027) | 1,290 (1,245–1,369) | 1,431 (1,405–1,454) |
| Zoom 35, água na tela toda | 1,078 (0,977–1,154) | 1,225 (1,183–1,252) | 1,281 (1,230–1,318) | 1,756 (1,737–1,766) |

- **C custa mais que A:** +0,17 ms no zoom 110 e até +0,68 ms com a água na tela toda.
- **De onde vem o custo:** cada pixel de lago passa pelo fundo opaco (ruídos) e pela superfície translúcida (ondulação, reflexo e o `light()` por luz).
- **Malhas da vitrine:** chão 224 vértices / 220 triângulos; fundo + barranco 4 417 / 8 256; água 4 033 / 7 872. Não foram otimizadas; a água e o fundo podem usar menos anéis no centro.
- **CPU de renderização:** igual nas quatro variantes, entre 0,34 e 0,51 ms.
- **Alcance:** é medição local indicativa de uma GPU, não FPS da cidade integrada.

### C.6 Limitações

- **Forma:** o contorno visível é o círculo de jogo. Do lado da câmera, a borda é uma linha nítida; o “recorte” diminui, mas não some.
- **Reflexo:** só o céu e a margem oposta. Casas, árvores e postes não se refletem como imagem; os postes aparecem só como risco de luz.
- **Luzes:** no GL Compatibility, cada malha recebe no máximo 8 luzes pontuais (`rendering/limits/opengl/max_lights_per_object`, valor padrão que o projeto do jogo não muda), e a vista inteira até 32. Numa cidade com muitos postes em volta de um lago grande, uma integração precisaria dividir a malha da água.
- **Transparência:** a água não grava profundidade. Nesta cena não há outro objeto transparente, então não foi testada a ordenação com fumaça, vidro ou interface 3D.
- **Sombra:** o barranco de 0,15 m some na resolução das sombras do jogo (sombra até 650 m).
- **Integração:** exige trocar a caixa do chão por uma malha com abertura, abaixar a água e acrescentar fundo e dois shaders. Isso vai além da IA-020 (“não alterar vértices, silhueta, … altura”) e pede decisão humana e registro na SPEC antes do código.
- **Contexto:** as árvores das seeds 1412 e 2877 são uma transcrição da regra do `CityWorld` (`Reset`/`Sync`); na seed 42 são as mesmas de A/B, para comparar lado a lado.
- **Avaliação:** uma GPU e um driver; sem Forward+/Mobile; sem build exportada; sem partida integrada; sem avaliação humana, inclusive de quem ainda não lê.
- **Seeds:** a seed 42 é **vitrine**. As cenas das seeds 1412 e 2877 são geradas pelo mesmo gerador fora do Git, para as capturas e o validador.

### C.7 Direção aprovada e questões históricas
**Decisão humana posterior à amostra:** em 2026-10-14 o responsável respondeu “Sim, lago irregular nas cidades novas”. A SPEC foi registrada antes de código de produção, commit `5ebd3f6`: contorno irregular, margens em relevo, água visível coincidente com a área de água usada pela validação; geografia e funcionamento dos saves antigos preservados. Não aprova C, regeneração de cidades existentes nem um rio adicional. O lago sozinho já cumpre “rio ou lago”. Cores, materiais e alturas devem ser avaliados nessa direção autorizada, com captação, validade e save/load compatíveis. A aparência continua pendente de avaliação humana.

As perguntas originais abaixo são preservadas como histórico da entrega do autor. Não constituem seis aprovações obrigatórias; o contorno circular da pergunta 1 foi superado pela direção irregular e parte das demais escolhas já está delimitada pela decisão registrada.

1. **Contorno:** o lago pode continuar um círculo, com a água visível até o círculo de jogo (como em C)? Ou deve ter contorno irregular, o que muda a regra de água (`InWater`), a validade de obras e ruas, a captação, a exclusão de árvores e a poluição?
2. **Lotes válidos sobre a água:** com a água desenhada até o círculo, lotes hoje válidos ficam parcialmente sobre ela (fazenda até ~8 m, casa ~0,6 m). A regra deve passar a testar o lote inteiro contra a água, o que é mudança de regra? Ou essa sobreposição é aceitável?
3. **Água abaixo do chão:** a direção “lago rebaixado” (água ~0,15 m abaixo da grama, barranco visível só do lado oposto à câmera) é aceitável para avaliar no jogo?
4. **Noite:** a água deve manter à noite o reflexo de um céu que no jogo não escurece, mais o risco de luz dos postes? Ou a água noturna deve ficar mais escura que a grama?
5. **Integração:** há interesse em integrar C, ou parte dela, sabendo que isso muda o chão e a altura da água além do que a IA-020 permite? Se sim, a decisão precisa ser registrada na SPEC antes do código.
6. **Rio e captação:** confirma-se lago sem rio na V1? A captação, que pode ficar a até 22 m da borda, deve ter alguma ligação visível com a água?

### C.8 Como regenerar a amostra C

O projeto de verificação de C fica fora do Git, em `artifacts/water-study/c/project/`. Ele tem as mesmas *junctions* e o mesmo cuidado de A/B. **Não apague a pasta com `Remove-Item -Recurse` enquanto houver junctions.** A ferramenta de dados (`artifacts/water-study/c/lakedata/`, .NET 8) compila as fontes reais de `src/IndexCities.Simulation` num executável à parte. Ela não abre saves e não compila nem altera o projeto do jogo.

```sh
# a partir de artifacts/water-study/c (Git Bash):
dotnet lakedata/bin/Release/net8.0/LakeData.dll cases --seeds=42,1412,2877 --out=lakedata/cases.json --commit=<sha>
dotnet lakedata/bin/Release/net8.0/LakeData.dll intrusion --seeds=42,1412,2877 --out=lakedata/intrusion.json
powershell -NoProfile -ExecutionPolicy Bypass -File harness.ps1 setup
./run_c.sh "$PWD/final" --perf     # importação, manifesto de A/B, build, validadores C e A/B, capturas, cópias do README
powershell -NoProfile -ExecutionPolicy Bypass -File harness.ps1 teardown
```

- `Source/ws_c_cases.json` é a versão enxuta de `lakedata/cases.json`, com a origem registrada. Uma integração deve ler `LakeCentre`/`LakeRadius` do estado do mapa, **não** deste arquivo.
- **Derivados:** `Materials/ws_c_*.tres`, `Meshes/C/` e `Preview/C/` vêm de `build_water_study_c.gd`.
- **Sem sincronia manual:** a profundidade e o raio vão por vértice da malha para os shaders, e não há constante duplicada entre shader e script.

## 1. Resposta curta

- **Diagnóstico visual proposto.** O jogo desenha um cilindro perfeito, 9,5 cm **acima** da grama, com um talude externo que acende como um disco convexo. A grama encosta direto na água, sem margem. A faixa clara acompanha o círculo e pode reforçar a leitura de piscina. O albedo da água recebe a iluminação difusa do material, com turquesa próximo da luminosidade da grama. A aparência continua insatisfatória segundo o responsável; a relação entre forma, margem e material é a hipótese visual avaliada aqui.
- **Amostra A** (mesma malha, mesmo raio, mesma altura e mesma silhueta; só material e adereços dentro do círculo). A borda real do disco e o talude ficam com a cor e a normal exatas da grama, e o “prato” some: a diferença medida na borda caiu de **65,1 para 8,9**. Entre a borda e a água entra uma margem natural irregular, com **linha d'água 0,9–5,4 m para dentro do círculo**, relevo falso de bacia pela luz do jogo, raso, profundo escuro e reflexo estilizado. Custo de GPU **+0,01 a +0,08 ms**. **Limite:** a forma continua presa ao círculo, não há rebaixo nem paralaxe de verdade, e a água visível fica menor que a área de jogo.
- **Amostra B** (conceitual). O chão é rebaixado 0,8 m, a margem tem relevo real e sombreamento real, e a linha d'água é **a mesma que A pinta** (desvio máximo de 0,005 m em 295 raios). Toda a margem do lago fica dentro do círculo de jogo, e um rio curto (~85 m fora do círculo) chega da borda leste do mapa. Custo **+0,23 a +0,32 ms**. Exige **decisões de geometria** (§8) antes de qualquer uso.
- **Recomendação** (§10, revista na segunda rodada). A e B não estão prontas. A amostra C é a proposta pendente desta rodada (seção Amostra C).

## 2. Diagnóstico do visual atual (medido na `main` 528d30d)

| Aspecto | Como está no jogo | Efeito na leitura |
| --- | --- | --- |
| **Silhueta** | `CylinderMesh` de 64 segmentos, raio R exato. Na seed 42, R = 28,577 m e centro (152,609; 85,983), recalculados com o xorshift64\* da simulação. A regra de jogo usa o mesmo círculo (`InWater`: distância < R). | Neste cenário, o círculo sem margem pode lembrar um objeto ou piscina. A irregularidade do material atual é de cor interna e não toca o contorno. |
| **Disco elevado** | Tampa em y = +0,035 e chão em −0,06: a água fica **9,5 cm acima da grama**. O talude R → R + 1 aparece até R + 0,63, com a normal inclinada 8,5° para fora. | A luz do jogo trata o lago como convexo: o lado do sol acende e o oposto escurece, ao contrário de uma depressão. Diferença média de cor dentro/fora da borda (zoom 45): **65,1** (soma RGB 0–765). |
| **Transição da margem** | Grama uniforme até a água; sem terra, areia, lama molhada nem declive. Árvores excluídas até R + 9 m. | Corte seco e anel de “gramado aparado” em volta. Não há onde a água “encontre” o terreno. |
| **Raso** | Faixa clara de largura constante (3,2 m), com uma linha mais clara de 0,6 m que segue o círculo. | Lê como borda/rejunte de piscina e reforça o círculo. |
| **Reflexo e luz** | Opaco, com albedo iluminado pelo modelo difuso do material e Fresnel de ~11% na câmera de 38°. Turquesa `#6bb3bd`, com valor parecido ao da grama. À noite continua turquesa. | Pode lembrar uma superfície pintada. A pouca separação de luminosidade entre água e margem dificulta a leitura de profundidade. |
| **Escala e paisagem** | Lago de 57–80 m de diâmetro (R 28–40), ruas de 10,5 m, casas de ~8,8 m, mapa de 512 m. Não há rio, embora a SPEC fale em “rio/lago”. | Uma lagoa redonda isolada num gramado vazio não forma paisagem. *(Tamanho e quantidade são parâmetros de jogo, fora deste estudo.)* |

**Causa raiz:** a água é uma primitiva sobre o chão. Material só recolore. Além disso, o círculo também é a regra de jogo, então mudar o contorno visível exige decidir a relação entre água visível e área de jogo.

## 3. Amostra A — mesma malha, só material (+ adereços dentro do círculo)

**Igual ao jogo, conferido pelo validador:**

- mesmo `CylinderMesh` (R, R + 1, 0,15, 64 segmentos, 4 anéis);
- mesma posição (centro, y = −0,04), rotação, escala e sombra;
- mesmo chão (`BoxMesh` 512 × 1 × 512 com `env_grass_soft`).

Muda o `material_override` do lago (`Materials/ws_water_a.tres`). Opcionalmente entram duas `MultiMeshInstance3D` decorativas (14 moitas de junco e 20 pedras), todas **dentro do círculo** (no máximo a R − 0,4 m).

**Como o shader `ws_water_a` funciona** (tudo no fragmento, sem deslocar vértice):

1. **Borda invisível:** a borda real do disco e o talude lateral recebem a cor do chão, por um espelho exato de `env_grass_soft`, com normal para cima e a mesma rugosidade e especular. No chão longe do lago, A e antes dão diferença média **0,0**.
2. **Linha d'água irregular dentro do círculo:** a largura da margem vem de ruído no espaço do mundo (baías de 38 m, lobos de 14 m, recorte de 4,6 m) e varia de **0,9 a 5,4 m**. A água visível nunca passa da borda de jogo.
3. **Margem natural:** a grama desce em línguas, mais escura e úmida perto da água; depois vêm terra, areia em trechos, pedrisco fino e uma faixa de lama molhada na linha d'água.
4. **Relevo falso de bacia:** a normal da margem inclina para dentro, então o sol do jogo ilumina a margem como depressão. O declive começa numa linha irregular, e não no círculo, para a faixa iluminada não redesenhar o contorno.
5. **Água:**
   - fundo visível no raso, escurecendo para um profundo azul-esverdeado;
   - reflexo estilizado do céu do jogo (Fresnel) e faixa escura com o reflexo da margem do lado oposto à câmera (acompanha a rotação Q/E);
   - linha de contato clara e fina, só de perto;
   - ondulação e rajadas lentas só na normal;
   - brilho especular do sol e dos postes à noite.
6. **Tempo:** usa o mesmo parâmetro `host_time_s` que o `CityWorld` já alimenta, com frequências que fecham em 3600 s. A ondulação continua decorativa, com cadência uniforme e parada na pausa, como na IA-020.

**O que A não resolve:**

- o contorno total continua um círculo deformado por dentro;
- não há rebaixo, oclusão nem paralaxe: de perto, a “bacia” é pintada;
- o disco segue 9,5 cm acima do chão (invisível, mas presente);
- a água visível é menor que a área de jogo (§9).

![A: só material (esq.) e com juncos/pedras (dir.), zoom 45](Preview/Captures/a_material_only_vs_props_z45.jpg)

## 4. Amostra B — geometria conceitual, NÃO APROVADA PARA GAMEPLAY

Só na cena `Preview/ws_sample_b.tscn`. O gerador é `Source/ws_shore_geometry.gd`, determinístico.

**Chão.** Só na prévia, um terreno gerado substitui o `BoxMesh`:

- retalho fino de 1 m (x 120–256, z 24–120) costurado a uma grade de 8 m no resto do mapa;
- as 46 células da costura viram leques, sem junção em T e sem fatias finas;
- faces laterais iguais às do chão do jogo.

No chão plano, o shader `ws_terrain_b` produz exatamente a grama do jogo: longe da bacia, B e antes dão diferença média **0,0**.

**Lago.**

- Nível d'água 0,8 m abaixo do chão. A margem desce do **próprio círculo de jogo** (inclinação zero no topo, sem vinco) até a linha d'água e cruza o nível com ângulo, sem faixa plana rente à superfície.
- Barrancos curtos em alguns trechos do contorno. O fundo desce até 2,2 m (só para a cor do raso).
- A linha d'água é a mesma de A. **Toda a margem e toda a água do lago ficam dentro do círculo de jogo.**

**Rio (fora da simulação atual).** Trecho de ~85 m entre o círculo do lago e o limite leste do mapa:

- cerca de 8–16 m de largura, mais largo na foz, com margens de 3,2–4,8 m;
- termina num corte vertical de água na borda do mapa, no estilo maquete do próprio chão;
- água mais turva e riscos discretos de correnteza (fase dupla, período de 12 s; decorativo).

**Água.** Malha plana no nível d'água, que entra sob a margem; a linha d'água nasce da interseção com o relevo. O gerador grava por vértice a profundidade do fundo, a distância até a margem, a direção da margem e a correnteza, então não há textura de profundidade nem passe transparente.

**Adereços.** 35 moitas de junco e 23 pedras (as mesmas do lago de A, mais as do rio). A sombra real da margem aparece pouco: com a câmera a ~538 m e sombras de até 650 m, a resolução do mapa de sombras não registra degraus de 0,8 m (§9).

## 5. Capturas (mesma câmera, luz e cena; antes | A | B)

Godot 4.7.2 mono, **GL Compatibility** (`opengl3`; OpenGL 3.3.0 NVIDIA 546.18), RTX 3050 Laptop. Viewport de 1280 × 800 (a janela padrão do jogo), MSAA 4x. Câmera do `Main.UpdateCamera` (ortogonal, `size` = zoom, foco + (300, 330, 300) girado pelo yaw). Sol do `Main._Process` (cor `fff0d0`, sombra até 650 m) e ambiente do `Main` (`b9c9d5` / `afc5ce` × 0,32). À noite, postes acesos como no `CityWorld` (Omni de 17 m, energia 1,3). Água com tempo decorativo fixo de 12 s.

| Tomada | Zoom | Foco (x, z) | Yaw | Hora (energia; inclinação do sol) |
| --- | ---: | --- | ---: | --- |
| `z45_day` / `z45_night` | 45 | (163, 66), margem nordeste e foz | 45 | 10:00 (0,882; −54,0°) / 21:00 (0,12; −15°) |
| `z110_day` / `z110_night` / `z110_dusk` | 110 | (158, 82) | 45 | 10:00 / 21:00 / 17:00 (0,348; −26,6°) |
| `z250_day` / `z250_night` | 250 | (152,61; 85,98), centro do lago | 45 | 10:00 / 21:00 |
| `z110_yaw225_day` | 110 | (158, 82) | 225 | 10:00 (câmera do outro lado) |
| `z45_shore_day` | 45 | (125, 90), captação e margem oeste | 45 | 10:00 |

Nesta pasta, em JPEG leve:

![Zoom 45, 10:00](Preview/Captures/compare_z45_day.jpg)
![Zoom 250, 10:00](Preview/Captures/compare_z250_day.jpg)
![Zoom 110, 21:00](Preview/Captures/compare_z110_night.jpg)

**Conjunto completo** em PNG (local e ignorado pelo Git), em `artifacts/verification/water-study/final/`:

- `shots/`: 29 quadros de 1280 × 800;
- `sheets/`: 9 folhas antes | A | B, 3 escadas de zoom (dia e noite) por variante e 2 pares de A só material × com adereços;
- `render_report.json` e `perf_report.json`.

As rodadas de calibragem (`round1`–`round3`) também ficaram lá, como histórico.

**Medidas do relatório:**

- **Movimento em 0,6 s** (soma RGB média no quadro, zoom 45): antes 1,76, A 1,18, B 1,55. Ondulação perceptível só de perto, sem cintilar.
- **Contorno do disco:** antes 65,1, A 8,9. Em B, 17,3: a borda já é terreno real e a diferença vem do início do declive iluminado.

## 6. Malhas, passadas de material e custo

Contagem real lida das cenas salvas (`validate_report.json`). A parte comparada é o chão + água + adereços. O contexto (rua, postes, casas, captação, 133 árvores, saia e bordas) é **idêntico** nas três cenas: 153 malhas, 136 311 vértices, 47 222 triângulos e 471 itens de desenho.

| Parte comparada | Antes | A | B (conceitual) |
| --- | --- | --- | --- |
| Malhas / itens de desenho | 2 / 2 | 4 / 4 | 5 / 5 |
| Vértices | 546 | 2 978 | 25 912 |
| Triângulos | 780 | 2 104 | 44 152 |
| Materiais × passadas | 2 × 1 (opacos) | 4 × 1 (opacos) | 5 × 1 (opacos) |
| Lago | cilindro 522 v / 768 t | **o mesmo** cilindro, 522 v / 768 t | água 3 444 v / 6 464 t + corte do rio 40 v / 20 t |
| Chão | caixa 24 v / 12 t | caixa 24 v / 12 t | 17 968 v / 34 898 t (retalho 1 m: 13 289 v / 26 112 t; resto em 8 m) |
| Adereços (MultiMesh) | — | 14 juncos (88 v / 66 t cada) + 20 pedras (60 v / 20 t) | 35 juncos + 23 pedras |

**Custo de GPU** do quadro inteiro (ms). 1280 × 800, MSAA 4x, RTX 3050 Laptop. Aquecimento de 300 quadros, 3 rodadas com a ordem das variantes girando, 150 quadros por medida. Média, com o intervalo entre rodadas entre parênteses:

| Vista | Antes | A | B |
| --- | ---: | ---: | ---: |
| Zoom 110 (jogo) | 0,721 (0,699–0,742) | 0,727 (0,722–0,730) | 1,038 (1,009–1,071) |
| Zoom 45 no lago | 0,795 (0,790–0,802) | 0,869 (0,854–0,886) | 1,058 (1,051–1,069) |
| Zoom 35, água na tela toda | 0,913 (0,905–0,919) | 0,997 (0,965–1,052) | 1,140 (1,124–1,162) |

- A CPU de renderização ficou entre +0,03 e +0,04 ms em A e entre +0,04 e +0,08 ms em B.
- Os shaders têm ramos para não pagar o que não aparece: água aberta não calcula grama, e chão plano em B só calcula a grama. As imagens antes e depois da otimização são idênticas (diferença máxima de 1 em 765).
- Em B, boa parte do extra vem dos triângulos pequenos do retalho de 1 m. Numa integração, a malha pode ser mais econômica (§9).
- É uma comparação local de custo de renderização nesta GPU; não mede FPS nem desempenho da cidade integrada.

## 7. Restrições respeitadas e verificações executadas

- **Unidades e eixos:** Y para cima, metros, padrões no XZ do mundo.
- **Sem herança:** nenhum código, textura, modelo ou arquivo de projeto antigo ou externo. Sem textura nenhuma: o ruído é calculado.
- **Sem gameplay:** sem colisão, física, script, conector ou marcador próprio nas cenas, e sem luzes além do sol e de 2 postes.
- **GL Compatibility, uma passada opaca:**
  - não usa textura de tela ou profundidade, `ALPHA`, `EMISSION`, `discard`, `light()`, uniform global ou de instância;
  - não usa `render_mode`, exceto `cull_disabled` nos juncos;
  - nenhum `vertex()` altera `VERTEX` ou `NORMAL`.
- **Derivadas:** `fwidth` só é chamado em fluxo uniforme. Detalhes menores que ~3 px somem, sem cintilar.
- **Tempo:** compatível com o `host_time_s` do `CityWorld`, com fechamento em 3600 s.
- **Validador headless** (`Source/validate_water_study.gd`): **1 593 verificações, 0 falhas.** Ele confere:
  - análise dos shaders pela Godot, com os uniforms esperados;
  - malha de A idêntica à do `CityWorld` e, no texto do `CityWorld.cs` atual, o cilindro R/R+1/0,15/64 em y −0,04, o chão em caixa e os dois materiais integrados;
  - B marcada como conceitual;
  - adereços de A dentro do círculo;
  - B determinística (duas gerações idênticas);
  - linha d'água de B igual à de A;
  - margem entre 0,9 e 5,4 m;
  - hash com média 0,50, desvio 0,288 e correlação vizinha ≤ 0,016 em 6 sais;
  - materiais `.tres` que só apontam o shader;
  - pasta sem imagens importáveis fora de `Preview/Captures`;
  - este README.
- **Importação** headless no projeto de verificação: exit 0, sem erros nem avisos. Captura renderizada: exit 0, sem `SHADER ERROR`.
- **Isolamento:**
  - nenhum arquivo fora desta pasta foi alterado (`git status` limpo fora de `Assets/WaterStudy/`);
  - o projeto real não foi aberto nem importado, e os `.import` de `Assets/V1` seguem intactos;
  - não foi usado save pessoal.
- **Janela:** as capturas rodaram sem roubar foco, numa janela de 1 × 1 px sem borda e com `no_focus`, renderizando num `SubViewport` de 1280 × 800.

## 8. O que depende de uma decisão de geometria aprovada

Nada abaixo foi decidido. Esta é a lista do que precisa de decisão de geometria (humana ou do coordenador, conforme a SPEC/AGENTS) **antes** de B — ou parte de A além do material — ir para o jogo:

1. **Água visível × área de jogo.** Em A e em B (lago), a água visível fica 0,9–5,4 m para dentro do círculo `InWater`, e a faixa de margem continua não construível. Decidir se isso é aceitável e se a mensagem “O edifício invade a água” deve falar em margem/lago. *(Decisão de UX; vale até para A, que vai além do escopo da IA-020: “faixa rasa e ondulação”.)*
2. **Chão rebaixado.** Substituir o `BoxMesh` do chão por terreno gerado com a bacia (ou um buraco com retalho) no `CityWorld.Reset`, mais o novo lago. Profundidade do rebaixo (0,8 m), largura e perfil da margem.
3. **Contorno de jogo.** Manter o círculo como regra, como B foi construído (só muda a renderização), ou adotar o contorno irregular. A segunda opção exige mudar:
   - `InWater` (obras e ruas);
   - a validade das posições;
   - a distância da captação (|d − R| ≤ 22 m);
   - o ponto de amostra da poluição;
   - a exclusão de árvores (R + 9);
   - a geração por seed.
4. **Rio.** Existir ou não na V1, de onde vem e onde termina. Um rio que atravessa o mapa **corta ruas sem pontes, adiadas na V1**. Também afeta a captação ao longo do rio, a poluição, a geração e a reprodutibilidade por seed (versão do gerador nos saves).
5. **Borda do mapa.** Como o rio sai do mapa (corte de maquete, como na prévia) e a linha de limite passando sobre a água.
6. **Forma determinística.** A margem depende só da posição no mundo e dos parâmetros. Mudar constantes muda o visual de todas as cidades existentes (não muda o jogo se o círculo continuar sendo a regra).
7. **Adereços decorativos.** Se juncos e pedras podem existir, e se juncos contam ou não como vegetação na regra de qualidade ambiental (5C). Hoje são só aparência.
8. **Escala do lago.** Raio de 28–40 m e quantidade de corpos d'água são parâmetros de jogo, fora deste estudo; listados porque a escala também pesa no “irreal”.

## 9. Limitações explícitas

- **Avaliação:**
  - uma GPU (RTX 3050 Laptop) e um driver;
  - sem Forward+/Mobile, sem build exportada, sem hardware fraco;
  - sem teste no jogo integrado (HUD, prévias, dia e noite contínuos);
  - sem avaliação humana, inclusive de quem ainda não lê.
- **Escopo da prévia:** uma seed (42) e uma cena de vitrine; não é partida.
- **A:**
  - a forma é limitada pelo círculo, então o lago continua “arredondado”;
  - a bacia é pintada (sem oclusão nem paralaxe);
  - um prédio encostado no limite legal do círculo fica 0,9–5,4 m longe da água visível; a captação desta cena, 4 m fora do círculo, fica a ~5–9 m (ver `z45_shore_day`);
  - o disco segue 9,5 cm acima do chão, invisível na prática; a sombra do cilindro não aparece nas tomadas.
- **B:**
  - geometria de prévia, não otimizada para integração: retalho de 1 m em todo o retângulo da bacia e do rio;
  - o rio tem caminho fixo e só desenha uma direção;
  - a sombra de margens de 0,8 m se perde na resolução das sombras do jogo;
  - a água só fica plana porque nível e rio estão na mesma cota.
- **Noite:** só os 2 postes da cena.
- **Medidas:** custo e movimento são medições locais indicativas.
- **Referências do coordenador avaliadas e não usadas:**
  - `ReflectionProbe` (até 2 por malha no Compatibility): na câmera de 38°, o reflexo real de margens baixas é estreito, e a faixa de reflexo estilizada entrega a pista principal sem sonda nem atualização.
  - Textura de tela ou profundidade: exigiria o passe transparente e a reconstrução de profundidade do OpenGL. A já conhece o círculo, e B grava a profundidade por vértice.
- **Correção de dados:** o estudo anterior (RecoveryEnvironment) registrou o lago da seed 42 como (152,61; 86,04) com R = 28,38. Recalculando com o código C# da simulação, o valor é (152,609; 85,983) com R = 28,577. Este estudo usa o valor recalculado.

## 10. Recomendação curta (revista na segunda rodada, 2026-10-14)

O visual atual falha porque a água é um **disco opaco colocado acima do chão**. Material novo só recolore esse disco.

- **A e B não estão prontas e não devem ser integradas.**
  - **A** pinta lama e margem **dentro** do círculo de jogo. Isso cria um anel marrom uniforme e terra visível onde é proibido construir.
  - **B** muda a geografia: rio, borda do mapa e bacia com margem dentro do círculo. Isso está fora da IA-020, e a geometria está presa à seed 42.
  - As recomendações anteriores desta seção (“A como paliativo”, “B como menor passo”) ficam **retiradas**.
- **A amostra C** é uma proposta para avaliar, **não integrada** e com revisão humana PENDENTE. Ela mostra água visível até o círculo real, rebaixo só sob a água, reflexo suave e nenhum rio. O que ela resolve e o que não resolve está em C.2 e C.6.
- **A seed 42 é vitrine.** As cenas desta pasta existem para comparar. Posições, medidas e alturas não são regra nem decisão.
- **Qualquer integração**, desta ou de outra amostra:
  - deve **parametrizar os dados do mapa**: ler `LakeCentre`/`LakeRadius` do estado da cidade e gerar a geometria por cidade, sem constantes de seed e sem ler arquivos desta pasta;
  - deve **preservar as condições válidas**: o que hoje é posição válida ou inválida para obras e ruas (`InWater`, `QuoteBuilding`, `QuoteRoad`), o alcance da captação (|d − R| ≤ 22 m), a exclusão de árvores (R + 9 m), a amostra de poluição e o save/load;
  - deve passar antes pela decisão humana e pelo registro na SPEC, porque a IA-020 não cobre troca do chão nem da altura da água.

## 11. Como regenerar, validar e capturar

O projeto de verificação fica fora do Git (`artifacts/verification/water-study/project/`). Ele tem um `project.godot` próprio (GL Compatibility, janela de 1 × 1 px sem foco) e *junctions* para `Assets/WaterStudy`, `Assets/V1/{Scenes,Meshes,Materials}` e `Assets/RecoveryEnvironment/{Materials,Shaders}`. Assim, `res://` resolve igual ao jogo, e o projeto real não é importado nem alterado.

As *junctions* não ficam criadas entre usos: `harness.ps1 setup` cria e `harness.ps1 teardown` remove só os links. **Não apague essa pasta com `Remove-Item -Recurse` enquanto houver junctions:** no Windows PowerShell 5.1 isso pode apagar o conteúdo das pastas reais.

```sh
# a partir de artifacts/verification/water-study (Git Bash):
powershell -NoProfile -ExecutionPolicy Bypass -File harness.ps1 setup
./run_all.sh "$PWD/final" --readme-captures     # build + validate (headless) + capturas
godot --path project --rendering-driver opengl3 --script res://Assets/WaterStudy/Source/render_water_study.gd -- --out="$PWD/final" --perf-only
# isolados:
godot --headless --path project --script res://Assets/WaterStudy/Source/build_water_study.gd -- --report=<json>
godot --headless --path project --script res://Assets/WaterStudy/Source/validate_water_study.gd -- --report=<json> --cityworld=<repo>/src/IndexCities.Godot/CityWorld.cs
powershell -NoProfile -ExecutionPolicy Bypass -File harness.ps1 teardown
```

- **Amostra C:** comandos próprios na seção C.8 (projeto de verificação separado em `artifacts/water-study/c/`).
- **Derivados:** `Materials/`, `Meshes/` e `Preview/*.tscn` vêm do gerador. Para mudar algo, edite `Shaders/` ou `Source/` e rode o build de novo.
- **Sincronia:** os parâmetros da linha d'água são **iguais** nos shaders e em `ws_shore_geometry.gd` (`BANK`, `GRASS_REACH`).
- **Logs:** `artifacts/verification/water-study/logs/` (importação, build, validação, captura e desempenho de cada rodada).
- **Exportação:** sugere-se excluir `Assets/WaterStudy/` inteira do preset. É estudo.

## 12. Arquivos

| Caminho | O que é | Tamanho |
| --- | --- | ---: |
| `Shaders/ws_common.gdshaderinc` | Hash e ruído próprios, espelho da grama, largura da margem, ondulação, rajadas, céu | 8,0 KB |
| `Shaders/ws_water_a.gdshader` | Amostra A: água + margem pintadas na malha atual | 12,0 KB |
| `Shaders/ws_terrain_b.gdshader` / `ws_water_b.gdshader` | Amostra B: chão com bacia / água da bacia e do rio | 5,0 / 6,7 KB |
| `Shaders/ws_reeds.gdshader` | Juncos (duas faces com a mesma normal) | 0,9 KB |
| `Materials/*.tres` | 4 `ShaderMaterial` que só apontam o shader; pedra e corte do rio em `StandardMaterial3D` | < 1 KB cada |
| `Meshes/ws_b_*.res`, `ws_reed_clump.res`, `ws_rock.res` | Malhas geradas (compactadas) | 213 + 83 + 4 KB |
| `Preview/ws_before.tscn`, `ws_sample_a.tscn`, `ws_sample_b.tscn` | Cenas de vitrine (antes / A / B), sem script | 36–46 KB |
| `Preview/Captures/*.jpg` | 5 folhas para este README (`.gdignore`) | 450 KB |
| `Source/ws_shore_geometry.gd` | Gerador de B, espelho do ruído, adereços | 27 KB |
| `Source/build_water_study.gd` / `validate_water_study.gd` / `render_water_study.gd` | Gerador de recursos e cenas / validador / capturas e custo | 16 / 16 / 16 KB |
| **Amostra C** | | |
| `Source/ws_lake_c.gd` | Gerador do lago C (chão com abertura, barranco + fundo, água) a partir de centro e raio | 14,4 KB |
| `Source/ws_c_cases.json` | Lagos reais (seeds 42, 1412 e 2877), vitrine, limites e árvores, com a origem | 39,2 KB |
| `Source/build_water_study_c.gd` / `validate_water_study_c.gd` / `render_water_study_c.gd` | Gerador de materiais, malhas e cenas / validador / capturas, folhas e custo | 15,9 / 20,8 / 15,7 KB |
| `Shaders/ws_water_c.gdshader` / `ws_lakebed_c.gdshader` | Superfície translúcida (reflexo, risco de luz, ondulação) / fundo e barranco vistos através da água | 6,1 / 5,5 KB |
| `Materials/ws_c_water.tres`, `ws_c_lakebed.tres` | `ShaderMaterial` que só apontam o shader | < 1 KB cada |
| `Meshes/C/ws_c_s42_{ground,bed,water}.res` | Malhas da vitrine da seed 42 (compactadas) | 2,5 + 133,3 + 93,7 KB |
| `Preview/C/ws_sample_c_s42.tscn` | Cena da amostra C na seed 42, sem script; exemplos de limite ocultos | 40,8 KB |
| `Preview/Captures/c_*.jpg` | 9 folhas da amostra C para este README (`.gdignore`); as de A/B não foram tocadas | 635 KB |

## 13. Origem, autoria e execução

- **Autoria:** shaders, gerador, validador e capturas são originais, escritos para este pedido em 2026-10-14.
- **Técnicas genéricas implementadas do zero:**
  - ruído de valor com hash inteiro de constantes próprias;
  - soma de ondas senoidais com frentes deformadas;
  - Fresnel estilizado;
  - fluxo em duas fases cruzadas;
  - perfis de Hermite e grade costurada por leques.
- **Espelho da grama:** a função de `RecoveryEnvironment/Shaders/env_grass_soft.gdshader`, integrada neste projeto, foi reproduzida para a borda ficar invisível. Nenhum arquivo daquela pasta foi alterado; ela só é lida e instanciada no “antes”.
- **Contexto da cena:** peças de `Assets/V1` instanciadas como contexto, sem cópia nem alteração.
- **Nada externo:** nenhum código, textura, modelo ou arquivo de projeto anterior ou da internet foi consultado ou copiado.
- **Modelo efetivo:** Claude Opus 5.5 (`claude-opus-5-5`) no Claude Code, com esforço de raciocínio **max** configurado na sessão. “Ultracode” não é uma opção selecionável neste runtime e não foi usado como recurso. Sem subagentes e sem workflows.
- **Coordenação:** papel de especialista de assets. A integração, a revisão e as escolhas de produto ficam com o coordenador (Codex).
- **Amostra C (segunda rodada):**
  - shaders, gerador, validador, capturas e a ferramenta de dados são originais, escritos para este pedido em 2026-10-14;
  - técnicas genéricas: absorção por canal, Fresnel estilizado, raio refletido contra o círculo e brilho alongado na tela;
  - nada foi copiado de projeto anterior ou externo; a única transcrição é a regra de árvores do próprio `CityWorld` (`Reset`/`Sync`), usada só como contexto das seeds 1412 e 2877;
  - os arquivos de A/B não foram alterados; o validador de C confere o hash de cada um contra o `HEAD`, e o validador de A/B continua passando;
  - modelo efetivo: Claude Opus 5.5 (`claude-opus-5-5`) no Claude Code, com `--effort max`, conferido na linha de comando do processo; sem subagentes e sem workflows.
