using UnityEngine;

// Nutcracker: patrulla a ciegas y a ratos abre su ojo para buscar MOVIMIENTO. Recuerda al último que vio
// aunque deje de moverse. Escopeta: mata de un tiro, recarga cada 2 disparos. Patada si lo tocas.
// Solo recibe daño con el ojo abierto (~5 golpes de pala); vulnerable al aturdidor.
// Estados:
//   NutcrackerPatrol
//   NutcrackerScan
//   NutcrackerHunt
//   NutcrackerShoot
//   NutcrackerReload
//   Stunned (común)
public class NutcrackerController : EnemyController
{
    public enum STATE
    {
        PATROL, SCAN, HUNT, SHOOT, RELOAD
    };

    [Header("Nutcracker")]
    public float scanIntervalMin = 6f;
    public float scanIntervalMax = 12f;
    public float scanDuration = 3f;
    public float huntSpeed = 4f;
    public float shootRange = 15f;
    [Tooltip("Tiempo apuntando antes de disparar (sonido del martillo)")]
    public float aimTime = 0.8f;
    public float timeBetweenShots = 1.2f;
    public int shotsPerReload = 2;
    public float reloadTime = 3f;
    public float kickDistance = 1.2f;
    [Tooltip("Si el jugador está más lejos que esto, lo pierde")]
    public float loseDistance = 25f;

    [Header("Nutcracker (runtime)")]
    public bool eyeOpen;
    public int shellsLoaded = 2;
    public Vector3 lastKnownPlayerPosition;

    // Valores por defecto al agregar el componente (o al hacer Reset en el Inspector)
    void Reset()
    {
        visionDistance = 20f;
        visionAngle = 70f;
        attackDistance = 1.2f;
        maxHealth = 5f;
        patrolSpeed = 2f;
    }

    protected override State CreateInitialState() => new NutcrackerPatrol(this);
    public override State CreateDefaultState() => new NutcrackerPatrol(this);

    // Solo recibe daño con el ojo abierto
    protected override bool CanBeDamaged() => base.CanBeDamaged() && eyeOpen;
}
