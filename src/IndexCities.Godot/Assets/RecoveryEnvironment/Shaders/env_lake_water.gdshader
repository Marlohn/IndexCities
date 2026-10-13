// IndexCities - RecoveryEnvironment - agua natural de lago com ondulacao discreta.
// Amostra da frente de assets (IA). Revisao humana: PENDENTE.
//
// SOMENTE ESTETICO. Nao simula nivel, vazao, captacao, capacidade, producao, consumo,
// qualidade, poluicao nem profundidade real. A faixa clara junto a margem e as manchas mais
// escuras no meio sao so aparencia: nao indicam area navegavel, risco ou recuo de construcao.
//
// Feito para a malha atual do lago no CityWorld (CylinderMesh: tampa em leque a partir do
// centro, top_radius = R, bottom_radius = R + 1, height = 0,15). NAO desloca vertices:
// silhueta, raio, nivel e colisao/picking ficam exatamente como estao. A ondulacao e so
// de sombreamento (normal perturbada no fragmento) e o gradiente da margem e so de cor.
//
// Distancia ate a margem sem parametro do host: na tampa em leque, o varying `v_rim`
// (0 no vertice central, 1 nos vertices da borda) interpola para d / R, entao
// R ~ d / v_rim (erro < 0,2 % com 64 segmentos). O talude lateral do cilindro (normal
// inclinada) conta como margem. Se a malha mudar (outro formato de lago), revisar isso.
//
// Tempo: por padrao usa TIME (tempo real do motor). As frequencias sao ajustadas para
// um numero inteiro de ciclos em 3600 s, o rollover padrao de TIME, entao nao ha salto
// visivel quando o relogio do shader reinicia. Para a agua parar com o jogo pausado (ou
// seguir outro relogio), o host pode passar `host_time_s` >= 0 (use fmod(segundos, 3600)).
//
// Compatibilidade: Godot 4.7, renderizador GL Compatibility (OpenGL 3.3 / GLES 3), o do
// jogo; os outros renderizadores nao foram testados. Opaco, sem textura de tela/profundidade
// e sem transparencia.
shader_type spatial;

// Cores. Fundo e raso ficam na familia azul-esverdeada; a borda e um pouco mais clara.
uniform vec3 deep_color : source_color = vec3(0.231373, 0.533333, 0.596078);
uniform vec3 shallow_color : source_color = vec3(0.352941, 0.647059, 0.619608);
uniform vec3 shore_color : source_color = vec3(0.474510, 0.725490, 0.658824);
// Cor "refletida" (cor de fundo do ambiente do jogo, #b9c9d5). Misturada ao albedo por um
// Fresnel suave; sem textura de reflexo nem sonda de ambiente.
uniform vec3 sky_color : source_color = vec3(0.725490, 0.788235, 0.835294);

// Faixa rasa: largura media a partir da borda da tampa e onde a cor de fundo se completa (m).
uniform float shallow_width_m : hint_range(0.5, 20.0) = 3.2;
uniform float deep_distance_m : hint_range(2.0, 60.0) = 9.0;
// Linha clara rente a borda real da malha (m).
uniform float shore_line_m : hint_range(0.0, 3.0) = 0.6;
// Irregularidade so da COR da faixa rasa (a borda da malha continua circular).
uniform float edge_wobble_m : hint_range(0.0, 6.0) = 2.6;
// Manchas mais escuras e irregulares no fundo (0 = fundo uniforme).
uniform float deep_variation : hint_range(0.0, 1.0) = 0.6;

// Ondulacao: 0 desliga (agua parada com gradiente e reflexo).
uniform float ripple_strength : hint_range(0.0, 2.0) = 1.0;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.55;
uniform float wind_angle_deg : hint_range(-180.0, 180.0) = 35.0;
// Facetas low-poly opcionais: 0 = ondulacao suave; > 0 = normal constante por triangulo
// de lado ~facet_size_m (volta a suave quando o triangulo fica pequeno na tela).
uniform float facet_size_m : hint_range(0.0, 6.0) = 0.0;

uniform float reflection_strength : hint_range(0.0, 1.0) = 0.6;
uniform float reflection_power : hint_range(1.0, 5.0) = 1.8;
uniform float roughness_value : hint_range(0.0, 1.0) = 0.3;
uniform float specular_value : hint_range(0.0, 1.0) = 0.28;

