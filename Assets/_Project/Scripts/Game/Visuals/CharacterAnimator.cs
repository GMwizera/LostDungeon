using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>Plays the 3D character's animations and turns it to face where it goes.</summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public const string AttackTrigger = "Attack";
        public const string CastTrigger = "Cast";

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform model;
        [SerializeField] private Rigidbody body;
        [SerializeField] private float runSpeed = 2.8f;
        [SerializeField] private float turnSpeed = 720f;

        private Quaternion wantedRotation = Quaternion.identity;
        private float lockFacingUntil;

        public Transform Model => model;

        private void Awake()
        {
            if (body == null) body = GetComponentInParent<Rigidbody>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (model == null && animator != null) model = animator.transform;
            if (model != null) wantedRotation = model.rotation;
        }

        /// <summary>Turns to face a direction, like an enemy facing the player.</summary>
        public void FaceDirection(Vector2 direction, bool instant = false)
        {
            if (direction.sqrMagnitude < 0.0001f || model == null)
                return;
            wantedRotation = Ground.Facing(direction);
            lockFacingUntil = Time.time + 0.25f; // keep facing it for a moment, even if it slides
            if (instant)
                model.rotation = wantedRotation;
        }

        /// <summary>Plays an action once, like "Attack" or "Cast".</summary>
        public void PlayAction(string trigger)
        {
            if (animator != null && !string.IsNullOrEmpty(trigger))
                animator.SetTrigger(trigger);
        }

        public void PlayHit()
        {
            if (animator != null)
                animator.SetTrigger(HitId);
        }

        public void SetDead(bool dead)
        {
            if (animator == null)
                return;
            animator.SetBool(DeadId, dead);
            if (!dead)
            {
                // Back from the pool, so start from idle again.
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void LateUpdate()
        {
            if (model == null)
                return;

            Vector2 velocity = body != null ? Ground.ToFloor(body.linearVelocity) : Vector2.zero;
            float speed = velocity.magnitude;
            if (animator != null)
                animator.SetFloat(SpeedId, Mathf.Clamp01(speed / runSpeed), 0.08f, Time.deltaTime);

            if (speed > 0.1f && Time.time >= lockFacingUntil)
                wantedRotation = Ground.Facing(velocity);
            model.rotation = Quaternion.RotateTowards(model.rotation, wantedRotation, turnSpeed * Time.deltaTime);
        }
    }
}
