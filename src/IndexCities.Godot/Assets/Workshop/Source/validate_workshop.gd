## Validacao da oficina (cena nativa e GLB), sem cidade nem save.
## godot --headless --path src/IndexCities.Godot --script res://Assets/Workshop/Source/validate_workshop.gd
extends SceneTree

const PACKAGE := "res://Assets/Workshop"
const SCENE := PACKAGE+"/Scenes/commercial.mechanic_shop.a.tscn"
const GLB := PACKAGE+"/Scenes/commercial.mechanic_shop.a.glb"
const CAR := "res://Assets/V1/Scenes/vehicle.car_passenger.a.tscn"
const TRUCK := "res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn"
const TOW := "res://Assets/Construction/Scenes/vehicle.tow_truck.a.tscn"
const LOT := Vector2(22,19)
const MAX_TRIANGLES := 13000
const ANCHORS := ["Entrance","WaitingSpot0","WaitingSpot1","ServiceBay1","TowTruckBase","BayThreshold0","BayThreshold1","ServiceDoor","ActivitySign"]
const PARTS := ["Site","Building","ArchitecturalDetails","Gate","WorkshopFurnishings","LiftFrame","LiftPlatform","YardProps","SignHousing","WrenchEmblem","ReplaceableActivityLabel"]
var errors: Array = []
var records: Array = []
var fits: Array = []
var current: Dictionary

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	var native: PackedScene = load(SCENE)
	if native == null:
		printerr("cena nativa ausente")
		quit(1)
		return
	var a := native.instantiate()
	root.add_child(a)
	var palette_material: StandardMaterial3D = a.get_node("Site").mesh.surface_get_material(0)
	var source_palette := Image.load_from_file(PACKAGE+"/Materials/palette.png")
	if palette_material.albedo_texture.get_image().get_data() != source_palette.get_data():
		errors.append("paleta serializada diferente da fonte PNG")
	_inspect(a,"native")
	_contract(a)
	_fits(a)
	a.queue_free()
	var doc := GLTFDocument.new()
	var state := GLTFState.new()
	if doc.append_from_file(GLB,state) != OK:
		errors.append("reimportacao GLB")
	else:
		var imported := doc.generate_scene(state)
		if imported == null:
			errors.append("GLB nao gerou cena")
		else:
			_inspect(imported,"glb")
			for n in ["LiftPlatform","Gate","Anchors"]:
				if imported.find_child(n,true,false) == null:
					errors.append("GLB sem "+n)
			imported.free()
	if records.size() == 2 and records[0].triangles != records[1].triangles:
		errors.append("triangulos diferem entre nativo e GLB")
	var report := FileAccess.open(PACKAGE+"/Preview/validation.json",FileAccess.WRITE)
	report.store_string(JSON.stringify({"engine":Engine.get_version_info().string,"records":records,"vehicle_fits":fits,"errors":errors,"result":"PASS" if errors.is_empty() else "FAIL"},"\t"))
	for record in records:
		print("[workshop-validate] %s meshes=%d triangles=%d materials=%d" % [record.format,record.meshes,record.triangles,record.materials.size()])
	for error in errors:
		printerr("[workshop-validate] ",error)
	print("[workshop-validate] ","PASS" if errors.is_empty() else "FAIL")
	quit(0 if errors.is_empty() else 1)

func _expect(ok: bool, msg: String) -> void:
	if not ok:
		errors.append(msg)

