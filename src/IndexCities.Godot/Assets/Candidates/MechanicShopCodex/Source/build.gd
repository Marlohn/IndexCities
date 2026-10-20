## Oficina original, preparada para integracao futura. Somente autoria visual.
## godot --headless --path src/IndexCities.Godot --script res://Assets/Candidates/MechanicShopCodex/Source/build.gd
extends SceneTree

const Geo := preload("res://Assets/Candidates/MechanicShopCodex/Source/geometry.gd")
const ROOT := "res://Assets/Candidates/MechanicShopCodex"
const CREAM := Color("d9cdb4")
const TRIM := Color("f1e5c9")
const BLUE := Color("335c66")
const BLUE_DARK := Color("243e48")
const ORANGE := Color("bc613d")
const ROOF := Color("3c4a52")
const BRICK := Color("9f715b")
const STEEL := Color("829597")
const GLASS := Color("355466")
const DARK := Color("29363b")

var geo = Geo.new()
var asset: Node3D
var materials: Dictionary = {}
var meshes: Array = []
var text_meshes: Array = []
var failures: Array = []
var palette_image: Image
var palette_texture: ImageTexture
var palette_colors: Dictionary = {}

func _initialize() -> void:
	for folder in ["Meshes","Materials","Scenes","Preview"]:
		DirAccess.make_dir_recursive_absolute(ROOT.path_join(folder))
	asset = Node3D.new()
	asset.name = "MechanicShopCodex"
	asset.set_meta("visual_id", "candidate.mechanic_shop.codex.a")
	asset.set_meta("status", "asset futuro; estetica pendente de revisao humana; sem gameplay")
	asset.set_meta("candidate_lot_m", Vector2(22,17))
	asset.set_meta("front", "-Z")
	_make_materials()
	_ground()
	_save_part("Site")
	_shell()
	_roof()
	_office()
	_save_part("Building")
	_front_details()
	_rear_details()
	_save_part("ArchitecturalDetails")
	_shutter()
	_save_part("ShutterClosed", Vector3(-1.45,3.5,-0.99))
	_interior()
	_save_part("WorkshopFurnishings")
	_lift()
	_save_part("LiftArms", Vector3(-6.905,0.35,2.35))
	_yard_props()
	_save_part("YardProps")
	_signs()
	_markers()
	_finish_palette()
	var packed := PackedScene.new()
	_check(packed.pack(asset) == OK, "empacotar cena")
	_check(ResourceSaver.save(packed,ROOT+"/Scenes/mechanic_shop_codex.a.tscn") == OK,"salvar cena")
	# Intercambio portavel, sem scripts nem dependencia dos assets V1.
	var gltf := GLTFDocument.new()
	var state := GLTFState.new()
	_check(gltf.append_from_scene(asset,state) == OK,"preparar GLB")
	_check(gltf.write_to_filesystem(state,ROOT+"/Scenes/mechanic_shop_codex.a.glb") == OK,"exportar GLB")
	_manifest()
	asset.free()
	if not failures.is_empty():
		for failure in failures:
			printerr(failure)
		quit(1)
	else:
		print("[mechanic-codex-build] OK parts=%d triangles=%d" % [meshes.size(), _triangle_total()])
		quit(0)

func _check(ok: bool, label: String) -> void:
	if not ok:
		failures.append(label)

func _make_materials() -> void:
	palette_image = Image.create(32,32,false,Image.FORMAT_RGBA8)
	palette_image.fill(Color.WHITE)
	var palette_path := ROOT+"/Materials/palette.res"
	_check(ResourceSaver.save(ImageTexture.create_from_image(palette_image),palette_path) == OK,"paleta inicial")
	palette_texture = load(palette_path)
	for kind in ["paint","metal","glass","rubber","lettering"]:
		var m := StandardMaterial3D.new()
		m.resource_name = "mechanic_codex_"+kind
		m.albedo_texture = palette_texture
		m.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
		m.roughness = 0.85
		if kind == "metal":
			m.roughness = 0.48
			m.metallic = 0.25
		elif kind == "glass":
			m.roughness = 0.23
			m.metallic = 0.28
		elif kind == "rubber":
			m.roughness = 0.97
		elif kind == "lettering":
			m.albedo_texture = null
			m.albedo_color = TRIM
		_check(ResourceSaver.save(m,ROOT+"/Materials/"+kind+".tres") == OK,"material "+kind)
		materials[kind] = load(ROOT+"/Materials/"+kind+".tres")

func _palette_uv(color: Color) -> Vector2:
	var key := color.to_rgba32()
	if not palette_colors.has(key):
		var index := palette_colors.size()
		if index >= 1024:
			failures.append("paleta excedeu 1024 cores")
			return Vector2.ZERO
		palette_colors[key] = index
		palette_image.set_pixel(index%32,index/32,color)
	var slot: int = palette_colors[key]
	return Vector2((slot%32+0.5)/32.0,(slot/32+0.5)/32.0)

