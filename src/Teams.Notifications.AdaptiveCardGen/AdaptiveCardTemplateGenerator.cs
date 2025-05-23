using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        var properties = content.GetPropertiesFromJson();

        var modelName = $"{fileName}Model";
        var controllerName = $"{fileName}Controller";
        var filename = $"{fileName}.json";
        var modelSource = GenerateModel(modelName, properties);
        spc.AddSource($"{fileName}Model.g.cs", SourceText.From(modelSource, Encoding.UTF8));

        var controllerSource = GenerateController(modelName, controllerName, filename);
        spc.AddSource($"{fileName}Controller.g.cs", SourceText.From(controllerSource, Encoding.UTF8));
    }



    private static string GenerateModel(string modelName, Dictionary<string, string> props)
    {
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
            .Single(str => str.EndsWith(name));


        using (var stream = assembly.GetManifestResourceStream(resourcePath))
        using (var reader = new StreamReader(stream))
            return reader.ReadToEnd();
    }
}