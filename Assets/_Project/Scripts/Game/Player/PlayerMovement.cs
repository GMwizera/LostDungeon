using LostDungeon.Game.Movement;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Top-down movement driven by physics, so walls stop the player.
    /// Implements IDashable so the Dash ability can push it without knowing it is the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour, IDashable
    {
        [SerializeField] private float moveSpeed = 2.5f;

        private Rigidbody2D body;
        private PlayerInputReader input;
        private Vector2 dashVelocity;
        private float dashTimeLeft;

        /// <summary>Last direction the player moved in. Abilities aim with this.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        public bool IsDashing => dashTimeLeft > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.01f && !IsDashing)
                FacingDirection = move.normalized;
        }

        private void FixedUpdate()
        {
            if (IsDashing)
            {
                body.linearVelocity = dashVelocity;
                dashTimeLeft -= Time.fixedDeltaTime;
                return;
            }

            // ClampMagnitude stops diagonal movement being faster than straight movement.
            body.linearVelocity = Vector2.ClampMagnitude(input.Move, 1f) * moveSpeed;
        }

        public void Dash(Vector2 direction, float speed, float duration)
        {
            dashVelocity = direction.normalized * speed;
            dashTimeLeft = duration;
        }

        private void OnDisable()
        {
            dashTimeLeft = 0f;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
