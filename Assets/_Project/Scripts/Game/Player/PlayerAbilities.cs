using System.Collections.Generic;
using LostDungeon.Core.Events;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Visuals;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Connects the player's buttons to three ability slots. It never knows what an ability does:
    /// it checks the cooldown, calls Activate, and announces it through GameEvents.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private AbilityDefinition attack;
        [SerializeField] private AbilityDefinition dash;
        [SerializeField] private AbilityDefinition special;
        [SerializeField] private DirectionalSpriteAnimator spriteAnimator;

        private readonly List<AbilitySlot> slots = new List<AbilitySlot>();
        private PlayerInputReader input;
        private PlayerMovement movement;
        private AbilitySlot attackSlot;
        private AbilitySlot dashSlot;
        private AbilitySlot specialSlot;

        /// <summary>For the HUD's ability indicators (Day 5).</summary>
        public IReadOnlyList<AbilitySlot> Slots => slots;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement>();
            if (spriteAnimator == null)
                spriteAnimator = GetComponentInChildren<DirectionalSpriteAnimator>();

            attackSlot = AddSlot(attack);
            dashSlot = AddSlot(dash);
            specialSlot = AddSlot(special);
        }

        private void OnEnable()
        {
            input.AttackPressed += OnAttack;
            input.DashPressed += OnDash;
            input.FireballPressed += OnSpecial;
        }

        private void OnDisable()
        {
            input.AttackPressed -= OnAttack;
            input.DashPressed -= OnDash;
            input.FireballPressed -= OnSpecial;
        }

        /// <summary>The special ability (Fireball) is a reward for finishing level 1.</summary>
        public void SetSpecialUnlocked(bool unlocked)
        {
            if (specialSlot != null)
                specialSlot.Unlocked = unlocked;
        }

        private void Update()
        {
            foreach (AbilitySlot slot in slots)
                slot.Cooldown.Tick(Time.deltaTime);
        }

        private void OnAttack() => TryUse(attackSlot);
        private void OnDash() => TryUse(dashSlot);
        private void OnSpecial() => TryUse(specialSlot);

        private void TryUse(AbilitySlot slot)
        {
            if (slot == null || !slot.IsReady || Time.timeScale == 0f)
                return; // no attacks while paused or on a menu screen

            var context = new AbilityContext(gameObject, transform.position, movement.FacingDirection);
            slot.Ability.Activate(context);
            slot.Cooldown.Start();

            if (slot.Ability.PlaysActionAnimation && spriteAnimator != null)
                spriteAnimator.PlayAction();

            GameEvents.RaiseAbilityActivated(slot.Ability.DisplayName, slot.Ability.CooldownSeconds);
        }

        private AbilitySlot AddSlot(AbilityDefinition ability)
        {
            if (ability == null)
                return null;
            var slot = new AbilitySlot(ability);
            slots.Add(slot);
            return slot;
        }
    }
}
