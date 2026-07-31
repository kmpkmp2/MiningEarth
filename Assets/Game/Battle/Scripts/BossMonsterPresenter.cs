using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;
using DeepEarth.Combat;
using DeepEarth.Map;

namespace DeepEarth.Battle
{
    // 4종 보스 전용 턴제 래퍼. 보스별 고유 메카닉(어미 동굴거미 소환, 해골 군주 임계값 웨이브,
    // 전금속 거인 파트 타겟팅)을 훅 오버라이드로 처리한다. 돌 골렘은 기본 동작 그대로.
    public class BossMonsterPresenter : MonsterPresenter
    {
        public enum Role { StoneGolem, MotherCaveSpider, SkeletonWarlord, ColossusBody, ColossusCore, CaveRat }

        private readonly Role _role;
        private readonly Combat.MonsterSource _source;
        private readonly Transform _spawnPoint;
        private readonly MultiPartBossCoordinator _coordinator;
        private readonly BlockType _oreType;
        private readonly CancellationTokenSource _spawnCts = new CancellationTokenSource();

        private bool _threshold75, _threshold50, _threshold25;

        // CaveRat: 공격 1회 실행마다 다음 공격에 누적되는 랜덤(1~5) 데미지 보너스.
        private int _caveRatRageBonus;

        public override bool IsTargetable => _role != Role.ColossusBody || !_coordinator.MainBodyIsInvincible;

        public BossMonsterPresenter(
            Combat.MonsterPresenter combatPresenter, MonsterPatternModel pattern, TurnModel turn,
            Role role, Combat.MonsterSource source, Transform spawnPoint,
            MultiPartBossCoordinator coordinator = null, BlockType oreType = default)
            : base(combatPresenter, pattern, turn)
        {
            _role = role;
            _source = source;
            _spawnPoint = spawnPoint;
            _coordinator = coordinator;
            _oreType = oreType;

            if (_role == Role.MotherCaveSpider)
                SpawnInitialBabiesAsync().Forget();
        }

        public override void Dispose()
        {
            _spawnCts.Cancel();
            _spawnCts.Dispose();
            base.Dispose();
        }

        public override async UniTask ReceivePlayerAttackAsync()
        {
            // 코어가 이 공격으로 죽으면 OnMonsterKilled 핸들러가 View GameObject를 즉시 Destroy하므로,
            // base 호출(및 그 이후의 히트 이펙트 대기) 전에 위치를 미리 캐싱해둔다.
            Vector3 lastKnownPos = _role == Role.ColossusCore && CombatPresenter.View != null
                ? CombatPresenter.View.transform.position
                : Vector3.zero;

            await base.ReceivePlayerAttackAsync();

            if (_role == Role.SkeletonWarlord && !CombatPresenter.Model.IsDead)
                CheckWarlordThresholds();

            if (_role == Role.ColossusCore && CombatPresenter.Model.IsDead)
                _coordinator.HandlePartDestroyed(_oreType, lastKnownPos);
        }

        public override async UniTask<PatternStepData> ExecuteTurnAsync()
        {
            // 전금속 거인 본체: 코어가 하나라도 살아있는 동안 자신의 턴을 소모하지 않는다
            // (기존 실시간 경로의 "does not attack while cores are alive"와 동일한 동작).
            if (_role == Role.ColossusBody && _coordinator.MainBodyIsInvincible)
                return CurrentStep;

            var executedStep = CurrentStep;
            var result = await base.ExecuteTurnAsync();

            // CaveRat: 공격을 실행할 때마다 다음 공격 데미지에 랜덤(1~5)이 누적된다.
            if (_role == Role.CaveRat && !CombatPresenter.Model.IsDead && executedStep != null &&
                (executedStep.intentType == IntentType.Attack || executedStep.intentType == IntentType.HeavyAttack))
            {
                _caveRatRageBonus += Random.Range(1, 6);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Boss]\nCaveRat Rage\nBonus Damage : {_caveRatRageBonus}");
#endif
            }

            return result;
        }

        protected override int ModifyOutgoingDamage(int baseDamage)
        {
            if (_role == Role.CaveRat) return baseDamage + _caveRatRageBonus;
            return base.ModifyOutgoingDamage(baseDamage);
        }

        // ── Mother Cave Spider ───────────────────────────────────────────────

        protected override void OnSummonStep()
        {
            if (_role != Role.MotherCaveSpider) return;
            if (CountAliveBabies() >= 10) return;
            SpawnBabySpiderAsync().Forget();
        }