// < 0: usa TIME. >= 0: tempo em segundos fornecido pelo host (ex.: relogio que pausa).
uniform float host_time_s = -1.0;

varying vec3 v_world;
varying vec2 v_centre;
varying float v_rim;
varying float v_bank;

// Hash inteiro original (mesma familia do shader da grama, outras constantes de sal).
float env_cell_hash(ivec2 cell, uint salt) {
	uvec2 u = uvec2(cell + ivec2(65536));
	uint h = u.x * 0x8F2D5C37u + u.y * 0x4CF5AD43u + salt * 0x2E6B1A95u;
	h ^= h >> 15u;
	h *= 0xE3A9B0D1u;
	h ^= h >> 13u;
	h *= 0x68E31DA5u;
	h ^= h >> 16u;
	return float(h >> 8u) * (1.0 / 16777216.0);
}

float env_value_noise(vec2 p, uint salt) {
	vec2 i = floor(p);
	vec2 f = p - i;
	ivec2 c = ivec2(i);
	float a = env_cell_hash(c, salt);
	float b = env_cell_hash(c + ivec2(1, 0), salt);
	float d = env_cell_hash(c + ivec2(0, 1), salt);
	float e = env_cell_hash(c + ivec2(1, 1), salt);
	vec2 w = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, w.x), mix(d, e, w.x), w.y);
}

const mat2 ROT_W = mat2(vec2(0.7314, 0.6820), vec2(-0.6820, 0.7314));  // 43 graus
const mat2 ROT_G = mat2(vec2(0.4226, -0.9063), vec2(0.9063, 0.4226));  // -65 graus
const mat2 ROT_K = mat2(vec2(0.9455, 0.3256), vec2(-0.3256, 0.9455));  // 19 graus

// Quatro trens de onda: comprimento (m), desvio do vento (rad), inclinacao maxima (rad),
// fase inicial e curvatura das frentes (rad de fase por unidade de ruido). Direcoes bem
// abertas e frentes curvas evitam listras paralelas. Velocidade pela dispersao de aguas
// profundas (w = sqrt(g k)), reduzida por ripple_speed.
const vec4 WAVE_LENGTH = vec4(4.3, 2.7, 1.75, 1.15);
const vec4 WAVE_TURN = vec4(0.0, 0.85, -0.7, 1.9);
const vec4 WAVE_SLOPE = vec4(0.050, 0.046, 0.040, 0.032);
const vec4 WAVE_PHASE = vec4(0.0, 1.7, 4.1, 2.6);
const vec4 WAVE_BEND = vec4(3.2, 3.8, 4.4, 5.0);

// Frequencia angular com numero inteiro de ciclos em 3600 s (sem salto no rollover).
// ripple_speed = 0 da 0 ciclos: ondulacao parada.
float env_loop_omega(float omega) {
	return TAU * round(omega * 3600.0 / TAU) / 3600.0;
}

// Mesmo criterio da grama: 1 com >= ~8 px por comprimento, 0 com ~3 px.
float env_scale_fade(float size_m, float foot_m) {
	return 1.0 - smoothstep(size_m * 0.125, size_m * 0.33, foot_m);
}

// Gradiente (dh/dx, dh/dz) da superficie ondulada no ponto q. So sombreamento.
vec2 env_ripple_gradient(vec2 q, float t, float foot) {
	float wind = radians(wind_angle_deg);
	// Dois campos lentos de curvatura (fixos no espaco) entortam as frentes de onda.
	vec2 bend = vec2(env_value_noise(ROT_K * q / 6.5, 79u), env_value_noise(ROT_W * q / 9.0, 83u)) - 0.5;
	vec2 grad = vec2(0.0);
	for (int i = 0; i < 4; i++) {
		float a = wind + WAVE_TURN[i];
		vec2 dir = vec2(cos(a), sin(a));
		float k = TAU / WAVE_LENGTH[i];
		float w = env_loop_omega(sqrt(9.81 * k) * ripple_speed);
		float warp = (i == 0 || i == 2) ? bend.x : bend.y;
		float phase = k * dot(dir, q) - w * t + WAVE_PHASE[i] + WAVE_BEND[i] * warp;
		grad += dir * (WAVE_SLOPE[i] * cos(phase) * env_scale_fade(WAVE_LENGTH[i], foot));
	}
	// Rajadas: manchas de ondulacao mais forte/fraca que derivam devagar num circulo de
	// 30 m com periodo de 900 s (tambem fecha em 3600 s).
	float orbit = TAU * t / 900.0;
	vec2 drift = 30.0 * vec2(cos(orbit), sin(orbit));
	float gust = env_value_noise(ROT_G * (q + drift) / 17.0, 71u);
	return grad * mix(0.3, 1.0, smoothstep(0.2, 0.8, gust));
}

