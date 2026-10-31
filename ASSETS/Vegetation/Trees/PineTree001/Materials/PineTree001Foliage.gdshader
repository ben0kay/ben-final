// Opaque faceted pine foliage with Final Machine's existing shared forest palette controls.
shader_type spatial;
render_mode cull_back;

// Existing F1 ForestFoliagePalette.Apply updates this pair on shared ShaderMaterial resources.
uniform vec4 forest_tint : source_color = vec4(0.43, 0.55, 0.37, 1.0);
uniform float forest_tint_strength : hint_range(0.0, 1.0) = 0.25;
uniform float palette_response : hint_range(0.0, 1.0) = 0.94;
uniform float foliage_roughness : hint_range(0.5, 1.0) = 0.94;

void fragment() {
    vec3 original = COLOR.rgb;
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    float luma = dot(original, weights);
    float palette_luma = max(dot(forest_tint.rgb, weights), 0.05);
    vec3 matched = clamp(forest_tint.rgb * (luma / palette_luma), vec3(0.0), vec3(1.0));
    ALBEDO = mix(original, matched, clamp(forest_tint_strength * palette_response, 0.0, 1.0));
    ROUGHNESS = foliage_roughness;
    METALLIC = 0.0;
}
