using System;
using LethalFactory.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalFactory.Collectibles
{
    /// <summary>
    /// Objeto que el jugador puede recoger. Se pone en cualquier objeto de la escena
    /// (un modelo, un cubo...) y CollectibleManager lo cuenta solo.
    ///
    /// Recoger:
    ///   - requireKeyPress = false → basta con tocarlo.
    ///   - requireKeyPress = true  → hay que estar cerca y presionar E.
    ///
    /// Si el objeto no tiene un collider trigger, se le agrega una SphereCollider trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public class Collectible : MonoBehaviour
    {
        /// <summary>Se lanza cuando cualquier coleccionable es recogido.</summary>
        public static event Action<Collectible> AnyCollected;

        [Header("Datos")]
        [SerializeField] private string displayName = "Chatarra";

        [Header("Recoger")]
        [SerializeField] private bool requireKeyPress = true;
        [Tooltip("Radio del trigger que se crea si el objeto no tiene uno")]
        [SerializeField] private float pickupRadius = 1.5f;

        [Header("Ruido al recoger (atrae enemigos, 0 = sin ruido)")]
        [SerializeField] private float pickupNoiseRadius = 6f;

        [Header("Feedback")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;
        [Tooltip("Efecto que se instancia al recogerlo (opcional)")]
        [SerializeField] private GameObject pickupEffect;

        [Header("Animación idle")]
        [SerializeField] private bool animate = true;
        [SerializeField] private float spinSpeed = 60f;
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 2f;

        private PlayerCharacter _playerInRange;
        private Vector3 _startPosition;
        private bool _collected;

        public string DisplayName => displayName;
        public bool IsCollected => _collected;

        private void Awake()
        {
            EnsureTrigger();
        }

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            if (_collected) return;

            if (animate)
            {
                transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = _startPosition + Vector3.up * offset;
            }

            if (requireKeyPress && _playerInRange != null)
            {
                if (_playerInRange.IsDead)
                {
                    _playerInRange = null;
                    return;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame)
                    Collect(_playerInRange);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null || player.IsDead) return;

            if (requireKeyPress) _playerInRange = player;
            else Collect(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_playerInRange != null && other.GetComponentInParent<PlayerCharacter>() == _playerInRange)
                _playerInRange = null;
        }

        /// <summary>Recoge el objeto (se puede llamar desde otros scripts).</summary>
        public void Collect(PlayerCharacter by)
        {
            if (_collected) return;
            _collected = true;
            _playerInRange = null;

            if (pickupNoiseRadius > 0f)
                NoiseSystem.Emit(transform.position, pickupNoiseRadius);

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Debug.Log($"[Collectible] Recogiste {displayName}", this);
            AnyCollected?.Invoke(this);

            gameObject.SetActive(false);
        }

        private void EnsureTrigger()
        {
            foreach (Collider col in GetComponents<Collider>())
            {
                if (col.isTrigger) return;
            }

            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;

            // El radio del SphereCollider está en espacio local: se compensa la escala.
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            trigger.radius = scale > 0f ? pickupRadius / scale : pickupRadius;
        }

        private void OnGUI()
        {
            if (_collected || !requireKeyPress || _playerInRange == null) return;

            const float width = 260f;
            Rect rect = new Rect((Screen.width - width) / 2f, Screen.height / 2f + 30f, width, 24f);
            GUI.Box(rect, $"[E] Recoger {displayName}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
