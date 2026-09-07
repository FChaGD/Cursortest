using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 정비창 그리드에서 진행 중인 이동 활동 하나를 그리는 데 필요한 표시 정보(Docs/기획/20번 §3.3,
    /// 설계 25번 §5.2) - FormationGridView.SetMovePaths가 이 목록을 받아 경로선(FormationPathLineView)과
    /// 이동 중 유닛 아이콘(별도 풀, 렌더 순서상 항상 그 위)을 함께 그린다.
    /// </summary>
    public readonly struct FormationMovePathVisual
    {
        public IReadOnlyList<int> PathSlotIndices { get; }
        public float Progress01 { get; }
        public Sprite Icon { get; }
        // FormationActivity.PartialSegmentIndex/Weight를 그대로 실어 나른다(기획 21번, 설계 26번 §2) -
        // 기본값(-1/1)은 "부분 구간 없음"(일반 이동)과 동일하게 동작한다.
        public int PartialSegmentIndex { get; }
        public float PartialSegmentWeight { get; }

        public FormationMovePathVisual(IReadOnlyList<int> pathSlotIndices, float progress01, Sprite icon, int partialSegmentIndex = -1, float partialSegmentWeight = 1f)
        {
            PathSlotIndices = pathSlotIndices;
            Progress01 = progress01;
            Icon = icon;
            PartialSegmentIndex = partialSegmentIndex;
            PartialSegmentWeight = partialSegmentWeight;
        }
    }
}
