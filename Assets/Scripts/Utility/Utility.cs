using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public static class Component
    {
        public static T GetComponent<T>(this GameObject go, bool bAdd) where T : UnityEngine.Component
        {
            if (go.TryGetComponent<T>(out var component))
            {
                return component;
            }

            if (bAdd)
            {
                component = go.AddComponent<T>();
                return component;
            }

            return null;
        }
    }
}
