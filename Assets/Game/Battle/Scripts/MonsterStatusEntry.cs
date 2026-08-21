namespace DeepEarth.Battle
{
    // 몬스터 하단 상태 아이콘 1개 분량의 표시 데이터. IconKey는 Addressable Sprite 키.
    public readonly struct MonsterStatusEntry
    {
        public readonly string IconKey;
        public readonly string Title;
        public readonly string Description;

        public MonsterStatusEntry(string iconKey, string title, string description)
        {
            IconKey = iconKey;
            Title = title;
            Description = description;
        }
    }
}
