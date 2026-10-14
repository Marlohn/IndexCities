## IndexCities - WaterStudy - validador headless (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
##   godot --headless --path <projeto> --script res://Assets/WaterStudy/Source/validate_water_study.gd -- --report=<arquivo.json> [--cityworld=<CityWorld.cs>]
##
## Confere: arquivos; shaders analisados pela Godot e sem recursos proibidos (tela, profundidade,
## textura, ALPHA, EMISSION, discard, light(), deslocamento de vertices); malha da amostra A
## identica a do CityWorld; cenas sem script, colisao, fisica, conectores ou luzes extras;
## B marcada como conceitual; aderecos dentro do circulo; determinismo; hash bem distribuido;
## linha d'agua construida em B igual a pintada em A. Grava a tabela de vertices/passadas.
extends SceneTree

const Geo := preload("res://Assets/WaterStudy/Source/ws_shore_geometry.gd")
const ROOT := "res://Assets/WaterStudy"

var _checks := 0
var _fails: Array[String] = []
var _report := {}


func _initialize() -> void:
	_run.call_deferred()


func _check(ok: bool, what: String) -> void:
	_checks += 1
	if not ok:
		_fails.append(what)
		push_error("[validate] FALHA: " + what)


func _run() -> void:
	var report_path := ""
	var cityworld := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--report="):
			report_path = a.substr(9)
		elif a.begins_with("--cityworld="):
			cityworld = a.substr(12)
	_files()
	_shaders()
	_cityworld_source(cityworld)
	_scenes()
	_geometry()
	_hash_quality()
	_report["checks"] = _checks
	_report["failures"] = _fails
	if report_path != "":
		var f := FileAccess.open(report_path, FileAccess.WRITE)
		f.store_string(JSON.stringify(_report, "  ", false))
		f.close()
	print("[validate] %d checks, %d falhas" % [_checks, _fails.size()])
	quit(0 if _fails.is_empty() else 1)


func _files() -> void:
	for p in ["Shaders/ws_common.gdshaderinc", "Shaders/ws_water_a.gdshader", "Shaders/ws_terrain_b.gdshader",
			"Shaders/ws_water_b.gdshader", "Shaders/ws_reeds.gdshader", "Materials/ws_water_a.tres",
			"Materials/ws_terrain_b.tres", "Materials/ws_water_b.tres", "Materials/ws_reeds.tres",
			"Materials/ws_rock.tres", "Materials/ws_river_cut.tres", "Meshes/ws_b_terrain.res",
			"Meshes/ws_b_water.res", "Meshes/ws_b_river_cut.res", "Meshes/ws_reed_clump.res", "Meshes/ws_rock.res",
			"Preview/ws_before.tscn", "Preview/ws_sample_a.tscn", "Preview/ws_sample_b.tscn",
			"Preview/Captures/.gdignore", "README.md"]:
		_check(FileAccess.file_exists(ROOT + "/" + p), "arquivo presente: " + p)
	for m in ["ws_water_a", "ws_terrain_b", "ws_water_b", "ws_reeds"]:
		var txt := FileAccess.get_file_as_string(ROOT + "/Materials/%s.tres" % m)
		_check(txt.find("shader_parameter/") < 0 and txt.find("/Shaders/%s.gdshader" % m) >= 0, "material %s so aponta o shader (padroes no shader)" % m)
	var readme := FileAccess.get_file_as_string(ROOT + "/README.md")
	_check(readme.find("Revisão humana: PENDENTE") >= 0, "README declara Revisão humana: PENDENTE")
	_check(readme.find("NÃO APROVADA PARA GAMEPLAY") >= 0, "README rotula a geometria de B como NÃO APROVADA PARA GAMEPLAY")
	_check(readme.find("decisão de geometria") >= 0, "README lista o que depende de decisão de geometria")
	# Nenhuma imagem importavel fora de Preview/Captures (que tem .gdignore).
	var stray := []
	_walk(ProjectSettings.globalize_path(ROOT), stray)
	_check(stray.is_empty(), "sem imagens importaveis fora de Preview/Captures: %s" % [stray])


