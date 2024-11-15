shader_type canvas_item;
uniform vec2[100] roots;
uniform int idclose = -1;
void fragment() {
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y);
	float best = 999999999.0;
	COLOR = vec4(0.0,0.0,0.0,0.0);
	for (int i = 0; i < 100; i++){
		float d = distance(roots[i], scale);
		
		if (d < 10.0){
			COLOR = vec4(0.0,1.0,0.0,idclose == i?1.0:0.4);
		}
	}
}
