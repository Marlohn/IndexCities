## IndexCities - WaterStudy - Lago irregular: gerador do lago irregular com margem em relevo.
## Gerador visual integrado a partir da amostra D. Estetica pendente de revisao humana:
## alturas, larguras, perfis e cores sao PROPOSTAS, nao decisoes de produto.
##
## USO (GDScript):
##   var gen = preload("res://Assets/RecoveryEnvironment/Lake/Source/ws_lake_d.gd").new()
##   var out: Dictionary = gen.build(centre, boundary, {})   # params opcionais (sobrescrevem DEFAULTS)
## USO (C#), sem portar o codigo:
##   var gen = (GodotObject)GD.Load<GDScript>("res://Assets/RecoveryEnvironment/Lake/Source/ws_lake_d.gd").New();
##   var out = (Godot.Collections.Dictionary)gen.Call("build", centre, boundary, new Godot.Collections.Dictionary());
##
## ENTRADA
##   centre   : Vector2 (x, z), metros, mundo. O poligono deve ser star-shaped em relacao a ele.
##   boundary : PackedVector2Array (x, z), metros, mundo: poligono SIMPLES da linha d'agua, em ordem
##              angular em volta do centro, sem ponto final repetido (sentido do angulo crescente de
##              +X para +Z; se vier no sentido oposto, e invertido e `boundary_reversed` = true).
##              E a area que o root considera agua. Este gerador NAO a redefine nem a desenha de novo:
##              os vertices do poligono sao copiados tal como vieram para o anel da margem.
## SAIDA (Dictionary)
##   ok, errors
##   ground : ArrayMesh do chao do mapa inteiro (512 x 512, topo em ground_y, laterais ate -1,06) com
##            abertura EXATAMENTE no poligono. Superficie 0 = chao plano (material real env_grass_soft);
##            superficie 1 = faixa de margem em relevo, toda FORA do poligono (ws_d_shore).
##   bed    : ArrayMesh do fundo, todo DENTRO do poligono (ws_d_bed, opaco).
##   water  : ArrayMesh da superficie da agua no nivel water_y, cobrindo exatamente o poligono
##            (ws_d_water, transparente).
##   boundary, boundary_reversed, band_outer (PackedVector2Array do limite externo do relevo),
##   water_y, ground_y, bed_min_y, materials (caminhos), surfaces, params (efetivos), stats.
## Depois do build, o objeto responde terrain_height(q), surface_height(q), signed_distance(q) e
## in_polygon(q) com as mesmas funcoes que geraram as malhas (para o root integrar picking e
## posicionamento). Nenhuma malha tem colisao, picking, conector ou regra de validade.
##
## PIVO E ORIENTACAO: todas as malhas ja estao em coordenadas do MUNDO (Y para cima, metros); o no
## que as recebe fica na origem, sem rotacao nem escala.
##
## O anel da margem (vertices do poligono, y = water_y) e o MESMO nas tres malhas: mesma posicao em
## float32, sem vertice extra nas arestas (sem juncao em T) e sem geometria duplicada fora da margem.
## Terreno continuo (C1) atravessando a linha d'agua: a inclinacao da margem fora do poligono e a do
## fundo logo dentro dele sao iguais na linha.
##
## SOMENTE APARENCIA: nivel, profundidade, relevo, raso e cores nao representam volume, vazao,
## captacao, recurso, area navegavel, recuo de obra nem vegetacao da regra ambiental.
##
## Dados por vertice (lidos pelos shaders; nenhuma constante geometrica duplicada nos shaders):
##   ground (sup. 1) e bed : UV  = (distancia assinada ao poligono em m: > 0 fora, < 0 dentro;
##                                  altura em relacao a agua em m)
##                           UV2 = (carater da margem 0 suave .. 1 barranco; largura da faixa em m)
##   water                 : UV  = vetor (dx, dz) ate o ponto mais proximo da margem (m)
##                           UV2 = (profundidade do fundo sob o ponto em m;
##                                  altura que a margem mais proxima atinge em 1,2 m, em m)
extends RefCounted

const ROOT := "res://Assets/RecoveryEnvironment/Lake"
const MAT_GRASS := "res://Assets/RecoveryEnvironment/Materials/env_grass_soft.tres"
const MAT_SHORE := ROOT + "/Materials/ws_d_shore.tres"
const MAT_BED := ROOT + "/Materials/ws_d_bed.tres"
const MAT_WATER := ROOT + "/Materials/ws_d_water.tres"
const SURFACE_GRASS := 0
const SURFACE_SHORE := 1

