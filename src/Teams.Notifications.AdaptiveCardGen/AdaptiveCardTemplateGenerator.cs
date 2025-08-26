using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AdaptiveCards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

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
            var data = Regex.Replace(adaptiveExecute.DataJson, @"\r\n?|\n", string.Empty);
            var props = data.ExtractPropertiesFromJson();

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
                string.Join(",", props)));
            if (!props.Any()) continue;
            var actionModelName = $"{fileName}{adaptiveExecute.Verb}ActionModel";
            var actionSource = GenerateActionModel(actionModelName, props);
            spc.AddSource($"{actionModelName}.g.cs", SourceText.From(actionSource, Encoding.UTF8));
        }

        var modelProperties = content.GetMustachePropertiesFromString();
        var modelName = $"{fileName}Model";
        var controllerName = $"{fileName}Controller";
        var filename = $"{fileName}.json";
        var modelSource = GenerateModel(modelName, modelProperties);
        spc.AddSource($"{modelName}.g.cs", SourceText.From(modelSource, Encoding.UTF8));

        var controllerSource = GenerateController(modelName, controllerName, filename, spc);
        spc.AddSource($"{controllerName}.g.cs", SourceText.From(controllerSource, Encoding.UTF8));
    }

    private static string GetTypeFromActionModelMustache(KeyValuePair<string, string>? argMustacheProperties)
    {
        var prop = argMustacheProperties?.Value;
        if (prop == "file") return "required string";
        // seems double but intellisense doesn't like it otherwise
        if ( prop == null ||string.IsNullOrWhiteSpace(prop)) return "string?";
        return prop;
    }

    private static string GenerateActionModel(string actionModelName, List<PropWithMustache> props)
    {
        var propertiesOfTheModel = string.Join("\n", props.OrderBy(x => x.Property).Select(p => $"        public {MakeRequiredIfNeeded(GetTypeFromActionModelMustache(p.MustacheProperties))} {p.Property} {{ get; set; }}"));
        return
            $$"""
              #nullable enable
              namespace Teams.Notifications.Api.Action.Models;
              public class {{actionModelName}}
              {
              {{propertiesOfTheModel}}
              }
              #nullable disable
              """;
    }

    private static string MakeRequiredIfNeeded(string input)
    {
        return input switch
        {
            "string" => "required string",
            "int" => "required int",
            _ => input
        };
    }

    private static string GenerateModel(string modelName, Dictionary<string, string> props)
    {
        if (props.Values.Any(x => x == "file"))
        {
            props = props.Where(x => x.Value != "file").ToDictionary(x => x.Key, x => x.Value);
            props.Add("File", "required IFormFile");
        }

        if (props.Values.Any(x => x == "file?"))
        {
            props = props.Where(x => x.Value != "file?").ToDictionary(x => x.Key, x => x.Value);
            props.Add("File", "IFormFile?");
        }

        // key is the prop name, value the type, since keys are distinct by nature in Dictionaries
        var propertiesOfTheModel = string.Join("\n", props.OrderBy(x => x.Value).Select(p => $"        public {MakeRequiredIfNeeded(p.Value)} {p.Key} {{ get; set; }}"));

        return
            $$"""
              #nullable enable
              namespace Teams.Notifications.Api.Models;
              public class {{modelName}} : BaseTemplateModel
              {
              {{propertiesOfTheModel}}
              }
              #nullable disable
              """;
    }

    private static string GenerateController(string modelName, string controllerName, string filename, SourceProductionContext spc)
    {
        var text = ReadResource("CardTemplateController.csgen", spc)
            .Replace("{{controllerName}}", controllerName)
            .Replace("{{modelName}}", modelName)
            .Replace("{{filename}}", filename);
        return text;
    }

    private static string ReadResource(string name, SourceProductionContext spc)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly
            .GetManifestResourceNames()
            .Single(str => str.EndsWith(name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ACG001",
                    "AdaptiveCard generation file could not be found",
                    "Name: {0}",
                    "AdaptiveCardGen",
                    DiagnosticSeverity.Error,
                    true),
                Location.None,
                name));
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}