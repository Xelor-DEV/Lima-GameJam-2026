using UnityEngine;

public class ScissorsController : MonoBehaviour
{
    public CuttingPattern currentPattern;
    public TrailRenderer cutTrail;

    [Header("Estado")]
    public bool isCutting = false;
    public bool levelComplete = false;

    [Range(0, 1)] public float currentProgress = 0f;
    public float winThreshold = 0.98f;

    void Update()
    {
        if (levelComplete) return;

        // Mientras mantengas presionado
        if (Input.GetMouseButton(0))
        {
            ProcessCut();
        }
        else
        {
            StopCutting();
        }
    }

    void ProcessCut()
    {
        if (currentPattern == null) return;

        isCutting = true;
        if (cutTrail) cutTrail.emitting = true;

        // 1. CHEQUEO DE SEGURIDAD
        // Verificamos si estás dentro del margen, pero NO detenemos el corte si fallas.
        bool isSafe = currentPattern.IsPositionSafe(transform.position);

        if (isSafe)
        {
            // Solo si es seguro, intentamos "comer" nodos y avanzar progreso
            currentProgress = currentPattern.UpdateCuttingProgress(transform.position);

            // Chequeo de victoria
            if (currentProgress >= winThreshold)
            {
                WinGame();
            }
        }
        else
        {
            // ESTÁS FUERA DEL MARGEN
            // Aquí ya no reseteamos nada, solo avisamos (futuro sistema de puntos)
            Debug.Log("¡Cuidado! Te saliste del margen (Perdiendo puntos...)");
        }
    }

    void StopCutting()
    {
        isCutting = false;
        if (cutTrail) cutTrail.emitting = false;
    }

    void WinGame()
    {
        levelComplete = true;
        StopCutting();
        Debug.Log("¡GANASTE! Figura completada.");
    }
}