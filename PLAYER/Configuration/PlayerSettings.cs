// Shared FPS movement, stance and mouse settings; authored independently from test scenes.
using Godot;

[GlobalClass]
public partial class PlayerSettings : Resource
{
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 5f;
    [Export] public float SprintMultiplier { get; set; } = 1.6f;
    [Export] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export] public float Acceleration { get; set; } = 35f;
    [Export] public float Deceleration { get; set; } = 45f;
    [Export] public float ReverseAcceleration { get; set; } = 50f;
    [Export] public float Gravity { get; set; } = 20f;
    [Export] public float JumpVelocity { get; set; } = 6f;

    [ExportGroup("Stance")]
    [Export] public float StandingHeight { get; set; } = 1.8f;
    [Export] public float CrouchHeight { get; set; } = 1.2f;
    [Export] public float StandingEyeHeight { get; set; } = 1.65f;
    [Export] public float CrouchEyeHeight { get; set; } = 1.05f;
    [Export] public float StanceSpeed { get; set; } = 6f;

    [ExportGroup("Mouse Look")]
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float MouseSensitivity { get; set; } = 0.10f;
    [Export] public bool InvertMouseY { get; set; } = true;
    [Export(PropertyHint.Range, "10,89,1")] public float MaximumPitch { get; set; } = 85f;
}
