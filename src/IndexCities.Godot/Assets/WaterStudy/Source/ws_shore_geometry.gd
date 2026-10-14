## IndexCities - WaterStudy - gerador de geometria e espelho de ruido (ferramenta de autoria).
## Revisao humana: PENDENTE.
##
## A geometria da AMOSTRA B (bacia rebaixada, margem irregular, trecho de rio) e CONCEITUAL e
## NAO APROVADA PARA GAMEPLAY. Ela existe so na previa isolada do estudo; nao muda o mapa, a
## seed, a regra de agua (`InWater`), a validade de obras/ruas, a captacao, a poluicao, o save
## nem nada da simulacao. Sem colisao, sem picking, sem conectores.
##
## Unidades: metros, Y para cima. Tudo e deterministico (mesma entrada -> mesmos vertices).
##
## O hash/ruido e a largura de margem (`bank_width`) espelham, com as MESMAS constantes,
## Shaders/ws_common.gdshaderinc: B constroi em relevo a mesma linha d'agua que A pinta.
extends RefCounted

# --- Mundo do jogo (CityWorld.Reset / SimulationTuning, main 528d30d) ---
const MAP_HALF := 256.0
const GROUND_Y := -0.06          # topo do BoxMesh do chao (y -0,56, altura 1)
const GROUND_BOTTOM_Y := -1.06   # base do mesmo BoxMesh
# Lago da seed 42, recalculado com o xorshift64* exato da simulacao (C# unchecked ulong).
const LAKE_CENTRE := Vector2(152.60851295874232, 85.982556337495026)
const LAKE_RADIUS := 28.57678906431947

# --- Amostra B (conceitual) ---
const WATER_Y := GROUND_Y - 0.8  # nivel d'agua 0,8 m abaixo do chao
const BANK_H := 0.8              # altura da margem (topo -> linha d'agua)
const LAKE_DEPTH := 2.2          # profundidade maxima decorativa do fundo (cor do raso)
const RIVER_DEPTH := 1.0
const EDGE_BED_MIN_Y := -0.98    # no limite do mapa o leito fica acima do topo da saia (-1,0)
const GRID_STEP := 1.0

## Parametros da linha d'agua: IGUAIS aos padroes de ws_water_a.gdshader.
const BANK := {"min_m": 1.0, "max_m": 5.5, "jag_m": 0.8, "lobe_m": 14.0, "bay_m": 38.0}

## Rio conceitual: do interior do lago (indice 0, foz) ate alem do limite leste do mapa.
const RIVER_POINTS: Array[Vector2] = [
	Vector2(163.0, 76.0), Vector2(176.0, 67.0), Vector2(191.0, 61.0), Vector2(206.0, 59.5),
	Vector2(220.0, 54.0), Vector2(235.0, 47.5), Vector2(249.0, 44.0), Vector2(266.0, 42.0),
]

const _MASK32 := 0xFFFFFFFF


# =============================================================================================
# Espelho do hash/ruido do shader (aritmetica uint de 32 bits sem estourar int64).
static func _mul32(a: int, b: int) -> int:
	var lo := (a * (b & 0xFFFF)) & _MASK32
	var hi := ((a * (b >> 16)) & 0xFFFF) << 16
	return (lo + hi) & _MASK32


static func ws_hash(cx: int, cy: int, salt: int) -> float:
	var ux := (cx + 65536) & _MASK32
	var uy := (cy + 65536) & _MASK32
	var h := (_mul32(ux, 0x9F3B6A2D) + _mul32(uy, 0x6C1E85B7) + _mul32(salt, 0xD2A47C19)) & _MASK32
	h ^= h >> 16
	h = _mul32(h, 0x4E8F2B63)
	h ^= h >> 14
	h = _mul32(h, 0xA7356E8B)
	h ^= h >> 15
	return float(h >> 8) * (1.0 / 16777216.0)


static func ws_noise(p: Vector2, salt: int) -> float:
	var i := p.floor()
	var f := p - i
	var cx := int(i.x)
	var cy := int(i.y)
	var a := ws_hash(cx, cy, salt)
	var b := ws_hash(cx + 1, cy, salt)
	var d := ws_hash(cx, cy + 1, salt)
	var e := ws_hash(cx + 1, cy + 1, salt)
	var w := f * f * (Vector2(3.0, 3.0) - 2.0 * f)
	return lerpf(lerpf(a, b, w.x), lerpf(d, e, w.x), w.y)


