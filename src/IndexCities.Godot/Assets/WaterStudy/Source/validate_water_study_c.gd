## IndexCities - WaterStudy - AMOSTRA C: validador headless (ferramenta de autoria; NAO e jogo).
## Revisao humana: PENDENTE.
##
##   godot --headless --path <projeto> --script res://Assets/WaterStudy/Source/validate_water_study_c.gd -- \
##         --report=<json> [--generated=res://_generated] [--ab-manifest=<json com sha256 de A/B no HEAD>]
##
## O relatorio lista COMPORTAMENTOS verificados (cada um com as asserções que o compoem), para nao
## confundir quantidade de assercoes com quantidade de testes. Nada aqui avalia estetica.
##   geometria  : 3 lagos reais (ws_c_cases.json) + envelope sintetico de todas as seeds (raio 28-40 m,
##                centros nos cantos e no meio da faixa medida nas seeds 1-5000): agua ate o circulo,
##                sem chao dentro do circulo, sem fundo seco, rebaixo so dentro do circulo, sem rio,
##                cobertura sem buraco/sobreposicao, faces, determinismo, janela dentro do mapa.
##   shaders    : carregam, uniforms, sem tela/profundidade/textura/discard/uniform global ou de
##                instancia, vertex() nao desloca, transparencia so na agua, relogio host_time_s.
##   cenas      : sem script/fisica/marcador proprio, luzes, materiais, metadados, exemplos de
##                limite ocultos, malha salva == malha regenerada a partir do centro/raio reais.
##   dados      : casos conferidos pelas regras reais na ferramenta de dados (0 falhas).
##   preservacao: arquivos de A/B iguais ao HEAD (se --ab-manifest), sem imagem fora de Captures,
##                README com o estado e a recomendacao exigidos.
extends SceneTree

const LakeC := preload("res://Assets/WaterStudy/Source/ws_lake_c.gd")
const ROOT := "res://Assets/WaterStudy"
const CASES := ROOT + "/Source/ws_c_cases.json"
const EPS_R := 0.002  # m; vertices em float32 a ~200 m da origem

var _behaviours := {}
var _current := ""
var _fails: Array[String] = []
var _asserts := 0
var _report := {}


func _initialize() -> void:
	_run.call_deferred()


func _begin(name: String) -> void:
	_current = name
	if not _behaviours.has(name):
		_behaviours[name] = {"assertions": 0, "failures": []}


func _check(ok: bool, what: String) -> void:
	_asserts += 1
	_behaviours[_current]["assertions"] += 1
	if not ok:
		_behaviours[_current]["failures"].append(what)
		_fails.append("%s: %s" % [_current, what])
		push_error("[validate-c] FALHA %s: %s" % [_current, what])


func _run() -> void:
	var report_path := ""
	var generated := "res://_generated"
	var manifest := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--report="):
			report_path = a.substr(9)
		elif a.begins_with("--generated="):
			generated = a.substr(12)
		elif a.begins_with("--ab-manifest="):
			manifest = a.substr(14)
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(CASES))
	_data(data)
	_shaders()
	_geometry(data)
	_scenes(data, generated)
	_preservation(manifest)
	_readme()
	var summary := {}
	for k in _behaviours:
		summary[k] = {"ok": (_behaviours[k]["failures"] as Array).is_empty(), "assertions": _behaviours[k]["assertions"], "failures": _behaviours[k]["failures"]}
	_report["behaviours"] = summary
	_report["behaviours_total"] = _behaviours.size()
	_report["behaviours_failed"] = summary.values().filter(func(b): return not b["ok"]).size()
	_report["assertions_total"] = _asserts
	_report["failures"] = _fails
	if report_path != "":
		var f := FileAccess.open(report_path, FileAccess.WRITE)
		f.store_string(JSON.stringify(_report, "  ", false))
		f.close()
	print("[validate-c] %d comportamentos (%d assercoes), %d comportamentos com falha" % [_behaviours.size(), _asserts, _report["behaviours_failed"]])
	quit(0 if _fails.is_empty() else 1)


