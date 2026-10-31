// Reusable CharacterBody3D; keeps Godot callbacks in one owner and delegates input, movement and crouch.
using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public PlayerSettings Settings { get; set; } = new();

    private Node3D _cameraPivot = null!;
    private readonly PlayerInput _input;
    private readonly PlayerMovement _movement;
    private readonly PlayerStance _stance;
    public bool IsCrouching => _stance.IsCrouching;

    // =========================================================
    public PlayerController()
    {
        _input = new PlayerInput(this);
        _movement = new PlayerMovement(this);
        _stance = new PlayerStance(this);
    }

    // =========================================================
    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _stance.Initialize();
        FloorSnapLength = 0.4f;
        FloorMaxAngle = Mathf.DegToRad(46f);
        FloorStopOnSlope = true;
        _input.CaptureMouse();
        SetProcess(false);
    }

    // =========================================================
    public override void _UnhandledInput(InputEvent input) => _input.Handle(input);

    // =========================================================
    public override void _PhysicsProcess(double delta) => _movement.Advance((float)delta);

    // =========================================================
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut) _input.ReleaseMouse();
    }

    // =========================================================
    public override void _ExitTree() => _input.ReleaseMouse();
}
