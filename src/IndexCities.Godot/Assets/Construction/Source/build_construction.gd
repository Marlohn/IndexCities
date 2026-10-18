## Original editable geometry recipes. Tool only; never attached to delivered scenes.
extends SceneTree

const ROOT = "res://Assets/Construction"
const Builder = preload("res://Assets/V1/Source/kit/mesh_builder.gd")
const Truck = preload("res://Assets/V1/Source/recipes/vehicle_truck_cargo.gd")
const Palette = preload("res://Assets/V1/Source/kit/palette.gd")
const MATERIALS = {
	"construction_ochre": {"albedo":"#cf9b3e", "roughness":0.66},
	"construction_ochre_dark": {"albedo":"#986c2c", "roughness":0.72},
	"construction_sand": {"albedo":"#b9a27c", "roughness":0.97},
	"construction_gravel": {"albedo":"#8f8c86", "roughness":0.98},
	"construction_asphalt": {"albedo":"#30363b", "roughness":0.98},
	"construction_tarp": {"albedo":"#415457", "roughness":0.9},
	"construction_wood": {"albedo":"#b58c5d", "roughness":0.86},
	"construction_wood_end": {"albedo":"#d0ac7c", "roughness":0.89},
}
var entries: Array = []
var cached: Dictionary = {}
var truck = Truck.new()

func _initialize() -> void:
	call_deferred("build_all")

func builder():
	return Builder.new()

func mat(slot: String) -> Material:
	if not cached.has(slot):
		var folder = ROOT if MATERIALS.has(slot) else "res://Assets/V1"
		cached[slot] = load("%s/Materials/%s.tres" % [folder, slot])
		assert(cached[slot] != null, "Missing material " + slot)
	return cached[slot]

func mesh(b, id: String) -> Mesh:
	var m = b.commit(Callable(self, "mat"))
	m.resource_name = id
	var path = ROOT + "/Meshes/" + id + ".tres"
	assert(ResourceSaver.save(m, path) == OK)
	return ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REPLACE)

func node(parent: Node3D, title: String, pos = Vector3.ZERO) -> Node3D:
	var n = Node3D.new()
	n.name = title
	n.position = pos
	parent.add_child(n)
	return n

func part(parent: Node3D, title: String, b, id: String, pos = Vector3.ZERO) -> MeshInstance3D:
	var n = MeshInstance3D.new()
	n.name = title
	n.mesh = mesh(b, id)
	n.position = pos
	parent.add_child(n)
	return n

func marker(parent: Node3D, title: String, pos: Vector3) -> void:
	var n = Marker3D.new()
	n.name = title
	n.position = pos
	parent.add_child(n)

func own(n: Node, r: Node) -> void:
	for c in n.get_children():
		c.owner = r
		own(c, r)

func v(p: Vector3) -> Array:
	return [snapped(p.x,0.0001),snapped(p.y,0.0001),snapped(p.z,0.0001)]

func inspect(n: Node3D, t: Transform3D, r: Node3D, data: Dictionary) -> void:
	t = t * n.transform
	if n is MeshInstance3D:
		var box = exact_bounds(n.mesh,t)
		data.aabb = data.aabb.merge(box) if data.has("aabb") else box
		var triangles = 0
		for s in n.mesh.get_surface_count():
			var count = n.mesh.surface_get_array_index_len(s)
			triangles += (count if count > 0 else n.mesh.surface_get_array_len(s)) / 3
		data.meshes.append({"node":str(r.get_path_to(n)), "mesh":n.mesh.resource_path,"triangles":triangles,"surfaces":n.mesh.get_surface_count()})
		data.triangles += triangles
		data.surfaces += n.mesh.get_surface_count()
	for c in n.get_children():
		if c is Node3D:
			inspect(c,t,r,data)

func exact_bounds(m: Mesh, t: Transform3D) -> AABB:
	var box: AABB
	var first=true
	for s in m.get_surface_count():
		for point in m.surface_get_arrays(s)[Mesh.ARRAY_VERTEX]:
			var p=t*point
			box=AABB(p,Vector3.ZERO) if first else box.expand(p)
			first=false
	return box

func save(r: Node3D, id: String, contract: Dictionary) -> void:
	r.name = id.replace("vehicle.", "").replace(".a", "").to_pascal_case()
	r.set_meta("visual_id",id)
	r.set_meta("asset_status","PENDENTE de revisao humana")
	var d = {"meshes":[],"triangles":0,"surfaces":0}
	inspect(r,Transform3D.IDENTITY,r,d)
	r.set_meta("visual_envelope_min",d.aabb.position)
	r.set_meta("visual_envelope_size",d.aabb.size)
	own(r,r)
	var ps = PackedScene.new()
	assert(ps.pack(r) == OK)
	var path = ROOT + "/Scenes/" + id + ".tscn"
	assert(ResourceSaver.save(ps,path) == OK)
	entries.append({"visual_id":id,"scene":path,"envelope_all_states_rest":{"min":v(d.aabb.position),"max":v(d.aabb.end),"size":v(d.aabb.size)},"mesh_instances":d.meshes.size(),"triangles_all_states":d.triangles,"surfaces_all_states":d.surfaces,"meshes":d.meshes,"contract":contract})
	print("[construction-build] %s triangles=%d surfaces=%d instances=%d envelope=%s" % [id,d.triangles,d.surfaces,d.meshes.size(),str(d.aabb.size)])
	r.free()

