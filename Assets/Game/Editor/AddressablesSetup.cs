using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Mining;
using DeepEarth.Combat;
using DeepEarth.Event;
using DeepEarth.UI;
using DeepEarth.Map;

namespace DeepEarth.Editor
{
    public static class AddressablesSetup
    {
        private const string RootPath = "Assets/Game";
        public static bool SilentMode = false;

        [MenuItem("Tools/Setup Game Addressables")]
        public static void SetupGame()
        {
            try
            {
                Debug.Log("Starting automated game setup...");

                // 1. Create Folder Structure
                CreateFolders();

                // 2. Generate Procedural Textures and Materials
                GenerateTexturesAndMaterials();
                GenerateEffectIconSprites();

                // 3. Generate Block Prefabs
                GenerateBlockPrefabs();

                // 4. Generate Monster Prefabs
                GenerateMonsterPrefabs();

                // Generate Boss Prefabs
                GenerateBossPrefabs();

                // 5. Generate UI Prefabs
                GenerateUIPrefabs();

                // Generate Map Prefabs and Materials
                GenerateMapPrefabsAndMaterials();

                // 6. Generate Hit Particle Prefab
                GameObject particlePrefab = GenerateParticlePrefab();

                // 7. Setup Addressable Groups and Keys
                ConfigureAddressables();

                // 8. Create and Setup Main Scene
                CreateMainScene(particlePrefab);

                // 9. Build Addressables Content
                BuildAddressables();

                Debug.Log("Automated game setup COMPLETED successfully! Please open Assets/Game/Scenes/MainGameScene.unity and press Play.");
                if (!SilentMode)
                {
                    EditorUtility.DisplayDialog("Setup Successful", "Game structures, prefabs, materials, scene, and Addressables configured successfully!\n\nOpen 'Assets/Game/Scenes/MainGameScene.unity' and run.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Setup failed with exception: {ex.Message}\n{ex.StackTrace}");
                if (!SilentMode)
                {
                    EditorUtility.DisplayDialog("Setup Failed", $"An error occurred during setup:\n{ex.Message}", "OK");
                }
            }
        }

        private static void CreateFolders()
        {
            string[] features = { "Core", "Mining", "Combat", "Event", "UI", "Common", "Map" };
            string[] subfolders = { "Prefabs", "Materials", "Textures", "Scripts" };

            foreach (var feature in features)
            {
                string featurePath = Path.Combine(RootPath, feature);
                EnsureDirectory(featurePath);

                foreach (var sub in subfolders)
                {
                    EnsureDirectory(Path.Combine(featurePath, sub));
                }
            }

            EnsureDirectory(Path.Combine(RootPath, "Scenes"));
            EnsureDirectory(Path.Combine(RootPath, "Editor"));
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.ImportAsset(path);
            }
        }

        private static void GenerateTexturesAndMaterials()
        {
            // Define block and monster types with their colors
            var configs = new Dictionary<string, (Color baseCol, Color spotCol, string feature)>
            {
                { "Dirt", (new Color(0.45f, 0.28f, 0.15f), new Color(0.35f, 0.2f, 0.1f), "Mining") },
                { "Stone", (new Color(0.5f, 0.5f, 0.5f), new Color(0.35f, 0.35f, 0.35f), "Mining") },
                { "Root", (new Color(0.32f, 0.20f, 0.12f), new Color(0.22f, 0.13f, 0.08f), "Mining") },
                { "Iron", (new Color(0.6f, 0.5f, 0.45f), new Color(0.7f, 0.3f, 0.15f), "Mining") },
                { "Silver", (new Color(0.8f, 0.82f, 0.85f), new Color(0.95f, 0.95f, 0.95f), "Mining") },
                { "Gold", (new Color(0.95f, 0.75f, 0.15f), new Color(1.0f, 0.9f, 0.5f), "Mining") },
                { "Diamond", (new Color(0.2f, 0.75f, 0.9f), new Color(0.6f, 0.95f, 1.0f), "Mining") },
                { "Rat", (new Color(0.4f, 0.38f, 0.35f), new Color(0.9f, 0.5f, 0.6f), "Combat") },
                { "Spider", (new Color(0.15f, 0.15f, 0.18f), new Color(0.9f, 0.1f, 0.1f), "Combat") },
                { "Boss_Rat", (new Color(0.22f, 0.20f, 0.18f), new Color(0.9f, 0.3f, 0.3f), "Combat") },
                { "Boss_Spider", (new Color(0.12f, 0.08f, 0.15f), new Color(0.9f, 0.1f, 0.1f), "Combat") },
                { "Boss_Golem", (new Color(0.4f, 0.4f, 0.42f), new Color(0.3f, 0.5f, 0.7f), "Combat") },
                { "Boss_Worm", (new Color(0.9f, 0.45f, 0.1f), new Color(1.0f, 0.8f, 0.2f), "Combat") },
                { "Boss_Titan", (new Color(0.2f, 0.65f, 0.85f), new Color(0.6f, 0.95f, 1.0f), "Combat") }
            };

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");

            foreach (var cfg in configs)
            {
                string name = cfg.Key;
                Color baseColor = cfg.Value.baseCol;
                Color spotColor = cfg.Value.spotCol;
                string feature = cfg.Value.feature;

                string texPath = $"{RootPath}/{feature}/Textures/Texture_{name}.png";
                string matPath = $"{RootPath}/{feature}/Materials/Material_{name}.mat";

                // Generate procedural 16x16 pixel texture
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
                tex.filterMode = FilterMode.Point; // Pixel art style

                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        // Create pixel noise pattern
                        float noise = UnityEngine.Random.value;
                        Color col;
                        if (noise < 0.15f) col = spotColor;
                        else col = baseColor * UnityEngine.Random.Range(0.85f, 1.15f);
                        tex.SetPixel(x, y, col);
                    }
                }
                tex.Apply();

                // Save texture
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(texPath, bytes);
                AssetDatabase.ImportAsset(texPath);

                // Set texture import settings (Point filter, no compression)
                var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null)
                {
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                // Generate Material
                Material mat = new Material(shader);
                Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                mat.mainTexture = savedTex;
                mat.color = baseColor;

                AssetDatabase.CreateAsset(mat, matPath);
            }

