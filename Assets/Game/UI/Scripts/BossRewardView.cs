using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Combat;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class BossRewardView : MonoBehaviour
    {
        [Header("Title Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Choice Cards")]
        [SerializeField] private List<Button> cardButtons;
        [SerializeField] private List<TextMeshProUGUI> cardTitleTexts;
        [SerializeField] private List<TextMeshProUGUI> cardDescTexts;
        [SerializeField] private List<Image> cardBackgrounds;

        public event Action<int> OnRewardSelected;

        private void Start()
        {
            for (int i = 0; i < cardButtons.Count; i++)
            {
                int index = i; // Closure
                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.AddListener(() => OnRewardSelected?.Invoke(index));
                }
            }
        }

        public void PopulateRewards(List<BossRewardChoice> choices)
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Instance.GetTranslation("boss_reward_title");
            }
            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.Instance.GetTranslation("boss_reward_subtitle");
            }

            for (int i = 0; i < cardButtons.Count; i++)
            {
                if (cardButtons[i] == null) continue;

                if (i < choices.Count)
                {
                    cardButtons[i].gameObject.SetActive(true);
                    var choice = choices[i];

                    if (i < cardTitleTexts.Count && cardTitleTexts[i] != null)
                    {
                        cardTitleTexts[i].text = LocalizationManager.Instance.GetTranslation(choice.TitleKey);
                    }

                    if (i < cardDescTexts.Count && cardDescTexts[i] != null)
                    {
                        cardDescTexts[i].text = LocalizationManager.Instance.GetTranslation(choice.DescriptionKey);
                    }

                    // Style card based on rarity
                    if (i < cardBackgrounds.Count && cardBackgrounds[i] != null)
                    {
                        if (choice.IsRare)
                        {
                            // Glowing deep purple-gold for rare rewards
                            cardBackgrounds[i].color = new Color(0.35f, 0.22f, 0.45f, 1f);
                            if (i < cardTitleTexts.Count && cardTitleTexts[i] != null)
                            {
                                cardTitleTexts[i].color = new Color(1f, 0.85f, 0.3f); // Gold Title
                            }
                        }
                        else
                        {
                            // Slate grey for normal rewards
                            cardBackgrounds[i].color = new Color(0.18f, 0.2f, 0.23f, 1f);
                            if (i < cardTitleTexts.Count && cardTitleTexts[i] != null)
                            {
                                cardTitleTexts[i].color = Color.white;
                            }
                        }
                    }
                }
                else
                {
                    cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void SelectRewardByIndex(int index)
        {
            if (index >= 0 && index < cardButtons.Count && cardButtons[index] != null && cardButtons[index].gameObject.activeSelf)
            {
                cardButtons[index].onClick.Invoke();
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
