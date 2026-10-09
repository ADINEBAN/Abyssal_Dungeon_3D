using UnityEngine;
using AbyssalDungeon.Combat;

namespace AbyssalDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private DodgeSystem dodgeSystem;
        [SerializeField] private PlayerAnimationController animationController;

        private CharacterController controller;
        private Vector3 velocity;
        private Vector3 currentMoveVelocity;
        private bool isGrounded;
        private bool isAttacking;
        private bool isDodging;
        private bool isDead;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
            if (dodgeSystem == null) dodgeSystem = GetComponent<DodgeSystem>();
            if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        }

        private void Start()
        {
            if (playerStats != null)
            {
                playerStats.OnDeath += HandleDeath;
            }
            if (playerCombat != null)
            {
                playerCombat.OnAttack += () => { isAttacking = true; if(animationController) animationController.SetAttacking(true); };
                playerCombat.OnAttackEnd += () => { isAttacking = false; if(animationController) animationController.SetAttacking(false); };
            }
            if (dodgeSystem != null)
            {
                dodgeSystem.OnDodgeStart += () => { isDodging = true; if(animationController) animationController.SetDodging(true); };
                dodgeSystem.OnDodgeEnd += () => { isDodging = false; if(animationController) animationController.SetDodging(false); };
            }
        }

        private void Update()
        {
            if (isDead) return;

            CheckGrounded();
            HandleInput();
            HandleMovement();
            HandleGravity();

            UpdateAnimations();
        }

        private void CheckGrounded()
        {
            if (groundCheck != null)
            {
                isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }
            else
            {
                isGrounded = controller.isGrounded;
            }

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        private void HandleInput()
        {
            if (isAttacking || isDodging) return;

            if (Input.GetMouseButtonDown(0) && playerCombat != null)
            {
                playerCombat.BasicAttack();
            }
            if (Input.GetKeyDown(KeyCode.Q) && playerCombat != null)
            {
                playerCombat.UseSkill(0);
            }
            if (Input.GetKeyDown(KeyCode.E) && playerCombat != null)
            {
                playerCombat.UseSkill(1);
            }
            if (Input.GetKeyDown(KeyCode.R) && playerCombat != null)
            {
                playerCombat.UseSkill(2);
            }
            if (Input.GetKeyDown(KeyCode.Space) && dodgeSystem != null)
            {
                Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
                dodgeSystem.TryDodge(moveInput, cameraTransform);
            }
        }

        private void HandleMovement()
        {
            if (isAttacking || isDodging)
            {
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, Time.deltaTime * deceleration);
                controller.Move(currentMoveVelocity * Time.deltaTime);
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, moveDir.normalized * moveSpeed, Time.deltaTime * acceleration);
            }
            else
            {
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, Time.deltaTime * deceleration);
            }

            controller.Move(currentMoveVelocity * Time.deltaTime);
        }

        private void HandleGravity()
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateAnimations()
        {
            if (animationController != null)
            {
                float currentSpeed = new Vector3(currentMoveVelocity.x, 0, currentMoveVelocity.z).magnitude;
                animationController.SetSpeed(currentSpeed);
                animationController.SetGrounded(isGrounded);
            }
        }

        private void HandleDeath()
        {
            isDead = true;
            if (animationController != null)
            {
                animationController.SetDead(true);
            }
            Debug.Log("Player Died");
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnDeath -= HandleDeath;
            }
        }
    }
}
