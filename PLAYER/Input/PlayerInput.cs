// One centralized input owner: arrow movement, Shift sprint, RMB jump, right Ctrl crouch, mouse look.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerInput
    {
        private readonly PlayerController _player;
        private float _pitch;
        private bool _jumpRequested, _crouchRequested;

        public PlayerInput(PlayerController player) => _player = player;
        private bool Captured => Input.MouseMode == Input.MouseModeEnum.Captured && _player.GetWindow().HasFocus();

        // =========================================================
        // Preserve War Machine's physical bindings; the mouse vertical axis is inverted by default.
        public void Handle(InputEvent input)
        {
            if (input is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.PhysicalKeycode == Key.Escape)
                {
                    ReleaseMouse(); _player.GetViewport().SetInputAsHandled(); return;
                }
                if (Captured && key.PhysicalKeycode == Key.Ctrl && key.Location == KeyLocation.Right)
                {
                    _crouchRequested = true; _player.GetViewport().SetInputAsHandled(); return;
                }
            }

            if (input is InputEventMouseButton mouse && mouse.Pressed)
            {
                if (!Captured && mouse.ButtonIndex == MouseButton.Left && _player.GetWindow().HasFocus())
                {
                    CaptureMouse(); _player.GetViewport().SetInputAsHandled(); return;
                }
                if (Captured && mouse.ButtonIndex == MouseButton.Right)
                {
                    _jumpRequested = true; _player.GetViewport().SetInputAsHandled(); return;
                }
            }

            if (Captured && input is InputEventMouseMotion motion)
            {
                float sensitivity = Mathf.Clamp(_player.Settings.MouseSensitivity, 0.01f, 1f);
                float limit = Mathf.Clamp(_player.Settings.MaximumPitch, 10f, 89f);
                float verticalSign = _player.Settings.InvertMouseY ? 1f : -1f;
                _player.RotateY(Mathf.DegToRad(-motion.ScreenRelative.X * sensitivity));
                _pitch = Mathf.Clamp(_pitch + motion.ScreenRelative.Y * verticalSign * sensitivity, -limit, limit);
                _player._cameraPivot.RotationDegrees = new Vector3(_pitch, 0, 0);
                _player.GetViewport().SetInputAsHandled();
            }
        }

        // =========================================================
        public Vector2 MovementAxis()
        {
            if (!Captured) return Vector2.Zero;
            return new Vector2(
                (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
                (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0)
            ).Normalized();
        }

        // =========================================================
        public bool SprintHeld => Captured && Input.IsPhysicalKeyPressed(Key.Shift);

        // =========================================================
        public bool ConsumeJump() { bool result = _jumpRequested; _jumpRequested = false; return result; }

        // =========================================================
        public bool ConsumeCrouch() { bool result = _crouchRequested; _crouchRequested = false; return result; }

        // =========================================================
        public void CaptureMouse() => Input.MouseMode = Input.MouseModeEnum.Captured;

        // =========================================================
        public void ReleaseMouse()
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _jumpRequested = _crouchRequested = false;
        }
    }
}
