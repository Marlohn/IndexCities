## Receita original: arvore de copa larga (uma variante) — amostra V1, revisao PENDENTE.
## Somente visual. Copa facetada: aglomerado de quatro icosferas (tres massas baixas a
## ~120 graus + uma de topo) com deslocamento deterministico. A planta fica quase redonda,
## entao uma rotacao Y por instancia (se a integracao quiser) nao muda muito o envelope.
extends RefCounted

const FAMILY_ID := "vegetation.tree_broadleaf.a"
## Prefixo conforme o pedido. E generico: uma futura familia de arvore deve usar prefixo proprio
## (decisao do coordenador) para nao regravar estes materiais.
const MATERIAL_PREFIX := "tree_"
## Folhagem levemente escura/quente para separar do chao gramado claro do PREVIEW (render do
## chao ~#b0c491-#bed29e, lum ~0,74-0,79; nao e a cor de grama do jogo, ainda indefinida).
## Alvo: topo iluminado com lum <= ~0,65. Tom escuro um pouco mais frio para dar volume.
const MATERIALS := {
	"tree_bark": {"albedo": "#7a5a3f", "roughness": 0.95},
	"tree_foliage": {"albedo": "#56783f", "roughness": 0.9},
	"tree_foliage_dark": {"albedo": "#3f6b3a", "roughness": 0.9},
}

## Medidas CANDIDATAS (metros) — nao aprovam ocupacao, custo ou efeito ambiental.
## Tronco: pe alargado baixo (raiz) + fuste conico que termina ENTERRADO na copa (sem tampa).
const TRUNK_SIDES := 7
const FLARE_HEIGHT := 0.2
const R_FOOT := 0.35
const R_COLLAR := 0.27
const R_TOP := 0.16
const TRUNK_TOP_Y := 3.9

## Galhos (de, ate, espessura): comecam dentro do tronco e terminam dentro de uma massa
## da copa, por isso nao tem tampas nas pontas (seriam faces sempre escondidas).
const BRANCHES := [
	[Vector3(0, 2.1, 0), Vector3(0.72, 3.55, -0.42), 0.14],
	[Vector3(0, 2.2, 0), Vector3(-0.7, 3.6, -0.4), 0.13],
	[Vector3(0, 2.35, 0), Vector3(0.0, 3.65, 0.74), 0.12],
]

## Massas da copa: material/slot, centro, raio, jitter, semente, escala.
## Tres massas baixas em azimutes ~-30, 90 e 210 graus (d ~0,9-0,95 m do eixo) + uma de topo
## larga, levemente puxada para -Z (fecha o vao frontal da planta). Dois tons: claro no topo
## e numa massa baixa, escuro nas outras duas (le volume e aparece na silhueta).
const CANOPY := [
	{"slot": "tree_foliage", "c": Vector3(0.823, 4.12, -0.475), "r": 1.38, "j": 0.1, "seed": 11, "s": Vector3(1.0, 0.85, 1.0)},
	{"slot": "tree_foliage_dark", "c": Vector3(0.0, 4.22, 0.9), "r": 1.38, "j": 0.12, "seed": 23, "s": Vector3(1.0, 0.85, 1.0)},
	{"slot": "tree_foliage_dark", "c": Vector3(-0.823, 4.15, -0.475), "r": 1.33, "j": 0.12, "seed": 37, "s": Vector3(1.0, 0.85, 1.0)},
	{"slot": "tree_foliage", "c": Vector3(0.0, 4.96, -0.3), "r": 1.62, "j": 0.1, "seed": 41, "s": Vector3(1.0, 0.86, 1.0)},
]


## Folga (espaco normalizado da massa) ao decidir que um triangulo esta enterrado.
const BURY_MARGIN := 0.02


## Triangulos (anti-horarios vistos de fora) de uma massa da copa, gerados pela icosfera do
## kit num construtor temporario: mesmos vertices e jitter que b.icosphere produziria.
static func _lobe_tris(kit, l: Dictionary) -> Array:
	var t = kit.builder()
	t.icosphere("lobe", l["c"], l["r"], 1, l["j"], l["seed"], l["s"])
	var arr: Array = t.commit(func(_slot): return null).surface_get_arrays(0)
	var v: PackedVector3Array = arr[Mesh.ARRAY_VERTEX]
	var idx: PackedInt32Array = arr[Mesh.ARRAY_INDEX]
	var out: Array = []
	for i in range(0, idx.size(), 3):
		# A malha guarda a ordem horaria da Godot (a, c, b); volta para anti-horaria.
		out.append([v[idx[i]], v[idx[i + 2]], v[idx[i + 1]]])
	return out


