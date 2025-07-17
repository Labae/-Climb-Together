using System;
using System.Collections.Generic;
using Cysharp.Text;
using Data.Player.Abilities;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using Gameplay.Player.Interfaces;
using Gameplay.Player.Locomotion;
using R3;
using Systems.Input.Utilities;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerLocomotion : IDisposable
    {
        private readonly List<IPlayerLocomotion> _locomotions;
        private readonly Subject<IPlayerLocomotion> _onLocomotionExecuted = new();

        // 입력 추적 및 캐싱
        private float _lastValidInputTime;
        private float _lastValidInput;
        private float _lastProcessedInput;
        private bool _isExecuting;

        // 상태 추적
        private IPlayerLocomotion _currentLocomotion;
        private readonly ReactiveProperty<bool> _isMoving = new();

        private readonly CompositeDisposable _disposables = new();

        #region Properties

        public Observable<IPlayerLocomotion> OnLocomotionExecuted => _onLocomotionExecuted.AsObservable();

        /// <summary>현재 로코모션이 실행 중인지 여부</summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>현재 실행 중인 로코모션</summary>
        public IPlayerLocomotion CurrentLocomotion => _currentLocomotion;

        /// <summary>움직임 상태 (반응형)</summary>
        public ReadOnlyReactiveProperty<bool> IsMoving => _isMoving.ToReadOnlyReactiveProperty();

        /// <summary>마지막 유효한 입력값</summary>
        public float LastValidInput => _lastValidInput;

        /// <summary>마지막 유효한 입력 시간</summary>
        public float LastValidInputTime => _lastValidInputTime;

        #endregion

        public PlayerLocomotion(PlayerMovementAbility movementAbility,
            Observable<float> movementInput,
            IPhysicsController physicsController,
            IGroundChecker groundChecker)
        {
            _locomotions = new List<IPlayerLocomotion>
            {
                new DefaultLocomotion(movementAbility, physicsController, groundChecker),
                new NoneLocomotion(physicsController),
            };

            SetupInputHandling(movementInput);
        }

        #region Input Handling

        /// <summary>
        /// 입력 처리 설정 - InputUtility를 활용한 향상된 입력 처리
        /// </summary>
        private void SetupInputHandling(Observable<float> movementInput)
        {
            // 기본 입력 처리
            movementInput
                .Subscribe(OnMovementInput)
                .AddTo(_disposables);

            // 입력 활성화/비활성화 감지
            movementInput
                .Select(input => InputUtility.IsInputActive(input))
                .DistinctUntilChanged()
                .Subscribe(OnInputActiveStateChanged)
                .AddTo(_disposables);

            // 입력 방향 변화 감지
            movementInput
                .Select(input => InputUtility.GetInputDirection(input))
                .DistinctUntilChanged()
                .Where(direction => direction != 0)
                .Subscribe(OnInputDirectionChanged)
                .AddTo(_disposables);

            // 스무딩된 입력 처리 (선택적)
            var smoothedInput = movementInput
                .Scan((previous, current) => InputUtility.SmoothInput(previous, current, 10f, Time.fixedDeltaTime))
                .Where(input => !InputUtility.InDeadZone(input));

            // 부드러운 입력 변화에 대한 반응
            smoothedInput
                .Subscribe(OnSmoothedInput)
                .AddTo(_disposables);
        }

        private void OnMovementInput(float horizontalInput)
        {
            // InputUtility를 사용하여 입력 유효성 검사
            if (InputUtility.IsInputActive(horizontalInput))
            {
                UpdateValidInput(horizontalInput);
            }

            ProcessMovement(horizontalInput);
        }

        private void OnInputActiveStateChanged(bool isActive)
        {
            _isExecuting = isActive;

            if (!isActive)
            {
                // 입력이 비활성화되면 None 로코모션 강제 실행
                ProcessMovement(0f);
            }
        }

        private void OnInputDirectionChanged(int direction)
        {
            GameLogger.Debug(ZString.Concat("Input direction changed: ", direction), LogCategory.Player);
        }

        private void OnSmoothedInput(float smoothedInput)
        {
            // 부드러운 입력 변화에 대한 추가 처리 (필요시)
        }

        /// <summary>
        /// 유효한 입력 정보 업데이트
        /// </summary>
        private void UpdateValidInput(float input)
        {
            _lastValidInput = input;
            _lastValidInputTime = Time.time;
        }

        #endregion

        #region Movement Processing

        private void ProcessMovement(float input)
        {
            // 입력 변화가 있을 때만 처리 (성능 최적화)
            if (Mathf.Approximately(input, _lastProcessedInput))
            {
                return;
            }

            _lastProcessedInput = input;

            foreach (var locomotion in _locomotions)
            {
                if (!locomotion.CanExecute(input))
                {
                    continue;
                }

                ExecuteLocomotion(locomotion, input);
                return;
            }

            // 어떤 로코모션도 실행되지 않은 경우
            GameLogger.Warning(ZString.Concat("No locomotion could be executed for input: ", input), LogCategory.Player);
        }

        /// <summary>
        /// 로코모션 실행 및 상태 업데이트
        /// </summary>
        private void ExecuteLocomotion(IPlayerLocomotion locomotion, float input)
        {
            var previousLocomotion = _currentLocomotion;
            _currentLocomotion = locomotion;

            locomotion.Execute(input);
            _onLocomotionExecuted.OnNext(locomotion);

            // 움직임 상태 업데이트
            UpdateMovementState(locomotion, input);

            // 로코모션 변경 로깅
            LogLocomotionChange(previousLocomotion, locomotion, input);
        }

        /// <summary>
        /// 움직임 상태 업데이트
        /// </summary>
        private void UpdateMovementState(IPlayerLocomotion locomotion, float input)
        {
            bool wasMoving = _isMoving.Value;
            bool isNowMoving = DetermineMovementState(locomotion, input);

            if (wasMoving != isNowMoving)
            {
                _isMoving.OnNext(isNowMoving);
            }
        }

        /// <summary>
        /// 로코모션과 입력을 기반으로 움직임 상태 결정
        /// </summary>
        private bool DetermineMovementState(IPlayerLocomotion locomotion, float input)
        {
            return locomotion switch
            {
                DefaultLocomotion => InputUtility.IsInputActive(input),
                NoneLocomotion => false,
                _ => InputUtility.IsInputActive(input)
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 현재 입력이 특정 방향인지 확인
        /// </summary>
        /// <param name="direction">확인할 방향 (1: 오른쪽, -1: 왼쪽)</param>
        public bool IsInputDirection(int direction)
        {
            return InputUtility.GetInputDirection(_lastValidInput) == direction;
        }

        /// <summary>
        /// 입력이 최근에 변경되었는지 확인
        /// </summary>
        /// <param name="timeThreshold">시간 임계값</param>
        public bool HasRecentInputChange(float timeThreshold = 0.1f)
        {
            return Time.time - _lastValidInputTime <= timeThreshold;
        }

        /// <summary>
        /// 특정 로코모션이 현재 활성화되어 있는지 확인
        /// </summary>
        public bool IsLocomotionActive<T>() where T : IPlayerLocomotion
        {
            return _currentLocomotion is T;
        }

        /// <summary>
        /// 수동으로 로코모션 강제 실행
        /// </summary>
        public void ForceLocomotion(float input)
        {
            ProcessMovement(input);
        }

        /// <summary>
        /// 로코모션 정지
        /// </summary>
        public void StopLocomotion()
        {
            ProcessMovement(0f);
            _isExecuting = false;
        }

        #endregion

        #region Logging

        private void LogLocomotionChange(IPlayerLocomotion previous, IPlayerLocomotion current, float input)
        {
            if (previous != current)
            {
                var message = ZString.Concat(
                    "Locomotion changed: ",
                    previous?.GetName() ?? "None",
                    " -> ",
                    current.GetName(),
                    " (Input: ",
                    input.ToString("F2"),
                    ", Direction: ",
                    InputUtility.GetInputDirection(input),
                    ")"
                );

                GameLogger.Debug(message, LogCategory.Player);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용 로코모션 정보
        /// </summary>
        public string GetLocomotionDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine(ZString.Concat("Current: ", _currentLocomotion?.GetName() ?? "None"));
            sb.AppendLine(ZString.Concat("Executing: ", _isExecuting));
            sb.AppendLine(ZString.Concat("Moving: ", _isMoving.Value));
            sb.AppendLine(ZString.Concat("Last Input: ", _lastValidInput.ToString("F2")));
            sb.AppendLine(ZString.Concat("Input Time: ", (Time.time - _lastValidInputTime).ToString("F2"), "s ago"));
            sb.Append(ZString.Concat("Direction: ", InputUtility.GetInputDirection(_lastValidInput)));
            return sb.ToString();
        }
#endif

        #endregion

        public void Dispose()
        {
            foreach (var locomotionAction in _locomotions)
            {
                locomotionAction?.Dispose();
            }

            _onLocomotionExecuted?.Dispose();
            _isMoving?.Dispose();
            _disposables?.Dispose();
        }
    }
}
