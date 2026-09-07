using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 정비창 그리드(열×행) 위에서 점유 슬롯을 피하는 최단 경로를 찾는다(Docs/설계/25번 §4). 그리드
    /// 규모가 작아(최대 8×2 수준) 너비 우선 탐색(BFS)이면 충분하다 - A*의 이점이 없다.
    /// </summary>
    internal static class FormationPathFinder
    {
        // blocked: 점유된 슬롯 판정(FormationLayout에 실제 기록된 슬롯 + 다른 진행 중 활동의
        // TargetSlotIndex 합집합, 호출자가 계산해 넘긴다). origin/target 자신은 blocked여도 항상
        // 통과 가능한 것으로 본다 - 출발점은 자기 자신이 비우는 중인 슬롯, 도착점은 이 이동이
        // 예약한 슬롯이기 때문이다.
        public static IReadOnlyList<int> FindPath(int origin, int target, int columnCount, int rowCount, IReadOnlyCollection<int> blocked)
        {
            if (origin == target) return new[] { origin };

            var blockedSet = new HashSet<int>(blocked);
            var queue = new Queue<int>();
            var cameFrom = new Dictionary<int, int>();
            queue.Enqueue(origin);
            cameFrom[origin] = origin;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    return ReconstructPath(cameFrom, origin, target);
                }

                foreach (var next in GetNeighbors(current, columnCount, rowCount))
                {
                    if (cameFrom.ContainsKey(next)) continue;
                    if (next != target && blockedSet.Contains(next)) continue;
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            // 경로가 없으면(사방이 막힘) 직선(2점) 폴백 - 시각적으로 다른 유닛과 겹쳐 보일 수 있으나
            // 배치 자체를 막지는 않는다(설계 25번 §4, 제작 단계 재검토 대상).
            return new[] { origin, target };
        }

        private static IEnumerable<int> GetNeighbors(int index, int columnCount, int rowCount)
        {
            var col = index % columnCount;
            var row = index / columnCount;
            if (col > 0) yield return index - 1;
            if (col < columnCount - 1) yield return index + 1;
            if (row > 0) yield return index - columnCount;
            if (row < rowCount - 1) yield return index + columnCount;
        }

        private static IReadOnlyList<int> ReconstructPath(Dictionary<int, int> cameFrom, int origin, int target)
        {
            var path = new List<int> { target };
            var current = target;
            while (current != origin)
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
