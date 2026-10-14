// IndexCities - WaterStudy - Lago irregular: faixa de margem em relevo (FORA do poligono de agua).
// Integrado a partir do estudo D da frente de assets. Estetica pendente de revisao humana.
//
// SOMENTE APRESENTACAO. Grama umida, terra exposta, areia e faixa molhada nao representam
// recurso, qualidade da agua, poluicao, vegetacao da regra ambiental nem area de obra.
//
// Superficie 1 da malha `ground` gerada por Source/ws_lake_d.gd:
//   UV  = (distancia ao poligono em m, > 0 aqui; altura acima da agua em m)
//   UV2 = (carater da margem: 0 suave .. 1 barranco; largura da faixa em m)
// No alto da faixa (plano, longe da agua) a cor e exatamente a da grama do jogo (espelho de
// env_grass_soft), entao a costura com o chao plano (material real) nao aparece. A inclinacao vem
// da normal da propria malha: a terra so aparece onde o barranco e inclinado.
//
// Opaco, uma passada. GL Compatibility: so aritmetica, uint e fwidth; sem texturas.
shader_type spatial;

#include "res://Assets/RecoveryEnvironment/Lake/Shaders/ws_d_bank.gdshaderinc"

varying vec3 v_world;
varying vec3 v_nworld;
varying vec4 v_attr;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_nworld = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz;
	v_attr = vec4(UV, UV2);
}

void fragment() {
	vec2 p = v_world.xz;
	float foot = d_foot(p);
	float h = v_attr.y;
	float slope = 1.0 - clamp(normalize(v_nworld).y, 0.0, 1.0);
	NORMAL = normalize((VIEW_MATRIX * vec4(d_relief_normal(v_nworld), 0.0)).xyz);
	ALBEDO = d_bank_color(p, h, slope, v_attr.z, foot);
	ROUGHNESS = d_bank_roughness(p, h);
}
