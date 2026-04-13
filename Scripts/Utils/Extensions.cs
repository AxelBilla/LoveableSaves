using System;
using System.Reflection;

namespace LoveableSaves {

    public static class SaveExtensions {

        public static void SetValue(this MemberInfo member, object obj, object value) {
            switch (member.MemberType) {
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(obj, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(obj, value);
                    break;
            }
        }

        public static object GetValue(this MemberInfo member, object obj) {
            switch (member.MemberType) {
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(obj);
                case MemberTypes.Property:
                    return ((PropertyInfo)member).GetValue(obj);
            }
            return null;
        }

        public static Type ResolveMemberType(this MemberInfo member) {
            switch (member.MemberType) {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
            }
            return null;
        }
    }
}