const DEFAULTS := {
	# Mapa do jogo (CityWorld.Reset): chao BoxMesh 512 x 1 x 512 com topo em -0,06 e base em -1,06;
	# "saia" marrom 518 x 4 x 518 com topo em -1,0 (fica por baixo do chao inteiro).
	"map_half": 256.0,
	"ground_y": -0.06,
	"ground_bottom_y": -1.06,
	"skirt_top_y": -1.0,
	# Folga minima entre o ponto mais fundo do lago e o topo da saia: a saia nao pode aparecer
	# atraves da agua nem cortar o fundo.
	"skirt_clearance_m": 0.08,
	# ---- propostas visuais (nao sao regra de jogo) ----
	# Nivel d'agua abaixo do chao plano (altura do barranco). Ver a folha de nivel no README.
	"water_drop_m": 0.62,
	# Fundo mais fundo abaixo da agua. A cor do fundo vem da absorcao no shader, nao de um poco:
	# com a saia em -1,0, queda + fundo + folga <= 0,94 m.
	"depth_max_m": 0.2,
	# Faixa de relevo FORA do poligono (largura medida na perpendicular a margem). Na maior parte da
	# margem, rampa de grama larga e suave; em trechos (carater de barranco), barranco curto que nasce
	# na agua. Os trechos sao manchas de ruido no mundo (~10-20 m), nunca a margem inteira.
	"soft_width_min_m": 3.5,
	"soft_width_max_m": 7.5,
	"cut_width_min_m": 1.6,
	"cut_width_max_m": 2.8,
	"cut_noise_low": 0.44,
	"cut_noise_high": 0.72,
	# Perfil (t = distancia / largura, 0 na agua, 1 no chao plano), misturado pelo carater:
	#   suave    : 1 - (1 - t)^k (grama descendo ate a agua);
	#   barranco : face reta que nasce na agua e sobe ate a crista em t = cut_crest (arredondada em
	#              +-cut_round da crista), depois o alto plano. Mais inclinado junto a agua, como um
	#              barranco erodido, e com a quebra nitida no alto.
	"bank_power_gentle": 1.6,
	"cut_crest_min": 0.38,
	"cut_crest_max": 0.62,
	"cut_round": 0.28,
	# Fundo: continua a margem (mesma inclinacao na linha d'agua) e depois desce ate o fundo maximo:
	# junto ao barranco, pe raso curto e queda logo; nas margens suaves, rampa longa ou prateleira rasa.
	"cut_toe_depth_m": 0.03,
	"cut_drop_start_m": 0.25,
	"cut_drop_len_m": 0.8,
	"gentle_drop_len_m": 3.0,
	"shelf_depth_m": 0.06,
	"shelf_len_min_m": 1.5,
	"shelf_len_max_m": 5.0,
	# Altura da vegetacao baixa da margem refletida pela agua (m).
	"reflect_veg_m": 0.1,
	# Janela quadrada do chao em volta do lago (folga alem do anel externo, m).
	"window_pad_m": 4.0,
	# Espacamento maximo entre raios na margem: cada aresta do poligono ganha raios intermediarios
	# nos aneis internos e externos (nunca na propria linha d'agua).
	"ray_spacing_m": 1.25,
	"assign_materials": true,
}

## Aneis do fundo a partir da margem (m, distancia ao longo do raio), finos junto a margem.
const BED_X := [0.1, 0.22, 0.37, 0.56, 0.8, 1.1, 1.48, 1.95, 2.55, 3.3, 4.25, 5.45, 7.0, 9.0, 11.5]
## Depois dos aneis absolutos: fracoes do raio que sobra ate o centro (do centro para fora).
const BED_FRAC := [0.1, 0.27, 0.48, 0.72]

var p := {}
var water_y := 0.0
var ground_y := 0.0
var drop := 0.0
var depth_limit := 0.0
var _centre := Vector2.ZERO
var _poly := PackedVector2Array()
var _n := 0
var _ea := PackedVector2Array()
var _ed := PackedVector2Array()
var _el2 := PackedFloat32Array()
var _ang := PackedFloat64Array()
var _n_w1 := FastNoiseLite.new()
var _n_w2 := FastNoiseLite.new()
var _n_k := FastNoiseLite.new()
var _n_s := FastNoiseLite.new()
var _n_s2 := FastNoiseLite.new()
var _n_d := FastNoiseLite.new()
var _n_m := FastNoiseLite.new()


func _init() -> void:
	p = DEFAULTS.duplicate()
	# Ruido no espaco do MUNDO, fixo e independente de seed: a forma da margem depende de onde o
	# poligono cai no mapa. Nao ha constante de seed nem geracao da area de agua aqui.
	for cfg in [[_n_w1, 7301, 1.0 / 31.0], [_n_w2, 7307, 1.0 / 12.0], [_n_k, 7309, 1.0 / 21.0],
			[_n_s, 7321, 1.0 / 23.0], [_n_s2, 7331, 1.0 / 9.0], [_n_d, 7333, 1.0 / 15.0], [_n_m, 7349, 1.0 / 13.0]]:
		var n: FastNoiseLite = cfg[0]
		n.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
		n.seed = cfg[1]
		n.frequency = cfg[2]
		n.fractal_type = FastNoiseLite.FRACTAL_NONE


# ------------------------------------------------------------------------------------ entrada

## Confere o poligono: >= 8 vertices, nenhum no centro, e cada aresta gira no mesmo sentido em
## volta do centro, somando exatamente uma volta (isso garante poligono simples e star-shaped).
static func check_boundary(centre: Vector2, boundary: PackedVector2Array) -> Dictionary:
	var errors := []
	var n := boundary.size()
	if n < 8:
		return {"ok": false, "errors": ["poligono com %d vertices (minimo 8)" % n]}
	var area := 0.0
	for i in n:
		var a := boundary[i]
		var b := boundary[(i + 1) % n]
		area += a.x * b.y - b.x * a.y
	area *= 0.5
	var poly := boundary.duplicate()
	if area < 0.0:
		poly.reverse()
	var total := 0.0
	for i in n:
		var a := poly[i] - centre
		var b := poly[(i + 1) % n] - centre
		if a.length() < 0.5:
			errors.append("vertice %d a menos de 0,5 m do centro" % i)
		var cr := a.x * b.y - a.y * b.x
		if cr <= 1e-6:
			errors.append("aresta %d nao gira em volta do centro (nao e star-shaped)" % i)
		total += atan2(cr, a.dot(b))
	if absf(total - TAU) > 1e-3:
		errors.append("o poligono da %.5f rad em volta do centro (esperado 2 pi)" % total)
	return {"ok": errors.is_empty(), "errors": errors, "reversed": area < 0.0, "area_m2": absf(area), "polygon": poly}


# ------------------------------------------------------------------------------------ campos

func _n01(n: FastNoiseLite, q: Vector2) -> float:
	return clampf(0.5 + 0.5 * n.get_noise_2d(q.x, q.y), 0.0, 1.0)


## Carater da margem em q: 0 = suave (rampa de grama), 1 = barranco. Manchas de ruido no mundo.
func bank_kappa(q: Vector2) -> float:
	return smoothstep(float(p["cut_noise_low"]), float(p["cut_noise_high"]), _n01(_n_k, q))


