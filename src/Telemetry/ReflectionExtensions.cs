using System.Reflection;

namespace Telemetry;

public static class ReflectionExtensions
{
    public static MethodInfo GetMethod(Delegate del) => del.Method;
}