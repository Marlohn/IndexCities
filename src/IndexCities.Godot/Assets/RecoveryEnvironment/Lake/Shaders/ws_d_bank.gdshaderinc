// IndexCities - WaterStudy - Lago irregular: espelho da grama do jogo e paleta da margem, compartilhados
// pela faixa de margem (ws_d_shore) e pelo fundo (ws_d_bed). Estudo visual da frente de assets (IA).
// Revisao humana: PENDENTE. Proposta NAO integrada.
//
// SOMENTE APRESENTACAO. Grama umida, terra, areia, plantas e lodo nao representam recurso,
// qualidade da agua, poluicao, vegetacao da regra ambiental nem area de obra.
//
// A paleta e declarada AQUI como uniforms com os mesmos padroes nos dois shaders opacos (margem e
// fundo), para a cor ser continua na linha d'agua. Se um material mudar uma cor da paleta, mude a
// mesma cor no outro.
#ifndef WS_D_BANK
#define WS_D_BANK

#include "res://Assets/RecoveryEnvironment/Lake/Shaders/ws_d_common.gdshaderinc"

// ---------------------------------------------------------------------------------------------
// Espelho EXATO da grama integrada (RecoveryEnvironment/Shaders/env_grass_soft.gdshader): mesmo
// hash, sais, rotacoes, formula e padroes dos uniforms (com a mesma dica source_color). Serve para
// a faixa de margem sair, no alto do barranco, com a mesma cor do chao plano (que usa o material
// real), sem costura. O validador (Source/validate_ws_d.gd) compara estas constantes com as do
// shader integrado. Se env_grass_soft mudar, atualizar este espelho.
uniform vec3 grass_base_color : source_color = vec3(0.490196, 0.607843, 0.321569);
uniform vec3 grass_warm_tint : source_color = vec3(0.525490, 0.627451, 0.329412);
uniform vec3 grass_cool_tint : source_color = vec3(0.423529, 0.580392, 0.321569);
uniform float grass_variation : hint_range(0.0, 1.0) = 0.7;
uniform float grass_patch_size_m : hint_range(8.0, 200.0) = 56.0;
uniform float grass_detail_size_m : hint_range(2.0, 40.0) = 12.0;
uniform float grass_grain_size_m : hint_range(0.4, 8.0) = 1.2;
uniform float grass_grain_strength : hint_range(0.0, 0.15) = 0.025;
// Igual ao chao do jogo (env_grass_soft: roughness 0,85, specular padrao).
uniform float grass_roughness : hint_range(0.0, 1.0) = 0.85;

float dg_env_hash(ivec2 cell, uint salt) {
	uvec2 u = uvec2(cell + ivec2(65536));
	uint h = u.x * 0x8F2D5C37u + u.y * 0x4CF5AD43u + salt * 0x2E6B1A95u;
	h ^= h >> 15u;
	h *= 0xE3A9B0D1u;
	h ^= h >> 13u;
	h *= 0x68E31DA5u;
	h ^= h >> 16u;
	return float(h >> 8u) * (1.0 / 16777216.0);
}

float dg_env_noise(vec2 p, uint salt) {
	vec2 i = floor(p);
	vec2 f = p - i;
	ivec2 c = ivec2(i);
	float a = dg_env_hash(c, salt);
	float b = dg_env_hash(c + ivec2(1, 0), salt);
	float d = dg_env_hash(c + ivec2(0, 1), salt);
	float e = dg_env_hash(c + ivec2(1, 1), salt);
	vec2 w = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, w.x), mix(d, e, w.x), w.y);
}

const mat2 DG_ROT_A = mat2(vec2(0.8480, 0.5299), vec2(-0.5299, 0.8480));
const mat2 DG_ROT_B = mat2(vec2(0.2588, -0.9659), vec2(0.9659, 0.2588));
const mat2 DG_ROT_C = mat2(vec2(0.6018, 0.7986), vec2(-0.7986, 0.6018));
const mat2 DG_ROT_D = mat2(vec2(0.9877, -0.1564), vec2(0.1564, 0.9877));

vec3 d_grass(vec2 p, float foot) {
	float macro = dg_env_noise(DG_ROT_A * p / grass_patch_size_m, 11u) * 0.67
			+ dg_env_noise(DG_ROT_B * p / (grass_patch_size_m * 0.47), 23u) * 0.33;
	float meso = dg_env_noise(DG_ROT_C * p / grass_detail_size_m, 37u);
	float meso_w = 0.22 * d_fade(grass_detail_size_m, foot);
	float n = mix(macro, meso, meso_w);
	float t = smoothstep(0.22, 0.78, n);
	t = 0.5 + (t - 0.5) * grass_variation;
	vec3 col = t < 0.5 ? mix(grass_cool_tint, grass_base_color, t * 2.0)
			: mix(grass_base_color, grass_warm_tint, (t - 0.5) * 2.0);
	float grain = dg_env_noise(DG_ROT_D * p / grass_grain_size_m, 53u) - 0.5;
	col *= 1.0 + grain * 2.0 * grass_grain_strength * d_fade(grass_grain_size_m, foot);
	return col;
}