func _walk(dir: String, stray: Array) -> void:
	var da := DirAccess.open(dir)
	if da == null:
		return
	for f in da.get_files():
		if f.get_extension().to_lower() in ["png", "jpg", "jpeg", "webp", "svg", "exr", "hdr"]:
			stray.append(dir.path_join(f))
	for d in da.get_directories():
		if d == "Captures":
			continue
		_walk(dir.path_join(d), stray)


func _body(code: String, fn: String) -> String:
	var at := code.find("void %s()" % fn)
	if at < 0:
		return ""
	var open := code.find("{", at)
	var depth := 0
	for i in range(open, code.length()):
		var ch := code[i]
		if ch == "{":
			depth += 1
		elif ch == "}":
			depth -= 1
			if depth == 0:
				return code.substr(open, i - open + 1)
	return ""


func _strip_comments(code: String) -> String:
	var out := ""
	for line in code.split("\n"):
		var c := line.find("//")
		out += (line.substr(0, c) if c >= 0 else line) + "\n"
	return out


func _shaders() -> void:
	var expected := {
		"ws_water_a": ["host_time_s", "lake_radius_m", "bank_min_m", "bank_max_m", "grass_base_color", "water_deep_color"],
		"ws_terrain_b": ["ground_y", "water_level_y", "grass_base_color", "soil_wet_color"],
		"ws_water_b": ["host_time_s", "shallow_depth_m", "flow_strength", "water_deep_color"],
		"ws_reeds": ["roughness_value"],
	}
	var include := _strip_comments(FileAccess.get_file_as_string(ROOT + "/Shaders/ws_common.gdshaderinc"))
	var forbidden := ["SCREEN_TEXTURE", "hint_screen_texture", "DEPTH_TEXTURE", "hint_depth_texture", "ALPHA",
		"EMISSION", "discard", "texture(", "sampler2D", "void light()", "global uniform", "instance uniform"]
	var info := {}
	for name in expected:
		var path: String = ROOT + "/Shaders/%s.gdshader" % name
		var sh := load(path) as Shader
		_check(sh != null, "shader carrega: " + name)
		if sh == null:
			continue
		var uniforms := []
		for u in sh.get_shader_uniform_list():
			uniforms.append(u["name"])
		for u in expected[name]:
			_check(u in uniforms, "%s: uniform %s analisado pela Godot" % [name, u])
		var code := _strip_comments(sh.code) + include
		for token in forbidden:
			_check(code.find(token) < 0, "%s sem %s" % [name, token])
		var vtx := _body(_strip_comments(sh.code), "vertex")
		for token in ["VERTEX =", "VERTEX +=", "VERTEX -=", "VERTEX *=", "POSITION", "NORMAL ="]:
			_check(vtx.find(token) < 0, "%s: vertex() nao altera %s" % [name, token.strip_edges()])
		var bare := _strip_comments(sh.code)
		if name == "ws_reeds":
			_check(bare.find("render_mode cull_disabled;") >= 0 and bare.count("render_mode") == 1, "juncos: so cull_disabled")
		else:
			_check(bare.find("render_mode") < 0, "%s sem render_mode (iluminacao padrao igual ao chao)" % name)
		info[name] = {"uniforms": uniforms.size(), "chars": sh.code.length()}
	_report["shaders"] = info


func _cityworld_source(path: String) -> void:
	if path == "":
		_report["cityworld_source"] = "nao conferido (sem --cityworld)"
		return
	var src := FileAccess.get_file_as_string(path)
	_check(src.length() > 0, "CityWorld.cs legivel")
	_check(src.find("new CylinderMesh{TopRadius=(float)state.LakeRadius,BottomRadius=(float)state.LakeRadius+1,Height=.15f,RadialSegments=64}") >= 0,
			"CityWorld ainda usa CylinderMesh R / R+1 / 0,15 / 64 (base da amostra A)")
	_check(src.find("Position=P(state.LakeCentre,-.04f)") >= 0, "CityWorld ainda posiciona o lago em y = -0,04")
	_check(src.find("Box(_static,new(half*2,1,half*2),new(0,-.56f,0)") >= 0, "CityWorld ainda usa o chao BoxMesh 512 x 1 x 512 em y = -0,56")
	_check(src.find("env_grass_soft.tres") >= 0 and src.find("env_lake_water.tres") >= 0, "CityWorld usa os materiais integrados (antes)")
	_report["cityworld_source"] = path


