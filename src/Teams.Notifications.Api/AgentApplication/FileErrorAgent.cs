using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.App.AdaptiveCards;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Teams.Notifications.Api.Models;
using AdaptiveCard = AdaptiveCards.AdaptiveCard;

namespace Teams.Notifications.Api.AgentApplication;

public class FileErrorAgent : Microsoft.Agents.Builder.App.AgentApplication
{
    public FileErrorAgent(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        AdaptiveCards.OnActionExecute("process", ProcessCardActionAsync);
    }

    protected async Task<AdaptiveCardInvokeResponse> ProcessCardActionAsync(ITurnContext turnContext, ITurnState turnState, object data, CancellationToken cancellationToken)
    {
        var json = AdaptiveCardBuilder.CreateFileProcessingRestartedCard().ToJson();
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

 
    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (var member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Welcome, in this channel you can find all the files that have failed"), cancellationToken);
                var attachement = new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = AdaptiveCardBuilder.CreateFileProcessingErrorCard().ToJson()
                };
                await turnContext.SendActivityAsync(MessageFactory.Attachment(attachement), cancellationToken);
            }
        }
    }
}