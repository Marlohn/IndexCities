## Receita original: caminhao bau medio generico (uma variante) — amostra V1, revisao PENDENTE.
##
## SOMENTE APRESENTACAO. Cada caminhao exibido corresponde a um veiculo real da simulacao;
## a cena so apresenta o estado recebido (posicao, direcao, giro/esterco das rodas).
## Sem script, sem animacao autonoma, sem carga a mostra, sem logos/texto/marca, portas
## fechadas, farois/lanternas NAO emissivos (acender so pela integracao a partir de estado real).
## Codigo original escrito para este projeto (sem origem em acervos anteriores).
extends RefCounted

const Rig := preload("res://Assets/V1/Source/kit/breakdown_rig.gd")
const FAMILY_ID := "vehicle.truck_cargo.a"
const MATERIAL_PREFIX := "truck_"
const MATERIALS := {
	"truck_cab": {"albedo": "#e9ebed", "roughness": 0.55},
	"truck_box": {"albedo": "#c3c8ce", "roughness": 0.7},
	"truck_light_front_off": {"albedo": "#f1eedc", "roughness": 0.25},
	"truck_light_rear_off": {"albedo": "#a2433a", "roughness": 0.3},
}

## Medidas CANDIDATAS (metros). Nao aprovam capacidade de carga, faixa, raio de giro ou vaga.
const LENGTH := 7.2
const WIDTH := 2.45
const HEIGHT := 3.3
const HALF_L := LENGTH * 0.5
const WHEEL_R := 0.47
const WHEEL_W := 0.30
const WHEEL_X := 0.98           # centro das rodas em X (bitola 1,96 m)
const AXLE_F_Z := -2.55         # eixo dianteiro (frente em -Z)
const AXLE_R_Z := 1.75          # eixo traseiro (entre-eixos 4,30 m)
const CAB_HW := 1.15            # meia largura da cabine
const CAB_FRONT := -3.52        # face frontal da cabine (para-choque vai ate -3,60)
const CAB_REAR := -1.58
const CAB_TOP := 2.78
const BOX_HW := WIDTH * 0.5     # meia largura do bau
const BOX_Y0 := 1.12
const BOX_Z0 := -1.45
const BOX_Z1 := 3.535           # face traseira do bau (portas); macanetas vao ate +3,60
## Tampa frontal de servico (cabine avancada): dobradica na base da grade, abre para a frente.
const HOOD_HINGE := Vector3(0.0, 1.02, CAB_FRONT - 0.03)
const HOOD_OPEN_DEGREES := -100.0
## Encaixes (lado +X; o -X e o espelho): pisca sobre os farois e na ponta do para-choque traseiro.
const HAZARD_FRONT_R := Vector3(0.98, 1.36, CAB_FRONT - 0.05)
const HAZARD_REAR_R := Vector3(1.0, 0.66, 3.47)
const SMOKE_AT := Vector3(0.0, 1.58, CAB_FRONT - 0.12)


