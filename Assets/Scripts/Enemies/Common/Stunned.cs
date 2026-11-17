using UnityEngine;

// Estado compartido: el enemigo se queda quieto unos segundos (aturdidor del jugador)
// y después regresa a su estado por defecto (controller.CreateDefaultState()).
public class Stunned : State
{
    float duration;
    float timer;

    public Stunned(EnemyController _controller, float _duration) : base(_controller)
    {
        name = COMMON.STUNNED;
        duration = _duration;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isStunned");
        timer = 0f;
        agent.isStopped = true;
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isStunned");
        agent.isStopped = false;
        base.Exit();
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            ChangeState(controller.CreateDefaultState());
        }
    }
}
