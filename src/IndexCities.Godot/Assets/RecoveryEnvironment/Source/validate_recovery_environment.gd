## Validador headless da amostra RecoveryEnvironment (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
##   godot --headless --path src/IndexCities.Godot --script res://Assets/RecoveryEnvironment/Source/validate_recovery_environment.gd
##
## Confere: shaders compilam no parser da Godot; nada de textura, transparencia, tela/
## profundidade nem deslocamento de vertice; materiais sem parametros duplicados; previa com
## camera/luz/ambiente iguais ao Main.cs e chao/lago iguais ao CityWorld.cs; hash do ruido
## bem distribuido; pasta sem imagens importaveis fora de Preview/Captures.
## Limite: headless NAO compila o GLSL na GPU (isso so acontece na captura com janela).
extends SceneTree

const Build := preload("res://Assets/RecoveryEnvironment/Source/build_recovery_environment.gd")
const ROOT := "res://Assets/RecoveryEnvironment"
const GRASS_SHADER := ROOT + "/Shaders/env_grass_soft.gdshader"
const WATER_SHADER := ROOT + "/Shaders/env_lake_water.gdshader"
const GRASS_MATERIAL := ROOT + "/Materials/env_grass_soft.tres"
const WATER_MATERIAL := ROOT + "/Materials/env_lake_water.tres"
const PREVIEW_SCENE := ROOT + "/Preview/recovery_environment_preview.tscn"

const GRASS_UNIFORMS := ["base_color", "warm_tint", "cool_tint", "variation", "patch_size_m",
	"detail_size_m", "grain_size_m", "grain_strength", "roughness_value"]
const WATER_UNIFORMS := ["deep_color", "shallow_color", "shore_color", "sky_color",
	"shallow_width_m", "deep_distance_m", "shore_line_m", "edge_wobble_m", "deep_variation", "ripple_strength",
	"ripple_speed", "wind_angle_deg", "facet_size_m", "reflection_strength", "reflection_power",
	"roughness_value", "specular_value", "host_time_s"]

var _checks := 0
var _fails := 0


func _initialize() -> void:
	_validate()
	print("[validate] %d checks, %d falhas" % [_checks, _fails])
	quit(0 if _fails == 0 else 1)


func _check(ok: bool, what: String) -> void:
	_checks += 1
	if not ok:
		_fails += 1
		push_error("[validate] FALHA: " + what)


func _validate() -> void:
	_check_shader(GRASS_SHADER, GRASS_UNIFORMS)
	_check_shader(WATER_SHADER, WATER_UNIFORMS)
	_check_default(GRASS_SHADER, "base_color", "vec3(0.490196, 0.607843, 0.321569)")  # #7d9b52 do CityWorld
	_check_default(GRASS_SHADER, "roughness_value", "0.85")  # igual ao material atual
	_check_material(GRASS_MATERIAL, GRASS_SHADER)
	_check_material(WATER_MATERIAL, WATER_SHADER)
	_check_preview()
	_check_hash()
	_check_folder()


func _source(path: String) -> String:
	return FileAccess.get_file_as_string(path)


## Codigo sem comentarios (// e /* */), para as buscas nao pegarem texto explicativo.
func _code(path: String) -> String:
	var rx := RegEx.new()
	rx.compile("/\\*[\\s\\S]*?\\*/|//[^\\n]*")
	return rx.sub(_source(path), "", true)


func _check_shader(path: String, expected: Array) -> void:
	var shader: Shader = load(path)
	_check(shader != null, "carregar %s" % path)
	if shader == null:
		return
	_check(shader.get_mode() == Shader.MODE_SPATIAL, "%s e spatial" % path)
	var names := []
	for u in shader.get_shader_uniform_list():
		names.append(u["name"])
	_check(not names.is_empty(), "%s compilou no parser (uniforms visiveis)" % path)
	for n in expected:
		_check(names.has(n), "%s tem uniform %s" % [path, n])
	var src := _code(path)
	var rx := RegEx.new()
	# Somente estetico e barato: sem texturas, tela/profundidade, transparencia ou luz propria.
	for forbidden in ["sampler2D", "sampler3D", "samplerCube", "hint_screen_texture", "hint_depth_texture",
			"hint_normal_roughness_texture", "ALPHA", "EMISSION", "void light(", "render_mode", "discard",
			"instance uniform", "global uniform"]:
		_check(src.find(forbidden) == -1, "%s nao usa %s" % [path, forbidden])
	# vertex(): nenhuma atribuicao a VERTEX/POSITION/NORMAL (silhueta, nivel e raio intactos).
	var body := _function_body(src, "void vertex()")
	_check(body != "", "%s tem vertex()" % path)
	rx.compile("\\b(VERTEX|POSITION|NORMAL|TANGENT|BINORMAL)(\\.[xyzw]+)?\\s*(\\+=|-=|\\*=|/=|=(?!=))")
	_check(rx.search(body) == null, "%s nao desloca/reorienta vertices em vertex()" % path)
	_check(_source(path).find("Revisao humana: PENDENTE") != -1, "%s declara revisao PENDENTE" % path)


