using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public enum FormationActivityKind { Adding, Moving }

    /// <summary>
    /// Field에서 진행 중인 배치/이동 행동 하나(Docs/설계/25번 §3.1). Adding은 OriginSlotIndex(-1)/
    /// PathSlotIndices(빈 리스트)가 의미 없다. RequiredSeconds는 Adding=소요시간 그대로, Moving=경로
    /// 길이/이동 속도로 환산한 값 - 두 종류를 같은 ElapsedSeconds/RequiredSeconds 진행률로 다뤄
    /// 일시정지/재개/강제완료 로직을 하나로 공유한다(기획 20번 §3.2/§3.3 대칭 구조).
    /// </summary>
    public sealed class FormationActivity
    {
        public const int NoSlot = -1;

        public string UnitId { get; }
        public FormationActivityKind Kind { get; }
        public int TargetSlotIndex { get; }
        public int OriginSlotIndex { get; }
        public IReadOnlyList<int> PathSlotIndices { get; }
        public float RequiredSeconds { get; }
        public float ElapsedSeconds { get; internal set; }
        public bool IsPaused { get; internal set; }

        public float Progress01 => RequiredSeconds <= 0f ? 1f : Mathf.Clamp01(ElapsedSeconds / RequiredSeconds);

        internal FormationActivity(string unitId, FormationActivityKind kind, int targetSlotIndex, int originSlotIndex, IReadOnlyList<int> pathSlotIndices, float requiredSeconds)
        {
            UnitId = unitId;
            Kind = kind;
            TargetSlotIndex = targetSlotIndex;
            OriginSlotIndex = originSlotIndex;
            PathSlotIndices = pathSlotIndices;
            RequiredSeconds = requiredSeconds;
        }
    }
}