func _finish_palette() -> void:
	# Instancia nova conserva a imagem CPU tambem com o renderer headless.
	palette_texture = ImageTexture.create_from_image(palette_image)
	_check(ResourceSaver.save(palette_texture,ROOT+"/Materials/palette.res") == OK,"salvar atlas de cores")
	for kind in materials:
		if kind != "lettering":
			materials[kind].albedo_texture = palette_texture
			_check(ResourceSaver.save(materials[kind],ROOT+"/Materials/"+kind+".tres") == OK,"material final "+kind)
	_check(palette_image.save_png(ROOT+"/Materials/palette.png") == OK,"paleta PNG de autoria")

func _save_part(label: String, pivot: Vector3 = Vector3.ZERO) -> void:
	for slot in geo.slots.values():
		for i in slot.v.size():
			slot.v[i] -= pivot
	var mesh: ArrayMesh = geo.commit(materials,Callable(self,"_palette_uv"))
	var path := ROOT+"/Meshes/"+label.to_snake_case()+".res"
	_check(ResourceSaver.save(mesh,path) == OK,"malha "+label)
	var instance := MeshInstance3D.new()
	instance.name = label
	instance.mesh = load(path)
	instance.position = pivot
	asset.add_child(instance)
	instance.owner = asset
	var triangles := 0
	for i in mesh.get_surface_count():
		triangles += mesh.surface_get_array_len(i)/3
	meshes.append({"name":label,"path":path,"triangles":triangles,"surfaces":mesh.get_surface_count(),"pivot":[pivot.x,pivot.y,pivot.z],"local_aabb":_aabb(mesh.get_aabb()),"aabb":_aabb(AABB(mesh.get_aabb().position+pivot,mesh.get_aabb().size))})
	geo = Geo.new()

func _box(p: Vector3, s: Vector3, c: Color, b: float = 0.025, slot: String = "paint") -> void:
	geo.box(p,s,c,b,slot)

func _ground() -> void:
	_box(Vector3(0,-0.12,0),Vector3(21.98,0.24,16.98),Color("898d83"),0.08)
	# Concreto em placas: sutis variacoes de cor, juntas rebaixadas.
	for ix in 11:
		for iz in 8:
			var shade := 0.014*sin(ix*15.3+iz*6.7)
			_box(Vector3(-10.0+ix*2,0.005,-7.44+iz*2),Vector3(1.988,0.02,1.988),Color("aaaea3").lightened(shade),0.006)
	# Passeio frontal pertence ao lote, sem rua embutida.
	_box(Vector3(0,0.045,-7.97),Vector3(21.98,0.10,1.03),Color("c9c7b7"),0.035)
	for i in 22:
		_box(Vector3(-10.5+i,0.098,-7.97),Vector3(0.99,0.007,0.99),Color("d2cdbb").darkened(0.009*(i%3)),0.002)
	# Canal de drenagem fora do caminho pedonal.
	_box(Vector3(-3.3,0.03,-2.7),Vector3(12.6,0.035,0.22),Color("606968"),0.01,"metal")
	for i in 70:
		_box(Vector3(-9.5+i*0.18,0.052,-2.7),Vector3(0.035,0.015,0.18),STEEL,0.003,"metal")
	# Guias curtas, fora das duas faixas de aproximacao.
	for x in [-10.45,10.45]:
		_box(Vector3(x,0.12,1.55),Vector3(0.18,0.24,12.6),Color("c7c4b2"),0.045)

