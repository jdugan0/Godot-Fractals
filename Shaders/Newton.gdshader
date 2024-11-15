shader_type canvas_item;
uniform vec2[100] roots;
uniform int valid = 0;
void fragment() {
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y);
	float zoom = length(res)/2.0;
	mat2 c = mat2(vec2(scale.x, scale.y),vec2(-scale.y, scale.x)) / zoom;
	float epsilon = 0.00001;
	
	for (int j = 0; j < 10; j++){
		mat2 result = mat2(1);
		mat2 h = mat2(1);
		for (int i = 0; i < valid; i++){
			mat2 root = mat2(roots[i],vec2(-roots[i].y,roots[i].x)) / zoom;
			result *= (c - root);
			h *= ((c - mat2(epsilon)) - root);
		}
		mat2 prime = (result - h) * mat2(1.0/epsilon);
		c = -1.0 * (result * inverse(prime)) + c;
	}
	vec2 zero = vec2(c[0]);
	
		
	//COLOR = vec4(zero, 0.0, 1.0);
	float best = 1.0 / 0.0;
	int id = -1;
	for (int i = 0; i < valid; i++){
		float d = distance(roots[i] / zoom, zero);		
		if (d < best){
			best = d;
			id = i;
		}
	}
	if (id == -1){
		COLOR = vec4(1.0,1.0,1.0,1.0);
	}
	else{
		COLOR = vec4(float(id)/float(valid),0.0,0.0,1.0);
	}
	
}
