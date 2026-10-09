using System.Collections;
using UnityEngine;
using AbyssalDungeon.Combat;

namespace AbyssalDungeon.Dungeon
{
    public enum TrapType { Spike, Fire, Magic }

    [RequireComponent(typeof(Collider))]
    public class TrapSystem : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] private TrapType trapType;
        [SerializeField] private float damageAmount = 20f;
        [SerializeField] private float cooldownTime = 2.0f;
        
        [Header("References")]
        [SerializeField] private ParticleSystem trapVFX;
        [SerializeField] private Transform movingPart; // For spikes

        private bool isReady = true;
        private Collider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (isReady && other.CompareTag("Player"))
            {
                ActivateTrap(other.gameObject);
            }
        }

        private void ActivateTrap(GameObject target)
        {
            isReady = false;
            
            // Visuals
            if (trapVFX != null) trapVFX.Play();

            // Apply specific trap mechanics
            switch (trapType)
            {
                case TrapType.Spike:
                    if (movingPart != null) StartCoroutine(SpikeAnimation());
                    break;
                case TrapType.Fire:
                    Debug.Log("Fire burst!");
                    break;
                case TrapType.Magic:
                    Debug.Log("Arcane blast!");
                    break;
            }

            // Apply Damage
            var health = target.GetComponent<IDamageable>();
            if (health != null)
            {
                health.TakeDamage(new DamageInfo { attacker = gameObject, target = target.gameObject, baseDamage = damageAmount, finalDamage = damageAmount, damageType = DamageType.Physical });
            }
            else
            {
                Debug.LogWarning($"Target {target.name} does not have IDamageable!");
            }

            StartCoroutine(CooldownRoutine());
        }

        private IEnumerator SpikeAnimation()
        {
            Vector3 startPos = movingPart.localPosition;
            Vector3 endPos = startPos + Vector3.up * 1.5f;
            
            // Rise up
            float t = 0;
            while(t < 1)
            {
                t += Time.deltaTime * 5f;
                movingPart.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Retract
            t = 0;
            while(t < 1)
            {
                t += Time.deltaTime * 3f;
                movingPart.localPosition = Vector3.Lerp(endPos, startPos, t);
                yield return null;
            }
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownTime);
            isReady = true;
        }
    }
}

