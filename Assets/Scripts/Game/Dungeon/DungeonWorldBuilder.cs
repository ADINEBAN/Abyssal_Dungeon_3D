using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    [System.Serializable]
    public class DungeonRoomRuntime
    {
        public int index;
        public int gridX;
        public int gridY;
        public RoomType type;
        public DungeonBiome biome;
        public string title;
        public string subTypeLabel;
        public bool isSanctuary;
        public Vector3 center;
        public float width;
        public float length;
        public bool activated;
        public bool cleared;
        public bool visited;
        public bool discovered;
        public int wave1InitialCount;
        public bool hasReinforcementWave;
        public bool wave2Spawned;
        public bool openNorth;
        public bool openSouth;
        public bool openWest;
        public bool openEast;
        public Transform roomRoot;
        public Transform monstersRoot;
        public DungeonGate entryGate;
        public DungeonGate exitGate;
        public WorldTreasureChest rewardChest;
        public List<int> connectedRoomIndices = new List<int>();
        public List<DungeonGate> roomGates = new List<DungeonGate>();
        public List<ActorUnit> activeMonsters = new List<ActorUnit>();
        public List<WorldRunePillar> puzzlePillars = new List<WorldRunePillar>();
    }

    /// <summary>
    /// Builds the 7-room 3D Abyssal Dungeon, spawns the 3-4 Hunter party, runs room encounters,
    /// Puzzle room, Traps, Chests, Healing Shrines, Loot Orbs, Abyss Director adjustments, and Boss completion.
    /// </summary>
    public class DungeonWorldBuilder : MonoBehaviour
    {
        public static DungeonWorldBuilder Instance { get; private set; }

        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private AbyssDirectorAI _director;
        public List<DungeonRoomRuntime> Rooms = new List<DungeonRoomRuntime>();
        public List<ActorUnit> PartyUnits = new List<ActorUnit>();
        [SerializeField] private ActorUnit _localHero;
        public ActorUnit LocalHero
        {
            get => _localHero;
            private set => _localHero = value;
        }
        public bool IsPrebakedInScene => _isPrebakedInScene;
        private bool _runtimeInitialized;

        public int CurrentRoomIndex = 0;
        public string CurrentObjective = "Tiến vào Phó Bản Vực Thẳm";
        public string BannerMessage = "";
        public float BannerTimer = 0f;
        public bool RunFinished = false;
        public bool SoulAnchorRespawnAvailable = true;

        // Tracked run rewards
        public int RunGoldGained = 0;
        public int RunExpGained = 0;
        public int RunStartLevel = 1;
        public readonly List<EquipItem> RunLootItems = new List<EquipItem>();

        public event Action<string> OnLootNotification;

        private Material _floorMat, _wallMat, _pillarMat, _trimMat;

        private void Awake()
        {
            Instance = this;
            if (!_isPrebakedInScene)
                HeroPlayerInput.Interactables.Clear();
            GroundTelegraphFx.ActiveHazards.Clear();
        }

        private void Start()
        {
            if (_isPrebakedInScene && !_runtimeInitialized)
                InitializePrebaked();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void InitializePrebaked()
        {
            if (_runtimeInitialized) return;
            _runtimeInitialized = true;
            Instance = this;

            GameSession.I.EnsureDefaultParty();
            RunStartLevel = GameSession.I.Profile.level;
            SoulAnchorRespawnAvailable = true;
            CombatFeelManager.I.ResetRunCombo();

            if (_director == null)
                _director = GetComponentInChildren<AbyssDirectorAI>();
            if (_director == null)
            {
                var dirGo = new GameObject("AbyssDirectorAI");
                dirGo.transform.SetParent(transform, false);
                _director = dirGo.AddComponent<AbyssDirectorAI>();
            }
            _director.AnalyzePartyComposition();
            _director.OnDirectorAdjusted += msg => ShowBanner(msg, 4.5f);

            SetupEnvironmentLighting();
            CreateDungeonMaterials();

            // 1. Synchronize pre-baked Hero_0..3 with player's chosen Class/Build/Party from ClassSelection & Lobby
            SyncPrebakedHeroParty();

            // 2. Synchronize pre-baked Monsters, Boss, and Props with selectedFloor & set unentered rooms dormant
            int floor = GameSession.I.Profile.selectedFloor;
            float hpM = _director != null ? _director.HpMultiplier : 1f;
            float atkM = _director != null ? _director.AtkMultiplier : 1f;

            for (int rIdx = 0; rIdx < Rooms.Count; rIdx++)
            {
                var rm = Rooms[rIdx];
                rm.activated = false;
                rm.cleared = false;
                rm.visited = false;
                rm.discovered = false;
                rm.wave1InitialCount = 0;
                rm.hasReinforcementWave = false;
                rm.wave2Spawned = false;

                if (rm.puzzlePillars != null)
                {
                    for (int p = 0; p < rm.puzzlePillars.Count; p++)
                    {
                        if (rm.puzzlePillars[p] != null)
                            rm.puzzlePillars[p].BindCallback(OnPuzzleRuneActivated);
                    }
                }

                if (rm.activeMonsters != null)
                {
                    for (int m = rm.activeMonsters.Count - 1; m >= 0; m--)
                    {
                        ActorUnit mon = rm.activeMonsters[m];
                        if (mon == null)
                        {
                            rm.activeMonsters.RemoveAt(m);
                            continue;
                        }
                        mon.ReinitializePrebakedMonster(floor, hpM, atkM);
                        mon.OnKilled -= HandleMonsterKilled;
                        mon.OnKilled += HandleMonsterKilled;
                        mon.SetRoomDormant(true);
                    }
                }
            }

            RevealFogOfWarForRoom(0);
            ActivateRoom(0);
            ShowBanner($"TẦNG {floor}: ABYSSAL DUNGEON — {_director.CompositionTag}", 4f);
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.Dungeon);
        }

        private void SyncPrebakedHeroParty()
        {
            Vector3 startCenter = Rooms.Count > 0 ? Rooms[0].center + new Vector3(0f, 0f, -1.5f) : new Vector3(0f, 0f, -1.5f);
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(-2.4f, 0f, -1.6f),
                new Vector3(2.4f, 0f, -1.6f),
                new Vector3(0f, 0f, -2.8f)
            };

            int desiredLocalSlot = (LanRoomBridge.HasInstance && LanRoomBridge.I.IsConnectedClient)
                ? Mathf.Clamp(LanRoomBridge.I.LocalSlotIndex, 0, 3)
                : 0;
            if (GameSession.I.Party[desiredLocalSlot] == null || !GameSession.I.Party[desiredLocalSlot].active)
                desiredLocalSlot = 0;

            var activeHeroes = new List<ActorUnit>(4);
            int spawned = 0;

            for (int i = 0; i < 4; i++)
            {
                var slot = GameSession.I.Party[i];
                ActorUnit unit = (i < PartyUnits.Count) ? PartyUnits[i] : null;

                if (slot == null || !slot.active)
                {
                    if (unit != null) unit.gameObject.SetActive(false);
                    continue;
                }

                if (unit == null)
                {
                    var heroGo = new GameObject($"Hero_{i}_{slot.playerName}");
                    unit = heroGo.AddComponent<ActorUnit>();
                }

                unit.gameObject.SetActive(true);
                unit.gameObject.name = $"Hero_{i}_{slot.playerName}";
                unit.SyncPhysicsPosition(startCenter + offsets[Mathf.Clamp(spawned, 0, 3)]);

                bool isLocal = (i == desiredLocalSlot);
                unit.InitHero(slot.classId, slot.buildId, GameSession.I.Profile.level, slot.playerName, isLocal, i);

                var oldInput = unit.GetComponent<HeroPlayerInput>();
                var oldAi = unit.GetComponent<HeroPartyAI>();
                var oldNet = unit.GetComponent<RemoteNetInterpolator>();

                if (isLocal)
                {
                    LocalHero = unit;
                    if (oldAi != null) Destroy(oldAi);
                    if (oldNet != null) Destroy(oldNet);
                    if (oldInput == null) unit.gameObject.AddComponent<HeroPlayerInput>();

                    if (Camera.main != null)
                    {
                        var cam = Camera.main.GetComponent<OrbitCamera3D>();
                        if (cam == null) cam = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                        cam.Target = unit.transform;
                    }
                }
                else
                {
                    if (oldInput != null) Destroy(oldInput);
                    bool isRemoteNetworkControlled =
                        LanRoomBridge.HasInstance &&
                        (LanRoomBridge.I.IsConnectedClient ||
                         (LanRoomBridge.I.IsHosting && slot.isHuman && LanRoomBridge.I.ConnectedClientCount > 0));

                    if (isRemoteNetworkControlled)
                    {
                        if (oldAi != null) Destroy(oldAi);
                        if (oldNet == null) unit.gameObject.AddComponent<RemoteNetInterpolator>();
                    }
                    else
                    {
                        if (oldNet != null) Destroy(oldNet);
                        if (oldAi == null) unit.gameObject.AddComponent<HeroPartyAI>();
                    }
                }

                activeHeroes.Add(unit);
                spawned++;
            }

            PartyUnits.Clear();
            PartyUnits.AddRange(activeHeroes);
        }

        public void BuildAndStartDungeon()
        {
            Instance = this;
            GameSession.I.EnsureDefaultParty();
            RunStartLevel = GameSession.I.Profile.level;
            SoulAnchorRespawnAvailable = true;
            if (Application.isPlaying)
                CombatFeelManager.I.ResetRunCombo();

            // Setup Abyss Director
            var dirGo = new GameObject("AbyssDirectorAI");
            dirGo.transform.SetParent(transform, false);
            var director = dirGo.AddComponent<AbyssDirectorAI>();
            _director = director;
            director.AnalyzePartyComposition();
            director.OnDirectorAdjusted += msg => ShowBanner(msg, 4.5f);

            SetupEnvironmentLighting();
            CreateDungeonMaterials();
            ConstructRoomsGeometry();
            SpawnHeroParty();

            // Activate Entrance Room immediately
            ActivateRoom(0);
            ShowBanner($"TẦNG {GameSession.I.Profile.selectedFloor}: ABYSSAL DUNGEON — {director.CompositionTag}", 4f);
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.Dungeon);

            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
        }

        private void SetupEnvironmentLighting()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            DungeonBiome startBiome = (Rooms != null && Rooms.Count > 0) ? Rooms[0].biome : DungeonBiome.ForgottenTemple;
            atmo.ApplyBiome(startBiome, instant: true);
        }

        private void CreateDungeonMaterials()
        {
            _floorMat = PbrMaterialLibrary.CathedralFloor();
            _wallMat = PbrMaterialLibrary.GothicStone();
            _pillarMat = PbrMaterialLibrary.GothicStone(new Color(0.76f, 0.72f, 0.68f));
            _trimMat = PbrMaterialLibrary.RunicGold();
        }

        private void ConstructRoomsGeometry()
        {
            int floor = GameSession.I.Profile.selectedFloor;
            int seedOffset = GameSession.I.Profile.highestFloor + floor * 17;
            var specs = ProceduralDungeonGenerator.GenerateSeededDungeon(floor, seedOffset, out _);

            const float pitch = ProceduralDungeonGenerator.GridCellPitch; // 34f
            const float corridorLen = 10f; // 34f - 24f = 10f exact N/S and E/W bridge
            const float corridorW = 6f;

            for (int i = 0; i < specs.Length; i++)
            {
                var sp = specs[i];
                float w = sp.width;
                float len = sp.length;

                // Boss room is 32x32 at gridY=4; shift Z by +4m so its South wall (124m) is 10m North of (0,3)'s North wall (114m)
                float extraZ = sp.type == RoomType.Boss ? (len - 24f) * 0.5f : 0f;
                Vector3 center = new Vector3(sp.gridX * pitch, 0f, sp.gridY * pitch + extraZ);
                DungeonBiome biome = sp.biome;

                var room = new DungeonRoomRuntime
                {
                    index = i,
                    gridX = sp.gridX,
                    gridY = sp.gridY,
                    type = sp.type,
                    biome = biome,
                    title = !string.IsNullOrEmpty(sp.customTitle)
                        ? sp.customTitle
                        : $"Khu {i + 1}/{specs.Length}: {GameDB.RoomName(sp.type)}",
                    subTypeLabel = !string.IsNullOrEmpty(sp.subTypeLabel) ? sp.subTypeLabel : GameDB.RoomName(sp.type),
                    isSanctuary = sp.isSanctuary,
                    center = center,
                    width = w,
                    length = len,
                    openNorth = sp.openNorth,
                    openSouth = sp.openSouth,
                    openWest = sp.openWest,
                    openEast = sp.openEast
                };
                if (sp.connectedIndices != null)
                    room.connectedRoomIndices.AddRange(sp.connectedIndices);

                Rooms.Add(room);

                var rRoot = new GameObject($"Room_{i}_G({sp.gridX},{sp.gridY})_{sp.type}_{biome}");
                rRoot.transform.SetParent(transform, false);
                room.roomRoot = rRoot.transform;

                var mRoot = new GameObject("RoomMonsters");
                mRoot.transform.SetParent(rRoot.transform, false);
                room.monstersRoot = mRoot.transform;

                float wallH = sp.type == RoomType.Boss ? 6.6f : 5.2f;

                // Modular 3D Gothic Architecture with N/S/E/W branching doorways
                ModularDungeonArchitect.BuildModularRoomShell(
                    rRoot.transform, center, w, len, wallH, biome,
                    openSouthDoor: sp.openSouth, openNorthDoor: sp.openNorth,
                    doorwayWidth: corridorW,
                    openWestDoor: sp.openWest, openEastDoor: sp.openEast);

                // Biome-specific 3D Prop Storytelling
                ModularDungeonArchitect.DressBiomeEnvironmentProps(rRoot.transform, center, w, len, biome);

                if (sp.openSouth)
                {
                    var g = DungeonGate.Create(rRoot.transform, new Vector3(center.x, 0f, center.z - len * 0.5f), corridorW, eastWest: false);
                    room.entryGate = g;
                    room.roomGates.Add(g);
                }

                if (sp.openNorth)
                {
                    float northZ = center.z + len * 0.5f;
                    var g = DungeonGate.Create(rRoot.transform, new Vector3(center.x, 0f, northZ), corridorW, eastWest: false);
                    room.exitGate = g;
                    room.roomGates.Add(g);
                    ModularDungeonArchitect.BuildModularCorridor(rRoot.transform, northZ, corridorLen, corridorW, wallH, biome, center.x);
                }

                if (sp.openWest)
                {
                    float westX = center.x - w * 0.5f;
                    var g = DungeonGate.Create(rRoot.transform, new Vector3(westX, 0f, center.z), corridorW, eastWest: true);
                    if (room.entryGate == null) room.entryGate = g;
                    room.roomGates.Add(g);
                }

                if (sp.openEast)
                {
                    float eastX = center.x + w * 0.5f;
                    var g = DungeonGate.Create(rRoot.transform, new Vector3(eastX, 0f, center.z), corridorW, eastWest: true);
                    if (room.exitGate == null) room.exitGate = g;
                    room.roomGates.Add(g);
                    ModularDungeonArchitect.BuildModularCorridorEW(rRoot.transform, eastX, corridorLen, corridorW, wallH, center.z, biome);
                }

                DecorateRoom(room, rRoot.transform, wallH);

                // Pre-populate room monsters & Boss as Prefab Instances in Edit Mode (or initial build)
                if (room.type != RoomType.Entrance && !room.isSanctuary)
                {
                    SpawnEncounterForRoom(room, dormantForUnactivatedRoom: true);
                }
            }

            // Initialize Fog of War from Entrance (0)
            RevealFogOfWarForRoom(0);
        }

        public void RevealFogOfWarForRoom(int roomIdx)
        {
            if (roomIdx < 0 || roomIdx >= Rooms.Count) return;
            var rm = Rooms[roomIdx];
            rm.visited = true;
            rm.discovered = true;
            for (int k = 0; k < rm.connectedRoomIndices.Count; k++)
            {
                int nb = rm.connectedRoomIndices[k];
                if (nb >= 0 && nb < Rooms.Count)
                    Rooms[nb].discovered = true;
            }
        }

        private void DecorateRoom(DungeonRoomRuntime room, Transform root, float wallH)
        {
            Vector3 c = room.center;
            float hl = room.length * 0.5f - 2.4f;

            if (room.index == 0)
            {
                // Entry abyssal portal behind party + starter Healing Shrine + Chapter 1 Lore Monolith
                BuildDecorativePortal(root, c + new Vector3(0f, 0f, -6.2f), new Color(0.65f, 0.25f, 1f));
                WorldHealingShrine.Create(root, c + new Vector3(5.2f, 0f, 1.5f));
                WorldLoreMonolith.Create(root, c + new Vector3(-5.2f, 0f, 1.8f), 0);
                return;
            }

            if (room.isSanctuary)
            {
                // Side-branch Healing Shrine Sanctuary (Room 5) + Chapter 2 Lore Monolith
                WorldHealingShrine.Create(root, c + new Vector3(-3.8f, 0f, 1.5f));
                WorldHealingShrine.Create(root, c + new Vector3(3.8f, 0f, 1.5f));
                WorldLoreMonolith.Create(root, c + new Vector3(0f, 0f, -3.5f), 1);
                room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(0f, 0f, 4.2f), 2);
                room.rewardChest.UnlockChest();
                return;
            }

            switch (room.type)
            {
                case RoomType.Combat:
                    WorldDungeonTrap.Create(root, c + new Vector3(-3.8f, 0f, -2.2f), false);
                    WorldDungeonTrap.Create(root, c + new Vector3(3.8f, 0f, 2.2f), room.index >= 4);
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(0f, 0f, hl - 1.2f), room.index >= 2 ? 2 : 1);
                    if (room.index == 2 || room.index == 6)
                    {
                        // Side-branch Choice Altar (Tế Đàn Lựa Chọn) + Bonus Treasure Vault Chest
                        string altarTitle = room.index == 2 ? "Tế Đàn Lựa Chọn Nguyền Rủa" : "Tế Đàn Phong Ấn Huyết Tinh";
                        WorldChoiceAltar.Create(root, c + new Vector3(5.6f, 0f, -2.8f), altarTitle);
                        var bonusChest = WorldTreasureChest.Create(root, c + new Vector3(-4.2f, 0f, hl - 1.4f), room.index == 6 ? 3 : 2);
                        bonusChest.UnlockChest();
                    }
                    else
                    {
                        WorldEnvironmentalEvent.Create(root, c + new Vector3(5.8f, 0f, -3.2f), "Tế Đàn Huyết Nguyệt", room.index);
                    }
                    break;

                case RoomType.Elite:
                    WorldDungeonTrap.Create(root, c + new Vector3(0f, 0f, -2.5f), true);
                    WorldEnvironmentalEvent.Create(root, c + new Vector3(-6.4f, 0f, -2.8f), "Lõi Năng Lượng Cổ Đại", room.index);
                    WorldLoreMonolith.Create(root, c + new Vector3(6.2f, 0f, -2.8f), 2);
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(4.5f, 0f, hl - 1.4f), 2);
                    break;

                case RoomType.Puzzle:
                    // 3 Rune Pillars that must be activated + Spike Trap at center
                    Vector3[] runePos =
                    {
                        c + new Vector3(-6f, 0f, -3.2f),
                        c + new Vector3(6f, 0f, -3.2f),
                        c + new Vector3(0f, 0f, 4.8f)
                    };
                    for (int r = 0; r < 3; r++)
                    {
                        var pillar = WorldRunePillar.Create(root, runePos[r], r + 1, OnPuzzleRuneActivated);
                        room.puzzlePillars.Add(pillar);
                    }
                    WorldDungeonTrap.Create(root, c, false);
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(0f, 0f, hl - 1.2f), 3);
                    break;

                case RoomType.MiniBoss:
                    WorldHealingShrine.Create(root, c + new Vector3(6.5f, 0f, hl - 1.5f));
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(-6.5f, 0f, hl - 1.5f), 3);
                    break;

                case RoomType.Boss:
                    // Grand arcane summoning sigil on cathedral floor
                    CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "BossSigil", root,
                        c + Vector3.up * 0.02f, new Vector3(9.5f, 0.02f, 9.5f),
                        CharacterVisualFactory.MakeTransparentMat(new Color(0.75f, 0.15f, 0.95f, 0.30f), new Color(0.75f, 0.15f, 0.95f)));
                    break;
            }
        }

        private void SpawnTorch(Transform parent, Vector3 pos, Color col)
        {
            var t = new GameObject("Torch");
            t.transform.SetParent(parent, false);
            t.transform.position = pos;
            var flameMat = CharacterVisualFactory.MakeMat(col, 0.9f, 0f, col * 3f);
            CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Flame", t.transform, Vector3.zero, new Vector3(0.26f, 0.36f, 0.26f), flameMat);
            var light = t.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = col;
            light.range = 16f;
            light.intensity = 2.1f;
            light.shadows = LightShadows.None;
        }

        private void BuildDecorativePortal(Transform parent, Vector3 pos, Color col)
        {
            var p = new GameObject("AbyssPortalDecor");
            p.transform.SetParent(parent, false);
            p.transform.position = pos;
            ObjMeshLoader.SpawnPiece("AbyssalPortal", p.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.95f, PbrMaterialLibrary.GothicStone());
            ObjMeshLoader.SpawnPiece("EscapeFloorPortal", p.transform, new Vector3(0f, 0.02f, 0f), Quaternion.identity, Vector3.one * 0.9f,
                PbrMaterialLibrary.AbyssalCrystal(col * 0.8f, col * 1.4f, 2.4f), addBoxCollider: false);
            var pl = p.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = col;
            pl.range = 10f;
            pl.intensity = 2.0f;
            pl.shadows = LightShadows.None;
        }

        private void SpawnHeroParty()
        {
            Vector3 startCenter = Rooms[0].center + new Vector3(0f, 0f, -1.5f);
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(-2.4f, 0f, -1.6f),
                new Vector3(2.4f, 0f, -1.6f),
                new Vector3(0f, 0f, -2.8f)
            };

            int desiredLocalSlot = (LanRoomBridge.HasInstance && LanRoomBridge.I.IsConnectedClient)
                ? Mathf.Clamp(LanRoomBridge.I.LocalSlotIndex, 0, 3)
                : 0;
            if (GameSession.I.Party[desiredLocalSlot] == null || !GameSession.I.Party[desiredLocalSlot].active)
                desiredLocalSlot = 0;

            int spawned = 0;
            for (int i = 0; i < 4; i++)
            {
                int slotIdx = i;
                var slot = GameSession.I.Party[i];
                if (slot == null || !slot.active) continue;

                bool isLocal = (i == desiredLocalSlot);
                Vector3 spawnPos = startCenter + offsets[Mathf.Clamp(spawned, 0, 3)];
                string prefabName = $"PF_Hero_Slot{slotIdx}_{GameDB.GetClass(slot.classId).name}";

                var unit = BakedAssetRegistry.InstantiateOrBuildPrefab<ActorUnit>(
                    BakedAssetRegistry.HeroesPrefabFolder,
                    prefabName,
                    null,
                    spawnPos,
                    Quaternion.identity,
                    () =>
                    {
                        var heroGo = new GameObject(prefabName);
                        var u = heroGo.AddComponent<ActorUnit>();
                        u.InitHero(slot.classId, slot.buildId, GameSession.I.Profile.level, slot.playerName, isLocal, slotIdx);
                        if (isLocal)
                            heroGo.AddComponent<HeroPlayerInput>();
                        else
                            heroGo.AddComponent<HeroPartyAI>();
                        return heroGo;
                    },
                    u =>
                    {
                        if (u == null) return;
                        u.gameObject.name = $"Hero_{slotIdx}_{slot.playerName}";
                        u.IsLocalPlayer = isLocal;
                        u.PartySlotIndex = slotIdx;
                        u.DisplayName = slot.playerName;
                    });

                if (isLocal)
                {
                    LocalHero = unit;
                    if (unit.GetComponent<HeroPlayerInput>() == null)
                        unit.gameObject.AddComponent<HeroPlayerInput>();
                    if (Camera.main != null)
                    {
                        var cam = Camera.main.GetComponent<OrbitCamera3D>();
                        if (cam == null) cam = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                        cam.Target = unit.transform;
                    }
                }
                else
                {
                    bool isRemoteNetworkControlled =
                        LanRoomBridge.HasInstance &&
                        (LanRoomBridge.I.IsConnectedClient ||
                         (LanRoomBridge.I.IsHosting && slot.isHuman && LanRoomBridge.I.ConnectedClientCount > 0));

                    if (isRemoteNetworkControlled)
                    {
                        var oldAi = unit.GetComponent<HeroPartyAI>();
                        if (oldAi != null) BakedAssetRegistry.DestroySafe(oldAi);
                        if (unit.GetComponent<RemoteNetInterpolator>() == null)
                            unit.gameObject.AddComponent<RemoteNetInterpolator>();
                    }
                    else
                    {
                        if (unit.GetComponent<HeroPartyAI>() == null)
                            unit.gameObject.AddComponent<HeroPartyAI>();
                    }
                }

                PartyUnits.Add(unit);
                spawned++;
            }
        }

        private void Update()
        {
            if (BannerTimer > 0f) BannerTimer -= Time.deltaTime;
            if (RunFinished) return;

            // Check total party wipe
            bool anyAliveHero = false;
            foreach (var h in PartyUnits)
                if (h != null && !h.IsDead && !h.IsDowned) anyAliveHero = true;

            if (!anyAliveHero && PartyUnits.Count > 0)
            {
                if (SoulAnchorRespawnAvailable)
                {
                    TriggerSoulAnchorPartyRespawn();
                    return;
                }
                CompleteRun(false);
                return;
            }

            // Detect player entering any room in the 2D Branching Dungeon Graph
            if (LocalHero != null && !LocalHero.IsDead)
            {
                float hx = LocalHero.transform.position.x;
                float hz = LocalHero.transform.position.z;
                for (int i = 0; i < Rooms.Count; i++)
                {
                    var rm = Rooms[i];
                    float minX = rm.center.x - rm.width * 0.5f + 1.1f;
                    float maxX = rm.center.x + rm.width * 0.5f - 1.1f;
                    float minZ = rm.center.z - rm.length * 0.5f + 1.1f;
                    float maxZ = rm.center.z + rm.length * 0.5f - 1.1f;
                    if (hx >= minX && hx <= maxX && hz >= minZ && hz <= maxZ)
                    {
                        if (CurrentRoomIndex != i)
                        {
                            CurrentRoomIndex = i;
                            RevealFogOfWarForRoom(i);
                            if (BiomeAtmosphereController.Instance != null)
                                BiomeAtmosphereController.Instance.ApplyBiome(rm.biome, instant: false);
                        }
                        if (!rm.activated) ActivateRoom(i);
                        break;
                    }
                }
            }

            // Check active room completion
            if (CurrentRoomIndex >= 0 && CurrentRoomIndex < Rooms.Count)
            {
                var cur = Rooms[CurrentRoomIndex];
                if (cur.activated && !cur.cleared)
                {
                    cur.activeMonsters.RemoveAll(m => m == null || m.IsDead);

                    // Wave 2 Reinforcement Trigger: when 50% of Wave 1 has been defeated!
                    if (cur.hasReinforcementWave && !cur.wave2Spawned && cur.wave1InitialCount > 0)
                    {
                        int triggerThreshold = Mathf.FloorToInt(cur.wave1InitialCount * 0.5f);
                        if (cur.activeMonsters.Count <= triggerThreshold)
                        {
                            SpawnWave2Reinforcements(cur);
                        }
                    }

                    if (cur.type == RoomType.Puzzle)
                    {
                        int lit = 0;
                        foreach (var p in cur.puzzlePillars) if (p.Activated) lit++;
                        CurrentObjective = $"{cur.title} — Kích hoạt Ấn Phong Cổ ({lit}/3) [Phím F]";
                        if (lit >= 3 && cur.activeMonsters.Count == 0)
                            MarkRoomCleared(cur);
                    }
                    else if (cur.type == RoomType.Boss)
                    {
                        CurrentObjective = "ĐÁNH BẠI BOSS CUỐI: ABYSSAL KNIGHT!";
                        if (cur.activeMonsters.Count == 0)
                            MarkRoomCleared(cur);
                    }
                    else
                    {
                        string waveTag = cur.hasReinforcementWave ? (cur.wave2Spawned ? " [Đợt 2/2]" : " [Đợt 1/2]") : "";
                        CurrentObjective = $"{cur.title}{waveTag} — Tiêu diệt quái còn lại: {cur.activeMonsters.Count}";
                        if (cur.activeMonsters.Count == 0 && (!cur.hasReinforcementWave || cur.wave2Spawned))
                            MarkRoomCleared(cur);
                    }
                }
            }
        }

        private void SetAllRoomGates(DungeonRoomRuntime room, bool open)
        {
            if (room == null) return;
            for (int i = 0; i < room.roomGates.Count; i++)
            {
                if (room.roomGates[i] != null) room.roomGates[i].SetOpen(open);
            }
            if (room.entryGate != null) room.entryGate.SetOpen(open);
            if (room.exitGate != null) room.exitGate.SetOpen(open);
        }

        private void ActivateRoom(int idx)
        {
            var room = Rooms[idx];
            if (room.activated) return;
            room.activated = true;
            RevealFogOfWarForRoom(idx);

            if (room.type == RoomType.Entrance || room.isSanctuary)
            {
                room.cleared = true;
                SetAllRoomGates(room, true);
                if (room.rewardChest != null) room.rewardChest.UnlockChest();
                CurrentObjective = room.index == 0
                    ? "Tiến về phía Bắc qua Cổng Sắt để khám phá Hầm Ngục Rẽ Nhánh!"
                    : $"{room.title} — Hồi phục sinh lực tại Đài Hồi Phục [Phím F] hoặc khám phá nhánh tiếp theo.";
                if (room.index > 0)
                    ShowBanner($"{room.title.ToUpper()} — KHU VỰC AN TOÀN & HỒI PHỤC!", 3.2f);
                return;
            }

            // Lock all doorways of this room during encounter & pull AI party inside room bounds
            SetAllRoomGates(room, false);

            foreach (var h in PartyUnits)
            {
                if (h != null && !h.IsLocalPlayer && !h.IsDead)
                {
                    bool outsideX = Mathf.Abs(h.transform.position.x - room.center.x) > room.width * 0.5f - 1.5f;
                    bool outsideZ = Mathf.Abs(h.transform.position.z - room.center.z) > room.length * 0.5f - 1.5f;
                    if (outsideX || outsideZ)
                    {
                        if (h.Controller != null) h.Controller.enabled = false;
                        Vector3 safeTarget = LocalHero != null ? LocalHero.transform.position : room.center;
                        Vector3 dirToCenter = (room.center - safeTarget).normalized;
                        h.transform.position = safeTarget + dirToCenter * 1.4f + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0f, UnityEngine.Random.Range(-1.5f, 1.5f));
                        if (h.Controller != null) h.Controller.enabled = true;
                    }
                }
            }

            room.activeMonsters.RemoveAll(m => m == null || m.IsDead);
            if (room.activeMonsters.Count > 0)
            {
                ActivatePrebakedEncounterForRoom(room);
            }
            else
            {
                SpawnEncounterForRoom(room, dormantForUnactivatedRoom: false);
            }
        }

        private void ActivatePrebakedEncounterForRoom(DungeonRoomRuntime room)
        {
            int floor = GameSession.I.Profile.selectedFloor;
            var dir = AbyssDirectorAI.Instance;
            float hpM = dir != null ? dir.HpMultiplier : 1f;
            float atkM = dir != null ? dir.AtkMultiplier : 1f;
            float eliteChance = dir != null ? dir.GetFloorEliteAffixChance(floor) : 0.25f;

            if (dir != null && dir.SpawnBonusHealingShrine && room.type != RoomType.Entrance)
            {
                WorldHealingShrine.Create(room.roomRoot != null ? room.roomRoot : transform,
                    room.center + new Vector3(-room.width * 0.32f, 0f, -room.length * 0.28f));
            }

            int promotedElites = 0;
            int maxPromotions = Mathf.Clamp(1 + floor / 5, 1, 3);
            for (int i = 0; i < room.activeMonsters.Count; i++)
            {
                var m = room.activeMonsters[i];
                if (m == null) continue;
                if (!m.IsBoss && !m.IsMiniBoss && !m.IsElite && promotedElites < maxPromotions && UnityEngine.Random.value < eliteChance)
                {
                    m.IsElite = true;
                    promotedElites++;
                }
                m.ReinitializePrebakedMonster(floor, hpM, atkM);
                m.OnKilled -= HandleMonsterKilled;
                m.OnKilled += HandleMonsterKilled;
                m.SetRoomDormant(false);

                var bossAi = m.GetComponent<AbyssalBossAI>();
                if (bossAi != null)
                {
                    bossAi.ArenaCenter = room.center;
                    bossAi.ArenaRadius = room.width * 0.5f - 1.2f;
                }
            }

            // Top up Wave 1 density with layered Frontline + Backline Ranged/Mage support (preserves all baked monsters!)
            if (room.type == RoomType.Combat || room.type == RoomType.Elite)
            {
                int targetWave1 = room.type == RoomType.Combat
                    ? Mathf.Clamp(6 + (floor >= 5 ? 1 : 0) + (dir != null ? Mathf.Max(0, dir.ExtraMonsterDelta) : 0), 6, 9)
                    : Mathf.Clamp(5 + (floor >= 6 ? 1 : 0), 5, 7);

                Transform mParent = room.monstersRoot != null ? room.monstersRoot : (room.roomRoot != null ? room.roomRoot : transform);
                string[] frontlineIds = { "SkeletonWarrior", "Skeleton", "Demon", "ForestMonster", "Zombie" };
                string[] backlineIds = { "GoblinArcher", "SkeletonMage" };

                int added = 0;
                while (room.activeMonsters.Count < targetWave1)
                {
                    bool spawnRanged = (added % 2 == 0); // Prioritize ranged/mage backline depth!
                    string id = spawnRanged
                        ? backlineIds[(added + floor) % backlineIds.Length]
                        : frontlineIds[(added + floor) % frontlineIds.Length];
                    bool makeElite = !spawnRanged && promotedElites < maxPromotions && UnityEngine.Random.value < eliteChance;
                    if (makeElite) promotedElites++;

                    Vector3 offset = spawnRanged
                        ? new Vector3((added % 2 == 0 ? -1f : 1f) * UnityEngine.Random.Range(4.2f, 7.2f), 0f, UnityEngine.Random.Range(4.5f, 7.2f))
                        : new Vector3(UnityEngine.Random.Range(-5.2f, 5.2f), 0f, UnityEngine.Random.Range(0.5f, 3.8f));
                    var extra = SpawnMonsterAt(GameDB.Enemies[id], room.center + offset, floor, makeElite, false, false, mParent);
                    room.activeMonsters.Add(extra);
                    added++;
                }
            }

            room.hasReinforcementWave = (room.type == RoomType.Combat || room.type == RoomType.Elite || room.type == RoomType.MiniBoss);
            room.wave1InitialCount = room.activeMonsters.Count;
            room.wave2Spawned = false;

            ShowRoomEncounterBanner(room);
        }

        private void SpawnWave2Reinforcements(DungeonRoomRuntime room)
        {
            if (room == null || room.wave2Spawned) return;
            room.wave2Spawned = true;

            int floor = GameSession.I.Profile.selectedFloor;
            var dir = AbyssDirectorAI.Instance;
            int count = dir != null ? dir.GetWave2ReinforcementCount(floor, room.type) : 4;
            float eliteChance = dir != null ? dir.GetFloorEliteAffixChance(floor) : 0.30f;
            Transform mParent = room.monstersRoot != null ? room.monstersRoot : (room.roomRoot != null ? room.roomRoot : transform);

            string[] frontlinePool = room.type == RoomType.Elite
                ? new[] { "DarkKnight", "Demon", "SkeletonWarrior" }
                : new[] { "SkeletonWarrior", "Demon", "Goblin", "ForestMonster", "Skeleton" };
            string[] backlinePool = { "GoblinArcher", "SkeletonMage" };

            int guaranteedElites = room.type == RoomType.Elite ? 2 : (floor >= 4 ? 1 : 0);
            int elitesSpawned = 0;

            for (int i = 0; i < count; i++)
            {
                // Layered formation: ~45% Backline Ranged/Mage + ~55% Frontline Melee/Assassin
                bool isBackline = (i % 2 == 1);
                string id = isBackline
                    ? backlinePool[(i + floor) % backlinePool.Length]
                    : frontlinePool[(i + floor) % frontlinePool.Length];

                bool isElite = (elitesSpawned < guaranteedElites) || (!isBackline && UnityEngine.Random.value < eliteChance * 0.75f);
                if (isElite) elitesSpawned++;

                float sideSign = (i % 2 == 0) ? -1f : 1f;
                Vector3 spawnPos = isBackline
                    ? room.center + new Vector3(sideSign * UnityEngine.Random.Range(4.8f, room.width * 0.38f), 0f, UnityEngine.Random.Range(3.8f, room.length * 0.36f))
                    : room.center + new Vector3(sideSign * UnityEngine.Random.Range(2.5f, room.width * 0.34f), 0f, UnityEngine.Random.Range(-2.5f, 3.5f));

                var unit = SpawnMonsterAt(GameDB.Enemies[id], spawnPos, floor, isElite, false, false, mParent);
                room.activeMonsters.Add(unit);
                CharacterVisualFactory.SpawnBurst(spawnPos + Vector3.up * 0.6f, new Color(0.75f, 0.22f, 1f), 1.4f, 0.35f);
            }

            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.72f);
            ShowBanner($"ĐỢT TĂNG CƯỜNG (WAVE 2): +{count} QUÁI VỰC THẲM ĐỔ BỘ!", 3.2f);
        }

        private void ShowRoomEncounterBanner(DungeonRoomRuntime room)
        {
            if (!Application.isPlaying) return;
            switch (room.type)
            {
                case RoomType.Elite:
                    ShowBanner("CHẠM TRÁN QUÁI TINH ANH: DARK KNIGHT & DEMON!", 3.2f);
                    break;
                case RoomType.Puzzle:
                    ShowBanner("PHÒNG GIẢI ĐỐ: Thắp sáng cả 3 Trụ Ấn Phong Cổ [Phím F] & tiêu diệt Kẻ Canh Giữ!", 4f);
                    break;
                case RoomType.MiniBoss:
                    ShowBanner("MINI BOSS XUẤT HIỆN: ANCIENT CRYSTAL GOLEM!", 3.5f);
                    break;
                case RoomType.Boss:
                    ShowBanner("BOSS CUỐI: ABYSSAL KNIGHT — CHIẾN BINH VỰC THẲM!", 4.5f);
                    break;
            }
        }

        private void SpawnEncounterForRoom(DungeonRoomRuntime room, bool dormantForUnactivatedRoom = false)
        {
            int floor = GameSession.I.Profile.selectedFloor;
            var dir = AbyssDirectorAI.Instance;
            int delta = dir != null ? dir.ExtraMonsterDelta : 0;
            Transform mParent = room.monstersRoot != null ? room.monstersRoot : (room.roomRoot != null ? room.roomRoot : transform);

            if (!dormantForUnactivatedRoom && dir != null && dir.SpawnBonusHealingShrine && room.type != RoomType.Entrance)
            {
                WorldHealingShrine.Create(room.roomRoot != null ? room.roomRoot : transform,
                    room.center + new Vector3(-room.width * 0.32f, 0f, -room.length * 0.28f));
            }

            switch (room.type)
            {
                case RoomType.Combat:
                    {
                        string[] frontline = room.index == 1
                            ? new[] { "Goblin", "Skeleton", "Zombie", "SkeletonWarrior" }
                            : new[] { "SkeletonWarrior", "Demon", "ForestMonster", "Skeleton", "Zombie" };
                        string[] backline = { "GoblinArcher", "SkeletonMage" };
                        float eliteChance = dir != null ? dir.GetFloorEliteAffixChance(floor) : 0.25f;
                        int count = Mathf.Clamp((room.index == 1 ? 6 : 7) + delta, 5, 10);
                        Vector3[] deterministicOffsets =
                        {
                            new Vector3(-3.8f, 0f, 1.5f),
                            new Vector3(3.8f, 0f, 1.5f),
                            new Vector3(-5.2f, 0f, 5.2f),
                            new Vector3(5.2f, 0f, 5.2f),
                            new Vector3(0f, 0f, 2.2f),
                            new Vector3(0f, 0f, 6.2f),
                            new Vector3(-3.2f, 0f, -1.6f),
                            new Vector3(3.2f, 0f, -1.6f),
                            new Vector3(-6.0f, 0f, 3.8f),
                            new Vector3(6.0f, 0f, 3.8f)
                        };
                        for (int i = 0; i < count; i++)
                        {
                            bool isBackline = (i % 3 == 2); // Structured Frontline + Backline Ranged/Mage
                            string id = isBackline ? backline[i % backline.Length] : frontline[i % frontline.Length];
                            bool bonusElite = !isBackline && ((dir != null && dir.PromoteBonusElite && i == count - 1) || (!dormantForUnactivatedRoom && UnityEngine.Random.value < eliteChance * 0.5f));
                            Vector3 pos = dormantForUnactivatedRoom
                                ? (room.center + deterministicOffsets[i % deterministicOffsets.Length])
                                : (isBackline
                                    ? room.center + new Vector3(UnityEngine.Random.Range(-6.2f, 6.2f), 0f, UnityEngine.Random.Range(4.2f, 6.8f))
                                    : RandomPointInRoom(room, 3.5f));
                            var m = SpawnMonsterAt(GameDB.Enemies[id], pos, floor, bonusElite, false, false, mParent);
                            if (dormantForUnactivatedRoom) m.SetRoomDormant(true);
                            room.activeMonsters.Add(m);
                        }
                        if (!dormantForUnactivatedRoom)
                        {
                            room.hasReinforcementWave = true;
                            room.wave1InitialCount = room.activeMonsters.Count;
                            room.wave2Spawned = false;
                        }
                        break;
                    }

                case RoomType.Elite:
                    {
                        var m1 = SpawnMonsterAt(GameDB.Enemies["DarkKnight"], room.center + new Vector3(-2.5f, 0f, 3f), floor, true, false, false, mParent);
                        var m2 = SpawnMonsterAt(GameDB.Enemies["Demon"], room.center + new Vector3(2.5f, 0f, 3f), floor, true, false, false, mParent);
                        var m3 = SpawnMonsterAt(GameDB.Enemies["SkeletonWarrior"], room.center + new Vector3(0f, 0f, 0.5f), floor, false, false, false, mParent);
                        var m4 = SpawnMonsterAt(GameDB.Enemies["SkeletonMage"], room.center + new Vector3(0f, 0f, 5.5f), floor, false, false, false, mParent);
                        room.activeMonsters.Add(m1);
                        room.activeMonsters.Add(m2);
                        room.activeMonsters.Add(m3);
                        room.activeMonsters.Add(m4);
                        if (delta > 0)
                            room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], room.center + new Vector3(-4.5f, 0f, 4.5f), floor, false, false, false, mParent));
                        if (dormantForUnactivatedRoom)
                        {
                            foreach (var m in room.activeMonsters) if (m != null) m.SetRoomDormant(true);
                        }
                        else
                        {
                            room.hasReinforcementWave = true;
                            room.wave1InitialCount = room.activeMonsters.Count;
                            room.wave2Spawned = false;
                            ShowRoomEncounterBanner(room);
                        }
                        break;
                    }

                case RoomType.Puzzle:
                    {
                        var m1 = SpawnMonsterAt(GameDB.Enemies["ForestMonster"], room.center + new Vector3(-4f, 0f, 2f), floor, false, false, false, mParent);
                        var m2 = SpawnMonsterAt(GameDB.Enemies["ForestMonster"], room.center + new Vector3(4f, 0f, 2f), floor, false, false, false, mParent);
                        var m3 = SpawnMonsterAt(GameDB.Enemies["SkeletonMage"], room.center + new Vector3(0f, 0f, 5f), floor, false, false, false, mParent);
                        room.activeMonsters.Add(m1);
                        room.activeMonsters.Add(m2);
                        room.activeMonsters.Add(m3);
                        if (dormantForUnactivatedRoom)
                        {
                            foreach (var m in room.activeMonsters) if (m != null) m.SetRoomDormant(true);
                        }
                        else ShowRoomEncounterBanner(room);
                        break;
                    }

                case RoomType.MiniBoss:
                    {
                        var m1 = SpawnMonsterAt(GameDB.Enemies["CrystalGolem"], room.center + new Vector3(0f, 0f, 3.5f), floor, true, true, false, mParent);
                        var m2 = SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], room.center + new Vector3(-5.5f, 0f, 4.5f), floor, false, false, false, mParent);
                        var m3 = SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], room.center + new Vector3(5.5f, 0f, 4.5f), floor, false, false, false, mParent);
                        room.activeMonsters.Add(m1);
                        room.activeMonsters.Add(m2);
                        room.activeMonsters.Add(m3);
                        if (delta >= 0)
                            room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["SkeletonWarrior"], room.center + new Vector3(0f, 0f, 0.5f), floor, false, false, false, mParent));
                        if (dormantForUnactivatedRoom)
                        {
                            foreach (var m in room.activeMonsters) if (m != null) m.SetRoomDormant(true);
                        }
                        else
                        {
                            room.hasReinforcementWave = true;
                            room.wave1InitialCount = room.activeMonsters.Count;
                            room.wave2Spawned = false;
                            ShowRoomEncounterBanner(room);
                        }
                        break;
                    }

                case RoomType.Boss:
                    {
                        var bossUnit = SpawnMonsterAt(GameDB.Boss, room.center + new Vector3(0f, 0f, 5f), floor, true, false, isBoss: true, parent: mParent);
                        var bossAi = bossUnit.GetComponent<AbyssalBossAI>();
                        if (bossAi != null)
                        {
                            bossAi.ArenaCenter = room.center;
                            bossAi.ArenaRadius = room.width * 0.5f - 1.2f;
                        }
                        if (dormantForUnactivatedRoom) bossUnit.SetRoomDormant(true);
                        else ShowRoomEncounterBanner(room);
                        room.activeMonsters.Add(bossUnit);
                        break;
                    }
            }
        }

        public ActorUnit SpawnMonsterAt(EnemyDef def, Vector3 pos, int floor, bool elite, bool miniBoss, bool isBoss = false, Transform parent = null)
        {
            var dir = AbyssDirectorAI.Instance;
            float hpM = dir != null ? dir.HpMultiplier : 1f;
            float atkM = dir != null ? dir.AtkMultiplier : 1f;

            bool actualBoss = isBoss || def.archetype == Archetype.Boss;
            bool actualMiniBoss = miniBoss || def.archetype == Archetype.MiniBoss;
            bool actualElite = elite || def.archetype == Archetype.Elite;

            string folder = actualBoss ? BakedAssetRegistry.BossPrefabFolder : BakedAssetRegistry.MonstersPrefabFolder;
            string prefabName = actualBoss
                ? $"PF_Boss_{def.id}"
                : (actualMiniBoss
                    ? $"PF_MiniBoss_{def.id}"
                    : (actualElite ? $"PF_Monster_{def.id}_Elite" : $"PF_Monster_{def.id}"));

            Vector3 spawnPos = new Vector3(pos.x, 0f, pos.z);
            var unit = BakedAssetRegistry.InstantiateOrBuildPrefab<ActorUnit>(
                folder,
                prefabName,
                parent,
                spawnPos,
                Quaternion.Euler(0f, 180f, 0f),
                () =>
                {
                    var go = new GameObject(prefabName);
                    var u = go.AddComponent<ActorUnit>();
                    u.InitMonster(def, floor, hpM, atkM, actualElite, actualMiniBoss);
                    if (actualBoss)
                    {
                        go.AddComponent<AbyssalBossAI>();
                    }
                    else
                    {
                        var ai = go.AddComponent<MonsterAI>();
                        ai.SpawnHome = spawnPos;
                    }
                    return go;
                },
                u =>
                {
                    if (u == null) return;
                    u.gameObject.name = actualBoss ? $"Boss_{def.id}" : $"Monster_{def.id}";
                    u.MonsterDef = def;
                    u.BakedEnemyId = def.id;
                    u.IsElite = actualElite;
                    u.IsMiniBoss = actualMiniBoss;
                    u.IsBoss = actualBoss;
                    var ai = u.GetComponent<MonsterAI>();
                    if (ai != null) ai.SpawnHome = spawnPos;
                });

            unit.OnKilled -= HandleMonsterKilled;
            unit.OnKilled += HandleMonsterKilled;
            return unit;
        }

        private void HandleMonsterKilled(ActorUnit m)
        {
            if (m == null || m.MonsterDef == null) return;
            if (AbyssDirectorAI.Instance != null) AbyssDirectorAI.Instance.RecordKill();

            int floor = GameSession.I.Profile.selectedFloor;
            float mult = (m.IsBoss ? 1f : (m.IsMiniBoss ? 2.2f : (m.IsElite ? 1.6f : 1f))) * (1f + (floor - 1) * 0.2f);
            int exp = Mathf.RoundToInt(m.MonsterDef.exp * mult);
            int gold = Mathf.RoundToInt(m.MonsterDef.gold * mult);

            AwardExpAndGold(exp, gold);

            // Loot drop roll (Section 3.8)
            float dropChance = m.IsBoss ? 1f : (m.IsMiniBoss ? 1f : (m.IsElite ? 0.78f : 0.35f));
            if (UnityEngine.Random.value <= dropChance)
            {
                int drops = m.IsBoss ? 3 : (m.IsMiniBoss ? 2 : 1);
                int minRarity = m.IsBoss ? 3 : (m.IsMiniBoss ? 2 : (m.IsElite ? 1 : 0));
                for (int i = 0; i < drops; i++)
                {
                    float luck = m.IsBoss ? 0.9f : 0f;
                    EquipItem item = LootRoller.RollItem(floor, minRarity, luck);
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 0f, UnityEngine.Random.Range(-1.2f, 1.2f));
                    Vector3 dropPos = m.transform.position + offset;
                    LootOrbPickup.Spawn(dropPos, item);
                    if (LanRoomBridge.HasInstance && LanRoomBridge.I.IsHosting)
                        LanRoomBridge.I.BroadcastInstancedLootDrop(dropPos, floor, minRarity, luck);
                }
            }
        }

        public void AwardExpAndGold(int exp, int gold)
        {
            RunExpGained += exp;
            RunGoldGained += gold;
            GameSession.I.Profile.gold += gold;
            bool leveled = GameSession.I.AddExp(exp);
            if (leveled)
            {
                GameAudio.I.PlaySfx(GameSfx.LevelUp, 1f);
                foreach (var h in PartyUnits)
                {
                    if (h != null && !h.IsDead)
                    {
                        h.Level = GameSession.I.Profile.level;
                        h.RecalculateStats(true);
                        CharacterVisualFactory.SpawnBurst(h.transform.position + Vector3.up, new Color(1f, 0.88f, 0.3f), 2.5f, 0.6f);
                        CharacterVisualFactory.SpawnFloatingText(h.transform.position + Vector3.up * 2.4f, $"LEVEL UP! Lv.{h.Level}", new Color(1f, 0.9f, 0.3f), 1.3f);
                    }
                }
                ShowBanner($"THĂNG CẤP! Đạt Level {GameSession.I.Profile.level} (+2 Điểm Chỉ Số, +1 Điểm Kỹ Năng [Phím I])", 3.5f);
            }
        }

        public void CollectLootItem(EquipItem item)
        {
            if (item == null) return;
            RunLootItems.Add(item);
            GameSession.I.AddItemToBag(item, autoEquipIfBetter: true);
            if (LocalHero != null) LocalHero.RecalculateStats(false);
            GameAudio.I.PlaySfx(GameSfx.Loot, 0.85f);
            string instancedTag = GameModeManager.IsMultiplayerCoOp ? "[Loot Riêng Co-op] " : "";
            string msg = $"{instancedTag}Nhặt [{GameDB.RarityNames[(int)item.rarity]}] {item.name} ({item.Summary()})";
            OnLootNotification?.Invoke(msg);
        }

        public void ApplyRemotePuzzlePillarActivation(int roomIdx, int pillarIdx)
        {
            DungeonRoomRuntime puzzleRoom = null;
            if (roomIdx >= 0 && roomIdx < Rooms.Count) puzzleRoom = Rooms[roomIdx];
            if (puzzleRoom == null)
            {
                for (int i = 0; i < Rooms.Count; i++)
                {
                    if (Rooms[i].type == RoomType.Puzzle && Rooms[i].puzzlePillars.Count > 0)
                    {
                        puzzleRoom = Rooms[i];
                        break;
                    }
                }
            }
            if (puzzleRoom == null) return;

            for (int i = 0; i < puzzleRoom.puzzlePillars.Count; i++)
            {
                var p = puzzleRoom.puzzlePillars[i];
                if (p != null && p.PillarIndex == pillarIdx && !p.Activated)
                {
                    p.ActivateFromNetwork();
                    break;
                }
            }
        }

        private void OnPuzzleRuneActivated(WorldRunePillar pillar)
        {
            GameAudio.I.PlaySfx(GameSfx.Heal, 0.9f);
            AwardExpAndGold(35, 20);

            DungeonRoomRuntime puzzleRoom = null;
            int foundRoomIdx = -1;
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].puzzlePillars.Contains(pillar) || Rooms[i].type == RoomType.Puzzle)
                {
                    puzzleRoom = Rooms[i];
                    foundRoomIdx = i;
                    if (Rooms[i].puzzlePillars.Contains(pillar)) break;
                }
            }
            if (puzzleRoom == null && Rooms.Count > 3)
            {
                puzzleRoom = Rooms[3];
                foundRoomIdx = 3;
            }

            if (pillar != null && !pillar.TriggeredRemotely && LanRoomBridge.HasInstance)
            {
                LanRoomBridge.I.BroadcastPuzzlePillarActivated(foundRoomIdx, pillar.PillarIndex);
            }

            int lit = 0;
            if (puzzleRoom != null)
            {
                foreach (var p in puzzleRoom.puzzlePillars) if (p.Activated) lit++;
                Vector3 targetConduitPos = puzzleRoom.rewardChest != null
                    ? puzzleRoom.rewardChest.Position
                    : (puzzleRoom.entryGate != null ? puzzleRoom.entryGate.transform.position : puzzleRoom.center);
                if (pillar != null)
                {
                    RuneSealEnergyBeam.SpawnConnectorBeam(transform, pillar.Position, targetConduitPos, new Color(1f, 0.85f, 0.28f));
                }
                // Also link activated pillars to each other to form the runic triangle circuit
                for (int i = 0; i < puzzleRoom.puzzlePillars.Count; i++)
                {
                    var other = puzzleRoom.puzzlePillars[i];
                    if (other != null && other != pillar && other.Activated)
                    {
                        RuneSealEnergyBeam.SpawnConnectorBeam(transform, pillar.Position, other.Position, new Color(0.28f, 0.95f, 1f));
                    }
                }
            }
            ShowBanner($"Đã thắp sáng Ấn Phong Cổ ({lit}/3)!", 2.5f);
        }

        private void MarkRoomCleared(DungeonRoomRuntime room)
        {
            room.cleared = true;
            SetAllRoomGates(room, true);
            if (room.rewardChest != null) room.rewardChest.UnlockChest();

            // Revive any downed teammates automatically on clearing a room!
            foreach (var h in PartyUnits)
                if (h != null && h.IsDowned) h.Revive(0.5f);

            GameAudio.I.PlaySfx(GameSfx.Door, 0.85f);

            if (room.type == RoomType.Boss)
            {
                CurrentObjective = "BOSS ĐÃ BỊ HẠ GỤC! Nhặt chiến lợi phẩm & bước vào Cổng Chiến Thắng [Phím F]!";
                ShowBanner("CHIẾN THẮNG! ABYSSAL KNIGHT ĐÃ BỊ TIÊU DIỆT!", 6f);
                GameAudio.I.PlayMusic(GameMusic.Victory);
                AwardExpAndGold(250 * GameSession.I.Profile.selectedFloor, 200 * GameSession.I.Profile.selectedFloor);

                WorldExitPortal.Create(transform, room.center + new Vector3(0f, 0f, 8f), "HOÀN THÀNH PHÓ BẢN [Phím F]", () => CompleteRun(true));
                StartCoroutine(AutoCompleteAfterDelay(9f));
            }
            else
            {
                if (AbyssDirectorAI.Instance != null)
                    AbyssDirectorAI.Instance.EvaluateAndAdjustNextEncounter(room.index);

                CurrentObjective = $"Đã dọn sạch {room.title}! Mở Rương [F] và chọn ngã rẽ tiếp theo trên Bản Đồ Cổ Ngữ.";
            }
        }

        private IEnumerator AutoCompleteAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!RunFinished) CompleteRun(true);
        }

        public void CompleteRun(bool victory)
        {
            if (RunFinished) return;
            RunFinished = true;

            var dir = AbyssDirectorAI.Instance;
            int clearedCount = 0;
            foreach (var r in Rooms) if (r.cleared && r.type != RoomType.Entrance) clearedCount++;

            if (victory)
            {
                var prof = GameSession.I.Profile;
                if (prof.selectedFloor >= prof.highestFloor)
                    prof.highestFloor = prof.selectedFloor + 1;
                prof.potions = Mathf.Max(prof.potions, 5);
                GameSession.I.SaveProfile();
            }

            GameSession.I.LastRun = new RunReport
            {
                victory = victory,
                floor = GameSession.I.Profile.selectedFloor,
                elapsedTime = dir != null ? dir.RunElapsed : 0f,
                monstersKilled = dir != null ? dir.TotalKills : 0,
                roomsCleared = clearedCount,
                totalRooms = Rooms.Count - 1,
                playerDeaths = dir != null ? dir.TotalDeaths : 0,
                damageDealt = dir != null ? dir.TotalDamageDealt : 0f,
                damageTaken = dir != null ? dir.TotalDamageTaken : 0f,
                healingDone = dir != null ? dir.TotalHealing : 0f,
                expGained = RunExpGained,
                goldGained = RunGoldGained,
                startLevel = RunStartLevel,
                endLevel = GameSession.I.Profile.level,
                directorScore = dir != null ? dir.PerformanceScore : 50f,
                directorVerdict = dir != null ? dir.GetVerdictLabel() : "Cân bằng",
                lootedItems = new List<EquipItem>(RunLootItems)
            };

            StartCoroutine(TransitionToEndGameRoutine(victory ? 1.2f : 2.2f));
        }

        private IEnumerator TransitionToEndGameRoutine(float delay)
        {
            if (!GameSession.I.LastRun.victory)
            {
                ShowBanner("TỔ ĐỘI ĐÃ GỤC NGÃ TRONG VỰC THẲM...", 2.5f);
                GameAudio.I.PlayMusic(GameMusic.Defeat);
            }
            yield return new WaitForSeconds(delay);
            GameSession.GoToScene("EndGame");
        }

        public void ShowBanner(string text, float duration = 3.2f)
        {
            BannerMessage = text;
            BannerTimer = duration;
        }

        private void TriggerSoulAnchorPartyRespawn()
        {
            SoulAnchorRespawnAvailable = false;
            Vector3 safeAnchor = (CurrentRoomIndex >= 0 && CurrentRoomIndex < Rooms.Count)
                ? (Rooms[CurrentRoomIndex].center + new Vector3(0f, 0f, -Rooms[CurrentRoomIndex].length * 0.30f))
                : Vector3.zero;

            GameAudio.I.PlaySfx(GameSfx.LevelUp, 1f);
            for (int i = 0; i < PartyUnits.Count; i++)
            {
                var h = PartyUnits[i];
                if (h == null) continue;
                Vector3 respawnPos = safeAnchor + new Vector3((i - 1.5f) * 1.6f, 0f, 0f);
                if (h.Controller != null) h.Controller.enabled = false;
                h.transform.position = respawnPos;
                if (h.Controller != null) h.Controller.enabled = true;
                h.Revive(0.75f);
                h.AddShield(h.MaxHp * 0.35f);
                h.ApplyStatus(StatusType.DefUp, 4.0f, 0f, h);
            }

            // Push nearby enemies back from the Soul Anchor respawn zone
            if (LocalHero != null)
            {
                foreach (var u in ActorUnit.AllUnits)
                {
                    if (u != null && u.Team == Team.Monster && !u.IsDead)
                    {
                        float d = Vector3.Distance(safeAnchor, u.transform.position);
                        if (d < 7.5f) u.ApplyKnockback(u.transform.position - safeAnchor, 9f);
                    }
                }
            }

            if (ScreenCombatFeedbackUI.Instance != null)
                ScreenCombatFeedbackUI.Instance.NotifyCombatSurge(new Color(1f, 0.88f, 0.35f), 0.48f);
            ShowBanner("MỎ NEO LINH HỒN KÍCH HOẠT — HỒI SINH TỔ ĐỘI (1 LẦN/TẦNG)!", 4.5f);
        }

        private static Vector3 RandomPointInRoom(DungeonRoomRuntime r, float margin)
        {
            float x = UnityEngine.Random.Range(-r.width * 0.5f + margin, r.width * 0.5f - margin);
            float z = UnityEngine.Random.Range(-r.length * 0.5f + margin, r.length * 0.5f - margin);
            return r.center + new Vector3(x, 0f, z);
        }
    }

    // ==================== 3D INTERACTIVE DUNGEON PROPS ====================

    public class LootOrbPickup : MonoBehaviour
    {
        public EquipItem Item;
        private float _t;
        private Transform _itemPivot;
        private Transform _beamTransform;
        private Transform _runeOuter;
        private Transform _runeInner;

        private static Mesh _cachedLootBeamMesh;
        private static Texture2D _cachedRuneTex;
        private static Texture2D _cachedBeamGradTex;
        private static Material _cachedParticleMat;
        private static readonly Material[] _cachedBeamMats = new Material[6];
        private static readonly Material[] _cachedRuneMats = new Material[6];

        private static readonly Color[] LootBeamPalette =
        {
            new Color(0.88f, 0.90f, 0.96f, 1f), // Common (White/Silver)
            new Color(0.22f, 0.98f, 0.42f, 1f), // Uncommon (Emerald Green)
            new Color(0.20f, 0.68f, 1.00f, 1f), // Rare (Sapphire Blue)
            new Color(0.76f, 0.30f, 1.00f, 1f), // Epic (Arcane Purple)
            new Color(1.00f, 0.64f, 0.12f, 1f), // Legendary (Golden Orange)
            new Color(1.00f, 0.18f, 0.34f, 1f)  // Mythic (Crimson Red)
        };

        private static Mesh GetLootBeamMesh()
        {
            if (_cachedLootBeamMesh != null) return _cachedLootBeamMesh;
            var go = Resources.Load<GameObject>("VFX/Meshes/VFX_LootBeam3D");
            if (go != null)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    _cachedLootBeamMesh = mf.sharedMesh;
                    return _cachedLootBeamMesh;
                }
            }
