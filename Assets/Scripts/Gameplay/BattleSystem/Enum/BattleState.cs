namespace Gameplay.BattleSystem.Enum
{
    public enum BattleState
    {
        BattleStart,                // 전투 초기화
        PlayerTurn,                 // 플레이어 턴
        EnemyTurn,                  // 적 턴
        EnemyTurnTransition,        // 적 턴 전환 대기
        BattleEnd                   // 전투 종료
    }
}
