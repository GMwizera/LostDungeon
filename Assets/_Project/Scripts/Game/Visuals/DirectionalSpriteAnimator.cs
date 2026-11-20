using LostDungeon.Core.Movement;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>
    /// Picks the sprite that matches the direction a Rigidbody2D is moving,
    /// and plays the run frames while it moves. Works for the player and enemies
    /// because it only looks at velocity, not at who is driving it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private CharacterSpriteSet sprites;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private float framesPerSecond = 14f;
        [SerializeField] private float movingThreshold = 0.05f;

        private SpriteRenderer spriteRenderer;
        private float frameTimer;

        /// <summary>Current facing, 0 to 7. See EightWayDirection for the order.</summary>
        public int Direction { get; private set; } = EightWayDirection.Down;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (body == null)
                body = GetComponentInParent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            if (sprites == null || body == null)
                return;

            Vector2 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude > movingThreshold * movingThreshold)
            {
                Direction = EightWayDirection.FromVector(velocity.x, velocity.y);
                frameTimer += Time.deltaTime * framesPerSecond;
                spriteRenderer.sprite = sprites.GetRun(Direction, (int)frameTimer);
            }
            else
            {
                frameTimer = 0f;
                spriteRenderer.sprite = sprites.GetIdle(Direction);
            }
        }
    }
}
