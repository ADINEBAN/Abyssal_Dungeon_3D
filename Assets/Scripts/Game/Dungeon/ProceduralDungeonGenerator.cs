using System;
using System.Collections.Generic;
using UnityEngine;
using AbyssalDungeon.VFX;

namespace AbyssGame
{
    /// <summary>
    /// Phase 6: Seeded Deterministic Dungeon Layout & Pacing Generator + Environmental Events.
    /// Follows `level-design` (sawtooth tension/rest pacing curve) and `procedural-gen` (seeded RNG + graph reachability validation).
    /// </summary>
    public static class ProceduralDungeonGenerator
    {
        public struct RoomNodeSpec
        {
            public int index;
            public RoomType type;
            public DungeonBiome biome;
            public float width;
            public float length;
            public float wallHeight;
            public bool isSideBranch;
            public int branchFromIndex;
            public bool coolArcaneAccent;
            public float tensionTarget;
            public bool hasEnvironmentalEvent;
            public string eventTitle;
        }

        /// <summary>
        /// Generates a deterministic 8-room (7-purpose) Ancient Gothic Cathedral dungeon graph:
        /// Main critical path (0 Entrance -> 1 Combat Hub -> 3 Puzzle -> 4 Elite -> 5 MiniBoss -> 6 Combat -> 7 Boss Arena)
        /// PLUS an East-branching side path (1 Combat Hub -> East Vaulted Corridor -> 2 Treasure Vault).
        /// Validates full graph reachability (main + branch) via BFS.
        /// </summary>
        public static RoomNodeSpec[] GenerateSeededDungeon(int floor, int seedOffset, out bool graphValid)
        {
            int seed = (floor * 7919) ^ (seedOffset * 104729) ^ 0x5A17C3;
            var rng = new System.Random(seed);

            // Distinct widths, lengths, ceiling heights, and warm/cool lighting across all 7 purposeful zones
            var specs = new RoomNodeSpec[]
            {
                new RoomNodeSpec
                {
                    index = 0, type = RoomType.Entrance, biome = DungeonBiome.ForgottenTemple,
                    width = 20f, length = 18f, wallHeight = 7.8f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = true,
                    tensionTarget = 0.10f, hasEnvironmentalEvent = false, eventTitle = ""
                },
                new RoomNodeSpec
                {
                    index = 1, type = RoomType.Combat, biome = DungeonBiome.AncientDungeon,
                    width = 24f + rng.Next(0, 2) * 2f, length = 24f, wallHeight = 7.2f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = false,
                    tensionTarget = 0.50f, hasEnvironmentalEvent = true, eventTitle = "Tế Đàn Huyết Nguyệt"
                },
                new RoomNodeSpec
                {
                    index = 2, type = RoomType.Treasure, biome = DungeonBiome.ForgottenTemple,
                    width = 16f, length = 16f, wallHeight = 6.8f,
                    isSideBranch = true, branchFromIndex = 1, coolArcaneAccent = true,
                    tensionTarget = 0.35f, hasEnvironmentalEvent = false, eventTitle = "Hầm Bảo Vật Cổ"
                },
                new RoomNodeSpec
                {
                    index = 3, type = RoomType.Puzzle, biome = DungeonBiome.AbyssalCave,
                    width = 24f, length = 22f, wallHeight = 7.8f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = true,
                    tensionTarget = 0.48f, hasEnvironmentalEvent = false, eventTitle = "Ấn Phong Cổ Tự"
                },
                new RoomNodeSpec
                {
                    index = 4, type = RoomType.Elite, biome = DungeonBiome.UndergroundFortress,
                    width = 28f, length = 26f, wallHeight = 8.2f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = false,
                    tensionTarget = 0.85f, hasEnvironmentalEvent = true, eventTitle = "Lõi Năng Lượng Cổ Đại"
                },
                new RoomNodeSpec
                {
                    index = 5, type = RoomType.MiniBoss, biome = DungeonBiome.CrystalChamber,
                    width = 28f, length = 26f, wallHeight = 8.6f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = true,
                    tensionTarget = 0.90f, hasEnvironmentalEvent = false, eventTitle = ""
                },
                new RoomNodeSpec
                {
                    index = 6, type = RoomType.Combat, biome = DungeonBiome.CursedForest,
                    width = 26f, length = 24f, wallHeight = 7.6f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = false,
                    tensionTarget = 0.68f, hasEnvironmentalEvent = true, eventTitle = "Giếng Hồn Vực Thẳm"
                },
                new RoomNodeSpec
                {
                    index = 7, type = RoomType.Boss, biome = DungeonBiome.BossArena,
                    width = 36f, length = 36f, wallHeight = 9.4f,
                    isSideBranch = false, branchFromIndex = -1, coolArcaneAccent = true,
                    tensionTarget = 1.00f, hasEnvironmentalEvent = false, eventTitle = ""
                }
            };

            graphValid = ValidateDungeonGraphReachability(specs);
            return specs;
        }

