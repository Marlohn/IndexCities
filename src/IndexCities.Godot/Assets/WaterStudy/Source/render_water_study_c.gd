## IndexCities - WaterStudy - AMOSTRA C: capturas e folhas comparativas (ferramenta de autoria;
## NAO e jogo). Revisao humana: PENDENTE.
##
## Precisa de renderizacao real (NAO usar --headless). SubViewport proprio de 1280 x 800 (janela
## padrao do jogo), MSAA 4x; a janela do processo pode ser minima e sem foco.
##
##   godot --path <projeto> --rendering-driver opengl3 --script res://Assets/WaterStudy/Source/render_water_study_c.gd -- \
##         --out=<pasta> [--generated=res://_generated] [--seeds=42,1412,2877] [--readme-captures] [--perf]
##
## Seed 42: referencia (Preview/C/ws_c_before_s42.tscn), A, B (cenas historicas) e C
## (Preview/C/ws_sample_c_s42.tscn). Outras seeds: antes e C gerados pelo build_water_study_c.gd em
## <generated> (fora do Git). Mesma camera do jogo (ortogonal, size = zoom, foco + (300, 330, 300)
## girado pelo yaw), sol pela hora (Main._Process), postes acesos a noite; agua com tempo decorativo
## fixo (12 s) para a captura ser reproduzivel. Os focos de A/B sao reexpressos em relacao ao lago.
extends SceneTree

const Build := preload("res://Assets/WaterStudy/Source/build_water_study.gd")
const ROOT := "res://Assets/WaterStudy"
const README_CAPTURES := ROOT + "/Preview/Captures"
const VIEW := Vector2i(1280, 800)
const WATER_TIME := 12.0
## Focos das tomadas de A/B na seed 42, como deslocamento a partir do centro do lago.
const A42_CENTRE := Vector2(152.608513, 85.982556)
const F110 := Vector2(158.0, 82.0)
const F45 := Vector2(163.0, 66.0)
const LABELS := {
	"before": "REFERENCIA - materiais integrados do jogo",
	"a": "A (historica) - so material, NAO pronta",
	"b": "B (historica) - conceitual, NAO pronta",
	"c": "C - agua ate o circulo real (proposta)",
	"c_flush": "C com agua no nivel do chao (0 m)",
	"c_drop40": "C com agua 0,40 m abaixo do chao",
	"c_drop15": "C com agua 0,15 m abaixo do chao (padrao)",
	"before_limits": "REFERENCIA - materiais integrados do jogo",
}
const SHOTS := ["z110_day", "z110_night", "z45_day", "z45_night"]

