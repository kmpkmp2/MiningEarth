using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.Combat
{
    /// <summary>
    /// Extends MonsterPresenter with elite-specific skill logic for all 6 elite types.
    /// HandleTouched and StartAttackLoop are overridden to implement elite mechanics.
    /// </summary>
    public class EliteMonsterPresenter : MonsterPresenter
    {
        public event Action OnEliteDefeated;

        // Field initializer runs before base constructor — safe for virtual dispatch from base ctor.
        private readonly CancellationTokenSource _eliteCts = new CancellationTokenSource();

        private EliteMonsterModel EliteModel => (EliteMonsterModel)Model;

        // Tracks how many CurseAttackDamage stacks were applied by CursedPriest.
        private int _debuffStacksApplied;

        // Current attack interval, modified by CursedKnight enrage.
        private float _currentAttackInterval;

        public EliteMonsterPresenter(EliteMonsterModel model, MonsterView view)
            : base(model, view)
        {
            _currentAttackInterval = model.AttackInterval;

            // Start skill-specific loops that run alongside the attack loop.
            switch (model.Data.eliteSkillType)
            {
                case EliteSkillType.MineralTheft:
                    MineralTheftLoopAsync().Forget();
                    break;
                case EliteSkillType.AttackDebuff:
                    AttackDebuffLoopAsync().Forget();
                    break;
            }
        }

        public override void Dispose()
        {
            _eliteCts.Cancel();
            _eliteCts.Dispose();
            base.Dispose();
        }

        // ── Touch Handler ────────────────────────────────────────────────────────

        protected override void HandleTouched()
        {
            if (EliteModel.IsDead) return;

            var state = GameManager.Instance?.CurrentState;
            if (state != GameState.Playing && state != GameState.BossCombat) return;

            int damage = StatManager.Instance.GetAttackDamage();
            // 유물: 작은 숫돌 — 엘리트에게 주는 피해 +%
            float eliteBonus = RelicManager.Instance?.GetEliteDamageBonus() ?? 0f;
            if (eliteBonus > 0f)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f + eliteBonus)));

            // IronArmor: absorb damage into armor until broken
            if (EliteModel.Data.eliteSkillType == EliteSkillType.IronArmor && !EliteModel.IsArmorBroken)
            {
                EliteModel.TakeArmorDamage(damage);
                EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f,
                    $"-{damage} (방어)", Color.grey);
                EffectSystem.Instance.SpawnHitParticles(View.transform.position, Color.grey);
                EffectSystem.Instance.ShakeCamera(0.08f, 0.03f);
                View.PlayHurtFeedback();

                if (EliteModel.IsArmorBroken)
                {
                    EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up,
                        "방어 붕괴!", new Color(1f, 0.5f, 0f));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[Elite]\nSkill Activated\nIronArmor : Armor Broken");
