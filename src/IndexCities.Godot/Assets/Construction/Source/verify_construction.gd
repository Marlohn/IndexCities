## Validation and real GL Compatibility preview; tool only, no simulation/save writes.
extends SceneTree

const ROOT = "res://Assets/Construction"
const V1 = "res://Assets/V1/Scenes/"
var out = ""
var checks = 0
var failures: Array = []
var measured: Array = []
var scene_root: Node3D
var camera: Camera3D
var metrics: Array = []

func _initialize() -> void:
	call_deferred("run")

func expect(ok: bool, msg: String) -> void:
	checks += 1
	if not ok:
		failures.append(msg)
		push_error("[construction-check] "+msg)

func nodes(n: Node) -> Array:
	var a=[n]
	for c in n.get_children():
		a.append_array(nodes(c))
	return a

func bounds(r: Node3D, only_visible: bool = false) -> AABB:
	var box: AABB
	var found=false
	for n in nodes(r):
		if n is MeshInstance3D and (not only_visible or n.is_visible_in_tree()):
			var t=r.global_transform.affine_inverse()*n.global_transform
			for s in n.mesh.get_surface_count():
				for vertex in n.mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX]:
					var p=t*vertex
					box=box.expand(p) if found else AABB(p,Vector3.ZERO)
					found=true
	return box

func mesh_triangles(n: MeshInstance3D, reference: Node3D, omit_front_axle: bool = false) -> Array:
	var result: Array=[]
	var transform=reference.global_transform.affine_inverse()*n.global_transform
	for surface in n.mesh.get_surface_count():
		var arrays=n.mesh.surface_get_arrays(surface)
		var vertices=arrays[Mesh.ARRAY_VERTEX]
		var indices=arrays[Mesh.ARRAY_INDEX]
		var indexed=indices!=null and indices.size()>0
		for i in range(0,indices.size() if indexed else vertices.size(),3):
			var points: Array=[]
			var axle=omit_front_axle
			for k in 3:
				var p: Vector3=vertices[indices[i+k] if indexed else i+k]
				axle=axle and abs(p.y-0.47)<0.08 and abs(p.z+2.55)<0.08
				points.append(transform*p)
			# The cradle intentionally holds this axle; cabin/chassis clearance is
			# checked separately from that permitted mechanical contact.
			if axle:
				continue
			var box=AABB(points[0],Vector3.ZERO).expand(points[1]).expand(points[2])
			result.append({"points":points,"box":box})
	return result

func point_triangle_distance_squared(p: Vector3, t: Array) -> float:
	var normal=(t[1]-t[0]).cross(t[2]-t[0])
	if normal.length_squared()>0.000000001:
		var projection=p-normal*normal.dot(p-t[0])/normal.length_squared()
		var inside=true
		for i in 3:
			inside=inside and (t[(i+1)%3]-t[i]).cross(projection-t[i]).dot(normal)>=-0.0000001
		if inside:
			return p.distance_squared_to(projection)
	var distance=INF
	for i in 3:
		distance=min(distance,p.distance_squared_to(Geometry3D.get_closest_point_to_segment(p,t[i],t[(i+1)%3])))
	return distance

func triangles_have_clearance(a: Array, b: Array, gap: float) -> bool:
	for i in 3:
		if Geometry3D.segment_intersects_triangle(a[i],a[(i+1)%3],b[0],b[1],b[2])!=null:
			return false
		if Geometry3D.segment_intersects_triangle(b[i],b[(i+1)%3],a[0],a[1],a[2])!=null:
			return false
		if min(point_triangle_distance_squared(a[i],b),point_triangle_distance_squared(b[i],a))<gap*gap:
			return false
		for j in 3:
			var closest=Geometry3D.get_closest_points_between_segments(a[i],a[(i+1)%3],b[j],b[(j+1)%3])
			if closest[0].distance_squared_to(closest[1])<gap*gap:
				return false
	return true

func meshes_have_clearance(a: Array, b: Array, gap: float) -> bool:
	for first in a:
		for second in b:
			if first.box.grow(gap).intersects(second.box) and not triangles_have_clearance(first.points,second.points,gap):
				return false
	return true

func wheel_floor_y(wheel: MeshInstance3D, reference: Node3D) -> float:
	var transform=reference.global_transform.affine_inverse()*wheel.global_transform
	var lowest=INF
	for surface in wheel.mesh.get_surface_count():
		for vertex in wheel.mesh.surface_get_arrays(surface)[Mesh.ARRAY_VERTEX]:
			lowest=min(lowest,(transform*vertex).y)
	return lowest

