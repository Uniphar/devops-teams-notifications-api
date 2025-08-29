namespace Teams.Notifications.Api.Telemetry;

public static class ReflectionExtensions
{
    public static MethodInfo GetMethod(Delegate del) => del.Method;
}