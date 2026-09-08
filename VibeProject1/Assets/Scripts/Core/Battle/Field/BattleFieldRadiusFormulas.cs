using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 전장 반지름/스폰 지점 계산 공식 공용화 - BattleFieldLayout(프로덕션, 행 2 고정)과
    /// BattleTestFieldLayout(배틀 테스트 씬, 행 가변)이 "행 몇 개인지"만 다르고 나머지 공식은
    /// 완전히 동일했다(DRY, Docs/Refactor/2026-09-08_전투도메인.md ④ 수정 S). "대형 반지름"
    /// (FormationExtentRadius)만 각 클래스가 자기 방식대로 계산해 이 공용 공식에 넘긴다.
    /// </summary>
    internal static class BattleFieldRadiusFormulas
    {
        public const float ColumnSpacing = 1f;
        public const float RowSpacing = 1f;
        public const float SpawnRadiusMargin = 10f;
        public const float FieldBoundaryGap = 2f;

        public static Vector2 ComputeSpawnPoint(int spawnPointIndex, float spawnRadius)
        {
            var angleRad = spawnPointIndex * (360f / BattleFieldGeometry.SpawnPointCount) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * spawnRadius;
        }

        public static float ComputeFieldRadius(float spawnRadius) => spawnRadius - FieldBoundaryGap;

        public static float ComputeStandardActivityRadius(float formationExtentRadius) => formationExtentRadius + TacticsTuning.StandardRadiusMarginMeters;

        public static float ComputeSpawnRadius(float formationExtentRadius) => formationExtentRadius + SpawnRadiusMargin;
    }
}
