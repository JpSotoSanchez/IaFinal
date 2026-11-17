using UnityEngine;

// Se congela por completo mientras el jugador lo mira (agent.isStopped = true, animator.speed = 0).
// Transiciones planeadas:
//   el jugador deja de mirarlo -> CoilHeadChase
public class CoilHeadFrozen : State
{
    protected CoilHeadController coilHead;

    public CoilHeadFrozen(CoilHeadController _coilHead) : base(_coilHead)
    {
        coilHead = _coilHead;
        name = CoilHeadController.STATE.FROZEN;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isFrozen");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isFrozen");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
