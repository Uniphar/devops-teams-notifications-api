using Microsoft.Agents.Extensions.Teams.App;

namespace Teams.Notifications.Api.Agents;

public class CardActionAgent : AgentApplication
{
    private readonly IFrontgateApiService _frontgateApiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CardActionAgent> _logger;
    private readonly ITeamsManagerService _teamsManagerService;
    private readonly ICustomEventTelemetryClient _telemetry;

    public CardActionAgent(
        AgentApplicationOptions options,
        ITeamsManagerService teamsManagerService,
        IFrontgateApiService frontgateApiService,
        ICustomEventTelemetryClient telemetry,
        ILogger<CardActionAgent> logger,
        IHttpClientFactory httpClientFactory
    ) : base(options)
    {
        _telemetry = telemetry;
        _logger = logger;
        _teamsManagerService = teamsManagerService;
        _frontgateApiService = frontgateApiService;
        _httpClientFactory = httpClientFactory;
        RegisterExtension(new TeamsAgentExtension(this),
            tae =>
            {
                tae.OnMessageEdit(MessageEdited);
                tae.MessageExtensions.OnQuery("findNuGetPackage", OnQuery);
                tae.MessageExtensions.OnSelectItem(OnSelectItem);
                tae.MessageExtensions.OnQueryLink(OnQueryLink);

                tae.OnFeedbackLoop(MyFeedbackLoopHandler);
            });
        AdaptiveCards.OnSearch("dataset", OnSearchDS);
        AdaptiveCards.OnActionExecute(new Regex(".*?"), ProcessCardActionAsync);
        OnMessageReactionsAdded(OnMessageReaction);
        OnActivity(ActivityTypes.Message, OnMessageAsync);
    }

    private Task MyFeedbackLoopHandler(ITurnContext turnContext, ITurnState turnState, FeedbackLoopData feedbackLoopData, CancellationToken cancellationToken)
    {
        // Do something with FeedbackLoopData
        Trace.WriteLine("FeedbackLoop handler");
        return Task.CompletedTask;
    }

    private Task<MessagingExtensionResult> OnQueryLink(ITurnContext turnContext, ITurnState turnState, string url, CancellationToken cancellationToken) => Task.FromResult(new MessagingExtensionResult { Text = "On Query Link" });

    private Task<IList<AdaptiveCardsSearchResult>> OnSearchDS(ITurnContext turnContext, ITurnState turnState, Query<AdaptiveCardsSearchParams> query, CancellationToken cancellationToken)
    {
        var qt = query.Parameters.QueryText;
        IList<AdaptiveCardsSearchResult> result = new List<AdaptiveCardsSearchResult>
        {
            new("search", qt)
        };
        return Task.FromResult(result);
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var numFiles = turnState.Temp.InputFiles.Count;
        await turnContext.SendActivityAsync("found files " + numFiles, cancellationToken: cancellationToken);
    }

    private async Task<MessagingExtensionResult> OnSelectItem(ITurnContext turnContext, ITurnState turnState, object item, CancellationToken cancellationToken)
    {
        var package = ((JsonElement)item).Deserialize<PackageItem>();
        if (package is null)
        {
            await turnContext.SendActivityAsync("selected item is not a packageItem", cancellationToken: cancellationToken);
            _logger.LogWarning("Selected Item cannot be deserialized as a PackageItem");
            return null!;
        }

        await turnContext.SendActivityAsync("selected item " + JsonSerializer.Serialize(item), cancellationToken: cancellationToken);
        ThumbnailCard card = new()
        {
            Title = $"{package.PackageId}, {package.Version}",
            Subtitle = package.Description,
            Buttons =
            [
                new() { Type = ActionTypes.OpenUrl, Title = "Nuget Package", Value = $"https://www.nuget.org/packages/{package.PackageId}" },
                new() { Type = ActionTypes.OpenUrl, Title = "Project", Value = package.ProjectUrl }
            ]
        };

        if (!string.IsNullOrEmpty(package.IconUrl)) card.Images = [new(package.IconUrl, "Icon")];

        MessagingExtensionAttachment attachment = new()
        {
            ContentType = ThumbnailCard.ContentType,
            Content = card
        };

        return await Task.FromResult(new MessagingExtensionResult
        {
            Type = "result",
            AttachmentLayout = "list",
            Attachments = [attachment]
        });
    }

