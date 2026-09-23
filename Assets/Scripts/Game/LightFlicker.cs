using UnityEngine;

namespace Ushimitsu.Game
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 1f;
        public float flickerAmount = 0.25f;
        public float speed = 6f;
        [Tooltip("Chance per second of a brief blackout.")]
        public float blackoutChance = 0.12f;

        Light target;
        float noiseSeed;
        float blackoutTimer;

        void Awake()
        {
            target = GetComponent<Light>();
            noiseSeed = Random.value * 100f;
            if (baseIntensity <= 0f) baseIntensity = target.intensity;
        }

        void Update()
        {
            if (blackoutTimer > 0f)
            {
                blackoutTimer -= Time.deltaTime;
                target.intensity = baseIntensity * 0.05f;
                return;
            }

            if (Random.value < blackoutChance * Time.deltaTime)
            {
                blackoutTimer = Random.Range(0.05f, 0.18f);
                return;
            }

            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * speed);
            target.intensity = baseIntensity * (1f - flickerAmount + noise * flickerAmount * 2f);
        }
    }
}
