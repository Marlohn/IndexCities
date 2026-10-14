## IndexCities - WaterStudy - AMOSTRA C: gerador do lago (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE. Proposta visual NAO integrada; medidas e alturas sao propostas.
##
## Tudo parametrizado pelo centro e pelo raio REAIS do lago (CityState.LakeCentre/LakeRadius);
## nao ha constante de seed. Gera tres malhas, em coordenadas do mundo:
##   ground : o chao do jogo (512 x 512, topo em y = -0,06, laterais ate -1,06) com uma abertura
##            poligonal exatamente no circulo do lago (vertices sobre o circulo). Usa o material
##            real da grama (env_grass_soft) sem espelho: fora do circulo o chao e o mesmo.
##   bed    : barranco vertical NO circulo (do chao ate o fundo) + fundo da bacia. O rebaixo existe
##            so dentro do circulo; fora dele o chao continua plano.
##   water  : superficie plana no nivel d'agua, cobrindo o circulo inteiro (borda = circulo).
## A linha d'agua visivel e, portanto, o proprio circulo de jogo (InWater): nao ha faixa de
## terra dentro da agua nem agua fora do circulo. Sem rio.
##
## SOMENTE APARENCIA: profundidade, prateleira rasa e barranco nao representam nivel, volume,
## vazao, captacao, recurso, area navegavel nem recuo de obra.
##
## Dados por vertice (lidos pelos shaders, sem constantes duplicadas entre shader e script):
##   water: UV  = posicao relativa ao centro do lago (m, XZ); UV2 = (profundidade do fundo sob a agua em m, raio R em m)
##   bed  : UV  = (s, altura acima da agua em m: > 0 no barranco seco); UV2 = (profundidade sob a agua, 0 no fundo / 1 + altura do barranco seco)
extends RefCounted

const MAP_HALF := 256.0
## Topo do chao do jogo: BoxMesh 512 x 1 x 512 em y = -0,56 (CityWorld.Reset).
const GROUND_Y := -0.06
const GROUND_BOTTOM_Y := -1.06
## Topo da "saia" marrom da borda do mapa (CityWorld.Reset: caixa 518 x 4 x 518 em y = -3). O fundo
## do lago fica acima dela para nao aparecer atraves da agua; o rebaixo maximo e ~0,9 m.
const SKIRT_TOP_Y := -1.0

## Parametros visuais propostos (nao sao regra de jogo).
const DEFAULTS := {
	"segments": 192,            # vertices no circulo (erro de corda <= 6 mm com R = 40 m)
	"water_drop_m": 0.15,       # agua abaixo do chao: barranco visivel so do lado oposto a camera
	"edge_depth_min_m": 0.04,   # profundidade junto ao barranco (varia ao longo da margem)
	"edge_depth_max_m": 0.22,
	"shelf_min_m": 0.8,         # largura da prateleira rasa antes do talude (baias e lobos)
	"shelf_max_m": 6.0,
	"deep_m": 0.72,             # fundo; a cor escura vem da absorcao (shader), nao de um poco fundo
	"window_pad_m": 6.0,        # janela quadrada do chao em volta do lago
}

## Aneis do fundo (distancia s a partir da margem, m): finos na margem, largos no centro.
const RINGS_S := [0.0, 0.2, 0.45, 0.75, 1.1, 1.5, 2.0, 2.6, 3.3, 4.1, 5.0, 6.0, 7.1, 8.3, 9.6, 11.0,
	12.6, 15.0, 18.0, 22.0, 26.0, 31.0, 36.0]

var p := {}
var _n_shelf := FastNoiseLite.new()
var _n_lobe := FastNoiseLite.new()
var _n_edge := FastNoiseLite.new()
var _n_floor := FastNoiseLite.new()


