// IndexCities - WaterStudy - AMOSTRA C: barranco e fundo do lago vistos atraves da agua.
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE. Proposta NAO integrada.
//
// SOMENTE APRESENTACAO. Lodo, areia, plantas submersas, pedrisco e barranco nao representam
// recurso, qualidade da agua, poluicao, vegetacao da regra ambiental nem area de obra.
//
// Malha gerada por Source/ws_lake_c.gd a partir do centro e do raio reais do lago:
//   UV  = (distancia s ate a margem em m, altura acima da agua em m)
//   UV2 = (profundidade sob a agua em m, 0 no fundo / 1 + altura do barranco seco no barranco)
// A parte submersa ja sai "vista atraves da agua": absorcao por canal e cor do corpo d'agua
// pela profundidade daquele ponto. A superficie (ws_water_c) so acrescenta reflexo e brilho.
//
// Opaco. GL Compatibility: so aritmetica, uint e fwidth (via ws_common); sem texturas.
shader_type spatial;

#include "res://Assets/WaterStudy/Shaders/ws_common.gdshaderinc"

// Fundo (cores proximas entre si, baixa saturacao: nada de faixa contrastante na margem).
uniform vec3 silt_color : source_color = vec3(0.329, 0.341, 0.212);
uniform vec3 silt_dark_color : source_color = vec3(0.247, 0.267, 0.169);
uniform vec3 sand_color : source_color = vec3(0.502, 0.459, 0.337);
uniform vec3 weed_color : source_color = vec3(0.263, 0.341, 0.169);
uniform vec3 pebble_color : source_color = vec3(0.420, 0.408, 0.361);
// Barranco: terra umida, raizes e grama pendente no topo (a grama do chao e a do jogo).
uniform vec3 bank_soil_color : source_color = vec3(0.290, 0.235, 0.165);
uniform vec3 bank_wet_color : source_color = vec3(0.196, 0.173, 0.129);
uniform vec3 bank_grass_color : source_color = vec3(0.333, 0.455, 0.208);
// Agua: absorcao por canal (1/m) e cor do corpo d'agua profundo (luz espalhada).
uniform vec3 absorption_per_m = vec3(2.4, 1.05, 0.9);
uniform float path_factor : hint_range(1.0, 4.0) = 2.0;
uniform vec3 water_body_color : source_color = vec3(0.110, 0.251, 0.290);
uniform float roughness_value : hint_range(0.0, 1.0) = 0.92;

varying vec3 v_world;
varying vec2 v_uv;
varying vec2 v_uv2;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_uv = UV;
	v_uv2 = UV2;
}

vec3 bed_color(vec2 p, float s, float depth, float foot) {
	// Lodo claro/escuro em manchas grandes; areia clara so em trechos da prateleira rasa.
	float big = ws_noise(WS_ROT_S * p / 9.0, 701u);
	float mid = ws_noise(WS_ROT_T * p / 3.1, 709u);
	vec3 col = mix(silt_dark_color, silt_color, smoothstep(0.25, 0.75, 0.7 * big + 0.3 * mid));
	float sand = smoothstep(0.5, 0.75, ws_noise(WS_ROT_U * p / 11.0, 719u)) * (1.0 - smoothstep(0.12, 0.3, depth));
	col = mix(col, sand_color, sand * 0.35);
	// Capim submerso junto a margem, com largura irregular (0 a ~1,6 m): continua a grama do chao
	// por baixo de uma lamina d'agua, em vez de uma faixa de terra ou de lama.
	float reach = 1.6 * smoothstep(0.3, 0.75, ws_noise(WS_ROT_BAY * p / 5.0, 743u));
	float grass = (1.0 - smoothstep(reach * 0.55, reach + 0.05, s)) * (0.75 + 0.25 * mid);
	col = mix(col, weed_color, grass);
	// Tufos submersos esparsos na prateleira.
	float weed_n = ws_noise(WS_ROT_LOBE * p / 4.2, 727u) * 0.75 + ws_noise(WS_ROT_JAG * p / 1.3, 733u) * 0.25 * ws_fade(1.3, foot);
	float weed = smoothstep(0.6, 0.75, weed_n) * smoothstep(0.06, 0.2, depth) * (1.0 - smoothstep(0.35, 0.6, depth));
	col = mix(col, weed_color, weed * 0.7);
	// Pedrisco fino perto do barranco (so de perto).
	float pebble = step(0.86, ws_noise(WS_ROT_BAY * p / 0.55, 739u)) * (1.0 - smoothstep(0.8, 2.5, s)) * ws_fade(0.55, foot);
	col = mix(col, pebble_color, pebble * 0.6);
	return col;
}

vec3 bank_color(vec2 p, float above, float drop, float foot) {
	// `above`: altura acima da agua (m); `drop`: altura total do barranco seco.
	float t = clamp(above / max(drop, 1e-3), -1.0, 1.0);
	vec3 col = mix(bank_wet_color, bank_soil_color, smoothstep(0.0, 0.45, t));
	// Raizes/estrias verticais finas (so de perto).
	float stria = ws_noise(vec2(dot(p, vec2(0.7071, 0.7071)) / 0.35, above / 0.9), 751u);
	col *= 1.0 - 0.18 * smoothstep(0.55, 0.85, stria) * ws_fade(0.35, foot);
	// Grama pendente do topo: comprimento irregular ao longo da margem (nunca uma faixa igual).
	float hang = 0.15 + 0.85 * ws_noise(WS_ROT_JAG * p / 2.3, 757u) * ws_noise(WS_ROT_S * p / 0.9, 761u);
	float grass = smoothstep(1.0 - hang, 1.0 - hang + 0.12, t);
	col = mix(col, bank_grass_color, grass);
	return col;
}

void fragment() {
	vec2 p = v_world.xz;
	float foot = ws_foot(p);
	float s = v_uv.x;
	float depth = v_uv2.x;
	bool wall = v_uv2.y > 0.5;
	vec3 col;
	if (wall) {
		// Barranco: UV.y = altura acima da agua; UV2.y = 1 + altura total do barranco seco.
		float above = v_uv.y;
		col = above >= 0.0 ? bank_color(p, above, v_uv2.y - 1.0, foot) : mix(bank_wet_color, silt_dark_color, 0.5);
		depth = -above;
	} else {
		col = bed_color(p, s, depth, foot);
		// Sombra/oclusao do barranco e da grama pendente sobre o fundo logo junto a margem, fraca e
		// em trechos (largura ~0,1 a 0,6 m, as vezes nenhuma), para nao desenhar um contorno escuro.
		float occl_n = ws_noise(WS_ROT_LOBE * p / 3.3, 947u);
		float occl_w = 0.1 + 0.5 * occl_n;
		float occl_k = mix(1.0, 0.8, smoothstep(0.35, 0.7, occl_n));
		col *= mix(occl_k, 1.0, smoothstep(0.0, occl_w, s));
	}
	// Visto atraves da agua: absorcao por canal + cor do corpo d'agua. Nada muda acima da agua.
	float d = max(depth, 0.0);
	vec3 tr = exp(-absorption_per_m * d * path_factor);
	col = col * tr + water_body_color * (vec3(1.0) - tr);
	ALBEDO = col;
	ROUGHNESS = roughness_value;
	SPECULAR = 0.25;
}
