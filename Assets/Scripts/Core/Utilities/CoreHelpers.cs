using System;
using UnityEngine;

namespace Core.Utilities
{
    public static class CoreHelpers
    {
        public static T FindComponentInChildren<T>(Transform parent, string name = null) where T : Component
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            foreach (Transform child in parent)
            {
                if (string.IsNullOrEmpty(name) || child.name == name)
                {
                    var component = child.GetComponent<T>();
                    if (component != null) return component;
                }

                var deepResult = FindComponentInChildren<T>(child, name);
                if (deepResult != null) return deepResult;
            }

            return null;
        }
    }
}
