using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteScreenScaler : MonoBehaviour
{
    private Camera _cam;
    private float _initialCamSize;
    private Vector3 _initialScale;

    void Start()
    {
        _cam = Camera.main;

        // Guardamos los valores iniciales como referencia
        if (_cam != null)
        {
            _initialCamSize = _cam.orthographicSize;
        }
        _initialScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (_cam == null) return;

        // Calculamos el ratio de cambio en la cámara
        float currentCamSize = _cam.orthographicSize;
        float sizeRatio = currentCamSize / _initialCamSize;

        // Aplicamos ese ratio a la escala original para que visualmente se vea igual
        transform.localScale = _initialScale * sizeRatio;
    }
}