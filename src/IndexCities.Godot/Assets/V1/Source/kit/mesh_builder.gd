## Construtor de malhas low-poly facetadas — amostra visual V1 do IndexCities.
##
## SOMENTE APRESENTACAO: nao contem regra de gameplay, simulacao ou dados de jogo.
## Codigo original escrito para este projeto (sem origem em acervos anteriores).
##
## Uso: acumule primitivas em "slots" (o nome do slot e o nome do material da paleta);
## depois `commit()` gera um ArrayMesh com UMA superficie por slot (agrupamento por
## material => poucas draw calls por modelo). Normais por face = visual facetado.
##
## Convencoes: Y para cima, 1 unidade = 1 metro (candidato), fachada candidata em -Z.
## Faces sao informadas em ordem anti-horaria vista de fora (regra da mao direita);
## o builder converte para a ordem horaria que a Godot usa em faces frontais.
extends RefCounted


class SlotData:
	extends RefCounted
	var verts := PackedVector3Array()
	var norms := PackedVector3Array()
	var uvs := PackedVector2Array()


## Transformacao corrente aplicada a toda primitiva emitida.
var xform: Transform3D = Transform3D.IDENTITY
var _stack: Array[Transform3D] = []
var _slots: Dictionary = {}
var _order: PackedStringArray = PackedStringArray()


# ---------------------------------------------------------------------------
# Pilha de transformacoes
# ---------------------------------------------------------------------------

func push(t: Transform3D) -> void:
	_stack.push_back(xform)
	xform = xform * t


func push_translate(v: Vector3) -> void:
	push(Transform3D(Basis.IDENTITY, v))


## Rotacao em graus em torno de Y (positivo = anti-horario visto de cima).
func push_rotate_y(deg: float, origin: Vector3 = Vector3.ZERO) -> void:
	push(Transform3D(Basis(Vector3.UP, deg_to_rad(deg)), origin))


func push_basis(b: Basis, origin: Vector3 = Vector3.ZERO) -> void:
	push(Transform3D(b, origin))


func pop() -> void:
	assert(not _stack.is_empty(), "mesh_builder: pop() sem push()")
	xform = _stack.pop_back()


# ---------------------------------------------------------------------------
# Emissao basica
# ---------------------------------------------------------------------------

func _slot(name: String) -> SlotData:
	if not _slots.has(name):
		_slots[name] = SlotData.new()
		_order.append(name)
	return _slots[name]


static func _planar_uv(p: Vector3, n: Vector3) -> Vector2:
	var a := n.abs()
	if a.y >= a.x and a.y >= a.z:
		return Vector2(p.x, p.z)
	if a.x >= a.z:
		return Vector2(p.z, -p.y)
	return Vector2(p.x, -p.y)


## Triangulo com pontos locais em ordem anti-horaria vista do lado frontal.
func tri(slot: String, a: Vector3, b: Vector3, c: Vector3) -> void:
	var wa := xform * a
	var wb := xform * b
	var wc := xform * c
	if xform.basis.determinant() < 0.0:
		var t := wb
		wb = wc
		wc = t
	var n := (wb - wa).cross(wc - wa)
	if n.length_squared() < 1e-14:
		return
	n = n.normalized()
	var s := _slot(slot)
	# Godot: face frontal em ordem horaria => emite a, c, b.
	for p in [wa, wc, wb]:
		s.verts.append(p)
		s.norms.append(n)
		s.uvs.append(_planar_uv(p, n))


## Poligono convexo (leque). `hint` (opcional, espaco local) e a direcao para fora:
## se informado, a ordem e corrigida automaticamente.
func face(slot: String, pts: Array, hint: Vector3 = Vector3.ZERO) -> void:
	if pts.size() < 3:
		return
	var p: Array = pts
	if hint != Vector3.ZERO:
		var n: Vector3 = (pts[1] - pts[0]).cross(pts[2] - pts[0])
		var k := 2
		while n.length_squared() < 1e-14 and k + 1 < pts.size():
			k += 1
			n = (pts[1] - pts[0]).cross(pts[k] - pts[0])
		if n.dot(hint) < 0.0:
			p = pts.duplicate()
			p.reverse()
	for i in range(1, p.size() - 1):
		tri(slot, p[0], p[i], p[i + 1])


func quad(slot: String, a: Vector3, b: Vector3, c: Vector3, d: Vector3, hint: Vector3 = Vector3.ZERO) -> void:
	face(slot, [a, b, c, d], hint)


