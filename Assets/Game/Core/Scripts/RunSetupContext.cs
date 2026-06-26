namespace DeepEarth.Core
{
    /// <summary>
    /// RunSetupPanel에서 설정된 런 시작 데이터를 LoadingScene으로 전달하는 정적 컨텍스트.
    /// 씬 전환 간에도 유지되며 런 종료 시 Reset()으로 초기화한다.
    /// </summary>
    public static class RunSetupContext
    {
        public static CharacterID SelectedCharacter          { get; private set; } = CharacterID.Prisoner;
        public static string      SelectedPickaxeID           { get; private set; } = "pickaxe_wood";
        public static bool        IsRunSetupComplete           { get; set; }         = false;
        public static bool        IsInitializedByLoadingScene  { get; set; }         = false;

        public static void MarkComplete(CharacterID character, string pickaxeID)
        {
            SelectedCharacter  = character;
            SelectedPickaxeID  = pickaxeID;
            IsRunSetupComplete = true;
        }

        public static void Reset()
        {
            SelectedCharacter           = CharacterID.Prisoner;
            SelectedPickaxeID           = "pickaxe_wood";
            IsRunSetupComplete          = false;
            IsInitializedByLoadingScene = false;
        }
    }
}
