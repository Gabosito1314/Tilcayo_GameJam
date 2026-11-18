using System.Collections;
using UnityEngine;

namespace Tilcayo.Player
{
    /// <summary>
    /// Controlador de movimiento y físicas 2D para Tilcayo con Game Feel esencial:
    /// Coyote Time (0.12s), Jump Buffer (0.10s) y Squash & Stretch procedural.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movimiento Horizontal")]
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 70f;

        [Header("Salto")]
        [SerializeField] private float jumpForce = 11.0f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float fallGravityMultiplier = 1.3f;
        [SerializeField] private float coyoteTimeDuration = 0.12f;
        [SerializeField] private float jumpBufferDuration = 0.10f;

        [Header("Detección de Suelo")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.15f);
        [SerializeField] private LayerMask groundLayerMask;

        [Header("Squash & Stretch")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private Vector3 jumpScale = new Vector3(0.8f, 1.25f, 1f);
        [SerializeField] private Vector3 landScale = new Vector3(1.25f, 0.75f, 1f);
        [SerializeField] private float squashStretchSpeed = 12f;

        // Componentes
        private Rigidbody2D rb;
        private Collider2D col;
        private Vector3 initialVisualScale = Vector3.one;

        // Timers & Estados
        private float horizontalInput;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private bool isGrounded;
        private bool wasGrounded;
        private bool isFacingRight = true;
        private bool canMove = true;
        private float defaultGravityScale;
        private Coroutine squashCoroutine;
        private float airTime = 0f;

        // Propiedades públicas para lectura de otros scripts/animaciones
        public bool IsGrounded => isGrounded;
        public float HorizontalInput => horizontalInput;
        public Vector2 CurrentVelocity => rb.linearVelocity;
        public bool IsFacingRight => isFacingRight;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            defaultGravityScale = rb.gravityScale;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (visualTransform == null)
            {
                visualTransform = transform;
            }
            if (Mathf.Approximately(visualTransform.localScale.x, 1f) && Mathf.Approximately(visualTransform.localScale.y, 1f))
            {
                visualTransform.localScale = new Vector3(1.5f, 1.5f, 1f);
            }
            initialVisualScale = visualTransform.localScale;

            // Si groundLayerMask no está configurada, usar por defecto Default + Ground + PassableGrates
            if (groundLayerMask == 0)
            {
                groundLayerMask = LayerMask.GetMask("Default", "Ground", "PassableGrates");
                if (groundLayerMask == 0) groundLayerMask = ~0; // fallback a todo excepto triggers
            }
        }

        private void Update()
        {
            if (!canMove)
            {
                horizontalInput = 0f;
                return;
            }

            // Lectura de entrada
            horizontalInput = PlayerInputReader.GetHorizontal();

            // Orientación del sprite
            if (horizontalInput > 0.05f && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < -0.05f && isFacingRight)
            {
                Flip();
            }

            // Jump Buffer
            if (PlayerInputReader.GetJumpDown())
            {
                jumpBufferCounter = jumpBufferDuration;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // Coyote Time
            if (isGrounded)
            {
                coyoteCounter = coyoteTimeDuration;
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }

            // Ejecución de Salto
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                ExecuteJump();
            }

            // Corte de Salto (variable jump height)
            if (!PlayerInputReader.GetJumpHeld() && rb.linearVelocity.y > 0.1f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                coyoteCounter = 0f;
            }
        }

        private void FixedUpdate()
        {
            CheckGround();
            ApplyHorizontalMovement();
            ApplyGravityModifiers();
        }

        private void CheckGround()
        {
            Vector2 checkPos = groundCheck != null 
                ? (Vector2)groundCheck.position 
                : (Vector2)transform.position + Vector2.down * (col.bounds.extents.y);

            // Leve extensión hacia abajo para asegurar contacto continuo sobre cualquier superficie y evitar parpadeo en las juntas
            Vector2 effectivePos = checkPos + Vector2.down * 0.05f;
            bool groundHit = Physics2D.OverlapBox(effectivePos, groundCheckSize, 0f, groundLayerMask);

            isGrounded = groundHit;

            if (!isGrounded)
            {
                airTime += Time.fixedDeltaTime;
            }
            else
            {
                // Detección de aterrizaje para Squash procedimental: solo si realmente cayó desde el aire (salto o caída prolongada)
                if (!wasGrounded && airTime > 0.12f && rb.linearVelocity.y <= 0.1f)
                {
                    TriggerSquashAndStretch(landScale);
                }
                airTime = 0f;
            }

            wasGrounded = isGrounded;
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float currentSpeed = rb.linearVelocity.x;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float newSpeedX = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }

        private void ApplyGravityModifiers()
        {
            if (rb.linearVelocity.y < -0.1f)
            {
                // Caída más rápida para mejor sensación arcade
                rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
            }
            else
            {
                rb.gravityScale = defaultGravityScale;
            }
        }

        private void ExecuteJump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            TriggerSquashAndStretch(jumpScale);
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            Vector3 s = visualTransform.localScale;
            s.x = Mathf.Abs(s.x) * (isFacingRight ? 1f : -1f);
            visualTransform.localScale = s;
            initialVisualScale.x = Mathf.Abs(initialVisualScale.x) * (isFacingRight ? 1f : -1f);
        }

        public void TriggerSquashAndStretch(Vector3 targetModifier)
        {
            if (squashCoroutine != null) StopCoroutine(squashCoroutine);
            squashCoroutine = StartCoroutine(SquashRoutine(targetModifier));
        }

        private IEnumerator SquashRoutine(Vector3 modifier)
        {
            float signX = isFacingRight ? 1f : -1f;
            Vector3 squashed = new Vector3(
                Mathf.Abs(initialVisualScale.x) * modifier.x * signX,
                initialVisualScale.y * modifier.y,
                initialVisualScale.z
            );
            Vector3 targetOriginal = new Vector3(
                Mathf.Abs(initialVisualScale.x) * signX,
                initialVisualScale.y,
                initialVisualScale.z
            );

            visualTransform.localScale = squashed;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * squashStretchSpeed;
                visualTransform.localScale = Vector3.Lerp(squashed, targetOriginal, t);
                yield return null;
            }

            visualTransform.localScale = targetOriginal;
        }

        public void SetCanMove(bool allowed)
        {
            canMove = allowed;
            if (!allowed)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        public void ResetVelocity()
        {
            rb.linearVelocity = Vector2.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
            Gizmos.DrawWireCube(checkPos + Vector2.down * 0.05f, groundCheckSize);
        }
    }
}
