using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 전투 중 Field 배치 타이머가 아직 진행 중인 신규 아군 1명의 정보(Docs/설계/25번 §6.3) -
    /// BattleViewPresenter가 반투명 유령(BattlePendingReinforcementView)을 그리는 데 쓴다. 실제
    /// BattleCharacterUnit이 아니라 순수 표시용 데이터라 IBattleCombatant를 구현하지 않는다.
    /// </summary>
    public readonly struct PendingReinforcementInfo
    {
        public string UnitId { get; }
        public Vector2 Position { get; }
        public float RemainingSeconds { get; }

        public PendingReinforcementInfo(string unitId, Vector2 position, float remainingSeconds)
        {
            UnitId = unitId;
            Position = position;
            RemainingSeconds = remainingSeconds;
        }
    }
}
