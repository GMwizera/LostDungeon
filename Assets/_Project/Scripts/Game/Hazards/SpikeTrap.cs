using LostDungeon.Core.Combat;
using LostDungeon.Game.Combat;
using UnityEngine;

namespace LostDungeon.Game.Hazards
{
    /// <summary>
    /// Floor spikes that rise and fall. While they are up they hurt anything that implements IDamageable
    /// standing on them, whether that is the player or an enemy. Needs a trigger Collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpikeTrap : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private Transform spikes;
        [SerializeField] private float cycleSeconds = 2.4f;
        [SerializeField] private float upFraction = 0.45f;
        [SerializeField] private float hiddenDepth = 0.45f;

        private readonly System.Collections.Generic.Dictionary<IDamageable, float> nextHit =
            new System.Collections.Generic.Dictionary<IDamageable, float>();
        private Vector3 spikesUp;
        private float offset;

        public bool SpikesUp => Mathf.Repeat(Time.time + offset, cycleSeconds) < cycleSeconds * upFraction;

        private void Awake()
        {
            if (spikes != null)
                spikesUp = spikes.localPosition;
            offset = transform.position.x * 0.37f + transform.position.z * 0.61f; // traps don't all move together
        }

        private void Update()
        {
            if (spikes == null)
                return;
            float target = SpikesUp ? 0f : -hiddenDepth;
            Vector3 position = spikes.localPosition;
            position.y = Mathf.MoveTowards(position.y, spikesUp.y + target, Time.deltaTime * 4f);
            spikes.localPosition = position;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!SpikesUp)
                return;
            GameObject who = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (!who.TryGetComponent(out IDamageable target) || target.IsDead)
                return;
            if (nextHit.TryGetValue(target, out float time) && Time.time < time)
                return; // each victim is hurt at most twice a second
            nextHit[target] = Time.time + 0.5f;
            target.TakeDamage(damage);
            CombatFeedback.RaiseHit(who, damage, false);
        }
    }
}
