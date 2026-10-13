## Receita original: estacao de tratamento de esgoto (utility.sewage_treatment.a) — amostra V1,
## segunda onda, revisao humana PENDENTE. Somente visual: capacidade, vazao, emissao, operador,
## acessos e custo sao da simulacao/integracao. Medidas sao CANDIDATAS para compatibilizacao.
##
## Leitura pretendida ("esgoto", nunca "agua limpa"): raias de aeracao RETANGULARES longas com
## passarela e guarda-corpo ocre, decantador secundario circular com ponte radial (no movel
## separado), biodigestor cilindrico terroso com cupula conica, leitos de secagem de lodo rasos,
## casa de controle pequena com porta para -Z e tubulacao castanha ligando as etapas.
## Codigo de cor: ocre/castanho/oliva + concreto. NENHUM azul (o azul e da agua potavel).
##
## Estado real (o host liga): State_Water (efluente marrom-oliva + espuma de aeracao nas raias,
## no canal de chegada e no decantador) e State_Sludge (torta de lodo nos leitos). Sem estado,
## os tanques aparecem vazios (fundo concreto escuro): estrutura permanente fica no Body.
##
## Fluxo sugerido pela tubulacao (so leitura visual, nao e regra): chegada (-X) -> canal de
## chegada -> raias de aeracao -> decantador -> saida tratada (+X); lodo do decantador ->
## biodigestor -> leitos de secagem.
extends RefCounted

const FAMILY_ID := "utility.sewage_treatment.a"
const MATERIAL_PREFIX := "sewage_"
const MATERIALS := {
	# Acento ocre: guarda-corpos, corpo do biodigestor, testeiras da casa de controle.
	"sewage_ochre": {"albedo": "#b08a4a", "roughness": 0.85},
	# Castanho: tubulacao, cupula do biodigestor, telhado da casa de controle.
	"sewage_pipe": {"albedo": "#7a5a3a", "roughness": 0.7},
	# Efluente em processo (somente em State_Water).
	"sewage_effluent": {"albedo": "#7d7444", "roughness": 0.3},
	# Torta de lodo (somente em State_Sludge).
	"sewage_sludge": {"albedo": "#5b4a35", "roughness": 0.95},
}

## Lote logico do host (X = largura, Z = profundidade); pivo no centro, fachada -Z.
const LOT := Vector2(26.0, 28.0)

## Flag de AUTORIA (revisao de estados). A versao publicada sai SEMPRE com false.
const PREVIEW_STATES := false


# --- Raias de aeracao (3 raias ao longo de Z, lado -X) -----------------------------
const AER_X0 := -12.8
const AER_Z0 := -13.4
const AER_Z1 := 3.6
const LANE_W := 2.6
const WALL_T := 0.3
const WALK_W := 1.0          # passarela sobre a parede leste das raias
const RIM_Y := 1.3           # topo das paredes (tanque elevado; y >= 0 obrigatorio)
const WATER_Y := 1.05
const FLOOR_Y := 0.08
const XWALK_Z0 := -5.0       # passarela transversal sobre as raias
const XWALK_Z1 := -4.0
const RAIL_H := 1.0
# Canal de chegada (fundos das raias).
const CH_X0 := -12.2
const CH_Z1 := 5.0
# Casa de controle (frente, centro: porta em x = 0 voltada a -Z).
const HOUSE_X := 2.1
const HOUSE_Z0 := -13.0
const HOUSE_Z1 := -9.2
const HOUSE_PLINTH := 0.25
const HOUSE_WALL_TOP := 3.2
const HOUSE_RIDGE := 4.35
const HOUSE_EAVE := 0.35
const DOOR_W := 1.0
const DOOR_H := 2.2
# Decantador secundario (frente-direita).
const CLAR_C := Vector3(7.8, 0.0, -8.0)
const CLAR_R := 4.5
const CLAR_T := 0.3
const CLAR_H := 1.2
const CLAR_WATER_Y := 0.95
const CLAR_SIDES := 20
const BRIDGE_DEG := 135.0    # ponte radial aponta para frente-esquerda (rotacao Y do no)
# Biodigestor (fundos-esquerda: o volume alto nao esconde o resto no isometrico).
## Largo e baixo com cupula grande (le "biodigestor/gasometro", nao silo de fazenda).
const DIG_C := Vector3(-8.0, 0.0, 9.6)
const DIG_R := 3.8
const DIG_BASE_H := 0.35
const DIG_WALL_TOP := 3.6
const DIG_SIDES := 16
## Perfil da cupula (raio, altura) a partir do beiral.
const DIG_DOME := [[4.05, 3.6], [4.05, 3.78], [3.65, 4.75], [2.85, 5.55], [1.6, 6.1], [0.7, 6.28]]
# Leitos de secagem (fundos-direita): 4 leitos ao longo de X empilhados em Z.
const BED_X0 := 3.6
const BED_X1 := 12.8
const BED_Z0 := 1.0
const BED_W := 2.8
const BED_CURB := 0.25
const BED_H := 0.5
const BEDS := 4
# Tubulacao.
const PIPE_R := 0.22
const PIPE_Y := 0.45
## Linhas de difusores de ar ao longo das raias (z) — mesma posicao para tubos e espuma.
const AIR_Z := [-10.5, -7.0, -1.8, 1.4]
## Aglomerado de espuma por difusor: [deslocamento (x, dy, z), raio, fase]. Discos empilhados
## com dy >= 0,012 m entre si (sem z-fighting); x espelhado alternadamente para variar.
const FOAM := [
	[Vector3(0.0, 0.0, 0.0), 0.55, 0.0],
	[Vector3(0.55, 0.012, 0.35), 0.36, 10.0],
	[Vector3(-0.45, 0.024, -0.42), 0.3, 25.0],
	[Vector3(0.2, 0.036, -0.62), 0.22, 5.0],
]


# ---------------------------------------------------------------------------
# Auxiliares
# ---------------------------------------------------------------------------

## Trecho reto de tubo (prisma de 6 lados) entre dois pontos, sem tampas.
static func _pipe(b, slot: String, a: Vector3, c: Vector3, r: float, sides: int = 6) -> void:
	var d := c - a
	var length := d.length()
	if length < 1e-4:
		return
	var y := d / length
	var ref := Vector3.UP if absf(y.dot(Vector3.UP)) < 0.95 else Vector3.RIGHT
	var x := ref.cross(y).normalized()
	var z := x.cross(y).normalized()
	b.push(Transform3D(Basis(x, y, z), a))
	b.frustum(slot, Vector3.ZERO, r, r, length, sides, false, false, 30.0)
	b.pop()


