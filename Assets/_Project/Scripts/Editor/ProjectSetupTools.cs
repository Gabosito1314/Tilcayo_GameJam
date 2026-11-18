using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Tilcayo.Core;
using Tilcayo.Player;
using Tilcayo.Environment;
using Tilcayo.Enemies;
using Tilcayo.Camera;

namespace Tilcayo.Editor
{
    /// <summary>
    /// Herramienta integral de automatización para Tilcayo: Pesadilla en Sistemas.
    /// Configura Layers, Sorting Layers, Físicas 2D, Slicing de Sprites, Animaciones, Prefabs y Escenas.
    /// </summary>
    public static class ProjectSetupTools
    {
        private const string SPRITES_PATH = "Assets/_Project/Sprites";
        private const string ANIMS_PATH = "Assets/_Project/Animations";
        private const string PREFABS_PATH = "Assets/_Project/Prefabs";
        private const string SCENES_PATH = "Assets/Scenes";

        [InitializeOnLoadMethod]
        private static void AutoRunOnce()
        {
            if (!EditorPrefs.GetBool("Tilcayo_UI_V1_Built", false))
            {
                EditorPrefs.SetBool("Tilcayo_UI_V1_Built", true);
                EditorApplication.delayCall += () =>
                {
                    CreateLevelScenes();
                };
            }
        }

        [MenuItem("Tilcayo/6. Actualizar HUD en Escena Actual", false, 60)]
        public static void UpdateHUDInCurrentScene()
        {
            var existingHUD = GameObject.Find("HUD_Canvas");
            if (existingHUD != null)
            {
                Undo.DestroyObjectImmediate(existingHUD);
            }
            CreateInGameHUDCanvas();
            Debug.Log("<color=#00FFAA>[Tilcayo Setup] ¡HUD actualizado en la escena actual!</color>");
        }

        [MenuItem("Tilcayo/7. Configurar Cómic en Menú Principal", false, 65)]
        public static void SetupComicIntroInMainMenuScene()
        {
            string scenePath = $"{SCENES_PATH}/Scene_MainMenu.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            var canvasObj = GameObject.Find("MainMenu_Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Tilcayo] No se encontró MainMenu_Canvas en Scene_MainMenu.");
                return;
            }

            var menuCtrl = canvasObj.GetComponent<MainMenuController>();
            if (menuCtrl == null) menuCtrl = canvasObj.AddComponent<MainMenuController>();

            Transform existingPanel = canvasObj.transform.Find("Panel_ComicIntro");
            if (existingPanel != null)
            {
                Undo.DestroyObjectImmediate(existingPanel.gameObject);
            }

            GameObject comicPanel = new GameObject("Panel_ComicIntro");
            comicPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtComic = comicPanel.AddComponent<RectTransform>();
            rtComic.anchorMin = Vector2.zero;
            rtComic.anchorMax = Vector2.one;
            rtComic.offsetMin = Vector2.zero;
            rtComic.offsetMax = Vector2.zero;
            var comicBg = comicPanel.AddComponent<Image>();
            comicBg.color = Color.black;
            comicBg.raycastTarget = true;

            var comic1Sprites = LoadSprites($"{SPRITES_PATH}/Comic/Comic 1 Tilcayo.png");
            var comic2Sprites = LoadSprites($"{SPRITES_PATH}/Comic/Comic 2 Tilcayo.png");
            Sprite spComic1 = (comic1Sprites != null && comic1Sprites.Length > 0) ? comic1Sprites[0] : null;
            Sprite spComic2 = (comic2Sprites != null && comic2Sprites.Length > 0) ? comic2Sprites[0] : null;

            // Cómic 1 (Mitad izquierda)
            GameObject c1Obj = new GameObject("Comic_Image_1");
            c1Obj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtC1 = c1Obj.AddComponent<RectTransform>();
            rtC1.anchorMin = new Vector2(0f, 0f);
            rtC1.anchorMax = new Vector2(0.5f, 1f);
            rtC1.anchoredPosition = Vector2.zero;
            rtC1.sizeDelta = new Vector2(-80f, -80f);
            var imgC1 = c1Obj.AddComponent<Image>();
            if (spComic1 != null) imgC1.sprite = spComic1;
            imgC1.preserveAspect = true;
            imgC1.color = Color.white;
            imgC1.raycastTarget = false;

            // Cómic 2 (Mitad derecha)
            GameObject c2Obj = new GameObject("Comic_Image_2");
            c2Obj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtC2 = c2Obj.AddComponent<RectTransform>();
            rtC2.anchorMin = new Vector2(0.5f, 0f);
            rtC2.anchorMax = new Vector2(1f, 1f);
            rtC2.anchoredPosition = Vector2.zero;
            rtC2.sizeDelta = new Vector2(-80f, -80f);
            var imgC2 = c2Obj.AddComponent<Image>();
            if (spComic2 != null) imgC2.sprite = spComic2;
            imgC2.preserveAspect = true;
            imgC2.color = Color.white;
            imgC2.raycastTarget = false;

            // Texto de indicación para continuar
            GameObject promptObj = new GameObject("Text_ContinuePrompt");
            promptObj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtPrompt = promptObj.AddComponent<RectTransform>();
            rtPrompt.anchorMin = new Vector2(0.5f, 0f);
            rtPrompt.anchorMax = new Vector2(0.5f, 0f);
            rtPrompt.anchoredPosition = new Vector2(0f, 40f);
            rtPrompt.sizeDelta = new Vector2(800f, 45f);
            var txtPrompt = promptObj.AddComponent<Text>();
            txtPrompt.text = "[ Haz clic o presiona ESPACIO para continuar ]";
            txtPrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPrompt.fontSize = 22;
            txtPrompt.alignment = TextAnchor.LowerCenter;
            txtPrompt.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
            txtPrompt.raycastTarget = false;

            comicPanel.SetActive(false);

            var btnPlayObj = GameObject.Find("Btn_IniciarJuego");
            var btnQuitObj = GameObject.Find("Btn_SalirJuego");
            Button btnPlay = btnPlayObj != null ? btnPlayObj.GetComponent<Button>() : null;
            Button btnQuit = btnQuitObj != null ? btnQuitObj.GetComponent<Button>() : null;

