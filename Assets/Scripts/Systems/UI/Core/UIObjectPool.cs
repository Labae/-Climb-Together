using System.Collections.Generic;
using UnityEngine;

namespace Systems.UI.Core
{
    public class UIObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool = new();
        private readonly List<T> _active = new();

        public UIObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(_prefab, _parent);
            }

            obj.gameObject.SetActive(true);
            _active.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj != null && _active.Remove(obj))
            {
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public void ReturnAll()
        {
            foreach (var obj in _active)
            {
                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    _pool.Enqueue(obj);
                }
            }

            _active.Clear();
        }
    }
}
