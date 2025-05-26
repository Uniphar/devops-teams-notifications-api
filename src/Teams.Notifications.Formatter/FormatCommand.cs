using AdaptiveCards;
using Teams.Notifications.AdaptiveCardGen;
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
        var props = text.GetPropertiesFromJson();
        if (!props.IsValidTypes(out var WrongItems))
        {
            var file = Path.GetFileName(sourcePath);
            AnsiConsole.MarkupLineInterpolated($"[bold red]The following file has incompatible properties[/] [bold white]{file}[/] ");
            var table = new Table();
            table.AddColumn(new TableColumn("[green]Template[/]"));
            table.AddColumn(new TableColumn(new Markup("[yellow]Type[/]")));
            table.AddColumn(new TableColumn("[blue]Property name[/]"));
            WrongItems.ToList().ForEach(x => table.AddRow("[bold green]{{" + x.Key + ":" + x.Value + "}}[/]", $"[yellow]{x.Value}[/]", $"[blue]{x.Key}[/]"));
            AnsiConsole.Write(table);
            GitHubActions.Error("Formatting", $"One of the files has incompatible properties, check the following file: {file} for property: {string.Join(",", WrongItems.Keys)}, unrecognised type(s) {string.Join(",", WrongItems.Values)}");
            throw new InvalidDataException($"Unrecognised types {string.Join(",", WrongItems.Values)}");
        }

        if (!props.IsValidFile(out _))
        {
            var file = Path.GetFileName(sourcePath);
            AnsiConsole.MarkupLineInterpolated($"[bold red]The following file has a file-url or file-name but not the File as property name[/] [bold white]{file}[/]");
            AnsiConsole.MarkupLine("Only [bold white]{{FileName:file}}[/] or/and [bold white]{{FileUrl:file}}[/] , which will create a IFormFile File entry to upload to");

            GitHubActions.Error("Formatting", $"One of the files has incompatible properties, check the following file: {file} for property: {string.Join(",", WrongItems.Keys)}, unrecognised type(s) {string.Join(",", WrongItems.Values)}");
            throw new InvalidDataException($"Unrecognised types {string.Join(",", WrongItems.Values)}");
        }

        var item = AdaptiveCard.FromJson(text).Card;
        var formatted = item.ToJson() ?? string.Empty;

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