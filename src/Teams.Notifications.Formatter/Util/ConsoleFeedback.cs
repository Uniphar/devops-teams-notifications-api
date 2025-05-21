namespace Teams.Notifications.Formatter.Util;

internal static class ConsoleFeedback
{
    public static void Success(string title) => AnsiConsole.MarkupLineInterpolated($"[green]✓[/] [dim]{title}[/]");

    public static void Updated(string title) => AnsiConsole.MarkupLineInterpolated($"[yellow]✎[/] {title}");


    public static void Error(string title, string message) => AnsiConsole.MarkupLineInterpolated($"[red]🞬 {title}: {message}[/]");
}