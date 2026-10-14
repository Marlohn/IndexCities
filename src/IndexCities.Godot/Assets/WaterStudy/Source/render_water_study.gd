## IndexCities - WaterStudy - capturas antes / A / B (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
## Precisa de renderizacao real (NAO usar --headless). Renderiza num SubViewport proprio de
## 1280 x 800 (janela padrao do jogo) com MSAA 4x, entao a janela do processo pode ser minima
## e sem foco (projeto de verificacao do estudo: 1 x 1 px, borderless, no_focus).
##
##   godot --path <projeto> --script res://Assets/WaterStudy/Source/render_water_study.gd -- --out=<pasta> [--perf] [--readme-captures]
##
## Mesma camera do jogo (ortogonal, size = zoom, foco + (300, 330, 300) girado pelo yaw), mesmo
## sol pela hora (Main._Process), mesmo ambiente; agua com tempo decorativo fixo (12 s) para a
## captura ser reproduzivel. Postes acesos a noite como no CityWorld (luz 17 m, energia 1,3).
extends SceneTree

const Build := preload("res://Assets/WaterStudy/Source/build_water_study.gd")
const Geo := preload("res://Assets/WaterStudy/Source/ws_shore_geometry.gd")
const PREVIEW := "res://Assets/WaterStudy/Preview"
const README_CAPTURES := "res://Assets/WaterStudy/Preview/Captures"
const VIEW := Vector2i(1280, 800)
const WATER_TIME := 12.0
const VARIANTS := ["before", "a", "b"]
const SCENES := {"before": "ws_before.tscn", "a": "ws_sample_a.tscn", "b": "ws_sample_b.tscn"}
const LABELS := {
	"before": "ANTES - main 528d30d (disco atual)",
	"a": "A - mesma malha/silhueta, so material",
	"b": "B - geometria CONCEITUAL, NAO aprovada",
}

const F250 := Vector3(152.6085, 0, 85.9826)
const F110 := Vector3(158.0, 0, 82.0)
const F45 := Vector3(163.0, 0, 66.0)
const F45_SHORE := Vector3(125.0, 0, 90.0)

## Conjunto principal: 3 zooms x dia/noite + fim de tarde + camera virada para o sol.
const SHOTS := [
	["z45_day", {"zoom": 45.0, "focus": F45, "hour": 10.0}],
	["z45_night", {"zoom": 45.0, "focus": F45, "hour": 21.0}],
	["z110_day", {"zoom": 110.0, "focus": F110, "hour": 10.0}],
	["z110_night", {"zoom": 110.0, "focus": F110, "hour": 21.0}],
	["z250_day", {"zoom": 250.0, "focus": F250, "hour": 10.0}],
	["z250_night", {"zoom": 250.0, "focus": F250, "hour": 21.0}],
	["z110_dusk", {"zoom": 110.0, "focus": F110, "hour": 17.0}],
	["z110_yaw225_day", {"zoom": 110.0, "focus": F110, "hour": 10.0, "yaw": 225.0}],
	["z45_shore_day", {"zoom": 45.0, "focus": F45_SHORE, "hour": 10.0}],
]

