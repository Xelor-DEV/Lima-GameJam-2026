using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;
using NexusChaser.CycloneAMS;
// Asegúrate de incluir Localization si usas GetLocalizedString()
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

    // --- NUEVO: Configuración "Bonita" copiada del PlayerDialogueController ---
    [Header("Dialogue Animation (Typewriter)")]
    [SerializeField] private float typingSpeed = 0.05f; // Velocidad de escritura
    [SerializeField] private float popDuration = 0.4f;  // Duración de aparecer/desaparecer
    [SerializeField] private Ease popInEase = Ease.OutBack; // Rebote al entrar
    [SerializeField] private Ease popOutEase = Ease.InBack; // Suavidad al salir

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

    // Ya no necesitamos _typingCoroutine porque usaremos DOTween

    private List<int> _availableIndices = new List<int>();

    private void Start()
    {
        _playerIndex = GetComponent<PlayerInput>().playerIndex;
        _moveDistance = rightSlot.anchoredPosition.x;

        RefreshCharacterList();

        UpdateVisuals(instant: true);

        // Inicialización segura del diálogo
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

        if (ready)
        {
            centerSlot.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack);
        }
        else
        {
            centerSlot.DOScale(1.0f, 0.2f).SetEase(Ease.InQuad);
        }

        OnReadyStatusChanged?.Invoke(_playerIndex, _isReady);

        // Llamada al nuevo sistema de UI
        HandleDialogueUI(ready, selectedChar);
    }

    /// <summary>
    /// Método refactorizado para usar DOTween Sequence y DOText,
    /// imitando el estilo de PlayerDialogueController.
    /// </summary>
    private void HandleDialogueUI(bool show, CharacterData charData)
    {
        // 1. Matamos cualquier tween previo en el contenedor y el texto
        // para evitar conflictos si el jugador pulsa botones rápido.
        dialogueContainer.transform.DOKill();
        dialogueText.DOKill();

        if (show)
        {
            dialogueContainer.SetActive(true);
            dialogueText.text = ""; // Limpiamos texto
            dialogueContainer.transform.localScale = Vector3.zero;

            // Creamos una secuencia para encadenar: Aparecer -> Escribir
            Sequence seq = DOTween.Sequence();

            // Paso A: Pop In (Escala de 0 a 1 con rebote)
            seq.Append(dialogueContainer.transform.DOScale(Vector3.one, popDuration).SetEase(popInEase));

            // Paso B: Escribir texto (Typewriter)
            if (charData != null)
            {
                string textToType = charData.selectionQuote.GetLocalizedString();
                float totalTypeDuration = textToType.Length * typingSpeed;

                // DOText escribe el texto letra por letra de forma optimizada
                seq.Append(dialogueText.DOText(textToType, totalTypeDuration).SetEase(Ease.Linear));
            }
        }
        else
        {
            // Pop Out (Escala de 1 a 0) y luego desactivar
            dialogueContainer.transform
                .DOScale(Vector3.zero, popDuration)
                .SetEase(popOutEase)
                .OnComplete(() =>
                {
                    dialogueContainer.SetActive(false);
                    dialogueText.text = "";
                });
        }

        // Cambio de color del nombre (feedback visual extra)
        nameText.color = show ? Color.yellow : Color.white;
    }

    #endregion
}