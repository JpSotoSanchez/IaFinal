using UnityEngine;

// Persigue al jugador muy rápido (chaseSpeed).
// Transiciones planeadas:
//   isPlayerLookingAtMe(lookedAtAngle) -> CoilHeadFrozen
//   canAttackPlayer() -> CoilHeadAttack
//   distanceToPlayer() > loseDistance o sin línea de vista por loseTime s -> CoilHeadPatrol
public class CoilHeadChase : State
{
    protected CoilHeadController coilHead;

    public CoilHeadChase(CoilHeadController _coilHead) : base(_coilHead)
    {
        coilHead = _coilHead;
        name = CoilHeadController.STATE.CHASE;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isRunning");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isRunning");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
