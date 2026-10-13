## Receita original: rua urbana 1+1 (reta, esquina em L, poste) — amostra V1, revisao PENDENTE.
##
## SOMENTE APRESENTACAO. Nao define largura de gameplay, capacidade, custo, vagas,
## travessias nem posicionamento/espacamento de postes: tudo isso e calibracao/integracao.
##
## Tecnica: UMA secao transversal (perfil) e varrida ao longo de um caminho (reta ou arco).
## Como reta e esquina usam o mesmo perfil, todos os conectores tem exatamente a mesma
## secao, e pecas consecutivas encaixam sem emenda visivel.
extends RefCounted

const FAMILY_ID := "road.urban_1x1.a"
const MATERIAL_PREFIX := "road_"
## Cores candidatas. Desvios das sugestoes do coordenador (revisao 1, todos reversiveis):
##   road_sidewalk #cac4b8 -> #bdb7ab e road_curb #ddd8cd -> #d2cdc2 (calcada competia com paredes claras);
##   road_lamp_glass_off #e5e3d8 -> cinza de vidro apagado (o quase-branco parecia aceso de dia).
const MATERIALS := {
	"road_asphalt": {"albedo": "#4a4e54", "roughness": 0.95},
	"road_sidewalk": {"albedo": "#bdb7ab", "roughness": 0.92},
	"road_sidewalk_service": {"albedo": "#a49a89", "roughness": 0.95},
	"road_curb": {"albedo": "#d2cdc2", "roughness": 0.9},
	"road_marking_yellow": {"albedo": "#e0b23d", "roughness": 0.8},
	"road_lamp_glass_off": {"albedo": "#8d918f", "roughness": 0.3},
	# Alternativa "ligada": definida mas NAO aplicada. A integracao so pode troca-la quando
	# houver noite efetiva E energia realmente atendida (SPEC, iluminacao publica embutida).
	"road_lamp_glass_on": {"albedo": "#fff1cf", "roughness": 0.35, "emission": "#ffe0a3", "emission_energy": 3.0},
}

const ID_STRAIGHT := "road.urban_1x1.a.straight"
const ID_CORNER := "road.urban_1x1.a.corner_l"
const ID_LAMP := "road.urban_1x1.a.lamp"
const ID_LAMP_GLASS := "road.urban_1x1.a.lamp_glass"

const ASPHALT := "road_asphalt"
const SIDEWALK := "road_sidewalk"
const SERVICE := "road_sidewalk_service"
const CURB := "road_curb"
const MARK := "road_marking_yellow"

# --- Secao transversal CANDIDATA (metros) -------------------------------------
const LANE_WIDTH := 3.25
const LANES := 2
const SIDEWALK_WIDTH := 2.0          # inclui o meio-fio
const CURB_WIDTH := 0.2
const CURB_CHAMFER := 0.04           # chanfro da aresta superior do meio-fio (pega luz); 0 = face vertical integral
# Paginacao da calcada (so visual): meio-fio + faixa de servico = 0,70 m e faixa livre = 1,30 m,
# proporcao de referencia da ABNT NBR 9050 (faixa livre >= 1,20 m; servico ~0,70 m).
# SERVICE_STRIP = false pinta a faixa de servico com road_sidewalk (so os materiais sugeridos).
const SERVICE_STRIP := true
const SERVICE_ZONE := 0.7            # face do meio-fio -> fim da faixa de servico (inclui o meio-fio)
const SERVICE_WIDTH := SERVICE_ZONE - CURB_WIDTH         # 0,50 m pintados em road_sidewalk_service
const ASPHALT_Y := 0.03
const SIDEWALK_Y := 0.18             # meio-fio = 0,15 m acima do asfalto
const MARK_Y := 0.04                 # sinalizacao 0,01 m acima do asfalto
const MARK_WIDTH := 0.2              # levemente exagerada para leitura no zoom da cidade
const DASH_PERIOD := 5.25            # ritmo da linha central tracejada (2 por modulo)
const DASH_LENGTH := 2.0             # traco/periodo = 38 %, mantido tambem no arco
const DASH_MIN := 0.3                # tracos recortados mais curtos que isto nao sao gerados
const HALF_ASPHALT := LANE_WIDTH * LANES * 0.5           # 3,25
const HALF_WIDTH := HALF_ASPHALT + SIDEWALK_WIDTH        # 5,25

