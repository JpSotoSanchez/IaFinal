using UnityEngine;

// Lo que los enemigos necesitan saber del jugador.
// El jugador real (lo hace otro compañero) solo tiene que implementar esta interfaz
// y tener el tag "Player"; mientras tanto la implementa PlayerStub.
public interface IPlayer
{
    Transform Eyes { get; }          // cámara / cabeza del jugador
    bool IsMoving { get; }           // se está desplazando (Nutcracker)
    bool IsLookingAround { get; }    // está moviendo la cámara (Nutcracker)
    bool IsDead { get; }

    // ¿El jugador tiene a target dentro de su vista (ángulo) y sin paredes de por medio?
    bool IsLookingAt(Transform target, float maxAngle);

    void TakeDamage(int amount);
    void Kill();
}
