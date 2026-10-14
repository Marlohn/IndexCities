## IndexCities - WaterStudy - AMOSTRA C: gerador de materiais, malhas e cenas de previa
## (ferramenta de autoria; NAO e jogo). Revisao humana: PENDENTE. Proposta NAO integrada.
##
##   godot --headless --path <projeto> --script res://Assets/WaterStudy/Source/build_water_study_c.gd -- \
##         [--seeds=42] [--dest=res://Assets/WaterStudy] [--param=water_drop_m:0.15] [--tag=] [--report=<json>]
##
## Le os lagos reais de Source/ws_c_cases.json (centro/raio de new CitySimulation(seed).State) e gera,
## para cada seed, em <dest>:
##   Meshes/C/ws_c_s<seed><tag>_{ground,bed,water}.res
##   Preview/C/ws_sample_c_s<seed><tag>.tscn     amostra C (lago so dentro do circulo real)
##   Preview/C/ws_c_before_s<seed>.tscn          referencia com materiais integrados do jogo nessa seed
##                                               (nao e uma captura de partida real)
## Materiais (uma vez): Materials/ws_c_water.tres, Materials/ws_c_lakebed.tres.
##
## A seed 42 e a VITRINE versionada (mesmo contexto das cenas A/B, inclusive as arvores de
## ws_before.tscn, para comparar lado a lado). As outras seeds conferem a parametrizacao: o mesmo
## gerador, so com outro centro/raio, e o contexto da vitrine reexpresso em relacao ao lago, com as
## arvores pela regra do CityWorld. Nenhum arquivo de A/B e alterado. Nenhuma cena tem script,
## colisao, fisica, picking ou conector. Os exemplos de limite (lotes validos que entram no circulo)
## ficam num no oculto `LimitExamples`, ligado so na tomada de limite.
extends SceneTree

const LakeC := preload("res://Assets/WaterStudy/Source/ws_lake_c.gd")
const Build := preload("res://Assets/WaterStudy/Source/build_water_study.gd")

const ROOT := "res://Assets/WaterStudy"
const CASES := ROOT + "/Source/ws_c_cases.json"
const AB_BEFORE := ROOT + "/Preview/ws_before.tscn"
const V1_SCENES := "res://Assets/V1/Scenes/"
const ENV_GRASS := "res://Assets/RecoveryEnvironment/Materials/env_grass_soft.tres"
const ENV_WATER := "res://Assets/RecoveryEnvironment/Materials/env_lake_water.tres"
const MAT_WATER := ROOT + "/Materials/ws_c_water.tres"
const MAT_BED := ROOT + "/Materials/ws_c_lakebed.tres"
const ROAD_WIDTH := 10.5
## Lotes do catalogo (Model.cs) usados nos exemplos de limite.
const LOTS := {"House": Vector2(12, 16), "Farm": Vector2(36, 40)}
const LIMIT_SCENES := {"House": "residential.small_house.a", "Farm": "agriculture.farm_small.a"}
## Foco das tomadas de A/B (seed 42), reexpresso em relacao ao centro do lago.
const F110_OFFSET := Vector2(158.0 - 152.608513, 82.0 - 85.982556)


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var seeds := []
	var dest := ROOT
	var tag := ""
	var report_path := ""
	var params := {}
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--seeds="):
			for s in a.substr(8).split(","):
				seeds.append(int(s))
		elif a.begins_with("--dest="):
			dest = a.substr(7)
		elif a.begins_with("--tag="):
			tag = a.substr(6)
		elif a.begins_with("--param="):
			var kv := a.substr(8).split(":")
			params[kv[0]] = float(kv[1])
		elif a.begins_with("--report="):
			report_path = a.substr(9)
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(CASES))
	var report := {"cases_source": data["source"], "params": params, "dest": dest, "tag": tag, "seeds": {}}
	var ok := _materials()
	for c in data["cases"]:
		if not seeds.is_empty() and not (int(c["seed"]) in seeds):
			continue
		if ok:
			ok = _seed(c, dest, tag, params, report)
	if report_path != "":
		var f := FileAccess.open(report_path, FileAccess.WRITE)
		f.store_string(JSON.stringify(report, "  ", false))
		f.close()
	print("[build-c] %s" % ("OK" if ok else "FALHOU"))
	quit(0 if ok else 1)


