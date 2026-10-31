// Opaque low-poly bark, retaining the mesh's authored vertex colours without foliage tint.
shader_type spatial;
render_mode cull_back;
void fragment() {
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 0.95;
    METALLIC = 0.0;
}