func _init(params := {}) -> void:
	p = DEFAULTS.duplicate()
	for k in params:
		p[k] = params[k]
	# Ruido no espaco do MUNDO (fixo, independente de seed): a forma da prateleira depende de onde
	# a margem cai no mapa, entao muda com o lago real de cada seed sem nenhuma constante de seed.
	for cfg in [[_n_shelf, 9101, 1.0 / 34.0], [_n_lobe, 9103, 1.0 / 11.0], [_n_edge, 9107, 1.0 / 7.5], [_n_floor, 9109, 1.0 / 13.0]]:
		var n: FastNoiseLite = cfg[0]
		n.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
		n.seed = cfg[1]
		n.frequency = cfg[2]
		n.fractal_type = FastNoiseLite.FRACTAL_NONE


static func water_y(params := {}) -> float:
	return GROUND_Y - float(params.get("water_drop_m", DEFAULTS["water_drop_m"]))


func _n01(n: FastNoiseLite, q: Vector2) -> float:
	return clampf(0.5 + 0.5 * n.get_noise_2d(q.x, q.y), 0.0, 1.0)


## Largura da prateleira rasa (m) para o ponto da margem q.
func shelf_width(q: Vector2) -> float:
	var t := smoothstep(0.28, 0.72, 0.65 * _n01(_n_shelf, q) + 0.35 * _n01(_n_lobe, q))
	return lerpf(float(p["shelf_min_m"]), float(p["shelf_max_m"]), t)


## Profundidade (m) junto ao barranco para o ponto da margem q.
func edge_depth(q: Vector2) -> float:
	return lerpf(float(p["edge_depth_min_m"]), float(p["edge_depth_max_m"]), smoothstep(0.2, 0.8, _n01(_n_edge, q)))


## Profundidade do fundo sob a agua (m) a uma distancia s da margem, na direcao da margem q.
## Prateleira quase plana, talude e fundo levemente ondulado. Sempre > 0 dentro do circulo.
func depth_at(q: Vector2, s: float, world: Vector2) -> float:
	var d0 := edge_depth(q)
	var w := shelf_width(q)
	# Prateleira quase plana ate ~0,8 w, queda curta (1,5 m + 0,25 w) e fundo levemente ondulado.
	var shelf := d0 + 0.07 * clampf(s / w, 0.0, 1.0)
	var slope := smoothstep(0.8 * w, 1.05 * w + 1.5, s)
	var deep := float(p["deep_m"]) * (0.86 + 0.14 * _n01(_n_floor, world))
	var limit := water_y(p) - SKIRT_TOP_Y - 0.06
	return clampf(lerpf(shelf, deep, slope), 0.03, limit)


func _dir(i: int, n: int) -> Vector2:
	var a := TAU * float(i) / float(n)
	return Vector2(cos(a), sin(a))


## Gera ground/bed/water para o lago real (centro e raio em metros, plano XZ do mundo).
func build(centre: Vector2, radius: float) -> Dictionary:
	var n: int = int(p["segments"])
	var wy := water_y(p)
	var rings: Array[float] = []
	for s in RINGS_S:
		if s < radius - 1.0:
			rings.append(float(s))
	# Profundidade por (anel, segmento): uma unica fonte para o fundo e para a agua.
	var depth := PackedFloat32Array()
	depth.resize(rings.size() * n)
	var centre_depth := 0.0
	for i in n:
		var dir := _dir(i, n)
		var q := centre + dir * radius
		for k in rings.size():
			var world := centre + dir * (radius - rings[k])
			depth[k * n + i] = depth_at(q, rings[k], world)
	for i in n:
		centre_depth += depth[(rings.size() - 1) * n + i] / float(n)
	centre_depth = maxf(centre_depth, 0.03)
	var out := {
		"ground": _ground(centre, radius, n),
		"bed": _bed(centre, radius, n, rings, depth, centre_depth, wy),
		"water": _water(centre, radius, n, rings, depth, centre_depth, wy),
	}
	var dmin := INF
	var dmax := 0.0
	for d in depth:
		dmin = minf(dmin, d)
		dmax = maxf(dmax, d)
	var smin := INF
	var smax := 0.0
	for i in n:
		var w := shelf_width(centre + _dir(i, n) * radius)
		smin = minf(smin, w)
		smax = maxf(smax, w)
	out["stats"] = {
		"centre": [centre.x, centre.y], "radius_m": radius, "segments": n, "rings": rings.size(),
		"water_y": wy, "ground_y": GROUND_Y, "bank_height_m": GROUND_Y - wy,
		"depth_min_m": dmin, "depth_max_m": dmax, "shelf_width_min_m": smin, "shelf_width_max_m": smax,
		"chord_sagitta_m": radius * (1.0 - cos(PI / float(n))),
	}
	return out


