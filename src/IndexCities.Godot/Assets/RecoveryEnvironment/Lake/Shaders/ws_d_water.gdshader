// IndexCities - WaterStudy - Lago irregular: superficie da agua (transparente, reflexo suave).
// Integrado a partir do estudo D da frente de assets. Estetica pendente de revisao humana.
//
// SOMENTE APRESENTACAO. Reflexo, brilho e ondulacao nao representam nivel, vazao, correnteza,
// poluicao, captacao, recurso nem area navegavel. A ondulacao e so da normal de sombreamento
// (nenhum vertice se move) e usa SO o relogio decorativo `host_time_s` do host (o CityWorld ja o
// alimenta com fmod(t, 3600) e ele para com a pausa, como na IA-020). Sem TIME do motor e sem
// tempo de economia: sem host, a agua fica parada.
//
// Malha `water` gerada por Source/ws_lake_d.gd, cobrindo exatamente o poligono de agua:
//   UV  = vetor (dx, dz) ate o ponto mais proximo da margem (m)
//   UV2 = (profundidade do fundo sob o ponto em m; altura que essa margem atinge em 1,2 m, em m)
//
// Mistura PRE-MULTIPLICADA (como na amostra C, testada no GL Compatibility da Godot 4.7.2): o fundo
// opaco (ws_d_bed) ja sai visto atraves da agua; aqui ALPHA = refletancia estilizada, que tira do
// fundo a parte refletida, e o light() soma:
//   - o ceu do jogo refletido, ou a margem quando o raio refletido bate nela ainda baixo (calculado
//     com o vetor ate a margem mais proxima: serve para qualquer contorno, sem raio de circulo);
//   - o brilho das luzes pontuais (postes) como um risco vertical quebrado pela ondulacao.
// GL Compatibility: sem textura de tela/profundidade, sem textura nenhuma; uma passada.
shader_type spatial;
render_mode blend_premul_alpha, depth_draw_opaque, cull_back;

#include "res://Assets/RecoveryEnvironment/Lake/Shaders/ws_d_common.gdshaderinc"

// Ceu refletido: o fundo do jogo (#b9c9d5) no horizonte e um zenite um pouco mais azul.
uniform vec3 sky_horizon : source_color = vec3(0.725490, 0.788235, 0.835294);
uniform vec3 sky_zenith : source_color = vec3(0.500, 0.640, 0.780);
// Reflexo da margem (grama e barranco vistos de baixo, mais escuros).
uniform vec3 shore_reflect_color : source_color = vec3(0.200, 0.270, 0.140);
// Fresnel estilizado: refletancia com a agua vista de cima e rasante.
uniform float fresnel_min : hint_range(0.0, 1.0) = 0.16;
uniform float fresnel_max : hint_range(0.0, 1.0) = 0.6;
uniform float fresnel_power : hint_range(1.0, 6.0) = 3.0;
// Largura (m) em que o reflexo entra a partir da linha d'agua (so antisserrilhado da borda).
uniform float edge_soft_m : hint_range(0.0, 1.0) = 0.12;
// Brilho do ceu refletido a noite, em fracao do dia (o ceu e a luz ambiente do jogo nao mudam com
// a hora; so o sol enfraquece).
uniform float night_sky_floor : hint_range(0.0, 1.0) = 0.55;
// Escala do reflexo somado no light().
uniform float reflection_gain : hint_range(0.0, 2.0) = 0.2;
// Risco de luz dos postes (e do sol, se alinhado): largura lateral e vertical na tela (rad).
uniform float glint_width : hint_range(0.005, 0.5) = 0.035;
uniform float glint_length : hint_range(0.005, 1.0) = 0.28;
uniform float lamp_glint_strength : hint_range(0.0, 4.0) = 2.0;
uniform float sun_glint_strength : hint_range(0.0, 4.0) = 0.6;
// Brisa: faixas largas e lentas de agua mais lisa (reflete mais o ceu) ou mais crespa, alongadas na
// direcao do vento. Visiveis de longe (zoom 110), onde a ondulacao fina ja some; deslizam so com o
// relogio decorativo (orbita de 1200 s, fecha em 3600 s). 0 desliga.
uniform float sheen_strength : hint_range(0.0, 1.0) = 0.38;
// Ondulacao decorativa (0 desliga).
uniform float ripple_strength : hint_range(0.0, 3.0) = 0.55;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.55;
uniform float wind_angle_deg : hint_range(-180.0, 180.0) = 35.0;
// Relogio decorativo do host (s, 0..3600). Parado se o host nao alimentar.
uniform float host_time_s = 0.0;

