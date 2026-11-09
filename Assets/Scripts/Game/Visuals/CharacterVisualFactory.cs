using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Creates URP-compatible 3D characters (using Mixamo FBX models from Assets/Art/asset_3d when imported,
    /// plus custom procedural 3D meshes for Crystal Golem, Forest Monster, Bear Form, Soul Minion, 10 weapon sets,
    /// dungeon props, telegraphs, projectiles, and VFX).
    /// </summary>
    public static class CharacterVisualFactory
    {
        private static Shader _urpLit;
        private static Shader _urpParticles;
        private static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();

        public static Shader UrpLit
        {
            get
            {
                if (_urpLit == null) _urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (_urpLit == null) _urpLit = Shader.Find("Standard");
                return _urpLit;
            }
        }

        public static Shader UrpUnlit
        {
            get
            {
                if (_urpParticles == null) _urpParticles = Shader.Find("Universal Render Pipeline/Unlit");
                if (_urpParticles == null) _urpParticles = UrpLit;
                return _urpParticles;
            }
        }

        public static Texture2D LoadEnvTexture(string name)
        {
            if (_texCache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Environment/" + name);
            if (t != null) _texCache[name] = t;
            return t;
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            Sprite s = Resources.Load<Sprite>(resourcePath);
            if (s != null) return s;
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return null;
        }

        public static Material MakeMat(Color color, float smoothness = 0.35f, float metallic = 0.1f, Color emission = default, Texture mainTex = null, Vector2 tiling = default)
        {
            var m = new Material(UrpLit);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (mainTex != null)
            {
                m.mainTexture = mainTex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", mainTex);
                if (tiling != Vector2.zero)
                {
                    m.mainTextureScale = tiling;
                    if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
                }
            }
            if (emission.maxColorComponent > 0.01f)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            }
            return m;
        }

        public static Material MakeTransparentMat(Color color, Color emission = default)
        {
            var m = new Material(UrpUnlit);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            m.renderQueue = 3000;
            if (emission.maxColorComponent > 0.01f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            return m;
        }

        /// <summary>
        /// Ensures every SkinnedMeshRenderer & MeshRenderer on a character casts real 3D human shadows
        /// (with updateWhenOffscreen=true so shadow caster culling never drops the character),
        /// and attaches CharacterHumanShadowCaster for grounded foot & projected human silhouette shadows.
        /// </summary>
        public static void EnsureCharacterShadowCasters(GameObject root, Animator animator = null, float shadowScale = 1f)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (r is TrailRenderer || r is ParticleSystemRenderer || r.GetComponent<TextMesh>() != null)
                    continue;
                if (r.name.StartsWith("ShadowBlob_", System.StringComparison.Ordinal))
                    continue;

                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }

            var caster = root.GetComponent<CharacterHumanShadowCaster>();
            if (caster == null) caster = root.AddComponent<CharacterHumanShadowCaster>();
            caster.Bind(root.transform, animator, shadowScale);
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Quaternion localRot = default, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot != default) go.transform.localRotation = localRot;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            if (mat != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            return go;
        }

        /// <summary>Upgrades any Standard/Legacy materials on an imported FBX instance to URP Lit and applies optional tint.</summary>
        public static void UpgradeRenderersToUrp(GameObject root, Color tint, float tintBlend, Color emission = default)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var oldMats = r.sharedMaterials;
                var newMats = new Material[oldMats.Length];
                for (int i = 0; i < oldMats.Length; i++)
                {
                    Material src = oldMats[i];
                    Texture baseTex = null;
                    Texture bumpTex = null;
                    Color baseCol = Color.white;
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap") != null) baseTex = src.GetTexture("_BaseMap");
                        else if (src.HasProperty("_MainTex")) baseTex = src.GetTexture("_MainTex");
                        if (src.HasProperty("_BumpMap")) bumpTex = src.GetTexture("_BumpMap");
                        if (src.HasProperty("_BaseColor")) baseCol = src.GetColor("_BaseColor");
                        else if (src.HasProperty("_Color")) baseCol = src.GetColor("_Color");
                    }
                    Color finalCol = Color.Lerp(baseCol, tint, tintBlend);
                    finalCol.a = 1f;
                    Material dst = MakeMat(finalCol, 0.35f, 0.15f, emission, baseTex);
                    if (bumpTex != null && dst.HasProperty("_BumpMap"))
                    {
                        dst.SetTexture("_BumpMap", bumpTex);
                        dst.EnableKeyword("_NORMALMAP");
                    }
                    newMats[i] = dst;
                }
                r.sharedMaterials = newMats;
            }
        }

        private static readonly MaterialPropertyBlock _charMpb = new MaterialPropertyBlock();

        private static Transform FindSocket(Transform root, params string[] candidates)
        {
            if (root == null) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int c = 0; c < candidates.Length; c++)
            {
                string want = candidates[c];
                for (int i = 0; i < all.Length; i++)
                    if (string.Equals(all[i].name, want, System.StringComparison.OrdinalIgnoreCase))
                        return all[i];
            }
            for (int c = 0; c < candidates.Length; c++)
            {
                string want = candidates[c];
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name.IndexOf(want, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return all[i];
            }
            return root;
        }

        private static void ApplySubtleAccentPropertyBlock(GameObject root, Color tint, float tintWeight, Color emission)
        {
            if (root == null || (tintWeight <= 0.001f && emission.maxColorComponent <= 0.001f)) return;
            Color baseCol = Color.Lerp(Color.white, tint, Mathf.Clamp01(tintWeight));
            baseCol.a = 1f;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                string rn = r.name ?? string.Empty;
                if (rn.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rn.IndexOf("Hair", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rn.IndexOf("Skn", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                _charMpb.Clear();
                _charMpb.SetColor("_BaseColor", baseCol);
                _charMpb.SetColor("_Color", baseCol);
                if (emission.maxColorComponent > 0.01f)
                    _charMpb.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(_charMpb);
            }
        }

        public static GameObject BuildHeroVisual(ClassDef cls, Transform parent, out Animator animator, int buildIndex = 0)
        {
            Color buildAccent = ClassSpecializationSystem.GetBuildAccentColor(cls.id, buildIndex, cls.color);
            Color bodyTint = Color.Lerp(cls.color, buildAccent, 0.45f);

            GameObject exportedHeroPrefab = Resources.Load<GameObject>("Models/Heroes/Hero_" + cls.name);
            GameObject root;
            Transform rHand;
            Transform lHand;
            Transform head;

            if (exportedHeroPrefab != null)
            {
                root = Object.Instantiate(exportedHeroPrefab);
                animator = root.GetComponentInChildren<Animator>();
                if (animator != null) animator.applyRootMotion = false;
                rHand = FindSocket(root.transform, "RH_WP", "Bip001 R Hand", "RightHand");
                lHand = FindSocket(root.transform, "LH_WP", "Bip001 L Hand", "LeftHand");
                head = FindSocket(root.transform, "Bip001 Head", "HP_Head", "Head");

                if (buildIndex > 0)
                    ApplySubtleAccentPropertyBlock(root, buildAccent, 0.09f, buildAccent * 0.05f);
            }
#if UNITY_EDITOR
            else if (TryBuildEditorLiveHeroFallback(cls, out root, out animator))
            {
                rHand = FindSocket(root.transform, "RH_WP", "Bip001 R Hand", "RightHand");
                lHand = FindSocket(root.transform, "LH_WP", "Bip001 L Hand", "LeftHand");
                head = FindSocket(root.transform, "Bip001 Head", "HP_Head", "Head");
                if (buildIndex > 0)
                    ApplySubtleAccentPropertyBlock(root, buildAccent, 0.09f, buildAccent * 0.05f);
            }
#endif
            else
            {
                root = BuildBaseModel(cls.model, bodyTint, 0.30f, buildAccent * 0.14f, out animator, out rHand, out lHand, out head);
            }

            root.name = "Visual_" + cls.name;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            // Dress Dark Fantasy 3D Class Silhouette & Weapon Trails, then enforce real 3D Human Cast Shadows
            ClassVisualArmorKit.DressHeroClassRegalia(cls, buildIndex, root, animator, rHand, lHand, head);
            EnsureCharacterShadowCasters(root, animator, 1.0f);
            return root;
        }

        public static GameObject BuildMonsterVisual(EnemyDef def, bool forceEliteAura, Transform parent, out Animator animator)
        {
            Color emis = (def.archetype == Archetype.Boss || def.archetype == Archetype.Elite || forceEliteAura)
                ? def.tint * 0.35f : Color.black;

            string exportedEnemyKey = (def.archetype == Archetype.Boss || def.id == "AbyssalKnight")
                ? "Models/Enemies/Boss_AbyssalKnight"
                : ("Models/Enemies/Enemy_" + def.id);
            GameObject exportedEnemyPrefab = Resources.Load<GameObject>(exportedEnemyKey);

            GameObject root;
            Transform rHand;
            Transform lHand;
            Transform head;
            bool usingExportedRig = exportedEnemyPrefab != null;

            if (usingExportedRig)
            {
                root = Object.Instantiate(exportedEnemyPrefab);
                animator = root.GetComponentInChildren<Animator>();
                if (animator != null) animator.applyRootMotion = false;
                rHand = FindSocket(root.transform, "RH_WP", "Bip001 R Hand", "RightHand");
                lHand = FindSocket(root.transform, "LH_WP", "Bip001 L Hand", "LeftHand");
                head = FindSocket(root.transform, "Bip001 Head", "HP_Head", "Head");

                if (forceEliteAura || def.archetype == Archetype.Elite || def.archetype == Archetype.Boss)
                    ApplySubtleAccentPropertyBlock(root, def.tint, 0.16f, emis * 0.35f);
            }
            else
            {
                root = BuildBaseModel(def.model, def.tint, 0.62f, emis, out animator, out rHand, out lHand, out head);
            }

            root.name = "Visual_" + def.id;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * def.scale;

            if (!usingExportedRig)
            {
                if (def.weapon != WeaponKind.None && def.archetype != Archetype.Boss)
                    AttachWeapon(def.weapon, def.tint, rHand, lHand, root.transform);
                if (!string.IsNullOrEmpty(def.accessory))
                    AttachMonsterAccessory(def.accessory, def.tint, head, root.transform);
            }

            // Dress Elite/Boss trails, floating Elite Crown Banner & hit-recoil polish
            EnemyVisualArmorKit.DressEnemyRegalia(def, forceEliteAura || def.archetype == Archetype.Elite, def.archetype == Archetype.Boss, root, animator, rHand, lHand, head);
            EnsureCharacterShadowCasters(root, animator, Mathf.Clamp(def.scale, 0.85f, 1.85f));
            return root;
        }

        private static GameObject BuildBaseModel(ModelKind kind, Color tint, float tintBlend, Color emission,
            out Animator animator, out Transform rHand, out Transform lHand, out Transform head)
        {
            animator = null;
            rHand = null;
            lHand = null;
            head = null;

            if (kind == ModelKind.Golem)
            {
                var go = BuildCrystalGolemMesh(tint);
                rHand = go.transform.Find("ArmR");
                lHand = go.transform.Find("ArmL");
                head = go.transform.Find("Head");
                return go;
            }
            if (kind == ModelKind.Treant)
            {
                var go = BuildForestTreantMesh(tint);
                rHand = go.transform.Find("ArmR");
                lHand = go.transform.Find("ArmL");
                head = go.transform.Find("Head");
                return go;
            }

            string resKey = kind == ModelKind.Paladin ? "Models/Paladin" :
                            kind == ModelKind.Vanguard ? "Models/Vanguard" : "Models/Kachujin";

            GameObject prefab = Resources.Load<GameObject>(resKey);
            if (prefab != null)
            {
                GameObject inst = Object.Instantiate(prefab);
                UpgradeRenderersToUrp(inst, tint, tintBlend, emission);
                animator = inst.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (animator.runtimeAnimatorController == null)
                    {
                        var ctrl = Resources.Load<RuntimeAnimatorController>("Animators/HeroAnimator");
                        if (ctrl != null) animator.runtimeAnimatorController = ctrl;
                    }
                    animator.applyRootMotion = false;
                    if (animator.isHuman)
                    {
                        rHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                        lHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        head = animator.GetBoneTransform(HumanBodyBones.Head);
                    }
                }
                if (rHand == null) rHand = FindDeep(inst.transform, "RightHand");
                if (lHand == null) lHand = FindDeep(inst.transform, "LeftHand");
                if (head == null) head = FindDeep(inst.transform, "Head");
                return inst;
            }

            // Articulated procedural armored humanoid fallback
            var fallback = BuildArmoredHumanoidMesh(tint, emission);
            rHand = fallback.transform.Find("ArmR/HandR");
            lHand = fallback.transform.Find("ArmL/HandL");
            head = fallback.transform.Find("Head");
            return fallback;
        }

        private static Transform FindDeep(Transform root, string partial)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf(partial, System.StringComparison.OrdinalIgnoreCase) >= 0) return t;
            return root;
        }

