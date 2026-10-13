## Receita original: posto de combustivel (commercial.fuel_station.a) — amostra V1 (segunda
## onda), revisao humana PENDENTE. Somente visual: nao define operador, produtos, estoque,
## capacidade, filas, acesso real nem custo. Medidas sao CANDIDATAS para compatibilizacao.
##
## SPEC: o posto recebe Combustivel importado fisicamente, armazena e abastece veiculos quando
## houver recurso e acesso. O modelo mostra so a estrutura permanente: cobertura sobre 4
## pilares, 2 ilhas com 2 bombas cada (visor LISO, mangueiras recolhidas), loja/caixa simples
## nos fundos, laje do tanque subterraneo com tampas e respiros, patio pavimentado com dois
## rebaixos de meio-fio (entrada/saida) na borda -Z. Sem oficina, lava-rapido, conveniencia
## ampliada, precos, logos, nomes, veiculos, pessoas ou luzes acesas.
##
## O nivel de combustivel NAO e mostrado (tanque enterrado): nao ha no State_* nesta familia.
## O letreiro e um no separado ("Sign") com painel liso + Label3D VAZIO na testeira da loja:
## a integracao preenche com o nome da empresa REAL operadora. A testeira da cobertura e lisa.
##
## Pivo = centro do lote 24 x 24 m no solo; fachada/acessos voltados a -Z.
extends RefCounted

const FAMILY_ID := "commercial.fuel_station.a"
const MATERIAL_PREFIX := "fuel_"
const MATERIALS := {
	# Acento da familia (testeira da cobertura, cabecas das bombas, faixa da loja). Vermelho
	# carmim: matiz distinto do telhado ceramico da casa, do toldo ocre da mercearia e do
	# azul/patina da Prefeitura (candidato, PENDENTE; trocavel por override de superficie).
	"fuel_accent": {"albedo": "#b8343f", "roughness": 0.72},
	# Creme areia: paredes da loja e painel superior da cobertura (um tom acima do piso
	# `concrete` sob a cobertura, para os dois nao se fundirem na planta/isometrico).
	"fuel_wall": {"albedo": "#d9cdb8", "roughness": 0.9},
}

# --- Lote e piso (metros, CANDIDATOS) ---------------------------------------------
const LOT := Vector2(24.0, 24.0)
const HX := 12.0
const HZ := 12.0
const PAVE_Y := 0.04          # topo do asfalto do patio
const PAD_Y := 0.08           # topo do piso de concreto sob a cobertura
# Meio-fio frontal com dois rebaixos (entrada a -X, saida a +X: trafego pela direita).
const CURB_Z1 := -11.5            # meio-fio frontal de 0,5 m
const CURB_TOP := 0.18        # mesma cota da calcada da rua da amostra
const CUT_TOP := 0.05         # ponta baixa das rampas do rebaixo (asfalto a 0,04)
const CUT_HALF := 1.8         # meia largura livre de cada rebaixo
const CUT_X := 9.4            # centro dos rebaixos em |x|
const WING := 0.5             # rampa lateral de cada rebaixo
const SIDE_CURB_W := 0.3      # guia baixa nas bordas +-X e +Z
const SIDE_CURB_TOP := 0.15
# Ilhas e bombas.
const ISLAND_Z := [-4.2, 0.8] # centro das ilhas (paralelas a X)
const ISLAND_HX := 5.6
const ISLAND_HZ := 0.6
const ISLAND_H := 0.18
const PUMP_X := 2.4           # bombas em x = +-2,4 (4,8 m entre paradas)
const PILLAR_X := 4.8
const PILLAR_HALF := 0.24
const STOP_OFFSET := 1.5      # da face da ilha ao eixo do veiculo parado
# Cobertura.
const CAN_X0 := -7.4
const CAN_X1 := 7.4
const CAN_Z0 := -6.4
const CAN_Z1 := 3.0
const CAN_Y0 := 5.0           # altura livre sob a cobertura
const CAN_Y1 := 6.0           # topo da testeira
const CAN_RIM := 0.32         # borda branca visivel em volta do painel superior
const CAN_CHAMFER := 0.15
const CAN_TOP := 6.06         # topo do painel superior da cobertura
const CANOPY_TOP_SLOT := "fuel_wall"
const PAD_X := 7.8
const PAD_Z0 := -6.8
const PAD_Z1 := 3.4
# Loja / caixa.
const SHOP_X0 := -6.6
const SHOP_X1 := 2.4
const SHOP_Z0 := 7.0
const SHOP_Z1 := 11.6
const SHOP_FLOOR := 0.2
const SHOP_WALL_TOP := 3.2
const BAND_Y0 := 3.15
const BAND_Y1 := 4.35
const BAND_OUT := 0.06
const DECK_Y := 4.1
const COPING_H := 0.12
const AC_X := -4.4            # caixa de ar-condicionado na laje (lado -X, fundos)
const AC_Z := 9.9
const APRON_Z0 := 6.0
const WIN_X0 := -6.0
const WIN_X1 := -1.0
const WIN_Y0 := 0.75
const SF_TOP := 2.6
const DOOR_X0 := -0.3
const DOOR_X1 := 1.5
const DOOR_TOP := 2.4
# Letreiro (no separado) na testeira da loja.
const SIGN_W := 5.4
const SIGN_H := 0.9
const SIGN_FRAME := 0.06
const SIGN_CENTER_Y := 3.75
# Tanque subterraneo (laje + tampas + respiros) no canto fundos/+X.
const TANK_X0 := 7.6
const TANK_X1 := 11.6
const TANK_Z0 := 7.0
const TANK_Z1 := 11.6
const TANK_TOP := 0.13
const CAP_Z := 7.8
const CAP_XS := [8.5, 9.6, 10.7]
const VENT_X := 11.15
const VENT_ZS := [10.45, 10.8, 11.15]
const VENT_TOP := 4.0


