using System;
using UnityEngine;

// Sistema de ruido global. Cualquiera puede emitir un ruido (jugador corriendo, objeto que
// cae, rugido...) y los enemigos con canHear = true lo escuchan si están dentro del radio.
public static class NoiseSystem
{
    // posición del ruido, radio en el que se escucha
    public static event Action<Vector3, float> OnNoise;

    public static bool debugDraw = true;

    public static void Emit(Vector3 position, float radius)
    {
        if (radius <= 0f) return;
        if (debugDraw) Debug.DrawRay(position, Vector3.up * radius * 0.2f, Color.cyan, 0.5f);
        OnNoise?.Invoke(position, radius);
    }
}
