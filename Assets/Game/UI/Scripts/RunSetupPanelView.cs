using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class RunSetupPanelView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Character Selection")]
        [SerializeField] private TextMeshProUGUI charNameText;
        [SerializeField] private TextMeshProUGUI charDescText;
        [SerializeField] private Button          charPrevButton;
        [SerializeField] private Button          charNextButton;

        [Header("Pickaxe Selection")]
        [SerializeField] private TextMeshProUGUI pickaxeNameText;
        [SerializeField] private TextMeshProUGUI pickaxeStatText;
        [SerializeField] private Button          pickaxePrevButton;
        [SerializeField] private Button          pickaxeNextButton;

        [Header("Actions")]
        [SerializeField] private Button          startRunButton;
        [SerializeField] private TextMeshProUGUI startRunButtonLabel;
        [SerializeField] private Button          backButton;

        public event Action OnStartRunClicked;
        public event Action OnBackClicked;
        public event Action OnCharPrevClicked;
        public event Action OnCharNextClicked;
        public event Action OnPickaxePrevClicked;
        public event Action OnPickaxeNextClicked;

        private void Start()
        {
            if (startRunButton  != null) startRunButton.onClick.AddListener(()   => OnStartRunClicked?.Invoke());
            if (backButton      != null) backButton.onClick.AddListener(()        => OnBackClicked?.Invoke());
            if (charPrevButton  != null) charPrevButton.onClick.AddListener(()   => OnCharPrevClicked?.Invoke());
            if (charNextButton  != null) charNextButton.onClick.AddListener(()   => OnCharNextClicked?.Invoke());
            if (pickaxePrevButton != null) pickaxePrevButton.onClick.AddListener(() => OnPickaxePrevClicked?.Invoke());
            if (pickaxeNextButton != null) pickaxeNextButton.onClick.AddListener(() => OnPickaxeNextClicked?.Invoke());
        }

        public void SetTitle(string text)
        {
            if (titleText != null) titleText.text = text;
        }

        public void SetCharacter(string name, string desc)
        {
            if (charNameText != null) charNameText.text = name;
            if (charDescText != null) charDescText.text = desc;
        }

        public void SetPickaxe(string name, string stat)
        {
            if (pickaxeNameText != null) pickaxeNameText.text = name;
            if (pickaxeStatText != null) pickaxeStatText.text = stat;
        }

        public void SetStartButtonLabel(string text)
        {
            if (startRunButtonLabel != null) startRunButtonLabel.text = text;
        }
    }
}
