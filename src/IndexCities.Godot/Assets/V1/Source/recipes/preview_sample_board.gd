## Composicao de REVISAO da amostra V1 (Preview/v1_sample_board.tscn).
##
## Somente para olhar silhuetas, paleta e proporcoes lado a lado. NAO e uma partida,
## NAO povoa o jogo, NAO define lotes, recuos, acessos ou trafego: as posicoes abaixo
## sao arbitrarias de vitrine. O SIM e o caminhao aparecem parados como pecas de escala.
## Iluminacao, ceu e chao existem apenas nesta cena de preview.
extends RefCounted

const FAMILY_ID := "preview.sample_board"

const ROAD_STRAIGHT := "res://Assets/V1/Scenes/road.urban_1x1.a.straight.tscn"
const ROAD_CORNER := "res://Assets/V1/Scenes/road.urban_1x1.a.corner_l.tscn"
const ROAD_LAMP := "res://Assets/V1/Scenes/road.urban_1x1.a.lamp.tscn"
const CITY_HALL := "res://Assets/V1/Scenes/civic.city_hall.a.tscn"
const HOUSE := "res://Assets/V1/Scenes/residential.small_house.a.tscn"
const GROCERY := "res://Assets/V1/Scenes/commercial.grocery_small.a.tscn"
const TREE := "res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn"
const SIM := "res://Assets/V1/Scenes/agent.sim_pedestrian.a.tscn"
const TRUCK := "res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn"

const OUT_PATH := "res://Assets/V1/Preview/v1_sample_board.tscn"

## Modulo candidato da via 1+1 (largura total 10,5 m). Lido do proprio conector quando existir.
var module := 10.5
var _kit


func build(kit) -> void:
	_kit = kit
	for p in [ROAD_STRAIGHT, ROAD_CORNER, ROAD_LAMP, CITY_HALL, HOUSE, GROCERY, TREE, SIM, TRUCK]:
		if not ResourceLoader.exists(p):
			kit.err("Prancha: cena ausente %s (gere as familias antes)" % p)
			return
	var root := Node3D.new()
	root.name = "V1SampleBoard"
	root.set_meta("purpose", "preview de revisao visual da amostra V1; nao e partida, nao povoa o jogo, posicoes arbitrarias")
	root.set_meta("asset_status", kit.ASSET_STATUS)

	_add_environment(root)
	_add_ground(root)

	# Via ao longo de X (z = 0), 9 modulos retos + esquina em L virando para -Z + 2 modulos.
	var straight_xs: Array = []
	for k in 9:
		straight_xs.append(-4.0 * module + module * k)
	var road: Node3D = kit.add_node(root, "Road")
	var lamps: Node3D = kit.add_node(root, "Lamps")
	for k in straight_xs.size():
		var x: float = straight_xs[k]
		var piece: Node3D = kit.instance(road, ROAD_STRAIGHT, "Straight%02d" % k, Transform3D(Basis.IDENTITY, Vector3(x, 0, 0)))
		if k % 2 == 0:
			_lamp_at_slot(lamps, piece, "Lamp%02d" % k)
	var corner_x: float = straight_xs[straight_xs.size() - 1] + module
	var corner: Node3D = kit.instance(road, ROAD_CORNER, "CornerL", Transform3D(Basis.IDENTITY, Vector3(corner_x, 0, 0)))
	_lamp_at_slot(lamps, corner, "LampCorner")
	# Pecas retas giradas 90 graus (eixo da peca passa a Z) saindo do conector -Z da esquina.
	var rot := Basis(Vector3.UP, deg_to_rad(90.0))
	for k in 2:
		var z := -module * (k + 1)
		kit.instance(road, ROAD_STRAIGHT, "Side%02d" % k, Transform3D(rot, Vector3(corner_x, 0, z)))

	# Construcoes do lado +Z, fachada (-Z) voltada para a via. Recuo de vitrine: 0,8 m da calcada.
	var front_z := module * 0.5 + 0.8
	var row: Node3D = kit.add_node(root, "Buildings")
	var x_cursor := -40.0
	x_cursor = _place_row(row, TREE, "Tree01", x_cursor, front_z + 0.6, 2.0)
	x_cursor = _place_row(row, CITY_HALL, "CityHall", x_cursor, front_z, 3.0)
	x_cursor = _place_row(row, TREE, "Tree02", x_cursor, front_z + 0.6, 2.0)
	x_cursor = _place_row(row, HOUSE, "SmallHouse", x_cursor, front_z, 3.0)
	x_cursor = _place_row(row, TREE, "Tree03", x_cursor, front_z + 0.6, 2.0)
	x_cursor = _place_row(row, GROCERY, "GrocerySmall", x_cursor, front_z, 2.0)
	_place_row(row, TREE, "Tree04", x_cursor, front_z + 0.6, 2.0)

	# Pecas de escala (paradas): SIM na calcada, caminhao na faixa do sentido +X (mao direita).
	var props: Node3D = kit.add_node(root, "ScaleProps")
	var face_px := Basis(Vector3.UP, deg_to_rad(-90.0))   # frente -Z -> +X
	kit.instance(props, SIM, "SimPedestrian", Transform3D(face_px, Vector3(12.0, 0.18, module * 0.5 - 1.0)))
	kit.instance(props, TRUCK, "TruckCargo", Transform3D(face_px, Vector3(24.0, 0.03, 3.25 * 0.5)))

	_add_camera(root)

	kit._own(root, root)
	var ps := PackedScene.new()
	var e := ps.pack(root)
	if e == OK:
		e = ResourceSaver.save(ps, OUT_PATH)
	if e != OK:
		kit.err("Falha ao salvar prancha %s (%s)" % [OUT_PATH, error_string(e)])
	else:
		print("[board] ", OUT_PATH)
	root.free()


