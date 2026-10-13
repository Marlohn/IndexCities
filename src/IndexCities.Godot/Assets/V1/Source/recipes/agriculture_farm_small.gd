## Receita original: fazenda pequena (agriculture.farm_small.a) — segunda onda da amostra V1,
## revisao humana PENDENTE. Somente visual: nao define producao, cultura, area produtiva,
## trabalho, custo, tempo nem estoque. Medidas sao CANDIDATAS para compatibilizacao.
##
## Composicao no lote logico do host (36 x 40 m, pivo no centro, frente -Z):
## - faixa frontal: celeiro/galpao agricola de empena frontal (paredes vermelho-celeiro,
##   acabamento branco, cobertura metalica cinza-escura) com portao grande FECHADO voltado a -Z,
##   silo baixo (estrutura vazia, sem estoque) ao lado e terreiro de terra batida ligando a
##   borda frontal (acesso do host no centro) ao portao e ao campo;
## - campo em L ocupando a maior parte do lote: base de terra clara (bordas claras) com
##   canteiros de SOLO PREPARADO em fileiras ao longo de Z (estrutura permanente no Body),
##   cercado por uma cerca baixa branca de postes e travessas com vao na entrada do campo;
## - State_Crop (oculto): fileiras de plantacao verde GENERICA sobre os canteiros. So o host
##   liga, a partir de producao real. Sem cultura identificavel, tratores, animais ou caixas.
extends RefCounted

const FAMILY_ID := "agriculture.farm_small.a"
const MATERIAL_PREFIX := "farm_"
const MATERIALS := {
	"farm_barn": {"albedo": "#9c3d34", "roughness": 0.88},
	"farm_roof": {"albedo": "#55504c", "roughness": 0.7},
	"farm_soil": {"albedo": "#6e4b32", "roughness": 0.97},
	"farm_dirt": {"albedo": "#b8996f", "roughness": 0.97},
}
## Plantacao reaproveita a folhagem da arvore (paleta coerente; sem material novo).
const CROP_TOP := "tree_foliage"
const CROP_SIDE := "tree_foliage_dark"

## So para conferencia local: true liga State_Crop na cena gerada. A ENTREGA sai com false.
const DEBUG_SHOW_STATES := false

# --- Lote do host -----------------------------------------------------------------
const LOT := Vector2(36.0, 40.0)

# --- Celeiro (medidas CANDIDATAS, metros) ------------------------------------------
const BARN_CX := -11.4      # centro do corpo em X
const BARN_CZ := -11.0      # centro do corpo em Z (fachada em z = -15)
const HW := 5.0             # meia largura (corpo 10 m em X)
const HD := 4.0             # meia profundidade (corpo 8 m em Z)
const PLINTH_H := 0.3
const PLINTH_OUT := 0.06
const WALL_TOP := 4.2
const SLOPE := 0.7          # ~35 graus
const ROOF_T := 0.18
const EAVE := 0.45          # beiral lateral (+-X)
const GABLE_OVER := 0.35    # beiral nas empenas (+-Z)
const DOOR_W := 4.4
const DOOR_TOP := 3.75
const LOFT_W := 1.5
const LOFT_Y0 := 4.6
const LOFT_Y1 := 6.1

# --- Silo baixo -----------------------------------------------------------------------
const SILO_C := Vector2(-4.1, -10.2)
const SILO_R := 1.5
const SILO_TOP := 6.4
const SILO_CAP := 7.5

# --- Terreiro / acesso e campo ---------------------------------------------------------
const TRACK_HW := 1.8
const YARD_TOP := 0.05
const FIELD_Z0 := -4.4      # frente do campo principal
const FIELD_RX0 := 2.6      # bloco frontal direito do campo comeca em x = 2,6
const FIELD_RZ0 := -17.4
const FIELD_X := 17.4
const FIELD_Z1 := 19.5
const FIELD_BASE_TOP := 0.06
const FIELD_BORDER := 0.45  # borda clara (terra batida) em volta do solo preparado
const SOIL_TOP := 0.09      # piso de solo preparado (sulcos) sobre a base
const FIELD_MARGIN := 0.6   # da borda da base ate os canteiros
const BED_PITCH := 1.8
const BED_BOTTOM_W := 1.4   # base do camalhao
const BED_TOP_W := 0.5      # crista estreita: taludes ~32 graus leem nas duas diagonais
const BED_H := 0.32

