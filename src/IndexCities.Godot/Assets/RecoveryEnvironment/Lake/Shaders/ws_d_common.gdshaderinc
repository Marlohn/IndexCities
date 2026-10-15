#ifndef WS_D_COMMON
#define WS_D_COMMON
// IndexCities - WaterStudy - Lago irregular: funcoes compartilhadas pela margem (ws_d_shore), pelo
// fundo (ws_d_bed) e pela agua (ws_d_water): hash, ruido, ondulacao, rajadas e ceu. Estudo visual da frente de assets (IA).
// Revisao humana: PENDENTE. Proposta NAO integrada.
//
// SOMENTE APRESENTACAO. Nada aqui representa nivel, profundidade, vazao, correnteza, poluicao,
// captacao, recurso, area navegavel, vegetacao da regra ambiental ou recuo de obra.
//
// Unidades: metros, Y para cima, padroes no plano XZ do MUNDO (continuos entre malhas e
// independentes de camera, hora e seed). GL Compatibility (GLSL 3.30 / ES 3.0): so aritmetica,
// uint e fwidth; sem texturas, tela ou profundidade.
//
// Pasta independente: hash, ruido, ondulacao e rajadas foram trazidos da amostra C/A
// (Shaders/ws_common.gdshaderinc, mesmo estudo) para D nao depender de arquivos historicos.
//
// O espelho da grama e a paleta da margem ficam em ws_d_bank.gdshaderinc (so margem e fundo).

// ---------------------------------------------------------------------------------------------
// Hash inteiro e ruido de valor do estudo.
float d_hash(ivec2 cell, uint salt) {
	uvec2 u = uvec2(cell + ivec2(65536));
	uint h = u.x * 0x9F3B6A2Du + u.y * 0x6C1E85B7u + salt * 0xD2A47C19u;
	h ^= h >> 16u;
	h *= 0x4E8F2B63u;
	h ^= h >> 14u;
	h *= 0xA7356E8Bu;
	h ^= h >> 15u;
	return float(h >> 8u) * (1.0 / 16777216.0);
}

// Ruido de valor 2D em [0, 1], interpolacao Hermite.
float d_noise(vec2 p, uint salt) {
	vec2 i = floor(p);
	vec2 f = p - i;
	ivec2 c = ivec2(i);
	float a = d_hash(c, salt);
	float b = d_hash(c + ivec2(1, 0), salt);
	float d = d_hash(c + ivec2(0, 1), salt);
	float e = d_hash(c + ivec2(1, 1), salt);
	vec2 w = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, w.x), mix(d, e, w.x), w.y);
}

// Rotacoes fixas (nenhuma camada alinhada a X/Z, para nao lembrar a grade de construcao).
const mat2 D_ROT_A = mat2(vec2(0.8910, 0.4540), vec2(-0.4540, 0.8910));   //  27 graus
const mat2 D_ROT_B = mat2(vec2(0.5299, -0.8480), vec2(0.8480, 0.5299));   // -58 graus
const mat2 D_ROT_C = mat2(vec2(0.3256, 0.9455), vec2(-0.9455, 0.3256));   //  71 graus
const mat2 D_ROT_D = mat2(vec2(0.9703, -0.2419), vec2(0.2419, 0.9703));   // -14 graus
const mat2 D_ROT_E = mat2(vec2(0.7193, 0.6947), vec2(-0.6947, 0.7193));   //  44 graus
const mat2 D_ROT_F = mat2(vec2(0.1219, -0.9925), vec2(0.9925, 0.1219));   // -83 graus

// Tamanho de um pixel no chao (m). Chamar so em fluxo uniforme (fwidth).
float d_foot(vec2 p) {
	vec2 fw = fwidth(p);
	return max(fw.x, fw.y);
}

// 1 enquanto um detalhe de `size_m` ocupa ~8 px ou mais; 0 quando cai para ~3 px (anti-cintilacao).
float d_fade(float size_m, float foot_m) {
	return 1.0 - smoothstep(size_m * 0.125, size_m * 0.33, foot_m);
}

// ---------------------------------------------------------------------------------------------
// Ondulacao decorativa (so normal de sombreamento, nunca desloca vertices).

// Frequencia angular com numero inteiro de ciclos em 3600 s: sem salto quando o relogio
// decorativo do host (host_time_s = fmod(t, 3600)) da a volta.
float d_loop_omega(float omega) {
	return TAU * round(omega * 3600.0 / TAU) / 3600.0;
}

// Gradiente (dh/dx, dh/dz) de quatro trens de onda com frentes entortadas por ruido fixo.
vec2 d_ripple_gradient(vec2 p, float t, float foot, float speed, float wind_deg) {
	const float LEN[4] = float[](4.7, 2.9, 1.9, 1.25);
	const float TURN[4] = float[](0.0, 0.78, -0.92, 1.73);
	const float SLOPE[4] = float[](0.055, 0.048, 0.040, 0.030);
	const float PHASE[4] = float[](0.0, 2.1, 4.4, 0.9);
	const float BEND[4] = float[](3.0, 3.6, 4.3, 5.1);
	float wind = radians(wind_deg);
	vec2 bend = vec2(d_noise(D_ROT_D * p / 7.1, 431u), d_noise(D_ROT_E * p / 10.3, 433u)) - 0.5;
	vec2 g = vec2(0.0);
	for (int i = 0; i < 4; i++) {
		float a = wind + TURN[i];
		vec2 dir = vec2(cos(a), sin(a));
		float k = TAU / LEN[i];
		float w = d_loop_omega(sqrt(9.81 * k) * speed);
		float warp = (i % 2 == 0) ? bend.x : bend.y;
		float ph = k * dot(dir, p) - w * t + PHASE[i] + BEND[i] * warp;
		g += dir * (SLOPE[i] * cos(ph) * d_fade(LEN[i], foot));
	}
	return g;
}

// Rajadas de vento: manchas lentas de agua mais crespa (1) ou mais lisa (0). Orbita de 900 s.
float d_gust(vec2 p, float t) {
	float orbit = TAU * t / 900.0;
	vec2 drift = 34.0 * vec2(cos(orbit), sin(orbit));
	float g1 = d_noise(D_ROT_F * (p + drift) / 19.0, 439u);
	float g2 = d_noise(D_ROT_A * (p - drift * 0.6) / 8.5, 443u);
	return smoothstep(0.28, 0.82, 0.7 * g1 + 0.3 * g2);
}

// Ceu estilizado refletido (o jogo usa fundo de cor lisa; o zenite e um pouco mais azul).
vec3 d_sky(vec3 r, vec3 horizon, vec3 zenith) {
	return mix(horizon, zenith, smoothstep(0.05, 0.9, r.y));
}

#endif
