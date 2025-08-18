using System;
using System.Collections.Generic;
using Cysharp.Text;
using Debugging;
using Gameplay.BattleSystem.Core;
using Gameplay.BattleSystem.TurnOrder;
using Gameplay.BattleSystem.Units;
using R3;
using VContainer;

public class TurnService : IDisposable
{
    [Inject] private readonly TurnOrderService _turnOrderService;

    private readonly CompositeDisposable _disposables = new();

    // 기존 이벤트 (호환성)
    public event Action<TurnOrderEntry> OnTurnChanged;
    public event Action<int> OnRoundChanged;
    public event Action<IReadOnlyList<TurnOrderEntry>> OnTurnOrderUpdated;

    public BattleUnit CurrentUnit => _turnOrderService.CurrentTurn.CurrentValue?.Unit;
    public bool IsPlayerTurn => CurrentUnit is PlayerUnit;
    public bool IsEnemyTurn => CurrentUnit is EnemyUnit;
    public EnemyUnit CurrentEnemy => IsEnemyTurn ? CurrentUnit as EnemyUnit : null;

    public int ActiveEnemyCount => _turnOrderService.GetActiveEnemies().Count;

    public void Initialize(PlayerUnit playerUnit, List<EnemyUnit> enemyUnits)
    {
        _turnOrderService.Initialize(playerUnit, enemyUnits);
        SetupEventBridges();

        GameLogger.Info(
            ZString.Format("Turn Service 초기화 완료 - 플레이어: {0}, 적: {1}", playerUnit.UnitName, enemyUnits.Count));
    }

    private void SetupEventBridges()
    {
        // CurrentTurn 변경 시
        _turnOrderService.CurrentTurn
            .Skip(1) // 초기값 스킵
            .Subscribe(entry =>
            {
                if (entry != null)
                {
                    GameLogger.Debug(ZString.Format("턴 변경: {0} ({1})", entry.Unit.UnitName,
                        entry.IsPlayer ? "플레이어" : "적"));
                    OnTurnChanged?.Invoke(entry);
                }
            })
            .AddTo(_disposables);

        // 라운드 변경
        _turnOrderService.OnRoundStarted
            .Subscribe(info =>
            {
                GameLogger.Debug(ZString.Format("라운드 {0} 시작!", info.NewRound));
                OnRoundChanged?.Invoke(info.NewRound);
            })
            .AddTo(_disposables);

        // 턴 순서 업데이트
        _turnOrderService.TurnOrder
            .Subscribe(order => OnTurnOrderUpdated?.Invoke(order))
            .AddTo(_disposables);
    }

    public bool HasMoreEnemyTurns() => IsEnemyTurn && _turnOrderService.HasValidTurn();

    public void AdvanceToNextTurn() => _turnOrderService.AdvanceToNextTurn();

    public bool AreAllEnemiesDefeated() => _turnOrderService.GetActiveEnemies().Count == 0;

    public List<EnemyUnit> GetActiveEnemies() => _turnOrderService.GetActiveEnemies();

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
