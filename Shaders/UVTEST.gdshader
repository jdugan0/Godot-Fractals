shader_type canvas_item;
uniform vec2 roots;
void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y);
	COLOR = vec4(distance(roots, scale) / length(res),0.0, 0.0, 1.0);
	
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
