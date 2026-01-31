using UnityEngine;
using UnityEngine.InputSystem;

public class test : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Vector2 moveInput;

    // Referencia al componente de física
    private Rigidbody2D rb;

    private void Awake()
    {
        // Inicializamos la referencia
        rb = GetComponent<Rigidbody2D>();
    }

    public void FixedUpdate()
    {
        // Aplicamos el movimiento directamente a la velocidad del Rigidbody
        // Esto garantiza un movimiento fluido y con colisiones correctas
        rb.linearVelocity = new Vector2(moveInput.x * speed, moveInput.y * speed);
    }

    public void GetMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
