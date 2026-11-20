using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// The only player script that talks to the Input System.
    /// Other scripts read Move or listen to the events, so changing controls
    /// (keyboard, gamepad, touch joystick) never touches gameplay code.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }

        public event Action AttackPressed;
        public event Action DashPressed;
        public event Action FireballPressed;
        public event Action InteractPressed;
        public event Action PausePressed;

        private InputAction move;
        private InputAction attack;
        private InputAction dash;
        private InputAction fireball;
        private InputAction interact;
        private InputAction pause;

        private void Awake()
        {
            // Project-wide actions: Assets/Settings/InputSystem_Actions.inputactions
            InputActionMap map = InputSystem.actions.FindActionMap("Player", throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            attack = map.FindAction("Attack", throwIfNotFound: true);
            dash = map.FindAction("Dash", throwIfNotFound: true);
            fireball = map.FindAction("Fireball", throwIfNotFound: true);
            interact = map.FindAction("Interact", throwIfNotFound: true);
            pause = map.FindAction("Pause", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            attack.performed += OnAttack;
            dash.performed += OnDash;
            fireball.performed += OnFireball;
            interact.performed += OnInteract;
            pause.performed += OnPause;
        }

        private void OnDisable()
        {
            attack.performed -= OnAttack;
            dash.performed -= OnDash;
            fireball.performed -= OnFireball;
            interact.performed -= OnInteract;
            pause.performed -= OnPause;
            Move = Vector2.zero;
        }

        private void Update()
        {
            Move = move.ReadValue<Vector2>();
        }

        private void OnAttack(InputAction.CallbackContext _) => AttackPressed?.Invoke();
        private void OnDash(InputAction.CallbackContext _) => DashPressed?.Invoke();
        private void OnFireball(InputAction.CallbackContext _) => FireballPressed?.Invoke();
        private void OnInteract(InputAction.CallbackContext _) => InteractPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();
    }
}
