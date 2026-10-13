## Previews FINAIS versionados da amostra V1 (chamado por render_v1_previews.gd --final).
##
## Gera capturas reais da Godot: prancha do conjunto, close, vista dos fundos, lineup
## frontal com grade, miniaturas transparentes, folha de contato com rotulos, paleta SVG
## e uma medicao local de draw calls/primitivas. Iluminacao, ceu, chao e grade sao do
## preview; nao fazem parte dos assets. Nao executa gameplay.
extends RefCounted

const ROOT := "res://Assets/V1"
const CAPTURES := "res://Assets/V1/Preview/Captures"
const THUMBS := "res://Assets/V1/Thumbnails"
const BOARD := "res://Assets/V1/Preview/v1_sample_board.tscn"

## Familias com miniatura (visual_id, cena, rotulo curto PT, rotacao Y em graus para o lineup).
const FAMILIES := [
	["civic.city_hall.a", "res://Assets/V1/Scenes/civic.city_hall.a.tscn", "Prefeitura", 0.0],
	["residential.small_house.a", "res://Assets/V1/Scenes/residential.small_house.a.tscn", "Casa simples", 0.0],
	["commercial.grocery_small.a", "res://Assets/V1/Scenes/commercial.grocery_small.a.tscn", "Mercearia pequena", 0.0],
	["road.urban_1x1.a.straight", "res://Assets/V1/Scenes/road.urban_1x1.a.straight.tscn", "Rua 1+1 reta", 0.0],
	["road.urban_1x1.a.corner_l", "res://Assets/V1/Scenes/road.urban_1x1.a.corner_l.tscn", "Rua 1+1 esquina L", 0.0],
	["road.urban_1x1.a.lamp", "res://Assets/V1/Scenes/road.urban_1x1.a.lamp.tscn", "Poste", 0.0],
	["vegetation.tree_broadleaf.a", "res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn", "Arvore", 0.0],
	["agent.sim_pedestrian.a", "res://Assets/V1/Scenes/agent.sim_pedestrian.a.tscn", "SIM pedestre", 0.0],
	["vehicle.truck_cargo.a", "res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn", "Caminhao de carga", -90.0],
]

## Segunda onda: thumbnails/lineup/folha propria. Nos State_* sao ligados SO no render
## (ilustracao de catalogo "em operacao"); nas cenas eles continuam ocultos por padrao.
const FAMILIES_W2 := [
	["civic.works_depot.a", "res://Assets/V1/Scenes/civic.works_depot.a.tscn", "Patio de Obras", 0.0],
	["agriculture.farm_small.a", "res://Assets/V1/Scenes/agriculture.farm_small.a.tscn", "Fazenda pequena", 0.0],
	["commercial.fuel_station.a", "res://Assets/V1/Scenes/commercial.fuel_station.a.tscn", "Posto", 0.0],
	["utility.water_intake.a", "res://Assets/V1/Scenes/utility.water_intake.a.tscn", "Captacao de agua", 0.0],
	["utility.water_treatment.a", "res://Assets/V1/Scenes/utility.water_treatment.a.tscn", "Tratamento de agua", 0.0],
	["utility.sewage_treatment.a", "res://Assets/V1/Scenes/utility.sewage_treatment.a.tscn", "Tratamento de esgoto", 0.0],
	["utility.wind_power.a", "res://Assets/V1/Scenes/utility.wind_power.a.tscn", "Energia eolica", 0.0],
]
const BOARD_W2 := "res://Assets/V1/Preview/v1_wave2_board.tscn"

const DIR_ISO_FR := Vector3(1, 1, -1)
const DIR_ISO_BL := Vector3(-1, 1, 1)
const DIR_FRONT := Vector3(0, 0, -1)

var report := {}


