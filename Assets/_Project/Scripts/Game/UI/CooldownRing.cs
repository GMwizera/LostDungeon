using UnityEngine;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>
    /// A circular cooldown ring drawn with UI Toolkit's vector painter: a dim track, and a bright arc
    /// that refills clockwise from the top as the ability recharges. A full bright ring means "ready".
    /// </summary>
    public class CooldownRing : VisualElement
    {
        private float remaining; // 1 = just used, 0 = ready
        private Color color = Color.white;
        private Color track = new Color(1f, 1f, 1f, 0.15f);
        private float thickness = 4f;

        public CooldownRing()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            generateVisualContent += Draw;
        }

        public void Configure(Color ringColor, float ringThickness)
        {
            color = ringColor;
            thickness = ringThickness;
            MarkDirtyRepaint();
        }

        /// <summary>Fraction of the cooldown still to go (0 = ready).</summary>
        public float Remaining
        {
            get => remaining;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, remaining))
                    return;
                remaining = clamped;
                MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - thickness * 0.5f;
            if (radius <= 0f)
                return;

            Painter2D painter = context.painter2D;
            Vector2 centre = rect.center;
            painter.lineWidth = thickness;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = track;
            painter.BeginPath();
            painter.Arc(centre, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            float filled = (1f - remaining) * 360f;
            if (filled <= 0.5f)
                return;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(centre, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + filled));
            painter.Stroke();
        }
    }
}
