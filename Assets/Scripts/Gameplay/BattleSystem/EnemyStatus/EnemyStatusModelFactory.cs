using Gameplay.BattleSystem.Core;

namespace Gameplay.BattleSystem.EnemyStatus
{
    /// <summary>
    /// EnemyStatusModel 생성을 담당하는 Factory
    /// </summary>
    public static class EnemyStatusModelFactory
    {
        public static EnemyStatusModel Create(BattleUnit unit)
        {
            if (unit == null)
            {
                throw new System.ArgumentNullException(nameof(unit));
            }

            return new EnemyStatusModel(unit);
        }

        public static EnemyStatusModel[] CreateMultiple(BattleUnit[] units)
        {
            if (units == null)
            {
                throw new System.ArgumentNullException(nameof(units));
            }

            var models = new EnemyStatusModel[units.Length];
            for (var i = 0; i < units.Length; i++)
            {
                if (units[i] == null)
                {
                    continue;
                }

                models[i] = Create(units[i]);
            }

            return models;
        }
    }
}
