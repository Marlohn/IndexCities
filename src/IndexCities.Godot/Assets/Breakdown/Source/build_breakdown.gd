## Props genericos de quebra de veiculo (SOMENTE APRESENTACAO). Ferramenta de autoria;
## nunca e carregada pelo jogo.
##
##   godot --headless --path src/IndexCities.Godot --script res://Assets/Breakdown/Source/build_breakdown.gd
##
## Gera warning_triangle, breakdown_smoke e hazard_lights, os materiais proprios e o
## manifesto. Quem coloca, liga e remove os props e a integracao, a partir do estado real
## do veiculo; nada aqui decide quando um veiculo quebra. Geometria original.
extends SceneTree

const ROOT := "res://Assets/Breakdown"
const Builder := preload("res://Assets/V1/Source/kit/mesh_builder.gd")
const Palette := preload("res://Assets/V1/Source/kit/palette.gd")
const Rig := preload("res://Assets/V1/Source/kit/breakdown_rig.gd")
const STATUS := "PENDENTE de revisao humana; sem regra de produto"

const MATERIALS := {
	"breakdown_red": {"albedo": "#c0322a", "roughness": 0.45},
	"breakdown_amber_off": {"albedo": "#9a6a1c", "roughness": 0.3},
	"breakdown_smoke": {"albedo": "#7d7f84", "roughness": 1.0},
	"breakdown_amber_on": {"albedo": "#ffb43a", "roughness": 0.3, "emission": "#ff9d1c", "emission_energy": 2.6},
}

var entries: Array = []
var cache: Dictionary = {}
var failures: PackedStringArray = PackedStringArray()


func _initialize() -> void:
	call_deferred("build_all")


func fail(msg: String) -> void:
	failures.append(msg)
	push_error("[breakdown-build] " + msg)


func builder():
	return Builder.new()


func mat(slot: String) -> Material:
	if not cache.has(slot):
		var folder: String = ROOT if MATERIALS.has(slot) else "res://Assets/V1"
		cache[slot] = load("%s/Materials/%s.tres" % [folder, slot])
		if cache[slot] == null:
			fail("Material ausente " + slot)
	return cache[slot]


func save_mesh(b, id: String) -> Mesh:
	var m: Mesh = b.commit(Callable(self, "mat"))
	m.resource_name = id
	var path := ROOT + "/Meshes/" + id + ".tres"
	if ResourceSaver.save(m, path) != OK:
		fail("Falha ao salvar " + path)
	return ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REPLACE)


