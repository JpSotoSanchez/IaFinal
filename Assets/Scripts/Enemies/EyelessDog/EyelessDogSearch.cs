using UnityEngine;

// Embiste searchCharges veces a puntos aleatorios alrededor de searchCenter (searchRadius).
// Transiciones planeadas:
//   oye un ruido -> EyelessDogHostile(pos)
//   canAttackPlayer() -> playerInfo.Kill()
//   termina -> EyelessDogPassive
public class EyelessDogSearch : State
{
    protected EyelessDogController dog;
    protected Vector3 searchCenter;

    public EyelessDogSearch(EyelessDogController _dog, Vector3 _searchCenter) : base(_dog)
    {
        dog = _dog;
        searchCenter = _searchCenter;
        name = EyelessDogController.STATE.SEARCH;
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
