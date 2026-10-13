## Renderizador de previews da amostra V1 (ferramenta de autoria; NAO e jogo).
##
## Requer renderizacao real (NAO usar --headless). Exemplos:
##
##   # Prancha de revisao de uma ou mais cenas (4 vistas por cena, com grade de 1 m,
##   # figura de escala de 1,75 m e marcadores candidatos destacados):
##   godot --path <raiz> --script res://Assets/V1/Source/render_v1_previews.gd -- \
##       --review=res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn --out=C:/tmp/review
##
##   # Previews finais versionados (prancha do conjunto, lineup, miniaturas):
##   godot --path <raiz> --script res://Assets/V1/Source/render_v1_previews.gd -- --final
##   (ou -- --final-wave2 para refazer so as capturas/miniaturas da segunda onda)
##
## Iluminacao, chao e grade sao EXCLUSIVOS do preview; nao fazem parte dos assets.
extends SceneTree

const VIEW_W := 800
const VIEW_H := 600
const PREVIEW_DIR := "res://Assets/V1/Preview"
const THUMB_DIR := "res://Assets/V1/Thumbnails"
const MANIFEST_PATH := "res://Assets/V1/manifest.v1.json"
const BOARD_SCENE := "res://Assets/V1/Preview/v1_sample_board.tscn"

## Direcao (do alvo para a camera). Fachada candidata em -Z.
const DIR_ISO_FR := Vector3(1, 1, -1)    # isometrico verdadeiro: frente (-Z) + lado +X
const DIR_ISO_BL := Vector3(-1, 1, 1)    # fundos (+Z) + lado -X
const DIR_FRONT := Vector3(0, 0, -1)     # elevacao frontal
const DIR_TOP := Vector3(0, 1, 0)        # planta (fachada embaixo da imagem)

const GRID_SHADER := """
shader_type spatial;
uniform vec3 base_color : source_color = vec3(0.56, 0.66, 0.47);
uniform bool vertical = false;
varying vec3 wpos;
void vertex() { wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
float gridline(vec2 p, float w) {
	vec2 g = abs(fract(p - 0.5) - 0.5) / max(fwidth(p), vec2(1e-4));
	return 1.0 - clamp(min(g.x, g.y) / w, 0.0, 1.0);
}
void fragment() {
	vec2 p = vertical ? wpos.xy : wpos.xz;
	float l1 = gridline(p, 1.0);
	float l5 = gridline(p / 5.0, 1.6);
	vec3 c = base_color;
	c = mix(c, c * 0.80, l1 * 0.55);
	c = mix(c, c * 0.58, l5);
	if (!vertical) {
		float ax = 1.0 - clamp(abs(wpos.z) / max(fwidth(wpos.z), 1e-4) / 1.5, 0.0, 1.0);
		float az = 1.0 - clamp(abs(wpos.x) / max(fwidth(wpos.x), 1e-4) / 1.5, 0.0, 1.0);
		c = mix(c, vec3(0.80, 0.25, 0.22), ax * 0.85);
		c = mix(c, vec3(0.20, 0.35, 0.80), az * 0.85);
	}
	ALBEDO = c;
	ROUGHNESS = 1.0;
}
"""

