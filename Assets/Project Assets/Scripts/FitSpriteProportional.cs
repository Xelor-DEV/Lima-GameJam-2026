using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitSpriteProportionalRelative : MonoBehaviour
{
    void Awake()
    {
        AdjustScaleRelative();
    }

    void AdjustScaleRelative()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // 1. Centrar el objeto respecto a la cámara
        Vector3 camPos = Camera.main.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);

        // 2. Obtener altura de la cámara y del sprite (en su estado actual)
        // sr.bounds.size toma en cuenta la escala actual del Transform
        float currentSpriteHeight = sr.bounds.size.y;
        float worldScreenHeight = Camera.main.orthographicSize * 2.0f;

        // 3. Calcular cuánto le falta (o le sobra) para llegar al alto de pantalla
        float factor = worldScreenHeight / currentSpriteHeight;

        // 4. Multiplicar la escala actual por ese factor
        // Esto mantiene la proporción y respeta la escala inicial
        transform.localScale *= factor;
    }
}