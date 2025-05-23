using Teams.Notifications.Api.Agents;

namespace Teams.Notifications.Api.Models;

public class AdaptiveCardBuilder
{
    public static AdaptiveCard CreateFileProcessingCard(FileErrorModelOld model, string? url)
    {
        string statusString;
        string title;
        string info;
        switch (model.Status)
        {
            case FileErrorStatusEnum.Failed:
                statusString = "❌ Failed";
                title = "🚨 File Processing Error";
                info = "There was an issue processing the file. Please check the file and make sure the system is working right";
                break;
            case FileErrorStatusEnum.InProgress:
                statusString = "In progress";
                title = "File is reprocessing";
                info = "The external system has let us known that they are not reprocessing your file";
                break;
            case FileErrorStatusEnum.SystemNotified:
                statusString = "System notified";
                title = "We sent a message to the external system";
                info = "Thank you for pressing reprocess, the external system has been notified and we are working on getting it reprocessed, you can check this card for updates";
                break;
            // can never happen only failed and in progress are options
            case FileErrorStatusEnum.Success:
           
            default:
                statusString = "❌ Failed";
                title = "🚨 File Processing Error";
                info = "There was an issue processing the file.";
                break;
        }

        var actions = new List<AdaptiveAction>();
        if (model.Status == FileErrorStatusEnum.Failed)
            actions.Add(
                new AdaptiveExecuteAction
                {
                    Title = "🔁 Reprocess File",
                    Data = new AdaptiveCardSubmitData
                    {
                        FileName = model.FileName,
                        System = model.System,
                        JobId = model.JobId
                    },
                    Verb = "process",
                    Id = "process"
                });
        if (!string.IsNullOrWhiteSpace(url))
            actions.Add(new AdaptiveOpenUrlAction
            {
                Title = "📂 Download file",
                Url = new Uri(url)
            });
        if (!string.IsNullOrEmpty(model.ErrorMessage))
            actions.Add(new AdaptiveShowCardAction
            {
                Title = "🔍 View Error Details",
                Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
                {
                    Body =
                    [
                        new AdaptiveTextBlock
                        {
                            Text = model.ErrorMessage,
                            Wrap = true,
                            FontType = AdaptiveFontType.Monospace,
                            Color = AdaptiveTextColor.Attention
                        }
                    ]
                }
            });
        var adaptiveFacts = new List<AdaptiveFact>
        {
            new("File:", model.FileName),
            new("System:", model.System),
            new("Job ID:", model.JobId),
            new("Status:", statusString)
        };
        if (model.OriginalErrorTimestamp != null)
            adaptiveFacts.Add(new AdaptiveFact("Timestamp:", model.OriginalErrorTimestamp?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
        var titleColor = model.Status switch
        {
            FileErrorStatusEnum.InProgress => AdaptiveTextColor.Warning,
            FileErrorStatusEnum.Failed => AdaptiveTextColor.Attention,
            _ => AdaptiveTextColor.Good
        };
        return new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = title,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Large,
                    Color = titleColor
                },

                new AdaptiveTextBlock
                {
                    Text = info,
                    Wrap = true
                },

                new AdaptiveFactSet
                {
                    Facts = adaptiveFacts
                }
            ],
            Actions = actions
        };
    }
}