# ------------------------------------------------------------------------------------- dados
func _data(data: Dictionary) -> void:
	_begin("dados: lagos reais conferidos pelas regras do jogo (ferramenta C#)")
	var cases: Array = data["cases"]
	_check(cases.size() >= 2, "mais de uma seed (%d)" % cases.size())
	_check(bool(data["source"]["all_checks_ok"]), "ferramenta de dados sem falha")
	var seeds := {}
	for c in cases:
		seeds[int(c["seed"])] = true
		_check(int(c["checks"]["failures"]) == 0 and int(c["checks"]["count"]) > 0, "seed %d: %d verificacoes reais, 0 falhas" % [c["seed"], c["checks"]["count"]])
		var r := float(c["lake_radius"])
		_check(r >= 28.0 and r <= 40.0, "seed %d: raio %.3f dentro de 28 + 12*U" % [c["seed"], r])
		_check(c["limits"].size() >= 1, "seed %d: exemplos de limite presentes" % c["seed"])
	_check(seeds.size() == cases.size() and seeds.has(42), "seeds distintas e a vitrine 42 presente")
	_report["seeds"] = seeds.keys()


# ----------------------------------------------------------------------------------- shaders
func _strip(code: String) -> String:
	var out := ""
	for line in code.split("\n"):
		var c := line.find("//")
		out += (line.substr(0, c) if c >= 0 else line) + "\n"
	return out


func _body(code: String, fn: String) -> String:
	var at := code.find("void %s()" % fn)
	if at < 0:
		return ""
	var open := code.find("{", at)
	var depth := 0
	for i in range(open, code.length()):
		if code[i] == "{":
			depth += 1
		elif code[i] == "}":
			depth -= 1
			if depth == 0:
				return code.substr(open, i - open + 1)
	return ""


func _shaders() -> void:
	_begin("shaders: GL Compatibility, uma passada, sem recursos proibidos")
	var expected := {
		"ws_water_c": ["host_time_s", "fresnel_min", "night_sky_floor", "reflection_gain", "lamp_glint_strength", "ripple_strength"],
		"ws_lakebed_c": ["absorption_per_m", "water_body_color", "silt_color", "bank_soil_color"],
	}
	var include := _strip(FileAccess.get_file_as_string(ROOT + "/Shaders/ws_common.gdshaderinc"))
	var forbidden := ["SCREEN_TEXTURE", "hint_screen_texture", "DEPTH_TEXTURE", "hint_depth_texture", "texture(",
		"sampler2D", "discard", "global uniform", "instance uniform", "EMISSION"]
	var info := {}
	for name in expected:
		var sh := load(ROOT + "/Shaders/%s.gdshader" % name) as Shader
		_check(sh != null, "carrega %s" % name)
		if sh == null:
			continue
		var uniforms := []
		for u in sh.get_shader_uniform_list():
			uniforms.append(u["name"])
		for u in expected[name]:
			_check(u in uniforms, "%s: uniform %s analisado pela Godot" % [name, u])
		var bare := _strip(sh.code)
		for token in forbidden:
			_check((bare + include).find(token) < 0, "%s sem %s" % [name, token])
		var vtx := _body(bare, "vertex")
		for token in ["VERTEX =", "VERTEX +=", "VERTEX -=", "VERTEX *=", "POSITION", "NORMAL ="]:
			_check(vtx.find(token) < 0, "%s: vertex() nao altera %s" % [name, token.strip_edges()])
		# Nenhuma constante de seed nos shaders (o lago vem da malha).
		for lit in ["152.6", "85.98", "28.57"]:
			_check(bare.find(lit) < 0, "%s sem constante da seed 42 (%s)" % [name, lit])
		info[name] = {"uniforms": uniforms.size(), "chars": sh.code.length()}
	var water := _strip((load(ROOT + "/Shaders/ws_water_c.gdshader") as Shader).code)
	_check(water.find("render_mode blend_premul_alpha, depth_draw_opaque, cull_back;") >= 0 and water.count("render_mode") == 1,
		"agua: unica passada transparente pre-multiplicada declarada")
	_check(water.find("host_time_s >= 0.0 ? host_time_s : TIME") >= 0, "agua: relogio decorativo do host (para na pausa) como na IA-020")
	var bed := _strip((load(ROOT + "/Shaders/ws_lakebed_c.gdshader") as Shader).code)
	_check(bed.find("render_mode") < 0 and bed.find("ALPHA") < 0, "fundo: opaco, sem render_mode nem ALPHA")
	_check(bed.find("TIME") < 0 and bed.find("host_time_s") < 0, "fundo: estatico (nenhum relogio)")
	for m in [["ws_c_water", "ws_water_c"], ["ws_c_lakebed", "ws_lakebed_c"]]:
		var txt := FileAccess.get_file_as_string(ROOT + "/Materials/%s.tres" % m[0])
		_check(txt.find("shader_parameter/") < 0 and txt.find("/Shaders/%s.gdshader" % m[1]) >= 0, "material %s so aponta o shader" % m[0])
	var gen := FileAccess.get_file_as_string(ROOT + "/Source/ws_lake_c.gd")
	for lit in ["152.6", "85.98", "28.57", "1412", "2877"]:
		_check(gen.find(lit) < 0, "gerador sem constante de seed (%s)" % lit)
	_report["shaders"] = info