func _save(res: Resource, path: String, flags := 0) -> bool:
	var err := ResourceSaver.save(res, path, flags)
	if err != OK:
		push_error("[build-c] falha ao salvar %s: %s" % [path, error_string(err)])
		return false
	return true


## Texto escrito a mao (como em A/B): no --headless o ResourceSaver gravaria cada uniform como
## `null`. O material so aponta o shader; os padroes vivem no shader.
func _materials() -> bool:
	for pair in [[MAT_WATER, "ws_water_c.gdshader", "ws_c_water"], [MAT_BED, "ws_lakebed_c.gdshader", "ws_c_lakebed"]]:
		var text := "[gd_resource type=\"ShaderMaterial\" format=3]\n\n"
		text += "[ext_resource type=\"Shader\" path=\"%s\" id=\"1_shader\"]\n\n" % (ROOT + "/Shaders/" + pair[1])
		text += "[resource]\nresource_name = \"%s\"\nshader = ExtResource(\"1_shader\")\n" % pair[2]
		var f := FileAccess.open(pair[0], FileAccess.WRITE)
		if f == null:
			push_error("[build-c] falha ao escrever %s" % pair[0])
			return false
		f.store_string(text)
		f.close()
		if ResourceLoader.load(pair[0], "", ResourceLoader.CACHE_MODE_REPLACE) == null:
			push_error("[build-c] material ilegivel: %s" % pair[0])
			return false
	return true


func _seed(c: Dictionary, dest: String, tag: String, params: Dictionary, report: Dictionary) -> bool:
	var seed := int(c["seed"])
	var centre := Vector2(c["lake_centre"][0], c["lake_centre"][1])
	var radius := float(c["lake_radius"])
	for d in [dest + "/Meshes/C", dest + "/Preview/C"]:
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(d))
	var gen = LakeC.new(params)
	var t0 := Time.get_ticks_msec()
	var m: Dictionary = gen.build(centre, radius)
	var gen_ms := Time.get_ticks_msec() - t0
	var base := "ws_c_s%d%s" % [seed, tag]
	var paths := {}
	for k in ["ground", "bed", "water"]:
		var mesh := m[k] as ArrayMesh
		mesh.resource_name = "%s_%s" % [base, k]
		paths[k] = "%s/Meshes/C/%s_%s.res" % [dest, base, k]
		if not _save(mesh, paths[k], ResourceSaver.FLAG_COMPRESS):
			return false
	var ctx := _context_data(c, seed)
	var scenes := {"c": "%s/Preview/C/ws_sample_c_s%d%s.tscn" % [dest, seed, tag]}
	# Compare with the integrated lake material, including the seed 42 showcase.
	# Historical A/B scenes remain untouched.
	if tag == "":
		scenes["before"] = "%s/Preview/C/ws_c_before_s%d.tscn" % [dest, seed]
	for variant in scenes:
		var scene := _scene(variant, c, centre, radius, m["stats"], paths, ctx, params)
		var packed := PackedScene.new()
		var err := packed.pack(scene)
		scene.free()
		if err != OK:
			push_error("[build-c] pack %s: %s" % [scenes[variant], error_string(err)])
			return false
		if not _save(packed, scenes[variant]):
			return false
	report["seeds"][str(seed)] = {"stats": m["stats"], "generate_ms": gen_ms, "meshes": paths, "scenes": scenes,
		"trees": ctx["trees"].size(), "trees_source": ctx["trees_source"]}
	print("[build-c] seed %d: R %.3f, centro (%.3f; %.3f), %d ms, %s" % [seed, radius, centre.x, centre.y, gen_ms, scenes.values()])
	return true


