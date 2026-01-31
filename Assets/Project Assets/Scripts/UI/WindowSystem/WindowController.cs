using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WindowController : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("El GameObject REAL de la ventana (el Panel) que se activará/desactivará")]
    [SerializeField] private GameObject windowView;

    [Header("Referencias de Navegación")]
    [Tooltip("El botón externo que abre esta ventana (para volver el foco al cerrar)")]
    [SerializeField] private Button openButtonTrigger;

    [Tooltip("El botón interno para cerrar la ventana")]
    [SerializeField] private Button closeButton;

    [Tooltip("El elemento que debe seleccionarse primero al abrir la ventana")]
    [SerializeField] private GameObject firstSelectedObject;

    [Header("Comportamiento de Bloqueo")]
    [SerializeField] private bool shouldBlockButtons = false;
    [Tooltip("Lista de botones a desactivar mientras esta ventana está abierta")]
    [ReorderableList(ListStyle.Boxed, "Buttons to Block", Foldable = true)]
    [SerializeField] private Button[] buttonsToBlock;

    [Header("Eventos")]
    [Tooltip("Se ejecuta INMEDIATAMENTE al llamar abrir (antes de la animación)")]
    public UnityEvent OnWindowOpenStart;
    [Tooltip("Se ejecuta INMEDIATAMENTE al llamar cerrar (antes de la animación)")]
    public UnityEvent OnWindowCloseStart;
    [Tooltip("Se ejecuta DESPUÉS de terminar la animación de entrada")]
    public UnityEvent OnWindowShown;
    [Tooltip("Se ejecuta DESPUÉS de terminar la animación de salida")]
    public UnityEvent OnWindowHidden;

    private IWindowAnimation _animationModule;
    private bool _isAnimating = false;

    private void Awake()
    {
        // Buscamos si hay un módulo de animación adjunto (Strategy Pattern)
        _animationModule = GetComponent<IWindowAnimation>();

        if (windowView != null && windowView.activeSelf)
        {
            windowView.SetActive(false);
        }

        // Asignar listeners automáticamente si están referenciados
        if (openButtonTrigger != null)
            openButtonTrigger.onClick.AddListener(OpenWindow);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseWindow);
    }

    public async void OpenWindow()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // 0. Evento Inmediato (NUEVO)
        OnWindowOpenStart?.Invoke();

        // 1. Activar el objeto base
        windowView.SetActive(true);

        // 2. Bloquear botones externos si es necesario
        ToggleExternalButtons(false);

        // 3. Ejecutar animación (Modular o Default)
        if (_animationModule != null)
        {
            await _animationModule.AnimateOpen(windowView);
        }

        // 4. Manejo del Foco (Accessibility/Gamepad)
        if (firstSelectedObject != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); // Limpiar selección previa
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
        }

        // 5. Disparar evento final
        OnWindowShown?.Invoke();
        _isAnimating = false;
    }

    public async void CloseWindow()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // 0. Evento Inmediato (NUEVO)
        OnWindowCloseStart?.Invoke();

        // 1. Ejecutar animación de cierre
        if (_animationModule != null)
        {
            await _animationModule.AnimateClose(windowView);
        }

        // 2. Desactivar el objeto (si no hay modulo, esto es instantáneo)
        windowView.SetActive(false);

        // 3. Restaurar interactividad de botones externos
        ToggleExternalButtons(true);

        // 4. Restaurar el foco al botón que abrió la ventana
        if (openButtonTrigger != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(openButtonTrigger.gameObject);
        }

        // 5. Disparar evento final
        OnWindowHidden?.Invoke();
        _isAnimating = false;
    }

    private void ToggleExternalButtons(bool isInteractable)
    {
        if (!shouldBlockButtons || buttonsToBlock == null) return;

        foreach (var btn in buttonsToBlock)
        {
            if (btn != null) btn.interactable = isInteractable;
        }
    }
}