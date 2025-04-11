using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.AgentApplication;

public class FileErrorAgent : Microsoft.Agents.Builder.App.AgentApplication
{
    public FileErrorAgent(AgentApplicationOptions options) : base(options)
    {
        AdaptiveCards.OnActionExecute("adaptiveCard/action", CardActionAsync);

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        OnActivity(ActivityTypes.Message, MessageActivityAsync, rank: RouteRank.Last);
        // OnActivity("adaptiveCard/action", CardActionAsync, rank: RouteRank.Last);
    }

    protected async Task<AdaptiveCardInvokeResponse> CardActionAsync(ITurnContext turnContext, ITurnState turnState, object data, CancellationToken cancellationToken)
    {
        var json = AdaptiveCardBuilder.CreateFileProcessingErrorCard().ToJson();
        // Create a response message based on the response content type from the WeatherForecastAgent
        var attachement = new Attachment()
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

    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var json = AdaptiveCardBuilder.CreateFileProcessingErrorCard().ToJson();
        // Create a response message based on the response content type from the WeatherForecastAgent
        var attachement = new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = json
        };

        if (string.IsNullOrEmpty(turnContext.Activity.Text))
        {
            var pendingActivity = new Activity
            {
                Type = "message",
                Id = "43e90820-15fd-11f0-8473-9933b332e0c0",
                Attachments = new List<Attachment> { attachement }
            };
            await turnContext.UpdateActivityAsync(pendingActivity, cancellationToken);
        }
        else await turnContext.SendActivityAsync(MessageFactory.Attachment(attachement), cancellationToken);

        //response.Id = turnContext.Activity.ReplyToId;
        // Send the response message back to the user. 
        //await turnContext.UpdateActivityAsync(response, cancellationToken); 
    }

    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (var member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Welcome, in this channel you can find all the files that have failed"), cancellationToken);
            }
        }
    }
}