using System;
using UnityEngine;

namespace AbyssGame
{
    public enum GpuHardwareClass
    {
        UnknownOrFallback = 0,
        IntegratedEntry = 1,       // Intel HD / UHD 620 / 630, older APUs
        IntegratedModern = 2,      // Intel Iris Xe / Plus, AMD Vega 7/8 / Radeon 680M/780M, Apple M-series base
        DiscreteEntry = 3,         // GTX 1050 / 1650, RTX 2050, RX 560 / 570 / 6400, Arc A380, MX450/550
        DiscreteMidRange = 4,      // GTX 1660 Ti, RTX 2060 / 3050 / 3060 / 4060, RX 6600 / 7600, Arc A750/A770
        DiscreteHighEnd = 5        // RTX 3070 / 3080 / 4070 / 4080 / 4090, RX 6800 / 7800 XT / 7900 XTX
    }

    [Serializable]
    public struct HardwareDetectionReport
    {
        public string gpuName;
        public string gpuVendor;
        public int vendorId;
        public int deviceId;
        public string graphicsApi;
        public int reportedVramMB;
        public int estimatedDedicatedVramMB;
        public int systemRamMB;
        public string cpuName;
        public int cpuCores;
        public int cpuFrequencyMHz;
        public int shaderLevel;
        public bool supportsInstancing;
        public bool supportsComputeShaders;
        public bool supportsShadows;
        public bool supports2DArrayTextures;
        public bool supportsAsyncGPUReadback;
        public int maxTextureSize;
        public int supportedRenderTargetCount;
        public bool isIntegratedOrSharedMemory;
        public int screenWidth;
        public int screenHeight;
        public GpuHardwareClass hardwareClass;
        public GraphicsQualityTier staticRecommendedTier;
        public string recommendationSummary;
    }

    /// <summary>
    /// Conservative multi-signal hardware capability detector for Unity 2021.3.11f1 + URP 12.1.7.
    /// Evaluates SystemInfo.graphicsDeviceName, graphicsMemorySize, systemMemorySize, processorCount,
    /// processorFrequency, graphicsShaderLevel, and real GPU capabilities (MRT, Compute, Instancing, MaxTexSize).
    /// Never trusts GPU name or Windows WDDM shared-memory VRAM reporting alone.
    /// </summary>
    public static class GraphicsDetector
    {
        private const int VendorIntel = 0x8086;  // 32902
        private const int VendorNvidia = 0x10DE; // 4318
        private const int VendorAmd = 0x1002;    // 4098
        private const int VendorApple = 0x106B;  // 4203

        public static HardwareDetectionReport AnalyzeHardware()
        {
            string rawName = SystemInfo.graphicsDeviceName ?? "Unknown GPU";
            string rawVendor = SystemInfo.graphicsDeviceVendor ?? "Unknown Vendor";
            int vendorId = SystemInfo.graphicsDeviceVendorID;
            int deviceId = SystemInfo.graphicsDeviceID;
            int reportedVram = Mathf.Max(0, SystemInfo.graphicsMemorySize);
            int sysRam = Mathf.Max(0, SystemInfo.systemMemorySize);
            int cores = Mathf.Max(1, SystemInfo.processorCount);
            int freqMhz = Mathf.Max(0, SystemInfo.processorFrequency);
            int shaderLvl = SystemInfo.graphicsShaderLevel;

            bool instancing = SystemInfo.supportsInstancing;
            bool compute = SystemInfo.supportsComputeShaders;
            bool shadows = SystemInfo.supportsShadows;
            bool tex2DArray = SystemInfo.supports2DArrayTextures;
            bool asyncReadback = SystemInfo.supportsAsyncGPUReadback;
            int maxTex = SystemInfo.maxTextureSize;
            int mrtCount = SystemInfo.supportedRenderTargetCount;

            int w = Mathf.Max(640, Screen.width);
            int h = Mathf.Max(360, Screen.height);

            string upperName = rawName.ToUpperInvariant();
            string upperVendor = rawVendor.ToUpperInvariant();

            bool isSharedMemory = DetectSharedMemoryArchitecture(upperName, upperVendor, vendorId, reportedVram, sysRam);
            int effectiveVram = EstimateEffectiveVramMB(reportedVram, sysRam, isSharedMemory);

            GpuHardwareClass hwClass = ClassifyGpuConservatively(
                upperName, upperVendor, vendorId, isSharedMemory, effectiveVram, sysRam,
                cores, freqMhz, shaderLvl, compute, instancing, tex2DArray, maxTex, mrtCount);

            GraphicsQualityTier recommended = DetermineConservativePreset(
                hwClass, isSharedMemory, effectiveVram, sysRam, cores, freqMhz,
                shaderLvl, compute, instancing, shadows, mrtCount, w, h, out string reason);

            return new HardwareDetectionReport
            {
                gpuName = rawName,
                gpuVendor = rawVendor,
                vendorId = vendorId,
                deviceId = deviceId,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                reportedVramMB = reportedVram,
                estimatedDedicatedVramMB = effectiveVram,
                systemRamMB = sysRam,
                cpuName = SystemInfo.processorType ?? "Unknown CPU",
                cpuCores = cores,
                cpuFrequencyMHz = freqMhz,
                shaderLevel = shaderLvl,
                supportsInstancing = instancing,
                supportsComputeShaders = compute,
                supportsShadows = shadows,
                supports2DArrayTextures = tex2DArray,
                supportsAsyncGPUReadback = asyncReadback,
                maxTextureSize = maxTex,
                supportedRenderTargetCount = mrtCount,
                isIntegratedOrSharedMemory = isSharedMemory,
                screenWidth = w,
                screenHeight = h,
                hardwareClass = hwClass,
                staticRecommendedTier = recommended,
                recommendationSummary = reason
            };
        }

