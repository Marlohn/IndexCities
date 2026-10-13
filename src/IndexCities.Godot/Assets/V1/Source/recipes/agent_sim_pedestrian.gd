## Receita original: SIM pedestre adulto estilizado (uma variante) — amostra V1, revisao PENDENTE.
##
## SOMENTE APRESENTACAO. Cada SIM exibido corresponde a um agente real da simulacao; esta
## cena apenas apresenta o estado recebido do host (posicao, direcao, andando/parado).
## Sem script, sem autoplay, sem animacao autonoma: o host toca "walk"/"idle" quando um SIM
## real anda/para. Sem acessorios que impliquem trabalho/atividade (bolsas, maletas, celulares).
## Codigo original escrito para este projeto (sem origem em acervos anteriores).
extends RefCounted

const FAMILY_ID := "agent.sim_pedestrian.a"
const MATERIAL_PREFIX := "sim_"
## Cores padrao CANDIDATAS. O host pode variar pele/cabelo/roupa por SIM real trocando o
## material por superficie (nenhuma regra de diversidade e decidida aqui).
const MATERIALS := {
	"sim_skin": {"albedo": "#c58c67", "roughness": 0.8},
	"sim_hair": {"albedo": "#4b3427", "roughness": 0.85},
	"sim_top": {"albedo": "#3f7db3", "roughness": 0.85},
	"sim_bottom": {"albedo": "#3a414b", "roughness": 0.88},
	"sim_shoes": {"albedo": "#2a2a2a", "roughness": 0.7},
}

## Medidas CANDIDATAS (metros). Nao aprovam velocidade, ocupacao de calcada ou capacidade.
const HEIGHT := 1.75
const HIPS_Y := 0.88                       # altura do no Hips (articulacao do quadril)
const HIP_X := 0.082                       # meia distancia entre as articulacoes do quadril
const SHOULDER := Vector3(0.205, 0.378, 0.0)  # pivo do ombro relativo a Hips
const NECK_Y := 0.47                       # base do pescoco (origem do no Head) relativa a Hips
## Tronco octogonal de secao eliptica: aneis [y relativo a Hips, raio X, raio Z]. A barra da
## camisa e mais funda em Z para cobrir o topo das coxas durante a caminhada.
const TORSO_RINGS := [
	[-0.07, 0.194, 0.156],   # barra da camisa
	[0.13, 0.170, 0.118],    # cintura
	[0.36, 0.185, 0.128],    # peito
	[0.43, 0.168, 0.116],    # quina do ombro (arredondada)
	[0.53, 0.085, 0.060],    # gola (tampa; o pescoco nasce aqui)
]
## Abertura dos bracos (graus): o filho "Mesh" de ArmL/ArmR e girado em Z para afastar a mao
## do quadril (silhueta legivel de frente); a manga usa a mesma inclinacao, entao o braco
## fica centrado nela. O pivo e a rotacao em X do no ArmL/ArmR continuam livres para o host.
const ARM_SPLAY := 7.0
## Manga (aneis [y, raio X, raio Z] no espaco do ombro, eixo inclinado ARM_SPLAY): mais funda
## em Z na boca, onde o braco balanca para frente/tras, para o braco nao atravessar o tecido.
const SLEEVE_RINGS := [
	[-0.125, 0.070, 0.100],
	[-0.01, 0.064, 0.074],
	[0.032, 0.043, 0.050],
	[0.052, 0.0, 0.0],
]

## Caminhada: ciclo de 1,0 s, pernas +-WALK_LEG_DEG em torno de WALK_LEG_BIAS_DEG (frente
## +23, tras -29: no apoio duplo o calcanhar da frente e o bico de tras tocam o solo juntos),
## bracos em oposicao +-WALK_ARM_DEG. Pernas/bracos seguem um cosseno amostrado em WALK_KEYS
## chaves; a altura do Hips em cada chave e calculada da geometria real da sola (ponto mais
## baixo rente ao solo), o que da a leve oscilacao vertical sem pe atravessando o chao.
const WALK_CYCLE_S := 1.0
const WALK_LEG_DEG := 26.0
const WALK_LEG_BIAS_DEG := -3.0
const WALK_ARM_DEG := 20.0
const WALK_KEYS := 16
## Sapato (z, metros, relativo a articulacao do quadril): bico em -Z, calcanhar em +Z.
const SHOE_TOE_Z := -0.18
const SHOE_HEEL_Z := 0.075
const SHOE_HALF_W := 0.064


