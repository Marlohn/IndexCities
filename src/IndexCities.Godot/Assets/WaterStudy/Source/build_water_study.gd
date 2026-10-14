## IndexCities - WaterStudy - gerador dos materiais, malhas e cenas de previa (ferramenta de
## autoria; NAO e jogo). Revisao humana: PENDENTE.
##
##   godot --headless --path <projeto> --script res://Assets/WaterStudy/Source/build_water_study.gd -- [--report=<arquivo.json>]
##
## Gera, so dentro de Assets/WaterStudy:
##   Materials/*.tres            materiais (ShaderMaterial / StandardMaterial3D)
##   Meshes/*.res                malhas da amostra B e dos aderecos (deterministicas)
##   Preview/ws_before.tscn      estado atual da main 528d30d (referencia, so leitura)
##   Preview/ws_sample_a.tscn    A: MESMA malha do lago, material novo + aderecos dentro do circulo
##   Preview/ws_sample_b.tscn    B: bacia rebaixada + rio CONCEITUAIS, NAO APROVADOS PARA GAMEPLAY
##
## As tres cenas compartilham o mesmo contexto (camera, sol, ambiente, chao, saia, bordas do mapa,
## rua, postes, casas, captacao e arvores) para que so a agua/margem mude entre elas. Pecas do
## pacote V1 sao instanciadas (nao copiadas nem alteradas). Nenhuma cena tem script, colisao,
## fisica, picking, conector ou marcador funcional.
extends SceneTree

const Geo := preload("res://Assets/WaterStudy/Source/ws_shore_geometry.gd")

const ROOT := "res://Assets/WaterStudy"
const MAT := ROOT + "/Materials"
const MESH := ROOT + "/Meshes"
const PREVIEW := ROOT + "/Preview"
const V1_SCENES := "res://Assets/V1/Scenes/"
const ENV_GRASS := "res://Assets/RecoveryEnvironment/Materials/env_grass_soft.tres"
const ENV_WATER := "res://Assets/RecoveryEnvironment/Materials/env_lake_water.tres"

# Camera, sol e ambiente do Main.cs (main 528d30d).
const CAMERA_OFFSET := Vector3(300, 330, 300)
const SUN_COLOR := "fff0d0"
const BG_COLOR := "b9c9d5"
const AMBIENT_COLOR := "afc5ce"
const AMBIENT_ENERGY := 0.32
const ROAD_WIDTH := 10.5

# Contexto fixo da previa (vitrine; nao e partida nem lote aprovado).
const ROAD_Z := 51.0
const ROAD_X0 := 100.0
const ROAD_PIECES := 6
const LAMP_XS := [108.0, 140.0]
const HOUSE_XS := [108.0, 124.0, 140.0, 156.0]
const HOUSE_Z := 37.0
const INTAKE_POS := Vector2(112.0, 86.0)
const TREE_ATTEMPTS := 145
const TREE_SEED := 42 ^ 123451


static func camera_transform(focus: Vector3, yaw: float) -> Transform3D:
	var pos := focus + CAMERA_OFFSET.rotated(Vector3.UP, deg_to_rad(yaw - 45.0))
	return Transform3D(Basis.looking_at(focus - pos, Vector3.UP), pos)


## Main._Process: luz do dia pela hora; rotacao Y fixa em -30.
static func sun_for_hour(hour: float) -> Dictionary:
	var daylight := clampf(sin((hour - 6.0) / 12.0 * PI), 0.0, 1.0)
	return {"energy": 0.12 + daylight * 0.88, "rotation_degrees": Vector3(-15.0 - daylight * 45.0, -30.0, 0.0)}


static func flat(hex: String, rough := 0.85) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(hex)
	m.roughness = rough
	return m


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var report_path := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--report="):
			report_path = a.substr(9)
	var t0 := Time.get_ticks_msec()
	var report := {}
	var ok := _build(report)
	report["build_ms"] = Time.get_ticks_msec() - t0
	if report_path != "":
		var f := FileAccess.open(report_path, FileAccess.WRITE)
		f.store_string(JSON.stringify(report, "  ", false))
		f.close()
	print("[build] %s em %d ms" % ["OK" if ok else "FALHOU", report["build_ms"]])
	quit(0 if ok else 1)