func _width_noise(q: Vector2) -> float:
	return smoothstep(0.3, 0.72, 0.62 * _n01(_n_w1, q) + 0.38 * _n01(_n_w2, q))


## Largura da rampa suave (m) em q.
func soft_width(q: Vector2) -> float:
	return lerpf(float(p["soft_width_min_m"]), float(p["soft_width_max_m"]), _width_noise(q))


## Largura do barranco (m) em q: a crista fica a cut_crest * esta largura da agua.
func cut_width(q: Vector2) -> float:
	return lerpf(float(p["cut_width_min_m"]), float(p["cut_width_max_m"]), _width_noise(q))


## Largura representativa da faixa (m) em q (so para atributo e estatistica).
func bank_width(q: Vector2) -> float:
	return lerpf(soft_width(q), cut_width(q), _cut_mix(bank_kappa(q)))


## Mistura suave -> barranco (o carater perto de 0 ou 1 fica puro).
func _cut_mix(kappa: float) -> float:
	return smoothstep(0.08, 0.92, kappa)


## Posicao da crista do barranco (fracao da largura do barranco) em q.
func _cut_crest(q: Vector2) -> float:
	return lerpf(float(p["cut_crest_min"]), float(p["cut_crest_max"]), _n01(_n_m, q))


## Rampa com teto arredondado (C1): u ate 1 - r, curva ate 1 em 1 + r, depois 1.
static func _smooth_clamp(u: float, r: float) -> float:
	if u <= 1.0 - r:
		return u
	if u >= 1.0 + r:
		return 1.0
	var k := 1.0 + r - u
	return 1.0 - k * k / (4.0 * r)


## Fracao da queda ja vencida a uma distancia d >= 0 da agua: mistura das ALTURAS de dois perfis,
## cada um com a sua largura (a crista do barranco fica sempre perto da agua).
##   suave    : 1 - (1 - d / largura_suave)^k
##   barranco : face reta desde a agua ate a crista (largura_barranco * cut_crest), crista
##              arredondada em +-cut_round, depois plano.
func bank_profile(q: Vector2, d: float) -> float:
	var soft := 1.0 - pow(1.0 - clampf(d / soft_width(q), 0.0, 1.0), float(p["bank_power_gentle"]))
	var cut := _smooth_clamp(maxf(d, 0.0) / (cut_width(q) * _cut_crest(q)), float(p["cut_round"]))
	return lerpf(soft, cut, _cut_mix(bank_kappa(q)))


## Inclinacao da margem (m/m) na linha d'agua, do lado de fora.
func land_slope0(q: Vector2) -> float:
	var soft := float(p["bank_power_gentle"]) / soft_width(q)
	var cut := 1.0 / (cut_width(q) * _cut_crest(q))
	return drop * lerpf(soft, cut, _cut_mix(bank_kappa(q)))


## Distancia da agua a partir da qual o terreno ja e (quase) o chao plano. Continua no espaco, para os
## aneis externos nao saltarem entre raios vizinhos; o resto de rampa suave (< 1 cm onde o barranco
## domina) e absorvido pelo anel externo, que avanca ate o chao plano.
func flat_distance(q: Vector2) -> float:
	var cut := cut_width(q) * _cut_crest(q) * (1.0 + float(p["cut_round"]))
	return lerpf(soft_width(q), cut, smoothstep(0.85, 1.0, _cut_mix(bank_kappa(q))))


## Altura (y do mundo) do terreno a uma distancia d >= 0 FORA do poligono.
func land_height(q: Vector2, d: float) -> float:
	return water_y + drop * bank_profile(q, d)


## Profundidade (m, > 0) do fundo a uma distancia x >= 0 DENTRO do poligono. A inclinacao inicial e
## a mesma da margem do lado de fora (terreno C1 na linha d'agua). Depois: junto ao barranco, um pe
## raso curto e a queda logo adiante; nas margens suaves, rampa longa ou uma prateleira rasa.
func bed_depth(q: Vector2, x: float) -> float:
	var kappa := bank_kappa(q)
	var cm := _cut_mix(kappa)
	var s0 := maxf(land_slope0(q), 0.005)
	var deep := float(p["depth_max_m"]) * lerpf(0.8, 1.0, _n01(_n_d, q))
	var shelf := smoothstep(0.4, 0.66, _n01(_n_s, q)) * (1.0 - kappa)
	var a_depth := lerpf(lerpf(deep, float(p["shelf_depth_m"]), shelf), float(p["cut_toe_depth_m"]), cm)
	var x0 := lerpf(lerpf(float(p["shelf_len_min_m"]), float(p["shelf_len_max_m"]), _n01(_n_s2, q)), float(p["cut_drop_start_m"]), cm)
	var drop_len := lerpf(float(p["gentle_drop_len_m"]), float(p["cut_drop_len_m"]), cm)
	var a := a_depth * (1.0 - exp(-s0 * x / a_depth))
	var b := (deep - a_depth) * smoothstep(x0, x0 + drop_len, x)
	return clampf(a + b, 0.0, depth_limit)


## Altura que a margem atinge a 1,2 m da agua (+ vegetacao baixa): usada so no reflexo da margem.
func bank_reflect_height(q: Vector2) -> float:
	return drop * bank_profile(q, 1.2) + float(p["reflect_veg_m"])


# ------------------------------------------------------------------------------------ poligono

func _prepare_edges() -> void:
	_ea.resize(_n)
	_ed.resize(_n)
	_el2.resize(_n)
	_ang.resize(_n)
	for i in _n:
		var a := _poly[i]
		var d := _poly[(i + 1) % _n] - a
		_ea[i] = a
		_ed[i] = d
		_el2[i] = maxf(d.length_squared(), 1e-12)
		var v := a - _centre
		var ang := atan2(v.y, v.x)
		_ang[i] = ang if i == 0 else _ang[0] + fposmod(ang - _ang[0], TAU)


