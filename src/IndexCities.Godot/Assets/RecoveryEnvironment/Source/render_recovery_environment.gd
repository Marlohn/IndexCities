## Capturas da amostra RecoveryEnvironment (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
## Precisa de renderizacao real: NAO usar --headless (abre uma janela da Godot).
## Usa o renderizador configurado no projeto (GL Compatibility no IndexCities).
##
##   # Conjunto final (antes x depois no mesmo angulo, close, aberto, giro, fim de tarde, noite):
##   godot --path src/IndexCities.Godot --script res://Assets/RecoveryEnvironment/Source/render_recovery_environment.gd -- --final
##   # Variantes de calibragem (nao versionadas):
##   godot --path src/IndexCities.Godot --script res://Assets/RecoveryEnvironment/Source/render_recovery_environment.gd -- --calib --out=C:/tmp/calib
##
## A cena e a de Preview/ (camera, luz e ambiente iguais aos do jogo). "Antes" troca so os
## materiais do chao e do lago pelos materiais planos atuais do CityWorld (#7d9b52 e #6bb3bd,
## roughness 0,85). A agua usa `host_time_s` fixo para a captura ser reproduzivel.
##
## --final grava em Preview/Captures (com .gdignore): antes/depois em 1440 x 900, comparacoes
## lado a lado, close da margem, folha 2 x 2 de variacoes, tira de movimento da agua e
## render_report.json (renderizador, GPU, tempos por quadro, delta de movimento, custo antes/depois).
extends SceneTree

const Build := preload("res://Assets/RecoveryEnvironment/Source/build_recovery_environment.gd")
const ROOT := "res://Assets/RecoveryEnvironment"
const PREVIEW_SCENE := ROOT + "/Preview/recovery_environment_preview.tscn"
const CAPTURE_DIR := ROOT + "/Preview/Captures"
## Mesmo tamanho de viewport do projeto (display/window/size 1440 x 900) e MSAA 4x do Main.
const VIEW := Vector2i(1440, 900)
const WATER_TIME := 12.0
## Foco junto a margem norte do lago (perto das casas) para os closes.
const SHORE_FOCUS := Vector3(150, 0, 60)
const GAME_COLORS := {"ground": "7d9b52", "lake": "6bb3bd"}

var _sub: SubViewport
var _scene: Node3D
var _ground: MeshInstance3D
var _lake: MeshInstance3D
var _grass: ShaderMaterial
var _water: ShaderMaterial
var _report := {}


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var final_mode := false
	var calib := false
	var out_dir := CAPTURE_DIR
	for a in OS.get_cmdline_user_args():
		if a == "--final":
			final_mode = true
		elif a == "--calib":
			calib = true
		elif a.begins_with("--out="):
			out_dir = a.substr(6)
	if not final_mode and not calib:
		push_error("[render] use --final ou --calib")
		quit(2)
		return
	if DisplayServer.get_name() == "headless":
		push_error("[render] precisa de janela/renderizacao real; rode sem --headless")
		quit(2)
		return
	var abs_out := ProjectSettings.globalize_path(out_dir) if out_dir.begins_with("res://") else out_dir
	DirAccess.make_dir_recursive_absolute(abs_out)
	if not _setup():
		quit(1)
		return
	_report["renderer"] = RenderingServer.get_current_rendering_method()
	_report["driver"] = RenderingServer.get_current_rendering_driver_name()
	_report["adapter"] = "%s / %s" % [RenderingServer.get_video_adapter_vendor(), RenderingServer.get_video_adapter_name()]
	_report["api"] = RenderingServer.get_video_adapter_api_version()
	_report["viewport"] = [VIEW.x, VIEW.y]
	_report["msaa_3d"] = "4x"
	_report["shots"] = {}
	print("[render] %s / %s / %s" % [_report["renderer"], _report["driver"], _report["adapter"]])
	if final_mode:
		await _final(abs_out)
	if calib:
		await _calibration(abs_out)
	var f := FileAccess.open(abs_out.path_join("render_report.json"), FileAccess.WRITE)
	f.store_string(JSON.stringify(_report, "  ", false))
	f.close()
	print("[render] OK -> %s" % abs_out)
	quit(0)


