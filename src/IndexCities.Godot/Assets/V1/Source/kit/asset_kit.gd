## Contexto do gerador de assets da amostra V1 (somente visual, uso em ferramenta).
##
## Recebido por cada receita em `build(kit)`. Salva materiais, malhas e cenas nativas
## Godot 4 em texto (`.tres` / `.tscn`) dentro de res://Assets/V1 e coleta os dados do
## manifesto (envelope, marcadores candidatos, superficies, triangulos).
##
## Nao executa gameplay, nao conhece simulacao e nao e carregado pelo jogo em runtime.
extends RefCounted

const ROOT := "res://Assets/V1"
const Builder := preload("res://Assets/V1/Source/kit/mesh_builder.gd")
const Palette := preload("res://Assets/V1/Source/kit/palette.gd")

## Status gravado em toda cena gerada (metadado da raiz).
const ASSET_STATUS := "amostra V1 para compatibilizacao - revisao humana PENDENTE"

var entries: Array = []
var errors: PackedStringArray = PackedStringArray()
var _material_cache: Dictionary = {}


func builder():
	return Builder.new()


func err(msg: String) -> void:
	errors.append(msg)
	push_error(msg)


# ---------------------------------------------------------------------------
# Materiais
# ---------------------------------------------------------------------------

func material_path(mat_name: String) -> String:
	return "%s/Materials/%s.tres" % [ROOT, mat_name]


func _save_material(mat_name: String, def: Dictionary, force: bool) -> void:
	var path := material_path(mat_name)
	if not force and ResourceLoader.exists(path):
		return
	var m := Palette.make_material(def)
	m.resource_name = mat_name
	var e := ResourceSaver.save(m, path)
	if e != OK:
		err("Falha ao salvar material %s (%s)" % [path, error_string(e)])


## Grava os materiais da paleta base. `force` regrava mesmo se ja existirem.
func ensure_palette(force: bool) -> void:
	for k in Palette.MATERIALS.keys():
		_save_material(k, Palette.MATERIALS[k], force)


## Materiais locais de uma receita (`const MATERIALS` na receita). Nomes nao podem
## colidir com a paleta base e DEVEM comecar com `const MATERIAL_PREFIX` da receita.
func ensure_materials(defs: Dictionary, prefix: String) -> void:
	for k in defs.keys():
		if Palette.MATERIALS.has(k):
			err("Material local '%s' colide com a paleta base" % k)
			continue
		if prefix == "" or not str(k).begins_with(prefix):
			err("Material local '%s' precisa comecar com MATERIAL_PREFIX '%s'" % [k, prefix])
			continue
		# Sempre regravado: so a receita dona (prefixo) escreve estes arquivos.
		_save_material(k, defs[k], true)


func mat(mat_name: String) -> Material:
	if _material_cache.has(mat_name):
		return _material_cache[mat_name]
	var path := material_path(mat_name)
	if not ResourceLoader.exists(path):
		err("Material inexistente: %s (defina na paleta ou em MATERIALS da receita)" % path)
		return null
	var m: Material = load(path)
	_material_cache[mat_name] = m
	return m


# ---------------------------------------------------------------------------
# Malhas
# ---------------------------------------------------------------------------

## Converte o builder em ArrayMesh, salva em Meshes/<file_id>.tres e devolve o recurso
## recarregado do disco (assim as cenas referenciam o arquivo, sem embutir a malha).
func save_mesh(b, file_id: String) -> ArrayMesh:
	if b.is_empty():
		err("Malha vazia: %s" % file_id)
		return null
	var mesh: ArrayMesh = b.commit(Callable(self, "mat"))
	mesh.resource_name = file_id
	var path := "%s/Meshes/%s.tres" % [ROOT, file_id]
	var e := ResourceSaver.save(mesh, path)
	if e != OK:
		err("Falha ao salvar malha %s (%s)" % [path, error_string(e)])
		return null
	return ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REPLACE)


static func mesh_triangles(mesh: Mesh) -> int:
	var t := 0
	for s in mesh.get_surface_count():
		var il: int = mesh.surface_get_array_index_len(s)
		t += (il if il > 0 else mesh.surface_get_array_len(s)) / 3
	return t


# ---------------------------------------------------------------------------
# Nos de cena
# ---------------------------------------------------------------------------

func new_root(node_name: String, visual_id: String) -> Node3D:
	var r := Node3D.new()
	r.name = node_name
	r.set_meta("visual_id", visual_id)
	r.set_meta("asset_status", ASSET_STATUS)
	return r