# --- Pecas -------------------------------------------------------------------
const MODULE_LENGTH := 10.5          # um modulo = largura total (esquina quadrada)
const ARC_SEGMENTS := 12             # segmentos por arco na esquina
const LAMP_SETBACK := CURB_WIDTH + SERVICE_WIDTH * 0.5   # face do meio-fio -> eixo do poste (0,45)

# --- Poste -------------------------------------------------------------------
const LAMP_POLE_HEIGHT := 6.5
const LAMP_ARM_REACH := 1.6          # eixo do poste -> centro da luminaria (para -Z)


func build(kit) -> void:
	_build_straight(kit, MODULE_LENGTH, ID_STRAIGHT)
	_build_corner(kit)
	_build_lamp(kit)


# =============================================================================
# Caminhos: s = distancia ao longo do eixo da via; d = deslocamento lateral
# (+d = lado +Z na reta / lado externo na esquina); y = altura.
# =============================================================================

static func _line_path(length: float) -> Dictionary:
	return {"kind": "line", "length": length, "step": INF}


static func _arc_path() -> Dictionary:
	var length := HALF_WIDTH * PI * 0.5
	return {
		"kind": "arc",
		"center": Vector3(-HALF_WIDTH, 0.0, -HALF_WIDTH),
		"radius": HALF_WIDTH,
		"length": length,
		"step": length / float(ARC_SEGMENTS),
	}


static func _pt(path: Dictionary, s: float, d: float, y: float) -> Vector3:
	if path["kind"] == "line":
		return Vector3(-float(path["length"]) * 0.5 + s, y, d)
	var rc: float = path["radius"]
	var c: Vector3 = path["center"]
	var th := s / rc
	return Vector3(c.x + (rc + d) * cos(th), y, c.z + (rc + d) * sin(th))


static func _side_dir(path: Dictionary, s: float) -> Vector3:
	if path["kind"] == "line":
		return Vector3(0, 0, 1)
	var th := s / float(path["radius"])
	return Vector3(cos(th), 0, sin(th))


static func _tangent(path: Dictionary, s: float) -> Vector3:
	if path["kind"] == "line":
		return Vector3(1, 0, 0)
	var th := s / float(path["radius"])
	return Vector3(-sin(th), 0, cos(th))


static func _stations(s0: float, s1: float, max_step: float) -> PackedFloat64Array:
	var n := 1
	if is_finite(max_step) and max_step > 0.0:
		n = maxi(1, int(ceil((s1 - s0) / max_step - 1e-6)))
	var out := PackedFloat64Array()
	for i in n + 1:
		out.append(lerpf(s0, s1, float(i) / float(n)))
	return out


## Varre um segmento do perfil (a -> b em (d, y)) entre estacoes consecutivas.
func _sweep(b, path: Dictionary, st: PackedFloat64Array, seg: Dictionary) -> void:
	var a: Vector2 = seg["a"]
	var e: Vector2 = seg["b"]
	var n: Vector2 = seg["n"]
	for i in st.size() - 1:
		var s0 := st[i]
		var s1 := st[i + 1]
		var hint: Vector3 = _side_dir(path, (s0 + s1) * 0.5) * n.x + Vector3.UP * n.y
		b.quad(seg["slot"], _pt(path, s0, a.x, a.y), _pt(path, s0, e.x, e.y),
			_pt(path, s1, e.x, e.y), _pt(path, s1, a.x, a.y), hint)


## Tampa (poligono convexo em (d, y)) na estacao s, voltada para `outward`.
func _cap(b, path: Dictionary, s: float, outward: Vector3, poly: Array, slot: String) -> void:
	var pts := []
	for q in poly:
		pts.append(_pt(path, s, q.x, q.y))
	b.face(slot, pts, outward)


# =============================================================================
# Secao transversal unica (perfil de superficie + tampas dos conectores)
# =============================================================================

static func _service_slot() -> String:
	return SERVICE if SERVICE_STRIP else SIDEWALK


