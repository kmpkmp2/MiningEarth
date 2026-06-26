using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    public enum PickaxeSlotAction { Buy, Equip }

    public class PickaxeSlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI miningPowerText;
        [SerializeField] private TextMeshProUGUI durabilityText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonLabel;

        private static readonly Color LockedColor    = new Color(0.10f, 0.10f, 0.14f, 0.95f);
        private static readonly Color UnlockedColor  = new Color(0.10f, 0.18f, 0.10f, 0.95f);
        private static readonly Color EquippedColor  = new Color(0.08f, 0.22f, 0.08f, 0.97f);
        private static readonly Color BtnBuy         = new Color(0.12f, 0.26f, 0.52f, 1f);
        private static readonly Color BtnBuyDisabled = new Color(0.20f, 0.20f, 0.25f, 1f);
        private static readonly Color BtnEquip       = new Color(0.13f, 0.42f, 0.13f, 1f);
        private static readonly Color BtnEquipped    = new Color(0.22f, 0.22f, 0.28f, 1f);

        public event Action<PickaxeData, PickaxeSlotAction> OnActionClicked;

        private PickaxeData _data;

        public void SetData(PickaxeData data, bool isUnlocked, bool isEquipped, bool canAfford)
        {
            _data = data;
            if (data == null) return;

            var loc = LocalizationManager.Instance;

            if (nameText)        nameText.text        = loc?.GetTranslation(data.nameLocKey) ?? data.nameLocKey;
            if (miningPowerText) miningPowerText.text = loc?.GetFormatted("shop_pickaxe_mining_power", data.miningPower) ?? $"⛏ {data.miningPower}";
            if (durabilityText)  durabilityText.text  = loc?.GetFormatted("shop_pickaxe_durability", data.baseMaxDurability) ?? $"♦ {data.baseMaxDurability}";

            if (costText)
            {
                if (isUnlocked || data.isDefault)
                    costText.text = "";
                else
                    costText.text = BuildCostString(data, loc);
            }

            if (background)
                background.color = isEquipped ? EquippedColor : (isUnlocked ? UnlockedColor : LockedColor);

            if (actionButton && actionButtonLabel)
            {
                actionButton.onClick.RemoveAllListeners();
                if (isEquipped)
                {
                    actionButtonLabel.text  = loc?.GetTranslation("shop_pickaxe_equipped") ?? "EQUIPPED";
                    actionButton.GetComponent<Image>().color = BtnEquipped;
                    actionButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    actionButtonLabel.text  = loc?.GetTranslation("shop_pickaxe_equip") ?? "EQUIP";
                    actionButton.GetComponent<Image>().color = BtnEquip;
                    actionButton.interactable = true;
                    actionButton.onClick.AddListener(() => OnActionClicked?.Invoke(_data, PickaxeSlotAction.Equip));
                }
                else
                {
                    actionButtonLabel.text  = loc?.GetTranslation("shop_pickaxe_buy") ?? "BUY";
                    actionButton.GetComponent<Image>().color = canAfford ? BtnBuy : BtnBuyDisabled;
                    actionButton.interactable = canAfford;
                    if (canAfford)
                        actionButton.onClick.AddListener(() => OnActionClicked?.Invoke(_data, PickaxeSlotAction.Buy));
                }
            }
        }

        private static string BuildCostString(PickaxeData data, LocalizationManager loc)
        {
            if (data.unlockCost == null || data.unlockCost.Count == 0) return "";
            var parts = new System.Text.StringBuilder();
            foreach (var cost in data.unlockCost)
            {
                if (parts.Length > 0) parts.Append("  ");
                string resName = loc?.GetTranslation($"item_{cost.resourceType.ToString().ToLower()}_name") ?? cost.resourceType.ToString();
                parts.Append($"{resName} x{cost.amount}");
            }
            return parts.ToString();
        }

        public static PickaxeSlotView Create(Transform parent)
        {
            var go = new GameObject("PickaxeSlot", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 130f);

            var bg = go.AddComponent<Image>();
            bg.color = LockedColor;

            // Icon placeholder
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.offsetMin = new Vector2(10f, 10f);
            iconRt.offsetMax = new Vector2(90f, -10f);
            iconGo.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

            // Info area
            var infoGo = new GameObject("Info", typeof(RectTransform));
            infoGo.transform.SetParent(go.transform, false);
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(0.7f, 1f);
            infoRt.offsetMin = new Vector2(100f, 6f);
            infoRt.offsetMax = new Vector2(0f, -6f);

            var nameTmp = AddTMP(infoGo.transform, "Name",
                new Vector2(0f, 0.68f), new Vector2(1f, 1f), Vector4.zero,
                16f, true, Color.white, TMPro.TextAlignmentOptions.Left);

            var powerTmp = AddTMP(infoGo.transform, "Power",
                new Vector2(0f, 0.38f), new Vector2(1f, 0.70f), Vector4.zero,
                13f, false, new Color(0.75f, 0.9f, 0.75f), TMPro.TextAlignmentOptions.Left);

            var durTmp = AddTMP(infoGo.transform, "Durability",
                new Vector2(0f, 0.10f), new Vector2(1f, 0.42f), Vector4.zero,
                13f, false, new Color(0.75f, 0.85f, 1f), TMPro.TextAlignmentOptions.Left);

            // Cost text (bottom left of item)
            var costGo = new GameObject("Cost", typeof(RectTransform));
            costGo.transform.SetParent(go.transform, false);
            var costRt = costGo.GetComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0f, 0f);
            costRt.anchorMax = new Vector2(0.7f, 0.30f);
            costRt.offsetMin = new Vector2(100f, 4f);
            costRt.offsetMax = new Vector2(0f, 0f);
            var costTmp = costGo.AddComponent<TextMeshProUGUI>();
            costTmp.fontSize = 11f;
            costTmp.color    = new Color(0.85f, 0.75f, 0.45f);
            costTmp.alignment = TMPro.TextAlignmentOptions.Left;
            costTmp.enableWordWrapping = false;

            // Action button (right side)
            var btnGo = new GameObject("ActionBtn", typeof(RectTransform));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.7f, 0.15f);
            btnRt.anchorMax = new Vector2(1f, 0.85f);
            btnRt.offsetMin = new Vector2(6f, 0f);
            btnRt.offsetMax = new Vector2(-10f, 0f);
            btnGo.AddComponent<Image>().color = new Color(0.20f, 0.20f, 0.25f, 1f);
            var btn = btnGo.AddComponent<Button>();

            var btnLabelGo = new GameObject("Label", typeof(RectTransform));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            Stretch(btnLabelGo.GetComponent<RectTransform>());
            var btnLabel = btnLabelGo.AddComponent<TextMeshProUGUI>();
            btnLabel.fontSize  = 14f;
            btnLabel.fontStyle = TMPro.FontStyles.Bold;
            btnLabel.color     = Color.white;
            btnLabel.alignment = TMPro.TextAlignmentOptions.Center;

            // Separator
            var sepGo = new GameObject("Sep", typeof(RectTransform));
            sepGo.transform.SetParent(go.transform, false);
            var sepRt = sepGo.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0f);
            sepRt.anchorMax = new Vector2(1f, 0.02f);
            sepRt.offsetMin = Vector2.zero;
            sepRt.offsetMax = Vector2.zero;
            sepGo.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.30f, 0.5f);

            var view             = go.AddComponent<PickaxeSlotView>();
            view.background      = bg;
            view.nameText        = nameTmp;
            view.miningPowerText = powerTmp;
            view.durabilityText  = durTmp;
            view.costText        = costTmp;
            view.actionButton    = btn;
            view.actionButtonLabel = btnLabel;

            return view;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddTMP(Transform parent, string goName,
            Vector2 anchorMin, Vector2 anchorMax, Vector4 offset,
            float size, bool bold, Color color, TMPro.TextAlignmentOptions align)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(offset.x, offset.y);
            rt.offsetMax = new Vector2(offset.z, offset.w);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = size;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            return tmp;
        }
    }
}