        /// <summary>
        /// Validates that every room (both main critical path and side-branch Treasure Room) is reachable from Entrance (0) via BFS.
        /// </summary>
        public static bool ValidateDungeonGraphReachability(RoomNodeSpec[] specs)
        {
            if (specs == null || specs.Length < 2) return false;
            var adj = new List<int>[specs.Length];
            for (int i = 0; i < specs.Length; i++) adj[i] = new List<int>();

            int prevMain = -1;
            for (int i = 0; i < specs.Length; i++)
            {
                if (specs[i].isSideBranch && specs[i].branchFromIndex >= 0 && specs[i].branchFromIndex < specs.Length)
                {
                    adj[specs[i].branchFromIndex].Add(i);
                    adj[i].Add(specs[i].branchFromIndex);
                }
                else
                {
                    if (prevMain >= 0)
                    {
                        adj[prevMain].Add(i);
                        adj[i].Add(prevMain);
                    }
                    prevMain = i;
                }
            }

            var visited = new bool[specs.Length];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;
            int reached = 0;

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                reached++;
                foreach (int nb in adj[cur])
                {
                    if (!visited[nb])
                    {
                        visited[nb] = true;
                        queue.Enqueue(nb);
                    }
                }
            }
            return reached == specs.Length && visited[specs.Length - 1];
        }

