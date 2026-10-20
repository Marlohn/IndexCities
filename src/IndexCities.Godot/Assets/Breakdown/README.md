# Quebras — props genéricos e ganchos nos veículos

Visual de quebra de veículo, **somente apresentação**. Nada aqui decide quando um veículo quebra, quem o atende ou quanto custa (SPEC: [Quebras, guincho local e oficina](../../../../docs/spec/transporte-circulacao.md)). A integração instancia, encaixa e remove os props a partir do estado real do veículo. Estética com revisão humana pendente; geometria original (receita em `Source/build_breakdown.gd`, kit de `Assets/V1/Source/kit`).

## Props (`Scenes/`)

| Cena | Nós | Custo | Uso |
| --- | --- | --- | --- |
| `prop.warning_triangle.a.tscn` | `Body` | 120 triângulos | Triângulo de sinalização de 0,64 m, duas faces iguais, pivô na base no solo. A integração põe atrás do veículo (+Z do veículo) e decide a distância |
| `prop.breakdown_smoke.a.tscn` | `Smoke` (`CPUParticles3D`) | 16 partículas de 20 triângulos, sem textura, material sem sombreamento | `emitting = true` ao entrar na árvore; `Smoke.emitting = false` para parar. Instancie com o `transform` já na origem `HoodSmoke` **antes** de `add_child` (as partículas são em coordenadas globais) |
| `prop.hazard_lights.a.tscn` | `LightFL`, `LightFR`, `LightRL`, `LightRR` (cada um com `Lens` e `Glow`), `AnimationPlayer` | 352 triângulos, sem `Light3D` | Quatro lentes âmbar. `AnimationPlayer` com `blink` (0,8 s em loop, **autoplay**): `Glow` (emissivo) fica visível 0,0–0,4 s e some 0,4–0,8 s; `Lens` âmbar apagada fica sempre |

**Encaixe do pisca:** ao instanciar `hazard_lights` (filho do veículo), copiar o transform de `HazardFL/FR/RL/RR` do veículo para `LightFL/FR/RL/RR`. O −Z local de cada `LightXX` é a direção em que a lente olha (frente −Z, traseira +Z), igual ao marcador. O `Light3D` não existe: o brilho é só material emissivo.

## Ganchos nos 9 veículos motorizados

Presentes em `vehicle.car_passenger.a`, `vehicle.truck_cargo.a` (V1) e `vehicle.truck_dump.a`, `truck_mixer`, `truck_flatbed`, `tow_truck`, `tow_truck_heavy`, `asphalt_paver`, `road_roller` (Construction):

| Nó | O que é |
| --- | --- |
| `Hood` (`MeshInstance3D`) | Capô/tampa com a **origem na dobradiça**. Fechado = `rotation` zero e o veículo fica idêntico ao anterior (mesmo envelope) |
| `AnimationPlayer/hood_open` | 0,9 s; um track `Hood:rotation`; o nó gira em X até o ângulo da tabela e fica aberto. Sem autoplay. Para fechar, `play_backwards` |
| `HazardFL`, `HazardFR`, `HazardRL`, `HazardRR` | `Marker3D` nos quatro cantos (−X = esquerda; frente −Z, traseira +Z; o −Z local aponta para fora). Centro da lente do pisca |
| `HoodSmoke` | `Marker3D` sobre o compartimento do motor (eixo +Y para cima) |

| Veículo | Dobradiça do `Hood` (m) | `hood_open` (graus, eixo X) | Descrição |
| --- | --- | --- | --- |
| `car_passenger` | (0; 0,90; −0,85) | +62 | Capô inteiro; levanta a frente |
| `truck_cargo`, `truck_dump`, `truck_mixer`, `truck_flatbed`, `tow_truck`, `tow_truck_heavy` | (0; 0,86; −3,55) (`tow_truck_heavy`: −4,015) | −100 | Cabine avançada: tampa frontal (grade) que abre para a frente, deixando à mostra o compartimento do motor |
| `asphalt_paver` | (0; 1,55; −0,75) | −60 | Tampa frontal da cobertura do motor, dobradiça à frente |
| `road_roller` | (0; 1,62; 1,54) | +60 | Tampa traseira do bloco do motor |

As posições exatas e os marcadores de cada cena estão em `Assets/V1/manifest.v1.json` e `Assets/Construction/manifest.construction.json` (`contract.breakdown`).

**Cor do carro:** o `Hood` do `car_passenger` tem superfície `car_paint` própria. Ao aplicar uma variante de cor com `set_surface_override_material` no `Body`, aplique a **mesma** variante no `Hood`.

## Verificação

```powershell
$engine = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe'
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Breakdown/Source/build_breakdown.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Breakdown/Source/validate_breakdown.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/V1/Source/build_v1_assets.gd -- --only=vehicle_
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Construction/Source/build_construction.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Construction/Source/verify_construction.gd -- --validate-only
scripts/PreviewBreakdown.ps1    # capturas em artifacts/verification/quebras-assets/
```

`verify_construction.gd` confere os ganchos nos 7 veículos de Construction (único `AnimationPlayer` permitido é o de `hood_open`, sem autoplay; marcadores espelhados; capô aberto dentro da faixa de 2,9 m). `validate_breakdown.gd` confere os props e os ganchos dos dois veículos V1. As capturas `breakdown_*.png` mostram capô aberto, triângulo, pisca (aceso e apagado) e fumaça em todos os 9 veículos (`breakdown_fleet.png`), no carro e no caminhão.