## Ponto da linha d'agua (poligono) mais proximo de q.
func nearest_on_boundary(q: Vector2) -> Vector2:
	var best := INF
	var bp := Vector2.ZERO
	for i in _n:
		var a := _ea[i]
		var d := _ed[i]
		var pt := a + d * clampf((q - a).dot(d) / _el2[i], 0.0, 1.0)
		var dd := q.distance_squared_to(pt)
		if dd < best:
			best = dd
			bp = pt
	return bp


## Ponto dentro do poligono (exato para poligono star-shaped em relacao ao centro): acha a aresta
## cujo setor angular contem q e testa de que lado dela q esta. Na linha d'agua conta como fora.
## (Geometry2D.is_point_in_polygon foi evitado: errou um ponto fora do retangulo envolvente.)
func in_polygon(q: Vector2) -> bool:
	var v := q - _centre
	if v.length_squared() < 1e-12:
		return true
	var a := _ang[0] + fposmod(atan2(v.y, v.x) - _ang[0], TAU)
	var lo := 0
	var hi := _n - 1
	while lo < hi:
		var mid := (lo + hi + 1) >> 1
		if _ang[mid] <= a:
			lo = mid
		else:
			hi = mid - 1
	var p0 := _ea[lo]
	var e := _ed[lo]
	var side_q := e.x * (q.y - p0.y) - e.y * (q.x - p0.x)
	var side_c := e.x * (_centre.y - p0.y) - e.y * (_centre.x - p0.x)
	return side_q * side_c > 0.0


## Distancia assinada ao poligono (m): > 0 fora, < 0 dentro.
func signed_distance(q: Vector2) -> float:
	var d := q.distance_to(nearest_on_boundary(q))
	return -d if in_polygon(q) else d


## Altura do terreno (fundo dentro do poligono, margem e chao fora). Mesmas funcoes das malhas.
func terrain_height(q: Vector2) -> float:
	var sd := signed_distance(q)
	if sd < 0.0:
		return water_y - bed_depth(q, -sd)
	return land_height(q, sd)


## Altura da superficie visivel: a agua dentro do poligono, o terreno fora.
func surface_height(q: Vector2) -> float:
	return water_y if in_polygon(q) else terrain_height(q)


## Amostra o raio a partir do vertice da margem (passo de 0,2 m) ate o terreno ficar plano:
## devolve e (m ao longo do raio) e d (distancia verdadeira a agua) em cada amostra.
func _march(start: Vector2, dir: Vector2) -> Dictionary:
	var es := PackedFloat32Array([0.0])
	var ds := PackedFloat32Array([0.0])
	var e := 0.0
	while e < 60.0:
		e += 0.2
		var q := start + dir * e
		var d := q.distance_to(nearest_on_boundary(q))
		es.append(e)
		ds.append(d)
		if d >= flat_distance(q):
			return {"e": es, "d": ds}
	return {}


## Distancias a agua em que ficam os aneis da faixa (mesma quantidade em todos os raios): ao longo da
## face do barranco e densos em volta da crista (a crista cai sempre no mesmo anel em raios vizinhos),
## depois espalhados ate o chao plano.
func band_targets(q: Vector2) -> PackedFloat32Array:
	var dc := cut_width(q) * _cut_crest(q)
	var rc := float(p["cut_round"]) * dc
	var near := [0.08 * dc, 0.3 * dc, 0.6 * dc, dc - rc, dc - 0.45 * rc, dc, dc + 0.45 * rc, dc + rc]
	var top: float = near[near.size() - 1]
	var far := maxf(flat_distance(q), top + 0.2)
	var out := PackedFloat32Array()
	for x in near:
		out.append(float(x))
	for f in [0.12, 0.26, 0.42, 0.6, 0.8, 1.0]:
		out.append(lerpf(top, far, float(f)))
	return out


## Primeiro e em que a amostragem do raio atinge a distancia alvo (interpolacao linear).
func _e_at(m: Dictionary, target: float) -> float:
	var es: PackedFloat32Array = m["e"]
	var ds: PackedFloat32Array = m["d"]
	for k in range(1, es.size()):
		if ds[k] >= target:
			var d0 := ds[k - 1]
			var f := clampf((target - d0) / maxf(ds[k] - d0, 1e-6), 0.0, 1.0)
			return lerpf(es[k - 1], es[k], f)
	return es[es.size() - 1]


# ------------------------------------------------------------------------------------ build

