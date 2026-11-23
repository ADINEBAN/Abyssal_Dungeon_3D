using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Subtle Perlin-noise flicker for wall torches and braziers (zero GC allocation).
    /// </summary>
    public class TorchFlickerBehaviour : MonoBehaviour
    {
        public Light TargetLight;
        public float BaseIntensity = 2.0f;
        public float FlickerAmplitude = 0.32f;
        public float FlickerSpeed = 6.5f;
        private float _seed;
        private Vector3 _baseLocalPos;

        private void Awake()
        {
            _seed = Random.Range(0f, 100f);
            _baseLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (TargetLight == null) return;
            float t = Time.time * FlickerSpeed;
            float n = Mathf.PerlinNoise(_seed, t) * 2f - 1f;
            TargetLight.intensity = BaseIntensity + n * FlickerAmplitude;
            transform.localPosition = _baseLocalPos + new Vector3(n * 0.025f, Mathf.Abs(n) * 0.03f, 0f);
        }
    }
}
