using System;
using Core.Behaviours;
using Gameplay.Common.Interfaces;
using R3;
using UnityEngine;

namespace Gameplay.Common
{
    [Serializable]
    public class GroundChecker : CoreBehaviour, IGroundChecker
    {
        [Header("Detection Settings")]
        [SerializeField, Min(0.1f)]
        private float _width = 1f;

        [SerializeField, Min(0.01f)] private float _distance = 0.05f;

        [SerializeField, Min(3)] private int _rayCount = 4;

        [SerializeField, Min(2)] private int _capacity = 4;

        [SerializeField] private LayerMask _groundLayerMask;

        [Header("Detection Accuracy")]
        [SerializeField, Range(0.1f, 1f), Tooltip("최소 몇 프로의 레이가 땅에 닿아야 접지로 판정할지")]
        private float _groundThreshold = 0.5f;

        [Header("Optimization")]
        [SerializeField, Tooltip("FixedUpdate 대신 수동 체크를 사용할지")]
        private bool _useManualCheck = false;

        private ContactFilter2D _contactFilter2D;
        private RaycastHit2D[] _hitResults;

        // 성능 최적화용 재사용 변수
        private Vector2 _rayStart = Vector2.zero;
        private Vector2 _rayDirection = Vector2.down;

        private ReactiveProperty<bool> _isGrounded = new(false);
        private bool _wasGroundedLastFrame;

        // 캐싱된 값들
        private Vector2 _lastCheckedPosition;
        private bool _positionChanged;

        private readonly Subject<Unit> _onGroundEntered = new();
        private readonly Subject<Unit> _onGroundExited = new();

        public Observable<Unit> OnGroundEntered => _onGroundEntered.AsObservable();
        public Observable<Unit> OnGroundExited => _onGroundExited.AsObservable();

        public ReadOnlyReactiveProperty<bool> IsGrounded => _isGrounded.ToReadOnlyReactiveProperty();
        public bool WasGroundedLastFrame => _wasGroundedLastFrame;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _contactFilter2D = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundLayerMask,
                useTriggers = false
            };

            _hitResults = new RaycastHit2D[_capacity];
            _wasGroundedLastFrame = false;
            _lastCheckedPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_useManualCheck) return;

            CheckGroundState();
        }

        /// <summary>
        /// 수동으로 접지 상태를 체크합니다.
        /// </summary>
        public void CheckGroundState()
        {
            Vector2 currentPosition = transform.position;

            // 위치가 변경되었거나 강제 체크인 경우에만 실행
            _positionChanged = Vector2.Distance(currentPosition, _lastCheckedPosition) > 0.001f;

            if (_positionChanged || !_useManualCheck)
            {
                Check(currentPosition, _distance);
                _lastCheckedPosition = currentPosition;
            }
        }

        /// <summary>
        /// 특정 위치에서 접지 상태를 체크합니다.
        /// </summary>
        /// <param name="origin">체크할 위치</param>
        /// <param name="distance">체크할 거리</param>
        public void CheckAtPosition(Vector2 origin, float distance = -1)
        {
            if (distance < 0) distance = _distance;
            Check(origin, distance);
        }

        private void Check(Vector2 origin, float distance)
        {
            if (_hitResults == null || distance <= 0) return;

            var halfWidth = _width * 0.5f;
            var startX = origin.x - halfWidth;
            var endX = origin.x + halfWidth;

            int groundHitCount = 0;
            int totalRays = _rayCount + 1; // 0부터 _rayCount까지이므로 +1

            for (var i = 0; i < totalRays; i++)
            {
                var t = (float)i / (_rayCount); // 단일 레이인 경우 중앙에
                var rayX = Mathf.Lerp(startX, endX, t);

                // Vector2 재사용으로 GC 방지
                _rayStart.x = rayX;
                _rayStart.y = origin.y;

                int hitCount = Physics2D.Raycast(_rayStart, _rayDirection,
                    _contactFilter2D, _hitResults, distance);

                if (hitCount > 0)
                {
                    groundHitCount++;
                }
            }

            // 임계값 기반 접지 판정
            bool foundGround = groundHitCount >= Mathf.CeilToInt(totalRays * _groundThreshold);

            UpdateGroundState(foundGround);
        }

        private void UpdateGroundState(bool foundGround)
        {
            _wasGroundedLastFrame = _isGrounded.Value;

            // 상태가 실제로 변경된 경우에만 업데이트
            if (_isGrounded.Value != foundGround)
            {
                _isGrounded.OnNext(foundGround);
                HandleEvents();
            }
        }

        private void HandleEvents()
        {
            if (_isGrounded.Value && !_wasGroundedLastFrame)
            {
                _onGroundEntered.OnNext(Unit.Default);
            }
            else if (!_isGrounded.Value && _wasGroundedLastFrame)
            {
                _onGroundExited.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// 현재 접지 상태를 즉시 반환합니다 (캐시된 값)
        /// </summary>
        public bool IsCurrentlyGrounded() => _isGrounded.Value;

        /// <summary>
        /// 접지 상태를 강제로 설정합니다 (디버그용)
        /// </summary>
        public void ForceGroundState(bool grounded)
        {
            UpdateGroundState(grounded);
        }

        protected override void HandleDestruction()
        {
            _onGroundEntered?.Dispose();
            _onGroundExited?.Dispose();
            _isGrounded?.Dispose();
            base.HandleDestruction();
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var pos = Application.isPlaying ? transform.position : (Vector3)transform.position;
            var halfWidth = _width * 0.5f;
            var startX = pos.x - halfWidth;
            var endX = pos.x + halfWidth;

            // 전체 체크 영역 표시
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            var topLeft = new Vector3(startX, pos.y, 0);
            var topRight = new Vector3(endX, pos.y, 0);
            var bottomLeft = new Vector3(startX, pos.y - _distance, 0);
            var bottomRight = new Vector3(endX, pos.y - _distance, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomLeft, bottomRight);

            // 개별 레이들
            int totalRays = _rayCount + 1;
            for (var i = 0; i < totalRays; i++)
            {
                var t = totalRays == 1 ? 0.5f : (float)i / _rayCount;
                var rayX = Mathf.Lerp(startX, endX, t);
                var rayStart = new Vector3(rayX, pos.y, 0);
                var rayEnd = new Vector3(rayX, pos.y - _distance, 0);

                Gizmos.color = Application.isPlaying && _isGrounded.Value ? Color.green : Color.red;
                Gizmos.DrawLine(rayStart, rayEnd);
                Gizmos.DrawWireSphere(rayStart, 0.02f);
            }

            // 임계값 정보 표시
            if (Application.isPlaying)
            {
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.5f,
                    $"Grounded: {_isGrounded.Value}\nThreshold: {_groundThreshold:F1}", style);
            }
        }

        private void OnValidate()
        {
            // 값 검증 및 보정
            _rayCount = Mathf.Max(1, _rayCount);
            _width = Mathf.Max(0.1f, _width);
            _distance = Mathf.Max(0.01f, _distance);
            _capacity = Mathf.Max(1, _capacity);
            _groundThreshold = Mathf.Clamp(_groundThreshold, 0.1f, 1f);
        }
#endif
    }
}
