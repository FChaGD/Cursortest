using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Core
{
    /// <summary>
    /// 배치 UI의 드래그 상태 추적과 드래그 고스트 표시만 전담한다(TripMapInteractionCoordinator와
    /// 같은 이유로 FormationPanel에서 분리 - SRP). 드롭 시 실제로 무엇을 반영할지(교체/스왑/취소
    /// 판단)는 더 이상 여기서 하지 않는다 - Hub/Field 배치 UI가 그 판단을 각자 다르게 내려야 해서
    /// (Docs/설계/25번 §2.2) 호출자(FormationGridEditor)에게 위임했다. 이 클래스는 "지금 무엇을
    /// 드래그 중인지"와 고스트 아이콘 표시만 안다.
    /// </summary>
    internal class FormationDragCoordinator
    {
        private FormationUnitIconView dragGhostPrefab;
        private Canvas rootCanvas;

        private FormationUnitIconView dragGhost;

        public IFormationUnit DraggedUnit { get; private set; }
        public int? DraggedFromSlot { get; private set; }
        public bool DropHandled { get; private set; }

        public void Rebind(FormationUnitIconView dragGhostPrefab, Canvas rootCanvas)
        {
            this.dragGhostPrefab = dragGhostPrefab;
            this.rootCanvas = rootCanvas;
        }

        public void CancelActiveDrag()
        {
            if (dragGhost != null)
            {
                dragGhost.gameObject.SetActive(false);
            }

            DraggedUnit = null;
            DraggedFromSlot = null;
            DropHandled = false;
        }

        public void BeginFromPalette(IFormationUnit unit, PointerEventData eventData) => BeginDrag(unit, null, eventData);

        // 슬롯 인덱스가 가리키는 유닛이 무엇인지는 더 이상 이 클래스가 조회하지 않는다 - 호출자가
        // 이미 해석한 IFormationUnit을 그대로 넘긴다(레이아웃 조회 책임을 호출자에게 넘김).
        public void BeginFromGrid(IFormationUnit unit, int originSlotIndex, PointerEventData eventData) => BeginDrag(unit, originSlotIndex, eventData);

        private void BeginDrag(IFormationUnit unit, int? originSlotIndex, PointerEventData eventData)
        {
            DraggedUnit = unit;
            DraggedFromSlot = originSlotIndex;
            DropHandled = false;

            if (dragGhost == null && dragGhostPrefab != null && rootCanvas != null)
            {
                dragGhost = UnityEngine.Object.Instantiate(dragGhostPrefab, rootCanvas.transform);
                dragGhost.SetRaycastTarget(false);
            }

            if (dragGhost == null)
            {
                return;
            }

            dragGhost.Bind(unit);
            dragGhost.gameObject.SetActive(true);
            dragGhost.transform.SetAsLastSibling();
            UpdateGhostPosition(eventData);
        }

        public void UpdateGhostPosition(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                dragGhost.transform.position = eventData.position;
            }
        }

        // 드롭이 실제로 처리됐음을 기록한다 - EndDrag가 "취소(빈 곳에 드롭)"인지 구분하는 데 쓴다.
        public void MarkDropHandled() => DropHandled = true;

        // 드래그가 끝날 때(취소/이동/팔레트 배치 전부 포함) 고스트를 숨기고 상태를 리셋한다. 리셋
        // 직전 상태를 스냅샷으로 반환하므로, 호출자는 이 메서드 호출 전에 값을 따로 읽어둘 필요가 없다.
        public (IFormationUnit unit, int? fromSlot, bool wasHandled) EndDrag()
        {
            if (dragGhost != null)
            {
                dragGhost.gameObject.SetActive(false);
            }

            var snapshot = (DraggedUnit, DraggedFromSlot, DropHandled);
            DraggedUnit = null;
            DraggedFromSlot = null;
            DropHandled = false;
            return snapshot;
        }
    }
}
