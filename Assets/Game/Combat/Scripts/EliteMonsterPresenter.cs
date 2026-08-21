using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.Combat
{
    // 엘리트 6종의 스킬 로직 보유. 실행은 Battle.EliteMonsterPresenter(턴 기반 훅)가 아래 public 메서드를 호출해 담당한다.
    public class EliteMonsterPresenter : MonsterPresenter
    {
        // Tracks how many CurseAttackDamage stacks were applied by CursedPriest.
        private int _debuffStacksApplied;

        public EliteMonsterPresenter(EliteMonsterModel model, MonsterView view) : base(model, view) { }

        // ── Elite Skills ─────────────────────────────────────────────────────────

        // IronPlateSpider: Poison — deals 10% current HP (min 1) to player.
        // public — Battle.EliteMonsterPresenter(턴제 래퍼)가 동일 로직을 재사용한다.
        public void ApplyPoison()
        {
            if (RelicManager.Instance?.HasPoisonImmunity() ?? false)
            {
                Vector3 immunePos = Camera.main != null
                    ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                    : View.transform.position + Vector3.up;
                EffectSystem.Instance.SpawnDamageText(immunePos, "면역!", Color.green);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log("[Elite]\nSkill Blocked\nIronPlateSpider Poison : PoisonImmunity");
#endif
                return;
            }
            int poisonDmg = Mathf.Max(1, Mathf.RoundToInt(StatManager.Instance.CurrentHP * 0.1f));
            StatManager.Instance.TakeDamage(poisonDmg);

            EffectSystem.Instance.FlashScreen(new Color(0.2f, 0.8f, 0.2f, 0.3f), 0.2f);
            EffectSystem.Instance.ShakeCamera(0.15f, 0.05f);

            Vector3 textPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : View.transform.position + Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, $"-{poisonDmg} 독", new Color(0.2f, 0.9f, 0.2f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nPoison Damage : {poisonDmg}");
#endif
        }

        // CursedKnight: Enrage at each HP threshold. public — 턴제 래퍼가 재사용.
        // 데미지 증가분 자체는 Battle.EliteMonsterPresenter.ModifyOutgoingDamage(EnrageCount 기반)가 처리한다.
        public void OnEnrage(int stack)
        {
            EffectSystem.Instance.FlashScreen(new Color(1f, 0.3f, 0f, 0.3f), 0.25f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            Vector3 textPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : View.transform.position + Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, "격분!", new Color(1f, 0.4f, 0f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nEnrage Stack : {stack}");
#endif
        }

        // public — 턴제 래퍼(Skeleton Miner의 OnSpecialStep)가 재사용.
        public string StealOneMineralFromInventory()
        {
            string[] priority = { "Item_Diamond", "Item_Gold", "Item_Silver", "Item_Iron" };
            foreach (var id in priority)
            {
                if (InventoryManager.Instance.GetItemCount(id) > 0)
                {
                    InventoryManager.Instance.RemoveItem(id, 1);
                    return id;
                }
            }
            return null;
        }

        // public — 턴제 래퍼(Cursed Priest의 OnDebuffStep, 생성자 즉시 적용)가 재사용.
        public void ApplyAttackDebuffStack()
        {
            int current = StatManager.Instance.GetEffectStack(EffectType.CurseAttackDamage);
            if (current < GameSettings.MaxBuffDebuffStack)
            {
                StatManager.Instance.AddEffect(EffectType.CurseAttackDamage);
                _debuffStacksApplied++;

                Vector3 textPos = Camera.main != null
                    ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                    : Vector3.up;
                EffectSystem.Instance.SpawnDamageText(textPos, "공격력 저하!", new Color(0.5f, 0.2f, 0.8f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Elite]\nSkill Activated\nCursedPriest AttackDebuff Stack : {_debuffStacksApplied}");
#endif
            }
        }

        // CursedPriest: Removes all stacks applied during this fight on death. public — 턴제 래퍼가 재사용.
        public void RemoveAllDebuffStacks()
        {
            if (_debuffStacksApplied <= 0) return;
            StatManager.Instance.RemoveEffect(EffectType.CurseAttackDamage, _debuffStacksApplied);
            _debuffStacksApplied = 0;
        }

        // MagmaLizard: 공격 적중 시 일정 확률로 화상 부여. public — 턴제 래퍼(OnAttackLanded)가 재사용.
        public void ApplyBurnChance(float chance = 0.4f)
        {
            if (UnityEngine.Random.value >= chance) return;
            StatusEffectManager.Instance?.ApplyBurn();

            Vector3 textPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : View.transform.position + Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, "화상!", new Color(1f, 0.4f, 0f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Elite]\nSkill Activated\nMagmaLizard BurnOnHit");
#endif
        }

        // MagmaLizard: 용암분출 — 다음 피격 1회의 피해를 증가시키는 취약 상태.
        private bool _vulnerableNextHit;

        // 상태 아이콘 표시 등 읽기 전용 조회용. ConsumeVulnerable()과 달리 상태를 소모하지 않는다.
        public bool IsVulnerableNextHit => _vulnerableNextHit;

        public void SetVulnerable()
        {
            _vulnerableNextHit = true;
            Vector3 textPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : View.transform.position + Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, "용암 분출!", new Color(1f, 0.5f, 0f));
        }

        // 소비형 — 호출 시점에 취약 상태였다면 true를 반환하고 즉시 해제한다.
        public bool ConsumeVulnerable()
        {
            if (!_vulnerableNextHit) return false;
            _vulnerableNextHit = false;
            return true;
        }
    }
}
