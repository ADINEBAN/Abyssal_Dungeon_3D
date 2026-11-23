using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Ensures every Enemy 3D model (both Humanoid FBX and custom Golem/Treant rigs)
    /// exhibits distinct Idle, Walk/Run, Attack, Hit Reaction, and Death animations (Section 2.4.2).
    /// </summary>
    public class EnemyProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _customConstruct;
        [SerializeField] private Vector3 _baseLocalPos;
        [SerializeField] private Quaternion _baseLocalRot = Quaternion.identity;
        private Vector3 _lastPos;
        private float _phase;
        private float _attackPulse;
        private float _hitPulse;
        private bool _dead;
        private float _deathProgress;

        public void Initialize(Transform root, Animator anim, bool customConstruct)
        {
            _root = root;
            _animator = anim;
            _customConstruct = customConstruct;
            _baseLocalPos = root != null ? root.localPosition : Vector3.zero;
            _baseLocalRot = root != null ? root.localRotation : Quaternion.identity;
            _lastPos = root != null ? root.position : Vector3.zero;
            _phase = Random.Range(0f, 6.28f);
        }

        public void TriggerAttackPulse() => _attackPulse = 1f;
        public void TriggerHitReaction() => _hitPulse = 1f;
        public void TriggerDeathCollapse() => _dead = true;

        private void LateUpdate()
        {
            if (_root == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            if (_dead)
            {
                _deathProgress = Mathf.MoveTowards(_deathProgress, 1f, dt * 2.4f);
                if (_customConstruct || _animator == null)
                {
                    _root.localRotation = _baseLocalRot * Quaternion.Euler(-82f * _deathProgress, 0f, 0f);
                    _root.localPosition = _baseLocalPos + new Vector3(0f, -0.35f * _deathProgress, 0f);
                }
                return;
            }

            Vector3 delta = _root.position - _lastPos;
            delta.y = 0f;
            float planarSpeed = delta.magnitude / dt;
            _lastPos = _root.position;

            _phase += dt * (planarSpeed > 0.25f ? 9.5f : 2.4f);
            _attackPulse = Mathf.MoveTowards(_attackPulse, 0f, dt * 3.8f);
            _hitPulse = Mathf.MoveTowards(_hitPulse, 0f, dt * 5.2f);

            if (_customConstruct || _animator == null)
            {
                float bob = planarSpeed > 0.25f ? Mathf.Abs(Mathf.Sin(_phase)) * 0.09f : Mathf.Sin(_phase) * 0.03f;
                float pitch = _attackPulse * 24f - _hitPulse * 18f;
                float roll = planarSpeed > 0.25f ? Mathf.Sin(_phase) * 5.5f : 0f;
                _root.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
                _root.localRotation = _baseLocalRot * Quaternion.Euler(pitch, 0f, roll);
            }
            else if (_hitPulse > 0.01f)
            {
                _root.localRotation = _baseLocalRot * Quaternion.Euler(-_hitPulse * 10f, 0f, 0f);
            }
            else
            {
                _root.localRotation = _baseLocalRot;
            }
        }
    }
}
