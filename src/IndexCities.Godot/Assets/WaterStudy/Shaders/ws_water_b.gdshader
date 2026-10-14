// IndexCities - WaterStudy - AMOSTRA B: superficie d'agua plana da bacia e do rio conceituais.
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE.
//
// GEOMETRIA CONCEITUAL, NAO APROVADA PARA GAMEPLAY: so existe na previa isolada do estudo.
// SOMENTE ESTETICO: profundidade do fundo, raso, reflexo, ondulacao e os riscos de correnteza do
// rio sao aparencia. Nao representam nivel, vazao, captacao, poluicao, recurso, navegacao nem
// qualquer estado da simulacao (que nao tem rio nem profundidade).
//
// Malha alvo: ws_b_water.res (plano em y = nivel d'agua; entra um pouco sob a margem e a linha
// d'agua nasce da intersecao com o relevo). Atributos gravados pelo gerador:
//   UV.x  = profundidade decorativa do fundo sob a superficie (m)
//   UV.y  = distancia aproximada ate a linha d'agua (m, > 0 dentro d'agua)
//   UV2   = direcao horizontal da margem mais proxima (para o reflexo do lado oposto a camera)
//   COLOR = r: 1 no rio, 0 no lago; gb: direcao da correnteza decorativa (0..1 -> -1..1)
//
// GL Compatibility: opaco, uma passada, sem render_mode, sem textura/tela/profundidade.
shader_type spatial;

#include "res://Assets/WaterStudy/Shaders/ws_common.gdshaderinc"

uniform vec3 water_deep_color : source_color = vec3(0.122, 0.290, 0.333);
uniform vec3 water_mid_color : source_color = vec3(0.208, 0.416, 0.416);
uniform vec3 water_bottom_color : source_color = vec3(0.490, 0.478, 0.345);
uniform vec3 soil_wet_color : source_color = vec3(0.275, 0.224, 0.169);
uniform vec3 river_tint_color : source_color = vec3(0.243, 0.388, 0.341);
uniform vec3 sky_horizon_color : source_color = vec3(0.725490, 0.788235, 0.835294);
uniform vec3 sky_zenith_color : source_color = vec3(0.553, 0.690, 0.792);
uniform vec3 bank_reflection_color : source_color = vec3(0.200, 0.290, 0.180);
uniform vec3 lap_color : source_color = vec3(0.741, 0.761, 0.659);
// Profundidade (m) em que o fundo deixa de aparecer e em que o profundo se completa.
uniform float shallow_depth_m : hint_range(0.05, 3.0) = 0.32;
uniform float deep_depth_m : hint_range(0.3, 6.0) = 1.8;
// Rio mais turvo: o fundo some antes e a cor puxa para verde-oliva (so aparencia).
uniform float river_turbidity : hint_range(0.0, 1.0) = 0.65;
uniform float deep_variation : hint_range(0.0, 1.0) = 0.25;
uniform float reflection_strength : hint_range(0.0, 1.0) = 0.42;
uniform float gust_contrast : hint_range(0.0, 1.0) = 0.3;
uniform float bank_reflection_m : hint_range(0.0, 8.0) = 2.4;
uniform float bank_reflection_strength : hint_range(0.0, 1.0) = 0.5;
uniform float lap_strength : hint_range(0.0, 1.0) = 0.25;
uniform float ripple_strength : hint_range(0.0, 2.0) = 1.0;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.5;
uniform float wind_angle_deg : hint_range(-180.0, 180.0) = 35.0;
// Correnteza decorativa no rio (0 desliga): alinha a ondulacao e desenha riscos longos.
uniform float flow_strength : hint_range(0.0, 1.0) = 0.6;
uniform float water_roughness : hint_range(0.0, 1.0) = 0.12;
uniform float water_specular : hint_range(0.0, 1.0) = 0.4;
uniform float host_time_s = -1.0;

varying vec3 v_world;
varying float v_depth;
varying float v_shore;
varying vec2 v_outward;
varying float v_river;
varying vec2 v_flow;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_depth = UV.x;
	v_shore = UV.y;
	v_outward = UV2;
	v_river = COLOR.r;
	v_flow = COLOR.gb * 2.0 - 1.0;
}

