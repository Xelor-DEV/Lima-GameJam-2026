using UnityEngine;
using System.Collections.Generic;

public class CuttingPattern : MonoBehaviour
{
    [Header("Configuración de la Figura")]
    public List<Transform> points; // Tus esquinas (Transforms)
    public bool isClosedLoop = true;

    [Header("Configuración de Juego")]
    [Tooltip("Distancia máxima permitida para no perder (Ancho del camino)")]
    public float errorMargin = 0.5f;
    [Tooltip("Radio de corte: qué tan cerca debe pasar la tijera para validar un tramo")]
    public float cutDetectionRadius = 0.2f;
    [Tooltip("Densidad: Cada cuánto espacio generar un punto de control (menor = más precisión)")]
    public float nodeDensity = 0.1f;

    // Estructura interna para recordar qué parte ya cortamos
    private class CutNode
    {
        public Vector2 position;
        public bool isCut;
        public CutNode(Vector2 pos) { position = pos; isCut = false; }
    }

    private List<CutNode> _cutNodes = new List<CutNode>();
    private int _totalNodes = 0;
    private int _cutCount = 0;

    void Start()
    {
        GenerateCutNodes();
    }

    // Genera puntos invisibles a lo largo de todas las líneas
    void GenerateCutNodes()
    {
        _cutNodes.Clear();
        if (points.Count < 2) return;

        for (int i = 0; i < points.Count; i++)
        {
            if (i == points.Count - 1 && !isClosedLoop) break;

            Vector2 start = points[i].position;
            Vector2 end = (i == points.Count - 1) ? points[0].position : points[i + 1].position;

            float distance = Vector2.Distance(start, end);
            // Calculamos cuántos nodos caben en esta línea
            int nodesToSpawn = Mathf.CeilToInt(distance / nodeDensity);

            for (int j = 0; j <= nodesToSpawn; j++)
            {
                float t = (float)j / nodesToSpawn;
                Vector2 pos = Vector2.Lerp(start, end, t);
                _cutNodes.Add(new CutNode(pos));
            }
        }
        _totalNodes = _cutNodes.Count;
    }

    // --- LÓGICA DE CORTE ---

    // 1. Verifica si estamos cortando nodos nuevos
    public float UpdateCuttingProgress(Vector2 scissorPos)
    {
        foreach (var node in _cutNodes)
        {
            if (!node.isCut)
            {
                // Si la tijera toca este nodo, márcalo como cortado
                if (Vector2.Distance(scissorPos, node.position) <= cutDetectionRadius)
                {
                    node.isCut = true;
                    _cutCount++;
                }
            }
        }

        // Retorna el porcentaje (0.0 a 1.0)
        return (float)_cutCount / Mathf.Max(1, _totalNodes);
    }

    // 2. Verifica si nos salimos del margen (Igual que antes)
    public bool IsPositionSafe(Vector2 scissorPosition)
    {
        if (points.Count < 2) return false;
        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            if (i == points.Count - 1 && !isClosedLoop) break;
            Vector2 p1 = points[i].position;
            Vector2 p2 = (i == points.Count - 1) ? points[0].position : points[i + 1].position;

            float d = GetDistanceFromPointToLineSegment(scissorPosition, p1, p2);
            if (d < minDistance) minDistance = d;
        }
        return minDistance <= errorMargin;
    }

    // Utilidad matemática
    float GetDistanceFromPointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = point - a;
        float magnitudeAB = ab.sqrMagnitude;
        if (magnitudeAB == 0) return ap.magnitude;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / magnitudeAB);
        return Vector2.Distance(point, a + ab * t);
    }

    // --- GIZMOS MEJORADOS ---
    private void OnDrawGizmos()
    {
        // Dibujo original del margen
        if (points == null || points.Count < 2) return;

        // Dibuja los nodos de progreso (Solo en Play Mode se verán los cambios)
        if (Application.isPlaying && _cutNodes != null)
        {
            foreach (var node in _cutNodes)
            {
                // Verde si ya se cortó, Rojo si falta
                Gizmos.color = node.isCut ? Color.green : Color.red;
                Gizmos.DrawSphere(node.position, 0.05f);
            }
        }
        else
        {
            // En modo editor, dibuja líneas azules para mostrar la forma
            for (int i = 0; i < points.Count; i++)
            {
                if (i == points.Count - 1 && !isClosedLoop) break;
                Transform next = (i == points.Count - 1) ? points[0] : points[i + 1];
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(points[i].position, next.position);
            }
        }

        // Dibuja el margen de error
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        foreach (var p in points) if (p != null) Gizmos.DrawWireSphere(p.position, errorMargin);
    }
}