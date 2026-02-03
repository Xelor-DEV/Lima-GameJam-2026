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

    // NOTA: Se ha eliminado la lista local 'phraseIndices' porque ahora
    // el estado se gestiona dentro de CharacterDialogueData.

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
        // Ya no hace falta llamar a RefillPhraseIndices aquí

        float initialDelay = Random.Range(minRandomDelay, maxRandomDelay) + (playerSeed * 1.5f);
        chatterCoroutine = StartCoroutine(RandomChatterRoutine(initialDelay));
    }

    // --- NUEVA INICIALIZACIÓN (Escena de Victoria) ---
    public void InitializeVictory(CharacterDialogueData data)
    {
        currentData = data;
        StopAllCoroutines();
    }

    // --- MÉTODO PARA FRASE DE VICTORIA ---
    public void PlayRandomVictoryPhrase()
    {
        if (currentData == null) return;

        // Solicitamos la siguiente frase inteligente al Data
        LocalizedString victoryLocalized = currentData.GetNextVictoryPhrase();

        if (victoryLocalized != null && !victoryLocalized.IsEmpty)
        {
            string victoryText = victoryLocalized.GetLocalizedString();
            StartCoroutine(TypewriterEffect(victoryText, autoClose: false));
        }
    }

    IEnumerator RandomChatterRoutine(float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        while (!isGameEnded)
        {
            if (currentData != null)
            {
                // Obtenemos la frase usando la lógica interna del ScriptableObject
                LocalizedString textObj = currentData.GetNextRandomPhrase();

                if (textObj != null && !textObj.IsEmpty)
                {
                    string textToSay = textObj.GetLocalizedString();
                    yield return StartCoroutine(TypewriterEffect(textToSay, autoClose: true));
                }
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

        // Pedimos la frase (Success o Fail) al Data, que recordará qué ya se dijo
        LocalizedString finalPhraseObj = isWinner ?
                                         currentData.GetNextSuccessPhrase() :
                                         currentData.GetNextFailPhrase();

        if (finalPhraseObj == null || finalPhraseObj.IsEmpty) return 0f;

        string finalPhrase = finalPhraseObj.GetLocalizedString();

        StopAllCoroutines();
        if (uiInfo.dialogueContainer != null) uiInfo.dialogueContainer.transform.DOKill();
        if (uiInfo.dialogueText != null) uiInfo.dialogueText.DOKill();

        StartCoroutine(TypewriterEffect(finalPhrase, autoClose: true));

        float totalTypeDuration = finalPhrase.Length * typingSpeed;
        float readingDuration = Mathf.Max(minDisplayTime, finalPhrase.Split(' ').Length * displayDurationPerWord);

        return popDuration + totalTypeDuration + readingDuration + popDuration;
    }

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
        // Reproducir sonido de blip si existe en el data
        // (Nota: puedes añadir la lógica de sonido aquí si lo deseas, usando currentData.voiceBlip)

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