func _mesh(verts: PackedVector3Array, normals: PackedVector3Array, uv: PackedVector2Array, uv2: PackedVector2Array, idx: PackedInt32Array) -> ArrayMesh:
	var arr := []
	arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = verts
	arr[Mesh.ARRAY_NORMAL] = normals
	if uv.size() > 0:
		arr[Mesh.ARRAY_TEX_UV] = uv
	if uv2.size() > 0:
		arr[Mesh.ARRAY_TEX_UV2] = uv2
	arr[Mesh.ARRAY_INDEX] = idx
	var m := ArrayMesh.new()
	m.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arr)
	return m


## Chao do jogo com abertura no circulo. Topo em 9 celulas (a central e a janela em volta do lago,
## cuja borda externa so tem os 4 cantos: sem juncao em T com as celulas vizinhas) + 4 laterais.
## Godot: face frontal em sentido horario visto de frente; aqui, visto de cima (+Y).
func _ground(centre: Vector2, radius: float, n: int) -> ArrayMesh:
	var v := PackedVector3Array()
	var nm := PackedVector3Array()
	var idx := PackedInt32Array()
	var s: float = radius + float(p["window_pad_m"])
	var xs := [-MAP_HALF, centre.x - s, centre.x + s, MAP_HALF]
	var zs := [-MAP_HALF, centre.y - s, centre.y + s, MAP_HALF]
	var grid := {}
	for zi in 4:
		for xi in 4:
			grid[Vector2i(xi, zi)] = v.size()
			v.append(Vector3(xs[xi], GROUND_Y, zs[zi]))
			nm.append(Vector3.UP)
	for zi in 3:
		for xi in 3:
			if xi == 1 and zi == 1:
				continue
			var a: int = grid[Vector2i(xi, zi)]
			var c: int = grid[Vector2i(xi + 1, zi + 1)]
			_tri_up(idx, a, grid[Vector2i(xi + 1, zi)], c, v)
			_tri_up(idx, a, c, grid[Vector2i(xi, zi + 1)], v)
	# Janela: cantos (sentido do angulo crescente no plano XZ: +X -> +Z).
	var corner_ids := [grid[Vector2i(2, 2)], grid[Vector2i(1, 2)], grid[Vector2i(1, 1)], grid[Vector2i(2, 1)]]  # 45, 135, 225, 315 graus
	var corner_ang := [PI * 0.25, PI * 0.75, PI * 1.25, PI * 1.75]
	var ring0 := v.size()
	for i in n:
		var d := _dir(i, n)
		v.append(Vector3(centre.x + d.x * radius, GROUND_Y, centre.y + d.y * radius))
		nm.append(Vector3.UP)
	# Cada aresta do poligono (i, i+1) vira um triangulo com o canto "dono" do seu angulo medio;
	# entre dois cantos consecutivos, o triangulo (canto_k, v_m, canto_k+1) fecha o lado.
	var owner := PackedInt32Array()
	owner.resize(n)
	for i in n:
		var a := fposmod(TAU * (float(i) + 0.5) / float(n) - PI * 0.25, TAU)  # 0 no canto de 45 graus
		owner[i] = int(floor(a / (PI * 0.5))) % 4  # lado k: entre corner_ang[k] e corner_ang[k+1]
	for i in n:
		var k := owner[i]
		var a_mid := fposmod(TAU * (float(i) + 0.5) / float(n) - corner_ang[k], TAU)
		var c_id: int = corner_ids[k] if a_mid < PI * 0.25 else corner_ids[(k + 1) % 4]
		_tri_up(idx, ring0 + i, ring0 + (i + 1) % n, c_id, v)
	for k in 4:
		# vertice do circulo mais proximo do meio do lado k
		var mid_ang: float = corner_ang[k] + PI * 0.25
		var m := int(round(mid_ang / TAU * float(n))) % n
		_tri_up(idx, corner_ids[k], ring0 + m, corner_ids[(k + 1) % 4], v)
	# Laterais do chao (iguais as da caixa do jogo): de GROUND_BOTTOM_Y a GROUND_Y.
	var sides := [[Vector3(-MAP_HALF, 0, -MAP_HALF), Vector3(MAP_HALF, 0, -MAP_HALF), Vector3(0, 0, -1)],
		[Vector3(MAP_HALF, 0, -MAP_HALF), Vector3(MAP_HALF, 0, MAP_HALF), Vector3(1, 0, 0)],
		[Vector3(MAP_HALF, 0, MAP_HALF), Vector3(-MAP_HALF, 0, MAP_HALF), Vector3(0, 0, 1)],
		[Vector3(-MAP_HALF, 0, MAP_HALF), Vector3(-MAP_HALF, 0, -MAP_HALF), Vector3(-1, 0, 0)]]
	for sd in sides:
		var a0: Vector3 = sd[0]
		var a1: Vector3 = sd[1]
		var base := v.size()
		for corner in [Vector3(a0.x, GROUND_Y, a0.z), Vector3(a1.x, GROUND_Y, a1.z), Vector3(a1.x, GROUND_BOTTOM_Y, a1.z), Vector3(a0.x, GROUND_BOTTOM_Y, a0.z)]:
			v.append(corner)
			nm.append(sd[2])
		_tri_facing(idx, base, base + 1, base + 2, v, sd[2])
		_tri_facing(idx, base, base + 2, base + 3, v, sd[2])
	return _mesh(v, nm, PackedVector2Array(), PackedVector2Array(), idx)


