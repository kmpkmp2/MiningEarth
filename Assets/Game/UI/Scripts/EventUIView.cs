using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Event;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class EventUIView : MonoBehaviour
    {
        [Header("Title & Description")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Choice Buttons")]
        [SerializeField] private List<Button> optionButtons;
        [SerializeField] private List<TextMeshProUGUI> optionTitleTexts;
        [SerializeField] private List<TextMeshProUGUI> optionDescTexts;

        public event Action<int> OnOptionSelected;

        private void Start()
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                int index = i; // Closure
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected?.Invoke(index));
                }
            }
        }

        public void PopulateEvent(EventData data)
        {
            if (titleText != null) titleText.text = LocalizationManager.Instance.GetTranslation(data.EventTitle);
            if (descriptionText != null) descriptionText.text = LocalizationManager.Instance.GetTranslation(data.EventDescription);

            // Enable buttons matching option count
            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] == null) continue;

                if (i < data.Options.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    var option = data.Options[i];

                    if (i < optionTitleTexts.Count && optionTitleTexts[i] != null)
                    {
                        optionTitleTexts[i].text = LocalizationManager.Instance.GetTranslation(option.Title);
                    }

                    if (i < optionDescTexts.Count && optionDescTexts[i] != null)
                    {
                        optionDescTexts[i].text = LocalizationManager.Instance.GetTranslation(option.Description);
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
