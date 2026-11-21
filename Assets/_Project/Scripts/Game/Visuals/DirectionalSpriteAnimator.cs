using LostDungeon.Core.Movement;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>
    /// Picks the sprite that matches the direction a Rigidbody2D is moving,
    /// plays the run frames while it moves, and plays the action frames once when asked (attacks).
    /// Works for the player and enemies because it only looks at velocity, not at who is driving it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private CharacterSpriteSet sprites;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private float framesPerSecond = 14f;
        [SerializeField] private float actionFramesPerSecond = 30f;
        [SerializeField] private float movingThreshold = 0.05f;

        private SpriteRenderer spriteRenderer;
        private float frameTimer;
        private float actionTimer = -1f; // below zero = no action playing

        /// <summary>Current facing, 0 to 7. See EightWayDirection for the order.</summary>
        public int Direction { get; private set; } = EightWayDirection.Down;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (body == null)
                body = GetComponentInParent<Rigidbody2D>();
        }

        /// <summary>Plays the action frames once in the current facing direction.</summary>
        public void PlayAction()
        {
            if (sprites != null && sprites.ActionFrameCount(Direction) > 0)
                actionTimer = 0f;
        }

        private void LateUpdate()
        {
            if (sprites == null || body == null)
                return;

            Vector2 velocity = body.linearVelocity;
            bool moving = velocity.sqrMagnitude > movingThreshold * movingThreshold;
            if (moving)
                Direction = EightWayDirection.FromVector(velocity.x, velocity.y);

            if (actionTimer >= 0f)
            {
                int frame = (int)actionTimer;
                if (frame < sprites.ActionFrameCount(Direction))
                {
                    spriteRenderer.sprite = sprites.GetAction(Direction, frame);
                    actionTimer += Time.deltaTime * actionFramesPerSecond;
                    return;
                }
                actionTimer = -1f; // finished
            }

            if (moving)
            {
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
