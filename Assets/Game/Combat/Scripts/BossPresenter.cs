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
    public class BossPresenter : System.IDisposable
    {
        public BossData Model { get; private set; }
        public BossView UI { get; private set; }
        public MonsterView View3D { get; private set; }

        public event Action OnBossDefeated;

        private readonly Transform _spawnPoint;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // MotherCaveSpider
        private readonly List<MonsterPresenter> _activeBabySpiders = new List<MonsterPresenter>();
        private readonly List<GameObject> _activeBabyObjects = new List<GameObject>();

        // SkeletonWarlord
        private readonly List<MonsterPresenter> _activeSkeletons = new List<MonsterPresenter>();
        private readonly List<GameObject> _activeSkeletonObjects = new List<GameObject>();
        private bool _threshold75Triggered;
        private bool _threshold50Triggered;
        private bool _threshold25Triggered;

        // AllMetalColossus
        private readonly List<BossCoreView> _activeCores = new List<BossCoreView>();
        private readonly List<GameObject> _coreObjects = new List<GameObject>();
        private bool _isInvincible;

        public BossPresenter(BossData model, BossView ui, MonsterView view3D, Transform spawnPoint)
        {
            Model = model;
            UI = ui;
            View3D = view3D;
            _spawnPoint = spawnPoint;

            UI?.Initialize();
            UI?.SetBossName(Model.BossNameKey);
            UI?.SetHP(Model.CurrentHP, Model.MaxHP);
            UI?.SetVisible(true);

            View3D.OnTouched += HandleTouched;

            StartAttackLoop().Forget();

            if (Model.ID == BossID.MotherCaveSpider)
            {
                SpawnInitialBabySpiders().Forget();
                StartBabySpawnLoop().Forget();
            }
            else if (Model.ID == BossID.AllMetalColossus)
            {
                _isInvincible = true;
                SpawnOreCores();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            if (View3D != null)
                View3D.OnTouched -= HandleTouched;

            ClearActiveMinions();
            DestroyOreCores();
        }

        // ── Touch Handler ─────────────────────────────────────────────────────

        private void HandleTouched()
        {
            if (Model.IsDead) return;
            if (GameManager.Instance?.CurrentState != GameState.BossCombat) return;

            if (Model.ID == BossID.AllMetalColossus && _isInvincible)
            {
                EffectSystem.Instance.SpawnDamageText(
                    View3D.transform.position + Vector3.up * 0.8f,
                    "INVINCIBLE!",
                    new Color(0.2f, 0.6f, 1f));
                EffectSystem.Instance.ShakeCamera(0.05f, 0.02f);
                return;
            }

            int damage = StatManager.Instance.GetAttackDamage();
            if (StatManager.Instance.BossDamageToBossMultiplier > 1.0f)
                damage = Mathf.RoundToInt(damage * StatManager.Instance.BossDamageToBossMultiplier);

            Model.TakeDamage(damage);
            EffectSystem.Instance.SpawnDamageText(View3D.transform.position + Vector3.up * 0.5f, damage.ToString(), Color.white);
            EffectSystem.Instance.SpawnHitParticles(View3D.transform.position, View3D.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.12f, 0.04f);
            View3D.PlayHurtFeedback();

            if (Model.ID == BossID.SkeletonWarlord && !Model.IsDead)
                CheckWarlordThresholds();

            UI?.SetHP(Model.CurrentHP, Model.MaxHP);

            if (Model.IsDead)
            {
                ClearActiveMinions();
                DestroyOreCores();
                OnBossDefeated?.Invoke();
            }
        }

        // ── Attack Loop ───────────────────────────────────────────────────────

        private async UniTaskVoid StartAttackLoop()
        {
            try
            {
                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Model.AttackInterval), cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance?.CurrentState != GameState.BossCombat)
                        continue;

                    // AllMetalColossus does not attack while cores are alive
                    if (Model.ID == BossID.AllMetalColossus && _isInvincible)
                        continue;

                    View3D.PlayAttackAnimation();
                    await UniTask.Delay(100, cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance?.CurrentState != GameState.BossCombat)
                        continue;

                    if (Model.Damage > 0)
                    {
                        StatManager.Instance.TakeDamage(Model.Damage);
                        EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                        EffectSystem.Instance.ShakeCamera(0.25f, 0.12f);

                        Vector3 textPos = Camera.main != null
                            ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Camera.main.transform.right * -0.5f
                            : View3D.transform.position + Vector3.up;
                        EffectSystem.Instance.SpawnDamageText(textPos, $"-{Model.Damage} HP", Color.red);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Mother Cave Spider ────────────────────────────────────────────────

        private async UniTaskVoid SpawnInitialBabySpiders()
        {
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    if (Model.IsDead || _cts.IsCancellationRequested) return;
                    await SpawnBabySpiderAsync();
                    await UniTask.Delay(300, cancellationToken: _cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid StartBabySpawnLoop()
        {
            try
            {
                // Wait for initial 6 to finish spawning
                await UniTask.Delay(2500, cancellationToken: _cts.Token);

                while (!Model.IsDead && !_cts.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, cancellationToken: _cts.Token);

                    if (Model.IsDead || GameManager.Instance?.CurrentState != GameState.BossCombat)
                        continue;

                    if (_activeBabySpiders.Count < 10)
                        await SpawnBabySpiderAsync();
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask SpawnBabySpiderAsync()
        {
            GameObject mGo = await PoolSystem.Instance.GetAsync(AddressableKeys.MonsterSpider, _spawnPoint);
            if (mGo == null || Model.IsDead)
            {
                if (mGo != null) PoolSystem.Instance.Return(mGo);
                return;
            }

            Vector3 localOffset = new Vector3(
                UnityEngine.Random.Range(-2.0f, 2.0f),
                0f,
                UnityEngine.Random.Range(-0.5f, 1.5f));
            mGo.transform.position = _spawnPoint.position + _spawnPoint.TransformDirection(localOffset);
            mGo.transform.rotation = _spawnPoint.rotation;
            _activeBabyObjects.Add(mGo);

            var view = mGo.GetComponent<MonsterView>();
            if (view == null) view = mGo.AddComponent<MonsterView>();
            view.InitializeSpawn(_activeBabySpiders.Count);

            // Baby spiders: 50% CaveSpider HP (=1), 1 DMG
            var model = new MonsterModel(MonsterType.CaveSpider, maxHp: 1, damage: 1, attackInterval: 1.5f);
            var presenter = new MonsterPresenter(model, view);
            _activeBabySpiders.Add(presenter);
            presenter.OnMonsterKilled += HandleBabySpiderKilled;

            Debug.Log($"[Boss]\nBaby Spider Spawned\nCount : {_activeBabySpiders.Count}");
        }

        private void HandleBabySpiderKilled(MonsterPresenter presenter)
        {
            presenter.OnMonsterKilled -= HandleBabySpiderKilled;
            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.1f, 0.04f);

            GameObject go = presenter.View.gameObject;
            _activeBabyObjects.Remove(go);
            PoolSystem.Instance.Return(go);
            _activeBabySpiders.Remove(presenter);
            presenter.Dispose();

            Debug.Log($"[Boss]\nBaby Spider Killed\nRemaining : {_activeBabySpiders.Count}");
        }

        // ── Skeleton Warlord ──────────────────────────────────────────────────

        private void CheckWarlordThresholds()
        {
            float hpRatio = (float)Model.CurrentHP / Model.MaxHP;

            if (!_threshold75Triggered && hpRatio <= 0.75f)
            {
                _threshold75Triggered = true;
                SpawnWarlordSkeletons(2).Forget();
            }
            if (!_threshold50Triggered && hpRatio <= 0.50f)
            {
                _threshold50Triggered = true;
                SpawnWarlordSkeletons(2).Forget();
            }
            if (!_threshold25Triggered && hpRatio <= 0.25f)
            {
                _threshold25Triggered = true;
                SpawnWarlordSkeletons(3).Forget();
            }
        }

        private async UniTaskVoid SpawnWarlordSkeletons(int count)
        {
            try
            {
                EffectSystem.Instance.SpawnDamageText(
                    View3D.transform.position + Vector3.up * 0.8f,
                    "SUMMON!",
                    new Color(0.8f, 0.8f, 1f));

                for (int i = 0; i < count; i++)
                {
                    if (Model.IsDead || _cts.IsCancellationRequested) return;
                    await SpawnWarlordSkeletonAsync();
                    await UniTask.Delay(200, cancellationToken: _cts.Token);
                }

                Debug.Log($"[Boss]\nSkeleton Wave Spawned\nCount : {count}\nTotal Active : {_activeSkeletons.Count}");
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask SpawnWarlordSkeletonAsync()
        {
            GameObject mGo = await PoolSystem.Instance.GetAsync(AddressableKeys.MonsterSkeleton, _spawnPoint);
            if (mGo == null || Model.IsDead)
            {
                if (mGo != null) PoolSystem.Instance.Return(mGo);
                return;
            }

            Vector3 localOffset = new Vector3(
                UnityEngine.Random.Range(-2.5f, 2.5f),
                0f,
                UnityEngine.Random.Range(-0.5f, 2.0f));
            mGo.transform.position = _spawnPoint.position + _spawnPoint.TransformDirection(localOffset);
            mGo.transform.rotation = _spawnPoint.rotation;
            _activeSkeletonObjects.Add(mGo);

            var view = mGo.GetComponent<MonsterView>();
            if (view == null) view = mGo.AddComponent<MonsterView>();
            view.InitializeSpawn(_activeSkeletons.Count);

            var model = new MonsterModel(MonsterType.Skeleton, maxHp: 3, damage: 2, attackInterval: 2.0f);
            var presenter = new MonsterPresenter(model, view);
            _activeSkeletons.Add(presenter);
            presenter.OnMonsterKilled += HandleSkeletonKilled;
        }

        private void HandleSkeletonKilled(MonsterPresenter presenter)
        {
            presenter.OnMonsterKilled -= HandleSkeletonKilled;
            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetMonsterColor());

            GameObject go = presenter.View.gameObject;
            _activeSkeletonObjects.Remove(go);
            PoolSystem.Instance.Return(go);
            _activeSkeletons.Remove(presenter);
            presenter.Dispose();

            Debug.Log($"[Boss]\nSummoned Skeleton Killed\nRemaining : {_activeSkeletons.Count}");
        }

        // ── All Metal Colossus ────────────────────────────────────────────────

        private void SpawnOreCores()
        {
            var oreTypes = new BlockType[] { BlockType.Iron, BlockType.Silver, BlockType.Gold, BlockType.Diamond };
            // Shuffle for random assignment
            for (int i = 3; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = oreTypes[i]; oreTypes[i] = oreTypes[j]; oreTypes[j] = tmp;
            }

            Vector3[] offsets = {
                new Vector3(-1.2f, 0f,  1.0f),
                new Vector3( 1.2f, 0f,  1.0f),
                new Vector3(-1.2f, 0f, -0.3f),
                new Vector3( 1.2f, 0f, -0.3f)
            };

            int coreHp = 20 + Mathf.Max(0, (Model.SpawnDepth / 50) - 4) * 10;

            for (int i = 0; i < 4; i++)
            {
                var coreGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                coreGo.name = $"BossCore_{oreTypes[i]}";
                coreGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                Vector3 worldPos = _spawnPoint.position + _spawnPoint.TransformDirection(offsets[i]);
                coreGo.transform.position = worldPos;

                Color coreColor = oreTypes[i] switch
                {
                    BlockType.Iron    => new Color(0.55f, 0.55f, 0.6f),
                    BlockType.Silver  => new Color(0.8f,  0.8f,  0.9f),
                    BlockType.Gold    => new Color(1.0f,  0.84f, 0.0f),
                    BlockType.Diamond => new Color(0.0f,  0.9f,  1.0f),
                    _                 => Color.white
                };

                var coreView = coreGo.AddComponent<BossCoreView>();
                coreView.Setup(oreTypes[i], coreHp, coreColor);
                coreView.OnCoreDestroyed += HandleCoreDestroyed;

                _activeCores.Add(coreView);
                _coreObjects.Add(coreGo);

                Debug.Log($"[Boss]\nOre Core Spawned\nOre : {oreTypes[i]}\nHP : {coreHp}\nPos : {worldPos.x:F2},{worldPos.y:F2},{worldPos.z:F2}");
            }

            EffectSystem.Instance.SpawnDamageText(
                View3D.transform.position + Vector3.up * 0.8f,
                "DESTROY THE CORES!",
                new Color(1f, 0.8f, 0f));
        }

        private void HandleCoreDestroyed(BossCoreView core)
        {
            core.OnCoreDestroyed -= HandleCoreDestroyed;
            _activeCores.Remove(core);

            // Drop the ore this core held
            string itemId = core.OreType switch
            {
                BlockType.Iron    => "Item_Iron",
                BlockType.Silver  => "Item_Silver",
                BlockType.Gold    => "Item_Gold",
                BlockType.Diamond => "Item_Diamond",
                _                 => ""
            };

            if (!string.IsNullOrEmpty(itemId))
            {
                InventoryManager.Instance.AddItem(itemId, 1);
                EffectSystem.Instance.SpawnDamageText(
                    core.transform.position + Vector3.up * 0.5f,
                    $"+{core.OreType}",
                    Color.yellow);
            }

            EffectSystem.Instance.FlashScreen(new Color(1f, 0.8f, 0f, 0.3f), 0.2f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            // Destroy the core GameObject
            GameObject coreGo = core.gameObject;
            _coreObjects.Remove(coreGo);
            GameObject.Destroy(coreGo);

            Debug.Log($"[Boss]\nCore Destroyed\nOre : {core.OreType}\nRemaining Cores : {_activeCores.Count}");

            if (_activeCores.Count == 0)
            {
                _isInvincible = false;
                EffectSystem.Instance.SpawnDamageText(
                    View3D.transform.position + Vector3.up * 1.0f,
                    "COLOSSUS EXPOSED!",
                    Color.red);
                EffectSystem.Instance.ShakeCamera(0.5f, 0.2f);
                Debug.Log("[Boss]\nAll Cores Destroyed\nColossus Now Vulnerable");
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        private void ClearActiveMinions()
        {
            foreach (var pres in _activeBabySpiders)
                pres.Dispose();
            foreach (var go in _activeBabyObjects)
                if (go != null) PoolSystem.Instance.Return(go);
            _activeBabySpiders.Clear();
            _activeBabyObjects.Clear();

            foreach (var pres in _activeSkeletons)
                pres.Dispose();
            foreach (var go in _activeSkeletonObjects)
                if (go != null) PoolSystem.Instance.Return(go);
            _activeSkeletons.Clear();
            _activeSkeletonObjects.Clear();
        }

        private void DestroyOreCores()
        {
            foreach (var core in _activeCores)
                if (core != null) core.OnCoreDestroyed -= HandleCoreDestroyed;
            _activeCores.Clear();

            foreach (var go in _coreObjects)
                if (go != null) GameObject.Destroy(go);
            _coreObjects.Clear();
        }
    }
}
