# Tools/GeneratePhase1PBRAndAudio.ps1
# Generates seamless 512x512 Dark Fantasy PBR Texture Maps (Albedo, Normal, MetallicSmoothness, AO, Emission, Decals)
# and 7 Biome Ambient Audio Loops (.wav) into Assets/Art/Textures/Environment, Assets/Audio/Ambience, and Assets/Resources.

$ErrorActionPreference = "Stop"

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class Phase1AssetSynthesizer
{
    private static float Hash(int x, int y, int period, int seed)
    {
        x = ((x % period) + period) % period;
        y = ((y % period) + period) % period;
        int n = x * 374761393 + y * 668265263 + seed * 1442695041;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float Smooth(float t) { return t * t * (3f - 2f * t); }

    private static float TileNoise(float u, float v, int freq, int seed)
    {
        float x = u * freq;
        float y = v * freq;
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);
        float fx = Smooth(x - ix);
        float fy = Smooth(y - iy);
        float a = Hash(ix, iy, freq, seed);
        float b = Hash(ix + 1, iy, freq, seed);
        float c = Hash(ix, iy + 1, freq, seed);
        float d = Hash(ix + 1, iy + 1, freq, seed);
        return (a * (1f - fx) + b * fx) * (1f - fy) + (c * (1f - fx) + d * fx) * fy;
    }

    private static float Fbm(float u, float v, int baseFreq, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, norm = 0f;
        int f = baseFreq;
        for (int i = 0; i < octaves; i++)
        {
            sum += TileNoise(u, v, f, seed + i * 31) * amp;
            norm += amp;
            amp *= 0.5f;
            f *= 2;
        }
        return sum / norm;
    }

    private static void SaveBytesToPng(string pathA, string pathB, int size, byte[] bgra)
    {
        using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            var rect = new Rectangle(0, 0, size, size);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bgra, 0, data.Scan0, bgra.Length);
            bmp.UnlockBits(data);
            bmp.Save(pathA, ImageFormat.Png);
            if (!string.IsNullOrEmpty(pathB)) bmp.Save(pathB, ImageFormat.Png);
        }
    }

    private static byte ClampB(float v)
    {
        int iv = (int)(v * 255f + 0.5f);
        if (iv < 0) return 0;
        if (iv > 255) return 255;
        return (byte)iv;
    }

    public static void GeneratePbrFamily(string dirArt, string dirRes, string prefix, int mode)
    {
        int S = 512;
        float[] H = new float[S * S];
        byte[] alb = new byte[S * S * 4];
        byte[] nrm = new byte[S * S * 4];
        byte[] msk = new byte[S * S * 4];
        byte[] ao  = new byte[S * S * 4];
        byte[] emi = (mode == 5) ? new byte[S * S * 4] : null;

        for (int y = 0; y < S; y++)
        {
            float v = y / (float)S;
            for (int x = 0; x < S; x++)
            {
                float u = x / (float)S;
                int idx = y * S + x;
                int p = idx * 4;

                float n1 = Fbm(u, v, 6, 4, 100 + mode * 17);
                float n2 = Fbm(u, v, 16, 3, 300 + mode * 29);
                float r = 0.25f, g = 0.25f, b = 0.28f, height = 0.5f, metal = 0.05f, smooth = 0.28f, occ = 1f;
                float er = 0f, eg = 0f, eb = 0f;

                if (mode == 0 || mode == 2) // GothicStone / MossyRuins
                {
                    float row = v * 4f;
                    int rIdx = (int)Math.Floor(row);
                    float fy = row - rIdx;
                    float col = (u + (rIdx % 2) * 0.5f) * 2f;
                    int cIdx = (int)Math.Floor(col);
                    float fx = col - cIdx;
                    float edge = Math.Min(Math.Min(fx, 1f - fx) * 14f, Math.Min(fy, 1f - fy) * 10f);
                    float bevel = Math.Min(1f, Math.Max(0f, edge));
                    float blockVar = Hash(cIdx, rIdx, 8, 77) * 0.14f - 0.07f;
                    height = bevel * (0.72f + n1 * 0.22f + n2 * 0.08f);
                    occ = 0.42f + 0.58f * bevel;
                    float baseVal = 0.27f + blockVar + n1 * 0.12f - (1f - bevel) * 0.14f;
                    r = baseVal * 1.04f; g = baseVal * 0.99f; b = baseVal * 0.96f;
                    smooth = 0.22f + n2 * 0.14f;

                    if (mode == 2) // Moss in crevices & damp stone
                    {
                        float mossMask = Math.Max(0f, Math.Min(1f, (1f - bevel) * 0.85f + (n1 - 0.48f) * 1.9f));
                        r = r * (1f - mossMask) + 0.13f * mossMask;
                        g = g * (1f - mossMask) + 0.29f * mossMask;
                        b = b * (1f - mossMask) + 0.14f * mossMask;
                        smooth = smooth * (1f - mossMask) + 0.42f * mossMask;
                    }
                }
                else if (mode == 1) // CathedralFloor flagstones
                {
                    float gx = (u * 3f) % 1f;
                    float gy = (v * 3f) % 1f;
                    float border = Math.Min(Math.Min(gx, 1f - gx), Math.Min(gy, 1f - gy)) * 16f;
                    float bev = Math.Min(1f, Math.Max(0f, border));
                    height = bev * (0.76f + n1 * 0.18f + n2 * 0.06f);
                    occ = 0.38f + 0.62f * bev;
                    float val = 0.23f + n1 * 0.11f - (1f - bev) * 0.12f;
                    r = val * 0.98f; g = val * 0.97f; b = val * 1.05f;
                    smooth = 0.34f + n2 * 0.18f;
                }
                else if (mode == 3) // WroughtIron
                {
                    float dimple = Fbm(u, v, 12, 4, 911);
                    height = 0.45f + dimple * 0.45f + n2 * 0.10f;
                    occ = 0.68f + dimple * 0.32f;
                    float val = 0.20f + dimple * 0.14f;
                    r = val * 0.96f; g = val * 0.99f; b = val * 1.08f;
                    metal = 0.86f;
                    smooth = 0.48f + dimple * 0.20f;
                }
                else if (mode == 4) // AgedOakWood
                {
                    float plank = (v * 5f) % 1f;
                    float seam = Math.Min(1f, Math.Min(plank, 1f - plank) * 18f);
                    float grain = Fbm(u * 0.35f, v * 3.5f, 8, 4, 555);
                    height = seam * (0.55f + grain * 0.40f);
                    occ = 0.45f + 0.55f * seam;
                    float w = 0.18f + grain * 0.16f;
                    r = w * 1.38f; g = w * 0.92f; b = w * 0.58f;
                    metal = 0.02f;
                    smooth = 0.24f + grain * 0.14f;
                }
                else if (mode == 5) // AbyssalCrystal
                {
                    float facet = Math.Abs(n1 - 0.5f) * 2f;
                    float vein = Math.Max(0f, 1f - Math.Abs(n2 - 0.5f) * 11f);
                    height = 0.35f + facet * 0.55f + vein * 0.1f;
                    occ = 0.65f + facet * 0.35f;
                    r = 0.07f + vein * 0.20f;
                    g = 0.20f + facet * 0.22f + vein * 0.65f;
                    b = 0.38f + facet * 0.35f + vein * 0.85f;
                    metal = 0.22f;
                    smooth = 0.82f;
                    er = vein * 0.15f; eg = vein * 0.72f; eb = vein * 1.0f;
                }

                H[idx] = height;
                alb[p + 0] = ClampB(b); alb[p + 1] = ClampB(g); alb[p + 2] = ClampB(r); alb[p + 3] = 255;
                // URP MetallicGlossMap: R = Metallic, A = Smoothness
                msk[p + 0] = ClampB(metal); msk[p + 1] = ClampB(occ); msk[p + 2] = ClampB(metal); msk[p + 3] = ClampB(smooth);
                ao[p + 0]  = ClampB(occ); ao[p + 1] = ClampB(occ); ao[p + 2] = ClampB(occ); ao[p + 3] = 255;

                if (emi != null)
                {
                    emi[p + 0] = ClampB(eb); emi[p + 1] = ClampB(eg); emi[p + 2] = ClampB(er); emi[p + 3] = 255;
                }
            }
        }

        // Compute tangent-space normal map from wrapped heightfield
        float strength = (mode == 3) ? 2.6f : 4.2f;
        for (int y = 0; y < S; y++)
        {
            int ym = (y - 1 + S) % S;
            int yp = (y + 1) % S;
            for (int x = 0; x < S; x++)
            {
                int xm = (x - 1 + S) % S;
                int xp = (x + 1) % S;
                float dx = (H[y * S + xm] - H[y * S + xp]) * strength;
                float dy = (H[ym * S + x] - H[yp * S + x]) * strength;
                float dz = 1f;
                float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                dx /= len; dy /= len; dz /= len;
                int p = (y * S + x) * 4;
                nrm[p + 0] = ClampB(dz * 0.5f + 0.5f); // B
                nrm[p + 1] = ClampB(dy * 0.5f + 0.5f); // G
                nrm[p + 2] = ClampB(dx * 0.5f + 0.5f); // R
                nrm[p + 3] = 255;
            }
        }

        SaveBytesToPng(Path.Combine(dirArt, prefix + "_Albedo.png"), Path.Combine(dirRes, prefix + "_Albedo.png"), S, alb);
        SaveBytesToPng(Path.Combine(dirArt, prefix + "_Normal.png"), Path.Combine(dirRes, prefix + "_Normal.png"), S, nrm);
        SaveBytesToPng(Path.Combine(dirArt, prefix + "_Mask.png"),   Path.Combine(dirRes, prefix + "_Mask.png"),   S, msk);
        if (emi != null)
            SaveBytesToPng(Path.Combine(dirArt, prefix + "_Emission.png"), Path.Combine(dirRes, prefix + "_Emission.png"), S, emi);
        else
            SaveBytesToPng(Path.Combine(dirArt, prefix + "_AO.png"), Path.Combine(dirRes, prefix + "_AO.png"), S, ao);
    }

    public static void GenerateDecals(string dirArt, string dirRes)
    {
        int S = 256;
        byte[] crack = new byte[S * S * 4];
        byte[] scorch = new byte[S * S * 4];

        for (int y = 0; y < S; y++)
        {
            float ny = (y / (float)S) * 2f - 1f;
            for (int x = 0; x < S; x++)
            {
                float nx = (x / (float)S) * 2f - 1f;
                float rad = (float)Math.Sqrt(nx * nx + ny * ny);
                float falloff = Math.Max(0f, 1f - rad);
                float u = x / (float)S, v = y / (float)S;
                float n1 = Fbm(u, v, 8, 4, 808);
                float n2 = Fbm(u, v, 16, 3, 909);
                int p = (y * S + x) * 4;

                // Cracks & Moss RGBA decal
                float line = Math.Max(0f, 1f - Math.Abs(n1 - 0.5f) * 14f) * falloff;
                float moss = Math.Max(0f, (n2 - 0.54f) * 2.5f) * falloff * falloff;
                float a1 = Math.Min(0.85f, line * 0.9f + moss * 0.75f);
                crack[p + 0] = ClampB(0.08f * (1f - moss) + 0.10f * moss);
                crack[p + 1] = ClampB(0.09f * (1f - moss) + 0.28f * moss);
                crack[p + 2] = ClampB(0.08f * (1f - moss) + 0.14f * moss);
                crack[p + 3] = ClampB(a1);

                // Battle Scorch & Ember Ash RGBA decal
                float sc = Math.Max(0f, (falloff * falloff * (0.65f + n1 * 0.6f)) - 0.12f);
                scorch[p + 0] = ClampB(0.05f);
                scorch[p + 1] = ClampB(0.05f + sc * 0.06f);
                scorch[p + 2] = ClampB(0.06f + sc * 0.12f);
                scorch[p + 3] = ClampB(Math.Min(0.82f, sc));
            }
        }

        SaveBytesToPng(Path.Combine(dirArt, "T_DecalCracksMoss.png"), Path.Combine(dirRes, "T_DecalCracksMoss.png"), S, crack);
        SaveBytesToPng(Path.Combine(dirArt, "T_DecalBattleScorch.png"), Path.Combine(dirRes, "T_DecalBattleScorch.png"), S, scorch);
    }

    public static void WriteAmbientWav(string pathA, string pathB, float baseHz, float shimmerHz, float lfoHz, int seed)
    {
        int sr = 22050;
        int seconds = 6;
        int count = sr * seconds;
        var rnd = new Random(seed);
        short[] pcm = new short[count];
        double twoPi = Math.PI * 2.0;
        float lp = 0f;

        for (int i = 0; i < count; i++)
        {
            double t = i / (double)sr;
            // Exact integer cycles over 6s for seamless click-free loop!
            double f1 = Math.Round(baseHz * seconds) / seconds;
            double f2 = Math.Round((baseHz * 1.498f) * seconds) / seconds;
            double f3 = Math.Round(shimmerHz * seconds) / seconds;
            double lfo = 0.65 + 0.35 * Math.Sin(twoPi * (Math.Round(lfoHz * seconds) / seconds) * t);

            float rawNoise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp = lp * 0.965f + rawNoise * 0.035f; // Low-passed wind/cavern air

            double sig = Math.Sin(twoPi * f1 * t) * 0.28 * lfo
                       + Math.Sin(twoPi * f2 * t) * 0.16
                       + Math.Sin(twoPi * f3 * t) * 0.08 * (0.5 + 0.5 * Math.Cos(twoPi * 0.5 * t))
                       + lp * 0.38;

            // Crossfade tail for zero-click loop
            double env = 1.0;
            if (i < 1200) env = i / 1200.0;
            else if (i > count - 1200) env = (count - i) / 1200.0;

            int val = (int)(sig * env * 9500.0);
            if (val < -32000) val = -32000;
            if (val > 32000) val = 32000;
            pcm[i] = (short)val;
        }

        WriteWavFile(pathA, sr, pcm);
        if (!string.IsNullOrEmpty(pathB)) WriteWavFile(pathB, sr, pcm);
    }

    private static void WriteWavFile(string path, int sr, short[] samples)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            int dataBytes = samples.Length * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataBytes);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sr);
            bw.Write(sr * 2);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataBytes);
            foreach (short s in samples) bw.Write(s);
        }
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies "System.Drawing"