var _grid_shader: Shader


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var review: PackedStringArray = PackedStringArray()
	var out_dir := ""
	var final_mode := false
	var wave2_only := false
	var thumb_scene := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--review="):
			review = a.substr(9).split(",", false)
		elif a.begins_with("--out="):
			out_dir = a.substr(6)
		elif a.begins_with("--thumb="):
			thumb_scene = a.substr(8)
		elif a == "--final":
			final_mode = true
		elif a == "--final-wave2":
			final_mode = true
			wave2_only = true
	_grid_shader = Shader.new()
	_grid_shader.code = GRID_SHADER
	var ok := true
	if not review.is_empty():
		if out_dir == "":
			printerr("[render] --out=<pasta> e obrigatorio com --review")
			quit(2)
			return
		DirAccess.make_dir_recursive_absolute(out_dir)
		for p in review:
			ok = (await _render_review(p, out_dir)) and ok
	if final_mode:
		ok = (await _render_final(wave2_only)) and ok
	if thumb_scene != "":
		# Somente a miniatura de UMA cena (nao refaz capturas nem miniaturas das demais).
		var impl = load("res://Assets/V1/Source/kit/final_previews.gd").new()
		var vid: String = thumb_scene.get_file().get_basename()
		ok = (await impl._thumbnails(self, [[vid, thumb_scene, "", 0.0]])) and ok
	if review.is_empty() and not final_mode and thumb_scene == "":
		printerr("[render] nada a fazer: use --review=... --out=... ou --final")
		ok = false
	quit(0 if ok else 1)


# ---------------------------------------------------------------------------
# Infra de render
# ---------------------------------------------------------------------------

func _make_env(transparent: bool) -> WorldEnvironment:
	var env := Environment.new()
	env.background_mode = Environment.BG_CLEAR_COLOR if transparent else Environment.BG_COLOR
	env.background_color = Color("#cfdee8")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#c9d5df")
	env.ambient_light_energy = 0.55
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 1.0
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 1.6
	var we := WorldEnvironment.new()
	we.environment = env
	return we


func _make_sun() -> DirectionalLight3D:
	var sun := DirectionalLight3D.new()
	var travel := Vector3(0.45, -1.0, 0.8).normalized()
	sun.transform = Transform3D(Basis.looking_at(travel, Vector3.UP), Vector3.ZERO)
	sun.light_color = Color("#fff3e2")
	sun.light_energy = 1.15
	sun.shadow_enabled = true
	sun.shadow_blur = 1.0
	sun.directional_shadow_max_distance = 600.0
	return sun


func _make_ground(extent: float) -> MeshInstance3D:
	var pm := PlaneMesh.new()
	pm.size = Vector2(extent, extent)
	var sm := ShaderMaterial.new()
	sm.shader = _grid_shader
	pm.material = sm
	var mi := MeshInstance3D.new()
	mi.mesh = pm
	mi.position = Vector3(0, -0.005, 0)
	return mi


func _make_backwall(z: float, w: float, h: float) -> MeshInstance3D:
	var qm := QuadMesh.new()
	qm.size = Vector2(w, h)
	var sm := ShaderMaterial.new()
	sm.shader = _grid_shader
	sm.set_shader_parameter("vertical", true)
	sm.set_shader_parameter("base_color", Vector3(0.80, 0.83, 0.86))
	qm.material = sm
	var mi := MeshInstance3D.new()
	mi.mesh = qm
	# QuadMesh olha para +Z; girar para a face ficar voltada a -Z (para a camera frontal).
	mi.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(0, h * 0.5, z))
	return mi


func _scale_figure() -> Node3D:
	# Figura neutra de 1,75 m SOMENTE para comparacao de escala no preview.
	var n := Node3D.new()
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color("#5b5f66")
	var body := CapsuleMesh.new()
	body.radius = 0.22
	body.height = 1.45
	body.material = mat
	var bmi := MeshInstance3D.new()
	bmi.mesh = body
	bmi.position = Vector3(0, 0.725, 0)
	n.add_child(bmi)
	var head := SphereMesh.new()
	head.radius = 0.15
	head.height = 0.3
	head.material = mat
	var hmi := MeshInstance3D.new()
	hmi.mesh = head
	hmi.position = Vector3(0, 1.6, 0)
	n.add_child(hmi)
	return n


