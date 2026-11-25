using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Optional component placed on secondary/detail child layers of a VFX prefab
    /// (e.g., extra embers, secondary smoke trails, ground rune rings, shock cones)
    /// so Low/Medium presets cleanly cull secondary layers while preserving core readability.
    /// </summary>
    public class VfxLayerTierFilter : MonoBehaviour
    {
        public VfxQualityOption minQualityTier = VfxQualityOption.Medium;
        public bool isSecondarySparkLayer = false;
        public bool isShockwaveRingLayer = false;

        public bool ShouldBeActive(VfxTierRules rules, bool forceTelegraph)
        {
            if (forceTelegraph) return true;
            if (rules.qualityLevel < minQualityTier) return false;
            if (isSecondarySparkLayer && !rules.enableSecondarySparks) return false;
            if (isShockwaveRingLayer && !rules.enableShockwaveRings) return false;
            return true;
        }
    }
}