func run(host, wave2_only: bool = false) -> bool:
	var ok := true
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CAPTURES))
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(THUMBS))
	_write_gdignore()
	# Preserva medicoes/listas anteriores quando so parte das capturas e refeita.
	var prev_path := CAPTURES + "/render_report.json"
	if FileAccess.file_exists(prev_path):
		var prev = JSON.parse_string(FileAccess.get_file_as_string(prev_path))
		if prev is Dictionary:
			report = prev
	if not wave2_only:
		ok = (await _board(host)) and ok
		ok = (await _lineup(host, FAMILIES, "v1_lineup_front.png", "lineup_front")) and ok
		ok = (await _thumbnails(host, FAMILIES)) and ok
		ok = (await _stress(host)) and ok
	ok = (await _board_wave2(host)) and ok
	ok = (await _lineup(host, FAMILIES_W2, "v1_wave2_lineup_front.png", "lineup_front_wave2")) and ok
	ok = (await _thumbnails(host, FAMILIES_W2)) and ok
	_palette_svg()
	if not wave2_only:
		ok = (await _contact_sheet(host, {
			"title": "IndexCities - amostra visual V1 para compatibilizacao (CANDIDATA - revisao humana PENDENTE)",
			"board": "v1_board_iso.png", "families": FAMILIES, "lineup": "v1_lineup_front.png",
			"lineup_label": "Lineup em elevacao frontal (grade 1 m, linhas fortes a cada 5 m; figura de referencia 1,75 m):",
			"out": "v1_contact_sheet.png"})) and ok
	ok = (await _contact_sheet(host, {
		"title": "IndexCities - V1 segunda onda: funcoes restantes (CANDIDATA - revisao humana PENDENTE)",
		"board": "v1_wave2_board_iso.png", "families": FAMILIES_W2, "lineup": "v1_wave2_lineup_front.png",
		"lineup_label": "Lineup em elevacao frontal (grade 1 m; figura 1,75 m). Elementos State_* ligados so para ilustrar:",
		"out": "v1_wave2_contact_sheet.png"})) and ok
	var f := FileAccess.open(CAPTURES + "/render_report.json", FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(report, "  ", false))
		f.close()
	print("[final] relatorio: ", CAPTURES, "/render_report.json")
	return ok


func _write_gdignore() -> void:
	# Capturas sao documentacao: o editor Godot nao precisa importa-las.
	var f := FileAccess.open(CAPTURES + "/.gdignore", FileAccess.WRITE)
	if f:
		f.store_string("")
		f.close()


func _save(img: Image, path: String) -> bool:
	var e := img.save_png(path)
	if e != OK:
		printerr("[final] falha ao salvar ", path)
		return false
	print("[final] ", path, " (", img.get_width(), "x", img.get_height(), ")")
	return true


static func _merge_boxes(host, root: Node3D, names: Array) -> AABB:
	var box := AABB()
	var has := false
	for n in names:
		var node := root.get_node_or_null(n) as Node3D
		if node == null:
			continue
		var b: AABB = node.transform * host._content_aabb(node)
		box = b if not has else box.merge(b)
		has = true
	return box


func _load_board() -> Node3D:
	var ps: PackedScene = load(BOARD)
	if ps == null:
		return null
	var board: Node3D = ps.instantiate()
	# O host do preview adiciona seu proprio ambiente/sol/camera.
	for n in ["PreviewEnvironment", "PreviewSun", "PreviewCameraIso"]:
		var c := board.get_node_or_null(n)
		if c:
			board.remove_child(c)
			c.free()
	return board


## Liga nos State_* (estoque, cultivo, agua em processo) SO para a captura.
static func _show_states(n: Node) -> void:
	for c in n.get_children():
		if String(c.name).begins_with("State_") and c is Node3D:
			(c as Node3D).visible = true
		_show_states(c)


