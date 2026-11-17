using UnityEngine;

// Va a lastSeenPosition y se queda buscando searchDuration s.
// Transiciones planeadas:
//   canSeePlayer() -> ThumperChase
//   termina -> ThumperPatrol
public class ThumperSearch : State
{
    protected ThumperController thumper;

    public ThumperSearch(ThumperController _thumper) : base(_thumper)
    {
        thumper = _thumper;
        name = ThumperController.STATE.SEARCH;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isSearching");
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isSearching");
        base.Exit();
    }

    public override void Update()
    {
        // TODO: comportamiento
        // TODO: transiciones (ver arriba)
    }
}
