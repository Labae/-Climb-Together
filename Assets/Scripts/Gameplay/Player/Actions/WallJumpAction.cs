using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Systems.Physics;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.Actions
{
    public class WallJumpAction : IPlayerAction
    {
        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundDetector _groundDetector;
        private readonly IWallDetector _wallDetector;

        private float _lastExecutionTime;
        private int _executionCount;

        private readonly bool _enableDetailedLogging;

        public int LastExecutionCount => _executionCount;
        public int ExecutionCount => _executionCount;
        public float WallJumpForce => _movementAbility?.WallJumpForce ?? 0f;
        public float WallJumpDuration => _movementAbility?.WallJumpDuration ?? 0f;

        public WallJumpAction(PlayerMovementAbility movementAbility,
            IPhysicsController physicsController,
            IGroundDetector groundDetector, IWallDetector wallDetector,
            bool enableDetailedLogging = false)
        {
            _movementAbility = movementAbility;
            _physicsController = physicsController;
            _groundDetector = groundDetector;
            _wallDetector = wallDetector;
            _enableDetailedLogging = enableDetailedLogging;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("WallJumpAction initialized", LogCategory.Player);
            }
        }

        public bool CanExecute()
        {
            try
            {
                // 기본 조건 확인
                if (_movementAbility == null || _physicsController == null || _groundDetector == null ||
                    _wallDetector == null)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Warning("WallJump: Missing required components", LogCategory.Player);
                    }

                    return false;
                }

                if (!_movementAbility.HasWallJump)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallJump: Wall jump not enabled", LogCategory.Player);
                    }

                    return false;
                }

                // 접지 상태 확인
                if (_groundDetector.IsCurrentlyGrounded())
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallJump: Currently grounded", LogCategory.Player);
                    }

                    return false;
                }

                if (!_wallDetector.IsCurrentlyDetectingWall())
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallJump: No wall detected", LogCategory.Player);
                    }

                    return false;
                }

                // 점프 파워 확인
                if (_movementAbility.WallJumpForce <= 0f)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Warning("WallJump: Invalid jump power", LogCategory.Player);
                    }

                    return false;
                }

                // 물리 상태 확인 (이미 하강 중이지 않으면 점프 불가)
                if (!PhysicsUtility.IsFalling(_physicsController.GetVelocity()))
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallJump: Not falling", LogCategory.Player);
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in WallJumpAction: ", e.Message), LogCategory.Player);
                return false;
            }
        }

        public void Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    GameLogger.Warning("WallJump.Execute called when CanExecute is false", LogCategory.Player);
                    return;
                }

                var wallJumpForce = _movementAbility.WallJumpForce;
                var wallSide = _wallDetector.WallSide.CurrentValue;

                _physicsController.Jump(wallJumpForce);

                _lastExecutionTime = Time.time;
                _executionCount++;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(
                        ZString.Concat("WallJump: Wall jump executed from ", wallSide, " with force: ",
                            wallJumpForce.ToString("F2")), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("WallJumpAction: Disposing...", LogCategory.Player));
            }
        }

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 정보 반환
        /// </summary>
        public string GetDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Wall Jump Action ===");
            sb.AppendLine(ZString.Concat("Can Execute: ", CanExecute()));
            sb.AppendLine(ZString.Concat("Has Wall Jump: ", _movementAbility?.HasWallJump ?? false));
            sb.AppendLine(ZString.Concat("Wall Jump Force: ", WallJumpForce.ToString("F2")));
            sb.AppendLine(ZString.Concat("Wall Jump Duration: ", WallJumpDuration.ToString("F2"), "s"));
            sb.AppendLine(ZString.Concat("Execution Count: ", _executionCount));
            sb.AppendLine(ZString.Concat("Last Execution: ", _lastExecutionTime.ToString("F2"), "s"));
            sb.AppendLine(ZString.Concat("Is Grounded: ", _groundDetector?.IsCurrentlyGrounded() ?? false));
            sb.AppendLine(ZString.Concat("Wall Detected: ", _wallDetector?.IsCurrentlyDetectingWall() ?? false));
            if (_wallDetector?.IsCurrentlyDetectingWall() == true)
            {
                sb.Append(ZString.Concat("Wall Side: ", _wallDetector.WallSide.CurrentValue));
            }
            return sb.ToString();
        }
#endif

        #endregion
    }
}
