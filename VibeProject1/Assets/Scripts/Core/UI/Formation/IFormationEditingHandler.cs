namespace Game.Core
{
    /// <summary>
    /// FormationGridEditor가 드롭/드래그/제거 이벤트를 감지한 뒤 "실제로 무엇을 반영할지"를 위임하는
    /// 정책 인터페이스 - Hub(로컬 편집+Apply 버튼)와 Field(즉시 반영+배치 시간)가 이 인터페이스의
    /// 구현체만 다르다(Docs/기획/20번 §3.1, 설계 25번 §2.2). "배경 진행 활동"(배치/이동 중) 관련
    /// 멤버는 Field에만 있는 개념이라 IFormationActivityHandler로 분리했다(ISP,
    /// Docs/Refactor/2026-09-08_공통.md §6.3 수정 G) - Hub(HubFormationPanel)는 이 인터페이스만
    /// 구현하고 IFormationActivityHandler는 구현하지 않는다.
    /// </summary>
    internal interface IFormationEditingHandler
    {
        // FormationGridEditor가 렌더링에 쓸 "현재 실제 반영 상태" 스냅샷을 요청한다 - Hub는 로컬
        // 편집 중인 사본을, Field는 IFormationRepository의 현재 값을 그대로 돌려준다.
        FormationLayout GetDisplayLayout();

        // 팔레트에서 빈/점유 슬롯으로 드롭(신규 배치 시도).
        void HandlePaletteDrop(IFormationUnit unit, int targetSlotIndex);

        // 그리드 내 슬롯→슬롯 이동(드래그).
        void HandleGridMove(string unitId, int originSlotIndex, int targetSlotIndex);

        // 그리드 밖으로 드래그해 배치 취소, 혹은 명시적 제거.
        void HandleRemove(string unitId, int slotIndex);

#if UNITY_EDITOR
        // 배치 UI 그리드 디버그 리사이즈 전용(FormationGridDebugView 연동, FormationGridEditor.
        // HandleDebugApply) - 뷰의 열/행 수만 바꾸고 이 메서드로 데이터 모델(FormationLayout)을
        // 함께 재정렬하지 않으면 새로 넓어진 칸에 배치가 조용히 실패한다(실전 확인). Core/Debug/Formation
        // 폴더를 지울 때는 이 메서드와 구현부의 #if UNITY_EDITOR 블록도 함께 지운다.
        void ResizeGrid(int columns, int rows);
#endif
    }
}
