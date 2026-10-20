---
name: assets-3d
description: Como criar, alterar e validar assets 3D do IndexCities (prédios, veículos, props) por receita GDScript no Godot, com convenções de escala, âncoras, animações e validação. Use ao produzir ou mexer em qualquer coisa em src/IndexCities.Godot/Assets.
---

# Assets 3D no IndexCities

Regras que valem sempre: `AGENTS.md` (seção 6, sem herança: todo asset é original) e `docs/ARCHITECTURE.md` seção 6 (asset é só apresentação, substituível sem mudar gameplay). Os pacotes existentes são o melhor exemplo, cada um com `README.md`: `Assets/V1` (catálogo, com manifest), `Assets/Construction` (veículos e máquinas de obra), `Assets/Workshop` (oficina, com GLB e âncoras) e `Assets/Breakdown` (props de quebra). Siga o padrão do pacote mais parecido com o seu.

## Asset é só apresentação

- Nenhum nó, medida ou âncora cria preço, capacidade, vaga, acesso, emprego ou regra. Isso vem da SPEC. Medidas e footprint são **propostas** até a SPEC ou o responsável aprovar; diga isso no README.
- Nada de pessoas ou veículos embutidos no prédio: o jogo coloca os agentes reais nas âncoras.
- A simulação não referencia caminhos de cena; a apresentação resolve o `VisualId` (ex.: `commercial.mechanic_shop.a`).

## Convenções

- Y para cima, 1 unidade = 1 metro, pivô no solo (y = 0), no centro do lote ou do corpo principal.
- Fachada e frente voltadas para **−Z**. Em âncoras (`Marker3D`), em geral o −Z local é o heading (para onde o veículo ou o SIM olha, ou para fora da abertura). Há exceções, como fonte de luz e saída de fumaça apontando em Y: o README do pacote diz o eixo de cada âncora.
- Nó raiz com os metadados que o jogo lê: `visual_id`, `asset_status` e envelope visual (`visual_envelope_min`/`visual_envelope_size`), como em `Assets/V1`.
- Nome do pacote = família do `VisualId` (`civic.`, `commercial.`, `residential.`, `vehicle.`, `prop.` …) + variante `.a`.
- Malha gerada por **receita GDScript** em `Source/`, nunca durante a partida. Variante de cor = trocar material por superfície, sem regenerar a malha.
- Sem scripts, luzes, colisão ou física no asset. Animações (`AnimationPlayer`) com nomes claros (`lift_up`, `gate_open`, `hood_open`); o jogo decide quando tocar. Autoplay só em efeito contínuo de prop que aparece já ativo (ex.: `blink` do pisca-alerta).
- Orçamento de triângulos coerente com o resto do pacote (veja os manifests e READMEs); chanfro só onde aparece na câmera do jogo.

## Contrato de integração

O README do pacote documenta o que o jogo usa: nós, âncoras e animações (na oficina, uma tabela com caminho, posição, heading e uso); no `Assets/V1`, o `manifest.v1.json` traz marcadores, envelopes e partes. Mudou um nome, um nó ou uma posição? Atualize o README, o manifest (se houver) e os metadados da raiz, e avise quem integra: o jogo procura esses nomes, e uma troca silenciosa quebra a apresentação.

## Validação (obrigatória antes de entregar)

Nesta máquina: `$engine = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe'`. Rode a partir da raiz do repositório.

```powershell
& $engine --headless --path src/IndexCities.Godot --script res://Assets/<Pacote>/Source/build_<x>.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/<Pacote>/Source/validate_<x>.gd   # ou verify_<x>.gd
scripts/Preview<Pacote>.ps1   # quando existir (Workshop, Breakdown): capturas em janela real, em artifacts/verification/...
```

- Rode o validador do pacote e, se ele não cobrir o que você mudou, estenda-o. Ele deve conferir pelo menos: partes e âncoras esperadas, animações e seus valores finais, orçamento e normais finitas. Quando o pacote exporta GLB (hoje, a oficina), confira também a reimportação. Quando há âncoras de veículo, posicione o **veículo real do jogo** nelas e verifique que ele não atravessa o prédio.
- Olhe as capturas, inclusive da **câmera do jogo** (ortográfica, cerca de 38°, diagonais). Um carro escondido pela verga só aparece nessa vista.
- Se mexeu num asset já usado pelo jogo (ex.: veículos), rode também os validadores dos outros pacotes e os cenários de QA que usam esse asset (skill `qa-godot`).
- Não commite ruído de reimportação (`*.import` e `*.uid` alterados só pelo editor).
