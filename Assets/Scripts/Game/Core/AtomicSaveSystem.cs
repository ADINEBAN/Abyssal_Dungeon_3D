using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 8: Crash-safe Atomic Save System (.tmp -> .bak -> target) with Schema Versioning
    /// and RPG Equipment Comparison System (follows `save-systems` and `rpg` skills).
    /// </summary>
    [Serializable]
    public class SaveEnvelopeV3
    {
        public int schemaVersion = 3;
        public string savedAtIso = "";
        public PersistentProfile profile;
    }

    public static class AtomicSaveSystem
    {
        public const int CurrentSchemaVersion = 3;
        private const string FileName = "abyssal_save_v3.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => SavePath + ".tmp";
        private static string BackupPath => SavePath + ".bak";

        public static void SaveAtomic(PersistentProfile profile)
        {
            if (profile == null) return;
            try
            {
                var env = new SaveEnvelopeV3
                {
                    schemaVersion = CurrentSchemaVersion,
                    savedAtIso = DateTime.UtcNow.ToString("o"),
                    profile = profile
                };

                string json = JsonUtility.ToJson(env, true);
                File.WriteAllText(TempPath, json, Encoding.UTF8);

                if (File.Exists(SavePath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(SavePath, BackupPath);
                }
                File.Move(TempPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AtomicSaveSystem] Atomic write warning: " + e.Message);
            }
        }

        public static bool TryLoadAtomic(out PersistentProfile loadedProfile)
        {
            loadedProfile = null;
            if (TryReadEnvelope(SavePath, out loadedProfile)) return true;
            if (TryReadEnvelope(BackupPath, out loadedProfile)) return true;
            return false;
        }

        private static bool TryReadEnvelope(string path, out PersistentProfile profile)
        {
            profile = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return false;

                var env = JsonUtility.FromJson<SaveEnvelopeV3>(json);
                if (env != null && env.profile != null)
                {
                    MigrateIfNeeded(env);
                    profile = env.profile;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AtomicSaveSystem] Failed reading '{path}': {e.Message}");
            }
            return false;
        }

        private static void MigrateIfNeeded(SaveEnvelopeV3 env)
        {
            if (env.schemaVersion < 3 && env.profile != null)
            {
                if (env.profile.skillRanks == null || env.profile.skillRanks.Length != 4)
                    env.profile.skillRanks = new[] { 1, 1, 1, 1 };
                if (env.profile.cameraDistance < 3.4f || env.profile.cameraDistance > 15f)
                    env.profile.cameraDistance = 7.8f;
                env.schemaVersion = CurrentSchemaVersion;
            }
        }

        public static void DeleteAtomicSaves()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch { }
        }
    }

    /// <summary>
    /// Phase 8: Side-by-side RPG Equipment Comparison Calculator.
    /// Compares a candidate bag item against the currently equipped item in the same slot.
    /// </summary>
    public static class EquipmentComparisonSystem
    {
        public static string FormatComparisonDelta(EquipItem candidate, EquipItem equipped, int classId)
        {
            if (candidate == null) return "";

            bool compat = ClassSpecializationSystem.EvaluateCompatibility(classId, candidate, out float mult, out string note);
            if (!compat)
            {
                return $"<color=#FF5555>✖ {note}</color>";
            }

            var deltas = new List<string>();
            float eqAtk = equipped != null ? equipped.atk : 0f;
            float eqDef = equipped != null ? equipped.def : 0f;
            float eqHp = equipped != null ? equipped.hp : 0f;
            float eqMp = equipped != null ? equipped.mana : 0f;
            float eqCrit = equipped != null ? equipped.crit : 0f;
            float eqAspd = equipped != null ? equipped.atkSpeed : 0f;
            float eqCdr = equipped != null ? equipped.cdr : 0f;

            AppendDelta(deltas, "ATK", candidate.atk - eqAtk, false);
            AppendDelta(deltas, "DEF", candidate.def - eqDef, false);
            AppendDelta(deltas, "HP", candidate.hp - eqHp, false);
            AppendDelta(deltas, "MP", candidate.mana - eqMp, false);
            AppendDelta(deltas, "Crit", (candidate.crit - eqCrit) * 100f, true);
            AppendDelta(deltas, "ASPD", (candidate.atkSpeed - eqAspd) * 100f, true);
            AppendDelta(deltas, "CDR", (candidate.cdr - eqCdr) * 100f, true);

            float candScore = candidate.ScoreForClass(classId);
            float eqScore = equipped != null ? equipped.ScoreForClass(classId) : 0f;
            float diffScore = candScore - eqScore;
            string verdict = diffScore >= 0.5f
                ? $"<color=#55FF88>▲ +{diffScore:0} Lực Chiến</color>"
                : (diffScore <= -0.5f ? $"<color=#FF6666>▼ {diffScore:0} Lực Chiến</color>" : "<color=#CCCCCC>= Tương đương</color>");

            string affinityTag = mult > 1.01f ? " <color=#FFD955>[Hợp hệ +12%]</color>" : "";
            string joined = deltas.Count > 0 ? string.Join(" ", deltas) : "";
            return $"{verdict}{affinityTag}  {joined}";
        }

        private static void AppendDelta(List<string> list, string label, float diff, bool isPercent)
        {
            if (Mathf.Abs(diff) < 0.25f) return;
            string unit = isPercent ? "%" : "";
            if (diff > 0f)
                list.Add($"<color=#55FF88>▲+{diff:0.#}{unit} {label}</color>");
            else
                list.Add($"<color=#FF6666>▼{diff:0.#}{unit} {label}</color>");
        }
    }
}