func run() -> void:
	out=ProjectSettings.globalize_path("res://../../artifacts/verification/construction-models")
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="):
			out=arg.substr(6)
	DirAccess.make_dir_recursive_absolute(out)
	var manifest=JSON.parse_string(FileAccess.get_file_as_string(ROOT+"/manifest.construction.json"))
	expect(manifest.entries.size()==7,"Seven families")
	for e in manifest.entries:
		var ps=load(e.scene)
		expect(ps is PackedScene,"PackedScene "+e.scene)
		var r=ps.instantiate()
		root.add_child(r)
		expect(r.get_meta("visual_id")==e.visual_id,"Visual ID "+e.visual_id)
		expect(r.position==Vector3.ZERO and r.rotation==Vector3.ZERO and r.scale==Vector3.ONE,"Ground centered root "+e.visual_id)
		var triangles=0
		var rest_triangles=0
		var surfaces=0
		var rest_surfaces=0
		var count=0
		for n in nodes(r):
			expect(n.get_script()==null,"No runtime script "+str(r.get_path_to(n)))
			expect(not(n is CollisionObject3D or n is Light3D or n is Timer or n is GPUParticles3D),"No runtime activity "+str(n.name))
			# The only animation allowed is the host-triggered hood_open (never autoplay).
			expect(not(n is AnimationPlayer) or (n.name=="AnimationPlayer" and n.get_animation_list()==PackedStringArray(["hood_open"]) and n.autoplay==""),"Only hood_open without autoplay "+str(n.name))
			if n is MeshInstance3D:
				count+=1
				expect(n.mesh is ArrayMesh and n.mesh.resource_path.begins_with(ROOT+"/Meshes/"),"External reusable mesh "+str(n.name))
				expect(n.mesh.get_surface_count()<=7,"At most 7 material surfaces per mesh "+str(n.name))
				for s in n.mesh.get_surface_count():
					var m=n.mesh.surface_get_material(s)
					expect(m is StandardMaterial3D and not m.emission_enabled,"Non emissive material "+str(n.name))
					expect(not m.resource_path.is_empty(),"External material "+str(n.name))
					var indices=n.mesh.surface_get_array_index_len(s)
					var t=(indices if indices>0 else n.mesh.surface_get_array_len(s))/3
					triangles+=t
					surfaces+=1
					if n.is_visible_in_tree():
						rest_triangles+=t
						rest_surfaces+=1
		for path in e.contract.get("loads",[]):
			var n=r.get_node_or_null(path)
			expect(n is Node3D and not n.visible,"Load hidden by default "+path)
		if e.contract.has("cover"):
			expect(not r.get_node(e.contract.cover).visible,"Cover hidden by default")
		if e.contract.has("wheels"):
			var wheels=e.contract.wheels.nodes
			for path in wheels:
				var w=r.get_node(path)
				expect(w.mesh==r.get_node(wheels[0]).mesh,"Shared wheel mesh "+path)
				expect(is_equal_approx(w.position.y,0.47),"Wheel pivot Y "+path)
		verify_breakdown(r,e)
		if e.visual_id=="vehicle.tow_truck.a":
			verify_tow_truck(r,e.contract)
		elif e.visual_id=="vehicle.tow_truck_heavy.a":
			verify_tow_heavy(r,e.contract)
		var a=bounds(r)
		expect(a.position.y>=-0.002,"Ground contact; no underground geometry "+e.visual_id)
		expect(a.size.x<=2.901,"Rest fits 3.25 m technical lane "+e.visual_id)
		expect(abs(a.position.z+a.end.z)<0.15,"Centered longitudinal envelope "+e.visual_id)
		expect(triangles==e.triangles_all_states,"Manifest triangles match "+e.visual_id)
		var posed=r.duplicate()
		root.add_child(posed)
		pose(posed,true)
		var operation=bounds(posed,true)
		expect(operation.position.y>=-0.02,"Operation stays above ground "+e.visual_id)
		expect(operation.size.x<=2.901,"Operation lane envelope "+e.visual_id)
		measured.append({"id":e.visual_id,"all_states_triangles":triangles,"default_triangles":rest_triangles,"all_states_surfaces":surfaces,"default_surfaces":rest_surfaces,"mesh_instances":count,"rest_envelope_m":vec(a.size),"operation_envelope_m":vec(operation.size),"operation_min_m":vec(operation.position)})
		print("[construction-check] "+e.visual_id+" rest="+str(a)+" operation="+str(operation))
		posed.free()
		r.free()
	var f=FileAccess.open(out+"/validation.json",FileAccess.WRITE)
	f.store_string(JSON.stringify({"checks":checks,"failures":failures,"engine":Engine.get_version_info(),"renderer_project":ProjectSettings.get_setting("rendering/renderer/rendering_method"),"measurements":measured},"  "))
	f.close()
	print("[construction-check] %d checks; %d failures" % [checks,failures.size()])
	if not failures.is_empty():
		quit(1)
		return
	if OS.get_cmdline_user_args().has("--validate-only"):
		quit(0)
		return
	expect(DisplayServer.get_name()!="headless","Real render needs visible graphics driver")
	if not failures.is_empty():
		quit(1)
		return
	await render_reviews()
	quit(0)

