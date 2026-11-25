using LostDungeon.Game.Combat;
using LostDungeon.Game.Pooling;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>Listens for hits (Observer) and shows pooled sparks where they land.</summary>
    public class CombatEffectsPresenter : MonoBehaviour
    {
        [SerializeField] private PooledEffect hitSparks;
        [SerializeField] private PooledEffect critSparks;
        [SerializeField] private PooledEffect playerHurt;

        private void OnEnable() => CombatFeedback.Hit += OnHit;
        private void OnDisable() => CombatFeedback.Hit -= OnHit;

        private void OnHit(Vector3 position, int amount, bool critical, bool playerWasHurt)
        {
            PooledEffect effect = playerWasHurt ? playerHurt : critical ? critSparks : hitSparks;
            if (effect != null && PoolManager.HasInstance)
                PoolManager.Instance.Spawn(effect, position + Vector3.down * 0.4f, Quaternion.identity);
        }
    }
}
