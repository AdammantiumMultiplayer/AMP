using System;
using System.Reflection;

namespace AMP.Extension {
    internal static class ObjectExtension {
        internal static System.Object GetFieldValue(this System.Object obj, string name) {
            if(obj == null) { return null; }

            Type type = obj.GetType();

            foreach(string part in name.Split('.')) {
                FieldInfo info = type.GetField(part, BindingFlags.Instance | BindingFlags.NonPublic);
                if(info == null) { return null; }

                obj = info.GetValue(obj);
            }
            return obj;
        }

        internal static T GetFieldValue<T>(this System.Object obj, string name) {
            System.Object retval = GetFieldValue(obj, name);
            if(retval == null) { return default(T); }

            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }

        internal static void SetFieldValue<T>(this System.Object obj, string name, T val) {
            if(obj == null) { return; }

            foreach(string part in name.Split('.')) {
                Type type = obj.GetType();
                FieldInfo info = type.GetField(part, BindingFlags.Instance | BindingFlags.NonPublic);
                if(info == null) { return; }

                info.SetValue(obj, val);
                return;
            }
        }
    }
}
