function Initialize-DevopsTeamsNotificationApiWorkload {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('dev', 'test', 'prod')]
        [string] $Environment 
    )
    $botTemplate = Join-Path $PSScriptRoot -ChildPath ".\bot.bicep"
    $devopsBotName = Resolve-UniResourceName 'bot' $p_devopsDomain -Dev:$Dev -Environment $Environment
    # Minimal application permissions required for:
    # - Installing a Teams app into any team, channel, or chat
    # - Reading all messages (teams/channel/chats)
    # - Adding users to teams/channels/chats
    # - Creating teams
    # - Reading/writing files in channels

    $botPermissionsNeeded = @(
        # Group control to create groups, update membership, and manage private/shared channels.
        "Group.ReadWrite.All"
        # Create new Microsoft Teams
        "Team.Create"
        # Not everything is fully covered by Group.ReadWrite.All
        "TeamMember.ReadWrite.All"
        # Add/remove users from channels, including private/shared channels
        "ChannelMember.ReadWrite.All"
        # Read and update channels
        "ChannelSettings.ReadWrite.All"
        # Read *all* channel messages across the entire tenant (standard + private + shared)
        "ChannelMessage.Read.All"
        # Send messages as the bot/app into any channel
        "ChannelMessage.Send"
        # Read/write all chat messages (1:1 + group chats) and send messages
        "Chat.ReadWrite.All"
        # Add/remove members from chats (1:1 or multiparty)
        "ChatMember.ReadWrite.All"
        # Needed to get the teams app ids from the app catalog, which we have installed already
        "AppCatalog.ReadWrite.All"
        # install/uninstall a Teams app for a USER and read installed apps
        "TeamsAppInstallation.ReadWriteAndConsentSelfForUser.All"
        # install/uninstall a Teams app for a CHAT and read installed apps
        "TeamsAppInstallation.ReadWriteAndConsentSelfForChat.All"
        # Read and write files in Teams channels (SharePoint-backed)
        "Files.ReadWrite.All"
        # Read user profiles – needed for resolving user IDs to add to teams/chats/channels
        "User.Read.All"
    )

    $devopsClusterIdentityName = Resolve-UniComputeDomainSAName $Environment $global:p_devopsDomain
    $aksClusterApp = Get-AzADApplication -DisplayName $devopsClusterIdentityName
    $deploymentName = Resolve-DeploymentName -Suffix '-TeamsNotificationApiBot'
    $devopsDomainRgName = Resolve-UniResourceName 'resource-group' $p_devopsDomain -Dev:$Dev -Environment $Environment
    $token = Get-AzAccessToken -ResourceUrl $global:g_microsoftGraphApi -AsSecureString
    Connect-MgGraph -AccessToken $token.Token -NoWelcome
    # we need a bot service which is not the workload identity, but a separate app registration (so we can get its secret for local debugging)
    # so to do this we will create a debug bot in the dev environment and give it the same permissions as the workload identity
    if ($Environment -eq 'dev') {
        $devopsBotNameDebug = Resolve-UniResourceName 'bot' $p_devopsDomain -Environment 'debug'
        $devopsBotEntraIdAppDebug = Get-AzADApplication -DisplayName $devopsBotNameDebug
        if (!$devopsBotEntraIdAppDebug) { 
            $devopsBotEntraIdAppDebug = New-AzADApplication -DisplayName $devopsBotNameDebug -SigninAudience AzureADMyOrg 
        }
        $devopsBotEntraIdServicePrincipalDebug = Get-AzADServicePrincipal -ApplicationId $devopsBotEntraIdAppDebug.AppId

        if (!$devopsBotEntraIdServicePrincipalDebug) {
            $devopsBotEntraIdServicePrincipalDebug = New-AzADServicePrincipal -ApplicationId $devopsBotEntraIdAppDebug.AppId 
        }
        
        $debugDeploymentConfig = @{
            Mode              = 'Incremental'
            Name              = $deploymentName
            ResourceGroupName = $devopsDomainRgName
            TemplateFile      = $botTemplate
            endpoint          = 'https://XXXXX.devtunnels.ms/devops-teams-notification-api/api/messages'
            environment       = 'debug'
            botName           = $devopsBotNameDebug
            devopsBotAppId    = $devopsBotEntraIdAppDebug.AppId
            Verbose           = ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)
        }
        # creates the resources for the bot only!
        # endpoint is just an example, you will need to change it all the time
        New-AzResourceGroupDeployment @debugDeploymentConfig

     
        # for debug purposes, give the same creds as the workload
        $debugGrantPermissionConfig = @{
            ApplicationName = $devopsBotNameDebug
            Permissions     = $botPermissionsNeeded
            RevokeExisting  = $true
            Verbose         = ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)
        }
        Grant-MicrosoftGraphPermission @debugGrantPermissionConfig
    }
    $deploymentConfig = @{
        Mode              = 'Incremental'
        Name              = $deploymentName
        ResourceGroupName = $devopsDomainRgName
        TemplateFile      = $botTemplate
        endpoint          = "https://api.$Environment.uniphar.ie/devops-teams-notification-api/api/messages"
        environment       = $Environment
        botName           = $devopsBotName
        devopsBotAppId    = $aksClusterApp.AppId
        Verbose           = ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)
    }
    # deploy the bot service, using the workload identity of the k8s cluster
    New-AzResourceGroupDeployment @deploymentConfig

    # devops-teams-notifications-api needs permissions to graph stuff, since we use workload identity we can use this
    # in the future add revoke existing if needed and use a custom workload identity for the bot
    $grantPermissionConfig = @{
        ApplicationName = $devopsClusterIdentityName
        Permissions     = $botPermissionsNeeded
        RevokeExisting  = $true
        Verbose         = ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)
    }
    Grant-MicrosoftGraphPermission @grantPermissionConfig
}