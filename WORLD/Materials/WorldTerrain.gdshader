shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

varying vec3 world_position;
varying float terrain_upness;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    terrain_upness = clamp(dot(normalize(MODEL_NORMAL_MATRIX * NORMAL), vec3(0.0, 1.0, 0.0)), 0.0, 1.0);
}

void fragment() {
    float variation = sin(world_position.x * 0.17 + sin(world_position.z * 0.08)) *
        sin(world_position.z * 0.19 - world_position.x * 0.05);
    vec3 grass = mix(vec3(0.19, 0.29, 0.16), vec3(0.30, 0.40, 0.20), 0.5 + variation * 0.22);
    float rock_weight = 1.0 - smoothstep(0.58, 0.84, terrain_upness);
    vec3 stone = vec3(0.35, 0.36, 0.34) + variation * 0.025;
    ALBEDO = mix(grass, stone, rock_weight);
    ROUGHNESS = 0.94;
    METALLIC = 0.0;
}
