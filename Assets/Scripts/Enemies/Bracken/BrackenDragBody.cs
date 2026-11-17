using UnityEngine;

// Arrastra el cuerpo hasta favoriteSpot y después se aleja de ahí.
// Transiciones planeadas:
//   llega a favoriteSpot -> BrackenPatrol
public class BrackenDragBody : State
{
    protected BrackenController bracken;

    public BrackenDragBody(BrackenController _bracken) : base(_bracken)
    {
        bracken = _bracken;
        name = BrackenController.STATE.DRAG_BODY;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isDragging");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isDragging");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
