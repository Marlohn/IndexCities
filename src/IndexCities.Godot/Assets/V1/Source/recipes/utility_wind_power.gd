## Receita original: aerogerador (utility.wind_power.a) — segunda onda da amostra V1,
## revisao humana PENDENTE. Somente visual: nao define capacidade, variabilidade, geracao,
## conexao a rede nem custo. Medidas sao CANDIDATAS para compatibilizacao.
##
## Nos: Body (laje de fundacao, torre tubular afunilada, porta da torre, cabine de
## transformador com porta voltada a -Z), Nacelle (Node3D no topo da torre, pivo no eixo
## de guinada Y, com a malha da nacele) e Nacelle/Rotor (Node3D com pivo no CENTRO DO CUBO,
## eixo de rotacao = Z local; cubo + 3 pas, pose padrao com uma pa para cima).
## SEM AnimationPlayer, sem luzes e sem material emissivo: girar o rotor ou a nacele e
## decisao do host a partir de estado REAL (geracao/vento); nada aqui sugere geracao garantida.
extends RefCounted

const FAMILY_ID := "utility.wind_power.a"
const MATERIAL_PREFIX := "wind_"
const MATERIALS := {
	"wind_tower": {"albedo": "#e6e4de", "roughness": 0.55},
	"wind_tip": {"albedo": "#b4473b", "roughness": 0.6},
}

const LOT := Vector2(18.0, 18.0)

# --- Medidas CANDIDATAS (metros) ----------------------------------------------------
const SLAB_R := 3.3          # laje octogonal de fundacao (raio ate o vertice)
const SLAB_H := 0.3
const TOWER_R0 := 1.0        # raio da torre na base
const TOWER_R1 := 0.62       # raio no topo
const TOWER_TOP := 21.95     # topo da torre = pivo de guinada da nacele
const TOWER_SIDES := 12
const HUB_LOCAL := Vector3(0.0, 1.05, -2.25)   # centro do cubo no referencial da nacele
const R_TIP := 8.4           # raio da ponta da pa (disco varrido de 16,8 m)
const DOOR_W := 0.9
const DOOR_Y0 := 0.55
const DOOR_H := 2.1
# Cabine do transformador (ao pe da torre, lado +X, fora da laje; porta voltada a -Z).
const KIOSK_PAD_MN := Vector3(3.45, 0.0, -2.95)
const KIOSK_PAD_MX := Vector3(6.15, 0.14, -0.65)
const KIOSK_MN := Vector3(3.65, 0.14, -2.7)
const KIOSK_MX := Vector3(5.95, 2.6, -0.9)

## Estacoes da pa: [raio, corda, espessura, torcao (graus, em relacao ao plano do rotor)].
## Torcao pequena: a pa le larga na elevacao frontal e no isometrico (legibilidade > fidelidade).
const BLADE := [
	[0.45, 0.60, 0.44, 30.0],
	[1.40, 1.70, 0.34, 20.0],
	[3.20, 1.45, 0.24, 13.0],
	[5.60, 1.05, 0.17, 8.0],
	[7.30, 0.72, 0.12, 5.0],
	[8.40, 0.34, 0.07, 3.0],
]
## A partir desta estacao (indice) a pa recebe a faixa vermelha estatica (nao emissiva).
const TIP_FROM := 4


static func _tower_r(y: float) -> float:
	return lerpf(TOWER_R0, TOWER_R1, clampf((y - SLAB_H) / (TOWER_TOP - SLAB_H), 0.0, 1.0))


