using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using DG.Tweening;
using NexusChaser.CycloneAMS;

public class PlayerDialogueController : MonoBehaviour
{
    [Header("Settings")]
    public float minRandomDelay = 5f;
    public float maxRandomDelay = 15f;

    [Header("Typewriter Settings")]
    [Tooltip("Velocidad de escritura. Menor es más rápido. 0.04 es un buen punto rápido.")]
    public float typingSpeed = 0.04f; // Ajustado para ser más rápido (antes 0.05)
    public float displayDurationPerWord = 0.5f;
    public float minDisplayTime = 2f;

    [Header("Punctuation Pauses")]
    [Tooltip("Pausa extra al encontrar una coma")]
    public float commaPause = 0.15f;
    [Tooltip("Pausa extra al terminar una frase (., !, ?)")]
    public float sentencePause = 0.35f;

    [Header("Audio Settings")]
    [Tooltip("Tono base mínimo")]
    public float minPitch = 0.9f;
    [Tooltip("Tono base máximo")]
    public float maxPitch = 1.1f;
    [Tooltip("Cuánto sube el pitch si la frase tiene signos de exclamación (!)")]
    public float excitementPitchOffset = 0.25f;
    [Tooltip("Porcentaje del clip que debe reproducirse antes de permitir el siguiente (0.5 = superposición alta, 1.0 = sin superposición).")]
    [Range(0.1f, 1.0f)]
    public float audioOverlap = 0.7f;

    [Header("Animation Settings")]
    public float popDuration = 0.4f;
    public Ease popInEase = Ease.OutBack;
    public Ease popOutEase = Ease.InBack;

    private PlayerUIInfo uiInfo;
    private CharacterDialogueData currentData;
    private CycloneClip currentVoiceBlip;

    private Coroutine chatterCoroutine;
    private bool isGameEnded = false;

    private void Awake()
    {
        uiInfo = GetComponent<PlayerUIInfo>();

        if (uiInfo != null && uiInfo.dialogueContainer != null)
        {
            uiInfo.dialogueContainer.transform.localScale = Vector3.zero;
            uiInfo.dialogueContainer.SetActive(false);
        }
    }

    // --- INICIALIZACIONES (Sin cambios estructurales, solo añadimos el Data) ---
    public void Initialize(CharacterDialogueData data, int playerSeed, CharacterData fullCharData = null)
    {
        currentData = data;
        if (fullCharData != null) currentVoiceBlip = fullCharData.voiceBlip;

        float initialDelay = Random.Range(minRandomDelay, maxRandomDelay) + (playerSeed * 1.5f);
        chatterCoroutine = StartCoroutine(RandomChatterRoutine(initialDelay));
    }

    public void InitializeVictory(CharacterDialogueData data, CharacterData fullCharData = null)
    {
        currentData = data;
        if (fullCharData != null) currentVoiceBlip = fullCharData.voiceBlip;
        StopAllCoroutines();
    }

    public void SetVoiceBlip(CycloneClip clip)
    {
        currentVoiceBlip = clip;
    }

    // --- MÉTODOS PÚBLICOS (Sin cambios lógicos) ---
    public void PlayRandomVictoryPhrase()
    {
        if (currentData == null) return;
        LocalizedString victoryLocalized = currentData.GetNextVictoryPhrase();
        if (victoryLocalized != null && !victoryLocalized.IsEmpty)
        {
            StartCoroutine(TypewriterEffect(victoryLocalized.GetLocalizedString(), autoClose: false));
        }
    }

