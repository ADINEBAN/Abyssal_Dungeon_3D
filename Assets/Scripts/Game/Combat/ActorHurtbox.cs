using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Dedicated 3D Hurtbox Volume attached to every ActorUnit (Ignore Raycast layer 2, Trigger CapsuleCollider).
    /// Enables true 3D spatial weapon-hurtbox sweeps and exact surface contact points for hit sparks without
    /// obstructing camera collision or ground aim raycasts.
    /// </summary>
    public class ActorHurtbox : MonoBehaviour
    {
        [SerializeField] private ActorUnit _owner;
        [SerializeField] private CapsuleCollider _capsule;

        public ActorUnit Owner
        {
            get => _owner != null ? _owner : (_owner = GetComponentInParent<ActorUnit>());
            private set => _owner = value;
        }

        public CapsuleCollider Capsule
        {
            get => _capsule != null ? _capsule : (_capsule = GetComponent<CapsuleCollider>());
            private set => _capsule = value;
        }

        public static ActorHurtbox AttachTo(ActorUnit unit, float radius, float height)
        {
            if (unit == null) return null;
            Transform existing = unit.transform.Find("HurtboxVolume");
            GameObject go = existing != null ? existing.gameObject : new GameObject("HurtboxVolume");
            go.transform.SetParent(unit.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.layer = 2; // Ignore Raycast so camera/aim rays pass cleanly

            var hb = go.GetComponent<ActorHurtbox>();
            if (hb == null) hb = go.AddComponent<ActorHurtbox>();
            hb.Owner = unit;

            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.radius = Mathf.Max(0.32f, radius * 1.12f);
            cap.height = Mathf.Max(1.2f, height * 1.04f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
            hb.Capsule = cap;
            return hb;
        }

        public Vector3 ClosestImpactPoint(Vector3 fromWorld)
        {
            if (Capsule != null && Capsule.enabled && gameObject.activeInHierarchy)
            {
                return Capsule.ClosestPoint(fromWorld);
            }
            return transform.position + Vector3.up * 1.05f;
        }
    }
}
