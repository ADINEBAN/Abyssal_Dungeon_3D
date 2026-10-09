using UnityEngine;
using AbyssalDungeon.Core;
using System.Collections;

namespace AbyssalDungeon.Audio
{
    public enum MusicType
    {
        MainMenu,
        Dungeon,
        Boss,
        Town,
        Victory,
        Defeat
    }

    public enum SFXType
    {
        PlayerAttack,
        PlayerHit,
        EnemyHit,
        ItemPickup,
        LevelUp,
        MenuClick
    }

    [System.Serializable]
    public class MusicTrack
    {
        public MusicType type;
        public AudioClip clip;
    }

    [System.Serializable]
    public class SFXTrack
    {
        public SFXType type;
        public AudioClip clip;
    }

    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private MusicTrack[] _musicTracks;
        [SerializeField] private SFXTrack[] _sfxTracks;

        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        private bool _isMuted = false;

        protected override void Awake()
        {
            base.Awake();
            if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            
            _musicSource.loop = true;
            UpdateVolumes();
        }

        public void PlayMusic(MusicType type, float fadeTime = 1f)
        {
            AudioClip clipToPlay = null;
            foreach (var track in _musicTracks)
            {
                if (track.type == type)
                {
                    clipToPlay = track.clip;
                    break;
                }
            }

            if (clipToPlay == null)
            {
                Debug.LogWarning($"[AudioManager] Music track {type} not found.");
                return;
            }

            if (_musicSource.isPlaying && _musicSource.clip == clipToPlay) return;

            StartCoroutine(CrossfadeMusic(clipToPlay, fadeTime));
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
        {
            float currentTime = 0;
            float startVol = _musicSource.volume;

            while (currentTime < duration / 2)
            {
                currentTime += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0, currentTime / (duration / 2));
                yield return null;
            }

            _musicSource.clip = newClip;
            _musicSource.Play();

            currentTime = 0;
            float targetVol = _musicVolume * _masterVolume;

            while (currentTime < duration / 2)
            {
                currentTime += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0, targetVol, currentTime / (duration / 2));
                yield return null;
            }
        }

        public void PlaySFX(SFXType type)
        {
            if (_isMuted) return;

            AudioClip clipToPlay = null;
            foreach (var track in _sfxTracks)
            {
                if (track.type == type)
                {
                    clipToPlay = track.clip;
                    break;
                }
            }

            if (clipToPlay != null)
            {
                _sfxSource.PlayOneShot(clipToPlay, _sfxVolume * _masterVolume);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX {type} not found.");
            }
        }

        public void SetVolumes(float master, float music, float sfx)
        {
            _masterVolume = Mathf.Clamp01(master);
            _musicVolume = Mathf.Clamp01(music);
            _sfxVolume = Mathf.Clamp01(sfx);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (_isMuted)
            {
                _musicSource.volume = 0;
                _sfxSource.volume = 0;
            }
            else
            {
                _musicSource.volume = _musicVolume * _masterVolume;
                _sfxSource.volume = _sfxVolume * _masterVolume;
            }
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            UpdateVolumes();
        }
    }
}
