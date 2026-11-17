using System.Collections.Generic;
using UnityEngine;

// Eyeless Dog: ciego, caza solo por el oído y en manada (3 perros). Al encontrar presa ruge
// y alerta a los perros cercanos. Embiste en línea recta y mata de un golpe. ~12 golpes de pala.
// Estados:
//   EyelessDogPassive
//   EyelessDogAlert
//   EyelessDogHostile
//   EyelessDogSearch
//   Stunned (común)
public class EyelessDogController : EnemyController
{
    public enum STATE
    {
        PASSIVE, ALERT, HOSTILE, SEARCH
    };

    [Header("Eyeless Dog")]
    public float alertSpeed = 3.5f;
    public float chargeSpeed = 12f;
    public float roarDuration = 1.2f;
    [Tooltip("Radio en el que su rugido alerta a otros perros")]
    public float packAlertRadius = 25f;
    [Tooltip("Si oye un ruido más cerca que esto pasa directo a HOSTILE")]
    public float hostileNoiseDistance = 8f;
    [Tooltip("Embestidas extra alrededor de la zona después de la primera")]
    public int searchCharges = 2;
    public float searchRadius = 6f;

    // Todos los perros vivos de la escena (para el rugido de manada)
    public static readonly List<EyelessDogController> Pack = new List<EyelessDogController>();

    // Valores por defecto al agregar el componente (o al hacer Reset en el Inspector)
    void Reset()
    {
        canHear = true;
        visionDistance = 0f;
        attackDistance = 1.5f;
        maxHealth = 12f;
        patrolSpeed = 2f;
        wanderRadius = 10f;
    }

    protected override void Awake()
    {
        canHear = true;        // se fuerza antes de OnEnable para suscribirse al NoiseSystem
        visionDistance = 0f;   // es ciego
        base.Awake();
    }

    protected override State CreateInitialState() => new EyelessDogPassive(this);
    public override State CreateDefaultState() => new EyelessDogPassive(this);

    protected override void OnEnable()
    {
        base.OnEnable();
        Pack.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Pack.Remove(this);
    }

    // Lo llama EyelessDogHostile al rugir: avisa a los perros cercanos.
    public void AlertPack(Vector3 targetPosition)
    {
        foreach (EyelessDogController other in Pack)
        {
            if (other == this || other.IsDead) continue;
            if (Vector3.Distance(transform.position, other.transform.position) <= packAlertRadius)
                other.ReceivePackAlert(targetPosition);
        }
    }

    public void ReceivePackAlert(Vector3 targetPosition)
    {
        // TODO: si no está ya HOSTILE -> ForceState(new EyelessDogHostile(this, targetPosition));
        //       (eso hace la reacción en cadena: ese perro también ruge y avisa a otros)
    }
}
