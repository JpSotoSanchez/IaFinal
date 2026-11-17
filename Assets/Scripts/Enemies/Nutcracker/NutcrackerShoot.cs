using UnityEngine;

// Apunta (aimTime) y dispara un raycast hacia el jugador: mata de un tiro. shellsLoaded--.
// Transiciones planeadas:
//   shellsLoaded == 0 -> NutcrackerReload
//   pierde línea de vista -> NutcrackerHunt
public class NutcrackerShoot : State
{
    protected NutcrackerController nutcracker;

    public NutcrackerShoot(NutcrackerController _nutcracker) : base(_nutcracker)
    {
        nutcracker = _nutcracker;
        name = NutcrackerController.STATE.SHOOT;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isShooting");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isShooting");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
