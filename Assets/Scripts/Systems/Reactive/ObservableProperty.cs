using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Reactive
{
    [Serializable]
    public class ObservableProperty<T>
    {
        [SerializeField] private T _value;

        public event Action<T> OnValueChanged;

        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        public ObservableProperty(T value)
        {
            _value = value;
        }

        private void SetValue(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, _value))
            {
                return;
            }

            _value = value;
            OnValueChanged?.Invoke(value);
        }

        public static implicit operator T(ObservableProperty<T> property)
        {
            return property.Value;
        }
    }
}