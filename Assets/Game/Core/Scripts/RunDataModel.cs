namespace DeepEarth.Core
{
    /// <summary>
    /// 단일 런(Run)의 초기 스탯을 담는 모델.
    /// LoadingPresenter에서 Create()로 생성되고 RunEnd 시 Clear()로 제거된다.
    /// PlayerData(SaveData)와 분리하여 런 전용 데이터만 보관한다.
    /// </summary>
    public class RunDataModel
    {
        private static RunDataModel _current;
        public static RunDataModel Current => _current;

        public CharacterID Character          { get; }
        public string      PickaxeID          { get; }
        public int         StartingMaxHP       { get; private set; }
        public int         StartingMiningPower { get; private set; }
        public int         StartingAttackPower { get; private set; }
        public int         StartingPickaxeDur  { get; private set; }
        public int         StartingInvSize     { get; private set; }

        private RunDataModel(CharacterID character, string pickaxeID)
        {
            Character = character;
            PickaxeID = pickaxeID;
        }

        public static RunDataModel Create(CharacterID character, string pickaxeID)
        {
            _current = new RunDataModel(character, pickaxeID);
            return _current;
        }

        public void ApplyStats(int maxHP, int miningPower, int attackPower, int pickaxeDur, int invSize)
        {
            StartingMaxHP       = maxHP;
            StartingMiningPower = miningPower;
            StartingAttackPower = attackPower;
            StartingPickaxeDur  = pickaxeDur;
            StartingInvSize     = invSize;
        }

        public static void Clear() => _current = null;
    }
}