## Barranco (anel vertical no circulo) + fundo (aneis ate o centro).
func _bed(centre: Vector2, radius: float, n: int, rings: Array[float], depth: PackedFloat32Array, centre_depth: float, wy: float) -> ArrayMesh:
	var v := PackedVector3Array()
	var nm := PackedVector3Array()
	var uv := PackedVector2Array()
	var uv2 := PackedVector2Array()
	var idx := PackedInt32Array()
	# Barranco: topo no chao, base no fundo junto a margem; normal para o centro do lago.
	var wall0 := v.size()
	for i in n:
		var d := _dir(i, n)
		var x := centre.x + d.x * radius
		var z := centre.y + d.y * radius
		var bottom := wy - depth[i]
		v.append(Vector3(x, GROUND_Y, z))
		v.append(Vector3(x, bottom, z))
		nm.append(Vector3(-d.x, 0, -d.y))
		nm.append(Vector3(-d.x, 0, -d.y))
		uv.append(Vector2(0.0, GROUND_Y - wy))
		uv.append(Vector2(0.0, bottom - wy))
		uv2.append(Vector2(wy - GROUND_Y, 1.0 + GROUND_Y - wy))
		uv2.append(Vector2(depth[i], 1.0 + GROUND_Y - wy))
	for i in n:
		var j := (i + 1) % n
		var a := wall0 + 2 * i
		var b := wall0 + 2 * j
		_tri_facing(idx, a, b, b + 1, v, nm[a])
		_tri_facing(idx, a, b + 1, a + 1, v, nm[a])
	# Fundo.
	var bed0 := v.size()
	for k in rings.size():
		for i in n:
			var d := _dir(i, n)
			var r := radius - rings[k]
			v.append(Vector3(centre.x + d.x * r, wy - depth[k * n + i], centre.y + d.y * r))
			nm.append(Vector3.UP)
			uv.append(Vector2(rings[k], -depth[k * n + i]))
			uv2.append(Vector2(depth[k * n + i], 0.0))
	var cid := v.size()
	v.append(Vector3(centre.x, wy - centre_depth, centre.y))
	nm.append(Vector3.UP)
	uv.append(Vector2(radius, -centre_depth))
	uv2.append(Vector2(centre_depth, 0.0))
	_rings_indices(idx, bed0, n, rings.size(), cid, v)
	_smooth_normals(v, nm, idx, bed0)
	return _mesh(v, nm, uv, uv2, idx)


