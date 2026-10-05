using UnityEngine;

namespace AlleyStealth
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CatSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private FreeMovementController player;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;
        private SpriteRenderer spriteRenderer;
        private float animationTime;
        private bool wasWalking;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void LateUpdate()
        {
            if (player == null) return;
            bool walking = player.ActualMovement.sqrMagnitude > 0.000001f;
            if (walking != wasWalking) animationTime = 0f;
            wasWalking = walking;
            animationTime += Time.deltaTime;
            Sprite[] frames = walking ? walkFrames : idleFrames;
            if (frames.Length > 0)
                spriteRenderer.sprite = frames[Mathf.FloorToInt(animationTime * framesPerSecond) % frames.Length];
            // Flip the picture, not the collision capsule or camera-facing sprite plane.
            if (Mathf.Abs(player.ScreenHorizontalMovement) > 0.0001f)
                spriteRenderer.flipX = player.ScreenHorizontalMovement < 0f;
        }
    }
}