func _scenes() -> void:
	var table := {}
	for v in [["before", "ws_before.tscn"], ["a", "ws_sample_a.tscn"], ["b", "ws_sample_b.tscn"]]:
		var packed := load(ROOT + "/Preview/" + v[1]) as PackedScene
		_check(packed != null, "cena carrega: " + v[1])
		if packed == null:
			continue
		var scene := packed.instantiate() as Node3D
		var bad := []
		var lights := []
		var stats := {"study": {"meshes": 0, "vertices": 0, "triangles": 0, "surfaces": 0, "draw_items": 0, "materials": {}},
			"context": {"meshes": 0, "vertices": 0, "triangles": 0, "surfaces": 0, "draw_items": 0, "materials": {}}}
		_scan(scene, scene, bad, lights, stats)
		_check(bad.is_empty(), "%s: sem script/colisao/fisica/conector proprio: %s" % [v[1], bad])
		_check(lights.size() == 3, "%s: so Sol + 2 luzes de poste (%s)" % [v[1], lights])
		for k in stats:
			stats[k]["materials"] = (stats[k]["materials"] as Dictionary).keys()
		table[v[0]] = stats
		if v[0] in ["before", "a"]:
			var lake := scene.get_node("World/LakeSurface") as MeshInstance3D
			var cyl := lake.mesh as CylinderMesh
			_check(cyl != null, "%s: lago e CylinderMesh" % v[0])
			if cyl != null:
				_check(is_equal_approx(cyl.top_radius, Geo.LAKE_RADIUS) and is_equal_approx(cyl.bottom_radius, Geo.LAKE_RADIUS + 1.0)
						and is_equal_approx(cyl.height, 0.15) and cyl.radial_segments == 64 and cyl.rings == 4
						and cyl.cap_top and cyl.cap_bottom, "%s: CylinderMesh igual ao CityWorld (R, R+1, 0,15, 64, rings 4)" % v[0])
			_check(lake.position.is_equal_approx(Vector3(Geo.LAKE_CENTRE.x, -0.04, Geo.LAKE_CENTRE.y)) and lake.rotation == Vector3.ZERO
					and lake.scale == Vector3.ONE, "%s: lago na mesma posicao/rotacao/escala do CityWorld" % v[0])
			_check(lake.cast_shadow == GeometryInstance3D.SHADOW_CASTING_SETTING_ON, "%s: sombra do lago como no CityWorld" % v[0])
			var ground := scene.get_node("World/Ground") as MeshInstance3D
			_check((ground.mesh as BoxMesh).size == Vector3(512, 1, 512) and ground.position == Vector3(0, -0.56, 0), "%s: chao igual ao CityWorld" % v[0])
		if v[0] == "a":
			_check(scene.get_node("World/LakeSurface").material_override.resource_path.ends_with("ws_water_a.tres"), "A: so o material do lago muda")
			_props_inside(scene, "A")
		if v[0] == "b":
			_check(bool(scene.get_meta("conceptual_geometry_not_approved_for_gameplay", false)), "B: cena marcada como geometria conceitual nao aprovada")
			for p in ["World/GroundConceptualBasin", "World/WaterConceptual", "World/RiverMapEdgeCut"]:
				_check(bool(scene.get_node(p).get_meta("conceptual_geometry_not_approved_for_gameplay", false)), "B: %s marcado como conceitual" % p)
			_check(scene.get_node_or_null("World/LakeSurface") == null, "B: sem o disco atual")
		scene.free()
	_report["scene_table"] = table


