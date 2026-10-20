# Oficina mecânica — `commercial.mechanic_shop.a`

Asset 3D original de IndexCities, pronto para integração. **Somente apresentação**: nenhum nó cria preço, capacidade, vaga funcional, atendimento ou emprego — isso é da SPEC ([Quebras, guincho local e oficina](../../../../docs/spec/transporte-circulacao.md)). Estética e medidas são propostas da frente de assets, com revisão humana pendente.

**Abrir no Godot:** `Scenes/commercial.mechanic_shop.a.tscn`. **Intercâmbio:** `Scenes/commercial.mechanic_shop.a.glb` (inclui as animações). Receita editável: `Source/build_workshop.gd` (geometria em `Source/geometry.gd`). Não há geração de malhas durante a partida.

## Footprint proposto

- **Lote 22 × 19 m** (X × Z), pivô no centro do lote, no solo; Y para cima, 1 unidade = 1 metro, fachada em −Z. Antes da integração o candidato era 22 × 17 m; os 2 m a mais são o pátio da frente, para o guincho de plataforma (7,2 × 2,9 m) estacionar sem bloquear a porta da recepção nem as baias.
- Construção (galpão + recepção): x ∈ [−10,15; 9,70], z ∈ [−0,30; 8,80]. Pátio da frente: z ∈ [−8,45; −0,30] mais o passeio de 1 m na borda −Z. Medidas propostas, não aprovam lote nem escala de gameplay.
- Orçamento: 12 918 triângulos (incluindo o letreiro), 11 malhas, 5 materiais, atlas de cores de 32 × 32 px. Era 37 094 no candidato; quinas chanfradas só onde aparecem.

## Contrato de integração

Todas as âncoras são `Marker3D` (no GLB, `Node3D` com o mesmo nome). O **−Z local** de cada uma aponta para onde o veículo/SIM olha ou para fora da abertura. Âncora só dá posição e heading: não concede acesso, vaga, capacidade nem atendimento.

| Caminho | Posição (m, referencial do lote) | Heading (−Z local) | Uso |
| --- | --- | --- | --- |
| `LiftPlatform/ServiceBay0` | (−6,905; 0; 3,35) com o elevador embaixo | −Z (rua) | Carro REAL sobre o elevador. **Filho** de `LiftPlatform`: parentear o carro nele (`transform` local identidade) e ele sobe junto |
| `Anchors/ServiceBay1` | (−1,45; 0; 4,00) | −Z | Piso atrás do portão; cabe um carro ou o caminhão de carga (cabe com folga de 0,4 m à frente) |
| `Anchors/TowTruckBase` | (6,60; 0; −5,80) | −X (nariz para oeste) | Origem do guincho de plataforma próprio (`vehicle.tow_truck.a`). Cabe em repouso e com a plataforma inclinada, e sai para oeste pelo pátio |
| `Anchors/WaitingSpot0` / `WaitingSpot1` | (4,5; 0; −3,1) e (5,5; 0; −3,1) | +Z (de frente para a oficina) | Onde um SIM espera o serviço, junto ao banco da recepção |
| `Anchors/Entrance` | (8,13; 0; −1,65) | −Z | Soleira da porta da recepção (pela fachada −Z, como os demais prédios) |
| `Anchors/BayThreshold0` / `BayThreshold1` | (−6,9; 0; −0,3) e (−1,45; 0; −0,3) | −Z | Soleiras das duas baias, na linha da fachada |
| `Anchors/ServiceDoor`, `Anchors/ActivitySign` | porta dos fundos; letreiro | +Z; −Z | Referências visuais |

Os veículos entram com a origem do veículo na âncora e o nariz para o heading da âncora (nariz para −Z, para a rua, nas duas baias). O veículo não vem embutido no asset.

### Nós animados

| Nó | `AnimationPlayer` | Animações | Efeito |
| --- | --- | --- | --- |
| `LiftPlatform` (`Node3D`; filhos `Carriage`, `ServiceBay0`, `AnimationPlayer`) | `LiftPlatform/AnimationPlayer` | `lift_up` (3,0 s), `lift_down` (3,0 s) | `position` do `LiftPlatform` sobe 1,4 m em Y (pose de repouso = embaixo). Carrinhos, 4 braços e 4 apoios sobem; as colunas (`LiftFrame`) ficam |
| `Gate` (`MeshInstance3D`, pivô no topo do portão) | `Gate/AnimationPlayer` | `gate_open` (1,6 s), `gate_close` (1,6 s) | `scale.y` vai de 1 a 0,05: a cortina recolhe para a caixa do rolo acima da abertura. Repouso = fechado |

Nenhuma animação tem autoplay. O jogo escolhe quando tocar; `seek(fim)` dá a pose final. Interpolação linear.

### Demais nós

`Site` (laje, passeio, canal), `Building`, `ArchitecturalDetails`, `WorkshopFurnishings` (bancada, carrinho de ferramentas, travessas), `LiftFrame` (colunas e travessa do elevador), `YardProps` (pneus, compressor, banco, jardineira), `SignHousing`, `WrenchEmblem`, `ReplaceableActivityLabel` (`TextMesh` genérico "OFICINA", substituível; no GLB vira geometria).

## Verificação

```powershell
$engine = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe'
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Workshop/Source/build_workshop.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Workshop/Source/validate_workshop.gd
scripts/PreviewWorkshop.ps1     # capturas em artifacts/verification/quebras-assets/
```

`validate_workshop.gd` confere: partes e âncoras, animações e seus valores finais, orçamento de triângulos, malhas e normais finitas, reimportação do GLB (mesmos triângulos, `LiftPlatform`/`Gate`/`Anchors`) e, com os veículos reais do jogo nas âncoras (carro no elevador embaixo e em cima com o capô aberto, carro e caminhão de carga na `ServiceBay1`, guincho na `TowTruckBase`), que ficam dentro do lote e sem atravessar o corpo da oficina. Relatório em `Preview/validation.json`. A preview (`Source/preview_workshop.gd`, GL Compatibility, janela real) grava `workshop_lift_down/up`, `gate_closed/open`, `tow_base`, `hero_all` e `top`.

## Limites

Colisão, navegação, acesso real, preços, recursos, funcionamento e estados de obra seguem a SPEC e a calibração autorizada. Elevador e portão têm só o movimento visual; iluminação, pessoas e atividade vêm da simulação. Sem LOD distante. Estética ainda sem aprovação humana. Autoria: receita original do candidato (GPT-6.1-Sol) evoluída nesta entrega.
