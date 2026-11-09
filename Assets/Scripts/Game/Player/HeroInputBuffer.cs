using UnityEngine;

namespace AbyssGame
{
    public enum BufferedActionType
    {
        None = 0,
        Dodge,
        Jump,
        BasicAttack,
        HeavyAttack,
        Skill0,
        Skill1,
        Skill2,
        Skill3
    }

    /// <summary>
    /// Zero-allocation 180ms Action Input Buffer (input-systems skill):
    /// Captures combat/evasion inputs pressed during the final recovery frames of another action
    /// and executes them on the exact first frame the Hero becomes actionable.
    /// </summary>
    public struct HeroInputBuffer
    {
        public const float DefaultBufferWindow = 0.18f;

        private BufferedActionType _queuedType;
        private float _expireTime;
        private Vector3 _queuedDir;
        private Vector3 _queuedAimPoint;

        public bool HasBufferedAction => _queuedType != BufferedActionType.None && Time.unscaledTime <= _expireTime;

        public void Enqueue(BufferedActionType type, Vector3 dir, Vector3 aimPoint, float window = DefaultBufferWindow)
        {
            // Higher-priority defensive inputs (Dodge) always override buffered attacks
            if (_queuedType == BufferedActionType.Dodge && Time.unscaledTime <= _expireTime && type != BufferedActionType.Dodge)
                return;

            _queuedType = type;
            _queuedDir = dir;
            _queuedAimPoint = aimPoint;
            _expireTime = Time.unscaledTime + window;
        }

        public bool TryConsume(out BufferedActionType type, out Vector3 dir, out Vector3 aimPoint)
        {
            if (_queuedType == BufferedActionType.None || Time.unscaledTime > _expireTime)
            {
                _queuedType = BufferedActionType.None;
                type = BufferedActionType.None;
                dir = Vector3.zero;
                aimPoint = Vector3.zero;
                return false;
            }

            type = _queuedType;
            dir = _queuedDir;
            aimPoint = _queuedAimPoint;
            _queuedType = BufferedActionType.None;
            return true;
        }

        public void Clear()
        {
            _queuedType = BufferedActionType.None;
            _expireTime = 0f;
        }
    }
}

