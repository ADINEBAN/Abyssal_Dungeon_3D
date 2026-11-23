using System;
using UnityEngine;

namespace AbyssGame
{
    public class ModalAutoRebuildBehaviour : MonoBehaviour
    {
        public Action OnRebuild;
        private void OnEnable()
        {
            OnRebuild?.Invoke();
        }
    }
}
