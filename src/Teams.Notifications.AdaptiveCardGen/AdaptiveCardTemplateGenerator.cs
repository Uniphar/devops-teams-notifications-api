using AdaptiveCards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
namespace Teams.Notifications.AdaptiveCardGen;

[Generator]
public class AdaptiveCardTemplateGenerator : IIncrementalGenerator

{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // defined in the Teams.Notifications.Api.csproj as additional item's
        var templateAndContent = context
            .AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".json", StringComparison.Ordinal) && file.Path.Contains("Templates"))
            .Select((file, _) => (file.Path, Content: file.GetText()!.ToString()));

        // get the content of each item, when you call this method
        context.RegisterSourceOutput(templateAndContent, (spc, item) => { CreateFiles(item, spc); });
    }

    private void CreateFiles((string Path, string Content) item, SourceProductionContext spc)
    {
        var (path, content) = item;

        var fileName = Path.GetFileNameWithoutExtension(path);
        var card = AdaptiveCard.FromJson(content).Card;
        var itemWithUnique = card.Actions.Where(x => x.Type == "Action.Execute");
        foreach (var action in itemWithUnique)
        {
            if (action is not AdaptiveExecuteAction adaptiveExecute) continue;
            var data = Regex.Replace(adaptiveExecute.DataJson, @"\r\n?|\n", "");
            var fullString = string.Empty;
            var converter = JsonConvert.DeserializeObject(data);

            //foreach (var prop in converter.GetType().GetProperties()) fullString += $" {prop.Name}, {prop.GetValue(converter, null)}";
            // to show warnings in the IDE, we need to use this, just an example
            spc.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ACG001",
                    "AdaptiveCard Property Names",
                    "Properties: {0}",
                    "AdaptiveCardGen",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None,
                data));
        }

        var modelProperties = content.GetPropertiesFromJson();
        var modelName = $"{fileName}Model";
        var controllerName = $"{fileName}Controller";
        var filename = $"{fileName}.json";
        var modelSource = GenerateModel(modelName, modelProperties);
        spc.AddSource($"{fileName}Model.g.cs", SourceText.From(modelSource, Encoding.UTF8));

        var controllerSource = GenerateController(modelName, controllerName, filename);
        spc.AddSource($"{fileName}Controller.g.cs", SourceText.From(controllerSource, Encoding.UTF8));
    }


    private static string GenerateModel(string modelName, Dictionary<string, string> props)
    {
        if (props.Values.Any(x => x == "file"))
        {
            props = props.Where(x => x.Value != "file").ToDictionary(x => x.Key, x => x.Value);
            props.Add("File", "IFormFile");
        }

        // key is the prop name, value the type, since keys are distinct by nature in Dictionaries
        var propertiesOfTheModel = string.Join("\n", props.OrderBy(x => x.Value).Select(p => $"        public {p.Value} {p.Key} {{ get; set; }}"));

        return
            $$"""
              namespace Teams.Notifications.Api.Models;
              public class {{modelName}} : BaseTemplateModel
              {
              {{propertiesOfTheModel}}
              }
              """;
    }

    private static string GenerateController(string modelName, string controllerName, string filename)
    {
        var text = ReadResource("CardTemplateController.csgen")
            .Replace("{{controllerName}}", controllerName)
            .Replace("{{modelName}}", modelName)
            .Replace("{{filename}}", filename);
        return text;
    }

    private static string ReadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly
            .GetManifestResourceNames()
            .Single(str => str.EndsWith(name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) return string.Empty;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}