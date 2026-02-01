using UnityEngine;
using System.Collections.Generic;

public class CuttingPattern : MonoBehaviour
{
    [Header("Configuración de la Figura")]
    public List<Transform> points;
    public bool isClosedLoop = true;

    [Header("Configuración Visual")]
    [Tooltip("Prefab que contiene un LineRenderer. Se instanciará uno por cada tramo.")]
    public GameObject lineSegmentPrefab;
    [Tooltip("Ancho de la línea visual")]
    public float lineWidth = 0.1f;

    [Header("Dificultad")]
    public float errorMargin = 0.5f;
    public float cutDetectionRadius = 0.2f;
    public float nodeDensity = 0.1f; // Menor número = más segmentos = más suavidad

    // Estructura interna
    private class CutNode
    {
        public Vector3 worldPosition;
        public bool isCut;
        public GameObject visualSegment; // Referencia al objeto visual de este tramo

        public CutNode(Vector3 pos) { worldPosition = pos; isCut = false; }
    }

    private List<CutNode> _cutNodes = new List<CutNode>();
    private int _totalNodes = 0;
    private int _cutCount = 0;
    private Color _myColor = Color.white;

    // Llamado por ScissorsController al inicio
    public void InitializeVisuals(Color playerColor)
    {
        _myColor = playerColor;
        GenerateNodesAndVisuals();
    }

    void GenerateNodesAndVisuals()
    {
        // 1. Limpieza previa (por si se reinicia)
        foreach (var node in _cutNodes)
        {
            if (node.visualSegment != null) Destroy(node.visualSegment);
        }
        _cutNodes.Clear();

        if (points.Count < 2) return;

        // 2. Generar nodos y segmentos visuales
        for (int i = 0; i < points.Count; i++)
        {
            if (i == points.Count - 1 && !isClosedLoop) break;

            Transform p1 = points[i];
            Transform p2 = (i == points.Count - 1) ? points[0] : points[i + 1];

            float distance = Vector3.Distance(p1.position, p2.position);
            int segmentsCount = Mathf.Max(1, Mathf.CeilToInt(distance / nodeDensity));

            for (int j = 0; j < segmentsCount; j++)
            {
                float t1 = (float)j / segmentsCount;
                float t2 = (float)(j + 1) / segmentsCount;

                Vector3 posStart = Vector3.Lerp(p1.position, p2.position, t1);
                Vector3 posEnd = Vector3.Lerp(p1.position, p2.position, t2);

                // Crear el nodo lógico
                CutNode newNode = new CutNode(posStart);

                // Crear el segmento visual (El "palito" entre este nodo y el siguiente)
                if (lineSegmentPrefab != null)
                {
                    GameObject segObj = Instantiate(lineSegmentPrefab, transform);
                    segObj.name = $"Segment_{_cutNodes.Count}";

                    LineRenderer lr = segObj.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        lr.useWorldSpace = true;
                        lr.startWidth = lineWidth;
                        lr.endWidth = lineWidth;
                        lr.startColor = _myColor;
                        lr.endColor = _myColor;
                        lr.positionCount = 2;
                        lr.SetPosition(0, posStart);
                        lr.SetPosition(1, posEnd);
                    }
                    newNode.visualSegment = segObj;
                }

                _cutNodes.Add(newNode);
            }
        }

        _totalNodes = _cutNodes.Count;
    }

    // --- LÓGICA DE CORTE "Cualquier Orden" ---
    public bool TryCutNode(Vector3 scissorPos)
    {
        bool cutSomething = false;

        // Revisamos TODOS los nodos. Como son independientes, no importa el orden.
        foreach (var node in _cutNodes)
        {
            if (!node.isCut)
            {
                // Si la tijera toca este punto...
                if (Vector3.Distance(scissorPos, node.worldPosition) <= cutDetectionRadius)
                {
                    node.isCut = true;
                    _cutCount++;
                    cutSomething = true;

                    // Apagamos SU segmento visual específico
                    if (node.visualSegment != null)
                    {
                        node.visualSegment.SetActive(false);
                    }
                }
            }
        }

        return cutSomething;
    }

    public float GetProgress()
    {
        return (float)_cutCount / Mathf.Max(1, _totalNodes);
    }

    public bool IsPositionSafe(Vector3 scissorPosition)
    {
        if (points.Count < 2) return false;
        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            if (i == points.Count - 1 && !isClosedLoop) break;

            Vector3 p1 = points[i].position;
            Vector3 p2 = (i == points.Count - 1) ? points[0].position : points[i + 1].position;

            float d = GetDistanceFromPointToLineSegment(scissorPosition, p1, p2);
            if (d < minDistance) minDistance = d;
        }
        return minDistance <= errorMargin;
    }

    float GetDistanceFromPointToLineSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = point - a;
        float magnitudeAB = ab.sqrMagnitude;
        if (magnitudeAB == 0) return ap.magnitude;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / magnitudeAB);
        return Vector3.Distance(point, a + ab * t);
    }

    private void OnDrawGizmos()
    {
        // Gizmos para ver los nodos en el editor
        if (points == null || points.Count < 2) return;

        // Dibujar el esqueleto base
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Count; i++)
        {
            if (i == points.Count - 1 && !isClosedLoop) break;
            Transform next = (i == points.Count - 1) ? points[0] : points[i + 1];
            if (points[i] != null && next != null)
                Gizmos.DrawLine(points[i].position, next.position);
        }

        // Simulación visual de los nodos (para ajustar densidad)
        // NOTA: Esto es aproximado en editor, en Play Mode es exacto.
        if (!Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            for (int i = 0; i < points.Count; i++)
            {
                if (i == points.Count - 1 && !isClosedLoop) break;
                Transform p1 = points[i];
                Transform p2 = (i == points.Count - 1) ? points[0] : points[i + 1];
                float dist = Vector3.Distance(p1.position, p2.position);
                int count = Mathf.Max(1, Mathf.CeilToInt(dist / nodeDensity));
                for (int j = 0; j < count; j++)
                {
                    float t = (float)j / count;
                    Gizmos.DrawSphere(Vector3.Lerp(p1.position, p2.position, t), 0.05f);
                }
            }
        }
    }
}