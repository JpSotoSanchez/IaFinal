using UnityEngine;

// Deambula sin haber oído nada. Es ciego: nunca usa canSeePlayer().
// Transiciones planeadas:
//   heardNoise(out pos) y pos está a menos de hostileNoiseDistance -> EyelessDogHostile(pos)
//   heardNoise(out pos) -> EyelessDogAlert(pos)
public class EyelessDogPassive : Patrol
{
    protected EyelessDogController dog;

    public EyelessDogPassive(EyelessDogController _dog) : base(_dog)
    {
        dog = _dog;
        name = EyelessDogController.STATE.PASSIVE;
    }

    public override void Update()
    {
        base.Update();   // movimiento entre waypoints

        // TODO: transiciones (ver arriba)
    }
}
