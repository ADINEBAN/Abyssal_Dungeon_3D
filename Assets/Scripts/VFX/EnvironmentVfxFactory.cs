using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using AbyssGame;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// Complete Ancient Gothic Dark Fantasy Environment VFX Library (Phase 1 — 25 effects across 5 groups).
    /// Shares cached URP materials/textures, reuses Blender VFX meshes (SM_Vfx_*), supports Low/Medium/High scaling,
    /// distance culling, and VFXManager pooling with zero duplicate managers or gameplay interference.
    /// </summary>
    public static class EnvironmentVfxFactory
    {
        // Ancient Gothic Dark Fantasy Core Palette
        public static readonly Color AntiqueGold = new Color(0.831f, 0.686f, 0.216f, 1f);   // #D4AF37
        public static readonly Color Bronze = new Color(0.549f, 0.384f, 0.224f, 1f);        // #8C6239
        public static readonly Color DarkBrown = new Color(0.231f, 0.157f, 0.118f, 1f);     // #3B281E
        public static readonly Color ObsidianBlack = new Color(0.071f, 0.063f, 0.086f, 1f); // #121016
        public static readonly Color ArcanePurple = new Color(0.541f, 0.247f, 0.988f, 1f);  // #8A3FFC

        public static readonly VFXType[] AllPhase1Types =
        {
            // Fire (6)
            VFXType.Env_TorchFire,
            VFXType.Env_BrazierFire,
            VFXType.Env_CandleFlame,
            VFXType.Env_TrapFire,
            VFXType.Env_GroundFire,
            VFXType.Env_RisingEmbers,
            // Smoke (4)
            VFXType.Env_TorchSmoke,
            VFXType.Env_BlackFireSmoke,
            VFXType.Env_AmbientSmoke,
            VFXType.Env_HazardDenseSmoke,
            // Atmosphere (5)
            VFXType.Env_GroundFog,
            VFXType.Env_FloatingDust,
            VFXType.Env_AshParticles,
            VFXType.Env_FloatingEmbers,
            VFXType.Env_AtmosphericParticles,
            // Magic Environment (5)
            VFXType.Env_CrystalGlow,
            VFXType.Env_FloatingRunes,
            VFXType.Env_MagicalEnergy,
            VFXType.Env_PortalParticles,
            VFXType.Env_AncientAltarActivation,
            // Environmental Interaction (5)
            VFXType.Env_TrapActivation,
            VFXType.Env_FireExplosion,
            VFXType.Env_GroundDustBurst,
            VFXType.Env_StoneDebris,
            VFXType.Env_EnvironmentalImpact
        };

        private static readonly Dictionary<string, Material> _matCache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static Mesh _fallbackDebrisMesh;
        private static Mesh _fallbackConeMesh;

        public static bool IsEnvironmentVfx(VFXType type)
        {
            return type >= VFXType.Env_TorchFire && type <= VFXType.Env_EnvironmentalImpact;
        }

        public static GameObject CreateEnvironmentVfx(VFXType type)
        {
            var root = new GameObject($"VFX_{type}");
            bool isTelegraph = (type == VFXType.Env_TrapActivation);
            bool isLooping = !IsOneShotBurst(type);

            switch (type)
            {
                // 1. Fire VFX (6)
                case VFXType.Env_TorchFire: BuildTorchFire(root); break;
                case VFXType.Env_BrazierFire: BuildBrazierFire(root); break;
                case VFXType.Env_CandleFlame: BuildCandleFlame(root); break;
                case VFXType.Env_TrapFire: BuildTrapFire(root); break;
                case VFXType.Env_GroundFire: BuildGroundFire(root); break;
                case VFXType.Env_RisingEmbers: BuildRisingEmbers(root); break;

                // 2. Smoke VFX (4)
                case VFXType.Env_TorchSmoke: BuildTorchSmoke(root); break;
                case VFXType.Env_BlackFireSmoke: BuildBlackFireSmoke(root); break;
                case VFXType.Env_AmbientSmoke: BuildAmbientSmoke(root); break;
                case VFXType.Env_HazardDenseSmoke: BuildHazardDenseSmoke(root); break;

                // 3. Atmosphere VFX (5)
                case VFXType.Env_GroundFog: BuildGroundFog(root); break;
                case VFXType.Env_FloatingDust: BuildFloatingDust(root); break;
                case VFXType.Env_AshParticles: BuildAshParticles(root); break;
                case VFXType.Env_FloatingEmbers: BuildFloatingEmbers(root); break;
                case VFXType.Env_AtmosphericParticles: BuildAtmosphericParticles(root); break;

                // 4. Magic Environment VFX (5)
                case VFXType.Env_CrystalGlow: BuildCrystalGlow(root); break;
                case VFXType.Env_FloatingRunes: BuildFloatingRunes(root); break;
                case VFXType.Env_MagicalEnergy: BuildMagicalEnergy(root); break;
                case VFXType.Env_PortalParticles: BuildPortalParticles(root); break;
                case VFXType.Env_AncientAltarActivation: BuildAncientAltarActivation(root); break;

                // 5. Environmental Interaction (5)
                case VFXType.Env_TrapActivation: BuildTrapActivation(root); break;
                case VFXType.Env_FireExplosion: BuildFireExplosion(root); break;
                case VFXType.Env_GroundDustBurst: BuildGroundDustBurst(root); break;
                case VFXType.Env_StoneDebris: BuildStoneDebris(root); break;
                case VFXType.Env_EnvironmentalImpact: BuildEnvironmentalImpact(root); break;
            }

            var responder = root.AddComponent<ScalableVfxResponder>();
            responder.isCombatTelegraph = isTelegraph;

            if (isLooping)
            {
                var emitter = root.AddComponent<EnvironmentVfxEmitter>();
                emitter.minQualityTier = GetEmitterMinTier(type);
                emitter.distanceMultiplier = GetDistanceMultiplier(type);
                emitter.isCriticalHazardOrTelegraph =
                    type == VFXType.Env_TrapFire ||
                    type == VFXType.Env_TrapActivation ||
                    type == VFXType.Env_PortalParticles ||
                    type == VFXType.Env_CrystalGlow ||
                    type == VFXType.Env_GroundFire;
            }

            return root;
        }

        private static bool IsOneShotBurst(VFXType type)
        {
            return type == VFXType.Env_AncientAltarActivation ||
                   type == VFXType.Env_TrapActivation ||
                   type == VFXType.Env_FireExplosion ||
                   type == VFXType.Env_GroundDustBurst ||
                   type == VFXType.Env_StoneDebris ||
                   type == VFXType.Env_EnvironmentalImpact;
        }

        private static VfxQualityOption GetEmitterMinTier(VFXType type)
        {
            switch (type)
            {
                case VFXType.Env_AmbientSmoke:
                case VFXType.Env_HazardDenseSmoke:
                case VFXType.Env_FloatingDust:
                case VFXType.Env_AshParticles:
                    return VfxQualityOption.Medium;
                default:
                    return VfxQualityOption.Low;
            }
        }

        private static float GetDistanceMultiplier(VFXType type)
        {
            switch (type)
            {
                case VFXType.Env_PortalParticles:
                case VFXType.Env_AtmosphericParticles:
                case VFXType.Env_GroundFog:
                    return 0.95f;
                case VFXType.Env_CandleFlame:
                case VFXType.Env_FloatingDust:
                    return 0.65f;
                default:
                    return 0.85f;
            }
        }

        /// <summary>
        /// Spawns a sculpted 3-candle Gothic saucer cluster (SM_Vfx_GothicCandleCluster) plus pooled/child candle flames.
        /// Used by ModularDungeonArchitect across forgotten temples, boss steps, and corridors.
        /// </summary>
        public static GameObject SpawnCandleClusterProp(Transform parent, Vector3 localPos, Quaternion localRot, Color? flameTint = null)
        {
            var holder = new GameObject("GothicCandleCluster");
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = localRot;

            Mesh candleMesh = LoadVfxMesh("SM_Vfx_GothicCandleCluster");
            if (candleMesh != null)
            {
                var meshGo = new GameObject("CandleClusterMesh");
                meshGo.transform.SetParent(holder.transform, false);
                meshGo.AddComponent<MeshFilter>().sharedMesh = candleMesh;
                var mr = meshGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetCandleClusterPropMaterial();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }

            // Three staggered wick heights matching SM_Vfx_GothicCandleCluster geometry
            Vector3[] wickOffsets =
            {
                new Vector3(0.015f, 0.44f, 0.010f),
                new Vector3(-0.075f, 0.34f, -0.055f),
                new Vector3(0.080f, 0.25f, -0.045f)
            };

            var mgr = VFXManager.EnsureInstance();
            for (int i = 0; i < wickOffsets.Length; i++)
            {
                if (mgr != null && Application.isPlaying)
                {
                    mgr.SpawnLoopingVFX(VFXType.Env_CandleFlame, holder.transform, wickOffsets[i], Quaternion.identity, flameTint, i == 0 ? 1.0f : 0.82f);
                }
                else
                {
                    var f = CreateEnvironmentVfx(VFXType.Env_CandleFlame);
                    f.transform.SetParent(holder.transform, false);
                    f.transform.localPosition = wickOffsets[i];
                    f.transform.localScale = Vector3.one * (i == 0 ? 1.0f : 0.82f);
                }
            }

            return holder;
        }

        // ==================== 1. FIRE VFX (6) ====================

        private static void BuildTorchFire(GameObject root)
        {
            // Core flickering flame wisps (cross-textured additive fire tongue)
            var core = AddParticleChild(root.transform, "FlameWisps", loop: true, maxParticles: 16);
            var main = core.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.68f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.46f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            main.startColor = new Color(1.0f, 0.68f, 0.24f, 0.92f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = core.emission;
            em.rateOverTime = 14f;

            var sh = core.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 12f;
            sh.radius = 0.08f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            AddSizeFade(core, 0.7f, 1.15f, 0.2f);
            AddColorGradient(core, new Color(1.0f, 0.92f, 0.56f, 0.95f), AntiqueGold, Bronze, 0.0f);
            AddCurlNoise(core, 0.35f, 1.6f);
            SetRenderer(core, GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp",
                new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, radialFade: 1f, pulseSpeed: 6f),
                ParticleSystemRenderMode.Billboard);

            // Rising spark flecks (Medium+)
            var sparks = AddParticleChild(root.transform, "TorchSparks", loop: true, maxParticles: 12);
            var sm = sparks.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.1f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.7f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            sm.startColor = AntiqueGold;
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            var sem = sparks.emission;
            sem.rateOverTime = 8f;
            var ssh = sparks.shape;
            ssh.shapeType = ParticleSystemShapeType.Cone;
            ssh.angle = 22f;
            ssh.radius = 0.09f;
            ssh.rotation = new Vector3(-90f, 0f, 0f);
            AddCurlNoise(sparks, 0.45f, 2.2f);
            AddColorGradient(sparks, new Color(1f, 0.88f, 0.48f, 1f), Bronze, DarkBrown, 0f);
            var sRend = SetRenderer(sparks, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            sRend.velocityScale = 0.12f;
            sparks.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        private static void BuildBrazierFire(GameObject root)
        {
            // Broad hearth glow core
            var hearth = AddParticleChild(root.transform, "BrazierFlames", loop: true, maxParticles: 24);
            var main = hearth.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.90f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.92f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
            main.startColor = new Color(1.0f, 0.58f, 0.18f, 0.92f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = hearth.emission;
            em.rateOverTime = 18f;
            var sh = hearth.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 16f;
            sh.radius = 0.34f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            AddSizeFade(hearth, 0.75f, 1.25f, 0.25f);
            AddColorGradient(hearth, new Color(1.0f, 0.90f, 0.52f, 0.95f), new Color(0.95f, 0.42f, 0.12f, 0.85f), DarkBrown, 0f);
            AddCurlNoise(hearth, 0.52f, 1.5f);
            SetRenderer(hearth, GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp",
                new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, radialFade: 1f, pulseSpeed: 6f),
                ParticleSystemRenderMode.Billboard);

            // Swirling forge sparks (Medium+)
            var embers = AddParticleChild(root.transform, "BrazierEmbers", loop: true, maxParticles: 20);
            var emMain = embers.main;
            emMain.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            emMain.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
            emMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
            emMain.startColor = AntiqueGold;
            emMain.simulationSpace = ParticleSystemSimulationSpace.World;
            embers.emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
            var eEm = embers.emission;
            eEm.rateOverTime = 11f;
            var eSh = embers.shape;
            eSh.shapeType = ParticleSystemShapeType.Cone;
            eSh.angle = 26f;
            eSh.radius = 0.32f;
            eSh.rotation = new Vector3(-90f, 0f, 0f);
            AddCurlNoise(embers, 0.75f, 1.9f);
            AddColorGradient(embers, AntiqueGold, Bronze, DarkBrown, 0f);
            var eRend = SetRenderer(embers, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            eRend.velocityScale = 0.14f;
            embers.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        private static void BuildCandleFlame(GameObject root)
        {
            var flame = AddParticleChild(root.transform, "CandleWickFlame", loop: true, maxParticles: 6);
            var main = flame.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.15f);
            main.startColor = new Color(1.0f, 0.84f, 0.42f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var em = flame.emission;
            em.rateOverTime = 8f;
            var sh = flame.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.01f;

            AddSizeFade(flame, 0.85f, 1.08f, 0.4f);
            AddColorGradient(flame, new Color(1.0f, 0.96f, 0.76f, 0.98f), AntiqueGold, Bronze, 0f);
            AddCurlNoise(flame, 0.06f, 3.2f);
            SetRenderer(flame, GetAdditiveMat("M_Env_CandleFlame", "T_Vfx_FlameWisp",
                new Color(1.65f, 1.35f, 0.75f, 1f), new Color(1.20f, 0.65f, 0.18f, 1f), 1.0f, Vector2.zero, radialFade: 1f, pulseSpeed: 8f),
                ParticleSystemRenderMode.Billboard);

            var halo = AddParticleChild(root.transform, "CandleWaxHalo", loop: true, maxParticles: 3);
            var hm = halo.main;
            hm.startLifetime = 0.55f;
            hm.startSpeed = 0.0f;
            hm.startSize = 0.24f;
            hm.startColor = new Color(0.88f, 0.65f, 0.22f, 0.28f);
            hm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var hem = halo.emission;
            hem.rateOverTime = 3f;
            SetRenderer(halo, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            halo.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.High;
        }

        private static void BuildTrapFire(GameObject root)
        {
            var jet = AddParticleChild(root.transform, "TrapFireJets", loop: true, maxParticles: 28);
            var main = jet.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.78f);
            main.startColor = new Color(1.0f, 0.48f, 0.12f, 0.96f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = jet.emission;
            em.rateOverTime = 36f;
            var sh = jet.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 14f;
            sh.radius = 0.55f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            AddSizeFade(jet, 0.7f, 1.3f, 0.25f);
            AddColorGradient(jet, new Color(1.0f, 0.92f, 0.48f, 1f), new Color(0.98f, 0.36f, 0.08f, 0.9f), DarkBrown, 0f);
            AddCurlNoise(jet, 0.6f, 2.5f);
            SetRenderer(jet, GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp",
                new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, radialFade: 1f, pulseSpeed: 6f),
                ParticleSystemRenderMode.Billboard);

            var floorRing = AddParticleChild(root.transform, "TrapGrateHeatRing", loop: true, maxParticles: 4);
            var fm = floorRing.main;
            fm.startLifetime = 0.6f;
            fm.startSpeed = 0f;
            fm.startSize = 2.4f;
            fm.startColor = new Color(1.0f, 0.42f, 0.12f, 0.65f);
            var fem = floorRing.emission;
            fem.rateOverTime = 4f;
            var fRend = SetRenderer(floorRing, GetAdditiveMat("M_Env_ShockRingGold", "T_Vfx_ShockRing",
                new Color(1.55f, 0.85f, 0.28f, 1f), new Color(1.1f, 0.22f, 0.06f, 1f), 1.0f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            fRend.renderMode = ParticleSystemRenderMode.Billboard;
            fRend.alignment = ParticleSystemRenderSpace.Local;
            floorRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floorRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        }

        private static void BuildGroundFire(GameObject root)
        {
            var carpet = AddParticleChild(root.transform, "GroundEmberCarpet", loop: true, maxParticles: 10);
            var cm = carpet.main;
            cm.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            cm.startSpeed = 0.05f;
            cm.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.1f);
            cm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.startColor = new Color(0.85f, 0.34f, 0.12f, 0.55f);
            cm.simulationSpace = ParticleSystemSimulationSpace.World;
            var cem = carpet.emission;
            cem.rateOverTime = 4.5f;
            var csh = carpet.shape;
            csh.shapeType = ParticleSystemShapeType.Circle;
            csh.radius = 0.85f;
            csh.rotation = new Vector3(90f, 0f, 0f);
            AddColorGradient(carpet, new Color(1f, 0.55f, 0.16f, 0.0f), new Color(0.95f, 0.36f, 0.10f, 0.58f), DarkBrown, 0f);
            var cRend = SetRenderer(carpet, GetAdditiveMat("M_Env_GroundFireBed", "T_Vfx_GroundFogWisp",
                new Color(1.45f, 0.58f, 0.14f, 1f), new Color(0.85f, 0.18f, 0.08f, 1f), 0.9f, new Vector2(0.05f, 0.03f), radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            cRend.alignment = ParticleSystemRenderSpace.Local;
            carpet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            carpet.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            var licks = AddParticleChild(root.transform, "GroundFireLicks", loop: true, maxParticles: 18);
            var lm = licks.main;
            lm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.80f);
            lm.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.05f);
            lm.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
            lm.startColor = new Color(1.0f, 0.55f, 0.15f, 0.88f);
            lm.simulationSpace = ParticleSystemSimulationSpace.World;
            var lem = licks.emission;
            lem.rateOverTime = 14f;
            var lsh = licks.shape;
            lsh.shapeType = ParticleSystemShapeType.Circle;
            lsh.radius = 0.95f;
            lsh.rotation = new Vector3(-90f, 0f, 0f);
            AddSizeFade(licks, 0.7f, 1.1f, 0.15f);
            AddColorGradient(licks, AntiqueGold, new Color(0.95f, 0.34f, 0.10f, 0.88f), DarkBrown, 0f);
            AddCurlNoise(licks, 0.45f, 1.8f);
            SetRenderer(licks, GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp",
                new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, radialFade: 1f, pulseSpeed: 6f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildRisingEmbers(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "RisingEmbers", loop: true, maxParticles: 28);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startColor = AntiqueGold;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 12f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 20f;
            sh.radius = 0.45f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            AddCurlNoise(ps, 0.85f, 1.25f);
            AddColorGradient(ps, new Color(1.0f, 0.85f, 0.42f, 0.95f), Bronze, DarkBrown, 0f);
            var rend = SetRenderer(ps, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            rend.velocityScale = 0.15f;
        }

        // ==================== 2. SMOKE VFX (4) ====================

        private static void BuildTorchSmoke(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "TorchSmokePlume", loop: true, maxParticles: 16);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.28f, 0.22f, 0.19f, 0.42f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 6.5f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 10f;
            sh.radius = 0.08f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);

            AddSizeFade(ps, 0.65f, 1.45f, 2.2f);
            AddColorGradient(ps, new Color(0.32f, 0.24f, 0.19f, 0.0f), new Color(0.22f, 0.18f, 0.16f, 0.42f), ObsidianBlack, 0f);
            AddCurlNoise(ps, 0.28f, 0.85f);
            SetRenderer(ps, GetParticleMat("M_Env_TorchSmoke", "T_Vfx_GothicSmoke",
                new Color(0.38f, 0.30f, 0.25f, 0.58f), ObsidianBlack, 0.85f, Vector2.zero, distortion: 0.03f, edgeSoftness: 0.48f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildBlackFireSmoke(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "BlackFireSmoke", loop: true, maxParticles: 24);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.85f, 1.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.65f, 1.15f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.14f, 0.11f, 0.12f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 8f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 16f;
            sh.radius = 0.32f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);

            AddSizeFade(ps, 0.65f, 1.4f, 2.3f);
            AddColorGradient(ps, new Color(0.24f, 0.16f, 0.12f, 0.0f), new Color(0.10f, 0.08f, 0.10f, 0.68f), ObsidianBlack, 0f);
            AddCurlNoise(ps, 0.45f, 0.9f);
            SetRenderer(ps, GetParticleMat("M_Env_BlackSmoke", "T_Vfx_GothicSmoke",
                new Color(0.20f, 0.15f, 0.15f, 0.78f), new Color(0.05f, 0.04f, 0.06f, 0.95f), 0.75f, Vector2.zero, distortion: 0.04f, edgeSoftness: 0.45f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAmbientSmoke(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "AmbientSmoke", loop: true, maxParticles: 14);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 6.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.32f, 0.28f, 0.36f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 2.4f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(4.5f, 0.8f, 4.5f);

            AddSizeFade(ps, 0.8f, 1.15f, 1.35f);
            AddColorGradient(ps, new Color(0.35f, 0.30f, 0.38f, 0f), new Color(0.28f, 0.25f, 0.34f, 0.24f), ObsidianBlack, 0f);
            AddCurlNoise(ps, 0.22f, 0.35f);
            SetRenderer(ps, GetParticleMat("M_Env_AmbientSmoke", "T_Vfx_GroundFogWisp",
                new Color(0.45f, 0.40f, 0.50f, 0.38f), ObsidianBlack, 0.9f, new Vector2(0.02f, 0.01f), distortion: 0.03f, edgeSoftness: 0.50f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildHazardDenseSmoke(GameObject root)
        {
            var dense = AddParticleChild(root.transform, "HazardDenseSmoke", loop: true, maxParticles: 22);
            var main = dense.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.58f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 1.85f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.18f, 0.14f, 0.18f, 0.52f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = dense.emission;
            em.rateOverTime = 6.5f;
            var sh = dense.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 1.4f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var rot = dense.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

            AddSizeFade(dense, 0.7f, 1.3f, 1.85f);
            AddColorGradient(dense, new Color(0.24f, 0.17f, 0.16f, 0f), new Color(0.14f, 0.11f, 0.14f, 0.56f), ObsidianBlack, 0f);
            AddCurlNoise(dense, 0.36f, 0.65f);
            SetRenderer(dense, GetParticleMat("M_Env_BlackSmoke", "T_Vfx_GothicSmoke",
                new Color(0.20f, 0.15f, 0.15f, 0.78f), new Color(0.05f, 0.04f, 0.06f, 0.95f), 0.75f, Vector2.zero, distortion: 0.04f, edgeSoftness: 0.45f),
                ParticleSystemRenderMode.Billboard);

            var underEmber = AddParticleChild(root.transform, "HazardSmolderGlow", loop: true, maxParticles: 10);
            var um = underEmber.main;
            um.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            um.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            um.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            um.startColor = Bronze;
            um.simulationSpace = ParticleSystemSimulationSpace.World;
            var uem = underEmber.emission;
            uem.rateOverTime = 5f;
            var ush = underEmber.shape;
            ush.shapeType = ParticleSystemShapeType.Circle;
            ush.radius = 1.2f;
            ush.rotation = new Vector3(-90f, 0f, 0f);
            AddColorGradient(underEmber, AntiqueGold, Bronze, DarkBrown, 0f);
            SetRenderer(underEmber, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            underEmber.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.High;
        }

        // ==================== 3. ATMOSPHERE VFX (5) ====================

        private static void BuildGroundFog(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "GroundFogWisps", loop: true, maxParticles: 16);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.6f, 4.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.42f, 0.38f, 0.54f, 0.24f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 2.6f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(8.5f, 0.25f, 8.5f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

            AddSizeFade(ps, 0.85f, 1.15f, 1.35f);
            AddColorGradient(ps, new Color(0.38f, 0.36f, 0.52f, 0f), new Color(0.32f, 0.30f, 0.46f, 0.26f), ObsidianBlack, 0f);
            AddCurlNoise(ps, 0.18f, 0.25f);
            var rend = SetRenderer(ps, GetParticleMat("M_Env_GroundFog", "T_Vfx_GroundFogWisp",
                new Color(0.46f, 0.42f, 0.58f, 0.42f), new Color(0.12f, 0.10f, 0.18f, 0.7f), 0.92f, new Vector2(0.03f, 0.015f), distortion: 0.03f, edgeSoftness: 0.52f),
                ParticleSystemRenderMode.Billboard);
            rend.alignment = ParticleSystemRenderSpace.Local;
            ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ps.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        }

        private static void BuildFloatingDust(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "FloatingDustMotes", loop: true, maxParticles: 40);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.0f, 7.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(0.88f, 0.78f, 0.56f, 0.48f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 7.5f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(10f, 3.8f, 10f);

            AddColorGradient(ps, new Color(AntiqueGold.r, AntiqueGold.g, AntiqueGold.b, 0f), new Color(0.90f, 0.80f, 0.58f, 0.48f), Bronze, 0f);
            AddCurlNoise(ps, 0.20f, 0.45f);
            SetRenderer(ps, GetParticleMat("M_Env_DustMotes", "T_Vfx_EmberSpark",
                new Color(0.95f, 0.86f, 0.65f, 0.65f), Bronze, 1.0f, Vector2.zero, distortion: 0f, edgeSoftness: 0.45f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAshParticles(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "AshFlakes", loop: true, maxParticles: 34);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.025f;
            main.startColor = new Color(0.62f, 0.54f, 0.48f, 0.68f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 6.5f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(11f, 4.2f, 11f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-1.4f, 1.4f);

            AddColorGradient(ps, new Color(0.68f, 0.58f, 0.50f, 0f), new Color(0.45f, 0.38f, 0.35f, 0.68f), ObsidianBlack, 0f);
            AddCurlNoise(ps, 0.32f, 0.65f);
            SetRenderer(ps, GetParticleMat("M_Env_AshFlakes", "T_Vfx_AshFlake",
                new Color(0.68f, 0.60f, 0.55f, 0.78f), DarkBrown, 0.9f, Vector2.zero, distortion: 0f, edgeSoftness: 0.30f, radialFade: 0f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildFloatingEmbers(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "FloatingEmbers", loop: true, maxParticles: 32);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startColor = new Color(1.0f, 0.68f, 0.22f, 0.90f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 7.5f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(11f, 3.2f, 11f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.20f, 0.52f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            AddCurlNoise(ps, 0.42f, 0.95f);
            AddColorGradient(ps, new Color(1f, 0.86f, 0.42f, 0f), AntiqueGold, Bronze, 0f);
            SetRenderer(ps, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAtmosphericParticles(GameObject root)
        {
            // Base layer: biome-tinted floating motes
            var motes = AddParticleChild(root.transform, "AtmoMotes", loop: true, maxParticles: 55);
            var main = motes.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.8f, 6.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
            main.startColor = new Color(0.88f, 0.82f, 0.68f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = motes.emission;
            em.rateOverTime = 11f;
            var sh = motes.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(22f, 6f, 22f);

            var vel = motes.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.08f, 0.34f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            AddCurlNoise(motes, 0.28f, 0.55f);
            AddColorGradient(motes, new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.80f), new Color(1f, 1f, 1f, 0.72f), 0f);
            SetRenderer(motes, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.05f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);

            // Secondary layer: drifting ash flakes (Medium+)
            var ash = AddParticleChild(root.transform, "AtmoAshFlakes", loop: true, maxParticles: 24);
            var am = ash.main;
            am.startLifetime = new ParticleSystem.MinMaxCurve(4.0f, 6.5f);
            am.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            am.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            am.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            am.startColor = new Color(0.58f, 0.52f, 0.48f, 0.42f);
            am.simulationSpace = ParticleSystemSimulationSpace.World;
            var aem = ash.emission;
            aem.rateOverTime = 4.5f;
            var ashShape = ash.shape;
            ashShape.shapeType = ParticleSystemShapeType.Box;
            ashShape.scale = new Vector3(20f, 5.5f, 20f);
            AddCurlNoise(ash, 0.24f, 0.45f);
            AddColorGradient(ash, new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.65f), new Color(1f, 1f, 1f, 0.35f), 0f);
            SetRenderer(ash, GetParticleMat("M_Env_AshFlakes", "T_Vfx_AshFlake",
                new Color(0.68f, 0.60f, 0.55f, 0.78f), DarkBrown, 0.9f, Vector2.zero, distortion: 0f, edgeSoftness: 0.30f, radialFade: 0f),
                ParticleSystemRenderMode.Billboard);
            ash.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        // ==================== 4. MAGIC ENVIRONMENT VFX (5) ====================

        private static void BuildCrystalGlow(GameObject root)
        {
            var aura = AddParticleChild(root.transform, "CrystalAuraFlare", loop: true, maxParticles: 6);
            var am = aura.main;
            am.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            am.startSpeed = 0.0f;
            am.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
            am.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
            am.startColor = new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0.38f);
            var aem = aura.emission;
            aem.rateOverTime = 2.4f;
            var arot = aura.rotationOverLifetime;
            arot.enabled = true;
            arot.z = 0.25f;
            AddSizeFade(aura, 0.78f, 1.08f, 0.85f);
            AddColorGradient(aura, new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0f), new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0.48f), AntiqueGold, 0f);
            SetRenderer(aura, GetAdditiveMat("M_Env_CrystalFlare", "T_Vfx_StarFlare",
                new Color(1.45f, 0.95f, 1.85f, 1f), new Color(0.55f, 0.25f, 1.15f, 1f), 1.0f, Vector2.zero, radialFade: 1f, pulseSpeed: 2.5f),
                ParticleSystemRenderMode.Billboard);

            var motes = AddParticleChild(root.transform, "CrystalPrismMotes", loop: true, maxParticles: 20);
            var mm = motes.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            mm.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 0.65f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            mm.startColor = new Color(0.75f, 0.58f, 1.0f, 0.85f);
            mm.simulationSpace = ParticleSystemSimulationSpace.World;
            var mem = motes.emission;
            mem.rateOverTime = 9f;
            var msh = motes.shape;
            msh.shapeType = ParticleSystemShapeType.Sphere;
            msh.radius = 0.85f;
            var mvel = motes.velocityOverLifetime;
            mvel.enabled = true;
            mvel.space = ParticleSystemSimulationSpace.World;
            mvel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            mvel.y = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            mvel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            AddCurlNoise(motes, 0.35f, 1.1f);
            AddColorGradient(motes, new Color(1f, 0.9f, 1f, 0f), ArcanePurple, AntiqueGold, 0f);
            SetRenderer(motes, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            motes.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        private static void BuildFloatingRunes(GameObject root)
        {
            var ps = AddParticleChild(root.transform, "FloatingRuneGlyphs", loop: true, maxParticles: 12);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.46f);
            main.startColor = AntiqueGold;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 3.8f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.95f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.radial = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 2;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.99f);
            sheet.cycleCount = 1;

            AddSizeFade(ps, 0.65f, 1.05f, 0.55f);
            AddColorGradient(ps, new Color(AntiqueGold.r, AntiqueGold.g, AntiqueGold.b, 0f), AntiqueGold, ArcanePurple, 0f);
            SetRenderer(ps, GetAdditiveMat("M_Env_RunesSheet", "T_Vfx_RunesSheet",
                new Color(1.65f, 1.38f, 0.58f, 1f), new Color(0.62f, 0.28f, 1.35f, 1f), 1.0f, Vector2.zero, radialFade: 0f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildMagicalEnergy(GameObject root)
        {
            var streams = AddParticleChild(root.transform, "ArcaneEnergyStreams", loop: true, maxParticles: 24);
            var main = streams.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.60f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.startColor = ArcanePurple;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var em = streams.emission;
            em.rateOverTime = 12f;
            var sh = streams.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.85f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var vel = streams.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.radial = new ParticleSystem.MinMaxCurve(-0.35f, -0.35f);

            AddCurlNoise(streams, 0.25f, 1.4f);
            AddColorGradient(streams, new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0f), ArcanePurple, AntiqueGold, 0f);
            var rend = SetRenderer(streams, GetAdditiveMat("M_Env_EnergyTrail", "T_Vfx_EnergyTrail",
                new Color(1.35f, 0.95f, 1.85f, 1f), new Color(0.75f, 0.28f, 1.45f, 1f), 1.05f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            rend.velocityScale = 0.16f;
        }

        private static void BuildPortalParticles(GameObject root)
        {
            // 1. 3D Blender Vortex Spiral Funnel Mesh Particle (UV-scrolling Arcane Purple + Antique Gold)
            Mesh vortexMesh = LoadVfxMesh("SM_Vfx_VortexSpiral");
            var vortex = AddParticleChild(root.transform, "PortalVortexMesh", loop: true, maxParticles: 3);
            var vm = vortex.main;
            vm.startLifetime = 2.6f;
            vm.startSpeed = 0f;
            vm.startSize = 1.05f;
            vm.startColor = new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0.62f);
            vm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var vem = vortex.emission;
            vem.rateOverTime = 1.2f;
            var vrot = vortex.rotationOverLifetime;
            vrot.enabled = true;
            vrot.y = 0.95f;
            AddColorGradient(vortex, new Color(ArcanePurple.r, ArcanePurple.g, ArcanePurple.b, 0f), new Color(0.65f, 0.30f, 1.0f, 0.65f), AntiqueGold, 0f);
            var vMat = GetAdditiveMat("M_Env_PortalVortex", "T_Vfx_EnergyTrail",
                new Color(1.55f, 1.15f, 0.65f, 1f), new Color(0.75f, 0.28f, 1.55f, 1f), 0.95f, new Vector2(0.35f, -0.75f), radialFade: 0f, fadeEnds: 1f);
            var vRend = SetRenderer(vortex, vMat, vortexMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (vortexMesh != null)
            {
                vRend.mesh = vortexMesh;
                vRend.alignment = ParticleSystemRenderSpace.Local;
            }
            vortex.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            // 2. Swirling Portal Motes & Floating Runes (Medium+)
            var motes = AddParticleChild(root.transform, "PortalSwirlMotes", loop: true, maxParticles: 28);
            var mm = motes.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.3f);
            mm.startSpeed = 0.2f;
            mm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            mm.startColor = ArcanePurple;
            mm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var mem = motes.emission;
            mem.rateOverTime = 14f;
            var msh = motes.shape;
            msh.shapeType = ParticleSystemShapeType.Circle;
            msh.radius = 1.35f;
            msh.rotation = new Vector3(-90f, 0f, 0f);
            var mvel = motes.velocityOverLifetime;
            mvel.enabled = true;
            mvel.space = ParticleSystemSimulationSpace.Local;
            mvel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            mvel.y = new ParticleSystem.MinMaxCurve(0.65f, 1.25f);
            mvel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            mvel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            mvel.orbitalY = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            mvel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
            mvel.radial = new ParticleSystem.MinMaxCurve(-0.45f, -0.45f);
            AddColorGradient(motes, new Color(AntiqueGold.r, AntiqueGold.g, AntiqueGold.b, 0f), ArcanePurple, AntiqueGold, 0f);
            SetRenderer(motes, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            motes.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        private static void BuildAncientAltarActivation(GameObject root)
        {
            // 1. Vertical Sanctuary Pillar Light Beam
            var pillar = AddParticleChild(root.transform, "AltarLightPillar", loop: false, maxParticles: 2, duration: 1.3f);
            var pm = pillar.main;
            pm.startLifetime = 1.15f;
            pm.startSpeed = 0.4f;
            pm.startSize3D = true;
            pm.startSizeX = 2.0f;
            pm.startSizeY = 5.5f;
            pm.startSizeZ = 2.0f;
            pm.startColor = AntiqueGold;
            SetSingleBurst(pillar, 0f, 1);
            AddColorGradient(pillar, new Color(1f, 0.92f, 0.56f, 0.9f), AntiqueGold, ArcanePurple, 0f);
            var pRend = SetRenderer(pillar, GetAdditiveMat("M_Env_PillarBeam", "T_Vfx_PillarBeam",
                new Color(1.85f, 1.45f, 0.62f, 1f), new Color(0.68f, 0.32f, 1.45f, 1f), 1.05f, Vector2.zero, radialFade: 0f, fadeEnds: 1f),
                ParticleSystemRenderMode.Billboard);
            pRend.alignment = ParticleSystemRenderSpace.Facing;
            pillar.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            // 3. Upward Fountain of Ancient Runes + Sparks (Medium+)
            var runes = AddParticleChild(root.transform, "AltarRunesBurst", loop: false, maxParticles: 16, duration: 1.8f);
            var rum = runes.main;
            rum.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
            rum.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            rum.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.44f);
            rum.startColor = AntiqueGold;
            rum.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(runes, 0f, 10);
            var rush = runes.shape;
            rush.shapeType = ParticleSystemShapeType.Cone;
            rush.angle = 26f;
            rush.radius = 0.85f;
            rush.rotation = new Vector3(-90f, 0f, 0f);
            var sheet = runes.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 2;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.99f);
            AddColorGradient(runes, AntiqueGold, ArcanePurple, Bronze, 0f);
            SetRenderer(runes, GetAdditiveMat("M_Env_RunesSheet", "T_Vfx_RunesSheet",
                new Color(1.65f, 1.38f, 0.58f, 1f), new Color(0.62f, 0.28f, 1.35f, 1f), 1.0f, Vector2.zero, radialFade: 0f),
                ParticleSystemRenderMode.Billboard);
            runes.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        // ==================== 5. ENVIRONMENTAL INTERACTION (5) ====================

        private static void BuildTrapActivation(GameObject root)
        {
            // Critical Telegraph — always stays clearly visible on Low!
            var ring = AddParticleChild(root.transform, "TrapTelegraphRing", loop: false, maxParticles: 2, duration: 0.75f);
            var rm = ring.main;
            rm.startLifetime = 0.65f;
            rm.startSpeed = 0f;
            rm.startSize = 3.1f;
            rm.startColor = new Color(1.0f, 0.52f, 0.14f, 0.92f);
            SetSingleBurst(ring, 0f, 1);
            AddSizeFade(ring, 0.55f, 0.95f, 1.05f);
            AddColorGradient(ring, new Color(1f, 0.72f, 0.22f, 0.95f), new Color(1f, 0.35f, 0.10f, 0.85f), DarkBrown, 0f);
            var rRend = SetRenderer(ring, GetAdditiveMat("M_Env_ShockRingGold", "T_Vfx_ShockRing",
                new Color(1.65f, 1.05f, 0.32f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.1f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            rRend.alignment = ParticleSystemRenderSpace.Local;
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            var rattle = AddParticleChild(root.transform, "TrapWarningSparks", loop: false, maxParticles: 16, duration: 0.70f);
            var wm = rattle.main;
            wm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            wm.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
            wm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            wm.startColor = AntiqueGold;
            SetSingleBurst(rattle, 0f, 12);
            var wsh = rattle.shape;
            wsh.shapeType = ParticleSystemShapeType.Cone;
            wsh.angle = 32f;
            wsh.radius = 0.75f;
            wsh.rotation = new Vector3(-90f, 0f, 0f);
            AddColorGradient(rattle, new Color(1f, 0.88f, 0.42f, 1f), Bronze, DarkBrown, 0f);
            var wr = SetRenderer(rattle, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            wr.velocityScale = 0.12f;
        }

        private static void BuildFireExplosion(GameObject root)
        {
            // 1. Bright core flash
            var flash = AddParticleChild(root.transform, "ExplosionCoreFlash", loop: false, maxParticles: 2, duration: 0.35f);
            var fm = flash.main;
            fm.startLifetime = 0.26f;
            fm.startSpeed = 0f;
            fm.startSize = 3.2f;
            fm.startColor = new Color(1.0f, 0.86f, 0.48f, 0.95f);
            SetSingleBurst(flash, 0f, 1);
            AddSizeFade(flash, 0.5f, 1.2f, 0.3f);
            AddColorGradient(flash, new Color(1f, 0.95f, 0.75f, 1f), new Color(1f, 0.48f, 0.12f, 0.75f), DarkBrown, 0f);
            SetRenderer(flash, GetAdditiveMat("M_Env_CrystalFlare", "T_Vfx_StarFlare",
                new Color(1.85f, 1.25f, 0.55f, 1f), new Color(1.35f, 0.38f, 0.08f, 1f), 1.1f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);

            // 2. Fireball wisps
            var fire = AddParticleChild(root.transform, "ExplosionFireball", loop: false, maxParticles: 24, duration: 1.1f);
            var fbm = fire.main;
            fbm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            fbm.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
            fbm.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.45f);
            fbm.startRotation = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            fbm.startColor = new Color(1.0f, 0.56f, 0.14f, 0.96f);
            fbm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(fire, 0f, 16);
            var fsh = fire.shape;
            fsh.shapeType = ParticleSystemShapeType.Sphere;
            fsh.radius = 0.45f;
            AddSizeFade(fire, 0.65f, 1.25f, 0.45f);
            AddColorGradient(fire, new Color(1f, 0.90f, 0.48f, 1f), new Color(0.95f, 0.36f, 0.08f, 0.9f), DarkBrown, 0f);
            SetRenderer(fire, GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp",
                new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, radialFade: 1f, pulseSpeed: 6f),
                ParticleSystemRenderMode.Billboard);

            // 3. Blender 3D ShockCone Wave (Medium+)
            Mesh shockMesh = LoadVfxMesh("SM_Vfx_ShockCone") ?? GetFallbackShockConeMesh();
            var cone = AddParticleChild(root.transform, "ExplosionShockCone", loop: false, maxParticles: 2, duration: 0.6f);
            var cm = cone.main;
            cm.startLifetime = 0.45f;
            cm.startSpeed = 0f;
            cm.startSize = 2.6f;
            cm.startColor = new Color(1.0f, 0.55f, 0.16f, 0.85f);
            SetSingleBurst(cone, 0f, 1);
            AddSizeFade(cone, 0.4f, 1.0f, 1.35f);
            AddColorGradient(cone, AntiqueGold, new Color(0.95f, 0.32f, 0.08f, 0.72f), DarkBrown, 0f);
            var cRend = SetRenderer(cone, GetAdditiveMat("M_Env_ShockConeFire", "T_Vfx_NovaWave",
                new Color(1.65f, 0.95f, 0.35f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.0f, new Vector2(0f, -1.2f), radialFade: 0f, fadeEnds: 1f),
                ParticleSystemRenderMode.Mesh);
            cRend.mesh = shockMesh;
            cRend.alignment = ParticleSystemRenderSpace.Local;
            cone.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;

            // 4. Rising obsidian soot smoke
            var smoke = AddParticleChild(root.transform, "ExplosionScorchSmoke", loop: false, maxParticles: 16, duration: 1.8f);
            var sm = smoke.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
            sm.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.7f);
            sm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            sm.startColor = new Color(0.16f, 0.12f, 0.12f, 0.65f);
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(smoke, 0.06f, 10);
            var ssh = smoke.shape;
            ssh.shapeType = ParticleSystemShapeType.Cone;
            ssh.angle = 35f;
            ssh.radius = 0.55f;
            ssh.rotation = new Vector3(-90f, 0f, 0f);
            AddSizeFade(smoke, 0.7f, 1.35f, 1.8f);
            AddColorGradient(smoke, new Color(0.24f, 0.15f, 0.11f, 0.1f), new Color(0.10f, 0.08f, 0.10f, 0.65f), ObsidianBlack, 0f);
            SetRenderer(smoke, GetParticleMat("M_Env_BlackSmoke", "T_Vfx_GothicSmoke",
                new Color(0.20f, 0.15f, 0.15f, 0.78f), new Color(0.05f, 0.04f, 0.06f, 0.95f), 0.75f, Vector2.zero, distortion: 0.04f, edgeSoftness: 0.45f),
                ParticleSystemRenderMode.Billboard);

            // 5. Radial sparks spray (High)
            var sparks = AddParticleChild(root.transform, "ExplosionSparks", loop: false, maxParticles: 24, duration: 1.0f);
            var spm = sparks.main;
            spm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.90f);
            spm.startSpeed = new ParticleSystem.MinMaxCurve(4.0f, 8.0f);
            spm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            spm.gravityModifier = 1.2f;
            spm.startColor = AntiqueGold;
            spm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(sparks, 0f, 20);
            var spsh = sparks.shape;
            spsh.shapeType = ParticleSystemShapeType.Hemisphere;
            spsh.radius = 0.35f;
            AddColorGradient(sparks, new Color(1f, 0.90f, 0.48f, 1f), Bronze, DarkBrown, 0f);
            var spr = SetRenderer(sparks, GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Stretch);
            spr.velocityScale = 0.14f;
            var filter = sparks.gameObject.AddComponent<VfxLayerTierFilter>();
            filter.minQualityTier = VfxQualityOption.High;
            filter.isSecondarySparkLayer = true;
        }

        private static void BuildGroundDustBurst(GameObject root)
        {
            var ring = AddParticleChild(root.transform, "DustRadialSpread", loop: false, maxParticles: 22, duration: 1.4f);
            var rm = ring.main;
            rm.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
            rm.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            rm.startSize = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
            rm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            rm.startColor = new Color(0.54f, 0.44f, 0.36f, 0.56f);
            rm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(ring, 0f, 16);
            var rsh = ring.shape;
            rsh.shapeType = ParticleSystemShapeType.Circle;
            rsh.radius = 0.5f;
            rsh.rotation = new Vector3(-90f, 0f, 0f);

            var drag = ring.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.dampen = 0.18f;
            drag.limit = 0.4f;

            var rot = ring.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            AddSizeFade(ring, 0.65f, 1.25f, 1.75f);
            AddColorGradient(ring, new Color(0.58f, 0.46f, 0.36f, 0.55f), Bronze, DarkBrown, 0f);
            SetRenderer(ring, GetParticleMat("M_Env_DustBurst", "T_Vfx_GothicSmoke",
                new Color(0.56f, 0.45f, 0.36f, 0.68f), DarkBrown, 0.90f, Vector2.zero, distortion: 0.02f, edgeSoftness: 0.46f),
                ParticleSystemRenderMode.Billboard);
            ring.transform.localPosition = new Vector3(0f, 0.14f, 0f);
        }

        private static void BuildStoneDebris(GameObject root)
        {
            Mesh stoneMesh = LoadVfxMesh("SM_Vfx_StoneDebris") ?? GetFallbackDebrisMesh();
            var chunks = AddParticleChild(root.transform, "StoneDebrisChunks", loop: false, maxParticles: 18, duration: 1.8f);
            var cm = chunks.main;
            cm.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.75f);
            cm.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.8f);
            cm.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
            cm.startRotation3D = true;
            cm.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.gravityModifier = 2.4f;
            cm.startColor = new Color(0.45f, 0.39f, 0.35f, 1f);
            cm.simulationSpace = ParticleSystemSimulationSpace.World;

            SetSingleBurst(chunks, 0f, 14);
            var csh = chunks.shape;
            csh.shapeType = ParticleSystemShapeType.Cone;
            csh.angle = 42f;
            csh.radius = 0.42f;
            csh.rotation = new Vector3(-90f, 0f, 0f);

            var crot = chunks.rotationOverLifetime;
            crot.enabled = true;
            crot.separateAxes = true;
            crot.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
            crot.y = new ParticleSystem.MinMaxCurve(-5f, 5f);
            crot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            AddSizeFade(chunks, 1.0f, 1.0f, 0.08f);

            // Real floor bounce on High/Ultra (automatically stripped on Low/Medium by ScalableVfxResponder!)
            var col = chunks.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.bounce = 0.38f;
            col.dampen = 0.35f;

            var cRend = SetRenderer(chunks, GetStoneDebrisMaterial(), ParticleSystemRenderMode.Mesh);
            cRend.mesh = stoneMesh;
            cRend.alignment = ParticleSystemRenderSpace.Local;

            // Puff of stone dust accompanying shattered fragments
            var dust = AddParticleChild(root.transform, "StoneShatterDust", loop: false, maxParticles: 10, duration: 1.2f);
            var dm = dust.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);
            dm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            dm.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            dm.startColor = new Color(0.48f, 0.42f, 0.36f, 0.48f);
            SetSingleBurst(dust, 0f, 7);
            AddSizeFade(dust, 0.7f, 1.2f, 1.6f);
            AddColorGradient(dust, new Color(0.55f, 0.46f, 0.38f, 0.48f), DarkBrown, ObsidianBlack, 0f);
            SetRenderer(dust, GetParticleMat("M_Env_DustBurst", "T_Vfx_GothicSmoke",
                new Color(0.56f, 0.45f, 0.36f, 0.68f), DarkBrown, 0.90f, Vector2.zero, distortion: 0.02f, edgeSoftness: 0.46f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildEnvironmentalImpact(GameObject root)
        {
            // 1. Ground shockwave ring
            var ring = AddParticleChild(root.transform, "ImpactShockRing", loop: false, maxParticles: 2, duration: 0.85f);
            var rm = ring.main;
            rm.startLifetime = 0.65f;
            rm.startSpeed = 0f;
            rm.startSize = 5.5f;
            rm.startColor = new Color(1.0f, 0.72f, 0.28f, 0.92f);
            SetSingleBurst(ring, 0f, 1);
            AddSizeFade(ring, 0.22f, 0.95f, 1.2f);
            AddColorGradient(ring, AntiqueGold, Bronze, DarkBrown, 0f);
            var rRend = SetRenderer(ring, GetAdditiveMat("M_Env_ShockRingGold", "T_Vfx_ShockRing",
                new Color(1.65f, 1.05f, 0.32f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.1f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            rRend.alignment = ParticleSystemRenderSpace.Local;
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            // 2. Ground fissure/crack impression
            var crack = AddParticleChild(root.transform, "ImpactCrackDecal", loop: false, maxParticles: 2, duration: 1.6f);
            var cm = crack.main;
            cm.startLifetime = 1.45f;
            cm.startSpeed = 0f;
            cm.startSize = 4.2f;
            cm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.startColor = new Color(0.95f, 0.65f, 0.22f, 0.80f);
            SetSingleBurst(crack, 0f, 1);
            AddColorGradient(crack, AntiqueGold, Bronze, ObsidianBlack, 0f);
            var cRend = SetRenderer(crack, GetAdditiveMat("M_Env_GroundCrack", "T_Vfx_GroundCrack",
                new Color(1.55f, 0.95f, 0.32f, 1f), new Color(0.75f, 0.28f, 0.95f, 1f), 0.95f, Vector2.zero, radialFade: 1f),
                ParticleSystemRenderMode.Billboard);
            cRend.alignment = ParticleSystemRenderSpace.Local;
            crack.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            crack.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            // 3. Blender 3D ShockCone (Medium+)
            Mesh shockMesh = LoadVfxMesh("SM_Vfx_ShockCone") ?? GetFallbackShockConeMesh();
            var cone = AddParticleChild(root.transform, "ImpactShockCone", loop: false, maxParticles: 2, duration: 0.65f);
            var km = cone.main;
            km.startLifetime = 0.50f;
            km.startSpeed = 0f;
            km.startSize = 2.8f;
            km.startColor = new Color(0.85f, 0.65f, 0.28f, 0.78f);
            SetSingleBurst(cone, 0f, 1);
            AddSizeFade(cone, 0.35f, 1.0f, 1.35f);
            AddColorGradient(cone, AntiqueGold, Bronze, DarkBrown, 0f);
            var kRend = SetRenderer(cone, GetAdditiveMat("M_Env_ShockConeImpact", "T_Vfx_NovaWave",
                new Color(1.45f, 1.05f, 0.42f, 1f), new Color(0.85f, 0.42f, 0.16f, 1f), 0.95f, new Vector2(0f, -1.0f), radialFade: 0f, fadeEnds: 1f),
                ParticleSystemRenderMode.Mesh);
            kRend.mesh = shockMesh;
            kRend.alignment = ParticleSystemRenderSpace.Local;
            var coneFilter = cone.gameObject.AddComponent<VfxLayerTierFilter>();
            coneFilter.minQualityTier = VfxQualityOption.Medium;
            coneFilter.isShockwaveRingLayer = true;

            // 4. Radial dust plume + flying stone debris
            var dust = AddParticleChild(root.transform, "ImpactDustPlume", loop: false, maxParticles: 16, duration: 1.5f);
            var dm = dust.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.4f);
            dm.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.2f);
            dm.startSize = new ParticleSystem.MinMaxCurve(0.75f, 1.3f);
            dm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            dm.startColor = new Color(0.52f, 0.42f, 0.34f, 0.56f);
            dm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(dust, 0f, 12);
            var dsh = dust.shape;
            dsh.shapeType = ParticleSystemShapeType.Circle;
            dsh.radius = 0.65f;
            dsh.rotation = new Vector3(-90f, 0f, 0f);
            AddSizeFade(dust, 0.7f, 1.25f, 1.7f);
            AddColorGradient(dust, new Color(0.55f, 0.45f, 0.36f, 0.56f), DarkBrown, ObsidianBlack, 0f);
            SetRenderer(dust, GetParticleMat("M_Env_DustBurst", "T_Vfx_GothicSmoke",
                new Color(0.56f, 0.45f, 0.36f, 0.68f), DarkBrown, 0.90f, Vector2.zero, distortion: 0.02f, edgeSoftness: 0.46f),
                ParticleSystemRenderMode.Billboard);

            Mesh stoneMesh = LoadVfxMesh("SM_Vfx_StoneDebris") ?? GetFallbackDebrisMesh();
            var debris = AddParticleChild(root.transform, "ImpactStoneShards", loop: false, maxParticles: 12, duration: 1.5f);
            var sm = debris.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.95f, 1.4f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(3.4f, 6.2f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            sm.startRotation3D = true;
            sm.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            sm.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            sm.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            sm.gravityModifier = 2.2f;
            sm.startColor = new Color(0.45f, 0.38f, 0.34f, 1f);
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetSingleBurst(debris, 0f, 10);
            var ssh = debris.shape;
            ssh.shapeType = ParticleSystemShapeType.Cone;
            ssh.angle = 38f;
            ssh.radius = 0.5f;
            ssh.rotation = new Vector3(-90f, 0f, 0f);
            var sRend = SetRenderer(debris, GetStoneDebrisMaterial(), ParticleSystemRenderMode.Mesh);
            sRend.mesh = stoneMesh;
            sRend.alignment = ParticleSystemRenderSpace.Local;
            debris.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        // ==================== HELPERS & SHARED ASSETS ====================

        private static ParticleSystem AddParticleChild(Transform parent, string name, bool loop, int maxParticles, float duration = 2.0f)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = loop;
            main.duration = duration;
            main.maxParticles = maxParticles;
            main.stopAction = ParticleSystemStopAction.None;
            go.SetActive(true);
            return ps;
        }

        private static void SetSingleBurst(ParticleSystem ps, float time, short count)
        {
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
        }

        private static void AddSizeFade(ParticleSystem ps, float startScale, float midScale, float endScale)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0.0f, startScale),
                new Keyframe(0.35f, midScale),
                new Keyframe(1.0f, endScale));
            sol.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
        }

        private static void AddColorGradient(ParticleSystem ps, Color c0, Color c1, Color c2, float endAlpha)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(c0, 0.0f),
                    new GradientColorKey(c1, 0.45f),
                    new GradientColorKey(c2, 1.0f)
                },
                new[]
                {
                    new GradientAlphaKey(c0.a > 0.01f ? c0.a : 0.0f, 0.0f),
                    new GradientAlphaKey(Mathf.Max(0.25f, c1.a), 0.25f),
                    new GradientAlphaKey(Mathf.Max(0.20f, c1.a * 0.75f), 0.72f),
                    new GradientAlphaKey(endAlpha, 1.0f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        private static void AddCurlNoise(ParticleSystem ps, float strength, float freq)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = freq;
            noise.scrollSpeed = 0.55f;
            noise.quality = ParticleSystemNoiseQuality.Medium;
        }

        private static ParticleSystemRenderer SetRenderer(ParticleSystem ps, Material mat, ParticleSystemRenderMode mode)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = mode;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        public static Texture2D LoadVfxTex(string texName)
        {
            if (_texCache.TryGetValue(texName, out var cached) && cached != null)
                return cached;

            var tex = Resources.Load<Texture2D>("VFX/Textures/" + texName);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                _texCache[texName] = tex;
            }
            return tex;
        }

        public static Mesh LoadVfxMesh(string meshName)
        {
            if (_meshCache.TryGetValue(meshName, out var cached) && cached != null)
                return cached;

            Mesh m = Resources.Load<Mesh>("VFX/Meshes/" + meshName);
            if (m == null)
            {
                var prefab = Resources.Load<GameObject>("VFX/Meshes/" + meshName);
                if (prefab != null)
                {
                    var mf = prefab.GetComponentInChildren<MeshFilter>(true);
                    if (mf != null) m = mf.sharedMesh;
                }
            }

            if (m != null)
                _meshCache[meshName] = m;
            return m;
        }

        public static Material GetParticleMat(
            string matName,
            string texName,
            Color tint,
            Color edgeCol,
            float intensity,
            Vector2 scrollSpeed,
            float distortion = 0.03f,
            float edgeSoftness = 0.45f,
            float radialFade = 1.0f)
        {
            if (_matCache.TryGetValue(matName, out var m) && m != null)
                return m;

            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null)
            {
                _matCache[matName] = saved;
                return saved;
            }

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalEnvParticle")
                        ?? Shader.Find("AbyssalDungeon/EnvParticle")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit");

            m = new Material(sh) { name = matName, enableInstancing = true };
            Texture2D tex = LoadVfxTex(texName);
            if (tex != null)
            {
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            if (m.HasProperty("_SecondaryColor")) m.SetColor("_SecondaryColor", edgeCol);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            if (m.HasProperty("_ScrollSpeed")) m.SetVector("_ScrollSpeed", new Vector4(scrollSpeed.x, scrollSpeed.y, 0f, 0f));
            if (m.HasProperty("_DistortionAmp")) m.SetFloat("_DistortionAmp", distortion);
            if (m.HasProperty("_EdgeSoftness")) m.SetFloat("_EdgeSoftness", edgeSoftness);
            if (m.HasProperty("_UseRadialFade")) m.SetFloat("_UseRadialFade", radialFade);

            _matCache[matName] = m;
            return m;
        }

        public static Material GetAdditiveMat(
            string matName,
            string texName,
            Color coreColor,
            Color outerColor,
            float intensity,
            Vector2 scrollSpeed,
            float radialFade = 1.0f,
            float fadeEnds = 0.0f,
            float pulseSpeed = 0.0f)
        {
            if (_matCache.TryGetValue(matName, out var m) && m != null)
                return m;

            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null)
            {
                _matCache[matName] = saved;
                return saved;
            }

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalEnvAdditive")
                        ?? Shader.Find("AbyssalDungeon/EnvAdditive")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit");

            m = new Material(sh) { name = matName, enableInstancing = true };
            Texture2D tex = LoadVfxTex(texName);
            if (tex != null)
            {
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            if (m.HasProperty("_CoreColor")) m.SetColor("_CoreColor", coreColor);
            if (m.HasProperty("_OuterColor")) m.SetColor("_OuterColor", outerColor);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            if (m.HasProperty("_ScrollSpeed")) m.SetVector("_ScrollSpeed", new Vector4(scrollSpeed.x, scrollSpeed.y, 0f, 0f));
            if (m.HasProperty("_RadialFade")) m.SetFloat("_RadialFade", radialFade);
            if (m.HasProperty("_FadeEnds")) m.SetFloat("_FadeEnds", fadeEnds);
            if (m.HasProperty("_PulseSpeed")) m.SetFloat("_PulseSpeed", pulseSpeed);

            _matCache[matName] = m;
            return m;
        }

        public static Material GetStoneDebrisMaterial()
        {
            const string matName = "M_Env_StoneDebris";
            if (_matCache.TryGetValue(matName, out var m) && m != null) return m;
            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null) { _matCache[matName] = saved; return saved; }

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(sh) { name = matName, enableInstancing = true };
            Texture2D stoneTex = PbrMaterialLibrary.LoadPbrTex("T_GothicStone_Albedo");
            if (stoneTex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", stoneTex);
                m.mainTexture = stoneTex;
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.55f, 0.48f, 0.42f, 1f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.22f);
            _matCache[matName] = m;
            return m;
        }

        public static Material GetCandleClusterPropMaterial()
        {
            const string matName = "M_Env_CandleCluster";
            if (_matCache.TryGetValue(matName, out var m) && m != null) return m;
            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null) { _matCache[matName] = saved; return saved; }

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(sh) { name = matName, enableInstancing = true };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.76f, 0.66f, 0.50f, 1f));
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.15f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.42f);
            _matCache[matName] = m;
            return m;
        }

        /// <summary>
        /// Pre-builds all shared Environment VFX materials into a dictionary for Editor .mat asset baking.
        /// </summary>
        public static Dictionary<string, Material> BuildAllSharedMaterials()
        {
            var dict = new Dictionary<string, Material>(StringComparer.Ordinal);
            void Reg(Material mat) { if (mat != null && !dict.ContainsKey(mat.name)) dict[mat.name] = mat; }

            Reg(GetAdditiveMat("M_Env_FlameWisp", "T_Vfx_FlameWisp", new Color(1.75f, 1.35f, 0.62f, 1f), new Color(1.35f, 0.42f, 0.08f, 1f), 1.05f, Vector2.zero, 1f, 0f, 6f));
            Reg(GetAdditiveMat("M_Env_CandleFlame", "T_Vfx_FlameWisp", new Color(1.65f, 1.35f, 0.75f, 1f), new Color(1.20f, 0.65f, 0.18f, 1f), 1.0f, Vector2.zero, 1f, 0f, 8f));
            Reg(GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark", new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, 1f));
            Reg(GetAdditiveMat("M_Env_GroundFireBed", "T_Vfx_GroundFogWisp", new Color(1.45f, 0.58f, 0.14f, 1f), new Color(0.85f, 0.18f, 0.08f, 1f), 0.9f, new Vector2(0.05f, 0.03f), 1f));
            Reg(GetAdditiveMat("M_Env_ShockRingGold", "T_Vfx_ShockRing", new Color(1.65f, 1.05f, 0.32f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.1f, Vector2.zero, 1f));
            Reg(GetParticleMat("M_Env_TorchSmoke", "T_Vfx_GothicSmoke", new Color(0.38f, 0.30f, 0.25f, 0.58f), ObsidianBlack, 0.85f, Vector2.zero, 0.03f, 0.48f));
            Reg(GetParticleMat("M_Env_BlackSmoke", "T_Vfx_GothicSmoke", new Color(0.20f, 0.15f, 0.15f, 0.78f), new Color(0.05f, 0.04f, 0.06f, 0.95f), 0.75f, Vector2.zero, 0.04f, 0.45f));
            Reg(GetParticleMat("M_Env_AmbientSmoke", "T_Vfx_GroundFogWisp", new Color(0.45f, 0.40f, 0.50f, 0.38f), ObsidianBlack, 0.9f, new Vector2(0.02f, 0.01f), 0.03f, 0.50f));
            Reg(GetParticleMat("M_Env_GroundFog", "T_Vfx_GroundFogWisp", new Color(0.46f, 0.42f, 0.58f, 0.42f), new Color(0.12f, 0.10f, 0.18f, 0.7f), 0.92f, new Vector2(0.03f, 0.015f), 0.03f, 0.52f));
            Reg(GetParticleMat("M_Env_DustMotes", "T_Vfx_EmberSpark", new Color(0.95f, 0.86f, 0.65f, 0.65f), Bronze, 1.0f, Vector2.zero, 0f, 0.45f));
            Reg(GetParticleMat("M_Env_AshFlakes", "T_Vfx_AshFlake", new Color(0.68f, 0.60f, 0.55f, 0.78f), DarkBrown, 0.9f, Vector2.zero, 0f, 0.30f, 0f));
            Reg(GetAdditiveMat("M_Env_CrystalFlare", "T_Vfx_StarFlare", new Color(1.45f, 0.95f, 1.85f, 1f), new Color(0.55f, 0.25f, 1.15f, 1f), 1.0f, Vector2.zero, 1f, 0f, 2.5f));
            Reg(GetAdditiveMat("M_Env_RunesSheet", "T_Vfx_RunesSheet", new Color(1.65f, 1.38f, 0.58f, 1f), new Color(0.62f, 0.28f, 1.35f, 1f), 1.0f, Vector2.zero, 0f));
            Reg(GetAdditiveMat("M_Env_EnergyTrail", "T_Vfx_EnergyTrail", new Color(1.35f, 0.95f, 1.85f, 1f), new Color(0.75f, 0.28f, 1.45f, 1f), 1.05f, Vector2.zero, 1f));
            Reg(GetAdditiveMat("M_Env_PortalVortex", "T_Vfx_EnergyTrail", new Color(1.55f, 1.15f, 0.65f, 1f), new Color(0.75f, 0.28f, 1.55f, 1f), 0.95f, new Vector2(0.35f, -0.75f), 0f, 1f));
            Reg(GetAdditiveMat("M_Env_RuneCirclePurple", "T_Vfx_GothicRuneCircle", new Color(1.65f, 1.32f, 0.55f, 1f), new Color(0.68f, 0.28f, 1.45f, 1f), 0.95f, Vector2.zero, 1f));
            Reg(GetAdditiveMat("M_Env_RuneCircleGold", "T_Vfx_GothicRuneCircle", new Color(1.85f, 1.48f, 0.62f, 1f), new Color(0.75f, 0.36f, 1.35f, 1f), 1.05f, Vector2.zero, 1f));
            Reg(GetAdditiveMat("M_Env_PillarBeam", "T_Vfx_PillarBeam", new Color(1.85f, 1.45f, 0.62f, 1f), new Color(0.68f, 0.32f, 1.45f, 1f), 1.05f, Vector2.zero, 0f, 1f));
            Reg(GetAdditiveMat("M_Env_ShockConeFire", "T_Vfx_NovaWave", new Color(1.65f, 0.95f, 0.35f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.0f, new Vector2(0f, -1.2f), 0f, 1f));
            Reg(GetAdditiveMat("M_Env_ShockConeImpact", "T_Vfx_NovaWave", new Color(1.45f, 1.05f, 0.42f, 1f), new Color(0.85f, 0.42f, 0.16f, 1f), 0.95f, new Vector2(0f, -1.0f), 0f, 1f));
            Reg(GetParticleMat("M_Env_DustBurst", "T_Vfx_GothicSmoke", new Color(0.56f, 0.45f, 0.36f, 0.68f), DarkBrown, 0.90f, Vector2.zero, 0.02f, 0.46f));
            Reg(GetAdditiveMat("M_Env_GroundCrack", "T_Vfx_GroundCrack", new Color(1.55f, 0.95f, 0.32f, 1f), new Color(0.75f, 0.28f, 0.95f, 1f), 0.95f, Vector2.zero, 1f));
            Reg(GetStoneDebrisMaterial());
            Reg(GetCandleClusterPropMaterial());
            return dict;
        }

        private static Mesh GetFallbackDebrisMesh()
        {
            if (_fallbackDebrisMesh != null) return _fallbackDebrisMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _fallbackDebrisMesh = go.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(go);
            return _fallbackDebrisMesh;
        }

        private static Mesh GetFallbackShockConeMesh()
        {
            if (_fallbackConeMesh != null) return _fallbackConeMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _fallbackConeMesh = go.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(go);
            return _fallbackConeMesh;
        }
    }
}

