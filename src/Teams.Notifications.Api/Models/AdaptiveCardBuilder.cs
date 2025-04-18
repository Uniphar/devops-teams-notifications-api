using System;
using System.Collections.Generic;
using System.Globalization;
using AdaptiveCards;

namespace Teams.Notifications.Api.Models;

public class AdaptiveCardBuilder
{
    public static AdaptiveCard CreateFileProcessingCard(FileErrorModel model, string? url)
    {
        string statusString;
        string title;
        string info;
        switch (model.Status)
        {
            case FileErrorStatusEnum.Failed:
                statusString = "❌ Failed";
                title = "🚨 File Processing Error";
                info = "There was an issue processing the file.";
                break;
            case FileErrorStatusEnum.InProgress:
                statusString = "In progress";
                title = "File is reprocessing";
                info = "You requested to reprocess the file";
                break;
            // can never happen only failed and in progress are options
            case FileErrorStatusEnum.Succes:
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
                    Data = new
                    {
                        action = "reprocessFile"
                    },
                    Verb = "process",
                    Id = "process"
                });
        if (!string.IsNullOrWhiteSpace(url))
            actions.Add(new AdaptiveOpenUrlAction
            {
                Title = "📂 Open file in Teams",
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
        return new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = title,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Large,
                    Color = AdaptiveTextColor.Warning
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