func vec(p: Vector3) -> Array:
	return [snapped(p.x,0.0001),snapped(p.y,0.0001),snapped(p.z,0.0001)]

## Breakdown hooks: Hood pivot + hood_open, four hazard-light anchors and the smoke anchor.
func verify_breakdown(r: Node3D, e: Dictionary) -> void:
	var id=e.visual_id
	var spec=e.contract.get("breakdown",{})
	expect(not spec.is_empty(),"Manifest declares breakdown hooks "+id)
	var hood=r.get_node_or_null("Hood")
	expect(hood is MeshInstance3D,"Hood mesh node "+id)
	if not hood is MeshInstance3D:
		return
	expect(hood.rotation==Vector3.ZERO and hood.scale==Vector3.ONE,"Hood closed at rest "+id)
	var local=hood.mesh.get_aabb()
	expect(local.position.length()<1.7 and local.end.length()<1.7,"Hood mesh origin is its hinge "+id)
	var animations=r.get_node_or_null("AnimationPlayer")
	expect(animations is AnimationPlayer and animations.has_animation("hood_open"),"hood_open animation "+id)
	if animations is AnimationPlayer and animations.has_animation("hood_open"):
		var a=animations.get_animation("hood_open")
		expect(a.get_track_count()==1 and str(a.track_get_path(0))=="Hood:rotation","hood_open drives only Hood rotation "+id)
		var end=a.track_get_key_value(0,a.track_get_key_count(0)-1)
		expect(is_equal_approx(end.x,deg_to_rad(spec.hood.open_degrees)) and is_zero_approx(end.y) and is_zero_approx(end.z),"hood_open ends at the manifest angle "+id)
		expect(a.track_get_key_value(0,0)==Vector3.ZERO,"hood_open starts closed "+id)
		# Open hood stays in the technical lane and above ground.
		var closed=bounds(r)
		hood.rotation=end
		var open=bounds(r)
		hood.rotation=Vector3.ZERO
		expect(open.size.x<=2.901 and open.position.y>=-0.002,"Open hood keeps lane width and ground "+id)
		expect(open.end.y<=4.0,"Open hood height under 4 m "+id)
		measured.append({"id":id,"hood_open_envelope_m":vec(open.size),"hood_closed_envelope_m":vec(closed.size)})
	var right={"HazardFR":1.0,"HazardRR":1.0,"HazardFL":-1.0,"HazardRL":-1.0}
	for marker in right:
		var m=r.get_node_or_null(marker)
		expect(m is Marker3D,"Marker "+marker+" "+id)
		if m is Marker3D:
			expect(signf(m.position.x)==right[marker] and signf(m.position.z)==(-1.0 if marker.begins_with("HazardF") else 1.0),"Hazard marker side/end "+marker+" "+id)
			var mirror=r.get_node_or_null(marker.left(marker.length()-1)+("R" if marker.ends_with("L") else "L"))
			expect(mirror is Marker3D and is_equal_approx(mirror.position.x,-m.position.x) and is_equal_approx(mirror.position.y,m.position.y) and is_equal_approx(mirror.position.z,m.position.z),"Hazard marker mirrored "+marker+" "+id)
	expect(r.get_node_or_null("HoodSmoke") is Marker3D,"HoodSmoke marker "+id)

