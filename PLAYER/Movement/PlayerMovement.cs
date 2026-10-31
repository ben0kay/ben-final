// Yaw-relative physics locomotion, sprint, jump and gravity; no duplicated input bindings.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerMovement
    {
        private readonly PlayerController _player;
        public PlayerMovement(PlayerController player) => _player = player;

        // =========================================================
        public void Advance(float dt)
        {
            _player._stance.Advance(dt);
            Vector2 keys = _player._input.MovementAxis();
            Vector3 forward = -_player.GlobalBasis.Z, right = _player.GlobalBasis.X;
            forward.Y = 0; right.Y = 0;
            Vector3 direction = (forward.Normalized() * keys.Y + right.Normalized() * keys.X).Normalized();

            var settings = _player.Settings;
            float speed = Mathf.Max(0.1f, settings.WalkSpeed);
            if (_player.IsCrouching) speed *= Mathf.Clamp(settings.CrouchSpeedMultiplier, 0.1f, 1f);
            else if (_player._input.SprintHeld) speed *= Mathf.Max(1f, settings.SprintMultiplier);

            Vector3 horizontal = new(_player.Velocity.X, 0, _player.Velocity.Z);
            Vector3 target = direction * speed;
            float acceleration = target.LengthSquared() < 0.0001f ? settings.Deceleration
                : horizontal.Dot(target) < 0 ? settings.ReverseAcceleration : settings.Acceleration;
            horizontal = horizontal.MoveToward(target, Mathf.Max(1f, acceleration) * dt);

            bool jump = _player._input.ConsumeJump() && _player.IsOnFloor() && !_player.IsCrouching;
            float vertical = jump ? Mathf.Max(0f, settings.JumpVelocity)
                : _player.IsOnFloor() ? -0.1f : _player.Velocity.Y - Mathf.Max(1f, settings.Gravity) * dt;
            _player.Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
            _player.MoveAndSlide();
        }
    }
}
