using LostDungeon.Game.Combat;
using LostDungeon.Game.Pooling;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// Fires a pooled projectile in the facing direction. The Fireball and the skeleton archer's
    /// arrow both use this class: a different projectile prefab and numbers make a different ability.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Projectile")]
    public class ProjectileAbility : AbilityDefinition
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float speed = 7f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float spawnDistance = 0.4f;

        protected override void Perform(in AbilityContext context)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{DisplayName} has no projectile prefab.", this);
                return;
            }

            Vector2 spawnPoint = context.Origin + context.Direction * spawnDistance;
            Projectile projectile = PoolManager.Instance.Spawn(projectilePrefab, Ground.ToWorld(spawnPoint), Ground.Facing(context.Direction));
            projectile.Launch(context.Owner, context.Direction, speed, damage);
        }
    }
}