static func _chamfer_rect(x0: float, z0: float, x1: float, z1: float, c: float) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2(x0 + c, z0), Vector2(x1 - c, z0), Vector2(x1, z0 + c), Vector2(x1, z1 - c),
		Vector2(x1 - c, z1), Vector2(x0 + c, z1), Vector2(x0, z1 - c), Vector2(x0, z0 + c),
	])


## Anel (friso/faixa) entre dois poligonos XZ correspondentes. A face externa aponta para
## fora a partir do CENTROIDE do poligono (a loja nao fica centrada no pivo).
static func _ring(b, slot: String, outer: PackedVector2Array, inner: PackedVector2Array, y0: float, y1: float) -> void:
	var n := outer.size()
	var cen := Vector2.ZERO
	for p in outer:
		cen += p
	cen /= float(n)
	for i in n:
		var j := (i + 1) % n
		var o0 := outer[i]; var o1 := outer[j]
		var i0 := inner[i]; var i1 := inner[j]
		var mid := (o0 + o1) * 0.5 - cen
		b.quad(slot, Vector3(o0.x, y0, o0.y), Vector3(o1.x, y0, o1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3(mid.x, 0, mid.y))
		b.quad(slot, Vector3(i0.x, y1, i0.y), Vector3(i1.x, y1, i1.y), Vector3(o1.x, y1, o1.y), Vector3(o0.x, y1, o0.y), Vector3.UP)
		b.quad(slot, Vector3(i0.x, y0, i0.y), Vector3(i1.x, y0, i1.y), Vector3(o1.x, y0, o1.y), Vector3(o0.x, y0, o0.y), Vector3.DOWN)


## Rampa (cunha) de meio-fio ao longo de X: topo inclinado de y_a (em xa) a y_b (em xb).
static func _wedge(b, slot: String, xa: float, xb: float, z0: float, z1: float, y_base: float, y_a: float, y_b: float) -> void:
	var sx := signf(xb - xa)
	b.quad(slot, Vector3(xa, y_a, z0), Vector3(xb, y_b, z0), Vector3(xb, y_b, z1), Vector3(xa, y_a, z1), Vector3.UP)
	b.face(slot, [Vector3(xa, y_base, z0), Vector3(xb, y_base, z0), Vector3(xb, y_b, z0), Vector3(xa, y_a, z0)], Vector3(0, 0, -1))
	b.face(slot, [Vector3(xa, y_base, z1), Vector3(xb, y_base, z1), Vector3(xb, y_b, z1), Vector3(xa, y_a, z1)], Vector3(0, 0, 1))
	b.quad(slot, Vector3(xb, y_base, z0), Vector3(xb, y_base, z1), Vector3(xb, y_b, z1), Vector3(xb, y_b, z0), Vector3(sx, 0, 0))
	if y_a - y_base > 0.005:
		b.quad(slot, Vector3(xa, y_base, z0), Vector3(xa, y_base, z1), Vector3(xa, y_a, z1), Vector3(xa, y_a, z0), Vector3(-sx, 0, 0))


## Janela no referencial local de fachada: parede no plano z = 0, exterior em -Z.
static func _window(b, cx: float, y0: float, w: float, h: float) -> void:
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


## Caixilho metalico (barra) no referencial de fachada.
static func _bar(b, x0: float, y0: float, x1: float, y1: float, dep: float = 0.1) -> void:
	b.box("metal_dark", Vector3(x0, y0, -dep), Vector3(x1, y1, 0.0), PackedStringArray(["+z"]))


# ---------------------------------------------------------------------------
# Partes
# ---------------------------------------------------------------------------

static func _ground(b) -> void:
	# Patio de asfalto em todo o lote (ocupa o lote: escala do host ~1).
	b.box("road_asphalt", Vector3(-HX, 0.0, -HZ), Vector3(HX, PAVE_Y, HZ), PackedStringArray(["-y"]))
	# Piso de concreto sob a cobertura (abastecimento).
	b.box("concrete", Vector3(-PAD_X, PAVE_Y, PAD_Z0), Vector3(PAD_X, PAD_Y, PAD_Z1), PackedStringArray(["-y"]))
	# Guia baixa de acabamento nas bordas +-X e +Z (o patio so abre nos dois rebaixos da frente).
	var sk := PackedStringArray(["-y"])
	var e := HX - SIDE_CURB_W
	b.box("concrete", Vector3(-HX, PAVE_Y, CURB_Z1), Vector3(-e, SIDE_CURB_TOP, HZ), sk)
	b.box("concrete", Vector3(e, PAVE_Y, CURB_Z1), Vector3(HX, SIDE_CURB_TOP, HZ), sk)
	b.box("concrete", Vector3(-e, PAVE_Y, HZ - SIDE_CURB_W), Vector3(e, SIDE_CURB_TOP, HZ), sk)


static func _front_curb(b) -> void:
	var z0 := -HZ
	var z1 := CURB_Z1
	var sk := PackedStringArray(["-y"])
	# Trechos altos: cantos e trecho central entre os rebaixos.
	var xo := CUT_X + CUT_HALF + WING      # borda externa da rampa externa
	var xi := CUT_X - CUT_HALF - WING      # borda interna da rampa interna
	for sx in [-1.0, 1.0]:
		var a: float = sx * xo
		var e: float = sx * HX
		b.box("concrete", Vector3(minf(a, e), PAVE_Y, z0), Vector3(maxf(a, e), CURB_TOP, z1), sk)
	b.box("concrete", Vector3(-xi, PAVE_Y, z0), Vector3(xi, CURB_TOP, z1), sk)
	# Rebaixos: rampas laterais; no vao o asfalto do patio segue ate a borda do lote
	# (o meio-fio claro fica visivelmente interrompido = entrada/saida, sem setas).
	for sx in [-1.0, 1.0]:
		var c0: float = sx * (CUT_X - CUT_HALF)
		var c1: float = sx * (CUT_X + CUT_HALF)
		_wedge(b, "concrete", c0, sx * xi, z0, z1, PAVE_Y, CUT_TOP, CURB_TOP)
		_wedge(b, "concrete", c1, sx * xo, z0, z1, PAVE_Y, CUT_TOP, CURB_TOP)


## Bomba (dispenser) com visor liso nas duas faces e mangueiras recolhidas na face -Z.
static func _pump(b, cx: float, cz: float, y0: float) -> void:
	var hx := 0.55
	var hz := 0.28
	var body_top := y0 + 1.95
	var head_top := y0 + 2.3
	b.box("metal_dark", Vector3(cx - hx - 0.07, y0, cz - hz - 0.06), Vector3(cx + hx + 0.07, y0 + 0.12, cz + hz + 0.06), PackedStringArray(["-y"]))
	b.bevel_box("trim_white", Vector3(cx - hx, y0 + 0.12, cz - hz), Vector3(cx + hx, body_top, cz + hz), 0.05, false)
	b.bevel_box("fuel_accent", Vector3(cx - hx - 0.04, body_top, cz - hz - 0.04), Vector3(cx + hx + 0.04, head_top, cz + hz + 0.04), 0.05, true)
	# Visores LISOS (sem numeros), um em cada face longa.
	for s in [-1.0, 1.0]:
		var zf: float = cz + s * (hz + 0.012)
		b.quad("glass_dark", Vector3(cx - 0.36, y0 + 1.28, zf), Vector3(cx + 0.36, y0 + 1.28, zf), Vector3(cx + 0.36, y0 + 1.76, zf), Vector3(cx - 0.36, y0 + 1.76, zf), Vector3(0, 0, s))
	# Mangueiras recolhidas: saem sob a cabeca, fazem uma alca e terminam no bico guardado.
	var zh := cz - hz - 0.07
	for sx in [-1.0, 1.0]:
		var top := Vector3(cx + sx * 0.47, body_top - 0.02, zh)
		var low := Vector3(cx + sx * 0.62, y0 + 0.62, zh - 0.02)
		var noz := Vector3(cx + sx * 0.3, y0 + 1.0, zh)
		b.beam("metal_dark", top, low, 0.065)
		b.beam("metal_dark", low, noz, 0.065)
		b.box("metal_dark", Vector3(cx + sx * 0.3 - 0.07, y0 + 0.92, cz - hz - 0.12), Vector3(cx + sx * 0.3 + 0.07, y0 + 1.18, cz - hz), PackedStringArray(["+z"]))


## Contorno XZ da ilha com pontas arredondadas (meio-octogono): assinatura classica de
## ilha de bombas, legivel no isometrico sem depender de texto.
static func _island_poly(zc: float) -> PackedVector2Array:
	var r := ISLAND_HZ
	var pts := PackedVector2Array()
	for k in 5:   # ponta +X: de -90 a +90 graus
		var a := deg_to_rad(-90.0 + 45.0 * k)
		pts.append(Vector2(ISLAND_HX - r + cos(a) * r, zc + sin(a) * r))
	for k in 5:   # ponta -X: de 90 a 270 graus
		var a := deg_to_rad(90.0 + 45.0 * k)
		pts.append(Vector2(-ISLAND_HX + r + cos(a) * r, zc + sin(a) * r))
	return pts


static func _islands(b) -> void:
	var it := PAD_Y + ISLAND_H
	for zc in ISLAND_Z:
		b.extrude("trim_white", _island_poly(zc), PAD_Y, it, true, false)
		for sx in [-1.0, 1.0]:
			_pump(b, sx * PUMP_X, zc, it)
			# Pilar da cobertura com protecao de base no acento.
			var px: float = sx * PILLAR_X
			var p := PILLAR_HALF
			b.bevel_box("fuel_accent", Vector3(px - p - 0.04, it, zc - p - 0.04), Vector3(px + p + 0.04, it + 1.0, zc + p + 0.04), 0.04, false)
			b.box("trim_white", Vector3(px - p, it + 1.0, zc - p), Vector3(px + p, CAN_Y0, zc + p), PackedStringArray(["-y", "+y"]))


static func _canopy(b) -> void:
	var lo := CAN_Y0 + 0.12
	var hi := CAN_Y1 - 0.1
	# Quinas verticais chanfradas (mesma assinatura da loja/mercearia).
	var rect := _chamfer_rect(CAN_X0, CAN_Z0, CAN_X1, CAN_Z1, CAN_CHAMFER)
	# Forro (face inferior) + filete branco inferior.
	b.extrude("trim_white", rect, CAN_Y0, lo, false, true)
	# Testeira lisa no acento (sem marca/texto).
	b.extrude("fuel_accent", rect, lo, hi, false, false)
	# Capa superior (pingadeira) + cobertura.
	var o := 0.04
	b.extrude("trim_white", _chamfer_rect(CAN_X0 - o, CAN_Z0 - o, CAN_X1 + o, CAN_Z1 + o, CAN_CHAMFER + 0.02), hi, CAN_Y1 + 0.02, true, true)
	# Painel de cobertura creme dentro da borda branca (o branco puro estourava no isometrico;
	# em `concrete` ele tinha o MESMO tom do piso sob a cobertura e os dois se fundiam).
	var r := CAN_RIM
	b.box(CANOPY_TOP_SLOT, Vector3(CAN_X0 + r, CAN_Y1 + 0.02, CAN_Z0 + r), Vector3(CAN_X1 - r, CAN_TOP, CAN_Z1 - r), PackedStringArray(["-y"]))


static func _shop(b) -> void:
	# Plataforma/calcada da loja (piso + passeio frontal).
	b.box("concrete", Vector3(SHOP_X0 - 0.5, PAVE_Y, APRON_Z0), Vector3(SHOP_X1 + 0.5, SHOP_FLOOR, SHOP_Z1), PackedStringArray(["-y"]))
	var wall := _chamfer_rect(SHOP_X0, SHOP_Z0, SHOP_X1, SHOP_Z1, 0.1)
	b.extrude("fuel_wall", wall, SHOP_FLOOR, SHOP_WALL_TOP, false, false)
	# Faixa (testeira) no acento em volta da loja, saliente 6 cm.
	var band := _chamfer_rect(SHOP_X0 - BAND_OUT, SHOP_Z0 - BAND_OUT, SHOP_X1 + BAND_OUT, SHOP_Z1 + BAND_OUT, 0.12)
	_ring(b, "fuel_accent", band, wall, BAND_Y0, BAND_Y1)
	# Platibanda: capa branca + faces internas + laje escura rebaixada.
	var cap_o := _chamfer_rect(SHOP_X0 - 0.1, SHOP_Z0 - 0.1, SHOP_X1 + 0.1, SHOP_Z1 + 0.1, 0.14)
	var cap_i := _chamfer_rect(SHOP_X0 + 0.22, SHOP_Z0 + 0.22, SHOP_X1 - 0.22, SHOP_Z1 - 0.22, 0.04)
	_ring(b, "trim_white", cap_o, cap_i, BAND_Y1, BAND_Y1 + COPING_H)
	var ix0 := SHOP_X0 + 0.22; var ix1 := SHOP_X1 - 0.22
	var iz0 := SHOP_Z0 + 0.22; var iz1 := SHOP_Z1 - 0.22
	b.quad("fuel_wall", Vector3(ix0, DECK_Y, iz0), Vector3(ix1, DECK_Y, iz0), Vector3(ix1, BAND_Y1, iz0), Vector3(ix0, BAND_Y1, iz0), Vector3(0, 0, 1))
	b.quad("fuel_wall", Vector3(ix0, DECK_Y, iz1), Vector3(ix1, DECK_Y, iz1), Vector3(ix1, BAND_Y1, iz1), Vector3(ix0, BAND_Y1, iz1), Vector3(0, 0, -1))
	b.quad("fuel_wall", Vector3(ix0, DECK_Y, iz0), Vector3(ix0, DECK_Y, iz1), Vector3(ix0, BAND_Y1, iz1), Vector3(ix0, BAND_Y1, iz0), Vector3(1, 0, 0))
	b.quad("fuel_wall", Vector3(ix1, DECK_Y, iz0), Vector3(ix1, DECK_Y, iz1), Vector3(ix1, BAND_Y1, iz1), Vector3(ix1, BAND_Y1, iz0), Vector3(-1, 0, 0))
	b.quad("road_asphalt", Vector3(ix0, DECK_Y, iz0), Vector3(ix1, DECK_Y, iz0), Vector3(ix1, DECK_Y, iz1), Vector3(ix0, DECK_Y, iz1), Vector3.UP)
	# Equipamento neutro na laje (caixa de ar-condicionado, mesma linguagem da mercearia):
	# quebra a laje escura lisa no isometrico. Desligado/sem estado.
	var ac := Vector3(AC_X, DECK_Y, AC_Z)
	b.box("metal_dark", ac + Vector3(-0.7, 0.0, -0.5), ac + Vector3(0.7, 0.12, 0.5), PackedStringArray(["-y"]))
	b.bevel_box("trim_white", ac + Vector3(-0.62, 0.12, -0.42), ac + Vector3(0.62, 0.86, 0.42), 0.04, false)
	b.cylinder("metal_dark", ac + Vector3(0.0, 0.86, 0.0), 0.3, 0.03, 8, true, false, 22.5)

	# --- Fachada -Z: vitrine escura + porta de vidro fechada --------------------------
	b.push_translate(Vector3(0, 0, SHOP_Z0))
	var mb := 0.1
	b.quad("glass_dark", Vector3(WIN_X0, WIN_Y0, -0.02), Vector3(WIN_X1, WIN_Y0, -0.02), Vector3(WIN_X1, SF_TOP, -0.02), Vector3(WIN_X0, SF_TOP, -0.02), Vector3(0, 0, -1))
	b.quad("glass_dark", Vector3(DOOR_X0, SHOP_FLOOR, -0.02), Vector3(DOOR_X1, SHOP_FLOOR, -0.02), Vector3(DOOR_X1, SF_TOP, -0.02), Vector3(DOOR_X0, SF_TOP, -0.02), Vector3(0, 0, -1))
	# Quadro da vitrine.
	_bar(b, WIN_X0 - mb, SF_TOP, WIN_X1 + mb, SF_TOP + mb, 0.12)
	_bar(b, WIN_X0 - mb, WIN_Y0, WIN_X0, SF_TOP, 0.12)
	_bar(b, WIN_X1, WIN_Y0, WIN_X1 + mb, SF_TOP, 0.12)
	for k in 2:
		var x: float = WIN_X0 + (WIN_X1 - WIN_X0) * float(k + 1) / 3.0
		_bar(b, x - mb * 0.5, WIN_Y0, x + mb * 0.5, SF_TOP)
	# Peitoril claro sob a vitrine.
	b.box("trim_white", Vector3(WIN_X0 - mb - 0.05, WIN_Y0 - 0.08, -0.18), Vector3(WIN_X1 + mb + 0.05, WIN_Y0, 0.0), PackedStringArray(["+z"]))
	# Porta: batentes, bandeira, montante central, rodape e puxadores.
	_bar(b, DOOR_X0 - mb, SHOP_FLOOR, DOOR_X0, SF_TOP + mb, 0.12)
	_bar(b, DOOR_X1, SHOP_FLOOR, DOOR_X1 + mb, SF_TOP + mb, 0.12)
	_bar(b, DOOR_X0, SF_TOP, DOOR_X1, SF_TOP + mb, 0.12)
	_bar(b, DOOR_X0, DOOR_TOP, DOOR_X1, DOOR_TOP + mb)
	var dm := (DOOR_X0 + DOOR_X1) * 0.5
	_bar(b, dm - 0.05, SHOP_FLOOR, dm + 0.05, DOOR_TOP)
	_bar(b, DOOR_X0, SHOP_FLOOR, DOOR_X1, SHOP_FLOOR + 0.14)
	for sx in [-1.0, 1.0]:
		var px: float = dm + sx * 0.16
		b.box("metal_dark", Vector3(px - 0.025, 1.0, -0.17), Vector3(px + 0.025, 1.7, -0.1), PackedStringArray(["+z"]))
	b.pop()

	# --- Laterais: uma janela em cada (fundos lisos) ----------------------------------
	var zc := (SHOP_Z0 + SHOP_Z1) * 0.5
	b.push_rotate_y(-90.0, Vector3(SHOP_X1, 0, 0))   # local x = +Z do mundo, exterior +X
	_window(b, zc, 1.3, 1.8, 1.1)
	b.pop()
	b.push_rotate_y(90.0, Vector3(SHOP_X0, 0, 0))    # local x = -Z do mundo, exterior -X
	_window(b, -zc, 1.3, 1.8, 1.1)
	b.pop()
	# Fundos (+Z): uma janela alta pequena (fachada cega muito grande na vista de tras).
	b.push_rotate_y(180.0, Vector3(0, 0, SHOP_Z1))   # local x = -X do mundo, exterior +Z
	_window(b, -(SHOP_X0 + SHOP_X1) * 0.5 + 1.8, 1.9, 1.4, 0.7)
	b.pop()


static func _tank(b) -> void:
	# Laje sobre o tanque subterraneo (vazio/cheio nao e mostrado: estado real do host).
	b.bevel_box("concrete", Vector3(TANK_X0, PAVE_Y, TANK_Z0), Vector3(TANK_X1, TANK_TOP, TANK_Z1), 0.03, false)
	for x in CAP_XS:
		b.cylinder("trim_white", Vector3(x, TANK_TOP, CAP_Z), 0.44, 0.015, 10, true, false)
		b.cylinder("metal_dark", Vector3(x, TANK_TOP, CAP_Z), 0.3, 0.05, 10, true, false)
	# Respiros verticais finos com terminal e uma abracadeira comum.
	for z in VENT_ZS:
		b.cylinder("trim_white", Vector3(VENT_X, TANK_TOP, z), 0.055, VENT_TOP - TANK_TOP, 6, false, false)
		b.cylinder("metal_dark", Vector3(VENT_X, VENT_TOP, z), 0.1, 0.12, 6, true, true)
	b.box("metal_dark", Vector3(VENT_X - 0.05, 2.5, VENT_ZS[0] - 0.08), Vector3(VENT_X + 0.05, 2.6, VENT_ZS[2] + 0.08))
	b.box("metal_dark", Vector3(VENT_X - 0.09, TANK_TOP, VENT_ZS[0] - 0.12), Vector3(VENT_X + 0.09, TANK_TOP + 0.25, VENT_ZS[2] + 0.12), PackedStringArray(["-y"]))


# ---------------------------------------------------------------------------
# Construcao
# ---------------------------------------------------------------------------

func build(kit) -> void:
	var b = kit.builder()
	_ground(b)
	_front_curb(b)
	_islands(b)
	_canopy(b)
	_shop(b)
	_tank(b)
	print("[fuel] triangulos Body no builder: ", b.triangle_count(), " | slots: ", b.slot_names())
	var mesh = kit.save_mesh(b, FAMILY_ID)

	# --- Painel do letreiro (malha propria, no separado) ----------------------------
	var sb = kit.builder()
	var shw := SIGN_W * 0.5
	var shh := SIGN_H * 0.5
	var fr := SIGN_FRAME
	var pf := -0.14    # face do painel
	var ff := -0.18    # face da moldura
	sb.box("shop_sign_blank", Vector3(-shw, -shh, pf), Vector3(shw, shh, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, shh, ff), Vector3(shw + fr, shh + fr, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, -shh - fr, ff), Vector3(shw + fr, -shh, 0.0), PackedStringArray(["+z"]))
	sb.box("metal_dark", Vector3(-shw - fr, -shh, ff), Vector3(-shw, shh, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	sb.box("metal_dark", Vector3(shw, -shh, ff), Vector3(shw + fr, shh, 0.0), PackedStringArray(["+z", "-y", "+y"]))
	var sign_mesh = kit.save_mesh(sb, FAMILY_ID + ".sign_panel")

	var root = kit.new_root("FuelStation", FAMILY_ID)
	kit.add_mesh(root, "Body", mesh)
	var sign_x := (SHOP_X0 + SHOP_X1) * 0.5
	var sign = kit.add_node(root, "Sign", Transform3D(Basis.IDENTITY, Vector3(sign_x, SIGN_CENTER_Y, SHOP_Z0 - BAND_OUT - 0.005)))
	sign.set_meta("replaceable", true)
	sign.set_meta("panel_size_m", Vector2(SIGN_W, SIGN_H))
	sign.set_meta("note", "letreiro substituivel na testeira da loja; SignText e preenchido pela integracao com o nome da empresa REAL operadora (sem precos)")
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
	lbl.width = (SIGN_W - 0.3) / lbl.pixel_size
	lbl.double_sided = false
	# Label3D le-se a partir de +Z local; girar 180 graus para ficar voltado a -Z.
	lbl.transform = Transform3D(Basis(Vector3.UP, PI), Vector3(0.0, 0.0, pf - 0.012))
	sign.add_child(lbl)

	# --- Marcadores CANDIDATOS ---------------------------------------------------------
	kit.add_marker(root, "Candidate_VehicleEntry", Vector3(-CUT_X, 0.0, -HZ + 0.2), Vector3.FORWARD,
		"entrada de veiculos candidata no rebaixo -X da borda frontal; acesso real e da integracao")
	kit.add_marker(root, "Candidate_VehicleExit", Vector3(CUT_X, 0.0, -HZ + 0.2), Vector3.FORWARD,
		"saida de veiculos candidata no rebaixo +X da borda frontal; acesso real e da integracao")
	var n := 1
	for zc in ISLAND_Z:
		for sx in [-1.0, 1.0]:
			kit.add_marker(root, "Candidate_PumpStop_%d" % n, Vector3(sx * PUMP_X, 0.0, zc - ISLAND_HZ - STOP_OFFSET), Vector3.RIGHT,
				"parada candidata ao lado da bomba (face -Z da ilha), circulacao em +X; nao define fila nem capacidade")
			n += 1
	kit.add_marker(root, "Candidate_FuelDelivery", Vector3(CAP_XS[1], 0.0, TANK_Z0 - 0.6), Vector3.FORWARD,
		"descarga candidata do caminhao-tanque diante das tampas do tanque subterraneo; forward para fora da laje")
	kit.add_marker(root, "Candidate_PedestrianEntry", Vector3((DOOR_X0 + DOOR_X1) * 0.5, 0.0, APRON_Z0 - 0.4), Vector3.FORWARD,
		"entrada de pedestre candidata diante da porta de vidro da loja; acesso real e da integracao")

	kit.save_scene(root, FAMILY_ID, {
		"family_id": FAMILY_ID,
		"title_pt": "Posto de combustivel",
		"lot": LOT,
		"proposed": {
			"lote_x_z_m": [LOT.x, LOT.y],
			"cobertura_x_z_m": [CAN_X1 - CAN_X0, CAN_Z1 - CAN_Z0],
			"altura_livre_cobertura_m": CAN_Y0,
			"topo_cobertura_m": CAN_TOP,
			"testeira_altura_m": CAN_Y1 - CAN_Y0,
			"pilares": 4,
			"ilhas": {"quantidade": 2, "comprimento_m": ISLAND_HX * 2.0, "largura_m": ISLAND_HZ * 2.0, "altura_m": ISLAND_H, "centros_z_m": ISLAND_Z, "pontas": "arredondadas (meio-octogono, raio = meia largura)"},
			"bombas": {"quantidade": 4, "l_x_p_x_a_m": [1.18, 0.64, 2.3], "x_m": [-PUMP_X, PUMP_X]},
			"loja_x_z_m": [SHOP_X1 - SHOP_X0, SHOP_Z1 - SHOP_Z0],
			"loja_topo_platibanda_m": BAND_Y1 + COPING_H,
			"letreiro_l_x_a_m": [SIGN_W + 2.0 * SIGN_FRAME, SIGN_H + 2.0 * SIGN_FRAME],
			"laje_tanque_x_z_m": [TANK_X1 - TANK_X0, TANK_Z1 - TANK_Z0],
			"respiros_altura_m": VENT_TOP + 0.12,
			"rebaixos_centro_x_m": [-CUT_X, CUT_X],
			"rebaixo_largura_livre_m": CUT_HALF * 2.0,
			"meio_fio_altura_m": CURB_TOP,
		},
		"parts": {
			"Body": "estrutura (7 superficies = slots): road_asphalt (patio, laje da loja), concrete (piso sob a cobertura, meio-fio e rebaixos, calcada da loja, laje do tanque), trim_white (ilhas de pontas arredondadas, corpo das bombas, pilares, forro/capa da cobertura, capa da platibanda, molduras, aneis das tampas, respiros, caixa do ar-condicionado), fuel_accent (testeira da cobertura, cabecas das bombas, protecao dos pilares, faixa da loja), metal_dark (bases das bombas, mangueiras/bicos, caixilhos, tampas, terminais dos respiros, base/ventilador do ar-condicionado), glass_dark (vitrine, porta, janelas, visores lisos), fuel_wall (paredes da loja, painel superior da cobertura)",
			"Sign": "Node3D substituivel/removivel na testeira -Z da loja (pode ser trocado inteiro pela integracao)",
			"Sign/SignPanel": "painel liso shop_sign_blank + moldura metal_dark (malha commercial.fuel_station.a.sign_panel)",
			"Sign/SignText": "Label3D VAZIO, centralizado no painel, voltado a -Z; preencher com o nome da empresa REAL operadora; sem precos",
			"Candidate_VehicleEntry": "Marker3D candidato no rebaixo -X da borda frontal, forward -Z",
			"Candidate_VehicleExit": "Marker3D candidato no rebaixo +X da borda frontal, forward -Z",
			"Candidate_PumpStop_1..4": "Marker3D candidatos no solo ao lado de cada bomba (face -Z de cada ilha), forward +X (circulacao)",
			"Candidate_FuelDelivery": "Marker3D candidato diante das tampas do tanque, forward -Z (para fora da laje)",
			"Candidate_PedestrianEntry": "Marker3D candidato diante da porta da loja, forward -Z",
		},
		"notes": [
			"Pivo no solo, no centro do lote 24 x 24 m; o patio pavimentado ocupa o lote inteiro (escala do host ~1).",
			"Circulacao candidata: entra pelo rebaixo -X, percorre as faixas ao longo das ilhas em +X e sai pelo rebaixo +X (trafego pela direita). Nao ha setas pintadas; a regra real de fluxo e da integracao.",
			"A cobertura (altura livre 5,0 m) esconde a ilha de tras na vista isometrica frente-direita; a ilha da frente e visivel inteira. Se a integracao precisar ver veiculos sob a cobertura, a cobertura pode virar no proprio (nao feito: aumenta superficies).",
			"Sem no State_*: o combustivel fica no tanque enterrado e nao ha luzes; nivel/abastecimento sao mostrados pela integracao (UI/veiculos), nao pela malha.",
			"Painel superior da cobertura em fuel_wall (creme areia), um tom acima do piso concrete sob ela: em concrete os dois tinham o mesmo tom e se fundiam na planta. Caixa de ar-condicionado neutra na laje da loja (mesma linguagem da mercearia), sem estado.",
			"Icone: composicao propria e exagerada para 64 px (sem o lote inteiro; patio em L, loja 1,6 m mais perto, testeira 1,9 m, bombas ~1,5x, ilha da frente em z = -5,3); nao e planta nem medida.",
			"SignText: font_size 48, pixel_size 0,01 (altura de corpo ~0,48 m), largura util 5,1 m: uma linha de ~16 caracteres no painel 5,4 x 0,9 m. Nomes maiores: a integracao reduz pixel_size.",
			"Meio-fio frontal a 0,18 m (mesma cota da calcada da rua da amostra); nos rebaixos o meio-fio desce em rampas de 0,5 m ate 0,05 m e o asfalto do patio (0,04 m) segue ate a borda do lote (vao livre de 3,6 m).",
			"Sem precos, logos, nomes, veiculos, pessoas, luzes acesas, oficina, lava-rapido ou conveniencia ampliada. Portas fechadas; detalhes desenhados nao sao acessos funcionais (somente os Marker3D candidatos).",
		],
	})


# ---------------------------------------------------------------------------
# Icone SVG simplificado (cobertura + bombas + loja), mesma camera isometrica
# ---------------------------------------------------------------------------

## Composicao propria do icone (64 px): sem o lote inteiro (o asfalto de 24 x 24 m encolhia o
## posto a um quadrado escuro); patio em L so em volta da cobertura e da loja; loja 1,6 m mais
## perto da cobertura; testeira (1,9 m), pilares e bombas exagerados para lerem em poucos pixels
## (a testeira real de 0,9 m virava so contorno), bombas ~1,5x; ilha da frente 1,1 m mais a frente p/ as
## bombas aparecerem sob a testeira alta. Ilha de tras omitida (fica sob a cobertura).
const ICON_SHOP_DZ := -1.6
const ICON_ISLAND_Z := -5.3
const ICON_CAN_Y0 := 4.4
const ICON_CAN_Y1 := 6.3

func draw_icon(ic) -> void:
	var asphalt: String = ic.c("road_asphalt")
	var conc: String = ic.c("concrete")
	var white: String = ic.c("trim_white")
	var acc: String = ic.c("fuel_accent")
	var wall: String = ic.c("fuel_wall")
	var glass: String = ic.c("glass_dark")
	var metal: String = ic.c("metal_dark")
	var blank: String = ic.c("shop_sign_blank")
	var sz0: float = SHOP_Z0 + ICON_SHOP_DZ
	var sz1: float = SHOP_Z1 + ICON_SHOP_DZ
	# Patio em L (asfalto): faixa larga sob a cobertura + trecho estreito ate a loja.
	var bh := 0.12
	var bx0 := -8.6; var bx1 := 8.6; var bz0 := -7.6; var bz1 := sz1 + 0.6
	var nx := SHOP_X1 + 1.0      # fim do trecho da loja em +X
	var nz := PAD_Z1 + 0.4       # fim da faixa larga em +Z
	ic.part(-100.0)
	ic.quad([Vector3(bx0, bh, bz0), Vector3(bx1, bh, bz0), Vector3(bx1, bh, nz), Vector3(nx, bh, nz), Vector3(nx, bh, bz1), Vector3(bx0, bh, bz1)], asphalt)
	ic.quad([Vector3(bx1, 0.0, bz0), Vector3(bx0, 0.0, bz0), Vector3(bx0, bh, bz0), Vector3(bx1, bh, bz0)], asphalt)
	ic.quad([Vector3(bx1, 0.0, nz), Vector3(bx1, 0.0, bz0), Vector3(bx1, bh, bz0), Vector3(bx1, bh, nz)], asphalt)
	ic.quad([Vector3(nx, 0.0, bz1), Vector3(nx, 0.0, nz), Vector3(nx, bh, nz), Vector3(nx, bh, bz1)], asphalt)
	ic.part(-99.0)
	ic.box(Vector3(-PAD_X, bh, PAD_Z0), Vector3(PAD_X, 0.2, PAD_Z1), conc)
	ic.part(-99.0)
	ic.box(Vector3(SHOP_X0 - 0.5, bh, sz0 - 1.0), Vector3(SHOP_X1 + 0.5, 0.25, sz1), conc)
	# Loja: paredes creme, faixa no acento, laje escura com capa branca, vitrine escura.
	ic.part()
	ic.box(Vector3(SHOP_X0, 0.25, sz0), Vector3(SHOP_X1, BAND_Y0, sz1), wall)
	ic.facade_rect(WIN_X0, WIN_X1, WIN_Y0, SF_TOP, sz0 - 0.02, glass)
	ic.facade_rect(DOOR_X0, DOOR_X1, SHOP_FLOOR + 0.05, SF_TOP, sz0 - 0.02, glass)
	ic.window_side((sz0 + sz1) * 0.5, 1.85, 1.8, 1.1, SHOP_X1 + 0.02)
	ic.part()
	ic.box(Vector3(SHOP_X0 - 0.1, BAND_Y0, sz0 - 0.1), Vector3(SHOP_X1 + 0.1, BAND_Y1 + 0.15, sz1 + 0.1), acc, white)
	ic.facade_rect(-4.8, 0.6, 3.3, 4.3, sz0 - 0.12, blank)
	ic.part(0.5)
	ic.quad([Vector3(SHOP_X0 + 0.35, BAND_Y1 + 0.16, sz1 - 0.35), Vector3(SHOP_X1 - 0.35, BAND_Y1 + 0.16, sz1 - 0.35),
		Vector3(SHOP_X1 - 0.35, BAND_Y1 + 0.16, sz0 + 0.35), Vector3(SHOP_X0 + 0.35, BAND_Y1 + 0.16, sz0 + 0.35)], asphalt)
	# Ilha da frente com duas bombas e os dois pilares da frente.
	var zc := ICON_ISLAND_Z
	var it := 0.45
	ic.part()
	ic.box(Vector3(-ISLAND_HX, 0.2, zc - 0.65), Vector3(ISLAND_HX, it, zc + 0.65), white)
	for sx in [-1.0, 1.0]:
		var qx: float = sx * PILLAR_X
		ic.part()
		ic.box(Vector3(qx - 0.42, it, zc - 0.42), Vector3(qx + 0.42, 1.7, zc + 0.42), acc)
		ic.part()
		ic.box(Vector3(qx - 0.36, 1.7, zc - 0.36), Vector3(qx + 0.36, ICON_CAN_Y0, zc + 0.36), white)
		var px: float = sx * PUMP_X
		ic.part()
		ic.box(Vector3(px - 0.92, it, zc - 0.5), Vector3(px + 0.92, it + 0.14, zc + 0.5), metal)
		ic.part()
		ic.box(Vector3(px - 0.86, it + 0.14, zc - 0.45), Vector3(px + 0.86, 2.4, zc + 0.45), white)
		ic.facade_rect(px - 0.58, px + 0.58, 1.35, 2.15, zc - 0.47, glass)
		ic.part()
		ic.box(Vector3(px - 0.95, 2.4, zc - 0.53), Vector3(px + 0.95, 3.0, zc + 0.53), acc)
	# Cobertura por ultimo (fica acima de tudo): testeira alta no acento + capa branca + painel creme.
	ic.part(20.0)
	ic.box(Vector3(CAN_X0, ICON_CAN_Y0, CAN_Z0), Vector3(CAN_X1, ICON_CAN_Y1, CAN_Z1), acc)
	ic.part(20.1)
	ic.box(Vector3(CAN_X0 - 0.14, ICON_CAN_Y1, CAN_Z0 - 0.14), Vector3(CAN_X1 + 0.14, ICON_CAN_Y1 + 0.22, CAN_Z1 + 0.14), white)
	ic.part(20.2)
	var ty := ICON_CAN_Y1 + 0.23
	ic.quad([Vector3(CAN_X0 + 0.6, ty, CAN_Z1 - 0.6), Vector3(CAN_X1 - 0.6, ty, CAN_Z1 - 0.6),
		Vector3(CAN_X1 - 0.6, ty, CAN_Z0 + 0.6), Vector3(CAN_X0 + 0.6, ty, CAN_Z0 + 0.6)], wall)
