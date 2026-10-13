## Receita original: estacao de tratamento de agua potavel (utility.water_treatment.a) —
## amostra V1 da segunda onda, revisao humana PENDENTE. Somente visual: nao define vazao,
## capacidade, custo, equipe, acesso, qualidade da agua nem regra de tratamento.
##
## Composicao (fluxo de leitura): agua bruta chega pelos FUNDOS (+Z) num "T" azul que
## alimenta 2 decantadores circulares abertos (anel de concreto + canaleta periferica + poco
## central + coluna central + passarela fixa) -> bloco de filtros retangulares abertos em fileira (com calhas) -> coletor azul na
## frente dos filtros -> reservatorio de agua tratada (cilindro fechado claro com faixa azul)
## -> saida pela lateral -X. Casa de controle/quimica com porta voltada a -Z no centro da
## frente do lote. Tanques VAZIOS no Body; as laminas de agua limpa ficam em State_Water
## (oculto por padrao: o host liga com estado real de operacao).
##
## Codigo de cor da cadeia de agua potavel: concreto claro/branco + AZUL (tubos, faixa do
## reservatorio, porta, faixa da casa). Ocre/oliva/castanho ficam reservados ao esgoto.
extends RefCounted

const FAMILY_ID := "utility.water_treatment.a"
const MATERIAL_PREFIX := "water_"
const MATERIALS := {
	"water_concrete": {"albedo": "#c9cdc8", "roughness": 0.9},
	"water_pipe": {"albedo": "#2f6fa8", "roughness": 0.45, "metallic": 0.25},
	# Lamina de agua limpa (SOMENTE em State_Water). Nao emissiva.
	"water_clean": {"albedo": "#5aa9d6", "roughness": 0.12, "metallic": 0.05},
}

# --- Lote logico do host (X = largura, Z = profundidade); pivo no centro ------------------
const LOT := Vector2(24.0, 26.0)


# --- Decantadores circulares (medidas CANDIDATAS, metros) --------------------------------
const CL_CENTERS := [Vector2(-7.2, 8.4), Vector2(7.2, 8.4)]
const CL_R := 3.9            # raio externo da parede
const CL_T := 0.3
const CL_H := 1.8            # topo da borda acima do solo
const CL_RIM_H := 0.12
const CL_LIP := 0.05
const CL_FLOOR := 0.25
const CL_WATER := 1.55
const CL_SIDES := 16
const CL_PHASE := 11.25      # faces planas voltadas a +-X / +-Z (tubos entram em faces planas)
## Canaleta periferica (vertedor) presa por dentro da parede: aneis concentricos legiveis.
const LA_W := 0.5            # largura da canaleta (da parede para dentro)
const LA_T := 0.12           # espessura da parede interna da canaleta
const LA_Y0 := 1.05          # fundo da canaleta
const LA_Y1 := 1.65          # topo (acima da lamina de agua do estado)
## Poco de alimentacao central (anteparo cilindrico sob a plataforma).
const FW_R := 1.15
const FW_T := 0.12
const FW_Y0 := 0.85
const FW_Y1 := 1.72
const FW_SIDES := 12

# --- Filtros (bloco retangular aberto, celulas em fileira ao longo de X) ------------------
const FB_X0 := -6.9
const FB_X1 := 6.9
const FB_Z0 := -2.2
const FB_Z1 := 2.6
const FB_H := 1.5
const FB_T := 0.35
const FB_N := 4
const FB_FLOOR := 0.25
const FB_WATER := 1.15

# --- Reservatorio de agua tratada (cilindro fechado) -------------------------------------
const RES_C := Vector2(-6.8, -7.4)
const RES_R := 3.6
const RES_H := 5.0
const RES_ROOF := 0.7
const RES_BAND := Vector2(3.8, 4.5)
const RES_SIDES := 16

# --- Casa de controle/quimica ------------------------------------------------------------
const CT_X0 := -2.4
const CT_X1 := 3.6
const CT_Z0 := -11.8
const CT_Z1 := -7.4
const CT_TOP := 3.6
const CT_PARAPET := 4.05
const WG_X0 := 3.5           # ala da quimica, mais baixa (comeca dentro da casa: sem fresta)
const WG_X1 := 8.8
const WG_Z0 := -11.4
const WG_Z1 := -7.8
const WG_TOP := 2.9
const WG_PARAPET := 3.25
const PLINTH_H := 0.3
const CHAMFER := 0.08
const DOOR_W := 1.2
const DOOR_H := 2.3
const STEP_D := 0.55