        private static bool DetectSharedMemoryArchitecture(string upperName, string upperVendor, int vendorId, int reportedVram, int sysRam)
        {
            // 1. Intel GPUs are integrated EXCEPT Intel Arc discrete series (A380/A580/A750/A770/B570/B580)
            if (vendorId == VendorIntel || upperVendor.Contains("INTEL") || upperName.Contains("INTEL"))
            {
                bool isDiscreteArc = upperName.Contains("ARC") &&
                                    (upperName.Contains("A380") || upperName.Contains("A580") ||
                                     upperName.Contains("A750") || upperName.Contains("A770") ||
                                     upperName.Contains("B570") || upperName.Contains("B580"));
                if (!isDiscreteArc)
                    return true;
            }

            // 2. Explicit integrated naming across Intel / AMD / Qualcomm / Mobile
            if (upperName.Contains("IRIS") || upperName.Contains("UHD") || upperName.Contains("HD GRAPHICS") ||
                upperName.Contains("VEGA 6") || upperName.Contains("VEGA 7") || upperName.Contains("VEGA 8") ||
                upperName.Contains("VEGA 10") || upperName.Contains("VEGA 11") ||
                upperName.Contains("RADEON(TM) GRAPHICS") || upperName.Contains("RADEON GRAPHICS") ||
                upperName.Contains("610M") || upperName.Contains("660M") || upperName.Contains("680M") ||
                upperName.Contains("740M") || upperName.Contains("760M") || upperName.Contains("780M") ||
                upperName.Contains("ADRENO") || upperName.Contains("MALI") || upperName.Contains("APPLE M"))
            {
                return true;
            }

            // 3. Heuristic for Windows WDDM shared aperture: if reported VRAM is within 128 MB of 50% of System RAM
            if (sysRam > 4000 && Mathf.Abs(reportedVram - (sysRam / 2)) <= 128)
            {
                bool hasExplicitDiscreteTag = upperName.Contains("RTX") || upperName.Contains("GTX") ||
                                              upperName.Contains("RADEON RX") || upperName.Contains("QUADRO");
                if (!hasExplicitDiscreteTag)
                    return true;
            }

            return false;
        }

        private static int EstimateEffectiveVramMB(int reportedVram, int sysRam, bool isSharedMemory)
        {
            if (!isSharedMemory)
                return reportedVram;

            if (sysRam >= 15000) return Mathf.Min(reportedVram, 2048);
            if (sysRam >= 11000) return Mathf.Min(reportedVram, 1536);
            return Mathf.Min(reportedVram, 1024);
        }

