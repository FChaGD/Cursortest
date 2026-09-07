using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using Game.Core.DebugTools;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Core
{
    /// <summary>
    /// 배치 UI의 그리드/팔레트 렌더링과 드래그 이벤트 중계를 전담하는 공용 로직(Docs/설계/25번 §2.2) -
    /// Hub/Field 배치 UI(HubFormationPanel/FieldFormationPanel)가 공유한다. "드롭 시 무엇을 반영할지"
    /// (교체/스왑/타이머 시작 등)는 이 클래스가 판단하지 않고 IFormationEditingHandler에 위임한다 -
    /// 이 클래스는 화면 갱신과 이벤트 중계만 담당한다(SRP).
    /// </summary>
    internal class FormationGridEditor
    {
        private readonly IFormationEditingHandler handler;
        private readonly FormationDragCoordinator dragCoordinator = new();

        private GameObject panelRoot;
        private FormationPaletteView paletteView;
        private FormationGridView gridView;
        private FormationInfoPanelView infoPanelView;
#if UNITY_EDITOR
        // 그리드 크기 디버그 패널 연동 지점 - Core/Debug/Formation 폴더를 지울 때는 이 #if UNITY_EDITOR
        // 블록들도 함께 지운다(DEBUG_FEATURES.md 참고).
        private FormationGridDebugView debugView;
#endif

        private ICaravanRosterProvider rosterProvider;
        private IUnitConditionRepository conditionRepository;

        private FormationLayout displayLayout;
        private readonly Dictionary<string, IFormationUnit> unitsById = new();
        private IReadOnlyList<IFormationUnit> currentRoster = Array.Empty<IFormationUnit>();

        public GameObject PanelRoot => panelRoot;
        // 배치가 아직 하나도 저장된 적 없을 때(repository.TryLoadCurrent 실패) 핸들러가 기본 그리드
        // 모양을 결정하는 데 쓴다 - FormationGridView의 인스펙터 기본값이 단일 출처다.
        public int GridColumnCount => gridView.ColumnCount;
        public int GridRowCount => gridView.RowCount;

        public FormationGridEditor(IFormationEditingHandler handler)
        {
            this.handler = handler;
        }

        public bool TryBind(SceneUIRoot sceneUIRoot, FormationUnitIconView dragGhostPrefab)
        {
            if (!sceneUIRoot.TryGetElement<Transform>(FormationUIElementIds.PanelRoot, out var rootTransform))
            {
                WarnMissing(FormationUIElementIds.PanelRoot);
                return false;
            }
            panelRoot = rootTransform.gameObject;

            if (!sceneUIRoot.TryGetElement<FormationPaletteView>(FormationUIElementIds.PaletteRoot, out paletteView))
            {
                WarnMissing(FormationUIElementIds.PaletteRoot);
                return false;
            }

            if (!sceneUIRoot.TryGetElement<FormationGridView>(FormationUIElementIds.GridRoot, out gridView))
            {
                WarnMissing(FormationUIElementIds.GridRoot);
                return false;
            }

            if (!sceneUIRoot.TryGetElement<FormationInfoPanelView>(FormationUIElementIds.InfoPanelRoot, out infoPanelView))
            {
                WarnMissing(FormationUIElementIds.InfoPanelRoot);
                return false;
            }

#if UNITY_EDITOR
            // 디버그 패널은 보조 기능이라 없어도 나머지 배치 UI는 정상 동작해야 한다 - 없으면 조용히 건너뛴다.
            sceneUIRoot.TryGetElement<FormationGridDebugView>(FormationUIElementIds.DebugPanelRoot, out debugView);
#endif

            var rootCanvas = panelRoot.GetComponentInParent<Canvas>()?.rootCanvas;
            dragCoordinator.Rebind(dragGhostPrefab, rootCanvas);

            panelRoot.SetActive(false);
            return true;
        }

        public void SetSources(ICaravanRosterProvider rosterProvider, IUnitConditionRepository conditionRepository)
        {
            this.rosterProvider = rosterProvider;
            this.conditionRepository = conditionRepository;
        }

        public void Open()
        {
            if (panelRoot == null)
            {
                return;
            }

            RefreshRosterCache();
            displayLayout = handler.GetDisplayLayout();
            gridView.SetGridDimensions(displayLayout.ColumnCount, displayLayout.RowCount);
            gridView.Initialize(HandleSlotDropped, HandleUnitIconClicked, HandleGridIconBeginDrag, HandleIconDrag, HandleIconEndDrag);

            RefreshAllSlots();
            infoPanelView.Clear();
#if UNITY_EDITOR
            debugView?.Initialize(gridView.ColumnCount, gridView.RowCount, gridView.SlotSize, HandleDebugApply);
#endif

            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                return;
            }

            // 드래그 도중 패널이 강제로 닫히면(예: Field 씬에서 인카운터 발생 시) dragGhost가
            // panelRoot가 아니라 rootCanvas 바로 아래 별도로 떠 있어 panelRoot 비활성화와 무관하게
            // 화면에 그대로 남는다 - 패널을 닫을 때는 항상 진행 중인 드래그도 함께 정리한다.
            dragCoordinator.CancelActiveDrag();
            panelRoot.SetActive(false);
        }

        // 핸들러가 백그라운드에서 상태를 바꿨을 때(Field 활동 완료/취소 등, Docs/설계/25번 §3.4) 다시
        // 그리라고 요청한다 - 패널이 닫혀 있으면 아무 것도 하지 않는다(다시 열 때 Open()이 최신
        // 상태를 자연히 반영한다).
        public void RequestRefresh()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            RefreshRosterCache();
            displayLayout = handler.GetDisplayLayout();
            RefreshAllSlots();
        }

        // 진행 중인 배치/이동 활동을 경로선 + 슬롯 오버레이 마크 + 이동 아이콘으로 그린다(설계 25번
        // §5) - 잔여시간이 매 프레임 줄어들므로 FieldFormationPanel.Update()가 매 프레임 호출한다.
        // Hub는 GetActiveActivities()가 항상 빈 목록이라 이 메서드 전체가 자연히 무해하다. 세 호출
        // (SetPathLines→SetActivityOverlays→SetTravelerIcons) 순서가 곧 렌더 순서다(실전 확인:
        // 슬롯 배경 < 경로선 < 오버레이 마크 < 이동 아이콘) - 순서를 바꾸지 말 것.
        public void TickActivityOverlays()
        {
            if (panelRoot == null || !panelRoot.activeSelf || displayLayout == null)
            {
                return;
            }

            var activities = handler.GetActiveActivities();
            var overlays = new List<FormationActivityOverlayVisual>();
            var movePaths = new List<FormationMovePathVisual>();

            foreach (var activity in activities)
            {
                unitsById.TryGetValue(activity.UnitId, out var unit);

                overlays.Add(new FormationActivityOverlayVisual(activity.TargetSlotIndex, activity, unit));

                if (activity.Kind == FormationActivityKind.Moving)
                {
                    overlays.Add(new FormationActivityOverlayVisual(activity.OriginSlotIndex, activity, unit));
                    // 이동 중인 유닛 아이콘이 경로 위를 실시간으로 지나가도록(기획 20번 §3.3) 진행률과
                    // 아이콘을 함께 넘긴다 - 정적인 출발/도착 오버레이(위 overlays)와는 별개다.
                    movePaths.Add(new FormationMovePathVisual(activity.PathSlotIndices, activity.Progress01, unit?.Icon));
                }
            }

            gridView.SetPathLines(movePaths);
            gridView.SetActivityOverlays(overlays);
            gridView.SetTravelerIcons(movePaths);
        }

        private void RefreshRosterCache()
        {
            currentRoster = rosterProvider?.GetRoster() ?? Array.Empty<IFormationUnit>();
            unitsById.Clear();
            foreach (var unit in currentRoster)
            {
                unitsById[unit.Id] = unit;
            }
        }

