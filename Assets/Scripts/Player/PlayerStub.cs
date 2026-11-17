using UnityEngine;
using UnityEngine.InputSystem;

// JUGADOR PROVISIONAL para probar a los enemigos. Lo reemplaza el jugador real del equipo;
// lo único que los enemigos necesitan es que el jugador implemente IPlayer y tenga tag "Player".
//
// Controles:
//   WASD mover · Mouse mirar · Shift correr (mucho ruido) · Ctrl agacharse (sin ruido)
//   Q tirar objeto (ruido fuerte) · Clic izq. pala · Clic der./F aturdidor · Esc soltar mouse
[RequireComponent(typeof(CharacterController))]
public class PlayerStub : MonoBehaviour, IPlayer
{
    [Header("Referencias")]
    public Transform cameraPivot;

    [Header("Movimiento")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float mouseSensitivity = 0.1f;
    public float gravity = -20f;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public float cameraHeight = 0.6f;     // respecto al centro del jugador

    [Header("Vida")]
    public int maxHealth = 100;

    [Header("Ruido (radio en metros)")]
    public float walkNoiseRadius = 5f;
    public float runNoiseRadius = 15f;
    public float dropNoiseRadius = 20f;
    public float stepInterval = 0.4f;

    [Header("Herramientas")]
    public float shovelDamage = 1f;
    public float shovelRange = 2.5f;
    public float stunRange = 12f;
    public float stunDuration = 4f;

    CharacterController characterController;
    float pitch;
    float verticalVelocity;
    float stepTimer;
    int health;
    bool isCrouching;
    bool isRunning;
    string lastMessage = "";

    public Transform Eyes => cameraPivot;
    public bool IsMoving { get; private set; }
    public bool IsLookingAround { get; private set; }
    public bool IsDead { get; private set; }
    public int Health => health;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        health = maxHealth;
        if (cameraPivot == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraPivot = cam.transform;
        }
    }

    void Start()
    {
        LockCursor(true);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
            return;
        }

        if (IsDead)
        {
            IsMoving = false;
            IsLookingAround = false;
            return;
        }

        Look(mouse);
        Move(kb);
        Tools(kb, mouse);
    }

    void Look(Mouse mouse)
    {
        Vector2 delta = Cursor.lockState == CursorLockMode.Locked ? mouse.delta.ReadValue() : Vector2.zero;
        IsLookingAround = delta.sqrMagnitude > 4f;

        transform.Rotate(0f, delta.x * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move(Keyboard kb)
    {
        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        if (kb.aKey.isPressed) input.x -= 1;
        input = Vector2.ClampMagnitude(input, 1f);

        isCrouching = kb.leftCtrlKey.isPressed;
        isRunning = kb.leftShiftKey.isPressed && !isCrouching;
        IsMoving = input.sqrMagnitude > 0.01f;

        // Altura (agacharse)
        float altura = isCrouching ? crouchHeight : standHeight;
        characterController.height = altura;
        characterController.center = new Vector3(0f, -(standHeight - altura) * 0.5f, 0f);
        if (cameraPivot != null)
            cameraPivot.localPosition = new Vector3(0f, cameraHeight - (standHeight - altura), 0f);

        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        if (characterController.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime);

        // Ruido de pasos
        if (IsMoving && !isCrouching)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                NoiseSystem.Emit(transform.position, isRunning ? runNoiseRadius : walkNoiseRadius);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void Tools(Keyboard kb, Mouse mouse)
    {
        if (kb.qKey.wasPressedThisFrame)
        {
            NoiseSystem.Emit(transform.position, dropNoiseRadius);
            Message("Tiraste un objeto (ruido)");
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            EnemyController enemy = RaycastEnemy(shovelRange);
            if (enemy != null)
            {
                enemy.TakeHit(shovelDamage);
                Message($"Golpe de pala a {enemy.name}");
            }
        }

        if (mouse.rightButton.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)
        {
            EnemyController enemy = RaycastEnemy(stunRange);
            if (enemy != null)
            {
                enemy.Stun(stunDuration);
                Message($"Aturdiste a {enemy.name}");
            }
        }
    }

    EnemyController RaycastEnemy(float range)
    {
        if (cameraPivot == null) return null;
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit, range, ~0,
                            QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<EnemyController>();
        }
        return null;
    }

    // ------------------------------------------------------------------
    // IPlayer
    // ------------------------------------------------------------------

    public bool IsLookingAt(Transform target, float maxAngle)
    {
        if (cameraPivot == null || target == null) return false;

        Vector3 punto = target.position;
        Vector3 direccion = punto - cameraPivot.position;
        if (Vector3.Angle(cameraPivot.forward, direccion) > maxAngle) return false;

        // ¿Hay una pared entre la cámara y el objetivo?
        if (Physics.Raycast(cameraPivot.position, direccion.normalized, out RaycastHit hit, direccion.magnitude,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == target.root;
        }
        return true;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        health = Mathf.Max(0, health - amount);
        Message($"Recibiste {amount} de daño");
        if (health <= 0) Kill();
    }

    public void Kill()
    {
        if (IsDead) return;
        health = 0;
        IsDead = true;
        Message("MORISTE (provisional: aquí iría el game over)");
        Debug.Log("[PlayerStub] El jugador murió");
    }

    // ------------------------------------------------------------------
    // HUD provisional
    // ------------------------------------------------------------------

    void Message(string text)
    {
        lastMessage = text;
        Debug.Log("[PlayerStub] " + text);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 330, 120), GUIContent.none);
        GUI.Label(new Rect(20, 15, 320, 20), $"Vida: {health}/{maxHealth}   " +
                  (isCrouching ? "AGACHADO" : isRunning ? "CORRIENDO" : "CAMINANDO"));
        GUI.Label(new Rect(20, 35, 320, 20), "WASD mover · Shift correr · Ctrl agacharse");
        GUI.Label(new Rect(20, 55, 320, 20), "Q tirar objeto · Clic izq. pala · Clic der./F aturdir");
        GUI.Label(new Rect(20, 75, 320, 20), "Esc soltar mouse · Clic para volver a capturarlo");
        GUI.Label(new Rect(20, 100, 320, 20), lastMessage);

        // mira
        GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
    }
}
