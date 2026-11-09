using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Designer-editable ScriptableObject wrapper for BiomeAtmosphereData (stored in Assets/ScriptableObjects/Biomes/).
    /// Dedicated file matching class name so Unity serializes m_Script GUID without Missing Script warnings.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeAtmosphere", menuName = "AbyssalDungeon/Biome Atmosphere Profile")]
    public class BiomeAtmosphereSO : ScriptableObject
    {
        public BiomeAtmosphereData Data;
    }
}

