using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// Field(상행 중 이동뷰/전투뷰)에서 쓰는 배치(Formation) UI 조율자 - Hub와 달리 Apply 버튼 없이
    /// 모든 행동이 즉시 반영된다(Docs/기획/20번 §3.1). 실제 반영은 IFormationRepository에 곧바로
    /// 쓰지 않고 IFieldFormationActivityRepository에 배치/이동 활동을 등록하는 것으로 대신한다 -
    /// 그 저장소가 시간이 지나 활동을 완료 처리할 때 비로소 FormationLayout에 반영된다(설계 25번
    /// §3.2). 그리드/팔레트 렌더링과 드래그 이벤트 중계는 공용 로직(FormationGridEditor)에 위임한다.
    /// </summary>
    public class FieldFormationPanel : MonoBehaviour, IUIPanel, IFormationEditingHandler
    {
        [SerializeField] private FormationUnitIconView dragGhostPrefab;

        public string PanelId => UIPanelIds.Formation;

        private FormationGridEditor gridEditor;
        private Button closeButton;

        private ICaravanRosterProvider rosterProvider;
        private IFormationRepository formationRepository;
        private IFieldFormationActivityRepository activityRepository;
        private IUIManager uiManager;

        public void RegisterFieldFormationUI(ICaravanRosterProvider rosterProvider, IFormationRepository formationRepository, IUnitConditionRepository conditionRepository, IFieldFormationActivityRepository activityRepository, IUIManager uiManager, string sceneName)
        {
            this.rosterProvider = rosterProvider;
            this.formationRepository = formationRepository;
            this.activityRepository = activityRepository;
            this.uiManager = uiManager;

            var contentScene = SceneManager.GetSceneByName(sceneName);
            if (!contentScene.IsValid())
            {
                Debug.LogWarning($"'{sceneName}' 씬을 찾을 수 없어 Formation UI를 등록하지 못했다.");
                return;
            }

            SceneUIRoot sceneUIRoot = null;
            foreach (var rootObject in contentScene.GetRootGameObjects())
            {
                sceneUIRoot = rootObject.GetComponentInChildren<SceneUIRoot>(true);
                if (sceneUIRoot != null)
                {
                    break;
                }
            }

            if (sceneUIRoot == null)
            {
                Debug.LogWarning($"'{sceneName}' 씬에서 {nameof(SceneUIRoot)}를 찾을 수 없다.");
                return;
            }

            gridEditor = new FormationGridEditor(this);
            if (!gridEditor.TryBind(sceneUIRoot, dragGhostPrefab))
            {
                return;
            }

            // Field 배치 UI에는 Apply 버튼이 없다(즉시 반영) - Close 버튼만 찾는다.
            if (!sceneUIRoot.TryGetElement<Button>(FormationUIElementIds.CloseButton, out closeButton))
            {
                Debug.LogWarning($"Formation UI에서 '{FormationUIElementIds.CloseButton}' 요소를 찾을 수 없다.");
                return;
            }

            gridEditor.SetSources(rosterProvider, conditionRepository);

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => uiManager.Close(PanelId));

            // 백그라운드에서 활동이 완료/취소될 때마다(패널이 열려 있는 동안) 화면을 다시 그린다.
            if (activityRepository != null)
            {
                activityRepository.OnActivityCompleted -= HandleActivityChanged;
                activityRepository.OnActivityCompleted += HandleActivityChanged;
                activityRepository.OnActivityCancelled -= HandleActivityCancelled;
                activityRepository.OnActivityCancelled += HandleActivityCancelled;
            }
        }

        public void Open() => gridEditor.Open();
        public void Close() => gridEditor?.Close();

        // 진행 중인 활동은 매 프레임 잔여시간이 줄어들므로(설계 25번 §5.1), 패널이 열려 있는 동안
        // 매 프레임 오버레이/경로선을 다시 그린다 - RequestRefresh(이벤트 기반)와 별개 경로다.
        private void Update() => gridEditor?.TickActivityOverlays();

        public IReadOnlyList<FormationActivity> GetActiveActivities() => activityRepository?.ActiveActivities ?? Array.Empty<FormationActivity>();

#if UNITY_EDITOR
        public void ResizeGrid(int columns, int rows)
        {
            if (formationRepository == null) return;

            var layout = formationRepository.TryLoadCurrent(out var current)
                ? current.Resize(columns, rows)
                : new FormationLayout(columns, rows);
            formationRepository.Apply(layout);
        }
