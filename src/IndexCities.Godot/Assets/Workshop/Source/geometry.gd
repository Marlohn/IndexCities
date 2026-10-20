## Geometria original de autoria. Sem dependencia da simulacao ou de outros kits.
extends RefCounted

var slots: Dictionary = {}
var transform := Transform3D.IDENTITY

func face(points: Array, outward: Vector3, color: Color, slot: String = "paint") -> void:
	var p := points.duplicate()
	if (p[1] - p[0]).cross(p[2] - p[0]).dot(outward) < 0.0:
		p.reverse()
	var normal := (transform.basis * outward).normalized()
	if not slots.has(slot):
		slots[slot] = {"v": PackedVector3Array(), "n": PackedVector3Array(), "c": PackedColorArray()}
	var s: Dictionary = slots[slot]
	for i in range(1, p.size() - 1):
		# Ordem horaria das faces frontais do Godot.
		for q in [p[0], p[i + 1], p[i]]:
			s.v.append(transform * q)
			s.n.append(normal)
			s.c.append(color)

func box(center: Vector3, size: Vector3, color: Color, bevel: float = 0.025, slot: String = "paint") -> void:
	var h := size * 0.5
	var b := minf(bevel, minf(h.x, minf(h.y, h.z)) * 0.48)
	# Chanfro menor que 2 cm nao aparece na camera do jogo: caixa simples (12 em vez de 44 triangulos).
	if b < 0.02:
		b = 0.0
	var inside := h - Vector3.ONE * b
	for axis in 3:
		var j := (axis + 1) % 3
		var k := (axis + 2) % 3
		for sign_a in [-1.0, 1.0]:
			var n := Vector3.ZERO
			n[axis] = sign_a
			var pts: Array = []
			for v in [Vector2(-1,-1), Vector2(1,-1), Vector2(1,1), Vector2(-1,1)]:
				var q := center
				q[axis] += h[axis] * sign_a
				q[j] += inside[j] * v.x
				q[k] += inside[k] * v.y
				pts.append(q)
			face(pts, n, color, slot)
	if b < 0.00001:
		return
	for axis in 3:
		var j := (axis + 1) % 3
		var k := (axis + 2) % 3
		for sj in [-1.0, 1.0]:
			for sk in [-1.0, 1.0]:
				var n := Vector3.ZERO
				n[j] = sj
				n[k] = sk
				var pts: Array = []
				for values in [Vector3(-1,1,0), Vector3(1,1,0), Vector3(1,0,1), Vector3(-1,0,1)]:
					var q := center
					q[axis] += inside[axis] * values.x
					q[j] += sj * (inside[j] + b * values.y)
					q[k] += sk * (inside[k] + b * values.z)
					pts.append(q)
				face(pts, n.normalized(), color.lightened(0.025), slot)
	for sx in [-1.0, 1.0]:
		for sy in [-1.0, 1.0]:
			for sz in [-1.0, 1.0]:
				var s := Vector3(sx, sy, sz)
				face([center + s * (inside + Vector3(b,0,0)), center + s * (inside + Vector3(0,b,0)), center + s * (inside + Vector3(0,0,b))], s.normalized(), color.lightened(0.04), slot)

func beam(a: Vector3, b: Vector3, width: float, depth: float, color: Color, slot: String = "paint") -> void:
	var prior := transform
	var y := (b - a).normalized()
	var x := y.cross(Vector3.FORWARD).normalized()
	if x.length_squared() < 0.1:
		x = y.cross(Vector3.RIGHT).normalized()
	var z := x.cross(y).normalized()
	transform = prior * Transform3D(Basis(x, y, z), (a + b) * 0.5)
	box(Vector3.ZERO, Vector3(width, a.distance_to(b), depth), color, minf(width,depth)*0.18, slot)
	transform = prior

func cylinder(center: Vector3, radius: float, height: float, color: Color, segments: int = 16, axis: Vector3 = Vector3.UP, slot: String = "paint", top_radius: float = -1.0) -> void:
	var prior := transform
	var y := axis.normalized()
	var x := y.cross(Vector3.FORWARD).normalized()
	if x.length_squared() < 0.1:
		x = y.cross(Vector3.RIGHT).normalized()
	transform = prior * Transform3D(Basis(x,y,x.cross(y).normalized()), center)
	var r2 := radius if top_radius < 0 else top_radius
	for i in segments:
		var a := TAU * i / segments
		var b := TAU * (i + 1) / segments
		var p := Vector3(cos(a)*radius,-height/2,sin(a)*radius)
		var q := Vector3(cos(b)*radius,-height/2,sin(b)*radius)
		var r := Vector3(cos(b)*r2,height/2,sin(b)*r2)
		var s := Vector3(cos(a)*r2,height/2,sin(a)*r2)
		face([p,q,r,s], Vector3(cos((a+b)/2), (radius-r2)/maxf(height,0.001),sin((a+b)/2)).normalized(), color, slot)
		face([Vector3(0,height/2,0),s,r], Vector3.UP, color.lightened(0.02), slot)
		face([Vector3(0,-height/2,0),q,p], Vector3.DOWN, color.darkened(0.1), slot)
	transform = prior

func tyre(center: Vector3, radius: float = 0.37, width: float = 0.22, axis: Vector3 = Vector3.RIGHT) -> void:
	var prior := transform
	var y := axis.normalized()
	var x := y.cross(Vector3.FORWARD).normalized()
	if x.length_squared() < 0.1:
		x = Vector3.RIGHT
	transform = prior * Transform3D(Basis(x,y,x.cross(y).normalized()), center)
	var section := [Vector2(0.55,-0.5),Vector2(0.85,-0.5),Vector2(1,-0.30),Vector2(1,0.3),Vector2(0.85,0.5),Vector2(0.55,0.5)]
	for i in 20:
		var a := TAU*i/20
		var b := TAU*(i+1)/20
		for j in section.size():
			var s: Vector2 = section[j]
			var t: Vector2 = section[(j+1)%section.size()]
			var pts := [Vector3(cos(a)*s.x*radius,s.y*width,sin(a)*s.x*radius),Vector3(cos(b)*s.x*radius,s.y*width,sin(b)*s.x*radius),Vector3(cos(b)*t.x*radius,t.y*width,sin(b)*t.x*radius),Vector3(cos(a)*t.x*radius,t.y*width,sin(a)*t.x*radius)]
			var n: Vector3 = (pts[1]-pts[0]).cross(pts[2]-pts[0]).normalized()
			face(pts,n,Color("252b30").lightened(0.018*(j%2)),"rubber")
	transform = prior

func commit(materials: Dictionary, palette_uv: Callable) -> ArrayMesh:
	var mesh := ArrayMesh.new()
	for key in slots:
		var s: Dictionary = slots[key]
		var arrays := []
		arrays.resize(Mesh.ARRAY_MAX)
		arrays[Mesh.ARRAY_VERTEX] = s.v
		arrays[Mesh.ARRAY_NORMAL] = s.n
		# A pequena paleta sRGB evita a interpretacao diferente de vertex colors
		# entre GL Compatibility, Forward+ e o padrao glTF (cores lineares).
		var uvs := PackedVector2Array()
		for color in s.c:
			uvs.append(palette_uv.call(color))
		arrays[Mesh.ARRAY_TEX_UV] = uvs
		mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
		mesh.surface_set_material(mesh.get_surface_count()-1,materials[key])
		mesh.surface_set_name(mesh.get_surface_count()-1,key)
	return mesh