var _sub: SubViewport
var _scene: Node3D
var _variant := ""
var _report := {}
var _out := ""


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var perf := false
	var perf_only := false
	var readme := false
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--out="):
			_out = a.substr(6)
		elif a == "--perf":
			perf = true
		elif a == "--perf-only":
			perf_only = true
		elif a == "--readme-captures":
			readme = true
	if _out == "":
		push_error("[render] informe --out=<pasta>")
		quit(2)
		return
	if DisplayServer.get_name() == "headless":
		push_error("[render] precisa de renderizacao real; rode sem --headless")
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
	print("[render] %s / %s / %s / %s" % [_report["renderer"], _report["driver"], _report["adapter"], _report["api"]])
	if perf_only:
		await _perf()
		var pf := FileAccess.open(_out.path_join("perf_report.json"), FileAccess.WRITE)
		pf.store_string(JSON.stringify(_report, "  ", false))
		pf.close()
		print("[render] perf OK -> %s" % _out)
		quit(0)
		return
	var images := {}
	for v in VARIANTS:
		_load(v)
		for shot in SHOTS:
			images["%s_%s" % [v, shot[0]]] = await _capture("%s_%s" % [v, shot[0]], shot[1], true)
		if v == "a":
			images["a_z110_day_material_only"] = await _capture("a_z110_day_material_only", {"zoom": 110.0, "focus": F110, "hour": 10.0, "props": false}, true)
			images["a_z45_day_material_only"] = await _capture("a_z45_day_material_only", {"zoom": 45.0, "focus": F45, "hour": 10.0, "props": false}, true)
		# Movimento: mesmo quadro 0,6 s depois (tempo decorativo).
		images["%s_motion" % v] = await _capture("%s_motion" % v, {"zoom": 45.0, "focus": F45, "hour": 10.0, "water_time": WATER_TIME + 0.6}, false)
		_report["shots"]["%s_seam" % v] = _seam_metric(images["%s_z45_shore_day" % v], {"zoom": 45.0, "focus": F45_SHORE})
		_report["shots"]["%s_motion_delta" % v] = _diff(images["%s_z45_day" % v], images["%s_motion" % v], Rect2i(0, 0, VIEW.x, VIEW.y))
	# Chao longe do lago: B (espelho da grama) deve ser igual ao antes.
	_report["far_ground_b_vs_before"] = _diff(images["before_z250_day"], images["b_z250_day"], Rect2i(0, 0, 360, 220))
	_report["far_ground_a_vs_before"] = _diff(images["before_z250_day"], images["a_z250_day"], Rect2i(0, 0, 360, 220))
	# Folhas comparativas (antes | A | B) em meia resolucao com rotulos.
	var header := await _header_strip()
	for shot in SHOTS:
		var key: String = shot[0]
		_triptych([images["before_" + key], images["a_" + key], images["b_" + key]], header, _out.path_join("sheets/compare_%s.png" % key))
	_pair(images["a_z110_day_material_only"], images["a_z110_day"], _out.path_join("sheets/a_material_only_vs_props_z110.png"))
	_pair(images["a_z45_day_material_only"], images["a_z45_day"], _out.path_join("sheets/a_material_only_vs_props_z45.png"))
	_zoom_ladder(images, header)
	if readme:
		_readme_copies()
	if perf:
		await _perf()
	var f := FileAccess.open(_out.path_join("render_report.json"), FileAccess.WRITE)
	f.store_string(JSON.stringify(_report, "  ", false))
	f.close()
	print("[render] OK -> %s" % _out)
	quit(0)


func _load(v: String) -> void:
	if _scene != null:
		_sub.remove_child(_scene)
		_scene.free()
	var packed: PackedScene = load(PREVIEW + "/" + SCENES[v])
	_scene = packed.instantiate() as Node3D
	_sub.add_child(_scene)
	_variant = v


func _set_shot(shot: Dictionary) -> void:
	var cam := _scene.get_node("GameCamera") as Camera3D
	var focus: Vector3 = shot.get("focus", F110)
	cam.size = float(shot.get("zoom", 110.0))
	cam.transform = Build.camera_transform(focus, float(shot.get("yaw", 45.0)))
	var hour := float(shot.get("hour", 10.0))
	var sun := _scene.get_node("Sun") as DirectionalLight3D
	var s := Build.sun_for_hour(hour)
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	# Postes: acesos a noite (hora < 6 ou >= 18), como o CityWorld com energia atendida.
	var night := hour < 6.0 or hour >= 18.0
	for lamp in _scene.get_node("Context").get_children():
		if not lamp.name.begins_with("Lamp"):
			continue
		(lamp.get_node("LampLight") as OmniLight3D).visible = night
		var glass := lamp.get_node_or_null("LampGlass") as MeshInstance3D
		if glass != null:
			glass.material_override = load("res://Assets/V1/Materials/road_lamp_glass_%s.tres" % ("on" if night else "off"))
	# Tempo decorativo fixo da agua (o host do jogo passa host_time_s).
	var water_time := float(shot.get("water_time", WATER_TIME))
	for path in ["World/LakeSurface", "World/WaterConceptual"]:
		var mi := _scene.get_node_or_null(path) as MeshInstance3D
		if mi != null:
			var m := (mi.material_override as ShaderMaterial).duplicate() as ShaderMaterial
			m.set_shader_parameter("host_time_s", water_time)
			mi.material_override = m
	var props := _scene.get_node_or_null("World/DecorativeProps") as Node3D
	if props != null:
		props.visible = bool(shot.get("props", true))


