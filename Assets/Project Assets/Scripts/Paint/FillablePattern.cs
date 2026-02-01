using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class FillablePattern : MonoBehaviour
{
    [Header("Configuración de Relleno")]
    [Tooltip("Prefab del punto/pixel que aparecerá al rellenar.")]
    public GameObject fillDotPrefab;

    [Tooltip("Separación entre puntos. Menor valor = Más denso (más costoso).")]
    public float gridDensity = 0.2f;

    [Tooltip("Radio del pincel para rellenar múltiples puntos a la vez.")]
    public float brushRadius = 0.3f;

    // Estado interno
    private class FillNode
    {
        public Vector3 position;
        public bool isFilled;
        public GameObject visualObj; // El objeto visual (sprite)
    }

    private List<FillNode> _nodes = new List<FillNode>();
    private PolygonCollider2D _polyCollider;
    private int _filledCount = 0;
    private int _totalNodes = 0;
    private Color _targetColor;

    private void Awake()
    {
        _polyCollider = GetComponent<PolygonCollider2D>();
    }

    public void InitializeVisuals(Color playerColor)
    {
        _targetColor = playerColor;
        GenerateGrid();
    }

    // Genera puntos dentro del colisionador
    void GenerateGrid()
    {
        // Limpiar previos
        foreach (var node in _nodes) if (node.visualObj) Destroy(node.visualObj);
        _nodes.Clear();

        Bounds bounds = _polyCollider.bounds;

        // Barrido en X e Y sobre el área de la figura
        for (float x = bounds.min.x; x <= bounds.max.x; x += gridDensity)
        {
            for (float y = bounds.min.y; y <= bounds.max.y; y += gridDensity)
            {
                Vector2 point = new Vector2(x, y);

                // Verificamos si el punto cae DENTRO del polígono
                if (_polyCollider.OverlapPoint(point))
                {
                    CreateNode(point);
                }
            }
        }
        _totalNodes = _nodes.Count;
    }

    void CreateNode(Vector2 pos)
    {
        FillNode node = new FillNode();
        node.position = pos;
        node.isFilled = false;

        if (fillDotPrefab != null)
        {
            GameObject dot = Instantiate(fillDotPrefab, transform);
            dot.transform.position = pos;

            // Inicialmente invisible o transparente
            SpriteRenderer sr = dot.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(_targetColor.r, _targetColor.g, _targetColor.b, 0.1f); // Casi transparente
                sr.transform.localScale = Vector3.one * (gridDensity * 0.8f); // Ajuste de tamaño
            }
            node.visualObj = dot;
        }
        _nodes.Add(node);
    }

    // --- LÓGICA DE RELLENO ---

    // Intenta rellenar en la posición del pincel
    public int TryFillArea(Vector3 brushPos)
    {
        int nodesFilledNow = 0;

        foreach (var node in _nodes)
        {
            if (!node.isFilled)
            {
                // Si el nodo está dentro del radio del pincel
                if (Vector3.Distance(brushPos, node.position) <= brushRadius)
                {
                    node.isFilled = true;
                    nodesFilledNow++;

                    // Cambiar visualmente a "Rellenado"
                    if (node.visualObj != null)
                    {
                        SpriteRenderer sr = node.visualObj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.color = _targetColor; // Color sólido
                        }
                    }
                }
            }
        }

        if (nodesFilledNow > 0) _filledCount += nodesFilledNow;
        return nodesFilledNow;
    }

    // Verifica si la posición es segura (dentro de la figura)
    public bool IsPositionSafe(Vector3 position)
    {
        // OverlapPoint devuelve true si el punto está dentro del collider
        return _polyCollider.OverlapPoint((Vector2)position);
    }

    public float GetProgress()
    {
        return _totalNodes == 0 ? 0 : (float)_filledCount / _totalNodes;
    }
}