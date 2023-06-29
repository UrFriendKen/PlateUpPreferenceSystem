using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PreferenceSystem.Utils
{
    public class ReflectionUtils
    {
        private static Dictionary<(Type, string), MethodInfo> cachedMethods = new Dictionary<(Type, string), MethodInfo>();

        public static Dictionary<(Type, string), FieldInfo> cachedFields = new Dictionary<(Type, string), FieldInfo>();

        public static MethodInfo GetMethod<T>(string methodName, BindingFlags flags)
        {
            (Type, string) key = (typeof(T), methodName);
            if (cachedMethods.TryGetValue(key, out var value))
            {
                return value;
            }
            cachedMethods[key] = typeof(T).GetMethod(methodName, flags);
            return cachedMethods[key];
        }

        public static FieldInfo GetField<T>(string fieldName, BindingFlags flags)
        {
            (Type, string) key = (typeof(T), fieldName);
            if (cachedFields.TryGetValue(key, out var value))
            {
                return value;
            }
            cachedFields[key] = typeof(T).GetField(fieldName, flags);
            return cachedFields[key];
        }

        public static MethodInfo GetMethod<T>(string methodName)
        {
            (Type, string) key = (typeof(T), methodName);
            if (cachedMethods.TryGetValue(key, out var value))
            {
                return value;
            }
            cachedMethods[key] = AccessTools.Method(typeof(T), methodName, (Type[])null, (Type[])null);
            return cachedMethods[key];
        }

        public static FieldInfo GetField<T>(string fieldName)
        {
            (Type, string) key = (typeof(T), fieldName);
            if (cachedFields.TryGetValue(key, out var value))
            {
                return value;
            }
            cachedFields[key] = AccessTools.Field(typeof(T), fieldName);
            return cachedFields[key];
        }
    }
}
