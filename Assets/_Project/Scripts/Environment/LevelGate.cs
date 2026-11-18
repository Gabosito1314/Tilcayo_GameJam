using UnityEngine;
using UnityEngine.SceneManagement;
using Tilcayo.Core;

namespace Tilcayo.Environment
{
    /// <summary>
    /// Portón de salida o puerta de red para avanzar de nivel o finalizar la partida.
    /// En los niveles 1 y 2 requiere recoger los 3 disquetes; en el nivel 3 requiere derrotar al jefe.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGate : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private string nextSceneName = "Scene_Level_02";
        [SerializeField] private bool requiresBossDefeat = false;
        [SerializeField] private bool isFinalGameExit = false;
        [SerializeField] private int requiredDisks = 3;

        [Header("Feedback Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool isBossDefeated;
        private bool isTriggered;
        private Coroutine flashRoutine;

        public int RequiredDisks
        {
            get => requiredDisks;
            set => requiredDisks = value;
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (!requiresBossDefeat)
            {
                isBossDefeated = true;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.OnBossDefeated -= HandleBossDefeated;
        }

        private void HandleBossDefeated()
        {
            isBossDefeated = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isTriggered) return;

            if (collision.CompareTag("Player") || collision.GetComponent<Player.PlayerMovement2D>() != null)
            {
                if (requiresBossDefeat && !isBossDefeated)
                {
                    Debug.Log("¡El portón está bloqueado por el Kernel Boss!");
                    FlashGateLocked();
                    return;
                }

                // En niveles normales (Nivel 1 y 2), se deben recoger los 3 disquetes para pasar
                if (!requiresBossDefeat && requiredDisks > 0)
                {
                    int disks = GameManager.Instance != null ? GameManager.Instance.CurrentLevelDisks : 0;
                    if (disks < requiredDisks)
                    {
                        Debug.Log($"[LevelGate] ¡Portón bloqueado! Se requieren {requiredDisks} disquetes (tienes {disks}).");
                        FlashGateLocked();
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.FlashDiskCounter();
                        }
                        return;
                    }
                }

                isTriggered = true;
                if (isFinalGameExit)
                {
                    GameEvents.TriggerLevelCompleted();
                }
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnLevelAdvanced();
                    }
                    GameEvents.TriggerRequestLoadLevel(nextSceneName);
                }
            }
        }

        private void FlashGateLocked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayHit();
            }

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashLockedRoutine());
        }

        private System.Collections.IEnumerator FlashLockedRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
                yield return new WaitForSeconds(0.25f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
    }
}
