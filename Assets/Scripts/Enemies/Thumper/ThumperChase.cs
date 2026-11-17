using UnityEngine;

// Persigue acelerando en línea recta hasta maxChaseSpeed; si gira más de turnAngleThreshold frena a baseChaseSpeed.
// Transiciones planeadas:
//   canAttackPlayer() -> ThumperAttack
//   !canSeePlayer() -> guarda lastSeenPosition -> ThumperSearch
public class ThumperChase : State
{
    protected ThumperController thumper;

    public ThumperChase(ThumperController _thumper) : base(_thumper)
    {
        thumper = _thumper;
        name = ThumperController.STATE.CHASE;
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