    public float PlayEndGameDialogue(bool isWinner)
    {
        isGameEnded = true;
        if (chatterCoroutine != null) StopCoroutine(chatterCoroutine);
        if (currentData == null) return 0f;

        LocalizedString finalPhraseObj = isWinner ? currentData.GetNextSuccessPhrase() : currentData.GetNextFailPhrase();
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

    // --- CORRUTINA PRINCIPAL ---
    IEnumerator RandomChatterRoutine(float startDelay)
    {
        yield return new WaitForSeconds(startDelay);
        while (!isGameEnded)
        {
            if (currentData != null)
            {
                LocalizedString textObj = currentData.GetNextRandomPhrase();
                if (textObj != null && !textObj.IsEmpty)
                {
                    yield return StartCoroutine(TypewriterEffect(textObj.GetLocalizedString(), autoClose: true));
                }
            }
            float nextDelay = Random.Range(minRandomDelay, maxRandomDelay);
            yield return new WaitForSeconds(nextDelay);
        }
    }

    // --- EFECTO TYPEWRITER MEJORADO ---
    IEnumerator TypewriterEffect(string text, bool autoClose)
    {
        if (uiInfo.dialogueContainer == null || uiInfo.dialogueText == null) yield break;

        uiInfo.dialogueContainer.transform.DOKill();
        uiInfo.dialogueText.DOKill();
        uiInfo.dialogueText.text = "";
        uiInfo.dialogueContainer.transform.localScale = Vector3.zero;
        uiInfo.dialogueContainer.SetActive(true);

        // 1. POP IN
        yield return uiInfo.dialogueContainer.transform
            .DOScale(Vector3.one, popDuration)
            .SetEase(popInEase)
            .WaitForCompletion();

        // 2. ESCRITURA CON INTELIGENCIA DE PUNTUACIÓN Y AUDIO
        float nextAudioTime = 0f;
        float clipDuration = 0.1f; // Valor por defecto seguro
        if (currentVoiceBlip != null && currentVoiceBlip.Clip != null)
        {
            clipDuration = currentVoiceBlip.Clip.length;
        }

        // Variable para controlar si la frase actual es "emocionante"
        bool currentPhraseExcited = CheckIfSentenceIsExcited(text, 0);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            uiInfo.dialogueText.text += c;

            // A. MANEJO DE PUNTUACIÓN Y PAUSAS
            if (IsPunctuation(c))
            {
                // Si encontramos un punto final, recalculamos emoción para la siguiente frase
                if (c == '.' || c == '!' || c == '?')
                {
                    currentPhraseExcited = CheckIfSentenceIsExcited(text, i + 1);
                    yield return new WaitForSeconds(sentencePause);
                }
                else if (c == ',')
                {
                    yield return new WaitForSeconds(commaPause);
                }

                // No reproducimos sonido en signos de puntuación, solo esperamos
                continue;
            }

            // B. LÓGICA DE AUDIO (Solo si no es espacio en blanco)
            if (currentVoiceBlip != null && !char.IsWhiteSpace(c))
            {
                if (Time.time >= nextAudioTime)
                {
                    // Calculamos pitch base + offset si estamos en modo emoción
                    float basePitch = Random.Range(minPitch, maxPitch);
                    float finalPitch = currentPhraseExcited ? basePitch + excitementPitchOffset : basePitch;

                    if (CycloneAudioDriver.Instance != null)
                    {
                        CycloneAudioDriver.Instance.SetPitch(currentVoiceBlip, finalPitch);
                        // Usamos PlayOneShot para permitir solapamiento (superposición)
                        CycloneAudioDriver.Instance.PlayOneShot(currentVoiceBlip);
                    }

                    // C. CÁLCULO DE TIEMPO DE ESPERA (SUPERPOSICIÓN)
                    // Permitimos que el siguiente sonido suene cuando este haya recorrido el % definido (audioOverlap)
                    // Ej: Clip 0.1s, Overlap 0.7 -> Siguiente sonido habilitado a los 0.07s
                    float waitTime = clipDuration * audioOverlap;

                    // Clampeamos para que nunca sea menor a la velocidad de escritura (para no spammear demasiado)
                    waitTime = Mathf.Max(waitTime, typingSpeed);

                    nextAudioTime = Time.time + waitTime;
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        if (!autoClose) yield break;

        // 3. LECTURA
        float wait = Mathf.Max(minDisplayTime, text.Split(' ').Length * displayDurationPerWord);
        yield return new WaitForSeconds(wait);

        // 4. POP OUT
        yield return uiInfo.dialogueContainer.transform
            .DOScale(Vector3.zero, popDuration)
            .SetEase(popOutEase)
            .WaitForCompletion();

        uiInfo.dialogueContainer.SetActive(false);
    }

    // Helpers
    private bool IsPunctuation(char c)
    {
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ':' || c == ';';
    }

    // Mira hacia adelante en el string hasta encontrar el final de la frase.
    // Si encuentra un '!', retorna true.
    private bool CheckIfSentenceIsExcited(string fullText, int startIndex)
    {
        if (startIndex >= fullText.Length) return false;

        for (int i = startIndex; i < fullText.Length; i++)
        {
            char c = fullText[i];
            if (c == '!') return true; // ¡Encontré emoción!
            if (c == '.' || c == '?') return false; // Fin de frase normal
        }
        return false;
    }
}