# --- Tubulacao ---------------------------------------------------------------------------
const PIPE_Y := 0.6
const PIPE_R := 0.24
const RAW_R := 0.28
const RAW_Z_T := 8.4         # "T" da agua bruta entre os decantadores
const INLET_Z := 12.45       # caixa de chegada da adutora (fundos)
const HEADER_Z := -2.85      # coletor de agua filtrada na frente dos filtros
const CL_TO_FB_X := 5.4      # tubos decantador -> filtros em x = +-5,4
const OUTLET_X := -11.4      # caixa onde a saida desce para o solo (lateral -X)


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Anel (friso/cinta) entre dois poligonos XZ correspondentes.
static func _band(b, slot: String, outer: PackedVector2Array, inner: PackedVector2Array, y0: float, y1: float) -> void:
	var n := outer.size()
	for i in n:
		var j := (i + 1) % n
		var o0 := outer[i]; var o1 := outer[j]
		var i0 := inner[i]; var i1 := inner[j]
		var mid := (o0 + o1) * 0.5 - (inner[0] + inner[n / 2]) * 0.5
		b.quad(slot, Vector3(o0.x, y0, o0.y), Vector3(o1.x, y0, o1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3(mid.x, 0, mid.y))
		b.quad(slot, Vector3(i0.x, y1, i0.y), Vector3(i1.x, y1, i1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3.UP)


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


static func _flange(b, p: Vector3, axis: Vector3, r: float) -> void:
	var a := axis.normalized() * 0.06
	_pipe(b, "water_pipe", p - a, p + a, r)


## Sela de concreto sob tubo horizontal (`along_x` = eixo do tubo em X).
static func _saddle(b, x: float, z: float, along_x: bool) -> void:
	var top := PIPE_Y - PIPE_R + 0.1
	var hw := 0.34
	var ht := 0.18
	if along_x:
		b.box("water_concrete", Vector3(x - ht, 0.0, z - hw), Vector3(x + ht, top, z + hw), PackedStringArray(["-y"]))
	else:
		b.box("water_concrete", Vector3(x - hw, 0.0, z - ht), Vector3(x + hw, top, z + ht), PackedStringArray(["-y"]))


static func _ring_pts(c: Vector2, r: float, sides: int, phase: float) -> Array:
	var pts := []
	for i in sides:
		var a := deg_to_rad(phase) + TAU * float(i) / float(sides)
		pts.append(Vector2(c.x + cos(a) * r, c.y + sin(a) * r))
	return pts


## Parede anelar: face externa (y0_out..y1), interna (y0_in..y1) e topo opcional.
static func _ring(b, slot: String, c: Vector2, r_out: float, r_in: float, y0_out: float, y0_in: float, y1: float, sides: int, phase: float, top: bool) -> void:
	var o: Array = _ring_pts(c, r_out, sides, phase)
	var n: Array = _ring_pts(c, r_in, sides, phase)
	for i in sides:
		var j := (i + 1) % sides
		var o0: Vector2 = o[i]; var o1: Vector2 = o[j]
		var i0: Vector2 = n[i]; var i1: Vector2 = n[j]
		var dm := (o0 + o1) * 0.5 - c
		b.quad(slot, Vector3(o0.x, y0_out, o0.y), Vector3(o1.x, y0_out, o1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3(dm.x, 0, dm.y))
		b.quad(slot, Vector3(i0.x, y0_in, i0.y), Vector3(i1.x, y0_in, i1.y), Vector3(i1.x, y1, i1.y), Vector3(i0.x, y1, i0.y), Vector3(-dm.x, 0, -dm.y))
		if top:
			b.quad(slot, Vector3(i0.x, y1, i0.y), Vector3(i1.x, y1, i1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3.UP)


## Coroa plana (voltada para cima) entre os raios r0 < r1, na altura y.
static func _annulus(b, slot: String, c: Vector2, r0: float, r1: float, y: float, sides: int, phase: float) -> void:
	var a: Array = _ring_pts(c, r0, sides, phase)
	var o: Array = _ring_pts(c, r1, sides, phase)
	for i in sides:
		var j := (i + 1) % sides
		var a0: Vector2 = a[i]; var a1: Vector2 = a[j]
		var o0: Vector2 = o[i]; var o1: Vector2 = o[j]
		b.quad(slot, Vector3(a0.x, y, a0.y), Vector3(a1.x, y, a1.y), Vector3(o1.x, y, o1.y), Vector3(o0.x, y, o0.y), Vector3.UP)


static func _disc(b, slot: String, c: Vector2, r: float, y: float, sides: int, phase: float) -> void:
	var pts := []
	for p in _ring_pts(c, r, sides, phase):
		pts.append(Vector3(p.x, y, p.y))
	b.face(slot, pts, Vector3.UP)


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


## Volume de laje plana com platibanda, capa clara e faixa azul (casa de controle / ala).
func _flat_block(b, x0: float, z0: float, x1: float, z1: float, top: float, parapet: float) -> void:
	var po := 0.06
	b.extrude("concrete_dark", _chamfer_rect(x0 - po, z0 - po, x1 + po, z1 + po, CHAMFER + 0.03), 0.0, PLINTH_H, true, false)
	b.extrude("water_concrete", _chamfer_rect(x0, z0, x1, z1, CHAMFER), PLINTH_H, parapet, false, false)
	var pt := 0.2
	var ix0 := x0 + pt; var ix1 := x1 - pt; var iz0 := z0 + pt; var iz1 := z1 - pt
	b.quad("water_concrete", Vector3(ix0, top, iz0), Vector3(ix1, top, iz0), Vector3(ix1, parapet, iz0), Vector3(ix0, parapet, iz0), Vector3(0, 0, 1))
	b.quad("water_concrete", Vector3(ix0, top, iz1), Vector3(ix1, top, iz1), Vector3(ix1, parapet, iz1), Vector3(ix0, parapet, iz1), Vector3(0, 0, -1))
	b.quad("water_concrete", Vector3(ix0, top, iz0), Vector3(ix0, top, iz1), Vector3(ix0, parapet, iz1), Vector3(ix0, parapet, iz0), Vector3(1, 0, 0))
	b.quad("water_concrete", Vector3(ix1, top, iz0), Vector3(ix1, top, iz1), Vector3(ix1, parapet, iz1), Vector3(ix1, parapet, iz0), Vector3(-1, 0, 0))
	b.quad("concrete_dark", Vector3(ix0, top, iz0), Vector3(ix1, top, iz0), Vector3(ix1, top, iz1), Vector3(ix0, top, iz1), Vector3.UP)
	# Capa da platibanda.
	var co := 0.05
	var y1 := parapet + 0.08
	var ci := pt - 0.04
	var sk := PackedStringArray(["-y"])
	b.box("trim_white", Vector3(x0 - co, parapet, z0 - co), Vector3(x1 + co, y1, z0 + ci), sk)
	b.box("trim_white", Vector3(x0 - co, parapet, z1 - ci), Vector3(x1 + co, y1, z1 + co), sk)
	b.box("trim_white", Vector3(x0 - co, parapet, z0 + ci), Vector3(x0 + ci, y1, z1 - ci), PackedStringArray(["-y", "-z", "+z"]))
	b.box("trim_white", Vector3(x1 - ci, parapet, z0 + ci), Vector3(x1 + co, y1, z1 - ci), PackedStringArray(["-y", "-z", "+z"]))
	# Faixa azul logo abaixo da laje (codigo de cor da agua potavel).
	_band(b, "water_pipe", _chamfer_rect(x0 - 0.04, z0 - 0.04, x1 + 0.04, z1 + 0.04, CHAMFER + 0.02),
		_chamfer_rect(x0, z0, x1, z1, CHAMFER), top - 0.45, top - 0.1)


func _control_house(b) -> void:
	_flat_block(b, CT_X0, CT_Z0, CT_X1, CT_Z1, CT_TOP, CT_PARAPET)
	_flat_block(b, WG_X0, WG_Z0, WG_X1, WG_Z1, WG_TOP, WG_PARAPET)
	# Porta (fechada) centrada em x = 0 na fachada -Z, com moldura e pingadeira.
	b.push_translate(Vector3(0, 0, CT_Z0))
	var hw := DOOR_W * 0.5
	var y0 := PLINTH_H
	var y1 := y0 + DOOR_H
	var sk := PackedStringArray(["+z"])
	b.box("water_pipe", Vector3(-hw, y0, -0.05), Vector3(hw, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("metal_dark", Vector3(hw - 0.22, y0 + 0.95, -0.12), Vector3(hw - 0.16, y0 + 1.4, -0.05), sk)
	b.box("trim_white", Vector3(-hw - 0.13, y1, -0.13), Vector3(hw + 0.13, y1 + 0.13, 0.0), sk)
	b.box("trim_white", Vector3(-hw - 0.13, y0, -0.13), Vector3(-hw, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(hw, y0, -0.13), Vector3(hw + 0.13, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(-hw - 0.45, y1 + 0.25, -0.75), Vector3(hw + 0.45, y1 + 0.35, 0.0), sk)
	_window(b, -1.6, 1.25, 1.0, 1.15)
	_window(b, 2.2, 1.25, 1.8, 1.15)
	b.pop()
	b.box("concrete_dark", Vector3(-0.9, 0.0, CT_Z0 - 0.06 - STEP_D), Vector3(0.9, 0.15, CT_Z0 - 0.06), PackedStringArray(["-y", "+z"]))
	# Ala da quimica: janelas altas na frente e na lateral +X.
	b.push_translate(Vector3(0, 0, WG_Z0))
	_window(b, 5.0, 1.45, 1.6, 0.75)
	_window(b, 7.4, 1.45, 1.6, 0.75)
	b.pop()
	b.push_rotate_y(-90.0, Vector3(WG_X1, 0, (WG_Z0 + WG_Z1) * 0.5))
	_window(b, 0.0, 1.45, 1.4, 0.75)
	b.pop()
	# Lateral -X e fundos da casa de controle.
	b.push_rotate_y(90.0, Vector3(CT_X0, 0, (CT_Z0 + CT_Z1) * 0.5))
	_window(b, 0.0, 1.25, 1.2, 1.15)
	b.pop()
	b.push_rotate_y(180.0, Vector3(0, 0, CT_Z1))
	_window(b, -1.4, 1.25, 1.2, 1.15)
	b.pop()
	# Exaustor na laje.
	b.box("metal_dark", Vector3(0.6, CT_TOP, -10.2), Vector3(1.5, CT_TOP + 0.55, -9.3), PackedStringArray(["-y"]))
	b.box("trim_white", Vector3(0.5, CT_TOP + 0.55, -10.3), Vector3(1.6, CT_TOP + 0.65, -9.2))


func _clarifier(b, c: Vector2, bridge_dir: float) -> void:
	var r_in := CL_R - CL_T
	var yw := CL_H - CL_RIM_H
	_ring(b, "water_concrete", c, CL_R, r_in, 0.0, CL_FLOOR, yw, CL_SIDES, CL_PHASE, false)
	_ring(b, "trim_white", c, CL_R + CL_LIP, r_in, yw, yw, CL_H, CL_SIDES, CL_PHASE, true)
	_disc(b, "concrete_dark", c, r_in, CL_FLOOR, CL_SIDES, CL_PHASE)
	# Canaleta periferica: parede interna clara + fundo (aneis concentricos = "decantador").
	var r_la := r_in - LA_W
	_ring(b, "trim_white", c, r_la, r_la - LA_T, LA_Y0, LA_Y0, LA_Y1, CL_SIDES, CL_PHASE, true)
	_annulus(b, "water_concrete", c, r_la, r_in, LA_Y0, CL_SIDES, CL_PHASE)
	# Poco de alimentacao central (anteparo cilindrico fixo).
	_ring(b, "water_concrete", c, FW_R, FW_R - FW_T, FW_Y0, FW_Y0, FW_Y1, FW_SIDES, 15.0, true)
	# Coluna central + plataforma do acionamento.
	b.cylinder("water_concrete", Vector3(c.x, CL_FLOOR, c.y), 0.45, CL_H - CL_FLOOR, 8, false, false, 22.5)
	b.box("metal_dark", Vector3(c.x - 0.7, CL_H, c.y - 0.7), Vector3(c.x + 0.7, CL_H + 0.18, c.y + 0.7))
	b.box("metal_dark", Vector3(c.x - 0.32, CL_H + 0.18, c.y - 0.32), Vector3(c.x + 0.32, CL_H + 0.62, c.y + 0.32), PackedStringArray(["-y"]))
	# Passarela FIXA da borda ao centro (estrutura; nenhuma rotacao representada).
	var x_in: float = c.x + bridge_dir * 0.7
	var x_out: float = c.x + bridge_dir * (CL_R + CL_LIP + 0.25)
	var bx0 := minf(x_in, x_out)
	var bx1 := maxf(x_in, x_out)
	b.box("metal_dark", Vector3(bx0, CL_H, c.y - 0.45), Vector3(bx1, CL_H + 0.14, c.y + 0.45), PackedStringArray(["-y"]))
	var rail_y := CL_H + 1.1
	for sz in [-1.0, 1.0]:
		var z: float = c.y + sz * 0.42
		b.beam("trim_white", Vector3(bx0 + 0.05, rail_y, z), Vector3(bx1 - 0.05, rail_y, z), 0.07)
		for t in [0.06, 0.5, 0.94]:
			var px: float = lerpf(bx0 + 0.05, bx1 - 0.05, t)
			b.box("trim_white", Vector3(px - 0.035, CL_H + 0.14, z - 0.035), Vector3(px + 0.035, rail_y, z + 0.035), PackedStringArray(["-y", "+y"]))


func _filters(b) -> void:
	var sk := PackedStringArray(["-y", "+y"])
	var cap := PackedStringArray(["-y"])
	var co := 0.03
	var yc := FB_H + 0.08
	# Paredes longas (frente/fundos) e transversais.
	b.box("water_concrete", Vector3(FB_X0, 0.0, FB_Z0), Vector3(FB_X1, FB_H, FB_Z0 + FB_T), sk)
	b.box("water_concrete", Vector3(FB_X0, 0.0, FB_Z1 - FB_T), Vector3(FB_X1, FB_H, FB_Z1), sk)
	b.box("trim_white", Vector3(FB_X0 - co, FB_H, FB_Z0 - co), Vector3(FB_X1 + co, yc, FB_Z0 + FB_T + co), cap)
	b.box("trim_white", Vector3(FB_X0 - co, FB_H, FB_Z1 - FB_T - co), Vector3(FB_X1 + co, yc, FB_Z1 + co), cap)
	var cell := (FB_X1 - FB_X0 - FB_T * float(FB_N + 1)) / float(FB_N)
	for i in FB_N + 1:
		var x0 := FB_X0 + float(i) * (cell + FB_T)
		b.box("water_concrete", Vector3(x0, 0.0, FB_Z0 + FB_T), Vector3(x0 + FB_T, FB_H, FB_Z1 - FB_T), PackedStringArray(["-y", "+y", "-z", "+z"]))
		b.box("trim_white", Vector3(x0 - co, FB_H, FB_Z0 + FB_T + co), Vector3(x0 + FB_T + co, yc, FB_Z1 - FB_T - co), PackedStringArray(["-y", "-z", "+z"]))
	# Fundo (leito) das celulas.
	b.quad("concrete_dark", Vector3(FB_X0 + FB_T, FB_FLOOR, FB_Z0 + FB_T), Vector3(FB_X1 - FB_T, FB_FLOOR, FB_Z0 + FB_T), Vector3(FB_X1 - FB_T, FB_FLOOR, FB_Z1 - FB_T), Vector3(FB_X0 + FB_T, FB_FLOOR, FB_Z1 - FB_T), Vector3.UP)
	# Calhas de lavagem (uma por celula, presas nas paredes longas).
	for i in FB_N:
		var xc := FB_X0 + FB_T + float(i) * (cell + FB_T) + cell * 0.5
		b.box("trim_white", Vector3(xc - 0.18, 0.95, FB_Z0 + FB_T), Vector3(xc + 0.18, 1.3, FB_Z1 - FB_T), PackedStringArray(["-z", "+z"]))


func _reservoir(b) -> void:
	var c := RES_C
	b.cylinder("concrete_dark", Vector3(c.x, 0.0, c.y), RES_R + 0.18, 0.25, RES_SIDES, true, false, 11.25)
	b.cylinder("trim_white", Vector3(c.x, 0.25, c.y), RES_R, RES_H - 0.25, RES_SIDES, false, false, 11.25)
	# Faixa azul.
	_ring(b, "water_pipe", c, RES_R + 0.05, RES_R, RES_BAND.x, RES_BAND.y, RES_BAND.y, RES_SIDES, 11.25, true)
	# Cobertura conica + respiro.
	b.frustum("water_concrete", Vector3(c.x, RES_H, c.y), RES_R + 0.1, 0.75, RES_ROOF, RES_SIDES, true, false, 11.25)
	b.cylinder("metal_dark", Vector3(c.x, RES_H + RES_ROOF, c.y), 0.35, 0.35, 8, false, false, 22.5)
	b.cylinder("trim_white", Vector3(c.x, RES_H + RES_ROOF + 0.35, c.y), 0.55, 0.08, 8, true, true, 22.5)
	# Escada de marinheiro na face -Z (estrutura fixa).
	var zf := c.y - RES_R * cos(deg_to_rad(11.25)) - 0.22
	var ytop := RES_H + 0.55
	for sx in [-1.0, 1.0]:
		var x: float = c.x + sx * 0.25
		b.box("metal_dark", Vector3(x - 0.035, 0.25, zf - 0.035), Vector3(x + 0.035, ytop, zf + 0.035), PackedStringArray(["-y"]))
		b.box("metal_dark", Vector3(x - 0.035, ytop - 0.05, zf - 0.035), Vector3(x + 0.035, ytop, zf + 0.22), PackedStringArray(["-y"]))
	var k := 0
	var yr := 0.7
	while yr < ytop - 0.2:
		b.box("metal_dark", Vector3(c.x - 0.25, yr - 0.025, zf - 0.025), Vector3(c.x + 0.25, yr + 0.025, zf + 0.025), PackedStringArray(["-x", "+x"]))
		yr += 0.45
		k += 1


func _pipes(b) -> void:
	# (1) Agua bruta: caixa de chegada nos fundos -> "T" -> decantadores.
	b.bevel_box("water_concrete", Vector3(-0.55, 0.0, INLET_Z - 0.5), Vector3(0.55, 0.4, INLET_Z + 0.5), 0.05, false)
	_pipe(b, "water_pipe", Vector3(0.0, 0.4, INLET_Z), Vector3(0.0, PIPE_Y + RAW_R, INLET_Z), RAW_R, true)
	_flange(b, Vector3(0.0, 0.46, INLET_Z), Vector3.UP, RAW_R + 0.09)
	_pipe(b, "water_pipe", Vector3(0.0, PIPE_Y, INLET_Z + RAW_R), Vector3(0.0, PIPE_Y, RAW_Z_T), RAW_R, false)
	var xw: float = 7.2 - CL_R * cos(deg_to_rad(11.25))
	_pipe(b, "water_pipe", Vector3(-xw - 0.1, PIPE_Y, RAW_Z_T), Vector3(xw + 0.1, PIPE_Y, RAW_Z_T), RAW_R, false)
	_pipe(b, "water_pipe", Vector3(-0.42, PIPE_Y, RAW_Z_T), Vector3(0.42, PIPE_Y, RAW_Z_T), RAW_R + 0.1)
	for sx in [-1.0, 1.0]:
		var s: float = sx
		_flange(b, Vector3(s * (xw - 0.08), PIPE_Y, RAW_Z_T), Vector3.RIGHT, RAW_R + 0.09)
		_saddle(b, s * 1.9, RAW_Z_T, true)
	_saddle(b, 0.0, 10.6, false)
	# (2) Decantadores -> filtros (x = +-5,4).
	# z onde o tubo em x = +-5,4 encontra a parede externa do decantador (centro x = +-7,2).
	var dx: float = absf(CL_CENTERS[1].x) - CL_TO_FB_X
	var zc: float = CL_CENTERS[1].y - sqrt(CL_R * CL_R - dx * dx)
	for sx in [-1.0, 1.0]:
		var x: float = sx * CL_TO_FB_X
		_pipe(b, "water_pipe", Vector3(x, PIPE_Y, zc + 0.3), Vector3(x, PIPE_Y, FB_Z1 - 0.15), PIPE_R, false)
		_flange(b, Vector3(x, PIPE_Y, FB_Z1 + 0.08), Vector3.BACK, PIPE_R + 0.09)
		_saddle(b, x, (zc + FB_Z1) * 0.5, false)
	# (3) Coletor de agua filtrada na frente dos filtros -> reservatorio.
	var cell := (FB_X1 - FB_X0 - FB_T * float(FB_N + 1)) / float(FB_N)
	var x_end: float = RES_C.x
	_pipe(b, "water_pipe", Vector3(6.2, PIPE_Y, HEADER_Z), Vector3(x_end - PIPE_R, PIPE_Y, HEADER_Z), PIPE_R, true)
	for i in FB_N:
		var xc := FB_X0 + FB_T + float(i) * (cell + FB_T) + cell * 0.5
		_pipe(b, "water_pipe", Vector3(xc, PIPE_Y, FB_Z0 + 0.1), Vector3(xc, PIPE_Y, HEADER_Z), PIPE_R * 0.8, false)
		_flange(b, Vector3(xc, PIPE_Y, FB_Z0 - 0.08), Vector3.BACK, PIPE_R * 0.8 + 0.08)
	for xs in [-3.36, 3.36]:
		_saddle(b, float(xs), HEADER_Z, true)
	var z_res: float = RES_C.y + RES_R * cos(deg_to_rad(11.25))
	_pipe(b, "water_pipe", Vector3(x_end, PIPE_Y, HEADER_Z + PIPE_R), Vector3(x_end, PIPE_Y, z_res - 0.1), PIPE_R, false)
	_flange(b, Vector3(x_end, PIPE_Y, z_res + 0.08), Vector3.BACK, PIPE_R + 0.09)
	# (4) Saida de agua tratada: lateral -X, desce para o solo numa caixa de registro.
	var x_res: float = RES_C.x - RES_R * cos(deg_to_rad(11.25))
	_pipe(b, "water_pipe", Vector3(x_res + 0.1, PIPE_Y, RES_C.y), Vector3(OUTLET_X - PIPE_R, PIPE_Y, RES_C.y), PIPE_R, false)
	_pipe(b, "water_pipe", Vector3(OUTLET_X, PIPE_Y + PIPE_R, RES_C.y), Vector3(OUTLET_X, 0.4, RES_C.y), PIPE_R, true)
	_flange(b, Vector3(x_res - 0.08, PIPE_Y, RES_C.y), Vector3.RIGHT, PIPE_R + 0.09)
	b.bevel_box("water_concrete", Vector3(OUTLET_X - 0.55, 0.0, RES_C.y - 0.55), Vector3(OUTLET_X + 0.55, 0.4, RES_C.y + 0.55), 0.05, false)
	_flange(b, Vector3(OUTLET_X, 0.46, RES_C.y), Vector3.UP, PIPE_R + 0.09)


func _water_state(kit):
	var w = kit.builder()
	for cv in CL_CENTERS:
		_disc(w, "water_clean", cv, CL_R - CL_T, CL_WATER, CL_SIDES, CL_PHASE)
	var cell := (FB_X1 - FB_X0 - FB_T * float(FB_N + 1)) / float(FB_N)
	for i in FB_N:
		var x0 := FB_X0 + FB_T + float(i) * (cell + FB_T)
		w.quad("water_clean", Vector3(x0, FB_WATER, FB_Z0 + FB_T), Vector3(x0 + cell, FB_WATER, FB_Z0 + FB_T), Vector3(x0 + cell, FB_WATER, FB_Z1 - FB_T), Vector3(x0, FB_WATER, FB_Z1 - FB_T), Vector3.UP)
	return kit.save_mesh(w, FAMILY_ID + ".water")


func build(kit) -> void:
	var b = kit.builder()
	_control_house(b)
	_clarifier(b, CL_CENTERS[0], -1.0)
	_clarifier(b, CL_CENTERS[1], 1.0)
	_filters(b)
	_reservoir(b)
	_pipes(b)
	print("[water_treatment] triangulos Body no builder: ", b.triangle_count())
	var mesh = kit.save_mesh(b, FAMILY_ID)
	var water_mesh = _water_state(kit)

	var root = kit.new_root("WaterTreatment", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var st = kit.add_node(root, "State_Water")
	st.visible = false
	st.set_meta("state", "laminas de agua limpa nos decantadores e filtros; o host liga somente com operacao REAL (agua em tratamento)")
	kit.add_mesh(st, "WaterSurfaces", water_mesh)
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(0.0, 0.0, CT_Z0 - 0.06 - STEP_D - 0.45), Vector3.FORWARD,
		"entrada de pedestre candidata diante da porta da casa de controle (x = 0, borda -Z); acesso real e da integracao")
	kit.add_marker(root, "Candidate_PipeInlet", Vector3(0.0, 0.0, LOT.y * 0.5), Vector3.BACK,
		"entrada candidata de agua bruta nos FUNDOS (+Z), na caixa de chegada; rede real e da integracao")
	kit.add_marker(root, "Candidate_PipeOutlet", Vector3(-LOT.x * 0.5, 0.0, RES_C.y), Vector3.LEFT,
		"saida candidata de agua tratada na lateral -X, junto a caixa onde o tubo desce para o solo; rede real e da integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Tratamento de agua potavel",
		"lot": LOT,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"decantadores_diametro_m": CL_R * 2.0,
			"decantadores_borda_m": CL_H,
			"decantadores_canaleta_largura_m": LA_W,
			"decantadores_poco_central_diametro_m": FW_R * 2.0,
			"decantadores_centros_xz_m": [[CL_CENTERS[0].x, CL_CENTERS[0].y], [CL_CENTERS[1].x, CL_CENTERS[1].y]],
			"filtros_x_z_m": [FB_X1 - FB_X0, FB_Z1 - FB_Z0],
			"filtros_celulas": FB_N,
			"filtros_borda_m": FB_H,
			"reservatorio_diametro_m": RES_R * 2.0,
			"reservatorio_altura_parede_m": RES_H,
			"reservatorio_topo_m": RES_H + RES_ROOF + 0.43,
			"casa_controle_x_z_m": [CT_X1 - CT_X0, CT_Z1 - CT_Z0],
			"casa_controle_platibanda_m": CT_PARAPET + 0.08,
			"ala_quimica_x_z_m": [WG_X1 - WG_X0, WG_Z1 - WG_Z0],
			"porta_l_x_a_m": [DOOR_W, DOOR_H],
			"tubos_diametro_m": PIPE_R * 2.0,
			"agua_bruta_diametro_m": RAW_R * 2.0,
		},
		"parts": {
			"Body": "estrutura permanente com tanques VAZIOS (6 superficies = slots): water_concrete (paredes dos decantadores e filtros, fundo da canaleta periferica, poco de alimentacao central, coluna central, casa, cobertura do reservatorio, selas, caixas), trim_white (bordas, parede interna da canaleta periferica, capas, guarda-corpos, calhas, corpo do reservatorio, molduras), concrete_dark (fundos dos tanques, plintos, lajes, degrau), water_pipe (tubos, flanges, faixa do reservatorio, faixa da casa, porta), metal_dark (passarelas, acionamentos, escada, exaustor), glass_dark (janelas)",
			"State_Water": "Node3D OCULTO por padrao: laminas de agua limpa (water_clean) nos 2 decantadores e nas 4 celulas de filtro (malha utility.water_treatment.a.water); o host liga somente com operacao real",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta (x = 0, junto a borda -Z), forward -Z",
			"Candidate_PipeInlet": "Marker3D candidato da entrada de agua bruta nos fundos (+Z), forward +Z",
			"Candidate_PipeOutlet": "Marker3D candidato da saida de agua tratada na lateral -X, forward -X",
		},
		"notes": [
			"Pivo no centro do lote 24 x 26 m (solo). Decantadores nos fundos (centros x = +-7,2, z = 8,4), filtros no meio (z = -2,2 a 2,6), reservatorio a frente-esquerda (centro -6,8, -7,4), casa de controle/quimica a frente com a porta em x = 0.",
			"Fluxo de leitura: fundos (agua bruta, 'T') -> decantadores -> filtros -> coletor -> reservatorio -> saida -X. Tubos ilustrativos: nao definem rede hidraulica, vazao nem capacidade.",
			"Passarelas dos decantadores sao FIXAS (estrutura); nenhuma parte movel ou rotacao e representada. Canaleta periferica + poco central + parede externa formam aneis concentricos (leitura de decantador com ou sem agua).",
			"Somente tratamento de agua potavel: captacao e esgoto sao familias separadas (utility.water_intake.a, utility.sewage_treatment.a).",
			"Sem luzes de operacao, produtos quimicos expostos, estoque ou pessoas. Porta sempre fechada; detalhes desenhados nao sao acessos funcionais (somente os Marker3D candidatos).",
		],
	})


## Icone simplificado (make_v1_icons.gd): decantadores redondos com agua, filtros com agua,
## reservatorio branco com faixa azul, casa de controle e tubos azuis.
func draw_icon(ic) -> void:
	var conc: String = ic.c("water_concrete")
	var white: String = ic.c("trim_white")
	var pipe: String = ic.c("water_pipe")
	var water: String = ic.c("water_clean")
	var dark: String = ic.c("metal_dark")
	# Tubos no chao (desenhados antes de tudo).
	ic.part(-50.0)
	ic.box(Vector3(-0.3, 0.3, 8.1), Vector3(0.3, 0.9, 12.5), pipe)
	ic.part(-50.0)
	ic.box(Vector3(-3.4, 0.3, 8.1), Vector3(3.4, 0.9, 8.7), pipe)
	ic.part(-50.0)
	ic.box(Vector3(-6.8, 0.3, -3.1), Vector3(6.2, 0.9, -2.6), pipe)
	ic.part(-50.0)
	ic.box(Vector3(-7.1, 0.3, -3.9), Vector3(-6.5, 0.9, -2.6), pipe)
	ic.part(-50.0)
	ic.box(Vector3(-11.4, 0.3, -7.7), Vector3(-10.0, 0.9, -7.1), pipe)
	for sx in [-1.0, 1.0]:
		ic.part(-50.0)
		ic.box(Vector3(sx * 5.4 - 0.3, 0.3, 2.6), Vector3(sx * 5.4 + 0.3, 0.9, 5.0), pipe)
	# Decantadores: tambor de concreto com lamina de agua + coluna central + passarela.
	for i in 2:
		var cx: float = -7.2 if i == 0 else 7.2
		ic.part()
		ic.prism(Vector3(cx, 0.0, 8.4), 3.9, 3.9, 1.8, 12, conc, water)
		ic.part(3.0)
		ic.prism(Vector3(cx, 1.8, 8.4), 0.6, 0.6, 0.5, 6, dark)
		ic.part(2.5)
		var bx0: float = cx if i == 1 else cx - 4.1
		var bx1: float = cx + 4.1 if i == 1 else cx
		ic.box(Vector3(bx0, 1.8, 7.95), Vector3(bx1, 2.0, 8.85), dark)
	# Filtros: bloco com lamina de agua + divisorias claras.
	ic.part()
	ic.box(Vector3(-6.9, 0.0, -2.2), Vector3(6.9, 1.5, 2.6), conc, water)
	for k in 5:
		var x0: float = -6.9 + 3.3625 * k
		ic.part(2.0)
		ic.box(Vector3(x0, 1.5, -2.2), Vector3(x0 + 0.35, 1.62, 2.6), white)
	ic.part(2.0)
	ic.box(Vector3(-6.9, 1.5, -2.2), Vector3(6.9, 1.62, -1.85), white)
	ic.part(2.0)
	ic.box(Vector3(-6.9, 1.5, 2.25), Vector3(6.9, 1.62, 2.6), white)
	# Casa de controle/quimica com faixa azul e porta azul.
	ic.part()
	ic.box(Vector3(3.5, 0.0, -11.4), Vector3(8.8, 3.25, -7.8), conc, conc)
	ic.facade_rect(3.5, 8.8, 2.45, 2.8, -11.42, pipe)
	ic.window_front(5.0, 1.8, 1.6, 0.75, -11.43)
	ic.window_front(7.4, 1.8, 1.6, 0.75, -11.43)
	ic.part()
	ic.box(Vector3(-2.4, 0.0, -11.8), Vector3(3.6, 4.05, -7.4), conc, conc)
	ic.facade_rect(-2.4, 3.6, 3.15, 3.5, -11.82, pipe)
	ic.facade_rect(-0.73, 0.73, 0.3, 2.73, -11.82, white)
	ic.facade_rect(-0.6, 0.6, 0.3, 2.6, -11.83, pipe)
	ic.window_front(2.2, 1.8, 1.8, 1.15, -11.83)
	ic.side_rect(-11.4, -7.8, 2.45, 2.8, 8.82, pipe)
	# Reservatorio: cilindro branco, faixa azul, cobertura conica.
	ic.part()
	ic.prism(Vector3(-6.8, 0.0, -7.4), 3.6, 3.6, 3.8, 12, white)
	ic.part(0.2)
	ic.prism(Vector3(-6.8, 3.8, -7.4), 3.65, 3.65, 0.7, 12, pipe)
	ic.part(0.4)
	ic.prism(Vector3(-6.8, 4.5, -7.4), 3.6, 3.6, 0.5, 12, white)
	ic.part(0.6)
	ic.prism(Vector3(-6.8, 5.0, -7.4), 3.7, 1.3, 0.6, 12, conc, white)
