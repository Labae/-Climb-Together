using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.Actions
{
    /// <summary>
    /// 지상에서의 점프 액션
    /// </summary>
    public class GroundJumpAction : IPlayerAction
    {
        #region Fields

        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundDetector _groundDetector;

        // 성능 추적
        private float _lastExecutionTime;
        private int _executionCount;

        // 설정
        private readonly bool _enableDetailedLogging;

        #endregion

        #region Properties

        /// <summary>마지막 실행 시간</summary>
        public float LastExecutionTime => _lastExecutionTime;

        /// <summary>총 실행 횟수</summary>
        public int ExecutionCount => _executionCount;

        /// <summary>점프 파워</summary>
        public float JumpPower => _movementAbility?.JumpPower ?? 0f;

        #endregion

        #region Constructor

        public GroundJumpAction(PlayerMovementAbility movementAbility, IPhysicsController physicsController,
            IGroundDetector groundDetector, bool enableDetailedLogging = false)
        {
            _movementAbility = movementAbility ?? throw new ArgumentNullException(nameof(movementAbility));
            _physicsController = physicsController ?? throw new ArgumentNullException(nameof(physicsController));
            _groundDetector = groundDetector ?? throw new ArgumentNullException(nameof(groundDetector));
            _enableDetailedLogging = enableDetailedLogging;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("GroundJumpAction initialized", LogCategory.Player);
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
                        GameLogger.Warning("GroundJump: Missing required components", LogCategory.Player);
                    }

                    return false;
                }

                // 접지 상태 확인
                bool isGrounded = _groundDetector.IsCurrentlyGrounded();
                if (!isGrounded)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("GroundJump: Not grounded", LogCategory.Player);
                    }

                    return false;
                }

                // 점프 파워 확인
                if (_movementAbility.JumpPower <= 0f)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Warning("GroundJump: Invalid jump power", LogCategory.Player);
                    }

                    return false;
                }

                // 물리 상태 확인 (이미 상승 중이면 점프 불가)
                if (PhysicsUtility.IsRising(_physicsController.GetVelocity()))
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("GroundJump: Already rising", LogCategory.Player);
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in GroundJumpAction.CanExecute: ", e.Message),
                    LogCategory.Player);
                return false;
            }
        }

        public void Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    GameLogger.Warning("GroundJumpAction.Execute called when CanExecute is false", LogCategory.Player);
                    return;
                }

                var jumpPower = _movementAbility.JumpPower;
                _physicsController.Jump(jumpPower);

                // 실행 정보 업데이트
                _lastExecutionTime = Time.time;
                _executionCount++;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Ground Jump executed with power: ", jumpPower.ToString("F2")),
                        LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in GroundJumpAction.Execute: ", e.Message), LogCategory.Player);
            }
        }

        public void Dispose()
        {
            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("GroundJumpAction disposed (executed ", _executionCount, " times)"),
                    LogCategory.Player);
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
            sb.AppendLine("=== Ground Jump Action ===");
            sb.AppendLine(ZString.Concat("Can Execute: ", CanExecute()));
            sb.AppendLine(ZString.Concat("Jump Power: ", JumpPower.ToString("F2")));
            sb.AppendLine(ZString.Concat("Execution Count: ", _executionCount));
            sb.AppendLine(ZString.Concat("Last Execution: ", _lastExecutionTime.ToString("F2"), "s"));
            sb.Append(ZString.Concat("Is Grounded: ", _groundDetector?.IsCurrentlyGrounded() ?? false));
            return sb.ToString();
        }
#endif

        #endregion
    }
}
