using System.Collections.Generic;
using Data.Common;
using Gameplay.Common.Interfaces;
using Gameplay.Physics.Interfaces;
using R3;
using Systems.Physics;
using Systems.Physics.Enums;
using Systems.Physics.Utilities;
using UnityEngine;

namespace Gameplay.Physics
{
    /// <summary>
    /// 우선순위 기반 속도 제어를 제공하는 물리 컨트롤러의 기본 클래스
    /// Override > Set > Add 순서로 처리되며, 각 타입별로 우선순위를 가집니다.
    /// </summary>
    public abstract class PhysicsControllerBase : IPhysicsController
    {
        #region Fields

        // 핵심 컴포넌트
        protected readonly Rigidbody2D _rigidbody2D;
        protected readonly IGroundDetector GroundDetector;
        protected readonly PhysicsSettings _physicsSettings;

        // 속도 요청 시스템 (메모리 할당 최적화)
        private readonly List<VelocityRequest> _pendingRequests = new(16);
        private readonly Dictionary<ForceType, VelocityRequest> _activeForces = new(8);
        private readonly List<VelocityRequest> _setRequests = new(8);
        private readonly List<VelocityRequest> _addRequests = new(8);

        // 중력 시스템
        private bool _gravityEnabled = true;
        private readonly float _normalGravity;
        private readonly float _terminalVelocity;

        // 제약 조건
        private bool _lockHorizontal = false;
        private bool _lockVertical = false;

        // 반응형 속성들
        private readonly ReactiveProperty<Vector2> _velocity = new();
        private readonly ReactiveProperty<float> _horizontalVelocity = new();
        private readonly ReactiveProperty<float> _verticalVelocity = new();
        private readonly ReactiveProperty<bool> _isRising = new();
        private readonly ReactiveProperty<bool> _isFalling = new();
        private readonly ReactiveProperty<bool> _isMoving = new();
        private readonly ReactiveProperty<bool> _isNearlyStationary = new();

        // 캐시된 값들 (성능 최적화)
        private Vector2 _lastVelocity;

        #endregion

        #region Properties

        /// <summary>현재 속도 벡터</summary>
        public ReadOnlyReactiveProperty<Vector2> Velocity => _velocity.ToReadOnlyReactiveProperty();

        /// <summary>현재 수평 속도</summary>
        public ReadOnlyReactiveProperty<float> HorizontalVelocity => _horizontalVelocity.ToReadOnlyReactiveProperty();

        /// <summary>현재 수직 속도</summary>
        public ReadOnlyReactiveProperty<float> VerticalVelocity => _verticalVelocity.ToReadOnlyReactiveProperty();

        /// <summary>상승 중인지 여부</summary>
        public ReadOnlyReactiveProperty<bool> IsRising => _isRising.ToReadOnlyReactiveProperty();

        /// <summary>하강 중인지 여부</summary>
        public ReadOnlyReactiveProperty<bool> IsFalling => _isFalling.ToReadOnlyReactiveProperty();

        /// <summary>움직이고 있는지 여부</summary>
        public ReadOnlyReactiveProperty<bool> IsMoving => _isMoving.ToReadOnlyReactiveProperty();

        /// <summary>거의 정지 상태인지 여부</summary>
        public ReadOnlyReactiveProperty<bool> IsNearlyStationary => _isNearlyStationary.ToReadOnlyReactiveProperty();

        #endregion

        #region Constructor

        protected PhysicsControllerBase(Rigidbody2D rigidbody2D,
            PhysicsSettings physicsSettings, IGroundDetector groundDetector)
        {
            _rigidbody2D = rigidbody2D;
            GroundDetector = groundDetector;
            _physicsSettings = physicsSettings;

            // Unity 물리 설정
            _rigidbody2D.gravityScale = 0;
            _rigidbody2D.freezeRotation = true;

            // 중력 설정
            _normalGravity = physicsSettings.NormalGravity;
            _terminalVelocity = physicsSettings.TerminalVelocity;

            // 초기 속도 캐싱
            _lastVelocity = _rigidbody2D.linearVelocity;
        }

        #endregion

        #region Core Update

        /// <summary>
        /// 물리 업데이트 - FixedUpdate에서 호출되어야 함
        /// </summary>
        public void FixedUpdate()
        {
            ApplyGravity(Time.fixedDeltaTime);
            ProcessVelocityRequests();
        }

        #endregion

        #region Gravity System

