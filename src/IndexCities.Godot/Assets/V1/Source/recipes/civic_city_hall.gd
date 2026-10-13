## Receita original: Prefeitura (civic.city_hall.a) — amostra V1, revisao humana PENDENTE.
##
## SOMENTE APRESENTACAO: nenhuma regra de gameplay, simulacao, acesso ou ocupacao.
## Geometria 100% autoral a partir das primitivas do kit (sem origem externa).
##
## Composicao (silhueta singular, legivel sem texto):
##   embasamento elevado -> bloco principal de dois pavimentos com cobertura de quatro
##   aguas em ardosia -> pavilhao central mais alto -> portico saliente com colunas e
##   frontao triangular (timpano azul) -> atico -> tambor -> cupula patinada -> lanternim.
## Sem relogio, sem bandeira, sem texto, sem simbolos. Portas fechadas, vidros escuros.
##
## Convencoes: Y para cima, 1 un = 1 m (candidato), fachada candidata em -Z,
## pivo no solo no centro do corpo principal (embasamento, sem a escadaria).
extends RefCounted

const FAMILY_ID := "civic.city_hall.a"
const MATERIAL_PREFIX := "civic_"
const MATERIALS := {
	"civic_stone": {"albedo": "#c9c1af", "roughness": 0.88},
	"civic_roof": {"albedo": "#5f6b75", "roughness": 0.8},
	"civic_dome": {"albedo": "#5a9a8c", "roughness": 0.6, "metallic": 0.1},
	"civic_accent": {"albedo": "#3a62a0", "roughness": 0.7},
}

const STONE := "civic_stone"
const ROOF := "civic_roof"
const DOME := "civic_dome"
const ACCENT := "civic_accent"
const TRIM := "trim_white"
const GLASS := "glass_dark"
const PLINTH := "plinth_stone"

# ---------------------------------------------------------------------------
# Medidas CANDIDATAS (metros). Nao aprovam footprint, acesso, capacidade ou custo.
# ---------------------------------------------------------------------------
const PODIUM_H := 1.0
const PODIUM_LEDGE := 0.25

# Bloco principal (alas + miolo atravessando o pavilhao central).
const MAIN_X := 12.0
const MAIN_Z0 := -4.4
const MAIN_Z1 := 6.7
const MAIN_WALL_TOP := 8.2
const MAIN_CORNICE_TOP := 8.8
const MAIN_RIDGE := 10.1
const STRING_Y := 4.75

# Pavilhao central.
const CEN_X := 5.4
const CEN_Z0 := -5.0
const CEN_Z1 := 7.3
const CEN_WALL_TOP := 9.7
const CEN_FASCIA_Y := 9.45
const CEN_CORNICE_TOP := 10.3

# Portico.
const POR_FRONT := -7.6        # borda do embasamento sob o portico
const ENT_FRONT := -7.5        # plano da arquitrave e do timpano
const ENT_X := 5.0
const COL_Z := -6.95
const COL_XS := [-4.25, -1.45, 1.45, 4.25]
const COL_R := 0.45
const COL_SHAFT_TOP := 8.45
const ABACUS_TOP := 8.9
const ARCHITRAVE_TOP := 9.3
const PED_X := 5.45            # meia largura do frontao/cornija do portico
const PED_FRONT := -7.95       # face frontal da cornija horizontal do portico
const RAKE_FRONT := PED_FRONT - 0.03   # cornija inclinada 3 cm a frente (sem faces coplanares)
const ROOF_FRONT := PED_FRONT - 0.06   # beiral do telhado do portico, a frente da cornija inclinada
const PED_APEX := 12.05        # 0,3 m abaixo do capeamento do atico (sem tangencia na elevacao)

# Escadaria.
const STEP_COUNT := 5
const STEP_TREAD := 0.5
const STAIR_X := 4.6

