using UnityEngine;
using UnityEngine.AI;

// Base de los controllers de los enemigos (equivale al StateController de clase).
// Guarda el estado actual, la configuración del Inspector, la vida y el stun.
// Cada enemigo hereda de aquí (BrackenController, CoilHeadController, ...).
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío se busca el objeto con tag Player")]
    public Transform player;

    [Header("Vista")]
    public float visionDistance = 10f;
    public float visionAngle = 45f;
    [Tooltip("Altura de los ojos respecto al pivote del objeto")]
    public float eyeHeight = 0.6f;
    public LayerMask visionMask = ~0;

    [Header("Oído")]
    public bool canHear = false;
    [Tooltip("Multiplica el radio de los ruidos que escucha")]
    public float hearingMultiplier = 1f;

    [Header("Combate")]
    public float attackDistance = 1.5f;

    [Header("Vida")]
    public float maxHealth = 5f;
    public bool invulnerable = false;
    public bool canBeStunned = true;

    [Header("Patrulla")]
    [Tooltip("Si no hay waypoints, deambula a puntos aleatorios dentro de wanderRadius")]
    public Transform[] waypoints;
    public float wanderRadius = 15f;
    public float patrolSpeed = 2f;

    [Header("Debug")]
    [SerializeField] string currentStateName;
    [SerializeField] float currentHealth;

    public NavMeshAgent Agent { get; private set; }
    public Animator Animator { get; private set; }
    public Transform Player => player;
    public IPlayer PlayerInfo { get; private set; }
    public bool IsDead { get; private set; }

    protected State currentState;

    bool hasNoise;
    Vector3 lastNoisePosition;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    protected virtual void OnEnable()
    {
        if (canHear) NoiseSystem.OnNoise += OnNoise;
    }

    protected virtual void OnDisable()
    {
        NoiseSystem.OnNoise -= OnNoise;
    }

    protected virtual void Start()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (player != null) PlayerInfo = player.GetComponentInParent<IPlayer>();
        else Debug.LogWarning($"{name}: no encontré al jugador (tag Player)", this);

        currentState = CreateInitialState();
    }

    protected virtual void Update()
    {
        if (IsDead || currentState == null) return;
        currentState = currentState.Process();
        currentStateName = currentState != null && currentState.name != null ? currentState.name.ToString() : "-";
    }

    // Estado con el que arranca el enemigo.
    protected abstract State CreateInitialState();

    // Estado al que regresa después de un stun (normalmente su patrulla).
    public abstract State CreateDefaultState();

    // Cambia de estado desde fuera de la máquina (stun, daño, alerta de otro enemigo...).
    public void ForceState(State next)
    {
        if (IsDead || next == null) return;
        currentState?.Exit();
        currentState = next;
    }

    // ------------------------------------------------------------------
    // Vida y stun (pala / aturdidor del jugador)
    // ------------------------------------------------------------------

    protected virtual bool CanBeDamaged() => !invulnerable;

    public virtual void TakeHit(float damage)
    {
        if (IsDead || !CanBeDamaged()) return;
        currentHealth -= damage;
        Debug.Log($"{name} recibió {damage} de daño ({currentHealth}/{maxHealth})");
        if (currentHealth <= 0) Die();
    }

    public virtual void Stun(float duration)
    {
        if (IsDead || !canBeStunned) return;
        Debug.Log($"{name} aturdido {duration}s");
        ForceState(new Stunned(this, duration));
    }

    protected virtual void Die()
    {
        IsDead = true;
        currentState?.Exit();
        currentState = null;
        currentStateName = "DEAD";
        if (Agent.isOnNavMesh) Agent.isStopped = true;
        Agent.enabled = false;
        // Animator?.SetTrigger("isDead");
        transform.Rotate(90f, 0f, 0f); // provisional: se "cae" mientras no hay animación
        Debug.Log($"{name} murió");
    }

    // ------------------------------------------------------------------
    // Oído
    // ------------------------------------------------------------------

    void OnNoise(Vector3 position, float radius)
    {
        if (IsDead) return;
        if (Vector3.Distance(transform.position, position) <= radius * hearingMultiplier)
        {
            hasNoise = true;
            lastNoisePosition = position;
        }
    }

    // Regresa el último ruido escuchado (si hay) y lo marca como atendido.
    public bool TryConsumeNoise(out Vector3 position)
    {
        position = lastNoisePosition;
        if (!hasNoise) return false;
        hasNoise = false;
        return true;
    }

    // ------------------------------------------------------------------
    // Gizmos: cono de visión y radio de ataque al seleccionar el enemigo
    // ------------------------------------------------------------------

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 ojos = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Vector3 izq = Quaternion.Euler(0, -visionAngle, 0) * transform.forward * visionDistance;
        Vector3 der = Quaternion.Euler(0, visionAngle, 0) * transform.forward * visionDistance;
        Gizmos.DrawLine(ojos, ojos + izq);
        Gizmos.DrawLine(ojos, ojos + der);
        Gizmos.DrawLine(ojos + izq, ojos + der);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
