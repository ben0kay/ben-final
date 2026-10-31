// Crouch toggle, smooth capsule/camera height, and clearance guard before standing up.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerStance
    {
        private readonly PlayerController _player;
        private CollisionShape3D _collider = null!;
        private CapsuleShape3D _capsule = null!;
        private PhysicsShapeQueryParameters3D _standingQuery = null!;
        public bool IsCrouching { get; private set; }
        public PlayerStance(PlayerController player) => _player = player;

        // =========================================================
        public void Initialize()
        {
            _collider = _player.GetNode<CollisionShape3D>("CollisionShape3D");
            _capsule = (CapsuleShape3D)_collider.Shape.Duplicate();
            _collider.Shape = _capsule;
            var standing = (CapsuleShape3D)_capsule.Duplicate();
            standing.Height = Mathf.Max(standing.Radius * 2f, _player.Settings.StandingHeight);
            _standingQuery = new PhysicsShapeQueryParameters3D
            {
                Shape = standing,
                CollisionMask = _player.CollisionMask,
                Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() }
            };
            IsCrouching = false;
            _capsule.Height = standing.Height;
            _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
            _player._cameraPivot.Position = Vector3.Up * _player.Settings.StandingEyeHeight;
        }

        // =========================================================
        public void Advance(float dt)
        {
            if (_player._input.ConsumeCrouch())
            {
                if (IsCrouching) { if (CanStand()) IsCrouching = false; }
                else if (_player.IsOnFloor()) IsCrouching = true;
            }

            var settings = _player.Settings;
            float target = IsCrouching ? settings.CrouchHeight : settings.StandingHeight;
            _capsule.Height = Mathf.MoveToward(_capsule.Height, Mathf.Max(_capsule.Radius * 2f, target),
                Mathf.Max(0.1f, settings.StanceSpeed) * dt);
            _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
            float eyeTarget = IsCrouching ? settings.CrouchEyeHeight : settings.StandingEyeHeight;
            float eye = Mathf.MoveToward(_player._cameraPivot.Position.Y, eyeTarget, Mathf.Max(0.1f, settings.StanceSpeed) * dt);
            _player._cameraPivot.Position = Vector3.Up * eye;
        }

        // =========================================================
        private bool CanStand()
        {
            float height = Mathf.Max(_capsule.Radius * 2f, _player.Settings.StandingHeight);
            _standingQuery.Transform = _player.GlobalTransform
                * new Transform3D(Basis.Identity, Vector3.Up * (height * 0.5f + 0.03f));
            return _player.GetWorld3D().DirectSpaceState.IntersectShape(_standingQuery, 1).Count == 0;
        }
    }
}
