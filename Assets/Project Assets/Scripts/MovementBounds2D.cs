using UnityEngine;

public class MovementBounds2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Collider2D targetCollider;

    // Ya no son SerializeField porque se controlan por código, 
    // pero las dejamos privadas para uso interno.
    private Vector2 areaSize;
    private Vector2 areaCenter;
    private bool isInitialized = false;

    private void Awake()
    {
        // Auto-asignar collider si no viene puesto en el prefab
        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Configura los límites de movimiento dinámicamente.
    /// </summary>
    /// <param name="center">El centro del área en coordenadas de mundo.</param>
    /// <param name="size">El tamaño total (ancho y alto) del área.</param>
    public void SetBounds(Vector2 center, Vector2 size)
    {
        areaCenter = center;
        areaSize = size;
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || targetCollider == null) return;

        // 1. Obtenemos los límites del área definida
        float halfAreaWidth = areaSize.x / 2f;
        float halfAreaHeight = areaSize.y / 2f;

        // 2. Obtenemos la mitad del tamaño del objeto (para que no se salga la mitad del sprite)
        float objHalfWidth = targetCollider.bounds.extents.x;
        float objHalfHeight = targetCollider.bounds.extents.y;

        // 3. Calculamos los límites finales (Clamp)
        // El área jugable real es el área total MENOS el tamaño del personaje
        float minX = (areaCenter.x - halfAreaWidth) + objHalfWidth;
        float maxX = (areaCenter.x + halfAreaWidth) - objHalfWidth;
        float minY = (areaCenter.y - halfAreaHeight) + objHalfHeight;
        float maxY = (areaCenter.y + halfAreaHeight) - objHalfHeight;

        // 4. Aplicamos la restricción
        Vector3 currentPos = targetCollider.transform.position;

        // Mathf.Clamp asegura que el valor se mantenga entre min y max
        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

        targetCollider.transform.position = currentPos;
    }

    private void OnDrawGizmos()
    {
        // Solo dibujamos si se ha inicializado o si estamos probando (podrías forzar valores aquí para debug)
        if (isInitialized)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize.x, areaSize.y, 0f));
        }
    }
}