var _sub: SubViewport
var _scene: Node3D
var _out := ""
var _report := {}
var _label_cache := {}


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var generated := "res://_generated"
	var seeds := [42, 1412, 2877]
	var readme := false
	var perf := false
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--out="):
			_out = a.substr(6)
		elif a.begins_with("--generated="):
			generated = a.substr(12)
		elif a.begins_with("--seeds="):
			seeds = []
			for s in a.substr(8).split(","):
				seeds.append(int(s))
		elif a == "--readme-captures":
			readme = true
		elif a == "--perf":
			perf = true
	if _out == "" or DisplayServer.get_name() == "headless":
		push_error("[render-c] informe --out=<pasta> e rode sem --headless")
		quit(2)
		return
	for d in ["shots", "sheets"]:
		DirAccess.make_dir_recursive_absolute(_out.path_join(d))
	_sub = SubViewport.new()
	_sub.size = VIEW
	_sub.own_world_3d = true
	_sub.msaa_3d = Viewport.MSAA_4X
	_sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(_sub)
	RenderingServer.viewport_set_measure_render_time(_sub.get_viewport_rid(), true)
	_report["renderer"] = RenderingServer.get_current_rendering_method()
	_report["driver"] = RenderingServer.get_current_rendering_driver_name()
	_report["adapter"] = "%s / %s" % [RenderingServer.get_video_adapter_vendor(), RenderingServer.get_video_adapter_name()]
	_report["api"] = RenderingServer.get_video_adapter_api_version()
	_report["godot"] = Engine.get_version_info()["string"]
	_report["viewport"] = [VIEW.x, VIEW.y]
	_report["msaa_3d"] = "4x"
	_report["water_time_s"] = WATER_TIME
	_report["shots"] = {}
	print("[render-c] %s / %s / %s / %s" % [_report["renderer"], _report["driver"], _report["adapter"], _report["api"]])
	var images := {}
	var meta := {}
	for seed in seeds:
		var scenes := {}
		if seed == 42:
			scenes = {"before": ROOT + "/Preview/C/ws_c_before_s42.tscn", "a": ROOT + "/Preview/ws_sample_a.tscn",
				"b": ROOT + "/Preview/ws_sample_b.tscn", "c": ROOT + "/Preview/C/ws_sample_c_s42.tscn"}
			for variant in ["flush", "drop40"]:
				var vpath := generated + "/Preview/C/ws_sample_c_s42_%s.tscn" % variant
				if ResourceLoader.exists(vpath):
					scenes["c_" + variant] = vpath
			# "Antes" com os exemplos de limite (a cena historica de A/B nao os tem): so para a tomada de limite.
			var before_limits := generated + "/Preview/C/ws_c_before_s42.tscn"
			if ResourceLoader.exists(before_limits):
				scenes["before_limits"] = before_limits
		else:
			scenes = {"before": generated + "/Preview/C/ws_c_before_s%d.tscn" % seed, "c": generated + "/Preview/C/ws_sample_c_s%d.tscn" % seed}
		for v in scenes:
			if not ResourceLoader.exists(scenes[v]):
				push_error("[render-c] cena ausente: %s" % scenes[v])
				quit(1)
				return
			_load(scenes[v])
			var centre := Vector2(A42_CENTRE)
			if _scene.has_meta("lake_centre_xz"):
				centre = _scene.get_meta("lake_centre_xz")
			meta[seed] = {"centre": centre}
			var f110 := centre + (F110 - A42_CENTRE)
			var f45 := centre + (F45 - A42_CENTRE)
			var shot_list := []
			if v in ["c_flush", "c_drop40"]:
				shot_list = [["z45_day", {"zoom": 45.0, "focus": f45, "hour": 10.0}]]
			elif v == "before_limits":
				shot_list = []
			else:
				shot_list = [["z110_day", {"zoom": 110.0, "focus": f110, "hour": 10.0}], ["z110_night", {"zoom": 110.0, "focus": f110, "hour": 21.0}],
					["z45_day", {"zoom": 45.0, "focus": f45, "hour": 10.0}], ["z45_night", {"zoom": 45.0, "focus": f45, "hour": 21.0}]]
			if v in ["before", "before_limits", "c"] and _scene.get_node_or_null("LimitExamples") != null:
				for ex in _limit_foci(centre):
					shot_list.append(["z45_limit_%s_day" % ex[0], {"zoom": 45.0, "focus": ex[1], "hour": 10.0, "limits": true}])
			if v == "c" and seed == 42:
				shot_list.append(["z110_dusk", {"zoom": 110.0, "focus": f110, "hour": 17.0}])
				shot_list.append(["z110_yaw225_day", {"zoom": 110.0, "focus": f110, "hour": 10.0, "yaw": 225.0}])
				shot_list.append(["z250_day", {"zoom": 250.0, "focus": centre, "hour": 10.0}])
				shot_list.append(["z45_motion_a", {"zoom": 45.0, "focus": f45, "hour": 10.0, "water_time": WATER_TIME + 0.6}])
			for shot in shot_list:
				var key := "s%d_%s_%s" % [seed, v, shot[0]]
				images[key] = await _capture(key, shot[1])
	# ------------------------------------------------------------------ folhas
	for shot in SHOTS:
		if images.has("s42_before_" + shot):
			await _grid([["before", images["s42_before_" + shot]], ["a", images["s42_a_" + shot]], ["b", images["s42_b_" + shot]], ["c", images["s42_c_" + shot]]],
				2, _out.path_join("sheets/s42_compare4_%s.png" % shot), 1.0)
	for seed in seeds:
		for shot in SHOTS + ["z45_limit_farm_day", "z45_limit_house_day"]:
			var kb := "s%d_before_%s" % [seed, shot]
			if not images.has(kb):
				kb = "s%d_before_limits_%s" % [seed, shot]
			var kc := "s%d_c_%s" % [seed, shot]
			if images.has(kb) and images.has(kc):
				await _grid([["before", images[kb]], ["c", images[kc]]], 2, _out.path_join("sheets/s%d_pair_%s.png" % [seed, shot]), 1.0)
	for shot in SHOTS:
		var row := []
		for seed in seeds:
			var k := "s%d_c_%s" % [seed, shot]
			if images.has(k):
				row.append(["seed %d (R %.1f m)" % [seed, _radius_of(seed)], images[k]])
		if row.size() > 1:
			await _grid(row, row.size(), _out.path_join("sheets/seeds_c_%s.png" % shot), 0.5)
	if images.has("s42_c_flush_z45_day") and images.has("s42_c_drop40_z45_day"):
		await _grid([["c_flush", images["s42_c_flush_z45_day"]], ["c_drop15", images["s42_c_z45_day"]], ["c_drop40", images["s42_c_drop40_z45_day"]]],
			3, _out.path_join("sheets/s42_drop_compare_z45_day.png"), 1.0)
	if readme:
		_readme_copies()
	if perf:
		await _perf()
	var f := FileAccess.open(_out.path_join("render_report.json"), FileAccess.WRITE)
	f.store_string(JSON.stringify(_report, "  ", false))
	f.close()
	print("[render-c] OK -> %s" % _out)
	quit(0)


