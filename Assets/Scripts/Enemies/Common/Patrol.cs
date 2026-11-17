using UnityEngine;
using UnityEngine.AI;

// Patrulla compartida: recorre los waypoints del controller en orden (en loop).
// Si el enemigo no tiene waypoints, deambula a puntos aleatorios del NavMesh.
// Cada enemigo hereda de aquí (BrackenPatrol, ThumperPatrol...) y agrega SUS transiciones
// en Update() después de llamar base.Update().
public class Patrol : State
{
    int currentWaypointIndex = -1;

    public Patrol(EnemyController _controller) : base(_controller)
    {
        name = COMMON.PATROL;
    }

    public override void Enter()
    {
        // animator.SetTrigger("isWalking");
        agent.isStopped = false;
        agent.speed = controller.patrolSpeed;
        GoToNextPoint();   // se pone destino desde el inicio (antes se saltaba el waypoint 0)
        base.Enter();
    }

    public override void Exit()
    {
        // animator.ResetTrigger("isWalking");
        base.Exit();
    }

    public override void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            GoToNextPoint();
        }
    }

    protected void GoToNextPoint()
    {
        Transform[] waypoints = controller.waypoints;
        if (waypoints != null && waypoints.Length > 0)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            if (waypoints[currentWaypointIndex] != null)
                agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        else if (RandomNavMeshPoint(npc.transform.position, controller.wanderRadius, out Vector3 punto))
        {
            agent.SetDestination(punto);
        }
    }

    public static bool RandomNavMeshPoint(Vector3 centro, float radio, out Vector3 resultado)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 candidato = centro + Random.insideUnitSphere * radio;
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                resultado = hit.position;
                return true;
            }
        }
        resultado = centro;
        return false;
    }
}
