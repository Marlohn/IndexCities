## Gerador dos icones SVG de catalogo da amostra V1 (ferramenta; NAO e jogo).
##
##   godot --headless --path <raiz> --script res://Assets/V1/Source/make_v1_icons.gd
##
##   ... -- --only=utility.wind      (so icones cujo visual_id comeca assim)
##
## Desenha versoes SIMPLIFICADAS dos predios (Prefeitura, casa, mercearia aqui; as demais
## familias desenham o proprio icone em `draw_icon(ic)` dentro da receita) com
## blocos 3D projetados na MESMA camera isometrica das miniaturas (direcao 1,1,-1),
## sombreamento plano pela mesma direcao de luz do preview e cores lidas dos materiais
## gerados (Assets/V1/Materials). Sem texto, logo ou marca: o icone deve ser
## reconhecivel por quem ainda nao le. Tambem rasteriza PNGs de conferencia em
## Preview/Captures. Estilo e formas sao CANDIDATOS (revisao humana PENDENTE).
extends SceneTree

const ICON_DIR := "res://Assets/V1/Icons"
const CAPTURES := "res://Assets/V1/Preview/Captures"
const VIEW := 128.0
const PAD := 7.0
const STROKE := "#2b2f33"

# Camera isometrica verdadeira identica a das miniaturas (ver render_v1_previews.gd).
var _d := Vector3(1, 1, -1).normalized()
var _right := Vector3(-1, 0, -1).normalized()
var _up := Vector3(-1, 2, 1).normalized()
var _light := -Vector3(0.45, -1.0, 0.8).normalized()
var _colors := {}

# Cada "parte" e uma lista de faces {pts: Array[Vector3], col: String, decal: bool}.
var _parts: Array = []
var _cur: Array = []
var _bias := 0.0
var _missing: PackedStringArray = PackedStringArray()

## Receitas que desenham o proprio icone com `func draw_icon(ic) -> void` (ic = este script).
const RECIPE_ICONS := [
	"res://Assets/V1/Source/recipes/civic_works_depot.gd",
	"res://Assets/V1/Source/recipes/agriculture_farm_small.gd",
	"res://Assets/V1/Source/recipes/commercial_fuel_station.gd",
	"res://Assets/V1/Source/recipes/utility_water_intake.gd",
	"res://Assets/V1/Source/recipes/utility_water_treatment.gd",
	"res://Assets/V1/Source/recipes/utility_sewage_treatment.gd",
	"res://Assets/V1/Source/recipes/utility_wind_power.gd",
]


func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(ICON_DIR))
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CAPTURES))
	var only := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--only="):
			only = a.substr(7)
	var ok := true
	var jobs := [
		["civic.city_hall.a", Callable(self, "_city_hall")],
		["residential.small_house.a", Callable(self, "_house")],
		["commercial.grocery_small.a", Callable(self, "_grocery")],
	]
	for path in RECIPE_ICONS:
		if not ResourceLoader.exists(path):
			continue
		var script: GDScript = load(path)
		if script == null:
			printerr("[icons] receita nao carregou: ", path)
			ok = false
			continue
		var recipe = script.new()
		if recipe.has_method("draw_icon"):
			# A instancia (RefCounted) fica no job: um Callable sozinho nao a mantem viva.
			jobs.append([str(script.get_script_constant_map().get("FAMILY_ID", "")), Callable(recipe, "draw_icon").bind(self), recipe])
	for j in jobs:
		if only != "" and not String(j[0]).begins_with(only):
			continue
		ok = _emit(j[0], j[1]) and ok
	if ok:
		print("[icons] OK. No projeto do jogo, reimporte os SVG alterados (abra o editor ou rode 'godot --headless --path <raiz> --import'): o jogo em execucao usa o cache .godot/imported.")
	quit(0 if ok else 1)


# ---------------------------------------------------------------------------
# Cores dos materiais reais
# ---------------------------------------------------------------------------

