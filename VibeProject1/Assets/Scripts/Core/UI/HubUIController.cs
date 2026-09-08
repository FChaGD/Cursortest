using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UIManager 산하 컴포넌트. Hub 씬이 로드된 시점에 Hub 씬의 SceneUIRoot를 찾아
    /// 버튼 클릭 동작과 배경 이미지를 연결한다. "상행 준비"/"배치" 버튼은 씬 전환 커튼이 완전히
    /// 걷히기 전까지는 상호작용을 막는다(사용자 확정) - ISceneRevealSignal 참고.
    /// </summary>
    public class HubUIController : MonoBehaviour, IHubUIController
    {
        [SerializeField] private Sprite backgroundSprite;

        /// <summary>
        /// Hub↔Field 씬 전환 연출(SceneTransitionEffectController)이 슬라이드시킬 대상 - Background+
        /// 버튼 전체를 감싸는 루트다. RegisterHubUI가 끝나야 값이 채워진다(Docs/설계/10-2026-08-26-씬전환_연출_아키텍처.md §8).
        /// </summary>
        public RectTransform ContentRoot { get; private set; }

        private Button departureButton;
        private Button formationButton;
        private Button tacticsButton;
        private ISceneRevealSignal sceneRevealSignal;
        private IUIManager uiManager;

        public void RegisterHubUI(SceneUIRoot sceneUIRoot, IUIManager uiManager, ISceneRevealSignal sceneRevealSignal)
        {
            this.uiManager = uiManager;
            this.sceneRevealSignal = sceneRevealSignal;

            if (!sceneUIRoot.TryGetElement<RectTransform>(HubUIElementIds.ContentRoot, out var contentRoot))
            {
                Debug.LogWarning($"Hub UI에서 '{HubUIElementIds.ContentRoot}' 요소를 찾을 수 없다. {nameof(UIElementMarker)}가 부착되어 있는지 확인하라.");
            }
            ContentRoot = contentRoot;

            departureButton = BindButton(sceneUIRoot, HubUIElementIds.DepartureButton, () => uiManager.Open(UIPanelIds.Trip));
            formationButton = BindButton(sceneUIRoot, HubUIElementIds.FormationButton, () => uiManager.Open(UIPanelIds.Formation));
            tacticsButton = BindButton(sceneUIRoot, HubUIElementIds.TacticsButton, () => uiManager.Open(UIPanelIds.Tactics));

            // 화면이 완전히 드러나기 전까지는 두 버튼 다 비활성 - 전환 없이 로드된 경우(최초 진입 등)엔
            // SceneRevealed가 즉시 발생해 사실상 바로 다시 활성화된다.
            SetTopLevelButtonsInteractable(false);
            sceneRevealSignal.SceneRevealed -= HandleSceneRevealed;
            sceneRevealSignal.SceneRevealed += HandleSceneRevealed;

            // 배치/방향성 지시/상행 준비 패널이 하나라도 열려있는 동안은 이 버튼들을 완전히 숨긴다 -
            // 이전엔 인터랙터블만 안 건드려서, 패널이 열려도 버튼이 그대로 보이고 눌리기까지 했다
            // (사용자 확정, 2026-09-07). 패널끼리 중첩 전환되는 동안(상행 준비→배치→복귀)에는 계속
            // 숨김 상태가 유지되고, 최상위 패널까지 완전히 닫혀야 다시 나타난다(IUIManager.OnAnyPanelOpenChanged 참고).
            uiManager.OnAnyPanelOpenChanged -= HandleAnyPanelOpenChanged;
            uiManager.OnAnyPanelOpenChanged += HandleAnyPanelOpenChanged;

            ApplyBackground(sceneUIRoot);
        }

        private void HandleSceneRevealed(ContentSceneId sceneId)
        {
            if (sceneId != ContentSceneId.Hub)
            {
                return;
            }

            SetTopLevelButtonsInteractable(true);
        }

        private void HandleAnyPanelOpenChanged(bool isAnyPanelOpen) => SetTopLevelButtonsVisible(!isAnyPanelOpen);

        private void SetTopLevelButtonsInteractable(bool interactable)
        {
            if (departureButton != null)
            {
                departureButton.interactable = interactable;
            }

            if (formationButton != null)
            {
                formationButton.interactable = interactable;
            }

            if (tacticsButton != null)
            {
                tacticsButton.interactable = interactable;
            }
        }

        private void SetTopLevelButtonsVisible(bool visible)
        {
            if (departureButton != null)
            {
                departureButton.gameObject.SetActive(visible);
            }

            if (formationButton != null)
            {
                formationButton.gameObject.SetActive(visible);
            }

            if (tacticsButton != null)
            {
                tacticsButton.gameObject.SetActive(visible);
            }
        }

        private void ApplyBackground(SceneUIRoot sceneUIRoot)
        {
            if (backgroundSprite == null)
            {
                return;
            }

            if (!sceneUIRoot.TryGetElement<Image>(HubUIElementIds.Background, out var background))
            {
                Debug.LogWarning($"Hub UI에서 배경 Image('{HubUIElementIds.Background}')를 찾을 수 없다. {nameof(UIElementMarker)}가 부착되어 있는지 확인하라.");
                return;
            }

            background.sprite = backgroundSprite;
        }

        private static Button BindButton(SceneUIRoot sceneUIRoot, string id, UnityAction action)
        {
            if (!sceneUIRoot.TryGetElement<Button>(id, out var button))
            {
                Debug.LogWarning($"Hub UI에서 '{id}' 버튼을 찾을 수 없다. {nameof(UIElementMarker)}가 부착되어 있는지 확인하라.");
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            return button;
        }
    }
}
