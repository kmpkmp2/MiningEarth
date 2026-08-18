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

        // 슬라임 분열처럼 몬스터 사망 시 새 몬스터가 즉시 생겨나는 경우, 그 사망을 유발한
        // 플레이어 턴 직후 몬스터 턴을 건너뛰고 다시 플레이어에게 턴을 넘기기 위한 신호.
        event Action OnMonsterSplit;
    }
}