static func _body(kit):
	var b = kit.builder()
	# Laje de fundacao octogonal (face plana voltada a -Z).
	b.frustum("concrete", Vector3.ZERO, SLAB_R, SLAB_R - 0.12, SLAB_H, 8, true, false, 22.5)
	# Torre tubular afunilada (faceta plana voltada a -Z para a porta).
	b.frustum("wind_tower", Vector3(0, SLAB_H, 0), TOWER_R0, TOWER_R1, TOWER_TOP - SLAB_H, TOWER_SIDES, true, false, 15.0)
	# Flanges entre segmentos (aneis discretos) e base escura.
	for y in [8.2, 15.4]:
		var r := _tower_r(y) + 0.05
		b.frustum("wind_tower", Vector3(0, y, 0), r, r, 0.16, TOWER_SIDES, true, true, 15.0)
	b.frustum("concrete_dark", Vector3(0, SLAB_H, 0), TOWER_R0 + 0.06, _tower_r(SLAB_H + 0.2) + 0.06, 0.2, TOWER_SIDES, true, false, 15.0)
	# Porta da torre (fechada) com moldura e degrau.
	var zf := -_tower_r(DOOR_Y0) * cos(deg_to_rad(15.0))
	var y1 := DOOR_Y0 + DOOR_H
	b.box("metal_dark", Vector3(-DOOR_W * 0.5, DOOR_Y0, zf - 0.06), Vector3(DOOR_W * 0.5, y1, zf + 0.2), PackedStringArray(["+z", "-y"]))
	b.box("wind_tower", Vector3(-DOOR_W * 0.5 - 0.1, y1, zf - 0.1), Vector3(DOOR_W * 0.5 + 0.1, y1 + 0.12, zf + 0.25), PackedStringArray(["+z"]))
	for sx in [-1.0, 1.0]:
		var x0: float = sx * DOOR_W * 0.5
		var x1: float = sx * (DOOR_W * 0.5 + 0.1)
		b.box("wind_tower", Vector3(minf(x0, x1), DOOR_Y0, zf - 0.1), Vector3(maxf(x0, x1), y1, zf + 0.25), PackedStringArray(["+z", "-y"]))
	b.box("concrete", Vector3(-0.75, 0.0, zf - 0.75), Vector3(0.75, DOOR_Y0, zf + 0.3), PackedStringArray(["-y", "+z"]))
	# Cabine do transformador (fora da laje, sobre base propria): corpo, cobertura com beiral,
	# porta metalica FECHADA com venezianas voltada a -Z.
	b.box("concrete_dark", KIOSK_PAD_MN, KIOSK_PAD_MX, PackedStringArray(["-y"]))
	b.bevel_box("concrete", KIOSK_MN, KIOSK_MX, 0.08, false)
	b.box("concrete_dark", Vector3(KIOSK_MN.x - 0.15, KIOSK_MX.y, KIOSK_MN.z - 0.15), Vector3(KIOSK_MX.x + 0.15, KIOSK_MX.y + 0.18, KIOSK_MX.z + 0.15))
	var kc := (KIOSK_MN.x + KIOSK_MX.x) * 0.5
	var kz := KIOSK_MN.z
	var ky := KIOSK_MN.y
	b.box("metal_dark", Vector3(kc - 0.5, ky + 0.1, kz - 0.05), Vector3(kc + 0.5, ky + 2.1, kz + 0.02), PackedStringArray(["+z"]))
	for i in 4:
		var yy := ky + 1.2 + 0.2 * float(i)
		b.box("concrete_dark", Vector3(kc - 0.38, yy, kz - 0.09), Vector3(kc + 0.38, yy + 0.07, kz - 0.05), PackedStringArray(["+z"]))
	return b


static func _nacelle(kit):
	var b = kit.builder()
	# Coroa de guinada e corpo da nacele (referencial: origem no eixo de guinada, topo da torre).
	b.cylinder("wind_tower", Vector3(0, -0.02, 0), TOWER_R1 + 0.06, 0.2, TOWER_SIDES, true, false, 15.0)
	b.bevel_box("wind_tower", Vector3(-0.85, 0.15, -1.35), Vector3(0.85, 1.95, 3.0), 0.2)
	# Carenagem do eixo ate o cubo (cilindro ao longo de -Z).
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, -1), Vector3(0, 1, 0)), Vector3(0, HUB_LOCAL.y, -1.3))
	b.frustum("wind_tower", Vector3.ZERO, 0.62, 0.55, 0.55, 10, false, false, 0.0)
	b.pop()
	return b