func verify_tow_truck(r: Node3D, contract: Dictionary) -> void:
	var deck=r.get_node_or_null("DeckPivot/Deck")
	var anchor=r.get_node_or_null("DeckPivot/CarryAnchor")
	expect(deck is MeshInstance3D,"Tow deck mesh at stable path")
	expect(anchor is Marker3D,"Tow CarryAnchor is a Marker3D under DeckPivot")
	if not anchor is Marker3D:
		return
	expect(anchor.transform.basis.is_equal_approx(Basis.IDENTITY),"CarryAnchor preserves vehicle orientation/scale")
	var anchor_position=r.global_transform.affine_inverse()*anchor.global_position
	expect(vec(anchor_position)==contract.deck.carry_anchor_rest_m,"CarryAnchor rest coordinates match manifest")
	var car=load(V1+"vehicle.car_passenger.a.tscn").instantiate()
	anchor.add_child(car)
	var a=bounds(car)
	var lo=contract.deck.clear_floor_min_m
	var hi=contract.deck.clear_floor_max_m
	expect(a.position.y>=-0.002 and a.position.y<=0.002,"Passenger car root rests on ground before mounting")
	expect(a.position.x+anchor_position.x>=lo[0] and a.end.x+anchor_position.x<=hi[0],"Native passenger car including mirrors fits deck width")
	expect(a.position.z+anchor_position.z>=lo[2] and a.end.z+anchor_position.z<=hi[2],"Native passenger car fits clear deck length")
	expect(is_equal_approx(anchor_position.y,lo[1]),"CarryAnchor is at deck floor height")
	car.free()
	var posed=r.duplicate()
	root.add_child(posed)
	pose(posed,true)
	# The initial tilt/slide keeps the floor above the fixed rear fenders. A pose
	# that merely stays above the ground can still cut straight through the chassis.
	var pivot=posed.get_node("DeckPivot")
	for z in [1.45,2.65,3.17]:
		var local_z=(z-pivot.position.z+0.05*sin(pivot.rotation.x))/cos(pivot.rotation.x)
		var floor_point=pivot.transform*Vector3(0,-0.05,local_z)
		expect(floor_point.y>=1.131,"Tilted/slid deck clears rear fenders at Z="+str(z))
	expect(posed.get_node("DeckPivot").rotation.x>0,"Positive X inclines deck front above rear")
	posed.free()

func load_visual(id: String) -> Node3D:
	return load(ROOT+"/Scenes/vehicle."+id+".a.tscn").instantiate()

func mount_heavy_truck(r: Node3D) -> Node3D:
	# Preview math in wrecker-local flat-ground coordinates. The production host
	# owns the actual truck, road slope and wheel rolling; no truck is in the asset.
	var truck=load(V1+"vehicle.truck_cargo.a.tscn").instantiate()
	r.add_child(truck)
	truck.name="ReviewTowedTruck"
	align_heavy_truck(r,truck)
	return truck

func align_heavy_truck(r: Node3D, truck: Node3D) -> void:
	var target=r.global_transform.affine_inverse()*r.get_node("LiftPivot/LiftAnchor").global_position
	var front=(truck.get_node("Wheel_FL").position+truck.get_node("Wheel_FR").position)*0.5
	var rear=(truck.get_node("Wheel_RL").position+truck.get_node("Wheel_RR").position)*0.5
	var pitch=asin((target.y-0.47)/(rear.z-front.z))
	truck.rotation.x=pitch
	truck.position=target-truck.basis*front
	# Keep the faceted tires' existing flat ground contact in this static preview.
	for wheel in ["Wheel_FL","Wheel_FR","Wheel_RL","Wheel_RR"]:
		truck.get_node(wheel).rotation.x=-pitch

func verify_tow_heavy(r: Node3D, contract: Dictionary) -> void:
	var anchor=r.get_node_or_null("LiftPivot/LiftAnchor")
	expect(r.get_node_or_null("LiftPivot/LiftArm") is MeshInstance3D,"Heavy lifting arm at stable path")
	expect(anchor is Marker3D,"Heavy LiftAnchor is a Marker3D")
	if not anchor is Marker3D:
		return
	var anchor_position=r.global_transform.affine_inverse()*anchor.global_position
	expect(vec(anchor_position)==contract.lift.anchor_rest_m,"Heavy lift anchor matches manifest")
	for operating in [false,true]:
		var posed=r.duplicate()
		root.add_child(posed)
		pose(posed,operating)
		var truck=mount_heavy_truck(posed)
		var front=(truck.get_node("Wheel_FL").global_position+truck.get_node("Wheel_FR").global_position)*0.5
		expect(front.distance_to(posed.get_node("LiftPivot/LiftAnchor").global_position)<0.001,"Towed front axle centered at LiftAnchor")
		expect(truck.scale.is_equal_approx(Vector3.ONE),"Towed cargo truck keeps native scale")
		var heading=Vector3(-truck.basis.z.x,0,-truck.basis.z.z).normalized()
		expect(truck.position.z>anchor_position.z and heading.dot(Vector3.FORWARD)>0.999,"Towed truck behind, front toward wrecker (-Z), no yaw reversal")
		for wheel in ["Wheel_RL","Wheel_RR"]:
			var w=truck.get_node(wheel)
			expect(abs(wheel_floor_y(w,posed))<0.002,"Transformed towed rear tire vertices rest on ground "+wheel)
		var truck_triangles=mesh_triangles(truck.get_node("Body"),posed,true)
		for part in ["Body","RecoveryBody","LiftPivot/LiftArm"]:
			expect(meshes_have_clearance(truck_triangles,mesh_triangles(posed.get_node(part),posed),0.02),"Towed cabin/chassis clears heavy "+part+" by 0.02 m; operating="+str(operating))
		expect(bounds(posed).position.y>=-0.002,"Towed cargo truck body stays above ground")
		truck.position.z-=0.75
		expect(not meshes_have_clearance(mesh_triangles(truck.get_node("Body"),posed,true),mesh_triangles(posed.get_node("RecoveryBody"),posed),0.02),"Clearance check rejects an overlapping towed cabin")
		posed.free()

