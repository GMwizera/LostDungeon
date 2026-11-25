using LostDungeon.Game.Cameras;
using LostDungeon.Game.Movement;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Moves the hero with physics, so walls stop them.
    /// Input follows the camera, so "up" always goes up the screen.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour, IDashable
    {
        [SerializeField] private float moveSpeed = 2.8f;

        private Rigidbody body;
        private PlayerInputReader input;
        private Vector2 dashVelocity;
        private float dashTimeLeft;

        /// <summary>The last way the player moved. Abilities aim this way.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        public bool IsDashing => dashTimeLeft > 0f;

        /// <summary>How fast the player moves, for the animations.</summary>
        public Vector2 FloorVelocity => Ground.ToFloor(body.linearVelocity);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // so fast dashes don't go through walls
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