func c(mat_name: String) -> String:
	if _colors.has(mat_name):
		return _colors[mat_name]
	var path := "res://Assets/V1/Materials/%s.tres" % mat_name
	# Material ausente e ERRO (antes virava magenta silencioso quando os icones eram gerados
	# antes das familias). O icone com cor faltando nao e gravado.
	var hex := ""
	if ResourceLoader.exists(path):
		var m = load(path)
		if m is BaseMaterial3D:
			hex = "#" + (m as BaseMaterial3D).albedo_color.to_html(false)
	if hex == "":
		_missing.append(mat_name)
		push_error("icone: material ausente ou invalido %s (gere as familias antes dos icones)" % path)
		return "#000000"
	_colors[mat_name] = hex
	return hex


# ---------------------------------------------------------------------------
# Primitivas (coordenadas em metros, fachada -Z)
# ---------------------------------------------------------------------------

## Inicia uma nova parte. `bias` soma a profundidade de ordenacao (desenha mais tarde).
func part(bias: float = 0.0) -> void:
	if not _cur.is_empty():
		_parts.append({"faces": _cur, "bias": _bias})
	_cur = []
	_bias = bias


func quad(pts: Array, col: String, decal := false) -> void:
	_cur.append({"pts": pts, "col": col, "decal": decal})


func box(mn: Vector3, mx: Vector3, col: String, top_col := "") -> void:
	var t := top_col if top_col != "" else col
	quad([Vector3(mn.x, mx.y, mx.z), Vector3(mx.x, mx.y, mx.z), Vector3(mx.x, mx.y, mn.z), Vector3(mn.x, mx.y, mn.z)], t)
	quad([Vector3(mx.x, mn.y, mx.z), Vector3(mx.x, mn.y, mn.z), Vector3(mx.x, mx.y, mn.z), Vector3(mx.x, mx.y, mx.z)], col)
	quad([Vector3(mn.x, mn.y, mn.z), Vector3(mn.x, mn.y, mx.z), Vector3(mn.x, mx.y, mx.z), Vector3(mn.x, mx.y, mn.z)], col)
	quad([Vector3(mx.x, mn.y, mn.z), Vector3(mn.x, mn.y, mn.z), Vector3(mn.x, mx.y, mn.z), Vector3(mx.x, mx.y, mn.z)], col)
	quad([Vector3(mn.x, mn.y, mx.z), Vector3(mx.x, mn.y, mx.z), Vector3(mx.x, mx.y, mx.z), Vector3(mn.x, mx.y, mx.z)], col)


## Telhado de duas aguas. axis "x": cumeeira paralela a X (empenas em +-X).
func gable(mn: Vector3, mx: Vector3, ridge_y: float, axis: String, col: String, gable_col := "") -> void:
	var g := gable_col if gable_col != "" else col
	if axis == "x":
		var zm := (mn.z + mx.z) * 0.5
		var r0 := Vector3(mn.x, ridge_y, zm)
		var r1 := Vector3(mx.x, ridge_y, zm)
		quad([Vector3(mn.x, mn.y, mn.z), Vector3(mx.x, mn.y, mn.z), r1, r0], col)
		quad([Vector3(mx.x, mn.y, mx.z), Vector3(mn.x, mn.y, mx.z), r0, r1], col)
		quad([Vector3(mx.x, mn.y, mn.z), Vector3(mx.x, mn.y, mx.z), r1], g)
		quad([Vector3(mn.x, mn.y, mx.z), Vector3(mn.x, mn.y, mn.z), r0], g)
	else:
		var xm := (mn.x + mx.x) * 0.5
		var r0 := Vector3(xm, ridge_y, mn.z)
		var r1 := Vector3(xm, ridge_y, mx.z)
		quad([Vector3(mn.x, mn.y, mx.z), Vector3(mn.x, mn.y, mn.z), r0, r1], col)
		quad([Vector3(mx.x, mn.y, mn.z), Vector3(mx.x, mn.y, mx.z), r1, r0], col)
		quad([Vector3(mx.x, mn.y, mn.z), Vector3(mn.x, mn.y, mn.z), r0], g)
		quad([Vector3(mn.x, mn.y, mx.z), Vector3(mx.x, mn.y, mx.z), r1], g)