## Superficie da agua: mesmos aneis e segmentos do fundo (a profundidade por vertice casa).
func _water(centre: Vector2, radius: float, n: int, rings: Array[float], depth: PackedFloat32Array, centre_depth: float, wy: float) -> ArrayMesh:
	var v := PackedVector3Array()
	var nm := PackedVector3Array()
	var uv := PackedVector2Array()
	var uv2 := PackedVector2Array()
	var idx := PackedInt32Array()
	for k in rings.size():
		for i in n:
			var d := _dir(i, n)
			var r := radius - rings[k]
			v.append(Vector3(centre.x + d.x * r, wy, centre.y + d.y * r))
			nm.append(Vector3.UP)
			uv.append(d * r)
			uv2.append(Vector2(depth[k * n + i], radius))
	var cid := v.size()
	v.append(Vector3(centre.x, wy, centre.y))
	nm.append(Vector3.UP)
	uv.append(Vector2.ZERO)
	uv2.append(Vector2(centre_depth, radius))
	_rings_indices(idx, 0, n, rings.size(), cid, v)
	return _mesh(v, nm, uv, uv2, idx)


func _rings_indices(idx: PackedInt32Array, base: int, n: int, nr: int, cid: int, v: PackedVector3Array) -> void:
	for k in nr - 1:
		for i in n:
			var j := (i + 1) % n
			var a := base + k * n + i
			var b := base + k * n + j
			var c := base + (k + 1) * n + j
			var d := base + (k + 1) * n + i
			_tri_up(idx, a, b, c, v)
			_tri_up(idx, a, c, d, v)
	for i in n:
		_tri_up(idx, base + (nr - 1) * n + i, base + (nr - 1) * n + (i + 1) % n, cid, v)


## Triangulo com a face frontal para +Y (corrige a ordem pelo sinal da normal geometrica).
func _tri_up(idx: PackedInt32Array, a: int, b: int, c: int, v: PackedVector3Array) -> void:
	_tri_facing(idx, a, b, c, v, Vector3.UP)


## Godot: frente em sentido horario visto de frente, i.e. (b - a) x (c - a) aponta para TRAS.
func _tri_facing(idx: PackedInt32Array, a: int, b: int, c: int, v: PackedVector3Array, facing: Vector3) -> void:
	var nrm := (v[b] - v[a]).cross(v[c] - v[a])
	if nrm.dot(facing) > 0.0:
		idx.append_array([a, c, b])
	else:
		idx.append_array([a, b, c])


func _smooth_normals(v: PackedVector3Array, nm: PackedVector3Array, idx: PackedInt32Array, from: int) -> void:
	var acc := PackedVector3Array()
	acc.resize(v.size())
	for t in range(0, idx.size(), 3):
		var a := idx[t]
		var b := idx[t + 1]
		var c := idx[t + 2]
		if a < from:
			continue
		var f := (v[c] - v[a]).cross(v[b] - v[a])  # ordem horaria da Godot -> normal frontal
		acc[a] += f
		acc[b] += f
		acc[c] += f
	for i in range(from, v.size()):
		var nn := acc[i]
		nm[i] = nn.normalized() if nn.length() > 1e-9 else Vector3.UP
		if nm[i].y < 0.0:
			nm[i] = -nm[i]
