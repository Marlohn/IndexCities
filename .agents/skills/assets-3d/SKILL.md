---
name: assets-3d
description: Como criar, alterar e validar assets 3D do IndexCities (prédios, veículos, props) por receita GDScript no Godot, com convenções de escala, âncoras, animações e validação. Use ao produzir ou mexer em qualquer coisa em src/IndexCities.Godot/Assets.
---

# Assets 3D no IndexCities

Regras que valem sempre: `AGENTS.md` (seção 6, sem herança: todo asset é original) e `docs/ARCHITECTURE.md` seção 6 (asset é só apresentação, substituível sem mudar gameplay). Os pacotes existentes são o melhor exemplo: `Assets/V1`, `Assets/Construction`, `Assets/Workshop` e `Assets/Breakdown`, cada um com `README.md`.

## Asset é só apresentação

- Nenhum nó, medida ou âncora cria preço, capacidade, vaga, acesso, emprego ou regra. Isso vem da SPEC. Medidas e footprint são **propostas** até a SPEC ou o responsável aprovar; diga isso no README.
- Nada de pessoas ou veículos embutidos no prédio: o jogo coloca os agentes reais nas âncoras.
- A simulação não referencia caminhos de cena; a apresentação resolve o `VisualId` (ex.: `commercial.mechanic_shop.a`).

## Convenções

- Y para cima, 1 unidade = 1 metro, pivô no solo (y = 0), no centro do lote ou do corpo principal.
- Fachada e frente voltadas para **−Z**. Em âncoras (`Marker3D`), o −Z local é o heading: para onde o veículo ou o SIM olha, ou para fora da abertura.
- Nome do pacote = família do `VisualId` (`civic.`, `commercial.`, `residential.`, `vehicle.`, `prop.` …) + variante `.a`.
- Pasta do pacote: `Scenes/` (`.tscn` e `.glb`), `Meshes/`, `Materials/`, `Source/` (receita editável), `Preview/`, `README.md` e manifest quando houver.
- Malha gerada por **receita GDScript** (`Source/build_*.gd`), nunca durante a partida. Variante de cor = trocar material por superfície, sem regenerar a malha.
- Sem autoplay, scripts, luzes, colisão ou física no asset. Animações (`AnimationPlayer`) só com nomes claros (`lift_up`, `gate_open`, `hood_open`, `blink`), e o jogo decide quando tocar.
- Orçamento de triângulos coerente com o resto do pacote (veja os manifests e READMEs); chanfro só onde aparece na câmera do jogo.

## Contrato de integração

O README do pacote lista, numa tabela, cada nó, âncora e animação que o jogo usa: caminho, posição, heading e uso. É o que o integrador lê. Mudou um nome ou uma posição? Atualize o README e avise quem integra.

## Validação (obrigatória antes de entregar)

Nesta máquina, a engine de console fica em `C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe`.

```powershell
& $engine --headless --path src/IndexCities.Godot --script res://Assets/<Pacote>/Source/build_<x>.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/<Pacote>/Source/validate_<x>.gd
scripts/Preview<Pacote>.ps1   # quando existir (Workshop, Breakdown): capturas em janela real, em artifacts/verification/...
```

- O validador confere partes, âncoras, animações e valores finais, orçamento, normais finitas, reimportação do GLB e, quando há âncoras de veículo, o **veículo real do jogo** posicionado nelas sem atravessar o prédio.
- Olhe as capturas, inclusive da **câmera do jogo** (ortográfica, cerca de 38°, diagonais). Um carro escondido pela verga só aparece nessa vista.
- Se mexeu num asset já usado pelo jogo (ex.: veículos), rode também os validadores dos outros pacotes e os cenários de QA que usam esse asset (skill `qa-godot`).
- Não commite ruído de reimportação (`*.import` e `*.uid` alterados só pelo editor).
