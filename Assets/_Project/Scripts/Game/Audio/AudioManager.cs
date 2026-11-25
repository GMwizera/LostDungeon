using LostDungeon.Core.Events;
using LostDungeon.Game.Managers;
using UnityEngine;

namespace LostDungeon.Game.Audio
{
    /// <summary>
    /// Plays music and sounds (Singleton and Observer).
    /// It listens to events, so the rest of the game never talks to audio.
    /// </summary>
    [DefaultExecutionOrder(-100)] // runs before other scripts need it
    public class AudioManager : Singleton<AudioManager>
    {
        public const string UIClick = "UIClick";

        [SerializeField] private SoundLibrary library;
        [SerializeField, Range(1, 16)] private int effectVoices = 8;

        private AudioSource musicSource;
        private AudioSource[] voices;
        private int nextVoice;
        private float effectsVolume = 1f;
        private float musicVolume = 1f;
        private int lastPlayerHealth = -1;

        /// <summary>Music volume from settings, from 0 to 1.</summary>
        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null && library != null)
                    musicSource.volume = musicVolume * library.MusicVolume;
            }
        }

        public float EffectsVolume
        {
            get => effectsVolume;
            set => effectsVolume = Mathf.Clamp01(value);
        }

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicate)
                return;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            voices = new AudioSource[effectVoices];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
        }

        private void Start()
        {
            if (!IsDuplicate && library != null && library.Music != null)
            {
                musicSource.clip = library.Music;
                musicSource.volume = musicVolume * library.MusicVolume;
                musicSource.Play();
            }
        }

        private void OnEnable()
        {
            GameEvents.AbilityActivated += OnAbilityActivated;
            GameEvents.EnemyAttacked += OnEnemyAttacked;
            GameEvents.EnemyDefeated += OnEnemyDefeated;
            GameEvents.PlayerHealthChanged += OnPlayerHealthChanged;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.ItemCollected += OnItemCollected;
            GameEvents.LevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.AbilityActivated -= OnAbilityActivated;
            GameEvents.EnemyAttacked -= OnEnemyAttacked;
            GameEvents.EnemyDefeated -= OnEnemyDefeated;
            GameEvents.PlayerHealthChanged -= OnPlayerHealthChanged;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.ItemCollected -= OnItemCollected;
            GameEvents.LevelCompleted -= OnLevelCompleted;
        }

        /// <summary>Plays a sound by name. Missing sounds are just skipped.</summary>
        public void Play(string soundId)
        {
            if (IsDuplicate || library == null || !library.TryGet(soundId, out SoundLibrary.Sound sound))
                return;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;

            voice.pitch = 1f + Random.Range(-sound.pitchVariation, sound.pitchVariation);
            voice.PlayOneShot(sound.clips[Random.Range(0, sound.clips.Length)], sound.volume * effectsVolume);
        }

        private void OnAbilityActivated(string abilityName, float cooldown) => Play(abilityName);

        private void OnEnemyAttacked(string enemyId)
        {
            // Use this enemy's own sound if it has one, else the shared one.
            if (library != null && library.TryGet($"EnemyAttack.{enemyId}", out _))
                Play($"EnemyAttack.{enemyId}");
            else
                Play("EnemyAttack");
        }

        private void OnEnemyDefeated(string enemyId) => Play("EnemyDefeated");

        private void OnPlayerHealthChanged(int current, int max)
        {
            if (lastPlayerHealth >= 0 && current < lastPlayerHealth)
                Play("PlayerHurt");
            lastPlayerHealth = current;
        }

        private void OnPlayerDied()
        {
            Play("PlayerDied");
            lastPlayerHealth = -1;
        }

        private void OnItemCollected(string itemId, int amount) => Play("ItemCollected");
        private void OnLevelCompleted(int level) => Play("LevelComplete");
    }
}
