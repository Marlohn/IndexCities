## Receita original: captacao de agua bruta (utility.water_intake.a) — amostra V1 da segunda
## onda, revisao humana PENDENTE. Somente visual: nao define vazao, capacidade, custo,
## acesso, rede hidraulica, qualidade da agua nem regra de fonte natural.
##
## Composicao: casa de bombas pequena (porta dupla voltada a -Z), duas linhas de succao azuis
## aparentes sobre selas de concreto ate a tomada d'agua (caixa de concreto com grades) na
## borda +Z do lote — lado CANDIDATO voltado para o rio/lago do terreno — e a adutora de
## recalque saindo pela lateral +X ate uma caixa de registro na borda do lote, onde desce
## para o solo. NAO ha tratamento aqui (sem tanques, decantadores ou filtros: isso e outra
## instalacao fisica, utility.water_treatment.a) e NAO ha superficie de agua modelada (o
## rio/lago e do terreno).
##
## Codigo de cor da cadeia de agua potavel: concreto claro/branco + AZUL (telhado metalico e
## tubos). Os tons ocre/oliva/castanho ficam reservados ao esgoto.
extends RefCounted

const FAMILY_ID := "utility.water_intake.a"
const MATERIAL_PREFIX := "intake_"
const MATERIALS := {
	# Concreto claro levemente frio (codigo "agua limpa"); o compartilhado `concrete` e mais quente.
	"intake_concrete": {"albedo": "#c9cdc8", "roughness": 0.9},
	# Telhado metalico azul-ardosia (mais claro e menos saturado que os tubos, para os tubos
	# continuarem destacados quando passam diante do telhado no isometrico).
	"intake_roof": {"albedo": "#5f87b0", "roughness": 0.55, "metallic": 0.2},
	"intake_pipe": {"albedo": "#2f6fa8", "roughness": 0.45, "metallic": 0.25},
}

# --- Lote logico do host (X = largura, Z = profundidade); pivo no centro ------------------
const LOT := Vector2(14.0, 16.0)


# --- Casa de bombas (medidas CANDIDATAS, metros) ------------------------------------------
const PH_X := 3.0            # meia largura (6,0 m em X)
const PH_Z0 := -6.6          # fachada -Z
const PH_Z1 := -1.6          # fundos (5,0 m em Z)
const PLINTH_H := 0.3
const PLINTH_OUT := 0.06
const WALL_TOP := 3.3
const CHAMFER := 0.08
const EAVE := 0.35           # beiral lateral (+-X)
const GABLE_OVER := 0.3      # beiral na empena frontal/fundos
const SLOPE := 0.45          # ~24 graus
const ROOF_T := 0.14
const RIB_STEP := 0.62       # passo das nervuras da telha metalica
const DOOR_W := 2.0          # porta dupla de equipamento (fechada)
const DOOR_H := 2.4
const LANDING_D := 0.55

# --- Tubulacao ---------------------------------------------------------------------------
const SUCTION_XS := [-1.4, 1.4]
const SUCTION_Y := 0.85
const SUCTION_R := 0.28
const OUT_Z := -4.1          # adutora de recalque (lateral +X)
const OUT_Y := 1.0
const OUT_R := 0.25
const OUT_DROP_X := 6.3      # onde a adutora desce para o solo (caixa de registro)
const VALVE_X := 4.1

# --- Tomada d'agua (cabeceira) na borda +Z -----------------------------------------------
const HW_X := 3.0
const HW_Z0 := 4.6
const HW_Z1 := 7.9
const HW_H := 1.9
const WING_TIP := Vector2(4.8, 6.85)   # ponta das alas (x, z) — lado +X; espelhado em -X
const WING_T := 0.4
const WING_ROOT_H := 1.45
const WING_TIP_H := 0.85
## Enrocamento da margem (x, z, raio) no lado +X; espelhado em -X.
const RIPRAP := [[5.45, 7.2, 0.68], [6.3, 6.55, 0.5], [5.55, 6.1, 0.42]]


