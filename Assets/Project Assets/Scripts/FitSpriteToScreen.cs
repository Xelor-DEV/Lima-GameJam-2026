using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitSpriteToScreen : MonoBehaviour
{
    void Awake()
    {
        ScaleToFill();
    }

    void ScaleToFill()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // 1. Resetear la escala para cálculos limpios
        transform.localScale = Vector3.one;

        // 2. Obtener dimensiones del sprite en unidades de Unity
        float width = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;

        // 3. Obtener dimensiones de la cámara (Orthographic)
        float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        // 4. Calcular el factor de escala necesario
        Vector3 newScale = transform.localScale;
        newScale.x = worldScreenWidth / width;
        newScale.y = worldScreenHeight / height;

        // 5. Aplicar la nueva escala
        transform.localScale = newScale;
    }
}