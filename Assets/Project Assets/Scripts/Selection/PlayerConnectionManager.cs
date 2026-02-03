using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;
using NexusChaser.CycloneAMS; // [Cita: PlayerConnectionManager.cs]

public class PlayerConnectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayersSessionData session;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Settings")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private int minPlayersRequired = 2;
    [SerializeField] private string sceneToLoad;

    [Header("Audio Settings")]
    [SerializeField] private CycloneClip playerJoinClip;      // Sonido al entrar jugador
    [SerializeField] private CycloneClip countdownTickClip;   // Sonido: 3... 2... 1...
    [SerializeField] private CycloneClip countdownGoClip;     // Sonido: GO!

    [Header("Events")]
    public UnityEvent OnAllPlayersReady;

    private Dictionary<int, bool> _playerReadyStates = new Dictionary<int, bool>();
    private Coroutine _countdownCoroutine;
    private bool _gameHasStarted = false;

    private void Awake()
    {
        if (session != null)
        {
            session.ResetSession();
            ResetAllCharacterDialogues();
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void ResetAllCharacterDialogues()
    {
        // Verificamos que tengamos acceso a la base de datos a través de la sesión
        if (session.charDB != null && session.charDB.characterRoster != null)
        {
            foreach (var character in session.charDB.characterRoster)
            {
                // Verificamos que el personaje y su data de diálogo existan
                if (character != null && character.dialogueData != null)
                {
                    // Reiniciamos la memoria (Shuffle Bag) de este personaje
                    character.dialogueData.ResetHistory();
                }
            }
            Debug.Log("[PlayerConnectionManager] All character dialogue histories have been reset.");
        }
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        if (session == null) return;

        session.AddPlayer(input);
        input.gameObject.name = $"Player_{input.playerIndex}";

        if (container != null)
        {
            input.transform.SetParent(container, false);

            // Feedback visual
            input.transform.localScale = Vector3.zero;
            input.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        // --- Feedback de Audio (Player Join) ---
        PlaySound(playerJoinClip);
        // ---------------------------------------

        _playerReadyStates[input.playerIndex] = false;

        CharacterSelector selector = input.GetComponent<CharacterSelector>();
        if (selector != null)
        {
            selector.OnReadyStatusChanged += HandlePlayerReadyStatus;
        }
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        if (_gameHasStarted) return;

        if (session != null)
        {
            session.RemovePlayer(input);
        }

        if (_playerReadyStates.ContainsKey(input.playerIndex))
        {
            _playerReadyStates.Remove(input.playerIndex);
        }

        CharacterSelector selector = input.GetComponent<CharacterSelector>();
        if (selector != null)
        {
            selector.OnReadyStatusChanged -= HandlePlayerReadyStatus;
        }

        CheckGlobalReadiness();
    }

    private void HandlePlayerReadyStatus(int playerIndex, bool isReady)
    {
        if (_playerReadyStates.ContainsKey(playerIndex))
        {
            _playerReadyStates[playerIndex] = isReady;
        }

        CheckGlobalReadiness();
    }

    private void CheckGlobalReadiness()
    {
        if (_playerReadyStates.Count == 0)
        {
            StopCountdown();
            return;
        }

        bool hasMinPlayers = _playerReadyStates.Count >= minPlayersRequired;
        bool allReady = _playerReadyStates.Count > 0 && _playerReadyStates.Values.All(state => state == true);

        if (hasMinPlayers && allReady)
        {
            if (_countdownCoroutine == null)
            {
                _countdownCoroutine = StartCoroutine(StartCountdownRoutine());
            }
        }
        else
        {
            StopCountdown();
        }
    }

    private void StopCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartCountdownRoutine()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        int remainingTime = countdownSeconds;

        while (remainingTime > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = remainingTime.ToString();

                // Animación visual
                countdownText.transform.localScale = Vector3.one * 1.5f;
                countdownText.transform.DOScale(1f, 0.5f);
            }

            // --- Feedback de Audio (Tick) ---
            PlaySound(countdownTickClip);
            // --------------------------------

            yield return new WaitForSeconds(1f);
            remainingTime--;
        }

        if (countdownText != null) countdownText.text = "0"; // O "GO!"

        // --- Feedback de Audio (GO!) ---
        PlaySound(countdownGoClip);
        // -------------------------------

        Debug.Log("GO!");
        _gameHasStarted = true;

        OnAllPlayersReady?.Invoke();

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneLoader.Instance.LoadLevel(sceneToLoad);
        }
    }

    // Helper para simplificar la llamada y chequeo de nulos
    private void PlaySound(CycloneClip clip)
    {
        if (CycloneAudioDriver.Instance != null && clip != null)
        {
            CycloneAudioDriver.Instance.PlayOneShot(clip); // [Cite: CycloneAudioDriver.cs]
        }
    }
}