## Mesma convencao do mat2(vec2(c, s), vec2(-s, c)) * p do shader.
static func rot(p: Vector2, c: float, s: float) -> Vector2:
	return Vector2(c * p.x - s * p.y, s * p.x + c * p.y)


## Espelho de ws_bank_width (constantes de rotacao com os mesmos 4 decimais do shader).
static func bank_width(p: Vector2, prm: Dictionary = BANK) -> float:
	var bay := ws_noise(rot(p, 0.8910, 0.4540) / float(prm["bay_m"]), 401)
	var lobe := ws_noise(rot(p, 0.5299, -0.8480) / float(prm["lobe_m"]), 409)
	var n := 0.6 * bay + 0.4 * lobe
	var w := float(prm["min_m"]) + (float(prm["max_m"]) - float(prm["min_m"])) * smoothstep(0.3, 0.7, n)
	w += float(prm["jag_m"]) * (ws_noise(rot(p, 0.3256, 0.9455) / 4.6, 419) - 0.5)
	return maxf(w, 0.25)


## Ponto da linha d'agua da amostra A/B na direcao `dir` (iteracao de ponto fixo r = R - w).
static func waterline_point(dir: Vector2, centre: Vector2 = LAKE_CENTRE, radius: float = LAKE_RADIUS) -> Vector2:
	var r := radius - 4.0
	for i in 12:
		r = radius - bank_width(centre + dir * r)
	return centre + dir * r


# =============================================================================================
# Perfil da margem: t = 0 no topo (nivel do chao), 1 na linha d'agua.
# Base: Hermite com inclinacao 0 no topo (sem vinco no chao) e 0,8 na agua (cruza o nivel
# d'agua com angulo, sem trecho plano rente a superficie). Barranco: queda curta no meio.
static func profile(t: float, scarp: float) -> float:
	var base := -1.2 * t * t * t + 2.2 * t * t
	var steep := 0.15 * base + 0.85 * smoothstep(0.28, 0.62, t)
	return lerpf(base, steep, scarp)


static func profile_end_slope(scarp: float) -> float:
	return lerpf(0.8, 0.12, scarp)


static func scarp_amount(p: Vector2) -> float:
	return smoothstep(0.58, 0.78, ws_noise(rot(p, 0.9703, -0.2419) / 9.0, 461))


## Fracao plana no topo da margem antes do declive (espelho de `top` em ws_water_a.gdshader:
## grass_reach 0,85, ruido WS_ROT_T / 5,5 m, sal 103). O declive comeca numa linha irregular,
## nao no circulo, para a faixa iluminada do talude nao desenhar o contorno circular.
const GRASS_REACH := 0.85


static func bank_top_fraction(p: Vector2) -> float:
	var n := ws_noise(rot(p, 0.7193, 0.6947) / 5.5, 103)
	return clampf(GRASS_REACH * (0.3 + 0.95 * n) * 0.55, 0.0, 0.7)


## Inclinacao minima do fundo logo apos a linha d'agua (m/m), para o raso nao se estender
## demais onde a margem e larga e suave.
const UNDERWATER_MIN_SLOPE := 0.22


# =============================================================================================
# Rio: polilinha densa (Catmull-Rom) + baldes espaciais para consulta rapida.
var _river: PackedVector2Array
var _river_u: PackedFloat32Array
var _buckets := {}
const _BUCKET := 8.0
const _RIVER_REACH := 16.0


func _init() -> void:
	_build_river()