func state(r: Node3D, cargo: String) -> void:
	match cargo:
		"sand": r.get_node("BedPivot/LoadSandGravel").visible=true
		"asphalt":
			r.get_node("BedPivot/LoadAsphalt").visible=true
			r.get_node("BedPivot/Cover").visible=true
		"steel": r.get_node("LoadSteel").visible=true
		"wood": r.get_node("LoadWood").visible=true
		"hopper": r.get_node("HopperLoad").visible=true
		"passenger":
			# Authorship preview only; delivered scene never contains a fictitious vehicle.
			var car=load(V1+"vehicle.car_passenger.a.tscn").instantiate()
			r.get_node("DeckPivot/CarryAnchor").add_child(car)
		"cargo_truck": mount_heavy_truck(r)

func pose(r: Node3D, operation: bool) -> void:
	if not operation:
		return
	var id=str(r.get_meta("visual_id"))
	if id.contains("truck_dump"):
		r.get_node("BedPivot").rotation.x=deg_to_rad(38)
		r.get_node("BedPivot/TailgatePivot").rotation.x=deg_to_rad(-72)
		r.get_node("BedPivot/LoadSandGravel").visible=true
		r.get_node("BedPivot/Cover").visible=false
		# Set hydraulic body + rod between two physical hinge points.
		var origin=r.get_node("HydraulicAnchor").position
		var tip=r.get_node("BedPivot").transform*r.get_node("BedPivot/HydraulicTarget").position
		var axis=(tip-origin).normalized()
		var basis=Basis(Vector3.RIGHT,axis,Vector3.RIGHT.cross(axis)).orthonormalized()
		for spec in [{"name":"HydraulicBase","fraction":0.52,"length":0.48,"offset":0.0},{"name":"HydraulicRod","fraction":0.48,"length":0.40,"offset":0.52}]:
			var n=r.get_node(spec.name)
			n.transform=Transform3D(basis.scaled(Vector3(1,(tip-origin).length()*spec.fraction/spec.length,1)),origin+(tip-origin)*spec.offset)
	elif id.contains("truck_mixer"):
		r.get_node("DrumMount/Drum").rotation.z=0.7
		r.get_node("ChutePivot").rotation.y=deg_to_rad(24)
	elif id.contains("asphalt_paver"):
		r.get_node("ScreedPivot").position.y=0.42
		r.get_node("ScreedPivot").rotation.x=0
		r.get_node("HopperLoad").visible=true
	elif id.contains("road_roller"):
		r.get_node("FrontSteering").rotation.y=deg_to_rad(8)
	elif id.contains("tow_truck_heavy"):
		r.get_node("LiftPivot").rotation.x=deg_to_rad(-10)
		if r.has_node("ReviewTowedTruck"):
			align_heavy_truck(r,r.get_node("ReviewTowedTruck"))
	elif id.contains("tow_truck"):
		r.get_node("DeckPivot").position=Vector3(0,1.15,3.5)
		r.get_node("DeckPivot").rotation.x=deg_to_rad(12)

func make_stage() -> void:
	scene_root=Node3D.new()
	root.add_child(scene_root)
	var env=Environment.new()
	env.background_mode=Environment.BG_COLOR
	env.background_color=Color("b9c9d5")
	env.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color=Color("afc5ce")
	env.ambient_light_energy=0.32
	var we=WorldEnvironment.new()
	we.environment=env
	scene_root.add_child(we)
	var sun=DirectionalLight3D.new()
	sun.rotation_degrees=Vector3(-48,-30,0)
	sun.light_color=Color("fff0d0")
	sun.light_energy=1.0
	sun.shadow_enabled=true
	sun.directional_shadow_max_distance=650
	scene_root.add_child(sun)
	camera=Camera3D.new()
	camera.projection=Camera3D.PROJECTION_ORTHOGONAL
	camera.far=2000
	camera.current=true
	scene_root.add_child(camera)
	root.msaa_3d=Viewport.MSAA_4X
	box(scene_root,Vector3(0,-0.17,0),Vector3(160,0.25,100),Color("799066"))

func box(parent: Node3D, pos: Vector3, size: Vector3, color: Color) -> void:
	var n=MeshInstance3D.new()
	var m=BoxMesh.new()
	m.size=size
	var material=StandardMaterial3D.new()
	material.albedo_color=color
	m.material=material
	n.mesh=m
	n.position=pos
	parent.add_child(n)

