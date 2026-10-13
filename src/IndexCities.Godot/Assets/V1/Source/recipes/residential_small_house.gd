## Receita original: casa simples terrea unifamiliar (residential.small_house.a) — amostra V1,
## revisao humana PENDENTE. Somente visual: nao define lote, ocupacao, moradores, acesso,
## capacidade nem custo. Medidas sao CANDIDATAS para compatibilizacao com a integracao.
##
## Linguagem comum com a mercearia (commercial.grocery_small.a): plinto plinth_stone,
## quinas verticais chanfradas, janelas com vidro escuro recuado + moldura/peitoril trim_white.
extends RefCounted

const FAMILY_ID := "residential.small_house.a"
const MATERIAL_PREFIX := "house_"
const MATERIALS := {
	"house_wall": {"albedo": "#ebcb98", "roughness": 0.9},
	"house_roof": {"albedo": "#b5543a", "roughness": 0.85},
}

# --- Medidas CANDIDATAS (metros) -------------------------------------------------
const W := 8.0              # corpo em X (fachada)
const D := 7.0              # corpo em Z (profundidade)
const PLINTH_H := 0.3       # plinto / piso interno
const PLINTH_OUT := 0.06    # saliencia do plinto
const WALL_TOP := 3.4       # topo das paredes (3,1 m sobre o plinto; folga p/ cobertura da entrada)
const CHAMFER := 0.1        # chanfro das quinas verticais
const EAVE := 0.45          # beiral frontal/fundos (horizontal)
const GABLE_OVER := 0.35    # beiral nas empenas (+-X)
const SLOPE := 0.65         # inclinacao do telhado (~33 graus)
const ROOF_T := 0.16        # espessura vertical da cobertura
const TILE_ROWS := 5        # fiadas de telha sugeridas por agua
const TILE_STEP := 0.06     # degrau entre fiadas
const DOOR_X := 2.3         # porta fora do centro (lado +X da fachada)
const DOOR_W := 0.95
const DOOR_H := 2.1
const DOOR_TOP_Y := PLINTH_H + DOOR_H
const STEP_D := 0.7         # profundidade do degrau/patamar
const STEP_H := 0.15
# Cobertura da entrada (duas aguas, cumeeira em Z), sob o beiral principal.
const CANOPY_HALF := 0.85
const CANOPY_OUT := 1.05
const CANOPY_EAVE_Y := 2.72
const CANOPY_APEX_Y := 3.08
const CHIMNEY_TOP := 6.57   # topo da capa da chamine (opcional, decorativa)


static func _low(z: float) -> float:
	# Face inferior da cobertura (passa pelo topo da parede no plano das fachadas).
	return WALL_TOP + SLOPE * (D * 0.5 - absf(z))


static func _top(z: float) -> float:
	return _low(z) + ROOF_T


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Prisma de perfil (z, y) extrudado ao longo de X (de x0 a x1).
static func _prism_x(b, slot: String, poly_zy: PackedVector2Array, x0: float, x1: float, caps: bool = true) -> void:
	b.push_basis(Basis(Vector3(0, 0, 1), Vector3(1, 0, 0), Vector3(0, 1, 0)), Vector3.ZERO)
	b.extrude(slot, poly_zy, x0, x1, caps, caps, true)
	b.pop()


## Janela no referencial local de fachada: parede no plano z = 0, exterior em -Z.
static func _window(b, cx: float, y0: float, w: float, h: float, mullion: bool = true) -> void:
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
	# Peitoril saliente (mais largo e mais profundo que a moldura).
	b.box("trim_white", Vector3(x0 - f - 0.06, y0 - 0.1, -dep - 0.08), Vector3(x1 + f + 0.06, y0, 0.0), sk)
	if mullion:
		b.box("trim_white", Vector3(cx - 0.035, y0, -0.075), Vector3(cx + 0.035, y1, -0.02), sk)


