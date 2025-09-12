using Teams.Notifications.Formatter;

var app = new CommandApp();

app.Configure(config => { config.AddCommand<FormatCommand>("format"); });
return app.Run(args);