#if UNITY_EDITOR
        private static bool TryBuildEditorLiveHeroFallback(ClassDef cls, out GameObject root, out Animator animator)
        {
            root = null;
            animator = null;
            if (cls == null) return false;

            string rigPath = $"Assets/Art/Exported/Characters/HeroRig_{cls.name}.prefab";
            GameObject srcRig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (srcRig == null) return false;

            root = new GameObject("Hero_" + cls.name);
            var rigInst = Object.Instantiate(srcRig);
            rigInst.name = "Rig";
            foreach (var col in rigInst.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);
            rigInst.transform.SetParent(root.transform, false);

            animator = rigInst.GetComponent<Animator>();
            if (animator == null) animator = rigInst.AddComponent<Animator>();
            var ctrl = Resources.Load<RuntimeAnimatorController>("Animators/HeroAnimator_" + cls.name)
                    ?? Resources.Load<RuntimeAnimatorController>("Animators/HeroAnimator");
            if (ctrl != null) animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            string rWpn = null, lWpn = null, bookWpn = null;
            switch (cls.name)
            {
                case "Knight":    rWpn = "Wpn_KnightSword";         lWpn = "Wpn_KnightShield"; break;
                case "Guardian":  rWpn = "Wpn_GuardianMace";        lWpn = "Wpn_GuardianShield"; break;
                case "Berserker": rWpn = "Wpn_BerserkerGreatsword"; break;
                case "Assassin":  rWpn = "Wpn_AssassinBladeR";      lWpn = "Wpn_AssassinBladeL"; break;
                case "Ranger":    lWpn = "Wpn_RangerBow"; break;
                case "Mage":      rWpn = "Wpn_MageStaff"; break;
                case "Warlock":   rWpn = "Wpn_WarlockSword";        bookWpn = "Wpn_WarlockBook"; break;
                case "Druid":     rWpn = "Wpn_DruidStaff"; break;
                case "Cleric":    rWpn = "Wpn_ClericHammer";        lWpn = "Wpn_ClericShield"; break;
            }

            AttachEditorWeaponFallback(rigInst.transform, "RH_WP", rWpn, "SignatureMainWeapon");
            AttachEditorWeaponFallback(rigInst.transform, "LH_WP", lWpn, "SignatureOffhand");
            if (!string.IsNullOrEmpty(bookWpn))
            {
                var bSrc = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Exported/Weapons/{bookWpn}.prefab");
                if (bSrc != null)
                {
                    var bInst = Object.Instantiate(bSrc);
                    bInst.name = "WarlockFloatingGrimoire";
                    bInst.transform.SetParent(root.transform, false);
                    bInst.transform.localPosition = new Vector3(-0.50f, 1.42f, 0.22f);
                    bInst.transform.localRotation = Quaternion.Euler(18f, -24f, 0f);
                }
            }
            return true;
        }

        private static void AttachEditorWeaponFallback(Transform rigRoot, string socketName, string wpnName, string instName)
        {
            if (string.IsNullOrEmpty(wpnName)) return;
            var wSrc = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Exported/Weapons/{wpnName}.prefab");
            if (wSrc == null) return;
            Transform socket = FindSocket(rigRoot, socketName, socketName.StartsWith("R") ? "Bip001 R Hand" : "Bip001 L Hand");
            var wInst = Object.Instantiate(wSrc);
            wInst.name = instName;
            foreach (var col in wInst.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);
            Quaternion rot = wInst.transform.localRotation;
            Vector3 pos = wInst.transform.localPosition;
            Vector3 scl = wInst.transform.localScale;
            wInst.transform.SetParent(socket, false);
            wInst.transform.localRotation = rot;
            wInst.transform.localScale = scl;
            wInst.transform.localPosition = pos.sqrMagnitude > 0.09f ? Vector3.zero : pos;
        }