            AssetDatabase.SaveAssets();
        }

        private static void GenerateEffectIconSprites()
        {
            var keys = new string[]
            {
                "Effect_Placeholder",
                "Effect_CharacterPassive_Miner",
                "Effect_CharacterPassive_Mercenary",
                "Effect_CharacterPassive_GraveRobber",
                "Effect_Buff_Attack",
                "Effect_Buff_MaxHP",
                "Effect_Buff_Inventory",
                "Effect_Buff_MonsterDecrease",
                "Effect_Buff_HazardDecrease",
                "Effect_Debuff_Attack",
                "Effect_Debuff_MaxHP",
                "Effect_Debuff_MonsterEncounter",
                "Effect_Debuff_HazardEncounter",
                "Effect_Debuff_InstantDamage",
                "Effect_Debuff_MiningFail",
                "Effect_BossReward_AttackPower",
                "Effect_BossReward_MaxHP",
                "Effect_BossReward_MiningPower",
                "Effect_BossReward_Mineral",
                "Effect_BossReward_SpawnDecrease",
                "Effect_BossReward_HealDrop",
                "Effect_BossReward_Revive",
                "Effect_BossReward_BossDamage",
                "Effect_BossReward_Mineral50",
                "Effect_BossReward_DoubleEvent"
            };

            foreach (var key in keys)
            {
                string path = $"{RootPath}/UI/Textures/{key}.png";
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;

                // Fill with transparent background
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        tex.SetPixel(x, y, new Color(0,0,0,0));
                    }
                }

                // Choose a drawing pattern based on key
                Color mainCol = Color.white;
                if (key.Contains("Passive")) mainCol = new Color(0.2f, 0.6f, 1f); // Blue
                else if (key.Contains("Buff")) mainCol = new Color(0.2f, 0.8f, 0.2f); // Green
                else if (key.Contains("Debuff")) mainCol = new Color(0.9f, 0.2f, 0.2f); // Red
                else if (key.Contains("BossReward")) mainCol = new Color(1f, 0.8f, 0f); // Gold

                // Detailed drawing shapes
                if (key.Contains("Attack"))
                {
                    // Draw a sword
                    for (int i = 3; i < 13; i++)
                    {
                        tex.SetPixel(i, i, mainCol); // blade
                        tex.SetPixel(i-1, i, mainCol * 0.8f);
                    }
                    // Handle/Guard
                    tex.SetPixel(2, 2, new Color(0.5f, 0.35f, 0.2f));
                    tex.SetPixel(3, 1, new Color(0.5f, 0.35f, 0.2f));
                    tex.SetPixel(1, 3, new Color(0.5f, 0.35f, 0.2f));
                }
                else if (key.Contains("MaxHP") || key.Contains("HealDrop"))
                {
                    // Draw a heart
                    for (int y = 3; y < 13; y++)
                    {
                        for (int x = 2; x < 14; x++)
                        {
                            // simple heart formula
                            float px = (x - 7.5f) / 5f;
                            float py = (y - 7.5f) / 5f;
                            // Heart equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                            float val = (px*px + py*py - 0.6f);
                            if (val*val*val - px*px*py*py*py <= 0.05f)
                            {
                                tex.SetPixel(x, y, mainCol);
                            }
                        }
                    }
                }
                else if (key.Contains("Inventory") || key.Contains("Mineral"))
                {
                    // Draw a chest/bag/gem
                    // Draw Diamond shape
                    for (int y = 2; y < 14; y++)
                    {
                        int span = 6 - Math.Abs(y - 7);
                        for (int x = 7 - span; x <= 8 + span; x++)
                        {
                            tex.SetPixel(x, y, mainCol);
                        }
                    }
                }
                else if (key.Contains("Monster") || key.Contains("Spawn"))
                {
                    // Draw a skull
                    for (int y = 4; y < 13; y++)
                    {
                        for (int x = 4; x < 12; x++)
                        {
                            tex.SetPixel(x, y, mainCol);
                        }
                    }
                    // Jaw
                    for (int x = 6; x < 10; x++)
                    {
                        tex.SetPixel(x, 2, mainCol);
                        tex.SetPixel(x, 3, mainCol);
                    }
                    // Eyes (Transparent)
                    tex.SetPixel(6, 8, new Color(0,0,0,0));
                    tex.SetPixel(9, 8, new Color(0,0,0,0));
                }
                else if (key.Contains("Hazard") || key.Contains("Lava") || key.Contains("Water"))
                {
                    // Draw a droplet
                    for (int y = 2; y < 14; y++)
                    {
                        int span = (y < 8) ? (y - 2) / 2 : (13 - y);
                        for (int x = 7 - span; x <= 8 + span; x++)
                        {
                            tex.SetPixel(x, y, mainCol);
                        }
                    }
                }
                else if (key.Contains("Miner") || key.Contains("Mining"))
                {
                    // Draw crossed hammers/pickaxe
                    // Arc at top
                    for (int x = 3; x <= 12; x++) tex.SetPixel(x, 12, mainCol);
                    for (int y = 3; y <= 11; y++) tex.SetPixel(7, y, new Color(0.5f, 0.35f, 0.2f)); // brown handle
                    tex.SetPixel(7, 12, new Color(0.3f, 0.3f, 0.3f));
                }
                else if (key.Contains("Mercenary"))
                {
                    // Crossed swords
                    for (int i = 2; i < 14; i++)
                    {
                        tex.SetPixel(i, i, mainCol);
                        tex.SetPixel(15 - i, i, mainCol);
                    }
                }
                else if (key.Contains("GraveRobber"))
                {
                    // Draw diamond outline & fill
                    for (int y = 3; y < 13; y++)
                    {
                        int span = 5 - Math.Abs(y - 7);
                        for (int x = 7 - span; x <= 8 + span; x++)
                        {
                            tex.SetPixel(x, y, new Color(0f, 0.85f, 1f));
                        }
                    }
                }
                else if (key.Contains("Revive"))
                {
                    // Draw a green plus cross
                    for (int i = 3; i < 13; i++)
                    {
                        tex.SetPixel(7, i, Color.green);
                        tex.SetPixel(8, i, Color.green);
                        tex.SetPixel(i, 7, Color.green);
                        tex.SetPixel(i, 8, Color.green);
                    }
                }
                else if (key.Contains("DoubleEvent"))
                {
                    // Draw '2x' or double arrows
                    for (int y = 4; y < 12; y++)
                    {
                        tex.SetPixel(5, y, mainCol);
                        tex.SetPixel(9, y, mainCol);
                    }
                }
                else
                {
                    // Placeholder: simple border with question mark or cross
                    for (int i = 0; i < 16; i++)
                    {
                        tex.SetPixel(i, 0, mainCol);
                        tex.SetPixel(i, 15, mainCol);
                        tex.SetPixel(0, i, mainCol);
                        tex.SetPixel(15, i, mainCol);
                    }
                    tex.SetPixel(7, 7, mainCol);
                }

                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void GenerateBlockPrefabs()
        {
            string[] blocks = { "Dirt", "Stone", "Root", "Iron", "Silver", "Gold", "Diamond" };

            foreach (var b in blocks)
            {
                string prefabPath = $"{RootPath}/Mining/Prefabs/Mining_Block_{b}.prefab";
                string matPath = $"{RootPath}/Mining/Materials/Material_{b}.mat";

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Mining_Block_{b}";

                var view = go.AddComponent<BlockView>();
                var renderer = go.GetComponent<MeshRenderer>();
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                renderer.sharedMaterial = mat;

                // Bind view reference via reflection
                typeof(BlockView).GetField("meshRenderer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, renderer);

                // Save prefab
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                GameObject.DestroyImmediate(go);
            }
        }

        private static void GenerateMonsterPrefabs()
        {
            var monsters = new Dictionary<string, PrimitiveType>
            {
                { "Rat", PrimitiveType.Cube },
                { "Spider", PrimitiveType.Sphere }
            };

            foreach (var m in monsters)
            {
                string name = m.Key;
                PrimitiveType primType = m.Value;

                string prefabPath = $"{RootPath}/Combat/Prefabs/Combat_Monster_{name}.prefab";
                string matPath = $"{RootPath}/Combat/Materials/Material_{name}.mat";

                GameObject go = GameObject.CreatePrimitive(primType);
                go.name = $"Combat_Monster_{name}";

                var view = go.AddComponent<MonsterView>();
                var renderer = go.GetComponent<MeshRenderer>();
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                renderer.sharedMaterial = mat;

                // Adjust size slightly
                if (name == "Rat") go.transform.localScale = new Vector3(0.8f, 0.5f, 1.2f);
                else go.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);

                // Bind view reference
                typeof(MonsterView).GetField("meshRenderer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, renderer);

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                GameObject.DestroyImmediate(go);
            }
        }

        private static void GenerateBossPrefabs()
        {
            var bosses = new Dictionary<string, (PrimitiveType type, Vector3 scale)>
            {
                { "Boss_Rat", (PrimitiveType.Cube, new Vector3(1.8f, 1.2f, 2.5f)) },
                { "Boss_Spider", (PrimitiveType.Sphere, new Vector3(2.0f, 1.5f, 2.0f)) },
                { "Boss_Golem", (PrimitiveType.Cube, new Vector3(2.2f, 2.8f, 2.2f)) },
                { "Boss_Worm", (PrimitiveType.Capsule, new Vector3(1.2f, 3.0f, 1.2f)) },
                { "Boss_Titan", (PrimitiveType.Cube, new Vector3(3.0f, 4.5f, 3.0f)) }
            };

            foreach (var b in bosses)
            {
                string name = b.Key;
                PrimitiveType primType = b.Value.type;
                Vector3 scale = b.Value.scale;

                string prefabPath = $"{RootPath}/Combat/Prefabs/Combat_{name}.prefab";
                string matPath = $"{RootPath}/Combat/Materials/Material_{name}.mat";

                GameObject go = GameObject.CreatePrimitive(primType);
                go.name = $"Combat_{name}";

                var view = go.AddComponent<MonsterView>();
                var renderer = go.GetComponent<MeshRenderer>();
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null) renderer.sharedMaterial = mat;

                go.transform.localScale = scale;

                // Bind view reference via reflection
                typeof(MonsterView).GetField("meshRenderer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, renderer);

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                GameObject.DestroyImmediate(go);
            }
        }

        private static void GenerateUIPrefabs()
        {
            // 7.1 Effect Icon Prefab
            GameObject iconGo = new GameObject("UI_Prefab_EffectIcon", typeof(RectTransform));
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
            var iconView = iconGo.AddComponent<EffectIconView>();
            var iconImgGo = CreateUIElement("IconImage", iconGo, Vector2.zero, new Vector2(60, 60), new Vector2(0.5f, 0.5f));
            var iconImg = iconImgGo.AddComponent<Image>();
            
            var stackTextGo = CreateUIText("StackText", iconGo, "", 18, Color.yellow, new Vector2(15, -15));
            var stackTextTmp = stackTextGo.GetComponent<TextMeshProUGUI>();
            stackTextTmp.alignment = TextAlignmentOptions.BottomRight;
            stackTextTmp.rectTransform.sizeDelta = new Vector2(50, 30);
            
            var tooltipTrigger = iconGo.AddComponent<TooltipTrigger>();
            
            SetRef(iconView, "iconImage", iconImg);
            SetRef(iconView, "stackText", stackTextTmp);
            SetRef(iconView, "tooltipTrigger", tooltipTrigger);
            
            PrefabUtility.SaveAsPrefabAsset(iconGo, $"{RootPath}/UI/Prefabs/UI_Prefab_EffectIcon.prefab");
            GameObject.DestroyImmediate(iconGo);

            // 7.2 Effect Card Prefab
            GameObject cardGo = new GameObject("UI_Prefab_EffectCard", typeof(RectTransform));
            cardGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 150);
            var cardView = cardGo.AddComponent<EffectCardView>();
            
            var borderGo = CreateUIElement("BorderOutline", cardGo, Vector2.zero, new Vector2(800, 150), new Vector2(0.5f, 0.5f));
            var borderImg = borderGo.AddComponent<Image>();
            
            var innerBgGo = CreateUIElement("InnerBackground", borderGo, Vector2.zero, new Vector2(792, 142), new Vector2(0.5f, 0.5f));
            var innerBgImg = innerBgGo.AddComponent<Image>();
            innerBgImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            
            var cardIconGo = CreateUIElement("Icon", innerBgGo, new Vector2(-330, 0), new Vector2(90, 90), new Vector2(0.5f, 0.5f));
            var cardIconImg = cardIconGo.AddComponent<Image>();
            
            var cardNameGo = CreateUIText("NameText", innerBgGo, "Effect Name", 30, Color.white, new Vector2(60, 35));
            var cardNameTmp = cardNameGo.GetComponent<TextMeshProUGUI>();
            cardNameTmp.alignment = TextAlignmentOptions.Left;
            cardNameTmp.rectTransform.sizeDelta = new Vector2(500, 40);
            
            var cardTypeGo = CreateUIText("TypeText", innerBgGo, "Type", 22, Color.cyan, new Vector2(60, 5));
            var cardTypeTmp = cardTypeGo.GetComponent<TextMeshProUGUI>();
            cardTypeTmp.alignment = TextAlignmentOptions.Left;
            cardTypeTmp.rectTransform.sizeDelta = new Vector2(500, 30);
            
            var cardDescGo = CreateUIText("DescText", innerBgGo, "Effect Description goes here", 22, Color.gray, new Vector2(60, -35));
            var cardDescTmp = cardDescGo.GetComponent<TextMeshProUGUI>();
            cardDescTmp.alignment = TextAlignmentOptions.Left;
            cardDescTmp.rectTransform.sizeDelta = new Vector2(500, 40);
            
            SetRef(cardView, "iconImage", cardIconImg);
            SetRef(cardView, "nameText", cardNameTmp);
            SetRef(cardView, "typeText", cardTypeTmp);
            SetRef(cardView, "descText", cardDescTmp);
            SetRef(cardView, "borderOutline", borderImg);
            
            PrefabUtility.SaveAsPrefabAsset(cardGo, $"{RootPath}/UI/Prefabs/UI_Prefab_EffectCard.prefab");
            GameObject.DestroyImmediate(cardGo);

            // 7.3 Relic Popup Panel Prefab
            GameObject rpGo = new GameObject("UI_Panel_RelicPopup", typeof(RectTransform));
            var rpView = rpGo.AddComponent<RelicPopupView>();
            var rpRt = rpGo.GetComponent<RectTransform>();
            rpRt.sizeDelta = new Vector2(1080, 1920);
            
            var rpOverlayGo = CreateUIElement("Overlay", rpGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var rpOverlayImg = rpOverlayGo.AddComponent<Image>();
            rpOverlayImg.color = new Color(0f, 0f, 0f, 0.85f);
            rpOverlayGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            rpOverlayGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            rpOverlayGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            rpOverlayGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            
            var panelRootGo = CreateUIElement("PopupRoot", rpOverlayGo, Vector2.zero, new Vector2(900, 1400), new Vector2(0.5f, 0.5f));
            var panelRootImg = panelRootGo.AddComponent<Image>();
            panelRootImg.color = new Color(0.12f, 0.12f, 0.14f, 1.0f);
            
            var rpTitleGo = CreateUIText("TitleText", panelRootGo, "Active Effects", 50, Color.white, new Vector2(0, 600));
            var rpTitleTmp = rpTitleGo.GetComponent<TextMeshProUGUI>();
            rpTitleTmp.rectTransform.sizeDelta = new Vector2(800, 80);
            
            var rpCloseBtnGo = CreateUIButton("CloseButton", panelRootGo, "CLOSE", new Vector2(0, -600), new Vector2(300, 80));
            var closeBtn = rpCloseBtnGo.GetComponent<Button>();
            var rpCloseTxt = rpCloseBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (rpCloseTxt != null) rpCloseTxt.fontSize = 32;
            
            var scrollViewGo = CreateUIElement("ScrollView", panelRootGo, new Vector2(0, 0), new Vector2(840, 1000), new Vector2(0.5f, 0.5f));
            var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            var viewportGo = CreateUIElement("Viewport", scrollViewGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            
            var contentGo = CreateUIElement("Content", viewportGo, Vector2.zero, new Vector2(840, 0), new Vector2(0.5f, 1.0f));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(0, -1000);
            contentRt.offsetMax = new Vector2(0, 0);
            
            var lg = contentGo.AddComponent<VerticalLayoutGroup>();
            lg.childControlWidth = true;
            lg.childControlHeight = false;
            lg.childForceExpandWidth = true;
            lg.childForceExpandHeight = false;
            lg.spacing = 15;
            lg.padding = new RectOffset(10, 10, 15, 15);
            
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
            
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            
            SetRef(rpView, "popupRoot", panelRootGo);
            SetRef(rpView, "contentParent", contentRt);
            SetRef(rpView, "closeButton", closeBtn);
            SetRef(rpView, "titleText", rpTitleTmp);
            
            var cardPrefabVal = AssetDatabase.LoadAssetAtPath<GameObject>($"{RootPath}/UI/Prefabs/UI_Prefab_EffectCard.prefab");
            SetRef(rpView, "cardPrefab", cardPrefabVal);
            
            rpGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(rpGo, $"{RootPath}/UI/Prefabs/UI_Panel_RelicPopup.prefab");
            GameObject.DestroyImmediate(rpGo);

            // 7.4 Inventory Popup Panel Prefab
            GameObject ipGo = new GameObject("UI_Panel_InventoryPopup", typeof(RectTransform));
            var ipView = ipGo.AddComponent<InventoryPopupView>();
            var ipRt = ipGo.GetComponent<RectTransform>();
            ipRt.sizeDelta = new Vector2(1080, 1920);
            
            var ipOverlayGo = CreateUIElement("Overlay", ipGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var ipOverlayImg = ipOverlayGo.AddComponent<Image>();
            ipOverlayImg.color = new Color(0f, 0f, 0f, 0.85f);
            ipOverlayGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ipOverlayGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ipOverlayGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            ipOverlayGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            
            var ipPanelRootGo = CreateUIElement("PopupRoot", ipOverlayGo, Vector2.zero, new Vector2(900, 1000), new Vector2(0.5f, 0.5f));
            var ipPanelRootImg = ipPanelRootGo.AddComponent<Image>();
            ipPanelRootImg.color = new Color(0.12f, 0.12f, 0.14f, 1.0f);
            
            var ipTitleGo = CreateUIText("TitleText", ipPanelRootGo, "Inventory Details", 50, Color.white, new Vector2(0, 400));
            var ipTitleTmp = ipTitleGo.GetComponent<TextMeshProUGUI>();
            ipTitleTmp.rectTransform.sizeDelta = new Vector2(800, 80);
            
            var ipStatsGo = CreateUIText("StatsText", ipPanelRootGo, "Resources display", 34, Color.white, new Vector2(0, 0));
            var ipStatsTmp = ipStatsGo.GetComponent<TextMeshProUGUI>();
            ipStatsTmp.alignment = TextAlignmentOptions.Center;
            ipStatsTmp.rectTransform.sizeDelta = new Vector2(800, 600);
            
            var ipCloseBtnGo = CreateUIButton("CloseButton", ipPanelRootGo, "CLOSE", new Vector2(0, -400), new Vector2(300, 80));
            var ipCloseBtn = ipCloseBtnGo.GetComponent<Button>();
            var ipCloseTxt = ipCloseBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (ipCloseTxt != null) ipCloseTxt.fontSize = 32;
            
            SetRef(ipView, "popupRoot", ipPanelRootGo);
            SetRef(ipView, "closeButton", ipCloseBtn);
            SetRef(ipView, "titleText", ipTitleTmp);
            SetRef(ipView, "inventoryStatsText", ipStatsTmp);
            
            ipGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(ipGo, $"{RootPath}/UI/Prefabs/UI_Panel_InventoryPopup.prefab");
            GameObject.DestroyImmediate(ipGo);


            // 1. HUD Prefab
            GameObject hudGo = new GameObject("UI_Panel_HUD", typeof(RectTransform));
            var hudView = hudGo.AddComponent<GameUIView>();

            // Setup Layout
            var hudRt = hudGo.GetComponent<RectTransform>();
            hudRt.sizeDelta = new Vector2(1080, 1920);

            // Add Black Background/Vignette panel
            var bgGo = CreateUIElement("Background", hudGo, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.1f, 0.1f, 0.6f);
            bgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // HUD Top Panel
            var topPanelGo = CreateUIElement("TopPanel", hudGo, new Vector2(0, -100), new Vector2(900, 200), new Vector2(0.5f, 1.0f));
            var hpTextGo = CreateUIText("HPText", topPanelGo, "HP: 10 / 10", 38, Color.green, new Vector2(-200, 40));
            var sliderGo = CreateUIElement("HPSlider", topPanelGo, new Vector2(-200, -20), new Vector2(400, 30), new Vector2(0.5f, 0.5f));
            var slider = sliderGo.AddComponent<Slider>();

            // Slider Background
            var sBgGo = CreateUIElement("Background", sliderGo, Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            var sBgImg = sBgGo.AddComponent<Image>();
            sBgImg.color = new Color(0.2f, 0.1f, 0.1f, 1f);
            sBgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sBgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            sBgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sBgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Slider Fill Area
            var sFillArea = CreateUIElement("Fill Area", sliderGo, Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            sFillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sFillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
            sFillArea.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sFillArea.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var sFillGo = CreateUIElement("Fill", sFillArea, Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            var sFillImg = sFillGo.AddComponent<Image>();
            sFillImg.color = Color.red;
            sFillGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            sFillGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            sFillGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            sFillGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            slider.fillRect = sFillGo.GetComponent<RectTransform>();

            var depthTextGo = CreateUIText("DepthText", topPanelGo, "Depth: 0m", 48, Color.white, new Vector2(220, 40));
            var diffTextGo = CreateUIText("DifficultyText", topPanelGo, "Region: Very Easy", 30, Color.yellow, new Vector2(220, -10));
            var bagTextGo = CreateUIText("BagText", topPanelGo, "Bag: 0 / 10", 32, Color.cyan, new Vector2(-200, -70));

            // Resources Panel
            var resPanelGo = CreateUIElement("ResourcePanel", hudGo, new Vector2(0, 150), new Vector2(900, 150), new Vector2(0.5f, 0.0f));
            var ironTextGo = CreateUIText("IronText", resPanelGo, "Iron: 0", 32, new Color(0.8f, 0.7f, 0.6f), new Vector2(-300, 0));
            var silverTextGo = CreateUIText("SilverText", resPanelGo, "Silver: 0", 32, Color.white, new Vector2(-100, 0));
            var goldTextGo = CreateUIText("GoldText", resPanelGo, "Gold: 0", 32, Color.yellow, new Vector2(100, 0));
            var diamondTextGo = CreateUIText("DiamondText", resPanelGo, "Diamond: 0", 32, Color.cyan, new Vector2(300, 0));

            // Buttons (Top Right corner)
            var settingsBtnGo = CreateUIButton("SettingsButton", hudGo, "⚙️", new Vector2(400, 860), new Vector2(120, 60));
            var relicBtnGo = CreateUIButton("RelicButton", hudGo, "유물", new Vector2(260, 860), new Vector2(120, 60));
            var inventoryBtnGo = CreateUIButton("InventoryButton", hudGo, "인벤토리", new Vector2(120, 860), new Vector2(120, 60));

            // EffectIconContainer ScrollRect
            var effectContainerGo = CreateUIElement("EffectIconContainer", hudGo, new Vector2(0, -220), new Vector2(800, 80), new Vector2(0.5f, 1.0f));
            var hudScrollRect = effectContainerGo.AddComponent<ScrollRect>();
            hudScrollRect.horizontal = true;
            hudScrollRect.vertical = false;
            
            var hudViewportGo = CreateUIElement("Viewport", effectContainerGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var hudViewportRt = hudViewportGo.GetComponent<RectTransform>();
            hudViewportRt.anchorMin = Vector2.zero;
            hudViewportRt.anchorMax = Vector2.one;
            hudViewportRt.offsetMin = Vector2.zero;
            hudViewportRt.offsetMax = Vector2.zero;
            hudViewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            hudViewportGo.AddComponent<Mask>().showMaskGraphic = false;
            
            var hudContentGo = CreateUIElement("Content", hudViewportGo, Vector2.zero, new Vector2(0, 80), new Vector2(0.0f, 0.5f));
            var hudContentRt = hudContentGo.GetComponent<RectTransform>();
            hudContentRt.anchorMin = new Vector2(0, 0);
            hudContentRt.anchorMax = new Vector2(0, 1);
            hudContentRt.offsetMin = Vector2.zero;
            hudContentRt.offsetMax = Vector2.zero;
            
            var hudLg = hudContentGo.AddComponent<HorizontalLayoutGroup>();
            hudLg.childControlWidth = false;
            hudLg.childControlHeight = false;
            hudLg.childForceExpandWidth = false;
            hudLg.childForceExpandHeight = false;
            hudLg.spacing = 10;
            
            var hudCsf = hudContentGo.AddComponent<ContentSizeFitter>();
            hudCsf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            
            hudScrollRect.viewport = hudViewportRt;
            hudScrollRect.content = hudContentRt;

            // Tooltip Panel
            var tooltipPanelGo = CreateUIElement("TooltipPanel", hudGo, Vector2.zero, new Vector2(300, 100), new Vector2(0.5f, 0f));
            tooltipPanelGo.SetActive(false);
            var tooltipPanelImg = tooltipPanelGo.AddComponent<Image>();
            tooltipPanelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            
            var tooltipTitleGo = CreateUIText("TooltipTitle", tooltipPanelGo, "Title", 22, Color.yellow, new Vector2(0, 20));
            var tooltipTitleTmp = tooltipTitleGo.GetComponent<TextMeshProUGUI>();
            tooltipTitleTmp.alignment = TextAlignmentOptions.Center;
            tooltipTitleTmp.rectTransform.sizeDelta = new Vector2(280, 40);
            
            var tooltipDescGo = CreateUIText("TooltipDesc", tooltipPanelGo, "Description", 18, Color.white, new Vector2(0, -20));
            var tooltipDescTmp = tooltipDescGo.GetComponent<TextMeshProUGUI>();
            tooltipDescTmp.alignment = TextAlignmentOptions.Center;
            tooltipDescTmp.rectTransform.sizeDelta = new Vector2(280, 50);

            // Add EffectHUDView component to effectContainerGo
            var hudEffectView = effectContainerGo.AddComponent<EffectHUDView>();
            SetRef(hudEffectView, "containerParent", hudContentRt);
            SetRef(hudEffectView, "tooltipPanel", tooltipPanelGo);
            SetRef(hudEffectView, "tooltipTitle", tooltipTitleTmp);
            SetRef(hudEffectView, "tooltipDesc", tooltipDescTmp);

            // Load generated EffectIcon prefab
            var iconPrefabVal = AssetDatabase.LoadAssetAtPath<GameObject>($"{RootPath}/UI/Prefabs/UI_Prefab_EffectIcon.prefab");
            SetRef(hudEffectView, "iconPrefab", iconPrefabVal);

            // Bind references
            SetRef(hudView, "hpText", hpTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "hpSlider", slider);
            SetRef(hudView, "depthText", depthTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "difficultyText", diffTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "inventorySizeText", bagTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "ironText", ironTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "silverText", silverTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "goldText", goldTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "diamondText", diamondTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(hudView, "settingsButton", settingsBtnGo.GetComponent<Button>());
            SetRef(hudView, "settingsButtonLabel", settingsBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(hudView, "relicButton", relicBtnGo.GetComponent<Button>());
            SetRef(hudView, "relicButtonLabel", relicBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(hudView, "inventoryButton", inventoryBtnGo.GetComponent<Button>());
            SetRef(hudView, "inventoryButtonLabel", inventoryBtnGo.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(hudView, "effectIconContainer", effectContainerGo.transform);

            PrefabUtility.SaveAsPrefabAsset(hudGo, $"{RootPath}/UI/Prefabs/UI_Panel_HUD.prefab");
            GameObject.DestroyImmediate(hudGo);


            // 2. GameOver Prefab
            GameObject goGo = new GameObject("UI_Panel_GameOver", typeof(RectTransform));
            var goView = goGo.AddComponent<GameOverUIView>();
            var goRt = goGo.GetComponent<RectTransform>();
            goRt.sizeDelta = new Vector2(1080, 1920);

            // Transparent dark overlay
            var overlayGo = CreateUIElement("Overlay", goGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.92f); // Dark background
            overlayGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            overlayGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            overlayGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            overlayGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Title
            var titleGo = CreateUIText("GameOverTitle", overlayGo, "YOU DIED", 84, Color.red, new Vector2(0, 450));

            // Summary Info
            var resDepthGo = CreateUIText("ResultDepth", overlayGo, "Depth Reached: 0m", 44, Color.white, new Vector2(0, 260));
            var resWillGo = CreateUIText("WillEarned", overlayGo, "Will Gathered: +0", 38, Color.yellow, new Vector2(0, 160));
            var bestDepthGo = CreateUIText("BestDepth", overlayGo, "Best Depth: 0m", 38, Color.yellow, new Vector2(0, 60));
            var totalWillGo = CreateUIText("TotalWill", overlayGo, "Total Will: 0", 38, Color.yellow, new Vector2(0, -40));

            // Restart Button (Main Menu Button)
            var restartBtnGo = CreateUIButton("RestartButton", overlayGo, "MAIN MENU", new Vector2(0, -250), new Vector2(400, 90));
            var restartTxt = restartBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (restartTxt != null) restartTxt.fontSize = 38;

            // Bind references
            SetRef(goView, "titleText", titleGo.GetComponent<TextMeshProUGUI>());
            SetRef(goView, "resultDepthText", resDepthGo.GetComponent<TextMeshProUGUI>());
            SetRef(goView, "willEarnedText", resWillGo.GetComponent<TextMeshProUGUI>());
            SetRef(goView, "totalWillText", totalWillGo.GetComponent<TextMeshProUGUI>());
            SetRef(goView, "bestDepthText", bestDepthGo.GetComponent<TextMeshProUGUI>());
            SetRef(goView, "restartButton", restartBtnGo.GetComponent<Button>());
            SetRef(goView, "restartButtonLabel", restartTxt);

            goGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(goGo, $"{RootPath}/UI/Prefabs/UI_Panel_GameOver.prefab");
            GameObject.DestroyImmediate(goGo);


            // 3. Event Panel Prefab
            GameObject evGo = new GameObject("UI_Panel_Event", typeof(RectTransform));
            var evView = evGo.AddComponent<EventUIView>();
            var evRt = evGo.GetComponent<RectTransform>();
            evRt.sizeDelta = new Vector2(1080, 1920);

            // Overlay
            var evOverlayGo = CreateUIElement("Overlay", evGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var evOverlayImg = evOverlayGo.AddComponent<Image>();
            evOverlayImg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            evOverlayGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            evOverlayGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            evOverlayGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            evOverlayGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Header/Title
            var evTitleGo = CreateUIText("EventTitle", evOverlayGo, "Treasure Box Found", 56, Color.yellow, new Vector2(0, 480));
            var evDescGo = CreateUIText("EventDescription", evOverlayGo, "Select one option to apply standard effects.", 32, Color.white, new Vector2(0, 340));
            var evDescTmp = evDescGo.GetComponent<TextMeshProUGUI>();
            evDescTmp.rectTransform.sizeDelta = new Vector2(850, 200);

            // Three choice buttons
            var buttonsList = new List<Button>();
            var optTitleList = new List<TextMeshProUGUI>();
            var optDescList = new List<TextMeshProUGUI>();

            float[] yOffsets = { 100f, -120f, -340f };

            for (int i = 0; i < 3; i++)
            {
                var optBtnGo = CreateUIButton($"OptionButton_{i}", evOverlayGo, "", new Vector2(0, yOffsets[i]), new Vector2(800, 160));
                var optBtn = optBtnGo.GetComponent<Button>();
                
                // Customize choice texts
                var optTitleGo = CreateUIText("Title", optBtnGo, $"Option {i+1}", 32, Color.cyan, new Vector2(0, 35));
                var optDescGo = CreateUIText("Desc", optBtnGo, "Buff details show here", 26, Color.white, new Vector2(0, -25));

                buttonsList.Add(optBtn);
                optTitleList.Add(optTitleGo.GetComponent<TextMeshProUGUI>());
                optDescList.Add(optDescGo.GetComponent<TextMeshProUGUI>());
            }

            // Bind references
            SetRef(evView, "titleText", evTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(evView, "descriptionText", evDescTmp);
            SetRef(evView, "optionButtons", buttonsList);
            SetRef(evView, "optionTitleTexts", optTitleList);
            SetRef(evView, "optionDescTexts", optDescList);

            evGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(evGo, $"{RootPath}/UI/Prefabs/UI_Panel_Event.prefab");
            GameObject.DestroyImmediate(evGo);


            // 4. Settings Panel Prefab
            GameObject settingsGo = new GameObject("UI_Panel_Settings", typeof(RectTransform));
            var settingsView = settingsGo.AddComponent<SettingsUIView>();
            var settingsRt = settingsGo.GetComponent<RectTransform>();
            settingsRt.sizeDelta = new Vector2(1080, 1920);

            // Overlay
            var settingsOverlayGo = CreateUIElement("Overlay", settingsGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var settingsOverlayImg = settingsOverlayGo.AddComponent<Image>();
            settingsOverlayImg.color = new Color(0f, 0f, 0f, 0.90f);
            settingsOverlayGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            settingsOverlayGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            settingsOverlayGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            settingsOverlayGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Title
            var settingsTitleGo = CreateUIText("SettingsTitle", settingsOverlayGo, "SETTINGS", 64, Color.white, new Vector2(0, 400));

            // Language Tab Label
            var langLabelGo = CreateUIText("LanguageLabel", settingsOverlayGo, "Language: ", 36, Color.gray, new Vector2(0, 180));

            // Language Buttons Container
            var langContainer = CreateUIElement("LangContainer", settingsOverlayGo, new Vector2(0, 80), new Vector2(600, 100), new Vector2(0.5f, 0.5f));
            var langKoBtnGo = CreateUIButton("LanguageKoButton", langContainer, "한글", new Vector2(-150, 0), new Vector2(250, 80));
            var langEnBtnGo = CreateUIButton("LanguageEnButton", langContainer, "English", new Vector2(150, 0), new Vector2(250, 80));

            // Close Button
            var closeBtnGo = CreateUIButton("CloseButton", settingsOverlayGo, "CLOSE", new Vector2(0, -200), new Vector2(300, 80));
            var closeTxt = closeBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (closeTxt != null) closeTxt.fontSize = 32;

            // Bind references
            SetRef(settingsView, "titleText", settingsTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(settingsView, "closeButton", closeBtnGo.GetComponent<Button>());
            SetRef(settingsView, "closeButtonLabel", closeTxt);
            SetRef(settingsView, "languageTabLabel", langLabelGo.GetComponent<TextMeshProUGUI>());
            SetRef(settingsView, "languageKoButton", langKoBtnGo.GetComponent<Button>());
            SetRef(settingsView, "languageEnButton", langEnBtnGo.GetComponent<Button>());

            settingsGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(settingsGo, $"{RootPath}/UI/Prefabs/UI_Panel_Settings.prefab");
            GameObject.DestroyImmediate(settingsGo);


            // 5. Boss Room HUD UI
            GameObject bossHUDGo = new GameObject("UI_Panel_BossRoom", typeof(RectTransform));
            var bossHUDView = bossHUDGo.AddComponent<BossView>();
            var bossHUDRt = bossHUDGo.GetComponent<RectTransform>();
            bossHUDRt.sizeDelta = new Vector2(1080, 1920);

            // Boss HUD Panel Root at the top
            var bossTopPanelGo = CreateUIElement("TopPanel", bossHUDGo, new Vector2(0, -100), new Vector2(900, 240), new Vector2(0.5f, 1.0f));
            var bossTopPanelRt = bossTopPanelGo.GetComponent<RectTransform>();
            bossTopPanelRt.anchorMin = new Vector2(0.5f, 1.0f);
            bossTopPanelRt.anchorMax = new Vector2(0.5f, 1.0f);
            
            var bossHUDImg = bossTopPanelGo.AddComponent<Image>();
            bossHUDImg.color = new Color(0.15f, 0.05f, 0.05f, 0.9f); // Dark red panel background

            // BOSS label (Literal)
            var bossHUDTitleGo = CreateUIText("TitleText", bossTopPanelGo, "BOSS", 36, new Color(1f, 0.85f, 0.3f), new Vector2(0, 80));
            // Boss name key
            var bossHUDNameGo = CreateUIText("NameText", bossTopPanelGo, "Boss Name", 44, Color.white, new Vector2(0, 30));

            // HP Slider
            var bossHPSliderGo = CreateUIElement("HPSlider", bossTopPanelGo, new Vector2(0, -35), new Vector2(700, 32), new Vector2(0.5f, 0.5f));
            var bossHPSlider = bossHPSliderGo.AddComponent<Slider>();

            // Slider Background
            var bossHPSliderBgGo = CreateUIElement("Background", bossHPSliderGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bossHPSliderBgImg = bossHPSliderBgGo.AddComponent<Image>();
            bossHPSliderBgImg.color = new Color(0.25f, 0.08f, 0.08f, 1f);
            bossHPSliderBgGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bossHPSliderBgGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bossHPSliderBgGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bossHPSliderBgGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Slider Fill Area
            var bossHPSliderFillArea = CreateUIElement("Fill Area", bossHPSliderGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            bossHPSliderFillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bossHPSliderFillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bossHPSliderFillArea.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bossHPSliderFillArea.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var bossHPSliderFillGo = CreateUIElement("Fill", bossHPSliderFillArea, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bossHPSliderFillImg = bossHPSliderFillGo.AddComponent<Image>();
            bossHPSliderFillImg.color = Color.red;
            bossHPSliderFillGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bossHPSliderFillGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            bossHPSliderFillGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bossHPSliderFillGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            bossHPSlider.fillRect = bossHPSliderFillGo.GetComponent<RectTransform>();

            // HP text on slider
            var bossHUDHPTextGo = CreateUIText("HPText", bossHPSliderGo, "20 / 20", 26, Color.white, Vector2.zero);
            var bossHUDHPTextRt = bossHUDHPTextGo.GetComponent<RectTransform>();
            bossHUDHPTextRt.anchorMin = Vector2.zero;
            bossHUDHPTextRt.anchorMax = Vector2.one;
            bossHUDHPTextRt.offsetMin = Vector2.zero;
            bossHUDHPTextRt.offsetMax = Vector2.zero;

            // Shield text
            var bossHUDShieldTextGo = CreateUIText("ShieldText", bossTopPanelGo, "(Shield: 10 / 10)", 28, new Color(0.2f, 0.6f, 1f), new Vector2(0, -90));

            // Bind BossView fields
            SetRef(bossHUDView, "bossTitleText", bossHUDTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(bossHUDView, "bossNameText", bossHUDNameGo.GetComponent<TextMeshProUGUI>());
            SetRef(bossHUDView, "hpSlider", bossHPSlider);
            SetRef(bossHUDView, "hpText", bossHUDHPTextGo.GetComponent<TextMeshProUGUI>());
            SetRef(bossHUDView, "shieldText", bossHUDShieldTextGo.GetComponent<TextMeshProUGUI>());

            bossHUDGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(bossHUDGo, $"{RootPath}/UI/Prefabs/UI_Panel_BossRoom.prefab");
            GameObject.DestroyImmediate(bossHUDGo);


            // 6. Boss Reward UI
            GameObject rewardGo = new GameObject("UI_Panel_BossReward", typeof(RectTransform));
            var rewardView = rewardGo.AddComponent<BossRewardView>();
            var rewardRt = rewardGo.GetComponent<RectTransform>();
            rewardRt.sizeDelta = new Vector2(1080, 1920);

            // Overlay dark background
            var rewardOverlayGo = CreateUIElement("Overlay", rewardGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var rewardOverlayRt = rewardOverlayGo.GetComponent<RectTransform>();
            rewardOverlayRt.anchorMin = Vector2.zero;
            rewardOverlayRt.anchorMax = Vector2.one;
            rewardOverlayRt.offsetMin = Vector2.zero;
            rewardOverlayRt.offsetMax = Vector2.zero;

            var rewardOverlayImg = rewardOverlayGo.AddComponent<Image>();
            rewardOverlayImg.color = new Color(0.05f, 0.05f, 0.06f, 0.95f);

            // Title and Subtitle
            var rewardTitleGo = CreateUIText("TitleText", rewardOverlayGo, "BOSS DEFEATED", 72, new Color(1f, 0.85f, 0.3f), new Vector2(0, 480));
            var rewardSubtitleGo = CreateUIText("SubtitleText", rewardOverlayGo, "Select one powerful run-local buff:", 32, Color.white, new Vector2(0, 360));
            var rewardSubtitleTmp = rewardSubtitleGo.GetComponent<TextMeshProUGUI>();
            rewardSubtitleTmp.rectTransform.sizeDelta = new Vector2(900, 100);

            // 3 reward choices cards (side by side, x = -320, 0, 320)
            var cardButtons = new List<Button>();
            var cardTitleTexts = new List<TextMeshProUGUI>();
            var cardDescTexts = new List<TextMeshProUGUI>();
            var cardBackgrounds = new List<Image>();

            float[] cardXPositions = { -320f, 0f, 320f };

            for (int i = 0; i < 3; i++)
            {
                var rewardCardGo = CreateUIButton($"Card_{i}", rewardOverlayGo, "", new Vector2(cardXPositions[i], -100), new Vector2(290, 520));
                var cardBtn = rewardCardGo.GetComponent<Button>();
                var cardImg = rewardCardGo.GetComponent<Image>();
                cardImg.color = new Color(0.18f, 0.2f, 0.23f, 1f); // Slate grey base

                var cTitleGo = CreateUIText("TitleText", rewardCardGo, "Buff Title", 32, Color.white, new Vector2(0, 200));
                var cTitleTmp = cTitleGo.GetComponent<TextMeshProUGUI>();
                cTitleTmp.rectTransform.sizeDelta = new Vector2(270, 80);

                var cDescGo = CreateUIText("DescText", rewardCardGo, "Buff Description goes here...", 24, Color.lightGray, new Vector2(0, -50));
                var cDescTmp = cDescGo.GetComponent<TextMeshProUGUI>();
                cDescTmp.alignment = TextAlignmentOptions.TopLeft;
                cDescTmp.rectTransform.sizeDelta = new Vector2(250, 320);

                cardButtons.Add(cardBtn);
                cardTitleTexts.Add(cTitleTmp);
                cardDescTexts.Add(cDescTmp);
                cardBackgrounds.Add(cardImg);
            }

            // Bind BossRewardView fields
            SetRef(rewardView, "titleText", rewardTitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(rewardView, "subtitleText", rewardSubtitleTmp);
            SetRef(rewardView, "cardButtons", cardButtons);
            SetRef(rewardView, "cardTitleTexts", cardTitleTexts);
            SetRef(rewardView, "cardDescTexts", cardDescTexts);
            SetRef(rewardView, "cardBackgrounds", cardBackgrounds);

            rewardGo.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(rewardGo, $"{RootPath}/UI/Prefabs/UI_Panel_BossReward.prefab");
            GameObject.DestroyImmediate(rewardGo);
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
            var btn = go.AddComponent<Button>();
            
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

        private static GameObject GenerateParticlePrefab()
        {
            string path = $"{RootPath}/Common/Prefabs/Common_HitParticles.prefab";

            GameObject go = new GameObject("Common_HitParticles");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSize = 0.15f;
            main.startSpeed = 4.0f;
            main.startLifetime = 0.4f;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0.0f, 25);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            // Apply texture sheet/material if needed, or default
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void ConfigureAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            // Create target groups
            string[] groups = { "Core", "Mining", "Combat", "Event", "UI", "Common" };
            var groupDict = new Dictionary<string, AddressableAssetGroup>();

            foreach (var g in groups)
            {
                var group = settings.FindGroup(g);
                if (group == null)
                {
                    group = settings.CreateGroup(g, false, false, false, settings.DefaultGroup.Schemas);
                }
                groupDict[g] = group;
            }

            // Map and register assets
            var assetsToRegister = new List<(string path, string key, string group)>
            {
                // Blocks
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Dirt.prefab", AddressableKeys.BlockDirt, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Stone.prefab", AddressableKeys.BlockStone, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Root.prefab", AddressableKeys.BlockRoot, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Iron.prefab", AddressableKeys.BlockIron, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Silver.prefab", AddressableKeys.BlockSilver, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Gold.prefab", AddressableKeys.BlockGold, "Mining" ),
                ( $"{RootPath}/Mining/Prefabs/Mining_Block_Diamond.prefab", AddressableKeys.BlockDiamond, "Mining" ),

                // Monsters
                ( $"{RootPath}/Combat/Prefabs/Combat_Monster_Rat.prefab", AddressableKeys.MonsterRat, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Monster_Spider.prefab", AddressableKeys.MonsterSpider, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Boss_Rat.prefab", AddressableKeys.MonsterBossRat, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Boss_Spider.prefab", AddressableKeys.MonsterBossSpider, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Boss_Golem.prefab", AddressableKeys.MonsterBossGolem, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Boss_Worm.prefab", AddressableKeys.MonsterBossWorm, "Combat" ),
                ( $"{RootPath}/Combat/Prefabs/Combat_Boss_Titan.prefab", AddressableKeys.MonsterBossTitan, "Combat" ),

                // UI
                ( $"{RootPath}/UI/Prefabs/UI_Panel_HUD.prefab", AddressableKeys.UIPanelHUD, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_GameOver.prefab", AddressableKeys.UIPanelGameOver, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_Event.prefab", AddressableKeys.UIPanelEvent, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_Settings.prefab", AddressableKeys.UIPanelSettings, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_BossRoom.prefab", AddressableKeys.UIPanelBossRoom, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_BossReward.prefab", AddressableKeys.UIPanelBossReward, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_RelicPopup.prefab", AddressableKeys.UIPanelRelicPopup, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Panel_InventoryPopup.prefab", AddressableKeys.UIPanelInventoryPopup, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Prefab_EffectIcon.prefab", AddressableKeys.UIEffectIcon, "UI" ),
                ( $"{RootPath}/UI/Prefabs/UI_Prefab_EffectCard.prefab", AddressableKeys.UIEffectCard, "UI" ),

                // Map & Themes
                ( $"{RootPath}/Map/Prefabs/Map_Wall_Segment.prefab", AddressableKeys.MapWallSegment, "UI" ),
                ( $"{RootPath}/Map/Prefabs/ThemeManager.prefab", AddressableKeys.ThemeManager, "UI" ),
                ( $"{RootPath}/Map/Materials/Material_Theme_Dirt.mat", AddressableKeys.ThemeDirt, "Mining" ),
                ( $"{RootPath}/Map/Materials/Material_Theme_Stone.mat", AddressableKeys.ThemeStone, "Mining" ),
                ( $"{RootPath}/Map/Materials/Material_Theme_Iron.mat", AddressableKeys.ThemeIron, "Mining" ),
                ( $"{RootPath}/Map/Materials/Material_Theme_Gold.mat", AddressableKeys.ThemeGold, "Mining" ),
                ( $"{RootPath}/Map/Materials/Material_Theme_Crystal.mat", AddressableKeys.ThemeCrystal, "Mining" ),
                // Fonts
                ( $"{RootPath}/Common/Fonts/Pretendard-Regular SDF.asset", AddressableKeys.FontDefault, "Common" ),
                ( $"{RootPath}/Common/Fonts/NotoSansKR SDF.asset", AddressableKeys.FontNotoSansKR, "Common" )
            };

            var effectKeys = new string[]
            {
                "Effect_Placeholder",
                "Effect_CharacterPassive_Miner",
                "Effect_CharacterPassive_Mercenary",
                "Effect_CharacterPassive_GraveRobber",
                "Effect_Buff_Attack",
                "Effect_Buff_MaxHP",
                "Effect_Buff_Inventory",
                "Effect_Buff_MonsterDecrease",
                "Effect_Buff_HazardDecrease",
                "Effect_Debuff_Attack",
                "Effect_Debuff_MaxHP",
                "Effect_Debuff_MonsterEncounter",
                "Effect_Debuff_HazardEncounter",
                "Effect_Debuff_InstantDamage",
                "Effect_Debuff_MiningFail",
                "Effect_BossReward_AttackPower",
                "Effect_BossReward_MaxHP",
                "Effect_BossReward_MiningPower",
                "Effect_BossReward_Mineral",
                "Effect_BossReward_SpawnDecrease",
                "Effect_BossReward_HealDrop",
                "Effect_BossReward_Revive",
                "Effect_BossReward_BossDamage",
                "Effect_BossReward_Mineral50",
                "Effect_BossReward_DoubleEvent"
            };

            foreach (var ek in effectKeys)
            {
                assetsToRegister.Add( ($"{RootPath}/UI/Textures/{ek}.png", ek, "UI") );
            }

            foreach (var entry in assetsToRegister)
            {
                var guid = AssetDatabase.AssetPathToGUID(entry.path);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"Skipped Addressable setup for missing asset: {entry.path}");
                    continue;
                }

                var addressableEntry = settings.CreateOrMoveEntry(guid, groupDict[entry.group]);
                addressableEntry.address = entry.key;
                
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, addressableEntry, true);
            }

            AssetDatabase.SaveAssets();
        }

        private static void CreateMainScene(GameObject particlePrefab)
        {
            string scenePath = $"{RootPath}/Scenes/MainGameScene.unity";

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Add Bootstrapper GameObject
            GameObject bootGo = new GameObject("GameBootstrapper");
            var bootstrap = bootGo.AddComponent<GameBootstrap>();

            // Create requested GameplayRoot & MapRoot hierarchy
            GameObject gameplayRoot = new GameObject("GameplayRoot");

            GameObject mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(gameplayRoot.transform);

            GameObject floorParent = new GameObject("Floor");
            floorParent.transform.SetParent(mapRoot.transform);

            GameObject leftWallParent = new GameObject("LeftWall");
            leftWallParent.transform.SetParent(mapRoot.transform);

            GameObject rightWallParent = new GameObject("RightWall");
            rightWallParent.transform.SetParent(mapRoot.transform);

            GameObject ceilingParent = new GameObject("Ceiling");
            ceilingParent.transform.SetParent(mapRoot.transform);

            GameObject blockSpawnRoot = new GameObject("BlockSpawnRoot");
            blockSpawnRoot.transform.SetParent(mapRoot.transform);
            blockSpawnRoot.transform.position = new Vector3(0, 0, 3);

            GameObject playerRoot = new GameObject("PlayerRoot");
            playerRoot.transform.SetParent(gameplayRoot.transform);
            playerRoot.transform.position = Vector3.zero;

            GameObject monsterRoot = new GameObject("MonsterRoot");
            monsterRoot.transform.SetParent(gameplayRoot.transform);
            monsterRoot.transform.position = new Vector3(0, 0, 3.2f);

            GameObject eventRoot = new GameObject("EventRoot");
            eventRoot.transform.SetParent(gameplayRoot.transform);
            eventRoot.transform.position = Vector3.zero;

            // Create UI Canvas
            GameObject canvasGo = new GameObject("UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();

            // Red Flash Overlay Image
            GameObject flashGo = CreateUIElement("FlashOverlay", canvasGo, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1f, 0f, 0f, 0f);
            flashGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            flashGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            flashGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            flashGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            flashGo.SetActive(false);

            // Bind bootstrap references
            SetRef(bootstrap, "canvas", canvas);
            SetRef(bootstrap, "flashOverlay", flashImg);
            SetRef(bootstrap, "particlePrefab", particlePrefab);
            SetRef(bootstrap, "blockSpawnPoint", blockSpawnRoot.transform);
            SetRef(bootstrap, "monsterSpawnPoint", monsterRoot.transform);
            SetRef(bootstrap, "mapRoot", mapRoot.transform);
            SetRef(bootstrap, "floorParent", floorParent.transform);
            SetRef(bootstrap, "leftWallParent", leftWallParent.transform);
            SetRef(bootstrap, "rightWallParent", rightWallParent.transform);
            SetRef(bootstrap, "ceilingParent", ceilingParent.transform);

            // Setup Event System in scene (required for UI interaction)
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Setup Camera position and rotation inside the tunnel
            var cameraGo = GameObject.FindWithTag("MainCamera");
            if (cameraGo != null)
            {
                cameraGo.transform.position = new Vector3(0f, 0.4f, -1.5f);
                cameraGo.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
                var cam = cameraGo.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.12f, 0.1f, 0.1f);
                }
            }

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void GenerateMapPrefabsAndMaterials()
        {
            // 1. Create Theme Materials
            var themeConfigs = new Dictionary<string, string>
            {
                { "Theme_Dirt", "Dirt" },
                { "Theme_Stone", "Stone" },
                { "Theme_Iron", "Iron" },
                { "Theme_Gold", "Gold" },
                { "Theme_Crystal", "Diamond" }
            };

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");

            foreach (var config in themeConfigs)
            {
                string themeKey = config.Key;
                string baseTexName = config.Value;

                string texPath = $"{RootPath}/Mining/Textures/Texture_{baseTexName}.png";
                string matPath = $"{RootPath}/Map/Materials/Material_{themeKey}.mat";

                Material mat = new Material(shader);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                }
                
                // Customize color slightly for theme ambiance
                if (themeKey == "Theme_Dirt") mat.color = new Color(0.45f, 0.28f, 0.15f);
                else if (themeKey == "Theme_Stone") mat.color = new Color(0.5f, 0.5f, 0.5f);
                else if (themeKey == "Theme_Iron") mat.color = new Color(0.6f, 0.5f, 0.45f);
                else if (themeKey == "Theme_Gold") mat.color = new Color(0.95f, 0.75f, 0.15f);
                else if (themeKey == "Theme_Crystal") mat.color = new Color(0.2f, 0.75f, 0.9f);

                AssetDatabase.CreateAsset(mat, matPath);
            }

            // 2. Create Wall Segment Prefab
            GameObject wallGo = new GameObject("Map_Wall_Segment");
            var segmentView = wallGo.AddComponent<WallSegmentView>();

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(wallGo.transform);
            floor.transform.localPosition = new Vector3(0, -1.5f, 0);
            floor.transform.localScale = new Vector3(3f, 0.1f, 2f);

            // Ceiling
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(wallGo.transform);
            ceiling.transform.localPosition = new Vector3(0, 1.5f, 0);
            ceiling.transform.localScale = new Vector3(3f, 0.1f, 2f);

            // LeftWall
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(wallGo.transform);
            leftWall.transform.localPosition = new Vector3(-1.5f, 0, 0);
            leftWall.transform.localScale = new Vector3(0.1f, 3f, 2f);

            // RightWall
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(wallGo.transform);
            rightWall.transform.localPosition = new Vector3(1.5f, 0, 0);
            rightWall.transform.localScale = new Vector3(0.1f, 3f, 2f);

            // Bind mesh renderer fields via reflection
            SetRef(segmentView, "floorRenderer", floor.GetComponent<MeshRenderer>());
            SetRef(segmentView, "ceilingRenderer", ceiling.GetComponent<MeshRenderer>());
            SetRef(segmentView, "leftWallRenderer", leftWall.GetComponent<MeshRenderer>());
            SetRef(segmentView, "rightWallRenderer", rightWall.GetComponent<MeshRenderer>());

            PrefabUtility.SaveAsPrefabAsset(wallGo, $"{RootPath}/Map/Prefabs/Map_Wall_Segment.prefab");
            GameObject.DestroyImmediate(wallGo);

            // 3. Create Theme Manager Prefab
            GameObject themeMgrGo = new GameObject("ThemeManager");
            themeMgrGo.AddComponent<ThemeView>();
            themeMgrGo.AddComponent<ThemeManager>();

            PrefabUtility.SaveAsPrefabAsset(themeMgrGo, $"{RootPath}/Map/Prefabs/ThemeManager.prefab");
            GameObject.DestroyImmediate(themeMgrGo);

            AssetDatabase.SaveAssets();
        }

        private static void BuildAddressables()
        {
            Debug.Log("Building Addressables content...");
            AddressableAssetSettings.BuildPlayerContent();
        }
    }
}
