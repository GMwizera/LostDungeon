using LostDungeon.Game.Combat;
using LostDungeon.Game.Pooling;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// Fires a pooled projectile in the facing direction. The Fireball asset uses this;
    /// a different projectile prefab and numbers make a different ability with no new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Projectile")]
    public class ProjectileAbility : AbilityDefinition
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float speed = 6f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float spawnDistance = 0.25f;

        public override void Activate(in AbilityContext context)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{DisplayName} has no projectile prefab.", this);
                return;
            }

            Vector2 spawnPoint = context.Origin + context.Direction * spawnDistance;
            Projectile projectile = PoolManager.Instance.Spawn(projectilePrefab, spawnPoint, Quaternion.identity);
            projectile.Launch(context.Owner, context.Direction, speed, damage);
        }
    }
}