func _board_wave2(host) -> bool:
	var ps: PackedScene = load(BOARD_W2)
	if ps == null:
		printerr("[final] prancha da onda 2 ausente: ", BOARD_W2)
		return false
	var board: Node3D = ps.instantiate()
	for n in ["PreviewEnvironment", "PreviewSun"]:
		var c := board.get_node_or_null(n)
		if c:
			board.remove_child(c)
			c.free()
	_show_states(board)
	var box := _merge_boxes(host, board, ["Road", "Facilities", "LotOutlines"])
	var ok := true
	report["measure_wave2_board"] = await _measure(host, board, host._camera_for(box, DIR_ISO_FR, 16.0 / 9.0, 1.03), Vector2i(1920, 1080))
	var img: Image = await host._render(board, host._camera_for(box, DIR_ISO_FR, 16.0 / 9.0, 1.03), Vector2i(2560, 1440), [], false)
	ok = _save(img, CAPTURES + "/v1_wave2_board_iso.png") and ok
	img = await host._render(board, host._camera_for(box, DIR_ISO_BL, 16.0 / 9.0, 1.03), Vector2i(2560, 1440), [], false)
	ok = _save(img, CAPTURES + "/v1_wave2_board_iso_back.png") and ok
	board.free()
	return ok


func _board(host) -> bool:
	var board := _load_board()
	if board == null:
		printerr("[final] prancha ausente: ", BOARD)
		return false
	var ok := true
	var all_box := _merge_boxes(host, board, ["Road", "Buildings", "Lamps", "ScaleProps"])
	var img: Image = await host._render(board, host._camera_for(all_box, DIR_ISO_FR, 16.0 / 9.0, 1.03), Vector2i(2560, 1440), [], false)
	ok = _save(img, CAPTURES + "/v1_board_iso.png") and ok
	img = await host._render(board, host._camera_for(all_box, DIR_ISO_BL, 16.0 / 9.0, 1.03), Vector2i(2560, 1440), [], false)
	ok = _save(img, CAPTURES + "/v1_board_iso_back.png") and ok
	var close_box := _merge_boxes(host, board, ["Buildings/SmallHouse", "Buildings/GrocerySmall", "ScaleProps", "Buildings/Tree03"])
	close_box = close_box.merge(AABB(Vector3(close_box.position.x, 0, -5.25), Vector3(close_box.size.x, 0.2, 1)))
	img = await host._render(board, host._camera_for(close_box, DIR_ISO_FR, 16.0 / 9.0, 1.08), Vector2i(1920, 1080), [], false)
	ok = _save(img, CAPTURES + "/v1_board_closeup.png") and ok
	board.free()
	return ok


func _lineup(host, families: Array, out_name: String, report_key: String) -> bool:
	var holder := Node3D.new()
	var x := 0.0
	var gap := 2.5
	var placed := []
	for fam in families:
		if String(fam[0]).begins_with("road.urban_1x1.a.s") or String(fam[0]).begins_with("road.urban_1x1.a.c"):
			continue  # pecas planas nao leem em elevacao
		var ps: PackedScene = load(fam[1])
		if ps == null:
			continue
		var inst: Node3D = ps.instantiate()
		inst.rotation_degrees.y = fam[3]
		_show_states(inst)
		var tmp := Node3D.new()
		tmp.add_child(inst)
		var box: AABB = host._content_aabb(tmp)
		tmp.remove_child(inst)
		tmp.free()
		inst.position = Vector3(x - box.position.x, 0, -box.get_center().z)
		holder.add_child(inst)
		placed.append({"id": fam[0], "x_from": snappedf(x, 0.01), "width": snappedf(box.size.x, 0.01), "height": snappedf(box.size.y, 0.01)})
		x += box.size.x + gap
	var fig: Node3D = host._scale_figure()
	fig.position = Vector3(x + 0.5, 0, 0)
	holder.add_child(fig)
	var box_all: AABB = host._content_aabb(holder)
	var extras: Array = [host._make_ground(400.0), host._make_backwall(box_all.end.z + 2.0, 400.0, 60.0)]
	var img: Image = await host._render(holder, host._camera_for(box_all.grow(0.5), DIR_FRONT, 3.2, 1.04), Vector2i(2880, 900), extras, false)
	report[report_key] = placed
	holder.free()
	return _save(img, CAPTURES + "/" + out_name)


