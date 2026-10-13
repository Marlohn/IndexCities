## Prancha TECNICA de revisao da segunda onda (Preview/v1_wave2_board.tscn).
##
## Mostra as sete funcoes restantes da V1 ao longo de uma rua 1+1, cada uma dentro do
## contorno do LOTE LOGICO usado pelo host (Model.cs: largura X x profundidade Z), com o
## pivo no centro do lote e a fachada -Z voltada para a rua. A fileira do outro lado da
## rua e girada 180 graus para conferir a rotacao. Contornos, luz e chao sao SO do
## preview. Nao e partida, nao povoa o jogo e nao define regras, acessos ou trafego.
extends RefCounted

const FAMILY_ID := "preview.wave2_board"

const ROAD_STRAIGHT := "res://Assets/V1/Scenes/road.urban_1x1.a.straight.tscn"
const OUT_PATH := "res://Assets/V1/Preview/v1_wave2_board.tscn"

## [cena, nome do no, lote X, lote Z, lado (+1 = lado +Z da rua, -1 = lado -Z girado)]
const ROW := [
	["res://Assets/V1/Scenes/civic.works_depot.a.tscn", "WorksDepot", 24.0, 22.0, 1],
	["res://Assets/V1/Scenes/commercial.fuel_station.a.tscn", "FuelStation", 24.0, 24.0, 1],
	["res://Assets/V1/Scenes/agriculture.farm_small.a.tscn", "FarmSmall", 36.0, 40.0, 1],
	["res://Assets/V1/Scenes/utility.wind_power.a.tscn", "WindPower", 18.0, 18.0, 1],
	["res://Assets/V1/Scenes/utility.water_intake.a.tscn", "WaterIntake", 14.0, 16.0, -1],
	["res://Assets/V1/Scenes/utility.water_treatment.a.tscn", "WaterTreatment", 24.0, 26.0, -1],
	["res://Assets/V1/Scenes/utility.sewage_treatment.a.tscn", "SewageTreatment", 26.0, 28.0, -1],
]

const MODULE := 10.5
const GAP := 3.0
const LOT_SETBACK := 0.6   # da borda da calcada ate a frente do lote (so vitrine)

var _kit


func build(kit) -> void:
	_kit = kit
	for r in ROW:
		if not ResourceLoader.exists(r[0]):
			print("[board2] cena ausente, prancha da onda 2 nao gerada: ", r[0])
			return
	var root := Node3D.new()
	root.name = "V1Wave2Board"
	root.set_meta("purpose", "preview tecnico da segunda onda; lotes do host desenhados; nao e partida")
	root.set_meta("asset_status", kit.ASSET_STATUS)
	_add_environment(root)

	var facilities: Node3D = kit.add_node(root, "Facilities")
	var outlines: Node3D = kit.add_node(root, "LotOutlines")
	var line_mat := StandardMaterial3D.new()
	line_mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	line_mat.albedo_color = Color("#fffdf2")
	var x_pos := {1: 0.0, -1: 0.0}
	var max_x := 0.0
	for r in ROW:
		var side: int = r[4]
		var w: float = r[2]
		var d: float = r[3]
		var cx: float = x_pos[side] + w * 0.5
		var front := MODULE * 0.5 + LOT_SETBACK
		var cz: float = side * (front + d * 0.5)
		var basis := Basis.IDENTITY if side > 0 else Basis(Vector3.UP, PI)
		kit.instance(facilities, r[0], r[1], Transform3D(basis, Vector3(cx, 0, cz)))
		_outline(outlines, "Lot_" + String(r[1]), Vector3(cx, 0.03, cz), w, d, line_mat)
		x_pos[side] = x_pos[side] + w + GAP
		max_x = max(max_x, x_pos[side])
	var road: Node3D = kit.add_node(root, "Road")
	var count := int(ceil(max_x / MODULE)) + 1
	for k in count:
		kit.instance(road, ROAD_STRAIGHT, "Straight%02d" % k, Transform3D(Basis.IDENTITY, Vector3(-MODULE * 0.5 + MODULE * k, 0, 0)))
	_add_ground(root, max_x)

	kit._own(root, root)
	var ps := PackedScene.new()
	var e := ps.pack(root)
	if e == OK:
		e = ResourceSaver.save(ps, OUT_PATH)
	if e != OK:
		kit.err("Falha ao salvar prancha %s (%s)" % [OUT_PATH, error_string(e)])
	else:
		print("[board2] ", OUT_PATH)
	root.free()


## Contorno do lote (4 faixas de 0,25 m), material sem luz. So preview.
func _outline(parent: Node3D, node_name: String, c: Vector3, w: float, d: float, mat: Material) -> void:
	var t := 0.25
	var holder: Node3D = _kit.add_node(parent, node_name, Transform3D(Basis.IDENTITY, c))
	var bars := [
		[Vector3(0, 0, -d * 0.5 + t * 0.5), Vector2(w, t)],
		[Vector3(0, 0, d * 0.5 - t * 0.5), Vector2(w, t)],
		[Vector3(-w * 0.5 + t * 0.5, 0, 0), Vector2(t, d)],
		[Vector3(w * 0.5 - t * 0.5, 0, 0), Vector2(t, d)],
	]
	var i := 0
	for b in bars:
		var pm := PlaneMesh.new()
		pm.size = b[1]
		pm.material = mat
		var mi := MeshInstance3D.new()
		mi.name = "Edge%d" % i
		mi.mesh = pm
		mi.position = b[0]
		mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		holder.add_child(mi)
		i += 1


func _add_environment(root: Node3D) -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#cfdee8")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#c9d5df")
	env.ambient_light_energy = 0.55
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	var we := WorldEnvironment.new()
	we.name = "PreviewEnvironment"
	we.environment = env
	root.add_child(we)
	var sun := DirectionalLight3D.new()
	sun.name = "PreviewSun"
	sun.transform = Transform3D(Basis.looking_at(Vector3(0.45, -1.0, 0.8).normalized(), Vector3.UP), Vector3(0, 30, 0))
	sun.light_color = Color("#fff3e2")
	sun.light_energy = 1.15
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 800.0
	root.add_child(sun)


func _add_ground(root: Node3D, length: float) -> void:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color("#8fae6e")
	mat.roughness = 1.0
	var pm := PlaneMesh.new()
	pm.size = Vector2(length + 40.0, 130.0)
	pm.material = mat
	var mi := MeshInstance3D.new()
	mi.name = "PreviewGround"
	mi.mesh = pm
	mi.position = Vector3(length * 0.5, -0.01, 0)
	root.add_child(mi)
