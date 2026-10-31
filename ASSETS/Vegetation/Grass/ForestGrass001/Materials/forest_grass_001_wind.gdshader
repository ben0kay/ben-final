// Geometric grass ribbons: true blade silhouettes, opaque rendering and lightweight GPU wind.
shader_type spatial;
render_mode cull_disabled;

// Shared, project-wide palette; the local grass material still controls its own tint.
// Material-local uniforms avoid a project-global compile-time dependency.
// The F1 debug control updates the shared materials together, only on changes.
uniform vec4 forest_tint : source_color = vec4(0.43, 0.55, 0.37, 1.0);
uniform float forest_tint_strength : hint_range(0.0, 1.0) = 0.25;
uniform sampler2D grass_atlas : source_color, filter_linear_mipmap_anisotropic;
// Kept for compatibility with existing ForestGrass001 material parameter sets.
uniform float alpha_cutoff : hint_range(0.05,0.95) = 0.39;
uniform float wind_strength : hint_range(0.0,0.2) = 0.045;
uniform float wind_speed : hint_range(0.0,5.0) = 1.1;
uniform vec2 wind_direction = vec2(0.82, 0.57);
uniform bool distance_fade_enabled = false;
uniform float sparse_distance = 44.0;
uniform float end_distance = 68.0;
uniform float foliage_backlight_strength : hint_range(0.0,0.35) = 0.12;
uniform vec3 foliage_tint : source_color = vec3(1.0);
uniform float palette_response : hint_range(0.0,1.0) = 0.85;
varying flat float distance_presence;
void vertex() {
    vec3 root_world = (MODEL_MATRIX * vec4(0.0,0.0,0.0,1.0)).xyz;
    float d = distance(root_world.xz, CAMERA_POSITION_WORLD.xz);
    distance_presence = distance_fade_enabled ? 1.0 - smoothstep(sparse_distance, end_distance, d) : 1.0;
    float anchored = smoothstep(0.06, 0.74, VERTEX.y);
    float offset = dot(root_world.xz, vec2(0.27,0.71));
    float wave = sin(TIME * wind_speed + offset + VERTEX.y * 1.9)
        + 0.28 * sin(TIME * wind_speed * 0.61 + offset * 1.4);
    VERTEX.xz += normalize(wind_direction) * wave * wind_strength * anchored * anchored;
    VERTEX.y *= distance_presence;
}
void fragment() {
    // Opaque narrow ribbons: no alpha-scissor overdraw from broad texture cards.
    vec3 texel = texture(grass_atlas, UV).rgb;
    vec3 original = texel * COLOR.rgb * foliage_tint;
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    float original_luma = dot(original, weights);
    float palette_luma = max(dot(forest_tint.rgb, weights), 0.05);
    vec3 matched = clamp(forest_tint.rgb * (original_luma / palette_luma), vec3(0.0), vec3(1.0));
    ALBEDO = mix(original, matched, clamp(forest_tint_strength * palette_response, 0.0, 1.0));
    ROUGHNESS = 0.93;
    SPECULAR = 0.25;
    METALLIC = 0.0;
    // The original winding and geometric normal directions are preserved in the GLTF.
    // Face the interpolated ribbon normal toward the viewer for two-sided lighting.
    vec3 n = normalize(NORMAL);
    if (dot(n, VIEW) < 0.0) n = -n;
    NORMAL = n;
    BACKLIGHT = ALBEDO * foliage_backlight_strength;
}
