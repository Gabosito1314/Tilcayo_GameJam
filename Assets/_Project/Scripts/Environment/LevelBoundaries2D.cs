using UnityEngine;
using Tilcayo.Player;

namespace Tilcayo.Environment
{
    /// <summary>
    /// Delimita los bordes físicos del nivel (muro izquierdo al inicio, muro derecho al final)
    /// y gestiona la zona de caída al vacío para reubicar a Tilcayo en un lugar seguro y restarle una vida.
    /// </summary>
    public class LevelBoundaries2D : MonoBehaviour
    {
        [Header("Referencias a Paneles (Detectadas automáticamente)")]
        [SerializeField] private Transform panelLeft;
        [SerializeField] private Transform panelRight;

        [Header("Muros de Bloqueo")]
        [Tooltip("Posición X calculada del borde izquierdo de Background_Panel_0")]
        [SerializeField] private float leftWallX = -10.0f;
        [Tooltip("Posición X calculada del borde derecho de Background_Panel_2")]
        [SerializeField] private float rightWallX = 65.0f;
        [SerializeField] private float wallHeight = 40.0f;
        [SerializeField] private float wallCenterY = 8.0f;
        [SerializeField] private float wallThickness = 1.2f;

        [Header("Detección de Caída")]
        [Tooltip("Altura Y por debajo de la cual se considera caída al vacío")]
        [SerializeField] private float fallThresholdY = -4.5f;

        private void Awake()
        {
            EnsureBoundaries();
        }

        private void Start()
        {
            EnsureBoundaries();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureBoundaries();
        }
#endif

        public void EnsureBoundaries()
        {
            UpdatePanelBounds();

            // 1. Muro izquierdo: cubre el borde izquierdo de Background_Panel_0
            var leftWall = transform.Find("Boundary_Left_Wall");
            if (leftWall == null)
            {
                var go = new GameObject("Boundary_Left_Wall");
                go.transform.SetParent(transform, false);
                leftWall = go.transform;
            }
            leftWall.position = new Vector3(leftWallX, wallCenterY, 0f);
            leftWall.gameObject.layer = LayerMask.NameToLayer("Ground");
            if (leftWall.gameObject.layer == -1) leftWall.gameObject.layer = 0;
            var colLeft = leftWall.GetComponent<BoxCollider2D>();
            if (colLeft == null) colLeft = leftWall.gameObject.AddComponent<BoxCollider2D>();
            colLeft.size = new Vector2(wallThickness, wallHeight);

            // 2. Muro derecho: cubre el borde derecho de Background_Panel_2
            var rightWall = transform.Find("Boundary_Right_Wall");
            if (rightWall == null)
            {
                var go = new GameObject("Boundary_Right_Wall");
                go.transform.SetParent(transform, false);
                rightWall = go.transform;
            }
            rightWall.position = new Vector3(rightWallX, wallCenterY, 0f);
            rightWall.gameObject.layer = LayerMask.NameToLayer("Ground");
            if (rightWall.gameObject.layer == -1) rightWall.gameObject.layer = 0;
            var colRight = rightWall.GetComponent<BoxCollider2D>();
            if (colRight == null) colRight = rightWall.gameObject.AddComponent<BoxCollider2D>();
            colRight.size = new Vector2(wallThickness, wallHeight);

            // 3. Zona de caída (Trigger horizontal a lo largo del vacío)
            var fallZone = transform.Find("Boundary_Fall_Zone");
            if (fallZone == null)
            {
                var go = new GameObject("Boundary_Fall_Zone");
                go.transform.SetParent(transform, false);
                fallZone = go.transform;
            }
            fallZone.position = new Vector3((leftWallX + rightWallX) * 0.5f, fallThresholdY - 1.5f, 0f);
            var colFall = fallZone.GetComponent<BoxCollider2D>();
            if (colFall == null) colFall = fallZone.gameObject.AddComponent<BoxCollider2D>();
            colFall.isTrigger = true;
            colFall.size = new Vector2(Mathf.Abs(rightWallX - leftWallX) + 40f, 4f);
            var trigger = fallZone.GetComponent<LevelBoundariesFallTrigger>();
            if (trigger == null) trigger = fallZone.gameObject.AddComponent<LevelBoundariesFallTrigger>();
            trigger.Init(this);
        }

        private void UpdatePanelBounds()
        {
            if (panelLeft == null)
            {
                var p0 = GameObject.Find("Background_Panel_0");
                if (p0 != null) panelLeft = p0.transform;
            }

            if (panelRight == null)
            {
                var p2 = GameObject.Find("Background_Panel_2");
                if (p2 != null) panelRight = p2.transform;
            }

            // Fallback: buscar los paneles de fondo por nombre si cambiaron de jerarquía
            if (panelLeft == null || panelRight == null)
            {
                var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                SpriteRenderer minSr = null;
                SpriteRenderer maxSr = null;
                float minX = float.MaxValue;
                float maxX = float.MinValue;

                foreach (var sr in renderers)
                {
                    if (sr != null && sr.gameObject.name.Contains("Background_Panel"))
                    {
                        if (sr.bounds.min.x < minX)
                        {
                            minX = sr.bounds.min.x;
                            minSr = sr;
                        }
                        if (sr.bounds.max.x > maxX)
                        {
                            maxX = sr.bounds.max.x;
                            maxSr = sr;
                        }
                    }
                }

                if (panelLeft == null && minSr != null) panelLeft = minSr.transform;
                if (panelRight == null && maxSr != null) panelRight = maxSr.transform;
            }

            // Calcular las coordenadas exactas de los bordes según los sprites renderizados
            if (panelLeft != null)
            {
                var sr0 = panelLeft.GetComponent<SpriteRenderer>();
                if (sr0 != null)
                {
                    leftWallX = sr0.bounds.min.x;
                    wallCenterY = sr0.bounds.center.y + 5f;
                }
                else
                {
                    leftWallX = panelLeft.position.x;
                }
            }

            if (panelRight != null)
            {
                var sr2 = panelRight.GetComponent<SpriteRenderer>();
                if (sr2 != null)
                {
                    rightWallX = sr2.bounds.max.x;
                }
                else
                {
                    rightWallX = panelRight.position.x;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            CheckAndHandleFall(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndHandleFall(collision.gameObject);
        }

        public void CheckAndHandleFall(GameObject obj)
        {
            var health = obj.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.HandleFallIntoAbyss();
            }
        }

        private void OnDrawGizmos()
        {
            UpdatePanelBounds();
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            // Muro izquierdo cubriendo el borde de Background_Panel_0
            Gizmos.DrawWireCube(new Vector3(leftWallX, wallCenterY, 0f), new Vector3(wallThickness, wallHeight, 0.1f));
            // Muro derecho cubriendo el borde de Background_Panel_2
            Gizmos.DrawWireCube(new Vector3(rightWallX, wallCenterY, 0f), new Vector3(wallThickness, wallHeight, 0.1f));
            // Zona de caída al abismo
            Gizmos.color = new Color(0.9f, 0.1f, 0.1f, 0.35f);
            Gizmos.DrawWireCube(new Vector3((leftWallX + rightWallX) * 0.5f, fallThresholdY - 1.5f, 0f), new Vector3(Mathf.Abs(rightWallX - leftWallX) + 40f, 4f, 0.1f));
        }
    }

    public class LevelBoundariesFallTrigger : MonoBehaviour
    {
        private LevelBoundaries2D boundaries;

        public void Init(LevelBoundaries2D parent)
        {
            boundaries = parent;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (boundaries != null)
            {
                boundaries.CheckAndHandleFall(collision.gameObject);
            }
        }
    }
}
