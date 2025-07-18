using System;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using R3;
using Systems.Input.Utilities;
using Systems.Physics;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player.Locomotion
{
    public class WallSlideLocomotion : IPlayerLocomotion
    {
        #region Fields

        private readonly PlayerMovementAbility _movementAbility;
        private readonly IPhysicsController _physicsController;
        private readonly IGroundDetector _groundDetector;
        private readonly IWallDetector _wallDetector;

        // Wall Slide 상태 추적
        private bool _isWallSliding;
        private WallSideType _currentWallSide;
        private float _wallSlideStartTime;
        private float _lastInput;

        // 설정값들
        private readonly bool _enableDetailedLogging;

        #endregion

        #region Properties

        public string GetName() => "WallSlide";

        /// <summary>현재 벽 슬라이딩 중인지 여부</summary>
        public bool IsWallSliding => _isWallSliding;

        /// <summary>벽 슬라이딩 지속 시간</summary>
        public float WallSlideDuration => _isWallSliding ? Time.time - _wallSlideStartTime : 0f;

        #endregion

        #region Constructor

        public WallSlideLocomotion(PlayerMovementAbility movementAbility,
            IPhysicsController physicsController,
            IGroundDetector groundDetector,
            IWallDetector wallDetector,
            Observable<float> movementInput,
            bool enableDetailedLogging = false)
        {
            _movementAbility = movementAbility ?? throw new ArgumentNullException(nameof(movementAbility));
            _physicsController = physicsController ?? throw new ArgumentNullException(nameof(physicsController));
            _groundDetector = groundDetector ?? throw new ArgumentNullException(nameof(groundDetector));
            _wallDetector = wallDetector ?? throw new ArgumentNullException(nameof(wallDetector));
            if (movementInput == null)
            {
                throw new ArgumentNullException(nameof(movementInput));
            }

            _enableDetailedLogging = enableDetailedLogging;

            // 입력 추적
            movementInput.Subscribe(input => _lastInput = input);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("WallSlideLocomotion initialized", LogCategory.Player);
            }
        }

        #endregion

        #region IPlayerLocomotion Implementation

        public bool CanExecute(float horizontalInput)
        {
            try
            {
                // 1. 벽 슬라이딩 기능이 활성화되어 있는지
                if (!_movementAbility.HasWallSlide)
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallSlide: Feature not enabled", LogCategory.Player);
                    }

                    return false;
                }

                // 2. 벽이 감지되고 있는지
                if (!_wallDetector.IsCurrentlyDetectingWall())
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallSlide: No wall detected", LogCategory.Player);
                    }

                    return false;
                }

                // 3. 땅에 닿지 않았는지
                if (_groundDetector.IsCurrentlyGrounded())
                {
                    if (_enableDetailedLogging && _isWallSliding)
                    {
                        GameLogger.Debug("WallSlide: Currently grounded", LogCategory.Player);
                    }

                    return false;
                }

                // 4. 떨어지고 있는지 (중요: 올라가는 중에는 벽 슬라이딩 안 함)
                var velocity = _physicsController.GetVelocity();
                if (!PhysicsUtility.IsFalling(velocity))
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallSlide: Not falling", LogCategory.Player);
                    }

                    return false;
                }

                // 5. 벽 방향으로 입력하고 있는지
                if (!IsInputTowardsWall())
                {
                    if (_enableDetailedLogging)
                    {
                        GameLogger.Debug("WallSlide: No input towards wall", LogCategory.Player);
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in WallSlideLocomotion.CanExecute: ", e.Message),
                    LogCategory.Player);
                return false;
            }
        }

        public void Execute(float horizontalInput)
        {
            try
            {
                if (!CanExecute(horizontalInput))
                {
                    if (_isWallSliding)
                    {
                        EndWallSlide();
                    }

                    return;
                }

                if (!_isWallSliding)
                {
                    StartWallSlide();
                }

                // 벽 슬라이딩 물리 적용
                ApplyWallSlidePhysics();

                if (_enableDetailedLogging)
                {
                    var wallSide = _wallDetector.WallSide.CurrentValue;
                    GameLogger.Debug(ZString.Concat("WallSlide executing on ", wallSide, " wall"), LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in WallSlideLocomotion.Execute: ", e.Message),
                    LogCategory.Player);
            }
        }

        public void Dispose()
        {
            if (_isWallSliding)
            {
                EndWallSlide();
            }

            if (_enableDetailedLogging)
            {
                GameLogger.Debug("WallSlideLocomotion disposed", LogCategory.Player);
            }
        }

        #endregion

        #region Wall Slide Logic

        private void StartWallSlide()
        {
            _isWallSliding = true;
            _currentWallSide = _wallDetector.WallSide.CurrentValue;
            _wallSlideStartTime = Time.time;

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Wall slide started on ", _currentWallSide, " wall"),
                    LogCategory.Player);
            }
        }

        private void EndWallSlide()
        {
            if (!_isWallSliding) return;

            _isWallSliding = false;

            if (_enableDetailedLogging)
            {
                var slideTime = Time.time - _wallSlideStartTime;
                GameLogger.Debug(ZString.Concat("Wall slide ended after ", slideTime.ToString("F2"), "s"),
                    LogCategory.Player);
            }
        }

        private void ApplyWallSlidePhysics()
        {
            var currentVelocity = _physicsController.GetVelocity();

            // 1. 수직 속도를 벽 슬라이딩 속도로 제한
            ApplyWallSlideVerticalSpeed(currentVelocity);

            // 2. 수평 속도 조정 (벽에 살짝 붙어있도록)
            // ApplyWallSlideHorizontalSpeed();
        }

        private void ApplyWallSlideVerticalSpeed(Vector2 currentVelocity)
        {
            // 벽 슬라이딩 시 최대 낙하 속도 제한
            float maxWallSlideSpeed = -_movementAbility.WallSlideSpeed;

            // 현재 속도가 벽 슬라이딩 속도보다 빠르면 제한
            if (currentVelocity.y < maxWallSlideSpeed)
            {
                _physicsController.RequestVelocity(
                    VelocityRequest.SetVertical(maxWallSlideSpeed, VelocityPriority.Override)
                );

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Wall slide speed limited to: ", maxWallSlideSpeed.ToString("F2")),
                        LogCategory.Player);
                }
            }
        }

        private void ApplyWallSlideHorizontalSpeed()
        {
            // 벽에 살짝 붙어있도록 수평 속도 조정
            float wallDirection = _currentWallSide == WallSideType.Right ? 0.2f : -0.2f;

            _physicsController.RequestVelocity(
                VelocityRequest.SetHorizontal(wallDirection, VelocityPriority.Background)
            );
        }

        private bool IsInputTowardsWall()
        {
            var currentWallSide = _wallDetector.WallSide.CurrentValue;

            // 입력이 없으면 벽 슬라이딩 불가
            if (InputUtility.InDeadZone(_lastInput))
            {
                return false;
            }

            var inputDirection = InputUtility.GetInputDirection(_lastInput);

            // 벽 방향과 입력 방향이 일치하는지 확인
            bool towardsWall = (currentWallSide == WallSideType.Right && inputDirection > 0) ||
                               (currentWallSide == WallSideType.Left && inputDirection < 0);

            if (_enableDetailedLogging && !towardsWall && _isWallSliding)
            {
                GameLogger.Debug(ZString.Concat("Input not towards wall. Wall: ", currentWallSide,
                    ", Input direction: ", inputDirection), LogCategory.Player);
            }

            return towardsWall;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 벽 슬라이딩 정보
        /// </summary>
        public string GetWallSlideDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Wall Slide Locomotion ===");
            sb.AppendLine(ZString.Concat("Can Execute: ", CanExecute(_lastInput)));
            sb.AppendLine(ZString.Concat("Is Wall Sliding: ", _isWallSliding));

            if (_isWallSliding)
            {
                sb.AppendLine(ZString.Concat("Wall Side: ", _currentWallSide));
                sb.AppendLine(ZString.Concat("Duration: ", WallSlideDuration.ToString("F2"), "s"));
            }

            sb.AppendLine(ZString.Concat("Has Wall Slide: ", _movementAbility.HasWallSlide));
            sb.AppendLine(ZString.Concat("Wall Detected: ", _wallDetector.IsCurrentlyDetectingWall()));
            sb.AppendLine(ZString.Concat("Is Grounded: ", _groundDetector.IsCurrentlyGrounded()));
            sb.AppendLine(ZString.Concat("Is Falling: ", PhysicsUtility.IsFalling(_physicsController.GetVelocity())));
            sb.AppendLine(ZString.Concat("Input Towards Wall: ", IsInputTowardsWall()));
            sb.AppendLine(ZString.Concat("Last Input: ", _lastInput.ToString("F2")));
            sb.Append(ZString.Concat("Wall Slide Speed: ", _movementAbility.WallSlideSpeed.ToString("F2")));

            return sb.ToString();
        }
#endif

        #endregion
    }
}
