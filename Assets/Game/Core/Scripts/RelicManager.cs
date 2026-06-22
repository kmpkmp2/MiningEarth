using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DeepEarth.Core
{
    public class RelicManager : MonoBehaviour
    {
        private static RelicManager _instance;
        public static RelicManager Instance => _instance;

        private const string TreasureLabel = "Relic_Treasure";
        private const string TombstoneLabel = "Relic_Tombstone";

        private readonly List<RelicData> _treasurePool = new List<RelicData>();
        private readonly List<RelicData> _tombstonePool = new List<RelicData>();
        private readonly List<RelicData> _activeRelics = new List<RelicData>();
        private readonly HashSet<string> _acquiredIDs = new HashSet<string>();

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

        public async UniTask InitializeAsync()
        {
            var treasureRelics = await ResourceManager.Instance.LoadAllByLabelAsync<RelicData>(TreasureLabel);
            var tombstoneRelics = await ResourceManager.Instance.LoadAllByLabelAsync<RelicData>(TombstoneLabel);

            _treasurePool.Clear();
            _tombstonePool.Clear();
            _treasurePool.AddRange(treasureRelics);
            _tombstonePool.AddRange(tombstoneRelics);

            Debug.Log($"[Relic]\nInitialized\nTreasure Pool : {_treasurePool.Count}\nTombstone Pool : {_tombstonePool.Count}");
        }

        public List<RelicData> GetAvailableTreasureRelics()
        {
            return _treasurePool.FindAll(r => !_acquiredIDs.Contains(r.relicID));
        }

        public List<RelicData> GetAvailableTombstoneRelics()
        {
            return _tombstonePool.FindAll(r => !_acquiredIDs.Contains(r.relicID));
        }

        public void AddRelic(RelicData relic)
        {
            if (relic == null || _acquiredIDs.Contains(relic.relicID)) return;

            _activeRelics.Add(relic);
            _acquiredIDs.Add(relic.relicID);

            if (relic.attackBonus != 0)
                StatManager.Instance.BossAttackModifier += relic.attackBonus;

            if (relic.maxHPBonus != 0)
            {
                StatManager.Instance.BossMaxHPModifier += relic.maxHPBonus;
                if (relic.maxHPBonus > 0)
                    StatManager.Instance.Heal(relic.maxHPBonus);
            }

            if (relic.resourceMultiplierBonus != 0)
                StatManager.Instance.BossResourceModifier += relic.resourceMultiplierBonus;

            if (relic.monsterAttackBonus != 0)
                StatManager.Instance.RelicMonsterAttackBonus += relic.monsterAttackBonus;

            EffectSystemType displayType = relic.isTombstone ? EffectSystemType.Special : EffectSystemType.Buff;
            EffectManager.Instance?.RegisterEffect(
                relic.relicID,
                relic.nameLocKey,
                relic.descLocKey,
                displayType,
                0f,
                BuildDisplayString(relic),
                relic.isTombstone ? "Tombstone" : "Treasure",
                relic.iconKey
            );

            StatManager.Instance.TriggerStatsUpdated();
            Debug.Log($"[Relic]\nAcquired\n{relic.relicID}");
        }

        public int GetBurnDurationModifier()
        {
            int total = 0;
            foreach (var r in _activeRelics) total += r.burnDurationModifier;
            return total;
        }

        public int GetBurnDamageModifier()
        {
            int total = 0;
            foreach (var r in _activeRelics) total += r.burnDamageModifier;
            return total;
        }

        public bool CheckBurnImmunity()
        {
            foreach (var r in _activeRelics)
            {
                if (r.burnImmunityChance > 0f && Random.value < r.burnImmunityChance)
                    return true;
            }
            return false;
        }

        public int GetMonsterAttackBonus()
        {
            int total = 0;
            foreach (var r in _activeRelics) total += r.monsterAttackBonus;
            return total;
        }

        public void LogBurnContributions()
        {
            foreach (var r in _activeRelics)
            {
                if (r.burnDurationModifier == 0 && r.burnDamageModifier == 0) continue;

                string log = $"[Burn]\nRelic Applied\n{r.relicID}";
                if (r.burnDurationModifier != 0)
                    log += $"\nDuration {(r.burnDurationModifier > 0 ? "+" : "")}{r.burnDurationModifier}";
                if (r.burnDamageModifier != 0)
                    log += $"\nDamage {(r.burnDamageModifier > 0 ? "+" : "")}{r.burnDamageModifier}";
                Debug.Log(log);
            }
        }

        public void ClearAll()
        {
            foreach (var r in _activeRelics)
                EffectManager.Instance?.RemoveEffect(r.relicID);

            _activeRelics.Clear();
            _acquiredIDs.Clear();
        }

        private static string BuildDisplayString(RelicData relic)
        {
            var sb = new System.Text.StringBuilder();

            if (relic.attackBonus != 0)
                sb.Append($"공격 {(relic.attackBonus > 0 ? "+" : "")}{relic.attackBonus}  ");
            if (relic.maxHPBonus != 0)
                sb.Append($"HP {(relic.maxHPBonus > 0 ? "+" : "")}{relic.maxHPBonus}  ");
            if (relic.burnDurationModifier != 0)
                sb.Append($"화상 지속 {(relic.burnDurationModifier > 0 ? "+" : "")}{relic.burnDurationModifier}턴  ");
            if (relic.burnDamageModifier != 0)
                sb.Append($"화상 피해 {(relic.burnDamageModifier > 0 ? "+" : "")}{relic.burnDamageModifier}  ");
            if (relic.resourceMultiplierBonus != 0)
                sb.Append($"광물 +{relic.resourceMultiplierBonus * 100:0}%  ");
            if (relic.monsterAttackBonus != 0)
                sb.Append($"몬스터 공격 +{relic.monsterAttackBonus}  ");
            if (relic.burnImmunityChance > 0f)
                sb.Append($"화상 무효 {relic.burnImmunityChance * 100:0}%  ");

            return sb.ToString().TrimEnd();
        }
    }
}