func build_all() -> void:
	for folder in ["Scenes","Meshes","Materials","Preview"]:
		DirAccess.make_dir_recursive_absolute(ROOT + "/" + folder)
	for slot in MATERIALS:
		var m = Palette.make_material(MATERIALS[slot])
		m.resource_name = slot
		assert(ResourceSaver.save(m, ROOT + "/Materials/" + slot + ".tres") == OK)
	build_dump()
	build_mixer()
	build_flatbed()
	build_tow_truck()
	build_tow_truck_heavy()
	build_paver()
	build_roller()
	var f = FileAccess.open(ROOT + "/manifest.construction.json",FileAccess.WRITE)
	f.store_string(JSON.stringify({"schema":"indexcities.construction/1","authorship":"GPT-6.1-Sol xhigh; original recipes with current project V1 kit/palette","status":"Estetica PENDENTE de revisao humana","conventions":{"front":"-Z","up":"+Y","unit":"meter","pivot":"ground, center","runtime_scripts":false,"autoplay":false,"loads_visible_by_default":false},"entries":entries},"  "))
	f.close()
	print("[construction-build] PASS 7 scenes; original editable sources; no runtime scripts")
	quit(0)

func truck_base(id: String) -> Node3D:
	var r = Node3D.new()
	var b = builder()
	# Same project's cab recipe, so door glass, mirrors and bevels match the current fleet.
	truck._cab(b)
	for x in [-0.49,0.49]:
		b.box_c("metal_dark",Vector3(x,0.77,0),Vector3(0.15,0.24,6.8))
	for z in [-2.55,1.45,2.65]:
		b.cylinder_x("metal_dark",Vector3(0,0.47,z),0.085,2.05,8)
	for side in [-1.0,1.0]:
		b.bevel_box("metal_light",Vector3(side*0.9-0.22,0.54,-1.3),Vector3(side*0.9+0.22,1.05,-0.35),0.06)
		b.box_c("metal_dark",Vector3(side*1.03,1.06,2.04),Vector3(0.43,0.14,2.26))
		b.box_c("rubber",Vector3(side*1.03,0.63,3.15),Vector3(0.4,0.68,0.06))
		for y in [0.5,0.78]:
			b.box_c("metal_light",Vector3(side*1.13,y,0.4),Vector3(0.08,0.08,1.22))
		b.box_c("truck_light_rear_off",Vector3(side*0.91,0.95,3.46),Vector3(0.35,0.18,0.06))
	b.bevel_box("metal_dark",Vector3(-1.1,0.4,3.33),Vector3(1.1,0.56,3.6),0.035)
	part(r,"Body",b,id+".body")
	var wm = mesh(truck._wheel(self),"vehicle.construction_truck.wheel")
	for axle in [{"letter":"F","z":-2.55},{"letter":"M","z":1.45},{"letter":"R","z":2.65}]:
		for side in [{"letter":"L","x":-1.04},{"letter":"R","x":1.04}]:
			var w = MeshInstance3D.new()
			w.name = "Wheel_"+axle.letter+side.letter
			w.mesh = wm
			w.position = Vector3(side.x,0.47,axle.z)
			r.add_child(w)
	marker(r,"Candidate_Discharge",Vector3(0,0,3.9))
	return r

func wheel_contract() -> Dictionary:
	return {"nodes":["Wheel_FL","Wheel_FR","Wheel_ML","Wheel_MR","Wheel_RL","Wheel_RR"],"roll_axis":"X","steer_front_axis":"Y","radius_m":0.47,"forward_roll":"negative radians = -distance / radius"}

func shell_sides(b, slot: String, x_bottom: float, x_top: float, y0: float, y1: float, z0: float, z1: float, thickness: float) -> void:
	for sx in [-1.0,1.0]:
		var a = Vector3(sx*x_bottom,y0,z0)
		var c = Vector3(sx*x_top,y1,z0)
		var d = Vector3(sx*x_top,y1,z1)
		var e = Vector3(sx*x_bottom,y0,z1)
		b.face(slot,[a,c,d,e],Vector3(sx,0,0))
		var shift = Vector3(-sx*thickness,0,0)
		b.face(slot,[a+shift,e+shift,d+shift,c+shift],Vector3(-sx,0,0))
		b.face(slot,[c,c+shift,d+shift,d],Vector3.UP)
		b.face(slot,[a,e,e+shift,a+shift],Vector3.DOWN)
		for zz in [z0,z1]:
			b.face(slot,[Vector3(sx*x_bottom,y0,zz),Vector3(sx*x_top,y1,zz),Vector3(sx*x_top,y1,zz)+shift,Vector3(sx*x_bottom,y0,zz)+shift],Vector3(0,0,-1 if zz==z0 else 1))

