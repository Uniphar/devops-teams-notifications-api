using System;
using System.Collections.Generic;
using System.Globalization;
using AdaptiveCards;

namespace Teams.Notifications.Api.Models;

public class AdaptiveCardBuilder
{
    public static AdaptiveCard CreateFileProcessingCard(FileErrorModel model)
    {
        string statusString;
        string title;
        string info;
        switch (model.Status)
        {
            case FileErrorStatusEnum.Succes:
                statusString = "Success";
                title = "File is now cleared";
                info = string.Empty;
                break;
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
                        action = "reprocessFile",
                        fileId = "file123",
                        jobId = "FG-20250410-0915"
                    },
                    Verb = "process",
                    Id = "process"
                });
        actions.Add(new AdaptiveOpenUrlAction
        {
            Title = "📂 Open file in Teams",
            Url = new Uri("https://teams.microsoft.com/file/Report_Q1_2025.xlsx")
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
                            Text = "Stack Trace",
                            Weight = AdaptiveTextWeight.Bolder,
                            Spacing = AdaptiveSpacing.Small
                        },

                        new AdaptiveTextBlock
                        {
                            Text = "System.NullReferenceException: Object reference not set to an instance of an object.\n   at FrontGate.Processing.FileHandler.Process(String filePath)\n   at FrontGate.JobRunner.Run(Job job)\n   at FrontGate.MainController.Execute()",
                            Wrap = true,
                            FontType = AdaptiveFontType.Monospace,
                            Color = AdaptiveTextColor.Attention
                        }
                    ]
                }
            });

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
                    Facts =
                    [
                        new AdaptiveFact("File:", model.FileName),
                        new AdaptiveFact("System:", model.System),
                        new AdaptiveFact("Job ID:", model.JobId),
                        model.OriginalErrorTimestamp != null ? new AdaptiveFact("Timestamp:", model.OriginalErrorTimestamp?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)) : new AdaptiveFact(),
                        new AdaptiveFact("Status:", statusString)
                    ]
                }
            ],
            Actions = actions
        };
    }
}