func _save(res: Resource, path: String, flags := 0) -> bool:
	var err := ResourceSaver.save(res, path, flags)
	if err != OK:
		push_error("[build] falha ao salvar %s: %s" % [path, error_string(err)])
		return false
	return true


func _build(report: Dictionary) -> bool:
	for d in [MAT, MESH, PREVIEW]:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(d))
	# ---------------------------------------------------------------- materiais
	var mats := {
		"ws_water_a": "ws_water_a.gdshader", "ws_terrain_b": "ws_terrain_b.gdshader",
		"ws_water_b": "ws_water_b.gdshader", "ws_reeds": "ws_reeds.gdshader",
	}
	for name in mats:
		# Texto escrito a mao: no --headless o ResourceSaver gravaria cada uniform como `null`.
		# Assim o material so aponta o shader e os padroes continuam vivendo no shader.
		var path: String = MAT + "/%s.tres" % name
		var text := "[gd_resource type=\"ShaderMaterial\" format=3]\n\n"
		text += "[ext_resource type=\"Shader\" path=\"%s\" id=\"1_shader\"]\n\n" % (ROOT + "/Shaders/" + mats[name])
		text += "[resource]\nresource_name = \"%s\"\nshader = ExtResource(\"1_shader\")\n" % name
		var f := FileAccess.open(path, FileAccess.WRITE)
		if f == null:
			push_error("[build] falha ao escrever %s" % path)
			return false
		f.store_string(text)
		f.close()
		if ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REPLACE) == null:
			push_error("[build] material ilegivel: %s" % path)
			return false
	var rock := StandardMaterial3D.new()
	rock.resource_name = "ws_rock"
	rock.vertex_color_use_as_albedo = true
	rock.roughness = 0.9
	if not _save(rock, MAT + "/ws_rock.tres"):
		return false
	var cut := flat("2a5560", 0.35)
	cut.resource_name = "ws_river_cut"
	if not _save(cut, MAT + "/ws_river_cut.tres"):
		return false
	# ------------------------------------------------------------------ malhas
	var geo := Geo.new()
	var t_geo := Time.get_ticks_msec()
	var b := geo.build_b()
	report["geometry_ms"] = Time.get_ticks_msec() - t_geo
	report["b_stats"] = b["stats"]
	var meshes := {
		"ws_b_terrain": b["terrain"], "ws_b_water": b["water"], "ws_b_river_cut": b["cut_face"],
		"ws_reed_clump": Geo.reed_mesh(), "ws_rock": Geo.rock_mesh(),
	}
	for name in meshes:
		(meshes[name] as ArrayMesh).resource_name = name
		if not _save(meshes[name], MESH + "/%s.res" % name, ResourceSaver.FLAG_COMPRESS):
			return false
	# --------------------------------------------------------------- aderecos
	var lake_props := Geo.lake_props()
	var river_props := geo.river_props()
	report["props"] = {
		"lake_reeds": lake_props["reeds"].size(), "lake_rocks": lake_props["rocks"].size(),
		"river_reeds": river_props["reeds"].size(), "river_rocks": river_props["rocks"].size(),
	}
	# ----------------------------------------------------------------- cenas
	var trees := _tree_spots(geo)
	report["trees"] = {"total": trees.size(), "removed_in_b_by_river": trees.filter(func(t): return t["river"]).size()}
	for variant in ["before", "a", "b"]:
		var scene := _scene(variant, geo, trees, lake_props, river_props)
		var packed := PackedScene.new()
		var err := packed.pack(scene)
		if err != OK:
			push_error("[build] pack %s: %s" % [variant, error_string(err)])
			return false
		var path: String = PREVIEW + "/" + {"before": "ws_before", "a": "ws_sample_a", "b": "ws_sample_b"}[variant] + ".tscn"
		scene.free()
		if not _save(packed, path):
			return false
	print("[build] materiais, malhas e cenas gerados; B: %s" % [b["stats"]])
	return true


