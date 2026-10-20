## Capturas por GPU dos veiculos quebrados (capo aberto + triangulo + pisca + fumaca), montados
## como a integracao fara: props instanciados e encaixados nos marcadores do veiculo.
## -- --out=<absoluto em artifacts/verification> [--shots=car_on,car_off,truck_on,fleet]
extends SceneTree

const BREAKDOWN := "res://Assets/Breakdown/Scenes/"
const FLEET := [
	"res://Assets/V1/Scenes/vehicle.car_passenger.a.tscn",
	"res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.truck_dump.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.truck_mixer.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.truck_flatbed.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.tow_truck.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.tow_truck_heavy.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.asphalt_paver.a.tscn",
	"res://Assets/Construction/Scenes/vehicle.road_roller.a.tscn",
]
const SHOTS := {
	"car_on":{"vehicles":[0],"blink":0.2,"view":Vector3(-1,0.7,-1.0),"size":9.0,"target":Vector3(0,1.0,2.0)},
	"car_off":{"vehicles":[0],"blink":0.6,"view":Vector3(-1,0.7,-1.0),"size":9.0,"target":Vector3(0,1.0,2.0)},
	"car_rear":{"vehicles":[0],"blink":0.2,"view":Vector3(1,0.7,1.0),"size":9.0,"target":Vector3(0,0.8,1.5)},
	"truck_on":{"vehicles":[1],"blink":0.2,"view":Vector3(-1,0.7,-1.0),"size":12.0,"target":Vector3(0,1.4,2.0)},
	"car_and_truck":{"vehicles":[0,1],"blink":0.2,"view":Vector3(-1,0.7,-1.0),"size":16.0,"target":Vector3(0,1.2,1.0)},
	"fleet":{"vehicles":[0,1,2,3,4,5,6,7,8],"blink":0.2,"view":Vector3(-1,0.8,-1.0),"size":38.0,"target":Vector3(0,1.0,0.0)},
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
		printerr("[breakdown-preview] --out precisa ficar dentro de ",allowed)
		quit(2)
		return
	DirAccess.make_dir_recursive_absolute(output_dir)
	_run.call_deferred()

func _paint(car: Node3D) -> void:
	var paint: Material = load("res://Assets/V1/Materials/car_paint_blue.tres")
	for part in ["Body","Hood"]:
		var mi := car.get_node_or_null(part) as MeshInstance3D
		if mi != null:
			for i in mi.mesh.get_surface_count():
				if mi.mesh.surface_get_name(i) == "car_paint":
					mi.set_surface_override_material(i,paint)

## Veiculo quebrado: capo aberto, triangulo atras, pisca nos 4 marcadores, fumaca no HoodSmoke.
func _broken(scene_path: String, at: Vector3, blink_time: float) -> Node3D:
	var vehicle: Node3D = (load(scene_path) as PackedScene).instantiate()
	vehicle.position = at
	stage.add_child(vehicle)
	if scene_path.ends_with("car_passenger.a.tscn"):
		_paint(vehicle)
	var anim: AnimationPlayer = vehicle.get_node("AnimationPlayer")
	anim.play("hood_open")
	anim.seek(anim.current_animation_length,true)
	anim.pause()
	var box := AABB()
	var first := true
	for n in vehicle.find_children("*","MeshInstance3D",true,false):
		var b: AABB = (n as MeshInstance3D).global_transform * (n as MeshInstance3D).mesh.get_aabb()
		box = b if first else box.merge(b)
		first = false
	var smoke: Node3D = (load(BREAKDOWN+"prop.breakdown_smoke.a.tscn") as PackedScene).instantiate()
	smoke.transform = vehicle.get_node("HoodSmoke").global_transform
	stage.add_child(smoke)
	var hazard: Node3D = (load(BREAKDOWN+"prop.hazard_lights.a.tscn") as PackedScene).instantiate()
	vehicle.add_child(hazard)
	hazard.transform = Transform3D.IDENTITY
	for corner in ["FL","FR","RL","RR"]:
		hazard.get_node("Light"+corner).global_transform = vehicle.get_node("Hazard"+corner).global_transform
	var blink: AnimationPlayer = hazard.get_node("AnimationPlayer")
	blink.play("blink")
	blink.seek(blink_time,true)
	blink.pause()
	var triangle: Node3D = (load(BREAKDOWN+"prop.warning_triangle.a.tscn") as PackedScene).instantiate()
	stage.add_child(triangle)
	triangle.global_position = Vector3(at.x,0,box.end.z+4.0)
	return vehicle

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
	stage.add_child(sun)
	var world := WorldEnvironment.new()
	world.environment = environment
	stage.add_child(world)
	var ground := MeshInstance3D.new()
	var plane := BoxMesh.new()
	plane.size = Vector3(80,0.2,80)
	ground.mesh = plane
	ground.position = Vector3(0,-0.1,0)
	var gm := StandardMaterial3D.new()
	gm.albedo_color = Color("6d7473")
	ground.material_override = gm
	stage.add_child(ground)
	camera = Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.far = 300
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
		var holder := Node3D.new()
		stage.add_child(holder)
		var spawned: Array = []
		var list: Array = shot.vehicles
		for i in list.size():
			var x: float = ((i % 3) - 1) * 11.0 if list.size() > 2 else (i - (list.size() - 1) / 2.0) * 6.5
			var z: float = ((i / 3) - 1) * 12.0 if list.size() > 2 else 0.0
			spawned.append(_broken(FLEET[list[i]], Vector3(x, 0, z), shot.blink))
		var view: Vector3 = shot.view
		camera.position = shot.target + view.normalized()*100.0
		camera.look_at(shot.target)
		camera.size = shot.size
		for i in 160:
			await process_frame
		await RenderingServer.frame_post_draw
		var img := viewport.get_texture().get_image()
		if img == null or img.is_empty():
			errors.append("captura vazia "+key)
		else:
			img.resize(1920,1200,Image.INTERPOLATE_LANCZOS)
			var file := "breakdown_"+key+".png"
			if img.save_png(output_dir.path_join(file)) != OK:
				errors.append("falha gravando "+file)
			captures.append({"file":file,"width":img.get_width(),"height":img.get_height()})
			print("[breakdown-preview] captured ",file)
		for child in stage.get_children():
			if child != sun and child != world and child != ground and child != camera:
				child.free()
		holder = null
	var report := FileAccess.open(output_dir.path_join("report_breakdown.json"),FileAccess.WRITE)
	report.store_string(JSON.stringify({"renderer":RenderingServer.get_current_rendering_method(),"gpu":RenderingServer.get_video_adapter_name(),"captures":captures,"errors":errors,"save_access":false},"\t"))
	print("[breakdown-preview] ","OK" if errors.is_empty() else "FAILED"," captures=",captures.size())
	quit(0 if errors.is_empty() else 1)
