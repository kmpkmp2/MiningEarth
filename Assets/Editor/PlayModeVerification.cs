using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DeepEarth.Core;
using DeepEarth.Mining;
using DeepEarth.Map;
using DeepEarth.Common;
using DeepEarth.UI;
using DeepEarth.Combat;

namespace DeepEarth.Editor
{
    [InitializeOnLoad]
    public static class PlayModeVerification
    {
        private static bool s_isTesting = false;

        static PlayModeVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Run PlayMode Verification Test")]
        public static void RunTestMenu()
        {
            UnityEngine.Debug.Log("Starting PlayMode Verification Test...");
            s_isTesting = true;
            EditorPrefs.SetBool("DeepEarth_IsTesting", true);
            
            // First load LoadingScene.unity to start the game flow from the beginning
            EditorSceneManager.OpenScene("Assets/Game/Scenes/LoadingScene.unity");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (EditorPrefs.GetBool("DeepEarth_IsTesting", false))
                {
                    EditorPrefs.SetBool("DeepEarth_IsTesting", false);
                    var go = new GameObject("PlayModeTestRunner");
                    go.AddComponent<PlayModeTestRunner>();
                }
            }
        }
    }

    public class PlayModeTestRunner : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(CoRunTest());
        }

        private IEnumerator CoRunTest()
        {
            UnityEngine.Debug.Log("TEST RUNNER: Initialized. Waiting for StartMenuScene to load...");
            
            // Wait for StartMenuScene
            float timeout = 10f;
            float elapsed = 0f;
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "StartMenuScene" && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "StartMenuScene")
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: Timeout waiting for StartMenuScene!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            UnityEngine.Debug.Log("TEST RUNNER: StartMenuScene loaded. Simulating Play button click...");
            yield return new WaitForSeconds(1.0f);

            // Find StartMenuUIView and trigger OnPlayClicked
            var startMenuView = FindAnyObjectByType<StartMenuUIView>();
            if (startMenuView == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: StartMenuUIView not found!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            // Simulate Play Click
            var playButtonField = typeof(StartMenuUIView).GetField("playButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (playButtonField != null)
            {
                var playButton = (UnityEngine.UI.Button)playButtonField.GetValue(startMenuView);
                playButton.onClick.Invoke();
            }
            else
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: playButton field not found!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            // Wait for MainGameScene
            UnityEngine.Debug.Log("TEST RUNNER: Waiting for MainGameScene...");
            elapsed = 0f;
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainGameScene" && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainGameScene")
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: Timeout waiting for MainGameScene!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            UnityEngine.Debug.Log("TEST RUNNER: MainGameScene loaded. Waiting for managers to initialize...");
            
            // Wait for manager singletons to initialize
            while (GameManager.Instance == null || StatManager.Instance == null || MapPresenter.Instance == null || ThemePresenter.Instance == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            UnityEngine.Debug.Log("TEST RUNNER: Managers initialized. Starting mining simulation...");

            int targetDepth = 55;
            
            // Test mining multiple blocks to reach depth 55 (to cross the Stone Cave boundary at 50)
            while (GameManager.Instance != null && GameManager.Instance.CurrentDepth < targetDepth)
            {
                if (GameManager.Instance.CurrentState == GameState.EventPause)
                {
                    UnityEngine.Debug.Log("TEST RUNNER: EventPause detected. Simulating event decision...");
                    var eventView = FindAnyObjectByType<EventUIView>();
                    if (eventView != null)
                    {
                        var buttonsField = typeof(EventUIView).GetField("optionButtons", BindingFlags.NonPublic | BindingFlags.Instance);
                        var buttons = (System.Collections.Generic.List<UnityEngine.UI.Button>)buttonsField?.GetValue(eventView);
                        if (buttons != null && buttons.Count > 0)
                        {
                            buttons[0].onClick.Invoke();
                        }
                    }
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }
                else if (GameManager.Instance.CurrentState == GameState.BossReward)
                {
                    UnityEngine.Debug.Log("TEST RUNNER: BossReward detected. Simulating reward selection...");
                    var rewardView = FindAnyObjectByType<BossRewardView>();
                    if (rewardView != null)
                    {
                        rewardView.SelectRewardByIndex(0);
                    }
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }
                else if (GameManager.Instance.CurrentState != GameState.Playing && GameManager.Instance.CurrentState != GameState.BossCombat)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // Check HP to prevent dying
                if (StatManager.Instance != null && StatManager.Instance.CurrentHP <= 2)
                {
                    // Heal player to prevent death during testing
                    StatManager.Instance.Heal(StatManager.Instance.GetMaxHP());
                }

                // Check if any monsters or bosses are active and attack them first
                var activeMonsters = FindObjectsByType<MonsterView>(FindObjectsSortMode.None);
                if (activeMonsters != null && activeMonsters.Length > 0)
                {
                    foreach (var monster in activeMonsters)
                    {
                        var monsterOnMouseDown = typeof(MonsterView).GetMethod("OnMouseDown", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (monsterOnMouseDown != null)
                        {
                            UnityEngine.Debug.Log($"TEST RUNNER: Attacking monster/boss: {monster.name}");
                            monsterOnMouseDown.Invoke(monster, null);
                        }
                    }
                    yield return new WaitForSeconds(0.05f); // attack fast
                    continue; // skip block mining while in combat
                }

                // Get current block object and presenter
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    var miningSystem = MiningSystem.Instance;
                    if (miningSystem == null)
                    {
                        UnityEngine.Debug.LogError("TEST RUNNER FAILED: MiningSystem not found!");
                        EditorApplication.isPlaying = false;
                        yield break;
                    }

                    var blockPresenterField = typeof(MiningSystem).GetField("_currentBlockPresenter", BindingFlags.NonPublic | BindingFlags.Instance);
                    var blockPresenter = (BlockPresenter)blockPresenterField?.GetValue(miningSystem);

                    if (blockPresenter != null && blockPresenter.View != null && blockPresenter.Model != null && !blockPresenter.Model.IsDestroyed)
                    {
                        var blockView = blockPresenter.View;
                        var onMouseDownMethod = typeof(BlockView).GetMethod("OnMouseDown", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (onMouseDownMethod != null)
                        {
                            int currentHp = StatManager.Instance != null ? StatManager.Instance.CurrentHP : 0;
                            int maxHp = StatManager.Instance != null ? StatManager.Instance.GetMaxHP() : 0;
                            int currentDepth = GameManager.Instance != null ? GameManager.Instance.CurrentDepth : 0;
                            int mapDepth = MapPresenter.Instance != null && MapPresenter.Instance.Model != null ? MapPresenter.Instance.Model.CurrentDepth : 0;

                            UnityEngine.Debug.Log($"TEST RUNNER: Mining block. Depth: {currentDepth}, Hits Left: {blockPresenter.Model.CurrentHits}, MapRoot Z: {mapDepth}, Player HP: {currentHp}/{maxHp}");
                            onMouseDownMethod.Invoke(blockView, null);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("TEST RUNNER FAILED: OnMouseDown not found on BlockView!");
                            EditorApplication.isPlaying = false;
                            yield break;
                        }
                    }
                }

                yield return new WaitForSeconds(0.05f); // Mine fast
            }

            UnityEngine.Debug.Log($"TEST RUNNER: Target depth {targetDepth} reached successfully! Checking theme updates...");
            yield return new WaitForSeconds(0.5f);

            // Test RelicPopup
            UnityEngine.Debug.Log("TEST RUNNER: Verifying RelicPopup functionality...");
            var gameUIView = FindAnyObjectByType<GameUIView>();
            if (gameUIView == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: GameUIView not found!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            var relicBtnField = typeof(GameUIView).GetField("relicButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (relicBtnField == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: relicButton field not found in GameUIView!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            var relicBtn = (UnityEngine.UI.Button)relicBtnField.GetValue(gameUIView);
            if (relicBtn == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: relicButton is null!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            UnityEngine.Debug.Log("TEST RUNNER: Clicking RelicButton...");
            relicBtn.onClick.Invoke();

            yield return new WaitForSeconds(0.5f);

            var relicPopupView = FindAnyObjectByType<RelicPopupView>(FindObjectsInactive.Include);
            if (relicPopupView == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: RelicPopupView not found in scene!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            var popupRootField = typeof(RelicPopupView).GetField("popupRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            var popupRoot = (GameObject)popupRootField?.GetValue(relicPopupView);
            if (popupRoot == null || !popupRoot.activeSelf)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: RelicPopup popupRoot is not active after click!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            UnityEngine.Debug.Log("TEST RUNNER: RelicPopup active. Simulating CloseButton click...");
            var closeBtnField = typeof(RelicPopupView).GetField("closeButton", BindingFlags.NonPublic | BindingFlags.Instance);
            var closeBtn = (UnityEngine.UI.Button)closeBtnField?.GetValue(relicPopupView);
            if (closeBtn == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: closeButton not found on RelicPopupView!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            closeBtn.onClick.Invoke();

            yield return new WaitForSeconds(0.5f);

            if (popupRoot.activeSelf)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: RelicPopup popupRoot is still active after clicking close!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            UnityEngine.Debug.Log("TEST RUNNER: RelicPopup closed successfully. Verifying theme updates...");

            // Verify Theme
            if (ThemePresenter.Instance == null || ThemePresenter.Instance.Model == null)
            {
                UnityEngine.Debug.LogError("TEST RUNNER FAILED: ThemePresenter.Instance or Model is null!");
                EditorApplication.isPlaying = false;
                yield break;
            }

            string currentTheme = ThemePresenter.Instance.Model.ThemeKey;
            UnityEngine.Debug.Log($"TEST RUNNER: Active theme key: {currentTheme}");
            if (currentTheme != AddressableKeys.ThemeStone)
            {
                UnityEngine.Debug.LogError($"TEST RUNNER FAILED: Theme is incorrect! Expected Theme_Stone (depth {targetDepth}), got {currentTheme}");
                EditorApplication.isPlaying = false;
                yield break;
            }

            // Verify MapRoot translation
            if (MapPresenter.Instance != null && MapPresenter.Instance.Model != null)
            {
                float mapRootZ = MapPresenter.Instance.Model.CurrentDepth;
                UnityEngine.Debug.Log($"TEST RUNNER: MapRoot position: {MapPresenter.Instance.InstanceMapRootPosition()}");
            }
            
            UnityEngine.Debug.Log("=================================================");
            UnityEngine.Debug.Log("PLAYMODE VERIFICATION COMPLETED AND PASSED SUCCESSFULLY!");
            UnityEngine.Debug.Log("=================================================");

            yield return new WaitForSeconds(1.0f);
            EditorApplication.isPlaying = false;
            yield return new WaitForSeconds(1.0f);
            EditorApplication.Exit(0);
        }
    }

    public static class MapPresenterExtensions
    {
        public static string InstanceMapRootPosition(this MapPresenter presenter)
        {
            var viewField = typeof(MapPresenter).GetField("_view", BindingFlags.NonPublic | BindingFlags.Instance);
            var view = (MapView)viewField?.GetValue(presenter);
            if (view != null)
            {
                var mapRootField = typeof(MapView).GetField("mapRoot", BindingFlags.NonPublic | BindingFlags.Instance);
                var mapRoot = (Transform)mapRootField?.GetValue(view);
                if (mapRoot != null)
                {
                    return mapRoot.position.ToString();
                }
            }
            return "Unknown";
        }
    }
}
