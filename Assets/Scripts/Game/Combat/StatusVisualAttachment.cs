using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Real-time 3D Status & Shield Visual Auras attached to ActorUnit:
    /// Shows readable 3D cues for Stagger/Stun stars and Burn embers.
    /// </summary>
    public class StatusVisualAttachment : MonoBehaviour
    {
        [SerializeField] private ActorUnit _unit;
        private GameObject _shieldDome;
        private GameObject _staggerStars;
        private GameObject _freezeCrystal;
        private GameObject _burnRing;
        private GameObject _poisonRing;
        private GameObject _curseHex;

        public static StatusVisualAttachment AttachTo(ActorUnit unit)
        {
            if (unit == null) return null;
            var comp = unit.GetComponent<StatusVisualAttachment>();
            if (comp == null) comp = unit.gameObject.AddComponent<StatusVisualAttachment>();
            comp._unit = unit;
            return comp;
        }

        private void LateUpdate()
        {
            if (_unit == null) _unit = GetComponent<ActorUnit>();
            if (_unit == null || _unit.IsDead)
            {
                SetAllInactive();
                return;
            }

            float t = Time.time;

            if (_shieldDome != null) _shieldDome.SetActive(false);

            bool dazed = _unit.IsStaggered || _unit.HasStatus(StatusType.Stun);
            if (dazed && _staggerStars == null)
            {
                _staggerStars = new GameObject("VfxStaggerStars");
                _staggerStars.transform.SetParent(transform, false);
                float h = _unit.Controller != null ? _unit.Controller.height + 0.35f : 2.25f;
                _staggerStars.transform.localPosition = new Vector3(0f, h, 0f);
                var starMat = CharacterVisualFactory.MakeMat(new Color(1f, 0.88f, 0.22f), 0.9f, 0.2f, new Color(1f, 0.82f, 0.2f) * 2.6f);
                for (int i = 0; i < 3; i++)
                {
                    float ang = i * 120f * Mathf.Deg2Rad;
                    CharacterVisualFactory.Prim(
                        PrimitiveType.Cube, "Star" + i, _staggerStars.transform,
                        new Vector3(Mathf.Cos(ang) * 0.42f, 0f, Mathf.Sin(ang) * 0.42f),
                        Vector3.one * 0.13f, starMat, Quaternion.Euler(45f, 45f, 0f));
                }
            }
            if (_staggerStars != null)
            {
                _staggerStars.SetActive(dazed);
                if (dazed) _staggerStars.transform.Rotate(0f, 220f * Time.deltaTime, 0f);
            }

            if (_freezeCrystal != null) _freezeCrystal.SetActive(false);

            bool burning = _unit.HasStatus(StatusType.Burn);
            if (burning && _burnRing == null)
            {
                _burnRing = new GameObject("VfxBurnEmbers");
                _burnRing.transform.SetParent(transform, false);
                _burnRing.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var fireMat = CharacterVisualFactory.MakeMat(new Color(1f, 0.48f, 0.1f), 0.9f, 0f, new Color(1f, 0.42f, 0.08f) * 3f);
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * 90f * Mathf.Deg2Rad;
                    CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Ember" + i, _burnRing.transform,
                        new Vector3(Mathf.Cos(ang) * 0.52f, (i % 2) * 0.22f, Mathf.Sin(ang) * 0.52f),
                        Vector3.one * 0.12f, fireMat);
                }
            }
            if (_burnRing != null)
            {
                _burnRing.SetActive(burning);
                if (burning)
                {
                    _burnRing.transform.Rotate(0f, -180f * Time.deltaTime, 0f);
                    _burnRing.transform.localPosition = new Vector3(0f, 0.95f + Mathf.Sin(t * 6f) * 0.08f, 0f);
                }
            }

            if (_poisonRing != null) _poisonRing.SetActive(false);
            if (_curseHex != null) _curseHex.SetActive(false);
        }

        private void SetAllInactive()
        {
            if (_shieldDome != null) _shieldDome.SetActive(false);
            if (_staggerStars != null) _staggerStars.SetActive(false);
            if (_freezeCrystal != null) _freezeCrystal.SetActive(false);
            if (_burnRing != null) _burnRing.SetActive(false);
            if (_poisonRing != null) _poisonRing.SetActive(false);
            if (_curseHex != null) _curseHex.SetActive(false);
        }
    }
}
