using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;

namespace Teams.Notifications.Api.OpenApiTransformer;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "NotificationScheme"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["NotificationScheme"] = new()
                    {
                        Name = "Authorization",
                        Description = "JWT Bearer authentication for API access",
                        In = ParameterLocation.Header,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        Type = SecuritySchemeType.Http
                    }
                }
                ;
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
        }
    }
}