func build_dump() -> void:
	var id = "vehicle.truck_dump.a"
	var r = truck_base(id)
	var pivot = node(r,"BedPivot",Vector3(0,1.18,2.9))
	var b = builder()
	b.push_translate(-pivot.position)
	b.bevel_box("construction_ochre_dark",Vector3(-1.02,1.13,-1.3),Vector3(1.02,1.28,3.25),0.035)
	shell_sides(b,"construction_ochre",1.02,1.2,1.26,2.24,-1.3,3.22,0.09)
	b.bevel_box("construction_ochre",Vector3(-1.2,1.26,-1.38),Vector3(1.2,2.42,-1.25),0.035)
	for side in [-1.0,1.0]:
		b.box_c("construction_ochre_dark",Vector3(side*1.2,2.24,0.98),Vector3(0.11,0.12,4.65))
		for z in [-0.7,0.35,1.4,2.45]:
			b.beam("construction_ochre_dark",Vector3(side*1.05,1.3,z),Vector3(side*1.235,2.2,z),0.09)
	# Short headboard over cab rear; visibly a tipper, no closed-box silhouette.
	b.bevel_box("construction_ochre",Vector3(-1.19,2.34,-1.62),Vector3(1.19,2.44,-1.23),0.025)
	b.pop()
	part(pivot,"Bed",b,id+".bed")
	var gate = node(pivot,"TailgatePivot",Vector3(0,1.04,0.34))
	b = builder()
	b.bevel_box("construction_ochre",Vector3(-1.16,-0.92,-0.035),Vector3(1.16,0.02,0.075),0.035)
	for x in [-0.74,0,0.74]:
		b.box_c("construction_ochre_dark",Vector3(x,-0.44,0.095),Vector3(0.08,0.82,0.06))
	part(gate,"Tailgate",b,id+".tailgate")
	for spec in [{"name":"LoadSandGravel","slot":"construction_sand"},{"name":"LoadAsphalt","slot":"construction_asphalt"}]:
		b = builder()
		b.push_translate(-pivot.position-Vector3(0,0.14,0))
		mound(b,spec.slot,1.075,-1.15,3.09,1.32,2.16,0.23)
		if spec.name == "LoadSandGravel":
			for i in 18:
				var x = sin(i*7.3)*0.82
				var z = -0.95+fmod(i*1.37,3.85)
				var y = 2.14+(1.0-abs(x))*0.18
				b.bevel_box("construction_gravel",Vector3(x-0.08,y-0.07,z-0.10),Vector3(x+0.08,y+0.07,z+0.10),0.04)
		b.pop()
		part(pivot,spec.name,b,id+"."+spec.name.to_snake_case(),Vector3(0,0.14,0)).visible = false
	b = builder()
	b.push_translate(-pivot.position)
	# Thick low-poly tarp, covers front 80%; dark asphalt remains readable at rear.
	for side in [-1.0,1.0]:
		b.face("construction_tarp",[Vector3(side*1.22,2.25,-1.29),Vector3(0,2.43,-1.29),Vector3(0,2.40,2.28),Vector3(side*1.22,2.25,2.28)],Vector3(0,1,0))
		b.box_c("construction_tarp",Vector3(side*1.225,2.13,0.49),Vector3(0.045,0.26,3.56))
		for z in [-1.12,-0.25,0.65,1.55,2.18]:
			b.beam("metal_dark",Vector3(side*1.24,2.2,z),Vector3(side*1.12,1.98,z),0.027)
	b.beam("metal_dark",Vector3(-1.23,2.28,2.29),Vector3(1.23,2.28,2.29),0.055)
	b.pop()
	part(pivot,"Cover",b,id+".cover").visible=false
	marker(r,"HydraulicAnchor",Vector3(0,0.89,-0.65))
	marker(pivot,"HydraulicTarget",Vector3(0,0.05,-2.7))
	for spec in [{"name":"HydraulicBase","radius":0.105,"length":0.48,"slot":"construction_ochre_dark"},{"name":"HydraulicRod","radius":0.055,"length":0.40,"slot":"metal_light"}]:
		b=builder()
		b.cylinder(spec.slot,Vector3.ZERO,spec.radius,spec.length,10)
		part(r,spec.name,b,id+"."+spec.name.to_snake_case(),Vector3(0,0.85,-0.55)).rotation.x=deg_to_rad(-55)
	save(r,id,{"wheels":wheel_contract(),"bed":{"node":"BedPivot","axis":"X","operation_degrees":38,"pivot_m":[0,1.18,2.9]},"tailgate":{"node":"BedPivot/TailgatePivot","axis":"X","operation_degrees":-72},"loads":["BedPivot/LoadSandGravel","BedPivot/LoadAsphalt"],"cover":"BedPivot/Cover","hydraulics":{"nodes":["HydraulicBase","HydraulicRod"],"anchors":["HydraulicAnchor","BedPivot/HydraulicTarget"]}})

func mound(b, slot: String, hw: float, z0: float, z1: float, bottom: float, rim: float, rise: float) -> void:
	var corners = [Vector3(-hw,rim,z0),Vector3(hw,rim,z0),Vector3(hw,rim,z1),Vector3(-hw,rim,z1)]
	var ridge = [Vector3(-0.16,rim+rise,z0+0.65),Vector3(0.17,rim+rise*0.86,z1-0.6)]
	b.face(slot,[corners[0],corners[1],ridge[0]],Vector3.UP)
	b.face(slot,[corners[1],corners[2],ridge[1],ridge[0]],Vector3.UP)
	b.face(slot,[corners[2],corners[3],ridge[1]],Vector3.UP)
	b.face(slot,[corners[3],corners[0],ridge[0],ridge[1]],Vector3.UP)
	for i in 4:
		var j=(i+1)%4
		b.face(slot,[Vector3(corners[i].x,bottom,corners[i].z),Vector3(corners[j].x,bottom,corners[j].z),corners[j],corners[i]],Vector3((corners[i].x+corners[j].x)/2,0,(corners[i].z+corners[j].z)/2-(z0+z1)/2))

func clip_band(poly: Array, boundary: float, above: bool, twist: float) -> Array:
	var result: Array=[]
	if poly.is_empty():
		return result
	var previous: Vector2=poly[-1]
	var pv=previous.y-twist*previous.x-boundary
	var pi=pv>=0 if above else pv<=0
	for current in poly:
		var cv=current.y-twist*current.x-boundary
		var ci=cv>=0 if above else cv<=0
		if ci!=pi:
			result.append(previous.lerp(current,pv/(pv-cv)))
		if ci:
			result.append(current)
		previous=current
		pv=cv
		pi=ci
	return result

