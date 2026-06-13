using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class BossView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI bossTitleText; // Literal "BOSS"
        [SerializeField] private TextMeshProUGUI bossNameText;  // Boss name
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;        // e.g., "20 / 20"
        [SerializeField] private TextMeshProUGUI shieldText;    // e.g., "(Shield: 10 / 10)"

        public void Initialize()
        {
            if (bossTitleText != null)
            {
                bossTitleText.text = LocalizationManager.Instance.GetTranslation("boss_title");
            }
            if (shieldText != null)
            {
                shieldText.gameObject.SetActive(false);
            }
        }

        public void SetBossName(string nameKey)
        {
            if (bossNameText != null)
            {
                bossNameText.text = LocalizationManager.Instance.GetTranslation(nameKey);
            }
        }

        public void SetHP(int current, int max)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;
            }

            if (hpText != null)
            {
                hpText.text = $"{current} / {max}";
            }
        }

        public void SetShield(int currentShield, int maxShield)
        {
            if (shieldText != null)
            {
                if (currentShield > 0)
                {
                    shieldText.gameObject.SetActive(true);
                    shieldText.text = $"(Shield: {currentShield} / {maxShield})";
                }
                else
                {
                    shieldText.gameObject.SetActive(false);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