        /// <summary>
        /// 중력 적용 - 땅에 닿지 않았을 때만 작동
        /// </summary>
        private void ApplyGravity(float fixedDeltaTime)
        {
            if (!_gravityEnabled || GroundDetector.IsCurrentlyGrounded())
            {
                return;
            }

            var currentGravity = GetCurrentGravity();
            var gravityVelocity = currentGravity * fixedDeltaTime;

            float currentVerticalVelocity = GetVerticalSpeed();
            var newVerticalVelocity = currentVerticalVelocity + gravityVelocity;

            // 터미널 속도 제한
            if (newVerticalVelocity < _terminalVelocity)
            {
                gravityVelocity = _terminalVelocity - currentVerticalVelocity;
            }

            RequestVelocity(VelocityRequest.AddVertical(gravityVelocity));
        }

        /// <summary>중력 활성화/비활성화</summary>
        public void SetGravityEnabled(bool enabled)
        {
            _gravityEnabled = enabled;
        }

        /// <summary>현재 중력값 반환</summary>
        public float GetCurrentGravity()
        {
            return _gravityEnabled ? _normalGravity : 0f;
        }

        /// <summary>빠른 낙하 - 즉시 터미널 속도로 설정</summary>
        public void FastFall()
        {
            RequestVelocity(VelocityRequest.SetVertical(_terminalVelocity, VelocityPriority.Override));
        }

        #endregion

        #region Velocity Request System

        /// <summary>
        /// 속도 요청 추가 - 다음 프레임에 처리됨
        /// </summary>
        public void RequestVelocity(VelocityRequest request)
        {
            _pendingRequests.Add(request);
        }

        /// <summary>
        /// 지속적인 힘 설정 - 매 프레임마다 적용됨
        /// </summary>
        public void SetForce(ForceType type, Vector2 force)
        {
            _activeForces[type] = VelocityRequest.Add(force, type);
        }

        /// <summary>
        /// 지속적인 힘 제거
        /// </summary>
        public void RemoveForce(ForceType type)
        {
            _activeForces.Remove(type);
        }

        /// <summary>
        /// 속도 요청들을 처리하여 최종 속도 계산
        /// 우선순위: Override > Set > Add > ActiveForces
        /// </summary>
        private void ProcessVelocityRequests()
        {
            Vector2 finalVelocity = _rigidbody2D.linearVelocity;

            // 임시 리스트 초기화
            _setRequests.Clear();
            _addRequests.Clear();

            // Override 요청 찾기
            VelocityRequest? overrideRequest = FindHighestPriorityOverride();

            // 요청들을 타입별로 분류
            ClassifyRequests();

            // Override가 있으면 우선 처리
            if (overrideRequest.HasValue)
            {
                ApplyOverrideRequest(ref finalVelocity, overrideRequest.Value);
            }
            else
            {
                // Set 요청들 처리 (우선순위 순)
                ApplySetRequests(ref finalVelocity);

                // Add 요청들 처리
                ApplyAddRequests(ref finalVelocity);

                // 지속적인 힘들 처리
                ApplyActiveForces(ref finalVelocity);
            }

            // 제약 조건 및 속도 제한 적용
            ApplyConstraints(ref finalVelocity);

            // 최종 속도 적용
            _rigidbody2D.linearVelocity = finalVelocity;
            _pendingRequests.Clear();

            // 이벤트 체크 (변화가 있을 때만)
            CheckVelocityEventsIfChanged();
        }

        /// <summary>
        /// 가장 높은 우선순위의 Override 요청 찾기
        /// </summary>
        private VelocityRequest? FindHighestPriorityOverride()
        {
            VelocityRequest? overrideRequest = null;
            int highestOverridePriority = VelocityPriority.Background;

            for (int i = 0; i < _pendingRequests.Count; i++)
            {
                var request = _pendingRequests[i];
                if (request.RequestType == VelocityRequestType.Override &&
                    request.Priority >= highestOverridePriority)
                {
                    overrideRequest = request;
                    highestOverridePriority = request.Priority;
                }
            }

            return overrideRequest;
        }

        /// <summary>
        /// 요청들을 타입별로 분류
        /// </summary>
        private void ClassifyRequests()
        {
            for (int i = 0; i < _pendingRequests.Count; i++)
            {
                var request = _pendingRequests[i];

                switch (request.RequestType)
                {
                    case VelocityRequestType.Set:
                        _setRequests.Add(request);
                        break;
                    case VelocityRequestType.Add:
                        _addRequests.Add(request);
                        break;
                        // Override는 이미 처리됨
                }
            }
        }