func build(centre: Vector2, boundary: PackedVector2Array, params := {}) -> Dictionary:
	var t_start := Time.get_ticks_usec()
	p = DEFAULTS.duplicate()
	for k in params:
		p[k] = params[k]
	ground_y = float(p["ground_y"])
	drop = float(p["water_drop_m"])
	water_y = ground_y - drop
	depth_limit = water_y - (float(p["skirt_top_y"]) + float(p["skirt_clearance_m"]))
	var chk := check_boundary(centre, boundary)
	if not chk["ok"]:
		return {"ok": false, "errors": chk["errors"]}
	if drop <= 0.0 or depth_limit < 0.05:
		return {"ok": false, "errors": ["nivel d'agua incompativel com o chao e a saia (queda %.3f, folga %.3f)" % [drop, depth_limit]]}
	_centre = centre
	_poly = chk["polygon"]
	_n = _poly.size()
	_prepare_edges()

	# Raios: do centro por cada vertice do poligono e por pontos intermediarios das arestas (m por
	# aresta, para os aneis internos e externos ficarem com espacamento <= ~1,25 m). O anel da margem
	# usa SO os vertices do poligono (sem vertice no meio das arestas: sem juncao em T na linha d'agua).
	# Todos os aneis usam os mesmos raios, entao as faixas entre aneis nunca se cruzam (star-shaped).
	var longest := 0.0
	for i in _n:
		longest = maxf(longest, _poly[i].distance_to(_poly[(i + 1) % _n]))
	var m_sub := clampi(int(ceil(longest / float(p["ray_spacing_m"]))), 1, 6)
	var n_rays := _n * m_sub
	var shore_pts := PackedVector2Array()
	var dirs := PackedVector2Array()
	var radii := PackedFloat32Array()
	var r_min := INF
	for k in n_rays:
		var i := k / m_sub
		var f := float(k % m_sub) / float(m_sub)
		var sp := _poly[i] if k % m_sub == 0 else _poly[i].lerp(_poly[(i + 1) % _n], f)
		var v := sp - centre
		shore_pts.append(sp)
		radii.append(v.length())
		dirs.append(v / v.length())
		r_min = minf(r_min, v.length())
	# Faixa: cada raio e amostrado ate o chao plano; os aneis caem em distancias fixas do perfil.
	var n_band := 14
	var band_e := []  # por raio: PackedFloat32Array com n_band distancias ao longo do raio
	var outer_e := PackedFloat32Array()
	outer_e.resize(n_rays)
	for k in n_rays:
		var mr := _march(shore_pts[k], dirs[k])
		if mr.is_empty():
			return {"ok": false, "errors": ["faixa de margem sem fim no raio %d" % k]}
		var tg := band_targets(shore_pts[k])
		var row := PackedFloat32Array()
		var last := 0.0
		for j in n_band:
			var e := maxf(_e_at(mr, tg[j]), last + 0.03)
			row.append(e)
			last = e
		band_e.append(row)
		outer_e[k] = row[n_band - 1]
	# Anel externo: no chao plano com folga, suavizado (maximo dos vizinhos); se ali o terreno ainda
	# nao chegou ao chao plano (os campos variam no espaco), avanca ate chegar.
	var outer_pushed := 0
	var reach := 2 * m_sub
	for k in n_rays:
		var mx := 0.0
		for j in range(-reach, reach + 1):
			mx = maxf(mx, outer_e[(k + j + n_rays) % n_rays])
		var e := maxf(mx, (band_e[k] as PackedFloat32Array)[n_band - 2] + 0.05) + 0.5
		var q := shore_pts[k] + dirs[k] * e
		while land_height(q, q.distance_to(nearest_on_boundary(q))) < ground_y - 1e-5 and e < 80.0:
			e += 0.25
			q = shore_pts[k] + dirs[k] * e
			outer_pushed += 1
		var row: PackedFloat32Array = band_e[k]
		row[n_band - 1] = e
		band_e[k] = row

	# Niveis (de dentro para fora): fracoes ate o centro, aneis absolutos do fundo, margem (so os
	# vertices do poligono), faixa, circulo.
	var bed_x := []
	for x in BED_X:
		if float(x) > 0.0 and float(x) <= 0.6 * r_min:
			bed_x.append(float(x))
	var x_last: float = bed_x[bed_x.size() - 1]
	var levels := []  # [tipo, valor]
	for f in BED_FRAC:
		levels.append(["frac", float(f)])
	for j in range(bed_x.size() - 1, -1, -1):
		levels.append(["bed", bed_x[j]])
	levels.append(["shore", 0.0])
	var l_shore := levels.size() - 1
	for j in n_band:
		levels.append(["band", float(j)])
	var l_band_out := levels.size() - 1
	var r_circle := 0.0
	for k in n_rays:
		r_circle = maxf(r_circle, radii[k] + (band_e[k] as PackedFloat32Array)[n_band - 1])
	r_circle += 1.5
	levels.append(["circle", r_circle])
	var l_circle := levels.size() - 1
	var n_levels := levels.size()

	# Posicoes e atributos (por nivel: lista de indices em pos; o centro no fim).
	var pos := PackedVector3Array()
	var uv := PackedVector2Array()
	var uv2 := PackedVector2Array()
	var lvl := []  # por nivel: PackedInt32Array de indices
	var bed_min_y := water_y
	for l in n_levels:
		var kind: String = levels[l][0]
		var val: float = levels[l][1]
		var ids := PackedInt32Array()
		var count := _n if kind == "shore" else n_rays
		for k in count:
			var q: Vector2
			var y: float
			var sd: float
			if kind == "shore":
				q = _poly[k]
				y = water_y
				sd = 0.0
			elif kind == "frac" or kind == "bed":
				if kind == "frac":
					q = centre + dirs[k] * ((radii[k] - x_last) * val)
				else:
					q = shore_pts[k] - dirs[k] * val
				var x := q.distance_to(nearest_on_boundary(q))
				y = water_y - bed_depth(q, x)
				sd = -x
			elif kind == "band":
				q = shore_pts[k] + dirs[k] * (band_e[k] as PackedFloat32Array)[int(val)]
				sd = q.distance_to(nearest_on_boundary(q))
				# O anel externo e a juncao com o chao plano: exatamente no topo do chao.
				y = ground_y if int(val) == n_band - 1 else land_height(q, sd)
			else:
				q = centre + dirs[k] * val
				sd = q.distance_to(nearest_on_boundary(q))
				y = ground_y
			bed_min_y = minf(bed_min_y, y)
			ids.append(pos.size())
			pos.append(Vector3(q.x, y, q.y))
			uv.append(Vector2(sd, y - water_y))
			uv2.append(Vector2(bank_kappa(q), bank_width(q)))
		lvl.append(ids)
	var cid := pos.size()
	var c_dist := centre.distance_to(nearest_on_boundary(centre))
	var c_depth := bed_depth(centre, c_dist)
	pos.append(Vector3(centre.x, water_y - c_depth, centre.y))
	uv.append(Vector2(-c_dist, -c_depth))
	uv2.append(Vector2(bank_kappa(centre), bank_width(centre)))
	bed_min_y = minf(bed_min_y, water_y - c_depth)

	# Triangulos por regiao: entre niveis com o mesmo numero de vertices, quadrilateros com a diagonal
	# valida; entre a margem (n vertices) e um nivel subdividido (n * m), leques por aresta.
	var tri_bed := PackedInt32Array()
	var tri_band := PackedInt32Array()
	var tri_trans := PackedInt32Array()
	for l in n_levels - 1:
		var strip := PackedInt32Array()
		_stitch(strip, pos, lvl[l], lvl[l + 1], m_sub)
		if l < l_shore:
			tri_bed.append_array(strip)
		elif l < l_band_out:
			tri_band.append_array(strip)
		else:
			tri_trans.append_array(strip)
	var inner: PackedInt32Array = lvl[0]
	for k in inner.size():
		tri_bed.append_array(_tri_up(inner[k], inner[(k + 1) % inner.size()], cid, pos))

	# Normais conjuntas (fundo + margem + transicao): a mesma normal na linha d'agua das duas malhas.
	var nrm := PackedVector3Array()
	nrm.resize(pos.size())
	for tris in [tri_bed, tri_band, tri_trans]:
		for t in range(0, tris.size(), 3):
			var a: int = tris[t]
			var b: int = tris[t + 1]
			var c: int = tris[t + 2]
			var f := (pos[c] - pos[a]).cross(pos[b] - pos[a])  # ordem horaria da Godot -> normal frontal
			nrm[a] += f
			nrm[b] += f
			nrm[c] += f
	for i in nrm.size():
		nrm[i] = nrm[i].normalized() if nrm[i].length() > 1e-12 else Vector3.UP
	for i in (lvl[l_circle] as PackedInt32Array):
		nrm[i] = Vector3.UP

	# ---------------------------------------------------------------- malhas
	var bed_mesh := _sub_mesh(tri_bed, pos, nrm, uv, uv2, null)
	var shore := _sub_arrays(tri_band, pos, nrm, uv, uv2, null)
	# Agua: mesma grade do fundo, plana no nivel d'agua.
	var wpos := pos.duplicate()
	var wuv := PackedVector2Array()
	var wuv2 := PackedVector2Array()
	wuv.resize(pos.size())
	wuv2.resize(pos.size())
	for l in range(0, l_shore + 1):
		for idx in (lvl[l] as PackedInt32Array):
			var q := Vector2(pos[idx].x, pos[idx].z)
			var nb := q if l == l_shore else nearest_on_boundary(q)
			wpos[idx] = Vector3(q.x, water_y, q.y)
			wuv[idx] = nb - q
			wuv2[idx] = Vector2(water_y - pos[idx].y, bank_reflect_height(nb))
	var cq := centre
	var cnb := nearest_on_boundary(cq)
	wpos[cid] = Vector3(cq.x, water_y, cq.y)
	wuv[cid] = cnb - cq
	wuv2[cid] = Vector2(c_depth, bank_reflect_height(cnb))
	var wnrm := PackedVector3Array()
	wnrm.resize(pos.size())
	wnrm.fill(Vector3.UP)
	var water_mesh := _sub_mesh(tri_bed, wpos, wnrm, wuv, wuv2, null)

	# Chao plano (superficie 0): transicao faixa -> circulo, circulo -> janela quadrada, resto do mapa e laterais.
	var g := _ground_flat(centre, r_circle, lvl[l_circle], tri_trans, pos, nrm)
	if g.has("error"):
		return {"ok": false, "errors": [g["error"]]}
	var ground_mesh := ArrayMesh.new()
	ground_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, g["arrays"])
	ground_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, shore)
	ground_mesh.surface_set_name(SURFACE_GRASS, "grass_env_grass_soft")
	ground_mesh.surface_set_name(SURFACE_SHORE, "shore_ws_d_shore")
	bed_mesh.surface_set_name(0, "bed_ws_d_bed")
	water_mesh.surface_set_name(0, "water_ws_d_water")
	if bool(p["assign_materials"]):
		for pair in [[ground_mesh, SURFACE_GRASS, MAT_GRASS], [ground_mesh, SURFACE_SHORE, MAT_SHORE], [bed_mesh, 0, MAT_BED], [water_mesh, 0, MAT_WATER]]:
			if ResourceLoader.exists(pair[2]):
				(pair[0] as ArrayMesh).surface_set_material(pair[1], load(pair[2]))

	var band_outer := PackedVector2Array()
	var wmin := INF
	var wmax := 0.0
	var cut_len := 0.0
	var shore_len := 0.0
	for i in (lvl[l_band_out] as PackedInt32Array):
		band_outer.append(Vector2(pos[i].x, pos[i].z))
	for i in _n:
		var w := bank_width(_poly[i])
		wmin = minf(wmin, w)
		wmax = maxf(wmax, w)
		var seg := _poly[i].distance_to(_poly[(i + 1) % _n])
		shore_len += seg
		cut_len += seg * _cut_mix(bank_kappa(_poly[i]))
	var stats := {
		"centre": [centre.x, centre.y], "boundary_vertices": _n, "area_m2": chk["area_m2"],
		"radius_min_m": r_min, "radius_max_m": Array(radii).max(), "longest_edge_m": longest,
		"levels": {"bed_frac": BED_FRAC.size(), "bed_abs": bed_x.size(), "shore": 1, "band": n_band, "circle": 1, "total": n_levels},
		"rays": n_rays, "rays_per_edge": m_sub,
		"ground_y": ground_y, "water_y": water_y, "bank_height_m": drop, "bed_min_y": bed_min_y,
		"depth_max_m": water_y - bed_min_y, "skirt_top_y": float(p["skirt_top_y"]), "skirt_gap_m": bed_min_y - float(p["skirt_top_y"]),
		"bank_width_at_waterline_min_m": wmin, "bank_width_at_waterline_max_m": wmax,
		"shoreline_m": shore_len, "cut_bank_fraction": cut_len / shore_len,
		"band_extent_ray_min_m": _band_extent_stat(band_e, n_band, true), "band_extent_ray_max_m": _band_extent_stat(band_e, n_band, false),
		"window_half_m": g["window_half"], "circle_radius_m": r_circle, "outer_ring_push_steps": outer_pushed,
		"counts": {
			"ground_grass": _counts(g["arrays"]), "ground_shore": _counts(shore),
			"bed": _counts(bed_mesh.surface_get_arrays(0)), "water": _counts(water_mesh.surface_get_arrays(0)),
		},
		"build_ms": (Time.get_ticks_usec() - t_start) / 1000.0,
	}
	return {
		"ok": true, "errors": [],
		"ground": ground_mesh, "bed": bed_mesh, "water": water_mesh,
		"boundary": _poly, "boundary_reversed": chk["reversed"], "band_outer": band_outer,
		"water_y": water_y, "ground_y": ground_y, "bed_min_y": bed_min_y,
		"surfaces": {"ground": ["grass (env_grass_soft, chao plano)", "shore (ws_d_shore, margem em relevo)"], "bed": ["ws_d_bed"], "water": ["ws_d_water"]},
		"materials": {"grass": MAT_GRASS, "shore": MAT_SHORE, "bed": MAT_BED, "water": MAT_WATER},
		"params": p.duplicate(), "stats": stats,
	}


