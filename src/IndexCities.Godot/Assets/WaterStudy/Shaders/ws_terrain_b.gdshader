// IndexCities - WaterStudy - AMOSTRA B: chao com bacia rebaixada (margem em relevo real).
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE.
//
// GEOMETRIA CONCEITUAL, NAO APROVADA PARA GAMEPLAY: so existe na previa isolada do estudo.
// SOMENTE ESTETICO: as cores de margem, barranco, praia e faixa molhada nao representam solo,
// fertilidade, erosao, nivel, profundidade, recurso, area construivel nem estado da simulacao.
//
// Malha alvo: ws_b_terrain.res (grade gerada por Source/ws_shore_geometry.gd), que substitui o
// BoxMesh do chao SO nesta previa. No chao plano o resultado e exatamente a grama do jogo
// (espelho de env_grass_soft, mesma iluminacao padrao); dentro da bacia a cor vem da altura em
// relacao a agua e da inclinacao REAL da malha. UV2.x = 1 marca as faces laterais do limite do
// mapa (grama, como as faces do BoxMesh do jogo).
//
// GL Compatibility: opaco, uma passada, sem render_mode, sem textura/tela/profundidade.
shader_type spatial;

#include "res://Assets/WaterStudy/Shaders/ws_common.gdshaderinc"

uniform vec3 grass_base_color : source_color = vec3(0.490196, 0.607843, 0.321569);
uniform vec3 grass_warm_tint : source_color = vec3(0.525490, 0.627451, 0.329412);
uniform vec3 grass_cool_tint : source_color = vec3(0.423529, 0.580392, 0.321569);
uniform float grass_variation : hint_range(0.0, 1.0) = 0.7;
uniform float grass_patch_size_m : hint_range(8.0, 200.0) = 56.0;
uniform float grass_detail_size_m : hint_range(2.0, 40.0) = 12.0;
uniform float grass_grain_size_m : hint_range(0.4, 8.0) = 1.2;
uniform float grass_grain_strength : hint_range(0.0, 0.15) = 0.025;
uniform float grass_roughness : hint_range(0.0, 1.0) = 0.85;

// Niveis da previa B (iguais a ws_shore_geometry.gd).
uniform float ground_y = -0.06;
uniform float water_level_y = -0.86;
uniform float bank_height_m = 0.8;

uniform vec3 soil_dry_color : source_color = vec3(0.561, 0.486, 0.357);
uniform vec3 soil_damp_color : source_color = vec3(0.431, 0.365, 0.267);
uniform vec3 soil_wet_color : source_color = vec3(0.275, 0.224, 0.169);
uniform vec3 scarp_color : source_color = vec3(0.478, 0.388, 0.282);
uniform vec3 sand_color : source_color = vec3(0.690, 0.627, 0.490);
uniform vec3 pebble_color : source_color = vec3(0.612, 0.596, 0.545);
uniform vec3 lush_grass_color : source_color = vec3(0.302, 0.459, 0.212);
uniform float lush_strength : hint_range(0.0, 1.0) = 0.55;
uniform float grass_reach : hint_range(0.0, 1.5) = 0.85;
uniform float sand_amount : hint_range(0.0, 1.0) = 0.6;
uniform float pebble_amount : hint_range(0.0, 1.0) = 0.3;

varying vec3 v_world;
varying float v_side;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_side = UV2.x;
}

void fragment() {
	// Derivadas (fwidth) so aqui, em fluxo uniforme; o ramo abaixo nao usa derivadas.
	vec2 p = v_world.xz;
	float foot = ws_foot(p);
	vec3 grass = ws_grass_mirror(p, foot, grass_base_color, grass_warm_tint, grass_cool_tint,
			grass_variation, grass_patch_size_m, grass_detail_size_m, grass_grain_size_m,
			grass_grain_strength);
	float below = ground_y - v_world.y;            // > 0 dentro da bacia
	vec3 col = grass;
	float rough = grass_roughness;

	// Chao plano e faces laterais: so a grama (mesmo custo do chao atual). Margem: o resto.
	if (below > 0.003 && v_side < 0.5) {
		vec3 n_world = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
		float h = v_world.y - water_level_y;       // altura acima do nivel d'agua
		float steep = 1.0 - n_world.y;             // 0 = plano
		float tb = clamp(below / bank_height_m, 0.0, 1.0);

		float n_soil = ws_noise(WS_ROT_S * p / 2.7, 101u);
		float n_tongue = ws_noise(WS_ROT_T * p / 5.5, 103u);
		float n_peb = ws_noise(WS_ROT_U * p / 0.42, 107u);
		float n_sand = ws_noise(WS_ROT_JAG * p / 11.0, 109u);

		// Grama desce em linguas (as vezes ate a agua); recua nos barrancos; topo = grama pura.
		float reach = grass_reach * (0.3 + 0.95 * n_tongue);
		float grass_amt = 1.0 - smoothstep(reach * 0.7, reach + 0.05, tb);
		grass_amt *= 1.0 - smoothstep(0.1, 0.24, steep) * smoothstep(0.02, 0.12, tb);
		grass_amt = max(grass_amt, 1.0 - smoothstep(0.004, 0.03, below));
		float lush = smoothstep(0.2, 0.95, tb) * lush_strength;
		vec3 bank_grass = mix(grass, lush_grass_color * (0.92 + 0.16 * n_soil), lush);

		vec3 soil = mix(soil_dry_color, soil_damp_color, smoothstep(0.45, 0.85, tb));
		soil = mix(soil, scarp_color, smoothstep(0.1, 0.26, steep));
		float beach = (1.0 - smoothstep(0.03, 0.09, steep)) * smoothstep(0.5, 0.8, tb)
				* smoothstep(0.55, 0.8, n_sand) * sand_amount;
		soil = mix(soil, sand_color, beach);
		soil *= 0.88 + 0.24 * n_soil;
		float peb = smoothstep(0.72, 0.9, n_peb) * pebble_amount * ws_fade(0.6, foot)
				* smoothstep(0.2, 0.5, tb) * smoothstep(0.06, 0.16, h);
		soil = mix(soil, pebble_color * (0.9 + 0.2 * n_soil), peb);
		float wet = 1.0 - smoothstep(0.03, 0.13, h);
		soil = mix(soil, soil_wet_color, wet);

		col = mix(soil, bank_grass, grass_amt);
		rough = mix(mix(0.92, 0.55, wet), grass_roughness, grass_amt);
	}

	ALBEDO = col;
	ROUGHNESS = rough;
	SPECULAR = 0.5;
}
