using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.UI;
using DeepEarth.Map;

namespace DeepEarth.Combat
{
    public class BossPresenter
    {
        public BossData Model { get; private set; }
        public BossView UI { get; private set; }
        public MonsterView View3D { get; private set; }

        public event Action OnBossDefeated;

        private readonly Transform _spawnPoint;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // Golem Shield Phase variables
        private int _golemHitCounter = 0;
        private int _golemShieldHP = 0;
        private const int GolemMaxShieldHP = 10;

        // Queen Spider Spawning variables
        private readonly List<MonsterPresenter> _activeBabySpiders = new List<MonsterPresenter>();
        private readonly List<GameObject> _activeBabyObjects = new List<GameObject>();

        public BossPresenter(BossData model, BossView ui, MonsterView view3D, Transform spawnPoint)
        {
            Model = model;
            UI = ui;
            View3D = view3D;
            _spawnPoint = spawnPoint;

            UI?.Initialize();
            UI?.SetBossName(Model?.BossNameKey);
            UI?.SetHP(Model.CurrentHP, Model.MaxHP);
            UI?.SetVisible(true);

            View3D.OnTouched += HandleTouched;

            StartAttackLoop().Forget();

            if (Model.ID == BossID.QueenSpider)
            {
                StartSpiderSpawnLoop().Forget();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            if (View3D != null)
            {
                View3D.OnTouched -= HandleTouched;
            }

            ClearBabySpiders();
        }

        private void HandleTouched()
        {
            if (Model.IsDead) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.BossCombat)
            {
                return;
            }

            int damage = StatManager.Instance.GetAttackDamage();
            // Apply Boss Slayer Rare Buff (+50% Damage to Bosses)
            if (StatManager.Instance.BossDamageToBossMultiplier > 1.0f)
            {
                damage = Mathf.RoundToInt(damage * StatManager.Instance.BossDamageToBossMultiplier);
            }

            // Golem Shield Interaction
            if (Model.ID == BossID.RockGolem && _golemShieldHP > 0)
            {
                int shieldDamage = Mathf.Min(damage, _golemShieldHP);
                _golemShieldHP -= shieldDamage;
                damage -= shieldDamage;

                // Shield impact feedback
                EffectSystem.Instance.SpawnDamageText(View3D.transform.position + Vector3.up * 0.5f, $"{shieldDamage} Shield", new Color(0.2f, 0.6f, 1f));
                EffectSystem.Instance.ShakeCamera(0.12f, 0.04f);
                View3D.PlayHurtFeedback();

                UI?.SetShield(_golemShieldHP, GolemMaxShieldHP);

                if (_golemShieldHP <= 0)
                {
                    EffectSystem.Instance.SpawnDamageText(View3D.transform.position + Vector3.up * 0.8f, "SHIELD BROKEN", Color.red);
                }
            }

            // Deal remaining damage to Boss
            if (damage > 0)
            {
                Model.TakeDamage(damage);
                EffectSystem.Instance.SpawnDamageText(View3D.transform.position + Vector3.up * 0.5f, damage.ToString(), Color.white);
                EffectSystem.Instance.SpawnHitParticles(View3D.transform.position, View3D.GetMonsterColor());
                EffectSystem.Instance.ShakeCamera(0.12f, 0.04f);
                View3D.PlayHurtFeedback();

                // If Golem, increment hit counter to regenerate shield
                if (Model.ID == BossID.RockGolem && !Model.IsDead)
                {
                    _golemHitCounter++;
                    if (_golemHitCounter >= 4)
                    {
                        _golemHitCounter = 0;
                        _golemShieldHP = GolemMaxShieldHP;
                        UI?.SetShield(_golemShieldHP, GolemMaxShieldHP);
                        EffectSystem.Instance.SpawnDamageText(View3D.transform.position + Vector3.up * 0.8f, "SHIELD CHARGED", new Color(0.2f, 0.6f, 1f));
                    }
                }
            }

            UI?.SetHP(Model.CurrentHP, Model.MaxHP);

            if (Model.IsDead)
            {
                OnBossDefeated?.Invoke();
            }
        }