func _capture(name: String, shot: Dictionary, save: bool) -> Image:
	_set_shot(shot)
	var rid := _sub.get_viewport_rid()
	for i in 10:
		await process_frame
	var cpu := 0.0
	var gpu := 0.0
	const N := 6
	for i in N:
		await process_frame
		await RenderingServer.frame_post_draw
		cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
		gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
	var img := _sub.get_texture().get_image()
	img.convert(Image.FORMAT_RGB8)
	if save:
		img.save_png(_out.path_join("shots/%s.png" % name))
	var info := {}
	for k in shot:
		var val = shot[k]
		info[k] = [val.x, val.y, val.z] if val is Vector3 else val
	info["cpu_ms_indicative"] = snappedf(cpu / N, 0.001)
	info["gpu_ms_indicative"] = snappedf(gpu / N, 0.001)
	_report["shots"][name] = info
	print("[render] %s  gpu %.2f ms" % [name, gpu / N])
	return img


## Visibilidade do contorno do circulo: diferenca media de cor entre pixels logo DENTRO
## (R - 0,25 m) e logo FORA (R + 0,6 m) da borda real, ao longo do arco visivel do quadro.
## Quanto menor, menos o "prato" aparece. (No antes a borda e agua x grama.)
func _seam_metric(img: Image, shot: Dictionary) -> Dictionary:
	_set_shot(shot)
	var cam := _scene.get_node("GameCamera") as Camera3D
	var c := Geo.LAKE_CENTRE
	var sum := 0.0
	var n := 0
	# Altura real da superficie logo dentro da borda: tampa do disco (antes/A) ou chao (B).
	var y_in := Geo.GROUND_Y if _variant == "b" else 0.035
	for k in 720:
		var ang := TAU * k / 720.0
		var dir := Vector2(cos(ang), sin(ang))
		var pin := c + dir * (Geo.LAKE_RADIUS - 0.25)
		var pout := c + dir * (Geo.LAKE_RADIUS + 0.6)
		var sin_ := cam.unproject_position(Vector3(pin.x, y_in, pin.y))
		var sout := cam.unproject_position(Vector3(pout.x, -0.06, pout.y))
		if not Rect2(Vector2(2, 2), Vector2(VIEW) - Vector2(4, 4)).has_point(sin_) or not Rect2(Vector2(2, 2), Vector2(VIEW) - Vector2(4, 4)).has_point(sout):
			continue
		var a := img.get_pixelv(Vector2i(sin_))
		var b := img.get_pixelv(Vector2i(sout))
		sum += (absf(a.r - b.r) + absf(a.g - b.g) + absf(a.b - b.b)) * 255.0
		n += 1
	return {"mean_rgb_sum_inside_vs_outside_edge": snappedf(sum / maxi(n, 1), 0.01), "samples": n}


func _diff(a: Image, b: Image, rect: Rect2i) -> Dictionary:
	var sum := 0.0
	var peak := 0.0
	var n := 0
	for y in range(rect.position.y, rect.end.y, 2):
		for x in range(rect.position.x, rect.end.x, 2):
			var ca := a.get_pixel(x, y)
			var cb := b.get_pixel(x, y)
			var d := (absf(ca.r - cb.r) + absf(ca.g - cb.g) + absf(ca.b - cb.b)) * 255.0
			sum += d
			peak = maxf(peak, d)
			n += 1
	return {"mean_rgb_sum": snappedf(sum / maxi(n, 1), 0.01), "max_rgb_sum": snappedf(peak, 0.01), "samples": n}


## Faixa de rotulos (renderizada pela propria Godot com a fonte padrao).
func _header_strip() -> Image:
	var w := VIEW.x / 2
	var vp := SubViewport.new()
	vp.size = Vector2i(w * 3 + 12, 34)
	vp.transparent_bg = false
	vp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	var bg := ColorRect.new()
	bg.color = Color("1f2b2c")
	bg.size = Vector2(vp.size)
	vp.add_child(bg)
	for i in VARIANTS.size():
		var lab := Label.new()
		lab.text = LABELS[VARIANTS[i]]
		lab.position = Vector2(i * (w + 6) + 10, 6)
		lab.add_theme_font_size_override("font_size", 17)
		lab.add_theme_color_override("font_color", Color("ffe7a3") if VARIANTS[i] == "b" else Color("eef3f4"))
		vp.add_child(lab)
	root.add_child(vp)
	for i in 4:
		await process_frame
	await RenderingServer.frame_post_draw
	var img := vp.get_texture().get_image()
	img.convert(Image.FORMAT_RGB8)
	root.remove_child(vp)
	vp.free()
	return img


func _half(img: Image) -> Image:
	var im := img.duplicate() as Image
	im.resize(VIEW.x / 2, VIEW.y / 2, Image.INTERPOLATE_LANCZOS)
	return im


