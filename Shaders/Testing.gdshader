shader_type canvas_item;
void vertex() {
	VERTEX += vec2(cos(TIME)*100.0, sin(TIME)*100.0);
}

void fragment() {
	COLOR = vec4(UV, 1.0, 1.0);
}

