
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.AgentApplication;

public class FileErrorAgent : Microsoft.Agents.Builder.App.AgentApplication
{
    public FileErrorAgent(AgentApplicationOptions options) : base(options)
    {

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        OnActivity(ActivityTypes.Message, MessageActivityAsync, rank: RouteRank.Last);
    }

    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        ChatHistory chatHistory = turnState.GetValue("conversation.chatHistory", () => new ChatHistory());

        var json = AdaptiveCardBuilder.CreateFileProcessingErrorCard().ToJson();
        // Create a response message based on the response content type from the WeatherForecastAgent
        IActivity response = MessageFactory.Attachment(new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = json
        });

        // Send the response message back to the user. 
        await turnContext.SendActivityAsync(response, cancellationToken);
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