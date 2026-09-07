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

        public FormationActivityOverlayVisual(int slotIndex, FormationActivity activity, IFormationUnit unit)
        {
            SlotIndex = slotIndex;
            Activity = activity;
            Unit = unit;
        }
    }
}