func _contract(shop: Node3D) -> void:
	for n in PARTS:
		_expect(shop.get_node_or_null(n) != null,"parte ausente: "+n)
	_expect(shop.get_node("Gate").position.distance_to(Vector3(-1.45,3.5,-0.99+1.0)) < 0.001,"pivo do portao")
	for n in ANCHORS:
		_expect(shop.get_node_or_null("Anchors/"+n) is Marker3D,"ancora ausente: "+n)
	_expect(shop.get_node_or_null("LiftPlatform/ServiceBay0") is Marker3D,"ServiceBay0 sob LiftPlatform")
	_expect(shop.get_node_or_null("LiftPlatform/Carriage") is MeshInstance3D,"malha do elevador")
	var lift: AnimationPlayer = shop.get_node_or_null("LiftPlatform/AnimationPlayer")
	var gate: AnimationPlayer = shop.get_node_or_null("Gate/AnimationPlayer")
	_expect(lift != null and lift.has_animation("lift_up") and lift.has_animation("lift_down") and lift.autoplay == "","lift_up/lift_down sem autoplay")
	_expect(gate != null and gate.has_animation("gate_open") and gate.has_animation("gate_close") and gate.autoplay == "","gate_open/gate_close sem autoplay")
	if lift != null and lift.has_animation("lift_up"):
		var up := lift.get_animation("lift_up")
		var rest: Vector3 = shop.get_node("LiftPlatform").position
		var end: Vector3 = up.track_get_key_value(0,up.track_get_key_count(0)-1)
		_expect(up.track_get_key_value(0,0).distance_to(rest) < 0.001,"lift_up comeca na pose baixa")
		_expect(absf(end.y-rest.y-1.4) < 0.001 and absf(end.x-rest.x) < 0.001 and absf(end.z-rest.z) < 0.001,"lift_up sobe 1,4 m na vertical")
	if gate != null and gate.has_animation("gate_open"):
		var open := gate.get_animation("gate_open")
		var scale: Vector3 = open.track_get_key_value(0,open.track_get_key_count(0)-1)
		_expect(scale.y < 0.1,"gate_open recolhe a cortina")
	var base: Marker3D = shop.get_node("Anchors/TowTruckBase")
	_expect((-base.global_transform.basis.z).distance_to(Vector3.LEFT) < 0.001,"TowTruckBase com nariz para -X")
	_expect(shop.get_node("Anchors/Entrance").global_position.z < 0.0,"Entrance na fachada -Z")
	_expect(shop.get_node("Anchors/ServiceBay1").global_position.x > shop.get_node("LiftPlatform/ServiceBay0").global_position.x,"baias distintas")

func _meshes(node: Node, out: Array) -> void:
	if node is MeshInstance3D and node.mesh != null and node.is_visible_in_tree():
		out.append(node)
	for c in node.get_children():
		_meshes(c,out)

func _triangles_of(mi: MeshInstance3D, reference: Node3D, skip_floor: bool) -> Array:
	var result: Array = []
	var t := reference.global_transform.affine_inverse()*mi.global_transform
	for s in mi.mesh.get_surface_count():
		var arrays := mi.mesh.surface_get_arrays(s)
		var vertices: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
		var indices = arrays[Mesh.ARRAY_INDEX]
		var indexed: bool = indices != null and indices.size() > 0
		for i in range(0,indices.size() if indexed else vertices.size(),3):
			var pts: Array = []
			for k in 3:
				pts.append(t*vertices[indices[i+k] if indexed else i+k])
			var box := AABB(pts[0],Vector3.ZERO).expand(pts[1]).expand(pts[2])
			if skip_floor and box.end.y < 0.03:
				continue
			result.append(box)
	return result

