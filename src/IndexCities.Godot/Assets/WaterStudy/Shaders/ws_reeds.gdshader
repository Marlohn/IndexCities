// IndexCities - WaterStudy - juncos decorativos (amostras A e B).
// Estudo visual da frente de assets (IA). Revisao humana: PENDENTE.
//
// SOMENTE ESTETICO: nao e vegetacao da regra de qualidade ambiental, nao e recurso, obstaculo,
// nem estado da simulacao. Laminas finas vistas dos dois lados: a normal da malha e usada nas
// duas faces (sem inverter no verso), para a moita nao ficar preta de um lado.
// Cor por vertice (base escura -> ponta clara) multiplicada pela cor da instancia do MultiMesh.
// GL Compatibility: opaco, uma passada, sem textura.
shader_type spatial;
render_mode cull_disabled;

uniform float roughness_value : hint_range(0.0, 1.0) = 0.9;

varying vec3 v_normal_view;

void vertex() {
	v_normal_view = (MODELVIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
}

void fragment() {
	ALBEDO = COLOR.rgb;
	ROUGHNESS = roughness_value;
	NORMAL = normalize(v_normal_view);
}
