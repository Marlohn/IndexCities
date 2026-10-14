// IndexCities - WaterStudy - AMOSTRA A: agua + margem pintadas DENTRO do disco atual do lago.
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE.
//
// SOMENTE ESTETICO. A faixa de margem, a linha d'agua irregular, o raso, o reflexo e a
// ondulacao nao representam nivel, profundidade, vazao, poluicao, captacao, recurso, area
// navegavel nem recuo de construcao. A area de jogo continua sendo o circulo inteiro.
//
// Malha alvo: EXATAMENTE a do CityWorld (CylinderMesh top_radius = R, bottom_radius = R + 1,
// height = 0,15, 64 segmentos, em y = -0,04). Este shader NAO desloca vertices: silhueta,
// raio, altura, colisao e picking ficam iguais. Tudo e cor e normal no fragmento:
//  1. a borda real do disco e o talude lateral recebem a cor e a normal exatas da grama do
//     chao (espelho de env_grass_soft), entao o "prato" elevado deixa de aparecer;
//  2. entre a borda e a linha d'agua, uma MARGEM natural (grama que desce em linguas, terra,
//     areia, pedrisco, faixa molhada) de largura irregular: a linha d'agua visivel deixa de ser
//     um circulo, sempre DENTRO do circulo de jogo;
//  3. a margem recebe normal inclinada para dentro (relevo falso de bacia, sem geometria): a
//     luz do jogo a ilumina como depressao, e nao como disco elevado;
//  4. a agua: fundo visivel no raso, profundo escuro, reflexo estilizado do ceu (Fresnel),
//     faixa escura de reflexo da margem do lado oposto a camera, ondulacao e rajadas lentas.
//
// Raio sem parametro do host: na tampa em leque, (R no vertice da borda, 0 no centro) /
// (1 na borda, 0 no centro) interpolados dao R exato em qualquer ponto. `lake_radius_m` > 0
// sobrescreve. O talude lateral e detectado pela normal inclinada.
//
// GL Compatibility: opaco, uma passada, sem render_mode (mesma iluminacao padrao do chao),
// sem textura, tela, profundidade, ALPHA, EMISSION, light() ou discard.
shader_type spatial;

#include "res://Assets/WaterStudy/Shaders/ws_common.gdshaderinc"

// ---- Grama: espelho de env_grass_soft (mesmos padroes e dicas do original) ----
uniform vec3 grass_base_color : source_color = vec3(0.490196, 0.607843, 0.321569);
uniform vec3 grass_warm_tint : source_color = vec3(0.525490, 0.627451, 0.329412);
uniform vec3 grass_cool_tint : source_color = vec3(0.423529, 0.580392, 0.321569);
uniform float grass_variation : hint_range(0.0, 1.0) = 0.7;
uniform float grass_patch_size_m : hint_range(8.0, 200.0) = 56.0;
uniform float grass_detail_size_m : hint_range(2.0, 40.0) = 12.0;
uniform float grass_grain_size_m : hint_range(0.4, 8.0) = 1.2;
uniform float grass_grain_strength : hint_range(0.0, 0.15) = 0.025;
uniform float grass_roughness : hint_range(0.0, 1.0) = 0.85;

// ---- Linha d'agua (m, a partir da borda do circulo para dentro) ----
uniform float bank_min_m : hint_range(0.0, 10.0) = 1.0;
uniform float bank_max_m : hint_range(0.5, 14.0) = 5.5;
uniform float bank_jag_m : hint_range(0.0, 3.0) = 0.8;
uniform float bank_lobe_m : hint_range(4.0, 80.0) = 14.0;
uniform float bank_bay_m : hint_range(10.0, 160.0) = 38.0;

