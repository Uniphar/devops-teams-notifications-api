using Microsoft.AspNetCore.OpenApi;

namespace Teams.Notifications.Api.OpenApiTransformer;

public sealed class AddAdaptiveCardDocsTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var paths = document.Paths;
        foreach (var path in paths)
        foreach (var operation in path.Value.Operations)
            if (operation.Key == OperationType.Get)
            {
                var okResponse = operation.Value.Responses["200"];
                var schema = okResponse.Content["application/json"].Schema;
                schema.Type = "object";
                schema.ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Find out more about Adaptive Cards",
                    Url = new Uri("https://adaptivecards.io/schemas/adaptive-card.json")
                };
            }

        return Task.CompletedTask;
    }
}