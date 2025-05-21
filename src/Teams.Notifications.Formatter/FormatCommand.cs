using AdaptiveCards;
using Teams.Notifications.Formatter.Util;

namespace Teams.Notifications.Formatter;

internal sealed class FormatCommand : Command<FormatCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        var differ = new FilesDiffer(Directory.GetCurrentDirectory());
        differ.AddAllUnderPath("./../Teams.Notifications.Api/Templates", "*.json", FormatFile);

        if (!settings.Check)
        {
            if (!differ.Apply())
                throw new Exception("Failed to apply changes to config files.");
            return 0;
        }

        if (differ.Check("Formatting", "File differs after formatting"))
            return 0;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLineInterpolated($"[red]One of more config files were not formatted before commiting.[/] Run `dotnet run format` in the `{typeof(FormatCommand).Assembly.GetName().Name}` project directory.");
        GitHubActions.Error("Formatting", $"One of more config files were not formatted before commiting. Run `dotnet run format` in the `{typeof(FormatCommand).Assembly.GetName().Name}` project directory, and commit the updated config files.");
        throw new Exception("Failed to apply changes to config files.");
    }

    private static void FormatFile(string sourcePath, Stream formattedFile)
    {
        var text = File.ReadAllText(sourcePath);
        var item = AdaptiveCard.FromJson(text).Card;
        var formatted = item.ToJson();
        var sw = new StreamWriter(formattedFile);

        sw.Write(formatted);
        sw.Flush(); //otherwise you are risking empty stream
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--check")]
        public bool Check { get; init; }
    }
}