func _shell() -> void:
	# Galpao: paredes com espessura, baia vazia de 4 m e fachada dividida.
	_box(Vector3(-3.65,2.2,7.25),Vector3(12.5,4.4,0.30),CREAM,0.05)
	for x in [-9.75,2.45]:
		_box(Vector3(x,2.2,3.17),Vector3(0.30,4.4,8.45),CREAM,0.045)
	for part in [{"x":-9.35,"w":0.8},{"x":-4.45,"w":0.8},{"x":1.85,"w":1.4}]:
		_box(Vector3(part.x,2.1,-0.98),Vector3(part.w,4.2,0.34),CREAM,0.055)
	# Pilar central e verga mantem vãos livres x[-8.95,-4.85] e [-4.05,1.15].
	_box(Vector3(-3.65,4.0,-0.98),Vector3(12.5,0.80,0.36),CREAM,0.045)
	_box(Vector3(-3.65,0.018,3.15),Vector3(12.05,0.04,8.0),Color("888d84"),0.01)
	# Rodape e juntas da alvenaria: modelados apenas onde sao visiveis.
	for x in [-9.93,2.63]:
		_box(Vector3(x,0.38,3.15),Vector3(0.065,0.76,8.5),BRICK,0.012)
	_box(Vector3(-3.65,0.38,7.435),Vector3(12.5,0.76,0.065),BRICK,0.012)
	for i in 4:
		var y := 0.11+i*0.18
		for x in [-9.969,2.669]:
			_box(Vector3(x,y,3.15),Vector3(0.008,0.014,8.48),Color("c0a58b"),0.001)
		for j in 15:
			var z := -0.84+j*0.56+(i%2)*0.28
			for x in [-9.97,2.67]:
				_box(Vector3(x,y+0.085,z),Vector3(0.008,0.16,0.014),Color("b29a81"),0.001)
	# Ombreiras com rebaixo escuro e metal exterior.
	for bounds in [Vector2(-8.95,-4.85),Vector2(-4.05,1.15)]:
		for x in [bounds.x,bounds.y]:
			_box(Vector3(x,1.76,-1.055),Vector3(0.17,3.52,0.42),BLUE_DARK,0.025,"metal")
			_box(Vector3(x,1.78,-1.29),Vector3(0.11,3.56,0.08),TRIM,0.022)
		_box(Vector3((bounds.x+bounds.y)/2,3.51,-1.14),Vector3(bounds.y-bounds.x+0.17,0.16,0.35),BLUE_DARK,0.025,"metal")
	# Faixa superior, testa levemente arredondada e abrigo dos portoes.
	_box(Vector3(-3.65,4.22,-1.12),Vector3(12.72,0.55,0.28),BLUE,0.075)
	_box(Vector3(-3.65,3.83,-1.43),Vector3(12.95,0.13,1.05),ROOF,0.045,"metal")
	_box(Vector3(-3.65,3.765,-1.925),Vector3(12.92,0.11,0.075),ORANGE,0.02,"metal")
	for x in [-8.8,-4.5,0.75]:
		geo.beam(Vector3(x,3.42,-1.10),Vector3(x,3.76,-1.82),0.065,0.065,BLUE_DARK,"metal")

func _roof() -> void:
	# Cobertura industrial de duas aguas com costuras, remates e lanternim.
	var x0 := -10.08
	var x1 := 2.78
	var mid := -3.65
	var z0 := -1.25
	var z1 := 7.62
	var low := 4.48
	var high := 5.46
	for side in [-1,1]:
		var x: float = x0 if side == -1 else x1
		var n := Vector3(side*0.15,1,0).normalized()
		geo.face([Vector3(x,low,z0),Vector3(mid,high,z0),Vector3(mid,high,z1),Vector3(x,low,z1)],n,ROOF,"metal")
		for i in 15:
			var z := z0 + i*(z1-z0)/14
			geo.beam(Vector3(x,low+0.025,z),Vector3(mid,high+0.025,z),0.028,0.035,ROOF.lightened(0.09),"metal")
		_box(Vector3(x,low-0.04,(z0+z1)/2),Vector3(0.14,0.19,z1-z0+0.08),ROOF.darkened(0.12),0.035,"metal")
		for z in [z0,z1]:
			geo.beam(Vector3(x,low,z),Vector3(mid,high,z),0.12,0.11,ROOF.lightened(0.06),"metal")
	_box(Vector3(mid,high+0.025,(z0+z1)/2),Vector3(0.16,0.12,z1-z0+0.08),ROOF.lightened(0.09),0.04,"metal")
	# Fechamento das empenas.
	for z in [-1.09,7.32]:
		geo.face([Vector3(-9.9,4.35,z),Vector3(2.6,4.35,z),Vector3(mid,5.40,z)],Vector3(0,0,-1 if z<0 else 1),CREAM)
	# Lanternim longitudinal baixo: subvolumes repetiveis legiveis de cima.
	_box(Vector3(mid,5.5,3.5),Vector3(2.2,0.73,5.0),BLUE_DARK,0.04)
	for side in [-1,1]:
		for i in 6:
			_box(Vector3(mid+side*1.105,5.55,1.45+i*0.82),Vector3(0.04,0.43,0.71),GLASS.lightened(0.10),0.01,"glass")
	_box(Vector3(mid,5.93,3.5),Vector3(2.55,0.13,5.32),ROOF,0.055,"metal")
	for i in 7:
		_box(Vector3(mid,6.005,1.0+i*0.83),Vector3(2.5,0.024,0.025),ROOF.lightened(0.10),0.005,"metal")
	# Calhas e tubos com cotovelos, fora das aberturas.
	for x in [-10.02,2.73]:
		_box(Vector3(x,4.38,3.17),Vector3(0.17,0.15,9.0),ROOF,0.035,"metal")
		for z in [6.9]:
			geo.cylinder(Vector3(x,2.10,z),0.055,4.1,ROOF,12,Vector3.UP,"metal")
			for y in [0.5,2.5,3.8]:
				_box(Vector3(x, y,z),Vector3(0.16,0.06,0.17),STEEL,0.01,"metal")
	# Extrator com venezianas, nao emissivo e sem animacao.
	geo.cylinder(Vector3(-8.1,5.03,5.75),0.26,0.62,STEEL,16,Vector3.UP,"metal")
	for y in [5.28,5.37,5.46]:
		geo.cylinder(Vector3(-8.1,y,5.75),0.35,0.045,ROOF,16,Vector3.UP,"metal")
	geo.cylinder(Vector3(-8.1,5.53,5.75),0.39,0.12,STEEL,16,Vector3.UP,"metal",0.13)

