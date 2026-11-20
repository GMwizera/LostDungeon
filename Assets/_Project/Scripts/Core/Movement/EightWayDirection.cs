using System;

namespace LostDungeon.Core.Movement
{
    /// <summary>
    /// Turns a movement vector into one of 8 sprite directions.
    /// Order matches the Kenney character sprites (Male_0 ... Male_7):
    /// 0 up-left, 1 left, 2 down-left, 3 down, 4 down-right, 5 right, 6 up-right, 7 up.
    /// Each step is 45 degrees anticlockwise.
    /// </summary>
    public static class EightWayDirection
    {
        public const int Count = 8;
        public const int Down = 3;

        private const double FirstDirectionAngle = 135.0; // index 0 points up-left
        private const double StepAngle = 360.0 / Count;

        /// <summary>
        /// Returns the direction index for (x, y). Callers should not pass a zero vector.
        /// </summary>
        public static int FromVector(float x, float y)
        {
            double angle = Math.Atan2(y, x) * 180.0 / Math.PI; // 0 = right, anticlockwise
            int steps = (int)Math.Round((angle - FirstDirectionAngle) / StepAngle);
            return ((steps % Count) + Count) % Count;
        }
    }
}
