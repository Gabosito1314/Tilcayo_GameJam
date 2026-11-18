using System.Collections;
using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Player
{
    /// <summary>
    /// Baba de código viviente (Slime TP):
    /// 1. Mientras viaja (hasta 6 de distancia): alterna entre SlimeTP_1 y SlimeTP_2.
    /// 2. Al llegar a su distancia máxima o chocar con pared: reproduce SlimeTP_0, luego SlimeTP_3.
    /// 3. Aparece SlimeTilcayo_1 y se queda esperando hasta que el jugador presiona E para teletransportarse.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CodeOozeCrawler : MonoBehaviour
    {
        [Header("Comportamiento")]
        [SerializeField] private float crawlSpeed = 6.0f;
        [SerializeField] private float maxDistance = 6.0f;
        [SerializeField] private float waitTimeout = 20.0f;
        [SerializeField] private float gravityScale = 2.0f;

        [Header("Detección de Obstáculos")]
        [SerializeField] private LayerMask solidObstacleMask;

        [Header("Sprites de la Secuencia")]
        [SerializeField] private Sprite spriteSlimeTP_1; // Viaje fase 1
        [SerializeField] private Sprite spriteSlimeTP_2; // Viaje fase 2
        [SerializeField] private Sprite spriteSlimeTP_0; // Llegada asentamiento
        [SerializeField] private Sprite spriteSlimeTP_3; // Llegada estallido digital
        [SerializeField] private Sprite spriteSlimeTilcayo_1; // Clon esperando

        [Header("Referencias Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ParticleSystem burstParticles;

        private Rigidbody2D rb;
        private Collider2D col;
        private float facingDirection = 1f;
        private Vector3 startPosition;
        private bool isAtDestination = false;
        private bool isReadyForTeleport = false;
        private bool isExpired = false;
        private Coroutine crawlAnimCoroutine;
        private Coroutine settleCoroutine;

        public bool IsAtDestination => isAtDestination;
        public bool IsReadyForTeleport => isReadyForTeleport;
        public Vector3 TeleportTargetPosition => transform.position;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            rb.gravityScale = gravityScale;

            if (Mathf.Approximately(transform.localScale.x, 1f) && Mathf.Approximately(transform.localScale.y, 1f))
            {
                transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Desactivar Animator para que no sobreescriba los frames de sprite secuenciales
            var anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
            }

            // Layer CloneSlime (ignora rejas y trampas)
            int slimeLayer = LayerMask.NameToLayer("CloneSlime");
            if (slimeLayer != -1)
            {
                gameObject.layer = slimeLayer;
            }

            if (solidObstacleMask == 0)
            {
                solidObstacleMask = LayerMask.GetMask("Ground", "Default");
            }
        }

        public void Initialize(float direction)
        {
            facingDirection = Mathf.Sign(direction);
            startPosition = transform.position;
            isAtDestination = false;
            isReadyForTeleport = false;
            isExpired = false;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facingDirection < 0;
            }

            GameEvents.TriggerSlimeSpawned();

            // Iniciar animación de viaje intercalando SlimeTP_1 y SlimeTP_2
            crawlAnimCoroutine = StartCoroutine(TravelAnimationRoutine());
        }

        private IEnumerator TravelAnimationRoutine()
        {
            int step = 0;
            while (!isAtDestination && !isExpired)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = (step % 2 == 0) ? spriteSlimeTP_1 : spriteSlimeTP_2;
                }
                step++;
                yield return new WaitForSeconds(0.12f);
            }
        }

        private void Update()
        {
            if (isExpired) return;

            // Comprobar distancia máxima recorrida (6 unidades)
            if (!isAtDestination)
            {
                float traveled = Mathf.Abs(transform.position.x - startPosition.x);
                if (traveled >= maxDistance)
                {
                    ArriveAtDestination();
                }
            }
        }

        private void FixedUpdate()
        {
            if (isExpired) return;

            if (!isAtDestination)
            {
                // Avance continuo
                rb.linearVelocity = new Vector2(facingDirection * crawlSpeed, rb.linearVelocity.y);

                // Detectar si choca de frente con una pared vertical sólida (excluyendo rejas pasables y el piso horizontal)
                Vector2 rayOrigin = (Vector2)transform.position + Vector2.up * 0.35f;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * facingDirection, 0.4f, solidObstacleMask);
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
                {
                    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("PassableGrates"))
                    {
                        // Asegurar que sea impacto frontal (pared vertical) y no el suelo horizontal
                        if (hit.fraction > 0.01f && Mathf.Abs(hit.normal.x) > 0.5f)
                        {
                            ArriveAtDestination();
                        }
                    }
                }
            }
            else
            {
                // Firme en el suelo esperando la orden de teletransporte
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isAtDestination || isExpired) return;
            if (collision.gameObject.layer == LayerMask.NameToLayer("PassableGrates")) return;

            // Detectar choque físico lateral con pared sólida
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (Mathf.Abs(contact.normal.x) > 0.6f)
                {
                    ArriveAtDestination();
                    break;
                }
            }
        }

        public void ArriveAtDestination()
        {
            if (isAtDestination) return;
            isAtDestination = true;

            if (crawlAnimCoroutine != null)
            {
                StopCoroutine(crawlAnimCoroutine);
            }

            rb.linearVelocity = Vector2.zero;

            settleCoroutine = StartCoroutine(SettleSequenceRoutine());
        }

        private IEnumerator SettleSequenceRoutine()
        {
            // Secuencia requerida: una vez a máxima distancia -> SlimeTP_0 (0.24s), SlimeTP_3 (0.24s)
            if (spriteRenderer != null && spriteSlimeTP_0 != null)
            {
                spriteRenderer.sprite = spriteSlimeTP_0;
            }
            yield return new WaitForSeconds(0.24f);

            if (spriteRenderer != null && spriteSlimeTP_3 != null)
            {
                spriteRenderer.sprite = spriteSlimeTP_3;
            }
            yield return new WaitForSeconds(0.24f);

            // Aparece SlimeTilcayo_1 y se queda esperando hasta que el jugador presiona E
            if (spriteRenderer != null && spriteSlimeTilcayo_1 != null)
            {
                spriteRenderer.sprite = spriteSlimeTilcayo_1;
            }

            isReadyForTeleport = true;

            // Efecto de pulso mientras espera que el jugador presione E
            float timer = 0f;
            while (timer < waitTimeout && !isExpired)
            {
                if (spriteRenderer != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 6f, 0.3f);
                    spriteRenderer.color = new Color(1f - pulse * 0.2f, 1f, 1f - pulse * 0.2f, 1f);
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (!isExpired)
            {
                Expire();
            }
        }

        public void BurstAndTeleport()
        {
            if (isExpired) return;
            isExpired = true;

            if (crawlAnimCoroutine != null) StopCoroutine(crawlAnimCoroutine);
            if (settleCoroutine != null) StopCoroutine(settleCoroutine);

            if (burstParticles != null)
            {
                burstParticles.transform.parent = null;
                burstParticles.Play();
                Destroy(burstParticles.gameObject, 1.2f);
            }

            GameEvents.TriggerSlimeExpired();
            Destroy(gameObject);
        }

        public void Expire()
        {
            if (isExpired) return;
            isExpired = true;

            if (crawlAnimCoroutine != null) StopCoroutine(crawlAnimCoroutine);
            if (settleCoroutine != null) StopCoroutine(settleCoroutine);

            GameEvents.TriggerSlimeExpired();
            Destroy(gameObject, 0.05f);
        }

        private void OnDestroy()
        {
            if (!isExpired)
            {
                GameEvents.TriggerSlimeExpired();
            }
        }

        public void SetSprites(Sprite tp1, Sprite tp2, Sprite tp0, Sprite tp3, Sprite tilcayo1)
        {
            spriteSlimeTP_1 = tp1;
            spriteSlimeTP_2 = tp2;
            spriteSlimeTP_0 = tp0;
            spriteSlimeTP_3 = tp3;
            spriteSlimeTilcayo_1 = tilcayo1;
        }
    }
}
