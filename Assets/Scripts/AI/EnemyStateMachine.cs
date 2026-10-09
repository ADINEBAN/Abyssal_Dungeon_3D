using System;
using UnityEngine;

namespace AbyssalDungeon.AI
{
    public enum EnemyStateType
    {
        Idle,
        Patrol,
        Detect,
        Chase,
        Attack,
        Hit,
        Stunned,
        Retreat,
        Dead
    }

    [RequireComponent(typeof(AbyssalDungeon.Enemy.EnemyController))]
    public class EnemyStateMachine : MonoBehaviour
    {
        public EnemyStateType CurrentStateType { get; private set; }
        public EnemyState CurrentState { get; private set; }

        public event Action<EnemyStateType> OnStateChanged;

        private AbyssalDungeon.Enemy.EnemyController _enemyController;

        private void Awake()
        {
            _enemyController = GetComponent<AbyssalDungeon.Enemy.EnemyController>();
        }

        private void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Execute();
            }
        }

        public void ChangeState(EnemyStateType newStateType, EnemyState newState)
        {
            if (CurrentState != null)
            {
                CurrentState.Exit();
            }

            CurrentStateType = newStateType;
            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(newStateType);
        }
    }
}