# --- Cerca baixa (opcional no pedido; branca = acabamento do celeiro) -------------------
const FENCE_ON := true
const FENCE_IN := 0.22      # recuo da cerca a partir da borda da base do campo
const FENCE_H := 0.95
const POST_W := 0.16
const POST_STEP := 4.2
const RAIL_T := 0.08
const RAIL_H := 0.12
const RAIL_YS := [0.42, 0.78]
const FIELD_GATE_X0 := -2.2  # vao da cerca na entrada do campo: de x = -2,2 ate a quina interna


static func _low(x: float) -> float:
	return WALL_TOP + SLOPE * (HW - absf(x))


## Prisma de perfil (x, y) extrudado ao longo de Z (de z0 a z1).
static func _prism_z(b, slot: String, poly_xy: PackedVector2Array, z0: float, z1: float, caps: bool = true) -> void:
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, 1), Vector3(0, 1, 0)), Vector3.ZERO)
	b.extrude(slot, poly_xy, z0, z1, caps, caps, true)
	b.pop()


static func _rect(x0: float, z0: float, x1: float, z1: float) -> PackedVector2Array:
	return PackedVector2Array([Vector2(x0, z0), Vector2(x1, z0), Vector2(x1, z1), Vector2(x0, z1)])


## Travessas em X de uma folha (referencial de fachada: parede em z = 0, exterior -Z).
static func _x_leaf(b, x0: float, x1: float, y0: float, y1: float) -> void:
	var t := 0.12
	b.push_basis(Basis.IDENTITY, Vector3(0, 0, -0.12))
	var i := 0.16
	b.beam("trim_white", Vector3(x0 + i, y0 + i, 0), Vector3(x1 - i, y1 - i, 0), t)
	b.beam("trim_white", Vector3(x1 - i, y0 + i, 0), Vector3(x0 + i, y1 - i, 0), t)
	b.pop()


## Abertura com painel vermelho, moldura branca e X (portao principal ou porta do feno).
## Sempre FECHADA; e so desenho, nao acesso funcional.
static func _x_door(b, cx: float, w: float, y0: float, y1: float, leaves: int) -> void:
	var f := 0.16
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var sk := PackedStringArray(["+z"])
	b.box("farm_barn", Vector3(x0, y0, -0.06), Vector3(x1, y1, 0.0), PackedStringArray(["+z", "-y"]))
	# Moldura (a frente fica 0,02 m adiante das travessas: sem z-fighting).
	b.box("trim_white", Vector3(x0 - f, y1, -0.2), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -0.2), Vector3(x0, y1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(x1, y0, -0.2), Vector3(x1 + f, y1, 0.0), PackedStringArray(["+z", "-y"]))
	if leaves == 2:
		b.box("trim_white", Vector3(cx - 0.07, y0, -0.2), Vector3(cx + 0.07, y1, -0.06), PackedStringArray(["+z", "-y"]))
		_x_leaf(b, x0, cx - 0.07, y0, y1)
		_x_leaf(b, cx + 0.07, x1, y0, y1)
	else:
		b.box("trim_white", Vector3(x0 - f, y0 - f, -0.2), Vector3(x1 + f, y0, 0.0), sk)
		_x_leaf(b, x0, x1, y0, y1)


## Janela-veneziana fechada (moldura branca + painel escuro), referencial de fachada.
static func _shutter(b, cx: float, y0: float, w: float, h: float) -> void:
	var f := 0.13
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad("farm_roof", Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box("trim_white", Vector3(x0 - f, y1, -0.12), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f - 0.05, y0 - f, -0.16), Vector3(x1 + f + 0.05, y0, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -0.12), Vector3(x0, y1, 0.0), sk)
	b.box("trim_white", Vector3(x1, y0, -0.12), Vector3(x1 + f, y1, 0.0), sk)
	b.box("trim_white", Vector3(cx - 0.04, y0, -0.08), Vector3(cx + 0.04, y1, -0.02), sk)


