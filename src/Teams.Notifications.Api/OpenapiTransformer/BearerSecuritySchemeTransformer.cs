﻿namespace Teams.Notifications.Api.OpenApiTransformer;

/// <summary>
///     Transforms an OpenAPI document to include a JWT Bearer security scheme if the 'NotificationScheme' authentication
///     scheme is registered.
/// </summary>
/// <remarks>
///     Use this transformer to automatically add JWT Bearer authentication details to the OpenAPI document
///     when the 'NotificationScheme' is present. This enables API consumers to understand and utilize JWT Bearer
///     authentication for protected endpoints.
/// </remarks>
/// <param name="authenticationSchemeProvider">
///     The authentication scheme provider used to retrieve available authentication schemes for determining whether to add
///     the Bearer security scheme.
/// </param>
public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    /// <summary>
    ///     Asynchronously updates the specified OpenAPI document to include security scheme information for notification
    ///     authentication if applicable.
    /// </summary>
    /// <remarks>
    ///     If the notification authentication scheme is present, the method adds a JWT Bearer security
    ///     scheme to the OpenAPI document's components. The document is modified in place.
    /// </remarks>
    /// <param name="document">
    ///     The OpenAPI document to be transformed. This document will be modified to include security
    ///     schemes if required.
    /// </param>
    /// <param name="context">
    ///     The context for the document transformation, providing additional information or services needed during the
    ///     process.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous transformation operation.</returns>
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