static func _centroid(pts: Array) -> Vector3:
	var c := Vector3.ZERO
	for q in pts:
		c += q
	return c / float(pts.size())


# ---------------------------------------------------------------------------
# Primitivas
# ---------------------------------------------------------------------------

## Caixa alinhada aos eixos de `mn` a `mx`. `skip` pode conter "+x","-x","+y","-y","+z","-z".
func box(slot: String, mn: Vector3, mx: Vector3, skip: PackedStringArray = PackedStringArray()) -> void:
	var x0 := mn.x; var y0 := mn.y; var z0 := mn.z
	var x1 := mx.x; var y1 := mx.y; var z1 := mx.z
	if not skip.has("+y"):
		quad(slot, Vector3(x0, y1, z1), Vector3(x1, y1, z1), Vector3(x1, y1, z0), Vector3(x0, y1, z0))
	if not skip.has("-y"):
		quad(slot, Vector3(x0, y0, z0), Vector3(x1, y0, z0), Vector3(x1, y0, z1), Vector3(x0, y0, z1))
	if not skip.has("+x"):
		quad(slot, Vector3(x1, y0, z1), Vector3(x1, y0, z0), Vector3(x1, y1, z0), Vector3(x1, y1, z1))
	if not skip.has("-x"):
		quad(slot, Vector3(x0, y0, z0), Vector3(x0, y0, z1), Vector3(x0, y1, z1), Vector3(x0, y1, z0))
	if not skip.has("+z"):
		quad(slot, Vector3(x0, y0, z1), Vector3(x1, y0, z1), Vector3(x1, y1, z1), Vector3(x0, y1, z1))
	if not skip.has("-z"):
		quad(slot, Vector3(x1, y0, z0), Vector3(x0, y0, z0), Vector3(x0, y1, z0), Vector3(x1, y1, z0))


## Caixa centrada em `center` (centro geometrico) com dimensoes `size`.
func box_c(slot: String, center: Vector3, size: Vector3, skip: PackedStringArray = PackedStringArray()) -> void:
	box(slot, center - size * 0.5, center + size * 0.5, skip)


## Caixa com arestas chanfradas (26 faces). Pega luz nas quinas: assinatura low-poly.
## `bevel` em metros; e limitado a 45% da menor dimensao. Base inferior opcional.
func bevel_box(slot: String, mn: Vector3, mx: Vector3, bevel: float, bottom: bool = true) -> void:
	var size := mx - mn
	var r: float = min(bevel, min(size.x, min(size.y, size.z)) * 0.45)
	if r <= 0.0005:
		box(slot, mn, mx, PackedStringArray() if bottom else PackedStringArray(["-y"]))
		return
	var c := (mn + mx) * 0.5
	var h := size * 0.5
	# Vertices de cada canto: px (na face X), py (face Y), pz (face Z).
	var P := {}
	for sx in [-1, 1]:
		for sy in [-1, 1]:
			for sz in [-1, 1]:
				var key := Vector3i(sx, sy, sz)
				P[key] = [
					c + Vector3(sx * h.x, sy * (h.y - r), sz * (h.z - r)),
					c + Vector3(sx * (h.x - r), sy * h.y, sz * (h.z - r)),
					c + Vector3(sx * (h.x - r), sy * (h.y - r), sz * h.z),
				]
	# Faces principais.
	for sx in [-1, 1]:
		var pts := []
		for k in [Vector3i(sx, -1, -1), Vector3i(sx, -1, 1), Vector3i(sx, 1, 1), Vector3i(sx, 1, -1)]:
			pts.append(P[k][0])
		face(slot, pts, Vector3(sx, 0, 0))
	for sy in [-1, 1]:
		if sy < 0 and not bottom:
			continue
		var pts := []
		for k in [Vector3i(-1, sy, -1), Vector3i(1, sy, -1), Vector3i(1, sy, 1), Vector3i(-1, sy, 1)]:
			pts.append(P[k][1])
		face(slot, pts, Vector3(0, sy, 0))
	for sz in [-1, 1]:
		var pts := []
		for k in [Vector3i(-1, -1, sz), Vector3i(1, -1, sz), Vector3i(1, 1, sz), Vector3i(-1, 1, sz)]:
			pts.append(P[k][2])
		face(slot, pts, Vector3(0, 0, sz))
	# Chanfros das 12 arestas.
	for sx in [-1, 1]:
		for sy in [-1, 1]:
			var a: Array = P[Vector3i(sx, sy, -1)]
			var b: Array = P[Vector3i(sx, sy, 1)]
			quad(slot, a[0], b[0], b[1], a[1], Vector3(sx, sy, 0))
	for sx in [-1, 1]:
		for sz in [-1, 1]:
			var a: Array = P[Vector3i(sx, -1, sz)]
			var b: Array = P[Vector3i(sx, 1, sz)]
			quad(slot, a[0], b[0], b[2], a[2], Vector3(sx, 0, sz))
	for sy in [-1, 1]:
		for sz in [-1, 1]:
			var a: Array = P[Vector3i(-1, sy, sz)]
			var b: Array = P[Vector3i(1, sy, sz)]
			quad(slot, a[1], b[1], b[2], a[2], Vector3(0, sy, sz))
	# Triangulos dos 8 cantos.
	for key in P.keys():
		var t: Array = P[key]
		face(slot, [t[0], t[1], t[2]], Vector3(key.x, key.y, key.z))


