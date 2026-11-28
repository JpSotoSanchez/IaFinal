using UnityEngine;

namespace LethalFactory.Player
{
    /// <summary>
    /// HUD provisional con OnGUI: vida, estado, controles, último mensaje y mira.
    /// Solo escucha los eventos de PlayerCharacter y PlayerTools; se puede quitar o
    /// reemplazar por un Canvas sin afectar al resto.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private bool showControls = true;
        [SerializeField] private float messageDuration = 2.5f;

        private PlayerCharacter _character;
        private PlayerNoise _noise;
        private PlayerTools _tools;
        private string _message = "";
        private float _messageUntil;
        private Texture2D _pixel;

        private void Awake()
        {
            _character = GetComponent<PlayerCharacter>();
            _noise = GetComponent<PlayerNoise>();
            _tools = GetComponent<PlayerTools>();

            _pixel = new Texture2D(1, 1);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }

        private void OnEnable()
        {
            _character.Damaged += OnDamaged;
            _character.Died += OnDied;
            if (_tools != null) _tools.Feedback += ShowMessage;
        }

        private void OnDisable()
        {
            _character.Damaged -= OnDamaged;
            _character.Died -= OnDied;
            if (_tools != null) _tools.Feedback -= ShowMessage;
        }

        private void OnDestroy()
        {
            if (_pixel != null) Destroy(_pixel);
        }

        private void OnDamaged(int amount) => ShowMessage($"Recibiste {amount} de daño");

        private void OnDied() => ShowMessage("MORISTE", float.PositiveInfinity);

        private void ShowMessage(string text) => ShowMessage(text, messageDuration);

        private void ShowMessage(string text, float duration)
        {
            _message = text;
            _messageUntil = Time.time + duration;
        }

        private void OnGUI()
        {
            float height = showControls ? 120f : 62f;
            GUI.Box(new Rect(10, 10, 340, height), GUIContent.none);

            // Barra de vida
            float ratio = _character.MaxHealth > 0 ? (float)_character.Health / _character.MaxHealth : 0f;
            Rect bar = new Rect(20, 20, 200, 14);
            DrawRect(bar, new Color(0f, 0f, 0f, 0.6f));
            DrawRect(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), Color.Lerp(Color.red, Color.green, ratio));
            GUI.Label(new Rect(230, 17, 120, 20), $"{_character.Health}/{_character.MaxHealth}  {StateLabel()}");

            float y = 40f;
            if (showControls)
            {
                GUI.Label(new Rect(20, y, 330, 20), "WASD mover · Shift correr · F linterna");
                GUI.Label(new Rect(20, y + 18, 330, 20), "Clic izq. pala · Clic der. aturdidor · Q tirar objeto");
                GUI.Label(new Rect(20, y + 36, 330, 20), "Esc soltar mouse · Clic para volver a capturarlo");
                y += 58f;
            }

            if (Time.time < _messageUntil)
                GUI.Label(new Rect(20, y, 330, 20), _message);

            // Mira
            if (!_character.IsDead && Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }

        private string StateLabel()
        {
            if (_character.IsDead) return "MUERTO";
            if (_noise != null && _noise.IsRunning) return "CORRIENDO";
            return _character.IsMoving ? "CAMINANDO" : "QUIETO";
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = previous;
        }
    }
}
