## Receita original: Patio Municipal de Obras (civic.works_depot.a) — amostra V1 (segunda
## onda), revisao humana PENDENTE.
##
## SOMENTE APRESENTACAO: nenhuma regra de gameplay, simulacao, acesso, capacidade ou estoque.
## Geometria 100% autoral a partir das primitivas do kit (sem origem externa).
##
## Composicao (leitura sem texto: "garagem grande + patio com baias de material"):
##   galpao de obras na metade de tras (portoes de veiculo FECHADOS em azul civico voltados
##   a -Z + porta de pessoal; cobertura de duas aguas rasa com lanternim claro) -> escritorio
##   terreo anexo no lado +X (laje em ardosia atras de platibanda com capa branca e faixa azul,
##   porta azul: parentesco discreto com a Prefeitura) -> patio em placas de concreto na frente
##   com abertura livre na borda -Z (sem portao movel), mureta baixa dentro do lote -> quatro
##   baias de material de tres lados em concreto + area da massa asfaltica, VAZIAS no Body.
## Estoque fisico (State_Stock) e ESTADO REAL do host: nos separados, ocultos por padrao,
## um filho por material V1. Nada aqui representa estoque infinito.
## Sem veiculos, pessoas, ferramentas, texto, simbolos ou janelas acesas.
##
## Convencoes: Y para cima, 1 un = 1 m (candidato), fachada/acesso candidato em -Z,
## pivo no solo no CENTRO do lote logico 24 x 22 m (contrato da segunda onda).
extends RefCounted

const FAMILY_ID := "civic.works_depot.a"
const MATERIAL_PREFIX := "depot_"
const MATERIALS := {
	"depot_wall": {"albedo": "#d4b064", "roughness": 0.88},
	"depot_sand": {"albedo": "#c7a46f", "roughness": 0.95},
	"depot_timber": {"albedo": "#b8773f", "roughness": 0.85},
	"depot_pave": {"albedo": "#837e74", "roughness": 0.95},
}

const WALL := "depot_wall"
const ROOF := "civic_roof"
const ACCENT := "civic_accent"
const TRIM := "trim_white"
const GLASS := "glass_dark"
const PAVE := "depot_pave"
const CONC := "concrete"


## So para conferencia local de estados no render de revisao. A versao final SEMPRE false.
const DEBUG_SHOW_STATE := false

# ---------------------------------------------------------------------------
# Medidas CANDIDATAS (metros). Nao aprovam footprint, acesso, capacidade ou custo.
# ---------------------------------------------------------------------------
const LOT := Vector2(24.0, 22.0)
# Icone: recorte da frente vazia do patio e placa de piso espessa (so no icone).
const ICON_Z_FRONT := -3.9
const ICON_BAY_W := 1.95
const ICON_SLAB := 0.35
const ICON_BAY_H := 0.85         # divisores mais baixos no icone: o estoque aparece por cima
const ICON_BAY_BACK_H := 1.3
const HX := 11.95              # meia extensao do pavimento em X (lote 24)
const HZ := 10.95              # meia extensao do pavimento em Z (lote 22)
const SLAB_H := 0.08

# Mureta baixa de perimetro (dentro do lote) com abertura frontal livre.
const MW_T := 0.25
const MW_H := 0.7
const GATE_HALF := 3.6         # abertura de 7,2 m centrada em x = 0
const PILLAR := 0.5
const PILLAR_H := 1.5
# Juntas do piso de concreto do patio (linhas de placa).
const JOINT_W := 0.08
const JOINT_XS := [-6.0, -2.0, 2.0, 6.0]
const JOINT_ZS := [-6.4, -2.2]

# Baias de material (tres lados), abertas para +X, no lado -X do patio.
const BAY_N := 4
const BAY_W := 2.4             # vao interno em Z
const BAY_DIV := 0.25
const BAY_Z0 := -8.45          # face frontal do primeiro divisor
const BAY_MOUTH_X := -9.0      # boca das baias
const BAY_BACK_T := 0.25
const BAY_H := 1.3
const BAY_BACK_H := 1.6
# Area da massa asfaltica (piso elevado) diante da primeira baia.
const ASPH_X0 := -11.7
const ASPH_X1 := -9.2
const ASPH_Z0 := -10.5
const ASPH_Z1 := -8.75
const ASPH_H := 0.12

# Galpao de obras.
const SH_X0 := -8.5
const SH_X1 := 5.5
const SH_Z0 := 2.2
const SH_Z1 := 10.2
const SH_PLINTH := 0.5
const SH_EAVE := 5.2
const SH_RIDGE := 6.0
const ROOF_T := 0.22
const ROOF_OH := 0.5           # beiral frente/fundos
const ROOF_OHX := 0.35         # beiral nas empenas
# Lanternim de cumeeira.
const MONITOR := true
const MON_W := 2.2
const MON_X0 := -5.5
const MON_X1 := 2.5
const MON_TOP := 6.95
const DOOR_W := 4.0
const DOOR_H := 4.3
const DOOR_XS := [-4.8, 1.8]
const PDOOR_X := -1.5
const PDOOR_W := 1.0
const PDOOR_H := 2.2

# Escritorio terreo anexo (lado +X).
const OF_X0 := 5.5
const OF_X1 := 11.3
const OF_Z0 := 1.4
const OF_Z1 := 7.4
const OF_PLINTH := 0.3
const OF_ROOF_Y := 3.6         # laje plana (ardosia) atras da platibanda
const OF_PARAPET := 4.0        # topo da platibanda
const OF_PARAPET_T := 0.22
const OF_BAND_Y0 := 3.3        # faixa azul civica sob a capa
const OF_BAND_Y1 := 3.85
const OF_COPING := 0.12
const OF_DOOR_X := 8.4
const OF_DOOR_W := 1.1
const OF_DOOR_H := 2.3