func build(kit) -> void:
	var torso_mesh = kit.save_mesh(_torso(kit), FAMILY_ID + ".torso")
	var head_mesh = kit.save_mesh(_head(kit), FAMILY_ID + ".head")
	var arm_mesh = kit.save_mesh(_arm(kit), FAMILY_ID + ".arm")
	var leg_mesh = kit.save_mesh(_leg(kit), FAMILY_ID + ".leg")

	var root = kit.new_root("SimPedestrian", FAMILY_ID)
	var rig = kit.add_node(root, "Rig")
	var hips = kit.add_node(rig, "Hips", Transform3D(Basis.IDENTITY, Vector3(0, HIPS_Y, 0)))
	kit.add_mesh(hips, "Torso", torso_mesh)
	kit.add_mesh(hips, "Head", head_mesh, Transform3D(Basis.IDENTITY, Vector3(0, NECK_Y, 0)))
	var arm_l = kit.add_node(hips, "ArmL", Transform3D(Basis.IDENTITY, Vector3(-SHOULDER.x, SHOULDER.y, SHOULDER.z)))
	kit.add_mesh(arm_l, "Mesh", arm_mesh, Transform3D(Basis(Vector3.BACK, deg_to_rad(-ARM_SPLAY)), Vector3.ZERO))
	var arm_r = kit.add_node(hips, "ArmR", Transform3D(Basis.IDENTITY, Vector3(SHOULDER.x, SHOULDER.y, SHOULDER.z)))
	kit.add_mesh(arm_r, "Mesh", arm_mesh, Transform3D(Basis(Vector3.BACK, deg_to_rad(ARM_SPLAY)), Vector3.ZERO))
	var leg_l = kit.add_node(hips, "LegL", Transform3D(Basis.IDENTITY, Vector3(-HIP_X, 0, 0)))
	kit.add_mesh(leg_l, "Mesh", leg_mesh)
	var leg_r = kit.add_node(hips, "LegR", Transform3D(Basis.IDENTITY, Vector3(HIP_X, 0, 0)))
	kit.add_mesh(leg_r, "Mesh", leg_mesh)
	root.add_child(_animation_player())
	var stride := _stride_per_cycle()

	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "SIM pedestre (adulto estilizado)",
		"proposed": {
			"altura_total_m": HEIGHT,
			"altura_quadril_m": HIPS_Y,
			"largura_ombros_m": snappedf(torso_mesh.get_aabb().size.x, 0.01),
			"proporcao_cabecas": 5.8,   # altura total / (queixo ~1,45 m ate o topo do cabelo 1,75 m)
			"ciclo_walk_s": WALK_CYCLE_S,
			"avanco_por_ciclo_walk_m": snappedf(stride, 0.01),
		},
		"parts": {
			"Rig": "Node3D raiz do rig (identidade). O host move/gira a RAIZ SimPedestrian no mundo.",
			"Rig/Hips": "Node3D na altura do quadril; 'walk' anima so a posicao Y (oscilacao leve).",
			"Rig/Hips/Torso": "MeshInstance3D; 1 superficie sim_top (camiseta, inclui as mangas curtas fixas no tronco).",
			"Rig/Hips/Head": "MeshInstance3D com origem na base do pescoco; superficies sim_skin (rosto/pescoco), sim_hair (cabelo curto).",
			"Rig/Hips/ArmL, ArmR": "Node3D com pivo no ombro (o host/animacao gira em X local; + = mao para a frente); filho 'Mesh' (abertura fixa de %.0f graus em Z) com malha compartilhada .arm (1 superficie sim_skin)." % ARM_SPLAY,
			"Rig/Hips/LegL, LegR": "Node3D com pivo na articulacao do quadril (gira em X local; + = pe para a frente); filho 'Mesh' com malha compartilhada .leg (superficies sim_bottom, sim_shoes).",
			"AnimationPlayer": "biblioteca padrao '' com 'idle' (pose neutra, loop) e 'walk' (loop 1,0 s); autoplay vazio.",
		},
		"notes": [
			"Frente voltada a -Z; pivo no solo entre os pes. Rotacao positiva em X leva pe/mao para a frente (-Z).",
			"Sem script, sem autoplay, sem animacao autonoma: o host toca 'walk' somente quando um SIM real se desloca e 'idle' quando para.",
			"'walk' em speed_scale 1,0 avanca ~%.2f m por ciclo (estimativa geometrica do passo sem deslizar: pernas +23/-29 graus, sola em arco); o host pode usar speed_scale = velocidade_real / %.2f. A velocidade e o trajeto sao do SIM real, nao da animacao." % [stride, stride],
			"Variacao visual por SIM real (a cargo do host, a partir de dados reais): Torso e ArmL/ArmR/Mesh tem 1 superficie, entao material_override por parte basta (roupa de cima; pele dos bracos). Head e LegL/LegR/Mesh tem 2 superficies: usar set_surface_override_material(mesh.surface_find_by_name(slot), material) para trocar so sim_skin/sim_hair ou sim_bottom/sim_shoes. Nenhuma regra de diversidade e decidida aqui.",
			"Sem acessorios que impliquem trabalho/atividade (bolsas, maletas, celulares, uniformes).",
			"'walk': Hips oscila entre ~%.3f e %.2f m (calculado da sola: o ponto mais baixo dos sapatos fica rente ao solo em cada chave; erro <= 2 mm entre chaves)." % [max(_foot_depth(WALK_LEG_BIAS_DEG + WALK_LEG_DEG), _foot_depth(WALK_LEG_BIAS_DEG - WALK_LEG_DEG)), HIPS_Y],
			"Mangas fixas no tronco (limite de 10 superficies impede manga no braco); a boca da manga e mais funda em Z para o braco a +-%.0f graus nao atravessar o tecido." % WALK_ARM_DEG,
		],
	})