func add_node(parent: Node, node_name: String, xform: Transform3D = Transform3D.IDENTITY) -> Node3D:
	var n := Node3D.new()
	n.name = node_name
	n.transform = xform
	parent.add_child(n)
	return n


func add_mesh(parent: Node, node_name: String, mesh: Mesh, xform: Transform3D = Transform3D.IDENTITY) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = node_name
	mi.mesh = mesh
	mi.transform = xform
	parent.add_child(mi)
	return mi


## Marcador CANDIDATO (porta, carga, conector...). Convencao: o eixo -Z local do
## marcador aponta para `forward` (frente Godot), p.ex. para fora da porta.
## Nao e acesso funcional: so a integracao/simulacao define acessos reais.
func add_marker(parent: Node, node_name: String, pos: Vector3, forward: Vector3 = Vector3.FORWARD, note: String = "") -> Marker3D:
	var m := Marker3D.new()
	m.name = node_name
	var f := forward.normalized()
	var up := Vector3.UP if abs(f.dot(Vector3.UP)) < 0.99 else Vector3.BACK
	m.transform = Transform3D(Basis.looking_at(f, up), pos)
	m.gizmo_extents = 0.4
	m.set_meta("candidate", true)
	if note != "":
		m.set_meta("note", note)
	parent.add_child(m)
	return m


## Instancia outra cena gerada (ex.: poste dentro de um conjunto de amostra).
func instance(parent: Node, scene_path: String, node_name: String, xform: Transform3D = Transform3D.IDENTITY) -> Node3D:
	var ps: PackedScene = load(scene_path)
	if ps == null:
		err("Cena nao encontrada para instanciar: %s" % scene_path)
		return null
	var n: Node3D = ps.instantiate()
	n.name = node_name
	n.transform = xform
	parent.add_child(n)
	return n


static func _own(node: Node, root: Node) -> void:
	for c in node.get_children():
		c.owner = root
		if c.scene_file_path == "":
			_own(c, root)


static func _rel_xform(node: Node, root: Node) -> Transform3D:
	var t := Transform3D.IDENTITY
	var n := node
	while n != null and n != root:
		if n is Node3D:
			t = (n as Node3D).transform * t
		n = n.get_parent()
	return t


static func _v(v: Vector3) -> Array:
	return [snappedf(v.x, 0.001), snappedf(v.y, 0.001), snappedf(v.z, 0.001)]


static func _collect(node: Node, out: Array) -> void:
	for c in node.get_children():
		out.append(c)
		_collect(c, out)


# ---------------------------------------------------------------------------
# Cena + manifesto
# ---------------------------------------------------------------------------