func _band_extent_stat(band_e: Array, n_band: int, want_min: bool) -> float:
	var v := INF if want_min else 0.0
	for row in band_e:
		var e: float = (row as PackedFloat32Array)[n_band - 1]
		v = minf(v, e) if want_min else maxf(v, e)
	return v


func _counts(arrays: Array) -> Dictionary:
	return {"vertices": (arrays[Mesh.ARRAY_VERTEX] as PackedVector3Array).size(), "triangles": (arrays[Mesh.ARRAY_INDEX] as PackedInt32Array).size() / 3}


## Subconjunto da grade polar em arrays proprios (indices remapeados).
func _sub_arrays(tris: PackedInt32Array, pos: PackedVector3Array, nrm: PackedVector3Array, uv: PackedVector2Array, uv2: PackedVector2Array, _unused) -> Array:
	var remap := {}
	var v := PackedVector3Array()
	var n := PackedVector3Array()
	var t1 := PackedVector2Array()
	var t2 := PackedVector2Array()
	var idx := PackedInt32Array()
	idx.resize(tris.size())
	for k in tris.size():
		var src: int = tris[k]
		if not remap.has(src):
			remap[src] = v.size()
			v.append(pos[src])
			n.append(nrm[src])
			t1.append(uv[src])
			t2.append(uv2[src])
		idx[k] = remap[src]
	var arr := []
	arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = v
	arr[Mesh.ARRAY_NORMAL] = n
	arr[Mesh.ARRAY_TEX_UV] = t1
	arr[Mesh.ARRAY_TEX_UV2] = t2
	arr[Mesh.ARRAY_INDEX] = idx
	return arr


