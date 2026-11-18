using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Camera
{
    /// <summary>
    /// Cámara 2D suave con amortiguación (Smooth Damping 0.2s),
    /// seguimiento vertical de salto con confinador de límites superior e inferior (Confiner),
    /// y Camera Shake para Game Feel.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Objetivo")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.0f, -10f);

        [Header("Suavizado (Damping 0.2)")]
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Límites del Escenario (Confiner)")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private bool autoDetectBackground = false;

        [Header("Límites Verticales (Salto & Caída)")]
        [SerializeField] private bool clampVertical = true;
        [SerializeField] private float minCameraY = 1.5f;
        [SerializeField] private float maxCameraY = 3.5f;

        [Header("Límites Horizontales")]
        [SerializeField] private bool clampHorizontal = true;
        [SerializeField] private float minCameraX = 0f;
        [SerializeField] private float maxCameraX = 65f;

        private Vector3 currentVelocity = Vector3.zero;
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0.15f;
        private UnityEngine.Camera cam;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = UnityEngine.Camera.main;
        }

        private void OnEnable()
        {
            GameEvents.OnTeleportUsed += HandleTeleportShake;
            GameEvents.OnPlayerDamaged += HandleDamageShake;
        }

        private void OnDisable()
        {
            GameEvents.OnTeleportUsed -= HandleTeleportShake;
            GameEvents.OnPlayerDamaged -= HandleDamageShake;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (autoDetectBackground)
            {
                AutoCalibrateBoundsFromBackground();
            }
        }

        /// <summary>
        /// Detecta automáticamente los fondos en la escena y calcula los límites exactos
        /// de la cámara para que el salto mueva la cámara sin salirse nunca del fondo ni mostrar el color azul.
        /// </summary>
        public void AutoCalibrateBoundsFromBackground()
        {
            var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            bool hasBg = false;
            Bounds totalBgBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var sr in renderers)
            {
                if (sr.sortingLayerName == "Background" || sr.gameObject.name.ToLower().Contains("background") || sr.gameObject.name.ToLower().Contains("stage"))
                {
                    if (!hasBg)
                    {
                        totalBgBounds = sr.bounds;
                        hasBg = true;
                    }
                    else
                    {
                        totalBgBounds.Encapsulate(sr.bounds);
                    }
                }
            }

            if (hasBg && cam != null && cam.orthographic)
            {
                float camHalfH = cam.orthographicSize;
                float camHalfW = camHalfH * cam.aspect;

                // Calibración vertical segura
                if (totalBgBounds.size.y >= camHalfH * 2f)
                {
                    minCameraY = totalBgBounds.min.y + camHalfH;
                    maxCameraY = totalBgBounds.max.y - camHalfH;
                }
                else
                {
                    // Si el fondo es más bajo que el alto del viewport, anclar el límite inferior al fondo
                    // pero permitir que la cámara siga el salto verticalmente
                    minCameraY = totalBgBounds.center.y - 0.5f;
                    maxCameraY = totalBgBounds.center.y + 1.5f;
                }

                // Calibración horizontal segura
                if (totalBgBounds.size.x >= camHalfW * 2f)
                {
                    minCameraX = totalBgBounds.min.x + camHalfW;
                    maxCameraX = totalBgBounds.max.x - camHalfW;
                }
                else
                {
                    minCameraX = totalBgBounds.center.x;
                    maxCameraX = totalBgBounds.center.x;
                }

                useBounds = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Posición objetivo (sigue a Tilcayo tanto en X como en Y al saltar)
            Vector3 desiredPos = target.position + offset;

            // Suavizado con SmoothDamp (amortiguación 0.2s)
            Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);

            // Confinador estricto para no salir del límite ni mostrar la parte azul exterior
            if (useBounds)
            {
                if (clampVertical)
                {
                    smoothedPos.y = Mathf.Clamp(smoothedPos.y, minCameraY, maxCameraY);
                }

                if (clampHorizontal)
                {
                    smoothedPos.x = Mathf.Clamp(smoothedPos.x, minCameraX, maxCameraX);
                }
            }

            // Aplicar Screen Shake si está activo
            if (shakeDuration > 0f)
            {
                shakeDuration -= Time.deltaTime;
                Vector3 shakeOffset = (Vector3)(Random.insideUnitCircle * shakeMagnitude);
                shakeOffset.z = 0f;
                smoothedPos += shakeOffset;
            }

            transform.position = smoothedPos;
        }

        public void TriggerShake(float duration = 0.15f, float magnitude = 0.15f)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
        }

        private void HandleTeleportShake(Vector3 from, Vector3 to) => TriggerShake(0.12f, 0.12f);
        private void HandleDamageShake(int cur, int max) => TriggerShake(0.25f, 0.25f);

        private void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                Gizmos.color = Color.cyan;
                float centerX = (minCameraX + maxCameraX) * 0.5f;
                float centerY = (minCameraY + maxCameraY) * 0.5f;
                Vector3 center = new Vector3(centerX, centerY, 0f);
                Vector3 size = new Vector3(Mathf.Max(0.1f, maxCameraX - minCameraX), Mathf.Max(0.1f, maxCameraY - minCameraY), 0f);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
