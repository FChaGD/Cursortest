using System;
using System.Collections;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// "피격 시 즉시 흰색으로 바꿨다가 잠시 뒤 원래 색으로 복원"하는 연출 공용화 -
    /// BattleCharacterUnitView/BattleProtectedUnitView가 거의 동일한 코루틴을 각자 갖고 있었다(DRY,
    /// Docs/Refactor/2026-09-08_Field.md §3 수정 M). canRestore는 복원 시점에 다시 확인할 조건이 있을
    /// 때만 넘긴다(예: 사망 틴트를 덮어쓰지 않기 위한 unit.IsAlive) - null이면 항상 복원한다.
    /// </summary>
    internal static class BattleHitFlash
    {
        public static IEnumerator Run(SpriteRenderer bodyRenderer, Color flashColor, Color restoreColor, float durationSeconds, Func<bool> canRestore = null)
        {
            bodyRenderer.color = flashColor;
            yield return new WaitForSeconds(durationSeconds);
            if (bodyRenderer != null && (canRestore == null || canRestore()))
            {
                bodyRenderer.color = restoreColor;
            }
        }
    }
}
