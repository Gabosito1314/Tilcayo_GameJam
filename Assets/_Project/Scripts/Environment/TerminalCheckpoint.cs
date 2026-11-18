using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Environment
{
    /// <summary>
    /// Terminal de PC de laboratorio que actúa como Checkpoint.
    /// Al activarse por Tilcayo, cambia de apariencia y registra la posición de respawn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TerminalCheckpoint : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private SpriteRenderer screenRenderer;
        [SerializeField] private Sprite activeScreenSprite;
        [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private ParticleSystem activationParticles;

        private bool isActivated;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isActivated) return;

            if (collision.CompareTag("Player") || collision.GetComponent<Player.PlayerMovement2D>() != null)
            {
                ActivateCheckpoint();
            }
        }

        private void ActivateCheckpoint()
        {
            isActivated = true;
            Vector3 respawnPos = spawnAnchor != null ? spawnAnchor.position : transform.position;

            if (screenRenderer != null)
            {
                if (activeScreenSprite != null) screenRenderer.sprite = activeScreenSprite;
                screenRenderer.color = activeColor;
            }

            if (activationParticles != null)
            {
                activationParticles.Play();
            }

            GameEvents.TriggerCheckpointReached(respawnPos);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isActivated ? Color.green : Color.yellow;
            Vector3 p = spawnAnchor != null ? spawnAnchor.position : transform.position;
            Gizmos.DrawWireSphere(p, 0.4f);
        }
    }
}