        private async UniTaskVoid SpawnInitialBabiesAsync()
        {
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    if (CombatPresenter.Model.IsDead || _spawnCts.IsCancellationRequested) return;
                    await SpawnBabySpiderAsync();
                    await UniTask.Delay(300, cancellationToken: _spawnCts.Token);
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private int CountAliveBabies() => _source.ActivePresenters.Count(p => p.Model.Type == MonsterType.CaveSpider);

        private async UniTask SpawnBabySpiderAsync()
        {
            GameObject go = await PoolSystem.Instance.GetAsync(AddressableKeys.MonsterSpider, _spawnPoint);
            if (go == null || CombatPresenter.Model.IsDead)
            {
                if (go != null) PoolSystem.Instance.Return(go);
                return;
            }

            Vector3 localOffset = new Vector3(Random.Range(-2.0f, 2.0f), 0f, Random.Range(-0.5f, 1.5f));
            go.transform.position = _spawnPoint.position + _spawnPoint.TransformDirection(localOffset);
            go.transform.rotation = _spawnPoint.rotation;

            var view = go.GetComponent<Combat.MonsterView>();
            if (view == null) view = go.AddComponent<Combat.MonsterView>();
            view.InitializeSpawn(CountAliveBabies());

            var model = new Combat.MonsterModel(MonsterType.CaveSpider, maxHp: 1, damage: 1);
            var presenter = new Combat.MonsterPresenter(model, view);
            presenter.OnMonsterKilled += _ =>
            {
                PoolSystem.Instance.Return(go);
                presenter.Dispose();
            };
            _source.Add(presenter);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Boss]\nBaby Spider Spawned\nCount : {CountAliveBabies()}");
#endif
        }

        // ── Skeleton Warlord ─────────────────────────────────────────────────

        private void CheckWarlordThresholds()
        {
            float hpRatio = (float)CombatPresenter.Model.CurrentHP / CombatPresenter.Model.MaxHP;

            if (!_threshold75 && hpRatio <= 0.75f) { _threshold75 = true; SpawnSkeletonWaveAsync(2).Forget(); }
            if (!_threshold50 && hpRatio <= 0.50f) { _threshold50 = true; SpawnSkeletonWaveAsync(2).Forget(); }
            if (!_threshold25 && hpRatio <= 0.25f) { _threshold25 = true; SpawnSkeletonWaveAsync(3).Forget(); }
        }

        private async UniTaskVoid SpawnSkeletonWaveAsync(int count)
        {
            try
            {
                Vector3 textPos = CombatPresenter.View != null ? CombatPresenter.View.transform.position + Vector3.up * 0.8f : Vector3.up;
                EffectSystem.Instance.SpawnDamageText(textPos, "SUMMON!", new Color(0.8f, 0.8f, 1f));

                for (int i = 0; i < count; i++)
                {
                    if (CombatPresenter.Model.IsDead || _spawnCts.IsCancellationRequested) return;
                    await SpawnWarlordSkeletonAsync();
                    await UniTask.Delay(200, cancellationToken: _spawnCts.Token);
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Boss]\nSkeleton Wave Spawned\nCount : {count}");
#endif
            }
            catch (System.OperationCanceledException) { }
        }

        private async UniTask SpawnWarlordSkeletonAsync()
        {
            GameObject go = await PoolSystem.Instance.GetAsync(AddressableKeys.MonsterSkeleton, _spawnPoint);
            if (go == null || CombatPresenter.Model.IsDead)
            {
                if (go != null) PoolSystem.Instance.Return(go);
                return;
            }

            Vector3 localOffset = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-0.5f, 2.0f));
            go.transform.position = _spawnPoint.position + _spawnPoint.TransformDirection(localOffset);
            go.transform.rotation = _spawnPoint.rotation;

            var view = go.GetComponent<Combat.MonsterView>();
            if (view == null) view = go.AddComponent<Combat.MonsterView>();
            view.InitializeSpawn(0);

            var model = new Combat.MonsterModel(MonsterType.Skeleton, maxHp: 3, damage: 2);
            var presenter = new Combat.MonsterPresenter(model, view);
            presenter.OnMonsterKilled += _ =>
            {
                PoolSystem.Instance.Return(go);
                presenter.Dispose();
            };
            _source.Add(presenter);
        }
    }
}