## Contexto: vitrine da seed (dados reais conferidos), arvores e exemplos de limite.
func _context_data(c: Dictionary, seed: int) -> Dictionary:
	var ctx := {"trees": [], "trees_source": ""}
	if seed == 42 and ResourceLoader.exists(AB_BEFORE):
		# Mesmas arvores das cenas historicas de A/B, para a comparacao lado a lado.
		var before := (load(AB_BEFORE) as PackedScene).instantiate() as Node3D
		for t in before.get_node("Context/Trees").get_children():
			var n3 := t as Node3D
			ctx["trees"].append({"p": Vector2(n3.position.x, n3.position.z), "scale": n3.scale.x, "yaw": n3.rotation_degrees.y})
		before.free()
		ctx["trees_source"] = "Preview/ws_before.tscn (vitrine historica A/B)"
	else:
		for t in c["trees"]:
			ctx["trees"].append({"p": Vector2(t["p"][0], t["p"][1]), "scale": float(t["scale"]), "yaw": float(t["yaw_deg"])})
		ctx["trees_source"] = "ws_c_cases.json (transcricao de CityWorld.Reset/Sync)"
	# Regra do CityWorld.Sync: arvore escondida a menos de max(L, P)/2 + 4 de um lote.
	for t in ctx["trees"]:
		t["hidden_by_limits"] = false
		for k in c["limits"]:
			var lim: Dictionary = c["limits"][k]
			var lot: Vector2 = LOTS[lim["kind"]]
			var half := maxf(lot.x, lot.y) / 2.0 + 4.0
			if absf(t["p"].x - lim["position"][0]) < half and absf(t["p"].y - lim["position"][1]) < half:
				t["hidden_by_limits"] = true
	return ctx


static func flat(hex: String, rough := 0.85) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(hex)
	m.roughness = rough
	return m


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