## Segmentos do perfil. Tags: "outer_top" (topo da calcada +d) e "lot" (faces voltadas ao lote).
static func _profile() -> Array:
	var segs := []
	segs.append({"slot": ASPHALT, "a": Vector2(-HALF_ASPHALT, ASPHALT_Y), "b": Vector2(HALF_ASPHALT, ASPHALT_Y), "n": Vector2(0, 1), "tag": ""})
	var curb_in := HALF_ASPHALT
	var curb_out := HALF_ASPHALT + CURB_WIDTH
	for sgn in [-1.0, 1.0]:
		segs.append({"slot": CURB, "a": Vector2(sgn * curb_in, ASPHALT_Y), "b": Vector2(sgn * curb_in, SIDEWALK_Y - CURB_CHAMFER),
			"n": Vector2(-sgn, 0), "tag": ""})
		segs.append({"slot": CURB, "a": Vector2(sgn * curb_in, SIDEWALK_Y - CURB_CHAMFER), "b": Vector2(sgn * (curb_in + CURB_CHAMFER), SIDEWALK_Y),
			"n": Vector2(-sgn, 1).normalized(), "tag": ""})
		segs.append({"slot": CURB, "a": Vector2(sgn * (curb_in + CURB_CHAMFER), SIDEWALK_Y), "b": Vector2(sgn * curb_out, SIDEWALK_Y),
			"n": Vector2(0, 1), "tag": ""})
		var serv_out := curb_out + SERVICE_WIDTH
		segs.append({"slot": _service_slot(), "a": Vector2(sgn * curb_out, SIDEWALK_Y), "b": Vector2(sgn * serv_out, SIDEWALK_Y),
			"n": Vector2(0, 1), "tag": ""})
		segs.append({"slot": SIDEWALK, "a": Vector2(sgn * serv_out, SIDEWALK_Y), "b": Vector2(sgn * HALF_WIDTH, SIDEWALK_Y),
			"n": Vector2(0, 1), "tag": "outer_top" if sgn > 0.0 else ""})
		segs.append({"slot": SIDEWALK, "a": Vector2(sgn * HALF_WIDTH, 0.0), "b": Vector2(sgn * HALF_WIDTH, SIDEWALK_Y),
			"n": Vector2(sgn, 0), "tag": "lot"})
	return segs


## Poligonos convexos da secao no conector (identicos em todas as pecas).
static func _section_caps() -> Array:
	var caps := []
	caps.append({"slot": ASPHALT, "poly": [Vector2(-HALF_ASPHALT, 0), Vector2(HALF_ASPHALT, 0), Vector2(HALF_ASPHALT, ASPHALT_Y), Vector2(-HALF_ASPHALT, ASPHALT_Y)]})
	var ci := HALF_ASPHALT
	var co := HALF_ASPHALT + CURB_WIDTH
	for sgn in [-1.0, 1.0]:
		caps.append({"slot": CURB, "poly": [Vector2(sgn * ci, 0), Vector2(sgn * co, 0), Vector2(sgn * co, SIDEWALK_Y),
			Vector2(sgn * (ci + CURB_CHAMFER), SIDEWALK_Y), Vector2(sgn * ci, SIDEWALK_Y - CURB_CHAMFER)]})
		caps.append({"slot": SIDEWALK, "poly": [Vector2(sgn * co, 0), Vector2(sgn * HALF_WIDTH, 0), Vector2(sgn * HALF_WIDTH, SIDEWALK_Y), Vector2(sgn * co, SIDEWALK_Y)]})
	return caps


func _section_ends(b, path: Dictionary) -> void:
	var L: float = path["length"]
	for cap in _section_caps():
		_cap(b, path, 0.0, -_tangent(path, 0.0), cap["poly"], cap["slot"])
		_cap(b, path, L, _tangent(path, L), cap["poly"], cap["slot"])