func lathe_z(b, profile: Array, sides: int, slot: String, paint_twist: float = 0.0) -> void:
	for k in profile.size()-1:
		var z0=profile[k][0]
		var r0=profile[k][1]
		var z1=profile[k+1][0]
		var r1=profile[k+1][1]
		for i in sides:
			var a0=TAU*i/sides
			var a1=TAU*(i+1)/sides
			var middle=(a0+a1)/2
			var hint=Vector3(cos(middle),sin(middle),-(r1-r0)/(z1-z0))
			var quad=[Vector2(z0,a0),Vector2(z0,a1),Vector2(z1,a1),Vector2(z1,a0)]
			# Clip paint into the un-twisted planar cone facet. Every resulting point
			# stays on that same facet: no twisted geometry, overlays or protrusions.
			for period in range(-2,4):
				for color in 2:
					var low=period*PI+(0.0 if color==0 else 0.65)
					var high=period*PI+(0.65 if color==0 else PI)
					var clipped=clip_band(clip_band(quad,low,true,paint_twist),high,false,paint_twist)
					if clipped.size()<3:
						continue
					var points: Array=[]
					for p in clipped:
						var u=(p.y-a0)/(a1-a0)
						var radius=lerp(r0,r1,(p.x-z0)/(z1-z0))
						var radial=Vector2(cos(a0),sin(a0)).lerp(Vector2(cos(a1),sin(a1)),u)*radius
						points.append(Vector3(radial.x,radial.y,p.x))
					b.face("construction_ochre" if color==0 else slot,points,hint)

func build_mixer() -> void:
	var id="vehicle.truck_mixer.a"
	var r=truck_base(id)
	var b=builder()
	for z in [-0.82,2.18]:
		b.bevel_box("construction_ochre_dark",Vector3(-0.8,1.05,z-0.22),Vector3(0.8,1.5,z+0.22),0.05)
		for sx in [-1.0,1.0]:
			b.beam("metal_dark",Vector3(sx*0.72,1.3,z),Vector3(sx*0.38,2.05,z),0.15)
	b.bevel_box("trim_white",Vector3(-1.12,1.22,-1.38),Vector3(-0.68,2.2,-0.65),0.08)
	# Rear feed funnel and ladder stay still while the drum rotates.
	for x in [-0.62,0.62]:
		b.beam("metal_dark",Vector3(x,1.15,3.06),Vector3(x,3.12,2.56),0.055)
	for y in [1.35,1.7,2.05,2.4,2.75]:
		b.beam("metal_light",Vector3(0.64,y,3.04-(y-1.35)*0.25),Vector3(1.04,y,3.04-(y-1.35)*0.25),0.045)
	shell_sides(b,"trim_white",0.22,0.46,2.81,3.2,2.44,2.91,0.055)
	b.box_c("trim_white",Vector3(0,3.18,2.9),Vector3(0.86,0.07,0.1))
	part(r,"MixerFrame",b,id+".frame")
	var mount=node(r,"DrumMount",Vector3(0,2.32,0.52))
	mount.rotation.x=deg_to_rad(-12)
	b=builder()
	var profile=[[-1.78,0.0],[-1.72,0.45],[-1.22,0.86],[-0.75,0.96],[0.35,0.91],[1.10,0.64],[1.52,0.36],[1.66,0.36],[1.67,0.0]]
	# Color is part of the same continuous mesh, never an intersecting overlay.
	lathe_z(b,profile,16,"trim_white",0.78)
	part(mount,"Drum",b,id+".drum")
	var chute=node(r,"ChutePivot",Vector3(0,2.62,2.49))
	b=builder()
	# Open channel, no slab covering the opening.
	b.push_basis(Basis(Vector3.RIGHT,deg_to_rad(44)))
	b.box_c("metal_light",Vector3(0,-0.07,0.69),Vector3(0.48,0.08,1.4))
	for x in [-0.25,0.25]:
		b.box_c("trim_white",Vector3(x,0.035,0.69),Vector3(0.055,0.23,1.4))
	b.pop()
	part(chute,"Chute",b,id+".chute")
	save(r,id,{"wheels":wheel_contract(),"drum":{"node":"DrumMount/Drum","axis":"local Z","mount_tilt_x_degrees":-12},"chute":{"node":"ChutePivot","axis":"Y","operation_degrees":24},"loads":[],"note":"Drum is permanent body; no visible concrete inventory or default delivery"})

