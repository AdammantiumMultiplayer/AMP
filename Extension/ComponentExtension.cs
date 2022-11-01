using UnityEngine;

namespace AMP.Extension {
    internal static class ComponentExtension {

        internal static T GetElseAddComponent<T>(this GameObject go) where T : Component {
            T a = go.GetComponent<T>();
            if(a == null) a = go.AddComponent<T>();
            return a;
        }

    }
}