## Celeiro em coordenadas locais (centro do corpo na origem, fachada em z = -HD).
static func _barn(b) -> void:
	var apex := _low(0.0)
	var xe := HW + EAVE
	var ze := HD + GABLE_OVER
	# Plinto e paredes.
	b.extrude("concrete", _rect(-HW - PLINTH_OUT, -HD - PLINTH_OUT, HW + PLINTH_OUT, HD + PLINTH_OUT), 0.0, PLINTH_H, true, false)
	b.extrude("farm_barn", _rect(-HW, -HD, HW, HD), PLINTH_H, WALL_TOP, false, false)
	for sz in [-1.0, 1.0]:
		b.face("farm_barn", [Vector3(-HW, WALL_TOP, sz * HD), Vector3(HW, WALL_TOP, sz * HD), Vector3(0.0, apex, sz * HD)], Vector3(0, 0, sz))
	# Cobertura metalica de duas aguas, cumeeira em Z (empena voltada para a frente).
	var roof := PackedVector2Array([
		Vector2(-xe, _low(xe)), Vector2(0.0, apex), Vector2(xe, _low(xe)),
		Vector2(xe, _low(xe) + ROOF_T), Vector2(0.0, apex + ROOF_T), Vector2(-xe, _low(xe) + ROOF_T),
	])
	_prism_z(b, "farm_roof", roof, -ze, ze)
	# Cumeeira (capa losangular).
	b.push_basis(Basis(Vector3.BACK, deg_to_rad(45.0)), Vector3(0.0, apex + ROOF_T - 0.03, 0.0))
	b.box("farm_roof", Vector3(-0.12, -0.12, -ze), Vector3(0.12, 0.12, ze))
	b.pop()
	# Testeiras brancas em V nas empenas e nas bordas dos beirais laterais.
	var board := PackedVector2Array([
		Vector2(-xe - 0.03, _low(xe) - 0.06), Vector2(-xe - 0.03, _low(xe) + ROOF_T + 0.06),
		Vector2(0.0, apex + ROOF_T + 0.08),
		Vector2(xe + 0.03, _low(xe) + ROOF_T + 0.06), Vector2(xe + 0.03, _low(xe) - 0.06),
		Vector2(0.0, apex - 0.06),
	])
	_prism_z(b, "trim_white", board, -ze - 0.05, -ze)
	_prism_z(b, "trim_white", board, ze, ze + 0.05)
	for sx in [-1.0, 1.0]:
		var x_in: float = sx * xe
		var x_out: float = sx * (xe + 0.05)
		b.box("trim_white", Vector3(minf(x_in, x_out), _low(xe) - 0.07, -ze), Vector3(maxf(x_in, x_out), _low(xe) + ROOF_T + 0.02, ze), PackedStringArray(["+x" if sx < 0 else "-x"]))
	# Cantoneiras brancas e faixa branca na linha das paredes (frente e fundos).
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			var cx: float = sx * HW
			var cz: float = sz * HD
			b.box("trim_white", Vector3(cx - 0.17, PLINTH_H, cz - 0.17), Vector3(cx + 0.17, WALL_TOP, cz + 0.17), PackedStringArray(["-y"]))
	for sz in [-1.0, 1.0]:
		var z_in: float = sz * HD
		var z_out: float = sz * (HD + 0.08)
		b.box("trim_white", Vector3(-HW - 0.06, WALL_TOP - 0.2, minf(z_in, z_out)), Vector3(HW + 0.06, WALL_TOP, maxf(z_in, z_out)), PackedStringArray(["+z" if sz < 0 else "-z"]))
	# Lanternim de ventilacao na cumeeira (silhueta de celeiro).
	b.box("trim_white", Vector3(-0.6, apex - 0.4, -0.6), Vector3(0.6, apex + 1.25, 0.6), PackedStringArray(["-y"]))
	for d in [Vector3(1, 0, 0), Vector3(-1, 0, 0), Vector3(0, 0, 1), Vector3(0, 0, -1)]:
		var c: Vector3 = d * 0.61 + Vector3(0, apex + 0.78, 0)
		var half: Vector3 = Vector3(0.03, 0.28, 0.36) if absf(d.x) > 0.5 else Vector3(0.36, 0.28, 0.03)
		b.box("farm_roof", c - half, c + half)
	b.gable_roof("farm_roof", Vector2(-0.82, -0.82), Vector2(0.82, 0.82), apex + 1.25, apex + 1.85, "z")

	# Fachada frontal (-Z): portao grande fechado + porta do feno na empena.
	b.push_translate(Vector3(0, 0, -HD))
	_x_door(b, 0.0, DOOR_W, PLINTH_H, DOOR_TOP, 2)
	_x_door(b, 0.0, LOFT_W, LOFT_Y0, LOFT_Y1, 1)
	b.pop()
	# Fundos (+Z): porta do feno.
	b.push_rotate_y(180.0, Vector3(0, 0, HD))
	_x_door(b, 0.0, LOFT_W, LOFT_Y0, LOFT_Y1, 1)
	b.pop()
	# Laterais: duas venezianas fechadas em cada lado.
	for sx in [-1.0, 1.0]:
		b.push_rotate_y(-90.0 * sx, Vector3(sx * HW, 0, 0))
		_shutter(b, -2.0, 1.7, 1.2, 1.0)
		_shutter(b, 2.0, 1.7, 1.2, 1.0)
		b.pop()


