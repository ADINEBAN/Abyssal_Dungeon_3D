using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using AbyssGame;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// On-demand Runtime QA & Profiler Harness for Environment VFX Phase 1.
    /// Activated ONLY when flagged explicitly via PlayerPrefs ("Abyssal_RunVfxPhase1Benchmark" == 1).
    /// Measures real PerformanceMonitor CPU/GPU Frame Time, FPS, Draw Calls, Batches, SetPass, Active Particles,
    /// and GC Allocations across Low, Medium, and High presets with all 25 Environment VFX active.
    /// </summary>
    public class EnvironmentVfxPhase1Verifier : MonoBehaviour
    {
        public const string BenchmarkPrefKey = "Abyssal_RunVfxPhase1Benchmark";
        public const string ReportFileName = "EnvironmentVfxPhase1_RuntimeReport.txt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CheckScheduledBenchmark()
        {
            if (PlayerPrefs.GetInt(BenchmarkPrefKey, 0) != 1)
                return;

            PlayerPrefs.SetInt(BenchmarkPrefKey, 0);
            PlayerPrefs.Save();

            var go = new GameObject("[EnvironmentVfxPhase1Verifier]");
            DontDestroyOnLoad(go);
            go.AddComponent<EnvironmentVfxPhase1Verifier>();
        }

        private IEnumerator Start()
        {
            // Wait for DungeonWorldBuilder / scene bootstrap to finish constructing the 7 rooms
            yield return new WaitForSeconds(2.0f);

            var sb = new StringBuilder();
            sb.AppendLine("=== ABYSSAL DUNGEON 3D — ENVIRONMENT VFX PHASE 1 RUNTIME REPORT ===");
            sb.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} | VRAM: {SystemInfo.graphicsMemorySize} MB");

            var gm = GraphicsManager.Instance;
            var mgr = VFXManager.EnsureInstance();
            var cam = Camera.main;

            // 1. Verify all 25 Environment VFX spawn & pool cleanly in front of active camera
            Vector3 anchor = cam != null
                ? cam.transform.position + cam.transform.forward * 7f
                : Vector3.zero;
            anchor.y = 0.25f;

            var spawnedLoops = new System.Collections.Generic.List<(VFXType, GameObject)>();
            int colIdx = 0;
            foreach (VFXType t in EnvironmentVfxFactory.AllPhase1Types)
            {
                Vector3 pos = anchor + new Vector3((colIdx % 5 - 2) * 2.2f, 0.1f, (colIdx / 5 - 2) * 2.2f);
                if (t == VFXType.Env_TrapActivation ||
                    t == VFXType.Env_FireExplosion ||
                    t == VFXType.Env_GroundDustBurst ||
                    t == VFXType.Env_StoneDebris ||
                    t == VFXType.Env_EnvironmentalImpact ||
                    t == VFXType.Env_AncientAltarActivation)
                {
                    mgr.SpawnVFX(t, pos, Quaternion.identity, 2.5f);
                }
                else
                {
                    var lo = mgr.SpawnLoopingVFX(t, null, pos, Quaternion.identity);
                    if (lo != null) spawnedLoops.Add((t, lo));
                }
                colIdx++;
            }

            // 2. Profile across Low, Medium, High presets including camera motion + simultaneous bursts
            GraphicsQualityTier[] tiers = { GraphicsQualityTier.Low, GraphicsQualityTier.Medium, GraphicsQualityTier.High };
            for (int i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (gm != null)
                    gm.SetQualityTier(tier, isUserManualSelection: false, saveImmediately: false);

                // Re-trigger simultaneous burst interactions
                mgr.SpawnVFX(VFXType.Env_TrapActivation, anchor + new Vector3(-2f, 0.05f, 0f), 0.9f);
                mgr.SpawnVFX(VFXType.Env_FireExplosion, anchor + new Vector3(0f, 0.1f, 0f), 1.8f);
                mgr.SpawnVFX(VFXType.Env_StoneDebris, anchor + new Vector3(2f, 0.1f, 0f), 1.8f);
                mgr.SpawnVFX(VFXType.Env_EnvironmentalImpact, anchor + new Vector3(0f, 0.05f, 2f), 1.6f);
                mgr.SpawnVFX(VFXType.Env_AncientAltarActivation, anchor + new Vector3(0f, 0.05f, -2f), 2.0f);

                // Let frames settle & sweep camera slightly
                float tStart = Time.unscaledTime;
                while (Time.unscaledTime - tStart < 1.6f)
                {
                    if (cam != null)
                        cam.transform.RotateAround(anchor, Vector3.up, 24f * Time.deltaTime);
                    yield return null;
                }

                VfxQualitySystem.GetActiveParticleTelemetry(out int activeSys, out int activeParts);
                mgr.GetPoolStats(out int pooledInactive, out int activeTracked);

                if (gm != null && gm.PerfMonitor != null)
                {
                    var snap = gm.PerfMonitor.CaptureSnapshot();
                    sb.AppendLine(
                        $"[{tier}] FPS={snap.smoothedFps:F1} (1%Low={snap.onePercentLowFps:F1}) | " +
                        $"CPU={snap.cpuFrameTimeMs:F2}ms GPU={snap.gpuFrameTimeMs:F2}ms | " +
                        $"Draws={snap.drawCalls} Batches={snap.batches} SetPass={snap.setPassCalls} | " +
                        $"Particles={activeParts} (Systems={activeSys}) | GCAlloc={snap.gcAllocBytesPerFrame} B | " +
                        $"Pool(Active={activeTracked}, Inactive={pooledInactive})");
                }
                else
                {
                    sb.AppendLine($"[{tier}] ActiveSystems={activeSys}, ActiveParticles={activeParts}, Pool(Active={activeTracked}, Inactive={pooledInactive})");
                }
            }

            // Release spawned test loops cleanly and validate pool recovery
            foreach (var item in spawnedLoops)
                mgr.ReleaseVFX(item.Item1, item.Item2);

            yield return null;
            mgr.GetPoolStats(out int finalPooled, out int finalActive);
            sb.AppendLine($"Post-Release Pool Check: PooledInactive={finalPooled}, ActiveTracked={finalActive}");

            string report = sb.ToString();
            Debug.Log(report);

            string outPath = Path.Combine(Application.dataPath, "..", ReportFileName);
            File.WriteAllText(outPath, report, Encoding.UTF8);

            Destroy(gameObject);
        }
    }
}

