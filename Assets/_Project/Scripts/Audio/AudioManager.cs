using System;
using System.Collections;
using UnityEngine;

namespace MoodCharacters
{
    [DefaultExecutionOrder(-90)]
    public class AudioManager : MonoBehaviour
    {
        private const string MasterKey = "audio_master";
        private const string MusicKey = "audio_music";
        private const string SfxKey = "audio_sfx";
        private const string MuteKey = "audio_muted";

        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource characterSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Background music per mood")]
        [SerializeField] private AudioClip neutralMusic;
        [SerializeField] private AudioClip happyMusic;
        [SerializeField] private AudioClip angryMusic;
        [SerializeField] private AudioClip sadMusic;

        [Header("UI sounds")]
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip screenChangeClip;

        [Header("Behaviour")]
        [SerializeField] private float crossfadeTime = 1.2f;

        private AudioSource activeMusic;
        private AudioSource idleMusic;
        private float activeWeight = 1f;
        private float idleWeight;
        private Coroutine fadeRoutine;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.6f;
        public float SfxVolume { get; private set; } = 0.9f;
        public bool IsMuted { get; private set; }

        public event Action SettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            musicSourceA = EnsureSource(musicSourceA, true);
            musicSourceB = EnsureSource(musicSourceB, true);
            characterSource = EnsureSource(characterSource, false);
            uiSource = EnsureSource(uiSource, false);

            activeMusic = musicSourceA;
            idleMusic = musicSourceB;

            MasterVolume = PlayerPrefs.GetFloat(MasterKey, MasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, SfxVolume);
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyVolumes();
        }

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.ActiveCharacterChanged += OnActiveCharacterChanged;
                gm.MoodChanged += OnMoodChanged;
                gm.CharacterReacted += OnCharacterReacted;
            }

            PlayMusic(MusicFor(Mood.Neutral), true);
        }

        private void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.ActiveCharacterChanged -= OnActiveCharacterChanged;
                gm.MoodChanged -= OnMoodChanged;
                gm.CharacterReacted -= OnCharacterReacted;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            ApplyVolumes();
        }

        private AudioSource EnsureSource(AudioSource source, bool loop)
        {
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyVolumes()
        {
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
            activeMusic.volume = activeWeight * MusicVolume;
            idleMusic.volume = idleWeight * MusicVolume;
            characterSource.volume = SfxVolume;
            uiSource.volume = SfxVolume;
        }

        private void OnActiveCharacterChanged(MoodCharacter character)
        {
            characterSource.Stop();
            PlayMusic(MusicFor(character.CurrentMood), false);
        }

        private void OnMoodChanged(MoodCharacter character, Mood mood)
        {
            PlayCharacterClip(character.GetMoodClip(mood));
            PlayMusic(MusicFor(mood), false);
        }

        private void OnCharacterReacted(MoodCharacter character)
        {
            PlayCharacterClip(character.ReactClip);
        }

        private AudioClip MusicFor(Mood mood)
        {
            AudioClip clip;
            switch (mood)
            {
                case Mood.Happy: clip = happyMusic; break;
                case Mood.Angry: clip = angryMusic; break;
                case Mood.Sad: clip = sadMusic; break;
                default: clip = neutralMusic; break;
            }
            return clip != null ? clip : neutralMusic;
        }

        public void PlayCharacterClip(AudioClip clip)
        {
            characterSource.Stop();
            if (clip == null)
            {
                return;
            }
            characterSource.clip = clip;
            characterSource.Play();
        }

        public void PlayClick()
        {
            if (clickClip != null)
            {
                uiSource.PlayOneShot(clickClip);
            }
        }

        public void PlayScreenChange()
        {
            if (screenChangeClip != null)
            {
                uiSource.PlayOneShot(screenChangeClip);
            }
        }

        private void PlayMusic(AudioClip clip, bool instant)
        {
            if (clip == null || (activeMusic.clip == clip && activeMusic.isPlaying))
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (instant)
            {
                idleMusic.Stop();
                idleWeight = 0f;
                activeMusic.clip = clip;
                activeMusic.Play();
                activeWeight = 1f;
                return;
            }

            AudioSource previous = activeMusic;
            float previousWeight = activeWeight;
            activeMusic = idleMusic;
            idleMusic = previous;
            idleWeight = previousWeight;
            activeWeight = 0f;

            activeMusic.clip = clip;
            activeMusic.Play();
            fadeRoutine = StartCoroutine(Crossfade());
        }

        private IEnumerator Crossfade()
        {
            float startIdle = idleWeight;
            float time = 0f;
            while (time < crossfadeTime)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / crossfadeTime);
                activeWeight = t;
                idleWeight = Mathf.Lerp(startIdle, 0f, t);
                yield return null;
            }

            activeWeight = 1f;
            idleWeight = 0f;
            idleMusic.Stop();
            fadeRoutine = null;
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            SaveAndNotify();
        }

        public void ToggleMute()
        {
            SetMuted(!IsMuted);
        }

        private void SaveAndNotify()
        {
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumes();
            SettingsChanged?.Invoke();
        }
    }
}
