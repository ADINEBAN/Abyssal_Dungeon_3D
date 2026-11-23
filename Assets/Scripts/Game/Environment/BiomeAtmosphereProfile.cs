using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// All 7 Dark Fantasy environments specified in Master Prompt Section 2.1.
    /// </summary>
    public enum DungeonBiome
    {
        ForgottenTemple = 0,
        AncientDungeon = 1,
        UndergroundFortress = 2,
        AbyssalCave = 3,
        CrystalChamber = 4,
        CursedForest = 5,
        BossArena = 6
    }

    [Serializable]
    public struct BiomeAtmosphereData
    {
        public DungeonBiome biome;
        public string vietnameseTitle;
        public string ambienceClipName;

        // Trilight Ambient & Fog
        public Color skyAmbient;
        public Color equatorAmbient;
        public Color groundAmbient;
        public Color fogColor;
        public float fogStart;
        public float fogEnd;
        public Color cameraBgColor;

        // Main Directional Rim / Shaft Light
        public Color sunColor;
        public float sunIntensity;
        public Vector3 sunEuler;

        // Torches, Mist & Floating Environmental Particles
        public Color torchLightColor;
        public Color torchCoreColor;
        public Color mistColor;
        public Color particleColor;
        public float particleRiseSpeed;
        public float particleSize;

        // Material Tints
        public Color wallStoneTint;
        public Color floorStoneTint;
        public bool preferMossyStone;
    }

    public static class BiomeAtmosphereLibrary
    {
        public static BiomeAtmosphereData Get(DungeonBiome biome)
        {
            return GetBuiltIn(biome);
        }

        public static DungeonBiome BiomeForDungeonRoom(int roomIndex, RoomType type)
        {
            if (type == RoomType.Boss) return DungeonBiome.BossArena;
            switch (roomIndex)
            {
                case 0: return DungeonBiome.ForgottenTemple;
                case 1: return DungeonBiome.AncientDungeon;
                case 2: return DungeonBiome.UndergroundFortress;
                case 3: return DungeonBiome.AbyssalCave;
                case 4: return DungeonBiome.CrystalChamber;
                case 5: return DungeonBiome.CursedForest;
                default: return DungeonBiome.BossArena;
            }
        }

        public static BiomeAtmosphereData GetBuiltIn(DungeonBiome biome)
        {
            switch (biome)
            {
                case DungeonBiome.ForgottenTemple: // Matches Reference Image 4 (warm sandstone + cool slate clerestory shafts)
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Đền Thờ Lãng Quên (Forgotten Temple)",
                        ambienceClipName = "amb_forgotten_temple",
                        skyAmbient = new Color(0.28f, 0.31f, 0.40f),
                        equatorAmbient = new Color(0.22f, 0.20f, 0.24f),
                        groundAmbient = new Color(0.11f, 0.09f, 0.08f),
                        fogColor = new Color(0.13f, 0.14f, 0.19f),
                        fogStart = 18f,
                        fogEnd = 66f,
                        cameraBgColor = new Color(0.08f, 0.09f, 0.13f),
                        sunColor = new Color(0.84f, 0.88f, 0.98f),
                        sunIntensity = 1.18f,
                        sunEuler = new Vector3(46f, -38f, 0f),
                        torchLightColor = new Color(1.00f, 0.62f, 0.22f),
                        torchCoreColor = new Color(1.00f, 0.88f, 0.52f),
                        mistColor = new Color(0.42f, 0.46f, 0.58f, 0.22f),
                        particleColor = new Color(0.95f, 0.86f, 0.68f, 0.45f),
                        particleRiseSpeed = 0.18f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.86f, 0.78f, 0.68f),
                        floorStoneTint = new Color(0.80f, 0.73f, 0.65f),
                        preferMossyStone = false
                    };

                case DungeonBiome.AncientDungeon:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Ngục Cổ Đá Đen (Ancient Dungeon)",
                        ambienceClipName = "amb_ancient_dungeon",
                        skyAmbient = new Color(0.20f, 0.22f, 0.30f),
                        equatorAmbient = new Color(0.16f, 0.16f, 0.21f),
                        groundAmbient = new Color(0.08f, 0.08f, 0.10f),
                        fogColor = new Color(0.08f, 0.09f, 0.14f),
                        fogStart = 16f,
                        fogEnd = 60f,
                        cameraBgColor = new Color(0.05f, 0.06f, 0.10f),
                        sunColor = new Color(0.72f, 0.78f, 0.95f),
                        sunIntensity = 0.95f,
                        sunEuler = new Vector3(50f, -32f, 0f),
                        torchLightColor = new Color(1.00f, 0.56f, 0.18f),
                        torchCoreColor = new Color(1.00f, 0.85f, 0.45f),
                        mistColor = new Color(0.32f, 0.36f, 0.48f, 0.25f),
                        particleColor = new Color(0.82f, 0.78f, 0.72f, 0.40f),
                        particleRiseSpeed = 0.14f,
                        particleSize = 0.08f,
                        wallStoneTint = new Color(0.74f, 0.73f, 0.78f),
                        floorStoneTint = new Color(0.68f, 0.68f, 0.74f),
                        preferMossyStone = false
                    };

                case DungeonBiome.UndergroundFortress:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Pháo Đài Ngầm Sắt Nung (Underground Fortress)",
                        ambienceClipName = "amb_underground_fortress",
                        skyAmbient = new Color(0.24f, 0.18f, 0.18f),
                        equatorAmbient = new Color(0.19f, 0.14f, 0.14f),
                        groundAmbient = new Color(0.10f, 0.06f, 0.05f),
                        fogColor = new Color(0.13f, 0.08f, 0.08f),
                        fogStart = 16f,
                        fogEnd = 58f,
                        cameraBgColor = new Color(0.08f, 0.04f, 0.05f),
                        sunColor = new Color(0.95f, 0.76f, 0.62f),
                        sunIntensity = 1.02f,
                        sunEuler = new Vector3(44f, -28f, 0f),
                        torchLightColor = new Color(1.00f, 0.44f, 0.12f),
                        torchCoreColor = new Color(1.00f, 0.82f, 0.38f),
                        mistColor = new Color(0.45f, 0.28f, 0.22f, 0.22f),
                        particleColor = new Color(1.00f, 0.52f, 0.16f, 0.68f),
                        particleRiseSpeed = 0.62f,
                        particleSize = 0.075f,
                        wallStoneTint = new Color(0.66f, 0.62f, 0.64f),
                        floorStoneTint = new Color(0.60f, 0.58f, 0.60f),
                        preferMossyStone = false
                    };

                case DungeonBiome.AbyssalCave:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Hang Động Vực Thẳm Cổ Ngữ (Abyssal Cave)",
                        ambienceClipName = "amb_abyssal_cave",
                        skyAmbient = new Color(0.20f, 0.16f, 0.32f),
                        equatorAmbient = new Color(0.15f, 0.12f, 0.24f),
                        groundAmbient = new Color(0.07f, 0.05f, 0.12f),
                        fogColor = new Color(0.09f, 0.06f, 0.17f),
                        fogStart = 15f,
                        fogEnd = 56f,
                        cameraBgColor = new Color(0.05f, 0.03f, 0.11f),
                        sunColor = new Color(0.72f, 0.62f, 1.00f),
                        sunIntensity = 0.92f,
                        sunEuler = new Vector3(48f, -42f, 0f),
                        torchLightColor = new Color(0.62f, 0.36f, 1.00f),
                        torchCoreColor = new Color(0.86f, 0.72f, 1.00f),
                        mistColor = new Color(0.38f, 0.24f, 0.62f, 0.28f),
                        particleColor = new Color(0.68f, 0.45f, 1.00f, 0.60f),
                        particleRiseSpeed = 0.28f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.64f, 0.60f, 0.76f),
                        floorStoneTint = new Color(0.58f, 0.55f, 0.70f),
                        preferMossyStone = false
                    };

                case DungeonBiome.CrystalChamber:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Điện Tinh Thể Aether (Crystal Chamber)",
                        ambienceClipName = "amb_crystal_chamber",
                        skyAmbient = new Color(0.18f, 0.28f, 0.38f),
                        equatorAmbient = new Color(0.14f, 0.22f, 0.30f),
                        groundAmbient = new Color(0.06f, 0.10f, 0.15f),
                        fogColor = new Color(0.07f, 0.14f, 0.22f),
                        fogStart = 18f,
                        fogEnd = 64f,
                        cameraBgColor = new Color(0.04f, 0.09f, 0.15f),
                        sunColor = new Color(0.66f, 0.90f, 1.00f),
                        sunIntensity = 1.08f,
                        sunEuler = new Vector3(52f, -35f, 0f),
                        torchLightColor = new Color(0.24f, 0.82f, 1.00f),
                        torchCoreColor = new Color(0.75f, 0.98f, 1.00f),
                        mistColor = new Color(0.24f, 0.58f, 0.78f, 0.26f),
                        particleColor = new Color(0.38f, 0.92f, 1.00f, 0.65f),
                        particleRiseSpeed = 0.34f,
                        particleSize = 0.085f,
                        wallStoneTint = new Color(0.68f, 0.78f, 0.86f),
                        floorStoneTint = new Color(0.62f, 0.72f, 0.82f),
                        preferMossyStone = false
                    };

                case DungeonBiome.CursedForest:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Thánh Điện Rừng Nguyền Rủa (Cursed Forest)",
                        ambienceClipName = "amb_cursed_forest",
                        skyAmbient = new Color(0.16f, 0.26f, 0.20f),
                        equatorAmbient = new Color(0.12f, 0.20f, 0.16f),
                        groundAmbient = new Color(0.06f, 0.09f, 0.06f),
                        fogColor = new Color(0.07f, 0.13f, 0.10f),
                        fogStart = 15f,
                        fogEnd = 56f,
                        cameraBgColor = new Color(0.04f, 0.08f, 0.06f),
                        sunColor = new Color(0.70f, 0.92f, 0.78f),
                        sunIntensity = 0.96f,
                        sunEuler = new Vector3(46f, -30f, 0f),
                        torchLightColor = new Color(0.38f, 0.95f, 0.52f),
                        torchCoreColor = new Color(0.82f, 1.00f, 0.80f),
                        mistColor = new Color(0.24f, 0.48f, 0.34f, 0.28f),
                        particleColor = new Color(0.48f, 0.98f, 0.55f, 0.58f),
                        particleRiseSpeed = 0.22f,
                        particleSize = 0.095f,
                        wallStoneTint = new Color(0.74f, 0.82f, 0.72f),
                        floorStoneTint = new Color(0.68f, 0.76f, 0.66f),
                        preferMossyStone = true
                    };

                default: // BossArena
                    return new BiomeAtmosphereData
                    {
                        biome = DungeonBiome.BossArena,
                        vietnameseTitle = "Đại Thánh Đường Thẳm Sâu (Boss Arena)",
                        ambienceClipName = "amb_boss_arena",
                        skyAmbient = new Color(0.24f, 0.15f, 0.28f),
                        equatorAmbient = new Color(0.18f, 0.11f, 0.22f),
                        groundAmbient = new Color(0.09f, 0.05f, 0.10f),
                        fogColor = new Color(0.10f, 0.05f, 0.14f),
                        fogStart = 20f,
                        fogEnd = 74f,
                        cameraBgColor = new Color(0.06f, 0.03f, 0.09f),
                        sunColor = new Color(0.88f, 0.72f, 0.96f),
                        sunIntensity = 1.12f,
                        sunEuler = new Vector3(44f, -36f, 0f),
                        torchLightColor = new Color(0.86f, 0.28f, 1.00f),
                        torchCoreColor = new Color(1.00f, 0.76f, 1.00f),
                        mistColor = new Color(0.46f, 0.18f, 0.58f, 0.28f),
                        particleColor = new Color(0.95f, 0.36f, 0.82f, 0.65f),
                        particleRiseSpeed = 0.48f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.72f, 0.66f, 0.78f),
                        floorStoneTint = new Color(0.66f, 0.60f, 0.74f),
                        preferMossyStone = false
                    };
            }
        }
    }
}
