using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using DeepEarth.UI;
using DeepEarth.Core;

namespace DeepEarth.Editor
{
    public static class SetupScenesEditor
    {
        private const string RootPath = "Assets/Game";
        public static bool SilentMode = true;

        [MenuItem("Tools/Setup Game Scenes and Flow")]
        public static void SetupGameScenes()
        {
            try
            {
                Debug.Log("Starting automated scene structure setup...");

                // 1. Create Scenes Folder
                string scenesFolder = $"{RootPath}/Scenes";
                if (!Directory.Exists(scenesFolder))
                {
                    Directory.CreateDirectory(scenesFolder);
                    AssetDatabase.ImportAsset(scenesFolder);
                }

                // 2. Relocate MainGameScene.unity if it exists in the old Core/Scenes location
                string oldScenePath = $"{RootPath}/Core/Scenes/MainGameScene.unity";
                string newScenePath = $"{scenesFolder}/MainGameScene.unity";

                if (File.Exists(oldScenePath))
                {
                    Debug.Log($"Relocating MainGameScene.unity to {newScenePath}");
                    string moveResult = AssetDatabase.MoveAsset(oldScenePath, newScenePath);
                    if (!string.IsNullOrEmpty(moveResult))
                    {
                        Debug.LogError($"Failed to move MainGameScene.unity: {moveResult}");
                    }
                    else
                    {
                        // Delete old Core/Scenes directory if empty
                        string oldScenesDir = $"{RootPath}/Core/Scenes";
                        if (Directory.Exists(oldScenesDir) && Directory.GetFiles(oldScenesDir).Length == 0)
                        {
                            Directory.Delete(oldScenesDir);
                            File.Delete(oldScenesDir + ".meta");
                        }
                    }
                }

                // 3. Create Loading Scene
                CreateLoadingScene($"{scenesFolder}/LoadingScene.unity");

                // 4. Create Start Menu Scene
                CreateStartMenuScene($"{scenesFolder}/StartMenuScene.unity");

                // 5. Update Build Settings
                RegisterBuildSettings(scenesFolder);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("Scene structure and flow configured successfully!");
                if (!SilentMode)
                {
                    EditorUtility.DisplayDialog("Setup Successful", "Scenes (LoadingScene, StartMenuScene, MainGameScene) created and configured successfully in Assets/Game/Scenes.\n\nLoadingScene has been registered as the start scene.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Scene setup failed: {ex.Message}\n{ex.StackTrace}");
                if (!SilentMode)
                {
                    EditorUtility.DisplayDialog("Setup Failed", $"An error occurred during scene setup:\n{ex.Message}", "OK");
                }
            }
        }

        private static void CreateLoadingScene(string path)
        {
            Debug.Log($"Creating LoadingScene at: {path}");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Add EventSystem in scene (required for UI interaction)
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create Canvas
            GameObject canvasGo = new GameObject("UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();

            // Background Image
            GameObject bgGo = CreateUIElement("Background", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.08f, 0.08f, 1f); // Dark background
            bgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Loading Title
            CreateUIText("TitleText", bgGo, "DEEP EARTH", 72, Color.white, new Vector2(0, 300));

            // Slider
            GameObject sliderGo = CreateUIElement("LoadingSlider", bgGo, new Vector2(0, -100), new Vector2(600, 40), new Vector2(0.5f, 0.5f));
            var slider = sliderGo.AddComponent<Slider>();

            // Slider Background
            GameObject sBgGo = CreateUIElement("Background", sliderGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var sBgImg = sBgGo.AddComponent<Image>();
            sBgImg.color = new Color(0.2f, 0.18f, 0.18f, 1f);
            sBgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sBgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            sBgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sBgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Slider Fill Area
            GameObject sFillArea = CreateUIElement("Fill Area", sliderGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            sFillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sFillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
            sFillArea.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sFillArea.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            GameObject sFillGo = CreateUIElement("Fill", sFillArea, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var sFillImg = sFillGo.AddComponent<Image>();
            sFillImg.color = new Color(1f, 0.5f, 0.25f, 1f); // Nice orange color
            sFillGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sFillGo.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f); // Start empty
            sFillGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sFillGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            slider.fillRect = sFillGo.GetComponent<RectTransform>();

            // Status Text
            GameObject statusTextGo = CreateUIText("StatusText", bgGo, "Initializing...", 36, Color.gray, new Vector2(0, -180));

            // Add Loading Controller GameObject
            GameObject controllerGo = new GameObject("LoadingSceneController");
            var controller = controllerGo.AddComponent<LoadingSceneController>();
            SetRef(controller, "progressSlider", slider);
            SetRef(controller, "progressText", statusTextGo.GetComponent<TextMeshProUGUI>());

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateStartMenuScene(string path)
        {
            Debug.Log($"Creating StartMenuScene at: {path}");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Add EventSystem in scene (required for UI interaction)
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create Canvas
            GameObject canvasGo = new GameObject("UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();

            // View script on Canvas
            var startMenuView = canvasGo.AddComponent<StartMenuUIView>();

            // Top-right Gold Display
            GameObject goldTextGo = CreateUIText("TopRightGoldText", canvasGo, "Gold: 0", 36, Color.yellow, new Vector2(-50, -50));
            var goldRt = goldTextGo.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(1, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(1, 1);
            goldRt.sizeDelta = new Vector2(400, 100);
            var goldTmp = goldTextGo.GetComponent<TextMeshProUGUI>();
            goldTmp.alignment = TextAlignmentOptions.Right;

            // Background Image
            GameObject bgGo = CreateUIElement("Background", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.08f, 0.08f, 1f);
            bgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // 1. Menu Panel
            GameObject menuPanelGo = CreateUIElement("MenuPanel", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            menuPanelGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            menuPanelGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            menuPanelGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            menuPanelGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            GameObject titleTextGo = CreateUIText("TitleText", menuPanelGo, "DEEP EARTH", 96, new Color(1f, 0.75f, 0.4f), new Vector2(0, 450));
            GameObject playBtnGo = CreateUIButton("PlayButton", menuPanelGo, "PLAY", new Vector2(0, 160), new Vector2(400, 90));
            GameObject charBtnGo = CreateUIButton("CharacterButton", menuPanelGo, "CHARACTER", new Vector2(0, 50), new Vector2(400, 90));
            GameObject upgradeBtnGo = CreateUIButton("UpgradeButton", menuPanelGo, "UPGRADES", new Vector2(0, -60), new Vector2(400, 90));
            GameObject shopBtnGo = CreateUIButton("ShopButton", menuPanelGo, "SHOP", new Vector2(0, -170), new Vector2(400, 90));
            GameObject settingsBtnGo = CreateUIButton("SettingsButton", menuPanelGo, "SETTINGS", new Vector2(0, -280), new Vector2(400, 90));

            // 2. Upgrade Panel
            GameObject upgradePanelGo = CreateUIElement("UpgradePanel", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            upgradePanelGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            upgradePanelGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            upgradePanelGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            upgradePanelGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            GameObject upgradeTitleGo = CreateUIText("UpgradeTitle", upgradePanelGo, "META UPGRADES", 60, Color.white, new Vector2(0, 450));
            GameObject willTextGo = CreateUIText("WillText", upgradePanelGo, "Will: 0", 36, Color.yellow, new Vector2(0, 360));

            // Upgrades Container
            GameObject shopContainer = CreateUIElement("ShopContainer", upgradePanelGo, new Vector2(0, 0), new Vector2(800, 420), new Vector2(0.5f, 0.5f));

            // Upgrade Row 1: Mining Power
            GameObject powerRow = CreateUIElement("PowerRow", shopContainer, new Vector2(0, 130), new Vector2(750, 100), new Vector2(0.5f, 0.5f));
            GameObject powerLvlGo = CreateUIText("Level", powerRow, "Mining Power: Lv.1", 32, Color.white, new Vector2(-150, 0));
            GameObject powerCostGo = CreateUIText("Cost", powerRow, "10 Will", 28, Color.yellow, new Vector2(100, 0));
            GameObject powerBtnGo = CreateUIButton("BuyButton", powerRow, "UPGRADE", new Vector2(280, 0), new Vector2(160, 60));

            // Upgrade Row 2: Max HP
            GameObject hpRow = CreateUIElement("HPRow", shopContainer, new Vector2(0, 10), new Vector2(750, 100), new Vector2(0.5f, 0.5f));
            GameObject hpLvlGo = CreateUIText("Level", hpRow, "Max HP: Lv.1", 32, Color.white, new Vector2(-150, 0));
            GameObject hpCostGo = CreateUIText("Cost", hpRow, "8 Will", 28, Color.yellow, new Vector2(100, 0));
            GameObject hpBtnGo = CreateUIButton("BuyButton", hpRow, "UPGRADE", new Vector2(280, 0), new Vector2(160, 60));

            // Upgrade Row 3: Bag Size
            GameObject bagRow = CreateUIElement("BagRow", shopContainer, new Vector2(0, -110), new Vector2(750, 100), new Vector2(0.5f, 0.5f));
            GameObject bagLvlGo = CreateUIText("Level", bagRow, "Bag Size: Lv.1", 32, Color.white, new Vector2(-150, 0));
            GameObject bagCostGo = CreateUIText("Cost", bagRow, "5 Will", 28, Color.yellow, new Vector2(100, 0));
            GameObject bagBtnGo = CreateUIButton("BuyButton", bagRow, "UPGRADE", new Vector2(280, 0), new Vector2(160, 60));

            // Upgrade Back Button
            GameObject upgradeBackBtnGo = CreateUIButton("BackButton", upgradePanelGo, "BACK", new Vector2(0, -380), new Vector2(300, 80));
            upgradePanelGo.SetActive(false);

            // 3. Settings Panel
            GameObject settingsPanelGo = CreateUIElement("SettingsPanel", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            settingsPanelGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            settingsPanelGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            settingsPanelGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            settingsPanelGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            GameObject settingsTitleGo = CreateUIText("SettingsTitle", settingsPanelGo, "SETTINGS", 60, Color.white, new Vector2(0, 400));
            GameObject settingsLangLabelGo = CreateUIText("LanguageLabel", settingsPanelGo, "Language", 36, Color.gray, new Vector2(0, 180));

            GameObject langContainer = CreateUIElement("LangContainer", settingsPanelGo, new Vector2(0, 80), new Vector2(600, 100), new Vector2(0.5f, 0.5f));
            GameObject langKoBtnGo = CreateUIButton("LanguageKoButton", langContainer, "한글", new Vector2(-150, 0), new Vector2(250, 80));
            GameObject langEnBtnGo = CreateUIButton("LanguageEnButton", langContainer, "English", new Vector2(150, 0), new Vector2(250, 80));

            GameObject settingsBackBtnGo = CreateUIButton("BackButton", settingsPanelGo, "BACK", new Vector2(0, -380), new Vector2(300, 80));
            settingsPanelGo.SetActive(false);

            // 4. Shop Panel
            GameObject shopPanelGo = CreateUIElement("ShopPanel", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            shopPanelGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            shopPanelGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            shopPanelGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            shopPanelGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            GameObject shopTitleGo = CreateUIText("ShopTitle", shopPanelGo, "SHOP", 60, Color.white, new Vector2(0, 300));
            GameObject shopDescGo = CreateUIText("ShopDescText", shopPanelGo, "SHOP COMING SOON!", 36, Color.yellow, new Vector2(0, 100));
            GameObject shopBackBtnGo = CreateUIButton("BackButton", shopPanelGo, "BACK", new Vector2(0, -380), new Vector2(300, 80));
            shopPanelGo.SetActive(false);

            // 5. Character Selection Popup Panel
            GameObject charPopupPanelGo = CreateUIElement("CharacterPopupPanel", canvasGo, Vector2.zero, new Vector2(900, 650), new Vector2(0.5f, 0.5f));
            var charPopupView = charPopupPanelGo.AddComponent<CharacterPopupView>();
            var popupImg = charPopupPanelGo.AddComponent<Image>();
            popupImg.color = new Color(0.12f, 0.12f, 0.15f, 0.98f);

            GameObject charPopupTitleGo = CreateUIText("TitleText", charPopupPanelGo, "CHARACTER SELECT", 44, new Color(1f, 0.75f, 0.4f), new Vector2(0, 260));
            
            // Left list of character buttons
            GameObject charListContainer = CreateUIElement("CharacterList", charPopupPanelGo, new Vector2(-250, 0), new Vector2(320, 420), new Vector2(0.5f, 0.5f));
            GameObject prisonerBtnGo = CreateUIButton("PrisonerButton", charListContainer, "Prisoner", new Vector2(0, 140), new Vector2(300, 75));
            GameObject mercenaryBtnGo = CreateUIButton("MercenaryButton", charListContainer, "Mercenary", new Vector2(0, 50), new Vector2(300, 75));
            GameObject minerBtnGo = CreateUIButton("MinerButton", charListContainer, "Miner", new Vector2(0, -40), new Vector2(300, 75));
            GameObject graveRobberBtnGo = CreateUIButton("GraveRobberButton", charListContainer, "Grave Robber", new Vector2(0, -130), new Vector2(300, 75));

            // Right character info panel
            GameObject infoContainer = CreateUIElement("CharacterInfo", charPopupPanelGo, new Vector2(180, 0), new Vector2(500, 420), new Vector2(0.5f, 0.5f));
            
            GameObject charNameGo = CreateUIText("NameText", infoContainer, "Character Name", 36, Color.white, new Vector2(0, 160));
            var nameTmp = charNameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.alignment = TextAlignmentOptions.Left;
            charNameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 60);

            GameObject charDescGo = CreateUIText("DescriptionText", infoContainer, "Description...", 24, Color.lightGray, new Vector2(0, 20));
            var descTmp = charDescGo.GetComponent<TextMeshProUGUI>();
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            charDescGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 200);

            GameObject charCostGo = CreateUIText("CostText", infoContainer, "Unlock Cost...", 24, Color.yellow, new Vector2(0, -110));
            var costTmp = charCostGo.GetComponent<TextMeshProUGUI>();
            costTmp.alignment = TextAlignmentOptions.Left;
            charCostGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 50);

            GameObject ownedResGo = CreateUIText("OwnedResourcesText", infoContainer, "Owned Resources...", 20, Color.gray, new Vector2(0, -170));
            var ownedTmp = ownedResGo.GetComponent<TextMeshProUGUI>();
            ownedTmp.alignment = TextAlignmentOptions.Left;
            ownedResGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 50);

            // Bottom action buttons
            GameObject unlockBtnGo = CreateUIButton("UnlockButton", charPopupPanelGo, "UNLOCK", new Vector2(0, -255), new Vector2(240, 65));
            GameObject selectBtnGo = CreateUIButton("SelectButton", charPopupPanelGo, "SELECT", new Vector2(0, -255), new Vector2(240, 65));
            GameObject closeBtnGo = CreateUIButton("CloseButton", charPopupPanelGo, "CLOSE", new Vector2(280, -255), new Vector2(160, 65));

            charPopupPanelGo.SetActive(false);

            // Bind view references in CharacterPopupView
            SetRef(charPopupView, "popupPanel", charPopupPanelGo);
            SetRef(charPopupView, "titleText", charPopupTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(charPopupView, "prisonerButton", prisonerBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "mercenaryButton", mercenaryBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "minerButton", minerBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "graveRobberButton", graveRobberBtnGo.GetComponent<Button>());

            SetRef(charPopupView, "charNameText", nameTmp);
            SetRef(charPopupView, "charDescText", descTmp);
            SetRef(charPopupView, "charCostText", costTmp);
            SetRef(charPopupView, "ownedResourcesText", ownedTmp);

            SetRef(charPopupView, "unlockButton", unlockBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "unlockButtonText", unlockBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(charPopupView, "selectButton", selectBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "selectButtonText", selectBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(charPopupView, "closeButton", closeBtnGo.GetComponent<Button>());
            SetRef(charPopupView, "closeButtonText", closeBtnGo.GetComponentInChildren<TextMeshProUGUI>());

            // Bind references
            SetRef(startMenuView, "menuPanel", menuPanelGo);
            SetRef(startMenuView, "upgradePanel", upgradePanelGo);
            SetRef(startMenuView, "settingsPanel", settingsPanelGo);
            SetRef(startMenuView, "shopPanel", shopPanelGo);

            SetRef(startMenuView, "playButton", playBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "characterSelectionButton", charBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "characterButtonLabel", charBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "characterPopupView", charPopupView);
            SetRef(startMenuView, "upgradeButton", upgradeBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "shopButton", shopBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "settingsButton", settingsBtnGo.GetComponent<Button>());

            SetRef(startMenuView, "upgradeBackButton", upgradeBackBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "settingsBackButton", settingsBackBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "shopBackButton", shopBackBtnGo.GetComponent<Button>());

            SetRef(startMenuView, "willText", willTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "topRightGoldText", goldTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "miningPowerLevelText", powerLvlGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "miningPowerCostText", powerCostGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "miningPowerButton", powerBtnGo.GetComponent<Button>());
            
            SetRef(startMenuView, "maxHPLevelText", hpLvlGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "maxHPCostText", hpCostGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "maxHPButton", hpBtnGo.GetComponent<Button>());

            SetRef(startMenuView, "inventoryLevelText", bagLvlGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "inventoryCostText", bagCostGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "inventoryButton", bagBtnGo.GetComponent<Button>());

            SetRef(startMenuView, "langKoButton", langKoBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "langEnButton", langEnBtnGo.GetComponent<Button>());
            SetRef(startMenuView, "settingsTitleText", settingsTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "settingsLangLabelText", settingsLangLabelGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "settingsCloseLabelText", settingsBackBtnGo.GetComponentInChildren<TextMeshProUGUI>());

            SetRef(startMenuView, "shopTitleText", shopTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "shopDescText", shopDescGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "shopCloseLabelText", shopBackBtnGo.GetComponentInChildren<TextMeshProUGUI>());

            SetRef(startMenuView, "menuTitleText", titleTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(startMenuView, "playButtonLabel", playBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "upgradeButtonLabel", upgradeBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "shopButtonLabel", shopBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "settingsButtonLabel", settingsBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "upgradeBackButtonLabel", upgradeBackBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(startMenuView, "shopBackButtonLabel", shopBackBtnGo.GetComponentInChildren<TextMeshProUGUI>());

            // Add Bootstrapper GameObject
            GameObject bootGo = new GameObject("StartMenuBootstrapper");
            var bootstrapper = bootGo.AddComponent<StartMenuBootstrapper>();
            SetRef(bootstrapper, "view", startMenuView);

            EditorSceneManager.SaveScene(scene, path);
        }

        private static GameObject CreateUIElement(string name, GameObject parent, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.pivot = pivot;
            return go;
        }

        private static GameObject CreateUIText(string name, GameObject parent, string text, int fontSize, Color color, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = TMP_Settings.defaultFontAsset;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(800, 100);

            return go;
        }

        private static GameObject CreateUIButton(string name, GameObject parent, string labelText, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateUIElement(name, parent, pos, size, new Vector2(0.5f, 0.5f));
            
            // Image
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.22f, 0.25f, 1f);

            // Button
            go.AddComponent<Button>();

            // Button label
            if (!string.IsNullOrEmpty(labelText))
            {
                var labelGo = CreateUIText("Label", go, labelText, 28, Color.white, Vector2.zero);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
            }

            return go;
        }

        private static void SetRef(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"Reflection failed: Field {fieldName} not found on {target.GetType().Name}");
            }
        }

        private static void RegisterBuildSettings(string folderPath)
        {
            Debug.Log("Registering scenes in Build Settings...");
            string[] scenePaths = {
                $"{folderPath}/LoadingScene.unity",
                $"{folderPath}/StartMenuScene.unity",
                $"{folderPath}/MainGameScene.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
        }
    }
}
