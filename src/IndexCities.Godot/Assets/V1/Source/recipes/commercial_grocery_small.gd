## Receita original: mercearia pequena de bairro (commercial.grocery_small.a) — amostra V1,
## revisao humana PENDENTE. Somente visual: nao define operador, ramo, estoque, acesso,
## capacidade nem custo. Medidas sao CANDIDATAS para compatibilizacao com a integracao.
##
## O letreiro e um no separado ("Sign") com painel liso + Label3D VAZIO: a integracao
## preenche com o nome da empresa REAL operadora (SPEC 10A/2B). Nenhum nome, logo,
## pictograma de ramo ou mercadoria na estrutura: o mesmo modelo-base pode abrigar outro
## ramo varejista no futuro.
##
## Linguagem comum com a casa (residential.small_house.a): plinto plinth_stone, quinas
## verticais chanfradas, janelas com vidro escuro recuado + moldura/peitoril trim_white.
extends RefCounted

const FAMILY_ID := "commercial.grocery_small.a"
const MATERIAL_PREFIX := "shop_"
const MATERIALS := {
	"shop_wall": {"albedo": "#a9c6a6", "roughness": 0.9},
	# Toldo ocre (candidato, PENDENTE): o verde-azulado #2f8c74 repetia o matiz da cupula da
	# Prefeitura; para quem nao le, cada predio fica com um acento proprio (cupula pátina,
	# telhado ceramico, toldo ocre). A integracao pode trocar por override de superficie.
	"shop_awning": {"albedo": "#d39b35", "roughness": 0.85},
	"shop_sign_blank": {"albedo": "#f7f4ec", "roughness": 0.7},
}