func _sub_mesh(tris: PackedInt32Array, pos: PackedVector3Array, nrm: PackedVector3Array, uv: PackedVector2Array, uv2: PackedVector2Array, _unused) -> ArrayMesh:
	var m := ArrayMesh.new()
	m.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, _sub_arrays(tris, pos, nrm, uv, uv2, null))
	return m


## Chao plano do jogo fora da faixa: transicao ate um circulo, costura circulo -> janela quadrada
## (so os 4 cantos na janela: sem juncao em T com as 8 celulas do resto do mapa) e laterais.
func _ground_flat(centre: Vector2, r_circle: float, circle_ids: PackedInt32Array, tri_trans: PackedInt32Array, pos: PackedVector3Array, nrm: PackedVector3Array) -> Dictionary:
	var half := float(p["map_half"])
	var s := r_circle + float(p["window_pad_m"])
	if centre.x - s <= -half + 1.0 or centre.x + s >= half - 1.0 or centre.y - s <= -half + 1.0 or centre.y + s >= half - 1.0:
		return {"error": "janela do lago (%.1f m) sai do mapa" % s}
	var v := PackedVector3Array()
	var n := PackedVector3Array()
	var idx := PackedInt32Array()
	# Transicao (vertices dos niveis l_band_out e l_circle, com as normais conjuntas).
	var remap := {}
	for k in tri_trans.size():
		var src: int = tri_trans[k]
		if not remap.has(src):
			remap[src] = v.size()
			v.append(pos[src])
			n.append(nrm[src])
		idx.append(remap[src])
	# Grade 4 x 4 do mapa (a celula central e a janela).
	var xs := [-half, centre.x - s, centre.x + s, half]
	var zs := [-half, centre.y - s, centre.y + s, half]
	var grid := {}
	for zi in 4:
		for xi in 4:
			grid[Vector2i(xi, zi)] = v.size()
			v.append(Vector3(xs[xi], ground_y, zs[zi]))
			n.append(Vector3.UP)
	for zi in 3:
		for xi in 3:
			if xi == 1 and zi == 1:
				continue
			var a: int = grid[Vector2i(xi, zi)]
			var c: int = grid[Vector2i(xi + 1, zi + 1)]
			idx.append_array(_tri_up(a, grid[Vector2i(xi + 1, zi)], c, v))
			idx.append_array(_tri_up(a, c, grid[Vector2i(xi, zi + 1)], v))
	# Costura circulo -> cantos da janela, por angulo (os dois lacos sao star-shaped e o circulo e convexo).
	var inner := PackedInt32Array()
	var a_in := PackedFloat64Array()
	var a0 := 0.0
	var n_in := circle_ids.size()
	for i in n_in:
		var vid: int = remap[circle_ids[i]]
		inner.append(vid)
		var q := Vector2(v[vid].x, v[vid].z) - centre
		var ang := atan2(q.y, q.x)
		if i == 0:
			a0 = ang
		a_in.append(a0 + fposmod(ang - a0, TAU))
	# Cantos em angulo crescente a partir do primeiro canto com angulo <= a0.
	var corners := [[PI * 0.25, grid[Vector2i(2, 2)]], [PI * 0.75, grid[Vector2i(1, 2)]], [PI * 1.25, grid[Vector2i(1, 1)]], [PI * 1.75, grid[Vector2i(2, 1)]]]
	var outer := PackedInt32Array()
	var a_out := PackedFloat64Array()
	var start := 0
	var best := INF
	for k in 4:
		var back := fposmod(a0 - float(corners[k][0]), TAU)
		if back < best:
			best = back
			start = k
	for k in 5:
		var cr: Array = corners[(start + k) % 4]
		outer.append(int(cr[1]))
		a_out.append(a0 - best + PI * 0.5 * k)
	inner.append(inner[0])
	a_in.append(a_in[0] + TAU)
	var i := 0
	var j := 0
	while i < n_in or j < 4:
		if j < 4 and (i == n_in or a_out[j + 1] < a_in[i + 1]):
			idx.append_array(_tri_up(inner[i], outer[j + 1], outer[j], v))
			j += 1
		else:
			idx.append_array(_tri_up(inner[i], inner[i + 1], outer[j], v))
			i += 1
	# Laterais do chao (iguais as da caixa do jogo): de ground_bottom_y a ground_y, uma face por trecho
	# da borda (mesmos vertices da grade do topo: sem juncao em T na quina do mapa).
	var gb := float(p["ground_bottom_y"])
	var cuts := [-half, centre.x - s, centre.x + s, half]
	var cuts_z := [-half, centre.y - s, centre.y + s, half]
	var sides := []
	for k in 3:
		sides.append([Vector3(cuts[k], 0, -half), Vector3(cuts[k + 1], 0, -half), Vector3(0, 0, -1)])
		sides.append([Vector3(half, 0, cuts_z[k]), Vector3(half, 0, cuts_z[k + 1]), Vector3(1, 0, 0)])
		sides.append([Vector3(cuts[k + 1], 0, half), Vector3(cuts[k], 0, half), Vector3(0, 0, 1)])
		sides.append([Vector3(-half, 0, cuts_z[k + 1]), Vector3(-half, 0, cuts_z[k]), Vector3(-1, 0, 0)])
	for sd in sides:
		var p0: Vector3 = sd[0]
		var p1: Vector3 = sd[1]
		var base := v.size()
		for corner in [Vector3(p0.x, ground_y, p0.z), Vector3(p1.x, ground_y, p1.z), Vector3(p1.x, gb, p1.z), Vector3(p0.x, gb, p0.z)]:
			v.append(corner)
			n.append(sd[2])
		idx.append_array(_tri_facing(base, base + 1, base + 2, v, sd[2]))
		idx.append_array(_tri_facing(base, base + 2, base + 3, v, sd[2]))
	var arr := []
	arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = v
	arr[Mesh.ARRAY_NORMAL] = n
	arr[Mesh.ARRAY_INDEX] = idx
	return {"arrays": arr, "window_half": s}