## Mesma regra de arvores do CityWorld.Reset (145 tentativas, fora de lago + 9 m e de |z| < 10)
## e do CityWorld.Sync (sem arvores sobre lotes e perto de ruas). RNG proprio (deterministico).
func _tree_spots(geo) -> Array:
	var rng := RandomNumberGenerator.new()
	rng.seed = TREE_SEED
	var half := Geo.MAP_HALF
	var out := []
	for i in TREE_ATTEMPTS:
		var p := Vector2((rng.randf() * 2.0 - 1.0) * (half - 10.0), (rng.randf() * 2.0 - 1.0) * (half - 10.0))
		var scale := 0.6 + rng.randf() * 0.45
		var yaw := float(rng.randi_range(0, 359))
		if p.distance_to(Geo.LAKE_CENTRE) < Geo.LAKE_RADIUS + 9.0 or absf(p.y) < 10.0:
			continue
		if _near_context(p):
			continue
		var q: Dictionary = geo.river_query(p)
		var in_river := false
		if q["found"]:
			in_river = float(q["dist"]) < Geo.river_half_width(q["u"]) + Geo.river_bank_width(q["u"]) + 3.0
		out.append({"p": p, "scale": scale, "yaw": yaw, "river": in_river})
	return out


func _near_context(p: Vector2) -> bool:
	if absf(p.y - ROAD_Z) < ROAD_WIDTH / 2.0 + 3.0 and p.x > ROAD_X0 - 3.0 and p.x < ROAD_X0 + ROAD_PIECES * ROAD_WIDTH + 3.0:
		return true
	for hx in HOUSE_XS:
		if absf(p.x - hx) < 8.0 + 4.0 and absf(p.y - HOUSE_Z) < 8.0 + 4.0:
			return true
	return absf(p.x - INTAKE_POS.x) < 8.0 + 4.0 and absf(p.y - INTAKE_POS.y) < 8.0 + 4.0


func _add(parent: Node, child: Node, owner_node: Node) -> Node:
	parent.add_child(child)
	child.owner = owner_node
	return child


func _instance(id: String) -> Node3D:
	var packed: PackedScene = load(V1_SCENES + id + ".tscn")
	return packed.instantiate(PackedScene.GEN_EDIT_STATE_INSTANCE) as Node3D


func _box(size: Vector3, pos: Vector3, mat: Material, node_name: String) -> MeshInstance3D:
	var bm := BoxMesh.new()
	bm.size = size
	var mi := MeshInstance3D.new()
	mi.name = node_name
	mi.mesh = bm
	mi.position = pos
	mi.material_override = mat
	return mi