## Linha central amarela tracejada, com o traco centrado em cada periodo e a fase ancorada
## no inicio da peca (s = 0).
## - Reta: periodo FIXO de DASH_PERIOD. Em comprimentos multiplos de 5,25 m (o modulo de
##   10,5 m), o padrao e simetrico e retas consecutivas mantem o mesmo ritmo. Outros
##   comprimentos geram um aviso, e o traco que passaria da ponta e recortado.
## - Arco: o comprimento (~8,25 m) nao e multiplo de 5,25. Usa um numero inteiro de periodos,
##   escala o traco na mesma proporcao traco/periodo (38 %) e afina o ritmo de forma gradual.
func _center_dashes(b, path: Dictionary) -> void:
	var L: float = path["length"]
	var period := DASH_PERIOD
	if path["kind"] == "line":
		var rest := fposmod(L, DASH_PERIOD)
		if rest > 1e-3 and DASH_PERIOD - rest > 1e-3:
			push_warning("road_urban_1x1: reta de %.3f m nao e multipla de %.2f m; o tracejado perde o ritmo na junta." % [L, DASH_PERIOD])
	else:
		period = L / float(maxi(1, roundi(L / DASH_PERIOD)))
	var dl := period * DASH_LENGTH / DASH_PERIOD
	var hw := MARK_WIDTH * 0.5
	var prof := [
		{"slot": MARK, "a": Vector2(-hw, MARK_Y), "b": Vector2(hw, MARK_Y), "n": Vector2(0, 1)},
		{"slot": MARK, "a": Vector2(-hw, ASPHALT_Y), "b": Vector2(-hw, MARK_Y), "n": Vector2(-1, 0)},
		{"slot": MARK, "a": Vector2(hw, ASPHALT_Y), "b": Vector2(hw, MARK_Y), "n": Vector2(1, 0)},
	]
	var cap := [Vector2(-hw, ASPHALT_Y), Vector2(hw, ASPHALT_Y), Vector2(hw, MARK_Y), Vector2(-hw, MARK_Y)]
	var k := 0
	while true:
		var c := (float(k) + 0.5) * period
		k += 1
		var sa := c - dl * 0.5
		if sa > L - DASH_MIN:
			break
		var sb := minf(c + dl * 0.5, L)
		if sb - sa < DASH_MIN:
			continue
		var st := _stations(sa, sb, path["step"])
		for seg in prof:
			_sweep(b, path, st, seg)
		_cap(b, path, sa, -_tangent(path, sa), cap, MARK)
		_cap(b, path, sb, _tangent(path, sb), cap, MARK)


# =============================================================================
# Reta (comprimento parametrizado; so o modulo de 10,5 m e produzido agora)
# =============================================================================

func _straight_mesh(kit, length: float, file_id: String):
	var b = kit.builder()
	var path := _line_path(length)
	var st := _stations(0.0, length, path["step"])
	for seg in _profile():
		_sweep(b, path, st, seg)
	_section_ends(b, path)
	_center_dashes(b, path)
	return kit.save_mesh(b, file_id)