## Costura dois niveis consecutivos (lacos na mesma ordem angular). Mesmo numero de vertices:
## quadrilateros. Margem (n) x nivel subdividido (n * m): por aresta do poligono, leque do vertice
## inicial ate o raio do meio, o triangulo da aresta e o leque do vertice final.
func _stitch(tris: PackedInt32Array, pos: PackedVector3Array, a: PackedInt32Array, b: PackedInt32Array, m: int) -> void:
	if a.size() == b.size():
		var n := a.size()
		for k in n:
			var k1 := (k + 1) % n
			_quad(tris, pos, a[k], a[k1], b[k1], b[k])
		return
	var shore: PackedInt32Array = a if a.size() < b.size() else b
	var fine: PackedInt32Array = b if a.size() < b.size() else a
	var n := shore.size()
	var nf := fine.size()
	var h := m / 2
	for i in n:
		var p0 := shore[i]
		var p1 := shore[(i + 1) % n]
		var base := i * m
		for j in h:
			tris.append_array(_tri_up(p0, fine[(base + j) % nf], fine[(base + j + 1) % nf], pos))
		tris.append_array(_tri_up(p0, p1, fine[(base + h) % nf], pos))
		for j in range(h, m):
			tris.append_array(_tri_up(p1, fine[(base + j) % nf], fine[(base + j + 1) % nf], pos))


## Quadrilatero a-b-c-d: diagonal valida no plano (pode ser concavo); entre duas validas, a mais curta em 3D.
func _quad(tris: PackedInt32Array, pos: PackedVector3Array, a: int, b: int, c: int, d: int) -> void:
	var ac_ok := _splits(pos[a], pos[c], pos[b], pos[d])
	var bd_ok := _splits(pos[b], pos[d], pos[a], pos[c])
	var use_ac := ac_ok if ac_ok != bd_ok else pos[a].distance_squared_to(pos[c]) <= pos[b].distance_squared_to(pos[d])
	if use_ac:
		tris.append_array(_tri_up(a, b, c, pos) + _tri_up(a, c, d, pos))
	else:
		tris.append_array(_tri_up(a, b, d, pos) + _tri_up(b, c, d, pos))


## A diagonal p-q (no plano XZ) separa u e v (lados opostos): so entao ela fica dentro do quadrilatero.
static func _splits(pp: Vector3, qq: Vector3, u: Vector3, v: Vector3) -> bool:
	var dx := qq.x - pp.x
	var dz := qq.z - pp.z
	var su := dx * (u.z - pp.z) - dz * (u.x - pp.x)
	var sv := dx * (v.z - pp.z) - dz * (v.x - pp.x)
	return su * sv < 0.0


## Triangulo com a face frontal para +Y.
func _tri_up(a: int, b: int, c: int, v: PackedVector3Array) -> PackedInt32Array:
	return _tri_facing(a, b, c, v, Vector3.UP)


## Godot: frente em sentido horario visto de frente, i.e. (b - a) x (c - a) aponta para TRAS.
func _tri_facing(a: int, b: int, c: int, v: PackedVector3Array, facing: Vector3) -> PackedInt32Array:
	if (v[b] - v[a]).cross(v[c] - v[a]).dot(facing) > 0.0:
		return PackedInt32Array([a, c, b])
	return PackedInt32Array([a, b, c])
