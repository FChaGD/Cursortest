using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// Hub에서 쓰는 배치(Formation) UI 조율자 - 로컬 편집 상태(currentLayout)를 들고 있다가 "적용"
    /// 버튼을 눌렀을 때만 IFormationRepository에 반영한다. 적용 없이 닫으면 세션 상태를 그냥 버린다 -
    /// 다음에 열 때 항상 repository에서 다시 불러오므로 별도의 되돌리기 로직 없이 "마지막 적용
    /// 상태로 복귀"가 성립한다(기존 FormationPanel 동작 그대로, Docs/설계/25번 §2.3 - Field는 이
    /// 모델을 쓰지 않고 FieldFormationPanel이 즉시 반영 모델로 대신 담당한다).
    /// 그리드/팔레트 렌더링과 드래그 이벤트 중계는 공용 로직(FormationGridEditor)에 위임하고, 이
    /// 클래스는 "드롭 시 무엇을 반영할지"(IFormationEditingHandler)와 적용 버튼만 담당한다.
    /// </summary>
    public class HubFormationPanel : MonoBehaviour, IUIPanel, IFormationEditingHandler
    {
        [SerializeField] private FormationUnitIconView dragGhostPrefab;

        public string PanelId => UIPanelIds.Formation;

        private FormationGridEditor gridEditor;
        private Button applyButton;
        private Button closeButton;

        private IFormationRepository repository;
        private IUIManager uiManager;

        private FormationLayout currentLayout;

        public void RegisterFormationUI(ICaravanRosterProvider rosterProvider, IFormationRepository repository, IUnitConditionRepository conditionRepository, IUIManager uiManager, string sceneName)
        {
            this.repository = repository;
            this.uiManager = uiManager;

            // 배치 UI 화면 요소는 콘텐츠 씬(Hub 등) 안에 있어 그 씬이 언로드되면 함께 파괴된다.
            // 다른 콘텐츠 씬이 로드될 때마다 이 메서드가 다시 호출되어 그 씬의 사본으로 재바인딩한다.
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

            if (!sceneUIRoot.TryGetElement<Button>(FormationUIElementIds.ApplyButton, out applyButton))
            {
                Debug.LogWarning($"Formation UI에서 '{FormationUIElementIds.ApplyButton}' 요소를 찾을 수 없다.");
                return;
            }

            if (!sceneUIRoot.TryGetElement<Button>(FormationUIElementIds.CloseButton, out closeButton))
            {
                Debug.LogWarning($"Formation UI에서 '{FormationUIElementIds.CloseButton}' 요소를 찾을 수 없다.");
                return;
            }

            gridEditor.SetSources(rosterProvider, conditionRepository);

            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(HandleApply);

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => uiManager.Close(PanelId));
        }

        public void Open()
        {
            currentLayout = BuildInitialLayout();
            gridEditor.Open();
        }

        // 순수 "숨기기"만 한다. 상행 준비 UI 등으로 되돌아가는 네비게이션은 UIManager.Close(PanelId)의
        // 책임이므로 버튼 등 외부에서 패널을 닫을 때는 이 메서드를 직접 호출하지 말고 반드시
        // uiManager.Close(PanelId)를 거칠 것.
        public void Close() => gridEditor?.Close();

        private FormationLayout BuildInitialLayout()
        {
            if (repository != null && repository.TryLoadCurrent(out var saved))
            {
                return saved.Clone();
            }

            return new FormationLayout(gridEditor.GridColumnCount, gridEditor.GridRowCount);
        }

        private void HandleApply()
        {
            if (repository == null)
            {
                Debug.LogWarning($"{nameof(IFormationRepository)}가 연결되어 있지 않아 배치를 상행에 적용하지 못했다.");
                return;
            }

            repository.Apply(currentLayout.Clone());
        }

        // IFormationEditingHandler 구현 - 전부 로컬 currentLayout만 건드린다(Apply 전까지 미반영).
        // "배경 진행 활동" 관련 멤버(IsUnitReserved/HandleRedirectMove/GetActiveActivities)는 Hub에
        // 개념 자체가 없어 IFormationActivityHandler로 분리됐고, Hub는 그 인터페이스를 구현하지
        // 않는다(ISP, Docs/Refactor/2026-09-08_공통.md §6.3 수정 G).
        public FormationLayout GetDisplayLayout() => currentLayout;

        public void HandlePaletteDrop(IFormationUnit unit, int targetSlotIndex)
        {
            // 기존 점유 유닛은 슬롯 표시에서만 해제된다(상행 관리 데이터 삭제 아님).
            currentLayout.SetUnitId(targetSlotIndex, unit.Id);
        }

        public void HandleGridMove(string unitId, int originSlotIndex, int targetSlotIndex)
        {
            var targetUnitId = currentLayout.GetUnitId(targetSlotIndex);
            if (string.IsNullOrEmpty(targetUnitId))
            {
                currentLayout.SetUnitId(targetSlotIndex, unitId);
                currentLayout.Clear(originSlotIndex);
            }
            else
            {
                currentLayout.Swap(originSlotIndex, targetSlotIndex);
            }
        }

        public void HandleRemove(string unitId, int slotIndex) => currentLayout.Clear(slotIndex);

#if UNITY_EDITOR
        public void ResizeGrid(int columns, int rows) => currentLayout = currentLayout.Resize(columns, rows);
#endif
    }
}