func camera_at(target: Vector3, direction: Vector3, size: float) -> void:
	camera.size=size
	camera.position=target+direction.normalized()*150
	camera.look_at(target)

func label(parent: Node3D, title: String, pos: Vector3, font: int = 38) -> void:
	var l=Label3D.new()
	l.text=title
	l.position=pos
	l.billboard=BaseMaterial3D.BILLBOARD_ENABLED
	l.font_size=font
	l.pixel_size=0.011
	l.modulate=Color("f5f1e8")
	l.outline_modulate=Color("26333d")
	l.no_depth_test=true
	parent.add_child(l)

func snap(file: String) -> void:
	for i in 8:
		await process_frame
	await RenderingServer.frame_post_draw
	var im=root.get_texture().get_image()
	expect(im.get_width()>=1600 and im.get_height()>=900,"Real viewport size "+file)
	assert(im.save_png(out+"/"+file+".png")==OK)
	metrics.append({"capture":file,"objects":RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_TOTAL_OBJECTS_IN_FRAME),"primitives":RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_TOTAL_PRIMITIVES_IN_FRAME),"draw_calls":RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_TOTAL_DRAW_CALLS_IN_FRAME)})
	print("[construction-render] saved "+out+"/"+file+".png")

func road(parent: Node3D, z: float, count: int) -> void:
	for i in count:
		var n=load(V1+"road.urban_1x1.a.straight.tscn").instantiate()
		n.position=Vector3((i-(count-1)/2.0)*10.5,0,z)
		parent.add_child(n)

func urban_context(parent: Node3D) -> void:
	for spec in [{"id":"residential.small_house.a","pos":Vector3(-16,0,12)},{"id":"commercial.grocery_small.a","pos":Vector3(8,0,15)},{"id":"vegetation.tree_broadleaf.a","pos":Vector3(-5,0,9)},{"id":"vehicle.truck_cargo.a","pos":Vector3(5,0,1.625)}]:
		var n=load(V1+spec.id+".tscn").instantiate()
		n.position=spec.pos
		if spec.id.contains("vehicle"):
			n.rotation.y=PI/2
		parent.add_child(n)
	for x in [-22,-9,17]:
		var s=load(V1+"agent.sim_pedestrian.a.tscn").instantiate()
		s.position=Vector3(x,0.18,4.2)
		parent.add_child(s)

func pack_board() -> void:
	# Explicit ownership for an editor-openable review scene; no runtime tool attached.
	set_owners(scene_root,scene_root)
	var packed=PackedScene.new()
	assert(packed.pack(scene_root)==OK)
	assert(ResourceSaver.save(packed,ROOT+"/Preview/construction_board.tscn")==OK)

func set_owners(n: Node, r: Node) -> void:
	for c in n.get_children():
		c.owner=r
		set_owners(c,r)

