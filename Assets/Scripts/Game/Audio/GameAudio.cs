using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    public enum GameMusic { None, MainMenu, Town, Dungeon, Boss, Victory, Defeat }
    public enum GameSfx { Click, Attack, HeavyAttack, ComboFinisher, GuardBreak, Magic, Hit, CritHit, Heal, Shield, Dodge, Loot, LevelUp, Door, Trap, BossRoar, Explosion, Footstep, Jump, LockOn }
    public enum FootstepSurface { Stone = 0, Wood = 1, Metal = 2, DirtMoss = 3, CrystalWater = 4 }

    /// <summary>
    /// Plays BGM and SFX from Resources/Audio if present, and synthesizes rich procedural clips
    /// as fallback so every combat action, UI click, level-up, and boss fight has crisp audio.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private static GameAudio _inst;
        public static GameAudio I
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<GameAudio>();
                    if (_inst == null)
                    {
                        var go = new GameObject("[GameAudio]");
                        DontDestroyOnLoad(go);
                        _inst = go.AddComponent<GameAudio>();
                    }
                }
                return _inst;
            }
        }

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private AudioSource _sfxVariedSource;
        private AudioSource _footstepSource;
        private AudioSource _ambSource;
        private GameMusic _currentMusic = GameMusic.None;
        private string _currentAmbience = "";
        private int _lastStepVariant = -1;
        private readonly Dictionary<GameSfx, AudioClip> _sfxCache = new Dictionary<GameSfx, AudioClip>();
        private readonly Dictionary<GameMusic, AudioClip> _bgmCache = new Dictionary<GameMusic, AudioClip>();
        private readonly Dictionary<string, AudioClip> _ambCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _footstepCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _weaponClipCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.spatialBlend = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _sfxVariedSource = gameObject.AddComponent<AudioSource>();
            _sfxVariedSource.loop = false;
            _sfxVariedSource.playOnAwake = false;
            _sfxVariedSource.spatialBlend = 0f;

            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.loop = false;
            _footstepSource.playOnAwake = false;
            _footstepSource.spatialBlend = 0f;

            _ambSource = gameObject.AddComponent<AudioSource>();
            _ambSource.loop = true;
            _ambSource.playOnAwake = false;
            _ambSource.spatialBlend = 0f;

            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            var p = GameSession.I.Profile;
            AudioListener.volume = Mathf.Clamp01(p.masterVol);
            if (_bgmSource != null) _bgmSource.volume = Mathf.Clamp01(p.musicVol) * 0.42f;
            if (_sfxSource != null) _sfxSource.volume = Mathf.Clamp01(p.sfxVol);
            if (_sfxVariedSource != null) _sfxVariedSource.volume = Mathf.Clamp01(p.sfxVol);
            if (_footstepSource != null) _footstepSource.volume = Mathf.Clamp01(p.sfxVol);
            if (_ambSource != null) _ambSource.volume = Mathf.Clamp01(p.sfxVol) * 0.34f;
        }

        public void PlayAmbience(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                StopAmbience();
                return;
            }
            if (_currentAmbience == clipName && _ambSource != null && _ambSource.isPlaying)
                return;

            _currentAmbience = clipName;
            if (!_ambCache.TryGetValue(clipName, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + clipName);
                _ambCache[clipName] = clip;
            }

            if (clip != null && _ambSource != null)
            {
                ApplyVolumes();
                _ambSource.clip = clip;
                _ambSource.loop = true;
                _ambSource.Play();
            }
        }

        public void StopAmbience()
        {
            _currentAmbience = "";
            if (_ambSource != null && _ambSource.isPlaying)
                _ambSource.Stop();
        }

        public void PlayMusic(GameMusic music)
        {
            if (music == _currentMusic && _bgmSource.isPlaying) return;
            _currentMusic = music;
            if (music == GameMusic.None) { _bgmSource.Stop(); return; }

            if (!_bgmCache.TryGetValue(music, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/bgm_" + music.ToString().ToLowerInvariant());
                if (clip == null) clip = BuildProceduralMusic(music);
                _bgmCache[music] = clip;
            }
            ApplyVolumes();
            _bgmSource.clip = clip;
            _bgmSource.loop = (music != GameMusic.Victory && music != GameMusic.Defeat);
            _bgmSource.Play();
        }

        public void PlaySfx(GameSfx sfx, float volScale = 1f)
        {
            AudioClip clip = GetOrBuildSfx(sfx);
            ApplyVolumes();
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volScale));
        }

        public void PlaySfxVaried(GameSfx sfx, float volScale = 1f, float pitchSpread = 0.06f, float basePitch = 1f)
        {
            AudioClip clip = GetOrBuildSfx(sfx);
            ApplyVolumes();
            if (_sfxVariedSource != null)
            {
                _sfxVariedSource.pitch = Mathf.Clamp(basePitch + UnityEngine.Random.Range(-pitchSpread, pitchSpread), 0.65f, 1.45f);
                _sfxVariedSource.PlayOneShot(clip, Mathf.Clamp01(volScale));
            }
            else
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volScale));
            }
        }

        /// <summary>
        /// Plays distinct surface-reactive footstep audio (Stone, Wood, Metal, Dirt/Moss, Crystal/Water)
        /// strictly one step at a time (single-voice, never overlapping) synchronized to the local hero's foot touchdown.
        /// </summary>
        public void PlayFootstep(FootstepSurface surface, bool isSprint, bool isLanding, bool isLeftFoot, float volumeScale = 1f)
        {
            int variant = UnityEngine.Random.Range(1, 5);
            if (variant == _lastStepVariant) variant = (variant % 4) + 1;
            _lastStepVariant = variant;

            string surfTag = GetSurfaceAudioTag(surface);
            string key = isLanding ? ("sfx_land_" + surfTag) : $"sfx_step_{surfTag}_{variant}";

            if (!_footstepCache.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + key);
                if (clip == null)
                    clip = BuildProceduralSurfaceFootstep(surface, variant, isLanding);
                _footstepCache[key] = clip;
            }

            ApplyVolumes();
            float footBias = isLeftFoot ? -0.03f : 0.03f;
            float gaitPitch = isLanding ? 0.90f : (isSprint ? 1.05f : 1.0f);
            float jitter = UnityEngine.Random.Range(-0.025f, 0.025f);
            float finalPitch = Mathf.Clamp(gaitPitch + footBias + jitter, 0.78f, 1.22f);

            AudioSource src = _footstepSource != null ? _footstepSource : _sfxVariedSource;
            if (src != null)
            {
                // Stop any previous step tail so only 1 crisp step sound is heard per foot contact
                src.Stop();
                src.pitch = finalPitch;
                src.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            }
            else if (_sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            }
        }

        public void StopFootsteps()
        {
            if (_footstepSource != null && _footstepSource.isPlaying)
                _footstepSource.Stop();
        }

        public static string GetClassWeaponAudioTag(int classId)
        {
            switch (Mathf.Clamp(classId, 0, 9))
            {
                case 0: return "sword";     // Knight (Sword & Shield)
                case 1: return "hammer";    // Guardian (Tower Shield & Warhammer)
                case 2: return "greataxe";  // Berserker (GreatSword / Greataxe)
                case 3: return "dagger";    // Assassin (Twin Daggers)
                case 4: return "bow";       // Ranger (Longbow)
                case 5: return "staff";     // Mage (Elemental Staff)
                case 6: return "scepter";   // Warlock (Abyssal Scepter / Wand)
                case 7: return "gauntlet";  // Monk (Martial Gauntlets / Fists)
                case 8: return "scythe";    // Druid (Nature Staff / Primal Claws)
                case 9: return "mace";      // Cleric (Sacred Flanged Mace)
                default: return "sword";
            }
        }

        /// <summary>
        /// Plays the signature weapon attack sound corresponding to the character's class weapon.
        /// </summary>
        public void PlayWeaponAttack(int classId, int comboStep, bool isHeavy, bool isFinisher, float volumeScale = 1f)
        {
            string wpnTag = GetClassWeaponAudioTag(classId);
            string key = (isHeavy || isFinisher) ? $"sfx_wpn_{wpnTag}_skill" : $"sfx_wpn_{wpnTag}_attack";

            if (!_weaponClipCache.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + key);
                _weaponClipCache[key] = clip;
            }

            ApplyVolumes();
            float basePitch = isHeavy ? 0.88f : (isFinisher ? 0.94f : (0.96f + Mathf.Clamp(comboStep, 0, 3) * 0.05f));
            float pitch = Mathf.Clamp(basePitch + UnityEngine.Random.Range(-0.03f, 0.03f), 0.72f, 1.32f);

            if (clip != null)
            {
                AudioSource src = _sfxVariedSource != null ? _sfxVariedSource : _sfxSource;
                if (src != null)
                {
                    src.pitch = pitch;
                    src.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
                }
            }
            else
            {
                PlaySfxVaried(isHeavy ? GameSfx.HeavyAttack : (isFinisher ? GameSfx.ComboFinisher : GameSfx.Attack), volumeScale, 0.04f, basePitch);
            }
        }

        /// <summary>
        /// Plays the signature weapon skill sound + skill archetype layer when casting any class skill or weapon skill.
        /// </summary>
        public void PlayWeaponSkill(int classId, SkillDef skill, int slot, float volumeScale = 1f)
        {
            string wpnTag = GetClassWeaponAudioTag(classId);
            string key = $"sfx_wpn_{wpnTag}_skill";

            if (!_weaponClipCache.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + key);
                _weaponClipCache[key] = clip;
            }

            ApplyVolumes();
            bool isUlt = (skill != null && skill.ultimate) || slot == 3;
            float basePitch = isUlt ? 0.86f : (1.02f - Mathf.Clamp(slot, 0, 3) * 0.03f);
            float pitch = Mathf.Clamp(basePitch + UnityEngine.Random.Range(-0.03f, 0.03f), 0.72f, 1.28f);

            if (clip != null)
            {
                AudioSource src = _sfxVariedSource != null ? _sfxVariedSource : _sfxSource;
                if (src != null)
                {
                    src.pitch = pitch;
                    src.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
                }
            }

            // Secondary elemental/role accent layer on _sfxSource so ultimates, heals, and shields sound distinct
            if (skill != null)
            {
                if (skill.kind == SkillKind.HealAll || skill.kind == SkillKind.HealTarget || skill.kind == SkillKind.Revive || skill.kind == SkillKind.Cleanse)
                    PlaySfx(GameSfx.Heal, volumeScale * 0.55f);
                else if (skill.kind == SkillKind.ShieldAll || skill.kind == SkillKind.ShieldSelf)
                    PlaySfx(GameSfx.Shield, volumeScale * 0.55f);
                else if (skill.ultimate)
                    PlaySfx(GameSfx.Explosion, volumeScale * 0.50f);
            }
        }

        private static string GetSurfaceAudioTag(FootstepSurface surface)
        {
            switch (surface)
            {
                case FootstepSurface.Wood: return "wood";
                case FootstepSurface.Metal: return "metal";
                case FootstepSurface.DirtMoss: return "dirt";
                case FootstepSurface.CrystalWater: return "crystal";
                default: return "stone";
            }
        }

        private static AudioClip BuildProceduralSurfaceFootstep(FootstepSurface surface, int variant, bool isLanding)
        {
            int sr = 22050;
            float dur = isLanding ? 0.24f : 0.17f;
            int n = Mathf.Max(256, Mathf.RoundToInt(sr * dur));
            float[] data = new float[n];
            var rng = new System.Random((int)surface * 3571 + variant * 193 + (isLanding ? 911 : 0));

            float toeDelay = isLanding ? 0.018f : (0.028f + variant * 0.004f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float u = (float)i / n;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float v = 0f;

                switch (surface)
                {
                    case FootstepSurface.Wood:
                        {
                            // Hollow oak plank thump + board knock + subtle timber creak harmonic
                            float f1 = 128f + variant * 9f;
                            float f2 = f1 * 1.58f;
                            float f3 = f1 * 2.42f;
                            float pitchDrop = 1f + 0.35f * Mathf.Exp(-t * 45f);
                            float envBody = Mathf.Exp(-t * (isLanding ? 19f : 25f));
                            float envKnock = Mathf.Exp(-t * 54f);
                            float plank = (0.72f * Mathf.Sin(2f * Mathf.PI * f1 * pitchDrop * t)
                                         + 0.48f * Mathf.Sin(2f * Mathf.PI * f2 * t)
                                         + 0.28f * Mathf.Sin(2f * Mathf.PI * f3 * t) * envKnock) * envBody;
                            float creak = 0.16f * Mathf.Sin(2f * Mathf.PI * (520f + variant * 40f + Mathf.Sin(t * 95f) * 24f) * t)
                                        * Mathf.Sin(Mathf.PI * u) * Mathf.Exp(-t * 14f);
                            float toe = t >= toeDelay
                                ? 0.42f * Mathf.Sin(2f * Mathf.PI * (f1 * 1.18f) * (t - toeDelay)) * Mathf.Exp(-(t - toeDelay) * 38f)
                                : 0f;
                            v = plank + noise * envKnock * 0.38f + creak + toe;
                            break;
                        }

                    case FootstepSurface.Metal:
                        {
                            // Forged iron grate clink + metallic plate ring
                            float b = 430f + variant * 28f;
                            float ring = 0.42f * Mathf.Sin(2f * Mathf.PI * b * t) * Mathf.Exp(-t * 24f)
                                       + 0.34f * Mathf.Sin(2f * Mathf.PI * (b * 1.47f) * t) * Mathf.Exp(-t * 32f)
                                       + 0.26f * Mathf.Sin(2f * Mathf.PI * (b * 2.18f) * t) * Mathf.Exp(-t * 45f)
                                       + 0.18f * Mathf.Sin(2f * Mathf.PI * (b * 3.12f) * t) * Mathf.Exp(-t * 62f);
                            float thump = 0.42f * Mathf.Sin(2f * Mathf.PI * 145f * Mathf.Exp(-t * 15f) * t) * Mathf.Exp(-t * 34f);
                            v = ring + thump + noise * Mathf.Exp(-t * 95f) * 0.55f;
                            break;
                        }

                    case FootstepSurface.DirtMoss:
                        {
                            // Gritty earth crunch & moss foliage rustle
                            float grain = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (44f + variant * 6f) * t) * Mathf.Sin(2f * Mathf.PI * 118f * t);
                            float env = Mathf.Sin(Mathf.Min(1f, u * 1.15f) * Mathf.PI) * Mathf.Exp(-t * (isLanding ? 16f : 22f));
                            float thud = 0.46f * Mathf.Sin(2f * Mathf.PI * (102f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 34f);
                            v = thud + noise * grain * env * 0.78f;
                            break;
                        }

                    case FootstepSurface.CrystalWater:
                        {
                            // Crystalline chime tap + water ripple splash
                            float cf = 1160f + variant * 92f;
                            float chime = 0.40f * Mathf.Sin(2f * Mathf.PI * cf * t) * Mathf.Exp(-t * 28f)
                                        + 0.25f * Mathf.Sin(2f * Mathf.PI * (cf * 1.62f) * t) * Mathf.Exp(-t * 42f);
                            float splash = noise * Mathf.Exp(-t * 48f) * 0.55f;
                            float thud = 0.35f * Mathf.Sin(2f * Mathf.PI * 172f * Mathf.Exp(-t * 14f) * t) * Mathf.Exp(-t * 36f);
                            v = chime + splash + thud;
                            break;
                        }

                    default: // Stone
                        {
                            // Crisp armored heel-toe strike on cathedral stone slab
                            float hf = 158f + variant * 11f;
                            float heel = 0.65f * Mathf.Sin(2f * Mathf.PI * (hf * Mathf.Exp(-t * 14f)) * t) * Mathf.Exp(-t * (isLanding ? 28f : 38f));
                            float click = noise * Mathf.Exp(-t * 92f) * 0.72f;
                            float toe = t >= toeDelay ? noise * Mathf.Exp(-(t - toeDelay) * 68f) * 0.48f : 0f;
                            v = heel + click + toe;
                            break;
                        }
                }

                data[i] = Mathf.Clamp(v * (isLanding ? 0.88f : 0.76f), -1f, 1f);
            }

            var clip = AudioClip.Create($"sfx_step_{surface}_{variant}", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GetOrBuildSfx(GameSfx sfx)
        {
            if (!_sfxCache.TryGetValue(sfx, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/sfx_" + sfx.ToString().ToLowerInvariant());
                if (clip == null) clip = BuildProceduralSfx(sfx);
                _sfxCache[sfx] = clip;
            }
            return clip;
        }

        private static AudioClip BuildProceduralSfx(GameSfx sfx)
        {
            int sr = 22050;
            float dur = 0.22f;
            switch (sfx)
            {
                case GameSfx.Click: dur = 0.08f; break;
                case GameSfx.Attack: dur = 0.16f; break;
                case GameSfx.HeavyAttack: dur = 0.34f; break;
                case GameSfx.ComboFinisher: dur = 0.30f; break;
                case GameSfx.GuardBreak: dur = 0.42f; break;
                case GameSfx.Dodge: dur = 0.20f; break;
                case GameSfx.LevelUp: dur = 0.75f; break;
                case GameSfx.BossRoar: dur = 0.85f; break;
                case GameSfx.Explosion: dur = 0.45f; break;
                case GameSfx.Heal:
                case GameSfx.Loot: dur = 0.38f; break;
                case GameSfx.Footstep: dur = 0.07f; break;
                case GameSfx.Jump: dur = 0.16f; break;
                case GameSfx.LockOn: dur = 0.11f; break;
            }

            int n = Mathf.Max(256, Mathf.RoundToInt(sr * dur));
            float[] data = new float[n];
            var rng = new System.Random((int)sfx * 7919 + 17);

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float u = (float)i / n;
                float env = Mathf.Pow(1f - u, 1.6f);
                float v = 0f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

                switch (sfx)
                {
                    case GameSfx.Click:
                        v = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Pow(1f - u, 3f);
                        break;
                    case GameSfx.Attack:
                        v = (noise * 0.55f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 130f, u) * t) * 0.45f) * env;
                        break;
                    case GameSfx.HeavyAttack:
                        v = (noise * 0.50f * Mathf.Sin(Mathf.PI * u)
                             + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 58f, u) * t) * 0.85f
                             + 0.35f * Mathf.Sin(2f * Mathf.PI * 110f * t)) * Mathf.Pow(1f - u, 1.35f);
                        break;
                    case GameSfx.ComboFinisher:
                        v = (noise * 0.42f
                             + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(620f, 165f, u) * t) * 0.65f
                             + 0.38f * Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Pow(1f - u, 2.2f)) * env;
                        break;
                    case GameSfx.GuardBreak:
                        float shatter = Mathf.Sin(2f * Mathf.PI * (u < 0.35f ? 1180f : 740f) * t) * 0.5f;
                        float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(175f, 48f, u) * t) * 0.78f;
                        v = (noise * 0.55f + shatter + sub) * Mathf.Pow(1f - u, 1.4f);
                        break;
                    case GameSfx.Magic:
                        v = (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 760f, u) * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 1120f * t)) * 0.5f * env;
                        break;
                    case GameSfx.Hit:
                        v = (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(210f, 75f, u) * t) * 0.7f) * Mathf.Pow(1f - u, 2.2f);
                        break;
                    case GameSfx.CritHit:
                        v = (noise * 0.45f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(540f, 110f, u) * t) * 0.8f) * env;
                        break;
                    case GameSfx.Heal:
                        float fHeal = u < 0.5f ? 523.25f : 659.25f;
                        v = (Mathf.Sin(2f * Mathf.PI * fHeal * t) + 0.4f * Mathf.Sin(4f * Mathf.PI * fHeal * t)) * 0.45f * env;
                        break;
                    case GameSfx.Shield:
                        v = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 440f, u) * t) * 0.5f * env;
                        break;
                    case GameSfx.Dodge:
                        v = noise * 0.45f * Mathf.Sin(Mathf.PI * u);
                        break;
                    case GameSfx.Loot:
                        float fLoot = u < 0.35f ? 587f : (u < 0.7f ? 784f : 1046f);
                        v = Mathf.Sin(2f * Mathf.PI * fLoot * t) * 0.5f * env;
                        break;
                    case GameSfx.LevelUp:
                        float[] notes = { 440f, 554.37f, 659.25f, 880f };
                        float fn = notes[Mathf.Clamp((int)(u * 4f), 0, 3)];
                        v = (Mathf.Sin(2f * Mathf.PI * fn * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * fn * t)) * 0.5f * (1f - u * 0.6f);
                        break;
                    case GameSfx.Door:
                        v = (noise * 0.35f + Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.65f) * env;
                        break;
                    case GameSfx.Trap:
                        v = (noise * 0.6f + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.4f) * env;
                        break;
                    case GameSfx.BossRoar:
                        v = (noise * 0.45f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 58f, u) * t) * 0.75f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.1f));
                        break;
                    case GameSfx.Explosion:
                        v = (noise * 0.7f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 42f, u) * t) * 0.6f) * Mathf.Pow(1f - u, 1.5f);
                        break;
                    case GameSfx.Footstep:
                        v = (noise * 0.55f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(135f, 62f, u) * t) * 0.45f) * Mathf.Pow(1f - u, 2.8f);
                        break;
                    case GameSfx.Jump:
                        v = (noise * 0.25f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 340f, u) * t) * 0.45f) * Mathf.Sin(Mathf.PI * u);
                        break;
                    case GameSfx.LockOn:
                        float fLock = u < 0.45f ? 660f : 990f;
                        v = Mathf.Sin(2f * Mathf.PI * fLock * t) * 0.45f * Mathf.Pow(1f - u, 1.8f);
                        break;
                }
                data[i] = Mathf.Clamp(v * 0.65f, -1f, 1f);
            }

            var clip = AudioClip.Create("sfx_" + sfx, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildProceduralMusic(GameMusic music)
        {
            int sr = 22050;
            float dur = (music == GameMusic.Victory || music == GameMusic.Defeat) ? 4f : 8f;
            int n = Mathf.RoundToInt(sr * dur);
            float[] data = new float[n];

            float[] scale;
            float tempo;
            switch (music)
            {
                case GameMusic.MainMenu:
                    scale = new[] { 146.83f, 174.61f, 220f, 261.63f, 293.66f, 220f, 174.61f, 164.81f };
                    tempo = 1f;
                    break;
                case GameMusic.Town:
                    scale = new[] { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 392f, 329.63f };
                    tempo = 1f;
                    break;
                case GameMusic.Boss:
                    scale = new[] { 110f, 130.81f, 146.83f, 155.56f, 146.83f, 130.81f, 116.54f, 110f };
                    tempo = 2f;
                    break;
                case GameMusic.Victory:
                    scale = new[] { 261.63f, 329.63f, 392f, 523.25f };
                    tempo = 1f;
                    break;
                case GameMusic.Defeat:
                    scale = new[] { 220f, 196f, 174.61f, 146.83f };
                    tempo = 1f;
                    break;
                default: // Dungeon
                    scale = new[] { 130.81f, 155.56f, 196f, 185f, 155.56f, 146.83f, 130.81f, 123.47f };
                    tempo = 1.25f;
                    break;
            }

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                int step = (int)(t * tempo) % scale.Length;
                float local = (t * tempo) % 1f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(local));
                float f = scale[step];
                float pad = 0.25f * Mathf.Sin(2f * Mathf.PI * (f * 0.5f) * t);
                float lead = 0.30f * Mathf.Sin(2f * Mathf.PI * f * t) * env;
                float over = 0.12f * Mathf.Sin(2f * Mathf.PI * (f * 2f) * t) * env;
                data[i] = Mathf.Clamp((pad + lead + over) * 0.55f, -1f, 1f);
            }

            var clip = AudioClip.Create("bgm_" + music, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

