using System;
using System.Collections.Generic;

namespace DeepEarth.Battle
{
    // RunTurnLoopAsync가 스폰원(일반 몬스터/엘리트/보스)에 대해 필요로 하는 최소 계약.
    // CombatSystem은 이미 이 형태를 만족하고, 엘리트/보스는 MonsterSource로 구현한다.
    // Combat.MonsterPresenter를 명시적으로 정규화 — DeepEarth.Battle 네임스페이스 안이라 unqualified
    // "MonsterPresenter"는 같은 네임스페이스의 Battle.MonsterPresenter(래퍼)로 잘못 해석된다.
    public interface IMonsterSource
    {
        IReadOnlyList<Combat.MonsterPresenter> ActivePresenters { get; }
        bool HasActiveMonsters { get; }
        event Action<Combat.MonsterPresenter> OnMonsterSpawned;
    }
}
