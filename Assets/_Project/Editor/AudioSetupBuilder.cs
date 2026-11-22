using System;
using System.IO;
using System.Text;
using LostDungeon.Game.Audio;
using UnityEditor;
using UnityEngine;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Generates simple placeholder sounds and music as WAV files, fills a SoundLibrary with them,
    /// and puts an AudioManager in the scene. Replace any clip later by dragging a real sound into
    /// the SoundLibrary asset; existing WAVs and the library are never overwritten.
    /// </summary>
    public static class AudioSetupBuilder
    {
        private const string AudioFolder = "Assets/_Project/Audio/Generated";
        private const string LibraryPath = "Assets/_Project/ScriptableObjects/Audio/SoundLibrary.asset";
        private const int Rate = 22050;

        private static System.Random noiseSource = new System.Random(1);

        public static void AddToScene()
        {
            SoundLibrary library = GetOrCreateLibrary();
            var go = new GameObject("AudioManager");
            SetValues(go.AddComponent<AudioManager>(), ("library", library));
        }

        private static SoundLibrary GetOrCreateLibrary()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (existing != null)
                return existing;

            EnsureFolder(AudioFolder);
            EnsureFolder(Path.GetDirectoryName(LibraryPath)?.Replace('\\', '/'));

            var sounds = new (string id, float volume, Func<float[]> make)[]
            {
                ("Sword", 0.7f, Sword),
                ("Dash", 0.5f, Dash),
                ("Fireball", 0.6f, Fireball),
                ("EnemyAttack", 0.5f, EnemyGrunt),
                ("EnemyAttack.Archer", 0.6f, BowTwang),
                ("EnemyDefeated", 0.6f, EnemyDefeated),
                ("PlayerHurt", 0.7f, PlayerHurt),
                ("PlayerDied", 0.7f, PlayerDied),
                ("ItemCollected", 0.6f, Pickup),
                ("LevelComplete", 0.7f, LevelComplete),
                ("UIClick", 0.5f, UIClick),
            };

            var library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);

            var serialized = new SerializedObject(library);
            serialized.FindProperty("music").objectReferenceValue = CreateClip("Music", Music());
            serialized.FindProperty("musicVolume").floatValue = 0.35f;

            SerializedProperty list = serialized.FindProperty("sounds");
            list.arraySize = sounds.Length;
            for (int i = 0; i < sounds.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = sounds[i].id;
                element.FindPropertyRelative("volume").floatValue = sounds[i].volume;
                element.FindPropertyRelative("pitchVariation").floatValue = 0.08f;
                SerializedProperty clips = element.FindPropertyRelative("clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = CreateClip(sounds[i].id, sounds[i].make());
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return library;
        }

        // ---------- Sounds ----------

        private static float[] Sword()
        {
            float filtered = 0f;
            return Make(0.18f, (t, p) =>
            {
                filtered += Mathf.Lerp(0.6f, 0.05f, p) * (Noise() - filtered);
                return filtered * Envelope(p, 0.08f);
            });
        }

        private static float[] Dash()
        {
            float filtered = 0f;
            return Make(0.25f, (t, p) =>
            {
                filtered += Mathf.Lerp(0.03f, 0.3f, p) * (Noise() - filtered);
                return filtered * 1.6f * Envelope(p, 0.5f);
            });
        }

        private static float[] Fireball()
        {
            float filtered = 0f;
            return Make(0.4f, (t, p) =>
            {
                filtered += 0.08f * (Noise() - filtered);
                float tone = Sine(Mathf.Lerp(320f, 120f, p), t) * 0.4f;
                return (filtered * 2f + tone) * Envelope(p, 0.05f);
            });
        }

        private static float[] EnemyGrunt() =>
            Make(0.16f, (t, p) => Square(Mathf.Lerp(150f, 90f, p), t) * 0.35f * Envelope(p, 0.1f));

        private static float[] BowTwang() =>
            Make(0.22f, (t, p) => (Sine(620f, t) + Sine(930f, t) * 0.3f) * Mathf.Exp(-p * 9f) * 0.6f);

        private static float[] EnemyDefeated() =>
            Make(0.35f, (t, p) => Square(Mathf.Lerp(400f, 90f, p), t) * 0.3f * Envelope(p, 0.02f));

        private static float[] PlayerHurt()
        {
            float filtered = 0f;
            return Make(0.2f, (t, p) =>
            {
                filtered += 0.2f * (Noise() - filtered);
                return (Square(Mathf.Lerp(220f, 150f, p), t) * 0.3f + filtered * 0.5f) * Envelope(p, 0.03f);
            });
        }

        private static float[] PlayerDied() =>
            Make(0.9f, (t, p) => Sine(Mathf.Lerp(330f, 110f, p), t) * 0.5f * Envelope(p, 0.02f));

        private static float[] Pickup() =>
            Make(0.18f, (t, p) => Sine(p < 0.5f ? 880f : 1320f, t) * 0.5f * Envelope(p, 0.05f));

        private static float[] LevelComplete()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            return Make(0.9f, (t, p) =>
            {
                int index = Mathf.Min((int)(t / 0.13f), notes.Length - 1);
                float noteTime = t - index * 0.13f;
                return (Sine(notes[index], t) + Sine(notes[index] * 2f, t) * 0.2f) * Mathf.Exp(-noteTime * 4f) * 0.45f;
            });
        }

        private static float[] UIClick() =>
            Make(0.05f, (t, p) => Sine(1200f, t) * 0.5f * (1f - p));

        /// <summary>16-second loop in A minor: soft bass chords and a plucked melody.</summary>
        private static float[] Music()
        {
            float[] bass = { 110f, 87.31f, 130.81f, 98f }; // A2, F2, C3, G2
            float[] scale = { 220f, 261.63f, 293.66f, 329.63f, 392f, 440f };
            var picker = new System.Random(3);
            var melody = new float[32];
            for (int i = 0; i < melody.Length; i++)
                melody[i] = picker.Next(4) == 0 ? 0f : scale[picker.Next(scale.Length)]; // 0 = rest

            const float length = 16f;
            return Make(length, (t, p) =>
            {
                float root = bass[(int)(t / 4f) % bass.Length];
                float sample = Sine(root, t) * 0.18f + Sine(root * 1.5f, t) * 0.06f;

                int step = (int)(t / 0.5f) % melody.Length;
                if (melody[step] > 0f)
                    sample += Sine(melody[step], t) * Mathf.Exp(-(t % 0.5f) * 6f) * 0.12f;

                float edge = Mathf.Min(t, length - t);
                return sample * Mathf.Clamp01(edge / 0.02f); // tiny fade so the loop point doesn't click
            });
        }

        // ---------- Synth helpers ----------

        private static float[] Make(float seconds, Func<float, float, float> sampleAt)
        {
            var samples = new float[(int)(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = sampleAt(i / (float)Rate, i / (float)samples.Length);
            return samples;
        }

        private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        private static float Square(float frequency, float t) => Mathf.Sign(Sine(frequency, t));
        private static float Noise() => (float)(noiseSource.NextDouble() * 2.0 - 1.0);

        /// <summary>Quick rise over the first part of the sound, then a fade to silence.</summary>
        private static float Envelope(float progress, float attack) =>
            progress < attack ? progress / attack : Mathf.Pow(1f - (progress - attack) / (1f - attack), 2f);

        private static AudioClip CreateClip(string name, float[] samples)
        {
            string path = $"{AudioFolder}/{name}.wav";
            if (!File.Exists(path))
                WriteWav(path, samples);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void WriteWav(string path, float[] samples)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataBytes = samples.Length * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);          // fmt chunk size
            writer.Write((short)1);    // PCM
            writer.Write((short)1);    // mono
            writer.Write(Rate);
            writer.Write(Rate * 2);    // bytes per second
            writer.Write((short)2);    // bytes per sample
            writer.Write((short)16);   // bits per sample
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            foreach (float sample in samples)
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }
}
