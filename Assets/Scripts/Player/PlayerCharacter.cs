using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalFactory.Player
{
    /// <summary>
    /// Convierte al FirstPersonExplorer en el jugador "real" que entienden los enemigos:
    /// implementa IPlayer (vida, muerte, si se mueve, si mira algo) y avisa por eventos
    /// cuando recibe daño o muere, para que un GameManager / HUD pueda reaccionar.
    ///
    /// No modifica al FirstPersonExplorer: solo lo lee y lo apaga cuando el jugador muere.
    /// Al agregar este componente se agregan solos PlayerNoise y PlayerTools.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FirstPersonExplorer), typeof(PlayerNoise), typeof(PlayerTools))]
    public class PlayerCharacter : MonoBehaviour, IPlayer
    {
        [Header("Referencias")]
        [Tooltip("Cámara / cabeza del jugador. Si se deja vacío se usa la Camera hija")]
        [SerializeField] private Transform eyes;

        [Header("Vida")]
        [SerializeField] private int maxHealth = 100;

        [Header("Detección (lo que leen los enemigos)")]
        [Tooltip("Velocidad horizontal mínima (m/s) para considerar que el jugador se está moviendo")]
        [SerializeField] private float movingThreshold = 0.2f;
        [Tooltip("Movimiento del mouse por frame (px) a partir del cual se considera que mira alrededor")]
        [SerializeField] private float lookAroundThreshold = 2f;
        [Tooltip("Capas que bloquean la línea de vista en IsLookingAt")]
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        [Header("Muerte")]
        [SerializeField] private bool unlockCursorOnDeath = true;

        /// <summary>(vida actual, vida máxima)</summary>
        public event Action<int, int> HealthChanged;
        /// <summary>Cantidad de daño recibido.</summary>
        public event Action<int> Damaged;
        public event Action Died;

        private CharacterController _controller;
        private FirstPersonExplorer _explorer;
        private PlayerNoise _noise;
        private PlayerTools _tools;
        private Animator _animator;
        private int _health;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

        // ------------------------------------------------------------------
        // IPlayer
        // ------------------------------------------------------------------

        public Transform Eyes => eyes;
        public bool IsMoving { get; private set; }
        public bool IsLookingAround { get; private set; }
        public bool IsDead { get; private set; }

        public int Health => _health;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _explorer = GetComponent<FirstPersonExplorer>();
            _noise = GetComponent<PlayerNoise>();
            _tools = GetComponent<PlayerTools>();
            _animator = GetComponentInChildren<Animator>();

            if (eyes == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                eyes = cam != null ? cam.transform : transform;
            }

            _health = maxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(_health, maxHealth);
        }

        private void Update()
        {
            if (IsDead)
            {
                IsMoving = false;
                IsLookingAround = false;
                return;
            }

            // Se mueve = el CharacterController se desplazó en horizontal (lo mueve el Explorer).
            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            IsMoving = velocity.sqrMagnitude > movingThreshold * movingThreshold;

            // Mira alrededor = está moviendo el mouse con el cursor capturado (igual que el Explorer).
            Mouse mouse = Mouse.current;
            IsLookingAround = mouse != null
                              && Cursor.lockState == CursorLockMode.Locked
                              && mouse.delta.ReadValue().sqrMagnitude > lookAroundThreshold * lookAroundThreshold;
        }

        public bool IsLookingAt(Transform target, float maxAngle)
        {
            if (IsDead || eyes == null || target == null) return false;

            Vector3 direction = target.position - eyes.position;
            if (Vector3.Angle(eyes.forward, direction) > maxAngle) return false;

            // ¿Hay una pared entre la cámara y el objetivo?
            if (Physics.Raycast(eyes.position, direction.normalized, out RaycastHit hit, direction.magnitude,
                                lineOfSightMask, QueryTriggerInteraction.Ignore))
            {
                return hit.transform.root == target.root;
            }
            return true;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            _health = Mathf.Max(0, _health - amount);
            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(_health, maxHealth);

            if (_health <= 0) Kill();
        }

        public void Kill()
        {
            if (IsDead) return;

            IsDead = true;
            IsMoving = false;
            IsLookingAround = false;

            if (_health != 0)
            {
                _health = 0;
                HealthChanged?.Invoke(_health, maxHealth);
            }

            // Congela al jugador sin tocar el código del Explorer.
            _explorer.enabled = false;
            _noise.enabled = false;
            _tools.enabled = false;

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetBool(IsRunningHash, false);
            }

            if (unlockCursorOnDeath)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Debug.Log("[PlayerCharacter] El jugador murió", this);
            Died?.Invoke();
        }

        // ------------------------------------------------------------------
        // Extras (curación, botiquines...)
        // ------------------------------------------------------------------

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            _health = Mathf.Min(maxHealth, _health + amount);
            HealthChanged?.Invoke(_health, maxHealth);
        }
    }
}
