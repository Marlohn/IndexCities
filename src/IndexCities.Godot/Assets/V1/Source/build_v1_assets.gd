## Gerador da amostra visual V1 (ferramenta de autoria; NAO e carregado pelo jogo).
##
## Executa as receitas originais em Source/recipes e grava materiais, malhas, cenas e
## o manifesto em res://Assets/V1. Exemplo (projeto com raiz em src/IndexCities.Godot):
##
##   godot --headless --path <raiz-do-projeto> --script res://Assets/V1/Source/build_v1_assets.gd
##   ... -- --only=civic_city_hall      (somente receitas cujo NOME DE ARQUIVO comeca assim;
##                                       as demais receitas nem sao carregadas)
##   ... -- --force-palette             (regrava os materiais COMPARTILHADOS da paleta)
##   ... -- --no-manifest               (nao toca em manifest.v1.json)
##
## Execucao completa reescreve o manifesto; com --only as entradas reconstruidas sao
## FUNDIDAS no manifesto existente (as demais familias ficam intactas).
extends SceneTree

const AssetKit := preload("res://Assets/V1/Source/kit/asset_kit.gd")

## Ordem importa: o conjunto de amostra (preview) instancia cenas geradas antes.
const RECIPES := [
	"res://Assets/V1/Source/recipes/civic_city_hall.gd",
	"res://Assets/V1/Source/recipes/residential_small_house.gd",
	"res://Assets/V1/Source/recipes/commercial_grocery_small.gd",
	"res://Assets/V1/Source/recipes/road_urban_1x1.gd",
	"res://Assets/V1/Source/recipes/vegetation_tree_broadleaf.gd",
	"res://Assets/V1/Source/recipes/agent_sim_pedestrian.gd",
	"res://Assets/V1/Source/recipes/vehicle_truck_cargo.gd",
	# Segunda onda (2026-10-12): funcoes restantes do catalogo V1.
	"res://Assets/V1/Source/recipes/civic_works_depot.gd",
	"res://Assets/V1/Source/recipes/agriculture_farm_small.gd",
	"res://Assets/V1/Source/recipes/commercial_fuel_station.gd",
	"res://Assets/V1/Source/recipes/utility_water_intake.gd",
	"res://Assets/V1/Source/recipes/utility_water_treatment.gd",
	"res://Assets/V1/Source/recipes/utility_sewage_treatment.gd",
	"res://Assets/V1/Source/recipes/utility_wind_power.gd",
	"res://Assets/V1/Source/recipes/preview_sample_board.gd",
	"res://Assets/V1/Source/recipes/preview_wave2_board.gd",
]

const MANIFEST_PATH := "res://Assets/V1/manifest.v1.json"


func _initialize() -> void:
	var only: PackedStringArray = PackedStringArray()
	var force_palette := false
	var manifest := true
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--only="):
			only = a.substr(7).split(",", false)
		elif a == "--force-palette":
			force_palette = true
		elif a == "--no-manifest":
			manifest = false
	var kit = AssetKit.new()
	kit.ensure_palette(force_palette)
	var built: PackedStringArray = PackedStringArray()
	for path in RECIPES:
		if not only.is_empty():
			var base: String = path.get_file().get_basename()
			var wanted := false
			for o in only:
				if base.begins_with(o):
					wanted = true
			if not wanted:
				continue
		if not ResourceLoader.exists(path):
			print("[build] receita ausente (ignorada): ", path)
			continue
		var script: GDScript = load(path)
		if script == null:
			kit.err("Receita nao carregou: %s" % path)
			continue
		var consts: Dictionary = script.get_script_constant_map()
		var fid: String = consts.get("FAMILY_ID", "")
		if consts.has("MATERIALS"):
			kit.ensure_materials(consts["MATERIALS"], str(consts.get("MATERIAL_PREFIX", "")))
		var recipe = script.new()
		print("[build] ", fid, "  <- ", path)
		recipe.build(kit)
		built.append(fid)
	for e in kit.entries:
		print("[entry] %s | scene=%s | tris=%d | surfaces=%d | envelope=%s" % [
			e["visual_id"] + ("/" + e["piece"] if e["piece"] != "" else ""),
			e["scene"], e["triangles_total"], e["mesh_surfaces_total"], JSON.stringify(e["envelope"])])
		for m in e["markers"]:
			print("         marker %s pos=%s fwd=%s" % [m["node"], JSON.stringify(m["position"]), JSON.stringify(m["forward"])])
	if manifest and only.is_empty():
		kit.write_manifest(MANIFEST_PATH, {"built_families": built})
		print("[build] manifesto: ", MANIFEST_PATH)
	elif manifest:
		# Build parcial: funde no manifesto existente sem tocar nas demais familias.
		kit.merge_manifest(MANIFEST_PATH, built)
		print("[build] manifesto fundido (parcial): ", MANIFEST_PATH)
	if kit.errors.is_empty():
		print("[build] OK (%d familias, %d cenas)" % [built.size(), kit.entries.size()])
		quit(0)
	else:
		for m in kit.errors:
			printerr("[build] ERRO: ", m)
		quit(1)