#endif
                }
                return;
            }

            // Standard damage to monster HP
            EliteModel.TakeDamage(damage);
            EffectSystem.Instance.SpawnDamageText(View.transform.position + Vector3.up * 0.5f,
                damage.ToString(), Color.white);
            EffectSystem.Instance.SpawnHitParticles(View.transform.position, View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.12f, 0.04f);
            View.PlayHurtFeedback();

            // IronArmor: poison counter (every 3 touches after armor breaks)
            if (EliteModel.Data.eliteSkillType == EliteSkillType.IronArmor)
            {
                EliteModel.IncrementTouch();
                if (EliteModel.ShouldTriggerPoison())
                {
                    EliteModel.ResetPoisonCounter();
                    ApplyPoison();
                }
            }

            // Enrage: check thresholds on each hit
            if (EliteModel.Data.eliteSkillType == EliteSkillType.Enrage)
            {
                int enrageCount = EliteModel.CheckAndTriggerEnrage();
                if (enrageCount > 0)
                    OnEnrage(enrageCount);
            }

            if (EliteModel.IsDead)
            {
                HandleEliteDefeated();
            }
        }

        // ── Attack Loop (overridden to use _eliteCts and dynamic interval) ───────

        protected override async UniTaskVoid StartAttackLoop()
        {
            try
            {
                if (EliteModel.InitialAttackDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(EliteModel.InitialAttackDelay),
                        cancellationToken: _eliteCts.Token);

                while (!EliteModel.IsDead && !_eliteCts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_currentAttackInterval),
                        cancellationToken: _eliteCts.Token);

                    var s = GameManager.Instance?.CurrentState;
                    if (EliteModel.IsDead || s != GameState.Playing) continue;

                    View.PlayAttackAnimation();
                    await UniTask.Delay(100, cancellationToken: _eliteCts.Token);

                    s = GameManager.Instance?.CurrentState;
                    if (EliteModel.IsDead || s != GameState.Playing) continue;

                    // Enraged elites deal +1 damage per enrage stack
                    int dmg = EliteModel.Damage + EliteModel.EnrageCount;
                    StatManager.Instance.TakeDamage(dmg);

                    EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.35f), 0.15f);
                    EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);

                    Vector3 textPos = Camera.main != null
                        ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                            + Camera.main.transform.right * -0.5f
                        : View.transform.position + Vector3.up;

                    EffectSystem.Instance.SpawnDamageText(textPos, $"-{dmg} HP", Color.red);
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Elite Skills ─────────────────────────────────────────────────────────

        // IronPlateSpider: Poison — deals 10% current HP (min 1) to player.
        private void ApplyPoison()
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

        // CursedKnight: Enrage at each HP threshold.
        private void OnEnrage(int stack)
        {
            // Reduce attack interval by 15% per stack (minimum 0.3s)
            _currentAttackInterval = Mathf.Max(0.3f, EliteModel.AttackInterval * (1f - 0.15f * stack));

            EffectSystem.Instance.FlashScreen(new Color(1f, 0.3f, 0f, 0.3f), 0.25f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            Vector3 textPos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : View.transform.position + Vector3.up;
            EffectSystem.Instance.SpawnDamageText(textPos, "격분!", new Color(1f, 0.4f, 0f));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nEnrage Stack : {stack}\nNew Interval : {_currentAttackInterval:F2}");
#endif
        }

        // SkeletonMiner: Steals one mineral from the player's inventory every 5 seconds.
        private async UniTaskVoid MineralTheftLoopAsync()
        {
            try
            {
                while (!EliteModel.IsDead && !_eliteCts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(5f), cancellationToken: _eliteCts.Token);
                    if (EliteModel.IsDead) break;

                    string stolen = StealOneMineralFromInventory();
                    if (stolen != null)
                    {
                        Vector3 textPos = View != null
                            ? View.transform.position + Vector3.up * 0.5f
                            : Vector3.up;
                        EffectSystem.Instance.SpawnDamageText(textPos, "광물 도둑!!", Color.yellow);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log($"[Elite]\nSkill Activated\nSkeletonMiner Steal : {stolen}");
#endif
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private string StealOneMineralFromInventory()
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

        // CursedPriest: Applies CurseAttackDamage immediately and every 1 second.
        private async UniTaskVoid AttackDebuffLoopAsync()
        {
            try
            {
                ApplyAttackDebuffStack();

                while (!EliteModel.IsDead && !_eliteCts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: _eliteCts.Token);
                    if (EliteModel.IsDead) break;

                    ApplyAttackDebuffStack();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ApplyAttackDebuffStack()
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

        // CursedPriest: Removes all stacks applied during this fight on death.
        private void RemoveAllDebuffStacks()
        {
            if (_debuffStacksApplied <= 0) return;
            StatManager.Instance.RemoveEffect(EffectType.CurseAttackDamage, _debuffStacksApplied);
            _debuffStacksApplied = 0;
        }

        // ── Defeat ───────────────────────────────────────────────────────────────

        private void HandleEliteDefeated()
        {
            // CursedPriest: clear all combat debuffs on death
            if (EliteModel.Data.eliteSkillType == EliteSkillType.AttackDebuff)
                RemoveAllDebuffStacks();

            // BigSlime: split on death — delegate split to EliteCombatSystem via event
            // (EliteCombatSystem subscribes to OnEliteDefeated and handles split logic)

            OnEliteDefeated?.Invoke();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nDefeated\nType : {EliteModel.Data.monsterType}");
#endif
        }
    }
}
