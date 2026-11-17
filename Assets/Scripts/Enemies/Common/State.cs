using UnityEngine;
using UnityEngine.AI;

// Clase base de todos los estados de todos los enemigos.
// Mismo patrón de clase: cada estado pasa por ENTER -> UPDATE -> EXIT y Process()
// regresa el siguiente estado. Para cambiar de estado se asigna nextState y stage = EXIT
// (o se usa ChangeState(), que hace exactamente eso).
public abstract class State
{
    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    };

    // Estados que comparten todos los enemigos. Cada enemigo tiene además su propio enum STATE.
    public enum COMMON
    {
        PATROL, STUNNED
    };

    public System.Enum name;          // COMMON.X o <Enemigo>Controller.STATE.X
    protected EVENT stage;
    protected State nextState;

    protected EnemyController controller;
    protected GameObject npc;
    protected NavMeshAgent agent;
    protected Animator animator;      // puede ser null mientras no haya modelo con Animator
    protected Transform player;
    protected IPlayer playerInfo;     // puede ser null si el jugador no implementa IPlayer

    public State(EnemyController _controller)
    {
        controller = _controller;
        npc = _controller.gameObject;
        agent = _controller.Agent;
        animator = _controller.Animator;
        player = _controller.Player;
        playerInfo = _controller.PlayerInfo;
        stage = EVENT.ENTER;
    }

    public virtual void Enter() { stage = EVENT.UPDATE; }
    public virtual void Update() { stage = EVENT.UPDATE; }
    public virtual void Exit() { stage = EVENT.EXIT; }

    public State Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }
        return this;
    }

    protected void ChangeState(State next)
    {
        nextState = next;
        stage = EVENT.EXIT;
    }

    // ------------------------------------------------------------------
    // Sensores (los valores salen del controller, configurables en el Inspector)
    // ------------------------------------------------------------------

    protected Vector3 EyePosition => npc.transform.position + Vector3.up * controller.eyeHeight;

    protected Vector3 PlayerEyePosition
    {
        get
        {
            if (playerInfo != null && playerInfo.Eyes != null) return playerInfo.Eyes.position;
            return player.position + Vector3.up * 0.6f;
        }
    }

    protected bool PlayerAvailable => player != null && (playerInfo == null || !playerInfo.IsDead);

    public float distanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(npc.transform.position, player.position);
    }

    // Cono de visión + raycast para que no vea a través de las paredes.
    public bool canSeePlayer()
    {
        return canSeePlayer(controller.visionDistance, controller.visionAngle);
    }

    public bool canSeePlayer(float distance, float angle)
    {
        if (!PlayerAvailable) return false;

        Vector3 direccion = PlayerEyePosition - EyePosition;
        if (direccion.magnitude > distance) return false;

        Vector3 plana = direccion;
        plana.y = 0;
        if (Vector3.Angle(npc.transform.forward, plana) > angle) return false;

        return hasLineOfSight();
    }

    // ¿Hay algo entre los ojos del enemigo y el jugador?
    public bool hasLineOfSight()
    {
        if (!PlayerAvailable) return false;

        Vector3 origen = EyePosition;
        Vector3 direccion = PlayerEyePosition - origen;
        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, direccion.magnitude,
                            controller.visionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == player.root;
        }
        return true;
    }

    public bool canAttackPlayer()
    {
        return PlayerAvailable && distanceToPlayer() < controller.attackDistance;
    }

    public bool isPlayerClose(float distancia)
    {
        return PlayerAvailable && distanceToPlayer() < distancia;
    }

    // ¿El jugador está mirando a este enemigo? (Coil-Head y Bracken)
    public bool isPlayerLookingAtMe(float maxAngle)
    {
        if (!PlayerAvailable || playerInfo == null) return false;
        return playerInfo.IsLookingAt(npc.transform, maxAngle);
    }

    // ¿El enemigo escuchó algo? (solo enemigos con canHear = true, p. ej. Eyeless Dog)
    public bool heardNoise(out Vector3 posicion)
    {
        return controller.TryConsumeNoise(out posicion);
    }

    // Gira al npc hacia un punto en el plano horizontal.
    protected void LookAt(Vector3 punto, float rotationSpeed)
    {
        Vector3 direccion = punto - npc.transform.position;
        direccion.y = 0;
        if (direccion.sqrMagnitude < 0.0001f) return;
        npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation,
            Quaternion.LookRotation(direccion), Time.deltaTime * rotationSpeed);
    }
}
