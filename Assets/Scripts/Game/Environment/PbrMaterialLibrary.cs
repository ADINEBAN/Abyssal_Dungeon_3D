using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Shared URP PBR & Custom HLSL Material Library for Phase 1 Dark Fantasy environments.
    /// Every material uses shared instances + GPU Instancing / SRP Batcher compatibility to minimize Draw Calls & SetPass calls.
    /// </summary>
    public static class PbrMaterialLibrary
    {
        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static Texture2D _softParticleTex;
        private static Texture2D _softShadowTex;

        public static Material GothicStone(Color? tint = null)
        {
            Color c = tint ?? new Color(0.84f, 0.78f, 0.72f, 1f);
            string key = "GothicStone_" + ColorUtility.ToHtmlStringRGB(c);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
                BuildUrpPbr("M_" + key, "T_GothicStone", c, metallic: 0.06f, smoothness: 0.28f, bumpScale: 1.15f, aoStrength: 0.85f));
            _cache[key] = m;
            return m;
        }

        public static Material CathedralFloor(Color? tint = null)
        {
            Color c = tint ?? new Color(0.80f, 0.76f, 0.72f, 1f);
            string key = "CathedralFloor_" + ColorUtility.ToHtmlStringRGB(c);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
                BuildUrpPbr("M_" + key, "T_CathedralFloor", c, metallic: 0.08f, smoothness: 0.36f, bumpScale: 1.10f, aoStrength: 0.88f));
            _cache[key] = m;
            return m;
        }

        public static Material MossyRuins(Color? tint = null)
        {
            Color c = tint ?? new Color(0.78f, 0.84f, 0.76f, 1f);
            string key = "MossyRuins_" + ColorUtility.ToHtmlStringRGB(c);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
                BuildUrpPbr("M_" + key, "T_MossyRuins", c, metallic: 0.04f, smoothness: 0.38f, bumpScale: 1.20f, aoStrength: 0.90f));
            _cache[key] = m;
            return m;
        }

        public static Material WroughtIron()
        {
            if (_cache.TryGetValue("WroughtIron", out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_WroughtIron", () =>
                BuildUrpPbr("M_WroughtIron", "T_WroughtIron", new Color(0.76f, 0.79f, 0.86f, 1f), metallic: 0.86f, smoothness: 0.54f, bumpScale: 0.95f, aoStrength: 0.80f));
            _cache["WroughtIron"] = m;
            return m;
        }

        public static Material AgedOakWood()
        {
            if (_cache.TryGetValue("AgedOakWood", out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_AgedOakWood", () =>
                BuildUrpPbr("M_AgedOakWood", "T_AgedOakWood", new Color(0.86f, 0.74f, 0.62f, 1f), metallic: 0.03f, smoothness: 0.28f, bumpScale: 1.05f, aoStrength: 0.85f));
            _cache["AgedOakWood"] = m;
            return m;
        }

        public static Material RunicGold()
        {
            if (_cache.TryGetValue("RunicGold", out var m) && m != null) return m;
            m = BakedAssetRegistry.GetOrCreateMaterial("M_RunicGold", () =>
                BuildUrpPbr("M_RunicGold", "T_WroughtIron", new Color(0.92f, 0.72f, 0.30f, 1f), metallic: 0.88f, smoothness: 0.68f, bumpScale: 0.65f, aoStrength: 0.65f));
            _cache["RunicGold"] = m;
            return m;
        }

        public static Material AbyssalCrystal(Color baseCol, Color rimCol, float pulseSpeed = 2.2f)
        {
            string key = $"Crystal_{ColorUtility.ToHtmlStringRGB(baseCol)}_{ColorUtility.ToHtmlStringRGB(rimCol)}";
            if (_cache.TryGetValue(key, out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
            {
                Shader sh = Resources.Load<Shader>("Shaders/AbyssalCrystalLit")
                            ?? Shader.Find("AbyssalDungeon/CrystalLit")
                            ?? Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

                var mat = new Material(sh) { name = "M_" + key, enableInstancing = true };
                Texture2D alb = LoadPbrTex("T_AbyssalCrystal_Albedo");
                Texture2D nrm = LoadPbrTex("T_AbyssalCrystal_Normal");
                Texture2D emi = LoadPbrTex("T_AbyssalCrystal_Emission");

                if (mat.HasProperty("_BaseMap") && alb != null) mat.SetTexture("_BaseMap", alb);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                if (mat.HasProperty("_BumpMap") && nrm != null) mat.SetTexture("_BumpMap", nrm);
                if (mat.HasProperty("_EmissionMap") && emi != null) mat.SetTexture("_EmissionMap", emi);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", rimCol * 1.35f);
                if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", rimCol * 1.65f);
                if (mat.HasProperty("_PulseSpeed")) mat.SetFloat("_PulseSpeed", pulseSpeed);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.86f);
                mat.EnableKeyword("_EMISSION");
                return mat;
            });

            _cache[key] = m;
            return m;
        }

        public static Material TorchFlame(Color outerCol, Color coreCol)
        {
            string key = $"Flame_{ColorUtility.ToHtmlStringRGB(outerCol)}";
            if (_cache.TryGetValue(key, out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
            {
                Shader sh = Resources.Load<Shader>("Shaders/AbyssalTorchFlame")
                            ?? Shader.Find("AbyssalDungeon/TorchFlame");

                if (sh != null)
                {
                    var mat = new Material(sh) { name = "M_" + key, enableInstancing = true };
                    mat.SetColor("_CoreColor", coreCol * 2.4f);
                    mat.SetColor("_OuterColor", outerCol * 1.9f);
                    return mat;
                }
                return CharacterVisualFactory.MakeTransparentMat(new Color(outerCol.r, outerCol.g, outerCol.b, 0.85f), outerCol * 2.5f);
            });
            _cache[key] = m;
            return m;
        }

        public static Material GroundMist(Color mistColor)
        {
            string key = $"Mist_{ColorUtility.ToHtmlStringRGBA(mistColor)}";
            if (_cache.TryGetValue(key, out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
            {
                Shader sh = Resources.Load<Shader>("Shaders/AbyssalGroundMist")
                            ?? Shader.Find("AbyssalDungeon/GroundMist");

                if (sh != null)
                {
                    var mat = new Material(sh) { name = "M_" + key, enableInstancing = true };
                    mat.SetColor("_MistColor", mistColor);
                    return mat;
                }
                return CharacterVisualFactory.MakeTransparentMat(mistColor);
            });
            _cache[key] = m;
            return m;
        }

        public static Material SurfaceDecal(bool battleScorch)
        {
            string key = battleScorch ? "DecalScorch" : "DecalCracksMoss";
            if (_cache.TryGetValue(key, out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_" + key, () =>
            {
                Texture2D tex = LoadPbrTex(battleScorch ? "T_DecalBattleScorch" : "T_DecalCracksMoss");
                Shader sh = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");
                var mat = new Material(sh) { name = "M_" + key, enableInstancing = true };
                mat.renderQueue = 2465;

                Color tint = battleScorch ? new Color(0.18f, 0.14f, 0.14f, 0.42f) : new Color(0.45f, 0.50f, 0.42f, 0.36f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                mat.color = tint;
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    mat.mainTexture = tex;
                }
                return mat;
            });
            _cache[key] = m;
            return m;
        }

        public static Material SoftShadowDecalMaterial()
        {
            if (_cache.TryGetValue("SoftShadowDecal", out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_SoftContactShadow", () =>
            {
                if (_softShadowTex == null)
                {
                    _softShadowTex = BakedAssetRegistry.GetOrCreateTexture("T_SoftContactShadow", () =>
                    {
                        int sz = 64;
                        var t = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
                        {
                            name = "T_SoftContactShadow",
                            wrapMode = TextureWrapMode.Clamp,
                            filterMode = FilterMode.Bilinear
                        };
                        var cols = new Color32[sz * sz];
                        for (int y = 0; y < sz; y++)
                        {
                            float ny = (y / (float)(sz - 1)) * 2f - 1f;
                            for (int x = 0; x < sz; x++)
                            {
                                float nx = (x / (float)(sz - 1)) * 2f - 1f;
                                float distSq = nx * nx + ny * ny;
                                float falloff = Mathf.Clamp01(1f - distSq * 1.15f);
                                float smooth = falloff * falloff * (3f - 2f * falloff);
                                byte a = (byte)Mathf.RoundToInt(smooth * 175f);
                                cols[y * sz + x] = new Color32(6, 5, 8, a);
                            }
                        }
                        t.SetPixels32(cols);
                        t.Apply(false, false);
                        return t;
                    });
                }

                Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Particles/Standard Unlit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");
                var mat = new Material(sh) { name = "M_SoftContactShadow", enableInstancing = true };
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                mat.color = Color.white;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", _softShadowTex);
                mat.mainTexture = _softShadowTex;
                mat.renderQueue = 2470;
                return mat;
            });
            _cache["SoftShadowDecal"] = m;
            return m;
        }

        public static Material SoftAdditiveParticleMaterial()
        {
            if (_cache.TryGetValue("SoftParticleAdd", out var m) && m != null) return m;

            m = BakedAssetRegistry.GetOrCreateMaterial("M_SoftGlowParticle", () =>
            {
                if (_softParticleTex == null)
                {
                    _softParticleTex = BakedAssetRegistry.GetOrCreateTexture("T_SoftGlowParticle", () =>
                    {
                        int sz = 64;
                        var t = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
                        {
                            name = "T_SoftGlowParticle",
                            wrapMode = TextureWrapMode.Clamp
                        };
                        var cols = new Color32[sz * sz];
                        for (int y = 0; y < sz; y++)
                        {
                            float ny = (y / (float)(sz - 1)) * 2f - 1f;
                            for (int x = 0; x < sz; x++)
                            {
                                float nx = (x / (float)(sz - 1)) * 2f - 1f;
                                float r = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                                byte a = (byte)Mathf.RoundToInt(r * r * 255f);
                                cols[y * sz + x] = new Color32(255, 255, 255, a);
                            }
                        }
                        t.SetPixels32(cols);
                        t.Apply(false, false);
                        return t;
                    });
                }

                Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Particles/Standard Unlit")
                            ?? Shader.Find("Sprites/Default");
                var mat = new Material(sh) { name = "M_SoftGlowParticle" };
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f); // Additive
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", _softParticleTex);
                mat.mainTexture = _softParticleTex;
                mat.renderQueue = 3010;
                return mat;
            });
            _cache["SoftParticleAdd"] = m;
            return m;
        }

        private static Material BuildUrpPbr(
            string matName,
            string prefix,
            Color baseColor,
            float metallic,
            float smoothness,
            float bumpScale,
            float aoStrength)
        {
            // Prefer persistent Editor-created Material in Resources if available
            Material saved = Resources.Load<Material>("Environment/Materials/" + matName);
            if (saved != null)
            {
                var inst = new Material(saved) { name = matName, enableInstancing = true };
                if (inst.HasProperty("_BaseColor")) inst.SetColor("_BaseColor", baseColor);
                inst.color = baseColor;
                if (prefix == "T_GothicStone")
                {
                    inst.mainTextureScale = new Vector2(2f, 2f);
                    if (inst.HasProperty("_BumpScale")) inst.SetFloat("_BumpScale", 1.55f);
                }
                return inst;
            }

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh) { name = matName, enableInstancing = true };

            Texture2D alb = LoadPbrTex(prefix + "_Albedo");
            Texture2D nrm = LoadPbrTex(prefix + "_Normal");
            Texture2D msk = LoadPbrTex(prefix + "_Mask");
            Texture2D ao  = LoadPbrTex(prefix + "_AO");

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            m.color = baseColor;

            if (alb != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", alb);
                m.mainTexture = alb;
            }

            if (nrm != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", nrm);
                if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }

            if (msk != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", msk);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);

            if (ao != null && m.HasProperty("_OcclusionMap"))
            {
                m.SetTexture("_OcclusionMap", ao);
                if (m.HasProperty("_OcclusionStrength")) m.SetFloat("_OcclusionStrength", aoStrength);
                m.EnableKeyword("_OCCLUSIONMAP");
            }

            return m;
        }

        public static Texture2D LoadPbrTex(string name)
        {
            if (_texCache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Environment/PBR/" + name);
            if (t != null)
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.anisoLevel = 4;
                _texCache[name] = t;
            }
            return t;
        }
    }
}

