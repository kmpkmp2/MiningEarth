using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using DeepEarth.UI;

namespace DeepEarth.Editor
{
    public static class CheckMainSceneUI
    {
        [MenuItem("Tools/Debug MainGameScene UI")]
        public static void DebugUI()
        {
            Debug.Log("--- STARTING PREFAB DIAGNOSIS ---");
            
            // 1. Check HUD Prefab
            string hudPrefabPath = "Assets/Game/UI/Prefabs/UI_Panel_HUD.prefab";
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);
            if (hudPrefab == null)
            {
                Debug.LogError($"[DIAG] HUD Prefab not found at {hudPrefabPath}!");
            }
            else
            {
                Debug.Log($"[DIAG] Loaded HUD Prefab successfully.");
                var hudView = hudPrefab.GetComponent<GameUIView>();
                if (hudView == null)
                {
                    Debug.LogError("[DIAG] GameUIView component not found on HUD Prefab!");
                }
                else
                {
                    CheckButton(hudView, "relicButton");
                    CheckButton(hudView, "inventoryButton");
                    CheckButton(hudView, "settingsButton");
                }
            }

            // 2. Check RelicPopup Prefab
            string relicPrefabPath = "Assets/Game/UI/Prefabs/UI_Panel_RelicPopup.prefab";
            GameObject relicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relicPrefabPath);
            if (relicPrefab == null)
            {
                Debug.LogError($"[DIAG] RelicPopup Prefab not found at {relicPrefabPath}!");
            }
            else
            {
                var view = relicPrefab.GetComponent<RelicPopupView>();
                if (view == null)
                {
                    Debug.LogError("[DIAG] RelicPopupView component not found on RelicPopup Prefab!");
                }
                else
                {
                    var closeBtnField = typeof(RelicPopupView).GetField("closeButton", BindingFlags.NonPublic | BindingFlags.Instance);
                    var closeBtn = (Button)closeBtnField?.GetValue(view);
                    Debug.Log($"[DIAG] RelicPopup closeButton present: {closeBtn != null}");
                }
            }

            // 3. Check InventoryPopup Prefab
            string invPrefabPath = "Assets/Game/UI/Prefabs/UI_Panel_InventoryPopup.prefab";
            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(invPrefabPath);
            if (invPrefab == null)
            {
                Debug.LogError($"[DIAG] InventoryPopup Prefab not found at {invPrefabPath}!");
            }
            else
            {
                var view = invPrefab.GetComponent<InventoryPopupView>();
                if (view == null)
                {
                    Debug.LogError("[DIAG] InventoryPopupView component not found on InventoryPopup Prefab!");
                }
                else
                {
                    var closeBtnField = typeof(InventoryPopupView).GetField("closeButton", BindingFlags.NonPublic | BindingFlags.Instance);
                    var closeBtn = (Button)closeBtnField?.GetValue(view);
                    Debug.Log($"[DIAG] InventoryPopup closeButton present: {closeBtn != null}");
                }
            }

            Debug.Log("--- ENDING PREFAB DIAGNOSIS ---");
        }

        private static void CheckButton(object view, string fieldName)
        {
            var field = view.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                Debug.LogError($"[DIAG] Button field {fieldName} not found in GameUIView!");
                return;
            }
            var btn = (Button)field.GetValue(view);
            if (btn == null)
            {
                Debug.LogError($"[DIAG] Button {fieldName} reference is NULL on prefab!");
            }
            else
            {
                Debug.Log($"[DIAG] Button {fieldName}: Name={btn.gameObject.name}, Interactable={btn.interactable}");
            }
        }
    }
}
