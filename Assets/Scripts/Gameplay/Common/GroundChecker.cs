using System;
using Core.Behaviours;
using Gameplay.Common.Interfaces;
using UnityEngine;

namespace Gameplay.Common
{
    [Serializable]
    public class GroundChecker : CoreBehaviour, IGroundChecker
    {
        [Header("Detection Settings")]
        [SerializeField, Min(0.1f)]
        private float _width = 1f;
        
        [SerializeField, Min(0.01f)]
        private float _distance = 0.05f;

        [SerializeField, Min(3)]
        private int _rayCount = 4;
        
        [SerializeField, Min(2)]
        private int _capacity = 4;
        
        [SerializeField] 
        private LayerMask _groundLayerMask;
        
        [Header("Detection Accuracy")]
        [SerializeField, Range(0.1f, 1f), Tooltip("최소 몇 프로의 레이가 땅에 닿아야 접지로 판정할지")]
        private float _groundThreshold = 0.5f;
        
        private ContactFilter2D _contactFilter2D;
        private RaycastHit2D[] _hitResults;
        
        // 성능 최적화용 재사용 변수
        private Vector2 _rayStart = Vector2.zero;
        
        private bool _isGrounded;
        private bool _wasGroundedLastFrame;

        public event Action OnGroundEnter;
        public event Action OnGroundExit;

        public bool IsGrounded => _isGrounded;
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
            _isGrounded = false;
            _wasGroundedLastFrame = false;
        }

        private void FixedUpdate()
        {
            Check(transform.position, _distance);
        }

        private void Check(Vector2 origin, float distance)
        {
            if (_hitResults == null || distance <= 0) return;
            
            var startX = origin.x - _width * 0.5f;
            var endX = origin.x + _width * 0.5f;

            int groundHitCount = 0;
            
            for (var i = 0; i <= _rayCount; i++)
            {
                var t = (float)i / _rayCount;
                var rayX = Mathf.Lerp(startX, endX, t);
                
                // Vector2 재사용으로 GC 방지
                _rayStart.x = rayX;
                _rayStart.y = origin.y;
                
                int size = Physics2D.Raycast(_rayStart, Vector2.down,
                    _contactFilter2D, _hitResults, distance);
                    
                if (size > 0)
                {
                    groundHitCount++;
                }
            }
            
            // 임계값 기반 접지 판정
            bool foundGround = groundHitCount >= (_rayCount + 1) * _groundThreshold;
            
            _wasGroundedLastFrame = _isGrounded;
            _isGrounded = foundGround;

            HandleEvents();
        }

        private void HandleEvents()
        {
            if (_isGrounded && !_wasGroundedLastFrame)
            {
                OnGroundEnter?.Invoke();   
            }
            else if (!_isGrounded && _wasGroundedLastFrame)
            {
                OnGroundExit?.Invoke();
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var pos = Application.isPlaying ? transform.position : (Vector3)transform.position;
            var startX = pos.x - _width * 0.5f;
            var endX = pos.x + _width * 0.5f;

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
            for (var i = 0; i <= _rayCount; i++)
            {
                var t = (float)i / _rayCount;
                var rayX = Mathf.Lerp(startX, endX, t);
                var rayStart = new Vector3(rayX, pos.y, 0);
                var rayEnd = new Vector3(rayX, pos.y - _distance, 0);
                
                Gizmos.color = Application.isPlaying && _isGrounded ? Color.green : Color.red;
                Gizmos.DrawLine(rayStart, rayEnd);
                Gizmos.DrawWireSphere(rayStart, 0.02f);
            }
        }
        
        private void OnValidate()
        {
            if (_rayCount < 1) _rayCount = 1;
            if (_width < 0.1f) _width = 0.1f;
            if (_distance < 0.01f) _distance = 0.01f;
            if (_capacity < 1) _capacity = 1;
            if (_groundThreshold < 0.1f) _groundThreshold = 0.1f;
            if (_groundThreshold > 1f) _groundThreshold = 1f;
        }
        #endif
    }
}