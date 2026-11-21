using System.Collections.Generic;
using LostDungeon.Core.Combat;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>Close-range strike: damages every IDamageable in a small circle in front of the user.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Melee Attack")]
    public class MeleeAttackAbility : AbilityDefinition
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float reach = 0.3f;
        [SerializeField] private float radius = 0.3f;
        [SerializeField, Range(0f, 1f)] private float critChance = 0.15f;
        [SerializeField] private float critMultiplier = 2f;

        private readonly List<Collider2D> hits = new List<Collider2D>();
        private readonly HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        public override void Activate(in AbilityContext context)
        {
            Vector2 center = context.Origin + context.Direction * reach;
            Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, hits);

            damaged.Clear();
            foreach (Collider2D hit in hits)
            {
                GameObject target = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
                if (target == context.Owner || !target.TryGetComponent(out IDamageable damageable))
                    continue;
                if (damageable.IsDead || !damaged.Add(damageable))
                    continue; // already hit this swing (it may have several colliders)

                DamageResult result = DamageCalculator.Roll(damage, critChance, critMultiplier, Random.value);
                damageable.TakeDamage(result.Amount);
            }
        }
    }
}
