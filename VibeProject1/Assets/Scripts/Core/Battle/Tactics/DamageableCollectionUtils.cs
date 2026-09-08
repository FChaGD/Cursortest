using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// "IDamageable 목록" 연산 공용화 - FrontlineFormationCoordinator/RangedSurroundCoordinator가
    /// 거의 동일한 ContainsReference/ComputeAveragePosition/AddDistinct를 각자 구현하고 있었다(DRY,
    /// Docs/Refactor/2026-09-08_전투도메인.md ② 수정 Q). IDamageable은 Equals를 오버라이드하지 않아
    /// 참조 동등성으로 충분하다.
    /// </summary>
    internal static class DamageableCollectionUtils
    {
        // IReadOnlyList<IDamageable>는 List<T>와 달리 Contains가 없어 수동 순회가 필요하다.
        public static bool ContainsReference(IReadOnlyList<IDamageable> list, IDamageable value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], value)) return true;
            }
            return false;
        }

        public static void AddDistinct(List<IDamageable> list, IDamageable candidate)
        {
            if (candidate != null && !list.Contains(candidate)) list.Add(candidate);
        }

        public static Vector2 ComputeAveragePosition(IReadOnlyList<IDamageable> units)
        {
            if (units.Count == 0) return Vector2.zero;

            var sum = Vector2.zero;
            foreach (var unit in units) sum += unit.Position;
            return sum / units.Count;
        }
    }
}
