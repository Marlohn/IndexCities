## Preview e verificacao por GPU. Nao abre cidade, nao le nem grava saves.
## -- --out=<absoluto em artifacts/verification> --lighting=game|studio --views=hero,front,rear,detail,scale
extends SceneTree

const PACKAGE := "res://Assets/Workshop"
const SCENE := PACKAGE+"/Scenes/commercial.mechanic_shop.a.tscn"
const VIEWS := {
	"hero":{"p":Vector3(0.78,0.65,-1.0),"target":Vector3(0,1.6,0),"size":18.5},
	"front":{"p":Vector3(0.1,0.23,-1),"target":Vector3(0,2.2,0.2),"size":15.0},
	"rear":{"p":Vector3(-0.9,0.68,1),"target":Vector3(0,1.8,0.3),"size":18.5},
	"detail":{"p":Vector3(0.38,0.18,-1),"target":Vector3(-6.95,1.8,1.3),"size":6.0},
	"office":{"p":Vector3(0.80,0.4,-1),"target":Vector3(6.3,1.8,-0.4),"size":7.5},
	"scale":{"p":Vector3(0.85,0.90,-1),"target":Vector3(0,1.0,-1.5),"size":35.0},
	"thumb":{"p":Vector3(0.78,0.65,-1),"target":Vector3(0,2.2,0),"size":28.5},
}

var output_dir := ""
var lighting := "game"
var selected := PackedStringArray(["hero","front","rear","detail","office","scale","thumb"])
var viewport: SubViewport
var stage: Node3D
var camera: Camera3D
var captures: Array = []
var errors: Array = []
var asset_instance: Node3D
var interactive := false
var context_trees: Array = []
var source_format := "native"

func _initialize() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg == "--inspect":
			interactive = true
		elif arg.begins_with("--out="):
			output_dir = arg.substr(6).replace("\\","/")
		elif arg.begins_with("--lighting="):
			lighting = arg.substr(11)
		elif arg == "--source=glb":
			source_format = "glb"
		elif arg.begins_with("--views="):
			selected = arg.substr(8).split(",")
	var allowed := ProjectSettings.globalize_path("res://../../artifacts/verification").simplify_path().replace("\\","/")+"/"
	if not output_dir.is_absolute_path() or not (output_dir.simplify_path()+"/").begins_with(allowed):
		printerr("[workshop-preview] --out precisa ficar dentro de ",allowed)
		quit(2)
		return
	DirAccess.make_dir_recursive_absolute(output_dir)
	_run.call_deferred()