## Silo baixo: estrutura vazia (nao representa estoque).
static func _silo(b) -> void:
	var c := Vector3(SILO_C.x, 0.0, SILO_C.y)
	b.frustum("concrete", c, SILO_R + 0.3, SILO_R + 0.22, 0.3, 12, true, false, 15.0)
	b.cylinder("metal_light", c + Vector3(0, 0.3, 0), SILO_R, SILO_TOP - 0.3, 12, false, false, 15.0)
	# Aneis de reforco (leem como gomos de chapa no isometrico).
	for y in [2.2, 4.3]:
		b.cylinder("metal_light", c + Vector3(0, y, 0), SILO_R + 0.06, 0.14, 12, true, true, 15.0)
	b.frustum("farm_roof", c + Vector3(0, SILO_TOP, 0), SILO_R + 0.12, 0.32, SILO_CAP - SILO_TOP, 12, false, true, 15.0)
	b.cylinder("trim_white", c + Vector3(0, SILO_CAP - 0.02, 0), 0.34, 0.22, 8, true, false, 0.0)


## Terreiro de terra batida: da borda frontal (acesso no centro) ao portao e ao campo.
static func _yard(b) -> void:
	var poly := PackedVector2Array([
		Vector2(TRACK_HW, -LOT.y * 0.5 + 0.1), Vector2(TRACK_HW, FIELD_Z0), Vector2(-TRACK_HW, FIELD_Z0),
		Vector2(-TRACK_HW, BARN_CZ - HD), Vector2(BARN_CX - 2.8, BARN_CZ - HD),
		Vector2(BARN_CX - 2.8, -17.2), Vector2(BARN_CX - 1.0, -19.0), Vector2(-3.4, -19.0),
		Vector2(-2.5, -LOT.y * 0.5 + 0.1),
	])
	b.extrude("farm_dirt", poly, 0.0, YARD_TOP, true, false)


## Canteiro (camalhao) de solo preparado, secao trapezoidal ao longo de Z, sem face inferior.
static func _bed(b, cx: float, z0: float, z1: float, y0: float) -> void:
	var hb := BED_BOTTOM_W * 0.5
	var ht := BED_TOP_W * 0.5
	var y1 := y0 + BED_H
	var a0 := Vector3(cx - hb, y0, z0); var a1 := Vector3(cx - hb, y0, z1)
	var b0 := Vector3(cx - ht, y1, z0); var b1 := Vector3(cx - ht, y1, z1)
	var c0 := Vector3(cx + ht, y1, z0); var c1 := Vector3(cx + ht, y1, z1)
	var d0 := Vector3(cx + hb, y0, z0); var d1 := Vector3(cx + hb, y0, z1)
	b.quad("farm_soil", a0, a1, b1, b0, Vector3(-1, 0.6, 0))
	b.quad("farm_soil", b0, b1, c1, c0, Vector3.UP)
	b.quad("farm_soil", c0, c1, d1, d0, Vector3(1, 0.6, 0))
	b.face("farm_soil", [a0, b0, c0, d0], Vector3(0, 0, -1))
	b.face("farm_soil", [a1, b1, c1, d1], Vector3(0, 0, 1))


