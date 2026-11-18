using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Tilcayo.Player;

namespace Tilcayo.Core
{
    /// <summary>
    /// Gestor de la interfaz de usuario:
    /// - Barra superior izquierda: Pausa (Botones 1_5), Reinicio (Botones 1_7), Silencio (Botones 1_8),
    ///   Habilidad disponible (Botones 2_3), Botón TP E (Botones 1_6), 3 Vidas (Botones 2_0).
    /// - Control del cursor del ratón (desbloqueado en pausa, oculto en gameplay).
    /// - Panel de victoria con el banner limpio (Botones 2_8).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Botones de Control (Top-Left)")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseButtonImage;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteButtonImage;
        [SerializeField] private Button exitMenuButton;

        [Header("Indicadores de Habilidad")]
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image abilityCooldownFill;
        [SerializeField] private Button tpPromptButton;

        [Header("HUD - Vidas (Tilcayo)")]
        [SerializeField] private Image[] catHealthIcons;
        [SerializeField] private Sprite activeCatSprite;
        [SerializeField] private Sprite inactiveCatSprite;

        [Header("HUD - Disquetes")]
        [SerializeField] private Text diskCounterText;

        [Header("Paneles")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button victoryMainMenuButton;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Guía de Jefe (Nivel 3)")]
        [SerializeField] private Sprite bossGuideSprite;

        private bool wasPausedLastFrame = false;
        private bool isBossGuideActive = false;

        public bool IsBossGuideActive => isBossGuideActive;

        private void Awake()
        {
            Instance = this;
#if UNITY_EDITOR
            if (bossGuideSprite == null)
            {
                bossGuideSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Comic/Guía del jefe calabaza cibernético.png");
            }
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamaged += UpdateHealthUI;
            GameEvents.OnPlayerHealed += UpdateHealthOnHeal;
            GameEvents.OnCooldownUpdate += UpdateCooldownUI;
            GameEvents.OnDiskCollected += UpdateDiskCounterUI;
            GameEvents.OnLevelCompleted += ShowVictoryPanel;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamaged -= UpdateHealthUI;
            GameEvents.OnPlayerHealed -= UpdateHealthOnHeal;
            GameEvents.OnCooldownUpdate -= UpdateCooldownUI;
            GameEvents.OnDiskCollected -= UpdateDiskCounterUI;
            GameEvents.OnLevelCompleted -= ShowVictoryPanel;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            // Conectar botones
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonClicked);

            if (muteButton != null)
                muteButton.onClick.AddListener(OnMuteButtonClicked);

            if (tpPromptButton != null)
            {
                tpPromptButton.onClick.AddListener(OnTeleportPromptClicked);
                tpPromptButton.gameObject.SetActive(false);
            }

            HookupVictoryButton();
            HookupExitMenuButton();
            if (exitMenuButton != null)
            {
                exitMenuButton.gameObject.SetActive(false);
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Estado inicial del ratón: oculto durante la partida hasta pausar con ESC
            UpdateCursorState(false);
            UpdateMuteVisuals();
            UpdateCooldownUI(1f);
            UpdateHealthUI(3, 3);
            UpdateDiskCounterUI(0);

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Scene_Level_03")
            {
                StartCoroutine(ShowBossGuideRoutine());
            }
        }

        private void Update()
        {
            if (isBossGuideActive) return;

            bool isPaused = GameManager.Instance != null && GameManager.Instance.IsPaused;

            // Detección de cambio en estado de pausa (por ESC o botón)
            if (isPaused != wasPausedLastFrame)
            {
                wasPausedLastFrame = isPaused;
                UpdateCursorState(isPaused);

                if (pausePanel != null)
                {
                    pausePanel.SetActive(isPaused);
                }

                if (pauseButtonImage != null)
                {
                    pauseButtonImage.color = isPaused ? new Color(1f, 0.8f, 0.3f, 1f) : Color.white;
                }
            }

            // Visibilidad del botón Salir a Menú: solo se ve cuando se oprime ESC / en pausa
            if (exitMenuButton == null)
            {
                HookupExitMenuButton();
            }
            if (exitMenuButton != null && exitMenuButton.gameObject.activeSelf != isPaused)
            {
                exitMenuButton.gameObject.SetActive(isPaused);
            }

            // Actualizar visibilidad del botón de TP (Botones 1_6)
            // Solo aparece si se puede teletransportar (E activo, destino seguro)
            if (tpPromptButton != null)
            {
                bool canTP = SlimeTPAbility.Instance != null && SlimeTPAbility.Instance.CanTeleportNow();
                if (tpPromptButton.gameObject.activeSelf != canTP)
                {
                    tpPromptButton.gameObject.SetActive(canTP);
                }
            }

            // Actualizar estado de habilidad (Botones 2_3)
            if (abilityIcon != null)
            {
                bool ready = SlimeTPAbility.Instance == null || 
                             (!SlimeTPAbility.Instance.IsOnCooldown && !SlimeTPAbility.Instance.IsCastingAbility);
                abilityIcon.color = ready ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.65f);
            }
        }

