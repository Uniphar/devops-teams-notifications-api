// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Agents.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Teams.Notifications.Api.Extensions;

public static class AspNetExtensions
{

    public static void AddAgentAspNetAuthentication(this IServiceCollection services, IConfiguration configuration, string tokenValidationSectionName = "TokenValidation", ILogger logger = null)
    {
        var audience = configuration["ClientId"];
        var tenantId = configuration["TenantId"];


        List<string> validTokenIssuers =
        [
            "https://api.botframework.com",
            "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
            "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
            "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/",
            "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a/v2.0",
            "https://sts.windows.net/69e9b82d-4842-4902-8d1e-abc5b98a55e8/",
            "https://login.microsoftonline.com/69e9b82d-4842-4902-8d1e-abc5b98a55e8/v2.0"
        ];


        if (!string.IsNullOrEmpty(tenantId))
        {
            validTokenIssuers.Add(string.Format(CultureInfo.InvariantCulture, AuthenticationConstants.ValidTokenIssuerUrlTemplateV1, tenantId));
            validTokenIssuers.Add(string.Format(CultureInfo.InvariantCulture, AuthenticationConstants.ValidTokenIssuerUrlTemplateV2, tenantId));
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidIssuers = validTokenIssuers,
                    ValidAudiences = [audience],
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true
                };
                // Using Microsoft.IdentityModel.Validators
                options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
            });
    }
}
