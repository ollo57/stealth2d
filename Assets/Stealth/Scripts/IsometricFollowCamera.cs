using UnityEngine;

namespace AlleyStealth
{
    public sealed class IsometricFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.18f;
        [SerializeField] private Vector2 horizontalLimits = new Vector2(-11f, 11f);
        [SerializeField] private Vector2 depthLimits = new Vector2(-1.5f, 3f);
        [SerializeField] private Vector3 viewOffset = new Vector3(7.35f, 11.2f, -12.73f);
        private Vector3 followVelocity;

        private void LateUpdate()
        {
            if (player == null) return;
            Vector3 focus = new Vector3(
                Mathf.Clamp(player.position.x, horizontalLimits.x, horizontalLimits.y), 0f,
                Mathf.Clamp(player.position.z, depthLimits.x, depthLimits.y));
            transform.position = Vector3.SmoothDamp(transform.position, focus + viewOffset,
                ref followVelocity, followSmoothTime);
        }
    }
}
