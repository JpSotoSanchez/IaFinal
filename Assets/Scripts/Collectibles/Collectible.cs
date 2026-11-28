using System;
using LethalFactory.Player;
using UnityEngine;

namespace LethalFactory.Collectibles
{
    /// <summary>
    /// Objeto que el jugador recoge con solo tocarlo.
    /// Necesita un Collider con "Is Trigger" activado.
    /// Al recogerlo, PlayerHUD muestra "Recogiste {displayName}".
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        /// <summary>Se lanza al recoger cualquier coleccionable (lo escucha PlayerHUD).</summary>
        public static event Action<Collectible> AnyCollected;

        [SerializeField] private string displayName = "Chatarra";
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

        public string DisplayName => displayName;

        private void OnTriggerEnter(Collider other)
        {
            // Solo el jugador puede recogerlo
            if (other.GetComponentInParent<PlayerCharacter>() == null) return;

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

            AnyCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
