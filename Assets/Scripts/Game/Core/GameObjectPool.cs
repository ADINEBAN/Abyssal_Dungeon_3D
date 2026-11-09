using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 3 & Phase 12 (`performance-optimization` skill Pattern 3):
    /// Zero-GC Component Pool eliminating per-hit Instantiate/Destroy and Mesh/Material allocation spikes
    /// for high-frequency combat VFX (SlashArcRibbonFx, DirectionalSparkBurstFx, ExpandingBurstFx)
    /// and FloatingCombatText.
    /// </summary>
    public static class GameObjectPool
    {
        private static readonly Stack<FloatingCombatText> _floatingTextPool = new Stack<FloatingCombatText>(48);
        private static readonly Stack<ExpandingBurstFx> _burstPool = new Stack<ExpandingBurstFx>(32);
        private static readonly Stack<SlashArcRibbonFx> _slashArcPool = new Stack<SlashArcRibbonFx>(24);
        private static readonly Stack<DirectionalSparkBurstFx> _sparkBurstPool = new Stack<DirectionalSparkBurstFx>(24);

        public static int TotalPooledInstances =>
            _floatingTextPool.Count + _burstPool.Count + _slashArcPool.Count + _sparkBurstPool.Count;

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

        public static SlashArcRibbonFx GetSlashArc(Vector3 pos, Quaternion rot)
        {
            while (_slashArcPool.Count > 0)
            {
                var item = _slashArcPool.Pop();
                if (item != null)
                {
                    item.transform.SetPositionAndRotation(pos, rot);
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            var go = new GameObject("PooledVfxSlashArc");
            go.transform.SetPositionAndRotation(pos, rot);
            return go.AddComponent<SlashArcRibbonFx>();
        }

        public static void ReleaseSlashArc(SlashArcRibbonFx item)
        {
            if (item == null) return;
            if (_slashArcPool.Count >= 32)
            {
                Object.Destroy(item.gameObject);
                return;
            }
            item.gameObject.SetActive(false);
            _slashArcPool.Push(item);
        }

        public static DirectionalSparkBurstFx GetSparkBurst(Vector3 pos, Quaternion rot)
        {
            while (_sparkBurstPool.Count > 0)
            {
                var item = _sparkBurstPool.Pop();
                if (item != null)
                {
                    item.transform.SetPositionAndRotation(pos, rot);
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            var go = new GameObject("PooledVfxHitSparks");
            go.transform.SetPositionAndRotation(pos, rot);
            return go.AddComponent<DirectionalSparkBurstFx>();
        }

        public static void ReleaseSparkBurst(DirectionalSparkBurstFx item)
        {
            if (item == null) return;
            if (_sparkBurstPool.Count >= 32)
            {
                Object.Destroy(item.gameObject);
                return;
            }
            item.gameObject.SetActive(false);
            _sparkBurstPool.Push(item);
        }
    }
}
