using UnityEngine;

// Le rompe el cuello al jugador: playerInfo.Kill().
// Transiciones planeadas:
//   -> BrackenDragBody
public class BrackenAttack : State
{
    protected BrackenController bracken;

    public BrackenAttack(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.ATTACK;
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