# Atico, tambor, cupula, lanternim (eixo central).
const DOME_Z := 1.15
const ATTIC_HALF := 4.2
const ATTIC_TOP := 12.35
const COPING_TOP := 12.6
const DRUM_R := 3.2
const DRUM_H := 0.95
const DOME_R := 3.1
const DOME_SIDES := 16
const DOME_PHASE := 11.25      # tambor: uma face do poligono voltada para -Z
const DOME_ANGS := [0.0, 21.0, 42.0, 60.0, 75.0]   # aneis da cupula (graus); abertura no topo p/ o lanternim
const DOME_RIBS := false
const RIB_SLOT := "trim_white"
const RIB_T := 0.2
# Lanternim (remate): maior e mais baixo, para ler em tamanho de icone.
const LANT_R := 0.75
const LANT_H := 0.42
const LANT_CAP_R := 0.95
const LANT_CAP_H := 0.4
const LANT_CAP_SLOT := "trim_white"   # A/B: "civic_dome" (patina) ou "trim_white"
# Bocel claro em cada degrau (marca a escadaria sem nova superficie).
const NOSING_H := 0.05
const NOSING_OUT := 0.05
const NOSING_IN := 0.08
const NOSING_UP := 0.015

## Alturas e contagens medidas durante a construcao (para o manifesto).
var _dome_shell_top := 0.0
var _top_y := 0.0
var _win_tris := 0
var _win_count := 0


