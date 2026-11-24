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
            virtualMove = Vector2.zero;
        }

        private Vector2 virtualMove;

        private void Update()
        {
            Vector2 device = move.ReadValue<Vector2>();
            Move = device.sqrMagnitude > 0.01f ? device : virtualMove;
        }

        /// <summary>On-screen joystick input (mobile). Keyboard or gamepad still wins when used.</summary>
        public void SetVirtualMove(Vector2 value) => virtualMove = Vector2.ClampMagnitude(value, 1f);

        /// <summary>On-screen buttons call these, so touch goes through the same events as keys.</summary>
        public void PressAttack() { if (isActiveAndEnabled) AttackPressed?.Invoke(); }
        public void PressDash() { if (isActiveAndEnabled) DashPressed?.Invoke(); }
        public void PressFireball() { if (isActiveAndEnabled) FireballPressed?.Invoke(); }
        public void PressInteract() { if (isActiveAndEnabled) InteractPressed?.Invoke(); }

        private void OnAttack(InputAction.CallbackContext _) => AttackPressed?.Invoke();
        private void OnDash(InputAction.CallbackContext _) => DashPressed?.Invoke();
        private void OnFireball(InputAction.CallbackContext _) => FireballPressed?.Invoke();
        private void OnInteract(InputAction.CallbackContext _) => InteractPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();
    }
}