## Telhado de quatro aguas com cumeeira paralela a X (recuo `inset` nas pontas).
func hip(mn: Vector3, mx: Vector3, ridge_y: float, inset: float, col: String) -> void:
	var zm := (mn.z + mx.z) * 0.5
	var r0 := Vector3(mn.x + inset, ridge_y, zm)
	var r1 := Vector3(mx.x - inset, ridge_y, zm)
	var a := Vector3(mn.x, mn.y, mn.z)
	var b := Vector3(mx.x, mn.y, mn.z)
	var d := Vector3(mx.x, mn.y, mx.z)
	var e := Vector3(mn.x, mn.y, mx.z)
	quad([a, b, r1, r0], col)
	quad([d, e, r0, r1], col)
	quad([e, a, r0], col)
	quad([b, d, r1], col)


## Prisma regular vertical (tambor/cupula facetada).
func prism(center: Vector3, r0: float, r1: float, h: float, sides: int, col: String, top_col := "") -> void:
	var t := top_col if top_col != "" else col
	var top := []
	for i in sides:
		var a0 := TAU * i / sides + PI / sides
		var a1 := TAU * (i + 1) / sides + PI / sides
		var p0 := center + Vector3(cos(a0) * r0, 0, sin(a0) * r0)
		var p1 := center + Vector3(cos(a1) * r0, 0, sin(a1) * r0)
		var q0 := center + Vector3(cos(a0) * r1, h, sin(a0) * r1)
		var q1 := center + Vector3(cos(a1) * r1, h, sin(a1) * r1)
		if r1 > 0.001:
			quad([p1, p0, q0, q1], col)
		else:
			quad([p1, p0, q0], col)
		top.append(q0)
	if r1 > 0.001:
		quad(top, t)


## Retangulo na fachada -Z (decal levemente a frente da face em z).
func facade_rect(x0: float, x1: float, y0: float, y1: float, z: float, col: String) -> void:
	quad([Vector3(x1, y0, z), Vector3(x0, y0, z), Vector3(x0, y1, z), Vector3(x1, y1, z)], col, true)


## Retangulo na face +X (decal) com z0 < z1.
func side_rect(z0: float, z1: float, y0: float, y1: float, x: float, col: String) -> void:
	quad([Vector3(x, y0, z1), Vector3(x, y0, z0), Vector3(x, y1, z0), Vector3(x, y1, z1)], col, true)


## Janela = moldura clara + vidro escuro recuado visualmente.
func window_front(xc: float, yc: float, w: float, h: float, z: float) -> void:
	facade_rect(xc - w * 0.5 - 0.18, xc + w * 0.5 + 0.18, yc - h * 0.5 - 0.18, yc + h * 0.5 + 0.18, z, c("trim_white"))
	facade_rect(xc - w * 0.5, xc + w * 0.5, yc - h * 0.5, yc + h * 0.5, z - 0.01, c("glass_dark"))


func window_side(zc: float, yc: float, w: float, h: float, x: float) -> void:
	side_rect(zc - w * 0.5 - 0.18, zc + w * 0.5 + 0.18, yc - h * 0.5 - 0.18, yc + h * 0.5 + 0.18, x, c("trim_white"))
	side_rect(zc - w * 0.5, zc + w * 0.5, yc - h * 0.5, yc + h * 0.5, x + 0.01, c("glass_dark"))


# ---------------------------------------------------------------------------
# Desenhos simplificados (proporcoes das cenas da amostra)
# ---------------------------------------------------------------------------

