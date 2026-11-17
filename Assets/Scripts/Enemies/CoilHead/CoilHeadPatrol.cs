using UnityEngine;

// Patrulla buscando al jugador.
// Transiciones planeadas:
//   isPlayerLookingAtMe(lookedAtAngle) -> CoilHeadFrozen
//   canSeePlayer() -> CoilHeadChase
public class CoilHeadPatrol : Patrol
{
    protected CoilHeadController coilHead;

    public CoilHeadPatrol(CoilHeadController _coilHead) : base(_coilHead)
    {
        coilHead = _coilHead;
        name = CoilHeadController.STATE.PATROL;
    }

    public override void Update()
    {
        base.Update();   // movimiento entre waypoints

        // TODO: transiciones (ver arriba)
    }
}
