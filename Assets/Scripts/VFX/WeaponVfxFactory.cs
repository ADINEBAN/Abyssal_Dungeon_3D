using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using AbyssGame;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// Complete Blender 5.3.0 3D Weapon & Combat VFX Factory for Abyssal Dungeon 3D (Unity 2021.3.11f1 + URP 12.1.7).
    /// Powers 3D volumetric slashes, heavy serrated cleaves, 360-degree blade whirlwinds, rifled piercing thrusts,
    /// Gothic Aegis shield blocks, poise shatters, 3D runic arrows & spell bolts, and build-reactive weapon enchant auras.
    /// Reuses VFXManager pooling + VfxQualitySystem scaling with zero duplicate managers or gameplay side effects.
    /// </summary>
    public static class WeaponVfxFactory
    {
        public static readonly VFXType[] AllWeaponVfxTypes =
        {
            VFXType.Wpn_LightSlash,
            VFXType.Wpn_HeavyCleave,
            VFXType.Wpn_SpinWhirlwind,
            VFXType.Wpn_ThrustPierce,
            VFXType.Wpn_ShieldBlockAegis,
            VFXType.Wpn_PoiseShatter,
            VFXType.Wpn_MuzzleCastBurst,
            VFXType.Wpn_ArrowImpact,
            VFXType.Wpn_SpellImpact,
            VFXType.Wpn_EnchantAura,
            VFXType.Wpn_Asn_DualBladeSlash,
            VFXType.Wpn_Asn_RapidCombo,
            VFXType.Wpn_Asn_DashAfterimage,
            VFXType.Wpn_Asn_BackstabImpact,
            VFXType.Wpn_Asn_PoisonImpact,
            VFXType.Wpn_Asn_ShadowBurst
        };

        private static readonly Dictionary<string, Material> _matCache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public static bool IsWeaponVfx(VFXType type)
        {
            return type >= VFXType.Wpn_LightSlash && type <= VFXType.Wpn_Asn_ShadowBurst;
        }

        public static Mesh LoadWeaponMesh(string meshName)
        {
            if (_meshCache.TryGetValue(meshName, out var cached) && cached != null)
                return cached;

            string[] searchPaths =
            {
                "VFX/Meshes/" + meshName,
                "VFX/Meshes/Assassin/" + meshName,
                "VFX/Meshes/Shared/" + meshName
            };

            Mesh m = null;
            for (int i = 0; i < searchPaths.Length && m == null; i++)
            {
                m = Resources.Load<Mesh>(searchPaths[i]);
                if (m == null)
                {
                    var prefab = Resources.Load<GameObject>(searchPaths[i]);
                    if (prefab != null)
                    {
                        var mf = prefab.GetComponentInChildren<MeshFilter>(true);
                        if (mf != null) m = mf.sharedMesh;
                    }
                }
            }

            if (m != null)
                _meshCache[meshName] = m;
            return m;
        }

        public static Texture2D LoadWeaponTex(string texName)
        {
            if (_texCache.TryGetValue(texName, out var cached) && cached != null)
                return cached;

            var tex = Resources.Load<Texture2D>("VFX/Textures/" + texName);
            if (tex != null)
                _texCache[texName] = tex;
            return tex;
        }

        public static GameObject CreateWeaponVfx(VFXType type)
        {
            var root = new GameObject($"VFX_{type}");
            bool isTelegraph = false;
            switch (type)
            {
                case VFXType.Wpn_LightSlash:          BuildLightSlash(root); break;
                case VFXType.Wpn_HeavyCleave:         BuildHeavyCleave(root); break;
                case VFXType.Wpn_SpinWhirlwind:       BuildSpinWhirlwind(root); break;
                case VFXType.Wpn_ThrustPierce:        BuildThrustPierce(root); break;
                case VFXType.Wpn_ShieldBlockAegis:    BuildShieldBlockAegis(root); break;
                case VFXType.Wpn_PoiseShatter:        BuildPoiseShatter(root); break;
                case VFXType.Wpn_MuzzleCastBurst:     BuildMuzzleCastBurst(root); break;
                case VFXType.Wpn_ArrowImpact:         BuildArrowImpact(root); break;
                case VFXType.Wpn_SpellImpact:         BuildSpellImpact(root); break;
                case VFXType.Wpn_EnchantAura:         BuildEnchantAuraPrefab(root); break;
                case VFXType.Wpn_Asn_DualBladeSlash:  BuildAsnDualBladeSlash(root); break;
                case VFXType.Wpn_Asn_RapidCombo:      BuildAsnRapidCombo(root); break;
                case VFXType.Wpn_Asn_DashAfterimage:  BuildAsnDashAfterimage(root); break;
                case VFXType.Wpn_Asn_BackstabImpact:  BuildAsnBackstabImpact(root); isTelegraph = true; break;
                case VFXType.Wpn_Asn_PoisonImpact:    BuildAsnPoisonImpact(root); isTelegraph = true; break;
                case VFXType.Wpn_Asn_ShadowBurst:     BuildAsnShadowBurst(root); isTelegraph = true; break;
            }

            var responder = root.AddComponent<ScalableVfxResponder>();
            responder.isCombatTelegraph = isTelegraph;
            return root;
        }

        // ==================== 10 POOLED 3D WEAPON VFX PREFABS ====================

        private static void BuildLightSlash(GameObject root)
        {
            Mesh slashMesh = LoadWeaponMesh("SM_Vfx_WpnSlashCrescent");
            var arc = AddParticleChild(root.transform, "SlashCrescent3D", loop: false, maxParticles: 2, duration: 0.25f);
            var am = arc.main;
            am.startLifetime = 0.20f;
            am.startSpeed = 0f;
            am.startSize = 1.0f;
            am.startColor = new Color(1.0f, 0.92f, 0.62f, 0.95f);
            SetBurst(arc, 0f, 1);
            AddSizeCurve(arc, 0.85f, 1.05f, 1.14f);
            AddAlphaFade(arc, 1.0f, 0.85f, 0f);
            var aRend = SetRenderer(arc, GetSlashTrailMaterial("M_Wpn_SlashCrescent", "T_Vfx_SlashCrescent",
                new Color(1.85f, 1.55f, 0.85f, 1f), new Color(0.95f, 0.52f, 0.16f, 1f)),
                slashMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (slashMesh != null)
            {
                aRend.mesh = slashMesh;
                aRend.alignment = ParticleSystemRenderSpace.Local;
            }

            AddArcSparks(root.transform, "SlashEdgeSparks", count: 8, radius: 1.45f, speed: 3.6f, VfxQualityOption.Medium);
        }

        private static void BuildHeavyCleave(GameObject root)
        {
            Mesh cleaveMesh = LoadWeaponMesh("SM_Vfx_WpnHeavyCleaveWave") ?? LoadWeaponMesh("SM_Vfx_WpnSlashCrescent");
            var cleave = AddParticleChild(root.transform, "HeavyCleaveWave3D", loop: false, maxParticles: 2, duration: 0.34f);
            var cm = cleave.main;
            cm.startLifetime = 0.28f;
            cm.startSpeed = 0f;
            cm.startSize = 1.08f;
            cm.startColor = new Color(1.0f, 0.88f, 0.48f, 0.98f);
            SetBurst(cleave, 0f, 1);
            AddSizeCurve(cleave, 0.82f, 1.08f, 1.22f);
            AddAlphaFade(cleave, 1.0f, 0.90f, 0f);
            var cRend = SetRenderer(cleave, GetSlashTrailMaterial("M_Wpn_HeavyCleave", "T_Vfx_WpnSerratedArc",
                new Color(2.05f, 1.65f, 0.72f, 1f), new Color(1.35f, 0.32f, 0.12f, 1f)),
                cleaveMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (cleaveMesh != null)
            {
                cRend.mesh = cleaveMesh;
                cRend.alignment = ParticleSystemRenderSpace.Local;
            }

            AddArcSparks(root.transform, "HeavyCleaveEmbers", count: 16, radius: 1.85f, speed: 5.5f, VfxQualityOption.Low);
        }

        private static void BuildSpinWhirlwind(GameObject root)
        {
            Mesh spinMesh = LoadWeaponMesh("SM_Vfx_WpnSpinWhirlwind");
            var spin = AddParticleChild(root.transform, "WhirlwindHelix3D", loop: false, maxParticles: 2, duration: 0.45f);
            var sm = spin.main;
            sm.startLifetime = 0.36f;
            sm.startSpeed = 0f;
            sm.startSize = 1.05f;
            sm.startColor = new Color(1.0f, 0.92f, 0.68f, 0.95f);
            SetBurst(spin, 0f, 1);

            var rot = spin.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.y = 14.5f; // High-speed 360 cyclone spin around vertical Y axis

            AddSizeCurve(spin, 0.78f, 1.06f, 1.20f);
            AddAlphaFade(spin, 0.95f, 0.88f, 0f);
            var sRend = SetRenderer(spin, GetWeaponAuraMaterial("M_Wpn_SpinWhirlwind", "T_Vfx_WpnSerratedArc",
                new Color(1.85f, 1.55f, 0.78f, 1f), new Color(0.85f, 0.48f, 0.18f, 1f), new Vector2(-3.2f, 0f), 1.2f, 0.0f),
                spinMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (spinMesh != null)
            {
                sRend.mesh = spinMesh;
                sRend.alignment = ParticleSystemRenderSpace.Local;
            }

            AddArcSparks(root.transform, "WhirlwindSparks", count: 14, radius: 1.75f, speed: 4.8f, VfxQualityOption.Medium);
        }

        private static void BuildThrustPierce(GameObject root)
        {
            Mesh spearMesh = LoadWeaponMesh("SM_Vfx_WpnThrustSpearCone");
            var spear = AddParticleChild(root.transform, "ThrustLanceCone3D", loop: false, maxParticles: 2, duration: 0.30f);
            var sm = spear.main;
            sm.startLifetime = 0.24f;
            sm.startSpeed = 2.8f;
            sm.startSize = 1.0f;
            sm.startColor = new Color(1.0f, 0.92f, 0.72f, 0.95f);
            sm.simulationSpace = ParticleSystemSimulationSpace.Local;
            SetBurst(spear, 0f, 1);
            var sh = spear.shape;
            sh.enabled = false;

            var rot = spear.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.z = 16.0f; // Rifled drill spin along forward +Z thrust axis!

            AddSizeCurve(spear, 0.65f, 1.08f, 0.85f);
            AddAlphaFade(spear, 1.0f, 0.82f, 0f);
            var sRend = SetRenderer(spear, GetWeaponAuraMaterial("M_Wpn_ThrustCone", "T_Vfx_WpnRuneStreak",
                new Color(1.95f, 1.68f, 0.88f, 1f), new Color(0.85f, 0.42f, 1.25f, 1f), new Vector2(-4.0f, 0.4f), 1.25f, 0.0f),
                spearMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Stretch);
            if (spearMesh != null)
            {
                sRend.mesh = spearMesh;
                sRend.alignment = ParticleSystemRenderSpace.Local;
            }

            // Sonic collar ring at hilt
            var ring = AddParticleChild(root.transform, "ThrustSonicCollar", loop: false, maxParticles: 2, duration: 0.28f);
            var rm = ring.main;
            rm.startLifetime = 0.22f;
            rm.startSpeed = 0f;
            rm.startSize = 1.45f;
            rm.startColor = new Color(1.0f, 0.88f, 0.55f, 0.88f);
            SetBurst(ring, 0f, 1);
            AddSizeCurve(ring, 0.35f, 1.0f, 1.35f);
            AddAlphaFade(ring, 0.95f, 0.65f, 0f);
            var rRend = SetRenderer(ring, EnvironmentVfxFactory.GetAdditiveMat("M_Env_ShockRingGold", "T_Vfx_ShockRing",
                new Color(1.65f, 1.05f, 0.32f, 1f), new Color(1.25f, 0.28f, 0.08f, 1f), 1.1f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);
            rRend.alignment = ParticleSystemRenderSpace.Local;
        }

        private static void BuildShieldBlockAegis(GameObject root)
        {
            Mesh aegisMesh = LoadWeaponMesh("SM_Vfx_WpnShieldAegis");
            var dome = AddParticleChild(root.transform, "AegisBarrier3D", loop: false, maxParticles: 2, duration: 0.42f);
            var dm = dome.main;
            dm.startLifetime = 0.34f;
            dm.startSpeed = 0f;
            dm.startSize = 1.05f;
            dm.startColor = new Color(1.0f, 0.88f, 0.48f, 0.92f);
            SetBurst(dome, 0f, 1);
            AddSizeCurve(dome, 0.78f, 1.05f, 1.12f);
            AddAlphaFade(dome, 1.0f, 0.78f, 0f);
            var dRend = SetRenderer(dome, GetWeaponAuraMaterial("M_Wpn_ShieldAegis", "T_Vfx_WpnAegisHex",
                new Color(1.95f, 1.65f, 0.72f, 1f), new Color(0.85f, 0.55f, 0.18f, 1f), Vector2.zero, 1.25f, 0.004f),
                aegisMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (aegisMesh != null)
            {
                dRend.mesh = aegisMesh;
                dRend.alignment = ParticleSystemRenderSpace.Local;
            }

            // Outward metallic/holy deflection sparks
            AddArcSparks(root.transform, "BlockDeflectSparks", count: 12, radius: 0.35f, speed: 6.2f, VfxQualityOption.Low);
        }

        private static void BuildPoiseShatter(GameObject root)
        {
            // Expanding golden-crimson shatter lines decal
            var shatter = AddParticleChild(root.transform, "PoiseShatterRing", loop: false, maxParticles: 2, duration: 0.55f);
            var sm = shatter.main;
            sm.startLifetime = 0.45f;
            sm.startSpeed = 0f;
            sm.startSize = 3.4f;
            sm.startColor = new Color(1.0f, 0.78f, 0.25f, 0.95f);
            SetBurst(shatter, 0f, 1);
            AddSizeCurve(shatter, 0.28f, 1.0f, 1.28f);
            AddAlphaFade(shatter, 1.0f, 0.75f, 0f);
            SetRenderer(shatter, EnvironmentVfxFactory.GetAdditiveMat("M_Wpn_PoiseShatterLines", "T_Vfx_ShatterLines",
                new Color(1.95f, 1.55f, 0.55f, 1f), new Color(1.35f, 0.28f, 0.12f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);

            // 3D Crystal/Armor shard burst
            Mesh shardMesh = LoadWeaponMesh("SM_Vfx_WpnSpellBoltCore") ?? EnvironmentVfxFactory.LoadVfxMesh("SM_Vfx_StoneDebris");
            if (shardMesh != null)
            {
                var shards = AddParticleChild(root.transform, "ArmorShatterShards3D", loop: false, maxParticles: 16, duration: 0.75f);
                var shm = shards.main;
                shm.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.68f);
                shm.startSpeed = new ParticleSystem.MinMaxCurve(4.2f, 7.8f);
                shm.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
                shm.startRotation3D = true;
                shm.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                shm.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                shm.gravityModifier = 1.8f;
                shm.startColor = new Color(1.0f, 0.82f, 0.34f, 0.95f);
                shm.simulationSpace = ParticleSystemSimulationSpace.World;
                SetBurst(shards, 0f, 12);
                var shShape = shards.shape;
                shShape.shapeType = ParticleSystemShapeType.Sphere;
                shShape.radius = 0.35f;
                AddAlphaFade(shards, 1.0f, 0.85f, 0f);
                var sRend = SetRenderer(shards, GetWeaponAuraMaterial("M_Wpn_CrystalShards", "T_Vfx_WpnRuneStreak",
                    new Color(1.95f, 1.55f, 0.58f, 1f), new Color(1.25f, 0.35f, 0.14f, 1f), Vector2.zero, 1.2f, 0f),
                    ParticleSystemRenderMode.Mesh);
                sRend.mesh = shardMesh;
                shards.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
            }
        }

        private static void BuildMuzzleCastBurst(GameObject root)
        {
            var flash = AddParticleChild(root.transform, "MuzzleStarFlash", loop: false, maxParticles: 2, duration: 0.26f);
            var fm = flash.main;
            fm.startLifetime = 0.20f;
            fm.startSpeed = 0f;
            fm.startSize = 0.92f;
            fm.startColor = new Color(1.0f, 0.90f, 0.62f, 0.90f);
            SetBurst(flash, 0f, 1);
            AddSizeCurve(flash, 0.35f, 1.0f, 0.15f);
            AddAlphaFade(flash, 1.0f, 0.70f, 0f);
            SetRenderer(flash, EnvironmentVfxFactory.GetAdditiveMat("M_Env_CrystalFlare", "T_Vfx_StarFlare",
                new Color(1.75f, 1.48f, 0.72f, 1f), new Color(0.85f, 0.42f, 1.25f, 1f), 1.05f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);

            AddArcSparks(root.transform, "MuzzleSparks", count: 6, radius: 0.15f, speed: 4.5f, VfxQualityOption.Medium);
        }

        private static void BuildArrowImpact(GameObject root)
        {
            var flash = AddParticleChild(root.transform, "ArrowPierceFlash", loop: false, maxParticles: 2, duration: 0.26f);
            var fm = flash.main;
            fm.startLifetime = 0.20f;
            fm.startSpeed = 0f;
            fm.startSize = 1.45f;
            fm.startColor = new Color(0.75f, 1.0f, 0.78f, 0.95f);
            SetBurst(flash, 0f, 1);
            AddSizeCurve(flash, 0.45f, 1.1f, 0.2f);
            AddAlphaFade(flash, 1.0f, 0.65f, 0f);
            SetRenderer(flash, EnvironmentVfxFactory.GetAdditiveMat("M_Env_CrystalFlare", "T_Vfx_StarFlare",
                new Color(1.45f, 1.85f, 1.15f, 1f), new Color(0.28f, 0.95f, 0.48f, 1f), 1.1f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);

            AddArcSparks(root.transform, "ArrowWindSparks", count: 9, radius: 0.15f, speed: 6.0f, VfxQualityOption.Low);
        }

        private static void BuildSpellImpact(GameObject root)
        {
            var nova = AddParticleChild(root.transform, "SpellImpactNova", loop: false, maxParticles: 2, duration: 0.35f);
            var nm = nova.main;
            nm.startLifetime = 0.28f;
            nm.startSpeed = 0f;
            nm.startSize = 1.95f;
            nm.startColor = new Color(0.92f, 0.78f, 1.0f, 0.95f);
            SetBurst(nova, 0f, 1);
            AddSizeCurve(nova, 0.35f, 1.05f, 1.30f);
            AddAlphaFade(nova, 1.0f, 0.75f, 0f);
            SetRenderer(nova, EnvironmentVfxFactory.GetAdditiveMat("M_Wpn_SpellNova", "T_Vfx_NovaWave",
                new Color(1.75f, 1.35f, 1.95f, 1f), new Color(0.68f, 0.28f, 1.45f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);

            AddArcSparks(root.transform, "SpellArcaneSparks", count: 11, radius: 0.22f, speed: 5.8f, VfxQualityOption.Low);
        }

        private static void BuildEnchantAuraPrefab(GameObject root)
        {
            Mesh sheathMesh = LoadWeaponMesh("SM_Vfx_WpnBladeAuraSheath");
            if (sheathMesh != null)
            {
                var meshGo = new GameObject("BladeAuraSheath3D");
                meshGo.transform.SetParent(root.transform, false);
                meshGo.AddComponent<MeshFilter>().sharedMesh = sheathMesh;
                var mr = meshGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetWeaponAuraMaterial("M_Wpn_BladeAuraSheath", "T_Vfx_WpnRuneStreak",
                    new Color(1.85f, 1.48f, 0.62f, 1f), new Color(0.85f, 0.42f, 0.16f, 1f), new Vector2(-1.4f, 0.25f), 1.05f, 0.006f);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            var motes = AddParticleChild(root.transform, "EnchantRuneMotes", loop: true, maxParticles: 10, duration: 2.0f);
            var mm = motes.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            mm.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
            mm.startColor = new Color(1.0f, 0.86f, 0.48f, 0.85f);
            mm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var mem = motes.emission;
            mem.rateOverTime = 5.5f;
            var msh = motes.shape;
            msh.shapeType = ParticleSystemShapeType.Box;
            msh.position = new Vector3(0f, 0.52f, 0f);
            msh.scale = new Vector3(0.08f, 0.82f, 0.08f);
            AddAlphaFade(motes, 0.0f, 0.85f, 0.0f);
            SetRenderer(motes, EnvironmentVfxFactory.GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);
            motes.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;
        }

        // ==================== RUNTIME WEAPON ATTACHMENTS & 3D PROJECTILES ====================

        /// <summary>
        /// Inspects the actual 3D MeshFilter bounds on a weapon (or falls back to socket axis) and aligns
        /// SM_Vfx_WpnBladeAuraSheath cleanly from crossguard/collar to blade/staff tip so it never misaligns!
        /// </summary>
        public static GameObject AttachSmartWeaponAura(Transform weaponOrSocket, Color accentColor, float widthMultiplier = 1.0f)
        {
            if (weaponOrSocket == null) return null;

            MeshFilter targetMf = null;
            var mfs = weaponOrSocket.GetComponentsInChildren<MeshFilter>(true);
            float bestLen = 0f;
            for (int i = 0; i < mfs.Length; i++)
            {
                var mf = mfs[i];
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.name.StartsWith("BladeSheath", StringComparison.Ordinal)) continue;
                Vector3 sz = mf.sharedMesh.bounds.size;
                float maxDim = Mathf.Max(sz.x, Mathf.Max(sz.y, sz.z));
                if (maxDim > bestLen)
                {
                    bestLen = maxDim;
                    targetMf = mf;
                }
            }

            if (targetMf != null && bestLen > 0.12f)
            {
                Bounds b = targetMf.sharedMesh.bounds;
                Vector3 sz = b.size;
                Vector3 axisDir;
                float axisMin, axisMax, crossThickness;

                if (sz.z >= sz.x && sz.z >= sz.y)
                {
                    bool posTip = Mathf.Abs(b.max.z) >= Mathf.Abs(b.min.z);
                    axisDir = posTip ? Vector3.forward : Vector3.back;
                    axisMin = posTip ? b.min.z : -b.max.z;
                    axisMax = posTip ? b.max.z : -b.min.z;
                    crossThickness = Mathf.Max(sz.x, sz.y);
                }
                else if (sz.y >= sz.x && sz.y >= sz.z)
                {
                    bool posTip = Mathf.Abs(b.max.y) >= Mathf.Abs(b.min.y);
                    axisDir = posTip ? Vector3.up : Vector3.down;
                    axisMin = posTip ? b.min.y : -b.max.y;
                    axisMax = posTip ? b.max.y : -b.min.y;
                    crossThickness = Mathf.Max(sz.x, sz.z);
                }
                else
                {
                    bool posTip = Mathf.Abs(b.max.x) >= Mathf.Abs(b.min.x);
                    axisDir = posTip ? Vector3.right : Vector3.left;
                    axisMin = posTip ? b.min.x : -b.max.x;
                    axisMax = posTip ? b.max.x : -b.min.x;
                    crossThickness = Mathf.Max(sz.y, sz.z);
                }

                float totalLen = Mathf.Max(0.22f, axisMax - axisMin);
                // Start 24% up from pommel/grip so aura wraps the blade/head rather than the wielder's hand
                float startAlong = axisMin + totalLen * 0.24f;
                float auraLen = Mathf.Clamp(totalLen * 0.78f, 0.24f, 1.65f);
                float auraWidth = Mathf.Clamp(crossThickness * 3.2f, 0.55f, 1.65f) * widthMultiplier;

                Vector3 perpCenter = b.center - Vector3.Project(b.center, axisDir);
                Vector3 localStart = perpCenter + axisDir * startAlong;
                Quaternion localRot = Quaternion.FromToRotation(Vector3.up, axisDir);

                return AttachWeaponEnchantAura(
                    targetMf.transform,
                    localStart,
                    localRot,
                    new Vector3(auraWidth, auraLen, auraWidth),
                    accentColor);
            }

            return AttachWeaponEnchantAura(
                weaponOrSocket,
                new Vector3(0f, 0.14f, 0f),
                Quaternion.identity,
                new Vector3(0.75f * widthMultiplier, 0.68f, 0.75f * widthMultiplier),
                accentColor);
        }

        /// <summary>
        /// Attaches a build-reactive 3D Blender Weapon Enchant Aura (SM_Vfx_WpnBladeAuraSheath) + subtle elemental motes
        /// directly onto any hero or boss weapon without casting shadows or adding colliders.
        /// </summary>
        public static GameObject AttachWeaponEnchantAura(
            Transform weaponParent,
            Vector3 localOffset,
            Quaternion localRot,
            Vector3 auraScale,
            Color accentColor)
        {
            if (weaponParent == null) return null;

            Transform existing = weaponParent.Find("WeaponEnchantAura3D");
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

            GameObject auraRoot = new GameObject("WeaponEnchantAura3D");
            auraRoot.transform.SetParent(weaponParent, false);
            auraRoot.transform.localPosition = localOffset;
            auraRoot.transform.localRotation = localRot;
            auraRoot.transform.localScale = auraScale;

            Mesh sheathMesh = LoadWeaponMesh("SM_Vfx_WpnBladeAuraSheath");
            if (sheathMesh != null)
            {
                var sheathGo = new GameObject("BladeSheathMesh");
                sheathGo.transform.SetParent(auraRoot.transform, false);
                sheathGo.AddComponent<MeshFilter>().sharedMesh = sheathMesh;
                var mr = sheathGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetWeaponAuraMaterial("M_Wpn_BladeAuraSheath", "T_Vfx_WpnRuneStreak",
                    new Color(1.85f, 1.48f, 0.62f, 1f), new Color(0.85f, 0.42f, 0.16f, 1f), new Vector2(-1.4f, 0.25f), 1.0f, 0.006f);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var mpb = new MaterialPropertyBlock();
                Color brightCore = Color.Lerp(accentColor, new Color(1f, 0.96f, 0.82f), 0.52f);
                mpb.SetColor("_CoreColor", brightCore * 1.65f);
                mpb.SetColor("_RimColor", accentColor * 1.15f);
                mpb.SetFloat("_Opacity", 0.58f);
                mr.SetPropertyBlock(mpb);
            }

            // Subtle rising rune/ember sparks on Medium+ quality tiers
            var motes = AddParticleChild(auraRoot.transform, "WeaponAuraSparks", loop: true, maxParticles: 8, duration: 2.0f);
            var mm = motes.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            mm.startSpeed = new ParticleSystem.MinMaxCurve(0.10f, 0.26f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            mm.startColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.82f);
            mm.simulationSpace = ParticleSystemSimulationSpace.World;
            var mem = motes.emission;
            mem.rateOverTime = 4.5f;
            var msh = motes.shape;
            msh.shapeType = ParticleSystemShapeType.Box;
            msh.position = new Vector3(0f, 0.48f, 0f);
            msh.scale = new Vector3(0.08f, 0.78f, 0.08f);
            AddAlphaFade(motes, 0.0f, 0.85f, 0.0f);
            SetRenderer(motes, EnvironmentVfxFactory.GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);
            motes.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = VfxQualityOption.Medium;

            auraRoot.AddComponent<ScalableVfxResponder>();
            return auraRoot;
        }

        /// <summary>
        /// Builds a sculpted 3D Blender projectile visual (SM_Vfx_WpnArrowBolt for bows, SM_Vfx_WpnSpellBoltCore for spells)
        /// plus a URP runic TrailRenderer on a spawned projectile GameObject.
        /// </summary>
        public static TrailRenderer BuildProjectile3DVisual(GameObject projGo, bool isArrow, Color color, bool empowered, out Material ownedTrailMat)
        {
            ownedTrailMat = null;
            string meshKey = isArrow ? "SM_Vfx_WpnArrowBolt" : "SM_Vfx_WpnSpellBoltCore";
            Mesh projMesh = LoadWeaponMesh(meshKey);

            var visualChild = new GameObject(isArrow ? "ArrowBolt3D" : "SpellBoltCore3D");
            visualChild.transform.SetParent(projGo.transform, false);
            visualChild.transform.localScale = Vector3.one * (empowered ? 1.28f : 1.0f);

            if (projMesh != null)
            {
                visualChild.AddComponent<MeshFilter>().sharedMesh = projMesh;
                var mr = visualChild.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetWeaponAuraMaterial(
                    isArrow ? "M_Wpn_ArrowBolt3D" : "M_Wpn_SpellBolt3D",
                    "T_Vfx_WpnRuneStreak",
                    new Color(1.95f, 1.75f, 0.95f, 1f),
                    new Color(0.65f, 0.45f, 1.25f, 1f),
                    new Vector2(-3.2f, 0.2f),
                    1.35f,
                    0.0f);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var mpb = new MaterialPropertyBlock();
                Color core = Color.Lerp(color, Color.white, 0.58f);
                mpb.SetColor("_CoreColor", core * 2.0f);
                mpb.SetColor("_RimColor", color * 1.35f);
                mpb.SetFloat("_Opacity", 0.96f);
                mr.SetPropertyBlock(mpb);

                var spin = visualChild.AddComponent<WeaponProjectileSpin3D>();
                spin.SpinZ = isArrow ? 540f : 380f;
            }
            else
            {
                var mat = CharacterVisualFactory.MakeMat(color, 0.9f, 0f, color * 2.4f);
                CharacterVisualFactory.Prim(PrimitiveType.Sphere, "FallbackOrb", visualChild.transform, Vector3.zero, new Vector3(0.30f, 0.30f, 0.56f), mat);
            }

            var trailNode = new GameObject("ProjectileRuneTrail");
            trailNode.transform.SetParent(projGo.transform, false);
            var tr = trailNode.AddComponent<TrailRenderer>();
            tr.time = isArrow ? 0.13f : 0.16f;
            tr.minVertexDistance = 0.05f;
            tr.numCornerVertices = 3;
            tr.numCapVertices = 3;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.widthCurve = new AnimationCurve(
                new Keyframe(0f, empowered ? 0.34f : (isArrow ? 0.18f : 0.26f)),
                new Keyframe(0.45f, empowered ? 0.20f : 0.12f),
                new Keyframe(1f, 0f));

            Shader trailShader = Resources.Load<Shader>("Shaders/AbyssalWeaponTrail") ?? Shader.Find("AbyssalDungeon/WeaponTrail");
            if (trailShader != null)
            {
                ownedTrailMat = new Material(trailShader);
                Texture2D tex = LoadWeaponTex("T_Vfx_WpnRuneStreak") ?? LoadWeaponTex("T_Vfx_EnergyTrail");
                if (tex != null) ownedTrailMat.SetTexture("_MainTex", tex);
                ownedTrailMat.SetColor("_MainColor", Color.Lerp(color, Color.white, 0.55f) * 1.4f);
                ownedTrailMat.SetColor("_EdgeColor", color);
                ownedTrailMat.SetFloat("_Progress", 0.92f);
                ownedTrailMat.SetFloat("_Opacity", 1.25f);
            }
            else
            {
                ownedTrailMat = CharacterVisualFactory.MakeTransparentMat(new Color(color.r, color.g, color.b, 0.72f), color * 2f);
            }
            tr.sharedMaterial = ownedTrailMat;
            return tr;
        }

        // ==================== SHARED URP MATERIALS & HELPERS ====================

        public static Material GetSlashTrailMaterial(string matName, string texName, Color mainCol, Color edgeCol)
        {
            if (_matCache.TryGetValue(matName, out var m) && m != null)
                return m;

            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null)
            {
                _matCache[matName] = saved;
                return saved;
            }

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalWeaponTrail")
                        ?? Shader.Find("AbyssalDungeon/WeaponTrail")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh) { name = matName, enableInstancing = true };
            Texture2D tex = LoadWeaponTex(texName);
            if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_MainColor")) m.SetColor("_MainColor", mainCol);
            if (m.HasProperty("_EdgeColor")) m.SetColor("_EdgeColor", edgeCol);
            if (m.HasProperty("_Progress")) m.SetFloat("_Progress", 0.95f);
            if (m.HasProperty("_Opacity")) m.SetFloat("_Opacity", 1.35f);
            _matCache[matName] = m;
            return m;
        }

        public static Material GetWeaponAuraMaterial(
            string matName,
            string texName,
            Color coreColor,
            Color rimColor,
            Vector2 scrollSpeed,
            float intensity = 1.15f,
            float shimmerAmp = 0.006f)
        {
            if (_matCache.TryGetValue(matName, out var m) && m != null)
                return m;

            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null)
            {
                _matCache[matName] = saved;
                return saved;
            }

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalWeaponAura")
                        ?? Shader.Find("AbyssalDungeon/WeaponAura")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh) { name = matName, enableInstancing = true };
            Texture2D tex = LoadWeaponTex(texName);
            if (tex != null)
            {
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                m.mainTexture = tex;
            }
            if (m.HasProperty("_CoreColor")) m.SetColor("_CoreColor", coreColor);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", rimColor);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            if (m.HasProperty("_Opacity")) m.SetFloat("_Opacity", 0.92f);
            if (m.HasProperty("_ScrollSpeed")) m.SetVector("_ScrollSpeed", new Vector4(scrollSpeed.x, scrollSpeed.y, 0f, 0f));
            if (m.HasProperty("_ShimmerAmp")) m.SetFloat("_ShimmerAmp", shimmerAmp);
            _matCache[matName] = m;
            return m;
        }

        public static Dictionary<string, Material> BuildAllSharedWeaponMaterials()
        {
            var dict = new Dictionary<string, Material>(StringComparer.Ordinal);
            void Reg(Material mat) { if (mat != null && !dict.ContainsKey(mat.name)) dict[mat.name] = mat; }

            Reg(GetSlashTrailMaterial("M_Wpn_SlashCrescent", "T_Vfx_SlashCrescent", new Color(1.85f, 1.55f, 0.85f, 1f), new Color(0.95f, 0.52f, 0.16f, 1f)));
            Reg(GetSlashTrailMaterial("M_Wpn_HeavyCleave", "T_Vfx_WpnSerratedArc", new Color(2.05f, 1.65f, 0.72f, 1f), new Color(1.35f, 0.32f, 0.12f, 1f)));
            Reg(GetWeaponAuraMaterial("M_Wpn_SpinWhirlwind", "T_Vfx_WpnSerratedArc", new Color(1.85f, 1.55f, 0.78f, 1f), new Color(0.85f, 0.48f, 0.18f, 1f), new Vector2(-3.2f, 0f), 1.2f, 0f));
            Reg(GetWeaponAuraMaterial("M_Wpn_ThrustCone", "T_Vfx_WpnRuneStreak", new Color(1.95f, 1.68f, 0.88f, 1f), new Color(0.85f, 0.42f, 1.25f, 1f), new Vector2(-4.0f, 0.4f), 1.25f, 0f));
            Reg(GetWeaponAuraMaterial("M_Wpn_ShieldAegis", "T_Vfx_WpnAegisHex", new Color(1.95f, 1.65f, 0.72f, 1f), new Color(0.85f, 0.55f, 0.18f, 1f), Vector2.zero, 1.25f, 0.004f));
            Reg(GetWeaponAuraMaterial("M_Wpn_CrystalShards", "T_Vfx_WpnRuneStreak", new Color(1.95f, 1.55f, 0.58f, 1f), new Color(1.25f, 0.35f, 0.14f, 1f), Vector2.zero, 1.2f, 0f));
            Reg(GetWeaponAuraMaterial("M_Wpn_BladeAuraSheath", "T_Vfx_WpnRuneStreak", new Color(1.85f, 1.48f, 0.62f, 1f), new Color(0.85f, 0.42f, 0.16f, 1f), new Vector2(-1.4f, 0.25f), 1.0f, 0.006f));
            Reg(GetWeaponAuraMaterial("M_Wpn_ArrowBolt3D", "T_Vfx_WpnRuneStreak", new Color(1.95f, 1.75f, 0.95f, 1f), new Color(0.35f, 0.95f, 0.55f, 1f), new Vector2(-3.2f, 0.2f), 1.35f, 0f));
            Reg(GetWeaponAuraMaterial("M_Wpn_SpellBolt3D", "T_Vfx_WpnRuneStreak", new Color(1.95f, 1.75f, 0.95f, 1f), new Color(0.65f, 0.45f, 1.25f, 1f), new Vector2(-3.2f, 0.2f), 1.35f, 0f));
            Reg(EnvironmentVfxFactory.GetAdditiveMat("M_Wpn_PoiseShatterLines", "T_Vfx_ShatterLines", new Color(1.95f, 1.55f, 0.55f, 1f), new Color(1.35f, 0.28f, 0.12f, 1f), 1.15f, Vector2.zero, 1f));
            Reg(EnvironmentVfxFactory.GetAdditiveMat("M_Wpn_SpellNova", "T_Vfx_NovaWave", new Color(1.75f, 1.35f, 1.95f, 1f), new Color(0.68f, 0.28f, 1.45f, 1f), 1.15f, Vector2.zero, 1f));

            // Phase 2B — Bespoke Assassin Dual Blades URP Materials (Crimson + Obsidian Smoke + Shadow Purple + Venom Green)
            Reg(GetSlashTrailMaterial("M_Asn_CrimsonSlash", "T_Asn_CrimsonShadowSlash", new Color(2.15f, 0.22f, 0.36f, 1f), new Color(0.75f, 0.16f, 1.35f, 1f)));
            Reg(GetAlphaSmokeMaterial("M_Asn_ShadowSmoke", "T_Asn_ShadowSmokeWisps", new Color(0.14f, 0.08f, 0.22f, 0.88f)));
            Reg(GetWeaponAuraMaterial("M_Asn_BackstabCore", "T_Asn_BackstabSigil", new Color(2.25f, 0.24f, 0.38f, 1f), new Color(0.85f, 0.14f, 1.45f, 1f), new Vector2(0f, -1.8f), 1.38f, 0f));
            Reg(GetWeaponAuraMaterial("M_Asn_VenomSplash", "T_Asn_VenomSplatter", new Color(0.32f, 1.92f, 0.62f, 1f), new Color(0.62f, 0.18f, 1.25f, 1f), new Vector2(0f, -1.4f), 1.32f, 0.004f));
            Reg(GetWeaponAuraMaterial("M_Asn_ShadowPhantom", "T_Asn_ShadowSmokeWisps", new Color(1.18f, 0.28f, 1.95f, 1f), new Color(1.85f, 0.18f, 0.34f, 1f), new Vector2(0f, 1.8f), 1.15f, 0.006f));
            return dict;
        }

        public static Material GetAlphaSmokeMaterial(string matName, string texName, Color baseCol)
        {
            if (_matCache.TryGetValue(matName, out var m) && m != null)
                return m;

            Material saved = Resources.Load<Material>("VFX/Materials/" + matName);
            if (saved != null)
            {
                _matCache[matName] = saved;
                return saved;
            }

            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default");
            m = new Material(sh) { name = matName, enableInstancing = true };
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f); // Alpha blend so dark obsidian smoke reads cleanly!
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);
            Texture2D tex = LoadWeaponTex(texName) ?? LoadWeaponTex("T_Vfx_GothicSmoke");
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
            m.color = baseCol;
            m.renderQueue = 3012;
            _matCache[matName] = m;
            return m;
        }

        // ==================== BESPOKE ASSASSIN DUAL BLADES 3D VFX BUILDERS ====================

        private static void BuildAsnDualBladeSlash(GameObject root)
        {
            Mesh scissorMesh = LoadWeaponMesh("SM_Asn_DualBladeSlash") ?? LoadWeaponMesh("SM_Shared_Slash_Double");
            var slash = AddParticleChild(root.transform, "AsnScissorSlash3D", loop: false, maxParticles: 2, duration: 0.30f);
            var sm = slash.main;
            sm.startLifetime = 0.24f;
            sm.startSpeed = 0f;
            sm.startSize = 1.04f;
            sm.startColor = Color.white;
            SetBurst(slash, 0f, 1);
            AddSizeCurve(slash, 0.82f, 1.06f, 1.18f);
            AddAlphaFade(slash, 1.0f, 0.88f, 0f);
            var sRend = SetRenderer(slash, GetSlashTrailMaterial("M_Asn_CrimsonSlash", "T_Asn_CrimsonShadowSlash",
                new Color(2.15f, 0.22f, 0.36f, 1f), new Color(0.75f, 0.16f, 1.35f, 1f)),
                scissorMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (scissorMesh != null) { sRend.mesh = scissorMesh; sRend.alignment = ParticleSystemRenderSpace.Local; }

            // Obsidian dark-smoke wake puffs
            var smoke = AddParticleChild(root.transform, "AsnBlackSmokeWake", loop: false, maxParticles: 8, duration: 0.38f);
            var km = smoke.main;
            km.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.36f);
            km.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            km.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            km.startColor = new Color(0.12f, 0.06f, 0.20f, 0.82f);
            SetBurst(smoke, 0.02f, 5);
            var ksh = smoke.shape; ksh.shapeType = ParticleSystemShapeType.Circle; ksh.radius = 0.85f;
            AddAlphaFade(smoke, 0.78f, 0.55f, 0f);
            SetRenderer(smoke, GetAlphaSmokeMaterial("M_Asn_ShadowSmoke", "T_Asn_ShadowSmokeWisps", new Color(0.14f, 0.08f, 0.22f, 0.88f)),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAsnRapidCombo(GameObject root)
        {
            Mesh flurryMesh = LoadWeaponMesh("SM_Asn_RapidComboTrails") ?? LoadWeaponMesh("SM_Shared_Slash_MultiEnergy");
            var flurry = AddParticleChild(root.transform, "AsnRapidFlurry3D", loop: false, maxParticles: 3, duration: 0.44f);
            var fm = flurry.main;
            fm.startLifetime = 0.36f;
            fm.startSpeed = 0f;
            fm.startSize = 1.08f;
            fm.startColor = Color.white;
            SetBurst(flurry, 0f, 1);
            AddSizeCurve(flurry, 0.78f, 1.08f, 1.22f);
            AddAlphaFade(flurry, 1.0f, 0.90f, 0f);
            var fRend = SetRenderer(flurry, GetSlashTrailMaterial("M_Asn_CrimsonSlash", "T_Asn_CrimsonShadowSlash",
                new Color(2.15f, 0.22f, 0.36f, 1f), new Color(0.75f, 0.16f, 1.35f, 1f)),
                flurryMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (flurryMesh != null) { fRend.mesh = flurryMesh; fRend.alignment = ParticleSystemRenderSpace.Local; }

            AddArcSparks(root.transform, "AsnCrimsonRazorSparks", count: 12, radius: 1.25f, speed: 5.2f, VfxQualityOption.Low);
        }

        private static void BuildAsnDashAfterimage(GameObject root)
        {
            Mesh phantomMesh = LoadWeaponMesh("SM_Asn_DashAfterimage");
            var ph = AddParticleChild(root.transform, "AsnHoodedPhantom3D", loop: false, maxParticles: 2, duration: 0.42f);
            var pm = ph.main;
            pm.startLifetime = 0.34f;
            pm.startSpeed = 0f;
            pm.startSize = 1.0f;
            pm.startColor = new Color(0.88f, 0.45f, 1.0f, 0.88f);
            SetBurst(ph, 0f, 1);
            AddSizeCurve(ph, 0.96f, 1.02f, 1.08f);
            AddAlphaFade(ph, 0.92f, 0.62f, 0f);
            var pRend = SetRenderer(ph, GetWeaponAuraMaterial("M_Asn_ShadowPhantom", "T_Asn_ShadowSmokeWisps",
                new Color(1.18f, 0.28f, 1.95f, 1f), new Color(1.85f, 0.18f, 0.34f, 1f), new Vector2(0f, 1.8f), 1.15f, 0.006f),
                phantomMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (phantomMesh != null) { pRend.mesh = phantomMesh; pRend.alignment = ParticleSystemRenderSpace.Local; }

            var smoke = AddParticleChild(root.transform, "AsnDashSmokeTrail", loop: false, maxParticles: 10, duration: 0.45f);
            var sm = smoke.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.40f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.72f);
            sm.startColor = new Color(0.10f, 0.05f, 0.16f, 0.85f);
            SetBurst(smoke, 0f, 6);
            var sh = smoke.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.position = new Vector3(0f, 0.85f, 0f); sh.scale = new Vector3(0.45f, 1.1f, 0.45f);
            AddAlphaFade(smoke, 0.85f, 0.55f, 0f);
            SetRenderer(smoke, GetAlphaSmokeMaterial("M_Asn_ShadowSmoke", "T_Asn_ShadowSmokeWisps", new Color(0.14f, 0.08f, 0.22f, 0.88f)),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAsnBackstabImpact(GameObject root)
        {
            Mesh bsMesh = LoadWeaponMesh("SM_Asn_BackstabImpact") ?? LoadWeaponMesh("SM_Shared_Impact_WeaponStar");
            var core = AddParticleChild(root.transform, "AsnBackstabStilettoCross3D", loop: false, maxParticles: 2, duration: 0.48f);
            var cm = core.main;
            cm.startLifetime = 0.40f;
            cm.startSpeed = 0f;
            cm.startSize = 1.12f;
            cm.startColor = Color.white;
            SetBurst(core, 0f, 1);
            AddSizeCurve(core, 0.55f, 1.12f, 1.28f);
            AddAlphaFade(core, 1.0f, 0.85f, 0f);
            var cRend = SetRenderer(core, GetWeaponAuraMaterial("M_Asn_BackstabCore", "T_Asn_BackstabSigil",
                new Color(2.25f, 0.24f, 0.38f, 1f), new Color(0.85f, 0.14f, 1.45f, 1f), new Vector2(0f, -1.8f), 1.38f, 0f),
                bsMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (bsMesh != null) { cRend.mesh = bsMesh; cRend.alignment = ParticleSystemRenderSpace.Local; }

            AddArcSparks(root.transform, "AsnBackstabBloodSparks", count: 14, radius: 0.65f, speed: 6.2f, VfxQualityOption.Low);
        }

        private static void BuildAsnPoisonImpact(GameObject root)
        {
            Mesh venMesh = LoadWeaponMesh("SM_Asn_PoisonBladeImpact") ?? LoadWeaponMesh("SM_Shared_Impact_GroundBurst");
            var ven = AddParticleChild(root.transform, "AsnVenomFangCrown3D", loop: false, maxParticles: 2, duration: 0.58f);
            var vm = ven.main;
            vm.startLifetime = 0.48f;
            vm.startSpeed = 0f;
            vm.startSize = 1.08f;
            vm.startColor = Color.white;
            SetBurst(ven, 0f, 1);
            AddSizeCurve(ven, 0.45f, 1.05f, 1.24f);
            AddAlphaFade(ven, 1.0f, 0.85f, 0f);
            var vRend = SetRenderer(ven, GetWeaponAuraMaterial("M_Asn_VenomSplash", "T_Asn_VenomSplatter",
                new Color(0.32f, 1.92f, 0.62f, 1f), new Color(0.62f, 0.18f, 1.25f, 1f), new Vector2(0f, -1.4f), 1.32f, 0.004f),
                venMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (venMesh != null) { vRend.mesh = venMesh; vRend.alignment = ParticleSystemRenderSpace.Local; }

            // Bubbling emerald toxin droplets
            var drops = AddParticleChild(root.transform, "AsnVenomDroplets", loop: false, maxParticles: 12, duration: 0.55f);
            var dm = drops.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            dm.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
            dm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            dm.gravityModifier = 0.95f;
            dm.startColor = new Color(0.22f, 0.98f, 0.45f, 0.95f);
            SetBurst(drops, 0.02f, 10);
            var dsh = drops.shape; dsh.shapeType = ParticleSystemShapeType.Hemisphere; dsh.radius = 0.45f;
            AddAlphaFade(drops, 1.0f, 0.8f, 0f);
            SetRenderer(drops, EnvironmentVfxFactory.GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(0.35f, 1.95f, 0.65f, 1f), new Color(0.45f, 0.15f, 0.95f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Billboard);
        }

        private static void BuildAsnShadowBurst(GameObject root)
        {
            Mesh burstMesh = LoadWeaponMesh("SM_Asn_ShadowBurst");
            var burst = AddParticleChild(root.transform, "AsnShadowSickleNova3D", loop: false, maxParticles: 2, duration: 0.78f);
            var bm = burst.main;
            bm.startLifetime = 0.66f;
            bm.startSpeed = 0f;
            bm.startSize = 1.12f;
            bm.startColor = Color.white;
            SetBurst(burst, 0f, 1);
            var rot = burst.rotationOverLifetime;
            rot.enabled = true; rot.separateAxes = true; rot.y = 8.5f;
            AddSizeCurve(burst, 0.42f, 1.08f, 1.35f);
            AddAlphaFade(burst, 0.96f, 0.84f, 0f);
            var bRend = SetRenderer(burst, GetWeaponAuraMaterial("M_Asn_ShadowPhantom", "T_Asn_ShadowSmokeWisps",
                new Color(1.18f, 0.28f, 1.95f, 1f), new Color(1.85f, 0.18f, 0.34f, 1f), new Vector2(0f, 1.8f), 1.15f, 0.006f),
                burstMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard);
            if (burstMesh != null) { bRend.mesh = burstMesh; bRend.alignment = ParticleSystemRenderSpace.Local; }

            // Dense obsidian smoke ring
            var smoke = AddParticleChild(root.transform, "AsnAbyssalSmokeCloud", loop: false, maxParticles: 16, duration: 0.75f);
            var km = smoke.main;
            km.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
            km.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.6f);
            km.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
            km.startColor = new Color(0.08f, 0.04f, 0.14f, 0.90f);
            SetBurst(smoke, 0f, 12);
            var ksh = smoke.shape; ksh.shapeType = ParticleSystemShapeType.Circle; ksh.radius = 0.85f;
            AddAlphaFade(smoke, 0.90f, 0.65f, 0f);
            SetRenderer(smoke, GetAlphaSmokeMaterial("M_Asn_ShadowSmoke", "T_Asn_ShadowSmokeWisps", new Color(0.14f, 0.08f, 0.22f, 0.88f)),
                ParticleSystemRenderMode.Billboard);
        }

        private static void AddArcSparks(Transform parent, string name, short count, float radius, float speed, VfxQualityOption minTier)
        {
            var sparks = AddParticleChild(parent, name, loop: false, maxParticles: count + 4, duration: 0.45f);
            var sm = sparks.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            sm.gravityModifier = 0.85f;
            sm.startColor = new Color(1.0f, 0.88f, 0.48f, 0.95f);
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            SetBurst(sparks, 0f, count);

            var sh = sparks.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 36f;
            sh.radius = radius;

            AddAlphaFade(sparks, 1.0f, 0.78f, 0f);
            var rend = SetRenderer(sparks, EnvironmentVfxFactory.GetAdditiveMat("M_Env_EmberSpark", "T_Vfx_EmberSpark",
                new Color(1.85f, 1.35f, 0.55f, 1f), new Color(1.25f, 0.42f, 0.10f, 1f), 1.15f, Vector2.zero, 1f),
                ParticleSystemRenderMode.Stretch);
            rend.velocityScale = 0.14f;
            if (minTier != VfxQualityOption.Low)
                sparks.gameObject.AddComponent<VfxLayerTierFilter>().minQualityTier = minTier;
        }

        private static ParticleSystem AddParticleChild(Transform parent, string name, bool loop, int maxParticles, float duration)
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

        private static void SetBurst(ParticleSystem ps, float time, short count)
        {
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
        }

        private static void AddSizeCurve(ParticleSystem ps, float s0, float sMid, float sEnd)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, s0),
                new Keyframe(0.35f, sMid),
                new Keyframe(1f, sEnd)));
        }

        private static void AddAlphaFade(ParticleSystem ps, float a0, float aMid, float aEnd)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(aMid, 0.45f), new GradientAlphaKey(aEnd, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
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
    }

    /// <summary>
    /// Spins 3D arrow slipstreams and faceted spell bolt cores smoothly around their flight axis (+Z).
    /// </summary>
    public sealed class WeaponProjectileSpin3D : MonoBehaviour
    {
        public float SpinZ = 420f;
        private void Update() => transform.Rotate(0f, 0f, SpinZ * Time.deltaTime, Space.Self);
    }
}
