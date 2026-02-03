using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Game/Character System/Dialogue Data")]
public class CharacterDialogueData : ScriptableObject
{
    [Header("Gameplay Chatter")]
    [Tooltip("Random phrases displayed during gameplay loops")]
    [ReorderableList(ListStyle.Boxed, "Random Phrases", Foldable = true)]
    public LocalizedString[] randomPhrases;

    [Header("Minigame Feedback")]
    [Tooltip("Phrases displayed when the player successfully completes a minigame")]
    [ReorderableList(ListStyle.Boxed, "On Minigame Success", Foldable = true)]
    public LocalizedString[] onMinigameSuccess;

    [Tooltip("Phrases displayed when the player fails a minigame")]
    [ReorderableList(ListStyle.Boxed, "On Minigame Fail", Foldable = true)]
    public LocalizedString[] onMinigameFail;

    [Header("Results")]
    [ReorderableList(ListStyle.Boxed, "Victory Phrases", Foldable = true)]
    public LocalizedString[] victoryPhrases;

    // --- MEMORIA INTERNA (No visible en Inspector) ---
    // Usamos System.NonSerialized para evitar que Unity intente guardar estos estados
    // entre sesiones de editor si modificas el asset, y para ocultarlos.

    [System.NonSerialized] private List<int> _randomIndices = new List<int>();
    [System.NonSerialized] private int _lastRandomIndex = -1;

    [System.NonSerialized] private List<int> _successIndices = new List<int>();
    [System.NonSerialized] private int _lastSuccessIndex = -1;

    [System.NonSerialized] private List<int> _failIndices = new List<int>();
    [System.NonSerialized] private int _lastFailIndex = -1;

    [System.NonSerialized] private List<int> _victoryIndices = new List<int>();
    [System.NonSerialized] private int _lastVictoryIndex = -1;

    public void ResetHistory()
    {
        _randomIndices.Clear();
        _successIndices.Clear();
        _failIndices.Clear();
        _victoryIndices.Clear();

        _lastRandomIndex = -1;
        _lastSuccessIndex = -1;
        _lastFailIndex = -1;
        _lastVictoryIndex = -1;
    }

    // --- MÉTODOS PÚBLICOS DE ACCESO ---

    public LocalizedString GetNextRandomPhrase()
    {
        return GetNextPhrase(randomPhrases, _randomIndices, ref _lastRandomIndex);
    }

    public LocalizedString GetNextSuccessPhrase()
    {
        return GetNextPhrase(onMinigameSuccess, _successIndices, ref _lastSuccessIndex);
    }

    public LocalizedString GetNextFailPhrase()
    {
        return GetNextPhrase(onMinigameFail, _failIndices, ref _lastFailIndex);
    }

    public LocalizedString GetNextVictoryPhrase()
    {
        return GetNextPhrase(victoryPhrases, _victoryIndices, ref _lastVictoryIndex);
    }

    // --- LÓGICA CENTRAL DE SHUFFLE BAG ---

    private LocalizedString GetNextPhrase(LocalizedString[] sourceArray, List<int> availableIndices, ref int lastIndexUsed)
    {
        // 1. Si no hay frases, devolvemos null o un string vacío seguro
        if (sourceArray == null || sourceArray.Length == 0)
            return null; // O return new LocalizedString();

        // 2. Si solo hay una frase, no hay nada que barajar, la devolvemos siempre.
        if (sourceArray.Length == 1)
            return sourceArray[0];

        // 3. Si la lista de disponibles está vacía, la rellenamos (Reset del ciclo)
        if (availableIndices.Count == 0)
        {
            for (int i = 0; i < sourceArray.Length; i++)
            {
                availableIndices.Add(i);
            }
        }

        // 4. Seleccionamos un índice aleatorio de la lista disponible
        int listIndex = Random.Range(0, availableIndices.Count);
        int realIndex = availableIndices[listIndex];

        // 5. COMPROBACIÓN DE REPETICIÓN INMEDIATA
        // Si acabamos de resetear la bolsa (availableIndices está lleno) y el que salió
        // es IGUAL al último que dijimos antes del reset, forzamos uno diferente.
        if (realIndex == lastIndexUsed && availableIndices.Count > 1)
        {
            // Tomamos el siguiente (o el anterior), asegurando loop
            // Básicamente elegimos "otro" de la lista
            int newListIndex = (listIndex + 1) % availableIndices.Count;
            listIndex = newListIndex;
            realIndex = availableIndices[listIndex];
        }

        // 6. Actualizamos memoria
        lastIndexUsed = realIndex;
        availableIndices.RemoveAt(listIndex);

        return sourceArray[realIndex];
    }
}