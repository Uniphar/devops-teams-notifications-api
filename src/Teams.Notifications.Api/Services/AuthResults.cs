// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Teams.Notifications.Api.Services;

internal sealed class AuthResults
{
    public required AuthenticationResult MsalAuthResult { get; init; }
}