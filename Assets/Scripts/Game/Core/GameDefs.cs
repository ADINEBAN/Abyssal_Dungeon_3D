using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    public enum Team { Hero, Monster }
    public enum Role { Tank, MeleeDPS, RangedDPS, Healer, Support, Hybrid }

    public enum SkillKind
    {
        MeleeArc,      // hit enemies in a cone in front
        Projectile,    // fire projectile(s)
        GroundAoE,     // delayed explosion at aim point
        SelfAoE,       // explosion around caster
        Dash,          // dash forward damaging along the path
        HealAll,       // heal allies around caster
        HealTarget,    // heal lowest HP ally
        ShieldAll,     // shield allies around caster
        ShieldSelf,    // shield on caster
        BuffSelf,      // status on self
        BuffAll,       // status on allies around
        Taunt,         // force monsters to target caster
        Summon,        // summon a minion
        Revive,        // revive downed allies
        Trap,          // place a trap at aim point
        Transform,     // shapeshift (Druid)
        Cleanse        // remove debuffs + small shield
    }

    public enum StatusType { None, Burn, Poison, Slow, Freeze, Stun, Curse, AtkUp, DefUp, SpeedUp, Regen, Rage, Taunted }
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
    public enum ItemSlot { Weapon, Armor, Helmet, Ring, Amulet }
    public enum WeaponKind { None, SwordShield, GreatSword, Daggers, Bow, Staff, Wand, Fists, Mace, TowerShield, Claws }
    public enum ModelKind { Kachujin, Paladin, Vanguard, Golem, Treant }
    public enum Archetype { Melee, Ranged, Mage, Tank, Assassin, Elite, MiniBoss, Boss }
    public enum RoomType { Entrance, Combat, Elite, Puzzle, MiniBoss, Boss }

    public class SkillDef
    {
        public string name;
        public string desc;
        public SkillKind kind;
        public float cooldown = 6f;
        public float mana = 15f;
        public float power = 1.5f;      // multiplier of ATK (damage / heal / shield)
        public float radius = 3f;
        public float range = 10f;
        public float arc = 100f;
        public int count = 1;
        public bool pierce;
        public StatusType status = StatusType.None;
        public float statusDur = 0f;
        public float statusPower = 0f;
        public bool useCastAnim = true;
        public bool ultimate;
        public Color color = Color.white;
        public StatusType selfStatus = StatusType.None; // secondary effect on caster
        public float selfStatusDur;
    }

    public class ClassDef
    {
        public int id;
        public string name;
        public Role role;
        public string roleText;
        public string desc;
        public float hp, mana, atk, def, atkSpeed, crit, moveSpeed;
        public WeaponKind weapon;
        public ModelKind model;
        public Color color;
        public bool ranged;
        public float attackRange;
        public SkillDef[] skills;
        public string[] builds;
        public string[] buildDesc;
        public string passive;
    }

    public class EnemyDef
    {
        public string id;
        public string name;
        public Archetype archetype;
        public float hp, atk, def, speed, detect, attackRange, attackCd;
        public int exp, gold;
        public ModelKind model;
        public Color tint;
        public float scale = 1f;
        public WeaponKind weapon;
        public string accessory = "";
        public Color projColor = Color.red;
        public float animSpeed = 1f;
    }

    /// <summary>Static game database: 10 classes, 40 skills, 10 monsters + boss.</summary>
    public static class GameDB
    {
        public static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        public static readonly Color[] RarityColors =
        {
            new Color(0.80f, 0.80f, 0.80f), new Color(0.35f, 0.90f, 0.35f), new Color(0.30f, 0.60f, 1.00f),
            new Color(0.72f, 0.38f, 1.00f), new Color(1.00f, 0.60f, 0.10f), new Color(1.00f, 0.25f, 0.40f)
        };
        public static readonly string[] SlotNames = { "Vũ khí", "Giáp", "Mũ", "Nhẫn", "Amulet" };

        public static readonly ClassDef[] Classes;
        public static readonly Dictionary<string, EnemyDef> Enemies = new Dictionary<string, EnemyDef>();
        public static EnemyDef Boss;

        static GameDB()
        {
            Classes = new ClassDef[10];
            Color fire = new Color(1f, 0.45f, 0.1f), ice = new Color(0.45f, 0.85f, 1f), holy = new Color(1f, 0.92f, 0.55f),
                  dark = new Color(0.6f, 0.2f, 0.95f), nature = new Color(0.35f, 0.95f, 0.4f), blood = new Color(0.95f, 0.15f, 0.15f),
                  steel = new Color(0.75f, 0.85f, 1f), gold = new Color(1f, 0.8f, 0.25f), chi = new Color(1f, 0.65f, 0.2f);

            Classes[0] = new ClassDef
            {
                id = 0, name = "Knight", role = Role.Tank, roleText = "Đỡ đòn / Đấu sĩ",
                desc = "Tập trung vào phòng thủ, chống chịu và bảo vệ đồng đội.",
                hp = 1100, mana = 200, atk = 80, def = 60, atkSpeed = 1.0f, crit = 0.10f, moveSpeed = 5.2f,
                weapon = WeaponKind.SwordShield, model = ModelKind.Paladin, color = new Color(0.3f, 0.65f, 1f), ranged = false, attackRange = 2.6f,
                builds = new[] { "Iron Vanguard", "Radiant Paladin", "Dark Knight" },
                buildDesc = new[]
                {
                    "+30% HP, +22% Phòng thủ — Pháo đài thép tuyến đầu.",
                    "+30% Mana, +28% Hiệu quả hồi/khiên; Judgement hồi máu & ban khiên cho cả đội.",
                    "+28% Tấn công, +10% Hút máu, +6% Crit; hiến 4% HP khi tung Q/R gây nổ Huyết Ám + Nguyền."
                },
                passive = "Kiên cường: giảm 10% sát thương nhận vào.",
                skills = new[]
                {
                    new SkillDef { name = "Shield Bash", desc = "Đập khiên làm choáng kẻ địch phía trước.", kind = SkillKind.MeleeArc, cooldown = 5, mana = 15, power = 1.6f, radius = 3.2f, arc = 110, status = StatusType.Stun, statusDur = 1.2f, useCastAnim = false, color = steel },
                    new SkillDef { name = "Iron Guard", desc = "Tạo khiên bằng 30% HP và tăng phòng thủ.", kind = SkillKind.ShieldSelf, cooldown = 12, mana = 20, power = 0.30f, selfStatus = StatusType.DefUp, selfStatusDur = 6, color = steel },
                    new SkillDef { name = "Taunt", desc = "Khiêu khích quái xung quanh tấn công mình.", kind = SkillKind.Taunt, cooldown = 10, mana = 15, radius = 10, statusDur = 4, selfStatus = StatusType.DefUp, selfStatusDur = 4, color = gold },
                    new SkillDef { name = "Judgement", desc = "Bổ xuống mặt đất gây sát thương lớn và choáng.", kind = SkillKind.SelfAoE, cooldown = 24, mana = 40, power = 3.2f, radius = 5.5f, status = StatusType.Stun, statusDur = 1.5f, ultimate = true, color = holy }
                }
            };
            Classes[1] = new ClassDef
            {
                id = 1, name = "Guardian", role = Role.Tank, roleText = "Đỡ đòn / Hỗ trợ",
                desc = "Tạo khiên, giảm sát thương và bảo vệ đồng đội.",
                hp = 1250, mana = 220, atk = 60, def = 75, atkSpeed = 0.9f, crit = 0.05f, moveSpeed = 5.0f,
                weapon = WeaponKind.TowerShield, model = ModelKind.Paladin, color = new Color(0.9f, 0.73f, 0.32f), ranged = false, attackRange = 2.6f,
                builds = new[] { "Immortal Bastion", "Thorns Templar", "Valkyrie Aegis" },
                buildDesc = new[]
                {
                    "+35% HP, +25% DEF, +15% Khiên; Sanctuary tự cộng thêm khiên cá nhân 14% HP.",
                    "+18% DEF & ATK, +15% HP; phản đòn 35% sát thương cận chiến kẻ địch gây ra.",
                    "+16% Tốc chạy, +18% Giảm hồi chiêu, +15% ATK; Shield Wave phóng thêm giáo ánh sáng xuyên thấu."
                },
                passive = "Hộ vệ: đồng minh gần bạn nhận ít hơn 8% sát thương.",
                skills = new[]
                {
                    new SkillDef { name = "Shield Wave", desc = "Sóng khiên làm chậm kẻ địch.", kind = SkillKind.MeleeArc, cooldown = 5, mana = 12, power = 1.3f, radius = 4.5f, arc = 150, status = StatusType.Slow, statusDur = 2.5f, statusPower = 0.4f, useCastAnim = false, color = gold },
                    new SkillDef { name = "Sanctuary", desc = "Tạo khiên 18% HP cho cả đội.", kind = SkillKind.ShieldAll, cooldown = 14, mana = 30, power = 0.18f, radius = 14, color = gold },
                    new SkillDef { name = "Aegis", desc = "Tăng mạnh phòng thủ cho cả đội.", kind = SkillKind.BuffAll, cooldown = 16, mana = 25, radius = 14, status = StatusType.DefUp, statusDur = 7, color = steel },
                    new SkillDef { name = "Citadel", desc = "Khiêu khích + khiên 50% HP.", kind = SkillKind.Taunt, cooldown = 26, mana = 40, radius = 12, statusDur = 6, power = 0.5f, ultimate = true, selfStatus = StatusType.DefUp, selfStatusDur = 6, color = holy }
                }
            };
            Classes[2] = new ClassDef
            {
                id = 2, name = "Berserker", role = Role.MeleeDPS, roleText = "DPS cận chiến / Đấu sĩ",
                desc = "Càng mất máu càng mạnh, thiên về lối chơi mạo hiểm.",
                hp = 900, mana = 120, atk = 115, def = 30, atkSpeed = 1.2f, crit = 0.2f, moveSpeed = 5.6f,
                weapon = WeaponKind.GreatSword, model = ModelKind.Vanguard, color = new Color(0.85f, 0.2f, 0.15f), ranged = false, attackRange = 2.9f,
                builds = new[] { "Blood Frenzy", "Titan Juggernaut", "Crimson Whirlwind" },
                buildDesc = new[]
                {
                    "+22% Tốc đánh, +12% Hút máu; dưới 50% HP tăng thêm tới +35% Tốc đánh điên cuồng.",
                    "+25% HP, +20% DEF, +12% ATK; Cleave dậm chấn động làm chậm quái xung quanh.",
                    "+24% ATK, +12% Crit, +20% Sát thương chí mạng; Ragnarok ban thêm tăng tốc cuồng phong."
                },
                passive = "Cuồng nộ: sát thương tăng tới +60% khi HP thấp.",
                skills = new[]
                {
                    new SkillDef { name = "Cleave", desc = "Chém vòng cung rộng.", kind = SkillKind.MeleeArc, cooldown = 4, mana = 10, power = 1.8f, radius = 3.6f, arc = 200, useCastAnim = false, color = blood },
                    new SkillDef { name = "Bloodlust", desc = "Cuồng huyết: +40% tấn công, hút máu 15%.", kind = SkillKind.BuffSelf, cooldown = 14, mana = 20, status = StatusType.Rage, statusDur = 7, color = blood },
                    new SkillDef { name = "War Cry", desc = "Tăng tấn công cả đội.", kind = SkillKind.BuffAll, cooldown = 15, mana = 20, radius = 14, status = StatusType.AtkUp, statusDur = 7, color = chi },
                    new SkillDef { name = "Ragnarok", desc = "Xoáy kiếm hủy diệt xung quanh.", kind = SkillKind.SelfAoE, cooldown = 24, mana = 40, power = 4.2f, radius = 6, status = StatusType.Stun, statusDur = 1f, ultimate = true, useCastAnim = false, color = blood }
                }
            };
            Classes[3] = new ClassDef
            {
                id = 3, name = "Assassin", role = Role.MeleeDPS, roleText = "DPS bùng nổ / Cơ động",
                desc = "Áp sát, gây sát thương chí mạng và kết liễu mục tiêu.",
                hp = 720, mana = 160, atk = 105, def = 22, atkSpeed = 1.6f, crit = 0.35f, moveSpeed = 6.6f,
                weapon = WeaponKind.Daggers, model = ModelKind.Kachujin, color = new Color(0.7f, 0.3f, 0.85f), ranged = false, attackRange = 2.4f,
                builds = new[] { "Nightblade", "Phantom Dancer", "Venom Alchemist" },
                buildDesc = new[]
                {
                    "+38% Sát thương chí mạng, +10% Crit, +12% ATK — Nhất kích tất sát từ sau lưng.",
                    "+20% Giảm hồi chiêu, +14% Tốc chạy; Shadow Step hoàn 35% hồi chiêu & tạo khiên ảo ảnh.",
                    "+18% ATK, +6% Hút máu; Venom phóng 3 phi tiêu độc và kẻ địch trúng độc chết sẽ phát nổ mây độc."
                },
                passive = "Kết liễu: +50% sát thương lên mục tiêu dưới 30% HP.",
                skills = new[]
                {
                    new SkillDef { name = "Backstab", desc = "Đâm hiểm hóc, sát thương rất cao.", kind = SkillKind.MeleeArc, cooldown = 5, mana = 15, power = 2.6f, radius = 2.8f, arc = 70, useCastAnim = false, color = dark },
                    new SkillDef { name = "Shadow Step", desc = "Lướt xuyên kẻ địch, bất tử ngắn.", kind = SkillKind.Dash, cooldown = 7, mana = 15, power = 1.3f, range = 9, radius = 2f, useCastAnim = false, color = dark },
                    new SkillDef { name = "Venom", desc = "Tẩm độc gây sát thương theo thời gian.", kind = SkillKind.MeleeArc, cooldown = 8, mana = 15, power = 0.9f, radius = 3f, arc = 120, status = StatusType.Poison, statusDur = 6, statusPower = 0.35f, useCastAnim = false, color = nature },
                    new SkillDef { name = "Death Blossom", desc = "Vũ điệu tử thần quanh bản thân.", kind = SkillKind.SelfAoE, cooldown = 22, mana = 35, power = 3.8f, radius = 4.5f, ultimate = true, useCastAnim = false, color = dark }
                }
            };
            Classes[4] = new ClassDef
            {
                id = 4, name = "Ranger", role = Role.RangedDPS, roleText = "DPS tầm xa / Tiện ích",
                desc = "Tấn công từ xa, đặt bẫy và gây hiệu ứng bất lợi.",
                hp = 760, mana = 200, atk = 95, def = 25, atkSpeed = 1.4f, crit = 0.25f, moveSpeed = 5.8f,
                weapon = WeaponKind.Bow, model = ModelKind.Kachujin, color = new Color(0.27f, 0.7f, 0.36f), ranged = true, attackRange = 16f,
                builds = new[] { "Deadeye Sniper", "Frost Warden", "Tempest Volley" },
                buildDesc = new[]
                {
                    "+15% Crit, +28% Crit DMG; Mắt Ưng x2 (+20% Crit xa) & Piercing Shot bắn kèm tên xung kích.",
                    "+16% Giảm hồi chiêu, +20% Mana, +12% HP — Kiểm soát chiến trường bằng Bẫy Băng & làm chậm.",
                    "+25% Tốc đánh, +12% Tốc chạy; Volley bắn thêm 2 mũi tên gió xuyên thấu & tăng tốc."
                },
                passive = "Mắt ưng: +10% chí mạng với mục tiêu ở xa (>= 8m).",
                skills = new[]
                {
                    new SkillDef { name = "Piercing Shot", desc = "Mũi tên xuyên thấu.", kind = SkillKind.Projectile, cooldown = 4, mana = 12, power = 2.0f, range = 22, pierce = true, useCastAnim = false, color = nature },
                    new SkillDef { name = "Frost Trap", desc = "Đặt bẫy băng đóng băng kẻ địch.", kind = SkillKind.Trap, cooldown = 10, mana = 20, power = 1.0f, radius = 3.2f, range = 10, status = StatusType.Freeze, statusDur = 2.2f, color = ice },
                    new SkillDef { name = "Volley", desc = "Bắn 5 mũi tên hình quạt.", kind = SkillKind.Projectile, cooldown = 8, mana = 20, power = 0.95f, range = 18, count = 5, useCastAnim = false, color = nature },
                    new SkillDef { name = "Arrow Storm", desc = "Mưa tên làm chậm cả khu vực.", kind = SkillKind.GroundAoE, cooldown = 24, mana = 40, power = 3.2f, radius = 5.5f, range = 16, status = StatusType.Slow, statusDur = 3, statusPower = 0.5f, ultimate = true, color = nature }
                }
            };
            Classes[5] = new ClassDef
            {
                id = 5, name = "Mage", role = Role.RangedDPS, roleText = "DPS diện rộng / Khống chế",
                desc = "Sử dụng phép thuật mạnh để gây sát thương diện rộng.",
                hp = 650, mana = 500, atk = 110, def = 15, atkSpeed = 1.0f, crit = 0.15f, moveSpeed = 5.2f,
                weapon = WeaponKind.Staff, model = ModelKind.Kachujin, color = new Color(0.2f, 0.8f, 1f), ranged = true, attackRange = 16f,
                builds = new[] { "Pyromancer", "Cryomancer", "Arcanist" },
                buildDesc = new[]
                {
                    "Burn mạnh hơn 50%, +30% Sát thương chí mạng, +10% Crit, +12% ATK.",
                    "Đóng băng/làm chậm lâu hơn 50%, +15% HP; Frost Nova tạo Khiên Băng 22% HP.",
                    "+32% Mana, +22% Giảm hồi chiêu; Arcane Pulse phóng thêm 2 tia bí thuật hội tụ."
                },
                passive = "Tinh thông nguyên tố: kỹ năng tốn ít mana hơn 10%.",
                skills = new[]
                {
                    new SkillDef { name = "Fireball", desc = "Cầu lửa nổ gây bỏng.", kind = SkillKind.Projectile, cooldown = 3.5f, mana = 20, power = 2.0f, radius = 2.8f, range = 20, status = StatusType.Burn, statusDur = 4, statusPower = 0.3f, color = fire },
                    new SkillDef { name = "Frost Nova", desc = "Đóng băng kẻ địch xung quanh.", kind = SkillKind.SelfAoE, cooldown = 10, mana = 30, power = 1.1f, radius = 5.5f, status = StatusType.Freeze, statusDur = 2f, color = ice },
                    new SkillDef { name = "Arcane Pulse", desc = "Xung năng lượng bí thuật tốc độ cao.", kind = SkillKind.Projectile, cooldown = 2.5f, mana = 15, power = 1.6f, range = 22, pierce = true, color = new Color(0.75f, 0.45f, 1f) },
                    new SkillDef { name = "Meteor", desc = "Triệu hồi thiên thạch hủy diệt.", kind = SkillKind.GroundAoE, cooldown = 24, mana = 60, power = 4.5f, radius = 5.5f, range = 18, status = StatusType.Burn, statusDur = 4, statusPower = 0.4f, ultimate = true, color = fire }
                }
            };
            Classes[6] = new ClassDef
            {
                id = 6, name = "Warlock", role = Role.RangedDPS, roleText = "DOT / Debuff / Triệu hồi",
                desc = "Gây sát thương theo thời gian, nguyền rủa và triệu hồi.",
                hp = 700, mana = 450, atk = 95, def = 20, atkSpeed = 1.0f, crit = 0.1f, moveSpeed = 5.2f,
                weapon = WeaponKind.Wand, model = ModelKind.Vanguard, color = new Color(0.6f, 0.18f, 0.95f), ranged = true, attackRange = 15f,
                builds = new[] { "Affliction Reaper", "Demonologist", "Soul Harvester" },
                buildDesc = new[]
                {
                    "+18% ATK, +12% Giảm hồi chiêu, +6% Hút máu — Tối ưu sát thương Nguyền Rủa & Thiêu Đốt.",
                    "+25% Mana, +16% HP, +15% CDR; Soul Minion triệu hồi CÙNG LÚC 2 Tiểu Quỷ Hực Hồn!",
                    "+22% ATK, +10% Hút máu, +8% Crit; Soul Bolt hồi lại 12 Mana mỗi lần tung chiêu."
                },
                passive = "Hút hồn: đòn đánh thường hồi 4% sát thương thành máu.",
                skills = new[]
                {
                    new SkillDef { name = "Soul Bolt", desc = "Tia linh hồn hút máu.", kind = SkillKind.Projectile, cooldown = 3, mana = 15, power = 1.6f, range = 20, color = dark },
                    new SkillDef { name = "Curse", desc = "Nguyền rủa: tăng 25% sát thương nhận vào + độc.", kind = SkillKind.GroundAoE, cooldown = 9, mana = 25, power = 0.6f, radius = 4.5f, range = 15, status = StatusType.Curse, statusDur = 7, statusPower = 0.25f, color = dark },
                    new SkillDef { name = "Soul Minion", desc = "Triệu hồi tiểu quỷ chiến đấu 20 giây.", kind = SkillKind.Summon, cooldown = 18, mana = 35, power = 0.5f, statusDur = 20, color = dark },
                    new SkillDef { name = "Cataclysm", desc = "Đại hồng thủy hắc ám thiêu đốt.", kind = SkillKind.GroundAoE, cooldown = 26, mana = 60, power = 3.8f, radius = 6.5f, range = 16, status = StatusType.Burn, statusDur = 5, statusPower = 0.35f, ultimate = true, color = dark }
                }
            };
            Classes[7] = new ClassDef
            {
                id = 7, name = "Monk", role = Role.Hybrid, roleText = "DPS cận chiến / Hỗ trợ",
                desc = "Đánh nhanh, cơ động và có khả năng hỗ trợ đồng đội.",
                hp = 850, mana = 250, atk = 90, def = 35, atkSpeed = 1.8f, crit = 0.2f, moveSpeed = 6.4f,
                weapon = WeaponKind.Fists, model = ModelKind.Kachujin, color = new Color(1f, 0.65f, 0.2f), ranged = false, attackRange = 2.4f,
                builds = new[] { "Windwalker", "Mistweaver", "Dragon Fist" },
                buildDesc = new[]
                {
                    "+24% Tốc đánh, +14% Tốc chạy, +8% Crit; Flurry tung liên hoàn 5 đòn chớp nhoáng.",
                    "+35% Hiệu quả hồi máu, +25% Mana, +14% CDR; Zen Mend ban thêm khiên khí công cho cả đội.",
                    "+25% ATK, +25% Crit DMG; Palm Strike phóng thêm kình lực Hỏa Long tầm xa gây Bỏng."
                },
                passive = "Nội công: mỗi đòn đánh hồi 4 mana.",
                skills = new[]
                {
                    new SkillDef { name = "Flurry", desc = "Liên hoàn quyền 3 đòn.", kind = SkillKind.MeleeArc, cooldown = 4, mana = 10, power = 0.75f, radius = 3f, arc = 120, count = 3, useCastAnim = false, color = chi },
                    new SkillDef { name = "Zen Mend", desc = "Hồi máu và hồi phục cho cả đội.", kind = SkillKind.HealAll, cooldown = 12, mana = 30, power = 1.4f, radius = 14, status = StatusType.Regen, statusDur = 5, color = nature },
                    new SkillDef { name = "Palm Strike", desc = "Chưởng làm choáng và đẩy lùi.", kind = SkillKind.MeleeArc, cooldown = 8, mana = 15, power = 2.1f, radius = 3.2f, arc = 90, status = StatusType.Stun, statusDur = 1.5f, useCastAnim = false, color = chi },
                    new SkillDef { name = "Dragon Fist", desc = "Lao tới và tung quyền rồng.", kind = SkillKind.Dash, cooldown = 22, mana = 35, power = 3.6f, range = 8, radius = 4.5f, ultimate = true, useCastAnim = false, color = fire }
                }
            };
            Classes[8] = new ClassDef
            {
                id = 8, name = "Druid", role = Role.Healer, roleText = "Hỗ trợ / Hồi máu / Lai",
                desc = "Có thể chuyển dạng để thay đổi vai trò trong chiến đấu.",
                hp = 850, mana = 400, atk = 75, def = 40, atkSpeed = 1.0f, crit = 0.1f, moveSpeed = 5.5f,
                weapon = WeaponKind.Staff, model = ModelKind.Vanguard, color = new Color(0.2f, 0.85f, 0.6f), ranged = true, attackRange = 14f,
                builds = new[] { "Grove Healer", "Ursine Guardian", "Stormcaller" },
                buildDesc = new[]
                {
                    "+35% Hiệu quả hồi máu, +25% Mana, +12% CDR — Đại tư tế rừng già cứu trợ cả tổ đội.",
                    "+28% HP, +25% DEF; Hóa Gấu kéo dài 18s (+45% Khiên HP) & dậm đất khiêu khích quái.",
                    "+25% ATK, +15% CDR, +8% Crit; Thorns & Nature's Wrath giáng thêm sét gây Choáng."
                },
                passive = "Thiên nhiên: hồi 1% HP mỗi giây khi ngoài giao tranh.",
                skills = new[]
                {
                    new SkillDef { name = "Thorns", desc = "Gai độc làm chậm và gây độc.", kind = SkillKind.GroundAoE, cooldown = 6, mana = 15, power = 1.2f, radius = 3.5f, range = 14, status = StatusType.Poison, statusDur = 5, statusPower = 0.25f, color = nature },
                    new SkillDef { name = "Regrowth", desc = "Hồi máu mạnh cho đồng minh yếu nhất.", kind = SkillKind.HealTarget, cooldown = 6, mana = 25, power = 2.8f, range = 20, status = StatusType.Regen, statusDur = 6, color = nature },
                    new SkillDef { name = "Bear Form", desc = "Hóa gấu 12 giây: cận chiến, +60% phòng thủ.", kind = SkillKind.Transform, cooldown = 18, mana = 20, statusDur = 12, color = new Color(0.6f, 0.4f, 0.2f) },
                    new SkillDef { name = "Nature's Wrath", desc = "Cơn thịnh nộ thiên nhiên: sát thương + hồi máu cả đội.", kind = SkillKind.SelfAoE, cooldown = 26, mana = 60, power = 2.6f, radius = 7.5f, status = StatusType.Slow, statusDur = 3, statusPower = 0.4f, ultimate = true, color = nature }
                }
            };
            Classes[9] = new ClassDef
            {
                id = 9, name = "Cleric", role = Role.Healer, roleText = "Hồi máu / Hỗ trợ",
                desc = "Hồi máu, tạo khiên, giải hiệu ứng và hồi sinh đồng đội.",
                hp = 900, mana = 500, atk = 60, def = 45, atkSpeed = 0.9f, crit = 0.05f, moveSpeed = 5.0f,
                weapon = WeaponKind.Mace, model = ModelKind.Paladin, color = new Color(1f, 0.89f, 0.63f), ranged = true, attackRange = 14f,
                builds = new[] { "Divine Oracle", "Holy Inquisitor", "Lightbringer" },
                buildDesc = new[]
                {
                    "+35% Hiệu quả hồi máu, +25% Mana; Group Heal ban thêm Phước Lành +45% DEF cho đồng minh.",
                    "+38% ATK, +15% Crit, +12% Tốc đánh; Smite phóng thêm 2 tia phán quyết gây Bỏng Thánh.",
                    "+20% Giảm hồi chiêu, +20% Hồi/Khiên, +16% HP — Duy trì khiên thanh tẩy & Hồi Sinh liên tục."
                },
                passive = "Phước lành: hồi máu tăng 15%.",
                skills = new[]
                {
                    new SkillDef { name = "Smite", desc = "Tia thánh quang trừng phạt.", kind = SkillKind.Projectile, cooldown = 3, mana = 12, power = 1.7f, range = 20, color = holy },
                    new SkillDef { name = "Group Heal", desc = "Hồi máu cả đội.", kind = SkillKind.HealAll, cooldown = 9, mana = 35, power = 2.2f, radius = 16, color = holy },
                    new SkillDef { name = "Purify", desc = "Giải hiệu ứng xấu + khiên 12% HP.", kind = SkillKind.Cleanse, cooldown = 12, mana = 25, power = 0.12f, radius = 16, color = holy },
                    new SkillDef { name = "Resurrection", desc = "Hồi sinh đồng đội gục ngã và hồi máu cả đội.", kind = SkillKind.Revive, cooldown = 40, mana = 80, power = 2.0f, radius = 40, ultimate = true, color = holy }
                }
            };

            // ---------------- Monsters ----------------
            AddEnemy(new EnemyDef { id = "Goblin", name = "Goblin", archetype = Archetype.Melee, hp = 300, atk = 26, def = 10, speed = 4.4f, detect = 14, attackRange = 2.0f, attackCd = 1.4f, exp = 20, gold = 8, model = ModelKind.Kachujin, tint = new Color(0.45f, 0.85f, 0.35f), scale = 0.72f, weapon = WeaponKind.Daggers, accessory = "ears" });
            AddEnemy(new EnemyDef { id = "GoblinArcher", name = "Goblin Archer", archetype = Archetype.Ranged, hp = 230, atk = 28, def = 5, speed = 4.0f, detect = 18, attackRange = 13f, attackCd = 2.0f, exp = 25, gold = 10, model = ModelKind.Kachujin, tint = new Color(0.6f, 0.75f, 0.3f), scale = 0.72f, weapon = WeaponKind.Bow, accessory = "ears", projColor = new Color(0.9f, 0.8f, 0.4f) });
            AddEnemy(new EnemyDef { id = "Skeleton", name = "Skeleton", archetype = Archetype.Melee, hp = 360, atk = 30, def = 15, speed = 3.6f, detect = 14, attackRange = 2.2f, attackCd = 1.6f, exp = 25, gold = 12, model = ModelKind.Vanguard, tint = new Color(0.92f, 0.9f, 0.8f), scale = 0.95f, weapon = WeaponKind.SwordShield, accessory = "skull" });
            AddEnemy(new EnemyDef { id = "SkeletonWarrior", name = "Skeleton Warrior", archetype = Archetype.Tank, hp = 650, atk = 34, def = 30, speed = 3.2f, detect = 14, attackRange = 2.4f, attackCd = 1.9f, exp = 40, gold = 20, model = ModelKind.Paladin, tint = new Color(0.85f, 0.83f, 0.72f), scale = 1.0f, weapon = WeaponKind.None, accessory = "skull" });
            AddEnemy(new EnemyDef { id = "SkeletonMage", name = "Skeleton Mage", archetype = Archetype.Mage, hp = 290, atk = 36, def = 6, speed = 3.4f, detect = 18, attackRange = 12f, attackCd = 2.6f, exp = 35, gold = 18, model = ModelKind.Kachujin, tint = new Color(0.75f, 0.7f, 0.95f), scale = 0.95f, weapon = WeaponKind.Staff, accessory = "skull", projColor = new Color(0.6f, 0.3f, 1f) });
            AddEnemy(new EnemyDef { id = "Zombie", name = "Zombie", archetype = Archetype.Melee, hp = 520, atk = 32, def = 10, speed = 2.4f, detect = 12, attackRange = 2.0f, attackCd = 2.0f, exp = 22, gold = 8, model = ModelKind.Vanguard, tint = new Color(0.5f, 0.65f, 0.45f), scale = 1.0f, weapon = WeaponKind.Claws, animSpeed = 0.7f });
            AddEnemy(new EnemyDef { id = "DarkKnight", name = "Dark Knight", archetype = Archetype.Elite, hp = 1500, atk = 52, def = 35, speed = 3.8f, detect = 18, attackRange = 2.8f, attackCd = 1.8f, exp = 90, gold = 50, model = ModelKind.Paladin, tint = new Color(0.35f, 0.2f, 0.45f), scale = 1.18f, weapon = WeaponKind.None, accessory = "eyes" });
            AddEnemy(new EnemyDef { id = "Demon", name = "Demon", archetype = Archetype.Assassin, hp = 700, atk = 46, def = 15, speed = 5.2f, detect = 20, attackRange = 2.3f, attackCd = 1.5f, exp = 60, gold = 35, model = ModelKind.Vanguard, tint = new Color(0.95f, 0.35f, 0.3f), scale = 1.05f, weapon = WeaponKind.Claws, accessory = "horns" });
            AddEnemy(new EnemyDef { id = "CrystalGolem", name = "Crystal Golem", archetype = Archetype.Tank, hp = 1000, atk = 44, def = 40, speed = 2.6f, detect = 14, attackRange = 3.2f, attackCd = 2.4f, exp = 80, gold = 45, model = ModelKind.Golem, tint = new Color(0.55f, 0.75f, 1f), scale = 1.0f });
            AddEnemy(new EnemyDef { id = "ForestMonster", name = "Forest Monster", archetype = Archetype.Melee, hp = 600, atk = 36, def = 18, speed = 3.4f, detect = 14, attackRange = 2.8f, attackCd = 2.0f, exp = 35, gold = 16, model = ModelKind.Treant, tint = new Color(0.35f, 0.6f, 0.25f), scale = 1.0f });

            Boss = new EnemyDef { id = "AbyssalKnight", name = "Abyssal Knight", archetype = Archetype.Boss, hp = 16000, atk = 70, def = 40, speed = 4.2f, detect = 60, attackRange = 3.6f, attackCd = 2.2f, exp = 600, gold = 400, model = ModelKind.Paladin, tint = new Color(0.45f, 0.12f, 0.2f), scale = 1.85f, weapon = WeaponKind.None, accessory = "crown", projColor = new Color(0.7f, 0.1f, 1f) };
        }

        static void AddEnemy(EnemyDef d) { Enemies[d.id] = d; }

        public static ClassDef GetClass(int id) { return Classes[Mathf.Clamp(id, 0, Classes.Length - 1)]; }

        public static string StatusName(StatusType s)
        {
            switch (s)
            {
                case StatusType.Burn: return "Bỏng";
                case StatusType.Poison: return "Độc";
                case StatusType.Slow: return "Chậm";
                case StatusType.Freeze: return "Đóng băng";
                case StatusType.Stun: return "Choáng";
                case StatusType.Curse: return "Nguyền";
                case StatusType.AtkUp: return "+Công";
                case StatusType.DefUp: return "+Thủ";
                case StatusType.SpeedUp: return "+Tốc";
                case StatusType.Regen: return "Hồi phục";
                case StatusType.Rage: return "Cuồng huyết";
                case StatusType.Taunted: return "Khiêu khích";
            }
            return "";
        }

        public static bool IsDebuff(StatusType s)
        {
            return s == StatusType.Burn || s == StatusType.Poison || s == StatusType.Slow || s == StatusType.Freeze ||
                   s == StatusType.Stun || s == StatusType.Curse || s == StatusType.Taunted;
        }

        public static string RoomName(RoomType t)
        {
            switch (t)
            {
                case RoomType.Entrance: return "Cổng Vực Thẳm";
                case RoomType.Combat: return "Phòng Chiến Đấu";
                case RoomType.Elite: return "Hầm Mộ Tinh Anh";
                case RoomType.Puzzle: return "Phòng Ấn Phong (Giải đố)";
                case RoomType.MiniBoss: return "Kẻ Giữ Cổng (Mini Boss)";
                case RoomType.Boss: return "Điện Thờ Vực Thẳm (Boss)";
            }
            return "";
        }

        public static string BuildName(int cls, int build)
        {
            var c = GetClass(cls);
            return c.builds[Mathf.Clamp(build, 0, 2)];
        }

        public static float ExpToNext(int level) { return Mathf.Round(120f * Mathf.Pow(level, 1.45f)); }
    }
}

