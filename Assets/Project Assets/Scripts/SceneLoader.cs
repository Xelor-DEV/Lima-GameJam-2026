using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening; // Necesario para DOTween
using System.Threading.Tasks; // Necesario para async/await

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("UI References")]
    [SerializeField] private RectTransform container; // El objeto padre que hará POP
    [SerializeField] private Image progressBar;       // La imagen con Fill Amount

    [Header("Settings")]
    [SerializeField] private float popDuration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutBack; // Efecto rebote al aparecer
    [SerializeField] private Ease hideEase = Ease.InBack;  // Efecto anticipación al irse

    private void Awake()
    {
        // Lógica Singleton Persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicialización: Ocultar contenedor y resetear barra
        container.localScale = Vector3.zero;
        progressBar.fillAmount = 0f;
    }

    /// <summary>
    /// Método público para llamar desde cualquier lugar: SceneLoader.Instance.LoadLevel("NombreEscena");
    /// </summary>
    public async void LoadLevel(string sceneName)
    {
        // 1. Pausar el juego para evitar lógica corriendo de fondo mientras cargamos
        Time.timeScale = 0;

        // 2. Efecto POP de aparición
        // Usamos .SetUpdate(true) para que la animación corra incluso con Time.timeScale = 0
        await container.DOScale(Vector3.one, popDuration)
            .SetEase(showEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        // 3. Cargar la escena asíncronamente
        await LoadSceneAsync(sceneName);

        // 4. Efecto POP de desaparición (Inverso)
        await container.DOScale(Vector3.zero, popDuration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        // 5. Despausar el juego y limpiar
        progressBar.fillAmount = 0f;
        Time.timeScale = 1;
    }

    private async Task LoadSceneAsync(string sceneName)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName);

        // Evitamos que la escena se active sola inmediatamente para controlar la barra
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // El progreso de Unity va de 0 a 0.9 mientras carga.
            // Dividimos entre 0.9 para normalizarlo a 0 -> 1 para nuestra barra.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            progressBar.fillAmount = progress;

            // Cuando llega a 0.9, la carga técnica ha terminado
            if (operation.progress >= 0.9f)
            {
                // (Opcional) Pequeña espera visual para que el usuario vea la barra al 100%
                progressBar.fillAmount = 1f;
                await Task.Delay(500);

                // Permitimos el cambio de escena
                operation.allowSceneActivation = true;
            }

            // Esperamos al siguiente frame
            await Task.Yield();
        }
    }
}