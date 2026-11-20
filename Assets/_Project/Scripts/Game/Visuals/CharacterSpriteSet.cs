using System;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>
    /// All sprites for one character, grouped by the 8 facing directions.
    /// The player and every enemy type can share or swap these assets.
    /// Build the Kenney one with Tools > Lost Dungeon > Build Male Sprite Set.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Character Sprite Set")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [Serializable]
        public class DirectionFrames
        {
            public Sprite idle;
            public Sprite[] run = Array.Empty<Sprite>();
            public Sprite[] action = Array.Empty<Sprite>();
        }

        [SerializeField] private DirectionFrames[] directions = new DirectionFrames[8];

        public int DirectionCount => directions.Length;

        public Sprite GetIdle(int direction) => directions[direction].idle;

        public Sprite GetRun(int direction, int frame) => Pick(directions[direction].run, frame, direction);

        public Sprite GetAction(int direction, int frame) => Pick(directions[direction].action, frame, direction);

        public int ActionFrameCount(int direction) => directions[direction].action.Length;

        private Sprite Pick(Sprite[] frames, int frame, int direction)
        {
            return frames.Length == 0 ? directions[direction].idle : frames[frame % frames.Length];
        }

#if UNITY_EDITOR
        public void EditorSetDirections(DirectionFrames[] frames) => directions = frames;
#endif
    }
}
