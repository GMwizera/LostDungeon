using System;
using System.IO;
using LostDungeon.Game.UI;
using UnityEditor;
using UnityEngine;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Draws the game icons as PNG files from simple shapes.
    /// Old PNGs are kept, so you can swap in your own art.
    /// </summary>
    public static class IconFactory
    {
        private const string Folder = "Assets/_Project/Art/Generated/Icons";
        public const string IconsAssetPath = "Assets/_Project/ScriptableObjects/UI/UIIcons.asset";
        private const int Size = 64;

        // The game's colours, taken from the design.
        private static readonly Color Gold = new Color(0.96f, 0.62f, 0.04f);      // #f59e0b
        private static readonly Color Red = new Color(0.94f, 0.27f, 0.27f);       // #ef4444
        private static readonly Color Grey = new Color(0.25f, 0.25f, 0.27f);      // #3f3f46
        private static readonly Color Light = new Color(0.63f, 0.63f, 0.67f);     // #a1a1aa
        private static readonly Color Dark = new Color(0.16f, 0.09f, 0f);         // #2a1700, used on top of gold
        private static readonly Color Blue = new Color(0.22f, 0.74f, 0.97f);      // #38bdf8

        public static UIIcons Build()
        {
            EnsureFolder(Folder);
            EnsureFolder(Path.GetDirectoryName(IconsAssetPath)?.Replace('\\', '/'));

            var icons = AssetDatabase.LoadAssetAtPath<UIIcons>(IconsAssetPath);
            if (icons == null)
            {
                icons = ScriptableObject.CreateInstance<UIIcons>();
                AssetDatabase.CreateAsset(icons, IconsAssetPath);
            }

            icons.heartFull = Icon("HeartFull", (x, y) => Heart(x, y) ? Red : (Color?)null);
            icons.heartEmpty = Icon("HeartEmpty", (x, y) => Heart(x, y) && !Heart(x * 1.35f, y * 1.35f + 0.08f) ? Grey : (Color?)null);
            icons.locked = Icon("Lock", Lock);
            icons.completed = Icon("Check", Check);
            icons.available = Icon("Play", (x, y) => Triangle(x, y) ? Dark : (Color?)null);
            icons.gold = Icon("Coin", Coin);
            icons.key = Icon("Key", KeyShape);
            icons.potion = Icon("Potion", Potion);
            icons.sword = Icon("Sword", Sword);
            icons.dash = Icon("Dash", Dash);
            icons.fireball = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Generated/Fireball.png");
            icons.cloud = Icon("Cloud", Cloud);
            icons.flag = Icon("Flag", Flag);

            EditorUtility.SetDirty(icons);
            AssetDatabase.SaveAssets();
            return icons;
        }

        // ---------- Shapes (x and y go from -1 to 1) ----------

        private static bool Heart(float x, float y)
        {
            x *= 1.25f;
            y = y * 1.25f + 0.25f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        private static Color? Lock(float x, float y)
        {
            bool body = Mathf.Abs(x) < 0.62f && y > -0.85f && y < 0.1f;
            float ring = Mathf.Sqrt(x * x + (y - 0.1f) * (y - 0.1f));
            bool shackle = y > 0.1f && ring < 0.5f && ring > 0.3f;
            bool hole = Mathf.Abs(x) < 0.09f && y > -0.55f && y < -0.2f;
            if (hole) return null;
            return body || shackle ? Light : (Color?)null;
        }

        private static Color? Check(float x, float y)
        {
            bool shortStroke = DistanceToSegment(x, y, -0.65f, 0f, -0.15f, -0.55f) < 0.17f;
            bool longStroke = DistanceToSegment(x, y, -0.15f, -0.55f, 0.7f, 0.6f) < 0.17f;
            return shortStroke || longStroke ? Blue : (Color?)null;
        }

        private static Color? Cloud(float x, float y)
        {
            bool puff = Circle(x, y, -0.35f, 0f, 0.32f) || Circle(x, y, 0.05f, 0.2f, 0.42f) || Circle(x, y, 0.42f, -0.02f, 0.3f);
            bool body = y > -0.3f && y < 0.05f && x > -0.62f && x < 0.7f;
            bool drop = (DistanceToSegment(x, y, -0.3f, -0.45f, -0.4f, -0.75f) < 0.06f) ||
                        (DistanceToSegment(x, y, 0.05f, -0.45f, -0.05f, -0.75f) < 0.06f) ||
                        (DistanceToSegment(x, y, 0.4f, -0.45f, 0.3f, -0.75f) < 0.06f);
            return (puff || body) && y > -0.32f || drop ? Blue : (Color?)null;
        }

        private static Color? Flag(float x, float y)
        {
            bool pole = x > -0.62f && x < -0.48f && y > -0.85f && y < 0.8f;
            bool cloth = x >= -0.48f && x < 0.62f && y > 0.05f && y < 0.75f && !(x > 0.25f && Mathf.Abs(y - 0.4f) < (x - 0.25f) * 0.9f);
            return pole || cloth ? Gold : (Color?)null;
        }

        private static bool Circle(float x, float y, float cx, float cy, float r) =>
            (x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r;

        private static bool Triangle(float x, float y) => x > -0.45f && Mathf.Abs(y) < (0.65f - x) * 0.75f && x < 0.7f;

        private static Color? Coin(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 0.85f) return null;
            return r > 0.62f || (Mathf.Abs(x) < 0.12f && Mathf.Abs(y) < 0.4f) ? Gold : new Color(0.85f, 0.62f, 0.15f);
        }

        private static Color? KeyShape(float x, float y)
        {
            float r = Mathf.Sqrt((x + 0.45f) * (x + 0.45f) + y * y);
            bool bow = r < 0.42f && r > 0.2f;
            bool shaft = x > -0.1f && x < 0.85f && Mathf.Abs(y) < 0.1f;
            bool teeth = (Mathf.Abs(x - 0.55f) < 0.07f || Mathf.Abs(x - 0.78f) < 0.07f) && y < 0f && y > -0.35f;
            return bow || shaft || teeth ? Gold : (Color?)null;
        }

        private static Color? Potion(float x, float y)
        {
            bool bulb = x * x + (y + 0.25f) * (y + 0.25f) < 0.42f;
            bool neck = Mathf.Abs(x) < 0.17f && y > 0.2f && y < 0.7f;
            bool cork = Mathf.Abs(x) < 0.22f && y >= 0.7f && y < 0.88f;
            if (cork) return new Color(0.6f, 0.42f, 0.25f);
            if (bulb) return y < -0.1f ? Red : new Color(1f, 0.45f, 0.5f);
            return neck ? Light : (Color?)null;
        }

        private static Color? Sword(float x, float y)
        {
            bool blade = DistanceToSegment(x, y, -0.35f, -0.35f, 0.75f, 0.75f) < 0.1f;
            bool guard = DistanceToSegment(x, y, -0.55f, -0.15f, -0.15f, -0.55f) < 0.08f;
            bool grip = DistanceToSegment(x, y, -0.35f, -0.35f, -0.75f, -0.75f) < 0.08f;
            if (guard || grip) return Gold;
            return blade ? Light : (Color?)null;
        }

        private static Color? Dash(float x, float y)
        {
            for (int i = 0; i < 3; i++)
            {
                float offset = -0.55f + i * 0.45f;
                if (DistanceToSegment(x, y, offset - 0.2f, 0.45f, offset + 0.2f, 0f) < 0.11f ||
                    DistanceToSegment(x, y, offset + 0.2f, 0f, offset - 0.2f, -0.45f) < 0.11f)
                    return Blue;
            }
            return null;
        }

        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            var p = new Vector2(px, py);
            var a = new Vector2(ax, ay);
            var b = new Vector2(bx, by);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ---------- Rendering ----------

        /// <summary>Draws a shape with smooth edges and saves it as a sprite.</summary>
        private static Sprite Icon(string name, Func<float, float, Color?> shape)
        {
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                const int samples = 4;
                for (int py = 0; py < Size; py++)
                {
                    for (int px = 0; px < Size; px++)
                    {
                        Color sum = Color.clear;
                        int hits = 0;
                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                float x = (px + (sx + 0.5f) / samples) / Size * 2f - 1f;
                                float y = (py + (sy + 0.5f) / samples) / Size * 2f - 1f;
                                Color? c = shape(x, y);
                                if (c.HasValue)
                                {
                                    sum += c.Value;
                                    hits++;
                                }
                            }
                        }

                        Color pixel = hits > 0 ? sum / hits : Color.clear;
                        pixel.a = hits / (float)(samples * samples);
                        texture.SetPixel(px, py, pixel);
                    }
                }

                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 256f; // makes pickups a quarter of a tile wide
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