func mesh_node(parent: Node3D, node_name: String, mesh: Mesh, pos: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var n := MeshInstance3D.new()
	n.name = node_name
	n.mesh = mesh
	n.position = pos
	parent.add_child(n)
	return n


func triangles(mesh: Mesh) -> int:
	var t := 0
	for s in mesh.get_surface_count():
		var il: int = mesh.surface_get_array_index_len(s)
		t += (il if il > 0 else mesh.surface_get_array_len(s)) / 3
	return t


func own(n: Node, r: Node) -> void:
	for c in n.get_children():
		c.owner = r
		own(c, r)


func save_scene(r: Node3D, id: String, contract: Dictionary) -> void:
	r.set_meta("visual_id", id)
	r.set_meta("asset_status", STATUS)
	var tris := 0
	var mesh_nodes: Array = []
	var box := AABB()
	var first := true
	var stack: Array = [[r, Transform3D.IDENTITY]]
	while not stack.is_empty():
		var item: Array = stack.pop_back()
		var n: Node = item[0]
		var t: Transform3D = item[1]
		if n is Node3D and n != r:
			t = t * (n as Node3D).transform
		if n is MeshInstance3D and (n as MeshInstance3D).mesh != null:
			var mi := n as MeshInstance3D
			var b: AABB = t * mi.mesh.get_aabb()
			box = b if first else box.merge(b)
			first = false
			tris += triangles(mi.mesh)
			mesh_nodes.append({"node": str(r.get_path_to(n)), "mesh": mi.mesh.resource_path, "triangles": triangles(mi.mesh), "visible_by_default": mi.visible})
		for c in n.get_children():
			stack.append([c, t])
	r.set_meta("visual_envelope_min", box.position)
	r.set_meta("visual_envelope_size", box.size)
	own(r, r)
	var ps := PackedScene.new()
	if ps.pack(r) != OK:
		fail("Falha ao empacotar " + id)
	var path := ROOT + "/Scenes/" + id + ".tscn"
	if ResourceSaver.save(ps, path) != OK:
		fail("Falha ao salvar " + path)
	entries.append({
		"visual_id": id,
		"scene": path,
		"root_node": str(r.name),
		"envelope_m": {"min": vec(box.position), "max": vec(box.end), "size": vec(box.size)},
		"triangles": tris,
		"meshes": mesh_nodes,
		"contract": contract,
	})
	print("[breakdown-build] %s triangles=%d envelope=%s" % [id, tris, str(box.size)])
	r.free()


func vec(p: Vector3) -> Array:
	return [snappedf(p.x, 0.0001), snappedf(p.y, 0.0001), snappedf(p.z, 0.0001)]


func build_all() -> void:
	for folder in ["Scenes", "Meshes", "Materials", "Preview"]:
		DirAccess.make_dir_recursive_absolute(ROOT + "/" + folder)
	for slot in MATERIALS:
		var m := Palette.make_material(MATERIALS[slot])
		m.resource_name = slot
		if ResourceSaver.save(m, ROOT + "/Materials/" + slot + ".tres") != OK:
			fail("Material " + slot)
	var smoke_material := Palette.make_material({"albedo": "#7d7f84", "roughness": 1.0})
	smoke_material.resource_name = "breakdown_smoke"
	smoke_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	smoke_material.vertex_color_use_as_albedo = true
	smoke_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	smoke_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	if ResourceSaver.save(smoke_material, ROOT + "/Materials/breakdown_smoke.tres") != OK:
		fail("Material breakdown_smoke")
	build_triangle()
	build_smoke()
	build_hazard()
	var f := FileAccess.open(ROOT + "/manifest.breakdown.json", FileAccess.WRITE)
	f.store_string(JSON.stringify({
		"schema": "indexcities.breakdown/1",
		"status": STATUS,
		"authorship": "geometria e fonte originais escritas para IndexCities (receita em Source/build_breakdown.gd)",
		"conventions": {"up": "+Y", "unit": "meter", "front": "-Z", "pivot": "ground, center", "runtime_scripts": false,
			"note": "Props de apresentacao. A integracao instancia, posiciona e remove a partir do estado real do veiculo."},
		"entries": entries,
	}, "  "))
	f.close()
	if failures.is_empty():
		print("[breakdown-build] OK %d props" % entries.size())
		quit(0)
	else:
		quit(1)


# ---------------------------------------------------------------------------
# warning_triangle
# ---------------------------------------------------------------------------

## Prisma triangular (placa) entre z0 e z1; `pts` em (x, y).
func prism(b, slot: String, pts: Array, z0: float, z1: float) -> void:
	var front: Array = []
	var back: Array = []
	for p in pts:
		front.append(Vector3(p.x, p.y, z1))
		back.append(Vector3(p.x, p.y, z0))
	b.face(slot, front, Vector3(0, 0, 1))
	b.face(slot, back, Vector3(0, 0, -1))
	for i in pts.size():
		var j := (i + 1) % pts.size()
		var mid: Vector3 = (front[i] + front[j]) * 0.5
		var center := Vector3(0, 0, (z0 + z1) * 0.5)
		for q in pts:
			center += Vector3(q.x, q.y, 0) / float(pts.size())
		b.face(slot, [back[i], back[j], front[j], front[i]], mid - center)


func triangle_points(side: float, base_y: float, inset: float = 0.0) -> Array:
	# Triangulo equilatero com a base em y = base_y; `inset` encolhe para dentro.
	var h := side * sqrt(3.0) / 2.0
	var centroid := Vector2(0, base_y + h / 3.0)
	var corners := [Vector2(-side / 2.0, base_y), Vector2(side / 2.0, base_y), Vector2(0, base_y + h)]
	var out: Array = []
	for c in corners:
		var d: Vector2 = c - centroid
		out.append(centroid + d * (1.0 - inset / (h * 2.0 / 3.0)))
	return out


func build_triangle() -> void:
	var r := Node3D.new()
	r.name = "WarningTriangle"
	var b = builder()
	var side := 0.64
	var base := 0.05
	# Moldura vermelha, miolo branco e pes. Placa de duas faces (z -0.015..0.015).
	prism(b, "breakdown_red", triangle_points(side, base), -0.015, 0.015)
	prism(b, "trim_white", triangle_points(side, base, 0.115), -0.0185, 0.0185)
	prism(b, "breakdown_red", triangle_points(side, base, 0.205), -0.022, 0.022)
	prism(b, "trim_white", triangle_points(side, base, 0.245), -0.0245, 0.0245)
	for sx in [-1.0, 1.0]:
		b.bevel_box("metal_dark", Vector3(sx * 0.255 - 0.035, 0.0, -0.09), Vector3(sx * 0.255 + 0.035, 0.05, 0.09), 0.012)
	var m := save_mesh(b, "prop.warning_triangle.a")
	mesh_node(r, "Body", m)
	save_scene(r, "prop.warning_triangle.a", {
		"nodes": ["Body"],
		"pivot": "centro da base no solo; frente da placa (face principal) em +Z, igual atras ao veiculo; as duas faces sao iguais",
		"placement": "a integracao posiciona atras do veiculo (+Z do veiculo) e vira a face para o transito que se aproxima; o asset nao define a distancia",
		"animation": "nenhuma",
	})


# ---------------------------------------------------------------------------
# breakdown_smoke
# ---------------------------------------------------------------------------

func build_smoke() -> void:
	var r := Node3D.new()
	r.name = "BreakdownSmoke"
	var b = builder()
	# Uma "nuvem" facetada de 20 faces; vira varias via CPUParticles3D.
	b.icosphere("breakdown_smoke", Vector3.ZERO, 0.3, 0, 0.05, 11, Vector3(1.0, 0.85, 1.0))
	var puff := save_mesh(b, "prop.breakdown_smoke.a.puff")
	var p := CPUParticles3D.new()
	p.name = "Smoke"
	p.mesh = puff
	p.emitting = true
	p.amount = 16
	p.lifetime = 2.6
	p.lifetime_randomness = 0.3
	p.fixed_fps = 30
	p.local_coords = false
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_SPHERE
	p.emission_sphere_radius = 0.12
	p.direction = Vector3(0, 1, 0)
	p.spread = 14.0
	p.initial_velocity_min = 0.55
	p.initial_velocity_max = 0.95
	p.gravity = Vector3(0.12, 0.25, 0.0)
	p.damping_min = 0.15
	p.damping_max = 0.3
	p.angle_min = -180.0
	p.angle_max = 180.0
	p.angular_velocity_min = -20.0
	p.angular_velocity_max = 20.0
	var grow := Curve.new()
	grow.add_point(Vector2(0, 0.35))
	grow.add_point(Vector2(1, 1.7))
	p.scale_amount_curve = grow
	var fade := Gradient.new()
	fade.offsets = PackedFloat32Array([0.0, 0.18, 1.0])
	fade.colors = PackedColorArray([Color(0.2, 0.2, 0.22, 0.0), Color(0.24, 0.24, 0.27, 0.8), Color(0.42, 0.42, 0.45, 0.0)])
	p.color_ramp = fade
	r.add_child(p)
	# Sem AABB declarado: o motor calcula; o caminho de emissao e curto (~2,5 m de altura).
	save_scene(r, "prop.breakdown_smoke.a", {
		"nodes": ["Smoke (CPUParticles3D, 16 particulas, malha de 20 faces, sem textura)"],
		"pivot": "origem da emissao; a integracao encaixa em HoodSmoke do veiculo (mesma orientacao)",
		"activity": "emitting = true ao entrar na arvore; `Smoke.emitting = false` para parar; remover o no encerra o efeito",
		"cost": "16 particulas de 20 triangulos (320 triangulos vivos), material sem sombreamento",
		"animation": "nenhuma",
	})


# ---------------------------------------------------------------------------
# hazard_lights
# ---------------------------------------------------------------------------

func lens_housing() -> Mesh:
	var b = builder()
	b.bevel_box("breakdown_amber_off", Vector3(-0.17, -0.085, -0.035), Vector3(0.17, 0.085, 0.035), 0.02)
	return save_mesh(b, "prop.hazard_lights.a.lens")


func lens_glow() -> Mesh:
	var b = builder()
	b.bevel_box("breakdown_amber_on", Vector3(-0.14, -0.058, -0.05), Vector3(0.14, 0.058, -0.015), 0.02)
	return save_mesh(b, "prop.hazard_lights.a.glow")


func build_hazard() -> void:
	var r := Node3D.new()
	r.name = "HazardLights"
	var housing := lens_housing()
	var glow := lens_glow()
	# Posicao nominal (carro compacto): a integracao copia o transform de HazardFL/FR/RL/RR
	# do veiculo para cada LightXX. Frente olha para -Z, traseira para +Z.
	var nominal := {
		"LightFL": Vector3(-0.80, 0.80, -1.99), "LightFR": Vector3(0.80, 0.80, -1.99),
		"LightRL": Vector3(-0.80, 0.88, 2.00), "LightRR": Vector3(0.80, 0.88, 2.00),
	}
	for light_name in nominal:
		var holder := Node3D.new()
		holder.name = light_name
		holder.position = nominal[light_name]
		if light_name.begins_with("LightR"):
			holder.rotation.y = PI
		r.add_child(holder)
		mesh_node(holder, "Lens", housing)
		mesh_node(holder, "Glow", glow)
	var a := Animation.new()
	a.length = 0.8
	a.loop_mode = Animation.LOOP_LINEAR
	for light_name in nominal:
		var t := a.add_track(Animation.TYPE_VALUE)
		a.track_set_path(t, NodePath("%s/Glow:visible" % light_name))
		a.value_track_set_update_mode(t, Animation.UPDATE_DISCRETE)
		a.track_set_interpolation_type(t, Animation.INTERPOLATION_NEAREST)
		a.track_insert_key(t, 0.0, true)
		a.track_insert_key(t, 0.4, false)
	var lib := AnimationLibrary.new()
	lib.add_animation("blink", a)
	var ap := AnimationPlayer.new()
	ap.name = "AnimationPlayer"
	ap.add_animation_library("", lib)
	ap.autoplay = "blink"
	r.add_child(ap)
	save_scene(r, "prop.hazard_lights.a", {
		"nodes": ["LightFL", "LightFR", "LightRL", "LightRR", "<cada uma>/Lens (carcaca ambar apagada)", "<cada uma>/Glow (lente ambar emissiva, alterna visivel)", "AnimationPlayer"],
		"animation": "blink: 0,8 s em loop; todas as lentes acendem 0,0-0,4 s e apagam 0,4-0,8 s; autoplay = blink",
		"placement": "ao instanciar, copiar o transform global de HazardFL/FR/RL/RR do veiculo para LightFL/FR/RL/RR; -Z local de cada Light e a direcao em que a lente olha",
		"cost": "4 lentes de 2 caixas chanfradas; sem Light3D",
	})