## Telhado de duas aguas (prisma). Base retangular em y0, cumeeira em y1.
## `ridge_axis` = "x" (cumeeira paralela a X, empenas em +-X) ou "z".
func gable_roof(slot: String, mn_xz: Vector2, mx_xz: Vector2, y0: float, y1: float, ridge_axis: String = "x", bottom: bool = true) -> void:
	var x0 := mn_xz.x; var z0 := mn_xz.y; var x1 := mx_xz.x; var z1 := mx_xz.y
	var c := Vector3((x0 + x1) * 0.5, (y0 + y1) * 0.5, (z0 + z1) * 0.5)
	if ridge_axis == "x":
		var zm := (z0 + z1) * 0.5
		var r0 := Vector3(x0, y1, zm); var r1 := Vector3(x1, y1, zm)
		var a := Vector3(x0, y0, z0); var b := Vector3(x1, y0, z0)
		var d := Vector3(x0, y0, z1); var e := Vector3(x1, y0, z1)
		face(slot, [a, b, r1, r0], _centroid([a, b, r1, r0]) - c)
		face(slot, [d, e, r1, r0], _centroid([d, e, r1, r0]) - c)
		face(slot, [a, d, r0], Vector3(-1, 0, 0))
		face(slot, [b, e, r1], Vector3(1, 0, 0))
		if bottom:
			face(slot, [a, b, e, d], Vector3.DOWN)
	else:
		var xm := (x0 + x1) * 0.5
		var r0 := Vector3(xm, y1, z0); var r1 := Vector3(xm, y1, z1)
		var a := Vector3(x0, y0, z0); var b := Vector3(x0, y0, z1)
		var d := Vector3(x1, y0, z0); var e := Vector3(x1, y0, z1)
		face(slot, [a, b, r1, r0], _centroid([a, b, r1, r0]) - c)
		face(slot, [d, e, r1, r0], _centroid([d, e, r1, r0]) - c)
		face(slot, [a, d, r0], Vector3(0, 0, -1))
		face(slot, [b, e, r1], Vector3(0, 0, 1))
		if bottom:
			face(slot, [a, d, e, b], Vector3.DOWN)


## Telhado de quatro aguas. `ridge_inset` recua a cumeeira das pontas (no eixo da cumeeira).
func hip_roof(slot: String, mn_xz: Vector2, mx_xz: Vector2, y0: float, y1: float, ridge_inset: float, ridge_axis: String = "x", bottom: bool = true) -> void:
	var x0 := mn_xz.x; var z0 := mn_xz.y; var x1 := mx_xz.x; var z1 := mx_xz.y
	var c := Vector3((x0 + x1) * 0.5, (y0 + y1) * 0.5, (z0 + z1) * 0.5)
	var a := Vector3(x0, y0, z0); var b := Vector3(x1, y0, z0)
	var d := Vector3(x1, y0, z1); var e := Vector3(x0, y0, z1)
	var r0: Vector3; var r1: Vector3
	if ridge_axis == "x":
		var zm := (z0 + z1) * 0.5
		var ins: float = min(ridge_inset, (x1 - x0) * 0.5 - 0.001)
		r0 = Vector3(x0 + ins, y1, zm); r1 = Vector3(x1 - ins, y1, zm)
		face(slot, [a, b, r1, r0], _centroid([a, b, r1, r0]) - c)
		face(slot, [d, e, r0, r1], _centroid([d, e, r0, r1]) - c)
		face(slot, [e, a, r0], _centroid([e, a, r0]) - c)
		face(slot, [b, d, r1], _centroid([b, d, r1]) - c)
	else:
		var xm := (x0 + x1) * 0.5
		var ins: float = min(ridge_inset, (z1 - z0) * 0.5 - 0.001)
		r0 = Vector3(xm, y1, z0 + ins); r1 = Vector3(xm, y1, z1 - ins)
		face(slot, [b, d, r1, r0], _centroid([b, d, r1, r0]) - c)
		face(slot, [e, a, r0, r1], _centroid([e, a, r0, r1]) - c)
		face(slot, [a, b, r0], _centroid([a, b, r0]) - c)
		face(slot, [d, e, r1], _centroid([d, e, r1]) - c)
	if bottom:
		face(slot, [a, b, d, e], Vector3.DOWN)


