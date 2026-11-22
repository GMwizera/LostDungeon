using LostDungeon.Game.Abilities;
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// Everything that makes one enemy type different, as an asset.
    /// To add an enemy type: make a new EnemyData, pick a behaviour and an attack ability. No code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "Enemy";
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private float scale = 1f;

        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0)] private int armor;
        [SerializeField] private float moveSpeed = 1.6f;

        [Header("Ranges (world units)")]
        [SerializeField] private float detectionRange = 3.5f;
        [SerializeField] private float attackRange = 0.45f;
        [SerializeField] private float preferredDistance = 2f;
        [SerializeField] private float guardRadius = 2f;

        [Header("Strategies")]
        [SerializeField] private EnemyBehaviour behaviour;
        [SerializeField] private AbilityDefinition attack;

        public string Id => id;
        public Color Tint => tint;
        public float Scale => scale;
        public int MaxHealth => maxHealth;
        public int Armor => armor;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float PreferredDistance => preferredDistance;
        public float GuardRadius => guardRadius;
        public IEnemyBehaviour Behaviour => behaviour;
        public IAbility Attack => attack;
    }
}
