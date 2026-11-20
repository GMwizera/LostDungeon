using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Top-down movement driven by physics, so walls stop the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;

        private Rigidbody2D body;
        private PlayerInputReader input;

        /// <summary>Last direction the player moved in. Abilities will aim with this.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.01f)
                FacingDirection = move.normalized;
        }

        private void FixedUpdate()
        {
            // ClampMagnitude stops diagonal movement being faster than straight movement.
            body.linearVelocity = Vector2.ClampMagnitude(input.Move, 1f) * moveSpeed;
        }

        private void OnDisable()
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
