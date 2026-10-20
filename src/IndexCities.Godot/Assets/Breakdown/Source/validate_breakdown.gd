## Validacao dos props de quebra e dos ganchos dos veiculos V1. Sem cidade nem save.
## godot --headless --path src/IndexCities.Godot --script res://Assets/Breakdown/Source/validate_breakdown.gd
extends SceneTree

const ROOT := "res://Assets/Breakdown"
const V1_VEHICLES := ["res://Assets/V1/Scenes/vehicle.car_passenger.a.tscn", "res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn"]
var checks := 0
var failures: Array = []

func _initialize() -> void:
	_run.call_deferred()

func expect(ok: bool, msg: String) -> void:
	checks += 1
	if not ok:
		failures.append(msg)
		printerr("[breakdown-validate] ", msg)

func _run() -> void:
	_props()
	for path in V1_VEHICLES:
		_vehicle(path)
	var report := FileAccess.open(ROOT+"/Preview/validation.json", FileAccess.WRITE)
	report.store_string(JSON.stringify({"checks": checks, "failures": failures, "engine": Engine.get_version_info().string}, "\t"))
	print("[breakdown-validate] %d checks; %d failures" % [checks, failures.size()])
	quit(0 if failures.is_empty() else 1)

func _walk(n: Node, out: Array) -> void:
	out.append(n)
	for c in n.get_children():
		_walk(c, out)

func _props() -> void:
	var triangle: Node3D = (load(ROOT+"/Scenes/prop.warning_triangle.a.tscn") as PackedScene).instantiate()
	expect(triangle.get_node_or_null("Body") is MeshInstance3D, "triangulo: Body")
	var size: Vector3 = triangle.get_meta("visual_envelope_size")
	expect(size.x <= 0.7 and size.y <= 0.7, "triangulo: envelope compacto")
	var smoke: Node3D = (load(ROOT+"/Scenes/prop.breakdown_smoke.a.tscn") as PackedScene).instantiate()
	var particles := smoke.get_node_or_null("Smoke") as CPUParticles3D
	expect(particles != null and particles.amount <= 16 and particles.mesh != null, "fumaca: CPUParticles3D barato")
	var hazard: Node3D = (load(ROOT+"/Scenes/prop.hazard_lights.a.tscn") as PackedScene).instantiate()
	for corner in ["FL", "FR", "RL", "RR"]:
		var light := hazard.get_node_or_null("Light"+corner)
		expect(light != null and light.get_node_or_null("Lens") is MeshInstance3D and light.get_node_or_null("Glow") is MeshInstance3D, "pisca: Light"+corner)
		if light != null:
			expect(is_equal_approx((-light.transform.basis.z).z, -1.0 if corner.begins_with("F") else 1.0), "pisca: heading Light"+corner)
	var player := hazard.get_node_or_null("AnimationPlayer") as AnimationPlayer
	expect(player != null and player.has_animation("blink"), "pisca: animacao blink")
	if player != null and player.has_animation("blink"):
		expect(player.get_animation("blink").loop_mode == Animation.LOOP_LINEAR and player.get_animation("blink").get_track_count() == 4, "pisca: blink em loop com 4 tracks")
	var nodes: Array = []
	for scene in [triangle, smoke, hazard]:
		_walk(scene, nodes)
	for n in nodes:
		expect(n.get_script() == null and not (n is Light3D) and not (n is CollisionObject3D) and not (n is GPUParticles3D), "sem runtime inesperado: "+str(n.name))
		if n is MeshInstance3D:
			for s in n.mesh.get_surface_count():
				expect(n.mesh.surface_get_material(s) != null, "material ausente em "+str(n.name))
	for scene in [triangle, smoke, hazard]:
		scene.free()

func _vehicle(path: String) -> void:
	var v: Node3D = (load(path) as PackedScene).instantiate()
	root.add_child(v)
	var hood := v.get_node_or_null("Hood") as MeshInstance3D
	expect(hood != null and hood.rotation == Vector3.ZERO, path.get_file()+": Hood fechado")
	var player := v.get_node_or_null("AnimationPlayer") as AnimationPlayer
	expect(player != null and player.get_animation_list() == PackedStringArray(["hood_open"]) and player.autoplay == "", path.get_file()+": so hood_open, sem autoplay")
	if player != null and player.has_animation("hood_open"):
		var a := player.get_animation("hood_open")
		expect(a.get_track_count() == 1 and str(a.track_get_path(0)) == "Hood:rotation", path.get_file()+": hood_open gira so o Hood")
	for name in ["HazardFL", "HazardFR", "HazardRL", "HazardRR", "HoodSmoke"]:
		expect(v.get_node_or_null(name) is Marker3D, path.get_file()+": "+name)
	var fl := v.get_node("HazardFL") as Node3D
	var fr := v.get_node("HazardFR") as Node3D
	expect(is_equal_approx(fl.position.x, -fr.position.x) and fl.position.z < 0.0, path.get_file()+": pisca dianteiro espelhado")
	var size: Vector3 = v.get_meta("visual_envelope_size")
	expect(size.y < 3.4, path.get_file()+": envelope fechado preservado")
	v.queue_free()