var _radii := {}


func _radius_of(seed: int) -> float:
	return float(_radii.get(seed, 0.0))


func _load(path: String) -> void:
	if _scene != null:
		_sub.remove_child(_scene)
		_scene.free()
	_scene = (load(path) as PackedScene).instantiate() as Node3D
	_sub.add_child(_scene)
	if _scene.has_meta("seed") and _scene.has_meta("lake_radius_m"):
		_radii[int(_scene.get_meta("seed"))] = float(_scene.get_meta("lake_radius_m"))


## Um enquadramento por exemplo de limite: entre o lote e o ponto mais proximo da margem.
func _limit_foci(centre: Vector2) -> Array:
	var out := []
	var radius := float(_scene.get_meta("lake_radius_m", 30.0))
	for child in _scene.get_node("LimitExamples").get_children():
		if not child.name.begins_with("Limit_"):
			continue
		var p := Vector2((child as Node3D).position.x, (child as Node3D).position.z)
		var edge := centre + (p - centre).normalized() * radius
		var kind := "farm" if String(child.name).find("fazenda") >= 0 else "house"
		out.append([kind, p.lerp(edge, 0.5)])
	return out


func _set_shot(shot: Dictionary) -> void:
	var cam := _scene.get_node("GameCamera") as Camera3D
	var focus: Vector2 = shot["focus"]
	cam.size = float(shot.get("zoom", 110.0))
	cam.transform = Build.camera_transform(Vector3(focus.x, 0, focus.y), float(shot.get("yaw", 45.0)))
	var hour := float(shot.get("hour", 10.0))
	var sun := _scene.get_node("Sun") as DirectionalLight3D
	var s := Build.sun_for_hour(hour)
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	var night := hour < 6.0 or hour >= 18.0
	for lamp in _scene.get_node("Context").get_children():
		if not lamp.name.begins_with("Lamp"):
			continue
		(lamp.get_node("LampLight") as OmniLight3D).visible = night
		var glass := lamp.get_node_or_null("LampGlass") as MeshInstance3D
		if glass != null:
			glass.material_override = load("res://Assets/V1/Materials/road_lamp_glass_%s.tres" % ("on" if night else "off"))
	var water_time := float(shot.get("water_time", WATER_TIME))
	for path in ["World/LakeSurface", "World/WaterConceptual", "World/LakeWater"]:
		var mi := _scene.get_node_or_null(path) as MeshInstance3D
		if mi != null:
			var m := (mi.material_override as ShaderMaterial).duplicate() as ShaderMaterial
			m.set_shader_parameter("host_time_s", water_time)
			mi.material_override = m
	var show_limits := bool(shot.get("limits", false))
	var limits := _scene.get_node_or_null("LimitExamples") as Node3D
	if limits != null:
		limits.visible = show_limits
	var props := _scene.get_node_or_null("World/DecorativeProps") as Node3D
	if props != null:
		props.visible = true
	for tree in _scene.get_node("Context/Trees").get_children():
		if tree.has_meta("hidden_by_limits"):
			(tree as Node3D).visible = not show_limits


