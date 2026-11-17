using UnityEngine;

// Persigue al jugador que recuerda aunque ya no se mueva (huntSpeed, actualiza lastKnownPlayerPosition).
// Transiciones planeadas:
//   canSeePlayer() && distanceToPlayer() < shootRange -> NutcrackerShoot
//   distanceToPlayer() < kickDistance -> patada: playerInfo.Kill()
//   distanceToPlayer() > loseDistance -> NutcrackerPatrol
public class NutcrackerHunt : State
{
    protected NutcrackerController nutcracker;

    public NutcrackerHunt(NutcrackerController _nutcracker) : base(_nutcracker)
    {
        nutcracker = _nutcracker;
        name = NutcrackerController.STATE.HUNT;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isWalking");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isWalking");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
