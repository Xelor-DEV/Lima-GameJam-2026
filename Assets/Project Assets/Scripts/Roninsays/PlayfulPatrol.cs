using UnityEngine;
using DG.Tweening;

public class PlayfulPatrol : MonoBehaviour
{
    [Header("Movimiento (Ida y Vuelta)")]
    public Transform pointA;
    public Transform pointB;
    public float moveDuration = 1.5f;
    public Ease moveEase = Ease.OutBack; // El rebote al llegar

    [Header("Wiggle (Bamboleo Suave)")]
    [Tooltip("Ángulo máximo de rotación hacia los lados")]
    public float wiggleAngle = 10f;
    [Tooltip("Qué tan rápido hace el 'wobble' (independiente del movimiento)")]
    public float wiggleSpeed = 0.5f;

    private SpriteRenderer _spriteRenderer;
    private Sequence _patrolSequence;
    private Tween _wiggleTween;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 1. IMPORTANTE: Matamos cualquier animación previa en este transform
        // para evitar esa sensación de "superposición" o temblor fantasma.
        transform.DOKill();

        // Posición inicial
        if (pointA != null) transform.position = pointA.position;

        StartPatrol();
        StartSmoothWiggle();
    }

    void StartPatrol()
    {
        _patrolSequence = DOTween.Sequence();

        // Tramo A -> B
        _patrolSequence.Append(transform.DOMove(pointB.position, moveDuration).SetEase(moveEase));
        _patrolSequence.AppendCallback(() => FlipSprite(true)); // Girar vista
        _patrolSequence.AppendInterval(0.2f); // Pequeña pausa

        // Tramo B -> A
        _patrolSequence.Append(transform.DOMove(pointA.position, moveDuration).SetEase(moveEase));
        _patrolSequence.AppendCallback(() => FlipSprite(false)); // Girar vista
        _patrolSequence.AppendInterval(0.2f);

        _patrolSequence.SetLoops(-1);
    }

    void StartSmoothWiggle()
    {
        // Reseteamos rotación por si acaso
        transform.rotation = Quaternion.identity;

        // En lugar de Shake (que es ruidoso), usamos una rotación pendular controlada.
        // Giramos hacia -Z y luego hacia +Z infinitamente.

        // Paso 1: Inclinamos un poco al inicio para empezar el ciclo
        transform.localRotation = Quaternion.Euler(0, 0, -wiggleAngle);

        // Paso 2: Animamos hacia el lado opuesto y le decimos que vaya y venga (Yoyo)
        _wiggleTween = transform.DOLocalRotate(new Vector3(0, 0, wiggleAngle), wiggleSpeed)
            .SetEase(Ease.InOutSine) // InOutSine es CLAVE para que se sienta suave y no robótico
            .SetLoops(-1, LoopType.Yoyo);
    }

    void FlipSprite(bool faceLeft)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = faceLeft;
        }
    }

    private void OnDestroy()
    {
        transform.DOKill(); // Limpieza total al destruir
    }
}