// ---------------------------------------------------------------------------------------------
// Paleta da margem (cores proximas da grama, baixa saturacao: nada de faixa contrastante).
// Grama umida perto da agua (um pouco mais funda e fria).
uniform vec3 moist_grass_color : source_color = vec3(0.380, 0.540, 0.290);
// Terra exposta, so onde o barranco e inclinado (em manchas).
uniform vec3 soil_color : source_color = vec3(0.455, 0.392, 0.282);
uniform vec3 soil_dark_color : source_color = vec3(0.345, 0.298, 0.220);
// Areia/pedrisco, so em trechos de margem suave e rente a agua.
uniform vec3 sand_color : source_color = vec3(0.640, 0.604, 0.478);
// Fundo submerso: plantas, lodo e areia molhada.
uniform vec3 weed_color : source_color = vec3(0.220, 0.300, 0.160);
uniform vec3 silt_color : source_color = vec3(0.275, 0.290, 0.192);
uniform vec3 silt_dark_color : source_color = vec3(0.212, 0.231, 0.161);
// Escurecimento da faixa molhada junto a linha d'agua (fator de albedo) e sua altura (m).
uniform float wet_darkening : hint_range(0.3, 1.0) = 0.66;
uniform float wet_height_min_m : hint_range(0.0, 0.3) = 0.015;
uniform float wet_height_max_m : hint_range(0.0, 0.5) = 0.07;
// Inclinacao (1 - normal.y) em que a terra comeca a aparecer e em que ja domina.
uniform float soil_slope_start : hint_range(0.0, 0.6) = 0.05;
uniform float soil_slope_full : hint_range(0.0, 0.8) = 0.2;
// Realce do relevo SO no sombreamento: a parte horizontal da normal e multiplicada por este fator
// (1 = normal geometrica). No plano (normal para cima) nao muda nada, entao o alto da faixa continua
// igual ao chao do jogo. Nao muda vertices, silhueta nem a inclinacao usada para a terra.
uniform float relief_shading_boost : hint_range(1.0, 3.0) = 2.3;

// Cor da margem (fora e logo abaixo da linha d'agua) em p, para uma altura h em relacao a agua
// (m; < 0 debaixo d'agua), inclinacao `slope` = 1 - normal.y e carater `kappa` (0 suave, 1 barranco).
// Em h = 0 o resultado e o mesmo para a margem e para o fundo (continuidade na linha d'agua).
// No alto da faixa (plano, longe da agua) o resultado e exatamente a grama do jogo.
vec3 d_bank_color(vec2 p, float h, float slope, float kappa, float foot) {
	vec3 col = d_grass(p, foot);
	// Umidade: grama mais funda nos primeiros ~0,3 m acima da agua, com bordas irregulares.
	float moist_top = 0.12 + 0.22 * d_noise(D_ROT_D * p / 3.4, 801u);
	float moist = 1.0 - smoothstep(moist_top * 0.35, moist_top, h);
	col = mix(col, moist_grass_color, moist * (0.45 + 0.25 * d_noise(D_ROT_E * p / 1.6, 803u)));
	// Terra exposta: so onde o barranco e inclinado, em manchas (nunca uma faixa continua).
	float steep = smoothstep(soil_slope_start, soil_slope_full, slope);
	float patchn = 0.62 * d_noise(D_ROT_A * p / 2.6, 811u) + 0.38 * d_noise(D_ROT_C * p / 0.9, 821u) * d_fade(0.9, foot)
			+ 0.19 * (1.0 - d_fade(0.9, foot));
	float soil = steep * smoothstep(0.34, 0.6, patchn + 0.28 * steep);
	vec3 soil_col = mix(soil_dark_color, soil_color, d_noise(D_ROT_B * p / 1.3, 827u));
	col = mix(col, soil_col, soil);
	// Areia/pedrisco: trechos de margem suave, rente a agua.
	float beach = (1.0 - kappa) * smoothstep(0.5, 0.7, d_noise(D_ROT_F * p / 8.0, 831u));
	float sand = beach * (1.0 - smoothstep(0.02, 0.11, h)) * (0.7 + 0.3 * d_noise(D_ROT_E * p / 0.7, 833u));
	col = mix(col, sand_color, sand);
	// Faixa molhada fina e irregular junto a linha d'agua (e tudo abaixo dela).
	float wet_h = mix(wet_height_min_m, wet_height_max_m, d_noise(D_ROT_C * p / 1.9, 841u));
	float wet = 1.0 - smoothstep(wet_h * 0.35, wet_h, h);
	col *= mix(1.0, wet_darkening, wet);
	return col;
}

// Normal de sombreamento (mundo) com o relevo realcado so nas rampas: a face inclinada do barranco
// fica com a normal geometrica (realcar ali a deixaria quase preta do lado oposto ao sol).
vec3 d_relief_normal(vec3 n_world) {
	vec3 n = normalize(n_world);
	float b = mix(relief_shading_boost, 1.0, smoothstep(0.06, 0.2, 1.0 - n.y));
	return normalize(vec3(n.x * b, n.y, n.z * b));
}

// Rugosidade da margem: a faixa molhada fica um pouco mais lisa.
float d_bank_roughness(vec2 p, float h) {
	float wet_h = mix(wet_height_min_m, wet_height_max_m, d_noise(D_ROT_C * p / 1.9, 841u));
	return mix(grass_roughness, 0.62, 1.0 - smoothstep(wet_h * 0.35, wet_h, h));
}

#endif