func _city_hall() -> void:
	var stone := c("civic_stone")
	var roof := c("civic_roof")
	var dome := c("civic_dome")
	var trim := c("trim_white")
	var plinth := c("plinth_stone")
	part(-100.0)  # embasamento sempre atras de tudo
	box(Vector3(-12.25, 0, -7.6), Vector3(12.25, 1.0, 7.6), plinth)
	# Alas separadas para a ordem de desenho funcionar com o pavilhao entre elas.
	for sx in [-1.0, 1.0]:
		var x0: float = 5.4 if sx > 0 else -12.0
		var x1: float = 12.0 if sx > 0 else -5.4
		var ox0: float = x0 - (0.3 if sx < 0 else 0.0)
		var ox1: float = x1 + (0.3 if sx > 0 else 0.0)
		part()
		box(Vector3(x0, 1.0, -4.4), Vector3(x1, 8.2, 6.7), stone)
		for k in 3:
			var xc: float = sx * (6.7 + k * 1.9)
			window_front(xc, 2.9, 1.1, 2.0, -4.42)
			window_front(xc, 6.4, 1.1, 1.8, -4.42)
		if sx > 0:
			for zc in [-2.3, 0.6, 3.5]:
				window_side(zc, 2.9, 1.1, 2.0, 12.02)
				window_side(zc, 6.4, 1.1, 1.8, 12.02)
		part()
		box(Vector3(ox0, 8.2, -4.7), Vector3(ox1, 8.8, 7.0), trim, roof)
		part()
		hip(Vector3(ox0, 8.8, -4.7), Vector3(ox1, 8.8, 7.0), 10.1, 2.6, roof)
	# Pavilhao central com porta azul.
	part()
	box(Vector3(-5.4, 1.0, -5.0), Vector3(5.4, 9.7, 7.3), stone)
	facade_rect(-0.9, 0.9, 1.0, 4.5, -5.02, c("civic_accent"))
	window_front(-2.85, 2.9, 1.0, 2.0, -5.02)
	window_front(2.85, 2.9, 1.0, 2.0, -5.02)
	window_front(-2.85, 6.6, 1.0, 2.0, -5.02)
	window_front(0.0, 6.6, 1.0, 2.0, -5.02)
	window_front(2.85, 6.6, 1.0, 2.0, -5.02)
	part()
	box(Vector3(-5.6, 9.7, -5.2), Vector3(5.6, 10.3, 7.5), trim, roof)
	# Atico + tambor + cupula + lanternim (acima de tudo: desenhados por ultimo).
	part(6.0)
	box(Vector3(-4.2, 10.3, -3.05), Vector3(4.2, 12.35, 5.35), stone)
	part(6.1)
	box(Vector3(-4.35, 12.35, -3.2), Vector3(4.35, 12.6, 5.5), trim, roof)
	part(10.0)
	prism(Vector3(0, 12.6, 1.15), 3.2, 3.2, 0.95, 10, stone)
	part(10.1)
	prism(Vector3(0, 13.55, 1.15), 3.35, 3.35, 0.25, 10, trim)
	# Icone: cupula com menos facetas que o modelo (legibilidade em 32-128 px).
	var rings := [[3.1, 13.8], [2.6, 15.25], [1.55, 16.35], [0.8, 16.79]]
	for i in rings.size() - 1:
		part(10.2 + i * 0.1)
		prism(Vector3(0, rings[i][1], 1.15), rings[i][0], rings[i + 1][0], rings[i + 1][1] - rings[i][1], 10, dome)
	part(11.0)
	prism(Vector3(0, 16.79, 1.15), 0.75, 0.75, 0.42, 8, trim)
	part(11.1)
	prism(Vector3(0, 17.21, 1.15), 0.95, 0.0, 0.5, 8, trim)
	# Escadaria com bocel claro em cada degrau.
	for k in 5:
		part()
		var zf := -10.1 + 0.5 * k
		var top := 0.2 * (k + 1)
		box(Vector3(-4.6, 0, zf), Vector3(4.6, top, -7.6), plinth)
		facade_rect(-4.6, 4.6, top - 0.07, top, zf - 0.01, trim)
	# Colunas, entablamento e frontao com timpano azul.
	for xc in [-4.25, -1.45, 1.45, 4.25]:
		part()
		box(Vector3(xc - 0.4, 1.0, -7.35), Vector3(xc + 0.4, 8.9, -6.55), trim)
	part()
	box(Vector3(-5.45, 8.9, -7.95), Vector3(5.45, 9.7, -5.0), trim, roof)
	part()
	gable(Vector3(-5.45, 9.7, -7.95), Vector3(5.45, 9.7, -5.0), 12.05, "z", roof, c("civic_accent"))


