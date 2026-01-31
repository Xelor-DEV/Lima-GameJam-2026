using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

public class DOTweenPopModule : MonoBehaviour, IWindowAnimation
{
    [Header("Configuración del Pop")]
    [SerializeField] private float duration = 0.3f;

    [Header("Curvas de Animación")]
    [Tooltip("OutBack crea el efecto de rebote al final (crece más de 1 y vuelve)")]
    [SerializeField] private Ease openEase = Ease.OutBack;

    [Tooltip("InBack crea una anticipación (se encoge un poco antes de desaparecer)")]
    [SerializeField] private Ease closeEase = Ease.InBack;

    private Vector3? _cachedScale = null;

    public async Task AnimateOpen(GameObject windowContent)
    {
        RectTransform rect = windowContent.GetComponent<RectTransform>();

        if (rect == null) return;

        // 1. CAPTURA DE ESCALA (Solo la primera vez o si no se ha guardado)
        // Verificamos si aun no tenemos la escala guardada y si la actual es válida (no es cero)
        if (_cachedScale == null && rect.localScale != Vector3.zero)
        {
            _cachedScale = rect.localScale;
        }
        // Fallback de seguridad: Si por alguna razón llegó siendo 0, asumimos 1 para no romperlo,
        // pero idealmente usará el valor que pusiste en el editor.
        Vector3 targetScale = _cachedScale ?? Vector3.one;

        // 2. PREPARACIÓN
        // Colapsamos la ventana a 0 para el efecto pop
        rect.localScale = Vector3.zero;

        // 3. ANIMACIÓN
        Sequence seq = DOTween.Sequence();

        // Usamos 'targetScale' en lugar de Vector3.one
        seq.Join(rect.DOScale(targetScale, duration).SetEase(openEase));

        await seq.AsyncWaitForCompletion();
    }

    public async Task AnimateClose(GameObject windowContent)
    {
        RectTransform rect = windowContent.GetComponent<RectTransform>();

        if (rect == null) return;

        Sequence seq = DOTween.Sequence();

        // Animamos hacia CERO
        seq.Join(rect.DOScale(Vector3.zero, duration).SetEase(closeEase));

        await seq.AsyncWaitForCompletion();

        // 4. RESTAURACIÓN
        // IMPORTANTÍSIMO: Devolvemos la escala a su tamaño original antes de que se desactive.
        // Si no hacemos esto, la próxima vez que se active el objeto (antes de llamar a AnimateOpen),
        // tendrá escala 0 y nuestra lógica de captura fallaría o se vería raro.
        if (_cachedScale != null)
        {
            rect.localScale = _cachedScale.Value;
        }
        else
        {
            rect.localScale = Vector3.one; // Fallback
        }
    }
}