func build(kit) -> void:
	var wheel_mesh = kit.save_mesh(_wheel(kit), FAMILY_ID + ".wheel")
	var body_mesh = kit.save_mesh(_body(kit), FAMILY_ID + ".body")
	var hood_mesh = kit.save_mesh(_hood_mesh(kit), FAMILY_ID + ".hood")

	var root = kit.new_root("TruckCargo", FAMILY_ID)
	kit.add_mesh(root, "Body", body_mesh)
	var wheels := {
		"Wheel_FL": Vector3(-WHEEL_X, WHEEL_R, AXLE_F_Z),
		"Wheel_FR": Vector3(WHEEL_X, WHEEL_R, AXLE_F_Z),
		"Wheel_RL": Vector3(-WHEEL_X, WHEEL_R, AXLE_R_Z),
		"Wheel_RR": Vector3(WHEEL_X, WHEEL_R, AXLE_R_Z),
	}
	for n in wheels.keys():
		kit.add_mesh(root, n, wheel_mesh, Transform3D(Basis.IDENTITY, wheels[n]))
	kit.add_marker(root, "Candidate_CargoDoor", Vector3(0, 0, HALF_L + 0.30), Vector3.BACK,
		"CANDIDATO: ponto no solo atras das portas traseiras (fechadas). Nao e acesso funcional.")
	add_breakdown_nodes(root, hood_mesh)
	Rig.add_hood_animation(root, HOOD_OPEN_DEGREES)

	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Caminhao bau medio (carga local)",
		"proposed": {
			"comprimento_m": LENGTH,
			"largura_bau_m": WIDTH,
			"largura_com_retrovisores_m": 2.9,
			"altura_m": HEIGHT,
			"raio_roda_m": WHEEL_R,
			"largura_roda_m": WHEEL_W,
			"eixo_dianteiro_z_m": AXLE_F_Z,
			"eixo_traseiro_z_m": AXLE_R_Z,
			"entre_eixos_m": AXLE_R_Z - AXLE_F_Z,
			"bitola_m": WHEEL_X * 2.0,
			"bau_m": {"x": WIDTH, "y": [BOX_Y0, HEIGHT], "z": [BOX_Z0, BOX_Z1]},
		},
		"parts": {
			"Body": "cabine + bau + chassi + para-choques + portas traseiras fechadas (<= 7 superficies)",
			"Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR": "MeshInstance3D com UMA malha compartilhada (.wheel; superficies metal_light, rubber), origem no centro do eixo. Rolagem: rotacao em X local (andar para a frente, -Z, = angulo negativo = -distancia / raio). Dianteiras podem ser estercadas em Y local (compor Basis(Y, esterco) * Basis(X, rolagem)).",
			"Hood": "MeshInstance3D com a tampa frontal de servico (grade); origem na dobradica %s. `hood_open` (AnimationPlayer) gira o no em X ate %.0f graus; rotacao 0 = fechada. Superficies metal_dark e truck_cab (a cor da cabine e a do Body)." % [str(HOOD_HINGE), HOOD_OPEN_DEGREES],
			"HazardFL, HazardFR, HazardRL, HazardRR, HoodSmoke": "Marker3D de encaixe dos props de quebra (pisca-alerta e fumaca). Sem acesso nem regra.",
			"Candidate_CargoDoor": "Marker3D CANDIDATO no solo, 0,30 m atras do ponto mais traseiro (z = +3,90), forward +Z (para fora das portas). Nao e acesso funcional nem vaga.",
		},
		"notes": [
			"Frente (cabine) voltada a -Z; esquerda = -X. Pivo no solo, centrado na projecao do veiculo (comprimento -3,60..+3,60).",
			"Rodas: centro em y = %.2f; eixo dianteiro z = %.2f, traseiro z = %.2f, x = +-%.2f. Roda simetrica (serve dos dois lados sem espelhar)." % [WHEEL_R, AXLE_F_Z, AXLE_R_Z, WHEEL_X],
			"Farois (truck_light_front_off) e lanternas (truck_light_rear_off) NAO emissivos; a integracao pode trocar a superficie por material 'ligado' a partir de estado real.",
			"Cores por superficie (truck_cab, truck_box) podem ser trocadas por variante sem regenerar a malha. Sem logos, texto ou marca.",
			"Sem carga a mostra: o bau e fechado; o que o veiculo transporta e estado da simulacao.",
		],
	})


# ---------------------------------------------------------------------------
# Auxiliares locais
# ---------------------------------------------------------------------------

## Extrusao de um perfil no plano (z, y) ao longo de X (de x0 a x1).
static func _extrude_x(b, slot: String, poly_zy: PackedVector2Array, x0: float, x1: float) -> void:
	# Base: x local -> Z do mundo, y local -> X do mundo, z local -> Y do mundo (det = +1).
	b.push_basis(Basis(Vector3(0, 0, 1), Vector3(1, 0, 0), Vector3(0, 1, 0)))
	b.extrude(slot, poly_zy, x0, x1, true, true, true)
	b.pop()


## Painel levemente saliente (vidro/farol) apoiado numa face plana: `corners` no plano,
## `n` normal para fora, `off` = saliencia. Gera a face frontal e as 4 bordas.
static func _panel(b, slot: String, corners: Array, n: Vector3, off: float) -> void:
	var nn := n.normalized()
	var outer := []
	var cen := Vector3.ZERO
	for c in corners:
		outer.append((c as Vector3) + nn * off)
		cen += c
	cen /= float(corners.size())
	b.face(slot, outer, nn)
	for i in corners.size():
		var j := (i + 1) % corners.size()
		var mid: Vector3 = ((corners[i] as Vector3) + (corners[j] as Vector3)) * 0.5
		b.face(slot, [corners[i], corners[j], outer[j], outer[i]], mid - cen)


