shader_type canvas_item;
uniform vec2 offset;
uniform float zoomFactor;
uniform vec2 res = vec2(1920, 1080);

vec2 complexMult(vec2 one, vec2 two){
	return vec2(one.x * two.x - one.y * two.y, one.x * two.y + one.y * two.x);
}
// z  => (x, y)
// w  => (a, b)
vec2 complex_pow_complex(vec2 z, vec2 w) {
    // Convert z to polar form: z = r e^{iθ}
    float r = length(z);              // sqrt(x^2 + y^2)
    float theta = atan(z.y, z.x); // or built-in atan2 if available

    // w = a + i b
    float a = w.x;
    float b = w.y;

    // We'll compute z^w = e^{w * ln(z)} = e^{(a + i b) * (ln(r) + iθ)}

    // Real part of (a + i b) * (ln(r) + i θ) = a ln(r) - b θ
    float real_part = a * log(r + 1e-8) - b * theta;

    // Imag part of (a + i b) * (ln(r) + i θ) = a θ + b ln(r)
    float imag_part = a * theta + b * log(r + 1e-8);

    // e^{ real_part } is the magnitude
    float magnitude = exp(real_part);

    // imag_part is the new angle
    float angle = imag_part;

    // Finally convert from polar back to cartesian
    return vec2( 
        magnitude * cos(angle),
        magnitude * sin(angle)
    );
}

vec2 complexDivide(vec2 a, vec2 b) {
    float denom = b.x * b.x + b.y * b.y + 1e-8;
    return vec2(
        (a.x * b.x + a.y * b.y) / denom,
        (a.y * b.x - a.x * b.y) / denom
    );
}
vec2 complexSub(vec2 a, vec2 b){
	return a - b;
}
vec2 complexAdd(vec2 a, vec2 b){
	return a + b;
}
vec2 transform(vec2 a){
	vec2 scale = (a + vec2(-1920.0/2.0, -1080.0/2.0)) / max(res.x, res.y)/ zoomFactor+ offset;
	return scale;
}
vec3 hsv_to_rgb(float hue_degrees, float saturation, float value)
{
    // Normalize hue to [0, 360)
    float h = mod(hue_degrees, 360.0);
    float s = clamp(saturation, 0.0, 1.0);
    float v = clamp(value, 0.0, 1.0);

    // "c" is chroma, "x" is the offset, "m" is to shift back into [0,1]
    float c = v * s;
    float x = c * (1.0 - abs(mod(h / 60.0, 2.0) - 1.0));
    float m = v - c;
    
    vec3 rgb;
    
    if (h < 60.0) {
        rgb = vec3(c, x, 0.0);
    } else if (h < 120.0) {
        rgb = vec3(x, c, 0.0);
    } else if (h < 180.0) {
        rgb = vec3(0.0, c, x);
    } else if (h < 240.0) {
        rgb = vec3(0.0, x, c);
    } else if (h < 300.0) {
        rgb = vec3(x, 0.0, c);
    } else {
        // 300 <= h < 360
        rgb = vec3(c, 0.0, x);
    }

    return rgb + vec3(m, m, m);
}

float func(vec2 z) {
	vec2 result = vec2(0.00);
    float angle = atan(result.y, result.x);

    return angle + PI;
}

void fragment() {
	bool early = false;
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y));
	
	COLOR = vec4(hsv_to_rgb(func(scale) * (180.0 / PI),1.0,1.0),1.0);
}

