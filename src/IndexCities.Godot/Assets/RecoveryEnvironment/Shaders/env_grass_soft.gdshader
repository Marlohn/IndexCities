// IndexCities - RecoveryEnvironment - grama com variacao suave de cor.
// Amostra da frente de assets (IA). Revisao humana: PENDENTE.
//
// SOMENTE ESTETICO. A variacao de cor nao representa bioma, fertilidade, umidade,
// obstaculo, zona, lote, valor do terreno nem qualquer estado da simulacao. O padrao
// e deterministico e depende so da posicao em metros no MUNDO (plano XZ, Y para cima):
// e continuo entre malhas vizinhas e nao muda com a camera, a hora ou a seed.
//
// Sem texturas. Ruido de valor original (hash inteiro proprio + interpolacao Hermite)
// em tres escalas: manchas grandes, manchas medias e granulado fino. Cada escala some
// suavemente quando fica pequena demais na tela (fwidth), para nao cintilar com zoom
// afastado.
//
// Compatibilidade: Godot 4.7, renderizador GL Compatibility (OpenGL 3.3 / GLES 3), o do
// jogo; os outros renderizadores nao foram testados. Opaco, sem render_mode especial:
// sombreamento padrao igual ao StandardMaterial3D que o jogo usa hoje.
// Nao desloca vertices: geometria, nivel e colisao da malha hospedeira ficam intactos.
shader_type spatial;

// Cor media: a mesma do chao atual do jogo (CityWorld, #7d9b52). Com variation = 0
// o resultado e exatamente essa cor lisa.
uniform vec3 base_color : source_color = vec3(0.490196, 0.607843, 0.321569);
// Extremos da variacao: mancha mais quente/amarelada e mancha mais fria/profunda.
// Ficam proximos da cor media para nao parecerem terra, areia, mata ou agua.
uniform vec3 warm_tint : source_color = vec3(0.525490, 0.627451, 0.329412);
uniform vec3 cool_tint : source_color = vec3(0.423529, 0.580392, 0.321569);
// Quanto da variacao aparece (0 = cor lisa atual, 1 = extremos acima).
uniform float variation : hint_range(0.0, 1.0) = 0.7;
// Tamanhos aproximados das manchas, em metros.
uniform float patch_size_m : hint_range(8.0, 200.0) = 56.0;
uniform float detail_size_m : hint_range(2.0, 40.0) = 12.0;
uniform float grain_size_m : hint_range(0.4, 8.0) = 1.2;
// Granulado fino: so luminancia, +-grain_strength. Some sozinho com zoom afastado.
uniform float grain_strength : hint_range(0.0, 0.15) = 0.025;
// Igual ao material atual do chao (StandardMaterial3D com roughness 0.85).
uniform float roughness_value : hint_range(0.0, 1.0) = 0.85;

varying vec3 v_world;

// Hash inteiro original (multiplicacoes e xor-shifts com constantes proprias) de uma
// celula da grade para [0, 1). Aritmetica uint e exata em GL 3.3 / GLES 3, sem a
// imprecisao dos hashes baseados em seno. O deslocamento de 65536 evita converter
// inteiros negativos (mapa de +-256 m; celulas >= 0,4 m).
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

// Ruido de valor 2D em [0, 1], interpolacao Hermite (C1).
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

// Rotacoes fixas por escala: nenhuma camada fica alinhada aos eixos X/Z, para o padrao
// nao lembrar a grade logica de construcao nem lotes.
const mat2 ROT_A = mat2(vec2(0.8480, 0.5299), vec2(-0.5299, 0.8480));   // 32 graus
const mat2 ROT_B = mat2(vec2(0.2588, -0.9659), vec2(0.9659, 0.2588));   // -75 graus
const mat2 ROT_C = mat2(vec2(0.6018, 0.7986), vec2(-0.7986, 0.6018));   // 53 graus
const mat2 ROT_D = mat2(vec2(0.9877, -0.1564), vec2(0.1564, 0.9877));   // -9 graus

// Peso de uma escala de tamanho `size_m` dado o tamanho do pixel no chao `foot_m`:
// 1 enquanto a mancha ocupa ~8 px ou mais, 0 quando cai para ~3 px.
float env_scale_fade(float size_m, float foot_m) {
	return 1.0 - smoothstep(size_m * 0.125, size_m * 0.33, foot_m);
}

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 p = v_world.xz;
	vec2 fw = fwidth(p);
	float foot = max(fw.x, fw.y);

	// Manchas grandes (duas oitavas) + medias. Resultado centrado em 0,5.
	float macro = env_value_noise(ROT_A * p / patch_size_m, 11u) * 0.67
			+ env_value_noise(ROT_B * p / (patch_size_m * 0.47), 23u) * 0.33;
	float meso = env_value_noise(ROT_C * p / detail_size_m, 37u);
	float meso_w = 0.22 * env_scale_fade(detail_size_m, foot);
	float n = mix(macro, meso, meso_w);
	// Expande o contraste (o ruido somado concentra perto de 0,5) e aplica a intensidade.
	float t = smoothstep(0.22, 0.78, n);
	t = 0.5 + (t - 0.5) * variation;

	// Interpolacao por partes que passa exatamente pela cor media em t = 0,5.
	vec3 col = t < 0.5
			? mix(cool_tint, base_color, t * 2.0)
			: mix(base_color, warm_tint, (t - 0.5) * 2.0);

	// Granulado fino: so luminancia e so de perto.
	float grain = env_value_noise(ROT_D * p / grain_size_m, 53u) - 0.5;
	col *= 1.0 + grain * 2.0 * grain_strength * env_scale_fade(grain_size_m, foot);

	ALBEDO = col;
	ROUGHNESS = roughness_value;
}