func _office() -> void:
	# Recepcao lateral: volume mais baixo, vidro com rebaixo e cobertura propria.
	_box(Vector3(6.04,1.65,2.47),Vector3(6.82,3.3,9.26),CREAM,0.065)
	_box(Vector3(6.04,0.26,2.47),Vector3(6.88,0.52,9.32),BRICK,0.045)
	_box(Vector3(6.04,3.33,2.47),Vector3(7.24,0.22,9.63),ROOF,0.065,"metal")
	# Telhado baixo com painéis separados e beirais generosos.
	for i in 12:
		_box(Vector3(6.04,3.465,-2.15+i*0.83),Vector3(7.18,0.025,0.025),ROOF.lightened(0.10),0.007,"metal")
	# Vitrine frontal e porta sao overlays de geometria recuada visualmente.
	_box(Vector3(6.05,1.66,-2.185),Vector3(5.82,2.39,0.10),BLUE_DARK,0.055,"metal")
	for x in [3.85,5.23,6.62]:
		_box(Vector3(x,1.72,-2.255),Vector3(1.23,1.80,0.04),GLASS,0.015,"glass")
		# Reflexos largos discretos desenhados na geometria do vidro.
		geo.face([Vector3(x-0.57,0.86,-2.28),Vector3(x-0.15,0.86,-2.28),Vector3(x+0.55,2.58,-2.28),Vector3(x+0.13,2.58,-2.28)],Vector3.FORWARD,Color("577985"),"glass")
		_box(Vector3(x,0.69,-2.30),Vector3(1.31,0.19,0.16),BLUE,0.025,"metal")
	for x in [3.16,4.54,5.93,7.31,8.95]:
		_box(Vector3(x,1.65,-2.31),Vector3(0.075,2.28,0.10),TRIM,0.015,"metal")
	# Porta, soleira, puxador.
	_box(Vector3(8.13,1.22,-2.26),Vector3(1.45,2.42,0.13),BLUE,0.04,"metal")
	_box(Vector3(8.13,1.47,-2.34),Vector3(1.15,1.59,0.035),GLASS,0.02,"glass")
	_box(Vector3(8.13,0.16,-2.42),Vector3(1.62,0.14,0.45),Color("b1ada0"),0.035)
	geo.beam(Vector3(7.68,1.0,-2.42),Vector3(7.68,1.35,-2.42),0.035,0.04,TRIM,"metal")
	# Marquise simples de oficina, sem toldo de cafeteria.
	_box(Vector3(6.1,2.93,-2.65),Vector3(7.05,0.13,1.30),ORANGE,0.045,"metal")
	_box(Vector3(6.1,2.84,-3.25),Vector3(7.05,0.14,0.07),ORANGE.darkened(0.18),0.015,"metal")
	for x in [3.2,8.9]:
		geo.beam(Vector3(x,2.42,-2.3),Vector3(x,2.87,-3.04),0.055,0.055,BLUE_DARK,"metal")
	# Janela lateral dividida, com peitoril e sombreado superior.
	_box(Vector3(9.475,1.85,0.55),Vector3(0.08,1.50,3.95),BLUE_DARK,0.04,"metal")
	for z in [-0.85,0.55,1.95]:
		_box(Vector3(9.525,1.88,z),Vector3(0.035,1.24,1.25),GLASS,0.015,"glass")
		_box(Vector3(9.56,1.88,z+0.67),Vector3(0.055,1.35,0.055),TRIM,0.01,"metal")
	_box(Vector3(9.58,1.08,0.55),Vector3(0.24,0.10,4.14),TRIM,0.025)
	_box(Vector3(9.55,2.68,0.55),Vector3(0.20,0.11,4.17),TRIM,0.025)

