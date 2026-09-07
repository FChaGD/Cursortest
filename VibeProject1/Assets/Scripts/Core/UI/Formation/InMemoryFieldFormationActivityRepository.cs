using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// IFieldFormationActivityRepository의 인메모리 구현(Docs/설계/25번 §3.2). InMemoryFormationRepository와
    /// 같은 성격의 Bootstrap 상주 저장소이지만, 저것과 달리 Update()에서 매 프레임 진행률을 스스로
    /// 갱신한다 - 정비창 UI가 닫혀 있어도(Field 패널이 비활성 상태여도) 배치/이동이 계속 흐르게 하는
    /// 핵심 지점(기획 20번 §1 전제 2).
    /// </summary>
    public class InMemoryFieldFormationActivityRepository : MonoBehaviour, IFieldFormationActivityRepository, IManagedComponent
    {
        private readonly List<FormationActivity> activities = new();
        private IFormationRepository formationRepository;

        public event Action<FormationActivity> OnActivityCompleted;
        public event Action<string> OnActivityCancelled;

        public IReadOnlyList<FormationActivity> ActiveActivities => activities;

        public void RegisterSelf(IDependencyRegistrar registrar)
        {
            registrar.Register<IFieldFormationActivityRepository>(this);
        }

        public void ResolveDependencies(IDependencyRegistrar registrar)
        {
            registrar.TryResolve<IFormationRepository>(out formationRepository);
        }

        public bool TryGetActivity(string unitId, out FormationActivity activity)
        {
            activity = activities.FirstOrDefault(a => a.UnitId == unitId);
            return activity != null;
        }

        public bool IsSlotReserved(int slotIndex) => activities.Any(a => a.TargetSlotIndex == slotIndex);
        public bool IsUnitBusy(string unitId) => activities.Any(a => a.UnitId == unitId);

        public void BeginAdd(string unitId, int targetSlotIndex, float requiredSeconds)
        {
            activities.Add(new FormationActivity(unitId, FormationActivityKind.Adding, targetSlotIndex, FormationActivity.NoSlot, Array.Empty<int>(), requiredSeconds));
        }

        public void BeginMove(string unitId, int originSlotIndex, int targetSlotIndex, IReadOnlyList<int> pathSlotIndices, float requiredSeconds)
        {
            activities.Add(new FormationActivity(unitId, FormationActivityKind.Moving, targetSlotIndex, originSlotIndex, pathSlotIndices, requiredSeconds));
        }

        public void Cancel(string unitId)
        {
            var index = activities.FindIndex(a => a.UnitId == unitId);
            if (index < 0) return;

            activities.RemoveAt(index);
            OnActivityCancelled?.Invoke(unitId);
        }

        public void PauseAll()
        {
            foreach (var activity in activities) activity.IsPaused = true;
        }

        public void ResumeAdding()
        {
            foreach (var activity in activities)
            {
                if (activity.Kind == FormationActivityKind.Adding) activity.IsPaused = false;
            }
        }

        public void ResumeAll()
        {
            foreach (var activity in activities) activity.IsPaused = false;
        }

        public void ForceCompleteAll()
        {
            // Complete()가 리스트에서 제거하므로 스냅샷을 먼저 떠서 순회한다.
            foreach (var activity in activities.ToList())
            {
                Complete(activity);
            }
        }

        private void Update()
        {
            // 역순 순회 - Complete()가 즉시 리스트에서 제거해도 다음 인덱스가 안 밀린다.
            for (var i = activities.Count - 1; i >= 0; i--)
            {
                var activity = activities[i];
                if (activity.IsPaused) continue;

                activity.ElapsedSeconds += Time.deltaTime;
                if (activity.ElapsedSeconds >= activity.RequiredSeconds)
                {
                    Complete(activity);
                }
            }
        }

        private void Complete(FormationActivity activity)
        {
            activities.Remove(activity);
            ApplyToLayout(activity);
            OnActivityCompleted?.Invoke(activity);
        }

        // Adding: 대상 슬롯에 기록. Moving: 출발 슬롯을 비우고 대상 슬롯에 기록 - 실제 FormationLayout은
        // 이동 도중 내내 "출발 슬롯에 그대로 있음"으로 취급된다(설계 25번 §3.2 - 시각적 유령 표시는
        // 별도 오버레이일 뿐 진짜 배치 데이터가 아니다).
        private void ApplyToLayout(FormationActivity activity)
        {
            if (formationRepository == null) return;

            // Hub에서 "적용" 버튼을 한 번도 누르지 않은 상태(TryLoadCurrent 실패)에서 Field가 첫
            // 배치를 완료하는 경우 - 반영할 대상 자체가 없어 조용히 무시되던 버그(실전 확인, 2026-09-05:
            // 타이머/화면 표시는 정상인데 실제로 대열에 배치되지 않아 전투가 아군 0명으로 시작함).
            // 이 경우 최소한의 새 레이아웃을 만들어서라도 반영한다.
            if (!formationRepository.TryLoadCurrent(out var layout))
            {
                layout = new FormationLayout(FormationLayout.DefaultColumnCount, FormationLayout.DefaultRowCount);
            }

            // 출발 슬롯은 "지금도 내가 차지하고 있을 때만" 비운다 - 맞바꾸기(Swap) 이동에서 상대
            // 유닛이 먼저 완료돼 내 출발 슬롯(=상대의 도착 슬롯)을 이미 차지한 경우, 무조건 비우면
            // 방금 도착한 상대를 지워버리는 버그(실전 검증 2026-09-07)가 있었다.
            if (activity.Kind == FormationActivityKind.Moving && layout.GetUnitId(activity.OriginSlotIndex) == activity.UnitId)
            {
                layout.Clear(activity.OriginSlotIndex);
            }
            layout.SetUnitId(activity.TargetSlotIndex, activity.UnitId);
            formationRepository.Apply(layout);
        }
    }
}
