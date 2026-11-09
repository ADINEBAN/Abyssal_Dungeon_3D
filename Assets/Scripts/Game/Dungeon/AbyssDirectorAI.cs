using System;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Section 3.6 AI Dungeon Director ("Abyss Director").
    /// Tracks:
    /// 1. Player deaths          5. Healing done
    /// 2. Damage dealt           6. Party composition (Tank / Healer / DPS / CC)
    /// 3. Damage taken           7. Monster kill speed
    /// 4. Dungeon elapsed time
    /// Dynamically adjusts next encounter monster stats, density, Elite upgrades, or spawns Healing Shrines.
    /// </summary>
    public class AbyssDirectorAI : MonoBehaviour
    {
        public static AbyssDirectorAI Instance { get; private set; }

        // Cumulative run metrics
        public int TotalDeaths;
        public float TotalDamageDealt;
        public float TotalDamageTaken;
        public float TotalHealing;
        public int TotalKills;
        public float RunElapsed;

        // Per-room window metrics
        private int _roomDeaths;
        private float _roomDealt;
        private float _roomTaken;
        private float _roomHealing;
        private int _roomKills;
        private float _roomTime;

        // Composition analysis
        public bool HasTank;
        public bool HasHealer;
        public int PartyCount;
        public string CompositionTag = "Cân bằng";

        // Director output
        public float PerformanceScore = 50f; // 0..100
        public float ThreatBudget = 100f;    // 60..160 clamped budget
        public float HpMultiplier = 1.0f;
        public float AtkMultiplier = 1.0f;
        public int ExtraMonsterDelta = 0;    // -1, 0, +1, +2
        public bool PromoteBonusElite = false;
        public bool SpawnBonusHealingShrine = false;
        public string LastDecision = "Abyss Director đang phân tích tổ đội...";

        public event Action<string> OnDirectorAdjusted;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AnalyzePartyComposition()
        {
            HasTank = false;
            HasHealer = false;
            int dpsCount = 0;
            PartyCount = 0;

            foreach (var slot in GameSession.I.Party)
            {
                if (slot == null || !slot.active) continue;
                PartyCount++;
                ClassDef c = GameDB.GetClass(slot.classId);
                if (c.role == Role.Tank) HasTank = true;
                else if (c.role == Role.Healer || c.id == 7) HasHealer = true;
                else dpsCount++;
            }

            if (HasTank && HasHealer && dpsCount >= 1)
                CompositionTag = "Đội hình Cân bằng";
            else if (!HasTank && !HasHealer)
                CompositionTag = "Đội hình Thuần Công (Tặng thêm Đài hồi phục)";
            else if (HasTank && HasHealer && dpsCount == 0)
                CompositionTag = "Đội hình Thủ Vững";
            else
                CompositionTag = "Đội hình Linh hoạt";

            if (!HasHealer) SpawnBonusHealingShrine = true;
            LastDecision = $"Phân tích đội ({PartyCount} người): {CompositionTag} | Độ khó 100%";
        }

        private void Update()
        {
            RunElapsed += Time.deltaTime;
            _roomTime += Time.deltaTime;
        }

        public void RecordDamageDealt(float v) { TotalDamageDealt += v; _roomDealt += v; }
        public void RecordDamageTaken(float v) { TotalDamageTaken += v; _roomTaken += v; }
        public void RecordHealing(float v) { TotalHealing += v; _roomHealing += v; }
        public void RecordKill() { TotalKills++; _roomKills++; }
        public void RecordPlayerDeath() { TotalDeaths++; _roomDeaths++; }

        public float KillsPerMinute => RunElapsed > 3f ? (TotalKills / (RunElapsed / 60f)) : 0f;

        /// <summary>Called when the party clears a room and advances to the next encounter.</summary>
        public void EvaluateAndAdjustNextEncounter(int completedRoomIndex)
        {
            float score = 52f;

            // Damage efficiency ratio
            float ratio = _roomDealt / Mathf.Max(80f, _roomTaken);
            score += Mathf.Clamp((ratio - 2.2f) * 6.5f, -18f, 24f);

            // Kill speed bonus
            float roomKpm = _roomTime > 2f ? (_roomKills / (_roomTime / 60f)) : 8f;
            score += Mathf.Clamp((roomKpm - 8f) * 1.6f, -10f, 16f);

            // Healing sustain bonus
            if (_roomHealing > _roomTaken * 0.5f && _roomTaken > 50f) score += 6f;

            // Heavy penalty for party knockdowns/deaths
            score -= _roomDeaths * 18f;

            // 5. Completion time curve (target ~28s per combat room)
            float timeDelta = Mathf.Clamp((28f - _roomTime) * 0.35f, -8f, 10f);
            score += timeDelta;

            // 6. Party composition synergy factor
            if (HasTank && HasHealer) score += 3f;

            // Smooth blend with previous score (max per-room shift clamped so difficulty never feels unfair)
            float targetScore = Mathf.Clamp(score, 15f, 95f);
            float maxDelta = 18f;
            PerformanceScore = Mathf.Clamp(
                PerformanceScore + Mathf.Clamp(targetScore - PerformanceScore, -maxDelta, maxDelta),
                15f, 95f);
            ThreatBudget = Mathf.Round(Mathf.Lerp(65f, 155f, (PerformanceScore - 15f) / 80f));

            if (PerformanceScore >= 68f)
            {
                HpMultiplier = Mathf.Lerp(1.15f, 1.45f, (PerformanceScore - 68f) / 27f);
                AtkMultiplier = Mathf.Lerp(1.10f, 1.30f, (PerformanceScore - 68f) / 27f);
                ExtraMonsterDelta = PerformanceScore >= 82f ? 2 : 1;
                PromoteBonusElite = true;
                SpawnBonusHealingShrine = false;
                LastDecision = $"[Abyss Director {PerformanceScore:0}đ] Tổ đội áp đảo → Tăng {Mathf.RoundToInt((HpMultiplier - 1f) * 100)}% sức mạnh quái & thêm Quái Tinh Anh!";
            }
            else if (PerformanceScore <= 38f)
            {
                HpMultiplier = 0.78f;
                AtkMultiplier = 0.80f;
                ExtraMonsterDelta = -1;
                PromoteBonusElite = false;
                SpawnBonusHealingShrine = true;
                LastDecision = $"[Abyss Director {PerformanceScore:0}đ] Tổ đội gặp khó → Giảm 20% sức mạnh quái & triệu hồi Đài Hồi Phục hỗ trợ!";
            }
            else
            {
                HpMultiplier = 1.0f;
                AtkMultiplier = 1.0f;
                ExtraMonsterDelta = 0;
                PromoteBonusElite = false;
                SpawnBonusHealingShrine = !HasHealer;
                LastDecision = $"[Abyss Director {PerformanceScore:0}đ] Nhịp độ cân bằng ({KillsPerMinute:0.0} quái/phút) → Giữ độ khó chuẩn.";
            }

            // Reset room metrics
            _roomDeaths = 0;
            _roomDealt = 0f;
            _roomTaken = 0f;
            _roomHealing = 0f;
            _roomKills = 0;
            _roomTime = 0f;

            OnDirectorAdjusted?.Invoke(LastDecision);
        }

        public string GetVerdictLabel()
        {
            if (PerformanceScore >= 68f) return $"Áp đảo ({PerformanceScore:0}/100 - Tăng Tinh Anh)";
            if (PerformanceScore <= 38f) return $"Hỗ trợ ({PerformanceScore:0}/100 - Giảm tải & Hồi phục)";
            return $"Cân bằng ({PerformanceScore:0}/100)";
        }

        public string GetDetailedTelemetrySummary()
        {
            return $"• Điểm Hiệu Suất: {PerformanceScore:0}/100 (Threat Budget: {ThreatBudget:0}%)\n" +
                   $"• Tử trận / Gục ngã: {TotalDeaths}  |  Tốc độ diệt: {KillsPerMinute:0.1} quái/phút\n" +
                   $"• Sát thương gây ra: {TotalDamageDealt:0}  |  Sát thương nhận: {TotalDamageTaken:0}\n" +
                   $"• Hồi phục: {TotalHealing:0}  |  Thời gian: {RunElapsed:0}s\n" +
                   $"• Cấu hình đội ({PartyCount} người): {CompositionTag}\n" +
                   $"• Hệ số Quái hiện tại: HP x{HpMultiplier:0.00} | ATK x{AtkMultiplier:0.00} | Mật độ {(ExtraMonsterDelta >= 0 ? "+" + ExtraMonsterDelta : ExtraMonsterDelta.ToString())}";
        }
    }
}

