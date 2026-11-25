using System.Collections.Generic;
using LostDungeon.Core.Combat;
using LostDungeon.Game.Combat;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>Close-range strike: damages every IDamageable in a small sphere in front of the user.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Melee Attack")]
    public class MeleeAttackAbility : AbilityDefinition
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float reach = 0.45f;
        [SerializeField] private float radius = 0.45f;
        [SerializeField, Range(0f, 1f)] private float critChance = 0.15f;
        [SerializeField] private float critMultiplier = 2f;

        private readonly Collider[] hits = new Collider[16];
        private readonly HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        protected override void Perform(in AbilityContext context)
        {
            Vector2 center = context.Origin + context.Direction * reach;
            int count = Physics.OverlapSphereNonAlloc(Ground.ToWorld(center, Ground.BodyHeight), radius, hits,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);

            damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                GameObject target = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
                if (target == context.Owner || Faction.AreAllies(target, context.Owner) ||
                    !target.TryGetComponent(out IDamageable damageable))
                    continue;
                if (damageable.IsDead || !damaged.Add(damageable))
                    continue; // already hit this swing (it may have several colliders)

                DamageResult result = DamageCalculator.Roll(damage, critChance, critMultiplier, Random.value);
                damageable.TakeDamage(result.Amount);
                CombatFeedback.RaiseHit(target, result.Amount, result.IsCritical);
            }
        }
    }
}
