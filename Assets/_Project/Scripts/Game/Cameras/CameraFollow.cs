using UnityEngine;

namespace LostDungeon.Game.Cameras
{
    /// <summary>Smoothly keeps the camera centred on a target.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 velocity;

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Jumps straight to the target, e.g. when a level starts.</summary>
        public void SnapToTarget()
        {
            if (target == null)
                return;
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