func _triptych(imgs: Array, header: Image, path: String) -> void:
	var w := VIEW.x / 2
	var h := VIEW.y / 2
	var out := Image.create_empty(w * 3 + 12, h + header.get_height(), false, Image.FORMAT_RGB8)
	out.fill(Color("1f2b2c"))
	out.blit_rect(header, Rect2i(Vector2i.ZERO, header.get_size()), Vector2i.ZERO)
	for i in imgs.size():
		out.blit_rect(_half(imgs[i]), Rect2i(0, 0, w, h), Vector2i(i * (w + 6), header.get_height()))
	out.save_png(path)


func _pair(a: Image, b: Image, path: String) -> void:
	var w := VIEW.x / 2
	var h := VIEW.y / 2
	var out := Image.create_empty(w * 2 + 6, h, false, Image.FORMAT_RGB8)
	out.fill(Color("1f2b2c"))
	out.blit_rect(_half(a), Rect2i(0, 0, w, h), Vector2i.ZERO)
	out.blit_rect(_half(b), Rect2i(0, 0, w, h), Vector2i(w + 6, 0))
	out.save_png(path)


## Uma folha por variante: zoom 45 / 110 / 250 de dia (linha 1) e de noite (linha 2).
func _zoom_ladder(images: Dictionary, header: Image) -> void:
	var w := VIEW.x / 2
	var h := VIEW.y / 2
	for v in VARIANTS:
		var out := Image.create_empty(w * 3 + 12, h * 2 + 6, false, Image.FORMAT_RGB8)
		out.fill(Color("1f2b2c"))
		var keys := [["z45_day", "z110_day", "z250_day"], ["z45_night", "z110_night", "z250_night"]]
		for r in 2:
			for c in 3:
				out.blit_rect(_half(images["%s_%s" % [v, keys[r][c]]]), Rect2i(0, 0, w, h), Vector2i(c * (w + 6), r * (h + 6)))
		out.save_png(_out.path_join("sheets/ladder_%s.png" % v))


## Copias leves (JPEG) para o README da pasta (Preview/Captures tem .gdignore). As folhas
## completas em PNG ficam na pasta de saida (artifacts/, ignorada pelo Git).
func _readme_copies() -> void:
	var dst := ProjectSettings.globalize_path(README_CAPTURES)
	DirAccess.make_dir_recursive_absolute(dst)
	for key in ["compare_z45_day", "compare_z110_day", "compare_z250_day", "compare_z110_night", "a_material_only_vs_props_z45"]:
		var img := Image.load_from_file(_out.path_join("sheets/%s.png" % key))
		img.save_jpg(dst.path_join("%s.jpg" % key), 0.88)


## Custo de GPU/CPU do quadro inteiro (1280 x 800, MSAA 4x). Aquecimento longo para o clock da
## GPU estabilizar; 3 rodadas com a ordem das variantes girando; media, minimo e maximo por
## rodada. Medicao local indicativa (uma GPU), nao benchmark.
func _perf() -> void:
	const FRAMES := 150
	const ROUNDS := 3
	var views := [["z110_day", {"zoom": 110.0, "focus": F110, "hour": 10.0}],
		["z45_lake", {"zoom": 45.0, "focus": F250, "hour": 10.0}],
		["z35_lake_fullscreen", {"zoom": 35.0, "focus": F250, "hour": 10.0}]]
	var result := {}
	var rid := _sub.get_viewport_rid()
	_load("before")
	_set_shot(views[0][1])
	for i in 300:
		await process_frame
	for view in views:
		var runs := {}
		for v in VARIANTS:
			runs[v] = {"gpu": [], "cpu": []}
		for round_i in ROUNDS:
			for j in VARIANTS.size():
				var v: String = VARIANTS[(j + round_i) % VARIANTS.size()]
				_load(v)
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
		for v in VARIANTS:
			var gs: Array = runs[v]["gpu"]
			var cs: Array = runs[v]["cpu"]
			row[v] = {"gpu_ms_mean": snappedf((gs[0] + gs[1] + gs[2]) / 3.0, 0.001), "gpu_ms_min": gs.min(), "gpu_ms_max": gs.max(),
				"cpu_ms_mean": snappedf((cs[0] + cs[1] + cs[2]) / 3.0, 0.001), "gpu_ms_rounds": gs}
		result[view[0]] = row
		print("[render] perf %s: %s" % [view[0], row])
	_report["perf_1280x800_msaa4x"] = result