#if UNITY_EDITOR
            var edGo = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Meshes/VFX_LootBeam3D.fbx")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_LootBeam3D.fbx");
            if (edGo != null)
            {
                var mf = edGo.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    _cachedLootBeamMesh = mf.sharedMesh;
                    return _cachedLootBeamMesh;
                }
            }
#endif
            return null;
        }

        private static Texture2D GetBeamGradientTex()
        {
            if (_cachedBeamGradTex != null) return _cachedBeamGradTex;
            int w = 64, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "T_LootBeamGrad_HDR",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                // Strong bright base fading softly toward top of open-ended pillar
                float vFade = Mathf.Pow(1f - v, 1.35f) * Mathf.SmoothStep(0f, 0.06f, v);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float streak = 0.72f + 0.28f * Mathf.Sin(u * Mathf.PI * 6f + v * 8f);
                    float a = Mathf.Clamp01(vFade * streak);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            _cachedBeamGradTex = tex;
            return tex;
        }

        private static Texture2D GetRuneCircleTex()
        {
            if (_cachedRuneTex != null) return _cachedRuneTex;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "T_LootRuneCircle_HDR",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 1f) { px[y * size + x] = Color.clear; continue; }

                    float ang = Mathf.Atan2(dy, dx);
                    float outerRing = Mathf.Exp(-Mathf.Pow((r - 0.88f) / 0.032f, 2f));
                    float innerRing = Mathf.Exp(-Mathf.Pow((r - 0.68f) / 0.025f, 2f));
                    float coreRing  = Mathf.Exp(-Mathf.Pow((r - 0.36f) / 0.028f, 2f));

                    // Runic glyph ticks between innerRing and outerRing
                    float runeBand = (r > 0.71f && r < 0.85f)
                        ? Mathf.Clamp01(Mathf.Sin(ang * 16f) * Mathf.Cos((r - 0.78f) * 45f)) * 0.85f
                        : 0f;

                    // 6-pointed hexagram star lines inside the rune ring
                    float star = 0f;
                    if (r > 0.34f && r < 0.69f)
                    {
                        float s1 = Mathf.Abs(Mathf.Sin(ang * 3f));
                        float targetR = Mathf.Lerp(0.36f, 0.67f, s1);
                        star = Mathf.Exp(-Mathf.Pow((r - targetR) / 0.028f, 2f)) * 0.75f;
                    }

                    float centerGlow = Mathf.Clamp01(1f - r * 1.15f) * 0.24f;
                    float a = Mathf.Clamp01(outerRing + innerRing * 0.85f + coreRing * 0.7f + runeBand + star + centerGlow);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            _cachedRuneTex = tex;
            return tex;
        }

        private static Material GetBeamMaterial(int rarityIdx)
        {
            rarityIdx = Mathf.Clamp(rarityIdx, 0, 5);
            if (_cachedBeamMats[rarityIdx] != null) return _cachedBeamMats[rarityIdx];

            Color col = LootBeamPalette[rarityIdx];
            float hdrIntensity = 1.9f + rarityIdx * 0.48f;
            Shader sh = Shader.Find("AbyssalDungeon/VFX_URP_Unlit");
            Material mat;
            if (sh != null)
            {
                mat = new Material(sh) { name = "M_LootBeam3D_" + rarityIdx };
                mat.SetTexture("_MainTex", GetBeamGradientTex());
                mat.SetColor("_MainColor", new Color(col.r, col.g, col.b, 0.78f));
                mat.SetColor("_FresnelColor", Color.Lerp(col, Color.white, 0.45f));
                mat.SetFloat("_FresnelPower", 2.0f);
                mat.SetFloat("_EmissionIntensity", hdrIntensity);
                mat.SetFloat("_ScrollSpeedX", 0.08f);
                mat.SetFloat("_ScrollSpeedY", -0.65f);
                mat.SetFloat("_EdgeFade", 1.15f);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.renderQueue = 3050;
            }
            else
            {
                mat = CharacterVisualFactory.MakeTransparentMat(new Color(col.r, col.g, col.b, 0.48f), col * hdrIntensity);
            }
            _cachedBeamMats[rarityIdx] = mat;
            return mat;
        }

        private static Material GetRuneMaterial(int rarityIdx)
        {
            rarityIdx = Mathf.Clamp(rarityIdx, 0, 5);
            if (_cachedRuneMats[rarityIdx] != null) return _cachedRuneMats[rarityIdx];

            Color col = LootBeamPalette[rarityIdx];
            float hdr = 2.2f + rarityIdx * 0.45f;
            Shader sh = Shader.Find("AbyssalDungeon/VFX_URP_Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh) { name = "M_LootRuneCircle_" + rarityIdx };
            Texture2D tex = GetRuneCircleTex();
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainColor")) mat.SetColor("_MainColor", new Color(col.r, col.g, col.b, 0.92f));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col * hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col * hdr);
            if (mat.HasProperty("_FresnelColor")) mat.SetColor("_FresnelColor", Color.white);
            if (mat.HasProperty("_EmissionIntensity")) mat.SetFloat("_EmissionIntensity", hdr);
            if (mat.HasProperty("_ScrollSpeedX")) mat.SetFloat("_ScrollSpeedX", 0f);
            if (mat.HasProperty("_ScrollSpeedY")) mat.SetFloat("_ScrollSpeedY", 0f);
            if (mat.HasProperty("_EdgeFade")) mat.SetFloat("_EdgeFade", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3045;
            _cachedRuneMats[rarityIdx] = mat;
            return mat;
        }

        public static void Spawn(Vector3 pos, EquipItem item)
        {
            if (item == null) return;
            int rIdx = Mathf.Clamp((int)item.rarity, 0, 5);
            Color col = LootBeamPalette[rIdx];

            var go = new GameObject("LootDrop3D_" + item.rarity);
            go.transform.position = new Vector3(pos.x, 0.02f, pos.z);

            var p = go.AddComponent<LootOrbPickup>();
            p.Item = item;

            // 1. 3D Equipment Mesh floating & rotating at waist/chest height
            var itemPivot = new GameObject("ItemVisualPivot");
            itemPivot.transform.SetParent(go.transform, false);
            itemPivot.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            p._itemPivot = itemPivot.transform;

            var orbMat = CharacterVisualFactory.MakeMat(
                Color.Lerp(col, new Color(0.85f, 0.85f, 0.92f), 0.32f),
                0.88f, 0.82f, col * (1.5f + rIdx * 0.35f));
            string pieceName = EnemyVisualArmorKit.GetEquipmentMeshPieceName(item.slot);
            ObjMeshLoader.SpawnPiece(pieceName, itemPivot.transform, Vector3.zero, Quaternion.Euler(16f, 0f, -14f), Vector3.one * 0.95f, orbMat, addBoxCollider: false);

            // 2. Blender 3D Open-Ended Tapered Light Pillar + Swirling Helical Ribbons (VFX_LootBeam3D)
            var beamGo = new GameObject("VFX_LootBeam3D");
            beamGo.transform.SetParent(go.transform, false);
            beamGo.transform.localPosition = Vector3.zero;
            float beamRadiusScale = 0.65f + rIdx * 0.08f;
            float beamHeightScale = 0.72f + rIdx * 0.11f;
            beamGo.transform.localScale = new Vector3(beamRadiusScale, beamHeightScale, beamRadiusScale);
            p._beamTransform = beamGo.transform;

            Mesh lootMesh = GetLootBeamMesh();
            Material beamMat = GetBeamMaterial(rIdx);
            if (lootMesh != null)
            {
                var mf = beamGo.AddComponent<MeshFilter>();
                mf.sharedMesh = lootMesh;
                var mr = beamGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = beamMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            else
            {
                CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "FallbackBeam", beamGo.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.35f, 1.5f, 0.35f), beamMat);
            }

            // 3. Rotating Ground Rune Magic Circle (Flush with Dungeon Floor)
            var runeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            runeQuad.name = "GroundRuneCircle";
            Destroy(runeQuad.GetComponent<Collider>());
            runeQuad.transform.SetParent(go.transform, false);
            float runeSize = 1.35f + rIdx * 0.16f;
            runeQuad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            runeQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            runeQuad.transform.localScale = new Vector3(runeSize, runeSize, 1f);
            var runeMr = runeQuad.GetComponent<MeshRenderer>();
            runeMr.sharedMaterial = GetRuneMaterial(rIdx);
            runeMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            runeMr.receiveShadows = false;
            p._runeOuter = runeQuad.transform;

            if (rIdx >= 2)
            {
                var innerQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                innerQuad.name = "InnerRuneSeal";
                Destroy(innerQuad.GetComponent<Collider>());
                innerQuad.transform.SetParent(go.transform, false);
                float innerSize = runeSize * 0.68f;
                innerQuad.transform.localPosition = new Vector3(0f, 0.026f, 0f);
                innerQuad.transform.localRotation = Quaternion.Euler(90f, 45f, 0f);
                innerQuad.transform.localScale = new Vector3(innerSize, innerSize, 1f);
                var inMr = innerQuad.GetComponent<MeshRenderer>();
                inMr.sharedMaterial = GetRuneMaterial(rIdx);
                inMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                inMr.receiveShadows = false;
                p._runeInner = innerQuad.transform;
            }

            // 4. Rising Light Motes Particle System (Lightweight MX350 budget: 10..28 particles)
            var psGo = new GameObject("RisingLootMotes");
            psGo.transform.SetParent(go.transform, false);
            psGo.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.85f, 1.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = Color.Lerp(col, Color.white, 0.35f);
            main.maxParticles = 10 + rIdx * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 5f + rIdx * 3.5f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.42f + rIdx * 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 0.4f), new GradientColorKey(col, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            if (_cachedParticleMat == null)
            {
                Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
                _cachedParticleMat = new Material(psh) { name = "M_LootMote_Shared" };
                if (_cachedParticleMat.HasProperty("_BaseColor")) _cachedParticleMat.SetColor("_BaseColor", Color.white);
            }
            psr.sharedMaterial = _cachedParticleMat;

            // 5. Floor Point Light for Uncommon+ drops
            if (rIdx >= 1)
            {
                var lGo = new GameObject("LootGlowLight");
                lGo.transform.SetParent(go.transform, false);
                lGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                var lt = lGo.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.shadows = LightShadows.None;
                lt.color = col;
                lt.range = 2.6f + rIdx * 0.45f;
                lt.intensity = 1.1f + rIdx * 0.35f;
            }
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_itemPivot != null)
            {
                _itemPivot.localPosition = new Vector3(0f, 0.56f + Mathf.Sin(_t * 3.4f) * 0.11f, 0f);
                _itemPivot.Rotate(0f, 92f * Time.deltaTime, 0f, Space.World);
            }
            if (_beamTransform != null)
            {
                _beamTransform.Rotate(0f, -48f * Time.deltaTime, 0f, Space.Self);
            }
            if (_runeOuter != null)
            {
                _runeOuter.Rotate(0f, 0f, 36f * Time.deltaTime, Space.Self);
            }
            if (_runeInner != null)
            {
                _runeInner.Rotate(0f, 0f, -54f * Time.deltaTime, Space.Self);
            }

            var dw = DungeonWorldBuilder.Instance;
            if (dw != null && dw.LocalHero != null && !dw.LocalHero.IsDead)
            {
                Vector3 heroPos = dw.LocalHero.transform.position;
                float d = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(heroPos.x, heroPos.z));
                if (d < 4.5f)
                {
                    Vector3 targetGround = new Vector3(heroPos.x, 0.02f, heroPos.z);
                    transform.position = Vector3.MoveTowards(transform.position, targetGround, 12.5f * Time.deltaTime);
                }
                if (d < 1.55f)
                {
                    int rIdx = Item != null ? Mathf.Clamp((int)Item.rarity, 0, 5) : 0;
                    CharacterVisualFactory.SpawnBurst(heroPos + Vector3.up * 0.9f, LootBeamPalette[rIdx], 1.6f, 0.38f);
                    dw.CollectLootItem(Item);
                    Destroy(gameObject);
                }
            }
        }
    }
}