## Salva a cena em Scenes/<file_id>.tscn e registra a entrada de manifesto.
## `info` aceita: family_id, piece, title_pt, proposed (Dictionary de medidas candidatas),
## parts (Dictionary no -> descricao), notes (Array de strings), lot (Vector2 X,Z do lote
## logico do host: o build falha se o envelope exceder), envelope_include (AABB extra, ex.:
## disco varrido por rotor). Nos chamados State_* sao listados no manifesto (estado real).
func save_scene(root: Node3D, file_id: String, info: Dictionary = {}) -> String:
	var path := "%s/Scenes/%s.tscn" % [ROOT, file_id]
	var nodes: Array = []
	_collect(root, nodes)
	var aabb := AABB()
	var has_aabb := false
	var meshes: Array = []
	var markers: Array = []
	var tris_total := 0
	var surfaces_total := 0
	for n in nodes:
		if n is MeshInstance3D and (n as MeshInstance3D).mesh != null:
			var mi := n as MeshInstance3D
			var xf := _rel_xform(mi, root)
			var box: AABB = xf * mi.mesh.get_aabb()
			# Envelope = extensao MAXIMA: inclui malhas de estado ocultas por padrao (State_*).
			aabb = box if not has_aabb else aabb.merge(box)
			has_aabb = true
			var t := mesh_triangles(mi.mesh)
			tris_total += t
			surfaces_total += mi.mesh.get_surface_count()
			var snames := []
			for s in mi.mesh.get_surface_count():
				snames.append(mi.mesh.call("surface_get_name", s) if mi.mesh is ArrayMesh else str(s))
			meshes.append({
				"node": str(root.get_path_to(mi)),
				"mesh": mi.mesh.resource_path,
				"surfaces": snames,
				"triangles": t,
				"visible_by_default": mi.visible,
			})
		elif n is Marker3D:
			var mk := n as Marker3D
			var xf := _rel_xform(mk, root)
			markers.append({
				"node": str(root.get_path_to(mk)),
				"position": _v(xf.origin),
				"forward": _v((-xf.basis.z).normalized()),
				"note": str(mk.get_meta("note", "")),
			})
	# Volume extra declarado pela receita (ex.: disco varrido por um rotor que gira).
	if info.has("envelope_include"):
		var extra: AABB = info["envelope_include"]
		aabb = extra if not has_aabb else aabb.merge(extra)
		has_aabb = true
	# Nos de estado: so o host liga/desliga a partir de estado REAL da simulacao.
	var states: Array = []
	for n in nodes:
		if String(n.name).begins_with("State_"):
			states.append({"node": str(root.get_path_to(n)), "visible_by_default": (n as Node3D).visible if n is Node3D else true})
	if info.has("lot"):
		var lot: Vector2 = info["lot"]
		root.set_meta("lot_candidate_m", lot)
		# Pivo = centro do lote (o host poe Position no centro e a porta em z = -D/2).
		var hx := lot.x * 0.5 + 0.001
		var hz := lot.y * 0.5 + 0.001
		if has_aabb and (aabb.position.x < -hx or aabb.end.x > hx or aabb.position.z < -hz or aabb.end.z > hz):
			err("%s: envelope x[%.2f, %.2f] z[%.2f, %.2f] sai do lote centrado %.1f x %.1f" % [file_id, aabb.position.x, aabb.end.x, aabb.position.z, aabb.end.z, lot.x, lot.y])
	if has_aabb:
		root.set_meta("visual_envelope_min", aabb.position)
		root.set_meta("visual_envelope_size", aabb.size)
	_own(root, root)
	var ps := PackedScene.new()
	var pe := ps.pack(root)
	if pe != OK:
		err("Falha ao empacotar cena %s (%s)" % [path, error_string(pe)])
	else:
		var e := ResourceSaver.save(ps, path)
		if e != OK:
			err("Falha ao salvar cena %s (%s)" % [path, error_string(e)])
	var entry := {
		"visual_id": str(root.get_meta("visual_id", "")),
		"family_id": str(info.get("family_id", root.get_meta("visual_id", ""))),
		"piece": str(info.get("piece", "")),
		"title_pt": str(info.get("title_pt", "")),
		"scene": path,
		"root_node": str(root.name),
		"envelope": {"min": _v(aabb.position), "max": _v(aabb.end), "size": _v(aabb.size)} if has_aabb else {},
		"markers": markers,
		"meshes": meshes,
		"triangles_total": tris_total,
		"mesh_surfaces_total": surfaces_total,
		"proposed": info.get("proposed", {}),
		"parts": info.get("parts", {}),
		"notes": info.get("notes", []),
		"lot_m": [info["lot"].x, info["lot"].y] if info.has("lot") else [],
		"state_nodes": states,
		"status": ASSET_STATUS,
	}
	entries.append(entry)
	root.free()
	return path


## Funde as entradas desta execucao no manifesto existente (substitui as da mesma cena).
## Usado por builds parciais (--only) para nao regenerar familias ja integradas.
func merge_manifest(path: String, built: PackedStringArray) -> void:
	var old_entries: Array = []
	var old_built: Array = []
	if FileAccess.file_exists(path):
		var parsed = JSON.parse_string(FileAccess.get_file_as_string(path))
		if parsed is Dictionary:
			old_entries = parsed.get("entries", [])
			old_built = parsed.get("built_families", [])
	var fresh := {}
	for e in entries:
		fresh[e["scene"]] = true
	var merged: Array = []
	for e in old_entries:
		if not fresh.has(e.get("scene", "")):
			merged.append(e)
	merged.append_array(entries)
	merged.sort_custom(func(a, b): return String(a["scene"]) < String(b["scene"]))
	var fams: Array = old_built.duplicate()
	for f in built:
		if not fams.has(f):
			fams.append(f)
	entries = merged
	write_manifest(path, {"built_families": fams})


func write_manifest(path: String, extra: Dictionary = {}) -> void:
	var data := {
		"schema": "indexcities.assets.v1.sample_manifest/1",
		"status": ASSET_STATUS,
		"conventions": {
			"up_axis": "+Y",
			"unit": "1 unidade = 1 metro (candidato, nao aprova escala de gameplay)",
			"facade_candidate": "-Z",
			"pivot": "no solo (y = 0)",
			"marker_forward": "eixo -Z local do Marker3D aponta para 'forward'",
		},
		"entries": entries,
	}
	data.merge(extra, true)
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		err("Falha ao abrir manifesto %s" % path)
		return
	f.store_string(JSON.stringify(data, "  ", false))
	f.close()