## Tronco de cone / cilindro (raio_inferior, raio_superior) com `sides` lados, eixo Y.
## `base` e o centro da base. `phase_deg` gira os vertices (ex.: 22.5 para octogono "deitado").
func frustum(slot: String, base: Vector3, r_bottom: float, r_top: float, height: float, sides: int = 8, cap_top: bool = true, cap_bottom: bool = true, phase_deg: float = 0.0) -> void:
	var bot := []
	var top := []
	for i in sides:
		var ang := deg_to_rad(phase_deg) + TAU * float(i) / float(sides)
		var dir := Vector3(cos(ang), 0.0, sin(ang))
		bot.append(base + dir * r_bottom)
		top.append(base + dir * r_top + Vector3(0, height, 0))
	var c := base + Vector3(0, height * 0.5, 0)
	for i in sides:
		var j := (i + 1) % sides
		if r_top <= 0.0005:
			face(slot, [bot[i], bot[j], top[i]], _centroid([bot[i], bot[j], top[i]]) - c)
		elif r_bottom <= 0.0005:
			face(slot, [bot[i], top[j], top[i]], _centroid([bot[i], top[j], top[i]]) - c)
		else:
			face(slot, [bot[i], bot[j], top[j], top[i]], _centroid([bot[i], bot[j], top[j], top[i]]) - c)
	if cap_top and r_top > 0.0005:
		face(slot, top, Vector3.UP)
	if cap_bottom and r_bottom > 0.0005:
		face(slot, bot, Vector3.DOWN)


func cylinder(slot: String, base: Vector3, radius: float, height: float, sides: int = 8, cap_top: bool = true, cap_bottom: bool = true, phase_deg: float = 0.0) -> void:
	frustum(slot, base, radius, radius, height, sides, cap_top, cap_bottom, phase_deg)


## Cilindro deitado ao longo de X (ex.: rodas, eixos). `center` = centro do cilindro.
func cylinder_x(slot: String, center: Vector3, radius: float, length: float, sides: int = 10, phase_deg: float = 0.0) -> void:
	push_basis(Basis(Vector3(0, 0, 1), deg_to_rad(-90.0)), center + Vector3(-length * 0.5, 0, 0))
	cylinder(slot, Vector3.ZERO, radius, length, sides, true, true, phase_deg)
	pop()


## Icosfera facetada com deslocamento deterministico (seed) para copas/rochas/cabecas.
## `scale` deforma (ex.: Vector3(1, 0.8, 1) achata). subdiv 0 = 20 faces, 1 = 80 faces.
func icosphere(slot: String, center: Vector3, radius: float, subdiv: int = 1, jitter: float = 0.0, rng_seed: int = 1, scale: Vector3 = Vector3.ONE) -> void:
	var t := (1.0 + sqrt(5.0)) / 2.0
	var verts: Array = [
		Vector3(-1, t, 0), Vector3(1, t, 0), Vector3(-1, -t, 0), Vector3(1, -t, 0),
		Vector3(0, -1, t), Vector3(0, 1, t), Vector3(0, -1, -t), Vector3(0, 1, -t),
		Vector3(t, 0, -1), Vector3(t, 0, 1), Vector3(-t, 0, -1), Vector3(-t, 0, 1),
	]
	for i in verts.size():
		verts[i] = (verts[i] as Vector3).normalized()
	var faces: Array = [
		[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11],
		[1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8],
		[3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9],
		[4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1],
	]
	for _s in subdiv:
		var cache := {}
		var nf: Array = []
		for f in faces:
			var m: Array = []
			for e in [[f[0], f[1]], [f[1], f[2]], [f[2], f[0]]]:
				var key := Vector2i(min(e[0], e[1]), max(e[0], e[1]))
				if not cache.has(key):
					verts.append(((verts[e[0]] as Vector3) + (verts[e[1]] as Vector3)).normalized())
					cache[key] = verts.size() - 1
				m.append(cache[key])
			nf.append([f[0], m[0], m[2]])
			nf.append([f[1], m[1], m[0]])
			nf.append([f[2], m[2], m[1]])
			nf.append([m[0], m[1], m[2]])
		faces = nf
	var rng := RandomNumberGenerator.new()
	rng.seed = rng_seed
	var pos: Array = []
	for v in verts:
		var k := 1.0 + (rng.randf_range(-jitter, jitter) if jitter > 0.0 else 0.0)
		pos.append(center + (v as Vector3) * radius * k * scale)
	for f in faces:
		var a: Vector3 = pos[f[0]]; var b: Vector3 = pos[f[1]]; var c: Vector3 = pos[f[2]]
		face(slot, [a, b, c], _centroid([a, b, c]) - center)