func build_flatbed() -> void:
	var id="vehicle.truck_flatbed.a"
	var r=truck_base(id)
	var b=builder()
	b.bevel_box("construction_ochre_dark",Vector3(-1.22,1.12,-1.39),Vector3(1.22,1.3,3.44),0.035)
	b.box_c("wood_door",Vector3(0,1.315,1.0),Vector3(2.27,0.04,4.7))
	for sx in [-1.0,1.0]:
		b.box_c("construction_ochre",Vector3(sx*1.2,1.48,1.0),Vector3(0.06,0.28,4.8))
		for z in [-1.29,0.2,1.75,3.32]:
			b.box_c("metal_dark",Vector3(sx*1.18,1.64,z),Vector3(0.09,0.72,0.09))
	b.bevel_box("construction_ochre",Vector3(-1.2,1.3,-1.43),Vector3(1.2,2.49,-1.32),0.03)
	for x in [-0.8,-0.4,0,0.4,0.8]:
		b.box_c("metal_dark",Vector3(x,2.05,-1.445),Vector3(0.055,0.68,0.02))
	part(r,"Flatbed",b,id+".bed")
	for cargo in ["Steel","Wood"]:
		b=builder()
		b.push_translate(Vector3(0,-1.32,0))
		for y in [1.40,1.62,1.84]:
			for x in [-0.76,-0.38,0,0.38,0.76]:
				if cargo=="Steel":
					# I section ends stay visibly open, no solid rectangular stand-ins.
					for yy in [y,y+0.16]:
						b.box_c("metal_light",Vector3(x,yy,0.91),Vector3(0.29,0.035,4.02))
					b.box_c("metal_dark",Vector3(x,y+0.08,0.91),Vector3(0.045,0.16,4.02))
				else:
					b.bevel_box("construction_wood",Vector3(x-0.155,y-0.02,-1.10),Vector3(x+0.155,y+0.16,2.99),0.014)
					b.box_c("construction_wood_end",Vector3(x,y+0.07,3.0),Vector3(0.27,0.14,0.012))
		for z in [-0.45,1.95]:
			# Load-specific tied straps disappear with the load.
			b.box_c("rubber",Vector3(0,2.035,z),Vector3(1.9,0.04,0.075))
			for sx in [-1.0,1.0]:
				b.beam("rubber",Vector3(sx*0.96,2.03,z),Vector3(sx*1.16,1.39,z),0.065)
		b.pop()
		part(r,"Load"+cargo,b,id+".load_"+cargo.to_lower(),Vector3(0,1.32,0)).visible=false
	save(r,id,{"wheels":wheel_contract(),"loads":["LoadSteel","LoadWood"],"quantity":"Load pivots at deck Y=1.32; host may scale local Y or hide from real quantity; no inventory is encoded","bed":"Flatbed remains when empty; tie straps are inside each load node"})

func build_tow_truck() -> void:
	var id="vehicle.tow_truck.a"
	var r=truck_base(id)
	# A physical flat recovery deck, not a cargo bed with tall enclosing walls.
	var b=builder()
	for sx in [-1.0,1.0]:
		b.box_c("construction_ochre_dark",Vector3(sx*0.66,1.04,0.95),Vector3(0.16,0.12,4.75))
		b.bevel_box("construction_ochre",Vector3(sx*0.88-0.23,0.57,-1.27),Vector3(sx*0.88+0.23,1.06,-0.42),0.045)
		# Broad service stripe beneath the door glass; no lettering or external logo.
		b.box_c("construction_ochre",Vector3(sx*1.168,1.53,-2.71),Vector3(0.022,0.18,1.24))
		b.box_c("trim_white",Vector3(sx*1.181,1.53,-2.71),Vector3(0.008,0.045,1.24))
	# Unlit amber light bar, kept separate from the cab's seven material slots.
	b.bevel_box("metal_dark",Vector3(-0.82,2.78,-2.45),Vector3(0.82,2.86,-2.12),0.025)
	for sx in [-1.0,1.0]:
		b.bevel_box("construction_ochre",Vector3(sx*0.58-0.22,2.86,-2.43),Vector3(sx*0.58+0.22,2.97,-2.14),0.045)
	b.box_c("trim_white",Vector3(0,2.89,-2.285),Vector3(0.65,0.06,0.25))
	part(r,"RecoveryFrame",b,id+".frame")
	var pivot=node(r,"DeckPivot",Vector3(0,1.15,2.9))
	b=builder()
	b.push_translate(-pivot.position)
	b.bevel_box("metal_light",Vector3(-1.20,1.10,-1.38),Vector3(1.20,1.24,3.28),0.025)
	# Thin tapered tail; the review pose shows an initial tilt/slide, not full unloading.
	Truck._extrude_x(b,"metal_light",PackedVector2Array([Vector2(3.25,1.10),Vector2(3.25,1.24),Vector2(3.55,1.13),Vector2(3.55,1.10)]),-1.20,1.20)
	for sx in [-1.0,1.0]:
		b.box_c("construction_ochre",Vector3(sx*1.20,1.27,0.95),Vector3(0.08,0.12,4.65))
		# Low wheel guides and tiedown apertures read at the game's isometric angle.
		for z in [-0.9,0.0,0.9,1.8,2.7]:
			b.box_c("metal_dark",Vector3(sx*1.12,1.245,z),Vector3(0.10,0.01,0.22))
		b.beam("construction_ochre",Vector3(sx*1.12,1.24,-1.33),Vector3(sx*1.12,2.43,-1.33),0.10)
		b.beam("construction_ochre_dark",Vector3(sx*1.12,1.27,-0.89),Vector3(sx*1.12,1.83,-1.33),0.07)
	b.beam("construction_ochre",Vector3(-1.12,2.43,-1.33),Vector3(1.12,2.43,-1.33),0.11)
	b.beam("construction_ochre_dark",Vector3(-1.12,1.83,-1.33),Vector3(1.12,1.83,-1.33),0.08)
	# Winch housing/spool at the headboard, clear of the passenger car's nose.
	b.bevel_box("construction_ochre_dark",Vector3(-0.36,1.28,-1.36),Vector3(0.36,1.51,-1.15),0.025)
	b.cylinder_x("metal_dark",Vector3(0,1.49,-1.24),0.10,0.43,10)
	b.pop()
	part(pivot,"Deck",b,id+".deck")
	marker(pivot,"CarryAnchor",Vector3(0,0.09,-1.815))
	save(r,id,{"wheels":wheel_contract(),"loads":[],"deck":{"node":"DeckPivot","mesh":"DeckPivot/Deck","axis":"X","rest_position_m":[0,1.15,2.9],"operation_position_m":[0,1.15,3.5],"operation_degrees":12,"rotation_direction":"Positive X raises front (-Z) relative to rear; positive Z slides rearward","carry_anchor":"DeckPivot/CarryAnchor","carry_anchor_rest_m":[0,1.24,1.085],"clear_floor_min_m":[-1.14,1.24,-1.10],"clear_floor_max_m":[1.14,1.24,3.27]},"note":"Empty by default; host parents the real carried vehicle at CarryAnchor with identity local transform. Deck pose is illustrative, not automatic motion or simulation timing. Passenger car fits at native scale; full-size cargo truck does not fit this compact deck."})