// Centro do triangulo (grade triangular de lado `size`) que contem q.
vec2 env_facet_centre(vec2 q, float size) {
	// Base do reticulado: e1 = (1, 0), e2 = (0,5, sqrt(3)/2), escalada por `size`.
	vec2 s = vec2(q.x - q.y * 0.57735027, q.y * 1.15470054) / size;
	vec2 cell = floor(s);
	vec2 f = s - cell;
	vec2 bary = (f.x + f.y < 1.0) ? vec2(1.0 / 3.0) : vec2(2.0 / 3.0);
	vec2 c = (cell + bary) * size;
	return vec2(c.x + c.y * 0.5, c.y * 0.8660254);
}

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	// Centro do lago no mundo (origem da malha); constante em toda a malha.
	v_centre = MODEL_MATRIX[3].xz;
	// Vertice central da tampa: 0; vertices da borda e do talude: 1.
	v_rim = length(VERTEX.xz) > 0.01 ? 1.0 : 0.0;
	// Talude lateral (normal inclinada ~8,5 graus para fora): 1; tampa (normal +Y): 0.
	v_bank = NORMAL.y < 0.995 ? 1.0 : 0.0;
}

void fragment() {
	vec2 p = v_world.xz;
	vec2 fw = fwidth(p);
	float foot = max(fw.x, fw.y);
	float t = host_time_s >= 0.0 ? host_time_s : TIME;

	// Distancia ate a margem (m), estimada pelo leque da tampa; no talude vale 0.
	float rim = clamp(v_rim, 0.001, 1.0);
	float d = length(p - v_centre);
	float shore_d = mix(d / rim - d, 0.0, step(0.5, v_bank));

	// Cor: faixa rasa clara de largura irregular -> fundo quase uniforme com manchas suaves.
	float wobble = ((env_value_noise(ROT_W * p / 11.0, 61u) - 0.5) * 1.4
			+ (env_value_noise(ROT_G * p / 4.3, 67u) - 0.5) * 0.6) * edge_wobble_m;
	float depth_d = max(shore_d + wobble, 0.0);
	float to_deep = smoothstep(shallow_width_m * 0.35, deep_distance_m, depth_d);
	vec3 col = mix(shallow_color, deep_color, to_deep);
	float patches = smoothstep(0.35, 0.85, env_value_noise(ROT_K * p / 21.0, 73u));
	col = mix(col, deep_color * 0.84, to_deep * patches * deep_variation);
	// Linha clara rente a borda (sem irregularidade: acompanha a borda real da malha).
	col = mix(shore_color, col, smoothstep(0.0, shore_line_m, shore_d));

	// Ondulacao: normal perturbada (suave ou facetada), mais fraca no talude.
	vec2 grad = env_ripple_gradient(p, t, foot);
	if (facet_size_m > 0.0) {
		float facet_on = env_scale_fade(facet_size_m * 1.5, foot);
		if (facet_on > 0.0) {
			grad = mix(grad, env_ripple_gradient(env_facet_centre(p, facet_size_m), t, foot), facet_on);
		}
	}
	grad *= ripple_strength * (1.0 - 0.75 * v_bank);
	vec3 n_base = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
	vec3 n_world = normalize(n_base + vec3(-grad.x, 0.0, -grad.y));
	vec3 n_view = normalize((VIEW_MATRIX * vec4(n_world, 0.0)).xyz);

	// Reflexo estilizado da cor do ambiente: Fresnel suave com a normal ondulada. E albedo
	// (iluminado pelo sol e pelo ambiente do jogo), entao escurece a noite junto com o resto.
	float facing = clamp(dot(n_view, VIEW), 0.0, 1.0);
	float fres = pow(1.0 - facing, reflection_power);
	col = mix(col, sky_color, clamp(fres * reflection_strength, 0.0, 1.0));

	ALBEDO = col;
	NORMAL = n_view;
	ROUGHNESS = roughness_value;
	SPECULAR = specular_value;
}
