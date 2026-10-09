using System;
using UnityEngine;
using AbyssalDungeon.Classes;

namespace AbyssalDungeon.LAN
{
    [Serializable] public class LanPacket
    {
        public string type, text;
        public int id, choice, build;
        public bool ready;
        public LanInput input;
        public LanWorld world;
    }
    [Serializable] public class LanInput
    {
        public float x, z, yaw;
        public int action; // 1 attack, 2-5 skills, 6 dodge, 7 interact, 8 equip, 9 upgrade
        public int item;
    }
    [Serializable] public class LanItem
    {
        public int slot, rarity, power;
        public string name;
    }
    [Serializable] public class LanActor
    {
        public int id, classId, build, level = 1, xp, gold, kills, deaths, points, action, actionSeq;
        public string name;
        public bool ready, connected = true;
        public Vector3 position;
        public float yaw, hp, maxHp, mana, maxMana, shield, attack, defense;
        public float dealt, taken, healed, downTime, dodgeUntil, actionUntil;
        public float[] cooldowns = new float[6];
        public LanItem[] bag = new LanItem[0];
        public LanItem[] equipped = new LanItem[5];
        public int form;
    }
    [Serializable] public class LanEnemy
    {
        public int id, kind, phase = 1, action, actionSeq;
        public Vector3 position, aim;
        public float yaw, hp, maxHp, attack, nextAttack, castUntil, slowUntil, burnUntil, burnTick;
        public int burnOwner;
        public bool elite, boss;
    }
    [Serializable] public class LanEffect
    {
        public int id, kind, owner;
        public Vector3 position, target;
        public float radius, time;
        public string text;
    }
    [Serializable] public class LanWorld
    {
        public int tick, room = -1, kills, rune, run = 1;
        public string stage = "Lobby", objective = "Chon class va san sang", message = "";
        public float elapsed, roomTime, difficulty = 1, totalDealt, totalTaken, totalHealing;
        public LanActor[] players = new LanActor[0];
        public LanEnemy[] enemies = new LanEnemy[0];
        public LanEffect[] effects = new LanEffect[0];
    }
    [CreateAssetMenu(menuName = "Abyssal Dungeon/LAN Catalog")]
    public class LanCatalog : ScriptableObject
    {
        public ClassData[] classes;
        public GameObject[] warriors, enemies;
        public GameObject boss;
        public Material stone, metal, gold, crystal, cloth, danger;
    }
    public static class LanRules
    {
        public const int Protocol = 1, MaxPlayers = 4, MinPlayers = 3, DefaultPort = 7777;
        public static readonly string[] Classes = { "Knight", "Guardian", "Berserker", "Assassin", "Ranger", "Mage", "Warlock", "Monk", "Druid", "Cleric" };
        public static readonly string[] Roles = { "Tank / Dau si", "Tank / Ho tro", "Can chien / Cuong no", "Sat thu / Co dong", "Cung thu / Bay", "Phep / Dien rong", "Nguyen rua / Trieu hoi", "Vo su / Ho tro", "Bien hinh / Hoi mau", "Hoi mau / Hoi sinh" };
        public static readonly string[] Rooms = { "CONG VUC THAM", "HAM MO TINH ANH", "PHONG AN PHONG", "KE GIU CONG", "HANH LANG BONG TOI", "DIEN THO VUC THAM" };
        public static readonly string[] Rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        public static readonly string[] Slots = { "Vu khi", "Giap", "Mu", "Nhan", "Amulet" };
        public static readonly string[,] Skills = {
            { "Shield Bash", "Iron Guard", "Taunt", "Judgement" },
            { "Shield Wave", "Sanctuary", "Aegis", "Citadel" },
            { "Cleave", "Bloodlust", "War Cry", "Ragnarok" },
            { "Backstab", "Shadow Step", "Venom", "Death Blossom" },
            { "Piercing Shot", "Frost Trap", "Volley", "Arrow Storm" },
            { "Fireball", "Frost Nova", "Arcane Pulse", "Meteor" },
            { "Soul Bolt", "Curse", "Soul Minion", "Cataclysm" },
            { "Flurry", "Zen Mend", "Palm Strike", "Dragon Fist" },
            { "Thorns", "Regrowth", "Bear Form", "Nature's Wrath" },
            { "Smite", "Group Heal", "Purify", "Resurrection" }
        };
        public static string BuildName(int cls, int build)
        {
            if (cls == 5) return new[] { "Fire", "Frost", "Arcane" }[Mathf.Clamp(build, 0, 2)];
            if (cls == 0) return new[] { "Tank", "Paladin", "Dark Knight" }[Mathf.Clamp(build, 0, 2)];
            return new[] { "Can bang", "Tan cong", "Ho tro" }[Mathf.Clamp(build, 0, 2)];
        }
        public static Color ClassColor(int i)
        {
            Color[] c = { new Color(.3f,.65f,1), new Color(.9f,.73f,.32f), new Color(.8f,.17f,.13f), new Color(.7f,.3f,.85f), new Color(.27f,.7f,.36f), new Color(.2f,.8f,1), new Color(.6f,.18f,.95f), new Color(1,.65f,.2f), new Color(.2f,.85f,.6f), new Color(1,.89f,.63f) };
            return c[Mathf.Clamp(i, 0, 9)];
        }
        public static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
        public static bool CanStart(LanWorld w)
        {
            if (w.players.Length < MinPlayers || w.players.Length > MaxPlayers) return false;
            foreach (var p in w.players) if (!p.ready || !p.connected) return false;
            return w.stage == "Lobby" || w.stage == "Town";
        }
        public static bool Ranged(int c) { return c == 4 || c == 5 || c == 6 || c == 8 || c == 9; }
        public static float RoomZ(int room) { return room * 32; }
        public static string CleanName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Hunter";
            s = s.Replace("<", "").Replace(">", "").Replace("\n", "").Replace("\r", "").Trim();
            return s.Substring(0, Math.Min(18, s.Length));
        }
    }
}
