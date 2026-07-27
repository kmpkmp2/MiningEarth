namespace DeepEarth.Battle
{
    // 한 번의 몬스터 조우(Node 진입~클리어) 동안의 턴제 전투 상태 총괄. SaveData에 저장되지 않는 휘발성 데이터.
    public class BattleModel
    {
        public TurnModel Turn { get; } = new TurnModel();
        public IntentModel Intent { get; } = new IntentModel();
    }
}