func build_tow_truck_heavy() -> void:
	var id="vehicle.tow_truck_heavy.a"
	var r=truck_base(id)
	# Extend the working silhouette behind the existing fleet chassis; recenter
	# the cab/chassis so the complete heavy wrecker is about eight meters long.
	for child in r.get_children():
		child.position.z-=0.4
	var b=builder()
	for sx in [-1.0,1.0]:
		b.bevel_box("construction_ochre",Vector3(sx*0.90-0.28,1.12,-1.74),Vector3(sx*0.90+0.28,1.64,2.67),0.045)
		for z in [-1.15,0.0,1.15,2.19]:
			b.box_c("metal_dark",Vector3(sx*1.185,1.37,z),Vector3(0.012,0.32,0.018))
			b.box_c("metal_light",Vector3(sx*1.193,1.49,z+0.22),Vector3(0.018,0.035,0.15))
		b.box_c("trim_white",Vector3(sx*1.193,1.22,0.45),Vector3(0.012,0.08,4.20))
		b.box_c("construction_ochre",Vector3(sx*1.168,1.53,-3.11),Vector3(0.022,0.18,1.24))
		b.box_c("trim_white",Vector3(sx*1.181,1.53,-3.11),Vector3(0.008,0.045,1.24))
	# Closed tool lockers flank an open recovery mechanism; no cargo platform.
	b.bevel_box("construction_ochre_dark",Vector3(-0.63,1.05,-1.70),Vector3(0.63,1.36,2.57),0.045)
	for sx in [-1.0,1.0]:
		b.beam("construction_ochre",Vector3(sx*0.96,1.45,-1.70),Vector3(sx*0.96,3.21,-1.70),0.14)
	b.beam("construction_ochre",Vector3(-0.96,3.21,-1.70),Vector3(0.96,3.21,-1.70),0.16)
	b.bevel_box("metal_dark",Vector3(-0.82,2.78,-2.85),Vector3(0.82,2.86,-2.52),0.025)
	for sx in [-1.0,1.0]:
		b.bevel_box("construction_ochre",Vector3(sx*0.58-0.22,2.86,-2.83),Vector3(sx*0.58+0.22,2.97,-2.54),0.045)
	# Compact raised recovery boom with a nested extension and stowed cable.
	b.beam("construction_ochre_dark",Vector3(0,1.50,-0.67),Vector3(0,2.68,1.47),0.43)
	b.beam("construction_ochre",Vector3(0,2.59,1.32),Vector3(0,2.88,2.50),0.29)
	for sx in [-1.0,1.0]:
		b.beam("metal_light",Vector3(sx*0.32,1.49,0.20),Vector3(sx*0.19,2.48,1.45),0.085)
	b.cylinder_x("metal_dark",Vector3(0,1.67,-0.89),0.20,0.75,12)
	b.beam("metal_dark",Vector3(0,2.82,2.51),Vector3(0,1.82,2.51),0.025)
	b.beam("metal_dark",Vector3(0,1.82,2.51),Vector3(0,1.73,2.66),0.07)
	b.beam("metal_dark",Vector3(0,1.73,2.66),Vector3(0,1.87,2.73),0.07)
	part(r,"RecoveryBody",b,id+".recovery_body")
	var lift=node(r,"LiftPivot",Vector3(0,0.68,2.5))
	b=builder()
	b.beam("construction_ochre_dark",Vector3(0,0,0),Vector3(0,0.22,0.96),0.30)
	b.beam("construction_ochre",Vector3(0,0.22,0.84),Vector3(0,0.34,1.42),0.22)
	b.beam("metal_dark",Vector3(-0.96,0.34,1.43),Vector3(0.96,0.34,1.43),0.17)
	for sx in [-1.0,1.0]:
		# Two open axle cradles, supporting the actual truck's front axle.
		b.box_c("metal_light",Vector3(sx*0.73,0.38,1.50),Vector3(0.24,0.08,0.20))
		for z in [1.40,1.60]:
			b.box_c("construction_ochre",Vector3(sx*0.73,0.48,z),Vector3(0.24,0.20,0.06))
	part(lift,"LiftArm",b,id+".lift_arm")
	marker(lift,"LiftAnchor",Vector3(0,0.42,1.50))
	for child in r.get_children():
		child.position.z-=0.065
	save(r,id,{"wheels":wheel_contract(),"loads":[],"lift":{"node":"LiftPivot","mesh":"LiftPivot/LiftArm","axis":"X","operation_degrees":-10,"rotation_direction":"Negative X raises the rear axle cradle (+Z)","anchor":"LiftPivot/LiftAnchor","anchor_rest_m":[0,1.10,3.935],"source_front_axle_m":[0,0.47,-2.55],"source_rear_axle_m":[0,0.47,1.75],"source_rear_wheel_radius_m":0.47,"source_scene":"res://Assets/V1/Scenes/vehicle.truck_cargo.a.tscn","alignment":"Same horizontal heading: carried truck behind (+Z), its front -Z toward the wrecker; pitch positive X so rear wheels stay grounded, then translate source front axle center onto LiftAnchor. Keep native scale; align axle, not vehicle root. Counter-rotate all four towed-truck wheels by body pitch for the flat-ground review pose."},"note":"Heavy axle-lift wrecker, delivered alone. Host controls the real towed truck and wheel contact, no cargo truck clone or motion is embedded."})

