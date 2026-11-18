using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Player
{
    /// <summary>
    /// Controlador principal de Tilcayo.
    /// Orquesta movimiento, habilidades, salud y alimenta las animaciones del Animator.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement2D), typeof(SlimeTPAbility), typeof(PlayerHealth))]
    public class TilcayoController : MonoBehaviour
    {
        [Header("Animador")]
        [SerializeField] private Animator animator;

        private PlayerMovement2D movement;
        private SlimeTPAbility ability;
        private PlayerHealth health;

        // Hashes de parámetros de Animator para alto rendimiento
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsUsingAbilityHash = Animator.StringToHash("IsUsingAbility");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int IsDazedHash = Animator.StringToHash("IsDazed");
        private static readonly int TriggerTeleportHash = Animator.StringToHash("Teleport");

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            ability = GetComponent<SlimeTPAbility>();
            health = GetComponent<PlayerHealth>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnTeleportUsed += HandleTeleport;
        }

        private void OnDisable()
        {
            GameEvents.OnTeleportUsed -= HandleTeleport;
        }

        private void Start()
        {
            if (health != null && health.IsDazedAtRespawn)
            {
                PlayDazedImmediately();
            }
        }

        public void PlayDazedImmediately()
        {
            if (animator != null)
            {
                animator.SetBool(IsDazedHash, true);
                animator.Play("Dazed", 0, 0f);
                animator.Update(0f);
            }
        }

        private void Update()
        {
            UpdateAnimationParameters();
        }

        private void UpdateAnimationParameters()
        {
            if (animator == null) return;

            float horizontalSpeed = Mathf.Abs(movement.CurrentVelocity.x);
            float verticalSpeed = movement.CurrentVelocity.y;
            bool grounded = movement.IsGrounded;
            bool dead = health.IsDead;
            bool dazed = health.IsDazedAtRespawn;
            bool usingAbility = ability.IsCastingAbility;

            animator.SetFloat(SpeedHash, horizontalSpeed);
            animator.SetFloat(VerticalVelocityHash, verticalSpeed);
            animator.SetBool(IsGroundedHash, grounded);
            animator.SetBool(IsDeadHash, dead);
            animator.SetBool(IsDazedHash, dazed);
            animator.SetBool(IsUsingAbilityHash, usingAbility);
        }

        private void HandleTeleport(Vector3 fromPos, Vector3 toPos)
        {
            if (animator != null)
            {
                animator.SetTrigger(TriggerTeleportHash);
            }
        }
    }
}