# ---------------------------------------------------------------------------
# Malhas (cada uma com origem no pivo da propria parte)
# ---------------------------------------------------------------------------

## Tronco octogonal achatado em Z, com barra da camisa cobrindo o topo das pernas e
## mangas curtas (ombros) em volta dos pivos dos bracos. Origem = Hips.
func _torso(kit):
	var b = kit.builder()
	_loft(b, "sim_top", TORSO_RINGS, 8, 22.5, true, true)
	# Mangas curtas (cobrem o pivo do ombro; o braco gira por dentro delas). Eixo inclinado
	# ARM_SPLAY graus (topo para o pescoco): a cupula da manga continua a linha do ombro em vez
	# de formar uma "ombreira" saliente. Boca da manga levemente aberta.
	for sx in [-1.0, 1.0]:
		var c := Vector3(sx * SHOULDER.x, SHOULDER.y, 0)
		b.push_basis(Basis(Vector3.BACK, deg_to_rad(sx * ARM_SPLAY)), c)
		_loft(b, "sim_top", SLEEVE_RINGS, 8, 22.5, true, false)
		b.pop()
	return b


## Casca de secao eliptica entre aneis [y, raio_x, raio_z] (todos com `sides` lados).
## Facetada; tampas opcionais no primeiro e no ultimo anel.
static func _loft(b, slot: String, rings: Array, sides: int, phase_deg: float, cap_bottom: bool, cap_top: bool) -> void:
	var pts := []
	for r in rings:
		var ring := []
		for i in sides:
			var ang := deg_to_rad(phase_deg) + TAU * float(i) / float(sides)
			ring.append(Vector3(cos(ang) * float(r[1]), float(r[0]), sin(ang) * float(r[2])))
		pts.append(ring)
	for k in rings.size() - 1:
		var lo: Array = pts[k]
		var hi: Array = pts[k + 1]
		for i in sides:
			var j := (i + 1) % sides
			var mid: Vector3 = ((lo[i] as Vector3) + lo[j] + hi[i] + hi[j]) * 0.25
			b.face(slot, [lo[i], lo[j], hi[j], hi[i]], Vector3(mid.x, 0, mid.z))
	if cap_bottom:
		b.face(slot, pts[0], Vector3.DOWN)
	if cap_top:
		b.face(slot, pts[pts.size() - 1], Vector3.UP)


## Cabeca levemente aumentada (leitura no zoom isometrico) com cabelo curto neutro.
## Origem = base do pescoco.
func _head(kit):
	var b = kit.builder()
	b.frustum("sim_skin", Vector3(0, -0.02, 0), 0.055, 0.05, 0.14, 6, false, false, 30.0)
	b.icosphere("sim_skin", Vector3(0, 0.235, -0.005), 0.138, 1, 0.0, 3, Vector3(0.93, 1.0, 0.97))
	# Nariz discreto: indica a frente (-Z) em zoom proximo.
	b.box("sim_skin", Vector3(-0.017, 0.19, -0.155), Vector3(0.017, 0.232, -0.115), PackedStringArray(["+z"]))
	# Cabelo curto: casca deslocada para cima/tras; o rosto (frente-baixo) fica de fora.
	b.icosphere("sim_hair", Vector3(0, 0.2565, 0.024), 0.15, 1, 0.015, 7, Vector3(0.97, 0.95, 0.98))
	return b