# --- Medidas CANDIDATAS (metros) -------------------------------------------------
const W := 10.0             # corpo em X (fachada)
const D := 12.0             # corpo em Z (profundidade)
const PLINTH_H := 0.2
const PLINTH_OUT := 0.05
const WALL_TOP := 4.6       # nivel da laje (4,4 m de parede sobre o plinto)
const PARAPET_TOP := 5.1    # platibanda de 0,5 m
const PARAPET_T := 0.22     # espessura da platibanda
const COPING_H := 0.1
const CHAMFER := 0.1
# Vitrine/porta (referencial da fachada, x = X do mundo).
const SF_X0 := -4.1         # inicio do caixilho da vitrine
const SF_X1 := 4.1          # fim do caixilho (porta no lado +X)
const SF_TOP := 3.45        # topo do caixilho
const SILL_Y := 0.6         # topo da mureta sob a vitrine
const DOOR_X0 := 2.4
const DOOR_X1 := 4.0
const DOOR_TOP := 2.6
# Toldo.
const AWN_X := 4.7          # meia largura
const AWN_DEPTH := 1.3      # <= 1,6 m (raso o bastante p/ a vitrine aparecer no isometrico)
const AWN_Y_WALL := 3.85
const AWN_Y_EDGE := 3.3
const AWN_VALANCE := 0.3
const AWN_STRIPES := 15
# Letreiro (no separado).
const SIGN_W := 6.0
const SIGN_H := 1.5
const SIGN_FRAME := 0.08
const SIGN_CENTER_Y := 4.95
const SIGN_Z := -D * 0.5 - 0.07   # costas do letreiro (a frente da pingadeira)
const FRONT_W := 7.2        # largura do frontao elevado atras do letreiro
const FRONT_TOP := 6.05     # topo do frontao (capa ate +0,10)
# Porta de servico (lateral +X, perto dos fundos).
const SERVICE_Z := 3.8
const SERVICE_W := 1.3
const SERVICE_H := 2.4


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Anel (friso/cinta) entre dois poligonos XZ correspondentes, centrados na origem.
static func _ring(b, slot: String, outer: PackedVector2Array, inner: PackedVector2Array, y0: float, y1: float) -> void:
	var n := outer.size()
	for i in n:
		var j := (i + 1) % n
		var o0 := outer[i]; var o1 := outer[j]
		var i0 := inner[i]; var i1 := inner[j]
		var mid := (o0 + o1) * 0.5
		b.quad(slot, Vector3(o0.x, y0, o0.y), Vector3(o1.x, y0, o1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3(mid.x, 0, mid.y))
		b.quad(slot, Vector3(i0.x, y1, i0.y), Vector3(i1.x, y1, i1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3.UP)
		b.quad(slot, Vector3(i0.x, y0, i0.y), Vector3(i1.x, y0, i1.y), Vector3(o1.x, y0, o1.y), Vector3(o0.x, y0, o0.y), Vector3.DOWN)


## Janela no referencial local de fachada: parede no plano z = 0, exterior em -Z.
static func _window(b, cx: float, y0: float, w: float, h: float, mullion: bool = true) -> void:
	var f := 0.12
	var dep := 0.12
	var x0 := cx - w * 0.5
	var x1 := cx + w * 0.5
	var y1 := y0 + h
	var sk := PackedStringArray(["+z"])
	b.quad("glass_dark", Vector3(x0, y0, -0.02), Vector3(x1, y0, -0.02), Vector3(x1, y1, -0.02), Vector3(x0, y1, -0.02), Vector3(0, 0, -1))
	b.box("trim_white", Vector3(x0 - f, y1, -dep), Vector3(x1 + f, y1 + f, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f, y0, -dep), Vector3(x0, y1, 0.0), sk)
	b.box("trim_white", Vector3(x1, y0, -dep), Vector3(x1 + f, y1, 0.0), sk)
	b.box("trim_white", Vector3(x0 - f - 0.06, y0 - 0.1, -dep - 0.08), Vector3(x1 + f + 0.06, y0, 0.0), sk)
	if mullion:
		b.box("trim_white", Vector3(cx - 0.035, y0, -0.075), Vector3(cx + 0.035, y1, -0.02), sk)


## Caixilho metalico (barra) no referencial de fachada.
static func _bar(b, x0: float, y0: float, x1: float, y1: float, dep: float = 0.1) -> void:
	b.box("metal_dark", Vector3(x0, y0, -dep), Vector3(x1, y1, 0.0), PackedStringArray(["+z"]))


## Vitrine escura + porta de vidro de duas folhas, caixilhos metal_dark.
static func _storefront(b) -> void:
	var mb := 0.1   # largura das barras
	# Vidros (um plano so, levemente a frente da parede; caixilhos na frente dele).
	b.quad("glass_dark", Vector3(SF_X0, SILL_Y, -0.02), Vector3(DOOR_X0, SILL_Y, -0.02), Vector3(DOOR_X0, SF_TOP, -0.02), Vector3(SF_X0, SF_TOP, -0.02), Vector3(0, 0, -1))
	b.quad("glass_dark", Vector3(DOOR_X0, PLINTH_H, -0.02), Vector3(DOOR_X1, PLINTH_H, -0.02), Vector3(DOOR_X1, SF_TOP, -0.02), Vector3(DOOR_X0, SF_TOP, -0.02), Vector3(0, 0, -1))
	# Quadro externo.
	_bar(b, SF_X0 - mb, SF_TOP, SF_X1 + mb, SF_TOP + mb, 0.12)
	_bar(b, SF_X0 - mb, SILL_Y, SF_X0, SF_TOP, 0.12)
	# Batente externo da porta (cobre de DOOR_X1 ate o fim do quadro, sem fresta de parede).
	_bar(b, DOOR_X1, PLINTH_H, SF_X1 + mb, SF_TOP, 0.12)
	# Travessa horizontal (bandeira) em toda a largura, alinhada ao topo da porta.
	_bar(b, SF_X0, DOOR_TOP, SF_X1, DOOR_TOP + mb)
	# Montantes da vitrine (3 panos).
	var vw := (DOOR_X0 - 0.12 - SF_X0) / 3.0
	for i in 2:
		var x: float = SF_X0 + vw * float(i + 1)
		_bar(b, x - mb * 0.5, SILL_Y, x + mb * 0.5, DOOR_TOP)
		_bar(b, x - mb * 0.5, DOOR_TOP + mb, x + mb * 0.5, SF_TOP)
	# Pilar entre vitrine e porta.
	_bar(b, DOOR_X0 - 0.12, PLINTH_H, DOOR_X0, SF_TOP, 0.12)
	# Porta de vidro: duas folhas com montante central, rodape e puxadores.
	var dm := (DOOR_X0 + DOOR_X1) * 0.5
	_bar(b, dm - 0.05, PLINTH_H, dm + 0.05, DOOR_TOP)
	_bar(b, DOOR_X0, PLINTH_H, DOOR_X1, PLINTH_H + 0.14)
	for sx in [-1.0, 1.0]:
		var px: float = dm + sx * 0.16
		b.box("metal_dark", Vector3(px - 0.025, 1.0, -0.17), Vector3(px + 0.025, 1.7, -0.1), PackedStringArray(["+z"]))
	# Mureta sob a vitrine (revestida em pedra, como o plinto) com peitoril claro.
	b.box("plinth_stone", Vector3(SF_X0 - mb, PLINTH_H, -0.04), Vector3(DOOR_X0 - 0.12, SILL_Y - 0.08, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(SF_X0 - mb - 0.05, SILL_Y - 0.08, -0.2), Vector3(DOOR_X0 - 0.12, SILL_Y, 0.0), PackedStringArray(["+z"]))


## Toldo listrado (shop_awning + trim_white), com bambinela em zigue-zague e abas laterais.
static func _awning(b) -> void:
	var t := 0.04
	var z1 := -AWN_DEPTH
	var yv := AWN_Y_EDGE - AWN_VALANCE
	var sw := (AWN_X * 2.0) / float(AWN_STRIPES)
	for i in AWN_STRIPES:
		var slot := "shop_awning" if i % 2 == 0 else "trim_white"
		var xa := -AWN_X + sw * i
		var xb := xa + sw
		var xm := (xa + xb) * 0.5
		b.quad(slot, Vector3(xa, AWN_Y_WALL, 0.0), Vector3(xb, AWN_Y_WALL, 0.0), Vector3(xb, AWN_Y_EDGE, z1), Vector3(xa, AWN_Y_EDGE, z1), Vector3(0, 1, 0))
		b.quad(slot, Vector3(xa, AWN_Y_WALL - t, 0.0), Vector3(xb, AWN_Y_WALL - t, 0.0), Vector3(xb, AWN_Y_EDGE - t, z1 + t), Vector3(xa, AWN_Y_EDGE - t, z1 + t), Vector3(0, -1, 0))
		var front := [Vector3(xa, AWN_Y_EDGE, z1), Vector3(xb, AWN_Y_EDGE, z1), Vector3(xb, yv, z1), Vector3(xm, yv - 0.12, z1), Vector3(xa, yv, z1)]
		b.face(slot, front, Vector3(0, 0, -1))
		var back := []
		for p in front:
			back.append(Vector3(p.x, minf(p.y, AWN_Y_EDGE - t), z1 + t))
		b.face(slot, back, Vector3(0, 0, 1))
	# Abas laterais triangulares (dupla face).
	for sx in [-1.0, 1.0]:
		var x: float = sx * AWN_X
		var tri := [Vector3(x, AWN_Y_WALL, 0.0), Vector3(x, AWN_Y_EDGE, z1), Vector3(x, yv, z1)]
		b.face("shop_awning", tri, Vector3(sx, 0, 0))
		b.face("shop_awning", tri, Vector3(-sx, 0, 0))
	# Sem maos-francesas visiveis: no isometrico elas apareciam como "riscos" soltos abaixo
	# das abas laterais. A lona fica presa a parede pela borda superior.


func build(kit) -> void:
	var b = kit.builder()
	var hx := W * 0.5
	var hz := D * 0.5

	# --- Plinto, volume, platibanda e laje -----------------------------------------
	b.extrude("plinth_stone", _chamfer_rect(-hx - PLINTH_OUT, -hz - PLINTH_OUT, hx + PLINTH_OUT, hz + PLINTH_OUT, CHAMFER + 0.03), 0.0, PLINTH_H, true, false)
	b.extrude("shop_wall", _chamfer_rect(-hx, -hz, hx, hz, CHAMFER), PLINTH_H, PARAPET_TOP, false, false)
	var ix := hx - PARAPET_T
	var iz := hz - PARAPET_T
	# Faces internas da platibanda (voltadas para a laje).
	b.quad("shop_wall", Vector3(-ix, WALL_TOP, -iz), Vector3(ix, WALL_TOP, -iz), Vector3(ix, PARAPET_TOP, -iz), Vector3(-ix, PARAPET_TOP, -iz), Vector3(0, 0, 1))
	b.quad("shop_wall", Vector3(-ix, WALL_TOP, iz), Vector3(ix, WALL_TOP, iz), Vector3(ix, PARAPET_TOP, iz), Vector3(-ix, PARAPET_TOP, iz), Vector3(0, 0, -1))
	b.quad("shop_wall", Vector3(-ix, WALL_TOP, -iz), Vector3(-ix, WALL_TOP, iz), Vector3(-ix, PARAPET_TOP, iz), Vector3(-ix, PARAPET_TOP, -iz), Vector3(1, 0, 0))
	b.quad("shop_wall", Vector3(ix, WALL_TOP, -iz), Vector3(ix, WALL_TOP, iz), Vector3(ix, PARAPET_TOP, iz), Vector3(ix, PARAPET_TOP, -iz), Vector3(-1, 0, 0))
	# Laje plana.
	b.quad("concrete_dark", Vector3(-ix, WALL_TOP, -iz), Vector3(ix, WALL_TOP, -iz), Vector3(ix, WALL_TOP, iz), Vector3(-ix, WALL_TOP, iz), Vector3.UP)
	# Pingadeira (capa) da platibanda.
	var co := 0.05
	var ci := ix - 0.04
	var czi := iz - 0.04
	var y0 := PARAPET_TOP
	var y1 := PARAPET_TOP + COPING_H
	b.box("trim_white", Vector3(-hx - co, y0, -hz - co), Vector3(hx + co, y1, -czi), PackedStringArray(["-y"]))
	b.box("trim_white", Vector3(-hx - co, y0, czi), Vector3(hx + co, y1, hz + co), PackedStringArray(["-y"]))
	b.box("trim_white", Vector3(-hx - co, y0, -czi), Vector3(-ci, y1, czi), PackedStringArray(["-y", "-z", "+z"]))
	b.box("trim_white", Vector3(ci, y0, -czi), Vector3(hx + co, y1, czi), PackedStringArray(["-y", "-z", "+z"]))
	# Frontao: trecho elevado da platibanda frontal atras do letreiro (silhueta de comercio).
	var fw := FRONT_W * 0.5
	b.box("shop_wall", Vector3(-fw, y1, -hz), Vector3(fw, FRONT_TOP, -iz), PackedStringArray(["-y"]))
	b.box("trim_white", Vector3(-fw - co, FRONT_TOP, -hz - co), Vector3(fw + co, FRONT_TOP + COPING_H, -iz + 0.04), PackedStringArray(["-y"]))
	# Friso no nivel da laje, contornando o volume.
	_ring(b, "trim_white", _chamfer_rect(-hx - 0.06, -hz - 0.06, hx + 0.06, hz + 0.06, CHAMFER + 0.02),
		_chamfer_rect(-hx, -hz, hx, hz, CHAMFER), WALL_TOP - 0.16, WALL_TOP - 0.02)

	# --- Equipamento neutro na laje (caixa de ar-condicionado) -------------------
	var ac := Vector3(-2.2, WALL_TOP, 2.6)
	b.box("metal_dark", ac + Vector3(-0.65, 0.0, -0.5), ac + Vector3(0.65, 0.12, 0.5), PackedStringArray(["-y"]))
	b.box("trim_white", ac + Vector3(-0.6, 0.12, -0.45), ac + Vector3(0.6, 0.82, 0.45), PackedStringArray(["-y"]))
	b.cylinder("metal_dark", ac + Vector3(0.0, 0.82, 0.0), 0.3, 0.03, 8, true, false, 22.5)

	# --- Fachada frontal (-Z) --------------------------------------------------------
	b.push_translate(Vector3(0, 0, -hz))
	_storefront(b)
	_awning(b)
	b.pop()
	# Soleira/degrau baixo diante da porta de vidro.
	b.box("plinth_stone", Vector3(DOOR_X0 - 0.1, 0.0, -hz - PLINTH_OUT - 0.5), Vector3(DOOR_X1 + 0.1, 0.1, -hz - PLINTH_OUT), PackedStringArray(["-y", "+z"]))

	# --- Lateral +X: porta de servico lisa + duas janelas altas ------------------------
	b.push_rotate_y(-90.0, Vector3(hx, 0, 0))   # local x = +Z do mundo
	var s0 := SERVICE_Z - SERVICE_W * 0.5
	var s1 := SERVICE_Z + SERVICE_W * 0.5
	var sy1 := PLINTH_H + SERVICE_H
	b.box("concrete_dark", Vector3(s0, PLINTH_H, -0.05), Vector3(s1, sy1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(s0 - 0.12, sy1, -0.12), Vector3(s1 + 0.12, sy1 + 0.12, 0.0), PackedStringArray(["+z"]))
	b.box("trim_white", Vector3(s0 - 0.12, PLINTH_H, -0.12), Vector3(s0, sy1, 0.0), PackedStringArray(["+z", "-y"]))
	b.box("trim_white", Vector3(s1, PLINTH_H, -0.12), Vector3(s1 + 0.12, sy1, 0.0), PackedStringArray(["+z", "-y"]))
	_window(b, -2.6, 2.5, 2.2, 0.9)
	_window(b, 0.9, 2.5, 2.2, 0.9)
	b.pop()
	# --- Lateral -X: duas janelas altas ---------------------------------------------
	b.push_rotate_y(90.0, Vector3(-hx, 0, 0))   # local x = -Z do mundo
	_window(b, 2.6, 2.5, 2.2, 0.9)
	_window(b, -2.4, 2.5, 2.2, 0.9)
	b.pop()
	# --- Fundos (+Z): janela pequena ---------------------------------------------------
	b.push_rotate_y(180.0, Vector3(0, 0, hz))   # local x = -X do mundo
	_window(b, 2.4, 2.2, 1.0, 0.8, false)
	b.pop()

	print("[grocery] triangulos Body no builder: ", b.triangle_count())
	var mesh = kit.save_mesh(b, FAMILY_ID)

	# --- Painel do letreiro (malha propria, no separado) ----------------------------
	var sb = kit.builder()
	var shw := SIGN_W * 0.5
	var shh := SIGN_H * 0.5
	var fr := SIGN_FRAME
	var pf := -0.16    # face do painel
	var ff := -0.2     # face da moldura
	sb.box("shop_sign_blank", Vector3(-shw, -shh, pf), Vector3(shw, shh, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, shh, ff), Vector3(shw + fr, shh + fr, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, -shh - fr, ff), Vector3(shw + fr, -shh, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, -shh, ff), Vector3(-shw, shh, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	sb.box("metal_dark", Vector3(shw, -shh, ff), Vector3(shw + fr, shh, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	sb.quad("metal_dark", Vector3(-shw - fr, -shh - fr, 0.0), Vector3(shw + fr, -shh - fr, 0.0), Vector3(shw + fr, shh + fr, 0.0), Vector3(-shw - fr, shh + fr, 0.0), Vector3(0, 0, 1))
	var sign_mesh = kit.save_mesh(sb, FAMILY_ID + ".sign_panel")

	var root = kit.new_root("GrocerySmall", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var sign = kit.add_node(root, "Sign", Transform3D(Basis.IDENTITY, Vector3(0.0, SIGN_CENTER_Y, SIGN_Z)))
	sign.set_meta("replaceable", true)
	sign.set_meta("panel_size_m", Vector2(SIGN_W, SIGN_H))
	sign.set_meta("note", "letreiro substituivel; SignText e preenchido pela integracao com o nome da empresa REAL operadora")
	kit.add_mesh(sign, "SignPanel", sign_mesh)
	var lbl := Label3D.new()
	lbl.name = "SignText"
	lbl.text = ""
	lbl.font_size = 48
	lbl.pixel_size = 0.01
	lbl.outline_size = 0
	lbl.modulate = Color("#2b3136")
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	lbl.width = (SIGN_W - 0.4) / lbl.pixel_size
	lbl.double_sided = false
	# Label3D le-se a partir de +Z local; girar 180 graus para ficar voltado a -Z.
	lbl.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(0.0, 0.0, pf - 0.012))
	sign.add_child(lbl)

	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3((DOOR_X0 + DOOR_X1) * 0.5, 0.0, -hz - PLINTH_OUT - 0.5 - 0.45), Vector3.FORWARD,
		"entrada de pedestre candidata diante da porta de vidro; acesso real e da integracao")
	kit.add_marker(root, "Candidate_CargoPoint", Vector3(hx + PLINTH_OUT + 1.0, 0.0, SERVICE_Z), Vector3.RIGHT,
		"ponto de carga candidato; acesso real e da integracao")
	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Mercearia pequena",
		"proposed": {
			"corpo_x_m": W,
			"corpo_z_m": D,
			"plinto_m": PLINTH_H,
			"parede_sobre_plinto_m": WALL_TOP - PLINTH_H,
			"platibanda_m": PARAPET_TOP - WALL_TOP,
			"topo_platibanda_com_capa_m": PARAPET_TOP + COPING_H,
			"topo_letreiro_m": SIGN_CENTER_Y + SIGN_H * 0.5 + SIGN_FRAME,
			"letreiro_l_x_a_m": [SIGN_W + 2.0 * SIGN_FRAME, SIGN_H + 2.0 * SIGN_FRAME],
			"toldo_profundidade_m": AWN_DEPTH,
			"toldo_largura_m": AWN_X * 2.0,
			"porta_servico_l_x_a_m": [SERVICE_W, SERVICE_H],
		},
		"parts": {
			"Body": "estrutura (7 superficies = slots): plinth_stone (plinto, mureta, soleira), shop_wall (paredes, platibanda, frontao), concrete_dark (laje e porta de servico), trim_white (capas, friso, molduras, listras claras do toldo, caixa do ar-condicionado), metal_dark (caixilhos, base do ar-condicionado), glass_dark (vitrine, porta de vidro, janelas), shop_awning (listras de cor e abas do toldo)",
			"Sign": "Node3D substituivel/removivel separado da estrutura (pode ser trocado inteiro pela integracao)",
			"Sign/SignPanel": "painel liso shop_sign_blank + moldura/costas metal_dark (malha commercial.grocery_small.a.sign_panel)",
			"Sign/SignText": "Label3D VAZIO, centralizado no painel, voltado a -Z; preencher com o nome da empresa REAL operadora",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta de vidro, forward -Z",
			"Candidate_CargoPoint": "Marker3D candidato fora da porta de servico (lateral +X), forward +X",
		},
		"notes": [
			"Pivo no solo, centrado no corpo 10 x 12 m. Envelope excede o corpo em -Z pelo toldo (1,3 m a partir da parede; borda em z = -7,3) e em +-X/+-Z pelo plinto (0,05 m) e capas (0,05 m).",
			"SignText: font_size 48, pixel_size 0,01 (altura de corpo ~0,48 m), largura util 5,6 m com quebra de palavra: cabem ~2 linhas de ~18 caracteres no painel 6,0 x 1,5 m (verificado com texto de teste fora do repo). Nomes maiores: a integracao reduz pixel_size.",
			"Sem o no Sign o frontao fica liso e a estrutura continua visualmente completa (o letreiro nao e parte da malha Body).",
			"Toldo em superficie propria shop_awning (listras = shop_awning + trim_white): a integracao PODERA trocar a cor do toldo por override de superficie caso a identidade do operador use isso (NAO decidido).",
			"Sem nome, logo, emblema, pictograma de ramo, produtos, carrinhos ou paletes: modelo-base reutilizavel por outro ramo varejista.",
			"Portas sempre fechadas; detalhes desenhados nao sao acessos funcionais (somente os Marker3D candidatos).",
		],
	})