# --------------------------------------------------------------------------------- geometria
func _lakes(data: Dictionary) -> Array:
	var out := []
	for c in data["cases"]:
		out.append(["seed %d" % int(c["seed"]), Vector2(c["lake_centre"][0], c["lake_centre"][1]), float(c["lake_radius"])])
	# Envelope medido nas seeds 1-5000 (ferramenta de dados): x [151,86; 175,83], z [85,29; 109,26], R [28; 40).
	for r in [28.0, 34.0, 39.999]:
		for cx in [151.86, 175.83]:
			for cz in [85.29, 109.26]:
				out.append(["envelope R%.0f (%.2f; %.2f)" % [r, cx, cz], Vector2(cx, cz), r])
		out.append(["envelope R%.0f meio" % r, Vector2(163.85, 97.28), r])
	return out


func _arrays(m: ArrayMesh) -> Array:
	return m.surface_get_arrays(0)


func _tri_area(a: Vector3, b: Vector3, c: Vector3) -> float:
	return absf((b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z)) * 0.5


func _geometry(data: Dictionary) -> void:
	var gen = LakeC.new()
	var wy := LakeC.water_y()
	var table := []
	for lake in _lakes(data):
		var label: String = lake[0]
		var c: Vector2 = lake[1]
		var r: float = lake[2]
		var m: Dictionary = gen.build(c, r)
		var m2: Dictionary = LakeC.new().build(c, r)
		var w := _arrays(m["water"])
		var g := _arrays(m["ground"])
		var b := _arrays(m["bed"])
		var wv: PackedVector3Array = w[Mesh.ARRAY_VERTEX]
		var gv: PackedVector3Array = g[Mesh.ARRAY_VERTEX]
		var bv: PackedVector3Array = b[Mesh.ARRAY_VERTEX]
		var buv2: PackedVector2Array = b[Mesh.ARRAY_TEX_UV2]
		var n: int = int(gen.p["segments"])
		# 1) Agua ate o circulo real, nada fora dele, superficie plana.
		_begin("geometria: agua visivel ate o circulo real e so dentro dele")
		var max_out := 0.0
		var flat_ok := true
		for v in wv:
			max_out = maxf(max_out, Vector2(v.x, v.z).distance_to(c) - r)
			flat_ok = flat_ok and absf(v.y - wy) < 1e-5
		var rim_err := 0.0
		for i in n:
			rim_err = maxf(rim_err, absf(Vector2(wv[i].x, wv[i].z).distance_to(c) - r))
		_check(max_out <= EPS_R, "%s: nenhum vertice de agua fora do circulo (max %.4f m)" % [label, max_out])
		_check(rim_err <= EPS_R, "%s: anel da borda sobre o circulo (erro %.4f m)" % [label, rim_err])
		_check(flat_ok, "%s: superficie plana no nivel d'agua" % label)
		var sagitta := r * (1.0 - cos(PI / float(n)))
		_check(sagitta <= 0.006, "%s: corda do poligono no maximo %.4f m dentro do circulo" % [label, sagitta])
		# 2) Chao: sem chao dentro do circulo; cobertura complementar sem buraco nem sobreposicao.
		_begin("geometria: chao do jogo so fora do circulo, cobertura sem buraco nem sobreposicao")
		var gi: PackedInt32Array = g[Mesh.ARRAY_INDEX]
		var ground_area := 0.0
		var inside_bad := 0
		var top_up := true
		for t in range(0, gi.size(), 3):
			var a := gv[gi[t]]
			var bb := gv[gi[t + 1]]
			var cc := gv[gi[t + 2]]
			if absf(a.y - LakeC.GROUND_Y) > 1e-6 or absf(bb.y - LakeC.GROUND_Y) > 1e-6 or absf(cc.y - LakeC.GROUND_Y) > 1e-6:
				continue  # laterais
			ground_area += _tri_area(a, bb, cc)
			var cen := Vector2((a.x + bb.x + cc.x) / 3.0, (a.z + bb.z + cc.z) / 3.0)
			if cen.distance_to(c) < r - sagitta - EPS_R:
				inside_bad += 1
			top_up = top_up and (cc - a).cross(bb - a).y > 0.0
		var wi: PackedInt32Array = w[Mesh.ARRAY_INDEX]
		var water_area := 0.0
		var water_up := true
		for t in range(0, wi.size(), 3):
			var a := wv[wi[t]]
			var bb := wv[wi[t + 1]]
			var cc := wv[wi[t + 2]]
			water_area += _tri_area(a, bb, cc)
			water_up = water_up and (cc - a).cross(bb - a).y > 0.0
		var map_area := 4.0 * LakeC.MAP_HALF * LakeC.MAP_HALF
		_check(inside_bad == 0, "%s: nenhum triangulo do chao dentro do circulo (%d)" % [label, inside_bad])
		_check(absf(ground_area + water_area - map_area) < 0.05, "%s: chao + agua = mapa (%.4f m2 de diferenca)" % [label, ground_area + water_area - map_area])
		_check(absf(water_area - PI * r * r) < PI * r * r * 0.001, "%s: area da agua = circulo (%.2f de %.2f m2)" % [label, water_area, PI * r * r])
		_check(top_up and water_up, "%s: faces do chao e da agua voltadas para cima" % label)
		# 3) Fundo submerso (sem terra seca dentro do circulo) e acima da saia do mapa.
		_begin("geometria: fundo sempre submerso dentro do circulo, barranco no circulo")
		var dry := 0
		var below_skirt := 0
		var wall_off := 0.0
		var wall_top_ok := true
		var bed_out := 0.0
		for i in bv.size():
			var v := bv[i]
			var d := Vector2(v.x, v.z).distance_to(c)
			if buv2[i].y > 0.5:
				wall_off = maxf(wall_off, absf(d - r))
				if v.y > wy:
					wall_top_ok = wall_top_ok and absf(v.y - LakeC.GROUND_Y) < 1e-5
				continue
			bed_out = maxf(bed_out, d - r)
			if v.y > wy - 0.03 + 1e-4:
				dry += 1
			if v.y < LakeC.SKIRT_TOP_Y + 0.05:
				below_skirt += 1
		_check(dry == 0, "%s: nenhum ponto do fundo a menos de 3 cm da superficie (%d)" % [label, dry])
		_check(below_skirt == 0, "%s: fundo acima da saia do mapa (%d abaixo)" % [label, below_skirt])
		_check(bed_out <= EPS_R, "%s: fundo dentro do circulo (max %.4f m fora)" % [label, bed_out])
		_check(wall_off <= EPS_R and wall_top_ok, "%s: barranco vertical sobre o circulo, topo no chao (erro %.4f m)" % [label, wall_off])
		# 4) Rebaixo so dentro do circulo.
		_begin("geometria: rebaixo so sob a agua (nada abaixo do chao fora do circulo)")
		var low_out := 0
		for arr in [gv, bv, wv]:
			for v in arr:
				if v.y < LakeC.GROUND_Y - 1e-4 and Vector2(v.x, v.z).distance_to(c) > r + EPS_R:
					if absf(absf(v.x) - LakeC.MAP_HALF) < 1e-3 or absf(absf(v.z) - LakeC.MAP_HALF) < 1e-3:
						continue  # laterais do chao na borda do mapa (iguais as da caixa do jogo)
					low_out += 1
		_check(low_out == 0, "%s: %d vertices rebaixados fora do circulo" % [label, low_out])
		# 5) Um so corpo d'agua (sem rio): agua so no disco, um anel de borda.
		_begin("geometria: um so corpo d'agua, sem rio")
		_check(wv.size() == n * (wv.size() / n) + 1 and max_out <= EPS_R, "%s: disco unico (%d vertices, %d por anel)" % [label, wv.size(), n])
		# 6) Determinismo e janela dentro do mapa.
		_begin("geometria: deterministica e dentro do mapa")
		var same := true
		for k in ["ground", "bed", "water"]:
			var a1 := _arrays(m[k])
			var a2 := _arrays(m2[k])
			same = same and (a1[Mesh.ARRAY_VERTEX] as PackedVector3Array) == (a2[Mesh.ARRAY_VERTEX] as PackedVector3Array)
			same = same and (a1[Mesh.ARRAY_INDEX] as PackedInt32Array) == (a2[Mesh.ARRAY_INDEX] as PackedInt32Array)
		_check(same, "%s: duas geracoes identicas" % label)
		var s: float = r + float(gen.p["window_pad_m"])
		_check(absf(c.x) + s < LakeC.MAP_HALF and absf(c.y) + s < LakeC.MAP_HALF, "%s: janela do chao dentro do mapa" % label)
		table.append({"lake": label, "radius_m": r, "stats": m["stats"],
			"vertices": {"ground": gv.size(), "bed": bv.size(), "water": wv.size()},
			"triangles": {"ground": gi.size() / 3, "bed": (b[Mesh.ARRAY_INDEX] as PackedInt32Array).size() / 3, "water": wi.size() / 3}})
	_report["geometry"] = table


