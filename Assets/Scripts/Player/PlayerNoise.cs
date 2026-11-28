using UnityEngine;

namespace LethalFactory.Player
{
    /// <summary>
    /// Ruido de pasos del jugador para los enemigos con oído (NoiseSystem).
    /// Lee la velocidad real del CharacterController que mueve el FirstPersonExplorer,
    /// así no depende de qué teclas use el Explorer.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerNoise : MonoBehaviour
    {
        [Header("Pasos (radio en metros)")]
        [SerializeField] private float walkNoiseRadius = 5f;
        [SerializeField] private float runNoiseRadius = 15f;

        [Header("Ritmo de pasos (segundos)")]
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float runStepInterval = 0.35f;

        [Header("Umbrales de velocidad (m/s)")]
        [Tooltip("A partir de esta velocidad horizontal suenan los pasos")]
        [SerializeField] private float movingThreshold = 0.2f;
        [Tooltip("A partir de esta velocidad se considera que corre (Explorer: caminar 3.6, correr 6.5)")]
        [SerializeField] private float runSpeedThreshold = 5f;

        private CharacterController _controller;
        private float _stepTimer;

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnDisable()
        {
            IsRunning = false;
            _stepTimer = 0f;
        }

        private void Update()
        {
            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            bool walking = _controller.isGrounded && speed > movingThreshold;
            IsRunning = walking && speed >= runSpeedThreshold;

            if (!walking)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;
            float interval = IsRunning ? runStepInterval : walkStepInterval;
            if (_stepTimer >= interval)
            {
                _stepTimer = 0f;
                EmitNoise(IsRunning ? runNoiseRadius : walkNoiseRadius);
            }
        }

        /// <summary>Emite un ruido en la posición del jugador (lo usan también las herramientas).</summary>
        public void EmitNoise(float radius)
        {
            NoiseSystem.Emit(transform.position, radius);
        }
    }
}
