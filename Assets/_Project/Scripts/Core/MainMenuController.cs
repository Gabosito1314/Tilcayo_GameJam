using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Tilcayo.Core
{
    /// <summary>
    /// Controlador del Menú Principal y de la cinemática de cómics y guía introductoria.
    /// Al pulsar "Iniciar Juego", muestra un fondo negro y reproduce la secuencia:
    /// 1. Cómic 1 aparece primero en el lado izquierdo (~5 segundos).
    /// 2. Cómic 2 aparece a su lado en el lado derecho (~5 segundos).
    /// 3. Cómic 3 aparece en el centro (~5 segundos).
    /// 4. Guía de la calavera (Cyber-Skull Neon) aparece (~5 segundos) antes de entrar al Nivel 1.
    /// Permite avanzar cada diapositiva con Espacio, Enter, E, Clic o por tiempo.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Botones del Menú Principal")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        [Header("Elementos de la Secuencia de Cómic")]
        [SerializeField] private GameObject comicPanel;
        [SerializeField] private Image comicImage1;
        [SerializeField] private Image comicImage2;
        [SerializeField] private Image singleCenterImage;
        [SerializeField] private Text promptText;

        [Header("Sprites de Cómic y Guía")]
        [SerializeField] private Sprite comic1Sprite;
        [SerializeField] private Sprite comic2Sprite;
        [SerializeField] private Sprite comic3Sprite;
        [SerializeField] private Sprite guideCyberSkullSprite;

        [Header("Tiempos y Ajustes")]
        [Tooltip("Tiempo en segundos para Cómic 1 antes de mostrar Cómic 2")]
        [SerializeField] private float waitComic1 = 5.0f;
        [Tooltip("Tiempo en segundos para Cómic 2 a su lado")]
        [SerializeField] private float waitComic2 = 5.0f;
        [Tooltip("Tiempo en segundos para Cómic 3")]
        [SerializeField] private float waitComic3 = 5.0f;
        [Tooltip("Tiempo en segundos para la Guía Cyber-Skull Neon")]
        [SerializeField] private float waitGuideCyberSkull = 5.0f;
        [SerializeField] private string nextSceneName = "Scene_Level_01";

        private bool isComicSequenceRunning = false;
        private bool hasSkippedStep = false;
        private Coroutine comicCoroutine = null;

        private void Awake()
        {
            FindReferencesIfNull();
            EnsureSingleCenterImage();
        }

        private void Start()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (comicPanel != null)
            {
                comicPanel.SetActive(false);
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (isComicSequenceRunning)
            {
                // Permitir al jugador avanzar con Espacio, Enter, E o Clic
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    hasSkippedStep = true;
                }
            }
        }

        public void OnPlayClicked()
        {
            if (isComicSequenceRunning) return;
            comicCoroutine = StartCoroutine(PlayComicSequenceRoutine());
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator PlayComicSequenceRoutine()
        {
            isComicSequenceRunning = true;
            hasSkippedStep = false;

            EnsureSingleCenterImage();

            // Ocultar botones del menú para evitar clics duplicados
            if (playButton != null) playButton.gameObject.SetActive(false);
            if (quitButton != null) quitButton.gameObject.SetActive(false);

            if (comicPanel != null)
            {
                comicPanel.SetActive(true);
            }

            if (singleCenterImage != null)
            {
                singleCenterImage.gameObject.SetActive(false);
            }

            // Asignar sprites si están configurados
            if (comic1Sprite != null && comicImage1 != null) comicImage1.sprite = comic1Sprite;
            if (comic2Sprite != null && comicImage2 != null) comicImage2.sprite = comic2Sprite;

            // ==========================================
            // ETAPA 1: CÓMIC 1 PRIMERO (unos 5 segundos)
            // ==========================================
            if (comicImage1 != null)
            {
                comicImage1.gameObject.SetActive(true);
                comicImage1.color = new Color(1f, 1f, 1f, 0f);
            }

            if (comicImage2 != null)
            {
                comicImage2.gameObject.SetActive(false);
            }

            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "[ Haz clic o presiona ESPACIO para continuar ]";
                promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, 0f);
            }

            // Fade-in de Cómic 1
            float elapsed = 0f;
            float fadeInDuration = 0.28f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                if (comicImage1 != null) comicImage1.color = new Color(1f, 1f, 1f, alpha);
                if (promptText != null) promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, alpha);
                yield return null;
            }
            if (comicImage1 != null) comicImage1.color = Color.white;

            yield return WaitForStepAdvance(waitComic1);

            // ===================================================
            // ETAPA 2: CÓMIC 2 APARECE A SU LADO (unos 5 segundos)
            // ===================================================
            if (comicImage2 != null)
            {
                comicImage2.gameObject.SetActive(true);
                comicImage2.color = new Color(1f, 1f, 1f, 0f);
            }

            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                if (comicImage2 != null) comicImage2.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            if (comicImage2 != null) comicImage2.color = Color.white;

            yield return WaitForStepAdvance(waitComic2);

            // Fade-out de Cómic 1 y 2
            elapsed = 0f;
            float fadeOutDuration = 0.22f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                if (comicImage1 != null) comicImage1.color = new Color(1f, 1f, 1f, alpha);
                if (comicImage2 != null) comicImage2.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            if (comicImage1 != null) comicImage1.gameObject.SetActive(false);
            if (comicImage2 != null) comicImage2.gameObject.SetActive(false);

            // ==========================================
            // ETAPA 3: CÓMIC 3 (unos 5 segundos)
            // ==========================================
            if (singleCenterImage != null && comic3Sprite != null)
            {
                singleCenterImage.sprite = comic3Sprite;
                singleCenterImage.gameObject.SetActive(true);
                singleCenterImage.color = new Color(1f, 1f, 1f, 0f);

                if (promptText != null)
                {
                    promptText.text = "[ Haz clic o presiona ESPACIO para continuar ]";
                }

                elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                    singleCenterImage.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
                singleCenterImage.color = Color.white;

                yield return WaitForStepAdvance(waitComic3);

                // Fade-out Cómic 3
                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                    singleCenterImage.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
                singleCenterImage.gameObject.SetActive(false);
            }

            // ==========================================
            // ETAPA 4: GUÍA DE LA CALAVERA (CYBER-SKULL) (unos 5 segundos)
            // ==========================================
            if (singleCenterImage != null && guideCyberSkullSprite != null)
            {
                singleCenterImage.sprite = guideCyberSkullSprite;
                singleCenterImage.gameObject.SetActive(true);
                singleCenterImage.color = new Color(1f, 1f, 1f, 0f);

                if (promptText != null)
                {
                    promptText.text = "[ Haz clic o presiona ESPACIO para comenzar ]";
                }

                elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                    singleCenterImage.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
                singleCenterImage.color = Color.white;

                yield return WaitForStepAdvance(waitGuideCyberSkull);

                // Fade-out final hacia Nivel 1
                elapsed = 0f;
                float finalFade = 0.35f;
                while (elapsed < finalFade)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = 1f - Mathf.Clamp01(elapsed / finalFade);
                    singleCenterImage.color = new Color(1f, 1f, 1f, alpha);
                    if (promptText != null) promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, alpha);
                    yield return null;
                }
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator WaitForStepAdvance(float duration)
        {
            float timer = 0f;
            hasSkippedStep = false;
            // Debounce para evitar que una pulsación salte múltiples viñetas seguidas
            yield return new WaitForSecondsRealtime(0.15f);
            hasSkippedStep = false;

            while (timer < duration && !hasSkippedStep)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            hasSkippedStep = false;
        }

        public void FindReferencesIfNull()
        {
            if (playButton == null)
            {
                var go = GameObject.Find("Btn_IniciarJuego");
                if (go != null) playButton = go.GetComponent<Button>();
            }

            if (quitButton == null)
            {
                var go = GameObject.Find("Btn_SalirJuego");
                if (go != null) quitButton = go.GetComponent<Button>();
            }

            if (comicPanel == null)
            {
                var panelGo = GameObject.Find("Panel_ComicIntro");
                if (panelGo != null)
                {
                    comicPanel = panelGo;
                    var t1 = panelGo.transform.Find("Comic_Image_1");
                    if (t1 != null) comicImage1 = t1.GetComponent<Image>();
                    var t2 = panelGo.transform.Find("Comic_Image_2");
                    if (t2 != null) comicImage2 = t2.GetComponent<Image>();
                    var ts = panelGo.transform.Find("Comic_Image_Single");
                    if (ts != null) singleCenterImage = ts.GetComponent<Image>();
                    var tp = panelGo.transform.Find("Text_ContinuePrompt");
                    if (tp != null) promptText = tp.GetComponent<Text>();
                }
            }

#if UNITY_EDITOR
            if (comic1Sprite == null)
                comic1Sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Comic/Comic 1 Tilcayo.png");
            if (comic2Sprite == null)
                comic2Sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Comic/Comic 2 Tilcayo.png");
            if (comic3Sprite == null)
                comic3Sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Comic/Comic 3 Tilcayo.png");
            if (guideCyberSkullSprite == null)
                guideCyberSkullSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Comic/Guía del Cyber-Skull Neon.png");
#endif
        }

        private void EnsureSingleCenterImage()
        {
            if (singleCenterImage != null) return;
            if (comicPanel == null) return;

            var existing = comicPanel.transform.Find("Comic_Image_Single");
            if (existing != null)
            {
                singleCenterImage = existing.GetComponent<Image>();
                return;
            }

            GameObject go = new GameObject("Comic_Image_Single", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(comicPanel.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(40f, 65f);
            rt.offsetMax = new Vector2(-40f, -25f);

            singleCenterImage = go.GetComponent<Image>();
            singleCenterImage.preserveAspect = true;
            singleCenterImage.raycastTarget = false;
            singleCenterImage.gameObject.SetActive(false);

            if (promptText != null)
            {
                promptText.transform.SetAsLastSibling();
            }
        }
    }
}
