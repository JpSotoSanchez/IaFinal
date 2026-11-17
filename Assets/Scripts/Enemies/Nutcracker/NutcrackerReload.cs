using UnityEngine;

// Recarga durante reloadTime s (momento en que el jugador puede atacarlo).
// Transiciones planeadas:
//   termina -> shellsLoaded = shotsPerReload -> NutcrackerHunt
public class NutcrackerReload : State
{
    protected NutcrackerController nutcracker;

    public NutcrackerReload(NutcrackerController _nutcracker) : base(_nutcracker)
    {
        nutcracker = _nutcracker;
        name = NutcrackerController.STATE.RELOAD;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isReloading");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isReloading");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
