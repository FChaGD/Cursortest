using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 시뮬레이션이 새로 만들어졌다는 사실, 전투 도중 아군이 늦게 합류했다는 사실, 그리고 아직 합류
    /// 전인 신규 아군의 진행 상태만 알리는 좁은 계약 - 뷰 계층(BattleViewPresenter)이
    /// LiveBattleSimulationRule의 다른 멤버(Evaluate 등)를 몰라도 되게 한다(ISP). OnAllySpawnedMidBattle은
    /// Field 배치 시간(Docs/설계/25번 §6.2) 타이머가 전투 도중 완료돼 유닛이 늦게 합류할 때
    /// 발행된다 - OnSimulationBuilt(전투 시작 1회)에는 반영되지 않는 유닛이라 뷰가 별도로 스폰해야
    /// 한다. OnPendingReinforcementsChanged는 그 전까지(타이머가 아직 남은 동안) 매 틱 현재 목록을
    /// 통째로 알려준다(Docs/설계/25번 §6.3, 정비창 UI를 열 수 없는 전투 중에도 "곧 합류한다"는
    /// 정보를 전달하기 위해 사용자가 추가 확정한 범위).
    /// </summary>
    public interface IBattleSimulationEvents
    {
        event Action<BattleSimulationLoop> OnSimulationBuilt;
        event Action<IBattleCombatant> OnAllySpawnedMidBattle;
        event Action<IReadOnlyList<PendingReinforcementInfo>> OnPendingReinforcementsChanged;
    }
}
