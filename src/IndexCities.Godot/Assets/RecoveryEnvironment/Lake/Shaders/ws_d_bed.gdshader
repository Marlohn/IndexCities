// IndexCities - WaterStudy - Lago irregular: fundo do lago (DENTRO do poligono), visto atraves da agua.
// Integrado a partir do estudo D da frente de assets. Estetica pendente de revisao humana.
//
// SOMENTE APRESENTACAO. Lodo, plantas submersas, areia e "profundidade" nao representam recurso,
// qualidade da agua, poluicao, volume, area navegavel nem vegetacao da regra ambiental.
//
// Malha `bed` gerada por Source/ws_lake_d.gd (mesmos atributos da faixa de margem):
//   UV  = (distancia assinada ao poligono em m, < 0 aqui; altura em relacao a agua em m, <= 0)
//   UV2 = (carater da margem: 0 suave .. 1 barranco; largura da faixa em m)
// Na linha d'agua (altura 0) a cor e a mesma da faixa de margem (mesma funcao d_bank_color e mesma
// normal): o terreno continua por baixo da agua sem costura. Com a profundidade, a margem submersa
// passa para o fundo (lodo, plantas) e a absorcao por canal leva tudo para a cor do corpo d'agua.
// A superficie (ws_d_water) so acrescenta reflexo e brilho.
//
// Opaco, uma passada. GL Compatibility: so aritmetica, uint e fwidth; sem texturas.
shader_type spatial;

#include "res://Assets/RecoveryEnvironment/Lake/Shaders/ws_d_bank.gdshaderinc"

// Agua: absorcao por canal (1/m), fator de caminho (luz desce e sobe) e cor do corpo d'agua.
uniform vec3 absorption_per_m = vec3(12.0, 6.6, 5.4);
uniform float path_factor : hint_range(1.0, 4.0) = 1.6;
uniform vec3 water_body_color : source_color = vec3(0.157, 0.341, 0.396);

varying vec3 v_world;
varying vec3 v_nworld;
varying vec4 v_attr;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_nworld = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz;
	v_attr = vec4(UV, UV2);
}

vec3 floor_color(vec2 p, float depth, float kappa, float foot) {
	float big = d_noise(D_ROT_D * p / 9.0, 701u);
	float mid = d_noise(D_ROT_E * p / 3.1, 709u);
	vec3 col = mix(silt_dark_color, silt_color, smoothstep(0.25, 0.75, 0.7 * big + 0.3 * mid));
	// Plantas submersas em tufos, so no raso.
	float weed_n = 0.75 * d_noise(D_ROT_B * p / 4.2, 727u) + 0.25 * d_noise(D_ROT_C * p / 1.3, 733u) * d_fade(1.3, foot)
			+ 0.125 * (1.0 - d_fade(1.3, foot));
	float weed = smoothstep(0.58, 0.74, weed_n) * (1.0 - smoothstep(0.1, 0.22, depth));
	col = mix(col, weed_color, weed * 0.8);
	// Areia clara nas prateleiras rasas das margens suaves (rasos turquesa em trechos, nunca um anel em
	// volta do lago todo); no fundo a absorcao domina.
	float sand = smoothstep(0.42, 0.7, d_noise(D_ROT_F * p / 11.0, 719u)) * (1.0 - kappa) * (1.0 - smoothstep(0.07, 0.12, depth));
	col = mix(col, sand_color * 0.85, sand * 0.6);
	return col;
}

void fragment() {
	vec2 p = v_world.xz;
	float foot = d_foot(p);
	float h = min(v_attr.y, 0.0);
	float depth = -h;
	float slope = 1.0 - clamp(normalize(v_nworld).y, 0.0, 1.0);
	// Debaixo d'agua a luz chega difusa: o relevo do fundo sombreia cada vez menos com a profundidade
	// (sem isso a rampa submersa voltada para o sol acende como um anel claro em volta da margem).
	vec3 n_shade = normalize(mix(d_relief_normal(v_nworld), vec3(0.0, 1.0, 0.0), smoothstep(0.0, 0.12, depth) * 0.8));
	NORMAL = normalize((VIEW_MATRIX * vec4(n_shade, 0.0)).xyz);
	// Margem submersa -> fundo: comeca do zero na linha d'agua. Onde ja e so fundo (a maior parte do
	// lago), a cor da margem (espelho da grama, terra, areia) nem e calculada: mesmo resultado, menos custo.
	float sub = smoothstep(0.012, 0.15, depth);
	vec3 col;
	float rough = 0.9;
	if (sub >= 1.0) {
		col = floor_color(p, depth, v_attr.z, foot);
	} else {
		col = mix(d_bank_color(p, h, slope, v_attr.z, foot), floor_color(p, depth, v_attr.z, foot), sub);
		rough = mix(d_bank_roughness(p, h), 0.9, sub);
	}
	// Visto atraves da agua: absorcao por canal + cor do corpo d'agua. Nada muda na linha (depth 0).
	vec3 tr = exp(-absorption_per_m * depth * path_factor);
	ALBEDO = col * tr + water_body_color * (vec3(1.0) - tr);
	ROUGHNESS = rough;
	SPECULAR = mix(0.5, 0.12, sub);
}
