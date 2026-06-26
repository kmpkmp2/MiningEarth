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

        private readonly List<AchievementModel> _models = new List<AchievementModel>();
        private bool _eventsSubscribed;

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
            UnsubscribeEvents();
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
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != AchievementType.OreMined) continue;
                if (model.Data.targetOreType != type) continue;
                AddProgress(model, amount);
            }
        }

        private void HandleBossKilled(string bossID)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != AchievementType.BossKill) continue;
                bool matchesBoss = string.IsNullOrEmpty(model.Data.targetBossID)
                                   || model.Data.targetBossID == bossID;
                if (!matchesBoss) continue;
                AddProgress(model, 1);
            }
        }

        private void HandleDepthReached(int depth)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != AchievementType.ReachDepth) continue;
                if (depth > model.CurrentProgress)
                {
                    bool completed = model.SetProgress(depth);
                    LogProgress(model);
                    SaveProgress();
                    OnProgressUpdated?.Invoke();
                    if (completed) NotifyCompleted(model);
                }
            }
        }

        private void HandleRepairWithOre(BlockType oreType)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != AchievementType.RepairWithOre) continue;
                if (model.Data.targetOreType != oreType) continue;
                AddProgress(model, 1);
            }
        }

        private void HandleCharacterUnlocked(CharacterID id)
        {
            UpdateByType(AchievementType.UnlockCharacter);
        }

        // ── Progress helpers ─────────────────────────────────────────────────
        private void UpdateByType(AchievementType type, int amount = 1)
        {
            foreach (var model in _models)
            {
                if (model.IsCompleted || model.Data.type != type) continue;
                AddProgress(model, amount);
            }
        }

        private void AddProgress(AchievementModel model, int amount)
        {
            bool completed = model.AddProgress(amount);
            LogProgress(model);
            SaveProgress();
            OnProgressUpdated?.Invoke();
            if (completed) NotifyCompleted(model);
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
