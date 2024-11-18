shader_type canvas_item;
uniform vec2[100] roots; //roots given in pixel coords on screen
uniform vec2 offset;
uniform float zoomFactor;
uniform int valid = 0;
uniform vec2 res = vec2(1920, 1080);
uniform bool colorScheme;
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
vec3 hsvToRGB(float angle){
	float H = angle/60.0;
	
	if (H <= 1.0){
		return vec3(1.0,H,0.0);
	}
	else if (H <= 2.0){
		return vec3(-H + 2.0, 1.0, 0.0);
	}
	else if (H <= 3.0){
		return vec3(0.0,1.0,H - 2.0);
	}
	else if (H <= 4.0){
		return vec3(0.0, -H + 4.0, 1.0);
	}
	else if (H<=5.0){
		return vec3(H - 4.0, 0.0, -H + 5.0);
	}
	else if (H<=6.0){
		return vec3(1.0, H - 5.0, 0.0);
	}
}
void fragment() {
	bool early = false;
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y));
	
	//float epsilon = 0.0001;
	float diff = 0.0;
	int iterations = 0;
	for (int j = 0; j < 600; j++){
		vec2 result = vec2(1,0);
		//vec2 h = vec2(1,0);
		
		vec2 prime = vec2(0);
		
		for (int i = 0; i < valid; i++){
			vec2 root = roots[i];
			result = complexMult(result, (scale - root));
			//h = complexMult(((scale - vec2(epsilon,0)) - root), h);
			prime += complexDivide(vec2(1,0), (scale - root));
		}
		//vec2 prime1 = complexDivide((result - h),vec2(epsilon,0));
		prime = complexMult(result,prime);
		//diff = length(-prime + prime1); //debug variable
		scale = scale - complexDivide(result, prime);
		//iterations++;
		if (length(prime) < 1e-3 / pow(float(valid),float(valid))){
			//early = true;
			break;
		} 
		 // using analytical derivative NOT numerical
	}
	vec2 zero = scale;
	
		
	//COLOR = vec4(zero, 0.0, 1.0);
	float best = 1.0 / 0.0;
	int id = -1;
	for (int i = 0; i < valid; i++){
		float d = distance(roots[i], zero);
		if (d < best){
			best = d;
			id = i;
		}
	}
	if (id == -1 || early){
		COLOR = vec4(1.0,1.0,1.0,1.0);
	}
	else{
		if (colorScheme){
			COLOR = vec4(hsvToRGB(float(id) * (360.0 / float(valid))),1.0);
		}
		else{
			COLOR = vec4(float(id) / float(valid), 0.0, 0.0, 1.0);
		}
		
	}
	
	
}
