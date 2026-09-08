using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// "엔트리 목록에서 키가 일치하는 항목 하나를 찾는" 선형 탐색을 공용화 - 여러 테이블 자산
    /// (CharacterStatsTableAsset 등)이 타입만 다를 뿐 동일한 foreach 탐색을 각자 구현하고 있었다(DRY,
    /// TacticsPanel.IndexOfOption과 같은 성격, Docs/Refactor/2026-09-08_전투도메인.md ① 수정 O).
    /// 테이블 엔트리 수가 작고(직업/적 타입 등 한 자릿수) 조회가 설정 시점에만 일어나 O(n) 그대로 둔다.
    /// </summary>
    internal static class TableEntryLookup
    {
        public static bool TryFind<TEntry, TKey>(IReadOnlyList<TEntry> entries, TKey key, Func<TEntry, TKey> keySelector, out TEntry entry)
        {
            var comparer = EqualityComparer<TKey>.Default;
            foreach (var candidate in entries)
            {
                if (comparer.Equals(keySelector(candidate), key))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