func _capture(name: String, shot: Dictionary) -> Image:
	_set_shot(shot)
	var rid := _sub.get_viewport_rid()
	for i in 10:
		await process_frame
	var gpu := 0.0
	const N := 6
	for i in N:
		await process_frame
		await RenderingServer.frame_post_draw
		gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
	var img := _sub.get_texture().get_image()
	img.convert(Image.FORMAT_RGB8)
	img.save_png(_out.path_join("shots/%s.png" % name))
	var info := {}
	for k in shot:
		var val = shot[k]
		info[k] = [val.x, val.y] if val is Vector2 else val
	info["gpu_ms_indicative"] = snappedf(gpu / N, 0.001)
	_report["shots"][name] = info
	print("[render-c] %s  gpu %.2f ms" % [name, gpu / N])
	return img


## Rotulo renderizado pela propria Godot (fonte padrao).
func _label(text: String, width: int) -> Image:
	var key := "%s|%d" % [text, width]
	if _label_cache.has(key):
		return _label_cache[key]
	var vp := SubViewport.new()
	vp.size = Vector2i(width, 30)
	vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	var bg := ColorRect.new()
	bg.color = Color("1f2b2c")
	bg.size = Vector2(vp.size)
	vp.add_child(bg)
	var lab := Label.new()
	lab.text = text
	lab.position = Vector2(10, 4)
	lab.add_theme_font_size_override("font_size", 16)
	lab.add_theme_color_override("font_color", Color("ffe7a3") if text.begins_with("C") else Color("eef3f4"))
	vp.add_child(lab)
	root.add_child(vp)
	for i in 4:
		await process_frame
	await RenderingServer.frame_post_draw
	var img := vp.get_texture().get_image()
	img.convert(Image.FORMAT_RGB8)
	root.remove_child(vp)
	vp.free()
	_label_cache[key] = img
	return img


## Grade com rotulo sobre cada painel. `scale` 1.0 = meia resolucao por painel (640 x 400).
func _grid(panels: Array, cols: int, path: String, scale: float) -> void:
	var w := int(VIEW.x / 2 * scale)
	var h := int(VIEW.y / 2 * scale)
	var lh := 30
	var rows := int(ceil(panels.size() / float(cols)))
	var out := Image.create_empty(w * cols + 6 * (cols - 1), (h + lh) * rows + 6 * (rows - 1), false, Image.FORMAT_RGB8)
	out.fill(Color("1f2b2c"))
	for i in panels.size():
		var x := (i % cols) * (w + 6)
		var y := (i / cols) * (h + lh + 6)
		var text: String = LABELS.get(panels[i][0], panels[i][0])
		var lab: Image = await _label(text, w)
		out.blit_rect(lab, Rect2i(0, 0, w, lh), Vector2i(x, y))
		var im := (panels[i][1] as Image).duplicate() as Image
		im.resize(w, h, Image.INTERPOLATE_LANCZOS)
		out.blit_rect(im, Rect2i(0, 0, w, h), Vector2i(x, y + lh))
	out.save_png(path)