func _function_body(src: String, signature: String) -> String:
	var at := src.find(signature)
	if at == -1:
		return ""
	var open := src.find("{", at)
	var depth := 0
	for i in range(open, src.length()):
		var c := src[i]
		if c == "{":
			depth += 1
		elif c == "}":
			depth -= 1
			if depth == 0:
				return src.substr(open, i - open + 1)
	return ""


func _check_default(path: String, uniform: String, literal: String) -> void:
	var rx := RegEx.new()
	rx.compile("uniform\\s+\\w+\\s+" + uniform + "\\b[^=;]*=\\s*([^;]+);")
	var m := rx.search(_source(path))
	_check(m != null and m.get_string(1).strip_edges() == literal,
		"%s: padrao de %s = %s" % [path, uniform, literal])


func _check_material(path: String, shader_path: String) -> void:
	var m := ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REPLACE) as ShaderMaterial
	_check(m != null, "carregar %s como ShaderMaterial" % path)
	if m == null:
		return
	_check(m.shader != null and m.shader.resource_path == shader_path, "%s usa %s" % [path, shader_path])
	_check(_source(path).find("shader_parameter/") == -1, "%s sem parametros duplicados (padrao no shader)" % path)


func _near(a: float, b: float, eps := 0.0005) -> bool:
	return absf(a - b) <= eps


func _check_preview() -> void:
	var packed: PackedScene = load(PREVIEW_SCENE)
	_check(packed != null, "carregar %s" % PREVIEW_SCENE)
	if packed == null:
		return
	var scene := packed.instantiate() as Node3D
	_check(scene != null, "instanciar a previa")
	if scene == null:
		return
	_check(String(scene.get_meta("asset_status", "")).contains("PENDENTE"), "previa marcada PENDENTE")

	# Camera = Main.cs (ortogonal, size = zoom, far 2000, offset (300,330,300) girado, olhando o foco).
	var cam := scene.get_node_or_null("GameCamera") as Camera3D
	_check(cam != null, "previa tem GameCamera")
	if cam:
		_check(cam.projection == Camera3D.PROJECTION_ORTHOGONAL, "camera ortogonal")
		_check(_near(cam.size, 110.0) and _near(cam.far, 2000.0), "camera size 110 e far 2000")
		var expected := Build.camera_transform(Build.PREVIEW_FOCUS, Build.PREVIEW_YAW)
		_check(cam.transform.is_equal_approx(expected), "camera na formula de Main.UpdateCamera")
		_check(cam.transform.origin.is_equal_approx(Build.PREVIEW_FOCUS + Vector3(300, 330, 300)), "camera em foco + (300,330,300) com yaw 45")

	# Sol e ambiente = Main.cs.
	var sun := scene.get_node_or_null("Sun") as DirectionalLight3D
	_check(sun != null, "previa tem Sun")
	if sun:
		var s := Build.sun_for_hour(Build.PREVIEW_HOUR)
		_check(sun.light_color.is_equal_approx(Color("fff0d0")), "cor do sol fff0d0")
		_check(_near(sun.light_energy, s["energy"]), "energia do sol pela hora")
		_check(sun.rotation_degrees.is_equal_approx(s["rotation_degrees"]), "rotacao do sol pela hora")
		_check(sun.shadow_enabled and _near(sun.directional_shadow_max_distance, 650.0), "sombra ligada, 650 m")
	var we := scene.get_node_or_null("WorldEnvironment") as WorldEnvironment
	_check(we != null and we.environment != null, "previa tem WorldEnvironment")
	if we and we.environment:
		var e := we.environment
		_check(e.background_mode == Environment.BG_COLOR and e.background_color.is_equal_approx(Color("b9c9d5")), "fundo b9c9d5")
		_check(e.ambient_light_source == Environment.AMBIENT_SOURCE_COLOR and e.ambient_light_color.is_equal_approx(Color("afc5ce")) and _near(e.ambient_light_energy, 0.32), "ambiente afc5ce 0,32")

	# Chao e lago com as medidas do CityWorld.Reset; materiais da amostra.
	var ground := scene.get_node_or_null("World/Ground") as MeshInstance3D
	_check(ground != null and ground.mesh is BoxMesh, "chao BoxMesh")
	if ground and ground.mesh is BoxMesh:
		_check((ground.mesh as BoxMesh).size.is_equal_approx(Vector3(512, 1, 512)), "chao 512 x 1 x 512")
		_check(ground.position.is_equal_approx(Vector3(0, -0.56, 0)), "chao em y -0,56 (topo -0,06)")
		_check(ground.material_override != null and ground.material_override.resource_path == GRASS_MATERIAL, "chao usa %s" % GRASS_MATERIAL)
	var lake := scene.get_node_or_null("World/Lake") as MeshInstance3D
	_check(lake != null and lake.mesh is CylinderMesh, "lago CylinderMesh")
	if lake and lake.mesh is CylinderMesh:
		var c := lake.mesh as CylinderMesh
		var r: float = scene.get_meta("lake_radius_m", -1.0)
		_check(_near(c.top_radius, r) and _near(c.bottom_radius, r + 1.0), "lago top R e bottom R + 1 (como CityWorld)")
		_check(_near(c.height, 0.15) and c.radial_segments == 64 and c.cap_top, "lago altura 0,15, 64 segmentos, tampa")
		_check(_near(lake.position.y, -0.04), "lago em y -0,04 (topo +0,035)")
		_check(lake.material_override != null and lake.material_override.resource_path == WATER_MATERIAL, "lago usa %s" % WATER_MATERIAL)

	# Higiene da cena: sem scripts, fisica/colisao, luzes extras, animacao ou particulas.
	var lights := 0
	var bad := []
	var stack: Array[Node] = [scene]
	while not stack.is_empty():
		var n: Node = stack.pop_back()
		for ch in n.get_children():
			stack.push_back(ch)
		if n.get_script() != null:
			bad.append("script em " + str(n.get_path()))
		if n is CollisionObject3D or n is CollisionShape3D:
			bad.append("fisica em " + str(n.get_path()))
		if n is AnimationPlayer or n is GPUParticles3D or n is CPUParticles3D:
			bad.append("animacao/particulas em " + str(n.get_path()))
		if n is Light3D:
			lights += 1
	_check(bad.is_empty(), "previa sem script/fisica/animacao: %s" % [bad])
	_check(lights == 1, "previa so com o Sun (luzes: %d)" % lights)
	scene.free()


