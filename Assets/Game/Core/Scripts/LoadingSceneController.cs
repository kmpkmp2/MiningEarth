using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using TMPro;
using Cysharp.Threading.Tasks;

namespace DeepEarth.Core
{
    public class LoadingSceneController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;

        private void Start()
        {
            StartLoadingAsync().Forget();
        }

        private async UniTaskVoid StartLoadingAsync()
        {
            SetProgress(0f, "Initializing systems...");

            // Initialize global managers
            var meta = MetaProgressionManager.Instance;
            var loc = LocalizationManager.Instance;
            
            await UniTask.Delay(300);
            SetProgress(0.2f, "Initializing Addressables...");

            // Initialize Addressables
            var initHandle = Addressables.InitializeAsync();
            await initHandle.ToUniTask();

            await UniTask.Delay(300);
            SetProgress(0.5f, "Loading save data...");

            // SaveManager.Load is already done by MetaProgressionManager, but let's be explicit
            SaveManager.Load();

            await UniTask.Delay(300);
            SetProgress(0.8f, "Loading game assets...");

            // Simulate loading visual assets
            await UniTask.Delay(400);
            SetProgress(1.0f, "Ready!");
            await UniTask.Delay(200);

            // Load Start Menu Scene
            SceneManager.LoadScene("StartMenuScene");
        }

        private void SetProgress(float value, string statusText)
        {
            if (progressSlider != null)
            {
                progressSlider.value = value;
            }
            if (progressText != null)
            {
                progressText.text = statusText;
            }
        }
    }
}