func _front_details() -> void:
	# Rolamento recolhido acima da baia aberta, sem esconder a profundidade.
	_box(Vector3(-6.9,3.39,-0.92),Vector3(4.04,0.26,0.29),BLUE,0.045,"metal")
	for y in [3.3,3.4,3.5]:
		_box(Vector3(-6.9,y,-1.09),Vector3(3.99,0.025,0.025),BLUE.lightened(0.12),0.003,"metal")
	# Ombreiras com protecao de impacto em ocre.
	for x in [-8.95,-4.85,-4.05,1.15]:
		_box(Vector3(x,0.46,-1.355),Vector3(0.15,0.89,0.07),Color("c7a867"),0.02,"metal")
		for y in [0.25,0.55,0.85]:
			_box(Vector3(x,y,-1.395),Vector3(0.15,0.035,0.012),DARK,0.001)
	# Luminarias: geometria sem luz ligada ou garantia de funcionamento.
	for x in [-8.95,-4.45,1.9]:
		geo.beam(Vector3(x,3.96,-1.24),Vector3(x,3.87,-1.55),0.045,0.045,BLUE_DARK,"metal")
		geo.cylinder(Vector3(x,3.84,-1.56),0.18,0.10,BLUE,16,Vector3.UP,"metal",0.07)
		geo.cylinder(Vector3(x,3.779,-1.56),0.135,0.018,TRIM,16)

func _shutter() -> void:
	_box(Vector3(-1.45,1.745,-0.99),Vector3(5.12,3.45,0.13),BLUE_DARK,0.03,"metal")
	for i in 22:
		var y := 0.105+i*0.153
		_box(Vector3(-1.45,y,-1.083),Vector3(5.04,0.139,0.055),BLUE.lightened(0.012*(i%3)),0.015,"metal")
	for x in [-2.8,-0.1]:
		_box(Vector3(x,2.48,-1.125),Vector3(0.86,0.37,0.055),BLUE_DARK,0.025,"metal")
		_box(Vector3(x,2.48,-1.16),Vector3(0.72,0.25,0.025),GLASS.lightened(0.04),0.015,"glass")
	_box(Vector3(-1.45,0.12,-1.14),Vector3(5.05,0.13,0.11),STEEL,0.02,"metal")
	_box(Vector3(-1.45,1.06,-1.14),Vector3(0.40,0.055,0.07),DARK,0.015,"metal")

func _interior() -> void:
	# Fundo mais escuro e piso por baia; nenhum veiculo/figurante embutido.
	_box(Vector3(-6.9,1.7,6.95),Vector3(4.9,3.36,0.07),Color("69766f"),0.01)
	_box(Vector3(-6.9,0.047,2.8),Vector3(4.0,0.018,7.36),Color("787f74"),0.005)
	# Bancada, tampo de madeira, painel perfurado e armario.
	_box(Vector3(-6.95,0.44,6.20),Vector3(3.17,0.83,0.68),BLUE_DARK,0.055,"metal")
	_box(Vector3(-6.95,0.91,6.20),Vector3(3.36,0.12,0.86),Color("ae875d"),0.04)
	_box(Vector3(-6.95,1.81,6.79),Vector3(3.31,1.38,0.09),Color("8b9385"),0.035)
	for ix in 18:
		for iy in 6:
			_box(Vector3(-8.43+ix*0.175,1.29+iy*0.18,6.73),Vector3(0.025,0.025,0.009),Color("626f68"),0.002)
	for i in 5:
		var x := -8.0+i*0.43
		geo.beam(Vector3(x,1.42,6.69),Vector3(x+0.035,1.99-i*0.045,6.69),0.045,0.022,STEEL,"metal")
		geo.cylinder(Vector3(x+0.035,1.99-i*0.045,6.69),0.065,0.027,STEEL,8,Vector3.FORWARD,"metal")
	for x in [-7.9,-6.85,-5.8]:
		_box(Vector3(x,0.5,5.845),Vector3(0.94,0.65,0.035),BLUE,0.025,"metal")
		_box(Vector3(x,0.70,5.81),Vector3(0.30,0.03,0.035),STEEL,0.005,"metal")
	# Carrinho de ferramentas recolhido ao lado, sem cruzar trajeto da baia.
	_box(Vector3(-9.05,0.52,1.8),Vector3(0.65,0.78,1.13),ORANGE,0.055,"metal")
	for y in [0.28,0.43,0.58,0.73]:
		_box(Vector3(-8.71,y,1.8),Vector3(0.025,0.035,0.91),DARK,0.005,"metal")
	_box(Vector3(-9.05,0.95,1.8),Vector3(0.73,0.09,1.20),DARK,0.035,"metal")
	for x in [-9.26,-8.84]:
		for z in [1.4,2.2]:
			geo.cylinder(Vector3(x,0.10,z),0.085,0.05,DARK,12,Vector3.RIGHT,"rubber")
	# Travessas da estrutura na profundidade, nao plano preto falso.
	for z in [1.4,4.0,6.5]:
		geo.beam(Vector3(-9.58,4.18,z),Vector3(2.28,4.18,z),0.12,0.12,BLUE_DARK,"metal")

