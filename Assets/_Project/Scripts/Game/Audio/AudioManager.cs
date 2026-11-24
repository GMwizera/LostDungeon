using LostDungeon.Core.Events;
using LostDungeon.Game.Managers;
using UnityEngine;

namespace LostDungeon.Game.Audio
{
    /// <summary>
    /// Plays music and sound effects (Singleton + Observer).
    /// It listens to GameEvents, so combat, enemies and levels never reference audio at all:
    /// removing this object leaves a silent but working game.
    /// UI buttons will call Play(UIClick) when the menus are built.
    /// </summary>
    [DefaultExecutionOrder(-100)] // set up before menus and gameplay scripts ask for it
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

        /// <summary>Player's music setting, 0 to 1 (multiplied by the library's base music volume).</summary>
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

        /// <summary>Plays a sound by id. Unknown ids are ignored, so missing clips never break the game.</summary>
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
            // A specific sound per enemy type if there is one, otherwise the shared one.
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
