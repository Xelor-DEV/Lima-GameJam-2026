using UnityEngine;
using UnityEngine.Events;

// A versatile initializer script that triggers UnityEvents during 
// the Awake and Start lifecycle phases.
public class InitializationHandler : MonoBehaviour
{
    [Header("Lifecycle Events")]
    [Tooltip("Actions to execute when the script instance is being loaded.")]
    public UnityEvent onAwakeEvent;

    [Tooltip("Actions to execute before the first frame update.")]
    public UnityEvent onStartEvent;

    private void Awake()
    {
        onAwakeEvent?.Invoke();
    }

    private void Start()
    {
        onStartEvent?.Invoke();
    }
}