## Tubo em polilinha: trechos + luvas cubicas nas curvas (escondem as pontas abertas).
static func _pipe_path(b, slot: String, pts: Array, r: float) -> void:
	for i in pts.size() - 1:
		_pipe(b, slot, pts[i], pts[i + 1], r)
	for i in range(1, pts.size() - 1):
		var p: Vector3 = pts[i]
		b.box_c(slot, p, Vector3(r, r, r) * 2.5)


## Calcos de concreto sob um trecho de tubo no chao (a cada ~3 m).
static func _sleepers(b, a: Vector3, c: Vector3, y_top: float) -> void:
	var d := c - a
	var n := maxi(1, int(floor(d.length() / 3.0)))
	var along_x := absf(d.x) > absf(d.z)
	for i in n:
		var p := a + d * ((float(i) + 0.5) / float(n))
		var sz := Vector3(0.3, 0, 0.6) if along_x else Vector3(0.6, 0, 0.3)
		b.box("concrete_dark", Vector3(p.x - sz.x * 0.5, 0.0, p.z - sz.z * 0.5), Vector3(p.x + sz.x * 0.5, y_top, p.z + sz.z * 0.5), PackedStringArray(["-y"]))


## Guarda-corpo ao longo de X (corrimao + travessa + montantes), base em y.
static func _rail_x(b, x0: float, x1: float, z: float, y: float) -> void:
	var t := 0.08
	b.box("sewage_ochre", Vector3(x0, y + RAIL_H - t, z - t * 0.5), Vector3(x1, y + RAIL_H, z + t * 0.5))
	b.box("sewage_ochre", Vector3(x0, y + RAIL_H * 0.5 - 0.03, z - 0.03), Vector3(x1, y + RAIL_H * 0.5 + 0.03, z + 0.03))
	var n := maxi(1, int(ceil((x1 - x0) / 2.4)))
	for i in n + 1:
		var x := lerpf(x0 + 0.04, x1 - 0.04, float(i) / float(n))
		b.box("sewage_ochre", Vector3(x - 0.04, y, z - 0.04), Vector3(x + 0.04, y + RAIL_H - t, z + 0.04), PackedStringArray(["-y", "+y"]))


## Guarda-corpo ao longo de Z.
static func _rail_z(b, z0: float, z1: float, x: float, y: float) -> void:
	var t := 0.08
	b.box("sewage_ochre", Vector3(x - t * 0.5, y + RAIL_H - t, z0), Vector3(x + t * 0.5, y + RAIL_H, z1))
	b.box("sewage_ochre", Vector3(x - 0.03, y + RAIL_H * 0.5 - 0.03, z0), Vector3(x + 0.03, y + RAIL_H * 0.5 + 0.03, z1))
	var n := maxi(1, int(ceil((z1 - z0) / 2.4)))
	for i in n + 1:
		var z := lerpf(z0 + 0.04, z1 - 0.04, float(i) / float(n))
		b.box("sewage_ochre", Vector3(x - 0.04, y, z - 0.04), Vector3(x + 0.04, y + RAIL_H - t, z + 0.04), PackedStringArray(["-y", "+y"]))


## Parede anelar (decantador): faces externa, interna e topo.
static func _ring_wall(b, slot: String, c: Vector3, r_out: float, r_in: float, y0: float, y1: float, sides: int, inner_slot: String = "") -> void:
	var islot := inner_slot if inner_slot != "" else slot
	for i in sides:
		var a0 := TAU * float(i) / float(sides)
		var a1 := TAU * float(i + 1) / float(sides)
		var d0 := Vector3(cos(a0), 0, sin(a0))
		var d1 := Vector3(cos(a1), 0, sin(a1))
		var dm := (d0 + d1).normalized()
		var o0 := c + d0 * r_out; var o1 := c + d1 * r_out
		var i0 := c + d0 * r_in; var i1 := c + d1 * r_in
		b.quad(slot, o0 + Vector3(0, y0, 0), o1 + Vector3(0, y0, 0), o1 + Vector3(0, y1, 0), o0 + Vector3(0, y1, 0), dm)
		b.quad(islot, i0 + Vector3(0, y0, 0), i1 + Vector3(0, y0, 0), i1 + Vector3(0, y1, 0), i0 + Vector3(0, y1, 0), -dm)
		b.quad(slot, i0 + Vector3(0, y1, 0), i1 + Vector3(0, y1, 0), o1 + Vector3(0, y1, 0), o0 + Vector3(0, y1, 0), Vector3.UP)


