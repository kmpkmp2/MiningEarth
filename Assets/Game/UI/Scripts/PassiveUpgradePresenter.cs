using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class PassiveUpgradePresenter : IDisposable
    {
        private readonly CharacterPassiveRowView _view;
        private CharacterID _currentCharacterID;

        public PassiveUpgradePresenter(CharacterPassiveRowView view)
        {
            _view = view;
            _view.OnUpgradeClicked += HandleUpgrade;
        }

        public void SetCharacter(CharacterID id)
        {
            _currentCharacterID = id;
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null || MetaProgressionManager.Instance == null) return;

            var staticData = CharacterDatabase.Get(_currentCharacterID);
            if (staticData == null || staticData.PassiveLevels == null || staticData.PassiveLevels.Count == 0)
            {
                _view.SetNoPassive();
                return;
            }

            var meta     = MetaProgressionManager.Instance;
            var loc      = LocalizationManager.Instance;
            int level    = meta.GetPassiveLevel(_currentCharacterID);
            int maxLevel = meta.GetPassiveMaxLevel(_currentCharacterID);
            int cost     = meta.GetPassiveUpgradeCost(_currentCharacterID);
            bool canAfford = meta.Will >= cost;

            string passiveName = loc?.GetTranslation(staticData.PassiveNameKey) ?? staticData.PassiveNameKey;

            string currentDesc;
            if (level <= 0)
            {
                currentDesc = loc?.GetTranslation("passive_not_upgraded") ?? "미강화";
            }
            else
            {
                int curIdx = Mathf.Min(level - 1, staticData.PassiveLevels.Count - 1);
                currentDesc = loc?.GetTranslation(staticData.PassiveLevels[curIdx].DescKey)
                              ?? staticData.PassiveLevels[curIdx].DescKey;
            }

            string nextDesc;
            if (level >= maxLevel)
            {
                nextDesc = loc?.GetTranslation("passive_max_level") ?? "MAX";
            }
            else
            {
                nextDesc = loc?.GetTranslation(staticData.PassiveLevels[level].DescKey)
                           ?? staticData.PassiveLevels[level].DescKey;
            }

            _view.SetPassiveData(passiveName, level, maxLevel, currentDesc, nextDesc, cost, canAfford);
        }

        public void Dispose()
        {
            if (_view != null)
                _view.OnUpgradeClicked -= HandleUpgrade;
        }

        private void HandleUpgrade()
        {
            if (MetaProgressionManager.Instance == null) return;
            MetaProgressionManager.Instance.UpgradePassive(_currentCharacterID);
            Refresh();
        }
    }
}
