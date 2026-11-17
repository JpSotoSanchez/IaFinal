using UnityEngine;

// Coil-Head: el más rápido persiguiendo, pero no se mueve mientras lo miras.
// No mata de un golpe pero ataca muy rápido. Invulnerable al daño; sí se puede aturdir.
// Estados:
//   CoilHeadPatrol
//   CoilHeadChase
//   CoilHeadFrozen
//   CoilHeadAttack
//   Stunned (común)
public class CoilHeadController : EnemyController
{
    public enum STATE
    {
        PATROL, CHASE, FROZEN, ATTACK
    };

    [Header("Coil-Head")]
    public float chaseSpeed = 9f;
    [Tooltip("Ángulo de la vista del jugador para considerar que lo está mirando")]
    public float lookedAtAngle = 35f;
    public int attackDamage = 20;
    public float attackCooldown = 0.4f;
    [Tooltip("Si el jugador está más lejos que esto, lo pierde")]
    public float loseDistance = 25f;
    [Tooltip("Segundos sin línea de vista para perderlo")]
    public float loseTime = 5f;

    // Valores por defecto al agregar el componente (o al hacer Reset en el Inspector)
    void Reset()
    {
        visionDistance = 20f;
        visionAngle = 70f;
        attackDistance = 1.5f;
        patrolSpeed = 2.5f;
    }

    protected override void Awake()
    {
        invulnerable = true;   // no recibe daño, pero canBeStunned sigue en true
        base.Awake();
    }

    protected override State CreateInitialState() => new CoilHeadPatrol(this);
    public override State CreateDefaultState() => new CoilHeadPatrol(this);
}