func _lift() -> void:
	# Equipamento separado: pose estatica baixa. Sem autoplay ou carro gratuito.
	for x in [-8.28,-5.53]:
		_box(Vector3(x,0.085,2.35),Vector3(0.56,0.14,0.68),BLUE_DARK,0.035,"metal")
		_box(Vector3(x,1.57,2.35),Vector3(0.28,3.02,0.34),BLUE,0.04,"metal")
		_box(Vector3(x,1.64,2.14),Vector3(0.14,2.65,0.06),STEEL,0.015,"metal")
	_box(Vector3(-6.905,3.15,2.35),Vector3(3.08,0.18,0.29),BLUE,0.04,"metal")
	_box(Vector3(-8.46,1.17,2.34),Vector3(0.10,0.28,0.22),TRIM,0.025)
	geo.cylinder(Vector3(-8.525,1.22,2.34),0.035,0.025,ORANGE,12,Vector3.LEFT)
	_save_part("LiftFrame")
	for x in [-8.28,-5.53]:
		_box(Vector3(x,0.49,2.35),Vector3(0.38,0.39,0.42),BLUE_DARK,0.035,"metal")
		var inward := 1.0 if x < -7 else -1.0
		for dz in [-0.86,0.86]:
			geo.beam(Vector3(x,0.35,2.35),Vector3(x+inward*0.84,0.35,2.35+dz),0.12,0.15,Color("c29c53"),"metal")
			geo.cylinder(Vector3(x+inward*0.84,0.44,2.35+dz),0.12,0.035,DARK,12,Vector3.UP,"rubber")

func _rear_details() -> void:
	# Janelas industriais altas, ventilacao e porta de servico.
	for x in [-7.8,-5.0,-2.2,0.6]:
		_box(Vector3(x,3.0,7.45),Vector3(2.3,1.05,0.12),BLUE_DARK,0.035,"metal")
		for dx in [-0.75,0,0.75]:
			_box(Vector3(x+dx,3.0,7.525),Vector3(0.67,0.84,0.035),GLASS,0.012,"glass")
		_box(Vector3(x,2.4,7.53),Vector3(2.48,0.10,0.21),TRIM,0.025)
	# Venezianas laterais do galpao.
	for z in [1.0,4.0]:
		_box(Vector3(-9.94,2.55,z),Vector3(0.09,1.1,1.9),BLUE_DARK,0.03,"metal")
		for i in 8:
			_box(Vector3(-10.015,2.09+i*0.125,z),Vector3(0.09,0.065,1.72),BLUE,0.015,"metal")
	_box(Vector3(5.0,1.15,7.135),Vector3(1.2,2.3,0.10),BLUE,0.04,"metal")
	_box(Vector3(5.39,1.1,7.20),Vector3(0.24,0.035,0.04),TRIM,0.01,"metal")
	# Unidade tecnica compacta, grelha e conduites.
	_box(Vector3(8.0,1.0,7.43),Vector3(1.56,0.95,0.65),Color("b5b7a9"),0.06,"metal")
	geo.cylinder(Vector3(8.30,1.0,7.78),0.33,0.035,DARK,20,Vector3.BACK,"metal")
	for i in 9:
		_box(Vector3(7.48+i*0.065,1.0,7.78),Vector3(0.025,0.69,0.025),STEEL,0.003,"metal")
	for dx in [-0.2,0.2]:
		geo.beam(Vector3(8.3+dx,0.5,7.7),Vector3(8.3+dx,0.1,7.7),0.06,0.06,ROOF,"metal")