func _scene(variant: String, geo, trees: Array, lake_props: Dictionary, river_props: Dictionary) -> Node3D:
	var root := Node3D.new()
	root.name = {"before": "WaterStudyBefore", "a": "WaterStudySampleA", "b": "WaterStudySampleB"}[variant]
	root.set_meta("study", "IndexCities WaterStudy - revisao humana PENDENTE")
	root.set_meta("variant", variant)
	root.set_meta("lake_centre_xz", Geo.LAKE_CENTRE)
	root.set_meta("lake_radius_m", Geo.LAKE_RADIUS)
	match variant:
		"before":
			root.set_meta("note", "Estado atual da main 528d30d: CylinderMesh do CityWorld + materiais RecoveryEnvironment (so leitura).")
		"a":
			root.set_meta("note", "Amostra A: mesma malha, posicao, raio, altura e silhueta do lago; so material novo e aderecos decorativos dentro do circulo.")
		"b":
			root.set_meta("note", "Amostra B: GEOMETRIA CONCEITUAL NAO APROVADA PARA GAMEPLAY (bacia rebaixada, margem irregular, trecho de rio). So previa isolada.")
			root.set_meta("conceptual_geometry_not_approved_for_gameplay", true)
	# Ambiente, sol e camera (Main._Ready / _Process / UpdateCamera).
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(BG_COLOR)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(AMBIENT_COLOR)
	env.ambient_light_energy = AMBIENT_ENERGY
	var we := WorldEnvironment.new()
	we.name = "WorldEnvironment"
	we.environment = env
	_add(root, we, root)
	var sun := DirectionalLight3D.new()
	sun.name = "Sun"
	var s := sun_for_hour(10.0)
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	sun.light_color = Color(SUN_COLOR)
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 650.0
	_add(root, sun, root)
	var cam := Camera3D.new()
	cam.name = "GameCamera"
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = 110.0
	cam.far = 2000.0
	cam.current = true
	cam.transform = camera_transform(Vector3(Geo.LAKE_CENTRE.x, 0, Geo.LAKE_CENTRE.y), 45.0)
	_add(root, cam, root)
	# Mundo: mesmas medidas do CityWorld.Reset.
	var world := _add(root, Node3D.new(), root)
	world.name = "World"
	var half := Geo.MAP_HALF
	if variant == "b":
		var ground := MeshInstance3D.new()
		ground.name = "GroundConceptualBasin"
		ground.mesh = load(MESH + "/ws_b_terrain.res")
		ground.material_override = load(MAT + "/ws_terrain_b.tres")
		ground.set_meta("conceptual_geometry_not_approved_for_gameplay", true)
		_add(world, ground, root)
	else:
		_add(world, _box(Vector3(half * 2, 1, half * 2), Vector3(0, -0.56, 0), load(ENV_GRASS), "Ground"), root)
	_add(world, _box(Vector3(half * 2 + 6, 4, half * 2 + 6), Vector3(0, -3, 0), flat("917656"), "Skirt"), root)
	var border := flat("dce8b9")
	var bi := 0
	for a in [Vector3(-half, 0.2, 0), Vector3(half, 0.2, 0), Vector3(0, 0.2, -half), Vector3(0, 0.2, half)]:
		var along_x := absf(a.z) > 0.01
		_add(world, _box(Vector3(half * 2, 0.12, 0.25) if along_x else Vector3(0.25, 0.12, half * 2), a, border, "MapBorder%d" % bi), root)
		bi += 1
	# Agua.
	if variant == "b":
		var water := MeshInstance3D.new()
		water.name = "WaterConceptual"
		water.mesh = load(MESH + "/ws_b_water.res")
		water.material_override = load(MAT + "/ws_water_b.tres")
		water.set_meta("conceptual_geometry_not_approved_for_gameplay", true)
		_add(world, water, root)
		var cutmi := MeshInstance3D.new()
		cutmi.name = "RiverMapEdgeCut"
		cutmi.mesh = load(MESH + "/ws_b_river_cut.res")
		cutmi.material_override = load(MAT + "/ws_river_cut.tres")
		cutmi.set_meta("conceptual_geometry_not_approved_for_gameplay", true)
		_add(world, cutmi, root)
	else:
		var cyl := CylinderMesh.new()
		cyl.top_radius = Geo.LAKE_RADIUS
		cyl.bottom_radius = Geo.LAKE_RADIUS + 1.0
		cyl.height = 0.15
		cyl.radial_segments = 64
		var lake := MeshInstance3D.new()
		lake.name = "LakeSurface"
		lake.mesh = cyl
		lake.position = Vector3(Geo.LAKE_CENTRE.x, -0.04, Geo.LAKE_CENTRE.y)
		lake.material_override = load(ENV_WATER if variant == "before" else MAT + "/ws_water_a.tres")
		_add(world, lake, root)
	# Aderecos decorativos (A e B): juncos e pedras; so aparencia.
	if variant != "before":
		var props := _add(world, Node3D.new(), root)
		props.name = "DecorativeProps"
		props.set_meta("note", "Juncos e pedras decorativos: sem colisao, sem estado, nao contam como vegetacao/recurso.")
		var reeds: Array = lake_props["reeds"].duplicate()
		var rocks: Array = lake_props["rocks"].duplicate()
		if variant == "b":
			reeds.append_array(river_props["reeds"])
			rocks.append_array(river_props["rocks"])
		_add(props, _multimesh("Reeds", load(MESH + "/ws_reed_clump.res"), load(MAT + "/ws_reeds.tres"), reeds, variant, geo, 0.02, 801), root)
		_add(props, _multimesh("Rocks", load(MESH + "/ws_rock.res"), load(MAT + "/ws_rock.tres"), rocks, variant, geo, 0.06, 809), root)
	# Contexto: rua com postes, casas, captacao e arvores (identico nas tres cenas).
	var ctx := _add(root, Node3D.new(), root)
	ctx.name = "Context"
	for k in ROAD_PIECES:
		var piece := _instance("road.urban_1x1.a.straight")
		piece.name = "Road%d" % k
		piece.position = Vector3(ROAD_X0 + ROAD_WIDTH * (k + 0.5), 0.01, ROAD_Z)
		_add(ctx, piece, root)
	for k in LAMP_XS.size():
		var lamp := _instance("road.urban_1x1.a.lamp")
		lamp.name = "Lamp%d" % k
		lamp.position = Vector3(LAMP_XS[k], 0.01 + 0.18, ROAD_Z + ROAD_WIDTH / 2.0 - 0.7)
		_add(ctx, lamp, root)
		var light := OmniLight3D.new()
		light.name = "LampLight"
		var marker := lamp.get_node_or_null("Candidate_LightSource") as Node3D
		light.position = marker.position if marker != null else Vector3(0, 5, 0)
		light.light_color = Color("ffd793")
		light.omni_range = 17.0
		light.light_energy = 1.3
		light.shadow_enabled = false
		light.visible = false
		_add(lamp, light, root)
	for k in HOUSE_XS.size():
		var house := _instance("residential.small_house.a")
		house.name = "House%d" % k
		house.position = Vector3(HOUSE_XS[k], 0, HOUSE_Z)
		house.rotation_degrees = Vector3(0, 180, 0)
		_add(ctx, house, root)
	var intake := _instance("utility.water_intake.a")
	intake.name = "WaterIntake"
	intake.position = Vector3(INTAKE_POS.x, 0, INTAKE_POS.y)
	intake.rotation_degrees = Vector3(0, 90, 0)
	_add(ctx, intake, root)
	var tree_root := _add(ctx, Node3D.new(), root)
	tree_root.name = "Trees"
	var n := 0
	for t in trees:
		if variant == "b" and t["river"]:
			continue
		var tree := _instance("vegetation.tree_broadleaf.a")
		tree.name = "Tree%d" % n
		tree.position = Vector3(t["p"].x, 0, t["p"].y)
		tree.scale = Vector3.ONE * float(t["scale"])
		tree.rotation_degrees = Vector3(0, t["yaw"], 0)
		_add(tree_root, tree, root)
		n += 1
	return root