# ------------------------------------------------------------------------------------- cenas
func _owned_by_v1(node: Node, root: Node) -> bool:
	var n := node
	while n != null and n != root:
		if n.scene_file_path.begins_with("res://Assets/V1/"):
			return true
		n = n.get_parent()
	return false


func _scan(node: Node, root: Node, bad: Array, lights: Array) -> void:
	if node.get_script() != null:
		bad.append("script em " + str(root.get_path_to(node)))
	if node is CollisionObject3D or node is CollisionShape3D or node is PhysicsBody3D or node is NavigationRegion3D:
		bad.append("fisica em " + str(root.get_path_to(node)))
	if node is Marker3D and not _owned_by_v1(node, root):
		bad.append("marcador proprio em " + str(root.get_path_to(node)))
	if node is Light3D:
		lights.append(str(node.name))
	for ch in node.get_children():
		_scan(ch, root, bad, lights)


func _scenes(data: Dictionary, generated: String) -> void:
	var by_seed := {}
	for c in data["cases"]:
		by_seed[int(c["seed"])] = c
	var list := [[ROOT + "/Preview/C/ws_sample_c_s42.tscn", 42, "c"], [ROOT + "/Preview/C/ws_c_before_s42.tscn", 42, "before"]]
	for c in data["cases"]:
		var sd := int(c["seed"])
		if sd != 42:
			list.append([generated + "/Preview/C/ws_sample_c_s%d.tscn" % sd, sd, "c"])
			list.append([generated + "/Preview/C/ws_c_before_s%d.tscn" % sd, sd, "before"])
	var checked := []
	for item in list:
		var path: String = item[0]
		if not ResourceLoader.exists(path):
			_begin("cenas: todos os casos declarados precisam existir")
			_check(false, "cena presente: %s" % path)
			continue
		var seed: int = item[1]
		var variant: String = item[2]
		var scene := (load(path) as PackedScene).instantiate() as Node3D
		checked.append(path)
		_begin("cenas: sem script, fisica ou marcador proprio; so sol + 2 postes")
		var bad := []
		var lights := []
		_scan(scene, scene, bad, lights)
		_check(bad.is_empty(), "%s: %s" % [path.get_file(), bad])
		_check(lights.size() == 3, "%s: luzes %s" % [path.get_file(), lights])
		_begin("cenas: metadados e dados reais do lago")
		var c: Dictionary = by_seed[seed]
		var centre := Vector2(c["lake_centre"][0], c["lake_centre"][1])
		_check(int(scene.get_meta("seed", -1)) == seed, "%s: seed %d" % [path.get_file(), seed])
		_check((scene.get_meta("lake_centre_xz", Vector2.ZERO) as Vector2).distance_to(centre) < 1e-3
			and absf(float(scene.get_meta("lake_radius_m", 0.0)) - float(c["lake_radius"])) < 1e-4, "%s: centro/raio do caso real" % path.get_file())
		_check(bool(scene.get_meta("not_integrated", false)) and String(scene.get_meta("study", "")).find("PENDENTE") >= 0, "%s: marcada como nao integrada e PENDENTE" % path.get_file())
		var limits := scene.get_node_or_null("LimitExamples") as Node3D
		_check(limits != null and not limits.visible, "%s: exemplos de limite existem e ficam ocultos" % path.get_file())
		if variant == "c":
			_begin("cenas: C usa o chao/grama reais, fundo e agua da amostra, sem o disco")
			var ground := scene.get_node("World/GroundWithLakeOpening") as MeshInstance3D
			var bed := scene.get_node("World/LakeBed") as MeshInstance3D
			var water := scene.get_node("World/LakeWater") as MeshInstance3D
			_check(ground.material_override.resource_path == "res://Assets/RecoveryEnvironment/Materials/env_grass_soft.tres", "%s: chao com env_grass_soft real (sem espelho)" % path.get_file())
			_check(bed.material_override.resource_path == ROOT + "/Materials/ws_c_lakebed.tres", "%s: fundo com ws_c_lakebed" % path.get_file())
			_check(water.material_override.resource_path == ROOT + "/Materials/ws_c_water.tres" and water.cast_shadow == GeometryInstance3D.SHADOW_CASTING_SETTING_OFF, "%s: agua com ws_c_water, sem sombra projetada" % path.get_file())
			_check(scene.get_node_or_null("World/LakeSurface") == null and scene.get_node_or_null("World/Ground") == null, "%s: sem o disco e sem a caixa do chao" % path.get_file())
			_begin("cenas: malha salva = malha regenerada pelo centro/raio reais")
			var fresh: Dictionary = LakeC.new(scene.get_meta("proposal_params", {})).build(centre, float(c["lake_radius"]))
			for pair in [["ground", ground], ["bed", bed], ["water", water]]:
				var saved := ((pair[1] as MeshInstance3D).mesh as ArrayMesh).surface_get_arrays(0)
				var now := (fresh[pair[0]] as ArrayMesh).surface_get_arrays(0)
				_check((saved[Mesh.ARRAY_VERTEX] as PackedVector3Array) == (now[Mesh.ARRAY_VERTEX] as PackedVector3Array)
					and (saved[Mesh.ARRAY_INDEX] as PackedInt32Array) == (now[Mesh.ARRAY_INDEX] as PackedInt32Array), "%s: %s identica" % [path.get_file(), pair[0]])
		scene.free()
	_report["scenes_checked"] = checked


