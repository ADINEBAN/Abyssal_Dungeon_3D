using System;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Runtime verification benchmark runner and facade bridging PerformanceMonitor and AdaptiveQualityController.
    /// Zero per-frame GC allocations.
    /// </summary>
    public class GraphicsAutoDetect : MonoBehaviour
    {
        public float SmoothedFrameTimeMs => PerformanceMonitor.Instance != null
            ? PerformanceMonitor.Instance.EffectiveFrameTimeMs
            : 16.67f;

        public float SmoothedFps => PerformanceMonitor.Instance != null
            ? PerformanceMonitor.Instance.SmoothedFps
            : 60f;

        public float CurrentRenderScale => AdaptiveQualityController.Instance != null
            ? AdaptiveQualityController.Instance.CurrentRenderScale
            : 1.0f;

        public GraphicsQualityTier RuntimeRecommendedTier { get; private set; } = GraphicsQualityTier.Low;
        public string RuntimeStatusMessage { get; private set; } = "Đang đo đạc hiệu năng thời gian thực...";

        private int _warmupFramesRemaining = 25;
        private bool _benchmarkActive;
        private float _benchmarkTimer;
        private float _benchmarkSumMs;
        private int _benchmarkSampleCount;
        private Action<GraphicsQualityTier, string> _onBenchmarkComplete;

        public void NotifySceneOrPresetChanged(float baseRenderScale, float baseShadowDist = 50f, float baseVfxDensity = 1.0f)
        {
            _warmupFramesRemaining = 30;
            if (AdaptiveQualityController.Instance != null)
            {
                AdaptiveQualityController.Instance.ResetBaselineFromPreset(baseRenderScale, baseShadowDist, baseVfxDensity);
            }
        }

        /// <summary>
        /// Starts a 2.5-second live frame-time benchmark to verify static hardware detection against real GPU frame times.
        /// </summary>
        public void StartRuntimeVerificationBenchmark(GraphicsQualityTier staticCandidate, Action<GraphicsQualityTier, string> onCompleted = null)
        {
            RuntimeRecommendedTier = staticCandidate;
            _warmupFramesRemaining = 25;
            _benchmarkActive = true;
            _benchmarkTimer = 2.5f;
            _benchmarkSumMs = 0f;
            _benchmarkSampleCount = 0;
            _onBenchmarkComplete = onCompleted;
            RuntimeStatusMessage = $"Đang kiểm chứng hiệu năng thực tế ở mức {staticCandidate}...";
        }

        private void Update()
        {
            if (!_benchmarkActive)
            {
                if (AdaptiveQualityController.Instance != null && !string.IsNullOrEmpty(AdaptiveQualityController.Instance.StatusSummary))
                {
                    RuntimeStatusMessage = AdaptiveQualityController.Instance.StatusSummary;
                }
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0001f || dt > 0.20f)
                return;

            if (_warmupFramesRemaining > 0)
            {
                _warmupFramesRemaining--;
                return;
            }

            float frameMs = SmoothedFrameTimeMs;
            _benchmarkTimer -= dt;
            _benchmarkSumMs += frameMs;
            _benchmarkSampleCount++;

            if (_benchmarkTimer <= 0f)
            {
                _benchmarkActive = false;
                float avgMs = _benchmarkSampleCount > 0 ? (_benchmarkSumMs / _benchmarkSampleCount) : frameMs;
                float avgFps = avgMs > 0.1f ? (1000f / avgMs) : 60f;

                GraphicsQualityTier verified = RuntimeRecommendedTier;
                if (avgMs > 22.5f && verified > GraphicsQualityTier.Low)
                {
                    verified = (GraphicsQualityTier)((int)verified - 1);
                    RuntimeStatusMessage = $"Đo thực tế: {avgFps:0} FPS ({avgMs:0.0} ms) -> Điều chỉnh khuyến nghị an toàn xuống {verified}.";
                }
                else
                {
                    RuntimeStatusMessage = $"Đo thực tế ổn định: {avgFps:0} FPS ({avgMs:0.0} ms) -> Xác nhận khuyến nghị {verified}.";
                }

                RuntimeRecommendedTier = verified;
                var cb = _onBenchmarkComplete;
                _onBenchmarkComplete = null;
                cb?.Invoke(verified, RuntimeStatusMessage);
            }
        }
    }
}