static func _low(x: float) -> float:
	return WALL_TOP + SLOPE * (PH_X - absf(x))


static func _top(x: float) -> float:
	return _low(x) + ROOF_T


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Prisma de perfil (x, y) extrudado ao longo de Z (de z0 a z1).
static func _prism_z(b, slot: String, poly_xy: PackedVector2Array, z0: float, z1: float, caps: bool = true) -> void:
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, 1), Vector3(0, 1, 0)), Vector3.ZERO)
	b.extrude(slot, poly_xy, z0, z1, caps, caps, true)
	b.pop()


## Tubo (cilindro octogonal) entre dois pontos quaisquer.
static func _pipe(b, slot: String, from: Vector3, to: Vector3, r: float, caps: bool = true, sides: int = 8) -> void:
	var d := to - from
	var length := d.length()
	if length < 1e-4:
		return
	var y := d / length
	var ref := Vector3.UP if absf(y.dot(Vector3.UP)) < 0.95 else Vector3.RIGHT
	var x := ref.cross(y).normalized()
	var z := x.cross(y).normalized()
	b.push(Transform3D(Basis(x, y, z), from))
	b.cylinder(slot, Vector3.ZERO, r, length, sides, caps, caps, 22.5)
	b.pop()


## Flange (anel curto mais largo) centrado em `p`, eixo `axis`.
static func _flange(b, slot: String, p: Vector3, axis: Vector3, r: float) -> void:
	var a := axis.normalized() * 0.06
	_pipe(b, slot, p - a, p + a, r)


## Janela no referencial local de fachada: parede no plano z = 0, exterior em -Z.
static func _window(b, cx: float, y0: float, w: float, h: float) -> void:
	var f := 0.12
	var dep := 0.12
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad("glass_dark", Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box("trim_white", Vector3(x0 - f, y1, -dep), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -dep), Vector3(x0, y1, 0.0), sk)
	b.box("trim_white", Vector3(x1, y0, -dep), Vector3(x1 + f, y1, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f - 0.05, y0 - 0.1, -dep - 0.06), Vector3(x1 + f + 0.05, y0, 0.0), sk)


## Veneziana de ventilacao (referencial de fachada).
static func _vent(b, cx: float, y0: float, w: float, h: float) -> void:
	var f := 0.1
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad("glass_dark", Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box("trim_white", Vector3(x0 - f, y1, -0.1), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0 - f, -0.1), Vector3(x1 + f, y0, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -0.1), Vector3(x0, y1, 0.0), sk)
	b.box("trim_white", Vector3(x1, y0, -0.1), Vector3(x1 + f, y1, 0.0), sk)
	for i in 2:
		var yy := y0 + h * (float(i) + 1.0) / 3.0
		b.box("trim_white", Vector3(x0, yy - 0.035, -0.08), Vector3(x1, yy + 0.035, -0.02), sk)


