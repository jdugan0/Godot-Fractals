shader_type canvas_item;
uniform vec2[100] roots;
void fragment() {
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y);
	float best = 999999999.0;
	int id = -1;
	float count = 0.0;
	bool circle = false;
	for (int i = 0; i < 100; i++){
		float d = distance(roots[i], scale);
		
		if (d < length(res)){
			count++;
			if (d < 10.0){
				circle = true;
				break;
			}
			if (d < best){
				best = d;
				id = i;
				
			}
		}
	}
	if (circle){
		COLOR = vec4(0.0,1.0,0.0,1.0);
	}
	else{
		if (id == -1){
			COLOR = vec4(1.0,1.0,1.0,1.0);
		}
		else{
			COLOR = vec4(float(id)/count,0.0,0.0,1.0);
		}
	}
	
}
