using UnityEngine;

// Se cansó de que lo miren: corre directo al jugador (enragedSpeed).
// Transiciones planeadas:
//   canAttackPlayer() -> BrackenAttack
public class BrackenEnraged : State
{
    protected BrackenController bracken;

    public BrackenEnraged(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.ENRAGED;
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