func build_paver() -> void:
	var id="vehicle.asphalt_paver.a"
	var r=Node3D.new()
	var b=builder()
	b.bevel_box("construction_ochre_dark",Vector3(-0.96,0.48,-1.5),Vector3(0.96,1.12,1.5),0.08)
	b.bevel_box("construction_ochre",Vector3(-0.9,0.95,-0.65),Vector3(0.9,1.65,0.91),0.1)
	for sx in [-1.0,1.0]:
		for z in [-0.3,-0.1,0.1,0.3,0.5]:
			b.box_c("metal_dark",Vector3(sx*0.912,1.39,z),Vector3(0.025,0.29,0.055))
		b.beam("metal_dark",Vector3(sx*0.82,1.24,0.84),Vector3(sx*0.82,2.73,0.84),0.07)
		b.beam("metal_dark",Vector3(sx*0.82,1.24,-0.45),Vector3(sx*0.82,2.73,-0.45),0.07)
		b.box_c("metal_light",Vector3(sx*1.01,1.00,0.83),Vector3(0.19,0.08,0.6))
	# Flat canopy keeps the hopper and screed unobstructed.
	b.bevel_box("construction_ochre",Vector3(-1.03,2.71,-0.67),Vector3(1.03,2.84,1.11),0.055)
	b.bevel_box("rubber",Vector3(-0.3,1.48,0.3),Vector3(0.3,1.62,0.78),0.04)
	b.bevel_box("rubber",Vector3(-0.3,1.6,0.71),Vector3(0.3,2.06,0.82),0.05)
	b.beam("metal_dark",Vector3(0,1.58,-0.11),Vector3(0,2.02,-0.22),0.10)
	b.box_c("glass_dark",Vector3(0,2.05,-0.22),Vector3(0.5,0.08,0.32))
	b.beam("metal_dark",Vector3(0.66,1.4,0.67),Vector3(0.66,2.3,0.67),0.1)
	# Central open feed floor slopes toward conveyor throat.
	b.face("metal_dark",[Vector3(-0.76,0.69,-2.1),Vector3(0.76,0.69,-2.1),Vector3(0.63,0.87,-0.72),Vector3(-0.63,0.87,-0.72)],Vector3.UP)
	b.box_c("construction_ochre_dark",Vector3(0,0.72,-2.12),Vector3(1.53,0.12,0.12))
	part(r,"Body",b,id+".body")
	for sx in [-1.0,1.0]:
		var t=builder()
		# Chamfered elongated track loop; all cleats batched into this one mesh.
		var profile=PackedVector2Array([Vector2(-1.57,0.12),Vector2(-1.82,0.32),Vector2(-1.82,0.56),Vector2(-1.57,0.79),Vector2(1.28,0.79),Vector2(1.55,0.56),Vector2(1.55,0.32),Vector2(1.28,0.12)])
		Truck._extrude_x(t,"rubber",profile,-0.22,0.22)
		for z in [-1.39,-0.72,-0.05,0.62,1.23]:
			t.cylinder_x("metal_dark",Vector3(0,0.45,z),0.25,0.47,10,18)
		for z in 13:
			t.box_c("metal_dark",Vector3(0,0.77,-1.46+z*0.22),Vector3(0.45,0.055,0.09))
		part(r,"TrackLeft" if sx<0 else "TrackRight",t,id+".track",Vector3(sx*0.98,-0.12,0))
		var h=node(r,"HopperLeftPivot" if sx<0 else "HopperRightPivot",Vector3(sx*0.76,0.75,-1.42))
		t=builder()
		# Folding wing is an open scoop with front lip and tall outside cheek.
		t.face("construction_ochre",[Vector3(0,-0.05,-0.68),Vector3(sx*0.4,0.28,-0.68),Vector3(sx*0.4,0.48,0.68),Vector3(0,0.1,0.68)],Vector3.UP)
		t.face("construction_ochre_dark",[Vector3(sx*0.4,0.28,-0.68),Vector3(sx*0.4,0.87,-0.68),Vector3(sx*0.4,0.87,0.68),Vector3(sx*0.4,0.48,0.68)],Vector3(sx,0,0))
		t.face("construction_ochre",[Vector3(sx*0.36,0.28,-0.68),Vector3(sx*0.36,0.87,-0.68),Vector3(sx*0.36,0.87,0.68),Vector3(sx*0.36,0.48,0.68)],Vector3(-sx,0,0))
		t.face("construction_ochre",[Vector3(0,-0.05,-0.68),Vector3(sx*0.4,0.28,-0.68),Vector3(sx*0.4,0.44,-0.68),Vector3(0,0.12,-0.68)],Vector3(0,0,-1))
		t.box_c("construction_ochre",Vector3(sx*0.41,0.86,0),Vector3(0.065,0.08,1.44))
		t.beam("construction_ochre",Vector3(0,0.1,0.68),Vector3(sx*0.4,0.87,0.68),0.08)
		part(h,"Wing",t,id+".hopper_"+str(int(sx)))
	b=builder()
	b.push_translate(Vector3(0,-0.84,0))
	mound(b,"construction_asphalt",0.81,-1.99,-0.82,0.84,1.04,0.24)
	b.pop()
	part(r,"HopperLoad",b,id+".hopper_load",Vector3(0,0.84,0)).visible=false
	var sc=node(r,"ScreedPivot",Vector3(0,0.50,1.32))
	b=builder()
	for sx in [-1.0,1.0]:
		b.beam("metal_dark",Vector3(sx*0.65,0.15,-0.35),Vector3(sx*0.84,-0.11,0.61),0.13)
	b.bevel_box("construction_ochre",Vector3(-1.24,-0.22,0.38),Vector3(1.24,0.16,0.94),0.05)
	b.box_c("metal_dark",Vector3(0,-0.33,0.73),Vector3(2.48,0.18,0.59))
	for sx in [-1.0,1.0]:
		b.box_c("metal_dark",Vector3(sx*1.23,-0.12,0.72),Vector3(0.08,0.4,0.8))
	part(sc,"Screed",b,id+".screed")
	marker(r,"Candidate_HopperFeed",Vector3(0,0,-2.6))
	marker(r,"Candidate_PavementExit",Vector3(0,0,2.35))
	# Center the complete physical silhouette, not just the engine block.
	for child in r.get_children():
		child.position.z-=0.13
	save(r,id,{"hopper":{"left":"HopperLeftPivot","right":"HopperRightPivot","axis":"Z","fold_degrees":[-22,22]},"loads":["HopperLoad"],"screed":{"node":"ScreedPivot","axis":"X","rest_y_m":0.50,"operation_y_m":0.42,"operation_degrees":0},"tracks":["TrackLeft","TrackRight"],"width_m":2.54,"note":"Compact lane-size equipment; width does not determine road capacity or lane geometry"})

