using UnityEngine;
using Tilcayo.Player;

namespace Tilcayo.Environment
{
    /// <summary>
    /// Trampa o peligro 2D (sierras, láseres, picos, abismos).
    /// Daña o derrota a Tilcayo al contacto.
    /// </summary>
    public class Hazard2D : MonoBehaviour
    {
        [Header("Daño")]
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private bool isInstantDeath = false;

        [Header("Rotación de Sierras (Opcional)")]
        [SerializeField] private bool rotateContinuously = false;
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Ciclo Láser (Opcional)")]
        [SerializeField] private bool isTimedLaser = false;
        [SerializeField] private float onDuration = 2.0f;
        [SerializeField] private float offDuration = 2.0f;
        [SerializeField] private GameObject laserVisual;
        [SerializeField] private Collider2D laserCollider;

        private float laserTimer;
        private bool isLaserActive = true;

        private void Awake()
        {
            int hazardLayer = LayerMask.NameToLayer("Hazards");
            if (hazardLayer != -1)
            {
                gameObject.layer = hazardLayer;
            }

            if (isTimedLaser && laserCollider == null)
            {
                laserCollider = GetComponent<Collider2D>();
            }
        }

        private void Update()
        {
            if (rotateContinuously)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }

            if (isTimedLaser)
            {
                laserTimer += Time.deltaTime;
                float currentPeriod = isLaserActive ? onDuration : offDuration;
                if (laserTimer >= currentPeriod)
                {
                    laserTimer = 0f;
                    isLaserActive = !isLaserActive;
                    if (laserVisual != null) laserVisual.SetActive(isLaserActive);
                    if (laserCollider != null) laserCollider.enabled = isLaserActive;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandleHit(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.gameObject);
        }

        private void HandleHit(GameObject hitObj)
        {
            if (hitObj.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                int dmg = isInstantDeath ? playerHealth.MaxHealth : damageAmount;
                playerHealth.TakeDamage(dmg, transform.position);
            }
        }
    }
}