func _envelope(inst: Node3D) -> AABB:
	if inst.has_meta("visual_envelope_min") and inst.has_meta("visual_envelope_size"):
		return AABB(inst.get_meta("visual_envelope_min"), inst.get_meta("visual_envelope_size"))
	return AABB(Vector3(-1, 0, -1), Vector3(2, 2, 2))


## Coloca a cena com a frente do envelope em `front_z` e a borda -X em `x_left`.
## Devolve a nova borda livre (+X) somando `gap`.
func _place_row(parent: Node3D, path: String, node_name: String, x_left: float, front_z: float, gap: float) -> float:
	var inst: Node3D = _kit.instance(parent, path, node_name)
	var env := _envelope(inst)
	inst.position = Vector3(x_left - env.position.x, 0, front_z - env.position.z)
	return x_left + env.size.x + gap


func _lamp_at_slot(parent: Node3D, piece: Node3D, node_name: String) -> void:
	var slot := piece.find_child("Candidate_LampSlot", true, false) as Node3D
	if slot == null:
		return
	var xf: Transform3D = piece.transform * slot.transform
	# O poste tem o braco para -Z local; alinhar o -Z do poste com o forward do slot.
	_kit.instance(parent, ROAD_LAMP, node_name, Transform3D(xf.basis.orthonormalized(), xf.origin))


func _add_environment(root: Node3D) -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#cfdee8")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#c9d5df")
	env.ambient_light_energy = 0.55
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 1.6
	var we := WorldEnvironment.new()
	we.name = "PreviewEnvironment"
	we.environment = env
	root.add_child(we)
	var sun := DirectionalLight3D.new()
	sun.name = "PreviewSun"
	sun.transform = Transform3D(Basis.looking_at(Vector3(0.45, -1.0, 0.8).normalized(), Vector3.UP), Vector3(0, 30, 0))
	sun.light_color = Color("#fff3e2")
	sun.light_energy = 1.15
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 600.0
	root.add_child(sun)


func _add_ground(root: Node3D) -> void:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color("#8fae6e")
	mat.roughness = 1.0
	var pm := PlaneMesh.new()
	pm.size = Vector2(150, 110)
	pm.material = mat
	var mi := MeshInstance3D.new()
	mi.name = "PreviewGround"
	mi.mesh = pm
	mi.position = Vector3(8, -0.01, 0)
	root.add_child(mi)


func _add_camera(root: Node3D) -> void:
	var cam := Camera3D.new()
	cam.name = "PreviewCameraIso"
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = 62.0
	cam.near = 0.1
	cam.far = 1000.0
	var d := Vector3(1, 1, -1).normalized()
	var target := Vector3(8, 0, 2)
	cam.transform = Transform3D(Basis.looking_at(-d, Vector3.UP), target + d * 200.0)
	cam.current = true
	root.add_child(cam)