func _marker_gizmos(content: Node3D) -> Node3D:
	var g := Node3D.new()
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color("#ff2bd6")
	mat.no_depth_test = true
	mat.render_priority = 10
	for m in content.find_children("*", "Marker3D", true, false):
		var mk := m as Marker3D
		var xf := _rel(mk, content)
		var s := SphereMesh.new()
		s.radius = 0.16
		s.height = 0.32
		s.material = mat
		var smi := MeshInstance3D.new()
		smi.mesh = s
		smi.position = xf.origin
		g.add_child(smi)
		var cone := CylinderMesh.new()
		cone.top_radius = 0.0
		cone.bottom_radius = 0.14
		cone.height = 0.6
		cone.material = mat
		var cmi := MeshInstance3D.new()
		cmi.mesh = cone
		var fwd := (-xf.basis.z).normalized()
		var up := Vector3.UP if abs(fwd.dot(Vector3.UP)) < 0.99 else Vector3.BACK
		# Cilindro aponta em +Y; alinhar +Y com a frente do marcador.
		var bx := up.cross(fwd).normalized()
		var bz := bx.cross(fwd).normalized()
		cmi.transform = Transform3D(Basis(bx, fwd, bz), xf.origin + fwd * 0.45)
		g.add_child(cmi)
	return g


## Contorno amarelo do lote logico do host (centrado no pivo). So preview.
func _lot_outline(lot: Vector2) -> Node3D:
	var g := Node3D.new()
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color("#ffd23f")
	var t := 0.2
	for b in [[Vector3(0, 0, -lot.y * 0.5), Vector2(lot.x, t)], [Vector3(0, 0, lot.y * 0.5), Vector2(lot.x, t)],
			[Vector3(-lot.x * 0.5, 0, 0), Vector2(t, lot.y)], [Vector3(lot.x * 0.5, 0, 0), Vector2(t, lot.y)]]:
		var pm := PlaneMesh.new()
		pm.size = b[1]
		pm.material = mat
		var mi := MeshInstance3D.new()
		mi.mesh = pm
		mi.position = b[0] + Vector3(0, 0.02, 0)
		mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		g.add_child(mi)
	return g


static func _rel(node: Node, root: Node) -> Transform3D:
	var t := Transform3D.IDENTITY
	var n := node
	while n != null and n != root:
		if n is Node3D:
			t = (n as Node3D).transform * t
		n = n.get_parent()
	return t


static func _content_aabb(root: Node3D) -> AABB:
	var box := AABB()
	var has := false
	for n in root.find_children("*", "VisualInstance3D", true, false):
		var vi := n as VisualInstance3D
		if not vi.visible or vi is Label3D:
			continue
		var b: AABB = _rel(vi, root) * vi.get_aabb()
		box = b if not has else box.merge(b)
		has = true
	if not has:
		box = AABB(Vector3(-1, 0, -1), Vector3(2, 2, 2))
	return box


func _camera_for(box: AABB, dir: Vector3, aspect: float, margin: float = 1.12) -> Camera3D:
	var d := dir.normalized()
	var up := Vector3.UP if abs(d.dot(Vector3.UP)) < 0.99 else Vector3.BACK
	var basis := Basis.looking_at(-d, up)
	var right := basis.x
	var cup := basis.y
	var min_r := INF; var max_r := -INF; var min_u := INF; var max_u := -INF
	for i in 8:
		var p := box.get_endpoint(i)
		min_r = min(min_r, p.dot(right)); max_r = max(max_r, p.dot(right))
		min_u = min(min_u, p.dot(cup)); max_u = max(max_u, p.dot(cup))
	var center_r := (min_r + max_r) * 0.5
	var center_u := (min_u + max_u) * 0.5
	var w := max_r - min_r
	var h := max_u - min_u
	var cam := Camera3D.new()
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.keep_aspect = Camera3D.KEEP_HEIGHT
	cam.size = max(h, w / aspect) * margin
	cam.near = 0.05
	cam.far = 2000.0
	# Eixos ortonormais (right, cup, d): centraliza o enquadramento e recua 400 m na visada.
	var origin := right * center_r + cup * center_u + d * (box.get_center().dot(d) + 400.0)
	cam.transform = Transform3D(basis, origin)
	return cam


