using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Environment
{
    /// <summary>
    /// Disquete coleccionable repartido por las pesadillas de sistemas.
    /// Otorga puntuación y feedback de progreso.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollectibleDisk : MonoBehaviour
    {
        [Header("Animación Flotante")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2.5f;

        [Header("Efectos")]
        [SerializeField] private ParticleSystem collectParticles;

        private Vector3 startPosition;
        private bool isCollected;

        private void Start()
        {
            startPosition = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (isCollected) return;

            // Flotación suave procedural
            float yOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = startPosition + new Vector3(0f, yOffset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;

            if (collision.CompareTag("Player") || collision.GetComponent<Player.PlayerMovement2D>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            isCollected = true;

            if (collectParticles != null)
            {
                collectParticles.transform.parent = null;
                collectParticles.Play();
                Destroy(collectParticles.gameObject, 1f);
            }

            // Incrementar contador mediante GameEvents o GameManager
            GameEvents.TriggerDiskCollected(1);
            Destroy(gameObject);
        }
    }
}