## Copias leves (JPEG) para o README (Preview/Captures tem .gdignore). Nomes com prefixo c_;
## nenhuma captura historica de A/B e sobrescrita.
func _readme_copies() -> void:
	var dst := ProjectSettings.globalize_path(README_CAPTURES)
	for pair in [["s42_compare4_z110_day", "c_compare4_z110_day"], ["s42_compare4_z110_night", "c_compare4_z110_night"],
			["s42_compare4_z45_day", "c_compare4_z45_day"], ["s42_compare4_z45_night", "c_compare4_z45_night"],
			["seeds_c_z110_day", "c_seeds_z110_day"], ["seeds_c_z110_night", "c_seeds_z110_night"],
			["s42_pair_z45_limit_farm_day", "c_limit_farm_s42_z45_day"], ["s42_pair_z45_limit_house_day", "c_limit_house_s42_z45_day"], ["s42_drop_compare_z45_day", "c_drop_compare_z45_day"]]:
		var src := _out.path_join("sheets/%s.png" % pair[0])
		if not FileAccess.file_exists(src):
			continue
		var target := dst.path_join("%s.jpg" % pair[1])
		var img := Image.load_from_file(src)
		img.save_jpg(target, 0.82)


## Custo de GPU/CPU do quadro inteiro na seed 42 (antes, A, B, C). Mesmo metodo de A/B: aquecimento,
## 3 rodadas girando a ordem, 150 quadros por medida. Medicao local indicativa (uma GPU).
func _perf() -> void:
	const FRAMES := 150
	const ROUNDS := 3
	var variants := {"before": ROOT + "/Preview/C/ws_c_before_s42.tscn", "a": ROOT + "/Preview/ws_sample_a.tscn",
		"b": ROOT + "/Preview/ws_sample_b.tscn", "c": ROOT + "/Preview/C/ws_sample_c_s42.tscn"}
	var names := variants.keys()
	var c42 := A42_CENTRE
	var views := [["z110_day", {"zoom": 110.0, "focus": c42 + (F110 - A42_CENTRE), "hour": 10.0}],
		["z45_lake", {"zoom": 45.0, "focus": c42, "hour": 10.0}],
		["z35_lake_fullscreen", {"zoom": 35.0, "focus": c42, "hour": 10.0}],
		["z110_night", {"zoom": 110.0, "focus": c42 + (F110 - A42_CENTRE), "hour": 21.0}]]
	var rid := _sub.get_viewport_rid()
	_load(variants["before"])
	_set_shot(views[0][1])
	for i in 300:
		await process_frame
	var result := {}
	for view in views:
		var runs := {}
		for v in names:
			runs[v] = {"gpu": [], "cpu": []}
		for round_i in ROUNDS:
			for j in names.size():
				var v: String = names[(j + round_i) % names.size()]
				_load(variants[v])
				_set_shot(view[1])
				for i in 25:
					await process_frame
				var g := 0.0
				var c := 0.0
				for i in FRAMES:
					await process_frame
					await RenderingServer.frame_post_draw
					g += RenderingServer.viewport_get_measured_render_time_gpu(rid) / FRAMES
					c += RenderingServer.viewport_get_measured_render_time_cpu(rid) / FRAMES
				runs[v]["gpu"].append(snappedf(g, 0.001))
				runs[v]["cpu"].append(snappedf(c, 0.001))
		var row := {}
		for v in names:
			var gs: Array = runs[v]["gpu"]
			var cs: Array = runs[v]["cpu"]
			row[v] = {"gpu_ms_mean": snappedf((gs[0] + gs[1] + gs[2]) / 3.0, 0.001), "gpu_ms_min": gs.min(), "gpu_ms_max": gs.max(),
				"cpu_ms_mean": snappedf((cs[0] + cs[1] + cs[2]) / 3.0, 0.001), "gpu_ms_rounds": gs}
		result[view[0]] = row
		print("[render-c] perf %s: %s" % [view[0], row])
	_report["perf_1280x800_msaa4x"] = result
