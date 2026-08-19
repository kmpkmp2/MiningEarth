using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Audio;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    public class GameUIPresenter
    {
        private readonly GameUIView _view;
        private readonly GameManager _gameManager;

        // 저체력 경고음(Heartbeat) — HP 30% 이하 구간에서만 루프 재생. 1~30% 구간을 선형 보간해
        // 체력이 낮을수록 재생 속도(pitch)를 가속하며, HP 1%일 때 2배속에 도달한다.
        private const float HeartbeatHpThresholdPercent = 30f;
        private const float HeartbeatMinHpPercent = 1f;
        private const float HeartbeatMaxPitch = 2f;
        private bool _heartbeatActive;

        public RectTransform GetInventoryButtonRect() => _view?.GetInventoryButtonRect();

        private readonly EffectHUDPresenter _effectHUDPresenter;
        private readonly InventoryHUDPresenter _inventoryHUDPresenter;

        public GameUIPresenter(GameUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            // Subscribe to state change events
            StatManager.Instance.OnHPChanged += UpdateHP;
            StatManager.Instance.OnStatsUpdated += UpdateStats;
            StatManager.Instance.OnShieldChanged += UpdateShield;
            _gameManager.OnGameDataChanged += UpdateAll;

            // Subscribe to settings panel trigger
            _view.OnSettingsClicked += HandleSettingsClicked;
            _view.OnRelicClicked += HandleRelicClicked;
            _view.OnInventoryClicked += HandleInventoryClicked;

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateAll;
            }

            // Bind Effect HUD view
            var iconContainer = _view.GetEffectIconContainer();
            if (iconContainer != null)
            {
                var hudEffectView = iconContainer.GetComponent<EffectHUDView>();
                if (hudEffectView == null)
                {
                    hudEffectView = iconContainer.gameObject.AddComponent<EffectHUDView>();
                }
                _effectHUDPresenter = new EffectHUDPresenter(hudEffectView);
            }

            if (_view != null && InventoryManager.Instance != null)
            {
                _inventoryHUDPresenter = new InventoryHUDPresenter(_view, InventoryManager.Instance.Collection);
            }

            UpdateAll();
        }

        public void Dispose()
        {
            if (StatManager.Instance != null)
            {
                StatManager.Instance.OnHPChanged -= UpdateHP;
                StatManager.Instance.OnStatsUpdated -= UpdateStats;
                StatManager.Instance.OnShieldChanged -= UpdateShield;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameDataChanged -= UpdateAll;
            }

            if (_view != null)
            {
                _view.OnSettingsClicked -= HandleSettingsClicked;
                _view.OnRelicClicked -= HandleRelicClicked;
                _view.OnInventoryClicked -= HandleInventoryClicked;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdateAll;
            }

            _effectHUDPresenter?.Dispose();
            _inventoryHUDPresenter?.Dispose();

            if (_heartbeatActive)
            {
                AudioManager.Instance?.StopHeartbeatWarning();
                _heartbeatActive = false;
            }
        }

        private void HandleSettingsClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenSettings();
            }
        }

        private void HandleRelicClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenRelicPopup();
            }
        }

        private void HandleInventoryClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenInventoryPopup();
            }
        }

        private void UpdateHP()
        {
            int currentHP = StatManager.Instance.CurrentHP;
            int maxHP     = StatManager.Instance.GetMaxHP();
            _view.SetHP(currentHP, maxHP);
            UpdateHeartbeatWarning(currentHP, maxHP);
        }

        private void UpdateHeartbeatWarning(int currentHP, int maxHP)
        {
            float hpPercent = maxHP > 0 ? (float)currentHP / maxHP * 100f : 0f;

            if (currentHP > 0 && hpPercent <= HeartbeatHpThresholdPercent)
            {
                if (!_heartbeatActive)
                {
                    _heartbeatActive = true;
                    AudioManager.Instance?.PlayHeartbeatWarning(AddressableKeys.HUDSFXLowHPHeartbeat);
                }

                // 30% → 1배속, 1%(이하) → 2배속으로 선형 가속.
                float t = Mathf.Clamp01((HeartbeatHpThresholdPercent - hpPercent) / (HeartbeatHpThresholdPercent - HeartbeatMinHpPercent));
                float pitch = Mathf.Lerp(1f, HeartbeatMaxPitch, t);
                AudioManager.Instance?.SetHeartbeatWarningPitch(pitch);
            }
            else if (_heartbeatActive)
            {
                _heartbeatActive = false;
                AudioManager.Instance?.StopHeartbeatWarning();
            }
        }

        private void UpdateShield()
        {
            _view.SetShield(StatManager.Instance.CurrentShield);
        }

        private void UpdateStats()
        {
            UpdateHP();
        }

        private void UpdateAll()
        {
            UpdateHP();
            UpdateShield();
            _view.SetDepth(_gameManager.CurrentDepth, _gameManager.DifficultyName);
            _view.SetResources(_gameManager.IronCount, _gameManager.SilverCount, _gameManager.GoldCount, _gameManager.DiamondCount);
            
            if (LocalizationManager.Instance != null)
            {
                _view.LocalizeSettingsButton(LocalizationManager.Instance.GetTranslation("hud_settings_btn"));
                _view.LocalizeRelicButton(LocalizationManager.Instance.GetTranslation("hud_relic_btn"));
                _view.LocalizeInventoryButton(LocalizationManager.Instance.GetTranslation("hud_inventory_btn"));
            }
        }
    }
}
