using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.VFX
{
    public enum VFXType
    {
        Fire, Magic, Hit, CriticalHit, Heal, Shield, Poison, Burn, Freeze, Death, BossAura, DungeonFog, Portal, DodgeTrail, LevelUp, ChestOpen, TrapSpike, TrapFire, TrapMagic
    }

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private Dictionary<VFXType, GameObject> vfxPrefabs = new Dictionary<VFXType, GameObject>();
        private Dictionary<VFXType, Queue<GameObject>> pool = new Dictionary<VFXType, Queue<GameObject>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePools()
        {
            foreach (VFXType type in System.Enum.GetValues(typeof(VFXType)))
            {
                pool[type] = new Queue<GameObject>();
            }
        }

        public GameObject SpawnVFX(VFXType type, Vector3 position, float duration)
        {
            GameObject vfxObj = null;

            if (pool.ContainsKey(type) && pool[type].Count > 0)
            {
                vfxObj = pool[type].Dequeue();
                vfxObj.SetActive(true);
                vfxObj.transform.position = position;
            }
            else
            {
                if (!vfxPrefabs.ContainsKey(type) || vfxPrefabs[type] == null)
                {
                    vfxPrefabs[type] = ProceduralVFXFactory.CreateVFXPrefab(type);
                }
                
                if (vfxPrefabs[type] != null)
                {
                    vfxObj = Instantiate(vfxPrefabs[type], position, Quaternion.identity);
                }
            }

            if (vfxObj != null)
            {
                StartCoroutine(ReturnToPoolAfterDuration(type, vfxObj, duration));
                ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }

            return vfxObj;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDuration(VFXType type, GameObject vfxObj, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (vfxObj != null)
            {
                vfxObj.SetActive(false);
                pool[type].Enqueue(vfxObj);
            }
        }
    }
}