            SerializedObject soCtrl = new SerializedObject(menuCtrl);
            soCtrl.FindProperty("playButton").objectReferenceValue = btnPlay;
            soCtrl.FindProperty("quitButton").objectReferenceValue = btnQuit;
            soCtrl.FindProperty("comicPanel").objectReferenceValue = comicPanel;
            soCtrl.FindProperty("comicImage1").objectReferenceValue = imgC1;
            soCtrl.FindProperty("comicImage2").objectReferenceValue = imgC2;
            soCtrl.FindProperty("promptText").objectReferenceValue = txtPrompt;
            soCtrl.FindProperty("comic1Sprite").objectReferenceValue = spComic1;
            soCtrl.FindProperty("comic2Sprite").objectReferenceValue = spComic2;
            soCtrl.FindProperty("waitBeforeComic2").floatValue = 4.0f;
            soCtrl.FindProperty("comic1DimBrightness").floatValue = 0.32f;
            soCtrl.FindProperty("waitAfterComic2").floatValue = 5.0f;
            soCtrl.FindProperty("nextSceneName").stringValue = "Scene_Level_01";
            soCtrl.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=#00FFAA>[Tilcayo Setup] ¡Secuencia de Cómic configurada en Scene_MainMenu con éxito!</color>");
        }

        [MenuItem("Tilcayo/EJECUTAR SETUP COMPLETO (FASE 1 A 5)", false, 0)]
        public static void RunFullSetup()
        {
            SetupLayersAndPhysics();
            ConfigureAndSliceSprites();
            CreateAnimationsAndControllers();
            CreatePrefabs();
            CreateLevelScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Tilcayo Setup] ¡PROYECTO CONFIGURADO AL 100% CON ÉXITO!</b></color>");
        }

        [MenuItem("Tilcayo/CONFIGURAR NIVEL 1 COMO LÍNEA PLANA DE PRUEBA", false, 1)]
        public static void SetupLevel1FlatLine()
        {
            BuildLevelScene($"{SCENES_PATH}/Scene_Level_01.unity", 1, "Scene_Level_02");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"{SCENES_PATH}/Scene_Level_01.unity");
            Debug.Log("<color=#00FF88><b>[Tilcayo] ¡NIVEL 1 CONFIGURADO COMO LÍNEA PLANA LIMPIA!</b></color>");
        }

        [MenuItem("Tilcayo/1. Configurar Tags, Layers y Matriz de Físicas 2D", false, 10)]
        public static void SetupLayersAndPhysics()
        {
            // 1. Configurar Layers en TagManager.asset
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            string[] targetLayers = new string[]
            {
                "Ground",          // 6
                "Player",          // 7
                "CloneSlime",      // 8
                "PassableGrates",  // 9
                "Hazards",         // 10
                "Enemy",           // 11
                "Checkpoint",      // 12
                "Collectible"      // 13
            };

            for (int i = 0; i < targetLayers.Length; i++)
            {
                int layerIndex = 6 + i;
                SerializedProperty prop = layersProp.GetArrayElementAtIndex(layerIndex);
                prop.stringValue = targetLayers[i];
            }

            // 2. Configurar Sorting Layers
            SerializedProperty sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            string[] requiredSortingLayers = new string[]
            {
                "Default",
                "Background",
                "Environment",
                "Traps",
                "Characters",
                "Foreground",
                "UI"
            };

            // Recolectar nombres existentes
            HashSet<string> existingNames = new HashSet<string>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var el = sortingLayersProp.GetArrayElementAtIndex(i);
                existingNames.Add(el.FindPropertyRelative("name").stringValue);
            }

            for (int i = 0; i < requiredSortingLayers.Length; i++)
            {
                string sName = requiredSortingLayers[i];
                if (!existingNames.Contains(sName))
                {
                    int newIndex = sortingLayersProp.arraySize;
                    sortingLayersProp.InsertArrayElementAtIndex(newIndex);
                    var newLayer = sortingLayersProp.GetArrayElementAtIndex(newIndex);
                    newLayer.FindPropertyRelative("name").stringValue = sName;
                    newLayer.FindPropertyRelative("uniqueID").intValue = 1000 + newIndex;
                }
            }

            tagManager.ApplyModifiedProperties();

            // 3. Matriz de colisiones de Physics2D
            int layerGround = LayerMask.NameToLayer("Ground");
            int layerPlayer = LayerMask.NameToLayer("Player");
            int layerClone = LayerMask.NameToLayer("CloneSlime");
            int layerGrates = LayerMask.NameToLayer("PassableGrates");
            int layerHazards = LayerMask.NameToLayer("Hazards");
            int layerEnemy = LayerMask.NameToLayer("Enemy");

            if (layerClone != -1 && layerGrates != -1)
            {
                // CloneSlime IGNORA PassableGrates
                Physics2D.IgnoreLayerCollision(layerClone, layerGrates, true);
                Debug.Log("[Physics2D] CloneSlime ignora PassableGrates configurado.");
            }
            if (layerClone != -1 && layerHazards != -1)
            {
                // CloneSlime IGNORA Hazards
                Physics2D.IgnoreLayerCollision(layerClone, layerHazards, true);
            }
            if (layerClone != -1 && layerEnemy != -1)
            {
                Physics2D.IgnoreLayerCollision(layerClone, layerEnemy, true);
            }
            if (layerClone != -1 && layerPlayer != -1)
            {
                Physics2D.IgnoreLayerCollision(layerClone, layerPlayer, true);
            }

            if (layerPlayer != -1 && layerGrates != -1)
            {
                // Player NO ignora PassableGrates (colisiona y lo bloquea)
                Physics2D.IgnoreLayerCollision(layerPlayer, layerGrates, false);
            }
            if (layerPlayer != -1 && layerGround != -1)
            {
                Physics2D.IgnoreLayerCollision(layerPlayer, layerGround, false);
            }

            Debug.Log("[Tilcayo Setup] Tags, Layers y Matriz de Colisiones 2D aplicados correctamente.");
        }

        [MenuItem("Tilcayo/2. Configurar y Cortar Sprites", false, 20)]
        public static void ConfigureAndSliceSprites()
        {
            // Fondos (Single)
            ConfigureSingleSprite($"{SPRITES_PATH}/Backgrounds/Stage 1.png", TextureWrapMode.Repeat);
            ConfigureSingleSprite($"{SPRITES_PATH}/Backgrounds/Stage 2.png", TextureWrapMode.Repeat);
            ConfigureSingleSprite($"{SPRITES_PATH}/Backgrounds/Stage Final.png", TextureWrapMode.Repeat);
            ConfigureSingleSprite($"{SPRITES_PATH}/Backgrounds/Stage Alternativo.png", TextureWrapMode.Repeat);

            // Logos y Key Art (Single)
            ConfigureSingleSprite($"{SPRITES_PATH}/UI/Logo Tilcayo.png", TextureWrapMode.Clamp);
            ConfigureSingleSprite($"{SPRITES_PATH}/UI/Menu Principal.png", TextureWrapMode.Clamp);
            ConfigureSingleSprite($"{SPRITES_PATH}/UI/Promocional Tilcayo.png", TextureWrapMode.Clamp);

            // Spritesheets de personajes y enemigos con recorte milimétrico calibrado (PPU 400, sin sangrado de frames ni deslizamiento)
            // 1. Idle Tilcayo (3 frames uniformes de 550x793, exactamente centrados en el cuerpo a 275px, sin cola del 3er frame y anclados al suelo)
            SliceCustomSprites($"{SPRITES_PATH}/Characters/Idle Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Idle_0", 75, 0, 550, 793, 0.5f, 0.1614f),
                new CustomSliceDef("Idle_1", 649, 0, 550, 793, 0.5f, 0.1614f),
                new CustomSliceDef("Idle_2", 1223, 0, 550, 793, 0.5f, 0.1614f)
            }, 400f);

            // 2. Walk Tilcayo (3 frames uniformes de 620x793 con pies al nivel de Idle)
            SliceCustomSprites($"{SPRITES_PATH}/Characters/Walk Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Walk_0", 17, 0, 620, 793, 0.5f, 0.2799f),
                new CustomSliceDef("Walk_1", 664, 0, 620, 793, 0.5f, 0.2686f),
                new CustomSliceDef("Walk_2", 1308, 0, 620, 793, 0.5f, 0.2522f)
            }, 400f);

            // 3. Jump Tilcayo (3 frames uniformes de 600x793 anclados en base de despegue)
            SliceCustomSprites($"{SPRITES_PATH}/Characters/Jump Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Jump_0", 1, 0, 600, 793, 0.5f, 0.1412f),
                new CustomSliceDef("Jump_1", 685, 0, 600, 793, 0.5f, 0.1412f),
                new CustomSliceDef("Jump_2", 1349, 0, 600, 793, 0.5f, 0.1412f)
            }, 400f);

            // 4. Hability Tilcayo (3 frames uniformes de 580x793)
            SliceCustomSprites($"{SPRITES_PATH}/Characters/Hability Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Hability_0", 94, 0, 580, 793, 0.5f, 0.1412f),
                new CustomSliceDef("Hability_1", 673, 0, 580, 793, 0.5f, 0.1412f),
                new CustomSliceDef("Hability_2", 1316, 0, 580, 793, 0.5f, 0.1412f)
            }, 400f);

            // 5. Defeated Tilcayo (4 frames limpios anclados al suelo)
            SliceCustomSprites($"{SPRITES_PATH}/Characters/Defeated Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Defeat_0", 0, 0, 368, 941, 184f/368f, 0.3188f),
                new CustomSliceDef("Defeat_1", 368, 0, 422, 941, (593f - 368f)/422f, 0.3188f),
                new CustomSliceDef("Defeat_2", 790, 0, 433, 941, (1000f - 790f)/433f, 0.3188f),
                new CustomSliceDef("Defeat_3", 1223, 0, 449, 941, (1441f - 1223f)/449f, 0.3188f)
            }, 400f);

            // 6. Slime TP (4 frames limpios de habilidad)
            SliceCustomSprites($"{SPRITES_PATH}/Slime/Slime TP.png", new CustomSliceDef[]
            {
                new CustomSliceDef("SlimeTP_0", 0, 0, 465, 793, 248f/465f, 0.1790f),
                new CustomSliceDef("SlimeTP_1", 465, 0, 610, 793, (775f - 465f)/610f, 0.1790f),
                new CustomSliceDef("SlimeTP_2", 1075, 0, 453, 793, (1303f - 1075f)/453f, 0.1790f),
                new CustomSliceDef("SlimeTP_3", 1528, 0, 455, 793, (1741f - 1528f)/455f, 0.1790f)
            }, 400f);

            // 7. Slime Code Tilcayo (3 frames limpios para clon)
            SliceCustomSprites($"{SPRITES_PATH}/Slime/Slime Code Tilcayo.png", new CustomSliceDef[]
            {
                new CustomSliceDef("SlimeTilcayo_0", 0, 0, 625, 887, 312f/625f, 0.1477f),
                new CustomSliceDef("SlimeTilcayo_1", 625, 0, 452, 887, (848f - 625f)/452f, 0.1477f),
                new CustomSliceDef("SlimeTilcayo_2", 1077, 0, 697, 887, (1422f - 1077f)/697f, 0.1477f)
            }, 400f);

            // 8. Calavera Enemy Walk (3 frames uniformes de 540x793)
            SliceCustomSprites($"{SPRITES_PATH}/Enemies/Calavera Enemy Walk.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Calavera_0", 110, 0, 540, 793, 0.5f, 0.0f),
                new CustomSliceDef("Calavera_1", 706, 0, 540, 793, 0.5f, 0.0f),
                new CustomSliceDef("Calavera_2", 1306, 0, 540, 793, 0.5f, 0.0f)
            }, 400f);

            // 9. Final Boss Walk (3 frames uniformes de 550x941)
            SliceCustomSprites($"{SPRITES_PATH}/Enemies/Final Boss Walk.png", new CustomSliceDef[]
            {
                new CustomSliceDef("BossWalk_0", 0, 0, 550, 941, 0.5f, 0.0f),
                new CustomSliceDef("BossWalk_1", 554, 0, 550, 941, 0.5f, 0.0f),
                new CustomSliceDef("BossWalk_2", 1118, 0, 550, 941, 0.5f, 0.0f)
            }, 400f);

            // 10. Final Boss Defeat (3 frames limpios)
            SliceCustomSprites($"{SPRITES_PATH}/Enemies/Final Boss Defeat.png", new CustomSliceDef[]
            {
                new CustomSliceDef("BossDefeat_0", 0, 0, 520, 724, 268f/520f, 0.0f),
                new CustomSliceDef("BossDefeat_1", 520, 0, 546, 724, (795f - 520f)/546f, 0.0f),
                new CustomSliceDef("BossDefeat_2", 1066, 0, 541, 724, (1339f - 1066f)/541f, 0.0f)
            }, 400f);

            // Hojas de recursos y botones cortados en cuadrícula 4x3
            SliceGrid($"{SPRITES_PATH}/Environment/Hoja de Recursos 1.png", 4, 3, "Recurso1", 100f);
            SliceGrid($"{SPRITES_PATH}/Environment/Hoja de Recursos 2.png", 4, 3, "Recurso2", 100f);
            SliceGrid($"{SPRITES_PATH}/Environment/Hoja de Recursos 3.png", 4, 2, "Recurso3", 100f);
            SliceGrid($"{SPRITES_PATH}/Environment/Hoja de Recursos 4.png", 4, 2, "Recurso4", 100f);

            // 11. Botones 1 (Recortes limpios para UI)
            SliceCustomSprites($"{SPRITES_PATH}/UI/Botones 1.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Botones 1_0", 15, 792, 777, 273, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_1", 14, 535, 774, 260, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_2", 775, 658, 315, 349, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_3", 1096, 655, 352, 431, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_4", 10, 275, 779, 264, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_5", 802, 282, 196, 190, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_6", 965, 471, 265, 211, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_7", 1012, 281, 195, 190, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_8", 1221, 281, 196, 191, 0.5f, 0.5f),
                new CustomSliceDef("Botones 1_9", 8, 24, 1431, 250, 0.5f, 0.5f)
            }, 100f);

            // 12. Botones 2 (Recortes limpios sin restos adyacentes y Banner de Victoria aislado)
            SliceCustomSprites($"{SPRITES_PATH}/UI/Botones 2.png", new CustomSliceDef[]
            {
                new CustomSliceDef("Botones 2_0", 48, 729, 243, 240, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_1", 295, 729, 242, 240, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_2", 542, 729, 242, 240, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_3", 810, 698, 296, 310, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_4", 1106, 698, 382, 256, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_5", 15, 485, 501, 226, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_6", 520, 485, 496, 239, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_7", 1020, 485, 501, 226, 0.5f, 0.5f),
                new CustomSliceDef("Botones 2_8", 19, 213, 1497, 271, 0.5f, 0.5f)
            }, 100f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tilcayo Setup] Configuración y corte de sprites completado.");
        }

        private struct CustomSliceDef
        {
            public string name;
            public Rect rect;
            public Vector2 pivot;

            public CustomSliceDef(string name, float x, float y, float w, float h, float px = 0.5f, float py = 0.0f)
            {
                this.name = name;
                this.rect = new Rect(x, y, w, h);
                this.pivot = new Vector2(px, py);
            }
        }

        private static void SliceCustomSprites(string assetPath, CustomSliceDef[] defs, float ppu = 400f)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            SpriteMetaData[] metas = new SpriteMetaData[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = defs[i].name,
                    rect = defs[i].rect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = defs[i].pivot
                };
            }

            importer.spritesheet = metas;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static void ConfigureSingleSprite(string assetPath, TextureWrapMode wrapMode)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = wrapMode;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void SliceGrid(string assetPath, int columns, int rows, string prefix, float ppu)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // Si ya cuenta con cortes de sprite existentes, no sobreescribir
            if (importer.spritesheet != null && importer.spritesheet.Length > 0)
            {
                return;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            int width = tex != null ? tex.width : 1536;
            int height = tex != null ? tex.height : 1024;

            float cellW = (float)width / columns;
            float cellH = (float)height / rows;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    // En coordenadas de textura, Y=0 está abajo
                    float y = (rows - 1 - r) * cellH;
                    float x = c * cellW;

                    metas.Add(new SpriteMetaData
                    {
                        name = $"{prefix}_{r}_{c}",
                        rect = new Rect(x, y, cellW, cellH),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                }
            }

            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        [MenuItem("Tilcayo/3. Generar Animaciones y Controladores", false, 30)]
        public static void CreateAnimationsAndControllers()
        {
            if (!Directory.Exists(ANIMS_PATH)) Directory.CreateDirectory(ANIMS_PATH);

            // 1. Clips de Tilcayo
            var idleSprites = LoadSprites($"{SPRITES_PATH}/Characters/Idle Tilcayo.png");
            var walkSprites = LoadSprites($"{SPRITES_PATH}/Characters/Walk Tilcayo.png");
            var jumpSprites = LoadSprites($"{SPRITES_PATH}/Characters/Jump Tilcayo.png");
            var habilitySprites = LoadSprites($"{SPRITES_PATH}/Characters/Hability Tilcayo.png");
            var defeatSprites = LoadSprites($"{SPRITES_PATH}/Characters/Defeated Tilcayo.png");

            var clipIdle = CreateAnimationClip($"{ANIMS_PATH}/Tilcayo_Idle.anim", idleSprites, 4f, true);
            Sprite[] walkPingPong = (walkSprites != null && walkSprites.Length >= 3)
                ? new Sprite[] { walkSprites[0], walkSprites[1], walkSprites[2], walkSprites[1] }
                : walkSprites;
            var clipWalk = CreateAnimationClip($"{ANIMS_PATH}/Tilcayo_Walk.anim", walkPingPong, 6.67f, true);

            // Secuencia exacta de salto solicitada: Jump_0 (despegue 0.08s), Jump_1 (ascenso 0.24s), Hability_0 (ápice 0.16s), Jump_2 (caída 0.24s)
            Sprite habilityApex = (habilitySprites != null && habilitySprites.Length > 0) ? habilitySprites[0] : null;
            Sprite[] jumpSequence = new Sprite[]
            {
                jumpSprites != null && jumpSprites.Length > 0 ? jumpSprites[0] : null,
                jumpSprites != null && jumpSprites.Length > 1 ? jumpSprites[1] : null,
                habilityApex,
                jumpSprites != null && jumpSprites.Length > 2 ? jumpSprites[2] : null
            };
            float[] jumpTimes = new float[] { 0f, 0.08f, 0.32f, 0.48f };
            var clipJump = CreateTimedAnimationClip($"{ANIMS_PATH}/Tilcayo_Jump.anim", jumpSequence, jumpTimes, 0.72f, false);

            // Regla GDD / Usuario: para Hability solo se usan los 2 últimos frames (Hability_1, Hability_2)
            Sprite[] crouchHabilitySprites = habilitySprites != null && habilitySprites.Length >= 2
                ? new Sprite[] { habilitySprites[habilitySprites.Length - 2], habilitySprites[habilitySprites.Length - 1] }
                : habilitySprites;
            var clipHability = CreateAnimationClip($"{ANIMS_PATH}/Tilcayo_Hability.anim", crouchHabilitySprites, 6f, false);

            // Defeat en el lugar de muerte: Defeat_0, Defeat_1, Defeat_2
            Sprite[] deathSequence = (defeatSprites != null && defeatSprites.Length >= 3)
                ? new Sprite[] { defeatSprites[0], defeatSprites[1], defeatSprites[2] }
                : defeatSprites;
            var clipDefeat = CreateAnimationClip($"{ANIMS_PATH}/Tilcayo_Defeat.anim", deathSequence, 5f, false);

            // Dazed en el punto de respawn: Defeat_3 (hasta que el jugador se mueva)
            Sprite[] dazedSequence = (defeatSprites != null && defeatSprites.Length >= 4)
                ? new Sprite[] { defeatSprites[3] }
                : defeatSprites;
            var clipDazed = CreateAnimationClip($"{ANIMS_PATH}/Tilcayo_Dazed.anim", dazedSequence, 5f, true);

            // Crear Animator Controller de Tilcayo
            string tilcayoControllerPath = $"{ANIMS_PATH}/Tilcayo_AnimatorController.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(tilcayoControllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsUsingAbility", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDazed", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Teleport", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = clipIdle;
            rootStateMachine.defaultState = idleState;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = clipWalk;

            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = clipJump;

            var abilityState = rootStateMachine.AddState("Hability");
            abilityState.motion = clipHability;

            var defeatState = rootStateMachine.AddState("Defeat");
            defeatState.motion = clipDefeat;

            var dazedState = rootStateMachine.AddState("Dazed");
            dazedState.motion = clipDazed;

            // Transiciones
            // Idle -> Walk
            var tIdleToWalk = idleState.AddTransition(walkState);
            tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0.05f;

            // Walk -> Idle
            var tWalkToIdle = walkState.AddTransition(idleState);
            tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0.05f;

            // AnyState -> Jump
            var tAnyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            tAnyToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tAnyToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDazed");
            tAnyToJump.hasExitTime = false;
            tAnyToJump.duration = 0.02f;
            tAnyToJump.canTransitionToSelf = false;

            // Jump -> Idle
            var tJumpToIdle = jumpState.AddTransition(idleState);
            tJumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            tJumpToIdle.hasExitTime = false;
            tJumpToIdle.duration = 0.05f;

            // AnyState -> Ability
            var tAnyToAbility = rootStateMachine.AddAnyStateTransition(abilityState);
            tAnyToAbility.AddCondition(AnimatorConditionMode.If, 0, "IsUsingAbility");
            tAnyToAbility.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyToAbility.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDazed");
            tAnyToAbility.hasExitTime = false;
            tAnyToAbility.duration = 0.05f;
            tAnyToAbility.canTransitionToSelf = false;

            // Ability -> Idle
            var tAbilityToIdle = abilityState.AddTransition(idleState);
            tAbilityToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsUsingAbility");
            tAbilityToIdle.hasExitTime = true;
            tAbilityToIdle.duration = 0.05f;

            // AnyState -> Defeat (en el lugar donde muere con 0 vidas)
            var tAnyToDefeat = rootStateMachine.AddAnyStateTransition(defeatState);
            tAnyToDefeat.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            tAnyToDefeat.hasExitTime = false;
            tAnyToDefeat.duration = 0.02f;
            tAnyToDefeat.canTransitionToSelf = false;

            // AnyState -> Dazed (al reaparecer mareado en el checkpoint)
            var tAnyToDazed = rootStateMachine.AddAnyStateTransition(dazedState);
            tAnyToDazed.AddCondition(AnimatorConditionMode.If, 0, "IsDazed");
            tAnyToDazed.hasExitTime = false;
            tAnyToDazed.duration = 0.02f;
            tAnyToDazed.canTransitionToSelf = false;

            // Dazed -> Idle (cuando el jugador presiona cualquier dirección o botón de acción)
            var tDazedToIdle = dazedState.AddTransition(idleState);
            tDazedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDazed");
            tDazedToIdle.hasExitTime = false;
            tDazedToIdle.duration = 0.05f;

            // 2. Animaciones de Slime (Viaje intercalando SlimeTP_1 y SlimeTP_2 -> Settle SlimeTP_0, SlimeTP_3 -> Waiting SlimeTilcayo_1)
            var slimeTPSprites = LoadSprites($"{SPRITES_PATH}/Slime/Slime TP.png");
            var slimeTilcayoSprites = LoadSprites($"{SPRITES_PATH}/Slime/Slime Code Tilcayo.png");

            Sprite[] crawlSprites = (slimeTPSprites != null && slimeTPSprites.Length >= 3)
                ? new Sprite[] { slimeTPSprites[1], slimeTPSprites[2] }
                : slimeTPSprites;
            var clipSlimeCrawl = CreateAnimationClip($"{ANIMS_PATH}/Slime_Crawl.anim", crawlSprites, 6f, true);

            Sprite[] settleSprites = (slimeTPSprites != null && slimeTPSprites.Length >= 4)
                ? new Sprite[] { slimeTPSprites[0], slimeTPSprites[3] }
                : slimeTPSprites;
            var clipSlimeTP = CreateAnimationClip($"{ANIMS_PATH}/Slime_Burst.anim", settleSprites, 8f, false);

            Sprite[] waitingSprites = (slimeTilcayoSprites != null && slimeTilcayoSprites.Length >= 2)
                ? new Sprite[] { slimeTilcayoSprites[1] }
                : slimeTilcayoSprites;
            var clipSlimeWaiting = CreateAnimationClip($"{ANIMS_PATH}/Slime_Waiting.anim", waitingSprites, 4f, true);

            var slimeController = AnimatorController.CreateAnimatorControllerAtPath($"{ANIMS_PATH}/Slime_AnimatorController.controller");
            slimeController.AddParameter("Arrived", AnimatorControllerParameterType.Trigger);
            var smSlime = slimeController.layers[0].stateMachine;

            var stateSlimeCrawl = smSlime.AddState("Crawl");
            stateSlimeCrawl.motion = clipSlimeCrawl;
            smSlime.defaultState = stateSlimeCrawl;

            var stateSlimeSettle = smSlime.AddState("Settle");
            stateSlimeSettle.motion = clipSlimeTP;

            var stateSlimeWaiting = smSlime.AddState("Waiting");
            stateSlimeWaiting.motion = clipSlimeWaiting;

            var tCrawlToSettle = stateSlimeCrawl.AddTransition(stateSlimeSettle);
            tCrawlToSettle.AddCondition(AnimatorConditionMode.If, 0, "Arrived");
            tCrawlToSettle.hasExitTime = false;
            tCrawlToSettle.duration = 0.02f;

            var tSettleToWaiting = stateSlimeSettle.AddTransition(stateSlimeWaiting);
            tSettleToWaiting.hasExitTime = true;
            tSettleToWaiting.duration = 0.02f;

            // 3. Animaciones de Calavery y Boss
            var calaveraSprites = LoadSprites($"{SPRITES_PATH}/Enemies/Calavera Enemy Walk.png");
            // Ping-pong 1-2-3-2 para animación suave según GDD
            Sprite[] calaveraPingPong = calaveraSprites != null && calaveraSprites.Length >= 3
                ? new Sprite[] { calaveraSprites[0], calaveraSprites[1], calaveraSprites[2], calaveraSprites[1] }
                : calaveraSprites;
            var clipCalavera = CreateAnimationClip($"{ANIMS_PATH}/Calavera_Walk.anim", calaveraPingPong, 6f, true);

            var calaveraController = AnimatorController.CreateAnimatorControllerAtPath($"{ANIMS_PATH}/Calavera_AnimatorController.controller");
            var smCalavera = calaveraController.layers[0].stateMachine;
            var stateCalavera = smCalavera.AddState("Patrol");
            stateCalavera.motion = clipCalavera;
            smCalavera.defaultState = stateCalavera;

            // Boss
            var bossWalkSprites = LoadSprites($"{SPRITES_PATH}/Enemies/Final Boss Walk.png");
            var bossDefeatSprites = LoadSprites($"{SPRITES_PATH}/Enemies/Final Boss Defeat.png");
            Sprite[] bossWalkPingPong = bossWalkSprites != null && bossWalkSprites.Length >= 3
                ? new Sprite[] { bossWalkSprites[0], bossWalkSprites[1], bossWalkSprites[2], bossWalkSprites[1] }
                : bossWalkSprites;
            var clipBossWalk = CreateAnimationClip($"{ANIMS_PATH}/Boss_Walk.anim", bossWalkPingPong, 5f, true);
            var clipBossDefeat = CreateAnimationClip($"{ANIMS_PATH}/Boss_Defeat.anim", bossDefeatSprites, 4f, false);

            var bossController = AnimatorController.CreateAnimatorControllerAtPath($"{ANIMS_PATH}/Boss_AnimatorController.controller");
            bossController.AddParameter("Defeat", AnimatorControllerParameterType.Trigger);
            var smBoss = bossController.layers[0].stateMachine;
            var stateBossWalk = smBoss.AddState("Walk");
            stateBossWalk.motion = clipBossWalk;
            smBoss.defaultState = stateBossWalk;
            var stateBossDefeat = smBoss.AddState("Defeat");
            stateBossDefeat.motion = clipBossDefeat;
            var tBossDefeat = smBoss.AddAnyStateTransition(stateBossDefeat);
            tBossDefeat.AddCondition(AnimatorConditionMode.If, 0, "Defeat");
            tBossDefeat.hasExitTime = false;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tilcayo Setup] Animaciones y Controladores creados con éxito.");
        }

        private static Sprite[] LoadSprites(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s) sprites.Add(s);
            }
            sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            return sprites.ToArray();
        }

        private static Sprite FindSpriteByName(Sprite[] sprites, string name)
        {
            if (sprites == null) return null;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == name)
                    return sprites[i];
            }
            return null;
        }

        private static AnimationClip CreateAnimationClip(string path, Sprite[] sprites, float frameRate, bool loop)
        {
            var clip = new AnimationClip();
            clip.frameRate = frameRate;

            if (loop)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            if (sprites != null && sprites.Length > 0)
            {
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = (float)i / frameRate,
                        value = sprites[i]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimationClip CreateTimedAnimationClip(string path, Sprite[] sprites, float[] frameTimes, float totalDuration, bool loop)
        {
            var clip = new AnimationClip();
            clip.frameRate = 25f;

            if (loop)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.stopTime = totalDuration;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
            else
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                settings.stopTime = totalDuration;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            if (sprites != null && sprites.Length > 0 && frameTimes != null && frameTimes.Length == sprites.Length)
            {
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = frameTimes[i],
                        value = sprites[i]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        [MenuItem("Tilcayo/4. Generar Prefabs de Juego", false, 40)]
        public static void CreatePrefabs()
        {
            if (!Directory.Exists(PREFABS_PATH)) Directory.CreateDirectory(PREFABS_PATH);

            // 1. Prefab Slime Ooze - Escala 1.5x coincidente con Tilcayo
            GameObject slimeObj = new GameObject("SlimeOoze");
            slimeObj.layer = LayerMask.NameToLayer("CloneSlime");
            slimeObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var slimeSR = slimeObj.AddComponent<SpriteRenderer>();
            slimeSR.sortingLayerName = "Characters";
            var tpSprites = LoadSprites($"{SPRITES_PATH}/Slime/Slime TP.png");
            var codeSprites = LoadSprites($"{SPRITES_PATH}/Slime/Slime Code Tilcayo.png");
            Sprite tp1 = FindSpriteByName(tpSprites, "SlimeTP_1");
            Sprite tp2 = FindSpriteByName(tpSprites, "SlimeTP_2");
            Sprite tp0 = FindSpriteByName(tpSprites, "SlimeTP_0");
            Sprite tp3 = FindSpriteByName(tpSprites, "SlimeTP_3");
            Sprite tilcayo1 = FindSpriteByName(codeSprites, "SlimeTilcayo_1");

            if (tp1 != null) slimeSR.sprite = tp1;

            var slimeRb = slimeObj.AddComponent<Rigidbody2D>();
            slimeRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            slimeRb.freezeRotation = true;
            slimeRb.gravityScale = 2.0f;

            var slimeCol = slimeObj.AddComponent<CircleCollider2D>();
            slimeCol.radius = 0.25f;
            slimeCol.offset = new Vector2(0f, 0.25f);

            var slimeScript = slimeObj.AddComponent<CodeOozeCrawler>();
            slimeScript.SetSprites(tp1, tp2, tp0, tp3, tilcayo1);

            string slimePrefabPath = $"{PREFABS_PATH}/SlimeOoze.prefab";
            GameObject slimePrefab = PrefabUtility.SaveAsPrefabAsset(slimeObj, slimePrefabPath);
            GameObject.DestroyImmediate(slimeObj);

            // 2. Prefab Tilcayo (Player) - Escala X:1.5, Y:1.5 solicitada
            GameObject playerObj = new GameObject("Tilcayo_Player");
            playerObj.tag = "Player";
            playerObj.layer = LayerMask.NameToLayer("Player");
            playerObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var playerSR = playerObj.AddComponent<SpriteRenderer>();
            playerSR.sortingLayerName = "Characters";
            var idleSprites = LoadSprites($"{SPRITES_PATH}/Characters/Idle Tilcayo.png");
            if (idleSprites != null && idleSprites.Length > 0) playerSR.sprite = idleSprites[0];

            var playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerRb.freezeRotation = true;
            playerRb.gravityScale = 3.0f;

            var playerCol = playerObj.AddComponent<CapsuleCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.7f);
            playerCol.offset = new Vector2(0f, 0.85f);

            var playerAnim = playerObj.AddComponent<Animator>();
            var playerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ANIMS_PATH}/Tilcayo_AnimatorController.controller");
            if (playerCtrl != null) playerAnim.runtimeAnimatorController = playerCtrl;

            var groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = playerObj.transform;
            groundCheckObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            var dropPointObj = new GameObject("DropPoint");
            dropPointObj.transform.parent = playerObj.transform;
            dropPointObj.transform.localPosition = new Vector3(0.5f, 0.2f, 0f);

            var movement = playerObj.AddComponent<PlayerMovement2D>();
            var ability = playerObj.AddComponent<SlimeTPAbility>();
            var health = playerObj.AddComponent<PlayerHealth>();
            var controller = playerObj.AddComponent<TilcayoController>();
            playerObj.AddComponent<AudioSource>();

            // Configurar campos serializados mediante SerializedObject
            SerializedObject soMove = new SerializedObject(movement);
            soMove.FindProperty("groundCheck").objectReferenceValue = groundCheckObj.transform;
            soMove.FindProperty("groundLayerMask").intValue = LayerMask.GetMask("Default", "Ground", "PassableGrates");
            soMove.ApplyModifiedProperties();

            SerializedObject soAbility = new SerializedObject(ability);
            soAbility.FindProperty("slimePrefab").objectReferenceValue = slimePrefab;
            soAbility.FindProperty("dropPoint").objectReferenceValue = dropPointObj.transform;
            soAbility.ApplyModifiedProperties();

            string playerPrefabPath = $"{PREFABS_PATH}/Tilcayo.prefab";
            PrefabUtility.SaveAsPrefabAsset(playerObj, playerPrefabPath);
            GameObject.DestroyImmediate(playerObj);

            // 3. Prefab Calavery Virtual Enemy
            GameObject calaveraObj = new GameObject("Enemy_Calavery");
            calaveraObj.layer = LayerMask.NameToLayer("Enemy");
            var calaveraSR = calaveraObj.AddComponent<SpriteRenderer>();
            calaveraSR.sortingLayerName = "Characters";
            var calaveraSprites = LoadSprites($"{SPRITES_PATH}/Enemies/Calavera Enemy Walk.png");
            if (calaveraSprites != null && calaveraSprites.Length > 0) calaveraSR.sprite = calaveraSprites[0];

            var calaveraRb = calaveraObj.AddComponent<Rigidbody2D>();
            calaveraRb.freezeRotation = true;
            calaveraRb.gravityScale = 2.5f;

            var calaveraCol = calaveraObj.AddComponent<CircleCollider2D>();
            calaveraCol.radius = 0.55f;
            calaveraCol.offset = new Vector2(0f, 0.65f);

            var calaveraAnim = calaveraObj.AddComponent<Animator>();
            var calCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ANIMS_PATH}/Calavera_AnimatorController.controller");
            if (calCtrl != null) calaveraAnim.runtimeAnimatorController = calCtrl;

            calaveraObj.AddComponent<PatrolEnemy2D>();
            PrefabUtility.SaveAsPrefabAsset(calaveraObj, $"{PREFABS_PATH}/Enemy_Calavery.prefab");
            GameObject.DestroyImmediate(calaveraObj);

            // 4. Prefab CalabazaFinalBoss
            GameObject bossObj = new GameObject("Boss_BugOLantern");
            bossObj.layer = LayerMask.NameToLayer("Enemy");
            var bossSR = bossObj.AddComponent<SpriteRenderer>();
            bossSR.sortingLayerName = "Characters";
            var bossSprites = LoadSprites($"{SPRITES_PATH}/Enemies/Final Boss Walk.png");
            if (bossSprites != null && bossSprites.Length > 0) bossSR.sprite = bossSprites[0];

            var bossRb = bossObj.AddComponent<Rigidbody2D>();
            bossRb.freezeRotation = true;
            bossRb.gravityScale = 3f;

            var bossCol = bossObj.AddComponent<BoxCollider2D>();
            bossCol.size = new Vector2(1.5f, 1.8f);
            bossCol.offset = new Vector2(0f, 0.9f);

            var bossAnim = bossObj.AddComponent<Animator>();
            var bCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ANIMS_PATH}/Boss_AnimatorController.controller");
            if (bCtrl != null) bossAnim.runtimeAnimatorController = bCtrl;

            var bossScript = bossObj.AddComponent<PatrolEnemy2D>();
            SerializedObject soBoss = new SerializedObject(bossScript);
            soBoss.FindProperty("isBoss").boolValue = true;
            soBoss.FindProperty("maxHealth").intValue = 3;
            soBoss.FindProperty("patrolDistance").floatValue = 6f;
            soBoss.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(bossObj, $"{PREFABS_PATH}/Boss_BugOLantern.prefab");
            GameObject.DestroyImmediate(bossObj);

            // 5. Prefab Terminal Checkpoint
            GameObject termObj = new GameObject("Terminal_Checkpoint");
            termObj.layer = LayerMask.NameToLayer("Checkpoint");
            var termSR = termObj.AddComponent<SpriteRenderer>();
            termSR.sortingLayerName = "Environment";
            var env4Sprites = LoadSprites($"{SPRITES_PATH}/Environment/Hoja de Recursos 4.png");
            if (env4Sprites != null && env4Sprites.Length > 7) termSR.sprite = env4Sprites[7]; // Env_Hoja4_07

            var termCol = termObj.AddComponent<BoxCollider2D>();
            termCol.isTrigger = true;
            termCol.size = new Vector2(2f, 2.05f);

            termObj.AddComponent<TerminalCheckpoint>();
            PrefabUtility.SaveAsPrefabAsset(termObj, $"{PREFABS_PATH}/Terminal_Checkpoint.prefab");
            GameObject.DestroyImmediate(termObj);

            // 6. Prefab Hazard Saw (Sierra)
            GameObject sawObj = new GameObject("Hazard_Saw");
            sawObj.layer = LayerMask.NameToLayer("Hazards");
            var sawSR = sawObj.AddComponent<SpriteRenderer>();
            sawSR.sortingLayerName = "Traps";
            if (env4Sprites != null && env4Sprites.Length > 4) sawSR.sprite = env4Sprites[4]; // Env_Hoja4_04

            var sawCol = sawObj.AddComponent<CircleCollider2D>();
            sawCol.isTrigger = true;
            sawCol.radius = 1.7f;

            var sawScript = sawObj.AddComponent<Hazard2D>();
            SerializedObject soSaw = new SerializedObject(sawScript);
            soSaw.FindProperty("rotateContinuously").boolValue = true;
            soSaw.FindProperty("rotationSpeed").floatValue = 400f;
            soSaw.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(sawObj, $"{PREFABS_PATH}/Hazard_Saw.prefab");
            GameObject.DestroyImmediate(sawObj);

            // 7. Prefab Passable Grate (Reja pasable para baba)
            GameObject grateObj = new GameObject("Passable_Grate");
            grateObj.layer = LayerMask.NameToLayer("PassableGrates");
            var grateSR = grateObj.AddComponent<SpriteRenderer>();
            grateSR.sortingLayerName = "Environment";
            var env1Sprites = LoadSprites($"{SPRITES_PATH}/Environment/Hoja de Recursos 1.png");
            if (env1Sprites != null && env1Sprites.Length > 2) grateSR.sprite = env1Sprites[2];

            var grateCol = grateObj.AddComponent<BoxCollider2D>();
            grateCol.size = new Vector2(0.5f, 2.5f);

            PrefabUtility.SaveAsPrefabAsset(grateObj, $"{PREFABS_PATH}/Passable_Grate.prefab");
            GameObject.DestroyImmediate(grateObj);

            // 8. Prefab Collectible Disk
            GameObject diskObj = new GameObject("Collectible_Disk");
            diskObj.layer = LayerMask.NameToLayer("Collectible");
            var diskSR = diskObj.AddComponent<SpriteRenderer>();
            diskSR.sortingLayerName = "Environment";
            var env3Sprites = LoadSprites($"{SPRITES_PATH}/Environment/Hoja de Recursos 3.png");
            if (env3Sprites != null && env3Sprites.Length > 6) diskSR.sprite = env3Sprites[6]; // Env_Hoja3_06

            var diskCol = diskObj.AddComponent<CircleCollider2D>();
            diskCol.isTrigger = true;
            diskCol.radius = 0.8f;

            diskObj.AddComponent<CollectibleDisk>();
            PrefabUtility.SaveAsPrefabAsset(diskObj, $"{PREFABS_PATH}/Collectible_Disk.prefab");
            GameObject.DestroyImmediate(diskObj);

            // 9. Prefab Level Gate (Portal de Salida)
            GameObject gateObj = new GameObject("Level_Gate");
            var gateSR = gateObj.AddComponent<SpriteRenderer>();
            gateSR.sortingLayerName = "Environment";
            if (env3Sprites != null && env3Sprites.Length > 14) gateSR.sprite = env3Sprites[14]; // Env_Hoja3_14

            var gateCol = gateObj.AddComponent<BoxCollider2D>();
            gateCol.isTrigger = true;
            gateCol.size = new Vector2(5.01f, 3.27f);

            gateObj.AddComponent<LevelGate>();
            PrefabUtility.SaveAsPrefabAsset(gateObj, $"{PREFABS_PATH}/Level_Gate.prefab");
            GameObject.DestroyImmediate(gateObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tilcayo Setup] Todos los Prefabs fueron generados con éxito.");
        }

        [MenuItem("Tilcayo/4.5 Generar Prefabs de Environment (Hojas 1..4 y Fondos)", false, 45)]
        public static void GenerateEnvironmentPrefabs()
        {
            string envPrefabsRoot = $"{PREFABS_PATH}/Environment";
            if (!Directory.Exists(envPrefabsRoot)) Directory.CreateDirectory(envPrefabsRoot);

            int totalCreated = 0;
            for (int sheet = 1; sheet <= 4; sheet++)
            {
                string sheetFolder = $"{envPrefabsRoot}/Hoja_{sheet}";
                if (!Directory.Exists(sheetFolder)) Directory.CreateDirectory(sheetFolder);

                string sheetSpritePath = $"{SPRITES_PATH}/Environment/Hoja de Recursos {sheet}.png";
                var sprites = LoadSprites(sheetSpritePath);
                if (sprites == null || sprites.Length == 0) continue;

                for (int i = 0; i < sprites.Length; i++)
                {
                    var sp = sprites[i];
                    if (sp == null) continue;

                    string prefabName = $"Env_Hoja{sheet}_{i:D2}";
                    GameObject go = new GameObject(prefabName);
                    go.layer = LayerMask.NameToLayer("Ground");

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sp;
                    sr.sortingLayerName = "Environment";

                    var col = go.AddComponent<BoxCollider2D>();
                    Vector2 spSize = sp.rect.size / sp.pixelsPerUnit;
                    Vector2 pivotRel = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
                    col.size = spSize;
                    col.offset = new Vector2((0.5f - pivotRel.x) * spSize.x, (0.5f - pivotRel.y) * spSize.y);

                    string savePath = $"{sheetFolder}/{prefabName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(go, savePath);
                    GameObject.DestroyImmediate(go);
                    totalCreated++;
                }
            }

            // Fondos de escenario
            string bgFolder = $"{envPrefabsRoot}/Backgrounds";
            if (!Directory.Exists(bgFolder)) Directory.CreateDirectory(bgFolder);
            string[] bgNames = new string[] { "Stage 1", "Stage 2", "Stage Alternativo", "Stage Final" };
            foreach (var bgName in bgNames)
            {
                var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Backgrounds/{bgName}.png");
                if (bgSprite != null)
                {
                    string safeName = "Env_Bg_" + bgName.Replace(" ", "_");
                    GameObject go = new GameObject(safeName);
                    go.layer = 0;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.sortingLayerName = "Background";

                    PrefabUtility.SaveAsPrefabAsset(go, $"{bgFolder}/{safeName}.prefab");
                    GameObject.DestroyImmediate(go);
                    totalCreated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00FF88><b>[Tilcayo Setup] ¡{totalCreated} Prefabs de Environment y Fondos generados con éxito!</b></color>");
        }

        [MenuItem("Tilcayo/5. Construir Escenas de Niveles y Menú", false, 50)]
        public static void CreateLevelScenes()
        {
            if (!Directory.Exists(SCENES_PATH)) Directory.CreateDirectory(SCENES_PATH);

            BuildMainMenuScene($"{SCENES_PATH}/Scene_MainMenu.unity");
            BuildLevelScene($"{SCENES_PATH}/Scene_Level_01.unity", 1, "Scene_Level_02");
            BuildLevelScene($"{SCENES_PATH}/Scene_Level_02.unity", 2, "Scene_Level_03");
            BuildLevelScene($"{SCENES_PATH}/Scene_Level_03.unity", 3, "");

            // Registrar escenas en Build Settings
            var sceneList = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{SCENES_PATH}/Scene_MainMenu.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Scene_Level_01.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Scene_Level_02.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/Scene_Level_03.unity", true)
            };
            EditorBuildSettings.scenes = sceneList;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tilcayo Setup] 4 Escenas generadas y agregadas a EditorBuildSettings.");
        }

        private static void BuildMainMenuScene(string scenePath)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single
            );

            // Cámara
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<UnityEngine.Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();

            // Fondo del Menú
            GameObject bgObj = new GameObject("Menu_Background");
            var bgSR = bgObj.AddComponent<SpriteRenderer>();
            bgSR.sortingLayerName = "Background";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/UI/Menu Principal.png");
            if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/UI/Promocional Tilcayo.png");
            bgSR.sprite = bgSprite;
            bgObj.transform.position = new Vector3(0f, 0f, 0f);

            // Title Logo
            GameObject logoObj = new GameObject("Title_Logo");
            var logoSR = logoObj.AddComponent<SpriteRenderer>();
            logoSR.sortingLayerName = "UI";
            logoSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/UI/Logo Tilcayo.png");
            logoObj.transform.position = new Vector3(-3.2f, 2.0f, 0f);
            logoObj.transform.localScale = Vector3.one * 0.45f;

            // Core Managers
            CreateCoreManagers();

            // UI Canvas del menú
            CreateMainMenuCanvas();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void BuildLevelScene(string scenePath, int levelIndex, string nextScene)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single
            );

            // 1. Cámara con seguimiento
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<UnityEngine.Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            camObj.transform.position = new Vector3(0f, 1.5f, -10f);
            camObj.AddComponent<AudioListener>();
            var camFollow = camObj.AddComponent<CameraFollow2D>();
            SerializedObject soCam = new SerializedObject(camFollow);
            soCam.FindProperty("autoDetectBackground").boolValue = false;
            soCam.FindProperty("clampVertical").boolValue = true;
            soCam.FindProperty("minCameraY").floatValue = 1.5f;
            soCam.FindProperty("maxCameraY").floatValue = 3.5f;
            soCam.ApplyModifiedProperties();

            // 2. Fondo del Nivel (Parallax / Panorama con Z=0 alineado a la vista 2D)
            string bgPath = levelIndex == 1 ? $"{SPRITES_PATH}/Backgrounds/Stage 1.png" :
                            levelIndex == 2 ? $"{SPRITES_PATH}/Backgrounds/Stage 2.png" :
                                              $"{SPRITES_PATH}/Backgrounds/Stage Final.png";

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            for (int i = 0; i < 4; i++)
            {
                GameObject bgPart = new GameObject($"Background_Panel_{i}");
                var sr = bgPart.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Background";
                sr.sprite = bgSprite;
                bgPart.transform.position = new Vector3(i * 21.72f, 3.5f, 0f);
            }

            // 3. Suelos y Plataformas
            GameObject groundRoot = new GameObject("Ground_Environment");
            groundRoot.layer = LayerMask.NameToLayer("Ground");

            // Suelo principal continuo (Línea plana limpia con superficie exacta en Y=0)
            CreatePlatform(groundRoot, new Vector2(25f, -0.72f), new Vector2(70f, 2f));

            if (levelIndex >= 2)
            {
                // Plataformas elevadas solo para niveles 2 y 3
                CreatePlatform(groundRoot, new Vector2(8f, 2f), new Vector2(5f, 0.8f));
                CreatePlatform(groundRoot, new Vector2(16f, 3.5f), new Vector2(5f, 0.8f));
                CreatePlatform(groundRoot, new Vector2(26f, 2.5f), new Vector2(6f, 0.8f));
                CreatePlatform(groundRoot, new Vector2(36f, 4.0f), new Vector2(7f, 0.8f));
                CreatePlatform(groundRoot, new Vector2(48f, 2.0f), new Vector2(6f, 0.8f));

                // Techo de bloqueo sobre rejas para forzar teletransporte por slime
                CreatePlatform(groundRoot, new Vector2(12f, 4.5f), new Vector2(2f, 4f));
                CreatePlatform(groundRoot, new Vector2(31f, 5.0f), new Vector2(2f, 4f));

                // 4. Rejas pasables (Passable Grates)
                GameObject gratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Passable_Grate.prefab");
                if (gratePrefab != null)
                {
                    SpawnPrefab(gratePrefab, new Vector3(12f, 1.25f, 0f));
                    SpawnPrefab(gratePrefab, new Vector3(31f, 1.25f, 0f));
                }
            }

            // 5. Checkpoints
            if (levelIndex >= 2)
            {
                GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Terminal_Checkpoint.prefab");
                if (cpPrefab != null)
                {
                    SpawnPrefab(cpPrefab, new Vector3(18f, 0.6f, 0f));
                    SpawnPrefab(cpPrefab, new Vector3(38f, 0.6f, 0f));
                }
            }

            // 6. Trampas (Hazards)
            GameObject sawPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Hazard_Saw.prefab");
            if (sawPrefab != null && levelIndex >= 2)
            {
                SpawnPrefab(sawPrefab, new Vector3(21f, 0.8f, 0f));
                SpawnPrefab(sawPrefab, new Vector3(33f, 1.5f, 0f));
            }

            // 7. Enemigos
            if (levelIndex >= 2)
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Enemy_Calavery.prefab");
                if (enemyPrefab != null)
                {
                    SpawnPrefab(enemyPrefab, new Vector3(26f, 3.2f, 0f));
                }
            }

            if (levelIndex == 3)
            {
                GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Boss_BugOLantern.prefab");
                if (bossPrefab != null)
                {
                    SpawnPrefab(bossPrefab, new Vector3(50f, 1f, 0f));
                }
            }

            // 8. Disquetes coleccionables
            if (levelIndex >= 2)
            {
                GameObject diskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Collectible_Disk.prefab");
                if (diskPrefab != null)
                {
                    SpawnPrefab(diskPrefab, new Vector3(8f, 3.2f, 0f));
                    SpawnPrefab(diskPrefab, new Vector3(16f, 4.7f, 0f));
                    SpawnPrefab(diskPrefab, new Vector3(36f, 5.2f, 0f));
                }
            }

            // 9. Portón de salida
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Level_Gate.prefab");
            if (gatePrefab != null)
            {
                var gateInstance = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
                gateInstance.transform.position = new Vector3(56f, 1.0f, 0f);
                var gateScript = gateInstance.GetComponent<LevelGate>();
                SerializedObject soGate = new SerializedObject(gateScript);
                soGate.FindProperty("nextSceneName").stringValue = nextScene;
                soGate.FindProperty("requiresBossDefeat").boolValue = (levelIndex == 3);
                soGate.FindProperty("isFinalGameExit").boolValue = (levelIndex == 3);
                soGate.FindProperty("requiredDisks").intValue = (levelIndex == 3 ? 0 : 3);
                soGate.ApplyModifiedProperties();
            }

            // 10. Jugador (Tilcayo)
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Tilcayo.prefab");
            if (playerPrefab != null)
            {
                var playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInst.transform.position = new Vector3(0f, 1f, 0f);
                camFollow.SetTarget(playerInst.transform);
            }

            // 11. Core Managers & In-Game UI
            CreateCoreManagers();
            CreateInGameHUDCanvas();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreatePlatform(GameObject root, Vector2 pos, Vector2 size)
        {
            GameObject plat = new GameObject("Platform");
            plat.transform.parent = root.transform;
            plat.transform.position = pos;
            plat.layer = LayerMask.NameToLayer("Ground");

            // Alineación milimétrica de colisión con la superficie dibujada (el tile tiene 28px de vacío en la parte superior a PPU 100 = 0.28m)
            var col = plat.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x, size.y - 0.28f);
            col.offset = new Vector2(0f, -0.14f);

            // Visual simple con SpriteRenderer de bloque
            var sr = plat.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            var envSprites = LoadSprites($"{SPRITES_PATH}/Environment/Hoja de Recursos 1.png");
            if (envSprites != null && envSprites.Length > 0)
            {
                sr.sprite = envSprites[0];
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
            }
        }

        private static GameObject SpawnPrefab(GameObject prefab, Vector3 pos)
        {
            if (prefab == null) return null;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            return inst;
        }

        private static void CreateCoreManagers()
        {
            GameObject managersObj = new GameObject("[Managers]");
            managersObj.AddComponent<GameManager>();
            managersObj.AddComponent<SoundManager>();
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static void CreateMainMenuCanvas()
        {
            EnsureEventSystem();

            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var b1Sprites = LoadSprites($"{SPRITES_PATH}/UI/Botones 1.png");
            Sprite spPlay = FindSpriteByName(b1Sprites, "Botones 1_0");
            Sprite spQuit = FindSpriteByName(b1Sprites, "Botones 1_4");

            // Botón Iniciar Juego (Botones 1_0)
            GameObject btnPlayObj = new GameObject("Btn_IniciarJuego");
            btnPlayObj.transform.SetParent(canvasObj.transform, false);
            btnPlayObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var btnPlayImage = btnPlayObj.AddComponent<Image>();
            if (spPlay != null) btnPlayImage.sprite = spPlay;
            btnPlayImage.color = Color.white;
            btnPlayImage.preserveAspect = true;
            var btnPlay = btnPlayObj.AddComponent<Button>();

            RectTransform rtPlay = btnPlayObj.GetComponent<RectTransform>();
            rtPlay.anchoredPosition = new Vector2(-705f, -160f);
            rtPlay.sizeDelta = new Vector2(300f, 105f);

            // Botón Salir del Juego (Botones 1_4)
            GameObject btnQuitObj = new GameObject("Btn_SalirJuego");
            btnQuitObj.transform.SetParent(canvasObj.transform, false);
            btnQuitObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var btnQuitImage = btnQuitObj.AddComponent<Image>();
            if (spQuit != null) btnQuitImage.sprite = spQuit;
            btnQuitImage.color = Color.white;
            btnQuitImage.preserveAspect = true;
            var btnQuit = btnQuitObj.AddComponent<Button>();

            RectTransform rtQuit = btnQuitObj.GetComponent<RectTransform>();
            rtQuit.anchoredPosition = new Vector2(-205f, -160f);
            rtQuit.sizeDelta = new Vector2(300f, 102f);

            // Panel de Cómic de Introducción (Fondo Negro a pantalla completa)
            GameObject comicPanel = new GameObject("Panel_ComicIntro");
            comicPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtComic = comicPanel.AddComponent<RectTransform>();
            rtComic.anchorMin = Vector2.zero;
            rtComic.anchorMax = Vector2.one;
            rtComic.offsetMin = Vector2.zero;
            rtComic.offsetMax = Vector2.zero;
            var comicBg = comicPanel.AddComponent<Image>();
            comicBg.color = Color.black;
            comicBg.raycastTarget = true;

            var comic1Sprites = LoadSprites($"{SPRITES_PATH}/Comic/Comic 1 Tilcayo.png");
            var comic2Sprites = LoadSprites($"{SPRITES_PATH}/Comic/Comic 2 Tilcayo.png");
            Sprite spComic1 = (comic1Sprites != null && comic1Sprites.Length > 0) ? comic1Sprites[0] : null;
            Sprite spComic2 = (comic2Sprites != null && comic2Sprites.Length > 0) ? comic2Sprites[0] : null;

            // Cómic 1 (Mitad Izquierda)
            GameObject c1Obj = new GameObject("Comic_Image_1");
            c1Obj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtC1 = c1Obj.AddComponent<RectTransform>();
            rtC1.anchorMin = new Vector2(0f, 0f);
            rtC1.anchorMax = new Vector2(0.5f, 1f);
            rtC1.anchoredPosition = Vector2.zero;
            rtC1.sizeDelta = new Vector2(-80f, -80f);
            var imgC1 = c1Obj.AddComponent<Image>();
            if (spComic1 != null) imgC1.sprite = spComic1;
            imgC1.preserveAspect = true;
            imgC1.color = Color.white;
            imgC1.raycastTarget = false;

            // Cómic 2 (Mitad Derecha)
            GameObject c2Obj = new GameObject("Comic_Image_2");
            c2Obj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtC2 = c2Obj.AddComponent<RectTransform>();
            rtC2.anchorMin = new Vector2(0.5f, 0f);
            rtC2.anchorMax = new Vector2(1f, 1f);
            rtC2.anchoredPosition = Vector2.zero;
            rtC2.sizeDelta = new Vector2(-80f, -80f);
            var imgC2 = c2Obj.AddComponent<Image>();
            if (spComic2 != null) imgC2.sprite = spComic2;
            imgC2.preserveAspect = true;
            imgC2.color = Color.white;
            imgC2.raycastTarget = false;

            // Texto de indicación para continuar
            GameObject promptObj = new GameObject("Text_ContinuePrompt");
            promptObj.transform.SetParent(comicPanel.transform, false);
            RectTransform rtPrompt = promptObj.AddComponent<RectTransform>();
            rtPrompt.anchorMin = new Vector2(0.5f, 0f);
            rtPrompt.anchorMax = new Vector2(0.5f, 0f);
            rtPrompt.anchoredPosition = new Vector2(0f, 40f);
            rtPrompt.sizeDelta = new Vector2(800f, 45f);
            var txtPrompt = promptObj.AddComponent<Text>();
            txtPrompt.text = "[ Haz clic o presiona ESPACIO para continuar ]";
            txtPrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPrompt.fontSize = 22;
            txtPrompt.alignment = TextAnchor.LowerCenter;
            txtPrompt.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
            txtPrompt.raycastTarget = false;

            comicPanel.SetActive(false);

            // Controlador de Menú Principal
            var menuCtrl = canvasObj.AddComponent<MainMenuController>();
            SerializedObject soCtrl = new SerializedObject(menuCtrl);
            soCtrl.FindProperty("playButton").objectReferenceValue = btnPlay;
            soCtrl.FindProperty("quitButton").objectReferenceValue = btnQuit;
            soCtrl.FindProperty("comicPanel").objectReferenceValue = comicPanel;
            soCtrl.FindProperty("comicImage1").objectReferenceValue = imgC1;
            soCtrl.FindProperty("comicImage2").objectReferenceValue = imgC2;
            soCtrl.FindProperty("promptText").objectReferenceValue = txtPrompt;
            soCtrl.FindProperty("comic1Sprite").objectReferenceValue = spComic1;
            soCtrl.FindProperty("comic2Sprite").objectReferenceValue = spComic2;
            soCtrl.FindProperty("waitBeforeComic2").floatValue = 4.0f;
            soCtrl.FindProperty("comic1DimBrightness").floatValue = 0.32f;
            soCtrl.FindProperty("waitAfterComic2").floatValue = 5.0f;
            soCtrl.FindProperty("nextSceneName").stringValue = "Scene_Level_01";
            soCtrl.ApplyModifiedProperties();
        }

        private static void CreateInGameHUDCanvas()
        {
            EnsureEventSystem();

            GameObject canvasObj = new GameObject("HUD_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var uiManager = canvasObj.AddComponent<UIManager>();

            // Cargar sprites
            var b1Sprites = LoadSprites($"{SPRITES_PATH}/UI/Botones 1.png");
            var b2Sprites = LoadSprites($"{SPRITES_PATH}/UI/Botones 2.png");

            Sprite spPause = FindSpriteByName(b1Sprites, "Botones 1_5");
            Sprite spTP = FindSpriteByName(b1Sprites, "Botones 1_6");
            Sprite spRestart = FindSpriteByName(b1Sprites, "Botones 1_7");
            Sprite spMute = FindSpriteByName(b1Sprites, "Botones 1_8");

            Sprite spCatLife = FindSpriteByName(b2Sprites, "Botones 2_0");
            Sprite spCatDefeated = FindSpriteByName(b2Sprites, "Botones 2_2");
            Sprite spAbility = FindSpriteByName(b2Sprites, "Botones 2_3");
            Sprite spDisk = FindSpriteByName(b2Sprites, "Botones 2_4");
            Sprite spVictory = FindSpriteByName(b2Sprites, "Botones 2_8");

            // Barra superior izquierda
            GameObject hudBar = new GameObject("HUD_TopLeft_Bar");
            hudBar.transform.SetParent(canvasObj.transform, false);
            RectTransform rtBar = hudBar.AddComponent<RectTransform>();
            rtBar.anchorMin = new Vector2(0f, 1f);
            rtBar.anchorMax = new Vector2(0f, 1f);
            rtBar.pivot = new Vector2(0f, 1f);
            rtBar.anchoredPosition = new Vector2(25f, -25f);
            rtBar.sizeDelta = new Vector2(700f, 70f);

            // 1. Botón Pausa (Botones 1_5)
            GameObject btnPauseObj = new GameObject("Btn_Pause_Botones1_5");
            btnPauseObj.transform.SetParent(hudBar.transform, false);
            btnPauseObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgPause = btnPauseObj.AddComponent<Image>();
            if (spPause != null) imgPause.sprite = spPause;
            imgPause.color = Color.white;
            imgPause.preserveAspect = true;
            var btnPause = btnPauseObj.AddComponent<Button>();
            RectTransform rtPause = btnPauseObj.GetComponent<RectTransform>();
            rtPause.anchoredPosition = new Vector2(-280f, -25f);
            rtPause.sizeDelta = new Vector2(70f, 70f);

            // 2. Botón Reiniciar Nivel (Botones 1_7)
            GameObject btnRestartObj = new GameObject("Btn_Restart_Botones1_7");
            btnRestartObj.transform.SetParent(hudBar.transform, false);
            btnRestartObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgRestart = btnRestartObj.AddComponent<Image>();
            if (spRestart != null) imgRestart.sprite = spRestart;
            imgRestart.color = Color.white;
            imgRestart.preserveAspect = true;
            var btnRestart = btnRestartObj.AddComponent<Button>();
            RectTransform rtRestart = btnRestartObj.GetComponent<RectTransform>();
            rtRestart.anchoredPosition = new Vector2(-124f, -25f);
            rtRestart.sizeDelta = new Vector2(70f, 70f);

            // 3. Botón Silenciar (Botones 1_8)
            GameObject btnMuteObj = new GameObject("Btn_Mute_Botones1_8");
            btnMuteObj.transform.SetParent(hudBar.transform, false);
            btnMuteObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgMute = btnMuteObj.AddComponent<Image>();
            if (spMute != null) imgMute.sprite = spMute;
            imgMute.color = Color.white;
            imgMute.preserveAspect = true;
            var btnMute = btnMuteObj.AddComponent<Button>();
            RectTransform rtMute = btnMuteObj.GetComponent<RectTransform>();
            rtMute.anchoredPosition = new Vector2(30f, -25f);
            rtMute.sizeDelta = new Vector2(70f, 70f);

            // 3.5. Botón Salir a Menú (Botones 1_4)
            Sprite spExit = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Botones 1_4.png");
            GameObject btnExitObj = new GameObject("Btn_SalirMenu_Botones1_4");
            btnExitObj.transform.SetParent(hudBar.transform, false);
            btnExitObj.transform.localScale = new Vector3(0.3f, 0.3f, 0f);
            var imgExit = btnExitObj.AddComponent<Image>();
            if (spExit != null) imgExit.sprite = spExit;
            imgExit.color = Color.white;
            imgExit.preserveAspect = true;
            var btnExit = btnExitObj.AddComponent<Button>();
            RectTransform rtExit = btnExitObj.GetComponent<RectTransform>();
            rtExit.anchoredPosition = new Vector2(232f, -25f);
            rtExit.sizeDelta = new Vector2(783f, 265f);
            btnExitObj.SetActive(false);

            // 4. 3 Vidas de Tilcayo (Botones 2_0)
            float[] lifeXPositions = new float[] { 459.6f, 637.3f, 811.9f };
            Image[] lifeImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                string lifeName = i == 0 ? "Life_Botones2_0_0 (2)" : (i == 1 ? "Life_Botones2_0_0 (1)" : "Life_Botones2_0_0");
                GameObject lifeObj = new GameObject(lifeName);
                lifeObj.transform.SetParent(hudBar.transform, false);
                lifeObj.transform.localScale = new Vector3(2f, 2f, 1f);
                var imgLife = lifeObj.AddComponent<Image>();
                if (spCatLife != null) imgLife.sprite = spCatLife;
                imgLife.color = Color.white;
                imgLife.preserveAspect = true;
                RectTransform rtLife = lifeObj.GetComponent<RectTransform>();
                rtLife.anchoredPosition = new Vector2(lifeXPositions[i], -10f);
                rtLife.sizeDelta = new Vector2(90f, 90f);
                lifeImages[i] = imgLife;
            }

            // 5. Indicador de Habilidad Disponible (Botones 2_3)
            GameObject abilityObj = new GameObject("Icon_Ability_Botones2_3");
            abilityObj.transform.SetParent(hudBar.transform, false);
            abilityObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgAbility = abilityObj.AddComponent<Image>();
            if (spAbility != null) imgAbility.sprite = spAbility;
            imgAbility.color = Color.white;
            imgAbility.preserveAspect = true;
            RectTransform rtAbility = abilityObj.GetComponent<RectTransform>();
            rtAbility.anchoredPosition = new Vector2(1013.6f, -25f);
            rtAbility.sizeDelta = new Vector2(70f, 70f);

            // 6. Botón TP Disponible con E (Botones 1_6)
            GameObject btnTPObj = new GameObject("Btn_TP_Botones1_6");
            btnTPObj.transform.SetParent(hudBar.transform, false);
            btnTPObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgTP = btnTPObj.AddComponent<Image>();
            if (spTP != null) imgTP.sprite = spTP;
            imgTP.color = Color.white;
            imgTP.preserveAspect = true;
            var btnTP = btnTPObj.AddComponent<Button>();
            RectTransform rtTP = btnTPObj.GetComponent<RectTransform>();
            rtTP.anchoredPosition = new Vector2(1185f, -30.1f);
            rtTP.sizeDelta = new Vector2(70f, 70f);
            btnTPObj.SetActive(false); // Solo aparece cuando el TP puede usarse

            // 7. Contador de Disquetes coleccionables (Botones 2_4)
            GameObject diskObj = new GameObject("Icon_Disk_Botones2_4");
            diskObj.transform.SetParent(hudBar.transform, false);
            diskObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var imgDisk = diskObj.AddComponent<Image>();
            if (spDisk != null) imgDisk.sprite = spDisk;
            imgDisk.color = Color.white;
            imgDisk.preserveAspect = true;
            RectTransform rtDisk = diskObj.GetComponent<RectTransform>();
            rtDisk.anchoredPosition = new Vector2(1350f, -25f);
            rtDisk.sizeDelta = new Vector2(60f, 60f);

            GameObject txtDiskObj = new GameObject("Text_DiskCounter");
            txtDiskObj.transform.SetParent(hudBar.transform, false);
            txtDiskObj.transform.localScale = new Vector3(2f, 2f, 1f);
            var txtDisk = txtDiskObj.AddComponent<Text>();
            txtDisk.text = "00";
            txtDisk.alignment = TextAnchor.MiddleCenter;
            txtDisk.color = new Color(0.2f, 1f, 0.4f);
            txtDisk.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtDisk.fontSize = 24;
            txtDisk.fontStyle = FontStyle.Bold;
            RectTransform rtTxtDisk = txtDiskObj.GetComponent<RectTransform>();
            rtTxtDisk.anchoredPosition = new Vector2(1520f, -35f);
            rtTxtDisk.sizeDelta = new Vector2(100f, 100f);

            // Panel de Victoria (Con Botones 2_8)
            GameObject victoryPanel = new GameObject("Panel_Victory");
            victoryPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtVic = victoryPanel.AddComponent<RectTransform>();
            rtVic.anchorMin = Vector2.zero;
            rtVic.anchorMax = Vector2.one;
            rtVic.offsetMin = Vector2.zero;
            rtVic.offsetMax = Vector2.zero;
            var vicBg = victoryPanel.AddComponent<Image>();
            vicBg.color = new Color(0.04f, 0.04f, 0.08f, 0.90f);

            // Banner de Victoria (Botones 2_8 limpio)
            GameObject vicBannerObj = new GameObject("Banner_Victory_Botones2_8");
            vicBannerObj.transform.SetParent(victoryPanel.transform, false);
            var imgBanner = vicBannerObj.AddComponent<Image>();
            if (spVictory != null) imgBanner.sprite = spVictory;
            imgBanner.color = Color.white;
            imgBanner.preserveAspect = true;
            RectTransform rtBanner = vicBannerObj.GetComponent<RectTransform>();
            rtBanner.anchoredPosition = new Vector2(0f, 80f);
            rtBanner.sizeDelta = new Vector2(750f, 136f);

            GameObject vicTextObj = new GameObject("Text_Victory_Subtitle");
            vicTextObj.transform.SetParent(victoryPanel.transform, false);
            var vicText = vicTextObj.AddComponent<Text>();
            vicText.text = "¡PROYECTO ENTREGADO A TIEMPO!\nTilcayo Despertó de la Pesadilla";
            vicText.alignment = TextAnchor.MiddleCenter;
            vicText.color = new Color(0.3f, 1f, 0.6f);
            vicText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vicText.fontSize = 26;
            vicTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);
            vicTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 80f);

            // Botón Menú Principal en Victoria
            GameObject btnMenuVicObj = new GameObject("Btn_Victory_MainMenu");
            btnMenuVicObj.transform.SetParent(victoryPanel.transform, false);
            var imgMenuVic = btnMenuVicObj.AddComponent<Image>();
            if (spRestart != null) imgMenuVic.sprite = spRestart;
            imgMenuVic.color = Color.white;
            imgMenuVic.preserveAspect = true;
            var btnMenuVic = btnMenuVicObj.AddComponent<Button>();
            btnMenuVic.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_MainMenu");
            });
            RectTransform rtMenuVic = btnMenuVicObj.GetComponent<RectTransform>();
            rtMenuVic.anchoredPosition = new Vector2(0f, -130f);
            rtMenuVic.sizeDelta = new Vector2(60f, 60f);

            victoryPanel.SetActive(false);

            // Asignar en UIManager
            SerializedObject soUI = new SerializedObject(uiManager);
            soUI.FindProperty("pauseButton").objectReferenceValue = btnPause;
            soUI.FindProperty("pauseButtonImage").objectReferenceValue = imgPause;
            soUI.FindProperty("restartButton").objectReferenceValue = btnRestart;
            soUI.FindProperty("muteButton").objectReferenceValue = btnMute;
            soUI.FindProperty("muteButtonImage").objectReferenceValue = imgMute;
            soUI.FindProperty("exitMenuButton").objectReferenceValue = btnExit;
            soUI.FindProperty("abilityIcon").objectReferenceValue = imgAbility;
            soUI.FindProperty("tpPromptButton").objectReferenceValue = btnTP;
            soUI.FindProperty("activeCatSprite").objectReferenceValue = spCatLife;
            soUI.FindProperty("inactiveCatSprite").objectReferenceValue = spCatDefeated;
            soUI.FindProperty("diskCounterText").objectReferenceValue = txtDisk;
            soUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;

            SerializedProperty catArrayProp = soUI.FindProperty("catHealthIcons");
            catArrayProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                catArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = lifeImages[i];
            }
            soUI.ApplyModifiedProperties();
        }
    }
}