func _setup() -> bool:
	_sub = SubViewport.new()
	_sub.size = VIEW
	_sub.own_world_3d = true
	_sub.msaa_3d = Viewport.MSAA_4X
	_sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(_sub)
	var packed: PackedScene = load(PREVIEW_SCENE)
	if packed == null:
		push_error("[render] cena de previa ausente: %s" % PREVIEW_SCENE)
		return false
	_scene = packed.instantiate() as Node3D
	_sub.add_child(_scene)
	_ground = _scene.get_node("World/Ground") as MeshInstance3D
	_lake = _scene.get_node("World/Lake") as MeshInstance3D
	_grass = (_ground.material_override as ShaderMaterial).duplicate() as ShaderMaterial
	_water = (_lake.material_override as ShaderMaterial).duplicate() as ShaderMaterial
	RenderingServer.viewport_set_measure_render_time(_sub.get_viewport_rid(), true)
	return true


## Configura um quadro: camera do jogo, hora, materiais (antes/depois/variantes).
func _frame(shot: Dictionary) -> void:
	var cam := _scene.get_node("GameCamera") as Camera3D
	var focus: Vector3 = shot.get("focus", Build.PREVIEW_FOCUS)
	cam.size = float(shot.get("zoom", Build.PREVIEW_ZOOM))
	cam.transform = Build.camera_transform(focus, float(shot.get("yaw", Build.PREVIEW_YAW)))
	var sun := _scene.get_node("Sun") as DirectionalLight3D
	var s := Build.sun_for_hour(float(shot.get("hour", Build.PREVIEW_HOUR)))
	sun.rotation_degrees = s["rotation_degrees"]
	sun.light_energy = s["energy"]
	if shot.get("before", false):
		_ground.material_override = Build.flat_material(GAME_COLORS["ground"])
		_lake.material_override = Build.flat_material(GAME_COLORS["lake"])
	else:
		var g := _grass.duplicate() as ShaderMaterial
		for k in shot.get("grass", {}):
			g.set_shader_parameter(k, shot["grass"][k])
		var w := _water.duplicate() as ShaderMaterial
		w.set_shader_parameter("host_time_s", float(shot.get("water_time", WATER_TIME)))
		for k in shot.get("water", {}):
			w.set_shader_parameter(k, shot["water"][k])
		_ground.material_override = g
		_lake.material_override = w


func _capture(name: String, shot: Dictionary, out_dir: String) -> Image:
	_frame(shot)
	var rid := _sub.get_viewport_rid()
	# Quadros de aquecimento: compilacao de shaders e mapa de sombras.
	for i in 8:
		await process_frame
	var cpu := 0.0
	var gpu := 0.0
	const N := 12
	for i in N:
		await process_frame
		await RenderingServer.frame_post_draw
		cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
		gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
	var img := _sub.get_texture().get_image()
	img.convert(Image.FORMAT_RGB8)
	# out_dir vazio: so mede e devolve a imagem (usada em folhas compostas), sem gravar.
	if out_dir != "":
		var path := out_dir.path_join(name + ".png")
		if img.save_png(path) != OK:
			push_error("[render] falha ao salvar %s" % path)
	var info := shot.duplicate(true)
	for k in info.keys():
		if info[k] is Vector3:
			info[k] = [info[k].x, info[k].y, info[k].z]
	info["cpu_ms"] = snappedf(cpu / N, 0.001)
	info["gpu_ms"] = snappedf(gpu / N, 0.001)
	_report["shots"][name] = info
	print("[render] %s  cpu %.2f ms  gpu %.2f ms" % [name, cpu / N, gpu / N])
	return img