func _props_inside(scene: Node, label: String) -> void:
	for mmi_path in ["World/DecorativeProps/Reeds", "World/DecorativeProps/Rocks"]:
		var mmi := scene.get_node(mmi_path) as MultiMeshInstance3D
		var mm := mmi.multimesh
		# Le o buffer salvo (12 floats da transformacao + 4 da cor por instancia); no headless
		# get_instance_transform() nao reflete o arquivo.
		var buf := mm.buffer
		_check(buf.size() == mm.instance_count * 16, "%s: buffer do %s com %d instancias" % [label, mmi_path.get_file(), mm.instance_count])
		var far := 0.0
		for i in mm.instance_count:
			far = maxf(far, Vector2(buf[i * 16 + 3], buf[i * 16 + 11]).distance_to(Geo.LAKE_CENTRE))
		_check(mm.instance_count > 0 and far < Geo.LAKE_RADIUS - 0.4, "%s: %s dentro do circulo de jogo (max %.2f m < R %.2f)" % [label, mmi_path.get_file(), far, Geo.LAKE_RADIUS])


func _owned_by_v1(node: Node, root: Node) -> bool:
	var n := node
	while n != null and n != root:
		if n.scene_file_path.begins_with("res://Assets/V1/"):
			return true
		n = n.get_parent()
	return false


func _scan(node: Node, root: Node, bad: Array, lights: Array, stats: Dictionary) -> void:
	if node.get_script() != null:
		bad.append("script em " + str(root.get_path_to(node)))
	if node is CollisionObject3D or node is CollisionShape3D or node is PhysicsBody3D or node is NavigationRegion3D:
		bad.append("fisica em " + str(root.get_path_to(node)))
	if node is Marker3D and not _owned_by_v1(node, root):
		bad.append("marcador proprio em " + str(root.get_path_to(node)))
	if node is Light3D:
		lights.append(str(node.name))
	var bucket: String = "context" if str(root.get_path_to(node)).begins_with("Context") or str(root.get_path_to(node)) in ["World/Skirt"] or str(root.get_path_to(node)).begins_with("World/MapBorder") else "study"
	if node is MeshInstance3D and (node as MeshInstance3D).mesh != null:
		_count(node.mesh, 1, node, stats[bucket])
	elif node is MultiMeshInstance3D and (node as MultiMeshInstance3D).multimesh != null:
		var mm := (node as MultiMeshInstance3D).multimesh
		_count(mm.mesh, mm.instance_count, node, stats[bucket])
	for c in node.get_children():
		_scan(c, root, bad, lights, stats)


