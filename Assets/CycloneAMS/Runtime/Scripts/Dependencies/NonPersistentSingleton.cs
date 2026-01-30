using UnityEngine;

public class NonPersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [Title("Singleton Configuration")]

    [Label("Destroy Whole GameObject?")]

    [LeftToggle]
    [SerializeField]
    [Tooltip("Defines behavior when a duplicate is found:\n" +
              "True: Destroys the whole GameObject (recommended for managers).\n" +
              "False: Destroys only this component (useful if the object has other scripts attached).")]
    private bool _destroyWholeObjectOnDuplicate = true;


    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // Buscamos todos los objetos del tipo T
                    var objs = FindObjectsByType<T>(FindObjectsSortMode.None);

                    if (objs.Length > 0)
                    {
                        // Nos quedamos con el primero como la instancia oficial
                        _instance = objs[0];
                    }

                    if (objs.Length > 1)
                    {
                        Debug.LogError($"[Singleton] More than one '{typeof(T)}' found in the scene. Destroying duplicates.");

                        // Empezamos en 1 porque el 0 es la instancia válida
                        for (int i = 1; i < objs.Length; i++)
                        {
                            // Casteamos para poder leer la variable _destroyWholeObjectOnDuplicate de esa instancia específica
                            var duplicate = objs[i] as NonPersistentSingleton<T>;

                            // Si podemos leer su configuración y dice true, o si el cast falla (por seguridad), borramos el objeto
                            if (duplicate != null && duplicate._destroyWholeObjectOnDuplicate)
                            {
                                Destroy(objs[i].gameObject);
                            }
                            else
                            {
                                Destroy(objs[i]); // Solo borramos el componente
                            }
                        }
                    }

                    if (_instance == null)
                    {
                        GameObject singletonObj = new GameObject();
                        _instance = singletonObj.AddComponent<T>();
                        singletonObj.name = typeof(T).ToString() + " (Singleton)";
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicado de '{typeof(T)}' detectado en '{name}'.");

            if (_destroyWholeObjectOnDuplicate)
            {
                Destroy(gameObject); // Borra el GameObject entero (comportamiento clásico)
            }
            else
            {
                Destroy(this); // Borra solo el script, deja el GameObject vivo
            }
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = false;
            _instance = null;
        }
    }
}