func _thumbnails(host, families: Array) -> bool:
	var ok := true
	var thumbs: Array = report.get("thumbnails", [])
	for fam in families:
		var tp := "%s/%s.png" % [THUMBS, fam[0]]
		if thumbs.has(tp):
			thumbs.erase(tp)
		var ps: PackedScene = load(fam[1])
		if ps == null:
			printerr("[final] cena ausente para miniatura: ", fam[1])
			ok = false
			continue
		var inst: Node3D = ps.instantiate()
		_show_states(inst)
		var holder := Node3D.new()
		holder.add_child(inst)
		var box: AABB = host._content_aabb(holder)
		var img: Image = await host._render(holder, host._camera_for(box, DIR_ISO_FR, 1.0, 1.06), Vector2i(512, 512), [], true)
		holder.free()
		var path := "%s/%s.png" % [THUMBS, fam[0]]
		ok = _save(img, path) and ok
		thumbs.append(path)
	report["thumbnails"] = thumbs
	return ok


## Medicao LOCAL e sintetica (este notebook, RTX 3050, Forward+). Nao e benchmark do jogo.
func _measure(host, content: Node3D, cam: Camera3D, size: Vector2i) -> Dictionary:
	var sub := SubViewport.new()
	sub.size = size
	sub.own_world_3d = true
	sub.msaa_3d = Viewport.MSAA_4X
	sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	host.get_root().add_child(sub)
	sub.add_child(host._make_env(false))
	sub.add_child(host._make_sun())
	sub.add_child(content)
	sub.add_child(cam)
	cam.current = true
	var rid := sub.get_viewport_rid()
	RenderingServer.viewport_set_measure_render_time(rid, true)
	for i in 10:
		await host.process_frame
	var cpu := 0.0
	var gpu := 0.0
	var frames := 30
	for i in frames:
		await RenderingServer.frame_post_draw
		cpu += RenderingServer.viewport_get_measured_render_time_cpu(rid)
		gpu += RenderingServer.viewport_get_measured_render_time_gpu(rid)
	var V := RenderingServer.VIEWPORT_RENDER_INFO_TYPE_VISIBLE
	var S := RenderingServer.VIEWPORT_RENDER_INFO_TYPE_SHADOW
	var res := {
		"viewport": [size.x, size.y],
		"visible_objects": RenderingServer.viewport_get_render_info(rid, V, RenderingServer.VIEWPORT_RENDER_INFO_OBJECTS_IN_FRAME),
		"visible_primitives": RenderingServer.viewport_get_render_info(rid, V, RenderingServer.VIEWPORT_RENDER_INFO_PRIMITIVES_IN_FRAME),
		"visible_draw_calls": RenderingServer.viewport_get_render_info(rid, V, RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME),
		"shadow_draw_calls": RenderingServer.viewport_get_render_info(rid, S, RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME),
		"avg_viewport_cpu_ms": snappedf(cpu / frames, 0.001),
		"avg_viewport_gpu_ms": snappedf(gpu / frames, 0.001),
	}
	sub.remove_child(content)
	sub.queue_free()
	await host.process_frame
	return res


