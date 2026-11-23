using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Animates floating class signature relics (Mage Astrolabe Ring, Warlock Orbiting Grimoire, Monk Spirit Beads).
    /// </summary>
    public class ClassRelicFloatAnimator : MonoBehaviour
    {
        public bool OrbitMode;
        public bool SpinZ;
        private Vector3 _baseLocalPos;
        private float _phase;

        private void Start()
        {
            _baseLocalPos = transform.localPosition;
            _phase = Random.Range(0f, 6.28f);
        }

        private void Update()
        {
            float t = Time.time + _phase;
            if (OrbitMode)
            {
                transform.localPosition = _baseLocalPos + new Vector3(Mathf.Cos(t * 1.4f) * 0.08f, Mathf.Sin(t * 2.4f) * 0.09f, Mathf.Sin(t * 1.4f) * 0.08f);
                transform.localRotation = Quaternion.Euler(14f + Mathf.Sin(t * 2f) * 6f, -25f + Mathf.Cos(t * 1.3f) * 14f, 0f);
            }
            else if (SpinZ)
            {
                transform.localPosition = _baseLocalPos + new Vector3(0f, Mathf.Sin(t * 2.0f) * 0.05f, 0f);
                transform.Rotate(0f, 0f, 26f * Time.deltaTime, Space.Self);
            }
            else
            {
                transform.localPosition = _baseLocalPos + new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.055f, 0f);
            }
        }
    }
}