## Centros X e extensao Z de cada canteiro: [cx, z0, z1]. Grade unica e centrada em X;
## canteiros inteiramente a direita da quina interna vao ate a frente do bloco direito.
static func _bed_layout() -> Array:
	var out: Array = []
	var m := FIELD_MARGIN
	var span := (FIELD_X - m) * 2.0
	var n := int(floor((span - BED_BOTTOM_W) / BED_PITCH)) + 1
	var used := float(n - 1) * BED_PITCH + BED_BOTTOM_W
	var x := -FIELD_X + m + (span - used) * 0.5 + BED_BOTTOM_W * 0.5
	for i in n:
		var z0 := FIELD_Z0 + m
		if x - BED_BOTTOM_W * 0.5 >= FIELD_RX0 + m:
			z0 = FIELD_RZ0 + m
		out.append([x, z0, FIELD_Z1 - m])
		x += BED_PITCH
	return out


## Cerca baixa: postes + duas travessas ao longo de um trecho reto (alinhado a X ou Z).
## `end_post` = emite o poste do ponto final (trechos encadeados nao repetem a quina).
static func _fence_run(b, a: Vector2, c: Vector2, end_post: bool) -> void:
	var d := c - a
	var n := maxi(1, ceili(d.length() / POST_STEP))
	var hp := POST_W * 0.5
	for i in range(0, n + (1 if end_post else 0)):
		var p := a + d * (float(i) / float(n))
		b.box("trim_white", Vector3(p.x - hp, 0.0, p.y - hp), Vector3(p.x + hp, FENCE_H, p.y + hp), PackedStringArray(["-y"]))
	var ht := RAIL_T * 0.5
	for y in RAIL_YS:
		var yy: float = y
		if absf(d.x) > absf(d.y):
			b.box("trim_white", Vector3(minf(a.x, c.x), yy, a.y - ht), Vector3(maxf(a.x, c.x), yy + RAIL_H, a.y + ht))
		else:
			b.box("trim_white", Vector3(a.x - ht, yy, minf(a.y, c.y)), Vector3(a.x + ht, yy + RAIL_H, maxf(a.y, c.y)))


static func _fence(b) -> void:
	var i := FENCE_IN
	var pts := [
		Vector2(FIELD_RX0 + i, FIELD_Z0 + i), Vector2(FIELD_RX0 + i, FIELD_RZ0 + i),
		Vector2(FIELD_X - i, FIELD_RZ0 + i), Vector2(FIELD_X - i, FIELD_Z1 - i),
		Vector2(-FIELD_X + i, FIELD_Z1 - i), Vector2(-FIELD_X + i, FIELD_Z0 + i),
		Vector2(FIELD_GATE_X0, FIELD_Z0 + i),
	]
	for k in pts.size() - 1:
		_fence_run(b, pts[k], pts[k + 1], k == pts.size() - 2)


static func _field(b) -> void:
	var base := PackedVector2Array([
		Vector2(-FIELD_X, FIELD_Z0), Vector2(FIELD_RX0, FIELD_Z0), Vector2(FIELD_RX0, FIELD_RZ0),
		Vector2(FIELD_X, FIELD_RZ0), Vector2(FIELD_X, FIELD_Z1), Vector2(-FIELD_X, FIELD_Z1),
	])
	b.extrude("farm_dirt", base, 0.0, FIELD_BASE_TOP, true, false)
	var m := FIELD_BORDER
	var soil := PackedVector2Array([
		Vector2(-FIELD_X + m, FIELD_Z0 + m), Vector2(FIELD_RX0 + m, FIELD_Z0 + m), Vector2(FIELD_RX0 + m, FIELD_RZ0 + m),
		Vector2(FIELD_X - m, FIELD_RZ0 + m), Vector2(FIELD_X - m, FIELD_Z1 - m), Vector2(-FIELD_X + m, FIELD_Z1 - m),
	])
	b.extrude("farm_soil", soil, FIELD_BASE_TOP, SOIL_TOP, true, false)
	for bed in _bed_layout():
		_bed(b, bed[0], bed[1], bed[2], SOIL_TOP)
	if FENCE_ON:
		_fence(b)


