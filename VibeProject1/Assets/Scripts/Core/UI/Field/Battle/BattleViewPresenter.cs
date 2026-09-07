using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 순수 C# 협력 객체. FieldUIController가 최초 1회 생성해 유지하고, IBattleSimulationEvents를
    /// 구독해 매 전투 시작마다 View를 새로 스폰한다(FieldEncounterFlowCoordinator와 같은
    /// Bind/RebindViews 2단 구조 - 이벤트 구독은 최초 1회, 씬 참조 교체는 Field 씬 로드마다).
    /// 시뮬레이션(LiveBattleSimulationRule)은 이 클래스를 전혀 모른다 - 렌더링과 로직을 분리한다.
    /// </summary>
    internal class BattleViewPresenter
    {
        private bool eventsBound;

        // 월드 오브젝트 전환(Docs/설계/13번) - Canvas 하위 RectTransform이 아니라 BattleWorldRoot
        // 산하 일반 Transform이다(UI 좌표계와 섞이지 않도록).
        private Transform allyContainer;
        private Transform enemyContainer;
        private BattleCharacterUnitView characterViewPrefab;
        private BattleProtectedUnitView protectedViewPrefab;
        private BattlePendingReinforcementView pendingReinforcementViewPrefab;
        private BattleFieldWorldCameraView cameraView;
        private BattleBackgroundGridView backgroundView;

        private readonly List<BattleCharacterUnitView> activeCharacterViews = new();
        private readonly List<BattleProtectedUnitView> activeProtectedViews = new();
        // Field 배치 시간(설계 25번 §6.3) 전투 중 신규 소환 유령 - get-or-create 재사용(매 틱 갱신되므로
        // 파괴 후 재생성하면 낭비가 크다).
        private readonly List<BattlePendingReinforcementView> activeReinforcementViews = new();

        public void Bind(IBattleSimulationEvents simulationEvents)
        {
            if (eventsBound) return;

            simulationEvents.OnSimulationBuilt += Present;
            // Field 배치 시간(Docs/설계/25번 §6.2) 타이머가 전투 도중 완료돼 아군이 늦게 합류할 때 -
            // OnSimulationBuilt(전투 시작 1회)에는 반영되지 않는 유닛이라 뷰를 별도로 스폰해야 한다.
            simulationEvents.OnAllySpawnedMidBattle += unit => SpawnCharacterView(unit, allyContainer);
            simulationEvents.OnPendingReinforcementsChanged += HandlePendingReinforcementsChanged;
            eventsBound = true;
        }

        public void RebindViews(
            Transform allyContainer, Transform enemyContainer,
            BattleCharacterUnitView characterViewPrefab, BattleProtectedUnitView protectedViewPrefab,
            BattlePendingReinforcementView pendingReinforcementViewPrefab,
            BattleFieldWorldCameraView cameraView, BattleBackgroundGridView backgroundView)
        {
            this.allyContainer = allyContainer;
            this.enemyContainer = enemyContainer;
            this.characterViewPrefab = characterViewPrefab;
            this.protectedViewPrefab = protectedViewPrefab;
            this.pendingReinforcementViewPrefab = pendingReinforcementViewPrefab;
            this.cameraView = cameraView;
            this.backgroundView = backgroundView;
        }

        private void Present(BattleSimulationLoop simulation)
        {
            Clear();
            // 유닛을 스폰하기 전에 이번 전투의 전장 크기로 카메라/배경을 먼저 리셋한다(기획 §5) -
            // 리셋 시점을 스폰 직전으로 맞춰야 이전 전투의 확대 상태/타일 배치가 새 유닛에 잠깐이라도
            // 안 맞게 겹쳐 보이지 않는다.
            cameraView?.ConfigureFieldBounds(simulation.FieldRadius);
            backgroundView?.ConfigureField(simulation.SpawnRadius);
            foreach (var unit in simulation.Allies) SpawnCharacterView(unit, allyContainer);
            foreach (var unit in simulation.Enemies) SpawnCharacterView(unit, enemyContainer);
            foreach (var unit in simulation.ProtectedUnits) SpawnProtectedView(unit, allyContainer);
        }

        private void SpawnCharacterView(IBattleCombatant unit, Transform parent)
        {
            if (characterViewPrefab == null || parent == null) return;
            var view = Object.Instantiate(characterViewPrefab, parent);
            view.Bind(unit);
            activeCharacterViews.Add(view);
        }

        private void SpawnProtectedView(IDamageable unit, Transform parent)
        {
            if (protectedViewPrefab == null || parent == null) return;
            var view = Object.Instantiate(protectedViewPrefab, parent);
            view.Bind(unit);
            activeProtectedViews.Add(view);
        }

        // Field 배치 시간(설계 25번 §6.3) - 전투 중 진행 상태가 바뀔 때마다(매 틱) 전체 목록을 받아
        // get-or-create로 재사용한다. 개수가 줄면 남는 인스턴스는 숨기기만 하고 파괴하지 않는다.
        private void HandlePendingReinforcementsChanged(IReadOnlyList<PendingReinforcementInfo> pending)
        {
            if (pendingReinforcementViewPrefab == null || allyContainer == null) return;

            while (activeReinforcementViews.Count < pending.Count)
            {
                activeReinforcementViews.Add(Object.Instantiate(pendingReinforcementViewPrefab, allyContainer));
            }

            for (var i = 0; i < activeReinforcementViews.Count; i++)
            {
                if (i < pending.Count)
                {
                    activeReinforcementViews[i].gameObject.SetActive(true);
                    activeReinforcementViews[i].Bind(pending[i].Position, pending[i].RemainingSeconds);
                }
                else
                {
                    activeReinforcementViews[i].gameObject.SetActive(false);
                }
            }
        }

        // 다음 전투 시작 시 이전 전투의 View가 남아있지 않도록 정리한다. 개별 View는 사망/도주 시
        // 스스로 Destroy되지만(FadeAndDestroy), 전투가 도중에 중단되는 경우까지 대비한 안전장치다.
        private void Clear()
        {
            foreach (var view in activeCharacterViews) { if (view != null) Object.Destroy(view.gameObject); }
            foreach (var view in activeProtectedViews) { if (view != null) Object.Destroy(view.gameObject); }
            foreach (var view in activeReinforcementViews) { if (view != null) Object.Destroy(view.gameObject); }
            activeCharacterViews.Clear();
            activeProtectedViews.Clear();
            activeReinforcementViews.Clear();
        }
    }
}
