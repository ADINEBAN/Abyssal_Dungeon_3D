using System;
using System.Collections;
using UnityEngine;

namespace AbyssalDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class DodgeSystem : MonoBehaviour
    {
        [Header("Dodge Settings")]
        [SerializeField] private float dodgeDistance = 5f;
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 1f;
        [SerializeField] private float staminaCost = 10f; // Could integrate with stamina later
        
        [Header("VFX")]
        [SerializeField] private GameObject dodgeVfxPrefab;
        [SerializeField] private TrailRenderer dodgeTrail;

        private CharacterController controller;
        private float lastDodgeTime;
        public bool isInvincible { get; private set; }

        public event Action OnDodgeStart;
        public event Action OnDodgeEnd;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (dodgeTrail != null) dodgeTrail.enabled = false;
        }

        public void TryDodge(Vector3 inputDirection, Transform cameraTransform)
        {
            if (Time.time < lastDodgeTime + dodgeCooldown) return;

            Vector3 dodgeDirection = transform.forward;
            if (inputDirection.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                dodgeDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            }

            StartCoroutine(DodgeRoutine(dodgeDirection.normalized));
        }

        private IEnumerator DodgeRoutine(Vector3 direction)
        {
            lastDodgeTime = Time.time;
            isInvincible = true;
            OnDodgeStart?.Invoke();

            if (dodgeTrail != null) dodgeTrail.enabled = true;
            if (dodgeVfxPrefab != null)
            {
                Instantiate(dodgeVfxPrefab, transform.position, transform.rotation);
            }

            float timer = 0f;
            float dodgeSpeed = dodgeDistance / dodgeDuration;

            while (timer < dodgeDuration)
            {
                controller.Move(direction * (dodgeSpeed * Time.deltaTime));
                timer += Time.deltaTime;
                yield return null;
            }

            isInvincible = false;
            if (dodgeTrail != null) dodgeTrail.enabled = false;
            OnDodgeEnd?.Invoke();
        }
    }
}
