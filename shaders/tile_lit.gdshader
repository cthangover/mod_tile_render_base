shader_type canvas_item;

// Tiles and actors are multiplied by the ambient
// colour of the time of day (time_weights blends the four phases like timed_sprite), and
// point lights (the player, lit events) add to it. Lights only add up to white, so by day
// they are invisible and at night they open circles in the dark. Positions and radii are
// in logical cell units for terrain lights; free image lights arrive in canvas pixels after projection.

uniform vec4 day_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 morning_color : source_color = vec4(0.8207547, 0.6465379, 0.8050903, 1.0);
uniform vec4 evening_color : source_color = vec4(0.81671757, 0.7075472, 0.9433962, 1.0);
// The same deep night and phase palette as ui_base/timed_sprite, used by novel backgrounds.
uniform vec4 night_color : source_color = vec4(0.0, 0.13673696, 0.20754719, 1.0);
uniform float light_intensity : hint_range(0.0, 10.0) = 2.0;
uniform float light_falloff : hint_range(0.0, 10.0) = 3.0;
uniform vec4 time_weights = vec4(0.0, 1.0, 0.0, 0.0);

// A map that sets its own ambient (indoors) uses it instead of the time of day.
uniform bool use_ambient = false;
uniform vec4 ambient_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform int light_spaces[16]; // 0: cell-space falloff, 1: free circular light on the rendered 2D artwork.
uniform vec2 light_positions[16];
uniform vec4 light_colors[16];
uniform float light_radii[16];
uniform int light_count = 0;
uniform vec2 portable_position = vec2(-1000.0);
uniform vec4 portable_color : source_color = vec4(0.0);
uniform float portable_radius = 0.0;

uniform vec2 cell_x = vec2(1.0, 0.0);
uniform vec2 cell_y = vec2(0.0, 1.0);
uniform vec2 cell_offset = vec2(0.0);
varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	vec4 color = texture(TEXTURE, UV) * COLOR;

	vec3 ambient = use_ambient
		? ambient_color.rgb
		: (morning_color * time_weights.x + day_color * time_weights.y
			+ evening_color * time_weights.z + night_color * time_weights.w).rgb;

	vec2 cell = vec2(dot(world_pos, cell_x), dot(world_pos, cell_y)) + cell_offset;
	float portable = clamp(1.0 - distance(world_pos, portable_position) / max(portable_radius, 0.001), 0.0, 1.0);
	vec3 light = portable_color.rgb * portable_color.a * pow(portable, light_falloff) * light_intensity;
	for (int i = 0; i < 16; i++) {
		if (i >= light_count)
			break;
		vec2 sample_position = light_spaces[i] == 1 ? world_pos : cell;
        float k = clamp(1.0 - distance(sample_position, light_positions[i]) / max(light_radii[i], 0.001), 0.0, 1.0);
		light += light_colors[i].rgb * light_colors[i].a * pow(k, light_falloff) * light_intensity;
	}

	COLOR = vec4(color.rgb * min(ambient + light, vec3(1.0)), color.a);
}