# ---------------------------------------------------------------------------
# Auxiliares
# ---------------------------------------------------------------------------

static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Referencial local de fachada: parede em z = 0, exterior em -Z local, +X local horizontal.
static func _facade_xform(n: Vector3, origin: Vector3) -> Transform3D:
	var x := Vector3(-n.z, 0.0, n.x)
	return Transform3D(Basis(x, Vector3.UP, -n), origin)


## Janela no referencial de fachada: vidro escuro recuado + moldura/peitoril claros.
static func _window(b, cx: float, y0: float, w: float, h: float, mullion: bool = true) -> void:
	var f := 0.13
	var dep := 0.12
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad(GLASS, Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box(TRIM, Vector3(x0 - f, y1, -dep), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box(TRIM, Vector3(x0 - f, y0, -dep), Vector3(x0, y1, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(x1, y0, -dep), Vector3(x1 + f, y1, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(x0 - f - 0.06, y0 - 0.1, -dep - 0.08), Vector3(x1 + f + 0.06, y0, 0.0), sk)
	if mullion:
		b.box(TRIM, Vector3(cx - 0.035, y0, -0.075), Vector3(cx + 0.035, y1, -0.02), PackedStringArray(["+z", "-y", "+y"]))


## Anel (faixa/cinta) entre dois poligonos XZ correspondentes.
static func _ring(b, slot: String, outer: PackedVector2Array, inner: PackedVector2Array, y0: float, y1: float) -> void:
	var n := outer.size()
	var cen := Vector2.ZERO
	for q in outer:
		cen += q
	cen /= float(n)
	for i in n:
		var j := (i + 1) % n
		var o0 := outer[i]; var o1 := outer[j]
		var i0 := inner[i]; var i1 := inner[j]
		var mid := (o0 + o1) * 0.5 - cen
		b.quad(slot, Vector3(o0.x, y0, o0.y), Vector3(o1.x, y0, o1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3(mid.x, 0, mid.y))
		b.quad(slot, Vector3(i0.x, y1, i0.y), Vector3(i1.x, y1, i1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3.UP)


## Placa espessa: face superior (pts, quatro cantos) + faces laterais com espessura vertical `t`.
## `skip_edges` lista arestas (i -> i+1) sem face lateral. Sem face inferior.
static func _thick_quad(b, top_slot: String, side_slot: String, pts: Array, t: float, skip_edges: Array = []) -> void:
	b.face(top_slot, pts, Vector3.UP)
	var cen := Vector3.ZERO
	for p in pts:
		cen += p
	cen /= float(pts.size())
	var down := Vector3(0, -t, 0)
	for i in pts.size():
		if skip_edges.has(i):
			continue
		var p0: Vector3 = pts[i]
		var p1: Vector3 = pts[(i + 1) % pts.size()]
		var mid := (p0 + p1) * 0.5
		var out := Vector3(mid.x - cen.x, 0.0, mid.z - cen.z)
		b.face(side_slot, [p0, p1, p1 + down, p0 + down], out)


## Monte facetado (areia/brita, massa asfaltica): anel de base, anel intermediario e cume,
## com variacao deterministica. Sem base (apoia no piso).
static func _heap(b, slot: String, c: Vector3, rx: float, rz: float, h: float, sides: int, rng_seed: int) -> void:
	# Perfil de monte de material solto (angulo de repouso): base larga, ombro baixo,
	# topo arredondado. Aneis alternados (meio passo) evitam o aspecto de "pedra".
	var rng := RandomNumberGenerator.new()
	rng.seed = rng_seed
	var prof := [[1.0, 0.0], [0.72, 0.42], [0.38, 0.8]]
	var rings := []
	for r in prof.size():
		var ring := []
		for i in sides:
			var a := TAU * (float(i) + 0.5 * float(r % 2)) / float(sides) + 0.35
			var k: float = prof[r][0] * (1.0 + rng.randf_range(-0.06, 0.06))
			var y: float = h * prof[r][1] * (1.0 + (rng.randf_range(-0.05, 0.05) if r > 0 else 0.0))
			ring.append(c + Vector3(cos(a) * rx * k, y, sin(a) * rz * k))
		rings.append(ring)
	var top := c + Vector3(rx * 0.04, h, -rz * 0.04)
	var core := c + Vector3(0, h * 0.2, 0)
	for r in rings.size() - 1:
		var lo: Array = rings[r]
		var hi: Array = rings[r + 1]
		for i in sides:
			var j := (i + 1) % sides
			# Vertice do anel superior entre lo[i] e lo[j] (meio passo a frente quando r e par;
			# quando r e impar o anel inferior e que esta deslocado, entao e o vertice j).
			var hi_a: Vector3 = hi[i] if r % 2 == 0 else hi[j]
			var hi_b: Vector3 = hi[j] if r % 2 == 0 else hi[(j + 1) % sides]
			for t in [[lo[i], lo[j], hi_a], [lo[j], hi_b, hi_a]]:
				var cc: Vector3 = (t[0] + t[1] + t[2]) / 3.0
				b.face(slot, t, cc - core)
	var last: Array = rings[rings.size() - 1]
	for i in sides:
		var j := (i + 1) % sides
		var t := [last[i], last[j], top]
		var cc: Vector3 = (t[0] + t[1] + t[2]) / 3.0
		b.face(slot, t, cc - core)


# ---------------------------------------------------------------------------
# Construcao
# ---------------------------------------------------------------------------

func build(kit) -> void:
	var b = kit.builder()
	var parts_tris := {}
	var t0: int = b.triangle_count()
	_yard(b)
	parts_tris["patio_mureta"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_bays(b)
	parts_tris["baias_area_asfalto"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_shed(b)
	parts_tris["galpao"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_office(b)
	parts_tris["escritorio"] = b.triangle_count() - t0
	var body_tris: int = b.triangle_count()
	var mesh = kit.save_mesh(b, FAMILY_ID)

	# Estoque fisico (estado real do host): uma malha por material V1.
	var stock_defs := [
		["SandGravel", "stock_sand_gravel", Callable(self, "_stock_sand")],
		["Concrete", "stock_concrete", Callable(self, "_stock_concrete")],
		["Steel", "stock_steel", Callable(self, "_stock_steel")],
		["Wood", "stock_wood", Callable(self, "_stock_wood")],
		["Asphalt", "stock_asphalt", Callable(self, "_stock_asphalt")],
	]
	var stock_meshes := {}
	var stock_tris := {}
	for d in stock_defs:
		var sb = kit.builder()
		(d[2] as Callable).call(sb)
		stock_tris[d[0]] = sb.triangle_count()
		stock_meshes[d[0]] = kit.save_mesh(sb, FAMILY_ID + "." + d[1])
	print("[works_depot] Body tris = ", body_tris, " | por parte = ", parts_tris, " | estoque = ", stock_tris)

	var root = kit.new_root("WorksDepot", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var state = kit.add_node(root, "State_Stock")
	state.visible = DEBUG_SHOW_STATE
	state.set_meta("note", "estoque fisico REAL do patio; o host liga State_Stock e cada filho conforme o estoque de cada material (nada aqui e estoque infinito)")
	for d in stock_defs:
		kit.add_mesh(state, d[0], stock_meshes[d[0]])

	var bay_c := _bay_centers()
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(OF_DOOR_X, 0.0, OF_Z0 - 0.75 - 0.4), Vector3.FORWARD,
		"CANDIDATO: diante da porta do escritorio (degrau incluido), frente -Z; acesso real e da integracao")
	kit.add_marker(root, "Candidate_VehicleGate", Vector3(0.0, 0.0, -HZ + MW_T * 0.5), Vector3.FORWARD,
		"CANDIDATO: centro da abertura do patio na borda -Z (sem portao movel); acesso real e da integracao")
	var cargo := Vector3(-6.9, 0.0, bay_c[1])
	var to_gate := Vector3(0.0, 0.0, -HZ) - cargo
	kit.add_marker(root, "Candidate_CargoPoint", cargo, to_gate.normalized(),
		"CANDIDATO: no patio junto as baias, onde um caminhao descarregaria; frente apontando para a abertura")

	var bay_depth := BAY_MOUTH_X - (-HX + BAY_BACK_T)
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Patio Municipal de Obras",
		"lot": LOT,
		"proposed": {
			"lote_x_m": LOT.x,
			"lote_z_m": LOT.y,
			"pavimento_x_m": HX * 2.0,
			"pavimento_z_m": HZ * 2.0,
			"galpao_x_m": SH_X1 - SH_X0,
			"galpao_z_m": SH_Z1 - SH_Z0,
			"galpao_beiral_m": SH_EAVE,
			"galpao_cumeeira_m": snappedf(SH_RIDGE + ROOF_T, 0.01),
			"galpao_topo_lanternim_m": snappedf(MON_TOP + 0.12 + MON_W * 0.5 * (SH_RIDGE - SH_EAVE) / ((SH_Z1 - SH_Z0) * 0.5), 0.01),
			"portoes_veiculo_l_x_a_m": [DOOR_W, DOOR_H],
			"portoes_veiculo_qtd": DOOR_XS.size(),
			"porta_pessoal_galpao_l_x_a_m": [PDOOR_W, PDOOR_H],
			"escritorio_x_m": snappedf(OF_X1 - OF_X0, 0.01),
			"escritorio_z_m": snappedf(OF_Z1 - OF_Z0, 0.01),
			"escritorio_topo_platibanda_m": snappedf(OF_PARAPET + OF_COPING, 0.01),
			"porta_escritorio_l_x_a_m": [OF_DOOR_W, OF_DOOR_H],
			"baias_qtd": BAY_N,
			"baia_vao_z_m": BAY_W,
			"baia_profundidade_x_m": snappedf(bay_depth, 0.01),
			"baia_altura_divisor_m": BAY_H,
			"baia_altura_fundo_m": BAY_BACK_H,
			"mureta_altura_m": MW_H,
			"abertura_patio_m": GATE_HALF * 2.0,
			"pilar_abertura_altura_m": PILLAR_H,
		},
		"parts": {
			"Body": "malha unica estatica; 7 superficies (slot = material): depot_pave piso de concreto do patio, concrete mureta/pilares/baias/area do asfalto/embasamentos/degrau/juntas do piso, depot_wall paredes do galpao e do escritorio, civic_roof coberturas, trim_white lanternim/bordas da cobertura/cornija/molduras/marquise, glass_dark vidros, civic_accent portoes e portas (fechados)",
			"State_Stock": "Node3D OCULTO por padrao: estoque fisico REAL; o host liga o no e cada filho conforme o estoque de cada material",
			"State_Stock/SandGravel": "monte de areia/brita na 1a baia (frente)",
			"State_Stock/Concrete": "blocos pre-moldados (concrete_dark) empilhados em dois estrados de madeira na 2a baia",
			"State_Stock/Steel": "feixe de barras sobre dois calcos na 4a baia (junto ao galpao)",
			"State_Stock/Wood": "pilha de tabuas/caibros com separadores na 3a baia",
			"State_Stock/Asphalt": "monte escuro pequeno de massa asfaltica na area elevada diante da 1a baia",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta do escritorio, forward -Z",
			"Candidate_VehicleGate": "Marker3D candidato no centro da abertura do patio (z ~ -10,8), forward -Z",
			"Candidate_CargoPoint": "Marker3D candidato no patio junto as baias, forward para a abertura",
		},
		"notes": [
			"Pivo no solo, no CENTRO do lote logico 24 x 22 m; pavimento ocupa %.1f x %.1f m (escala do host ~1)." % [HX * 2.0, HZ * 2.0],
			"Portoes, portas e baias sao desenho; acessos/carga reais sao so os Marker3D candidatos, a confirmar na integracao.",
			"Baias VAZIAS no Body. O estoque (State_Stock) so aparece quando o host liga a partir de estoque REAL; cada filho e pequeno (menor que um Centro de Materiais). Thumbnails/pranchas podem ligar State_Stock so para ilustrar.",
			"Juntas do piso (faixas claras ~4 m) indicam piso de concreto; nao sao vagas, faixas de trafego nem acesso funcional.",
			"Icone (draw_icon): layout simplificado proprio (patio mais raso, 3 baias com estoque mais alto, divisores baixos) para leitura em 64 px; nao e medida do modelo.",
			"Identidade civica discreta: civic_roof (ardosia) e civic_accent (azul) reutilizados da Prefeitura; sem bandeira, brasao, texto ou simbolo.",
			"Triangulos (build): Body %d; por parte %s; estoque por material %s." % [body_tris, str(parts_tris), str(stock_tris)],
		],
	})


func _bay_centers() -> Array:
	var out := []
	for i in BAY_N:
		var za := BAY_Z0 + BAY_DIV + float(i) * (BAY_W + BAY_DIV)
		out.append(za + BAY_W * 0.5)
	return out


func _bay_center_x() -> float:
	return (BAY_MOUTH_X + (-HX + BAY_BACK_T)) * 0.5


# ---------------------------------------------------------------------------
# Patio, mureta e baias
# ---------------------------------------------------------------------------

func _yard(b) -> void:
	b.box(PAVE, Vector3(-HX, 0.0, -HZ), Vector3(HX, SLAB_H, HZ), PackedStringArray(["-y"]))
	_joints(b)
	# Mureta frontal em dois trechos + pilares marcando a abertura livre.
	var fz0 := -HZ
	var fz1 := -HZ + MW_T
	b.bevel_box(CONC, Vector3(-HX, 0.0, fz0), Vector3(-GATE_HALF - PILLAR, MW_H, fz1), 0.05, false)
	b.bevel_box(CONC, Vector3(GATE_HALF + PILLAR, 0.0, fz0), Vector3(HX, MW_H, fz1), 0.05, false)
	for s in [-1.0, 1.0]:
		var xa: float = -GATE_HALF - PILLAR if s < 0.0 else GATE_HALF
		var xb: float = -GATE_HALF if s < 0.0 else GATE_HALF + PILLAR
		b.bevel_box(CONC, Vector3(xa, 0.0, fz0 + 0.06), Vector3(xb, PILLAR_H, fz0 + 0.06 + PILLAR), 0.06, false)
		b.bevel_box(TRIM, Vector3(xa - 0.06, PILLAR_H, fz0), Vector3(xb + 0.06, PILLAR_H + 0.12, fz0 + PILLAR + 0.12), 0.03, false)
	# Laterais e fundos.
	var bay_z1 := BAY_Z0 + float(BAY_N) * (BAY_W + BAY_DIV) + BAY_DIV
	b.bevel_box(CONC, Vector3(-HX, 0.0, fz1), Vector3(-HX + MW_T, MW_H, BAY_Z0), 0.05, false)
	b.bevel_box(CONC, Vector3(-HX, 0.0, bay_z1), Vector3(-HX + MW_T, MW_H, HZ - MW_T), 0.05, false)
	b.bevel_box(CONC, Vector3(HX - MW_T, 0.0, fz1), Vector3(HX, MW_H, HZ - MW_T), 0.05, false)
	b.bevel_box(CONC, Vector3(-HX, 0.0, HZ - MW_T), Vector3(HX, MW_H, HZ), 0.05, false)


## Juntas de dilatacao do piso de concreto (placas ~4 m): diferenciam o patio de um
## estacionamento asfaltado sem pintar vagas. Faixas finas 1 cm acima do piso.
func _joints(b) -> void:
	var y := SLAB_H + 0.01
	var hw := JOINT_W * 0.5
	var z_front := -HZ + MW_T
	for x in JOINT_XS:
		var z_back: float = (OF_Z0 - 0.06) if x > OF_X0 else (SH_Z0 - 0.06)
		b.quad(CONC, Vector3(x - hw, y, z_front), Vector3(x + hw, y, z_front), Vector3(x + hw, y, z_back), Vector3(x - hw, y, z_back), Vector3.UP)
	for z in JOINT_ZS:
		var x0 := BAY_MOUTH_X
		var x1 := HX - MW_T
		b.quad(CONC, Vector3(x0, y, z - hw), Vector3(x1, y, z - hw), Vector3(x1, y, z + hw), Vector3(x0, y, z + hw), Vector3.UP)


func _bays(b) -> void:
	var bx0 := -HX
	var bx1 := -HX + BAY_BACK_T
	var bay_z1 := BAY_Z0 + float(BAY_N) * (BAY_W + BAY_DIV) + BAY_DIV
	# Parede de fundo continua (mais alta) e divisores.
	b.bevel_box(CONC, Vector3(bx0, 0.0, BAY_Z0), Vector3(bx1, BAY_BACK_H, bay_z1), 0.06, false)
	for i in BAY_N + 1:
		var z := BAY_Z0 + float(i) * (BAY_W + BAY_DIV)
		b.bevel_box(CONC, Vector3(bx1 - 0.02, 0.0, z), Vector3(BAY_MOUTH_X, BAY_H, z + BAY_DIV), 0.06, false)
	# Area elevada da massa asfaltica (sem estoque no Body).
	b.bevel_box(CONC, Vector3(ASPH_X0, 0.0, ASPH_Z0), Vector3(ASPH_X1, ASPH_H + SLAB_H, ASPH_Z1), 0.04, false)


# ---------------------------------------------------------------------------
# Galpao
# ---------------------------------------------------------------------------

func _shed(b) -> void:
	var zc := (SH_Z0 + SH_Z1) * 0.5
	# Embasamento claro + paredes (quinas verticais chanfradas).
	b.extrude(CONC, _chamfer_rect(SH_X0 - 0.05, SH_Z0 - 0.05, SH_X1 + 0.05, SH_Z1 + 0.05, 0.14), 0.0, SH_PLINTH, true, false)
	b.extrude(WALL, _chamfer_rect(SH_X0, SH_Z0, SH_X1, SH_Z1, 0.1), SH_PLINTH, SH_EAVE, false, false)
	# Empenas (triangulos de parede nas pontas +-X).
	for x in [SH_X0, SH_X1]:
		var sx: float = -1.0 if x == SH_X0 else 1.0
		b.face(WALL, [Vector3(x, SH_EAVE, SH_Z0 + 0.1), Vector3(x, SH_EAVE, SH_Z1 - 0.1), Vector3(x, SH_RIDGE - 0.025, zc)], Vector3(sx, 0, 0))
	# Cobertura de duas aguas de baixa inclinacao (ardosia) com bordas brancas.
	var s := (SH_RIDGE - SH_EAVE) / (zc - SH_Z0)
	var xa := SH_X0 - ROOF_OHX
	var xb := SH_X1 + ROOF_OHX
	var y_edge := SH_EAVE + ROOF_T - ROOF_OH * s
	var y_ridge := SH_RIDGE + ROOF_T
	# Agua frontal: arestas 0 (beiral), 1 (empena +X), 2 (cumeeira, sem face), 3 (empena -X).
	_thick_quad(b, ROOF, TRIM, [Vector3(xa, y_edge, SH_Z0 - ROOF_OH), Vector3(xb, y_edge, SH_Z0 - ROOF_OH),
		Vector3(xb, y_ridge, zc), Vector3(xa, y_ridge, zc)], ROOF_T, [2])
	_thick_quad(b, ROOF, TRIM, [Vector3(xb, y_edge, SH_Z1 + ROOF_OH), Vector3(xa, y_edge, SH_Z1 + ROOF_OH),
		Vector3(xa, y_ridge, zc), Vector3(xb, y_ridge, zc)], ROOF_T, [2])
	if MONITOR:
		_monitor(b, zc, y_ridge, s)
	# Fachada frontal: dois portoes de enrolar fechados + porta de pessoal + janela alta.
	b.push(_facade_xform(Vector3(0, 0, -1), Vector3(0, 0, SH_Z0)))
	for dx in DOOR_XS:
		_vehicle_door(b, dx)
	_personnel_door(b, PDOOR_X, 0.0, PDOOR_W, PDOOR_H)
	_window(b, PDOOR_X, 3.0, 0.9, 0.85, false)
	b.pop()
	# Fundos: faixa de janelas altas.
	b.push(_facade_xform(Vector3(0, 0, 1), Vector3(0, 0, SH_Z1)))
	for wx in [-4.2, -0.6, 3.0, 6.6]:
		_window(b, wx, 2.9, 2.2, 1.1)
	b.pop()
	# Lateral -X: duas janelas altas.
	b.push(_facade_xform(Vector3(-1, 0, 0), Vector3(SH_X0, 0, 0)))
	for wz in [4.6, 8.6]:
		_window(b, -wz, 2.9, 2.2, 1.1)
	b.pop()


## Lanternim de cumeeira (ventilacao/iluminacao zenital): silhueta "galpao" sem texto.
func _monitor(b, zc: float, y_ridge: float, s: float) -> void:
	var hz := MON_W * 0.5
	var x0 := MON_X0
	var x1 := MON_X1
	var y_base := y_ridge - hz * s - 0.12
	var y_glass0 := y_ridge - hz * s + 0.08
	b.box(TRIM, Vector3(x0, y_base, zc - hz), Vector3(x1, MON_TOP, zc + hz), PackedStringArray(["-y", "+y"]))
	for sz in [-1.0, 1.0]:
		var z: float = zc + sz * (hz + 0.012)
		b.quad(GLASS, Vector3(x0 + 0.2, y_glass0, z), Vector3(x1 - 0.2, y_glass0, z), Vector3(x1 - 0.2, MON_TOP - 0.1, z), Vector3(x0 + 0.2, MON_TOP - 0.1, z), Vector3(0, 0, sz))
	# Cobertura do lanternim: duas aguas rasas com bordas brancas.
	var oh := 0.3
	var ohx := 0.15
	var rt := 0.15
	var yt := MON_TOP + 0.12
	var ye := yt - oh * s
	var yr := yt + hz * s
	_thick_quad(b, ROOF, TRIM, [Vector3(x0 - ohx, ye, zc - hz - oh), Vector3(x1 + ohx, ye, zc - hz - oh),
		Vector3(x1 + ohx, yr, zc), Vector3(x0 - ohx, yr, zc)], rt, [2])
	_thick_quad(b, ROOF, TRIM, [Vector3(x1 + ohx, ye, zc + hz + oh), Vector3(x0 - ohx, ye, zc + hz + oh),
		Vector3(x0 - ohx, yr, zc), Vector3(x1 + ohx, yr, zc)], rt, [2])
	# Empenas do lanternim.
	for x in [x0, x1]:
		var sx: float = -1.0 if x == x0 else 1.0
		b.face(TRIM, [Vector3(x, MON_TOP, zc - hz), Vector3(x, MON_TOP, zc + hz), Vector3(x, yr - rt, zc)], Vector3(sx, 0, 0))


## Portao de enrolar FECHADO (azul civico) com ripas horizontais e moldura clara.
func _vehicle_door(b, cx: float) -> void:
	var hw := DOOR_W * 0.5
	var dz := -0.11
	b.box(ACCENT, Vector3(cx - hw, 0.0, dz), Vector3(cx + hw, DOOR_H, -0.05), PackedStringArray(["+z", "-y"]))
	var y := 0.55
	while y < DOOR_H - 0.3:
		b.box(ACCENT, Vector3(cx - hw + 0.02, y, dz - 0.035), Vector3(cx + hw - 0.02, y + 0.07, dz), PackedStringArray(["+z"]))
		y += 0.55
	var f := 0.22
	b.box(TRIM, Vector3(cx - hw - f, 0.0, -0.2), Vector3(cx - hw, DOOR_H, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(cx + hw, 0.0, -0.2), Vector3(cx + hw + f, DOOR_H, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(cx - hw - f - 0.05, DOOR_H, -0.24), Vector3(cx + hw + f + 0.05, DOOR_H + 0.3, 0.0), PackedStringArray(["+z", "-y"]))


## Porta de pessoal fechada (azul) com moldura clara; referencial de fachada.
func _personnel_door(b, cx: float, y0: float, w: float, h: float) -> void:
	var hw := w * 0.5
	b.box(ACCENT, Vector3(cx - hw, y0, -0.1), Vector3(cx + hw, y0 + h, -0.05), PackedStringArray(["+z", "-y"]))
	var f := 0.13
	b.box(TRIM, Vector3(cx - hw - f, y0, -0.16), Vector3(cx - hw, y0 + h, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(cx + hw, y0, -0.16), Vector3(cx + hw + f, y0 + h, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(cx - hw - f - 0.04, y0 + h, -0.2), Vector3(cx + hw + f + 0.04, y0 + h + f + 0.04, 0.0), PackedStringArray(["+z", "-y"]))


# ---------------------------------------------------------------------------
# Escritorio anexo
# ---------------------------------------------------------------------------

func _office(b) -> void:
	b.extrude(CONC, _chamfer_rect(OF_X0, OF_Z0 - 0.05, OF_X1 + 0.05, OF_Z1 + 0.05, 0.12), 0.0, OF_PLINTH, true, false)
	b.extrude(WALL, _chamfer_rect(OF_X0, OF_Z0, OF_X1, OF_Z1, 0.1), OF_PLINTH, OF_PARAPET, false, false)
	# Platibanda: faces internas + laje plana em ardosia (civic_roof).
	var ix0 := OF_X0 + OF_PARAPET_T
	var ix1 := OF_X1 - OF_PARAPET_T
	var iz0 := OF_Z0 + OF_PARAPET_T
	var iz1 := OF_Z1 - OF_PARAPET_T
	var ry := OF_ROOF_Y
	var py := OF_PARAPET
	b.quad(WALL, Vector3(ix0, ry, iz0), Vector3(ix1, ry, iz0), Vector3(ix1, py, iz0), Vector3(ix0, py, iz0), Vector3(0, 0, 1))
	b.quad(WALL, Vector3(ix0, ry, iz1), Vector3(ix1, ry, iz1), Vector3(ix1, py, iz1), Vector3(ix0, py, iz1), Vector3(0, 0, -1))
	b.quad(WALL, Vector3(ix0, ry, iz0), Vector3(ix0, ry, iz1), Vector3(ix0, py, iz1), Vector3(ix0, py, iz0), Vector3(1, 0, 0))
	b.quad(WALL, Vector3(ix1, ry, iz0), Vector3(ix1, ry, iz1), Vector3(ix1, py, iz1), Vector3(ix1, py, iz0), Vector3(-1, 0, 0))
	b.quad(ROOF, Vector3(ix0, ry, iz0), Vector3(ix1, ry, iz0), Vector3(ix1, ry, iz1), Vector3(ix0, ry, iz1), Vector3.UP)
	# Faixa azul civica (acento) contornando o volume, logo abaixo da capa.
	_ring(b, ACCENT, _chamfer_rect(OF_X0 - 0.04, OF_Z0 - 0.04, OF_X1 + 0.04, OF_Z1 + 0.04, 0.12),
		_chamfer_rect(OF_X0, OF_Z0, OF_X1, OF_Z1, 0.1), OF_BAND_Y0, OF_BAND_Y1)
	# Capa (pingadeira) branca da platibanda.
	var co := 0.06
	var cy0 := OF_PARAPET
	var cy1 := OF_PARAPET + OF_COPING
	b.box(TRIM, Vector3(OF_X0, cy0, OF_Z0 - co), Vector3(OF_X1 + co, cy1, iz0 + 0.04), PackedStringArray(["-y"]))
	b.box(TRIM, Vector3(OF_X0, cy0, iz1 - 0.04), Vector3(OF_X1 + co, cy1, OF_Z1 + co), PackedStringArray(["-y"]))
	b.box(TRIM, Vector3(OF_X0, cy0, iz0 + 0.04), Vector3(ix0 - 0.04, cy1, iz1 - 0.04), PackedStringArray(["-y", "-z", "+z"]))
	b.box(TRIM, Vector3(ix1 + 0.04, cy0, iz0 + 0.04), Vector3(OF_X1 + co, cy1, iz1 - 0.04), PackedStringArray(["-y", "-z", "+z"]))
	# Fachada -Z: porta azul central, duas janelas, marquise e degrau.
	b.push(_facade_xform(Vector3(0, 0, -1), Vector3(0, 0, OF_Z0)))
	_personnel_door(b, OF_DOOR_X, OF_PLINTH, OF_DOOR_W, OF_DOOR_H)
	for wx in [OF_DOOR_X - 1.85, OF_DOOR_X + 1.85]:
		_window(b, wx, 1.15, 1.3, 1.35)
	b.box(TRIM, Vector3(OF_DOOR_X - 1.0, OF_PLINTH + OF_DOOR_H + 0.25, -1.0), Vector3(OF_DOOR_X + 1.0, OF_PLINTH + OF_DOOR_H + 0.42, 0.0), PackedStringArray(["+z", "-y"]))
	b.box(CONC, Vector3(OF_DOOR_X - 0.8, 0.0, -0.75), Vector3(OF_DOOR_X + 0.8, 0.16, -0.05), PackedStringArray(["+z", "-y"]))
	b.pop()
	# Lateral +X: duas janelas.
	b.push(_facade_xform(Vector3(1, 0, 0), Vector3(OF_X1, 0, 0)))
	for wz in [2.9, 5.9]:
		_window(b, wz, 1.15, 1.3, 1.35)
	b.pop()
	# Fundos: uma janela.
	b.push(_facade_xform(Vector3(0, 0, 1), Vector3(0, 0, OF_Z1)))
	_window(b, -OF_DOOR_X, 1.15, 1.3, 1.35)
	b.pop()


# ---------------------------------------------------------------------------
# Estoque (State_Stock) — cada filho pequeno e dentro da sua baia/area
# ---------------------------------------------------------------------------

func _stock_sand(b) -> void:
	var c := Vector3(_bay_center_x() - 0.15, SLAB_H, _bay_centers()[0])
	_heap(b, "depot_sand", c, 1.2, 1.02, 1.05, 10, 7)


func _stock_concrete(b) -> void:
	var cz: float = _bay_centers()[1]
	var cx := _bay_center_x()
	for k in 2:
		var z0: float = cz - 1.08 + float(k) * 1.14
		# Estrado de madeira baixo + tres fiadas de blocos pre-moldados (duas pecas por fiada).
		b.box("depot_timber", Vector3(cx - 0.55, SLAB_H, z0), Vector3(cx + 0.55, SLAB_H + 0.12, z0 + 1.02), PackedStringArray(["-y"]))
		for j in 3:
			var y0 := SLAB_H + 0.12 + float(j) * 0.31
			for h in 2:
				var za := z0 + 0.03 + float(h) * 0.49
				b.box("concrete_dark", Vector3(cx - 0.5, y0, za), Vector3(cx + 0.5, y0 + 0.29, za + 0.47), PackedStringArray(["-y"]))


func _stock_steel(b) -> void:
	var cz: float = _bay_centers()[3]
	var cx := _bay_center_x()
	var L := 2.3
	# Dois calcos de concreto transversais (ao longo de Z).
	for sx in [-0.7, 0.7]:
		b.box(CONC, Vector3(cx + sx - 0.1, SLAB_H, cz - 0.6), Vector3(cx + sx + 0.1, SLAB_H + 0.14, cz + 0.6), PackedStringArray(["-y"]))
	# Feixe de barras ao longo de X, empilhadas 7-6-5-4, apoiadas nos calcos.
	var sec := 0.12
	var gap := 0.035
	var rows := [7, 6, 5, 4]
	for r in rows.size():
		var n: int = rows[r]
		var y0 := SLAB_H + 0.14 + float(r) * sec
		var w := float(n) * sec + float(n - 1) * gap
		for i in n:
			var z0 := cz - w * 0.5 + float(i) * (sec + gap)
			b.box("metal_light", Vector3(cx - L * 0.5, y0, z0), Vector3(cx + L * 0.5, y0 + sec, z0 + sec), PackedStringArray(["-y"]))


func _stock_wood(b) -> void:
	var cz: float = _bay_centers()[2]
	var cx := _bay_center_x()
	var L := 2.3
	var pw := 0.3      # largura da tabua
	var ph := 0.12     # espessura da tabua
	var pg := 0.04
	var n := 4
	var w := float(n) * pw + float(n - 1) * pg
	var y := SLAB_H
	for layer in 5:
		# Separadores transversais (sarrafos) sob cada camada.
		for sx in [-0.8, 0.8]:
			b.box("depot_timber", Vector3(cx + sx - 0.05, y, cz - w * 0.5 - 0.05), Vector3(cx + sx + 0.05, y + 0.06, cz + w * 0.5 + 0.05), PackedStringArray(["-y"]))
		y += 0.06
		var cnt: int = n if layer < 4 else n - 1
		for i in cnt:
			var z0 := cz - w * 0.5 + float(i) * (pw + pg)
			b.box("depot_timber", Vector3(cx - L * 0.5, y, z0), Vector3(cx + L * 0.5, y + ph, z0 + pw), PackedStringArray(["-y"]))
		y += ph


func _stock_asphalt(b) -> void:
	var c := Vector3((ASPH_X0 + ASPH_X1) * 0.5, SLAB_H + ASPH_H, (ASPH_Z0 + ASPH_Z1) * 0.5)
	_heap(b, "road_asphalt", c, 1.02, 0.74, 0.55, 10, 21)


# ---------------------------------------------------------------------------
# Icone de catalogo (make_v1_icons.gd chama draw_icon(ic)). Versao SIMPLIFICADA:
# galpao com portoes azuis + escritorio com faixa civica + patio + baias COM estoque
# (o icone ilustra o patio "em operacao"; no jogo o estoque so aparece por estado real).
# ---------------------------------------------------------------------------

func draw_icon(ic) -> void:
	var pave: String = ic.c(PAVE)
	var conc: String = ic.c(CONC)
	var wall: String = ic.c(WALL)
	var roof: String = ic.c(ROOF)
	var acc: String = ic.c(ACCENT)
	var white: String = ic.c(TRIM)
	var glass: String = ic.c(GLASS)
	var sand: String = ic.c("depot_sand")
	var timber: String = ic.c("depot_timber")
	var block: String = ic.c("concrete_dark")
	# Layout PROPRIO do icone (simplificado): patio mais raso e 3 baias com estoque alto,
	# porque em 64 px o patio inteiro vira so uma placa cinza e as baias somem. O topo do
	# piso usa o tom claro "concrete": e o tom percebido do piso iluminado no render 3D.
	var y0 := ICON_SLAB
	ic.part(-100.0)
	ic.box(Vector3(-HX, 0, ICON_Z_FRONT), Vector3(HX, y0, HZ), pave, conc)
	var bx1 := -HX + BAY_BACK_T
	var mouth := BAY_MOUTH_X + 0.3
	var bw := ICON_BAY_W
	var z_first := ICON_Z_FRONT + 0.3
	var n := 3
	ic.part(-50.0)
	ic.box(Vector3(-HX, y0, z_first), Vector3(bx1, y0 + ICON_BAY_BACK_H, z_first + float(n) * (bw + BAY_DIV) + BAY_DIV), conc)
	var centers := []
	for i in n + 1:
		var z := z_first + float(i) * (bw + BAY_DIV)
		ic.part()
		ic.box(Vector3(bx1, y0, z), Vector3(mouth, y0 + ICON_BAY_H, z + BAY_DIV), conc)
		if i < n:
			centers.append(z + BAY_DIV + bw * 0.5)
	var cx := (bx1 + mouth) * 0.5 + 0.1
	# Baia 1: monte de areia/brita (cone alto). Baia 2: blocos pre-moldados. Baia 3: madeira.
	ic.part()
	ic.prism(Vector3(cx, y0, centers[0]), 1.2, 0.7, 1.35, 6, sand)
	ic.part()
	ic.box(Vector3(cx - 0.75, y0, centers[1] - 0.9), Vector3(cx + 0.75, y0 + 0.2, centers[1] + 0.9), timber)
	ic.part(0.2)
	ic.box(Vector3(cx - 0.7, y0 + 0.2, centers[1] - 0.85), Vector3(cx + 0.7, y0 + 1.75, centers[1] + 0.85), block)
	ic.part()
	ic.box(Vector3(cx - 1.25, y0, centers[2] - 0.9), Vector3(cx + 1.25, y0 + 1.75, centers[2] + 0.9), timber)
	# Galpao: paredes e os dois grandes portoes azuis fechados (assinatura "garagem de obras").
	var zf := SH_Z0 - 0.02
	ic.part()
	ic.box(Vector3(SH_X0, y0, SH_Z0), Vector3(SH_X1, SH_EAVE, SH_Z1), wall)
	for dx in DOOR_XS:
		ic.facade_rect(dx - DOOR_W * 0.5 - 0.35, dx + DOOR_W * 0.5 + 0.35, y0, DOOR_H + 0.45, zf, white)
		ic.facade_rect(dx - DOOR_W * 0.5, dx + DOOR_W * 0.5, y0, DOOR_H, zf - 0.01, acc)
	# Cobertura de baixa inclinacao + lanternim.
	var zc := (SH_Z0 + SH_Z1) * 0.5
	var s := (SH_RIDGE - SH_EAVE) / (zc - SH_Z0)
	ic.part(1.0)
	ic.gable(Vector3(SH_X0 - ROOF_OHX, SH_EAVE - ROOF_OH * s, SH_Z0 - ROOF_OH), Vector3(SH_X1 + ROOF_OHX, SH_EAVE - ROOF_OH * s, SH_Z1 + ROOF_OH), SH_RIDGE + ROOF_T, "x", roof, wall)
	var hz := MON_W * 0.5
	ic.part(2.0)
	ic.box(Vector3(MON_X0, SH_RIDGE, zc - hz), Vector3(MON_X1, MON_TOP + 0.1, zc + hz), white)
	ic.facade_rect(MON_X0 + 0.3, MON_X1 - 0.3, SH_RIDGE + 0.3, MON_TOP - 0.1, zc - hz - 0.02, glass)
	ic.part(2.1)
	ic.gable(Vector3(MON_X0 - 0.2, MON_TOP + 0.1, zc - hz - 0.35), Vector3(MON_X1 + 0.2, MON_TOP + 0.1, zc + hz + 0.35), MON_TOP + 0.22 + hz * s + 0.15, "x", roof, white)
	# Escritorio anexo: faixa azul civica larga e platibanda branca (exageradas para o icone).
	var oz := OF_Z0 - 0.02
	ic.part(3.0)
	ic.box(Vector3(OF_X0, y0, OF_Z0), Vector3(OF_X1, 2.9, OF_Z1), wall)
	ic.facade_rect(OF_DOOR_X - 0.75, OF_DOOR_X + 0.75, y0, 2.62, oz, white)
	ic.facade_rect(OF_DOOR_X - 0.55, OF_DOOR_X + 0.55, y0, 2.45, oz - 0.01, acc)
	ic.facade_rect(OF_DOOR_X + 1.15, OF_DOOR_X + 2.55, 0.95, 2.35, oz, white)
	ic.facade_rect(OF_DOOR_X + 1.33, OF_DOOR_X + 2.37, 1.13, 2.17, oz - 0.01, glass)
	ic.part(3.1)
	ic.box(Vector3(OF_X0 - 0.05, 2.9, OF_Z0 - 0.05), Vector3(OF_X1 + 0.05, 3.95, OF_Z1 + 0.05), acc)
	ic.part(3.2)
	ic.box(Vector3(OF_X0 - 0.12, 3.95, OF_Z0 - 0.12), Vector3(OF_X1 + 0.12, 4.6, OF_Z1 + 0.12), white, roof)