## Superficie de revolucao em torno de X. `profile`: Array de [x, r, slot]; o segmento
## k -> k+1 usa o slot do ponto k. O perfil percorre a secao de -X para +X por fora.
static func _lathe_x(b, profile: Array, sides: int, phase_deg: float) -> void:
	for k in profile.size() - 1:
		var p0: Array = profile[k]
		var p1: Array = profile[k + 1]
		var slot: String = p0[2]
		var x0: float = p0[0]; var r0: float = p0[1]
		var x1: float = p1[0]; var r1: float = p1[1]
		var nx := -(r1 - r0)
		var nr := x1 - x0
		for i in sides:
			var a0 := deg_to_rad(phase_deg) + TAU * float(i) / float(sides)
			var a1 := deg_to_rad(phase_deg) + TAU * float(i + 1) / float(sides)
			var am := (a0 + a1) * 0.5
			var d0 := Vector3(0, cos(a0), sin(a0))
			var d1 := Vector3(0, cos(a1), sin(a1))
			var hint := Vector3(nx, 0, 0) + Vector3(0, cos(am), sin(am)) * nr
			var q00 := Vector3(x0, 0, 0) + d0 * r0
			var q01 := Vector3(x0, 0, 0) + d1 * r0
			var q10 := Vector3(x1, 0, 0) + d0 * r1
			var q11 := Vector3(x1, 0, 0) + d1 * r1
			if r0 < 1e-5:
				b.face(slot, [Vector3(x0, 0, 0), q11, q10], hint)
			elif r1 < 1e-5:
				b.face(slot, [q00, q01, Vector3(x1, 0, 0)], hint)
			else:
				b.face(slot, [q00, q01, q11, q10], hint)


# ---------------------------------------------------------------------------
# Roda (compartilhada pelas 4; origem no centro do eixo)
# ---------------------------------------------------------------------------

func _wheel(kit):
	var b = kit.builder()
	var hw := WHEEL_W * 0.5
	# Poligono de 12 lados com fase 15: a face inferior fica plana e apoiada no solo
	# (apotema = WHEEL_R), sem roda flutuando.
	var R := WHEEL_R / cos(deg_to_rad(15.0))
	var rim := 0.27
	var disc_x := hw - 0.035
	var profile := [
		[-disc_x, 0.0, "metal_light"],     # disco do cubo (recuado)
		[-disc_x, rim, "metal_light"],     # parede interna do aro
		[-hw, rim, "rubber"],              # flanco
		[-hw, R - 0.055, "rubber"],        # chanfro do pneu
		[-hw + 0.045, R, "rubber"],        # banda de rodagem
		[hw - 0.045, R, "rubber"],
		[hw, R - 0.055, "rubber"],
		[hw, rim, "metal_light"],
		[disc_x, rim, "metal_light"],
		[disc_x, 0.0, "metal_light"],
	]
	_lathe_x(b, profile, 12, 15.0)
	# Tampa hexagonal do cubo (sobressai dos dois lados; da leitura do giro).
	b.cylinder_x("metal_light", Vector3.ZERO, 0.095, WHEEL_W + 0.01, 6, 0.0)
	return b


# ---------------------------------------------------------------------------
# Corpo
# ---------------------------------------------------------------------------

func _body(kit):
	var b = kit.builder()
	_cab(b)
	_cargo_box(b)
	_chassis(b)
	return b