## Pa `k` (0 = para cima) no referencial do rotor (plano XY, eixo Z).
static func _blade(b, theta_deg: float) -> void:
	var th := deg_to_rad(theta_deg)
	var u := Vector3(-sin(th), cos(th), 0.0)
	var v := Vector3(cos(th), sin(th), 0.0)
	var rings: Array = []
	for st in BLADE:
		var r: float = st[0]
		var chord: float = st[1]
		var t: float = st[2]
		var tw := deg_to_rad(float(st[3]))
		var cdir := v * cos(tw) + Vector3(0, 0, 1) * sin(tw)
		var ndir := -v * sin(tw) + Vector3(0, 0, 1) * cos(tw)
		var o := u * r
		var prof := [
			Vector2(0.30 * chord, 0.0), Vector2(0.12 * chord, 0.5 * t), Vector2(-0.35 * chord, 0.35 * t),
			Vector2(-0.70 * chord, 0.0), Vector2(-0.35 * chord, -0.35 * t), Vector2(0.12 * chord, -0.5 * t),
		]
		var ring := []
		for q in prof:
			ring.append(o + cdir * q.x + ndir * q.y)
		rings.append(ring)
	for i in rings.size() - 1:
		var slot := "wind_tip" if i >= TIP_FROM else "wind_tower"
		var a: Array = rings[i]
		var c: Array = rings[i + 1]
		var mid_r := (float(BLADE[i][0]) + float(BLADE[i + 1][0])) * 0.5
		for k in 6:
			var k2 := (k + 1) % 6
			var cen: Vector3 = (a[k] + a[k2] + c[k] + c[k2]) * 0.25
			b.quad(slot, a[k], a[k2], c[k2], c[k], cen - u * mid_r)
	var tip: Array = rings[rings.size() - 1]
	b.face("wind_tip", tip, u)


static func _rotor(kit):
	var b = kit.builder()
	# Cubo: colar + cone do nariz apontando para -Z (referencial: origem no centro do cubo).
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, -1), Vector3(0, 1, 0)), Vector3(0, 0, 0.45))
	b.cylinder("wind_tower", Vector3.ZERO, 0.76, 0.7, 10, false, true, 0.0)
	b.frustum("wind_tower", Vector3(0, 0.7, 0), 0.76, 0.12, 1.2, 10, true, false, 0.0)
	b.pop()
	for k in 3:
		_blade(b, 120.0 * float(k))
	return b


