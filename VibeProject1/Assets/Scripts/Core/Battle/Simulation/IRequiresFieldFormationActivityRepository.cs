namespace Game.Core
{
    /// <summary>
    /// IBattleResultRule 구현체가 Field 배치 활동 저장소(Docs/설계/25번 §6)를 필요로 하면 이 마커를
    /// 구현한다(OCP) - IRequiresCaravanRoster/IRequiresUnitConditionRepository와 같은 패턴.
    /// BattleManager가 이 마커를 감지해 주입하므로, 어떤 규칙이 이 저장소를 쓰는지 BattleManager는
    /// 몰라도 된다.
    /// </summary>
    public interface IRequiresFieldFormationActivityRepository
    {
        void SetFieldFormationActivityRepository(IFieldFormationActivityRepository repository);
    }
}
