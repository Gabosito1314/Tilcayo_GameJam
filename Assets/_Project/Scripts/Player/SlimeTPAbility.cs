using System.Collections;
using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Player
{
    /// <summary>
    /// Gestiona la mecánica insignia de Tilcayo:
    /// Pulsación 1: Expulsa la baba de código.
    /// Pulsación 2: Teletransporte seguro instantáneo con validación Anti-Stuck OverlapBox.
    /// Cooldown: 1.2s post-TP o post-expiración.
    /// </summary>
    public class SlimeTPAbility : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private GameObject slimePrefab;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private PlayerMovement2D movement;

        [Header("Tiempos & Cooldown")]
        [SerializeField] private float cooldownDuration = 1.2f;
        [SerializeField] private float castFreezeDuration = 0.12f;

        [Header("Anti-Stuck Safe Teleport")]
        [SerializeField] private Vector2 playerBoundsCheck = new Vector2(0.8f, 0.9f);
        [SerializeField] private LayerMask obstacleLayerMask;

        public static SlimeTPAbility Instance { get; private set; }

        // Estado interno
        private CodeOozeCrawler activeSlime;
        private float cooldownTimer;
        private bool isCasting;
        private bool isUsingAbilityAnimation;

        public bool IsSlimeActive => activeSlime != null;
        public bool IsCastingAbility => isCasting || isUsingAbilityAnimation;
        public bool IsOnCooldown => cooldownTimer > 0f;
        public float CooldownProgress => Mathf.Clamp01(1f - (cooldownTimer / cooldownDuration));

        private void Awake()
        {
            Instance = this;
            if (movement == null) movement = GetComponent<PlayerMovement2D>();

            if (obstacleLayerMask == 0)
            {
                obstacleLayerMask = LayerMask.GetMask("Default", "Ground");
                if (obstacleLayerMask == 0) obstacleLayerMask = ~0;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnSlimeExpired += HandleSlimeExpired;
        }

        private void OnDisable()
        {
            GameEvents.OnSlimeExpired -= HandleSlimeExpired;
        }

        private void Update()
        {
            // Gestión de enfriamiento
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                float progress = CooldownProgress;
                GameEvents.TriggerCooldownUpdate(progress);
            }

            // Lectura de tecla de habilidad
            if (PlayerInputReader.GetAbilityDown() && !isCasting)
            {
                if (activeSlime == null)
                {
                    // Intento de soltar baba
                    if (cooldownTimer <= 0f)
                    {
                        StartCoroutine(DropSlimeRoutine());
                    }
                }
                else
                {
                    // Si el slime está activo pero aún viajando, presionar E frena el viaje inmediatamente
                    if (!activeSlime.IsAtDestination)
                    {
                        activeSlime.ArriveAtDestination();
                    }
                    else if (activeSlime.IsReadyForTeleport)
                    {
                        // Si ya está asentado como SlimeTilcayo_1, ejecutar teletransporte
                        AttemptTeleport();
                    }
                }
            }
        }

        private IEnumerator DropSlimeRoutine()
        {
            isCasting = true;
            isUsingAbilityAnimation = true;

            // Breve pausa para la pose Hability (Hability_1 -> Hability_2)
            if (movement != null)
            {
                movement.SetCanMove(false);
            }

            // Esperar transición a pose Hability_2 (~0.18s)
            yield return new WaitForSeconds(0.18f);

            // Una vez que está en Hability_2 entra SlimeTP
            Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position + Vector3.down * 0.2f;
            float dir = (movement != null && !movement.IsFacingRight) ? -1f : 1f;

            if (slimePrefab != null)
            {
                GameObject slimeObj = Instantiate(slimePrefab, spawnPos, Quaternion.identity);
                activeSlime = slimeObj.GetComponent<CodeOozeCrawler>();
                if (activeSlime != null)
                {
                    activeSlime.Initialize(dir);
                }
            }

            yield return new WaitForSeconds(0.12f);

            isUsingAbilityAnimation = false;
            if (movement != null)
            {
                movement.SetCanMove(true);
            }

            isCasting = false;
        }

        public bool CanTeleportNow()
        {
            if (activeSlime == null) return false;
            if (!activeSlime.IsReadyForTeleport) return false;

            Vector3 targetPos = activeSlime.TeleportTargetPosition;
            Vector2 checkCenter = (Vector2)targetPos + Vector2.up * 0.9f;
            Vector2 checkSize = new Vector2(0.5f, 1.0f);
            Collider2D[] stuckCols = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, obstacleLayerMask);
            foreach (var hitCol in stuckCols)
            {
                if (hitCol == null || hitCol.isTrigger || hitCol.gameObject == gameObject) continue;
                if (hitCol.gameObject.layer == LayerMask.NameToLayer("PassableGrates")) continue;
                if (hitCol.bounds.max.y <= targetPos.y + 0.3f) continue;
                return false;
            }
            return true;
        }

        public void TriggerTeleportFromUI()
        {
            if (CanTeleportNow())
            {
                AttemptTeleport();
            }
        }

        private void AttemptTeleport()
        {
            if (!CanTeleportNow())
            {
                Debug.LogWarning("[Anti-Stuck] Teletransporte bloqueado o destino inaccesible.");
                return;
            }

            Vector3 targetPos = activeSlime.TeleportTargetPosition;

            // Teletransporte exitoso
            Vector3 fromPos = transform.position;
            transform.position = targetPos;

            // Resetear inercia para control preciso post-TP
            if (movement != null)
            {
                movement.ResetVelocity();
                movement.TriggerSquashAndStretch(new Vector3(1.2f, 0.8f, 1f));
            }

            // Notificar evento
            GameEvents.TriggerTeleportUsed(fromPos, targetPos);

            // Destruir baba con efecto y comenzar enfriamiento
            if (activeSlime != null)
            {
                activeSlime.BurstAndTeleport();
                activeSlime = null;
            }

            StartCooldown();
        }

        private void HandleSlimeExpired()
        {
            if (activeSlime != null)
            {
                activeSlime = null;
                StartCooldown();
            }
        }

        private void StartCooldown()
        {
            cooldownTimer = cooldownDuration;
            GameEvents.TriggerCooldownUpdate(0f);
        }

        public void CancelAbility()
        {
            if (activeSlime != null)
            {
                Destroy(activeSlime.gameObject);
                activeSlime = null;
            }
            isCasting = false;
            StartCooldown();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, playerBoundsCheck);
        }
    }
}
