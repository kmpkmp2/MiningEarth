using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class AchievementManager : MonoBehaviour
    {
        private static AchievementManager _instance;
        public static AchievementManager Instance => _instance;

        private const string AchievementLabel = "Achievement";

        // OreMined는 채굴 블록 1개당 1회 발화하는 고빈도 이벤트라, 진행 틱마다 즉시 저장하면
        // 연속 채굴 시 매번 전체 SaveData를 동기 디스크 쓰기하게 되어 프레임 히치를 유발할 수 있다.
        // 그 경로에서만 저장을 배치(디바운스)하고, 완료(NotifyCompleted) 및 그 외 이벤트는 항상 즉시 저장한다.
        private const float SaveDebounceInterval = 2f;

        private readonly List<AchievementModel> _models = new List<AchievementModel>();
        private bool _eventsSubscribed;
        private bool _saveDirty;
        private float _saveDirtyTimer;

        public event Action<AchievementModel> OnAchievementCompleted;
        public event Action OnProgressUpdated;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            FlushPendingSave();
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (!_saveDirty) return;
            _saveDirtyTimer += Time.deltaTime;
            if (_saveDirtyTimer >= SaveDebounceInterval)
                FlushPendingSave();
        }

        // 모바일 백그라운드 전환/앱 종료 시 배치 대기 중인 진행도가 유실되지 않도록 강제 반영한다.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) FlushPendingSave();
        }

        private void OnApplicationQuit()
        {
            FlushPendingSave();
        }

        // ── Initialization ───────────────────────────────────────────────────
        public async UniTask InitializeAsync()
        {
            var allData = await ResourceManager.Instance.LoadAllByLabelAsync<AchievementData>(AchievementLabel);

            _models.Clear();
            foreach (var data in allData)
            {
                if (data == null || string.IsNullOrEmpty(data.achievementID)) continue;
                var saved = FindSaveEntry(data.achievementID);
                _models.Add(new AchievementModel(data, saved?.CurrentProgress ?? 0, saved?.IsCompleted ?? false));
            }

            SubscribeEvents();

            Debug.Log($"[Achievement]\nInitialized\nLoaded : {_models.Count}");
        }

        // ── Event subscriptions ──────────────────────────────────────────────
        private void SubscribeEvents()
        {
            if (_eventsSubscribed) return;
            _eventsSubscribed = true;

            GameEvents.OnMonsterKilled      += HandleMonsterKilled;
            GameEvents.OnOreMined           += HandleOreMined;
            GameEvents.OnBossKilled         += HandleBossKilled;
            GameEvents.OnDepthReached       += HandleDepthReached;
            GameEvents.OnPickaxeRepaired    += HandlePickaxeRepaired;
            GameEvents.OnRepairWithOre      += HandleRepairWithOre;
            GameEvents.OnCharacterUnlocked  += HandleCharacterUnlocked;
            GameEvents.OnRelicCollected     += HandleRelicCollected;
            GameEvents.OnRunStarted         += HandleRunStarted;
            GameEvents.OnPlayerDied         += HandlePlayerDied;
            GameEvents.OnTreasureOpened     += HandleTreasureOpened;
            GameEvents.OnTombstoneOpened    += HandleTombstoneOpened;
            GameEvents.OnLavaEncountered    += HandleLavaEncountered;
            GameEvents.OnWaterEncountered   += HandleWaterEncountered;
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;
            _eventsSubscribed = false;

            GameEvents.OnMonsterKilled      -= HandleMonsterKilled;
            GameEvents.OnOreMined           -= HandleOreMined;
            GameEvents.OnBossKilled         -= HandleBossKilled;
            GameEvents.OnDepthReached       -= HandleDepthReached;
            GameEvents.OnPickaxeRepaired    -= HandlePickaxeRepaired;
            GameEvents.OnRepairWithOre      -= HandleRepairWithOre;
            GameEvents.OnCharacterUnlocked  -= HandleCharacterUnlocked;
            GameEvents.OnRelicCollected     -= HandleRelicCollected;
            GameEvents.OnRunStarted         -= HandleRunStarted;
            GameEvents.OnPlayerDied         -= HandlePlayerDied;
            GameEvents.OnTreasureOpened     -= HandleTreasureOpened;
            GameEvents.OnTombstoneOpened    -= HandleTombstoneOpened;
            GameEvents.OnLavaEncountered    -= HandleLavaEncountered;
            GameEvents.OnWaterEncountered   -= HandleWaterEncountered;
        }

        // ── Event handlers ───────────────────────────────────────────────────
        private void HandleMonsterKilled()     => UpdateByType(AchievementType.MonsterKill);
        private void HandlePickaxeRepaired()   => UpdateByType(AchievementType.PickaxeRepair);
        private void HandleRelicCollected()    => UpdateByType(AchievementType.CollectRelic);
        private void HandleRunStarted()        => UpdateByType(AchievementType.RunCount);
        private void HandlePlayerDied()        => UpdateByType(AchievementType.DeathCount);
        private void HandleTreasureOpened()    => UpdateByType(AchievementType.TreasureOpen);
        private void HandleTombstoneOpened()   => UpdateByType(AchievementType.TombstoneOpen);
        private void HandleLavaEncountered()   => UpdateByType(AchievementType.LavaEncounter);
        private void HandleWaterEncountered()  => UpdateByType(AchievementType.WaterEncounter);

        private void HandleOreMined(BlockType type, int amount)
            => UpdateMatching(AchievementType.OreMined, amount, m => m.Data.targetOreType == type, allowDeferredSave: true);

        private void HandleBossKilled(string bossID)
            => UpdateMatching(AchievementType.BossKill, 1,
                m => string.IsNullOrEmpty(m.Data.targetBossID) || m.Data.targetBossID == bossID);

        private void HandleDepthReached(int depth)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != AchievementType.ReachDepth) continue;
                if (depth > model.CurrentProgress)
                    AddProgress(model, depth - model.CurrentProgress);
            }
        }

        private void HandleRepairWithOre(BlockType oreType)
            => UpdateMatching(AchievementType.RepairWithOre, 1, m => m.Data.targetOreType == oreType);

        private void HandleCharacterUnlocked(CharacterID id)
        {
            UpdateByType(AchievementType.UnlockCharacter);
        }

        // ── Progress helpers ─────────────────────────────────────────────────
        private void UpdateByType(AchievementType type, int amount = 1)
            => UpdateMatching(type, amount, null);

        // HandleOreMined/HandleBossKilled/HandleRepairWithOre/UpdateByType가 공유하던
        // "완료된 항목 건너뛰기 + 타입 일치 + (선택적) 추가 조건" foreach 패턴을 통합한 헬퍼.
        private void UpdateMatching(AchievementType type, int amount, Func<AchievementModel, bool> matches, bool allowDeferredSave = false)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != type) continue;
                if (matches != null && !matches(model)) continue;
                AddProgress(model, amount, allowDeferredSave);
            }
        }

        // allowDeferredSave: OreMined처럼 고빈도로 호출되는 경로에서만 true로 넘겨 저장을 배치한다.
        // 완료된 경우(completed)는 allowDeferredSave 값과 무관하게 항상 즉시 저장한다.
        private void AddProgress(AchievementModel model, int amount, bool allowDeferredSave = false)
        {
            bool completed = model.AddProgress(amount);
            LogProgress(model);

            if (completed || !allowDeferredSave)
                SaveProgress();
            else
                MarkSaveDirty();

            OnProgressUpdated?.Invoke();
            if (completed) NotifyCompleted(model);
        }

        private void MarkSaveDirty()
        {
            _saveDirty = true;
            _saveDirtyTimer = 0f;
        }

        private void FlushPendingSave()
        {
            if (_saveDirty) SaveProgress();
        }

        private void LogProgress(AchievementModel model)
        {
            Debug.Log($"[Achievement]\nProgress Updated\nType : {model.Data.type}\nCurrent : {model.CurrentProgress}\nTarget : {model.Data.targetValue}");
        }

        private void NotifyCompleted(AchievementModel model)
        {
            Debug.Log($"[Achievement]\nCompleted\nID : {model.Data.achievementID}\nReward : {model.Data.rewardType}");
            OnAchievementCompleted?.Invoke(model);
        }

        // ── Save / Load ──────────────────────────────────────────────────────
        private void SaveProgress()
        {
            var list = SaveManager.CurrentData.AchievementProgress;
            list.Clear();
            foreach (var model in _models)
            {
                list.Add(new AchievementSaveEntry
                {
                    AchievementID  = model.Data.achievementID,
                    CurrentProgress = model.CurrentProgress,
                    IsCompleted    = model.IsCompleted
                });
            }
            SaveManager.Save();

            _saveDirty = false;
            _saveDirtyTimer = 0f;
        }

        private AchievementSaveEntry FindSaveEntry(string id)
        {
            return SaveManager.CurrentData.AchievementProgress
                   ?.Find(e => e.AchievementID == id);
        }

        // ── Public queries ───────────────────────────────────────────────────
        public List<AchievementModel> GetAllAchievements() => new List<AchievementModel>(_models);

        public int CompletedCount
        {
            get
            {
                int n = 0;
                foreach (var m in _models) if (m.IsCompleted) n++;
                return n;
            }
        }
    }
}
