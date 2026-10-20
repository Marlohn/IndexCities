## Ganchos de quebra comuns aos veiculos motorizados (SOMENTE APRESENTACAO).
##
## Cada veiculo motorizado expoe: o no `Hood` (capo/tampa com pivo na dobradica e
## malha propria), a animacao `hood_open` num `AnimationPlayer`, os marcadores
## `HazardFL/FR/RL/RR` (onde o pisca-alerta se encaixa) e `HoodSmoke` (de onde sai a
## fumaca). Nada aqui decide quando o veiculo quebra: quem abre o capo, liga o pisca
## ou a fumaca e a integracao, a partir do estado real da simulacao.
## Codigo original escrito para este projeto.
extends RefCounted

const HOOD_NODE := "Hood"
const HOOD_ANIMATION := "hood_open"
const HOOD_SECONDS := 0.9
const SMOKE_MARKER := "HoodSmoke"


## Marcador de encaixe de prop. `forward` e o eixo -Z local do marcador.
static func marker(parent: Node, node_name: String, pos: Vector3, forward: Vector3 = Vector3.FORWARD, note: String = "") -> Marker3D:
	var m := Marker3D.new()
	m.name = node_name
	var f := forward.normalized()
	var up := Vector3.UP if abs(f.dot(Vector3.UP)) < 0.99 else Vector3.BACK
	m.transform = Transform3D(Basis.looking_at(f, up), pos)
	m.gizmo_extents = 0.3
	if note != "":
		m.set_meta("note", note)
	parent.add_child(m)
	return m


## Cria HazardFL/FR (frente, -Z) e HazardRL/RR (traseira, +Z) a partir das posicoes do lado
## direito (+X); o lado esquerdo (-X) e o espelho. A posicao e o centro da lente do pisca.
static func add_hazard_markers(root: Node, front_right: Vector3, rear_right: Vector3) -> void:
	var note := "Encaixe da lente do pisca-alerta (prop hazard_lights). Nao e acesso nem regra."
	marker(root, "HazardFL", Vector3(-front_right.x, front_right.y, front_right.z), Vector3.FORWARD, note)
	marker(root, "HazardFR", front_right, Vector3.FORWARD, note)
	marker(root, "HazardRL", Vector3(-rear_right.x, rear_right.y, rear_right.z), Vector3.BACK, note)
	marker(root, "HazardRR", rear_right, Vector3.BACK, note)


## Origem da fumaca do capo aberto (eixo +Y do marcador aponta para cima).
static func add_smoke_marker(root: Node, pos: Vector3) -> void:
	var m := Marker3D.new()
	m.name = SMOKE_MARKER
	m.position = pos
	m.gizmo_extents = 0.3
	m.set_meta("note", "Origem da fumaca (prop breakdown_smoke) sobre o compartimento do motor. Nao e acesso nem regra.")
	root.add_child(m)


## Adiciona `AnimationPlayer` com `hood_open`: o no `Hood` gira em X de 0 ate `open_degrees`
## (rotacao absoluta; o capo fechado e a rotacao 0). Sem autoplay.
static func add_hood_animation(root: Node, open_degrees: float) -> AnimationPlayer:
	var a := Animation.new()
	a.length = HOOD_SECONDS
	a.loop_mode = Animation.LOOP_NONE
	var t := a.add_track(Animation.TYPE_VALUE)
	a.track_set_path(t, NodePath("%s:rotation" % HOOD_NODE))
	a.track_set_interpolation_type(t, Animation.INTERPOLATION_CUBIC)
	a.track_insert_key(t, 0.0, Vector3.ZERO)
	a.track_insert_key(t, HOOD_SECONDS * 0.7, Vector3(deg_to_rad(open_degrees * 1.06), 0, 0))
	a.track_insert_key(t, HOOD_SECONDS, Vector3(deg_to_rad(open_degrees), 0, 0))
	var lib := AnimationLibrary.new()
	lib.add_animation(HOOD_ANIMATION, a)
	var ap := AnimationPlayer.new()
	ap.name = "AnimationPlayer"
	ap.add_animation_library("", lib)
	root.add_child(ap)
	return ap