## Porta dupla metalica (fechada) com moldura, referencial de fachada.
static func _double_door(b, y0: float) -> void:
	var hw := DOOR_W * 0.5
	var y1 := y0 + DOOR_H
	var f := 0.14
	var dep := 0.13
	var sk := PackedStringArray(["+z"])
	# Folhas azuis (codigo de cor da agua) com junta central escura e puxadores.
	b.box("intake_pipe", Vector3(-hw, y0, -0.05), Vector3(hw, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("metal_dark", Vector3(-0.035, y0, -0.08), Vector3(0.035, y1, -0.05), PackedStringArray(["+z", "-y"]))
	for sx in [-1.0, 1.0]:
		var px: float = sx * 0.18
		b.box("metal_dark", Vector3(px - 0.03, y0 + 0.95, -0.13), Vector3(px + 0.03, y0 + 1.45, -0.05), sk)
	b.box("trim_white", Vector3(-hw - f, y1, -dep), Vector3(hw + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(-hw - f, y0, -dep), Vector3(-hw, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(hw, y0, -dep), Vector3(hw + f, y1, 0.0), PackedStringArray(["+z", "-y"]))
	# Pingadeira/cobertura curta sobre a porta.
	b.box("trim_white", Vector3(-hw - 0.35, y1 + 0.28, -0.6), Vector3(hw + 0.35, y1 + 0.38, 0.0), sk)


## Parede de ala com topo inclinado: eixo de `a` a `c` (x, z), espessura t, alturas h0 -> h1.
static func _wing(b, a: Vector2, c: Vector2, t: float, h0: float, h1: float) -> void:
	var d := (c - a).normalized()
	var p := Vector2(-d.y, d.x) * (t * 0.5)
	var a0 := Vector3(a.x + p.x, 0.0, a.y + p.y)
	var a1 := Vector3(a.x - p.x, 0.0, a.y - p.y)
	var c0 := Vector3(c.x + p.x, 0.0, c.y + p.y)
	var c1 := Vector3(c.x - p.x, 0.0, c.y - p.y)
	var up0 := Vector3(0, h0, 0)
	var up1 := Vector3(0, h1, 0)
	b.quad("intake_concrete", a0, c0, c0 + up1, a0 + up0, Vector3(p.x, 0, p.y))
	b.quad("intake_concrete", a1, c1, c1 + up1, a1 + up0, Vector3(-p.x, 0, -p.y))
	b.quad("intake_concrete", c0, c1, c1 + up1, c0 + up1, Vector3(d.x, 0, d.y))
	b.quad("intake_concrete", a0 + up0, c0 + up1, c1 + up1, a1 + up0, Vector3.UP)


## Selas de concreto sob um tubo horizontal (eixo em X ou Z).
static func _saddle(b, center_xz: Vector2, along_x: bool, top_y: float) -> void:
	var hw := 0.38
	var ht := 0.2
	if along_x:
		b.box("intake_concrete", Vector3(center_xz.x - ht, 0.0, center_xz.y - hw), Vector3(center_xz.x + ht, top_y, center_xz.y + hw), PackedStringArray(["-y"]))
	else:
		b.box("intake_concrete", Vector3(center_xz.x - hw, 0.0, center_xz.y - ht), Vector3(center_xz.x + hw, top_y, center_xz.y + ht), PackedStringArray(["-y"]))


func _pump_house(b) -> void:
	var zc := (PH_Z0 + PH_Z1) * 0.5
	# Plinto e paredes (quinas chanfradas).
	b.extrude("concrete_dark", _chamfer_rect(-PH_X - PLINTH_OUT, PH_Z0 - PLINTH_OUT, PH_X + PLINTH_OUT, PH_Z1 + PLINTH_OUT, CHAMFER + 0.03), 0.0, PLINTH_H, true, false)
	b.extrude("intake_concrete", _chamfer_rect(-PH_X, PH_Z0, PH_X, PH_Z1, CHAMFER), PLINTH_H, WALL_TOP, false, false)
	# Empenas (frente e fundos) sob o telhado.
	b.face("intake_concrete", [Vector3(-PH_X, WALL_TOP, PH_Z0), Vector3(PH_X, WALL_TOP, PH_Z0), Vector3(0.0, _low(0.0), PH_Z0)], Vector3(0, 0, -1))
	b.face("intake_concrete", [Vector3(-PH_X, WALL_TOP, PH_Z1), Vector3(PH_X, WALL_TOP, PH_Z1), Vector3(0.0, _low(0.0), PH_Z1)], Vector3(0, 0, 1))
	# Telhado metalico de duas aguas (cumeeira em Z: empena voltada para a rua).
	var xe := PH_X + EAVE
	var z0 := PH_Z0 - GABLE_OVER
	var z1 := PH_Z1 + GABLE_OVER
	var roof := PackedVector2Array([
		Vector2(-xe, _low(xe)), Vector2(-xe, _top(xe)), Vector2(0.0, _top(0.0)),
		Vector2(xe, _top(xe)), Vector2(xe, _low(xe)), Vector2(0.0, _low(0.0)),
	])
	_prism_z(b, "intake_roof", roof, z0, z1)
	# Nervuras da telha (descem da cumeeira ao beiral).
	var n := int(floor((z1 - z0 - 0.3) / RIB_STEP))
	var start := (z0 + z1) * 0.5 - RIB_STEP * float(n - 1) * 0.5
	for k in n:
		var z := start + RIB_STEP * float(k)
		for s in [-1.0, 1.0]:
			var sx: float = s
			b.beam("intake_roof", Vector3(sx * 0.16, _top(0.16) + 0.015, z), Vector3(sx * (xe - 0.03), _top(xe - 0.03) + 0.015, z), 0.07)
	# Cumeeira (capa losangular).
	b.push_basis(Basis(Vector3(0, 0, 1), deg_to_rad(45.0)), Vector3(0.0, _top(0.0) - 0.02, 0.0))
	b.box("intake_roof", Vector3(-0.13, -0.13, z0), Vector3(0.13, 0.13, z1))
	b.pop()
	# Testeiras claras nas empenas e nos beirais (contorno legivel no isometrico).
	var board := PackedVector2Array([
		Vector2(-xe - 0.02, _low(xe) - 0.06), Vector2(-xe - 0.02, _top(xe) + 0.07),
		Vector2(0.0, _top(0.0) + 0.07),
		Vector2(xe + 0.02, _top(xe) + 0.07), Vector2(xe + 0.02, _low(xe) - 0.06),
		Vector2(0.0, _low(0.0) - 0.06),
	])
	_prism_z(b, "trim_white", board, z0 - 0.05, z0)
	_prism_z(b, "trim_white", board, z1, z1 + 0.05)
	for sx in [-1.0, 1.0]:
		var xa: float = sx * xe
		var xb: float = sx * (xe + 0.04)
		b.box("trim_white", Vector3(minf(xa, xb), _low(xe) - 0.07, z0), Vector3(maxf(xa, xb), _top(xe) + 0.02, z1), PackedStringArray(["-x" if sx > 0 else "+x"]))

	# Fachada -Z: porta dupla central + veneziana na empena.
	b.push_translate(Vector3(0, 0, PH_Z0))
	_double_door(b, PLINTH_H)
	_vent(b, 0.0, 3.55, 0.9, 0.5)
	b.pop()
	# Patamar diante da porta.
	b.box("concrete_dark", Vector3(-DOOR_W * 0.5 - 0.45, 0.0, PH_Z0 - PLINTH_OUT - LANDING_D), Vector3(DOOR_W * 0.5 + 0.45, 0.15, PH_Z0 - PLINTH_OUT), PackedStringArray(["-y", "+z"]))
	# Lateral +X (local x = +Z do mundo): janelas altas (a adutora sai abaixo, no centro).
	b.push_rotate_y(-90.0, Vector3(PH_X, 0, zc))
	_window(b, -1.45, 2.05, 1.2, 0.65)
	_window(b, 1.45, 2.05, 1.2, 0.65)
	b.pop()
	# Lateral -X (local x = -Z do mundo).
	b.push_rotate_y(90.0, Vector3(-PH_X, 0, zc))
	_window(b, -1.45, 2.05, 1.2, 0.65)
	_window(b, 1.45, 2.05, 1.2, 0.65)
	b.pop()
	# Fundos: veneziana alta na empena (os tubos de succao entram abaixo).
	b.push_rotate_y(180.0, Vector3(0, 0, PH_Z1))
	_vent(b, 0.0, 3.55, 0.9, 0.5)
	b.pop()


func _suction_lines(b) -> void:
	# Base de concreto (laje baixa) sob as linhas de succao, da casa ate a tomada.
	b.box("concrete_dark", Vector3(-2.5, 0.0, PH_Z1 + PLINTH_OUT), Vector3(2.5, 0.1, HW_Z0), PackedStringArray(["-y"]))
	for xv in SUCTION_XS:
		var x: float = xv
		_pipe(b, "intake_pipe", Vector3(x, SUCTION_Y, PH_Z1 - 0.05), Vector3(x, SUCTION_Y, HW_Z0 + 0.05), SUCTION_R, false)
		_flange(b, "intake_pipe", Vector3(x, SUCTION_Y, PH_Z1 + 0.08), Vector3.BACK, SUCTION_R + 0.09)
		_flange(b, "intake_pipe", Vector3(x, SUCTION_Y, 1.5), Vector3.BACK, SUCTION_R + 0.07)
		_flange(b, "intake_pipe", Vector3(x, SUCTION_Y, HW_Z0 - 0.08), Vector3.BACK, SUCTION_R + 0.09)
		for zs in [0.0, 3.1]:
			_saddle(b, Vector2(x, zs), false, SUCTION_Y - SUCTION_R + 0.12)


func _discharge_line(b) -> void:
	# Adutora de recalque: sai pela lateral +X, registro de gaveta, desce na caixa de registro.
	_pipe(b, "intake_pipe", Vector3(PH_X - 0.05, OUT_Y, OUT_Z), Vector3(OUT_DROP_X + OUT_R, OUT_Y, OUT_Z), OUT_R, false)
	_pipe(b, "intake_pipe", Vector3(OUT_DROP_X, OUT_Y + OUT_R, OUT_Z), Vector3(OUT_DROP_X, 0.4, OUT_Z), OUT_R, true)
	_flange(b, "intake_pipe", Vector3(PH_X + 0.08, OUT_Y, OUT_Z), Vector3.RIGHT, OUT_R + 0.09)
	# Registro: corpo + haste + volante.
	_pipe(b, "intake_pipe", Vector3(VALVE_X - 0.18, OUT_Y, OUT_Z), Vector3(VALVE_X + 0.18, OUT_Y, OUT_Z), OUT_R + 0.1)
	_pipe(b, "metal_dark", Vector3(VALVE_X, OUT_Y + OUT_R + 0.05, OUT_Z), Vector3(VALVE_X, OUT_Y + 0.75, OUT_Z), 0.05, false, 6)
	b.cylinder("metal_dark", Vector3(VALVE_X, OUT_Y + 0.75, OUT_Z), 0.3, 0.06, 8, true, true, 22.5)
	_saddle(b, Vector2(5.25, OUT_Z), true, OUT_Y - OUT_R + 0.1)
	# Caixa de registro (concreto) na borda do lote + colar em torno do tubo.
	b.bevel_box("intake_concrete", Vector3(OUT_DROP_X - 0.75, 0.0, OUT_Z - 0.75), Vector3(LOT.x * 0.5 - 0.05, 0.42, OUT_Z + 0.75), 0.05, false)
	_pipe(b, "intake_pipe", Vector3(OUT_DROP_X, 0.42, OUT_Z), Vector3(OUT_DROP_X, 0.54, OUT_Z), OUT_R + 0.09)


func _headwork(b) -> void:
	# Caixa da tomada d'agua (concreto) com capa clara.
	b.bevel_box("intake_concrete", Vector3(-HW_X, 0.0, HW_Z0), Vector3(HW_X, HW_H, HW_Z1), 0.08, false)
	# Faixa azul (codigo de cor da agua potavel) logo abaixo da capa: liga a tomada a casa de bombas.
	b.extrude("intake_pipe", _chamfer_rect(-HW_X - 0.03, HW_Z0 - 0.03, HW_X + 0.03, HW_Z1 + 0.03, 0.09), 1.5, 1.8, false, false)
	# Capa clara so na borda (moldura) + laje de tampas escura: o topo nao vira um bloco branco.
	var o := 0.04
	var w := 0.22
	var y1 := HW_H + 0.08
	var sk := PackedStringArray(["-y"])
	b.box("trim_white", Vector3(-HW_X - o, HW_H, HW_Z0 - o), Vector3(HW_X + o, y1, HW_Z0 + w), sk)
	b.box("trim_white", Vector3(-HW_X - o, HW_H, HW_Z1 - w), Vector3(HW_X + o, y1, HW_Z1 + o), sk)
	b.box("trim_white", Vector3(-HW_X - o, HW_H, HW_Z0 + w), Vector3(-HW_X + w, y1, HW_Z1 - w), PackedStringArray(["-y", "-z", "+z"]))
	b.box("trim_white", Vector3(HW_X - w, HW_H, HW_Z0 + w), Vector3(HW_X + o, y1, HW_Z1 - w), PackedStringArray(["-y", "-z", "+z"]))
	b.quad("concrete_dark", Vector3(-HW_X + w, HW_H + 0.03, HW_Z0 + w), Vector3(HW_X - w, HW_H + 0.03, HW_Z0 + w), Vector3(HW_X - w, HW_H + 0.03, HW_Z1 - w), Vector3(-HW_X + w, HW_H + 0.03, HW_Z1 - w), Vector3.UP)
	# Aberturas gradeadas na face +Z (voltada ao rio/lago).
	for xv in SUCTION_XS:
		var cx: float = xv
		b.quad("glass_dark", Vector3(cx - 0.75, 0.25, HW_Z1 + 0.01), Vector3(cx + 0.75, 0.25, HW_Z1 + 0.01), Vector3(cx + 0.75, 1.4, HW_Z1 + 0.01), Vector3(cx - 0.75, 1.4, HW_Z1 + 0.01), Vector3(0, 0, 1))
		for k in 6:
			var bx: float = cx - 0.625 + 0.25 * float(k)
			b.box("metal_dark", Vector3(bx - 0.035, 0.25, HW_Z1 + 0.01), Vector3(bx + 0.035, 1.4, HW_Z1 + 0.07), PackedStringArray(["-z", "-y"]))
		b.box("metal_dark", Vector3(cx - 0.8, 1.4, HW_Z1 + 0.01), Vector3(cx + 0.8, 1.5, HW_Z1 + 0.08), PackedStringArray(["-z"]))
	# Guias das grades acima do deck (grade icada: leitura de "tomada com grade" de longe).
	var gz := HW_Z1 - 0.75
	var top := HW_H + 0.08
	for xv in SUCTION_XS:
		var cx: float = xv
		for sx in [-1.0, 1.0]:
			var px: float = cx + sx * 0.8
			b.box("metal_dark", Vector3(px - 0.07, top, gz - 0.12), Vector3(px + 0.07, top + 1.25, gz + 0.12), PackedStringArray(["-y"]))
		b.box("metal_dark", Vector3(cx - 0.87, top + 1.25, gz - 0.12), Vector3(cx + 0.87, top + 1.37, gz + 0.12))
		for k in 5:
			var bx: float = cx - 0.5 + 0.25 * float(k)
			b.box("metal_dark", Vector3(bx - 0.03, top, gz - 0.03), Vector3(bx + 0.03, top + 0.9, gz + 0.03), PackedStringArray(["-y"]))
		b.box("metal_dark", Vector3(cx - 0.73, top + 0.9, gz - 0.04), Vector3(cx + 0.73, top + 0.98, gz + 0.04))
	# Alas curtas de cabeceira (muros de margem, so na borda +Z) + enrocamento de protecao
	# da margem (blocos de pedra) completando a borda do lado do rio/lago.
	for s in [-1.0, 1.0]:
		var sx: float = s
		_wing(b, Vector2(sx * (HW_X - 0.3), HW_Z1 - 0.3), Vector2(sx * WING_TIP.x, WING_TIP.y), WING_T, WING_ROOT_H, WING_TIP_H)
		var seed_base := 11 if sx > 0.0 else 37
		for i in RIPRAP.size():
			var r: Array = RIPRAP[i]
			var rad: float = r[2]
			# Centro alto o bastante para nada ficar abaixo do solo (y >= 0 no envelope).
			b.icosphere("concrete_dark", Vector3(sx * float(r[0]), rad * 0.62 * 1.17, float(r[1])), rad, 0, 0.16, seed_base + i, Vector3(1.0, 0.62, 0.88))


func build(kit) -> void:
	var b = kit.builder()
	_pump_house(b)
	_suction_lines(b)
	_discharge_line(b)
	_headwork(b)
	print("[water_intake] triangulos Body no builder: ", b.triangle_count())
	var mesh = kit.save_mesh(b, FAMILY_ID)

	var root = kit.new_root("WaterIntake", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(0.0, 0.0, PH_Z0 - PLINTH_OUT - LANDING_D - 0.4), Vector3.FORWARD,
		"entrada de pedestre candidata diante da porta dupla da casa de bombas; acesso real e da integracao")
	kit.add_marker(root, "Candidate_WaterSource", Vector3(0.0, 0.0, LOT.y * 0.5), Vector3.BACK,
		"tomada d'agua candidata na borda +Z do lote (lado candidato voltado ao rio/lago do terreno); fonte real e da simulacao")
	kit.add_marker(root, "Candidate_PipeOutlet", Vector3(LOT.x * 0.5, 0.0, OUT_Z), Vector3.RIGHT,
		"saida candidata da adutora de agua bruta na borda +X (o tubo desce para o solo na caixa de registro, eixo y = 1,0 antes da descida); rede real e da integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Captacao de agua",
		"lot": LOT,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"casa_bombas_x_z_m": [PH_X * 2.0, PH_Z1 - PH_Z0],
			"casa_bombas_parede_m": WALL_TOP - PLINTH_H,
			"casa_bombas_cumeeira_m": snappedf(_top(0.0) + 0.18, 0.01),
			"porta_dupla_l_x_a_m": [DOOR_W, DOOR_H],
			"tomada_dagua_x_z_a_m": [HW_X * 2.0, HW_Z1 - HW_Z0, HW_H + 0.08],
			"guias_grade_topo_m": HW_H + 0.08 + 1.37,
			"alas_cabeceira_ate_x_m": WING_TIP.x,
			"succao_diametro_m": SUCTION_R * 2.0,
			"adutora_diametro_m": OUT_R * 2.0,
		},
		"parts": {
			"Body": "malha unica (7 superficies = slots): intake_concrete (paredes, tomada d'agua, alas, selas, caixa de registro), intake_roof (telhado metalico azul, nervuras, cumeeira), intake_pipe (succao, adutora, flanges, corpo do registro, folhas da porta dupla, faixa azul da tomada), trim_white (testeiras, molduras, capa da tomada), concrete_dark (plinto, patamar, laje sob os tubos), metal_dark (junta e puxadores da porta, grades, guias, volante do registro), glass_dark (janelas, venezianas, aberturas da tomada)",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta (x = 0, junto a borda -Z), forward -Z",
			"Candidate_WaterSource": "Marker3D candidato na tomada d'agua, borda +Z do lote, forward +Z (lado do rio/lago)",
			"Candidate_PipeOutlet": "Marker3D candidato onde a adutora sai do lote, borda +X, forward +X",
		},
		"notes": [
			"Pivo no centro do lote 14 x 16 m (solo). Casa de bombas centrada em X, de z = -6,6 a -1,6; tomada d'agua de z = 4,6 a 7,9 com alas ate x = +-6,6; caixa de registro ate x = 6,95.",
			"Lado +Z CANDIDATO voltado ao rio/lago: as aberturas gradeadas da tomada ficam na face +Z. Nao ha agua modelada; o terreno fornece a margem. Margem/cota/talude reais sao compatibilizacao pendente com o terreno.",
			"Somente captacao: sem tanques, decantadores, filtros ou reservatorio (tratamento e a familia utility.water_treatment.a).",
			"Telhado metalico azul + tubos azuis + faixa azul na tomada + concreto claro = codigo de cor da agua potavel (esgoto usa ocre/oliva/castanho).",
			"Icone: mostra uma lamina de agua (rio/lago) atras da tomada SO para leitura de catalogo; a cena nao modela agua. Cor lida de water_clean (familia utility.water_treatment.a).",
			"Sem estado de operacao no visual (sem bombas girando, luzes ou agua corrente): nada a ligar pelo host nesta familia.",
			"Porta sempre fechada; detalhes desenhados nao sao acessos funcionais (somente os Marker3D candidatos).",
		],
	})


## Icone simplificado (make_v1_icons.gd): casa de bombas com telhado e porta azuis, tubos azuis
## e tomada d'agua com grades e faixa azul. SO no icone aparece uma lamina de agua (rio/lago do
## terreno) atras da tomada, para quem nao le entender "pega agua do rio"; a cena NAO modela
## agua. A cor vem de `water_clean` (material da familia utility.water_treatment.a).
func draw_icon(ic) -> void:
	var wall: String = ic.c("intake_concrete")
	var roof: String = ic.c("intake_roof")
	var pipe: String = ic.c("intake_pipe")
	var white: String = ic.c("trim_white")
	var dark: String = ic.c("metal_dark")
	var base: String = ic.c("concrete_dark")
	var water: String = ic.c("water_clean")
	# Agua do rio/lago (so icone) e laje sob os tubos: sempre atras de tudo.
	ic.part(-200.0)
	ic.quad([Vector3(-4.6, 0.0, 10.4), Vector3(4.6, 0.0, 10.4), Vector3(4.6, 0.0, 7.9), Vector3(-4.6, 0.0, 7.9)], water)
	ic.part(-100.0)
	ic.box(Vector3(-2.5, 0.0, -1.6), Vector3(2.5, 0.1, 4.6), base)
	# Tomada d'agua (caixa) com faixa azul + grades.
	ic.part()
	ic.box(Vector3(-3.0, 0.0, 4.6), Vector3(3.0, 1.98, 7.9), wall, white)
	ic.facade_rect(-3.0, 3.0, 1.5, 1.8, 4.58, pipe)
	ic.side_rect(4.6, 7.9, 1.5, 1.8, 3.02, pipe)
	for cx in [-1.4, 1.4]:
		ic.part(0.5)
		ic.box(Vector3(cx - 0.9, 1.98, 6.95), Vector3(cx + 0.9, 3.4, 7.25), dark)
	# Tubos de succao (quadrados no icone).
	for cx in [-1.4, 1.4]:
		ic.part()
		ic.box(Vector3(cx - 0.34, 0.55, -1.6), Vector3(cx + 0.34, 1.23, 4.6), pipe)
	# Casa de bombas: paredes claras, porta dupla azul com moldura, telhado azul.
	ic.part()
	ic.box(Vector3(-3.0, 0.0, -6.6), Vector3(3.0, 3.3, -1.6), wall)
	ic.facade_rect(-1.15, 1.15, 0.3, 2.85, -6.62, white)
	ic.facade_rect(-1.0, 1.0, 0.3, 2.7, -6.63, pipe)
	ic.window_side(-5.55, 2.4, 1.2, 0.65, 3.02)
	ic.window_side(-2.65, 2.4, 1.2, 0.65, 3.02)
	ic.part(0.2)
	ic.gable(Vector3(-3.35, 3.15, -6.9), Vector3(3.35, 3.15, -1.3), 4.8, "z", roof, wall)
	# Adutora pela lateral +X ate a caixa de registro.
	ic.part()
	ic.box(Vector3(3.0, 0.7, -4.4), Vector3(6.3, 1.3, -3.8), pipe)
	ic.part()
	ic.box(Vector3(5.6, 0.0, -4.85), Vector3(6.95, 0.45, -3.35), wall)
