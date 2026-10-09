using UnityEngine;

namespace AbyssalDungeon.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private readonly int hashSpeed = Animator.StringToHash("Speed");
        private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
        private readonly int hashAttackIndex = Animator.StringToHash("AttackIndex");
        private readonly int hashIsAttacking = Animator.StringToHash("IsAttacking");
        private readonly int hashIsDodging = Animator.StringToHash("IsDodging");
        private readonly int hashIsDead = Animator.StringToHash("IsDead");
        private readonly int hashSkillIndex = Animator.StringToHash("SkillIndex");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public void SetSpeed(float speed)
        {
            if (animator != null) animator.SetFloat(hashSpeed, speed);
        }

        public void SetGrounded(bool isGrounded)
        {
            if (animator != null) animator.SetBool(hashIsGrounded, isGrounded);
        }

        public void SetAttacking(bool isAttacking)
        {
            if (animator != null) animator.SetBool(hashIsAttacking, isAttacking);
        }

        public void SetAttackIndex(int index)
        {
            if (animator != null)
            {
                animator.SetInteger(hashAttackIndex, index);
                animator.SetTrigger("Attack"); // Typically you'd use a trigger to force transition
            }
        }

        public void SetDodging(bool isDodging)
        {
            if (animator != null) animator.SetBool(hashIsDodging, isDodging);
        }

        public void SetDead(bool isDead)
        {
            if (animator != null) animator.SetBool(hashIsDead, isDead);
        }

        public void SetSkillIndex(int index)
        {
            if (animator != null)
            {
                animator.SetInteger(hashSkillIndex, index);
                animator.SetTrigger("UseSkill");
            }
        }

        public event System.Action OnAttackImpactEvent;

        // Animation Event Hook
        public void OnAttackImpact()
        {
            OnAttackImpactEvent?.Invoke();
        }
    }
}
