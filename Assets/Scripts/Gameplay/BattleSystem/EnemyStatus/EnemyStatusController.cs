using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Gameplay.BattleSystem.UI.Base;
using R3;

namespace Gameplay.BattleSystem.EnemyStatus
{
    public class EnemyStatusController : BaseController<EnemyStatusModel, EnemyStatusView>
    {
        public EnemyStatusController(EnemyStatusModel model, EnemyStatusView view)
         : base(model, view)
        {
        }

        protected override void SetupBindings()
        {
            // 초기값 설정
            View.SetUnitName(Model.UnitName.Value);

            // 바인딩
            Model.HealthComponent.Health
                .Subscribe(healthData =>
                {
                    View.UpdateHealthAnimate(healthData).Forget();
                })
                .AddTo(_disposables);

            Model.ShieldComponent.Shield
                .Subscribe(shieldData =>
                {
                    View.UpdateShieldAnimate(shieldData).Forget();
                })
                .AddTo(_disposables);

            Model.HealthComponent.IsLowHealth
                .Subscribe(isLow =>
                {
                    View.SetLowHealthWarning(isLow);
                })
                .AddTo(_disposables);

            Model.HealthComponent.OnDamageTaken
                .Subscribe(damageData =>
                {
                    View.TriggerDamageEffect();

                    if (damageData.IsCritical)
                    {
                        View.ShowCriticalHitEffect();
                    }
                })
                .AddTo(_disposables);

            Model.HealthComponent.OnHealed
                .Subscribe(_ =>
                {
                    View.ShowHealEffect();
                })
                .AddTo(_disposables);

            Model.ShieldComponent.OnUnitBroken
                .Subscribe(_ =>
                {
                    GameLogger.Debug(ZString.Concat(Model.UnitName.Value, " is broken!"));
                })
                .AddTo(_disposables);
        }
    }
}
