## Receita original: carro de passeio compacto generico (hatch/sedan curto) — amostra V1, revisao PENDENTE.
##
## SOMENTE APRESENTACAO. Cada carro exibido corresponde a um veiculo real da simulacao;
## a cena so apresenta o estado recebido (posicao, direcao, giro/esterco das rodas).
## Sem script, sem animacao autonoma, sem logos/texto/marca, sem interior, portas
## fechadas, farois/lanternas NAO emissivos (acender so pela integracao a partir de estado real).
## A pintura e a superficie propria `car_paint`; o host varia a cor com
## `set_surface_override_material` usando `car_paint_*` (ou outro material).
## Codigo original escrito para este projeto (sem origem em acervos anteriores).
extends RefCounted

const FAMILY_ID := "vehicle.car_passenger.a"
const MATERIAL_PREFIX := "car_"
const MATERIALS := {
	"car_paint": {"albedo": "#7f8c99", "roughness": 0.4},
	"car_paint_red": {"albedo": "#b8342f", "roughness": 0.4},
	"car_paint_blue": {"albedo": "#2f5fa8", "roughness": 0.4},
	"car_paint_white": {"albedo": "#ecede9", "roughness": 0.4},
	"car_paint_yellow": {"albedo": "#e0b52c", "roughness": 0.4},
	"car_paint_green": {"albedo": "#3f8a4f", "roughness": 0.4},
	"car_headlight_off": {"albedo": "#f3e6b0", "roughness": 0.25},
	"car_taillight": {"albedo": "#a2433a", "roughness": 0.3},
}

## Medidas CANDIDATAS (metros). Nao aprovam capacidade, faixa, raio de giro ou vaga.
const LENGTH := 4.2
const WIDTH := 1.8
const HEIGHT := 1.48
const HALF_L := LENGTH * 0.5
const HALF_W := WIDTH * 0.5
const WHEEL_R := 0.30
const WHEEL_W := 0.20
const WHEEL_X := 0.79           # centro das rodas em X (bitola 1,58 m)
const AXLE_F_Z := -1.30         # eixo dianteiro (frente em -Z)
const AXLE_R_Z := 1.30          # eixo traseiro (entre-eixos 2,60 m)
const NOSE := 2.06              # pontas da carroceria (para-choques vao ate 2,10)
const FLOOR_Y := 0.20
const BELT_Y := 0.94            # linha de cintura (base dos vidros)
const CABIN_HW := 0.80          # meia largura da cabine


func build(kit) -> void:
	var wheel_mesh = kit.save_mesh(_wheel(kit), FAMILY_ID + ".wheel")
	var body_mesh = kit.save_mesh(_body(kit), FAMILY_ID + ".body")

	var root = kit.new_root("CarPassenger", FAMILY_ID)
	kit.add_mesh(root, "Body", body_mesh)
	var wheels := {
		"Wheel_FL": Vector3(-WHEEL_X, WHEEL_R, AXLE_F_Z),
		"Wheel_FR": Vector3(WHEEL_X, WHEEL_R, AXLE_F_Z),
		"Wheel_RL": Vector3(-WHEEL_X, WHEEL_R, AXLE_R_Z),
		"Wheel_RR": Vector3(WHEEL_X, WHEEL_R, AXLE_R_Z),
	}
	for n in wheels.keys():
		kit.add_mesh(root, n, wheel_mesh, Transform3D(Basis.IDENTITY, wheels[n]))

	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Carro de passeio compacto",
		"proposed": {
			"comprimento_m": LENGTH,
			"largura_m": WIDTH,
			"largura_com_retrovisores_m": 1.86,
			"altura_m": HEIGHT,
			"raio_roda_m": WHEEL_R,
			"largura_roda_m": WHEEL_W,
			"eixo_dianteiro_z_m": AXLE_F_Z,
			"eixo_traseiro_z_m": AXLE_R_Z,
			"entre_eixos_m": AXLE_R_Z - AXLE_F_Z,
			"bitola_m": WHEEL_X * 2.0,
		},
		"parts": {
			"Body": "carroceria + cabine + vidros + para-choques + farois/lanternas (<= 5 superficies: car_paint, glass_dark, metal_dark, car_headlight_off, car_taillight)",
			"Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR": "MeshInstance3D com UMA malha compartilhada (.wheel; superficies metal_light, rubber), origem no centro do eixo. Rolagem: rotacao em X local (andar para a frente, -Z, = angulo negativo = -distancia / raio). Dianteiras podem ser estercadas em Y local (compor Basis(Y, esterco) * Basis(X, rolagem)).",
		},
		"notes": [
			"Frente voltada a -Z; esquerda = -X. Pivo no solo, centrado na projecao do veiculo (comprimento -2,10..+2,10).",
			"Rodas: centro em y = %.2f; eixo dianteiro z = %.2f, traseiro z = %.2f, x = +-%.2f. Roda simetrica (serve dos dois lados sem espelhar)." % [WHEEL_R, AXLE_F_Z, AXLE_R_Z, WHEEL_X],
			"Pintura: superficie 'car_paint' do Body. Variante de cor = set_surface_override_material na superficie car_paint com car_paint_red/blue/white/yellow/green (a malha nao muda). A cor e do veiculo real, nao decidida aqui.",
			"Farois (car_headlight_off) e lanternas (car_taillight) NAO emissivos; a integracao pode trocar a superficie por material 'ligado' a partir de estado real.",
			"Sem interior, sem placa, sem logos ou texto.",
		],
	})


