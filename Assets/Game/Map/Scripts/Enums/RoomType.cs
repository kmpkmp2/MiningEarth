namespace DeepEarth.Map
{
    /// <summary>The type of room assigned to an active map node.</summary>
    public enum RoomType
    {
        Unknown,
        Mine,
        Monster,
        Elite,
        Event,
        Merchant,
        Treasure,
        Rest,
        Boss,
        Start   // Mine Entrance — 광산 입구, 각 50층 구간의 시작점
    }
}