## Valor pseudo-aleatorio deterministico em [0, 1).
static func _hash(i: int, j: int) -> float:
	var v := sin(float(i) * 12.9898 + float(j) * 78.233) * 43758.5453
	return v - floor(v)


## Fileira de plantacao generica: monte pentagonal ao longo de Z (laterais escuras, topo claro).
static func _crop_seg(b, cx: float, z0: float, z1: float, y0: float, w: float, h: float) -> void:
	var p := [Vector2(-w * 0.5, 0.0), Vector2(-w * 0.34, h * 0.72), Vector2(0.0, h), Vector2(w * 0.34, h * 0.72), Vector2(w * 0.5, 0.0)]
	var slots := [CROP_SIDE, CROP_TOP, CROP_TOP, CROP_SIDE]
	for k in 4:
		var pa: Vector2 = p[k]
		var pb: Vector2 = p[k + 1]
		var mid := (pa + pb) * 0.5
		b.quad(slots[k], Vector3(cx + pa.x, y0 + pa.y, z0), Vector3(cx + pb.x, y0 + pb.y, z0), Vector3(cx + pb.x, y0 + pb.y, z1), Vector3(cx + pa.x, y0 + pa.y, z1), Vector3(mid.x, mid.y - h * 0.3, 0))
	for zz in [z0, z1]:
		var pts := []
		for q in p:
			pts.append(Vector3(cx + q.x, y0 + q.y, zz))
		b.face(CROP_SIDE, pts, Vector3(0, 0, -1 if zz == z0 else 1))


const CROP_SEG_LEN := 6.4
const CROP_GAP := 0.4
const CROP_W := 0.98
const CROP_SINK := 0.12     # base enterrada na crista do camalhao (sem frestas nos taludes)


## Fileiras verdes sobre cada canteiro, cortadas numa grade UNICA em Z (os vaos alinham
## entre o bloco principal e o bloco direito; nada de grade desencontrada).
static func _crop(b) -> void:
	var y0 := SOIL_TOP + BED_H - CROP_SINK
	var i := 0
	for bed in _bed_layout():
		var z0: float = bed[1] + 0.2
		var z1: float = FIELD_Z1 - FIELD_MARGIN - 0.2
		var top := z1
		var j := 1
		while true:
			var cut := z1 - CROP_SEG_LEN * float(j)
			var h := 0.62 + 0.18 * _hash(i, j)
			if cut - CROP_GAP * 0.5 <= z0 + 2.0:
				_crop_seg(b, bed[0], z0, top, y0, CROP_W, h)
				break
			_crop_seg(b, bed[0], cut + CROP_GAP * 0.5, top, y0, CROP_W, h)
			top = cut - CROP_GAP * 0.5
			j += 1
		i += 1


