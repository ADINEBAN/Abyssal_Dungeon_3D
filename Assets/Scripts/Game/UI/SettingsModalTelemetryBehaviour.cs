using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    public class SettingsModalTelemetryBehaviour : MonoBehaviour
    {
        public Func<Text> GetTelemetryText;
        private float _timer;

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.35f;

            Text txt = GetTelemetryText?.Invoke();
            var gm = GraphicsManager.Instance;
            if (txt == null || gm == null) return;

            var hw = gm.HardwareReport;
            var pm = gm.PerfMonitor;
            var aq = gm.AdaptiveController;
            float fps = pm != null ? pm.SmoothedFps : (1f / Mathf.Max(0.001f, Time.unscaledDeltaTime));
            float low1 = pm != null ? pm.OnePercentLowFps : fps;
            float ms = pm != null ? pm.SmoothedCpuFrameTimeMs : (Time.unscaledDeltaTime * 1000f);
            float rScale = aq != null ? aq.CurrentRenderScale : (gm.SettingsData != null ? gm.SettingsData.renderScale : 1f);
            long draws = pm != null ? pm.DrawCallsCount : 0;
            long batches = pm != null ? pm.BatchesCount : 0;

            string memType = hw.isIntegratedOrSharedMemory
                ? $"RAM dùng chung ({hw.reportedVramMB} MB • An toàn: ~{hw.estimatedDedicatedVramMB} MB)"
                : $"VRAM rời: {hw.reportedVramMB} MB (RAM: {hw.systemRamMB} MB)";

            txt.text =
                $"<color=#F8DC85>PHẦN CỨNG:</color> {hw.gpuName} ({hw.graphicsApi})  |  <color=#78E888>FPS: {fps:0} (1% Low: {low1:0} • {ms:0.1} ms) • Scale: {Mathf.RoundToInt(rScale * 100)}% • DrawCalls: {draws} (Batches: {batches})</color>\n" +
                $"• Bộ nhớ Đồ họa: <color=#EDE0BA>{memType}</color> • Đang cấp phát: <color=#EDE0BA>{(pm != null ? pm.TotalAllocatedMemoryMB : 0f):0} MB</color>\n" +
                $"• Khuyến nghị: <color=#78E888>{hw.staticRecommendedTier.ToString().ToUpper()}</color> — {(aq != null ? aq.StatusSummary : hw.recommendationSummary)}";
        }
    }
}
