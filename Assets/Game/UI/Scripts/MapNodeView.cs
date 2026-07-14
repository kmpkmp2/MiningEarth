using System;
using UnityEngine;
using UnityEngine.UI;
using DeepEarth.Map;

namespace DeepEarth.UI
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private Button     button;
        [SerializeField] private Image      backgroundImage;
        [SerializeField] private Image      iconImage;
        [SerializeField] private GameObject completedMark;
        [SerializeField] private GameObject lockedOverlay;

        [SerializeField] private Color accessibleColor = Color.white;
        [SerializeField] private Color completedColor  = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color lockedColor     = new Color(0.25f, 0.25f, 0.25f, 1f);

        public event Action<string> OnClicked;
        public string NodeKey { get; private set; }

        private void Awake()
        {
            button?.onClick.AddListener(HandleClick);
        }

        public void Setup(MapNode data, Sprite icon, bool isAccessible, bool isCompleted)
        {
            NodeKey = $"{data.Floor}_{data.Column}";

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null && data.RoomType != RoomType.Mine);
            }

            UpdateVisuals(isAccessible, isCompleted);
        }

        public void Refresh(MapNode data, bool isAccessible, bool isCompleted)
        {
            UpdateVisuals(isAccessible, isCompleted);
        }

        private void UpdateVisuals(bool isAccessible, bool isCompleted)
        {
            bool locked = !isAccessible && !isCompleted;

            completedMark?.SetActive(isCompleted);
            lockedOverlay?.SetActive(locked);

            if (button != null)
                button.interactable = isAccessible && !isCompleted;

            if (backgroundImage != null)
            {
                backgroundImage.color = isCompleted ? completedColor
                                      : locked      ? lockedColor
                                      :               accessibleColor;
            }
        }

        private void HandleClick() => OnClicked?.Invoke(NodeKey);

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(HandleClick);
        }
    }
}
