using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tilcayo.Player;

namespace Tilcayo.Core
{
    /// <summary>
    /// Administrador del estado general del juego.
    /// Coordina checkpoints, respawn del jugador, transición de escenas y estado de victoria.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Puntos de Inicio & Checkpoint")]
        [SerializeField] private Vector3 currentCheckpoint;
        [SerializeField] private bool hasCheckpoint = false;
        [SerializeField] private float respawnDelay = 1.0f;

        [Header("Estado de Partida")]
        [SerializeField] private int totalDisksCollected = 0;
        [SerializeField] private int currentLevelDisksCollected = 0;
        [SerializeField] private bool isGamePaused = false;
        [SerializeField] private bool isLevelCompleted = false;

        private int disksAtLevelStart = 0;

        public int TotalDisks => totalDisksCollected;
        public int CurrentLevelDisks => currentLevelDisksCollected;
        public bool IsPaused => isGamePaused;
        public Vector3 CurrentCheckpoint => currentCheckpoint;
        public bool HasCheckpoint => hasCheckpoint;
        public static bool ShouldRespawnDazed { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnCheckpointReached += HandleCheckpointReached;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnDiskCollected += HandleDiskCollected;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnRequestLoadLevel += HandleRequestLoadLevel;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckpointReached -= HandleCheckpointReached;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnDiskCollected -= HandleDiskCollected;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnRequestLoadLevel -= HandleRequestLoadLevel;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (PlayerInputReader.GetPauseDown())
            {
                if (UIManager.Instance != null && UIManager.Instance.IsBossGuideActive)
                {
                    return;
                }
                TogglePause();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            hasCheckpoint = false;
            isLevelCompleted = false;
            Time.timeScale = 1f;

            currentLevelDisksCollected = 0;
            if (scene.name == "Scene_MainMenu")
            {
                totalDisksCollected = 0;
                disksAtLevelStart = 0;
            }
            else
            {
                totalDisksCollected = disksAtLevelStart;
            }

            // Encontrar punto de inicio por defecto del jugador en la nueva escena
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                currentCheckpoint = playerObj.transform.position;
                hasCheckpoint = true;
            }

            if (scene.name.StartsWith("Scene_Level"))
            {
                EnsureLevelBoundaries();
            }
        }

        private void EnsureLevelBoundaries()
        {
            if (FindFirstObjectByType<Tilcayo.Environment.LevelBoundaries2D>() == null)
            {
                GameObject bObj = new GameObject("[LevelBoundaries]");
                bObj.AddComponent<Tilcayo.Environment.LevelBoundaries2D>();
            }
        }

        private void HandleCheckpointReached(Vector3 pos)
        {
            currentCheckpoint = pos;
            hasCheckpoint = true;
            Debug.Log("[GameManager] Checkpoint actualizado a: " + pos);
        }

        private void HandlePlayerDied()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            // 1. En el lugar donde muere se reproduce la animación de derrota Defeat_0, Defeat_1, Defeat_2
            yield return new WaitForSeconds(1.0f);

            // 2. Al perder las 3 vidas, se reinicia el nivel completo y Tilcayo reaparece mareado en Defeat_3
            ShouldRespawnDazed = true;
            RestartCurrentLevel();
        }

        private void HandleDiskCollected(int amount)
        {
            totalDisksCollected += amount;
            currentLevelDisksCollected += amount;
        }

        public void OnLevelAdvanced()
        {
            disksAtLevelStart = totalDisksCollected;
            currentLevelDisksCollected = 0;
        }

        private void HandleLevelCompleted()
        {
            isLevelCompleted = true;
            Debug.Log("[GameManager] ¡NIVEL COMPLETADO! ¡Proyecto Entregado a Tiempo!");
        }

        private void HandleRequestLoadLevel(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[GameManager] No se pudo cargar la escena: " + sceneName);
            }
        }

        public void TogglePause()
        {
            isGamePaused = !isGamePaused;
            Time.timeScale = isGamePaused ? 0f : 1f;
        }

        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            isGamePaused = false;
            totalDisksCollected = disksAtLevelStart;
            currentLevelDisksCollected = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            isGamePaused = false;
            totalDisksCollected = 0;
            disksAtLevelStart = 0;
            currentLevelDisksCollected = 0;
            SceneManager.LoadScene("Scene_MainMenu");
        }
    }
}
