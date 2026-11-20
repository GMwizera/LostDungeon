using LostDungeon.Core.Combat;
using UnityEngine;

namespace LostDungeon.Game.Hazards
{
    /// <summary>
    /// Hurts anything that implements IDamageable while it stands on the trap.
    /// It does not know or care whether that is the player or an enemy.
    /// Needs a Collider2D set to "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpikeTrap : MonoBehaviour
    {
        [SerializeField] private int damage = 1;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.TryGetComponent(out IDamageable target) && !target.IsDead)
                target.TakeDamage(damage);
        }
    }
}