func build(kit) -> void:
	var b = kit.builder()
	b.push_translate(Vector3(BARN_CX, 0, BARN_CZ))
	_barn(b)
	b.pop()
	_silo(b)
	_yard(b)
	_field(b)
	var body_tris: int = b.triangle_count()
	var body = kit.save_mesh(b, FAMILY_ID)

	var cb = kit.builder()
	_crop(cb)
	var crop_tris: int = cb.triangle_count()
	var crop = kit.save_mesh(cb, FAMILY_ID + ".crop")
	print("[farm] triangulos Body: ", body_tris, " | State_Crop: ", crop_tris, " | canteiros: ", _bed_layout().size())

	var root = kit.new_root("FarmSmall", FAMILY_ID)
	kit.add_mesh(root, "Body", body)
	var st: Node3D = kit.add_node(root, "State_Crop")
	st.visible = DEBUG_SHOW_STATES
	kit.add_mesh(st, "Crop", crop)
	var door_z := BARN_CZ - HD - PLINTH_OUT
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(BARN_CX, 0.0, door_z - 0.8), Vector3.FORWARD,
		"CANDIDATO: diante do portao (fechado) do celeiro; nao e acesso funcional")
	kit.add_marker(root, "Candidate_CargoPoint", Vector3(-4.4, 0.0, -16.8), Vector3.RIGHT,
		"CANDIDATO: carga de Alimentos no terreiro ao lado do celeiro, voltado para o acesso de terra")
	kit.add_marker(root, "Candidate_FieldAccess", Vector3(0.0, 0.0, FIELD_Z0 - 0.4), Vector3.FORWARD,
		"CANDIDATO: entrada do campo (vao da cerca) no fim do acesso de terra; forward para fora do campo")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Fazenda pequena",
		"lot": LOT,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"celeiro_corpo_x_z_m": [HW * 2.0, HD * 2.0],
			"celeiro_centro_x_z_m": [BARN_CX, BARN_CZ],
			"celeiro_parede_m": WALL_TOP,
			"celeiro_cumeeira_m": snappedf(_low(0.0) + ROOF_T, 0.01),
			"celeiro_lanternim_topo_m": snappedf(_low(0.0) + 1.85, 0.01),
			"portao_l_x_a_m": [DOOR_W, DOOR_TOP - PLINTH_H],
			"silo_diametro_m": SILO_R * 2.0,
			"silo_topo_m": SILO_CAP + 0.2,
			"acesso_terra_largura_m": TRACK_HW * 2.0,
			"campo_x_m": [-FIELD_X, FIELD_X],
			"campo_z_principal_m": [FIELD_Z0, FIELD_Z1],
			"campo_z_bloco_direito_m": [FIELD_RZ0, FIELD_Z1],
			"canteiro_base_crista_altura_passo_m": [BED_BOTTOM_W, BED_TOP_W, BED_H, BED_PITCH],
			"cerca_altura_m": FENCE_H if FENCE_ON else 0.0,
		},
		"parts": {
			"Body": "malha unica (7 superficies = slots): farm_barn (paredes e paineis do celeiro), farm_roof (cobertura metalica, cumeeira, lanternim, chapeu do silo, fundo das venezianas), trim_white (testeiras, cantoneiras, molduras e X dos portoes, venezianas, cerca), concrete (plinto do celeiro e base do silo), metal_light (corpo do silo), farm_dirt (terreiro/acesso de terra e borda clara do campo), farm_soil (solo preparado e canteiros em fileiras)",
			"State_Crop": "Node3D OCULTO; malha State_Crop/Crop (tree_foliage + tree_foliage_dark): fileiras de plantacao verde generica sobre os canteiros. So o host liga a partir de producao REAL (cultivo em andamento).",
			"Candidate_PedestrianEntry": "Marker3D candidato diante do portao do celeiro, forward -Z",
			"Candidate_CargoPoint": "Marker3D candidato no terreiro a direita do celeiro (carga de Alimentos), forward +X para o acesso de terra",
			"Candidate_FieldAccess": "Marker3D candidato na entrada do campo (fim do acesso de terra, vao da cerca), forward -Z",
		},
		"notes": [
			"Pivo no centro do lote 36 x 40 m (solo). Celeiro 10 x 8 m na faixa frontal esquerda (x de -16,4 a -6,4; fachada em z = -15); silo em (-4,1; -10,2); acesso de terra do centro da borda frontal (z = -20) ate o campo (z = -4,4).",
			"Campo em L: bloco principal de z = -4,4 a 19,5 em toda a largura e bloco direito ate z = -17,4 (x >= 2,6). Canteiros de solo preparado ao longo de Z no Body (estrutura permanente); a plantacao e ESTADO (State_Crop).",
			"Plantacao generica (montes verdes sem cultura identificavel). Sem menu de culturas, tratores, animais, pessoas, caixas ou estoque a mostra. Silo e estrutura vazia (nao indica estoque).",
			"Cerca baixa branca opcional (pedido de compatibilizacao) com vao na entrada do campo; nao delimita o lote e nao e acesso funcional. Pode ser removida com FENCE_ON = false.",
			"Portao do celeiro sempre fechado; detalhes desenhados nao sao acessos funcionais (somente os Marker3D candidatos).",
		],
	})


