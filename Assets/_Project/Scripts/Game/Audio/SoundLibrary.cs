using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostDungeon.Game.Audio
{
    /// <summary>
    /// Maps sound ids (like "Sword" or "EnemyDefeated") to clips. Swapping sounds means editing
    /// this asset, never code. Several clips per id give variety.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Audio/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public string id;
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.3f)] public float pitchVariation = 0.08f;
        }

        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private List<Sound> sounds = new List<Sound>();

        private Dictionary<string, Sound> lookup;

        public AudioClip Music => music;
        public float MusicVolume => musicVolume;

        public bool TryGet(string id, out Sound sound)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sound>();
                foreach (Sound s in sounds)
                    if (!string.IsNullOrEmpty(s.id))
                        lookup[s.id] = s;
            }

            return lookup.TryGetValue(id, out sound) && sound.clips != null && sound.clips.Length > 0;
        }

        private void OnValidate() => lookup = null;
    }
}
