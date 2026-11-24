using UnityEngine;

namespace LethalFactory.Environment
{
    /// <summary>
    /// Subtle industrial fluorescent/sodium lamp flicker for eerie abandoned-factory ambiance.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float minIntensityMultiplier = 0.25f;
        [SerializeField] private float maxIntensityMultiplier = 1.15f;
        [SerializeField] private float flickerSpeed = 9.5f;
        [SerializeField] private float blackoutChance = 0.015f;

        private Light _light;
        private float _baseIntensity;
        private float _noiseSeed;
        private float _blackoutTimer;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _noiseSeed = Random.Range(0f, 500f);
        }

        private void Update()
        {
            if (_blackoutTimer > 0f)
            {
                _blackoutTimer -= Time.deltaTime;
                _light.intensity = _baseIntensity * 0.08f;
                return;
            }

            if (Random.value < blackoutChance * Time.deltaTime * 60f * 0.04f)
            {
                _blackoutTimer = Random.Range(0.05f, 0.22f);
                return;
            }

            float n = Mathf.PerlinNoise(_noiseSeed, Time.time * flickerSpeed);
            float mult = Mathf.Lerp(minIntensityMultiplier, maxIntensityMultiplier, n);
            _light.intensity = _baseIntensity * mult;
        }
    }
}
