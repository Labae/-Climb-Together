using System;
using System.Collections.Generic;
using Systems.Physics.Enums;
using Systems.Physics.Interfaces;
using UnityEngine;

namespace Systems.Physics
{
    public abstract class PhysicsControllerBase : IPhysicsController
    {
        protected readonly Rigidbody2D _rigidbody2D;
        
        private readonly List<VelocityRequest> _pendingRequests = new();
        private readonly Dictionary<ForceType, VelocityRequest> _activeForces = new();
        
        // 임시 리스트들 (GC 방지)
        private readonly List<VelocityRequest> _setRequests = new();
        private readonly List<VelocityRequest> _addRequests = new();
        
        // 제약 조건들
        private bool _lockHorizontal = false;
        private bool _lockVertical = false;
        
        // 상태
        private Vector2 _lastVelocity;
        
        // 이벤트
        public event Action<float> OnHorizontalVelocityChanged;
        public event Action<float> OnVerticalVelocityChanged;
        
        protected PhysicsControllerBase(Rigidbody2D rigidbody2D)
        {
            _rigidbody2D = rigidbody2D;
        }
        
        public void PhysicsUpdate()
        {
            ProcessVelocityRequests();
            _lastVelocity = _rigidbody2D.linearVelocity;
        }
        
         #region Request System
        
        public void RequestVelocity(VelocityRequest request)
        {
            _pendingRequests.Add(request);
        }
        
        public void SetForce(ForceType type, Vector2 force)
        {
            _activeForces[type] = VelocityRequest.Add(force, type);
        }
        
        public void RemoveForce(ForceType type)
        {
            _activeForces.Remove(type);
        }
        
        public void ProcessVelocityRequests()
        {
            Vector2 finalVelocity = _rigidbody2D.linearVelocity;
            
            // 리스트들 초기화
            _setRequests.Clear();
            _addRequests.Clear();
            
            VelocityRequest? overrideRequest = null;
            int highestOverridePriority = VelocityPriority.Background;
            
            // 한 번의 루프로 모든 요청 분류
            for (int i = 0; i < _pendingRequests.Count; i++)
            {
                var request = _pendingRequests[i];
                
                switch (request.RequestType)
                {
                    case VelocityRequestType.Override:
                        if (request.Priority >= highestOverridePriority)
                        {
                            overrideRequest = request;
                            highestOverridePriority = request.Priority;
                        }
                        break;
                        
                    case VelocityRequestType.Set:
                        _setRequests.Add(request);
                        break;
                        
                    case VelocityRequestType.Add:
                        _addRequests.Add(request);
                        break;
                }
            }
            
            // Override 처리
            if (overrideRequest.HasValue)
            {
                var request = overrideRequest.Value;
                if (request.AffectsX)
                {
                    finalVelocity.x = request.Velocity.x;
                }

                if (request.AffectsY)
                {
                    finalVelocity.y = request.Velocity.y;
                }
            }
            else
            {
                // Set 요청들 처리 (우선순위 정렬)
                if (_setRequests.Count > 0)
                {
                    SortByPriorityDescending(_setRequests);

                    foreach (var request in _setRequests)
                    {
                        // 일반 Set 처리
                        if (request.AffectsX)
                        {
                            finalVelocity.x = request.Velocity.x;
                        }

                        if (request.AffectsY)
                        {
                            finalVelocity.y = request.Velocity.y;
                        }
                    }
                }
                
                // Add 요청들 처리
                foreach (var request in _addRequests)
                {
                    if (request.AffectsX)
                    {
                        finalVelocity.x += request.Velocity.x;
                    }

                    if (request.AffectsY)
                    {
                        finalVelocity.y += request.Velocity.y;
                    }
                }
                
                // 지속적인 힘들 처리
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
            
            // 제약 조건 적용
            ApplyConstraints(ref finalVelocity);
            
            _rigidbody2D.linearVelocity = finalVelocity;
            _pendingRequests.Clear();
            
            CheckVelocityEvents();
        }
        
        private static void SortByPriorityDescending(List<VelocityRequest> list)
        {
            for (var i = 0; i < list.Count - 1; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    if (list[j].Priority > list[i].Priority)
                    {
                        (list[i], list[j]) = (list[j], list[i]);
                    }
                }
            }
        }
        
        #endregion
        
        #region Constraints & Limits
        
        protected virtual void ApplyConstraints(ref Vector2 velocity)
        {
            if (_lockHorizontal)
            {
                velocity.x = 0;
            }

            if (_lockVertical)
            {
                velocity.y = 0;
            }
        }
        
        public void LockHorizontalMovement(bool locked)
        {
            _lockHorizontal = locked;
            if (locked)
            {
                _rigidbody2D.linearVelocityX = 0f;
            }
        }
        
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
        
        public void Jump(float jumpSpeed)
        {
            RequestVelocity(VelocityRequest.SetVertical(jumpSpeed, VelocityPriority.Jump));
        }
        
        public void Move(float horizontalSpeed)
        {
            RequestVelocity(new VelocityRequest(
                VelocityRequestType.Set,
                new Vector2(horizontalSpeed, 0),
                ForceType.None,
                VelocityPriority.Movement,
                0f,
                true,
                false
            ));
        }
        
        public void Dash(Vector2 direction, float speed)
        {
            RequestVelocity(VelocityRequest.Override(direction.normalized * speed, ForceType.Dash));
        }
        
        public void Knockback(Vector2 direction, float force)
        {
            RequestVelocity(VelocityRequest.Override(direction.normalized * force, ForceType.Knockback));
        }
        
        public void Stop()
        {
            RequestVelocity(VelocityRequest.Set(Vector2.zero));
        }
        
        #endregion
        
        #region Getters
        
        public Vector2 GetVelocity()
        {
            return _rigidbody2D.linearVelocity;
        }

        public float GetHorizontalSpeed()
        {
            return _rigidbody2D.linearVelocityX;
        }
        
        public float GetVerticalSpeed()
        {
            return _rigidbody2D.linearVelocityY;
        }
        
        public bool IsMoving()
        {
            return _rigidbody2D.linearVelocity.sqrMagnitude > 0.01f;
        }
        
        #endregion
        
        #region Events
        
        private void CheckVelocityEvents()
        {
            var currentVelocity = _rigidbody2D.linearVelocity;
            
            if (Mathf.Abs(currentVelocity.x - _lastVelocity.x) > 0.1f)
            {
                OnHorizontalVelocityChanged?.Invoke(currentVelocity.x);
            }
            
            if (Mathf.Abs(currentVelocity.y - _lastVelocity.y) > 0.1f)
            {
                OnVerticalVelocityChanged?.Invoke(currentVelocity.y);
            }
        }
        
        #endregion
    }
}