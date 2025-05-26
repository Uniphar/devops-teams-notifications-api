using Teams.Notifications.Formatter;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Critical);

builder.Services.AddCommand<FormatCommand>("format");

builder.UseSpectreConsole(config => { config.SetExceptionHandler((ex, _) => AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything)); });

await builder.Build().RunAsync();