namespace Game.Core.Editor
{
    /// <summary>
    /// ScriptableObject 테이블 자산 경로의 단일 소스. 각 임포터(Tools/Game/Table/Import ...)가 만드는
    /// 자산과, 그 자산을 배선하는 인스톨러(ManagerHierarchyInstaller, BattleTestSceneInstaller)가
    /// 각자 같은 경로 문자열을 최대 3중으로 중복 선언하던 문제를 해소한다
    /// (Docs/Refactor/2026-09-08_공통.md 확장성 문제점 1). 폴더명의 "ScriptableObejct" 오타는 자산
    /// 리네임(GUID/씬 참조 영향)이 필요한 별도 작업이라 이번엔 그대로 옮겨왔다.
    /// </summary>
    internal static class TableAssetPaths
    {
        private const string Folder = "Assets/Prefabs/ScriptableObejct";

        public const string CharacterStatsTable = Folder + "/CharacterStatsTable.asset";
        public const string CharacterStringsTable = Folder + "/CharacterStringsTable.asset";
        public const string EnemyStatsTable = Folder + "/EnemyStatsTable.asset";
        public const string EnemyEncounterCompositionTable = Folder + "/EnemyEncounterCompositionTable.asset";
        public const string EnemyStringsTable = Folder + "/EnemyStringsTable.asset";
        public const string PartyPolicyCatalog = Folder + "/PartyTacticsPolicyCatalog.asset";
        public const string PartyPolicyStringsTable = Folder + "/PartyTacticsPolicyStringsTable.asset";
        public const string RoleGroupTacticsCatalog = Folder + "/RoleGroupTacticsCatalog.asset";
        public const string RoleGroupTacticsStringsTable = Folder + "/RoleGroupTacticsStringsTable.asset";
        public const string MercenaryRoleGroupMap = Folder + "/MercenaryRoleGroupMap.asset";
        public const string TripCityMap = Folder + "/TripCityMap.asset";
        public const string TripCityStringsTable = Folder + "/TripCityStringsTable.asset";
    }
}
