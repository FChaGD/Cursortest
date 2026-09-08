using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 정비창(Formation) 8열×2행 슬롯 좌표를 전장 좌표로 바꾼다 - 열이 상행 진행 방향(전방/후방)을
    /// 나타내는 세로축, 행이 대형 폭을 나타내는 가로축이 되도록 배치 UI를 반시계 90도 회전시킨
    /// 규칙이다(Docs/기획/08-2026-09-01-전투_해석로직_기획.md §2). 간격은 Field 전투 뷰 실제 크기가 정해지기 전
    /// 임시값이라 상수로 뒀다 - 이 클래스는 MonoBehaviour가 아닌 순수 C# 객체라 SerializeField를
    /// 붙여도 인스펙터에 노출되지 않는다(Docs/설계/06-2026-08-31-전투_핵심루프_아키텍처.md §4).
    /// 스폰 반지름은 더 이상 고정값이 아니다 - 대형(아군 배치) 크기가 커지면 적도 그만큼 더 바깥에서
    /// 스폰해야 "전장을 벗어난 곳에서 스폰"이라는 전제가 대형 크기와 무관하게 항상 성립한다. 도주
    /// 이탈 거리도 같은 스폰 반지름에서 파생시켜, 두 값이 서로 다른 임의의 상수로 따로 놀지 않게 한다.
    /// 아군 좌표 변환(IAllyPositionLayout)과 스폰/반지름 계산(IBattleFieldGeometry)을 인터페이스
    /// 레벨에서 분리했다(방향성 지시 축이 세 번째 반지름 계산을 얹기 전에 06번 문서 §10 backlog를
    /// 정리, Docs/설계/12번 §5.2) - 구현 클래스는 FormationExtentRadius 같은 내부 헬퍼를 공유해야
    /// 해서 그대로 하나다.
    /// </summary>
    public class BattleFieldLayout : IAllyPositionLayout, IBattleFieldGeometry
    {
        // 간격/마진/경계 상수와 스폰지점·반지름 계산 공식은 BattleFieldRadiusFormulas로 옮겼다 -
        // BattleTestFieldLayout(배틀 테스트 씬)이 "행 몇 개인지"만 다르고 완전히 같은 공식을 쓰고
        // 있었다(DRY, Docs/Refactor/2026-09-08_전투도메인.md ④ 수정 S). 이 클래스는 "행 2 고정"
        // 가정만 FormationExtentRadius에 남긴다.
        public Vector2 ComputeAllyPosition(int column, int row, int columnCount)
        {
            var y = (column - (columnCount - 1) / 2f) * BattleFieldRadiusFormulas.ColumnSpacing;
            var x = (row - 0.5f) * BattleFieldRadiusFormulas.RowSpacing;
            return new Vector2(x, y);
        }

        public Vector2 ComputeSpawnPoint(int spawnPointIndex, int columnCount)
            => BattleFieldRadiusFormulas.ComputeSpawnPoint(spawnPointIndex, ComputeSpawnRadius(columnCount));

        // 도주 유닛이 이만큼 이동하면 "전장을 완전히 벗어났다"고 본다 - 전장(실제 교전이 벌어지는
        // 범위)의 반지름과 같은 값이다. 인터페이스에는 이 메서드(도주 판정 기준)만 노출한다 - 지금은
        // "전장 반지름" 자체를 필요로 하는 다른 소비자가 없다(ISP, 필요해지면 인터페이스에 추가).
        public float ComputeFleeTravelDistance(int columnCount) => ComputeFieldRadius(columnCount);

        public float ComputeFieldRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeFieldRadius(ComputeSpawnRadius(columnCount));

        // 대형 중심 기준 "활동 반경 - 표준" 프리셋(Docs/기획/12번 §2.2) - 스폰/전장 반지름과 같은
        // FormationExtentRadius 파생 패밀리지만 마진 값(TacticsTuning)이 다르다.
        public float ComputeStandardActivityRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeStandardActivityRadius(FormationExtentRadius(columnCount));

        public float ComputeSpawnRadius(int columnCount) => BattleFieldRadiusFormulas.ComputeSpawnRadius(FormationExtentRadius(columnCount));

        // 대형의 네 모서리 중 원점에서 가장 먼 지점까지의 거리 - 행은 항상 2행(ComputeAllyPosition의
        // (row - 0.5f) 가정)이라 행 방향 반쪽 폭은 RowSpacing*0.5로 고정이고, 열 방향 반쪽 폭만
        // columnCount에 따라 늘어난다.
        private static float FormationExtentRadius(int columnCount)
        {
            var halfColumnExtent = (columnCount - 1) / 2f * BattleFieldRadiusFormulas.ColumnSpacing;
            var halfRowExtent = BattleFieldRadiusFormulas.RowSpacing * 0.5f;
            return Mathf.Sqrt(halfColumnExtent * halfColumnExtent + halfRowExtent * halfRowExtent);
        }
    }
}