## Disco horizontal (poligono regular) voltado para cima.
static func _disc(b, slot: String, c: Vector3, r: float, y: float, sides: int, phase_deg: float = 0.0) -> void:
	var pts := []
	for i in sides:
		var a := deg_to_rad(phase_deg) + TAU * float(i) / float(sides)
		pts.append(Vector3(c.x + cos(a) * r, y, c.z + sin(a) * r))
	b.face(slot, pts, Vector3.UP)


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Janela no referencial local de fachada: parede no plano z = 0, exterior em -Z.
static func _window(b, cx: float, y0: float, w: float, h: float) -> void:
	var f := 0.12
	var dep := 0.12
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad("glass_dark", Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box("sewage_ochre", Vector3(x0 - f, y1, -dep), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("sewage_ochre", Vector3(x0 - f, y0, -dep), Vector3(x0, y1, 0.0), sk)
	b.box("sewage_ochre", Vector3(x1, y0, -dep), Vector3(x1 + f, y1, 0.0), sk)
	b.box("sewage_ochre", Vector3(x0 - f - 0.06, y0 - 0.1, -dep - 0.08), Vector3(x1 + f + 0.06, y0, 0.0), sk)


# Geometria derivada das raias.
static func _lane_x0(i: int) -> float:
	return AER_X0 + WALL_T + float(i) * (LANE_W + WALL_T)


## Inicio da parede-passarela leste (= fim da 3a raia).
static func _walk_x0() -> float:
	return _lane_x0(2) + LANE_W


static func _aer_x1() -> float:
	return _walk_x0() + WALK_W


static func _bed_z0(k: int) -> float:
	return BED_Z0 + BED_CURB + float(k) * (BED_W + BED_CURB)


# ---------------------------------------------------------------------------
# Partes do Body
# ---------------------------------------------------------------------------

func _aeration(b) -> void:
	var ax1 := _aer_x1()
	var wx0 := _walk_x0()
	var sk_top := PackedStringArray(["-y"])
	# Paredes frontal e dos fundos (largura total), oeste, divisorias e parede-passarela leste.
	b.box("concrete", Vector3(AER_X0, 0, AER_Z0), Vector3(ax1, RIM_Y, AER_Z0 + WALL_T), sk_top)
	b.box("concrete", Vector3(AER_X0, 0, AER_Z1 - WALL_T), Vector3(ax1, RIM_Y, AER_Z1), sk_top)
	var zi0 := AER_Z0 + WALL_T
	var zi1 := AER_Z1 - WALL_T
	var ends := PackedStringArray(["-y", "-z", "+z"])
	b.box("concrete", Vector3(AER_X0, 0, zi0), Vector3(AER_X0 + WALL_T, RIM_Y, zi1), ends)
	for i in [1, 2]:
		var x := _lane_x0(i) - WALL_T
		b.box("concrete", Vector3(x, 0, zi0), Vector3(x + WALL_T, RIM_Y, zi1), ends)
	b.box("concrete", Vector3(wx0, 0, zi0), Vector3(ax1, RIM_Y, zi1), ends)
	# Fundo das raias (visivel quando vazio).
	for i in 3:
		var x0 := _lane_x0(i)
		b.quad("concrete_dark", Vector3(x0, FLOOR_Y, zi0), Vector3(x0 + LANE_W, FLOOR_Y, zi0), Vector3(x0 + LANE_W, FLOOR_Y, zi1), Vector3(x0, FLOOR_Y, zi1), Vector3.UP)
	# Passarela transversal (laje sobre as divisorias, acima do nivel d'agua).
	b.box("concrete", Vector3(AER_X0, RIM_Y - 0.12, XWALK_Z0), Vector3(wx0, RIM_Y + 0.06, XWALK_Z1))
	# Piso da passarela leste levemente elevado (le como passarela, nao so parede).
	b.box("concrete_dark", Vector3(wx0 + 0.06, RIM_Y, zi0), Vector3(ax1 - 0.06, RIM_Y + 0.06, zi1), sk_top)
	# Tubos de ar (difusores) descendo para as raias a partir de um coletor na passarela.
	b.box("metal_light", Vector3(wx0 + 0.15, RIM_Y + 0.06, zi0 + 0.6), Vector3(wx0 + 0.35, RIM_Y + 0.26, zi1 - 0.6), sk_top)
	for zc in AIR_Z:
		for i in 3:
			var xc := _lane_x0(i) + LANE_W * 0.5
			b.box("metal_light", Vector3(xc - 0.07, WATER_Y - 0.3, zc - 0.07), Vector3(xc + 0.07, RIM_Y + 0.22, zc + 0.07), sk_top)
		var xa := _lane_x0(0) + LANE_W * 0.5 - 0.07
		b.box("metal_light", Vector3(xa, RIM_Y + 0.1, zc - 0.07), Vector3(wx0 + 0.3, RIM_Y + 0.24, zc + 0.07))
	# Guarda-corpos: perimetro, passarela leste (dois lados) e transversal (dois lados).
	var top := RIM_Y + 0.06
	_rail_z(b, AER_Z0 + 0.1, AER_Z1, AER_X0 + 0.15, RIM_Y)
	_rail_x(b, AER_X0, ax1, AER_Z0 + 0.15, RIM_Y)
	_rail_z(b, AER_Z0 + 0.1, -6.4, ax1 - 0.1, top)
	_rail_z(b, -5.4, AER_Z1, ax1 - 0.1, top)
	_rail_z(b, AER_Z0 + 0.1, XWALK_Z0, wx0 + 0.08, top)
	_rail_z(b, XWALK_Z1, AER_Z1 - 0.1, wx0 + 0.08, top)
	_rail_x(b, AER_X0 + 0.2, wx0, XWALK_Z0 + 0.06, RIM_Y + 0.06)
	_rail_x(b, AER_X0 + 0.2, wx0, XWALK_Z1 - 0.06, RIM_Y + 0.06)
	# Escada de acesso a passarela leste (lado +X, detalhe visual; nao e acesso funcional).
	var steps := 7
	var rise := (RIM_Y + 0.06) / float(steps)
	var sz1 := -5.5
	for k in steps:
		var z0 := sz1 - 0.3 * float(steps - k)
		b.box("concrete", Vector3(ax1, 0, z0), Vector3(ax1 + 0.8, rise * float(k + 1), z0 + 0.3), PackedStringArray(["-y", "-x"]))
	# Canal de chegada nos fundos (gradeamento + guarda-corpo).
	var cz0 := AER_Z1
	b.box("concrete", Vector3(CH_X0, 0, CH_Z1 - WALL_T), Vector3(ax1, RIM_Y, CH_Z1), sk_top)
	b.box("concrete", Vector3(CH_X0, 0, cz0), Vector3(CH_X0 + WALL_T, RIM_Y, CH_Z1 - WALL_T), PackedStringArray(["-y", "-z", "+z"]))
	b.box("concrete", Vector3(ax1 - WALL_T, 0, cz0), Vector3(ax1, RIM_Y, CH_Z1 - WALL_T), PackedStringArray(["-y", "-z", "+z"]))
	b.quad("concrete_dark", Vector3(CH_X0 + WALL_T, FLOOR_Y, cz0), Vector3(ax1 - WALL_T, FLOOR_Y, cz0), Vector3(ax1 - WALL_T, FLOOR_Y, CH_Z1 - WALL_T), Vector3(CH_X0 + WALL_T, FLOOR_Y, CH_Z1 - WALL_T), Vector3.UP)
	# Grade de barras (gradeamento) inclinada atravessando o canal.
	var gx := CH_X0 + 1.6
	b.box("metal_dark", Vector3(gx - 0.12, RIM_Y, cz0 - 0.05), Vector3(gx + 0.12, RIM_Y + 0.9, CH_Z1 + 0.05))
	b.box("metal_dark", Vector3(gx - 0.5, RIM_Y + 0.9, cz0 - 0.05), Vector3(gx + 0.5, RIM_Y + 1.1, CH_Z1 + 0.05))
	_rail_x(b, CH_X0 + 0.1, ax1, CH_Z1 - 0.15, RIM_Y)


func _control_house(b) -> void:
	var hz0 := HOUSE_Z0
	var hz1 := HOUSE_Z1
	var hx := HOUSE_X
	b.extrude("concrete_dark", _chamfer_rect(-hx - 0.06, hz0 - 0.06, hx + 0.06, hz1 + 0.06, 0.12), 0.0, HOUSE_PLINTH, true, false)
	b.extrude("concrete", _chamfer_rect(-hx, hz0, hx, hz1, 0.1), HOUSE_PLINTH, HOUSE_WALL_TOP, false, false)
	# Testeira (beiral) ocre + telhado de quatro aguas castanho.
	var e := HOUSE_EAVE
	b.box("sewage_ochre", Vector3(-hx - e, HOUSE_WALL_TOP - 0.14, hz0 - e), Vector3(hx + e, HOUSE_WALL_TOP + 0.04, hz1 + e))
	b.hip_roof("sewage_pipe", Vector2(-hx - e, hz0 - e), Vector2(hx + e, hz1 + e), HOUSE_WALL_TOP + 0.04, HOUSE_RIDGE, 1.2, "x", false)
	# Fachada -Z: porta metalica central (fechada) + duas janelas.
	b.push_translate(Vector3(0, 0, hz0))
	var dx := DOOR_W * 0.5
	var dy1 := HOUSE_PLINTH + DOOR_H
	b.box("metal_dark", Vector3(-dx, HOUSE_PLINTH, -0.05), Vector3(dx, dy1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("metal_dark", Vector3(-dx + 0.12, HOUSE_PLINTH + 1.3, -0.08), Vector3(dx - 0.12, dy1 - 0.15, -0.05), PackedStringArray(["+z"]))
	b.box("sewage_ochre", Vector3(-dx - 0.12, dy1, -0.12), Vector3(dx + 0.12, dy1 + 0.12, 0.0), PackedStringArray(["+z"]))
	b.box("sewage_ochre", Vector3(-dx - 0.12, HOUSE_PLINTH, -0.12), Vector3(-dx, dy1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("sewage_ochre", Vector3(dx, HOUSE_PLINTH, -0.12), Vector3(dx + 0.12, dy1, 0.0), PackedStringArray(["+z", "-y"]))
	_window(b, -1.3, 1.25, 0.75, 1.0)
	_window(b, 1.3, 1.25, 0.75, 1.0)
	b.pop()
	# Lateral +X: janela.
	b.push_rotate_y(-90.0, Vector3(hx, 0, (hz0 + hz1) * 0.5))
	_window(b, 0.0, 1.25, 1.2, 1.0)
	b.pop()
	# Degrau diante da porta.
	b.box("concrete_dark", Vector3(-0.8, 0, hz0 - 0.06 - 0.5), Vector3(0.8, 0.15, hz0 - 0.06), PackedStringArray(["-y", "+z"]))


func _clarifier(b) -> void:
	var c := CLAR_C
	var r_in := CLAR_R - CLAR_T
	_ring_wall(b, "concrete", c, CLAR_R, r_in, 0.0, CLAR_H, CLAR_SIDES)
	_disc(b, "concrete_dark", c, r_in, 0.05, CLAR_SIDES)
	# Calha/vertedor periferico (anel interno): le como "tanque redondo de decantacao".
	_ring_wall(b, "concrete_dark", c, r_in - 0.45, r_in - 0.6, 0.05, CLAR_WATER_Y + 0.12, CLAR_SIDES)
	# Coluna central + acionamento da ponte.
	b.cylinder("concrete", c + Vector3(0, 0.05, 0), 0.55, 1.45, 10, true, false)
	b.cylinder("metal_dark", c + Vector3(0, 1.5, 0), 0.8, 0.42, 10, true, false)
	b.cylinder("sewage_ochre", c + Vector3(0, 1.92, 0), 0.4, 0.18, 8, true, false)
	# Caixa de saida do efluente tratado (+X) e tubo ate a borda do lote.
	var ox0 := c.x + CLAR_R - 0.3
	var ox1 := ox0 + 0.5
	b.box("concrete", Vector3(ox0, 0, c.z - 0.65), Vector3(ox1, 1.0, c.z + 0.65), PackedStringArray(["-y"]))
	_pipe(b, "sewage_pipe", Vector3(ox1, PIPE_Y, c.z), Vector3(LOT.x * 0.5 - 0.1, PIPE_Y, c.z), PIPE_R + 0.04)
	b.push_basis(Basis(Vector3(0, 0, 1), deg_to_rad(-90.0)), Vector3(LOT.x * 0.5 - 0.1, PIPE_Y, c.z))
	b.cylinder("sewage_pipe", Vector3.ZERO, PIPE_R + 0.12, 0.08, 8, true, true)
	b.pop()


func _digester(b) -> void:
	var c := DIG_C
	b.frustum("concrete", c, DIG_R + 0.3, DIG_R + 0.2, DIG_BASE_H, DIG_SIDES, true, false)
	b.cylinder("sewage_ochre", c + Vector3(0, DIG_BASE_H, 0), DIG_R, DIG_WALL_TOP - DIG_BASE_H, DIG_SIDES, false, false)
	# Cinta (escala e leitura de tanque) em concreto.
	b.frustum("concrete", c + Vector3(0, 1.9, 0), DIG_R + 0.06, DIG_R + 0.06, 0.22, DIG_SIDES, true, true)
	# Cupula facetada castanha com beiral.
	var rings: Array = DIG_DOME
	b.face("sewage_pipe", _ring_pts(c, rings[0][0], rings[0][1]), Vector3.DOWN)
	for i in rings.size() - 1:
		var r0: float = rings[i][0]
		var y0: float = rings[i][1]
		var r1: float = rings[i + 1][0]
		var y1: float = rings[i + 1][1]
		b.frustum("sewage_pipe", c + Vector3(0, y0, 0), r0, r1, y1 - y0, DIG_SIDES, false, false)
	var ytop: float = rings[rings.size() - 1][1]
	b.cylinder("metal_dark", c + Vector3(0, ytop - 0.02, 0), 0.75, 0.35, 8, true, false)
	_pipe(b, "metal_dark", c + Vector3(0.3, ytop + 0.3, 0.0), c + Vector3(0.3, ytop + 1.0, 0.0), 0.1)
	b.box_c("metal_dark", c + Vector3(0.3, ytop + 1.0, 0.0), Vector3(0.32, 0.12, 0.32))
	# Escada de marinheiro na face visivel (frente-direita), do chao ao beiral.
	var dir := Vector3(1, 0, -1).normalized()
	var side := Vector3(1, 0, 1).normalized()
	var base := c + dir * (DIG_R + 0.18)
	for s in [-1.0, 1.0]:
		var p: Vector3 = base + side * (0.3 * s)
		_pipe(b, "metal_dark", p + Vector3(0, DIG_BASE_H, 0), p + Vector3(0, DIG_WALL_TOP + 0.6, 0), 0.05, 4)
	for k in 6:
		var y := DIG_BASE_H + 0.45 + 0.5 * float(k)
		_pipe(b, "metal_dark", base - side * 0.3 + Vector3(0, y, 0), base + side * 0.3 + Vector3(0, y, 0), 0.03, 4)


static func _ring_pts(c: Vector3, r: float, y: float) -> Array:
	var pts := []
	for i in DIG_SIDES:
		var a := TAU * float(i) / float(DIG_SIDES)
		pts.append(Vector3(c.x + cos(a) * r, y, c.z + sin(a) * r))
	return pts


func _drying_beds(b) -> void:
	var sk := PackedStringArray(["-y"])
	var z_end := _bed_z0(BEDS)
	# Meios-fios: frente de cada leito e fundo do ultimo; extremos oeste/leste.
	for k in BEDS + 1:
		var z0 := BED_Z0 + float(k) * (BED_W + BED_CURB)
		b.box("concrete", Vector3(BED_X0, 0, z0), Vector3(BED_X1, BED_H, z0 + BED_CURB), sk)
	for x0 in [BED_X0, BED_X1 - BED_CURB]:
		for k in BEDS:
			var z0 := _bed_z0(k)
			b.box("concrete", Vector3(x0, 0, z0), Vector3(x0 + BED_CURB, BED_H, z0 + BED_W), PackedStringArray(["-y", "-z", "+z"]))
	for k in BEDS:
		var z0 := _bed_z0(k)
		b.quad("concrete_dark", Vector3(BED_X0 + BED_CURB, 0.06, z0), Vector3(BED_X1 - BED_CURB, 0.06, z0), Vector3(BED_X1 - BED_CURB, 0.06, z0 + BED_W), Vector3(BED_X0 + BED_CURB, 0.06, z0 + BED_W), Vector3.UP)
	# Distribuidor de lodo elevado ao longo do lado oeste, com bicas para cada leito.
	var hx := BED_X0 - 0.35
	var hy := 0.85
	_pipe(b, "sewage_pipe", Vector3(hx, hy, BED_Z0 + 1.0), Vector3(hx, hy, z_end - 1.0), PIPE_R - 0.04)
	for zz in [BED_Z0 + 1.0, z_end - 1.0]:
		b.box_c("sewage_pipe", Vector3(hx, hy, zz), Vector3(0.45, 0.45, 0.45))
	for k in BEDS:
		var zc := _bed_z0(k) + BED_W * 0.5
		_pipe(b, "sewage_pipe", Vector3(hx, hy, zc), Vector3(BED_X0 + 0.7, 0.62, zc), 0.12)
		b.box("concrete_dark", Vector3(hx - 0.15, 0, zc - 0.15), Vector3(hx + 0.15, hy - 0.1, zc + 0.15), sk)


func _pipes(b) -> void:
	var ax1 := _aer_x1()
	var y := PIPE_Y
	# Chegada do esgoto (-X) no canal dos fundos, com flange na borda do lote.
	var zin := (AER_Z1 + CH_Z1) * 0.5
	_pipe(b, "sewage_pipe", Vector3(-LOT.x * 0.5 + 0.1, y + 0.1, zin), Vector3(CH_X0 + 0.1, y + 0.1, zin), PIPE_R + 0.06)
	b.push_basis(Basis(Vector3(0, 0, 1), deg_to_rad(-90.0)), Vector3(-LOT.x * 0.5 + 0.02, y + 0.1, zin))
	b.cylinder("sewage_pipe", Vector3.ZERO, PIPE_R + 0.16, 0.08, 8, true, true)
	b.pop()
	# Raias -> decantador (atras da casa de controle).
	var zl := CLAR_C.z
	var xc_in := CLAR_C.x - CLAR_R + 0.1
	_pipe(b, "sewage_pipe", Vector3(ax1 - 0.05, y, zl), Vector3(xc_in, y, zl), PIPE_R)
	_sleepers(b, Vector3(ax1 + 0.6, 0, zl), Vector3(xc_in - 0.6, 0, zl), y - PIPE_R + 0.02)
	# Lodo: decantador -> biodigestor (corredor central).
	var xs := 0.8
	var zs := -2.4
	var zd := 8.6
	var xd := DIG_C.x + sqrt(DIG_R * DIG_R - pow(zd - DIG_C.z, 2.0)) - 0.1
	var p3 := [Vector3(CLAR_C.x, y, CLAR_C.z + CLAR_R - 0.1), Vector3(CLAR_C.x, y, zs), Vector3(xs, y, zs), Vector3(xs, y, zd), Vector3(xd, y, zd)]
	_pipe_path(b, "sewage_pipe", p3, PIPE_R - 0.03)
	_sleepers(b, Vector3(CLAR_C.x - 0.8, 0, zs), Vector3(xs + 0.8, 0, zs), y - PIPE_R + 0.05)
	_sleepers(b, Vector3(xs, 0, zs + 0.8), Vector3(xs, 0, zd - 0.8), y - PIPE_R + 0.05)
	# Lodo digerido: biodigestor -> leitos de secagem (fundos).
	var zb := _bed_z0(BEDS) - 1.0
	var xb := DIG_C.x + sqrt(DIG_R * DIG_R - pow(zb - DIG_C.z, 2.0)) - 0.1
	var hx := BED_X0 - 0.35
	_pipe_path(b, "sewage_pipe", [Vector3(xb, y, zb), Vector3(hx, y, zb), Vector3(hx, 0.85, zb)], PIPE_R - 0.04)
	_sleepers(b, Vector3(xb + 0.8, 0, zb), Vector3(hx - 0.8, 0, zb), y - PIPE_R + 0.06)


# ---------------------------------------------------------------------------
# Estados (host liga com estado real)
# ---------------------------------------------------------------------------

func _water_state(b) -> void:
	var zi0 := AER_Z0 + WALL_T
	var zi1 := AER_Z1 - WALL_T
	for i in 3:
		var x0 := _lane_x0(i)
		b.quad("sewage_effluent", Vector3(x0, WATER_Y, zi0), Vector3(x0 + LANE_W, WATER_Y, zi0), Vector3(x0 + LANE_W, WATER_Y, zi1), Vector3(x0, WATER_Y, zi1), Vector3.UP)
		# Espuma de aeracao sobre os difusores (le "esgoto em tratamento", nao agua limpa):
		# aglomerados irregulares de bolhas facetadas em tom bege-acinzentado.
		var k := 0
		for zc in AIR_Z:
			var xc := x0 + LANE_W * 0.5
			for f in FOAM:
				var o: Vector3 = f[0]
				var sx := -1.0 if (k + i) % 2 == 1 else 1.0
				_disc(b, "concrete", Vector3(xc + o.x * sx, 0, zc + o.z), f[1], WATER_Y + 0.02 + o.y, 6, 17.0 * float(k + i) + f[2])
			k += 1
	# Canal de chegada.
	var ax1 := _aer_x1()
	b.quad("sewage_effluent", Vector3(CH_X0 + WALL_T, WATER_Y - 0.1, AER_Z1), Vector3(ax1 - WALL_T, WATER_Y - 0.1, AER_Z1), Vector3(ax1 - WALL_T, WATER_Y - 0.1, CH_Z1 - WALL_T), Vector3(CH_X0 + WALL_T, WATER_Y - 0.1, CH_Z1 - WALL_T), Vector3.UP)
	# Decantador: espelho d'agua interno e calha periferica.
	var r_in := CLAR_R - CLAR_T
	_ring_flat(b, "sewage_effluent", CLAR_C, r_in - 0.02, r_in - 0.43, CLAR_WATER_Y - 0.05, CLAR_SIDES)
	_ring_flat(b, "sewage_effluent", CLAR_C, r_in - 0.62, 0.57, CLAR_WATER_Y, CLAR_SIDES)


## Anel plano horizontal (coroa) voltado para cima.
static func _ring_flat(b, slot: String, c: Vector3, r_out: float, r_in: float, y: float, sides: int) -> void:
	for i in sides:
		var a0 := TAU * float(i) / float(sides)
		var a1 := TAU * float(i + 1) / float(sides)
		var d0 := Vector3(cos(a0), 0, sin(a0))
		var d1 := Vector3(cos(a1), 0, sin(a1))
		b.quad(slot, c + d0 * r_in + Vector3(0, y, 0), c + d1 * r_in + Vector3(0, y, 0), c + d1 * r_out + Vector3(0, y, 0), c + d0 * r_out + Vector3(0, y, 0), Vector3.UP)


func _sludge_state(b) -> void:
	# Torta de lodo rachada: placas irregulares com frestas inclinadas (le "lodo secando",
	# nao tabuas nem terra arada). Deterministico.
	var xa := BED_X0 + BED_CURB + 0.06
	var xb := BED_X1 - BED_CURB - 0.06
	var g := 0.08
	for k in BEDS:
		var z0 := _bed_z0(k) + 0.06
		var z1 := _bed_z0(k) + BED_W - 0.06
		var zm := lerpf(z0, z1, 0.42 + 0.14 * float(k % 2))
		var h := 0.2 if k % 2 == 0 else 0.15
		for m in 2:
			var ra := z0 if m == 0 else zm + g * 0.5
			var rb := zm - g * 0.5 if m == 0 else z1
			# Linhas de fissura: x no topo (ra) e na base (rb) da fileira.
			var top := [xa]
			var bot := [xa]
			for j in 3:
				var cx := lerpf(xa, xb, (float(j) + 1.0) / 4.0 + 0.06 * sin(float(k * 5 + m * 3 + j * 2)))
				var s := 0.35 if (j + k + m) % 2 == 0 else -0.3
				top.append(cx + s)
				bot.append(cx - s)
			top.append(xb)
			bot.append(xb)
			for j in 4:
				var gl := g * 0.5 if j > 0 else 0.0
				var gr := g * 0.5 if j < 3 else 0.0
				var poly := PackedVector2Array([
					Vector2(top[j] + gl, ra), Vector2(top[j + 1] - gr, ra),
					Vector2(bot[j + 1] - gr, rb), Vector2(bot[j] + gl, rb),
				])
				b.extrude("sewage_sludge", poly, 0.06, 0.06 + h - 0.02 * float(j % 2), true, false)


# ---------------------------------------------------------------------------
# Ponte radial do decantador (parte movel: no separado, pivo no eixo)
# ---------------------------------------------------------------------------

func _bridge(b) -> void:
	var x0 := 0.35
	var x1 := CLAR_R + 0.1
	var yd := 1.5
	b.box("metal_dark", Vector3(x0, yd - 0.1, -0.45), Vector3(x1, yd, 0.45))
	for s in [-1.0, 1.0]:
		var zf: float = 0.4 * s
		b.box("metal_dark", Vector3(x0, yd - 0.45, zf - 0.05), Vector3(x1 - 0.4, yd - 0.1, zf + 0.05), PackedStringArray(["+y"]))
	# Carro de rolamento sobre a parede.
	b.box("metal_dark", Vector3(CLAR_R - 0.55, CLAR_H + 0.01, -0.6), Vector3(CLAR_R + 0.12, yd - 0.1, 0.6), PackedStringArray(["-y"]))
	# Guarda-corpos ocre dos dois lados.
	for s in [-1.0, 1.0]:
		_rail_x(b, x0 + 0.3, x1, 0.4 * s, yd)
	# Raspador de escuma descendo ate a lamina d'agua.
	b.box("metal_dark", Vector3(2.4, CLAR_WATER_Y + 0.02, -0.06), Vector3(2.55, yd - 0.1, 0.06), PackedStringArray(["+y"]))
	b.box("metal_dark", Vector3(1.4, CLAR_WATER_Y + 0.02, -0.08), Vector3(3.6, CLAR_WATER_Y + 0.14, 0.08))


# ---------------------------------------------------------------------------
# Icone de catalogo (ic = Source/make_v1_icons.gd). Versao SIMPLIFICADA "em operacao":
# raias retangulares com efluente oliva + espuma, decantador redondo com ponte e
# biodigestor ocre de cupula castanha; casa de controle pequena na frente. Os leitos de
# secagem ficam de fora (alargariam o icone e diminuiriam as formas principais).
# ---------------------------------------------------------------------------

## Retangulo horizontal (decal) no topo de uma parte, em y.
static func _ic_top_rect(ic, x0: float, z0: float, x1: float, z1: float, y: float, col: String) -> void:
	ic.quad([Vector3(x0, y, z0), Vector3(x1, y, z0), Vector3(x1, y, z1), Vector3(x0, y, z1)], col, true)


## Poligono regular horizontal (decal) em y.
static func _ic_disc(ic, cx: float, cz: float, r: float, y: float, sides: int, col: String, phase_deg: float = 0.0) -> void:
	var pts := []
	for i in sides:
		var a := deg_to_rad(phase_deg) + TAU * float(i) / float(sides)
		pts.append(Vector3(cx + cos(a) * r, y, cz + sin(a) * r))
	ic.quad(pts, col, true)


## Mancha irregular de espuma (decal horizontal, 8 vertices com raio deterministico).
static func _ic_blob(ic, cx: float, cz: float, r: float, y: float, col: String, seed: int) -> void:
	var pts := []
	for j in 8:
		var a := TAU * float(j) / 8.0 + 0.3 * float(seed)
		var rr := r * (0.86 + 0.14 * sin(float(seed) * 1.7 + float(j) * 2.3))
		pts.append(Vector3(cx + cos(a) * rr, y, cz + sin(a) * rr * 1.15))
	ic.quad(pts, col, true)


func draw_icon(ic) -> void:
	var conc: String = ic.c("concrete")
	var floor_col: String = ic.c("concrete_dark")
	var ochre: String = ic.c("sewage_ochre")
	var pipe: String = ic.c("sewage_pipe")
	var eff: String = ic.c("sewage_effluent")
	var dark: String = ic.c("metal_dark")
	var glass: String = ic.c("glass_dark")
	var ax1 := _aer_x1()
	var wx0 := _walk_x0()
	# Tubulacao no chao (sempre atras de tudo).
	var pr := 0.35
	ic.part(-50.0)
	ic.box(Vector3(ax1, 0.2, CLAR_C.z - pr), Vector3(CLAR_C.x - CLAR_R + 0.2, 0.9, CLAR_C.z + pr), pipe)
	# Biodigestor: base de concreto, corpo ocre com cinta clara, cupula castanha facetada
	# (menos facetas que o modelo: menos contorno, leitura melhor em 64 px).
	var sides := 8
	ic.part()
	ic.prism(DIG_C, DIG_R + 0.3, DIG_R + 0.2, DIG_BASE_H, sides, conc)
	ic.part(0.1)
	ic.prism(DIG_C + Vector3(0, DIG_BASE_H, 0), DIG_R, DIG_R, DIG_WALL_TOP - DIG_BASE_H, sides, ochre)
	ic.part(0.2)
	ic.prism(DIG_C + Vector3(0, 1.9, 0), DIG_R + 0.08, DIG_R + 0.08, 0.3, sides, conc)
	var dome := [[4.05, DIG_WALL_TOP], [3.15, 5.25], [0.75, 6.28]]
	for i in dome.size() - 1:
		ic.part(0.3 + 0.1 * float(i))
		ic.prism(DIG_C + Vector3(0, dome[i][1], 0), dome[i][0], dome[i + 1][0], dome[i + 1][1] - dome[i][1], sides, pipe)
	ic.part(0.8)
	ic.prism(DIG_C + Vector3(0, 6.28, 0), 0.75, 0.75, 0.4, 6, dark)
	# Raias de aeracao + canal de chegada: bloco de concreto com espelhos de efluente oliva
	# (um por raia/metade), espuma clara sobre os difusores.
	var y := RIM_Y + 0.01
	ic.part()
	ic.box(Vector3(AER_X0, 0, AER_Z0), Vector3(ax1, RIM_Y, CH_Z1), conc)
	var zi0 := AER_Z0 + WALL_T
	var zi1 := AER_Z1 - WALL_T
	for i in 3:
		var x0 := _lane_x0(i)
		_ic_top_rect(ic, x0, zi0, x0 + LANE_W, XWALK_Z0, y, eff)
		_ic_top_rect(ic, x0, XWALK_Z1, x0 + LANE_W, zi1, y, eff)
	_ic_top_rect(ic, AER_X0 + WALL_T, AER_Z1, ax1 - WALL_T, CH_Z1 - WALL_T, y, eff)
	_ic_top_rect(ic, wx0 + 0.08, zi0, ax1 - 0.08, zi1, y, floor_col)
	var k := 0
	for zc in AIR_Z:
		for i in 3:
			var xc := _lane_x0(i) + LANE_W * 0.5 + (0.2 if (i + k) % 2 == 0 else -0.2)
			_ic_blob(ic, xc, zc, 0.95, y + 0.01, conc, i * 3 + k * 7)
		k += 1
	# Guarda-corpos ocre (corrimao + montantes) na frente e na passarela leste.
	var rt := 0.16
	var ry0 := RIM_Y
	var ry1 := RIM_Y + RAIL_H
	ic.part(0.5)
	ic.box(Vector3(ax1 - 0.1 - rt * 0.5, ry1 - rt, AER_Z0 + 0.1), Vector3(ax1 - 0.1 + rt * 0.5, ry1, AER_Z1), ochre)
	for zp in [AER_Z0 + 0.1, -8.6, -4.5, -0.5, AER_Z1 - rt]:
		ic.part(0.5)
		ic.box(Vector3(ax1 - 0.1 - rt * 0.5, ry0, zp), Vector3(ax1 - 0.1 + rt * 0.5, ry1 - rt, zp + rt), ochre)
	ic.part(0.6)
	ic.box(Vector3(AER_X0, ry1 - rt, AER_Z0 + 0.1), Vector3(ax1 - 0.1, ry1, AER_Z0 + 0.1 + rt), ochre)
	for xp in [AER_X0 + 0.1, -9.6, -6.4]:
		ic.part(0.6)
		ic.box(Vector3(xp, ry0, AER_Z0 + 0.1), Vector3(xp + rt, ry1 - rt, AER_Z0 + 0.1 + rt), ochre)
	# Decantador secundario: anel de concreto, espelho oliva, acionamento e ponte radial.
	ic.part()
	ic.prism(CLAR_C, CLAR_R, CLAR_R, CLAR_H, 14, conc)
	_ic_disc(ic, CLAR_C.x, CLAR_C.z, CLAR_R - 0.55, CLAR_H + 0.01, 14, eff, 360.0 / 28.0)
	ic.part(1.0)
	ic.box(Vector3(CLAR_C.x - CLAR_R - 0.1, 1.35, CLAR_C.z - 0.45), Vector3(CLAR_C.x, 1.6, CLAR_C.z + 0.45), dark, ochre)
	ic.part(1.2)
	ic.prism(CLAR_C + Vector3(0, CLAR_H, 0), 0.8, 0.8, 0.7, 8, dark, ochre)
	# Casa de controle (fachada -Z, porta central metalica com moldura ocre).
	var hx := HOUSE_X
	var e := HOUSE_EAVE
	ic.part()
	ic.box(Vector3(-hx, 0, HOUSE_Z0), Vector3(hx, HOUSE_WALL_TOP, HOUSE_Z1), conc)
	var zf := HOUSE_Z0 - 0.02
	ic.facade_rect(-DOOR_W * 0.5 - 0.15, DOOR_W * 0.5 + 0.15, HOUSE_PLINTH, HOUSE_PLINTH + DOOR_H + 0.15, zf, ochre)
	ic.facade_rect(-DOOR_W * 0.5, DOOR_W * 0.5, HOUSE_PLINTH, HOUSE_PLINTH + DOOR_H, zf - 0.01, dark)
	for xw in [-1.4, 1.4]:
		ic.facade_rect(xw - 0.5, xw + 0.5, 1.1, 2.4, zf, ochre)
		ic.facade_rect(xw - 0.35, xw + 0.35, 1.25, 2.25, zf - 0.01, glass)
	ic.side_rect(-11.8, -10.4, 1.1, 2.4, hx + 0.02, ochre)
	ic.side_rect(-11.65, -10.55, 1.25, 2.25, hx + 0.03, glass)
	ic.part(0.1)
	ic.box(Vector3(-hx - e, HOUSE_WALL_TOP - 0.14, HOUSE_Z0 - e), Vector3(hx + e, HOUSE_WALL_TOP + 0.04, HOUSE_Z1 + e), ochre)
	ic.part(0.2)
	ic.hip(Vector3(-hx - e, HOUSE_WALL_TOP + 0.04, HOUSE_Z0 - e), Vector3(hx + e, HOUSE_WALL_TOP + 0.04, HOUSE_Z1 + e), HOUSE_RIDGE, 1.2, pipe)


# ---------------------------------------------------------------------------
# Montagem
# ---------------------------------------------------------------------------

func build(kit) -> void:
	var b = kit.builder()
	_aeration(b)
	_control_house(b)
	_clarifier(b)
	_digester(b)
	_drying_beds(b)
	_pipes(b)
	print("[sewage] triangulos Body no builder: ", b.triangle_count())
	var mesh = kit.save_mesh(b, FAMILY_ID)

	var bb = kit.builder()
	_bridge(bb)
	var bridge_mesh = kit.save_mesh(bb, FAMILY_ID + ".clarifier_bridge")

	var wb = kit.builder()
	_water_state(wb)
	var water_mesh = kit.save_mesh(wb, FAMILY_ID + ".state_water")

	var sb = kit.builder()
	_sludge_state(sb)
	var sludge_mesh = kit.save_mesh(sb, FAMILY_ID + ".state_sludge")

	var root = kit.new_root("SewageTreatment", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var bridge = kit.add_mesh(root, "ClarifierBridge", bridge_mesh, Transform3D(Basis(Vector3.UP, deg_to_rad(BRIDGE_DEG)), CLAR_C))
	bridge.set_meta("moving_part", "ponte radial do decantador: gira em torno do Y local (eixo do decantador); sem animacao, o host decide com estado real")
	var water = kit.add_node(root, "State_Water")
	water.visible = PREVIEW_STATES
	water.set_meta("state", "efluente em tratamento: raias de aeracao (com espuma), canal de chegada e decantador; ligar so com estado real")
	kit.add_mesh(water, "Effluent", water_mesh)
	var sludge = kit.add_node(root, "State_Sludge")
	sludge.visible = PREVIEW_STATES
	sludge.set_meta("state", "torta de lodo nos leitos de secagem; ligar so com estado real")
	kit.add_mesh(sludge, "SludgeCake", sludge_mesh)

	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(0.0, 0.0, HOUSE_Z0 - 0.06 - 0.5 - 0.35), Vector3.FORWARD,
		"entrada de pedestre candidata diante da porta da casa de controle (centro da borda frontal); acesso real e da integracao")
	var zin := (AER_Z1 + CH_Z1) * 0.5
	kit.add_marker(root, "Candidate_PipeInlet", Vector3(-LOT.x * 0.5 + 0.02, PIPE_Y + 0.1, zin), Vector3.LEFT,
		"chegada candidata do coletor de esgoto (borda -X, canal de chegada); conexao real e da integracao")
	kit.add_marker(root, "Candidate_PipeOutlet", Vector3(LOT.x * 0.5 - 0.02, PIPE_Y, CLAR_C.z), Vector3.RIGHT,
		"saida candidata do efluente tratado para o corpo d'agua (borda +X, caixa de saida do decantador); conexao real e da integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Tratamento de esgoto",
		"lot": LOT,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"raias_aeracao": {"quantidade": 3, "largura_util_m": LANE_W, "comprimento_m": AER_Z1 - AER_Z0, "borda_m": RIM_Y, "passarela_m": WALK_W},
			"decantador": {"diametro_m": CLAR_R * 2.0, "borda_m": CLAR_H},
			"biodigestor": {"diametro_m": DIG_R * 2.0, "parede_m": DIG_WALL_TOP, "topo_cupula_m": DIG_DOME[DIG_DOME.size() - 1][1]},
			"leitos_secagem": {"quantidade": BEDS, "l_x_c_m": [BED_W, BED_X1 - BED_X0 - 2.0 * BED_CURB], "meio_fio_m": BED_H},
			"casa_controle_l_x_p_m": [HOUSE_X * 2.0, HOUSE_Z1 - HOUSE_Z0],
			"porta_l_x_a_m": [DOOR_W, DOOR_H],
		},
		"parts": {
			"Body": "estrutura permanente (7 superficies = slots): concrete (paredes das raias, canal, decantador, leitos, casa, cintas e base do biodigestor), concrete_dark (fundos vazios, piso da passarela, calha do decantador, plinto/degrau, calcos), sewage_ochre (guarda-corpos, corpo do biodigestor, testeira, molduras de porta/janelas, topo do acionamento), sewage_pipe (tubulacao, cupula do biodigestor, telhado), metal_dark (porta, gradeamento, acionamento, escada de marinheiro, respiro), metal_light (coletor e descidas dos difusores de ar), glass_dark (janelas)",
			"ClarifierBridge": "parte movel separada: ponte radial (metal_dark + guarda-corpo sewage_ochre), pivo no eixo do decantador, sem animacao",
			"State_Water": "Node3D oculto: efluente marrom-oliva (sewage_effluent) nas raias, canal e decantador + espuma de aeracao bege-acinzentada (concrete) sobre os difusores",
			"State_Sludge": "Node3D oculto: torta de lodo rachada (sewage_sludge) nos leitos de secagem",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta da casa de controle, forward -Z",
			"Candidate_PipeInlet": "Marker3D candidato na chegada do esgoto (borda -X), forward -X",
			"Candidate_PipeOutlet": "Marker3D candidato na saida do efluente tratado (borda +X), forward +X",
		},
		"notes": [
			"Pivo no centro do lote 26 x 28 m, no solo; fachada/porta da casa de controle em -Z, centrada em x = 0.",
			"Distinta da agua potavel: raias RETANGULARES com espuma, biodigestor terroso e leitos de lodo; paleta ocre/castanho/oliva + concreto, sem azul.",
			"Tanques acima do solo (envelope y >= 0): borda das raias a 1,3 m, decantador a 1,2 m. Sem estado, os fundos aparecem vazios (concrete_dark).",
			"Capacidade, vazao, emissao, operacao e conexoes sao dados de simulacao/integracao; a tubulacao desenhada so sugere o fluxo.",
			"Escada da passarela e escada de marinheiro sao detalhes visuais, nao acessos funcionais (somente os Marker3D candidatos).",
		],
	})