// ---- Margem ----
uniform vec3 soil_dry_color : source_color = vec3(0.561, 0.486, 0.357);
uniform vec3 soil_damp_color : source_color = vec3(0.431, 0.365, 0.267);
uniform vec3 soil_wet_color : source_color = vec3(0.275, 0.224, 0.169);
uniform vec3 sand_color : source_color = vec3(0.690, 0.627, 0.490);
uniform vec3 pebble_color : source_color = vec3(0.612, 0.596, 0.545);
// Grama mais escura e umida perto da agua (so na margem; zero na borda real do disco).
uniform vec3 lush_grass_color : source_color = vec3(0.302, 0.459, 0.212);
uniform float lush_strength : hint_range(0.0, 1.0) = 0.55;
// Quanto a grama desce pela margem (fracao da largura), em linguas irregulares.
uniform float grass_reach : hint_range(0.0, 1.5) = 0.85;
uniform float sand_amount : hint_range(0.0, 1.0) = 0.6;
// Relevo falso: altura equivalente da margem (m) e exagero da inclinacao da normal.
uniform float bank_height_m : hint_range(0.0, 3.0) = 0.6;
uniform float bank_relief : hint_range(0.0, 6.0) = 2.4;
uniform float pebble_amount : hint_range(0.0, 1.0) = 0.3;

// ---- Agua ----
uniform vec3 water_deep_color : source_color = vec3(0.122, 0.290, 0.333);
uniform vec3 water_mid_color : source_color = vec3(0.208, 0.416, 0.416);
uniform vec3 water_bottom_color : source_color = vec3(0.490, 0.478, 0.345);
uniform vec3 sky_horizon_color : source_color = vec3(0.725490, 0.788235, 0.835294);
uniform vec3 sky_zenith_color : source_color = vec3(0.553, 0.690, 0.792);
uniform vec3 bank_reflection_color : source_color = vec3(0.200, 0.290, 0.180);
uniform vec3 lap_color : source_color = vec3(0.741, 0.761, 0.659);
// Escala (m) em que o fundo deixa de aparecer; distancia em que o profundo se completa.
uniform float shallow_scale_m : hint_range(0.2, 10.0) = 1.4;
uniform float deep_distance_m : hint_range(1.0, 30.0) = 8.0;
uniform float deep_variation : hint_range(0.0, 1.0) = 0.25;
uniform float reflection_strength : hint_range(0.0, 1.0) = 0.42;
uniform float gust_contrast : hint_range(0.0, 1.0) = 0.3;
uniform float bank_reflection_m : hint_range(0.0, 8.0) = 2.4;
uniform float bank_reflection_strength : hint_range(0.0, 1.0) = 0.5;
uniform float lap_strength : hint_range(0.0, 1.0) = 0.25;
uniform float ripple_strength : hint_range(0.0, 2.0) = 1.0;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.5;
uniform float wind_angle_deg : hint_range(-180.0, 180.0) = 35.0;
uniform float water_roughness : hint_range(0.0, 1.0) = 0.12;
uniform float water_specular : hint_range(0.0, 1.0) = 0.4;

// < 0: usa TIME. >= 0: tempo decorativo do host (CityWorld ja passa fmod(segundos, 3600)
// parado na pausa; mesmo nome de parametro do material integrado).
uniform float host_time_s = -1.0;
// <= 0: raio deduzido da malha em leque. > 0: raio do lago em metros.
uniform float lake_radius_m = -1.0;

varying vec3 v_world;
varying vec2 v_centre;
varying float v_rimw;
varying float v_rw;
varying float v_bevel;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_centre = MODEL_MATRIX[3].xz;
	float rl = length(VERTEX.xz);
	bool cap = NORMAL.y > 0.999;
	// Tampa: borda = (R, 1), centro = (0, 0). Talude/fundo: marcados como talude.
	v_rimw = cap ? (rl > 0.01 ? 1.0 : 0.0) : 1.0;
	v_rw = rl;
	v_bevel = cap ? 0.0 : 1.0;
}

