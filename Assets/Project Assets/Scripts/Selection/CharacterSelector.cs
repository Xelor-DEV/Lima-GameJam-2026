using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;
using NexusChaser.CycloneAMS;
using UnityEngine.Localization;

public class CharacterSelector : MonoBehaviour
{
    public event Action<int, bool> OnReadyStatusChanged;

    [Header("Data References")]
    [SerializeField] private PlayersSessionData sessionData;
    [SerializeField] private CharacterDatabase charDatabase;

    [Header("Dependencies")]
    [SerializeField] private CheatCodeDetector cheatDetector;

    [Header("UI References - Slots")]
    [SerializeField] private RectTransform leftSlot;
    [SerializeField] private RectTransform centerSlot;
    [SerializeField] private RectTransform rightSlot;

    [Header("UI References - Info")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject dialogueContainer;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Carousel Animation Config")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.OutQuad;

    [Header("Dialogue Animation (Typewriter)")]
    [Tooltip("Velocidad de escritura (menor es más rápido).")]
    [SerializeField] private float typingSpeed = 0.04f; // Más rápido
    [SerializeField] private float popDuration = 0.4f;
    [SerializeField] private Ease popInEase = Ease.OutBack;
    [SerializeField] private Ease popOutEase = Ease.InBack;

    [Header("Punctuation Pauses")]
    [SerializeField] private float commaPause = 0.15f;
    [SerializeField] private float sentencePause = 0.35f;

    [Header("Audio Settings")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float excitementPitchOffset = 0.25f;
    [Range(0.1f, 1.0f)]
    [SerializeField] private float audioOverlap = 0.7f; // Superposición

    [Header("Audio SFX")]
    [SerializeField] private CycloneClip moveSfx;
    [SerializeField] private CycloneClip selectSfx;
    [SerializeField] private CycloneClip cancelSfx;

    // Internal State
    private int _playerIndex;
    private int _visualIndex;
    private bool _isAnimating;
    private bool _isReady;
    private float _moveDistance;
    private List<int> _availableIndices = new List<int>();

    // Variable para controlar la corrutina de escritura
    private Coroutine _typingCoroutine;

    private void Start()
    {
        _playerIndex = GetComponent<PlayerInput>().playerIndex;
        _moveDistance = rightSlot.anchoredPosition.x;

        RefreshCharacterList();
        UpdateVisuals(instant: true);

        if (dialogueContainer != null)
        {
            dialogueContainer.transform.localScale = Vector3.zero;
            dialogueContainer.SetActive(false);
        }
    }

    public void RefreshCharacterList()
    {
        _availableIndices.Clear();
        Player currentPlayer = null;
        if (sessionData.players.Count > _playerIndex)
        {
            currentPlayer = sessionData.players[_playerIndex];
        }

        for (int i = 0; i < charDatabase.characterRoster.Length; i++)
        {
            CharacterData charData = charDatabase.characterRoster[i];
            if (currentPlayer != null && currentPlayer.lockedChars.Contains(charData))
            {
                continue;
            }
            _availableIndices.Add(i);
        }

        _visualIndex = 0;
        UpdateVisuals(instant: true);
    }

    #region Input Handlers
    // (Sin cambios en OnNavigate, OnSelect, OnCancel - se mantienen igual que en tu script original)
    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed || _isAnimating || _isReady) return;
        if (_availableIndices.Count <= 1) return;

        Vector2 navigationInput = context.ReadValue<Vector2>();
        float moveX = navigationInput.x;

        if (moveX > 0.5f) MoveCarousel(1);
        else if (moveX < -0.5f) MoveCarousel(-1);
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        if (!context.performed || _isAnimating || _isReady) return;
        if (cheatDetector != null && cheatDetector.WasInputConsumedByCheat()) return;
        SetReadyState(true);
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || !_isReady) return;
        if (cheatDetector != null && cheatDetector.WasInputConsumedByCheat()) return;
        SetReadyState(false);
    }
    #endregion

    #region Logic & Animation
    // (MoveCarousel, SwapSlotReferences, UpdateVisuals, SetImage, JumpToCharacter se mantienen igual)
    private void MoveCarousel(int direction)
    {
        if (CycloneAudioDriver.Instance != null && moveSfx != null)
        {
            CycloneAudioDriver.Instance.PlayOneShot(moveSfx);
        }

        _isAnimating = true;
        _visualIndex += direction;

        centerSlot.DOComplete();
        centerSlot.DOPunchPosition(new Vector3(direction * 15f, 0, 0), 0.2f, 5, 0.5f);

        RectTransform outgoing = centerSlot;
        RectTransform incoming = (direction > 0) ? rightSlot : leftSlot;
        RectTransform wrapping = (direction > 0) ? leftSlot : rightSlot;

        float targetX = (direction > 0) ? -_moveDistance : _moveDistance;

        Sequence seq = DOTween.Sequence();
        seq.Join(outgoing.DOAnchorPosX(targetX, animDuration).SetEase(animEase));
        seq.Join(incoming.DOAnchorPosX(0, animDuration).SetEase(animEase));

        float wrapStartPos = (direction > 0) ? _moveDistance : -_moveDistance;
        wrapping.anchoredPosition = new Vector2(wrapStartPos, 0);

        seq.OnComplete(() =>
        {
            SwapSlotReferences(direction);
            UpdateVisuals();
            nameText.transform.DOPunchScale(Vector2.one * 0.15f, 0.2f);
            _isAnimating = false;
        });
    }

    private void SwapSlotReferences(int direction)
    {
        RectTransform temp;
        if (direction > 0)
        {
            temp = leftSlot;
            leftSlot = centerSlot;
            centerSlot = rightSlot;
            rightSlot = temp;
        }
        else
        {
            temp = rightSlot;
            rightSlot = centerSlot;
            centerSlot = leftSlot;
            leftSlot = temp;
        }
    }

    private void UpdateVisuals(bool instant = false)
    {
        int total = _availableIndices.Count;
        if (total == 0) return;

        int centerLocalIdx = (_visualIndex % total + total) % total;
        int leftLocalIdx = ((centerLocalIdx - 1) % total + total) % total;
        int rightLocalIdx = ((centerLocalIdx + 1) % total + total) % total;

        int realCenterIdx = _availableIndices[centerLocalIdx];
        int realLeftIdx = _availableIndices[leftLocalIdx];
        int realRightIdx = _availableIndices[rightLocalIdx];

        SetImage(leftSlot, realLeftIdx);
        SetImage(centerSlot, realCenterIdx);
        SetImage(rightSlot, realRightIdx);

        nameText.text = charDatabase.GetCharacter(realCenterIdx).characterName;
    }

    private void SetImage(RectTransform slot, int dbIndex)
    {
        var data = charDatabase.GetCharacter(dbIndex);
        var img = slot.GetComponent<Image>();
        if (data != null) img.sprite = data.selectorImage;
    }

    public void JumpToCharacter(CharacterData targetChar)
    {
        RefreshCharacterList();
        int targetVisualIndex = _availableIndices.FindIndex(id => charDatabase.GetCharacter(id) == targetChar);

        if (targetVisualIndex != -1)
        {
            _isAnimating = false;
            _visualIndex = targetVisualIndex;
            UpdateVisuals(instant: true);

            Sequence unlockSeq = DOTween.Sequence();
            centerSlot.GetComponent<Image>().DOBlendableColor(Color.white, 0.1f).SetLoops(2, LoopType.Yoyo);
            unlockSeq.Append(transform.DOPunchScale(Vector3.one * 0.4f, 0.6f, 10, 1f));
            unlockSeq.Join(transform.DOShakePosition(0.5f, 20f, 20));
        }
    }
    #endregion

    #region Ready & Dialogue State

    private void SetReadyState(bool ready)
    {
        _isReady = ready;

        int total = _availableIndices.Count;
        int localIndex = (_visualIndex % total + total) % total;
        int realDbIndex = _availableIndices[localIndex];

        CharacterData selectedChar = charDatabase.GetCharacter(realDbIndex);

        if (CycloneAudioDriver.Instance != null)
        {
            if (ready && selectSfx != null) CycloneAudioDriver.Instance.PlayOneShot(selectSfx);
            else if (!ready && cancelSfx != null) CycloneAudioDriver.Instance.PlayOneShot(cancelSfx);
        }

        if (sessionData.players.Count > _playerIndex)
        {
            var p = sessionData.players[_playerIndex];
            p.selectedCharacter = ready ? selectedChar : null;
        }

        if (ready) centerSlot.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack);
        else centerSlot.DOScale(1.0f, 0.2f).SetEase(Ease.InQuad);

        OnReadyStatusChanged?.Invoke(_playerIndex, _isReady);

        // Llamada al nuevo sistema de UI
        HandleDialogueUI(ready, selectedChar);
    }

    /// <summary>
    /// Maneja la UI de diálogo. Usa corrutina manual en vez de DOTween Sequence para el texto
    /// para poder sincronizar el audio del "blip" correctamente.
    /// </summary>
    private void HandleDialogueUI(bool show, CharacterData charData)
    {
        // 1. Matamos animaciones previas
        dialogueContainer.transform.DOKill();
        dialogueText.DOKill();
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        if (show)
        {
            dialogueContainer.SetActive(true);
            dialogueText.text = ""; // Limpiamos texto inicial
            dialogueContainer.transform.localScale = Vector3.zero;

            // Animación de entrada (Pop In)
            dialogueContainer.transform
                .DOScale(Vector3.one, popDuration)
                .SetEase(popInEase)
                .OnComplete(() =>
                {
                    // Cuando termina de aparecer la burbuja, empezamos a escribir
                    if (charData != null)
                    {
                        string textToType = charData.selectionQuote.GetLocalizedString();
                        _typingCoroutine = StartCoroutine(TypewriterRoutine(textToType, charData.voiceBlip));
                    }
                });
        }
        else
        {
            // Animación de salida (Pop Out)
            dialogueContainer.transform
                .DOScale(Vector3.zero, popDuration)
                .SetEase(popOutEase)
                .OnComplete(() =>
                {
                    dialogueContainer.SetActive(false);
                    dialogueText.text = "";
                });
        }

        nameText.color = show ? Color.yellow : Color.white;
    }

    IEnumerator TypewriterRoutine(string text, CycloneClip voiceBlip)
    {
        dialogueText.text = "";
        float nextAudioTime = 0f;
        float clipDuration = 0.1f;

        if (voiceBlip != null && voiceBlip.Clip != null)
            clipDuration = voiceBlip.Clip.length;

        bool currentPhraseExcited = CheckIfSentenceIsExcited(text, 0);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            dialogueText.text += c;

            // 1. PUNTUACIÓN
            if (c == '.' || c == '!' || c == '?')
            {
                currentPhraseExcited = CheckIfSentenceIsExcited(text, i + 1);
                yield return new WaitForSeconds(sentencePause);
                continue;
            }
            else if (c == ',')
            {
                yield return new WaitForSeconds(commaPause);
                continue;
            }

            // 2. AUDIO CON SUPERPOSICIÓN
            if (voiceBlip != null && !char.IsWhiteSpace(c))
            {
                if (Time.time >= nextAudioTime)
                {
                    float basePitch = UnityEngine.Random.Range(minPitch, maxPitch);
                    float finalPitch = currentPhraseExcited ? basePitch + excitementPitchOffset : basePitch;

                    if (CycloneAudioDriver.Instance != null)
                    {
                        CycloneAudioDriver.Instance.SetPitch(voiceBlip, finalPitch);
                        CycloneAudioDriver.Instance.PlayOneShot(voiceBlip); // Clave para superponer
                    }

                    // Calculamos el delay basado en el % de overlap
                    float waitTime = clipDuration * audioOverlap;
                    // Nunca esperar menos que la velocidad de escritura para evitar sonidos ametralladora excesivos
                    waitTime = Mathf.Max(waitTime, typingSpeed);

                    nextAudioTime = Time.time + waitTime;
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // Detecta si la siguiente frase termina en !
    private bool CheckIfSentenceIsExcited(string fullText, int startIndex)
    {
        if (startIndex >= fullText.Length) return false;
        for (int i = startIndex; i < fullText.Length; i++)
        {
            char c = fullText[i];
            if (c == '!') return true;
            if (c == '.' || c == '?') return false;
        }
        return false;
    }

    #endregion
}