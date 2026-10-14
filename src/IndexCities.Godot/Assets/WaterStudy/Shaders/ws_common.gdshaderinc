// IndexCities - WaterStudy - funcoes compartilhadas pelas amostras A e B.
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE.
//
// SOMENTE APRESENTACAO. Nada aqui representa nivel, profundidade, vazao, correnteza, poluicao,
// captacao, recurso, area navegavel ou qualquer estado da simulacao.
//
// Unidades: metros, Y para cima, padroes no plano XZ do MUNDO (continuos entre malhas e
// independentes de camera, hora e seed). Compativel com o renderizador GL Compatibility
// (GLSL 3.30 / ES 3.0): so aritmetica, uint, fwidth; sem texturas, tela ou profundidade.
//
// A largura da margem `ws_bank_width` e o hash `ws_hash` sao espelhados, com as MESMAS
// constantes, em Source/ws_shore_geometry.gd. A amostra B constroi em geometria a mesma linha
// d'agua que a amostra A pinta. Se mudar uma, mude a outra.

// ---------------------------------------------------------------------------------------------
// Hash inteiro e ruido de valor proprios do estudo (constantes impares escolhidas para ele).
const ivec2 WS_HASH_OFFSET = ivec2(65536);

float ws_hash(ivec2 cell, uint salt) {
	uvec2 u = uvec2(cell + WS_HASH_OFFSET);
	uint h = u.x * 0x9F3B6A2Du + u.y * 0x6C1E85B7u + salt * 0xD2A47C19u;
	h ^= h >> 16u;
	h *= 0x4E8F2B63u;
	h ^= h >> 14u;
	h *= 0xA7356E8Bu;
	h ^= h >> 15u;
	return float(h >> 8u) * (1.0 / 16777216.0);
}

// Ruido de valor 2D em [0, 1], interpolacao Hermite.
float ws_noise(vec2 p, uint salt) {
	vec2 i = floor(p);
	vec2 f = p - i;
	ivec2 c = ivec2(i);
	float a = ws_hash(c, salt);
	float b = ws_hash(c + ivec2(1, 0), salt);
	float d = ws_hash(c + ivec2(0, 1), salt);
	float e = ws_hash(c + ivec2(1, 1), salt);
	vec2 w = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, w.x), mix(d, e, w.x), w.y);
}

// Rotacoes fixas (nenhuma camada alinhada a X/Z, para nao lembrar a grade de construcao).
const mat2 WS_ROT_BAY = mat2(vec2(0.8910, 0.4540), vec2(-0.4540, 0.8910));   //  27 graus
const mat2 WS_ROT_LOBE = mat2(vec2(0.5299, -0.8480), vec2(0.8480, 0.5299));  // -58 graus
const mat2 WS_ROT_JAG = mat2(vec2(0.3256, 0.9455), vec2(-0.9455, 0.3256));   //  71 graus
const mat2 WS_ROT_S = mat2(vec2(0.9703, -0.2419), vec2(0.2419, 0.9703));     // -14 graus
const mat2 WS_ROT_T = mat2(vec2(0.7193, 0.6947), vec2(-0.6947, 0.7193));     //  44 graus
const mat2 WS_ROT_U = mat2(vec2(0.1219, -0.9925), vec2(0.9925, 0.1219));     // -83 graus

// Tamanho de um pixel no chao (m).
float ws_foot(vec2 p) {
	vec2 fw = fwidth(p);
	return max(fw.x, fw.y);
}

// 1 enquanto um detalhe de `size_m` ocupa ~8 px ou mais; 0 quando cai para ~3 px (anti-cintilacao).
float ws_fade(float size_m, float foot_m) {
	return 1.0 - smoothstep(size_m * 0.125, size_m * 0.33, foot_m);
}

// ---------------------------------------------------------------------------------------------
// Espelho EXATO da grama integrada (RecoveryEnvironment/Shaders/env_grass_soft.gdshader, main
// 528d30d): mesmo hash, sais, rotacoes e formula. Serve para a borda do disco (A) e o chao da
// bacia (B) terem a mesma cor do chao do jogo, sem costura. Os parametros entram como uniforms
// com os mesmos padroes e a mesma dica source_color do original. Se env_grass_soft mudar,
// atualizar este espelho.
float wsg_env_hash(ivec2 cell, uint salt) {
	uvec2 u = uvec2(cell + ivec2(65536));
	uint h = u.x * 0x8F2D5C37u + u.y * 0x4CF5AD43u + salt * 0x2E6B1A95u;
	h ^= h >> 15u;
	h *= 0xE3A9B0D1u;
	h ^= h >> 13u;
	h *= 0x68E31DA5u;
	h ^= h >> 16u;
	return float(h >> 8u) * (1.0 / 16777216.0);
}

float wsg_env_noise(vec2 p, uint salt) {
	vec2 i = floor(p);
	vec2 f = p - i;
	ivec2 c = ivec2(i);
	float a = wsg_env_hash(c, salt);
	float b = wsg_env_hash(c + ivec2(1, 0), salt);
	float d = wsg_env_hash(c + ivec2(0, 1), salt);
	float e = wsg_env_hash(c + ivec2(1, 1), salt);
	vec2 w = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, w.x), mix(d, e, w.x), w.y);
}

const mat2 WSG_ROT_A = mat2(vec2(0.8480, 0.5299), vec2(-0.5299, 0.8480));
const mat2 WSG_ROT_B = mat2(vec2(0.2588, -0.9659), vec2(0.9659, 0.2588));
const mat2 WSG_ROT_C = mat2(vec2(0.6018, 0.7986), vec2(-0.7986, 0.6018));
const mat2 WSG_ROT_D = mat2(vec2(0.9877, -0.1564), vec2(0.1564, 0.9877));

