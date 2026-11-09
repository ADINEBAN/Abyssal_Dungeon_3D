using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Subtle Perlin-noise flicker for wall torches and braziers (zero GC allocation).
    /// </summary>
    public class TorchFlickerBehaviour : MonoBehaviour
    {
        public Light TargetLight;
        public float BaseIntensity = 2.0f;
        public float FlickerAmplitude = 0.32f;
        public float FlickerSpeed = 6.5f;
        private float _seed;
        private Vector3 _baseLocalPos;

        private void Awake()
        {
            _seed = Random.Range(0f, 100f);
            _baseLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (TargetLight == null) return;
            float t = Time.time * FlickerSpeed;
            float n = Mathf.PerlinNoise(_seed, t) * 2f - 1f;
            TargetLight.intensity = BaseIntensity + n * FlickerAmplitude;
            transform.localPosition = _baseLocalPos + new Vector3(n * 0.025f, Mathf.Abs(n) * 0.03f, 0f);
        }
    }

    /// <summary>
    /// Modular 3D Environment Builder assembling our 23 authored Dark Fantasy meshes, PBR materials,
    /// custom flame/crystal/mist shaders, decals, and biome-specific architectural storytelling.
    /// </summary>
    public static class ModularDungeonArchitect
    {
        private static Mesh _crossedFlameMesh;

        public static void BuildModularRoomShell(
            Transform root,
            Vector3 center,
            float width,
            float length,
            float wallHeight,
            DungeonBiome biome,
            bool openSouthDoor,
            bool openNorthDoor,
            float doorwayWidth = 6f)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material wallMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.wallStoneTint)
                : PbrMaterialLibrary.GothicStone(bData.wallStoneTint);
            Material floorMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.floorStoneTint)
                : PbrMaterialLibrary.CathedralFloor(bData.floorStoneTint);
            Material pillarMat = PbrMaterialLibrary.GothicStone(bData.wallStoneTint * 0.96f);

            // 1. Smooth physics floor collider (y = 0 top surface) so CharacterControllers never snag on bevel seams
            var floorColGo = new GameObject("FloorPhysicsCollider");
            floorColGo.transform.SetParent(root, false);
            floorColGo.transform.position = new Vector3(center.x, -0.25f, center.z);
            var fc = floorColGo.AddComponent<BoxCollider>();
            fc.size = new Vector3(width + 1.2f, 0.5f, length + 1.2f);

            // 2. Modular 4m x 4m recessed-channel stone floor tiles
            var floorGroup = new GameObject("ModularFloorTiles").transform;
            floorGroup.SetParent(root, false);
            int nx = Mathf.Max(1, Mathf.RoundToInt(width / 4f));
            int nz = Mathf.Max(1, Mathf.RoundToInt(length / 4f));
            float tileScaleX = (width / nx) / 4f;
            float tileScaleZ = (length / nz) / 4f;
            float startX = center.x - width * 0.5f + (width / nx) * 0.5f;
            float startZ = center.z - length * 0.5f + (length / nz) * 0.5f;

            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    Vector3 tPos = new Vector3(startX + ix * (width / nx), 0f, startZ + iz * (length / nz));
                    ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", floorGroup,
                        tPos, Quaternion.identity, new Vector3(tileScaleX, 1f, tileScaleZ),
                        floorMat, addBoxCollider: false, castShadows: false);
                }
            }

            // 3. Perimeter Physics Walls + Modular 4m Gothic Blind-Arch Wall Panels
            var wallGroup = new GameObject("ModularPerimeterWalls").transform;
            wallGroup.SetParent(root, false);
            float wallScaleY = wallHeight / 5.2f;
            float southZ = center.z - length * 0.5f;
            float northZ = center.z + length * 0.5f;
            float westX = center.x - width * 0.5f;
            float eastX = center.x + width * 0.5f;

            // West (-X, faces +X) & East (+X, faces -X)
            AddWallCollider(wallGroup, new Vector3(westX - 0.3f, wallHeight * 0.5f, center.z), new Vector3(0.6f, wallHeight + 1f, length));
            AddWallCollider(wallGroup, new Vector3(eastX + 0.3f, wallHeight * 0.5f, center.z), new Vector3(0.6f, wallHeight + 1f, length));

            for (int iz = 0; iz < nz; iz++)
            {
                float z = startZ + iz * (length / nz);
                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", wallGroup,
                    new Vector3(westX, 0f, z), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(tileScaleZ, wallScaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", wallGroup,
                    new Vector3(eastX, 0f, z), Quaternion.Euler(0f, -90f, 0f),
                    new Vector3(tileScaleZ, wallScaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);
            }

            // South Wall (-Z, faces +Z)
            BuildEndWallWithOptionalGate(wallGroup, center.x, southZ, width, wallHeight, doorwayWidth, openSouthDoor, 0f, wallMat, pillarMat);
            // North Wall (+Z, faces -Z)
            BuildEndWallWithOptionalGate(wallGroup, center.x, northZ, width, wallHeight, doorwayWidth, openNorthDoor, 180f, wallMat, pillarMat);

            // 4. Interior Gothic Pillar Colonnade + Overhead Vault Arches + Wall Sconce Torches
            var colonnade = new GameObject("GothicColonnade").transform;
            colonnade.SetParent(root, false);
            float aisleX = Mathf.Max(4.2f, width * 0.5f - 2.4f);
            int bays = Mathf.Clamp(Mathf.RoundToInt(length / 7.5f), 2, 5);
            float pScaleY = wallHeight / 5.1f;

            for (int b = 0; b <= bays; b++)
            {
                float t = (float)b / bays;
                float pz = Mathf.Lerp(southZ + 2.4f, northZ - 2.4f, t);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pPos = new Vector3(center.x + side * aisleX, 0f, pz);
                    ObjMeshLoader.SpawnPiece("GothicPillar", colonnade,
                        pPos, Quaternion.identity, new Vector3(1f, pScaleY, 1f),
                        pillarMat, addBoxCollider: true, castShadows: true);

                    // Mount wrought-iron wall torches on alternating pillars (budget-conscious point light count!)
                    bool mountLight = (b == 0 || b == bays || (b == bays / 2 && bays >= 3));
                    if (mountLight || b % 2 == 1)
                    {
                        Quaternion tRot = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);
                        Vector3 tPos = pPos + new Vector3(side < 0 ? 0.48f : -0.48f, 2.55f, 0f);
                        SpawnWallTorch(colonnade, tPos, tRot, bData.torchLightColor, bData.torchCoreColor, withPointLight: mountLight);
                    }
                }

                // Overhead Gothic Arch Rib spanning the central nave on key bays (castShadows=false so directional light casts crisp character shadows on floor)
                if (b == 0 || b == bays || b % 2 == 1)
                {
                    float spanScale = (aisleX * 2f) / 4.0f;
                    ObjMeshLoader.SpawnPiece("GothicArchVault", colonnade,
                        new Vector3(center.x, 0f, pz), Quaternion.identity,
                        new Vector3(spanScale, pScaleY, 1.15f), pillarMat, addBoxCollider: false, castShadows: false);
                }
            }

            // Longitudinal arcade arches connecting pillars along left & right aisles
            if (bays >= 2)
            {
                float baySpan = ((northZ - 2.4f) - (southZ + 2.4f)) / bays;
                float archScaleX = baySpan / 4.0f;
                for (int b = 0; b < bays; b++)
                {
                    float midZ = Mathf.Lerp(southZ + 2.4f, northZ - 2.4f, (b + 0.5f) / bays);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        ObjMeshLoader.SpawnPiece("GothicArchVault", colonnade,
                            new Vector3(center.x + side * aisleX, 0f, midZ), Quaternion.Euler(0f, 90f, 0f),
                            new Vector3(archScaleX, pScaleY * 0.92f, 0.95f), pillarMat, addBoxCollider: false, castShadows: false);
                    }
                }
            }

            // 5. Low-lying Scrolling Ground Mist + Floor Storytelling Decals (Cracks, Moss & Battle Scorches)
            SpawnGroundMistPlane(root, new Vector3(center.x, 0.08f, center.z), width - 1.5f, length - 1.5f, bData.mistColor);
            ScatterFloorDecals(root, center, width, length, (int)(center.z * 13f) + (int)biome * 37);
        }

        public static void BuildModularCorridor(
            Transform root,
            float southZ,
            float corridorLen,
            float corridorW,
            float wallHeight,
            DungeonBiome biome)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material wallMat = PbrMaterialLibrary.GothicStone(bData.wallStoneTint);
            Material floorMat = PbrMaterialLibrary.CathedralFloor(bData.floorStoneTint);

            var cor = new GameObject("ModularCorridor").transform;
            cor.SetParent(root, false);
            float centerZ = southZ + corridorLen * 0.5f;

            // Floor collider
            var fcGo = new GameObject("CorridorFloorCollider");
            fcGo.transform.SetParent(cor, false);
            fcGo.transform.position = new Vector3(0f, -0.25f, centerZ);
            fcGo.AddComponent<BoxCollider>().size = new Vector3(corridorW + 0.6f, 0.5f, corridorLen + 0.8f);

            // Wall colliders
            AddWallCollider(cor, new Vector3(-corridorW * 0.5f - 0.3f, wallHeight * 0.5f, centerZ), new Vector3(0.6f, wallHeight + 1f, corridorLen));
            AddWallCollider(cor, new Vector3(corridorW * 0.5f + 0.3f, wallHeight * 0.5f, centerZ), new Vector3(0.6f, wallHeight + 1f, corridorLen));

            int segs = Mathf.Max(1, Mathf.RoundToInt(corridorLen / 4f));
            float segLen = corridorLen / segs;
            float wScaleY = wallHeight / 5.2f;

            for (int i = 0; i < segs; i++)
            {
                float z = southZ + (i + 0.5f) * segLen;
                ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", cor,
                    new Vector3(0f, 0f, z), Quaternion.identity,
                    new Vector3(corridorW / 4f, 1f, segLen / 4f), floorMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(-corridorW * 0.5f, 0f, z), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(corridorW * 0.5f, 0f, z), Quaternion.Euler(0f, -90f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);
            }

            ObjMeshLoader.SpawnPiece("GothicArchVault", cor,
                new Vector3(0f, 0f, centerZ), Quaternion.identity,
                new Vector3(corridorW / 4f, wallHeight / 5.1f, 1f), wallMat, false, false);

            // Walkable Aged-Oak Wooden Plank Bridge along the corridor so transitioning between rooms plays authentic wood plank footsteps!
            BuildWoodenPlankDeck(cor, "CorridorBridge", new Vector3(0f, 0f, centerZ), Mathf.Min(3.6f, corridorW - 1.2f), corridorLen - 0.6f);

            SpawnWallTorch(cor, new Vector3(-corridorW * 0.5f + 0.1f, 2.35f, centerZ), Quaternion.Euler(0f, 90f, 0f),
                bData.torchLightColor, bData.torchCoreColor, withPointLight: true, range: 11f, intensity: 1.55f);
            SpawnWallTorch(cor, new Vector3(corridorW * 0.5f - 0.1f, 2.35f, centerZ), Quaternion.Euler(0f, -90f, 0f),
                bData.torchLightColor, bData.torchCoreColor, withPointLight: false);
        }

        public static void DressBiomeEnvironmentProps(
            Transform root,
            Vector3 center,
            float width,
            float length,
            DungeonBiome biome)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material stoneMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.wallStoneTint)
                : PbrMaterialLibrary.GothicStone(bData.wallStoneTint);

            var props = new GameObject("BiomeProps_" + biome).transform;
            props.SetParent(root, false);

            float hx = width * 0.5f - 3.6f;
            float hz = length * 0.5f - 3.6f;

            // Every room gets subtle ruined masonry rubble in 2 corners for authentic Dark Fantasy silhouette
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", props,
                center + new Vector3(-hx + 0.5f, 0f, -hz + 0.6f), Quaternion.Euler(0f, 28f, 0f), Vector3.one, stoneMat, addBoxCollider: false);
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", props,
                center + new Vector3(hx - 0.6f, 0f, hz - 0.8f), Quaternion.Euler(0f, 145f, 0f), Vector3.one * 0.92f, stoneMat, addBoxCollider: false);

            // Walkable Aged-Oak Wooden Plank Walkways & Biome Surface Decks for multi-surface footstep acoustics & VFX
            BuildWoodenPlankDeck(props, "WestQuartermasterDeck", center + new Vector3(-hx + 1.8f, 0f, 0f), 3.4f, Mathf.Min(10f, length * 0.45f));
            BuildWoodenPlankDeck(props, "NorthChestDeck", center + new Vector3(0f, 0f, hz - 1.2f), 5.2f, 3.2f);
            if (biome == DungeonBiome.UndergroundFortress)
            {
                BuildIronGrateDeck(props, "FortressIronGrate", center, 5.4f, 6.4f);
            }

            switch (biome)
            {
                case DungeonBiome.ForgottenTemple:
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-3.8f, 0f, hz), Quaternion.Euler(0f, 180f, 0f), Vector3.one, stoneMat, addBoxCollider: true);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(3.8f, 0f, hz), Quaternion.Euler(0f, 180f, 0f), Vector3.one, stoneMat, addBoxCollider: true);
                    break;

                case DungeonBiome.AncientDungeon:
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-hx, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f, stoneMat, addBoxCollider: true);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(hx, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.05f, stoneMat, addBoxCollider: true);
                    SpawnFireBrazier(props, center + new Vector3(-4.2f, 0f, hz - 0.5f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(4.2f, 0f, hz - 0.5f), bData.torchLightColor, bData.torchCoreColor, false);
                    break;

                case DungeonBiome.UndergroundFortress:
                    SpawnFireBrazier(props, center + new Vector3(-5.2f, 0f, -3.5f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(5.2f, 0f, -3.5f), bData.torchLightColor, bData.torchCoreColor, false);
                    SpawnFireBrazier(props, center + new Vector3(-5.2f, 0f, 4.0f), bData.torchLightColor, bData.torchCoreColor, false);
                    SpawnFireBrazier(props, center + new Vector3(5.2f, 0f, 4.0f), bData.torchLightColor, bData.torchCoreColor, true);
                    break;

                case DungeonBiome.AbyssalCave:
                    Material violetCrys = PbrMaterialLibrary.AbyssalCrystal(new Color(0.28f, 0.14f, 0.65f), new Color(0.68f, 0.32f, 1.0f), 2.5f);
                    ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                        center + new Vector3(-hx, 0f, -hz + 2.2f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.15f, violetCrys, addBoxCollider: true);
                    ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                        center + new Vector3(hx, 0f, hz - 2.2f), Quaternion.Euler(0f, -125f, 0f), Vector3.one * 1.25f, violetCrys, addBoxCollider: true);
                    break;

                case DungeonBiome.CrystalChamber:
                    Material cyanCrys = PbrMaterialLibrary.AbyssalCrystal(new Color(0.14f, 0.52f, 0.88f), new Color(0.32f, 0.92f, 1.0f), 2.8f);
                    Vector3[] cSpots =
                    {
                        center + new Vector3(-hx, 0f, -hz + 2.0f),
                        center + new Vector3(hx, 0f, -hz + 2.0f),
                        center + new Vector3(-hx, 0f, hz - 2.5f),
                        center + new Vector3(hx, 0f, hz - 2.5f)
                    };
                    for (int i = 0; i < cSpots.Length; i++)
                    {
                        ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                            cSpots[i], Quaternion.Euler(0f, i * 77f, 0f), Vector3.one * 1.38f, cyanCrys, addBoxCollider: true);
                    }
                    break;

                case DungeonBiome.CursedForest:
                    Material barkMat = PbrMaterialLibrary.AgedOakWood();
                    Vector3[] rSpots =
                    {
                        center + new Vector3(-hx + 0.4f, 0f, -3.5f),
                        center + new Vector3(hx - 0.4f, 0f, -3.5f),
                        center + new Vector3(-hx + 0.4f, 0f, 4.2f),
                        center + new Vector3(hx - 0.4f, 0f, 4.2f)
                    };
                    for (int i = 0; i < rSpots.Length; i++)
                    {
                        ObjMeshLoader.SpawnPiece("CursedTreeRoot", props,
                            rSpots[i], Quaternion.Euler(0f, i % 2 == 0 ? 45f : -135f, 0f), Vector3.one * 1.22f, barkMat, addBoxCollider: false);
                    }
                    break;

                case DungeonBiome.BossArena:
                    // Monumental Boss Altar & Twin Hooded Sentinel Statues at North Sanctuary Edge
                    ObjMeshLoader.SpawnPiece("BossThroneAltar", props,
                        center + new Vector3(0f, 0f, hz + 0.6f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: true);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-5.2f, 0f, hz + 0.2f), Quaternion.Euler(0f, 165f, 0f), Vector3.one * 1.22f, stoneMat, addBoxCollider: true);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(5.2f, 0f, hz + 0.2f), Quaternion.Euler(0f, -165f, 0f), Vector3.one * 1.22f, stoneMat, addBoxCollider: true);
                    SpawnFireBrazier(props, center + new Vector3(-3.4f, 0f, hz - 0.8f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(3.4f, 0f, hz - 0.8f), bData.torchLightColor, bData.torchCoreColor, true);
                    break;
            }
        }

        public static GameObject SpawnWallTorch(
            Transform parent,
            Vector3 worldOrLocalPos,
            Quaternion localRot,
            Color outerColor,
            Color coreColor,
            bool withPointLight = true,
            float range = 14f,
            float intensity = 2.0f)
        {
            var holder = new GameObject("WallTorchSconce");
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = worldOrLocalPos;
            holder.transform.localRotation = localRot;

            ObjMeshLoader.SpawnPiece("WallTorchSconce", holder.transform,
                Vector3.zero, Quaternion.identity, Vector3.one,
                PbrMaterialLibrary.WroughtIron(), addBoxCollider: false, castShadows: false);

            var flameGo = SpawnFlameVisual(holder.transform, new Vector3(0f, 0.56f, 0.40f), new Vector3(0.38f, 0.54f, 0.38f), outerColor, coreColor);

            if (withPointLight)
            {
                var lGo = new GameObject("TorchPointLight");
                lGo.transform.SetParent(holder.transform, false);
                lGo.transform.localPosition = new Vector3(0f, 0.64f, 0.46f);
                var pl = lGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = outerColor;
                pl.range = range;
                pl.intensity = intensity;
                pl.shadows = LightShadows.None;

                var fl = flameGo.AddComponent<TorchFlickerBehaviour>();
                fl.TargetLight = pl;
                fl.BaseIntensity = intensity;
                fl.FlickerAmplitude = intensity * 0.16f;
            }

            return holder;
        }

        public static GameObject SpawnFireBrazier(
            Transform parent,
            Vector3 localPos,
            Color outerColor,
            Color coreColor,
            bool withPointLight = true)
        {
            var holder = new GameObject("FireBrazier");
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPos;

            ObjMeshLoader.SpawnPiece("FireBrazierGrate", holder.transform,
                Vector3.zero, Quaternion.identity, Vector3.one,
                PbrMaterialLibrary.WroughtIron(), addBoxCollider: true);

            var flameGo = SpawnFlameVisual(holder.transform, new Vector3(0f, 1.18f, 0f), new Vector3(0.76f, 0.92f, 0.76f), outerColor, coreColor);

            if (withPointLight)
            {
                var lGo = new GameObject("BrazierPointLight");
                lGo.transform.SetParent(holder.transform, false);
                lGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                var pl = lGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = outerColor;
                pl.range = 15f;
                pl.intensity = 2.25f;
                pl.shadows = LightShadows.None;

                var fl = flameGo.AddComponent<TorchFlickerBehaviour>();
                fl.TargetLight = pl;
                fl.BaseIntensity = 2.25f;
                fl.FlickerAmplitude = 0.35f;
            }
            return holder;
        }

        public static GameObject SpawnFlameVisual(Transform parent, Vector3 localPos, Vector3 scale, Color outerColor, Color coreColor)
        {
            var go = new GameObject("AbyssalFlame");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetCrossedFlameMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = PbrMaterialLibrary.TorchFlame(outerColor, coreColor);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        public static GameObject SpawnGroundMistPlane(Transform parent, Vector3 pos, float sizeX, float sizeZ, Color mistColor)
        {
            var go = new GameObject("GroundMistLayer");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(sizeX, 1f, sizeZ);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ObjMeshLoader.GetHorizontalQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = PbrMaterialLibrary.GroundMist(mistColor);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        private static void ScatterFloorDecals(Transform root, Vector3 center, float width, float length, int seed)
        {
            var rnd = new System.Random(seed);
            var decalRoot = new GameObject("FloorStoryDecals").transform;
            decalRoot.SetParent(root, false);

            Mesh quad = ObjMeshLoader.GetHorizontalQuadMesh();
            Material crackMat = PbrMaterialLibrary.SurfaceDecal(false);
            Material scorchMat = PbrMaterialLibrary.SurfaceDecal(true);

            int count = 6;
            for (int i = 0; i < count; i++)
            {
                float rx = ((float)rnd.NextDouble() - 0.5f) * (width - 5f);
                float rz = ((float)rnd.NextDouble() - 0.5f) * (length - 5f);
                float sz = 2.2f + (float)rnd.NextDouble() * 2.0f;
                float rot = (float)rnd.NextDouble() * 360f;

                var d = new GameObject("Decal_" + i);
                d.transform.SetParent(decalRoot, false);
                d.transform.position = new Vector3(center.x + rx, 0.014f + i * 0.001f, center.z + rz);
                d.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                d.transform.localScale = new Vector3(sz, 1f, sz);

                d.AddComponent<MeshFilter>().sharedMesh = quad;
                var mr = d.AddComponent<MeshRenderer>();
                mr.sharedMaterial = (i % 3 == 0) ? scorchMat : crackMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
        }

        private static void BuildEndWallWithOptionalGate(
            Transform parent,
            float centerX,
            float z,
            float roomW,
            float wallH,
            float gapW,
            bool hasDoorway,
            float yRot,
            Material wallMat,
            Material frameMat)
        {
            float scaleY = wallH / 5.2f;
            if (!hasDoorway)
            {
                AddWallCollider(parent, new Vector3(centerX, wallH * 0.5f, z + (yRot < 90f ? -0.3f : 0.3f)), new Vector3(roomW + 1f, wallH + 1f, 0.6f));
                int n = Mathf.Max(1, Mathf.RoundToInt(roomW / 4f));
                float segW = roomW / n;
                float sx = centerX - roomW * 0.5f + segW * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    ObjMeshLoader.SpawnPiece("DungeonWall_Modular", parent,
                        new Vector3(sx + i * segW, 0f, z), Quaternion.Euler(0f, yRot, 0f),
                        new Vector3(segW / 4f, scaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);
                }
                return;
            }

            float sideW = (roomW - gapW) * 0.5f;
            if (sideW > 0.4f)
            {
                AddWallCollider(parent, new Vector3(centerX - (gapW * 0.5f + sideW * 0.5f), wallH * 0.5f, z), new Vector3(sideW, wallH + 1f, 0.7f));
                AddWallCollider(parent, new Vector3(centerX + (gapW * 0.5f + sideW * 0.5f), wallH * 0.5f, z), new Vector3(sideW, wallH + 1f, 0.7f));

                int sidePanels = Mathf.Max(1, Mathf.RoundToInt(sideW / 4f));
                float panelW = sideW / sidePanels;
                for (int s = -1; s <= 1; s += 2)
                {
                    float baseEdge = s < 0 ? (centerX - roomW * 0.5f) : (centerX + gapW * 0.5f);
                    for (int i = 0; i < sidePanels; i++)
                    {
                        float px = baseEdge + (i + 0.5f) * panelW;
                        ObjMeshLoader.SpawnPiece("DungeonWall_Modular", parent,
                            new Vector3(px, 0f, z), Quaternion.Euler(0f, yRot, 0f),
                            new Vector3(panelW / 4f, scaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);
                    }
                }
            }

            // Monumental Gothic Gate Frame around the 6m doorway
            ObjMeshLoader.SpawnPiece("GothicGateFrame", parent,
                new Vector3(centerX, 0f, z), Quaternion.Euler(0f, yRot, 0f),
                new Vector3(gapW / 6.0f, scaleY, 1f), frameMat, addBoxCollider: false, castShadows: false);
        }

        /// <summary>
        /// Builds a walkable Aged-Oak Wooden Plank Platform/Bridge with iron trim and a flush BoxCollider ("WoodFloor_*")
        /// so stepping onto it triggers authentic hollow wooden plank footsteps and wood-sawdust VFX.
        /// </summary>
        public static GameObject BuildWoodenPlankDeck(Transform parent, string name, Vector3 centerPos, float width, float length)
        {
            var deck = new GameObject("WoodFloor_" + name);
            deck.transform.SetParent(parent, false);
            deck.transform.position = new Vector3(centerPos.x, 0f, centerPos.z);

            var col = deck.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.08f, 0f);
            col.size = new Vector3(width, 0.20f, length); // Top surface at y = +0.02m (above stone collider at y = 0.0m)

            Material woodMat = PbrMaterialLibrary.AgedOakWood();
            Material ironMat = PbrMaterialLibrary.WroughtIron();

            int plankCount = Mathf.Clamp(Mathf.RoundToInt(length / 0.55f), 4, 22);
            float stepZ = length / plankCount;
            float plankLenZ = stepZ * 0.90f;

            for (int i = 0; i < plankCount; i++)
            {
                float lz = -length * 0.5f + (i + 0.5f) * stepZ;
                float shade = (i % 2 == 0) ? 1.0f : 0.94f;
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "WoodPlank_" + i;
                plank.transform.SetParent(deck.transform, false);
                plank.transform.localPosition = new Vector3(0f, 0.011f, lz);
                plank.transform.localScale = new Vector3(width - 0.14f, 0.022f * shade, plankLenZ);
                Object.Destroy(plank.GetComponent<Collider>());
                var mr = plank.GetComponent<MeshRenderer>();
                mr.sharedMaterial = woodMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }

            // Left & Right Wrought-Iron Edge Stringers
            for (int s = -1; s <= 1; s += 2)
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "WoodDeckIronEdge";
                rail.transform.SetParent(deck.transform, false);
                rail.transform.localPosition = new Vector3(s * (width * 0.5f - 0.06f), 0.014f, 0f);
                rail.transform.localScale = new Vector3(0.12f, 0.028f, length);
                Object.Destroy(rail.GetComponent<Collider>());
                var mr = rail.GetComponent<MeshRenderer>();
                mr.sharedMaterial = ironMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }

            return deck;
        }

        /// <summary>
        /// Builds a walkable Forged Iron Grate Floor Deck ("MetalFloor_*") for metallic clink footsteps & spark VFX.
        /// </summary>
        public static GameObject BuildIronGrateDeck(Transform parent, string name, Vector3 centerPos, float width, float length)
        {
            var deck = new GameObject("MetalFloor_" + name);
            deck.transform.SetParent(parent, false);
            deck.transform.position = new Vector3(centerPos.x, 0f, centerPos.z);

            var col = deck.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.08f, 0f);
            col.size = new Vector3(width, 0.20f, length);

            Material ironMat = PbrMaterialLibrary.WroughtIron();
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "IronGratePlate";
            plate.transform.SetParent(deck.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.011f, 0f);
            plate.transform.localScale = new Vector3(width, 0.022f, length);
            Object.Destroy(plate.GetComponent<Collider>());
            var mr = plate.GetComponent<MeshRenderer>();
            mr.sharedMaterial = ironMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            return deck;
        }

        private static void AddWallCollider(Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject("WallCollider");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static Mesh GetCrossedFlameMesh()
        {
            if (_crossedFlameMesh != null) return _crossedFlameMesh;
            _crossedFlameMesh = new Mesh { name = "CrossedTorchFlameMesh" };
            // Two vertical intersecting quads (X and Z planes) from y = -0.25 to +0.75 with UVs 0..1
            _crossedFlameMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.2f, 0f), new Vector3(-0.5f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0f), new Vector3(0.5f, -0.2f, 0f),
                new Vector3(0f, -0.2f, -0.5f), new Vector3(0f, 0.8f, -0.5f), new Vector3(0f, 0.8f, 0.5f), new Vector3(0f, -0.2f, 0.5f)
            };
            _crossedFlameMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f)
            };
            _crossedFlameMesh.normals = new[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };
            _crossedFlameMesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7
            };
            _crossedFlameMesh.RecalculateBounds();
            return _crossedFlameMesh;
        }
    }
}

