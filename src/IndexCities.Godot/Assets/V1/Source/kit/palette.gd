## Paleta CANDIDATA da amostra V1 (revisao humana PENDENTE).
##
## Cores originais definidas para este projeto. Cada entrada vira um material
## compartilhado `res://Assets/V1/Materials/<nome>.tres` (StandardMaterial3D).
## O nome do material e tambem o nome do "slot" usado pelo mesh_builder e o nome da
## superficie no ArrayMesh, o que permite trocar material por superficie na integracao
## (variantes de cor) sem regenerar a geometria.
##
## Regras de estilo: tons levemente dessaturados, contraste claro entre parede,
## telhado e acento; NENHUM material emissivo e usado por padrao (iluminacao de
## funcionamento so pode ser ligada pela integracao a partir de estado real).
extends RefCounted

const MATERIALS := {
	# Somente materiais COMPARTILHADOS entre familias. Materiais exclusivos de uma
	# familia ficam na propria receita (const MATERIALS, com MATERIAL_PREFIX obrigatorio),
	# para que cada familia evolua sem colisao.
	"glass_dark": {"albedo": "#3b5468", "roughness": 0.18, "metallic": 0.15},
	"trim_white": {"albedo": "#f2eee6", "roughness": 0.8},
	"metal_dark": {"albedo": "#3e454d", "roughness": 0.5, "metallic": 0.35},
	"metal_light": {"albedo": "#a3aab1", "roughness": 0.45, "metallic": 0.45},
	"concrete": {"albedo": "#bdb8ae", "roughness": 0.92},
	"concrete_dark": {"albedo": "#8f8c86", "roughness": 0.92},
	"plinth_stone": {"albedo": "#a19b91", "roughness": 0.9},
	"wood_door": {"albedo": "#6f4a33", "roughness": 0.75},
	"rubber": {"albedo": "#2a2c2f", "roughness": 0.92},
}


static func make_material(def: Dictionary) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(def.get("albedo", "#ff00ff"))
	m.roughness = float(def.get("roughness", 0.9))
	m.metallic = float(def.get("metallic", 0.0))
	if def.has("emission"):
		m.emission_enabled = true
		m.emission = Color(def["emission"])
		m.emission_energy_multiplier = float(def.get("emission_energy", 1.0))
	return m