#if UNITY_EDITOR
        private void HandleDebugApply(int columns, int rows, Vector2 size)
        {
            // 뷰(그리드 타일 수)만 바꾸고 데이터 모델(FormationLayout)을 함께 재정렬하지 않으면, 새로
            // 넓어진 칸의 슬롯 인덱스가 옛 배열 범위를 벗어나 배치가 조용히 실패하는 버그가 있었다
            // (실전 확인, 2026-09-06) - handler.ResizeGrid를 먼저 호출해 데이터부터 맞춘다.
            handler.ResizeGrid(columns, rows);
            displayLayout = handler.GetDisplayLayout();

            // 크기를 먼저 반영해야 열/행 변경으로 새로 생성되는 타일도 같은 크기로 만들어진다.
            gridView.SetSlotSize(size);
            gridView.SetGridDimensions(columns, rows);
            RefreshAllSlots();
        }
#endif

        private void HandleUnitIconClicked(IFormationUnit unit) => infoPanelView.Show(unit);

        // 카테고리 행 클릭 시 같은 카테고리 개체는 전부 스탯/외형이 동일하므로(기획 11번 §3), 대표로
        // 로스터에서 그 카테고리의 첫 개체 정보를 보여준다.
        private void HandlePaletteRowClicked(FormationCategoryKey key)
        {
            foreach (var unit in currentRoster)
            {
                if (FormationCategoryKey.Of(unit).Equals(key))
                {
                    infoPanelView.Show(unit);
                    return;
                }
            }
        }

        // 카테고리 행은 구체적인 개체를 모른다(설계 16번) - 여기서 그 카테고리의 가용(미배치+생존+
        // 미예약) 개체 하나를 골라 드래그 코디네이터에 그대로 넘긴다.
        private void HandlePaletteRowBeginDrag(FormationCategoryKey key, PointerEventData eventData)
        {
            var available = FindAvailableUnit(key);
            if (available == null)
            {
                return; // 방어적 - 잔여 0이면 행 자체가 비활성화라 정상 흐름에선 호출되지 않는다.
            }

            dragCoordinator.BeginFromPalette(available, eventData);
        }

        private IFormationUnit FindAvailableUnit(FormationCategoryKey key)
        {
            var placedIds = CollectPlacedIds();
            foreach (var unit in currentRoster)
            {
                if (!FormationCategoryKey.Of(unit).Equals(key)) continue;
                if (placedIds.Contains(unit.Id)) continue;
                if (handler.IsUnitReserved(unit.Id)) continue;
                if (conditionRepository != null && unit is IMercenaryUnit && conditionRepository.IsDead(unit.Id)) continue;

                return unit;
            }
            return null;
        }

        private HashSet<string> CollectPlacedIds()
        {
            var placedIds = new HashSet<string>();
            for (var i = 0; i < displayLayout.SlotCount; i++)
            {
                var id = displayLayout.GetUnitId(i);
                if (!string.IsNullOrEmpty(id))
                {
                    placedIds.Add(id);
                }
            }
            return placedIds;
        }

        private List<FormationCategorySummary> BuildCategorySummaries()
        {
            var placedIds = CollectPlacedIds();
            var order = new List<FormationCategoryKey>();
            var totals = new Dictionary<FormationCategoryKey, int>();
            var availables = new Dictionary<FormationCategoryKey, int>();
            var names = new Dictionary<FormationCategoryKey, string>();
            var icons = new Dictionary<FormationCategoryKey, Sprite>();

            foreach (var unit in currentRoster)
            {
                var key = FormationCategoryKey.Of(unit);
                if (!totals.ContainsKey(key))
                {
                    order.Add(key);
                    totals[key] = 0;
                    availables[key] = 0;
                    names[key] = unit.DisplayName;
                    icons[key] = unit.Icon;
                }

                totals[key]++;

                var isDead = conditionRepository != null && unit is IMercenaryUnit && conditionRepository.IsDead(unit.Id);
                // Field에서는 아직 배치가 완료되지 않았어도(레이아웃엔 안 나타남) 이미 다른 진행 중
                // 활동에 쓰이고 있으면 예약된 것으로 본다(handler.IsUnitReserved, 설계 25번 §3.3).
                var isReserved = placedIds.Contains(unit.Id) || handler.IsUnitReserved(unit.Id);
                if (!isDead && !isReserved)
                {
                    availables[key]++;
                }
            }

            var summaries = new List<FormationCategorySummary>(order.Count);
            foreach (var key in order)
            {
                summaries.Add(new FormationCategorySummary(key, names[key], icons[key], totals[key], availables[key]));
            }
            return summaries;
        }

        private void RefreshPalette()
        {
            paletteView.SetCategories(BuildCategorySummaries(), HandlePaletteRowClicked, HandlePaletteRowBeginDrag, HandleIconDrag, HandleIconEndDrag);
        }

        private void HandleGridIconBeginDrag(int originSlotIndex, FormationUnitIconView icon, PointerEventData eventData)
        {
            var unitId = displayLayout.GetUnitId(originSlotIndex);
            if (string.IsNullOrEmpty(unitId) || !unitsById.TryGetValue(unitId, out var unit))
            {
                return;
            }

            dragCoordinator.BeginFromGrid(unit, originSlotIndex, eventData);
        }

        private void HandleIconDrag(PointerEventData eventData) => dragCoordinator.UpdateGhostPosition(eventData);

        private void HandleSlotDropped(int targetSlotIndex)
        {
            var draggedUnit = dragCoordinator.DraggedUnit;
            if (draggedUnit == null)
            {
                return;
            }

            dragCoordinator.MarkDropHandled();

            if (dragCoordinator.DraggedFromSlot is { } sourceIndex)
            {
                if (sourceIndex == targetSlotIndex)
                {
                    return;
                }
                handler.HandleGridMove(draggedUnit.Id, sourceIndex, targetSlotIndex);
            }
            else
            {
                handler.HandlePaletteDrop(draggedUnit, targetSlotIndex);
            }

            // sourceIndex(원본 슬롯)는 아직 드래그 중인 아이콘이 점유하고 있으므로 여기서 갱신하지
            // 않는다 - 실제 갱신은 드래그가 끝나는 HandleIconEndDrag에서 처리한다(기존 FormationPanel
            // 동작 그대로).
            displayLayout = handler.GetDisplayLayout();
            RefreshSlot(targetSlotIndex);
        }

        private void HandleIconEndDrag(PointerEventData eventData)
        {
            var (unit, fromSlot, wasHandled) = dragCoordinator.EndDrag();

            if (!wasHandled && fromSlot.HasValue && unit != null)
            {
                // 타일/팔레트가 아닌 곳에 드롭 = 배치 취소/제거(기획 20번 §3.4).
                handler.HandleRemove(unit.Id, fromSlot.Value);
            }

            // 원본 슬롯의 아이콘 갱신은 반드시 여기(드래그가 실제로 끝나는 시점)에서 한다 - OnDrop
            // 시점(HandleSlotDropped)에는 이 아이콘이 아직 드래그 중인 오브젝트라, 거기서 갱신하면
            // 뒤이은 OnEndDrag 호출이 씹혀 드래그 상태가 초기화되지 않는 문제가 있었다.
            displayLayout = handler.GetDisplayLayout();
            if (fromSlot.HasValue)
            {
                RefreshSlot(fromSlot.Value);
            }
            RefreshPalette();
        }

        private void RefreshSlot(int index)
        {
            var unitId = displayLayout.GetUnitId(index);
            IFormationUnit unit = null;
            if (!string.IsNullOrEmpty(unitId))
            {
                unitsById.TryGetValue(unitId, out unit);
            }

            gridView.RenderSlot(index, unit);
        }

        private void RefreshAllSlots()
        {
            for (var i = 0; i < displayLayout.SlotCount; i++)
            {
                RefreshSlot(i);
            }
            // 슬롯 배치가 통째로 바뀌었을 수 있으므로(Open/디버그 리사이즈) 팔레트 잔여수도 다시
            // 계산한다 - 카테고리는 최대 5개뿐이라 매번 다시 그려도 비용이 미미하다(설계 16번 §3).
            RefreshPalette();
        }

        private static void WarnMissing(string id)
        {
            Debug.LogWarning($"Formation UI에서 '{id}' 요소를 찾을 수 없다. {nameof(UIElementMarker)}가 부착되어 있는지 확인하라.");
        }
    }
}
