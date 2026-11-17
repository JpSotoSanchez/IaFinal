using UnityEngine;

// Oyó algo: camina despacio (alertSpeed) hacia noisePosition.
// Transiciones planeadas:
//   oye otro ruido -> EyelessDogHostile(pos)
//   llega al punto sin oír nada más -> EyelessDogPassive
public class EyelessDogAlert : State
{
    protected EyelessDogController dog;
    protected Vector3 noisePosition;

    public EyelessDogAlert(EyelessDogController _dog, Vector3 _noisePosition) : base(_dog)
    {
        dog = _dog;
        noisePosition = _noisePosition;
        name = EyelessDogController.STATE.ALERT;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isSniffing");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isSniffing");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
