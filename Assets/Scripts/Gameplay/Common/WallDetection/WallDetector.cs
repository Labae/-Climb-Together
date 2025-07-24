using System;
using Core.Behaviours;
using Cysharp.Text;
using Debugging;
using Debugging.Enum;
using Gameplay.Common.Enums;
using Gameplay.Common.Interfaces;
using R3;
using Systems.Animations;
using UnityEngine;

namespace Gameplay.Common.WallDetection
{
    [Serializable]
    public class WallDetector : CoreBehaviour, IWallDetector
    {
        #region Inspector Fields

        [Header("Detection Settings")] [SerializeField, Min(0.1f)]
        private float _height = 1f;

        [SerializeField, Min(0.01f)] private float _distance = 0.1f;

        [SerializeField, Min(3)] private int _rayCount = 4;

        [SerializeField, Min(2)] private int _capacity = 4;

        [SerializeField] private LayerMask _wallLayerMask;

        [Header("Detection Accuracy")] [SerializeField, Range(0.1f, 1f), Tooltip("최소 몇 프로의 레이가 벽에 닿아야 벽 감지로 판정할지")]
        private float _wallThreshold = 0.5f;

        [Header("Direction Settings")] [SerializeField, Tooltip("고정된 방향으로 체크할지 (false면 플레이어 바라보는 방향)")]
        private bool _useFixedDirection = false;

        [SerializeField, Tooltip("고정 방향 (useFixedDirection이 true일 때만 사용)")]
        private FacingDirection _fixedDirection = FacingDirection.Right;

        [Header("Optimization")] [SerializeField, Tooltip("FixedUpdate 대신 수동 체크를 사용할지")]
        private bool _useManualCheck = false;

        [Header("Debug Options")] [SerializeField]
        private bool _enableDetailedLogging = false;

        #endregion

        #region Private Fields

        private ContactFilter2D _contactFilter2D;
        private RaycastHit2D[] _hitResults;

        // 성능 최적화용 재사용 변수
        private Vector2 _rayStart = Vector2.zero;
        private FacingDirection _currentDirection = FacingDirection.Right;

        private ReactiveProperty<bool> _isWallDetected = new(false);
        private ReactiveProperty<WallSideType> _wallSide = new(WallSideType.Right);
        private ReactiveProperty<Vector2> _wallNormal = new(Vector2.zero);
        private bool _wasWallDetectedLastFrame;

        // 캐싱된 값들
        private Vector2 _lastCheckedPosition;
        private FacingDirection _lastCheckedDirection;
        private bool _positionOrDirectionChanged;

        private readonly Subject<Unit> _onWallEntered = new();
        private readonly Subject<Unit> _onWallExited = new();

        // 플레이어 방향 제공자
        private IDirectionProvider _directionProvider;

        #endregion

        #region Properties

        public Observable<Unit> OnWallEntered => _onWallEntered.AsObservable();
        public Observable<Unit> OnWallExited => _onWallExited.AsObservable();

        public ReadOnlyReactiveProperty<bool> IsWallDetected => _isWallDetected.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<WallSideType> WallSide => _wallSide.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<Vector2> WallNormal => _wallNormal.ToReadOnlyReactiveProperty();
        public bool WasWallDetectedLastFrame => _wasWallDetectedLastFrame;

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();

            try
            {
                _contactFilter2D = new ContactFilter2D
                {
                    useLayerMask = true, layerMask = _wallLayerMask, useTriggers = false
                };

                _hitResults = new RaycastHit2D[_capacity];
                _wasWallDetectedLastFrame = false;
                _lastCheckedPosition = transform.position;
                _lastCheckedDirection = _useFixedDirection ? _fixedDirection : FacingDirection.Right;

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("WallChecker initialized", LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Failed to initialize WallChecker: ", e.Message), LogCategory.Player);
                throw;
            }
        }

        #endregion

        #region Update

        private void FixedUpdate()
        {
            if (!IsInitialized || _useManualCheck) return;

            try
            {
                CheckWallState();
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error in WallChecker FixedUpdate: ", e.Message), LogCategory.Player);
            }
        }

        #endregion

        #region Direction Provider

        /// <summary>
        /// 플레이어 방향 제공자를 설정합니다.
        /// </summary>
        /// <param name="directionProvider">플레이어 방향 제공자</param>
        public void SetDirectionProvider(IDirectionProvider directionProvider)
        {
            _directionProvider = directionProvider;

            // 방향 변경 이벤트 구독 (선택적 최적화)
            if (_directionProvider != null)
            {
                var d = Disposable.CreateBuilder();
                _directionProvider.OnDirectionChanged
                    .Subscribe(_ =>
                    {
                        if (!_useManualCheck)
                        {
                            CheckWallState();
                        }
                    })
                    .AddTo(ref d);
                d.RegisterTo(destroyCancellationToken);

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("Direction provider set and subscribed", LogCategory.Player);
                }
            }
        }

