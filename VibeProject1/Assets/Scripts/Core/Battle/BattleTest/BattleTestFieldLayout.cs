using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 배틀 테스트 씬 전용 파생 - BattleFieldLayout(실제 게임)과 같은 수식을 쓰되, 행 개수가 2로
    /// 고정된 BattleFieldLayout과 달리 열/행 모두 인스턴스 상태(RowCount/ColumnCount)로 가변이다.
    /// IAllyPositionLayout/IBattleFieldGeometry 인터페이스는 columnCount만 매개변수로 받고 row는
    /// 아예 모른다(실제 게임은 항상 2행을 가정) - 그래서 RowCount는 인터페이스에 없는, 이 클래스만의
    /// 추가 상태로 둔다(인터페이스 변경 없이 실제 게임 코드에 영향을 주지 않기 위함).
    /// 대열 범위 기즈모(BattleTestExtentGizmoView)가 매 프레임 ColumnCount/RowCount를 읽어 박스를
    /// 그리고, 모서리 드래그/숫자 입력이 이 값을 직접 바꾼다.
    /// </summary>
    public class BattleTestFieldLayout : IAllyPositionLayout, IBattleFieldGeometry
    {
        // 간격/마진/경계 상수와 스폰지점·반지름 계산 공식은 BattleFieldRadiusFormulas로 옮겼다 -
        // BattleFieldLayout(프로덕션)과 "행 몇 개인지"만 다르고 완전히 같은 공식을 쓰고 있었다(DRY,
        // Docs/Refactor/2026-09-08_전투도메인.md ④ 수정 S). 이 클래스는 가변 RowCount 가정만
        // FormationExtentRadius/HalfExtentX/SetExtentFromCorner에 남긴다.
        public int ColumnCount { get; set; } = FormationLayout.DefaultColumnCount;
        public int RowCount { get; set; } = 2;

        public Vector2 ComputeAllyPosition(int column, int row, int columnCount)
        {
            var y = (column - (columnCount - 1) / 2f) * BattleFieldRadiusFormulas.ColumnSpacing;
            var x = (row - (RowCount - 1) / 2f) * BattleFieldRadiusFormulas.RowSpacing;
            return new Vector2(x, y);
        }

        public Vector2 ComputeSpawnPoint(int spawnPointIndex, int columnCount)
            => BattleFieldRadiusFormulas.ComputeSpawnPoint(spawnPointIndex, ComputeSpawnRadius(columnCount));

        public float ComputeFleeTravelDistance(int columnCount) => ComputeFieldRadius(columnCount);

        public float ComputeFieldRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeFieldRadius(ComputeSpawnRadius(columnCount));

        public float ComputeStandardActivityRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeStandardActivityRadius(FormationExtentRadius(columnCount));

        public float ComputeSpawnRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeSpawnRadius(FormationExtentRadius(columnCount));

        // BattleFieldLayout과 달리 halfRowExtent도 RowCount에서 파생된다(열 공식과 대칭).
        private float FormationExtentRadius(int columnCount)
        {
            var halfColumnExtent = (columnCount - 1) / 2f * BattleFieldRadiusFormulas.ColumnSpacing;
            var halfRowExtent = (RowCount - 1) / 2f * BattleFieldRadiusFormulas.RowSpacing;
            return Mathf.Sqrt(halfColumnExtent * halfColumnExtent + halfRowExtent * halfRowExtent);
        }

        // 대열 범위 기즈모(BattleTestExtentGizmoView)가 그릴 사각형의 네 모서리(월드 좌표, 원점 중심).
        // ComputeAllyPosition과 같은 축 대응을 쓴다 - column→Y축, row→X축(클래스 요약 주석의 "반시계
        // 90도 회전" 그대로). "가로/세로"로 이름 붙이면 이 축 반전과 헷갈리므로 축을 직접 반환한다.
        public Vector2 ExtentMin => new(-HalfExtentX, -HalfExtentY);
        public Vector2 ExtentMax => new(HalfExtentX, HalfExtentY);

        private float HalfExtentX => (RowCount - 1) / 2f * BattleFieldRadiusFormulas.RowSpacing;
        private float HalfExtentY => (ColumnCount - 1) / 2f * BattleFieldRadiusFormulas.ColumnSpacing;

        // 모서리 드래그(BattleTestExtentGizmoView)가 호출한다 - 원점에서 대칭이라 절댓값의 2배가
        // 전체 폭/높이다. HalfExtent = (Count-1)/2*Spacing 공식의 역산(Count = 2*Half/Spacing + 1).
        public void SetExtentFromCorner(Vector2 worldCorner)
        {
            RowCount = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.Abs(worldCorner.x) / BattleFieldRadiusFormulas.RowSpacing + 1f));
            ColumnCount = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.Abs(worldCorner.y) / BattleFieldRadiusFormulas.ColumnSpacing + 1f));
        }
    }
}
