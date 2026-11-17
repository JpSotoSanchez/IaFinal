using UnityEngine;

// Golpes rápidos: playerInfo.TakeDamage(attackDamage) cada attackCooldown s.
// Transiciones planeadas:
//   isPlayerLookingAtMe(lookedAtAngle) -> CoilHeadFrozen
//   el jugador se aleja -> CoilHeadChase
//   jugador muerto -> CoilHeadPatrol
public class CoilHeadAttack : State
{
    protected CoilHeadController coilHead;

    public CoilHeadAttack(CoilHeadController _coilHead) : base(_coilHead)
    {
        coilHead = _coilHead;
        name = CoilHeadController.STATE.ATTACK;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isAttacking");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isAttacking");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