#endif

        public static GameObject BuildBearFormMesh()
        {
            GameObject beastPrefab = Resources.Load<GameObject>("Models/Heroes/Hero_DruidBeast");
            if (beastPrefab != null)
            {
                var beast = Object.Instantiate(beastPrefab);
                beast.name = "BearForm";
                ApplySubtleAccentPropertyBlock(beast, new Color(0.28f, 0.95f, 0.55f), 0.18f, new Color(0.08f, 0.32f, 0.16f));
                EnsureCharacterShadowCasters(beast, beast.GetComponentInChildren<Animator>(), 1.25f);
                return beast;
            }

            var root = new GameObject("BearForm");
            var fur = MakeMat(new Color(0.38f, 0.24f, 0.14f), 0.25f, 0.05f);
            var armor = MakeMat(new Color(0.2f, 0.7f, 0.45f), 0.6f, 0.4f, new Color(0.1f, 0.5f, 0.25f));
            var eye = MakeMat(new Color(0.3f, 1f, 0.5f), 0.8f, 0f, new Color(0.3f, 1f, 0.5f) * 2f);

            Prim(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.85f, 0), new Vector3(1.15f, 1.1f, 1.55f), fur, Quaternion.Euler(90, 0, 0));
            var hd = Prim(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.15f, 0.95f), new Vector3(0.75f, 0.7f, 0.8f), fur);
            Prim(PrimitiveType.Cube, "Snout", hd.transform, new Vector3(0, -0.1f, 0.5f), new Vector3(0.45f, 0.35f, 0.5f), fur);
            Prim(PrimitiveType.Sphere, "EarL", hd.transform, new Vector3(-0.35f, 0.42f, 0), Vector3.one * 0.25f, fur);
            Prim(PrimitiveType.Sphere, "EarR", hd.transform, new Vector3(0.35f, 0.42f, 0), Vector3.one * 0.25f, fur);
            Prim(PrimitiveType.Sphere, "EyeL", hd.transform, new Vector3(-0.2f, 0.12f, 0.42f), Vector3.one * 0.12f, eye);
            Prim(PrimitiveType.Sphere, "EyeR", hd.transform, new Vector3(0.2f, 0.12f, 0.42f), Vector3.one * 0.12f, eye);
            Prim(PrimitiveType.Cube, "RuneCrest", root.transform, new Vector3(0, 1.38f, 0.1f), new Vector3(0.95f, 0.18f, 0.7f), armor);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.42f : 0.42f;
                float z = (i < 2) ? 0.55f : -0.55f;
                Prim(PrimitiveType.Cylinder, "Leg" + i, root.transform, new Vector3(x, 0.4f, z), new Vector3(0.32f, 0.42f, 0.32f), fur);
            }
            return root;
        }

        public static GameObject BuildSoulMinionMesh()
        {
            GameObject minionPrefab = Resources.Load<GameObject>("Models/Enemies/Enemy_SoulMinion");
            if (minionPrefab != null)
            {
                var minion = Object.Instantiate(minionPrefab);
                minion.name = "SoulMinion";
                ApplySubtleAccentPropertyBlock(minion, new Color(0.68f, 0.32f, 1.0f), 0.22f, new Color(0.32f, 0.10f, 0.55f));
                return minion;
            }

            var root = new GameObject("SoulMinion");
            var bodyMat = MakeMat(new Color(0.28f, 0.08f, 0.45f), 0.5f, 0.2f, new Color(0.45f, 0.1f, 0.8f));
            var coreMat = MakeMat(new Color(0.75f, 0.35f, 1f), 0.9f, 0f, new Color(0.75f, 0.35f, 1f) * 2.5f);
            Prim(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0, 1.05f, 0), new Vector3(0.65f, 0.75f, 0.65f), bodyMat);
            Prim(PrimitiveType.Sphere, "Eye", root.transform, new Vector3(0, 1.12f, 0.28f), Vector3.one * 0.22f, coreMat);
            Prim(PrimitiveType.Cube, "HornL", root.transform, new Vector3(-0.25f, 1.45f, 0), new Vector3(0.1f, 0.35f, 0.1f), coreMat, Quaternion.Euler(0, 0, 22));
            Prim(PrimitiveType.Cube, "HornR", root.transform, new Vector3(0.25f, 1.45f, 0), new Vector3(0.1f, 0.35f, 0.1f), coreMat, Quaternion.Euler(0, 0, -22));
            return root;
        }

        private static GameObject BuildCrystalGolemMesh(Color tint)
        {
            var root = new GameObject("CrystalGolem");
            var stone = MakeMat(new Color(0.26f, 0.28f, 0.34f), 0.25f, 0.3f);
            var crystal = MakeMat(tint, 0.85f, 0.2f, tint * 1.8f);

            Prim(PrimitiveType.Cube, "Torso", root.transform, new Vector3(0, 1.45f, 0), new Vector3(1.35f, 1.25f, 0.95f), stone, Quaternion.Euler(4, 0, 0));
            Prim(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0, 1.5f, 0.42f), new Vector3(0.45f, 0.55f, 0.3f), crystal);
            var head = Prim(PrimitiveType.Cube, "Head", root.transform, new Vector3(0, 2.3f, 0.1f), new Vector3(0.65f, 0.55f, 0.65f), stone);
            Prim(PrimitiveType.Sphere, "Visor", head.transform, new Vector3(0, 0.05f, 0.48f), new Vector3(0.55f, 0.2f, 0.2f), crystal);

            var armR = Prim(PrimitiveType.Cube, "ArmR", root.transform, new Vector3(0.98f, 1.3f, 0.15f), new Vector3(0.52f, 1.25f, 0.52f), stone);
            var armL = Prim(PrimitiveType.Cube, "ArmL", root.transform, new Vector3(-0.98f, 1.3f, 0.15f), new Vector3(0.52f, 1.25f, 0.52f), stone);
            Prim(PrimitiveType.Cube, "CrystalFistR", armR.transform, new Vector3(0, -0.45f, 0.1f), new Vector3(0.8f, 0.45f, 0.8f), crystal);
            Prim(PrimitiveType.Cube, "CrystalFistL", armL.transform, new Vector3(0, -0.45f, 0.1f), new Vector3(0.8f, 0.45f, 0.8f), crystal);

            Prim(PrimitiveType.Cube, "LegR", root.transform, new Vector3(0.38f, 0.5f, 0), new Vector3(0.48f, 0.95f, 0.52f), stone);
            Prim(PrimitiveType.Cube, "LegL", root.transform, new Vector3(-0.38f, 0.5f, 0), new Vector3(0.48f, 0.95f, 0.52f), stone);

            // Crystal shards on shoulders/back
            Prim(PrimitiveType.Cube, "Shard1", root.transform, new Vector3(0.55f, 2.25f, -0.15f), new Vector3(0.25f, 0.75f, 0.25f), crystal, Quaternion.Euler(-18, 15, -22));
            Prim(PrimitiveType.Cube, "Shard2", root.transform, new Vector3(-0.55f, 2.25f, -0.15f), new Vector3(0.25f, 0.75f, 0.25f), crystal, Quaternion.Euler(-18, -15, 22));
            Prim(PrimitiveType.Cube, "Shard3", root.transform, new Vector3(0f, 2.35f, -0.35f), new Vector3(0.28f, 0.85f, 0.28f), crystal, Quaternion.Euler(-28, 0, 0));
            return root;
        }

        private static GameObject BuildForestTreantMesh(Color tint)
        {
            var root = new GameObject("ForestMonster");
            var bark = MakeMat(new Color(0.28f, 0.20f, 0.12f), 0.15f, 0f);
            var moss = MakeMat(tint, 0.35f, 0f, new Color(0.15f, 0.45f, 0.1f));
            var glow = MakeMat(new Color(0.5f, 1f, 0.3f), 0.8f, 0f, new Color(0.4f, 1f, 0.2f) * 2f);

            Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0, 1.15f, 0), new Vector3(0.9f, 0.95f, 0.85f), bark);
            var head = Prim(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 2.2f, 0.08f), new Vector3(1.15f, 0.95f, 1.05f), moss);
            Prim(PrimitiveType.Sphere, "EyeL", head.transform, new Vector3(-0.18f, -0.1f, 0.45f), Vector3.one * 0.14f, glow);
            Prim(PrimitiveType.Sphere, "EyeR", head.transform, new Vector3(0.18f, -0.1f, 0.45f), Vector3.one * 0.14f, glow);
            Prim(PrimitiveType.Cylinder, "BranchL", head.transform, new Vector3(-0.42f, 0.45f, 0), new Vector3(0.1f, 0.45f, 0.1f), bark, Quaternion.Euler(0, 0, 28));
            Prim(PrimitiveType.Cylinder, "BranchR", head.transform, new Vector3(0.42f, 0.45f, 0), new Vector3(0.1f, 0.45f, 0.1f), bark, Quaternion.Euler(0, 0, -28));

            Prim(PrimitiveType.Cylinder, "ArmR", root.transform, new Vector3(0.72f, 1.25f, 0.2f), new Vector3(0.26f, 0.65f, 0.26f), bark, Quaternion.Euler(22, 0, -18));
            Prim(PrimitiveType.Cylinder, "ArmL", root.transform, new Vector3(-0.72f, 1.25f, 0.2f), new Vector3(0.26f, 0.65f, 0.26f), bark, Quaternion.Euler(22, 0, 18));
            Prim(PrimitiveType.Cylinder, "RootR", root.transform, new Vector3(0.28f, 0.35f, 0), new Vector3(0.32f, 0.4f, 0.32f), bark);
            Prim(PrimitiveType.Cylinder, "RootL", root.transform, new Vector3(-0.28f, 0.35f, 0), new Vector3(0.32f, 0.4f, 0.32f), bark);
            return root;
        }

        private static GameObject BuildArmoredHumanoidMesh(Color color, Color emission)
        {
            var root = new GameObject("HumanoidFallback");
            var armor = MakeMat(color, 0.55f, 0.35f, emission);
            var dark = MakeMat(new Color(0.16f, 0.16f, 0.2f), 0.3f, 0.2f);
            var skin = MakeMat(new Color(0.85f, 0.72f, 0.62f), 0.25f, 0f);

            Prim(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 1.12f, 0), new Vector3(0.58f, 0.52f, 0.38f), armor);
            var head = Prim(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.72f, 0), Vector3.one * 0.36f, skin);
            Prim(PrimitiveType.Sphere, "Helm", head.transform, new Vector3(0, 0.12f, -0.04f), new Vector3(1.08f, 0.95f, 1.08f), armor);
            Prim(PrimitiveType.Sphere, "PauldronR", root.transform, new Vector3(0.38f, 1.42f, 0), new Vector3(0.28f, 0.22f, 0.28f), armor);
            Prim(PrimitiveType.Sphere, "PauldronL", root.transform, new Vector3(-0.38f, 1.42f, 0), new Vector3(0.28f, 0.22f, 0.28f), armor);

            var armR = new GameObject("ArmR"); armR.transform.SetParent(root.transform, false); armR.transform.localPosition = new Vector3(0.42f, 1.32f, 0);
            Prim(PrimitiveType.Capsule, "Limb", armR.transform, new Vector3(0, -0.26f, 0), new Vector3(0.16f, 0.28f, 0.16f), dark);
            var handR = new GameObject("HandR"); handR.transform.SetParent(armR.transform, false); handR.transform.localPosition = new Vector3(0, -0.52f, 0.08f);

            var armL = new GameObject("ArmL"); armL.transform.SetParent(root.transform, false); armL.transform.localPosition = new Vector3(-0.42f, 1.32f, 0);
            Prim(PrimitiveType.Capsule, "Limb", armL.transform, new Vector3(0, -0.26f, 0), new Vector3(0.16f, 0.28f, 0.16f), dark);
            var handL = new GameObject("HandL"); handL.transform.SetParent(armL.transform, false); handL.transform.localPosition = new Vector3(0, -0.52f, 0.08f);

            Prim(PrimitiveType.Capsule, "LegR", root.transform, new Vector3(0.16f, 0.42f, 0), new Vector3(0.2f, 0.42f, 0.2f), dark);
            Prim(PrimitiveType.Capsule, "LegL", root.transform, new Vector3(-0.16f, 0.42f, 0), new Vector3(0.2f, 0.42f, 0.2f), dark);
            return root;
        }

        public static void AttachWeapon(WeaponKind kind, Color accent, Transform rHand, Transform lHand, Transform root)
        {
            Material steel = MakeMat(new Color(0.78f, 0.82f, 0.88f), 0.75f, 0.85f);
            Material gold = MakeMat(new Color(0.95f, 0.78f, 0.25f), 0.7f, 0.85f);
            Material wood = MakeMat(new Color(0.38f, 0.24f, 0.12f), 0.25f, 0f);
            Material glow = MakeMat(accent, 0.8f, 0.2f, accent * 1.6f);

            Transform r = rHand != null ? rHand : root;
            Transform l = lHand != null ? lHand : root;
            float s = (rHand != null && rHand != root) ? 1f : 1f;

            switch (kind)
            {
                case WeaponKind.SwordShield:
                    {
                        var w = new GameObject("Sword"); w.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cube, "Blade", w.transform, new Vector3(0, 0.42f, 0), new Vector3(0.06f, 0.78f, 0.02f) * s, steel);
                        Prim(PrimitiveType.Cube, "Guard", w.transform, new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.04f, 0.05f) * s, gold);
                        WeaponBladeTrail3D.Attach(w.transform, new Vector3(0f, 0.78f, 0f), 0.34f, accent);
                        var sh = new GameObject("Shield"); sh.transform.SetParent(l, false);
                        Prim(PrimitiveType.Cube, "Plate", sh.transform, new Vector3(-0.08f, 0.05f, 0.06f), new Vector3(0.06f, 0.58f, 0.44f) * s, steel);
                        Prim(PrimitiveType.Cube, "Crest", sh.transform, new Vector3(-0.12f, 0.05f, 0.06f), new Vector3(0.03f, 0.26f, 0.22f) * s, glow);
                        break;
                    }
                case WeaponKind.TowerShield:
                    {
                        var w = new GameObject("Hammer"); w.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cylinder, "Shaft", w.transform, new Vector3(0, 0.28f, 0), new Vector3(0.04f, 0.32f, 0.04f), wood);
                        Prim(PrimitiveType.Cube, "Head", w.transform, new Vector3(0, 0.58f, 0), new Vector3(0.18f, 0.16f, 0.26f), gold);
                        WeaponBladeTrail3D.Attach(w.transform, new Vector3(0f, 0.62f, 0f), 0.36f, accent);
                        var sh = new GameObject("TowerShield"); sh.transform.SetParent(l, false);
                        Prim(PrimitiveType.Cube, "Tower", sh.transform, new Vector3(-0.1f, 0.05f, 0.08f), new Vector3(0.08f, 0.88f, 0.56f), gold);
                        Prim(PrimitiveType.Cube, "Core", sh.transform, new Vector3(-0.15f, 0.05f, 0.08f), new Vector3(0.03f, 0.45f, 0.25f), glow);
                        break;
                    }
                case WeaponKind.GreatSword:
                    {
                        var w = new GameObject("GreatSword"); w.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cube, "Blade", w.transform, new Vector3(0, 0.68f, 0), new Vector3(0.14f, 1.25f, 0.03f), steel);
                        Prim(PrimitiveType.Cube, "Edge", w.transform, new Vector3(0, 0.68f, 0), new Vector3(0.05f, 1.15f, 0.04f), glow);
                        Prim(PrimitiveType.Cube, "Guard", w.transform, new Vector3(0, 0.06f, 0), new Vector3(0.34f, 0.06f, 0.07f), gold);
                        WeaponBladeTrail3D.Attach(w.transform, new Vector3(0f, 1.25f, 0f), 0.52f, accent);
                        break;
                    }
                case WeaponKind.Daggers:
                    {
                        var dr = Prim(PrimitiveType.Cube, "DaggerR", r, new Vector3(0, 0.22f, 0.05f), new Vector3(0.04f, 0.42f, 0.02f), glow);
                        var dl = Prim(PrimitiveType.Cube, "DaggerL", l, new Vector3(0, 0.22f, 0.05f), new Vector3(0.04f, 0.42f, 0.02f), glow);
                        WeaponBladeTrail3D.Attach(dr.transform, new Vector3(0f, 0.5f, 0f), 0.24f, accent);
                        WeaponBladeTrail3D.Attach(dl.transform, new Vector3(0f, 0.5f, 0f), 0.24f, accent);
                        break;
                    }
                case WeaponKind.Bow:
                    {
                        var b = new GameObject("Bow"); b.transform.SetParent(l, false);
                        Prim(PrimitiveType.Cylinder, "LimbTop", b.transform, new Vector3(0, 0.28f, 0.08f), new Vector3(0.04f, 0.34f, 0.04f), wood, Quaternion.Euler(-16, 0, 0));
                        Prim(PrimitiveType.Cylinder, "LimbBot", b.transform, new Vector3(0, -0.28f, 0.08f), new Vector3(0.04f, 0.34f, 0.04f), wood, Quaternion.Euler(16, 0, 0));
                        Prim(PrimitiveType.Cylinder, "String", b.transform, new Vector3(0, 0, -0.02f), new Vector3(0.012f, 0.55f, 0.012f), glow);
                        break;
                    }
                case WeaponKind.Staff:
                    {
                        var st = new GameObject("Staff"); st.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cylinder, "Pole", st.transform, new Vector3(0, 0.45f, 0), new Vector3(0.05f, 0.78f, 0.05f), wood);
                        Prim(PrimitiveType.Sphere, "Orb", st.transform, new Vector3(0, 1.28f, 0), Vector3.one * 0.24f, glow);
                        WeaponBladeTrail3D.Attach(st.transform, new Vector3(0f, 1.28f, 0f), 0.26f, accent);
                        break;
                    }
                case WeaponKind.Wand:
                    {
                        var wn = new GameObject("Wand"); wn.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cylinder, "Rod", wn.transform, new Vector3(0, 0.28f, 0), new Vector3(0.04f, 0.36f, 0.04f), MakeMat(new Color(0.15f, 0.1f, 0.2f), 0.5f, 0.3f));
                        Prim(PrimitiveType.Sphere, "SkullOrb", wn.transform, new Vector3(0, 0.66f, 0), Vector3.one * 0.18f, glow);
                        WeaponBladeTrail3D.Attach(wn.transform, new Vector3(0f, 0.66f, 0f), 0.22f, accent);
                        break;
                    }
                case WeaponKind.Fists:
                    {
                        var fr = Prim(PrimitiveType.Sphere, "ChiFistR", r, Vector3.zero, Vector3.one * 0.22f, glow);
                        var fl = Prim(PrimitiveType.Sphere, "ChiFistL", l, Vector3.zero, Vector3.one * 0.22f, glow);
                        WeaponBladeTrail3D.Attach(fr.transform, Vector3.zero, 0.25f, accent);
                        WeaponBladeTrail3D.Attach(fl.transform, Vector3.zero, 0.25f, accent);
                        break;
                    }
                case WeaponKind.Mace:
                    {
                        var mc = new GameObject("HolyMace"); mc.transform.SetParent(r, false);
                        Prim(PrimitiveType.Cylinder, "Handle", mc.transform, new Vector3(0, 0.32f, 0), new Vector3(0.045f, 0.38f, 0.045f), gold);
                        Prim(PrimitiveType.Sphere, "SunHead", mc.transform, new Vector3(0, 0.74f, 0), Vector3.one * 0.24f, glow);
                        WeaponBladeTrail3D.Attach(mc.transform, new Vector3(0f, 0.74f, 0f), 0.30f, accent);
                        break;
                    }
                case WeaponKind.Claws:
                    {
                        var cr = Prim(PrimitiveType.Cube, "ClawR", r, new Vector3(0, 0.16f, 0.05f), new Vector3(0.08f, 0.32f, 0.04f), glow);
                        var cl = Prim(PrimitiveType.Cube, "ClawL", l, new Vector3(0, 0.16f, 0.05f), new Vector3(0.08f, 0.32f, 0.04f), glow);
                        WeaponBladeTrail3D.Attach(cr.transform, new Vector3(0f, 0.45f, 0f), 0.24f, accent);
                        WeaponBladeTrail3D.Attach(cl.transform, new Vector3(0f, 0.45f, 0f), 0.24f, accent);
                        break;
                    }
            }
        }

        private static void AttachMonsterAccessory(string acc, Color tint, Transform head, Transform root)
        {
            Transform h = head != null ? head : root;
            switch (acc)
            {
                case "ears":
                    var earMat = MakeMat(tint, 0.2f, 0f);
                    Prim(PrimitiveType.Cube, "EarL", h, new Vector3(-0.16f, 0.06f, 0), new Vector3(0.22f, 0.06f, 0.07f), earMat, Quaternion.Euler(0, 0, 22));
                    Prim(PrimitiveType.Cube, "EarR", h, new Vector3(0.16f, 0.06f, 0), new Vector3(0.22f, 0.06f, 0.07f), earMat, Quaternion.Euler(0, 0, -22));
                    break;
                case "skull":
                    var boneMat = MakeMat(new Color(0.92f, 0.9f, 0.82f), 0.25f, 0f);
                    Prim(PrimitiveType.Sphere, "SkullMask", h, new Vector3(0, 0.04f, 0.03f), Vector3.one * 0.26f, boneMat);
                    break;
                case "horns":
                    var hornMat = MakeMat(new Color(0.15f, 0.05f, 0.05f), 0.5f, 0.3f, new Color(0.6f, 0.1f, 0.05f));
                    Prim(PrimitiveType.Cube, "HornL", h, new Vector3(-0.12f, 0.2f, 0.02f), new Vector3(0.06f, 0.32f, 0.06f), hornMat, Quaternion.Euler(-15, 0, 25));
                    Prim(PrimitiveType.Cube, "HornR", h, new Vector3(0.12f, 0.2f, 0.02f), new Vector3(0.06f, 0.32f, 0.06f), hornMat, Quaternion.Euler(-15, 0, -25));
                    break;
                case "eyes":
                case "crown":
                    var redGlow = MakeMat(new Color(1f, 0.15f, 0.15f), 0.9f, 0f, new Color(1f, 0.1f, 0.1f) * 3f);
                    Prim(PrimitiveType.Sphere, "EyeGlowL", h, new Vector3(-0.05f, 0.05f, 0.13f), Vector3.one * 0.045f, redGlow);
                    Prim(PrimitiveType.Sphere, "EyeGlowR", h, new Vector3(0.05f, 0.05f, 0.13f), Vector3.one * 0.045f, redGlow);
                    if (acc == "crown")
                    {
                        var crownMat = MakeMat(new Color(0.2f, 0.05f, 0.3f), 0.7f, 0.8f, new Color(0.7f, 0.1f, 0.9f) * 1.5f);
                        Prim(PrimitiveType.Cylinder, "Crown", h, new Vector3(0, 0.18f, 0), new Vector3(0.24f, 0.06f, 0.24f), crownMat);
                        for (int i = 0; i < 4; i++)
                        {
                            float a = i * 90f * Mathf.Deg2Rad;
                            Prim(PrimitiveType.Cube, "Spike" + i, h, new Vector3(Mathf.Cos(a) * 0.11f, 0.28f, Mathf.Sin(a) * 0.11f), new Vector3(0.04f, 0.16f, 0.04f), redGlow);
                        }
                    }
                    break;
            }
        }

        // ---------------- Runtime VFX, Telegraphs & Floating Text ----------------

        public static void SpawnBurst(Vector3 pos, Color color, float radius = 1.6f, float duration = 0.45f)
        {
            var fx = GameObjectPool.GetBurst(pos);
            fx.Init(color, radius, duration);
        }

        public static void SpawnGroundTelegraph(Vector3 pos, float radius, float delay, Color color, System.Action onDetonate, Vector3 boxSize = default, Quaternion rot = default)
        {
            var go = new GameObject("GroundTelegraph");
            go.transform.position = new Vector3(pos.x, 0.05f, pos.z);
            if (rot != default) go.transform.rotation = rot;
            var tg = go.AddComponent<GroundTelegraphFx>();
            tg.Init(radius, delay, color, onDetonate, boxSize);
        }

        public static void SpawnFloatingText(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            Vector3 p = worldPos + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 0.2f, UnityEngine.Random.Range(-0.35f, 0.35f));
            var ft = GameObjectPool.GetFloatingText(p);
            ft.Init(text, color, scale);
        }
    }

    public class ExpandingBurstFx : MonoBehaviour
    {
        private Transform _sphere;
        private Material _mat;
        private Color _col;
        private float _radius;
        private float _dur;
        private float _t;

        public void Init(Color col, float radius, float dur)
        {
            _col = col;
            _radius = radius;
            _dur = Mathf.Max(0.1f, dur);
            _t = 0f;
            if (_mat == null)
                _mat = CharacterVisualFactory.MakeTransparentMat(new Color(col.r, col.g, col.b, 0.55f), col);
            else
            {
                _mat.color = new Color(col.r, col.g, col.b, 0.55f);
                if (_mat.HasProperty("_EmissionColor")) _mat.SetColor("_EmissionColor", col);
            }
            if (_sphere == null)
                _sphere = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Burst", transform, Vector3.zero, Vector3.one * 0.2f, _mat).transform;
            else
                _sphere.localScale = Vector3.one * 0.2f;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _dur);
            float s = Mathf.Lerp(0.25f, _radius * 2f, Mathf.Sqrt(u));
            if (_sphere != null) _sphere.localScale = new Vector3(s, s * 0.55f, s);
            if (_mat != null)
            {
                Color c = new Color(_col.r, _col.g, _col.b, (1f - u) * 0.55f);
                _mat.color = c;
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            }
            if (_t >= _dur)
            {
                GameObjectPool.ReleaseBurst(this);
            }
        }
    }

    public class GroundTelegraphFx : MonoBehaviour
    {
        private Transform _outer;
        private Transform _inner;
        private Material _mOuter, _mInner;
        private float _radius, _delay, _t;
        private Vector3 _box;
        private System.Action _cb;

        public static readonly List<GroundTelegraphFx> ActiveHazards = new List<GroundTelegraphFx>();
        public float HazardRadius => _box != Vector3.zero ? Mathf.Max(_box.x, _box.z) * 0.5f : _radius;

        public void Init(float radius, float delay, Color color, System.Action cb, Vector3 box)
        {
            _radius = radius;
            _delay = Mathf.Max(0.15f, delay);
            _cb = cb;
            _box = box;
            _mOuter = CharacterVisualFactory.MakeTransparentMat(new Color(color.r, color.g, color.b, 0.25f));
            _mInner = CharacterVisualFactory.MakeTransparentMat(new Color(color.r, color.g, color.b, 0.58f), color);

            if (box != Vector3.zero)
            {
                _outer = CharacterVisualFactory.Prim(PrimitiveType.Cube, "Outer", transform, new Vector3(0, 0, box.z * 0.5f), new Vector3(box.x, 0.02f, box.z), _mOuter).transform;
                _inner = CharacterVisualFactory.Prim(PrimitiveType.Cube, "Inner", transform, new Vector3(0, 0.01f, box.z * 0.5f), new Vector3(box.x, 0.025f, 0.1f), _mInner).transform;
            }
            else
            {
                _outer = CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "Outer", transform, Vector3.zero, new Vector3(radius * 2f, 0.01f, radius * 2f), _mOuter).transform;
                _inner = CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "Inner", transform, new Vector3(0, 0.01f, 0), new Vector3(0.1f, 0.015f, 0.1f), _mInner).transform;
            }
            ActiveHazards.Add(this);
        }

        private void OnDestroy()
        {
            ActiveHazards.Remove(this);
            if (_mOuter != null) Destroy(_mOuter);
            if (_mInner != null) Destroy(_mInner);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _delay);
            if (_inner != null)
            {
                if (_box != Vector3.zero)
                    _inner.localScale = new Vector3(_box.x, 0.025f, Mathf.Lerp(0.1f, _box.z, u));
                else
                {
                    float d = Mathf.Lerp(0.1f, _radius * 2f, u);
                    _inner.localScale = new Vector3(d, 0.015f, d);
                }
            }
            if (_t >= _delay)
            {
                var cb = _cb;
                _cb = null;
                cb?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    public class FloatingCombatText : MonoBehaviour
    {
        private TextMesh _tm;
        private Color _col;
        private float _t;
        private float _baseScale = 1f;
        private bool _emphatic;
        private Vector3 _vel;

        public void Init(string txt, Color col, float scale)
        {
            _col = col;
            _t = 0f;
            _baseScale = scale;
            _emphatic = scale >= 1.12f || (txt != null && txt.IndexOf('!') >= 0);
            _vel = new Vector3(UnityEngine.Random.Range(-0.85f, 0.85f), _emphatic ? 2.65f : 1.95f, UnityEngine.Random.Range(-0.35f, 0.35f));

            if (_tm == null) _tm = gameObject.AddComponent<TextMesh>();
            _tm.text = txt;
            _tm.fontSize = _emphatic ? 56 : 48;
            _tm.characterSize = 0.088f * scale;
            _tm.anchor = TextAnchor.MiddleCenter;
            _tm.alignment = TextAlignment.Center;
            _tm.fontStyle = FontStyle.Bold;
            _tm.color = col;

            // Overshoot pop-in on frame 0
            transform.localScale = Vector3.one * (_emphatic ? 1.48f : 1.22f);
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime > 0.0001f ? Mathf.Min(0.05f, Time.unscaledDeltaTime) : 0.016f;
            _t += dt;
            _vel.y -= 3.2f * dt;
            _vel.x *= Mathf.Exp(-3.5f * dt);
            transform.position += _vel * dt;

            float settle = _t < 0.14f
                ? Mathf.Lerp(_emphatic ? 1.48f : 1.22f, 1.0f, _t / 0.14f)
                : 1.0f;
            transform.localScale = Vector3.one * settle;

            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            if (_tm != null) _tm.color = new Color(_col.r, _col.g, _col.b, Mathf.Clamp01(1.38f - _t * 1.42f));
            if (_t >= 0.98f) GameObjectPool.ReleaseFloatingText(this);
        }
    }

    /// <summary>
    /// Ensures the character's 3D SkinnedMeshRenderers and weapon MeshRenderers cast real-time
    /// URP directional human shadows onto the floor without any artificial quad/box frames at the feet.
    /// </summary>
    public class CharacterHumanShadowCaster : MonoBehaviour
    {
        public void Bind(Transform visualRoot, Animator anim, float scale = 1f)
        {
            Transform oldGroup = transform.Find("HumanGroundShadowGroup");
            if (oldGroup != null) Destroy(oldGroup.gameObject);

            if (visualRoot == null) return;
            foreach (var smr in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.receiveShadows = true;
                smr.updateWhenOffscreen = true;
            }
            foreach (var mr in visualRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            }
        }
    }
}