## Renderiza `content` (ja montado) com camera dada. Devolve Image RGBA8.
func _render(content: Node3D, cam: Camera3D, size: Vector2i, extras: Array, transparent: bool) -> Image:
	var sub := SubViewport.new()
	sub.size = size
	sub.own_world_3d = true
	sub.transparent_bg = transparent
	sub.msaa_3d = Viewport.MSAA_8X
	sub.screen_space_aa = Viewport.SCREEN_SPACE_AA_DISABLED
	sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	get_root().add_child(sub)
	sub.add_child(_make_env(transparent))
	sub.add_child(_make_sun())
	for e in extras:
		sub.add_child(e)
	sub.add_child(content)
	sub.add_child(cam)
	cam.current = true
	for i in 4:
		await process_frame
	await RenderingServer.frame_post_draw
	var img := sub.get_texture().get_image()
	img.convert(Image.FORMAT_RGBA8)
	sub.remove_child(content)
	for e in extras:
		sub.remove_child(e)
		e.queue_free()
	sub.queue_free()
	await process_frame
	return img


# ---------------------------------------------------------------------------
# Modo revisao
# ---------------------------------------------------------------------------

func _render_review(scene_path: String, out_dir: String) -> bool:
	var ps: PackedScene = load(scene_path)
	if ps == null:
		printerr("[render] cena nao carregou: ", scene_path)
		return false
	var inst: Node3D = ps.instantiate()
	var box := _content_aabb(inst)
	var holder := Node3D.new()
	holder.add_child(inst)
	var fig := _scale_figure()
	fig.position = Vector3(box.end.x + 1.2, 0, box.position.z)
	holder.add_child(fig)
	holder.add_child(_marker_gizmos(inst))
	if inst.has_meta("lot_candidate_m"):
		holder.add_child(_lot_outline(inst.get_meta("lot_candidate_m")))
	var full := _content_aabb(holder)
	full = full.grow(0.3)
	var aspect := float(VIEW_W) / float(VIEW_H)
	var sheet := Image.create(VIEW_W * 2, VIEW_H * 2, false, Image.FORMAT_RGBA8)
	var views := [
		[DIR_ISO_FR, Vector2i(0, 0), false],
		[DIR_ISO_BL, Vector2i(VIEW_W, 0), false],
		[DIR_FRONT, Vector2i(0, VIEW_H), true],
		[DIR_TOP, Vector2i(VIEW_W, VIEW_H), false],
	]
	var extent: float = max(60.0, full.size.length() * 4.0)
	for v in views:
		var extras: Array = [_make_ground(extent)]
		if v[2]:
			extras.append(_make_backwall(full.end.z + 1.5, extent, max(20.0, full.size.y * 3.0)))
		var cam := _camera_for(full, v[0], aspect)
		var img: Image = await _render(holder, cam, Vector2i(VIEW_W, VIEW_H), extras, false)
		sheet.blit_rect(img, Rect2i(0, 0, VIEW_W, VIEW_H), v[1])
	var fname := scene_path.get_file().get_basename() + "_review.png"
	var out := out_dir.path_join(fname)
	var e := sheet.save_png(out)
	holder.free()
	if e != OK:
		printerr("[render] falha ao salvar ", out)
		return false
	print("[render] revisao: ", out, "  (TL iso frente-direita | TR iso fundos-esquerda | BL elevacao frontal | BR planta, fachada embaixo)")
	print("[render] envelope (m): min=", box.position, " size=", box.size)
	return true


# ---------------------------------------------------------------------------
# Modo final (previews versionados)
# ---------------------------------------------------------------------------

func _render_final(wave2_only: bool = false) -> bool:
	# Implementado em _final_impl() (arquivo separado mantem este script curto).
	var impl_path := "res://Assets/V1/Source/kit/final_previews.gd"
	if not ResourceLoader.exists(impl_path):
		printerr("[render] final_previews.gd ausente")
		return false
	var impl = load(impl_path).new()
	return await impl.run(self, wave2_only)