func build(kit) -> void:
	var body_b = _body(kit)
	var nac_b = _nacelle(kit)
	var rot_b = _rotor(kit)
	print("[wind] triangulos Body: ", body_b.triangle_count(), " | Nacelle: ", nac_b.triangle_count(), " | Rotor: ", rot_b.triangle_count())
	var body_mesh = kit.save_mesh(body_b, FAMILY_ID)
	var nacelle_mesh = kit.save_mesh(nac_b, FAMILY_ID + ".nacelle")
	var rotor_mesh = kit.save_mesh(rot_b, FAMILY_ID + ".rotor")

	var root = kit.new_root("WindPower", FAMILY_ID)
	kit.add_mesh(root, "Body", body_mesh)
	var nac: Node3D = kit.add_node(root, "Nacelle", Transform3D(Basis.IDENTITY, Vector3(0, TOWER_TOP, 0)))
	kit.add_mesh(nac, "NacelleMesh", nacelle_mesh)
	var rot: Node3D = kit.add_node(nac, "Rotor", Transform3D(Basis.IDENTITY, HUB_LOCAL))
	kit.add_mesh(rot, "RotorMesh", rotor_mesh)
	kit.add_marker(root, "Candidate_ServiceEntry", Vector3(0.0, 0.0, -SLAB_R * cos(deg_to_rad(22.5)) - 0.5), Vector3.FORWARD,
		"CANDIDATO: no solo diante da laje, alinhado a porta da torre (fechada); nao e acesso funcional")
	# Volume varrido: disco do rotor em QUALQUER guinada da nacele (cilindro vertical em volta
	# do eixo da torre) => superconjunto do disco na pose padrao (x +-R, y cubo +-R, z do rotor).
	var hub_y := TOWER_TOP + HUB_LOCAL.y
	var sweep := sqrt(pow(R_TIP + 0.1, 2.0) + pow(absf(HUB_LOCAL.z) + 0.35, 2.0))
	var env := AABB(Vector3(-sweep, hub_y - R_TIP - 0.1, -sweep), Vector3(sweep * 2.0, (R_TIP + 0.1) * 2.0, sweep * 2.0))
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Energia eolica (aerogerador)",
		"lot": LOT,
		"envelope_include": env,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"laje_raio_vertice_m": SLAB_R,
			"torre_raio_base_topo_m": [TOWER_R0, TOWER_R1],
			"torre_topo_pivo_guinada_m": TOWER_TOP,
			"cubo_altura_m": snappedf(hub_y, 0.01),
			"cubo_avanco_z_m": HUB_LOCAL.z,
			"raio_pa_m": R_TIP,
			"disco_varrido_diametro_m": R_TIP * 2.0,
			"envelope_varrido_raio_xz_m": snappedf(sweep, 0.001),
			"porta_torre_l_x_a_m": [DOOR_W, DOOR_H],
			"cabine_x_z_a_m": [snappedf(KIOSK_MX.x - KIOSK_MN.x, 0.01), snappedf(KIOSK_MX.z - KIOSK_MN.z, 0.01), snappedf(KIOSK_MX.y + 0.18, 0.01)],
		},
		"parts": {
			"Body": "malha (4 superficies = slots): concrete (laje octogonal, degrau, corpo da cabine), wind_tower (torre afunilada, flanges, moldura da porta), concrete_dark (base da torre, base e cobertura da cabine, venezianas), metal_dark (portas fechadas da torre e da cabine)",
			"Nacelle": "Node3D no topo da torre (y = %.2f), pivo no eixo de guinada Y; malha Nacelle/NacelleMesh (wind_tower): coroa, corpo e carenagem do eixo" % TOWER_TOP,
			"Nacelle/Rotor": "Node3D com pivo no CENTRO DO CUBO (local da nacele %s), eixo de rotacao = Z local; malha Nacelle/Rotor/RotorMesh (wind_tower + wind_tip): cubo com nariz voltado a -Z e 3 pas (pose padrao: uma pa para cima)" % str(HUB_LOCAL),
			"Candidate_ServiceEntry": "Marker3D candidato no solo diante da laje, alinhado a porta da torre, forward -Z (a cabine tem porta propria tambem voltada a -Z)",
		},
		"notes": [
			"Pivo no centro do lote 18 x 18 m, no eixo da torre (solo). Cubo a %.2f m; disco varrido de %.1f m de diametro no plano XY, a frente da nacele (z = %.2f)." % [hub_y, R_TIP * 2.0, HUB_LOCAL.z],
			"envelope_include = cilindro varrido pelo rotor em QUALQUER guinada (raio %.3f m em XZ, y de %.2f a %.2f): o envelope cobre rotacao do rotor e guinada da nacele e cabe no lote." % [sweep, hub_y - R_TIP - 0.1, hub_y + R_TIP + 0.1],
			"SEM AnimationPlayer, luzes de balizamento ou material emissivo. Girar Nacelle (Y) e Nacelle/Rotor (Z local) e decisao do host a partir de estado REAL (vento/geracao); a pose parada nao indica nada e nenhuma animacao autonoma sugere geracao garantida.",
			"Faixa vermelha estatica nas pontas das pas (material wind_tip, nao emissivo).",
			"Portas fechadas; detalhes desenhados nao sao acessos funcionais (somente o Marker3D candidato).",
		],
	})


## Placa fina (pa) no plano XY entre z0 e z1, contorno `pts` (Vector2 x, y) convexo.
static func _icon_slab(ic, pts: Array, z0: float, z1: float, col: String) -> void:
	var front := []
	var back := []
	for p in pts:
		front.append(Vector3(p.x, p.y, z0))
		back.append(Vector3(p.x, p.y, z1))
	ic.quad(front, col)
	ic.quad(back, col)
	for i in pts.size():
		var j := (i + 1) % pts.size()
		ic.quad([front[i], front[j], back[j], back[i]], col)


