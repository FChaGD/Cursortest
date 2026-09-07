using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// waypoints와 progress01(0=출발지, 1=도착지)로 경로 위 보간 위치를 계산한다(설계 25번 §4/§7).
    /// 각 구간(슬롯 사이)이 동일한 소요시간을 갖는다는 전제(FormationTiming.MoveSecondsPerSlot)를
    /// 그대로 이용해 구간 균등 보간한다. 정비창 UI의 이동 아이콘(FormationGridView)과 전투 시뮬레이션의
    /// 중단 지점 스폰 좌표(LiveBattleSimulationRule)가 같은 회피 경로를 서로 다른 좌표계(UI 앵커
    /// 좌표 vs 전장 월드 좌표)로 그려야 해서, 좌표계 변환은 각 호출자가 맡고 이 유틸리티는 순수
    /// 보간 수학만 공유한다 - 출발/도착 두 점만 Lerp하면 경로가 굴곡질 때(장애물 회피) 중단 지점이
    /// 실제 경로에서 벗어나 다른 슬롯 위치와 겹쳐 보이는 버그(실전 검증 2026-09-07)가 있었다.
    /// </summary>
    internal static class FormationPathInterpolation
    {
        public static Vector2 Evaluate(IReadOnlyList<Vector2> waypoints, float progress01)
        {
            if (waypoints.Count == 0) return Vector2.zero;
            if (waypoints.Count == 1) return waypoints[0];

            var segmentCount = waypoints.Count - 1;
            var scaled = Mathf.Clamp01(progress01) * segmentCount;
            var segmentIndex = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
            var t = scaled - segmentIndex;
            return Vector2.Lerp(waypoints[segmentIndex], waypoints[segmentIndex + 1], t);
        }
    }
}
