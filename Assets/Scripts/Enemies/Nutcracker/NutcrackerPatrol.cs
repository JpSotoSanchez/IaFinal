using UnityEngine;

// Patrulla 'a ciegas' (ojo cerrado). Cada Random.Range(scanIntervalMin, scanIntervalMax) s se detiene a escanear.
// Transiciones planeadas:
//   timer -> NutcrackerScan
//   distanceToPlayer() < kickDistance -> patada: playerInfo.Kill()
public class NutcrackerPatrol : Patrol
{
    protected NutcrackerController nutcracker;

    public NutcrackerPatrol(NutcrackerController _nutcracker) : base(_nutcracker)
    {
        nutcracker = _nutcracker;
        name = NutcrackerController.STATE.PATROL;
    }

    public override void Update()
    {
        base.Update();   // movimiento entre waypoints

        // TODO: transiciones (ver arriba)
    }
}
