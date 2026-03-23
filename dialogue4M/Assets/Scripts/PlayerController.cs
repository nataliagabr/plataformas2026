using UnityEngine;
using UnityEngine.InputSystem;

// Controlador simples para um jogo roll-a-ball usando o novo Input System.
// Anexe este script ao GameObject da bola (tem um Rigidbody).
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public enum MovementMode
    {
        AddForce,
        SetVelocity
    }

    [Header("Movement")]
    [Tooltip("Multiplicador de velocidade aplicado ao input (ajuste no Inspector)")]
    public float speed = 5f;
    [Tooltip("Modo de aplicação do movimento: AddForce = física mais natural, SetVelocity = controle mais responsivo")]
    public MovementMode movementMode = MovementMode.AddForce;

    [Header("Input (Input System)")]
    [Tooltip("Arraste aqui a Action do tipo Vector2 (ex: 'Move') do seu Input Actions -> Convert to InputActionReference")]
    public InputActionReference moveAction;

    Rigidbody rb;
    Vector2 moveInput = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            // Use callbacks para manter o último valor do input (mais responsivo que polling simples)
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
            moveAction.action.Disable();
        }
    }

    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        // Converter Vector2 (x,y) => Vector3 (x, 0, y)
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);

        if (movement.sqrMagnitude <= 0f)
            return;

        if (movementMode == MovementMode.AddForce)
        {
            rb.AddForce(movement * speed, ForceMode.Force);
        }
        else // SetVelocity
        {
            Vector3 target = movement * speed;
            // Preserve current vertical velocity (gravity/jumps)
            target.y = rb.linearVelocity.y;
            rb.linearVelocity = target;
        }
    }

    // Pequenas ajudas no Inspector
    void Reset()
    {
        speed = 5f;
        movementMode = MovementMode.AddForce;
    }
}