## Braco pendente (pele) com leve flexao do cotovelo para a frente. Origem = ombro.
func _arm(kit):
	var b = kit.builder()
	# Braco superior (o topo fica dentro da manga).
	b.frustum("sim_skin", Vector3(0, -0.28, 0), 0.046, 0.05, 0.27, 6, false, true, 30.0)
	# Antebraco + mao, flexionados ~10 graus para a frente no cotovelo.
	b.push_basis(Basis(Vector3.RIGHT, deg_to_rad(10.0)), Vector3(0, -0.27, 0))
	b.frustum("sim_skin", Vector3(0, -0.25, 0), 0.043, 0.05, 0.26, 6, false, true, 30.0)
	b.icosphere("sim_skin", Vector3(0, -0.29, 0), 0.056, 0, 0.0, 5, Vector3(0.78, 1.12, 0.95))
	b.pop()
	return b


## Perna com calca e sapato. Origem = articulacao do quadril; sola em y = -HIPS_Y.
func _leg(kit):
	var b = kit.builder()
	var foot := -HIPS_Y
	# Coxa (o topo fica dentro da barra da camisa).
	b.frustum("sim_bottom", Vector3(0, -0.42, 0), 0.075, 0.088, 0.45, 6, false, false, 30.0)
	# Canela ate a barra da calca.
	b.frustum("sim_bottom", Vector3(0, foot + 0.08, 0), 0.068, 0.078, 0.40, 6, false, true, 30.0)
	# Sapato com bico para a frente (-Z) e sola em arco centrado no quadril (leve
	# "toe spring"): parado, o apoio fica sob o tornozelo; andando, a sola rola no chao.
	b.push_basis(Basis(Vector3(0, 0, 1), Vector3(1, 0, 0), Vector3(0, 1, 0)))
	b.extrude("sim_shoes", _shoe_profile(), -SHOE_HALF_W, SHOE_HALF_W, true, true, true)
	b.pop()
	return b


## Perfil lateral do sapato no plano (z, y) da perna, origem na articulacao do quadril.
## Os pontos da sola ficam sobre o arco de raio HIPS_Y centrado no quadril.
static func _shoe_profile() -> PackedVector2Array:
	var p := PackedVector2Array()
	for z in [SHOE_HEEL_Z - 0.005, 0.035, 0.0, -0.05, -0.10, -0.15, SHOE_TOE_Z]:
		p.append(Vector2(z, -sqrt(HIPS_Y * HIPS_Y - z * z)))
	p.append(Vector2(SHOE_TOE_Z - 0.012, -HIPS_Y + 0.045))   # bico
	p.append(Vector2(SHOE_TOE_Z + 0.02, -HIPS_Y + 0.078))    # biqueira
	p.append(Vector2(-0.08, -HIPS_Y + 0.095))                # peito do pe
	p.append(Vector2(0.055, -HIPS_Y + 0.098))                # tornozelo
	p.append(Vector2(SHOE_HEEL_Z + 0.005, -HIPS_Y + 0.075))  # calcanhar (alto)
	p.append(Vector2(SHOE_HEEL_Z + 0.008, -HIPS_Y + 0.02))   # calcanhar (baixo)
	return p


## Distancia vertical do quadril ao ponto mais baixo do sapato com a perna girada `deg`
## em X (positivo = pe para a frente, -Z).
## Estimativa geometrica do avanco por ciclo de 'walk' sem o pe de apoio deslizar: no apoio,
## o quadril avanca do calcanhar (pe a frente) ate o bico (pe atras), rolando a sola em arco
## (raio HIPS_Y) entre eles. Dois apoios por ciclo. So documenta speed_scale para o host.
static func _stride_per_cycle() -> float:
	var front := deg_to_rad(WALK_LEG_BIAS_DEG + WALK_LEG_DEG)
	var back := deg_to_rad(WALK_LEG_BIAS_DEG - WALK_LEG_DEG)
	var prof := _shoe_profile()
	var heel: Vector2 = prof[0]                 # (z, y) mais atras do arco da sola
	var toe := Vector2(SHOE_TOE_Z, -sqrt(HIPS_Y * HIPS_Y - SHOE_TOE_Z * SHOE_TOE_Z))
	# z' = y sin + z cos (negativo = a frente do quadril).
	var heel_ahead := -(heel.y * sin(front) + heel.x * cos(front))
	var toe_behind := toe.y * sin(back) + toe.x * cos(back)
	var roll := HIPS_Y * (asin(heel.x / HIPS_Y) - asin(SHOE_TOE_Z / HIPS_Y))
	return 2.0 * (heel_ahead + roll + toe_behind)


