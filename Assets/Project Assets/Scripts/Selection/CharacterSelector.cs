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
        _isAnimating = true;
        _visualIndex += direction;

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
    public void JumpToCharacter(CharacterData targetChar)
    {
        // 1. Asegurarnos de que la lista interna esté actualizada (incluyendo el secreto)
        RefreshCharacterList();

        // 2. Buscar en qué posición visual quedó ese personaje
        // OJO: Buscamos en _availableIndices porque es la lista que usa el carrusel
        int targetVisualIndex = -1;

        for (int i = 0; i < _availableIndices.Count; i++)
        {
            int realDbIndex = _availableIndices[i];
            if (charDatabase.GetCharacter(realDbIndex) == targetChar)
            {
                targetVisualIndex = i;
                break;
            }
        }

        // 3. Si lo encontramos, movemos el carrusel
        if (targetVisualIndex != -1)
        {
            // Cancelamos cualquier animación en curso para evitar conflictos
            _isAnimating = false;
            DOTween.Kill(leftSlot);
            DOTween.Kill(centerSlot);
            DOTween.Kill(rightSlot);

            // Actualizamos el índice visual actual
            _visualIndex = targetVisualIndex;

            // Forzamos la actualización visual instantánea
            UpdateVisuals(instant: true);

            // Opcional: Feedback visual (sonido o parpadeo) de que se desbloqueó
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
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

        // Update Session Data
        if (sessionData.players.Count > _playerIndex)
        {
            var p = sessionData.players[_playerIndex];
            p.selectedCharacter = ready ? selectedChar : null;
        }

        OnReadyStatusChanged?.Invoke(_playerIndex, _isReady);
        HandleDialogueUI(ready, selectedChar);
    }

    private void HandleDialogueUI(bool show, CharacterData charData)
    {
        dialogueContainer.SetActive(show);
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