        private void UpdateCursorState(bool isPaused)
        {
            Cursor.visible = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Confined;
        }

        public void OnPauseButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        public void OnRestartButtonClicked()
        {
            UpdateCursorState(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartCurrentLevel();
            }
        }

        public void OnMuteButtonClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
                UpdateMuteVisuals();
            }
        }

        public void OnTeleportPromptClicked()
        {
            if (SlimeTPAbility.Instance != null)
            {
                SlimeTPAbility.Instance.TriggerTeleportFromUI();
            }
        }

        private void UpdateMuteVisuals()
        {
            if (muteButtonImage != null && SoundManager.Instance != null)
            {
                muteButtonImage.color = SoundManager.Instance.IsMuted 
                    ? new Color(0.9f, 0.25f, 0.25f, 0.85f) 
                    : Color.white;
            }
        }

        private void UpdateHealthUI(int currentHealth, int maxHealth)
        {
            EnsureCatHealthIcons();
            if (catHealthIcons == null || catHealthIcons.Length == 0) return;

            // Orden pedido por el usuario:
            // - Si pierde 1 vida: desaparece Life_Botones2_0_0 (índice 0)
            // - Si pierde 2 vidas: desaparece Life_Botones2_0_1 (índice 1)
            // - Si pierde 3 vidas: desaparece Life_Botones2_0_2 (índice 2)
            int livesLost = Mathf.Clamp(maxHealth - currentHealth, 0, catHealthIcons.Length);

            for (int i = 0; i < catHealthIcons.Length; i++)
            {
                if (catHealthIcons[i] == null) continue;

                // El ícono desaparece directamente (SetActive = false)
                bool isAlive = i >= livesLost;
                catHealthIcons[i].gameObject.SetActive(isAlive);
            }
        }

        private void EnsureCatHealthIcons()
        {
            if (catHealthIcons == null || catHealthIcons.Length < 3 || catHealthIcons[0] == null)
            {
                var bar = transform.Find("HUD_TopLeft_Bar");
                if (bar != null)
                {
                    Image i0 = bar.Find("Life_Botones2_0_0")?.GetComponent<Image>();
                    Image i1 = bar.Find("Life_Botones2_0_1")?.GetComponent<Image>();
                    Image i2 = bar.Find("Life_Botones2_0_2")?.GetComponent<Image>();

                    if (i0 == null) i0 = bar.Find("Life_Botones2_0_0 (2)")?.GetComponent<Image>();
                    if (i1 == null) i1 = bar.Find("Life_Botones2_0_0 (1)")?.GetComponent<Image>();

                    if (i0 != null && i1 != null && i2 != null)
                    {
                        catHealthIcons = new Image[] { i0, i1, i2 };
                    }
                }
            }
        }

        private void UpdateHealthOnHeal(int currentHealth)
        {
            UpdateHealthUI(currentHealth, 3);
        }

        private void UpdateCooldownUI(float percent)
        {
            if (abilityCooldownFill != null)
            {
                abilityCooldownFill.fillAmount = percent;
            }
        }

        private void UpdateDiskCounterUI(int amount)
        {
            if (diskCounterText != null && GameManager.Instance != null)
            {
                diskCounterText.text = GameManager.Instance.CurrentLevelDisks.ToString("D2");
            }
        }

        public void FlashDiskCounter()
        {
            if (diskCounterText != null)
            {
                StartCoroutine(FlashDiskTextRoutine());
            }
        }

        private IEnumerator FlashDiskTextRoutine()
        {
            Color orig = diskCounterText.color;
            diskCounterText.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            if (diskCounterText != null)
            {
                diskCounterText.color = orig;
            }
        }

        private void HandlePlayerDied()
        {
            UpdateHealthUI(0, 3);
        }

        public void ShowVictoryPanel()
        {
            Time.timeScale = 1f;
            UpdateCursorState(true);
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
            HookupVictoryButton();
        }

        public void HookupVictoryButton()
        {
            if (victoryMainMenuButton == null && victoryPanel != null)
            {
                var tr = victoryPanel.transform.Find("Btn_Victory_MainMenu");
                if (tr != null) victoryMainMenuButton = tr.GetComponent<Button>();
                else victoryMainMenuButton = victoryPanel.GetComponentInChildren<Button>(true);
            }

            if (victoryMainMenuButton == null)
            {
                var btnObj = GameObject.Find("Btn_Victory_MainMenu");
                if (btnObj != null) victoryMainMenuButton = btnObj.GetComponent<Button>();
            }

            if (victoryMainMenuButton != null)
            {
                victoryMainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
                victoryMainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        public void HookupExitMenuButton()
        {
            if (exitMenuButton == null)
            {
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b != null && b.gameObject.name == "Btn_SalirMenu_Botones1_4")
                    {
                        exitMenuButton = b;
                        break;
                    }
                }
            }

            if (exitMenuButton != null)
            {
                exitMenuButton.onClick.RemoveListener(OnMainMenuClicked);
                exitMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        public void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        public void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            UpdateCursorState(true);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_MainMenu");
            }
        }

        private IEnumerator ShowBossGuideRoutine()
        {
            isBossGuideActive = true;
            Time.timeScale = 0f;
            UpdateCursorState(true);

            // Crear panel overlay oscuro
            GameObject guidePanel = new GameObject("Panel_BossGuideIntro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            guidePanel.transform.SetParent(transform, false);
            guidePanel.transform.SetAsLastSibling();

            RectTransform rtPanel = guidePanel.GetComponent<RectTransform>();
            rtPanel.anchorMin = Vector2.zero;
            rtPanel.anchorMax = Vector2.one;
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;

            Image bgImage = guidePanel.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.94f);
            bgImage.raycastTarget = true;

            // Imagen de la guía del jefe calabaza
            GameObject imgObj = new GameObject("BossGuide_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgObj.transform.SetParent(guidePanel.transform, false);

            RectTransform rtImg = imgObj.GetComponent<RectTransform>();
            rtImg.anchorMin = Vector2.zero;
            rtImg.anchorMax = Vector2.one;
            rtImg.offsetMin = new Vector2(40f, 65f);
            rtImg.offsetMax = new Vector2(-40f, -25f);

            Image guideImg = imgObj.GetComponent<Image>();
            guideImg.sprite = bossGuideSprite;
            guideImg.preserveAspect = true;
            guideImg.raycastTarget = false;
            guideImg.color = new Color(1f, 1f, 1f, 0f);

            // Texto de continuar / comenzar
            GameObject txtObj = new GameObject("BossGuide_PromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(guidePanel.transform, false);

            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = new Vector2(0.5f, 0f);
            rtTxt.anchorMax = new Vector2(0.5f, 0f);
            rtTxt.pivot = new Vector2(0.5f, 0.5f);
            rtTxt.anchoredPosition = new Vector2(0f, 35f);
            rtTxt.sizeDelta = new Vector2(800f, 45f);

            Text prompt = txtObj.GetComponent<Text>();
            Font defaultFont = null;
            var anyText = GetComponentInChildren<Text>(true);
            if (anyText != null && anyText.font != null)
            {
                defaultFont = anyText.font;
            }
            if (defaultFont == null)
            {
                try
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch
                {
                    // Fallback silencioso si no está disponible
                }
            }
            prompt.font = defaultFont;
            prompt.fontSize = 22;
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = new Color(0.9f, 0.9f, 0.9f, 0f);
            prompt.text = "[ Haz clic o presiona ESPACIO para comenzar ]";

            // Fade-in inicial
            float elapsed = 0f;
            float fadeInDuration = 0.3f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                bgImage.color = new Color(0f, 0f, 0f, 0.94f * alpha);
                guideImg.color = new Color(1f, 1f, 1f, alpha);
                prompt.color = new Color(0.9f, 0.9f, 0.9f, 0.95f * alpha);
                yield return null;
            }
            guideImg.color = Color.white;
            prompt.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

            // Debounce breve
            yield return new WaitForSecondsRealtime(0.2f);

            // Esperar interacción o tiempo límite
            float timer = 0f;
            bool userAdvanced = false;
            while (timer < 8.0f && !userAdvanced)
            {
                timer += Time.unscaledDeltaTime;
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    userAdvanced = true;
                }
                yield return null;
            }

            // Fade-out suave
            elapsed = 0f;
            float fadeOutDuration = 0.25f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                bgImage.color = new Color(0f, 0f, 0f, 0.94f * alpha);
                guideImg.color = new Color(1f, 1f, 1f, alpha);
                prompt.color = new Color(0.9f, 0.9f, 0.9f, 0.95f * alpha);
                yield return null;
            }

            Destroy(guidePanel);

            isBossGuideActive = false;
            Time.timeScale = 1f;
            UpdateCursorState(false);
        }
    }
}
