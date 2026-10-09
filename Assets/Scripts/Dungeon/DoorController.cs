using System.Collections;
using UnityEngine;

namespace AbyssalDungeon.Dungeon
{
    [RequireComponent(typeof(BoxCollider))]
    public class DoorController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;

        [Header("References")]
        [SerializeField] private Transform doorVisuals;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private Collider triggerCollider;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem openVFX;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip lockedSound;

        [Header("Animation Settings")]
        [SerializeField] private Vector3 closedPosition;
        [SerializeField] private Vector3 openPosition;
        [SerializeField] private float animationDuration = 1.0f;

        private Coroutine animationCoroutine;

        private void Awake()
        {
            if (doorVisuals == null) doorVisuals = transform.GetChild(0);
            if (blockingCollider == null) blockingCollider = GetComponent<Collider>();
            
            closedPosition = doorVisuals.localPosition;
            openPosition = closedPosition + Vector3.up * 4f; // Example: moves up to open
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (isLocked)
                {
                    PlaySound(lockedSound);
                    Debug.Log("Door is locked.");
                }
                else if (!isOpen)
                {
                    Open();
                }
            }
        }

        public void Open()
        {
            if (isOpen || isLocked) return;
            isOpen = true;
            
            if (blockingCollider != null) blockingCollider.enabled = false;
            
            if (openVFX != null) openVFX.Play();
            PlaySound(openSound);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateDoor(openPosition));
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            
            if (blockingCollider != null) blockingCollider.enabled = true;
            
            PlaySound(closeSound);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateDoor(closedPosition));
        }

        public void Lock()
        {
            isLocked = true;
            Close();
        }

        public void Unlock()
        {
            isLocked = false;
        }

        private IEnumerator AnimateDoor(Vector3 targetPosition)
        {
            Vector3 startPos = doorVisuals.localPosition;
            float time = 0;

            while (time < 1f)
            {
                time += Time.deltaTime / animationDuration;
                doorVisuals.localPosition = Vector3.Lerp(startPos, targetPosition, time);
                yield return null;
            }
            
            doorVisuals.localPosition = targetPosition;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
