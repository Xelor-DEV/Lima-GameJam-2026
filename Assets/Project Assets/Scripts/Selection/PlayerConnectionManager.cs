using System.Collections; // Necesario para Corrutinas
using System.Collections.Generic;
using System.Linq;
using TMPro; // Necesario para TextMeshPro
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena
using DG.Tweening;

public class PlayerConnectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayersSessionData session;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI countdownText; // Referencia al texto

    [Header("Settings")]
    [SerializeField] private int countdownSeconds = 3; // Tiempo configurable
    [SerializeField] private int minPlayersRequired = 2;
    [SerializeField] private string sceneToLoad; // Nombre de la escena a cargar

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
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
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

            // --- NUEVA MEJORA: Feedback de entrada ---
            input.transform.localScale = Vector3.zero;
            input.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

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

        // 1. Verificamos que haya el mínimo de jugadores requeridos
        bool hasMinPlayers = _playerReadyStates.Count >= minPlayersRequired;

        // 2. Verificamos si todos los que están conectados están listos
        bool allReady = _playerReadyStates.Count > 0 && _playerReadyStates.Values.All(state => state == true);

        // Iniciamos cuenta atrás solo si se cumplen AMBAS condiciones
        if (hasMinPlayers && allReady)
        {
            if (_countdownCoroutine == null)
            {
                _countdownCoroutine = StartCoroutine(StartCountdownRoutine());
            }
        }
        else
        {
            // Si alguien cancela o un jugador se va bajando del mínimo, detenemos la cuenta
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
                // Opcional: Pequeña animación de escala con DOTween
                countdownText.transform.localScale = Vector3.one * 1.5f;
                countdownText.transform.DOScale(1f, 0.5f);
            }

            yield return new WaitForSeconds(1f);
            remainingTime--;
        }

        if (countdownText != null) countdownText.text = "0";

        Debug.Log("GO!");
        _gameHasStarted = true;

        OnAllPlayersReady?.Invoke();

        // Cambiar de escena
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}