        #endregion

        #region Wall Detection

        /// <summary>
        /// 수동으로 벽 감지 상태를 체크합니다.
        /// </summary>
        public void CheckWallState()
        {
            if (!IsInitialized) return;

            Vector2 currentPosition = transform.position;
            FacingDirection currentDirection = GetCurrentDirection();

            // 위치나 방향이 변경되었거나 강제 체크인 경우에만 실행
            _positionOrDirectionChanged = Vector2.Distance(currentPosition, _lastCheckedPosition) > 0.001f ||
                                          currentDirection != _lastCheckedDirection;

            if (_positionOrDirectionChanged || !_useManualCheck)
            {
                Check(currentPosition, currentDirection, _distance);
                _lastCheckedPosition = currentPosition;
                _lastCheckedDirection = currentDirection;
            }
        }

        /// <summary>
        /// 특정 위치와 방향에서 벽 감지 상태를 체크합니다.
        /// </summary>
        /// <param name="origin">체크할 위치</param>
        /// <param name="direction">체크할 방향</param>
        /// <param name="distance">체크할 거리</param>
        public void CheckAtPosition(Vector2 origin, FacingDirection direction, float distance = -1)
        {
            if (!IsInitialized) return;

            if (distance < 0) distance = _distance;
            Check(origin, direction, distance);
        }

        /// <summary>
        /// 양쪽 벽을 모두 체크합니다.
        /// </summary>
        /// <param name="origin">체크할 위치</param>
        /// <param name="distance">체크할 거리</param>
        /// <returns>양쪽 벽 감지 결과</returns>
        public WallDetectionResult CheckBothSides(Vector2 origin, float distance = -1)
        {
            if (!IsInitialized)
                return new WallDetectionResult(false, false);

            if (distance < 0) distance = _distance;

            var leftHit = CheckDirection(origin, Vector2.left, distance);
            var rightHit = CheckDirection(origin, Vector2.right, distance);

            return new WallDetectionResult(leftHit, rightHit);
        }

        private FacingDirection GetCurrentDirection()
        {
            if (_useFixedDirection)
                return _fixedDirection;

            if (_directionProvider != null)
                return _directionProvider.CurrentDirection;

            // 기본값으로 오른쪽 방향 반환
            return FacingDirection.Right;
        }

        private void Check(Vector2 origin, FacingDirection direction, float distance)
        {
            if (_hitResults == null || distance <= 0) return;

            _currentDirection = direction;
            Vector2 rayDirection = direction == FacingDirection.Right ? Vector2.right : Vector2.left;
            bool wallDetected = CheckDirection(origin, rayDirection, distance);

            UpdateWallState(wallDetected, direction);
        }

        private bool CheckDirection(Vector2 origin, Vector2 direction, float distance)
        {
            var halfHeight = _height * 0.5f;
            var startY = origin.y - halfHeight;
            var endY = origin.y + halfHeight;

            int wallHitCount = 0;
            int totalRays = _rayCount + 1;
            Vector2 bestNormal = Vector2.zero;
            float shortestDistance = float.MaxValue;

            for (var i = 0; i < totalRays; i++)
            {
                var t = totalRays == 1 ? 0.5f : (float)i / _rayCount;
                var rayY = Mathf.Lerp(startY, endY, t);

                _rayStart.x = origin.x;
                _rayStart.y = rayY;

                int hitCount = Physics2D.Raycast(_rayStart, direction,
                    _contactFilter2D, _hitResults, distance);

                if (hitCount > 0)
                {
                    wallHitCount++;

                    // 가장 가까운 벽의 법선 벡터 저장
                    for (int j = 0; j < hitCount; j++)
                    {
                        if (_hitResults[j].distance < shortestDistance)
                        {
                            shortestDistance = _hitResults[j].distance;
                            bestNormal = _hitResults[j].normal;
                        }
                    }
                }
            }

            // 벽이 감지되었으면 법선 벡터 업데이트
            if (wallHitCount >= Mathf.CeilToInt(totalRays * _wallThreshold))
            {
                _wallNormal.OnNext(bestNormal);
                return true;
            }

            return false;
        }

