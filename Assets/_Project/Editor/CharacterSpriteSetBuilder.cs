using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// One-click creation of the CharacterSpriteSet for the Kenney "Male" character,
    /// instead of dragging 168 sprites into slots by hand.
    /// </summary>
    public static class CharacterSpriteSetBuilder
    {
        private const string Folder = "Assets/_Project/Art/kenney_isometric-miniature-dungeon/Characters/Male";
        private const string OutputPath = "Assets/_Project/ScriptableObjects/MaleSpriteSet.asset";
        private const int Directions = 8;
        private const int RunFrames = 10;
        private const int ActionFrames = 10;

        [MenuItem("Tools/Lost Dungeon/Build Male Sprite Set")]
        public static void Build()
        {
            var frames = new CharacterSpriteSet.DirectionFrames[Directions];
            for (int d = 0; d < Directions; d++)
            {
                frames[d] = new CharacterSpriteSet.DirectionFrames
                {
                    idle = Load($"Male_{d}_Idle0"),
                    run = LoadSequence($"Male_{d}_Run", RunFrames),
                    action = LoadSequence($"Male_{d}_Pickup", ActionFrames),
                };
            }

            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(OutputPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CharacterSpriteSet>();
                AssetDatabase.CreateAsset(set, OutputPath);
            }

            set.EditorSetDirections(frames);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log($"Built {OutputPath}");
        }

        private static Sprite[] LoadSequence(string prefix, int count)
        {
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = Load(prefix + i);
            return sprites;
        }

        private static Sprite Load(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
            if (sprite == null)
                Debug.LogWarning($"Missing sprite {name}. Did you reimport the Characters folder?");
            return sprite;
        }
    }
}
