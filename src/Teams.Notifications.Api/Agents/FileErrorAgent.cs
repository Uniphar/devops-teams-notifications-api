using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Teams.Notifications.Api.Models;
using AdaptiveCard = AdaptiveCards.AdaptiveCard;

namespace Teams.Notifications.Api.Agents;

public class FileErrorAgent : AgentApplication
{
    public FileErrorAgent(AgentApplicationOptions options) : base(options)
    {
        AdaptiveCards.OnActionExecute("process", ProcessCardActionAsync);
    }

    [Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)] protected async Task MemberAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken) => await turnContext.SendActivityAsync(MessageFactory.Text("Welcome new user"), cancellationToken);

    [Route(RouteType = RouteType.Activity, Type = ActivityTypes.Message, Rank = RouteRank.Last)]
    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(turnContext.Activity.Text))
            await turnContext.SendActivityAsync(MessageFactory.Text("You are not meant to chat in this channel"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(ITurnContext turnContext, ITurnState turnState, object data, CancellationToken cancellationToken)
    {
        //mock we could get all the info from the turn contexts activity, do our api call and then return an in progress action
        var fileError = new FileErrorModel
        {
            FileName = "Unknown",
            System = "Unknown",
            JobId = "Unknown",
            Status = FileErrorStatusEnum.InProgress
        };
        var json = AdaptiveCardBuilder.CreateFileProcessingCard(fileError, null).ToJson();
        // Create a response message based on the response content type from the WeatherForecastAgent
        var attachement = new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = json
        };
        var pendingActivity = new Activity
        {
            Type = "message",
            Id = turnContext.Activity.ReplyToId,
            Attachments = new List<Attachment> { attachement }
        };
        await turnContext.UpdateActivityAsync(pendingActivity, cancellationToken);
        return new AdaptiveCardInvokeResponse();
    }
}