vec3 ws_grass_mirror(vec2 p, float foot, vec3 base, vec3 warm, vec3 cool, float variation,
		float patch_m, float detail_m, float grain_m, float grain_strength) {
	float macro = wsg_env_noise(WSG_ROT_A * p / patch_m, 11u) * 0.67
			+ wsg_env_noise(WSG_ROT_B * p / (patch_m * 0.47), 23u) * 0.33;
	float meso = wsg_env_noise(WSG_ROT_C * p / detail_m, 37u);
	float meso_w = 0.22 * ws_fade(detail_m, foot);
	float n = mix(macro, meso, meso_w);
	float t = smoothstep(0.22, 0.78, n);
	t = 0.5 + (t - 0.5) * variation;
	vec3 col = t < 0.5 ? mix(cool, base, t * 2.0) : mix(base, warm, (t - 0.5) * 2.0);
	float grain = wsg_env_noise(WSG_ROT_D * p / grain_m, 53u) - 0.5;
	col *= 1.0 + grain * 2.0 * grain_strength * ws_fade(grain_m, foot);
	return col;
}

// ---------------------------------------------------------------------------------------------
// Largura da margem (m), medida a partir do circulo real do lago para DENTRO. A linha d'agua
// fica onde (R - distancia ao centro) == ws_bank_width. Baias grandes + lobos + recorte fino.
// Espelhada em ws_shore_geometry.gd::bank_width().
float ws_bank_width(vec2 p, float wmin, float wmax, float jag_m, float lobe_m, float bay_m) {
	float bay = ws_noise(WS_ROT_BAY * p / bay_m, 401u);
	float lobe = ws_noise(WS_ROT_LOBE * p / lobe_m, 409u);
	float n = 0.6 * bay + 0.4 * lobe;
	float w = wmin + (wmax - wmin) * smoothstep(0.3, 0.7, n);
	w += jag_m * (ws_noise(WS_ROT_JAG * p / 4.6, 419u) - 0.5);
	return max(w, 0.25);
}

// ---------------------------------------------------------------------------------------------
// Ondulacao decorativa (so normal de sombreamento, nunca desloca vertices).

// Frequencia angular com numero inteiro de ciclos em 3600 s: sem salto quando o relogio
// decorativo (host_time_s = fmod(t, 3600)) ou o TIME do motor dao a volta.
float ws_loop_omega(float omega) {
	return TAU * round(omega * 3600.0 / TAU) / 3600.0;
}

// Gradiente (dh/dx, dh/dz) de quatro trens de onda com frentes entortadas por ruido fixo.
// flow_amt > 0 alinha os trens a `flow_dir` (aparencia de correnteza na amostra B; decorativo).
vec2 ws_ripple_gradient(vec2 p, float t, float foot, float speed, float wind_deg,
		vec2 flow_dir, float flow_amt) {
	const float LEN[4] = float[](4.7, 2.9, 1.9, 1.25);
	const float TURN[4] = float[](0.0, 0.78, -0.92, 1.73);
	const float SLOPE[4] = float[](0.055, 0.048, 0.040, 0.030);
	const float PHASE[4] = float[](0.0, 2.1, 4.4, 0.9);
	const float BEND[4] = float[](3.0, 3.6, 4.3, 5.1);
	float wind = radians(wind_deg);
	vec2 bend = vec2(ws_noise(WS_ROT_S * p / 7.1, 431u), ws_noise(WS_ROT_T * p / 10.3, 433u)) - 0.5;
	vec2 g = vec2(0.0);
	for (int i = 0; i < 4; i++) {
		float a = wind + TURN[i];
		vec2 dir = vec2(cos(a), sin(a));
		dir = normalize(mix(dir, flow_dir, clamp(flow_amt, 0.0, 1.0) * 0.85) + vec2(1e-4, 0.0));
		float k = TAU / LEN[i];
		float w = ws_loop_omega(sqrt(9.81 * k) * speed * (1.0 + 0.8 * flow_amt));
		float warp = (i % 2 == 0) ? bend.x : bend.y;
		float ph = k * dot(dir, p) - w * t + PHASE[i] + BEND[i] * warp * (1.0 - 0.6 * flow_amt);
		g += dir * (SLOPE[i] * cos(ph) * ws_fade(LEN[i], foot));
	}
	return g;
}

// Rajadas de vento ("cat's paws"): manchas lentas de agua mais crespa (1) ou mais lisa (0).
// Orbita de 900 s, fecha em 3600 s. Visiveis de longe como areas claras/escuras que derivam.
float ws_gust(vec2 p, float t) {
	float orbit = TAU * t / 900.0;
	vec2 drift = 34.0 * vec2(cos(orbit), sin(orbit));
	float g1 = ws_noise(WS_ROT_U * (p + drift) / 19.0, 439u);
	float g2 = ws_noise(WS_ROT_BAY * (p - drift * 0.6) / 8.5, 443u);
	return smoothstep(0.28, 0.82, 0.7 * g1 + 0.3 * g2);
}

// Ceu estilizado refletido (o jogo usa fundo de cor lisa; o zenite e um pouco mais azul).
vec3 ws_sky(vec3 r, vec3 horizon, vec3 zenith) {
	return mix(horizon, zenith, smoothstep(0.05, 0.9, r.y));
}
