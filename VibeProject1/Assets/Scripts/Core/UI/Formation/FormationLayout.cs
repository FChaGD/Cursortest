namespace Game.Core
{
    /// <summary>
    /// 슬롯 인덱스별로 배치된 유닛 Id와 그리드 모양(열/행 수)을 함께 보관한다. 빈 슬롯은 null.
    /// 그리드 모양을 데이터에 포함시킨 이유: 배치 UI 화면 요소는 콘텐츠 씬(Hub/Field 등)마다 별도
    /// 인스턴스라 각자 다른 열/행 수(FormationGridView의 씬별 값)를 가질 수 있었다 - 한쪽에서 바꾼
    /// 크기가 다른 쪽에 반영되지 않아 배치가 잘려 보이는 문제가 있었다. 이제 저장된 FormationLayout이
    /// 그리드 모양의 기준이 되고, FormationPanel.Open()이 이 값으로 그리드를 다시 맞춘다.
    /// </summary>
    public class FormationLayout
    {
        // 배치가 아직 없을 때(Hub 정비창을 한 번도 저장하지 않은 상태 등) 그리드 열/행 수의 단일
        // 출처 - FormationGridView의 인스펙터 기본값과 LiveBattleSimulationRule/
        // InMemoryFieldFormationActivityRepository의 배치 없음 폴백이 이 값을 공유한다. 따로 들고
        // 있으면 하나만 바뀌었을 때 조용히 어긋난다(BattleFieldGeometry와 같은 이유).
        public const int DefaultColumnCount = 8;
        public const int DefaultRowCount = 2;

        private readonly string[] slotUnitIds;

        public int ColumnCount { get; }
        public int RowCount { get; }
        public int SlotCount => slotUnitIds.Length;

        public FormationLayout(int columnCount, int rowCount)
        {
            ColumnCount = columnCount;
            RowCount = rowCount;
            slotUnitIds = new string[columnCount * rowCount];
        }

        private FormationLayout(int columnCount, int rowCount, string[] slotUnitIds)
        {
            ColumnCount = columnCount;
            RowCount = rowCount;
            this.slotUnitIds = slotUnitIds;
        }

        public string GetUnitId(int slotIndex) => slotUnitIds[slotIndex];

        public void SetUnitId(int slotIndex, string unitId) => slotUnitIds[slotIndex] = unitId;

        public void Clear(int slotIndex) => slotUnitIds[slotIndex] = null;

        public void Swap(int slotIndexA, int slotIndexB)
        {
            (slotUnitIds[slotIndexA], slotUnitIds[slotIndexB]) = (slotUnitIds[slotIndexB], slotUnitIds[slotIndexA]);
        }

        public FormationLayout Clone() => new(ColumnCount, RowCount, (string[])slotUnitIds.Clone());

        // 그리드 열/행 수를 바꾼 새 레이아웃을 만든다 - 기존 배치는 같은 (row, col) 위치 기준으로
        // 옮기고, 줄어든 영역 밖으로 밀려나는 배치는 버린다(배치 UI 디버그 그리드 리사이즈 전용). 뷰만
        // 리사이즈하고 데이터는 이 메서드로 맞추지 않으면, 새로 넓어진 칸의 슬롯 인덱스가 옛 배열
        // 범위를 벗어나 배치가 조용히 실패하는 버그가 있었다(실전 확인, 2026-09-06).
        public FormationLayout Resize(int newColumnCount, int newRowCount)
        {
            var resized = new FormationLayout(newColumnCount, newRowCount);
            var rowsToCopy = RowCount < newRowCount ? RowCount : newRowCount;
            var columnsToCopy = ColumnCount < newColumnCount ? ColumnCount : newColumnCount;

            for (var row = 0; row < rowsToCopy; row++)
            {
                for (var col = 0; col < columnsToCopy; col++)
                {
                    var unitId = slotUnitIds[row * ColumnCount + col];
                    if (!string.IsNullOrEmpty(unitId))
                    {
                        resized.SetUnitId(row * newColumnCount + col, unitId);
                    }
                }
            }

            return resized;
        }
    }
}