## Produto uint32 (mod 2^32) sem estourar o int64 do GDScript.
static func _mul32(a: int, b: int) -> int:
	var lo := (a & 0xFFFF) * b
	var hi := ((a >> 16) * (b & 0xFFFF)) & 0xFFFF
	return (lo + (hi << 16)) & 0xFFFFFFFF


## Replica do env_cell_hash (uint32) para conferir distribuicao do ruido.
static func _hash(cx: int, cy: int, salt: int) -> float:
	const M := 0xFFFFFFFF
	var ux := (cx + 65536) & M
	var uy := (cy + 65536) & M
	var h := (_mul32(ux, 0x8F2D5C37) + _mul32(uy, 0x4CF5AD43) + _mul32(salt, 0x2E6B1A95)) & M
	h ^= h >> 15
	h = _mul32(h, 0xE3A9B0D1)
	h ^= h >> 13
	h = _mul32(h, 0x68E31DA5)
	h ^= h >> 16
	return float(h >> 8) / 16777216.0


func _check_hash() -> void:
	# Celulas cobrindo o mapa inteiro na escala mais fina usada (0,4 m -> ~+-660 celulas).
	for salt in [11, 23, 37, 53, 61, 71]:
		var n := 0
		var sum := 0.0
		var sum2 := 0.0
		var cx := 0.0
		var cy := 0.0
		for y in range(-96, 96):
			for x in range(-96, 96):
				var v := _hash(x * 7, y * 7, salt)
				var vx := _hash(x * 7 + 1, y * 7, salt)
				var vy := _hash(x * 7, y * 7 + 1, salt)
				n += 1
				sum += v
				sum2 += v * v
				cx += (v - 0.5) * (vx - 0.5)
				cy += (v - 0.5) * (vy - 0.5)
		var mean := sum / n
		var std := sqrt(sum2 / n - mean * mean)
		var rx := cx / n / (std * std)
		var ry := cy / n / (std * std)
		_check(absf(mean - 0.5) < 0.01 and absf(std - 0.2887) < 0.01, "hash salt %d: media %.4f, desvio %.4f" % [salt, mean, std])
		_check(absf(rx) < 0.02 and absf(ry) < 0.02, "hash salt %d: correlacao vizinha x %.4f y %.4f" % [salt, rx, ry])


func _check_folder() -> void:
	var files := []
	_walk(ProjectSettings.globalize_path(ROOT), "", files)
	var total := 0
	for f in files:
		var rel: String = f[0]
		var size: int = f[1]
		total += size
		var ext := rel.get_extension().to_lower()
		if ext in ["png", "jpg", "jpeg", "webp", "svg", "exr", "hdr", "ktx", "dds"]:
			_check(rel.begins_with("Preview/Captures/"), "imagem so em Preview/Captures (ignorada pela Godot): %s" % rel)
		else:
			_check(size < 256 * 1024, "arquivo pequeno (< 256 KB): %s (%d B)" % [rel, size])
	if DirAccess.dir_exists_absolute(ProjectSettings.globalize_path(ROOT + "/Preview/Captures")):
		_check(FileAccess.file_exists(ROOT + "/Preview/Captures/.gdignore"), "Preview/Captures tem .gdignore")
	print("[validate] pasta: %d arquivos, %.1f KB" % [files.size(), total / 1024.0])


func _walk(abs_dir: String, rel: String, out: Array) -> void:
	var d := DirAccess.open(abs_dir.path_join(rel))
	if d == null:
		return
	d.include_hidden = true
	for f in d.get_files():
		var r := rel.path_join(f) if rel != "" else f
		var fa := FileAccess.open(abs_dir.path_join(r), FileAccess.READ)
		out.append([r, fa.get_length() if fa else 0])
	for sub in d.get_directories():
		_walk(abs_dir, rel.path_join(sub) if rel != "" else sub, out)