func _side_by_side(a: Image, b: Image, path: String) -> void:
	var half := Vector2i(VIEW.x / 2, VIEW.y / 2)
	var ia := a.duplicate() as Image
	var ib := b.duplicate() as Image
	ia.resize(half.x, half.y, Image.INTERPOLATE_LANCZOS)
	ib.resize(half.x, half.y, Image.INTERPOLATE_LANCZOS)
	var gap := 6
	var out := Image.create_empty(half.x * 2 + gap, half.y, false, Image.FORMAT_RGB8)
	out.fill(Color("24393a"))
	out.blit_rect(ia, Rect2i(Vector2i.ZERO, half), Vector2i.ZERO)
	out.blit_rect(ib, Rect2i(Vector2i.ZERO, half), Vector2i(half.x + gap, 0))
	out.save_png(path)


## Folha 2 x 2 em meia resolucao (ordem: esquerda-cima, direita-cima, esquerda-baixo, direita-baixo).
func _sheet(images: Array, path: String) -> void:
	var half := Vector2i(VIEW.x / 2, VIEW.y / 2)
	var gap := 6
	var out := Image.create_empty(half.x * 2 + gap, half.y * 2 + gap, false, Image.FORMAT_RGB8)
	out.fill(Color("24393a"))
	for i in images.size():
		var im := (images[i] as Image).duplicate() as Image
		im.resize(half.x, half.y, Image.INTERPOLATE_LANCZOS)
		out.blit_rect(im, Rect2i(Vector2i.ZERO, half), Vector2i((i % 2) * (half.x + gap), (i / 2) * (half.y + gap)))
	out.save_png(path)


## Mesmo recorte central de varios instantes, lado a lado.
func _strip(images: Array, path: String) -> void:
	var crop := Rect2i(490, 300, 460, 300)
	var gap := 6
	var out := Image.create_empty(crop.size.x * images.size() + gap * (images.size() - 1), crop.size.y, false, Image.FORMAT_RGB8)
	out.fill(Color("24393a"))
	for i in images.size():
		out.blit_rect(images[i], crop, Vector2i(i * (crop.size.x + gap), 0))
	out.save_png(path)


## Diferenca media/maxima por pixel (soma RGB, 0-765) dentro do recorte central.
func _motion_delta(a: Image, b: Image) -> Dictionary:
	var sum := 0.0
	var peak := 0.0
	var n := 0
	for y in range(300, 600, 3):
		for x in range(490, 950, 3):
			var ca := a.get_pixel(x, y)
			var cb := b.get_pixel(x, y)
			var d := (absf(ca.r - cb.r) + absf(ca.g - cb.g) + absf(ca.b - cb.b)) * 255.0
			sum += d
			peak = maxf(peak, d)
			n += 1
	return {"mean_rgb_sum": snappedf(sum / n, 0.01), "max_rgb_sum": snappedf(peak, 0.01), "samples": n}


## Custo de GPU/CPU do quadro inteiro, alternando antes/depois para reduzir o vies do clock.
func _perf() -> void:
	const FRAMES := 90
	var result := {}
	var centre := Vector3(Build.LAKE_CENTRE.x, 0, Build.LAKE_CENTRE.y)
	var rid := _sub.get_viewport_rid()
	for view in [["z45_lake", {"zoom": 45.0, "focus": centre}], ["z110", {}]]:
		var acc := {"before": [0.0, 0.0], "after": [0.0, 0.0]}
		for pass_index in 2:
			for mode in ["before", "after"]:
				var shot: Dictionary = (view[1] as Dictionary).duplicate()
				shot["before"] = mode == "before"
				_frame(shot)
				for i in 10:
					await process_frame
				for i in FRAMES:
					await process_frame
					await RenderingServer.frame_post_draw
					acc[mode][0] += RenderingServer.viewport_get_measured_render_time_gpu(rid) / (FRAMES * 2)
					acc[mode][1] += RenderingServer.viewport_get_measured_render_time_cpu(rid) / (FRAMES * 2)
		result[view[0]] = {
			"before_gpu_ms": snappedf(acc["before"][0], 0.001), "after_gpu_ms": snappedf(acc["after"][0], 0.001),
			"before_cpu_ms": snappedf(acc["before"][1], 0.001), "after_cpu_ms": snappedf(acc["after"][1], 0.001),
		}
		print("[render] perf %s: %s" % [view[0], result[view[0]]])
	_report["perf_1440x900_msaa4x"] = result


