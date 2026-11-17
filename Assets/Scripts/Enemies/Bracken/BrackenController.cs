using UnityEngine;

// Bracken: acecha por la espalda. Si lo miras huye; si lo miras más de 5 s se enoja y te mata.
// Mata de un golpe y arrastra el cuerpo a su lugar favorito. Vulnerable a pala (~5 golpes) y aturdidor.
// Estados:
//   BrackenPatrol
//   BrackenStalk
//   BrackenRetreat
//   BrackenEnraged
//   BrackenAttack
//   BrackenDragBody
//   Stunned (común)
public class BrackenController : EnemyController
{
    public enum STATE
    {
        PATROL, STALK, RETREAT, ENRAGED, ATTACK, DRAG_BODY
    };

    [Header("Bracken")]
    public float stalkSpeed = 3.5f;
    public float retreatSpeed = 6f;
    public float enragedSpeed = 8f;
    [Tooltip("Distancia detrás del jugador a la que intenta colocarse")]
    public float stalkDistance = 2f;
    [Tooltip("Ángulo de la vista del jugador para considerar que lo está mirando")]
    public float lookedAtAngle = 25f;
    [Tooltip("Segundos de contacto visual antes de enojarse")]
    public float maxStareTime = 5f;
    public float retreatDuration = 4f;
    [Tooltip("Lugar a donde arrastra los cuerpos")]
    public Transform favoriteSpot;

    [Header("Bracken (runtime)")]
    public float stareTimer;     // cuánto tiempo lo han mirado

    // Valores por defecto al agregar el componente (o al hacer Reset en el Inspector)
    void Reset()
    {
        visionDistance = 15f;
        visionAngle = 60f;
        attackDistance = 1.5f;
        maxHealth = 5f;
        patrolSpeed = 2.5f;
    }

    protected override State CreateInitialState() => new BrackenPatrol(this);
    public override State CreateDefaultState() => new BrackenPatrol(this);
}