func build(kit) -> void:
	var b = kit.builder()
	var parts_tris := {}
	var t0: int = b.triangle_count()
	_podium_and_stairs(b)
	parts_tris["embasamento_escadaria"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_main_block(b)
	parts_tris["alas"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_central_block(b)
	parts_tris["pavilhao"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_portico(b)
	parts_tris["portico"] = b.triangle_count() - t0
	t0 = b.triangle_count()
	_dome(b)
	parts_tris["atico_tambor_cupula_lanternim"] = b.triangle_count() - t0
	var total: int = b.triangle_count()
	print("[civic_city_hall] triangulos = ", total, " | por parte = ", parts_tris,
		" | janelas de fachada = ", _win_count, " x ", _win_tris / max(_win_count, 1), " = ", _win_tris)
	var mesh = kit.save_mesh(b, FAMILY_ID)

	var root = kit.new_root("CityHall", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var stair_front := POR_FRONT - STEP_COUNT * STEP_TREAD
	var stair_blocks_front := stair_front - 0.1
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3(0, 0, stair_front - 0.5), Vector3.FORWARD,
		"porta principal candidata; acesso real e definido pela integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Prefeitura",
		"proposed": {
			"corpo_x_m": (MAIN_X + PODIUM_LEDGE) * 2.0,
			"corpo_z_m": snappedf(CEN_Z1 + 0.3 - POR_FRONT, 0.01),
			"profundidade_paredes_alas_m": snappedf(MAIN_Z1 - MAIN_Z0, 0.01),
			"profundidade_paredes_pavilhao_m": snappedf(CEN_Z1 - CEN_Z0, 0.01),
			"escadaria_avanco_m": snappedf(POR_FRONT - stair_blocks_front, 0.01),
			"avanco_portico_alem_da_porta_m": snappedf(CEN_Z0 - ROOF_FRONT, 0.01),
			"avanco_total_alem_da_porta_m": snappedf(CEN_Z0 - stair_blocks_front, 0.01),
			"avanco_total_alem_fachada_alas_m": snappedf(MAIN_Z0 - stair_blocks_front, 0.01),
			"eixo_cupula_z_m": DOME_Z,
			"embasamento_m": PODIUM_H,
			"cornija_alas_m": MAIN_CORNICE_TOP,
			"cornija_pavilhao_central_m": CEN_CORNICE_TOP,
			"apice_frontao_m": PED_APEX,
			"topo_atico_m": COPING_TOP,
			"topo_cupula_casca_m": snappedf(_dome_shell_top, 0.01),
			"topo_lanternim_m": snappedf(_top_y, 0.01),
		},
		"parts": {
			"Body": "malha unica estatica; 7 superficies (slot = material): plinth_stone embasamento/escadaria, civic_stone paredes/atico/tambor/friso, trim_white colunas/cornijas/molduras/pilastras/bocel dos degraus/lanternim, civic_roof coberturas, glass_dark vidros, civic_accent porta/timpano, civic_dome cupula/capa do lanternim",
			"Candidate_PedestrianEntry": "marcador candidato no solo 0,5 m diante do primeiro degrau, eixo central, frente -Z",
		},
		"notes": [
			"Pivo no solo, no centro do embasamento (sem a escadaria). O eixo do atico/tambor/cupula fica em z = +1,15 m (atras do pivo).",
			"Medidas em Z (candidatas): o 'corpo ~15 m' inclui o portico aberto sobre o embasamento (z -7,6 a +7,6). Paredes: alas 11,1 m (z -4,4 a +6,7), pavilhao 12,3 m (z -5,0 a +7,3).",
			"Leituras do limite 'escadaria <= 3 m': medido da borda do embasamento = 2,6 m; da parede da porta (z -5,0) ate a frente da escadaria = 5,2 m (portico 3,0 m + escadaria); da fachada das alas (z -4,4) = 5,8 m. A referencia de lote/pivo e decisao da integracao.",
			"Desvio candidato do guia de estilo: embasamento de 1,0 m (guia: 0,2-0,45 m) para sustentar a escadaria larga de 5 degraus, sinal de entrada principal. A porta desenhada fica a +1,0 m, sem rampa; representacao de acessibilidade nao e decidida no asset. Pendente de revisao humana.",
			"Sem relogio, bandeira, texto ou simbolo; portas fechadas; vidros escuros sem interior.",
			"Desenho de porta/escadaria nao e acesso funcional: so o marcador candidato, a confirmar na integracao.",
			"civic_stone #c9c1af (pedra cinza-quente, mais escura que a sugestao): a sugestao #e3dccd apagava molduras/colunas brancas e o #d9c8a9 anterior ficava proximo da parede creme da casa. civic_accent #3a62a0 (sugestao #2e4e72) para a porta/timpano nao se confundirem com o vidro escuro. Cores pendentes de compatibilizacao entre familias.",
			"civic_dome mantida na sugestao #5a9a8c (patina verde-azulada). Tem matiz proximo ao do toldo/parede da mercearia: compatibilizacao entre familias pendente (alternativa testada: #5b949c, mais azulada, le menos como patina e se aproxima da ardosia).",
			"Variante com nervuras brancas na cupula existe na receita (DOME_RIBS), desligada: em tamanho pequeno fragmenta a massa patinada.",
			"Triangulos (build): total %d; por parte %s; janelas de fachada (ja contadas nas partes) %d x %d = %d, primeiro corte de uma eventual LOD." % [total, str(parts_tris), _win_count, _win_tris / max(_win_count, 1), _win_tris],
		],
	})


# ---------------------------------------------------------------------------
# Auxiliares de fachada
# ---------------------------------------------------------------------------

## Referencial local de uma fachada: plano da parede em z=0, para fora = -Z local,
## +X local = horizontal na fachada, +Y = cima. `n` = normal externa (horizontal).
static func _facade_xform(n: Vector3, origin: Vector3) -> Transform3D:
	var x := Vector3(-n.z, 0.0, n.x)
	return Transform3D(Basis(x, Vector3.UP, -n), origin)


## Janela no referencial de fachada: vidro escuro recuado + moldura e peitoril claros.
func _window(b, cx: float, y0: float, w: float, h: float, cross: bool = true) -> void:
	var t0: int = b.triangle_count()
	_window_geo(b, cx, y0, w, h, cross)
	_win_tris += b.triangle_count() - t0
	_win_count += 1


func _window_geo(b, cx: float, y0: float, w: float, h: float, cross: bool) -> void:
	var hw := w * 0.5
	var f := 0.14
	var d := 0.11
	b.quad(GLASS, Vector3(cx - hw, y0, -0.012), Vector3(cx + hw, y0, -0.012),
		Vector3(cx + hw, y0 + h, -0.012), Vector3(cx - hw, y0 + h, -0.012), Vector3(0, 0, -1))
	# Ombreiras.
	b.box(TRIM, Vector3(cx - hw - f, y0, -d), Vector3(cx - hw, y0 + h, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(cx + hw, y0, -d), Vector3(cx + hw + f, y0 + h, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	# Verga com leve cornija.
	b.box(TRIM, Vector3(cx - hw - f - 0.05, y0 + h, -d - 0.04), Vector3(cx + hw + f + 0.05, y0 + h + f + 0.04, 0.0), PackedStringArray(["+z", "-y"]))
	# Peitoril saliente.
	b.box(TRIM, Vector3(cx - hw - f - 0.08, y0 - 0.14, -d - 0.08), Vector3(cx + hw + f + 0.08, y0, 0.0), PackedStringArray(["+z", "-y"]))
	if cross:
		var ty := y0 + h * 0.68
		b.box(TRIM, Vector3(cx - 0.04, y0, -0.06), Vector3(cx + 0.04, y0 + h, -0.012), PackedStringArray(["+z", "-y", "+y"]))
		b.box(TRIM, Vector3(cx - hw, ty - 0.04, -0.06), Vector3(cx - 0.04, ty + 0.04, -0.012), PackedStringArray(["+z", "-x", "+x"]))
		b.box(TRIM, Vector3(cx + 0.04, ty - 0.04, -0.06), Vector3(cx + hw, ty + 0.04, -0.012), PackedStringArray(["+z", "-x", "+x"]))


## Pilastra plana no referencial de fachada.
func _pilaster(b, cx: float, w: float, y0: float, y1: float, depth: float = 0.12) -> void:
	b.box(TRIM, Vector3(cx - w * 0.5, y0, -depth), Vector3(cx + w * 0.5, y1, 0.0), PackedStringArray(["+z", "-y"]))


# ---------------------------------------------------------------------------
# Partes
# ---------------------------------------------------------------------------

func _podium_and_stairs(b) -> void:
	var l := PODIUM_LEDGE
	var ex := MAIN_X + l
	var cx := CEN_X + l
	var poly := PackedVector2Array([
		Vector2(-ex, MAIN_Z0 - l), Vector2(-cx, MAIN_Z0 - l), Vector2(-cx, POR_FRONT),
		Vector2(cx, POR_FRONT), Vector2(cx, MAIN_Z0 - l), Vector2(ex, MAIN_Z0 - l),
		Vector2(ex, MAIN_Z1 + l), Vector2(cx, MAIN_Z1 + l), Vector2(cx, CEN_Z1 + 0.3),
		Vector2(-cx, CEN_Z1 + 0.3), Vector2(-cx, MAIN_Z1 + l), Vector2(-ex, MAIN_Z1 + l),
	])
	b.extrude(PLINTH, poly, 0.0, PODIUM_H, true, false)
	# Escadaria frontal larga (degraus de 0,2 m) entre dois blocos laterais.
	var rise := PODIUM_H / float(STEP_COUNT)
	for i in STEP_COUNT:
		var z0 := POR_FRONT - STEP_TREAD * float(STEP_COUNT - i)
		var y1 := rise * float(i + 1)
		b.quad(PLINTH, Vector3(-STAIR_X, y1 - rise, z0), Vector3(STAIR_X, y1 - rise, z0),
			Vector3(STAIR_X, y1, z0), Vector3(-STAIR_X, y1, z0), Vector3(0, 0, -1))
		b.quad(PLINTH, Vector3(-STAIR_X, y1, z0), Vector3(STAIR_X, y1, z0),
			Vector3(STAIR_X, y1, z0 + STEP_TREAD), Vector3(-STAIR_X, y1, z0 + STEP_TREAD), Vector3.UP)
		# Bocel claro: linha branca em cada degrau (leitura de "escadaria" em tamanho pequeno).
		# Topo 1,5 cm acima do piso (sem coplanaridade); laterais encostam nos blocos (ocultas).
		b.box(TRIM, Vector3(-STAIR_X, y1 - NOSING_H, z0 - NOSING_OUT), Vector3(STAIR_X, y1 + NOSING_UP, z0 + NOSING_IN),
			PackedStringArray(["-y", "-x", "+x"]))
	var zf := POR_FRONT - STEP_TREAD * STEP_COUNT - 0.1
	for s in [-1.0, 1.0]:
		var xa: float = STAIR_X if s > 0.0 else -cx
		var xb: float = cx if s > 0.0 else -STAIR_X
		b.bevel_box(PLINTH, Vector3(xa, 0.0, zf), Vector3(xb, PODIUM_H + 0.12, POR_FRONT + 0.02), 0.05, false)


func _main_block(b) -> void:
	b.bevel_box(STONE, Vector3(-MAIN_X, PODIUM_H, MAIN_Z0), Vector3(MAIN_X, MAIN_WALL_TOP, MAIN_Z1), 0.1, false)
	# Faixa entre pavimentos, friso e cornija.
	b.box(TRIM, Vector3(-MAIN_X - 0.08, STRING_Y, MAIN_Z0 - 0.08), Vector3(MAIN_X + 0.08, STRING_Y + 0.25, MAIN_Z1 + 0.08), PackedStringArray(["-y"]))
	b.box(TRIM, Vector3(-MAIN_X - 0.1, 7.95, MAIN_Z0 - 0.1), Vector3(MAIN_X + 0.1, MAIN_WALL_TOP, MAIN_Z1 + 0.1), PackedStringArray(["-y"]))
	b.bevel_box(TRIM, Vector3(-MAIN_X - 0.3, MAIN_WALL_TOP, MAIN_Z0 - 0.3), Vector3(MAIN_X + 0.3, MAIN_CORNICE_TOP, MAIN_Z1 + 0.3), 0.08, false)
	# Cobertura de quatro aguas em ardosia, recuada para mostrar a cornija branca.
	var hz := (MAIN_Z1 - MAIN_Z0) * 0.5 + 0.15
	b.hip_roof(ROOF, Vector2(-MAIN_X - 0.15, MAIN_Z0 - 0.15), Vector2(MAIN_X + 0.15, MAIN_Z1 + 0.15),
		MAIN_CORNICE_TOP, MAIN_RIDGE, hz, "x", false)
	# Cunhais (pilastras de canto).
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			var xa: float = MAIN_X - 0.55 if sx > 0.0 else -MAIN_X - 0.06
			var xb: float = MAIN_X + 0.06 if sx > 0.0 else -MAIN_X + 0.55
			var za: float = MAIN_Z0 - 0.06 if sz < 0.0 else MAIN_Z1 - 0.55
			var zb: float = MAIN_Z0 + 0.55 if sz < 0.0 else MAIN_Z1 + 0.06
			b.box(TRIM, Vector3(xa, PODIUM_H, za), Vector3(xb, 7.95, zb), PackedStringArray(["-y", "+y"]))
	# Janelas das alas: frente e fundos (3 vaos por ala x 2 pavimentos).
	var wing_xs := [-10.4, -8.6, -6.8, 6.8, 8.6, 10.4]
	for fc in [[Vector3(0, 0, -1), MAIN_Z0], [Vector3(0, 0, 1), MAIN_Z1]]:
		var n: Vector3 = fc[0]
		b.push(_facade_xform(n, Vector3(0, 0, fc[1])))
		for wx in wing_xs:
			_window(b, wx, 1.85, 1.2, 2.3)
			_window(b, wx, 5.55, 1.2, 2.1)
		b.pop()
	# Laterais (4 vaos x 2 pavimentos).
	var side_zs := [-2.8, -0.167, 2.467, 5.1]
	for sx in [-1.0, 1.0]:
		var n := Vector3(sx, 0, 0)
		b.push(_facade_xform(n, Vector3(sx * MAIN_X, 0, 0)))
		for wz in side_zs:
			var cx: float = wz if sx > 0.0 else -wz
			_window(b, cx, 1.85, 1.2, 2.3)
			_window(b, cx, 5.55, 1.2, 2.1)
		b.pop()


func _central_block(b) -> void:
	b.bevel_box(STONE, Vector3(-CEN_X, PODIUM_H, CEN_Z0), Vector3(CEN_X, CEN_WALL_TOP, CEN_Z1), 0.1, false)
	b.box(TRIM, Vector3(-CEN_X - 0.1, CEN_FASCIA_Y, CEN_Z0 - 0.1), Vector3(CEN_X + 0.1, CEN_WALL_TOP, CEN_Z1 + 0.1), PackedStringArray(["-y"]))
	b.bevel_box(TRIM, Vector3(-CEN_X - 0.3, CEN_WALL_TOP, CEN_Z0 - 0.3), Vector3(CEN_X + 0.3, CEN_CORNICE_TOP, CEN_Z1 + 0.3), 0.08, false)
	# Laje de cobertura (ardosia) atras do portico; deixa um friso branco da cornija a mostra.
	b.box(ROOF, Vector3(-CEN_X, CEN_CORNICE_TOP, -3.0), Vector3(CEN_X, CEN_CORNICE_TOP + 0.08, CEN_Z1), PackedStringArray(["-y"]))
	# Cunhais do pavilhao.
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			var xa: float = CEN_X - 0.5 if sx > 0.0 else -CEN_X - 0.06
			var xb: float = CEN_X + 0.06 if sx > 0.0 else -CEN_X + 0.5
			var za: float = CEN_Z0 - 0.06 if sz < 0.0 else CEN_Z1 - 0.5
			var zb: float = CEN_Z0 + 0.5 if sz < 0.0 else CEN_Z1 + 0.06
			b.box(TRIM, Vector3(xa, PODIUM_H, za), Vector3(xb, CEN_FASCIA_Y, zb), PackedStringArray(["-y", "+y"]))
	# Fachada sob o portico: porta dupla fechada e janelas. Sem pilastras atras das colunas:
	# a parede de pedra na sombra separa as colunas brancas (figura x fundo).
	b.push(_facade_xform(Vector3(0, 0, -1), Vector3(0, 0, CEN_Z0)))
	_door(b)
	for wx in [-2.85, 2.85]:
		_window(b, wx, 1.85, 1.2, 2.3)
	for wx in [-2.85, 0.0, 2.85]:
		_window(b, wx, 5.9, 1.2, 2.3)
	b.pop()
	# Fundos do pavilhao.
	b.push(_facade_xform(Vector3(0, 0, 1), Vector3(0, 0, CEN_Z1)))
	for wx in [-2.85, 0.0, 2.85]:
		_window(b, wx, 1.85, 1.2, 2.3)
		_window(b, wx, 5.9, 1.2, 2.3)
	b.pop()


## Porta dupla fechada (acento azul) com bandeira de vidro e moldura; referencial de fachada.
func _door(b) -> void:
	var hw := 0.9
	var y0 := PODIUM_H
	var y1 := y0 + 3.5
	b.quad(ACCENT, Vector3(-hw, y0, -0.015), Vector3(hw, y0, -0.015), Vector3(hw, y1, -0.015), Vector3(-hw, y1, -0.015), Vector3(0, 0, -1))
	b.box(TRIM, Vector3(-0.04, y0, -0.07), Vector3(0.04, y1, -0.015), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(-hw, y1, -0.09), Vector3(hw, y1 + 0.14, -0.015), PackedStringArray(["+z", "-y", "-x", "+x"]))
	b.quad(GLASS, Vector3(-hw, y1 + 0.14, -0.015), Vector3(hw, y1 + 0.14, -0.015), Vector3(hw, y1 + 0.85, -0.015), Vector3(-hw, y1 + 0.85, -0.015), Vector3(0, 0, -1))
	var f := 0.17
	b.box(TRIM, Vector3(-hw - f, y0, -0.15), Vector3(-hw, y1 + 0.85, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(hw, y0, -0.15), Vector3(hw + f, y1 + 0.85, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	b.box(TRIM, Vector3(-hw - f - 0.12, y1 + 0.85, -0.3), Vector3(hw + f + 0.12, y1 + 1.15, 0.0), PackedStringArray(["+z", "-y"]))


func _portico(b) -> void:
	# Colunas (base, fuste com leve afinamento, equino e abaco).
	for cx in COL_XS:
		var x: float = cx
		b.box(TRIM, Vector3(x - 0.58, PODIUM_H, COL_Z - 0.58), Vector3(x + 0.58, PODIUM_H + 0.3, COL_Z + 0.58), PackedStringArray(["-y"]))
		b.frustum(TRIM, Vector3(x, PODIUM_H + 0.3, COL_Z), COL_R, COL_R * 0.86, COL_SHAFT_TOP - PODIUM_H - 0.3, 10, false, false)
		b.frustum(TRIM, Vector3(x, COL_SHAFT_TOP, COL_Z), COL_R * 0.88, 0.58, 0.2, 10, false, false)
		b.box(TRIM, Vector3(x - 0.62, COL_SHAFT_TOP + 0.2, COL_Z - 0.62), Vector3(x + 0.62, ABACUS_TOP, COL_Z + 0.62), PackedStringArray(["-y"]))
	# Entablamento: arquitrave branca, friso de pedra, cornija saliente.
	b.box(TRIM, Vector3(-ENT_X, ABACUS_TOP, ENT_FRONT), Vector3(ENT_X, ARCHITRAVE_TOP, CEN_Z0), PackedStringArray(["-y", "+z"]))
	b.box(STONE, Vector3(-ENT_X + 0.05, ARCHITRAVE_TOP, ENT_FRONT + 0.05), Vector3(ENT_X - 0.05, CEN_WALL_TOP, CEN_Z0), PackedStringArray(["-y", "+z"]))
	b.bevel_box(TRIM, Vector3(-PED_X, CEN_WALL_TOP, PED_FRONT), Vector3(PED_X, CEN_CORNICE_TOP, CEN_Z0 - 0.3), 0.08, false)
	# Frontao: timpano azul recuado, cornijas inclinadas brancas, telhado de ardosia.
	var y0 := CEN_CORNICE_TOP
	b.face(ACCENT, [Vector3(-PED_X, y0, ENT_FRONT), Vector3(PED_X, y0, ENT_FRONT), Vector3(0, PED_APEX, ENT_FRONT)], Vector3(0, 0, -1))
	_raking_cornice(b, y0)
	var zb := -3.02
	var zf := ROOF_FRONT
	b.quad(ROOF, Vector3(-PED_X, y0, zf), Vector3(0, PED_APEX, zf), Vector3(0, PED_APEX, zb), Vector3(-PED_X, y0, zb), Vector3(-0.3, 1, 0))
	b.quad(ROOF, Vector3(PED_X, y0, zf), Vector3(0, PED_APEX, zf), Vector3(0, PED_APEX, zb), Vector3(PED_X, y0, zb), Vector3(0.3, 1, 0))
	# Empena traseira (fecha o volume do frontao junto ao atico).
	b.face(STONE, [Vector3(-PED_X, y0, zb), Vector3(PED_X, y0, zb), Vector3(0, PED_APEX, zb)], Vector3(0, 0, 1))


## Cornijas inclinadas do frontao como uma divisa em "V invertido" extrudada em Z.
func _raking_cornice(b, y0: float) -> void:
	var t := 0.42
	var top0 := y0 - 0.02
	var apex := PED_APEX - 0.02
	var xe := PED_X - 0.04
	var poly := PackedVector2Array([
		Vector2(-xe, top0 + (PED_APEX - y0) * 0.04 / PED_X), Vector2(0, apex), Vector2(xe, top0 + (PED_APEX - y0) * 0.04 / PED_X),
		Vector2(xe, top0 - t), Vector2(0, apex - t), Vector2(-xe, top0 - t),
	])
	# Referencial: x local = x mundo, segunda coord do poligono = altura, y local = -z mundo.
	b.push_basis(Basis(Vector3(1, 0, 0), Vector3(0, 0, -1), Vector3(0, 1, 0)))
	# Frente em RAKE_FRONT (3 cm a frente da cornija horizontal em PED_FRONT): sem faces coplanares.
	b.extrude(TRIM, poly, -(ENT_FRONT + 0.2), -RAKE_FRONT, true, false)
	b.pop()


func _dome(b) -> void:
	var c := Vector3(0, 0, DOME_Z)
	# Atico quadrado de pedra com capeamento branco.
	b.bevel_box(STONE, Vector3(-ATTIC_HALF, CEN_CORNICE_TOP + 0.08, DOME_Z - ATTIC_HALF), Vector3(ATTIC_HALF, ATTIC_TOP, DOME_Z + ATTIC_HALF), 0.08, false)
	b.bevel_box(TRIM, Vector3(-ATTIC_HALF - 0.15, ATTIC_TOP, DOME_Z - ATTIC_HALF - 0.15), Vector3(ATTIC_HALF + 0.15, COPING_TOP, DOME_Z + ATTIC_HALF + 0.15), 0.06, false)
	# Topo do atico em ardosia (evita um grande quadrado branco visto de cima).
	var ai := ATTIC_HALF - 0.12
	b.box(ROOF, Vector3(-ai, COPING_TOP, DOME_Z - ai), Vector3(ai, COPING_TOP + 0.06, DOME_Z + ai), PackedStringArray(["-y"]))
	# Tambor: anel de base, corpo com janelas entre pilastras, cornija.
	var y := COPING_TOP + 0.06
	b.frustum(TRIM, c + Vector3(0, y, 0), DRUM_R + 0.3, DRUM_R + 0.22, 0.22, DOME_SIDES, true, false, DOME_PHASE)
	y += 0.22
	b.cylinder(STONE, c + Vector3(0, y, 0), DRUM_R, DRUM_H, DOME_SIDES, false, false, DOME_PHASE)
	var apothem := DRUM_R * cos(deg_to_rad(180.0 / DOME_SIDES))
	for k in DOME_SIDES:
		var ang := deg_to_rad(DOME_PHASE + 180.0 / DOME_SIDES + 360.0 * k / DOME_SIDES)
		var n := Vector3(cos(ang), 0, sin(ang))
		b.push(_facade_xform(n, c + n * apothem))
		if k % 2 == 0:
			var gy0 := y + 0.2
			var gy1 := y + DRUM_H - 0.16
			b.quad(GLASS, Vector3(-0.3, gy0, -0.012), Vector3(0.3, gy0, -0.012), Vector3(0.3, gy1, -0.012), Vector3(-0.3, gy1, -0.012), Vector3(0, 0, -1))
			b.box(TRIM, Vector3(-0.4, gy0 - 0.1, -0.08), Vector3(0.4, gy0, 0.0), PackedStringArray(["+z", "-y"]))
		else:
			_pilaster(b, 0.0, 0.62, y, y + DRUM_H, 0.14)
		b.pop()
	y += DRUM_H
	b.frustum(TRIM, c + Vector3(0, y, 0), DRUM_R + 0.25, DRUM_R + 0.36, 0.3, DOME_SIDES, true, false, DOME_PHASE)
	y += 0.3
	# Cupula facetada (aneis por angulo de elevacao).
	# Fase 0 na cupula: um vertice no eixo frontal, nervuras simetricas em relacao a X = 0.
	var angs: Array = DOME_ANGS
	var top_y := y
	for i in angs.size() - 1:
		var a0 := deg_to_rad(angs[i])
		var a1 := deg_to_rad(angs[i + 1])
		var ya := y + DOME_R * sin(a0)
		var yb := y + DOME_R * sin(a1)
		b.frustum(DOME, c + Vector3(0, ya, 0), DOME_R * cos(a0), DOME_R * cos(a1), yb - ya, DOME_SIDES, i == angs.size() - 2, false, 0.0)
		top_y = yb
	# Nervuras brancas (8) sobre as arestas da cupula: leitura classica de "domo civico".
	if DOME_RIBS:
		for j in 8:
			var th := deg_to_rad(45.0 * j)
			var dir := Vector3(cos(th), 0, sin(th))
			for i in angs.size() - 1:
				var a0 := deg_to_rad(angs[i])
				var a1 := deg_to_rad(angs[i + 1])
				var p0 := c + dir * (DOME_R * cos(a0) + 0.03) + Vector3(0, y + DOME_R * sin(a0), 0)
				var p1 := c + dir * (DOME_R * cos(a1) + 0.03) + Vector3(0, y + DOME_R * sin(a1), 0)
				b.beam(RIB_SLOT, p0, p1, RIB_T)
	# Lanternim: tambor branco octogonal com aberturas escuras e capa conica patinada,
	# apoiado na abertura plana do topo da cupula (raio ~0,8 m em 75 graus).
	b.cylinder(TRIM, c + Vector3(0, top_y, 0), LANT_R, LANT_H, 8, false, false, 22.5)
	var lap := LANT_R * cos(deg_to_rad(22.5))
	for k in 4:
		var ang := deg_to_rad(90.0 * k)
		var n := Vector3(cos(ang), 0, sin(ang))
		b.push(_facade_xform(n, c + n * lap))
		b.quad(GLASS, Vector3(-0.14, top_y + 0.09, -0.01), Vector3(0.14, top_y + 0.09, -0.01), Vector3(0.14, top_y + LANT_H - 0.09, -0.01), Vector3(-0.14, top_y + LANT_H - 0.09, -0.01), Vector3(0, 0, -1))
		b.pop()
	b.frustum(LANT_CAP_SLOT, c + Vector3(0, top_y + LANT_H, 0), LANT_CAP_R, 0.0, LANT_CAP_H, 8, false, true, 22.5)
	_dome_shell_top = top_y
	_top_y = top_y + LANT_H + LANT_CAP_H
