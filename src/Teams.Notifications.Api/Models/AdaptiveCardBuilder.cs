using System;
using System.Collections.Generic;
using AdaptiveCards;

namespace Teams.Notifications.Api.Models;

public class AdaptiveCardBuilder
{
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
                        new AdaptiveFact("Timestamp:", "2025-04-10 14:22 UTC"),
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
                new AdaptiveSubmitAction
                {
                    Title = "🔁 Reprocess File",
                    Data = new
                    {
                        action = "reprocessFile",
                        fileId = "file123",
                        jobId = "FG-20250410-0915"
                    }
                }
            }
        };

        return card;
    }
}