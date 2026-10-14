// IndexCities - WaterStudy - AMOSTRA C: superficie da agua com transparencia e reflexo suave.
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE. Proposta NAO integrada.
//
// SOMENTE APRESENTACAO. Reflexo, brilho e ondulacao nao representam nivel, vazao, correnteza,
// poluicao, captacao, recurso nem area navegavel. Ondulacao so na normal de sombreamento
// (nenhum vertice se move), com o mesmo relogio decorativo `host_time_s` que o CityWorld ja
// alimenta (para com a pausa; fecha em 3600 s), como na IA-020.
//
// Malha gerada por Source/ws_lake_c.gd a partir do centro e do raio reais do lago:
//   UV  = posicao relativa ao centro do lago (m, XZ)
//   UV2 = (profundidade do fundo sob este ponto em m, raio R do lago em m)
//
// Mistura PRE-MULTIPLICADA (testada no GL Compatibility da Godot 4.7.2: a engine nao multiplica
// a cor por ALPHA neste modo). O fundo/barranco opaco (ws_lakebed_c) ja sai visto atraves da agua;
// aqui ALPHA = F (refletancia estilizada) tira do fundo a parte refletida, e o light() soma:
//   - o ceu/margem refletidos, com o brilho do ceu acompanhando a energia do sol que o jogo ja
//     varia pela hora (de dia 1; a noite um piso, como a luz ambiente constante do jogo);
//   - o brilho das luzes pontuais (postes) como um risco vertical quebrado pela ondulacao.
// Nenhum parametro novo do host: so a luz da cena e o relogio decorativo existente.
// GL Compatibility: sem textura de tela/profundidade, sem textura nenhuma; uma passada.
shader_type spatial;
render_mode blend_premul_alpha, depth_draw_opaque, cull_back;

#include "res://Assets/WaterStudy/Shaders/ws_common.gdshaderinc"

// Ceu refletido: o fundo do jogo (#b9c9d5) no horizonte e um zenite um pouco mais azul.
uniform vec3 sky_horizon : source_color = vec3(0.725490, 0.788235, 0.835294);
uniform vec3 sky_zenith : source_color = vec3(0.470, 0.610, 0.760);
// Reflexo da margem oposta (barranco + grama) perto dela.
uniform vec3 shore_reflect_color : source_color = vec3(0.180, 0.235, 0.125);
// Altura media do que a margem reflete (barranco + grama, m) e quanto ela varia.
uniform float shore_reflect_height_m : hint_range(0.0, 3.0) = 0.9;
uniform float shore_reflect_variation : hint_range(0.0, 1.0) = 0.7;
// Fresnel estilizado: refletancia com a agua vista de cima e rasante.
uniform float fresnel_min : hint_range(0.0, 1.0) = 0.14;
uniform float fresnel_max : hint_range(0.0, 1.0) = 0.6;
uniform float fresnel_power : hint_range(1.0, 6.0) = 3.0;
// Brilho do ceu refletido a noite, em fracao do dia. O ceu (cor de fundo) e a luz ambiente do jogo
// nao mudam com a hora; so o sol enfraquece. Por isso o piso fica alto (a agua nao vira um buraco).
uniform float night_sky_floor : hint_range(0.0, 1.0) = 0.9;
// Escala do reflexo somado no light() (calibrada no GL Compatibility contra a grama do jogo).
uniform float reflection_gain : hint_range(0.0, 2.0) = 0.17;
// Risco de luz dos postes (e do sol, se alinhado): largura lateral e vertical na tela (rad).
uniform float glint_width : hint_range(0.005, 0.5) = 0.035;
uniform float glint_length : hint_range(0.005, 1.0) = 0.28;
uniform float lamp_glint_strength : hint_range(0.0, 4.0) = 2.0;
uniform float sun_glint_strength : hint_range(0.0, 4.0) = 0.6;
// Ondulacao decorativa (0 desliga).
uniform float ripple_strength : hint_range(0.0, 3.0) = 0.55;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.55;
uniform float wind_angle_deg : hint_range(-180.0, 180.0) = 35.0;
// < 0: usa TIME. >= 0: segundos do relogio decorativo do host (CityWorld: fmod(t, 3600)).
uniform float host_time_s = -1.0;

varying vec3 v_world;
varying vec2 v_local;
varying vec2 v_uv2;
varying vec3 v_refl;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_local = UV;
	v_uv2 = UV2;
}

void fragment() {
	float t = host_time_s >= 0.0 ? host_time_s : TIME;
	vec2 p = v_world.xz;
	float foot = ws_foot(p);
	// Normal ondulada (so sombreamento), com rajadas lentas que deixam areas mais crespas/lisas.
	float gust = ws_gust(p, t);
	vec2 g = ws_ripple_gradient(p, t, foot, ripple_speed, wind_angle_deg, vec2(1.0, 0.0), 0.0)
			* ripple_strength * mix(0.55, 1.35, gust);
	vec3 nw = normalize(vec3(-g.x, 1.0, -g.y));
	NORMAL = normalize((VIEW_MATRIX * vec4(nw, 0.0)).xyz);
	vec3 vw = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
	float cosv = clamp(dot(nw, vw), 0.0, 1.0);
	float fres = mix(fresnel_min, fresnel_max, pow(1.0 - cosv, fresnel_power));
	// Raio refletido: ceu, ou a margem oposta quando o raio cruza o circulo ainda baixo.
	vec3 r = reflect(-vw, nw);
	vec3 sky = ws_sky(r, sky_horizon, sky_zenith);
	float radius = v_uv2.y;
	vec2 dir = normalize(r.xz + vec2(1e-6, 0.0));
	float b = dot(v_local, dir);
	float c = dot(v_local, v_local) - radius * radius;
	float t_exit = -b + sqrt(max(b * b - c, 0.0));
	float rise = r.y / max(length(r.xz), 1e-4);
	float hit_h = t_exit * rise;
	vec2 shore_world = p + dir * t_exit;
	float veg = shore_reflect_height_m * (1.0 - shore_reflect_variation
			+ 2.0 * shore_reflect_variation * ws_noise(WS_ROT_JAG * shore_world / 2.7, 811u));
	float shore = 1.0 - smoothstep(veg * 0.6, veg * 1.25, hit_h);
	v_refl = mix(sky, shore_reflect_color, shore) * fres;
	ALBEDO = vec3(0.0);
	ALPHA = fres;
	ROUGHNESS = 0.1;
	SPECULAR = 0.5;
}

// Risco de reflexo de uma luz: desvio entre a direcao da luz e o raio refletido, medido na tela
// (x = lateral, y = vertical). Alongado na vertical como o reflexo de luzes em agua ondulada.
float ws_glint(vec3 light_dir, vec3 view_dir, vec3 n) {
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
		SPECULAR_LIGHT += ws_glint(LIGHT, VIEW, NORMAL) * sun_glint_strength * ATTENUATION * LIGHT_COLOR / PI * 0.25;
	} else {
		SPECULAR_LIGHT += ws_glint(LIGHT, VIEW, NORMAL) * lamp_glint_strength * ATTENUATION * LIGHT_COLOR;
	}
}
