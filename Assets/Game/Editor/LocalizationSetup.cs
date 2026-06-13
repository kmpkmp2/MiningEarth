using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using TMPro;

namespace DeepEarth.Editor
{
    public static class LocalizationSetup
    {
        private const string FontPath = "Assets/Game/Common/Fonts/Pretendard-Regular.ttf";
        private const string TargetSdfPath = "Assets/Game/Common/Fonts/Pretendard-Regular SDF.asset";
        private const string CloneSdfPath = "Assets/Game/Common/Fonts/NotoSansKR SDF.asset";
        private const string PrimaryFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/Setup Localization and Fonts")]
        public static void Setup()
        {
            try
            {
                Debug.Log("Starting automated localization and font setup...");

                // 1. Force import the new font file if it is not imported yet
                if (!File.Exists(FontPath))
                {
                    Debug.LogError($"Source font file not found at {FontPath}! Make sure you downloaded it.");
                    return;
                }
                AssetDatabase.ImportAsset(FontPath);
                Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if (font == null)
                {
                    Debug.LogError("Failed to load source Font asset!");
                    return;
                }

                // 2. Generate dynamic TMP Font Asset for Pretendard
                GenerateDynamicFontAsset(font, TargetSdfPath);

                // 3. Create a duplicate clone for NotoSansKR SDF to support both keys
                if (File.Exists(CloneSdfPath))
                {
                    AssetDatabase.DeleteAsset(CloneSdfPath);
                }
                AssetDatabase.CopyAsset(TargetSdfPath, CloneSdfPath);
                AssetDatabase.ImportAsset(CloneSdfPath);

                TMP_FontAsset pretendardSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetSdfPath);
                TMP_FontAsset notoSansSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CloneSdfPath);

                if (pretendardSdf == null || notoSansSdf == null)
                {
                    Debug.LogError("Failed to load created SDF font assets!");
                    return;
                }

                // Rename the cloned texture/material sub-objects to keep it clean
                RenameSubAssets(notoSansSdf, "NotoSansKR");

                // 4. Set fallback font on Primary Font (LiberationSans SDF)
                ConfigureFallback(pretendardSdf, notoSansSdf);

                // 5. Register in Addressables
                RegisterAddressable(TargetSdfPath, "Font_Default");
                RegisterAddressable(CloneSdfPath, "Font_NotoSansKR");

                // 6. Build Addressables content
                Debug.Log("Rebuilding Addressables content...");
                AddressableAssetSettings.BuildPlayerContent();

                // 7. Audit TMP elements in scene and prefabs
                AuditTextComponents();

                Debug.Log("Localization and Font Setup COMPLETED successfully! Please test Korean text in Playmode.");
                EditorUtility.DisplayDialog("Setup Successful", "Localization environment, Fallback Fonts, and Addressables configured successfully!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Setup failed with exception: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed", $"An error occurred during setup:\n{ex.Message}", "OK");
            }
        }

        private static void GenerateDynamicFontAsset(Font font, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            // Initialize FontEngine and load font face
            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(font, 90) != FontEngineError.Success)
            {
                throw new Exception($"Unable to load font face for [{font.name}]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.");
            }

            // Create new dynamic TMP_FontAsset
            TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            AssetDatabase.CreateAsset(fontAsset, outputPath);

            SetField(fontAsset, "m_Version", "1.1.0");
            SetField(fontAsset, "m_FaceInfo", FontEngine.GetFaceInfo());
            SetField(fontAsset, "m_SourceFontFile", font);
            SetField(fontAsset, "m_SourceFontFileGUID", AssetDatabase.AssetPathToGUID(FontPath));
            SetField(fontAsset, "m_SourceFontFile_EditorRef", font);
            
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            SetField(fontAsset, "m_ClearDynamicDataOnBuild", true);

            int atlasWidth = 1024;
            int atlasHeight = 1024;
            int atlasPadding = 9;

            SetField(fontAsset, "m_AtlasWidth", atlasWidth);
            SetField(fontAsset, "m_AtlasHeight", atlasHeight);
            SetField(fontAsset, "m_AtlasPadding", atlasPadding);
            SetField(fontAsset, "m_AtlasRenderMode", GlyphRenderMode.SDFAA);
            
            Texture2D texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
            texture.name = "Pretendard-Regular Atlas";
            AssetDatabase.AddObjectToAsset(texture, fontAsset);
            
            fontAsset.atlasTextures = new Texture2D[1] { texture };

            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null)
            {
                throw new Exception("Shader 'TextMeshPro/Distance Field' not found!");
            }
            Material mat = new Material(shader);
            mat.name = texture.name + " Material";
            mat.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + 1);
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            mat.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
            mat.SetTexture(ShaderUtilities.ID_MainTex, texture);
            mat.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            mat.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
            
            AssetDatabase.AddObjectToAsset(mat, fontAsset);
            fontAsset.material = mat;

            var freeRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - 1, atlasHeight - 1) };
            SetField(fontAsset, "m_FreeGlyphRects", freeRects);
            SetField(fontAsset, "m_UsedGlyphRects", new List<GlyphRect>());

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created dynamic TMP Font Asset at: {outputPath}");
        }

        private static void RenameSubAssets(TMP_FontAsset fontAsset, string fontName)
        {
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = $"{fontName}-Regular Atlas";
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontName}-Regular Atlas Material";
            }
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureFallback(TMP_FontAsset pretendard, TMP_FontAsset notoSans)
        {
            TMP_FontAsset primaryFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryFontPath);
            if (primaryFont == null)
            {
                Debug.LogWarning($"Primary font not found at {PrimaryFontPath}. Skipping fallback setup.");
                return;
            }

            if (primaryFont.fallbackFontAssetTable == null)
            {
                primaryFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            // Clean existing missing fallbacks or invalid ones
            primaryFont.fallbackFontAssetTable.RemoveAll(f => f == null);

            // Add Korean fallbacks if not present
            if (!primaryFont.fallbackFontAssetTable.Contains(pretendard))
            {
                primaryFont.fallbackFontAssetTable.Add(pretendard);
            }
            if (!primaryFont.fallbackFontAssetTable.Contains(notoSans))
            {
                primaryFont.fallbackFontAssetTable.Add(notoSans);
            }

            EditorUtility.SetDirty(primaryFont);
            AssetDatabase.SaveAssets();
            Debug.Log($"Configured fallbacks on {primaryFont.name}: added {pretendard.name} and {notoSans.name}");
        }

        private static void RegisterAddressable(string assetPath, string key, string groupName = "Common")
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) return;

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, false, settings.DefaultGroup.Schemas);
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"Asset not found at: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = key;
            
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            Debug.Log($"Registered addressable: {key} -> {assetPath}");
        }

        private static void AuditTextComponents()
        {
            Debug.Log("Auditing all TextMeshProUGUI components in prefabs...");
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game" });
            int fixedCount = 0;

            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool isDirty = false;

                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    if (tmp.font == null)
                    {
                        Debug.LogWarning($"Found missing font asset on {tmp.gameObject.name} in {path}. Restoring default font.");
                        tmp.font = TMP_Settings.defaultFontAsset;
                        isDirty = true;
                        fixedCount++;
                    }
                }

                if (isDirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"Font audit finished. Restored default font on {fixedCount} text components.");
        }

        private static void SetField(object obj, string name, object val)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(obj, val);
                    return;
                }
                type = type.BaseType;
            }
        }
    }
}
