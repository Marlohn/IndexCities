## Validador headless da amostra V1 (ferramenta; NAO e carregado pelo jogo).
##
##   godot --headless --path <raiz-do-projeto> --script res://Assets/V1/Source/validate_v1_assets.gd
##
## Carrega e instancia cada cena da amostra e confere o contrato visual combinado:
## nos esperados, marcadores candidatos, ausencia de scripts/luzes/fisica/autoplay,
## malhas e materiais como recursos externos resolviveis, nenhum material emissivo
## aplicado por padrao, letreiro vazio e metadados de ID/envelope. Sai com codigo 1
## se algo falhar. Nao valida estetica, desempenho, exportacao nem integracao C#.
extends SceneTree

const SCENES := {
	"res://Assets/V1/Scenes/civic.city_hall.a.tscn": {
		"visual_id": "civic.city_hall.a",
		"nodes": ["Body", "Candidate_PedestrianEntry"],
	},
	"res://Assets/V1/Scenes/residential.small_house.a.tscn": {
		"visual_id": "residential.small_house.a",
		"nodes": ["Body", "Candidate_PedestrianEntry"],
	},
	"res://Assets/V1/Scenes/commercial.grocery_small.a.tscn": {
		"visual_id": "commercial.grocery_small.a",
		"nodes": ["Body", "Sign", "Sign/SignPanel", "Sign/SignText", "Candidate_PedestrianEntry", "Candidate_CargoPoint"],
		"empty_labels": ["Sign/SignText"],
	},
	"res://Assets/V1/Scenes/road.urban_1x1.a.straight.tscn": {
		"visual_id": "road.urban_1x1.a",
		"nodes": ["Connector_West", "Connector_East", "Candidate_LampSlot"],
	},
	"res://Assets/V1/Scenes/road.urban_1x1.a.corner_l.tscn": {
		"visual_id": "road.urban_1x1.a",
		"nodes": ["Connector_West", "Connector_North", "Candidate_LampSlot"],
	},
	"res://Assets/V1/Scenes/road.urban_1x1.a.lamp.tscn": {
		"visual_id": "road.urban_1x1.a",
		"nodes": ["Body", "LampGlass", "Candidate_LightSource"],
	},
	"res://Assets/V1/Scenes/vegetation.tree_broadleaf.a.tscn": {
		"visual_id": "vegetation.tree_broadleaf.a",
		"nodes": ["Body"],
	},
	"res://Assets/V1/Scenes/agent.sim_pedestrian.a.tscn": {
		"visual_id": "agent.sim_pedestrian.a",
		"nodes": ["Rig", "Rig/Hips", "Rig/Hips/Torso", "Rig/Hips/Head", "Rig/Hips/ArmL", "Rig/Hips/ArmR", "Rig/Hips/LegL", "Rig/Hips/LegR", "AnimationPlayer"],
		"animations": ["idle", "walk"],
	},
	"res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn": {
		"visual_id": "vehicle.truck_cargo.a",
		"nodes": ["Body", "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR", "Candidate_CargoDoor"],
		"shared_mesh": ["Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"],
	},
	"res://Assets/V1/Scenes/vehicle.car_passenger.a.tscn": {
		"visual_id": "vehicle.car_passenger.a",
		"nodes": ["Body", "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"],
		"shared_mesh": ["Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"],
	},
	# --- Segunda onda (lote logico do host em X x Z; envelope precisa caber) ---------
	"res://Assets/V1/Scenes/civic.works_depot.a.tscn": {
		"visual_id": "civic.works_depot.a", "lot": Vector2(24, 22),
		"nodes": ["Body", "Candidate_PedestrianEntry", "Candidate_VehicleGate", "Candidate_CargoPoint", "State_Stock"],
	},
	"res://Assets/V1/Scenes/agriculture.farm_small.a.tscn": {
		"visual_id": "agriculture.farm_small.a", "lot": Vector2(36, 40),
		"nodes": ["Body", "Candidate_PedestrianEntry", "Candidate_CargoPoint", "State_Crop"],
	},
	"res://Assets/V1/Scenes/commercial.fuel_station.a.tscn": {
		"visual_id": "commercial.fuel_station.a", "lot": Vector2(24, 24),
		"nodes": ["Body", "Sign", "Sign/SignPanel", "Sign/SignText", "Candidate_PedestrianEntry", "Candidate_VehicleEntry", "Candidate_VehicleExit", "Candidate_FuelDelivery", "Candidate_PumpStop_1"],
		"empty_labels": ["Sign/SignText"],
	},
	"res://Assets/V1/Scenes/utility.water_intake.a.tscn": {
		"visual_id": "utility.water_intake.a", "lot": Vector2(14, 16),
		"nodes": ["Body", "Candidate_PedestrianEntry", "Candidate_WaterSource", "Candidate_PipeOutlet"],
	},
	"res://Assets/V1/Scenes/utility.water_treatment.a.tscn": {
		"visual_id": "utility.water_treatment.a", "lot": Vector2(24, 26),
		"nodes": ["Body", "Candidate_PedestrianEntry", "Candidate_PipeInlet", "Candidate_PipeOutlet", "State_Water"],
	},
	"res://Assets/V1/Scenes/utility.sewage_treatment.a.tscn": {
		"visual_id": "utility.sewage_treatment.a", "lot": Vector2(26, 28),
		"nodes": ["Body", "Candidate_PedestrianEntry", "Candidate_PipeInlet", "Candidate_PipeOutlet", "State_Water"],
	},
	"res://Assets/V1/Scenes/utility.wind_power.a.tscn": {
		"visual_id": "utility.wind_power.a", "lot": Vector2(18, 18),
		"nodes": ["Body", "Nacelle", "Nacelle/Rotor", "Candidate_ServiceEntry"],
	},
	"res://Assets/V1/Preview/v1_sample_board.tscn": {
		"preview": true,
		"nodes": ["Road", "Buildings", "Lamps", "ScaleProps"],
	},
	"res://Assets/V1/Preview/v1_wave2_board.tscn": {
		"preview": true,
		"nodes": ["Road", "Facilities", "LotOutlines"],
	},
}