func _build_river() -> void:
	_river = PackedVector2Array()
	var pts := RIVER_POINTS
	for i in pts.size() - 1:
		var p0 := pts[maxi(i - 1, 0)]
		var p1 := pts[i]
		var p2 := pts[i + 1]
		var p3 := pts[mini(i + 2, pts.size() - 1)]
		var steps := int(ceil(p1.distance_to(p2) / 0.5))
		for k in steps:
			var t := float(k) / steps
			var t2 := t * t
			var t3 := t2 * t
			_river.append(0.5 * ((2.0 * p1) + (-p0 + p2) * t + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2
					+ (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3))
	_river.append(pts[pts.size() - 1])
	_river_u = PackedFloat32Array()
	var acc := 0.0
	for i in _river.size():
		if i > 0:
			acc += _river[i].distance_to(_river[i - 1])
		_river_u.append(acc)
	for i in _river.size() - 1:
		var a := _river[i]
		var b := _river[i + 1]
		var lo := Vector2(minf(a.x, b.x), minf(a.y, b.y)) - Vector2.ONE * _RIVER_REACH
		var hi := Vector2(maxf(a.x, b.x), maxf(a.y, b.y)) + Vector2.ONE * _RIVER_REACH
		for bx in range(int(floor(lo.x / _BUCKET)), int(floor(hi.x / _BUCKET)) + 1):
			for bz in range(int(floor(lo.y / _BUCKET)), int(floor(hi.y / _BUCKET)) + 1):
				var key := Vector2i(bx, bz)
				if not _buckets.has(key):
					_buckets[key] = PackedInt32Array()
				var arr: PackedInt32Array = _buckets[key]
				arr.append(i)
				_buckets[key] = arr


## {dist, u, tangent (sentido da nascente -> foz), found}
func river_query(p: Vector2) -> Dictionary:
	var key := Vector2i(int(floor(p.x / _BUCKET)), int(floor(p.y / _BUCKET)))
	if not _buckets.has(key):
		return {"found": false}
	var best := INF
	var best_u := 0.0
	var best_t := Vector2.ZERO
	for i in (_buckets[key] as PackedInt32Array):
		var a := _river[i]
		var b := _river[i + 1]
		var ab := b - a
		var len2 := ab.length_squared()
		var h := clampf((p - a).dot(ab) / len2, 0.0, 1.0)
		var dist := p.distance_to(a + ab * h)
		if dist < best:
			best = dist
			best_u = _river_u[i] + h * sqrt(len2)
			best_t = -ab.normalized()
	return {"found": true, "dist": best, "u": best_u, "flow": best_t}


static func river_half_width(u: float) -> float:
	return 4.3 + 3.4 * (1.0 - smoothstep(8.0, 40.0, u)) + 0.8 * (ws_noise(Vector2(u / 13.0, 3.5), 467) - 0.5)


static func river_bank_width(u: float) -> float:
	return 3.2 + 1.6 * ws_noise(Vector2(u / 17.0, 7.5), 479)


# =============================================================================================
# Campo de altura da amostra B e campos auxiliares de cor da agua.
## Retorna [y, s_lake, s_river, river_found(0/1), flow.x, flow.y].
## s_* > 0 dentro d'agua (aprox. distancia horizontal a linha d'agua, m).
func fields(p: Vector2) -> PackedFloat32Array:
	# Lago: margem dentro do circulo de jogo; fora do circulo, chao plano.
	var e := LAKE_RADIUS - p.distance_to(LAKE_CENTRE)
	var y_lake := GROUND_Y
	var s_lake := -1000.0
	if e > -1.0:
		var w := bank_width(p)
		s_lake = e - w
		if e > 0.0:
			var sc := scarp_amount(p)
			var e0 := w * bank_top_fraction(p)
			if e < w:
				if e > e0:
					y_lake = GROUND_Y - BANK_H * profile((e - e0) / (w - e0), sc)
			else:
				var k := maxf(profile_end_slope(sc) * BANK_H / (w - e0), UNDERWATER_MIN_SLOPE)
				y_lake = WATER_Y - LAKE_DEPTH * (1.0 - exp(-(e - w) * k / LAKE_DEPTH))
	# Rio conceitual.
	var y_river := GROUND_Y
	var s_river := -1000.0
	var found := 0.0
	var flow := Vector2.ZERO
	var q := river_query(p)
	if q["found"]:
		var u: float = q["u"]
		var hw := river_half_width(u)
		var bw := river_bank_width(u)
		s_river = hw - float(q["dist"])
		flow = q["flow"]
		found = 1.0
		if s_river > -bw:
			var sc2 := scarp_amount(p + Vector2(31.0, -17.0))
			var top := bank_top_fraction(p)
			if s_river < 0.0:
				var tr := (s_river + bw) / bw
				if tr > top:
					y_river = GROUND_Y - BANK_H * profile((tr - top) / (1.0 - top), sc2)
			else:
				var k2 := maxf(profile_end_slope(sc2) * BANK_H / (bw * (1.0 - top)), UNDERWATER_MIN_SLOPE)
				y_river = WATER_Y - RIVER_DEPTH * (1.0 - exp(-s_river * k2 / RIVER_DEPTH))
	var y := minf(y_lake, y_river)
	if p.x > MAP_HALF - 8.0 and y < WATER_Y:
		y = maxf(y, lerpf(y, EDGE_BED_MIN_Y, smoothstep(MAP_HALF - 8.0, MAP_HALF - 2.0, p.x)))
	return PackedFloat32Array([y, s_lake, s_river, found, flow.x, flow.y])


## Retangulo da grade fina (GRID_STEP) que contem a bacia e o rio, alinhado a grade grossa
## (COARSE_STEP a partir de -256) para a costura entre as duas nao ter juncao em T.
const COARSE_STEP := 8.0


static func _snap_down(v: float) -> float:
	return -MAP_HALF + floorf((v + MAP_HALF) / COARSE_STEP) * COARSE_STEP


static func _snap_up(v: float) -> float:
	return -MAP_HALF + ceilf((v + MAP_HALF) / COARSE_STEP) * COARSE_STEP


static func fine_bounds() -> Rect2:
	var x0 := _snap_down(LAKE_CENTRE.x - LAKE_RADIUS - 4.0)
	var z0 := _snap_down(minf(LAKE_CENTRE.y - LAKE_RADIUS - 4.0, 42.0 - 4.3 - 4.8 - 5.0))
	var z1 := _snap_up(LAKE_CENTRE.y + LAKE_RADIUS + 4.0)
	return Rect2(x0, z0, MAP_HALF - x0, z1 - z0)


## Constroi tudo da amostra B. Chao: retalho fino (1 m) da bacia e do rio costurado a uma grade
## grossa (8 m) no resto do mapa; as celulas grossas vizinhas do retalho viram leques que usam os
## vertices finos da borda (sem juncao em T e sem fatias finas atravessando o mapa).
## Retorna {terrain, water, cut_face, stats}.
func build_b() -> Dictionary:
	var fb := fine_bounds()
	var x0 := fb.position.x
	var z0 := fb.position.y
	var x1 := fb.end.x
	var z1 := fb.end.y
	var nx := int(round((x1 - x0) / GRID_STEP)) + 1
	var nz := int(round((z1 - z0) / GRID_STEP)) + 1
	var ys := PackedFloat32Array()
	ys.resize(nx * nz)
	var s_all := PackedFloat32Array()
	s_all.resize(nx * nz)
	var rf_all := PackedFloat32Array()
	rf_all.resize(nx * nz)
	var flow_all := PackedVector2Array()
	flow_all.resize(nx * nz)
	for j in nz:
		for i in nx:
			var idx := j * nx + i
			var f := fields(Vector2(x0 + i * GRID_STEP, z0 + j * GRID_STEP))
			ys[idx] = f[0]
			s_all[idx] = maxf(f[1], f[2])
			rf_all[idx] = smoothstep(-2.5, 2.5, f[2] - f[1]) if f[3] > 0.5 else 0.0
			flow_all[idx] = Vector2(f[4], f[5])
	# --- Terreno: retalho fino ---
	var tv := PackedVector3Array()
	var tn := PackedVector3Array()
	var tuv2 := PackedVector2Array()
	var keys := {}
	for j in nz:
		for i in nx:
			var idx := j * nx + i
			var x := x0 + i * GRID_STEP
			var z := z0 + j * GRID_STEP
			tv.append(Vector3(x, ys[idx], z))
			var il := maxi(i - 1, 0)
			var ir := mini(i + 1, nx - 1)
			var jl := maxi(j - 1, 0)
			var jr := mini(j + 1, nz - 1)
			var dydx := (ys[j * nx + ir] - ys[j * nx + il]) / maxf((ir - il) * GRID_STEP, 0.001)
			var dydz := (ys[jr * nx + i] - ys[jl * nx + i]) / maxf((jr - jl) * GRID_STEP, 0.001)
			tn.append(Vector3(-dydx, 1.0, -dydz).normalized())
			tuv2.append(Vector2.ZERO)
			keys[Vector2i(int(round(x)), int(round(z)))] = idx
	var ti := PackedInt32Array()
	for j in nz - 1:
		for i in nx - 1:
			var a := j * nx + i
			var b := a + 1
			var c := a + nx
			var d := c + 1
			if absf(ys[a] - ys[d]) <= absf(ys[b] - ys[c]):
				_tri(ti, tv, a, b, d, Vector3.UP)
				_tri(ti, tv, a, d, c, Vector3.UP)
			else:
				_tri(ti, tv, a, b, c, Vector3.UP)
				_tri(ti, tv, b, d, c, Vector3.UP)
	var fine_triangles := ti.size() / 3
	# --- Terreno: grade grossa plana no resto do mapa, com leques na costura ---
	var per := int(round(COARSE_STEP / GRID_STEP))
	var nc := int(round(2.0 * MAP_HALF / COARSE_STEP)) + 1
	var coarse_cells := 0
	var fan_cells := 0
	for cj in nc - 1:
		for ci in nc - 1:
			var cx := -MAP_HALF + ci * COARSE_STEP
			var cz := -MAP_HALF + cj * COARSE_STEP
			var inside_z := cz >= z0 - 0.01 and cz + COARSE_STEP <= z1 + 0.01
			var inside_x := cx >= x0 - 0.01 and cx + COARSE_STEP <= x1 + 0.01
			if inside_x and inside_z:
				continue
			coarse_cells += 1
			var a := _cv(cx, cz, tv, tn, tuv2, keys)
			var b := _cv(cx + COARSE_STEP, cz, tv, tn, tuv2, keys)
			var c := _cv(cx, cz + COARSE_STEP, tv, tn, tuv2, keys)
			var d := _cv(cx + COARSE_STEP, cz + COARSE_STEP, tv, tn, tuv2, keys)
			var edge: Array[int] = []
			if is_equal_approx(cx + COARSE_STEP, x0) and inside_z:
				for k in per + 1:
					edge.append(keys[Vector2i(int(round(x0)), int(round(cz)) + k)])
				_fan(ti, tv, a, c, edge)
			elif is_equal_approx(cz + COARSE_STEP, z0) and inside_x:
				for k in per + 1:
					edge.append(keys[Vector2i(int(round(cx)) + k, int(round(z0)))])
				_fan(ti, tv, a, b, edge)
			elif is_equal_approx(cz, z1) and inside_x:
				for k in per + 1:
					edge.append(keys[Vector2i(int(round(cx)) + k, int(round(z1)))])
				_fan(ti, tv, c, d, edge)
			else:
				_tri(ti, tv, a, b, d, Vector3.UP)
				_tri(ti, tv, a, d, c, Vector3.UP)
				continue
			fan_cells += 1
	# --- Faces laterais do limite do mapa (como as do BoxMesh do jogo) ---
	var zline: Array[float] = []
	var zz := -MAP_HALF
	while zz <= MAP_HALF + 0.01:
		if zz < z0 - 0.01 or zz > z1 + 0.01:
			zline.append(zz)
		zz += COARSE_STEP
	var zf := z0
	while zf <= z1 + 0.01:
		zline.append(zf)
		zf += GRID_STEP
	zline.sort()
	var east := PackedInt32Array()
	for zv in zline:
		east.append(keys[Vector2i(int(round(MAP_HALF)), int(round(zv)))])
	var west := PackedInt32Array()
	var north := PackedInt32Array()
	var south := PackedInt32Array()
	for k in nc:
		var v := -MAP_HALF + k * COARSE_STEP
		west.append(keys[Vector2i(int(round(-MAP_HALF)), int(round(v)))])
		north.append(keys[Vector2i(int(round(v)), int(round(-MAP_HALF)))])
		south.append(keys[Vector2i(int(round(v)), int(round(MAP_HALF)))])
	for edge_def in [[east, Vector3(1, 0, 0)], [west, Vector3(-1, 0, 0)], [north, Vector3(0, 0, -1)], [south, Vector3(0, 0, 1)]]:
		var ids: PackedInt32Array = edge_def[0]
		var n: Vector3 = edge_def[1]
		var start := tv.size()
		for k in ids.size():
			var top := tv[ids[k]]
			tv.append(top)
			tn.append(n)
			tuv2.append(Vector2(1.0, 0.0))
			tv.append(Vector3(top.x, GROUND_BOTTOM_Y, top.z))
			tn.append(n)
			tuv2.append(Vector2(1.0, 0.0))
		for k in ids.size() - 1:
			var a2 := start + k * 2
			_tri(ti, tv, a2, a2 + 2, a2 + 1, n)
			_tri(ti, tv, a2 + 2, a2 + 3, a2 + 1, n)
	var terrain := _mesh(tv, tn, ti, PackedVector2Array(), tuv2, PackedColorArray())
	# --- Agua: celulas finas com algum canto abaixo do nivel; plano em WATER_Y ---
	var wv := PackedVector3Array()
	var wn := PackedVector3Array()
	var wuv := PackedVector2Array()
	var wuv2 := PackedVector2Array()
	var wcol := PackedColorArray()
	var wi := PackedInt32Array()
	var remap := {}
	for j in nz - 1:
		for i in nx - 1:
			var a := j * nx + i
			var corners := [a, a + 1, a + nx, a + nx + 1]
			var low := INF
			for c in corners:
				low = minf(low, ys[c])
			if low >= WATER_Y + 0.02:
				continue
			var mapped := []
			for c in corners:
				if not remap.has(c):
					remap[c] = wv.size()
					var ci: int = c % nx
					var cj: int = c / nx
					wv.append(Vector3(x0 + ci * GRID_STEP, WATER_Y, z0 + cj * GRID_STEP))
					wn.append(Vector3.UP)
					var depth := maxf(WATER_Y - ys[c], 0.0)
					wuv.append(Vector2(depth, s_all[c]))
					var cl := cj * nx + maxi(ci - 1, 0)
					var cr := cj * nx + mini(ci + 1, nx - 1)
					var cu := maxi(cj - 1, 0) * nx + ci
					var cd := mini(cj + 1, nz - 1) * nx + ci
					var g := Vector2(s_all[cr] - s_all[cl], s_all[cd] - s_all[cu])
					var outward := -g.normalized() if g.length() > 1e-5 else Vector2.ZERO
					wuv2.append(outward)
					var fl := flow_all[c]
					wcol.append(Color(rf_all[c], fl.x * 0.5 + 0.5, fl.y * 0.5 + 0.5, 1.0))
				mapped.append(remap[c])
			_tri(wi, wv, mapped[0], mapped[1], mapped[3], Vector3.UP)
			_tri(wi, wv, mapped[0], mapped[3], mapped[2], Vector3.UP)
	var water := _mesh(wv, wn, wi, wuv, wuv2, wcol)
	# --- Corte do rio no limite leste do mapa (secao d'agua vertical) ---
	var cv := PackedVector3Array()
	var cn := PackedVector3Array()
	var ci2 := PackedInt32Array()
	var col_e := _column(nx, nz, nx - 1)
	for k in col_e.size() - 1:
		var p0 := tv[col_e[k]]
		var p1 := tv[col_e[k + 1]]
		if p0.y >= WATER_Y and p1.y >= WATER_Y:
			continue
		var base := cv.size()
		cv.append(Vector3(MAP_HALF, WATER_Y, p0.z))
		cv.append(Vector3(MAP_HALF, minf(p0.y, WATER_Y), p0.z))
		cv.append(Vector3(MAP_HALF, WATER_Y, p1.z))
		cv.append(Vector3(MAP_HALF, minf(p1.y, WATER_Y), p1.z))
		for _r in 4:
			cn.append(Vector3(1, 0, 0))
		_tri(ci2, cv, base, base + 2, base + 1, Vector3(1, 0, 0))
		_tri(ci2, cv, base + 2, base + 3, base + 1, Vector3(1, 0, 0))
	var cut := _mesh(cv, cn, ci2, PackedVector2Array(), PackedVector2Array(), PackedColorArray())
	return {
		"terrain": terrain, "water": water, "cut_face": cut,
		"stats": {"fine_columns": nx, "fine_rows": nz, "fine_bounds": [x0, z0, x1, z1], "fine_triangles": fine_triangles,
			"coarse_step_m": COARSE_STEP, "coarse_cells": coarse_cells, "stitched_fan_cells": fan_cells,
			"terrain_vertices": tv.size(), "terrain_triangles": ti.size() / 3,
			"water_vertices": wv.size(), "water_triangles": wi.size() / 3,
			"cut_vertices": cv.size(), "cut_triangles": ci2.size() / 3},
	}


## Vertice da grade grossa (plano, no nivel do chao); reaproveita o vertice fino se ja existir.
static func _cv(x: float, z: float, tv: PackedVector3Array, tn: PackedVector3Array, tuv2: PackedVector2Array, keys: Dictionary) -> int:
	var key := Vector2i(int(round(x)), int(round(z)))
	if keys.has(key):
		return keys[key]
	var idx := tv.size()
	tv.append(Vector3(x, GROUND_Y, z))
	tn.append(Vector3.UP)
	tuv2.append(Vector2.ZERO)
	keys[key] = idx
	return idx


## Celula grossa costurada: leque dos dois cantos opostos (o0 perto de edge[0], o1 perto do fim)
## ate os vertices finos da aresta compartilhada.
static func _fan(out: PackedInt32Array, verts: PackedVector3Array, o0: int, o1: int, edge: Array[int]) -> void:
	var half := edge.size() / 2
	for k in half:
		_tri(out, verts, o0, edge[k], edge[k + 1], Vector3.UP)
	_tri(out, verts, o0, edge[half], o1, Vector3.UP)
	for k in range(half, edge.size() - 1):
		_tri(out, verts, o1, edge[k], edge[k + 1], Vector3.UP)


static func _column(nx: int, nz: int, i: int) -> PackedInt32Array:
	var out := PackedInt32Array()
	for j in nz:
		out.append(j * nx + i)
	return out


static func _row(nx: int, j: int) -> PackedInt32Array:
	var out := PackedInt32Array()
	for i in nx:
		out.append(j * nx + i)
	return out


## Triangulo com a face frontal (convencao da Godot: horario visto de frente) para `facing`.
static func _tri(out: PackedInt32Array, verts: PackedVector3Array, a: int, b: int, c: int, facing: Vector3) -> void:
	var front := (verts[c] - verts[a]).cross(verts[b] - verts[a])
	if front.dot(facing) >= 0.0:
		out.append_array([a, b, c])
	else:
		out.append_array([a, c, b])


static func _mesh(v: PackedVector3Array, n: PackedVector3Array, idx: PackedInt32Array, uv: PackedVector2Array,
		uv2: PackedVector2Array, col: PackedColorArray) -> ArrayMesh:
	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = v
	arrays[Mesh.ARRAY_NORMAL] = n
	if uv.size() == v.size():
		arrays[Mesh.ARRAY_TEX_UV] = uv
	if uv2.size() == v.size():
		arrays[Mesh.ARRAY_TEX_UV2] = uv2
	if col.size() == v.size():
		arrays[Mesh.ARRAY_COLOR] = col
	arrays[Mesh.ARRAY_INDEX] = idx
	var mesh := ArrayMesh.new()
	if v.size() > 0:
		mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	return mesh


# =============================================================================================
# Aderecos decorativos (juncos e pedras). So aparencia: nao sao vegetacao da regra 5C, nao
# contam como arvore, recurso, obstaculo ou estado; sem colisao. Posicoes deterministicas.

## Juncos e pedras ao longo da linha d'agua do LAGO (compartilhada por A e B). Tudo fica
## dentro do circulo de jogo (raio - margem). Retorna {reeds: [Vector3 xz+yaw], rocks: [...]}.
static func lake_props(centre: Vector2 = LAKE_CENTRE, radius: float = LAKE_RADIUS) -> Dictionary:
	var reeds: Array[Vector4] = []   # x, z, yaw, escala
	var rocks: Array[Vector4] = []
	var steps := 180
	for k in steps:
		var ang := TAU * float(k) / steps
		var dir := Vector2(cos(ang), sin(ang))
		var wl := waterline_point(dir, centre, radius)
		var cluster := ws_noise(rot(wl, 0.7193, 0.6947) / 9.0, 487)
		var jit := ws_hash(k, 3, 491)
		if cluster > 0.6 and jit > 0.35:
			var off := (ws_hash(k, 5, 491) - 0.6) * 1.6   # >0 para dentro d'agua
			var pos := wl - dir * off
			pos += Vector2(-dir.y, dir.x) * (ws_hash(k, 7, 491) - 0.5) * 1.2
			if pos.distance_to(centre) < radius - 0.6:
				reeds.append(Vector4(pos.x, pos.y, ws_hash(k, 9, 491) * TAU, 0.8 + 0.5 * ws_hash(k, 11, 491)))
		var rock_pick := ws_hash(k, 13, 503)
		if rock_pick > 0.86:
			var e_top := radius - centre.distance_to(wl)
			var along := 0.35 + 0.45 * ws_hash(k, 17, 503)
			var pos2 := wl + dir * e_top * along
			if pos2.distance_to(centre) < radius - 0.5:
				rocks.append(Vector4(pos2.x, pos2.y, ws_hash(k, 19, 503) * TAU, 0.45 + 0.6 * ws_hash(k, 23, 503)))
	return {"reeds": reeds, "rocks": rocks}


## Juncos/pedras extras nas margens do rio conceitual (so amostra B).
func river_props() -> Dictionary:
	var reeds: Array[Vector4] = []
	var rocks: Array[Vector4] = []
	for i in range(0, _river.size() - 1, 9):
		var a := _river[i]
		var b := _river[i + 1]
		if a.x > MAP_HALF - 6.0 or a.distance_to(LAKE_CENTRE) < LAKE_RADIUS + 2.0:
			continue
		var tangent := (b - a).normalized()
		var side := Vector2(-tangent.y, tangent.x)
		var u := _river_u[i]
		var hw := river_half_width(u)
		for sgn in [-1.0, 1.0]:
			var pick := ws_hash(i, int(sgn), 509)
			var pos: Vector2 = a + side * sgn * (hw - 0.4 + 0.9 * ws_hash(i, 21 + int(sgn), 509))
			if pick > 0.55:
				reeds.append(Vector4(pos.x, pos.y, ws_hash(i, 31 + int(sgn), 509) * TAU, 0.75 + 0.45 * ws_hash(i, 41 + int(sgn), 509)))
			elif pick < 0.12:
				var pos2: Vector2 = a + side * sgn * (hw + 1.2 + 1.5 * ws_hash(i, 51, 509))
				rocks.append(Vector4(pos2.x, pos2.y, ws_hash(i, 61, 509) * TAU, 0.4 + 0.5 * ws_hash(i, 71, 509)))
	return {"reeds": reeds, "rocks": rocks}


## Moita de juncos low-poly (laminas afinando, cor no vertice: base escura -> ponta clara).
static func reed_mesh() -> ArrayMesh:
	var v := PackedVector3Array()
	var n := PackedVector3Array()
	var c := PackedColorArray()
	var idx := PackedInt32Array()
	var blades := 11
	for b in blades:
		var ang := TAU * (float(b) / blades + 0.37 * ws_hash(b, 1, 521))
		var dir := Vector3(cos(ang), 0.0, sin(ang))
		var root := dir * (0.08 + 0.32 * ws_hash(b, 2, 521))
		var height := 0.9 + 0.7 * ws_hash(b, 3, 521)
		var lean := 0.18 + 0.3 * ws_hash(b, 4, 521)
		var width := 0.11 + 0.05 * ws_hash(b, 5, 521)
		var side := Vector3(-dir.z, 0.0, dir.x)
		var start := v.size()
		var rows := 4
		for r in rows:
			var t := float(r) / (rows - 1)
			var centre := root + dir * lean * t * t + Vector3(0.0, height * t, 0.0)
			var half := width * (1.0 - t * 0.92) * 0.5
			v.append(centre - side * half)
			v.append(centre + side * half)
			var normal := (dir * 0.35 + Vector3.UP * 0.4 + side.cross(Vector3.UP) * 0.2).normalized()
			n.append(normal)
			n.append(normal)
			var tip := Color(0.62, 0.66, 0.36)
			var base := Color(0.28, 0.33, 0.17)
			var col := base.lerp(tip, t)
			c.append(col)
			c.append(col)
		for r in rows - 1:
			var a := start + r * 2
			idx.append_array([a, a + 2, a + 1, a + 1, a + 2, a + 3])
	return _mesh(v, n, idx, PackedVector2Array(), PackedVector2Array(), c)


## Pedra low-poly (icosaedro deformado, faces planas, cor no vertice).
static func rock_mesh() -> ArrayMesh:
	var g := (1.0 + sqrt(5.0)) * 0.5
	var base := [Vector3(-1, g, 0), Vector3(1, g, 0), Vector3(-1, -g, 0), Vector3(1, -g, 0),
		Vector3(0, -1, g), Vector3(0, 1, g), Vector3(0, -1, -g), Vector3(0, 1, -g),
		Vector3(g, 0, -1), Vector3(g, 0, 1), Vector3(-g, 0, -1), Vector3(-g, 0, 1)]
	var faces := [[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11], [1, 5, 9], [5, 11, 4],
		[11, 10, 2], [10, 7, 6], [7, 1, 8], [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9],
		[4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]]
	var pts: Array[Vector3] = []
	for i in base.size():
		var p: Vector3 = (base[i] as Vector3).normalized()
		var k := 0.78 + 0.4 * ws_hash(i, 1, 523)
		pts.append(Vector3(p.x * k * 0.55, p.y * k * 0.32 + 0.08, p.z * k * 0.48))
	var v := PackedVector3Array()
	var n := PackedVector3Array()
	var c := PackedColorArray()
	var idx := PackedInt32Array()
	for f in faces.size():
		var tri: Array = faces[f]
		var a: Vector3 = pts[tri[0]]
		var b: Vector3 = pts[tri[1]]
		var cc: Vector3 = pts[tri[2]]
		var centre := (a + b + cc) / 3.0
		var normal := (b - a).cross(cc - a).normalized()
		if normal.dot(centre - Vector3(0, 0.08, 0)) < 0.0:
			normal = -normal
		var shade := 0.86 + 0.2 * ws_hash(f, 2, 523)
		var col := Color(0.62 * shade, 0.60 * shade, 0.55 * shade)
		var start := v.size()
		for q in [a, b, cc]:
			v.append(q)
			n.append(normal)
			c.append(col)
		_tri(idx, v, start, start + 1, start + 2, normal)
	return _mesh(v, n, idx, PackedVector2Array(), PackedVector2Array(), c)