func _build_straight(kit, length: float, file_id: String) -> void:
	var mesh = _straight_mesh(kit, length, file_id)
	var root = kit.new_root("RoadUrban1x1Straight", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var hl := length * 0.5
	kit.add_marker(root, "Connector_West", Vector3(-hl, 0, 0), Vector3(-1, 0, 0),
		"conector candidato: secao 1+1 identica em todas as pecas; nao define rede viaria")
	kit.add_marker(root, "Connector_East", Vector3(hl, 0, 0), Vector3(1, 0, 0),
		"conector candidato: secao 1+1 identica em todas as pecas; nao define rede viaria")
	kit.add_marker(root, "Candidate_LampSlot", Vector3(0, SIDEWALK_Y, HALF_ASPHALT + LAMP_SETBACK), Vector3(0, 0, -1),
		"posicao candidata de poste (calcada +Z, centro da faixa de servico); espacamento real e calibracao")
	kit.save_scene(root, file_id, {
		"family_id": FAMILY_ID,
		"piece": "straight",
		"title_pt": "Rua urbana 1+1 - reta (modulo)",
		"proposed": _proposed_section({
			"comprimento_modulo_m": length,
			"eixo": "X (x de -%.3f a +%.3f)" % [hl, hl],
		}),
		"parts": {
			"Body": _body_parts_text(),
		},
		"notes": [
			"Pivo no centro do modulo, no solo (y = 0). Nada abaixo de y = 0; faces -y omitidas.",
			"Sem faixa de pedestres, setas, texto, vagas ou carros: travessias e vagas sao geradas pela integracao/simulacao.",
			"Comprimento gerado por funcao parametrizada (_straight_mesh), com tracejado de periodo fixo 5,25 m ancorado na ponta oeste: retas de comprimento multiplo de 5,25 m mantem o ritmo; outros comprimentos geram aviso no build.",
			"Slot de poste em x = 0 vale para o modulo de 10,5 m; posicao e quantidade de slots em outros comprimentos ficam para a integracao.",
			"Paginacao da calcada (so visual): meio-fio 0,20 + faixa road_sidewalk_service 0,50 = 0,70 m; faixa livre 1,30 m. Nao e canteiro nem define ocupacao; SERVICE_STRIP = false a remove.",
			"Slot de poste so no lado +Z (iluminacao unilateral tipica de via 1+1); girar a peca 180 graus inverte o lado.",
			"Poste NAO embutido na cena: Candidate_LampSlot indica uma posicao candidata; a integracao instancia road.urban_1x1.a.lamp.",
		],
	})


# =============================================================================
# Esquina em L (liga a borda -X e a borda -Z)
# =============================================================================

func _build_corner(kit) -> void:
	var b = kit.builder()
	var path := _arc_path()
	var L: float = path["length"]
	var st := _stations(0.0, L, path["step"])
	for seg in _profile():
		if seg["tag"] == "outer_top" or seg["tag"] == "lot":
			continue
		_sweep(b, path, st, seg)
	# Calcada externa: todo o restante do quadrado alem do meio-fio curvo.
	var poly := PackedVector2Array()
	for s in st:
		var p := _pt(path, s, HALF_ASPHALT + CURB_WIDTH + SERVICE_WIDTH, SIDEWALK_Y)
		poly.append(Vector2(p.x, p.z))
	poly.append(Vector2(-HALF_WIDTH, HALF_WIDTH))
	poly.append(Vector2(HALF_WIDTH, HALF_WIDTH))
	poly.append(Vector2(HALF_WIDTH, -HALF_WIDTH))
	b.extrude(SIDEWALK, poly, SIDEWALK_Y, SIDEWALK_Y, true, false, false)
	# Faces voltadas aos lotes (+X e +Z).
	var hw := HALF_WIDTH
	b.quad(SIDEWALK, Vector3(hw, 0, -hw), Vector3(hw, 0, hw), Vector3(hw, SIDEWALK_Y, hw), Vector3(hw, SIDEWALK_Y, -hw), Vector3(1, 0, 0))
	b.quad(SIDEWALK, Vector3(-hw, 0, hw), Vector3(hw, 0, hw), Vector3(hw, SIDEWALK_Y, hw), Vector3(-hw, SIDEWALK_Y, hw), Vector3(0, 0, 1))
	_section_ends(b, path)
	_center_dashes(b, path)
	var mesh = kit.save_mesh(b, ID_CORNER)

	var root = kit.new_root("RoadUrban1x1CornerL", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	kit.add_marker(root, "Connector_West", Vector3(-hw, 0, 0), Vector3(-1, 0, 0),
		"conector candidato: secao 1+1 identica a reta")
	kit.add_marker(root, "Connector_North", Vector3(0, 0, -hw), Vector3(0, 0, -1),
		"conector candidato: secao 1+1 identica a reta")
	var c: Vector3 = path["center"]
	var r_lamp := HALF_WIDTH + HALF_ASPHALT + LAMP_SETBACK
	var dir45 := Vector3(1, 0, 1).normalized()
	kit.add_marker(root, "Candidate_LampSlot", Vector3(c.x, SIDEWALK_Y, c.z) + dir45 * r_lamp, -dir45,
		"posicao candidata de poste (calcada externa, meio do arco), voltada ao centro do arco")
	kit.save_scene(root, ID_CORNER, {
		"family_id": FAMILY_ID,
		"piece": "corner_l",
		"title_pt": "Rua urbana 1+1 - esquina em L",
		"proposed": _proposed_section({
			"quadrado_m": "%.2f x %.2f" % [2.0 * hw, 2.0 * hw],
			"centro_do_arco": "(%.3f, 0, %.3f)" % [c.x, c.z],
			"raio_asfalto_m": "%.2f -> %.2f" % [HALF_WIDTH - HALF_ASPHALT, HALF_WIDTH + HALF_ASPHALT],
			"raio_linha_central_m": HALF_WIDTH,
			"segmentos_por_arco": ARC_SEGMENTS,
		}),
		"parts": {
			"Body": _body_parts_text(),
		},
		"notes": [
			"Pivo no centro do quadrado, no solo. Liga a borda -X (Connector_West) a borda -Z (Connector_North).",
			"Calcada interna = quarto de disco r = 2,0 m; calcada externa = restante do quadrado, com meio-fio curvo.",
			"Tracejado no arco: 2 periodos de ~4,12 m (o arco de r = 5,25 m mede ~8,25 m) com traco proporcional de ~1,57 m; vaos de 2,55 m no arco e 2,90 m na junta com a reta (3,25 m entre retas). Nenhum traco cruza junta.",
			"Para outras orientacoes, girar a cena em multiplos de 90 graus em torno de Y.",
		],
	})


static func _body_parts_text() -> String:
	var slots := PackedStringArray([ASPHALT, CURB])
	if SERVICE_STRIP:
		slots.append(SERVICE)
	slots.append(SIDEWALK)
	slots.append(MARK)
	return "malha unica; superficies " + ", ".join(slots)


static func _proposed_section(extra: Dictionary) -> Dictionary:
	var d := {
		"faixas": "%d x %.2f m = %.2f m de asfalto" % [LANES, LANE_WIDTH, HALF_ASPHALT * 2.0],
		"calcada_m": SIDEWALK_WIDTH,
		"largura_total_m": HALF_WIDTH * 2.0,
		"meio_fio_largura_m": CURB_WIDTH,
		"meio_fio_altura_m": SIDEWALK_Y - ASPHALT_Y,
		"topo_asfalto_y": ASPHALT_Y,
		"topo_calcada_y": SIDEWALK_Y,
		"linha_central": "amarela tracejada, %.2f m de largura, traco %.1f m, periodo %.2f m na reta; no arco, traco e periodo escalados na mesma proporcao (candidata)" % [MARK_WIDTH, DASH_LENGTH, DASH_PERIOD],
		"faixa_de_servico_m": "%.2f (meio-fio %.2f + faixa %.2f)" % [SERVICE_ZONE, CURB_WIDTH, SERVICE_WIDTH],
		"faixa_livre_m": SIDEWALK_WIDTH - SERVICE_ZONE,
		"recuo_poste_m": LAMP_SETBACK,
		"status": "CANDIDATO - revisao humana PENDENTE",
	}
	d.merge(extra, true)
	return d


# =============================================================================
# Poste (pivo no centro da base da haste; braco projeta para -Z)
# =============================================================================

## Octogono em planta (retangulo de mn a mx com cantos cortados em `cut`), no nivel y.
static func _octagon(mn: Vector2, mx: Vector2, cut: float, y: float) -> Array:
	return [
		Vector3(mn.x + cut, y, mn.y), Vector3(mx.x - cut, y, mn.y),
		Vector3(mx.x, y, mn.y + cut), Vector3(mx.x, y, mx.y - cut),
		Vector3(mx.x - cut, y, mx.y), Vector3(mn.x + cut, y, mx.y),
		Vector3(mn.x, y, mx.y - cut), Vector3(mn.x, y, mn.y + cut),
	]


## Bloco de vidro sem face superior: paredes verticais com arestas verticais chanfradas
## (`cut`) e chanfro `bevel` em toda a borda inferior. Para pecas encaixadas sob outra.
func _lens(b, slot: String, mn: Vector3, mx: Vector3, cut: float, bevel: float) -> void:
	var c := Vector3((mn.x + mx.x) * 0.5, 0.0, (mn.z + mx.z) * 0.5)
	var top := _octagon(Vector2(mn.x, mn.z), Vector2(mx.x, mx.z), cut, mx.y)
	var mid := _octagon(Vector2(mn.x, mn.z), Vector2(mx.x, mx.z), cut, mn.y + bevel)
	# Corte interno que mantem o chanfro diagonal com o mesmo afastamento `bevel`.
	var cut_in := maxf(0.0, cut - (2.0 - sqrt(2.0)) * bevel)
	var bot := _octagon(Vector2(mn.x + bevel, mn.z + bevel), Vector2(mx.x - bevel, mx.z - bevel), cut_in, mn.y)
	for i in 8:
		var j := (i + 1) % 8
		var out: Vector3 = (mid[i] + mid[j]) * 0.5 - Vector3(c.x, mid[i].y, c.z)
		out.y = 0.0
		b.quad(slot, mid[i], mid[j], top[j], top[i], out)
		b.quad(slot, bot[i], bot[j], mid[j], mid[i], out.normalized() + Vector3.DOWN)
	b.face(slot, bot, Vector3.DOWN)

func _build_lamp(kit) -> void:
	var b = kit.builder()
	var reach := LAMP_ARM_REACH
	var top := LAMP_POLE_HEIGHT
	# Base: flange quadrada + colar octogonal.
	b.bevel_box("metal_dark", Vector3(-0.2, 0, -0.2), Vector3(0.2, 0.05, 0.2), 0.02, false)
	b.frustum("metal_dark", Vector3(0, 0.05, 0), 0.18, 0.14, 0.5, 8, true, false, 22.5)
	# Haste afunilada (levemente engrossada para leitura no zoom da cidade).
	b.frustum("metal_dark", Vector3(0, 0.55, 0), 0.11, 0.07, top - 0.55, 8, true, false, 22.5)
	# Braco: nasce do topo da haste (pescoco), sobe e projeta para -Z.
	var hy := top + 0.1                 # altura do eixo do braco junto a luminaria
	b.beam("metal_dark", Vector3(0, top - 0.06, 0.02), Vector3(0, hy - 0.01, -0.5), 0.09)
	b.beam("metal_dark", Vector3(0, hy - 0.02, -0.46), Vector3(0, hy, -(reach - 0.4)), 0.09)
	# Carcaca da luminaria (LED, plana, clara sobre haste escura = silhueta reconhecivel).
	var housing_y0 := hy - 0.05
	var housing_y1 := hy + 0.08
	var housing_bevel := 0.045
	b.bevel_box("metal_light", Vector3(-0.21, housing_y0, -(reach + 0.45)), Vector3(0.21, housing_y1, -(reach - 0.45)), housing_bevel)
	var mesh = kit.save_mesh(b, ID_LAMP)

	# Difusor: bloco de vidro de 0,11 m sob a carcaca, quase do tamanho dela, com laterais
	# verticais visiveis na camera isometrica (faixa continua que "acende" com o material on).
	# Sem face superior (fica dentro da carcaca); as paredes sobem ate dentro dela para nao
	# abrir fresta sob o chanfro.
	var g = kit.builder()
	var gy0 := housing_y0 - 0.11
	_lens(g, "road_lamp_glass_off", Vector3(-0.185, gy0, -(reach + 0.42)), Vector3(0.185, housing_y0 + housing_bevel, -(reach - 0.42)), 0.04, 0.04)
	var gmesh = kit.save_mesh(g, ID_LAMP_GLASS)

	var root = kit.new_root("StreetLamp", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	kit.add_mesh(root, "LampGlass", gmesh)
	kit.add_marker(root, "Candidate_LightSource", Vector3(0, (gy0 + housing_y0) * 0.5, -reach), Vector3(0, -1, 0),
		"origem candidata de luz; a integracao so liga com noite efetiva E energia atendida")
	kit.save_scene(root, ID_LAMP, {
		"family_id": FAMILY_ID,
		"piece": "lamp",
		"title_pt": "Poste urbano (iluminacao publica)",
		"proposed": {
			"altura_haste_m": top,
			"alcance_braco_m": reach,
			"altura_luminaria_m": snappedf(gy0, 0.01),
			"altura_total_m": snappedf(housing_y1, 0.01),
			"carcaca_m": "0,42 x 0,13 x 0,90",
			"difusor_m": "0,37 x 0,11 x 0,84 (aparente abaixo da carcaca)",
			"status": "CANDIDATO - revisao humana PENDENTE",
		},
		"parts": {
			"Body": "haste + braco + carcaca (metal_dark, metal_light)",
			"LampGlass": "difusor separado (road_lamp_glass_off); trocavel por road_lamp_glass_on pela integracao",
		},
		"notes": [
			"Pivo no centro da base da haste (y = 0); braco projeta para -Z: alinhar -Z do poste ao forward do Candidate_LampSlot.",
			"Sem Light3D e sem emissao aplicada: o asset NAO acende sozinho.",
			"road_lamp_glass_on existe em Materials/ apenas para a integracao trocar quando houver estado real.",
		],
	})
