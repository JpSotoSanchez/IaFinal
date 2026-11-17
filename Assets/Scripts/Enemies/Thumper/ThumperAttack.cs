using UnityEngine;

// playerInfo.TakeDamage(attackDamage) cada attackCooldown s.
// Transiciones planeadas:
//   el jugador se aleja -> ThumperChase
//   jugador muerto -> ThumperPatrol
public class ThumperAttack : State
{
    protected ThumperController thumper;

    public ThumperAttack(ThumperController _thumper) : base(_thumper)
    {
        thumper = _thumper;
        name = ThumperController.STATE.ATTACK;
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