# ---------------------------------------------------------------------------
# Auxiliares locais
# ---------------------------------------------------------------------------

## Extrusao de um perfil no plano (z, y) ao longo de X (de x0 a x1).
static func _extrude_x(b, slot: String, poly_zy: PackedVector2Array, x0: float, x1: float) -> void:
	b.push_basis(Basis(Vector3(0, 0, 1), Vector3(1, 0, 0), Vector3(0, 1, 0)))
	b.extrude(slot, poly_zy, x0, x1, true, true, true)
	b.pop()


## Painel levemente saliente (vidro) apoiado numa face plana: `corners` no plano,
## `n` normal para fora, `off` = saliencia. Gera a face frontal e as bordas.
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
## k -> k+1 usa o slot do ponto k.
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
	# Poligono de 10 lados com fase 18: a face inferior fica plana e apoiada no solo
	# (apotema = WHEEL_R), sem roda flutuando.
	var R := WHEEL_R / cos(deg_to_rad(18.0))
	var rim := 0.17
	var disc_x := hw - 0.025
	var profile := [
		[-disc_x, 0.0, "metal_light"],
		[-disc_x, rim, "metal_light"],
		[-hw, rim, "rubber"],
		[-hw, R - 0.04, "rubber"],
		[-hw + 0.03, R, "rubber"],
		[hw - 0.03, R, "rubber"],
		[hw, R - 0.04, "rubber"],
		[hw, rim, "metal_light"],
		[disc_x, rim, "metal_light"],
		[disc_x, 0.0, "metal_light"],
	]
	_lathe_x(b, profile, 10, 18.0)
	# Tampa hexagonal do cubo (da leitura do giro).
	b.cylinder_x("metal_light", Vector3.ZERO, 0.06, WHEEL_W + 0.01, 6, 0.0)
	return b


# ---------------------------------------------------------------------------
# Corpo
# ---------------------------------------------------------------------------

func _body(kit):
	var b = kit.builder()
	_lower_body(b)
	_cabin(b)
	_details(b)
	return b


func _arch(zc: float) -> Array:
	# Recorte do para-lama (z, y), da esquerda para a direita (z crescente).
	return [
		Vector2(zc - 0.42, FLOOR_Y), Vector2(zc - 0.42, 0.55), Vector2(zc - 0.30, 0.70),
		Vector2(zc + 0.30, 0.70), Vector2(zc + 0.42, 0.55), Vector2(zc + 0.42, FLOOR_Y),
	]


func _lower_body(b) -> void:
	# Perfil lateral (z, y) em sentido horario: nariz baixo, capo suave, cintura reta e
	# traseira curta; recortes dos para-lamas. Frente em -Z.
	var pts: Array = [
		Vector2(-NOSE, FLOOR_Y), Vector2(-NOSE, 0.62), Vector2(-NOSE + 0.10, 0.82),
		Vector2(-0.85, BELT_Y), Vector2(NOSE - 0.12, BELT_Y), Vector2(NOSE, 0.82),
		Vector2(NOSE, FLOOR_Y),
	]
	var rear: Array = _arch(AXLE_R_Z)
	rear.reverse()   # percorre de z maior para z menor
	var front: Array = _arch(AXLE_F_Z)
	front.reverse()
	pts.append_array(rear)
	pts.append_array(front)
	_extrude_x(b, "car_paint", PackedVector2Array(pts), -HALF_W, HALF_W)


