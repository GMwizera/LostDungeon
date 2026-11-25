using LostDungeon.Core.Events;
using UnityEngine;

namespace LostDungeon.Game.Cameras
{
    /// <summary>Looks down at the hero from an angle and follows them smoothly.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float pitch = 40f;
        [SerializeField] private float distance = 10.5f;
        [SerializeField] private float lookHeight = 0.8f;

        [SerializeField] private float hurtShake = 0.18f;
        [SerializeField] private float shakeSeconds = 0.25f;

        private Vector3 velocity;
        private Vector3 followPosition;
        private float shakeLeft;
        private int lastHealth = -1;

        /// <summary>How far the camera is turned. Player input uses it.</summary>
        public static float Yaw { get; private set; } = 45f;

        private void Awake()
        {
            Yaw = yaw;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            followPosition = transform.position;
        }

        private void OnEnable() => GameEvents.PlayerHealthChanged += OnPlayerHealthChanged;
        private void OnDisable() => GameEvents.PlayerHealthChanged -= OnPlayerHealthChanged;

        /// <summary>Shakes a little when the hero gets hurt.</summary>
        private void OnPlayerHealthChanged(int current, int max)
        {
            if (lastHealth >= 0 && current < lastHealth)
                shakeLeft = shakeSeconds;
            lastHealth = current;
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Jumps right to the target, like when a level starts.</summary>
        public void SnapToTarget()
        {
            if (target == null)
                return;
            followPosition = Goal();
            transform.SetPositionAndRotation(followPosition, Quaternion.Euler(pitch, yaw, 0f));
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            followPosition = Vector3.SmoothDamp(followPosition, Goal(), ref velocity, smoothTime);
            Vector3 shake = Vector3.zero;
            if (shakeLeft > 0f)
            {
                shakeLeft -= Time.deltaTime;
                shake = Random.insideUnitSphere * (hurtShake * Mathf.Clamp01(shakeLeft / shakeSeconds));
            }
            transform.position = followPosition + shake;
        }

        private Vector3 Goal()
        {
            Vector3 focus = target.position + Vector3.up * lookHeight;
            return focus - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
        }
    }
}
