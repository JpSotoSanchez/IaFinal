using UnityEngine;

// Thumper: sordo, caza solo con la vista. Muy rápido en línea recta y lento en las curvas.
// Si pierde al jugador va a su última posición vista. ~3 golpes para matar al jugador; ~4 golpes de pala.
// Estados:
//   ThumperPatrol
//   ThumperChase
//   ThumperAttack
//   ThumperSearch
//   Stunned (común)
public class ThumperController : EnemyController
{
    public enum STATE
    {
        PATROL, CHASE, ATTACK, SEARCH
    };

    [Header("Thumper")]
    public float baseChaseSpeed = 4f;
    public float maxChaseSpeed = 12f;
    [Tooltip("Velocidad que gana por segundo mientras va en línea recta")]
    public float acceleration = 5f;
    [Tooltip("Si el giro hacia el jugador supera este ángulo, pierde la velocidad")]
    public float turnAngleThreshold = 30f;
    public int attackDamage = 34;
    public float attackCooldown = 1f;
    public float searchDuration = 6f;

    [Header("Thumper (runtime)")]
    public Vector3 lastSeenPosition;

    // Valores por defecto al agregar el componente (o al hacer Reset en el Inspector)
    void Reset()
    {
        visionDistance = 25f;
        visionAngle = 60f;
        attackDistance = 1.6f;
        maxHealth = 4f;
        patrolSpeed = 3f;
    }

    protected override State CreateInitialState() => new ThumperPatrol(this);
    public override State CreateDefaultState() => new ThumperPatrol(this);

    public override void TakeHit(float damage)
    {
        base.TakeHit(damage);
        // TODO: al recibir un golpe pierde la aceleración (Agent.speed = baseChaseSpeed)
    }
}
