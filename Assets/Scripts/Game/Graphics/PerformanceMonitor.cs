using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace AbyssGame
{
    [Serializable]
    public struct PerformanceTelemetrySnapshot
    {
        public float cpuFrameTimeMs;
        public float gpuFrameTimeMs;
        public float smoothedFps;
        public float onePercentLowFps;
        public long drawCalls;
        public long batches;
        public long setPassCalls;
        public long triangles;
        public long vertices;
        public float totalAllocatedMemoryMB;
        public float totalReservedMemoryMB;
        public float monoHeapUsedMB;
        public long gcAllocBytesPerFrame;
        public int activeParticleSystems;
        public int activeParticlesCount;
        public float lastSceneLoadTimeMs;
    }

    /// <summary>
    /// Zero-GC runtime performance profiler and telemetry recorder for Unity 2021.3.11f1 + URP 12.1.7.
    /// Measures CPU Frame Time, GPU Frame Time (FrameTimingManager / ProfilerRecorder), FPS, 1% Low FPS,
    /// Draw Calls, SetPass Calls, Batches, Memory, GC Allocations, Particle Count, and Scene Loading Time.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        public float SmoothedCpuFrameTimeMs { get; private set; } = 16.67f;
        public float SmoothedGpuFrameTimeMs { get; private set; } = 0f;
        public float EffectiveFrameTimeMs => Mathf.Max(SmoothedCpuFrameTimeMs, SmoothedGpuFrameTimeMs);
        public float SmoothedFps => SmoothedCpuFrameTimeMs > 0.1f ? (1000f / SmoothedCpuFrameTimeMs) : 60f;
        public float OnePercentLowFps { get; private set; } = 60f;

        public long DrawCallsCount { get; private set; }
        public long BatchesCount { get; private set; }
        public long SetPassCallsCount { get; private set; }
        public long TrianglesCount { get; private set; }
        public long VerticesCount { get; private set; }

        public float TotalAllocatedMemoryMB { get; private set; }
        public float TotalReservedMemoryMB { get; private set; }
        public float MonoHeapUsedMB { get; private set; }
        public long GcAllocBytesPerFrame { get; private set; }

        public int ActiveParticleSystemsCount { get; private set; }
        public int ActiveParticlesCount { get; private set; }
        public int RegisteredPooledVfxCount { get; set; }
        public float LastSceneLoadTimeMs { get; private set; }

        private const int HistoryCapacity = 120;
        private readonly float[] _frameMsHistory = new float[HistoryCapacity];
        private int _historyWriteIdx;
        private int _historyCount;

        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        private float _slowStatsTimer;
        private long _prevMonoUsedBytes;
        private float _sceneLoadStartRealtime;

        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _setPassRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private ProfilerRecorder _verticesRecorder;
        private ProfilerRecorder _gcAllocRecorder;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneLoadStartRealtime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            _drawCallsRecorder.Dispose();
            _batchesRecorder.Dispose();
            _setPassRecorder.Dispose();
            _trianglesRecorder.Dispose();
            _verticesRecorder.Dispose();
            _gcAllocRecorder.Dispose();

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            _sceneLoadStartRealtime = Time.realtimeSinceStartup;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LastSceneLoadTimeMs = Mathf.Max(1f, (Time.realtimeSinceStartup - _sceneLoadStartRealtime) * 1000f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0001f || dt > 0.25f)
                return;

            float cpuMs = dt * 1000f;
            SmoothedCpuFrameTimeMs = Mathf.Lerp(SmoothedCpuFrameTimeMs, cpuMs, 0.08f);

            // Record into circular buffer for 1% low calculation
            _frameMsHistory[_historyWriteIdx] = cpuMs;
            _historyWriteIdx = (_historyWriteIdx + 1) % HistoryCapacity;
            if (_historyCount < HistoryCapacity) _historyCount++;

            // Capture GPU Frame Time via FrameTimingManager if supported
            FrameTimingManager.CaptureFrameTimings();
            uint numTimings = FrameTimingManager.GetLatestTimings(1, _frameTimings);
            if (numTimings > 0 && _frameTimings[0].gpuFrameTime > 0.01)
            {
                float gpuMs = (float)_frameTimings[0].gpuFrameTime;
                SmoothedGpuFrameTimeMs = SmoothedGpuFrameTimeMs <= 0.01f
                    ? gpuMs
                    : Mathf.Lerp(SmoothedGpuFrameTimeMs, gpuMs, 0.10f);
            }

            // Read ProfilerRecorder render counters (zero allocation)
            if (_drawCallsRecorder.Valid) DrawCallsCount = _drawCallsRecorder.LastValue;
            if (_batchesRecorder.Valid) BatchesCount = _batchesRecorder.LastValue;
            if (_setPassRecorder.Valid) SetPassCallsCount = _setPassRecorder.LastValue;
            if (_trianglesRecorder.Valid) TrianglesCount = _trianglesRecorder.LastValue;
            if (_verticesRecorder.Valid) VerticesCount = _verticesRecorder.LastValue;

            long monoNow = Profiler.GetMonoUsedSizeLong();
            if (_gcAllocRecorder.Valid && _gcAllocRecorder.LastValue >= 0)
            {
                GcAllocBytesPerFrame = _gcAllocRecorder.LastValue;
            }
            else
            {
                long delta = monoNow - _prevMonoUsedBytes;
                GcAllocBytesPerFrame = delta > 0 ? delta : 0;
            }
            _prevMonoUsedBytes = monoNow;

            // Update lower-frequency memory, 1% low, and particle telemetry every 0.5s
            _slowStatsTimer -= dt;
            if (_slowStatsTimer <= 0f)
            {
                _slowStatsTimer = 0.5f;
                RefreshSlowTelemetry(monoNow);
            }
        }

        private void RefreshSlowTelemetry(long monoUsedBytes)
        {
            const float invMB = 1f / (1024f * 1024f);
            TotalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() * invMB;
            TotalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() * invMB;
            MonoHeapUsedMB = monoUsedBytes * invMB;

            // Compute worst frame time in 120-frame window (~1% low) without allocating
            if (_historyCount > 0)
            {
                float worstMs = 0f;
                for (int i = 0; i < _historyCount; i++)
                {
                    if (_frameMsHistory[i] > worstMs)
                        worstMs = _frameMsHistory[i];
                }
                OnePercentLowFps = worstMs > 0.1f ? (1000f / worstMs) : SmoothedFps;
            }

            // Use VfxQualitySystem registry to avoid scene-wide FindObjectsOfType
            VfxQualitySystem.GetActiveParticleTelemetry(out int sysCount, out int partCount);
            ActiveParticleSystemsCount = sysCount;
            ActiveParticlesCount = partCount;
        }

        public PerformanceTelemetrySnapshot CaptureSnapshot()
        {
            return new PerformanceTelemetrySnapshot
            {
                cpuFrameTimeMs = SmoothedCpuFrameTimeMs,
                gpuFrameTimeMs = SmoothedGpuFrameTimeMs,
                smoothedFps = SmoothedFps,
                onePercentLowFps = OnePercentLowFps,
                drawCalls = DrawCallsCount,
                batches = BatchesCount,
                setPassCalls = SetPassCallsCount,
                triangles = TrianglesCount,
                vertices = VerticesCount,
                totalAllocatedMemoryMB = TotalAllocatedMemoryMB,
                totalReservedMemoryMB = TotalReservedMemoryMB,
                monoHeapUsedMB = MonoHeapUsedMB,
                gcAllocBytesPerFrame = GcAllocBytesPerFrame,
                activeParticleSystems = ActiveParticleSystemsCount,
                activeParticlesCount = ActiveParticlesCount,
                lastSceneLoadTimeMs = LastSceneLoadTimeMs
            };
        }
    }
}

