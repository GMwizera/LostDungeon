using System;

namespace LostDungeon.Core.Movement
{
    /// <summary>
    /// Picks which of the 8 sprite directions to show for a move.
    /// Same order as the Kenney sprites Male_0 to Male_7, turning left 45 degrees each time:
    /// 0 up-left, 1 left, 2 down-left, 3 down, 4 down-right, 5 right, 6 up-right, 7 up.
    /// </summary>
    public static class EightWayDirection
    {
        public const int Count = 8;
        public const int Down = 3;

        private const double FirstDirectionAngle = 135.0; // index 0 points up-left
        private const double StepAngle = 360.0 / Count;

        /// <summary>Direction number for (x, y). Don't pass (0, 0).</summary>
        public static int FromVector(float x, float y)
        {
            double angle = Math.Atan2(y, x) * 180.0 / Math.PI; // 0 is right, goes anticlockwise
            int steps = (int)Math.Round((angle - FirstDirectionAngle) / StepAngle);
            return ((steps % Count) + Count) % Count;
        }
    }
}