## Raio da maior esfera contida no poliedro facetado da massa, no espaco normalizado
## q = (p - c) / (r * s): menor distancia do centro ao plano de cada face (a massa e
## estrelada em relacao ao centro). Face voltada para dentro => 0 (nao enterra nada).
static func _inner_radius(l: Dictionary, tris: Array) -> float:
	var k: Vector3 = l["s"] * float(l["r"])
	var c: Vector3 = l["c"]
	var rho := INF
	for t in tris:
		var a: Vector3 = (t[0] - c) / k
		var bb: Vector3 = (t[1] - c) / k
		var cc: Vector3 = (t[2] - c) / k
		var n := (bb - a).cross(cc - a).normalized()
		rho = minf(rho, n.dot(a))
	return maxf(rho, 0.0)


## Verdadeiro se os 3 vertices (logo o triangulo inteiro, por convexidade da esfera) estao
## dentro da esfera inscrita de OUTRA massa: a face fica sempre escondida e nao e emitida.
## Faces que tocam a superficie externa do conjunto nunca passam neste teste.
static func _buried(t: Array, lobes: Array, own: int) -> bool:
	for j in lobes.size():
		if j == own:
			continue
		var l: Dictionary = lobes[j]["l"]
		var k: Vector3 = l["s"] * float(l["r"])
		var lim: float = float(lobes[j]["rho"]) - BURY_MARGIN
		var inside := true
		for p in t:
			if ((p - l["c"]) / k).length() >= lim:
				inside = false
				break
		if inside:
			return true
	return false


## Haste de secao quadrada SEM tampas nas pontas (as duas ficam dentro de outros volumes).
static func _branch(b, slot: String, from: Vector3, to: Vector3, thickness: float) -> void:
	var d := to - from
	var length := d.length()
	if length < 1e-5:
		return
	var y := d / length
	var ref := Vector3.UP if abs(y.dot(Vector3.UP)) < 0.95 else Vector3.RIGHT
	var x := ref.cross(y).normalized()
	var z := x.cross(y).normalized()
	var h := thickness * 0.5
	b.push(Transform3D(Basis(x, y, z), from))
	b.box(slot, Vector3(-h, 0, -h), Vector3(h, length, h), PackedStringArray(["-y", "+y"]))
	b.pop()


func build(kit) -> void:
	var b = kit.builder()
	# Tronco: pe baixo e suave (le como raiz, nao como vaso) + fuste ate dentro da copa.
	# Sem tampas: a base apoia no solo, a juncao e interna e o topo fica enterrado na copa.
	b.frustum("tree_bark", Vector3.ZERO, R_FOOT, R_COLLAR, FLARE_HEIGHT, TRUNK_SIDES, false, false)
	b.frustum("tree_bark", Vector3(0, FLARE_HEIGHT, 0), R_COLLAR, R_TOP, TRUNK_TOP_Y - FLARE_HEIGHT, TRUNK_SIDES, false, false)
	for br in BRANCHES:
		_branch(b, "tree_bark", br[0], br[1], br[2])
	# Copa: massas sobrepostas em dois tons (leitura de volume no isometrico). As faces de
	# uma massa totalmente enterradas em outra sao omitidas (economia para instancias em massa);
	# a superficie externa do conjunto e a silhueta ficam identicas.
	var lobes: Array = []
	for l in CANOPY:
		var tris := _lobe_tris(kit, l)
		lobes.append({"l": l, "tris": tris, "rho": _inner_radius(l, tris)})
	for i in lobes.size():
		var slot: String = lobes[i]["l"]["slot"]
		for t in lobes[i]["tris"]:
			if not _buried(t, lobes, i):
				b.tri(slot, t[0], t[1], t[2])
	var mesh = kit.save_mesh(b, FAMILY_ID)

	var root = kit.new_root("TreeBroadleaf", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Arvore de copa larga",
		"proposed": {
			"altura_total_m": 6.5,
			"diametro_copa_m": 4.3,
			"copa_largura_planta_m": {"min": 4.0, "max": 4.4, "media": 4.25},
			"copa_envelope_xz_m": [4.4, 4.38],
			"altura_livre_sob_copa_m": 2.9,
			"tronco_diametro_m": {"colo": 0.52, "sob_copa": 0.36},
		},
		"parts": {"Body": "malha unica (tronco + galhos + copa), 3 superficies: tree_bark, tree_foliage, tree_foliage_dark"},
		"notes": [
			"Pivo no centro da base do tronco. Sem colisao, ocupacao ou efeito ambiental: tudo isso e da integracao/simulacao.",
			"Copa em planta quase redonda (largura 4,0-4,4 m conforme a direcao); envelope da copa levemente deslocado para +Z (~0,14 m).",
			"O tronco continua ate y=3,9 dentro da copa (sem tampa); a altura livre visivel sob a copa e ~2,9 m.",
			"Faces de uma massa da copa totalmente enterradas em outra sao omitidas na geracao: a superficie externa e a silhueta nao mudam; a malha so fica aberta dentro do volume da copa.",
			"Altura livre ~2,9 m com copa de raio ~2,2 m: recuo em relacao a pista/fachadas, altura livre maior ou variante menor sao compatibilizacao pendente com a integracao.",
			"Candidata a MultiMesh por regiao na integracao (nao medido nesta amostra).",
		],
	})
