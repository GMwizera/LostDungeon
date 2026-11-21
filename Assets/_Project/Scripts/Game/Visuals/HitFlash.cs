using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>Tints a sprite briefly when something gets hit, so damage is easy to see.</summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private float duration = 0.12f;

        private Color normalColor = Color.white;
        private float endTime = -1f;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                normalColor = spriteRenderer.color;
        }

        public void Flash()
        {
            if (spriteRenderer == null)
                return;
            spriteRenderer.color = flashColor;
            endTime = Time.time + duration;
        }

        private void Update()
        {
            if (endTime > 0f && Time.time >= endTime)
            {
                spriteRenderer.color = normalColor;
                endTime = -1f;
            }
        }

        private void OnDisable()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;
            endTime = -1f;
        }
    }
}
