using UnityEngine;
using UnityEngine.InputSystem;

namespace AlleyStealth
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FreeMovementController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Camera movementCamera;
        [Header("Movement across the floor")]
        [SerializeField, Min(0.1f)] private float movementSpeed = 4f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0.1f)] private float groundStickSpeed = 2f;
        private CharacterController characterController;
        private InputAction moveAction;
        private float verticalSpeed;
        public Vector3 ActualMovement { get; private set; }
        public float ScreenHorizontalMovement { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (movementCamera == null) movementCamera = Camera.main;
            if (inputActions == null)
            {
                Debug.LogError("Assign the project's input actions to the player.", this);
                enabled = false;
                return;
            }
            // Own this action so enabling/disabling the player does not affect other input users.
            moveAction = inputActions.FindAction("Player/Move", true).Clone();
        }

        private void OnEnable() => moveAction?.Enable();
        private void OnDisable()
        {
            moveAction?.Disable();
            ActualMovement = Vector3.zero;
            ScreenHorizontalMovement = 0f;
            verticalSpeed = 0f;
        }
        private void OnDestroy() => moveAction?.Dispose();

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            Vector3 forward = movementCamera != null ? movementCamera.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // Flatten the camera direction onto the floor: W moves up the picture, not into the air.
            Vector3 velocity = (right * input.x + forward * input.y) * movementSpeed;
            if (characterController.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -groundStickSpeed;
            verticalSpeed += gravity * Time.deltaTime;
            velocity.y = verticalSpeed;
            Vector3 previousPosition = transform.position;
            characterController.Move(velocity * Time.deltaTime);
            Vector3 displacement = transform.position - previousPosition;
            displacement.y = 0f;
            ActualMovement = displacement;
            ScreenHorizontalMovement = Vector3.Dot(displacement, right);
        }
    }
}