func _run() -> void:
	viewport = SubViewport.new()
	viewport.size = Vector2i(2560,1600)
	viewport.msaa_3d = Viewport.MSAA_4X
	viewport.own_world_3d = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	get_root().add_child(viewport)
	stage = Node3D.new()
	viewport.add_child(stage)
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("b9c9d5")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("afc5ce")
	environment.ambient_light_energy = 0.32
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-48,-30,0)
	sun.light_color = Color("fff0d0")
	sun.light_energy = 1.0
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 650
	sun.shadow_blur = 1.0
	stage.add_child(sun)
	if lighting == "studio":
		environment.ambient_light_energy = 0.55
		environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
		environment.ssao_enabled = true
		environment.ssao_radius = 0.9
		environment.ssao_intensity = 1.8
		environment.ssao_power = 1.4
		sun.light_energy = 1.25
		sun.rotation_degrees = Vector3(-45,-145,0)
		sun.light_angular_distance = 3.0
		var fill := DirectionalLight3D.new()
		fill.rotation_degrees = Vector3(-30,-20,0)
		fill.light_energy = 0.38
		fill.light_color = Color("d3e6ed")
		stage.add_child(fill)
	var world := WorldEnvironment.new()
	world.environment = environment
	stage.add_child(world)
	if source_format == "glb":
		var doc := GLTFDocument.new()
		var state := GLTFState.new()
		if doc.append_from_file(PACKAGE+"/Scenes/commercial.mechanic_shop.a.glb",state) == OK:
			asset_instance = doc.generate_scene(state)
	else:
		var loaded: PackedScene = load(SCENE)
		if loaded != null:
			asset_instance = loaded.instantiate()
	if asset_instance == null:
		printerr("cena ausente/invalida")
		quit(1)
		return
	stage.add_child(asset_instance)
	_validate(asset_instance)
	_context()
	camera = Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.near = 0.1
	camera.far = 200
	stage.add_child(camera)
	camera.make_current()
	# Janela real mostra a mesma camera que produz a evidencia.
	var display := TextureRect.new()
	display.texture = viewport.get_texture()
	display.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	display.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	display.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	get_root().add_child(display)
	await process_frame
	for key in selected:
		if not VIEWS.has(key):
			errors.append("vista desconhecida "+key)
			continue
		var view: Dictionary = VIEWS[key]
		for tree in context_trees:
			tree.visible = key == "scale" or key == "rear"
		camera.position = view.target + view.p.normalized()*65.0
		camera.look_at(view.target)
		camera.size = view.size
		var thumb := key == "thumb"
		viewport.size = Vector2i(1024,1024) if thumb else Vector2i(2560,1600)
		for i in 8:
			await process_frame
		await RenderingServer.frame_post_draw
		var img := viewport.get_texture().get_image()
		if img == null or img.is_empty():
			errors.append("captura vazia "+key)
			continue
		img.resize(512 if thumb else 1920,512 if thumb else 1200,Image.INTERPOLATE_LANCZOS)
		var name := "mechanic_shop_"+lighting+"_"+("glb_" if source_format=="glb" else "")+key+".png"
		if img.save_png(output_dir.path_join(name)) != OK:
			errors.append("falha gravando "+name)
		captures.append({"file":name,"width":img.get_width(),"height":img.get_height(),"camera_size":camera.size})
		print("[workshop-preview] captured ",name)
	var report := FileAccess.open(output_dir.path_join("report_"+lighting+("_glb" if source_format=="glb" else "")+("_inspect" if interactive else "")+".json"),FileAccess.WRITE)
	report.store_string(JSON.stringify({"scene":SCENE,"renderer":RenderingServer.get_current_rendering_method(),"lighting":lighting,
		"lighting_source":"Main.cs:21-22 daytime constants copied manually" if lighting=="game" else "independent studio setup",
		"sun_energy":sun.light_energy,"sun_rotation_degrees":[sun.rotation_degrees.x,sun.rotation_degrees.y,sun.rotation_degrees.z],
		"sun_color":sun.light_color.to_html(),"ambient_energy":environment.ambient_light_energy,"ambient_color":environment.ambient_light_color.to_html(),
		"gpu":RenderingServer.get_video_adapter_name(),"source_format":source_format,"captures":captures,"errors":errors,"save_access":false},"\t"))
	print("[workshop-preview] ","OK" if errors.is_empty() else "FAILED", " captures=",captures.size()," errors=",errors.size())
	if interactive and errors.is_empty():
		viewport.size = Vector2i(1920,1200)
		camera.size = VIEWS.hero.size
		camera.position = VIEWS.hero.target+VIEWS.hero.p.normalized()*65.0
		camera.look_at(VIEWS.hero.target)
		var controls := Node.new()
		controls.set_script(load(PACKAGE+"/Source/viewer_controls.gd"))
		get_root().add_child(controls)
		controls.configure(camera,VIEWS.hero.target)
		print("[workshop-preview] INTERACTIVE: arraste para girar; roda para zoom; R restaura; Esc fecha")
		return
	quit(0 if errors.is_empty() else 1)

func _validate(node: Node) -> void:
	if node.get_script() != null:
		errors.append("script dentro do asset: "+str(node.name))
	if node is Light3D or node is CollisionObject3D or node is AnimationPlayer:
		errors.append("runtime nao permitido: "+str(node.name))
	if node is MeshInstance3D:
		var mi := node as MeshInstance3D
		if mi.mesh == null:
			errors.append("malha ausente: "+str(node.name))
		else:
			var a := mi.transform * mi.mesh.get_aabb()
			if not a.position.is_finite() or not a.size.is_finite():
				errors.append("envelope nao finito: "+str(node.name))
			if not mi.mesh is TextMesh and (a.position.x < -11.01 or a.end.x > 11.01 or a.position.z < -8.51 or a.end.z > 8.51):
				errors.append("fora do lote candidato: "+str(node.name))
	for child in node.get_children():
		_validate(child)

func _material(hex: String) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(hex)
	m.roughness = 0.93
	return m

func _context_box(p: Vector3, s: Vector3, color: String) -> void:
	var mi := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = s
	mi.mesh = mesh
	mi.position = p
	mi.material_override = _material(color)
	stage.add_child(mi)

func _context() -> void:
	_context_box(Vector3(0,-0.31,0),Vector3(42,0.35,32),"899276")
	_context_box(Vector3(0,-0.10,-12),Vector3(42,0.16,6.8),"5a6263")
	_context_box(Vector3(0,0.01,-8.9),Vector3(42,0.16,0.80),"c8c7b8")
	for i in 9:
		_context_box(Vector3(-19+i*4.6,-0.008,-12),Vector3(1.6,0.015,0.09),"d6bb7a")
	# Contexto usa os assets atuais deste projeto, fora do pacote entregue.
	for item in [
		{"path":"vegetation.tree_broadleaf.a","p":Vector3(-15,0,5.5),"scale":1.0},
		{"path":"vegetation.tree_broadleaf.a","p":Vector3(15,0,6.5),"scale":1.15},
		{"path":"agent.sim_pedestrian.a","p":Vector3(8.1,0,-5.0),"scale":1.0}]:
		var packed: PackedScene = load("res://Assets/V1/Scenes/"+item.path+".tscn")
		if packed != null:
			var obj := packed.instantiate() as Node3D
			obj.position = item.p
			obj.scale = Vector3.ONE*item.scale
			stage.add_child(obj)
			if item.path.begins_with("vegetation"):
				context_trees.append(obj)
