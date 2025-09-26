using Microsoft.AspNetCore.OpenApi;

namespace Teams.Notifications.Api.OpenApiTransformer;

public sealed class AddCorrectFileTransformer : IOpenApiOperationTransformer
{
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