#endif

        private void HandleActivityChanged(FormationActivity activity) => gridEditor?.RequestRefresh();
        private void HandleActivityCancelled(string unitId) => gridEditor?.RequestRefresh();

        // IFormationEditingHandler 구현 - 전부 실시간 반영(로컬 사본 없음).
        public FormationLayout GetDisplayLayout()
        {
            if (formationRepository != null && formationRepository.TryLoadCurrent(out var layout))
            {
                return layout;
            }
            return new FormationLayout(gridEditor.GridColumnCount, gridEditor.GridRowCount);
        }

        public bool IsUnitReserved(string unitId) => activityRepository != null && activityRepository.IsUnitBusy(unitId);

        public void HandlePaletteDrop(IFormationUnit unit, int targetSlotIndex)
        {
            if (activityRepository == null) return;

            var duration = unit.Kind switch
            {
                FormationUnitKind.Wagon => FormationTiming.WagonAddSeconds,
                FormationUnitKind.Facility => FormationTiming.FacilityAddSeconds,
                _ => FormationTiming.CharacterAddSeconds,
            };
            activityRepository.BeginAdd(unit.Id, targetSlotIndex, duration);
        }

        public void HandleGridMove(string unitId, int originSlotIndex, int targetSlotIndex)
        {
            if (activityRepository == null || formationRepository == null || !formationRepository.TryLoadCurrent(out var layout)) return;

            // 이미 이동 중인 유닛을 다시 드래그하면(재조정) 기존 활동을 취소하고 새로 시작한다 -
            // 그대로 두면 같은 유닛에 활동이 두 개 겹쳐 상태가 어긋난다(설계 25번 §3.2 안전장치).
            if (activityRepository.IsUnitBusy(unitId))
            {
                activityRepository.Cancel(unitId);
            }

            // 목표 슬롯에 이미 다른 유닛이 있으면 그 유닛도 반대 방향(내 출발 슬롯)으로 맞바꾸어
            // 이동시킨다 - 그냥 덮어쓰면 원래 있던 유닛이 레이아웃에서 통째로 사라지던 버그(실전
            // 검증 2026-09-07)의 정정. 그 유닛이 이미 이동 중이었다면 먼저 취소하고 새로 시작한다.
            var targetUnitId = layout.GetUnitId(targetSlotIndex);
            if (!string.IsNullOrEmpty(targetUnitId) && targetUnitId != unitId)
            {
                if (activityRepository.IsUnitBusy(targetUnitId))
                {
                    activityRepository.Cancel(targetUnitId);
                }
                StartMove(layout, targetUnitId, targetSlotIndex, originSlotIndex);
            }

            StartMove(layout, unitId, originSlotIndex, targetSlotIndex);
        }

        private void StartMove(FormationLayout layout, string unitId, int originSlotIndex, int targetSlotIndex)
        {
            var blocked = CollectBlockedSlots(layout, excludeUnitId: unitId);
            var path = FormationPathFinder.FindPath(originSlotIndex, targetSlotIndex, layout.ColumnCount, layout.RowCount, blocked);
            var duration = Mathf.Max(1, path.Count - 1) * FormationTiming.MoveSecondsPerSlot;
            activityRepository.BeginMove(unitId, originSlotIndex, targetSlotIndex, path, duration);
        }

        public void HandleRemove(string unitId, int slotIndex)
        {
            if (activityRepository != null && activityRepository.IsUnitBusy(unitId))
            {
                // 진행 중(배치/이동) 제거 - 즉시 취소, 로스터 복귀(기획 20번 §3.4).
                activityRepository.Cancel(unitId);
                return;
            }

            if (formationRepository == null || !formationRepository.TryLoadCurrent(out var layout)) return;

            var rosterUnit = rosterProvider?.GetRoster().FirstOrDefault(u => u.Id == unitId);
            if (rosterUnit != null && rosterUnit.Kind == FormationUnitKind.Wagon && WouldViolateMinimumCount(rosterUnit, layout))
            {
                return; // 마차 최소 1개 유지(기획 20번 §3.4, 2026-09-07 사용자 확정으로 시설은 제외) - 조용히 무시, 슬롯은 그대로 유지.
            }

            layout.Clear(slotIndex);
            formationRepository.Apply(layout);
        }

        // 배치 그리드 위에서 점유된 슬롯 + 진행 중인 다른 활동의 목표 슬롯을 합쳐 "막힌 칸"으로
        // 본다(설계 25번 §4). excludeUnitId는 지금 이동을 시작하는 유닛 자신의 출발 슬롯을 막힌
        // 것으로 잘못 취급하지 않기 위한 제외 대상이다.
        private HashSet<int> CollectBlockedSlots(FormationLayout layout, string excludeUnitId)
        {
            var blocked = new HashSet<int>();
            for (var i = 0; i < layout.SlotCount; i++)
            {
                var id = layout.GetUnitId(i);
                if (!string.IsNullOrEmpty(id) && id != excludeUnitId)
                {
                    blocked.Add(i);
                }
            }

            if (activityRepository != null)
            {
                foreach (var activity in activityRepository.ActiveActivities)
                {
                    if (activity.UnitId != excludeUnitId)
                    {
                        blocked.Add(activity.TargetSlotIndex);
                    }
                }
            }

            return blocked;
        }

        // 마차는 최소 1개가 대열(배치 완료된 슬롯)에 남아 있어야 한다(기획 20번 §3.4, 2026-09-07
        // 사용자 확정으로 시설은 제외 - 호출부(HandleRemove)가 Wagon일 때만 호출한다). 이 유닛을
        // 제거했을 때 같은 종류(Kind)가 0개가 되면 위반 - Kind로 일반화해둔 이유는 향후 다른 종류에도
        // 같은 최소 유지 규칙이 필요해지면 호출부만 바꿔 재사용하기 위함.
        private bool WouldViolateMinimumCount(IFormationUnit unitToRemove, FormationLayout layout)
        {
            var remainingOfKind = 0;
            for (var i = 0; i < layout.SlotCount; i++)
            {
                var id = layout.GetUnitId(i);
                if (string.IsNullOrEmpty(id) || id == unitToRemove.Id) continue;

                var placedUnit = rosterProvider?.GetRoster().FirstOrDefault(u => u.Id == id);
                if (placedUnit != null && placedUnit.Kind == unitToRemove.Kind)
                {
                    remainingOfKind++;
                }
            }
            return remainingOfKind < 1;
        }
    }
}
