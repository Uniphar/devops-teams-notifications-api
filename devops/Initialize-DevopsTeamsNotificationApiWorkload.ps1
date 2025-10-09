function Initialize-DevopsTeamsNotificationApiWorkload {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('dev', 'test', 'prod')]
        [string] $Environment 
    )
    $botTemplate = Join-Path $PSScriptRoot -ChildPath ".\bot.bicep"
    $devopsBotName = Resolve-UniResourceName 'bot' $p_devopsDomain -Dev:$Dev -Environment $Environment
    $botPermissionsNeeded = @(
        # add users to a channel    
        "ChannelMember.ReadWrite.All", 
        # read what channels are in a team
        "ChannelSettings.ReadWrite.All",
        # see members in the team
        "ChatMember.ReadWrite.All", 
        # read messages in a channel
        "ChatMessage.Read.All", 
        # send messages in a channel
        "ChannelMessage.Send", 
        # send chat messages, bot could use that if messaged privately
        "Chat.ReadWrite.All"
        # read/write files in a channel
        "Files.ReadWrite.All", 
        # Create a new team, frontgate
        "Team.Create", 
        # Add team members in a team
        "TeamMember.ReadWrite.All", 
        # read what teams are in a tenant
        "TeamSettings.ReadWrite.All",
        # ability to read user info without being signed in, needed for the bot
        "TeamsUserConfiguration.Read.All",
        # frontgate, to create a group, which then will be used to create a team
        "Group.ReadWrite.All",
        #ability to read user profile, needed for frontgate to get user id
        "User.Read.All"
    )
    $devopsClusterIdentityName = Resolve-UniComputeDomainSAName $Environment $global:p_devopsDomain
    $aksClusterApp = Get-AzADApplication -DisplayName $devopsClusterIdentityName
    if ($PSCmdlet.ShouldProcess('Devops', 'Deploy')) {
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
        
            # creates the resources for the bot only!
            # endpoint is just an example, you will need to change it all the time
            New-AzResourceGroupDeployment -Mode Incremental `
                -Name $deploymentName `
                -ResourceGroupName $devopsDomainRgName `
                -TemplateFile $botTemplate `
                -endpoint 'https://XXXXX.devtunnels.ms/devops-teams-notification-api/api/messages' `
                -environment 'debug' `
                -botName $devopsBotNameDebug `
                -devopsBotAppId $devopsBotEntraIdAppDebug.AppId `
                -Verbose:($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)

            $token = Get-AzAccessToken -ResourceUrl $global:g_microsoftGraphApi -AsSecureString
            Connect-MgGraph -AccessToken $token.Token -NoWelcome
            # for debug purposes, give the same creds as the workload
            Grant-MicrosoftGraphPermission -ApplicationName $devopsBotNameDebug -Permissions $botPermissionsNeeded -RevokeExisting   
        
        }
        # deploy the bot service, using the workload identity of the k8s cluster
        New-AzResourceGroupDeployment -Mode Incremental `
            -Name $deploymentName `
            -ResourceGroupName $devopsDomainRgName `
            -TemplateFile $devopsDomainTemplateFile `
            -endpoint "https://api.$environment.uniphar.ie/devops-teams-notification-api/api/messages" `
            -environment $Environment `
            -botName $devopsBotName `
            -devopsBotAppId $aksClusterApp.AppId `
            -Verbose:($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)

        # devops-teams-notifications-api needs permissions to graph stuff, since we use workload identity we can use this
        # in the future add revoke existing if needed and use a custom workload identity for the bot
        Grant-MicrosoftGraphPermission -ApplicationName $devopsClusterIdentityName -Permissions $botPermissionsNeeded

        
    }
    else {
        $TestResult = Test-AzResourceGroupDeployment -Mode Incremental `
            -Name $deploymentName `
            -ResourceGroupName $devopsDomainRgName `
            -TemplateFile $botTemplate `
            -endpoint "https://api.$environment.uniphar.ie/devops-teams-notification-api/api/messages" `
            -environment $Environment `
            -botName $devopsBotName `
            -devopsBotAppId $aksClusterApp.AppId `
            -Verbose:($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent -eq $true)

        if ($TestResult) {
            $TestResult
            throw "The deployment for $botTemplate did not pass validation."
        }
    }

}