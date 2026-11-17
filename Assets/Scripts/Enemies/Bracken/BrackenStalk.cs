using UnityEngine;

// Se acerca por la espalda del jugador sin que lo vea (punto detrás del jugador a stalkDistance, stalkSpeed).
// Transiciones planeadas:
//   isPlayerLookingAtMe(lookedAtAngle) -> BrackenRetreat
//   llega a su espalda (canAttackPlayer() y no lo mira) -> BrackenAttack
//   pierde al jugador -> BrackenPatrol
public class BrackenStalk : State
{
    protected BrackenController bracken;

    public BrackenStalk(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.STALK;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isSneaking");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isSneaking");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
