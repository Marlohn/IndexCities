## Capturas por GPU da oficina com veiculos reais do jogo nas ancoras. Nao abre cidade nem save.
## -- --out=<absoluto em artifacts/verification> --shots=lift_down,lift_up,gate_closed,gate_open,tow_base,top
extends SceneTree

const PACKAGE := "res://Assets/Workshop"
const SCENE := PACKAGE+"/Scenes/commercial.mechanic_shop.a.tscn"
const CAR := "res://Assets/V1/Scenes/vehicle.car_passenger.a.tscn"
const TRUCK := "res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn"
const TOW := "res://Assets/Construction/Scenes/vehicle.tow_truck.a.tscn"
const VIEWS := {
	"hero":{"p":Vector3(0.78,0.65,-1.0),"target":Vector3(0,1.6,1.0),"size":26.0},
	"bays":{"p":Vector3(0.55,0.42,-1.0),"target":Vector3(-4.2,1.6,3.0),"size":13.0},
	"yard":{"p":Vector3(0.78,0.7,-1.0),"target":Vector3(3.5,1.0,-3.0),"size":17.0},
	"top":{"p":Vector3(0,1,-0.001),"target":Vector3(0,0,0),"size":28.0},
}
## nome -> {view, lift ("down"/"up"), gate ("closed"/"open"), car_lift, truck_bay1, tow}
const SHOTS := {
	"lift_down":{"view":"bays","lift":"down","gate":"closed","car_lift":true},
	"lift_up":{"view":"bays","lift":"up","gate":"closed","car_lift":true},
	"gate_closed":{"view":"hero","lift":"down","gate":"closed","car_lift":true},
	"gate_open":{"view":"hero","lift":"up","gate":"open","car_lift":true,"truck_bay1":true},
	"tow_base":{"view":"yard","lift":"down","gate":"closed","tow":true},
	"hero_all":{"view":"hero","lift":"up","gate":"open","car_lift":true,"truck_bay1":true,"tow":true},
	"top":{"view":"top","lift":"down","gate":"open","car_lift":true,"truck_bay1":true,"tow":true},
}
var output_dir := ""
var selected := PackedStringArray(SHOTS.keys())
var viewport: SubViewport
var stage: Node3D
var camera: Camera3D
var captures: Array = []
var errors: Array = []

func _initialize() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="):
			output_dir = arg.substr(6).replace("\\","/")
		elif arg.begins_with("--shots="):
			selected = arg.substr(8).split(",")
	var allowed := ProjectSettings.globalize_path("res://../../artifacts/verification").simplify_path().replace("\\","/")+"/"
	if not output_dir.is_absolute_path() or not (output_dir.simplify_path()+"/").begins_with(allowed):
		printerr("[workshop-preview] --out precisa ficar dentro de ",allowed)
		quit(2)
		return
	DirAccess.make_dir_recursive_absolute(output_dir)
	_run.call_deferred()

func _seek(player: AnimationPlayer, animation: String) -> void:
	player.play(animation)
	player.seek(player.current_animation_length,true)
	player.pause()

func _paint(car: Node3D, material_name: String) -> void:
	var paint: Material = load("res://Assets/V1/Materials/"+material_name+".tres")
	for part in ["Body","Hood"]:
		var mi := car.get_node_or_null(part) as MeshInstance3D
		if mi == null:
			continue
		for i in mi.mesh.get_surface_count():
			if mi.mesh.surface_get_name(i) == "car_paint":
				mi.set_surface_override_material(i,paint)

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
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 650
	stage.add_child(sun)
	var world := WorldEnvironment.new()
	world.environment = environment
	stage.add_child(world)
	_context()
	camera = Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.far = 200
	stage.add_child(camera)
	camera.make_current()
	var display := TextureRect.new()
	display.texture = viewport.get_texture()
	display.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	display.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	display.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	get_root().add_child(display)
	for key in selected:
		if not SHOTS.has(key):
			errors.append("captura desconhecida "+key)
			continue
		var shot: Dictionary = SHOTS[key]
		var shop: Node3D = (load(SCENE) as PackedScene).instantiate()
		stage.add_child(shop)
		if shot.lift == "up":
			_seek(shop.get_node("LiftPlatform/AnimationPlayer"),"lift_up")
		if shot.gate == "open":
			_seek(shop.get_node("Gate/AnimationPlayer"),"gate_open")
		if shot.get("car_lift",false):
			var car: Node3D = (load(CAR) as PackedScene).instantiate()
			shop.get_node("LiftPlatform/ServiceBay0").add_child(car)
			_paint(car,"car_paint_red")
			if shot.lift == "up":
				car.get_node("Hood").rotation = Vector3(deg_to_rad(62.0),0,0)
		if shot.get("truck_bay1",false):
			(shop.get_node("Anchors/ServiceBay1") as Node3D).add_child((load(TRUCK) as PackedScene).instantiate())
		if shot.get("tow",false):
			var tow: Node3D = (load(TOW) as PackedScene).instantiate()
			shop.get_node("Anchors/TowTruckBase").add_child(tow)
		var view: Dictionary = VIEWS[shot.view]
		camera.position = view.target + view.p.normalized()*65.0
		camera.look_at(view.target,Vector3(0,0,1) if shot.view == "top" else Vector3.UP)
		camera.size = view.size
		for i in 8:
			await process_frame
		await RenderingServer.frame_post_draw
		var img := viewport.get_texture().get_image()
		if img == null or img.is_empty():
			errors.append("captura vazia "+key)
		else:
			img.resize(1920,1200,Image.INTERPOLATE_LANCZOS)
			var file := "workshop_"+key+".png"
			if img.save_png(output_dir.path_join(file)) != OK:
				errors.append("falha gravando "+file)
			captures.append({"file":file,"width":img.get_width(),"height":img.get_height()})
			print("[workshop-preview] captured ",file)
		shop.free()
	var report := FileAccess.open(output_dir.path_join("report_workshop.json"),FileAccess.WRITE)
	report.store_string(JSON.stringify({"renderer":RenderingServer.get_current_rendering_method(),"gpu":RenderingServer.get_video_adapter_name(),"captures":captures,"errors":errors,"save_access":false},"\t"))
	print("[workshop-preview] ","OK" if errors.is_empty() else "FAILED"," captures=",captures.size())
	quit(0 if errors.is_empty() else 1)

func _box(p: Vector3, s: Vector3, color: String) -> void:
	var mi := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = s
	mi.mesh = mesh
	mi.position = p
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(color)
	m.roughness = 0.93
	mi.material_override = m
	stage.add_child(mi)

func _context() -> void:
	_box(Vector3(0,-0.31,0),Vector3(48,0.35,44),"899276")
	_box(Vector3(0,-0.10,-14),Vector3(48,0.16,6.8),"5a6263")
	for i in 10:
		_box(Vector3(-21+i*4.6,-0.008,-14),Vector3(1.6,0.015,0.09),"d6bb7a")
