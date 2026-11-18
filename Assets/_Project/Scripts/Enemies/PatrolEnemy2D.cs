using System.Collections;
using UnityEngine;
using Tilcayo.Core;
using Tilcayo.Player;

namespace Tilcayo.Enemies
{
    public enum BossPhase
    {
        Vulnerable,
        Dangerous
    }

    /// <summary>
    /// Enemigo patrullero de vaivén reutilizable para Calavery Virtual y CalabazaFinalBoss.
    /// Soporta movimiento por distancia/puntos, detección de paredes y lógica de jefe opcional.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PatrolEnemy2D : MonoBehaviour
    {
        [Header("Patrulla")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private float patrolDistance = 4.0f;
        [SerializeField] private bool useFixedWaypoints = false;
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;

        [Header("Combate & Daño")]
        [SerializeField] private int damageToPlayer = 1;
        [SerializeField] private bool isBoss = false;
        [SerializeField] private int maxHealth = 3;

        [Header("Fases de Jefe (BugOLantern)")]
        [SerializeField] private float vulnerableDuration = 3.0f;
        [SerializeField] private float dangerousDuration = 6.0f;
        [SerializeField] private float blinkInterval = 0.12f;

        [Header("Referencias Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        private Rigidbody2D rb;
        private Collider2D col;
        private Vector3 startPos;
        private float leftLimit;
        private float rightLimit;
        private int movingDirection = 1; // 1 = derecha, -1 = izquierda
        private int currentHealth;
        private bool isDead;
        private BossPhase currentPhase = BossPhase.Vulnerable;
        private bool hasTakenHitThisPhase = false;
        private Coroutine bossPhaseCoroutine;

        public bool IsBoss => isBoss;
        public int CurrentHealth => currentHealth;
        public BossPhase CurrentPhase => currentPhase;
        public bool HasTakenHitThisPhase => hasTakenHitThisPhase;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) gameObject.layer = enemyLayer;

            currentHealth = isBoss ? maxHealth : 1;
        }

        private void Start()
        {
            startPos = transform.position;

            if (useFixedWaypoints && leftPoint != null && rightPoint != null)
            {
                leftLimit = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
                rightLimit = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
            }
            else
            {
                leftLimit = startPos.x - patrolDistance * 0.5f;
                rightLimit = startPos.x + patrolDistance * 0.5f;
            }

            if (isBoss)
            {
                bossPhaseCoroutine = StartCoroutine(BossPhaseRoutine());
            }
        }

        private void OnDisable()
        {
            if (bossPhaseCoroutine != null)
            {
                StopCoroutine(bossPhaseCoroutine);
                bossPhaseCoroutine = null;
            }
        }

        private IEnumerator BossPhaseRoutine()
        {
            while (!isDead)
            {
                // FASE 1: VULNERABLE (3 segundos)
                // Tilcayo puede atacarlo saltando sobre su cabeza (mecánica existente).
                // Solo puede recibir 1 de daño por ciclo de vulnerabilidad.
                currentPhase = BossPhase.Vulnerable;
                hasTakenHitThisPhase = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }

                yield return new WaitForSeconds(vulnerableDuration);

                if (isDead) yield break;

                // FASE 2: PELIGROSA / PARPADEO (6 segundos)
                // El jefe parpadea y cualquier contacto hace daño al jugador
                currentPhase = BossPhase.Dangerous;
                float elapsed = 0f;
                bool blinkToggle = false;

                while (elapsed < dangerousDuration && !isDead)
                {
                    blinkToggle = !blinkToggle;
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = blinkToggle 
                            ? new Color(1f, 0.3f, 0.3f, 0.35f) 
                            : new Color(1f, 0.75f, 0.75f, 1f);
                    }

                    yield return new WaitForSeconds(blinkInterval);
                    elapsed += blinkInterval;
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            // Movimiento horizontal
            rb.linearVelocity = new Vector2(movingDirection * moveSpeed, rb.linearVelocity.y);

            // Verificación de límites
            if (transform.position.x >= rightLimit && movingDirection > 0)
            {
                ChangeDirection(-1);
            }
            else if (transform.position.x <= leftLimit && movingDirection < 0)
            {
                ChangeDirection(1);
            }
        }

        private void ChangeDirection(int newDir)
        {
            movingDirection = newDir;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = movingDirection < 0;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandlePlayerContact(collision.gameObject);
        }

        private void HandlePlayerContact(GameObject target)
        {
            if (isDead) return;

            if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                // En Fase 2 (parpadeo peligroso), cualquier contacto hace daño al jugador
                if (isBoss && currentPhase == BossPhase.Dangerous)
                {
                    playerHealth.TakeDamage(damageToPlayer, transform.position);
                    return;
                }

                // Fase vulnerable: si el jugador cae sobre la cabeza del enemigo, dañar al enemigo (mecánica pisotón clásico)
                if (target.transform.position.y > transform.position.y + 0.4f)
                {
                    if (isBoss)
                    {
                        if (!hasTakenHitThisPhase)
                        {
                            hasTakenHitThisPhase = true;
                            TakeHit(1);
                        }
                    }
                    else
                    {
                        TakeHit(1);
                    }

                    if (target.TryGetComponent<Rigidbody2D>(out var playerRb))
                    {
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 8f); // rebote
                    }
                }
                else
                {
                    playerHealth.TakeDamage(damageToPlayer, transform.position);
                }
            }
        }

        public void TakeHit(int damage)
        {
            if (isDead) return;
            if (isBoss && currentPhase != BossPhase.Vulnerable) return;

            currentHealth -= damage;

            if (isBoss)
            {
                hasTakenHitThisPhase = true;
                GameEvents.TriggerBossDamaged(currentHealth, maxHealth);
            }

            StartCoroutine(FlashRoutine());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.12f);
                if (!isDead && (!isBoss || currentPhase == BossPhase.Vulnerable))
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }

        private void Die()
        {
            isDead = true;
            if (bossPhaseCoroutine != null)
            {
                StopCoroutine(bossPhaseCoroutine);
                bossPhaseCoroutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            rb.linearVelocity = Vector2.zero;
            col.enabled = false;

            if (isBoss)
            {
                GameEvents.TriggerBossDefeated();
            }

            if (animator != null)
            {
                animator.SetTrigger("Defeat");
            }

            Destroy(gameObject, 0.6f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 center = Application.isPlaying ? startPos : transform.position;
            Vector3 left = new Vector3(center.x - patrolDistance * 0.5f, center.y, center.z);
            Vector3 right = new Vector3(center.x + patrolDistance * 0.5f, center.y, center.z);
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left, 0.2f);
            Gizmos.DrawWireSphere(right, 0.2f);
        }
    }
}
