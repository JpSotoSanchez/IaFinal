using UnityEngine;

// Ruge (roarDuration, dog.AlertPack(targetPosition)) y embiste en línea recta hacia targetPosition (chargeSpeed).
// Transiciones planeadas:
//   canAttackPlayer() -> playerInfo.Kill()
//   termina la embestida -> EyelessDogSearch(targetPosition)
public class EyelessDogHostile : State
{
    protected EyelessDogController dog;
    protected Vector3 targetPosition;

    public EyelessDogHostile(EyelessDogController _dog, Vector3 _targetPosition) : base(_dog)
    {
        dog = _dog;
        targetPosition = _targetPosition;
        name = EyelessDogController.STATE.HOSTILE;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isCharging");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isCharging");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
