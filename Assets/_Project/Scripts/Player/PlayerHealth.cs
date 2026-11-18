using System.Collections;
using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Player
{
    /// <summary>
    /// Sistema de salud y vidas de Tilcayo:
    /// - 3 vidas por nivel.
    /// - Al perder una vida, el icono correspondiente desaparece (empezando por Life_Botones2_0_0, luego 2_0_1, luego 2_0_2).
    /// - Si cae al vacío (sin plataforma), pierde 1 vida y regresa a un lugar seguro en estado mareado (Defeat_3).
    /// - Si pierde las 3 vidas, muere y el nivel se reinicia por completo.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Configuración de Vidas")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilityDuration = 1.4f;
        [SerializeField] private float flashInterval = 0.1f;

        [Header("Retroceso (Knockback)")]
        [SerializeField] private float knockbackForce = 6f;

        [Header("Límite de Caída al Vacío")]
        [SerializeField] private float fallThresholdY = -4.5f;

        [Header("Referencias Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int currentHealth;
        private bool isInvulnerable;
        private bool isDead;
        private bool isDazedAtRespawn;
        private float dazedInputGraceTimer;
        private bool isRecoveringFromFall;
        private Rigidbody2D rb;
        private PlayerMovement2D movement;
        private SlimeTPAbility ability;
        private Coroutine flashCoroutine;

        private Vector3 spawnPosition;
        private Vector3 lastSafeGroundedPos;
        private bool hasSafeGroundedPos = false;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsDazedAtRespawn => isDazedAtRespawn;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement2D>();
            ability = GetComponent<SlimeTPAbility>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            currentHealth = maxHealth;
        }

        private void Start()
        {
            spawnPosition = transform.position;
            lastSafeGroundedPos = spawnPosition;
            hasSafeGroundedPos = true;
            GameEvents.TriggerPlayerDamaged(currentHealth, maxHealth);

            // Al reaparecer tras perder las 3 vidas, reaparece en Defeat_3 esperando acción
            if (GameManager.ShouldRespawnDazed)
            {
                GameManager.ShouldRespawnDazed = false;
                EnterDazedRespawnState();
            }
        }

        public void TakeDamage(int damage, Vector2 damageSourcePos)
        {
            if (isDead || isInvulnerable || isRecoveringFromFall) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            GameEvents.TriggerPlayerDamaged(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Aplicar knockback en dirección opuesta a la fuente del daño
                if (rb != null)
                {
                    float dirX = transform.position.x > damageSourcePos.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dirX * knockbackForce, knockbackForce * 0.75f);
                }

                StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void HandleFallIntoAbyss()
        {
            if (isDead || isRecoveringFromFall) return;

            // Al caer al vacío, Tilcayo pierde una vida
            currentHealth = Mathf.Max(0, currentHealth - 1);
            GameEvents.TriggerPlayerDamaged(currentHealth, maxHealth);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayHit();
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Regresar a un lugar seguro (último checkpoint o última plataforma segura pisada)
                Vector3 safePos = GetSafeRespawnPosition();
                StartCoroutine(FallRecoveryRoutine(safePos));
            }
        }

        public Vector3 GetSafeRespawnPosition()
        {
            if (GameManager.Instance != null && GameManager.Instance.HasCheckpoint)
            {
                return GameManager.Instance.CurrentCheckpoint;
            }

            if (hasSafeGroundedPos)
            {
                return lastSafeGroundedPos + Vector3.up * 0.2f;
            }

            return spawnPosition;
        }

        private IEnumerator FallRecoveryRoutine(Vector3 safePos)
        {
            isRecoveringFromFall = true;
            isInvulnerable = true;

            if (movement != null) movement.SetCanMove(false);
            if (ability != null) ability.CancelAbility();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            transform.position = safePos;

            // Colocar a Tilcayo mareado (Defeat_3) hasta que realice un movimiento o acción
            EnterDazedRespawnState();

            // Parpadeo de invulnerabilidad para no recibir daño inmediato
            yield return StartCoroutine(InvulnerabilityRoutine());

            isRecoveringFromFall = false;
        }

        public void EnterDazedRespawnState()
        {
            isDazedAtRespawn = true;
            isDead = false;
            dazedInputGraceTimer = 0.25f;

            if (movement != null) movement.SetCanMove(false);
            if (ability != null) ability.CancelAbility();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            var controller = GetComponent<TilcayoController>();
            if (controller != null)
            {
                controller.PlayDazedImmediately();
            }
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float timer = 0f;

            while (timer < invulnerabilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.25f);
                }
                yield return new WaitForSeconds(flashInterval);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
                yield return new WaitForSeconds(flashInterval);

                timer += flashInterval * 2f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            isInvulnerable = false;
        }

        private void Update()
        {
            // Registrar última posición segura pisada en suelo firme
            if (movement != null && movement.IsGrounded && transform.position.y > -2.0f)
            {
                lastSafeGroundedPos = transform.position;
                hasSafeGroundedPos = true;
            }

            // Detección automática de caída al abismo
            if (!isDead && !isRecoveringFromFall && transform.position.y < fallThresholdY)
            {
                HandleFallIntoAbyss();
            }

            if (isDazedAtRespawn)
            {
                if (dazedInputGraceTimer > 0f)
                {
                    dazedInputGraceTimer -= Time.deltaTime;
                }
                else
                {
                    // Salir automáticamente de Defeat_3 en caso de que el jugador se mueva o haga una acción
                    bool hasAction = Mathf.Abs(PlayerInputReader.GetHorizontal()) > 0.15f
                                  || PlayerInputReader.GetJumpDown()
                                  || PlayerInputReader.GetAbilityDown();

                    if (hasAction)
                    {
                        WakeUpFromRespawn();
                    }
                }
            }
        }

        public void WakeUpFromRespawn()
        {
            isDazedAtRespawn = false;
            isDead = false;
            if (movement != null) movement.SetCanMove(true);
        }

        private void Die()
        {
            isDead = true;
            isDazedAtRespawn = false;

            if (movement != null) movement.SetCanMove(false);
            if (ability != null) ability.CancelAbility();

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, 3.5f); // Salto en el lugar donde muere
            }

            GameEvents.TriggerPlayerDied();
        }

        public void StartRespawnSequence(Vector3 respawnPosition)
        {
            StartRespawnAtCheckpoint(respawnPosition);
        }

        public void StartRespawnAtCheckpoint(Vector3 respawnPosition)
        {
            transform.position = respawnPosition;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            EnterDazedRespawnState();
            if (spriteRenderer != null) spriteRenderer.color = Color.white;

            GameEvents.TriggerPlayerDamaged(currentHealth, maxHealth);
        }

        public void CompleteRespawn()
        {
            WakeUpFromRespawn();
        }

        public void Respawn(Vector3 respawnPosition)
        {
            StartRespawnAtCheckpoint(respawnPosition);
        }
    }
}