static func _foot_depth(deg: float) -> float:
	var th := deg_to_rad(deg)
	var d := 0.0
	for q in _shoe_profile():
		# Ponto (z = q.x, y = q.y) girado em X: y' = y cos - z sin.
		d = max(d, -(q.y * cos(th) - q.x * sin(th)))
	return d


# ---------------------------------------------------------------------------
# Animacoes (somente rotacao/posicao dos nos do rig; tocadas pelo host)
# ---------------------------------------------------------------------------

func _animation_player() -> AnimationPlayer:
	var ap := AnimationPlayer.new()
	ap.name = "AnimationPlayer"
	ap.autoplay = ""
	var lib := AnimationLibrary.new()
	lib.add_animation("idle", _anim_idle())
	lib.add_animation("walk", _anim_walk())
	ap.add_animation_library("", lib)
	return ap


static func _rot_track(a: Animation, path: String, keys: Array) -> void:
	var t := a.add_track(Animation.TYPE_ROTATION_3D)
	a.track_set_path(t, NodePath(path))
	a.track_set_interpolation_type(t, Animation.INTERPOLATION_CUBIC)
	for k in keys:
		a.rotation_track_insert_key(t, float(k[0]), Quaternion(Vector3.RIGHT, deg_to_rad(float(k[1]))))


static func _pos_track(a: Animation, path: String, keys: Array, interp: int = Animation.INTERPOLATION_CUBIC) -> void:
	var t := a.add_track(Animation.TYPE_POSITION_3D)
	a.track_set_path(t, NodePath(path))
	a.track_set_interpolation_type(t, interp)
	for k in keys:
		a.position_track_insert_key(t, float(k[0]), Vector3(0, float(k[1]), 0))


func _anim_idle() -> Animation:
	var a := Animation.new()
	a.length = 1.0
	a.loop_mode = Animation.LOOP_LINEAR
	_pos_track(a, "Rig/Hips", [[0.0, HIPS_Y]])
	for p in ["Rig/Hips/LegL", "Rig/Hips/LegR", "Rig/Hips/ArmL", "Rig/Hips/ArmR"]:
		_rot_track(a, p, [[0.0, 0.0]])
	return a


func _anim_walk() -> Animation:
	var a := Animation.new()
	var T := WALK_CYCLE_S
	a.length = T
	a.loop_mode = Animation.LOOP_LINEAR
	# Sem chave duplicada em t = T: o loop (interpolation_loop_wrap) fecha o ciclo e a
	# interpolacao fica suave na emenda.
	var hips := []
	var leg_l := []
	var leg_r := []
	var arm_l := []
	var arm_r := []
	for k in WALK_KEYS:
		var t := T * float(k) / float(WALK_KEYS)
		var c := cos(TAU * float(k) / float(WALK_KEYS))
		var l := WALK_LEG_BIAS_DEG + WALK_LEG_DEG * c
		var r := WALK_LEG_BIAS_DEG - WALK_LEG_DEG * c
		leg_l.append([t, l])
		leg_r.append([t, r])
		# Bracos em oposicao as pernas do mesmo lado.
		arm_l.append([t, -WALK_ARM_DEG * c])
		arm_r.append([t, WALK_ARM_DEG * c])
		# Quadril na altura em que a sola mais baixa (das duas pernas) toca o solo:
		# mais baixo nos extremos do passo, na altura neutra ao cruzar as pernas.
		hips.append([t, max(_foot_depth(l), _foot_depth(r))])
	_pos_track(a, "Rig/Hips", hips, Animation.INTERPOLATION_LINEAR)
	_rot_track(a, "Rig/Hips/LegL", leg_l)
	_rot_track(a, "Rig/Hips/LegR", leg_r)
	_rot_track(a, "Rig/Hips/ArmL", arm_l)
	_rot_track(a, "Rig/Hips/ArmR", arm_r)
	return a