func _cab(b) -> void:
	# Perfil lateral da cabine (z, y): frente levemente inclinada, teto chanfrado e
	# recorte do para-lama dianteiro.
	var prof := PackedVector2Array([
		Vector2(CAB_FRONT, 0.62),
		Vector2(CAB_FRONT, 1.62),
		Vector2(CAB_FRONT + 0.13, 2.62),
		Vector2(CAB_FRONT + 0.22, CAB_TOP),
		Vector2(CAB_REAR - 0.10, CAB_TOP),
		Vector2(CAB_REAR, CAB_TOP - 0.10),
		Vector2(CAB_REAR, 0.62),
		Vector2(AXLE_F_Z + 0.57, 0.62),
		Vector2(AXLE_F_Z + 0.57, 0.92),
		Vector2(AXLE_F_Z + 0.37, 1.10),
		Vector2(AXLE_F_Z - 0.37, 1.10),
		Vector2(AXLE_F_Z - 0.57, 0.92),
		Vector2(AXLE_F_Z - 0.57, 0.62),
	])
	_extrude_x(b, "truck_cab", prof, -CAB_HW, CAB_HW)

	# Para-brisa no plano inclinado da frente.
	var slope := 0.13 / 1.0
	var zf := func(y: float) -> float: return CAB_FRONT + (y - 1.62) * slope
	var n_front := Vector3(0, slope, -1.0).normalized()
	var wy0 := 1.74
	var wy1 := 2.52
	_panel(b, "glass_dark", [
		Vector3(-1.02, wy0, zf.call(wy0)), Vector3(1.02, wy0, zf.call(wy0)),
		Vector3(1.0, wy1, zf.call(wy1)), Vector3(-1.0, wy1, zf.call(wy1)),
	], n_front, 0.02)
	# Janelas laterais das portas.
	for sx in [-1.0, 1.0]:
		var x: float = sx * CAB_HW
		_panel(b, "glass_dark", [
			Vector3(x, 1.74, CAB_FRONT + 0.20), Vector3(x, 1.74, -2.42),
			Vector3(x, 2.52, -2.42), Vector3(x, 2.52, CAB_FRONT + 0.28),
		], Vector3(sx, 0, 0), 0.02)
		# Junta traseira da porta e macaneta.
		b.box("metal_dark", Vector3(x - 0.012 if sx < 0 else x - 0.005, 1.0, -2.33), Vector3(x + 0.005 if sx < 0 else x + 0.012, 2.6, -2.31))
		b.box("metal_dark", Vector3(x - 0.03 if sx < 0 else x - 0.005, 1.62, -2.52), Vector3(x + 0.005 if sx < 0 else x + 0.03, 1.66, -2.40))
		# Retrovisor: dois bracos + espelho voltado para tras.
		b.beam("metal_dark", Vector3(x, 2.36, CAB_FRONT + 0.26), Vector3(sx * 1.40, 2.36, CAB_FRONT + 0.12), 0.035)
		b.beam("metal_dark", Vector3(x, 1.96, CAB_FRONT + 0.22), Vector3(sx * 1.40, 2.00, CAB_FRONT + 0.12), 0.035)
		b.bevel_box("metal_dark", Vector3(sx * 1.40 - 0.05, 1.90, CAB_FRONT + 0.06), Vector3(sx * 1.40 + 0.05, 2.46, CAB_FRONT + 0.16), 0.02, true)
		b.box("glass_dark", Vector3(sx * 1.40 - 0.035, 1.94, CAB_FRONT + 0.16), Vector3(sx * 1.40 + 0.035, 2.42, CAB_FRONT + 0.175), PackedStringArray(["-z"]))

	# Pala sobre o para-brisa (aba na cor da cabine).
	b.box("truck_cab", Vector3(-1.08, 2.62, CAB_FRONT + 0.03), Vector3(1.08, 2.69, CAB_FRONT + 0.22))
	# Degraus de acesso sob as portas (atras do para-lama dianteiro).
	for sx in [-1.0, 1.0]:
		var xa: float = sx * (CAB_HW - 0.30)
		var xb: float = sx * (CAB_HW + 0.01)
		b.box("metal_dark", Vector3(min(xa, xb), 0.40, AXLE_F_Z + 0.62), Vector3(max(xa, xb), 0.46, CAB_REAR - 0.04))
		b.box("metal_dark", Vector3(min(xa, xb), 0.46, CAB_REAR - 0.10), Vector3(max(xa, xb), 0.62, CAB_REAR - 0.04), PackedStringArray(["+y"]))
	# Compartimento atras da grade (fundo escuro + aletas do radiador). A grade em si e a
	# tampa `Hood`, que cobre este fundo e se abre.
	b.box("metal_dark", Vector3(-0.70, 1.02, CAB_FRONT - 0.015), Vector3(0.70, 1.50, CAB_FRONT + 0.01), PackedStringArray(["+z"]))
	for x in [-0.45, -0.15, 0.15, 0.45]:
		b.box("metal_light", Vector3(x - 0.03, 1.08, CAB_FRONT - 0.03), Vector3(x + 0.03, 1.44, CAB_FRONT - 0.015), PackedStringArray(["+z"]))
	# Farois (apagados) nas quinas inferiores.
	for sx in [-1.0, 1.0]:
		var x0: float = sx * 0.78
		var x1: float = sx * 1.06
		b.box("metal_dark", Vector3(min(x0, x1) - 0.02, 0.96, CAB_FRONT - 0.02), Vector3(max(x0, x1) + 0.02, 1.26, CAB_FRONT + 0.01), PackedStringArray(["+z"]))
		b.box("truck_light_front_off", Vector3(min(x0, x1), 0.99, CAB_FRONT - 0.045), Vector3(max(x0, x1), 1.23, CAB_FRONT - 0.02), PackedStringArray(["+z"]))
	# Para-choque dianteiro.
	b.bevel_box("metal_dark", Vector3(-1.17, 0.40, -HALF_L), Vector3(1.17, 0.72, CAB_FRONT + 0.10), 0.04, true)