func _house() -> void:
	var wall := c("house_wall")
	var roof := c("house_roof")
	var trim := c("trim_white")
	part()
	box(Vector3(-4.06, 0, -3.56), Vector3(4.06, 0.3, 3.56), c("plinth_stone"))
	part()
	box(Vector3(-4.0, 0.3, -3.5), Vector3(4.0, 3.4, 3.5), wall)
	window_front(-2.45, 1.8, 1.2, 1.2, -3.52)
	window_front(-0.1, 1.8, 1.2, 1.2, -3.52)
	facade_rect(1.7, 2.9, 0.3, 2.55, -3.52, trim)
	facade_rect(1.83, 2.77, 0.3, 2.4, -3.53, c("wood_door"))
	window_side(0.0, 1.8, 1.2, 1.2, 4.02)
	part()
	box(Vector3(-2.6, 4.6, 1.15), Vector3(-2.0, 6.45, 1.75), roof)
	part()
	box(Vector3(-2.68, 6.45, 1.07), Vector3(-1.92, 6.57, 1.83), trim)
	part()
	gable(Vector3(-4.35, 3.4, -3.95), Vector3(4.35, 3.4, 3.95), 6.0, "x", roof, wall)
	part()
	gable(Vector3(1.45, 2.72, -4.55), Vector3(3.15, 2.72, -3.5), 3.08, "z", roof, trim)
	part()
	box(Vector3(1.55, 0, -4.26), Vector3(3.05, 0.15, -3.56), c("plinth_stone"))


func _grocery() -> void:
	var wall := c("shop_wall")
	var awning := c("shop_awning")
	var white := c("trim_white")
	part()
	box(Vector3(-5.05, 0, -6.05), Vector3(5.05, 0.2, 6.05), c("plinth_stone"))
	part()
	box(Vector3(-5.0, 0.2, -6.0), Vector3(5.0, 4.6, 6.0), wall, c("concrete_dark"))
	facade_rect(-4.1, 4.1, 0.6, 3.45, -6.02, c("metal_dark"))
	facade_rect(-4.0, -1.4, 0.68, 3.37, -6.03, c("glass_dark"))
	facade_rect(-1.3, 2.3, 0.68, 3.37, -6.03, c("glass_dark"))
	facade_rect(2.48, 3.92, 0.25, 2.55, -6.03, c("glass_dark"))
	side_rect(3.15, 4.45, 0.2, 2.6, 5.02, c("concrete_dark"))
	window_side(-2.5, 2.4, 1.4, 0.9, 5.02)
	window_side(0.2, 2.4, 1.4, 0.9, 5.02)
	# Platibanda (4 lados) e frontao elevado atras do letreiro.
	part()
	box(Vector3(-5.0, 4.6, 5.78), Vector3(5.0, 5.1, 6.0), wall, white)
	part()
	box(Vector3(-5.0, 4.6, -5.78), Vector3(-4.78, 5.1, 5.78), wall, white)
	part()
	box(Vector3(4.78, 4.6, -5.78), Vector3(5.0, 5.1, 5.78), wall, white)
	part()
	box(Vector3(-5.0, 4.6, -6.0), Vector3(5.0, 5.1, -5.78), wall, white)
	part()
	box(Vector3(-3.6, 5.1, -6.0), Vector3(3.6, 6.05, -5.78), wall, white)
	part()
	box(Vector3(-2.8, 4.6, 2.15), Vector3(-1.6, 5.42, 3.05), white)
	# Toldo listrado (acento da familia + branco), 15 faixas.
	var n := 15
	for k in n:
		part()
		var a := -4.7 + 9.4 * k / n
		var b := -4.7 + 9.4 * (k + 1) / n
		var col := awning if k % 2 == 0 else white
		quad([Vector3(b, 3.3, -7.3), Vector3(a, 3.3, -7.3), Vector3(a, 3.85, -6.0), Vector3(b, 3.85, -6.0)], col)
		quad([Vector3(b, 3.0, -7.3), Vector3(a, 3.0, -7.3), Vector3(a, 3.3, -7.3), Vector3(b, 3.3, -7.3)], col)
	# Letreiro LISO (sem texto/logo): placa substituivel na integracao.
	part()
	box(Vector3(-3.0, 4.2, -6.15), Vector3(3.0, 5.7, -6.0), c("metal_dark"))
	facade_rect(-2.9, 2.9, 4.28, 5.62, -6.16, c("shop_sign_blank"))


# ---------------------------------------------------------------------------
# Projecao, ordenacao e SVG
# ---------------------------------------------------------------------------

func _proj(p: Vector3) -> Vector2:
	return Vector2(p.dot(_right), -p.dot(_up))


func _shade(hex: String, n: Vector3) -> String:
	var base := Color(hex)
	var k: float = 0.62 + 0.5 * max(0.0, n.dot(_light))
	return "#" + Color(min(base.r * k, 1.0), min(base.g * k, 1.0), min(base.b * k, 1.0)).to_html(false)


