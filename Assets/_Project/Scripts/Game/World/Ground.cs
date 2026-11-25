using UnityEngine;

namespace LostDungeon.Game.World
{
    /// <summary>
    /// The dungeon floor is the XZ plane (Y is up). Gameplay rules (AI, abilities, the A* grid) think in
    /// 2D floor coordinates, so this converts between a floor point (x, y) and a world point (x, height, y).
    /// Keeping gameplay in 2D floor space is why the Core library did not change when the game went 3D.
    /// </summary>
    public static class Ground
    {
        /// <summary>Height of a character's chest: where attacks, projectiles and hit effects happen.</summary>
        public const float BodyHeight = 0.6f;

        public static Vector2 ToFloor(Vector3 world) => new Vector2(world.x, world.z);

        public static Vector3 ToWorld(Vector2 floor, float height = 0f) => new Vector3(floor.x, height, floor.y);

        /// <summary>Turns a floor direction into the rotation that faces it (models look down +Z).</summary>
        public static Quaternion Facing(Vector2 direction) =>
            direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up)
                : Quaternion.identity;

        /// <summary>
        /// Converts stick or WASD input (up = up the screen) into a floor direction,
        /// using the camera's turn around the vertical axis. With the 45 degree camera, "up" walks into the room.
        /// </summary>
        public static Vector2 ScreenToFloor(Vector2 input, float cameraYaw)
        {
            Vector3 world = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(input.x, 0f, input.y);
            return new Vector2(world.x, world.z);
        }
    }
}