        /// <summary>
        /// Override 요청 적용
        /// </summary>
        private void ApplyOverrideRequest(ref Vector2 finalVelocity, VelocityRequest request)
        {
            if (request.AffectsX)
            {
                finalVelocity.x = request.Velocity.x;
            }

            if (request.AffectsY)
            {
                finalVelocity.y = request.Velocity.y;
            }
        }

        /// <summary>
        /// Set 요청들 적용 (우선순위 순으로 X, Y 각각 처리)
        /// </summary>
        private void ApplySetRequests(ref Vector2 finalVelocity)
        {
            if (_setRequests.Count == 0) return;

            SortByPriorityDescending(_setRequests);

            // X와 Y 각각 가장 높은 우선순위만 적용
            bool xSet = false, ySet = false;

            for (int i = 0; i < _setRequests.Count && (!xSet || !ySet); i++)
            {
                var request = _setRequests[i];

                if (!xSet && request.AffectsX)
                {
                    finalVelocity.x = request.Velocity.x;
                    xSet = true;
                }

                if (!ySet && request.AffectsY)
                {
                    finalVelocity.y = request.Velocity.y;
                    ySet = true;
                }
            }
        }

        /// <summary>
        /// Add 요청들 적용
        /// </summary>
        private void ApplyAddRequests(ref Vector2 finalVelocity)
        {
            for (int i = 0; i < _addRequests.Count; i++)
            {
                var request = _addRequests[i];

                if (request.AffectsX)
                {
                    finalVelocity.x += request.Velocity.x;
                }

                if (request.AffectsY)
                {
                    finalVelocity.y += request.Velocity.y;
                }
            }
        }

        /// <summary>
        /// 지속적인 힘들 적용
        /// </summary>
        private void ApplyActiveForces(ref Vector2 finalVelocity)
        {
            foreach (var force in _activeForces.Values)
            {
                if (force.AffectsX)
                {
                    finalVelocity.x += force.Velocity.x;
                }

                if (force.AffectsY)
                {
                    finalVelocity.y += force.Velocity.y;
                }
            }
        }

        /// <summary>
        /// 우선순위 기준 내림차순 정렬
        /// </summary>
        private static void SortByPriorityDescending(List<VelocityRequest> list)
        {
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        #endregion

        #region Constraints & Limits

        /// <summary>
        /// 제약 조건 및 속도 제한 적용
        /// </summary>
        protected virtual void ApplyConstraints(ref Vector2 velocity)
        {
            // 축 잠금 적용
            if (_lockHorizontal)
            {
                velocity.x = 0;
            }

            if (_lockVertical)
            {
                velocity.y = 0;
            }

            // 최대 속도 제한 - PhysicsUtility 사용
            velocity = PhysicsUtility.ClampHorizontalVelocity(velocity, _physicsSettings.MaxHorizontalSpeed);
            velocity = PhysicsUtility.ClampVerticalVelocity(velocity, _physicsSettings.MaxVerticalSpeed);
        }

        /// <summary>
        /// 수평 이동 잠금/해제
        /// </summary>
        public void LockHorizontalMovement(bool locked)
        {
            _lockHorizontal = locked;
            if (locked)
            {
                _rigidbody2D.linearVelocityX = 0f;
            }
        }

        /// <summary>
        /// 수직 이동 잠금/해제
        /// </summary>
        public void LockVerticalMovement(bool locked)
        {
            _lockVertical = locked;
            if (locked)
            {
                _rigidbody2D.linearVelocityY = 0f;
            }
        }

        #endregion

        #region Quick Actions

        /// <summary>
        /// 점프 - 수직 속도를 설정
        /// </summary>
        public void Jump(float jumpSpeed)
        {
            RequestVelocity(VelocityRequest.SetVertical(jumpSpeed));
        }

        /// <summary>
        /// 이동 - 수평 속도를 지속적으로 적용
        /// </summary>
        public void Move(float horizontalSpeed)
        {
            // PhysicsUtility 사용하여 속도 유효성 검사
            if (PhysicsUtility.HasValidVelocity(horizontalSpeed))
            {
                SetForce(ForceType.Movement, Vector2.right * horizontalSpeed);
            }
            else
            {
                RemoveForce(ForceType.Movement);
                RequestVelocity(VelocityRequest.SetHorizontal(0f));
            }
        }

        /// <summary>
        /// 대시 - 방향과 속도로 Override
        /// </summary>
        public void Dash(Vector2 direction, float speed)
        {
            RequestVelocity(VelocityRequest.Override(direction.normalized * speed, ForceType.Dash));
        }

        /// <summary>
        /// 넉백 - 방향과 힘으로 Override
        /// </summary>
        public void Knockback(Vector2 direction, float force)
        {
            RequestVelocity(VelocityRequest.Override(direction.normalized * force, ForceType.Knockback));
        }

        /// <summary>
        /// 완전 정지 - 모든 속도를 0으로 설정
        /// </summary>
        public void Stop()
        {
            RequestVelocity(VelocityRequest.Set(Vector2.zero, ForceType.None, VelocityPriority.Override));
        }

        /// <summary>
        /// 점진적 정지 - 현재 속도를 감소시킴
        /// </summary>
        public void SlowDown(float deceleration, float deltaTime)
        {
            var currentVelocity = GetVelocity();
            var targetVelocity = Vector2.zero;
            var newVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, deceleration * deltaTime);

            // 거의 정지했으면 완전히 정지
            if (PhysicsUtility.IsNearlyStationary(newVelocity))
            {
                Stop();
            }
            else
            {
                RequestVelocity(VelocityRequest.Set(newVelocity));
            }
        }

