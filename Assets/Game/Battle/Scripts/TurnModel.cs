namespace DeepEarth.Battle
{
    public enum BattleState
    {
        Idle,
        PlayerTurn,
        TargetSelect,
        ShieldSelect,
        MonsterTurn,
        BattleEnd
    }

    // 라운드 단위 턴 상태.
    public class TurnModel
    {
        public BattleState Phase { get; set; } = BattleState.Idle;
    }
}
