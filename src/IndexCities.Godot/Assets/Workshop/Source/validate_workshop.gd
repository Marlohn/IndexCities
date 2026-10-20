## Validacao do recurso nativo e do GLB, sem cidade ou save.
extends SceneTree

const PACKAGE := "res://Assets/Workshop"
const ANCHORS := ["PedestrianDoor","ServiceDoor","OpenBayThreshold","ClosedBayThreshold","VehicleApproach","VehiclePresentationAnchor","ActivitySign"]
var errors: Array = []
var records: Array = []
var current: Dictionary

func _initialize() -> void:
	var native: PackedScene = load(PACKAGE+"/Scenes/commercial.mechanic_shop.a.tscn")
	if native == null:
		printerr("cena nativa ausente")
		quit(1)
		return
	var a := native.instantiate()
	var palette_material: StandardMaterial3D = a.get_node("Site").mesh.surface_get_material(0)
	var saved_palette := palette_material.albedo_texture.get_image()
	var source_palette := Image.load_from_file(PACKAGE+"/Materials/palette.png")
	print("[workshop-validate] palette saved=",saved_palette.get_pixel(0,0)," source=",source_palette.get_pixel(0,0)," uv=",a.get_node("Site").mesh.surface_get_arrays(0)[Mesh.ARRAY_TEX_UV][0])
	if saved_palette.get_data() != source_palette.get_data():
		errors.append("paleta serializada diferente da fonte PNG")
	_inspect(a,"native")
	a.free()
	var doc := GLTFDocument.new()
	var state := GLTFState.new()
	if doc.append_from_file(PACKAGE+"/Scenes/commercial.mechanic_shop.a.glb",state) != OK:
		errors.append("reimportacao GLB")
	else:
		var imported := doc.generate_scene(state)
		if imported == null:
			errors.append("GLB nao gerou cena")
		else:
			_inspect(imported,"glb")
			imported.free()
	if records.size() == 2:
		if records[0].triangles != records[1].triangles:
			errors.append("triangulos diferem entre nativo e GLB")
	var report_path := PACKAGE+"/Preview/validation.json"
	var report := FileAccess.open(report_path,FileAccess.WRITE)
	report.store_string(JSON.stringify({"engine":Engine.get_version_info().string,"records":records,"errors":errors,"result":"PASS" if errors.is_empty() else "FAIL"},"\t"))
	for record in records:
		print("[workshop-validate] %s meshes=%d triangles=%d materials=%d markers=%d" % [record.format,record.meshes,record.triangles,record.materials.size(),record.markers.size()])
	for error in errors:
		printerr("[workshop-validate] ",error)
	print("[workshop-validate] ","PASS" if errors.is_empty() else "FAIL")
	quit(0 if errors.is_empty() else 1)

func _inspect(node: Node, label: String) -> void:
	current = {"format":label,"meshes":0,"triangles":0,"surfaces":0,"materials":[],"markers":[],"bounds_min":Vector3(INF,INF,INF),"bounds_max":Vector3(-INF,-INF,-INF)}
	_visit(node,Transform3D.IDENTITY)
	var mn: Vector3 = current.bounds_min
	var mx: Vector3 = current.bounds_max
	if mn.x < -11.01 or mn.z < -8.51 or mx.x > 11.01 or mx.z > 8.51:
		errors.append(label+": envelope excede lote candidato")
	if current.meshes != 11:
		errors.append(label+": numero de partes inesperado")
	if label == "native":
		for name in ["ShutterClosed","LiftFrame","LiftArms","ReplaceableActivityLabel"]:
			if node.get_node_or_null(name) == null:
				errors.append("parte editavel ausente: "+name)
		if node.get_node("ShutterClosed").position.distance_to(Vector3(-1.45,3.5,-0.99)) > 0.001:
			errors.append("pivo do portao")
	if current.markers.size() != 7:
		errors.append(label+": ancoras candidatas ausentes")
	current.bounds_min = [mn.x,mn.y,mn.z]
	current.bounds_max = [mx.x,mx.y,mx.z]
	records.append(current.duplicate(true))

func _visit(node: Node, parent_transform: Transform3D) -> void:
	var t := parent_transform
	if node is Node3D:
		t = parent_transform * node.transform
	if node.get_script() != null or node is Light3D or node is AnimationPlayer or node is CollisionObject3D:
		errors.append(current.format+": runtime inesperado "+str(node.name))
	# glTF preserva as ancoras como Node3D comuns; o nome e a transformacao
	# persistem, embora o tipo Marker3D seja especifico do Godot.
	if str(node.name) in ANCHORS:
		current.markers.append(str(node.name))
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