## Tampa da grade (aberta no `Hood`): painel escuro + 3 frisos na cor da cabine, com o
## mesmo aspecto da grade fixa original. Origem da malha = dobradica (HOOD_HINGE).
func _hood_mesh(kit):
	var b = kit.builder()
	_hood(b)
	return b


func _hood(b) -> void:
	b.push_translate(-HOOD_HINGE)
	b.box("metal_dark", Vector3(-0.70, 1.02, CAB_FRONT - 0.05), Vector3(0.70, 1.50, CAB_FRONT - 0.015), PackedStringArray(["+z"]))
	for y in [1.12, 1.24, 1.36]:
		b.box("truck_cab", Vector3(-0.66, y, CAB_FRONT - 0.075), Vector3(0.66, y + 0.05, CAB_FRONT - 0.05), PackedStringArray(["+z"]))
	b.pop()


## No `Hood` e marcadores de encaixe (a animacao `hood_open` e adicionada por quem salva a
## cena). Usado tambem pelas carrocerias do Patio de Obras (res://Assets/Construction),
## que reaproveitam esta cabine.
func add_breakdown_nodes(root: Node3D, hood_mesh: Mesh) -> void:
	var hood := MeshInstance3D.new()
	hood.name = Rig.HOOD_NODE
	hood.mesh = hood_mesh
	hood.position = HOOD_HINGE
	hood.set_meta("open_degrees", HOOD_OPEN_DEGREES)
	root.add_child(hood)
	Rig.add_hazard_markers(root, HAZARD_FRONT_R, HAZARD_REAR_R)
	Rig.add_smoke_marker(root, SMOKE_AT)


func _cargo_box(b) -> void:
	b.bevel_box("truck_box", Vector3(-BOX_HW, BOX_Y0, BOX_Z0), Vector3(BOX_HW, HEIGHT, BOX_Z1), 0.06, true)
	# Frisos verticais sutis (mesma cor do bau) e longarina inferior.
	for sx in [-1.0, 1.0]:
		for z in [-0.45, 0.55, 1.55, 2.55]:
			if sx > 0:
				b.box("truck_box", Vector3(BOX_HW - 0.01, BOX_Y0 + 0.14, z - 0.04), Vector3(BOX_HW + 0.03, HEIGHT - 0.1, z + 0.04), PackedStringArray(["-x", "+y", "-y"]))
			else:
				b.box("truck_box", Vector3(-BOX_HW - 0.03, BOX_Y0 + 0.14, z - 0.04), Vector3(-BOX_HW + 0.01, HEIGHT - 0.1, z + 0.04), PackedStringArray(["+x", "+y", "-y"]))
		var xa: float = sx * (BOX_HW + 0.03)
		var xb: float = sx * (BOX_HW - 0.02)
		b.box("metal_light", Vector3(min(xa, xb), BOX_Y0 - 0.02, BOX_Z0 + 0.04), Vector3(max(xa, xb), BOX_Y0 + 0.12, BOX_Z1 - 0.04))

	# Portas traseiras FECHADAS (parte do Body): moldura, junta central, varoes e dobradicas.
	var zr := BOX_Z1
	for sx in [-1.0, 1.0]:
		var xa: float = sx * (BOX_HW + 0.02)
		var xb: float = sx * (BOX_HW - 0.13)
		b.box("metal_light", Vector3(min(xa, xb), BOX_Y0, zr - 0.06), Vector3(max(xa, xb), HEIGHT, zr + 0.03))
	b.box("metal_light", Vector3(-BOX_HW + 0.13, HEIGHT - 0.13, zr - 0.06), Vector3(BOX_HW - 0.13, HEIGHT, zr + 0.03))
	b.box("metal_light", Vector3(-BOX_HW + 0.13, BOX_Y0, zr - 0.06), Vector3(BOX_HW - 0.13, BOX_Y0 + 0.12, zr + 0.03))
	b.box("metal_dark", Vector3(-0.012, BOX_Y0 + 0.12, zr - 0.01), Vector3(0.012, HEIGHT - 0.13, zr + 0.015), PackedStringArray(["-z"]))
	for x in [-0.86, -0.30, 0.30, 0.86]:
		b.beam("metal_light", Vector3(x, BOX_Y0 + 0.16, zr + 0.03), Vector3(x, HEIGHT - 0.17, zr + 0.03), 0.04)
		# Manopla da trava (fechada).
		b.box("metal_dark", Vector3(x - 0.05, 1.78, zr + 0.02), Vector3(x + 0.05, 1.86, zr + 0.065), PackedStringArray(["-z"]))
	for sx in [-1.0, 1.0]:
		for y in [1.45, 2.2, 2.95]:
			var xa: float = sx * (BOX_HW - 0.22)
			var xb: float = sx * (BOX_HW - 0.12)
			b.box("metal_dark", Vector3(min(xa, xb), y - 0.05, zr - 0.01), Vector3(max(xa, xb), y + 0.05, zr + 0.025), PackedStringArray(["-z"]))