## O buffer e montado aqui (12 floats da Transform3D por linhas + 4 da cor, por instancia) e
## atribuido de uma vez: no modo --headless o servidor de renderizacao ficticio guarda o buffer,
## mas ignora set_instance_transform(), e a cena seria salva com todas as instancias na origem.
func _multimesh(node_name: String, mesh: Mesh, mat: Material, spots: Array, variant: String, geo, sink: float, salt: int) -> MultiMeshInstance3D:
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.use_colors = true
	mm.mesh = mesh
	mm.instance_count = spots.size()
	var buf := PackedFloat32Array()
	for i in spots.size():
		var s: Vector4 = spots[i]
		var y := 0.035
		if variant == "b":
			y = (geo.fields(Vector2(s.x, s.y)) as PackedFloat32Array)[0]
			y = maxf(y, Geo.WATER_Y - 0.25)
		var bs := Basis(Vector3.UP, s.z).scaled(Vector3.ONE * s.w)
		var o := Vector3(s.x, y - sink * s.w, s.y)
		buf.append_array([bs.x.x, bs.y.x, bs.z.x, o.x, bs.x.y, bs.y.y, bs.z.y, o.y, bs.x.z, bs.y.z, bs.z.z, o.z])
		var tint := 0.85 + 0.25 * Geo.ws_hash(i, 1, salt)
		var warm := 0.06 * (Geo.ws_hash(i, 2, salt) - 0.5)
		buf.append_array([tint + warm, tint, tint - warm, 1.0])
	mm.buffer = buf
	var mmi := MultiMeshInstance3D.new()
	mmi.name = node_name
	mmi.multimesh = mm
	mmi.material_override = mat
	return mmi
