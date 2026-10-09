using UnityEngine;
using UnityEngine.AI;

namespace AbyssalDungeon.Multiplayer
{
    public enum AIState
    {
        Follow,
        Combat,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class AIPartyMember : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;

        private NavMeshAgent agent;
        private Transform localPlayer;
        private AIState currentState = AIState.Follow;
        private float lastAttackTime;
        private int classIndex;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            FindLocalPlayer();
        }

        public void Initialize(int classIdx)
        {
            classIndex = classIdx;
        }

        private void FindLocalPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                localPlayer = player.transform;
            }
        }

        private void Update()
        {
            if (currentState == AIState.Dead) return;

            switch (currentState)
            {
                case AIState.Follow:
                    UpdateFollowState();
                    break;
                case AIState.Combat:
                    UpdateCombatState();
                    break;
            }
        }

        private void UpdateFollowState()
        {
            if (localPlayer == null)
            {
                FindLocalPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, localPlayer.position);
            
            if (distToPlayer > followDistance)
            {
                agent.SetDestination(localPlayer.position);
            }

            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null && Vector3.Distance(transform.position, nearestEnemy.position) < 10f)
            {
                currentState = AIState.Combat;
            }
        }

        private void UpdateCombatState()
        {
            Transform nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy == null)
            {
                currentState = AIState.Follow;
                return;
            }

            float distToEnemy = Vector3.Distance(transform.position, nearestEnemy.position);

            if (distToEnemy > attackRange)
            {
                agent.SetDestination(nearestEnemy.position);
            }
            else
            {
                agent.ResetPath();
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    ExecuteAttack(nearestEnemy);
                }
            }
        }

        private void ExecuteAttack(Transform target)
        {
            lastAttackTime = Time.time;
            
            switch (classIndex)
            {
                case 0:
                    Debug.Log($"{gameObject.name} (Tank) attacks {target.name} and taunts!");
                    break;
                case 1:
                    Debug.Log($"{gameObject.name} (DPS) attacks {target.name}!");
                    break;
                case 2:
                    Debug.Log($"{gameObject.name} (Healer) attacks {target.name} or heals an ally!");
                    break;
                default:
                    Debug.Log($"{gameObject.name} attacks {target.name}!");
                    break;
            }
        }

        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform nearest = null;
            float minDist = Mathf.Infinity;

            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy.transform;
                }
            }

            return nearest;
        }

        public void Die()
        {
            currentState = AIState.Dead;
            agent.isStopped = true;
            Debug.Log($"{gameObject.name} has died.");
        }

        public void Respawn(Vector3 position)
        {
            transform.position = position;
            currentState = AIState.Follow;
            agent.isStopped = false;
            Debug.Log($"{gameObject.name} has respawned.");
        }
    }
}
