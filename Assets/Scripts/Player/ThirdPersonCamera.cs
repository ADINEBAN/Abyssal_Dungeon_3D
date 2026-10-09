using UnityEngine;

namespace AbyssalDungeon.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

        [Header("Camera Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float sensitivity = 3f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Limits")]
        [SerializeField] private float yMinLimit = -20f;
        [SerializeField] private float yMaxLimit = 80f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayer;
        [SerializeField] private float collisionOffset = 0.2f;

        private float currentX = 0f;
        private float currentY = 20f;
        private Vector3 currentVelocity;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            
            Vector3 targetPos = target.position + offset;
            
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 desiredPosition = targetPos - (rotation * Vector3.forward * distance);

            // Handle Camera Collision
            if (Physics.Raycast(targetPos, desiredPosition - targetPos, out RaycastHit hit, distance, collisionLayer))
            {
                desiredPosition = hit.point + hit.normal * collisionOffset;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(targetPos);
        }

        private void HandleInput()
        {
            currentX += Input.GetAxis("Mouse X") * sensitivity;
            currentY -= Input.GetAxis("Mouse Y") * sensitivity;
            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                distance -= scroll * 5f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
