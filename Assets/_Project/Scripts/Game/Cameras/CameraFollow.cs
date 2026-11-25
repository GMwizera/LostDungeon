using UnityEngine;

namespace LostDungeon.Game.Cameras
{
    /// <summary>
    /// Isometric-style 3D camera: looks down at the hero from a fixed angle (45 degrees around, pitched down)
    /// and smoothly follows them across the floor. Movement input is turned by the same angle,
    /// so pressing "up" always walks up the screen.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float pitch = 40f;
        [SerializeField] private float distance = 10.5f;
        [SerializeField] private float lookHeight = 0.8f;

        private Vector3 velocity;

        /// <summary>The camera's turn around the vertical axis. Player input uses it.</summary>
        public static float Yaw { get; private set; } = 45f;

        private void Awake()
        {
            Yaw = yaw;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Jumps straight to the target, e.g. when a level starts.</summary>
        public void SnapToTarget()
        {
            if (target == null)
                return;
            transform.SetPositionAndRotation(Goal(), Quaternion.Euler(pitch, yaw, 0f));
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            transform.position = Vector3.SmoothDamp(transform.position, Goal(), ref velocity, smoothTime);
        }

        private Vector3 Goal()
        {
            Vector3 focus = target.position + Vector3.up * lookHeight;
            return focus - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
        }
    }
}
