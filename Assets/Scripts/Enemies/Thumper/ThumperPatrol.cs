using UnityEngine;

// Patrulla. Es sordo: solo reacciona a la vista (canHear = false).
// Transiciones planeadas:
//   canSeePlayer() -> ThumperChase
public class ThumperPatrol : Patrol
{
    protected ThumperController thumper;

    public ThumperPatrol(ThumperController _thumper) : base(_thumper)
    {
        thumper = _thumper;
        name = ThumperController.STATE.PATROL;
    }

    public override void Update()
    {
        base.Update();   // movimiento entre waypoints

        // TODO: transiciones (ver arriba)
    }
}