func _cabin(b) -> void:
	# Cabine (perfil z, y): para-brisa inclinado, teto curto e traseira de hatch.
	var roof := HEIGHT
	var prof := PackedVector2Array([
		Vector2(-0.85, BELT_Y), Vector2(-0.30, roof), Vector2(1.00, roof),
		Vector2(1.90, 1.02), Vector2(1.90, BELT_Y),
	])
	_extrude_x(b, "car_paint", prof, -CABIN_HW, CABIN_HW)

	# Para-brisa: plano inclinado (dz/dy = 0,55/0,54).
	var kf := 0.55 / 0.54
	var zf := func(y: float) -> float: return -0.85 + (y - BELT_Y) * kf
	var n_front := Vector3(0, 0.55, -0.54)
	_panel(b, "glass_dark", [
		Vector3(-0.68, 1.02, zf.call(1.02)), Vector3(0.68, 1.02, zf.call(1.02)),
		Vector3(0.68, 1.40, zf.call(1.40)), Vector3(-0.68, 1.40, zf.call(1.40)),
	], n_front, 0.02)

	# Vidro traseiro do hatch: plano inclinado de (z 1,00; y 1,48) a (z 1,90; y 1,02).
	var kr := 0.90 / 0.46
	var zr := func(y: float) -> float: return 1.00 + (roof - y) * kr
	var n_rear := Vector3(0, 0.90, 0.46)
	_panel(b, "glass_dark", [
		Vector3(-0.64, 1.40, zr.call(1.40)), Vector3(0.64, 1.40, zr.call(1.40)),
		Vector3(0.64, 1.10, zr.call(1.10)), Vector3(-0.64, 1.10, zr.call(1.10)),
	], n_rear, 0.02)

	# Janelas laterais (duas por lado, pilar B entre elas).
	for sx in [-1.0, 1.0]:
		var x: float = sx * CABIN_HW
		_panel(b, "glass_dark", [
			Vector3(x, 1.03, -0.62), Vector3(x, 1.03, 0.30),
			Vector3(x, 1.40, 0.30), Vector3(x, 1.40, -0.30),
		], Vector3(sx, 0, 0), 0.02)
		_panel(b, "glass_dark", [
			Vector3(x, 1.03, 0.44), Vector3(x, 1.03, 1.78),
			Vector3(x, 1.40, 1.10), Vector3(x, 1.40, 0.44),
		], Vector3(sx, 0, 0), 0.02)


func _details(b) -> void:
	# Para-choques (escuros) e saia lateral.
	b.bevel_box("metal_dark", Vector3(-0.90, 0.20, -HALF_L), Vector3(0.90, 0.46, -NOSE + 0.12), 0.03, true)
	b.bevel_box("metal_dark", Vector3(-0.90, 0.20, NOSE - 0.12), Vector3(0.90, 0.46, HALF_L), 0.03, true)
	for sx in [-1.0, 1.0]:
		var xa: float = sx * (HALF_W - 0.02)
		var xb: float = sx * (HALF_W + 0.012)
		b.box("metal_dark", Vector3(min(xa, xb), 0.20, -0.88), Vector3(max(xa, xb), 0.30, 0.88))
		# Juntas de porta e macanetas.
		for z in [-0.60, 0.38]:
			var ja: float = sx * (HALF_W - 0.005)
			var jb: float = sx * (HALF_W + 0.008)
			b.box("metal_dark", Vector3(min(ja, jb), 0.32, z - 0.012), Vector3(max(ja, jb), 0.94, z + 0.012))
		var ha: float = sx * (HALF_W - 0.005)
		var hb: float = sx * (HALF_W + 0.03)
		b.box("metal_dark", Vector3(min(ha, hb), 0.82, -0.20), Vector3(max(ha, hb), 0.86, -0.02))
		# Retrovisor.
		var ma: float = sx * (CABIN_HW - 0.01)
		var mb: float = sx * (CABIN_HW + 0.13)
		b.bevel_box("metal_dark", Vector3(min(ma, mb), 1.00, -0.62), Vector3(max(ma, mb), 1.12, -0.44), 0.02, true)

	# Frente: grade, farois (apagados). Os farois ficam no nariz inclinado.
	b.box("metal_dark", Vector3(-0.36, 0.50, -NOSE - 0.02), Vector3(0.36, 0.62, -NOSE + 0.01), PackedStringArray(["+z"]))
	for sx in [-1.0, 1.0]:
		var xa: float = sx * 0.52
		var xb: float = sx * 0.86
		b.box("car_headlight_off", Vector3(min(xa, xb), 0.60, -NOSE - 0.025), Vector3(max(xa, xb), 0.74, -NOSE + 0.01), PackedStringArray(["+z"]))
		# Lanternas traseiras (apagadas).
		b.box("car_taillight", Vector3(min(xa, xb), 0.62, NOSE - 0.01), Vector3(max(xa, xb), 0.78, NOSE + 0.025), PackedStringArray(["-z"]))
	# Faixa escura entre as lanternas (porta-malas).
	b.box("metal_dark", Vector3(-0.46, 0.64, NOSE - 0.01), Vector3(0.46, 0.72, NOSE + 0.015), PackedStringArray(["-z"]))