$dirArt = Join-Path (Get-Location) "Assets\Art\Textures\Environment"
$dirRes = Join-Path (Get-Location) "Assets\Resources\Environment\PBR"
$dirAmb = Join-Path (Get-Location) "Assets\Audio\Ambience"
$dirResAud = Join-Path (Get-Location) "Assets\Resources\Audio"

New-Item -ItemType Directory -Force -Path $dirArt, $dirRes, $dirAmb, $dirResAud | Out-Null

[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_GothicStone", 0)
[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_CathedralFloor", 1)
[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_MossyRuins", 2)
[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_WroughtIron", 3)
[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_AgedOakWood", 4)
[Phase1AssetSynthesizer]::GeneratePbrFamily($dirArt, $dirRes, "T_AbyssalCrystal", 5)
[Phase1AssetSynthesizer]::GenerateDecals($dirArt, $dirRes)

[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_forgotten_temple.wav"),    (Join-Path $dirResAud "amb_forgotten_temple.wav"),    65.4, 261.6, 0.33, 11)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_ancient_dungeon.wav"),     (Join-Path $dirResAud "amb_ancient_dungeon.wav"),     55.0, 164.8, 0.25, 22)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_underground_fortress.wav"),(Join-Path $dirResAud "amb_underground_fortress.wav"),49.0, 146.8, 0.50, 33)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_abyssal_cave.wav"),        (Join-Path $dirResAud "amb_abyssal_cave.wav"),        51.9, 207.6, 0.20, 44)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_crystal_chamber.wav"),     (Join-Path $dirResAud "amb_crystal_chamber.wav"),     98.0, 392.0, 0.40, 55)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_cursed_forest.wav"),       (Join-Path $dirResAud "amb_cursed_forest.wav"),       58.3, 233.1, 0.30, 66)
[Phase1AssetSynthesizer]::WriteAmbientWav((Join-Path $dirAmb "amb_boss_arena.wav"),          (Join-Path $dirResAud "amb_boss_arena.wav"),          43.6, 174.6, 0.66, 77)

Write-Host "[OK] Generated 26 PBR PNG maps + 7 Spatial Biome Ambience .wav files!"

