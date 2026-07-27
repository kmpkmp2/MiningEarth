namespace DeepEarth.Battle
{
    public enum TurnPhase
    {
        PlayerTurn,
        MonsterTurn
    }

    // 라운드 단위 턴 상태 — Player 방어 플래그는 다음 Monster Turn 1회에만 적용되고 소멸한다.
    public class TurnModel
    {
        public TurnPhase Phase { get; set; } = TurnPhase.PlayerTurn;
        public bool PlayerIsDefending { get; set; }
    }
}
