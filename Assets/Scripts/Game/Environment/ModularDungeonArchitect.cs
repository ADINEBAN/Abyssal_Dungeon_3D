using UnityEngine;
using AbyssalDungeon.VFX;

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
    /// Modular 3D Ancient Gothic Cathedral Environment Builder assembling modular ashlar walls,
    /// high ribbed ogive vaults (zero camera obstruction!), warm torch vs cool arcane lighting,
    /// lightweight URP volumetric light shafts, Fallen Angel & Guardian statues, stone coffins,
    /// barrels/crates, wall runes, tactical cover, and 4-directional corridor connectors.
    /// </summary>
    public static class ModularDungeonArchitect
    {
        private static Mesh _crossedFlameMesh;
        private static readonly Color CoolArcaneLight = new Color(0.36f, 0.74f, 1.00f);
        private static readonly Color WarmShaftTint = new Color(1.00f, 0.86f, 0.62f, 0.13f);
        private static readonly Color ArcaneShaftTint = new Color(0.46f, 0.78f, 1.00f, 0.15f);

        public static void BuildModularRoomShell(
            Transform root,
            Vector3 center,
            float width,
            float length,
            float wallHeight,
            DungeonBiome biome,
            bool openSouthDoor,
            bool openNorthDoor,
            float doorwayWidth = 6f,
            bool openEastDoor = false,
            bool openWestDoor = false,
            float sideDoorwayWidth = 6f,
            bool coolArcaneAccent = false)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material wallMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.wallStoneTint)
                : PbrMaterialLibrary.GothicStone(bData.wallStoneTint);
            Material floorMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.floorStoneTint)
                : PbrMaterialLibrary.CathedralFloor(bData.floorStoneTint);
            Material pillarMat = PbrMaterialLibrary.GothicStone(bData.wallStoneTint * 0.94f);

            // Ensure generous Gothic cathedral height so overhead ribbed arches NEVER block OrbitCamera3D!
            float effWallH = Mathf.Max(6.4f, wallHeight);

            // 1. Smooth physics floor collider (y = 0 top surface) so CharacterControllers never snag
            var floorColGo = new GameObject("FloorPhysicsCollider");
            floorColGo.transform.SetParent(root, false);
            floorColGo.transform.position = new Vector3(center.x, -0.25f, center.z);
            var fc = floorColGo.AddComponent<BoxCollider>();
            fc.size = new Vector3(width + 1.4f, 0.5f, length + 1.4f);

            // 2. Modular 4m x 4m Cathedral Flagstone Floor Tiles
            var floorGroup = new GameObject("ModularFloorTiles").transform;
            floorGroup.SetParent(root, false);
            int nx = Mathf.Max(1, Mathf.RoundToInt(width / 4f));
            int nz = Mathf.Max(1, Mathf.RoundToInt(length / 4f));
            float tileW = width / nx;
            float tileL = length / nz;
            float tileScaleX = tileW / 4f;
            float tileScaleZ = tileL / 4f;
            float startX = center.x - width * 0.5f + tileW * 0.5f;
            float startZ = center.z - length * 0.5f + tileL * 0.5f;

            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    Vector3 tPos = new Vector3(startX + ix * tileW, 0f, startZ + iz * tileL);
                    ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", floorGroup,
                        tPos, Quaternion.identity, new Vector3(tileScaleX, 1f, tileScaleZ),
                        floorMat, addBoxCollider: false, castShadows: false);
                }
            }

            // Central carved Gothic cathedral floor runner medallion (breaks up large stone rooms subtly!)
            ObjMeshLoader.SpawnPiece("FloorTile_GnG", floorGroup,
                new Vector3(center.x, 0.012f, center.z), Quaternion.identity,
                new Vector3(Mathf.Clamp(width * 0.08f, 1.0f, 1.75f), 1f, Mathf.Clamp(length * 0.08f, 1.0f, 1.75f)),
                floorMat, addBoxCollider: false, castShadows: false);

            // 3. Perimeter Physics Walls + Modular Gothic Blind-Arch Wall Panels (4-directional doorways)
            var wallGroup = new GameObject("ModularPerimeterWalls").transform;
            wallGroup.SetParent(root, false);
            float southZ = center.z - length * 0.5f;
            float northZ = center.z + length * 0.5f;
            float westX = center.x - width * 0.5f;
            float eastX = center.x + width * 0.5f;

            // West (-X, faces +X) & East (+X, faces -X) with optional branching side-corridor gate!
            BuildSideWallWithOptionalGate(wallGroup, westX, center.z, length, effWallH, sideDoorwayWidth, openWestDoor, 90f, wallMat, pillarMat);
            BuildSideWallWithOptionalGate(wallGroup, eastX, center.z, length, effWallH, sideDoorwayWidth, openEastDoor, -90f, wallMat, pillarMat);

            // South Wall (-Z, faces +Z) & North Wall (+Z, faces -Z)
            BuildEndWallWithOptionalGate(wallGroup, center.x, southZ, width, effWallH, doorwayWidth, openSouthDoor, 0f, wallMat, pillarMat);
            BuildEndWallWithOptionalGate(wallGroup, center.x, northZ, width, effWallH, doorwayWidth, openNorthDoor, 180f, wallMat, pillarMat);

            // 4. Interior Gothic Cathedral Colonnade + HIGH Ribbed Ogive Vault Arches + Warm/Cool Lighting
            var colonnade = new GameObject("GothicColonnade").transform;
            colonnade.SetParent(root, false);
            var vaults = new GameObject("GothicVaults").transform;
            vaults.SetParent(root, false);
            var shafts = new GameObject("VolumetricLightShafts").transform;
            shafts.SetParent(root, false);
            var wallRunes = new GameObject("WallRunes").transform;
            wallRunes.SetParent(root, false);

            // Keep wide >= 2.6m side aisles so AI/players can NEVER wedge behind columns!
            float aisleX = Mathf.Max(4.2f, width * 0.5f - 2.75f);
            int bays = Mathf.Clamp(Mathf.RoundToInt(length / 7.8f), 2, 5);
            float pScaleY = effWallH / 5.1f;

            for (int b = 0; b <= bays; b++)
            {
                float t = (float)b / bays;
                float pz = Mathf.Lerp(southZ + 2.8f, northZ - 2.8f, t);

                // Skip pillar right in front of an open East/West side doorway!
                bool nearSideGate = Mathf.Abs(pz - center.z) < (sideDoorwayWidth * 0.5f + 1.4f);

                for (int side = -1; side <= 1; side += 2)
                {
                    if (nearSideGate && ((side < 0 && openWestDoor) || (side > 0 && openEastDoor)))
                        continue;

                    Vector3 pPos = new Vector3(center.x + side * aisleX, 0f, pz);
                    var pillarGo = ObjMeshLoader.SpawnPiece("GothicPillar", colonnade,
                        pPos, Quaternion.identity, new Vector3(1.06f, pScaleY, 1.06f),
                        pillarMat, addBoxCollider: false, castShadows: false);
                    // Clean slim cylinder-approximated BoxCollider so characters glide around pillars
                    var pc = pillarGo.AddComponent<BoxCollider>();
                    pc.center = new Vector3(0f, 2.5f, 0f);
                    pc.size = new Vector3(0.92f, 5.0f, 0.92f);

                    // Mount wrought-iron wall torches on alternating pillars (warm cathedral glow, zero shadow cost!)
                    bool mountLight = (b == 0 && side < 0) || (b == bays && side > 0) || (b == bays / 2 && side > 0);
                    if (mountLight || b % 2 == 1)
                    {
                        Quaternion tRot = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);
                        Vector3 tPos = pPos + new Vector3(side < 0 ? 0.52f : -0.52f, 2.75f, 0f);
                        SpawnWallTorch(colonnade, tPos, tRot, bData.torchLightColor, bData.torchCoreColor,
                            withPointLight: mountLight, range: 20f, intensity: 2.55f);
                    }
                }

                // High Sculpted Blender Gothic Ribbed Ogive Vault Arch (springers at y = effWallH - 0.8m -> apex at effWallH + 2.35m, 100% ABOVE camera!)
                if (b == 0 || b == bays || b % 2 == 1)
                {
                    float vaultSpanScaleX = Mathf.Clamp((aisleX * 2.04f) / 6.0f, 1.2f, 5.2f);
                    ObjMeshLoader.SpawnPiece("GothicCathedralVaultRib", vaults,
                        new Vector3(center.x, effWallH - 0.78f, pz), Quaternion.identity,
                        new Vector3(vaultSpanScaleX, 1.05f, 1.15f), null, addBoxCollider: false, castShadows: false);

                    // Hanging chains high near upper cathedral vaults (never obstructing hero/enemy combat!)
                    ObjMeshLoader.SpawnPiece("HangingChain_GnG", vaults,
                        new Vector3(center.x - aisleX * 0.68f, effWallH - 1.05f, pz), Quaternion.identity,
                        Vector3.one * 0.92f, PbrMaterialLibrary.WroughtIron(), addBoxCollider: false, castShadows: false);
                    ObjMeshLoader.SpawnPiece("HangingChain_GnG", vaults,
                        new Vector3(center.x + aisleX * 0.68f, effWallH - 1.05f, pz), Quaternion.Euler(0f, 90f, 0f),
                        Vector3.one * 0.92f, PbrMaterialLibrary.WroughtIron(), addBoxCollider: false, castShadows: false);
                }

                // Recessed Glowing Wall Rune Tablets mounted cleanly on perimeter side walls
                if (b > 0 && b < bays && (!nearSideGate || (!openWestDoor && !openEastDoor)))
                {
                    if (!openWestDoor || !nearSideGate)
                    {
                        ObjMeshLoader.SpawnPiece("WallRuneTablet_Gothic", wallRunes,
                            new Vector3(westX + 0.16f, 0.85f, pz), Quaternion.Euler(0f, 90f, 0f),
                            Vector3.one * 1.05f, null, addBoxCollider: false, castShadows: false);
                    }
                    if (!openEastDoor || !nearSideGate)
                    {
                        ObjMeshLoader.SpawnPiece("WallRuneTablet_Gothic", wallRunes,
                            new Vector3(eastX - 0.16f, 0.85f, pz), Quaternion.Euler(0f, -90f, 0f),
                            Vector3.one * 1.05f, null, addBoxCollider: false, castShadows: false);
                    }
                }
            }

            // 5. Lightweight URP Volumetric Cathedral Light Shafts (zero realtime light cost!) + Optional Cool Arcane Zone Accent Light
            SpawnRoomVolumetricShafts(shafts, center, width, length, effWallH, biome, coolArcaneAccent);

            // 6. Floor Storytelling Decals (cracks, battle scorch, ancient floor runes) + Ambient Environment VFX
            ScatterFloorDecals(root, center, width, length, (int)(center.z * 13f + center.x * 7f) + (int)biome * 37);
            AttachRoomAmbientVfx(root, center, width, length, biome, bData);
        }

        private static void SpawnRoomVolumetricShafts(
            Transform parent,
            Vector3 center,
            float width,
            float length,
            float wallH,
            DungeonBiome biome,
            bool coolArcaneAccent)
        {
            bool isArcaneRoom = coolArcaneAccent
                || biome == DungeonBiome.AbyssalCave
                || biome == DungeonBiome.CrystalChamber
                || biome == DungeonBiome.BossArena;

            Color warmCol = WarmShaftTint;
            Color coolCol = ArcaneShaftTint;
            Material warmMat = PbrMaterialLibrary.LightShaft(warmCol, 0.62f);
            Material coolMat = PbrMaterialLibrary.LightShaft(coolCol, 0.68f);

            float sx = Mathf.Max(3.6f, width * 0.30f);
            float sz = Mathf.Max(3.4f, length * 0.28f);
            float scaleY = Mathf.Clamp(wallH / 7.2f, 0.90f, 1.25f);

            // Delicate slanted clerestory god-ray cones positioned cleanly along upper cathedral aisles (never flaring in camera face!)
            SpawnVolumetricShaft(parent, center + new Vector3(-sx, 0.05f, sz * 0.55f), Quaternion.Euler(0f, 16f, 0f),
                new Vector3(0.56f, scaleY, 0.56f), warmMat);
            SpawnVolumetricShaft(parent, center + new Vector3(sx, 0.05f, sz * 0.65f), Quaternion.Euler(0f, 196f, 0f),
                new Vector3(0.56f, scaleY, 0.56f), isArcaneRoom ? coolMat : warmMat);

            if (isArcaneRoom)
            {
                // Cool arcane blue light accent from magic zone (balances warm orange torchlight!)
                var lGo = new GameObject("ArcaneZoneAccentLight");
                lGo.transform.SetParent(parent, false);
                lGo.transform.position = center + new Vector3(0f, 4.2f, length * 0.22f);
                var pl = lGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = CoolArcaneLight;
                pl.range = Mathf.Max(16f, width * 0.72f);
                pl.intensity = 1.95f;
                pl.shadows = LightShadows.None;
            }
        }

        public static GameObject SpawnVolumetricShaft(Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material shaftMat)
        {
            var go = ObjMeshLoader.SpawnPiece("GothicLightShaft_Cone", parent, pos, rot, scale, null, addBoxCollider: false, castShadows: false);
            if (go != null && shaftMat != null)
            {
                var rends = go.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rends.Length; i++)
                {
                    rends[i].sharedMaterial = shaftMat;
                    rends[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rends[i].receiveShadows = false;
                }
            }
            return go;
        }

        /// <summary>
        /// Builds a North-South Ancient Gothic Vaulted Corridor connecting rooms along +Z.
        /// </summary>
        public static void BuildModularCorridor(
            Transform root,
            float southZ,
            float corridorLen,
            float corridorW,
            float wallHeight,
            DungeonBiome biome,
            float centerX = 0f)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material wallMat = PbrMaterialLibrary.GothicStone(bData.wallStoneTint);
            Material floorMat = PbrMaterialLibrary.CathedralFloor(bData.floorStoneTint);
            float effWallH = Mathf.Max(6.4f, wallHeight);

            var cor = new GameObject("ModularCorridor").transform;
            cor.SetParent(root, false);
            float centerZ = southZ + corridorLen * 0.5f;

            // Floor collider
            var fcGo = new GameObject("CorridorFloorCollider");
            fcGo.transform.SetParent(cor, false);
            fcGo.transform.position = new Vector3(centerX, -0.25f, centerZ);
            fcGo.AddComponent<BoxCollider>().size = new Vector3(corridorW + 0.8f, 0.5f, corridorLen + 1.0f);

            // Wall colliders
            AddWallCollider(cor, new Vector3(centerX - corridorW * 0.5f - 0.3f, effWallH * 0.5f, centerZ), new Vector3(0.6f, effWallH + 1f, corridorLen + 0.4f));
            AddWallCollider(cor, new Vector3(centerX + corridorW * 0.5f + 0.3f, effWallH * 0.5f, centerZ), new Vector3(0.6f, effWallH + 1f, corridorLen + 0.4f));

            int segs = Mathf.Max(1, Mathf.RoundToInt(corridorLen / 4f));
            float segLen = corridorLen / segs;
            float wScaleY = effWallH / 5.2f;

            for (int i = 0; i < segs; i++)
            {
                float z = southZ + (i + 0.5f) * segLen;
                ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", cor,
                    new Vector3(centerX, 0f, z), Quaternion.identity,
                    new Vector3(corridorW / 4f, 1f, segLen / 4f), floorMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(centerX - corridorW * 0.5f, 0f, z), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(centerX + corridorW * 0.5f, 0f, z), Quaternion.Euler(0f, -90f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);

                // Sculpted High Pointed Gothic Vault Rib overhead every corridor bay (well above camera!)
                ObjMeshLoader.SpawnPiece("GothicCathedralVaultRib", cor,
                    new Vector3(centerX, effWallH - 1.15f, z), Quaternion.identity,
                    new Vector3((corridorW + 0.4f) / 6.0f, 0.85f, 1.0f), null, false, false);
            }

            // Gothic Archway framing corridor center
            ObjMeshLoader.SpawnPiece("GothicArchVault", cor,
                new Vector3(centerX, 0f, centerZ), Quaternion.identity,
                new Vector3(corridorW / 4f, effWallH / 5.1f, 1f), wallMat, false, false);

            // Walkable Aged-Oak Wooden Plank Bridge along corridor center
            BuildWoodenPlankDeck(cor, "CorridorBridge", new Vector3(centerX, 0f, centerZ), Mathf.Min(3.6f, corridorW - 1.4f), corridorLen - 0.6f);

            // Warm orange sconce torches + subtle volumetric god-ray shaft + wall rune plaque
            SpawnWallTorch(cor, new Vector3(centerX - corridorW * 0.5f + 0.14f, 2.55f, centerZ), Quaternion.Euler(0f, 90f, 0f),
                bData.torchLightColor, bData.torchCoreColor, withPointLight: true, range: 13f, intensity: 1.75f);
            SpawnWallTorch(cor, new Vector3(centerX + corridorW * 0.5f - 0.14f, 2.55f, centerZ), Quaternion.Euler(0f, -90f, 0f),
                bData.torchLightColor, bData.torchCoreColor, withPointLight: false);

            ObjMeshLoader.SpawnPiece("WallRuneTablet_Gothic", cor,
                new Vector3(centerX + corridorW * 0.5f - 0.14f, 0.80f, southZ + 1.8f), Quaternion.Euler(0f, -90f, 0f),
                Vector3.one * 0.82f, null, addBoxCollider: false, castShadows: false);

            SpawnVolumetricShaft(cor, new Vector3(centerX, 0.02f, centerZ), Quaternion.Euler(0f, 25f, 0f),
                new Vector3(0.58f, 0.92f, 0.58f), PbrMaterialLibrary.LightShaft(WarmShaftTint, 0.85f));

            if (Application.isPlaying)
            {
                var mgr = VFXManager.EnsureInstance();
                if (mgr != null)
                    mgr.SpawnLoopingVFX(VFXType.Env_GroundFog, cor, new Vector3(centerX, 0.14f, centerZ), Quaternion.identity, bData.mistColor, 0.65f);
            }
        }

        /// <summary>
        /// Builds an East-West Gothic Vaulted Side Corridor connecting a Main Room to a Branching Side Room (e.g. Treasure Vault).
        /// </summary>
        public static void BuildModularSideCorridor(
            Transform root,
            float westX,
            float corridorLenX,
            float centerZ,
            float corridorW,
            float wallHeight,
            DungeonBiome biome)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material wallMat = PbrMaterialLibrary.GothicStone(bData.wallStoneTint);
            Material floorMat = PbrMaterialLibrary.CathedralFloor(bData.floorStoneTint);
            float effWallH = Mathf.Max(6.4f, wallHeight);

            var cor = new GameObject("ModularSideCorridor_EW").transform;
            cor.SetParent(root, false);
            float centerX = westX + corridorLenX * 0.5f;

            var fcGo = new GameObject("SideCorridorFloorCollider");
            fcGo.transform.SetParent(cor, false);
            fcGo.transform.position = new Vector3(centerX, -0.25f, centerZ);
            fcGo.AddComponent<BoxCollider>().size = new Vector3(corridorLenX + 1.0f, 0.5f, corridorW + 0.8f);

            // South & North corridor walls
            AddWallCollider(cor, new Vector3(centerX, effWallH * 0.5f, centerZ - corridorW * 0.5f - 0.3f), new Vector3(corridorLenX + 0.4f, effWallH + 1f, 0.6f));
            AddWallCollider(cor, new Vector3(centerX, effWallH * 0.5f, centerZ + corridorW * 0.5f + 0.3f), new Vector3(corridorLenX + 0.4f, effWallH + 1f, 0.6f));

            int segs = Mathf.Max(1, Mathf.RoundToInt(corridorLenX / 4f));
            float segLen = corridorLenX / segs;
            float wScaleY = effWallH / 5.2f;

            for (int i = 0; i < segs; i++)
            {
                float x = westX + (i + 0.5f) * segLen;
                ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", cor,
                    new Vector3(x, 0f, centerZ), Quaternion.identity,
                    new Vector3(segLen / 4f, 1f, corridorW / 4f), floorMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(x, 0f, centerZ - corridorW * 0.5f), Quaternion.Euler(0f, 0f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);

                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", cor,
                    new Vector3(x, 0f, centerZ + corridorW * 0.5f), Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(segLen / 4f, wScaleY, 1f), wallMat, false, false);

                ObjMeshLoader.SpawnPiece("GothicCathedralVaultRib", cor,
                    new Vector3(x, effWallH - 1.15f, centerZ), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3((corridorW + 0.4f) / 6.0f, 0.85f, 1.0f), null, false, false);
            }

            ObjMeshLoader.SpawnPiece("GothicArchVault", cor,
                new Vector3(centerX, 0f, centerZ), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(corridorW / 4f, effWallH / 5.1f, 1f), wallMat, false, false);

            // Cool arcane blue torch + warm torch contrast guiding player to hidden Treasure Room
            SpawnWallTorch(cor, new Vector3(centerX, 2.55f, centerZ + corridorW * 0.5f - 0.14f), Quaternion.Euler(0f, 180f, 0f),
                CoolArcaneLight, new Color(0.82f, 0.94f, 1f), withPointLight: true, range: 13f, intensity: 1.75f);
            SpawnWallTorch(cor, new Vector3(centerX, 2.55f, centerZ - corridorW * 0.5f + 0.14f), Quaternion.identity,
                bData.torchLightColor, bData.torchCoreColor, withPointLight: false);

            SpawnVolumetricShaft(cor, new Vector3(centerX, 0.02f, centerZ), Quaternion.Euler(0f, 110f, 0f),
                new Vector3(0.75f, 0.90f, 0.75f), PbrMaterialLibrary.LightShaft(ArcaneShaftTint, 1.15f));
        }

        public static void DressBiomeEnvironmentProps(
            Transform root,
            Vector3 center,
            float width,
            float length,
            DungeonBiome biome,
            RoomType roomType = RoomType.Combat)
        {
            BiomeAtmosphereData bData = BiomeAtmosphereLibrary.Get(biome);
            Material stoneMat = bData.preferMossyStone
                ? PbrMaterialLibrary.MossyRuins(bData.wallStoneTint)
                : PbrMaterialLibrary.GothicStone(bData.wallStoneTint);

            var props = new GameObject("BiomeProps_" + biome).transform;
            props.SetParent(root, false);

            // Perimeter prop coordinates kept safely along room walls (never trapping AI/players in corners)
            float hx = width * 0.5f - 2.1f;
            float hz = length * 0.5f - 2.3f;

            // Core Ancient Gothic Cathedral dressing along room perimeter
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", props,
                center + new Vector3(-hx + 0.4f, 0f, -hz + 0.5f), Quaternion.Euler(0f, 28f, 0f), Vector3.one * 0.9f, stoneMat, addBoxCollider: false, castShadows: false);
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", props,
                center + new Vector3(hx - 0.5f, 0f, hz - 0.7f), Quaternion.Euler(0f, 145f, 0f), Vector3.one * 0.88f, stoneMat, addBoxCollider: false, castShadows: false);

            // Wooden Barrels & Crates supply cluster tucked along Southwest perimeter
            ObjMeshLoader.SpawnPiece("WoodenBarrelsCrates_Gothic", props,
                center + new Vector3(-hx + 0.65f, 0f, -hz + 2.4f), Quaternion.Euler(0f, 24f, 0f),
                Vector3.one * 1.05f, null, addBoxCollider: true, castShadows: false);

            // Carved Stone Coffin & Ritual Table along far corners
            ObjMeshLoader.SpawnPiece("StoneCoffin_GnG", props,
                center + new Vector3(hx - 0.55f, 0f, -hz + 2.6f), Quaternion.Euler(0f, -90f, 0f), Vector3.one, stoneMat, addBoxCollider: true, castShadows: false);
            ObjMeshLoader.SpawnPiece("RitualTable_GnG", props,
                center + new Vector3(-hx + 0.60f, 0f, hz - 2.6f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, PbrMaterialLibrary.AgedOakWood(), addBoxCollider: true, castShadows: false);
            ObjMeshLoader.SpawnPiece("CandleGroup_GnG", props,
                center + new Vector3(-hx + 1.25f, 0f, hz - 1.7f), Quaternion.Euler(0f, 35f, 0f), Vector3.one, stoneMat, addBoxCollider: false, castShadows: false);

            // High Gothic Chandelier suspended high above combat sightline (y = 5.4m+)
            float chandY = roomType == RoomType.Boss ? 7.2f : 5.55f;
            ObjMeshLoader.SpawnPiece("GothicChandelier_GnG", props,
                center + new Vector3(0f, chandY, 0f), Quaternion.identity, Vector3.one * 1.12f, PbrMaterialLibrary.WroughtIron(), addBoxCollider: false, castShadows: false);

            // Tactical Combat Cover in Combat / Elite / MiniBoss rooms (wide >= 4.0m lanes around every cover piece!)
            if (roomType == RoomType.Combat || roomType == RoomType.Elite || roomType == RoomType.MiniBoss)
            {
                BuildRoomTacticalCover(props, center, width, length, stoneMat, roomType);
            }

            if (biome == DungeonBiome.UndergroundFortress)
            {
                BuildIronGrateDeck(props, "FortressIronGrate", center, 5.6f, 6.6f);
            }

            // Room-purpose & biome focal landmarks
            if (roomType == RoomType.Treasure)
            {
                // Treasure Vault: Fallen Angel Guardian + Ritual Candles + Twin Barrels + Ornate Braziers + Grand Steps
                ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                    center + new Vector3(-3.4f, 0f, hz - 0.6f), Quaternion.Euler(0f, 155f, 0f), Vector3.one * 1.02f, null, addBoxCollider: true, castShadows: false);
                ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                    center + new Vector3(3.4f, 0f, hz - 0.6f), Quaternion.Euler(0f, -155f, 0f), Vector3.one * 1.02f, null, addBoxCollider: true, castShadows: false);
                ObjMeshLoader.SpawnPiece("WoodenBarrelsCrates_Gothic", props,
                    center + new Vector3(hx - 0.8f, 0f, hz - 2.0f), Quaternion.Euler(0f, -110f, 0f), Vector3.one * 1.05f, null, addBoxCollider: true, castShadows: false);
                SpawnFireBrazier(props, center + new Vector3(-2.6f, 0f, 1.5f), bData.torchLightColor, bData.torchCoreColor, true);
                SpawnFireBrazier(props, center + new Vector3(2.6f, 0f, 1.5f), CoolArcaneLight, new Color(0.85f, 0.96f, 1f), false);
                EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(-1.5f, 0f, hz - 1.2f), Quaternion.identity, bData.torchCoreColor);
                EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(1.5f, 0f, hz - 1.2f), Quaternion.identity, bData.torchCoreColor);
                return;
            }

            switch (biome)
            {
                case DungeonBiome.ForgottenTemple:
                    // Entrance Cathedral Nave: Twin Fallen Angel Statues flanking the Northern Arch + Warrior Sentinels
                    ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                        center + new Vector3(-4.6f, 0f, hz - 0.6f), Quaternion.Euler(0f, 162f, 0f), Vector3.one * 1.08f, null, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                        center + new Vector3(4.6f, 0f, hz - 0.6f), Quaternion.Euler(0f, -162f, 0f), Vector3.one * 1.08f, null, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-4.8f, 0f, -hz + 1.4f), Quaternion.Euler(0f, 24f, 0f), Vector3.one * 1.02f, stoneMat, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(4.8f, 0f, -hz + 1.4f), Quaternion.Euler(0f, -24f, 0f), Vector3.one * 1.02f, stoneMat, addBoxCollider: true, castShadows: false);
                    SpawnFireBrazier(props, center + new Vector3(-3.3f, 0f, hz - 1.8f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(3.3f, 0f, hz - 1.8f), bData.torchLightColor, bData.torchCoreColor, false);
                    EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(-2.3f, 0f, hz - 0.8f), Quaternion.Euler(0f, 15f, 0f), bData.torchCoreColor);
                    EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(2.3f, 0f, hz - 0.8f), Quaternion.Euler(0f, -15f, 0f), bData.torchCoreColor);
                    break;

                case DungeonBiome.AncientDungeon:
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-hx + 0.6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f, stoneMat, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("WoodenBarrelsCrates_Gothic", props,
                        center + new Vector3(hx - 0.8f, 0f, hz - 2.2f), Quaternion.Euler(0f, -125f, 0f), Vector3.one * 1.02f, null, addBoxCollider: true, castShadows: false);
                    SpawnFireBrazier(props, center + new Vector3(-4.4f, 0f, hz - 1.0f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(4.4f, 0f, hz - 1.0f), bData.torchLightColor, bData.torchCoreColor, false);
                    break;

                case DungeonBiome.UndergroundFortress:
                    ObjMeshLoader.SpawnPiece("KnightStatue_GnG", props,
                        center + new Vector3(-hx + 0.7f, 0f, 3.2f), Quaternion.Euler(0f, 110f, 0f), Vector3.one * 1.10f, stoneMat, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("KnightStatue_GnG", props,
                        center + new Vector3(hx - 0.7f, 0f, 3.2f), Quaternion.Euler(0f, -110f, 0f), Vector3.one * 1.10f, stoneMat, addBoxCollider: true, castShadows: false);
                    SpawnFireBrazier(props, center + new Vector3(-5.5f, 0f, -3.5f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(5.5f, 0f, -3.5f), bData.torchLightColor, bData.torchCoreColor, false);
                    SpawnFireBrazier(props, center + new Vector3(-5.5f, 0f, 4.2f), bData.torchLightColor, bData.torchCoreColor, false);
                    SpawnFireBrazier(props, center + new Vector3(5.5f, 0f, 4.2f), bData.torchLightColor, bData.torchCoreColor, true);
                    if (Application.isPlaying)
                    {
                        var vfx = VFXManager.EnsureInstance();
                        if (vfx != null)
                            vfx.SpawnLoopingVFX(VFXType.Env_HazardDenseSmoke, props, center + new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Color(0.24f, 0.16f, 0.14f, 0.62f), 0.9f);
                    }
                    break;

                case DungeonBiome.AbyssalCave:
                    Material arcaneBlueCrys = PbrMaterialLibrary.AbyssalCrystal(new Color(0.16f, 0.44f, 0.88f), new Color(0.42f, 0.78f, 1.0f), 2.4f);
                    Vector3 caveC1 = center + new Vector3(-hx + 0.6f, 0f, -hz + 2.2f);
                    Vector3 caveC2 = center + new Vector3(hx - 0.6f, 0f, hz - 2.2f);
                    ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                        caveC1, Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.15f, arcaneBlueCrys, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                        caveC2, Quaternion.Euler(0f, -125f, 0f), Vector3.one * 1.25f, arcaneBlueCrys, addBoxCollider: true, castShadows: false);
                    // Inscribed floor rune ring in Puzzle sanctuary
                    ObjMeshLoader.SpawnPiece("EscapeFloorPortal", props,
                        center + Vector3.up * 0.015f, Quaternion.identity, new Vector3(1.85f, 0.5f, 1.85f),
                        PbrMaterialLibrary.AncientRuneGlow(CoolArcaneLight), addBoxCollider: false, castShadows: false);
                    if (Application.isPlaying)
                    {
                        var vfx = VFXManager.EnsureInstance();
                        if (vfx != null)
                        {
                            vfx.SpawnLoopingVFX(VFXType.Env_CrystalGlow, props, caveC1 + Vector3.up * 1.1f, Quaternion.identity, CoolArcaneLight, 1.0f);
                            vfx.SpawnLoopingVFX(VFXType.Env_CrystalGlow, props, caveC2 + Vector3.up * 1.2f, Quaternion.identity, CoolArcaneLight, 1.05f);
                        }
                    }
                    break;

                case DungeonBiome.CrystalChamber:
                    Material cyanCrys = PbrMaterialLibrary.AbyssalCrystal(new Color(0.14f, 0.52f, 0.88f), new Color(0.32f, 0.92f, 1.0f), 2.8f);
                    Vector3[] cSpots =
                    {
                        center + new Vector3(-hx + 0.6f, 0f, -hz + 2.2f),
                        center + new Vector3(hx - 0.6f, 0f, -hz + 2.2f),
                        center + new Vector3(-hx + 0.6f, 0f, hz - 2.4f),
                        center + new Vector3(hx - 0.6f, 0f, hz - 2.4f)
                    };
                    for (int i = 0; i < cSpots.Length; i++)
                    {
                        ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", props,
                            cSpots[i], Quaternion.Euler(0f, i * 77f, 0f), Vector3.one * 1.32f, cyanCrys, addBoxCollider: true, castShadows: false);
                        if (Application.isPlaying)
                        {
                            var vfx = VFXManager.EnsureInstance();
                            if (vfx != null)
                                vfx.SpawnLoopingVFX(VFXType.Env_CrystalGlow, props, cSpots[i] + Vector3.up * 1.25f, Quaternion.identity, new Color(0.32f, 0.88f, 1.0f, 1f), 1.1f);
                        }
                    }
                    break;

                case DungeonBiome.CursedForest:
                    Material barkMat = PbrMaterialLibrary.AgedOakWood();
                    Vector3[] rSpots =
                    {
                        center + new Vector3(-hx + 0.5f, 0f, -3.5f),
                        center + new Vector3(hx - 0.5f, 0f, -3.5f),
                        center + new Vector3(-hx + 0.5f, 0f, 4.2f),
                        center + new Vector3(hx - 0.5f, 0f, 4.2f)
                    };
                    for (int i = 0; i < rSpots.Length; i++)
                    {
                        ObjMeshLoader.SpawnPiece("CursedTreeRoot", props,
                            rSpots[i], Quaternion.Euler(0f, i % 2 == 0 ? 45f : -135f, 0f), Vector3.one * 1.18f, barkMat, addBoxCollider: false, castShadows: false);
                    }
                    break;

                case DungeonBiome.BossArena:
                    // Monumental Cathedral Sanctuary: Grand Steps + Boss Altar + Twin Fallen Angel Sentinels + Guardian Statues
                    Vector3 altarPos = center + new Vector3(0f, 0f, hz - 0.2f);
                    ObjMeshLoader.SpawnPiece("StoneStairs_Grand", props,
                        center + new Vector3(0f, 0f, hz - 3.6f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.55f, 0.55f, 0.90f), stoneMat, addBoxCollider: false, castShadows: false);
                    ObjMeshLoader.SpawnPiece("BossThroneAltar", props,
                        altarPos, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.22f, stoneMat, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                        center + new Vector3(-6.4f, 0f, hz - 0.8f), Quaternion.Euler(0f, 158f, 0f), Vector3.one * 1.32f, null, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", props,
                        center + new Vector3(6.4f, 0f, hz - 0.8f), Quaternion.Euler(0f, -158f, 0f), Vector3.one * 1.32f, null, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(-hx + 1.0f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.18f, stoneMat, addBoxCollider: true, castShadows: false);
                    ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", props,
                        center + new Vector3(hx - 1.0f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.18f, stoneMat, addBoxCollider: true, castShadows: false);
                    SpawnFireBrazier(props, center + new Vector3(-4.0f, 0f, hz - 1.6f), bData.torchLightColor, bData.torchCoreColor, true);
                    SpawnFireBrazier(props, center + new Vector3(4.0f, 0f, hz - 1.6f), bData.torchLightColor, bData.torchCoreColor, true);
                    EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(-2.4f, 0f, hz - 0.8f), Quaternion.Euler(0f, 20f, 0f), bData.torchCoreColor);
                    EnvironmentVfxFactory.SpawnCandleClusterProp(props, center + new Vector3(2.4f, 0f, hz - 0.8f), Quaternion.Euler(0f, -20f, 0f), bData.torchCoreColor);
                    if (Application.isPlaying)
                    {
                        var vfx = VFXManager.EnsureInstance();
                        if (vfx != null)
                        {
                            vfx.SpawnLoopingVFX(VFXType.Env_FloatingRunes, props, altarPos + Vector3.up * 1.1f, Quaternion.identity, CoolArcaneLight, 1.15f);
                            vfx.SpawnLoopingVFX(VFXType.Env_MagicalEnergy, props, altarPos + Vector3.up * 0.8f, Quaternion.identity, EnvironmentVfxFactory.ArcanePurple, 1.05f);
                        }
                    }
                    break;
            }
        }

        private static void BuildRoomTacticalCover(
            Transform parent,
            Vector3 center,
            float width,
            float length,
            Material stoneMat,
            RoomType roomType)
        {
            // Wide >= 4.2m flanking lanes around tactical cover so 3-4 co-op players + AI navigate smoothly with zero trapping
            float cx = Mathf.Clamp(width * 0.22f, 4.2f, 6.2f);
            float cz = Mathf.Clamp(length * 0.18f, 3.6f, 5.2f);

            if (roomType == RoomType.Combat)
            {
                ObjMeshLoader.SpawnPiece("StoneCoffin_GnG", parent,
                    center + new Vector3(-cx, 0f, -cz * 0.4f), Quaternion.Euler(0f, 15f, 0f),
                    Vector3.one * 0.96f, stoneMat, addBoxCollider: true, castShadows: false);
                ObjMeshLoader.SpawnPiece("WoodenBarrelsCrates_Gothic", parent,
                    center + new Vector3(cx, 0f, cz * 0.4f), Quaternion.Euler(0f, -25f, 0f),
                    Vector3.one * 0.95f, null, addBoxCollider: true, castShadows: false);
            }
            else if (roomType == RoomType.Elite || roomType == RoomType.MiniBoss)
            {
                // Twin symmetric low stone altar cover plinths flanking the center
                ObjMeshLoader.SpawnPiece("StoneCoffin_GnG", parent,
                    center + new Vector3(-cx, 0f, 0f), Quaternion.identity,
                    Vector3.one * 1.02f, stoneMat, addBoxCollider: true, castShadows: false);
                ObjMeshLoader.SpawnPiece("StoneCoffin_GnG", parent,
                    center + new Vector3(cx, 0f, 0f), Quaternion.identity,
                    Vector3.one * 1.02f, stoneMat, addBoxCollider: true, castShadows: false);
            }
        }

        public static GameObject SpawnWallTorch(
            Transform parent,
            Vector3 worldOrLocalPos,
            Quaternion localRot,
            Color outerColor,
            Color coreColor,
            bool withPointLight = true,
            float range = 15f,
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

                if (Application.isPlaying)
                {
                    var vfx = VFXManager.EnsureInstance();
                    if (vfx != null)
                    {
                        vfx.SpawnLoopingVFX(VFXType.Env_TorchFire, holder.transform, new Vector3(0f, 0.58f, 0.40f), Quaternion.identity, outerColor, 0.85f);
                        vfx.SpawnLoopingVFX(VFXType.Env_TorchSmoke, holder.transform, new Vector3(0f, 0.84f, 0.40f), Quaternion.identity, null, 0.85f);
                    }
                }
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
                PbrMaterialLibrary.WroughtIron(), addBoxCollider: true, castShadows: false);

            var flameGo = SpawnFlameVisual(holder.transform, new Vector3(0f, 1.18f, 0f), new Vector3(0.76f, 0.92f, 0.76f), outerColor, coreColor);

            if (Application.isPlaying)
            {
                var vfx = VFXManager.EnsureInstance();
                if (vfx != null)
                {
                    vfx.SpawnLoopingVFX(VFXType.Env_BrazierFire, holder.transform, new Vector3(0f, 1.15f, 0f), Quaternion.identity, outerColor, 0.92f);
                    vfx.SpawnLoopingVFX(VFXType.Env_BlackFireSmoke, holder.transform, new Vector3(0f, 1.55f, 0f), Quaternion.identity, null, 0.85f);
                    vfx.SpawnLoopingVFX(VFXType.Env_RisingEmbers, holder.transform, new Vector3(0f, 1.32f, 0f), Quaternion.identity, outerColor, 0.85f);
                }
            }

            if (withPointLight)
            {
                var lGo = new GameObject("BrazierPointLight");
                lGo.transform.SetParent(holder.transform, false);
                lGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                var pl = lGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = outerColor;
                pl.range = 15f;
                pl.intensity = 2.2f;
                pl.shadows = LightShadows.None;

                var fl = flameGo.AddComponent<TorchFlickerBehaviour>();
                fl.TargetLight = pl;
                fl.BaseIntensity = 2.2f;
                fl.FlickerAmplitude = 0.32f;
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
                float sz = 2.4f + (float)rnd.NextDouble() * 2.2f;
                float rot = (float)rnd.NextDouble() * 360f;

                var d = new GameObject("Decal_" + i);
                d.transform.SetParent(decalRoot, false);
                d.transform.position = new Vector3(center.x + rx, 0.015f + i * 0.001f, center.z + rz);
                d.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                d.transform.localScale = new Vector3(sz, 1f, sz);

                d.AddComponent<MeshFilter>().sharedMesh = quad;
                var mr = d.AddComponent<MeshRenderer>();
                mr.sharedMaterial = (i % 3 == 0) ? scorchMat : crackMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
        }

        private static void AttachRoomAmbientVfx(
            Transform root,
            Vector3 center,
            float width,
            float length,
            DungeonBiome biome,
            BiomeAtmosphereData bData)
        {
            if (!Application.isPlaying) return;
            var mgr = VFXManager.EnsureInstance();
            if (mgr == null) return;

            var vfxRoot = new GameObject("EnvironmentVfx").transform;
            vfxRoot.SetParent(root, false);

            float cornerX = width * 0.5f - 3.2f;
            float cornerZ = length * 0.5f - 3.2f;

            // Low-lying 3D ground mist wisps along outer aisles
            mgr.SpawnLoopingVFX(VFXType.Env_GroundFog, vfxRoot, center + new Vector3(-cornerX * 0.45f, 0.16f, 0f), Quaternion.identity, bData.mistColor, 1.0f);
            mgr.SpawnLoopingVFX(VFXType.Env_GroundFog, vfxRoot, center + new Vector3(cornerX * 0.45f, 0.16f, 0f), Quaternion.identity, bData.mistColor, 0.95f);

            // Corner aisle subtle ambient smoke wisps
            mgr.SpawnLoopingVFX(VFXType.Env_AmbientSmoke, vfxRoot, center + new Vector3(-cornerX, 0.35f, cornerZ), Quaternion.identity, bData.mistColor, 0.90f);
            mgr.SpawnLoopingVFX(VFXType.Env_AmbientSmoke, vfxRoot, center + new Vector3(cornerX, 0.35f, -cornerZ), Quaternion.identity, bData.mistColor, 0.90f);

            // Dust motes drifting through volumetric cathedral light rays + biome embers/runes
            switch (biome)
            {
                case DungeonBiome.ForgottenTemple:
                case DungeonBiome.CrystalChamber:
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingDust, vfxRoot, center + new Vector3(0f, 2.2f, 0f), Quaternion.identity, bData.particleColor, 1.05f);
                    break;

                case DungeonBiome.AncientDungeon:
                case DungeonBiome.CursedForest:
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingDust, vfxRoot, center + new Vector3(-2.5f, 1.9f, 0f), Quaternion.identity, bData.particleColor, 0.95f);
                    mgr.SpawnLoopingVFX(VFXType.Env_AshParticles, vfxRoot, center + new Vector3(2.5f, 2.1f, 0f), Quaternion.identity, null, 0.9f);
                    break;

                case DungeonBiome.UndergroundFortress:
                case DungeonBiome.BossArena:
                    mgr.SpawnLoopingVFX(VFXType.Env_AshParticles, vfxRoot, center + new Vector3(0f, 2.4f, 0f), Quaternion.identity, null, 1.15f);
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingEmbers, vfxRoot, center + new Vector3(0f, 1.5f, 0f), Quaternion.identity, bData.torchLightColor, 1.1f);
                    break;

                default: // AbyssalCave
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingDust, vfxRoot, center + new Vector3(0f, 1.9f, 0f), Quaternion.identity, CoolArcaneLight, 0.95f);
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingRunes, vfxRoot, center + new Vector3(0f, 1.2f, 0f), Quaternion.identity, CoolArcaneLight, 1.0f);
                    break;
            }
        }

        private static void BuildSideWallWithOptionalGate(
            Transform parent,
            float x,
            float centerZ,
            float roomLen,
            float wallH,
            float gapW,
            bool hasDoorway,
            float yRot,
            Material wallMat,
            Material frameMat)
        {
            float scaleY = wallH / 5.2f;
            float colOffset = yRot > 0f ? -0.3f : 0.3f;
            if (!hasDoorway)
            {
                AddWallCollider(parent, new Vector3(x + colOffset, wallH * 0.5f, centerZ), new Vector3(0.6f, wallH + 1f, roomLen + 0.8f));
                int n = Mathf.Max(1, Mathf.RoundToInt(roomLen / 4f));
                float segL = roomLen / n;
                float sz = centerZ - roomLen * 0.5f + segL * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    ObjMeshLoader.SpawnPiece("DungeonWall_Modular", parent,
                        new Vector3(x, 0f, sz + i * segL), Quaternion.Euler(0f, yRot, 0f),
                        new Vector3(segL / 4f, scaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);
                }
                return;
            }

            float sideL = (roomLen - gapW) * 0.5f;
            if (sideL > 0.4f)
            {
                AddWallCollider(parent, new Vector3(x + colOffset, wallH * 0.5f, centerZ - (gapW * 0.5f + sideL * 0.5f)), new Vector3(0.7f, wallH + 1f, sideL));
                AddWallCollider(parent, new Vector3(x + colOffset, wallH * 0.5f, centerZ + (gapW * 0.5f + sideL * 0.5f)), new Vector3(0.7f, wallH + 1f, sideL));

                int sidePanels = Mathf.Max(1, Mathf.RoundToInt(sideL / 4f));
                float panelL = sideL / sidePanels;
                for (int s = -1; s <= 1; s += 2)
                {
                    float baseEdge = s < 0 ? (centerZ - roomLen * 0.5f) : (centerZ + gapW * 0.5f);
                    for (int i = 0; i < sidePanels; i++)
                    {
                        float pz = baseEdge + (i + 0.5f) * panelL;
                        ObjMeshLoader.SpawnPiece("DungeonWall_Modular", parent,
                            new Vector3(x, 0f, pz), Quaternion.Euler(0f, yRot, 0f),
                            new Vector3(panelL / 4f, scaleY, 1f), wallMat, addBoxCollider: false, castShadows: false);
                    }
                }
            }

            ObjMeshLoader.SpawnPiece("GothicGateFrame", parent,
                new Vector3(x, 0f, centerZ), Quaternion.Euler(0f, yRot, 0f),
                new Vector3(gapW / 6.0f, scaleY, 1f), frameMat, addBoxCollider: false, castShadows: false);
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

            // Monumental Open Gothic Gate Frame around the doorway
            ObjMeshLoader.SpawnPiece("GothicGateFrame", parent,
                new Vector3(centerX, 0f, z), Quaternion.Euler(0f, yRot, 0f),
                new Vector3(gapW / 6.0f, scaleY, 1f), frameMat, addBoxCollider: false, castShadows: false);
        }

        public static void BuildGothicSanctuaryStage(Transform stageRoot, Vector3 focusCenter, bool wideForParty = false)
        {
            Color gloomyStoneTint = new Color(0.54f, 0.54f, 0.62f, 1f);
            Color gloomyFloorTint = new Color(0.58f, 0.58f, 0.66f, 1f);
            Material floorMat = PbrMaterialLibrary.CathedralFloor(gloomyFloorTint);
            Material wallMat = PbrMaterialLibrary.GothicStone(gloomyStoneTint);
            Material ironMat = PbrMaterialLibrary.WroughtIron();

            var floorRoot = new GameObject("SanctuaryStoneFloor").transform;
            floorRoot.SetParent(stageRoot, false);
            for (int ix = -2; ix <= 2; ix++)
            {
                for (int iz = -2; iz <= 2; iz++)
                {
                    Vector3 fPos = new Vector3(focusCenter.x + ix * 4f, 0f, focusCenter.z + iz * 4f);
                    ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", floorRoot,
                        fPos, Quaternion.identity, Vector3.one, floorMat, addBoxCollider: false, castShadows: false);
                }
            }

            float daisW = wideForParty ? 1.85f : 1.05f;
            ObjMeshLoader.SpawnPiece("FloorTile_GnG", stageRoot,
                new Vector3(focusCenter.x, 0.015f, focusCenter.z), Quaternion.identity,
                new Vector3(daisW, 1f, 1.05f), floorMat, addBoxCollider: false, castShadows: false);

            float backZ = focusCenter.z + 7.2f;
            for (int ix = -3; ix <= 3; ix++)
            {
                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", stageRoot,
                    new Vector3(focusCenter.x + ix * 4f, 0f, backZ), Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(1f, 1.25f, 1f), wallMat, addBoxCollider: false, castShadows: false);
            }
            for (int iz = -2; iz <= 2; iz++)
            {
                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", stageRoot,
                    new Vector3(focusCenter.x - 11.5f, 0f, focusCenter.z + iz * 4f), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(1f, 1.25f, 1f), wallMat, addBoxCollider: false, castShadows: false);
                ObjMeshLoader.SpawnPiece("DungeonWall_Modular", stageRoot,
                    new Vector3(focusCenter.x + 11.5f, 0f, focusCenter.z + iz * 4f), Quaternion.Euler(0f, -90f, 0f),
                    new Vector3(1f, 1.25f, 1f), wallMat, addBoxCollider: false, castShadows: false);
            }

            float[] apseAngles = { -58f, -34f, -13f, 13f, 34f, 58f };
            float radiusX = wideForParty ? 6.8f : 5.6f;
            float radiusZ = 5.2f;
            for (int i = 0; i < apseAngles.Length; i++)
            {
                float rad = apseAngles[i] * Mathf.Deg2Rad;
                Vector3 pPos = focusCenter + new Vector3(Mathf.Sin(rad) * radiusX, 0f, Mathf.Cos(rad) * radiusZ + 0.4f);
                ObjMeshLoader.SpawnPiece("GothicPillar", stageRoot,
                    pPos, Quaternion.Euler(0f, apseAngles[i], 0f), new Vector3(1.05f, 1.20f, 1.05f),
                    wallMat, addBoxCollider: false, castShadows: false);

                if (i == 1 || i == 4)
                {
                    SpawnWallTorch(stageRoot, pPos + new Vector3(0f, 2.55f, -0.45f), Quaternion.Euler(0f, 180f, 0f),
                        new Color(1.0f, 0.56f, 0.20f), new Color(1.0f, 0.88f, 0.52f), withPointLight: false);
                }
            }

            ObjMeshLoader.SpawnPiece("GothicArchVault", stageRoot,
                focusCenter + new Vector3(0f, 0f, 5.8f), Quaternion.Euler(0f, 180f, 0f),
                new Vector3(1.25f, 1.18f, 1.1f), wallMat, addBoxCollider: false, castShadows: false);
            ObjMeshLoader.SpawnPiece("GothicChandelier_GnG", stageRoot,
                focusCenter + new Vector3(0f, 4.4f, 1.8f), Quaternion.identity,
                Vector3.one * 0.95f, ironMat, addBoxCollider: false, castShadows: false);

            float statX = wideForParty ? 4.4f : 3.1f;
            ObjMeshLoader.SpawnPiece("FallenAngelStatue_Gothic", stageRoot,
                focusCenter + new Vector3(-statX, 0f, 4.1f), Quaternion.Euler(0f, 158f, 0f),
                Vector3.one * 1.02f, null, addBoxCollider: false, castShadows: false);
            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", stageRoot,
                focusCenter + new Vector3(statX, 0f, 4.1f), Quaternion.Euler(0f, -158f, 0f),
                Vector3.one * 1.08f, wallMat, addBoxCollider: false, castShadows: false);

            ObjMeshLoader.SpawnPiece("StoneCoffin_GnG", stageRoot,
                focusCenter + new Vector3(-statX - 1.6f, 0f, 2.4f), Quaternion.Euler(0f, 68f, 0f),
                Vector3.one * 0.95f, wallMat, addBoxCollider: false, castShadows: false);
            ObjMeshLoader.SpawnPiece("RitualTable_GnG", stageRoot,
                focusCenter + new Vector3(statX + 1.6f, 0f, 2.5f), Quaternion.Euler(0f, -68f, 0f),
                Vector3.one * 0.95f, PbrMaterialLibrary.AgedOakWood(), addBoxCollider: false, castShadows: false);

            float brazX = wideForParty ? 3.5f : 2.25f;
            SpawnFireBrazier(stageRoot, focusCenter + new Vector3(-brazX, 0f, 2.1f),
                new Color(1.0f, 0.54f, 0.18f), new Color(1.0f, 0.88f, 0.48f), withPointLight: true);
            SpawnFireBrazier(stageRoot, focusCenter + new Vector3(brazX, 0f, 2.1f),
                new Color(1.0f, 0.54f, 0.18f), new Color(1.0f, 0.88f, 0.48f), withPointLight: false);

            ObjMeshLoader.SpawnPiece("CandleGroup_GnG", stageRoot,
                focusCenter + new Vector3(-brazX + 0.65f, 0f, 1.55f), Quaternion.Euler(0f, 22f, 0f),
                Vector3.one, wallMat, addBoxCollider: false, castShadows: false);
            ObjMeshLoader.SpawnPiece("CandleGroup_GnG", stageRoot,
                focusCenter + new Vector3(brazX - 0.65f, 0f, 1.55f), Quaternion.Euler(0f, -24f, 0f),
                Vector3.one, wallMat, addBoxCollider: false, castShadows: false);

            SpawnVolumetricShaft(stageRoot, focusCenter + new Vector3(-1.4f, 0.02f, 2.6f), Quaternion.Euler(0f, 20f, 0f),
                new Vector3(0.92f, 0.95f, 0.92f), PbrMaterialLibrary.LightShaft(WarmShaftTint, 1.0f));

            if (Application.isPlaying)
            {
                var mgr = VFXManager.EnsureInstance();
                if (mgr != null)
                {
                    Color darkMist = new Color(0.16f, 0.18f, 0.28f, 0.42f);
                    mgr.SpawnLoopingVFX(VFXType.Env_GroundFog, stageRoot, focusCenter + new Vector3(-2.6f, 0.12f, 2.2f), Quaternion.identity, darkMist, 0.85f);
                    mgr.SpawnLoopingVFX(VFXType.Env_GroundFog, stageRoot, focusCenter + new Vector3(2.6f, 0.12f, 2.2f), Quaternion.identity, darkMist, 0.85f);
                    mgr.SpawnLoopingVFX(VFXType.Env_FloatingEmbers, stageRoot, focusCenter + new Vector3(0f, 1.3f, 1.2f), Quaternion.identity, new Color(1f, 0.62f, 0.22f, 0.9f), 0.85f);
                }
            }
        }

        public static GameObject BuildWoodenPlankDeck(Transform parent, string name, Vector3 centerPos, float width, float length)
        {
            var deck = new GameObject("WoodFloor_" + name);
            deck.transform.SetParent(parent, false);
            deck.transform.position = new Vector3(centerPos.x, 0f, centerPos.z);

            var col = deck.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.099f, 0f);
            col.size = new Vector3(width, 0.20f, length);

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

        public static GameObject BuildIronGrateDeck(Transform parent, string name, Vector3 centerPos, float width, float length)
        {
            var deck = new GameObject("MetalFloor_" + name);
            deck.transform.SetParent(parent, false);
            deck.transform.position = new Vector3(centerPos.x, 0f, centerPos.z);

            var col = deck.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.099f, 0f);
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
