## Gerador original da amostra RecoveryEnvironment (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
## Gera, a partir dos shaders em ../Shaders:
##   Materials/env_grass_soft.tres   (ShaderMaterial so com o shader; padroes vivem no shader)
##   Materials/env_lake_water.tres
##   Preview/recovery_environment_preview.tscn
##
## A cena de previa repete a camera, a luz e o ambiente do jogo (Main.cs) e o chao, a
## saia de terra, o lago e a borda do mapa do CityWorld.cs, com as mesmas medidas, para
## comparar "antes x depois" no mesmo angulo. E uma vitrine: nao e partida, nao povoa o
## jogo e nao define lotes, recuos, acessos ou densidade de vegetacao.
##
## Uso (headless funciona):
##   godot --headless --path src/IndexCities.Godot --script res://Assets/RecoveryEnvironment/Source/build_recovery_environment.gd
extends SceneTree

const ROOT := "res://Assets/RecoveryEnvironment"
const GRASS_SHADER := ROOT + "/Shaders/env_grass_soft.gdshader"
const WATER_SHADER := ROOT + "/Shaders/env_lake_water.gdshader"
const GRASS_MATERIAL := ROOT + "/Materials/env_grass_soft.tres"
const WATER_MATERIAL := ROOT + "/Materials/env_lake_water.tres"
const PREVIEW_SCENE := ROOT + "/Preview/recovery_environment_preview.tscn"

## Pecas do pacote V1 usadas so como CONTEXTO na previa (instanciadas, nao copiadas
## nem alteradas).
const V1_TREE := "res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn"
const V1_ROAD := "res://Assets/V1/Scenes/road.urban_1x1.a.straight.tscn"
const V1_HOUSE := "res://Assets/V1/Scenes/residential.small_house.a.tscn"

## --- Valores do jogo (copiados como NUMEROS de Main.cs / CityWorld.cs / simulacao) ---
## Tuning.MapHalfSize = 256 (Model.cs).
const MAP_HALF := 256.0
## Lago da seed 42 (CitySimulation: xorshift64* da propria simulacao, 3 primeiros sorteios):
## centro (152,6085; 86,0368), raio 28,3783 m. Nesta previa sao so numeros de vitrine.
const LAKE_CENTRE := Vector2(152.6085, 86.0368)
const LAKE_RADIUS := 28.3783
## Camera (Main.UpdateCamera): ortogonal, size = zoom, far 2000,
## posicao = foco + (300, 330, 300) girado em Y por (yaw - 45) graus, olhando o foco.
const CAMERA_OFFSET := Vector3(300, 330, 300)
const PREVIEW_FOCUS := Vector3(145, 0, 72)
const PREVIEW_ZOOM := 110.0
const PREVIEW_YAW := 45.0
## Hora da previa: 10:00 (luz de dia tipica, sol ainda inclinado).
const PREVIEW_HOUR := 10.0

## Chao de vitrine + contexto: fileira de rua, casas e arvores espalhadas como no jogo.
const TREE_ATTEMPTS := 145      # mesma quantidade de tentativas do CityWorld
const TREE_RNG_SEED := 20261013 # RNG proprio da previa (nao e o do jogo)
const ROAD_Z := 30.0
const ROAD_X0 := 105.25
const ROAD_PIECES := 10
const ROAD_PIECE_LEN := 10.5
const HOUSE_XS := [125.0, 140.0, 155.0]
const HOUSE_Z := 40.25
const CONTEXT_KEEP_OUT := Rect2(95.0, 20.0, 120.0, 30.0)  # x, z, largura, profundidade


func _initialize() -> void:
	var ok := _build()
	quit(0 if ok else 1)


func _build() -> bool:
	for p in [GRASS_SHADER, WATER_SHADER, V1_TREE, V1_ROAD, V1_HOUSE]:
		if not ResourceLoader.exists(p):
			push_error("[build] recurso ausente: %s" % p)
			return false
	for dir in [ROOT + "/Materials", ROOT + "/Preview"]:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(dir))
	var grass := _save_material(GRASS_SHADER, GRASS_MATERIAL, "env_grass_soft")
	var water := _save_material(WATER_SHADER, WATER_MATERIAL, "env_lake_water")
	if grass == null or water == null:
		return false
	var scene := _make_preview(grass, water)
	var packed := PackedScene.new()
	var err := packed.pack(scene)
	if err != OK:
		push_error("[build] falha ao empacotar a previa: %s" % error_string(err))
		return false
	err = ResourceSaver.save(packed, PREVIEW_SCENE)
	scene.free()
	if err != OK:
		push_error("[build] falha ao salvar %s: %s" % [PREVIEW_SCENE, error_string(err)])
		return false
	print("[build] OK: %s, %s, %s" % [GRASS_MATERIAL, WATER_MATERIAL, PREVIEW_SCENE])
	return true