        #endregion

        #region Getters

        /// <summary>현재 속도 벡터 반환</summary>
        public Vector2 GetVelocity() => _rigidbody2D.linearVelocity;

        /// <summary>현재 수평 속도 반환</summary>
        public float GetHorizontalSpeed() => _rigidbody2D.linearVelocityX;

        /// <summary>현재 수직 속도 반환</summary>
        public float GetVerticalSpeed() => _rigidbody2D.linearVelocityY;

        /// <summary>움직이고 있는지 여부 - PhysicsUtility 사용</summary>
        public bool IsCurrentlyMoving() => PhysicsUtility.IsMoving(_rigidbody2D.linearVelocity);

        /// <summary>거의 정지 상태인지 여부 - PhysicsUtility 사용</summary>
        public bool IsCurrentlyNearlyStationary() => PhysicsUtility.IsNearlyStationary(_rigidbody2D.linearVelocity);

        /// <summary>떨어지고 있는지 여부 - PhysicsUtility 사용</summary>
        public bool IsCurrentlyFalling() => PhysicsUtility.IsFalling(_rigidbody2D.linearVelocity);

        /// <summary>상승하고 있는지 여부 - PhysicsUtility 사용</summary>
        public bool IsCurrentlyRising() => PhysicsUtility.IsRising(_rigidbody2D.linearVelocity);

        /// <summary>속도 방향 반환 - PhysicsUtility 사용</summary>
        public int GetHorizontalDirection() => PhysicsUtility.GetVelocityDirection(GetHorizontalSpeed());

        /// <summary>이전 프레임과 속도가 변화했는지 확인</summary>
        public bool HasVelocityChanged() => PhysicsUtility.VelocityChanged(_rigidbody2D.linearVelocity, _lastVelocity);

        /// <summary>현재 속도가 같은 방향인지 확인</summary>
        public bool IsSameDirection(Vector2 otherVelocity) =>
            PhysicsUtility.SameDirection(_rigidbody2D.linearVelocity, otherVelocity);

        #endregion

        #region Events

        /// <summary>
        /// 속도 변화가 있을 때만 이벤트 체크 (성능 최적화) - PhysicsUtility 사용
        /// </summary>
        private void CheckVelocityEventsIfChanged()
        {
            var currentVelocity = _rigidbody2D.linearVelocity;

            // PhysicsUtility를 사용하여 속도 변화 감지
            if (PhysicsUtility.VelocityChanged(currentVelocity, _lastVelocity))
            {
                _velocity.OnNext(currentVelocity);
                _horizontalVelocity.OnNext(currentVelocity.x);
                _verticalVelocity.OnNext(currentVelocity.y);

                // PhysicsUtility를 사용하여 상태 판정
                _isRising.OnNext(PhysicsUtility.IsRising(currentVelocity));
                _isFalling.OnNext(PhysicsUtility.IsFalling(currentVelocity));
                _isMoving.OnNext(PhysicsUtility.IsMoving(currentVelocity));
                _isNearlyStationary.OnNext(PhysicsUtility.IsNearlyStationary(currentVelocity));

                _lastVelocity = currentVelocity;
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            _velocity?.Dispose();
            _horizontalVelocity?.Dispose();
            _verticalVelocity?.Dispose();
            _isRising?.Dispose();
            _isFalling?.Dispose();
            _isMoving?.Dispose();
            _isNearlyStationary?.Dispose();
        }

        #endregion
    }
}
