// Standalone fly camera for inspecting terrain; independent of the FPS player's controls.
using System;
using Godot;

public partial class WorldFlyCamera : Camera3D
{
    [Export] public float FlySpeed { get; set; } = 60f;
    [Export] public float FastMultiplier { get; set; } = 3f;
    [Export] public float MouseSensitivity { get; set; } = 0.12f;
    [Export] public bool InvertMouseY { get; set; } = true;

    private float _pitch, _yaw;
    private Vector3[] _routePoints = Array.Empty<Vector3>();
    private int _routeSegment;
    private float _routeSpeed;
    private bool _routeTeleport;
    public bool IsBenchmarkRouteActive { get; private set; }
    public event Action? BenchmarkRouteFinished;

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
        if (IsBenchmarkRouteActive)
        {
            // Don't advance the scripted route during alt-tab or lost focus.
            if (GetWindow().HasFocus()) AdvanceBenchmarkRoute((float)delta);
            return;
        }
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
    #region Repeatable benchmark route

    // =========================================================
    // Fixed geometry and camera pose give comparable passes at 60 or 180 m/s.
    // Points are kept within the current NxN preview; the optional jump is instant.
    public void StartBenchmarkRoute(float worldWidth, float speed, bool teleport)
    {
        float reach = Mathf.Max(8f, worldWidth * 0.32f);
        const float altitude = 180f;
        var origin = new Vector3(0, altitude, 0);
        var points = new System.Collections.Generic.List<Vector3>
        {
            origin, new(0, altitude, -reach), new(reach, altitude, -reach),
            new(reach, altitude, reach), new(-reach, altitude, reach),
            new(-reach, altitude, -reach), origin
        };
        if (teleport)
        {
            points.Add(new Vector3(reach, altitude, reach)); // instantaneous jump
            points.Add(origin); // return flight after teleport
        }
        _routePoints = points.ToArray();
        _routeSegment = 0;
        _routeSpeed = Mathf.Max(1f, speed);
        _routeTeleport = teleport;
        GlobalPosition = origin;
        _pitch = -12f; _yaw = 0;
        RotationDegrees = new Vector3(_pitch, _yaw, 0);
        IsBenchmarkRouteActive = true;
    }

    // =========================================================
    public void StopBenchmarkRoute()
    {
        IsBenchmarkRouteActive = false;
        _routePoints = Array.Empty<Vector3>();
    }

    // =========================================================
    private void AdvanceBenchmarkRoute(float delta)
    {
        float remainingDistance = _routeSpeed * Mathf.Min(delta, 0.25f);
        while (IsBenchmarkRouteActive && _routeSegment < _routePoints.Length - 1)
        {
            if (_routeTeleport && _routeSegment == 6)
            {
                GlobalPosition = _routePoints[7];
                _routeSegment = 7;
                break;
            }

            Vector3 destination = _routePoints[_routeSegment + 1];
            float distance = GlobalPosition.DistanceTo(destination);
            if (distance > remainingDistance)
            {
                GlobalPosition = GlobalPosition.MoveToward(destination, remainingDistance);
                break;
            }
            GlobalPosition = destination;
            remainingDistance -= distance;
            _routeSegment++;
            if (remainingDistance <= 0) break;
        }
        if (_routeSegment < _routePoints.Length - 1) return;
        StopBenchmarkRoute();
        BenchmarkRouteFinished?.Invoke();
    }
    #endregion

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