func _yard_props() -> void:
	# Pneus sobre rack lateral. Objetos de equipamento; nao estoque simulado.
	for z in [3.3,5.7]:
		for x in [10.0,10.63]:
			geo.beam(Vector3(x,0.1,z),Vector3(x,1.8,z),0.055,0.055,BLUE_DARK,"metal")
	for y in [0.37,1.2]:
		for x in [10.0,10.63]:
			geo.beam(Vector3(x,y,3.25),Vector3(x,y,5.75),0.055,0.055,BLUE_DARK,"metal")
		for i in 5:
			geo.tyre(Vector3(10.32,y+0.35,3.55+i*0.45),0.33,0.23,Vector3.FORWARD)
	# Compressor lateral discreto com protecao.
	geo.cylinder(Vector3(-10.43,0.65,5.9),0.28,0.97,ORANGE,16,Vector3.UP,"metal")
	geo.cylinder(Vector3(-10.43,1.18,5.9),0.17,0.17,DARK,12,Vector3.UP,"metal")
	for x in [-10.61,-10.25]:
		_box(Vector3(x,0.14,5.9),Vector3(0.07,0.28,0.54),BLUE_DARK,0.015,"metal")
	# Balizadores de protecao junto da recepcao, fora da porta.
	for x in [3.0,9.5]:
		geo.cylinder(Vector3(x,0.43,-3.7),0.075,0.86,BLUE,12,Vector3.UP,"metal")
		geo.cylinder(Vector3(x,0.75,-3.7),0.079,0.11,Color("d3b66c"),12)
	# Banco e jardineira de recepcao, sem usuarios ficticios.
	for x in [4.4,5.6]:
		_box(Vector3(x,0.24,-3.42),Vector3(0.075,0.46,0.50),BLUE_DARK,0.015,"metal")
	for z in [-3.63,-3.47,-3.31]:
		_box(Vector3(5.0,0.49,z),Vector3(1.55,0.085,0.13),Color("b08a58"),0.025)
	_box(Vector3(10.15,0.31,-0.85),Vector3(1.05,0.62,1.85),Color("b2a58a"),0.07)
	_box(Vector3(10.15,0.635,-0.85),Vector3(0.85,0.03,1.65),Color("5b6049"),0.01)
	# Pequenos arbustos facetados autorais; sem efeito ambiental declarado.
	for i in 3:
		_bush(Vector3(10.15,0.89,-1.4+i*0.56),0.39,Color("687b51").lightened(i*0.025))

func _bush(p: Vector3, radius: float, color: Color) -> void:
	var rings := [Vector2(0.6,-0.65),Vector2(1,0),Vector2(0.72,0.65)]
	for r in 2:
		for i in 7:
			var a := TAU*i/7
			var b := TAU*(i+1)/7
			var s: Vector2 = rings[r]
			var t: Vector2 = rings[r+1]
			var pts := [p+Vector3(cos(a)*s.x,s.y,sin(a)*s.x)*radius,p+Vector3(cos(b)*s.x,s.y,sin(b)*s.x)*radius,p+Vector3(cos(b)*t.x,t.y,sin(b)*t.x)*radius,p+Vector3(cos(a)*t.x,t.y,sin(a)*t.x)*radius]
			geo.face(pts,Vector3(cos((a+b)/2),0,sin((a+b)/2)),color.lightened(0.02*((i+r)%3)))
	for i in 7:
		var a := TAU*i/7
		var b := TAU*(i+1)/7
		geo.face([p+Vector3(cos(a)*0.72,0.65,sin(a)*0.72)*radius,p+Vector3(cos(b)*0.72,0.65,sin(b)*0.72)*radius,p+Vector3(0,radius,0)],Vector3.UP,color.lightened(0.06))

func _signs() -> void:
	# Designacao generica substituivel; nao fixa nome de empresa ou operador.
	_box(Vector3(5.92,3.77,-2.10),Vector3(6.0,0.94,0.22),BLUE_DARK,0.09,"metal")
	_box(Vector3(5.92,3.77,-2.225),Vector3(5.76,0.72,0.045),BLUE,0.05,"metal")
	_save_part("SignHousing")
	_text("OFICINA",Vector3(5.92,3.79,-2.265),0.52,TRIM,"ReplaceableActivityLabel")
	# Emblema mecanico, silhueta original, sem textura e legivel em miniatura.
	_box(Vector3(-3.65,4.65,-1.22),Vector3(1.25,1.25,0.15),ORANGE,0.13,"metal")
	var sign_origin := Vector3(-3.65,4.65,-1.315)
	# Poligono concavo da boca aberta: nao confundir com um martelo.
	var outline := PackedVector2Array([Vector2(-0.075,-0.36),Vector2(-0.075,0.02),Vector2(-0.18,0.12),Vector2(-0.26,0.28),Vector2(-0.22,0.43),Vector2(-0.13,0.50),Vector2(-0.13,0.29),Vector2(0,0.22),Vector2(0.13,0.29),Vector2(0.13,0.50),Vector2(0.22,0.43),Vector2(0.26,0.28),Vector2(0.18,0.12),Vector2(0.075,0.02),Vector2(0.075,-0.36)])
	for i in outline.size():
		outline[i] = outline[i].rotated(-0.62)
	var triangles := Geometry2D.triangulate_polygon(outline)
	for i in range(0,triangles.size(),3):
		var pts: Array = []
		for j in 3:
			var p: Vector2 = outline[triangles[i+j]]
			pts.append(sign_origin+Vector3(p.x,p.y,0))
		geo.face(pts,Vector3.FORWARD,TRIM,"metal")
	var ring := Vector2(0,-0.39).rotated(-0.62)
	var center := sign_origin+Vector3(ring.x,ring.y,0)
	for i in 12:
		var a := TAU*i/12
		var b := TAU*(i+1)/12
		geo.face([center+Vector3(cos(a),sin(a),0)*0.135,center+Vector3(cos(b),sin(b),0)*0.135,center+Vector3(cos(b),sin(b),0)*0.071,center+Vector3(cos(a),sin(a),0)*0.071],Vector3.FORWARD,TRIM,"metal")
	_save_part("WrenchEmblem")

