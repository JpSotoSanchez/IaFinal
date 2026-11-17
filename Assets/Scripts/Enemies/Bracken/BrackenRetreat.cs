using UnityEngine;

// Huye del jugador porque lo están mirando (retreatSpeed). Mientras lo miran: stareTimer += Time.deltaTime.
// Transiciones planeadas:
//   stareTimer >= maxStareTime -> BrackenEnraged
//   pasan retreatDuration s sin que lo miren -> BrackenStalk (stareTimer = 0)
public class BrackenRetreat : State
{
    protected BrackenController bracken;

    public BrackenRetreat(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.RETREAT;
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