## Extrusao vertical de um poligono simples no plano XZ (Vector2(x, z)) de y0 a y1.
func extrude(slot: String, poly_xz: PackedVector2Array, y0: float, y1: float, cap_top: bool = true, cap_bottom: bool = true, sides: bool = true) -> void:
	var n := poly_xz.size()
	if n < 3:
		return
	# Area com sinal no plano (x, z): define o lado "de fora" das arestas.
	var area := 0.0
	for i in n:
		var p := poly_xz[i]; var q := poly_xz[(i + 1) % n]
		area += p.x * q.y - q.x * p.y
	var idx := Geometry2D.triangulate_polygon(poly_xz)
	for i in range(0, idx.size(), 3):
		var a := poly_xz[idx[i]]; var b := poly_xz[idx[i + 1]]; var c := poly_xz[idx[i + 2]]
		if cap_top:
			face(slot, [Vector3(a.x, y1, a.y), Vector3(b.x, y1, b.y), Vector3(c.x, y1, c.y)], Vector3.UP)
		if cap_bottom:
			face(slot, [Vector3(a.x, y0, a.y), Vector3(b.x, y0, b.y), Vector3(c.x, y0, c.y)], Vector3.DOWN)
	if not sides:
		return
	for i in n:
		var p := poly_xz[i]; var q := poly_xz[(i + 1) % n]
		var edge := q - p
		var out2 := Vector2(edge.y, -edge.x) if area > 0.0 else Vector2(-edge.y, edge.x)
		face(slot, [Vector3(p.x, y0, p.y), Vector3(q.x, y0, q.y), Vector3(q.x, y1, q.y), Vector3(p.x, y1, p.y)], Vector3(out2.x, 0, out2.y))


## Viga/tira entre dois pontos com secao quadrada `thickness` (ex.: corrimaos, hastes).
func beam(slot: String, from: Vector3, to: Vector3, thickness: float) -> void:
	var d := to - from
	var length := d.length()
	if length < 1e-5:
		return
	var y := d / length
	var ref := Vector3.UP if abs(y.dot(Vector3.UP)) < 0.95 else Vector3.RIGHT
	var x := ref.cross(y).normalized()
	var z := x.cross(y).normalized()
	push(Transform3D(Basis(x, y, z), from))
	box(slot, Vector3(-thickness * 0.5, 0, -thickness * 0.5), Vector3(thickness * 0.5, length, thickness * 0.5))
	pop()


# ---------------------------------------------------------------------------
# Resultado
# ---------------------------------------------------------------------------

func is_empty() -> bool:
	return _order.is_empty()


func slot_names() -> PackedStringArray:
	return _order


func triangle_count() -> int:
	var t := 0
	for k in _order:
		t += (_slots[k] as SlotData).verts.size() / 3
	return t


## Gera o ArrayMesh. `material_for` e um Callable(slot: String) -> Material.
## Uma superficie por slot, na ordem de primeira utilizacao, nomeada com o slot.
func commit(material_for: Callable) -> ArrayMesh:
	var mesh := ArrayMesh.new()
	for name in _order:
		var s: SlotData = _slots[name]
		if s.verts.is_empty():
			continue
		var st := SurfaceTool.new()
		st.begin(Mesh.PRIMITIVE_TRIANGLES)
		for i in s.verts.size():
			st.set_normal(s.norms[i])
			st.set_uv(s.uvs[i])
			st.add_vertex(s.verts[i])
		st.index()
		var mat: Material = material_for.call(name)
		if mat != null:
			st.set_material(mat)
		st.commit(mesh)
		mesh.surface_set_name(mesh.get_surface_count() - 1, name)
	return mesh
