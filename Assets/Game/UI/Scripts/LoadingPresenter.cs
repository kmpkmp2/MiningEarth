using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    /// <summary>
    /// LoadingScene의 두 가지 모드를 처리하는 Presenter.
    ///   · RunSetupContext.IsRunSetupComplete == false → 앱 최초 시작 (StartMenuScene 진입)
    ///   · RunSetupContext.IsRunSetupComplete == true  → 런 초기화 (MainGameScene 진입)
    /// </summary>
    public class LoadingPresenter
    {
        private readonly LoadingPanelView _view;
        private readonly LoadingModel     _model;

        public LoadingPresenter(LoadingPanelView view)
        {
            _view  = view;
            _model = new LoadingModel();
            _model.OnChanged += SyncView;
        }

        public async UniTask ExecuteAsync()
        {
            if (RunSetupContext.IsRunSetupComplete)
                await RunInitAsync();
            else
                await AppStartupAsync();
        }

        // ── 앱 시작 초기화 ──────────────────────────────────────────
        private async UniTask AppStartupAsync()
        {
            float start = Time.time;

            _model.SetProgress(0f, Loc("loading_init") ?? "Loading...");
            await UniTask.Yield();

            _model.SetProgress(0.2f, Loc("loading_assets") ?? "Initializing assets...");
            await Addressables.InitializeAsync().ToUniTask();

            _model.SetProgress(0.5f, Loc("loading_save") ?? "Loading save data...");
            SaveManager.Load();
            await UniTask.Delay(200);

            _model.SetProgress(0.8f, Loc("loading_preparing") ?? "Preparing...");
            _ = MetaProgressionManager.Instance;
            _ = LocalizationManager.Instance;
            await UniTask.Delay(200);

            EnsureMinDisplay(start, 0.5f);

            _model.SetProgress(1f, Loc("loading_ready") ?? "Ready!");
            await UniTask.Delay(200);

            SceneManager.LoadScene(SceneNames.StartMenu);
        }

        // ── 런 초기화 (14단계) ─────────────────────────────────────
        private async UniTask RunInitAsync()
        {
            float start   = Time.time;
            const int N   = 14;
            const float MinDisplaySec = 0.3f;
            int step = 0;

            // 팁 표시
            _view?.SetTip(Loc("loading_tip_cave") ?? "The deeper you go, the stronger the monsters...");

            // 1. PlayerData 로드
            Step(ref step, N, Loc("loading_step_playerdata") ?? "Loading player data...");
            SaveManager.Load();
            await UniTask.Delay(50);

            // 2. RunData 생성
            Step(ref step, N, Loc("loading_step_rundata") ?? "Preparing run data...");
            var runData = RunDataModel.Create(RunSetupContext.SelectedCharacter, RunSetupContext.SelectedPickaxeID);
            await UniTask.Delay(50);

            // 3. 캐릭터 적용
            Step(ref step, N, Loc("loading_step_character") ?? "Applying character...");
            SaveManager.CurrentData.SelectedCharacterID = RunSetupContext.SelectedCharacter;
            await UniTask.Delay(50);

            // 4. 곡괭이 적용
            Step(ref step, N, Loc("loading_step_pickaxe") ?? "Equipping pickaxe...");
            SaveManager.CurrentData.EquippedPickaxeID = RunSetupContext.SelectedPickaxeID;
            if (PickaxeManager.Instance != null)
                await PickaxeManager.Instance.InitializeAsync();
            await UniTask.Delay(50);

            // 5. 시작 아이템 지급 (현재 미구현)
            Step(ref step, N, Loc("loading_step_items") ?? "Preparing starting items...");
            await UniTask.Delay(50);

            // 6. 캐릭터 패시브 적용 (StatManager.ResetStatsForRun 내에서 처리)
            Step(ref step, N, Loc("loading_step_passive") ?? "Applying character passive...");
            await UniTask.Delay(50);

            // 7. 기본 스탯 계산
            Step(ref step, N, Loc("loading_step_stats") ?? "Calculating base stats...");
            ComputeAndStoreStats(runData);
            await UniTask.Delay(50);

            // 8. 런 전용 데이터 초기화
            Step(ref step, N, Loc("loading_step_runreset") ?? "Initializing run state...");
            InventoryManager.Instance?.ClearRunInventory();
            EffectManager.Instance?.ClearRunEffects();
            StatusEffectManager.Instance?.ClearAll();
            RelicManager.Instance?.ClearAll();
            await UniTask.Delay(50);

            // 9. Addressables 에셋 프리로드
            Step(ref step, N, Loc("loading_step_assets") ?? "Loading assets...");
            await PreloadAddressablesAsync();

            // 10. 오브젝트 풀 초기화 (PoolSystem은 GameBootstrap에서 처리)
            Step(ref step, N, Loc("loading_step_pools") ?? "Setting up pools...");
            await UniTask.Delay(50);

            // 11. 번역 초기화
            Step(ref step, N, Loc("loading_step_locale") ?? "Loading localization...");
            _ = LocalizationManager.Instance;
            await UniTask.Delay(50);

            // 12. 사운드 초기화 (미구현)
            Step(ref step, N, Loc("loading_step_sound") ?? "Loading sounds...");
            await UniTask.Delay(50);

            // 13. 저장 데이터 검증
            Step(ref step, N, Loc("loading_step_verify") ?? "Verifying save data...");
            ValidateSaveData();
            await UniTask.Delay(50);

            // 14. MainGameScene 로드
            Step(ref step, N, Loc("loading_step_enter") ?? "Entering the mine...");
            RunSetupContext.IsInitializedByLoadingScene = true;
            RunSetupContext.IsRunSetupComplete          = false;

            // 최소 표시 시간 보장
            float elapsed = Time.time - start;
            if (elapsed < MinDisplaySec)
                await UniTask.Delay(Mathf.RoundToInt((MinDisplaySec - elapsed) * 1000));

            _model.SetProgress(1f, Loc("loading_ready") ?? "Ready!");
            await UniTask.Delay(300);

            SceneManager.LoadScene(SceneNames.MainGame);
        }

        // ── 헬퍼 ───────────────────────────────────────────────────
        private void Step(ref int step, int total, string text)
        {
            step++;
            _model.SetProgress((float)step / total, text);
        }

        private void SyncView() => _view?.SetProgress(_model.Progress, _model.StatusText);

        private static string Loc(string key) => LocalizationManager.Instance?.GetTranslation(key);

        private static void ComputeAndStoreStats(RunDataModel runData)
        {
            var meta       = MetaProgressionManager.Instance;
            var pkxMgr     = PickaxeManager.Instance;
            var charID     = RunSetupContext.SelectedCharacter;
            var staticData = CharacterDatabase.Get(charID);

            int baseHPBonus  = staticData?.BaseHPBonus ?? 0;
            int baseAtkBonus = staticData?.BaseAttackPowerBonus ?? 0;
            int baseMinBonus = staticData?.BaseMiningPowerBonus ?? 0;

            int maxHP = 10 + (meta != null ? (meta.MaxHPLevel - 1) * 2 : 0) + baseHPBonus;
            int atk   = 1 + (meta != null ? meta.AttackLevel - 1 : 0) + baseAtkBonus;
            int min   = (pkxMgr?.GetEquippedMiningPower() ?? 1)
                          + (meta != null ? meta.MiningPowerLevel - 1 : 0) + baseMinBonus;
            int invSz = 24 + (meta != null ? meta.InventorySizeLevel * 4 : 0);
            int dur   = pkxMgr != null ? pkxMgr.GetFinalMaxDurability(pkxMgr.EquippedPickaxeData) : 100;

            runData.ApplyStats(maxHP, min, atk, dur, invSz);
        }

        private static async UniTask PreloadAddressablesAsync()
        {
            var keys = new List<string>
            {
                AddressableKeys.UIPanelHUD,
                AddressableKeys.UIPanelGameOver,
                AddressableKeys.UIPanelEvent,
                AddressableKeys.UIPanelSettings,
                AddressableKeys.UIPanelRelicPopup,
                AddressableKeys.UIPanelInventoryPopup,
                AddressableKeys.UIPanelEventReveal,
            };
            foreach (var key in keys)
            {
                try { await ResourceManager.Instance.LoadAssetAsync<GameObject>(key); }
                catch (Exception e) { Debug.LogWarning($"[Loading] Preload failed: {key} — {e.Message}"); }
            }
        }

        private static void ValidateSaveData()
        {
            var d = SaveManager.CurrentData;
            if (d.UnlockedPickaxeIDs == null || d.UnlockedPickaxeIDs.Count == 0)
                d.UnlockedPickaxeIDs = new System.Collections.Generic.List<string> { "pickaxe_wood" };
            if (string.IsNullOrEmpty(d.EquippedPickaxeID))
                d.EquippedPickaxeID = "pickaxe_wood";
        }

        private static void EnsureMinDisplay(float startTime, float minSeconds)
        {
            // UniTask 외부에서 사용 — 이 메서드는 void이므로 await 불가.
            // 실제 대기는 호출 측에서 처리.
        }
    }
}
