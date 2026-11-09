using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssGame
{
    [Serializable]
    public class EquipItem
    {
        public string uid;
        public string name;
        public ItemSlot slot;
        public Rarity rarity;
        public EquipArchetype archetype = EquipArchetype.Universal;
        public int level;
        public float atk, def, hp, mana, crit, atkSpeed, lifesteal, cdr;
        public int value;

        public float Score()
        {
            return atk * 2.2f + def * 1.8f + hp * 0.25f + mana * 0.12f + crit * 450f + atkSpeed * 180f + lifesteal * 500f + cdr * 400f;
        }

        public float ScoreForClass(int classId)
        {
            if (!ClassSpecializationSystem.EvaluateCompatibility(classId, this, out float mult, out _))
                return -1f;
            return Score() * mult;
        }

        public string Summary()
        {
            var parts = new List<string>();
            if (archetype != EquipArchetype.Universal && slot != ItemSlot.Ring && slot != ItemSlot.Amulet)
                parts.Add($"[{ClassSpecializationSystem.GetArchetypeShortTag(archetype)}]");
            if (atk > 0.1f) parts.Add($"+{atk:0} ATK");
            if (def > 0.1f) parts.Add($"+{def:0} DEF");
            if (hp > 0.5f) parts.Add($"+{hp:0} HP");
            if (mana > 0.5f) parts.Add($"+{mana:0} MP");
            if (crit > 0.001f) parts.Add($"+{crit * 100:0.#}% Crit");
            if (atkSpeed > 0.001f) parts.Add($"+{atkSpeed * 100:0.#}% ASPD");
            if (lifesteal > 0.001f) parts.Add($"+{lifesteal * 100:0.#}% Hút máu");
            if (cdr > 0.001f) parts.Add($"+{cdr * 100:0.#}% Giảm hồi chiêu");
            return string.Join("  ", parts);
        }
    }

    [Serializable]
    public class PartySlotInfo
    {
        public bool active;
        public bool isHuman;
        public string playerName;
        public int classId;
        public int buildId;
        public int level = 1;
        public bool ready;
    }

    [Serializable]
    public class RunReport
    {
        public bool victory;
        public int floor = 1;
        public float elapsedTime;
        public int monstersKilled;
        public int roomsCleared;
        public int totalRooms = 7;
        public int playerDeaths;
        public float damageDealt;
        public float damageTaken;
        public float healingDone;
        public int expGained;
        public int goldGained;
        public int startLevel = 1;
        public int endLevel = 1;
        public float directorScore = 50f;
        public string directorVerdict = "Cân bằng";
        public List<EquipItem> lootedItems = new List<EquipItem>();
    }

    [Serializable]
    public class PersistentProfile
    {
        public string heroName = "Aether Hunter";
        public int selectedClass = 0;
        public int selectedBuild = 0;
        public int level = 1;
        public float exp = 0f;
        public int gold = 150;
        public int statPoints = 0;
        public int skillPoints = 1;
        public int[] skillRanks = new int[] { 1, 1, 1, 1 };
        public int bonusHpPts = 0;
        public int bonusAtkPts = 0;
        public int bonusDefPts = 0;
        public int bonusHastePts = 0;
        public int potions = 5;
        public int highestFloor = 1;
        public int selectedFloor = 1;
        public List<EquipItem> equipped = new List<EquipItem>(); // up to 5 slots stored with slot field
        public List<EquipItem> bag = new List<EquipItem>();
        public float masterVol = 0.85f;
        public float musicVol = 0.55f;
        public float sfxVol = 0.85f;
        public float mouseSens = 2.2f;
        public float cameraDistance = 7.8f;
        public bool invertCamY = false;
    }

    public static class LootRoller
    {
        private static readonly string[][] RingAmuletNames =
        {
            new[] { "Nhẫn Sức Mạnh", "Nhẫn Hút Hồn", "Nhẫn Tốc Chiến", "Nhẫn Băng Giá", "Nhẫn Vĩnh Hằng" },
            new[] { "Dây Chuyền Aether", "Bùa Hộ Mệnh Cổ", "Ấn Thánh Quang", "Trái Tim Vực Thẳm", "Huyết Ngọc" }
        };

        private static string PickArchetypeGearName(ItemSlot slot, EquipArchetype arch)
        {
            switch (arch)
            {
                case EquipArchetype.VanguardPlate:
                    if (slot == ItemSlot.Weapon)
                        return new[] { "Kiếm Khiên Kỵ Sĩ", "Búa Chiến Thánh Điện", "Đại Đao Huyết Nộ", "Trọng Kiếm Vực Thẳm" }[UnityEngine.Random.Range(0, 4)];
                    if (slot == ItemSlot.Armor)
                        return new[] { "Chiến Giáp Pháo Đài", "Thiết Giáp Hộ Vệ", "Giáp Vảy Hắc Long" }[UnityEngine.Random.Range(0, 3)];
                    return new[] { "Mũ Trụ Kỵ Sĩ", "Mũ Giáp Sừng Quỷ", "Mũ Hộ Vệ Cổ Ngữ" }[UnityEngine.Random.Range(0, 3)];

                case EquipArchetype.StalkerLeather:
                    if (slot == ItemSlot.Weapon)
                        return new[] { "Song Đoản Ảnh Sát", "Cung Phong Linh", "Thiết Quyền Hộ Thủ", "Đoản Kiếm Độc Long" }[UnityEngine.Random.Range(0, 4)];
                    if (slot == ItemSlot.Armor)
                        return new[] { "Giáp Da Bóng Đêm", "Chiến Y Thợ Săn", "Võ Phục Nội Công" }[UnityEngine.Random.Range(0, 3)];
                    return new[] { "Mũ Trùm Ảnh Vũ", "Mũ Xạ Thủ Ưng Nhãn", "Băng Trán Thiền Sư" }[UnityEngine.Random.Range(0, 3)];

                case EquipArchetype.ArcanistCloth:
                    if (slot == ItemSlot.Weapon)
                        return new[] { "Trượng Tinh Vân", "Quyền Trượng Hắc Hồn", "Pháp Trượng Thiên Thạch", "Đoản Trượng Nguyền Rủa" }[UnityEngine.Random.Range(0, 4)];
                    if (slot == ItemSlot.Armor)
                        return new[] { "Pháp Bào Tinh Tú", "Áo Choàng Hắc Ám", "Lễ Phục Bí Thuật" }[UnityEngine.Random.Range(0, 3)];
                    return new[] { "Mũ Chóp Nguyên Tố", "Vương Miện Hắc Ma", "Miện Tinh Cầu" }[UnityEngine.Random.Range(0, 3)];

                case EquipArchetype.SacredRelic:
                    if (slot == ItemSlot.Weapon)
                        return new[] { "Trượng Nguyệt Thực", "Chùy Thánh Quang", "Quyền Trượng Rừng Già", "Thánh Chùy Phán Quyết" }[UnityEngine.Random.Range(0, 4)];
                    if (slot == ItemSlot.Armor)
                        return new[] { "Thánh Y Tối Cao", "Giáp Rừng Nguyên Sinh", "Lễ Phục Ánh Sáng" }[UnityEngine.Random.Range(0, 3)];
                    return new[] { "Mũ Sừng Lộc Thần", "Mũ Miện Thánh Sứ", "Vòng Nguyệt Quế Cổ" }[UnityEngine.Random.Range(0, 3)];
            }
            return "Trang Bị Aether";
        }

        private static readonly string[] RarityPrefix =
        {
            "Thô", "Tinh Chế", "Hiếm", "Sử Thi", "Huyền Thoại", "Thần Thoại"
        };

        public static EquipItem RollItem(int floor, int minRarity = 0, float luckBonus = 0f)
        {
            float r = UnityEngine.Random.value - luckBonus * 0.12f;
            Rarity rar;
            if (r < 0.025f) rar = Rarity.Mythic;
            else if (r < 0.09f) rar = Rarity.Legendary;
            else if (r < 0.22f) rar = Rarity.Epic;
            else if (r < 0.45f) rar = Rarity.Rare;
            else if (r < 0.72f) rar = Rarity.Uncommon;
            else rar = Rarity.Common;

            if ((int)rar < minRarity) rar = (Rarity)Mathf.Clamp(minRarity, 0, 5);

            ItemSlot slot = (ItemSlot)UnityEngine.Random.Range(0, 5);
            int ri = (int)rar;
            float mult = (1f + ri * 0.55f) * (1f + (floor - 1) * 0.25f);

            EquipArchetype arch = EquipArchetype.Universal;
            string baseName;
            if (slot == ItemSlot.Ring || slot == ItemSlot.Amulet)
            {
                int idx = slot == ItemSlot.Ring ? 0 : 1;
                baseName = RingAmuletNames[idx][UnityEngine.Random.Range(0, RingAmuletNames[idx].Length)];
            }
            else
            {
                int curCls = GameSession.I != null ? GameSession.I.Profile.selectedClass : 0;
                arch = UnityEngine.Random.value < 0.65f
                    ? ClassSpecializationSystem.GetClassArchetype(curCls)
                    : (EquipArchetype)UnityEngine.Random.Range(1, 5);
                baseName = PickArchetypeGearName(slot, arch);
            }

            var it = new EquipItem
            {
                uid = Guid.NewGuid().ToString("N").Substring(0, 8),
                name = $"{baseName} [{RarityPrefix[ri]}]",
                slot = slot,
                rarity = rar,
                archetype = arch,
                level = Mathf.Max(1, floor),
                value = Mathf.RoundToInt(20 * mult)
            };

            switch (slot)
            {
                case ItemSlot.Weapon:
                    it.atk = Mathf.Round(14f * mult);
                    it.crit = ri >= 2 ? 0.03f + ri * 0.015f : 0f;
                    it.atkSpeed = ri >= 1 ? 0.04f + ri * 0.02f : 0f;
                    if (ri >= 4) it.lifesteal = 0.03f + (ri - 3) * 0.025f;
                    break;
                case ItemSlot.Armor:
                    it.def = Mathf.Round(10f * mult);
                    it.hp = Mathf.Round(75f * mult);
                    if (ri >= 3) it.cdr = 0.04f + (ri - 2) * 0.02f;
                    break;
                case ItemSlot.Helmet:
                    it.def = Mathf.Round(6f * mult);
                    it.hp = Mathf.Round(55f * mult);
                    it.mana = Mathf.Round(35f * mult);
                    if (ri >= 2) it.cdr = 0.03f + ri * 0.015f;
                    break;
                case ItemSlot.Ring:
                    it.atk = Mathf.Round(8f * mult);
                    it.crit = 0.03f + ri * 0.02f;
                    it.atkSpeed = 0.03f + ri * 0.02f;
                    if (ri >= 3) it.lifesteal = 0.02f + ri * 0.012f;
                    break;
                case ItemSlot.Amulet:
                    it.hp = Mathf.Round(60f * mult);
                    it.mana = Mathf.Round(50f * mult);
                    it.atk = Mathf.Round(6f * mult);
                    it.cdr = 0.04f + ri * 0.025f;
                    break;
            }
            return it;
        }
    }

    /// <summary>Global persistent session across MainMenu, Lobby, ClassSelection, Dungeon, EndGame, Town.</summary>
    public class GameSession : MonoBehaviour
    {
        private static GameSession _inst;
        public static GameSession I
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<GameSession>();
                    if (_inst == null)
                    {
                        var go = new GameObject("[GameSession]");
                        DontDestroyOnLoad(go);
                        _inst = go.AddComponent<GameSession>();
                    }
                }
                return _inst;
            }
        }

        public PersistentProfile Profile = new PersistentProfile();
        public PartySlotInfo[] Party = new PartySlotInfo[4];
        public RunReport LastRun = new RunReport();
        public string ReturnSceneAfterClassSelect = "Lobby";
        public bool RoomCreated = false;
        public string RoomCode = "ABYSS-01";

        public event Action OnProfileChanged;
        public event Action OnPartyChanged;

        private const string SaveKey = "AbyssalDungeon3D_Profile_v2";

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            DontDestroyOnLoad(gameObject);
            LoadProfile();
            EnsureDefaultParty();
        }

        public void EnsureDefaultParty()
        {
            if (Party == null || Party.Length != 4) Party = new PartySlotInfo[4];
            for (int i = 0; i < 4; i++)
                if (Party[i] == null) Party[i] = new PartySlotInfo();

            if (!Party[0].active)
            {
                CreateDefaultRoom();
            }
        }

        public void CreateDefaultRoom()
        {
            RoomCreated = true;
            RoomCode = "ABYSS-" + UnityEngine.Random.Range(100, 999);
            Party[0] = new PartySlotInfo
            {
                active = true, isHuman = true, playerName = Profile.heroName,
                classId = Profile.selectedClass, buildId = Profile.selectedBuild,
                level = Profile.level, ready = true
            };
            // Default balanced 4-hunter party: Knight/Selected + Mage + Ranger + Cleric
            int[] aiClasses = PickComplementaryClasses(Profile.selectedClass);
            string[] aiNames = { "Lyra (AI)", "Kael (AI)", "Seraphina (AI)" };
            for (int i = 0; i < 3; i++)
            {
                Party[i + 1] = new PartySlotInfo
                {
                    active = true, isHuman = false, playerName = aiNames[i],
                    classId = aiClasses[i], buildId = 0, level = Mathf.Max(1, Profile.level), ready = true
                };
            }
            NotifyPartyChanged();
        }

        private int[] PickComplementaryClasses(int leaderClass)
        {
            // Ensure Tank + AoE Mage + Ranger + Healer Cleric coverage
            var list = new List<int> { 0, 5, 4, 9, 1, 8, 2 };
            list.Remove(leaderClass);
            return new[] { list[0], list[1], list[2] };
        }

        public void ApplyPartyPreset(int presetIndex)
        {
            // 0: Balanced (Knight + Mage + Ranger + Cleric)
            // 1: Offense  (Berserker + Assassin + Mage + Ranger)
            // 2: Defense  (Knight + Guardian + Druid + Cleric)
            // 3: Control  (Guardian + Mage + Warlock + Ranger)
            int[][] presets =
            {
                new[] { 0, 5, 4, 9 },
                new[] { 2, 3, 5, 4 },
                new[] { 0, 1, 8, 9 },
                new[] { 1, 5, 6, 4 }
            };
            int[] p = presets[Mathf.Clamp(presetIndex, 0, presets.Length - 1)];
            string[] aiNames = { "Hunter", "Lyra (AI)", "Kael (AI)", "Seraphina (AI)" };

            Profile.selectedClass = p[0];
            for (int i = 0; i < 4; i++)
            {
                Party[i] = new PartySlotInfo
                {
                    active = true,
                    isHuman = (i == 0),
                    playerName = i == 0 ? Profile.heroName : aiNames[i],
                    classId = p[i],
                    buildId = 0,
                    level = Mathf.Max(1, Profile.level),
                    ready = true
                };
            }
            RoomCreated = true;
            SaveProfile();
            NotifyPartyChanged();
        }

        public void SetPartySize(int count)
        {
            count = Mathf.Clamp(count, 3, 4);
            for (int i = 0; i < 4; i++)
            {
                if (i < count)
                {
                    if (!Party[i].active)
                    {
                        Party[i].active = true;
                        Party[i].isHuman = (i == 0);
                        Party[i].playerName = i == 0 ? Profile.heroName : $"Hunter {i + 1} (AI)";
                        Party[i].classId = (Profile.selectedClass + i * 3) % 10;
                        Party[i].level = Profile.level;
                        Party[i].ready = true;
                    }
                }
                else
                {
                    Party[i].active = false;
                }
            }
            NotifyPartyChanged();
        }

        public int ActivePartyCount()
        {
            int n = 0;
            for (int i = 0; i < 4; i++) if (Party[i] != null && Party[i].active) n++;
            return n;
        }

        public bool AllPartyReady()
        {
            int n = 0;
            for (int i = 0; i < 4; i++)
            {
                if (Party[i] != null && Party[i].active)
                {
                    if (!Party[i].ready) return false;
                    n++;
                }
            }
            return n >= 3;
        }

        public void SelectLocalClass(int classId, int buildId)
        {
            Profile.selectedClass = Mathf.Clamp(classId, 0, 9);
            Profile.selectedBuild = Mathf.Clamp(buildId, 0, 2);

            // Auto-unequip incompatible Weapon back into bag when changing Class Archetype
            EquipItem weap = GetEquipped(ItemSlot.Weapon);
            if (weap != null && !ClassSpecializationSystem.EvaluateCompatibility(Profile.selectedClass, weap, out _, out _))
            {
                Profile.equipped.Remove(weap);
                Profile.bag.Add(weap);
            }

            EnsureDefaultParty();
            Party[0].classId = Profile.selectedClass;
            Party[0].buildId = Profile.selectedBuild;
            Party[0].level = Profile.level;
            SaveProfile();
            NotifyPartyChanged();
            OnProfileChanged?.Invoke();
        }

        public EquipItem GetEquipped(ItemSlot slot)
        {
            return Profile.equipped.Find(e => e != null && e.slot == slot);
        }

        public void AddItemToBag(EquipItem item, bool autoEquipIfBetter = true)
        {
            if (item == null) return;
            int cid = Profile.selectedClass;
            bool compat = ClassSpecializationSystem.EvaluateCompatibility(cid, item, out _, out _);
            EquipItem cur = GetEquipped(item.slot);
            float newScore = item.ScoreForClass(cid);
            float curScore = cur != null ? cur.ScoreForClass(cid) : 0f;

            if (autoEquipIfBetter && compat && (cur == null || newScore > curScore))
            {
                Equip(item);
            }
            else
            {
                Profile.bag.Add(item);
            }
            SaveProfile();
            OnProfileChanged?.Invoke();
        }

        public bool Equip(EquipItem item)
        {
            if (item == null) return false;
            if (!ClassSpecializationSystem.EvaluateCompatibility(Profile.selectedClass, item, out _, out _))
            {
                if (!Profile.bag.Contains(item)) Profile.bag.Add(item);
                return false;
            }
            Profile.bag.Remove(item);
            EquipItem cur = GetEquipped(item.slot);
            if (cur != null)
            {
                Profile.equipped.Remove(cur);
                Profile.bag.Add(cur);
            }
            Profile.equipped.Add(item);
            SaveProfile();
            OnProfileChanged?.Invoke();
            return true;
        }

        public void SellBagItem(EquipItem item)
        {
            if (item == null) return;
            if (Profile.bag.Remove(item))
            {
                Profile.gold += Mathf.Max(5, item.value);
                SaveProfile();
                OnProfileChanged?.Invoke();
            }
        }

        public bool AddExp(int amount)
        {
            if (amount <= 0) return false;
            Profile.exp += amount;
            bool leveled = false;
            while (Profile.level < 50 && Profile.exp >= GameDB.ExpToNext(Profile.level))
            {
                Profile.exp -= GameDB.ExpToNext(Profile.level);
                Profile.level++;
                Profile.statPoints += 2;
                Profile.skillPoints += 1;
                leveled = true;
            }
            for (int i = 0; i < 4; i++)
                if (Party[i] != null && Party[i].active) Party[i].level = Profile.level;
            SaveProfile();
            OnProfileChanged?.Invoke();
            return leveled;
        }

        public void SpendStatPoint(int statIndex)
        {
            if (Profile.statPoints <= 0) return;
            Profile.statPoints--;
            switch (statIndex)
            {
                case 0: Profile.bonusHpPts++; break;
                case 1: Profile.bonusAtkPts++; break;
                case 2: Profile.bonusDefPts++; break;
                case 3: Profile.bonusHastePts++; break;
            }
            SaveProfile();
            OnProfileChanged?.Invoke();
        }

        public bool SpendSkillPoint(int slot)
        {
            EnsureSkillRanksValid();
            if (slot < 0 || slot >= 4 || Profile.skillPoints <= 0) return false;
            if (Profile.skillRanks[slot] >= 5) return false;
            Profile.skillPoints--;
            Profile.skillRanks[slot]++;
            SaveProfile();
            OnProfileChanged?.Invoke();
            return true;
        }

        private void EnsureSkillRanksValid()
        {
            if (Profile.skillRanks == null || Profile.skillRanks.Length != 4)
                Profile.skillRanks = new int[] { 1, 1, 1, 1 };
            for (int i = 0; i < 4; i++)
                Profile.skillRanks[i] = Mathf.Clamp(Profile.skillRanks[i], 1, 5);
        }

        public void NotifyPartyChanged() => OnPartyChanged?.Invoke();
        public void NotifyProfileChanged() => OnProfileChanged?.Invoke();

        public void SaveProfile()
        {
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Profile));
                PlayerPrefs.Save();
                AtomicSaveSystem.SaveAtomic(Profile);
            }
            catch (Exception e) { Debug.LogWarning("[GameSession] Save failed: " + e.Message); }
        }

        public void LoadProfile()
        {
            if (AtomicSaveSystem.TryLoadAtomic(out PersistentProfile atomicLoaded) && atomicLoaded != null)
            {
                if (atomicLoaded.cameraDistance < 3.4f || atomicLoaded.cameraDistance > 15f)
                    atomicLoaded.cameraDistance = 7.8f;
                Profile = atomicLoaded;
            }
            else if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<PersistentProfile>(PlayerPrefs.GetString(SaveKey));
                    if (loaded != null)
                    {
                        if (loaded.cameraDistance < 3.4f || loaded.cameraDistance > 15f)
                            loaded.cameraDistance = 7.8f;
                        Profile = loaded;
                    }
                }
                catch { Profile = new PersistentProfile(); }
            }
            EnsureSkillRanksValid();
            AudioListener.volume = Profile.masterVol;
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            AtomicSaveSystem.DeleteAtomicSaves();
            Profile = new PersistentProfile();
            CreateDefaultRoom();
            OnProfileChanged?.Invoke();
        }

        public static void GoToScene(string sceneName)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                // Fallback if user opened SampleScene before BuildSettings refreshed: swap bootstrap mode in-place
                SceneBootstrap.LoadVirtualScene(sceneName);
            }
        }
    }
}