        private void UpdateWallState(bool wallDetected, FacingDirection direction)
        {
            _wasWallDetectedLastFrame = _isWallDetected.Value;

            if (_isWallDetected.Value != wallDetected)
            {
                _isWallDetected.OnNext(wallDetected);

                if (wallDetected)
                {
                    _wallSide.OnNext(direction == FacingDirection.Right ? WallSideType.Right : WallSideType.Left);
                }
                else
                {
                    _wallNormal.OnNext(Vector2.zero);
                }

                HandleEvents();

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug(ZString.Concat("Wall detection changed: ", wallDetected,
                        wallDetected ? ZString.Concat(" (Side: ", _wallSide.Value, ")") : ""), LogCategory.Player);
                }
            }
        }

        private void HandleEvents()
        {
            if (_isWallDetected.Value && !_wasWallDetectedLastFrame)
            {
                _onWallEntered.OnNext(Unit.Default);
            }
            else if (!_isWallDetected.Value && _wasWallDetectedLastFrame)
            {
                _onWallExited.OnNext(Unit.Default);
            }
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// 현재 벽 감지 상태를 즉시 반환합니다.
        /// </summary>
        public bool IsCurrentlyDetectingWall() => _isWallDetected.Value;

        /// <summary>
        /// 벽 감지 상태를 강제로 설정합니다 (디버그용).
        /// </summary>
        public void ForceWallState(bool detected)
        {
            UpdateWallState(detected, _currentDirection);

            if (_enableDetailedLogging)
            {
                GameLogger.Debug(ZString.Concat("Wall state forced to: ", detected), LogCategory.Player);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var pos = Application.isPlaying ? transform.position : (Vector3)transform.position;
            var direction = GetCurrentDirection();
            var rayDirection = direction == FacingDirection.Right ? Vector2.right : Vector2.left;
            var halfHeight = _height * 0.5f;

            // 체크 방향 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, rayDirection * _distance);

            // 체크 영역 표시
            var startY = pos.y - halfHeight;
            var endY = pos.y + halfHeight;

            Gizmos.color = new Color(1, 0, 1, 0.3f);
            var topStart = new Vector3(pos.x, endY, 0);
            var topEnd = new Vector3(pos.x + rayDirection.x * _distance, endY, 0);
            var bottomStart = new Vector3(pos.x, startY, 0);
            var bottomEnd = new Vector3(pos.x + rayDirection.x * _distance, startY, 0);

            Gizmos.DrawLine(topStart, topEnd);
            Gizmos.DrawLine(bottomStart, bottomEnd);
            Gizmos.DrawLine(topStart, bottomStart);
            Gizmos.DrawLine(topEnd, bottomEnd);

            // 개별 레이들
            int totalRays = _rayCount + 1;
            for (var i = 0; i < totalRays; i++)
            {
                var t = totalRays == 1 ? 0.5f : (float)i / _rayCount;
                var rayY = Mathf.Lerp(startY, endY, t);
                var rayStart = new Vector3(pos.x, rayY, 0);
                var rayEnd = new Vector3(pos.x + rayDirection.x * _distance, rayY, 0);

                Gizmos.color = Application.isPlaying && _isWallDetected.Value ? Color.red : Color.green;
                Gizmos.DrawLine(rayStart, rayEnd);
                Gizmos.DrawWireSphere(rayStart, 0.02f);
            }

            // 벽 법선 벡터 표시
            if (Application.isPlaying && _isWallDetected.Value)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(pos, _wallNormal.Value * 0.5f);

                // 방향 정보 표시
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.5f,
                    GetWallDebugInfo(), style);
            }
        }

        /// <summary>
        /// 디버그용 벽 감지 정보
        /// </summary>
        public string GetWallDebugInfo()
        {
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine("=== Wall Checker Debug Info ===");
            sb.AppendLine(ZString.Concat("Wall Detected: ", _isWallDetected.Value));
            sb.AppendLine(ZString.Concat("Wall Side: ", _wallSide.Value));
            sb.AppendLine(ZString.Concat("Direction: ", _currentDirection));
            sb.AppendLine(ZString.Concat("Use Fixed Direction: ", _useFixedDirection));
            sb.AppendLine(ZString.Concat("Manual Check: ", _useManualCheck));
            sb.AppendLine(ZString.Concat("Ray Count: ", _rayCount));
            sb.AppendLine(ZString.Concat("Detection Distance: ", _distance.ToString("F2")));
            sb.Append(ZString.Concat("Wall Normal: ", _wallNormal.Value.x.ToString("F2"), ", ",
                _wallNormal.Value.y.ToString("F2")));
            return sb.ToString();
        }
#endif

        #endregion

        #region Destruction

        protected override void HandleDestruction()
        {
            try
            {
                _onWallEntered?.Dispose();
                _onWallExited?.Dispose();
                _isWallDetected?.Dispose();
                _wallSide?.Dispose();
                _wallNormal?.Dispose();

                if (_enableDetailedLogging)
                {
                    GameLogger.Debug("WallChecker disposed", LogCategory.Player);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error(ZString.Concat("Error during WallChecker destruction: ", e.Message),
                    LogCategory.Player);
            }
            finally
            {
                base.HandleDestruction();
            }
        }

        public void Dispose()
        {
            if (this != null)
            {
                HandleDestruction();
            }
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            _rayCount = Mathf.Max(1, _rayCount);
            _height = Mathf.Max(0.1f, _height);
            _distance = Mathf.Max(0.01f, _distance);
            _capacity = Mathf.Max(1, _capacity);
            _wallThreshold = Mathf.Clamp(_wallThreshold, 0.1f, 1f);
        }

        #endregion
    }
}
