using UnityEngine;

public class MovementBounds2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Collider2D targetCollider;

    [Header("Configuración del Área")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Color gizmoColor = Color.green;

    private void LateUpdate()
    {
        if (targetCollider == null) return;

        // Obtenemos los límites del área
        float halfWidth = areaSize.x / 2f;
        float halfHeight = areaSize.y / 2f;

        // Obtenemos la mitad del tamaño del collider (extents)
        // Esto funciona para cualquier tipo de Collider2D
        float objHalfWidth = targetCollider.bounds.extents.x;
        float objHalfHeight = targetCollider.bounds.extents.y;

        // Calculamos los límites finales restando el tamaño del objeto
        float minX = (areaCenter.x - halfWidth) + objHalfWidth;
        float maxX = (areaCenter.x + halfWidth) - objHalfWidth;
        float minY = (areaCenter.y - halfHeight) + objHalfHeight;
        float maxY = (areaCenter.y + halfHeight) - objHalfHeight;

        // Aplicamos la restricción
        Vector3 currentPos = targetCollider.transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

        targetCollider.transform.position = currentPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        // Dibujamos el área rectangular
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize.x, areaSize.y, 0f));

        // Dibujamos un pequeño icono para el centro
        Gizmos.DrawSphere(areaCenter, 0.1f);
    }
}