        private async UniTaskVoid StartAttackLoop()
        {
            try
            {
                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Model.AttackInterval), cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.BossCombat)
                    {
                        continue;
                    }

                    // Play attack animation lunge
                    View3D.PlayAttackAnimation();
                    await UniTask.Delay(100, cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.BossCombat)
                    {
                        continue;
                    }

                    // Apply Lava Worm burn DOT
                    if (Model.ID == BossID.LavaWorm)
                    {
                        StatManager.Instance.ApplyBurn(3, 1);
                    }

                    // Deal attack damage to player
                    StatManager.Instance.TakeDamage(Model.Damage);

                    EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                    EffectSystem.Instance.ShakeCamera(0.25f, 0.12f);

                    Vector3 textWorldPos = Camera.main != null
                        ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * -0.5f
                        : View3D.transform.position + Vector3.up;

                    EffectSystem.Instance.SpawnDamageText(textWorldPos, $"-{Model.Damage} HP", Color.red);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean exit
            }
        }

        private async UniTaskVoid StartSpiderSpawnLoop()
        {
            try
            {
                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(5.0f), cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.BossCombat)
                    {
                        continue;
                    }

                    if (_activeBabySpiders.Count < 3)
                    {
                        await SpawnBabySpiderAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Clean exit
            }
        }

        private async UniTask SpawnBabySpiderAsync()
        {
            string key = AddressableKeys.MonsterSpider;
            GameObject mGo = await PoolSystem.Instance.GetAsync(key, _spawnPoint);
            if (mGo == null) return;

            // Offset baby spiders
            Vector3 localOffset = _activeBabySpiders.Count switch
            {
                0 => new Vector3(-1.0f, 0f, 0.5f),
                1 => new Vector3(1.0f, 0f, 0.5f),
                _ => new Vector3(0f, 0f, -0.5f)
            };

            Vector3 worldPos = _spawnPoint.position + _spawnPoint.TransformDirection(localOffset);
            mGo.transform.position = worldPos;
            mGo.transform.rotation = _spawnPoint.rotation;
            _activeBabyObjects.Add(mGo);

            var view = mGo.GetComponent<MonsterView>();
            if (view == null) view = mGo.AddComponent<MonsterView>();

            int spawnIdx = _activeBabySpiders.Count;
            view.InitializeSpawn(spawnIdx);
            Debug.Log($"[Battle]\nSpawn Monster\nIndex : {spawnIdx}\nPosition : {worldPos.x:F2},{worldPos.y:F2},{worldPos.z:F2}");

            var model = new MonsterModel(MonsterType.CaveSpider, GameManager.Instance.CurrentDepth);
            var presenter = new MonsterPresenter(model, view);
            _activeBabySpiders.Add(presenter);

            presenter.OnMonsterKilled += HandleBabySpiderKilled;
        }

        private void HandleBabySpiderKilled(MonsterPresenter presenter)
        {
            presenter.OnMonsterKilled -= HandleBabySpiderKilled;

            Debug.Log($"[Battle]\nMonster Dead\nSpawnIndex : {presenter.View.SpawnIndex}");

            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);

            GameObject go = presenter.View.gameObject;
            _activeBabyObjects.Remove(go);
            PoolSystem.Instance.Return(go);

            _activeBabySpiders.Remove(presenter);
            presenter.Dispose();
        }

        private void ClearBabySpiders()
        {
            foreach (var pres in _activeBabySpiders)
            {
                pres.Dispose();
            }
            _activeBabySpiders.Clear();

            foreach (var obj in _activeBabyObjects)
            {
                if (obj != null) PoolSystem.Instance.Return(obj);
            }
            _activeBabyObjects.Clear();
        }
    }
}