func _save_material(shader_path: String, out_path: String, name: String) -> ShaderMaterial:
	var shader: Shader = load(shader_path)
	if shader == null or shader.get_shader_uniform_list().is_empty():
		push_error("[build] shader nao compilou ou sem uniforms: %s" % shader_path)
		return null
	# Texto escrito direto: so o shader. Os valores padrao ficam no proprio .gdshader (fonte
	# unica); o ResourceSaver gravaria cada uniform como `null` no renderizador headless.
	var text := "[gd_resource type=\"ShaderMaterial\" format=3]\n\n"
	text += "[ext_resource type=\"Shader\" path=\"%s\" id=\"1_shader\"]\n\n" % shader_path
	text += "[resource]\nresource_name = \"%s\"\nshader = ExtResource(\"1_shader\")\n" % name
	var f := FileAccess.open(out_path, FileAccess.WRITE)
	if f == null:
		push_error("[build] falha ao gravar %s: %s" % [out_path, error_string(FileAccess.get_open_error())])
		return null
	f.store_string(text)
	f.close()
	# Recarrega do disco para a cena referenciar o arquivo (ext_resource), nao uma copia.
	var m := ResourceLoader.load(out_path, "", ResourceLoader.CACHE_MODE_REPLACE) as ShaderMaterial
	if m == null or m.shader == null or m.shader.resource_path != shader_path:
		push_error("[build] material relido invalido: %s" % out_path)
		return null
	return m


## Mesma formula de Main._Process: luz do dia pelo horario simulado.
static func sun_for_hour(hour: float) -> Dictionary:
	var daylight := clampf(sin((hour - 6.0) / 12.0 * PI), 0.0, 1.0)
	return {
		"energy": 0.12 + daylight * 0.88,
		"rotation_degrees": Vector3(-15.0 - daylight * 45.0, -30.0, 0.0),
	}


## Mesma formula de Main.UpdateCamera.
static func camera_transform(focus: Vector3, yaw: float) -> Transform3D:
	var pos := focus + CAMERA_OFFSET.rotated(Vector3.UP, deg_to_rad(yaw - 45.0))
	return Transform3D(Basis.looking_at(focus - pos, Vector3.UP), pos)


## Mesmo material plano do CityWorld.Material(hex): albedo + roughness 0,85.
static func flat_material(hex: String) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(hex)
	m.roughness = 0.85
	return m


func _own(node: Node, owner_node: Node) -> void:
	node.owner = owner_node


func _add(parent: Node, child: Node, owner_node: Node) -> Node:
	parent.add_child(child)
	_own(child, owner_node)
	return child


func _box(name: String, size: Vector3, at: Vector3, material: Material) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var mi := MeshInstance3D.new()
	mi.name = name
	mi.mesh = mesh
	mi.position = at
	mi.material_override = material
	return mi


