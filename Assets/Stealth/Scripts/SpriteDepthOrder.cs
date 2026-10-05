using UnityEngine;

namespace AlleyStealth
{
    [ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteDepthOrder : MonoBehaviour
    {
        [SerializeField] private Transform groundAnchor;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private int orderOffset;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (groundAnchor == null || gameplayCamera == null) return;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            Vector3 groundPosition = groundAnchor.position;
            groundPosition.y = 0f;
            // Sort by the feet, so a tall object's top cannot put it behind a shorter object.
            float depth = Vector3.Dot(groundPosition, gameplayCamera.transform.forward);
            spriteRenderer.sortingOrder = -Mathf.RoundToInt(depth * 100f) + orderOffset;
        }
    }
}
