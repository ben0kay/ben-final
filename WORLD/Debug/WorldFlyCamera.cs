// Standalone fly camera for inspecting terrain; independent of the FPS player's controls.
using Godot;

public partial class WorldFlyCamera : Camera3D
{
    [Export] public float FlySpeed { get; set; } = 28f;
    [Export] public float FastMultiplier { get; set; } = 3f;
    [Export] public float MouseSensitivity { get; set; } = 0.12f;
    [Export] public bool InvertMouseY { get; set; } = true;

    private float _pitch, _yaw;

    // =========================================================
    public override void _Ready()
    {
        _pitch = RotationDegrees.X;
        _yaw = RotationDegrees.Y;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.Escape)
        {
            ReleaseMouse(); GetViewport().SetInputAsHandled(); return;
        }
        if (input is InputEventMouseButton button && button.Pressed && button.ButtonIndex == MouseButton.Left
            && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetViewport().SetInputAsHandled(); return;
        }
        if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw = Mathf.Wrap(_yaw - motion.ScreenRelative.X * MouseSensitivity, -180f, 180f);
            _pitch = Mathf.Clamp(_pitch + motion.ScreenRelative.Y * (InvertMouseY ? 1f : -1f)
                * MouseSensitivity, -88f, 88f);
            RotationDegrees = new Vector3(_pitch, _yaw, 0f);
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // Only the active debug fly camera polls held keys; terrain chunks do not process frames.
    public override void _Process(double delta)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured || !GetWindow().HasFocus()) return;
        float x = (Pressed(Key.D) || Pressed(Key.Right) ? 1f : 0f) - (Pressed(Key.A) || Pressed(Key.Left) ? 1f : 0f);
        float z = (Pressed(Key.W) || Pressed(Key.Up) ? 1f : 0f) - (Pressed(Key.S) || Pressed(Key.Down) ? 1f : 0f);
        float y = (Pressed(Key.E) ? 1f : 0f) - (Pressed(Key.Q) ? 1f : 0f);
        Vector3 direction = GlobalBasis.X * x + -GlobalBasis.Z * z + Vector3.Up * y;
        if (direction.LengthSquared() < 0.001f) return;
        float speed = FlySpeed * (Pressed(Key.Shift) ? FastMultiplier : 1f);
        GlobalPosition += direction.Normalized() * speed * (float)delta;
    }

    // =========================================================
    private static bool Pressed(Key key) => Input.IsPhysicalKeyPressed(key);

    // =========================================================
    private static void ReleaseMouse() => Input.MouseMode = Input.MouseModeEnum.Visible;

    // =========================================================
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut) ReleaseMouse();
    }

    // =========================================================
    public override void _ExitTree() => ReleaseMouse();
}