## Porta frontal de madeira (fechada) com almofadas e moldura, referencial de fachada.
static func _door(b, cx: float, y0: float, w: float, h: float) -> void:
	var f := 0.12
	var dep := 0.12
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.box("wood_door", Vector3(x0, y0, -0.05), Vector3(x1, y1, 0.0), PackedStringArray(["+z", "-y"]))
	# Almofadas em relevo (leitura de "porta" no zoom de cidade).
	b.box("wood_door", Vector3(x0 + 0.14, y0 + 0.22, -0.08), Vector3(x1 - 0.14, y0 + 0.92, -0.05), sk)
	b.box("wood_door", Vector3(x0 + 0.14, y0 + 1.1, -0.08), Vector3(x1 - 0.14, y1 - 0.2, -0.05), sk)
	b.box("trim_white", Vector3(x0 - f, y1, -dep), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -dep), Vector3(x0, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(x1, y0, -dep), Vector3(x1 + f, y1, 0.0), PackedStringArray(["+z", "-y"]))


## Prisma de perfil (x, y) extrudado ao longo de Z (de z0 a z1).
static func _prism_z(b, slot: String, poly_xy: PackedVector2Array, z0: float, z1: float, caps: bool = true) -> void:
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, 1), Vector3(0, 1, 0)), Vector3.ZERO)
	b.extrude(slot, poly_xy, z0, z1, caps, caps, true)
	b.pop()


## Cobertura de entrada em duas aguas (cumeeira perpendicular a fachada) sobre a porta,
## com testeira clara em V na frente e maos-francesas. Fica SOB o beiral principal.
static func _canopy(b, cx: float) -> void:
	var half := CANOPY_HALF
	var out := CANOPY_OUT
	var yb := CANOPY_EAVE_Y      # face superior na borda lateral
	var ya := CANOPY_APEX_Y      # face superior na cumeeira
	var t := 0.09                # espessura vertical da placa
	var chevron := PackedVector2Array([
		Vector2(cx - half, yb), Vector2(cx, ya), Vector2(cx + half, yb),
		Vector2(cx + half, yb - t), Vector2(cx, ya - t), Vector2(cx - half, yb - t),
	])
	_prism_z(b, "house_roof", chevron, -out, 0.0)
	# Testeira em V na borda frontal (contorno legivel no isometrico).
	var ft := 0.05
	var board := PackedVector2Array([
		Vector2(cx - half - 0.03, yb - t - 0.04), Vector2(cx - half - 0.03, yb + 0.03), Vector2(cx, ya + 0.04),
		Vector2(cx + half + 0.03, yb + 0.03), Vector2(cx + half + 0.03, yb - t - 0.04), Vector2(cx, ya - t - 0.04),
	])
	_prism_z(b, "trim_white", board, -out - ft, -out)
	# Maos-francesas (terminam sob a placa, sem atravessar a cobertura).
	for sx in [-1.0, 1.0]:
		var x: float = cx + sx * (half - 0.14)
		var y_under: float = yb - t + (ya - yb) * (0.14 / half)
		b.beam("trim_white", Vector3(x, DOOR_TOP_Y - 0.25, 0.0), Vector3(x, y_under - 0.04, -out + 0.18), 0.07)


## Veneziana de ventilacao do sotao na empena (referencial de fachada).
static func _gable_vent(b, cx: float, y0: float, w: float, h: float) -> void:
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
	var n := 3
	for i in n:
		var yy := y0 + h * (float(i) + 0.5) / float(n)
		b.box("trim_white", Vector3(x0, yy - 0.035, -0.08), Vector3(x1, yy + 0.035, -0.02), sk)


