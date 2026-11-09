using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 12 (PHẦN 11 & `performance-optimization` skill):
    /// Lightweight Component Pool eliminating per-hit Instantiate/Destroy GC spikes
    /// for high-frequency combat VFX bursts and floating damage numbers.
    /// </summary>
    public static class GameObjectPool
    {
        private static readonly Stack<FloatingCombatText> _floatingTextPool = new Stack<FloatingCombatText>(48);
        private static readonly Stack<ExpandingBurstFx> _burstPool = new Stack<ExpandingBurstFx>(32);

        public static FloatingCombatText GetFloatingText(Vector3 pos)
        {
            while (_floatingTextPool.Count > 0)
            {
                var item = _floatingTextPool.Pop();
                if (item != null)
                {
                    item.transform.position = pos;
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            var go = new GameObject("PooledFloatingText");
            go.transform.position = pos;
            return go.AddComponent<FloatingCombatText>();
        }

        public static void ReleaseFloatingText(FloatingCombatText item)
        {
            if (item == null) return;
            if (_floatingTextPool.Count >= 64)
            {
                Object.Destroy(item.gameObject);
                return;
            }
            item.gameObject.SetActive(false);
            _floatingTextPool.Push(item);
        }

        public static ExpandingBurstFx GetBurst(Vector3 pos)
        {
            while (_burstPool.Count > 0)
            {
                var item = _burstPool.Pop();
                if (item != null)
                {
                    item.transform.position = pos;
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            var go = new GameObject("PooledVfxBurst");
            go.transform.position = pos;
            return go.AddComponent<ExpandingBurstFx>();
        }

        public static void ReleaseBurst(ExpandingBurstFx item)
        {
            if (item == null) return;
            if (_burstPool.Count >= 48)
            {
                Object.Destroy(item.gameObject);
                return;
            }
            item.gameObject.SetActive(false);
            _burstPool.Push(item);
        }
    }
}

