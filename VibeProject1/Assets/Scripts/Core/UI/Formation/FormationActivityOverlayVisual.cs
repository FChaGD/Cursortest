namespace Game.Core
{
    /// <summary>
    /// 정비창 그리드에서 슬롯 하나에 표시할 배치/이동 진행 오버레이 정보(Docs/기획/20번 §3.2/§3.3,
    /// 설계 25번 §5.1) - FormationGridView.SetActivityOverlays가 이 목록을 받아
    /// FormationActivityOverlayView 풀을 슬롯 좌표 위에 배치한다.
    /// </summary>
    public readonly struct FormationActivityOverlayVisual
    {
        public int SlotIndex { get; }
        public FormationActivity Activity { get; }
        public IFormationUnit Unit { get; }

        // 이 오버레이가 "이동 중인 활동의 도착 마크"인지 - 참이면 드래그해 목적지를 바꿀 수 있다
        // (기획 21번, 설계 26번 §5.1). 출발 마크(Origin)나 배치(Adding) 마크는 대상이 아니다.
        public bool IsRedirectableTarget => Activity.Kind == FormationActivityKind.Moving && SlotIndex == Activity.TargetSlotIndex;

        // 이 오버레이가 "이동 중인 활동의 출발 마크"인지 - 참이면 잔여시간 텍스트를 표시하지 않는다
        // (2026-09-07 사용자 확정: 같은 활동의 시간이 출발/도착 양쪽에 중복 표기되던 것을 도착
        // 한쪽으로만 정리). 배치(Adding) 마크는 애초에 도착 마크 하나뿐이라 항상 표시된다.
        public bool IsMoveOrigin => Activity.Kind == FormationActivityKind.Moving && SlotIndex == Activity.OriginSlotIndex;

        public FormationActivityOverlayVisual(int slotIndex, FormationActivity activity, IFormationUnit unit)
        {
            SlotIndex = slotIndex;
            Activity = activity;
            Unit = unit;
        }
    }
}
