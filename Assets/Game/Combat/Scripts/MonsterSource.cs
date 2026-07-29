using System;
using System.Collections.Generic;

namespace DeepEarth.Combat
{
    // 상시 로스터가 없는 전투(엘리트/보스)용 IMonsterSource 구현체.
    // CombatSystem처럼 MonoBehaviour 싱글톤일 필요가 없는 일회성 전투 스코프에 사용한다.
    public class MonsterSource : DeepEarth.Battle.IMonsterSource
    {
        private readonly List<MonsterPresenter> _active = new List<MonsterPresenter>();

        public IReadOnlyList<MonsterPresenter> ActivePresenters => _active;
        public bool HasActiveMonsters => _active.Count > 0;
        public event Action<MonsterPresenter> OnMonsterSpawned;

        public void Add(MonsterPresenter presenter)
        {
            _active.Add(presenter);
            presenter.OnMonsterKilled += HandleKilled;
            OnMonsterSpawned?.Invoke(presenter);
        }

        private void HandleKilled(MonsterPresenter presenter)
        {
            presenter.OnMonsterKilled -= HandleKilled;
            _active.Remove(presenter);
        }
    }
}