## Icone simplificado (make_v1_icons.gd): campo com fileiras verdes ATRAS, celeiro vermelho com
## portao em X e silo NA FRENTE (composicao compacta; campo encolhido para ler em 64 px).
func draw_icon(ic) -> void:
	var barn: String = ic.c("farm_barn")
	var roof: String = ic.c("farm_roof")
	var white: String = ic.c("trim_white")
	var soil: String = ic.c("farm_soil")
	var dirt: String = ic.c("farm_dirt")
	var leaf: String = ic.c(CROP_TOP)
	var leaf_dark: String = ic.c(CROP_SIDE)
	# Campo atras (+Z) e a direita do celeiro no isometrico: base clara + solo + 4 fileiras.
	ic.part(-100.0)
	ic.box(Vector3(-15.2, 0.0, -1.2), Vector3(1.2, 0.12, 15.2), dirt)
	ic.part(-99.0)
	ic.box(Vector3(-14.4, 0.12, -0.4), Vector3(0.4, 0.24, 14.4), soil)
	for k in 4:
		var x0 := -13.6 + 3.6 * float(k)
		ic.part(-60.0 + float(k))
		ic.box(Vector3(x0, 0.24, 0.4), Vector3(x0 + 2.2, 0.85, 13.6), leaf_dark, leaf)
	# Celeiro (corpo 10 x 8) com portao em X e porta do feno na empena frontal.
	var bx0 := -3.0
	var bx1 := 7.0
	var bz0 := -10.0
	var bz1 := -2.0
	var bc := (bx0 + bx1) * 0.5
	ic.part()
	ic.box(Vector3(bx0, 0.0, bz0), Vector3(bx1, 4.2, bz1), barn)
	var zf := bz0 - 0.02
	ic.facade_rect(bc - 2.5, bc + 2.5, 0.0, 3.9, zf, white)
	ic.facade_rect(bc - 2.2, bc + 2.2, 0.0, 3.6, zf - 0.01, barn)
	ic.quad([Vector3(bc - 2.2, 0.0, zf - 0.02), Vector3(bc - 1.65, 0.0, zf - 0.02), Vector3(bc + 2.2, 3.05, zf - 0.02), Vector3(bc + 2.2, 3.6, zf - 0.02), Vector3(bc + 1.65, 3.6, zf - 0.02), Vector3(bc - 2.2, 0.55, zf - 0.02)], white, true)
	ic.quad([Vector3(bc + 2.2, 0.0, zf - 0.02), Vector3(bc + 1.65, 0.0, zf - 0.02), Vector3(bc - 2.2, 3.05, zf - 0.02), Vector3(bc - 2.2, 3.6, zf - 0.02), Vector3(bc - 1.65, 3.6, zf - 0.02), Vector3(bc + 2.2, 0.55, zf - 0.02)], white, true)
	ic.side_rect(-7.6, -5.6, 1.6, 2.8, bx1 + 0.02, white)
	ic.side_rect(-7.4, -5.8, 1.75, 2.65, bx1 + 0.03, roof)
	ic.part(0.5)
	ic.gable(Vector3(bx0 - 0.5, 4.2, bz0 - 0.4), Vector3(bx1 + 0.5, 4.2, bz1 + 0.4), 7.9, "z", roof, barn)
	ic.facade_rect(bc - 0.8, bc + 0.8, 4.6, 6.1, bz0 - 0.42, white)
	ic.facade_rect(bc - 0.6, bc + 0.6, 4.75, 5.95, bz0 - 0.43, barn)
	ic.part(0.6)
	ic.box(Vector3(bc - 0.6, 7.3, -6.6), Vector3(bc + 0.6, 8.9, -5.4), white)
	ic.part(0.7)
	ic.gable(Vector3(bc - 0.85, 8.9, -6.85), Vector3(bc + 0.85, 8.9, -5.15), 9.5, "z", roof)
	# Silo ao lado (+X), na frente do campo.
	ic.part()
	ic.prism(Vector3(9.6, 0.0, -5.0), 1.6, 1.6, 6.4, 8, ic.c("metal_light"))
	ic.part(0.2)
	ic.prism(Vector3(9.6, 6.4, -5.0), 1.75, 0.3, 1.2, 8, roof)
