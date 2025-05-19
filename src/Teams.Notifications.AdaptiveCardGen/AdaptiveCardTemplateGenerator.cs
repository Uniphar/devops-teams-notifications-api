using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaptiveCardSourceGen;

[Generator]

public class AdaptiveCardTemplateGenerator : IIncrementalGenerator

{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var templates = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".json", StringComparison.Ordinal) && file.Path.Contains("Templates"));

        var templateAndContent = templates
            .Select((file, _) => (file.Path, Content: file.GetText()!.ToString()));

        context.RegisterSourceOutput(templateAndContent, (spc, item) =>
        {
            var (path, content) = item;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var matches = Regex.Matches(content, "{{(.*?)}}");

            var properties = matches
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            var modelSource = GenerateModel(fileName, properties);
            spc.AddSource($"{fileName}Model.g.cs", SourceText.From(modelSource, Encoding.UTF8));

            var controllerSource = GenerateController(fileName, properties);
            spc.AddSource($"{fileName}Controller.g.cs", SourceText.From(controllerSource, Encoding.UTF8));
        });
    }

    private static string GenerateModel(string name, List<string> props)
    {
        var propsCode = string.Join("\n", props.Select(p => $"        public string {p} {{ get; set; }}"));
        return
            $$"""
              namespace GeneratedCardModels
              {
                  public class {{name}}Model
                  {
              {{propsCode}}
                  }
              }
              """;
    }

    private static string GenerateController(string name, List<string> props)
    {
        var modelName = $"{name}Model";
        var safeRoute = name.ToLowerInvariant();
        var controllerName = $"{name}Controller";

        return
            $$"""
              using GeneratedCardModels;
              
              namespace GeneratedCardControllers
              {
                  [ApiController]
                  [Microsoft.AspNetCore.Mvc.Route("api/cards/[controller]")]
                  public class {{controllerName}} : ControllerBase
                  {
                      [HttpPost]
                      public IActionResult Render([FromBody] {{modelName}} model)
                      {
                          return Ok();
                      }
                  }
              }
              """;
    }
}