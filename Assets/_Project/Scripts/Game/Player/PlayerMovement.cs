using LostDungeon.Game.Cameras;
using LostDungeon.Game.Movement;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Moves the hero across the 3D floor with physics, so walls stop them.
    /// Input is turned to match the camera angle, so "up" always walks up the screen.
    /// Implements IDashable so the Dash ability can push it without knowing it is the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour, IDashable
    {
        [SerializeField] private float moveSpeed = 2.8f;

        private Rigidbody body;
        private PlayerInputReader input;
        private Vector2 dashVelocity;
        private float dashTimeLeft;

        /// <summary>Last floor direction the player moved in. Abilities aim with this.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        public bool IsDashing => dashTimeLeft > 0f;

        /// <summary>Current movement on the floor, used by the character animator.</summary>
        public Vector2 FloorVelocity => Ground.ToFloor(body.linearVelocity);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // dashes must not pass through walls
            input = GetComponent<PlayerInputReader>();
        }

        private Vector2 FloorInput => Ground.ScreenToFloor(Vector2.ClampMagnitude(input.Move, 1f), CameraFollow.Yaw);

        private void Update()
        {
            Vector2 move = FloorInput;
            if (move.sqrMagnitude > 0.01f && !IsDashing)
                FacingDirection = move.normalized;
        }

        private void FixedUpdate()
        {
            if (IsDashing)
            {
                body.linearVelocity = Ground.ToWorld(dashVelocity);
                dashTimeLeft -= Time.fixedDeltaTime;
                return;
            }

            body.linearVelocity = Ground.ToWorld(FloorInput * moveSpeed);
        }

        public void Dash(Vector2 direction, float speed, float duration)
        {
            dashVelocity = direction.normalized * speed;
            dashTimeLeft = duration;
        }

        private void OnDisable()
        {
            dashTimeLeft = 0f;
            if (body != null && !body.isKinematic)
                body.linearVelocity = Vector3.zero;
        }
    }
}