    private async Task<MessagingExtensionResult> OnQuery(ITurnContext turnContext, ITurnState turnState, Query<IDictionary<string, object>> query, CancellationToken cancellationToken)
    {
        var cmd = ProtocolJsonSerializer.ToObject<CommandValue<string>>(turnContext.Activity.Value);
        if (cmd.CommandId != "findNuGetPackage")
        {
            _logger.LogWarning("Received unexpected commandID {cmdName}", cmd.CommandId);
            return await Task.FromResult(new MessagingExtensionResult());
        }

        JsonElement el = default;
        if (query.Parameters.TryGetValue("NuGetPackageName", out var elObj))
        {
            if (elObj is JsonElement element)
                el = element;
            else
            {
                _logger.LogWarning("Received unexpected type for NuGetPackageName: {type}", elObj.GetType());
                return await Task.FromResult(new MessagingExtensionResult());
            }
        }
        else
        {
            _logger.LogWarning("Query Parameters does not include NuGetPackageName");
            return await Task.FromResult(new MessagingExtensionResult());
        }

        if (el.ValueKind == JsonValueKind.Undefined) return await Task.FromResult(new MessagingExtensionResult());

        var text = el.GetString() ?? string.Empty;


        var packages = await FindPackages(text);
        List<MessagingExtensionAttachment> attachments =
        [
            .. packages.Select(package =>
            {
                var cardValue = $$$"""
                                   {
                                       "id": "{{{package.PackageId}}}",
                                       "version" : "{{{package.Version}}}",
                                       "description" : "{{{PackageItem.NormalizeString(package.Description!)}}}",
                                       "projectUrl" : "{{{package.ProjectUrl}}}",
                                       "iconUrl" : "{{{package.IconUrl}}}"
                                   }
                                   """;

                ThumbnailCard previewCard = new() { Title = package.PackageId, Tap = new() { Type = "invoke", Value = cardValue } };
                if (!string.IsNullOrEmpty(package.IconUrl)) previewCard.Images = [new(package.IconUrl, "Icon")];

                MessagingExtensionAttachment attachment = new()
                {
                    ContentType = HeroCard.ContentType,
                    Content = new HeroCard { Title = package.Id },
                    Preview = previewCard.ToAttachment()
                };

                return attachment;
            })
        ];

        return new()
        {
            Type = "result",
            AttachmentLayout = "list",
            Attachments = attachments
        };
    }

    private async Task<IEnumerable<PackageItem>> FindPackages(string text)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var jsonResult = await httpClient.GetStringAsync($"https://azuresearch-usnc.nuget.org/query?q=id:{text}&prerelease=true");
        var data = JsonDocument.Parse(jsonResult).RootElement.GetProperty("data");
        var packages = data.Deserialize<PackageItem[]>();
        return packages!;
    }

    private Task OnMessageReaction(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken) => turnContext.SendActivityAsync("Message Reaction: " + turnContext.Activity.ReactionsAdded[0].Type, cancellationToken: cancellationToken);

    private Task MessageEdited(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken) => turnContext.SendActivityAsync("Message Edited: " + turnContext.Activity.Id, cancellationToken: cancellationToken);

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)]
    protected async Task MemberAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Welcome new user"), cancellationToken);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)]
    private async Task WelcomeMessageToUserAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
        var msg = member.Name ?? "not teams user";
        await turnContext.SendActivityAsync($"hi {msg}, use the '+' option on Teams message textbox to start the MessageExtension search", cancellationToken: cancellationToken);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Activity, Type = ActivityTypes.Message, Rank = RouteRank.Last)]
    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(turnContext.Activity.Text)) await turnContext.SendActivityAsync(MessageFactory.Text("You are not meant to chat in this channel"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken
    )
    {
        using (_telemetry.WithProperties([new("ActionExecute", "LogicAppErrorProcessActionModel")]))
        {
            try
            {
                var model = ProtocolJsonSerializer.ToObject<LogicAppErrorProcessActionModel>(data);
                var teamsChannelData = turnContext.Activity.GetChannelData<TeamsChannelData>();

                var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, cancellationToken: cancellationToken);
                var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, cancellationToken: cancellationToken);
                var channel = channels.FirstOrDefault(x => x.Id == teamsChannelData.Channel.Id);

                // Guard against null channel data, which can occur when the json can't be deserialized
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (teamDetails is null)
                {
                    _logger.LogWarning("No team data for this file");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                if (channel?.Name == null)
                {
                    _logger.LogWarning("Could not load channel name");
                    return AdaptiveCardInvokeResponseFactory.BadRequest("Something went wrong reprocessing the file");
                }

                var teamId = teamDetails.AadGroupId;
                var channelName = channel.Name;

                if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(channelName)) throw new InvalidOperationException("Team or channelName is missing from the context.");

                var channelId = await _teamsManagerService.GetChannelIdAsync(teamId, channelName, cancellationToken);
                var fileName = await _teamsManagerService.GetFileNameAsync(teamId, channelId, model.PostFileLocation ?? string.Empty, cancellationToken);
                var groupUniqueName = await _teamsManagerService.GetGroupNameUniqueName(teamId, cancellationToken);
                var teamName = await _teamsManagerService.GetTeamName(teamId, cancellationToken);

                var fileInfo = new LogicAppFrontgateFileInformation
                {
                    file_name = fileName,
                    storage_reference = groupUniqueName,
                    initial_display_name = teamName,
                    storage_folder = $"/{channelName}/error/"
                };
                // Upload the file to the external API
                var uploadResponse = await _frontgateApiService.UploadFileAsync(model.PostOriginalBlobUri ?? string.Empty, fileInfo, cancellationToken);

                return uploadResponse.IsSuccessStatusCode
                    ? AdaptiveCardInvokeResponseFactory.Message(model.PostSuccessMessage ?? "Succes")
                    : AdaptiveCardInvokeResponseFactory.BadRequest($"Failed to sent file: {uploadResponse.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card action");
                throw;
            }
        }
    }
}