// Riscos de correnteza: ruido alongado na direcao da correnteza, em duas fases cruzadas
// (periodo 12 s, divide 3600: sem salto). So aparencia.
float ws_flow_streaks(vec2 p, vec2 flow, float t, float foot) {
	const float PERIOD = 12.0;
	const float TRAVEL = 7.2;  // m por periodo
	vec2 side = vec2(-flow.y, flow.x);
	vec2 q = vec2(dot(p, flow), dot(p, side));
	float ph = fract(t / PERIOD);
	float ph2 = fract(t / PERIOD + 0.5);
	float n1 = ws_noise(vec2((q.x - ph * TRAVEL) / 6.0, q.y / 0.9), 571u);
	float n2 = ws_noise(vec2((q.x - ph2 * TRAVEL) / 6.0 + 17.3, q.y / 0.9), 577u);
	float w1 = 1.0 - abs(ph * 2.0 - 1.0);
	float n = n1 * w1 + n2 * (1.0 - w1);
	return smoothstep(0.62, 0.9, n) * ws_fade(1.8, foot);
}

void fragment() {
	vec2 p = v_world.xz;
	float foot = ws_foot(p);
	float t = host_time_s >= 0.0 ? host_time_s : TIME;
	float depth = max(v_depth, 0.0);
	float shore = max(v_shore, 0.0);
	float river = clamp(v_river, 0.0, 1.0);
	vec2 flow = length(v_flow) > 0.2 ? normalize(v_flow) : vec2(1.0, 0.0);

	// Corpo d'agua: fundo visivel no raso, profundo escuro; o rio e mais turvo.
	float n_soil = ws_noise(WS_ROT_S * p / 2.7, 101u);
	float turb = river * river_turbidity;
	float k = 1.0 - exp(-depth / (shallow_depth_m * mix(1.0, 0.35, turb)));
	vec3 bottom = mix(water_bottom_color, soil_wet_color, 0.35 * n_soil);
	vec3 mid = mix(water_mid_color, river_tint_color, turb);
	vec3 body = mix(bottom, mid, smoothstep(0.0, 0.6, k));
	body = mix(body, water_deep_color, smoothstep(0.0, 1.0, depth / deep_depth_m) * (1.0 - 0.5 * turb));
	float patches = ws_noise(WS_ROT_LOBE * p / 23.0, 113u) - 0.5;
	body *= 1.0 + patches * 0.22 * deep_variation * smoothstep(0.8, 1.8, depth);

	// Ondulacao (no rio, alinhada a correnteza decorativa) + rajadas.
	float gust = ws_gust(p, t);
	float flow_amt = river * flow_strength;
	vec2 grad = ws_ripple_gradient(p, t, foot, ripple_speed, wind_angle_deg, flow, flow_amt);
	grad *= ripple_strength * mix(0.3, 1.0, max(gust, flow_amt)) * smoothstep(0.0, 1.2, shore);
	vec3 n_water = normalize(vec3(-grad.x, 1.0, -grad.y));

	vec3 v_dir = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
	float cos_v = clamp(dot(n_water, v_dir), 0.0, 1.0);
	float fres = 0.25 + 1.6 * pow(1.0 - cos_v, 2.5);
	vec3 r_dir = reflect(-v_dir, n_water);
	vec3 sky = ws_sky(r_dir, sky_horizon_color, sky_zenith_color);
	vec3 cam_fwd = -INV_VIEW_MATRIX[2].xyz;
	vec2 cam_xz = normalize(cam_fwd.xz + vec2(1e-5, 0.0));
	vec2 outward = length(v_outward) > 0.2 ? normalize(v_outward) : vec2(0.0);
	float far_side = smoothstep(-0.1, 0.65, dot(outward, cam_xz));
	float band = (1.0 - smoothstep(0.0, bank_reflection_m, shore)) * far_side;
	float refl_amt = clamp(reflection_strength * fres * (1.0 + gust_contrast * (0.5 - gust)), 0.0, 1.0);
	float streak = ws_flow_streaks(p, flow, t, foot) * flow_amt;
	refl_amt = clamp(refl_amt + 0.07 * streak, 0.0, 1.0);
	vec3 water_col = mix(body, sky, refl_amt);
	water_col = mix(water_col, bank_reflection_color, band * bank_reflection_strength);
	float lap = (1.0 - smoothstep(0.0, 0.3, shore)) * lap_strength * ws_fade(0.9, foot);
	water_col = mix(water_col, lap_color, lap);

	ALBEDO = water_col;
	ROUGHNESS = water_roughness;
	SPECULAR = water_specular;
	NORMAL = normalize((VIEW_MATRIX * vec4(n_water, 0.0)).xyz);
}
