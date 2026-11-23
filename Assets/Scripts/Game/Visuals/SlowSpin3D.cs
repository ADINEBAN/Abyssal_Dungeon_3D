using UnityEngine;

namespace AbyssGame
{
    public class SlowSpin3D : MonoBehaviour
    {
        public float SpeedY = 55f;
        private void Update() => transform.Rotate(0f, SpeedY * Time.deltaTime, 0f, Space.Self);
    }
}