func _scene(variant: String, c: Dictionary, centre: Vector2, radius: float, stats: Dictionary, paths: Dictionary, ctx: Dictionary, params: Dictionary) -> Node3D:
	var seed := int(c["seed"])
	var root := Node3D.new()
	root.name = "WaterStudySampleC" if variant == "c" else "WaterStudyBeforeC"
	root.set_meta("study", "IndexCities WaterStudy - amostra C - revisao humana PENDENTE")
	root.set_meta("variant", variant)
	root.set_meta("seed", seed)
	root.set_meta("lake_centre_xz", centre)
	root.set_meta("lake_radius_m", radius)
	root.set_meta("not_integrated", true)
	if variant == "c":
		root.set_meta("note", "Amostra C: agua visivel ate o circulo real (LakeCentre/LakeRadius), rebaixo so dentro do circulo, superficie translucida com reflexo suave. Medidas e alturas sao PROPOSTAS, nao decisoes de produto. Sem rio.")
		root.set_meta("proposal_water_y", stats["water_y"])
		root.set_meta("proposal_bank_height_m", stats["bank_height_m"])
		root.set_meta("proposal_params", params)
	else:
		root.set_meta("note", "Estado atual do jogo nesta seed: CylinderMesh do CityWorld + materiais RecoveryEnvironment (referencia).")
	# Ambiente, sol e camera (Main._Ready / _Process / UpdateCamera), como em A/B.
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(Build.BG_COLOR)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(Build.AMBIENT_COLOR)
	env.ambient_light_energy = Build.AMBIENT_ENERGY
	var we := WorldEnvironment.new()
	we.name = "WorldEnvironment"
	we.environment = env
	_add(root, we, root)
	var sun := DirectionalLight3D.new()
	sun.name = "Sun"
	var s := Build.sun_for_hour(10.0)
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	sun.light_color = Color(Build.SUN_COLOR)
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 650.0
	_add(root, sun, root)
	var cam := Camera3D.new()
	cam.name = "GameCamera"
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = 110.0
	cam.far = 2000.0
	cam.current = true
	var f110 := centre + F110_OFFSET
	cam.transform = Build.camera_transform(Vector3(f110.x, 0, f110.y), 45.0)
	_add(root, cam, root)
	# Mundo.
	var world := _add(root, Node3D.new(), root)
	world.name = "World"
	var half := LakeC.MAP_HALF
	if variant == "c":
		var ground := MeshInstance3D.new()
		ground.name = "GroundWithLakeOpening"
		ground.mesh = load(paths["ground"])
		ground.material_override = load(ENV_GRASS)
		ground.set_meta("note", "Chao do jogo (mesma grama real) com abertura exatamente no circulo do lago.")
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
	if variant == "c":
		var bed := MeshInstance3D.new()
		bed.name = "LakeBed"
		bed.mesh = load(paths["bed"])
		bed.material_override = load(MAT_BED)
		_add(world, bed, root)
		var water := MeshInstance3D.new()
		water.name = "LakeWater"
		water.mesh = load(paths["water"])
		water.material_override = load(MAT_WATER)
		water.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		_add(world, water, root)
	else:
		var cyl := CylinderMesh.new()
		cyl.top_radius = radius
		cyl.bottom_radius = radius + 1.0
		cyl.height = 0.15
		cyl.radial_segments = 64
		var lake := MeshInstance3D.new()
		lake.name = "LakeSurface"
		lake.mesh = cyl
		lake.position = Vector3(centre.x, -0.04, centre.y)
		lake.material_override = load(ENV_WATER)
		_add(world, lake, root)
	# Contexto da vitrine (dados conferidos pelas regras reais na ferramenta de dados).
	var v: Dictionary = c["vitrine"]
	var road_z := float(v["road"]["z"])
	var road_x0 := float(v["road"]["x0"])
	var context := _add(root, Node3D.new(), root)
	context.name = "Context"
	for k in int(v["road"]["pieces"]):
		var piece := _instance("road.urban_1x1.a.straight")
		piece.name = "Road%d" % k
		piece.position = Vector3(road_x0 + ROAD_WIDTH * (k + 0.5), 0.01, road_z)
		_add(context, piece, root)
	for k in v["lamps"].size():
		var lamp := _instance("road.urban_1x1.a.lamp")
		lamp.name = "Lamp%d" % k
		lamp.position = Vector3(v["lamps"][k][0], 0.01 + 0.18, road_z + ROAD_WIDTH / 2.0 - 0.7)
		_add(context, lamp, root)
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
	for k in v["houses"].size():
		var house := _instance("residential.small_house.a")
		house.name = "House%d" % k
		var hp: Array = v["houses"][k]["position"]
		house.position = Vector3(hp[0], 0, hp[1])
		house.rotation = Vector3(0, float(v["houses"][k]["rotation_rad"]), 0)
		_add(context, house, root)
	var intake := _instance("utility.water_intake.a")
	intake.name = "WaterIntake"
	intake.position = Vector3(v["intake"]["position"][0], 0, v["intake"]["position"][1])
	intake.rotation = Vector3(0, float(v["intake"]["rotation_rad"]), 0)
	_add(context, intake, root)
	var tree_root := _add(context, Node3D.new(), root)
	tree_root.name = "Trees"
	var n := 0
	for t in ctx["trees"]:
		var tree := _instance("vegetation.tree_broadleaf.a")
		tree.name = "Tree%d" % n
		tree.position = Vector3(t["p"].x, 0, t["p"].y)
		tree.scale = Vector3.ONE * float(t["scale"])
		tree.rotation_degrees = Vector3(0, float(t["yaw"]), 0)
		if t["hidden_by_limits"]:
			tree.set_meta("hidden_by_limits", true)
		_add(tree_root, tree, root)
		n += 1
	# Exemplos no limite legal (ocultos por padrao): lote valido que mais entra no circulo.
	var limits := _add(root, Node3D.new(), root) as Node3D
	limits.name = "LimitExamples"
	limits.visible = false
	limits.set_meta("note", "Lotes VALIDOS hoje (QuoteBuilding testa centro e 4 cantos) que entram no circulo do lago; so para ver a relacao com a agua visivel. Nao e proposta de regra.")
	var outline := StandardMaterial3D.new()
	outline.albedo_color = Color("fff3b0")
	outline.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	for k in c["limits"]:
		var lim: Dictionary = c["limits"][k]
		var b := _instance(LIMIT_SCENES[lim["kind"]])
		b.name = "Limit_%s" % k
		b.position = Vector3(lim["position"][0], 0, lim["position"][1])
		b.rotation = Vector3(0, float(lim["rotation_rad"]), 0)
		b.set_meta("closest_approach_m", lim["closest_approach_m"])
		_add(limits, b, root)
		var lot: Vector2 = LOTS[lim["kind"]]
		var frame := Node3D.new()
		frame.name = "LotOutline_%s" % k
		frame.position = b.position + Vector3(0, 0.03, 0)
		frame.rotation = b.rotation
		_add(limits, frame, root)
		var w := 0.22
		for side in [[Vector3(lot.x, 0.04, w), Vector3(0, 0, -lot.y / 2.0)], [Vector3(lot.x, 0.04, w), Vector3(0, 0, lot.y / 2.0)],
				[Vector3(w, 0.04, lot.y), Vector3(-lot.x / 2.0, 0, 0)], [Vector3(w, 0.04, lot.y), Vector3(lot.x / 2.0, 0, 0)]]:
			var e := _box(side[0], side[1], outline, "Edge")
			e.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
			_add(frame, e, root)
	return root
