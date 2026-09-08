using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// IFormationEditingHandler에서 분리된 Field 전용 확장(ISP, Docs/Refactor/2026-09-08_공통.md §6.3
    /// 수정 G) - "배경에서 진행 중인 배치/이동 활동"이라는 개념 자체가 Field(IFieldFormationActivityRepository)
    /// 에만 존재해, Hub는 이 인터페이스를 구현하지 않는다. FormationGridEditor는 handler를
    /// IFormationActivityHandler로 캐스팅해 보관하고(Hub면 자연히 null), null이면 "활동 없음"으로
    /// 처리한다.
    /// </summary>
    internal interface IFormationActivityHandler
    {
        // 배치 완료는 아니지만(GetDisplayLayout에는 아직 안 나타남) 이미 다른 용도로 예약된 유닛인지 -
        // IFieldFormationActivityRepository.IsUnitBusy를 그대로 반환한다(팔레트 잔여수 계산에 반영,
        // 설계 25번 §3.3).
        bool IsUnitReserved(string unitId);

        // 이동 중인 유닛의 도착 고스트를 드래그해 목적지를 바꾼다(기획 21번, 설계 26번 §4).
        void HandleRedirectMove(string unitId, int newTargetSlotIndex);

        // 슬롯 오버레이/경로선 표시용(설계 25번 §5) - IFieldFormationActivityRepository.ActiveActivities를
        // 그대로 반환한다. 유닛 아이콘 해석은 FormationGridEditor가 이미 갖고 있는 로스터 캐시
        // (unitsById)로 충분해 별도 메서드가 없다.
        IReadOnlyList<FormationActivity> GetActiveActivities();
    }
}
