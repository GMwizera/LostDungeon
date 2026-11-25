using System.Collections.Generic;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>
    /// Flashes a model red when it gets hit.
    /// A MaterialPropertyBlock keeps the shared materials unchanged.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color flashColor = new Color(1f, 0.45f, 0.4f);
        [SerializeField] private Color flashGlow = new Color(0.9f, 0.15f, 0.05f);
        [SerializeField] private float duration = 0.12f;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private MaterialPropertyBlock block;
        private Color baseTint = Color.white;
        private float endTime = -1f;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                // Only flash the body and weapons, not effects or glowing eyes.
                bool lit = r.sharedMaterial != null && r.sharedMaterial.HasProperty(EmissionId)
                    && r.sharedMaterial.GetColor(EmissionId).maxColorComponent < 0.01f;
                if ((r is MeshRenderer || r is SkinnedMeshRenderer) && lit)
                    renderers.Add(r);
            }
        }

        public void SetBaseTint(Color tint)
        {
            baseTint = tint;
            Apply(baseTint, Color.black);
        }

        public void Flash()
        {
            Apply(baseTint * flashColor, flashGlow);
            endTime = Time.time + duration;
        }

        private void Update()
        {
            if (endTime > 0f && Time.time >= endTime)
            {
                Apply(baseTint, Color.black);
                endTime = -1f;
            }
        }

        private void OnDisable()
        {
            if (endTime > 0f)
                Apply(baseTint, Color.black);
            endTime = -1f;
        }

        private void Apply(Color color, Color glow)
        {
            if (block == null)
                return;
            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(EmissionId, glow);
                r.SetPropertyBlock(block);
            }
        }
    }
}