## Prisma octogonal ao longo de Z (de z0 a z1); r1 = 0 vira cone.
static func _icon_disc_z(ic, c: Vector2, r0: float, r1: float, z0: float, z1: float, col: String) -> void:
	var a := []
	var b := []
	for i in 8:
		var t := TAU * float(i) / 8.0 + PI / 8.0
		a.append(Vector3(c.x + cos(t) * r0, c.y + sin(t) * r0, z0))
		b.append(Vector3(c.x + cos(t) * r1, c.y + sin(t) * r1, z1))
	ic.quad(a, col)
	if r1 > 0.001:
		ic.quad(b, col)
	for i in 8:
		var j := (i + 1) % 8
		if r1 > 0.001:
			ic.quad([a[i], a[j], b[j], b[i]], col)
		else:
			ic.quad([a[i], a[j], b[i]], col)


## Icone simplificado (make_v1_icons.gd): laje, torre clara, nacele e rotor de 3 pas com
## pontas vermelhas (pa e torre levemente mais grossas que o modelo, para ler em 64 px).
func draw_icon(ic) -> void:
	var tower: String = ic.c("wind_tower")
	var tip: String = ic.c("wind_tip")
	var conc: String = ic.c("concrete")
	var dark: String = ic.c("concrete_dark")
	var door: String = ic.c("metal_dark")
	var hub := Vector3(0.0, TOWER_TOP + HUB_LOCAL.y, HUB_LOCAL.z)
	ic.part(-100.0)
	ic.prism(Vector3.ZERO, SLAB_R, SLAB_R - 0.1, SLAB_H, 8, conc)
	# Torre com porta.
	ic.part()
	ic.prism(Vector3(0, SLAB_H, 0), 1.45, 0.9, TOWER_TOP - SLAB_H, 8, tower)
	ic.facade_rect(-0.5, 0.5, SLAB_H + 0.2, SLAB_H + 2.5, -1.45 * cos(PI / 8.0) - 0.03, door)
	# Cabine do transformador (na frente-esquerda da torre no isometrico).
	ic.part(3.0)
	ic.box(KIOSK_MN, KIOSK_MX, conc)
	ic.facade_rect(4.3, 5.3, KIOSK_MN.y + 0.1, KIOSK_MN.y + 2.1, KIOSK_MN.z - 0.02, door)
	ic.part(3.1)
	ic.box(Vector3(KIOSK_MN.x - 0.15, KIOSK_MX.y, KIOSK_MN.z - 0.15), Vector3(KIOSK_MX.x + 0.15, KIOSK_MX.y + 0.2, KIOSK_MX.z + 0.15), dark)
	# Nacele.
	ic.part()
	ic.box(Vector3(-1.05, TOWER_TOP + 0.1, -1.35), Vector3(1.05, TOWER_TOP + 2.1, 3.0), tower)
	# Pas (uma para cima), desenhadas depois da nacele; cubo por ultimo.
	var stations := [[0.6, 1.9], [2.0, 2.2], [6.6, 1.2], [8.4, 0.55]]
	for k in 3:
		var th := deg_to_rad(120.0 * float(k))
		var u := Vector2(-sin(th), cos(th))
		var v := Vector2(cos(th), sin(th))
		var c2 := Vector2(hub.x, hub.y)
		var pts_in := []
		var pts_out := []
		for st in stations:
			var r: float = st[0]
			var ch: float = st[1]
			pts_in.append(c2 + u * r + v * (0.35 * ch))
			pts_out.append(c2 + u * r - v * (0.65 * ch))
		ic.part(5.0)
		_icon_slab(ic, [pts_in[0], pts_in[1], pts_in[2], pts_out[2], pts_out[1], pts_out[0]], hub.z + 0.18, hub.z - 0.18, tower)
		ic.part(5.1)
		_icon_slab(ic, [pts_in[2], pts_in[3], pts_out[3], pts_out[2]], hub.z + 0.17, hub.z - 0.19, tip)
	ic.part(6.0)
	_icon_disc_z(ic, Vector2(hub.x, hub.y), 0.85, 0.85, -1.35, hub.z - 0.3, tower)
	ic.part(6.1)
	_icon_disc_z(ic, Vector2(hub.x, hub.y), 0.85, 0.0, hub.z - 0.3, hub.z - 1.5, tower)