func _emit(icon_id: String, draw: Callable) -> bool:
	_parts = []
	_cur = []
	_bias = 0.0
	_missing = PackedStringArray()
	draw.call()
	part()
	if not _missing.is_empty():
		printerr("[icons] ", icon_id, ": NAO gravado; materiais ausentes: ", ", ".join(_missing))
		return false
	# Faces visiveis (backface culling) e ordem do pintor por parte e por face.
	var drawn := []
	var all_pts := []
	var part_order := []
	for i in _parts.size():
		var faces: Array = _parts[i]["faces"]
		var cen := Vector3.ZERO
		var cnt := 0
		for f in faces:
			for p in f["pts"]:
				cen += p
				cnt += 1
		part_order.append([cen.dot(_d) / max(cnt, 1) + float(_parts[i]["bias"]), i])
	part_order.sort_custom(func(a, b): return a[0] < b[0])
	for po in part_order:
		var faces: Array = _parts[po[1]]["faces"]
		var solid := []
		var decals := []
		# Centro da parte (faces solidas) orienta as normais para fora automaticamente.
		var pc := Vector3.ZERO
		var pn := 0
		for f in faces:
			if not f["decal"]:
				for p in f["pts"]:
					pc += p
					pn += 1
		pc /= max(pn, 1)
		for f in faces:
			var pts: Array = f["pts"]
			var n: Vector3 = (pts[1] - pts[0]).cross(pts[2] - pts[0]).normalized()
			var cen := Vector3.ZERO
			for p in pts:
				cen += p
			cen /= pts.size()
			if n.dot(cen - pc) < 0.0:
				n = -n
			if n.dot(_d) <= 0.0001:
				continue
			var item := {"pts": pts, "fill": _shade(f["col"], n), "depth": cen.dot(_d)}
			if f["decal"]:
				decals.append(item)
			else:
				solid.append(item)
			for p in pts:
				all_pts.append(_proj(p))
		solid.sort_custom(func(a, b): return a["depth"] < b["depth"])
		drawn.append_array(solid)
		drawn.append_array(decals)
	var mn := Vector2(INF, INF)
	var mx := Vector2(-INF, -INF)
	for q in all_pts:
		mn = Vector2(min(mn.x, q.x), min(mn.y, q.y))
		mx = Vector2(max(mx.x, q.x), max(mx.y, q.y))
	var span: float = max(mx.x - mn.x, mx.y - mn.y)
	var s: float = (VIEW - 2.0 * PAD) / span
	var off := Vector2((VIEW - (mx.x - mn.x) * s) * 0.5, (VIEW - (mx.y - mn.y) * s) * 0.5)
	var lines := PackedStringArray()
	lines.append('<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">')
	lines.append('<!-- IndexCities V1 - icone CANDIDATO de catalogo: %s. Gerado por Source/make_v1_icons.gd (original; sem texto/logo). Revisao humana PENDENTE. -->' % icon_id)
	var gx := VIEW * 0.5
	var gy := off.y + (mx.y - mn.y) * s + 1.0
	lines.append('<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="#000000" fill-opacity="0.12"/>' % [gx, min(gy, VIEW - 3.0), (mx.x - mn.x) * s * 0.42, 3.2])
	lines.append('<g stroke="%s" stroke-width="1.1" stroke-linejoin="round">' % STROKE)
	for d in drawn:
		var pp := PackedStringArray()
		for p in d["pts"]:
			var q := (_proj(p) - mn) * s + off
			pp.append("%.2f,%.2f" % [q.x, q.y])
		lines.append('<polygon points="%s" fill="%s"/>' % [" ".join(pp), d["fill"]])
	lines.append('</g>')
	lines.append('</svg>')
	var svg := "\n".join(lines)
	var path := "%s/%s.svg" % [ICON_DIR, icon_id]
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		printerr("[icons] falha ao gravar ", path)
		return false
	f.store_string(svg)
	f.close()
	var img := Image.new()
	if img.load_svg_from_string(svg, 2.0) == OK:
		img.save_png("%s/icon_%s_256.png" % [CAPTURES, icon_id])
	print("[icons] ", path, " (", drawn.size(), " poligonos)")
	return true
