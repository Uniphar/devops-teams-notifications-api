using Teams.Notifications.Formatter;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<FormatCommand>("format");
    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        return -99;
    });
});
return app.Run(args);

