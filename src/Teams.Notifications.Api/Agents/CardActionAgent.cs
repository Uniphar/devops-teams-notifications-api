using System.Text.RegularExpressions;
using Activity = Microsoft.Agents.Core.Models.Activity;
using Attachment = Microsoft.Agents.Core.Models.Attachment;

namespace Teams.Notifications.Api.Agents;

public class CardActionAgent : AgentApplication
{
    public CardActionAgent(AgentApplicationOptions options) : base(options)
    {
        AdaptiveCards.OnActionExecute(new Regex(".*?"), ProcessCardActionAsync);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Conversation, EventName = ConversationUpdateEvents.MembersAdded)]
    protected async Task MemberAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Welcome new user"), cancellationToken);
    }

    [Microsoft.Agents.Builder.App.Route(RouteType = RouteType.Activity, Type = ActivityTypes.Message, Rank = RouteRank.Last)]
    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(turnContext.Activity.Text))
            await turnContext.SendActivityAsync(MessageFactory.Text("You are not meant to chat in this channel"), cancellationToken);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(ITurnContext turnContext, ITurnState turnState, object data, CancellationToken cancellationToken)
    {
        // var submitData = ProtocolJsonSerializer.ToObject<AdaptiveCardSubmitData>(data);

        // Create a response message based on the response content type from the WeatherForecastAgent
        var attachment = new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = "We got an action, but this is not yet implemented!"
        };
        var pendingActivity = new Activity
        {
            Type = "message",
            Id = turnContext.Activity.ReplyToId,
            Attachments = new List<Attachment> { attachment }
        };
        await turnContext.UpdateActivityAsync(pendingActivity, cancellationToken);
        return new AdaptiveCardInvokeResponse();
    }
}