func _make_preview(grass: ShaderMaterial, water: ShaderMaterial) -> Node3D:
	var root := Node3D.new()
	root.name = "RecoveryEnvironmentPreview"
	root.set_meta("asset_status", "amostra RecoveryEnvironment - revisao humana PENDENTE")
	root.set_meta("purpose", "vitrine de materiais de grama e agua; nao e partida nem estado do jogo")
	root.set_meta("parity", "camera/luz/ambiente de Main.cs; chao, saia, lago e borda de CityWorld.cs")
	root.set_meta("lake_centre_xz", LAKE_CENTRE)
	root.set_meta("lake_radius_m", LAKE_RADIUS)
	root.set_meta("preview_hour", PREVIEW_HOUR)

	# Ambiente (Main._Ready): fundo cor, luz ambiente cor, nada mais alterado.
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("b9c9d5")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("afc5ce")
	env.ambient_light_energy = 0.32
	var we := WorldEnvironment.new()
	we.name = "WorldEnvironment"
	we.environment = env
	_add(root, we, root)

	# Sol (Main._Ready + formula horaria de Main._Process).
	var sun := DirectionalLight3D.new()
	sun.name = "Sun"
	var s := sun_for_hour(PREVIEW_HOUR)
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	sun.light_color = Color("fff0d0")
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 650.0
	_add(root, sun, root)

	# Camera do jogo.
	var cam := Camera3D.new()
	cam.name = "GameCamera"
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = PREVIEW_ZOOM
	cam.far = 2000.0
	cam.current = true
	cam.transform = camera_transform(PREVIEW_FOCUS, PREVIEW_YAW)
	cam.set_meta("focus", PREVIEW_FOCUS)
	cam.set_meta("yaw", PREVIEW_YAW)
	_add(root, cam, root)

	# Mundo estatico com as MESMAS medidas do CityWorld.Reset.
	var world := Node3D.new()
	world.name = "World"
	_add(root, world, root)
	var half := MAP_HALF
	_add(world, _box("Ground", Vector3(half * 2.0, 1.0, half * 2.0), Vector3(0, -0.56, 0), grass), root)
	_add(world, _box("GroundSkirt", Vector3(half * 2.0 + 6.0, 4.0, half * 2.0 + 6.0), Vector3(0, -3.0, 0), flat_material("917656")), root)

	var lake_mesh := CylinderMesh.new()
	lake_mesh.top_radius = LAKE_RADIUS
	lake_mesh.bottom_radius = LAKE_RADIUS + 1.0
	lake_mesh.height = 0.15
	lake_mesh.radial_segments = 64
	var lake := MeshInstance3D.new()
	lake.name = "Lake"
	lake.mesh = lake_mesh
	lake.position = Vector3(LAKE_CENTRE.x, -0.04, LAKE_CENTRE.y)
	lake.material_override = water
	_add(world, lake, root)

	var border := flat_material("dce8b9")
	var b := 0
	for a in [Vector3(-half, 0.2, 0), Vector3(half, 0.2, 0), Vector3(0, 0.2, -half), Vector3(0, 0.2, half)]:
		var along_x: bool = absf(a.z) > 0.01
		var size := Vector3(half * 2.0, 0.12, 0.25) if along_x else Vector3(0.25, 0.12, half * 2.0)
		_add(world, _box("MapBorder%d" % b, size, a, border), root)
		b += 1

	# Contexto (pecas V1 em escala 1, sem alteracao).
	var context := Node3D.new()
	context.name = "Context"
	_add(root, context, root)
	var road_scene: PackedScene = load(V1_ROAD)
	for i in ROAD_PIECES:
		var r := road_scene.instantiate() as Node3D
		r.name = "Road%02d" % i
		r.position = Vector3(ROAD_X0 + ROAD_PIECE_LEN * i, 0, ROAD_Z)
		_add(context, r, root)
	var house_scene: PackedScene = load(V1_HOUSE)
	for i in HOUSE_XS.size():
		var h := house_scene.instantiate() as Node3D
		h.name = "House%d" % i
		h.position = Vector3(HOUSE_XS[i], 0, HOUSE_Z)
		_add(context, h, root)

	# Arvores: mesma quantidade de tentativas e mesmas exclusoes do CityWorld (lago + 9 m,
	# faixa |z| < 10 m da rodovia), mais a area de contexto; escala 0,6-1,05 e giro em Y.
	var tree_scene: PackedScene = load(V1_TREE)
	var rng := RandomNumberGenerator.new()
	rng.seed = TREE_RNG_SEED
	var trees := 0
	for i in TREE_ATTEMPTS:
		var p := Vector2(rng.randf_range(-1.0, 1.0) * (half - 10.0), rng.randf_range(-1.0, 1.0) * (half - 10.0))
		var scale := rng.randf_range(0.6, 1.05)
		var turn := float(rng.randi_range(0, 359))
		if p.distance_to(LAKE_CENTRE) < LAKE_RADIUS + 9.0 or absf(p.y) < 10.0:
			continue
		if CONTEXT_KEEP_OUT.grow(4.0).has_point(p):
			continue
		var t := tree_scene.instantiate() as Node3D
		t.name = "Tree%03d" % trees
		t.position = Vector3(p.x, 0, p.y)
		t.scale = Vector3.ONE * scale
		t.rotation_degrees = Vector3(0, turn, 0)
		_add(context, t, root)
		trees += 1
	root.set_meta("context_trees", trees)
	return root
