using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// FormationGridEditor가 드롭/드래그/제거 이벤트를 감지한 뒤 "실제로 무엇을 반영할지"를 위임하는
    /// 정책 인터페이스 - Hub(로컬 편집+Apply 버튼)와 Field(즉시 반영+배치 시간)가 이 인터페이스의
    /// 구현체만 다르다(Docs/기획/20번 §3.1, 설계 25번 §2.2).
    /// </summary>
    internal interface IFormationEditingHandler
    {
        // FormationGridEditor가 렌더링에 쓸 "현재 실제 반영 상태" 스냅샷을 요청한다 - Hub는 로컬
        // 편집 중인 사본을, Field는 IFormationRepository의 현재 값을 그대로 돌려준다.
        FormationLayout GetDisplayLayout();

        // 배치 완료는 아니지만(GetDisplayLayout에는 아직 안 나타남) 이미 다른 용도로 예약된 유닛인지 -
        // Hub는 항상 false, Field는 IFieldFormationActivityRepository.IsUnitBusy를 그대로 반환한다
        // (팔레트 잔여수 계산에 반영, 설계 25번 §3.3).
        bool IsUnitReserved(string unitId);

        // 팔레트에서 빈/점유 슬롯으로 드롭(신규 배치 시도).
        void HandlePaletteDrop(IFormationUnit unit, int targetSlotIndex);

        // 그리드 내 슬롯→슬롯 이동(드래그).
        void HandleGridMove(string unitId, int originSlotIndex, int targetSlotIndex);

        // 그리드 밖으로 드래그해 배치 취소, 혹은 명시적 제거.
        void HandleRemove(string unitId, int slotIndex);

        // 슬롯 오버레이/경로선 표시용(설계 25번 §5) - Hub는 항상 빈 목록, Field는
        // IFieldFormationActivityRepository.ActiveActivities를 그대로 반환한다. 유닛 아이콘 해석은
        // FormationGridEditor가 이미 갖고 있는 로스터 캐시(unitsById)로 충분해 별도 메서드가 없다.
        IReadOnlyList<FormationActivity> GetActiveActivities();

#if UNITY_EDITOR
        // 배치 UI 그리드 디버그 리사이즈 전용(FormationGridDebugView 연동, FormationGridEditor.
        // HandleDebugApply) - 뷰의 열/행 수만 바꾸고 이 메서드로 데이터 모델(FormationLayout)을
        // 함께 재정렬하지 않으면 새로 넓어진 칸에 배치가 조용히 실패한다(실전 확인). Core/Debug/Formation
        // 폴더를 지울 때는 이 메서드와 구현부의 #if UNITY_EDITOR 블록도 함께 지운다.
        void ResizeGrid(int columns, int rows);
#endif
    }
}