func _count(mesh: Mesh, instances: int, node: GeometryInstance3D, s: Dictionary) -> void:
	s["meshes"] += 1
	s["draw_items"] += mesh.get_surface_count()
	for i in mesh.get_surface_count():
		var arr := mesh.surface_get_arrays(i)
		var nv: int = (arr[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
		var idx = arr[Mesh.ARRAY_INDEX]
		var ntri: int = (idx.size() if idx != null and idx.size() > 0 else nv) / 3
		s["vertices"] += nv * instances
		s["triangles"] += ntri * instances
		s["surfaces"] += 1
		var mat: Material = node.material_override if node.material_override != null else mesh.surface_get_material(i)
		var passes := 0
		var m := mat
		while m != null:
			passes += 1
			_check(not (m is BaseMaterial3D) or (m as BaseMaterial3D).transparency == BaseMaterial3D.TRANSPARENCY_DISABLED, "material opaco: %s" % m.resource_path)
			m = m.next_pass
		var key := (mat.resource_path.get_file() if mat != null and mat.resource_path != "" else (mat.get_class() if mat != null else "nenhum")) + " x%d passada(s)" % passes
		s["materials"][key] = true


func _geometry() -> void:
	var geo := Geo.new()
	var b1: Dictionary = geo.build_b()
	var b2: Dictionary = Geo.new().build_b()
	for k in ["terrain", "water", "cut_face"]:
		var m1 := b1[k] as ArrayMesh
		var m2 := b2[k] as ArrayMesh
		var same := m1.get_surface_count() == m2.get_surface_count()
		for i in m1.get_surface_count():
			var a1 := m1.surface_get_arrays(i)
			var a2 := m2.surface_get_arrays(i)
			same = same and (a1[Mesh.ARRAY_VERTEX] as PackedVector3Array) == (a2[Mesh.ARRAY_VERTEX] as PackedVector3Array)
			same = same and (a1[Mesh.ARRAY_INDEX] as PackedInt32Array) == (a2[Mesh.ARRAY_INDEX] as PackedInt32Array)
		_check(same, "B deterministica: %s identica em duas geracoes" % k)
	_report["b_stats"] = b1["stats"]
	# A linha d'agua que B constroi (cruzamento do relevo com o nivel d'agua ao longo de cada
	# raio) deve coincidir com a que A pinta (iteracao de ponto fixo da mesma funcao).
	var worst := 0.0
	var sum := 0.0
	var n := 0
	for k in 360:
		var ang := TAU * k / 360.0
		var dir := Vector2(cos(ang), sin(ang))
		var painted := Geo.waterline_point(dir)
		var q: Dictionary = geo.river_query(painted)
		if q["found"] and float(q["dist"]) < Geo.river_half_width(q["u"]) + Geo.river_bank_width(q["u"]) + 1.5:
			continue  # foz do rio conceitual: a margem do lago e cortada pelo canal
		var r_lo := 0.0
		var r_hi := Geo.LAKE_RADIUS
		for it in 40:
			var mid := 0.5 * (r_lo + r_hi)
			var y: float = (geo.fields(Geo.LAKE_CENTRE + dir * mid) as PackedFloat32Array)[0]
			if y < Geo.WATER_Y:
				r_lo = mid
			else:
				r_hi = mid
		var built := 0.5 * (r_lo + r_hi)
		var dist := absf(built - Geo.LAKE_CENTRE.distance_to(painted))
		worst = maxf(worst, dist)
		sum += dist
		n += 1
	_check(worst < 0.2, "linha d'agua de B = linha pintada de A (pior %.3f m, media %.3f m, %d raios)" % [worst, sum / maxi(n, 1), n])
	_report["waterline_a_vs_b"] = {"worst_m": snappedf(worst, 0.001), "mean_m": snappedf(sum / maxi(n, 1), 0.001), "rays": n}
	# Largura da margem dentro dos limites declarados e toda a agua do lago dentro do circulo.
	var wmin := INF
	var wmax := 0.0
	for k in 720:
		var ang2 := TAU * k / 720.0
		var p := Geo.waterline_point(Vector2(cos(ang2), sin(ang2)))
		var wv := Geo.LAKE_RADIUS - Geo.LAKE_CENTRE.distance_to(p)
		wmin = minf(wmin, wv)
		wmax = maxf(wmax, wv)
	_check(wmin > 0.5 and wmax < Geo.LAKE_RADIUS * 0.5, "margem entre %.2f e %.2f m, agua do lago dentro do circulo" % [wmin, wmax])
	_report["bank_width_range_m"] = [snappedf(wmin, 0.01), snappedf(wmax, 0.01)]


func _hash_quality() -> void:
	var res := {}
	for salt in [101, 401, 409, 419, 431, 439]:
		var s := 0.0
		var s2 := 0.0
		var corr := 0.0
		var n := 0
		for y in range(-64, 64):
			for x in range(-64, 64):
				var h := Geo.ws_hash(x, y, salt)
				var hn := Geo.ws_hash(x + 1, y, salt)
				s += h
				s2 += h * h
				corr += (h - 0.5) * (hn - 0.5)
				n += 1
		var mean := s / n
		var std := sqrt(s2 / n - mean * mean)
		var rho := (corr / n) / (std * std)
		res[str(salt)] = [snappedf(mean, 0.0001), snappedf(std, 0.0001), snappedf(rho, 0.0001)]
		_check(absf(mean - 0.5) < 0.01 and absf(std - 0.2887) < 0.01 and absf(rho) < 0.03, "hash sal %d: media %.4f, desvio %.4f, correlacao vizinha %.4f" % [salt, mean, std, rho])
	_report["hash_quality"] = res
