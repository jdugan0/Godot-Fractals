shader_type canvas_item;
uniform vec2[100] roots;
uniform int valid = 0;
uniform int idclose = -1;
uniform vec2 offset;
uniform float zoomFactor;
uniform vec2 res = vec2(1920, 1080);
vec2 transform(vec2 a){
	vec2 scale = (a + vec2(-1920.0/2.0, -1080.0/2.0)) / max(res.x, res.y)/ zoomFactor+ offset;
	return scale;
}
void fragment() {
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y));
	float best = 999999999.0;
	COLOR = vec4(0.0,0.0,0.0,0.0);
	for (int i = 0; i < valid; i++){
		float d = distance(roots[i], scale);
		
		if (d < 0.002 / zoomFactor){
			COLOR = vec4(0.0,1.0,0.0,idclose == i?1.0:0.4);
		}
	}
}
