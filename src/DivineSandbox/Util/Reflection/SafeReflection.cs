using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DivineSandbox.Util.Reflection;

internal static class SafeReflection {
    private const BindingFlags default_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static bool TryGetType(this Assembly assembly, string typeName, [NotNullWhen(returnValue: true)] out Type? type) {
        try {
            type = assembly.GetType(typeName);
            return type != null;
        }
        catch (Exception) {
            type = null;
            return false;
        }
    }

    public static bool TryGetMethod(this Type type, string methodName, BindingFlags? flags, [NotNullWhen(returnValue: true)] out MethodInfo? methodInfo) {
        try {
            methodInfo = type.GetMethod(methodName, flags ?? default_flags);
            return methodInfo != null;
        }
        catch (Exception) {
            methodInfo = null;
            return false;
        }
    }
    
    public static bool TryGetField(this Type type, string fieldName, BindingFlags? flags, [NotNullWhen(returnValue: true)] out FieldInfo? fieldInfo) {
        try {
            fieldInfo = type.GetField(fieldName, flags ?? default_flags);
            return fieldInfo != null;
        }
        catch (Exception) {
            fieldInfo = null;
            return false;
        }
    }
    
    public static bool TryGetProperty(this Type type, string propertyName, BindingFlags? flags, [NotNullWhen(returnValue: true)] out PropertyInfo? propertyInfo) {
        try {
            propertyInfo = type.GetProperty(propertyName, flags ?? default_flags);
            return propertyInfo != null;
        }
        catch (Exception) {
            propertyInfo = null;
            return false;
        }
    }
}