func _stress(host) -> bool:
	var board := _load_board()
	if board == null:
		return false
	var box := _merge_boxes(host, board, ["Road", "Buildings", "Lamps", "ScaleProps"])
	report["measure_board"] = await _measure(host, board, host._camera_for(box, DIR_ISO_FR, 16.0 / 9.0, 1.03), Vector2i(1920, 1080))
	board.free()
	# Grade sintetica: 20 x 20 casas + 400 arvores, instancias independentes (sem MultiMesh).
	var grid := Node3D.new()
	var house: PackedScene = load("res://Assets/V1/Scenes/residential.small_house.a.tscn")
	var tree: PackedScene = load("res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn")
	if house == null or tree == null:
		return false
	for i in 20:
		for j in 20:
			var h: Node3D = house.instantiate()
			h.position = Vector3(i * 14.0, 0, j * 14.0)
			grid.add_child(h)
			var t: Node3D = tree.instantiate()
			t.position = Vector3(i * 14.0 + 6.5, 0, j * 14.0 + 5.0)
			grid.add_child(t)
	var gbox: AABB = host._content_aabb(grid)
	report["measure_grid_400_houses_400_trees"] = await _measure(host, grid, host._camera_for(gbox, DIR_ISO_FR, 16.0 / 9.0, 1.02), Vector2i(1920, 1080))
	grid.free()
	print("[final] medicao: ", JSON.stringify({"board": report["measure_board"], "grid": report["measure_grid_400_houses_400_trees"]}))
	return true


static func _hex(c: Color) -> String:
	return "#" + c.to_html(false)


func _materials() -> Array:
	var out := []
	var dir := DirAccess.open(ROOT + "/Materials")
	if dir == null:
		return out
	var files := Array(dir.get_files())
	files.sort()
	for f in files:
		if not String(f).ends_with(".tres"):
			continue
		var m = load(ROOT + "/Materials/" + f)
		if m is StandardMaterial3D:
			out.append({"name": String(f).get_basename(), "albedo": _hex(m.albedo_color), "emissive": m.emission_enabled})
	return out


func _palette_svg() -> void:
	var mats := _materials()
	report["materials"] = mats
	var cols := 6
	var cw := 230
	var ch := 74
	var rows := int(ceil(mats.size() / float(cols)))
	var w := cols * cw + 40
	var h := rows * ch + 90
	var s := PackedStringArray()
	s.append('<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">' % [w, h, w, h])
	s.append('<rect width="100%" height="100%" fill="#f4f2ed"/>')
	s.append('<text x="20" y="34" font-family="sans-serif" font-size="20" fill="#2b2f33">IndexCities V1 - paleta CANDIDATA da amostra (revisao humana PENDENTE)</text>')
	s.append('<text x="20" y="58" font-family="sans-serif" font-size="13" fill="#5b6066">Gerado de Assets/V1/Materials/*.tres. Sem prefixo = compartilhado; prefixo = familia. (on) = alternativo emissivo NAO aplicado por padrao.</text>')
	for i in mats.size():
		var m: Dictionary = mats[i]
		var x := 20 + (i % cols) * cw
		var y := 76 + (i / cols) * ch
		s.append('<rect x="%d" y="%d" width="56" height="56" rx="6" fill="%s" stroke="#00000033"/>' % [x, y, m["albedo"]])
		s.append('<text x="%d" y="%d" font-family="monospace" font-size="13" fill="#2b2f33">%s%s</text>' % [x + 66, y + 24, m["name"], " (on)" if m["emissive"] else ""])
		s.append('<text x="%d" y="%d" font-family="monospace" font-size="12" fill="#5b6066">%s</text>' % [x + 66, y + 44, m["albedo"]])
	s.append('</svg>')
	var f := FileAccess.open(CAPTURES + "/v1_palette.svg", FileAccess.WRITE)
	if f:
		f.store_string("\n".join(s))
		f.close()
		print("[final] ", CAPTURES, "/v1_palette.svg")


func _tex(path: String) -> ImageTexture:
	var img := Image.load_from_file(ProjectSettings.globalize_path(path))
	if img == null:
		return null
	return ImageTexture.create_from_image(img)


func _label(parent: Control, text: String, pos: Vector2, size: int, color: Color, width: float = 0.0) -> Label:
	var l := Label.new()
	l.text = text
	l.position = pos
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	if width > 0.0:
		l.size = Vector2(width, 0)
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(l)
	return l