func _text(value: String, pos: Vector3, height: float, _color: Color, label: String) -> void:
	var mesh := TextMesh.new()
	mesh.text = value
	mesh.font_size = 64
	mesh.curve_step = 2.0
	mesh.pixel_size = height/64.0
	mesh.depth = 0.015
	mesh.material = materials["lettering"]
	var instance := MeshInstance3D.new()
	instance.name = label
	instance.mesh = mesh
	instance.position = pos
	instance.rotation.y = PI
	asset.add_child(instance)
	instance.owner = asset
	var triangles := 0
	for i in mesh.get_surface_count():
		var arrays := mesh.surface_get_arrays(i)
		var indices = arrays[Mesh.ARRAY_INDEX]
		triangles += (indices.size() if indices != null and indices.size()>0 else arrays[Mesh.ARRAY_VERTEX].size())/3
	text_meshes.append({"name":label,"text":value,"triangles":triangles,"material":"lettering","native":"TextMesh editavel","glb":"geometria triangulada","position":[pos.x,pos.y,pos.z]})

func _markers() -> void:
	var markers := Node3D.new()
	markers.name = "CandidateAnchors"
	markers.set_meta("status","referencias visuais, sem acesso ou capacidade de gameplay")
	asset.add_child(markers)
	markers.owner = asset
	for item in [
		{"name":"PedestrianDoor","p":Vector3(8.13,0,-2.65)},
		{"name":"ServiceDoor","p":Vector3(5.0,0,7.235)},
		{"name":"OpenBayThreshold","p":Vector3(-6.9,0,-1.3)},
		{"name":"ClosedBayThreshold","p":Vector3(-1.45,0,-1.3)},
		{"name":"VehicleApproach","p":Vector3(-6.9,0,-7.0)},
		{"name":"VehiclePresentationAnchor","p":Vector3(-6.9,0,2.35)},
		{"name":"ActivitySign","p":Vector3(5.92,3.79,-2.28)}]:
		var m := Marker3D.new()
		m.name = item.name
		m.position = item.p
		markers.add_child(m)
		m.owner = asset

func _aabb(a: AABB) -> Dictionary:
	return {"min":[a.position.x,a.position.y,a.position.z],"max":[a.end.x,a.end.y,a.end.z]}

func _triangle_total() -> int:
	var total := 0
	for item in meshes:
		total += item.triangles
	return total

func _manifest() -> void:
	var f := FileAccess.open(ROOT+"/manifest.json",FileAccess.WRITE)
	if f == null:
		failures.append("manifesto")
		return
	var text_triangles := 0
	for item in text_meshes:
		text_triangles += item.triangles
	f.store_string(JSON.stringify({
		"visual_id":"candidate.mechanic_shop.codex.a",
		"status":"asset para uso futuro, nao integrado; revisao humana pendente",
		"authorship":"geometria e fonte originais escritas nesta entrega para IndexCities",
		"scene":ROOT+"/Scenes/mechanic_shop_codex.a.tscn",
		"source":ROOT+"/Source/build.gd",
		"candidate_lot_m":[22,17],"up":"Y","front":"-Z","pivot":"centro do lote no solo",
		"gameplay":"sem preco, capacidade, emprego, receita, estoque, regras de reparo ou acesso funcional",
		"embedded_vehicles":0,"embedded_people":0,"lights":0,"scripts_in_asset":0,"autoplay":false,
		"materials":5,"triangles_geometry_excluding_text":_triangle_total(),"triangles_text":text_triangles,
		"palette":{"size":[32,32],"colors":palette_colors.size(),"source":"paleta original escrita pela receita; sem imagens externas"},
		"triangles_total":_triangle_total()+text_triangles,"mesh_instances_total":meshes.size()+text_meshes.size(),
		"geometry_parts":meshes,"text_parts":text_meshes,
		"notes":"Dimensoes e marcadores sao candidatos. Duas aberturas visuais nao definem capacidade. A simulacao futura deve comandar veiculos, pessoas, luzes e operacao. Textos genericos substituiveis."
	},"\t"))
