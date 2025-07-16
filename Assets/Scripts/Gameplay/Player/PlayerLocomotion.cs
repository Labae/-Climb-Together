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

namespace Gameplay.Player
{
    public class PlayerLocomotion : IDisposable
    {
        private readonly List<IPlayerLocomotion> _locomotions;
        private readonly Subject<IPlayerLocomotion> _onLocomotionExecuted = new();

        private float _lastValidInputTime;
        private float _lastValidInput;
        private readonly CompositeDisposable _disposables = new();

        public Observable<IPlayerLocomotion> OnLocomotionExecuted => _onLocomotionExecuted.AsObservable();

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

            movementInput.Subscribe(OnMovementInput).AddTo(_disposables);
        }

        private void OnMovementInput(float horizontalInput)
        {
            ProcessMovement(horizontalInput);
        }

        private void ProcessMovement(float input)
        {
            foreach (var locomotion in _locomotions)
            {
                if (!locomotion.CanExecute(input))
                {
                    continue;
                }

                locomotion.Execute(input);
                _onLocomotionExecuted.OnNext(locomotion);
                GameLogger.Debug(ZString.Concat("Locomotion executed: ", locomotion.GetName()), LogCategory.Player);
                return;
            }
        }

        public void Dispose()
        {
            foreach (var locomotionAction in _locomotions)
            {
                locomotionAction.Dispose();
            }
            _onLocomotionExecuted.Dispose();
            _disposables.Dispose();
        }
    }
}
