using UnityEngine;

// Patrulla buscando al jugador.
// Transiciones planeadas:
//   canSeePlayer() -> BrackenStalk
public class BrackenPatrol : Patrol
{
    protected BrackenController bracken;

    public BrackenPatrol(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.PATROL;
    }

    public override void Update()
    {
        base.Update();   // movimiento entre waypoints

        // TODO: transiciones (ver arriba)
    }
}