        private static GpuHardwareClass ClassifyGpuConservatively(
            string upperName,
            string upperVendor,
            int vendorId,
            bool isSharedMemory,
            int effectiveVramMB,
            int sysRamMB,
            int cpuCores,
            int cpuFreqMHz,
            int shaderLevel,
            bool supportsCompute,
            bool supportsInstancing,
            bool supports2DArray,
            int maxTexSize,
            int mrtCount)
        {
            if (shaderLevel < 45 || !supportsCompute || !supportsInstancing || !supports2DArray || maxTexSize < 8192 || mrtCount < 4)
                return GpuHardwareClass.IntegratedEntry;

            if (isSharedMemory)
            {
                bool isModernIgpus = upperName.Contains("IRIS") || upperName.Contains("XE") ||
                                     upperName.Contains("680M") || upperName.Contains("780M") ||
                                     upperName.Contains("760M") || upperName.Contains("VEGA") ||
                                     upperName.Contains("APPLE M");
                bool cpuAdequate = cpuCores >= 4 && (cpuFreqMHz == 0 || cpuFreqMHz >= 1800);
                if (isModernIgpus && sysRamMB >= 11000 && cpuAdequate)
                    return GpuHardwareClass.IntegratedModern;

                return GpuHardwareClass.IntegratedEntry;
            }

            bool isEntryDiscreteName =
                upperName.Contains("1650") || upperName.Contains("2050") || upperName.Contains("1050") ||
                upperName.Contains("1060") || upperName.Contains("1630") || upperName.Contains("MX") ||
                upperName.Contains("RX 550") || upperName.Contains("RX 560") || upperName.Contains("RX 570") ||
                upperName.Contains("RX 580") || upperName.Contains("RX 6400") || upperName.Contains("RX 6500") ||
                upperName.Contains("A380") || upperName.Contains("T600") || upperName.Contains("T1000");

            if (isEntryDiscreteName || effectiveVramMB < 5500)
            {
                if (effectiveVramMB < 2800)
                    return GpuHardwareClass.IntegratedModern;
                return GpuHardwareClass.DiscreteEntry;
            }

            bool isHighEndDiscreteName =
                upperName.Contains("3080") || upperName.Contains("3080 TI") || upperName.Contains("3090") ||
                upperName.Contains("4070") || upperName.Contains("4080") || upperName.Contains("4090") ||
                upperName.Contains("7800 XT") || upperName.Contains("7900") || upperName.Contains("6800 XT") ||
                upperName.Contains("6900") || upperName.Contains("6950");

            if (isHighEndDiscreteName && effectiveVramMB >= 9500 && sysRamMB >= 15000 && cpuCores >= 8)
                return GpuHardwareClass.DiscreteHighEnd;

            if (effectiveVramMB >= 5800 && sysRamMB >= 11000 && cpuCores >= 6)
                return GpuHardwareClass.DiscreteMidRange;

            return GpuHardwareClass.DiscreteEntry;
        }

        private static GraphicsQualityTier DetermineConservativePreset(
            GpuHardwareClass hwClass,
            bool isSharedMemory,
            int effectiveVramMB,
            int sysRamMB,
            int cpuCores,
            int cpuFreqMHz,
            int shaderLevel,
            bool supportsCompute,
            bool supportsInstancing,
            bool supportsShadows,
            int mrtCount,
            int screenWidth,
            int screenHeight,
            out string reason)
        {
            if (!supportsShadows || !supportsInstancing || shaderLevel < 45 || mrtCount < 4)
            {
                reason = "Thiếu tính năng Shader/MRT/Shadow hiện đại -> Khuyến nghị Thấp (Low).";
                return GraphicsQualityTier.Low;
            }

            long pixelCount = (long)screenWidth * screenHeight;
            bool isHighResDisplay = pixelCount > (2560L * 1440L);
            bool is1440pDisplay = pixelCount > (1920L * 1200L);
            string freqTag = cpuFreqMHz > 0 ? $"{cpuCores} nhân @ {cpuFreqMHz}MHz" : $"{cpuCores} nhân";

            GraphicsQualityTier tier;
            switch (hwClass)
            {
                case GpuHardwareClass.IntegratedEntry:
                case GpuHardwareClass.IntegratedModern:
                    // Per Section 2 Performance Goals: Integrated GPU / laptop without discrete GPU -> Low, targeting 1080p 60 FPS
                    tier = GraphicsQualityTier.Low;
                    reason = $"GPU tích hợp ({SystemInfo.graphicsDeviceName}, RAM dùng chung ~{effectiveVramMB} MB, CPU {freqTag}) -> Khuyến nghị Low (ưu tiên 1080p 60 FPS).";
                    break;

                case GpuHardwareClass.DiscreteEntry:
                    // GTX 1650 4GB / RTX 2050 class: High @ 1080p, Medium @ 1440p+
                    if (isHighResDisplay)
                        tier = GraphicsQualityTier.Low;
                    else if (is1440pDisplay || effectiveVramMB < 3600 || cpuCores < 4)
                        tier = GraphicsQualityTier.Medium;
                    else
                        tier = GraphicsQualityTier.High;

                    reason = $"GPU rời phổ thông ({SystemInfo.graphicsDeviceName}, VRAM ~{effectiveVramMB} MB, CPU {freqTag}, {screenWidth}x{screenHeight}) -> Khuyến nghị {tier}.";
                    break;

                case GpuHardwareClass.DiscreteMidRange:
                    tier = isHighResDisplay ? GraphicsQualityTier.High : GraphicsQualityTier.Ultra;
                    reason = $"GPU rời tầm trung-cao ({SystemInfo.graphicsDeviceName}, VRAM {effectiveVramMB} MB, CPU {freqTag}) -> Khuyến nghị {tier}.";
                    break;

                case GpuHardwareClass.DiscreteHighEnd:
                    tier = GraphicsQualityTier.Ultra;
                    reason = $"GPU rời cao cấp ({SystemInfo.graphicsDeviceName}, VRAM {effectiveVramMB} MB, CPU {freqTag}) -> Khuyến nghị Ultra (hỗ trợ bật Cinematic).";
                    break;

                default:
                    tier = GraphicsQualityTier.Medium;
                    reason = "Phần cứng chưa xác định rõ -> Khuyến nghị an toàn Medium.";
                    break;
            }

            return tier;
        }
    }
}
