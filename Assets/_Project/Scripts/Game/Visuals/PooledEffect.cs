using LostDungeon.Game.Pooling;
using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>
    /// A short visual effect (sword slash, wind burst, hit sparks). Spawned from the pool, it restarts
    /// its particles and fades its lights and meshes, then returns itself to the pool when finished.
    /// </summary>
    public class PooledEffect : MonoBehaviour, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private bool fadeMeshes = true;
        [SerializeField] private float spin; // degrees per second around the vertical axis (wind)

        private ParticleSystem[] particles;
        private Light[] lights;
        private float[] lightIntensities;
        private Renderer[] meshes;
        private Color[] meshColors;
        private MaterialPropertyBlock block;
        private float age;
        private bool active;

        private void Awake()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
            lights = GetComponentsInChildren<Light>(true);
            lightIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                lightIntensities[i] = lights[i].intensity;

            meshes = GetComponentsInChildren<MeshRenderer>(true);
            meshColors = new Color[meshes.Length];
            for (int i = 0; i < meshes.Length; i++)
                meshColors[i] = meshes[i].sharedMaterial != null && meshes[i].sharedMaterial.HasProperty(BaseColorId)
                    ? meshes[i].sharedMaterial.GetColor(BaseColorId) : Color.white;
            block = new MaterialPropertyBlock();
        }

        public void OnSpawned()
        {
            age = 0f;
            active = true;
            foreach (ParticleSystem system in particles)
            {
                system.Clear(true);
                system.Play(true);
            }
            UpdateFade(0f);
        }

        public void OnDespawned()
        {
            active = false;
            foreach (ParticleSystem system in particles)
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!active)
                return;
            age += Time.deltaTime;
            if (spin != 0f)
                transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
            UpdateFade(Mathf.Clamp01(age / lifetime));
            if (age >= lifetime)
            {
                active = false;
                PoolManager.Instance.Despawn(gameObject);
            }
        }

        private void UpdateFade(float t)
        {
            float strength = 1f - t * t;
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = lightIntensities[i] * strength;

            if (!fadeMeshes)
                return;
            for (int i = 0; i < meshes.Length; i++)
            {
                Color color = meshColors[i];
                color.a *= strength;
                meshes[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                meshes[i].SetPropertyBlock(block);
            }
        }
    }
}
