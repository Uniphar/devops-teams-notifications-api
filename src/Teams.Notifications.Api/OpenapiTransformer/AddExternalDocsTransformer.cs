// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;

namespace Teams.Notifications.Api.OpenapiTransformer;

public sealed class AddExternalDocsTransformer() : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
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

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.RequestBody?.Content == null || !operation.RequestBody.Content.TryGetValue("multipart/form-data", out var value)) return Task.CompletedTask;
        if (value.Schema.Type == "object" && value.Schema.Properties.ContainsKey("file"))
            operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
                {
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        ["file"] = new()
                        {
                            Style = ParameterStyle.Form
                        }
                    },
                    Schema = new OpenApiSchema
                    {
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new()
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        }
                    }
                }
                ;
        return Task.CompletedTask;
    }
}