func roller_drum() -> Mesh:
	var b=builder()
	Truck._lathe_x(b,[[-1.0,0.0,"construction_ochre"],[-1.0,0.37,"construction_ochre"],[-1.03,0.53,"metal_light"],[-0.96,0.60,"metal_light"],[0.96,0.60,"metal_light"],[1.03,0.53,"metal_light"],[1.0,0.37,"construction_ochre"],[1.0,0.0,"construction_ochre"]],16,11.25)
	b.cylinder_x("metal_dark",Vector3.ZERO,0.16,2.13,10)
	return mesh(b,"vehicle.road_roller.a.drum")

func build_roller() -> void:
	var id="vehicle.road_roller.a"
	var r=Node3D.new()
	var b=builder()
	b.bevel_box("construction_ochre_dark",Vector3(-0.65,0.83,-0.87),Vector3(0.65,1.2,0.93),0.09)
	b.bevel_box("construction_ochre",Vector3(-0.78,1.04,0.24),Vector3(0.78,1.69,1.54),0.11)
	for x in [-0.45,-0.22,0,0.22,0.45]:
		b.box_c("metal_dark",Vector3(x,1.43,1.551),Vector3(0.10,0.3,0.022))
	for sx in [-1.0,1.0]:
		b.beam("metal_dark",Vector3(sx*0.65,1.2,0.55),Vector3(sx*0.65,2.59,0.55),0.085)
		b.beam("metal_dark",Vector3(sx*0.65,1.2,-0.6),Vector3(sx*0.65,2.59,-0.6),0.085)
		b.box_c("metal_dark",Vector3(sx*1.03,0.75,1.4),Vector3(0.12,0.38,0.94))
		b.beam("metal_light",Vector3(sx*0.8,1.03,-0.28),Vector3(sx*0.8,1.6,-0.28),0.055)
	b.bevel_box("construction_ochre",Vector3(-0.86,2.58,-0.84),Vector3(0.86,2.71,0.78),0.06)
	b.bevel_box("rubber",Vector3(-0.29,1.3,-0.18),Vector3(0.29,1.47,0.28),0.045)
	b.bevel_box("rubber",Vector3(-0.29,1.4,0.2),Vector3(0.29,1.9,0.31),0.05)
	b.beam("metal_dark",Vector3(0,1.2,-0.56),Vector3(0,1.91,-0.67),0.08)
	# Steering wheel in plane perpendicular to column, visible in close iso.
	b.cylinder("metal_dark",Vector3(0,1.9,-0.67),0.22,0.045,10)
	b.box_c("metal_light",Vector3(-0.84,0.99,-0.02),Vector3(0.22,0.1,0.55))
	part(r,"Body",b,id+".body")
	var front=node(r,"FrontSteering",Vector3(0,0,-1.39))
	b=builder()
	for sx in [-1.0,1.0]:
		b.beam("construction_ochre",Vector3(sx*0.66,1.04,0.55),Vector3(sx*1.07,0.6,0),0.16)
		b.box_c("construction_ochre_dark",Vector3(sx*1.07,0.65,0.06),Vector3(0.13,0.32,0.63))
	b.beam("metal_dark",Vector3(-1.0,0.72,-0.51),Vector3(1.0,0.72,-0.51),0.055)
	part(front,"Fork",b,id+".front_fork")
	var dm=roller_drum()
	for spec in [{"parent":front,"name":"FrontDrum","pos":Vector3(0,0.5885,0)},{"parent":r,"name":"RearDrum","pos":Vector3(0,0.5885,1.4)}]:
		var drum=MeshInstance3D.new()
		drum.name=spec.name
		drum.mesh=dm
		drum.position=spec.pos
		spec.parent.add_child(drum)
	marker(r,"Candidate_ContactFront",Vector3(0,0,-1.39))
	marker(r,"Candidate_ContactRear",Vector3(0,0,1.4))
	save(r,id,{"drums":{"nodes":["FrontSteering/FrontDrum","RearDrum"],"axis":"X","radius_m":0.5885},"steering":{"node":"FrontSteering","axis":"Y","operation_degrees":8},"width_m":2.32,"note":"Two smooth steel drums, no operator figurine or independent compaction animation"})
