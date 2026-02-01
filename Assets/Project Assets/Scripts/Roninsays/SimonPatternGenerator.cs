using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SimonInputDefinition
{
    [Tooltip("El nombre exacto de la Acción en el Input System (ej: 'South', 'North', 'Move', 'Dpad')")]
    public string actionName;

    [Tooltip("Si es un botón, déjalo en (0,0). Si es un Stick o D-Pad, define la dirección (Ej: (0,1) para Arriba, (1,0) para Derecha).")]
    public Vector2 requiredDirection;

    [Header("Visuals")]
    public Sprite keyboardIcon;
    public Sprite gamepadIcon;

    // Propiedad helper para saber fácilmente si este paso requiere dirección
    public bool IsDirectional => requiredDirection != Vector2.zero;
}

public class SimonPatternGenerator : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Lista de inputs posibles que el juego puede pedir (Botones y Direcciones)")]
    public List<SimonInputDefinition> availableInputs;

    [Tooltip("Longitud de la secuencia a generar")]
    public int sequenceLength = 10;

    /// <summary>
    /// Genera una lista aleatoria de definiciones de input.
    /// </summary>
    public List<SimonInputDefinition> GenerateNewSequence()
    {
        List<SimonInputDefinition> sequence = new List<SimonInputDefinition>();

        if (availableInputs == null || availableInputs.Count == 0)
        {
            Debug.LogError("SimonPatternGenerator: No hay inputs configurados.");
            return sequence;
        }

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex = Random.Range(0, availableInputs.Count);
            sequence.Add(availableInputs[randomIndex]);
        }

        return sequence;
    }
}