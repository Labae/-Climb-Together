namespace Gameplay.BattleSystem.Interfaces
{
    public interface ITurnComponent
    {
        void Initialize(string unitName, IShieldComponent shieldComponent);
        void OnTurnStart();
        void OnTurnEnd();
    }
}
