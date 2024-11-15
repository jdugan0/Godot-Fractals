shader_type canvas_item;

void vertex() {
	
}

void fragment() {
	float r1 = SCREEN_PIXEL_SIZE.x / SCREEN_PIXEL_SIZE.y;
	vec2 scale = vec2((UV.x - 0.5f), (-UV.y + 0.5) * (r1))* 4.0f;
	mat2 c = mat2(vec2(scale.x, scale.y),vec2(-scale.y, scale.x));
	mat2 r = c;
	bool good = true;
	int count = 0;
	for (int i = 1; i < 1000; i++){
		r = r * r + c;
		if (sqrt(r[0][0] * r[0][0] + r[0][1] * r[0][1]) > 3.0f){
			good = false;
			break;
		}
		count++;
	}
	if (good){
		COLOR = vec4(1,1,1,1);
	}
	else{
		COLOR = vec4(float(count)/10.0,0.0,0.0,1.0);
	}
	
}