func _chassis(b) -> void:
	# Longarinas e subchassi sob o bau.
	for sx in [-1.0, 1.0]:
		var xa: float = sx * 0.42
		var xb: float = sx * 0.56
		b.box("metal_dark", Vector3(min(xa, xb), 0.62, CAB_FRONT + 0.15), Vector3(max(xa, xb), 0.86, BOX_Z1 - 0.12))
	b.box("metal_dark", Vector3(-0.62, 0.86, BOX_Z0 + 0.05), Vector3(0.62, BOX_Y0, BOX_Z1 - 0.12), PackedStringArray(["+y"]))
	# Eixos.
	for z in [AXLE_F_Z, AXLE_R_Z]:
		b.cylinder_x("metal_dark", Vector3(0, WHEEL_R, z), 0.065, (WHEEL_X - WHEEL_W * 0.5) * 2.0, 6, 0.0)
	# Para-lamas traseiros (rente ao fundo do bau) com aba vertical atras da roda.
	for sx in [-1.0, 1.0]:
		var xa: float = sx * 0.78
		var xb: float = sx * (BOX_HW - 0.04)
		b.box("metal_dark", Vector3(min(xa, xb), 1.0, AXLE_R_Z - 0.62), Vector3(max(xa, xb), BOX_Y0, AXLE_R_Z + 0.62), PackedStringArray(["+y"]))
		b.box("metal_dark", Vector3(min(xa, xb) + 0.02, 0.36, AXLE_R_Z + 0.58), Vector3(max(xa, xb) - 0.02, 1.0, AXLE_R_Z + 0.62))
	# Protecao lateral entre os eixos (barras + suportes).
	for sx in [-1.0, 1.0]:
		var xa: float = sx * (BOX_HW - 0.12)
		var xb: float = sx * (BOX_HW - 0.06)
		for y in [0.50, 0.80]:
			b.box("metal_light", Vector3(min(xa, xb), y, -1.30), Vector3(max(xa, xb), y + 0.08, AXLE_R_Z - 0.68))
		for z in [-1.0, 0.85]:
			b.beam("metal_dark", Vector3(sx * 0.56, 0.84, z), Vector3(sx * (BOX_HW - 0.12), 0.84, z), 0.05)
			b.beam("metal_dark", Vector3(sx * (BOX_HW - 0.09), 0.50, z), Vector3(sx * (BOX_HW - 0.09), 0.88, z), 0.04)
	# Travessa traseira com lanternas (apagadas) e para-choque traseiro.
	b.box("metal_dark", Vector3(-1.12, 0.84, BOX_Z1 - 0.30), Vector3(1.12, BOX_Y0, BOX_Z1 - 0.08), PackedStringArray(["+y"]))
	for sx in [-1.0, 1.0]:
		var xa: float = sx * 0.76
		var xb: float = sx * 1.06
		b.box("truck_light_rear_off", Vector3(min(xa, xb), 0.90, BOX_Z1 - 0.09), Vector3(max(xa, xb), 1.06, BOX_Z1 - 0.05), PackedStringArray(["-z"]))
		b.beam("metal_dark", Vector3(sx * 0.55, 0.54, BOX_Z1 - 0.12), Vector3(sx * 0.55, 0.86, BOX_Z1 - 0.16), 0.07)
	b.bevel_box("metal_dark", Vector3(-1.10, 0.40, BOX_Z1 - 0.16), Vector3(1.10, 0.56, BOX_Z1 - 0.02), 0.03, true)