varying vec3 v_world;
varying vec2 v_shore;
varying vec2 v_uv2;
varying vec3 v_refl;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_shore = UV;
	v_uv2 = UV2;
}

void fragment() {
	float t = host_time_s;
	vec2 p = v_world.xz;
	float foot = d_foot(p);
	// Normal ondulada (so sombreamento), com rajadas lentas que deixam areas mais crespas/lisas.
	float gust = d_gust(p, t);
	vec2 g = d_ripple_gradient(p, t, foot, ripple_speed, wind_angle_deg) * ripple_strength * mix(0.55, 1.35, gust);
	vec3 nw = normalize(vec3(-g.x, 1.0, -g.y));
	NORMAL = normalize((VIEW_MATRIX * vec4(nw, 0.0)).xyz);
	vec3 vw = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
	float cosv = clamp(dot(nw, vw), 0.0, 1.0);
	float fres = mix(fresnel_min, fresnel_max, pow(1.0 - cosv, fresnel_power));
	vec2 wdir = vec2(cos(radians(wind_angle_deg)), sin(radians(wind_angle_deg)));
	float orbit = TAU * t / 1200.0;
	vec2 pw = p + 22.0 * vec2(cos(orbit), sin(orbit));
	vec2 qs = vec2(dot(pw, wdir), dot(pw, vec2(-wdir.y, wdir.x)));
	float sheen = smoothstep(0.3, 0.72, 0.65 * d_noise(qs / vec2(30.0, 9.0), 451u) + 0.35 * d_noise(qs / vec2(13.0, 4.5), 457u));
	fres *= mix(1.0 - sheen_strength, 1.0 + sheen_strength, sheen);
	// Raio refletido: ceu, ou a margem quando o raio chega nela ainda abaixo do alto do barranco.
	vec3 r = reflect(-vw, nw);
	vec3 sky = d_sky(r, sky_horizon, sky_zenith);
	float s = length(v_shore);
	vec2 to_shore = v_shore / max(s, 1e-4);
	vec2 rdir = normalize(r.xz + vec2(1e-6, 0.0));
	float facing = dot(rdir, to_shore);
	float rise = r.y / max(length(r.xz), 1e-4);
	float shore = 0.0;
	// So perto da margem para onde o raio refletido vai (o resto do lago reflete so o ceu).
	if (facing > 0.05 && s * rise < 1.5 * v_uv2.y * facing) {
		float t_exit = s / facing;
		float hit_h = t_exit * rise;
		float bank_h = v_uv2.y * (0.75 + 0.5 * d_noise(D_ROT_C * (p + rdir * t_exit) / 2.7, 811u));
		shore = 1.0 - smoothstep(bank_h * 0.6, bank_h * 1.15, hit_h);
	}
	v_refl = mix(sky, shore_reflect_color, shore) * fres;
	ALBEDO = vec3(0.0);
	ALPHA = fres * smoothstep(0.0, edge_soft_m, s);
	ROUGHNESS = 0.1;
	SPECULAR = 0.5;
}

// Risco de reflexo de uma luz: desvio entre a direcao da luz e o raio refletido, medido na tela
// (x = lateral, y = vertical). Alongado na vertical como o reflexo de luzes em agua ondulada.
float d_glint(vec3 light_dir, vec3 view_dir, vec3 n) {
	vec3 refl = reflect(-view_dir, n);
	vec3 d = light_dir - refl;
	return exp(-(d.x * d.x) / (glint_width * glint_width) - (d.y * d.y) / (glint_length * glint_length));
}

void light() {
	if (LIGHT_IS_DIRECTIONAL) {
		// Energia do sol (o jogo varia de 0,12 a noite a 1,0 ao meio-dia); LIGHT_COLOR = cor * energia * PI.
		float energy = max(LIGHT_COLOR.r, max(LIGHT_COLOR.g, LIGHT_COLOR.b)) / PI;
		float sky = mix(night_sky_floor, 1.0, smoothstep(0.12, 0.9, energy));
		SPECULAR_LIGHT += v_refl * sky * reflection_gain;
		SPECULAR_LIGHT += d_glint(LIGHT, VIEW, NORMAL) * sun_glint_strength * ATTENUATION * LIGHT_COLOR / PI * 0.25;
	} else {
		SPECULAR_LIGHT += d_glint(LIGHT, VIEW, NORMAL) * lamp_glint_strength * ATTENUATION * LIGHT_COLOR;
	}
}
