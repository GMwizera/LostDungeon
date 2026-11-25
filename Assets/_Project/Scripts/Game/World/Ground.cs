using UnityEngine;

namespace LostDungeon.Game.World
{
    /// <summary>
    /// Gameplay works on a flat 2D floor, so this swaps between 2D floor and 3D world points.
    /// That is why the Core code didn't change when the game went 3D.
    /// </summary>
    public static class Ground
    {
        /// <summary>Chest height, where attacks and hits happen.</summary>
        public const float BodyHeight = 0.6f;

        public static Vector2 ToFloor(Vector3 world) => new Vector2(world.x, world.z);

        public static Vector3 ToWorld(Vector2 floor, float height = 0f) => new Vector3(floor.x, height, floor.y);

        /// <summary>Turns a floor direction into a rotation that faces it.</summary>
        public static Quaternion Facing(Vector2 direction) =>
            direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up)
                : Quaternion.identity;

        /// <summary>Turns stick or WASD input into a floor direction, using the camera angle.</summary>
        public static Vector2 ScreenToFloor(Vector2 input, float cameraYaw)
        {
            Vector3 world = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(input.x, 0f, input.y);
            return new Vector2(world.x, world.z);
        }
    }
}