func _final(out_dir: String) -> void:
	if not FileAccess.file_exists(out_dir.path_join(".gdignore")):
		FileAccess.open(out_dir.path_join(".gdignore"), FileAccess.WRITE).close()
	var tmp := ""  # intermediarias das folhas compostas: medidas, nao gravadas
	var before := await _capture("env_before_z110_h10", {"before": true}, out_dir)
	var after := await _capture("env_after_z110_h10", {}, out_dir)
	_side_by_side(before, after, out_dir.path_join("env_compare_z110_h10.png"))
	var before_wide := await _capture("env_before_z250_h10", {"before": true, "zoom": 250.0}, tmp)
	var after_wide := await _capture("env_after_z250_h10", {"zoom": 250.0}, tmp)
	_side_by_side(before_wide, after_wide, out_dir.path_join("env_compare_z250_h10.png"))
	await _capture("env_after_z45_shore_h10", {"zoom": 45.0, "focus": SHORE_FOCUS}, out_dir)
	# Folha de variacoes: giro de camera, fim de tarde, noite e a alternativa facetada (nao padrao).
	var sheet := []
	sheet.append(await _capture("env_after_z110_yaw135_h10", {"yaw": 135.0}, tmp))
	sheet.append(await _capture("env_after_z110_h17", {"hour": 17.0}, tmp))
	sheet.append(await _capture("env_after_z110_h21_night", {"hour": 21.0}, tmp))
	sheet.append(await _capture("env_alt_facet2_z110_h10", {"water": {"facet_size_m": 2.0}}, tmp))
	_sheet(sheet, out_dir.path_join("env_variants_sheet.png"))
	# Movimento da ondulacao: tres instantes (0,0 / 0,4 / 0,8 s) no mesmo close do lago.
	var centre := Vector3(Build.LAKE_CENTRE.x, 0, Build.LAKE_CENTRE.y)
	var frames := []
	for i in 3:
		frames.append(await _capture("env_after_z45_lake_t%d" % i, {"zoom": 45.0, "focus": centre, "water_time": WATER_TIME + 0.4 * i}, tmp))
	_strip(frames, out_dir.path_join("env_motion_strip.png"))
	_report["motion_0_4s"] = _motion_delta(frames[0], frames[1])
	_report["motion_0_8s"] = _motion_delta(frames[0], frames[2])
	await _perf()


func _calibration(out_dir: String) -> void:
	var centre := Vector3(Build.LAKE_CENTRE.x, 0, Build.LAKE_CENTRE.y)
	await _capture("calib_before_z110", {"before": true}, out_dir)
	await _capture("calib_default_z110", {}, out_dir)
	await _capture("calib_default_z45", {"zoom": 45.0, "focus": SHORE_FOCUS}, out_dir)
	await _capture("calib_default_z45_centre", {"zoom": 45.0, "focus": centre}, out_dir)
	await _capture("calib_default_z250", {"zoom": 250.0}, out_dir)
	await _capture("calib_default_z500", {"zoom": 500.0}, out_dir)
	await _capture("calib_default_yaw135", {"yaw": 135.0}, out_dir)
	await _capture("calib_default_h17", {"hour": 17.0}, out_dir)
	await _capture("calib_default_h21", {"hour": 21.0}, out_dir)
	await _capture("calib_grass_var100", {"grass": {"variation": 1.0}}, out_dir)
	await _capture("calib_water_ripple14_z45_centre", {"zoom": 45.0, "focus": centre, "water": {"ripple_strength": 1.4}}, out_dir)
	await _capture("calib_water_facet2_z45_centre", {"zoom": 45.0, "focus": centre, "water": {"facet_size_m": 2.0}}, out_dir)
	await _capture("calib_water_facet2_z110", {"water": {"facet_size_m": 2.0}}, out_dir)
	for i in 3:
		await _capture("calib_motion_t%d" % i, {"zoom": 45.0, "focus": centre, "water_time": WATER_TIME + 0.4 * i}, out_dir)
