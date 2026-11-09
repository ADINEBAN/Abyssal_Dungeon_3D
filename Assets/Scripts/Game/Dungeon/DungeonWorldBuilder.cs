using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    public class DungeonRoomRuntime
    {
        public int index;
        public RoomType type;
        public DungeonBiome biome;
        public string title;
        public Vector3 center;
        public float width;
        public float length;
        public bool activated;
        public bool cleared;
        public DungeonGate entryGate;
        public DungeonGate exitGate;
        public WorldTreasureChest rewardChest;
        public readonly List<ActorUnit> activeMonsters = new List<ActorUnit>();
        public readonly List<WorldRunePillar> puzzlePillars = new List<WorldRunePillar>();
    }

    /// <summary>
    /// Builds the 7-room 3D Abyssal Dungeon, spawns the 3-4 Hunter party, runs room encounters,
    /// Puzzle room, Traps, Chests, Healing Shrines, Loot Orbs, Abyss Director adjustments, and Boss completion.
    /// </summary>
    public class DungeonWorldBuilder : MonoBehaviour
    {
        public static DungeonWorldBuilder Instance { get; private set; }

        public readonly List<DungeonRoomRuntime> Rooms = new List<DungeonRoomRuntime>();
        public readonly List<ActorUnit> PartyUnits = new List<ActorUnit>();
        public ActorUnit LocalHero { get; private set; }

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
            HeroPlayerInput.Interactables.Clear();
            GroundTelegraphFx.ActiveHazards.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BuildAndStartDungeon()
        {
            GameSession.I.EnsureDefaultParty();
            RunStartLevel = GameSession.I.Profile.level;
            SoulAnchorRespawnAvailable = true;
            CombatFeelManager.I.ResetRunCombo();

            // Setup Abyss Director
            var dirGo = new GameObject("AbyssDirectorAI");
            dirGo.transform.SetParent(transform, false);
            var director = dirGo.AddComponent<AbyssDirectorAI>();
            director.AnalyzePartyComposition();
            director.OnDirectorAdjusted += msg => ShowBanner(msg, 4.5f);

            SetupEnvironmentLighting();
            CreateDungeonMaterials();
            ConstructRoomsGeometry();
            SpawnHeroParty();

            // Activate Entrance Room immediately
            ActivateRoom(0);
            ShowBanner($"TẦNG {GameSession.I.Profile.selectedFloor}: ABYSSAL DUNGEON — {director.CompositionTag}", 4f);
            GameAudio.I.PlayMusic(GameMusic.Dungeon);
        }

        private void SetupEnvironmentLighting()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);
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
            // Progression matching Section 3.5 & Section 2.1 (7 Distinct Biomes across 7 Rooms):
            // 0 Entrance (ForgottenTemple) -> 1 Combat (AncientDungeon) -> 2 Elite (UndergroundFortress)
            // -> 3 Puzzle/Event (AbyssalCave) -> 4 MiniBoss (CrystalChamber) -> 5 Combat (CursedForest) -> 6 Final Boss (BossArena)
            var specs = new (RoomType type, float w, float len)[]
            {
                (RoomType.Entrance, 20f, 20f),
                (RoomType.Combat,   24f, 24f),
                (RoomType.Elite,    26f, 26f),
                (RoomType.Puzzle,   24f, 24f),
                (RoomType.MiniBoss, 26f, 26f),
                (RoomType.Combat,   26f, 26f),
                (RoomType.Boss,     34f, 34f)
            };

            const float corridorLen = 8f;
            const float corridorW = 6f;
            float cursorZ = 0f;

            for (int i = 0; i < specs.Length; i++)
            {
                float w = specs[i].w;
                float len = specs[i].len;
                Vector3 center = new Vector3(0f, 0f, cursorZ + len * 0.5f);
                DungeonBiome biome = BiomeAtmosphereLibrary.BiomeForDungeonRoom(i, specs[i].type);

                var room = new DungeonRoomRuntime
                {
                    index = i,
                    type = specs[i].type,
                    biome = biome,
                    title = $"Phòng {i + 1}/7: {GameDB.RoomName(specs[i].type)}",
                    center = center,
                    width = w,
                    length = len
                };
                Rooms.Add(room);

                var rRoot = new GameObject($"Room_{i}_{specs[i].type}_{biome}");
                rRoot.transform.SetParent(transform, false);

                float wallH = specs[i].type == RoomType.Boss ? 6.6f : 5.2f;
                bool hasSouthDoor = (i > 0);
                bool hasNorthDoor = (i < specs.Length - 1);

                // Modular 3D Gothic Architecture (Pillars, Vault Arches, Recessed Channel Floor, Blind-Arch Walls, Sconce Torches, Mist & Decals)
                ModularDungeonArchitect.BuildModularRoomShell(
                    rRoot.transform, center, w, len, wallH, biome,
                    openSouthDoor: hasSouthDoor, openNorthDoor: hasNorthDoor, doorwayWidth: corridorW);

                // Biome-specific 3D Prop Storytelling (Statues, Crystals, Braziers, Cursed Roots, Ruined Rubble, Boss Altar)
                ModularDungeonArchitect.DressBiomeEnvironmentProps(rRoot.transform, center, w, len, biome);

                float southZ = cursorZ;
                if (hasSouthDoor)
                {
                    room.entryGate = DungeonGate.Create(rRoot.transform, new Vector3(0f, 0f, southZ), corridorW);
                }

                float northZ = cursorZ + len;
                if (hasNorthDoor)
                {
                    room.exitGate = DungeonGate.Create(rRoot.transform, new Vector3(0f, 0f, northZ), corridorW);
                    ModularDungeonArchitect.BuildModularCorridor(rRoot.transform, northZ, corridorLen, corridorW, wallH, biome);
                }

                DecorateRoom(room, rRoot.transform, wallH);
                cursorZ = northZ + corridorLen;
            }
        }

        private void DecorateRoom(DungeonRoomRuntime room, Transform root, float wallH)
        {
            Vector3 c = room.center;
            float hl = room.length * 0.5f - 2.2f;

            switch (room.type)
            {
                case RoomType.Entrance:
                    // Entry abyssal portal behind party + starter Healing Shrine
                    BuildDecorativePortal(root, c + new Vector3(0f, 0f, -6.2f), new Color(0.65f, 0.25f, 1f));
                    WorldHealingShrine.Create(root, c + new Vector3(4.6f, 0f, 1.5f));
                    break;

                case RoomType.Combat:
                    WorldDungeonTrap.Create(root, c + new Vector3(-3.8f, 0f, 0f), false);
                    WorldDungeonTrap.Create(root, c + new Vector3(3.8f, 0f, 0f), room.index == 5);
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(0f, 0f, hl - 1.2f), room.index >= 5 ? 2 : 1);
                    WorldEnvironmentalEvent.Create(root, c + new Vector3(6.2f, 0f, -2.5f),
                        room.index == 1 ? "Tế Đàn Huyết Nguyệt" : "Giếng Hồn Vực Thẳm", room.index);
                    if (room.index == 5)
                        WorldHealingShrine.Create(root, c + new Vector3(-6.5f, 0f, hl - 1.5f));
                    break;

                case RoomType.Elite:
                    WorldDungeonTrap.Create(root, c + new Vector3(0f, 0f, -2.5f), true);
                    WorldEnvironmentalEvent.Create(root, c + new Vector3(-6.4f, 0f, 0f), "Lõi Năng Lượng Cổ Đại", room.index);
                    room.rewardChest = WorldTreasureChest.Create(root, c + new Vector3(0f, 0f, hl - 1.2f), 2);
                    break;

                case RoomType.Puzzle:
                    // 3 Rune Pillars that must be activated + Spike Traps around center
                    Vector3[] runePos =
                    {
                        c + new Vector3(-6f, 0f, -2f),
                        c + new Vector3(6f, 0f, -2f),
                        c + new Vector3(0f, 0f, 4.5f)
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

            int spawned = 0;
            for (int i = 0; i < 4; i++)
            {
                var slot = GameSession.I.Party[i];
                if (slot == null || !slot.active) continue;

                var heroGo = new GameObject($"Hero_{i}_{slot.playerName}");
                heroGo.transform.position = startCenter + offsets[Mathf.Clamp(spawned, 0, 3)];
                var unit = heroGo.AddComponent<ActorUnit>();
                bool isLocal = (spawned == 0);
                unit.InitHero(slot.classId, slot.buildId, GameSession.I.Profile.level, slot.playerName, isLocal, i);

                if (isLocal)
                {
                    LocalHero = unit;
                    heroGo.AddComponent<HeroPlayerInput>();
                    if (Camera.main != null)
                    {
                        var cam = Camera.main.GetComponent<OrbitCamera3D>();
                        if (cam == null) cam = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                        cam.Target = heroGo.transform;
                    }
                }
                else
                {
                    heroGo.AddComponent<HeroPartyAI>();
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

            // Detect player entering next room
            if (LocalHero != null && !LocalHero.IsDead)
            {
                float hz = LocalHero.transform.position.z;
                for (int i = 0; i < Rooms.Count; i++)
                {
                    var rm = Rooms[i];
                    float minZ = rm.center.z - rm.length * 0.5f + 1.4f;
                    float maxZ = rm.center.z + rm.length * 0.5f;
                    if (hz >= minZ && hz <= maxZ)
                    {
                        if (CurrentRoomIndex != i)
                        {
                            CurrentRoomIndex = i;
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
                        CurrentObjective = $"{cur.title} — Tiêu diệt quái còn lại: {cur.activeMonsters.Count}";
                        if (cur.activeMonsters.Count == 0)
                            MarkRoomCleared(cur);
                    }
                }
            }
        }

        private void ActivateRoom(int idx)
        {
            var room = Rooms[idx];
            if (room.activated) return;
            room.activated = true;

            if (room.type == RoomType.Entrance)
            {
                room.cleared = true;
                if (room.exitGate != null) room.exitGate.SetOpen(true);
                CurrentObjective = "Tiến về phía Bắc qua Cổng Sắt để bắt đầu vượt Phó Bản!";
                return;
            }

            // Lock gates during encounter & pull AI party inside room
            if (room.entryGate != null) room.entryGate.SetOpen(false);
            if (room.exitGate != null) room.exitGate.SetOpen(false);

            foreach (var h in PartyUnits)
            {
                if (h != null && !h.IsLocalPlayer && !h.IsDead)
                {
                    if (h.transform.position.z < room.center.z - room.length * 0.5f + 0.5f)
                    {
                        if (h.Controller != null) h.Controller.enabled = false;
                        h.transform.position = LocalHero.transform.position + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, 0.5f);
                        if (h.Controller != null) h.Controller.enabled = true;
                    }
                }
            }

            SpawnEncounterForRoom(room);
        }

        private void SpawnEncounterForRoom(DungeonRoomRuntime room)
        {
            int floor = GameSession.I.Profile.selectedFloor;
            var dir = AbyssDirectorAI.Instance;
            int delta = dir != null ? dir.ExtraMonsterDelta : 0;

            if (dir != null && dir.SpawnBonusHealingShrine && room.type != RoomType.Entrance)
            {
                WorldHealingShrine.Create(transform, room.center + new Vector3(-room.width * 0.32f, 0f, -room.length * 0.28f));
            }

            switch (room.type)
            {
                case RoomType.Combat:
                    {
                        string[] pool = room.index == 1
                            ? new[] { "Goblin", "GoblinArcher", "Skeleton", "Zombie" }
                            : new[] { "Skeleton", "GoblinArcher", "SkeletonMage", "ForestMonster", "Zombie", "Demon" };
                        int count = Mathf.Clamp((room.index == 1 ? 5 : 6) + delta, 3, 9);
                        for (int i = 0; i < count; i++)
                        {
                            string id = pool[i % pool.Length];
                            bool bonusElite = (dir != null && dir.PromoteBonusElite && i == count - 1);
                            Vector3 pos = RandomPointInRoom(room, 3.5f);
                            room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies[id], pos, floor, bonusElite, false));
                        }
                        break;
                    }

                case RoomType.Elite:
                    {
                        // Elite Dark Knight + Demon Assassin + Skeleton Warrior Tank + Skeleton Mage
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["DarkKnight"], room.center + new Vector3(-2.5f, 0f, 3f), floor, true, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["Demon"], room.center + new Vector3(2.5f, 0f, 3f), floor, true, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["SkeletonWarrior"], room.center + new Vector3(0f, 0f, 0.5f), floor, false, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["SkeletonMage"], room.center + new Vector3(0f, 0f, 5.5f), floor, false, false));
                        if (delta > 0)
                            room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], RandomPointInRoom(room, 4f), floor, false, false));
                        ShowBanner("CHẠM TRÁN QUÁI TINH ANH: DARK KNIGHT & DEMON!", 3.2f);
                        break;
                    }

                case RoomType.Puzzle:
                    {
                        // Guardians watching the 3 Rune Pillars
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["ForestMonster"], room.center + new Vector3(-4f, 0f, 2f), floor, false, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["ForestMonster"], room.center + new Vector3(4f, 0f, 2f), floor, false, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["SkeletonMage"], room.center + new Vector3(0f, 0f, 5f), floor, false, false));
                        ShowBanner("PHÒNG GIẢI ĐỐ: Thắp sáng cả 3 Trụ Ấn Phong Cổ [Phím F] & tiêu diệt Kẻ Canh Giữ!", 4f);
                        break;
                    }

                case RoomType.MiniBoss:
                    {
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["CrystalGolem"], room.center + new Vector3(0f, 0f, 3.5f), floor, true, true));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], room.center + new Vector3(-5.5f, 0f, 4.5f), floor, false, false));
                        room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["GoblinArcher"], room.center + new Vector3(5.5f, 0f, 4.5f), floor, false, false));
                        if (delta >= 0)
                            room.activeMonsters.Add(SpawnMonsterAt(GameDB.Enemies["SkeletonWarrior"], room.center + new Vector3(0f, 0f, 0.5f), floor, false, false));
                        ShowBanner("MINI BOSS XUẤT HIỆN: ANCIENT CRYSTAL GOLEM!", 3.5f);
                        break;
                    }

                case RoomType.Boss:
                    {
                        var bossUnit = SpawnMonsterAt(GameDB.Boss, room.center + new Vector3(0f, 0f, 5f), floor, true, false, isBoss: true);
                        var bossAi = bossUnit.GetComponent<AbyssalBossAI>();
                        bossAi.ArenaCenter = room.center;
                        bossAi.ArenaRadius = room.width * 0.5f - 1.2f;
                        room.activeMonsters.Add(bossUnit);
                        ShowBanner("BOSS CUỐI: ABYSSAL KNIGHT — CHIẾN BINH VỰC THẲM!", 4.5f);
                        break;
                    }
            }
        }

        public ActorUnit SpawnMonsterAt(EnemyDef def, Vector3 pos, int floor, bool elite, bool miniBoss, bool isBoss = false)
        {
            var dir = AbyssDirectorAI.Instance;
            float hpM = dir != null ? dir.HpMultiplier : 1f;
            float atkM = dir != null ? dir.AtkMultiplier : 1f;

            var go = new GameObject("Monster_" + def.id);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var unit = go.AddComponent<ActorUnit>();
            unit.InitMonster(def, floor, hpM, atkM, elite, miniBoss);

            if (isBoss || def.archetype == Archetype.Boss)
            {
                go.AddComponent<AbyssalBossAI>();
            }
            else
            {
                var ai = go.AddComponent<MonsterAI>();
                ai.SpawnHome = go.transform.position;
            }

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
                    EquipItem item = LootRoller.RollItem(floor, minRarity, m.IsBoss ? 0.9f : 0f);
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 0f, UnityEngine.Random.Range(-1.2f, 1.2f));
                    LootOrbPickup.Spawn(m.transform.position + offset, item);
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
            string msg = $"Nhặt [{GameDB.RarityNames[(int)item.rarity]}] {item.name} ({item.Summary()})";
            OnLootNotification?.Invoke(msg);
        }

        private void OnPuzzleRuneActivated(WorldRunePillar pillar)
        {
            GameAudio.I.PlaySfx(GameSfx.Heal, 0.9f);
            AwardExpAndGold(35, 20);
            int lit = 0;
            var room = Rooms[3];
            foreach (var p in room.puzzlePillars) if (p.Activated) lit++;
            if (pillar != null && room.exitGate != null)
            {
                RuneSealEnergyBeam.SpawnConnectorBeam(transform, pillar.Position, room.exitGate.transform.position, new Color(1f, 0.85f, 0.28f));
            }
            ShowBanner($"Đã thắp sáng Ấn Phong Cổ ({lit}/3)!", 2.5f);
        }

        private void MarkRoomCleared(DungeonRoomRuntime room)
        {
            room.cleared = true;
            if (room.entryGate != null) room.entryGate.SetOpen(true);
            if (room.exitGate != null) room.exitGate.SetOpen(true);
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

                CurrentObjective = $"Đã dọn sạch {GameDB.RoomName(room.type)}! Mở Rương [F] và tiến sang phòng tiếp theo.";
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

    public class DungeonGate : MonoBehaviour
    {
        private Transform _bars;
        private BoxCollider _blocker;
        private bool _open = true;

        public static DungeonGate Create(Transform parent, Vector3 pos, float width)
        {
            var go = new GameObject("DungeonGate");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var g = go.AddComponent<DungeonGate>();
            g._blocker = go.AddComponent<BoxCollider>();
            g._blocker.center = new Vector3(0f, 2f, 0f);
            g._blocker.size = new Vector3(width, 4f, 0.5f);

            var barsRoot = new GameObject("Bars");
            barsRoot.transform.SetParent(go.transform, false);
            g._bars = barsRoot.transform;

            ObjMeshLoader.SpawnPiece("IronPortcullis", barsRoot.transform,
                Vector3.zero, Quaternion.identity, new Vector3(width / 5.05f, 1f, 1f),
                PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);

            g.SetOpen(true, instant: true);
            return g;
        }

        public void SetOpen(bool open, bool instant = false)
        {
            _open = open;
            if (_blocker != null) _blocker.enabled = !open;
            if (instant && _bars != null)
                _bars.localPosition = new Vector3(0f, open ? 3.85f : 0f, 0f);
        }

        private void Update()
        {
            if (_bars == null) return;
            Vector3 target = new Vector3(0f, _open ? 3.85f : 0f, 0f);
            _bars.localPosition = Vector3.MoveTowards(_bars.localPosition, target, 7.5f * Time.deltaTime);
        }
    }

    public class LootOrbPickup : MonoBehaviour
    {
        public EquipItem Item;
        private float _t;

        public static void Spawn(Vector3 pos, EquipItem item)
        {
            var go = new GameObject("LootOrb_" + item.rarity);
            go.transform.position = new Vector3(pos.x, 0.55f, pos.z);
            Color col = GameDB.RarityColors[(int)item.rarity];
            var orbMat = CharacterVisualFactory.MakeMat(col, 0.9f, 0.1f, col * 2.4f);
            var beamMat = CharacterVisualFactory.MakeTransparentMat(new Color(col.r, col.g, col.b, 0.32f), col);

            string pieceName = EnemyVisualArmorKit.GetEquipmentMeshPieceName(item.slot);
            ObjMeshLoader.SpawnPiece(pieceName, go.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.95f, orbMat, addBoxCollider: false);
            CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "Beam", go.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.14f, 1.1f, 0.14f), beamMat);

            var p = go.AddComponent<LootOrbPickup>();
            p.Item = item;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, 0.55f + Mathf.Sin(_t * 3.5f) * 0.12f, transform.position.z);
            transform.Rotate(0f, 95f * Time.deltaTime, 0f);

            var dw = DungeonWorldBuilder.Instance;
            if (dw != null && dw.LocalHero != null && !dw.LocalHero.IsDead)
            {
                float d = Vector3.Distance(transform.position, dw.LocalHero.transform.position);
                if (d < 4.5f)
                    transform.position = Vector3.MoveTowards(transform.position, dw.LocalHero.transform.position + Vector3.up * 0.9f, 12f * Time.deltaTime);
                if (d < 1.5f)
                {
                    dw.CollectLootItem(Item);
                    Destroy(gameObject);
                }
            }
        }
    }

    public class WorldTreasureChest : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;

        private bool _unlocked = false;
        private bool _opened = false;
        private int _minRarity = 1;
        private Transform _lid;
        private Renderer _gemRenderer;

        public static WorldTreasureChest Create(Transform parent, Vector3 pos, int minRarity)
        {
            var go = new GameObject("TreasureChest");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Front lock faces approaching player (-Z)

            var wood = PbrMaterialLibrary.AgedOakWood();
            var iron = PbrMaterialLibrary.WroughtIron();
            var lockMat = PbrMaterialLibrary.AbyssalCrystal(new Color(0.85f, 0.16f, 0.22f), new Color(1f, 0.25f, 0.2f), 3.0f);

            ObjMeshLoader.SpawnPiece("TreasureChest_IronboundBase", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, wood, addBoxCollider: true);
            var lidPivot = new GameObject("LidPivot");
            lidPivot.transform.SetParent(go.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.54f, -0.38f);
            ObjMeshLoader.SpawnPiece("TreasureChest_IronboundLid", lidPivot.transform, Vector3.zero, Quaternion.identity, Vector3.one, iron, addBoxCollider: false);
            var gem = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "LockGem", go.transform, new Vector3(0f, 0.44f, 0.43f), Vector3.one * 0.18f, lockMat);

            var chest = go.AddComponent<WorldTreasureChest>();
            chest._minRarity = minRarity;
            chest._lid = lidPivot.transform;
            chest._gemRenderer = gem.GetComponent<Renderer>();
            HeroPlayerInput.Interactables.Add(chest);
            return chest;
        }

        public void UnlockChest()
        {
            _unlocked = true;
            if (_gemRenderer != null)
                _gemRenderer.sharedMaterial = PbrMaterialLibrary.AbyssalCrystal(new Color(0.18f, 0.92f, 0.42f), new Color(0.35f, 1f, 0.55f), 3.2f);
        }

        public bool CanInteract() => _unlocked && !_opened;
        public string GetPrompt() => "[F] Mở Rương Kho Báu";

        public void Interact(ActorUnit user)
        {
            if (!_unlocked || _opened) return;
            _opened = true;
            if (_lid != null) _lid.localRotation = Quaternion.Euler(-65f, 0f, 0f);
            GameAudio.I.PlaySfx(GameSfx.Loot, 1f);

            int floor = GameSession.I.Profile.selectedFloor;
            if (DungeonWorldBuilder.Instance != null)
                DungeonWorldBuilder.Instance.AwardExpAndGold(45 * floor, 40 * floor);

            for (int i = 0; i < 2; i++)
            {
                EquipItem it = LootRoller.RollItem(floor, _minRarity, 0.25f);
                LootOrbPickup.Spawn(transform.position + new Vector3((i == 0 ? -0.7f : 0.7f), 0f, 0.9f), it);
            }
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.25f), 2.2f, 0.5f);
        }
    }

    public class WorldHealingShrine : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        private bool _used = false;
        private Transform _orb;

        public static WorldHealingShrine Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("HealingShrine");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var stone = PbrMaterialLibrary.GothicStone(new Color(0.76f, 0.84f, 0.78f));
            var heal = PbrMaterialLibrary.AbyssalCrystal(new Color(0.16f, 0.85f, 0.46f), new Color(0.32f, 1.0f, 0.62f), 2.8f);

            ObjMeshLoader.SpawnPiece("HealingShrinePedestal", go.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.95f, stone, addBoxCollider: true);
            var orb = ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", go.transform, new Vector3(0f, 2.15f, 0f), Quaternion.identity, Vector3.one * 0.32f, heal, addBoxCollider: false);

            var sh = go.AddComponent<WorldHealingShrine>();
            sh._orb = orb.transform;
            HeroPlayerInput.Interactables.Add(sh);
            return sh;
        }

        private void Update()
        {
            if (_orb != null && !_used)
            {
                _orb.localPosition = new Vector3(0f, 2.15f + Mathf.Sin(Time.time * 2.8f) * 0.10f, 0f);
                _orb.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => !_used;
        public string GetPrompt() => "[F] Cầu nguyện Đài Hồi Phục (+70% HP & Mana cả đội)";

        public void Interact(ActorUnit user)
        {
            if (_used) return;
            _used = true;
            if (_orb != null) _orb.gameObject.SetActive(false);
            GameAudio.I.PlaySfx(GameSfx.Heal, 1f);

            foreach (var u in ActorUnit.AllUnits)
            {
                if (u.Team == Team.Hero && !u.IsDead)
                {
                    if (u.IsDowned) u.Revive(0.7f);
                    else
                    {
                        u.ReceiveHeal(u.MaxHp * 0.7f, user);
                        u.Mana = Mathf.Min(u.MaxMana, u.Mana + u.MaxMana * 0.7f);
                        u.CleanseDebuffs();
                    }
                }
            }
            GameSession.I.Profile.potions = Mathf.Min(8, GameSession.I.Profile.potions + 1);
            GameSession.I.NotifyProfileChanged();
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up, new Color(0.25f, 1f, 0.55f), 4f, 0.6f);
        }
    }

    public class WorldRunePillar : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.0f;
        public bool Activated { get; private set; }
        private int _index;
        private Renderer _crystal;
        private Action<WorldRunePillar> _onActivated;

        public static WorldRunePillar Create(Transform parent, Vector3 pos, int idx, Action<WorldRunePillar> cb)
        {
            var go = new GameObject("RunePillar_" + idx);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var stone = PbrMaterialLibrary.GothicStone(new Color(0.68f, 0.66f, 0.80f));
            var unlitRune = PbrMaterialLibrary.AbyssalCrystal(new Color(0.20f, 0.48f, 0.95f), new Color(0.38f, 0.68f, 1.0f), 2.4f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, stone, addBoxCollider: true);
            var crys = ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", go.transform, new Vector3(0f, 1.92f, 0f), Quaternion.identity, Vector3.one * 0.36f, unlitRune, addBoxCollider: false);

            var rp = go.AddComponent<WorldRunePillar>();
            rp._index = idx;
            rp._crystal = crys.GetComponentInChildren<Renderer>();
            rp._onActivated = cb;
            HeroPlayerInput.Interactables.Add(rp);
            return rp;
        }

        private void Update()
        {
            if (_crystal != null) _crystal.transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.World);
        }

        public bool CanInteract() => !Activated;
        public string GetPrompt() => $"[F] Thắp sáng Trụ Ấn Phong #{_index}";

        public void Interact(ActorUnit user)
        {
            if (Activated) return;
            Activated = true;
            if (_crystal != null)
            {
                var litMat = PbrMaterialLibrary.AbyssalCrystal(new Color(1f, 0.78f, 0.22f), new Color(1f, 0.90f, 0.35f), 3.6f);
                foreach (var r in _crystal.transform.root == transform ? GetComponentsInChildren<Renderer>() : new[] { _crystal })
                {
                    if (r.gameObject.name != "RunicPillarAltar" && r.transform.parent != transform)
                        r.sharedMaterial = litMat;
                }
                _crystal.sharedMaterial = litMat;
            }
            if (user != null) user.AddShield(user.MaxHp * 0.15f);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.8f, new Color(1f, 0.85f, 0.25f), 2.5f, 0.5f);
            _onActivated?.Invoke(this);
        }
    }

    public class WorldDungeonTrap : MonoBehaviour
    {
        private bool _fireTrap;
        private float _timer;
        private Transform _hazardVisual;

        public static WorldDungeonTrap Create(Transform parent, Vector3 pos, bool fireTrap)
        {
            var go = new GameObject(fireTrap ? "FireTrap" : "SpikeTrap");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            ObjMeshLoader.SpawnPiece("SpikeTrapPlate", go.transform,
                Vector3.zero, Quaternion.identity, Vector3.one,
                PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);

            var hazardHolder = new GameObject("Hazard");
            hazardHolder.transform.SetParent(go.transform, false);

            if (fireTrap)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = (i % 2 == 0) ? -0.58f : 0.58f;
                    float z = (i < 2) ? -0.58f : 0.58f;
                    ModularDungeonArchitect.SpawnFlameVisual(hazardHolder.transform,
                        new Vector3(x, 0.42f, z), new Vector3(0.65f, 1.05f, 0.65f),
                        new Color(1f, 0.38f, 0.08f), new Color(1f, 0.90f, 0.45f));
                }
            }
            else
            {
                ObjMeshLoader.SpawnPiece("SpikeCluster", hazardHolder.transform,
                    Vector3.zero, Quaternion.identity, Vector3.one,
                    PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);
            }
            hazardHolder.SetActive(false);

            var tr = go.AddComponent<WorldDungeonTrap>();
            tr._fireTrap = fireTrap;
            tr._hazardVisual = hazardHolder.transform;
            tr._timer = UnityEngine.Random.Range(0f, 3f);
            return tr;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float cycle = _timer % 3.6f;
            bool active = cycle > 2.6f;
            if (_hazardVisual != null && _hazardVisual.gameObject.activeSelf != active)
            {
                _hazardVisual.gameObject.SetActive(active);
                if (active) TriggerTrapDamage();
            }
        }

        private void TriggerTrapDamage()
        {
            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned)
                {
                    Vector2 flatTrap = new Vector2(transform.position.x, transform.position.z);
                    Vector2 flatHero = new Vector2(u.transform.position.x, u.transform.position.z);
                    if (Vector2.Distance(flatTrap, flatHero) <= 1.55f)
                    {
                        if (u.transform.position.y >= 0.52f)
                        {
                            CharacterVisualFactory.SpawnFloatingText(u.transform.position + Vector3.up * 1.9f, "NHẢY QUA!", new Color(0.55f, 1f, 0.78f), 0.85f);
                            continue;
                        }
                        u.ReceiveHit(u.MaxHp * 0.10f, false, null);
                        if (_fireTrap) u.ApplyStatus(StatusType.Burn, 3f, 12f, null);
                    }
                }
            }
        }
    }

    public class WorldExitPortal : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.5f;
        private string _prompt;
        private Action _onEnter;
        private Transform _ring;

        public static WorldExitPortal Create(Transform parent, Vector3 pos, string prompt, Action onEnter)
        {
            var go = new GameObject("VictoryPortal");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color col = new Color(0.55f, 0.25f, 1f);
            ObjMeshLoader.SpawnPiece("AbyssalPortal", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, PbrMaterialLibrary.GothicStone(), addBoxCollider: false);
            var ring = ObjMeshLoader.SpawnPiece("EscapeFloorPortal", go.transform, new Vector3(0f, 0.02f, 0f), Quaternion.identity, Vector3.one * 1.05f,
                PbrMaterialLibrary.AbyssalCrystal(col * 0.85f, col * 1.5f, 2.8f), addBoxCollider: false);

            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 12f;
            l.intensity = 2.5f;

            var p = go.AddComponent<WorldExitPortal>();
            p._prompt = prompt;
            p._onEnter = onEnter;
            p._ring = ring.transform;
            HeroPlayerInput.Interactables.Add(p);
            return p;
        }

        private void Update()
        {
            if (_ring != null) _ring.Rotate(0f, 80f * Time.deltaTime, 0f, Space.Self);
        }

        public bool CanInteract() => true;
        public string GetPrompt() => _prompt;
        public void Interact(ActorUnit user) => _onEnter?.Invoke();
    }
}

