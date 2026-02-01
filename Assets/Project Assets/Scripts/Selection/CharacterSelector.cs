using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;
using NexusChaser.CycloneAMS;

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

    [Header("Animation Config")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.OutQuad;
    [SerializeField] private float typeSpeed = 0.04f;

    [Header("Audio SFX")]
    [SerializeField] private CycloneClip moveSfx;    // Sonido al mover carrusel
    [SerializeField] private CycloneClip selectSfx;  // Sonido al confirmar (Ready)
    [SerializeField] private CycloneClip cancelSfx;

    // Internal State
    private int _playerIndex;
    private int _visualIndex; // Index within the _availableIndices list
    private bool _isAnimating;
    private bool _isReady;
    private float _moveDistance;
    private Coroutine _typingCoroutine;

    // The list of Database Indices that are actually valid for this player
    private List<int> _availableIndices = new List<int>();

    private void Start()
    {
        _playerIndex = GetComponent<PlayerInput>().playerIndex;
        _moveDistance = rightSlot.anchoredPosition.x;

        // 1. Build the list of allowed characters for this player
        RefreshCharacterList();

        // 2. Initial render
        UpdateVisuals(instant: true);
        dialogueContainer.SetActive(false);
    }

    /// <summary>
    /// Rebuilds the list of selectable characters. 
    /// Call this method if a character is unlocked at runtime via Cheat Code.
    /// </summary>
    public void RefreshCharacterList()
    {
        _availableIndices.Clear();

        // Get the specific player data to check their locked list
        Player currentPlayer = null;
        if (sessionData.players.Count > _playerIndex)
        {
            currentPlayer = sessionData.players[_playerIndex];
        }

        for (int i = 0; i < charDatabase.characterRoster.Length; i++)
        {
            CharacterData charData = charDatabase.characterRoster[i];

            // If player data exists, check if this character is in their locked list
            if (currentPlayer != null && currentPlayer.lockedChars.Contains(charData))
            {
                continue; // Skip secret character
            }

            // Otherwise, add its original index to the available list
            _availableIndices.Add(i);
        }

        // Reset visual index to 0 to avoid out of bounds after list changes
        _visualIndex = 0;
        UpdateVisuals(instant: true);
    }

    #region Input Handlers

    public void OnNavigate(InputAction.CallbackContext context)
    {
        // Mantenemos tus validaciones de estado y cantidad de personajes
        if (!context.performed || _isAnimating || _isReady) return;
        if (_availableIndices.Count <= 1) return;

        // Leemos el valor como Vector2
        Vector2 navigationInput = context.ReadValue<Vector2>();

        // Extraemos solo el eje X
        float moveX = navigationInput.x;

        // Aplicamos la lógica de movimiento basada en el eje X
        if (moveX > 0.5f)
        {
            MoveCarousel(1);
        }
        else if (moveX < -0.5f)
        {
            MoveCarousel(-1);
        }
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        if (!context.performed || _isAnimating || _isReady) return;

        // NUEVA VALIDACIÓN:
        // Si el CheatDetector dice que este botón fue parte del código secreto en este frame,
        // ignoramos la acción de Seleccionar.
        if (cheatDetector != null && cheatDetector.WasInputConsumedByCheat())
        {
            return;
        }

        SetReadyState(true);
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || !_isReady) return;

        // NUEVA VALIDACIÓN:
        // Lo mismo para cancelar. Si el botón era parte del cheat, no cancelamos.
        if (cheatDetector != null && cheatDetector.WasInputConsumedByCheat())
        {
            return;
        }

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

        // Determine targets based on direction
        RectTransform outgoing = centerSlot; // Center always leaves
        RectTransform incoming = (direction > 0) ? rightSlot : leftSlot;
        RectTransform wrapping = (direction > 0) ? leftSlot : rightSlot;

        float targetX = (direction > 0) ? -_moveDistance : _moveDistance;

        Sequence seq = DOTween.Sequence();
        seq.Join(outgoing.DOAnchorPosX(targetX, animDuration).SetEase(animEase));
        seq.Join(incoming.DOAnchorPosX(0, animDuration).SetEase(animEase));

        // Instant wrap
        float wrapStartPos = (direction > 0) ? _moveDistance : -_moveDistance;
        wrapping.anchoredPosition = new Vector2(wrapStartPos, 0);

        seq.OnComplete(() =>
        {
            SwapSlotReferences(direction);
            UpdateVisuals();

            // --- NUEVA MEJORA: Punch en el nombre al cambiar ---
            nameText.transform.DOPunchScale(Vector2.one * 0.15f, 0.2f);
            _isAnimating = false;
        });
    }

    private void SwapSlotReferences(int direction)
    {
        RectTransform temp;
        if (direction > 0) // Moving Right
        {
            temp = leftSlot;
            leftSlot = centerSlot;
            centerSlot = rightSlot;
            rightSlot = temp;
        }
        else // Moving Left
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

        // Circular Math on the AVAILABLE list, not the full database
        int centerLocalIdx = (_visualIndex % total + total) % total;
        int leftLocalIdx = ((centerLocalIdx - 1) % total + total) % total;
        int rightLocalIdx = ((centerLocalIdx + 1) % total + total) % total;

        // Map local visual index -> Real Database Index
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

    /// <summary>
    /// Fuerza la visualización hacia un personaje específico (usado por Cheat Codes)
    /// </summary>
    // Actualización de JumpToCharacter en CharacterSelector.cs (llamado desde CheatCodeDetector)
    public void JumpToCharacter(CharacterData targetChar)
    {
        RefreshCharacterList();

        int targetVisualIndex = _availableIndices.FindIndex(id => charDatabase.GetCharacter(id) == targetChar);

        if (targetVisualIndex != -1)
        {
            _isAnimating = false;
            _visualIndex = targetVisualIndex;
            UpdateVisuals(instant: true);

            // --- NUEVA MEJORA: DESBLOQUEO ÉPICO ---
            Sequence unlockSeq = DOTween.Sequence();
            // Flash blanco
            centerSlot.GetComponent<Image>().DOBlendableColor(Color.white, 0.1f).SetLoops(2, LoopType.Yoyo);
            // Salto de escala grande
            unlockSeq.Append(transform.DOPunchScale(Vector3.one * 0.4f, 0.6f, 10, 1f));
            // Agitación de cámara (simulada en el objeto)
            unlockSeq.Join(transform.DOShakePosition(0.5f, 20f, 20));

            Debug.Log("¡PERSONAJE SECRETO DESBLOQUEADO!");
        }
    }

    #endregion

    #region Ready & Dialogue State

    private void SetReadyState(bool ready)
    {
        _isReady = ready;

        // Important: Get the CURRENT available character
        int total = _availableIndices.Count;
        int localIndex = (_visualIndex % total + total) % total;
        int realDbIndex = _availableIndices[localIndex];

        CharacterData selectedChar = charDatabase.GetCharacter(realDbIndex);

        if (CycloneAudioDriver.Instance != null)
        {
            if (ready && selectSfx != null)
            {
                CycloneAudioDriver.Instance.PlayOneShot(selectSfx);
            }
            else if (!ready && cancelSfx != null)
            {
                CycloneAudioDriver.Instance.PlayOneShot(cancelSfx);
            }
        }

        // Update Session Data
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
        HandleDialogueUI(ready, selectedChar);
    }

    private void HandleDialogueUI(bool show, CharacterData charData)
    {
        if (show)
        {
            dialogueContainer.SetActive(true);
            dialogueContainer.transform.localScale = Vector3.zero;
            dialogueContainer.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            dialogueContainer.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => dialogueContainer.SetActive(false));
        }

        //dialogueContainer.SetActive(show);
        nameText.color = show ? Color.yellow : Color.white;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        if (show && charData != null)
        {
            _typingCoroutine = StartCoroutine(TypewriterRoutine(charData.selectionQuote.GetLocalizedString()));
        }
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            // AudioManager.Play(voiceBlip); 
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    #endregion
}