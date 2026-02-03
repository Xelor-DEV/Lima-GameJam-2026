using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using DG.Tweening;

public class PlayerDialogueController : MonoBehaviour
{
    [Header("Settings")]
    public float minRandomDelay = 5f;
    public float maxRandomDelay = 15f;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;
    public float displayDurationPerWord = 0.5f;
    public float minDisplayTime = 2f;

    [Header("Animation Settings")]
    public float popDuration = 0.4f;
    public Ease popInEase = Ease.OutBack;
    public Ease popOutEase = Ease.InBack;

    private PlayerUIInfo uiInfo;
    private CharacterDialogueData currentData;
    private Coroutine chatterCoroutine;
    private bool isGameEnded = false;

    // Shuffle Bag
    private List<int> phraseIndices = new List<int>();

    private void Awake()
    {
        uiInfo = GetComponent<PlayerUIInfo>();

        if (uiInfo != null && uiInfo.dialogueContainer != null)
        {
            uiInfo.dialogueContainer.transform.localScale = Vector3.zero;
            uiInfo.dialogueContainer.SetActive(false);
        }
    }

    // --- INICIALIZACIÓN ESTÁNDAR (Minijuegos) ---
    public void Initialize(CharacterDialogueData data, int playerSeed)
    {
        currentData = data;
        RefillPhraseIndices();

        float initialDelay = Random.Range(minRandomDelay, maxRandomDelay) + (playerSeed * 1.5f);
        chatterCoroutine = StartCoroutine(RandomChatterRoutine(initialDelay));
    }

    // --- NUEVA INICIALIZACIÓN (Escena de Victoria) ---
    // No inicia la corrutina de RandomChatterRoutine
    public void InitializeVictory(CharacterDialogueData data)
    {
        currentData = data;
        // Detenemos cualquier corrutina previa por seguridad
        StopAllCoroutines();
    }

    // --- NUEVO MÉTODO PARA FRASE DE VICTORIA ---
    public void PlayRandomVictoryPhrase()
    {
        if (currentData == null || currentData.victoryPhrases == null || currentData.victoryPhrases.Length == 0)
            return;

        // Elegir frase aleatoria del array de victoria
        string victoryText = currentData.victoryPhrases[Random.Range(0, currentData.victoryPhrases.Length)].GetLocalizedString();

        // Llamamos al Typewriter indicando que NO se cierre automáticamente (autoClose = false)
        StartCoroutine(TypewriterEffect(victoryText, autoClose: false));
    }

    private void RefillPhraseIndices()
    {
        phraseIndices.Clear();
        if (currentData != null && currentData.randomPhrases != null)
        {
            for (int i = 0; i < currentData.randomPhrases.Length; i++)
            {
                phraseIndices.Add(i);
            }
        }
    }

    IEnumerator RandomChatterRoutine(float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        while (!isGameEnded)
        {
            if (currentData != null && currentData.randomPhrases.Length > 0)
            {
                if (phraseIndices.Count == 0) RefillPhraseIndices();

                int randomIndex = Random.Range(0, phraseIndices.Count);
                int selectedPhraseIdx = phraseIndices[randomIndex];
                phraseIndices.RemoveAt(randomIndex);

                string textToSay = currentData.randomPhrases[selectedPhraseIdx].GetLocalizedString();

                // En modo normal, autoClose es true por defecto
                yield return StartCoroutine(TypewriterEffect(textToSay, autoClose: true));
            }

            float nextDelay = Random.Range(minRandomDelay, maxRandomDelay);
            yield return new WaitForSeconds(nextDelay);
        }
    }

    public float PlayEndGameDialogue(bool isWinner)
    {
        isGameEnded = true;
        if (chatterCoroutine != null) StopCoroutine(chatterCoroutine);

        if (currentData == null) return 0f;

        LocalizedString[] pool = isWinner ? currentData.onMinigameSuccess : currentData.onMinigameFail;
        if (pool == null || pool.Length == 0) return 0f;

        string finalPhrase = pool[Random.Range(0, pool.Length)].GetLocalizedString();

        StopAllCoroutines();
        if (uiInfo.dialogueContainer != null) uiInfo.dialogueContainer.transform.DOKill();
        if (uiInfo.dialogueText != null) uiInfo.dialogueText.DOKill();

        StartCoroutine(TypewriterEffect(finalPhrase, autoClose: true));

        float totalTypeDuration = finalPhrase.Length * typingSpeed;
        float readingDuration = Mathf.Max(minDisplayTime, finalPhrase.Split(' ').Length * displayDurationPerWord);

        return popDuration + totalTypeDuration + readingDuration + popDuration;
    }

    // --- CORRUTINA MODIFICADA: AÑADIDO PARÁMETRO autoClose ---
    IEnumerator TypewriterEffect(string text, bool autoClose)
    {
        if (uiInfo.dialogueContainer == null || uiInfo.dialogueText == null) yield break;

        uiInfo.dialogueContainer.transform.DOKill();
        uiInfo.dialogueText.DOKill();

        uiInfo.dialogueText.text = "";
        uiInfo.dialogueContainer.transform.localScale = Vector3.zero;
        uiInfo.dialogueContainer.SetActive(true);

        // POP IN
        yield return uiInfo.dialogueContainer.transform
            .DOScale(Vector3.one, popDuration)
            .SetEase(popInEase)
            .WaitForCompletion();

        // ESCRITURA
        float duration = text.Length * typingSpeed;
        yield return uiInfo.dialogueText
            .DOText(text, duration)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        // SI NO DEBE CERRARSE (Victoria), TERMINAMOS AQUÍ
        if (!autoClose) yield break;

        // LECTURA
        float waitTime = Mathf.Max(minDisplayTime, text.Split(' ').Length * displayDurationPerWord);
        yield return new WaitForSeconds(waitTime);

        // POP OUT
        yield return uiInfo.dialogueContainer.transform
            .DOScale(Vector3.zero, popDuration)
            .SetEase(popOutEase)
            .WaitForCompletion();

        uiInfo.dialogueContainer.SetActive(false);
    }
}