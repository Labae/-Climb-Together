using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using R3;
using UnityEngine;

namespace Gameplay.Player.Jump.Actions
{
    /// <summary>
    /// 공중에서의 점프 액션 (더블 점프)
    /// </summary>
    public class AirJumpAction : IPlayerAction
    {
        #region Fields

        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundDetector _groundDetector;
        private readonly CompositeDisposable _disposables = new();

        // 점프 상태 관리
        private int _remainingJumps;
        private int _maxAirJumps;

        // 성능 추적
        private float _lastExecutionTime;
        private int _executionCount;

        // 설정
        private readonly bool _enableDetailedLogging;

        #endregion

        #region Properties

        /// <summary>남은 공중 점프 횟수</summary>
        public int RemainingJumps => _remainingJumps;

        /// <summary>최대 공중 점프 횟수</summary>
        public int MaxAirJumps => _maxAirJumps;

        /// <summary>마지막 실행 시간</summary>
        public float LastExecutionTime => _lastExecutionTime;

        /// <summary>총 실행 횟수</summary>
        public int ExecutionCount => _executionCount;

        /// <summary>공중 점프 파워</summary>
        public float AirJumpPower =>
            (_movementAbility?.JumpPower ?? 0f) * (_movementAbility?.DoubleJumpMultiplier ?? 1f);

        #endregion

        #region Constructor

        public AirJumpAction(PlayerMovementAbility movementAbility, IPhysicsController physicsController,
            IGroundDetector groundDetector, bool enableDetailedLogging = false)
        {
            _movementAbility = movementAbility ?? throw new ArgumentNullException(nameof(movementAbility));
            _physicsController = physicsController ?? throw new ArgumentNullException(nameof(physicsController));
            _groundDetector = groundDetector ?? throw new ArgumentNullException(nameof(groundDetector));
            _enableDetailedLogging = enableDetailedLogging;

            // 최대 공중 점프 횟수 설정
            _maxAirJumps = _movementAbility.HasDoubleJump ? 1 : 0;
            _remainingJumps = 0; // 처음엔 공중 점프 불가

            SetupGroundEvents();

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("AirJumpAction initialized (Max jumps: ", _maxAirJumps, ")"),
                    LogCategory.Player);
            }
        }

        #endregion

        #region Setup

        private void SetupGroundEvents()
        {
            try
            {
                // 착지 시 공중 점프 횟수 초기화
                _groundDetector.OnGroundEntered
                    .Subscribe(_ => OnGroundEntered())
                    .AddTo(_disposables);

                // 땅에서 떨어질 때 공중 점프 활성화
                _groundDetector.OnGroundExited
                    .Subscribe(_ => OnGroundExited())
                    .AddTo(_disposables);
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error setting up AirJumpAction events: ", e.Message),
                    LogCategory.Player);
            }
        }

        private void OnGroundEntered()
        {
            _remainingJumps = _maxAirJumps;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Air jumps reset: ", _remainingJumps), LogCategory.Player);
            }
        }

        private void OnGroundExited()
        {
            if (_enableDetailedLogging)
            {
                GameLogger.Debug("Left ground - air jumps available", LogCategory.Player);
            }
        }

        #endregion

        #region IPlayerAction Implementation

        public bool CanExecute()
        {
            try
            {
                // 기본 조건 확인
                if (_movementAbility == null || _physicsController == null || _groundDetector == null)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Warning("AirJump: Missing required components", LogCategory.Player);
                    }

                    return false;
                }

                // 더블 점프 기능 확인
                if (!_movementAbility.HasDoubleJump)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("AirJump: Double jump not enabled", LogCategory.Player);
                    }

                    return false;
                }

                // 접지 상태 확인 (공중에 있어야 함)
                if (_groundDetector.IsCurrentlyGrounded())
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("AirJump: Currently grounded", LogCategory.Player);
                    }

                    return false;
                }

                // 남은 점프 횟수 확인
                if (_remainingJumps <= 0)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("AirJump: No remaining jumps", LogCategory.Player);
                    }

                    return false;
                }

                // 점프 파워 확인
                if (AirJumpPower <= 0f)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Warning("AirJump: Invalid jump power", LogCategory.Player);
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in AirJumpAction.CanExecute: ", e.Message), LogCategory.Player);
                return false;
            }
        }

        public void Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    GameLogger.Warning("AirJumpAction.Execute called when CanExecute is false", LogCategory.Player);
                    return;
                }

                var airJumpPower = AirJumpPower;
                _physicsController.Jump(airJumpPower);
                _remainingJumps--;

                // 실행 정보 업데이트
                _lastExecutionTime = Time.time;
                _executionCount++;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Air Jump executed with power: ", airJumpPower.ToString("F2"),
                        " (Remaining: ", _remainingJumps, ")"), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in AirJumpAction.Execute: ", e.Message), LogCategory.Player);
            }
        }

        public void Dispose()
        {
            try
            {
                _disposables?.Dispose();

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("AirJumpAction disposed (executed ", _executionCount, " times)"),
                        LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error disposing AirJumpAction: ", e.Message), LogCategory.Player);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 남은 점프 횟수를 강제로 설정 (디버그용)
        /// </summary>
        /// <param name="count">설정할 횟수</param>
        public void ForceSetRemainingJumps(int count)
        {
            _remainingJumps = Mathf.Clamp(count, 0, _maxAirJumps);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Air jumps force set to: ", _remainingJumps), LogCategory.Player);
            }
        }

        /// <summary>
        /// 공중 점프 횟수 초기화
        /// </summary>
        public void ResetAirJumps()
        {
            _remainingJumps = _maxAirJumps;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("Air jumps manually reset", LogCategory.Player);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 정보 반환
        /// </summary>
        public string GetDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Air Jump Action ===");
            sb.AppendLine(ZString.Concat("Can Execute: ", CanExecute()));
            sb.AppendLine(ZString.Concat("Has Double Jump: ", _movementAbility?.HasDoubleJump ?? false));
            sb.AppendLine(ZString.Concat("Air Jump Power: ", AirJumpPower.ToString("F2")));
            sb.AppendLine(ZString.Concat("Remaining Jumps: ", _remainingJumps, "/", _maxAirJumps));
            sb.AppendLine(ZString.Concat("Execution Count: ", _executionCount));
            sb.AppendLine(ZString.Concat("Last Execution: ", _lastExecutionTime.ToString("F2"), "s"));
            sb.Append(ZString.Concat("Is Grounded: ", _groundDetector?.IsCurrentlyGrounded() ?? false));
            return sb.ToString();
        }
#endif

        #endregion
    }
}