        public static bool ValidateDungeonGraphReachability(int roomCount) => roomCount >= 2;
    }

    /// <summary>
    /// Interactive Environmental Event in Dungeon rooms (e.g., Blood Moon Altar, Ancient Energy Core, Abyssal Soul Well).
    /// Interacting triggers an arcane shockwave + optional ambush guardian and grants Party Buffs + Rare/Epic Loot.
    /// </summary>
    public class WorldEnvironmentalEvent : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        public bool Completed { get; private set; }

        private string _eventName;
        private int _roomIndex;
        private Transform _floatingCore;
        private Light _eventLight;
        private GameObject _idleRunesVfx;
        private GameObject _idleEnergyVfx;
        private Color _themeCol;

        public static WorldEnvironmentalEvent Create(Transform parent, Vector3 pos, string eventName, int roomIndex)
        {
            var go = new GameObject($"EnvEvent_{roomIndex}_{eventName}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color themeCol = roomIndex == 1
                ? new Color(0.95f, 0.22f, 0.32f)
                : (roomIndex == 2 ? new Color(0.25f, 0.78f, 1.0f) : new Color(0.72f, 0.28f, 1.0f));

            var stoneMat = PbrMaterialLibrary.GothicStone(new Color(0.52f, 0.48f, 0.62f));
            var crystalMat = PbrMaterialLibrary.AbyssalCrystal(themeCol, themeCol * 1.3f, 3.2f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.85f, 1.1f), stoneMat, addBoxCollider: true);
            var core = ObjMeshLoader.SpawnPiece("Equip_Amulet_Talisman", go.transform, new Vector3(0f, 1.75f, 0f), Quaternion.identity, Vector3.one * 2.2f, crystalMat, addBoxCollider: false);

            var lightGo = new GameObject("EventLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = themeCol;
            l.range = 8.5f;
            l.intensity = 2.2f;

            var ev = go.AddComponent<WorldEnvironmentalEvent>();
            ev._eventName = string.IsNullOrEmpty(eventName) ? "Tế Đàn Cổ Đại" : eventName;
            ev._roomIndex = roomIndex;
            ev._floatingCore = core.transform;
            ev._eventLight = l;
            ev._themeCol = themeCol;

            if (Application.isPlaying)
            {
                var mgr = VFXManager.EnsureInstance();
                if (mgr != null)
                {
                    ev._idleRunesVfx = mgr.SpawnLoopingVFX(VFXType.Env_FloatingRunes, go.transform, new Vector3(0f, 0.8f, 0f), Quaternion.identity, themeCol, 0.95f);
                    ev._idleEnergyVfx = mgr.SpawnLoopingVFX(VFXType.Env_MagicalEnergy, go.transform, new Vector3(0f, 0.6f, 0f), Quaternion.identity, themeCol, 0.9f);
                }
            }

            HeroPlayerInput.Interactables.Add(ev);
            return ev;
        }

        private void Update()
        {
            if (_floatingCore != null && !Completed)
            {
                _floatingCore.localPosition = new Vector3(0f, 1.75f + Mathf.Sin(Time.time * 3.2f) * 0.12f, 0f);
                _floatingCore.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => !Completed;
        public string GetPrompt() => $"[F] Kích hoạt Sự Kiện: {_eventName} (Thử thách & Buff Tổ Đội)";

        public void Interact(ActorUnit user)
        {
            if (Completed) return;
            Completed = true;

            if (_eventLight != null) _eventLight.intensity = 0.5f;
            if (_floatingCore != null) _floatingCore.localScale *= 0.65f;

            var mgr = VFXManager.EnsureInstance();
            if (mgr != null)
            {
                if (_idleRunesVfx != null) { mgr.ReleaseVFX(VFXType.Env_FloatingRunes, _idleRunesVfx); _idleRunesVfx = null; }
                if (_idleEnergyVfx != null) { mgr.ReleaseVFX(VFXType.Env_MagicalEnergy, _idleEnergyVfx); _idleEnergyVfx = null; }
                mgr.SpawnVFX(VFXType.Env_AncientAltarActivation, transform.position, Quaternion.identity, 2.2f, _themeCol, null, 1.0f);
                mgr.SpawnVFX(VFXType.Env_GroundDustBurst, transform.position + Vector3.up * 0.05f, Quaternion.identity, 1.6f, null, null, 0.9f);
            }

            GameAudio.I.PlaySfx(GameSfx.Heal, 1f);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(1f, 0.65f, 0.22f), 4.2f, 0.65f);

            var dw = DungeonWorldBuilder.Instance;
            if (dw != null)
            {
                // Grant Party-wide Haste + Shield + Empower buff
                foreach (var h in dw.PartyUnits)
                {
                    if (h != null && !h.IsDead)
                    {
                        h.AddShield(h.MaxHp * 0.25f);
                        h.ApplyStatus(StatusType.Rage, 14f, 0.25f, user);
                        h.ReceiveHeal(h.MaxHp * 0.25f, user);
                        CharacterVisualFactory.SpawnFloatingText(h.transform.position + Vector3.up * 2.3f,
                            $"PHƯỚC LÀNH {_eventName.ToUpper()}!", new Color(1f, 0.86f, 0.32f), 1.05f);
                    }
                }

                int floor = GameSession.I.Profile.selectedFloor;
                dw.AwardExpAndGold(60 * floor, 55 * floor);
                EquipItem reward = LootRoller.RollItem(floor, 2, 0.35f);
                LootOrbPickup.Spawn(transform.position + new Vector3(0f, 0f, -1.1f), reward);
                dw.ShowBanner($"SỰ KIỆN HOÀN TẤT: {_eventName} — Toàn đội nhận Lá Chắn, Tốc Độ & Trang Bị Hiếm!", 3.6f);
            }
        }
    }

    /// <summary>
    /// Visual energy beam connecting activated Rune Pillars to the North Gate in the Puzzle Room.
    /// </summary>
    public class RuneSealEnergyBeam : MonoBehaviour
    {
        public static void SpawnConnectorBeam(Transform parent, Vector3 fromPos, Vector3 toPos, Color color)
        {
            Vector3 start = fromPos + Vector3.up * 1.85f;
            Vector3 end = toPos + Vector3.up * 2.1f;
            Vector3 diff = end - start;
            float dist = diff.magnitude;
            if (dist < 0.2f) return;

            var beamMat = CharacterVisualFactory.MakeTransparentMat(new Color(color.r, color.g, color.b, 0.55f), color * 2.6f);
            var beam = CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "RuneEnergyBeam", parent,
                (start + end) * 0.5f, new Vector3(0.10f, dist * 0.5f, 0.10f),
                beamMat, Quaternion.FromToRotation(Vector3.up, diff.normalized));
            beam.AddComponent<RuneSealEnergyBeam>();
        }
    }
}