const FORBIDDEN_CLASSES := ["Light3D", "CollisionObject3D", "CollisionShape3D", "GPUParticles3D", "CPUParticles3D", "AudioStreamPlayer3D", "AudioStreamPlayer", "Timer", "NavigationRegion3D"]

var _fail := 0
var _checks := 0


func _ok(cond: bool, scene: String, msg: String) -> void:
	_checks += 1
	if not cond:
		_fail += 1
		printerr("[FAIL] %s :: %s" % [scene.get_file(), msg])


func _initialize() -> void:
	for path in SCENES.keys():
		_check_scene(path, SCENES[path])
	print("[validate] %d checks, %d falhas" % [_checks, _fail])
	quit(1 if _fail > 0 else 0)


static func _all(node: Node, out: Array) -> void:
	for c in node.get_children():
		out.append(c)
		_all(c, out)


func _check_scene(path: String, spec: Dictionary) -> void:
	var is_preview: bool = spec.get("preview", false)
	_ok(ResourceLoader.exists(path), path, "arquivo de cena existe")
	if not ResourceLoader.exists(path):
		return
	var ps: PackedScene = load(path)
	_ok(ps != null, path, "cena carrega (dependencias resolvidas)")
	if ps == null:
		return
	var root: Node = ps.instantiate()
	_ok(root != null and root is Node3D, path, "instancia como Node3D")
	if root == null:
		return
	if not is_preview:
		_ok(str(root.get_meta("visual_id", "")) == spec["visual_id"], path, "metadado visual_id == %s" % spec["visual_id"])
		_ok(root.has_meta("visual_envelope_min") and root.has_meta("visual_envelope_size"), path, "metadados de envelope presentes")
		_ok(str(root.get_meta("asset_status", "")).contains("PENDENTE"), path, "status de revisao PENDENTE gravado")
	for n in spec.get("nodes", []):
		_ok(root.get_node_or_null(n) != null, path, "no esperado '%s'" % n)
	if spec.has("lot") and root.has_meta("visual_envelope_size"):
		var lot: Vector2 = spec["lot"]
		var mn: Vector3 = root.get_meta("visual_envelope_min")
		var sz: Vector3 = root.get_meta("visual_envelope_size")
		var mx := mn + sz
		_ok(mn.x >= -lot.x * 0.5 - 0.001 and mx.x <= lot.x * 0.5 + 0.001 and mn.z >= -lot.y * 0.5 - 0.001 and mx.z <= lot.y * 0.5 + 0.001 and mn.y >= -0.001, path, "envelope cabe no lote %.0f x %.0f centrado no pivo, acima do solo" % [lot.x, lot.y])
		_ok(root.has_meta("lot_candidate_m"), path, "metadado lot_candidate_m presente")
	var nodes: Array = []
	_all(root, nodes)
	nodes.push_front(root)
	var tris := 0
	var meshes := 0
	for node in nodes:
		var inside_instance: bool = node != root and node.owner != root
		if not inside_instance or not is_preview:
			_ok(node.get_script() == null, path, "sem script em '%s'" % root.get_path_to(node))
		for cls in FORBIDDEN_CLASSES:
			if node.is_class(cls) and not (is_preview and node != root and node.get_parent() == root and node.name.begins_with("Preview")):
				_ok(false, path, "classe proibida %s em '%s'" % [cls, root.get_path_to(node)])
		if not is_preview and node != root and String(node.name).begins_with("State_") and node is Node3D:
			_ok(not (node as Node3D).visible, path, "no de estado '%s' oculto por padrao (host liga com estado real)" % root.get_path_to(node))
		if node is AnimationPlayer:
			_ok((node as AnimationPlayer).autoplay == "", path, "AnimationPlayer sem autoplay")
			for a in spec.get("animations", []):
				_ok((node as AnimationPlayer).has_animation(a), path, "animacao '%s' presente" % a)
		if node is MeshInstance3D and not is_preview:
			var mi := node as MeshInstance3D
			meshes += 1
			_ok(mi.mesh != null, path, "malha presente em '%s'" % root.get_path_to(mi))
			if mi.mesh == null:
				continue
			_ok(mi.mesh.resource_path.begins_with("res://Assets/V1/Meshes/"), path, "malha externa em Meshes/ ('%s')" % root.get_path_to(mi))
			_ok(mi.material_override == null, path, "sem material_override em '%s'" % root.get_path_to(mi))
			for s in mi.mesh.get_surface_count():
				var m := mi.mesh.surface_get_material(s)
				_ok(m != null, path, "superficie %d de '%s' tem material" % [s, root.get_path_to(mi)])
				if m == null:
					continue
				_ok(m.resource_path.begins_with("res://Assets/V1/Materials/"), path, "material compartilhado em Materials/ (%s)" % m.resource_path)
				if m is BaseMaterial3D:
					_ok(not (m as BaseMaterial3D).emission_enabled, path, "material aplicado nao emissivo (%s)" % m.resource_path)
				_ok(mi.get_surface_override_material(s) == null, path, "sem override de superficie em '%s'" % root.get_path_to(mi))
				var il: int = mi.mesh.surface_get_array_index_len(s)
				tris += (il if il > 0 else mi.mesh.surface_get_array_len(s)) / 3
	for lp in spec.get("empty_labels", []):
		var l := root.get_node_or_null(lp) as Label3D
		_ok(l != null and l.text == "", path, "Label3D '%s' existe e esta vazio (preenchido pela integracao com operador real)" % lp)
	var shared: Array = spec.get("shared_mesh", [])
	if shared.size() > 1:
		var first := root.get_node_or_null(shared[0]) as MeshInstance3D
		for s in shared:
			var mi2 := root.get_node_or_null(s) as MeshInstance3D
			_ok(first != null and mi2 != null and mi2.mesh == first.mesh, path, "'%s' compartilha a malha de '%s'" % [s, shared[0]])
	if not is_preview:
		print("[validate] %s ok-so-far | mesh instances=%d | tris instanciados=%d" % [path.get_file(), meshes, tris])
	else:
		print("[validate] %s (preview) carregada" % path.get_file())
	root.free()
