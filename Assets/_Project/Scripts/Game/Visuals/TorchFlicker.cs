using UnityEngine;

namespace LostDungeon.Game.Visuals
{
    /// <summary>Makes a light flicker softly like a real flame.</summary>
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [SerializeField] private float amount = 0.25f;
        [SerializeField] private float speed = 3.5f;

        private Light torch;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            torch = GetComponent<Light>();
            baseIntensity = torch.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            torch.intensity = baseIntensity * (1f + noise * 2f * amount);
        }
    }
}