func _contact_sheet(host, cfg: Dictionary) -> bool:
	var W := 2400
	var H := 4000
	var sub := SubViewport.new()
	sub.size = Vector2i(W, H)
	sub.transparent_bg = false
	sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	host.get_root().add_child(sub)
	var bg := ColorRect.new()
	bg.color = Color("#f4f2ed")
	bg.size = Vector2(W, H)
	sub.add_child(bg)
	var ink := Color("#2b2f33")
	var mute := Color("#5b6066")
	_label(bg, String(cfg["title"]), Vector2(30, 18), 34, ink)
	_label(bg, "Capturas reais Godot 4.7.2 (Forward+). Camera isometrica ortografica de preview; luz/ceu/chao/grade so do preview. Composicao arbitraria de vitrine: nao e partida, nao define lotes, recuos, acessos ou trafego. Medidas candidatas, nao aprovadas.", Vector2(30, 66), 20, mute, W - 60)
	var y := 130.0
	var board := _tex(CAPTURES + "/" + String(cfg["board"]))
	if board:
		var tr := TextureRect.new()
		tr.texture = board
		tr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		tr.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		tr.position = Vector2(30, y)
		tr.size = Vector2(W - 60, (W - 60) * 9.0 / 16.0)
		bg.add_child(tr)
		y += tr.size.y + 20
	_label(bg, "Miniaturas (isometrico verdadeiro; fachada candidata -Z e a face clara a direita, lado +X a esquerda):", Vector2(30, y), 24, ink)
	y += 40
	var tw := 250.0
	var i := 0
	for fam in cfg["families"]:
		var t := _tex("%s/%s.png" % [THUMBS, fam[0]])
		var x := 30.0 + (i % 9) * (tw + 13)
		var card := ColorRect.new()
		card.color = Color("#e6e2d8")
		card.position = Vector2(x, y)
		card.size = Vector2(tw, tw + 64)
		bg.add_child(card)
		if t:
			var tr2 := TextureRect.new()
			tr2.texture = t
			tr2.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			tr2.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			tr2.position = Vector2(x + 5, y + 5)
			tr2.size = Vector2(tw - 10, tw - 10)
			bg.add_child(tr2)
		_label(bg, String(fam[2]), Vector2(x + 8, y + tw - 2), 20, ink)
		_label(bg, String(fam[0]), Vector2(x + 8, y + tw + 26), 13, mute)
		i += 1
	y += tw + 90
	var lineup := _tex(CAPTURES + "/" + String(cfg["lineup"]))
	if lineup:
		_label(bg, String(cfg["lineup_label"]), Vector2(30, y), 24, ink)
		y += 40
		var tr3 := TextureRect.new()
		tr3.texture = lineup
		tr3.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		tr3.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		tr3.position = Vector2(30, y)
		tr3.size = Vector2(W - 60, (W - 60) * 900.0 / 2880.0)
		bg.add_child(tr3)
		y += tr3.size.y + 20
	_label(bg, "Paleta candidata (de Materials/*.tres):", Vector2(30, y), 24, ink)
	y += 40
	var mats: Array = report.get("materials", [])
	for k in mats.size():
		var m: Dictionary = mats[k]
		var cx := 30.0 + (k % 8) * 295
		var cy := y + (k / 8) * 64
		var sw := ColorRect.new()
		sw.color = Color(m["albedo"])
		sw.position = Vector2(cx, cy)
		sw.size = Vector2(52, 52)
		bg.add_child(sw)
		_label(bg, "%s%s" % [m["name"], " (on)" if m["emissive"] else ""], Vector2(cx + 60, cy + 2), 16, ink)
		_label(bg, String(m["albedo"]), Vector2(cx + 60, cy + 26), 14, mute)
	y += int(ceil(mats.size() / 8.0)) * 64 + 20
	sub.size = Vector2i(W, int(min(H, y + 20)))
	bg.size = Vector2(W, sub.size.y)
	for k in 4:
		await host.process_frame
	await RenderingServer.frame_post_draw
	var img := sub.get_texture().get_image()
	sub.queue_free()
	return _save(img, CAPTURES + "/" + String(cfg["out"]))
