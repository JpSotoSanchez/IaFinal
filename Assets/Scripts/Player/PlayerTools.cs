using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalFactory.Player
{
    /// <summary>
    /// Herramientas del jugador contra los enemigos:
    ///   Clic izq. = pala (daño a corta distancia)
    ///   Clic der. = aturdidor (stun a distancia)
    ///   Q         = tirar un objeto (ruido fuerte para distraer)
    /// La F se queda para la linterna del FirstPersonExplorer.
    /// </summary>
    public class PlayerTools : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Desde dónde se apunta. Si se deja vacío se usa la Camera hija")]
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Pala")]
        [SerializeField] private float shovelDamage = 1f;
        [SerializeField] private float shovelRange = 2.5f;
        [SerializeField] private float shovelCooldown = 0.6f;

        [Header("Aturdidor")]
        [SerializeField] private float stunRange = 12f;
        [SerializeField] private float stunDuration = 4f;
        [SerializeField] private float stunCooldown = 3f;

        [Header("Tirar objeto")]
        [SerializeField] private float dropNoiseRadius = 20f;
        [SerializeField] private float dropCooldown = 1f;

        /// <summary>Mensajes cortos para el HUD ("Golpeaste a X", "Aturdidor recargando"...).</summary>
        public event Action<string> Feedback;

        private PlayerNoise _noise;
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private bool _cursorWasLocked;
        private float _nextShovelTime;
        private float _nextStunTime;
        private float _nextDropTime;

        public float StunCooldownRemaining => Mathf.Max(0f, _nextStunTime - Time.time);

        private void Awake()
        {
            _noise = GetComponent<PlayerNoise>();

            if (aimOrigin == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                aimOrigin = cam != null ? cam.transform : transform;
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;

            // El Explorer usa el clic izq. para volver a capturar el cursor: ese clic no debe
            // contar como golpe, por eso solo se usan herramientas si ya estaba capturado.
            if (!_cursorWasLocked || Cursor.lockState != CursorLockMode.Locked) return;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame) UseShovel();
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) UseStunner();
            if (kb != null && kb.qKey.wasPressedThisFrame) DropObject();
        }

        private void LateUpdate()
        {
            _cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            _cursorWasLocked = false;
        }

        // ------------------------------------------------------------------
        // Herramientas
        // ------------------------------------------------------------------

        public void UseShovel()
        {
            if (Time.time < _nextShovelTime) return;
            _nextShovelTime = Time.time + shovelCooldown;

            EnemyController enemy = RaycastEnemy(shovelRange);
            if (enemy == null || enemy.IsDead)
            {
                Send("Golpe de pala al aire");
                return;
            }

            enemy.TakeHit(shovelDamage);
            Send(enemy.invulnerable ? $"La pala no le hace nada a {enemy.name}" : $"Golpe de pala a {enemy.name}");
        }

        public void UseStunner()
        {
            if (Time.time < _nextStunTime)
            {
                Send($"Aturdidor recargando ({StunCooldownRemaining:0.0}s)");
                return;
            }
            _nextStunTime = Time.time + stunCooldown;

            EnemyController enemy = RaycastEnemy(stunRange);
            if (enemy == null || enemy.IsDead)
            {
                Send("El aturdidor no le dio a nada");
                return;
            }

            if (!enemy.canBeStunned)
            {
                Send($"{enemy.name} no se puede aturdir");
                return;
            }

            enemy.Stun(stunDuration);
            Send($"Aturdiste a {enemy.name}");
        }

        public void DropObject()
        {
            if (Time.time < _nextDropTime) return;
            _nextDropTime = Time.time + dropCooldown;

            if (_noise != null) _noise.EmitNoise(dropNoiseRadius);
            else NoiseSystem.Emit(transform.position, dropNoiseRadius);
            Send("Tiraste un objeto (ruido)");
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        // Primer objeto que toca el rayo (ignorando al propio jugador). Si es un enemigo lo regresa;
        // si es una pared u otra cosa regresa null.
        private EnemyController RaycastEnemy(float range)
        {
            int count = Physics.RaycastNonAlloc(aimOrigin.position, aimOrigin.forward, _hits, range,
                                                hitMask, QueryTriggerInteraction.Ignore);

            RaycastHit? closest = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (closest == null || hit.distance < closest.Value.distance) closest = hit;
            }

            return closest?.collider.GetComponentInParent<EnemyController>();
        }

        private void Send(string message)
        {
            Debug.Log("[PlayerTools] " + message, this);
            Feedback?.Invoke(message);
        }
    }
}
