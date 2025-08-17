using System;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Debugging;
using Gameplay.BattleSystem.UI.Views;
using R3;

namespace Gameplay.BattleSystem.EnemyStatus
{
    public class EnemyStatusController : IDisposable
    {
        private readonly EnemyStatusModel _model;
        private readonly EnemyStatusView _view;
        private readonly CompositeDisposable _disposables = new();

        public EnemyStatusController(EnemyStatusModel model, EnemyStatusView view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ??  throw new ArgumentNullException(nameof(view));;
        }

        public void Initialize()
        {
            if (!_model.IsInitialized)
            {
                GameLogger.Error("Model is not initialized");
                return;
            }

            // 초기값 설정
            _view.SetUnitName(_model.UnitName.Value);

            // 바인딩
            _model.HealthComponent.Health
                .Subscribe(healthData =>
                {
                    _view.UpdateHealthAnimate(healthData).Forget();
                })
                .AddTo(_disposables);

            _model.ShieldComponent.Shield
                .Subscribe(shieldData =>
                {
                    _view.UpdateShieldAnimate(shieldData).Forget();
                })
                .AddTo(_disposables);

            _model.HealthComponent.IsLowHealth
                .Subscribe(isLow =>
                {
                    _view.SetLowHealthWarning(isLow);
                })
                .AddTo(_disposables);

            _model.HealthComponent.OnDamageTaken
                .Subscribe(damageData =>
                {
                    _view.TriggerDamageEffect();

                    if (damageData.IsCritical)
                    {
                        _view.ShowCriticalHitEffect();
                    }
                })
                .AddTo(_disposables);

            _model.HealthComponent.OnHealed
                .Subscribe(_ =>
                {
                    _view.ShowHealEffect();
                })
                .AddTo(_disposables);

            _model.ShieldComponent.OnUnitBroken
                .Subscribe(_ =>
                {
                    GameLogger.Debug(ZString.Concat(_model.UnitName.Value, " is broken!"));
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _model?.Dispose();
            _disposables?.Dispose();
        }
    }
}
