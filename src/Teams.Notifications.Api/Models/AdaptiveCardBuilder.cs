using System;
using System.Collections.Generic;
using AdaptiveCards;

namespace Teams.Notifications.Api.Models;

public class AdaptiveCardBuilder
{
     public static AdaptiveCard CreateFileProcessingRestartedCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "File is reprocessing",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Large,
                    Color = AdaptiveTextColor.Warning
                },
                new AdaptiveTextBlock
                {
                    Text = "You requested to reprocess the file",
                    Wrap = true
                },
                new AdaptiveFactSet
                {
                    Facts = new List<AdaptiveFact>
                    {
                        new AdaptiveFact("File:", "Report_Q1_2025.xlsx"),
                        new AdaptiveFact("System:", "FrontGate"),
                        new AdaptiveFact("Job ID:", "FG-20250410-0915"),
                        new AdaptiveFact("Timestamp:", DateTime.UtcNow.ToLongTimeString()),
                        new AdaptiveFact("Status:", "Processing")
                    }
                }
            },
            Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = "📂 Open file in Teams",
                    Url = new Uri("https://teams.microsoft.com/file/Report_Q1_2025.xlsx")
                }
            }
        };

        return card;
    }
    public static AdaptiveCard CreateFileProcessingErrorCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "🚨 File Processing Error",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Large,
                    Color = AdaptiveTextColor.Attention
                },
                new AdaptiveTextBlock
                {
                    Text = "There was an issue processing the file.",
                    Wrap = true
                },
                new AdaptiveFactSet
                {
                    Facts = new List<AdaptiveFact>
                    {
                        new AdaptiveFact("File:", "Report_Q1_2025.xlsx"),
                        new AdaptiveFact("System:", "FrontGate"),
                        new AdaptiveFact("Job ID:", "FG-20250410-0915"),
                        new AdaptiveFact("Timestamp:", DateTime.UtcNow.ToLongTimeString()),
                        new AdaptiveFact("Status:", "❌ Failed")
                    }
                }
            },
            Actions = new List<AdaptiveAction>
            {
                new AdaptiveShowCardAction
                {
                    Title = "🔍 View Error Details",
                    Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
                    {
                        Body = new List<AdaptiveElement>
                        {
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
                        }
                    }
                },
                new AdaptiveOpenUrlAction
                {
                    Title = "📂 Open in Teams",
                    Url = new Uri("https://teams.microsoft.com/file/Report_Q1_2025.xlsx")
                },
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
                }
            }
        };

        return card;
    }
}