using System;
using System.Reflection;

namespace AMP.Network.Packets.Extension {
    internal static class PacketExtension {
        internal static object GetValue(this MemberInfo memberInfo, object forObject) {
            switch(memberInfo.MemberType) {
                case MemberTypes.Field:
                    return ((FieldInfo)     memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)  memberInfo).GetValue(forObject);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static void SetValue(this MemberInfo memberInfo, object forObject, object value) {
            switch(memberInfo.MemberType) {
                case MemberTypes.Field:
                    ((FieldInfo)     memberInfo).SetValue(forObject, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)  memberInfo).SetValue(forObject, value);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static Type GetUnderlyingType(this MemberInfo member) {
            switch(member.MemberType) {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
        }
    }
}
