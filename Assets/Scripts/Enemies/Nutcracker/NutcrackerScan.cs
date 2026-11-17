using UnityEngine;

// Se detiene y abre el ojo (eyeOpen = true) durante scanDuration s buscando movimiento.
// Transiciones planeadas:
//   canSeePlayer() && (playerInfo.IsMoving || playerInfo.IsLookingAround) -> NutcrackerHunt
//   termina scanDuration -> NutcrackerPatrol (eyeOpen = false)
public class NutcrackerScan : State
{
    protected NutcrackerController nutcracker;

    public NutcrackerScan(NutcrackerController _nutcracker) : base(_nutcracker)
    {
        nutcracker = _nutcracker;
        name = NutcrackerController.STATE.SCAN;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isScanning");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isScanning");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