void fragment() {
	// Derivadas (fwidth) so aqui, em fluxo uniforme; os ramos abaixo nao usam derivadas.
	vec2 p = v_world.xz;
	float foot = ws_foot(p);
	float t = host_time_s >= 0.0 ? host_time_s : TIME;
	vec3 up_view = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);

	// Distancia ate o circulo real (m): > 0 dentro da tampa.
	float R = lake_radius_m > 0.0 ? lake_radius_m : v_rw / max(v_rimw, 1e-4);
	vec2 rel = p - v_centre;
	float d = max(length(rel), 1e-3);
	vec2 outward = rel / d;
	float e = R - d;
	float aa = max(foot, 0.02);

	// Linha d'agua irregular: s > 0 agua, s < 0 margem (aprox. distancia a linha d'agua, m).
	float w = ws_bank_width(p, bank_min_m, bank_max_m, bank_jag_m, bank_lobe_m, bank_bay_m);
	float s = e - w;
	float water = smoothstep(-0.7 * aa, 0.7 * aa, s) * (1.0 - v_bevel);
	float n_soil = ws_noise(WS_ROT_S * p / 2.7, 101u);

	// ----------------------------------------------------------------------------- margem
	// So calculada onde aparece (fora da agua aberta): poupa a grama espelhada no meio do lago.
	vec3 bank_col = vec3(0.0);
	vec3 n_bank = vec3(0.0, 1.0, 0.0);
	float bank_rough = grass_roughness;
	if (water < 0.999) {
		vec3 grass = ws_grass_mirror(p, foot, grass_base_color, grass_warm_tint, grass_cool_tint,
				grass_variation, grass_patch_size_m, grass_detail_size_m, grass_grain_size_m,
				grass_grain_strength);
		if (v_bevel > 0.5) {
			// Talude lateral do disco: exatamente a grama do chao, normal para cima.
			bank_col = grass;
		} else {
			float tb = clamp(e / max(w, 0.05), 0.0, 1.0);     // 0 na borda real, 1 na linha d'agua
			float n_tongue = ws_noise(WS_ROT_T * p / 5.5, 103u);
			float n_peb = ws_noise(WS_ROT_U * p / 0.42, 107u);
			float n_sand = ws_noise(WS_ROT_JAG * p / 11.0, 109u);
			float reach = grass_reach * (0.3 + 0.95 * n_tongue);
			// O declive comeca numa linha IRREGULAR dentro da margem (fracao `top`, mesma formula
			// de ws_shore_geometry.gd::bank_top_fraction), nao no circulo: a faixa iluminada do
			// talude nao desenha o contorno circular. Perfil "smootherstep" (inclinacao e
			// curvatura nulas no topo): derivada 30 t^2 (1 - t)^2; sem inclinacao no ultimo metro.
			float top = clamp(reach * 0.55, 0.0, 0.7);
			float tbs = clamp((tb - top) / max(1.0 - top, 0.05), 0.0, 1.0);
			float slope = bank_relief * bank_height_m * 30.0 * tbs * tbs * (1.0 - tbs) * (1.0 - tbs)
					/ max(w * (1.0 - top), 0.6);
			slope *= smoothstep(0.0, 1.2, e);
			n_bank = normalize(vec3(-outward.x * slope, 1.0, -outward.y * slope));
			// Grama que desce em linguas (as vezes ate a agua); perto da borda real, grama pura.
			float grass_amt = 1.0 - smoothstep(reach * 0.7, reach + 0.05, tb);
			grass_amt = max(grass_amt, 1.0 - smoothstep(0.25, 0.25 + max(0.35, 2.0 * aa), e));
			float lush = smoothstep(0.2, 0.95, tb) * lush_strength;
			vec3 bank_grass = mix(grass, lush_grass_color * (0.92 + 0.16 * n_soil), lush);
			vec3 soil = mix(soil_dry_color, soil_damp_color, smoothstep(0.45, 0.85, tb));
			soil = mix(soil, sand_color, smoothstep(0.55, 0.8, n_sand) * sand_amount * smoothstep(0.3, 0.6, tb));
			soil *= 0.88 + 0.24 * n_soil;
			float peb = smoothstep(0.72, 0.9, n_peb) * pebble_amount * ws_fade(0.6, foot)
					* smoothstep(0.2, 0.5, tb) * (1.0 - smoothstep(0.85, 0.97, tb));
			soil = mix(soil, pebble_color * (0.9 + 0.2 * n_soil), peb);
			float wet = smoothstep(0.82, 0.985, tb);
			soil = mix(soil, soil_wet_color, wet);
			bank_col = mix(soil, bank_grass, grass_amt);
			bank_rough = mix(mix(0.92, 0.55, wet), grass_roughness, grass_amt);
		}
	}

	// ------------------------------------------------------------------------------- agua
	vec3 water_col = vec3(0.0);
	vec3 n_water = vec3(0.0, 1.0, 0.0);
	if (water > 0.001) {
		float depth_vis = max(s, 0.0);
		float k = 1.0 - exp(-depth_vis / shallow_scale_m);
		vec3 bottom = mix(water_bottom_color, soil_wet_color, 0.35 * n_soil);
		vec3 body = mix(bottom, water_mid_color, smoothstep(0.0, 0.6, k));
		body = mix(body, water_deep_color, smoothstep(0.0, 1.0, depth_vis / deep_distance_m));
		float patches = ws_noise(WS_ROT_LOBE * p / 23.0, 113u) - 0.5;
		body *= 1.0 + patches * 0.22 * deep_variation * smoothstep(2.0, 8.0, depth_vis);

		// Ondulacao + rajadas. Normal so de sombreamento.
		float gust = ws_gust(p, t);
		vec2 grad = ws_ripple_gradient(p, t, foot, ripple_speed, wind_angle_deg, vec2(1.0, 0.0), 0.0);
		grad *= ripple_strength * mix(0.3, 1.0, gust) * smoothstep(0.0, 1.2, depth_vis);
		n_water = normalize(vec3(-grad.x, 1.0, -grad.y));

		// Reflexo estilizado: ceu (Fresnel suave) + margem refletida no lado oposto a camera.
		vec3 v_dir = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
		float cos_v = clamp(dot(n_water, v_dir), 0.0, 1.0);
		float fres = 0.25 + 1.6 * pow(1.0 - cos_v, 2.5);
		vec3 r_dir = reflect(-v_dir, n_water);
		vec3 sky = ws_sky(r_dir, sky_horizon_color, sky_zenith_color);
		vec3 cam_fwd = -INV_VIEW_MATRIX[2].xyz;
		vec2 cam_xz = normalize(cam_fwd.xz + vec2(1e-5, 0.0));
		float far_side = smoothstep(-0.1, 0.65, dot(outward, cam_xz));
		float band = (1.0 - smoothstep(0.0, bank_reflection_m, depth_vis)) * far_side;
		float refl_amt = clamp(reflection_strength * fres * (1.0 + gust_contrast * (0.5 - gust)), 0.0, 1.0);
		water_col = mix(body, sky, refl_amt);
		water_col = mix(water_col, bank_reflection_color, band * bank_reflection_strength);
		// Linha de contato clara e fina na beira (so de perto).
		float lap = (1.0 - smoothstep(0.0, 0.3, depth_vis)) * lap_strength * ws_fade(0.9, foot);
		water_col = mix(water_col, lap_color, lap);
	}

	// ------------------------------------------------------------------------------ saida
	vec3 col = mix(bank_col, water_col, water);
	vec3 n_world = normalize(mix(n_bank, n_water, water));
	ALBEDO = col;
	ROUGHNESS = mix(bank_rough, water_roughness, water);
	SPECULAR = mix(0.5, water_specular, water);
	NORMAL = normalize(mix((VIEW_MATRIX * vec4(n_world, 0.0)).xyz, up_view, v_bevel));
}