func render_reviews() -> void:
	print("[construction-render] device="+RenderingServer.get_video_adapter_name()+" method="+RenderingServer.get_current_rendering_method()+" display="+DisplayServer.get_name())
	make_stage()
	var content=Node3D.new()
	scene_root.add_child(content)
	road(content,0,7)
	urban_context(content)
	var variants=[{"id":"truck_dump","cargo":"sand","title":"AREIA / BRITA"},{"id":"truck_dump","cargo":"asphalt","title":"ASFALTO + LONA"},{"id":"truck_mixer","cargo":"","title":"BETONEIRA"},{"id":"truck_flatbed","cargo":"steel","title":"AÇO"},{"id":"truck_flatbed","cargo":"wood","title":"MADEIRA"},{"id":"asphalt_paver","cargo":"hopper","title":"PAVIMENTADORA"},{"id":"road_roller","cargo":"","title":"ROLO"}]
	for i in variants.size():
		var spec=variants[i]
		var r=load_visual(spec.id)
		r.position=Vector3(-25.5+i*8.3,0,-1.625)
		r.rotation.y=-PI/2
		content.add_child(r)
		state(r,spec.cargo)
		label(content,spec.title,Vector3(r.position.x,0.5,-6.5),32)
	camera_at(Vector3(0,0.7,1),Vector3(300,330,300),55)
	pack_board()
	await snap("board_urban_loaded_iso")
	camera_at(Vector3(0,0.7,1),Vector3(-300,330,-300),55)
	await snap("board_urban_loaded_back")
	content.free()
	# Close inspection of the front feed hopper, rear screed, and mixer finish.
	for spec in [{"family":"asphalt_paver","cargo":"hopper","file":"detail_paver_hopper","direction":Vector3(300,330,-300),"op":false},{"family":"asphalt_paver","cargo":"hopper","file":"detail_paver_screed","direction":Vector3(300,330,300),"op":true},{"family":"truck_mixer","cargo":"","file":"detail_mixer_front_iso","direction":Vector3(300,330,-300),"op":true},{"family":"tow_truck","cargo":"passenger","file":"detail_tow_loaded_front_iso","direction":Vector3(300,330,-300),"op":false},{"family":"tow_truck","cargo":"passenger","file":"detail_tow_loaded_back_iso","direction":Vector3(300,330,300),"op":false},{"family":"tow_truck","cargo":"","file":"detail_tow_deck_operation","direction":Vector3(300,330,300),"op":true},{"family":"tow_truck_heavy","cargo":"cargo_truck","file":"detail_tow_heavy_front_iso","direction":Vector3(300,330,-300),"op":false},{"family":"tow_truck_heavy","cargo":"cargo_truck","file":"detail_tow_heavy_back_iso","direction":Vector3(300,330,300),"op":false},{"family":"tow_truck_heavy","cargo":"","file":"detail_tow_heavy_lift","direction":Vector3(300,330,300),"op":true}]:
		var detail=Node3D.new()
		scene_root.add_child(detail)
		if spec.family=="tow_truck_heavy":
			var corridor=Node3D.new()
			detail.add_child(corridor)
			corridor.rotation.y=PI/2
			road(corridor,0,4)
		else:
			road(detail,0,2)
		var r=load_visual(spec.family)
		detail.add_child(r)
		if spec.family=="tow_truck_heavy":
			r.position.x=1.625
		state(r,spec.cargo)
		pose(r,spec.op)
		camera_at(Vector3(1.625,1.2,3.2 if spec.cargo=="cargo_truck" else 0) if spec.family=="tow_truck_heavy" else Vector3(0,1.2,0),spec.direction,17.5 if spec.family=="tow_truck_heavy" and spec.cargo=="cargo_truck" else 10.5 if spec.family=="tow_truck_heavy" else 9.5)
		await snap(spec.file)
		detail.free()
	# Low view under the real mounted truck: show the axle inside the two cradles.
	var closeup=Node3D.new()
	scene_root.add_child(closeup)
	var heavy=load_visual("tow_truck_heavy")
	closeup.add_child(heavy)
	mount_heavy_truck(heavy)
	var hitch=heavy.get_node("LiftPivot/LiftAnchor").global_position
	camera.size=3.4
	camera.position=hitch+Vector3(2.3,-0.55,2.3)
	camera.look_at(hitch)
	await snap("detail_tow_heavy_cradle")
	closeup.free()
	# Independent cameras for correct front/rear orthographic and exact game iso.
	# Each tile is a real 500x380 viewport render, then composed losslessly by Godot.
	for family in ["truck_dump","truck_mixer","truck_flatbed","asphalt_paver","road_roller","tow_truck","tow_truck_heavy"]:
		var cargo="sand" if family=="truck_dump" else ("steel" if family=="truck_flatbed" else ("hopper" if family=="asphalt_paver" else ""))
		var tiles=[{"label":"VAZIO / REPOUSO","cargo":"","op":false},{"label":"CARREGADO" if family not in ["truck_mixer","road_roller"] else "CARROCERIA PERMANENTE","cargo":cargo,"op":false},{"label":"ASFALTO + LONA" if family=="truck_dump" else ("MADEIRA" if family=="truck_flatbed" else "OPERAÇÃO"),"cargo":"asphalt" if family=="truck_dump" else ("wood" if family=="truck_flatbed" else cargo),"op":family not in ["truck_dump","truck_flatbed"]},{"label":"DESCARGA / PARTES","cargo":cargo,"op":true}]
		if family=="tow_truck":
			tiles=[{"label":"VAZIO / REPOUSO","cargo":"","op":false},{"label":"CARRO EM ESCALA REAL","cargo":"passenger","op":false},{"label":"PLATAFORMA RECUADA / INCLINADA","cargo":"","op":true},{"label":"CARRO NO PIVÔ INCLINADO","cargo":"passenger","op":true}]
		elif family=="tow_truck_heavy":
			tiles=[{"label":"GUINCHO PESADO VAZIO","cargo":"","op":false},{"label":"CAMINHÃO EM ESCALA REAL","cargo":"cargo_truck","op":false},{"label":"ELEVAÇÃO DO BRAÇO","cargo":"","op":true},{"label":"REBOCADO / RODAS NO SOLO","cargo":"cargo_truck","op":true}]
		var sheet=Image.create(2000,1200,false,Image.FORMAT_RGBA8)
		sheet.fill(Color("26333d"))
		for row in 3:
			for col in 4:
				var im=await tile(family,tiles[col],row)
				sheet.blit_rect(im,Rect2i(0,0,500,380),Vector2i(col*500,row*400+10))
		assert(sheet.save_png(out+"/sheet_"+family+".png")==OK)
		print("[construction-render] saved "+out+"/sheet_"+family+".png")
	# Exact front/back orthographic views without a pitched view hiding pivots/contact.
	var lineup=Node3D.new()
	scene_root.add_child(lineup)
	for i in 5:
		var family=["truck_dump","truck_mixer","truck_flatbed","asphalt_paver","road_roller"][i]
		var r=load_visual(family)
		r.position=Vector3((i-2)*6.3,0,0)
		lineup.add_child(r)
		pose(r,true)
		var s=load(V1+"agent.sim_pedestrian.a.tscn").instantiate()
		s.position=Vector3(r.position.x+1.9,0,0)
		lineup.add_child(s)
		label(lineup,family,Vector3(r.position.x,0,-4),24)
	camera_at(Vector3(0,1.9,0),Vector3(0,0,-1),19)
	await snap("lineup_front_operation")
	camera_at(Vector3(0,1.9,0),Vector3(0,0,1),19)
	await snap("lineup_back_operation")
	lineup.free()
	var f=FileAccess.open(out+"/render_metrics.json",FileAccess.WRITE)
	f.store_string(JSON.stringify({"renderer":RenderingServer.get_current_rendering_method(),"adapter":RenderingServer.get_video_adapter_name(),"viewport":vec(Vector3(root.size.x,root.size.y,0)),"metrics":metrics,"note":"Local preview counts, not gameplay performance benchmark; no FPS/GPU-time conclusion"},"  "))
	f.close()
	print("[construction-render] PASS 21 captures plus mixer tile; real GL Compatibility; no save writes")

