using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Extension {
    public static class ComponentExtension {

        public static T GetElseAddComponent<T>(this GameObject go) where T : Component {
            T a = go.GetComponent<T>();
            if(a == null) a = go.AddComponent<T>();
            return a;
        }

    }
}