func build(kit) -> void:
	var b = kit.builder()
	var hx := W * 0.5
	var hz := D * 0.5

	# --- Plinto e paredes (quinas verticais chanfradas) ---------------------------
	b.extrude("plinth_stone", _chamfer_rect(-hx - PLINTH_OUT, -hz - PLINTH_OUT, hx + PLINTH_OUT, hz + PLINTH_OUT, CHAMFER + 0.03), 0.0, PLINTH_H, true, false)
	b.extrude("house_wall", _chamfer_rect(-hx, -hz, hx, hz, CHAMFER), PLINTH_H, WALL_TOP, false, false)
	# Empenas triangulares (+-X) sob a cobertura.
	for sx in [-1.0, 1.0]:
		b.face("house_wall", [Vector3(sx * hx, WALL_TOP, -hz), Vector3(sx * hx, WALL_TOP, hz), Vector3(sx * hx, _low(0.0), 0.0)], Vector3(sx, 0, 0))

	# --- Cobertura ceramica de duas aguas (cumeeira em X) com fiadas de telha ----
	var ze := hz + EAVE
	var run := ze
	var rl := run / float(TILE_ROWS)
	var front: Array = [Vector2(-ze, _low(-ze))]
	for k in TILE_ROWS:
		var za := -ze + rl * k
		var zb := -ze + rl * (k + 1)
		front.append(Vector2(za, _top(za) + TILE_STEP))
		if k < TILE_ROWS - 1:
			front.append(Vector2(zb, _top(zb)))
	var poly := PackedVector2Array()
	for p in front:
		poly.append(p)
	poly.append(Vector2(0.0, _top(0.0)))
	for i in range(front.size() - 1, -1, -1):
		var p: Vector2 = front[i]
		poly.append(Vector2(-p.x, p.y))
	poly.append(Vector2(0.0, _low(0.0)))
	_prism_x(b, "house_roof", poly, -hx - GABLE_OVER, hx + GABLE_OVER)
	# Cumeeira (capa de secao losangular).
	b.push_basis(Basis(Vector3.RIGHT, deg_to_rad(45.0)), Vector3(0.0, _top(0.0) - 0.03, 0.0))
	b.box("house_roof", Vector3(-hx - GABLE_OVER, -0.12, -0.12), Vector3(hx + GABLE_OVER, 0.12, 0.12))
	b.pop()
	# Testeiras claras nas empenas (contorno do telhado legivel no isometrico).
	var bt := 0.04
	var board := PackedVector2Array([
		Vector2(-ze - 0.02, _low(-ze) - 0.05), Vector2(-ze - 0.02, _top(-ze) + 0.08),
		Vector2(0.0, _top(0.0) + 0.08),
		Vector2(ze + 0.02, _top(ze) + 0.08), Vector2(ze + 0.02, _low(ze) - 0.05),
		Vector2(0.0, _low(0.0) - 0.05),
	])
	_prism_x(b, "trim_white", board, hx + GABLE_OVER, hx + GABLE_OVER + bt)
	_prism_x(b, "trim_white", board, -hx - GABLE_OVER - bt, -hx - GABLE_OVER)
	# Testeiras (fascia) nas bordas dos beirais frontal e dos fundos.
	for sz in [-1.0, 1.0]:
		var z_in: float = sz * ze
		var z_out: float = sz * (ze + 0.04)
		b.box("trim_white", Vector3(-hx - GABLE_OVER, _low(ze) - 0.07, minf(z_in, z_out)), Vector3(hx + GABLE_OVER, _top(ze) + 0.02, maxf(z_in, z_out)), PackedStringArray(["+z" if sz < 0 else "-z"]))

	# --- Chamine (opcional, decorativa) na agua dos fundos -------------------------
	var chx := -2.3
	var chz := 1.45
	b.box("house_roof", Vector3(chx - 0.3, _low(chz + 0.3) - 0.05, chz - 0.3), Vector3(chx + 0.3, CHIMNEY_TOP - 0.12, chz + 0.3), PackedStringArray(["-y"]))
	b.box("trim_white", Vector3(chx - 0.38, CHIMNEY_TOP - 0.12, chz - 0.38), Vector3(chx + 0.38, CHIMNEY_TOP, chz + 0.38))

	# --- Fachada frontal (-Z): porta fora do centro, cobertura de entrada, 2 janelas
	b.push_translate(Vector3(0, 0, -hz))
	_door(b, DOOR_X, PLINTH_H, DOOR_W, DOOR_H)
	_canopy(b, DOOR_X)
	_window(b, -0.1, 1.2, 1.2, 1.2)
	_window(b, -2.45, 1.2, 1.2, 1.2)
	b.pop()
	# Degrau/patamar unico diante da porta.
	b.box("plinth_stone", Vector3(DOOR_X - 0.75, 0.0, -hz - PLINTH_OUT - STEP_D), Vector3(DOOR_X + 0.75, STEP_H, -hz - PLINTH_OUT), PackedStringArray(["-y", "+z"]))

	# --- Fundos (+Z): local x = -X do mundo ----------------------------------------
	b.push_rotate_y(180.0, Vector3(0, 0, hz))
	_window(b, -1.9, 1.2, 1.2, 1.2)
	_window(b, 1.9, 1.75, 0.8, 0.65, false)
	b.pop()
	# --- Lateral +X: local x = +Z do mundo ------------------------------------------
	b.push_rotate_y(-90.0, Vector3(hx, 0, 0))
	_window(b, 0.0, 1.2, 1.2, 1.2)
	_gable_vent(b, 0.0, 4.0, 0.7, 0.45)
	b.pop()
	# --- Lateral -X: local x = -Z do mundo ------------------------------------------
	b.push_rotate_y(90.0, Vector3(-hx, 0, 0))
	_window(b, 0.0, 1.2, 1.2, 1.2)
	_gable_vent(b, 0.0, 4.0, 0.7, 0.45)
	b.pop()

	print("[house] triangulos no builder: ", b.triangle_count())
	var mesh = kit.save_mesh(b, FAMILY_ID)

	var root = kit.new_root("SmallHouse", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(DOOR_X, 0.0, -hz - PLINTH_OUT - STEP_D - 0.4), Vector3.FORWARD,
		"entrada de pedestre candidata diante do degrau da porta; acesso real e da integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Casa simples terrea",
		"proposed": {
			"corpo_x_m": W,
			"corpo_z_m": D,
			"plinto_m": PLINTH_H,
			"parede_sobre_plinto_m": WALL_TOP - PLINTH_H,
			"cumeeira_m": snappedf(_top(0.0) + 0.14, 0.01),
			"beiral_frente_fundos_m": EAVE,
			"beiral_empenas_m": GABLE_OVER,
			"porta_l_x_a_m": [DOOR_W, DOOR_H],
			"degrau_m": STEP_H,
			"degrau_patamar_l_x_p_m": [1.5, STEP_D],
			"cobertura_entrada_l_x_p_m": [CANOPY_HALF * 2.0, CANOPY_OUT],
			"cobertura_entrada_cumeeira_m": CANOPY_APEX_Y,
			"chamine_topo_m": CHIMNEY_TOP,
		},
		"parts": {
			"Body": "malha unica (6 superficies = slots): house_wall (paredes e empenas), house_roof (telhado ceramico, cumeeira, cobertura da entrada, chamine), trim_white (testeiras, molduras, peitoris, veneziana, capa da chamine, maos-francesas), wood_door (porta frontal), glass_dark (vidros das janelas e fundo da veneziana), plinth_stone (plinto e degrau)",
			"Candidate_PedestrianEntry": "Marker3D candidato diante do degrau, forward -Z",
		},
		"notes": [
			"Pivo no solo, centrado no corpo 8 x 7 m. Envelope excede o corpo em -Z ate z = -4,60 (cobertura da entrada 1,05 m + testeira; degrau/patamar ate z = -4,26) e pelos beirais (0,45 m frente/fundos; 0,35 m + testeira nas empenas).",
			"Cobertura da entrada em duas aguas (cumeeira perpendicular a fachada) fica SOB o beiral principal (folga >= 0,03 m); le como \"telhadinho da porta\" no isometrico.",
			"Variantes de cor por troca de material (override de superficie) em house_wall (paredes/empenas), house_roof (telhado, cobertura da entrada, chamine), wood_door (porta) e, se desejado, trim_white (compartilhado: trocar por um material local, nao editar o compartilhado).",
			"Sem lote, cerca, jardim, garagem, moradores ou janelas acesas. Porta sempre fechada; a porta desenhada nao e acesso funcional.",
		],
	})