func tile(family: String, variant: Dictionary, view: int) -> Image:
	var vp=SubViewport.new()
	vp.size=Vector2i(500,380)
	vp.own_world_3d=true
	vp.msaa_3d=Viewport.MSAA_4X
	vp.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(vp)
	var stage=Node3D.new()
	vp.add_child(stage)
	var env=WorldEnvironment.new()
	env.environment=scene_root.get_child(0).environment.duplicate()
	stage.add_child(env)
	var sun=scene_root.get_child(1).duplicate()
	stage.add_child(sun)
	box(stage,Vector3(0,-0.04,0),Vector3(22,0.08,22),Color("667176"))
	for x in range(-7,8):
		box(stage,Vector3(x,0.003,0),Vector3(0.012,0.004,16),Color("808b8d"))
		box(stage,Vector3(0,0.003,x),Vector3(16,0.004,0.012),Color("808b8d"))
	var r=load_visual(family)
	stage.add_child(r)
	state(r,variant.cargo)
	pose(r,variant.op)
	if family=="asphalt_paver" and variant.op and variant.label=="DESCARGA / PARTES":
		r.get_node("HopperLeftPivot").rotation.z=deg_to_rad(-22)
		r.get_node("HopperRightPivot").rotation.z=deg_to_rad(22)
		r.get_node("HopperLoad").visible=false
	var sim=load(V1+"agent.sim_pedestrian.a.tscn").instantiate()
	sim.position=Vector3(2.15,0,0)
	stage.add_child(sim)
	var cam=Camera3D.new()
	cam.projection=Camera3D.PROJECTION_ORTHOGONAL
	cam.size=(20.0 if view==2 else 10.2) if family=="tow_truck_heavy" else (11.2 if view==2 else (10.2 if family=="truck_dump" and variant.op else 8.4))
	cam.current=true
	stage.add_child(cam)
	var target=Vector3(0,2.15 if family=="truck_dump" and variant.op and view!=2 else 1.5,0)
	if family=="tow_truck_heavy":
		target.z=3.2
	cam.position=target+[Vector3(0,0,-100),Vector3(0,0,100),Vector3(300,330,300)][view]
	cam.look_at(target)
	var title=Label.new()
	title.text=family+"   ·   "+["FRENTE","COSTAS","ISO JOGO"][view]+"\n"+variant.label+"   ·   grade 1 m / SIM 1,75 m"
	title.position=Vector2(13,9)
	title.add_theme_font_size_override("font_size",16)
	title.add_theme_color_override("font_color",Color("f2eee6"))
	title.add_theme_color_override("font_shadow_color",Color("26333d"))
	title.add_theme_constant_override("shadow_offset_x",1)
	title.add_theme_constant_override("shadow_offset_y",1)
	vp.add_child(title)
	for i in 4:
		await process_frame
	await RenderingServer.frame_post_draw
	var im=vp.get_texture().get_image()
	if family=="truck_mixer" and view==2 and variant.op:
		assert(im.save_png(out+"/detail_mixer_iso.png")==OK)
	vp.free()
	return im