## Veiculos do jogo nas ancoras: nao podem atravessar a oficina nem sair do lote.
func _fits(shop: Node3D) -> void:
	var shop_boxes: Array = []
	var shop_meshes: Array = []
	_meshes(shop,shop_meshes)
	for mi in shop_meshes:
		if mi.name == "Site" or mi.name == "ReplaceableActivityLabel":
			continue
		shop_boxes.append_array(_triangles_of(mi,shop,true))
	var cases := [
		{"name":"car@ServiceBay0 baixo","scene":CAR,"parent":"LiftPlatform/ServiceBay0","lift":false},
		{"name":"car@ServiceBay0 elevado","scene":CAR,"parent":"LiftPlatform/ServiceBay0","lift":true},
		{"name":"car@ServiceBay1","scene":CAR,"parent":"Anchors/ServiceBay1"},
		{"name":"truck@ServiceBay1","scene":TRUCK,"parent":"Anchors/ServiceBay1"},
		{"name":"tow@TowTruckBase","scene":TOW,"parent":"Anchors/TowTruckBase","tow_margin":0.6},
	]
	for item in cases:
		var vehicle: Node3D = (load(item.scene) as PackedScene).instantiate()
		var anchor: Node3D = shop.get_node(item.parent)
		var platform: Node3D = shop.get_node("LiftPlatform")
		var rest := platform.position
		if item.get("lift",false):
			platform.position = rest+Vector3(0,1.4,0)
		anchor.add_child(vehicle)
		var hood := vehicle.get_node_or_null("Hood")
		if hood != null and item.name.begins_with("car@ServiceBay0"):
			hood.rotation = Vector3(deg_to_rad(62.0),0,0)
		var meshes: Array = []
		_meshes(vehicle,meshes)
		var vehicle_box := AABB()
		var first := true
		var margin: float = item.get("tow_margin",0.02)
		for mi in meshes:
			for b in _triangles_of(mi,shop,false):
				vehicle_box = b if first else vehicle_box.merge(b)
				first = false
		var inside_lot: bool = vehicle_box.position.x >= -LOT.x/2.0 and vehicle_box.end.x+margin <= LOT.x/2.0 and vehicle_box.position.z >= -LOT.y/2.0 and vehicle_box.end.z <= LOT.y/2.0
		_expect(inside_lot,item.name+": fora do lote")
		var hits := 0
		var grown := vehicle_box.grow(0.0)
		for mi in meshes:
			if mi.name == "Hood" or mi.name.begins_with("Wheel"):
				continue
			for b in _triangles_of(mi,shop,false):
				for sb in shop_boxes:
					if b.intersects(sb) and b.intersection(sb).get_volume() > 0.0004 and sb.size.length() < 6.0 and mi.name == "Body":
						hits += 1
						break
				if hits > 0:
					break
		_expect(hits == 0,item.name+": corpo do veiculo atravessa a oficina")
		fits.append({"case":item.name,"vehicle_aabb_min":[vehicle_box.position.x,vehicle_box.position.y,vehicle_box.position.z],"vehicle_aabb_max":[vehicle_box.end.x,vehicle_box.end.y,vehicle_box.end.z],"inside_lot":inside_lot,"intersections":hits})
		vehicle.free()
		platform.position = rest
		grown = grown

func _inspect(node: Node, label: String) -> void:
	current = {"format":label,"meshes":0,"triangles":0,"surfaces":0,"materials":[],"bounds_min":Vector3(INF,INF,INF),"bounds_max":Vector3(-INF,-INF,-INF)}
	_visit(node,Transform3D.IDENTITY)
	var mn: Vector3 = current.bounds_min
	var mx: Vector3 = current.bounds_max
	if mn.x < -11.01 or mn.z < -9.51 or mx.x > 11.01 or mx.z > 9.51:
		errors.append(label+": envelope excede o lote proposto")
	if current.meshes != 11:
		errors.append(label+": numero de partes inesperado "+str(current.meshes))
	if current.triangles > MAX_TRIANGLES:
		errors.append(label+": orcamento de triangulos excedido "+str(current.triangles))
	if current.materials.size() > 5:
		errors.append(label+": mais de 5 materiais")
	current.bounds_min = [mn.x,mn.y,mn.z]
	current.bounds_max = [mx.x,mx.y,mx.z]
	records.append(current.duplicate(true))

func _visit(node: Node, parent_transform: Transform3D) -> void:
	var t := parent_transform
	if node is Node3D:
		t = parent_transform * node.transform
	if node.get_script() != null or node is Light3D or node is CollisionObject3D or node is GPUParticles3D:
		errors.append(current.format+": runtime inesperado "+str(node.name))
	if node is MeshInstance3D:
		current.meshes += 1
		var mesh: Mesh = node.mesh
		if mesh == null:
			errors.append("malha ausente "+str(node.name))
			return
		var bounds: AABB = t * mesh.get_aabb()
		current.bounds_min = current.bounds_min.min(bounds.position)
		current.bounds_max = current.bounds_max.max(bounds.end)
		for surface in mesh.get_surface_count():
			current.surfaces += 1
			var material: Material = mesh.surface_get_material(surface)
			if material == null:
				errors.append("material ausente "+str(node.name))
			elif not current.materials.has(material.resource_name):
				current.materials.append(material.resource_name)
			var arrays := mesh.surface_get_arrays(surface)
			var vertices: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
			var normals: PackedVector3Array = arrays[Mesh.ARRAY_NORMAL]
			var indices = arrays[Mesh.ARRAY_INDEX]
			current.triangles += (indices.size() if indices != null and indices.size()>0 else vertices.size())/3
			for i in vertices.size():
				if not vertices[i].is_finite() or not normals[i].is_finite() or absf(normals[i].length()-1.0)>0.02:
					errors.append("vertice/normal invalido "+str(node.name))
					break
	for child in node.get_children():
		_visit(child,t)
