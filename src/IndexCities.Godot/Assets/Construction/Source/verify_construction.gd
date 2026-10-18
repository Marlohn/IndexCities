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

func run() -> void:
	out=ProjectSettings.globalize_path("res://../../artifacts/verification/construction-models")
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="):
			out=arg.substr(6)
	DirAccess.make_dir_recursive_absolute(out)
	var manifest=JSON.parse_string(FileAccess.get_file_as_string(ROOT+"/manifest.construction.json"))
	expect(manifest.entries.size()==6,"Six families")
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
			expect(not(n is CollisionObject3D or n is Light3D or n is Timer or n is AnimationPlayer or n is GPUParticles3D),"No runtime activity "+str(n.name))
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
		if e.visual_id=="vehicle.tow_truck.a":
			verify_tow_truck(r,e.contract)
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
	for spec in [{"family":"asphalt_paver","cargo":"hopper","file":"detail_paver_hopper","direction":Vector3(300,330,-300),"op":false},{"family":"asphalt_paver","cargo":"hopper","file":"detail_paver_screed","direction":Vector3(300,330,300),"op":true},{"family":"truck_mixer","cargo":"","file":"detail_mixer_front_iso","direction":Vector3(300,330,-300),"op":true},{"family":"tow_truck","cargo":"passenger","file":"detail_tow_loaded_front_iso","direction":Vector3(300,330,-300),"op":false},{"family":"tow_truck","cargo":"passenger","file":"detail_tow_loaded_back_iso","direction":Vector3(300,330,300),"op":false},{"family":"tow_truck","cargo":"","file":"detail_tow_deck_operation","direction":Vector3(300,330,300),"op":true}]:
		var detail=Node3D.new()
		scene_root.add_child(detail)
		road(detail,0,2)
		var r=load_visual(spec.family)
		detail.add_child(r)
		state(r,spec.cargo)
		pose(r,spec.op)
		camera_at(Vector3(0,1.2,0),spec.direction,9.5)
		await snap(spec.file)
		detail.free()
	# Independent cameras for correct front/rear orthographic and exact game iso.
	# Each tile is a real 500x380 viewport render, then composed losslessly by Godot.
	for family in ["truck_dump","truck_mixer","truck_flatbed","asphalt_paver","road_roller","tow_truck"]:
		var cargo="sand" if family=="truck_dump" else ("steel" if family=="truck_flatbed" else ("hopper" if family=="asphalt_paver" else ""))
		var tiles=[{"label":"VAZIO / REPOUSO","cargo":"","op":false},{"label":"CARREGADO" if family not in ["truck_mixer","road_roller"] else "CARROCERIA PERMANENTE","cargo":cargo,"op":false},{"label":"ASFALTO + LONA" if family=="truck_dump" else ("MADEIRA" if family=="truck_flatbed" else "OPERAÇÃO"),"cargo":"asphalt" if family=="truck_dump" else ("wood" if family=="truck_flatbed" else cargo),"op":family not in ["truck_dump","truck_flatbed"]},{"label":"DESCARGA / PARTES","cargo":cargo,"op":true}]
		if family=="tow_truck":
			tiles=[{"label":"VAZIO / REPOUSO","cargo":"","op":false},{"label":"CARRO EM ESCALA REAL","cargo":"passenger","op":false},{"label":"PLATAFORMA RECUADA / INCLINADA","cargo":"","op":true},{"label":"CARRO NO PIVÔ INCLINADO","cargo":"passenger","op":true}]
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
	print("[construction-render] PASS 16 captures plus mixer tile; real GL Compatibility; no save writes")

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
	cam.size=11.2 if view==2 else (10.2 if family=="truck_dump" and variant.op else 8.4)
	cam.current=true
	stage.add_child(cam)
	var target=Vector3(0,2.15 if family=="truck_dump" and variant.op and view!=2 else 1.5,0)
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