# ------------------------------------------------------------------------------- preservacao
func _walk(dir: String, stray: Array) -> void:
	var da := DirAccess.open(dir)
	if da == null:
		return
	for f in da.get_files():
		if f.get_extension().to_lower() in ["png", "jpg", "jpeg", "webp", "svg", "exr", "hdr"]:
			stray.append(dir.path_join(f))
	for d in da.get_directories():
		if d != "Captures":
			_walk(dir.path_join(d), stray)


func _preservation(manifest: String) -> void:
	_begin("preservacao: A/B intactos, sem imagem importavel fora de Captures")
	var stray := []
	_walk(ProjectSettings.globalize_path(ROOT), stray)
	_check(stray.is_empty(), "sem imagens importaveis fora de Preview/Captures: %s" % [stray])
	_check(FileAccess.file_exists(ROOT + "/Preview/Captures/.gdignore"), "Preview/Captures com .gdignore")
	if manifest == "":
		_report["ab_manifest"] = "nao conferido (sem --ab-manifest)"
		return
	var m: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(manifest))
	var compared := 0
	for rel in m:
		if rel == "README.md":
			continue  # o README e atualizado de proposito (secao da amostra C)
		var path: String = ROOT + "/" + str(rel)
		_check(FileAccess.file_exists(path) and FileAccess.get_sha256(path) == m[rel], "igual ao HEAD: %s" % rel)
		compared += 1
	_report["ab_manifest"] = {"file": manifest, "compared": compared}


func _readme() -> void:
	_begin("README: estado PENDENTE, C nao integrada e recomendacao pedida")
	var t := FileAccess.get_file_as_string(ROOT + "/README.md")
	for needle in ["Revisão humana: PENDENTE", "NÃO APROVADA PARA GAMEPLAY", "decisão de geometria", "Amostra C",
			"não integrada", "A e B não estão prontas", "vitrine", "LakeCentre", "LakeRadius", "condições válidas"]:
		_check(t.find(needle) >= 0, "README contem \"%s\"" % needle)
