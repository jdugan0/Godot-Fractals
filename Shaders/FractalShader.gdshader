shader_type canvas_item;
uniform vec2 offset;
uniform float zoomFactor;
uniform vec2 res = vec2(1920, 1080);
vec2 complexMult(vec2 one, vec2 two){
	return vec2(one.x * two.x - one.y * two.y, one.x * two.y + one.y * two.x);
}
vec2 complexDivide(vec2 a, vec2 b) {
    float denom = b.x * b.x + b.y * b.y + 1e-8;
    return vec2(
        (a.x * b.x + a.y * b.y) / denom,
        (a.y * b.x - a.x * b.y) / denom
    );
}
vec2 transform(vec2 a){
	vec2 scale = (a + vec2(-1920.0/2.0, -1080.0/2.0)) / max(res.x, res.y)/ zoomFactor+ offset;
	return scale;
}
void fragment() {
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y));
	vec2 new = scale;
	bool color = false;
	for (int i = 0; i < 1000; i++){
		new = complexMult(new, new) + scale; 
		if (length(new) > 3.0){
			color = true;
			break;
		}
	}
	if (color){
		COLOR = vec4(0,0,0,1);
	}
	else{
		COLOR = vec4(1,1,1,1);
	}
	
}
